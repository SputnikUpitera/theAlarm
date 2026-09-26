using System;
using System.Windows.Forms;

namespace TheAlarm
{
	// Главный класс приложения - точка входа
	internal static class Program
	{
		// Главный метод, вызываемый при запуске приложения
		[STAThread]
		private static void Main()
		{
			using var instance = new System.Threading.Mutex(false, @"Local\TheAlarm.Application", out _);
			bool ownsInstance;
			try { ownsInstance = instance.WaitOne(0); }
			catch (System.Threading.AbandonedMutexException) { ownsInstance = true; }
			if (!ownsInstance)
			{
				MessageBox.Show("The Alarm is already running in the tray. Exit that copy before starting another build.", "The Alarm");
				return;
			}
			Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			Application.ThreadException += (_, e) =>
			{
				AppLog.Error("Unhandled UI exception", e.Exception);
				MessageBox.Show("An unexpected error occurred. Details: %LOCALAPPDATA%\\TheAlarm\\thealarm.log", "The Alarm");
			};
			AppDomain.CurrentDomain.UnhandledException += (_, e) => AppLog.Error("Fatal exception", e.ExceptionObject as Exception);
			// Включение визуальных стилей Windows
			Application.EnableVisualStyles();
			
			// Использование совместимого рендеринга текста
			Application.SetCompatibleTextRenderingDefault(false);
			
			// Запуск приложения с контекстом трея (без главной формы)
			try { Application.Run(new TrayAppContext()); }
			finally { instance.ReleaseMutex(); }
		}
	}
}
