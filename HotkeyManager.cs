using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TheAlarm
{
	[Flags]
	public enum HotkeyModifiers
	{
		None = 0,
		Alt = 0x0001,
		Control = 0x0002,
		Shift = 0x0004,
		Windows = 0x0008
	}

	public readonly struct HotkeyGesture : IEquatable<HotkeyGesture>
	{
		public HotkeyGesture(HotkeyModifiers modifiers, Keys key)
		{
			Modifiers = modifiers;
			Key = key;
		}

		public HotkeyModifiers Modifiers { get; }
		public Keys Key { get; }

		public bool Equals(HotkeyGesture other)
		{
			return Modifiers == other.Modifiers && Key == other.Key;
		}

		public override bool Equals(object? obj)
		{
			return obj is HotkeyGesture other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)Modifiers, (int)Key);
		}

		public override string ToString()
		{
			return HotkeyText.Format(this);
		}

		public uint ToWin32Modifiers()
		{
			return (uint)Modifiers;
		}
	}

	public static class HotkeyText
	{
		private static readonly Keys[] InvalidKeys =
		{
			Keys.None,
			Keys.ControlKey,
			Keys.Menu,
			Keys.ShiftKey,
			Keys.LControlKey,
			Keys.RControlKey,
			Keys.LShiftKey,
			Keys.RShiftKey,
			Keys.LMenu,
			Keys.RMenu
		};

		public static bool TryParse(MacroHotkey? hotkey, out HotkeyGesture gesture)
		{
			gesture = default;
			if (hotkey == null)
			{
				return false;
			}

			if (!TryParseModifiers(hotkey.Modifiers, out var modifiers))
			{
				return false;
			}

			if (!TryParseKey(hotkey.Key, out var key))
			{
				return false;
			}

			gesture = new HotkeyGesture(modifiers, key);
			return true;
		}

		public static MacroHotkey ToMacroHotkey(HotkeyGesture gesture)
		{
			return new MacroHotkey
			{
				Modifiers = SerializeModifiers(gesture.Modifiers),
				Key = gesture.Key.ToString()
			};
		}

		public static bool IsEmpty(MacroHotkey? hotkey)
		{
			return hotkey == null
				|| (string.IsNullOrWhiteSpace(hotkey.Modifiers) && string.IsNullOrWhiteSpace(hotkey.Key));
		}

		public static string Format(MacroHotkey? hotkey)
		{
			return TryParse(hotkey, out var gesture) ? Format(gesture) : string.Empty;
		}

		public static string Format(HotkeyGesture gesture)
		{
			var parts = new List<string>(4);
			if (gesture.Modifiers.HasFlag(HotkeyModifiers.Control))
			{
				parts.Add("Ctrl");
			}

			if (gesture.Modifiers.HasFlag(HotkeyModifiers.Alt))
			{
				parts.Add("Alt");
			}

			if (gesture.Modifiers.HasFlag(HotkeyModifiers.Shift))
			{
				parts.Add("Shift");
			}

			if (gesture.Modifiers.HasFlag(HotkeyModifiers.Windows))
			{
				parts.Add("Win");
			}

			parts.Add(gesture.Key.ToString());
			return string.Join("+", parts);
		}

		public static bool TryCreateGestureFromKeyEvent(KeyEventArgs e, out HotkeyGesture gesture)
		{
			gesture = default;
			var key = e.KeyCode;
			if (InvalidKeys.Contains(key))
			{
				return false;
			}

			var modifiers = HotkeyModifiers.None;
			if (e.Control)
			{
				modifiers |= HotkeyModifiers.Control;
			}

			if (e.Alt)
			{
				modifiers |= HotkeyModifiers.Alt;
			}

			if (e.Shift)
			{
				modifiers |= HotkeyModifiers.Shift;
			}

			if ((Control.ModifierKeys & Keys.LWin) == Keys.LWin || (Control.ModifierKeys & Keys.RWin) == Keys.RWin)
			{
				modifiers |= HotkeyModifiers.Windows;
			}

			gesture = new HotkeyGesture(modifiers, key);
			return true;
		}

		public static bool TryParseModifiers(string? value, out HotkeyModifiers modifiers)
		{
			modifiers = HotkeyModifiers.None;
			if (string.IsNullOrWhiteSpace(value))
			{
				return true;
			}

			var tokens = value.Split(new[] { '+', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (var token in tokens)
			{
				switch (token.Trim().ToLowerInvariant())
				{
					case "ctrl":
					case "control":
						modifiers |= HotkeyModifiers.Control;
						break;
					case "alt":
						modifiers |= HotkeyModifiers.Alt;
						break;
					case "shift":
						modifiers |= HotkeyModifiers.Shift;
						break;
					case "win":
					case "windows":
					case "lwin":
					case "rwin":
						modifiers |= HotkeyModifiers.Windows;
						break;
					default:
						return false;
				}
			}

			return true;
		}

		public static bool TryParseKey(string? value, out Keys key)
		{
			key = Keys.None;
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			if (!Enum.TryParse(value.Trim(), true, out key))
			{
				return false;
			}

			return !InvalidKeys.Contains(key);
		}

		private static string SerializeModifiers(HotkeyModifiers modifiers)
		{
			var parts = new List<string>(4);
			if (modifiers.HasFlag(HotkeyModifiers.Control))
			{
				parts.Add("Control");
			}

			if (modifiers.HasFlag(HotkeyModifiers.Alt))
			{
				parts.Add("Alt");
			}

			if (modifiers.HasFlag(HotkeyModifiers.Shift))
			{
				parts.Add("Shift");
			}

			if (modifiers.HasFlag(HotkeyModifiers.Windows))
			{
				parts.Add("Windows");
			}

			return string.Join("+", parts);
		}
	}

	public sealed class HotkeyActionBinding
	{
		public required string Id { get; init; }
		public required HotkeyGesture Gesture { get; init; }
		public required Action Handler { get; init; }
	}

	public sealed class MacroHotkeyBinding
	{
		public required string MacroId { get; init; }
		public bool IsActive { get; init; }
		public string ScriptText { get; init; } = string.Empty;
		public MacroHotkey Hotkey { get; init; } = new MacroHotkey();
		public required Action Handler { get; init; }
	}

	public sealed class HotkeyManager : IDisposable
	{
		private readonly GlobalHotkeyWindow _window;
		private readonly Dictionary<HotkeyGesture, List<Action>> _routes = new Dictionary<HotkeyGesture, List<Action>>();
		private readonly Dictionary<HotkeyGesture, List<string>> _gestureToMacroIds = new Dictionary<HotkeyGesture, List<string>>();
		private readonly Dictionary<string, string?> _macroStatuses = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		private List<HotkeyActionBinding> _internalBindings = new List<HotkeyActionBinding>();
		private List<MacroHotkeyBinding> _macroBindings = new List<MacroHotkeyBinding>();
		private bool _disposed;

		public HotkeyManager(GlobalHotkeyWindow window)
		{
			_window = window ?? throw new ArgumentNullException(nameof(window));
			_window.HotkeyPressed += OnHotkeyPressed;
		}

		public IReadOnlyDictionary<string, string?> MacroStatuses => _macroStatuses;

		public void SetInternalBindings(IEnumerable<HotkeyActionBinding> bindings)
		{
			_internalBindings = bindings?.ToList() ?? new List<HotkeyActionBinding>();
			Rebuild();
		}

		public IReadOnlyDictionary<string, string?> SetMacroBindings(IEnumerable<MacroHotkeyBinding> bindings)
		{
			_macroBindings = bindings?.ToList() ?? new List<MacroHotkeyBinding>();
			Rebuild();
			return _macroStatuses;
		}

		private void Rebuild()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			_window.UnregisterAllHotkeys();
			_routes.Clear();
			_gestureToMacroIds.Clear();
			_macroStatuses.Clear();

			foreach (var binding in _internalBindings)
			{
				AddRoute(binding.Gesture, binding.Handler, null);
			}

			var groupedValidMacros = new Dictionary<HotkeyGesture, List<MacroHotkeyBinding>>();
			foreach (var macro in _macroBindings)
			{
				_macroStatuses[macro.MacroId] = null;

				if (!macro.IsActive)
				{
					continue;
				}

				if (string.IsNullOrWhiteSpace(macro.ScriptText))
				{
					_macroStatuses[macro.MacroId] = "Active macro requires script text.";
					continue;
				}

				if (HotkeyText.IsEmpty(macro.Hotkey))
				{
					_macroStatuses[macro.MacroId] = "Active macro requires a hotkey.";
					continue;
				}

				if (!HotkeyText.TryParse(macro.Hotkey, out var gesture))
				{
					_macroStatuses[macro.MacroId] = "Hotkey format is invalid.";
					continue;
				}

				if (!groupedValidMacros.TryGetValue(gesture, out var macrosForGesture))
				{
					macrosForGesture = new List<MacroHotkeyBinding>();
					groupedValidMacros[gesture] = macrosForGesture;
				}

				macrosForGesture.Add(macro);
			}

			foreach (var entry in groupedValidMacros)
			{
				if (entry.Value.Count > 1)
				{
					foreach (var conflictingMacro in entry.Value)
					{
						_macroStatuses[conflictingMacro.MacroId] = "Hotkey conflicts with another user macro.";
					}

					continue;
				}

				var activeMacro = entry.Value[0];
				AddRoute(entry.Key, activeMacro.Handler, activeMacro.MacroId);
			}

			foreach (var entry in _routes.Keys.ToList())
			{
				if (_window.TryRegisterHotkey(entry, out _))
				{
					continue;
				}

				AppLog.Error($"Failed to register global hotkey '{entry}'.");
				if (_gestureToMacroIds.TryGetValue(entry, out var macroIds))
				{
					foreach (var macroId in macroIds)
					{
						_macroStatuses[macroId] = "Windows did not register this hotkey.";
					}
				}
			}
		}

		private void AddRoute(HotkeyGesture gesture, Action handler, string? macroId)
		{
			if (!_routes.TryGetValue(gesture, out var handlers))
			{
				handlers = new List<Action>();
				_routes[gesture] = handlers;
			}

			handlers.Add(handler);

			if (string.IsNullOrWhiteSpace(macroId))
			{
				return;
			}

			if (!_gestureToMacroIds.TryGetValue(gesture, out var macroIds))
			{
				macroIds = new List<string>();
				_gestureToMacroIds[gesture] = macroIds;
			}

			macroIds.Add(macroId);
		}

		private void OnHotkeyPressed(HotkeyGesture gesture)
		{
			if (!_routes.TryGetValue(gesture, out var handlers))
			{
				return;
			}

			foreach (var handler in handlers.ToList())
			{
				try
				{
					handler();
				}
				catch (Exception ex)
				{
					AppLog.Error($"Hotkey handler failed for '{gesture}'.", ex);
				}
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_window.HotkeyPressed -= OnHotkeyPressed;
			_window.UnregisterAllHotkeys();
		}
	}
}
