using Microsoft.Win32;
namespace TheAlarm;

internal static class StartupService
{
    private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TheAlarm";
    private static string Command => $"\"{Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "TheAlarm.exe")}\"";
    public static bool IsEnabled()
    {
        try { using var key = Registry.CurrentUser.OpenSubKey(RegistryPath); return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase); }
        catch (Exception ex) { AppLog.Error("Unable to read startup setting", ex); return false; }
    }
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
            if (enabled) key.SetValue(ValueName, Command); else key.DeleteValue(ValueName, false);
            return IsEnabled() == enabled;
        }
        catch (Exception ex) { AppLog.Error("Unable to change startup setting", ex); return false; }
    }
}

public enum ProcessAction { Close, Minimize }
