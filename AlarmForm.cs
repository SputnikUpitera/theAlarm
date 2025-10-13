using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace TheAlarm
{
	// Форма для управления будильниками
	public partial class AlarmForm : Form
	{
		// Событие для запроса показа всплывающего окна
		public event EventHandler<string>? RequestPopup;

		// Список всех установленных будильников
		private readonly List<AlarmItem> _alarms = new List<AlarmItem>();
		
		// Элементы управления формы
		private readonly DateTimePicker _timePicker;     // Выбор времени
		private readonly DateTimePicker _datePicker;     // Выбор даты
		private readonly CheckBox _dailyCheckbox;        // Чекбокс ежедневного будильника
		private readonly TextBox _messageBox;            // Ввод сообщения
		private readonly Button _setButton;              // Кнопка установки будильника
		private readonly ListView _alarmListView;        // Список установленных будильников
		private readonly Button _deleteSelectedButton;   // Кнопка удаления выбранного будильника

		// Цвета темной темы (как в Cursor)
		private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
		private static readonly Color DarkControl = Color.FromArgb(45, 45, 45);
		private static readonly Color DarkBorder = Color.FromArgb(60, 60, 60);
		private static readonly Color LightText = Color.FromArgb(220, 220, 220);
		private static readonly Color AccentBlue = Color.FromArgb(0, 122, 204);

		public AlarmForm()
		{
			Text = "The Alarm";
			FormBorderStyle = FormBorderStyle.FixedDialog;
			StartPosition = FormStartPosition.CenterScreen;
			MaximizeBox = false;
			MinimizeBox = false;
			Width = 560;
			Height = 420;
			
			// Применение темной темы к форме
			BackColor = DarkBackground;
			ForeColor = LightText;

			// Отступ от краев окна
			const int margin = 15;

			// Элемент выбора времени
			_timePicker = new DateTimePicker
			{
				Format = DateTimePickerFormat.Time,
				ShowUpDown = true,
				Left = margin,
				Top = margin,
				Width = 120,
				BackColor = DarkControl,
				ForeColor = LightText
			};
			
			// Элемент выбора даты
			_datePicker = new DateTimePicker
			{
				Format = DateTimePickerFormat.Short,
				Left = margin + 130, // Смещение относительно времени
				Top = margin,
				Width = 120,
				BackColor = DarkControl,
				ForeColor = LightText
			};
			_datePicker.Value = DateTime.Now; // Устанавливаем текущую дату

			// Чекбокс ежедневного будильника
			_dailyCheckbox = new CheckBox
			{
				Text = "Daily",
				Left = margin + 260, // Смещение относительно времени
				Top = margin,
				Width = 100,
				BackColor = DarkControl,
				ForeColor = LightText
			};
			_dailyCheckbox.Checked = true; // По умолчанию включен

			// Поле ввода сообщения будильника
			_messageBox = new TextBox
			{
				Left = margin,
				Top = margin + 40,
				Width = Width - margin * 3,  // Отступ справа
				BackColor = DarkControl,
				ForeColor = LightText,
				BorderStyle = BorderStyle.FixedSingle
			};
			
			// Кнопка установки будильника
			_setButton = new Button
			{
				Text = "Set Alarm",
				Left = margin,
				Top = margin + 80,
				Width = 120,
				BackColor = AccentBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			_setButton.FlatAppearance.BorderColor = AccentBlue;
			
			// Обработчик нажатия кнопки "Set Alarm"
			_setButton.Click += (_, __) =>
			{
				var now = DateTime.Now;
				var t = _timePicker.Value;
				var d = _datePicker.Value;
				
				// Создаем дату/время будильника на основе выбранной даты и времени
				var candidate = new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute, t.Second);
				
				// Если время уже прошло сегодня, переносим на завтра
				if (candidate <= now) candidate = candidate.AddDays(1);
				
				var msg = _messageBox.Text ?? string.Empty;
				var isDaily = _dailyCheckbox.Checked;
				
				// Добавляем будильник в список
				_alarms.Add(new AlarmItem { 
					TimeUtc = candidate.ToUniversalTime(), 
					Message = msg,
					IsDaily = isDaily
				});
				RefreshAlarmList();
				SaveAlarms();
			};

			Controls.Add(_timePicker);
			Controls.Add(_datePicker);
			Controls.Add(_dailyCheckbox);
			Controls.Add(_messageBox);
			Controls.Add(_setButton);

			// Список установленных будильников (таблица с двумя колонками)
			_alarmListView = new ListView
			{
				Left = margin,
				Top = margin + 120,
				Width = Width - margin * 3,  // Отступ справа
				Height = 200,
				View = View.Details,
				FullRowSelect = true,
				HideSelection = false,
				BackColor = DarkControl,
				ForeColor = LightText,
				BorderStyle = BorderStyle.FixedSingle
			};
			_alarmListView.Columns.Add("Time", 180);
			_alarmListView.Columns.Add("Message", Width - margin * 3 - 260);  // Динамическая ширина
			_alarmListView.Columns.Add("Type", 80);  // Колонка для типа будильника
			
			// Кнопка удаления выбранного будильника
			_deleteSelectedButton = new Button
			{
				Text = "Delete Selected",
				Left = Width - margin - 155,  // Отступ справа
				Top = Height - margin - 60,   // Отступ снизу
				Width = 140,
				BackColor = DarkControl,
				ForeColor = LightText,
				FlatStyle = FlatStyle.Flat
			};
			_deleteSelectedButton.FlatAppearance.BorderColor = DarkBorder;
			
			// Обработчик удаления будильника
			_deleteSelectedButton.Click += (_, __) =>
			{
				if (_alarmListView.SelectedItems.Count == 0) return;
				
				foreach (ListViewItem item in _alarmListView.SelectedItems)
				{
					if (item.Tag is AlarmItem ai)
					{
						_alarms.Remove(ai);
					}
				}
				RefreshAlarmList();
				SaveAlarms();
			};
			
			Controls.Add(_alarmListView);
			Controls.Add(_deleteSelectedButton);
		}

		// Получение и удаление всех сработавших будильников
		public List<string> ConsumeDueAlarms()
		{
			var nowUtc = DateTime.UtcNow;
			var due = new List<AlarmItem>();
			var dailyAlarms = new List<AlarmItem>();
			
			// Находим все будильники, чье время уже наступило
			foreach (var a in _alarms)
			{
				if (nowUtc >= a.TimeUtc)
				{
					if (a.IsDaily)
					{
						// Для ежедневных будильников переносим на следующий день
						dailyAlarms.Add(a);
					}
					else
					{
						// Для одноразовых будильников добавляем в список для удаления
						due.Add(a);
					}
				}
			}
			
			if (due.Count == 0 && dailyAlarms.Count == 0) return new List<string>();
			
			// Удаляем сработавшие одноразовые будильники из списка
			foreach (var a in due) _alarms.Remove(a);
			
			// Переносим ежедневные будильники на следующий день
			foreach (var a in dailyAlarms)
			{
				// Удаляем старый будильник
				_alarms.Remove(a);
				// Добавляем новый на следующий день
				_alarms.Add(new AlarmItem { 
					TimeUtc = a.TimeUtc.AddDays(1), 
					Message = a.Message,
					IsDaily = true
				});
			}
			
			RefreshAlarmList();
			
			// Сохраняем обновленный список будильников
			SaveAlarms();
			
			// Возвращаем список сообщений
			var messages = new List<string>();
			foreach (var a in due) messages.Add(a.Message);
			foreach (var a in dailyAlarms) messages.Add(a.Message);
			return messages;
		}

		// Загрузка списка будильников из файла
		public void LoadAlarms(List<AlarmItem> alarms)
		{
			_alarms.Clear();
			_alarms.AddRange(alarms);
			RefreshAlarmList();
		}

		// Получение списка будильников для сохранения
		public List<AlarmItem> GetAlarms()
		{
			return new List<AlarmItem>(_alarms);
		}

		// Сохранение списка будильников
		private void SaveAlarms()
		{
			try
			{
				// Сохранение происходит автоматически при изменении списка приложений
			}
			catch { }
		}

		// Обновление отображения списка будильников
		private void RefreshAlarmList()
		{
			_alarmListView.Items.Clear();
			
			foreach (var a in _alarms)
			{
				// Преобразуем время из UTC в локальное для отображения
				var localTime = a.TimeUtc.ToLocalTime();
				var item = new ListViewItem(new[] { 
					localTime.ToString("yyyy-MM-dd HH:mm:ss"), 
					a.Message,
					a.IsDaily ? "Daily" : "Once"
				}) { Tag = a };
				_alarmListView.Items.Add(item);
			}
			
			// Показываем список только если есть будильники
			_alarmListView.Visible = _alarms.Count > 0;
			_deleteSelectedButton.Visible = _alarms.Count > 0;
		}

		// Класс для хранения информации о будильнике
		public class AlarmItem
		{
			public DateTime TimeUtc { get; set; }  // Время срабатывания (UTC)
			public string Message { get; set; } = string.Empty;  // Сообщение будильника
			public bool IsDaily { get; set; } = false;  // Флаг ежедневного будильника
		}

		// Освобождение ресурсов формы
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_timePicker.Dispose();
				_datePicker.Dispose();
				_dailyCheckbox.Dispose();
				_messageBox.Dispose();
				_setButton.Dispose();
				_alarmListView.Dispose();
				_deleteSelectedButton.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
