using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace CURSORTrayApp
{
	// Главный контекст приложения, управляющий системным треем и всеми формами
	public class TrayAppContext : ApplicationContext
	{
		// Иконка в системном трее
		private readonly NotifyIcon _notifyIcon;
		// Форма с будильником (основное окно)
		private readonly AlarmForm _alarmForm;
		// Форма настроек (скрытое окно)
		private readonly SettingsForm _settingsForm;
		// Всплывающее окно с уведомлениями
		private readonly PopupForm _popupForm;
		// Таймер для проверки сработавших будильников
		private readonly System.Windows.Forms.Timer _alarmTimer;
		// Таймер для проверки углов экрана (резервный)
		private readonly System.Windows.Forms.Timer _cornerCheckTimer;
		// Глобальный хук мыши для отслеживания движения курсора
		private readonly LowLevelMouseHook _mouseHook = new LowLevelMouseHook();
		// Окно для регистрации глобальных горячих клавиш
		private readonly GlobalHotkeyWindow _hotkeyWindow;

		// Текущий режим работы приложения
		private AppMode _mode = AppMode.VisibleAlarm;

		public TrayAppContext()
		{
			// Создание всех форм приложения
			_alarmForm = new AlarmForm();
			_settingsForm = new SettingsForm();
			_popupForm = new PopupForm();

			// Загрузка пользовательской иконки из файла icon.jpg или создание стандартной
			Icon trayIcon = SystemIcons.Application;
			try
			{
				// Пытаемся загрузить icon.jpg из папки с программой
				string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.jpg");
				if (File.Exists(iconPath))
				{
					using var bmp = new Bitmap(iconPath);
					trayIcon = Icon.FromHandle(bmp.GetHicon());
				}
				else
				{
					// Если файл не найден, создаем простую иконку программно
					trayIcon = CreateDefaultIcon();
				}
			}
			catch 
			{
				// В случае ошибки создаем иконку по умолчанию
				trayIcon = CreateDefaultIcon();
			}

			// Настройка иконки в системном трее
			_notifyIcon = new NotifyIcon
			{
				Text = "The Alarm",
				Icon = trayIcon,
				Visible = true
			};
			_notifyIcon.MouseClick += NotifyIcon_MouseClick;
			_notifyIcon.ContextMenuStrip = BuildContextMenu();

			// Загрузка сохраненного списка приложений
			LoadApplicationsList();

			// Подписка на события форм
			_alarmForm.RequestPopup += AlarmForm_RequestPopup;
			_alarmForm.FormClosing += AnyForm_FormClosingToTray;
			_settingsForm.FormClosing += AnyForm_FormClosingToTray;

			// Таймер проверки будильников (каждую секунду)
			_alarmTimer = new System.Windows.Forms.Timer { Interval = 1000 };
			_alarmTimer.Tick += AlarmTimer_Tick;
			_alarmTimer.Start();

			// Резервный таймер проверки углов экрана (каждые 60 мс)
			_cornerCheckTimer = new System.Windows.Forms.Timer { Interval = 60 };
			_cornerCheckTimer.Tick += CornerCheckTimer_Tick;
			_cornerCheckTimer.Start();

			// Запуск глобального хука мыши для отслеживания движения курсора
			_mouseHook.MouseMove += MouseHook_MouseMove;
			_mouseHook.Start();

			// Регистрация глобальных горячих клавиш (Ctrl+Alt+F1)
			_hotkeyWindow = new GlobalHotkeyWindow();
			_hotkeyWindow.CanToggleEvaluator = () => _alarmForm.Visible || _settingsForm.Visible;
			_hotkeyWindow.ToggleRequested += ToggleSettings;
		}

		// Создание стандартной иконки программно (циферблат часов)
		private static Icon CreateDefaultIcon()
		{
			try
			{
				// Создаем bitmap 32x32 пикселей для качественной иконки
				var bmp = new Bitmap(32, 32);
				using (var g = Graphics.FromImage(bmp))
				{
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					
					// Фон - темно-синий градиент
					using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
						new Rectangle(0, 0, 32, 32),
						Color.FromArgb(0, 90, 180),
						Color.FromArgb(0, 50, 120),
						45f))
					{
						g.FillEllipse(brush, 0, 0, 31, 31);
					}
					
					// Белая рамка циферблата
					using (var pen = new Pen(Color.White, 2))
					{
						g.DrawEllipse(pen, 2, 2, 27, 27);
					}
					
					// Стрелки часов
					using (var pen = new Pen(Color.White, 2))
					{
						// Часовая стрелка (на 10 часов)
						g.DrawLine(pen, 16, 16, 11, 8);
						// Минутная стрелка (на 2 часа)
						g.DrawLine(pen, 16, 16, 23, 10);
					}
					
					// Центральная точка
					using (var brush = new SolidBrush(Color.White))
					{
						g.FillEllipse(brush, 14, 14, 4, 4);
					}
				}
				
				// Преобразуем в иконку
				IntPtr hIcon = bmp.GetHicon();
				Icon icon = Icon.FromHandle(hIcon);
				
				// Сохраняем иконку в файл app.ico для использования в проекте
				try
				{
					string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
					using (var fs = new FileStream(iconPath, FileMode.Create))
					{
						icon.Save(fs);
					}
				}
				catch { }
				
				return icon;
			}
			catch
			{
				// В случае ошибки возвращаем системную иконку
				return SystemIcons.Application;
			}
		}

		// Построение контекстного меню для иконки в трее
		private ContextMenuStrip BuildContextMenu()
		{
			var menu = new ContextMenuStrip();
			var openAlarm = new ToolStripMenuItem("Open The Alarm");
			openAlarm.Click += (_, __) => ShowAlarm();
			var exit = new ToolStripMenuItem("Exit");
			exit.Click += (_, __) => ExitApplication();
			menu.Items.Add(openAlarm);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(exit);
			return menu;
		}

		// Обработчик клика по иконке в трее
		private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ShowAlarm();
			}
		}

		// Показать окно будильника
		private void ShowAlarm()
		{
			_mode = AppMode.VisibleAlarm;
			_alarmForm.ShowInTaskbar = true;
			_alarmForm.Show();
			_alarmForm.Activate();
		}

		// Обработчик запроса на показ всплывающего окна
		private void AlarmForm_RequestPopup(object? sender, string message)
		{
			_popupForm.SetMessage(message);
			_popupForm.Show();
			_popupForm.Activate();
		}

		// Обработчик таймера для проверки сработавших будильников
		private void AlarmTimer_Tick(object? sender, EventArgs e)
		{
			var due = _alarmForm.ConsumeDueAlarms();
			foreach (var msg in due)
			{
				AlarmForm_RequestPopup(this, msg);
			}
		}

		// Обработчик резервного таймера проверки углов экрана
		private void CornerCheckTimer_Tick(object? sender, EventArgs e)
		{
			// Резервный метод на случай, если хук мыши не работает
			GetCursorPos(out POINT p);
			CheckCornersAndAct(p.X, p.Y);
		}

		// Обработчик движения мыши (основной метод отслеживания)
		private void MouseHook_MouseMove(int x, int y)
		{
			// Проверяем углы независимо от режима (работает даже когда окна скрыты!)
			CheckCornersAndAct(x, y);
		}

		// Проверка нахождения курсора в углах экрана и выполнение действий
		private void CheckCornersAndAct(int x, int y)
		{
			// Получаем размеры экрана
			int screenW = GetSystemMetrics(SM_CXSCREEN);
			int screenH = GetSystemMetrics(SM_CYSCREEN);
			
			// Определяем пороги для углов (2 пикселя от края)
			int marginX = 2;
			int rightThreshold = screenW - 1 - marginX;
			int topBand = 1 + marginX;
			int bottomThreshold = screenH - 1 - marginX;
			
			bool rightOfThreshold = x > rightThreshold;
			
			// Правый верхний угол - закрыть процессы
			if (rightOfThreshold && y < topBand)
			{
				PerformProcessAction(ProcessAction.Close);
				return;
			}
			
			// Правый нижний угол - свернуть процессы
			if (rightOfThreshold && y > bottomThreshold)
			{
				PerformProcessAction(ProcessAction.Minimize);
			}
		}

		// Время последнего выполнения действия (для защиты от повторных срабатываний)
		private DateTime _lastCornerActionUtc = DateTime.MinValue;

		// Выполнение действия над процессами (закрытие или сворачивание)
		private void PerformProcessAction(ProcessAction action)
		{
			// Защита от повторных срабатываний (debounce)
			var nowUtc = DateTime.UtcNow;
			if ((nowUtc - _lastCornerActionUtc).TotalMilliseconds < 250)
				return;
			_lastCornerActionUtc = nowUtc;

			// Получаем список процессов для данного действия
			var targets = _settingsForm.GetProcessConfigsForAction(action);
			foreach (var procConfig in targets)
			{
				var normalized = NormalizeProcessName(procConfig.Name);
				if (string.IsNullOrWhiteSpace(normalized)) continue;
				
				// Находим все запущенные процессы с таким именем
				foreach (var p in Process.GetProcessesByName(normalized))
				{
					try
					{
						if (action == ProcessAction.Close)
						{
							// Быстрое принудительное закрытие процесса
							TryTaskKill(normalized);
						}
						else if (action == ProcessAction.Minimize)
						{
							// Сворачивание всех окон процесса (кроме служебных)
							EnumThreadWindowsForProcess(p, (hWnd) =>
							{
								if (!ShouldAffectWindow(hWnd)) return true;
								SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero);
								ShowWindow(hWnd, SW_FORCEMINIMIZE);
								return true;
							});
						}
					}
					catch (Exception) { }
				}
			}
		}

		// Принудительное закрытие процесса через taskkill
		private static void TryTaskKill(string processBaseName)
		{
			try
			{
				var psi = new ProcessStartInfo("taskkill", $"/IM {processBaseName}.exe /F /T")
				{
					CreateNoWindow = true,
					UseShellExecute = false,
					WindowStyle = ProcessWindowStyle.Hidden,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				};
				using var proc = Process.Start(psi);
				proc?.WaitForExit(3000);
			}
			catch { }
		}

		// Нормализация имени процесса (удаление расширения и извлечение имени файла)
		private static string NormalizeProcessName(string input)
		{
			var s = input.Trim().Trim('"');
			if (s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				 s = s.Substring(0, s.Length - 4);
			
			// Если указан полный путь, извлекаем только имя файла
			try
			{
				var fileName = System.IO.Path.GetFileNameWithoutExtension(s);
				if (!string.IsNullOrEmpty(fileName)) return fileName;
			}
			catch { }
			return s;
		}

		// Перечисление всех окон процесса
		private static void EnumThreadWindowsForProcess(Process process, Func<IntPtr, bool> onWindow)
		{
			foreach (ProcessThread thread in process.Threads)
			{
				EnumThreadWindows((uint)thread.Id, (hWnd, lParam) =>
				{
					// Обрабатываем только видимые окна верхнего уровня
					if (IsWindowVisible(hWnd))
					{
						return onWindow(hWnd);
					}
					return true;
				}, IntPtr.Zero);
			}
		}

		// Переключение отображения окна настроек (по горячим клавишам)
		private void ToggleSettings(GlobalHotkeyWindow.ToggleKind kind)
		{
			if (kind == GlobalHotkeyWindow.ToggleKind.Open)
			{
				if (_settingsForm.Visible)
				{
					// Если настройки открыты - скрываем их
					_settingsForm.Hide();
					_mode = AppMode.HiddenNoWindow;
				}
				else
				{
					// Если настройки скрыты - показываем их и скрываем будильник
					_alarmForm.Hide();
					_settingsForm.Show();
					_settingsForm.Activate();
					_mode = AppMode.HiddenWithSettings;
				}
			}
			else if (kind == GlobalHotkeyWindow.ToggleKind.Close)
			{
				// Закрытие настроек
				_settingsForm.Hide();
				_mode = AppMode.HiddenNoWindow;
			}
		}

		// Обработчик попытки закрытия формы (сворачивание в трей вместо закрытия)
		private void AnyForm_FormClosingToTray(object? sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			(sender as Form)?.Hide();
			_mode = AppMode.VisibleAlarm;
		}

		// Выход из приложения
		private void ExitApplication()
		{
			// Сохраняем список приложений перед выходом
			SaveApplicationsList();
			_notifyIcon.Visible = false;
			_hotkeyWindow.Dispose();
			Environment.Exit(0);
		}

		// Путь к файлу конфигурации (ПОРТАТИВНЫЙ - рядом с exe-файлом!)
		private static readonly string ConfigFilePath = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"config.json"
		);

		// Загрузка списка приложений из файла конфигурации
		private void LoadApplicationsList()
		{
			try
			{
				// Проверяем существование файла перед чтением
				if (File.Exists(ConfigFilePath))
				{
					string json = File.ReadAllText(ConfigFilePath);
					var config = JsonSerializer.Deserialize<AppConfig>(json);
					if (config != null)
					{
						_settingsForm.LoadConfiguration(config.CloseProcesses, config.MinimizeProcesses);
					}
				}
				// Если файла нет - ничего не делаем, приложение работает с пустыми списками
			}
			catch { }
		}

		// Сохранение списка приложений в файл конфигурации
		private void SaveApplicationsList()
		{
			try
			{
				var config = new AppConfig
				{
					CloseProcesses = _settingsForm.GetProcessConfigsForAction(ProcessAction.Close),
					MinimizeProcesses = _settingsForm.GetProcessConfigsForAction(ProcessAction.Minimize)
				};

				// Файл будет создан автоматически в папке с программой
				string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(ConfigFilePath, json);
			}
			catch { }
		}

		// Класс для хранения конфигурации приложения
		private class AppConfig
		{
			public List<ProcessConfig> CloseProcesses { get; set; } = new List<ProcessConfig>();
			public List<ProcessConfig> MinimizeProcesses { get; set; } = new List<ProcessConfig>();
		}

		// Класс для хранения настроек процесса
		public class ProcessConfig
		{
			public string Name { get; set; } = string.Empty;
			public bool ProtectChildren { get; set; } = false;
		}

		// Освобождение ресурсов при закрытии приложения
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_notifyIcon.Dispose();
				_alarmTimer.Dispose();
				_cornerCheckTimer.Dispose();
				_hotkeyWindow.Dispose();
				_mouseHook.Dispose();
			}
			base.Dispose(disposing);
		}

		// ==================== Win32 API функции ====================
		
		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(int nIndex);
		
		// Константы для GetSystemMetrics
		private const int SM_CXSCREEN = 0;  // Ширина экрана
		private const int SM_CYSCREEN = 1;  // Высота экрана

		// Константы для сообщений Windows
		private const int WM_CLOSE = 0x0010;
		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_CLOSE = 0xF060;
		private const int SC_MINIMIZE = 0xF020;
		private const int SW_MINIMIZE = 6;
		private const int SW_FORCEMINIMIZE = 11;
		
		// Константы для стилей окон
		private const int GWL_EXSTYLE = -20;
		private const int WS_EX_TOOLWINDOW = 0x00000080;
		private const int WS_EX_NOACTIVATE = 0x08000000;
		private const int WS_EX_APPWINDOW = 0x00040000;

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

		// Проверка, нужно ли влиять на данное окно (исключаем служебные окна IME)
		private static bool ShouldAffectWindow(IntPtr hWnd)
		{
			// Игнорируем окна IME, служебные окна и неактивируемые окна
			int ex = GetWindowLong(hWnd, GWL_EXSTYLE);
			if ((ex & WS_EX_TOOLWINDOW) != 0) return false;
			if ((ex & WS_EX_NOACTIVATE) != 0) return false;
			
			// Исключаем окна IME по имени класса
			var sb = new System.Text.StringBuilder(256);
			if (GetClassName(hWnd, sb, sb.Capacity) > 0)
			{
				var cls = sb.ToString();
				if (cls.IndexOf("IME", StringComparison.OrdinalIgnoreCase) >= 0) return false;
				if (string.Equals(cls, "Default IME", StringComparison.OrdinalIgnoreCase)) return false;
				if (string.Equals(cls, "MSCTFIME UI", StringComparison.OrdinalIgnoreCase)) return false;
			}
			return true;
		}

		// Структура для хранения координат точки
		[StructLayout(LayoutKind.Sequential)]
		private struct POINT { public int X; public int Y; }

		// Перечисление режимов работы приложения
		private enum AppMode
		{
			VisibleAlarm,       // Видимое окно будильника
			HiddenWithSettings, // Скрытый режим с открытыми настройками
			HiddenNoWindow      // Полностью скрытый режим (только трей)
		}
	}
}
