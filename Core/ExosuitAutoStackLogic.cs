using System.Text;
using NMSE.Core.Utilities;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Moves item amounts from Exosuit cargo into existing matching stacks in Chest 1-10 inventories.
/// Only chest stacks that already contain the same item are valid destinations.
/// </summary>
internal static class ExosuitAutoStackLogic
{
    private sealed class ChestInventoryInfo
    {
        public required JsonObject Inventory { get; init; }
        public required JsonArray Slots { get; init; }
        public int ChestIndex { get; init; }
    }

    private sealed class ChestTarget
    {
        public required JsonObject Slot { get; init; }
        public int SlotIndex { get; init; }
        public int Amount { get; init; }
        public int MaxAmount { get; init; }
    }

    public static bool AutoStackCargoToChests(
        JsonObject cargoInventory,
        JsonObject playerState,
        out int movedUnits,
        out int touchedCargoSlots,
        ISet<(int x, int y)>? pinnedSourceSlots = null,
        (int x, int y)? sourceSlotFilter = null,
        string? sourceItemIdFilter = null)
    {
        movedUnits = 0;
        touchedCargoSlots = 0;

        var cargoSlots = cargoInventory.GetArray("Slots");
        if (cargoSlots == null || cargoSlots.Length == 0)
            return false;

        var chestInventories = new List<ChestInventoryInfo>();
        for (int i = 0; i < BaseLogic.ChestInventoryKeys.Length; i++)
        {
            var chestInventory = playerState.GetObject(BaseLogic.ChestInventoryKeys[i]);
            var slots = chestInventory?.GetArray("Slots");
            if (chestInventory != null && slots != null)
            {
                chestInventories.Add(new ChestInventoryInfo
                {
                    Inventory = chestInventory,
                    Slots = slots,
                    ChestIndex = i,
                });
            }
        }

        if (chestInventories.Count == 0)
            return false;

        bool changed = false;

        for (int cargoIndex = cargoSlots.Length - 1; cargoIndex >= 0; cargoIndex--)
        {
            JsonObject? cargoSlot;
            try { cargoSlot = cargoSlots.GetObject(cargoIndex); }
            catch { continue; }
            if (cargoSlot == null || IsTechnologySlot(cargoSlot))
                continue;

            if (!ShouldProcessSourceSlot(cargoSlot, pinnedSourceSlots, sourceSlotFilter, sourceItemIdFilter, out _))
                continue;

            string itemId = ExtractSlotItemId(cargoSlot);
            if (string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM")
                continue;

            int sourceAmount;
            try { sourceAmount = cargoSlot.GetInt("Amount"); }
            catch { continue; }

            if (sourceAmount <= 0)
                continue;

            var destinationChests = FindDestinationChests(chestInventories, itemId);
            if (destinationChests.Count == 0)
                continue;

            int movedFromCargoSlot = 0;
            foreach (var destinationChest in destinationChests)
            {
                movedFromCargoSlot = TryMoveToInventory(
                    sourceSlot: cargoSlot,
                    sourceAmount: sourceAmount,
                    itemId: itemId,
                    destination: destinationChest,
                    allowNewSlots: true);

                if (movedFromCargoSlot > 0)
                    break;
            }

            if (movedFromCargoSlot <= 0)
                continue;

            int remaining = sourceAmount - movedFromCargoSlot;
            movedUnits += movedFromCargoSlot;
            touchedCargoSlots++;
            changed = true;

            if (remaining <= 0)
            {
                cargoSlots.RemoveAt(cargoIndex);
            }
            else
            {
                cargoSlot.Set("Amount", remaining);
            }
        }

        return changed;
    }

    public static bool AutoStackCargoToStarship(
        JsonObject cargoInventory,
        JsonObject playerState,
        out int movedUnits,
        out int touchedCargoSlots,
        ISet<(int x, int y)>? pinnedSourceSlots = null,
        (int x, int y)? sourceSlotFilter = null,
        string? sourceItemIdFilter = null)
    {
        movedUnits = 0;
        touchedCargoSlots = 0;

        var ships = playerState.GetArray("ShipOwnership");
        if (ships == null || ships.Length == 0)
            return false;

        int primaryShip = 0;
        try { primaryShip = playerState.GetInt("PrimaryShip"); }
        catch { }

        if (primaryShip < 0 || primaryShip >= ships.Length)
            return false;

        var ship = ships.GetObject(primaryShip);
        var shipInventory = ship?.GetObject("Inventory");
        if (shipInventory == null)
            return false;

        return AutoStackCargoToInventory(
            cargoInventory,
            shipInventory,
            out movedUnits,
            out touchedCargoSlots,
            pinnedSourceSlots,
            sourceSlotFilter,
            sourceItemIdFilter);
    }

    public static bool AutoStackCargoToFreighter(
        JsonObject cargoInventory,
        JsonObject playerState,
        out int movedUnits,
        out int touchedCargoSlots,
        ISet<(int x, int y)>? pinnedSourceSlots = null,
        (int x, int y)? sourceSlotFilter = null,
        string? sourceItemIdFilter = null)
    {
        movedUnits = 0;
        touchedCargoSlots = 0;

        var freighterInventory = playerState.GetObject("FreighterInventory");
        if (freighterInventory == null)
            return false;

        return AutoStackCargoToInventory(
            cargoInventory,
            freighterInventory,
            out movedUnits,
            out touchedCargoSlots,
            pinnedSourceSlots,
            sourceSlotFilter,
            sourceItemIdFilter);
    }

    public static bool AutoStackFromInventoryToInventory(
        JsonObject sourceInventory,
        JsonObject destinationInventory,
        out int movedUnits,
        out int touchedSourceSlots,
        ISet<(int x, int y)>? pinnedSourceSlots = null,
        (int x, int y)? sourceSlotFilter = null,
        string? sourceItemIdFilter = null)
    {
        return AutoStackCargoToInventory(
            sourceInventory,
            destinationInventory,
            out movedUnits,
            out touchedSourceSlots,
            pinnedSourceSlots,
            sourceSlotFilter,
            sourceItemIdFilter);
    }

    private static bool AutoStackCargoToInventory(
        JsonObject cargoInventory,
        JsonObject destinationInventory,
        out int movedUnits,
        out int touchedCargoSlots,
        ISet<(int x, int y)>? pinnedSourceSlots = null,
        (int x, int y)? sourceSlotFilter = null,
        string? sourceItemIdFilter = null)
    {
        movedUnits = 0;
        touchedCargoSlots = 0;

        var cargoSlots = cargoInventory.GetArray("Slots");
        var destinationSlots = destinationInventory.GetArray("Slots");
        if (cargoSlots == null || cargoSlots.Length == 0 || destinationSlots == null)
            return false;

        bool changed = false;
        var destination = new ChestInventoryInfo
        {
            Inventory = destinationInventory,
            Slots = destinationSlots,
            ChestIndex = -1,
        };

        for (int cargoIndex = cargoSlots.Length - 1; cargoIndex >= 0; cargoIndex--)
        {
            JsonObject? cargoSlot;
            try { cargoSlot = cargoSlots.GetObject(cargoIndex); }
            catch { continue; }
            if (cargoSlot == null || IsTechnologySlot(cargoSlot))
                continue;

            if (!ShouldProcessSourceSlot(cargoSlot, pinnedSourceSlots, sourceSlotFilter, sourceItemIdFilter, out _))
                continue;

            string itemId = ExtractSlotItemId(cargoSlot);
            if (string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM")
                continue;

            int sourceAmount;
            try { sourceAmount = cargoSlot.GetInt("Amount"); }
            catch { continue; }

            if (sourceAmount <= 0)
                continue;

            var targets = FindMatchingTargets(destination.Inventory, destination.Slots, itemId);
            if (targets.Count == 0)
                continue;

            int movedFromCargoSlot = TryMoveToInventory(
                sourceSlot: cargoSlot,
                sourceAmount: sourceAmount,
                itemId: itemId,
                destination: destination,
                allowNewSlots: true);

            if (movedFromCargoSlot <= 0)
                continue;

            int remaining = sourceAmount - movedFromCargoSlot;
            movedUnits += movedFromCargoSlot;
            touchedCargoSlots++;
            changed = true;

            if (remaining <= 0)
                cargoSlots.RemoveAt(cargoIndex);
            else
                cargoSlot.Set("Amount", remaining);
        }

        return changed;
    }

    private static List<ChestInventoryInfo> FindDestinationChests(List<ChestInventoryInfo> chestInventories, string itemId)
    {
        var withAvailableStack = new List<ChestInventoryInfo>();
        var withFreeSlot = new List<ChestInventoryInfo>();

        foreach (var chest in chestInventories)
        {
            var targets = FindMatchingTargets(chest.Inventory, chest.Slots, itemId);
            if (targets.Count == 0)
                continue;

            foreach (var target in targets)
            {
                if (target.Amount < target.MaxAmount)
                {
                    withAvailableStack.Add(chest);
                    goto NextChest;
                }
            }

            if (GetAvailableChestPositions(chest.Inventory, chest.Slots).Count > 0)
                withFreeSlot.Add(chest);

        NextChest:;
        }

        withAvailableStack.AddRange(withFreeSlot);
        return withAvailableStack;
    }

    private static List<ChestTarget> FindMatchingTargets(JsonObject inventory, JsonArray slots, string itemId)
    {
        var results = new List<ChestTarget>();

        for (int i = 0; i < slots.Length; i++)
        {
            JsonObject? slot;
            try { slot = slots.GetObject(i); }
            catch { continue; }
            if (slot == null || !IsSlotEnabled(inventory, slot))
                continue;

            string targetId = ExtractSlotItemId(slot);
            if (!string.Equals(targetId, itemId, StringComparison.OrdinalIgnoreCase))
                continue;

            int amount = GetAmount(slot);
            int max = GetMaxAmount(slot);
            if (amount < 0 || max <= 0)
                continue;

            results.Add(new ChestTarget
            {
                Slot = slot,
                SlotIndex = i,
                Amount = amount,
                MaxAmount = max,
            });
        }

        results.Sort((a, b) =>
        {
            int byAmount = b.Amount.CompareTo(a.Amount);
            return byAmount != 0 ? byAmount : a.SlotIndex.CompareTo(b.SlotIndex);
        });

        return results;
    }

    private static int TryMoveToInventory(JsonObject sourceSlot, int sourceAmount, string itemId, ChestInventoryInfo destination, bool allowNewSlots)
    {
        if (sourceAmount <= 0)
            return 0;

        var targets = FindMatchingTargets(destination.Inventory, destination.Slots, itemId);
        if (targets.Count == 0)
            return 0;

        int targetMaxAmount = targets[0].MaxAmount > 0 ? targets[0].MaxAmount : GetMaxAmount(sourceSlot);
        if (targetMaxAmount <= 0)
            targetMaxAmount = sourceAmount;

        int remaining = sourceAmount;
        int movedUnits = 0;
        foreach (var target in targets)
        {
            int transfer = Math.Min(remaining, target.MaxAmount - target.Amount);
            if (transfer <= 0)
                continue;

            target.Slot.Set("Amount", target.Amount + transfer);
            remaining -= transfer;
            movedUnits += transfer;
        }

        if (!allowNewSlots || remaining <= 0)
            return movedUnits;

        var freePositions = GetAvailableChestPositions(destination.Inventory, destination.Slots);
        foreach (var (x, y) in freePositions)
        {
            int transfer = Math.Min(remaining, targetMaxAmount);
            if (transfer <= 0)
                break;

            var newSlot = InventorySlotHelper.DuplicateSlot(sourceSlot, x, y);
            newSlot.Set("Amount", transfer);
            newSlot.Set("MaxAmount", targetMaxAmount);
            destination.Slots.Add(newSlot);
            remaining -= transfer;
            movedUnits += transfer;
        }

        return movedUnits;
    }

    private static List<(int x, int y)> GetAvailableChestPositions(JsonObject inventory, JsonArray slots)
    {
        var positions = new List<(int x, int y)>();
        var occupied = new HashSet<(int x, int y)>();

        for (int i = 0; i < slots.Length; i++)
        {
            JsonObject? slot;
            try { slot = slots.GetObject(i); }
            catch { continue; }
            if (slot == null) continue;

            if (TryGetSlotPosition(slot, out int slotX, out int slotY))
                occupied.Add((slotX, slotY));
        }

        var validSlots = inventory.GetArray("ValidSlotIndices");
        if (validSlots != null)
        {
            for (int i = 0; i < validSlots.Length; i++)
            {
                JsonObject? idx;
                try { idx = validSlots.GetObject(i); }
                catch { continue; }
                if (idx == null) continue;

                int x;
                int y;
                try
                {
                    x = idx.GetInt("X");
                    y = idx.GetInt("Y");
                }
                catch
                {
                    continue;
                }

                if (!occupied.Contains((x, y)))
                    positions.Add((x, y));
            }
        }
        else
        {
            int width = 0;
            int height = 0;
            try { width = inventory.GetInt("Width"); } catch { }
            try { height = inventory.GetInt("Height"); } catch { }

            if (width <= 0 || height <= 0)
            {
                int maxX = -1;
                int maxY = -1;
                foreach (var (occupiedX, occupiedY) in occupied)
                {
                    if (occupiedX > maxX) maxX = occupiedX;
                    if (occupiedY > maxY) maxY = occupiedY;
                }

                if (width <= 0) width = maxX >= 0 ? maxX + 1 : 0;
                if (height <= 0) height = maxY >= 0 ? maxY + 1 : 0;
            }

            if (width > 0 && height > 0)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!occupied.Contains((x, y)))
                            positions.Add((x, y));
                    }
                }
            }
        }

        positions.Sort((a, b) =>
        {
            int byY = a.y.CompareTo(b.y);
            return byY != 0 ? byY : a.x.CompareTo(b.x);
        });
        return positions;
    }

    private static bool IsSlotEnabled(JsonObject inventory, JsonObject slot)
    {
        if (!TryGetSlotPosition(slot, out int x, out int y))
            return false;

        var validSlots = inventory.GetArray("ValidSlotIndices");
        if (validSlots == null)
            return true;

        for (int i = 0; i < validSlots.Length; i++)
        {
            JsonObject? idx;
            try { idx = validSlots.GetObject(i); }
            catch { continue; }
            if (idx == null) continue;
            if (idx.GetInt("X") == x && idx.GetInt("Y") == y)
                return true;
        }

        return false;
    }

    private static bool TryGetSlotPosition(JsonObject slot, out int x, out int y)
    {
        x = 0;
        y = 0;

        try
        {
            var index = slot.GetObject("Index");
            if (index == null)
                return false;

            x = index.GetInt("X");
            y = index.GetInt("Y");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ShouldProcessSourceSlot(
        JsonObject slot,
        ISet<(int x, int y)>? pinnedSourceSlots,
        (int x, int y)? sourceSlotFilter,
        string? sourceItemIdFilter,
        out (int x, int y) sourcePosition)
    {
        sourcePosition = default;

        if (!TryGetSlotPosition(slot, out int srcX, out int srcY))
            return sourceSlotFilter == null;

        sourcePosition = (srcX, srcY);

        if (sourceSlotFilter != null && sourcePosition != sourceSlotFilter.Value)
            return false;

        if (pinnedSourceSlots != null && pinnedSourceSlots.Contains(sourcePosition))
            return false;

        if (string.IsNullOrEmpty(sourceItemIdFilter))
            return true;

        string slotItemId = ExtractSlotItemId(slot);
        return string.Equals(slotItemId, sourceItemIdFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTechnologySlot(JsonObject slot)
    {
        try
        {
            var type = slot.GetObject("Type");
            var inventoryType = type?.GetString("InventoryType") ?? "";
            return string.Equals(inventoryType, "Technology", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static int GetAmount(JsonObject slot)
    {
        try { return slot.GetInt("Amount"); }
        catch { return 0; }
    }

    private static int GetMaxAmount(JsonObject slot)
    {
        try { return slot.GetInt("MaxAmount"); }
        catch { return 0; }
    }

    private static string ExtractSlotItemId(JsonObject slot)
    {
        object? raw = slot.Get("Id");
        if (raw is JsonObject idObject)
            raw = idObject.Get("Id");

        string id = raw switch
        {
            BinaryData data => BinaryDataToItemId(data),
            string text => text,
            _ => "",
        };

        if (string.IsNullOrEmpty(id))
            return "";
        if (id[0] == '^')
            return id;
        return "^" + id;
    }

    private static string BinaryDataToItemId(BinaryData data)
    {
        var bytes = data.ToByteArray();
        var sb = new StringBuilder();
        bool afterHash = false;

        for (int i = 0; i < bytes.Length; i++)
        {
            int b = bytes[i] & 0xFF;
            if (i == 0)
            {
                if (b != 0x5E)
                    return data.ToString();
                sb.Append('^');
                continue;
            }

            if (b == 0x23)
            {
                sb.Append('#');
                afterHash = true;
                continue;
            }

            if (afterHash)
            {
                sb.Append((char)b);
                continue;
            }

            const string hexChars = "0123456789ABCDEF";
            sb.Append(hexChars[(b >> 4) & 0xF]);
            sb.Append(hexChars[b & 0xF]);
        }

        return sb.ToString();
    }
}
