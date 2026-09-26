using System;
using System.IO;
using System.Text;

namespace TheAlarm
{
	public static class AppLog
	{
		private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheAlarm", "thealarm.log");
		private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
		private static readonly object Sync = new object();

		public static void Info(string message)
		{
			Write("INFO", message, null);
		}

		public static void Error(string message, Exception? exception = null)
		{
			Write("ERROR", message, exception);
		}

		private static void Write(string level, string message, Exception? exception)
		{
			try
			{
				var text = $"{DateTime.UtcNow:O} [{level}] {message}";
				if (exception != null)
				{
					text += Environment.NewLine + exception;
				}

				text += Environment.NewLine;

				lock (Sync)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
					File.AppendAllText(LogFilePath, text, Utf8NoBom);
				}
			}
			catch
			{
			}
		}
	}
}
