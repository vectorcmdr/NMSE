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

    public override string ToString() => Text;
}
