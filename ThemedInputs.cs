using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace TheAlarm;

internal sealed class InputBox : UserControl
{
    public TextBox Editor { get; } = new() { BorderStyle = BorderStyle.None };
    public InputBox()
    {
        Padding = new Padding(10, 7, 10, 6); Height = 32; BackColor = UiTheme.Input;
        Controls.Add(Editor); DoubleBuffered = true;
        Editor.GotFocus += (_, _) => Invalidate(); Editor.LostFocus += (_, _) => Invalidate();
    }
    public void EnableCodeScroll()
    {
        Editor.Multiline = true; Editor.Dock = DockStyle.Fill; Editor.ScrollBars = ScrollBars.None;
        var scroll = new TextScrollBar(Editor) { Dock = DockStyle.Right, Width = 12 };
        Controls.Add(scroll);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (Editor == null || Editor.Multiline) return;
        int height = Editor.PreferredHeight;
        Editor.SetBounds(Padding.Left, Math.Max(0, (ClientSize.Height - height) / 2), Math.Max(1, ClientSize.Width - Padding.Horizontal), height);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedButton.Shape(new Rectangle(0, 0, Width - 1, Height - 1), 7);
        using var pen = new Pen(Editor.Focused ? UiTheme.Accent : UiTheme.Border); e.Graphics.DrawPath(pen, path);
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedButton.Shape(new Rectangle(0, 0, Width - 1, Height - 1), 7);
        using var fill = new SolidBrush(BackColor); e.Graphics.FillPath(fill, path);
    }
}

internal sealed class ChoiceButton : RoundedButton
{
    private readonly string[] _choices;
    private readonly ContextMenuStrip _menu;
    private int _index;
    public int SelectedIndex { get => _index; set { _index = Math.Clamp(value, 0, _choices.Length - 1); Text = _choices[_index]; } }
    public ChoiceButton(params string[] choices)
    {
        ArgumentNullException.ThrowIfNull(choices);
        if (choices.Length == 0) throw new ArgumentException("At least one choice is required.", nameof(choices));
        _choices = choices; SelectedIndex = 0;
        // The button owns the menu. Closed is still inside WinForms' visibility transition.
        _menu = new ContextMenuStrip { BackColor = UiTheme.Surface, ForeColor = UiTheme.Text, ShowImageMargin = false, Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()) };
        for (int i = 0; i < _choices.Length; i++) { int index = i; _menu.Items.Add(_choices[i], null, (_, _) => { SelectedIndex = index; SelectionChanged?.Invoke(this, EventArgs.Empty); }); }
        Click += (_, _) =>
        {
            if (!IsDisposed && !Disposing) { _menu.Font = Font; _menu.Show(this, new Point(0, Height)); }
        };
    }
    protected override void Dispose(bool disposing) { if (disposing) _menu?.Dispose(); base.Dispose(disposing); }
    public event EventHandler? SelectionChanged;
}

internal sealed class DarkMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => UiTheme.Surface;
    public override Color MenuItemSelected => UiTheme.Input;
    public override Color MenuItemBorder => UiTheme.Border;
    public override Color MenuBorder => UiTheme.Border;
    public override Color SeparatorDark => UiTheme.Border;
    public override Color SeparatorLight => UiTheme.Border;
    public override Color MenuItemSelectedGradientBegin => UiTheme.Input;
    public override Color MenuItemSelectedGradientEnd => UiTheme.Input;
}

