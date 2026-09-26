using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace TheAlarm;

internal sealed class ProcessPickerForm : Form
{
    private readonly ListView _list = new() { Dock = DockStyle.Fill, View = View.Details, CheckBoxes = true, FullRowSelect = true, HideSelection = false, AccessibleName = "Running processes" };
    private readonly TextBox _search = new() { Dock = DockStyle.Top, PlaceholderText = "Search process or window title", AccessibleName = "Search processes" };
    private readonly List<(string Name, int Id, string Title)> _snapshot = new();
    public IEnumerable<string> SelectedNames => _list.CheckedItems.Cast<ListViewItem>().Select(item => item.Text).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public ProcessPickerForm()
    {
        Text = "Select running processes";
        ClientSize = new Size(720, 480);
        MinimumSize = new Size(560, 360);
        StartPosition = FormStartPosition.CenterParent;
        _list.Columns.Add("Process", 200);
        _list.Columns.Add("PID", 80);
        _list.Columns.Add("Window", 390);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        var add = new Button { Text = "Add selected", AutoSize = true, DialogResult = DialogResult.OK };
        var refresh = new Button { Text = "Refresh", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        actions.Controls.Add(add);
        actions.Controls.Add(cancel);
        actions.Controls.Add(refresh);
        Controls.Add(_list);
        Controls.Add(_search);
        Controls.Add(actions);
        AcceptButton = add;
        CancelButton = cancel;
        refresh.Click += (_, __) => RefreshProcesses();
        _search.TextChanged += (_, __) => Render();
        RefreshProcesses();
        UiTheme.Apply(this);
    }

    private void RefreshProcesses()
    {
        _snapshot.Clear();
        foreach (var process in Process.GetProcesses())
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
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var p in _snapshot.OrderBy(p => p.Name).ThenBy(p => p.Id))
                if (p.Name.Contains(_search.Text, StringComparison.OrdinalIgnoreCase) || p.Title.Contains(_search.Text, StringComparison.OrdinalIgnoreCase))
                    _list.Items.Add(new ListViewItem(new[] { p.Name, p.Id.ToString(), p.Title }));
        }
        finally { _list.EndUpdate(); }
    }
}
