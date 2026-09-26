using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TheAlarm;

internal sealed class ProcessPickerForm : ModernForm
{
    private readonly DarkListView _list = new() { Dock = DockStyle.Fill, CheckBoxes = true, AccessibleName = "Запущенные процессы" };
    private readonly InputBox _search = new() { Dock = DockStyle.Top, Height = 34 };
    private readonly List<(string Name, int Id, string Title)> _snapshot = new();
    private readonly HashSet<string> _selected = new(StringComparer.OrdinalIgnoreCase);
    private bool _rendering;
    public IEnumerable<string> SelectedNames => _selected.ToArray();

    public ProcessPickerForm()
    {
        Text = "Запущенные процессы";
        ClientSize = new Size(640, 464);
        MinimumSize = new Size(448, 288);
        _search.Editor.PlaceholderText = "Поиск процесса или названия окна";
        _list.Columns.Add("Процесс", 192);
        _list.Columns.Add("PID", 86);
        _list.Columns.Add("Окно", 312);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(6) };
        var add = new RoundedButton { Text = "Добавить", Primary = true, Size = new Size(112, 29), DialogResult = DialogResult.OK };
        var refresh = new RoundedButton { Text = "Обновить", Size = new Size(112, 29) };
        var cancel = new RoundedButton { Text = "Отмена", Size = new Size(112, 29), DialogResult = DialogResult.Cancel };
        actions.Controls.Add(add);
        actions.Controls.Add(cancel);
        actions.Controls.Add(refresh);
        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(19) };
        body.Controls.Add(_list); body.Controls.Add(_search); body.Controls.Add(actions); Controls.Add(body);
        AcceptButton = add;
        CancelButton = cancel;
        refresh.Click += (_, __) => RefreshProcesses();
        _search.Editor.TextChanged += (_, __) => Render();
        _list.ItemChecked += (_, e) =>
        {
            if (_rendering || e.Item == null || _list.Disposing || _list.IsDisposed) return;
            if (e.Item.Checked) _selected.Add(e.Item.Text); else _selected.Remove(e.Item.Text);
            _rendering = true;
            try { foreach (ListViewItem? item in _list.Items) if (item != null && item.Text.Equals(e.Item.Text, StringComparison.OrdinalIgnoreCase)) item.Checked = e.Item.Checked; }
            finally { _rendering = false; }
        };
        RefreshProcesses();
        UiTheme.Apply(this);
    }

    private void RefreshProcesses()
    {
        _snapshot.Clear();
        Process[] processes;
        try { processes = Process.GetProcesses(); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        { AppLog.Error("Unable to enumerate running processes", ex); Render(); return; }
        foreach (var process in processes)
        {
            using (process)
            {
                try { _snapshot.Add((process.ProcessName, process.Id, process.MainWindowTitle)); }
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
            }
        }
        Render();
    }

    private void Render()
    {
        _rendering = true;
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var p in _snapshot.OrderBy(p => p.Name).ThenBy(p => p.Id))
                if (p.Name.Contains(_search.Editor.Text, StringComparison.OrdinalIgnoreCase) || p.Title.Contains(_search.Editor.Text, StringComparison.OrdinalIgnoreCase))
                    _list.Items.Add(new ListViewItem(new[] { p.Name, p.Id.ToString(), p.Title }) { Checked = _selected.Contains(p.Name) });
        }
        finally { _list.EndUpdate(); _rendering = false; }
    }
}
