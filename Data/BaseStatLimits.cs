using System.Text.Json;

namespace NMSE.Data;

/// <summary>
/// Identifies the category of entity for stat-limit lookups.
/// </summary>
public enum StatCategory { Ship, Weapon, Freighter }

/// <summary>
/// Represents min/max range for a base stat value.
/// </summary>
public class BaseStatRange
{
    /// <summary>Stat identifier (e.g. "^SHIP_DAMAGE").</summary>
    public string Id { get; set; } = "";
    /// <summary>Maximum allowed value for this stat.</summary>
    public long MaxValue { get; set; }
    /// <summary>Minimum allowed value for this stat.</summary>
    public long MinValue { get; set; }
}

/// <summary>
/// Provides base stat limits for ships, weapons, and suits.
/// Used to validate user-entered stat values in the editor.
/// </summary>
public static class BaseStatLimits
{
    /// <summary>
    /// Ship base stat limits by ship type -> stat ID -> range.
    /// Ship types: "Normal", "Alien", "Robot", etc.
    /// Stat IDs: "^SHIP_DAMAGE", "^SHIP_SHIELD", "^SHIP_HYPERDRIVE", "^SHIP_AGILE".
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, BaseStatRange>> ShipStats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Normal"] = new()
        {
            ["^SHIP_DAMAGE"] = new BaseStatRange { Id = "^SHIP_DAMAGE", MinValue = 0, MaxValue = int.MaxValue },
            ["^SHIP_SHIELD"] = new BaseStatRange { Id = "^SHIP_SHIELD", MinValue = 0, MaxValue = int.MaxValue },
            ["^SHIP_HYPERDRIVE"] = new BaseStatRange { Id = "^SHIP_HYPERDRIVE", MinValue = 0, MaxValue = int.MaxValue },
            ["^SHIP_AGILE"] = new BaseStatRange { Id = "^SHIP_AGILE", MinValue = 0, MaxValue = int.MaxValue },
        },
        ["Alien"] = new()
        {
            ["^SHIP_DAMAGE"] = new BaseStatRange { Id = "^SHIP_DAMAGE", MinValue = 0, MaxValue = int.MaxValue },
            ["^SHIP_SHIELD"] = new BaseStatRange { Id = "^SHIP_SHIELD", MinValue = 0, MaxValue = int.MaxValue },
            ["^SHIP_HYPERDRIVE"] = new BaseStatRange { Id = "^SHIP_HYPERDRIVE", MinValue = 0, MaxValue = int.MaxValue },
            ["^SHIP_AGILE"] = new BaseStatRange { Id = "^SHIP_AGILE", MinValue = 0, MaxValue = int.MaxValue },
        },
    };

    /// <summary>
    /// Weapon (multitool) base stat limits by weapon type -> stat ID -> range.
    /// Stat IDs: "^WEAPON_DAMAGE", "^WEAPON_MINING", "^WEAPON_SCAN".
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, BaseStatRange>> WeaponStats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Normal"] = new()
        {
            ["^WEAPON_DAMAGE"] = new BaseStatRange { Id = "^WEAPON_DAMAGE", MinValue = 0, MaxValue = int.MaxValue },
            ["^WEAPON_MINING"] = new BaseStatRange { Id = "^WEAPON_MINING", MinValue = 0, MaxValue = int.MaxValue },
            ["^WEAPON_SCAN"] = new BaseStatRange { Id = "^WEAPON_SCAN", MinValue = 0, MaxValue = int.MaxValue },
        },
    };

    /// <summary>
    /// Freighter base stat limits by type -> stat ID -> range.
    /// Stat IDs: "^FREI_HYPERDRIVE", "^FREI_FLEET".
    /// </summary>
    public static readonly Dictionary<string, Dictionary<string, BaseStatRange>> FreighterStats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Normal"] = new()
        {
            ["^FREI_HYPERDRIVE"] = new BaseStatRange { Id = "^FREI_HYPERDRIVE", MinValue = 0, MaxValue = int.MaxValue },
            ["^FREI_FLEET"] = new BaseStatRange { Id = "^FREI_FLEET", MinValue = 0, MaxValue = int.MaxValue },
        },
    };

    /// <summary>
    /// Resolves the correct stat dictionary for the given category.
    /// </summary>
    private static Dictionary<string, Dictionary<string, BaseStatRange>> ResolveStatsDict(StatCategory category) => category switch
    {
        StatCategory.Ship => ShipStats,
        StatCategory.Weapon => WeaponStats,
        StatCategory.Freighter => FreighterStats,
        _ => ShipStats,
    };

    /// <summary>
    /// Validates a stat value against the known limits for the given entity type and stat.
    /// Returns the value clamped to the valid range, or the original value if no limits are known.
    /// </summary>
    /// <param name="entityType">Entity type (e.g. "Normal" for ships, "Normal" for weapons).</param>
    /// <param name="statId">The stat ID (e.g. "^SHIP_DAMAGE").</param>
    /// <param name="value">The value to validate.</param>
    /// <param name="isShip">True for ship stats, false for weapon stats.</param>
    /// <returns>The clamped value.</returns>
    public static double ClampStatValue(string entityType, string statId, double value, bool isShip)
    {
        return ClampStatValue(entityType, statId, value, isShip ? StatCategory.Ship : StatCategory.Weapon);
    }

    /// <summary>
    /// Validates a stat value against the known limits for the given category, entity type, and stat.
    /// Returns the value clamped to the valid range, or the original value if no limits are known.
    /// </summary>
    public static double ClampStatValue(string entityType, string statId, double value, StatCategory category)
    {
        var statsDict = ResolveStatsDict(category);
        if (statsDict.TryGetValue(entityType, out var stats) &&
            stats.TryGetValue(statId, out var range))
        {
            return Math.Max(range.MinValue, Math.Min(value, range.MaxValue));
        }
        return value;
    }

    /// <summary>
    /// Returns the clamped UI value unless it matches the original raw value (meaning
    /// the user didn't change it), in which case the original raw value is returned
    /// to preserve externally-edited data.
    /// When <paramref name="rawValues"/> is null or does not contain the stat, falls
    /// back to normal clamping.
    /// </summary>
    /// <remarks>
    /// Stat fields now use <see cref="NMSE.UI.Controls.InvariantNumericTextBox"/>
    /// which stores values as <c>double</c> internally with "G17" format
    /// and does <b>not</b> clamp values on display. The comparison is therefore
    /// <c>uiValue == raw</c> (not <c>uiValue == clamped(raw)</c>) because the UI
    /// always shows the unclamped original value when unchanged.
    /// </remarks>
    public static double ConditionalClampStatValue(string entityType, string statId, double uiValue,
        StatCategory category, Dictionary<string, double>? rawValues)
    {
        if (rawValues != null && rawValues.TryGetValue(statId, out double raw))
        {
            if (uiValue == raw)
                return raw; // User didn't change it - preserve original value
        }
        return ClampStatValue(entityType, statId, uiValue, category);
    }

    /// <summary>
    /// Gets the base stat range for a given entity type and stat ID.
    /// Returns null if no limits are defined for that combination.
    /// </summary>
    public static BaseStatRange? GetRange(string entityType, string statId, bool isShip)
    {
        return GetRange(entityType, statId, isShip ? StatCategory.Ship : StatCategory.Weapon);
    }

    /// <summary>
    /// Gets the base stat range for a given category, entity type, and stat ID.
    /// Returns null if no limits are defined for that combination.
    /// </summary>
    public static BaseStatRange? GetRange(string entityType, string statId, StatCategory category)
    {
        var statsDict = ResolveStatsDict(category);
        if (statsDict.TryGetValue(entityType, out var stats) &&
            stats.TryGetValue(statId, out var range))
        {
            return range;
        }
        return null;
    }

    /// <summary>
    /// Loads stat limits from a BaseStatLimits.json file produced by the extractor.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("Ships", out var shipsProp))
                LoadEntityStats(shipsProp, ShipStats);

            if (root.TryGetProperty("Weapons", out var weaponsProp))
                LoadEntityStats(weaponsProp, WeaponStats);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void LoadEntityStats(JsonElement entityProp, Dictionary<string, Dictionary<string, BaseStatRange>> target)
    {
        if (entityProp.ValueKind != JsonValueKind.Object) return;

        foreach (var typeProp in entityProp.EnumerateObject())
        {
            if (typeProp.Value.ValueKind != JsonValueKind.Object) continue;

            if (!typeProp.Value.TryGetProperty("BaseStats", out var statsProp)) continue;
            if (statsProp.ValueKind != JsonValueKind.Object) continue;

            var stats = new Dictionary<string, BaseStatRange>(StringComparer.OrdinalIgnoreCase);
            foreach (var statGroup in statsProp.EnumerateObject())
            {
                if (statGroup.Value.ValueKind != JsonValueKind.Object) continue;
                foreach (var statEntry in statGroup.Value.EnumerateObject())
                {
                    if (statEntry.Value.ValueKind != JsonValueKind.Object) continue;
                    var range = new BaseStatRange { Id = statEntry.Name };
                    if (statEntry.Value.TryGetProperty("MinValue", out var minProp) && minProp.TryGetInt64(out long min))
                        range.MinValue = min;
                    if (statEntry.Value.TryGetProperty("MaxValue", out var maxProp) && maxProp.TryGetInt64(out long max))
                        range.MaxValue = max;
                    stats[statEntry.Name] = range;
                }
            }
            if (stats.Count > 0)
                target[typeProp.Name] = stats;
        }
    }
}
