namespace TheAlarm;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(10, 10, 10);
    public static readonly Color Surface = Color.FromArgb(23, 23, 23);
    public static readonly Color Input = Color.FromArgb(30, 30, 30);
    public static readonly Color Border = Color.FromArgb(46, 46, 46);
    public static readonly Color Text = Color.FromArgb(250, 250, 250);
    public static readonly Color Muted = Color.FromArgb(163, 163, 163);
    public static readonly Color Accent = Color.FromArgb(20, 71, 230);
    public static void Apply(Control root)
    {
        foreach (Control control in root.Controls)
        {
            control.BackColor = control is ListView or MacroCard ? Surface : control is InputBox or TextBoxBase or ListControl ? Input : root.BackColor;
            control.ForeColor = control is Label && control.Font.Size <= 9 ? Muted : Text;
            if (control is TextBoxBase text) text.BorderStyle = BorderStyle.None;
            if (control is ListBox list) list.BorderStyle = BorderStyle.None;
            if (SystemInformation.HighContrast) { control.BackColor = SystemColors.Window; control.ForeColor = SystemColors.WindowText; }
            if (string.IsNullOrEmpty(control.AccessibleName) && !string.IsNullOrEmpty(control.Text) && control is not TextBox)
                control.AccessibleName = control.Text.Replace("&", "");
            Apply(control);
        }
    }
    public static Label Heading(string text) => new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) };
    public static Label Caption(string text) => new() { Text = text, AutoSize = true, ForeColor = Muted, Anchor = AnchorStyles.Left | AnchorStyles.Bottom, Margin = new Padding(0, 0, 0, 6) };
}
