using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TheAlarm
{
	public class TrayAppContext : ApplicationContext
	{
		private readonly NotifyIcon _notifyIcon;
		private readonly AlarmForm _alarmForm;
		private readonly MacroForm _macroForm;
		private readonly PopupForm _popupForm;
		private readonly System.Windows.Forms.Timer _alarmTimer;
		private readonly System.Windows.Forms.Timer _cornerCheckTimer;
		private readonly GlobalHotkeyWindow _hotkeyWindow;
		private readonly HotkeyManager _hotkeyManager;
		private readonly MacroExecutionService _macroExecutionService;
		private readonly AppStateRepository _stateRepository;

		private AppState _appState = new AppState();
		private bool _isLoadingState;
		private int _activeCorner = -1;
		private bool _disposing;

		public TrayAppContext()
		{
			_macroExecutionService = new MacroExecutionService();
			_stateRepository = new AppStateRepository();

			_alarmForm = new AlarmForm();
			_macroForm = new MacroForm(_macroExecutionService);
			_popupForm = new PopupForm();
			foreach (var form in new Form[] { _alarmForm, _macroForm, _popupForm }) UiTheme.Apply(form);

			var trayIcon = LoadTrayIcon();
			_notifyIcon = new NotifyIcon
			{
				Text = "The Alarm",
				Icon = trayIcon,
				Visible = true,
				ContextMenuStrip = BuildContextMenu()
			};
			_notifyIcon.MouseClick += NotifyIcon_MouseClick;

			_alarmForm.FormClosing += AnyForm_FormClosingToTray;
			_macroForm.FormClosing += AnyForm_FormClosingToTray;

			_alarmForm.AlarmsChanged += (_, __) => SaveState();
			_macroForm.MacrosChanged += (_, __) => SaveStateAndRefreshMacroHotkeys();
			_macroForm.RunRequested += macro => RunMacroActions(macro);

			LoadState();

			_hotkeyWindow = new GlobalHotkeyWindow();
			_hotkeyManager = new HotkeyManager(_hotkeyWindow);
			_alarmForm.VisibleChanged += (_, _) => RefreshWindowHotkey();
			_macroForm.VisibleChanged += (_, _) => RefreshWindowHotkey();
			_macroForm.EditorVisibilityChanged += (_, _) => RefreshMacroHotkeys();
			RefreshWindowHotkey();
			RefreshMacroHotkeys();

			_alarmTimer = new System.Windows.Forms.Timer { Interval = 1000 };
			_alarmTimer.Tick += AlarmTimer_Tick;
			_alarmTimer.Start();

			_cornerCheckTimer = new System.Windows.Forms.Timer { Interval = 200 };
			_cornerCheckTimer.Tick += CornerCheckTimer_Tick;
			_cornerCheckTimer.Start();

		}

		public class ProcessConfig
		{
			public string Name { get; set; } = string.Empty;
			public bool ProtectChildren { get; set; }
		}

		private void LoadState()
		{
			_isLoadingState = true;
			try
			{
				var loadResult = _stateRepository.Load();
				_appState = loadResult.State.Normalize();

				_alarmForm.LoadAlarms(_appState.Alarms);
				_alarmForm.AlarmSoundPath = _appState.AlarmSoundPath;
				_macroForm.LoadMacros(_appState.Macros.Definitions);

				if (!string.IsNullOrWhiteSpace(loadResult.WarningMessage))
				{
					MessageBox.Show(
						loadResult.WarningMessage,
						"The Alarm",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
				}
			}
			finally
			{
				_isLoadingState = false;
			}
		}

		private void SaveState()
		{
			if (_isLoadingState)
			{
				return;
			}

			var macros = _macroForm.GetMacros();
			var newState = new AppState
			{
				SchemaVersion = AppState.CurrentSchemaVersion,
				ProcessRules = macros.FirstOrDefault(m => m.IsCornerMacro)?.Actions ?? new ProcessRulesState(),
				Alarms = _alarmForm.GetAlarms(),
				AlarmSoundPath = _alarmForm.AlarmSoundPath,
				Macros = new MacroState
				{
					Definitions = macros
				},
				FutureData = _appState.FutureData
			}.Normalize();

			if (_stateRepository.Save(newState, out var errorMessage))
			{
				_appState = newState;
				return;
			}

			MessageBox.Show(
				errorMessage ?? "Failed to save configuration.",
				"The Alarm",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}

		private void SaveStateAndRefreshMacroHotkeys()
		{
			RefreshMacroHotkeys();
			SaveState();
		}

		private void RefreshMacroHotkeys()
		{
			if (_disposing) return;
			var macros = _macroForm.GetMacros();
			if (_macroForm.IsEditing) macros.Clear();
			var statuses = _hotkeyManager.SetMacroBindings(macros.Where(m => !m.IsCornerMacro).Select(CreateMacroBinding).ToList());
			_macroForm.SetRegistrationStatuses(statuses);
		}

		private MacroHotkeyBinding CreateMacroBinding(MacroDefinition definition)
		{
			var snapshot = definition.Clone().Normalize();
			return new MacroHotkeyBinding
			{
				MacroId = snapshot.Id,
				IsActive = snapshot.IsActive,
				Hotkey = snapshot.Hotkey.Clone(),
				ScriptText = snapshot.ScriptText,
				Handler = () => ExecuteMacro(snapshot.Id)
			};
		}

		private void ExecuteMacro(string macroId)
		{
			var macro = _macroForm.GetMacro(macroId);

			if (macro == null)
			{
				AppLog.Error($"Macro '{macroId}' was requested by hotkey but no longer exists.");
				return;
			}
			if (!macro.IsActive) return;

			RunMacroActions(macro);
		}

		private void RunMacroActions(MacroDefinition macro, ProcessAction? cornerAction = null)
		{
			if (cornerAction != ProcessAction.Minimize) PerformProcessAction(ProcessAction.Close, ToProcessConfigs(macro.Actions.CloseProcesses));
			if (cornerAction != ProcessAction.Close) PerformProcessAction(ProcessAction.Minimize, ToProcessConfigs(macro.Actions.MinimizeProcesses));
			if (macro.ScriptEnabled == true && !_macroExecutionService.TryExecute(macro, out var errorMessage))
			{
				MessageBox.Show(
					errorMessage ?? "Failed to start macro.",
					"Macro Execution",
					MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
		}

		private static List<ProcessConfig> ToProcessConfigs(List<ProcessRule> rules)
		{
			var configs = new List<ProcessConfig>();
			foreach (var rule in rules ?? new List<ProcessRule>())
			{
				if (rule == null)
				{
					continue;
				}

				configs.Add(new ProcessConfig
				{
					Name = rule.Name,
					ProtectChildren = rule.ProtectChildren
				});
			}

			return configs;
		}

		private static List<ProcessRule> ToProcessRules(List<ProcessConfig> configs)
		{
			var rules = new List<ProcessRule>();
			foreach (var config in configs ?? new List<ProcessConfig>())
			{
				if (config == null)
				{
					continue;
				}

				rules.Add(new ProcessRule
				{
					Name = config.Name,
					ProtectChildren = config.ProtectChildren
				}.Normalize());
			}

			return rules;
		}

		private static Icon LoadTrayIcon()
		{
			Icon trayIcon = SystemIcons.Application;
			try
			{
				var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.jpg");
				if (File.Exists(iconPath))
				{
					using var bmp = new Bitmap(iconPath);
					var hIcon = bmp.GetHicon();
					trayIcon = (Icon)Icon.FromHandle(hIcon).Clone();
					DestroyIcon(hIcon);
				}
				else
				{
					trayIcon = CreateDefaultIcon();
				}
			}
			catch
			{
				trayIcon = CreateDefaultIcon();
			}

			return trayIcon;
		}

		private static Icon CreateDefaultIcon()
		{
			try
			{
				using var bmp = new Bitmap(32, 32);
				using (var g = Graphics.FromImage(bmp))
				{
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
						new Rectangle(0, 0, 32, 32),
						Color.FromArgb(0, 90, 180),
						Color.FromArgb(0, 50, 120),
						45f);
					using var pen = new Pen(Color.White, 2);
					using var centerBrush = new SolidBrush(Color.White);

					g.FillEllipse(brush, 0, 0, 31, 31);
					g.DrawEllipse(pen, 2, 2, 27, 27);
					g.DrawLine(pen, 16, 16, 11, 8);
					g.DrawLine(pen, 16, 16, 23, 10);
					g.FillEllipse(centerBrush, 14, 14, 4, 4);
				}

				var hIcon = bmp.GetHicon();
				var cloned = (Icon)Icon.FromHandle(hIcon).Clone();
				DestroyIcon(hIcon);
				return cloned;
			}
			catch
			{
				return SystemIcons.Application;
			}
		}

		private ContextMenuStrip BuildContextMenu()
		{
			var menu = new ContextMenuStrip { Font = _alarmForm.Font, BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, ShowImageMargin = false, Padding = new Padding(3), Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()) };

			var openAlarm = new ToolStripMenuItem("Открыть будильник") { Padding = new Padding(6, 3, 6, 3), TextAlign = ContentAlignment.MiddleLeft };
			openAlarm.Click += (_, __) => ShowAlarm();

			var exit = new ToolStripMenuItem("Выйти") { Padding = new Padding(6, 3, 6, 3), TextAlign = ContentAlignment.MiddleLeft };
			exit.Click += (_, __) => ExitApplication();

			menu.Items.Add(openAlarm);
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(exit);
			return menu;
		}

		private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ShowAlarm();
			}
		}

		private void ShowAlarm()
		{
			if (_macroForm.IsEditing) { _macroForm.Activate(); return; }
			_macroForm.Hide();
			_alarmForm.ShowInTaskbar = true;
			_alarmForm.WindowState = FormWindowState.Normal;
			_alarmForm.Show();
			_alarmForm.BringToFront();
			_alarmForm.Activate();
		}

		private void ShowMacroWindow()
		{
			_alarmForm.Hide();
			_macroForm.ShowInTaskbar = true;
			_macroForm.WindowState = FormWindowState.Normal;
			_macroForm.Show();
			_macroForm.BringToFront();
			_macroForm.Activate();
		}

		private void RefreshWindowHotkey()
		{
			if (_disposing) return;
			_hotkeyManager.SetInternalBindings(_alarmForm.Visible || _macroForm.Visible
				? new[] { new HotkeyActionBinding { Id = "macro-window", Gesture = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.F1), Handler = ToggleMacroWindow } }
				: Array.Empty<HotkeyActionBinding>());
		}

		private void ToggleMacroWindow()
		{
			if (_macroForm.IsEditing) return;
			if (!_macroForm.Visible && (!_alarmForm.Visible || _alarmForm.WindowState == FormWindowState.Minimized)) return;
			if (_macroForm.Visible)
			{
				ShowAlarm();
				return;
			}

			ShowMacroWindow();
		}

		private void AlarmTimer_Tick(object? sender, EventArgs e)
		{
			var due = _alarmForm.ConsumeDueAlarms();
			if (due.Count == 0) return;
			AlarmAudio.Play(_alarmForm.AlarmSoundPath);
			_popupForm.SetMessage(string.Join(Environment.NewLine + Environment.NewLine, due));
			_popupForm.Show();
			_popupForm.Activate();
		}

		private void CornerCheckTimer_Tick(object? sender, EventArgs e)
		{
			if (GetCursorPos(out var point)) CheckCornersAndAct(point.X, point.Y);
		}

		private void CheckCornersAndAct(int x, int y)
		{
			var bounds = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
			int corner = -1;
			if (bounds.Contains(x, y))
			{
				bool left = x < bounds.Left + 3, right = x >= bounds.Right - 3;
				bool top = y < bounds.Top + 3, bottom = y >= bounds.Bottom - 3;
				corner = top && left ? 0 : top && right ? 1 : bottom && left ? 2 : bottom && right ? 3 : -1;
			}
			if (corner == _activeCorner) return;
			_activeCorner = corner;
			if (corner < 0) return;
			var macro = _macroForm.GetActiveCornerMacro();
			if (macro == null) return;
			bool enabled = corner switch { 0 => macro.TopLeft, 1 => macro.TopRight, 2 => macro.BottomLeft, _ => macro.BottomRight };
			if (enabled) RunMacroActions(macro, macro.GetCornerAction(corner));
		}

		private void PerformProcessAction(ProcessAction action, List<ProcessConfig> targets)
		{
			if (targets.Count == 0) return;
			Task.Run(() =>
			{
				try
				{
					if (targets.Count == 0)
					{
						return;
					}

					var processNames = targets
						.Select(target => NormalizeProcessName(target.Name))
						.Where(name => !string.IsNullOrWhiteSpace(name))
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToList();

					var descendants = targets.Any(t => !t.ProtectChildren) ? BuildChildProcessLookup() : new Dictionary<int, List<int>>();
					// Remove all target windows from view before waiting for any process termination.
					var prepared = new HashSet<int>();
					foreach (var target in targets)
					{
						foreach (var process in Process.GetProcessesByName(NormalizeProcessName(target.Name)))
						{
							using (process)
							{
								var ids = target.ProtectChildren ? new HashSet<int> { process.Id } : GetProcessIdsWithDescendants(process.Id, descendants);
								prepared.UnionWith(ids);
							}
						}
					}
					RequestMinimizeWindows(FindTargetWindows(prepared));
					if (action == ProcessAction.Minimize) return;
					foreach (var processName in processNames)
					{
						Process[] processes = Array.Empty<Process>();
						try
						{
							processes = Process.GetProcessesByName(processName);
						}
						catch
						{
						}

						if (processes.Length == 0)
						{
							continue;
						}

						var processConfig = targets.FirstOrDefault(target =>
							NormalizeProcessName(target.Name).Equals(processName, StringComparison.OrdinalIgnoreCase));

						if (action == ProcessAction.Close)
						{
							foreach (var process in processes) process.Dispose();
							if (processConfig?.ProtectChildren == true)
							{
								TryTaskKill(processName, false);
							}
							else
							{
								TryTaskKill(processName, true);
							}

							continue;
						}

					}
				}
				catch (Exception ex) { AppLog.Error("Process macro failed", ex); }
			});
		}

		private static void TryTaskKill(string processBaseName, bool includeChildren)
		{
			try
			{
				using var process = Process.Start(BuildTaskKillStartInfo(processBaseName, includeChildren));
				if (process == null) { AppLog.Error("taskkill did not start."); return; }
				if (!process.WaitForExit(1000)) AppLog.Info("taskkill is still running after one second.");
				else if (process.ExitCode != 0) AppLog.Error($"taskkill returned exit code {process.ExitCode}.");
			}
			catch (Exception ex) { AppLog.Error("Unable to run taskkill.", ex); }
		}

		private static string NormalizeProcessName(string input)
		{
			return ProcessRule.NormalizeName(input);
		}

		internal static ProcessStartInfo BuildTaskKillStartInfo(string name, bool includeChildren)
		{
			var info = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "taskkill.exe")) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
			info.ArgumentList.Add("/IM"); info.ArgumentList.Add(ProcessRule.NormalizeName(name) + ".exe"); info.ArgumentList.Add("/F");
			if (includeChildren) info.ArgumentList.Add("/T");
			return info;
		}

		internal static List<IntPtr> FindTargetWindows(IReadOnlySet<int> processIds)
		{
			var windows = new List<IntPtr>();
			if (processIds.Count == 0) return windows;
			EnumWindows((hWnd, _) =>
			{
				GetWindowThreadProcessId(hWnd, out var processId);
				if (processIds.Contains((int)processId) && IsWindowVisible(hWnd) && !IsIconic(hWnd) && ShouldAffectWindow(hWnd))
					windows.Add(hWnd);
				return true;
			}, IntPtr.Zero);
			return windows;
		}

		internal static int RequestMinimizeWindows(IEnumerable<IntPtr> windows)
		{
			int requested = 0;
			// Queue every request without waiting for another application's UI thread.
			foreach (var hWnd in windows.Distinct())
				if (ShowWindowAsync(hWnd, SW_FORCEMINIMIZE)) requested++;
			return requested;
		}

		private static HashSet<int> GetProcessIdsWithDescendants(int rootProcessId, Dictionary<int, List<int>> childrenByParent)
		{
			var result = new HashSet<int> { rootProcessId };
			var queue = new Queue<int>();
			queue.Enqueue(rootProcessId);

			while (queue.Count > 0)
			{
				var parentId = queue.Dequeue();
				if (!childrenByParent.TryGetValue(parentId, out var childIds))
				{
					continue;
				}

				foreach (var childId in childIds)
				{
					if (result.Add(childId))
					{
						queue.Enqueue(childId);
					}
				}
			}

			return result;
		}

		private static Dictionary<int, List<int>> BuildChildProcessLookup()
		{
			var childrenByParent = new Dictionary<int, List<int>>();
			var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
			if (snapshot == INVALID_HANDLE_VALUE)
			{
				return childrenByParent;
			}

			try
			{
				var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
				if (!Process32First(snapshot, ref entry))
				{
					return childrenByParent;
				}

				do
				{
					if (!childrenByParent.TryGetValue((int)entry.th32ParentProcessID, out var children))
					{
						children = new List<int>();
						childrenByParent[(int)entry.th32ParentProcessID] = children;
					}

					children.Add((int)entry.th32ProcessID);
				}
				while (Process32Next(snapshot, ref entry));
			}
			finally
			{
				CloseHandle(snapshot);
			}

			return childrenByParent;
		}

		private void AnyForm_FormClosingToTray(object? sender, FormClosingEventArgs e)
		{
			if (_disposing || e.CloseReason != CloseReason.UserClosing) return;
			e.Cancel = true;
			(sender as Form)?.Hide();
		}

		private void ExitApplication()
		{
			SaveState();
			ExitThread();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposing)
			{
				_disposing = true;
				var menu = _notifyIcon.ContextMenuStrip;
				var icon = _notifyIcon.Icon;
				_notifyIcon.Dispose();
				menu?.Dispose(); icon?.Dispose();
				AlarmAudio.Stop();
				_alarmTimer.Dispose();
				_cornerCheckTimer.Dispose();
				_hotkeyManager.Dispose();
				_hotkeyWindow.Dispose();
				_alarmForm.Dispose();
				_macroForm.Dispose();
				_popupForm.Dispose();
			}

			base.Dispose(disposing);
		}

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool EnumWindows(EnumWindowDelegate lpfn, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(int nIndex);

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern bool DestroyIcon(IntPtr hIcon);

		private static bool ShouldAffectWindow(IntPtr hWnd)
		{
			var ex = GetWindowLong(hWnd, GWL_EXSTYLE);
			if ((ex & WS_EX_TOOLWINDOW) != 0 || (ex & WS_EX_NOACTIVATE) != 0)
			{
				return false;
			}

			var className = new System.Text.StringBuilder(256);
			if (GetClassName(hWnd, className, className.Capacity) > 0)
			{
				var value = className.ToString();
				if (value.IndexOf("IME", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return false;
				}

				if (string.Equals(value, "Default IME", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(value, "MSCTFIME UI", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		}

		private delegate bool EnumWindowDelegate(IntPtr hWnd, IntPtr lParam);

		private const uint TH32CS_SNAPPROCESS = 0x00000002;
		private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
		private const int SM_CXSCREEN = 0;
		private const int SM_CYSCREEN = 1;
		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_MINIMIZE = 0xF020;
		private const int SW_FORCEMINIMIZE = 11;
		private const int GWL_EXSTYLE = -20;
		private const int WS_EX_TOOLWINDOW = 0x00000080;
		private const int WS_EX_NOACTIVATE = 0x08000000;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct PROCESSENTRY32
		{
			public uint dwSize;
			public uint cntUsage;
			public uint th32ProcessID;
			public IntPtr th32DefaultHeapID;
			public uint th32ModuleID;
			public uint cntThreads;
			public uint th32ParentProcessID;
			public int pcPriClassBase;
			public uint dwFlags;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szExeFile;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int X;
			public int Y;
		}
	}
}
