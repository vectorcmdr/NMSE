using NMSE.Models;

namespace NMSE.Core.Utilities;

/// <summary>
/// Static helpers for manipulating inventory slot JSON data.
/// Used by InventoryGridPanel for drag-and-drop operations.
/// </summary>
internal static class InventorySlotHelper
{
    /// <summary>
    /// Update the Index object inside a slot's JSON data to new grid coordinates.
    /// </summary>
    internal static void UpdateSlotIndex(JsonObject slotData, int newX, int newY)
    {
        var indexObj = slotData.GetObject("Index");
        if (indexObj != null)
        {
            indexObj.Set("X", newX);
            indexObj.Set("Y", newY);
        }
        else
        {
            // Create Index if missing
            indexObj = new JsonObject();
            indexObj.Add("X", newX);
            indexObj.Add("Y", newY);
            slotData.Add("Index", indexObj);
        }
    }

    /// <summary>
    /// Swap the Index coordinates between two slot JSON objects.
    /// Each slot's Index is updated to the other slot's position.
    /// </summary>
    internal static void SwapSlotIndices(JsonObject slotA, int posAx, int posAy,
        JsonObject slotB, int posBx, int posBy)
    {
        UpdateSlotIndex(slotA, posBx, posBy);
        UpdateSlotIndex(slotB, posAx, posAy);
    }

    /// <summary>
    /// Create a deep-copy slot JSON object at a new grid position, duplicating
    /// the item data from the source slot.
    /// </summary>
    internal static JsonObject DuplicateSlot(JsonObject sourceSlot, int targetX, int targetY)
    {
        var newSlot = new JsonObject();

        // Copy Type
        var srcType = sourceSlot.GetObject("Type");
        if (srcType != null)
        {
            var typeObj = new JsonObject();
            string invType = srcType.GetString("InventoryType") ?? "Product";
            typeObj.Add("InventoryType", invType);
            newSlot.Add("Type", typeObj);
        }

        // Copy Id
        var idVal = sourceSlot.Get("Id");
        if (idVal != null)
            newSlot.Add("Id", idVal);

        // Copy numeric fields
        try { newSlot.Add("Amount", sourceSlot.GetInt("Amount")); } catch { newSlot.Add("Amount", 0); }
        try { newSlot.Add("MaxAmount", sourceSlot.GetInt("MaxAmount")); } catch { newSlot.Add("MaxAmount", 0); }
        try
        {
            // Copy the raw value to preserve RawDouble text representation
            var dmg = sourceSlot.Get("DamageFactor");
            newSlot.Add("DamageFactor", dmg is RawDouble ? dmg : sourceSlot.GetDouble("DamageFactor"));
        }
        catch { newSlot.Add("DamageFactor", 0.0); }

        // Copy boolean fields
        newSlot.Add("FullyInstalled", true);
        newSlot.Add("AddedAutomatically", false);

        // Set target position
        var indexObj = new JsonObject();
        indexObj.Add("X", targetX);
        indexObj.Add("Y", targetY);
        newSlot.Add("Index", indexObj);

        return newSlot;
    }

    /// <summary>
    /// Returns true if the item ID represents a ship damage slot placeholder.
    /// Matches any ID starting with SHIPSLOT_DMG (e.g. ^SHIPSLOT_DMG1 through ^SHIPSLOT_DMG12).
    /// </summary>
    internal static bool IsDamageSlotItem(string? itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        ReadOnlySpan<char> id = itemId.AsSpan();
        if (id.Length > 0 && id[0] == '^')
            id = id.Slice(1);
        return id.StartsWith("SHIPSLOT_DMG", StringComparison.OrdinalIgnoreCase);
    }
}
