using System.Globalization;

namespace NMSE.Core.Utilities;

/// <summary>
/// Culture-safe numeric parsing and formatting helpers.
///
/// <para>Every method in this class produces values that are safe to serialise into
/// JSON (invariant <c>.</c> decimal separator).  The parsing methods accept both the
/// user's locale-specific format <em>and</em> the invariant format so that a German
/// user can type <c>1,5</c> and it resolves to <c>1.5</c>.</para>
///
/// <para>UI controls should prefer <c>InvariantNumericTextBox</c> which calls these
/// helpers automatically.  For DataGridView cells or other ad-hoc parsing, call
/// <see cref="TryParseDouble"/> directly.</para>
/// </summary>
public static class NumericParseHelper
{
    /// <summary>
    /// Attempts to parse <paramref name="input"/> as a double, handling both
    /// locale-specific and invariant decimal separators.
    ///
    /// <para><b>Parse strategy:</b></para>
    /// <list type="number">
    ///   <item>If the input contains a <c>.</c>, try invariant culture first
    ///         (since <c>.</c> is always the invariant decimal separator).</item>
    ///   <item>Otherwise try the user's current culture (so <c>1,5</c> works in de-DE).</item>
    ///   <item>Fall back to the other culture if the first attempt fails.</item>
    /// </list>
    ///
    /// This avoids the ambiguity where <c>23.7</c> under de-DE would be interpreted
    /// as 237 (dot = thousands separator) if we tried CurrentCulture first.
    /// </summary>
    /// <param name="input">The raw text entered by the user.</param>
    /// <param name="result">The parsed value on success; <c>0</c> on failure.</param>
    /// <returns><c>true</c> if parsing succeeded.</returns>
    public static bool TryParseDouble(string? input, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim();

        const NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;

        // If the text contains a dot, try invariant first to avoid misinterpreting
        // the dot as a thousands separator in comma-decimal locales.
        if (trimmed.Contains('.'))
        {
            if (double.TryParse(trimmed, style, CultureInfo.InvariantCulture, out result))
                return true;
            // Fall back to current culture
            if (double.TryParse(trimmed, style, CultureInfo.CurrentCulture, out result))
                return true;
        }
        else
        {
            // No dot present - try user's locale first (e.g. "1,5" -> 1.5 in de-DE).
            if (double.TryParse(trimmed, style, CultureInfo.CurrentCulture, out result))
                return true;
            // Fall back to invariant
            if (double.TryParse(trimmed, style, CultureInfo.InvariantCulture, out result))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Formats a double using invariant culture with full 17-significant-digit
    /// precision ("G17"), producing a string that is safe for JSON serialisation
    /// (always uses <c>.</c> as the decimal separator) and preserves full IEEE 754
    /// fidelity.  G17 guarantees that every distinct <c>double</c> value produces
    /// a distinct string and always round-trips back to the same bits.
    /// </summary>
    public static string FormatDouble(double value)
        => value.ToString("G17", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a double with the specified format string using invariant culture.
    /// </summary>
    public static string FormatDouble(double value, string format)
        => value.ToString(format, CultureInfo.InvariantCulture);
}
