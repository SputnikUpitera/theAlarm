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
		private readonly SettingsForm _settingsForm;
		private readonly MacroForm _macroForm;
		private readonly PopupForm _popupForm;
		private readonly System.Windows.Forms.Timer _alarmTimer;
		private readonly System.Windows.Forms.Timer _cornerCheckTimer;
		private readonly LowLevelMouseHook _mouseHook = new LowLevelMouseHook();
		private readonly GlobalHotkeyWindow _hotkeyWindow;
		private readonly HotkeyManager _hotkeyManager;
		private readonly MacroExecutionService _macroExecutionService;
		private readonly AppStateRepository _stateRepository;

		private AppState _appState = new AppState();
		private bool _isLoadingState;
		private bool _isProcessingAction;
		private DateTime _lastCornerActionUtc = DateTime.MinValue;

		public TrayAppContext()
		{
			_macroExecutionService = new MacroExecutionService();
			_stateRepository = new AppStateRepository();

			_alarmForm = new AlarmForm();
			_settingsForm = new SettingsForm();
			_macroForm = new MacroForm(_macroExecutionService);
			_popupForm = new PopupForm();

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
			_settingsForm.FormClosing += AnyForm_FormClosingToTray;
			_macroForm.FormClosing += AnyForm_FormClosingToTray;

			_alarmForm.AlarmsChanged += (_, __) => SaveState();
			_settingsForm.ConfigurationChanged += (_, __) => SaveState();
			_macroForm.MacrosChanged += (_, __) => SaveStateAndRefreshMacroHotkeys();

			LoadState();

			_hotkeyWindow = new GlobalHotkeyWindow();
			_hotkeyManager = new HotkeyManager(_hotkeyWindow);
			_hotkeyManager.SetInternalBindings(new[]
			{
				new HotkeyActionBinding
				{
					Id = "internal-settings-window",
					Gesture = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.F1),
					Handler = ToggleSettingsWindow
				},
				new HotkeyActionBinding
				{
					Id = "internal-macro-window",
					Gesture = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.F2),
					Handler = ToggleMacroWindow
				}
			});
			RefreshMacroHotkeys();

			_alarmTimer = new System.Windows.Forms.Timer { Interval = 1000 };
			_alarmTimer.Tick += AlarmTimer_Tick;
			_alarmTimer.Start();

			_cornerCheckTimer = new System.Windows.Forms.Timer { Interval = 200 };
			_cornerCheckTimer.Tick += CornerCheckTimer_Tick;
			_cornerCheckTimer.Start();

			_mouseHook.MouseMove += MouseHook_MouseMove;
			_mouseHook.Start();
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

				_settingsForm.LoadConfiguration(
					ToProcessConfigs(_appState.ProcessRules.CloseProcesses),
					ToProcessConfigs(_appState.ProcessRules.MinimizeProcesses));
				_alarmForm.LoadAlarms(_appState.Alarms);
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

			var newState = new AppState
			{
				SchemaVersion = AppState.CurrentSchemaVersion,
				ProcessRules = new ProcessRulesState
				{
					CloseProcesses = ToProcessRules(_settingsForm.GetProcessConfigsForAction(ProcessAction.Close)),
					MinimizeProcesses = ToProcessRules(_settingsForm.GetProcessConfigsForAction(ProcessAction.Minimize))
				},
				Alarms = _alarmForm.GetAlarms(),
				Macros = new MacroState
				{
					Definitions = _macroForm.GetMacros()
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
			var macros = _macroForm.GetMacros();
			var statuses = _hotkeyManager.SetMacroBindings(macros.Select(CreateMacroBinding).ToList());
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
			var macro = _macroForm
				.GetMacros()
				.FirstOrDefault(item => string.Equals(item.Id, macroId, StringComparison.OrdinalIgnoreCase));

			if (macro == null)
			{
				AppLog.Error($"Macro '{macroId}' was requested by hotkey but no longer exists.");
				return;
			}

			if (!_macroExecutionService.TryExecute(macro, out var errorMessage))
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

		private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ShowAlarm();
			}
		}

		private void ShowAlarm()
		{
			_settingsForm.Hide();
			_macroForm.Hide();
			_alarmForm.ShowInTaskbar = true;
			_alarmForm.WindowState = FormWindowState.Normal;
			_alarmForm.Show();
			_alarmForm.BringToFront();
			_alarmForm.Activate();
		}

		private void ShowSettingsWindow()
		{
			_alarmForm.Hide();
			_macroForm.Hide();
			_settingsForm.ShowInTaskbar = true;
			_settingsForm.WindowState = FormWindowState.Normal;
			_settingsForm.Show();
			_settingsForm.BringToFront();
			_settingsForm.Activate();
		}

		private void ShowMacroWindow()
		{
			_alarmForm.Hide();
			_settingsForm.Hide();
			_macroForm.ShowInTaskbar = true;
			_macroForm.WindowState = FormWindowState.Normal;
			_macroForm.Show();
			_macroForm.BringToFront();
			_macroForm.Activate();
		}

		private void ToggleSettingsWindow()
		{
			if (_settingsForm.Visible)
			{
				_settingsForm.Hide();
				return;
			}

			ShowSettingsWindow();
		}

		private void ToggleMacroWindow()
		{
			if (_macroForm.Visible)
			{
				_macroForm.Hide();
				return;
			}

			ShowMacroWindow();
		}

		private void AlarmTimer_Tick(object? sender, EventArgs e)
		{
			var due = _alarmForm.ConsumeDueAlarms();
			foreach (var message in due)
			{
				_popupForm.SetMessage(message);
				_popupForm.Show();
				_popupForm.Activate();
			}
		}

		private void CornerCheckTimer_Tick(object? sender, EventArgs e)
		{
			if (_mouseHook == null)
			{
				return;
			}

			GetCursorPos(out var point);
			CheckCornersAndAct(point.X, point.Y);
		}

		private void MouseHook_MouseMove(int x, int y)
		{
			CheckCornersAndAct(x, y);
		}

		private void CheckCornersAndAct(int x, int y)
		{
			var screenW = GetSystemMetrics(SM_CXSCREEN);
			var screenH = GetSystemMetrics(SM_CYSCREEN);

			const int margin = 2;
			var rightThreshold = screenW - 1 - margin;
			var topBand = 1 + margin;
			var bottomThreshold = screenH - 1 - margin;
			var rightOfThreshold = x > rightThreshold;

			if (rightOfThreshold && y < topBand)
			{
				PerformProcessAction(ProcessAction.Close);
				return;
			}

			if (rightOfThreshold && y > bottomThreshold)
			{
				PerformProcessAction(ProcessAction.Minimize);
			}
		}

		private void PerformProcessAction(ProcessAction action)
		{
			var nowUtc = DateTime.UtcNow;
			if ((nowUtc - _lastCornerActionUtc).TotalMilliseconds < 250)
			{
				return;
			}

			_lastCornerActionUtc = nowUtc;
			if (_isProcessingAction)
			{
				return;
			}

			_isProcessingAction = true;
			Task.Run(() =>
			{
				try
				{
					var targets = _settingsForm.GetProcessConfigsForAction(action);
					if (targets.Count == 0)
					{
						return;
					}

					var processNames = targets
						.Select(target => NormalizeProcessName(target.Name))
						.Where(name => !string.IsNullOrWhiteSpace(name))
						.Distinct(StringComparer.OrdinalIgnoreCase)
						.ToList();

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
							if (processConfig?.ProtectChildren == true)
							{
								TryTaskKillNoChildrenAsync(processName);
							}
							else
							{
								TryTaskKillAsync(processName);
							}

							continue;
						}

						foreach (var process in processes)
						{
							try
							{
								var processIdsToMinimize = processConfig?.ProtectChildren == true
									? new HashSet<int> { process.Id }
									: GetProcessIdsWithDescendants(process.Id);

								foreach (var processId in processIdsToMinimize)
								{
									TryMinimizeProcessWindows(processId);
								}
							}
							catch
							{
							}
							finally
							{
								try
								{
									process.Dispose();
								}
								catch
								{
								}
							}
						}
					}
				}
				finally
				{
					_isProcessingAction = false;
				}
			});
		}

		private static void TryTaskKillAsync(string processBaseName)
		{
			Task.Run(() =>
			{
				try
				{
					var startInfo = new ProcessStartInfo("taskkill", $"/IM {processBaseName}.exe /F /T")
					{
						CreateNoWindow = true,
						UseShellExecute = false,
						WindowStyle = ProcessWindowStyle.Hidden,
						RedirectStandardError = true,
						RedirectStandardOutput = true
					};
					using var process = Process.Start(startInfo);
					process?.WaitForExit(1000);
				}
				catch
				{
				}
			});
		}

		private static void TryTaskKillNoChildrenAsync(string processBaseName)
		{
			Task.Run(() =>
			{
				try
				{
					var startInfo = new ProcessStartInfo("taskkill", $"/IM {processBaseName}.exe /F")
					{
						CreateNoWindow = true,
						UseShellExecute = false,
						WindowStyle = ProcessWindowStyle.Hidden,
						RedirectStandardError = true,
						RedirectStandardOutput = true
					};
					using var process = Process.Start(startInfo);
					process?.WaitForExit(1000);
				}
				catch
				{
				}
			});
		}

		private static string NormalizeProcessName(string input)
		{
			var value = (input ?? string.Empty).Trim().Trim('"');
			if (value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				value = value.Substring(0, value.Length - 4);
			}

			try
			{
				var fileName = Path.GetFileNameWithoutExtension(value);
				if (!string.IsNullOrEmpty(fileName))
				{
					return fileName;
				}
			}
			catch
			{
			}

			return value;
		}

		private static void EnumThreadWindowsForProcess(Process process, Func<IntPtr, bool> onWindow)
		{
			ProcessThread[] threads;
			try
			{
				threads = process.Threads.Cast<ProcessThread>().ToArray();
			}
			catch
			{
				return;
			}

			foreach (var thread in threads)
			{
				try
				{
					EnumThreadWindows((uint)thread.Id, (hWnd, _) =>
					{
						if (IsWindowVisible(hWnd))
						{
							return onWindow(hWnd);
						}

						return true;
					}, IntPtr.Zero);
				}
				catch
				{
				}
			}
		}

		private static void TryMinimizeProcessWindows(int processId)
		{
			try
			{
				using var process = Process.GetProcessById(processId);
				EnumThreadWindowsForProcess(process, hWnd =>
				{
					if (!ShouldAffectWindow(hWnd))
					{
						return true;
					}

					SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero);
					ShowWindow(hWnd, SW_FORCEMINIMIZE);
					return true;
				});
			}
			catch
			{
			}
		}

		private static HashSet<int> GetProcessIdsWithDescendants(int rootProcessId)
		{
			var result = new HashSet<int> { rootProcessId };
			var childrenByParent = BuildChildProcessLookup();
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
			e.Cancel = true;
			(sender as Form)?.Hide();
		}

		private void ExitApplication()
		{
			SaveState();
			_notifyIcon.Visible = false;
			_hotkeyManager.Dispose();
			_hotkeyWindow.Dispose();
			Environment.Exit(0);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_notifyIcon.Dispose();
				_alarmTimer.Dispose();
				_cornerCheckTimer.Dispose();
				_hotkeyManager.Dispose();
				_hotkeyWindow.Dispose();
				_mouseHook.Dispose();
				_alarmForm.Dispose();
				_settingsForm.Dispose();
				_macroForm.Dispose();
				_popupForm.Dispose();
			}

			base.Dispose(disposing);
		}

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

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

		private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

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
