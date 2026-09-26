using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace TheAlarm;

internal enum ButtonIcon { None, Close, Minimize, Plus }

internal class RoundedButton : Button
{
    public bool Primary { get; set; }
    public ButtonIcon Icon { get; set; }
    private bool _hover;
    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    public RoundedButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Height = 29; Padding = new Padding(8, 0, 8, 0); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (SystemInformation.HighContrast) { base.OnPaint(e); return; }
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? BackColor);
        using var shape = Shape(new Rectangle(1, 1, Width - 3, Height - 3), 7);
        using var fill = new SolidBrush(Primary ? (_hover ? Color.FromArgb(37, 99, 235) : UiTheme.Accent) : (_hover ? UiTheme.Input : UiTheme.Surface));
        e.Graphics.FillPath(fill, shape);
        using var border = new Pen(Focused ? UiTheme.Accent : UiTheme.Border);
        e.Graphics.DrawPath(border, shape);
        if (Icon != ButtonIcon.None)
        {
            float x = (Width - 1) / 2f, y = (Height - 1) / 2f;
            using var pen = new Pen(Enabled ? UiTheme.Text : UiTheme.Muted, 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            if (Icon == ButtonIcon.Close) { e.Graphics.DrawLine(pen, x - 4, y - 4, x + 4, y + 4); e.Graphics.DrawLine(pen, x + 4, y - 4, x - 4, y + 4); }
            else { e.Graphics.DrawLine(pen, x - 5, y, x + 5, y); if (Icon == ButtonIcon.Plus) e.Graphics.DrawLine(pen, x, y - 5, x, y + 5); }
            return;
        }
        var textBounds = new Rectangle(Padding.Left, 0, Math.Max(1, Width - Padding.Horizontal), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, Enabled ? ForeColor : SystemColors.GrayText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }
    internal static GraphicsPath Shape(Rectangle r, int radius)
    {
        int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        if (d <= 0) return path;
        path.AddArc(r.Left, r.Top, d, d, 180, 90); path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
    }
}

internal sealed class ToggleSwitch : CheckBox
{
    public ToggleSwitch() { AutoSize = false; Size = new Size(116, 24); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (SystemInformation.HighContrast) { base.OnPaint(e); return; }
        e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        bool iconOnly = string.IsNullOrEmpty(Text);
        if (iconOnly)
        {
            var circle = new Rectangle(2, 2, Math.Min(Width, Height) - 5, Math.Min(Width, Height) - 5);
            using var background = new SolidBrush(UiTheme.Surface);
            using var outline = new Pen(Checked ? UiTheme.Accent : UiTheme.Muted, 2);
            e.Graphics.FillEllipse(background, circle); e.Graphics.DrawEllipse(outline, circle);
            circle.Inflate(-8, -8);
            using var ball = new SolidBrush(Checked ? UiTheme.Accent : UiTheme.Border);
            e.Graphics.FillEllipse(ball, circle);
            if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
            return;
        }
        var track = new Rectangle(1, (Height - 16) / 2, 29, 16);
        using var path = RoundedButton.Shape(track, track.Height / 2);
        using var fill = new SolidBrush(Checked ? UiTheme.Accent : Color.FromArgb(64, 64, 64));
        e.Graphics.FillPath(fill, path);
        int knob = track.Height - 6;
        e.Graphics.FillEllipse(Brushes.White, Checked ? track.Right - knob - 3 : track.Left + 3, track.Top + 3, knob, knob);
        if (!iconOnly) TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(36, 0, Width - 36, Height), ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
    }
}

internal sealed class MacroCard : Panel
{
    public MacroCard() { DoubleBuffered = true; }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = RoundedButton.Shape(new Rectangle(1, 1, Width - 3, Height - 3), 12);
        using var fill = new SolidBrush(BackColor); e.Graphics.FillPath(fill, shape);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = RoundedButton.Shape(new Rectangle(1, 1, Width - 3, Height - 3), 12);
        using var border = new Pen(UiTheme.Border); e.Graphics.DrawPath(border, shape);
    }
}
