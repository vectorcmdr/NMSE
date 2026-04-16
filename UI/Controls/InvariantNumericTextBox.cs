using System.ComponentModel;
using System.Globalization;
using NMSE.Core.Utilities;

namespace NMSE.UI.Controls;

/// <summary>
/// A TextBox subclass that accepts locale-aware decimal input and always stores
/// the value as an invariant-culture double. This is the primary safeguard
/// against locale-dependent decimal separators corrupting JSON save data.
/// Added because it avoids future regression bugs where a TextBox field is added.
///
/// <para><b>How it works:</b></para>
/// <list type="bullet">
///   <item>On <see cref="OnLeave"/>: uses <see cref="NumericParseHelper.TryParseDouble"/>
///         to parse the user's text (tries the user's locale first, then invariant).
///         The parsed value is stored in <see cref="NumericValue"/>.</item>
///   <item>When reading data from a save file, set <see cref="NumericValue"/> directly.
///         The control displays the value using the invariant format (<c>.</c> separator)
///         so the displayed text always round-trips safely through JSON.</item>
///   <item>When writing data back to JSON, read <see cref="NumericValue"/> — it is
///         always a culture-neutral <c>double?</c>.</item>
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
public class InvariantNumericTextBox : TextBox
{
    private double? _value;
    private bool _suppressTextChanged;

    /// <summary>
    /// Gets or sets the numeric value.  Setting this updates the displayed text
    /// using invariant culture (dot separator).  Returns <c>null</c> if the text
    /// cannot be parsed as a number.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public double? NumericValue
    {
        get => _value;
        set
        {
            _value = value;
            _suppressTextChanged = true;
            Text = value.HasValue ? NumericParseHelper.FormatDouble(value.Value) : "";
            _suppressTextChanged = false;
        }
    }

    /// <summary>
    /// Raised when <see cref="NumericValue"/> changes as a result of user editing
    /// (on focus loss).  Not raised when <see cref="NumericValue"/> is set
    /// programmatically — use the property setter directly in that case.
    /// </summary>
    public event EventHandler? NumericValueChanged;

    /// <summary>
    /// Attempts to parse the current text as a double, trying the user's locale
    /// first and invariant culture as a fallback.  Returns <c>true</c> if parsing
    /// succeeded and the stored value was updated.
    /// </summary>
    public bool TryCommit()
    {
        string input = Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            if (_value != null)
            {
                _value = null;
                NumericValueChanged?.Invoke(this, EventArgs.Empty);
            }
            return false;
        }

        if (NumericParseHelper.TryParseDouble(input, out double parsed))
        {
            double old = _value ?? double.NaN;
            _value = parsed;

            // Re-display in invariant format so the stored text always round-trips.
            _suppressTextChanged = true;
            Text = NumericParseHelper.FormatDouble(parsed);
            _suppressTextChanged = false;

            if (!old.Equals(parsed))
                NumericValueChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // Input is not a valid number — leave _value unchanged, leave text as-is.
        return false;
    }

    /// <summary>
    /// On focus loss, commit the user's text to the internal numeric value.
    /// </summary>
    protected override void OnLeave(EventArgs e)
    {
        TryCommit();
        base.OnLeave(e);
    }

    /// <summary>
    /// Suppresses spurious change notifications when we programmatically
    /// update Text from within <see cref="TryCommit"/> or the property setter.
    /// </summary>
    protected override void OnTextChanged(EventArgs e)
    {
        if (!_suppressTextChanged)
            base.OnTextChanged(e);
    }
}
