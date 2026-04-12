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
    private readonly Label _loadingLabel;
    private readonly GreenProgressBar _progressBar;

    internal SplashForm()
    {
        SuspendLayout();

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(420, 170);
        BackColor = Color.FromArgb(30, 30, 30);
        ShowInTaskbar = true;
        TopMost = true;
        ShowIcon = true;
        Text = MainFormResources.AppName;

        // Load the same app icon used by the main window so the taskbar entry
        // has a valid icon immediately while the startup splash is visible.
        try
        {
            Icon? appIcon = MainFormResources.LoadAppIcon();
            if (appIcon != null)
            {
                Icon = appIcon;
            }
            else
            {
                string exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Icon? exeIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (exeIcon != null)
                        Icon = exeIcon;
                }
            }

            // Ensure the window handle is created so Windows picks up the icon.
            var handle = Handle;
        }
        catch
        {
            // Non-critical. Works fine without an icon.
        }

        var titleLabel = new Label
        {
            Text = MainFormResources.AppName,
            ForeColor = Color.White,
            Font = _titleFont,
            TextAlign = ContentAlignment.BottomCenter,
            Dock = DockStyle.Top,
            Height = 70,
        };

        _loadingLabel = new Label
        {
            Text = "Loading databases...",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = _loadingFont,
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(0, 8, 0, 0),
        };

        _progressBar = new GreenProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = 18,
            Dock = DockStyle.Fill,
        };

        // Use a panel to add horizontal margins around the progress bar
        // since the bar ignores its own Margin when docked.
        var barPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            Padding = new Padding(20, 0, 20, 12),
        };
        barPanel.Controls.Add(_progressBar);

        Controls.Add(barPanel);
        Controls.Add(_loadingLabel);
        Controls.Add(titleLabel);

        ResumeLayout(false);
    }

    /// <summary>
    /// Update the progress bar value (0-100) and optionally the status text.
    /// Safe to call during synchronous startup on the UI thread.
    /// </summary>
    internal void SetProgress(int percent, string? statusText = null)
    {
        _progressBar.Value = Math.Clamp(percent, 0, 100);
        if (statusText != null)
            _loadingLabel.Text = statusText;
        Update();
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

    /// <summary>
    /// Owner-drawn progress bar that renders a green fill on a dark track,
    /// bypassing the Windows visual-styles theme which ignores ForeColor.
    /// </summary>
    private sealed class GreenProgressBar : Control
    {
        private static readonly Color TrackColor = Color.FromArgb(50, 50, 50);
        private static readonly Color BarColor = Color.FromArgb(60, 180, 75);

        private int _minimum;
        private int _maximum = 100;
        private int _value;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal int Minimum { get => _minimum; set { _minimum = value; Invalidate(); } }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal int Maximum { get => _maximum; set { _maximum = value; Invalidate(); } }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        internal int Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, _minimum, _maximum);
                Invalidate();
            }
        }

        public GreenProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            // Dark track background
            using (var trackBrush = new SolidBrush(TrackColor))
                g.FillRectangle(trackBrush, 0, 0, Width, Height);

            // Green fill proportional to value
            int range = _maximum - _minimum;
            if (range > 0 && _value > _minimum)
            {
                float fraction = (float)(_value - _minimum) / range;
                int fillWidth = (int)(Width * fraction);
                using var barBrush = new SolidBrush(BarColor);
                g.FillRectangle(barBrush, 0, 0, fillWidth, Height);
            }
        }
    }
}
