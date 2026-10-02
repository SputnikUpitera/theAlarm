using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using TheAlarm;

internal static class AuditChecks
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    public static void Run()
    {
        ProcessRules(); StorageFailures(); WindowBatchChecks.Run(); MenuLifetime(); PickerSelection(); AlarmCatchup(); LayoutAlignment(); WindowResources(); IdleRuntime();
        Check(!HotkeyText.TryParseKey("999999", out _) && !HotkeyText.TryParseKey("LWin", out _), "Invalid hotkey accepted");
        Console.WriteLine("PASS AUDIT: menu lifetime, corrupt/future config preservation, process names/arguments, picker selection, alarm catchup, key validation");
    }
    private static void ProcessRules()
    {
        foreach (var name in new[] { "app.worker", "my process", "dot.net.worker" })
        {
            var normalized = ProcessRule.NormalizeName(name + ".exe");
            for (int i = 0; i < 20; i++) normalized = ProcessRule.NormalizeName(normalized);
            Check(normalized == name, "Process normalization is not idempotent: " + name);
            var info = TrayAppContext.BuildTaskKillStartInfo(normalized, true);
            Check(info.ArgumentList.SequenceEqual(new[] { "/IM", name + ".exe", "/F", "/T" }), "Taskkill argument boundaries");
            Check(!info.RedirectStandardOutput && !info.RedirectStandardError, "Undrained redirected pipes");
        }
        var macro = new MacroDefinition { Actions = new ProcessRulesState { CloseProcesses = new List<ProcessRule> { null!, new() { Name = "a.b.exe" } } } };
        Check(macro.Clone().Normalize().Actions.CloseProcesses.Single().Name == "a.b", "Null rule in stored macro");
    }
    private static void StorageFailures()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "audit-state", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var repository = new AppStateRepository(dir);
        var valid = new AppState().Normalize();
        Check(repository.Save(valid, out _), "Initial config save");
        var protectedState = new EncryptionService().Protect(JsonSerializer.SerializeToUtf8Bytes(new AppState { SchemaVersion = 900 }));
        var future = JsonSerializer.SerializeToUtf8Bytes(new EncryptedFileEnvelope { CiphertextBase64 = Convert.ToBase64String(protectedState) });
        foreach (var bytes in new[] { Encoding.UTF8.GetBytes("broken envelope"), future, Encoding.UTF8.GetBytes("{\"Version\":999,\"CiphertextBase64\":\"AA==\"}") })
        {
            File.WriteAllBytes(repository.EncryptedConfigPath, bytes);
            Check(repository.Load().WarningMessage != null, "Unreadable config accepted");
            Check(!repository.Save(new AppState(), out _), "Unreadable config overwritten");
            Check(File.ReadAllBytes(repository.EncryptedConfigPath).SequenceEqual(bytes), "Original bytes changed");
        }
        File.Delete(repository.EncryptedConfigPath);
        repository.Load();
        Check(repository.Save(valid, out _), "Recovery after failed load");
    }
    private static void MenuLifetime()
    {
        using var host = new ModernForm { ClientSize = new Size(360, 220) };
        var button = new ChoiceButton("Close", "Minimize") { Location = new Point(30, 80), Width = 140 };
        host.Controls.Add(button); host.Show(); Application.DoEvents();
        var field = typeof(ChoiceButton).GetField("_menu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var menu = (ContextMenuStrip)field.GetValue(button)!;
        int selections = 0; button.SelectionChanged += (_, _) => selections++;
        var reasons = new[] { ToolStripDropDownCloseReason.AppClicked, ToolStripDropDownCloseReason.Keyboard, ToolStripDropDownCloseReason.AppFocusChange, ToolStripDropDownCloseReason.ItemClicked };
        for (int i = 0; i < 250; i++)
        {
            button.PerformClick(); Check(menu.Visible, $"Menu failed to open at cycle {i}; host visible={host.Visible}, enabled={button.Enabled}");
            Application.DoEvents();
            ((ToolStripMenuItem)menu.Items[i % 2]).PerformClick();
            menu.Close(reasons[i % reasons.Length]); Application.DoEvents();
            Check(!menu.IsDisposed, "Menu disposed inside close transition");
            Check(button.SelectedIndex == i % 2, "Wrong selection");
        }
        Check(selections == 250, "Selection handlers duplicated");
        host.Dispose(); Check(menu.IsDisposed, "Menu leaked on owner disposal");
        Console.WriteLine("PASS: 250 menu open/select/close cycles; disposal belongs to owner");
    }
    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private static void PickerSelection()
    {
        using var picker = new ProcessPickerForm(); picker.Show(); Application.DoEvents();
        var list = Descendants(picker).OfType<ListView>().Single();
        var search = Descendants(picker).OfType<TextBox>().Single();
        Check(list.Items.Count > 0, "No running processes");
        var name = list.Items[0].Text; list.Items[0].Checked = true;
        search.Text = "no-such-process-70dc29b1";
        Check(picker.SelectedNames.Contains(name), "Filtering lost selection");
        search.Clear();
        Check(list.Items.Cast<ListViewItem>().Where(i => i.Text == name).All(i => i.Checked), "Checkmarks not restored");
        list.Items.Cast<ListViewItem>().First(i => i.Text == name).Checked = false;
        Check(!picker.SelectedNames.Contains(name), "Uncheck did not propagate");
    }
    private static void AlarmCatchup()
    {
        using var form = new AlarmForm();
        form.LoadAlarms(new List<AlarmState> { new() { IsDaily = true, TimeUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), Message = "daily" }, new() { TimeUtc = DateTime.UtcNow.AddSeconds(-1), Message = "once" } });
        var watch = Stopwatch.StartNew(); var due = form.ConsumeDueAlarms(); watch.Stop();
        Check(due.Count == 2 && form.GetAlarms().Count == 1, "Lost due alarm");
        Check(form.GetAlarms()[0].TimeUtc > DateTime.UtcNow && form.ConsumeDueAlarms().Count == 0, "Daily catchup repeats");
        Check(watch.ElapsedMilliseconds < 2000, "Daily catchup blocked UI");
        Console.WriteLine($"PASS: ancient daily alarm catchup in {watch.ElapsedMilliseconds} ms");
    }
    private static void WindowResources()
    {
        void Cycle()
        {
            using var editor = new MacroActionForm(new MacroDefinition { Name = "Audit" });
            editor.Shown += (_, _) => editor.BeginInvoke((Action)(() => editor.DialogResult = DialogResult.Cancel));
            Check(editor.ShowDialog() == DialogResult.Cancel, "Modal cancel failed");
        }
        for (int i = 0; i < 3; i++) Cycle();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        using var process = Process.GetCurrentProcess();
        uint gdi = GetGuiResources(process.Handle, 0), user = GetGuiResources(process.Handle, 1);
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < 50; i++) Cycle();
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        long gdiDelta = (long)GetGuiResources(process.Handle, 0) - gdi, userDelta = (long)GetGuiResources(process.Handle, 1) - user;
        Check(gdiDelta < 32 && userDelta < 32, "Unbounded GUI resource growth");
        Console.WriteLine($"PASS: 50 modal editor cycles, GDI delta {gdiDelta}, USER delta {userDelta}, {watch.ElapsedMilliseconds} ms");
    }
    private static void LayoutAlignment()
    {
        using var editor = new MacroActionForm(new MacroDefinition());
        editor.Show(); Application.DoEvents();
        Check(editor.Font.Size == 9, "Body font must use compact 9pt size");
        Check(editor.Font.Bold, "Body font is not bold");
        foreach (var box in Descendants(editor).OfType<InputBox>().Where(b => !b.Editor.Multiline))
            Check(Math.Abs(box.Editor.Top * 2 + box.Editor.Height - box.ClientSize.Height) <= 1, "Input text is not vertically centered");
        Check(Descendants(editor).OfType<Label>().Any(l => l.Text == "Название"), "Name caption missing");
        Check(Descendants(editor).OfType<Label>().Any(l => l.Text.StartsWith("Функция")), "Trigger caption missing");
        var lists = Descendants(editor).OfType<ListView>().ToArray();
        var processList = (DarkListView)lists[0];
        var item = processList.Items.Add(new ListViewItem(new[] { "test.process", "" }));
        Application.DoEvents();
        var hit = processList.ToggleBounds(item);
        void Click(int message, int x, int y) => SendMessage(processList.Handle, message, (IntPtr)1, (IntPtr)((y << 16) | x));
        int cx = hit.Left + hit.Width / 2, cy = hit.Top + hit.Height / 2;
        Click(0x201, cx, cy); Click(0x202, cx, cy);
        Check(item.Checked, "Single click on right toggle failed");
        Click(0x203, cx, cy); Click(0x202, cx, cy);
        Check(item.Checked, "Double click on toggle undid the first click");
        PostMessage(processList.Handle, 0x201, (IntPtr)1, (IntPtr)((cy << 16) | 40));
        PostMessage(processList.Handle, 0x202, IntPtr.Zero, (IntPtr)((cy << 16) | 40));
        Application.DoEvents();
        Check(item.Checked, "Single click on row changed protection");
        Click(0x203, 40, cy); Click(0x202, 40, cy);
        Check(!item.Checked, "Double click on row did not change protection");
        Check(lists.Length == 2 && lists.All(l => l.Height >= 160), "Process panels too short");
        int before = lists[0].Height;
        editor.Height += 40; Application.DoEvents();
        Check(lists[0].Height >= before + 35, "Process panels do not consume extra height");
        editor.Close();
        Console.WriteLine("PASS: input vertical alignment, field captions, 9pt font, expanding process panels");
    }
    private static void IdleRuntime()
    {
        var state = new AppState().Normalize();
        foreach (var m in state.Macros.Definitions) m.IsActive = false;
        Check(new AppStateRepository().Save(state, out _), "Idle test setup");
        using var context = new TrayAppContext();
        using var timer = new System.Windows.Forms.Timer { Interval = 15000 };
        timer.Tick += (_, _) => { timer.Stop(); context.ExitThread(); };
        using var process = Process.GetCurrentProcess();
        var before = process.TotalProcessorTime;
        timer.Start(); var watch = Stopwatch.StartNew(); Application.Run(context); watch.Stop();
        process.Refresh();
        Console.WriteLine($"PASS: idle tray message loop {watch.ElapsedMilliseconds} ms; CPU {(process.TotalProcessorTime - before).TotalMilliseconds:F0} ms (diagnostic, not long-term soak)");
    }
    [DllImport("user32.dll")] private static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
}
