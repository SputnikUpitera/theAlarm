using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TheAlarm
{
	public sealed class MigrationService
	{
		private readonly JsonSerializerOptions _jsonOptions;

		public MigrationService(JsonSerializerOptions jsonOptions)
		{
			_jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
		}

		public AppState MigrateLegacyConfig(string legacyConfigPath)
		{
			if (string.IsNullOrWhiteSpace(legacyConfigPath))
			{
				throw new ArgumentException("Legacy config path is required.", nameof(legacyConfigPath));
			}

			var json = File.ReadAllText(legacyConfigPath);
			var legacyConfig = JsonSerializer.Deserialize<LegacyConfig>(json, _jsonOptions);

			return new AppState
			{
				ProcessRules = new ProcessRulesState
				{
					CloseProcesses = CloneRules(legacyConfig?.CloseProcesses),
					MinimizeProcesses = CloneRules(legacyConfig?.MinimizeProcesses)
				}
			}.Normalize();
		}

		private static List<ProcessRule> CloneRules(List<ProcessRule>? rules)
		{
			var cloned = new List<ProcessRule>();
			if (rules == null)
			{
				return cloned;
			}

			foreach (var rule in rules)
			{
				if (rule == null)
				{
					continue;
				}

				cloned.Add(rule.Clone());
			}

			return cloned;
		}

		private sealed class LegacyConfig
		{
			public List<ProcessRule> CloseProcesses { get; set; } = new List<ProcessRule>();
			public List<ProcessRule> MinimizeProcesses { get; set; } = new List<ProcessRule>();
		}
	}
}
