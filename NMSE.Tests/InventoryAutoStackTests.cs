using NMSE.Core;
using NMSE.Models;

namespace NMSE.Tests;

public class InventoryAutoStackTests
{
    [Fact]
    public void AutoStackCargoToChests_UsesSingleDestinationChestAndCreatesRemainderThere()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^FERRITE", 0, 0, 300, 1000)
            ],
            chests:
            [
                [MakeSlot("^FERRITE", 0, 0, 900, 1000)],
                [],
                [],
                [],
                [MakeSlot("^FERRITE", 1, 0, 200, 1000)],
                [], [], [], [], []
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(300, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var chest1Slots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(2, chest1Slots.Length);
        Assert.Equal(1000, chest1Slots.GetObject(0).GetInt("Amount"));
        Assert.Equal("^FERRITE", chest1Slots.GetObject(1).GetString("Id"));
        Assert.Equal(200, chest1Slots.GetObject(1).GetInt("Amount"));

        var chest5Slot = playerState.GetObject("Chest5Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal(200, chest5Slot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_LeavesCargoWhenItemDoesNotExistInAnyChest()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 150, 500)
            ],
            chests:
            [
                [MakeSlot("^FERRITE", 0, 0, 50, 1000)],
                [], [], [], [], [], [], [], [], []
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.False(changed);
        Assert.Equal(0, movedUnits);
        Assert.Equal(0, touchedCargoSlots);

        var cargoSlot = playerState.GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal("^CARBON", cargoSlot.GetString("Id"));
        Assert.Equal(150, cargoSlot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_CreatesNewSlotInSameChestForRemainder()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^OXYGEN", 2, 1, 500, 1000)
            ],
            chests:
            [
                [MakeSlot("^OXYGEN", 0, 0, 950, 1000)],
                [], [], [], [], [], [], [], [], []
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(500, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var chestSlots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(2, chestSlots.Length);
        Assert.Equal(1000, chestSlots.GetObject(0).GetInt("Amount"));
        Assert.Equal(450, chestSlots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_CreatesNewSlotWhenMatchingDestinationStackIsAlreadyFull()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^OXYGEN", 0, 0, 300, 999)
            ],
            chests:
            [
                [MakeSlot("^OXYGEN", 0, 0, 999, 999)],
                [], [], [], [], [], [], [], [], []
            ],
            validChestSlots:
            [
                [(0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0), (6, 0)]
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(300, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var chestSlots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(2, chestSlots.Length);
        Assert.Equal(999, chestSlots.GetObject(0).GetInt("Amount"));
        Assert.Equal("^OXYGEN", chestSlots.GetObject(1).GetString("Id"));
        Assert.Equal(300, chestSlots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_CreatesNewSlotWhenValidSlotIndicesIsMissing()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^MICROCHIP", 0, 0, 300, 999)
            ],
            chests:
            [
                [MakeSlot("^MICROCHIP", 0, 0, 999, 999)],
                [], [], [], [], [], [], [], [], []
            ]);

        var chest1Inventory = playerState.GetObject("Chest1Inventory")!;
        chest1Inventory.Set("ValidSlotIndices", null);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(300, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var chestSlots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(2, chestSlots.Length);
        Assert.Equal(999, chestSlots.GetObject(0).GetInt("Amount"));
        Assert.Equal("^MICROCHIP", chestSlots.GetObject(1).GetString("Id"));
        Assert.Equal(300, chestSlots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_PrefersExistingStackInOtherChestBeforeCreatingNewSlot()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^MICROCHIP", 0, 0, 120, 999)
            ],
            chests:
            [
                [MakeSlot("^MICROCHIP", 0, 0, 999, 999)],
                [MakeSlot("^MICROCHIP", 1, 0, 40, 999)],
                [], [], [], [], [], [], [], []
            ],
            validChestSlots:
            [
                [(0, 0), (1, 0), (2, 0)],
                [(1, 0), (2, 0)]
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(120, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var chest1Slots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(1, chest1Slots.Length);
        Assert.Equal(999, chest1Slots.GetObject(0).GetInt("Amount"));

        var chest2Slots = playerState.GetObject("Chest2Inventory")!.GetArray("Slots")!;
        Assert.Equal(1, chest2Slots.Length);
        Assert.Equal(160, chest2Slots.GetObject(0).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_LeavesRemainderInCargoWhenDestinationChestHasNoFreeValidSlot()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^OXYGEN", 2, 1, 500, 1000)
            ],
            chests:
            [
                [MakeSlot("^OXYGEN", 0, 0, 950, 1000)]
            ],
            validChestSlots:
            [
                [(0, 0)]
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(50, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlot = playerState.GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal(450, cargoSlot.GetInt("Amount"));

        var chestSlot = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal(1000, chestSlot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_DoesNotUseBlockedMatchingSlotAsDestination()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 100, 500)
            ],
            chests:
            [
                [MakeSlot("^CARBON", 9, 9, 200, 500)]
            ],
            validChestSlots:
            [
                [(0, 0), (1, 0)]
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.False(changed);
        Assert.Equal(0, movedUnits);
        Assert.Equal(0, touchedCargoSlots);

        var cargoSlot = playerState.GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal(100, cargoSlot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_FallsBackToNextValidChestWhenFirstMatchingChestCannotReceive()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^SODIUM", 0, 0, 150, 999)
            ],
            chests:
            [
                [MakeSlot("^SODIUM", 0, 0, 999, 999)],
                [MakeSlot("^SODIUM", 1, 0, 999, 999)],
                [], [], [], [], [], [], [], []
            ],
            validChestSlots:
            [
                [(0, 0)],
                [(1, 0), (2, 0)]
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(150, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var chest1Slots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(1, chest1Slots.Length);
        Assert.Equal(999, chest1Slots.GetObject(0).GetInt("Amount"));

        var chest2Slots = playerState.GetObject("Chest2Inventory")!.GetArray("Slots")!;
        Assert.Equal(2, chest2Slots.Length);
        Assert.Equal(999, chest2Slots.GetObject(0).GetInt("Amount"));
        Assert.Equal("^SODIUM", chest2Slots.GetObject(1).GetString("Id"));
        Assert.Equal(150, chest2Slots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToStarship_MovesToCurrentShipAndCreatesRemainderSlot()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 150, 999)
            ],
            chests:
            [
                [], [], [], [], [], [], [], [], [], []
            ]);

        var ship = new JsonObject();
        ship.Add("Inventory", MakeInventory([MakeSlot("^CARBON", 0, 0, 900, 999)]));
        ship.Add("Inventory_TechOnly", MakeInventory([]));
        var ships = new JsonArray();
        ships.Add(ship);
        playerState.Add("ShipOwnership", ships);
        playerState.Add("PrimaryShip", 0);

        bool changed = InventoryBulkActions.AutoStackCargoToStarship(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.True(changed);
        Assert.Equal(150, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(0, cargoSlots.Length);

        var shipSlots = playerState.GetArray("ShipOwnership")!.GetObject(0).GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(2, shipSlots.Length);
        Assert.Equal(999, shipSlots.GetObject(0).GetInt("Amount"));
        Assert.Equal("^CARBON", shipSlots.GetObject(1).GetString("Id"));
        Assert.Equal(51, shipSlots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToFreighter_LeavesCargoWhenItemDoesNotExistInFreighter()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 150, 999)
            ],
            chests:
            [
                [], [], [], [], [], [], [], [], [], []
            ]);

        playerState.Add("FreighterInventory", MakeInventory([MakeSlot("^FERRITE", 0, 0, 400, 999)]));
        playerState.Add("FreighterInventory_TechOnly", MakeInventory([]));

        bool changed = InventoryBulkActions.AutoStackCargoToFreighter(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots);

        Assert.False(changed);
        Assert.Equal(0, movedUnits);
        Assert.Equal(0, touchedCargoSlots);

        var cargoSlot = playerState.GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal("^CARBON", cargoSlot.GetString("Id"));
        Assert.Equal(150, cargoSlot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToStarship_SkipsPinnedSourceSlots()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 120, 999)
            ],
            chests:
            [
                [], [], [], [], [], [], [], [], [], []
            ]);

        var ship = new JsonObject();
        ship.Add("Inventory", MakeInventory([MakeSlot("^CARBON", 0, 1, 500, 999)]));
        ship.Add("Inventory_TechOnly", MakeInventory([]));
        var ships = new JsonArray();
        ships.Add(ship);
        playerState.Add("ShipOwnership", ships);
        playerState.Add("PrimaryShip", 0);

        var pinned = new HashSet<(int x, int y)> { (0, 0) };
        bool changed = InventoryBulkActions.AutoStackCargoToStarship(playerState.GetObject("Inventory")!, playerState, out int movedUnits, out int touchedCargoSlots, pinned);

        Assert.False(changed);
        Assert.Equal(0, movedUnits);
        Assert.Equal(0, touchedCargoSlots);

        var cargoSlot = playerState.GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal(120, cargoSlot.GetInt("Amount"));

        var shipSlot = playerState.GetArray("ShipOwnership")!.GetObject(0).GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal(500, shipSlot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_WithSourceSlotFilter_MovesOnlySelectedSlot()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 100, 999),
                MakeSlot("^OXYGEN", 1, 0, 80, 999)
            ],
            chests:
            [
                [MakeSlot("^CARBON", 0, 0, 700, 999), MakeSlot("^OXYGEN", 1, 0, 600, 999)],
                [], [], [], [], [], [], [], [], []
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(
            playerState.GetObject("Inventory")!,
            playerState,
            out int movedUnits,
            out int touchedCargoSlots,
            sourceSlotFilter: (1, 0),
            sourceItemIdFilter: "^OXYGEN");

        Assert.True(changed);
        Assert.Equal(80, movedUnits);
        Assert.Equal(1, touchedCargoSlots);

        var cargoSlots = playerState.GetObject("Inventory")!.GetArray("Slots")!;
        Assert.Equal(1, cargoSlots.Length);
        Assert.Equal("^CARBON", cargoSlots.GetObject(0).GetString("Id"));
        Assert.Equal(100, cargoSlots.GetObject(0).GetInt("Amount"));

        var chestSlots = playerState.GetObject("Chest1Inventory")!.GetArray("Slots")!;
        Assert.Equal(680, chestSlots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackCargoToChests_WithSourceSlotFilterAndMismatchedItem_DoesNothing()
    {
        var playerState = CreatePlayerState(
            cargoSlots:
            [
                MakeSlot("^CARBON", 0, 0, 100, 999)
            ],
            chests:
            [
                [MakeSlot("^CARBON", 0, 0, 500, 999)],
                [], [], [], [], [], [], [], [], []
            ]);

        bool changed = InventoryBulkActions.AutoStackCargoToChests(
            playerState.GetObject("Inventory")!,
            playerState,
            out int movedUnits,
            out int touchedCargoSlots,
            sourceSlotFilter: (0, 0),
            sourceItemIdFilter: "^OXYGEN");

        Assert.False(changed);
        Assert.Equal(0, movedUnits);
        Assert.Equal(0, touchedCargoSlots);

        var cargoSlot = playerState.GetObject("Inventory")!.GetArray("Slots")!.GetObject(0);
        Assert.Equal("^CARBON", cargoSlot.GetString("Id"));
        Assert.Equal(100, cargoSlot.GetInt("Amount"));
    }

    [Fact]
    public void AutoStackFromInventoryToInventory_WithSourceSlotFilter_MovesOnlySelectedSlot()
    {
        var sourceInventory = MakeInventory(
        [
            MakeSlot("^CARBON", 0, 0, 100, 999),
            MakeSlot("^OXYGEN", 1, 0, 75, 999)
        ]);

        var destinationInventory = MakeInventory(
        [
            MakeSlot("^CARBON", 2, 0, 500, 999),
            MakeSlot("^OXYGEN", 3, 0, 600, 999)
        ]);

        bool changed = InventoryBulkActions.AutoStackFromInventoryToInventory(
            sourceInventory,
            destinationInventory,
            out int movedUnits,
            out int touchedSourceSlots,
            sourceSlotFilter: (1, 0),
            sourceItemIdFilter: "^OXYGEN");

        Assert.True(changed);
        Assert.Equal(75, movedUnits);
        Assert.Equal(1, touchedSourceSlots);

        var sourceSlots = sourceInventory.GetArray("Slots")!;
        Assert.Equal(1, sourceSlots.Length);
        Assert.Equal("^CARBON", sourceSlots.GetObject(0).GetString("Id"));
        Assert.Equal(100, sourceSlots.GetObject(0).GetInt("Amount"));

        var destinationSlots = destinationInventory.GetArray("Slots")!;
        Assert.Equal(675, destinationSlots.GetObject(1).GetInt("Amount"));
    }

    [Fact]
    public void AutoStackFromInventoryToInventory_WithPinnedSelectedSlot_DoesNothing()
    {
        var sourceInventory = MakeInventory(
        [
            MakeSlot("^OXYGEN", 1, 0, 75, 999)
        ]);

        var destinationInventory = MakeInventory(
        [
            MakeSlot("^OXYGEN", 3, 0, 600, 999)
        ]);

        var pinned = new HashSet<(int x, int y)> { (1, 0) };

        bool changed = InventoryBulkActions.AutoStackFromInventoryToInventory(
            sourceInventory,
            destinationInventory,
            out int movedUnits,
            out int touchedSourceSlots,
            pinned,
            sourceSlotFilter: (1, 0),
            sourceItemIdFilter: "^OXYGEN");

        Assert.False(changed);
        Assert.Equal(0, movedUnits);
        Assert.Equal(0, touchedSourceSlots);

        var sourceSlot = sourceInventory.GetArray("Slots")!.GetObject(0);
        Assert.Equal(75, sourceSlot.GetInt("Amount"));
        var destinationSlot = destinationInventory.GetArray("Slots")!.GetObject(0);
        Assert.Equal(600, destinationSlot.GetInt("Amount"));
    }

    private static JsonObject CreatePlayerState(List<JsonObject> cargoSlots, List<List<JsonObject>> chests, List<List<(int x, int y)>>? validChestSlots = null)
    {
        var playerState = new JsonObject();
        playerState.Add("Inventory", MakeInventory(cargoSlots));
        playerState.Add("Inventory_TechOnly", MakeInventory([]));

        for (int i = 0; i < 10; i++)
        {
            var chestSlots = i < chests.Count ? chests[i] : [];
            List<(int x, int y)>? validSlots = null;
            if (validChestSlots != null && i < validChestSlots.Count)
                validSlots = validChestSlots[i];
            playerState.Add($"Chest{i + 1}Inventory", MakeInventory(chestSlots, validSlots));
        }

        return playerState;
    }

    private static JsonObject MakeInventory(List<JsonObject> slots, List<(int x, int y)>? validSlots = null)
    {
        var inventory = new JsonObject();
        inventory.Add("Width", 10);
        inventory.Add("Height", 12);

        var slotArray = new JsonArray();
        foreach (var slot in slots)
            slotArray.Add(slot);
        inventory.Add("Slots", slotArray);

        var valid = new JsonArray();
        if (validSlots != null)
        {
            foreach (var (x, y) in validSlots)
            {
                var idx = new JsonObject();
                idx.Add("X", x);
                idx.Add("Y", y);
                valid.Add(idx);
            }
        }
        else
        {
            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    var idx = new JsonObject();
                    idx.Add("X", x);
                    idx.Add("Y", y);
                    valid.Add(idx);
                }
            }
        }
        inventory.Add("ValidSlotIndices", valid);
        return inventory;
    }

    private static JsonObject MakeSlot(string itemId, int x, int y, int amount, int maxAmount)
    {
        var slot = new JsonObject();
        var type = new JsonObject();
        type.Add("InventoryType", "Product");
        slot.Add("Type", type);
        slot.Add("Id", itemId);
        slot.Add("Amount", amount);
        slot.Add("MaxAmount", maxAmount);
        slot.Add("DamageFactor", 0.0);
        slot.Add("FullyInstalled", true);
        slot.Add("AddedAutomatically", false);

        var index = new JsonObject();
        index.Add("X", x);
        index.Add("Y", y);
        slot.Add("Index", index);
        return slot;
    }
}
