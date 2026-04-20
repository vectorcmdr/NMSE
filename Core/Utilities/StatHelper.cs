using NMSE.Models;
using System.Globalization;

namespace NMSE.Core.Utilities;

/// <summary>
/// Shared helper for reading and writing BaseStatValues across inventory logic classes.
/// </summary>
internal static class StatHelper
{
    /// <summary>
    /// Read a base stat value from an inventory's BaseStatValues array.
    /// Returns 0.0 if the inventory is null, the array is missing, or the stat ID is not found.
    /// </summary>
    internal static double ReadBaseStatValue(JsonObject? inventory, string statId)
    {
        if (inventory == null) return 0.0;
        try
        {
            var baseStatValues = inventory.GetArray("BaseStatValues");
            if (baseStatValues == null) return 0.0;
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                var entry = baseStatValues.GetObject(i);
                if (entry.GetString("BaseStatID") == statId)
                    return entry.GetDouble("Value");
            }
        }
        catch { }
        return 0.0;
    }

    /// <summary>
    /// Reads the text representation of a base stat value from an inventory's
    /// BaseStatValues array. If the stored value is a <see cref="RawDouble"/>,
    /// this returns its original JSON text; otherwise it formats the double
    /// with "G17". Returns "0" if the stat is not found.
    /// </summary>
    internal static string ReadBaseStatText(JsonObject? inventory, string statId)
    {
        if (inventory == null) return "0";
        try
        {
            var baseStatValues = inventory.GetArray("BaseStatValues");
            if (baseStatValues == null) return "0";
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                var entry = baseStatValues.GetObject(i);
                if (entry.GetString("BaseStatID") == statId)
                    return entry.GetDoubleText("Value");
            }
        }
        catch { }
        return "0";
    }

    /// <summary>
    /// Write a base stat value in an inventory's BaseStatValues array.
    /// No-op if the inventory is null, the array is missing, or the stat ID is not found.
    /// When <paramref name="displayText"/> is provided the value is stored as a
    /// <see cref="RawDouble"/> so that serialisation reproduces the exact text the
    /// user typed (or that was loaded from the save file) rather than reformatting
    /// through "G17".
    /// </summary>
    internal static void WriteBaseStatValue(JsonObject? inventory, string statId, double value, string? displayText = null)
    {
        if (inventory == null) return;
        try
        {
            var baseStatValues = inventory.GetArray("BaseStatValues");
            if (baseStatValues == null) return;
            for (int i = 0; i < baseStatValues.Length; i++)
            {
                var entry = baseStatValues.GetObject(i);
                if (entry.GetString("BaseStatID") == statId)
                {
                    // If the existing value is a RawDouble whose numeric value matches
                    // the incoming value, skip the write to preserve the original JSON
                    // text representation and avoid precision loss from double-to-string
                    // round-tripping.
                    // Stat fields now use InvariantNumericTextBox (double-backed) so no
                    // decimal conversion occurs - exact double equality is safe.
                    var existing = entry.GetValue("Value");
                    if (existing is RawDouble rd && rd.Value == value)
                        return;
                    // Also skip for plain doubles that are bit-identical (guards
                    // against a redundant write after a previous save already
                    // replaced the RawDouble with a plain double).
                    if (existing is double d && d == value)
                        return;
                    // Store as RawDouble when display text is provided, so that
                    // the serialiser writes the user's exact text rather than
                    // reformatting through G17.
                    if (displayText != null)
                        entry.Set("Value", new RawDouble(value, displayText));
                    else
                        entry.Set("Value", value);
                    return;
                }
            }
        }
        catch { }
    }
}
