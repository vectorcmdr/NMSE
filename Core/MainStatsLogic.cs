using NMSE.Core.Utilities;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles reading and writing core player stats such as health, shield, energy, units, nanites, and quicksilver.
/// </summary>
internal static class MainStatsLogic
{
    /// <summary>
    /// Definitions for each player stat field, including display label, JSON key, and maximum allowed value.
    /// </summary>
    internal static readonly (string Label, string Key, decimal Maximum)[] StatFields =
    {
        ("Health", "Health", 999999),
        ("Shield", "Shield", 999999),
        ("Energy", "Energy", 999999),
        ("Units", "Units", uint.MaxValue),
        ("Nanites", "Nanites", uint.MaxValue),
        ("Quicksilver", "Specials", uint.MaxValue),
    };

    /// <summary>
    /// Reads a numeric stat value from the player state without clamping.
    /// Handles various underlying types including int, long, double, and decimal.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="key">The JSON key for the stat.</param>
    /// <returns>The raw stat value, or 0 on failure.</returns>
    internal static decimal ReadRawStatValue(JsonObject playerState, string key)
    {
        try
        {
            var value = playerState.Get(key) ?? playerState.GetValue(key);
            if (value == null) return 0;

            if (value is JsonObject jobj)
            {
                value = jobj.Get("Value") ?? jobj.Get("value");
                if (value == null) return 0;
            }

            if (value is int i)
                return (decimal)(uint)i;
            if (value is long l)
                return (decimal)(l & 0xFFFFFFFFL);
            if (value is Models.RawDouble rd)
                return CoerceToUnsignedDecimal(rd.Value);
            if (value is double dbl)
                return CoerceToUnsignedDecimal(dbl);
            if (value is decimal d)
                return CoerceToUnsignedDecimal(d);

            return Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Treats a negative value in the signed int range as an unsigned 32-bit integer
    /// (NMS stores large currency values as signed ints that overflow into negatives).
    /// </summary>
    private static decimal CoerceToUnsignedDecimal(double value)
        => value < 0 && value >= int.MinValue ? (decimal)(uint)(int)value : (decimal)value;

    /// <inheritdoc cref="CoerceToUnsignedDecimal(double)"/>
    private static decimal CoerceToUnsignedDecimal(decimal value)
        => value < 0 && value >= int.MinValue ? (decimal)(uint)(int)value : value;

    /// <summary>
    /// Reads a numeric stat value from the player state, clamping it within the specified range.
    /// Handles various underlying types including int, long, double, and decimal.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="key">The JSON key for the stat.</param>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    /// <returns>The clamped stat value, or 0 on failure.</returns>
    internal static decimal ReadStatValue(JsonObject playerState, string key, decimal minimum, decimal maximum)
    {
        decimal raw = ReadRawStatValue(playerState, key);
        if (raw < minimum) return minimum;
        if (raw > maximum) return maximum;
        return raw;
    }

    /// <summary>
    /// Writes all core player stat values to the player state JSON object.
    /// Currency values (units, nanites, quicksilver) are stored as unchecked unsigned integers.
    /// When raw values are provided, a stat is only written if the user changed it from its
    /// clamped display value. This preserves externally-edited values that fall outside
    /// the editor's UI range.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="health">The health value to write.</param>
    /// <param name="shield">The shield value to write.</param>
    /// <param name="energy">The energy value to write.</param>
    /// <param name="units">The units (currency) value to write.</param>
    /// <param name="nanites">The nanites value to write.</param>
    /// <param name="quicksilver">The quicksilver value to write.</param>
    /// <param name="rawValues">Optional raw (unclamped) values loaded from JSON. When provided,
    /// each stat is only written if the UI value differs from what clamping would produce.</param>
    internal static void WriteStatValues(JsonObject playerState, decimal health, decimal shield, decimal energy,
        decimal units, decimal nanites, decimal quicksilver,
        Dictionary<string, decimal>? rawValues = null)
    {
        WriteIfChanged(playerState, "Health", health, 0, 999999, rawValues, v => (int)v);
        WriteIfChanged(playerState, "Shield", shield, 0, 999999, rawValues, v => (int)v);
        WriteIfChanged(playerState, "Energy", energy, 0, 999999, rawValues, v => (int)v);
        WriteIfChanged(playerState, "Units", units, 0, uint.MaxValue, rawValues, v => unchecked((int)(uint)v));
        WriteIfChanged(playerState, "Nanites", nanites, 0, uint.MaxValue, rawValues, v => unchecked((int)(uint)v));
        WriteIfChanged(playerState, "Specials", quicksilver, 0, uint.MaxValue, rawValues, v => unchecked((int)(uint)v));
    }

    /// <summary>
    /// Writes a stat value only if the user actually changed it from its clamped display value.
    /// When <paramref name="rawValues"/> is null or does not contain the key, the value is always written.
    /// </summary>
    /// <remarks>
    /// MainStats values (Health, Shield, Energy, Units, etc.) are INTEGER values that
    /// <b>are</b> clamped on display via <see cref="ReadStatValue"/>. The comparison is
    /// therefore <c>uiValue == clamped(raw)</c> — the user sees the clamped value, and
    /// if they don't change it, the original raw is preserved.
    /// </remarks>
    private static void WriteIfChanged(JsonObject playerState, string key, decimal uiValue,
        decimal minimum, decimal maximum, Dictionary<string, decimal>? rawValues,
        Func<decimal, int> convert)
    {
        if (rawValues != null && rawValues.TryGetValue(key, out decimal raw))
        {
            decimal clamped = Math.Max(minimum, Math.Min(raw, maximum));
            if (uiValue == clamped)
                return; // User didn't change it - preserve original JSON value
        }
        RawNumberGuard.SetInt(playerState, key, convert(uiValue));
    }
}
