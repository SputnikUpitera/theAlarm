using System.Reflection;
using System.Text;
using System.Text.Json;
using TheAlarm;

internal static class Program
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private)!.GetValue(owner)!;
    private static void Call(object owner, string name) => owner.GetType().GetMethod(name, Private)!.Invoke(owner, null);

    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        AppLog.TestLogFilePath = Path.Combine(AppContext.BaseDirectory, "audit.log");
        Application.EnableVisualStyles();
        AuditChecks.Run();
        var state = new AppState();
        state.ProcessRules.CloseProcesses.Add(new ProcessRule { Name = "smoke-not-a-real-process", ProtectChildren = true });
        state.Macros.Definitions.Add(new MacroDefinition { ScriptText = "echo smoke-secret", IsActive = false });
        state.Normalize().Normalize();
        Check(state.Macros.Definitions.Count == 2, "Migration duplicated corner macro");
        var corner = state.Macros.Definitions.Single(m => m.IsCornerMacro);
        Check(corner.Actions.CloseProcesses.Single().ProtectChildren, "Lost process protection");
        Check(state.Macros.Definitions.Single(m => !m.IsCornerMacro).ScriptEnabled == true, "Lost legacy script enablement");
        Check(corner.TopLeftAction == ProcessAction.Close && corner.BottomRightAction == ProcessAction.Minimize, "Legacy corner defaults changed");
        corner.TopLeftAction = ProcessAction.Minimize; corner.BottomRightAction = ProcessAction.Close;
        var cornerCopy = JsonSerializer.Deserialize<MacroDefinition>(JsonSerializer.Serialize(corner.Clone()))!;
        Check(cornerCopy.GetCornerAction(0) == ProcessAction.Minimize && cornerCopy.GetCornerAction(3) == ProcessAction.Close, "Independent corner actions lost");
        corner.IsActive = false;
        state.Normalize();
        Check(!state.Macros.Definitions.Single(m => m.IsCornerMacro).IsActive, "Normalize re-enabled corner macro");
        var encryption = new EncryptionService();
        var plain = JsonSerializer.SerializeToUtf8Bytes(state);
        var encrypted = encryption.Protect(plain);
        Check(!Encoding.UTF8.GetString(encrypted).Contains("smoke-secret"), "Plaintext leaked");
        Check(encryption.Unprotect(encrypted).SequenceEqual(plain), "Encryption roundtrip");

        // Only the test executable directory is used for config; never read the user's build config.
        var repository = new AppStateRepository();
        Check(Path.GetDirectoryName(repository.EncryptedConfigPath) == Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), "Unexpected config path");
        Check(repository.Save(state, out var error), error ?? "Save failed");
        Check(repository.Load().State.Macros.Definitions.Count == 2, "Repository roundtrip");
        Check(repository.Load().State.Macros.Definitions.Single(m => m.IsCornerMacro).TopLeftAction == ProcessAction.Minimize, "Corner action repository roundtrip");
        using var context = new TrayAppContext();
        var alarm = Field<AlarmForm>(context, "_alarmForm");
        var macros = Field<MacroForm>(context, "_macroForm");
        var manager = Field<HotkeyManager>(context, "_hotkeyManager");
        Check(alarm.FormBorderStyle == FormBorderStyle.None && macros.FormBorderStyle == FormBorderStyle.None, "Native frame exists before first show");
        Check(Field<List<HotkeyActionBinding>>(manager, "_internalBindings").Count == 0, "Internal shortcut registered in tray");
        Call(context, "ToggleMacroWindow");
        Check(!alarm.Visible && !macros.Visible, "Tray shortcut opened a window");
        Call(context, "ShowAlarm");
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Check(Math.Abs(alarm.Left - (area.Left + (area.Width - alarm.Width) / 2)) <= 1, "Alarm not centered");
        Check(Math.Abs(alarm.Top - (area.Top + (area.Height - alarm.Height) / 2)) <= 1, "Alarm not vertically centered");
        Check(Field<List<HotkeyActionBinding>>(manager, "_internalBindings").Single().Gesture.Key == Keys.F1, "Expected F1 only");
        Call(context, "ToggleMacroWindow");
        Check(macros.Visible && !alarm.Visible, "Alarm to macros");
        Check(Math.Abs(macros.Left - (area.Left + (area.Width - macros.Width) / 2)) <= 1, "Macros not centered");
        Call(context, "ToggleMacroWindow");
        Check(alarm.Visible && !macros.Visible, "Macros to alarm");
        alarm.Hide();
        Check(Field<List<HotkeyActionBinding>>(manager, "_internalBindings").Count == 0, "Internal shortcut retained in tray");

        using var window = new GlobalHotkeyWindow();
        using var hotkeys = new HotkeyManager(window);
        var gesture = new HotkeyGesture(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.F11);
        int executions = 0;
        var statuses = hotkeys.SetMacroBindings(new[] { new MacroHotkeyBinding { MacroId = "process-only", IsActive = true, Hotkey = HotkeyText.ToMacroHotkey(gesture), Handler = () => executions++ } });
        Check(statuses["process-only"] == null, "Process-only macro rejected");
        typeof(HotkeyManager).GetMethod("OnHotkeyPressed", Private)!.Invoke(hotkeys, new object[] { gesture });
        Check(executions == 1, "Process-only route did not execute");
        using var popup = new PopupForm();
        popup.Show(); popup.Close();
        Check(!popup.IsDisposed, "Popup disposed on user close");
        popup.Show(); popup.Hide();
        alarm.LoadAlarms(new List<AlarmState> { new() { TimeUtc = DateTime.UtcNow.AddHours(1), Message = "Проверка расписания", IsDaily = true } });
        if (Environment.GetEnvironmentVariable("THEALARM_CAPTURE_UI") == "1")
        {
            // Documentation examples are isolated from stored user data and never executed.
            cornerCopy.Actions.CloseProcesses = new() { new() { Name = "notepad", ProtectChildren = true } };
            cornerCopy.Actions.MinimizeProcesses = new() { new() { Name = "CalculatorApp" } };
            var example = new MacroDefinition { Name = "Рабочие окна", IsActive = false, Hotkey = HotkeyText.ToMacroHotkey(gesture) };
            macros.LoadMacros(new[] { cornerCopy, example });
            Capture(alarm, "alarm"); Capture(macros, "macros");
        }
        using var editor = (Form)Activator.CreateInstance(typeof(AppState).Assembly.GetType("TheAlarm.MacroActionForm")!, cornerCopy)!;
        if (Environment.GetEnvironmentVariable("THEALARM_CAPTURE_UI") == "1") Capture(editor, "editor");
        using var picker = (Form)Activator.CreateInstance(typeof(AppState).Assembly.GetType("TheAlarm.ProcessPickerForm")!)!;
        if (Environment.GetEnvironmentVariable("THEALARM_CAPTURE_UI") == "1") Capture(picker, "processes");
        Console.WriteLine("PASS: migration, encryption, repository, tray gating, F1 toggle, process-only route, reusable popup");
    }
    private static void Capture(Form form, string name)
    {
        form.Show(); Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(AppContext.BaseDirectory, name + ".png")); form.Hide();
    }
}
