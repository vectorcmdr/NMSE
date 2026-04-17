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
        { "Traders", "UI-TRADERS.PNG" },
        { "Warriors", "UI-WARRIORS.PNG" },
        { "Explorers", "UI-EXPLORERS.PNG" },
        { "Autophage", "UI-BUILDERS.PNG" },
        { "Pirate", "UI-PIRATE.PNG" },
        { "Other Milestones / Stats", "UI-PERK.PNG"},
        { "Discoveries (Planets)", "UI-MILESTONES.PNG"},
        { "Discoveries (Creatures)", "UI-MILESTONES.PNG"},
        { "Travel", "UI-MILESTONES.PNG"},
        { "Multiplayer", "UI-PERK.PNG"},
        { "Pet Battles", "UI-PERK.PNG"},
    };

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
