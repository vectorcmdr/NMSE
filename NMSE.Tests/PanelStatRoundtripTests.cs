using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Synthetic round-trip tests simulating the exact user flow:
/// 1. Load a save (or build a JSON object that mirrors one)
/// 2. Simulate editing ONE field in a panel
/// 3. Save back and verify the correct inventories are updated
///
/// Stats are written to Inventory + Inventory_TechOnly (for v400+ Waypoint).
/// Inventory_Cargo must NOT be touched.
/// </summary>
[Collection("MutableStaticDatabases")]
public class PanelStatRoundtripTests
{
    // =========================================================================
    //  STARSHIP PANEL
    // =========================================================================

    [Theory]
    [InlineData("^SHIP_DAMAGE", 0)]
    [InlineData("^SHIP_SHIELD", 1)]
    [InlineData("^SHIP_HYPERDRIVE", 2)]
    [InlineData("^SHIP_AGILE", 3)]
    public void StarshipPanel_EditOneStat_OnlyThatStatChanges(string _, int editedIndex)
    {
        // Build a ship with realistic stat values that differ per inventory
        double[] invStats = { 80.0, 35.0, 10.0, 50.0 };
        double[] techStats = { 63.73244094848633, 31.199888229370117, 6.988905429840088, 46.931243896484375 };
        double[] cargoStats = { 80.0, 35.0, 10.0, 50.0 };
        string[] statIds = { "^SHIP_DAMAGE", "^SHIP_SHIELD", "^SHIP_HYPERDRIVE", "^SHIP_AGILE" };

        var (ship, playerState) = BuildShipJson(statIds, invStats, techStats, cargoStats);

        // Simulate editing only the target stat to a new value
        double newValue = 123.456789012345;
        double[] uiValues = (double[])invStats.Clone();
        uiValues[editedIndex] = newValue;

        var rawStatValues = new Dictionary<string, double>();
        for (int i = 0; i < statIds.Length; i++)
            rawStatValues[statIds[i]] = invStats[i];

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "TestShip",
            ShipIndex = -1,
            Damage = uiValues[0],
            Shield = uiValues[1],
            Hyperdrive = uiValues[2],
            Maneuver = uiValues[3],
            RawStatValues = rawStatValues
        };

        StarshipLogic.SaveShipData(ship, playerState, values);

