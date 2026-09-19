using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NMSE.Data;
using NMSE.Core.Utilities;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>Which field to sort chest contents by in <see cref="InventoryBulkActions.SortAllChests"/>.</summary>
internal enum ChestSortMode
{
    Name,
    Type,
    Rarity,
    TypeThenRarity,
}

/// <summary>Outcome of a <see cref="InventoryBulkActions.SortAllChests"/> call.</summary>
internal sealed class ChestSortResult
{
    /// <summary>True if the sort was applied. False if there wasn't enough space (nothing was changed).</summary>
    public required bool Success { get; init; }

    /// <summary>Total number of item stacks placed (after merging matching items together).</summary>
    public int StacksPlaced { get; init; }

    /// <summary>Total slots available for placement across all 10 chests after padding was reserved.</summary>
    public int SlotsAvailable { get; init; }

    /// <summary>Number of previously-occupied slots freed by merging matching stacks together.</summary>
    public int SlotsFreed { get; init; }
}

/// <summary>Outcome of a <see cref="InventoryBulkActions.MergeAllChestsInPlace"/> call.</summary>
internal sealed class ChestMergeResult
{
    /// <summary>Total number of item stacks remaining across all 10 chests after merging.</summary>
    public int StacksRemaining { get; init; }

    /// <summary>Number of previously-occupied slots freed by merging matching stacks together.</summary>
    public int SlotsFreed { get; init; }
}

/// <summary>
/// Provides bulk inventory operations that work directly on save JSON data.
/// These mirror the per-inventory context menu actions in InventoryGridPanel
/// but execute across all relevant inventories in one pass.
/// </summary>
internal static class InventoryBulkActions
{
    // Inventory collection helpers

