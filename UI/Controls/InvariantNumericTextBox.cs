using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NMSE.Core.Utilities;

namespace NMSE.UI.Controls;

/// <summary>
/// A composite control that pairs a locale-aware numeric <see cref="TextBox"/> with
/// up/down spinner buttons, providing the familiar <see cref="NumericUpDown"/>
/// experience while storing its value as a full-precision <c>double</c> (IEEE 754,
/// ~15-17 significant digits).
///
/// <para>The built-in <see cref="NumericUpDown"/> stores its value as <c>decimal</c>,
/// which silently truncates 1-2 significant digits during the
/// <c>double -> decimal -> double</c> round-trip.  This control avoids that loss by
/// keeping the value in <c>double</c> throughout, formatted with the <c>"G17"</c>
/// specifier (17 significant digits, enough to uniquely represent every IEEE 754
/// double).</para>
///
/// <para><b>Spinner behaviour:</b></para>
/// <list type="bullet">
///   <item>Click the ▲ / ▼ buttons, press the Up/Down arrow keys, or scroll the
///         mouse wheel to increment/decrement by <see cref="Increment"/>.</item>
///   <item>Optional <see cref="Minimum"/> and <see cref="Maximum"/> bounds are
///         enforced on every step.</item>
/// </list>
///
/// <para><b>Usage in panels:</b></para>
/// <code>
/// // In Designer.cs:
/// _scaleField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
/// _scaleField.NumericValueChanged += (s, e) => WriteScale();
///
/// // In LoadData:
/// _scaleField.NumericValue = comp.GetDouble("Scale");
///
/// // In WriteScale:
/// if (_scaleField.NumericValue is double val)
///     comp.Set("Scale", val);
/// </code>
/// </summary>

// If you're reading this and wondering why we don't just use
// the built-in NumericUpDown control:
// The built-in NUD control uses a decimal field internally (ugh).
// This causes it to silently truncate 1-2 significant digits during
// the double -> decimal -> double round-trip.
// This control avoids that loss by keeping the value in double throughout,
// formatted with the "G17" specifier (17 significant digits).
// At least we get mouse wheel events out of it I guess.
//
// At cross-platform porting time we shouldn't need to worry about this monstrosity.
//
// If you're intending to modify this control, ask a maintainer if you even should be.
public class InvariantNumericTextBox : UserControl, ISupportInitialize
{
    /// <summary>
    /// The inner text box used for editing the numeric input.
    /// </summary>
    private readonly TextBox _textBox;

    /// <summary>
    /// The spinner panel with up/down buttons.
    /// </summary>
    private readonly SpinnerPanel _spinner;

    /// <summary>
    /// The last committed numeric value, or <c>null</c> when the field is empty or invalid.
    /// </summary>
    private double? _value;

    /// <summary>
    /// Timer for auto-repeat while a spinner button is held down.
    /// </summary>
    private System.Windows.Forms.Timer? _repeatTimer;

    /// <summary>
    /// Current auto-repeat direction: <c>+1</c> for up, <c>-1</c> for down.
    /// </summary>
    private int _repeatDirection;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvariantNumericTextBox"/> control.
    /// </summary>
    public InvariantNumericTextBox()
    {
        _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
        };
        _textBox.Leave += (s, e) => TryCommit();
        _textBox.KeyDown += OnTextBoxKeyDown;
        _textBox.MouseWheel += OnTextBoxMouseWheel;

        _spinner = new SpinnerPanel(this);

        // Wrap in a border that mimics a standard TextBox/NUD
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        BackColor = SystemColors.Window;
        Padding = new Padding(0);

        Controls.Add(_textBox);
        Controls.Add(_spinner);

