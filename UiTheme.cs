using System.Drawing;
using System.Windows.Forms;

namespace TheAlarm;

internal static class UiTheme
{
    public static void Apply(Control root)
    {
        bool contrast = SystemInformation.HighContrast;
        foreach (Control control in root.Controls)
        {
            if (contrast)
            {
                control.BackColor = SystemColors.Window;
                control.ForeColor = SystemColors.WindowText;
            }
            if (control is Button button)
            {
                button.FlatStyle = contrast ? FlatStyle.Standard : FlatStyle.Flat;
                button.MinimumSize = new Size(0, 30);
                button.FlatAppearance.BorderColor = Color.FromArgb(100, 140, 165);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 75, 90);
            }
            if (string.IsNullOrEmpty(control.AccessibleName) && !string.IsNullOrEmpty(control.Text) && control is not TextBox)
                control.AccessibleName = control.Text.Replace("&", "");
            Apply(control);
        }
        if (root is Form form)
        {
            form.AutoScaleMode = AutoScaleMode.Dpi;
            if (contrast) { form.BackColor = SystemColors.Window; form.ForeColor = SystemColors.WindowText; }
        }
    }
}
