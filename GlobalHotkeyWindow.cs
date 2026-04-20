using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TheAlarm
{
	public sealed class GlobalHotkeyWindow : NativeWindow, IDisposable
	{
		private readonly Dictionary<int, HotkeyGesture> _registrations = new Dictionary<int, HotkeyGesture>();
		private int _nextRegistrationId = 1;
		private bool _disposed;

		public event Action<HotkeyGesture>? HotkeyPressed;

		public GlobalHotkeyWindow()
		{
			CreateHandle(new CreateParams());
		}

		public bool TryRegisterHotkey(HotkeyGesture gesture, out int registrationId)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			registrationId = _nextRegistrationId++;
			if (!RegisterHotKey(Handle, registrationId, (uint)gesture.ToWin32Modifiers(), (uint)gesture.Key))
			{
				registrationId = 0;
				return false;
			}

			_registrations[registrationId] = gesture;
			return true;
		}

		public void UnregisterAllHotkeys()
		{
			foreach (var registrationId in _registrations.Keys)
			{
				try
				{
					UnregisterHotKey(Handle, registrationId);
				}
				catch
				{
				}
			}

			_registrations.Clear();
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_HOTKEY)
			{
				var registrationId = m.WParam.ToInt32();
				if (_registrations.TryGetValue(registrationId, out var gesture))
				{
					HotkeyPressed?.Invoke(gesture);
				}
			}

			base.WndProc(ref m);
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			UnregisterAllHotkeys();
			DestroyHandle();
			GC.SuppressFinalize(this);
		}

		private const int WM_HOTKEY = 0x0312;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
	}
}