    /// <summary>
    /// Yields all technology inventory JSON objects from the save data.
    /// Includes Exosuit tech, all Multitools (Store), all Starships tech,
    /// Freighter tech, and all Exocraft tech.
    /// </summary>
    private static IEnumerable<JsonObject> EnumerateTechInventories(JsonObject playerState)
    {
        // Exosuit tech
        var inv = playerState.GetObject(ExosuitLogic.TechInventoryKey);
        if (inv != null) yield return inv;

        // Multitools: "Store" is tech (the editor treats it as tech inventory)
        var multitools = playerState.GetArray("Multitools");
        if (multitools != null)
        {
            for (int i = 0; i < multitools.Length; i++)
            {
                var tool = multitools.GetObject(i);
                if (tool == null) continue;
                inv = tool.GetObject("Store");
                if (inv != null) yield return inv;
            }
        }

        // Starships tech
        var ships = playerState.GetArray("ShipOwnership");
        if (ships != null)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                var ship = ships.GetObject(i);
                if (ship == null) continue;
                inv = ship.GetObject("Inventory_TechOnly");
                if (inv != null) yield return inv;
            }
        }

        // Freighter tech
        inv = playerState.GetObject("FreighterInventory_TechOnly");
        if (inv != null) yield return inv;

        // Exocraft tech
        var vehicles = playerState.GetArray("VehicleOwnership");
        if (vehicles != null)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                var vehicle = vehicles.GetObject(i);
                if (vehicle == null) continue;
                inv = vehicle.GetObject("Inventory_TechOnly");
                if (inv != null) yield return inv;
            }
        }
    }

    /// <summary>
    /// Yields all cargo (non-tech) inventory JSON objects from the save data.
    /// Includes Exosuit cargo, all Starships cargo, Freighter cargo,
    /// all Exocraft cargo, all 10 Chests, and all special Storage inventories.
    /// </summary>
    private static IEnumerable<JsonObject> EnumerateCargoInventories(JsonObject playerState)
    {
        // Exosuit cargo
        var inv = playerState.GetObject(ExosuitLogic.CargoInventoryKey);
        if (inv != null) yield return inv;

        // Starships cargo
        var ships = playerState.GetArray("ShipOwnership");
        if (ships != null)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                var ship = ships.GetObject(i);
                if (ship == null) continue;
                inv = ship.GetObject("Inventory");
                if (inv != null) yield return inv;
            }
        }

        // Freighter cargo
        inv = playerState.GetObject("FreighterInventory");
        if (inv != null) yield return inv;

        // Exocraft cargo
        var vehicles = playerState.GetArray("VehicleOwnership");
        if (vehicles != null)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                var vehicle = vehicles.GetObject(i);
                if (vehicle == null) continue;
                inv = vehicle.GetObject("Inventory");
                if (inv != null) yield return inv;
            }
        }

        // Standard chests (10)
        foreach (var key in BaseLogic.ChestInventoryKeys)
        {
            inv = playerState.GetObject(key);
            if (inv != null) yield return inv;
        }

        // Special storage inventories
        foreach (var (key, _, _) in BaseLogic.StorageInventories)
        {
            inv = playerState.GetObject(key);
            if (inv != null) yield return inv;
        }
    }

    /// <summary>
    /// Yields all inventory JSON objects (both cargo and tech) from the save data,
    /// excluding Chests and Storage inventories.
    /// Used for repair operations (chests/storage can't be damaged in-game).
    /// </summary>
    private static IEnumerable<JsonObject> EnumerateRepairableInventories(JsonObject playerState)
    {
        // Exosuit cargo + tech
        var inv = playerState.GetObject(ExosuitLogic.CargoInventoryKey);
        if (inv != null) yield return inv;
        inv = playerState.GetObject(ExosuitLogic.TechInventoryKey);
        if (inv != null) yield return inv;

        // Multitools: "Store" (tech inventory in the editor)
        var multitools = playerState.GetArray("Multitools");
        if (multitools != null)
        {
            for (int i = 0; i < multitools.Length; i++)
            {
                var tool = multitools.GetObject(i);
                if (tool == null) continue;
                inv = tool.GetObject("Store");
                if (inv != null) yield return inv;
            }
        }

        // Starships cargo + tech
        var ships = playerState.GetArray("ShipOwnership");
        if (ships != null)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                var ship = ships.GetObject(i);
                if (ship == null) continue;
                inv = ship.GetObject("Inventory");
                if (inv != null) yield return inv;
                inv = ship.GetObject("Inventory_TechOnly");
                if (inv != null) yield return inv;
            }
        }

        // Freighter cargo + tech
        inv = playerState.GetObject("FreighterInventory");
        if (inv != null) yield return inv;
        inv = playerState.GetObject("FreighterInventory_TechOnly");
        if (inv != null) yield return inv;

        // Exocraft cargo + tech
        var vehicles = playerState.GetArray("VehicleOwnership");
        if (vehicles != null)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                var vehicle = vehicles.GetObject(i);
                if (vehicle == null) continue;
                inv = vehicle.GetObject("Inventory");
                if (inv != null) yield return inv;
                inv = vehicle.GetObject("Inventory_TechOnly");
                if (inv != null) yield return inv;
            }
        }
    }

    // Bulk operations

    /// <summary>
    /// Recharges all chargeable technology items to their maximum charge amount
    /// across every technology inventory (Exosuit tech, Multitools, Starship tech,
    /// Freighter tech, and Exocraft tech).
    /// Equivalent to calling "Recharge All Technology" on each tech inventory.
    /// </summary>
    /// <returns>Total number of slots recharged.</returns>
    public static int RechargeAllTechnology(JsonObject playerState, GameItemDatabase database)
    {
        int recharged = 0;
        foreach (var inventory in EnumerateTechInventories(playerState))
        {
            recharged += RechargeInventoryTech(inventory, database);
        }
        return recharged;
    }

    /// <summary>
    /// Refills all stacks to their maximum amount across every cargo inventory
    /// (Exosuit cargo, Starship cargo, Freighter cargo, Exocraft cargo, Chests, and Storage).
    /// Equivalent to calling "Refill All Stacks" on each cargo inventory.
    /// </summary>
    /// <returns>Total number of slots refilled.</returns>
    public static int RefillAllStacks(JsonObject playerState, GameItemDatabase database)
    {
        int refilled = 0;
        foreach (var inventory in EnumerateCargoInventories(playerState))
        {
            refilled += RefillInventoryStacks(inventory);
        }
        return refilled;
    }

    /// <summary>
    /// Repairs all damaged slots across every inventory (cargo and tech), excluding
    /// Chests and Storage inventories (which can't be damaged in-game).
    /// Removes damage placeholders, clears DamageFactor, sets FullyInstalled,
    /// and removes BlockedByBrokenTech special slots.
    /// </summary>
    /// <returns>Total number of slots repaired.</returns>
    public static int RepairAllSlots(JsonObject playerState, GameItemDatabase database)
    {
        int repaired = 0;
        foreach (var inventory in EnumerateRepairableInventories(playerState))
        {
            repaired += RepairInventorySlots(inventory, database);
        }
        return repaired;
    }

    /// <summary>
    /// Repairs all damaged technology across every technology inventory (Exosuit tech,
    /// Multitools, Starship tech, Freighter tech, and Exocraft tech).
    /// Only repairs technology items (not regular cargo items) and only within
    /// technology inventories. Cargo inventories are not touched.
    /// </summary>
    /// <returns>Total number of technology items repaired.</returns>
    public static int RepairAllTechnology(JsonObject playerState, GameItemDatabase database)
    {
        int repaired = 0;
        foreach (var inventory in EnumerateTechInventories(playerState))
        {
            repaired += RepairInventoryTechnology(inventory, database);
        }
        return repaired;
    }

    // Per-inventory implementations

    /// <summary>
    /// Recharges all chargeable technology in a single inventory to max amount.
    /// Damaged items (Amount &lt; 0) are skipped - repair them first.
    /// Mirrors OnRechargeAllTech from InventoryGridPanel, operating on raw JSON.
    /// </summary>
    private static int RechargeInventoryTech(JsonObject inventory, GameItemDatabase database)
    {
        var slots = inventory.GetArray("Slots");
        if (slots == null) return 0;

        int recharged = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots.GetObject(i);
            if (slot == null) continue;

            string itemId = ReadSlotItemId(slot);
            if (string.IsNullOrEmpty(itemId)) continue;

			// Procedural tech items (e.g. ^UP_COLD3#84346) need the
			// seed suffix stripped for lookup, since the database does
			// not store the seeds the filter returned null.
			// Strip it to the base ID before lookup, mirroring the fallback
			// logic in InventoryGridPanel.ResolveGameItem.
			string lookupId = ProceduralSeedHelper.Strip(itemId).baseId;

            var gameItem = database.GetItem(lookupId);
            if (gameItem == null || !gameItem.IsChargeable) continue;

            int maxAmount = slot.GetInt("MaxAmount");
            if (maxAmount <= 0) continue;

            int currentAmount = slot.GetInt("Amount");
            // Damaged tech items carry Amount == -1; they must be repaired first.
            if (currentAmount < 0) continue;
            if (currentAmount >= maxAmount) continue;

            slot.Set("Amount", maxAmount);
            recharged++;
        }
        return recharged;
    }

    /// <summary>
    /// Refills all cargo item stacks in a single inventory to their max amount.
    /// Technology-type slots are skipped - they are recharged by
    /// <see cref="RechargeInventoryTech"/> instead.
    /// Mirrors OnRefillAllStacks from InventoryGridPanel, operating on raw JSON.
    /// </summary>
    private static int RefillInventoryStacks(JsonObject inventory)
    {
        var slots = inventory.GetArray("Slots");
        if (slots == null) return 0;

        int refilled = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots.GetObject(i);
            if (slot == null) continue;

            string itemId = ReadSlotItemId(slot);
            if (string.IsNullOrEmpty(itemId)) continue;

            // Technology-type items in cargo inventories (e.g. upgrade modules
            // installed in the general bag) carry charge amounts rather than
            // stack sizes. Skip them here; they belong to Recharge All Technology.
            string invType = ResolveInventoryTypeForSlot(slot, null);
            if (string.Equals(invType, "Technology", StringComparison.OrdinalIgnoreCase)) continue;

            int maxAmount = slot.GetInt("MaxAmount");
            if (maxAmount <= 0) continue;

            int currentAmount = slot.GetInt("Amount");
            if (currentAmount >= maxAmount) continue;

            slot.Set("Amount", maxAmount);
            refilled++;
        }
        return refilled;
    }

    /// <summary>
    /// Repairs all damaged slots in a single inventory. Removes damage placeholder
    /// items, clears DamageFactor, sets FullyInstalled, and removes
    /// BlockedByBrokenTech special slot entries.
    /// Mirrors OnRepairAllSlots from InventoryGridPanel, operating on raw JSON.
    /// </summary>
    private static int RepairInventorySlots(JsonObject inventory, GameItemDatabase database)
    {
        var slots = inventory.GetArray("Slots");
        if (slots == null) return 0;

        int repaired = 0;
        var indicesToRemove = new List<int>();

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots.GetObject(i);
            if (slot == null) continue;

            string itemId = ReadSlotItemId(slot);
            double damageFactor = slot.GetDouble("DamageFactor");

            // Track damage placeholder items for removal
            if (InventorySlotHelper.IsDamageSlotItem(itemId))
            {
                RepairSlot(slot, database);
                indicesToRemove.Add(i);
                repaired++;
                continue;
            }

            if (damageFactor > 0)
            {
                RepairSlot(slot, database);
                repaired++;
            }
        }

        // Remove damage placeholder slots from highest index first
        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            slots.RemoveAt(indicesToRemove[i]);

        // Remove BlockedByBrokenTech entries from SpecialSlots
        RemoveBlockedByBrokenTech(inventory);

        return repaired;
    }

    /// <summary>
    /// Repairs all damaged technology items in a single inventory. Only repairs
    /// items that have DamageFactor > 0 or are damage placeholders.
    /// Mirrors the technology-specific parts of repair logic.
    /// </summary>
    private static int RepairInventoryTechnology(JsonObject inventory, GameItemDatabase database)
    {
        var slots = inventory.GetArray("Slots");
        if (slots == null) return 0;

        int repaired = 0;
        var indicesToRemove = new List<int>();

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots.GetObject(i);
            if (slot == null) continue;

            string itemId = ReadSlotItemId(slot);

            // Damage placeholder items are always removed during tech repair
            if (InventorySlotHelper.IsDamageSlotItem(itemId))
            {
                RepairSlot(slot, database);
                indicesToRemove.Add(i);
                repaired++;
                continue;
            }

            double damageFactor = slot.GetDouble("DamageFactor");
            if (damageFactor <= 0) continue;

            // Only repair technology items
            var gameItem = database.GetItem(itemId);
            string invType = ResolveInventoryTypeForSlot(slot, gameItem);
            if (invType != "Technology") continue;

            RepairSlot(slot, database);
            repaired++;
        }

        // Remove damage placeholder slots from highest index first
        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            slots.RemoveAt(indicesToRemove[i]);

        // Remove BlockedByBrokenTech entries from SpecialSlots
        RemoveBlockedByBrokenTech(inventory);

        return repaired;
    }

    // Slot-level helpers

    /// <summary>
    /// Reads the item ID from a slot. Handles both the flat save-file format
    /// ("Id": "^ITEM") and the nested object format ("Id": {"Id": "^ITEM"}).
    /// BinaryData IDs (tech-pack packed bytes) are decoded to a hex string.
    /// Returns the raw ID string (e.g. "^HYPERDRIVE" or "^SHIPSLOT_DMG1"),
    /// or an empty string if the slot has no recognisable item ID.
    /// </summary>
    private static string ReadSlotItemId(JsonObject slot)
    {
        object? raw = slot.Get("Id");

        // Unwrap nested object format: { "Id": { "Id": "^ITEM" } }
        if (raw is JsonObject idObj)
            raw = idObj.Get("Id");

        return raw switch
        {
            BinaryData data => BinaryDataToItemId(data),
            string text     => text,
            _               => ""
        };
    }

    /// <summary>
    /// Repairs a single slot: clears DamageFactor, sets FullyInstalled, and
    /// restores Amount for damaged technology items.
    /// Mirrors RepairSlotData from InventoryGridPanel.
    /// </summary>
    private static void RepairSlot(JsonObject slot, GameItemDatabase database)
    {
        double damageFactor = slot.GetDouble("DamageFactor");
        bool wasDamaged = damageFactor > 0;

        try { slot.Set("DamageFactor", 0.0); } catch { }
        try { slot.Set("FullyInstalled", true); } catch { }

        // Fix Amount for damaged technology (Amount == -1 when damaged)
        if (wasDamaged)
        {
            int amount = slot.GetInt("Amount");
            if (amount < 0)
            {
                string itemId = ReadSlotItemId(slot);
                if (!string.IsNullOrEmpty(itemId))
                {
                    string lookupId = ProceduralSeedHelper.Strip(itemId).baseId;
                    var gameItem = database.GetItem(lookupId);
                    if (gameItem != null)
                    {
                        string invType = ResolveInventoryTypeForSlot(slot, gameItem);
                        if (invType == "Technology")
                        {
                            int techMaxAmount = gameItem.ChargeValue;
                            int repairedAmount = gameItem.BuildFullyCharged ? techMaxAmount : 0;
                            try { slot.Set("Amount", repairedAmount); } catch { }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolves the inventory type for a slot, checking the slot's Type property
    /// first, then falling back to the GameItem's ItemType.
    /// </summary>
    private static string ResolveInventoryTypeForSlot(JsonObject slot, GameItem? gameItem)
    {
        var typeObj = slot.GetObject("Type");
        if (typeObj != null)
        {
            string slotType = typeObj.GetString("InventoryType") ?? "";
            if (!string.IsNullOrEmpty(slotType))
                return slotType;
        }

        if (gameItem != null)
            return InventoryStackDatabase.ResolveInventoryTypeForItem(gameItem);

        return "Product";
    }

    /// <summary>
    /// Removes all BlockedByBrokenTech entries from an inventory's SpecialSlots array.
    /// </summary>
    private static void RemoveBlockedByBrokenTech(JsonObject inventory)
    {
        try
        {
            var specialSlots = inventory.GetArray("SpecialSlots");
            if (specialSlots == null) return;

            for (int i = specialSlots.Length - 1; i >= 0; i--)
            {
                var entry = specialSlots.GetObject(i);
                if (entry == null) continue;
                var typeObj = entry.GetObject("Type");
                if (typeObj == null) continue;
                string slotType = typeObj.GetString("InventorySpecialSlotType") ?? "";
                if (slotType == "BlockedByBrokenTech")
                    specialSlots.RemoveAt(i);
            }
        }
        catch { }
    }

    // Auto-stack (cross-inventory transfer) operations

    private sealed class DestinationInventoryInfo
    {
        public required JsonObject Inventory { get; init; }
        public required JsonArray Slots { get; init; }
        public int ChestIndex { get; init; }
    }

    private sealed class DestinationTarget
    {
        public required JsonObject Slot { get; init; }
        public int SlotIndex { get; init; }
        public int Amount { get; init; }
        public int MaxAmount { get; init; }
    }

    /// <summary>
    /// Moves item amounts from a source cargo inventory into existing matching stacks
    /// in Chest 1-10 inventories. Only chest stacks that already contain the same item
    /// are valid destinations; if a matching chest has spare capacity or free valid
    /// slots, new stacks may be created there.
    /// </summary>
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

        var chestInventories = new List<DestinationInventoryInfo>();
        for (int i = 0; i < BaseLogic.ChestInventoryKeys.Length; i++)
        {
            var chestInventory = playerState.GetObject(BaseLogic.ChestInventoryKeys[i]);
            var slots = chestInventory?.GetArray("Slots");
            if (chestInventory != null && slots != null)
            {
                chestInventories.Add(new DestinationInventoryInfo
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
            if (cargoSlot == null || IsAutoStackTechnologySlot(cargoSlot))
                continue;

            if (!ShouldProcessSourceSlot(cargoSlot, pinnedSourceSlots, sourceSlotFilter, sourceItemIdFilter, out _))
                continue;

            string itemId = ExtractAutoStackSlotItemId(cargoSlot);
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

    /// <summary>
    /// Moves item amounts from a source cargo inventory into existing matching stacks
    /// in the primary starship's cargo inventory.
    /// </summary>
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

    /// <summary>
    /// Moves item amounts from a source cargo inventory into existing matching stacks
    /// in the freighter's cargo inventory.
    /// </summary>
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

    /// <summary>
    /// Moves item amounts from a source inventory into existing matching stacks
    /// in a specified destination inventory.
    /// </summary>
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
        var destination = new DestinationInventoryInfo
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
            if (cargoSlot == null || IsAutoStackTechnologySlot(cargoSlot))
                continue;

            if (!ShouldProcessSourceSlot(cargoSlot, pinnedSourceSlots, sourceSlotFilter, sourceItemIdFilter, out _))
                continue;

            string itemId = ExtractAutoStackSlotItemId(cargoSlot);
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

    // Sort All Chests (cross-chest, stack-merging sort)

    private sealed class ChestSortGroup
    {
        public required string ItemId { get; init; }
        public required JsonObject Template { get; init; }
        public int TotalAmount { get; set; }
        public int MaxAmount { get; set; }
        public string SortName { get; set; } = "";
        public string SortType { get; set; } = "";
        public int SortRarityRank { get; set; }
    }

    /// <summary>
    /// Ascending rarity order (least to most rare) used by <see cref="ChestSortMode.Rarity"/>.
    /// Values not listed here (mission/weapon-only tags that don't apply to storable cargo)
    /// sort after every known rarity.
    /// </summary>
    private static readonly Dictionary<string, int> RarityRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VeryCommon"] = 0,
        ["Common"] = 1,
        ["Normal"] = 2,
        ["Uncommon"] = 3,
        ["Rare"] = 4,
        ["SuperRare"] = 5,
        ["VeryRare"] = 6,
        ["Epic"] = 7,
        ["Legendary"] = 8,
        ["Illegal"] = 9,
        ["Sentinel"] = 10,
        ["Impossible"] = 11,
        ["Always"] = 12,
    };

    /// <summary>
    /// Resolves the max stack size to use for a group when no source slot carried a usable
    /// MaxAmount (corrupted/hand-edited save) - falls back to the game's authoritative
    /// stack-size formula instead of trusting a missing/zero value, which would otherwise
    /// let a merge produce a stack exceeding the item's real in-game limit.
    /// </summary>
    private static int ResolveAuthoritativeMaxAmount(JsonObject templateSlot, GameItem? gameItem)
    {
        if (gameItem == null) return 0;
        string invType = ResolveInventoryTypeForSlot(templateSlot, gameItem);
        return InventoryStackDatabase.GetMaxAmount(gameItem, invType, "Chest");
    }

    private static readonly Regex TechPackHashPattern = new(@"^\^[0-9A-Fa-f]{12}$", RegexOptions.Compiled);

    /// <summary>Checks whether the given ID (without any #variant suffix) is a TechPack hash.</summary>
    private static bool IsTechPackHash(string baseId) => baseId.Length == 13 && TechPackHashPattern.IsMatch(baseId);

    /// <summary>
    /// Resolves a chest item ID to its <see cref="GameItem"/>, mirroring
    /// InventoryGridPanel.ResolveGameItem: strips any procedural #seed suffix, tries a direct
    /// database lookup, and falls back to the TechPacks hash table for IDs like
    /// "^808002C15CB6" that only resolve indirectly (e.g. certain upgrade modules). Without
    /// this fallback such items resolve to nothing, get an empty sort Type, and end up
    /// clustered at the very front of the sort instead of grouped with their real type.
    /// </summary>
    private static GameItem? ResolveChestGameItem(string itemId, GameItemDatabase database)
    {
        string baseId = ProceduralSeedHelper.Strip(itemId).baseId;

        var gameItem = database.GetItem(baseId);
        if (gameItem != null) return gameItem;

        if (IsTechPackHash(baseId) && TechPacks.Dictionary.TryGetValue(baseId, out var techPack))
            return database.GetItem(techPack.Id);

        return null;
    }

    /// <summary>
    /// Scans all 10 standard Chest inventories and groups every occupied slot by exact item ID
    /// (including any procedural #seed suffix, so distinct variants stay distinct), resolving
    /// each group's display name/type/rarity for sorting along the way.
    /// </summary>
    private static (Dictionary<string, ChestSortGroup> groups, List<string> order, int occupiedSlotCount) CollectChestGroups(
        IEnumerable<JsonObject?> chestInventories, GameItemDatabase database)
    {
        var groups = new Dictionary<string, ChestSortGroup>(StringComparer.OrdinalIgnoreCase);
        var groupOrder = new List<string>();
        int occupiedSlotCount = 0;

        foreach (var inv in chestInventories)
        {
            if (inv == null) continue;
            var slots = inv.GetArray("Slots");
            if (slots == null) continue;

            for (int i = 0; i < slots.Length; i++)
            {
                JsonObject? slot;
                try { slot = slots.GetObject(i); }
                catch { continue; }
                if (slot == null) continue;

                string itemId = ExtractAutoStackSlotItemId(slot);
                if (string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM")
                    continue;

                int amount;
                try { amount = slot.GetInt("Amount"); }
                catch { continue; }
                if (amount <= 0) continue;

                int maxAmount = 0;
                try { maxAmount = slot.GetInt("MaxAmount"); } catch { }

                occupiedSlotCount++;

                if (!groups.TryGetValue(itemId, out var group))
                {
                    group = new ChestSortGroup { ItemId = itemId, Template = slot };
                    groups[itemId] = group;
                    groupOrder.Add(itemId);
                }
                group.TotalAmount += amount;
                if (maxAmount > group.MaxAmount) group.MaxAmount = maxAmount;
            }
        }

        foreach (var group in groups.Values)
        {
            var gameItem = ResolveChestGameItem(group.ItemId, database);
            group.SortName = gameItem?.Name ?? group.ItemId;

            // ItemType is the item's database file/bucket (Products, Curiosities, Raw
            // Materials, Buildings, ...) - always populated, unlike the finer-grained
            // Category field (Fuel, Metal, Weapon, ...) which most crafted items simply
            // don't carry. Using ItemType consistently keeps every item's bucket the same
            // kind of thing instead of mixing two different granularities.
            group.SortType = gameItem?.ItemType ?? "";

            group.SortRarityRank = gameItem != null && RarityRank.TryGetValue(gameItem.Rarity, out int rank)
                ? rank
                : int.MaxValue;

            if (group.MaxAmount <= 0)
            {
                int authoritativeMax = ResolveAuthoritativeMaxAmount(group.Template, gameItem);
                if (authoritativeMax > 0) group.MaxAmount = authoritativeMax;
            }
        }

        return (groups, groupOrder, occupiedSlotCount);
    }

    /// <summary>
    /// Sorts items across all 10 standard Chest inventories as a single pool: matching items
    /// (same item ID, including any procedural seed) are first merged into as few stacks as
    /// possible (never exceeding each item's max stack size), the resulting stacks are sorted
    /// by name, type, rarity, or type then rarity, and then laid out across Chest 1-10 in order, filling one
    /// chest's slots before spilling into the next.
    /// </summary>
    /// <param name="playerState">The PlayerStateData JSON object.</param>
    /// <param name="database">Game item database, used to resolve names/categories for sorting.</param>
    /// <param name="mode">Whether to sort by item name, type, rarity, or type then rarity.</param>
    /// <param name="paddingPerChest">Number of trailing slots to leave empty at the end of each chest.</param>
    /// <returns>
    /// A <see cref="ChestSortResult"/> describing what happened. If there isn't enough room to
    /// place every stack given the requested padding, nothing is modified and Success is false.
    /// </returns>
    public static ChestSortResult SortAllChests(JsonObject playerState, GameItemDatabase database, ChestSortMode mode, int paddingPerChest)
    {
        if (paddingPerChest < 0) paddingPerChest = 0;

        var chestInventories = new List<JsonObject?>();
        var chestPositions = new List<List<(int x, int y)>>();
        foreach (var key in BaseLogic.ChestInventoryKeys)
        {
            var inv = playerState.GetObject(key);
            chestInventories.Add(inv);
            chestPositions.Add(inv != null ? GetAllValidPositions(inv) : new List<(int, int)>());
        }

        var (groups, groupOrder, occupiedSlotCount) = CollectChestGroups(chestInventories, database);

        if (groups.Count == 0)
            return new ChestSortResult { Success = true, StacksPlaced = 0, SlotsAvailable = 0, SlotsFreed = 0 };

        var sortedGroups = groupOrder.Select(id => groups[id]).ToList();
        switch (mode)
        {
            case ChestSortMode.Name:
                sortedGroups.Sort((a, b) =>
                {
                    int byName = string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
                    return byName != 0 ? byName : string.Compare(a.SortType, b.SortType, StringComparison.OrdinalIgnoreCase);
                });
                break;
            case ChestSortMode.Rarity:
                sortedGroups.Sort((a, b) =>
                {
                    int byRarity = a.SortRarityRank.CompareTo(b.SortRarityRank);
                    return byRarity != 0 ? byRarity : string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
                });
                break;
            case ChestSortMode.TypeThenRarity:
                sortedGroups.Sort((a, b) =>
                {
                    int byType = string.Compare(a.SortType, b.SortType, StringComparison.OrdinalIgnoreCase);
                    if (byType != 0) return byType;
                    int byRarity = a.SortRarityRank.CompareTo(b.SortRarityRank);
                    return byRarity != 0 ? byRarity : string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
                });
                break;
            default: // Type
                sortedGroups.Sort((a, b) =>
                {
                    int byType = string.Compare(a.SortType, b.SortType, StringComparison.OrdinalIgnoreCase);
                    return byType != 0 ? byType : string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
                });
                break;
        }

        // Expand each (now-merged) group into the minimum number of stacks, respecting
        // that item's max stack size - never combine more than MaxAmount into one slot.
        var stacks = new List<(string itemId, JsonObject template, int amount, int maxAmount)>();
        foreach (var group in sortedGroups)
        {
            int max = group.MaxAmount > 0 ? group.MaxAmount : group.TotalAmount;
            int remaining = group.TotalAmount;
            while (remaining > 0)
            {
                int take = Math.Min(remaining, max);
                stacks.Add((group.ItemId, group.Template, take, max));
                remaining -= take;
            }
        }

        // Reserve the requested padding at the end of each chest's slot list.
        var usablePositions = new List<List<(int x, int y)>>();
        int totalAvailable = 0;
        foreach (var positions in chestPositions)
        {
            int usableCount = Math.Max(0, positions.Count - paddingPerChest);
            var usable = positions.Take(usableCount).ToList();
            usablePositions.Add(usable);
            totalAvailable += usable.Count;
        }

        if (stacks.Count > totalAvailable)
        {
            return new ChestSortResult
            {
                Success = false,
                StacksPlaced = stacks.Count,
                SlotsAvailable = totalAvailable,
                SlotsFreed = 0,
            };
        }

        // Rebuild each chest's Slots array in sorted order.
        int cursor = 0;
        for (int i = 0; i < chestInventories.Count; i++)
        {
            var inv = chestInventories[i];
            if (inv == null) continue;

            var newSlots = new JsonArray();
            foreach (var (x, y) in usablePositions[i])
            {
                if (cursor >= stacks.Count) break;
                var s = stacks[cursor++];
                var newSlot = InventorySlotHelper.DuplicateSlot(s.template, x, y);
                newSlot.Set("Amount", s.amount);
                newSlot.Set("MaxAmount", s.maxAmount);
                newSlots.Add(newSlot);
            }

            inv.Set("Slots", newSlots);
        }

        return new ChestSortResult
        {
            Success = true,
            StacksPlaced = stacks.Count,
            SlotsAvailable = totalAvailable,
            SlotsFreed = Math.Max(0, occupiedSlotCount - stacks.Count),
        };
    }

    /// <summary>
    /// Merges matching item stacks across all 10 standard Chest inventories without reordering
    /// or resorting anything: each item's surviving stacks keep their original chest and slot
    /// position, only their Amount changes, and any now-redundant duplicate slots are removed
    /// to free space. Items with no duplicates elsewhere are left completely untouched.
    /// Unlike <see cref="SortAllChests"/>, this can never fail - it only ever removes slots,
    /// so there's no "not enough space" case and no padding concept.
    /// </summary>
    /// <param name="playerState">The PlayerStateData JSON object.</param>
    /// <param name="database">Game item database, used to resolve each item's max stack size.</param>
    public static ChestMergeResult MergeAllChestsInPlace(JsonObject playerState, GameItemDatabase database)
    {
        var chestSlotsArrays = new List<JsonArray?>();
        foreach (var key in BaseLogic.ChestInventoryKeys)
        {
            var inv = playerState.GetObject(key);
            chestSlotsArrays.Add(inv?.GetArray("Slots"));
        }

        // Collect every occupied slot, tagged with which chest/array-index it came from,
        // grouped by exact item ID in first-seen (i.e. current) order.
        var groups = new Dictionary<string, List<(int chestIdx, int arrayIdx, JsonObject slot, int amount, int maxAmount)>>(
            StringComparer.OrdinalIgnoreCase);
        int occupiedSlotCount = 0;

        for (int c = 0; c < chestSlotsArrays.Count; c++)
        {
            var slots = chestSlotsArrays[c];
            if (slots == null) continue;

            for (int i = 0; i < slots.Length; i++)
            {
                JsonObject? slot;
                try { slot = slots.GetObject(i); }
                catch { continue; }
                if (slot == null) continue;

                string itemId = ExtractAutoStackSlotItemId(slot);
                if (string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM")
                    continue;

                int amount;
                try { amount = slot.GetInt("Amount"); }
                catch { continue; }
                if (amount <= 0) continue;

                int maxAmount = 0;
                try { maxAmount = slot.GetInt("MaxAmount"); } catch { }

                occupiedSlotCount++;

                if (!groups.TryGetValue(itemId, out var list))
                {
                    list = new List<(int, int, JsonObject, int, int)>();
                    groups[itemId] = list;
                }
                list.Add((c, i, slot, amount, maxAmount));
            }
        }

        var slotsToRemovePerChest = new List<SortedSet<int>>();
        for (int c = 0; c < chestSlotsArrays.Count; c++)
            slotsToRemovePerChest.Add(new SortedSet<int>());

        foreach (var (itemId, entries) in groups)
        {
            if (entries.Count < 2) continue; // nothing to merge

            int total = entries.Sum(e => e.amount);
            int max = entries.Max(e => e.maxAmount);
            if (max <= 0)
            {
                var gameItem = ResolveChestGameItem(itemId, database);
                max = ResolveAuthoritativeMaxAmount(entries[0].slot, gameItem);
                if (max <= 0) max = total;
            }

            int neededStacks = (total + max - 1) / max; // ceiling division

            // neededStacks can only exceed entries.Count if the resolved max is smaller than
            // what's already crammed into an existing slot (only possible via the authoritative
            // fallback above, not the normal per-entry-derived max) - rebalancing would lose
            // quantity in that case, so leave everything untouched rather than risk data loss.
            if (neededStacks > entries.Count) continue;

            int remaining = total;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i < neededStacks)
                {
                    int take = Math.Min(remaining, max);
                    entries[i].slot.Set("Amount", take);
                    remaining -= take;
                }
                else
                {
                    // Surplus slot - its quantity was already folded into the surviving stacks above.
                    slotsToRemovePerChest[entries[i].chestIdx].Add(entries[i].arrayIdx);
                }
            }
        }

        int slotsFreed = 0;
        for (int c = 0; c < chestSlotsArrays.Count; c++)
        {
            var toRemove = slotsToRemovePerChest[c];
            if (toRemove.Count == 0) continue;

            var slots = chestSlotsArrays[c]!;
            foreach (int idx in toRemove.Reverse())
                slots.RemoveAt(idx);
            slotsFreed += toRemove.Count;
        }

        return new ChestMergeResult
        {
            StacksRemaining = occupiedSlotCount - slotsFreed,
            SlotsFreed = slotsFreed,
        };
    }

    /// <summary>
    /// Returns every valid (x, y) slot position for an inventory, sorted row-major
    /// (top-to-bottom, left-to-right), regardless of whether the slot is currently occupied.
    /// </summary>
    private static List<(int x, int y)> GetAllValidPositions(JsonObject inventory)
    {
        var positions = new List<(int x, int y)>();
        var seen = new HashSet<(int x, int y)>();

        var validSlots = inventory.GetArray("ValidSlotIndices");
        if (validSlots != null)
        {
            for (int i = 0; i < validSlots.Length; i++)
            {
                JsonObject? idx;
                try { idx = validSlots.GetObject(i); }
                catch { continue; }
                if (idx == null) continue;

                try
                {
                    var pos = (idx.GetInt("X"), idx.GetInt("Y"));
                    if (seen.Add(pos)) positions.Add(pos);
                }
                catch { }
            }
        }

        if (positions.Count == 0)
        {
            int width = 0, height = 0;
            try { width = inventory.GetInt("Width"); } catch { }
            try { height = inventory.GetInt("Height"); } catch { }
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    positions.Add((x, y));
        }

        positions.Sort((a, b) => a.Item2 != b.Item2 ? a.Item2.CompareTo(b.Item2) : a.Item1.CompareTo(b.Item1));
        return positions;
    }

    private static List<DestinationInventoryInfo> FindDestinationChests(List<DestinationInventoryInfo> chestInventories, string itemId)
    {
        var withAvailableStack = new List<DestinationInventoryInfo>();
        var withFreeSlot = new List<DestinationInventoryInfo>();

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

            if (GetAvailablePositions(chest.Inventory, chest.Slots).Count > 0)
                withFreeSlot.Add(chest);

        NextChest:;
        }

        withAvailableStack.AddRange(withFreeSlot);
        return withAvailableStack;
    }

    private static List<DestinationTarget> FindMatchingTargets(JsonObject inventory, JsonArray slots, string itemId)
    {
        var results = new List<DestinationTarget>();

        for (int i = 0; i < slots.Length; i++)
        {
            JsonObject? slot;
            try { slot = slots.GetObject(i); }
            catch { continue; }
            if (slot == null || !IsSlotEnabled(inventory, slot))
                continue;

            string targetId = ExtractAutoStackSlotItemId(slot);
            if (!string.Equals(targetId, itemId, StringComparison.OrdinalIgnoreCase))
                continue;

            int amount = GetAutoStackAmount(slot);
            int max = GetAutoStackMaxAmount(slot);
            if (amount < 0 || max <= 0)
                continue;

            results.Add(new DestinationTarget
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

    private static int TryMoveToInventory(JsonObject sourceSlot, int sourceAmount, string itemId, DestinationInventoryInfo destination, bool allowNewSlots)
    {
        if (sourceAmount <= 0)
            return 0;

        var targets = FindMatchingTargets(destination.Inventory, destination.Slots, itemId);
        if (targets.Count == 0)
            return 0;

        int targetMaxAmount = targets[0].MaxAmount > 0 ? targets[0].MaxAmount : GetAutoStackMaxAmount(sourceSlot);
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

        var freePositions = GetAvailablePositions(destination.Inventory, destination.Slots);
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

    private static List<(int x, int y)> GetAvailablePositions(JsonObject inventory, JsonArray slots)
    {
        var occupied = new HashSet<(int x, int y)>();
        for (int i = 0; i < slots.Length; i++)
        {
            JsonObject? slot;
            try { slot = slots.GetObject(i); }
            catch { continue; }
            if (slot == null) continue;

            if (TryGetAutoStackSlotPosition(slot, out int slotX, out int slotY))
                occupied.Add((slotX, slotY));
        }

        // GetAllValidPositions already covers ValidSlotIndices and an explicit Width/Height
        // grid. If neither yielded anything, infer a grid from the occupied slots themselves -
        // needed for legacy/malformed inventories with no size metadata at all.
        var allPositions = GetAllValidPositions(inventory);
        if (allPositions.Count == 0)
        {
            int maxX = -1, maxY = -1;
            foreach (var (occupiedX, occupiedY) in occupied)
            {
                if (occupiedX > maxX) maxX = occupiedX;
                if (occupiedY > maxY) maxY = occupiedY;
            }
            int width = maxX >= 0 ? maxX + 1 : 0;
            int height = maxY >= 0 ? maxY + 1 : 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    allPositions.Add((x, y));
        }

        // allPositions is already sorted row-major by GetAllValidPositions; Where preserves order.
        return allPositions.Where(p => !occupied.Contains(p)).ToList();
    }

    private static bool IsSlotEnabled(JsonObject inventory, JsonObject slot)
    {
        if (!TryGetAutoStackSlotPosition(slot, out int x, out int y))
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

    private static bool TryGetAutoStackSlotPosition(JsonObject slot, out int x, out int y)
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

        if (!TryGetAutoStackSlotPosition(slot, out int srcX, out int srcY))
            return sourceSlotFilter == null;

        sourcePosition = (srcX, srcY);

        if (sourceSlotFilter != null && sourcePosition != sourceSlotFilter.Value)
            return false;

        if (pinnedSourceSlots != null && pinnedSourceSlots.Contains(sourcePosition))
            return false;

        if (string.IsNullOrEmpty(sourceItemIdFilter))
            return true;

        string slotItemId = ExtractAutoStackSlotItemId(slot);
        return string.Equals(slotItemId, sourceItemIdFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAutoStackTechnologySlot(JsonObject slot)
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

    private static int GetAutoStackAmount(JsonObject slot)
    {
        try { return slot.GetInt("Amount"); }
        catch { return 0; }
    }

    private static int GetAutoStackMaxAmount(JsonObject slot)
    {
        try { return slot.GetInt("MaxAmount"); }
        catch { return 0; }
    }

    private static string ExtractAutoStackSlotItemId(JsonObject slot)
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
