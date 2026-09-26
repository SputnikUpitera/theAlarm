using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TheAlarm
{
	public sealed class AppStateRepository
	{
		private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

		private readonly EncryptionService _encryptionService;
		private readonly MigrationService _migrationService;
		private readonly JsonSerializerOptions _jsonOptions;
		private bool _saveBlocked;

		public AppStateRepository(string? directory = null)
		{
			var baseDirectory = Path.GetFullPath(directory ?? AppContext.BaseDirectory);
			EncryptedConfigPath = Path.Combine(baseDirectory, "config.dat");
			LegacyConfigPath = Path.Combine(baseDirectory, "config.json");
			_jsonOptions = CreateJsonOptions();
			_encryptionService = new EncryptionService();
			_migrationService = new MigrationService(_jsonOptions);
		}

		public string EncryptedConfigPath { get; }
		public string LegacyConfigPath { get; }

		public AppStateLoadResult Load()
		{
			_saveBlocked = false;
			try
			{
				if (File.Exists(EncryptedConfigPath))
				{
					return LoadEncryptedState();
				}

				if (File.Exists(LegacyConfigPath))
				{
					return MigrateLegacyState();
				}
			}
			catch (Exception ex)
			{
				AppLog.Error("Unexpected app state load failure.", ex);
				return FailedLoad("Не удалось загрузить сохраненное состояние.");
			}

			return AppStateLoadResult.Success(new AppState().Normalize());
		}

		public bool Save(AppState state, out string? errorMessage)
		{
			ArgumentNullException.ThrowIfNull(state);

			if (_saveBlocked)
			{
				errorMessage = "Сохранение заблокировано: исходная конфигурация не прочитана. Файл оставлен без изменений. Восстановите его и перезапустите приложение.";
				return false;
			}
			var tempPath = EncryptedConfigPath + ".tmp";

			try
			{
				var normalizedState = state.Normalize();
				var plainBytes = JsonSerializer.SerializeToUtf8Bytes(normalizedState, _jsonOptions);
				var encryptedBytes = _encryptionService.Protect(plainBytes);
				var envelope = new EncryptedFileEnvelope
				{
					Version = EncryptedFileEnvelope.CurrentVersion,
					Format = "dpapi-current-user",
					CiphertextBase64 = Convert.ToBase64String(encryptedBytes)
				};

				var envelopeJson = JsonSerializer.Serialize(envelope, _jsonOptions);
				File.WriteAllText(tempPath, envelopeJson, Utf8NoBom);

				if (File.Exists(EncryptedConfigPath))
				{
					File.Replace(tempPath, EncryptedConfigPath, null, true);
				}
				else
				{
					File.Move(tempPath, EncryptedConfigPath);
				}

				errorMessage = null;
				return true;
			}
			catch (Exception ex)
			{
				AppLog.Error("Failed to save app state.", ex);
				errorMessage = "Не удалось сохранить конфигурацию на диск.";
				return false;
			}
			finally
			{
				TryDeleteFile(tempPath);
			}
		}

		private AppStateLoadResult LoadEncryptedState()
		{
			try
			{
				var envelopeJson = File.ReadAllText(EncryptedConfigPath, Utf8NoBom);
				var envelope = JsonSerializer.Deserialize<EncryptedFileEnvelope>(envelopeJson, _jsonOptions);
				if (envelope == null || string.IsNullOrWhiteSpace(envelope.CiphertextBase64))
				{
					throw new InvalidDataException("Encrypted config envelope is empty.");
				}
				if (envelope.Version != EncryptedFileEnvelope.CurrentVersion || envelope.Format != "dpapi-current-user")
					throw new InvalidDataException("Unsupported encrypted config format.");

				var encryptedBytes = Convert.FromBase64String(envelope.CiphertextBase64);
				var plainBytes = _encryptionService.Unprotect(encryptedBytes);
				var state = JsonSerializer.Deserialize<AppState>(plainBytes, _jsonOptions) ?? throw new InvalidDataException("Empty app state.");
				if (state.SchemaVersion != AppState.CurrentSchemaVersion) throw new InvalidDataException("Unsupported app state version.");
				return AppStateLoadResult.Success(state.Normalize());
			}
			catch (CryptographicException ex)
			{
				AppLog.Error("Failed to decrypt encrypted app state.", ex);
				return FailedLoad("Не удалось расшифровать сохраненную конфигурацию текущего пользователя.");
			}
			catch (Exception ex)
			{
				AppLog.Error("Failed to read encrypted app state.", ex);
				return FailedLoad("Сохраненная конфигурация повреждена, недоступна или имеет неподдерживаемую версию.");
			}
		}

		private AppStateLoadResult MigrateLegacyState()
		{
			try
			{
				var migratedState = _migrationService.MigrateLegacyConfig(LegacyConfigPath);
				if (!Save(migratedState, out var saveError))
				{
					return FailedLoad($"Не удалось завершить миграцию legacy config.json: {saveError}");
				}

				string? warning = null;
				try
				{
					File.Delete(LegacyConfigPath);
				}
				catch (Exception ex)
				{
					AppLog.Error("Encrypted state was written, but legacy config.json could not be deleted.", ex);
					warning = "Старая plaintext-конфигурация была прочитана и перенесена, но удалить legacy config.json не удалось.";
				}

				AppLog.Info("Legacy config.json migrated to encrypted config.dat.");
				return AppStateLoadResult.Migrated(migratedState.Normalize(), warning);
			}
			catch (Exception ex)
			{
				AppLog.Error("Failed to migrate legacy config.json.", ex);
				return FailedLoad("Не удалось мигрировать legacy config.json.");
			}
		}

		private AppStateLoadResult FailedLoad(string message)
		{
			_saveBlocked = true;
			return AppStateLoadResult.WithWarning(new AppState().Normalize(), message + " Исходные файлы сохранены; запись заблокирована до восстановления конфигурации и перезапуска.");
		}

		private static JsonSerializerOptions CreateJsonOptions()
		{
			return new JsonSerializerOptions
			{
				WriteIndented = false,
				AllowTrailingCommas = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			};
		}

		private static void TryDeleteFile(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch
			{
			}
		}
	}

	public sealed class AppStateLoadResult
	{
		private AppStateLoadResult(AppState state, bool migratedFromLegacy, string? warningMessage)
		{
			State = state;
			MigratedFromLegacy = migratedFromLegacy;
			WarningMessage = warningMessage;
		}

		public AppState State { get; }
		public bool MigratedFromLegacy { get; }
		public string? WarningMessage { get; }

		public static AppStateLoadResult Success(AppState state)
		{
			return new AppStateLoadResult(state, false, null);
		}

		public static AppStateLoadResult Migrated(AppState state, string? warningMessage)
		{
			return new AppStateLoadResult(state, true, warningMessage);
		}

		public static AppStateLoadResult WithWarning(AppState state, string warningMessage)
		{
			return new AppStateLoadResult(state, false, warningMessage);
		}
	}
}
