using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheAlarm
{
	public sealed class AppState
	{
		public string AlarmSoundPath { get; set; } = string.Empty;
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
			if (!Macros.Definitions.Any(m => m.IsCornerMacro))
				Macros.Definitions.Insert(0, new MacroDefinition { Id = "screen-corners", Name = "Углы экрана", IsCornerMacro = true, IsActive = true,
					Actions = new ProcessRulesState { CloseProcesses = ProcessRules.CloseProcesses.Select(p => p.Clone()).ToList(), MinimizeProcesses = ProcessRules.MinimizeProcesses.Select(p => p.Clone()).ToList() } });

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
			var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

				var copy = definition.Clone().Normalize();
				if (!ids.Add(copy.Id)) { copy.Id = Guid.NewGuid().ToString("N"); ids.Add(copy.Id); }
				normalized.Add(copy);
			}

			Definitions = normalized;
		}
	}

	public sealed class MacroDefinition
	{
		public string Name { get; set; } = "Новый макрос";
		public bool IsCornerMacro { get; set; }
		public bool TopLeft { get; set; }
		public bool TopRight { get; set; } = true;
		public bool BottomLeft { get; set; }
		public bool BottomRight { get; set; } = true;
		public ProcessAction TopLeftAction { get; set; } = ProcessAction.Close;
		public ProcessAction TopRightAction { get; set; } = ProcessAction.Close;
		public ProcessAction BottomLeftAction { get; set; } = ProcessAction.Minimize;
		public ProcessAction BottomRightAction { get; set; } = ProcessAction.Minimize;
		public ProcessAction GetCornerAction(int corner) => corner switch { 0 => TopLeftAction, 1 => TopRightAction, 2 => BottomLeftAction, 3 => BottomRightAction, _ => throw new ArgumentOutOfRangeException(nameof(corner)) };
		public ProcessRulesState Actions { get; set; } = new();
		public bool? ScriptEnabled { get; set; }
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
				Name = Name, IsCornerMacro = IsCornerMacro, TopLeft = TopLeft, TopRight = TopRight,
				BottomLeft = BottomLeft, BottomRight = BottomRight, ScriptEnabled = ScriptEnabled,
				TopLeftAction = TopLeftAction, TopRightAction = TopRightAction, BottomLeftAction = BottomLeftAction, BottomRightAction = BottomRightAction,
				Actions = new ProcessRulesState { CloseProcesses = (Actions?.CloseProcesses ?? new()).Where(p => p != null).Select(p => p.Clone()).ToList(), MinimizeProcesses = (Actions?.MinimizeProcesses ?? new()).Where(p => p != null).Select(p => p.Clone()).ToList() },
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
			Name ??= "Макрос";
			Actions ??= new();
			Actions.Normalize();
			ScriptEnabled ??= !string.IsNullOrWhiteSpace(ScriptText);
			if (!Enum.IsDefined(TopLeftAction)) TopLeftAction = ProcessAction.Close;
			if (!Enum.IsDefined(TopRightAction)) TopRightAction = ProcessAction.Close;
			if (!Enum.IsDefined(BottomLeftAction)) BottomLeftAction = ProcessAction.Minimize;
			if (!Enum.IsDefined(BottomRightAction)) BottomRightAction = ProcessAction.Minimize;
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
			try
			{
				var fileName = System.IO.Path.GetFileName(value);
				if (!string.IsNullOrWhiteSpace(fileName))
				{
					value = fileName;
				}
			}
			catch
			{
			}

			return value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? value[..^4] : value;
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
