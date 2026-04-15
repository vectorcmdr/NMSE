namespace NMSE.Core.Utilities;

/// <summary>
/// Shared helper for validating and normalizing hex seed values.
/// </summary>
internal static class SeedHelper
{
    /// <summary>
    /// Maximum length of a hex seed string including the "0x" prefix.
    /// NMS seeds are typically 16 hex digits + "0x" prefix = 18 chars.
    /// </summary>
    private const int MaxSeedLength = 18;

    /// <summary>
    /// Normalizes a hex seed string: trims whitespace, uppercases hex digits,
    /// and ensures the "0x" prefix uses lowercase 'x'.
    /// </summary>
    /// <param name="seed">The raw seed input from the user.</param>
    /// <returns>The normalized seed string, or null if invalid.</returns>
    internal static string? NormalizeSeed(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed)) return null;

        // NMS save format uses uppercase hex digits with lowercase "0x" prefix (e.g. "0xABCD1234").
        // ToUpperInvariant() uppercases everything, then Replace("0X", "0x") restores the lowercase prefix.
        var normalized = seed.Trim().ToUpperInvariant().Replace("0X", "0x");
        if (!IsValidHexSeed(normalized)) return null;

        return normalized;
    }

    /// <summary>
    /// Validates whether a string is a valid hex seed.
    /// A valid seed is non-empty, within length limits, and contains only hex characters
    /// (optionally prefixed with "0x").
    /// </summary>
    /// <param name="seed">The seed string to validate (should already be normalized).</param>
    /// <returns>True if the seed is valid.</returns>
    internal static bool IsValidHexSeed(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed)) return false;
        if (seed.Length > MaxSeedLength) return false;

        var hexPart = seed;
        if (hexPart.StartsWith("0x", System.StringComparison.Ordinal))
            hexPart = hexPart[2..];

        if (hexPart.Length == 0) return false;

        foreach (char c in hexPart)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Normalizes a seed string that may be a hex seed (e.g. "0xABCD") or a plain integer (e.g. "0", "1").
    /// The game uses both formats for SpeciesSeed and GenusSeed fields.
    /// </summary>
    /// <param name="seed">The raw seed input from the user.</param>
    /// <returns>The normalized seed string, or null if invalid.</returns>
    internal static string? NormalizeSeedOrInteger(string? seed)
    {
        if (string.IsNullOrWhiteSpace(seed)) return null;
        var trimmed = seed.Trim();

        // Accept plain integer values (the game sometimes stores 0 or 1)
        if (long.TryParse(trimmed, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out _))
            return trimmed;

        return NormalizeSeed(seed);
    }
}
