using System.Globalization;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles discovery data operations including word/language knowledge, known items, and portal glyph management.
/// </summary>
internal static class CatalogueLogic
{
    /// <summary>
    /// Race columns used in the word knowledge UI, mapping display names to race indices.
    /// </summary>
    internal static readonly (string Name, int Index)[] RaceColumns =
    {
        ("Gek", 0),
        ("Vy'keen", 1),
        ("Korvax", 2),
        ("Atlas", 4),
        ("Autophage", 8),
    };

    /// <summary>
    /// Maps word group prefixes to their corresponding race indices for lookup.
    /// </summary>
    internal static readonly (string Prefix, int RaceIndex)[] RacePrefixes =
    {
        ("^TRA_", 0),
        ("^WAR_", 1),
        ("^EXP_", 2),
        ("^ATLAS_", 4),
        ("^ROBOT_", 3),
        ("^AUTO_", 8),
    };

    /// <summary>
    /// Total number of race slots in the word knowledge arrays.
    /// </summary>
    internal const int TotalRaceCount = 9;

    /// <summary>
    /// Item type identifiers considered as technology items for discovery purposes.
    /// </summary>
    internal static readonly HashSet<string> TechItemTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Technology", "Others", "Upgrades", "none", "Exocraft"
    };

    /// <summary>
    /// Item type identifiers considered as product items for discovery purposes.
    /// Excludes Technology and Upgrades.
    /// </summary>
    internal static readonly HashSet<string> ProductItemTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Products", "Constructed Technology", "Buildings", "Corvette",
        "Curiosities", "Others"
    };

    /// <summary>
    /// Determines whether a technology item is learnable:
    /// A technology is learnable if it is not procedural and not in the Maintenance
    /// category, OR if it is a portal glyph.
    /// Portal glyph detection is approximated by checking the ID prefix "^YOURPORTALGLYPH"
    /// since our GameItem model doesn't carry an IsPortalGlyph flag - and the game stores
    /// portal glyphs separately via KnownPortalRunes, so they don't typically appear in
    /// the technology picker anyway.
    /// </summary>
    /// <param name="item">The game item to check.</param>
    /// <returns>True if the technology is learnable.</returns>
    internal static bool IsLearnableTechnology(Data.GameItem item) =>
        (!item.IsProcedural && !string.Equals(item.Category, "Maintenance", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Determines whether a product item is learnable:
    /// A product is learnable if (IsCraftable AND NOT IsProcedural) OR
    /// IsSpecial (TradeCategory == "SpecialShop").
    /// This filters out non-craftable, procedural, and non-special products from
    /// the Known Products picker.
    /// </summary>
    /// <param name="item">The game item to check.</param>
    /// <returns>True if the product is learnable.</returns>
    internal static bool IsLearnableProduct(Data.GameItem item) =>
        (item.IsCraftable && !item.IsProcedural)
        || string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether a specific word group is marked as known for a given race.
    /// </summary>
    /// <param name="knownWordGroups">The JSON array of known word group entries.</param>
    /// <param name="groupName">The word group identifier.</param>
    /// <param name="raceOrdinal">The race index to check.</param>
    /// <returns><c>true</c> if the word is known for that race; otherwise <c>false</c>.</returns>
    internal static bool IsWordKnown(JsonArray knownWordGroups, string groupName, int raceOrdinal)
    {
        for (int i = 0; i < knownWordGroups.Length; i++)
        {
            var entry = knownWordGroups.GetObject(i);
            if (entry != null && groupName.Equals(entry.GetString("Group"), StringComparison.Ordinal))
            {
                var races = entry.GetArray("Races");
                return races != null && raceOrdinal < races.Length && races.GetBool(raceOrdinal);
            }
        }
        return false;
    }

    /// <summary>
    /// Sets or clears the known state for a word group and race.
    /// Adds a new entry if setting to known, or removes the entry if no races remain known.
    /// </summary>
    /// <param name="knownWordGroups">The JSON array of known word group entries.</param>
    /// <param name="groupName">The word group identifier.</param>
    /// <param name="raceOrdinal">The race index to update.</param>
    /// <param name="known">Whether the word should be marked as known.</param>
    internal static void SetWordKnown(JsonArray knownWordGroups, string groupName, int raceOrdinal, bool known)
    {
        for (int i = 0; i < knownWordGroups.Length; i++)
        {
            var entry = knownWordGroups.GetObject(i);
            if (entry != null && groupName.Equals(entry.GetString("Group"), StringComparison.Ordinal))
            {
                var races = entry.GetArray("Races");
                if (races == null) return;

                for (int ri = races.Length; ri < TotalRaceCount; ri++)
                    races.Add(false);

                races.Set(raceOrdinal, known);

                bool anyKnown = false;
                for (int r = 0; r < races.Length; r++)
                {
                    if (races.GetBool(r)) { anyKnown = true; break; }
                }
                if (!anyKnown)
                    knownWordGroups.RemoveAt(i);

                return;
            }
        }

        if (known)
        {
            var newEntry = new JsonObject();
            newEntry.Set("Group", groupName);
            var races = new JsonArray();
            for (int r = 0; r < TotalRaceCount; r++)
                races.Add(false);
            races.Set(raceOrdinal, true);
            newEntry.Set("Races", races);
            knownWordGroups.Add(newEntry);
        }
    }

    /// <summary>
    /// Sets the known state for all words that belong to a specific race.
    /// </summary>
    /// <param name="knownWordGroups">The JSON array of known word group entries.</param>
    /// <param name="words">The full word list from the word database.</param>
    /// <param name="raceOrdinal">The race index to update.</param>
    /// <param name="known">Whether the words should be marked as known.</param>
    /// <returns>The number of words affected.</returns>
    internal static int SetWordFlagsForRace(JsonArray knownWordGroups, IReadOnlyList<WordEntry> words, int raceOrdinal, bool known)
    {
        int count = 0;
        for (int w = 0; w < words.Count; w++)
        {
            var word = words[w];
            string? groupName = word.GetGroupForRace(raceOrdinal);
            if (groupName != null)
            {
                SetWordKnown(knownWordGroups, groupName, raceOrdinal, known);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Sets the known state for a set of words across all races that each word supports.
    /// </summary>
    /// <param name="knownWordGroups">The JSON array of known word group entries.</param>
    /// <param name="words">The subset of words to update.</param>
    /// <param name="raceColumns">The race columns (Name, Index) to iterate.</param>
    /// <param name="known">Whether the words should be marked as known.</param>
    /// <returns>The number of individual race-word flags changed.</returns>
    internal static int SetWordFlagsForEntries(JsonArray knownWordGroups, IReadOnlyList<WordEntry> words,
        (string Name, int Index)[] raceColumns, bool known)
    {
        int count = 0;
        for (int w = 0; w < words.Count; w++)
        {
            var word = words[w];
            for (int c = 0; c < raceColumns.Length; c++)
            {
                int raceOrdinal = raceColumns[c].Index;
                string? groupName = word.GetGroupForRace(raceOrdinal);
                if (groupName != null)
                {
                    SetWordKnown(knownWordGroups, groupName, raceOrdinal, known);
                    count++;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// Updates the word-related stat counters in the save data to match the current KnownWordGroups.
    /// The game uses these counters for milestone tracking and validation.
    /// </summary>
    /// <param name="saveData">The top-level save data JSON object.</param>
    /// <param name="knownWordGroups">The current KnownWordGroups JSON array.</param>
    internal static void SyncWordStats(JsonObject saveData, JsonArray knownWordGroups)
    {
        var globalStats = MilestoneLogic.FindGlobalStats(saveData);
        if (globalStats == null) return;

        // Count total known groups and per-race learned words
        int totalGroups = knownWordGroups.Length;
        int gekCount = 0, vykeenCount = 0, korvaxCount = 0;
        for (int i = 0; i < knownWordGroups.Length; i++)
        {
            try
            {
                var entry = knownWordGroups.GetObject(i);
                var races = entry?.GetArray("Races");
                if (races == null) continue;
                if (races.Length > 0 && races.GetBool(0)) gekCount++;     // Traders = 0
                if (races.Length > 1 && races.GetBool(1)) vykeenCount++;  // Warriors = 1
                if (races.Length > 2 && races.GetBool(2)) korvaxCount++;  // Explorers = 2
            }
            catch { }
        }

        // Update stat entries
        SetGlobalStatValue(globalStats, "^WORDS_LEARNT", totalGroups);
        SetGlobalStatValue(globalStats, "^TWORDS_LEARNT", gekCount);
        SetGlobalStatValue(globalStats, "^WWORDS_LEARNT", vykeenCount);
        SetGlobalStatValue(globalStats, "^EWORDS_LEARNT", korvaxCount);
    }

    /// <summary>
    /// Sets the IntValue of a global stat entry. Creates it if not found.
    /// </summary>
    private static void SetGlobalStatValue(JsonArray globalStats, string statId, int value)
    {
        for (int i = 0; i < globalStats.Length; i++)
        {
            try
            {
                var entry = globalStats.GetObject(i);
                if (entry == null) continue;
                if (statId.Equals(entry.GetString("Id"), StringComparison.Ordinal))
                {
                    var valueObj = entry.GetObject("Value");
                    if (valueObj != null)
                        valueObj.Set("IntValue", value);
                    return;
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Strips the leading "^" prefix that the game uses in save-file IDs.
    /// </summary>
    internal static string StripCaretPrefix(string id) =>
        id.Length > 1 && id[0] == '^' ? id[1..] : id;

    /// <summary>
    /// Ensures the "^" prefix that the game expects in save-file IDs.
    /// </summary>
    internal static string EnsureCaretPrefix(string id) =>
        id.Length > 0 && id[0] != '^' ? "^" + id : id;

    /// <summary>
    /// Loads the list of known item IDs from a named JSON array in the player state.
    /// The save file stores IDs with a leading "^" prefix (e.g. "^LASER"), but the
    /// game item database uses unprefixed IDs (e.g. "LASER"). This method strips
    /// the prefix so that loaded IDs match the database, preventing duplicate-add
    /// and incorrect-save bugs.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="arrayName">The JSON key for the known items array.</param>
    /// <returns>A list of known item ID strings (without "^" prefix).</returns>
    internal static List<string> LoadKnownItemIds(JsonObject playerState, string arrayName)
    {
        var ids = new List<string>();
        var items = playerState.GetArray(arrayName);
        if (items == null) return ids;

        for (int i = 0; i < items.Length; i++)
            ids.Add(StripCaretPrefix(items.GetString(i)));

        return ids;
    }

    /// <summary>
    /// Saves a list of known item IDs back to a named JSON array in the player state.
    /// Re-adds the "^" prefix that the game expects in save-file IDs.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="arrayName">The JSON key for the known items array.</param>
    /// <param name="ids">The item IDs to save (with or without "^" prefix).</param>
    internal static void SaveKnownItemIds(JsonObject playerState, string arrayName, List<string> ids)
    {
        var items = playerState.GetArray(arrayName);
        if (items == null)
        {
            items = new JsonArray();
            playerState.Set(arrayName, items);
        }

        // Normalise the incoming IDs so comparisons are consistent.
        var desired = new List<string>(ids.Count);
        foreach (var id in ids)
            desired.Add(EnsureCaretPrefix(id));

        // Build a set of the desired IDs for fast lookup.
        var desiredSet = new HashSet<string>(desired, StringComparer.OrdinalIgnoreCase);

        // Remove entries from the existing array that are no longer desired,
        // preserving the original order of entries that remain.
        for (int i = items.Length - 1; i >= 0; i--)
        {
            var existing = items.GetString(i);
            if (!desiredSet.Contains(existing))
                items.RemoveAt(i);
        }

        // Build the current set (after removals) to know what's already present.
        var currentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < items.Length; i++)
        {
            var existing = items.GetString(i);
            if (!string.IsNullOrEmpty(existing))
                currentSet.Add(existing);
        }

        // Append any new entries that weren't in the original array.
        foreach (var id in desired)
        {
            if (!currentSet.Contains(id))
                items.Add(id);
        }
    }

    /// <summary>
    /// Loads the portal glyph knowledge bitfield from the player state.
    /// Each bit represents whether a specific glyph is known.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <returns>The glyph bitfield integer.</returns>
    internal static int LoadGlyphBitfield(JsonObject playerState)
    {
        int runesBitfield = 0;
        try
        {
            var val = playerState.Get("KnownPortalRunes");
            if (val is int i) runesBitfield = i;
            else if (val is long l) runesBitfield = (int)l;
            else if (val != null) runesBitfield = Convert.ToInt32(val, CultureInfo.InvariantCulture);
        }
        catch { }
        return runesBitfield;
    }

    /// <summary>
    /// Saves the portal glyph knowledge bitfield to the player state.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="bitfield">The glyph bitfield integer to save.</param>
    internal static void SaveGlyphBitfield(JsonObject playerState, int bitfield)
    {
        playerState.Set("KnownPortalRunes", bitfield);
    }
}
