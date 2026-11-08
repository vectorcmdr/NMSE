using NMSE.UI.Controls;

namespace NMSE.UI.Util;

/// <summary>Declares the design DPI for the application's hand-built control trees.</summary>
internal static class DpiScaling
{
    /// <summary>
    /// Call once after constructing a form's controls, before showing it.
    /// Without an explicit baseline WinForms treats the current display DPI as
    /// the design DPI: text scales but literal control sizes remain at 100%.
    /// Each UserControl is an independent autoscaling boundary, so configuring
    /// only the containing Form is not sufficient.
    /// </summary>
    internal static void Initialize(Form form)
    {
        Suspend(form);
        Configure(form);
        Resume(form);
    }

    private static void Suspend(Control control)
    {
        control.SuspendLayout();
        foreach (Control child in control.Controls)
            Suspend(child);
    }

    private static void Configure(Control control)
    {
        // Designer-generated single-column fill layouts sometimes declare an
        // absolute 20px column. Its contents can then force the column wider
        // than the window after scaling, clipping the entire right-hand side.
        if (control is TableLayoutPanel { Dock: DockStyle.Fill, ColumnCount: 1 } table)
        {
            table.ColumnStyles.Clear();
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        }

        // Numeric editors lay out their native text box and spinner themselves;
        // their outer bounds are scaled by the enclosing panel. SplitContainer
        // must likewise inherit its enclosing container's scaling.
        if (control is Form || control is UserControl and not InvariantNumericTextBox)
        {
            var container = (ContainerControl)control;
            container.AutoScaleMode = AutoScaleMode.Dpi;
            container.AutoScaleDimensions = new SizeF(96, 96);
        }
        foreach (Control child in control.Controls)
            Configure(child);
    }

    private static void Resume(Control control)
    {
        control.ResumeLayout(true);
        foreach (Control child in control.Controls)
            Resume(child);
    }
}
