namespace TheAlarm;

public sealed class PopupForm : ModernForm
{
    private readonly Label _label = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(19), Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    public PopupForm()
    {
        Text = "Напоминание"; ClientSize = new Size(368, 224); MinimumSize = new Size(288, 192);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 53, Padding = new Padding(19, 3, 19, 16) };
        var close = new RoundedButton { Text = "Закрыть", Primary = true, Dock = DockStyle.Fill };
        close.Click += (_, _) => Hide(); footer.Controls.Add(close);
        Controls.Add(_label); Controls.Add(footer);
        FormClosing += (_, e) => { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
        VisibleChanged += (_, _) => { if (!Visible) AlarmAudio.Stop(); };
    }
    public void SetMessage(string message) => _label.Text = message;
}
