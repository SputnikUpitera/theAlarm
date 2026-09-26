using System.Drawing;
using System.Windows.Forms;
namespace TheAlarm;

public sealed class MacroForm : ModernForm
{
    private readonly FlowLayoutPanel _list = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16) };
    private readonly List<MacroDefinition> _models = new();
    private readonly Dictionary<string, Label> _statuses = new();
    public event EventHandler? MacrosChanged;
    public event Action<MacroDefinition>? RunRequested;
    public event EventHandler? EditorVisibilityChanged;
    public bool IsEditing { get; private set; }
    public MacroForm(MacroExecutionService service)
    {
        Text = "The Alarm"; ClientSize = new Size(704, 600); MinimumSize = new Size(624, 496);
        var heading = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(19, 14, 19, 6) };
        var title = UiTheme.Heading("Макросы"); title.Dock = DockStyle.Fill; heading.Controls.Add(title);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 54, Padding = new Padding(16, 8, 16, 8) };
        var add = new RoundedButton { Text = "Новый макрос", Primary = true, Size = new Size(176, 32) };
        add.Click += (_, _) => Edit(new MacroDefinition(), true);
        footer.Controls.Add(add); Controls.Add(_list); Controls.Add(footer); Controls.Add(heading);
        _list.SizeChanged += (_, _) => { foreach (Control c in _list.Controls) c.Width = Math.Max(432, _list.ClientSize.Width - 48); };
    }
    public void LoadMacros(IEnumerable<MacroDefinition> definitions) { _models.Clear(); _models.AddRange(definitions.Select(m => m.Clone().Normalize())); Render(); }
    public List<MacroDefinition> GetMacros() => _models.Select(m => m.Clone()).ToList();
    public MacroDefinition? GetMacro(string id) => _models.FirstOrDefault(m => m.Id.Equals(id, StringComparison.OrdinalIgnoreCase))?.Clone();
    public MacroDefinition? GetActiveCornerMacro() => _models.FirstOrDefault(m => m.IsCornerMacro && m.IsActive)?.Clone();
    public void SetRegistrationStatuses(IReadOnlyDictionary<string, string?> statuses)
    { foreach (var p in _statuses) p.Value.Text = statuses.TryGetValue(p.Key, out var s) ? s ?? "" : ""; }
    private void Changed() => MacrosChanged?.Invoke(this, EventArgs.Empty);
    private void Edit(MacroDefinition model, bool isNew)
    {
        using var editor = new MacroActionForm(model);
        editor.ValidateDefinition = candidate => _models.Any(other => other.Id != candidate.Id && !other.IsCornerMacro && HotkeyText.TryParse(other.Hotkey, out var a) && HotkeyText.TryParse(candidate.Hotkey, out var b) && a.Equals(b))
            ? "Это сочетание уже назначено другому макросу." : null;
        DialogResult result;
        IsEditing = true; EditorVisibilityChanged?.Invoke(this, EventArgs.Empty);
        try { result = editor.ShowDialog(this); }
        finally { IsEditing = false; EditorVisibilityChanged?.Invoke(this, EventArgs.Empty); }
        if (result != DialogResult.OK) return;
        if (isNew) _models.Add(editor.Result); else _models[_models.IndexOf(model)] = editor.Result;
        Render(); Changed();
    }
    private void Render()
    {
        _list.SuspendLayout();
        foreach (Control c in _list.Controls.Cast<Control>().ToArray()) c.Dispose();
        _statuses.Clear();
        foreach (var m in _models)
        {
            var card = new MacroCard { Width = Math.Max(432, _list.ClientSize.Width - 48), Height = m.IsCornerMacro ? 262 : 138, Margin = new Padding(0, 0, 0, 19) };
            card.Controls.Add(new Label { Text = m.Name, Location = new Point(14, 13), AutoSize = true, Font = new Font("Segoe UI", 11.25f, FontStyle.Bold) });
            var active = new ToggleSwitch { Text = "Включён", Checked = m.IsActive, Location = new Point(card.Width - 131, 13), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            active.CheckedChanged += (_, _) => { m.IsActive = active.Checked; Changed(); }; card.Controls.Add(active);
            if (m.IsCornerMacro)
            {
                var monitor = new CornerDiagram { Location = new Point(168, 50), Size = new Size(248, 126) };
                void Corner(string label, int x, int y, bool value, Action<bool> set)
                {
                    var toggle = new ToggleSwitch { Text = string.Empty, Size = new Size(32, 32), Location = new Point(x, y), Checked = value, AccessibleName = label };
                    toggle.CheckedChanged += (_, _) => { set(toggle.Checked); Changed(); }; monitor.Controls.Add(toggle);
                }
                Corner("Левый верхний угол", 0, 0, m.TopLeft, v => m.TopLeft = v); Corner("Правый верхний угол", monitor.Width - 32, 0, m.TopRight, v => m.TopRight = v);
                Corner("Левый нижний угол", 0, monitor.Height - 32, m.BottomLeft, v => m.BottomLeft = v); Corner("Правый нижний угол", monitor.Width - 32, monitor.Height - 32, m.BottomRight, v => m.BottomRight = v);
                void ActionChoice(int x, int y, ProcessAction action, Action<ProcessAction> set, string name)
                {
                    var choice = new ChoiceButton("Закрыть", "Свернуть") { Location = new Point(x, y), Size = new Size(101, 27), SelectedIndex = (int)action, AccessibleName = name };
                    choice.SelectionChanged += (_, _) => { set((ProcessAction)choice.SelectedIndex); Changed(); }; card.Controls.Add(choice);
                }
                ActionChoice(56, 51, m.TopLeftAction, v => m.TopLeftAction = v, "Действие левого верхнего угла");
                ActionChoice(427, 51, m.TopRightAction, v => m.TopRightAction = v, "Действие правого верхнего угла");
                ActionChoice(56, 144, m.BottomLeftAction, v => m.BottomLeftAction = v, "Действие левого нижнего угла");
                ActionChoice(427, 144, m.BottomRightAction, v => m.BottomRightAction = v, "Действие правого нижнего угла");
                card.Controls.Add(monitor);
            }
            else card.Controls.Add(new Label { Text = HotkeyText.Format(m.Hotkey), AutoSize = true, Location = new Point(152, 43) });
            int y = m.IsCornerMacro ? 200 : 72;
            var edit = new RoundedButton { Text = "Настроить действия", Location = new Point(14, y), Size = new Size(176, 29) };
            edit.Click += (_, _) => Edit(m, false); card.Controls.Add(edit);
            var run = new RoundedButton { Text = "Запустить", Location = new Point(200, y), Size = new Size(96, 29) };
            run.Click += (_, _) => RunRequested?.Invoke(m.Clone()); card.Controls.Add(run);
            if (!m.IsCornerMacro)
            {
                var delete = new RoundedButton { Text = "Удалить", Location = new Point(306, y), Size = new Size(96, 29) };
                delete.Click += (_, _) => { _models.Remove(m); Render(); Changed(); }; card.Controls.Add(delete);
            }
            var status = new Label { Location = new Point(14, y + 30), Width = 408, Height = 19 };
            _statuses[m.Id] = status; card.Controls.Add(status); _list.Controls.Add(card); UiTheme.Apply(card);
        }
        _list.ResumeLayout();
    }
}
