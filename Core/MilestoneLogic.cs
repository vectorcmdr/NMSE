using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles milestone and global stats operations including reading/writing stat entry values and locating the global stats group.
/// </summary>
internal static class MilestoneLogic
{
    /// <summary>
    /// Maps milestone section names to their UI icon filenames.
    /// </summary>
    internal static readonly Dictionary<string, string> SectionIconMap = new()
    {
        { "Milestones", "UI-MILESTONES.PNG" },
        { "Kills", "UI-SENT.PNG" },
        { "Gek", "UI-GEK.PNG" },
        { "Vy'keen", "UI-VYKEEN.PNG" },
        { "Korvax", "UI-KORVAX.PNG" },
        { "Merchants Guild", "UI-TRADERS.PNG" },
        { "Mercenaries Guild", "UI-WARRIORS.PNG" },
        { "Explorers Guild", "UI-EXPLORERS.PNG" },
        { "Autophage", "UI-BUILDERS.PNG" },
        { "Outlaws", "UI-PIRATE.PNG" },
        { "Other Milestones / Stats", "UI-PERK.PNG"},
        { "Discoveries (Planets)", "UI-MILESTONES.PNG"},
        { "Discoveries (Creatures)", "UI-MILESTONES.PNG"},
        { "Travel", "UI-MILESTONES.PNG"},
        { "Multiplayer", "UI-PERK.PNG"},
        { "Pet Battles", "UI-PERK.PNG"},
    };

    /// <summary>
    /// Guild leveled stat thresholds (normal-mode IntValues) sourced from leveledstatstable.MXML.
    /// Each entry maps a stat ID (without the ^ prefix) to an ordered array of threshold values.
    /// A player reaches rank N when their stat value is >= Levels[N].
    /// Max rank is <c>Levels.Length - 1</c>.
    /// </summary>
    internal static readonly Dictionary<string, int[]> GuildLeveledStats = new()
    {
        // Merchants Guild (Traders)
        { "TGUILD_STAND",    [-5, -2, 0, 3, 8, 14, 21, 30, 40, 60, 100] },
        { "TGDONE_MISSIONS", [0, 3, 5, 8, 12, 15, 20, 30, 40, 50, 60] },
        { "PLANTS_PLANTED",  [0, 5, 10, 15, 20, 30, 40, 50, 60, 70, 80] },
        { "PROC_PRODS",      [0, 1, 3, 8, 15, 25, 35, 50, 65, 80, 100] },
        // Mercenaries Guild (Warriors)
        { "WGUILD_STAND",    [-5, -2, 0, 3, 8, 14, 21, 30, 40, 60, 100] },
        { "WGDONE_MISSIONS", [0, 3, 5, 8, 12, 15, 20, 30, 40, 50, 60] },
        { "PIRATES_KILLED",  [0, 5, 10, 15, 20, 30, 40, 50, 60, 70, 80] },
        { "FIENDS_KILLED",   [0, 5, 10, 25, 50, 75, 100, 125, 150, 200, 250] },
        // Explorers Guild
        { "EGUILD_STAND",    [-5, -2, 0, 3, 8, 14, 21, 30, 40, 60, 100] },
        { "EGDONE_MISSIONS", [0, 3, 5, 8, 12, 15, 20, 30, 40, 50, 60] },
        { "RARE_SCANNED",    [0, 3, 10, 15, 20, 25, 30, 35, 40, 45, 50] },
        { "DISC_FLORA",      [0, 10, 20, 30, 50, 75, 100, 150, 200, 250, 300] },
        // Outlaws (Pirates)
        { "PIRATE_STAND",    [-5, -2, 0, 5, 12, 20, 32, 50, 75, 100, 150] },
        { "PIRATE_MISSIONS", [0, 3, 5, 8, 12, 15, 20, 30, 40, 50, 60] },
        { "BOUNTIES",        [0, 1, 3, 5, 10, 15, 20, 25, 30, 35, 40] },
        { "TRADERS_KILLED",  [0, 1, 3, 5, 10, 15, 20, 25, 30, 35, 40] },
        { "SMUGGLE_VALUE",   [0, 10000, 100000, 200000, 500000, 1000000, 2000000, 5000000, 10000000, 20000000, 50000000] },
    };

