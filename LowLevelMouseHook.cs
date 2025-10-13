using System;
using System.Runtime.InteropServices;

namespace TheAlarm
{
	public sealed class LowLevelMouseHook : IDisposable
	{
		private IntPtr _hookId = IntPtr.Zero;
		private HookProc? _proc;
		public event Action<int, int>? MouseMove;

		public void Start()
		{
			_proc = HookCallback;
			_hookId = SetHook(_proc);
		}

		public void Stop()
		{
			if (_hookId != IntPtr.Zero)
			{
				UnhookWindowsHookEx(_hookId);
				_hookId = IntPtr.Zero;
			}
		}

		public void Dispose()
		{
			Stop();
		}

		private IntPtr SetHook(HookProc proc)
		{
			using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
			using var curModule = curProcess.MainModule!;
			return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
		}

		private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && (wParam == (IntPtr)WM_MOUSEMOVE))
			{
				var info = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
				MouseMove?.Invoke(info.pt.x, info.pt.y);
			}
			return CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		private const int WH_MOUSE_LL = 14;
		private const int WM_MOUSEMOVE = 0x0200;

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT { public int x; public int y; }

		[StructLayout(LayoutKind.Sequential)]
		private struct MSLLHOOKSTRUCT
		{
			public POINT pt;
			public int mouseData;
			public int flags;
			public int time;
			public IntPtr dwExtraInfo;
		}

		private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}







