namespace NMSE.UI.Util;

/// <summary>
/// Cross-platform painting suspension for WinForms controls.
/// Hides the control via <see cref="Control.Visible"/> and suspends layout
/// to completely suppress intermediate redraws during batch updates.
/// Unlike <c>SuspendLayout</c> alone (which only defers layout, not painting),
/// hiding the control prevents all <c>WM_PAINT</c> processing on the control and
/// its entire child tree.  <see cref="Resume"/> re-shows the control, triggers
/// layout, and forces one synchronous repaint so the final state appears
/// atomically.
/// <para>
/// Note: toggling <see cref="Control.Visible"/> on a control that already
/// has a window handle simply calls <c>ShowWindow(SW_HIDE/SW_SHOW)</c> - it
/// does <b>not</b> destroy or recreate native handles in the subtree.
/// For controls whose handles have not yet been created, setting
/// <c>Visible = false</c> merely sets a flag and no handles are affected.
/// This is safe for the future cross-platform migration (Avalonia, etc.).
/// </para>
/// Call <see cref="Suspend"/> before a batch update and <see cref="Resume"/>
/// afterwards.
/// </summary>
internal static class RedrawHelper
{
    /// <summary>
    /// Hides the control and suspends layout logic.
    /// All painting and layout is suppressed until <see cref="Resume"/> is called.
    /// </summary>
    public static void Suspend(Control control)
    {
        control.Visible = false;
        control.SuspendLayout();
    }

    /// <summary>
    /// Resumes layout on the control, re-shows it, and triggers a full
    /// synchronous repaint of the control and all its children so the
    /// final state appears atomically.
    /// </summary>
    public static void Resume(Control control)
    {
        control.ResumeLayout(true);
        control.Visible = true;
        control.Invalidate(true);
        control.Update();
    }
}