    /// <summary>
    /// Returns the current rank (0..maxRank) for a guild stat based on its current value.
    /// Returns 0 if the stat is not in <see cref="GuildLeveledStats"/>.
    /// </summary>
    internal static int GetGuildRank(string statId, int value)
    {
        // Strip leading ^ if present (save JSON IDs use ^ prefix)
        string id = statId.TrimStart('^');
        if (!GuildLeveledStats.TryGetValue(id, out int[]? levels))
            return 0;
        int rank = 0;
        for (int i = 0; i < levels.Length; i++)
        {
            if (value >= levels[i])
                rank = i;
            else
                break;
        }
        return rank;
    }

    /// <summary>
    /// Returns the maximum rank for a guild stat (i.e. <c>Levels.Length - 1</c>),
    /// or 0 if the stat is not in <see cref="GuildLeveledStats"/>.
    /// </summary>
    internal static int GetGuildMaxRank(string statId)
    {
        string id = statId.TrimStart('^');
        return GuildLeveledStats.TryGetValue(id, out int[]? levels) ? levels.Length - 1 : 0;
    }

    /// <summary>
    /// Returns the amount the stat must increase to reach the next rank,
    /// or <c>-1</c> if already at max rank.
    /// Returns 0 if the stat has no leveled data.
    /// </summary>
    internal static int GetGuildNextRankIn(string statId, int value)
    {
        string id = statId.TrimStart('^');
        if (!GuildLeveledStats.TryGetValue(id, out int[]? levels))
            return 0;
        for (int i = 0; i < levels.Length; i++)
        {
            if (value < levels[i])
                return levels[i] - value;
        }
        return -1; // already at max rank
    }

    /// <summary>
    /// Finds the global stats array (group ID "^GLOBAL_STATS") within the save data's player state.
    /// </summary>
    /// <param name="saveData">The top-level save data JSON object.</param>
    /// <returns>The global stats JSON array, or <c>null</c> if not found.</returns>
    internal static JsonArray? FindGlobalStats(JsonObject saveData)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return null;
        var statsArr = playerState.GetArray("Stats");
        if (statsArr == null) return null;
        for (int i = 0; i < statsArr.Length; i++)
        {
            var group = statsArr.GetObject(i);
            if (group != null && (group.GetString("GroupId") ?? "") == "^GLOBAL_STATS")
                return group.GetArray("Stats");
        }
        return null;
    }

    /// <summary>
    /// Reads the numeric value from a stat entry, preferring IntValue and falling back to FloatValue.
    /// </summary>
    /// <param name="entry">The stat entry JSON object.</param>
    /// <returns>The integer stat value, or 0 if unreadable.</returns>
    internal static int ReadStatEntryValue(JsonObject entry)
    {
        int val = 0;
        try
        {
            var valueObj = entry.GetObject("Value");
            if (valueObj != null)
            {
                if (valueObj.Contains("IntValue"))
                    val = valueObj.GetInt("IntValue");
                else if (valueObj.Contains("FloatValue"))
                    val = (int)Math.Round(valueObj.GetFloat("FloatValue"));
            }
        }
        catch { }
        return val;
    }

    /// <summary>
    /// Writes a value to both IntValue and FloatValue fields of a stat entry.
    /// </summary>
    /// <param name="entry">The stat entry JSON object.</param>
    /// <param name="value">The integer value to write.</param>
    internal static void WriteStatEntryValue(JsonObject entry, int value)
    {
        var valueObj = entry.GetObject("Value");
        if (valueObj != null)
        {
            valueObj.Set("IntValue", value);
            // Preserve RawDouble for FloatValue if the numeric value matches
            var existing = valueObj.Get("FloatValue");
            double dval = (double)value;
            if (!(existing is RawDouble rd && rd.Value == dval))
                valueObj.Set("FloatValue", dval);
        }
    }
}
