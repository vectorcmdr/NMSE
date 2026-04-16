using System.Globalization;
using System.Text.RegularExpressions;

namespace NMSE.Core.Utilities;

/// <summary>
/// Shared helpers for procedural technology item seed generation and parsing.
/// Procedural tech items (UP_*) store their ID as "baseId#NNNNN" where NNNNN is
/// a 5-digit zero-padded decimal seed (range 00000-99999), matching the format
/// used by the game and other save editors (NomNom, NMSSaveEditor).
/// </summary>
internal static class ProceduralSeedHelper
{
    private static readonly Regex SeedSuffix = new(@"#(\d{5,})$", RegexOptions.Compiled);

    /// <summary>
    /// Generates a random 5-digit zero-padded procedural seed string (00000-99999).
    /// </summary>
    internal static string Generate()
    {
        return Random.Shared.Next(0, 100000).ToString("D5", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Splits an item ID into its base ID and optional procedural seed.
    /// Example: "^UP_LAUNX#91934" returns ("^UP_LAUNX", "91934").
    /// If no seed is present, the seed portion is empty.
    /// </summary>
    internal static (string baseId, string seed) Strip(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return (itemId, "");
        var m = SeedSuffix.Match(itemId);
        if (m.Success)
            return (itemId.Substring(0, m.Index), m.Groups[1].Value);
        return (itemId, "");
    }

    /// <summary>
    /// Returns true if the given seed text is a valid 5-digit decimal seed.
    /// </summary>
    internal static bool IsValidSeed(string? seed)
    {
        if (string.IsNullOrEmpty(seed)) return false;
        return Regex.IsMatch(seed, @"^\d{5}$");
    }
}
