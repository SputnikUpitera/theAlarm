namespace TheAlarm;

internal sealed class MacroActionForm : ModernForm
{
    public MacroDefinition Result { get; }
    public Func<MacroDefinition, string?>? ValidateDefinition { get; set; }
    public MacroActionForm(MacroDefinition source)
    {
        Result = source.Clone().Normalize(); Text = "Настройки макроса";
        ClientSize = new Size(800, 736); MinimumSize = new Size(704, 640);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, Padding = new Padding(19), AutoScroll = true };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        foreach (int h in new[] { 56, 64, 43, 0, 38, 38, 58, 99, 51 }) layout.RowStyles.Add(new RowStyle(h == 0 ? SizeType.Percent : SizeType.Absolute, h == 0 ? 100 : h));
        var heading = UiTheme.Heading("Действия макроса"); layout.Controls.Add(heading, 0, 0); layout.SetColumnSpan(heading, 2);
        var nameBox = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 6) }; var name = nameBox.Editor; name.Text = Result.Name; name.AccessibleName = "Название макроса";
        var hotkeyBox = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 6) }; var hotkey = hotkeyBox.Editor;
        hotkey.ReadOnly = true; hotkey.Text = Result.IsCornerMacro ? "Триггер: углы экрана" : HotkeyText.Format(Result.Hotkey); hotkey.PlaceholderText = "Нажмите сочетание"; hotkey.Enabled = !Result.IsCornerMacro; hotkey.AccessibleName = "Горячая клавиша";
        hotkey.KeyDown += (_, e) => { if (e.KeyCode == Keys.Tab) return; e.SuppressKeyPress = true; if (HotkeyText.TryCreateGestureFromKeyEvent(e, out var g)) { Result.Hotkey = HotkeyText.ToMacroHotkey(g); hotkey.Text = HotkeyText.Format(g); } };
        void Labeled(Control field, string caption, int column)
        {
            var group = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            group.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); group.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var label = UiTheme.Caption(caption); label.Margin = new Padding(column == 0 ? 0 : 8, 0, 0, 0);
            group.Controls.Add(label, 0, 0); group.Controls.Add(field, 0, 1); layout.Controls.Add(group, column, 1);
        }
        Labeled(nameBox, "Название", 0); Labeled(hotkeyBox, "Функция (триггер / хоткей)", 1);
        layout.Controls.Add(UiTheme.Caption("Закрыть процессы"), 0, 2);
        var minimizeCaption = UiTheme.Caption("Свернуть процессы"); minimizeCaption.Margin = new Padding(6, 0, 0, 5); layout.Controls.Add(minimizeCaption, 1, 2);
        DarkListView ProcessList(IEnumerable<ProcessRule> rules, int col)
        {
            var list = new DarkListView { Dock = DockStyle.Fill, RightSideChecks = true, AccessibleName = col == 0 ? "Закрыть процессы" : "Свернуть процессы" };
            list.Columns.Add("Процесс", 150); list.Columns.Add("Защитить дочерние", 150);
            foreach (var p in rules) list.Items.Add(new ListViewItem(new[] { p.Name, "" }) { Checked = p.ProtectChildren });
            var card = new MacroCard { Dock = DockStyle.Fill, Padding = new Padding(6), Margin = col == 0 ? new Padding(0, 0, 6, 6) : new Padding(6, 0, 0, 6) };
            card.Controls.Add(list); layout.Controls.Add(card, col, 3);
            void Add(string n) { n = ProcessRule.NormalizeName(n); if (n.Length > 0 && !list.Items.Cast<ListViewItem>().Any(i => i.Text.Equals(n, StringComparison.OrdinalIgnoreCase))) list.Items.Add(new ListViewItem(new[] { n, "" })); }
            var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = col == 0 ? new Padding(0, 0, 6, 6) : new Padding(6, 0, 0, 6) };
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            var pick = new RoundedButton { Text = "Из запущенных", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
            var remove = new RoundedButton { Text = "Убрать", Dock = DockStyle.Fill, Margin = Padding.Empty };
            pick.Click += (_, _) => { using var picker = new ProcessPickerForm(); if (picker.ShowDialog(this) == DialogResult.OK) foreach (var n in picker.SelectedNames) Add(n); };
            remove.Click += (_, _) => { foreach (var item in list.SelectedItems.Cast<ListViewItem>().ToArray()) list.Items.Remove(item); };
            row.Controls.Add(pick); row.Controls.Add(remove); layout.Controls.Add(row, col, 4);
            var manual = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = row.Margin };
            manual.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            manual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); manual.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
            var input = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) }; input.Editor.PlaceholderText = "Имя процесса";
            var add = new RoundedButton { Icon = ButtonIcon.Plus, Dock = DockStyle.Fill, Margin = Padding.Empty, AccessibleName = "Добавить процесс" };
            add.Click += (_, _) => { Add(input.Editor.Text); input.Editor.Clear(); }; manual.Controls.Add(input); manual.Controls.Add(add); layout.Controls.Add(manual, col, 5);
            return list;
        }
        var close = ProcessList(Result.Actions.CloseProcesses, 0); var minimize = ProcessList(Result.Actions.MinimizeProcesses, 1);
        var enabled = new ToggleSwitch { Text = "Выполнить команды", Checked = Result.ScriptEnabled == true, Dock = DockStyle.Fill, Margin = new Padding(0, 19, 0, 6) };
        var runner = new ChoiceButton(MacroRunnerTypes.Cmd, MacroRunnerTypes.PowerShell) { Dock = DockStyle.Fill, SelectedIndex = Result.RunnerType == MacroRunnerTypes.PowerShell ? 1 : 0, Margin = new Padding(6, 19, 0, 6) };
        var scriptBox = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) }; var script = scriptBox.Editor;
        scriptBox.EnableCodeScroll(); script.AcceptsReturn = true; script.AcceptsTab = true; script.WordWrap = false; script.Text = Result.ScriptText; script.Font = new Font("Consolas", 9, FontStyle.Bold); script.AccessibleName = "Команды";
        script.ReadOnly = !enabled.Checked; runner.Enabled = enabled.Checked; enabled.CheckedChanged += (_, _) => { script.ReadOnly = !enabled.Checked; runner.Enabled = enabled.Checked; };
        layout.Controls.Add(enabled, 0, 6); layout.Controls.Add(runner, 1, 6); layout.Controls.Add(scriptBox, 0, 7); layout.SetColumnSpan(scriptBox, 2);
        var save = new RoundedButton { Text = "Сохранить", Primary = true, Dock = DockStyle.Top, Height = 32, Margin = new Padding(0, 13, 6, 0) };
        var cancel = new RoundedButton { Text = "Отмена", DialogResult = DialogResult.Cancel, Dock = DockStyle.Top, Height = 32, Margin = new Padding(6, 13, 0, 0) };
        save.Click += (_, _) =>
        {
            Result.Name = name.Text.Trim(); Result.ScriptEnabled = enabled.Checked; Result.ScriptText = script.Text; Result.RunnerType = runner.SelectedIndex == 1 ? MacroRunnerTypes.PowerShell : MacroRunnerTypes.Cmd;
            List<ProcessRule> Read(DarkListView list) => list.Items.Cast<ListViewItem>().Select(i => new ProcessRule { Name = i.Text, ProtectChildren = i.Checked }).ToList();
            Result.Actions = new ProcessRulesState { CloseProcesses = Read(close), MinimizeProcesses = Read(minimize) };
            var error = string.IsNullOrWhiteSpace(Result.Name) ? "Укажите название." : !Result.IsCornerMacro && !HotkeyText.TryParse(Result.Hotkey, out _) ? "Укажите сочетание клавиш." : ValidateDefinition?.Invoke(Result);
            if (error != null) { MessageBox.Show(this, error, "Макрос"); return; }
            DialogResult = DialogResult.OK;
        };
        layout.Controls.Add(save, 0, 8); layout.Controls.Add(cancel, 1, 8); Controls.Add(layout); CancelButton = cancel;
    }
}
