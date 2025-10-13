using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CURSORTrayApp
{
	public sealed class GlobalHotkeyWindow : NativeWindow, IDisposable
	{
		private readonly int _openId;
		private readonly int _closeId;
		public event Action<ToggleKind>? ToggleRequested;
		public Func<bool>? CanToggleEvaluator { get; set; }

		public GlobalHotkeyWindow()
		{
			CreateHandle(new CreateParams());
			_openId = GetHashCode() ^ 0xA11;
			_closeId = GetHashCode() ^ 0xB22;
			RegisterHotKey(Handle, _openId, MOD_CONTROL | MOD_ALT, Keys.F1.GetHashCode());
			RegisterHotKey(Handle, _closeId, MOD_CONTROL | MOD_ALT, Keys.F2.GetHashCode());
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_HOTKEY)
			{
				int id = m.WParam.ToInt32();
				if (id == _openId)
				{
					if (CanToggleEvaluator == null || CanToggleEvaluator())
					{
						ToggleRequested?.Invoke(ToggleKind.Open);
					}
				}
				else if (id == _closeId)
				{
					if (CanToggleEvaluator == null || CanToggleEvaluator())
					{
						ToggleRequested?.Invoke(ToggleKind.Close);
					}
				}
			}
			base.WndProc(ref m);
		}

		public void Dispose()
		{
			UnregisterHotKey(Handle, _openId);
			UnregisterHotKey(Handle, _closeId);
			DestroyHandle();
		}

		public enum ToggleKind { Open, Close }

		private const int WM_HOTKEY = 0x0312;
		private const int MOD_ALT = 0x0001;
		private const int MOD_CONTROL = 0x0002;

		[DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
		[DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
	}
}