internal sealed class DarkListView : ListView
{
    public bool RightSideChecks { get; set; }
    private readonly ImageList _rowHeight = new() { ImageSize = new Size(1, 22), ColorDepth = ColorDepth.Depth32Bit };
    internal Rectangle ToggleBounds(ListViewItem item)
    {
        var bounds = item.SubItems[1].Bounds;
        return new Rectangle(bounds.Right - 28, bounds.Top, 28, bounds.Height);
    }
    protected override void WndProc(ref Message m)
    {
        if (RightSideChecks && (m.Msg == 0x201 || m.Msg == 0x203))
        {
            var point = new Point(unchecked((short)m.LParam.ToInt64()), unchecked((short)(m.LParam.ToInt64() >> 16)));
            var item = GetItemAt(point.X, point.Y);
            if (item != null && (ToggleBounds(item).Contains(point) || m.Msg == 0x203))
            {
                // A double-click on the toggle must not undo the first click.
                if (m.Msg == 0x201 || !ToggleBounds(item).Contains(point))
                {
                    Focus(); item.Selected = true; item.Focused = true;
                    item.Checked = !item.Checked; Invalidate(item.Bounds);
                }
                return;
            }
        }
        base.WndProc(ref m);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (RightSideChecks && e.KeyCode == Keys.Space)
        {
            foreach (ListViewItem item in SelectedItems) item.Checked = !item.Checked;
            Invalidate(); e.Handled = true; e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!SystemInformation.HighContrast) SetWindowTheme(Handle, "DarkMode_Explorer", null);
    }
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr hwnd, string theme, string? subId);
    public DarkListView()
    {
        View = View.Details; FullRowSelect = true; HideSelection = false; BorderStyle = BorderStyle.None;
        SmallImageList = _rowHeight;
        OwnerDraw = true; DoubleBuffered = true;
        DrawColumnHeader += (_, e) =>
        {
            using var b = new SolidBrush(UiTheme.Surface); e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text, Font, Rectangle.Inflate(e.Bounds, -8, 0), UiTheme.Muted, (RightSideChecks && e.ColumnIndex == 1 ? TextFormatFlags.Right : TextFormatFlags.Left) | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        DrawItem += (_, _) => { };
        DrawSubItem += (_, e) =>
        {
            if (e.Item == null || e.SubItem == null) return;
            using var b = new SolidBrush(e.Item.Selected ? Color.FromArgb(34, 54, 82) : UiTheme.Surface); e.Graphics.FillRectangle(b, e.Bounds);
            var bounds = Rectangle.Inflate(e.Bounds, -12, 0);
            if (RightSideChecks && e.ColumnIndex == 1)
            {
                var hit = ToggleBounds(e.Item);
                var box = new Rectangle(hit.Left + 7, hit.Top + (hit.Height - 14) / 2, 14, 14);
                using var pen = new Pen(e.Item.Checked ? UiTheme.Accent : UiTheme.Muted);
                e.Graphics.DrawRectangle(pen, box.X, box.Y, box.Width - 1, box.Height - 1);
                if (e.Item.Checked) { box.Inflate(-3, -3); using var fill = new SolidBrush(UiTheme.Accent); e.Graphics.FillRectangle(fill, box); }
            }
            if (CheckBoxes && e.ColumnIndex == 0)
            {
                var box = new Rectangle(bounds.Left, bounds.Top + (bounds.Height - 14) / 2, 14, 14);
                using var pen = new Pen(e.Item.Checked ? UiTheme.Accent : UiTheme.Muted);
                e.Graphics.DrawRectangle(pen, box.X, box.Y, box.Width - 1, box.Height - 1);
                if (e.Item.Checked) { using var fill = new SolidBrush(UiTheme.Accent); box.Inflate(-3, -3); e.Graphics.FillRectangle(fill, box); }
                bounds.X += 24; bounds.Width -= 24;
            }
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font, bounds, UiTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            using var line = new Pen(UiTheme.Border); e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        };
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Columns.Count == 0) return;
        if (RightSideChecks && Columns.Count == 2)
        {
            Columns[1].Width = TextRenderer.MeasureText("Защитить дочерние", Font).Width + 20;
            Columns[0].Width = Math.Max(70, ClientSize.Width - Columns[1].Width);
            return;
        }
        int used = Columns.Cast<ColumnHeader>().Take(Columns.Count - 1).Sum(c => c.Width);
        Columns[Columns.Count - 1].Width = Math.Max(80, ClientSize.Width - used);
    }
    protected override void Dispose(bool disposing) { if (disposing) _rowHeight.Dispose(); base.Dispose(disposing); }
}

internal sealed class TextScrollBar : Control
{
    private readonly TextBox _editor;
    private int VisibleLines => Math.Max(1, _editor.ClientSize.Height / Math.Max(1, _editor.Font.Height));
    private int LineCount => _editor.IsDisposed || !_editor.IsHandleCreated ? 1 : Math.Max(1, (int)SendMessage(_editor.Handle, 0xBA, IntPtr.Zero, IntPtr.Zero));
    private int MaxLine => Math.Max(0, LineCount - VisibleLines);
    public TextScrollBar(TextBox editor)
    {
        _editor = editor; DoubleBuffered = true; TabStop = false;
        editor.TextChanged += (_, _) => Invalidate(); editor.MouseWheel += (_, _) => Invalidate();
        editor.KeyUp += (_, _) => Invalidate(); editor.Resize += (_, _) => Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(UiTheme.Input);
        if (_editor.IsDisposed || MaxLine == 0 || Height < 1) return;
        int thumb = Math.Min(Height, Math.Max(24, (int)((long)Height * VisibleLines / LineCount)));
        int first = (int)SendMessage(_editor.Handle, 0xCE, IntPtr.Zero, IntPtr.Zero);
        int top = (int)Math.Clamp((long)first * (Height - thumb) / MaxLine, 0, Math.Max(0, Height - thumb));
        using var fill = new SolidBrush(UiTheme.Muted);
        using var shape = RoundedButton.Shape(new Rectangle(3, top, Math.Max(2, Width - 6), thumb), 3);
        e.Graphics.FillPath(fill, shape);
    }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { Capture = true; ScrollTo(e.Y); } }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (Capture) ScrollTo(e.Y); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); Capture = false; }
    private void ScrollTo(int y)
    {
        if (_editor.IsDisposed || !_editor.IsHandleCreated) return;
        int target = (int)Math.Clamp((long)y * MaxLine / Math.Max(1, Height), 0, MaxLine);
        int first = (int)SendMessage(_editor.Handle, 0xCE, IntPtr.Zero, IntPtr.Zero);
        SendMessage(_editor.Handle, 0xB6, IntPtr.Zero, (IntPtr)(target - first)); Invalidate();
    }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
}

internal sealed class CornerDiagram : Panel
{
    public CornerDiagram() { DoubleBuffered = true; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); using var pen = new Pen(UiTheme.Border, 2);
        e.Graphics.DrawRectangle(pen, 16, 16, Width - 33, Height - 33);
    }
}