        // --- Verify primary Inventory ---
        for (int i = 0; i < statIds.Length; i++)
        {
            double saved = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), statIds[i]);
            if (i == editedIndex)
                Assert.Equal(newValue, saved); // Edited stat should have new value
            else
                Assert.Equal(invStats[i], saved); // Unedited stats preserved
        }

        // --- Verify Inventory_TechOnly receives same values as Inventory ---
        for (int i = 0; i < statIds.Length; i++)
        {
            double saved = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_TechOnly"), statIds[i]);
            if (i == editedIndex)
                Assert.Equal(newValue, saved); // Edited stat written to TechOnly too
            else
                Assert.Equal(invStats[i], saved); // Unedited stats written from Inventory
        }

        // --- Verify Inventory_Cargo is COMPLETELY UNTOUCHED ---
        for (int i = 0; i < statIds.Length; i++)
        {
            double saved = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_Cargo"), statIds[i]);
            Assert.Equal(cargoStats[i], saved);
        }

        // --- Verify RawDouble text preservation in untouched Cargo inventory ---
        VerifyRawDoublesPreserved(ship.GetObject("Inventory_Cargo")!, statIds);
    }

    [Fact]
    public void StarshipPanel_EditNoStats_NothingChanges()
    {
        double[] invStats = { 80.0, 35.0, 10.0, 50.0 };
        double[] techStats = { 63.73244094848633, 31.199888229370117, 6.988905429840088, 46.931243896484375 };
        double[] cargoStats = { 80.0, 35.0, 10.0, 50.0 };
        string[] statIds = { "^SHIP_DAMAGE", "^SHIP_SHIELD", "^SHIP_HYPERDRIVE", "^SHIP_AGILE" };

        var (ship, playerState) = BuildShipJson(statIds, invStats, techStats, cargoStats);

        var rawStatValues = new Dictionary<string, double>();
        for (int i = 0; i < statIds.Length; i++)
            rawStatValues[statIds[i]] = invStats[i];

        // All values unchanged
        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "TestShip",
            ShipIndex = -1,
            Damage = invStats[0],
            Shield = invStats[1],
            Hyperdrive = invStats[2],
            Maneuver = invStats[3],
            RawStatValues = rawStatValues
        };

        StarshipLogic.SaveShipData(ship, playerState, values);

        // Inventory and TechOnly should both have the primary Inventory values
        // (stats are always written to both). Cargo must be untouched.
        for (int i = 0; i < statIds.Length; i++)
        {
            Assert.Equal(invStats[i], StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), statIds[i]));
            Assert.Equal(invStats[i], StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_TechOnly"), statIds[i]));
            Assert.Equal(cargoStats[i], StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_Cargo"), statIds[i]));
        }

        // RawDoubles must be preserved in Cargo (untouched)
        VerifyRawDoublesPreserved(ship.GetObject("Inventory_Cargo")!, statIds);
    }

    // =========================================================================
    //  MULTITOOL PANEL
    // =========================================================================

    [Theory]
    [InlineData("^WEAPON_DAMAGE", 0)]
    [InlineData("^WEAPON_MINING", 1)]
    [InlineData("^WEAPON_SCAN", 2)]
    public void MultitoolPanel_EditOneStat_OnlyThatStatChanges(string _, int editedIndex)
    {
        double[] storeStats = { 44.876543210987654, 33.123456789012345, 22.987654321098765 };
        string[] statIds = { "^WEAPON_DAMAGE", "^WEAPON_MINING", "^WEAPON_SCAN" };

        var (tool, playerState) = BuildToolJson(statIds, storeStats);

        double newValue = 99.999;
        double[] uiValues = (double[])storeStats.Clone();
        uiValues[editedIndex] = newValue;

        var rawStatValues = new Dictionary<string, double>();
        for (int i = 0; i < statIds.Length; i++)
            rawStatValues[statIds[i]] = storeStats[i];

        var values = new MultitoolLogic.ToolSaveValues
        {
            Name = "TestTool",
            Damage = uiValues[0],
            Mining = uiValues[1],
            Scan = uiValues[2],
            RawStatValues = rawStatValues
        };

        MultitoolLogic.SaveToolData(tool, playerState, values, true);

        for (int i = 0; i < statIds.Length; i++)
        {
            double saved = StatHelper.ReadBaseStatValue(tool.GetObject("Store"), statIds[i]);
            if (i == editedIndex)
                Assert.Equal(newValue, saved);
            else
                Assert.Equal(storeStats[i], saved);
        }
    }

    // =========================================================================
    //  FREIGHTER PANEL
    // =========================================================================

    [Theory]
    [InlineData("^FREI_HYPERDRIVE", 0)]
    [InlineData("^FREI_FLEET", 1)]
    public void FreighterPanel_EditOneStat_OnlyThatStatChanges(string _, int editedIndex)
    {
        double[] invStats = { 79.93309020996094, 41.5 };
        double[] techStats = { 22.44556677889900, 15.123456789 };
        string[] statIds = { "^FREI_HYPERDRIVE", "^FREI_FLEET" };

        var playerState = BuildFreighterJson(statIds, invStats, techStats);

        double newValue = 100.0;
        double[] uiValues = (double[])invStats.Clone();
        uiValues[editedIndex] = newValue;

        var rawStatValues = new Dictionary<string, double>();
        for (int i = 0; i < statIds.Length; i++)
            rawStatValues[statIds[i]] = invStats[i];

        var values = new FreighterLogic.FreighterSaveValues
        {
            Name = "TestFreighter",
            Hyperdrive = uiValues[0],
            FleetCoordination = uiValues[1],
            RawStatValues = rawStatValues
        };

        FreighterLogic.SaveFreighterData(playerState, values);

        // Primary inventory
        for (int i = 0; i < statIds.Length; i++)
        {
            double saved = StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory"), statIds[i]);
            if (i == editedIndex)
                Assert.Equal(newValue, saved);
            else
                Assert.Equal(invStats[i], saved);
        }

        // Tech inventory receives same values as primary (stats written to both)
        for (int i = 0; i < statIds.Length; i++)
        {
            double saved = StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory_TechOnly"), statIds[i]);
            if (i == editedIndex)
                Assert.Equal(newValue, saved);
            else
                Assert.Equal(invStats[i], saved);
        }
    }

    [Fact]
    public void FreighterPanel_EditNoStats_NothingChanges()
    {
        double[] invStats = { 79.93309020996094, 41.5 };
        double[] techStats = { 22.44556677889900, 15.123456789 };
        string[] statIds = { "^FREI_HYPERDRIVE", "^FREI_FLEET" };

        var playerState = BuildFreighterJson(statIds, invStats, techStats);

        var rawStatValues = new Dictionary<string, double>();
        for (int i = 0; i < statIds.Length; i++)
            rawStatValues[statIds[i]] = invStats[i];

        var values = new FreighterLogic.FreighterSaveValues
        {
            Name = "TestFreighter",
            Hyperdrive = invStats[0],
            FleetCoordination = invStats[1],
            RawStatValues = rawStatValues
        };

        FreighterLogic.SaveFreighterData(playerState, values);

        // Both inventories should have primary values (stats written to both)
        for (int i = 0; i < statIds.Length; i++)
        {
            Assert.Equal(invStats[i], StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory"), statIds[i]));
            Assert.Equal(invStats[i], StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory_TechOnly"), statIds[i]));
        }
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    private static (JsonObject ship, JsonObject playerState) BuildShipJson(
        string[] statIds, double[] invStats, double[] techStats, double[] cargoStats)
    {
        var ship = new JsonObject();
        ship.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);

        ship.Add("Inventory", BuildInventory(statIds, invStats));
        ship.Add("Inventory_TechOnly", BuildInventory(statIds, techStats));
        ship.Add("Inventory_Cargo", BuildInventory(statIds, cargoStats));

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);
        return (ship, playerState);
    }

    private static (JsonObject tool, JsonObject playerState) BuildToolJson(
        string[] statIds, double[] storeStats)
    {
        var tool = new JsonObject();
        tool.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        tool.Add("Resource", resource);
        tool.Add("Store", BuildInventory(statIds, storeStats));

        var playerState = new JsonObject();
        playerState.Add("ActiveMultiTool", 0);
        return (tool, playerState);
    }

    private static JsonObject BuildFreighterJson(
        string[] statIds, double[] invStats, double[] techStats)
    {
        var playerState = new JsonObject();
        playerState.Add("PlayerFreighterName", "TestFreighter");
        playerState.Add("FreighterInventory", BuildInventory(statIds, invStats));
        playerState.Add("FreighterInventory_TechOnly", BuildInventory(statIds, techStats));
        return playerState;
    }

    private static JsonObject BuildInventory(string[] statIds, double[] values)
    {
        var inv = new JsonObject();
        var bsv = new JsonArray();
        for (int i = 0; i < statIds.Length; i++)
        {
            var entry = new JsonObject();
            entry.Add("BaseStatID", statIds[i]);
            // Use RawDouble to simulate a parsed save file
            string rawText = values[i].ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
            entry.Add("Value", new RawDouble(values[i], rawText));
            bsv.Add(entry);
        }
        inv.Add("BaseStatValues", bsv);
        return inv;
    }

    private static void VerifyRawDoublesPreserved(JsonObject inv, string[] statIds)
    {
        var bsv = inv.GetArray("BaseStatValues");
        if (bsv == null) return;
        for (int i = 0; i < bsv.Length; i++)
        {
            var entry = bsv.GetObject(i);
            string statId = entry.GetString("BaseStatID") ?? "";
            if (Array.IndexOf(statIds, statId) >= 0)
            {
                var val = entry.GetValue("Value");
                Assert.IsType<RawDouble>(val);
            }
        }
    }
}
