using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheAlarm
{
	public sealed class AppState
	{
		public const int CurrentSchemaVersion = 1;

		public int SchemaVersion { get; set; } = CurrentSchemaVersion;
		public ProcessRulesState ProcessRules { get; set; } = new ProcessRulesState();
		public List<AlarmState> Alarms { get; set; } = new List<AlarmState>();
		public MacroState Macros { get; set; } = new MacroState();

		[JsonExtensionData]
		public Dictionary<string, JsonElement>? FutureData { get; set; }

		public AppState Normalize()
		{
			SchemaVersion = CurrentSchemaVersion;
			ProcessRules ??= new ProcessRulesState();
			ProcessRules.Normalize();
			Macros ??= new MacroState();
			Macros.Normalize();

			var normalizedAlarms = new List<AlarmState>();
			if (Alarms != null)
			{
				foreach (var alarm in Alarms)
				{
					if (alarm == null)
					{
						continue;
					}

					normalizedAlarms.Add(alarm.Normalize());
				}
			}

			Alarms = normalizedAlarms;
			return this;
		}
	}

	public sealed class MacroState
	{
		public List<MacroDefinition> Definitions { get; set; } = new List<MacroDefinition>();

		public void Normalize()
		{
			var normalized = new List<MacroDefinition>();
			if (Definitions == null)
			{
				Definitions = normalized;
				return;
			}

			foreach (var definition in Definitions)
			{
				if (definition == null)
				{
					continue;
				}

				normalized.Add(definition.Clone().Normalize());
			}

			Definitions = normalized;
		}
	}

	public sealed class MacroDefinition
	{
		public string Id { get; set; } = Guid.NewGuid().ToString("N");
		public bool IsActive { get; set; }
		public MacroHotkey Hotkey { get; set; } = new MacroHotkey();
		public string RunnerType { get; set; } = MacroRunnerTypes.Cmd;
		public string ScriptText { get; set; } = string.Empty;

		public MacroDefinition Clone()
		{
			return new MacroDefinition
			{
				Id = Id,
				IsActive = IsActive,
				Hotkey = Hotkey?.Clone() ?? new MacroHotkey(),
				RunnerType = RunnerType,
				ScriptText = ScriptText
			};
		}

		public MacroDefinition Normalize()
		{
			Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("N") : Id.Trim();
			Hotkey ??= new MacroHotkey();
			Hotkey = Hotkey.Normalize();
			RunnerType = MacroRunnerTypes.Normalize(RunnerType);
			ScriptText ??= string.Empty;
			return this;
		}
	}

	public sealed class MacroHotkey
	{
		public string Modifiers { get; set; } = string.Empty;
		public string Key { get; set; } = string.Empty;

		public MacroHotkey Clone()
		{
			return new MacroHotkey
			{
				Modifiers = Modifiers,
				Key = Key
			};
		}

		public MacroHotkey Normalize()
		{
			Modifiers = (Modifiers ?? string.Empty).Trim();
			Key = (Key ?? string.Empty).Trim();
			return this;
		}
	}

	public static class MacroRunnerTypes
	{
		public const string Cmd = "cmd";
		public const string PowerShell = "PowerShell";

		public static string Normalize(string? value)
		{
			if (string.Equals(value, PowerShell, StringComparison.OrdinalIgnoreCase))
			{
				return PowerShell;
			}

			return Cmd;
		}
	}

	public sealed class ProcessRulesState
	{
		public List<ProcessRule> CloseProcesses { get; set; } = new List<ProcessRule>();
		public List<ProcessRule> MinimizeProcesses { get; set; } = new List<ProcessRule>();

		public void Normalize()
		{
			CloseProcesses = NormalizeRules(CloseProcesses);
			MinimizeProcesses = NormalizeRules(MinimizeProcesses);
		}

		private static List<ProcessRule> NormalizeRules(List<ProcessRule>? rules)
		{
			var normalized = new List<ProcessRule>();
			if (rules == null)
			{
				return normalized;
			}

			foreach (var rule in rules)
			{
				if (rule == null)
				{
					continue;
				}

				var copy = rule.Normalize();
				if (!string.IsNullOrWhiteSpace(copy.Name))
				{
					normalized.Add(copy);
				}
			}

			return normalized;
		}
	}

	public sealed class ProcessRule
	{
		public string Name { get; set; } = string.Empty;
		public bool ProtectChildren { get; set; }

		public ProcessRule Clone()
		{
			return new ProcessRule
			{
				Name = Name,
				ProtectChildren = ProtectChildren
			};
		}

		public ProcessRule Normalize()
		{
			Name = NormalizeName(Name);
			return this;
		}

		public static string NormalizeName(string? input)
		{
			var value = (input ?? string.Empty).Trim().Trim('"');
			if (value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				value = value.Substring(0, value.Length - 4);
			}

			try
			{
				var fileName = System.IO.Path.GetFileNameWithoutExtension(value);
				if (!string.IsNullOrWhiteSpace(fileName))
				{
					return fileName;
				}
			}
			catch
			{
			}

			return value;
		}
	}

	public sealed class AlarmState
	{
		public DateTime TimeUtc { get; set; }
		public string Message { get; set; } = string.Empty;
		public bool IsDaily { get; set; }

		public AlarmState Clone()
		{
			return new AlarmState
			{
				TimeUtc = TimeUtc,
				Message = Message,
				IsDaily = IsDaily
			};
		}

		public AlarmState Normalize()
		{
			TimeUtc = NormalizeUtc(TimeUtc);
			Message ??= string.Empty;
			return this;
		}

		private static DateTime NormalizeUtc(DateTime value)
		{
			if (value.Kind == DateTimeKind.Utc)
			{
				return value;
			}

			if (value.Kind == DateTimeKind.Local)
			{
				return value.ToUniversalTime();
			}

			return DateTime.SpecifyKind(value, DateTimeKind.Utc);
		}
	}

	public sealed class EncryptedFileEnvelope
	{
		public const int CurrentVersion = 1;

		public int Version { get; set; } = CurrentVersion;
		public string Format { get; set; } = "dpapi-current-user";
		public string CiphertextBase64 { get; set; } = string.Empty;
	}
}
