using NMSE.Core;

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

        var palette = ThemeColors.Get(
            ThemeManager.Effective == AppTheme.Dark ? "Dark" : "Light");

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(420, 190);
        BackColor = palette.Background;
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

        // DPI scaling - handle is created above so DeviceDpi is valid.
        float dpiScale = (float)DeviceDpi / 96f;

        Size = new Size(
            (int)(420f * dpiScale + 0.5f),
            (int)(190f * dpiScale + 0.5f)
        );

        var titleLabel = new Label
        {
            Text = MainFormResources.AppName,
            ForeColor = palette.ForegroundText,
            Font = _titleFont,
            TextAlign = ContentAlignment.BottomCenter,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = (int)(80f * dpiScale + 0.5f),
        };

        _loadingLabel = new Label
        {
            Text = "Loading databases...",
            ForeColor = palette.SecondaryText,
            Font = _loadingFont,
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = (int)(36f * dpiScale + 0.5f),
            Padding = new Padding(0, (int)(8f * dpiScale + 0.5f), 0, 0),
        };

        _progressBar = new GreenProgressBar(palette.GridLineColor, palette.SuccessGreen)
        {
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Height = (int)(18f * dpiScale + 0.5f),
            Dock = DockStyle.Fill,
        };

        // Use a panel to add horizontal margins around the progress bar
        // since the bar ignores its own Margin when docked.
        var barPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            BackColor = palette.Background,
            Height = (int)(30f * dpiScale + 0.5f),
            Padding = new Padding(
                (int)(20 * dpiScale + 0.5f), 0,
                (int)(20 * dpiScale + 0.5f),
                (int)(12 * dpiScale + 0.5f)
            ),
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
        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => SetProgress(percent, statusText)));
            }
            catch (InvalidOperationException)
            {
                // The splash thread may already have exited during shutdown.
            }
            return;
        }

        _progressBar.Value = Math.Clamp(percent, 0, 100);
        if (statusText != null)
            _loadingLabel.Text = statusText;
        Update();
    }

    /// <summary>
    /// Closes the splash from the main thread. The form belongs to the splash
    /// thread, so the close must be marshalled to that thread.
    /// </summary>
    internal void CloseFromOwner()
    {
        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(Close));
            }
            catch (InvalidOperationException)
            {
                // The splash thread may already have exited during shutdown.
            }
            return;
        }

        Close();
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
    /// Owner-drawn progress bar that renders a green fill on a theme-coloured
    /// track, bypassing the Windows visual-styles theme which ignores ForeColor.
    /// </summary>
    private sealed class GreenProgressBar : Control
    {
        private readonly Color _trackColor;
        private readonly Color _barColor;

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

        internal GreenProgressBar(Color trackColor, Color barColor)
        {
            _trackColor = trackColor;
            _barColor = barColor;

            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var trackBrush = new SolidBrush(_trackColor))
                g.FillRectangle(trackBrush, 0, 0, Width, Height);

            // Fill proportional to value
            int range = _maximum - _minimum;
            if (range > 0 && _value > _minimum)
            {
                float fraction = (float)(_value - _minimum) / range;
                int fillWidth = (int)(Width * fraction);
                using var barBrush = new SolidBrush(_barColor);
                g.FillRectangle(barBrush, 0, 0, fillWidth, Height);
            }
        }
    }
}
