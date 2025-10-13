using System;
using System.Drawing;
using System.Windows.Forms;

namespace CURSORTrayApp
{
	// Всплывающее окно для отображения уведомлений будильника
	public class PopupForm : Form
	{
		// Метка для отображения текста сообщения
		private readonly Label _label;
		// Кнопка закрытия окна
		private readonly Button _closeButton;

		// Цвета темной темы (как в Cursor)
		private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
		private static readonly Color DarkControl = Color.FromArgb(45, 45, 45);
		private static readonly Color LightText = Color.FromArgb(220, 220, 220);
		private static readonly Color AccentBlue = Color.FromArgb(0, 122, 204);

		public PopupForm()
		{
			Text = "The Alarm";
			FormBorderStyle = FormBorderStyle.FixedToolWindow;
			StartPosition = FormStartPosition.CenterScreen;
			Width = 320;
			Height = 160;
			
			// Применение темной темы к форме
			BackColor = DarkBackground;
			ForeColor = LightText;

			// Отступ от краев
			const int margin = 15;

			// Метка с текстом уведомления
			_label = new Label 
			{ 
				Left = margin, 
				Top = margin, 
				Width = Width - margin * 3,  // Отступ справа
				Height = 60, 
				AutoSize = false,
				ForeColor = LightText
			};
			
			// Кнопка закрытия окна
			_closeButton = new Button 
			{ 
				Text = "Close", 
				Left = (Width - 90) / 2,  // Центрируем кнопку
				Top = Height - margin - 50,  // Отступ снизу
				Width = 90,
				BackColor = AccentBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			_closeButton.FlatAppearance.BorderColor = AccentBlue;
			_closeButton.Click += (_, __) => Hide();

			Controls.Add(_label);
			Controls.Add(_closeButton);
		}

		// Установка текста сообщения
		public void SetMessage(string message)
		{
			_label.Text = message;
		}
	}
}
