using System.Diagnostics;
using System.Runtime.InteropServices;
using TheAlarm;

internal static class WindowBatchChecks
{
    public static void Run()
    {
        var forms = new Form?[3];
        var handles = new IntPtr[3];
        var ready = Enumerable.Range(0, 3).Select(_ => new ManualResetEventSlim()).ToArray();
        var errors = new Exception?[3];
        using var blocked = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var threads = Enumerable.Range(0, 3).Select(index => new Thread(() =>
        {
            try
            {
                using var form = new Form
                {
                    Text = "Minimize audit " + index, ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), Size = new Size(160, 100)
                };
                forms[index] = form;
                form.Shown += (_, _) => { handles[index] = form.Handle; ready[index].Set(); };
                Application.Run(form);
            }
            catch (Exception ex) { errors[index] = ex; ready[index].Set(); }
        }) { IsBackground = true }).ToArray();
        try
        {
            foreach (var thread in threads) { thread.SetApartmentState(ApartmentState.STA); thread.Start(); }
            foreach (var signal in ready) Check(signal.Wait(5000), "Test window failed to start");
            Check(errors.All(e => e == null) && handles.All(h => h != IntPtr.Zero), "Test window startup failed");
            var found = TrayAppContext.FindTargetWindows(new HashSet<int> { Environment.ProcessId });
            Check(handles.All(found.Contains), "Batch enumeration missed a target window");
            Check(TrayAppContext.FindTargetWindows(new HashSet<int>()).Count == 0, "Empty targets matched windows");

            forms[0]!.BeginInvoke((Action)(() => { blocked.Set(); release.Wait(5000); }));
            Check(blocked.Wait(2000), "First window did not enter its blocked state");
            var watch = Stopwatch.StartNew();
            int queued = TrayAppContext.RequestMinimizeWindows(new[] { handles[0], handles[1], handles[2], handles[1] });
            watch.Stop();
            Check(queued == 3, "Requests were lost or duplicated");
            Check(watch.ElapsedMilliseconds < 500, "Batch waited for a blocked window");
            Check(SpinWait.SpinUntil(() => IsIconic(handles[1]) && IsIconic(handles[2]), 1500), "Responsive windows waited behind the blocked window");
            Check(!release.IsSet, "Blocked window was released before the regression check");
            Console.WriteLine($"PASS: three-window batch, blocked first UI thread, requests queued in {watch.ElapsedMilliseconds} ms");
        }
        finally
        {
            release.Set();
            foreach (var form in forms)
                if (form != null && !form.IsDisposed && form.IsHandleCreated) form.BeginInvoke((Action)(() => form.Close()));
            foreach (var thread in threads) if (thread.IsAlive) thread.Join(6000);
            foreach (var signal in ready) signal.Dispose();
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
}
