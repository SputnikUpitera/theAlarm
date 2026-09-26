using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace TheAlarm;

public class ModernForm : Form
{
    private readonly Label _title;
    public ModernForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;
        BackColor = UiTheme.Background; ForeColor = UiTheme.Text;
        Font = new Font("Segoe UI", 9, FontStyle.Bold);
        Padding = new Padding(1, 46, 1, 1);
        StartPosition = FormStartPosition.Manual;
        var caption = new Panel { Left = 1, Top = 1, Height = 44, Width = ClientSize.Width - 2, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = UiTheme.Background };
        _title = new Label { AutoSize = false, Left = 19, Top = 0, Height = 43, Width = 320, TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiTheme.Muted };
        var hide = new RoundedButton { Icon = ButtonIcon.Minimize, Size = new Size(29, 26), Top = 9, Left = caption.Width - 72, Anchor = AnchorStyles.Top | AnchorStyles.Right, AccessibleName = "Свернуть" };
        var close = new RoundedButton { Icon = ButtonIcon.Close, Size = new Size(29, 26), Top = 9, Left = caption.Width - 38, Anchor = AnchorStyles.Top | AnchorStyles.Right, AccessibleName = "Закрыть" };
        hide.Click += (_, _) => { if (Modal) WindowState = FormWindowState.Minimized; else Hide(); };
        close.Click += (_, _) => Close();
        hide.TabStop = false; close.TabStop = false;
        MouseEventHandler drag = (_, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); } };
        caption.MouseDown += drag; _title.MouseDown += drag;
        caption.Controls.AddRange(new Control[] { _title, hide, close });
        Controls.Add(caption);
    }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); if (_title != null) _title.Text = Text; }
    protected override void SetVisibleCore(bool value)
    {
        if (value && !Visible)
        {
            // Finish layout before Windows makes the HWND visible.
            UiTheme.Apply(this);
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
            UpdateShape();
        }
        base.SetVisibleCore(value);
    }
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); UpdateShape(); }
    private void UpdateShape()
    {
        if (Width < 24 || Height < 24) return;
        using var path = RoundedButton.Shape(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        var old = Region; Region = new Region(path); old?.Dispose();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        // The window has a uniform surface; only interior cards have outlines.
    }
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != 0x84 || WindowState != FormWindowState.Normal) return;
        var p = PointToClient(new Point(unchecked((short)m.LParam.ToInt64()), unchecked((short)(m.LParam.ToInt64() >> 16))));
        bool left = p.X < 6, right = p.X >= ClientSize.Width - 6, top = p.Y < 6, bottom = p.Y >= ClientSize.Height - 6;
        int hit = top && left ? 13 : top && right ? 14 : bottom && left ? 16 : bottom && right ? 17 : left ? 10 : right ? 11 : top ? 12 : bottom ? 15 : 0;
        if (hit != 0) m.Result = (IntPtr)hit;
    }
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
}
