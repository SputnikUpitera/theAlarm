using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TheAlarm
{
	// Форма настроек приложения (скрытое окно для управления процессами)
	public class SettingsForm : Form
	{
		// Список процессов для закрытия с флажками защиты дочерних процессов
		private readonly CheckedListBox _closeList;
		// Список процессов для сворачивания с флажками защиты дочерних процессов
		private readonly CheckedListBox _minimizeList;
		// Поле ввода имени процесса
		private readonly TextBox _addProcessBox;
		// Кнопка добавления в список закрытия
		private readonly Button _addToCloseButton;
		// Кнопка добавления в список сворачивания
		private readonly Button _addToMinimizeButton;
		// Кнопка удаления выбранного из списка закрытия
		private readonly Button _removeCloseButton;
		// Кнопка удаления выбранного из списка сворачивания
		private readonly Button _removeMinimizeButton;
		// Чекбокс для автозапуска с Windows
		private readonly CheckBox _autostartCheckbox;
		// Кнопка для принудительного включения автозапуска (с правами администратора)
		private readonly Button _forceAutostartButton;
		// Кнопка «Применить» — сохранить конфиг без закрытия формы
		private readonly Button _applyButton;
		// Флаг для предотвращения рекурсивных вызовов при изменении автозапуска
		private bool _isUpdatingAutostart = false;
		
		// Событие для уведомления об изменении конфигурации
		public event EventHandler? ConfigurationChanged;

		// Цвета темной темы (как в Cursor)
		private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
		private static readonly Color DarkControl = Color.FromArgb(45, 45, 45);
		private static readonly Color DarkBorder = Color.FromArgb(60, 60, 60);
		private static readonly Color LightText = Color.FromArgb(220, 220, 220);
		private static readonly Color AccentBlue = Color.FromArgb(0, 80, 140);

	public SettingsForm()
	{
		Text = "The Alarm - Settings";
		FormBorderStyle = FormBorderStyle.FixedDialog;
		StartPosition = FormStartPosition.CenterScreen;
		Width = 600;
		Height = 530;
			
			// Применение темной темы к форме
			BackColor = DarkBackground;
			ForeColor = LightText;

		// Константы для расположения элементов
		const int margin = 15;
		const int listWidth = 240;
		const int listHeight = 220;
		const int labelHeight = 35;

		// Метка для списка закрытия
		var closeLabel = new Label
		{
			Text = "Close Processes\n(Check = Protect Child Processes)",
			Left = margin,
			Top = margin,
			Width = listWidth,
			Height = labelHeight,
			ForeColor = LightText,
			Font = new Font("Segoe UI", 8.5f)
		};

		// Список процессов для закрытия (левая колонка) с флажками защиты дочерних процессов
		_closeList = new CheckedListBox 
		{ 
			Left = margin, 
			Top = margin + labelHeight, 
			Width = listWidth, 
			Height = listHeight,
			BackColor = DarkControl,
			ForeColor = LightText,
			BorderStyle = BorderStyle.FixedSingle,
			CheckOnClick = true
		};

		// Метка для списка сворачивания
		var minimizeLabel = new Label
		{
			Text = "Minimize Processes\n(Check = Protect Child Processes)",
			Left = margin * 2 + listWidth,
			Top = margin,
			Width = listWidth,
			Height = labelHeight,
			ForeColor = LightText,
			Font = new Font("Segoe UI", 8.5f)
		};
		
		// Список процессов для сворачивания (правая колонка) с флажками защиты дочерних процессов
		_minimizeList = new CheckedListBox 
		{ 
			Left = margin * 2 + listWidth, 
			Top = margin + labelHeight, 
			Width = listWidth, 
			Height = listHeight,
			BackColor = DarkControl,
			ForeColor = LightText,
			BorderStyle = BorderStyle.FixedSingle,
			CheckOnClick = true
		};
			
		// Поле ввода имени процесса
		_addProcessBox = new TextBox 
		{ 
			Left = margin, 
			Top = margin * 2 + labelHeight + listHeight, 
			Width = 180,
			BackColor = DarkControl,
			ForeColor = LightText,
			BorderStyle = BorderStyle.FixedSingle
		};
		
		// Кнопка "Add to Close" - рядом с полем ввода
		_addToCloseButton = new Button 
		{ 
			Text = "Add to Close", 
			Left = margin + 190, 
			Top = margin * 2 + labelHeight + listHeight, 
			Width = 110,
			BackColor = DarkControl,
			ForeColor = LightText,
			FlatStyle = FlatStyle.Flat
		};
		_addToCloseButton.FlatAppearance.BorderColor = DarkBorder;
		
		// Кнопка "Add to Minimize" - перемещена вправо, чтобы не пересекалась с "Add to Close"
		_addToMinimizeButton = new Button 
		{ 
			Text = "Add to Minimize", 
			Left = margin + 310, 
			Top = margin * 2 + labelHeight + listHeight, 
			Width = 130,
			BackColor = DarkControl,
			ForeColor = LightText,
			FlatStyle = FlatStyle.Flat
		};
		_addToMinimizeButton.FlatAppearance.BorderColor = DarkBorder;
		
		// Кнопки удаления выбранных элементов (Закрыть)
		_removeCloseButton = new Button 
		{ 
			Text = "Remove Selected (Close)", 
			Left = margin, 
			Top = margin * 3 + labelHeight + listHeight + 25, 
			Width = listWidth,
			BackColor = DarkControl,
			ForeColor = LightText,
			FlatStyle = FlatStyle.Flat
		};
		_removeCloseButton.FlatAppearance.BorderColor = DarkBorder;
		
		// Кнопки удаления выбранных элементов (Свернуть)
		_removeMinimizeButton = new Button 
		{ 
			Text = "Remove Selected (Min)", 
			Left = margin * 2 + listWidth, 
			Top = margin * 3 + labelHeight + listHeight + 25, 
			Width = listWidth,
			BackColor = DarkControl,
			ForeColor = LightText,
			FlatStyle = FlatStyle.Flat
		};
		_removeMinimizeButton.FlatAppearance.BorderColor = DarkBorder;

		// Кнопка «Применить» — сохранить конфиг в файл и применить во время работы
		_applyButton = new Button
		{
			Text = "Apply",
			Left = (Width - 120) / 2 - 40,
			Top = margin * 3 + labelHeight + listHeight + 65,
			Width = 120,
			Height = 26,
			BackColor = AccentBlue,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};
		_applyButton.FlatAppearance.BorderColor = AccentBlue;
		_applyButton.Click += (_, __) =>
		{
			OnConfigurationChanged();
			MessageBox.Show("Настройки применены и сохранены.", "The Alarm", MessageBoxButtons.OK, MessageBoxIcon.Information);
		};

		// Чекбокс автозапуска с Windows
		_autostartCheckbox = new CheckBox
		{
			Text = "Start with Windows",
			Left = margin,
			Top = margin * 4 + labelHeight + listHeight + 95,
			Width = 200,
			AutoSize = true,
			ForeColor = LightText
		};
		_isUpdatingAutostart = true;
		_autostartCheckbox.Checked = IsAutostartEnabled();
		_isUpdatingAutostart = false;
		_autostartCheckbox.CheckedChanged += AutostartCheckbox_CheckedChanged;

		// Кнопка для принудительного включения автозапуска (справа от чекбокса)
		_forceAutostartButton = new Button
		{
			Text = "Enable Autostart (Admin)",
			Left = margin + 200,
			Top = margin * 4 + labelHeight + listHeight + 95,
			Width = 180,
			Height = 24,
			BackColor = AccentBlue,
			ForeColor = Color.White,
			FlatStyle = FlatStyle.Flat
		};
		_forceAutostartButton.FlatAppearance.BorderColor = AccentBlue;
		_forceAutostartButton.Click += ForceAutostartButton_Click;

		// Отладочные элементы (показ координат курсора и тестовые кнопки)
		var cursorCoordsLabel = new Label 
		{ 
			Left = margin, 
			Top = margin * 5 + labelHeight + listHeight + 120, 
			Width = 240, 
			Height = 20, 
			Text = "(x,y)",
			ForeColor = LightText
		};
		
		var killSelectedButton = new Button 
		{ 
			Text = "Kill Selected (Close)", 
			Left = margin + 250, 
			Top = margin * 5 + labelHeight + listHeight + 120, 
			Width = 110,
			BackColor = DarkControl,
			ForeColor = LightText,
			FlatStyle = FlatStyle.Flat
		};
		killSelectedButton.FlatAppearance.BorderColor = DarkBorder;
		
		var minimizeSelectedButton = new Button 
		{ 
			Text = "Minimize Selected", 
			Left = margin + 370, 
			Top = margin * 5 + labelHeight + listHeight + 120, 
			Width = Width - margin - 400 - margin,  // Отступ до правого края
			BackColor = DarkControl,
			ForeColor = LightText,
			FlatStyle = FlatStyle.Flat
		};
		minimizeSelectedButton.FlatAppearance.BorderColor = DarkBorder;
			
			// Обработчик для тестовой кнопки "Kill Selected"
			killSelectedButton.Click += (_, __) =>
			{
				var name = _closeList.SelectedItem as string;
				if (!string.IsNullOrWhiteSpace(name))
				{
					foreach (var p in Process.GetProcessesByName(name))
					{
						try { p.Kill(); } catch { }
					}
				}
			};
			
			// Обработчик для тестовой кнопки "Minimize Selected"
			minimizeSelectedButton.Click += (_, __) =>
			{
				var name = _minimizeList.SelectedItem as string;
				if (!string.IsNullOrWhiteSpace(name))
				{
					foreach (var p in Process.GetProcessesByName(name))
					{
						try
						{
							foreach (ProcessThread thread in p.Threads)
							{
								EnumThreadWindows((uint)thread.Id, (hWnd, lParam) => { ShowWindow(hWnd, 6); return true; }, IntPtr.Zero);
							}
						}
						catch { }
					}
				}
			};
			
			// Таймер для отображения текущих координат курсора
			var debugTimer = new System.Windows.Forms.Timer { Interval = 100 };
			debugTimer.Tick += (_, __) =>
			{
				GetCursorPos(out var pt);
				cursorCoordsLabel.Text = $"X={pt.X} Y={pt.Y}";
			};
			debugTimer.Start();

			// Обработчик кнопки добавления в список закрытия
			_addToCloseButton.Click += (_, __) =>
			{
				var name = NormalizeProcessName(_addProcessBox.Text);
				if (!string.IsNullOrEmpty(name) && !_closeList.Items.Contains(name))
				{
					_closeList.Items.Add(name);
					OnConfigurationChanged(); // Уведомляем об изменении
				}
			};
			
			// Обработчик кнопки добавления в список сворачивания
			_addToMinimizeButton.Click += (_, __) =>
			{
				var name = NormalizeProcessName(_addProcessBox.Text);
				if (!string.IsNullOrEmpty(name) && !_minimizeList.Items.Contains(name))
				{
					_minimizeList.Items.Add(name);
					OnConfigurationChanged(); // Уведомляем об изменении
				}
			};
			
			// Обработчик кнопки удаления из списка закрытия
			_removeCloseButton.Click += (_, __) =>
			{
				var toRemove = _closeList.SelectedItem as string;
				if (toRemove != null)
				{
					_closeList.Items.Remove(toRemove);
					OnConfigurationChanged(); // Уведомляем об изменении
				}
			};
			
			// Обработчик кнопки удаления из списка сворачивания
			_removeMinimizeButton.Click += (_, __) =>
			{
				var toRemove = _minimizeList.SelectedItem as string;
				if (toRemove != null)
				{
					_minimizeList.Items.Remove(toRemove);
					OnConfigurationChanged(); // Уведомляем об изменении
				}
			};
			
			// Обработчик изменения состояния чекбоксов в списках
			_closeList.ItemCheck += (_, __) => OnConfigurationChanged();
			_minimizeList.ItemCheck += (_, __) => OnConfigurationChanged();

		// Добавление всех элементов на форму
		Controls.Add(closeLabel);
		Controls.Add(minimizeLabel);
		Controls.Add(_closeList);
		Controls.Add(_minimizeList);
		Controls.Add(_addProcessBox);
		Controls.Add(_addToCloseButton);
		Controls.Add(_addToMinimizeButton);
		Controls.Add(_removeCloseButton);
		Controls.Add(_removeMinimizeButton);
		Controls.Add(_applyButton);
		Controls.Add(_autostartCheckbox);
		Controls.Add(_forceAutostartButton);
		Controls.Add(cursorCoordsLabel);
		Controls.Add(killSelectedButton);
		Controls.Add(minimizeSelectedButton);
	}

	// Метод для вызова события изменения конфигурации
	private void OnConfigurationChanged()
	{
		ConfigurationChanged?.Invoke(this, EventArgs.Empty);
	}

	// Загрузка конфигурации из сохраненных данных
	public void LoadConfiguration(List<TrayAppContext.ProcessConfig> closeProcesses, List<TrayAppContext.ProcessConfig> minimizeProcesses)
	{
		_closeList.Items.Clear();
		_minimizeList.Items.Clear();
		foreach (var proc in closeProcesses)
		{
			int idx = _closeList.Items.Add(proc.Name);
			_closeList.SetItemChecked(idx, proc.ProtectChildren);
		}
		foreach (var proc in minimizeProcesses)
		{
			int idx = _minimizeList.Items.Add(proc.Name);
			_minimizeList.SetItemChecked(idx, proc.ProtectChildren);
		}
	}

	// Обработчик изменения состояния чекбокса автозапуска
	private void AutostartCheckbox_CheckedChanged(object? sender, EventArgs e)
	{
		if (_isUpdatingAutostart) return;
		
		_isUpdatingAutostart = true;
		try
		{
			bool success = SetAutostartEnabled(_autostartCheckbox.Checked);
			if (!success)
			{
				// Если не удалось изменить, возвращаем чекбокс в исходное состояние
				_autostartCheckbox.Checked = !_autostartCheckbox.Checked;
			}
		}
		finally
		{
			_isUpdatingAutostart = false;
		}
	}

	// Обработчик кнопки принудительного включения автозапуска
	private void ForceAutostartButton_Click(object? sender, EventArgs e)
	{
		try
		{
			// Получаем путь к текущему exe
			string exePath = Process.GetCurrentProcess().MainModule?.FileName 
				?? System.IO.Path.Combine(AppContext.BaseDirectory, "TheAlarm.exe");

			// Запускаем reg.exe с правами администратора для добавления в автозапуск
			var psi = new ProcessStartInfo
			{
				FileName = "reg",
				Arguments = $"add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\" /v \"TheAlarm\" /t REG_SZ /d \"\\\"{exePath}\\\"\" /f",
				UseShellExecute = true,
				Verb = "runas", // Запрос прав администратора
				WindowStyle = ProcessWindowStyle.Hidden
			};

			var process = Process.Start(psi);
			process?.WaitForExit();

			// Обновляем состояние чекбокса без вызова события
			_isUpdatingAutostart = true;
			try
			{
				_autostartCheckbox.Checked = IsAutostartEnabled();
			}
			finally
			{
				_isUpdatingAutostart = false;
			}

			MessageBox.Show("Автозапуск успешно включен!", "The Alarm", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Не удалось включить автозапуск: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

		// Проверка, включен ли автозапуск с Windows
		private static bool IsAutostartEnabled()
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
				return key?.GetValue("TheAlarm") != null;
			}
			catch
			{
				return false;
			}
		}

	// Включение/выключение автозапуска с Windows
	private static bool SetAutostartEnabled(bool enabled)
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
			if (key == null) return false;

			if (enabled)
			{
				// Получаем путь к исполняемому файлу для автозапуска
				string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName 
					?? System.IO.Path.Combine(AppContext.BaseDirectory, "TheAlarm.exe");
				key.SetValue("TheAlarm", $"\"{exePath}\"");
			}
			else
			{
				key.DeleteValue("TheAlarm", false);
			}
			return true;
		}
		catch 
		{
			return false;
		}
	}

	// Получение списка процессов для указанного действия (устаревший метод для обратной совместимости)
	public List<string> GetProcessesForAction(ProcessAction action)
	{
		return action == ProcessAction.Close
			? _closeList.Items.Cast<string>().ToList()
			: _minimizeList.Items.Cast<string>().ToList();
	}

	// Получение списка конфигураций процессов для указанного действия
	public List<TrayAppContext.ProcessConfig> GetProcessConfigsForAction(ProcessAction action)
	{
		var list = action == ProcessAction.Close ? _closeList : _minimizeList;
		var result = new List<TrayAppContext.ProcessConfig>();
		
		for (int i = 0; i < list.Items.Count; i++)
		{
			result.Add(new TrayAppContext.ProcessConfig
			{
				Name = list.Items[i] as string ?? string.Empty,
				ProtectChildren = list.GetItemChecked(i)
			});
		}
		
		return result;
	}

		// Нормализация имени процесса (удаление .exe, кавычек и извлечение имени из пути)
		private static string NormalizeProcessName(string input)
		{
			var s = (input ?? string.Empty).Trim().Trim('"');
			if (s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				s = s.Substring(0, s.Length - 4);
			try
			{
				var fileName = System.IO.Path.GetFileNameWithoutExtension(s);
				if (!string.IsNullOrEmpty(fileName)) return fileName;
			}
			catch { }
			return s;
		}

		// Win32 API для работы с окнами процессов
		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
		private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
		
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);
		
		// Структура для хранения координат курсора
		private struct POINT { public int X; public int Y; }

		// Освобождение ресурсов формы
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_closeList.Dispose();
				_minimizeList.Dispose();
				_addProcessBox.Dispose();
				_addToCloseButton.Dispose();
				_addToMinimizeButton.Dispose();
				_removeCloseButton.Dispose();
				_removeMinimizeButton.Dispose();
				_applyButton.Dispose();
				_autostartCheckbox.Dispose();
				_forceAutostartButton.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	// Перечисление действий над процессами
	public enum ProcessAction
	{
		Close,    // Закрыть процесс
		Minimize  // Свернуть окна процесса
	}
}
