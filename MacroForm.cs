using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TheAlarm
{
	public sealed class MacroForm : Form
	{
		private static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
		private static readonly Color DarkSurface = Color.FromArgb(40, 40, 40);
		private static readonly Color DarkControl = Color.FromArgb(48, 48, 48);
		private static readonly Color DarkBorder = Color.FromArgb(65, 65, 65);
		private static readonly Color LightText = Color.FromArgb(225, 225, 225);
		private static readonly Color MutedText = Color.FromArgb(170, 170, 170);
		private static readonly Color AccentBlue = Color.FromArgb(0, 122, 204);
		private static readonly Color DangerRed = Color.FromArgb(215, 85, 85);

		private readonly MacroExecutionService _executionService;
		private readonly LiveScrollPanel _cardsHost;
		private readonly Panel _cardsPanel;
		private readonly Button _addButton;
		private readonly Panel _bottomPanel;
		private readonly Button _bottomAddButton;
		private readonly Panel _emptyStatePanel;
		private readonly Label _emptyStateLabel;
		private readonly Button _emptyStateAddButton;
		private readonly System.Windows.Forms.Timer _changeDebounceTimer;
		private readonly List<MacroCardControl> _cards = new List<MacroCardControl>();
		private bool _isLoading;

		public MacroForm(MacroExecutionService executionService)
		{
			_executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));

			Text = "The Alarm - Macros";
			StartPosition = FormStartPosition.CenterScreen;
			MinimumSize = new Size(760, 560);
			ClientSize = new Size(900, 720);
			BackColor = DarkBackground;
			ForeColor = LightText;

			var headerPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 60,
				Padding = new Padding(16, 12, 16, 12),
				BackColor = DarkBackground
			};

			var titleLabel = new Label
			{
				AutoSize = true,
				Text = "User Macros",
				Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
				ForeColor = LightText,
				Location = new Point(0, 2)
			};

			var subtitleLabel = new Label
			{
				AutoSize = true,
				Text = "Each active macro can listen globally while the app stays in the tray.",
				Font = new Font("Segoe UI", 9f),
				ForeColor = MutedText,
				Location = new Point(0, 28)
			};

			_addButton = new Button
			{
				Text = "Add Macro",
				Width = 120,
				Height = 34,
				Anchor = AnchorStyles.Top | AnchorStyles.Right,
				BackColor = AccentBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Location = new Point(ClientSize.Width - 136, 12)
			};
			_addButton.FlatAppearance.BorderColor = AccentBlue;
			_addButton.Click += (_, __) => AddCard(null, scheduleChange: true);

			headerPanel.Controls.Add(titleLabel);
			headerPanel.Controls.Add(subtitleLabel);
			headerPanel.Controls.Add(_addButton);

			_emptyStatePanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 128,
				Padding = new Padding(20, 16, 20, 16),
				BackColor = DarkBackground
			};

			_emptyStateLabel = new Label
			{
				Dock = DockStyle.Top,
				Height = 52,
				Text = "Create your first macro below. Active macros keep working globally while the app is running in the tray.",
				ForeColor = MutedText,
				BackColor = DarkBackground,
				TextAlign = ContentAlignment.MiddleLeft
			};

			_emptyStateAddButton = new Button
			{
				Text = "Add First Macro",
				Dock = DockStyle.Top,
				Height = 38,
				Width = 180,
				BackColor = AccentBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat,
				Margin = new Padding(0, 12, 0, 0)
			};
			_emptyStateAddButton.FlatAppearance.BorderColor = AccentBlue;
			_emptyStateAddButton.Click += (_, __) => AddCard(null, scheduleChange: true);

			_emptyStatePanel.Controls.Add(_emptyStateAddButton);
			_emptyStatePanel.Controls.Add(_emptyStateLabel);

			_cardsHost = new LiveScrollPanel
			{
				Dock = DockStyle.Fill,
				AutoScroll = true,
				BackColor = DarkBackground
			};

			_cardsPanel = new Panel
			{
				Location = new Point(16, 8),
				BackColor = DarkBackground
			};
			_cardsHost.Controls.Add(_cardsPanel);

			_bottomPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 62,
				Padding = new Padding(16, 8, 16, 12),
				BackColor = DarkBackground
			};

			_bottomAddButton = new Button
			{
				Text = "Add New Macro",
				Dock = DockStyle.Fill,
				Height = 38,
				BackColor = AccentBlue,
				ForeColor = Color.White,
				FlatStyle = FlatStyle.Flat
			};
			_bottomAddButton.FlatAppearance.BorderColor = AccentBlue;
			_bottomAddButton.Click += (_, __) => AddCard(null, scheduleChange: true);

			_bottomPanel.Controls.Add(_bottomAddButton);

			Controls.Add(_bottomPanel);
			Controls.Add(_cardsHost);
			Controls.Add(_emptyStatePanel);
			Controls.Add(headerPanel);

			_changeDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
			_changeDebounceTimer.Tick += (_, __) =>
			{
				_changeDebounceTimer.Stop();
				if (!_isLoading)
				{
					MacrosChanged?.Invoke(this, EventArgs.Empty);
				}
			};

			Resize += (_, __) =>
			{
				_addButton.Left = ClientSize.Width - _addButton.Width - 16;
				LayoutCards();
			};
		}

		public event EventHandler? MacrosChanged;

		public void LoadMacros(IEnumerable<MacroDefinition> definitions)
		{
			_isLoading = true;
			try
			{
				_cardsPanel.SuspendLayout();
				foreach (var card in _cards.ToList())
				{
					RemoveCard(card);
				}

				var loadedDefinitions = definitions?.ToList() ?? new List<MacroDefinition>();

				foreach (var definition in loadedDefinitions)
				{
					AddCard(definition, scheduleChange: false);
				}
			}
			finally
			{
				_cardsPanel.ResumeLayout(true);
				_isLoading = false;
				LayoutCards();
				UpdateEmptyState();
			}
		}

		public List<MacroDefinition> GetMacros()
		{
			return _cards
				.Select(card => card.GetDefinition().Normalize())
				.ToList();
		}

		public void SetRegistrationStatuses(IReadOnlyDictionary<string, string?> statuses)
		{
			foreach (var card in _cards)
			{
				statuses.TryGetValue(card.MacroId, out var status);
				card.SetStatus(status);
			}
		}

		private void AddCard(MacroDefinition? definition, bool scheduleChange)
		{
			var normalizedDefinition = (definition ?? new MacroDefinition
			{
				Id = Guid.NewGuid().ToString("N"),
				IsActive = false,
				RunnerType = MacroRunnerTypes.Cmd
			}).Clone().Normalize();

			var card = new MacroCardControl(normalizedDefinition);
			card.DefinitionChanged += OnCardDefinitionChanged;
			card.DeleteRequested += (_, __) =>
			{
				RemoveCard(card);
				ScheduleChange();
			};
			card.RunRequested += (_, __) => RunCard(card);

			_cards.Add(card);
			_cardsPanel.Controls.Add(card);
			LayoutCards();
			UpdateEmptyState();

			if (scheduleChange)
			{
				ScheduleChange();
			}
		}

		private void RemoveCard(MacroCardControl card)
		{
			card.DefinitionChanged -= OnCardDefinitionChanged;
			card.Dispose();
			_cards.Remove(card);
			_cardsPanel.Controls.Remove(card);
			LayoutCards();
			UpdateEmptyState();
		}

		private void OnCardDefinitionChanged(object? sender, EventArgs e)
		{
			ScheduleChange();
		}

		private void RunCard(MacroCardControl card)
		{
			var definition = card.GetDefinition().Normalize();
			if (!_executionService.TryExecute(definition, out var errorMessage))
			{
				MessageBox.Show(
					errorMessage ?? "Failed to start macro.",
					"Macro Execution",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
				return;
			}

			MessageBox.Show(
				"Macro process started.",
				"Macro Execution",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		private void ScheduleChange()
		{
			if (_isLoading)
			{
				return;
			}

			_changeDebounceTimer.Stop();
			_changeDebounceTimer.Start();
		}

		private void LayoutCards()
		{
			var targetWidth = Math.Max(680, _cardsHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 32);
			var y = 0;
			foreach (var card in _cards)
			{
				card.Left = 0;
				card.Top = y;
				card.Width = targetWidth;
				y += card.Height + card.Margin.Bottom;
			}

			_cardsPanel.Width = targetWidth;
			_cardsPanel.Height = Math.Max(0, y);
			_cardsHost.AutoScrollMinSize = new Size(targetWidth + 16, Math.Max(0, y + 8));
		}

		private void UpdateEmptyState()
		{
			_emptyStatePanel.Visible = _cards.Count == 0;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_changeDebounceTimer.Dispose();
				foreach (var card in _cards.ToList())
				{
					RemoveCard(card);
				}
				_cardsPanel.Dispose();
				_cardsHost.Dispose();
				_addButton.Dispose();
				_bottomAddButton.Dispose();
				_bottomPanel.Dispose();
				_emptyStateAddButton.Dispose();
				_emptyStateLabel.Dispose();
				_emptyStatePanel.Dispose();
			}

			base.Dispose(disposing);
		}

		private sealed class MacroCardControl : Panel
		{
			private readonly CheckBox _activeCheckBox;
			private readonly HotkeyTextBox _hotkeyBox;
			private readonly ComboBox _runnerComboBox;
			private readonly Button _testButton;
			private readonly Button _deleteButton;
			private readonly TextBox _scriptBox;
			private readonly Label _statusLabel;
			private readonly Label _runnerLabel;
			private readonly Label _hotkeyLabel;

			public MacroCardControl(MacroDefinition definition)
			{
				MacroId = definition.Id;

				Height = 270;
				Margin = new Padding(0, 0, 0, 16);
				Padding = new Padding(16);
				BackColor = DarkSurface;
				BorderStyle = BorderStyle.FixedSingle;

				_activeCheckBox = new CheckBox
				{
					Text = "Active",
					AutoSize = true,
					Location = new Point(12, 14),
					ForeColor = LightText,
					BackColor = DarkSurface
				};

				_hotkeyLabel = new Label
				{
					Text = "Hotkey",
					AutoSize = true,
					Location = new Point(110, 16),
					ForeColor = MutedText,
					BackColor = DarkSurface
				};

				_hotkeyBox = new HotkeyTextBox
				{
					Location = new Point(160, 12),
					Width = 170
				};

				_runnerLabel = new Label
				{
					Text = "Runner",
					AutoSize = true,
					Location = new Point(350, 16),
					ForeColor = MutedText,
					BackColor = DarkSurface
				};

				_runnerComboBox = new ComboBox
				{
					Location = new Point(405, 12),
					Width = 120,
					DropDownStyle = ComboBoxStyle.DropDownList,
					BackColor = DarkControl,
					ForeColor = LightText,
					FlatStyle = FlatStyle.Flat
				};
				_runnerComboBox.Items.Add(MacroRunnerTypes.Cmd);
				_runnerComboBox.Items.Add(MacroRunnerTypes.PowerShell);

				_testButton = new Button
				{
					Text = "Test",
					Location = new Point(540, 10),
					Width = 80,
					Height = 30,
					BackColor = DarkControl,
					ForeColor = LightText,
					FlatStyle = FlatStyle.Flat
				};
				_testButton.FlatAppearance.BorderColor = DarkBorder;

				_deleteButton = new Button
				{
					Text = "Delete",
					Location = new Point(632, 10),
					Width = 90,
					Height = 30,
					BackColor = DarkControl,
					ForeColor = LightText,
					FlatStyle = FlatStyle.Flat
				};
				_deleteButton.FlatAppearance.BorderColor = DangerRed;

				_scriptBox = new TextBox
				{
					Location = new Point(12, 56),
					Multiline = true,
					ScrollBars = ScrollBars.Vertical,
					AcceptsReturn = true,
					AcceptsTab = true,
					BorderStyle = BorderStyle.FixedSingle,
					BackColor = DarkControl,
					ForeColor = LightText,
					Font = new Font("Consolas", 10f),
					Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
					Width = 710,
					Height = 160
				};

				_statusLabel = new Label
				{
					Location = new Point(12, 225),
					Width = 710,
					Height = 24,
					ForeColor = DangerRed,
					BackColor = DarkSurface
				};

				Controls.Add(_activeCheckBox);
				Controls.Add(_hotkeyLabel);
				Controls.Add(_hotkeyBox);
				Controls.Add(_runnerLabel);
				Controls.Add(_runnerComboBox);
				Controls.Add(_testButton);
				Controls.Add(_deleteButton);
				Controls.Add(_scriptBox);
				Controls.Add(_statusLabel);

				_activeCheckBox.CheckedChanged += (_, __) => DefinitionChanged?.Invoke(this, EventArgs.Empty);
				_hotkeyBox.ValueChanged += (_, __) => DefinitionChanged?.Invoke(this, EventArgs.Empty);
				_runnerComboBox.SelectedIndexChanged += (_, __) => DefinitionChanged?.Invoke(this, EventArgs.Empty);
				_scriptBox.TextChanged += (_, __) => DefinitionChanged?.Invoke(this, EventArgs.Empty);
				_testButton.Click += (_, __) => RunRequested?.Invoke(this, EventArgs.Empty);
				_deleteButton.Click += (_, __) => DeleteRequested?.Invoke(this, EventArgs.Empty);

				ApplyDefinition(definition);
			}

			public string MacroId { get; }

			public event EventHandler? DefinitionChanged;
			public event EventHandler? DeleteRequested;
			public event EventHandler? RunRequested;

			public MacroDefinition GetDefinition()
			{
				return new MacroDefinition
				{
					Id = MacroId,
					IsActive = _activeCheckBox.Checked,
					Hotkey = _hotkeyBox.Value.HasValue ? HotkeyText.ToMacroHotkey(_hotkeyBox.Value.Value) : new MacroHotkey(),
					RunnerType = MacroRunnerTypes.Normalize(_runnerComboBox.SelectedItem as string),
					ScriptText = _scriptBox.Text ?? string.Empty
				};
			}

			public void SetStatus(string? message)
			{
				_statusLabel.Text = message ?? string.Empty;
				_statusLabel.Visible = !string.IsNullOrWhiteSpace(message);
			}

			protected override void OnResize(EventArgs eventargs)
			{
				base.OnResize(eventargs);

				if (_scriptBox == null
					|| _statusLabel == null
					|| _deleteButton == null
					|| _testButton == null
					|| _runnerComboBox == null
					|| _runnerLabel == null)
				{
					return;
				}

				var innerWidth = ClientSize.Width - Padding.Left - Padding.Right;
				_scriptBox.Width = Math.Max(400, innerWidth - 8);
				_statusLabel.Width = Math.Max(400, innerWidth - 8);
				_deleteButton.Left = ClientSize.Width - Padding.Right - _deleteButton.Width - 4;
				_testButton.Left = _deleteButton.Left - _testButton.Width - 12;
				_runnerComboBox.Left = Math.Min(_testButton.Left - _runnerComboBox.Width - 16, 410);
				_runnerLabel.Left = _runnerComboBox.Left - 58;
			}

			private void ApplyDefinition(MacroDefinition definition)
			{
				_activeCheckBox.Checked = definition.IsActive;
				_hotkeyBox.SetValue(HotkeyText.TryParse(definition.Hotkey, out var gesture) ? gesture : (HotkeyGesture?)null);
				_runnerComboBox.SelectedItem = MacroRunnerTypes.Normalize(definition.RunnerType);
				if (_runnerComboBox.SelectedIndex < 0)
				{
					_runnerComboBox.SelectedItem = MacroRunnerTypes.Cmd;
				}
				_scriptBox.Text = definition.ScriptText ?? string.Empty;
				SetStatus(null);
			}
		}

		private sealed class HotkeyTextBox : TextBox
		{
			private HotkeyGesture? _value;

			public HotkeyTextBox()
			{
				ReadOnly = true;
				BackColor = DarkControl;
				ForeColor = LightText;
				BorderStyle = BorderStyle.FixedSingle;
			}

			public HotkeyGesture? Value => _value;

			public event EventHandler? ValueChanged;

			public void SetValue(HotkeyGesture? gesture)
			{
				_value = gesture;
				Text = gesture.HasValue ? HotkeyText.Format(gesture.Value) : string.Empty;
			}

			protected override bool IsInputKey(Keys keyData)
			{
				return true;
			}

			protected override void OnKeyDown(KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Tab)
				{
					base.OnKeyDown(e);
					return;
				}

				e.SuppressKeyPress = true;
				if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
				{
					SetValue(null);
					ValueChanged?.Invoke(this, EventArgs.Empty);
					return;
				}

				if (!HotkeyText.TryCreateGestureFromKeyEvent(e, out var gesture))
				{
					return;
				}

				SetValue(gesture);
				ValueChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		private sealed class LiveScrollPanel : Panel
		{
			private const int WM_VSCROLL = 0x0115;
			private const int WM_MOUSEWHEEL = 0x020A;
			private const int WM_HSCROLL = 0x0114;
			private const int SB_THUMBPOSITION = 4;
			private const int SB_THUMBTRACK = 5;
			private const int SIF_TRACKPOS = 0x0010;
			private const int SIF_POS = 0x0004;
			private const int SB_VERT = 1;
			private const int SB_HORZ = 0;

			public LiveScrollPanel()
			{
				DoubleBuffered = true;
				ResizeRedraw = true;
			}

			protected override void WndProc(ref Message m)
			{
				base.WndProc(ref m);

				if (m.Msg == WM_VSCROLL)
				{
					ApplyThumbTrackPosition(SB_VERT, GetScrollRequestCode(m.WParam));
				}
				else if (m.Msg == WM_HSCROLL)
				{
					ApplyThumbTrackPosition(SB_HORZ, GetScrollRequestCode(m.WParam));
				}

				if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
				{
					Invalidate(true);
					Update();
				}
			}

			private void ApplyThumbTrackPosition(int bar, int requestCode)
			{
				if (requestCode != SB_THUMBTRACK && requestCode != SB_THUMBPOSITION)
				{
					return;
				}

				var scrollInfo = new SCROLLINFO
				{
					cbSize = (uint)Marshal.SizeOf<SCROLLINFO>(),
					fMask = SIF_TRACKPOS | SIF_POS
				};

				if (!GetScrollInfo(Handle, bar, ref scrollInfo))
				{
					return;
				}

				if (bar == SB_VERT)
				{
					var target = requestCode == SB_THUMBTRACK ? scrollInfo.nTrackPos : scrollInfo.nPos;
					AutoScrollPosition = new Point(-AutoScrollPosition.X, target);
				}
				else
				{
					var target = requestCode == SB_THUMBTRACK ? scrollInfo.nTrackPos : scrollInfo.nPos;
					AutoScrollPosition = new Point(target, -AutoScrollPosition.Y);
				}
			}

			private static int GetScrollRequestCode(IntPtr wParam)
			{
				return unchecked((short)(wParam.ToInt64() & 0xFFFF));
			}

			[DllImport("user32.dll")]
			private static extern bool GetScrollInfo(IntPtr hwnd, int nBar, ref SCROLLINFO lpsi);

			[StructLayout(LayoutKind.Sequential)]
			private struct SCROLLINFO
			{
				public uint cbSize;
				public uint fMask;
				public int nMin;
				public int nMax;
				public uint nPage;
				public int nPos;
				public int nTrackPos;
			}
		}
	}
}
