using System.Text;
using NMSE.Data;
using NMSE.Core.Utilities;
using NMSE.Models;

namespace NMSE.Core;

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
    /// Damaged items (Amount &lt; 0) are skipped — repair them first.
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

            var gameItem = database.GetItem(itemId);
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
    /// Technology-type slots are skipped — they are recharged by
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
                    var gameItem = database.GetItem(itemId);
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
            return InventoryStackDatabase.ResolveInventoryType(gameItem.ItemType);

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
        var positions = new List<(int x, int y)>();
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
