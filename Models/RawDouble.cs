namespace NMSE.Models;

/// <summary>
/// A double value that preserves its original JSON text representation.
/// When a save file is parsed, floating-point numbers are stored as <see cref="RawDouble"/>
/// so that serialization can reproduce the exact original text rather than applying
/// <see cref="double.ToString(string)"/> which may produce a different (but numerically
/// equivalent) representation.
///
/// For example, the game may write <c>0.30000001192092898</c> but .NET's "G17" format
/// for the same IEEE 754 double produces <c>0.30000001192092896</c>. Both parse to the
/// same bits, but the text difference causes unnecessary diffs.
/// </summary>
public readonly struct RawDouble
{
    /// <summary>The parsed IEEE 754 double value.</summary>
    public readonly double Value;

    /// <summary>The original JSON text (e.g., "0.30000001192092898").</summary>
    public readonly string Text;

    public RawDouble(double value, string text)
    {
        Value = value;
        Text = text;
    }

    /// <summary>Implicit conversion to <see cref="double"/> for arithmetic and comparisons.</summary>
    public static implicit operator double(RawDouble rd) => rd.Value;

    /// <summary>
    /// Determines whether <paramref name="text"/> is a syntactically valid JSON number.
    /// This is stricter than <see cref="double.TryParse(string, out double)"/>: it
    /// accepts only the JSON number grammar and rejects leading <c>+</c>, a leading or
    /// trailing decimal point, thousands separators, surrounding whitespace, and the
    /// non-finite tokens <c>NaN</c> and <c>Infinity</c>.
    /// </summary>
    /// <param name="text">The candidate JSON number text.</param>
    /// <returns><c>true</c> when the text can be written verbatim into JSON.</returns>
    public static bool IsValidJsonNumber(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        ReadOnlySpan<char> span = text.AsSpan();
        int i = 0;

        if (span[i] == '-')
        {
            i++;
            if (i == span.Length)
                return false;
        }

        if (span[i] == '0')
        {
            i++;
        }
        else if (span[i] >= '1' && span[i] <= '9')
        {
            i++;
            while (i < span.Length && span[i] >= '0' && span[i] <= '9')
                i++;
        }
        else
        {
            return false;
        }

        if (i < span.Length && span[i] == '.')
        {
            i++;
            if (i == span.Length || span[i] < '0' || span[i] > '9')
                return false;
            while (i < span.Length && span[i] >= '0' && span[i] <= '9')
                i++;
        }

        if (i < span.Length && (span[i] == 'e' || span[i] == 'E'))
        {
            i++;
            if (i < span.Length && (span[i] == '+' || span[i] == '-'))
                i++;
            if (i == span.Length || span[i] < '0' || span[i] > '9')
                return false;
            while (i < span.Length && span[i] >= '0' && span[i] <= '9')
                i++;
        }

        return i == span.Length;
    }

    public override string ToString() => Text;
}
