namespace NMSE.UI;

/// <summary>
/// Lightweight splash screen shown during application startup while the
/// main form performs heavy initialisation (database loading, icon
/// preloading, panel construction).  Closed by the main form's Shown
/// handler once the main window is fully rendered.
/// </summary>
internal sealed class SplashForm : Form
{
    private readonly Font _titleFont = new("Segoe UI", 13f, FontStyle.Bold);
    private readonly Font _loadingFont = new("Segoe UI", 10f);

    internal SplashForm()
    {
        SuspendLayout();

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(420, 160);
        BackColor = Color.FromArgb(30, 30, 30);
        ShowInTaskbar = true;
        TopMost = true;
        ShowIcon = true;

        // Try to load the application icon for the taskbar entry.
        try
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, MainFormResources.IconPath);
            if (File.Exists(icoPath))
            {
                byte[] bytes = File.ReadAllBytes(icoPath);
                using var ms = new MemoryStream(bytes);
                Icon = new Icon(ms);
            }
        }
        catch
        {
            // Non-critical – splash works fine without an icon.
        }

        var titleLabel = new Label
        {
            Text = MainFormResources.AppName,
            ForeColor = Color.White,
            Font = _titleFont,
            TextAlign = ContentAlignment.BottomCenter,
            Dock = DockStyle.Top,
            Height = 80,
        };

        var loadingLabel = new Label
        {
            Text = "Loading databases\u2026",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = _loadingFont,
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0),
        };

        Controls.Add(loadingLabel);
        Controls.Add(titleLabel);

        ResumeLayout(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleFont.Dispose();
            _loadingFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