        Height = _textBox.PreferredHeight + 2; // match standard single-line TextBox height
    }

	// Broken into regions for clarity, it's a bit of a messy class but the sections are
	// logically grouped to help navigate it - and it isn't large, just... special.

	#region Public API (same surface as before + spinner properties)

	/// <summary>
	/// Gets or sets the numeric value.  Setting this updates the displayed text
	/// using invariant culture (dot separator).  Returns <c>null</c> if the text
	/// cannot be parsed as a number.
	///
	/// <para>Setting this property resets <see cref="UserModified"/> to <c>false</c>
	/// so that save logic can distinguish programmatic loads from user edits.</para>
	/// </summary>
	[Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? NumericValue
    {
        get => _value;
        set
        {
            _value = value;
            _userModified = false;
            _textBox.Text = value.HasValue ? NumericParseHelper.FormatDouble(value.Value) : "";
        }
    }

    /// <summary>
    /// Sets the numeric value and its display text directly, without reformatting.
    /// Use this when loading a value from the save file where the original text
    /// representation should be preserved exactly as it appears in the JSON.
    /// </summary>
    /// <param name="value">The parsed double value.</param>
    /// <param name="displayText">The exact text to display (e.g. the original JSON text).</param>
    public void SetValueWithText(double value, string displayText)
    {
        _value = value;
        _userModified = false;
        _textBox.Text = displayText;
    }

    /// <summary>
    /// Returns the current text displayed in the control. This is the text the user
    /// sees, which may be the original save file text or what the user typed.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string DisplayText => _textBox.Text;

    /// <summary>
    /// Indicates whether the value was changed by user interaction (typing, spinner,
    /// or mouse wheel) since the last programmatic <see cref="NumericValue"/> set.
    /// Panels use this to decide whether to write a UI-derived value or preserve the
    /// original raw JSON value.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool UserModified => _userModified;

    /// <summary>
    /// Backing field for <see cref="UserModified"/>.
    /// </summary>
    private bool _userModified;

    /// <summary>
    /// Raised when <see cref="NumericValue"/> changes as a result of user editing
    /// (on focus loss or spinner click).  Not raised when <see cref="NumericValue"/>
    /// is set programmatically - use the property setter directly in that case.
    /// </summary>
    public event EventHandler? NumericValueChanged;

    /// <summary>
    /// The amount added or subtracted on each spinner step.
    /// Defaults to <c>1.0</c>.
    /// </summary>
    [DefaultValue(1.0)]
    public double Increment { get; set; } = 1.0;

    /// <summary>Optional lower bound. <c>null</c> means unbounded.</summary>
    [DefaultValue(null)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? Minimum { get; set; }

    /// <summary>Optional upper bound. <c>null</c> means unbounded.</summary>
    [DefaultValue(null)]
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? Maximum { get; set; }

    /// <summary>
    /// Gets or sets the text displayed in the inner text box.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [AllowNull]
    public override string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value ?? "";
    }

    /// <summary>
    /// Gets or sets the text alignment of the inner text box.
    /// </summary>
    [DefaultValue(HorizontalAlignment.Left)]
    public HorizontalAlignment TextAlign
    {
        get => _textBox.TextAlign;
        set => _textBox.TextAlign = value;
    }

    /// <summary>
    /// Gets or sets whether the text box portion is read-only.
    /// </summary>
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => _textBox.ReadOnly;
        set => _textBox.ReadOnly = value;
    }

    /// <summary>
    /// Attempts to parse the current text as a double, trying the user's locale
    /// first and invariant culture as a fallback.  Returns <c>true</c> if parsing
    /// succeeded and the stored value was updated.
    /// </summary>
    public bool TryCommit()
    {
        string input = _textBox.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            if (_value != null)
            {
                _value = null;
                _userModified = true;
                NumericValueChanged?.Invoke(this, EventArgs.Empty);
            }
            return false;
        }

        if (NumericParseHelper.TryParseDouble(input, out double parsed))
        {
            double old = _value ?? double.NaN;
            _value = parsed;

            // Normalise to invariant format for JSON safety. If the text already
            // uses a dot as the decimal separator (or has no decimal separator at
            // all), it is already invariant-safe and we keep the user's exact text
            // to avoid G17 adding or changing trailing digits. Only reformat when
            // the user entered a locale-specific format (e.g. comma decimal).
            // AllowThousands is included to mirror the style used by
            // NumericParseHelper.TryParseDouble so the invariant check matches
            // the same inputs that the primary parse accepts.
            bool alreadyInvariant = double.TryParse(input,
                System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                System.Globalization.CultureInfo.InvariantCulture, out _);
            _textBox.Text = alreadyInvariant ? input : NumericParseHelper.FormatDouble(parsed);

            if (!old.Equals(parsed))
            {
                _userModified = true;
                NumericValueChanged?.Invoke(this, EventArgs.Empty);
            }
            return true;
        }

        // Input is not a valid number - leave _value unchanged, leave text as-is.
        return false;
    }

    #endregion

    #region Layout & painting

    /// <summary>
    /// Positions the inner text box and spinner within the control.
    /// </summary>
    /// <param name="e">A <see cref="LayoutEventArgs"/> that contains event data.</param>
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        int spinW = SystemInformation.VerticalScrollBarWidth;
        _spinner.SetBounds(Width - spinW, 0, spinW, Height);
        _textBox.SetBounds(1, 1, Width - spinW - 2, Height - 2);
    }

    /// <summary>
    /// Paints the control border to mimic a standard TextBox/NumericUpDown appearance.
    /// </summary>
    /// <param name="e">A <see cref="PaintEventArgs"/> that contains event data.</param>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Draw a single-pixel border that matches the WinForms TextBox/NUD appearance
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle, SystemColors.ControlDark, ButtonBorderStyle.Solid);
    }

    /// <summary>
    /// Focuses the inner text box when the control receives focus.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> that contains event data.</param>
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _textBox.Focus();
    }

    /// <summary>
    /// Synchronizes enabled state with the internal text box and spinner.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> that contains event data.</param>
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _textBox.Enabled = Enabled;
        _spinner.Enabled = Enabled;
        _spinner.Invalidate();
    }

    /// <summary>
    /// Applies font changes to the inner text box.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> that contains event data.</param>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _textBox.Font = Font;
    }

    #endregion

    #region Spinner stepping

    /// <summary>
    /// Increments or decrements the current numeric value by <see cref="Increment"/>.
    /// </summary>
    /// <param name="direction">The spinner direction: <c>+1</c> to increment, <c>-1</c> to decrement.</param>
    internal void Step(int direction)
    {
        // Commit any pending text first
        TryCommit();

        // Use decimal arithmetic on the displayed text so that fractional digits
        // are preserved exactly. Double arithmetic can drift trailing digits,
        // e.g. 45.803646087646487 + 3 = 48.803646087646484 in double, but
        // 48.803646087646487 in decimal - and the user expects the latter.
        string currentText = _textBox.Text.Trim();
        if (decimal.TryParse(currentText, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out decimal decCurrent))
        {
            decimal decNext = decCurrent + direction * (decimal)Increment;
            double nextDouble = (double)decNext;

            if (Minimum.HasValue && nextDouble < Minimum.Value)
            {
                nextDouble = Minimum.Value;
                decNext = (decimal)Minimum.Value;
            }
            if (Maximum.HasValue && nextDouble > Maximum.Value)
            {
                nextDouble = Maximum.Value;
                decNext = (decimal)Maximum.Value;
            }

            double old = _value ?? double.NaN;
            _value = nextDouble;
            _textBox.Text = decNext.ToString(CultureInfo.InvariantCulture);

            if (!old.Equals(nextDouble))
            {
                _userModified = true;
                NumericValueChanged?.Invoke(this, EventArgs.Empty);
            }
            return;
        }

        // Fallback: decimal parse failed (e.g. locale-specific text, extreme values).
        double current = _value ?? 0.0;
        double next = current + direction * Increment;

        if (Minimum.HasValue && next < Minimum.Value) next = Minimum.Value;
        if (Maximum.HasValue && next > Maximum.Value) next = Maximum.Value;

        double oldFallback = _value ?? double.NaN;
        _value = next;
        _textBox.Text = NumericParseHelper.FormatDouble(next);

        if (!oldFallback.Equals(next))
        {
            _userModified = true;
            NumericValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Starts the spinner auto-repeat timer for a held spinner button.
    /// </summary>
    /// <param name="direction">The repeat direction: <c>+1</c> for up, <c>-1</c> for down.</param>
    internal void StartAutoRepeat(int direction)
    {
        _repeatDirection = direction;
        _repeatTimer?.Dispose();
        _repeatTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _repeatTimer.Tick += (s, e) => Step(_repeatDirection);
        _repeatTimer.Start();
    }

    /// <summary>
    /// Stops the spinner auto-repeat timer and clears its resources.
    /// </summary>
    internal void StopAutoRepeat()
    {
        _repeatTimer?.Stop();
        _repeatTimer?.Dispose();
        _repeatTimer = null;
    }

    #endregion

    #region Keyboard & mouse-wheel on text box

    /// <summary>
    /// Handles Up/Down arrow key presses in the text box and updates the numeric value.
    /// </summary>
    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up) { Step(1); e.Handled = true; }
        else if (e.KeyCode == Keys.Down) { Step(-1); e.Handled = true; }
    }

    /// <summary>
    /// Handles mouse wheel scrolling over the text box and steps the numeric value.
    /// </summary>
    private void OnTextBoxMouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta > 0) Step(1);
        else if (e.Delta < 0) Step(-1);
        ((HandledMouseEventArgs)e).Handled = true;
    }

    #endregion

    #region ISupportInitialize (required by Designer-generated code)

    /// <inheritdoc />
    public void BeginInit() { }

    /// <inheritdoc />
    public void EndInit() { }

    #endregion

    /// <summary>
    /// Releases managed resources used by the control.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release managed resources; otherwise, <see langword="false"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _repeatTimer?.Dispose();
            _repeatTimer = null;
        }
        base.Dispose(disposing);
    }

    #region SpinnerPanel (up/down buttons drawn as a single child control)

    /// <summary>
    /// A lightweight panel that draws two triangular arrow buttons stacked vertically,
    /// mimicking the spinner portion of a standard <see cref="NumericUpDown"/>.
    /// </summary>
    private sealed class SpinnerPanel : Control
    {
        /// <summary>
        /// The owning <see cref="InvariantNumericTextBox"/> instance.
        /// </summary>
        private readonly InvariantNumericTextBox _owner;

        /// <summary>
        /// Whether the up button is currently pressed.
        /// </summary>
        private bool _upPressed;

        /// <summary>
        /// Whether the down button is currently pressed.
        /// </summary>
        private bool _downPressed;

        /// <summary>
        /// Whether the mouse is currently over the up button.
        /// </summary>
        private bool _upHot;

        /// <summary>
        /// Whether the mouse is currently over the down button.
        /// </summary>
        private bool _downHot;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpinnerPanel"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="InvariantNumericTextBox"/>.</param>
        public SpinnerPanel(InvariantNumericTextBox owner)
        {
            _owner = owner;
            SetStyle(ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.Selectable, true);
            // Selectable = true so the mouse capture works but we immediately
            // forward focus to the text box.
            TabStop = false;
            Dock = DockStyle.Right;
            Width = SystemInformation.VerticalScrollBarWidth;
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Paints the up/down spinner buttons.
        /// </summary>
        /// <param name="e">A <see cref="PaintEventArgs"/> that contains event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            int half = Height / 2;
            var upRect = new Rectangle(0, 0, Width, half);
            var downRect = new Rectangle(0, half, Width, Height - half);

            // Compute arrow size once from the smaller button so both
            // glyphs are guaranteed pixel-identical regardless of odd heights.
            int btnH = Math.Min(upRect.Height, downRect.Height);
            int arrowSz = Math.Max(3, Math.Min(Width, btnH) / 4);

            // Anti-alias so sub-pixel centering is smooth and both arrows
            // rasterise identically (integer FillPolygon can differ by
            // orientation due to annoying scan-line edge rules).
            var prevSmoothing = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            DrawButton(g, upRect, up: true, _upPressed, _upHot, arrowSz);
            DrawButton(g, downRect, up: false, _downPressed, _downHot, arrowSz);

            g.SmoothingMode = prevSmoothing;
        }

        /// <summary>
        /// Draws one of the spinner buttons, including its background, border, and arrow glyph.
        /// </summary>
        /// <param name="g">The graphics surface used for drawing.</param>
        /// <param name="rect">The bounds of the button.</param>
        /// <param name="up">Whether this is the up button.</param>
        /// <param name="pressed">Whether the button is currently pressed.</param>
        /// <param name="hot">Whether the mouse is currently over the button.</param>
        /// <param name="sz">The desired arrow size.</param>
        private void DrawButton(Graphics g, Rectangle rect, bool up, bool pressed, bool hot, int sz)
        {
            // Background (fill without anti-alias for crisp rectangles)
            var prevSmoothing = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            Color bg = !Enabled ? SystemColors.Control
                     : pressed ? SystemColors.ControlDark
                     : hot ? SystemColors.ControlLight
                     : SystemColors.Control;
            using var brush = new SolidBrush(bg);
            g.FillRectangle(brush, rect);

            // Border
            ControlPaint.DrawBorder(g, rect, SystemColors.ControlDark, ButtonBorderStyle.Solid);
            g.SmoothingMode = prevSmoothing;

            // Arrow glyph - equilateral(ish) triangle centred in the button
            // using PointF for sub-pixel precision so both orientations
            // produce visually identical results.
            Color arrowColor = Enabled ? SystemColors.ControlText : SystemColors.GrayText;
            using var arrowBrush = new SolidBrush(arrowColor);
            float cx = rect.X + rect.Width  / 2f;
            float cy = rect.Y + rect.Height / 2f;
            float halfW = sz;          // horizontal half-extent
            float halfH = sz * 0.75f;  // vertical half-extent

            PointF[] tri;
            if (up)
                tri = new[] { new PointF(cx, cy - halfH), new PointF(cx - halfW, cy + halfH), new PointF(cx + halfW, cy + halfH) };
            else
                tri = new[] { new PointF(cx, cy + halfH), new PointF(cx - halfW, cy - halfH), new PointF(cx + halfW, cy - halfH) };

            g.FillPolygon(arrowBrush, tri);
        }

        /// <summary>
        /// Returns whether the specified point lies within the upper spinner button.
        /// </summary>
        /// <param name="p">The point to test.</param>
        /// <returns><see langword="true"/> when the point is in the upper half; otherwise <see langword="false"/>.</returns>
        private bool IsUp(Point p) => p.Y < Height / 2;

        /// <summary>
        /// Handles mouse-down events for the spinner and initiates a step and auto-repeat.
        /// </summary>
        /// <param name="e">A <see cref="MouseEventArgs"/> that contains event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            bool up = IsUp(e.Location);
            _upPressed = up;
            _downPressed = !up;
            Invalidate();
            int dir = up ? 1 : -1;
            _owner.Step(dir);
            _owner.StartAutoRepeat(dir);
            _owner._textBox.Focus();
        }

        /// <summary>
        /// Handles mouse-up events for the spinner and stops auto-repeat.
        /// </summary>
        /// <param name="e">A <see cref="MouseEventArgs"/> that contains event data.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _upPressed = false;
            _downPressed = false;
            _owner.StopAutoRepeat();
            Invalidate();
        }

        /// <summary>
        /// Handles mouse movement over the spinner to update hover state.
        /// </summary>
        /// <param name="e">A <see cref="MouseEventArgs"/> that contains event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool up = IsUp(e.Location);
            bool oldUpHot = _upHot, oldDownHot = _downHot;
            _upHot = up;
            _downHot = !up;
            if (_upHot != oldUpHot || _downHot != oldDownHot)
                Invalidate();
        }

        /// <summary>
        /// Clears hover state when the mouse leaves the spinner area.
        /// </summary>
        /// <param name="e">An <see cref="EventArgs"/> that contains event data.</param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _upHot = false;
            _downHot = false;
            Invalidate();
        }
    }

    #endregion
}
