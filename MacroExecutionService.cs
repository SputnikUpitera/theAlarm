using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace TheAlarm
{
	public sealed class MacroExecutionService
	{
		public bool TryExecute(MacroDefinition definition, out string? errorMessage)
		{
			ArgumentNullException.ThrowIfNull(definition);

			definition = definition.Clone().Normalize();
			if (string.IsNullOrWhiteSpace(definition.ScriptText))
			{
				errorMessage = "Macro text is empty.";
				return false;
			}

			var startInfo = BuildStartInfo(definition);
			try
			{
				var process = Process.Start(startInfo);
				if (process == null)
				{
					errorMessage = "Failed to start elevated hidden shell process.";
					return false;
				}

				errorMessage = null;
				return true;
			}
			catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
			{
				errorMessage = "Macro execution was cancelled in the UAC prompt.";
				return false;
			}
			catch (Exception ex)
			{
				AppLog.Error("Failed to execute macro.", ex);
				errorMessage = ex.Message;
				return false;
			}
		}

		private static ProcessStartInfo BuildStartInfo(MacroDefinition definition)
		{
			var runnerType = MacroRunnerTypes.Normalize(definition.RunnerType);
			var script = runnerType == MacroRunnerTypes.PowerShell
				? definition.ScriptText
				: BuildCmdBootstrapScript(definition.ScriptText);

			return new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = BuildPowerShellArguments(script),
				UseShellExecute = true,
				Verb = "runas",
				WindowStyle = ProcessWindowStyle.Hidden
			};
		}

		private static string BuildPowerShellArguments(string script)
		{
			var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
			return $"-NoLogo -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand {encodedScript}";
		}

		private static string BuildCmdBootstrapScript(string cmdScriptText)
		{
			var encodedScript = Convert.ToBase64String(Encoding.UTF8.GetBytes(cmdScriptText));
			return string.Join(
				Environment.NewLine,
				"$ErrorActionPreference = 'Stop'",
				$"$scriptText = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String('{encodedScript}'))",
				"$psi = New-Object System.Diagnostics.ProcessStartInfo",
				"$psi.FileName = $env:ComSpec",
				"$psi.Arguments = '/Q /D /K'",
				"$psi.UseShellExecute = $false",
				"$psi.CreateNoWindow = $true",
				"$psi.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden",
				"$psi.RedirectStandardInput = $true",
				"$process = [System.Diagnostics.Process]::Start($psi)",
				"if ($null -eq $process) { throw 'Failed to start hidden cmd process.' }",
				"$process.StandardInput.Write($scriptText)",
				"if (-not $scriptText.EndsWith([Environment]::NewLine)) { $process.StandardInput.WriteLine() }",
				"$process.StandardInput.WriteLine('exit')",
				"$process.StandardInput.Close()"
			);
		}
	}
}
