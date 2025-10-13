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
			// Включение визуальных стилей Windows
			Application.EnableVisualStyles();
			
			// Использование совместимого рендеринга текста
			Application.SetCompatibleTextRenderingDefault(false);
			
			// Запуск приложения с контекстом трея (без главной формы)
			Application.Run(new TrayAppContext());
		}
	}
}
