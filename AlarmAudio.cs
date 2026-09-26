using System.Runtime.InteropServices;
using System.Media;

namespace TheAlarm;

internal static class AlarmAudio
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, System.Text.StringBuilder? result, int length, IntPtr callback);

    public static void Stop() => mciSendString("close alarm_audio", null, 0, IntPtr.Zero);

    public static void Play(string path)
    {
        Stop();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || path.Contains('"') ||
            mciSendString($"open \"{path}\" type mpegvideo alias alarm_audio", null, 0, IntPtr.Zero) != 0 ||
            mciSendString("play alarm_audio from 0", null, 0, IntPtr.Zero) != 0)
        {
            Stop();
            SystemSounds.Exclamation.Play();
        }
    }
}
