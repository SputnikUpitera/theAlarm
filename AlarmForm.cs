using System.Globalization;
namespace TheAlarm;

public sealed class AlarmForm : ModernForm
{
    public string AlarmSoundPath { get; set; } = string.Empty;
    public event EventHandler? AlarmsChanged;
    private readonly List<AlarmState> _alarms = new();
    private readonly DarkListView _list = new() { Dock = DockStyle.Fill };
    private readonly Label _empty = new() { Text = "Пока нет будильников\nДобавьте время и сообщение выше", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
    public AlarmForm()
    {
        Text = "The Alarm"; ClientSize = new Size(736, 648); MinimumSize = new Size(672, 592);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 19), ColumnCount = 1, RowCount = 7 };
        foreach (var h in new[] { 56, 115, 43 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        foreach (var h in new[] { 38, 85, 45 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        layout.Controls.Add(UiTheme.Heading("Будильники"), 0, 0);
        var create = new MacroCard { Dock = DockStyle.Fill, Padding = new Padding(13), Margin = new Padding(0, 0, 0, 10) };
        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 37)); fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
        var time = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 5) }; time.Editor.Text = DateTime.Now.ToString("HH:mm"); time.Editor.AccessibleName = "Время, часы и минуты";
        var date = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 5) }; date.Editor.Text = DateTime.Today.ToString("dd.MM.yyyy"); date.Editor.AccessibleName = "Дата, день месяц год";
        var daily = new ToggleSwitch { Text = "Ежедневно", Checked = true, Dock = DockStyle.Fill };
        var message = new InputBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 5) }; message.Editor.PlaceholderText = "О чём напомнить?";
        var add = new RoundedButton { Text = "Добавить", Primary = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 5) };
        fields.Controls.Add(time, 0, 0); fields.Controls.Add(date, 1, 0); fields.Controls.Add(daily, 2, 0);
        fields.Controls.Add(message, 0, 1); fields.SetColumnSpan(message, 3); fields.Controls.Add(add, 3, 1);
        create.Controls.Add(fields); layout.Controls.Add(create, 0, 1);
        add.Click += (_, _) =>
        {
            if (!TimeSpan.TryParseExact(time.Editor.Text, new[] { @"h\:mm", @"hh\:mm", @"hh\:mm\:ss" }, CultureInfo.InvariantCulture, out var t) || t.TotalHours >= 24 ||
                !DateTime.TryParseExact(date.Editor.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            { MessageBox.Show(this, "Введите время ЧЧ:ММ и дату ДД.ММ.ГГГГ.", "Будильник"); return; }
            var candidate = (daily.Checked ? DateTime.Today : d.Date).Add(t);
            if (daily.Checked && candidate <= DateTime.Now) candidate = candidate.AddDays(1);
            if (!daily.Checked && candidate <= DateTime.Now) { MessageBox.Show(this, "Выберите будущее время.", "Будильник"); return; }
            _alarms.Add(new AlarmState { TimeUtc = candidate.ToUniversalTime(), Message = message.Editor.Text, IsDaily = daily.Checked });
            RefreshAlarms(); AlarmsChanged?.Invoke(this, EventArgs.Empty);
        };
        layout.Controls.Add(UiTheme.Caption("РАСПИСАНИЕ"), 0, 2);
        _list.Columns.Add("Время", 168); _list.Columns.Add("Сообщение", 312); _list.Columns.Add("Повтор", 104);
        var table = new MacroCard { Dock = DockStyle.Fill, Padding = new Padding(6), Margin = Padding.Empty };
        table.Controls.Add(_list); table.Controls.Add(_empty); layout.Controls.Add(table, 0, 3);
        var remove = new RoundedButton { Text = "Удалить выбранные", Width = 184, Height = 29, Anchor = AnchorStyles.Right, Margin = new Padding(0, 6, 0, 3) };
        remove.Click += (_, _) => { foreach (ListViewItem item in _list.SelectedItems) _alarms.Remove((AlarmState)item.Tag!); RefreshAlarms(); AlarmsChanged?.Invoke(this, EventArgs.Empty); };
        layout.Controls.Add(remove, 0, 4);
        var soundSection = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Margin = new Padding(0, 16, 0, 0) };
        soundSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); soundSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        soundSection.Controls.Add(UiTheme.Caption("Сигнал будильника"), 0, 0);
        var audio = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        var choose = new RoundedButton { Text = "Загрузить MP3", Width = 136, Height = 30 };
        var reset = new RoundedButton { Text = "Стандартный сигнал", Width = 184, Height = 30 };
        var play = new RoundedButton { Text = "Слушать", Width = 88, Height = 30 };
        var stop = new RoundedButton { Text = "Стоп", Width = 72, Height = 30 };
        choose.Click += (_, _) => { using var dialog = new OpenFileDialog { Filter = "MP3 (*.mp3)|*.mp3", CheckFileExists = true }; if (dialog.ShowDialog(this) == DialogResult.OK) { AlarmSoundPath = dialog.FileName; AlarmsChanged?.Invoke(this, EventArgs.Empty); } };
        reset.Click += (_, _) => { AlarmSoundPath = string.Empty; AlarmsChanged?.Invoke(this, EventArgs.Empty); };
        play.Click += (_, _) => AlarmAudio.Play(AlarmSoundPath); stop.Click += (_, _) => AlarmAudio.Stop();
        audio.Controls.AddRange(new Control[] { choose, reset, play, stop }); soundSection.Controls.Add(audio, 0, 1); layout.Controls.Add(soundSection, 0, 5);
        var startup = new ToggleSwitch { Text = "Запускать с Windows", Width = 240, Checked = StartupService.IsEnabled(), Margin = new Padding(2, 13, 0, 0) };
        bool updating = false;
        startup.CheckedChanged += (_, _) => { if (updating) return; if (!StartupService.SetEnabled(startup.Checked)) { updating = true; startup.Checked = !startup.Checked; updating = false; MessageBox.Show(this, "Не удалось изменить автозапуск.", "The Alarm"); } };
        layout.Controls.Add(startup, 0, 6); Controls.Add(layout); RefreshAlarms();
    }
    public void LoadAlarms(List<AlarmState> alarms) { _alarms.Clear(); _alarms.AddRange(alarms.Where(a => a != null).Select(a => a.Clone().Normalize())); RefreshAlarms(); }
    public List<AlarmState> GetAlarms() => _alarms.Select(a => a.Clone().Normalize()).ToList();
    public List<string> ConsumeDueAlarms()
    {
        var now = DateTime.UtcNow; var due = _alarms.Where(a => a.TimeUtc <= now).ToList();
        foreach (var alarm in due)
        {
            if (!alarm.IsDaily) _alarms.Remove(alarm);
            else
            {
                var local = now.ToLocalTime().Date.Add(alarm.TimeUtc.ToLocalTime().TimeOfDay);
                if (local.ToUniversalTime() <= now) local = local.AddDays(1);
                alarm.TimeUtc = local.ToUniversalTime();
            }
        }
        if (due.Count > 0) { RefreshAlarms(); AlarmsChanged?.Invoke(this, EventArgs.Empty); }
        return due.Select(a => a.Message).ToList();
    }
    private void RefreshAlarms()
    {
        _list.Items.Clear();
        foreach (var a in _alarms) _list.Items.Add(new ListViewItem(new[] { a.TimeUtc.ToLocalTime().ToString("dd.MM.yyyy  HH:mm"), a.Message, a.IsDaily ? "Ежедневно" : "Один раз" }) { Tag = a });
        _empty.Visible = _alarms.Count == 0; _list.Visible = !_empty.Visible;
    }
}
