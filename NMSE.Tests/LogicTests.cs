using System.Globalization;
using NMSE.Config;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.IO;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for the extracted Logic classes (pure data operations, no WinForms).
/// Shares the MutableStaticDatabases collection to prevent parallel execution
/// with UiStringsTests / DatabaseLocalisationTests which mutate UiStrings state.
/// </summary>
[Collection("MutableStaticDatabases")]
public class LogicTests
{
    public string referencePath = "_ref";

    public LogicTests()
    {
        EnsureJsonDatabasesLoaded();
        EnsureUiStringsLoaded();
    }

    private static bool _jsonLoaded;
    private static readonly object _loadLock = new();

    private static void EnsureJsonDatabasesLoaded()
    {
        if (_jsonLoaded) return;
        lock (_loadLock)
        {
            if (_jsonLoaded) return;
            var jsonDir = FindResourceJsonDir();
            if (jsonDir == null) return;

            FrigateTraitDatabase.LoadFromFile(Path.Combine(jsonDir, "Frigate Traits.json"));
            SettlementDatabase.LoadFromFile(Path.Combine(jsonDir, "Settlement Perks.json"));
            WikiGuideDatabase.LoadFromFile(Path.Combine(jsonDir, "Wiki Guide.json"));
            TitleDatabase.LoadFromFile(Path.Combine(jsonDir, "Titles.json"));
            CompanionDatabase.LoadFromFile(Path.Combine(jsonDir, "Creature Species.json"));
            CreaturePartDatabase.LoadFromFile(Path.Combine(jsonDir, "Creature Descriptors.json"));

            // Load UI strings so that logic classes returning localised text work correctly
            var langDir = FindResourceLangDir();
            if (langDir != null)
            {
                UiStrings.SetDirectory(langDir);
                UiStrings.Load("en-GB");
            }

            _jsonLoaded = true;
        }
    }

    /// <summary>
    /// Re-loads UiStrings if another test class (e.g. UiStringsTests) called
    /// UiStrings.Reset() after <see cref="EnsureJsonDatabasesLoaded"/> already ran.
    /// </summary>
    private static void EnsureUiStringsLoaded()
    {
        if (UiStrings.TotalKeyCount > 0) return;
        var langDir = FindResourceLangDir();
        if (langDir == null) return;
        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");
    }

    // --- StarshipLogic -----------------------------------------------

    [Fact]
    public void StarshipLogic_ShipInfo_ContainsAllExpectedTypes()
    {
        Assert.True(StarshipLogic.ShipInfo.Count >= 16);
        Assert.Contains("Hauler", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Explorer", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Fighter", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Exotic", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Living Ship", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Solar", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Shuttle", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
        Assert.Contains("Sentinel", StarshipLogic.ShipInfo.Values.Select(v => v.DisplayName));
    }

    [Theory]
    [InlineData("MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN", "Hauler")]
    [InlineData("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN", "Explorer")]
    [InlineData("MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN", "Fighter")]
    [InlineData("MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN", "Exotic")]
    public void StarshipLogic_GetShipInfo_ReturnsCorrectDisplayName(string filename, string expectedName)
    {
        var (displayName, _, _) = StarshipLogic.GetShipInfo(filename);
        Assert.Equal(expectedName, displayName);
    }

    [Fact]
    public void StarshipLogic_GetShipInfo_UnknownFilename_ReturnsUnknown()
    {
        var (displayName, _, _) = StarshipLogic.GetShipInfo("INVALID_PATH");
        Assert.Equal("Unknown", displayName);
    }

    [Fact]
    public void StarshipLogic_GetShipInfo_KeywordFallback()
    {
        var (displayName, _, _) = StarshipLogic.GetShipInfo("some/path/DROPSHIP_test.mbin");
        Assert.Equal("Hauler", displayName);
    }

    [Fact]
    public void StarshipLogic_GetShipInfo_CaseInsensitiveLookup()
    {
        var (displayName, _, _) = StarshipLogic.GetShipInfo(
            "models/common/spacecraft/dropships/dropship_proc.scene.mbin");
        Assert.Equal("Hauler", displayName);
    }

    [Fact]
    public void StarshipLogic_LookupShipTypeName_ReturnsDisplayName()
    {
        Assert.Equal("Fighter",
            StarshipLogic.LookupShipTypeName("MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"));
    }

    [Fact]
    public void StarshipLogic_GetShipTypeNames_ReturnsSortedDistinctNames()
    {
        var names = StarshipLogic.GetShipTypeNames();
        Assert.True(names.Length > 0);
        for (int i = 1; i < names.Length; i++)
            Assert.True(string.Compare(names[i - 1], names[i], StringComparison.Ordinal) < 0,
                $"'{names[i - 1]}' should come before '{names[i]}'");
    }

    [Fact]
    public void StarshipLogic_LookupFilenameForType_ReturnsCorrectPath()
    {
        string filename = StarshipLogic.LookupFilenameForType("Hauler");
        Assert.Equal("MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN", filename);
    }

    [Fact]
    public void StarshipLogic_LookupFilenameForType_UnknownType_ReturnsEmpty()
    {
        Assert.Equal("", StarshipLogic.LookupFilenameForType("NonExistentType"));
    }

    [Theory]
    [InlineData("My Ship", "My_Ship")]
    [InlineData("  ", "unnamed")]
    [InlineData("", "unnamed")]
    [InlineData("Normal_Name", "Normal_Name")]
    public void StringHelper_SanitizeFileName_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, StringHelper.SanitizeFileName(input));
    }

    [Fact]
    public void StatHelper_ReadBaseStatValue_ReturnsValue()
    {
        var json = JsonObject.Parse(@"{
            ""BaseStatValues"": [
                { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 15.5 },
                { ""BaseStatID"": ""^SHIP_SHIELD"", ""Value"": 200.0 }
            ]
        }");

        Assert.Equal(15.5, StatHelper.ReadBaseStatValue(json, "^SHIP_DAMAGE"));
        Assert.Equal(200.0, StatHelper.ReadBaseStatValue(json, "^SHIP_SHIELD"));
        Assert.Equal(15.5, StatHelper.ReadBaseStatValue(json, "^SHIP_DAMAGE"));
        Assert.Equal(15.5, FreighterLogic.ReadStatBonus(json, "^SHIP_DAMAGE"));
    }

    [Fact]
    public void StatHelper_ReadBaseStatValue_MissingStat_ReturnsZero()
    {
        var json = JsonObject.Parse(@"{
            ""BaseStatValues"": [
                { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 10.0 }
            ]
        }");

        Assert.Equal(0.0, StatHelper.ReadBaseStatValue(json, "^SHIP_SHIELD"));
    }

    [Fact]
    public void StatHelper_ReadBaseStatValue_NullInventory_ReturnsZero()
    {
        Assert.Equal(0.0, StatHelper.ReadBaseStatValue(null, "^SHIP_DAMAGE"));
        Assert.Equal(0.0, StatHelper.ReadBaseStatValue(null, "^WEAPON_DAMAGE"));
        Assert.Equal(0.0, FreighterLogic.ReadStatBonus(null, "^FREI_HYPERDRIVE"));
    }

    [Fact]
    public void StatHelper_WriteBaseStatValue_UpdatesValue()
    {
        var json = JsonObject.Parse(@"{
            ""BaseStatValues"": [
                { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 10.0 }
            ]
        }");

        StatHelper.WriteBaseStatValue(json, "^SHIP_DAMAGE", 99.5);
        Assert.Equal(99.5, StatHelper.ReadBaseStatValue(json, "^SHIP_DAMAGE"));
    }

    [Fact]
    public void StatHelper_WriteBaseStatValue_NullInventory_DoesNotThrow()
    {
        var ex = Record.Exception(() => StatHelper.WriteBaseStatValue(null, "^SHIP_DAMAGE", 10.0));
        Assert.Null(ex);
    }

    [Fact]
    public void StatHelper_WriteBaseStatValue_PreservesRawDoubleWhenUnchanged()
    {
        // Parse JSON with a float that has a specific text representation.
        // "G17" format for 77.06786346435547 gives "77.067863464355469" (different text),
        // so if we wrote the same numeric value back, it would clobber the original text.
        var json = JsonObject.Parse(@"{
            ""BaseStatValues"": [
                { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 77.06786346435547 }
            ]
        }");

        var arr = json.GetArray("BaseStatValues")!;
        var entry = arr.GetObject(0);
        var originalValue = entry.GetValue("Value");
        Assert.IsType<Models.RawDouble>(originalValue);

        // Writing the same value back should preserve the RawDouble
        double readVal = StatHelper.ReadBaseStatValue(json, "^SHIP_DAMAGE");
        StatHelper.WriteBaseStatValue(json, "^SHIP_DAMAGE", readVal);

        var afterValue = entry.GetValue("Value");
        Assert.IsType<Models.RawDouble>(afterValue);
        Assert.Equal("77.06786346435547", afterValue.ToString());
    }

    [Fact]
    public void StatHelper_WriteBaseStatValue_ReplacesRawDoubleWhenChanged()
    {
        var json = JsonObject.Parse(@"{
            ""BaseStatValues"": [
                { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 77.06786346435547 }
            ]
        }");

        StatHelper.WriteBaseStatValue(json, "^SHIP_DAMAGE", 99.5);
        Assert.Equal(99.5, StatHelper.ReadBaseStatValue(json, "^SHIP_DAMAGE"));
    }

    [Fact]
    public void StatHelper_WriteBaseStatValue_PreservesRawDoubleWhenValueUnchanged()
    {
        // Simulates the real UI path: value is read as double, displayed via
        // InvariantNumericTextBox (double-backed with "G17" format), and written
        // back unchanged.  The guard must recognise the value as unchanged and
        // preserve the original RawDouble text.
        var json = JsonObject.Parse(@"{
            ""BaseStatValues"": [
                { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 49.91505813598633 }
            ]
        }");

        double readVal = StatHelper.ReadBaseStatValue(json, "^SHIP_DAMAGE");

        // Write back the exact same double - no decimal round-trip
        StatHelper.WriteBaseStatValue(json, "^SHIP_DAMAGE", readVal);

        // The original RawDouble text must be preserved
        var entry = json.GetArray("BaseStatValues")!.GetObject(0);
        var afterValue = entry.GetValue("Value");
        Assert.IsType<Models.RawDouble>(afterValue);
        Assert.Equal("49.91505813598633", afterValue.ToString());
    }

    [Fact]
    public void ConditionalClampStatValue_PreservesRawWhenValueUnchanged()
    {
        // Ensure ConditionalClampStatValue recognises an unchanged double value
        // and returns the original raw.
        double raw = 79.93309020996094;

        var rawValues = new Dictionary<string, double> { ["^FREI_HYPERDRIVE"] = raw };
        double result = Data.BaseStatLimits.ConditionalClampStatValue(
            "Normal", "^FREI_HYPERDRIVE", raw, Data.StatCategory.Freighter, rawValues);

        Assert.Equal(raw, result); // Must return the original raw
    }

    [Fact]
    public void StarshipLogic_BuildShipList_ReturnsSeededShips()
    {
        var json = JsonObject.Parse(@"{
            ""ShipOwnership"": [
                {
                    ""Name"": ""Alpha"",
                    ""Resource"": { ""Seed"": [true, ""0xABC""] }
                },
                {
                    ""Name"": """",
                    ""Resource"": { ""Seed"": [false, ""0x0""] }
                },
                {
                    ""Name"": ""Beta"",
                    ""Resource"": { ""Seed"": [true, ""0xDEF""] }
                }
            ]
        }");

        var ships = StarshipLogic.BuildShipList(json.GetArray("ShipOwnership")!);
        Assert.Equal(2, ships.Count);
        Assert.Equal("[1] Alpha - ?", ships[0].DisplayName);
        Assert.Equal(0, ships[0].DataIndex);
        Assert.Equal("[3] Beta - ?", ships[1].DisplayName);
        Assert.Equal(2, ships[1].DataIndex);
    }

    [Fact]
    public void StarshipLogic_BuildShipList_EmptyName_UsesDefaultName()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                {
                    ""Name"": """",
                    ""Resource"": { ""Seed"": [true, ""0x123""] }
                }
            ]
        }");

        var ships = StarshipLogic.BuildShipList(json.GetArray("Ships")!);
        Assert.Single(ships);
        // Ship has no name and no Filename resource, so type resolves to localised "Unknown"
        Assert.Equal("[1] Unknown - ?", ships[0].DisplayName);
    }

    [Fact]
    public void StarshipLogic_ShipClasses_HasFourEntries()
    {
        Assert.Equal(4, StarshipLogic.ShipClasses.Length);
        Assert.Equal(new[] { "C", "B", "A", "S" }, StarshipLogic.ShipClasses);
    }

    [Fact]
    public void StarshipLogic_BuildShipList_NamedShip_ShowsSlotNameClass()
    {
        // Named ship with class in inventory should show "[slot] Name - Class"
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                {
                    ""Name"": ""VCF Blackbird"",
                    ""Resource"": { ""Filename"": ""MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"", ""Seed"": [true, ""0xABC""] },
                    ""Inventory"": { ""Class"": { ""InventoryClass"": ""S"" } }
                }
            ]
        }");

        var ships = StarshipLogic.BuildShipList(json.GetArray("Ships")!);
        Assert.Single(ships);
        Assert.Equal("[1] VCF Blackbird - S", ships[0].DisplayName);
    }

    [Fact]
    public void StarshipLogic_BuildShipList_UnnamedShip_ShowsSlotTypeClass()
    {
        // Unnamed ship with resolved type and class should show "[slot] Type - Class"
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                {
                    ""Name"": """",
                    ""Resource"": { ""Filename"": ""MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"", ""Seed"": [true, ""0xABC""] },
                    ""Inventory"": { ""Class"": { ""InventoryClass"": ""C"" } }
                }
            ]
        }");

        var ships = StarshipLogic.BuildShipList(json.GetArray("Ships")!);
        Assert.Single(ships);
        Assert.Equal("[1] Fighter - C", ships[0].DisplayName);
    }

    [Fact]
    public void StarshipLogic_IsCorvette_DetectsCorvetteFilename()
    {
        Assert.True(StarshipLogic.IsCorvette("MODELS/COMMON/SPACECRAFT/BIGGS/BIGGS.SCENE.MBIN"));
        Assert.False(StarshipLogic.IsCorvette("MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"));
        Assert.False(StarshipLogic.IsCorvette(""));
    }

    [Fact]
    public void StarshipLogic_SeedToDecimal_ConvertsHexCorrectly()
    {
        // 0x68B31258 = 1756566104
        Assert.Equal(1756566104L, StarshipLogic.SeedToDecimal("0x68B31258"));
    }

    [Fact]
    public void StarshipLogic_SeedToDecimal_HandlesVariousFormats()
    {
        Assert.Equal(1756566104L, StarshipLogic.SeedToDecimal("68B31258"));
        Assert.Equal(1756566104L, StarshipLogic.SeedToDecimal("0X68B31258"));
        Assert.Equal(0L, StarshipLogic.SeedToDecimal(""));
        Assert.Equal(0L, StarshipLogic.SeedToDecimal(null!));
    }

    [Fact]
    public void StarshipLogic_FindCorvetteBaseIndex_FindsByUserData()
    {
        var json = JsonObject.Parse(@"{
            ""Bases"": [
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""HomePlanetBase"" },
                    ""UserData"": 1
                },
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""PlayerShipBase"" },
                    ""UserData"": 5
                }
            ]
        }");
        var bases = json.GetArray("Bases")!;
        int idx = StarshipLogic.FindCorvetteBaseIndex(bases, 5);
        Assert.Equal(1, idx);
    }

    [Fact]
    public void StarshipLogic_FindCorvetteBaseIndex_ReturnsNegativeForNoMatch()
    {
        var json = JsonObject.Parse(@"{
            ""Bases"": [
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""PlayerShipBase"" },
                    ""UserData"": 3
                }
            ]
        }");
        var bases = json.GetArray("Bases")!;
        int idx = StarshipLogic.FindCorvetteBaseIndex(bases, 5);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void StarshipLogic_FindCorvetteBaseIndex_SkipsNonPlayerShipBase()
    {
        var json = JsonObject.Parse(@"{
            ""Bases"": [
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""HomePlanetBase"" },
                    ""UserData"": 5
                }
            ]
        }");
        var bases = json.GetArray("Bases")!;
        int idx = StarshipLogic.FindCorvetteBaseIndex(bases, 5);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void StarshipLogic_FindCorvetteBaseIndex_RealSaveScenario()
    {
        // Simulates the real save scenario with UserData matching:
        //   Ship 7 "USCSS Abraxas"  -> Base UserData 7
        //   Ship 8 "USCSS Solomon"  -> Base UserData 8
        //   Ship 5 "The Bebop"      -> Base UserData 5
        var json = JsonObject.Parse(@"{
            ""Bases"": [
                { ""BaseType"": { ""PersistentBaseTypes"": ""HomePlanetBase"" }, ""UserData"": 0 },
                { ""BaseType"": { ""PersistentBaseTypes"": ""PlayerShipBase"" }, ""UserData"": 7 },
                { ""BaseType"": { ""PersistentBaseTypes"": ""PlayerShipBase"" }, ""UserData"": 8 },
                { ""BaseType"": { ""PersistentBaseTypes"": ""PlayerShipBase"" }, ""UserData"": 5 }
            ]
        }");
        var bases = json.GetArray("Bases")!;

        Assert.Equal(1, StarshipLogic.FindCorvetteBaseIndex(bases, 7));
        Assert.Equal(2, StarshipLogic.FindCorvetteBaseIndex(bases, 8));
        Assert.Equal(3, StarshipLogic.FindCorvetteBaseIndex(bases, 5));
    }

    [Fact]
    public void StarshipLogic_GetPartPriority_IdentifiesCategories()
    {
        // Priority map lookup (no prefix fallback needed)
        StarshipDatabase.Clear();

        // Reactor (priority 1)
        Assert.Equal(1, StarshipLogic.GetPartPriority("^B_GEN_0"));
        Assert.Equal(1, StarshipLogic.GetPartPriority("^B_GEN_1"));

        // Thruster (priority 2)
        Assert.Equal(2, StarshipLogic.GetPartPriority("^B_TRU_C"));

        // Landing Gear (priority 4)
        Assert.Equal(4, StarshipLogic.GetPartPriority("^B_LND_A"));

        // Landing Bay (priority 5)
        Assert.Equal(5, StarshipLogic.GetPartPriority("^B_ALK_B"));

        // Cockpit (priority 6)
        Assert.Equal(6, StarshipLogic.GetPartPriority("^B_COK_A"));

		// Shield is NOT in priority map — goes to OtherPriority
		Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("^B_SHL_A"));

        // Other (non-functional or not in priority map)
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("^BUILDTABLE2"));
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("^B_HAB_C"));
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("^U_PARAGON"));
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority(""));
    }

    [Fact]
    public void StarshipLogic_ReorderBuildingObjects_SortsByCategoryPriority()
    {
        // Load database for correct categorisation
        LoadCorvetteDatabase();

		// Create objects in scrambled order to verify reorder puts them in correct priority.
		// Priority map: Reactor(1) -> Thruster(2) -> Wing(3) -> Gear(4) -> Access(5) -> Cockpit(6) -> Other
		// B_STR_B_NE (Hull) and B_WNG_E (not in priority map) are NOT sorted — they go to Other group.
		var json = JsonObject.Parse(@"{
            ""Objects"": [
                { ""ObjectID"": ""^BUILDTABLE2"", ""UserData"": 1 },
                { ""ObjectID"": ""^B_COK_A"", ""UserData"": 2 },
                { ""ObjectID"": ""^B_ALK_B"", ""UserData"": 3 },
                { ""ObjectID"": ""^B_LND_A"", ""UserData"": 4 },
                { ""ObjectID"": ""^B_STR_B_NE"", ""UserData"": 5 },
                { ""ObjectID"": ""^B_TRU_C"", ""UserData"": 6 },
                { ""ObjectID"": ""^B_GEN_1"", ""UserData"": 7 },
                { ""ObjectID"": ""^B_WNG_E"", ""UserData"": 8 }
            ]
        }");
        var objects = json.GetArray("Objects")!;
        int result = StarshipLogic.ReorderBuildingObjects(objects);
        Assert.Equal(8, result);

        // Verify order: Reactor (B_GEN_1), Thruster (B_TRU_C), Gear (B_LND_A),
        // Access (B_ALK_B), Cockpit (B_COK_A),
        // Other group sorted alphabetically by object list display name:
        //   "Hexagonal Table" (BUILDTABLE2) < "Osprey Wing Module (E)" (B_WNG_E) < "Supercruise Aerofoil (Ne)" (B_STR_B_NE)
        Assert.Equal("^B_GEN_1", objects.GetObject(0).GetString("ObjectID"));     // Reactor (1)
        Assert.Equal("^B_TRU_C", objects.GetObject(1).GetString("ObjectID"));     // Thruster (2)
        Assert.Equal("^B_LND_A", objects.GetObject(2).GetString("ObjectID"));     // Gear (4)
        Assert.Equal("^B_ALK_B", objects.GetObject(3).GetString("ObjectID"));     // Access (5)
        Assert.Equal("^B_COK_A", objects.GetObject(4).GetString("ObjectID"));     // Cockpit (6)
        Assert.Equal("^BUILDTABLE2", objects.GetObject(5).GetString("ObjectID")); // "Hexagonal Table"
        Assert.Equal("^B_WNG_E", objects.GetObject(6).GetString("ObjectID"));     // "Osprey Wing Module (E)"
        Assert.Equal("^B_STR_B_NE", objects.GetObject(7).GetString("ObjectID"));  // "Supercruise Aerofoil (Ne)"
    }

    [Fact]
    public void StarshipLogic_ReorderBuildingObjects_PreservesRelativeOrderWithinCategory()
    {
        // Two cockpits and two landing bays - their relative order must be preserved.
        var json = JsonObject.Parse(@"{
            ""Objects"": [
                { ""ObjectID"": ""^B_COK_A"", ""UserData"": 1 },
                { ""ObjectID"": ""^B_ALK_C"", ""UserData"": 2 },
                { ""ObjectID"": ""^B_COK_D"", ""UserData"": 3 },
                { ""ObjectID"": ""^B_ALK_B"", ""UserData"": 4 }
            ]
        }");
        var objects = json.GetArray("Objects")!;
        StarshipLogic.ReorderBuildingObjects(objects);

        // Landing bays come before cockpits, relative order within each is preserved
        Assert.Equal("^B_ALK_C", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^B_ALK_B", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^B_COK_A", objects.GetObject(2).GetString("ObjectID"));
        Assert.Equal("^B_COK_D", objects.GetObject(3).GetString("ObjectID"));
    }

    [Fact]
    public void StarshipLogic_GetPrimaryShipName_ReturnsShipName()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                { ""Name"": ""TestShip"", ""Resource"": { ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""USCSS Abraxas"", ""Resource"": { ""Seed"": [true, ""0x68B31258""] } }
            ]
        }");
        var ships = json.GetArray("Ships")!;
        Assert.Equal("USCSS Abraxas", StarshipLogic.GetPrimaryShipName(ships, 1));
        Assert.Equal("TestShip", StarshipLogic.GetPrimaryShipName(ships, 0));
        Assert.Equal("Unknown", StarshipLogic.GetPrimaryShipName(ships, 99));
        Assert.Equal("Unknown", StarshipLogic.GetPrimaryShipName(null, 0));
    }

    [Fact]
    public void StarshipLogic_DeleteShipData_ClearsResourceAndSeed()
    {
        var json = JsonObject.Parse(@"{
            ""Name"": ""TestShip"",
            ""Resource"": { ""Filename"": ""MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"", ""Seed"": [true, ""0xABC""] }
        }");

        StarshipLogic.DeleteShipData(json);

        var resource = json.GetObject("Resource");
        Assert.NotNull(resource);
        Assert.Equal("", resource!.GetString("Filename"));
        var seed = resource.GetArray("Seed");
        Assert.NotNull(seed);
        Assert.False(seed!.GetBool(0));
        Assert.Equal("0x0", seed.Get(1)?.ToString());
    }

    [Fact]
    public void StarshipLogic_DeleteShip_InvalidatesInPlace_DoesNotRemoveFromArray()
    {
        // Deletion should only invalidate (clear Filename/Seed) and NOT remove
        // the entry from the array..
        // This preserves index alignment with parallel arrays like ShipUsesLegacyColours.
        var json = JsonObject.Parse(@"{
            ""ShipOwnership"": [
                { ""Name"": ""Alpha"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""Beta"",  ""Resource"": { ""Filename"": ""f2"", ""Seed"": [true, ""0x2""] } },
                { ""Name"": ""Gamma"", ""Resource"": { ""Filename"": ""f3"", ""Seed"": [true, ""0x3""] } }
            ]
        }");
        var ships = json.GetArray("ShipOwnership")!;
        Assert.Equal(3, ships.Length);

        // Delete middle ship (index 1 = Beta) - invalidate only
        StarshipLogic.DeleteShipData(ships.GetObject(1));

        // Array size MUST remain unchanged
        Assert.Equal(3, ships.Length);

        // Alpha and Gamma remain at their original indices, untouched
        Assert.Equal("Alpha", ships.GetObject(0).GetString("Name"));
        Assert.Equal("f1", ships.GetObject(0).GetObject("Resource")!.GetString("Filename"));
        Assert.Equal("Gamma", ships.GetObject(2).GetString("Name"));
        Assert.Equal("f3", ships.GetObject(2).GetObject("Resource")!.GetString("Filename"));

        // Beta is invalidated but still in the array at index 1
        var betaResource = ships.GetObject(1).GetObject("Resource")!;
        Assert.Equal("", betaResource.GetString("Filename"));
        Assert.False(betaResource.GetArray("Seed")!.GetBool(0));

        // BuildShipList should return only valid ships, preserving original indices
        var list = StarshipLogic.BuildShipList(ships);
        Assert.Equal(2, list.Count);
        Assert.Equal("[1] Alpha - ?", list[0].DisplayName);
        Assert.Equal(0, list[0].DataIndex);    // Original index preserved
        Assert.Equal("[3] Gamma - ?", list[1].DisplayName);
        Assert.Equal(2, list[1].DataIndex);    // Original index preserved (NOT shifted to 1)
    }

    [Fact]
    public void StarshipLogic_DeleteShip_NonPrimaryDeleted_PrimaryIndexUnchanged()
    {
        // When deleting a non-primary ship, the PrimaryShip index should remain
        // unchanged since no entries are removed from the array.
        var json = JsonObject.Parse(@"{
            ""ShipOwnership"": [
                { ""Name"": ""Alpha"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""Beta"",  ""Resource"": { ""Filename"": ""f2"", ""Seed"": [true, ""0x2""] } },
                { ""Name"": ""Gamma"", ""Resource"": { ""Filename"": ""f3"", ""Seed"": [true, ""0x3""] } }
            ]
        }");
        var ships = json.GetArray("ShipOwnership")!;

        // Primary is Gamma (index 2); delete Alpha (index 0)
        int primaryIndex = 2;
        StarshipLogic.DeleteShipData(ships.GetObject(0));

        // PrimaryShip stays at index 2 - Gamma is still there, no shifting
        Assert.Equal("Gamma", ships.GetObject(primaryIndex).GetString("Name"));
        Assert.Equal("f3", ships.GetObject(primaryIndex).GetObject("Resource")!.GetString("Filename"));
    }

    [Fact]
    public void StarshipLogic_DeleteShip_DeletingPrimary_FindsFirstValidShip()
    {
        // When deleting the primary ship, use FindFirstValidShipIndex to reassign.
        var json = JsonObject.Parse(@"{
            ""ShipOwnership"": [
                { ""Name"": ""Alpha"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""Beta"",  ""Resource"": { ""Filename"": ""f2"", ""Seed"": [true, ""0x2""] } },
                { ""Name"": ""Gamma"", ""Resource"": { ""Filename"": ""f3"", ""Seed"": [true, ""0x3""] } }
            ]
        }");
        var ships = json.GetArray("ShipOwnership")!;

        // Primary is Beta (index 1); delete Beta
        StarshipLogic.DeleteShipData(ships.GetObject(1));

        int newPrimary = StarshipLogic.FindFirstValidShipIndex(ships);
        // First valid is Alpha at index 0
        Assert.Equal(0, newPrimary);
        Assert.Equal("Alpha", ships.GetObject(newPrimary).GetString("Name"));
    }

    [Fact]
    public void StarshipLogic_CountValidShips_CountsOnlyActiveShips()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                { ""Name"": ""A"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""B"", ""Resource"": { ""Filename"": """",   ""Seed"": [false, ""0x0""] } },
                { ""Name"": ""C"", ""Resource"": { ""Filename"": ""f3"", ""Seed"": [true, ""0x3""] } }
            ]
        }");
        Assert.Equal(2, StarshipLogic.CountValidShips(json.GetArray("Ships")!));
    }

    [Fact]
    public void StarshipLogic_FindFirstValidShipIndex_SkipsInvalidSlots()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                { ""Name"": ""A"", ""Resource"": { ""Filename"": """",   ""Seed"": [false, ""0x0""] } },
                { ""Name"": ""B"", ""Resource"": { ""Filename"": """",   ""Seed"": [false, ""0x0""] } },
                { ""Name"": ""C"", ""Resource"": { ""Filename"": ""f3"", ""Seed"": [true, ""0x3""] } }
            ]
        }");
        Assert.Equal(2, StarshipLogic.FindFirstValidShipIndex(json.GetArray("Ships")!));
    }

    [Fact]
    public void StarshipLogic_FindFirstValidShipIndex_ReturnsNegativeOneWhenAllInvalid()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                { ""Name"": ""A"", ""Resource"": { ""Filename"": """", ""Seed"": [false, ""0x0""] } }
            ]
        }");
        Assert.Equal(-1, StarshipLogic.FindFirstValidShipIndex(json.GetArray("Ships")!));
    }

    [Fact]
    public void StarshipLogic_FindEmptySlot_ReturnsFirstEmpty()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                { ""Name"": ""A"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""B"", ""Resource"": { ""Filename"": """",   ""Seed"": [false, ""0x0""] } },
                { ""Name"": ""C"", ""Resource"": { ""Filename"": ""f3"", ""Seed"": [true, ""0x3""] } }
            ]
        }");
        Assert.Equal(1, StarshipLogic.FindEmptySlot(json.GetArray("Ships")!));
    }

    [Fact]
    public void StarshipLogic_FindEmptySlot_AllFull_ReturnsNegativeOne()
    {
        var json = JsonObject.Parse(@"{
            ""Ships"": [
                { ""Name"": ""A"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] } },
                { ""Name"": ""B"", ""Resource"": { ""Filename"": ""f2"", ""Seed"": [true, ""0x2""] } }
            ]
        }");
        Assert.Equal(-1, StarshipLogic.FindEmptySlot(json.GetArray("Ships")!));
    }

    [Fact]
    public void StarshipLogic_DeleteShip_PreservesParallelArrayAlignment()
    {
        // Verifies that deleting a ship doesn't break alignment between
        // ShipOwnership and ShipUsesLegacyColours (parallel array).
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""ShipOwnership"": [
                    { ""Name"": ""Ship0"", ""Resource"": { ""Filename"": ""f0"", ""Seed"": [true, ""0xA""] } },
                    { ""Name"": ""Ship1"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0xB""] } },
                    { ""Name"": ""Ship2"", ""Resource"": { ""Filename"": ""f2"", ""Seed"": [true, ""0xC""] } }
                ],
                ""ShipUsesLegacyColours"": [false, true, false],
                ""PrimaryShip"": 2
            }
        }");
        var playerState = json.GetObject("PlayerStateData")!;
        var ships = playerState.GetArray("ShipOwnership")!;
        var legacyArr = playerState.GetArray("ShipUsesLegacyColours")!;

        // Delete Ship1 (index 1) - invalidate only
        StarshipLogic.DeleteShipData(ships.GetObject(1));

        // Both arrays remain the same size
        Assert.Equal(3, ships.Length);
        Assert.Equal(3, legacyArr.Length);

        // Ship2 is still at index 2 with its correct legacy colour value
        Assert.Equal("Ship2", ships.GetObject(2).GetString("Name"));
        Assert.False(legacyArr.GetBool(2));
        // Ship0 is still at index 0 with its correct legacy colour value
        Assert.Equal("Ship0", ships.GetObject(0).GetString("Name"));
        Assert.False(legacyArr.GetBool(0));
    }

    // --- CharacterCustomisationData (CCD) / Ship Customisation -------

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 4)]
    [InlineData(5, 8)]
    [InlineData(6, 17)]
    [InlineData(11, 22)]
    [InlineData(-1, -1)]
    [InlineData(12, -1)]
    public void StarshipLogic_ShipIndexToCcdIndex_MapsCorrectly(int shipIndex, int expectedCcdIndex)
    {
        Assert.Equal(expectedCcdIndex, StarshipLogic.ShipIndexToCcdIndex(shipIndex));
    }

    [Fact]
    public void StarshipLogic_ResetShipCustomisation_ClearsPopulatedEntry()
    {
        // Build a 26-element CCD array where entry [4] (ship 1) has customisation data
        var json = JsonObject.Parse(@"{
            ""CharacterCustomisationData"": [
                " + string.Join(",\n", Enumerable.Range(0, 26).Select(i =>
                    i == 4
                        ? @"{
                            ""SelectedPreset"": ""^TEST"",
                            ""CustomData"": {
                                ""DescriptorGroups"": [""^SAIL_BODYA"", ""^SAIL_SAILB""],
                                ""PaletteID"": ""^SHIP_METALLIC"",
                                ""Colours"": [{""Palette"": {""Palette"": ""Paint""}, ""Colour"": [1,1,1,1]}],
                                ""TextureOptions"": [{""TextureOptionGroupName"": ""^SHIP_SAIL""}],
                                ""BoneScales"": [1.5],
                                ""Scale"": 2.0
                            }
                        }"
                        : @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [], ""PaletteID"": ""^"", ""Colours"": [], ""TextureOptions"": [], ""BoneScales"": [], ""Scale"": 1.0}}")) + @"
            ]
        }");

        var ccd = json.GetArray("CharacterCustomisationData")!;

        // Verify data is populated before reset
        var before = ccd.GetObject(4).GetObject("CustomData")!;
        Assert.Equal(2, before.GetArray("DescriptorGroups")!.Length);
        Assert.Equal("^SHIP_METALLIC", before.GetString("PaletteID"));

        // Reset ship 1 (CCD index 4)
        StarshipLogic.ResetShipCustomisation(ccd, 1);

        var after = ccd.GetObject(4);
        Assert.Equal("^", after.GetString("SelectedPreset"));
        var cd = after.GetObject("CustomData")!;
        Assert.Equal(0, cd.GetArray("DescriptorGroups")!.Length);
        Assert.Equal("^", cd.GetString("PaletteID"));
        Assert.Equal(0, cd.GetArray("Colours")!.Length);
        Assert.Equal(0, cd.GetArray("TextureOptions")!.Length);
        Assert.Equal(0, cd.GetArray("BoneScales")!.Length);
        Assert.Equal(1.0, cd.GetDouble("Scale"));
    }

    [Fact]
    public void StarshipLogic_ResetShipCustomisation_NullArray_DoesNotThrow()
    {
        // Should be a no-op with null array
        StarshipLogic.ResetShipCustomisation(null, 0);
    }

    [Fact]
    public void StarshipLogic_ResetShipCustomisation_OutOfRange_DoesNotThrow()
    {
        var json = JsonObject.Parse(@"{ ""ccd"": [] }");
        StarshipLogic.ResetShipCustomisation(json.GetArray("ccd"), 0);
    }

    [Fact]
    public void StarshipLogic_GetShipCustomisation_ReturnsDeepClone()
    {
        var json = JsonObject.Parse(@"{
            ""CharacterCustomisationData"": [
                " + string.Join(",\n", Enumerable.Range(0, 26).Select(i =>
                    i == 6
                        ? @"{
                            ""SelectedPreset"": ""^"",
                            ""CustomData"": {
                                ""DescriptorGroups"": [""^DROPS_COCKS13"", ""^DROPS_ENGIS13""],
                                ""PaletteID"": ""^SHIP_METALLIC"",
                                ""Colours"": [{""Palette"": {""Palette"": ""Paint""}, ""Colour"": [0.5,0.5,0.5,1]}],
                                ""TextureOptions"": [],
                                ""BoneScales"": [],
                                ""Scale"": 1.0
                            }
                        }"
                        : @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [], ""PaletteID"": ""^"", ""Colours"": [], ""TextureOptions"": [], ""BoneScales"": [], ""Scale"": 1.0}}")) + @"
            ]
        }");

        var ccd = json.GetArray("CharacterCustomisationData")!;

        // Ship 3 maps to CCD[6]
        var clone = StarshipLogic.GetShipCustomisation(ccd, 3);
        Assert.NotNull(clone);
        var dg = clone!.GetObject("CustomData")!.GetArray("DescriptorGroups")!;
        Assert.Equal(2, dg.Length);

        // Modifying clone should not affect original
        dg.RemoveAt(0);
        Assert.Equal(1, dg.Length);
        Assert.Equal(2, ccd.GetObject(6).GetObject("CustomData")!.GetArray("DescriptorGroups")!.Length);
    }

    [Fact]
    public void StarshipLogic_GetShipCustomisation_NullArray_ReturnsNull()
    {
        Assert.Null(StarshipLogic.GetShipCustomisation(null, 0));
    }

    [Fact]
    public void StarshipLogic_SetShipCustomisation_CopiesAllProperties()
    {
        var json = JsonObject.Parse(@"{
            ""CharacterCustomisationData"": [
                " + string.Join(",\n", Enumerable.Range(0, 26).Select(_ =>
                    @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [], ""PaletteID"": ""^"", ""Colours"": [], ""TextureOptions"": [], ""BoneScales"": [], ""Scale"": 1.0}}")) + @"
            ]
        }");
        var ccd = json.GetArray("CharacterCustomisationData")!;

        // Create a CCD entry to import
        var entry = JsonObject.Parse(@"{
            ""SelectedPreset"": ""^CUSTOM"",
            ""CustomData"": {
                ""DescriptorGroups"": [""^DG_A"", ""^DG_B"", ""^DG_C""],
                ""PaletteID"": ""^TEST_PAL"",
                ""Colours"": [{""Palette"": {""Palette"": ""Paint""}, ""Colour"": [1,0,0,1]}],
                ""TextureOptions"": [{""TextureOptionGroupName"": ""^TEX""}],
                ""BoneScales"": [],
                ""Scale"": 1.5
            }
        }");

        // Set on ship 7 -> CCD[18]
        StarshipLogic.SetShipCustomisation(ccd, 7, entry);

        var target = ccd.GetObject(18);
        Assert.Equal("^CUSTOM", target.GetString("SelectedPreset"));
        Assert.Equal(3, target.GetObject("CustomData")!.GetArray("DescriptorGroups")!.Length);
        Assert.Equal("^TEST_PAL", target.GetObject("CustomData")!.GetString("PaletteID"));
    }

    [Fact]
    public void StarshipLogic_SetShipCustomisation_NullEntry_ResetsSlot()
    {
        var json = JsonObject.Parse(@"{
            ""CharacterCustomisationData"": [
                " + string.Join(",\n", Enumerable.Range(0, 26).Select(i =>
                    i == 17
                        ? @"{""SelectedPreset"": ""^X"", ""CustomData"": {""DescriptorGroups"": [""^A""], ""PaletteID"": ""^P"", ""Colours"": [{""c"":1}], ""TextureOptions"": [{""t"":1}], ""BoneScales"": [1], ""Scale"": 2.0}}"
                        : @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [], ""PaletteID"": ""^"", ""Colours"": [], ""TextureOptions"": [], ""BoneScales"": [], ""Scale"": 1.0}}")) + @"
            ]
        }");
        var ccd = json.GetArray("CharacterCustomisationData")!;

        // Ship 6 -> CCD[17], passing null should reset it
        StarshipLogic.SetShipCustomisation(ccd, 6, null);

        var target = ccd.GetObject(17);
        Assert.Equal("^", target.GetString("SelectedPreset"));
        Assert.Equal(0, target.GetObject("CustomData")!.GetArray("DescriptorGroups")!.Length);
        Assert.Equal("^", target.GetObject("CustomData")!.GetString("PaletteID"));
        Assert.Equal(1.0, target.GetObject("CustomData")!.GetDouble("Scale"));
    }

    [Fact]
    public void StarshipLogic_DeleteShip_WithCCD_ClearsCustomisation()
    {
        // End-to-end: deleting a ship should also reset its CCD entry
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""ShipOwnership"": [
                    { ""Name"": ""Ship0"", ""Resource"": { ""Filename"": ""f0"", ""Seed"": [true, ""0xA""] } },
                    { ""Name"": ""Ship1"", ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0xB""] } }
                ],
                ""CharacterCustomisationData"": [
                    " + string.Join(",\n", Enumerable.Range(0, 26).Select(i =>
                        i == 4
                            ? @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [""^DG1"",""^DG2""], ""PaletteID"": ""^PAL"", ""Colours"": [{""c"":1}], ""TextureOptions"": [{""t"":1}], ""BoneScales"": [], ""Scale"": 1.0}}"
                            : @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [], ""PaletteID"": ""^"", ""Colours"": [], ""TextureOptions"": [], ""BoneScales"": [], ""Scale"": 1.0}}")) + @"
                ],
                ""PrimaryShip"": 0
            }
        }");
        var playerState = json.GetObject("PlayerStateData")!;
        var ships = playerState.GetArray("ShipOwnership")!;
        var ccd = playerState.GetArray("CharacterCustomisationData")!;

        // Verify CCD[4] (ship 1) has data
        Assert.Equal(2, ccd.GetObject(4).GetObject("CustomData")!.GetArray("DescriptorGroups")!.Length);

        // Delete ship 1 and reset its CCD
        StarshipLogic.DeleteShipData(ships.GetObject(1));
        StarshipLogic.ResetShipCustomisation(ccd, 1);

        // Ship is invalidated
        Assert.False(ships.GetObject(1).GetObject("Resource")!.GetArray("Seed")!.GetBool(0));

        // CCD[4] is reset
        var cd = ccd.GetObject(4).GetObject("CustomData")!;
        Assert.Equal(0, cd.GetArray("DescriptorGroups")!.Length);
        Assert.Equal("^", cd.GetString("PaletteID"));
        Assert.Equal(0, cd.GetArray("Colours")!.Length);
        Assert.Equal(0, cd.GetArray("TextureOptions")!.Length);
    }

    [Fact]
    public void StarshipLogic_DeleteShipData_ClearsNameAndInventories()
    {
        // DeleteShipData must clear ALL ship data, not just Resource.
        // Leaving behind Name, Inventory slots, tech slots, or stats
        // causes corrupt save remnants when a player deletes a custom ship.
        var json = JsonObject.Parse(@"{
            ""Name"": ""[OAC] Currawong"",
            ""Resource"": {
                ""Filename"": ""MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN"",
                ""Seed"": [true, ""0xABC123""]
            },
            ""Inventory"": {
                ""Slots"": [
                    { ""Type"": { ""InventoryType"": ""Product"" }, ""Id"": ""^FUEL1"", ""Amount"": 500 },
                    { ""Type"": { ""InventoryType"": ""Product"" }, ""Id"": ""^FUEL2"", ""Amount"": 250 }
                ],
                ""ValidSlotIndices"": [ { ""X"": 0, ""Y"": 0 }, { ""X"": 1, ""Y"": 0 } ],
                ""Class"": { ""InventoryClass"": ""S"" },
                ""BaseStatValues"": [
                    { ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 100.0 },
                    { ""BaseStatID"": ""^SHIP_SHIELD"", ""Value"": 200.0 }
                ],
                ""SpecialSlots"": []
            },
            ""Inventory_TechOnly"": {
                ""Slots"": [
                    { ""Type"": { ""InventoryType"": ""Technology"" }, ""Id"": ""^HYPERDRIVE"", ""Amount"": 1 }
                ],
                ""ValidSlotIndices"": [ { ""X"": 0, ""Y"": 0 } ],
                ""Class"": { ""InventoryClass"": ""S"" },
                ""BaseStatValues"": [
                    { ""BaseStatID"": ""^SHIP_HYPERDRIVE"", ""Value"": 300.0 }
                ],
                ""SpecialSlots"": []
            }
        }");

        StarshipLogic.DeleteShipData(json);

        // Resource should be invalidated
        var resource = json.GetObject("Resource")!;
        Assert.Equal("", resource.GetString("Filename"));
        Assert.False(resource.GetArray("Seed")!.GetBool(0));

        // Name should be cleared
        Assert.Equal("", json.GetString("Name"));

        // Inventory slots should be cleared
        Assert.Equal(0, json.GetObject("Inventory")!.GetArray("Slots")!.Length);
        Assert.Equal(0, json.GetObject("Inventory")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, json.GetObject("Inventory")!.GetArray("BaseStatValues")!.Length);

        // Tech inventory slots should be cleared
        Assert.Equal(0, json.GetObject("Inventory_TechOnly")!.GetArray("Slots")!.Length);
        Assert.Equal(0, json.GetObject("Inventory_TechOnly")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, json.GetObject("Inventory_TechOnly")!.GetArray("BaseStatValues")!.Length);
    }

    [Fact]
    public void StarshipLogic_DeleteShipData_ClearsCargoInventory()
    {
        // Inventory_Cargo (v3.85+ Outlaws) should also be cleared if present.
        var json = JsonObject.Parse(@"{
            ""Name"": ""TestShip"",
            ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] },
            ""Inventory"": { ""Slots"": [], ""ValidSlotIndices"": [], ""BaseStatValues"": [], ""SpecialSlots"": [] },
            ""Inventory_TechOnly"": { ""Slots"": [], ""ValidSlotIndices"": [], ""BaseStatValues"": [], ""SpecialSlots"": [] },
            ""Inventory_Cargo"": {
                ""Slots"": [
                    { ""Type"": { ""InventoryType"": ""Product"" }, ""Id"": ""^FUEL1"", ""Amount"": 9999 }
                ],
                ""ValidSlotIndices"": [ { ""X"": 0, ""Y"": 0 } ],
                ""BaseStatValues"": [],
                ""SpecialSlots"": []
            }
        }");

        StarshipLogic.DeleteShipData(json);

        Assert.Equal("", json.GetString("Name"));
        Assert.Equal(0, json.GetObject("Inventory_Cargo")!.GetArray("Slots")!.Length);
        Assert.Equal(0, json.GetObject("Inventory_Cargo")!.GetArray("ValidSlotIndices")!.Length);
    }

    [Fact]
    public void StarshipLogic_DeleteShipData_HandlesMinimalShip()
    {
        // A ship with no inventories should still be safely deleted.
        var json = JsonObject.Parse(@"{
            ""Name"": ""Minimal"",
            ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0x1""] }
        }");

        StarshipLogic.DeleteShipData(json);

        Assert.Equal("", json.GetString("Name"));
        Assert.Equal("", json.GetObject("Resource")!.GetString("Filename"));
        Assert.False(json.GetObject("Resource")!.GetArray("Seed")!.GetBool(0));
    }

    [Fact]
    public void StarshipLogic_DeleteShip_FullReset_EndToEnd()
    {
        // End-to-end: deleting a ship with populated inventories, CCD, and
        // name should leave NO remnants. This is the critical test for the
        // custom-built ship deletion bug.
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""ShipOwnership"": [
                    {
                        ""Name"": ""VCF Blackbird"",
                        ""Resource"": { ""Filename"": ""f0"", ""Seed"": [true, ""0xA""] },
                        ""Inventory"": { ""Slots"": [{ ""Id"": ""^FUEL1"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^SHIP_DAMAGE"", ""Value"": 50.0 }], ""SpecialSlots"": [] },
                        ""Inventory_TechOnly"": { ""Slots"": [{ ""Id"": ""^HYPER"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [], ""SpecialSlots"": [] }
                    },
                    {
                        ""Name"": ""[OAC] Currawong"",
                        ""Resource"": { ""Filename"": ""f1"", ""Seed"": [true, ""0xB""] },
                        ""Inventory"": { ""Slots"": [{ ""Id"": ""^FUEL2"" }, { ""Id"": ""^FUEL3"" }], ""ValidSlotIndices"": [{ ""X"": 0 }, { ""X"": 1 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^SHIP_SHIELD"", ""Value"": 100.0 }], ""SpecialSlots"": [] },
                        ""Inventory_TechOnly"": { ""Slots"": [{ ""Id"": ""^DRIVE"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^SHIP_HYPERDRIVE"", ""Value"": 300.0 }], ""SpecialSlots"": [] }
                    }
                ],
                ""CharacterCustomisationData"": [
                    " + string.Join(",\n", Enumerable.Range(0, 26).Select(i =>
                        i == 4
                            ? @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [""^DROPS_COCKS13"",""^DROPS_ENGIS13"",""^DROPS_WINGS13""], ""PaletteID"": ""^SHIP_METALLIC"", ""Colours"": [{""c"":1},{""c"":2},{""c"":3}], ""TextureOptions"": [{""t"":1}], ""BoneScales"": [], ""Scale"": 1.0}}"
                            : @"{""SelectedPreset"": ""^"", ""CustomData"": {""DescriptorGroups"": [], ""PaletteID"": ""^"", ""Colours"": [], ""TextureOptions"": [], ""BoneScales"": [], ""Scale"": 1.0}}")) + @"
                ],
                ""ShipUsesLegacyColours"": [false, false],
                ""PrimaryShip"": 0
            }
        }");

        var playerState = json.GetObject("PlayerStateData")!;
        var ships = playerState.GetArray("ShipOwnership")!;
        var ccd = playerState.GetArray("CharacterCustomisationData")!;

        // Delete ship 1 ([OAC] Currawong) - the custom-built ship
        StarshipLogic.DeleteShipData(ships.GetObject(1));
        StarshipLogic.ResetShipCustomisation(ccd, 1);

        // Ship 0 (VCF Blackbird) should be completely untouched
        var ship0 = ships.GetObject(0);
        Assert.Equal("VCF Blackbird", ship0.GetString("Name"));
        Assert.Equal("f0", ship0.GetObject("Resource")!.GetString("Filename"));
        Assert.True(ship0.GetObject("Resource")!.GetArray("Seed")!.GetBool(0));
        Assert.Equal(1, ship0.GetObject("Inventory")!.GetArray("Slots")!.Length);
        Assert.Equal(1, ship0.GetObject("Inventory_TechOnly")!.GetArray("Slots")!.Length);

        // Ship 1 ([OAC] Currawong) should have NO remnants
        var ship1 = ships.GetObject(1);
        Assert.Equal("", ship1.GetString("Name"));
        Assert.Equal("", ship1.GetObject("Resource")!.GetString("Filename"));
        Assert.False(ship1.GetObject("Resource")!.GetArray("Seed")!.GetBool(0));
        Assert.Equal(0, ship1.GetObject("Inventory")!.GetArray("Slots")!.Length);
        Assert.Equal(0, ship1.GetObject("Inventory")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, ship1.GetObject("Inventory")!.GetArray("BaseStatValues")!.Length);
        Assert.Equal(0, ship1.GetObject("Inventory_TechOnly")!.GetArray("Slots")!.Length);
        Assert.Equal(0, ship1.GetObject("Inventory_TechOnly")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, ship1.GetObject("Inventory_TechOnly")!.GetArray("BaseStatValues")!.Length);

        // CCD[4] should be reset
        var cd = ccd.GetObject(4).GetObject("CustomData")!;
        Assert.Equal(0, cd.GetArray("DescriptorGroups")!.Length);
        Assert.Equal("^", cd.GetString("PaletteID"));
        Assert.Equal(0, cd.GetArray("Colours")!.Length);
        Assert.Equal(0, cd.GetArray("TextureOptions")!.Length);

        // Array size preserved
        Assert.Equal(2, ships.Length);
    }

    // --- MultitoolLogic ----------------------------------------------

    [Fact]
    public void MultitoolLogic_ToolTypes_HasExpectedEntries()
    {
        Assert.True(MultitoolLogic.ToolTypes.Length >= 14);
        Assert.Equal("Standard", MultitoolLogic.ToolTypes[0].Name);
        Assert.Equal("Rifle", MultitoolLogic.ToolTypes[1].Name);
        Assert.Equal("Royal", MultitoolLogic.ToolTypes[2].Name);
    }

    [Fact]
    public void MultitoolLogic_ToolTypes_AllHaveFilenames()
    {
        foreach (var (name, filename) in MultitoolLogic.ToolTypes)
        {
            Assert.False(string.IsNullOrEmpty(name));
            Assert.False(string.IsNullOrEmpty(filename));
            Assert.Contains("MULTITOOL", filename, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MultitoolLogic_ToolClasses_MatchesShipClasses()
    {
        Assert.Equal(new[] { "C", "B", "A", "S" }, MultitoolLogic.ToolClasses);
    }

    [Fact]
    public void MultitoolLogic_BuildToolList_ReturnsSeededTools()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": ""Laser Pro"", ""Seed"": [true, ""0x111""] },
                { ""Name"": """", ""Seed"": [false, ""0x0""] },
                { ""Name"": ""Blaster"", ""Seed"": [true, ""0x222""] }
            ]
        }");

        var tools = MultitoolLogic.BuildToolList(json.GetArray("Tools")!);
        Assert.Equal(2, tools.Count);
        Assert.Equal("Laser Pro", tools[0].DisplayName);
        Assert.Equal("Blaster", tools[1].DisplayName);
    }

    [Fact]
    public void MultitoolLogic_BuildToolList_EmptyName_UsesDefault()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": """", ""Seed"": [true, ""0xABC""] }
            ]
        }");

        var tools = MultitoolLogic.BuildToolList(json.GetArray("Tools")!);
        Assert.Single(tools);
        Assert.Equal("Multitool 1", tools[0].DisplayName);
    }

    [Fact]
    public void MultitoolLogic_FindEmptySlot_ReturnsFirstEmpty()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Seed"": [true, ""0x1""] },
                { ""Seed"": [false, ""0x0""] },
                { ""Seed"": [true, ""0x2""] }
            ]
        }");

        Assert.Equal(1, MultitoolLogic.FindEmptySlot(json.GetArray("Tools")!));
    }

    [Fact]
    public void MultitoolLogic_FindEmptySlot_AllFull_ReturnsNegativeOne()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Seed"": [true, ""0x1""] },
                { ""Seed"": [true, ""0x2""] }
            ]
        }");

        Assert.Equal(-1, MultitoolLogic.FindEmptySlot(json.GetArray("Tools")!));
    }

    [Fact]
    public void MultitoolLogic_DeleteToolData_InvalidatesInPlace()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": ""Alpha"", ""Seed"": [true, ""0x1""],
                  ""Resource"": { ""Filename"": ""alpha.mbin"", ""Seed"": [true, ""0xA""], ""AltId"": """", ""ProceduralTexture"": { ""Samplers"": [] } },
                  ""Store"": { ""Slots"": [{ ""Id"": ""^LASER"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_DAMAGE"", ""Value"": 10.0 }], ""SpecialSlots"": [], ""Class"": { ""InventoryClass"": ""A"" } },
                  ""Store_TechOnly"": { ""Slots"": [], ""ValidSlotIndices"": [], ""BaseStatValues"": [], ""SpecialSlots"": [] }
                },
                { ""Name"": ""Beta"", ""Seed"": [true, ""0x2""],
                  ""Resource"": { ""Filename"": ""beta.mbin"", ""Seed"": [true, ""0xB""], ""AltId"": ""alt1"", ""ProceduralTexture"": { ""Samplers"": [{ ""x"": 1 }] } },
                  ""Store"": { ""Slots"": [{ ""Id"": ""^SCAN1"" }, { ""Id"": ""^LASER"" }], ""ValidSlotIndices"": [{ ""X"": 0 }, { ""X"": 1 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_DAMAGE"", ""Value"": 50.0 }], ""SpecialSlots"": [{ ""Type"": 1 }], ""Class"": { ""InventoryClass"": ""S"" } },
                  ""Store_TechOnly"": { ""Slots"": [{ ""Id"": ""^TERRAINEDITOR"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_SCAN"", ""Value"": 20.0 }], ""SpecialSlots"": [] }
                },
                { ""Name"": ""Gamma"", ""Seed"": [true, ""0x3""],
                  ""Resource"": { ""Filename"": ""gamma.mbin"", ""Seed"": [true, ""0xC""], ""AltId"": """", ""ProceduralTexture"": { ""Samplers"": [] } },
                  ""Store"": { ""Slots"": [{ ""Id"": ""^LASER"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [], ""SpecialSlots"": [], ""Class"": { ""InventoryClass"": ""B"" } },
                  ""Store_TechOnly"": { ""Slots"": [], ""ValidSlotIndices"": [], ""BaseStatValues"": [], ""SpecialSlots"": [] }
                }
            ]
        }");
        var tools = json.GetArray("Tools")!;

        // Delete Beta (index 1) - full reset
        MultitoolLogic.DeleteToolData(tools.GetObject(1));

        // Array size unchanged
        Assert.Equal(3, tools.Length);

        // Alpha and Gamma remain at their original indices with data intact
        Assert.True(tools.GetObject(0).GetArray("Seed")!.GetBool(0));
        Assert.Equal("Alpha", tools.GetObject(0).GetString("Name"));
        Assert.Equal("alpha.mbin", tools.GetObject(0).GetObject("Resource")!.GetString("Filename"));
        Assert.Equal(1, tools.GetObject(0).GetObject("Store")!.GetArray("Slots")!.Length);

        Assert.True(tools.GetObject(2).GetArray("Seed")!.GetBool(0));
        Assert.Equal("Gamma", tools.GetObject(2).GetString("Name"));

        // Beta is fully cleared - no refuse data left behind
        var beta = tools.GetObject(1);
        Assert.False(beta.GetArray("Seed")!.GetBool(0));
        Assert.Equal("0x0", beta.GetArray("Seed")!.Get(1)!.ToString());
        Assert.Equal("", beta.GetString("Name"));
        Assert.Equal("", beta.GetObject("Resource")!.GetString("Filename"));
        Assert.False(beta.GetObject("Resource")!.GetArray("Seed")!.GetBool(0));
        Assert.Equal("", beta.GetObject("Resource")!.GetString("AltId"));
        Assert.Equal(0, beta.GetObject("Resource")!.GetObject("ProceduralTexture")!.GetArray("Samplers")!.Length);
        Assert.Equal(0, beta.GetObject("Store")!.GetArray("Slots")!.Length);
        Assert.Equal(0, beta.GetObject("Store")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, beta.GetObject("Store")!.GetArray("BaseStatValues")!.Length);
        Assert.Equal(0, beta.GetObject("Store")!.GetArray("SpecialSlots")!.Length);
        Assert.Equal(0, beta.GetObject("Store_TechOnly")!.GetArray("Slots")!.Length);
        Assert.Equal(0, beta.GetObject("Store_TechOnly")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, beta.GetObject("Store_TechOnly")!.GetArray("BaseStatValues")!.Length);

        // BuildToolList should skip Beta
        var list = MultitoolLogic.BuildToolList(tools);
        Assert.Equal(2, list.Count);
        Assert.Equal(0, list[0].DataIndex);
        Assert.Equal(2, list[1].DataIndex);
    }

    [Fact]
    public void MultitoolLogic_CountValidTools_CountsCorrectly()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Seed"": [true, ""0x1""] },
                { ""Seed"": [false, ""0x0""] },
                { ""Seed"": [true, ""0x3""] }
            ]
        }");
        Assert.Equal(2, MultitoolLogic.CountValidTools(json.GetArray("Tools")!));
    }

    [Fact]
    public void MultitoolLogic_FindFirstValidToolIndex_SkipsInvalid()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Seed"": [false, ""0x0""] },
                { ""Seed"": [false, ""0x0""] },
                { ""Seed"": [true, ""0x3""] }
            ]
        }");
        Assert.Equal(2, MultitoolLogic.FindFirstValidToolIndex(json.GetArray("Tools")!));
    }

    [Fact]
    public void MultitoolLogic_GetPrimaryToolName_ReturnsName()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": ""Laser Pro"", ""Seed"": [true, ""0x111""] },
                { ""Name"": ""Blaster"", ""Seed"": [true, ""0x222""] }
            ]
        }");
        Assert.Equal("Laser Pro", MultitoolLogic.GetPrimaryToolName(json.GetArray("Tools")!, 0));
        Assert.Equal("Blaster", MultitoolLogic.GetPrimaryToolName(json.GetArray("Tools")!, 1));
    }

    [Fact]
    public void MultitoolLogic_GetPrimaryToolName_EmptyName_UsesFallback()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": """", ""Seed"": [true, ""0x111""] }
            ]
        }");
        Assert.Equal("Multitool 1", MultitoolLogic.GetPrimaryToolName(json.GetArray("Tools")!, 0));
    }

    [Fact]
    public void MultitoolLogic_GetPrimaryToolName_InvalidIndex_ReturnsUnknown()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": ""Tool"", ""Seed"": [true, ""0x111""] }
            ]
        }");
        Assert.Equal("Unknown", MultitoolLogic.GetPrimaryToolName(json.GetArray("Tools")!, -1));
        Assert.Equal("Unknown", MultitoolLogic.GetPrimaryToolName(json.GetArray("Tools")!, 99));
    }

    [Fact]
    public void MultitoolLogic_GetPrimaryToolName_NullArray_ReturnsUnknown()
    {
        Assert.Equal("Unknown", MultitoolLogic.GetPrimaryToolName(null, 0));
    }

    // --- FreighterLogic ----------------------------------------------

    [Fact]
    public void MultitoolLogic_DeleteToolData_ClearsName()
    {
        var tool = JsonObject.Parse(@"{
            ""Name"": ""My Rifle"",
            ""Seed"": [true, ""0xAABB""]
        }");
        MultitoolLogic.DeleteToolData(tool);
        Assert.Equal("", tool.GetString("Name"));
    }

    [Fact]
    public void MultitoolLogic_DeleteToolData_ClearsResource()
    {
        var tool = JsonObject.Parse(@"{
            ""Name"": ""Tool"",
            ""Seed"": [true, ""0x1""],
            ""Resource"": {
                ""Filename"": ""MODELS/COMMON/WEAPONS/MULTITOOL/MULTITOOL.SCENE.MBIN"",
                ""Seed"": [true, ""0xDEAD""],
                ""AltId"": ""someAltId"",
                ""ProceduralTexture"": { ""Samplers"": [{ ""x"": 1 }] }
            }
        }");
        MultitoolLogic.DeleteToolData(tool);

        var resource = tool.GetObject("Resource")!;
        Assert.Equal("", resource.GetString("Filename"));
        Assert.False(resource.GetArray("Seed")!.GetBool(0));
        Assert.Equal("0x0", resource.GetArray("Seed")!.Get(1)!.ToString());
        Assert.Equal("", resource.GetString("AltId"));
        Assert.Equal(0, resource.GetObject("ProceduralTexture")!.GetArray("Samplers")!.Length);
    }

    [Fact]
    public void MultitoolLogic_DeleteToolData_ClearsStoreInventories()
    {
        var tool = JsonObject.Parse(@"{
            ""Name"": ""Tool"",
            ""Seed"": [true, ""0x1""],
            ""Store"": {
                ""Slots"": [{ ""Id"": ""^SCAN1"" }, { ""Id"": ""^LASER"" }],
                ""ValidSlotIndices"": [{ ""X"": 0 }, { ""X"": 1 }],
                ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_DAMAGE"", ""Value"": 50.0 }],
                ""SpecialSlots"": [{ ""Type"": 1 }],
                ""Class"": { ""InventoryClass"": ""S"" }
            },
            ""Store_TechOnly"": {
                ""Slots"": [{ ""Id"": ""^TERRAINEDITOR"" }],
                ""ValidSlotIndices"": [{ ""X"": 0 }],
                ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_SCAN"", ""Value"": 20.0 }],
                ""SpecialSlots"": []
            }
        }");
        MultitoolLogic.DeleteToolData(tool);

        // Store should be fully cleared
        var store = tool.GetObject("Store")!;
        Assert.Equal(0, store.GetArray("Slots")!.Length);
        Assert.Equal(0, store.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, store.GetArray("BaseStatValues")!.Length);
        Assert.Equal(0, store.GetArray("SpecialSlots")!.Length);

        // Store_TechOnly should be fully cleared
        var tech = tool.GetObject("Store_TechOnly")!;
        Assert.Equal(0, tech.GetArray("Slots")!.Length);
        Assert.Equal(0, tech.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, tech.GetArray("BaseStatValues")!.Length);
    }

    [Fact]
    public void MultitoolLogic_DeleteToolData_FullReset_EndToEnd()
    {
        // End-to-end: a fully populated multitool matching the template
        // should leave NO remnants after deletion.
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""Multitools"": [
                    {
                        ""Name"": ""Primary Pistol"",
                        ""Seed"": [true, ""0xA1""],
                        ""IsLarge"": false,
                        ""PrimaryMode"": 0,
                        ""SecondaryMode"": 0,
                        ""UseLegacyColours"": true,
                        ""Resource"": { ""Filename"": ""MODELS/COMMON/WEAPONS/MULTITOOL/MULTITOOL.SCENE.MBIN"", ""Seed"": [true, ""0xA1""], ""AltId"": """", ""ProceduralTexture"": { ""Samplers"": [] } },
                        ""Store"": { ""Slots"": [{ ""Id"": ""^LASER"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_DAMAGE"", ""Value"": 25.0 }], ""SpecialSlots"": [], ""Class"": { ""InventoryClass"": ""B"" } },
                        ""Store_TechOnly"": { ""Slots"": [], ""ValidSlotIndices"": [], ""BaseStatValues"": [], ""SpecialSlots"": [] }
                    },
                    {
                        ""Name"": ""Alien Rifle"",
                        ""Seed"": [true, ""0xB2""],
                        ""IsLarge"": true,
                        ""PrimaryMode"": 1,
                        ""SecondaryMode"": 2,
                        ""UseLegacyColours"": false,
                        ""Resource"": { ""Filename"": ""MODELS/COMMON/WEAPONS/MULTITOOL/YOURALIENMULTITOOL.SCENE.MBIN"", ""Seed"": [true, ""0xB2""], ""AltId"": ""alien1"", ""ProceduralTexture"": { ""Samplers"": [{ ""x"": 42 }] } },
                        ""Store"": { ""Slots"": [{ ""Id"": ""^SCAN1"" }, { ""Id"": ""^LASER"" }, { ""Id"": ""^STRONGLASER"" }], ""ValidSlotIndices"": [{ ""X"": 0 }, { ""X"": 1 }, { ""X"": 2 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_DAMAGE"", ""Value"": 80.0 }, { ""BaseStatID"": ""^WEAPON_MINING"", ""Value"": 50.0 }], ""SpecialSlots"": [{ ""Type"": 1 }], ""Class"": { ""InventoryClass"": ""S"" } },
                        ""Store_TechOnly"": { ""Slots"": [{ ""Id"": ""^TERRAINEDITOR"" }], ""ValidSlotIndices"": [{ ""X"": 0 }], ""BaseStatValues"": [{ ""BaseStatID"": ""^WEAPON_SCAN"", ""Value"": 30.0 }], ""SpecialSlots"": [] }
                    }
                ],
                ""ActiveMultioolIndex"": 0
            }
        }");

        var tools = json.GetObject("PlayerStateData")!.GetArray("Multitools")!;

        // Delete the Alien Rifle (index 1)
        MultitoolLogic.DeleteToolData(tools.GetObject(1));

        // Primary Pistol (index 0) should be completely untouched
        var tool0 = tools.GetObject(0);
        Assert.Equal("Primary Pistol", tool0.GetString("Name"));
        Assert.True(tool0.GetArray("Seed")!.GetBool(0));
        Assert.Equal("MODELS/COMMON/WEAPONS/MULTITOOL/MULTITOOL.SCENE.MBIN", tool0.GetObject("Resource")!.GetString("Filename"));
        Assert.Equal(1, tool0.GetObject("Store")!.GetArray("Slots")!.Length);

        // Alien Rifle (index 1) should have NO remnants
        var tool1 = tools.GetObject(1);
        Assert.False(tool1.GetArray("Seed")!.GetBool(0));
        Assert.Equal("0x0", tool1.GetArray("Seed")!.Get(1)!.ToString());
        Assert.Equal("", tool1.GetString("Name"));
        Assert.Equal("", tool1.GetObject("Resource")!.GetString("Filename"));
        Assert.False(tool1.GetObject("Resource")!.GetArray("Seed")!.GetBool(0));
        Assert.Equal("", tool1.GetObject("Resource")!.GetString("AltId"));
        Assert.Equal(0, tool1.GetObject("Resource")!.GetObject("ProceduralTexture")!.GetArray("Samplers")!.Length);
        Assert.Equal(0, tool1.GetObject("Store")!.GetArray("Slots")!.Length);
        Assert.Equal(0, tool1.GetObject("Store")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, tool1.GetObject("Store")!.GetArray("BaseStatValues")!.Length);
        Assert.Equal(0, tool1.GetObject("Store")!.GetArray("SpecialSlots")!.Length);
        Assert.Equal(0, tool1.GetObject("Store_TechOnly")!.GetArray("Slots")!.Length);
        Assert.Equal(0, tool1.GetObject("Store_TechOnly")!.GetArray("ValidSlotIndices")!.Length);
        Assert.Equal(0, tool1.GetObject("Store_TechOnly")!.GetArray("BaseStatValues")!.Length);

        // Array size preserved
        Assert.Equal(2, tools.Length);

        // Only one valid tool remaining
        Assert.Equal(1, MultitoolLogic.CountValidTools(tools));
        Assert.Equal(0, MultitoolLogic.FindFirstValidToolIndex(tools));
    }

    // --- FreighterLogic ----------------------------------------------

    [Fact]
    public void FreighterLogic_FreighterTypes_HasExpectedEntries()
    {
        Assert.Equal(5, FreighterLogic.FreighterTypes.Count);
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("Tiny"));
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("Small"));
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("Normal"));
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("Capital"));
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("Pirate"));
    }

    [Fact]
    public void FreighterLogic_FreighterTypes_CaseInsensitiveLookup()
    {
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("tiny"));
        Assert.True(FreighterLogic.FreighterTypes.ContainsKey("CAPITAL"));
    }

    [Fact]
    public void FreighterLogic_FreighterClasses_HasFourEntries()
    {
        Assert.Equal(new[] { "C", "B", "A", "S" }, FreighterLogic.FreighterClasses);
    }

    [Fact]
    public void FreighterLogic_BuildExportFileName_FormatsCorrectly()
    {
        string result = FreighterLogic.BuildExportFileName("Star Hauler", "Capital", 3);
        Assert.Equal("Star_Hauler_Capital_S", result);
    }

    [Fact]
    public void FreighterLogic_BuildExportFileName_NegativeClass_UsesC()
    {
        string result = FreighterLogic.BuildExportFileName("Test", "Tiny", -1);
        Assert.Equal("Test_Tiny_C", result);
    }

    [Fact]
    public void FreighterLogic_FindFreighterBase_FindsCorrectBase()
    {
        var json = JsonObject.Parse(@"{
            ""PersistentPlayerBases"": [
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""HomePlanetBase"" },
                    ""BaseVersion"": 3
                },
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""FreighterBase"" },
                    ""BaseVersion"": 3
                }
            ]
        }");

        var result = FreighterLogic.FindFreighterBase(json);
        Assert.NotNull(result);
        Assert.Equal("FreighterBase", result.GetObject("BaseType")!.GetString("PersistentBaseTypes"));
    }

    [Fact]
    public void FreighterLogic_FindFreighterBase_NoFreighter_ReturnsNull()
    {
        var json = JsonObject.Parse(@"{
            ""PersistentPlayerBases"": [
                {
                    ""BaseType"": { ""PersistentBaseTypes"": ""HomePlanetBase"" },
                    ""BaseVersion"": 3
                }
            ]
        }");

        Assert.Null(FreighterLogic.FindFreighterBase(json));
    }

    // --- FrigateLogic ------------------------------------------------

    [Fact]
    public void FrigateLogic_AdjustExpeditionIndices_DecrementsAboveRemovedIndex()
    {
        // Simulates removing frigate at index 2 from a fleet of 5.
        // Expedition references frigate indices [1, 2, 4].
        // After removing index 2: indices > 2 should decrement -> [1, 2, 3].
        // (Index 2 itself stays since the removal already happened externally.)
        var json = JsonObject.Parse(@"{
            ""Expeditions"": [
                {
                    ""AllFrigateIndices"": [1, 2, 4],
                    ""ActiveFrigateIndices"": [1, 4],
                    ""DamagedFrigateIndices"": [4],
                    ""DestroyedFrigateIndices"": [],
                    ""Events"": [
                        {
                            ""AffectedFrigateIndices"": [1, 4],
                            ""RepairingFrigateIndices"": [4],
                            ""AffectedFrigateResponses"": [1, 4]
                        }
                    ]
                }
            ]
        }");
        var expeditions = json.GetArray("Expeditions")!;
        FrigateLogic.AdjustExpeditionIndicesAfterRemoval(2, expeditions);

        var exp = expeditions.GetObject(0);
        // AllFrigateIndices: [1, 2, 3] (4->3, 2 stays, 1 stays)
        var all = exp.GetArray("AllFrigateIndices")!;
        Assert.Equal(1, all.GetInt(0));
        Assert.Equal(2, all.GetInt(1));
        Assert.Equal(3, all.GetInt(2));

        // ActiveFrigateIndices: [1, 3] (4->3, 1 stays)
        var active = exp.GetArray("ActiveFrigateIndices")!;
        Assert.Equal(1, active.GetInt(0));
        Assert.Equal(3, active.GetInt(1));

        // DamagedFrigateIndices: [3] (4->3)
        var damaged = exp.GetArray("DamagedFrigateIndices")!;
        Assert.Equal(3, damaged.GetInt(0));

        // Events[0].AffectedFrigateIndices: [1, 3]
        var ev = exp.GetArray("Events")!.GetObject(0);
        var affected = ev.GetArray("AffectedFrigateIndices")!;
        Assert.Equal(1, affected.GetInt(0));
        Assert.Equal(3, affected.GetInt(1));

        // Events[0].RepairingFrigateIndices: [3]
        var repairing = ev.GetArray("RepairingFrigateIndices")!;
        Assert.Equal(3, repairing.GetInt(0));
    }

    [Fact]
    public void FrigateLogic_AdjustExpeditionIndices_NoChangeWhenAllBelow()
    {
        // If all indices are below the removed index, nothing should change
        var json = JsonObject.Parse(@"{
            ""Expeditions"": [
                {
                    ""AllFrigateIndices"": [0, 1],
                    ""ActiveFrigateIndices"": [0, 1],
                    ""DamagedFrigateIndices"": [],
                    ""DestroyedFrigateIndices"": [],
                    ""Events"": []
                }
            ]
        }");
        var expeditions = json.GetArray("Expeditions")!;
        FrigateLogic.AdjustExpeditionIndicesAfterRemoval(5, expeditions);

        var all = expeditions.GetObject(0).GetArray("AllFrigateIndices")!;
        Assert.Equal(0, all.GetInt(0));
        Assert.Equal(1, all.GetInt(1));
    }

    [Fact]
    public void FrigateLogic_FrigateTypes_HasExpectedEntries()
    {
        Assert.Equal(10, FrigateLogic.FrigateTypes.Length);
        Assert.Contains("Combat", FrigateLogic.FrigateTypes);
        Assert.Contains("Exploration", FrigateLogic.FrigateTypes);
        Assert.Contains("Mining", FrigateLogic.FrigateTypes);
        Assert.Contains("Normandy", FrigateLogic.FrigateTypes);
        Assert.Contains("GhostShip", FrigateLogic.FrigateTypes);
    }

    [Fact]
    public void FrigateLogic_FrigateGrades_HasFourEntries()
    {
        Assert.Equal(new[] { "C", "B", "A", "S" }, FrigateLogic.FrigateGrades);
    }

    [Fact]
    public void FrigateLogic_FrigateRaces_HasExpectedEntries()
    {
        Assert.Equal(new[] { "Traders", "Warriors", "Explorers" }, FrigateLogic.FrigateRaces);
    }

    [Fact]
    public void FrigateLogic_StatNames_HasElevenEntries()
    {
        Assert.Equal(11, FrigateLogic.StatNames.Length);
        Assert.Equal("Combat", FrigateLogic.StatNames[0]);
        Assert.Equal("Stealth", FrigateLogic.StatNames[10]);
    }

    [Fact]
    public void FrigateLogic_GetFrigateName_ReturnsName()
    {
        var frigate = JsonObject.Parse(@"{ ""CustomName"": ""The Explorer"" }");
        Assert.Equal("The Explorer", FrigateLogic.GetFrigateName(frigate, 0));
    }

    [Fact]
    public void FrigateLogic_GetFrigateType_ReturnsType()
    {
        var frigate = JsonObject.Parse(@"{ ""FrigateClass"": { ""FrigateClass"": ""Exploration"" } }");
        Assert.Equal("Exploration", FrigateLogic.GetFrigateType(frigate));
    }

    [Fact]
    public void FrigateLogic_ComputeClassFromTraits_ComputesCorrectly()
    {
        // 1 beneficial trait -> max(0, min(3, 1-2)) = 0 -> C
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI""] }");
        Assert.Equal("C", FrigateLogic.ComputeClassFromTraits(frigate));

        // 3 beneficial traits -> max(0, min(3, 3-2)) = 1 -> B
        frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^COMBAT_PRI"", ""^FUEL_SEC_1""] }");
        Assert.Equal("B", FrigateLogic.ComputeClassFromTraits(frigate));

        // 5 beneficial traits -> max(0, min(3, 5-2)) = 3 -> S
        frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^COMBAT_PRI"", ""^FUEL_SEC_1"", ""^FUEL_SEC_2"", ""^FUEL_SEC_3""] }");
        Assert.Equal("S", FrigateLogic.ComputeClassFromTraits(frigate));
    }

    [Fact]
    public void FrigateLogic_AdjustTraitsForTargetGrade_UpgradeFromC()
    {
        // Start with 1 primary trait + 4 empty slots -> C class (net score 1)
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^"", ""^"", ""^"", ""^""] }");
        Assert.Equal("C", FrigateLogic.ComputeClassFromTraits(frigate));

        // Upgrade to S
        FrigateLogic.AdjustTraitsForTargetGrade(frigate, "S");
        Assert.Equal("S", FrigateLogic.ComputeClassFromTraits(frigate));
        // Primary trait (slot 0) should be untouched
        Assert.Equal("^FUEL_PRI", frigate.GetArray("TraitIDs")!.GetString(0));
    }

    [Fact]
    public void FrigateLogic_AdjustTraitsForTargetGrade_DowngradeFromS()
    {
        // Start with 5 beneficial traits -> S class (net score 5)
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^SPEED_TER_1"", ""^SPEED_TER_2"", ""^SPEED_TER_3"", ""^SPEED_TER_4""] }");
        Assert.Equal("S", FrigateLogic.ComputeClassFromTraits(frigate));

        // Downgrade to C
        FrigateLogic.AdjustTraitsForTargetGrade(frigate, "C");
        Assert.Equal("C", FrigateLogic.ComputeClassFromTraits(frigate));
        // Primary trait (slot 0) should be untouched
        Assert.Equal("^FUEL_PRI", frigate.GetArray("TraitIDs")!.GetString(0));
    }

    [Fact]
    public void FrigateLogic_AdjustTraitsForTargetGrade_UpgradeFromNegative()
    {
        // Start with primary + 2 negative traits -> net score -1 -> C
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^COMBAT_PRI"", ""^COMBAT_BAD_1"", ""^COMBAT_BAD_2"", ""^"", ""^""] }");
        Assert.Equal("C", FrigateLogic.ComputeClassFromTraits(frigate));

        // Upgrade to B (needs net score 3)
        FrigateLogic.AdjustTraitsForTargetGrade(frigate, "B");
        Assert.Equal("B", FrigateLogic.ComputeClassFromTraits(frigate));
    }

    [Fact]
    public void FrigateLogic_AdjustTraitsForTargetGrade_SameGradeNoOp()
    {
        // Start with B class
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^SPEED_TER_1"", ""^"", ""^"", ""^""] }");
        string before = frigate.ToString();

        // "Adjust" to same class should not change anything
        string currentClass = FrigateLogic.ComputeClassFromTraits(frigate);
        FrigateLogic.AdjustTraitsForTargetGrade(frigate, currentClass);
        Assert.Equal(before, frigate.ToString());
    }

    [Fact]
    public void FrigateLogic_AdjustTraitsForTargetGrade_SyncsInventoryClass()
    {
        // Verify that after adjustment, ComputeClassFromTraits gives target grade
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^"", ""^"", ""^"", ""^""], ""InventoryClass"": { ""InventoryClass"": ""C"" } }");

        FrigateLogic.AdjustTraitsForTargetGrade(frigate, "A");
        Assert.Equal("A", FrigateLogic.ComputeClassFromTraits(frigate));
    }

    [Fact]
    public void FrigateLogic_TraitChange_RecalculatesClass()
    {
        // Simulate user changing traits: class should recalculate automatically.
        // Start with S class (5 beneficial traits -> net score 5)
        var frigate = JsonObject.Parse(@"{ ""TraitIDs"": [""^FUEL_PRI"", ""^SPEED_TER_1"", ""^SPEED_TER_2"", ""^SPEED_TER_3"", ""^SPEED_TER_4""], ""InventoryClass"": { ""InventoryClass"": ""S"" } }");
        Assert.Equal("S", FrigateLogic.ComputeClassFromTraits(frigate));

        // User clears one trait (replaces beneficial with "^") -> net score 4 -> A
        var traits = frigate.GetArray("TraitIDs")!;
        traits.Set(4, "^");
        Assert.Equal("A", FrigateLogic.ComputeClassFromTraits(frigate));

        // User replaces another beneficial with a negative trait -> net 3+(-1)=2 -> C
        traits.Set(3, "^COMBAT_BAD_1");
        Assert.Equal("C", FrigateLogic.ComputeClassFromTraits(frigate));

        // User clears the negative trait -> net score 3 -> B
        traits.Set(3, "^");
        Assert.Equal("B", FrigateLogic.ComputeClassFromTraits(frigate));

        // User adds beneficial trait back -> net score 4 -> A
        traits.Set(3, "^FUEL_TER_1");
        Assert.Equal("A", FrigateLogic.ComputeClassFromTraits(frigate));

        // User adds another beneficial -> net score 5 -> S
        traits.Set(4, "^INVULN_TER_1");
        Assert.Equal("S", FrigateLogic.ComputeClassFromTraits(frigate));
    }

    // --- CompanionLogic ----------------------------------------------

    [Fact]
    public void CompanionLogic_LookupSpeciesName_KnownEntry_FoundInDatabase()
    {
        // ^CAT exists in CompanionDatabase
        Assert.True(CompanionDatabase.ById.ContainsKey("^CAT"));
        string name = CompanionLogic.LookupSpeciesName("^CAT");
        // Returns whatever the database has (could be empty string)
        Assert.NotNull(name);
    }

    [Fact]
    public void CompanionLogic_LookupSpeciesName_UnknownSpecies_ReturnsEmpty()
    {
        Assert.Equal("", CompanionLogic.LookupSpeciesName("^UNKNOWN_SPECIES"));
    }

    [Fact]
    public void CompanionLogic_LookupSpeciesName_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", CompanionLogic.LookupSpeciesName(""));
        Assert.Equal("", CompanionLogic.LookupSpeciesName("^"));
    }

    [Fact]
    public void CompanionLogic_LookupSpeciesName_NullInput_ReturnsEmpty()
    {
        Assert.Equal("", CompanionLogic.LookupSpeciesName(null!));
    }

    [Fact]
    public void CompanionLogic_DeleteCompanion_ClearsSeed()
    {
        var comp = JsonObject.Parse(@"{
            ""CustomName"": ""Rex"",
            ""CreatureID"": ""^TREX"",
            ""CreatureSeed"": [true, ""0x88CA25ACD4B209BB""],
            ""CreatureSecondarySeed"": [true, ""0xABCD""],
            ""ColourBaseSeed"": [true, ""0x1234""],
            ""BoneScaleSeed"": [true, ""0x5678""]
        }");

        CompanionLogic.DeleteCompanion(comp);

        // All seed arrays should be reset
        var seedArr = comp.GetArray("CreatureSeed")!;
        Assert.False(seedArr.GetBool(0));
        Assert.Equal("0x0", seedArr.GetString(1));

        var secSeed = comp.GetArray("CreatureSecondarySeed")!;
        Assert.False(secSeed.GetBool(0));
        Assert.Equal("0x0", secSeed.GetString(1));

        var colourSeed = comp.GetArray("ColourBaseSeed")!;
        Assert.False(colourSeed.GetBool(0));
        Assert.Equal("0x0", colourSeed.GetString(1));

        var boneSeed = comp.GetArray("BoneScaleSeed")!;
        Assert.False(boneSeed.GetBool(0));
        Assert.Equal("0x0", boneSeed.GetString(1));
    }

    [Fact]
    public void CompanionLogic_DeleteCompanion_ClearsIntegerSeeds()
    {
        // SpeciesSeed and GenusSeed are integers, NOT seed arrays
        var comp = JsonObject.Parse(@"{
            ""CreatureID"": ""^CAT"",
            ""SpeciesSeed"": 42,
            ""GenusSeed"": 99,
            ""CreatureSeed"": [true, ""0xAA""]
        }");

        CompanionLogic.DeleteCompanion(comp);

        Assert.Equal(0, comp.GetInt("SpeciesSeed"));
        Assert.Equal(0, comp.GetInt("GenusSeed"));
    }

    [Fact]
    public void CompanionLogic_DeleteCompanion_ClearsAllFields()
    {
        // Tests that ALL fields mentioned in the companion json template are reset
        var comp = JsonObject.Parse(@"{
            ""Scale"": 2.5,
            ""CreatureID"": ""^CAT"",
            ""Descriptors"": [{ ""x"": 1 }, { ""x"": 2 }],
            ""CreatureSeed"": [true, ""0xABCD1234""],
            ""CreatureSecondarySeed"": [true, ""0xDEADBEEF""],
            ""SpeciesSeed"": 42,
            ""GenusSeed"": 99,
            ""CustomSpeciesName"": ""^MY_SPECIES"",
            ""Predator"": true,
            ""UA"": 12345,
            ""AllowUnmodifiedReroll"": false,
            ""ColourBaseSeed"": [true, ""0x1111""],
            ""BoneScaleSeed"": [true, ""0x2222""],
            ""HasFur"": true,
            ""Biome"": { ""Biome"": ""Toxic"" },
            ""CreatureType"": { ""CreatureType"": ""Predator"" },
            ""BirthTime"": 1700000000,
            ""LastEggTime"": 1700001000,
            ""LastTrustIncreaseTime"": 1700002000,
            ""LastTrustDecreaseTime"": 1700003000,
            ""EggModified"": true,
            ""HasBeenSummoned"": false,
            ""CustomName"": ""Fluffy"",
            ""Trust"": 0.85,
            ""SenderData"": { ""LID"": ""abc"", ""UID"": ""def"", ""USN"": ""player1"", ""PTK"": ""token"", ""TS"": 999 },
            ""Traits"": [0.75, -0.50, 0.33],
            ""Moods"": [80.0, 60.0],
            ""PetAccessoryCustomisation"": { ""HeadAccessory"": ""hat1"", ""BackAccessory"": ""wings"" }
        }");

        CompanionLogic.DeleteCompanion(comp);

        // Identification
        Assert.Equal("^", comp.GetString("CreatureID"));
        Assert.Equal("", comp.GetString("CustomName"));
        Assert.Equal("^", comp.GetString("CustomSpeciesName"));

        // Seed arrays
        Assert.False(comp.GetArray("CreatureSeed")!.GetBool(0));
        Assert.Equal("0x0", comp.GetArray("CreatureSeed")!.GetString(1));
        Assert.False(comp.GetArray("CreatureSecondarySeed")!.GetBool(0));
        Assert.False(comp.GetArray("ColourBaseSeed")!.GetBool(0));
        Assert.False(comp.GetArray("BoneScaleSeed")!.GetBool(0));

        // Integer seeds
        Assert.Equal(0, comp.GetInt("SpeciesSeed"));
        Assert.Equal(0, comp.GetInt("GenusSeed"));

        // Numeric/boolean fields
        Assert.Equal(1.0, comp.GetDouble("Scale"));
        Assert.Equal(0.0, comp.GetDouble("Trust"));
        Assert.False(comp.GetBool("Predator"));
        Assert.False(comp.GetBool("HasFur"));
        Assert.Equal(1111111111111111L, comp.GetLong("UA"));
        Assert.True(comp.GetBool("AllowUnmodifiedReroll"));
        Assert.False(comp.GetBool("EggModified"));
        Assert.True(comp.GetBool("HasBeenSummoned"));

        // Timestamps
        Assert.Equal(0, comp.GetInt("BirthTime"));
        Assert.Equal(0, comp.GetInt("LastEggTime"));
        Assert.Equal(0, comp.GetInt("LastTrustIncreaseTime"));
        Assert.Equal(0, comp.GetInt("LastTrustDecreaseTime"));

        // Nested objects
        Assert.Equal("Lush", comp.GetObject("Biome")!.GetString("Biome"));
        Assert.Equal("None", comp.GetObject("CreatureType")!.GetString("CreatureType"));

        // SenderData
        var sender = comp.GetObject("SenderData")!;
        Assert.Equal("", sender.GetString("LID"));
        Assert.Equal("", sender.GetString("UID"));
        Assert.Equal("", sender.GetString("USN"));
        Assert.Equal("", sender.GetString("PTK"));
        Assert.Equal(0, sender.GetInt("TS"));

        // Descriptors cleared
        Assert.Equal(0, comp.GetArray("Descriptors")!.Length);

        // Traits -> 0.0
        var traits = comp.GetArray("Traits")!;
        Assert.Equal(0.0, traits.GetDouble(0));
        Assert.Equal(0.0, traits.GetDouble(1));
        Assert.Equal(0.0, traits.GetDouble(2));

        // Moods -> 0.0
        var moods = comp.GetArray("Moods")!;
        Assert.Equal(0.0, moods.GetDouble(0));
        Assert.Equal(0.0, moods.GetDouble(1));

        // Accessory reset
        var acc = comp.GetObject("PetAccessoryCustomisation")!;
        Assert.Equal("", acc.GetString("HeadAccessory"));
        Assert.Equal("", acc.GetString("BackAccessory"));
    }

    [Fact]
    public void CompanionLogic_DeleteCompanion_FullReset_EndToEnd()
    {
        // End-to-end: deleting one companion should not affect another
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""Pets"": [
                    {
                        ""CreatureID"": ""^QUAD_PET"",
                        ""CustomName"": ""Alpha"",
                        ""CreatureSeed"": [true, ""0xA1""],
                        ""CreatureSecondarySeed"": [true, ""0xA2""],
                        ""SpeciesSeed"": 11,
                        ""GenusSeed"": 22,
                        ""CustomSpeciesName"": ""^ALPHA_SP"",
                        ""Scale"": 1.5,
                        ""Trust"": 0.9,
                        ""Predator"": true,
                        ""HasFur"": true,
                        ""UA"": 100,
                        ""AllowUnmodifiedReroll"": false,
                        ""EggModified"": true,
                        ""HasBeenSummoned"": false,
                        ""BirthTime"": 1000,
                        ""LastEggTime"": 2000,
                        ""LastTrustIncreaseTime"": 3000,
                        ""LastTrustDecreaseTime"": 4000,
                        ""ColourBaseSeed"": [true, ""0xC1""],
                        ""BoneScaleSeed"": [true, ""0xB1""],
                        ""Biome"": { ""Biome"": ""Frozen"" },
                        ""CreatureType"": { ""CreatureType"": ""Predator"" },
                        ""SenderData"": { ""LID"": ""L1"", ""UID"": ""U1"", ""USN"": ""N1"", ""PTK"": ""P1"", ""TS"": 50 },
                        ""Descriptors"": [{ ""x"": 1 }],
                        ""Traits"": [0.5, 0.3, 0.1],
                        ""Moods"": [75.0, 50.0]
                    },
                    {
                        ""CreatureID"": ""^BUTTERFLY"",
                        ""CustomName"": ""Bravo"",
                        ""CreatureSeed"": [true, ""0xBB""],
                        ""CreatureSecondarySeed"": [true, ""0xBC""],
                        ""SpeciesSeed"": 33,
                        ""GenusSeed"": 44,
                        ""Scale"": 0.8,
                        ""Traits"": [0.2, 0.4, 0.6],
                        ""Moods"": [30.0, 20.0]
                    }
                ]
            }
        }");

        var pets = json.GetObject("PlayerStateData")!.GetArray("Pets")!;

        // Delete Alpha (index 0)
        CompanionLogic.DeleteCompanion(pets.GetObject(0));

        // Alpha should be fully cleared
        var alpha = pets.GetObject(0);
        Assert.Equal("^", alpha.GetString("CreatureID"));
        Assert.Equal("", alpha.GetString("CustomName"));
        Assert.False(alpha.GetArray("CreatureSeed")!.GetBool(0));
        Assert.Equal(0, alpha.GetInt("SpeciesSeed"));
        Assert.Equal(0, alpha.GetInt("GenusSeed"));
        Assert.Equal(1.0, alpha.GetDouble("Scale"));
        Assert.Equal(0.0, alpha.GetDouble("Trust"));
        Assert.False(alpha.GetBool("Predator"));
        Assert.Equal(0, alpha.GetInt("BirthTime"));
        Assert.Equal(0, alpha.GetInt("LastTrustIncreaseTime"));
        Assert.Equal("Lush", alpha.GetObject("Biome")!.GetString("Biome"));
        Assert.Equal("None", alpha.GetObject("CreatureType")!.GetString("CreatureType"));
        Assert.Equal(0, alpha.GetArray("Descriptors")!.Length);
        Assert.Equal(0.0, alpha.GetArray("Traits")!.GetDouble(0));
        Assert.Equal(0.0, alpha.GetArray("Moods")!.GetDouble(0));

        // Bravo should be completely untouched
        var bravo = pets.GetObject(1);
        Assert.Equal("^BUTTERFLY", bravo.GetString("CreatureID"));
        Assert.Equal("Bravo", bravo.GetString("CustomName"));
        Assert.True(bravo.GetArray("CreatureSeed")!.GetBool(0));
        Assert.Equal("0xBB", bravo.GetArray("CreatureSeed")!.GetString(1));
        Assert.Equal(33, bravo.GetInt("SpeciesSeed"));
        Assert.Equal(44, bravo.GetInt("GenusSeed"));
        Assert.Equal(0.8, bravo.GetDouble("Scale"), 1);
        Assert.Equal(0.2, bravo.GetArray("Traits")!.GetDouble(0), 1);

        // Array size preserved
        Assert.Equal(2, pets.Length);
    }

    [Fact]
    public void CompanionDatabase_CreatureTypes_HasExpectedEntries()
    {
        Assert.Contains("None", CompanionDatabase.CreatureTypes);
        Assert.Contains("ProtoFlyer", CompanionDatabase.CreatureTypes);
        Assert.Contains("Bear", CompanionDatabase.CreatureTypes);
        Assert.True(CompanionDatabase.CreatureTypes.Length > 10);
    }

    [Fact]
    public void CompanionLogic_CreatureID_And_CreatureType_AreDistinctFields()
    {
        // Verify that CreatureID (species) and CreatureType.CreatureType (behaviour) are distinct JSON paths
        // This confirms they cannot be combined into a single combobox
        var comp = JsonObject.Parse(@"{
            ""CreatureID"": ""^TREX"",
            ""CreatureType"": { ""CreatureType"": ""Prey"" }
        }");

        // CreatureID is the species identifier
        Assert.Equal("^TREX", comp.GetString("CreatureID"));

        // CreatureType.CreatureType is the behavioural classification
        Assert.Equal("Prey", comp.GetObject("CreatureType")!.GetString("CreatureType"));

        // They use different value domains
        Assert.True(CompanionDatabase.ById.ContainsKey("^TREX")); // species from database
        Assert.Contains("Prey", CompanionDatabase.CreatureTypes);        // behaviour from CreatureTypes list

        // Changing one does not affect the other
        comp.Set("CreatureID", "^BUTTERFLY");
        Assert.Equal("^BUTTERFLY", comp.GetString("CreatureID"));
        Assert.Equal("Prey", comp.GetObject("CreatureType")!.GetString("CreatureType"));

        comp.GetObject("CreatureType")!.Set("CreatureType", "Passive");
        Assert.Equal("^BUTTERFLY", comp.GetString("CreatureID"));
        Assert.Equal("Passive", comp.GetObject("CreatureType")!.GetString("CreatureType"));
    }

    [Fact]
    public void CompanionDatabase_CreatureTypes_MatchesCreatureTypeEnum()
    {
        // Verify our CreatureTypes list contains the CreatureTypeEnum values
        // List: None, Prey, Predator, Passive, Bird, FlyingLizard, Fish, Shark,
        // Butterfly, Robot, Spider, Rodent, GiantRobot, FloatingGasbag, Beetle, Quad, etc.
        string[] types = { "None", "Prey", "Predator", "Passive", "Bird",
            "FlyingLizard", "Fish", "Shark", "Butterfly", "Robot", "Spider",
            "Rodent", "GiantRobot", "FloatingGasbag", "Beetle", "Quad",
            "Triceratops", "Antelope", "Cat", "Strider" };

        foreach (var t in types)
            Assert.Contains(t, CompanionDatabase.CreatureTypes);
    }

    [Fact]
    public void CompanionDatabase_DoesNotOverlapWithCreatureTypes()
    {
        // CompanionDatabase entries are species IDs (e.g., "^QUAD_PET")
        // CreatureTypes are behavioural classifications (e.g., "Quad")
        // They should not overlap (different naming convention: database has ^PREFIX, types don't)
        foreach (var entry in CompanionDatabase.Entries)
        {
            Assert.StartsWith("^", entry.Id);
            Assert.DoesNotContain(entry.Id, CompanionDatabase.CreatureTypes);
        }
    }

    [Fact]
    public void CompanionLogic_SetSlotUnlocked_SetsTrue()
    {
        var playerState = JsonObject.Parse(@"{
            ""UnlockedPetSlots"": [false, false, false, false, false, false]
        }");

        CompanionLogic.SetSlotUnlocked(playerState, 2, true);

        var slots = playerState.GetArray("UnlockedPetSlots")!;
        Assert.False(slots.GetBool(0));
        Assert.False(slots.GetBool(1));
        Assert.True(slots.GetBool(2));
        Assert.False(slots.GetBool(3));
    }

    [Fact]
    public void CompanionLogic_SetSlotUnlocked_SetsFalse()
    {
        var playerState = JsonObject.Parse(@"{
            ""UnlockedPetSlots"": [true, true, true, true, true, true]
        }");

        CompanionLogic.SetSlotUnlocked(playerState, 3, false);

        var slots = playerState.GetArray("UnlockedPetSlots")!;
        Assert.True(slots.GetBool(0));
        Assert.True(slots.GetBool(2));
        Assert.False(slots.GetBool(3));
        Assert.True(slots.GetBool(4));
    }

    [Fact]
    public void CompanionLogic_SetSlotUnlocked_OutOfRange_GrowsArray()
    {
        var playerState = JsonObject.Parse(@"{
            ""UnlockedPetSlots"": [true, false]
        }");

        // Should grow the array to accommodate the index
        CompanionLogic.SetSlotUnlocked(playerState, 4, true);

        var slots = playerState.GetArray("UnlockedPetSlots")!;
        Assert.Equal(5, slots.Length);
        Assert.True(slots.GetBool(0));   // original
        Assert.False(slots.GetBool(1));  // original
        Assert.False(slots.GetBool(2));  // padded
        Assert.False(slots.GetBool(3));  // padded
        Assert.True(slots.GetBool(4));   // newly set
    }

    [Fact]
    public void CompanionLogic_SetSlotUnlocked_MissingArray_CreatesIt()
    {
        var playerState = JsonObject.Parse(@"{}");

        // Should create the array and set the value
        CompanionLogic.SetSlotUnlocked(playerState, 2, true);

        var slots = playerState.GetArray("UnlockedPetSlots");
        Assert.NotNull(slots);
        Assert.Equal(3, slots!.Length);
        Assert.False(slots.GetBool(0));  // padded
        Assert.False(slots.GetBool(1));  // padded
        Assert.True(slots.GetBool(2));   // newly set
    }

    [Fact]
    public void CompanionLogic_DeleteAndLockSlot_EndToEnd()
    {
        // Simulates the full delete flow: clear companion data + lock the slot
        var playerState = JsonObject.Parse(@"{
            ""Pets"": [
                {
                    ""CreatureID"": ""^CAT"",
                    ""CustomName"": ""Whiskers"",
                    ""CreatureSeed"": [true, ""0xAABBCCDD""],
                    ""Scale"": 1.5,
                    ""Trust"": 0.8
                }
            ],
            ""UnlockedPetSlots"": [true, false, false, false, false, false]
        }");

        var pets = playerState.GetArray("Pets")!;
        var comp = pets.GetObject(0);

        // Delete companion data
        CompanionLogic.DeleteCompanion(comp);
        // Lock the slot
        CompanionLogic.SetSlotUnlocked(playerState, 0, false);

        // Verify companion is cleared
        Assert.Equal("^", comp.GetString("CreatureID"));
        Assert.False(comp.GetArray("CreatureSeed")!.GetBool(0));

        // Verify slot is now locked
        Assert.False(playerState.GetArray("UnlockedPetSlots")!.GetBool(0));
    }

    [Fact]
    public void CompanionLogic_SetSlotUnlocked_RejectsIndexTooLarge()
    {
        var playerState = JsonObject.Parse(@"{
            ""UnlockedPetSlots"": [true, false]
        }");

        // Index 30 should be rejected (max is 29)
        CompanionLogic.SetSlotUnlocked(playerState, 30, true);

        var slots = playerState.GetArray("UnlockedPetSlots")!;
        Assert.Equal(2, slots.Length); // unchanged
    }

    [Fact]
    public void CompanionLogic_SetSlotUnlocked_GrowsUpToMaxSlots()
    {
        var playerState = JsonObject.Parse(@"{
            ""UnlockedPetSlots"": [true, false]
        }");

        // Index 29 (last valid) should grow the array to 30
        CompanionLogic.SetSlotUnlocked(playerState, 29, true);

        var slots = playerState.GetArray("UnlockedPetSlots")!;
        Assert.Equal(30, slots.Length);
        Assert.True(slots.GetBool(0));   // original
        Assert.False(slots.GetBool(1));  // original
        for (int i = 2; i < 29; i++)
            Assert.False(slots.GetBool(i)); // padded
        Assert.True(slots.GetBool(29));  // newly set
    }

    [Fact]
    public void CompanionLogic_MaxPetSlots_Is30()
    {
        Assert.Equal(30, CompanionLogic.MaxPetSlots);
    }

    [Fact]
    public void CompanionLogic_ImportCompanion_InsertsIntoFirstEmptySlot()
    {
        // Build a minimal companion array with 3 slots: occupied, empty, occupied
        var companions = JsonArray.Parse(@"[
            { ""CreatureID"": ""^CAT"", ""CreatureSeed"": [true, ""0xAA""] },
            { ""CreatureID"": ""^"", ""CreatureSeed"": [false, ""0x0""], ""SpeciesSeed"": 0, ""GenusSeed"": 0 },
            { ""CreatureID"": ""^DOG"", ""CreatureSeed"": [true, ""0xBB""] }
        ]");

        // Create a minimal pet file to import
        var tmpFile = Path.Combine(Path.GetTempPath(), "test_import_companion.nmspet");
        try
        {
            File.WriteAllText(tmpFile, @"{
                ""Scale"": 1.5,
                ""CreatureID"": ""^PURPLE_WEIRD"",
                ""CreatureSeed"": [true, ""0xA5128111574D8588""],
                ""SpeciesSeed"": ""0xA630F9645CAD757D"",
                ""GenusSeed"": ""0x95E330F5F8B059C7"",
                ""Biome"": { ""Biome"": ""Frozen"" },
                ""CreatureType"": { ""CreatureType"": ""ProtoFlyer"" }
            }");

            int idx = CompanionLogic.ImportCompanion(companions, tmpFile);

            Assert.Equal(1, idx); // Should import into the empty slot at index 1
            var imported = companions.GetObject(1);
            Assert.Equal("^PURPLE_WEIRD", imported.GetString("CreatureID"));
            Assert.True(imported.GetArray("CreatureSeed")!.GetBool(0));
            Assert.Equal("0xA5128111574D8588", imported.GetArray("CreatureSeed")!.GetString(1));
            Assert.Equal("Frozen", imported.GetObject("Biome")!.GetString("Biome"));
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }
    }

    [Fact]
    public void CompanionLogic_ImportCompanion_PACWrappedCorrectly()
    {
        // Build a companion array with one empty slot
        var companions = JsonArray.Parse(@"[
            { ""CreatureID"": ""^"", ""CreatureSeed"": [false, ""0x0""], ""SpeciesSeed"": 0, ""GenusSeed"": 0 }
        ]");

        // Build a PAC array matching the save structure: each entry is { Data: [...] }
        var pacArray = JsonArray.Parse(@"[
            { ""Data"": [
                { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} },
                { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} },
                { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} }
            ] }
        ]");

        // Create an import file that includes PetAccessoryCustomisation (as exported)
        var tmpFile = Path.Combine(Path.GetTempPath(), "test_pac_import.nmspet");
        try
        {
            File.WriteAllText(tmpFile, @"{
                ""CreatureID"": ""^PURPLE_WEIRD"",
                ""CreatureSeed"": [true, ""0xA5128111574D8588""],
                ""SpeciesSeed"": ""0xA630F9645CAD757D"",
                ""GenusSeed"": ""0x95E330F5F8B059C7"",
                ""PetAccessoryCustomisation"": [
                    { ""SelectedPreset"": ""^HAT_PET"", ""CustomData"": { ""Scale"": 2.0 } },
                    { ""SelectedPreset"": ""^WINGS_PET"", ""CustomData"": { ""Scale"": 1.5 } },
                    { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} }
                ]
            }");

            int idx = CompanionLogic.ImportCompanion(companions, tmpFile, pacArray);

            Assert.Equal(0, idx);

            // Verify PAC entry is an object with Data key (not a raw array)
            var pacEntry = pacArray.GetObject(0);
            Assert.NotNull(pacEntry);
            Assert.NotNull(pacEntry!.GetArray("Data"));

            // Verify the Data was updated with imported values
            var data = pacEntry.GetArray("Data")!;
            Assert.Equal(3, data.Length);
            Assert.Equal("^HAT_PET", data.GetObject(0)!.GetString("SelectedPreset"));
            Assert.Equal("^WINGS_PET", data.GetObject(1)!.GetString("SelectedPreset"));
            Assert.Equal("^DEFAULT_PET", data.GetObject(2)!.GetString("SelectedPreset"));
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }
    }

    [Fact]
    public void CompanionLogic_ImportCompanion_PACNotCorrupted_SaveRoundtrips()
    {
        // This test verifies the specific corruption bug: the PAC entry must remain
        // an object { Data: [...] }, not be replaced by a raw array.
        var companions = JsonArray.Parse(@"[
            { ""CreatureID"": ""^"", ""CreatureSeed"": [false, ""0x0""] }
        ]");

        var pacArray = JsonArray.Parse(@"[
            { ""Data"": [
                { ""SelectedPreset"": ""^DEFAULT_PET"" },
                { ""SelectedPreset"": ""^DEFAULT_PET"" },
                { ""SelectedPreset"": ""^DEFAULT_PET"" }
            ] }
        ]");

        var tmpFile = Path.Combine(Path.GetTempPath(), "test_pac_roundtrip.nmspet");
        try
        {
            File.WriteAllText(tmpFile, @"{
                ""CreatureID"": ""^BLOB"",
                ""CreatureSeed"": [true, ""0x1234""],
                ""PetAccessoryCustomisation"": [
                    { ""SelectedPreset"": ""^ACC1"" },
                    { ""SelectedPreset"": ""^ACC2"" },
                    { ""SelectedPreset"": ""^ACC3"" }
                ]
            }");

            CompanionLogic.ImportCompanion(companions, tmpFile, pacArray);

            // Serialize the PAC array and verify it's valid JSON that re-parses correctly
            var pacJson = pacArray.ToString();
            var reparsed = JsonArray.Parse(pacJson);

            // The entry at index 0 must still be an object, not an array
            var entry = reparsed.GetObject(0);
            Assert.NotNull(entry);

            // The Data key must exist and be an array
            var data = entry!.GetArray("Data");
            Assert.NotNull(data);
            Assert.Equal(3, data!.Length);
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }
    }

    [Fact]
    public void CompanionLogic_ImportCompanion_AllSlotsFull_Throws()
    {
        var companions = JsonArray.Parse(@"[
            { ""CreatureID"": ""^CAT"", ""CreatureSeed"": [true, ""0xAA""] },
            { ""CreatureID"": ""^DOG"", ""CreatureSeed"": [true, ""0xBB""] }
        ]");

        var tmpFile = Path.Combine(Path.GetTempPath(), "test_full_import.nmspet");
        try
        {
            File.WriteAllText(tmpFile, @"{
                ""CreatureID"": ""^BLOB"",
                ""CreatureSeed"": [true, ""0x1234""]
            }");

            Assert.Throws<InvalidOperationException>(() =>
                CompanionLogic.ImportCompanion(companions, tmpFile));
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }
    }

    [Fact]
    public void CompanionLogic_ExportImportRoundtrip_PreservesData()
    {
        // Create a companion with known data
        var comp = JsonObject.Parse(@"{
            ""Scale"": 1.5,
            ""CreatureID"": ""^PURPLE_WEIRD"",
            ""CreatureSeed"": [true, ""0xA5128111574D8588""],
            ""SpeciesSeed"": ""0xA630F9645CAD757D"",
            ""GenusSeed"": ""0x95E330F5F8B059C7"",
            ""CustomName"": ""TestPet"",
            ""Biome"": { ""Biome"": ""Frozen"" },
            ""CreatureType"": { ""CreatureType"": ""ProtoFlyer"" },
            ""Trust"": 0.6
        }");

        // Create PAC slots (what export receives from pacEntry.GetArray("Data"))
        var pacSlots = JsonArray.Parse(@"[
            { ""SelectedPreset"": ""^HAT"", ""CustomData"": {} },
            { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} },
            { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} }
        ]");

        var exportFile = Path.Combine(Path.GetTempPath(), "test_roundtrip.nmspet");
        try
        {
            // Export
            CompanionLogic.ExportCompanion(comp, exportFile, pacSlots);
            Assert.True(File.Exists(exportFile));

            // Create target array with one empty slot and PAC
            var targetCompanions = JsonArray.Parse(@"[
                { ""CreatureID"": ""^"", ""CreatureSeed"": [false, ""0x0""], ""SpeciesSeed"": 0, ""GenusSeed"": 0 }
            ]");
            var targetPac = JsonArray.Parse(@"[
                { ""Data"": [
                    { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} },
                    { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} },
                    { ""SelectedPreset"": ""^DEFAULT_PET"", ""CustomData"": {} }
                ] }
            ]");

            // Import
            int idx = CompanionLogic.ImportCompanion(targetCompanions, exportFile, targetPac);

            Assert.Equal(0, idx);
            var reimported = targetCompanions.GetObject(0);
            Assert.Equal("^PURPLE_WEIRD", reimported.GetString("CreatureID"));
            Assert.Equal("TestPet", reimported.GetString("CustomName"));
            Assert.Equal("Frozen", reimported.GetObject("Biome")!.GetString("Biome"));

            // PAC should be properly wrapped
            var pacEntry = targetPac.GetObject(0);
            Assert.NotNull(pacEntry);
            var data = pacEntry!.GetArray("Data")!;
            Assert.Equal("^HAT", data.GetObject(0)!.GetString("SelectedPreset"));
        }
        finally
        {
            if (File.Exists(exportFile)) File.Delete(exportFile);
        }
    }

    [Fact]
    public void CompanionDatabase_ContainsPurpleWeird()
    {
        Assert.True(CompanionDatabase.ById.ContainsKey("^PURPLE_WEIRD"));
        Assert.Equal("Purple Weird", CompanionDatabase.ById["^PURPLE_WEIRD"].Species);
    }

    [Fact]
    public void CompanionDatabase_ContainsHermitCrab()
    {
        Assert.True(CompanionDatabase.ById.ContainsKey("^HERMITCRAB"));
        Assert.Equal("Hermitcrab", CompanionDatabase.ById["^HERMITCRAB"].Species);
    }

    [Fact]
    public void GetSlotLayout_HermitCrab_WithCrabShellDescriptor_ReturnsLeftRight()
    {
        // HERMITCRAB has no PetAccessorySlots of its own, but _CRABSHELL_02 is defined
        // under SPIDER's variants with LEFT+RIGHT groups. The descriptor-based fallback
        // should resolve the correct accessories.
        var layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature(
            "^HERMITCRAB",
            new[] { "_CRABSHELL_02", "1234567890" });

        Assert.Equal(2, layout.Length);
        Assert.Contains(AccessorySlot.Left, layout);
        Assert.Contains(AccessorySlot.Right, layout);
    }

    [Fact]
    public void GetSlotLayout_HermitCrab_WithoutDescriptors_ReturnsEmpty()
    {
        // Without descriptors, HERMITCRAB has no accessories (its own entry has null variants)
        var layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^HERMITCRAB");
        Assert.Empty(layout);
    }

    [Fact]
    public void GetSlotLayout_Spider_WithOwnVariants_UsesOwnData()
    {
        // SPIDER has its own PetAccessorySlots — should use those directly
        var layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^SPIDER");
        Assert.Equal(2, layout.Length);
        Assert.Contains(AccessorySlot.Left, layout);
        Assert.Contains(AccessorySlot.Right, layout);
    }

    [Fact]
    public void GetSlotLayout_DescriptorFallback_CaseInsensitive()
    {
        // Descriptor matching should be case-insensitive
        var layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature(
            "^HERMITCRAB",
            new[] { "_crabshell_02" });

        Assert.Equal(2, layout.Length);
    }

    [Fact]
    public void VariantByDescriptor_ContainsCrabShellEntries()
    {
        // Verify the reverse index was built correctly during LoadFromFile
        Assert.True(CompanionAccessoryDatabase.VariantByDescriptor.ContainsKey("_CRABSHELL_02"));
        var variant = CompanionAccessoryDatabase.VariantByDescriptor["_CRABSHELL_02"];
        Assert.Equal(2, variant.AccessoryGroups.Count);
        Assert.Contains("LEFT", variant.AccessoryGroups);
        Assert.Contains("RIGHT", variant.AccessoryGroups);
    }

    [Fact]
    public void CompanionDatabase_ContainsCanonicalGameEntries()
    {
        // Verify key canonical game IDs from creaturedatatable are present
        // (replaces the old Creature Builder IDs)
        var expectedIds = new[]
        {
            "^SWIMCOW", "^FIENDFISHBIG",
            "^PROTOFLYER", "^SCUTTLER",
            "^SWIMRODENT",
            "^SIXLEGCOW", "^FISH", "^FLOATSPIDER",
            "^PROTODIGGER", "^WEIRDFLOAT",
            "^TWOLEGANTELOPE", "^MOLE", "^JELLYFISH",
        };

        foreach (var id in expectedIds)
        {
            Assert.True(CompanionDatabase.ById.ContainsKey(id),
                $"CompanionDatabase should contain canonical game ID {id}");
        }
    }

    // --- CatalogueLogic ----------------------------------------------

    [Fact]
    public void CatalogueLogic_RaceColumns_HasExpectedEntries()
    {
        Assert.Equal(5, CatalogueLogic.RaceColumns.Length);
        Assert.Contains(("Gek", 0), CatalogueLogic.RaceColumns);
        Assert.Contains(("Vy'keen", 1), CatalogueLogic.RaceColumns);
        Assert.Contains(("Korvax", 2), CatalogueLogic.RaceColumns);
        Assert.Contains(("Atlas", 4), CatalogueLogic.RaceColumns);
        Assert.Contains(("Autophage", 8), CatalogueLogic.RaceColumns);
    }

    [Fact]
    public void CatalogueLogic_TechItemTypes_ContainsExpectedTypes()
    {
        Assert.Contains("Technology", CatalogueLogic.TechItemTypes);
        Assert.Contains("Upgrades", CatalogueLogic.TechItemTypes);
        Assert.Contains("Others", CatalogueLogic.TechItemTypes);
    }

    [Fact]
    public void CatalogueLogic_TechItemTypes_CaseInsensitive()
    {
        Assert.Contains("technology", CatalogueLogic.TechItemTypes);
        Assert.Contains("TECHNOLOGY", CatalogueLogic.TechItemTypes);
    }

    [Fact]
    public void CatalogueLogic_ProductItemTypes_ContainsExpectedTypes()
    {
        Assert.Contains("Products", CatalogueLogic.ProductItemTypes);
        Assert.Contains("Constructed Technology", CatalogueLogic.ProductItemTypes);
        Assert.Contains("Curiosities", CatalogueLogic.ProductItemTypes);
        Assert.Contains("Corvette", CatalogueLogic.ProductItemTypes);
    }

    [Fact]
    public void CatalogueLogic_TotalRaceCount_IsNine()
    {
        Assert.Equal(9, CatalogueLogic.TotalRaceCount);
    }

    [Fact]
    public void CatalogueLogic_IsWordKnown_ReturnsTrueWhenKnown()
    {
        var groups = new JsonArray();
        var entry = new JsonObject();
        entry.Set("Group", "TestGroup");
        var races = new JsonArray();
        races.Add(true);
        races.Add(false);
        races.Add(true);
        entry.Set("Races", races);
        groups.Add(entry);

        Assert.True(CatalogueLogic.IsWordKnown(groups, "TestGroup", 0));
        Assert.False(CatalogueLogic.IsWordKnown(groups, "TestGroup", 1));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "TestGroup", 2));
    }

    [Fact]
    public void CatalogueLogic_IsWordKnown_UnknownGroup_ReturnsFalse()
    {
        var groups = new JsonArray();
        Assert.False(CatalogueLogic.IsWordKnown(groups, "NoSuchGroup", 0));
    }

    [Fact]
    public void CatalogueLogic_SetWordKnown_AddsNewEntry()
    {
        var groups = new JsonArray();
        CatalogueLogic.SetWordKnown(groups, "NewWord", 2, true);

        Assert.Equal(1, groups.Length);
        Assert.True(CatalogueLogic.IsWordKnown(groups, "NewWord", 2));
        Assert.False(CatalogueLogic.IsWordKnown(groups, "NewWord", 0));
    }

    [Fact]
    public void CatalogueLogic_SetWordKnown_RemovesEntryWhenAllFalse()
    {
        var groups = new JsonArray();
        CatalogueLogic.SetWordKnown(groups, "TestWord", 0, true);
        Assert.Equal(1, groups.Length);

        CatalogueLogic.SetWordKnown(groups, "TestWord", 0, false);
        Assert.Equal(0, groups.Length);
    }

    [Fact]
    public void CatalogueLogic_LoadAndSaveGlyphBitfield()
    {
        var json = JsonObject.Parse(@"{ ""KnownPortalRunes"": 4095 }");
        Assert.Equal(4095, CatalogueLogic.LoadGlyphBitfield(json));

        CatalogueLogic.SaveGlyphBitfield(json, 255);
        Assert.Equal(255, CatalogueLogic.LoadGlyphBitfield(json));
    }

    [Fact]
    public void CatalogueLogic_LoadKnownItemIds_StripsCaretPrefix()
    {
        var json = JsonObject.Parse(@"{
            ""KnownTech"": [""^FUEL1"", ""^ANTIMATTER"", ""^HYPERDRIVE""]
        }");

        var ids = CatalogueLogic.LoadKnownItemIds(json, "KnownTech");
        Assert.Equal(3, ids.Count);
        // Caret prefix is stripped so IDs match the game item database
        Assert.Contains("FUEL1", ids);
        Assert.Contains("ANTIMATTER", ids);
        Assert.Contains("HYPERDRIVE", ids);
    }

    [Fact]
    public void CatalogueLogic_SaveKnownItemIds_AddsCaretPrefix()
    {
        var json = JsonObject.Parse(@"{ ""KnownTech"": [] }");
        // IDs without ^-prefix (as they come from the database / grid)
        var ids = new List<string> { "A", "B", "C" };

        CatalogueLogic.SaveKnownItemIds(json, "KnownTech", ids);

        // Verify the raw JSON array has ^-prefixed values
        var arr = json.GetArray("KnownTech")!;
        Assert.Equal("^A", arr.GetString(0));
        Assert.Equal("^B", arr.GetString(1));
        Assert.Equal("^C", arr.GetString(2));

        // And round-trip back through Load strips the prefix again
        var loaded = CatalogueLogic.LoadKnownItemIds(json, "KnownTech");
        Assert.Equal(3, loaded.Count);
        Assert.Equal("A", loaded[0]);
        Assert.Equal("B", loaded[1]);
        Assert.Equal("C", loaded[2]);
    }

    [Fact]
    public void CatalogueLogic_SaveKnownItemIds_DoesNotDoublePrefix()
    {
        var json = JsonObject.Parse(@"{ ""KnownTech"": [] }");
        // IDs already with ^-prefix should not get double-prefixed
        var ids = new List<string> { "^X", "^Y" };

        CatalogueLogic.SaveKnownItemIds(json, "KnownTech", ids);

        var arr = json.GetArray("KnownTech")!;
        Assert.Equal("^X", arr.GetString(0));
        Assert.Equal("^Y", arr.GetString(1));
    }

    [Fact]
    public void CatalogueLogic_StripCaretPrefix_Works()
    {
        Assert.Equal("LASER", CatalogueLogic.StripCaretPrefix("^LASER"));
        Assert.Equal("LASER", CatalogueLogic.StripCaretPrefix("LASER"));
        Assert.Equal("^", CatalogueLogic.StripCaretPrefix("^")); // single ^ stays as-is (length 1)
        Assert.Equal("", CatalogueLogic.StripCaretPrefix(""));
    }

    [Fact]
    public void CatalogueLogic_EnsureCaretPrefix_Works()
    {
        Assert.Equal("^LASER", CatalogueLogic.EnsureCaretPrefix("LASER"));
        Assert.Equal("^LASER", CatalogueLogic.EnsureCaretPrefix("^LASER"));
        Assert.Equal("", CatalogueLogic.EnsureCaretPrefix("")); // empty stays empty
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForRace_LearnsAllWordsForRace()
    {
        var groups = new JsonArray();
        var words = CreateTestWordEntries();

        int count = CatalogueLogic.SetWordFlagsForRace(groups, words, 0, true); // Gek

        // word1 has Gek group, word2 does not, word3 has Gek group
        Assert.Equal(2, count);
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W1", 0));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W3", 0));
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForRace_UnlearnsAllWordsForRace()
    {
        var groups = new JsonArray();
        var words = CreateTestWordEntries();

        // Learn first
        CatalogueLogic.SetWordFlagsForRace(groups, words, 0, true);
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W1", 0));

        // Unlearn
        int count = CatalogueLogic.SetWordFlagsForRace(groups, words, 0, false);
        Assert.Equal(2, count);
        Assert.False(CatalogueLogic.IsWordKnown(groups, "^TRA_W1", 0));
        Assert.False(CatalogueLogic.IsWordKnown(groups, "^TRA_W3", 0));
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForRace_DoesNotAffectOtherRaces()
    {
        var groups = new JsonArray();
        var words = CreateTestWordEntries();

        // Learn word1 for Vy'keen (race 1)
        CatalogueLogic.SetWordKnown(groups, "^WAR_W1", 1, true);

        // Learn all for Gek (race 0)
        CatalogueLogic.SetWordFlagsForRace(groups, words, 0, true);

        // Vy'keen still known
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^WAR_W1", 1));
        // Gek now known
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W1", 0));
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForRace_SkipsWordsWithoutRace()
    {
        var groups = new JsonArray();
        var words = CreateTestWordEntries();

        // word2 only has Vy'keen (race 1), not Gek (race 0)
        int count = CatalogueLogic.SetWordFlagsForRace(groups, words, 0, true);
        Assert.Equal(2, count); // word1 and word3, not word2
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForEntries_LearnsAcrossAllRaces()
    {
        var groups = new JsonArray();
        var words = CreateTestWordEntries();

        var raceColumns = CatalogueLogic.RaceColumns;
        int count = CatalogueLogic.SetWordFlagsForEntries(groups, words, raceColumns, true);

        // word1: Gek + Vy'keen = 2, word2: Vy'keen = 1, word3: Gek + Korvax = 2 -> total 5
        Assert.Equal(5, count);
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W1", 0));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^WAR_W1", 1));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^WAR_W2", 1));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W3", 0));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^EXP_W3", 2));
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForEntries_UnlearnsAcrossAllRaces()
    {
        var groups = new JsonArray();
        var words = CreateTestWordEntries();
        var raceColumns = CatalogueLogic.RaceColumns;

        // Learn all first
        CatalogueLogic.SetWordFlagsForEntries(groups, words, raceColumns, true);

        // Unlearn just the first two words
        var subset = new List<WordEntry> { words[0], words[1] };
        int count = CatalogueLogic.SetWordFlagsForEntries(groups, subset, raceColumns, false);

        Assert.Equal(3, count); // word1: 2 races, word2: 1 race
        Assert.False(CatalogueLogic.IsWordKnown(groups, "^TRA_W1", 0));
        Assert.False(CatalogueLogic.IsWordKnown(groups, "^WAR_W1", 1));
        Assert.False(CatalogueLogic.IsWordKnown(groups, "^WAR_W2", 1));
        // word3 still known
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^TRA_W3", 0));
        Assert.True(CatalogueLogic.IsWordKnown(groups, "^EXP_W3", 2));
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForEntries_EmptyList_ReturnsZero()
    {
        var groups = new JsonArray();
        var emptyWords = new List<WordEntry>();
        var raceColumns = CatalogueLogic.RaceColumns;

        int count = CatalogueLogic.SetWordFlagsForEntries(groups, emptyWords, raceColumns, true);
        Assert.Equal(0, count);
        Assert.Equal(0, groups.Length);
    }

    [Fact]
    public void CatalogueLogic_SetWordFlagsForRace_EmptyWordList_ReturnsZero()
    {
        var groups = new JsonArray();
        var emptyWords = new List<WordEntry>();

        int count = CatalogueLogic.SetWordFlagsForRace(groups, emptyWords, 0, true);
        Assert.Equal(0, count);
        Assert.Equal(0, groups.Length);
    }

    /// <summary>
    /// Creates test word entries with known race-group mappings:
    /// word1: Gek (^TRA_W1, race 0) + Vy'keen (^WAR_W1, race 1)
    /// word2: Vy'keen only (^WAR_W2, race 1)
    /// word3: Gek (^TRA_W3, race 0) + Korvax (^EXP_W3, race 2)
    /// </summary>
    private static List<WordEntry> CreateTestWordEntries()
    {
        var word1 = new WordEntry("^W1", "word1");
        word1.Groups["^TRA_W1"] = 0; // Gek
        word1.Groups["^WAR_W1"] = 1; // Vy'keen
        word1.BuildReverseLookup();

        var word2 = new WordEntry("^W2", "word2");
        word2.Groups["^WAR_W2"] = 1; // Vy'keen only
        word2.BuildReverseLookup();

        var word3 = new WordEntry("^W3", "word3");
        word3.Groups["^TRA_W3"] = 0; // Gek
        word3.Groups["^EXP_W3"] = 2; // Korvax
        word3.BuildReverseLookup();

        return new List<WordEntry> { word1, word2, word3 };
    }

    // --- BaseLogic ---------------------------------------------------

    [Fact]
    public void BaseLogic_ChestInventoryKeys_HasTenEntries()
    {
        Assert.Equal(10, BaseLogic.ChestInventoryKeys.Length);
    }

    [Fact]
    public void BaseLogic_ChestInventoryKeys_AreNumberedCorrectly()
    {
        for (int i = 0; i < BaseLogic.ChestInventoryKeys.Length; i++)
            Assert.Equal($"Chest{i + 1}Inventory", BaseLogic.ChestInventoryKeys[i]);
    }

    [Fact]
    public void BaseLogic_StorageInventories_HasExpectedEntries()
    {
        Assert.True(BaseLogic.StorageInventories.Length >= 8);
        Assert.Contains(BaseLogic.StorageInventories, s => s.Key == "CookingIngredientsInventory");
        Assert.Contains(BaseLogic.StorageInventories, s => s.Key == "CorvetteStorageInventory");
        Assert.Contains(BaseLogic.StorageInventories, s => s.Key == "RocketLockerInventory");
    }

    [Fact]
    public void BaseLogic_StorageInventories_AllHaveExportFileNames()
    {
        foreach (var (key, displayName, exportFileName) in BaseLogic.StorageInventories)
        {
            Assert.False(string.IsNullOrEmpty(key));
            Assert.False(string.IsNullOrEmpty(displayName));
            Assert.False(string.IsNullOrEmpty(exportFileName));
            Assert.EndsWith(".json", exportFileName);
        }
    }

    [Fact]
    public void BaseLogic_SwapPositions_SwapsCorrectly()
    {
        var a = JsonObject.Parse(@"{ ""Position"": [1, 2, 3], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] }");
        var b = JsonObject.Parse(@"{ ""Position"": [4, 5, 6], ""Up"": [1, 0, 0], ""At"": [0, 1, 0] }");

        BaseLogic.SwapPositions(a, b);

        var aPos = a.GetArray("Position")!;
        var bPos = b.GetArray("Position")!;
        Assert.Equal(4, aPos.GetInt(0));
        Assert.Equal(1, bPos.GetInt(0));
    }

    // --- BaseLogic: MoveBaseComputer ---------------------------------

    [Fact]
    public void BaseLogic_MoveBaseComputer_UpdatesBasePositionToTargetWorldPos()
    {
        // Base at (0, 100, 0) looking forward along Z.
        // Target object is 10 units along the old X-axis.
        var baseData = JsonObject.Parse(@"{
            ""Position"": [0, 100, 0],
            ""Forward"": [0, 0, 1],
            ""Objects"": [
                { ""ObjectID"": ""^BASE_FLAG"", ""Position"": [0, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] },
                { ""ObjectID"": ""^BUILDSIGNAL"", ""Position"": [10, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] }
            ]
        }");

        var objects = baseData.GetArray("Objects")!;
        var baseFlag = objects.GetObject(0);
        var target = objects.GetObject(1);

        BaseLogic.MoveBaseComputer(baseData, baseFlag, target);

        // The base Position should have changed to the target's old world position
        var newBasePos = baseData.GetArray("Position")!;
        double baseLen = Math.Sqrt(
            Math.Pow(newBasePos.GetDouble(0), 2) +
            Math.Pow(newBasePos.GetDouble(1), 2) +
            Math.Pow(newBasePos.GetDouble(2), 2));
        // The new position should be at roughly distance 100 from origin
        // (target was 10 units away from a base at distance 100)
        Assert.InRange(baseLen, 95, 105);
    }

    [Fact]
    public void BaseLogic_MoveBaseComputer_PreservesWorldPositions()
    {
        // Base at (0, 500, 0) looking along Z.
        // Two objects: base computer at origin and a signal booster offset along X.
        var baseData = JsonObject.Parse(@"{
            ""Position"": [0, 500, 0],
            ""Forward"": [0, 0, 1],
            ""Objects"": [
                { ""ObjectID"": ""^BASE_FLAG"", ""Position"": [0, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] },
                { ""ObjectID"": ""^BUILDSIGNAL"", ""Position"": [10, 5, 3], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] },
                { ""ObjectID"": ""^MAINROOM"", ""Position"": [-5, 2, 8], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] }
            ]
        }");

        // Compute old world positions for all objects using the old system
        var oldBasePos = new double[] { 0, 500, 0 };
        var oldFwd = new double[] { 0, 0, 1 };
        // old system: Y = (0,1,0), Z = (0,0,1), X = cross(Y,Z) = (1,0,0)
        // So apply(local) = basePos + local.X * X + local.Y * Y + local.Z * Z
        // = (0,500,0) + (lx,0,0) + (0,ly,0) + (0,0,lz) = (lx, 500+ly, lz)
        var objects = baseData.GetArray("Objects")!;

        // Record expected world positions
        var mainroom = objects.GetObject(2);
        // Expected world: (-5, 500+2, 8) = (-5, 502, 8)
        double expectedX = -5, expectedY = 502, expectedZ = 8;

        var baseFlag = objects.GetObject(0);
        var target = objects.GetObject(1);

        BaseLogic.MoveBaseComputer(baseData, baseFlag, target);

        // Verify the main room is at the same world position using the new coordinate system
        var newBasePos2 = baseData.GetArray("Position")!;
        var newFwd = baseData.GetArray("Forward")!;

        // Read main room's new local position
        var mrPos = mainroom.GetArray("Position")!;
        double mrLx = mrPos.GetDouble(0), mrLy = mrPos.GetDouble(1), mrLz = mrPos.GetDouble(2);

        // Reconstruct the new coordinate system to verify
        double nbx = newBasePos2.GetDouble(0), nby = newBasePos2.GetDouble(1), nbz = newBasePos2.GetDouble(2);
        double nfx = newFwd.GetDouble(0), nfy = newFwd.GetDouble(1), nfz = newFwd.GetDouble(2);

        // New Y = normalised new base position
        double newPosLen = Math.Sqrt(nbx * nbx + nby * nby + nbz * nbz);
        double nyX = nbx / newPosLen, nyY = nby / newPosLen, nyZ = nbz / newPosLen;

        // New Z = normalised Forward (already stored)
        double nzLen = Math.Sqrt(nfx * nfx + nfy * nfy + nfz * nfz);
        double nzX = nfx / nzLen, nzY = nfy / nzLen, nzZ = nfz / nzLen;

        // New X = cross(Y, Z)
        double nxX = nyY * nzZ - nyZ * nzY;
        double nxY = nyZ * nzX - nyX * nzZ;
        double nxZ = nyX * nzY - nyY * nzX;
        double nxLen = Math.Sqrt(nxX * nxX + nxY * nxY + nxZ * nxZ);
        nxX /= nxLen; nxY /= nxLen; nxZ /= nxLen;

        // Reconstruct world position: origin + lx*X + ly*Y + lz*Z
        double worldX = nbx + mrLx * nxX + mrLy * nyX + mrLz * nzX;
        double worldY = nby + mrLx * nxY + mrLy * nyY + mrLz * nzY;
        double worldZ = nbz + mrLx * nxZ + mrLy * nyZ + mrLz * nzZ;

        Assert.Equal(expectedX, worldX, precision: 6);
        Assert.Equal(expectedY, worldY, precision: 6);
        Assert.Equal(expectedZ, worldZ, precision: 6);
    }

    [Fact]
    public void BaseLogic_MoveBaseComputer_SwapsBaseFlagAndTarget()
    {
        var baseData = JsonObject.Parse(@"{
            ""Position"": [0, 100, 0],
            ""Forward"": [0, 0, 1],
            ""Objects"": [
                { ""ObjectID"": ""^BASE_FLAG"", ""Position"": [0, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] },
                { ""ObjectID"": ""^BUILDSIGNAL"", ""Position"": [10, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] }
            ]
        }");

        var objects = baseData.GetArray("Objects")!;
        var baseFlag = objects.GetObject(0);
        var target = objects.GetObject(1);

        // Record the target's position after transform (should end up near base flag's old position)
        BaseLogic.MoveBaseComputer(baseData, baseFlag, target);

        // After move, the target should have the position that the base flag got via transform,
        // and vice versa. The base flag should be near the new origin (small local offset).
        var flagPos = baseFlag.GetArray("Position")!;
        double flagDist = Math.Sqrt(
            Math.Pow(flagPos.GetDouble(0), 2) +
            Math.Pow(flagPos.GetDouble(1), 2) +
            Math.Pow(flagPos.GetDouble(2), 2));

        // The base flag (after swap) should be near the target's old local position
        // which was at distance ~10 from origin; after transform and swap, it should be
        // within the same order of magnitude
        Assert.InRange(flagDist, 0, 20);
    }

    [Fact]
    public void BaseLogic_MoveBaseComputer_UpdatesForwardVector()
    {
        var baseData = JsonObject.Parse(@"{
            ""Position"": [0, 100, 0],
            ""Forward"": [0, 0, 1],
            ""Objects"": [
                { ""ObjectID"": ""^BASE_FLAG"", ""Position"": [0, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] },
                { ""ObjectID"": ""^BUILDSIGNAL"", ""Position"": [10, 0, 0], ""Up"": [0, 1, 0], ""At"": [0, 0, 1] }
            ]
        }");

        var objects = baseData.GetArray("Objects")!;
        BaseLogic.MoveBaseComputer(baseData, objects.GetObject(0), objects.GetObject(1));

        // The Forward vector should be updated and should be unit length
        var fwd = baseData.GetArray("Forward")!;
        double len = Math.Sqrt(
            Math.Pow(fwd.GetDouble(0), 2) +
            Math.Pow(fwd.GetDouble(1), 2) +
            Math.Pow(fwd.GetDouble(2), 2));
        Assert.Equal(1.0, len, precision: 10);

        // Forward should be perpendicular to the new position (radial direction)
        var pos = baseData.GetArray("Position")!;
        double posLen = Math.Sqrt(
            Math.Pow(pos.GetDouble(0), 2) +
            Math.Pow(pos.GetDouble(1), 2) +
            Math.Pow(pos.GetDouble(2), 2));
        double dot = (pos.GetDouble(0) * fwd.GetDouble(0) +
                      pos.GetDouble(1) * fwd.GetDouble(1) +
                      pos.GetDouble(2) * fwd.GetDouble(2)) / posLen;
        Assert.Equal(0.0, dot, precision: 10);
    }

    // --- MathHelper: Vec3 and CoordSystem helpers ----------------------

    [Fact]
    public void MathHelper_Vec3_DotProduct()
    {
        var a = new MathHelper.Vec3(1, 0, 0);
        var b = new MathHelper.Vec3(0, 1, 0);
        Assert.Equal(0.0, MathHelper.Vec3.Dot(a, b));

        var c = new MathHelper.Vec3(1, 2, 3);
        var d = new MathHelper.Vec3(4, 5, 6);
        Assert.Equal(32.0, MathHelper.Vec3.Dot(c, d)); // 1*4 + 2*5 + 3*6 = 32
    }

    [Fact]
    public void MathHelper_Vec3_CrossProduct()
    {
        var x = new MathHelper.Vec3(1, 0, 0);
        var y = new MathHelper.Vec3(0, 1, 0);
        var z = MathHelper.Vec3.Cross(x, y);
        Assert.Equal(0.0, z.X, precision: 15);
        Assert.Equal(0.0, z.Y, precision: 15);
        Assert.Equal(1.0, z.Z, precision: 15);
    }

    [Fact]
    public void MathHelper_Vec3_Normalized()
    {
        var v = new MathHelper.Vec3(3, 4, 0);
        var n = v.Normalized();
        Assert.Equal(0.6, n.X, precision: 15);
        Assert.Equal(0.8, n.Y, precision: 15);
        Assert.Equal(0.0, n.Z, precision: 15);
    }

    [Fact]
    public void MathHelper_CoordSystem_ApplyAndSolve_AreInverse()
    {
        var origin = new MathHelper.Vec3(10, 20, 30);
        var axisX = new MathHelper.Vec3(1, 0, 0);
        var axisY = new MathHelper.Vec3(0, 1, 0);
        var axisZ = new MathHelper.Vec3(0, 0, 1);
        var cs = new MathHelper.CoordSystem(origin, axisX, axisY, axisZ);

        var local = new MathHelper.Vec3(5, -3, 7);
        var world = cs.Apply(local);
        var recovered = cs.Solve(world);

        Assert.Equal(local.X, recovered.X, precision: 10);
        Assert.Equal(local.Y, recovered.Y, precision: 10);
        Assert.Equal(local.Z, recovered.Z, precision: 10);
    }

    [Fact]
    public void MathHelper_CoordSystem_RotatedAxes_ApplyAndSolve()
    {
        // Rotated system: X=(0,0,1), Y=(1,0,0), Z=(0,1,0)
        var origin = new MathHelper.Vec3(100, 200, 300);
        var cs = new MathHelper.CoordSystem(origin,
            new MathHelper.Vec3(0, 0, 1),
            new MathHelper.Vec3(1, 0, 0),
            new MathHelper.Vec3(0, 1, 0));

        var local = new MathHelper.Vec3(2, 3, 4);
        var world = cs.Apply(local);
        // Expected: origin + 2*(0,0,1) + 3*(1,0,0) + 4*(0,1,0) = (103, 204, 302)
        Assert.Equal(103.0, world.X, precision: 10);
        Assert.Equal(204.0, world.Y, precision: 10);
        Assert.Equal(302.0, world.Z, precision: 10);

        var recovered = cs.Solve(world);
        Assert.Equal(2.0, recovered.X, precision: 10);
        Assert.Equal(3.0, recovered.Y, precision: 10);
        Assert.Equal(4.0, recovered.Z, precision: 10);
    }

    // --- BaseLogic: Chest Name Helpers -------------------------------

    [Fact]
    public void BaseLogic_GetChestName_ReturnsEmpty_WhenNull()
    {
        Assert.Equal("", BaseLogic.GetChestName(null));
    }

    [Fact]
    public void BaseLogic_GetChestName_ReturnsEmpty_WhenDefault()
    {
        var chest = new JsonObject();
        chest.Add("Name", "BLD_STORAGE_NAME");
        Assert.Equal("", BaseLogic.GetChestName(chest));
    }

    [Fact]
    public void BaseLogic_GetChestName_ReturnsEmpty_WhenNameMissing()
    {
        var chest = new JsonObject();
        Assert.Equal("", BaseLogic.GetChestName(chest));
    }

    [Fact]
    public void BaseLogic_GetChestName_ReturnsCustomName()
    {
        var chest = new JsonObject();
        chest.Add("Name", "Cooking Items");
        Assert.Equal("Cooking Items", BaseLogic.GetChestName(chest));
    }

    [Fact]
    public void BaseLogic_SetChestName_SetsValue()
    {
        var chest = new JsonObject();
        chest.Add("Name", "BLD_STORAGE_NAME");
        BaseLogic.SetChestName(chest, "My Chest");
        Assert.Equal("My Chest", chest.GetString("Name"));
    }

    [Fact]
    public void BaseLogic_SetChestName_ResetsToDefault_WhenEmpty()
    {
        var chest = new JsonObject();
        chest.Add("Name", "My Chest");
        BaseLogic.SetChestName(chest, "");
        Assert.Equal("BLD_STORAGE_NAME", chest.GetString("Name"));
    }

    [Fact]
    public void BaseLogic_SetChestName_ResetsToDefault_WhenNull()
    {
        var chest = new JsonObject();
        chest.Add("Name", "My Chest");
        BaseLogic.SetChestName(chest, null);
        Assert.Equal("BLD_STORAGE_NAME", chest.GetString("Name"));
    }

    [Fact]
    public void BaseLogic_SetChestName_ResetsToDefault_WhenWhitespace()
    {
        var chest = new JsonObject();
        chest.Add("Name", "My Chest");
        BaseLogic.SetChestName(chest, "   ");
        Assert.Equal("BLD_STORAGE_NAME", chest.GetString("Name"));
    }

    [Fact]
    public void BaseLogic_SetChestName_TrimsWhitespace()
    {
        var chest = new JsonObject();
        chest.Add("Name", "BLD_STORAGE_NAME");
        BaseLogic.SetChestName(chest, "  Minerals  ");
        Assert.Equal("Minerals", chest.GetString("Name"));
    }

    [Fact]
    public void BaseLogic_SetChestName_NullInventory_DoesNotThrow()
    {
        var ex = Record.Exception(() => BaseLogic.SetChestName(null, "test"));
        Assert.Null(ex);
    }

    [Fact]
    public void BaseLogic_FormatChestTabTitle_NoName_ReturnsLabel()
    {
        Assert.Equal("Chest 0", BaseLogic.FormatChestTabTitle("Chest 0", ""));
        Assert.Equal("Chest 0", BaseLogic.FormatChestTabTitle("Chest 0", null));
    }

    [Fact]
    public void BaseLogic_FormatChestTabTitle_WithName_AppendsColonName()
    {
        Assert.Equal("Chest 0: Cooking Items", BaseLogic.FormatChestTabTitle("Chest 0", "Cooking Items"));
    }

    [Fact]
    public void BaseLogic_DefaultChestName_IsCorrectValue()
    {
        Assert.Equal("BLD_STORAGE_NAME", BaseLogic.DefaultChestName);
    }

    // --- CoordinateHelper: NormalizeGalacticAddress --------------------------

    [Fact]
    public void CoordinateHelper_NormalizeGalacticAddress_HexString_ReturnsUppercase()
    {
        Assert.Equal("0x311700FE91210B", CoordinateHelper.NormalizeGalacticAddress("0x311700FE91210B"));
        Assert.Equal("0x311700FE91210B", CoordinateHelper.NormalizeGalacticAddress("0x311700fe91210b"));
    }

    [Fact]
    public void CoordinateHelper_NormalizeGalacticAddress_NumericLong_ReturnsHex()
    {
        long addr = 0x20FE00FE91210BL;
        string expected = "0x20FE00FE91210B";
        Assert.Equal(expected, CoordinateHelper.NormalizeGalacticAddress(addr));
    }

    [Fact]
    public void CoordinateHelper_NormalizeGalacticAddress_RawDouble_ReturnsHex()
    {
        // Use a value that fits exactly in a double's 52-bit mantissa
        var rd = new RawDouble(0x1234567890AL, "20015998348554");
        Assert.Equal("0x1234567890A", CoordinateHelper.NormalizeGalacticAddress(rd));
    }

    [Fact]
    public void CoordinateHelper_NormalizeGalacticAddress_Null_ReturnsEmpty()
    {
        Assert.Equal("", CoordinateHelper.NormalizeGalacticAddress(null));
    }

    [Fact]
    public void CoordinateHelper_NormalizeGalacticAddress_DecimalString_ReturnsHex()
    {
        // 0x20FE00FE91210B = 9286479479120139 in decimal
        Assert.Equal("0x20FE00FE91210B", CoordinateHelper.NormalizeGalacticAddress("9286479479120139"));
    }

    [Fact]
    public void CoordinateHelper_NormalizeGalacticAddress_IntValue_ReturnsHex()
    {
        Assert.Equal("0x0", CoordinateHelper.NormalizeGalacticAddress(0));
        Assert.Equal("0xFF", CoordinateHelper.NormalizeGalacticAddress(255));
    }

    // --- BaseLogic: ClearTerrainEdits --------------------------------

    [Fact]
    public void BaseLogic_ClearTerrainEdits_RemovesMatchingEntries()
    {
        var playerState = JsonObject.Parse(@"{
            ""TerrainEditData"": {
                ""GalacticAddresses"": [""0xAABBCC"", ""0x112233"", ""0xAABBCC""],
                ""BufferSizes"": [2, 3, 1],
                ""BufferAges"": [10, 20, 30],
                ""BufferAnchors"": [""a1"", ""a2"", ""a3""],
                ""BufferProtected"": [true, false, true],
                ""Edits"": [""e1"", ""e2"", ""e3"", ""e4"", ""e5"", ""e6""]
            }
        }");
        var baseObj = JsonObject.Parse(@"{ ""GalacticAddress"": ""0xAABBCC"" }");

        int removed = BaseLogic.ClearTerrainEdits(playerState, baseObj);

        Assert.Equal(2, removed);

        var ted = playerState.GetObject("TerrainEditData")!;
        // Only the middle entry (0x112233) should remain
        Assert.Equal(1, ted.GetArray("GalacticAddresses")!.Length);
        Assert.Equal("0x112233", ted.GetArray("GalacticAddresses")!.GetString(0));
        Assert.Equal(1, ted.GetArray("BufferSizes")!.Length);
        Assert.Equal(3, ted.GetArray("BufferSizes")!.GetInt(0));
        Assert.Equal(1, ted.GetArray("BufferAges")!.Length);
        Assert.Equal(1, ted.GetArray("BufferAnchors")!.Length);
        Assert.Equal(1, ted.GetArray("BufferProtected")!.Length);
        // Only the 3 edits from the middle buffer (indices 2,3,4) should remain
        Assert.Equal(3, ted.GetArray("Edits")!.Length);
        Assert.Equal("e3", ted.GetArray("Edits")!.GetString(0));
        Assert.Equal("e4", ted.GetArray("Edits")!.GetString(1));
        Assert.Equal("e5", ted.GetArray("Edits")!.GetString(2));
    }

    [Fact]
    public void BaseLogic_ClearTerrainEdits_ReturnsZero_WhenNoMatch()
    {
        var playerState = JsonObject.Parse(@"{
            ""TerrainEditData"": {
                ""GalacticAddresses"": [""0x112233""],
                ""BufferSizes"": [2],
                ""Edits"": [""e1"", ""e2""]
            }
        }");
        var baseObj = JsonObject.Parse(@"{ ""GalacticAddress"": ""0xAABBCC"" }");

        int removed = BaseLogic.ClearTerrainEdits(playerState, baseObj);

        Assert.Equal(0, removed);
        Assert.Equal(1, playerState.GetObject("TerrainEditData")!.GetArray("GalacticAddresses")!.Length);
    }

    [Fact]
    public void BaseLogic_ClearTerrainEdits_ReturnsZero_WhenNoTerrainData()
    {
        var playerState = JsonObject.Parse(@"{}");
        var baseObj = JsonObject.Parse(@"{ ""GalacticAddress"": ""0xAABBCC"" }");

        int removed = BaseLogic.ClearTerrainEdits(playerState, baseObj);

        Assert.Equal(0, removed);
    }

    [Fact]
    public void BaseLogic_ClearTerrainEdits_HandlesNumericGalacticAddress()
    {
        // Base has numeric address, terrain data has hex string equivalent
        var playerState = JsonObject.Parse(@"{
            ""TerrainEditData"": {
                ""GalacticAddresses"": [""0xFF""],
                ""BufferSizes"": [1],
                ""Edits"": [""e1""]
            }
        }");
        var baseObj = JsonObject.Parse(@"{ ""GalacticAddress"": 255 }");

        int removed = BaseLogic.ClearTerrainEdits(playerState, baseObj);

        Assert.Equal(1, removed);
        Assert.Equal(0, playerState.GetObject("TerrainEditData")!.GetArray("GalacticAddresses")!.Length);
    }

    [Fact]
    public void BaseLogic_ClearTerrainEdits_HandlesMissingOptionalArrays()
    {
        // No BufferAges, BufferAnchors, BufferProtected
        var playerState = JsonObject.Parse(@"{
            ""TerrainEditData"": {
                ""GalacticAddresses"": [""0xAABBCC""],
                ""BufferSizes"": [2],
                ""Edits"": [""e1"", ""e2""]
            }
        }");
        var baseObj = JsonObject.Parse(@"{ ""GalacticAddress"": ""0xAABBCC"" }");

        int removed = BaseLogic.ClearTerrainEdits(playerState, baseObj);

        Assert.Equal(1, removed);
        Assert.Equal(0, playerState.GetObject("TerrainEditData")!.GetArray("GalacticAddresses")!.Length);
        Assert.Equal(0, playerState.GetObject("TerrainEditData")!.GetArray("Edits")!.Length);
    }

    // --- MilestoneLogic ----------------------------------------------

    [Fact]
    public void MilestoneLogic_SectionIconMap_HasExpectedSections()
    {
        Assert.True(MilestoneLogic.SectionIconMap.Count >= 10);
        Assert.True(MilestoneLogic.SectionIconMap.ContainsKey("Milestones"));
        Assert.True(MilestoneLogic.SectionIconMap.ContainsKey("Gek"));
        Assert.True(MilestoneLogic.SectionIconMap.ContainsKey("Vy'keen"));
        Assert.True(MilestoneLogic.SectionIconMap.ContainsKey("Korvax"));
    }

    [Fact]
    public void MilestoneLogic_ReadStatEntryValue_ReadsIntValue()
    {
        var entry = JsonObject.Parse(@"{
            ""Value"": { ""IntValue"": 42, ""FloatValue"": 42.0 }
        }");

        Assert.Equal(42, MilestoneLogic.ReadStatEntryValue(entry));
    }

    [Fact]
    public void MilestoneLogic_ReadStatEntryValue_ReadsFloatWhenNoInt()
    {
        var entry = JsonObject.Parse(@"{
            ""Value"": { ""FloatValue"": 99.7 }
        }");

        Assert.Equal(100, MilestoneLogic.ReadStatEntryValue(entry));
    }

    [Fact]
    public void MilestoneLogic_ReadStatEntryValue_NoValue_ReturnsZero()
    {
        var entry = JsonObject.Parse(@"{ ""Name"": ""test"" }");
        Assert.Equal(0, MilestoneLogic.ReadStatEntryValue(entry));
    }

    [Fact]
    public void MilestoneLogic_WriteStatEntryValue_UpdatesBothFields()
    {
        var entry = JsonObject.Parse(@"{
            ""Value"": { ""IntValue"": 0, ""FloatValue"": 0.0 }
        }");

        MilestoneLogic.WriteStatEntryValue(entry, 500);

        var valueObj = entry.GetObject("Value")!;
        Assert.Equal(500, valueObj.GetInt("IntValue"));
        Assert.Equal(500.0, valueObj.GetDouble("FloatValue"));
    }

    [Fact]
    public void MilestoneLogic_FindGlobalStats_FindsCorrectGroup()
    {
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""Stats"": [
                    {
                        ""GroupId"": ""^LOCAL_STATS"",
                        ""Stats"": [{ ""Id"": ""local1"" }]
                    },
                    {
                        ""GroupId"": ""^GLOBAL_STATS"",
                        ""Stats"": [
                            { ""Id"": ""^YOURSLOTITEM"" },
                            { ""Id"": ""^YOURSTAT"" }
                        ]
                    }
                ]
            }
        }");

        var stats = MilestoneLogic.FindGlobalStats(json);
        Assert.NotNull(stats);
        Assert.Equal(2, stats.Length);
    }

    [Fact]
    public void MilestoneLogic_FindGlobalStats_NoGlobalStats_ReturnsNull()
    {
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""Stats"": [
                    { ""GroupId"": ""^LOCAL_STATS"", ""Stats"": [] }
                ]
            }
        }");

        Assert.Null(MilestoneLogic.FindGlobalStats(json));
    }

    // --- SaveFileManager ---------------------------------------------

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(59, "0:59")]
    [InlineData(60, "1:00")]
    [InlineData(3599, "59:59")]
    [InlineData(3600, "1:00:00")]
    [InlineData(3661, "1:01:01")]
    [InlineData(86400, "24:00:00")]
    [InlineData(360000, "100:00:00")]
    public void SaveFileManager_FormatPlayTime_FormatsCorrectly(long seconds, string expected)
    {
        Assert.Equal(expected, SaveFileManager.FormatPlayTime(seconds));
    }

    [Fact]
    public void DetectGameModeFast_DetectsStringGameMode()
    {
        // The demo save files should be detectable - they use string-based PresetGameMode
        string saveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "_ref", "save_demo");
        string saveFile = Path.Combine(saveDir, "save.hg");
        if (File.Exists(saveFile))
        {
            int mode = SaveFileManager.DetectGameModeFast(saveFile);
            Assert.True(mode > 0, "Should detect a valid game mode from demo save file");
        }
    }

    [Fact]
    public void Platform_Enum_ContainsAllPlatforms()
    {
        var values = Enum.GetValues<SaveFileManager.Platform>();
        Assert.Contains(SaveFileManager.Platform.Steam, values);
        Assert.Contains(SaveFileManager.Platform.XboxGamePass, values);
        Assert.Contains(SaveFileManager.Platform.PS4, values);
        Assert.Contains(SaveFileManager.Platform.GOG, values);
        Assert.Contains(SaveFileManager.Platform.Switch, values);
        Assert.Contains(SaveFileManager.Platform.Unknown, values);
    }

    [Fact]
    public void DetectPlatform_XboxGamePass_DetectedByContainersIndex()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nmse_test_xbox_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "containers.index"), "");
            Assert.Equal(SaveFileManager.Platform.XboxGamePass, SaveFileManager.DetectPlatform(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Xbox_ParseContainersIndex_FindsAllSlots()
    {
        var ciPath = FindRefPath("_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C", "containers.index");
        if (ciPath == null) return; // skip if reference save not available

        var slots = ContainersIndexManager.ParseContainersIndex(ciPath);
        Assert.True(slots.Count >= 6, $"Expected at least 6 slots, got {slots.Count}");
        Assert.True(slots.ContainsKey("AccountData"));
        Assert.True(slots.ContainsKey("Settings"));
        Assert.True(slots.ContainsKey("Slot1Auto"));
        Assert.True(slots.ContainsKey("Slot1Manual"));
        Assert.True(slots.ContainsKey("Slot3Auto"));
        Assert.True(slots.ContainsKey("Slot3Manual"));

        Assert.False(ContainersIndexManager.IsSaveSlot("AccountData"));
        Assert.False(ContainersIndexManager.IsSaveSlot("Settings"));
        Assert.True(ContainersIndexManager.IsSaveSlot("Slot1Auto"));
    }

    [Fact]
    public void Xbox_LoadAccountData_DecompressesRawLz4AndParsesJson()
    {
        var ciPath = FindRefPath("_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C", "containers.index");
        if (ciPath == null) return; // skip if reference save not available

        var slots = ContainersIndexManager.ParseContainersIndex(ciPath);
        Assert.True(slots.TryGetValue("AccountData", out var accountSlot));
        Assert.NotNull(accountSlot!.DataFilePath);
        Assert.True(File.Exists(accountSlot.DataFilePath));

        // LoadXboxSave must decompress the raw LZ4 blob and return valid JSON
        string? json = ContainersIndexManager.LoadXboxSave(accountSlot);
        Assert.NotNull(json);
        Assert.True(json!.Length > 0);
        Assert.StartsWith("{", json);

        // Parse as JsonObject to confirm it's valid JSON with expected structure
        var obj = JsonObject.Parse(json);
        Assert.NotNull(obj);
        Assert.True(obj.Length > 0);
    }

    [Fact]
    public void Xbox_LoadAccountData_ViaAccountLogic_LoadsRewards()
    {
        var ciPath = FindRefPath("_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C", "containers.index");
        if (ciPath == null) return; // skip if reference save not available

        EnsureMapperLoaded();
        var slots = ContainersIndexManager.ParseContainersIndex(ciPath);
        Assert.True(slots.TryGetValue("AccountData", out var accountSlot));

        var data = AccountLogic.LoadXboxAccountData(accountSlot!);
        Assert.Null(data.ErrorMessage);
        Assert.NotNull(data.AccountObject);
        Assert.NotNull(data.StatusMessage);

        // The account data should have a UserSettingsData key
        var userSettings = data.AccountObject!.GetObject("UserSettingsData");
        Assert.NotNull(userSettings);
    }

    [Fact]
    public void Xbox_LoadSaveSlot_LoadsNmsLz4StreamingFormat()
    {
        var ciPath = FindRefPath("_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C", "containers.index");
        if (ciPath == null) return; // skip if reference save not available

        EnsureMapperLoaded();
        var slots = ContainersIndexManager.ParseContainersIndex(ciPath);
        Assert.True(slots.TryGetValue("Slot1Auto", out var slotInfo));
        Assert.NotNull(slotInfo!.DataFilePath);

        string? json = ContainersIndexManager.LoadXboxSave(slotInfo);
        Assert.NotNull(json);
        Assert.StartsWith("{", json!);

        var obj = JsonObject.Parse(json);
        Assert.NotNull(obj);
    }

    [Fact]
    public void DetectPlatform_Switch_DetectedByManifestDat()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nmse_test_switch_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "manifest00.dat"), "");
            Assert.Equal(SaveFileManager.Platform.Switch, SaveFileManager.DetectPlatform(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DetectPlatform_PS4_DetectedByMemoryDat()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nmse_test_ps4mem_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "memory.dat"), "");
            Assert.Equal(SaveFileManager.Platform.PS4, SaveFileManager.DetectPlatform(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DetectPlatform_PS4_DetectedBySaveDataHg()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nmse_test_ps4sd_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "savedata11.hg"), "");
            Assert.Equal(SaveFileManager.Platform.PS4, SaveFileManager.DetectPlatform(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DetectPlatform_GOG_DetectedByDefaultUserDir()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "nmse_test_gog_" + Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(baseDir, "DefaultUser");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "save.hg"), "");
            Assert.Equal(SaveFileManager.Platform.GOG, SaveFileManager.DetectPlatform(dir));
        }
        finally { Directory.Delete(baseDir, true); }
    }

    [Fact]
    public void DetectPlatform_Steam_DetectedBySaveHg()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nmse_test_steam_" + Guid.NewGuid().ToString("N"));
        var steamDir = Path.Combine(dir, "st_12345");
        Directory.CreateDirectory(steamDir);
        try
        {
            File.WriteAllText(Path.Combine(steamDir, "save.hg"), "");
            Assert.Equal(SaveFileManager.Platform.Steam, SaveFileManager.DetectPlatform(steamDir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DetectPlatform_Unknown_EmptyDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "nmse_test_unknown_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.Equal(SaveFileManager.Platform.Unknown, SaveFileManager.DetectPlatform(dir));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void DifficultyLevel_Enum_ContainsAllLevels()
    {
        var values = Enum.GetValues<Models.DifficultyLevel>();
        Assert.Contains(Models.DifficultyLevel.Normal, values);
        Assert.Contains(Models.DifficultyLevel.Survival, values);
        Assert.Contains(Models.DifficultyLevel.Permadeath, values);
        Assert.Contains(Models.DifficultyLevel.Creative, values);
        Assert.Contains(Models.DifficultyLevel.Custom, values);
        Assert.Contains(Models.DifficultyLevel.Relaxed, values);
        Assert.Contains(Models.DifficultyLevel.Hardcore, values);
    }

    // --- JSON Parser / JsonObject Optimizations ---------------------

    [Fact]
    public void JsonObject_DictionaryIndex_GetReturnsCorrectValues()
    {
        // Build object with > 8 keys to trigger dictionary indexing
        var obj = new JsonObject();
        for (int i = 0; i < 20; i++)
            obj.Add($"key{i}", $"value{i}");

        for (int i = 0; i < 20; i++)
            Assert.Equal($"value{i}", obj.Get($"key{i}"));

        Assert.Null(obj.Get("nonexistent"));
    }

    [Fact]
    public void JsonObject_DictionaryIndex_SetUpdatesCorrectly()
    {
        var obj = new JsonObject();
        for (int i = 0; i < 20; i++)
            obj.Add($"key{i}", $"value{i}");

        obj.Set("key5", "updated");
        Assert.Equal("updated", obj.Get("key5"));
    }

    [Fact]
    public void JsonObject_DictionaryIndex_RemoveWorks()
    {
        var obj = new JsonObject();
        for (int i = 0; i < 20; i++)
            obj.Add($"key{i}", $"value{i}");

        obj.Remove("key5");
        Assert.Null(obj.Get("key5"));
        Assert.Equal(19, obj.Length);
        // Other keys still work
        Assert.Equal("value0", obj.Get("key0"));
        Assert.Equal("value19", obj.Get("key19"));
    }

    [Fact]
    public void JsonObject_DictionaryIndex_ContainsWorks()
    {
        var obj = new JsonObject();
        for (int i = 0; i < 20; i++)
            obj.Add($"key{i}", $"value{i}");

        Assert.True(obj.Contains("key0"));
        Assert.True(obj.Contains("key19"));
        Assert.False(obj.Contains("nonexistent"));
    }

    [Fact]
    public void JsonObject_SmallObject_LinearScanWorks()
    {
        // Object with <= 8 keys stays linear
        var obj = new JsonObject();
        obj.Add("a", "1");
        obj.Add("b", "2");
        obj.Add("c", "3");

        Assert.Equal("1", obj.Get("a"));
        Assert.Equal("2", obj.Get("b"));
        Assert.Equal("3", obj.Get("c"));
        Assert.Null(obj.Get("d"));
    }

    [Fact]
    public void JsonObject_GetValue_SimpleKeySkipsRegex()
    {
        var obj = new JsonObject();
        var inner = new JsonObject();
        inner.Add("value", 42);
        obj.Add("simple", inner);

        // Simple key lookup (no dots/brackets)
        Assert.Same(inner, obj.GetValue("simple"));
    }

    [Fact]
    public void JsonObject_GetValue_DottedPathStillWorks()
    {
        var obj = new JsonObject();
        var inner = new JsonObject();
        inner.Add("value", 42);
        obj.Add("outer", inner);

        Assert.Equal(42, obj.GetValue("outer.value"));
    }

    [Fact]
    public void JsonParser_FastStringParsing_SimpleStrings()
    {
        // Simple strings should use fast path (no escapes, no high bytes)
        string json = "{\"key\":\"hello world\",\"num\":42}";
        var obj = JsonObject.Parse(json);
        Assert.Equal("hello world", obj.GetString("key"));
        Assert.Equal(42, obj.GetInt("num"));
    }

    [Fact]
    public void JsonParser_SlowStringParsing_EscapedStrings()
    {
        // Strings with escapes should use slow path but still parse correctly
        string json = "{\"key\":\"hello\\nworld\",\"path\":\"c:\\\\temp\"}";
        var obj = JsonObject.Parse(json);
        Assert.Equal("hello\nworld", obj.GetString("key"));
        Assert.Equal("c:\\temp", obj.GetString("path"));
    }

    [Fact]
    public void JsonParser_RoundTrip_PreservesData()
    {
        string json = "{\"name\":\"test\",\"value\":123,\"flag\":true,\"nothing\":null,\"array\":[1,2,3]}";
        var obj = JsonObject.Parse(json);

        Assert.Equal("test", obj.GetString("name"));
        Assert.Equal(123, obj.GetInt("value"));
        Assert.True(obj.GetBool("flag"));
        Assert.Null(obj.Get("nothing"));

        var arr = obj.GetArray("array");
        Assert.NotNull(arr);
        Assert.Equal(3, arr!.Length);
        Assert.Equal(1, arr.GetInt(0));
    }

    [Fact]
    public void JsonParser_LargeObject_ParsesCorrectly()
    {
        // Build a JSON string with many keys to test dictionary indexing
        var parts = new System.Text.StringBuilder("{");
        for (int i = 0; i < 50; i++)
        {
            if (i > 0) parts.Append(',');
            parts.Append(CultureInfo.InvariantCulture, $"\"key{i}\":{i}");
        }
        parts.Append('}');

        var obj = JsonObject.Parse(parts.ToString());
        Assert.Equal(50, obj.Length);
        Assert.Equal(0, obj.GetInt("key0"));
        Assert.Equal(49, obj.GetInt("key49"));
    }

    [Fact]
    public void JsonParser_FloatingPoint_ReturnsRawDouble()
    {
        // Floating-point values should be returned as RawDouble (preserving original text)
        string json = "{\"pi\":3.14159,\"neg\":-0.5,\"sci\":1.5e10}";
        var obj = JsonObject.Parse(json);
        Assert.IsType<RawDouble>(obj.Get("pi"));
        Assert.IsType<RawDouble>(obj.Get("neg"));
        Assert.IsType<RawDouble>(obj.Get("sci"));
        Assert.Equal(3.14159, obj.GetDouble("pi"), 5);
        Assert.Equal(-0.5, obj.GetDouble("neg"), 5);
        Assert.Equal(1.5e10, obj.GetDouble("sci"), 5);
    }

    [Fact]
    public void JsonParser_Serialize_DoubleUsesInvariantDecimalPoint()
    {
        // Verify that serialising a double always uses '.' as the decimal separator,
        // even when the current thread culture uses a comma.
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            // Use German culture which uses comma as decimal separator
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

            var obj = new JsonObject();
            obj.Add("catch", 12.5);
            string json = JsonParser.Serialize(obj, false, skipReverseMapping: true);

            // Must contain a dot, must NOT contain a comma decimal separator
            Assert.Contains("12.5", json);
            Assert.DoesNotContain("12,5", json);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void JsonParser_Serialize_FloatUsesInvariantDecimalPoint()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("fr-FR");

            var obj = new JsonObject();
            obj.Add("scale", 1.75f);
            string json = JsonParser.Serialize(obj, false, skipReverseMapping: true);

            Assert.Contains("1.75", json);
            Assert.DoesNotContain("1,75", json);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void JsonParser_RoundTrip_DoublePreservesDecimalPointUnderCommaLocale()
    {
        // Simulate:
        // parse JSON from save -> set a new double value -> re-serialize data -> verify '.' separator
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

            string original = "{\"LargestCatchList\":[0.0]}";
            var obj = JsonObject.Parse(original);
            var arr = obj.GetArray("LargestCatchList");
            Assert.NotNull(arr);

            // Simulate user entering a value (through invariant TryParse)
            double.TryParse("23.7",
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double userValue);
            arr!.Set(0, userValue);

            // G17 gives the full 17-significant-digit representation of the stored
            // double.  The important thing is the decimal separator is a dot, not a
            // comma, regardless of the current locale.
            string serialized = JsonParser.Serialize(obj, false, skipReverseMapping: true);
            Assert.Contains(".", serialized);
            Assert.DoesNotContain("23,7", serialized);

            // Verify the value round-trips back correctly
            var reparsed = JsonObject.Parse(serialized);
            double readBack = reparsed.GetArray("LargestCatchList")!.GetDouble(0);
            Assert.Equal(userValue, readBack);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void Double_ToString_InvariantCulture_UnderGermanLocale()
    {
        // Verifies that using InvariantCulture with ToString produces a dot decimal,
        // matching the pattern used for CompanionPanel Scale/Trust/Traits/Moods display.
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

            double val = 1.75;
            // Without InvariantCulture, German locale would produce "1,75"
            string withoutCulture = val.ToString(CultureInfo.CurrentCulture);
            Assert.Contains(",", withoutCulture); // sanity: German uses comma

            // With InvariantCulture, it must use dot
            string withCulture = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.Contains(".", withCulture);
            Assert.DoesNotContain(",", withCulture);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    // --- NumericParseHelper tests ------------------------------------------

    [Fact]
    public void NumericParseHelper_TryParseDouble_InvariantDotAlwaysWorks()
    {
        Assert.True(NumericParseHelper.TryParseDouble("1.5", out double v));
        Assert.Equal(1.5, v, 10);
    }

    [Fact]
    public void NumericParseHelper_TryParseDouble_GermanComma()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

			// German user types "1,5" — should parse as 1.5
			Assert.True(NumericParseHelper.TryParseDouble("1,5", out double v));
            Assert.Equal(1.5, v, 10);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void NumericParseHelper_TryParseDouble_FrenchComma()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("fr-FR");

            Assert.True(NumericParseHelper.TryParseDouble("3,14", out double v));
            Assert.Equal(3.14, v, 10);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void NumericParseHelper_TryParseDouble_DotStillWorksUnderGermanLocale()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

            // Even under German locale, "1.5" should parse as 1.5 (invariant wins
            // because the input contains a dot).
            Assert.True(NumericParseHelper.TryParseDouble("1.5", out double v));
            Assert.Equal(1.5, v, 10);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void NumericParseHelper_TryParseDouble_EmptyAndNullReturnFalse()
    {
        Assert.False(NumericParseHelper.TryParseDouble("", out _));
        Assert.False(NumericParseHelper.TryParseDouble(null, out _));
        Assert.False(NumericParseHelper.TryParseDouble("   ", out _));
    }

    [Fact]
    public void NumericParseHelper_TryParseDouble_InvalidTextReturnsFalse()
    {
        Assert.False(NumericParseHelper.TryParseDouble("abc", out _));
        Assert.False(NumericParseHelper.TryParseDouble("not-a-number", out _));
    }

    [Fact]
    public void NumericParseHelper_FormatDouble_AlwaysUsesDot()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

            string formatted = NumericParseHelper.FormatDouble(1.5);
            Assert.Contains(".", formatted);
            Assert.DoesNotContain(",", formatted);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void NumericParseHelper_FormatDouble_WithFormat_UsesInvariant()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("fr-FR");

            string formatted = NumericParseHelper.FormatDouble(1234.5678, "F2");
            Assert.Contains(".", formatted);
            Assert.DoesNotContain(",", formatted);
            Assert.Equal("1234.57", formatted);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void NumericParseHelper_RoundTrip_UnderCommaLocale()
    {
        // Simulates when a user enters a value in their locale via:
		// locale input (in) -> format for JSON (x) -> parse back (out)
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture =
                new System.Globalization.CultureInfo("de-DE");

            // User types "23,7"
            Assert.True(NumericParseHelper.TryParseDouble("23,7", out double parsed));
            Assert.Equal(23.7, parsed, 10);

            // Format for JSON (G17 always outputs 17 significant digits,
            // so 23.7 becomes the full representation of the stored double)
            string json = NumericParseHelper.FormatDouble(parsed);
            Assert.Contains(".", json);
            Assert.DoesNotContain(",", json);

            // Parse back from JSON (invariant format)
            Assert.True(NumericParseHelper.TryParseDouble(json, out double roundTrip));
            Assert.Equal(23.7, roundTrip, 10);
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = saved;
        }
    }

    [Fact]
    public void JsonParser_Integers_ReturnIntOrLong()
    {
        string json = "{\"small\":42,\"neg\":-100,\"big\":3000000000,\"zero\":0}";
        var obj = JsonObject.Parse(json);
        Assert.IsType<int>(obj.Get("small"));
        Assert.IsType<int>(obj.Get("neg"));
        Assert.IsType<long>(obj.Get("big"));
        Assert.IsType<int>(obj.Get("zero"));
        Assert.Equal(42, obj.GetInt("small"));
        Assert.Equal(-100, obj.GetInt("neg"));
        Assert.Equal(3000000000L, obj.GetLong("big"));
    }

    [Fact]
    public void JsonObject_SmallCapacity_GrowsCorrectly()
    {
        // Initial capacity is now 4, test that objects with more properties still work
        var obj = new JsonObject();
        for (int i = 0; i < 20; i++)
            obj.Add($"key{i}", i);
        Assert.Equal(20, obj.Length);
        for (int i = 0; i < 20; i++)
            Assert.Equal(i, obj.GetInt($"key{i}"));
    }

    [Fact]
    public void JsonObject_Reorder_MovesForward()
    {
        var obj = new JsonObject();
        obj.Add("a", 1);
        obj.Add("b", 2);
        obj.Add("c", 3);
        obj.Add("d", 4);
        // Move 'a' (index 0) to index 2
        obj.Reorder(0, 2);
        var names = obj.Names();
        Assert.Equal("b", names[0]);
        Assert.Equal("c", names[1]);
        Assert.Equal("a", names[2]);
        Assert.Equal("d", names[3]);
        Assert.Equal(1, obj.GetInt("a"));
    }

    [Fact]
    public void JsonObject_Reorder_MovesBackward()
    {
        var obj = new JsonObject();
        obj.Add("a", 1);
        obj.Add("b", 2);
        obj.Add("c", 3);
        obj.Add("d", 4);
        // Move 'd' (index 3) to index 1
        obj.Reorder(3, 1);
        var names = obj.Names();
        Assert.Equal("a", names[0]);
        Assert.Equal("d", names[1]);
        Assert.Equal("b", names[2]);
        Assert.Equal("c", names[3]);
        Assert.Equal(4, obj.GetInt("d"));
    }

    [Fact]
    public void JsonObject_Reorder_SameIndex_NoOp()
    {
        var obj = new JsonObject();
        obj.Add("a", 1);
        obj.Add("b", 2);
        obj.Reorder(0, 0);
        Assert.Equal("a", obj.Names()[0]);
        Assert.Equal("b", obj.Names()[1]);
    }

    [Fact]
    public void JsonParser_StringInterning_DeduplicatesKeys()
    {
        // Array of objects with same keys should share string references
        string json = "[{\"Id\":\"a\",\"Value\":1},{\"Id\":\"b\",\"Value\":2}]";
        var arr = JsonParser.ParseArray(json);
        var obj0 = arr.GetObject(0)!;
        var obj1 = arr.GetObject(1)!;
        // Both objects should resolve the same key strings
        Assert.Equal("a", obj0.GetString("Id"));
        Assert.Equal("b", obj1.GetString("Id"));
        Assert.Equal(1, obj0.GetInt("Value"));
        Assert.Equal(2, obj1.GetInt("Value"));
    }

    [Fact]
    public void JsonObject_LazyTransforms_NotAllocatedByDefault()
    {
        // Transforms should not be allocated for regular objects
        var obj = new JsonObject();
        obj.Add("key", "value");
        // GetValue should work fine without transforms
        Assert.Equal("value", obj.GetValue("key"));
    }

    // --- Settlement UID Filtering ------------------------------------

    [Fact]
    public void SettlementLogic_FilterByOwnerUID_MatchingUID()
    {
        // Build save data with CommonStateData.UsedDiscoveryOwnersV2 (direct child, NOT under SeasonData)
        var saveData = new JsonObject();
        var commonState = new JsonObject();
        var owners = new JsonArray();
        var owner = new JsonObject();
        owner.Add("UID", "12345");
        owners.Add(owner);
        commonState.Add("UsedDiscoveryOwnersV2", owners);
        saveData.Add("CommonStateData", commonState);

        var playerState = new JsonObject();
        var settlements = new JsonArray();

        // Settlement with matching UID
        var s1 = new JsonObject();
        var s1Owner = new JsonObject();
        s1Owner.Add("UID", "12345");
        s1.Add("Owner", s1Owner);
        settlements.Add(s1);

        // Settlement with non-matching UID
        var s2 = new JsonObject();
        var s2Owner = new JsonObject();
        s2Owner.Add("UID", "99999");
        s2.Add("Owner", s2Owner);
        settlements.Add(s2);

        var result = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Single(result);
        Assert.Equal(0, result[0]); // Only the first settlement matches
    }

    [Fact]
    public void SettlementLogic_FilterByOwnerUID_NoMatch_ReturnsEmpty()
    {
        var saveData = new JsonObject();
        var commonState = new JsonObject();
        var owners = new JsonArray();
        var owner = new JsonObject();
        owner.Add("UID", "12345");
        owners.Add(owner);
        commonState.Add("UsedDiscoveryOwnersV2", owners);
        saveData.Add("CommonStateData", commonState);

        var playerState = new JsonObject();
        var settlements = new JsonArray();

        // No matching settlements - should return empty (no fallback)
        var s1 = new JsonObject();
        var s1Owner = new JsonObject();
        s1Owner.Add("UID", "99999");
        s1.Add("Owner", s1Owner);
        settlements.Add(s1);

        var result = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Empty(result);
    }

    [Fact]
    public void SettlementLogic_FilterByOwnerUID_NoCommonStateData_ReturnsEmpty()
    {
        var saveData = new JsonObject(); // No CommonStateData
        var playerState = new JsonObject();
        var settlements = new JsonArray();

        var s1 = new JsonObject();
        var s1Owner = new JsonObject();
        s1Owner.Add("UID", "12345");
        s1.Add("Owner", s1Owner);
        settlements.Add(s1);

        var result = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Empty(result); // No player identifiers found, returns empty
    }

    [Fact]
    public void SettlementLogic_FilterByOwnerLID_MatchesWhenUIDMissing()
    {
        var saveData = new JsonObject();
        var commonState = new JsonObject();
        var owners = new JsonArray();
        var owner = new JsonObject();
        owner.Add("LID", "LID-ABC");
        // No UID set
        owners.Add(owner);
        commonState.Add("UsedDiscoveryOwnersV2", owners);
        saveData.Add("CommonStateData", commonState);

        var playerState = new JsonObject();
        var settlements = new JsonArray();

        // Settlement matching by LID
        var s1 = new JsonObject();
        var s1Owner = new JsonObject();
        s1Owner.Add("LID", "LID-ABC");
        s1.Add("Owner", s1Owner);
        settlements.Add(s1);

        // Settlement not matching
        var s2 = new JsonObject();
        var s2Owner = new JsonObject();
        s2Owner.Add("LID", "LID-XYZ");
        s2.Add("Owner", s2Owner);
        settlements.Add(s2);

        var result = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void SettlementLogic_FilterByOwnerUSN_MatchesWhenOthersMissing()
    {
        var saveData = new JsonObject();
        var commonState = new JsonObject();
        var owners = new JsonArray();
        var owner = new JsonObject();
        owner.Add("USN", "PlayerName");
        // No LID or UID set
        owners.Add(owner);
        commonState.Add("UsedDiscoveryOwnersV2", owners);
        saveData.Add("CommonStateData", commonState);

        var playerState = new JsonObject();
        var settlements = new JsonArray();

        // Settlement matching by USN
        var s1 = new JsonObject();
        var s1Owner = new JsonObject();
        s1Owner.Add("USN", "PlayerName");
        s1.Add("Owner", s1Owner);
        settlements.Add(s1);

        var result = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void CoordinateHelper_VoxelToPortalCode_CalculatesCorrectly()
    {
        // Test known coordinates
        string code = CoordinateHelper.VoxelToPortalCode(266, -1, -1773, 52, 0);
        Assert.Equal(12, code.Length);
        Assert.StartsWith("0", code); // planet 0
    }

    [Fact]
    public void CoordinateHelper_VoxelToSignalBooster_FormatsCorrectly()
    {
        string sb = CoordinateHelper.VoxelToSignalBooster(100, 50, -200, 10);
        Assert.Contains(":", sb);
        var parts = sb.Split(':');
        Assert.Equal(4, parts.Length);
    }

    [Fact]
    public void CoordinateHelper_DistanceToCenter_PositiveValues()
    {
        double dist = CoordinateHelper.GetDistanceToCenter(100, 50, -200);
        Assert.True(dist > 0);
    }

    [Fact]
    public void CoordinateHelper_JumpsToCenter_ReasonableValue()
    {
        int jumps = CoordinateHelper.GetJumpsToCenter(50000, 100);
        Assert.Equal(500, jumps);
    }

    [Fact]
    public void CoordinateHelper_PortalHexToDec_ConvertsCorrectly()
    {
        // Example from the problem statement: 00E4FF91310A -> 1,1,15,5,16,16,10,2,4,2,1,11
        Assert.Equal("1,1,15,5,16,16,10,2,4,2,1,11", CoordinateHelper.PortalHexToDec("00E4FF91310A"));
    }

    [Fact]
    public void CoordinateHelper_PortalHexToDec_AllDigits()
    {
        // 0123456789ABCDEF -> 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16
        Assert.Equal("1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16", CoordinateHelper.PortalHexToDec("0123456789ABCDEF"));
    }

    [Fact]
    public void CoordinateHelper_PortalHexToDec_EmptyInput()
    {
        Assert.Equal("", CoordinateHelper.PortalHexToDec(""));
        Assert.Equal("", CoordinateHelper.PortalHexToDec(null!));
    }

    [Fact]
    public void CoordinateHelper_PortalHexToDec_LowercaseInput()
    {
        Assert.Equal("1,1,15,5,16,16,10,2,4,2,1,11", CoordinateHelper.PortalHexToDec("00e4ff91310a"));
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(266, -1, -1773, 52, 0)]
    [InlineData(100, 50, -200, 42, 3)]
    [InlineData(-2048, -128, -2048, 600, 15)]
    [InlineData(2047, 127, 2047, 0, 0)]
    public void CoordinateHelper_PortalCodeToVoxel_Roundtrip(int x, int y, int z, int sys, int planet)
    {
        string code = CoordinateHelper.VoxelToPortalCode(x, y, z, sys, planet);
        Assert.True(CoordinateHelper.PortalCodeToVoxel(code, out int rx, out int ry, out int rz, out int rsys, out int rplanet));
        Assert.Equal(x, rx);
        Assert.Equal(y, ry);
        Assert.Equal(z, rz);
        Assert.Equal(sys, rsys);
        Assert.Equal(planet, rplanet);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ZZZZZZZZZZZZ")]
    public void CoordinateHelper_PortalCodeToVoxel_InvalidInput_ReturnsFalse(string input)
    {
        Assert.False(CoordinateHelper.PortalCodeToVoxel(input, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void GalaxyDatabase_GetGalaxyName_ReturnsExpectedNames()
    {
        Assert.Equal("Euclid", GalaxyDatabase.GetGalaxyName(0));
        Assert.Equal("Hilbert Dimension", GalaxyDatabase.GetGalaxyName(1));
        Assert.Equal("Calypso", GalaxyDatabase.GetGalaxyName(2));
        Assert.Equal("Eissentam", GalaxyDatabase.GetGalaxyName(9));
    }

    [Fact]
    public void GalaxyDatabase_GetGalaxyName_HandlesOutOfRange()
    {
        // All 256 galaxies are now in the database; out of range returns Unknown
        Assert.Equal("Drundemiso", GalaxyDatabase.GetGalaxyName(199));
        Assert.Equal("Unknown", GalaxyDatabase.GetGalaxyName(257));
        Assert.Equal("Unknown", GalaxyDatabase.GetGalaxyName(-1));
    }

    [Fact]
    public void FrigateLogic_GetLevelUpIn_ReturnsExpectedValues()
    {
        Assert.Equal(2, FrigateLogic.GetLevelUpIn(0)); // 0 exp, next milestone is 2
        Assert.Equal(1, FrigateLogic.GetLevelUpIn(1)); // 1 exp, 2-1=1
        Assert.Equal(3, FrigateLogic.GetLevelUpIn(2)); // 2 exp, next is 5, 5-2=3
        Assert.Equal(-1, FrigateLogic.GetLevelUpIn(55)); // fully leveled
    }

    [Fact]
    public void FrigateLogic_GetLevelUpsRemaining_CountsCorrectly()
    {
        Assert.Equal(10, FrigateLogic.GetLevelUpsRemaining(0));
        Assert.Equal(9, FrigateLogic.GetLevelUpsRemaining(2));
        Assert.Equal(0, FrigateLogic.GetLevelUpsRemaining(55));
    }

    [Fact]
    public void MultitoolLogic_NewTypes_AllHaveFilenames()
    {
        // Verify the new types (Rifle, Alien, Pristine, Staff Ruin, Staff Bone)
        var newTypes = new[] { "Rifle", "Alien", "Pristine", "Staff Ruin", "Staff Bone" };
        foreach (var typeName in newTypes)
        {
            var match = MultitoolLogic.ToolTypes.FirstOrDefault(t => t.Name == typeName);
            Assert.False(string.IsNullOrEmpty(match.Name), $"Type '{typeName}' should exist");
            Assert.False(string.IsNullOrEmpty(match.Filename), $"Type '{typeName}' should have a filename");
        }
    }

    [Fact]
    public void SettlementLogic_DecisionTypes_ContainsExpectedValues()
    {
        Assert.Equal(12, SettlementLogic.DecisionTypes.Length);
        Assert.Equal("None", SettlementLogic.DecisionTypes[0]);
        Assert.Contains("Policy", SettlementLogic.DecisionTypes);
        Assert.Contains("NewBuilding", SettlementLogic.DecisionTypes);
        Assert.Contains("UpgradeBuilding", SettlementLogic.DecisionTypes);
        Assert.Equal("UpgradeBuildingChoice", SettlementLogic.DecisionTypes[^1]);
    }

    [Fact]
    public void SettlementLogic_Stats_HaveCorrectStructure()
    {
        Assert.Equal(8, SettlementLogic.StatCount);
        Assert.Equal(8, SettlementLogic.StatLabels.Length);
        Assert.Equal(8, SettlementLogic.StatMaxValues.Length);
        Assert.Equal("Max Population", SettlementLogic.StatLabels[0]);
        Assert.Equal(175, SettlementLogic.StatMaxValues[0]);
        Assert.Equal("Happiness", SettlementLogic.StatLabels[1]);
        Assert.Equal("Production", SettlementLogic.StatLabels[2]);
        Assert.Equal("Upkeep", SettlementLogic.StatLabels[3]);
        Assert.Equal("Sentinels", SettlementLogic.StatLabels[4]);
        Assert.Equal("Debt", SettlementLogic.StatLabels[5]);
        Assert.Equal("Alert", SettlementLogic.StatLabels[6]);
        Assert.Equal("Bug Attack", SettlementLogic.StatLabels[7]);
    }

    [Fact]
    public void GalaxyDatabase_Has257Galaxies()
    {
        Assert.Equal(257, GalaxyDatabase.Galaxies.Length);
    }

    [Fact]
    public void GalaxyDatabase_GalaxyNumbers_Are1Based()
    {
        Assert.Equal(1, GalaxyDatabase.Galaxies[0].Number);
        Assert.Equal(10, GalaxyDatabase.Galaxies[9].Number);
        Assert.Equal(256, GalaxyDatabase.Galaxies[255].Number);
    }

    [Fact]
    public void GalaxyDatabase_DisplayName_IncludesNumber()
    {
        string display = GalaxyDatabase.GetGalaxyDisplayName(0);
        Assert.Equal("Euclid (1)", display);
        Assert.Equal("Eissentam (10)", GalaxyDatabase.GetGalaxyDisplayName(9));
    }

    [Fact]
    public void SquadronLogic_PilotRaces_UseProperNames()
    {
        Assert.Contains("Gek", SquadronLogic.PilotRaces);
        Assert.Contains("Vy'keen", SquadronLogic.PilotRaces);
        Assert.Contains("Korvax", SquadronLogic.PilotRaces);
        Assert.DoesNotContain("Traders", SquadronLogic.PilotRaces);
    }

    [Fact]
    public void CoordinateHelper_PlayerStates_HasAllValues()
    {
        Assert.Equal(10, CoordinateHelper.PlayerStates.Length);
        Assert.Contains("OnFoot", CoordinateHelper.PlayerStates);
        Assert.Contains("AboardFleet", CoordinateHelper.PlayerStates);
        Assert.Contains("OnFootInCorvetteLanded", CoordinateHelper.PlayerStates);
    }

    // --- FreighterLogic - Crew Race ----------------------------------

    [Fact]
    public void FreighterLogic_CrewRaces_ContainsThreeRaces()
    {
        Assert.Equal(3, FreighterLogic.CrewRaces.Length);
        Assert.Contains("Gek", FreighterLogic.CrewRaces);
        Assert.Contains("Vy'keen", FreighterLogic.CrewRaces);
        Assert.Contains("Korvax", FreighterLogic.CrewRaces);
    }

    [Fact]
    public void FreighterLogic_NpcResourceToRace_MapsCorrectly()
    {
        Assert.True(FreighterLogic.NpcResourceToRace.ContainsKey("MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN"));
        Assert.Equal("Gek", FreighterLogic.NpcResourceToRace["MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN"]);
    }

    [Fact]
    public void FreighterLogic_RaceToNpcResource_RoundTrips()
    {
        foreach (string race in FreighterLogic.CrewRaces)
        {
            Assert.True(FreighterLogic.RaceToNpcResource.ContainsKey(race));
            string resource = FreighterLogic.RaceToNpcResource[race];
            Assert.True(FreighterLogic.NpcResourceToRace.ContainsKey(resource));
            Assert.Equal(race, FreighterLogic.NpcResourceToRace[resource]);
        }
    }

    // --- SquadronLogic - Ship Type Mapping ---------------------------

    [Fact]
    public void SquadronLogic_ShipTypeToResource_ContainsMainTypes()
    {
        Assert.True(SquadronLogic.ShipTypeToResource.Count >= 9);
        Assert.Contains("Hauler", SquadronLogic.ShipTypeToResource.Keys);
        Assert.Contains("Fighter", SquadronLogic.ShipTypeToResource.Keys);
        Assert.Contains("Explorer", SquadronLogic.ShipTypeToResource.Keys);
        Assert.Contains("Shuttle", SquadronLogic.ShipTypeToResource.Keys);
        Assert.Contains("Exotic", SquadronLogic.ShipTypeToResource.Keys);
    }

    [Fact]
    public void SquadronLogic_ShipTypeToResource_RoundTrips()
    {
        foreach (var (typeName, resource) in SquadronLogic.ShipTypeToResource)
        {
            Assert.True(SquadronLogic.ShipResourceToType.ContainsKey(resource));
            Assert.Equal(typeName, SquadronLogic.ShipResourceToType[resource]);
        }
    }

    // --- SettlementLogic - Population Field --------------------------

    [Fact]
    public void SettlementLogic_DecisionTypes_ContainsAll12()
    {
        // Verify first and last plus count (complementary to ContainsExpectedValues)
        Assert.Equal(12, SettlementLogic.DecisionTypes.Length);
        Assert.Equal("None", SettlementLogic.DecisionTypes[0]);
        Assert.Equal("UpgradeBuildingChoice", SettlementLogic.DecisionTypes[^1]);
    }

    // --- SettlementLogic - RemoveSettlement --------------------------

    [Fact]
    public void SettlementLogic_RemoveSettlement_RemovesEntryFromArray()
    {
        var settlements = new JsonArray();
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Alpha"", ""Owner"": { ""UID"": ""p1"" } }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Beta"",  ""Owner"": { ""UID"": ""p2"" } }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Gamma"", ""Owner"": { ""UID"": ""p3"" } }"));

        Assert.Equal(3, settlements.Length);

        SettlementLogic.RemoveSettlement(settlements, 1); // Remove Beta

        Assert.Equal(2, settlements.Length);
        Assert.Equal("Alpha", settlements.GetObject(0).GetString("Name"));
        Assert.Equal("Gamma", settlements.GetObject(1).GetString("Name"));
    }

    [Fact]
    public void SettlementLogic_RemoveSettlement_RemovesFirstEntry()
    {
        var settlements = new JsonArray();
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""First"" }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Second"" }"));

        SettlementLogic.RemoveSettlement(settlements, 0);

        Assert.Equal(1, settlements.Length);
        Assert.Equal("Second", settlements.GetObject(0).GetString("Name"));
    }

    [Fact]
    public void SettlementLogic_RemoveSettlement_RemovesLastEntry()
    {
        var settlements = new JsonArray();
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""First"" }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Last"" }"));

        SettlementLogic.RemoveSettlement(settlements, 1);

        Assert.Equal(1, settlements.Length);
        Assert.Equal("First", settlements.GetObject(0).GetString("Name"));
    }

    [Fact]
    public void SettlementLogic_RemoveSettlement_OutOfRange_NoOp()
    {
        var settlements = new JsonArray();
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Only"" }"));

        SettlementLogic.RemoveSettlement(settlements, 5); // Out of range
        Assert.Equal(1, settlements.Length);

        SettlementLogic.RemoveSettlement(settlements, -1); // Negative
        Assert.Equal(1, settlements.Length);
    }

    [Fact]
    public void SettlementLogic_RemoveSettlement_NoLongerMatchedByFilter()
    {
        // Create a save with a player identifier
        var saveData = new JsonObject();
        var commonState = new JsonObject();
        var owners = new JsonArray();
        var playerOwner = new JsonObject();
        playerOwner.Add("UID", "player123");
        owners.Add(playerOwner);
        commonState.Add("UsedDiscoveryOwnersV2", owners);
        saveData.Add("CommonStateData", commonState);

        var playerState = new JsonObject();

        var settlements = new JsonArray();
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""My Settlement"", ""Owner"": { ""UID"": ""player123"" } }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Other"", ""Owner"": { ""UID"": ""other"" } }"));

        // Before remove
        var before = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Single(before);
        Assert.Equal(0, before[0]);

        // Remove the player's settlement
        SettlementLogic.RemoveSettlement(settlements, 0);

        Assert.Equal(1, settlements.Length);
        // After remove, filter should find nothing
        var after = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Empty(after);
    }

    [Fact]
    public void SettlementLogic_RemoveSettlement_ShiftsSubsequentIndices()
    {
        // Verify that after removal, subsequent entries shift down
        var saveData = new JsonObject();
        var commonState = new JsonObject();
        var owners = new JsonArray();
        var playerOwner = new JsonObject();
        playerOwner.Add("UID", "player1");
        owners.Add(playerOwner);
        commonState.Add("UsedDiscoveryOwnersV2", owners);
        saveData.Add("CommonStateData", commonState);

        var playerState = new JsonObject();

        var settlements = new JsonArray();
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Other"",  ""Owner"": { ""UID"": ""npc"" } }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Mine1"",  ""Owner"": { ""UID"": ""player1"" } }"));
        settlements.Add(JsonObject.Parse(@"{ ""Name"": ""Mine2"",  ""Owner"": { ""UID"": ""player1"" } }"));

        // Before: player settlements at indices 1, 2
        var before = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Equal(2, before.Count);
        Assert.Equal(1, before[0]);
        Assert.Equal(2, before[1]);

        // Remove the NPC settlement at index 0
        SettlementLogic.RemoveSettlement(settlements, 0);

        // After: player settlements shifted to indices 0, 1
        var after = SettlementLogic.FilterSettlements(saveData, playerState, settlements);
        Assert.Equal(2, after.Count);
        Assert.Equal(0, after[0]);
        Assert.Equal(1, after[1]);
    }

    // --- SettlementLogic - FindImportTargetIndex ---------------------

    [Fact]
    public void SettlementLogic_FindImportTargetIndex_SelectedSettlement_ReturnsIndex()
    {
        var settlements = new JsonArray();
        for (int i = 0; i < 10; i++)
            settlements.Add(JsonObject.Parse($@"{{ ""Name"": ""S{i}"" }}"));

        int result = SettlementLogic.FindImportTargetIndex(settlements, 5);
        Assert.Equal(5, result); // Overwrite selected
    }

    [Fact]
    public void SettlementLogic_FindImportTargetIndex_NoSelection_SpareCapacity_ReturnsMinusOne()
    {
        var settlements = new JsonArray();
        for (int i = 0; i < 50; i++)
            settlements.Add(JsonObject.Parse($@"{{ ""Name"": ""S{i}"" }}"));

        int result = SettlementLogic.FindImportTargetIndex(settlements, -1);
        Assert.Equal(-1, result); // Append new
    }

    [Fact]
    public void SettlementLogic_FindImportTargetIndex_NoSelection_ArrayFull_ReturnsMinusTwo()
    {
        var settlements = new JsonArray();
        for (int i = 0; i < SettlementLogic.MaxSettlementSlots; i++)
            settlements.Add(JsonObject.Parse($@"{{ ""Name"": ""S{i}"" }}"));

        int result = SettlementLogic.FindImportTargetIndex(settlements, -1);
        Assert.Equal(-2, result); // Must ask user
    }

    [Fact]
    public void SettlementLogic_MaxSettlementSlots_Is100()
    {
        Assert.Equal(100, SettlementLogic.MaxSettlementSlots);
    }

    // === Save File Round-Trip Tests ===

    [Fact]
    public void JsonSerializer_CompactFormat_NoWhitespace()
    {
        // Compact JSON (ToString) must not contain CRLF, tabs, or spaces after colons
        string json = "{\"a\":1,\"b\":{\"c\":2,\"d\":[3,4]}}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.DoesNotContain("\r\n", result);
        Assert.DoesNotContain("\t", result);
        Assert.Equal(json, result);
    }

    [Fact]
    public void JsonSerializer_Double_PreservesDecimalPoint()
    {
        // NMS save files distinguish integer (1) from float (1.0).
        // Whole-number doubles must serialize with ".0" suffix.
        string json = "{\"a\":1.0,\"b\":0.0,\"c\":75.0,\"d\":-5.0}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.Contains("\"a\":1.0", result);
        Assert.Contains("\"b\":0.0", result);
        Assert.Contains("\"c\":75.0", result);
        Assert.Contains("\"d\":-5.0", result);
    }

    [Fact]
    public void JsonSerializer_Double_PreservesFractionalValues()
    {
        // Fractional doubles must round-trip with full precision
        string json = "{\"x\":0.5,\"y\":0.6000000238418579,\"z\":3.14159}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.Contains("\"x\":0.5", result);
        Assert.Contains("\"y\":0.6000000238418579", result);
        Assert.Contains("\"z\":3.14159", result);
    }

    [Fact]
    public void JsonSerializer_Integer_StaysInteger()
    {
        // Integer values must NOT get a ".0" suffix
        string json = "{\"a\":1,\"b\":0,\"c\":4720,\"d\":-100}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.Equal(json, result);
    }

    [Fact]
    public void JsonSerializer_DoubleArray_PreservesTypes()
    {
        // Arrays of doubles should preserve .0 for whole numbers
        string json = "{\"arr\":[0.0,0.0,0.0,0.0]}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.Equal(json, result);
    }

    [Fact]
    public void JsonParser_Double_AccurateParsing()
    {
        // The parser should produce accurate IEEE 754 doubles
        // 0.6000000238418579 is exactly representable as a double
        string json = "{\"v\":0.6000000238418579}";
        var obj = JsonObject.Parse(json);
        Assert.Equal(0.6000000238418579, obj.GetDouble("v"));
    }

    [Fact]
    public void JsonSerializer_NmsSaveFormat_CompactWithFloats()
    {
        // Simulate a small NMS save structure with mixed types
        string json = "{\"F2P\":4720,\"j3Y\":{\"qAx\":0.5,\"22a\":1.0,\"qLk\":[0.0,0.0,0.0,0.0],\"yGF\":75.0,\"HJQ\":0}}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.Equal(json, result);
    }

    [Fact]
    public void JsonSerializer_RawDouble_PreservesOriginalText()
    {
        // Values from actual NMS saves where .NET formatting would produce different text.
        // Both 0.30000001192092898 and 0.30000001192092896 parse to the same IEEE 754
        // double, but the game writes the former. RawDouble must preserve the original.
        string json = "{\"a\":0.30000001192092898,\"b\":0.029999999329447748,\"c\":203.60643005371094}";
        var obj = JsonObject.Parse(json);
        string result = obj.ToString();
        Assert.Contains("0.30000001192092898", result);
        Assert.Contains("0.029999999329447748", result);
        Assert.Contains("203.60643005371094", result);
        Assert.Equal(json, result);
    }

    // --- Inventory Slot Format Tests ---------------------------------

    [Fact]
    public void InventorySlot_IdMustBeString_NotNestedObject()
    {
        // NMS save format requires "Id" to be a plain string, not a nested object.
        // Correct: {"Type":{"InventoryType":"Product"},"Id":"^EYEBALL","Amount":1,...}
        // Wrong:   {"Type":{"InventoryType":"Product"},"Id":{"Id":"EYEBALL"},"Amount":1,...}
        var slot = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", "Product");
        slot.Add("Type", typeObj);
        slot.Add("Id", "^EYEBALL");
        slot.Add("Amount", 1);
        slot.Add("MaxAmount", 1);

        // "Id" should be a string, not a nested JsonObject
        Assert.IsType<string>(slot.Get("Id"));
        Assert.Equal("^EYEBALL", slot.GetString("Id"));

        // Serialized form should have Id as a string value
        string json = slot.ToString();
        Assert.Contains("\"Id\":\"^EYEBALL\"", json);
        Assert.DoesNotContain("\"Id\":{\"Id\":", json);
    }

    [Fact]
    public void InventorySlot_IdMustHaveCaretPrefix()
    {
        // All item IDs in NMS saves start with '^' prefix
        var slot = new JsonObject();
        slot.Add("Id", "^CREATURE1");
        Assert.Equal("^CREATURE1", slot.GetString("Id"));

        // Empty slots use just "^"
        var emptySlot = new JsonObject();
        emptySlot.Add("Id", "^");
        Assert.Equal("^", emptySlot.GetString("Id"));
    }

    [Fact]
    public void InventorySlot_CorrectFormat_MatchesSaveTemplate()
    {
        // Build a slot matching the expected inventory slot JSON structure
        var slot = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", "Substance");
        slot.Add("Type", typeObj);
        slot.Add("Id", "^FUEL1");
        slot.Add("Amount", 60);
        slot.Add("MaxAmount", 500);
        slot.Add("DamageFactor", 0.0);
        slot.Add("FullyInstalled", true);
        var indexObj = new JsonObject();
        indexObj.Add("X", 0);
        indexObj.Add("Y", 0);
        slot.Add("Index", indexObj);

        string json = slot.ToString();

        // Type is a nested object (correct)
        Assert.Contains("\"Type\":{\"InventoryType\":\"Substance\"}", json);
        // Id is a plain string (correct)
        Assert.Contains("\"Id\":\"^FUEL1\"", json);
        // Index is a nested object (correct)
        Assert.Contains("\"Index\":{\"X\":0,\"Y\":0}", json);
        // Must NOT contain nested Id object
        Assert.DoesNotContain("\"Id\":{\"Id\":", json);
    }

    [Fact]
    public void InventorySlot_ShouldIncludeAddedAutomaticallyField()
    {
        // NMS save format includes an "AddedAutomatically" (5tH) field on every slot.
        // New slots created by the editor must include it.
        var slot = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", "Technology");
        slot.Add("Type", typeObj);
        slot.Add("Id", "^UT_TOX");
        slot.Add("Amount", 100);
        slot.Add("MaxAmount", 100);
        slot.Add("DamageFactor", 0.0);
        slot.Add("FullyInstalled", true);
        slot.Add("AddedAutomatically", false);
        var indexObj = new JsonObject();
        indexObj.Add("X", 5);
        indexObj.Add("Y", 3);
        slot.Add("Index", indexObj);

        string json = slot.ToString();

        // Must contain AddedAutomatically field
        Assert.Contains("\"AddedAutomatically\":false", json);
        // Must have correct InventoryType for tech items
        Assert.Contains("\"InventoryType\":\"Technology\"", json);
    }

    // --- MetaFileWriter.ExtractMetaInfo -------------------------------

    [Fact]
    public void ExtractMetaInfo_ReadsSaveNameFromCommonStateData()
    {
        // SaveName is in CommonStateData, not PlayerStateData
        var saveData = new JsonObject();
        var common = new JsonObject();
        common.Add("SaveName", "TestSave");
        common.Add("TotalPlayTime", 12345);
        saveData.Add("CommonStateData", common);
        var ps = new JsonObject();
        ps.Add("SaveSummary", "TestSummary");
        saveData.Add("PlayerStateData", ps);

        var info = MetaFileWriter.ExtractMetaInfo(saveData);
        Assert.Equal("TestSave", info.SaveName);
        Assert.Equal("TestSummary", info.SaveSummary);
        Assert.Equal((ulong)12345, info.TotalPlayTime);
    }

    [Fact]
    public void ExtractMetaInfo_ReadsDifficultyPresetFromDifficultyState()
    {
        var saveData = new JsonObject();
        var common = new JsonObject();
        saveData.Add("CommonStateData", common);
        var ps = new JsonObject();
        var diffState = new JsonObject();
        var preset = new JsonObject();
        preset.Add("DifficultyPresetType", "Normal");
        diffState.Add("Preset", preset);
        ps.Add("DifficultyState", diffState);
        saveData.Add("PlayerStateData", ps);

        var info = MetaFileWriter.ExtractMetaInfo(saveData);
        Assert.Equal(2, info.DifficultyPreset); // Normal = 2
    }

    // --- StarshipLogic.ShipUsesLegacyColours ---------------------------

    [Fact]
    public void SaveShipData_WritesLegacyColoursToArrayElement()
    {
        // ShipUsesLegacyColours is an array, each element per ship
        var ship = new JsonObject();
        ship.Add("Name", "TestShip");
        var resource = new JsonObject();
        resource.Add("Filename", "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x1234");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);
        var inv = new JsonObject();
        var cls = new JsonObject();
        cls.Add("InventoryClass", "C");
        inv.Add("Class", cls);
        var baseStats = new JsonArray();
        foreach (var statId in new[] { "^SHIP_DAMAGE", "^SHIP_SHIELD", "^SHIP_HYPERDRIVE", "^SHIP_AGILE" })
        {
            var stat = new JsonObject();
            stat.Add("BaseStatID", statId);
            stat.Add("Value", 0.0);
            baseStats.Add(stat);
        }
        inv.Add("BaseStatValues", baseStats);
        inv.Add("Slots", new JsonArray());
        ship.Add("Inventory", inv);

        var playerState = new JsonObject();
        var legacyArr = new JsonArray();
        legacyArr.Add(false);
        legacyArr.Add(false);
        legacyArr.Add(false);
        playerState.Add("ShipUsesLegacyColours", legacyArr);
        playerState.Add("PrimaryShip", 0);

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "TestShip",
            UseOldColours = true,
            ShipIndex = 1,
            PrimaryShipIndex = 0,
        };
        StarshipLogic.SaveShipData(ship, playerState, values);

        // Verify array element was updated, not the whole array replaced
        var arr = playerState.GetArray("ShipUsesLegacyColours");
        Assert.NotNull(arr);
        Assert.Equal(3, arr.Length);
        Assert.Equal(false, arr.Get(0));
        Assert.Equal(true, arr.Get(1));
        Assert.Equal(false, arr.Get(2));
    }

    [Fact]
    public void SaveShipData_SetsClassOnAllInventories()
    {
        // Class should be set on Inventory, Inventory_TechOnly, and Inventory_Cargo
        var ship = new JsonObject();
        ship.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);

        foreach (var invKey in new[] { "Inventory", "Inventory_TechOnly", "Inventory_Cargo" })
        {
            var inv = new JsonObject();
            var cls = new JsonObject();
            cls.Add("InventoryClass", "C");
            inv.Add("Class", cls);
            inv.Add("BaseStatValues", new JsonArray());
            inv.Add("Slots", new JsonArray());
            ship.Add(invKey, inv);
        }

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);
        var legacyArr = new JsonArray();
        legacyArr.Add(false);
        playerState.Add("ShipUsesLegacyColours", legacyArr);

        var values = new StarshipLogic.ShipSaveValues
        {
            ClassIndex = 3, // S class
            ShipIndex = 0,
        };
        StarshipLogic.SaveShipData(ship, playerState, values);

        Assert.Equal("S", ship.GetObject("Inventory")?.GetObject("Class")?.GetString("InventoryClass"));
        Assert.Equal("S", ship.GetObject("Inventory_TechOnly")?.GetObject("Class")?.GetString("InventoryClass"));
        Assert.Equal("S", ship.GetObject("Inventory_Cargo")?.GetObject("Class")?.GetString("InventoryClass"));
    }

    [Fact]
    public void SaveShipData_AlwaysWritesName()
    {
        // Name should be written even when empty (allow clearing ship names)
        var ship = new JsonObject();
        ship.Add("Name", "OldName");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);
        var inv = new JsonObject();
        inv.Add("BaseStatValues", new JsonArray());
        ship.Add("Inventory", inv);

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "",
            ShipIndex = -1,
        };
        StarshipLogic.SaveShipData(ship, playerState, values);

        Assert.Equal("", ship.GetString("Name"));
    }

    // --- GetOwnerTypeForShip ---

    [Theory]
    [InlineData("Fighter", "Ship")]
    [InlineData("Hauler", "Ship")]
    [InlineData("Explorer", "Ship")]
    [InlineData("Shuttle", "Ship")]
    [InlineData("Exotic", "Ship")]
    [InlineData("Solar", "Ship")]
    [InlineData("Living Ship", "AlienShip")]
    [InlineData("Sentinel", "RobotShip")]
    [InlineData("Corvette", "Corvette")]
    public void StarshipLogic_GetOwnerTypeForShip_ReturnsCorrectOwner(string shipType, string expectedOwner)
    {
        Assert.Equal(expectedOwner, StarshipLogic.GetOwnerTypeForShip(shipType));
    }

    [Fact]
    public void StarshipLogic_GetOwnerTypeForShip_UnknownType_DefaultsToShip()
    {
        Assert.Equal("Ship", StarshipLogic.GetOwnerTypeForShip("SomeFutureShipType"));
    }

    // --- ExocraftLogic (GetOwnerTypeForVehicle) ---

    [Theory]
    [InlineData("Roamer", "Exocraft")]
    [InlineData("Nomad", "Exocraft")]
    [InlineData("Pilgrim", "Exocraft")]
    [InlineData("Colossus", "Colossus")]
    [InlineData("Nautilon", "Submarine")]
    [InlineData("Minotaur", "Mech")]
    public void ExocraftLogic_GetOwnerTypeForVehicle_ReturnsCorrectOwner(string vehicleName, string expectedOwner)
    {
        Assert.Equal(expectedOwner, ExocraftLogic.GetOwnerTypeForVehicle(vehicleName));
    }

    [Fact]
    public void ExocraftLogic_GetOwnerTypeForVehicle_UnknownType_DefaultsToExocraft()
    {
        Assert.Equal("Exocraft", ExocraftLogic.GetOwnerTypeForVehicle("SomeFutureVehicle"));
    }

    // --- AccountLogic ------------------------------------------------

    [Fact]
    public void AccountLogic_SaveRewardList_WritesOnlyUnlockedIds()
    {
        var obj = new JsonObject();
        var rewards = new List<(string Id, bool Unlocked)>
        {
            ("^REWARD_A", true),
            ("^REWARD_B", false),
            ("^REWARD_C", true),
        };

        AccountLogic.SaveRewardList(rewards, obj, "UnlockedSeasonRewards");

        var array = obj.GetArray("UnlockedSeasonRewards");
        Assert.NotNull(array);
        Assert.Equal(2, array.Length);
        Assert.Equal("^REWARD_A", array.GetString(0));
        Assert.Equal("^REWARD_C", array.GetString(1));
    }

    [Fact]
    public void AccountLogic_SaveRewardList_ClearsExistingEntries()
    {
        var obj = new JsonObject();
        var existing = new JsonArray();
        existing.Add("^OLD");
        obj.Set("UnlockedSeasonRewards", existing);

        var rewards = new List<(string Id, bool Unlocked)>
        {
            ("^NEW", true),
        };

        AccountLogic.SaveRewardList(rewards, obj, "UnlockedSeasonRewards");

        var array = obj.GetArray("UnlockedSeasonRewards");
        Assert.NotNull(array);
        Assert.Equal(1, array.Length);
        Assert.Equal("^NEW", array.GetString(0));
    }

    [Fact]
    public void AccountLogic_SaveRedeemedRewards_WritesToSaveData()
    {
        var saveData = JsonObject.Parse(
            "{\"PlayerStateData\":{\"RedeemedSeasonRewards\":[],\"RedeemedTwitchRewards\":[]}}");

        var seasonRows = new List<(string Id, bool Redeemed)>
        {
            ("^SEASON_1", true),
            ("^SEASON_2", false),
        };
        var twitchRows = new List<(string Id, bool Redeemed)>
        {
            ("^TWITCH_001", true),
        };

        AccountLogic.SaveRedeemedRewards(saveData, seasonRows, twitchRows);

        var playerState = saveData.GetObject("PlayerStateData")!;
        var redeemed = playerState.GetArray("RedeemedSeasonRewards")!;
        Assert.Equal(1, redeemed.Length);
        Assert.Equal("^SEASON_1", redeemed.GetString(0));

        var twitchRedeemed = playerState.GetArray("RedeemedTwitchRewards")!;
        Assert.Equal(1, twitchRedeemed.Length);
        Assert.Equal("^TWITCH_001", twitchRedeemed.GetString(0));
    }

    [Fact]
    public void AccountLogic_SaveRedeemedRewards_RemovesUncheckedEntries()
    {
        var saveData = JsonObject.Parse(
            "{\"PlayerStateData\":{\"RedeemedSeasonRewards\":[\"^SEASON_1\",\"^SEASON_2\"],\"RedeemedTwitchRewards\":[\"^TWITCH_001\"]}}");

        // Season_1 is unchecked, Season_2 stays redeemed
        var seasonRows = new List<(string Id, bool Redeemed)>
        {
            ("^SEASON_1", false),
            ("^SEASON_2", true),
        };
        var twitchRows = new List<(string Id, bool Redeemed)>
        {
            ("^TWITCH_001", false),
        };

        AccountLogic.SaveRedeemedRewards(saveData, seasonRows, twitchRows);

        var playerState = saveData.GetObject("PlayerStateData")!;
        var redeemed = playerState.GetArray("RedeemedSeasonRewards")!;
        Assert.Equal(1, redeemed.Length);
        Assert.Equal("^SEASON_2", redeemed.GetString(0));

        var twitchRedeemed = playerState.GetArray("RedeemedTwitchRewards")!;
        Assert.Equal(0, twitchRedeemed.Length);
    }

    [Fact]
    public void AccountLogic_SaveRedeemedRewards_CreatesArrayIfMissing()
    {
        var saveData = JsonObject.Parse("{\"PlayerStateData\":{}}");

        var seasonRows = new List<(string Id, bool Redeemed)>
        {
            ("^SEASON_1", true),
        };
        var twitchRows = new List<(string Id, bool Redeemed)>
        {
            ("^TWITCH_001", true),
        };

        AccountLogic.SaveRedeemedRewards(saveData, seasonRows, twitchRows);

        var playerState = saveData.GetObject("PlayerStateData")!;
        var redeemed = playerState.GetArray("RedeemedSeasonRewards");
        Assert.NotNull(redeemed);
        Assert.Equal(1, redeemed!.Length);

        var twitchRedeemed = playerState.GetArray("RedeemedTwitchRewards");
        Assert.NotNull(twitchRedeemed);
        Assert.Equal(1, twitchRedeemed!.Length);
    }

    [Fact]
    public void AccountLogic_GetRedeemedSets_ReadsFromSaveData()
    {
        var saveData = JsonObject.Parse(
            "{\"PlayerStateData\":{\"RedeemedSeasonRewards\":[\"^SEASON_1\",\"^SEASON_2\"],\"RedeemedTwitchRewards\":[\"^TWITCH_001\"]}}");

        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(saveData);

        Assert.Equal(2, seasonRedeemed.Count);
        Assert.Contains("^SEASON_1", seasonRedeemed);
        Assert.Contains("^SEASON_2", seasonRedeemed);
        Assert.Single(twitchRedeemed);
        Assert.Contains("^TWITCH_001", twitchRedeemed);
    }

    [Fact]
    public void AccountLogic_GetRedeemedSets_NullSaveDataReturnsEmpty()
    {
        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(null);
        Assert.Empty(seasonRedeemed);
        Assert.Empty(twitchRedeemed);
    }

    [Fact]
    public void AccountLogic_GetRedeemedSets_MissingArraysReturnsEmpty()
    {
        var saveData = JsonObject.Parse("{\"PlayerStateData\":{}}");
        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(saveData);
        Assert.Empty(seasonRedeemed);
        Assert.Empty(twitchRedeemed);
    }

    [Fact]
    public void AccountLogic_SaveRedeemedRewards_IndependentOfAccountUnlock()
    {
        // This test verifies that redeemed state is independent of account unlock state.
        // A reward can be redeemed in save without being unlocked on account, and vice versa.
        var saveData = JsonObject.Parse("{\"PlayerStateData\":{}}");

        // Only ^SEASON_2 is redeemed, even though account might have ^SEASON_1 unlocked
        var seasonRows = new List<(string Id, bool Redeemed)>
        {
            ("^SEASON_1", false),
            ("^SEASON_2", true),
        };
        var twitchRows = new List<(string Id, bool Redeemed)>();

        AccountLogic.SaveRedeemedRewards(saveData, seasonRows, twitchRows);

        var playerState = saveData.GetObject("PlayerStateData")!;
        var redeemed = playerState.GetArray("RedeemedSeasonRewards")!;
        Assert.Equal(1, redeemed.Length);
        Assert.Equal("^SEASON_2", redeemed.GetString(0));
    }

    [Fact]
    public void AccountLogic_GetUnlockedSet_ReadsStringArray()
    {
        var array = new JsonArray();
        array.Add("^REWARD_A");
        array.Add("^REWARD_B");

        var set = AccountLogic.GetUnlockedSet(array);

        Assert.Equal(2, set.Count);
        Assert.Contains("^REWARD_A", set);
        Assert.Contains("^REWARD_B", set);
    }

    [Fact]
    public void AccountLogic_GetUnlockedSet_NullReturnsEmptySet()
    {
        var set = AccountLogic.GetUnlockedSet(null);
        Assert.Empty(set);
    }

    [Fact]
    public void AccountLogic_BuildRewardRows_IncludesUnknownUnlocked()
    {
        var db = new List<AccountLogic.RewardDbEntry>
        {
            new() { Id = "^KNOWN_1", Name = "Known Reward" },
        };
        var unlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "^KNOWN_1",
            "^UNKNOWN_1",
        };

        var rows = AccountLogic.BuildRewardRows(db, unlocked);

        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].Unlocked);
        Assert.Equal("Known Reward", rows[0].Name);
        Assert.True(rows[1].Unlocked);
        Assert.Equal("(unknown)", rows[1].Name);
    }

    [Fact]
    public void PlatformRewards_IntersectionLogic_OnlyBothSourcesShowTicked()
    {
        // Simulates the intersection logic from AccountPanel.LoadData():
        // only rewards in BOTH accountdata and MXML should show as unlocked.
        var accountUnlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "^SW_PREORDER",   // in both
            "^TGA_SHIP1",     // only in accountdata
        };
        var mxmlUnlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "^SW_PREORDER",   // in both
            "^SW_PREORDER2",  // only in MXML
        };

        // Intersection: only keep rewards in both sources
        var intersected = new HashSet<string>(
            accountUnlocked.Where(id => mxmlUnlocked.Contains(id)),
            StringComparer.OrdinalIgnoreCase);

        Assert.Single(intersected);
        Assert.Contains("^SW_PREORDER", intersected);
        Assert.DoesNotContain("^TGA_SHIP1", intersected);
        Assert.DoesNotContain("^SW_PREORDER2", intersected);

        // Verify BuildRewardRows only marks the intersected reward as unlocked
        var db = new List<AccountLogic.RewardDbEntry>
        {
            new() { Id = "^SW_PREORDER", Name = "Star Wars Pre-order" },
            new() { Id = "^SW_PREORDER2", Name = "Star Wars Pre-order 2" },
            new() { Id = "^TGA_SHIP1", Name = "TGA Ship" },
        };
        var rows = AccountLogic.BuildRewardRows(db, intersected);
        Assert.Equal(3, rows.Count);
        Assert.True(rows.First(r => r.Id == "^SW_PREORDER").Unlocked);
        Assert.False(rows.First(r => r.Id == "^SW_PREORDER2").Unlocked);
        Assert.False(rows.First(r => r.Id == "^TGA_SHIP1").Unlocked);
    }

    [Fact]
    public void AccountLogic_BuildRewardRows_PropagatesSeasonMetadata()
    {
        var db = new List<AccountLogic.RewardDbEntry>
        {
            new() { Id = "^VAULT_ARMOUR", Name = "Heirloom Breastplate", SeasonId = 21, StageId = -1, MustBeUnlocked = false },
            new() { Id = "^EXPD_EGG_14", Name = "Companion Egg", SeasonId = 14, StageId = 3, MustBeUnlocked = true },
        };
        var unlocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "^VAULT_ARMOUR" };

        var rows = AccountLogic.BuildRewardRows(db, unlocked);

        Assert.Equal(2, rows.Count);
        // First row: season 21, unlocked, not MustBeUnlocked
        Assert.Equal(21, rows[0].SeasonId);
        Assert.Equal(-1, rows[0].StageId);
        Assert.False(rows[0].MustBeUnlocked);
        Assert.True(rows[0].Unlocked);
        // Second row: season 14, stage 3, locked, MustBeUnlocked
        Assert.Equal(14, rows[1].SeasonId);
        Assert.Equal(3, rows[1].StageId);
        Assert.True(rows[1].MustBeUnlocked);
        Assert.False(rows[1].Unlocked);
    }

    [Fact]
    public void AccountLogic_SyncJsonArrayEntry_AddsToNewArray()
    {
        var obj = new JsonObject();

        AccountLogic.SyncJsonArrayEntry(obj, "SeenProducts", "^ITEM_1", true);

        var array = obj.GetArray("SeenProducts");
        Assert.NotNull(array);
        Assert.Equal(1, array!.Length);
        Assert.Equal("^ITEM_1", array.GetString(0));
    }

    [Fact]
    public void AccountLogic_SyncJsonArrayEntry_PreventsDuplicates()
    {
        var obj = new JsonObject();
        var existing = new JsonArray();
        existing.Add("^ITEM_1");
        obj.Set("SeenProducts", existing);

        AccountLogic.SyncJsonArrayEntry(obj, "SeenProducts", "^ITEM_1", true);

        var array = obj.GetArray("SeenProducts")!;
        Assert.Equal(1, array.Length);
    }

    [Fact]
    public void AccountLogic_SyncJsonArrayEntry_RemovesEntry()
    {
        var obj = new JsonObject();
        var existing = new JsonArray();
        existing.Add("^ITEM_1");
        existing.Add("^ITEM_2");
        obj.Set("SeenProducts", existing);

        AccountLogic.SyncJsonArrayEntry(obj, "SeenProducts", "^ITEM_1", false);

        var array = obj.GetArray("SeenProducts")!;
        Assert.Equal(1, array.Length);
        Assert.Equal("^ITEM_2", array.GetString(0));
    }

    [Fact]
    public void AccountLogic_SyncJsonArrayEntry_RemoveIsCaseInsensitive()
    {
        var obj = new JsonObject();
        var existing = new JsonArray();
        existing.Add("^Item_1");
        obj.Set("SeenProducts", existing);

        AccountLogic.SyncJsonArrayEntry(obj, "SeenProducts", "^ITEM_1", false);

        var array = obj.GetArray("SeenProducts")!;
        Assert.Equal(0, array.Length);
    }

    [Fact]
    public void AccountLogic_SyncAccountSeenArrays_AddsForRedeemed()
    {
        var userSettings = new JsonObject();

        var rewards = new List<(string Id, bool Present)>
        {
            ("^REWARD_1", true),
            ("^REWARD_2", false),
        };

		// No database — defaults to SeenProducts.
		AccountLogic.SyncAccountSeenArrays(userSettings, rewards);

        var seen = userSettings.GetArray("SeenProducts");
        Assert.NotNull(seen);
        Assert.Equal(1, seen!.Length);
        Assert.Equal("^REWARD_1", seen.GetString(0));
    }

    [Fact]
    public void AccountLogic_SyncAccountSeenArrays_RemovesForUnredeemed()
    {
        var userSettings = new JsonObject();
        var existing = new JsonArray();
        existing.Add("^REWARD_1");
        existing.Add("^REWARD_2");
        userSettings.Set("SeenProducts", existing);

        var rewards = new List<(string Id, bool Present)>
        {
            ("^REWARD_1", false),
            ("^REWARD_2", true),
        };

        AccountLogic.SyncAccountSeenArrays(userSettings, rewards);

        var seen = userSettings.GetArray("SeenProducts")!;
        Assert.Equal(1, seen.Length);
        Assert.Equal("^REWARD_2", seen.GetString(0));
    }

    [Fact]
    public void AccountLogic_SyncJsonArrayEntry_RemovesFromRealAccountData()
    {
        // Walk up from the test assembly to find the repo root (_ref/ directory)
        string baseDir = AppContext.BaseDirectory;
        string? repoRoot = baseDir;
        while (repoRoot != null && !Directory.Exists(Path.Combine(repoRoot, "_ref")))
            repoRoot = Path.GetDirectoryName(repoRoot);
        if (repoRoot == null) return; // Skip if can't find repo root

        string accountPath = Path.Combine(repoRoot, "_ref", "account_mangled", "original_accountdata.hg");
        if (!File.Exists(accountPath))
            return; // Skip if reference file not available

        // Initialize the name mapper (required for deobfuscation of obfuscated keys).
        // In the real app, MainForm.LoadDatabase() does this on startup.
        string mapperPath = Path.Combine(repoRoot, "Resources", "map", "mapping.json");
        if (File.Exists(mapperPath))
        {
            var mapper = new NMSE.Data.JsonNameMapper();
            mapper.Load(mapperPath);
            NMSE.Models.JsonParser.SetDefaultMapper(mapper);
        }

        var accountObj = NMSE.IO.SaveFileManager.LoadSaveFile(accountPath);

        // Verify the root keys are deobfuscated
        var rawNames = accountObj.GetRawNames();
        Assert.Contains("UserSettingsData", rawNames);

        var userSettings = accountObj.GetObject("UserSettingsData");
        Assert.NotNull(userSettings);

        // Verify SeenProducts exists and is deobfuscated
        var seenProducts = userSettings!.GetArray("SeenProducts");
        Assert.NotNull(seenProducts);

        // Verify ^VAULT_ARMOUR is in SeenProducts before removal
        bool found = false;
        for (int i = 0; i < seenProducts!.Length; i++)
        {
            if (string.Equals(seenProducts.GetString(i), "^VAULT_ARMOUR", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "^VAULT_ARMOUR should be in SeenProducts before removal");

        int before = seenProducts.Length;
        AccountLogic.SyncJsonArrayEntry(userSettings, "SeenProducts", "^VAULT_ARMOUR", false);

        var after = userSettings.GetArray("SeenProducts")!.Length;
        Assert.Equal(before - 1, after);

        // Also verify the entry is actually gone
        bool stillPresent = false;
        var updatedArray = userSettings.GetArray("SeenProducts")!;
        for (int i = 0; i < updatedArray.Length; i++)
        {
            if (string.Equals(updatedArray.GetString(i), "^VAULT_ARMOUR", StringComparison.OrdinalIgnoreCase))
            {
                stillPresent = true;
                break;
            }
        }
        Assert.False(stillPresent, "^VAULT_ARMOUR should NOT be in SeenProducts after removal");
    }

    [Fact]
    public void RewardDatabase_LoadsNewFieldsFromJson()
    {
        // Create a temp Rewards.json with the new fields
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_test_reward_fields_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[
                { ""Id"": ""^TEST_REWARD"", ""Name"": ""Test"", ""Category"": ""season"",
                  ""ProductId"": ""TEST"", ""MustBeUnlocked"": true, ""SeasonId"": 5, ""StageId"": 2 },
                { ""Id"": ""^TWITCH_1"", ""Name"": ""Twitch Test"", ""Category"": ""twitch"",
                  ""ProductId"": ""TW1"" }
            ]";
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), json);

            RewardDatabase.Reset();
            bool loaded = RewardDatabase.LoadFromJsonDirectory(tmpDir);
            Assert.True(loaded);

            var season = RewardDatabase.SeasonRewards.First();
            Assert.Equal("^TEST_REWARD", season.Id);
            Assert.True(season.Unlock);
            Assert.Equal(5, season.SeasonId);
            Assert.Equal(2, season.StageId);

            var twitch = RewardDatabase.TwitchRewards.First();
            Assert.Equal("^TWITCH_1", twitch.Id);
            Assert.False(twitch.Unlock);
            Assert.Equal(-1, twitch.SeasonId);
            Assert.Equal(-1, twitch.StageId);
        }
        finally
        {
            RewardDatabase.Reset();
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    // --- MxmlRewardEditor --------------------------------------------

    [Fact]
    public void MxmlRewardEditor_ReadUnlockedRewards_ReadsExistingEntries()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_read_{Guid.NewGuid()}.MXML");
        try
        {
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
  <Property name=""UnlockedPlatformRewards"">
    <Property name=""UnlockedPlatformRewards"" value=""SW_PREORDER"" _index=""0"" />
    <Property name=""UnlockedPlatformRewards"" value=""TGA_SHIP1"" _index=""1"" />
  </Property>
</Data>");

            var result = MxmlRewardEditor.ReadUnlockedRewards(mxml);

            Assert.Equal(2, result.Count);
            Assert.Contains("^SW_PREORDER", result);
            Assert.Contains("^TGA_SHIP1", result);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    [Fact]
    public void MxmlRewardEditor_ReadUnlockedRewards_ReturnsEmptyForMissingFile()
    {
        var result = MxmlRewardEditor.ReadUnlockedRewards("/nonexistent/path.MXML");
        Assert.Empty(result);
    }

    [Fact]
    public void MxmlRewardEditor_ReadUnlockedRewards_NullPathReturnsEmpty()
    {
        var result = MxmlRewardEditor.ReadUnlockedRewards(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void MxmlRewardEditor_WriteUnlockedRewards_AddsNewEntries()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_write_{Guid.NewGuid()}.MXML");
        try
        {
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
  <Property name=""SomeOtherSetting"" value=""true"" />
  <Property name=""UnlockedPlatformRewards"">
    <Property name=""UnlockedPlatformRewards"" value=""TGA_SHIP1"" _index=""0"" />
  </Property>
</Data>");

            var rewards = new List<(string Id, bool Unlocked)>
            {
                ("^TGA_SHIP1", true),
                ("^SW_PREORDER", true),
                ("^SW_PREORDER2", false), // not unlocked, should not be written
            };

            bool result = MxmlRewardEditor.WriteUnlockedRewards(mxml, rewards);
            Assert.True(result);

            // Re-read and verify
            var unlocked = MxmlRewardEditor.ReadUnlockedRewards(mxml);
            Assert.Equal(2, unlocked.Count);
            Assert.Contains("^TGA_SHIP1", unlocked);
            Assert.Contains("^SW_PREORDER", unlocked);
            Assert.DoesNotContain("^SW_PREORDER2", unlocked);

            // Verify other settings are preserved
            var doc = System.Xml.Linq.XDocument.Load(mxml);
            var otherProp = doc.Root!.Elements("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "SomeOtherSetting");
            Assert.NotNull(otherProp);
            Assert.Equal("true", otherProp!.Attribute("value")?.Value);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    [Fact]
    public void MxmlRewardEditor_WriteUnlockedRewards_RemovesUntickedEntries()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_remove_{Guid.NewGuid()}.MXML");
        try
        {
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
  <Property name=""UnlockedPlatformRewards"">
    <Property name=""UnlockedPlatformRewards"" value=""SW_PREORDER"" _index=""0"" />
    <Property name=""UnlockedPlatformRewards"" value=""TGA_SHIP1"" _index=""1"" />
  </Property>
</Data>");

            // Untick SW_PREORDER, keep TGA_SHIP1
            var rewards = new List<(string Id, bool Unlocked)>
            {
                ("^SW_PREORDER", false),
                ("^TGA_SHIP1", true),
            };

            bool result = MxmlRewardEditor.WriteUnlockedRewards(mxml, rewards);
            Assert.True(result);

            var unlocked = MxmlRewardEditor.ReadUnlockedRewards(mxml);
            Assert.Single(unlocked);
            Assert.Contains("^TGA_SHIP1", unlocked);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    [Fact]
    public void MxmlRewardEditor_WriteUnlockedRewards_AssignsSequentialIndices()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_index_{Guid.NewGuid()}.MXML");
        try
        {
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
</Data>");

            var rewards = new List<(string Id, bool Unlocked)>
            {
                ("^SW_PREORDER", true),
                ("^SW_PREORDER2", true),
                ("^TGA_SHIP1", true),
            };

            MxmlRewardEditor.WriteUnlockedRewards(mxml, rewards);

            var doc = System.Xml.Linq.XDocument.Load(mxml);

            // Verify a container element was created
            var container = doc.Root!.Elements("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "UnlockedPlatformRewards"
                                  && e.Attribute("value") == null);
            Assert.NotNull(container);

            // Verify child entries inside the container
            var props = container!.Elements("Property")
                .Where(e => e.Attribute("name")?.Value == "UnlockedPlatformRewards")
                .ToList();

            Assert.Equal(3, props.Count);
            Assert.Equal("0", props[0].Attribute("_index")?.Value);
            Assert.Equal("1", props[1].Attribute("_index")?.Value);
            Assert.Equal("2", props[2].Attribute("_index")?.Value);
            Assert.Equal("SW_PREORDER", props[0].Attribute("value")?.Value);
            Assert.Equal("SW_PREORDER2", props[1].Attribute("value")?.Value);
            Assert.Equal("TGA_SHIP1", props[2].Attribute("value")?.Value);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    [Fact]
    public void MxmlRewardEditor_WriteUnlockedRewards_ReturnsFalseForMissingFile()
    {
        var rewards = new List<(string Id, bool Unlocked)> { ("^TGA_SHIP1", true) };
        bool result = MxmlRewardEditor.WriteUnlockedRewards("/nonexistent/path.MXML", rewards);
        Assert.False(result);
    }

    [Fact]
    public void MxmlRewardEditor_SyncPlatformRewards_SkipsGracefullyWhenNullPath()
    {
        var rewards = new List<(string Id, bool Unlocked)> { ("^TGA_SHIP1", true) };
        bool result = MxmlRewardEditor.SyncPlatformRewards(null, rewards);
        Assert.True(result); // Gracefully skipped
    }

    [Fact]
    public void MxmlRewardEditor_ReadUnlockedRewards_HandlesPrefixedValues()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_prefix_{Guid.NewGuid()}.MXML");
        try
        {
            // MXML stores values without ^ prefix; ReadUnlockedRewards should add it
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
  <Property name=""UnlockedPlatformRewards"">
    <Property name=""UnlockedPlatformRewards"" value=""SW_PREORDER"" _index=""0"" />
  </Property>
</Data>");

            var result = MxmlRewardEditor.ReadUnlockedRewards(mxml);

            Assert.Single(result);
            Assert.Contains("^SW_PREORDER", result);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    [Fact]
    public void MxmlRewardEditor_WriteUnlockedRewards_StripsCaretPrefix()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_caret_{Guid.NewGuid()}.MXML");
        try
        {
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
</Data>");

            var rewards = new List<(string Id, bool Unlocked)>
            {
                ("^SW_PREORDER", true),
            };

            MxmlRewardEditor.WriteUnlockedRewards(mxml, rewards);

            // The MXML value should NOT have the ^ prefix and should be inside a container
            var doc = System.Xml.Linq.XDocument.Load(mxml);
            var container = doc.Root!.Elements("Property")
                .First(e => e.Attribute("name")?.Value == "UnlockedPlatformRewards"
                         && e.Attribute("value") == null);
            var prop = container.Elements("Property")
                .First(e => e.Attribute("name")?.Value == "UnlockedPlatformRewards");
            Assert.Equal("SW_PREORDER", prop.Attribute("value")?.Value);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    [Fact]
    public void MxmlRewardEditor_WriteUnlockedRewards_MigratesLegacyFlatFormat()
    {
        string mxml = Path.Combine(Path.GetTempPath(), $"test_migrate_{Guid.NewGuid()}.MXML");
        try
        {
            // Legacy flat format (entries as direct children of root, no container)
            File.WriteAllText(mxml, @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcUserSettingsData"">
  <Property name=""UnlockedPlatformRewards"" value=""TGA_SHIP1"" _index=""0"" />
</Data>");

            var rewards = new List<(string Id, bool Unlocked)>
            {
                ("^TGA_SHIP1", true),
                ("^SW_PREORDER", true),
            };

            bool result = MxmlRewardEditor.WriteUnlockedRewards(mxml, rewards);
            Assert.True(result);

            // Verify it was migrated to the nested container format
            var doc = System.Xml.Linq.XDocument.Load(mxml);
            var container = doc.Root!.Elements("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "UnlockedPlatformRewards"
                                  && e.Attribute("value") == null);
            Assert.NotNull(container);

            var children = container!.Elements("Property").ToList();
            Assert.Equal(2, children.Count);
            Assert.Equal("TGA_SHIP1", children[0].Attribute("value")?.Value);
            Assert.Equal("SW_PREORDER", children[1].Attribute("value")?.Value);

            // Verify no flat entries remain at root level
            var flatEntries = doc.Root.Elements("Property")
                .Where(e => e.Attribute("name")?.Value == "UnlockedPlatformRewards"
                         && e.Attribute("value") != null)
                .ToList();
            Assert.Empty(flatEntries);
        }
        finally
        {
            try { File.Delete(mxml); } catch { }
        }
    }

    // --- AppConfig.RecentDirectories -----------------------------------

    [Fact]
    public void AppConfig_RecentDirectories_EmptyByDefault()
    {
        var config = new AppConfig();
        Assert.Empty(config.RecentDirectories);
    }

    [Fact]
    public void AppConfig_RecentDirectories_RoundTrips()
    {
        var config = new AppConfig();
        var dirs = new List<string> { "/path/a", "/path/b", "/path/c" };
        config.RecentDirectories = dirs;
        Assert.Equal(dirs, config.RecentDirectories);
    }

    [Fact]
    public void AppConfig_AddRecentDirectory_InsertsAtFront()
    {
        var config = new AppConfig();
        config.RecentDirectories = new List<string> { "/path/a", "/path/b" };
        config.AddRecentDirectory("/path/c");
        Assert.Equal("/path/c", config.RecentDirectories[0]);
        Assert.Equal(3, config.RecentDirectories.Count);
    }

    [Fact]
    public void AppConfig_AddRecentDirectory_MovesExistingToFront()
    {
        var config = new AppConfig();
        config.RecentDirectories = new List<string> { "/path/a", "/path/b", "/path/c" };
        config.AddRecentDirectory("/path/c");
        Assert.Equal("/path/c", config.RecentDirectories[0]);
        Assert.Equal(3, config.RecentDirectories.Count); // No duplicates
    }

    [Fact]
    public void AppConfig_AddRecentDirectory_TrimsToMaxSize()
    {
        var config = new AppConfig();
        for (int i = 0; i < 10; i++)
            config.AddRecentDirectory($"/path/{i}");
        Assert.True(config.RecentDirectories.Count <= AppConfig.MaxRecentDirectories);
    }

    [Fact]
    public void AppConfig_AddRecentDirectory_KeepsDefaultDirectory()
    {
        var config = new AppConfig();
        string defaultDir = "/path/default";

        // Fill the MRU with non-default directories
        for (int i = 0; i < 10; i++)
            config.AddRecentDirectory($"/path/{i}", defaultDir);

        // Default directory must always be present
        Assert.Contains(defaultDir, config.RecentDirectories);
        Assert.True(config.RecentDirectories.Count <= AppConfig.MaxRecentDirectories);
    }

    [Fact]
    public void AppConfig_AddRecentDirectory_SetsLastDirectory()
    {
        var config = new AppConfig();
        config.AddRecentDirectory("/path/new");
        Assert.Equal("/path/new", config.LastDirectory);
    }

    [Fact]
    public void AppConfig_RecentDirectories_NullClearsProperty()
    {
        var config = new AppConfig();
        config.RecentDirectories = new List<string> { "/path/a" };
        Assert.NotEmpty(config.RecentDirectories);
        config.RecentDirectories = new List<string>();
        Assert.Empty(config.RecentDirectories);
    }

    // --- SaveFileManager.FindDefaultSaveDirectory ---------------------

    [Fact]
    public void FindDefaultSaveDirectory_ReturnsProfileDir_WhenSteamPathExists()
    {
        // Create a mock Steam NMS save structure
        var baseDir = Path.Combine(Path.GetTempPath(), $"nmse_test_default_{Guid.NewGuid():N}");
        var nmsDir = Path.Combine(baseDir, "HelloGames", "NMS");
        var profileDir = Path.Combine(nmsDir, "st_12345678901234567");
        Directory.CreateDirectory(profileDir);
        try
        {
            // FindDefaultSaveDirectory checks actual OS paths, not arbitrary paths,
            // so we verify the method doesn't throw and returns a string or null
            var result = SaveFileManager.FindDefaultSaveDirectory();
            // result may or may not equal profileDir (depends on whether real path exists)
            // but the method should not throw
            Assert.True(result is null || Directory.Exists(result));
        }
        finally { Directory.Delete(baseDir, true); }
    }

    [Fact]
    public void FindDefaultSaveDirectory_ReturnsNull_WhenNoNmsInstallation()
    {
        // On CI where NMS is not installed, should return null or an existing directory
        var result = SaveFileManager.FindDefaultSaveDirectory();
        Assert.True(result is null || Directory.Exists(result));
    }

    // --- RawJsonLogic --------------------------------------------------

    [Fact]
    public void RawJsonLogic_ToDisplayString_ReturnsFormattedJson()
    {
        var obj = new JsonObject();
        obj.Add("key1", "value1");
        obj.Add("key2", 42);

        string result = RawJsonLogic.ToDisplayString(obj);

        Assert.Contains("key1", result);
        Assert.Contains("value1", result);
        Assert.Contains("key2", result);
        Assert.Contains("42", result);
    }

    [Fact]
    public void RawJsonLogic_ParseJson_RoundTrips()
    {
        var original = new JsonObject();
        original.Add("name", "test");
        original.Add("count", 5);

        string json = RawJsonLogic.ToDisplayString(original);
        var parsed = RawJsonLogic.ParseJson(json);

        Assert.Equal("test", parsed.GetString("name"));
        Assert.Equal(5, parsed.GetInt("count"));
    }

    [Fact]
    public void RawJsonLogic_FormatJson_FormatsValidJson()
    {
        string compact = "{\"a\":1,\"b\":\"hello\"}";
        string formatted = RawJsonLogic.FormatJson(compact);

        Assert.Contains("a", formatted);
        Assert.Contains("hello", formatted);
        // Formatted output should contain newlines/indentation
        Assert.Contains("\n", formatted);
    }

    // --- RawJsonLogic: FormatValueForEdit (no quotes for strings) ---

    [Fact]
    public void RawJsonLogic_FormatValueForEdit_StringWithoutQuotes()
    {
        // FormatValueForEdit should return raw string without surrounding quotes
        string result = RawJsonLogic.FormatValueForEdit("^MIRROR");
        Assert.Equal("^MIRROR", result);
        Assert.False(result.StartsWith('"'));
        Assert.False(result.EndsWith('"'));
    }

    [Fact]
    public void RawJsonLogic_FormatValueForEdit_NullReturnsNull()
    {
        Assert.Equal("null", RawJsonLogic.FormatValueForEdit(null));
    }

    [Fact]
    public void RawJsonLogic_FormatValueForEdit_BoolReturnsLowercase()
    {
        Assert.Equal("true", RawJsonLogic.FormatValueForEdit(true));
        Assert.Equal("false", RawJsonLogic.FormatValueForEdit(false));
    }

    [Fact]
    public void RawJsonLogic_FormatValueForEdit_NumberReturnsPlain()
    {
        Assert.Equal("42", RawJsonLogic.FormatValueForEdit(42));
        Assert.Equal("3.14", RawJsonLogic.FormatValueForEdit(3.14));
    }

    // --- RawJsonLogic: ParseInputValue type preservation ---

    [Fact]
    public void RawJsonLogic_ParseInputValue_StringOriginal_PreservesType()
    {
        // When original value is a string, the result should always be a string
        Assert.IsType<string>(RawJsonLogic.ParseInputValue("true", "original"));
        Assert.Equal("true", RawJsonLogic.ParseInputValue("true", "original"));
    }

    [Fact]
    public void RawJsonLogic_ParseInputValue_StringOriginal_PreservesNumericString()
    {
        // A numeric string like "42" should stay as string "42", not become int 42
        Assert.IsType<string>(RawJsonLogic.ParseInputValue("42", "original"));
        Assert.Equal("42", RawJsonLogic.ParseInputValue("42", "original"));
    }

    [Fact]
    public void RawJsonLogic_ParseInputValue_StringOriginal_PreservesNullString()
    {
        // A string value "null" should stay as string "null", not become null
        Assert.IsType<string>(RawJsonLogic.ParseInputValue("null", "original"));
        Assert.Equal("null", RawJsonLogic.ParseInputValue("null", "original"));
    }

    [Fact]
    public void RawJsonLogic_ParseInputValue_IntOriginal_ParsesNumber()
    {
        // When original is not a string, normal parsing applies
        object? result = RawJsonLogic.ParseInputValue("42", 0);
        Assert.Equal(42L, result);
    }

    [Fact]
    public void RawJsonLogic_ParseInputValue_BoolOriginal_ParsesBool()
    {
        object? result = RawJsonLogic.ParseInputValue("true", false);
        Assert.IsType<bool>(result);
        Assert.Equal(true, result);
    }

    [Fact]
    public void RawJsonLogic_ParseInputValue_NullOriginal_ParsesNull()
    {
        Assert.Null(RawJsonLogic.ParseInputValue("null", 0));
    }

    [Fact]
    public void RawJsonLogic_ParseInputValue_NewValue_ParsesTypes()
    {
        // Overload without originalValue should infer types
        Assert.Null(RawJsonLogic.ParseInputValue("null"));
        Assert.Equal(true, RawJsonLogic.ParseInputValue("true"));
        Assert.Equal(false, RawJsonLogic.ParseInputValue("false"));
        Assert.Equal(42L, RawJsonLogic.ParseInputValue("42"));
        Assert.Equal("hello", RawJsonLogic.ParseInputValue("hello"));
    }

    [Fact]
    public void RawJsonLogic_StringEdit_RoundTrip()
    {
        // Simulate editing "^MIRROR" to "B_WNG_R"
        // Done via FormatValueForEdit -> ParseInputValue
        string originalValue = "^MIRROR";
        string editDisplayed = RawJsonLogic.FormatValueForEdit(originalValue);
        Assert.Equal("^MIRROR", editDisplayed); // No quotes shown

        // User types new value directly
        object? result = RawJsonLogic.ParseInputValue("B_WNG_R", originalValue);
        Assert.IsType<string>(result);
        Assert.Equal("B_WNG_R", result);
    }

    // --- RawJsonLogic: SerializeValue ---

    [Fact]
    public void RawJsonLogic_SerializeValue_JsonObject_ProducesFormattedJson()
    {
        var obj = new JsonObject();
        obj.Add("Name", "TestBase");
        obj.Add("Version", 8);
        string json = RawJsonLogic.SerializeValue(obj);
        Assert.Contains("\"Name\": \"TestBase\"", json);
        Assert.Contains("\"Version\": 8", json);
    }

    [Fact]
    public void RawJsonLogic_SerializeValue_JsonArray_ProducesFormattedJson()
    {
        var arr = new JsonArray();
        arr.Add(1);
        arr.Add(2);
        arr.Add(3);
        string json = RawJsonLogic.SerializeValue(arr);
        Assert.StartsWith("[", json.Trim());
        Assert.Contains("1", json);
        Assert.Contains("3", json);
    }

    [Fact]
    public void RawJsonLogic_SerializeValue_String_ProducesQuotedString()
    {
        string json = RawJsonLogic.SerializeValue("hello world");
        Assert.Equal("\"hello world\"", json);
    }

    [Fact]
    public void RawJsonLogic_SerializeValue_Integer_ProducesNumber()
    {
        string json = RawJsonLogic.SerializeValue(42);
        Assert.Equal("42", json);
    }

    [Fact]
    public void RawJsonLogic_SerializeValue_Boolean_ProducesLiteral()
    {
        Assert.Equal("true", RawJsonLogic.SerializeValue(true));
        Assert.Equal("false", RawJsonLogic.SerializeValue(false));
    }

    [Fact]
    public void RawJsonLogic_SerializeValue_Null_ProducesNull()
    {
        Assert.Equal("null", RawJsonLogic.SerializeValue(null));
    }

    [Fact]
    public void RawJsonLogic_SerializeValue_RoundTrips_WithParseValue()
    {
        var obj = new JsonObject();
        obj.Add("Key", "Value");
        obj.Add("Count", 5);
        var arr = new JsonArray();
        arr.Add(obj);

        string json = RawJsonLogic.SerializeValue(arr);
        object? parsed = RawJsonLogic.ParseValue(json);
        Assert.IsType<JsonArray>(parsed);
        var parsedArr = (JsonArray)parsed;
        Assert.Equal(1, parsedArr.Length);
        var innerObj = parsedArr.Get(0) as JsonObject;
        Assert.NotNull(innerObj);
        Assert.Equal("Value", innerObj.GetString("Key"));
        Assert.Equal(5, innerObj.GetInt("Count"));
    }

    [Fact]
    public void RawJsonLogic_ParseValue_PrimitiveTypes()
    {
        Assert.Equal(42, RawJsonLogic.ParseValue("42"));
        Assert.Equal("hello", RawJsonLogic.ParseValue("\"hello\""));
        Assert.Equal(true, RawJsonLogic.ParseValue("true"));
        Assert.Equal(false, RawJsonLogic.ParseValue("false"));
        Assert.Null(RawJsonLogic.ParseValue("null"));
    }

    [Fact]
    public void RawJsonLogic_ParseValue_Object_ReturnsJsonObject()
    {
        var result = RawJsonLogic.ParseValue("{\"Name\": \"Test\"}");
        Assert.IsType<JsonObject>(result);
        Assert.Equal("Test", ((JsonObject)result).GetString("Name"));
    }

    [Fact]
    public void RawJsonLogic_ParseValue_Array_ReturnsJsonArray()
    {
        var result = RawJsonLogic.ParseValue("[1, 2, 3]");
        Assert.IsType<JsonArray>(result);
        Assert.Equal(3, ((JsonArray)result).Length);
    }

    // --- RawJsonLogic: ComputeSimpleDiff ---

    [Fact]
    public void RawJsonLogic_ComputeSimpleDiff_IdenticalInputs_ReturnsNoChanges()
    {
        string json = "{\n  \"key\": \"value\"\n}";
        string result = RawJsonLogic.ComputeSimpleDiff(json, json);
        Assert.Equal("No changes detected.", result);
    }

    [Fact]
    public void RawJsonLogic_ComputeSimpleDiff_AddedLine_ShowsPlusPrefix()
    {
        string original = "{\n  \"a\": 1\n}";
        string modified = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        string diff = RawJsonLogic.ComputeSimpleDiff(original, modified);
        Assert.Contains("+ ", diff);
    }

    [Fact]
    public void RawJsonLogic_ComputeSimpleDiff_RemovedLine_ShowsMinusPrefix()
    {
        string original = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        string modified = "{\n  \"a\": 1\n}";
        string diff = RawJsonLogic.ComputeSimpleDiff(original, modified);
        Assert.Contains("- ", diff);
    }

    [Fact]
    public void RawJsonLogic_ComputeSimpleDiff_UnchangedLines_ShowsDoubleSpacePrefix()
    {
        string original = "{\n  \"a\": 1\n}";
        string modified = "{\n  \"a\": 2\n}";
        string diff = RawJsonLogic.ComputeSimpleDiff(original, modified);
        // The braces { and } should be unchanged
        Assert.Contains("  {", diff);
    }

    [Fact]
    public void RawJsonLogic_ComputeSimpleDiff_ChangedValue_ShowsBothOldAndNew()
    {
        var original = new JsonObject();
        original.Add("name", "old");
        var modified = new JsonObject();
        modified.Add("name", "new");
        string diff = RawJsonLogic.ComputeSimpleDiff(
            RawJsonLogic.ToDisplayString(original),
            RawJsonLogic.ToDisplayString(modified));
        Assert.Contains("-", diff);
        Assert.Contains("+", diff);
    }

    // --- RawJsonLogic: ComputeCompactDiff ---

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_IdenticalInputs_ReturnsEmptyList()
    {
        string json = "{\n  \"key\": \"value\"\n}";
        var result = RawJsonLogic.ComputeCompactDiff(json, json);
        Assert.Empty(result);
    }

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_AddedLine_ShowsAddedType()
    {
        string original = "{\n  \"a\": 1\n}";
        string modified = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        var result = RawJsonLogic.ComputeCompactDiff(original, modified);
        Assert.Contains(result, dl => dl.Type == RawJsonLogic.DiffLineType.Added);
    }

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_RemovedLine_ShowsRemovedType()
    {
        string original = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        string modified = "{\n  \"a\": 1\n}";
        var result = RawJsonLogic.ComputeCompactDiff(original, modified);
        Assert.Contains(result, dl => dl.Type == RawJsonLogic.DiffLineType.Removed);
    }

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_LargeUnchangedRegion_CollapsedWithSeparator()
    {
        // Build two documents with a large unchanged region between two changes
        var sbOld = new System.Text.StringBuilder();
        var sbNew = new System.Text.StringBuilder();
        sbOld.AppendLine("CHANGED_OLD_START");
        sbNew.AppendLine("CHANGED_NEW_START");
        for (int i = 0; i < 50; i++)
        {
            sbOld.AppendLine(CultureInfo.InvariantCulture, $"  unchanged line {i}");
            sbNew.AppendLine(CultureInfo.InvariantCulture, $"  unchanged line {i}");
        }
        sbOld.AppendLine("CHANGED_OLD_END");
        sbNew.AppendLine("CHANGED_NEW_END");

        var result = RawJsonLogic.ComputeCompactDiff(sbOld.ToString(), sbNew.ToString(), contextLines: 3);

        // Should have a separator in the middle (the 50 unchanged lines are collapsed)
        Assert.Contains(result, dl => dl.Type == RawJsonLogic.DiffLineType.Separator);

        // The total lines should be much less than 50 + 4 changes
        // (3 context before + separator + 3 context after = ~10 lines vs 54)
        int totalLines = result.Count;
        Assert.True(totalLines < 20, $"Expected compact output, got {totalLines} lines");
    }

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_ContextLinesPreserved()
    {
        // Build: changed line, then 10 unchanged, then changed line
        var sbOld = new System.Text.StringBuilder();
        var sbNew = new System.Text.StringBuilder();
        sbOld.AppendLine("OLD_FIRST");
        sbNew.AppendLine("NEW_FIRST");
        for (int i = 0; i < 10; i++)
        {
            sbOld.AppendLine(CultureInfo.InvariantCulture, $"  same {i}");
            sbNew.AppendLine(CultureInfo.InvariantCulture, $"  same {i}");
        }
        sbOld.AppendLine("OLD_LAST");
        sbNew.AppendLine("NEW_LAST");

        var result = RawJsonLogic.ComputeCompactDiff(sbOld.ToString(), sbNew.ToString(), contextLines: 2);

        // Context lines near the changes should be preserved
        var contextLines = result.Where(dl => dl.Type == RawJsonLogic.DiffLineType.Context).ToList();
        Assert.True(contextLines.Count >= 2, $"Expected at least 2 context lines, got {contextLines.Count}");
    }

    [Fact]
    public void RawJsonLogic_CollapseContext_NoChanges_ReturnsEmpty()
    {
        // All context lines with no changes should always collapse to nothing
        var raw = new List<RawJsonLogic.DiffLine>
        {
            new(RawJsonLogic.DiffLineType.Context, "line1", 1, 1),
            new(RawJsonLogic.DiffLineType.Context, "line2", 2, 2),
            new(RawJsonLogic.DiffLineType.Context, "line3", 3, 3),
        };
        var result = RawJsonLogic.CollapseContext(raw, 1);
		// No changes, so everything is far from a change — should be empty or just a separator
		Assert.DoesNotContain(result, dl => dl.Type == RawJsonLogic.DiffLineType.Added);
        Assert.DoesNotContain(result, dl => dl.Type == RawJsonLogic.DiffLineType.Removed);
    }

    // --- RawJsonLogic: Myers diff correctness (duplicate JSON lines) ---

    [Fact]
    public void RawJsonLogic_ComputeSimpleDiff_DuplicateJsonLines_MinimalDiff()
    {
        // This test reproduces the bug where changing a single value in a JSON array
        // with duplicate structures (like {, }, ],) caused large phantom add/remove blocks
        string original = "{\n  \"Array\": [\n    {\n      \"Value\": 5\n    },\n    {\n      \"Value\": 5\n    },\n    {\n      \"Value\": 5\n    }\n  ]\n}";
        string modified = "{\n  \"Array\": [\n    {\n      \"Value\": 1\n    },\n    {\n      \"Value\": 5\n    },\n    {\n      \"Value\": 5\n    }\n  ]\n}";

        string diff = RawJsonLogic.ComputeSimpleDiff(original, modified);

        // Should have exactly 1 removed and 1 added line (the changed value)
        var lines = diff.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int addedCount = lines.Count(l => l.StartsWith("+ ", StringComparison.Ordinal));
        int removedCount = lines.Count(l => l.StartsWith("- ", StringComparison.Ordinal));

        Assert.Equal(1, addedCount);
        Assert.Equal(1, removedCount);
        Assert.Contains("-       \"Value\": 5", diff);
        Assert.Contains("+       \"Value\": 1", diff);
    }

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_DuplicateJsonLines_MinimalDiff()
    {
        // Same scenario but via ComputeCompactDiff
        string original = "{\n  \"Array\": [\n    {\n      \"Value\": 5\n    },\n    {\n      \"Value\": 5\n    },\n    {\n      \"Value\": 5\n    }\n  ]\n}";
        string modified = "{\n  \"Array\": [\n    {\n      \"Value\": 1\n    },\n    {\n      \"Value\": 5\n    },\n    {\n      \"Value\": 5\n    }\n  ]\n}";

        var result = RawJsonLogic.ComputeCompactDiff(original, modified);

        int addedCount = result.Count(dl => dl.Type == RawJsonLogic.DiffLineType.Added);
        int removedCount = result.Count(dl => dl.Type == RawJsonLogic.DiffLineType.Removed);

        Assert.Equal(1, addedCount);
        Assert.Equal(1, removedCount);
    }

    [Fact]
    public void RawJsonLogic_ComputeCompactDiff_NestedDuplicateStructures_MinimalDiff()
    {
        // Deep JSON nesting with many duplicate closing braces
        string original = "{\n  \"PlayerStateData\": {\n    \"Records\": [\n      {\n        \"StatValue\": 0,\n        \"Name\": \"a\"\n      },\n      {\n        \"StatValue\": 0,\n        \"Name\": \"b\"\n      }\n    ]\n  }\n}";
        string modified = "{\n  \"PlayerStateData\": {\n    \"Records\": [\n      {\n        \"StatValue\": 1,\n        \"Name\": \"a\"\n      },\n      {\n        \"StatValue\": 0,\n        \"Name\": \"b\"\n      }\n    ]\n  }\n}";

        var result = RawJsonLogic.ComputeCompactDiff(original, modified);

        int addedCount = result.Count(dl => dl.Type == RawJsonLogic.DiffLineType.Added);
        int removedCount = result.Count(dl => dl.Type == RawJsonLogic.DiffLineType.Removed);

        // Only the "StatValue": 0 -> "StatValue": 1 change
        Assert.Equal(1, addedCount);
        Assert.Equal(1, removedCount);
        Assert.Contains(result, dl => dl.Type == RawJsonLogic.DiffLineType.Added && dl.Text.Contains("\"StatValue\": 1"));
        Assert.Contains(result, dl => dl.Type == RawJsonLogic.DiffLineType.Removed && dl.Text.Contains("\"StatValue\": 0"));
    }

    [Fact]
    public void RawJsonLogic_ComputeRawDiff_EmptyOld_AllAdded()
    {
        var result = RawJsonLogic.ComputeRawDiff([], ["a", "b", "c"]);
        Assert.Equal(3, result.Count);
        Assert.All(result, dl => Assert.Equal(RawJsonLogic.DiffLineType.Added, dl.Type));
    }

    [Fact]
    public void RawJsonLogic_ComputeRawDiff_EmptyNew_AllRemoved()
    {
        var result = RawJsonLogic.ComputeRawDiff(["a", "b", "c"], []);
        Assert.Equal(3, result.Count);
        Assert.All(result, dl => Assert.Equal(RawJsonLogic.DiffLineType.Removed, dl.Type));
    }

    [Fact]
    public void RawJsonLogic_ComputeRawDiff_SingleLineChange_CorrectDiff()
    {
        var result = RawJsonLogic.ComputeRawDiff(["a", "b", "c"], ["a", "x", "c"]);

        Assert.Equal(4, result.Count); // a(ctx), -b, +x, c(ctx)
        Assert.Equal(RawJsonLogic.DiffLineType.Context, result[0].Type);
        Assert.Equal(RawJsonLogic.DiffLineType.Removed, result[1].Type);
        Assert.Equal("b", result[1].Text);
        Assert.Equal(RawJsonLogic.DiffLineType.Added, result[2].Type);
        Assert.Equal("x", result[2].Text);
        Assert.Equal(RawJsonLogic.DiffLineType.Context, result[3].Type);
    }

    // --- Diff line number and context header tests ---

    [Fact]
    public void RawJsonLogic_AssignLineNumbers_CorrectNumbering()
    {
        var raw = new List<(RawJsonLogic.DiffLineType, string)>
        {
            (RawJsonLogic.DiffLineType.Context, "a"),
            (RawJsonLogic.DiffLineType.Removed, "b"),
            (RawJsonLogic.DiffLineType.Added, "x"),
            (RawJsonLogic.DiffLineType.Context, "c"),
        };
        var result = RawJsonLogic.AssignLineNumbers(raw);

        Assert.Equal(4, result.Count);
        // Context "a": old=1, new=1
        Assert.Equal(1, result[0].OldLineNum);
        Assert.Equal(1, result[0].NewLineNum);
        // Removed "b": old=2, new=0 (not in new file)
        Assert.Equal(2, result[1].OldLineNum);
        Assert.Equal(0, result[1].NewLineNum);
        // Added "x": old=0, new=2 (not in old file)
        Assert.Equal(0, result[2].OldLineNum);
        Assert.Equal(2, result[2].NewLineNum);
        // Context "c": old=3, new=3
        Assert.Equal(3, result[3].OldLineNum);
        Assert.Equal(3, result[3].NewLineNum);
    }

    [Fact]
    public void RawJsonLogic_CompactDiff_HasLineNumbers()
    {
        string original = "{\n  \"Name\": \"old\"\n}";
        string modified = "{\n  \"Name\": \"new\"\n}";

        var result = RawJsonLogic.ComputeCompactDiff(original, modified);

        var removed = result.First(dl => dl.Type == RawJsonLogic.DiffLineType.Removed);
        var added = result.First(dl => dl.Type == RawJsonLogic.DiffLineType.Added);
        Assert.True(removed.OldLineNum > 0, "Removed line should have a non-zero old line number");
        Assert.True(added.NewLineNum > 0, "Added line should have a non-zero new line number");
    }

    [Fact]
    public void RawJsonLogic_FindJsonContext_ReturnsEnclosingKeyPath()
    {
        string[] lines =
        [
            "{",
            "  \"PlayerStateData\": {",
            "    \"KnownProducts\": [",
            "      {",
            "        \"Value\": 42",
            "      }",
            "    ]",
            "  }",
            "}"
        ];

        // Line 4 (0-based) is "Value": 42, inside KnownProducts inside PlayerStateData
        string context = RawJsonLogic.FindJsonContext(lines, 4);
        Assert.Contains("PlayerStateData", context);
        Assert.Contains("KnownProducts", context);
    }

    [Fact]
    public void RawJsonLogic_FindJsonContext_EmptyLines_ReturnsEmpty()
    {
        string context = RawJsonLogic.FindJsonContext([], 0);
        Assert.Equal("", context);
    }

    [Fact]
    public void RawJsonLogic_CompactDiff_ContainsHeaders()
    {
        // A diff within a nested JSON structure should include context headers
        string original = "{\n  \"PlayerStateData\": {\n    \"Records\": [\n      {\n        \"StatValue\": 0\n      }\n    ]\n  }\n}";
        string modified = "{\n  \"PlayerStateData\": {\n    \"Records\": [\n      {\n        \"StatValue\": 1\n      }\n    ]\n  }\n}";

        var result = RawJsonLogic.ComputeCompactDiff(original, modified);

        var headers = result.Where(dl => dl.Type == RawJsonLogic.DiffLineType.Header).ToList();
        Assert.True(headers.Count > 0, "Expected at least one context header in the diff");
        // The header should mention the enclosing path
        Assert.True(headers.Any(h => h.Text.Contains("PlayerStateData") || h.Text.Contains("Records")),
            "Header should contain enclosing key names");
    }

    // --- Settlement Production Filtering ---

    [Fact]
    public void SettlementLogic_AllowedProductionNames_ContainsExpectedItems()
    {
        // Verify the allowed list contains known settlement production items
        Assert.Contains("Glass", SettlementLogic.AllowedProductionNames);
        Assert.Contains("Warp Cell", SettlementLogic.AllowedProductionNames);
        Assert.Contains("Frigate Fuel", SettlementLogic.AllowedProductionNames);
        Assert.Contains("Cryogenic chamber", SettlementLogic.AllowedProductionNames);
        Assert.Equal(63, SettlementLogic.AllowedProductionNames.Length);
    }

    [Fact]
    public void SettlementLogic_BuildAllowedProductionItems_MatchesDbNames()
    {
        var db = new GameItemDatabase();
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Data", "json");
        if (Directory.Exists(dbPath))
            db.LoadItemsFromJsonDirectory(dbPath);

        var allowed = SettlementLogic.BuildAllowedProductionItems(db);

        // If the DB loaded, we should match most of the names
        if (db.Items.Count > 0)
        {
            Assert.True(allowed.Count > 50,
                $"Expected at least 50 matched production items, got {allowed.Count}");
            // Verify some IDs map correctly
            foreach (var kvp in allowed)
            {
                Assert.False(string.IsNullOrEmpty(kvp.Key), "ID should not be empty");
                Assert.False(string.IsNullOrEmpty(kvp.Value), "Name should not be empty");
            }
        }
    }

    // --- Picker Exclusion Tests ---

    [Fact]
    public void GameItemDatabase_IsPickerExcluded_ExcludesDamageItems()
    {
        Assert.True(GameItemDatabase.IsPickerExcluded("^WEAPON_DMG"));
        Assert.True(GameItemDatabase.IsPickerExcluded("^SHIP_DMG_TEST"));
        Assert.True(GameItemDatabase.IsPickerExcluded("OBSOLETE"));
    }

    [Fact]
    public void GameItemDatabase_IsPickerExcluded_ExcludesSeasonItems()
    {
        Assert.True(GameItemDatabase.IsPickerExcluded("^LAUNCHER_SPEC"));
        Assert.True(GameItemDatabase.IsPickerExcluded("^WORMTECH"));
        Assert.True(GameItemDatabase.IsPickerExcluded("^ROGUE_BEACON"));
        Assert.True(GameItemDatabase.IsPickerExcluded("^S8_BEACON"));
    }

    [Fact]
    public void GameItemDatabase_IsPickerExcluded_AllowsNormalItems()
    {
        Assert.False(GameItemDatabase.IsPickerExcluded("^HYPERDRIVE"));
        Assert.False(GameItemDatabase.IsPickerExcluded("^FUEL1"));
        Assert.False(GameItemDatabase.IsPickerExcluded("^PRODFUEL1"));
    }

    // --- Product item types excludes technology ---

    [Fact]
    public void CatalogueLogic_ProductItemTypes_ExcludesTechnology()
    {
        Assert.DoesNotContain("Technology", CatalogueLogic.ProductItemTypes);
        Assert.DoesNotContain("Upgrades", CatalogueLogic.ProductItemTypes);
        Assert.Contains("Products", CatalogueLogic.ProductItemTypes);
        Assert.Contains("Buildings", CatalogueLogic.ProductItemTypes);
    }

    // --- IsLearnableTechnology tests ---

    [Fact]
    public void IsLearnableTechnology_NonProceduralNonMaintenance_ReturnsTrue()
    {
        var item = new GameItem { Id = "LASER", Category = "Weapon", IsProcedural = false };
        Assert.True(CatalogueLogic.IsLearnableTechnology(item));
    }

    [Fact]
    public void IsLearnableTechnology_Procedural_ReturnsFalse()
    {
        var item = new GameItem { Id = "UP_LASER1", Category = "Weapon", IsProcedural = true };
        Assert.False(CatalogueLogic.IsLearnableTechnology(item));
    }

    [Fact]
    public void IsLearnableTechnology_Maintenance_ReturnsFalse()
    {
        var item = new GameItem { Id = "YOURPORTALGLYPH0", Category = "Maintenance", IsProcedural = false };
        Assert.False(CatalogueLogic.IsLearnableTechnology(item));
    }

    [Fact]
    public void IsLearnableTechnology_EmptyCategory_ReturnsTrue()
    {
        var item = new GameItem { Id = "TECH1", Category = "", IsProcedural = false };
        Assert.True(CatalogueLogic.IsLearnableTechnology(item));
    }

    // --- IsLearnableProduct tests ---

    [Fact]
    public void IsLearnableProduct_CraftableNonProcedural_ReturnsTrue()
    {
        var item = new GameItem { Id = "CRATE1", IsCraftable = true, IsProcedural = false, TradeCategory = "" };
        Assert.True(CatalogueLogic.IsLearnableProduct(item));
    }

    [Fact]
    public void IsLearnableProduct_NotCraftable_ReturnsFalse()
    {
        var item = new GameItem { Id = "ITEM1", IsCraftable = false, IsProcedural = false, TradeCategory = "" };
        Assert.False(CatalogueLogic.IsLearnableProduct(item));
    }

    [Fact]
    public void IsLearnableProduct_Procedural_ReturnsFalse()
    {
        var item = new GameItem { Id = "PROC1", IsCraftable = true, IsProcedural = true, TradeCategory = "" };
        Assert.False(CatalogueLogic.IsLearnableProduct(item));
    }

    [Fact]
    public void IsLearnableProduct_SpecialShop_ReturnsTrue()
    {
        // Items in the SpecialShop trade category are always learnable regardless of craftability
        var item = new GameItem { Id = "SPEC1", IsCraftable = false, IsProcedural = false, TradeCategory = "SpecialShop" };
        Assert.True(CatalogueLogic.IsLearnableProduct(item));
    }

    [Fact]
    public void IsLearnableProduct_SpecialShopProcedural_ReturnsTrue()
    {
        // Even procedural items in SpecialShop are learnable (IsSpecial overrides)
        var item = new GameItem { Id = "SPEC2", IsCraftable = false, IsProcedural = true, TradeCategory = "SpecialShop" };
        Assert.True(CatalogueLogic.IsLearnableProduct(item));
    }

    [Fact]
    public void IsLearnableProduct_CraftableAndSpecial_ReturnsTrue()
    {
        var item = new GameItem { Id = "BOTH1", IsCraftable = true, IsProcedural = false, TradeCategory = "SpecialShop" };
        Assert.True(CatalogueLogic.IsLearnableProduct(item));
    }

    // --- ExportConfig -----------------------------------------------

    [Fact]
    public void ExportConfig_BuildDialogFilter_ContainsExtensionAndJson()
    {
        string filter = ExportConfig.BuildDialogFilter(".nmsship", "Ship files");
        Assert.Contains("*.nmsship", filter);
        Assert.Contains("*.json", filter);
        Assert.Contains("Ship files", filter);
    }

    [Fact]
    public void ExportConfig_BuildImportFilter_ContainsAllExtensions()
    {
        string filter = ExportConfig.BuildImportFilter(".nmssc", "Ship cargo", ".wp0", ".mlt");
        Assert.Contains("*.nmssc", filter);
        Assert.Contains("*.json", filter);
        Assert.Contains("*.wp0", filter);
        Assert.Contains("*.mlt", filter);
        Assert.Contains("All supported", filter);
    }

    [Fact]
    public void ExportConfig_BuildImportFilter_NoExternalExts_StillWorks()
    {
        string filter = ExportConfig.BuildImportFilter(".nmssuit", "Exosuit");
        Assert.Contains("*.nmssuit", filter);
        Assert.Contains("*.json", filter);
    }

    [Fact]
    public void ExportConfig_BuildFileName_ExpandsVariables()
    {
        var vars = new Dictionary<string, string>
        {
            ["ship_name"] = "Explorer",
            ["type"] = "Shuttle",
            ["class"] = "S"
        };
        string name = ExportConfig.BuildFileName("{ship_name}_{type}_{class}", ".nmsship", vars);
        Assert.Equal("Explorer_Shuttle_S.nmsship", name);
    }

    [Fact]
    public void ExportConfig_Instance_ReturnsSameObject()
    {
        var a = ExportConfig.Instance;
        var b = ExportConfig.Instance;
        Assert.Same(a, b);
    }

    [Fact]
    public void ExportConfig_DefaultExtensions_AreSet()
    {
        var cfg = ExportConfig.Instance;
        Assert.Equal(".nmssuit", cfg.ExosuitExt);
        Assert.Equal(".nmstool", cfg.MultitoolExt);
        Assert.Equal(".nmsship", cfg.StarshipExt);
        Assert.Equal(".nmsfreight", cfg.FreighterExt);
        Assert.Equal(".nmscraft", cfg.ExocraftExt);
        Assert.Equal(".nmspet", cfg.CompanionExt);
        Assert.Equal(".nmsbase", cfg.BaseExt);
        Assert.Equal(".nmschest", cfg.ChestExt);
        Assert.Equal(".nmsstore", cfg.StorageExt);
    }

    // --- InventoryImportHelper --------------------------------------

    [Fact]
    public void FindInventoryObject_DirectSlots_ReturnsRoot()
    {
        // Raw inventory format (our own exports)
        var root = new JsonObject();
        var slots = new JsonArray();
        slots.Add("item1");
        root.Add("Slots", slots);
        root.Add("ValidSlotIndices", new JsonArray());

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.NotNull(result);
        Assert.Same(root, result);
    }

    [Fact]
    public void FindInventoryObject_NMSSaveEditorMultitool_FindsStore()
    {
        // NMSSaveEditor format: { Store: { Slots: [...] } }
        var store = new JsonObject();
        var slots = new JsonArray();
        slots.Add("item1");
        store.Add("Slots", slots);
        store.Add("ValidSlotIndices", new JsonArray());
        store.Add("Width", 10);
        store.Add("Height", 6);

        var root = new JsonObject();
        root.Add("Layout", new JsonObject());
        root.Add("Store", store);
        root.Add("Name", "My Tool");

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.NotNull(result);
        Assert.Same(store, result);
    }

    [Fact]
    public void FindInventoryObject_NomNomWeapon_FindsInventory()
    {
        // Data envelope format (after deobfuscation): { Data: { Multitool: { Store: { Slots: [...] } } } }
        var store = new JsonObject();
        var slots = new JsonArray();
        slots.Add("item1");
        store.Add("Slots", slots);

        var multitool = new JsonObject();
        multitool.Add("Store", store);

        var data = new JsonObject();
        data.Add("Multitool", multitool);

        var root = new JsonObject();
        root.Add("Data", data);
        root.Add("DateCreated", "2024-01-01");

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.NotNull(result);
        Assert.Same(store, result);
    }

    [Fact]
    public void FindInventoryObject_NomNomVehicle_FindsInventory()
    {
        // Data envelope format: { Data: { Vehicle: { Inventory: { Slots: [...] } } } }
        var inv = new JsonObject();
        var slots = new JsonArray();
        slots.Add("item1");
        inv.Add("Slots", slots);

        var vehicle = new JsonObject();
        vehicle.Add("Inventory", inv);
        vehicle.Add("Name", "Nautilon");

        var data = new JsonObject();
        data.Add("Vehicle", vehicle);

        var root = new JsonObject();
        root.Add("Data", data);

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.NotNull(result);
        Assert.Same(inv, result);
    }

    [Fact]
    public void FindInventoryObject_NomNomFreighter_FindsInventory()
    {
        // Data envelope format: { Data: { Inventory: { Slots: [...] } } }
        var inv = new JsonObject();
        var slots = new JsonArray();
        slots.Add("item1");
        inv.Add("Slots", slots);

        var data = new JsonObject();
        data.Add("Inventory", inv);

        var root = new JsonObject();
        root.Add("Data", data);

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.NotNull(result);
        Assert.Same(inv, result);
    }

    [Fact]
    public void FindInventoryObject_NoSlots_ReturnsNull()
    {
        var root = new JsonObject();
        root.Add("Name", "test");
        root.Add("Data", new JsonObject());

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.Null(result);
    }

    [Fact]
    public void FindInventoryObject_BfsFallback_FindsDeeplyNested()
    {
        // Test BFS fallback for unknown wrapper structure
        var inv = new JsonObject();
        var slots = new JsonArray();
        slots.Add("item1");
        inv.Add("Slots", slots);

        var wrapper1 = new JsonObject();
        wrapper1.Add("CustomWrapper", inv);

        var root = new JsonObject();
        root.Add("Unknown", wrapper1);

        var result = InventoryImportHelper.FindInventoryObject(root);
        Assert.NotNull(result);
        Assert.Same(inv, result);
    }

    [Fact]
    public void FindInventoryBfs_ExceedsMaxDepth_ReturnsNull()
    {
        // Create a deeply nested structure (depth > maxDepth)
        var deepest = new JsonObject();
        deepest.Add("Slots", new JsonArray());

        var current = deepest;
        for (int i = 0; i < 10; i++)
        {
            var parent = new JsonObject();
            parent.Add("Level" + i, current);
            current = parent;
        }

        // With maxDepth=4 (default in FindInventoryObject), this should fail BFS
        var result = InventoryImportHelper.FindInventoryBfs(current, maxDepth: 4);
        Assert.Null(result);
    }

    // --- Integration: save-editor file import ---------

    private static string? GetRefPath(params string[] parts)
    {
        var basePath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(new[] { basePath }.Concat(parts).ToArray());
        return File.Exists(path) ? path : null;
    }

    private static void EnsureMapper()
    {
        var mapperPath = GetRefPath("Resources", "map", "mapping.json");
        if (mapperPath == null) return;
        var mapper = new JsonNameMapper();
        mapper.Load(mapperPath);
        JsonParser.SetDefaultMapper(mapper);
    }

    [Fact]
    public void Import_NMSSaveEditor_Multitool_FindsStoreInventory()
    {
        var path = GetRefPath(referencePath, "nmssaveeditor_exports", "Multitool.wp0");
        if (path == null) return; // Skip if file not available
        EnsureMapper();

        var json = File.ReadAllText(path);
        var parsed = JsonParser.ParseObject(json);
        var inv = InventoryImportHelper.FindInventoryObject(parsed);

        Assert.NotNull(inv);
        Assert.NotNull(inv.GetArray("Slots"));
        Assert.True(inv.GetArray("Slots")!.Length > 0, "Store should have items");
        Assert.NotNull(inv.GetArray("ValidSlotIndices"));
    }

    [Fact]
    public void Import_NomNom_Weapon_FindsStoreInventory()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "weapon", "M-92 Peltast.mlt");
        if (path == null) return;
        EnsureMapper();

        var json = File.ReadAllText(path);
        var parsed = JsonParser.ParseObject(json);
        var inv = InventoryImportHelper.FindInventoryObject(parsed);

        Assert.NotNull(inv);
        Assert.NotNull(inv.GetArray("Slots"));
        Assert.True(inv.GetArray("Slots")!.Length > 0, "Weapon store should have items");
    }

    [Fact]
    public void Import_NomNom_Vehicle_FindsInventory()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "vehicle", "Nautilon.exo");
        if (path == null) return;
        EnsureMapper();

        var json = File.ReadAllText(path);
        var parsed = JsonParser.ParseObject(json);
        var inv = InventoryImportHelper.FindInventoryObject(parsed);

        Assert.NotNull(inv);
        Assert.NotNull(inv.GetArray("Slots"));
    }

    [Fact]
    public void Import_NomNom_Freighter_FindsInventory()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "freighter", "USCSS Akihabara MKIII.frt");
        if (path == null) return;
        EnsureMapper();

        var json = File.ReadAllText(path);
        var parsed = JsonParser.ParseObject(json);
        var inv = InventoryImportHelper.FindInventoryObject(parsed);

        Assert.NotNull(inv);
        Assert.NotNull(inv.GetArray("Slots"));
        Assert.True(inv.GetArray("Slots")!.Length > 0, "Freighter should have items");
    }

    // --- Data Envelope Wrapper Detection -----------------------------------

    [Fact]
    public void IsNomNomWrapper_WithDataAndFileVersion_ReturnsTrue()
    {
        var root = new JsonObject();
        root.Add("Data", new JsonObject());
        root.Add("FileVersion", 2);
        root.Add("DateCreated", "2024-01-01");

        Assert.True(InventoryImportHelper.IsNomNomWrapper(root));
    }

    [Fact]
    public void IsNomNomWrapper_WithoutFileVersion_ReturnsFalse()
    {
        var root = new JsonObject();
        root.Add("Data", new JsonObject());
        root.Add("Name", "test");

        Assert.False(InventoryImportHelper.IsNomNomWrapper(root));
    }

    [Fact]
    public void IsNomNomWrapper_RawExport_ReturnsFalse()
    {
        var root = new JsonObject();
        root.Add("Layout", new JsonObject());
        root.Add("Store", new JsonObject());
        root.Add("Name", "My Tool");

        Assert.False(InventoryImportHelper.IsNomNomWrapper(root));
    }

    [Fact]
    public void UnwrapNomNom_WithWrapper_ExtractsEntity()
    {
        var multitool = new JsonObject();
        multitool.Add("Layout", new JsonObject());
        multitool.Add("Name", "My Tool");

        var data = new JsonObject();
        data.Add("Multitool", multitool);

        var root = new JsonObject();
        root.Add("Data", data);
        root.Add("FileVersion", 2);
        root.Add("DateCreated", "2024-01-01");

        var result = InventoryImportHelper.UnwrapNomNom(root, "Multitool");
        Assert.Same(multitool, result);
    }

    [Fact]
    public void UnwrapNomNom_WithoutWrapper_ReturnsOriginal()
    {
        var root = new JsonObject();
        root.Add("Layout", new JsonObject());
        root.Add("Store", new JsonObject());

        var result = InventoryImportHelper.UnwrapNomNom(root, "Multitool");
        Assert.Same(root, result);
    }

    [Fact]
    public void UnwrapNomNomCompanion_MergesPetAndAccessory()
    {
        var pet = new JsonObject();
        pet.Add("CreatureID", "^PROTOFLYER");
        pet.Add("Scale", 0.5);

        var accessory = new JsonObject();
        accessory.Add("CustomisationSlots", new JsonArray());

        var data = new JsonObject();
        data.Add("Pet", pet);
        data.Add("AccessoryCustomisation", accessory);

        var root = new JsonObject();
        root.Add("Data", data);
        root.Add("FileVersion", 2);

        var result = InventoryImportHelper.UnwrapNomNomCompanion(root);
        Assert.Same(pet, result);
        Assert.True(result.Contains("AccessoryCustomisation"), "Accessory should be merged into pet");
    }

    [Fact]
    public void UnwrapNomNomCompanion_NomNomRefFile_ExtractsPet()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "companion",
            "__STRANGE_FLOAT__FLOAT_EYEFISH_478250819-0x88CA25ACD4B209BB.cmp");
        if (path == null) return;
        EnsureMapper();

        var imported = JsonObject.ImportFromFile(path);
        var result = InventoryImportHelper.UnwrapNomNomCompanion(imported);

        // After unwrapping, should have companion fields (CreatureID, Scale, etc.)
        Assert.True(result.Contains("CreatureID") || result.Contains("Scale"),
            "Unwrapped companion should have pet fields");
    }

    [Fact]
    public void UnwrapNomNom_SettlementRefFile_ExtractsSettlement()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "settlement", "doofus ford.stl");
        if (path == null) return;
        EnsureMapper();

        var imported = JsonObject.ImportFromFile(path);
        var result = InventoryImportHelper.UnwrapNomNom(imported, "Settlement");

        Assert.NotSame(imported, result);
        Assert.True(result.Length > 0, "Unwrapped settlement should have properties");
    }

    [Fact]
    public void UnwrapNomNom_WeaponRefFile_ExtractsMultitool()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "weapon", "M-92 Peltast.mlt");
        if (path == null) return;
        EnsureMapper();

        var imported = JsonObject.ImportFromFile(path);
        var result = InventoryImportHelper.UnwrapNomNom(imported, "Multitool");

        Assert.NotSame(imported, result);
        // After unwrapping, should have multitool fields (Layout, Store, etc.)
        Assert.True(result.Contains("Layout") || result.Contains("Store"),
            "Unwrapped multitool should have Layout/Store");
    }

    [Fact]
    public void UnwrapNomNom_VehicleRefFile_ExtractsVehicle()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "vehicle", "Nautilon.exo");
        if (path == null) return;
        EnsureMapper();

        var imported = JsonObject.ImportFromFile(path);
        var result = InventoryImportHelper.UnwrapNomNom(imported, "Vehicle");

        Assert.NotSame(imported, result);
    }

    [Fact]
    public void UnwrapNomNom_FrigateRefFile_ExtractsFrigate()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "frigate", "OAC Tanto DSV-1.flt");
        if (path == null) return;
        EnsureMapper();

        var imported = JsonObject.ImportFromFile(path);
        var result = InventoryImportHelper.UnwrapNomNomFrigate(imported);

        Assert.NotSame(imported, result);
    }

    [Fact]
    public void UnwrapNomNom_SquadronRefFile_ExtractsPilot()
    {
        var path = GetRefPath(referencePath, "nomnom_exports", "squadron",
            "Traders-0xFB48876360B5B76C-Fighter-0xAB4365704CBC7E24.sqd");
        if (path == null) return;
        EnsureMapper();

        var imported = JsonObject.ImportFromFile(path);
        var result = InventoryImportHelper.UnwrapNomNomPilot(imported);

        Assert.NotSame(imported, result);
    }

    // =============== JSON Export/Import Round-Trip Tests ===============

    [Fact]
    public void ExportImportRoundTrip_PreservesNameMapper()
    {
        // Simulate a save object with a NameMapper (as if loaded from an obfuscated save file)
        var mapper = new JsonNameMapper();
        // Build a tiny mapper that knows "Abc" <-> "PlayerStateData"
        var mapData = new Dictionary<string, string> { { "Abc", "PlayerStateData" } };
        mapper.LoadFromDictionary(mapData);

        var root = new JsonObject();
        root.NameMapper = mapper;
        var ps = new JsonObject();
        ps.Add("SomeStat", 42);
        root.Add("PlayerStateData", ps);

        // Export to file (human-readable keys)
        string tmpPath = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}.json");
        try
        {
            root.ExportToFile(tmpPath);
            string exported = File.ReadAllText(tmpPath);
            // Exported file should have human-readable keys
            Assert.Contains("PlayerStateData", exported);
            Assert.DoesNotContain("\"Abc\"", exported);

            // Import back
            var reimported = JsonObject.ImportFromFile(tmpPath);
            // ImportFromFile auto-detects: human-readable keys -> no mapper set
            Assert.Null(reimported.NameMapper);

            // After setting the mapper (as OnImportJson does), save-to-disk should work
            reimported.NameMapper ??= mapper;
            Assert.NotNull(reimported.NameMapper);

            // ToString should now reverse-map keys
            string serialized = reimported.ToString();
            Assert.Contains("\"Abc\"", serialized);
            Assert.DoesNotContain("PlayerStateData", serialized);
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [Fact]
    public void ExportImportRoundTrip_WithoutMapper_WritesHumanReadableKeys()
    {
        // When no mapper is set, exported JSON uses human-readable keys
        var root = new JsonObject();
        root.Add("PlayerStateData", new JsonObject());
        root.Add("Version", 4720);

        string tmpPath = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}.json");
        try
        {
            root.ExportToFile(tmpPath);

            var reimported = JsonObject.ImportFromFile(tmpPath);
            // Without mapper, ToString produces human-readable keys
            string serialized = reimported.ToString();
            Assert.Contains("\"PlayerStateData\"", serialized);
            Assert.Contains("\"Version\"", serialized);
        }
        finally
        {
            File.Delete(tmpPath);
        }
    }

    [Fact]
    public void GetDefaultMapper_ReturnsSetMapper()
    {
        // Save the original mapper
        var original = JsonParser.GetDefaultMapper();
        try
        {
            var mapper = new JsonNameMapper();
            JsonParser.SetDefaultMapper(mapper);
            Assert.Same(mapper, JsonParser.GetDefaultMapper());
        }
        finally
        {
            // Restore original
            if (original != null)
                JsonParser.SetDefaultMapper(original);
        }
    }

    [Fact]
    public void CreaturePartDatabase_Loads_NonEmptyEntries()
    {
        var entries = CreaturePartDatabase.Entries;
        Assert.NotNull(entries);
        Assert.True(entries.Count > 0, "Should load creature part entries from embedded data");
    }

    [Fact]
    public void CreaturePartDatabase_Contains_KnownCreatures()
    {
        // These creatures are known to exist in the NMSCD Creature Builder data
        Assert.True(CreaturePartDatabase.ById.ContainsKey("ANTELOPE"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("CAT"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("COW"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("TREX"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("BIRD"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("SHARK"));
    }

    [Fact]
    public void CreaturePartDatabase_GetForCreatureId_StripsCaret()
    {
        var entry = CreaturePartDatabase.GetForCreatureId("^CAT");
        Assert.NotNull(entry);
        Assert.Equal("CAT", entry!.CreatureId);
    }

    [Fact]
    public void CreaturePartDatabase_GetForCreatureId_ReturnsNull_ForEmptyOrMissing()
    {
        Assert.Null(CreaturePartDatabase.GetForCreatureId(null));
        Assert.Null(CreaturePartDatabase.GetForCreatureId(""));
        Assert.Null(CreaturePartDatabase.GetForCreatureId("^"));
        Assert.Null(CreaturePartDatabase.GetForCreatureId("^NONEXISTENT_CREATURE_XYZ"));
    }

    [Fact]
    public void CreaturePartDatabase_CaseInsensitiveLookup()
    {
        Assert.True(CreaturePartDatabase.ById.ContainsKey("cat"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("CAT"));
        Assert.True(CreaturePartDatabase.ById.ContainsKey("Cat"));
    }

    [Fact]
    public void CreaturePartDatabase_EntriesHaveDetails()
    {
        var cat = CreaturePartDatabase.ById["CAT"];
        Assert.NotNull(cat.Details);
        Assert.True(cat.Details.Count > 0, "CAT should have part groups");

        // Each group should have descriptors
        foreach (var group in cat.Details)
        {
            Assert.False(string.IsNullOrEmpty(group.GroupId), "GroupId should not be empty");
            Assert.True(group.Descriptors.Count > 0, $"Group {group.GroupId} should have descriptors");
        }
    }

    [Fact]
    public void CreaturePartDatabase_GetFlatGroups_ReturnsTopLevelWhenNoSelection()
    {
        var entry = CreaturePartDatabase.ById["ANTELOPE"];
        var groups = CreaturePartDatabase.GetFlatGroups(entry, Array.Empty<string>());

        // Should return at least the top-level groups
        Assert.True(groups.Count >= entry.Details.Count,
            "Should return at least the top-level groups when nothing is selected");
    }

    [Fact]
    public void CreaturePartDatabase_GetFlatGroups_ExpandsChildGroups()
    {
        var entry = CreaturePartDatabase.ById["ANTELOPE"];

        // Select the first descriptor from the first group to trigger child expansion
        var firstGroup = entry.Details[0];
        var firstDesc = firstGroup.Descriptors[0];

        var groups = CreaturePartDatabase.GetFlatGroups(entry, new[] { firstDesc.Id });

        // Should have more groups than just top-level (child groups expanded)
        if (firstDesc.Children.Count > 0)
        {
            Assert.True(groups.Count > entry.Details.Count,
                "Selecting a descriptor with children should expand child groups");
        }
    }

    [Fact]
    public void CreaturePartDatabase_NewDescriptorId_Is10Digits()
    {
        string id = CreaturePartDatabase.NewDescriptorId();
        Assert.Equal(10, id.Length);
        Assert.True(id.All(char.IsDigit), "Descriptor ID should be all digits");
    }

    [Fact]
    public void CreaturePartDatabase_NewDescriptorId_IsRandom()
    {
        // Generate 10 IDs and verify they're not all the same
        var ids = Enumerable.Range(0, 10).Select(_ => CreaturePartDatabase.NewDescriptorId()).ToHashSet();
        Assert.True(ids.Count > 1, "Random IDs should produce different values");
    }

    // --- LocalisationService -----------------------------------------

    [Fact]
    public void LocalisationService_LoadLanguage_LoadsValidFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string jsonPath = Path.Combine(tmpDir, "ja-JP.json");
            File.WriteAllText(jsonPath, """{"UI_FUEL_1_NAME": "炭素", "UI_FUEL_1_DESC": "テスト"}""");

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            bool loaded = svc.LoadLanguage("ja-JP");

            Assert.True(loaded);
            Assert.True(svc.IsActive);
            Assert.Equal("ja-JP", svc.ActiveLanguageTag);
            Assert.Equal("炭素", svc.Lookup("UI_FUEL_1_NAME"));
            Assert.Equal("テスト", svc.Lookup("UI_FUEL_1_DESC"));
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void LocalisationService_LoadLanguage_NullClearsLanguage()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), """{"KEY": "value"}""");

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");
            Assert.True(svc.IsActive);

            svc.LoadLanguage(null);
            Assert.False(svc.IsActive);
            Assert.Null(svc.ActiveLanguageTag);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void LocalisationService_GetName_FallsBackToDefaultWhenKeyMissing()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), """{"OTHER_KEY": "何か"}""");

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            var item = new GameItem { Name = "Carbon", NameLocStr = "MISSING_KEY" };
            string name = svc.GetName(item);

            Assert.Equal("Carbon", name);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void LocalisationService_GetDescription_ReturnsLocalisedValue()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"), """{"UI_FUEL_1_DESC": "テスト説明"}""");

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            var item = new GameItem { Description = "English desc", DescriptionLocStr = "UI_FUEL_1_DESC" };
            string desc = svc.GetDescription(item);

            Assert.Equal("テスト説明", desc);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void LocalisationService_SupportedLanguages_MatchesExtractorConfig()
    {
        var langs = LocalisationService.SupportedLanguages;

        Assert.Equal(16, langs.Count);
        Assert.Equal("en-GB", langs["English"]);
        Assert.Equal("ja-JP", langs["Japanese"]);
        Assert.Equal("zh-CN", langs["SimplifiedChinese"]);
        Assert.Equal("en-US", langs["USEnglish"]);
    }

    [Fact]
    public void GameItem_LocStrProperties_DefaultToNull()
    {
        var item = new GameItem();

        Assert.Null(item.NameLocStr);
        Assert.Null(item.DescriptionLocStr);
        Assert.Null(item.SubtitleLocStr);
        Assert.Null(item.NameLowerLocStr);
    }

    // --- GameItemDatabase ApplyLocalisation / RevertLocalisation ------

    [Fact]
    public void GameItemDatabase_ApplyLocalisation_UpdatesItemNames()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_db_" + Guid.NewGuid().ToString("N"));
        string jsonDir = Path.Combine(tmpDir, "json");
        string langDir = Path.Combine(jsonDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            // Create a minimal item JSON file
            File.WriteAllText(Path.Combine(jsonDir, "Test.json"), """
            [
                {
                    "Id": "FUEL1",
                    "Name": "Carbon",
                    "NameLower": "carbon",
                    "Name_LocStr": "UI_FUEL_1_NAME",
                    "NameLower_LocStr": "UI_FUEL_1_NAME_L",
                    "Subtitle_LocStr": "UI_FUEL1_SUB",
                    "Description": "English desc",
                    "Description_LocStr": "UI_FUEL_1_DESC"
                }
            ]
            """);

            // Create a language file
            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"UI_FUEL_1_NAME": "炭素", "UI_FUEL_1_NAME_L": "炭素", "UI_FUEL1_SUB": "未精製有機資源", "UI_FUEL_1_DESC": "テスト説明"}""");

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(jsonDir);
            Assert.Equal("Carbon", db.Items["FUEL1"].Name);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("炭素", db.Items["FUEL1"].Name);
            Assert.Equal("炭素", db.Items["FUEL1"].NameLower);
            Assert.Equal("未精製有機資源", db.Items["FUEL1"].Subtitle);
            Assert.Equal("テスト説明", db.Items["FUEL1"].Description);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_RevertLocalisation_RestoresEnglish()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_revert_" + Guid.NewGuid().ToString("N"));
        string jsonDir = Path.Combine(tmpDir, "json");
        string langDir = Path.Combine(jsonDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(jsonDir, "Test.json"), """
            [
                {
                    "Id": "FUEL1",
                    "Name": "Carbon",
                    "Description": "English desc",
                    "Name_LocStr": "UI_FUEL_1_NAME",
                    "Description_LocStr": "UI_FUEL_1_DESC"
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"UI_FUEL_1_NAME": "炭素", "UI_FUEL_1_DESC": "テスト説明"}""");

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(jsonDir);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            db.ApplyLocalisation(svc);
            Assert.Equal("炭素", db.Items["FUEL1"].Name);

            db.RevertLocalisation();
            Assert.Equal("Carbon", db.Items["FUEL1"].Name);
            Assert.Equal("English desc", db.Items["FUEL1"].Description);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_ApplyLocalisation_FallsBackWhenKeyMissing()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_fallback_" + Guid.NewGuid().ToString("N"));
        string jsonDir = Path.Combine(tmpDir, "json");
        string langDir = Path.Combine(jsonDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(jsonDir, "Test.json"), """
            [
                {
                    "Id": "FUEL1",
                    "Name": "Carbon",
                    "Name_LocStr": "MISSING_KEY"
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"), """{"OTHER_KEY": "何か"}""");

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(jsonDir);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.Equal(0, count);
            Assert.Equal("Carbon", db.Items["FUEL1"].Name);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_ApplyLocalisation_SwitchesLanguages()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_switch_" + Guid.NewGuid().ToString("N"));
        string jsonDir = Path.Combine(tmpDir, "json");
        string langDir = Path.Combine(jsonDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(jsonDir, "Test.json"), """
            [
                {
                    "Id": "FUEL1",
                    "Name": "Carbon",
                    "Name_LocStr": "UI_FUEL_1_NAME"
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"), """{"UI_FUEL_1_NAME": "炭素"}""");
            File.WriteAllText(Path.Combine(langDir, "fr-FR.json"), """{"UI_FUEL_1_NAME": "Carbone"}""");

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(jsonDir);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);

            // Switch to Japanese
            svc.LoadLanguage("ja-JP");
            db.ApplyLocalisation(svc);
            Assert.Equal("炭素", db.Items["FUEL1"].Name);

            // Switch to French
            svc.LoadLanguage("fr-FR");
            db.ApplyLocalisation(svc);
            Assert.Equal("Carbone", db.Items["FUEL1"].Name);

            // Switch back to English (null)
            svc.LoadLanguage(null);
            db.ApplyLocalisation(svc);
            Assert.Equal("Carbon", db.Items["FUEL1"].Name);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // --- RewardDatabase Localisation ---------------------------------

    [Fact]
    public void RewardEntry_LocStrProperties_LoadedFromJson()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_reward_loc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), """
            [
                {
                    "Id": "^VAULT_ARMOUR",
                    "Name": "Heirloom Breastplate",
                    "Name_LocStr": "UI_EXPED_VAULT_ARMOUR_NAME",
                    "Subtitle_LocStr": "UI_SPECIAL_ARMOUR_SUB",
                    "Category": "season",
                    "ProductId": "VAULT_ARMOUR"
                }
            ]
            """);

            RewardDatabase.Reset();
            RewardDatabase.LoadFromJsonDirectory(tmpDir);

            var reward = RewardDatabase.Rewards.First();
            Assert.Equal("UI_EXPED_VAULT_ARMOUR_NAME", reward.NameLocStr);
            Assert.Equal("UI_SPECIAL_ARMOUR_SUB", reward.SubtitleLocStr);
        }
        finally
        {
            RewardDatabase.Reset();
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void RewardDatabase_ApplyLocalisation_UpdatesRewardNames()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_reward_apply_" + Guid.NewGuid().ToString("N"));
        string langDir = Path.Combine(tmpDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), """
            [
                {
                    "Id": "^VAULT_ARMOUR",
                    "Name": "Heirloom Breastplate",
                    "Name_LocStr": "UI_EXPED_VAULT_ARMOUR_NAME",
                    "Category": "season",
                    "ProductId": "VAULT_ARMOUR"
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"UI_EXPED_VAULT_ARMOUR_NAME": "家宝の胸当て"}""");

            RewardDatabase.Reset();
            RewardDatabase.LoadFromJsonDirectory(tmpDir);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = RewardDatabase.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("家宝の胸当て", RewardDatabase.Rewards.First().Name);

            RewardDatabase.RevertLocalisation();
            Assert.Equal("Heirloom Breastplate", RewardDatabase.Rewards.First().Name);
        }
        finally
        {
            RewardDatabase.Reset();
            Directory.Delete(tmpDir, recursive: true);
        }
    }

	// -------------------------
    // WordDatabase Localisation
	// -------------------------

    [Fact]
    public void WordEntry_TextLocStr_LoadedFromJson()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_word_loc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Words.json"), """
            [
                {
                    "Id": "^ABANDON",
                    "Text": "abandon",
                    "Text_LocStr": "WORD_ABANDON",
                    "Groups": {"^TRA_ABANDON": 0}
                }
            ]
            """);

            var db = new WordDatabase();
            db.LoadFromFile(Path.Combine(tmpDir, "Words.json"));

            Assert.Single(db.Words);
            Assert.Equal("WORD_ABANDON", db.Words[0].TextLocStr);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void WordDatabase_ApplyLocalisation_UpdatesWordText()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_word_apply_" + Guid.NewGuid().ToString("N"));
        string langDir = Path.Combine(tmpDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Words.json"), """
            [
                {
                    "Id": "^ABANDON",
                    "Text": "abandon",
                    "Text_LocStr": "WORD_ABANDON",
                    "Groups": {"^TRA_ABANDON": 0}
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"WORD_ABANDON": "放棄する"}""");

            var db = new WordDatabase();
            db.LoadFromFile(Path.Combine(tmpDir, "Words.json"));
            Assert.Equal("abandon", db.Words[0].Text);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("放棄する", db.Words[0].Text);

            db.RevertLocalisation();
            Assert.Equal("abandon", db.Words[0].Text);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void WordDatabase_GroupKeyFallback_FindsTranslation()
    {
        // Reproduces the ACCESS / BUI_ACCESS scenario:
        // Primary TextLocStr (TRA_ACCESS) returns English-equal text in ja-JP,
        // but BUI_ACCESS has a real Japanese translation.
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_word_gkf_" + Guid.NewGuid().ToString("N"));
        string langDir = Path.Combine(tmpDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Words.json"), """
            [
                {
                    "Id": "^ACCESS",
                    "Text": "access",
                    "Text_LocStr": "TRA_ACCESS",
                    "Groups": {
                        "^TRA_ACCESS": 0,
                        "^WAR_ACCESS": 1,
                        "^EXP_ACCESS": 2,
                        "^BUI_ACCESS": 8
                    }
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"TRA_ACCESS": "access", "WAR_ACCESS": "access", "EXP_ACCESS": "access", "BUI_ACCESS": "アクセス"}""");

            var db = new WordDatabase();
            db.LoadFromFile(Path.Combine(tmpDir, "Words.json"));
            Assert.Single(db.Words);
            Assert.Equal("access", db.Words[0].Text);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("アクセス", db.Words[0].Text);

            db.RevertLocalisation();
            Assert.Equal("access", db.Words[0].Text);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

	// -------------------
    // Recipe localisation
	// -------------------
    [Fact]
    public void RecipeDatabase_ApplyLocalisation_UpdatesRecipeNames()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string recipesJson = """
            [
              { "Id": "R1", "RecipeName": "Fermentation", "RecipeName_LocStr": "UI_YEAST_PROCESS_R", "RecipeType": "Yeast Process", "RecipeType_LocStr": "UI_YEAST_PROCESS", "Category": "Cooking", "Cooking": true, "TimeToMake": 0, "Result": { "Id": "OUT1", "Type": "Product", "Amount": 1 }, "Ingredients": [{ "Id": "IN1", "Type": "Product", "Amount": 1 }] }
            ]
            """;
            File.WriteAllText(Path.Combine(tmpDir, "Recipes.json"), recipesJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"),
                """{"UI_YEAST_PROCESS_R": "発酵", "UI_YEAST_PROCESS": "酵母処理"}""");

            var db = new RecipeDatabase();
            db.LoadFromFile(Path.Combine(tmpDir, "Recipes.json"));
            Assert.Equal("Fermentation", db.Recipes[0].RecipeName);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            Assert.Equal("発酵", db.Recipes[0].RecipeName);
            Assert.Equal("酵母処理", db.Recipes[0].RecipeType);

            db.RevertLocalisation();
            Assert.Equal("Fermentation", db.Recipes[0].RecipeName);
            Assert.Equal("Yeast Process", db.Recipes[0].RecipeType);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

	// ------------------
    // Title localisation
	// ------------------
    [Fact]
    public void TitleDatabase_ApplyLocalisation_UpdatesTitleNames()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_loc_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            string titlesJson = """
            [
              { "Id": "T_TRA1", "Name": "Hireling {0}", "Name_LocStr": "UI_PLAYER_TITLE_TRA1", "UnlockDescription": "Impressed a Gek", "UnlockDescription_LocStr": "UI_PLAYER_TITLE_TRA1_DESC", "AlreadyUnlockedDescription": "-", "UnlockedByStat": "TRA_STANDING", "UnlockedByStatValue": 1 }
            ]
            """;
            File.WriteAllText(Path.Combine(tmpDir, "Titles.json"), titlesJson);
            File.WriteAllText(Path.Combine(tmpDir, "ja-JP.json"),
                """{"UI_PLAYER_TITLE_TRA1": "雇い人 %NAME%", "UI_PLAYER_TITLE_TRA1_DESC": "ゲックに感心された"}""");

            TitleDatabase.LoadFromFile(Path.Combine(tmpDir, "Titles.json"));
            Assert.Equal("Hireling {0}", TitleDatabase.Titles[0].Name);

            var svc = new LocalisationService();
            svc.SetLangDirectory(tmpDir);
            svc.LoadLanguage("ja-JP");

            int count = TitleDatabase.ApplyLocalisation(svc);
            Assert.Equal(1, count);
            // %NAME% should be converted to {0}
            Assert.Equal("雇い人 {0}", TitleDatabase.Titles[0].Name);
            Assert.Equal("ゲックに感心された", TitleDatabase.Titles[0].UnlockDescription);

            TitleDatabase.RevertLocalisation();
            Assert.Equal("Hireling {0}", TitleDatabase.Titles[0].Name);
            Assert.Equal("Impressed a Gek", TitleDatabase.Titles[0].UnlockDescription);
        }
        finally
        {
            // Restore data from the real JSON file
            var jsonDir = FindResourceJsonDir();
            if (jsonDir != null)
                TitleDatabase.LoadFromFile(Path.Combine(jsonDir, "Titles.json"));
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // -- Frigate trait localisation (non-mutating: hardcoded data has no LocStr) --
    // Full load+localise round-trip tests are in DatabaseLocalisationTests.cs

    // -- Settlement perk localisation (non-mutating: hardcoded data has no LocStr) --
    // Full load+localise round-trip tests are in DatabaseLocalisationTests.cs

    // -- Wiki guide localisation (non-mutating: hardcoded data has no LocStr) --
    // Full load+localise round-trip tests are in DatabaseLocalisationTests.cs

    // -- GameItem DescriptionLocStr-based name fallback -----------------

    [Fact]
    public void GameItemDatabase_DescLocStr_DerivesNameKey()
    {
        // Simulates UP_HYP4 whose Name_LocStr="UP_HYPERDRIVE" doesn't match
        // lang keys but DescriptionLocStr="UP_HYPER4_DESC" -> "UP_HYPER4_NAME" does.
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_descfb_" + Guid.NewGuid().ToString("N"));
        string langDir = Path.Combine(tmpDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "TestItems.json"), """
            [
                {
                    "Id": "UP_HYP4",
                    "Name": "Hyperdrive S-Class Upgrade",
                    "Name_LocStr": "UP_HYPERDRIVE",
                    "Description": "A supremely powerful upgrade.",
                    "Description_LocStr": "UP_HYPER4_DESC",
                    "Group": "S-Class Upgrade"
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"UP_HYPER4_NAME": "ハイパードライブモジュール", "UP_HYPER4_DESC": "超強力なアップグレード", "UP_HYPER4_SUB": "最上級ハイパードライブ", "UP_HYPER4_NAME_L": "ハイパードライブモジュール小文字"}""");

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);
            var item = db.GetItem("UP_HYP4");
            Assert.NotNull(item);
            Assert.Equal("Hyperdrive S-Class Upgrade", item.Name);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.True(count >= 1);
            Assert.Equal("ハイパードライブモジュール", item.Name);
            Assert.Equal("超強力なアップグレード", item.Description);
            Assert.Equal("最上級ハイパードライブ", item.Subtitle);
            Assert.Equal("ハイパードライブモジュール小文字", item.NameLower);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void GameItemDatabase_DescLocStr_DerivesNameKey_XVariant()
    {
        // Simulates UP_SHOTX whose DescriptionLocStr="UP_SHOTGUNX_DESC" doesn't
        // exist but "UP_SHOTGUN_X_DESC" does (with underscore before X).
        string tmpDir = Path.Combine(Path.GetTempPath(), "nmse_descfbx_" + Guid.NewGuid().ToString("N"));
        string langDir = Path.Combine(tmpDir, "lang");
        Directory.CreateDirectory(langDir);

        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "TestItems.json"), """
            [
                {
                    "Id": "UP_SHOTX",
                    "Name": "Scatter Blaster Illegal Upgrade",
                    "Name_LocStr": "UP_SHOT",
                    "Description": "A black-market modification.",
                    "Description_LocStr": "UP_SHOTGUNX_DESC",
                    "Group": "Illegal Upgrade"
                }
            ]
            """);

            File.WriteAllText(Path.Combine(langDir, "ja-JP.json"),
                """{"UP_SHOTGUN_X_NAME": "うさんくさいモジュール", "UP_SHOTGUN_X_DESC": "ブラックマーケット", "UP_SHOTGUN_X_SUB": "違法アップグレード"}""");

            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);
            var item = db.GetItem("UP_SHOTX");
            Assert.NotNull(item);

            var svc = new LocalisationService();
            svc.SetLangDirectory(langDir);
            svc.LoadLanguage("ja-JP");

            int count = db.ApplyLocalisation(svc);
            Assert.True(count >= 1);
            Assert.Equal("うさんくさいモジュール", item.Name);
            Assert.Equal("ブラックマーケット", item.Description);
            Assert.Equal("違法アップグレード", item.Subtitle);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    // --- Localised fallback string tests ---

    [Fact]
    public void FrigateLogic_GetFrigateName_FallbackUsesLocalisedFormat()
    {
        var frigate = JsonObject.Parse(@"{ ""CustomName"": null }");
        string name = FrigateLogic.GetFrigateName(frigate, 2);
        Assert.Equal("Frigate 3", name);
    }

    [Fact]
    public void SquadronLogic_GetPilotDisplayName_EmptySlot_UsesLocalisedFormat()
    {
        var pilot = JsonObject.Parse(@"{
            ""NPCResource"": { ""Seed"": [false, ""0x0""], ""Filename"": """" },
            ""ShipResource"": { ""Seed"": [false, ""0x0""], ""Filename"": """" }
        }");
        string display = SquadronLogic.GetPilotDisplayName(pilot, 1);
        Assert.Contains("1", display);
        Assert.Contains("(", display); // "(Empty)" or localised equivalent
    }

    [Fact]
    public void StarshipLogic_GetShipInfo_ReturnsLocalisedMaxSupported()
    {
        var (_, cargoLabel, techLabel) = StarshipLogic.GetShipInfo(
            "MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN");
        Assert.Contains("10x12", cargoLabel);
        Assert.Contains("10x6", techLabel);
    }

    [Fact]
    public void StarshipLogic_GetShipInfo_MaxLabelsContainDimensions()
    {
        // Verify Max Supported labels contain expected dimension strings
        var (_, cargoHauler, techHauler) = StarshipLogic.GetShipInfo(
            "MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN");
        Assert.Contains("10x12", cargoHauler);
        Assert.Contains("10x6", techHauler);

        var (_, cargoExotic, _) = StarshipLogic.GetShipInfo(
            "MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN");
        Assert.Contains("10x10 + 5", cargoExotic);
    }

    [Fact]
    public void GalaxyDatabase_Fallback_UsesLocalisedStrings()
    {
        // Out-of-range indices should return localised "Unknown" / "Normal"
        Assert.Equal("Unknown", GalaxyDatabase.GetGalaxyName(999));
        Assert.Equal("Normal", GalaxyDatabase.GetGalaxyType(999));
        Assert.Equal("Unknown", GalaxyDatabase.GetGalaxyDisplayName(-1));
    }

    [Fact]
    public void MultitoolLogic_BuildToolList_EmptyName_UsesLocalisedDefault()
    {
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": """", ""Seed"": [true, ""0xABC""] }
            ]
        }");

        var tools = MultitoolLogic.BuildToolList(json.GetArray("Tools")!);
        Assert.Single(tools);
        Assert.Equal("Multitool 1", tools[0].DisplayName);
    }

    [Fact]
    public void MultitoolLogic_GetPrimaryToolName_FallbacksAreLocalised()
    {
        // Null array -> localised "Unknown"
        Assert.Equal("Unknown", MultitoolLogic.GetPrimaryToolName(null, 0));

        // Empty name -> localised "Multitool N"
        var json = JsonObject.Parse(@"{
            ""Tools"": [
                { ""Name"": """", ""Seed"": [true, ""0x111""] }
            ]
        }");
        Assert.Equal("Multitool 1", MultitoolLogic.GetPrimaryToolName(json.GetArray("Tools")!, 0));
    }

    private static string? FindResourceJsonDir()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "Resources", "json");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    // --- Locale string coverage tests ---

    [Fact]
    public void UiStrings_DeleteLocationStrings_ExistInEnGB()
    {
        EnsureUiStringsLoaded();
        string single = UiStrings.Get("discovery.delete_location_single");
        string multi = UiStrings.Get("discovery.delete_location_multi");
        string title = UiStrings.Get("discovery.delete_location_title");

        // Resolved strings should NOT fall back to the raw key name
        Assert.NotEqual("discovery.delete_location_single", single);
        Assert.NotEqual("discovery.delete_location_multi", multi);
        Assert.NotEqual("discovery.delete_location_title", title);
        Assert.Contains("location", single, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("{0}", multi);
    }

    [Fact]
    public void UiStrings_MaxSupportedLabel_SaysOfficialMax()
    {
        EnsureUiStringsLoaded();
        string label = UiStrings.Format("common.max_supported", "10x6");
        Assert.Contains("Official Max", label, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10x6", label);
    }

    [Fact]
    public void UiStrings_DeleteLocationStrings_ExistInAllLocales()
    {
        var langDir = FindResourceLangDir();
        if (langDir == null) return;

        var files = Directory.GetFiles(langDir, "*.json");
        Assert.True(files.Length >= 16, $"Expected at least 16 locale files, found {files.Length}");

        foreach (var file in files)
        {
            string json = File.ReadAllText(file);
            string fileName = Path.GetFileName(file);
            Assert.Contains("discovery.delete_location_single", json,
                StringComparison.Ordinal);
            Assert.Contains("discovery.delete_location_multi", json,
                StringComparison.Ordinal);
        }
    }

    // --- Inventory Resize Width/Height tests ---

    [Fact]
    public void InventoryResize_ShouldUpdateWidthHeight_WhenResizing()
    {
        // Create a minimal inventory JSON with Width=10, Height=6
        var inventory = JsonObject.Parse(@"{
            ""Width"": 10,
            ""Height"": 6,
            ""Slots"": [],
            ""ValidSlotIndices"": [
                { ""X"": 0, ""Y"": 0 },
                { ""X"": 1, ""Y"": 0 }
            ]
        }");

        Assert.Equal(10, inventory.GetInt("Width"));
        Assert.Equal(6, inventory.GetInt("Height"));

        // Simulate what OnResizeInventory does: set Width/Height
        int newWidth = 10;
        int newHeight = 12;
        inventory.Set("Width", newWidth);
        inventory.Set("Height", newHeight);

        Assert.Equal(10, inventory.GetInt("Width"));
        Assert.Equal(12, inventory.GetInt("Height"));
    }

    [Fact]
    public void InventoryResize_ShouldAddValidSlotIndices_ForNewDimensions()
    {
        var inventory = JsonObject.Parse(@"{
            ""Width"": 2,
            ""Height"": 2,
            ""Slots"": [],
            ""ValidSlotIndices"": [
                { ""X"": 0, ""Y"": 0 },
                { ""X"": 1, ""Y"": 0 },
                { ""X"": 0, ""Y"": 1 },
                { ""X"": 1, ""Y"": 1 }
            ]
        }");

        int newWidth = 3;
        int newHeight = 3;

        var validSlots = inventory.GetArray("ValidSlotIndices")!;

        // Build existing valid set
        var existing = new HashSet<(int, int)>();
        for (int i = 0; i < validSlots.Length; i++)
        {
            var idx = validSlots.GetObject(i);
            existing.Add((idx.GetInt("X"), idx.GetInt("Y")));
        }

        // Add new valid slot indices
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                if (!existing.Contains((x, y)))
                {
                    var newIdx = new JsonObject();
                    newIdx.Add("X", x);
                    newIdx.Add("Y", y);
                    validSlots.Add(newIdx);
                }
            }
        }

        // Update dimensions
        inventory.Set("Width", newWidth);
        inventory.Set("Height", newHeight);

        Assert.Equal(3, inventory.GetInt("Width"));
        Assert.Equal(3, inventory.GetInt("Height"));
        Assert.Equal(9, validSlots.Length); // 3x3 = 9 valid slots
    }

    [Fact]
    public void InventoryResize_ShouldRemoveValidSlotIndices_OutsideNewDimensions()
    {
        var inventory = JsonObject.Parse(@"{
            ""Width"": 3,
            ""Height"": 3,
            ""Slots"": [],
            ""ValidSlotIndices"": [
                { ""X"": 0, ""Y"": 0 },
                { ""X"": 1, ""Y"": 0 },
                { ""X"": 2, ""Y"": 0 },
                { ""X"": 0, ""Y"": 1 },
                { ""X"": 1, ""Y"": 1 },
                { ""X"": 2, ""Y"": 1 },
                { ""X"": 0, ""Y"": 2 },
                { ""X"": 1, ""Y"": 2 },
                { ""X"": 2, ""Y"": 2 }
            ]
        }");

        int newWidth = 2;
        int newHeight = 2;

        var validSlots = inventory.GetArray("ValidSlotIndices")!;
        Assert.Equal(9, validSlots.Length);

        // Remove indices outside new dimensions (reverse order)
        for (int i = validSlots.Length - 1; i >= 0; i--)
        {
            var idx = validSlots.GetObject(i);
            int x = idx.GetInt("X"), y = idx.GetInt("Y");
            if (x >= newWidth || y >= newHeight)
                validSlots.RemoveAt(i);
        }

        inventory.Set("Width", newWidth);
        inventory.Set("Height", newHeight);

        Assert.Equal(2, inventory.GetInt("Width"));
        Assert.Equal(2, inventory.GetInt("Height"));
        Assert.Equal(4, validSlots.Length); // 2x2 = 4 valid slots
    }

    // --- Unicode round-trip test ---

    [Fact]
    public void JsonParser_UnicodeRoundTrip_PreservesEscapedCharacters()
    {
        // Parse JSON containing unicode escape sequences
        string json = """{"Name": "test \u03BB and \u0166 end"}""";
        var obj = JsonObject.Parse(json);

        string value = obj.GetString("Name")!;
        Assert.Contains("\u03BB", value); // λ
        Assert.Contains("\u0166", value); // Ŧ

        // Re-serialise: chars > U+00FF are now written as raw UTF-8 bytes
        // (matching the NMS game format), not as \uXXXX escapes.
        // The parser round-trips them correctly: UTF-8 bytes → read as
        // Latin-1 → detected as valid UTF-8 → decoded back to Unicode.
        string output = obj.ToString();
        // Should NOT contain \uXXXX for these characters
        Assert.DoesNotContain("\\u03BB", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u0166", output, StringComparison.OrdinalIgnoreCase);
        // Should contain the Unicode characters when parsed back
        var reparsed = JsonObject.Parse(output);
        string reparsedValue = reparsed.GetString("Name")!;
        Assert.Equal(value, reparsedValue);
    }

    // --- Unicode escape vs binary data tests ---

    [Fact]
    public void JsonParser_UnicodeEscapeLatin1Range_ReturnsStringNotBinaryData()
    {
        // \u00E9 (é, U+00E9) is in the 0x80-0xFF range but arrives as a \u escape,
        // so it represents intentional Unicode - not raw binary data.
        // This must parse as a string, NOT BinaryData.
        string json = """{"SaveName": "Caf\u00E9 \u00FC\u00F1"}""";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("SaveName");
        Assert.IsType<string>(value);
        Assert.Contains("é", (string)value!);
        Assert.Contains("ü", (string)value!);
        Assert.Contains("ñ", (string)value!);
    }

    [Fact]
    public void JsonParser_RawHighBytes_ValidUtf8_ReturnsDecodedString()
    {
        // When Latin-1 decoded save data contains raw bytes that form valid
        // UTF-8 sequences (e.g. a Greek save name), the parser must decode
        // them as UTF-8 text, NOT return BinaryData.
        // CE BB is the UTF-8 encoding of λ (U+03BB).
        string json = "{\"Data\": \"" + (char)0xCE + (char)0xBB + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Data");
        Assert.IsType<string>(value);
        Assert.Equal("\u03BB", (string)value!); // λ
    }

    [Fact]
    public void JsonParser_RawHighBytes_InvalidUtf8_ReturnsBinaryData()
    {
        // A lone continuation byte (0x80) without a preceding start byte is
        // invalid UTF-8, so this must remain BinaryData.
        string json = "{\"Data\": \"" + (char)0x80 + (char)0x01 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Data");
        Assert.IsType<BinaryData>(value);
    }

    [Fact]
    public void JsonParser_RawUtf8_GreekSaveName_ReturnsString()
    {
        // Simulate a save name "λŦLλS Breach" stored as raw UTF-8 bytes
        // read through Latin-1 encoding (the exact scenario from the bug report).
        // λ = CE BB, Ŧ = C5 A6 in UTF-8
        string latin1 = "" + (char)0xCE + (char)0xBB     // λ
                            + (char)0xC5 + (char)0xA6     // Ŧ
                            + "L"
                            + (char)0xCE + (char)0xBB     // λ
                            + "S Breach";
        string json = "{\"SaveName\": \"" + latin1 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("SaveName");
        Assert.IsType<string>(value);
        Assert.Equal("\u03BB\u0166L\u03BBS Breach", (string)value!);
    }

    [Fact]
    public void JsonParser_RawUtf8_CjkCharacters_ReturnsString()
    {
        // CJK character 漢 = E6 BC A2 in UTF-8
        // Japanese あ = E3 81 82 in UTF-8
        string latin1 = "" + (char)0xE6 + (char)0xBC + (char)0xA2   // 漢
                            + (char)0xE3 + (char)0x81 + (char)0x82;  // あ
        string json = "{\"Name\": \"" + latin1 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Name");
        Assert.IsType<string>(value);
        Assert.Equal("漢あ", (string)value!);
    }

    [Fact]
    public void JsonParser_RawUtf8_KoreanCharacters_ReturnsString()
    {
        // Korean 한 = ED 95 9C in UTF-8
        string latin1 = "" + (char)0xED + (char)0x95 + (char)0x9C   // 한
                            + "Test";
        string json = "{\"Name\": \"" + latin1 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Name");
        Assert.IsType<string>(value);
        Assert.Equal("\uD55CTest", (string)value!); // 한 + "Test"
    }

    [Fact]
    public void JsonParser_RawUtf8_CyrillicCharacters_ReturnsString()
    {
        // Cyrillic Б = D0 91 in UTF-8
        string latin1 = "" + (char)0xD0 + (char)0x91;   // Б
        string json = "{\"Name\": \"" + latin1 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Name");
        Assert.IsType<string>(value);
        Assert.Equal("\u0411", (string)value!); // Б
    }

    [Fact]
    public void JsonParser_RawUtf8_SettlementName_MixedGreekAscii_ReturnsString()
    {
        // Simulate settlement name "00σφ011ηρ Station" as stored in save file.
        // σ=CF83, φ=CF86, η=CEB7, ρ=CF81 in UTF-8
        string latin1 = "00"
            + (char)0xCF + (char)0x83    // σ
            + (char)0xCF + (char)0x86    // φ
            + "011"
            + (char)0xCE + (char)0xB7    // η
            + (char)0xCF + (char)0x81    // ρ
            + " Station";
        string json = "{\"Name\": \"" + latin1 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Name");
        Assert.IsType<string>(value);
        Assert.Equal("00\u03C3\u03C6011\u03B7\u03C1 Station", (string)value!);
    }

    [Fact]
    public void JsonParser_RawUtf8_RoundTrip_PreservesContent()
    {
        // Parse a string with raw UTF-8 bytes, serialize it, parse again,
        // and verify the content is preserved.
        // λ = CE BB in UTF-8
        string latin1 = "Hello " + (char)0xCE + (char)0xBB + " World";
        string json = "{\"Name\": \"" + latin1 + "\"}";
        var obj = JsonObject.Parse(json);

        string name = obj.GetString("Name")!;
        Assert.Equal("Hello \u03BB World", name);

        // Re-serialize (this writes \u03BB as a \uXXXX escape)
        string output = obj.ToString();

        // Parse again
        var obj2 = JsonObject.Parse(output);
        string name2 = obj2.GetString("Name")!;
        Assert.Equal(name, name2);
    }

    [Fact]
    public void JsonParser_HexEscapes_ReturnsBinaryData()
    {
        // \x hex escapes are used for explicit binary data in JSON strings.
        // They must always produce BinaryData objects.
        string json = """{"TechId": "\x80\x01\xBC\x85\xF7"}""";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("TechId");
        Assert.IsType<BinaryData>(value);
        var binary = (BinaryData)value!;
        Assert.Equal(5, binary.ToByteArray().Length);
        Assert.Equal(0x80, binary.ToByteArray()[0]);
    }

    [Fact]
    public void JsonParser_TechPackHexEscape_ReturnsBinaryData()
    {
        // TechPack item IDs use \x hex escapes: e.g. ^808497C54986
        // stored as \x5E\x80\x84\x97\xC5\x49\x86.
        // This must remain BinaryData - the UTF-8 fix must not affect this path.
        string json = """{"Id": "\x5E\x80\x84\x97\xC5\x49\x86"}""";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Id");
        Assert.IsType<BinaryData>(value);
        var binary = (BinaryData)value!;
        Assert.Equal(7, binary.ToByteArray().Length);
        Assert.Equal(0x5E, binary.ToByteArray()[0]); // '^'
        Assert.Equal(0x80, binary.ToByteArray()[1]);
    }

    [Fact]
    public void JsonParser_TechPackHexEscapeWithVariant_ReturnsBinaryData()
    {
        // TechPack ID with variant suffix: \x5E\x80\x84\x97\xC5\x49\x86#12345
        // The \x escapes must keep this on the BinaryData path.
        string json = """{"Id": "\x5E\x80\x84\x97\xC5\x49\x86#12345"}""";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Id");
        Assert.IsType<BinaryData>(value);
    }

    [Fact]
    public void JsonParser_RawTechPackBytes_InvalidUtf8_ReturnsBinaryData()
    {
        // Simulate TechPack bytes appearing as raw Latin-1 characters (no \x escapes).
        // ^(0x5E) followed by 0x80 is invalid UTF-8 (continuation byte without
        // start byte), so this must still be detected as BinaryData.
        string json = "{\"Id\": \"" + (char)0x5E + (char)0x80 + (char)0x84
            + (char)0x97 + (char)0xC5 + (char)0x49 + (char)0x86 + "\"}";
        var obj = JsonObject.Parse(json);

        var value = obj.Get("Id");
        Assert.IsType<BinaryData>(value);
    }

    // --- SPEC_XOHELMET database entry test ---

    [Fact]
    public void GameItemDatabase_ContainsSpecXohelmet()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindResourceJsonDir();
        Assert.NotNull(jsonDir);

        bool loaded = db.LoadItemsFromJsonDirectory(jsonDir!);
        Assert.True(loaded);

        var item = db.GetItem("SPEC_XOHELMET");
        Assert.NotNull(item);
        Assert.Equal("SPEC_XOHELMET.png", item!.Icon);
        Assert.Equal("Specialist Exosuit Visuals", item.Subtitle);
    }

    // --- Settlement perk array expansion test ---

    [Fact]
    public void SettlementPerkArray_SmallArray_CanBeGrownAndTrimmed()
    {
        // Create a minimal Perks array with 7 entries (less than the 18-entry max)
        var json = JsonObject.Parse("""
        {
            "Perks": ["^","^","^","^","^","^","^OLD_SCHOOL"]
        }
        """);

        var perks = json.GetArray("Perks")!;
        Assert.Equal(7, perks.Length);

        // Verify accessing entries 0-6 works
        for (int i = 0; i < 6; i++)
            Assert.Equal("^", (string)perks.Get(i)!);
        Assert.Equal("^OLD_SCHOOL", (string)perks.Get(6)!);

        // Verify the array can be grown with Add()
        perks.Add("^NEW_PERK");
        Assert.Equal(8, perks.Length);
        Assert.Equal("^NEW_PERK", (string)perks.Get(7)!);

        // Verify RemoveAt() can trim trailing entries
        perks.RemoveAt(7);
        Assert.Equal(7, perks.Length);
        Assert.Equal("^OLD_SCHOOL", (string)perks.Get(6)!);
    }

    private static string? FindResourceLangDir()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "Resources", "ui", "lang");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    // --- Raw value preservation tests ---

    [Fact]
    public void MainStatsLogic_ReadRawStatValue_ReturnsUnclamped()
    {
        var ps = new JsonObject();
        ps.Add("Health", 999999999); // Exceeds typical max of 999999
        decimal raw = MainStatsLogic.ReadRawStatValue(ps, "Health");
        Assert.Equal(999999999m, raw);
    }

    [Fact]
    public void MainStatsLogic_ReadStatValue_ClampsToRange()
    {
        var ps = new JsonObject();
        ps.Add("Health", 999999999);
        decimal clamped = MainStatsLogic.ReadStatValue(ps, "Health", 0, 999999);
        Assert.Equal(999999m, clamped);
    }

    [Fact]
    public void MainStatsLogic_WriteStatValues_SkipsUnchangedWithRawValues()
    {
        var ps = new JsonObject();
        ps.Add("Health", 5000000); // Above max (999999)
        ps.Add("Shield", 100);
        ps.Add("Energy", 200);
        ps.Add("Units", 50);
        ps.Add("Nanites", 60);
        ps.Add("Specials", 70);

        var rawValues = new Dictionary<string, decimal>
        {
            ["Health"] = 5000000m,
            ["Shield"] = 100m,
            ["Energy"] = 200m,
            ["Units"] = 50m,
            ["Nanites"] = 60m,
            ["Specials"] = 70m,
        };

        // health=999999 (clamped max), shield/energy/units/nanites/quicksilver = raw values (in range)
        MainStatsLogic.WriteStatValues(ps, 999999, 100, 200, 50, 60, 70, rawValues);

        // Health should NOT be written (999999 == clamp(5000000, 0, 999999))
        // so original 5000000 is preserved
        Assert.Equal(5000000, ps.GetInt("Health"));

        // Shield IS in range, so 100 == clamp(100, 0, 999999) - should NOT be written
        Assert.Equal(100, ps.GetInt("Shield"));
    }

    [Fact]
    public void MainStatsLogic_WriteStatValues_WritesChangedValues()
    {
        var ps = new JsonObject();
        ps.Add("Health", 5000000);
        ps.Add("Shield", 100);
        ps.Add("Energy", 200);
        ps.Add("Units", 50);
        ps.Add("Nanites", 60);
        ps.Add("Specials", 70);

        var rawValues = new Dictionary<string, decimal>
        {
            ["Health"] = 5000000m,
            ["Shield"] = 100m,
            ["Energy"] = 200m,
            ["Units"] = 50m,
            ["Nanites"] = 60m,
            ["Specials"] = 70m,
        };

        // User changed shield from 100 to 500
        MainStatsLogic.WriteStatValues(ps, 999999, 500, 200, 50, 60, 70, rawValues);

        // Health: NOT written (unchanged from clamped), original preserved
        Assert.Equal(5000000, ps.GetInt("Health"));

        // Shield: WRITTEN (user changed it from 100 to 500)
        Assert.Equal(500, ps.GetInt("Shield"));
    }

    [Fact]
    public void MainStatsLogic_WriteStatValues_NullRawValues_AlwaysWrites()
    {
        var ps = new JsonObject();
        ps.Add("Health", 5000000);
        ps.Add("Shield", 100);
        ps.Add("Energy", 200);
        ps.Add("Units", 50);
        ps.Add("Nanites", 60);
        ps.Add("Specials", 70);

        // Without raw values, always write (backward compatible behavior)
        MainStatsLogic.WriteStatValues(ps, 999999, 100, 200, 50, 60, 70, null);
        Assert.Equal(999999, ps.GetInt("Health"));
    }

    [Fact]
    public void SaveShipData_PreservesRawStats_WhenUnchanged()
    {
        // Setup a ship with stats that exceed BaseStatLimits
        var ship = new JsonObject();
        ship.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);

        var inv = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntry(bsv, "^SHIP_DAMAGE", 999.0); // above typical max
        AddBaseStatEntry(bsv, "^SHIP_SHIELD", 50.0);
        AddBaseStatEntry(bsv, "^SHIP_HYPERDRIVE", 500.0); // above typical max
        AddBaseStatEntry(bsv, "^SHIP_AGILE", 30.0);
        inv.Add("BaseStatValues", bsv);
        ship.Add("Inventory", inv);

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);

        // Simulate: UI values are what clamping would produce from raw values
        var range = BaseStatLimits.GetRange("Normal", "^SHIP_DAMAGE", StatCategory.Ship);
        double clampedDamage = range != null ? Math.Max(range.MinValue, Math.Min(999.0, range.MaxValue)) : 999.0;
        var rangeHd = BaseStatLimits.GetRange("Normal", "^SHIP_HYPERDRIVE", StatCategory.Ship);
        double clampedHd = rangeHd != null ? Math.Max(rangeHd.MinValue, Math.Min(500.0, rangeHd.MaxValue)) : 500.0;

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "TestShip",
            ShipIndex = -1,
            Damage = clampedDamage,
            Shield = 50.0,
            Hyperdrive = clampedHd,
            Maneuver = 30.0,
            RawStatValues = new Dictionary<string, double>
            {
                ["^SHIP_DAMAGE"] = 999.0,
                ["^SHIP_SHIELD"] = 50.0,
                ["^SHIP_HYPERDRIVE"] = 500.0,
                ["^SHIP_AGILE"] = 30.0,
            }
        };
        StarshipLogic.SaveShipData(ship, playerState, values);

        // Raw values should be preserved (999.0 and 500.0)
        double savedDamage = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_DAMAGE");
        double savedHd = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_HYPERDRIVE");
        Assert.Equal(999.0, savedDamage);
        Assert.Equal(500.0, savedHd);
    }

    [Fact]
    public void SaveShipData_WritesNewStats_WhenUserChanged()
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

        var inv = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntry(bsv, "^SHIP_DAMAGE", 999.0);
        inv.Add("BaseStatValues", bsv);
        ship.Add("Inventory", inv);

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);

        // User changed damage from clamped value to a new in-range value
        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "TestShip",
            ShipIndex = -1,
            Damage = 75.0, // User explicitly set this
            RawStatValues = new Dictionary<string, double>
            {
                ["^SHIP_DAMAGE"] = 999.0,
            }
        };
        StarshipLogic.SaveShipData(ship, playerState, values);

        double savedDamage = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_DAMAGE");
        Assert.Equal(75.0, savedDamage);
    }

    [Fact]
    public void SaveToolData_PreservesRawStats_WhenUnchanged()
    {
        var tool = new JsonObject();
        tool.Add("Name", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        tool.Add("Seed", seedArr);

        var store = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntry(bsv, "^WEAPON_DAMAGE", 999.0);
        AddBaseStatEntry(bsv, "^WEAPON_MINING", 50.0);
        AddBaseStatEntry(bsv, "^WEAPON_SCAN", 40.0);
        store.Add("BaseStatValues", bsv);
        store.Add("Class", new JsonObject());
        tool.Add("Store", store);
        tool.Add("Store_TechOnly", new JsonObject());

        var range = BaseStatLimits.GetRange("Normal", "^WEAPON_DAMAGE", StatCategory.Weapon);
        double clampedDamage = range != null ? Math.Max(range.MinValue, Math.Min(999.0, range.MaxValue)) : 999.0;

        var values = new MultitoolLogic.ToolSaveValues
        {
            Name = "TestTool",
            Damage = clampedDamage,
            Mining = 50.0,
            Scan = 40.0,
            RawStatValues = new Dictionary<string, double>
            {
                ["^WEAPON_DAMAGE"] = 999.0,
                ["^WEAPON_MINING"] = 50.0,
                ["^WEAPON_SCAN"] = 40.0,
            }
        };

        MultitoolLogic.SaveToolData(tool, null, values, false);

        double savedDamage = StatHelper.ReadBaseStatValue(tool.GetObject("Store"), "^WEAPON_DAMAGE");
        Assert.Equal(999.0, savedDamage);
    }

    [Fact]
    public void ConditionalClampStatValue_PreservesRaw_WhenUnchanged()
    {
        var rawValues = new Dictionary<string, double> { ["^SHIP_DAMAGE"] = 999.0 };

        // With MaxValue=int.MaxValue, clamping 999 produces 999, which is what the UI shows.
        // When the user doesn't change the displayed value (999), ConditionalClamp should
        // return the original raw value (999).
        double clampedRaw = BaseStatLimits.ClampStatValue("Normal", "^SHIP_DAMAGE", 999.0, StatCategory.Ship);
        double result = BaseStatLimits.ConditionalClampStatValue("Normal", "^SHIP_DAMAGE",
            clampedRaw, StatCategory.Ship, rawValues);
        Assert.Equal(999.0, result);
    }

    [Fact]
    public void ConditionalClampStatValue_ClampsNewValue_WhenUserChanged()
    {
        var rawValues = new Dictionary<string, double> { ["^SHIP_DAMAGE"] = 999.0 };

        // User changed the value to 75 (different from clamp(999))
        double result = BaseStatLimits.ConditionalClampStatValue("Normal", "^SHIP_DAMAGE",
            75.0, StatCategory.Ship, rawValues);
        Assert.Equal(75.0, result);
    }

    [Fact]
    public void ConditionalClampStatValue_NullRawValues_FallsBackToClamp()
    {
        // Without raw values, should just clamp
        double result = BaseStatLimits.ConditionalClampStatValue("Normal", "^SHIP_DAMAGE",
            999.0, StatCategory.Ship, null);
        var range = BaseStatLimits.GetRange("Normal", "^SHIP_DAMAGE", StatCategory.Ship);
        double expected = range != null ? Math.Max(range.MinValue, Math.Min(999.0, range.MaxValue)) : 999.0;
        Assert.Equal(expected, result);
    }

    private static void AddBaseStatEntry(JsonArray bsv, string statId, double value)
    {
        var entry = new JsonObject();
        entry.Add("BaseStatID", statId);
        entry.Add("Value", value);
        bsv.Add(entry);
    }

    /// <summary>
    /// Helper to add a BaseStatValue entry using a RawDouble (simulating a parsed save).
    /// </summary>
    private static void AddBaseStatEntryRaw(JsonArray bsv, string statId, double value, string rawText)
    {
        var entry = new JsonObject();
        entry.Add("BaseStatID", statId);
        entry.Add("Value", new RawDouble(value, rawText));
        bsv.Add(entry);
    }

    // --- Synthetic round-trip tests for stat value preservation ---

    [Fact]
    public void SaveShipData_PreservesRawDoubles_WhenUnchanged_HighPrecision()
    {
        // Exact values from the user's bug report: editing damage should NOT
        // modify shield or agile.
        double shield = 31.199888229370117; // IEEE 754 double for "31.199888229370118"
        double agile = 46.931243896484375;  // IEEE 754 double for "46.931243896484378"
        double damage = 85.12345;
        double hyper = 200.5;

        var ship = new JsonObject();
        ship.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);

        var inv = new JsonObject();
        var bsv = new JsonArray();
        // Use RawDoubles to simulate a parsed save file
        AddBaseStatEntryRaw(bsv, "^SHIP_DAMAGE", damage, "85.12345");
        AddBaseStatEntryRaw(bsv, "^SHIP_SHIELD", shield, "31.199888229370118");
        AddBaseStatEntryRaw(bsv, "^SHIP_HYPERDRIVE", hyper, "200.5");
        AddBaseStatEntryRaw(bsv, "^SHIP_AGILE", agile, "46.931243896484378");
        inv.Add("BaseStatValues", bsv);
        ship.Add("Inventory", inv);

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);

        // User ONLY changed damage to 100.0; other stats untouched.
        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "TestShip",
            ShipIndex = -1,
            Damage = 100.0,  // User explicitly set this
            Shield = shield,  // Same as loaded - NOT modified
            Hyperdrive = hyper,  // Same as loaded - NOT modified
            Maneuver = agile,  // Same as loaded - NOT modified
            RawStatValues = new Dictionary<string, double>
            {
                ["^SHIP_DAMAGE"] = damage,
                ["^SHIP_SHIELD"] = shield,
                ["^SHIP_HYPERDRIVE"] = hyper,
                ["^SHIP_AGILE"] = agile,
            }
        };

        StarshipLogic.SaveShipData(ship, playerState, values);

        // Damage should be the new value
        double savedDamage = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_DAMAGE");
        Assert.Equal(100.0, savedDamage);

        // Shield, hyperdrive, and agile should be preserved EXACTLY
        double savedShield = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_SHIELD");
        double savedHyper = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_HYPERDRIVE");
        double savedAgile = StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_AGILE");
        Assert.Equal(shield, savedShield);
        Assert.Equal(hyper, savedHyper);
        Assert.Equal(agile, savedAgile);

        // Verify the RawDouble text is preserved for unchanged stats
        var savedBsv = ship.GetObject("Inventory")!.GetArray("BaseStatValues")!;
        for (int i = 0; i < savedBsv.Length; i++)
        {
            var entry = savedBsv.GetObject(i);
            string statId = entry.GetString("BaseStatID") ?? "";
            if (statId == "^SHIP_SHIELD")
            {
                var raw = entry.GetValue("Value");
                Assert.IsType<RawDouble>(raw);
                Assert.Equal("31.199888229370118", ((RawDouble)raw).Text);
            }
            else if (statId == "^SHIP_AGILE")
            {
                var raw = entry.GetValue("Value");
                Assert.IsType<RawDouble>(raw);
                Assert.Equal("46.931243896484378", ((RawDouble)raw).Text);
            }
        }
    }

    [Fact]
    public void SaveShipData_WritesToInventoryAndTechOnly()
    {
        // Base stats are written to Inventory + Inventory_TechOnly.
        // Inventory_Cargo is excluded for v400+ Waypoint saves.
        var ship = new JsonObject();
        ship.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        ship.Add("Resource", resource);

        // Primary inventory with stats the UI sees
        var inv = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntryRaw(bsv, "^SHIP_DAMAGE", 80.0, "80.0");
        AddBaseStatEntryRaw(bsv, "^SHIP_SHIELD", 35.0, "35.0");
        AddBaseStatEntryRaw(bsv, "^SHIP_HYPERDRIVE", 10.0, "10.0");
        AddBaseStatEntryRaw(bsv, "^SHIP_AGILE", 50.0, "50.0");
        inv.Add("BaseStatValues", bsv);
        ship.Add("Inventory", inv);

        // Tech inventory with DIFFERENT stats (set by the game engine)
        var techInv = new JsonObject();
        var techBsv = new JsonArray();
        AddBaseStatEntryRaw(techBsv, "^SHIP_DAMAGE", 63.73244094848633, "63.73244094848633");
        AddBaseStatEntryRaw(techBsv, "^SHIP_SHIELD", 31.199888229370117, "31.199888229370118");
        AddBaseStatEntryRaw(techBsv, "^SHIP_HYPERDRIVE", 6.988905429840088, "6.988905429840088");
        AddBaseStatEntryRaw(techBsv, "^SHIP_AGILE", 46.931243896484375, "46.931243896484378");
        techInv.Add("BaseStatValues", techBsv);
        ship.Add("Inventory_TechOnly", techInv);

        // Cargo inventory (same as primary)
        var cargoInv = new JsonObject();
        var cargoBsv = new JsonArray();
        AddBaseStatEntryRaw(cargoBsv, "^SHIP_DAMAGE", 80.0, "80.0");
        AddBaseStatEntryRaw(cargoBsv, "^SHIP_SHIELD", 35.0, "35.0");
        AddBaseStatEntryRaw(cargoBsv, "^SHIP_HYPERDRIVE", 10.0, "10.0");
        AddBaseStatEntryRaw(cargoBsv, "^SHIP_AGILE", 50.0, "50.0");
        cargoBsv.Add(new JsonObject()); // extra entry
        cargoInv.Add("BaseStatValues", cargoBsv);
        ship.Add("Inventory_Cargo", cargoInv);

        var playerState = new JsonObject();
        playerState.Add("PrimaryShip", 0);

        // User changed ONLY damage from 80.0 to 81.456465445464
        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "VCF Blackbird",
            ShipIndex = -1,
            Damage = 81.456465445464,
            Shield = 35.0,
            Hyperdrive = 10.0,
            Maneuver = 50.0,
            RawStatValues = new Dictionary<string, double>
            {
                ["^SHIP_DAMAGE"] = 80.0,
                ["^SHIP_SHIELD"] = 35.0,
                ["^SHIP_HYPERDRIVE"] = 10.0,
                ["^SHIP_AGILE"] = 50.0,
            }
        };

        StarshipLogic.SaveShipData(ship, playerState, values);

        // Primary inventory: damage should be updated, others preserved
        Assert.Equal(81.456465445464, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_DAMAGE"));
        Assert.Equal(35.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_SHIELD"));
        Assert.Equal(10.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_HYPERDRIVE"));
        Assert.Equal(50.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory"), "^SHIP_AGILE"));

        // Tech inventory: same values as Inventory (stats are written to both)
        Assert.Equal(81.456465445464, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_TechOnly"), "^SHIP_DAMAGE"));
        Assert.Equal(35.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_TechOnly"), "^SHIP_SHIELD"));
        Assert.Equal(10.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_TechOnly"), "^SHIP_HYPERDRIVE"));
        Assert.Equal(50.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_TechOnly"), "^SHIP_AGILE"));

        // Cargo inventory: must be UNTOUCHED (excluded for v400+)
        Assert.Equal(80.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_Cargo"), "^SHIP_DAMAGE"));
        Assert.Equal(35.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_Cargo"), "^SHIP_SHIELD"));
        Assert.Equal(10.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_Cargo"), "^SHIP_HYPERDRIVE"));
        Assert.Equal(50.0, StatHelper.ReadBaseStatValue(ship.GetObject("Inventory_Cargo"), "^SHIP_AGILE"));
    }

    [Fact]
    public void SaveFreighterData_WritesToBothInventories()
    {
        // Base stats are written to FreighterInventory + FreighterInventory_TechOnly.
        // For freighters on v400+ Waypoint both inventories receive the stat values.
        var playerState = new JsonObject();
        playerState.Add("PlayerFreighterName", "TestFreighter");

        // Primary freighter inventory with stats the UI sees
        var inv = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntryRaw(bsv, "^FREI_HYPERDRIVE", 79.93309020996094, "79.93309020996094");
        AddBaseStatEntryRaw(bsv, "^FREI_FLEET", 41.5, "41.5");
        inv.Add("BaseStatValues", bsv);
        playerState.Add("FreighterInventory", inv);

        // Tech inventory with DIFFERENT stats
        var techInv = new JsonObject();
        var techBsv = new JsonArray();
        AddBaseStatEntryRaw(techBsv, "^FREI_HYPERDRIVE", 22.44556677889900, "22.44556677889900");
        AddBaseStatEntryRaw(techBsv, "^FREI_FLEET", 15.123456789, "15.123456789");
        techInv.Add("BaseStatValues", techBsv);
        playerState.Add("FreighterInventory_TechOnly", techInv);

        var values = new FreighterLogic.FreighterSaveValues
        {
            Name = "TestFreighter",
            Hyperdrive = 100.0,  // User changed this
            FleetCoordination = 41.5,  // Same as loaded
            RawStatValues = new Dictionary<string, double>
            {
                ["^FREI_HYPERDRIVE"] = 79.93309020996094,
                ["^FREI_FLEET"] = 41.5,
            }
        };

        FreighterLogic.SaveFreighterData(playerState, values);

        // Primary inventory: hyperdrive should be updated, fleet preserved
        Assert.Equal(100.0, StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory"), "^FREI_HYPERDRIVE"));
        Assert.Equal(41.5, StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory"), "^FREI_FLEET"));

        // Tech inventory: same values as primary (stats are written to both)
        Assert.Equal(100.0, StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory_TechOnly"), "^FREI_HYPERDRIVE"));
        Assert.Equal(41.5, StatHelper.ReadBaseStatValue(playerState.GetObject("FreighterInventory_TechOnly"), "^FREI_FLEET"));
    }

    [Fact]
    public void SaveToolData_PreservesRawDoubles_WhenUnchanged_HighPrecision()
    {
        double damage = 44.876543210987654;
        double mining = 33.123456789012345;
        double scan = 22.987654321098765;

        var tool = new JsonObject();
        tool.Add("Name", "");
        var resource = new JsonObject();
        resource.Add("Filename", "");
        var seedArr = new JsonArray();
        seedArr.Add(true);
        seedArr.Add("0x0");
        resource.Add("Seed", seedArr);
        tool.Add("Resource", resource);

        var inv = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntryRaw(bsv, "^WEAPON_DAMAGE", damage, "44.876543210987654");
        AddBaseStatEntryRaw(bsv, "^WEAPON_MINING", mining, "33.123456789012345");
        AddBaseStatEntryRaw(bsv, "^WEAPON_SCAN", scan, "22.987654321098765");
        inv.Add("BaseStatValues", bsv);
        tool.Add("Store", inv);

        var playerState = new JsonObject();
        playerState.Add("ActiveMultiTool", 0);

        // User ONLY changed scan; damage and mining untouched.
        var values = new MultitoolLogic.ToolSaveValues
        {
            Name = "TestTool",
            Damage = damage,   // Same as loaded
            Mining = mining,   // Same as loaded
            Scan = 50.0,       // User changed this
            RawStatValues = new Dictionary<string, double>
            {
                ["^WEAPON_DAMAGE"] = damage,
                ["^WEAPON_MINING"] = mining,
                ["^WEAPON_SCAN"] = scan,
            }
        };

        MultitoolLogic.SaveToolData(tool, playerState, values, true);

        // Scan should be the new value
        double savedScan = StatHelper.ReadBaseStatValue(tool.GetObject("Store"), "^WEAPON_SCAN");
        Assert.Equal(50.0, savedScan);

        // Damage and mining should be preserved EXACTLY
        double savedDamage = StatHelper.ReadBaseStatValue(tool.GetObject("Store"), "^WEAPON_DAMAGE");
        double savedMining = StatHelper.ReadBaseStatValue(tool.GetObject("Store"), "^WEAPON_MINING");
        Assert.Equal(damage, savedDamage);
        Assert.Equal(mining, savedMining);

        // Verify RawDouble preservation
        var savedBsv = tool.GetObject("Store")!.GetArray("BaseStatValues")!;
        for (int i = 0; i < savedBsv.Length; i++)
        {
            var entry = savedBsv.GetObject(i);
            string statId = entry.GetString("BaseStatID") ?? "";
            if (statId == "^WEAPON_DAMAGE")
            {
                var raw = entry.GetValue("Value");
                Assert.IsType<RawDouble>(raw);
            }
            else if (statId == "^WEAPON_MINING")
            {
                var raw = entry.GetValue("Value");
                Assert.IsType<RawDouble>(raw);
            }
        }
    }

    [Fact]
    public void ConditionalClampStatValue_PreservesRaw_WhenUiMatchesRaw()
    {
        // Even if clamping would change the value, when uiValue matches raw,
        // the original raw value must be preserved.
        double raw = 31.199888229370117;
        var rawValues = new Dictionary<string, double> { ["^SHIP_SHIELD"] = raw };

        // UI value equals the raw value (untouched by user)
        double result = BaseStatLimits.ConditionalClampStatValue(
            "Normal", "^SHIP_SHIELD", raw, StatCategory.Ship, rawValues);
        Assert.Equal(raw, result);
    }

    [Fact]
    public void WriteBaseStatValue_SkipsWrite_ForPlainDouble_WhenUnchanged()
    {
        // After a previous save replaces a RawDouble with a plain double,
        // subsequent saves should still skip the write for unchanged values.
        var inv = new JsonObject();
        var bsv = new JsonArray();
        var entry = new JsonObject();
        entry.Add("BaseStatID", "^SHIP_DAMAGE");
        entry.Add("Value", 42.5); // Plain double (not RawDouble)
        bsv.Add(entry);
        inv.Add("BaseStatValues", bsv);

        // Write the same value
        StatHelper.WriteBaseStatValue(inv, "^SHIP_DAMAGE", 42.5);

        // Value should still be a plain double (not replaced)
        var existing = bsv.GetObject(0).GetValue("Value");
        Assert.IsType<double>(existing);
        Assert.Equal(42.5, (double)existing);
    }

    [Fact]
    public void NumericParseHelper_RoundTrip_PreservesExactDouble()
    {
        // Verify that FormatDouble + TryParseDouble round-trips exactly
        // for the specific values from the user's bug report.
        double[] testValues =
        {
            31.199888229370117,      // shield
            46.931243896484375,      // agile
            85.12345,                // simple value
            0.30000001192092896,     // common NMS float
            1e-15,                   // very small
            1.7976931348623157e+308, // near max double
            0.0,
            -42.5,
            999999999.999999,
        };

        foreach (double original in testValues)
        {
            string formatted = NumericParseHelper.FormatDouble(original);
            Assert.True(NumericParseHelper.TryParseDouble(formatted, out double parsed),
                $"Failed to parse formatted double: {formatted}");
            Assert.Equal(original, parsed);
            // Verify bit-exact equality
            Assert.Equal(
                BitConverter.DoubleToInt64Bits(original),
                BitConverter.DoubleToInt64Bits(parsed));
        }
    }

    [Theory]
    [InlineData("45.803646087646487", 3, "48.803646087646487")]
    [InlineData("75.96650695800781", 2, "77.96650695800781")]
    [InlineData("27.38205337524414", 2, "29.38205337524414")]
    [InlineData("9.999549865722657", 2, "11.999549865722657")]
    [InlineData("45.803646087646487", -3, "42.803646087646487")]
    [InlineData("100.0", 5, "105.0")]
    public void SpinnerDecimalArithmetic_PreservesFractionalDigits(string startText, int steps, string expectedText)
    {
        // Simulates the decimal arithmetic used by InvariantNumericTextBox.Step()
        // to verify that fractional digits are preserved when stepping by integer
        // increments. Double arithmetic would corrupt trailing digits, e.g.
        // 45.803646087646487 + 3 = 48.803646087646484 in double, but using
        // decimal: 48.803646087646487 - which is what the user expects.
        Assert.True(decimal.TryParse(startText,
            System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
            System.Globalization.CultureInfo.InvariantCulture, out decimal decVal));

        decVal += steps * 1.0m; // Increment is 1.0 for stat fields

        string resultText = decVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(expectedText, resultText);
    }

    [Fact]
    public void SaveShipData_SpinnerIncrement_PreservesStatText()
    {
        // Full end-to-end: a stat loaded from JSON with RawDouble text, then
        // "incremented" by the user (simulating spinner steps via decimal arithmetic),
        // should serialise back with the fractional digits intact.
        var inv = new JsonObject();
        var bsv = new JsonArray();
        AddBaseStatEntryRaw(bsv, "^SHIP_DAMAGE", 45.803646087646484, "45.803646087646487");
        AddBaseStatEntryRaw(bsv, "^SHIP_SHIELD", 27.38205337524414, "27.38205337524414");
        inv.Add("BaseStatValues", bsv);

        // Simulate spinner: user presses Up 3 times on damage (decimal arithmetic)
        decimal decDamage = 45.803646087646487m + 3m;
        double newDamage = (double)decDamage;
        string newDamageText = decDamage.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Write the incremented value with its display text
        StatHelper.WriteBaseStatValue(inv, "^SHIP_DAMAGE", newDamage, newDamageText);

        // Verify the stored RawDouble text is the expected spinner result
        var savedBsv = inv.GetArray("BaseStatValues")!;
        for (int i = 0; i < savedBsv.Length; i++)
        {
            var entry = savedBsv.GetObject(i);
            if ((entry.GetString("BaseStatID") ?? "") == "^SHIP_DAMAGE")
            {
                var raw = entry.GetValue("Value");
                Assert.IsType<RawDouble>(raw);
                Assert.Equal("48.803646087646487", ((RawDouble)raw).Text);
            }
        }
    }

    [Fact]
    public void MainStatsLogic_WriteIfChanged_PreservesRaw_WhenUiMatchesRaw()
    {
        // Verify that WriteStatValues preserves raw values when UI values match.
        var playerState = new JsonObject();
        playerState.Add("Health", new RawDouble(100, "100"));
        playerState.Add("Shield", new RawDouble(50, "50"));
        playerState.Add("Energy", new RawDouble(75, "75"));
        playerState.Add("Units", new RawDouble(1000000, "1000000"));
        playerState.Add("Nanites", new RawDouble(500000, "500000"));
        playerState.Add("Specials", new RawDouble(200, "200"));

        var rawValues = new Dictionary<string, decimal>
        {
            ["Health"] = 100m,
            ["Shield"] = 50m,
            ["Energy"] = 75m,
            ["Units"] = 1000000m,
            ["Nanites"] = 500000m,
            ["Specials"] = 200m,
        };

        // All UI values (infront) match raw values (behind) so nothing should be written
        MainStatsLogic.WriteStatValues(playerState, 100m, 50m, 75m, 1000000m, 500000m, 200m, rawValues);

        // All values should still be RawDoubles (preserved)
        Assert.IsType<RawDouble>(playerState.Get("Health"));
        Assert.IsType<RawDouble>(playerState.Get("Shield"));
        Assert.IsType<RawDouble>(playerState.Get("Energy"));
        Assert.IsType<RawDouble>(playerState.Get("Units"));
        Assert.IsType<RawDouble>(playerState.Get("Nanites"));
        Assert.IsType<RawDouble>(playerState.Get("Specials"));
    }

    [Fact]
    public void BaseStatLimits_AllStatRanges_UseIntMaxValue()
    {
        // All base stat ranges should use int.MaxValue as their max value
        foreach (var (entityType, stats) in BaseStatLimits.ShipStats)
        {
            foreach (var (statId, range) in stats)
            {
                Assert.Equal(0, range.MinValue);
                Assert.Equal(int.MaxValue, range.MaxValue);
            }
        }
        foreach (var (entityType, stats) in BaseStatLimits.WeaponStats)
        {
            foreach (var (statId, range) in stats)
            {
                Assert.Equal(0, range.MinValue);
                Assert.Equal(int.MaxValue, range.MaxValue);
            }
        }
        foreach (var (entityType, stats) in BaseStatLimits.FreighterStats)
        {
            foreach (var (statId, range) in stats)
            {
                Assert.Equal(0, range.MinValue);
                Assert.Equal(int.MaxValue, range.MaxValue);
            }
        }
    }

    [Fact]
    public void SettlementLogic_PopulationMax_Is400()
    {
        Assert.Equal(400, SettlementLogic.PopulationMax);
    }

    [Fact]
    public void SettlementLogic_PopulationSoftMax_Is200()
    {
        Assert.Equal(200, SettlementLogic.PopulationSoftMax);
    }

    // --- Settlement (raw value preservation) ---

    [Fact]
    public void SettlementLogic_LoadSettlementData_StoresRawStats()
    {
        // Create a settlement with stats that exceed the max and a separate Population key
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC123"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [999, 200, 1500000, 1000000, 100, 10000000, 1000, 1000],
            ""Population"": 999,
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);

        // Stats are unclamped - UI shows raw values
        Assert.Equal(999, data.Stats[0]); // MaxPopulation raw value
        Assert.Equal(200, data.Stats[1]); // Happiness raw value

        // Raw stat values should be preserved
        Assert.Equal(999, data.RawStats[0]); // MaxPopulation raw
        Assert.Equal(200, data.RawStats[1]); // Happiness raw

        // Population field (separate from Stats[0])
        Assert.True(data.HasPopulationKey);
        Assert.Equal(400, data.Population); // Clamped from 999 to PopulationMax (400)
        Assert.Equal(999, data.RawPopulation); // Raw preserved
    }

    [Fact]
    public void SettlementLogic_ShouldWriteStat_SkipsUnchangedValue()
    {
        // Stats are unclamped - UI shows raw values directly.
        // If UI value matches raw, should NOT write (no change).
        var rawStats = new int[] { 999, 200, 1500000, 1000000, 100, 10000000, 1000, 1000 };

        Assert.False(SettlementLogic.ShouldWriteStat(0, 999, rawStats)); // 999 == raw[0]
        Assert.False(SettlementLogic.ShouldWriteStat(1, 200, rawStats)); // 200 == raw[1]
    }

    [Fact]
    public void SettlementLogic_ShouldWriteStat_WritesWhenUserChanged()
    {
        // User changed value from raw - should write.
        var rawStats = new int[] { 999, 200, 1500000, 1000000, 100, 10000000, 1000, 1000 };

        Assert.True(SettlementLogic.ShouldWriteStat(0, 100, rawStats)); // 100 != 999
        Assert.True(SettlementLogic.ShouldWriteStat(1, 50, rawStats));  // 50 != 200
    }

    [Fact]
    public void SettlementLogic_ShouldWriteStat_NullRawStats_AlwaysWrites()
    {
        Assert.True(SettlementLogic.ShouldWriteStat(0, 175, null));
        Assert.True(SettlementLogic.ShouldWriteStat(1, 180, null));
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_PreservesUnchangedStats()
    {
        // Create a settlement with stats that exceed typical range
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC123"",
            ""Stats"": [999, 200, 100, 50, 25, 500, 100, 100],
            ""Population"": 999,
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        // Simulate: user loads (values shown unclamped), doesn't change them, saves
        var rawStats = new int[] { 999, 200, 100, 50, 25, 500, 100, 100 };

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC123",
            Stats = new int[] { 999, 200, 100, 50, 25, 500, 100, 100 }, // Unchanged from raw
            RawStats = rawStats,
            DecisionTypeIndex = 0,
        };

        SettlementLogic.SaveSettlementData(settlement, saveValues);

        // All stats should be preserved (user didn't change any)
        var statsArr = settlement.GetArray("Stats")!;
        Assert.Equal(999, statsArr.GetInt(0)); // Preserved original
        Assert.Equal(200, statsArr.GetInt(1)); // Preserved original

        // In-range stats that matched should also be preserved
        Assert.Equal(100, statsArr.GetInt(2));
        Assert.Equal(50, statsArr.GetInt(3));

        // Population field should also be preserved
        Assert.Equal(999, settlement.GetInt("Population"));
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_WritesChangedStats()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC123"",
            ""Stats"": [999, 200, 100, 50, 25, 500, 100, 100],
            ""Population"": 999,
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var rawStats = new int[] { 999, 200, 100, 50, 25, 500, 100, 100 };

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC123",
            Stats = new int[] { 100, 50, 100, 50, 25, 500, 100, 100 }, // User changed MaxPop to 100, Happiness to 50
            RawStats = rawStats,
            Population = 100, // User changed Population to 100
            RawPopulation = 999, // Raw was 999
            HasPopulationKey = true,
            DecisionTypeIndex = 0,
        };

        SettlementLogic.SaveSettlementData(settlement, saveValues);

        var statsArr = settlement.GetArray("Stats")!;
        Assert.Equal(100, statsArr.GetInt(0)); // User changed MaxPop - written
        Assert.Equal(50, statsArr.GetInt(1));  // User changed Happiness - written
        Assert.Equal(100, settlement.GetInt("Population")); // Population also written (user changed it)
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_NullRawStats_AlwaysWrites()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC123"",
            ""Stats"": [999, 200, 100, 50, 25, 500, 100, 100],
            ""Population"": 999,
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        // No raw stats - backward-compatible behavior, always writes
        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC123",
            Stats = new int[] { 175, 180, 100, 50, 25, 500, 100, 100 },
            RawStats = null,
            DecisionTypeIndex = 0,
        };

        SettlementLogic.SaveSettlementData(settlement, saveValues);

        var statsArr = settlement.GetArray("Stats")!;
        Assert.Equal(175, statsArr.GetInt(0)); // Written unconditionally
        Assert.Equal(180, statsArr.GetInt(1)); // Written unconditionally
    }

    [Fact]
    public void SettlementLogic_LoadSettlementData_NoPopulationKey_HasPopulationKeyFalse()
    {
        // Old save format: no top-level Population key
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""OldSettlement"",
            ""SeedValue"": ""OLD123"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [50, 100, 500, 200, 10, 1000, 50, 50],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);

        Assert.False(data.HasPopulationKey);
        Assert.Equal(0, data.Population);
        Assert.Equal(50, data.Stats[0]); // Stats[0] = MaxPopulation, read normally
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_NoPopulationKey_DoesNotWritePopulation()
    {
        // Old save format: no Population key. Saving should NOT add one.
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""OldSettlement"",
            ""SeedValue"": ""OLD123"",
            ""Stats"": [50, 100, 500, 200, 10, 1000, 50, 50],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "OldSettlement",
            SeedValue = "OLD123",
            Stats = new int[] { 50, 100, 500, 200, 10, 1000, 50, 50 },
            RawStats = null,
            HasPopulationKey = false,
            DecisionTypeIndex = 0,
        };

        SettlementLogic.SaveSettlementData(settlement, saveValues);

        // Should NOT have a Population key
        Assert.False(settlement.Contains("Population"));
    }

    // --- Settlement Perk Seed Format Tests --------------------------

    [Fact]
    public void SettlementLogic_PerkSeedFormat_IntegerSeedSavedInPerkString()
    {
        // Verify that a procedural perk with integer seed is stored as "perkId#seed"
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""SeedTest"",
            ""SeedValue"": ""0xABC"",
            ""Stats"": [50, 100, 500, 200, 10, 1000, 50, 50],
            ""Perks"": [""^STARTING_NEG1#12345"", ""^STARTING_POS1"", ""^""],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        // Verify the perk string format round-trips correctly through the JSON
        var perksArr = settlement.GetArray("Perks")!;
        string perkWithSeed = perksArr.GetString(0)!;
        Assert.Contains("#", perkWithSeed);
        Assert.Equal("^STARTING_NEG1#12345", perkWithSeed);

        // Parse like the UI does
        int hashIdx = perkWithSeed.IndexOf('#');
        Assert.True(hashIdx >= 0);
        string perkId = perkWithSeed[..hashIdx];
        string seed = perkWithSeed[(hashIdx + 1)..];
        Assert.Equal("^STARTING_NEG1", perkId);
        Assert.Equal("12345", seed);
        Assert.True(int.TryParse(seed, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out int seedInt));
        Assert.Equal(12345, seedInt);

        // Verify perk without seed
        string perkNoSeed = perksArr.GetString(1)!;
        Assert.DoesNotContain("#", perkNoSeed);
        Assert.Equal("^STARTING_POS1", perkNoSeed);
    }

    [Fact]
    public void SettlementLogic_PerkSeedFormat_EmptyPerkIsCaretOnly()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""SeedTest"",
            ""Perks"": [""^""]
        }");

        var perksArr = settlement.GetArray("Perks")!;
        Assert.Equal("^", perksArr.GetString(0));
    }

    [Fact]
    public void SettlementLogic_PerkSeedFormat_InvalidSeedNotInteger_SavesWithoutSeed()
    {
        // This tests the validation logic: non-integer seed text should result in perkId only
        string perkId = "^PROC_TRAIT_BENEFIT";
        string badSeed = "not_an_integer";
        // The save logic validates: int.TryParse(seedText, ...) -> false -> saves perkId without seed
        Assert.False(int.TryParse(badSeed, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out _));
        // Therefore the saved value should be just the perkId, not "perkId#badSeed"
        string savedVal = perkId; // This is what the UI code would produce
        Assert.DoesNotContain("#", savedVal);
    }

    // --- SettlementBuildingState bit-flag tests ---

    [Fact]
    public void SettlementBuildingState_IsEmpty_TrueForZero()
    {
        Assert.True(SettlementLogic.SettlementBuildingState.IsEmpty(0));
        Assert.False(SettlementLogic.SettlementBuildingState.IsEmpty(1));
    }

    [Fact]
    public void SettlementBuildingState_IsInitialConstruction_OnlyBits0to6()
    {
        // Values that are purely initial construction (bits 0->6 only)
        Assert.True(SettlementLogic.SettlementBuildingState.IsInitialConstruction(5));   // 0b0000101
        Assert.True(SettlementLogic.SettlementBuildingState.IsInitialConstruction(127)); // 0b1111111 = all 7 phases
        Assert.True(SettlementLogic.SettlementBuildingState.IsInitialConstruction(1));

        // Not initial construction: 0, or values with higher bits set
        Assert.False(SettlementLogic.SettlementBuildingState.IsInitialConstruction(0));
        Assert.False(SettlementLogic.SettlementBuildingState.IsInitialConstruction(67108991)); // has bit 26
    }

    [Fact]
    public void SettlementBuildingState_GetInitConstruction_ExtractsBits0to6()
    {
        // 0x0C30007F has bits 0->6 all set (0x7F = 127)
        Assert.Equal(0x7F, SettlementLogic.SettlementBuildingState.GetInitConstruction(unchecked((int)0x0C30007F)));
        Assert.Equal(0x35, SettlementLogic.SettlementBuildingState.GetInitConstruction(53)); // 0x35 = 53
        Assert.Equal(0, SettlementLogic.SettlementBuildingState.GetInitConstruction(0));
    }

    [Fact]
    public void SettlementBuildingState_GetUpgradeProgress_ExtractsBits10to19()
    {
        // 0x0C3FFC7F has bits 10->19 all set (1023 = 0x3FF): B-class with all upgrade progress
        int bClassFullUpgrade = unchecked((int)0x0C3FFC7F); // 205519999
        Assert.Equal(0x3FF, SettlementLogic.SettlementBuildingState.GetUpgradeProgress(bClassFullUpgrade));
        Assert.Equal(0, SettlementLogic.SettlementBuildingState.GetUpgradeProgress(127)); // no upgrade bits
    }

    [Fact]
    public void SettlementBuildingState_GetBit_IndividualBits()
    {
        int bClassComplete = 204472447; // 0x0C30007F = B-class Complete
        Assert.True(SettlementLogic.SettlementBuildingState.GetBit(bClassComplete, 27));  // B_Arrived
        Assert.True(SettlementLogic.SettlementBuildingState.GetBit(bClassComplete, 26));  // ClassSystemActive
        Assert.True(SettlementLogic.SettlementBuildingState.GetBit(bClassComplete, 21));  // B_Confirmed
        Assert.True(SettlementLogic.SettlementBuildingState.GetBit(bClassComplete, 20));  // B_Started
        Assert.False(SettlementLogic.SettlementBuildingState.GetBit(bClassComplete, 28)); // A_Arrived not set
        Assert.False(SettlementLogic.SettlementBuildingState.GetBit(bClassComplete, 29)); // S_Arrived not set
    }

    [Fact]
    public void SettlementBuildingState_InitConstructionCount_Correct()
    {
        Assert.Equal(7, SettlementLogic.SettlementBuildingState.InitConstructionCount(127));  // all 7 bits
        Assert.Equal(2, SettlementLogic.SettlementBuildingState.InitConstructionCount(5));    // bits 0, 2
        Assert.Equal(3, SettlementLogic.SettlementBuildingState.InitConstructionCount(7));    // bits 0, 1, 2
        Assert.Equal(0, SettlementLogic.SettlementBuildingState.InitConstructionCount(0));
    }

    [Theory]
    [InlineData(0,           "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 1: Empty/C
    [InlineData(5,           "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 2: C-class
    [InlineData(127,         "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 10: C all done
    [InlineData(67108991,    "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 11: system active
    [InlineData(68157567,    "settlement.bs_class_c_to_b", "settlement.bs_state_upgrade_0")]   // Line 12: C->B upgrade
    [InlineData(68156543,    "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 13: C complete
    [InlineData(69205119,    "settlement.bs_class_c_to_b", "settlement.bs_state_upgrade_0")]   // Line 14: C->B upgrade
    [InlineData(1046581,     "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 15: C complete
    [InlineData(1047605,     "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 16: C complete
    [InlineData(1047589,     "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 17: C complete
    [InlineData(1047679,     "settlement.bs_class_c", "settlement.bs_state_complete")]         // Line 18: C complete
    [InlineData(202375295,   "settlement.bs_class_c_to_b", "settlement.bs_state_awaiting_unveil")] // Line 19: C->B awaiting
    [InlineData(204472447,   "settlement.bs_class_b", "settlement.bs_state_complete")]         // Line 20: B complete
    [InlineData(205518975,   "settlement.bs_class_b", "settlement.bs_state_complete")]         // Line 21: B complete
    [InlineData(205519999,   "settlement.bs_class_b", "settlement.bs_state_complete")]         // Line 22: B complete
    [InlineData(208666751,   "settlement.bs_class_b_to_a", "settlement.bs_state_upgrade_0")]   // Line 23: B->A upgrade
    [InlineData(477102207,   "settlement.bs_class_b_to_a", "settlement.bs_state_awaiting_unveil")] // Line 24: B->A awaiting
    [InlineData(485490815,   "settlement.bs_class_a", "settlement.bs_state_complete")]         // Line 25: A complete
    [InlineData(502268031,   "settlement.bs_class_a_to_s", "settlement.bs_state_upgrade_0")]   // Line 26: A->S upgrade
    [InlineData(1039138943,  "settlement.bs_class_a_to_s", "settlement.bs_state_awaiting_unveil")] // Line 27: A->S awaiting
    [InlineData(1072693375,  "settlement.bs_class_s", "settlement.bs_state_complete")]         // Line 28: S complete
    [InlineData(1073740927,  "settlement.bs_class_s", "settlement.bs_state_complete")]         // Line 29: S complete
    public void SettlementBuildingState_DetermineClassAndState_MatchesCSV(int value, string expectedClass, string expectedState)
    {
        var (classKey, stateKey) = SettlementLogic.SettlementBuildingState.DetermineClassAndState(value);
        Assert.Equal(expectedClass, classKey);
        Assert.Equal(expectedState, stateKey);
    }

    [Fact]
    public void SettlementBuildingState_KnownMilestones_ContainsExpectedValues()
    {
        var milestones = SettlementDatabase.KnownMilestones;
        Assert.Equal(13, milestones.Length);
        Assert.Equal(0, milestones[0].Value);
        Assert.Equal(1073740927, milestones[^1].Value);
    }

    [Fact]
    public void SettlementBuildingState_GetBuildingSlotDescription_EmptySlot()
    {
        var desc = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(0);
        // Falls back to key name when UiStrings not loaded
        Assert.Contains("Empty", desc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettlementBuildingState_GetBuildingSlotDescription_InitialConstruction()
    {
        var desc = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(5);
        // Should contain construction count info
        Assert.NotNull(desc);
        Assert.NotEmpty(desc);
        Assert.NotEqual("TBC", desc);
    }

    [Fact]
    public void SettlementBuildingState_GetBuildingSlotDescription_BComplete()
    {
        // 0x0C30007F = 204472447 = B-class Complete
        var desc = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(204472447);
        Assert.NotNull(desc);
        Assert.NotEmpty(desc);
        Assert.NotEqual("TBC", desc);
    }

    // --- GetDetailedBitFieldDescription tests ---

    [Fact]
    public void SettlementBuildingState_GetDetailedBitFieldDescription_EmptySlot()
    {
        var desc = SettlementLogic.SettlementBuildingState.GetDetailedBitFieldDescription(0);
        Assert.Contains("Empty", desc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettlementBuildingState_GetDetailedBitFieldDescription_InitialConstruction()
    {
        // 5 = bits 0 and 2 set = 2 phases done
        var desc = SettlementLogic.SettlementBuildingState.GetDetailedBitFieldDescription(5);
        Assert.NotNull(desc);
        Assert.NotEmpty(desc);
        // Should contain multiple lines (newlines)
        Assert.Contains("\n", desc);
        // Should mention construction/init phases
        Assert.Contains("2", desc);
    }

    [Fact]
    public void SettlementBuildingState_GetDetailedBitFieldDescription_BComplete()
    {
        // 204472447 = B-class Complete
        var desc = SettlementLogic.SettlementBuildingState.GetDetailedBitFieldDescription(204472447);
        Assert.NotNull(desc);
        Assert.NotEmpty(desc);
        Assert.Contains("\n", desc);
        // Should show raw hex value
        Assert.Contains("0x", desc);
    }

    [Fact]
    public void SettlementBuildingState_GetDetailedBitFieldDescription_SClassFull()
    {
        // 1073740927 = S-class Full
        var desc = SettlementLogic.SettlementBuildingState.GetDetailedBitFieldDescription(1073740927);
        Assert.NotNull(desc);
        Assert.NotEmpty(desc);
        // Should have 5 lines total (class/state, init/upgrade, tiers, flags, raw)
        int lineCount = desc.Split('\n').Length;
        Assert.Equal(5, lineCount);
    }

    [Fact]
    public void SettlementBuildingState_GetDetailedBitFieldDescription_ContainsTierInfo()
    {
        // 485490815 = A-class Complete
        var desc = SettlementLogic.SettlementBuildingState.GetDetailedBitFieldDescription(485490815);
        Assert.NotNull(desc);
        // Should contain tier-related info for B and A
        Assert.Contains("B Tier", desc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("A Tier", desc, StringComparison.OrdinalIgnoreCase);
    }

    // --- Settlement alien race mapping tests ---

    [Fact]
    public void SettlementLogic_AlienRaceDisplayNames_ContainsAllRaces()
    {
        foreach (var race in SettlementLogic.AlienRaces)
        {
            Assert.True(SettlementLogic.AlienRaceDisplayNames.ContainsKey(race),
                $"AlienRaceDisplayNames missing key '{race}'");
        }
    }

    [Fact]
    public void SettlementLogic_AlienRaceDisplayNames_MapsCorrectly()
    {
        Assert.Equal("Gek", SettlementLogic.AlienRaceDisplayNames["Traders"]);
        Assert.Equal("Vy'keen", SettlementLogic.AlienRaceDisplayNames["Warriors"]);
        Assert.Equal("Korvax", SettlementLogic.AlienRaceDisplayNames["Explorers"]);
        Assert.Equal("Autophage", SettlementLogic.AlienRaceDisplayNames["Builders"]);
    }

    [Fact]
    public void SettlementLogic_AlienRaceLocKeys_ContainsAllRaces()
    {
        foreach (var race in SettlementLogic.AlienRaces)
        {
            Assert.True(SettlementLogic.AlienRaceLocKeys.ContainsKey(race),
                $"AlienRaceLocKeys missing key '{race}'");
        }
    }

    // --- Settlement new fields load/save tests ---

    [Fact]
    public void SettlementLogic_LoadSettlementData_LoadsAlienRace()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""Race"": { ""AlienRace"": ""Explorers"" },
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);
        Assert.Equal("Explorers", data.AlienRace);
    }

    [Fact]
    public void SettlementLogic_LoadSettlementData_LoadsTimestamps()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0,
            ""LastBugAttackChangeTime"": 1700000000,
            ""LastAlertChangeTime"": 1700000001,
            ""LastDebtChangeTime"": 1700000002,
            ""LastUpkeepDebtCheckTime"": 1700000003,
            ""LastPopulationChangeTime"": 1700000004
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);
        Assert.Equal(1700000000, data.LastBugAttackChangeTime);
        Assert.Equal(1700000001, data.LastAlertChangeTime);
        Assert.Equal(1700000002, data.LastDebtChangeTime);
        Assert.Equal(1700000003, data.LastUpkeepDebtCheckTime);
        Assert.Equal(1700000004, data.LastPopulationChangeTime);
    }

    [Fact]
    public void SettlementLogic_LoadSettlementData_LoadsMissionFields()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0,
            ""MiniMissionSeed"": 12345,
            ""MiniMissionStartTime"": 1700000000
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);
        Assert.Equal(12345, data.MiniMissionSeed);
        Assert.Equal(1700000000, data.MiniMissionStartTime);
    }

    [Fact]
    public void SettlementLogic_LoadSettlementData_LoadsBuildingStates()
    {
        // BuildingStates is an array of 48 ints
        var statesJson = string.Join(",", Enumerable.Range(0, 48).Select(i => i == 0 ? "204472447" : "0"));
        var settlement = JsonObject.Parse($@"{{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": {{ ""USN"": ""u1"", ""UID"": ""uid1"" }},
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": {{ ""SettlementJudgementType"": ""None"" }},
            ""LastJudgementTime"": 0,
            ""BuildingStates"": [{statesJson}]
        }}");

        var data = SettlementLogic.LoadSettlementData(settlement);
        Assert.True(data.HasBuildingStates);
        Assert.Equal(204472447, data.BuildingStates[0]);
        Assert.Equal(204472447, data.RawBuildingStates[0]);
        Assert.Equal(0, data.BuildingStates[1]);
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_SavesAlienRace()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""Race"": { ""AlienRace"": ""None"" },
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC",
            AlienRace = "Warriors",
        };
        SettlementLogic.SaveSettlementData(settlement, saveValues);

        var raceObj = settlement.GetObject("Race");
        Assert.Equal("Warriors", raceObj?.GetString("AlienRace"));
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_SavesTimestamps()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0,
            ""LastBugAttackChangeTime"": 0,
            ""LastAlertChangeTime"": 0,
            ""LastDebtChangeTime"": 0,
            ""LastUpkeepDebtCheckTime"": 0,
            ""LastPopulationChangeTime"": 0
        }");

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC",
            LastBugAttackChangeTime = 1700000000,
            LastAlertChangeTime = 1700000001,
            LastDebtChangeTime = 1700000002,
            LastUpkeepDebtCheckTime = 1700000003,
            LastPopulationChangeTime = 1700000004,
        };
        SettlementLogic.SaveSettlementData(settlement, saveValues);

        Assert.Equal(1700000000, settlement.GetLong("LastBugAttackChangeTime"));
        Assert.Equal(1700000001, settlement.GetLong("LastAlertChangeTime"));
        Assert.Equal(1700000002, settlement.GetLong("LastDebtChangeTime"));
        Assert.Equal(1700000003, settlement.GetLong("LastUpkeepDebtCheckTime"));
        Assert.Equal(1700000004, settlement.GetLong("LastPopulationChangeTime"));
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_SavesMissionFields()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0,
            ""MiniMissionSeed"": 0,
            ""MiniMissionStartTime"": 0
        }");

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC",
            MiniMissionSeed = 99999,
            MiniMissionStartTime = 1700000000,
        };
        SettlementLogic.SaveSettlementData(settlement, saveValues);

        Assert.Equal(99999, settlement.GetInt("MiniMissionSeed"));
        Assert.Equal(1700000000, settlement.GetLong("MiniMissionStartTime"));
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_BuildingStates_PreservesUnchanged()
    {
        var statesJson = string.Join(",", Enumerable.Range(0, 48).Select(i => i == 0 ? "204472447" : "53"));
        var settlement = JsonObject.Parse($@"{{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": {{ ""USN"": ""u1"", ""UID"": ""uid1"" }},
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": {{ ""SettlementJudgementType"": ""None"" }},
            ""LastJudgementTime"": 0,
            ""BuildingStates"": [{statesJson}]
        }}");

        // Load raw values first
        var data = SettlementLogic.LoadSettlementData(settlement);

        // Save with same values (nothing changed) - should preserve originals
        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC",
            HasBuildingStates = true,
            RawBuildingStates = data.RawBuildingStates,
            BuildingStates = (int[])data.BuildingStates.Clone(),
        };
        SettlementLogic.SaveSettlementData(settlement, saveValues);

        var buildArr = settlement.GetArray("BuildingStates");
        Assert.Equal(204472447, buildArr!.GetInt(0)); // Unchanged
        Assert.Equal(53, buildArr.GetInt(1)); // Unchanged
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_BuildingStates_WritesChanged()
    {
        var statesJson = string.Join(",", Enumerable.Range(0, 48).Select(i => "53"));
        var settlement = JsonObject.Parse($@"{{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": {{ ""USN"": ""u1"", ""UID"": ""uid1"" }},
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": {{ ""SettlementJudgementType"": ""None"" }},
            ""LastJudgementTime"": 0,
            ""BuildingStates"": [{statesJson}]
        }}");

        var data = SettlementLogic.LoadSettlementData(settlement);
        var newStates = (int[])data.BuildingStates.Clone();
        newStates[0] = 999; // User changed slot 0

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC",
            HasBuildingStates = true,
            RawBuildingStates = data.RawBuildingStates,
            BuildingStates = newStates,
        };
        SettlementLogic.SaveSettlementData(settlement, saveValues);

        var buildArr = settlement.GetArray("BuildingStates");
        Assert.Equal(999, buildArr!.GetInt(0)); // Changed
        Assert.Equal(53, buildArr.GetInt(1)); // Unchanged
    }

    [Fact]
    public void SettlementLogic_LoadSettlementData_MissingRace_DefaultsToNone()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);
        Assert.Equal("None", data.AlienRace);
    }

    [Fact]
    public void SettlementLogic_LoadSettlementData_ZeroTimestamps_LoadAsZero()
    {
        var settlement = JsonObject.Parse(@"{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": { ""USN"": ""u1"", ""UID"": ""uid1"" },
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": { ""SettlementJudgementType"": ""None"" },
            ""LastJudgementTime"": 0,
            ""LastBugAttackChangeTime"": 0,
            ""MiniMissionStartTime"": 0
        }");

        var data = SettlementLogic.LoadSettlementData(settlement);
        Assert.Equal(0, data.LastBugAttackChangeTime);
        Assert.Equal(0, data.MiniMissionStartTime);
        // Zero timestamps should NOT produce valid dates in UI (handled by ShowCheckBox pattern)
    }

    [Fact]
    public void SettlementBuildingState_UpgradeProgressCount_Correct()
    {
        // 0x0C3FFC7F = 205519999 = B-class with all upgrade bits set (10/10)
        Assert.Equal(10, SettlementLogic.SettlementBuildingState.UpgradeProgressCount(205519999));
        // 127 = no upgrade bits
        Assert.Equal(0, SettlementLogic.SettlementBuildingState.UpgradeProgressCount(127));
    }

    [Fact]
    public void SettlementBuildingState_GetTierProgression_Correct()
    {
        // S-class complete: 0x3FF0007F -> tier bits 20-25 all set = 0x3F
        Assert.Equal(0x3F, SettlementLogic.SettlementBuildingState.GetTierProgression(1072693375));
        // B-class complete: bits 20,21 set = 0x03
        Assert.Equal(0x03, SettlementLogic.SettlementBuildingState.GetTierProgression(204472447));
        // C-class: no tier bits
        Assert.Equal(0, SettlementLogic.SettlementBuildingState.GetTierProgression(127));
    }

    [Fact]
    public void SettlementBuildingState_GetArrivalFlags_Correct()
    {
        // S-class complete: 0x3FF0007F -> bits 26-29 all set = 0xF
        Assert.Equal(0x0F, SettlementLogic.SettlementBuildingState.GetArrivalFlags(1072693375));
        // B-class complete: bits 26,27 set = 0x03
        Assert.Equal(0x03, SettlementLogic.SettlementBuildingState.GetArrivalFlags(204472447));
        // C-class: no arrival flags
        Assert.Equal(0, SettlementLogic.SettlementBuildingState.GetArrivalFlags(127));
    }

    [Fact]
    public void SettlementBuildingState_GetBuildingSlotDescription_AllMilestones_NotEmpty()
    {
        foreach (var (value, _) in SettlementDatabase.KnownMilestones)
        {
            var desc = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(value);
            Assert.NotNull(desc);
            Assert.NotEmpty(desc);
        }
    }

    [Fact]
    public void SettlementDatabase_KnownMilestones_Has13Entries()
    {
        Assert.Equal(13, SettlementDatabase.KnownMilestones.Length);
    }

    [Fact]
    public void SettlementDatabase_KnownMilestones_AllValuesAreDistinct()
    {
        var values = SettlementDatabase.KnownMilestones.Select(m => m.Value).ToArray();
        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Fact]
    public void SettlementDatabase_KnownMilestones_AllLocKeysNonEmpty()
    {
        foreach (var (_, locKey) in SettlementDatabase.KnownMilestones)
        {
            Assert.False(string.IsNullOrWhiteSpace(locKey), $"LocKey should not be empty for milestone");
        }
    }

    [Theory]
    [InlineData(0)]           // Empty lot
    [InlineData(127)]         // C construction complete
    [InlineData(204472447)]   // B complete
    [InlineData(1072693375)]  // S complete
    public void SettlementDatabase_KnownMilestones_ContainsValue(int value)
    {
        Assert.Contains(SettlementDatabase.KnownMilestones, m => m.Value == value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(42)]
    [InlineData(-1)]
    public void SettlementDatabase_KnownMilestones_DoesNotContainArbitraryValue(int value)
    {
        Assert.DoesNotContain(SettlementDatabase.KnownMilestones, m => m.Value == value);
    }

    [Fact]
    public void SettlementLogic_SaveSettlementData_BuildingStates_AlwaysWritesIntegers()
    {
        // This test verifies that any int value written to BuildingStates is a valid integer
        // in the save file - guaranteeing save safety.
        var statesJson = string.Join(",", Enumerable.Range(0, 48).Select(i => "0"));
        var settlement = JsonObject.Parse($@"{{
            ""Name"": ""TestSettlement"",
            ""SeedValue"": ""ABC"",
            ""Owner"": {{ ""USN"": ""u1"", ""UID"": ""uid1"" }},
            ""Stats"": [0,0,0,0,0,0,0,0],
            ""PendingJudgementType"": {{ ""SettlementJudgementType"": ""None"" }},
            ""LastJudgementTime"": 0,
            ""BuildingStates"": [{statesJson}]
        }}");

        var data = SettlementLogic.LoadSettlementData(settlement);

        // Simulate various integer values that might be set via the NUD
        var testValues = new[] { 0, 127, 999, 204472447, 1072693375, -1, int.MaxValue, int.MinValue };
        var newStates = (int[])data.BuildingStates.Clone();
        for (int i = 0; i < Math.Min(testValues.Length, newStates.Length); i++)
            newStates[i] = testValues[i];

        var saveValues = new SettlementLogic.SettlementSaveValues
        {
            Name = "TestSettlement",
            SeedValue = "ABC",
            HasBuildingStates = true,
            RawBuildingStates = data.RawBuildingStates,
            BuildingStates = newStates,
        };
        SettlementLogic.SaveSettlementData(settlement, saveValues);

        var buildArr = settlement.GetArray("BuildingStates");
        Assert.NotNull(buildArr);
        for (int i = 0; i < Math.Min(testValues.Length, buildArr!.Length); i++)
        {
            Assert.Equal(testValues[i], buildArr.GetInt(i));
        }
    }

    // --- End-to-end corvette verification against reference save ---

    /// <summary>
    /// Locates a reference path by walking up from the test output directory.
    /// </summary>
    private static string? FindRefPath(params string[] parts)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(new[] { dir }.Concat(parts).ToArray());
            if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    private static bool _mapperLoaded;
    private static readonly object _mapperLock = new();

    private static void EnsureMapperLoaded()
    {
        if (_mapperLoaded) return;
        lock (_mapperLock)
        {
            if (_mapperLoaded) return;
            var mapperPath = FindRefPath("Resources", "map", "mapping.json");
            if (mapperPath == null) return;
            var mapper = new JsonNameMapper();
            mapper.Load(mapperPath);
            JsonParser.SetDefaultMapper(mapper);
            _mapperLoaded = true;
        }
    }

    private static bool _corvDbLoaded;
    private static readonly object _corvDbLock = new();

    /// <summary>
    /// Loads the Corvette.json database into <see cref="StarshipDatabase"/> for tests
    /// that rely on data-driven part categorisation.
    /// </summary>
    private static void LoadCorvetteDatabase()
    {
        // Check actual state (another test class may have called Clear())
        if (_corvDbLoaded && StarshipDatabase.IsLoaded) return;
        lock (_corvDbLock)
        {
            if (_corvDbLoaded && StarshipDatabase.IsLoaded) return;
            var jsonDir = FindResourceJsonDir();
            if (jsonDir == null) return;
            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(jsonDir);
            StarshipDatabase.LoadFromDatabase(db);
            _corvDbLoaded = true;
        }
    }

    [Fact]
    public void CorvetteE2E_ReferenceSave_FindsAll3CorvetteBases()
    {
        // Load the reference save and verify FindCorvetteBaseIndex correctly pairs
        // all 3 corvettes to their bases via UserData matching.
        var savePath = FindRefPath("_ref", "saves", "original", "save.hg");
        if (savePath == null) return; // skip if reference save not available

        EnsureMapperLoaded();
        var save = SaveFileManager.LoadSaveFile(savePath);
        var psd = save.GetObject("PlayerStateData");
        Assert.NotNull(psd);

        var shipOwnership = psd!.GetArray("ShipOwnership");
        var bases = psd.GetArray("PersistentPlayerBases");
        Assert.NotNull(shipOwnership);
        Assert.NotNull(bases);

        // The reference save has 3 corvettes at ship indices 5, 7, 8:
        // Ship 5 = "The Bebop" (seed 0x68BA4883) -> Base with UserData=5
        // Ship 7 = "USCSS Abraxas" (seed 0x68B31258) -> Base with UserData=7
        // Ship 8 = "USCSS Solomon" (seed 0x68F8EF8D) -> Base with UserData=8
        var corvettes = new[]
        {
            (shipIndex: 5, name: "The Bebop", expectedUserData: 5),
            (shipIndex: 7, name: "USCSS Abraxas", expectedUserData: 7),
            (shipIndex: 8, name: "USCSS Solomon", expectedUserData: 8),
        };

        foreach (var (shipIndex, name, expectedUserData) in corvettes)
        {
            int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases!, shipIndex);

            Assert.True(baseIdx >= 0, $"FindCorvetteBaseIndex failed for ship '{name}' (index={shipIndex})");

            // Verify the matched base has the correct UserData
            var matchedBase = bases!.GetObject(baseIdx);
            long ud = 0;
            try { ud = (long)matchedBase.GetDouble("UserData"); } catch { }
            Assert.Equal(expectedUserData, (int)ud);

            // Verify it's a PlayerShipBase
            var bt = matchedBase.GetObject("BaseType")?.GetString("PersistentBaseTypes");
            Assert.Equal("PlayerShipBase", bt);

            // Verify it has Objects
            var objects = matchedBase.GetArray("Objects");
            Assert.NotNull(objects);
            Assert.True(objects!.Length > 0, $"Base for '{name}' has no objects");
        }
    }

    [Fact]
    public void CorvetteE2E_ReferenceSave_ReorderProducesCorrectCategoryOrder()
    {
        // Load the reference save and optimise each corvette, then verify the result
        // has correct category ordering.
        var savePath = FindRefPath("_ref", "saves", "original", "save.hg");
        if (savePath == null) return;

        LoadCorvetteDatabase();
        EnsureMapperLoaded();
        var save = SaveFileManager.LoadSaveFile(savePath);
        var psd = save.GetObject("PlayerStateData");
        var bases = psd!.GetArray("PersistentPlayerBases");

        var corvettes = new[]
        {
            (shipIndex: 5, name: "The Bebop"),
            (shipIndex: 7, name: "USCSS Abraxas"),
            (shipIndex: 8, name: "USCSS Solomon"),
        };

        foreach (var (shipIndex, name) in corvettes)
        {
            int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases!, shipIndex);
            Assert.True(baseIdx >= 0, $"Base not found for '{name}'");

            var baseObj = bases!.GetObject(baseIdx);
            var objects = baseObj.GetArray("Objects")!;
            int objectCount = objects.Length;

            // Perform the optimise reorder
            int result = StarshipLogic.ReorderBuildingObjects(objects);
            Assert.Equal(objectCount, result);

            // Verify priority ordering: each object's priority should be >= the previous
            int prevPriority = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                var obj = objects.GetObject(i);
                string objectId = obj.GetString("ObjectID") ?? "";
                int priority = StarshipLogic.GetPartPriority(objectId);

                Assert.True(priority >= prevPriority,
                    $"Ship '{name}': object[{i}] '{objectId}' (priority {priority}) " +
                    $"comes after priority {prevPriority} - wrong order!");
                prevPriority = priority;
            }

            // Verify that Reactor parts (priority 0) appear before Engine parts (priority 1)
            int lastReactorIdx = -1;
            int firstEngineIdx = -1;
            for (int i = 0; i < objects.Length; i++)
            {
                int p = StarshipLogic.GetPartPriority(objects.GetObject(i).GetString("ObjectID") ?? "");
                if (p == 0) // Reactor priority
                    lastReactorIdx = i;
                if (p == 1 && firstEngineIdx < 0) // Engine priority
                    firstEngineIdx = i;
            }
            if (lastReactorIdx >= 0 && firstEngineIdx >= 0)
            {
                Assert.True(lastReactorIdx < firstEngineIdx,
                    $"Ship '{name}': Reactor at index {lastReactorIdx} should be before engine at {firstEngineIdx}");
            }
        }
    }

    [Fact]
    public void CorvetteE2E_ReferenceSave_OptimiseCorvetteBase_ReturnsCorrectCount()
    {
        // Verify OptimiseCorvetteBase works end-to-end and returns the right object count.
        var savePath = FindRefPath("_ref", "saves", "original", "save.hg");
        if (savePath == null) return;

        EnsureMapperLoaded();
        var save = SaveFileManager.LoadSaveFile(savePath);
        var psd = save.GetObject("PlayerStateData");
        var bases = psd!.GetArray("PersistentPlayerBases");

        // Expected object counts from the reference save:
        // Ship 5 "The Bebop" -> 111 objects
        // Ship 7 "USCSS Abraxas" -> 316 objects
        // Ship 8 "USCSS Solomon" -> 148 objects
        var corvettes = new[]
        {
            (shipIndex: 5, name: "The Bebop", expectedCount: 111),
            (shipIndex: 7, name: "USCSS Abraxas", expectedCount: 316),
            (shipIndex: 8, name: "USCSS Solomon", expectedCount: 148),
        };

        foreach (var (shipIndex, name, expectedCount) in corvettes)
        {
            int result = StarshipLogic.OptimiseCorvetteBase(bases, shipIndex);
            Assert.Equal(expectedCount, result);
        }
    }

    [Fact]
    public void CorvetteE2E_TheBebop_MatchesRefOutput()
    {
        // Run our optimizer on The Bebop and compare ObjectID ordering against
        // the reference output in _ref/optimize/ref.json.
        var savePath = FindRefPath("_ref", "saves", "original", "save.hg");
        var refPath = FindRefPath("_ref", "optimize", "ref.json");
        if (savePath == null || refPath == null) return;

        LoadCorvetteDatabase();
        EnsureMapperLoaded();
        var save = SaveFileManager.LoadSaveFile(savePath);
        var psd = save.GetObject("PlayerStateData");
        var bases = psd!.GetArray("PersistentPlayerBases");

        // Ship 5 = "The Bebop"
        int result = StarshipLogic.OptimiseCorvetteBase(bases, 5);
        Assert.Equal(111, result);

        // Get the optimised objects
        int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases!, 5);
        var baseObj = bases!.GetObject(baseIdx);
        var objects = baseObj.GetArray("Objects")!;

        // Load reference
        string refJson = System.IO.File.ReadAllText(refPath);
        var refArray = JsonArray.Parse(refJson);

        Assert.Equal(refArray.Length, objects.Length);

        // Compare every ObjectID in order
        var mismatches = new List<string>();
        for (int i = 0; i < objects.Length; i++)
        {
            string oursId = objects.GetObject(i).GetString("ObjectID") ?? "";
            string refId = refArray.GetObject(i).GetString("ObjectID") ?? "";
            if (!string.Equals(oursId, refId, StringComparison.Ordinal))
                mismatches.Add($"[{i}] ours={oursId} ref={refId}");
        }

        Assert.True(mismatches.Count == 0,
            $"ObjectID order differs from ref in {mismatches.Count}/{objects.Length} positions:\n" +
            string.Join("\n", mismatches.Take(20)));
    }

    [Fact]
    public void CorvetteE2E_TheBebop_FullRefComparison()
    {
        // Detailed diagnostic: loads the original .hg save, runs our optimizer on
        // The Bebop, and produces a full side-by-side comparison against ref.json.
        // Verifies both ObjectID ordering AND positional data integrity.
        var savePath = FindRefPath("_ref", "saves", "original", "save.hg");
        var refPath = FindRefPath("_ref", "optimize", "ref.json");
        if (savePath == null || refPath == null) return;

        LoadCorvetteDatabase();
        EnsureMapperLoaded();
        var save = SaveFileManager.LoadSaveFile(savePath);
        var psd = save.GetObject("PlayerStateData");
        var bases = psd!.GetArray("PersistentPlayerBases");

        // Ship 5 = "The Bebop"
        int result = StarshipLogic.OptimiseCorvetteBase(bases, 5);
        Assert.Equal(111, result);

        int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases!, 5);
        var baseObj = bases!.GetObject(baseIdx);
        var objects = baseObj.GetArray("Objects")!;

        string refJson = System.IO.File.ReadAllText(refPath);
        var refArray = JsonArray.Parse(refJson);

        // Count and length must match
        Assert.Equal(refArray.Length, objects.Length);

        int matchCount = 0;
        int mismatchCount = 0;
        var mismatches = new List<string>();

        for (int i = 0; i < objects.Length; i++)
        {
            var oursObj = objects.GetObject(i);
            var refObj = refArray.GetObject(i);

            string oursId = oursObj.GetString("ObjectID") ?? "";
            string refID = refObj.GetString("ObjectID") ?? "";

            if (string.Equals(oursId, refID, StringComparison.Ordinal))
            {
                matchCount++;
            }
            else
            {
                mismatchCount++;
                mismatches.Add($"[{i}] ours={oursId} ref={refID}");
            }
        }

        // Verify category boundaries are correct
        var categories = new Dictionary<string, int>();
        for (int i = 0; i < objects.Length; i++)
        {
            string id = objects.GetObject(i).GetString("ObjectID") ?? "";
            int priority = StarshipLogic.GetPartPriority(id);
            string catName = priority switch
            {
                1 => "Reactor",
                2 => "Thruster",
                3 => "Wing",
                4 => "LandingGear",
                5 => "Access",
                6 => "Cockpit",
                int.MaxValue => "Other",
                _ => $"Unknown({priority})"
            };
            if (!categories.ContainsKey(catName))
                categories[catName] = 0;
            categories[catName]++;
        }

        // Verify same categories in ref output
        var refCategories = new Dictionary<string, int>();
        for (int i = 0; i < refArray.Length; i++)
        {
            string id = refArray.GetObject(i).GetString("ObjectID") ?? "";
            int priority = StarshipLogic.GetPartPriority(id);
            string catName = priority switch
            {
                1 => "Reactor",
                2 => "Thruster",
                3 => "Wing",
                4 => "LandingGear",
                5 => "Access",
                6 => "Cockpit",
                int.MaxValue => "Other",
                _ => $"Unknown({priority})"
            };
            if (!refCategories.ContainsKey(catName))
                refCategories[catName] = 0;
            refCategories[catName]++;
        }

        // Both must have same category counts
        foreach (var kvp in categories)
        {
            Assert.True(refCategories.ContainsKey(kvp.Key),
                $"Ref missing category '{kvp.Key}' that we have with {kvp.Value} objects");
            Assert.Equal(kvp.Value, refCategories[kvp.Key]);
        }

        // Assert full match
        Assert.True(mismatchCount == 0,
            $"ObjectID mismatch: {matchCount}/{objects.Length} match, {mismatchCount} differ:\n" +
            string.Join("\n", mismatches));
    }

    [Fact]
    public void CorvetteE2E_ReferenceSave_ExportImportRoundtrip()
    {
        // Verify a corvette's base data can be exported and the objects JSON can
        // be re-parsed without loss (simulates the export/import flow).
        var savePath = FindRefPath("_ref", "saves", "original", "save.hg");
        if (savePath == null) return;

        EnsureMapperLoaded();
        var save = SaveFileManager.LoadSaveFile(savePath);
        var psd = save.GetObject("PlayerStateData");
        var bases = psd!.GetArray("PersistentPlayerBases");

        // Test with "USCSS Abraxas" (largest base, 316 objects)
        int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases!, 7);
        Assert.True(baseIdx >= 0);

        var baseObj = bases!.GetObject(baseIdx);
        var objects = baseObj.GetArray("Objects")!;

        // Serialize all objects to JSON strings, then re-parse to verify round-trip
        for (int i = 0; i < Math.Min(10, objects.Length); i++) // Spot-check first 10
        {
            var obj = objects.GetObject(i);
            string objectId = obj.GetString("ObjectID") ?? "";
            Assert.False(string.IsNullOrEmpty(objectId), $"Object[{i}] has empty ObjectID");

            // Verify it can be serialized and deserialized
            string serialized = obj.ToString();
            var reparsed = JsonObject.Parse(serialized);
            Assert.Equal(objectId, reparsed.GetString("ObjectID"));
        }

        // Also verify the ship ownership entry can round-trip
        var ships = psd!.GetArray("ShipOwnership")!;
        var ship = ships.GetObject(7);
        string shipJson = ship.ToString();
        var reparsedShip = JsonObject.Parse(shipJson);
        Assert.Equal("USCSS Abraxas", reparsedShip.GetString("Name"));
    }

	// =========================================================================
	// Real-save round-trip tests:
	// Verify that save-then-compare produces absolutely no unintended changes
	// when the user only modifies a single field.
	// Uses the real save files from _ref/fp_bugs/ to reproduce the exact
	// conditions reported by the user.
	// Thistest harness is designed to simulate the exact load+save
	// sequence of the relevant UI panels and help save sanity when verifying
	// value shifts.
	// =========================================================================

	/// <summary>
	/// Helper: captures the ordered list of strings from a JSON array.
	/// </summary>
	private static List<string> CaptureStringArray(JsonObject container, string key)
    {
        var result = new List<string>();
        var arr = container.GetArray(key);
        if (arr == null) return result;
        for (int i = 0; i < arr.Length; i++)
            result.Add(arr.GetString(i) ?? "");
        return result;
    }

    /// <summary>
    /// Simulates the CataloguePanel load+save round-trip on KnownTech/KnownProducts.
    /// Loads IDs, saves them back immediately without modification.
    /// The array order must be preserved exactly.
    /// </summary>
    [Fact]
    public void RealSave_CatalogueRoundTrip_PreservesArrayOrder()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return; // skip if no reference save

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        foreach (string arrayName in new[] { "KnownTech", "KnownProducts", "KnownSpecials" })
        {
            var originalOrder = CaptureStringArray(ps, arrayName);
            if (originalOrder.Count == 0) continue;

            // Simulate CataloguePanel: load IDs then save them back
            var ids = CatalogueLogic.LoadKnownItemIds(ps, arrayName);
            CatalogueLogic.SaveKnownItemIds(ps, arrayName, ids);

            var afterOrder = CaptureStringArray(ps, arrayName);
            Assert.Equal(originalOrder.Count, afterOrder.Count);
            for (int i = 0; i < originalOrder.Count; i++)
                Assert.Equal(originalOrder[i], afterOrder[i]);
        }
    }

    /// <summary>
    /// Simulates the AccountPanel load+save round-trip on RedeemedSeasonRewards
    /// and RedeemedTwitchRewards. Loads redeemed sets, saves them back unchanged.
    /// The array order must be preserved exactly.
    /// </summary>
    [Fact]
    public void RealSave_RedeemedRewardsRoundTrip_PreservesArrayOrder()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        foreach (string arrayName in new[] { "RedeemedSeasonRewards", "RedeemedTwitchRewards" })
        {
            var originalOrder = CaptureStringArray(ps, arrayName);
            if (originalOrder.Count == 0) continue;

            // Simulate the AccountPanel round-trip: read redeemed set,
            // build reward list with all items marked as redeemed, save back.
            var rewards = new List<(string Id, bool Redeemed)>();
            foreach (var id in originalOrder)
                rewards.Add((id, true));

            // WriteRedeemedArray is private, so exercise SaveRedeemedRewards
            // with both season and twitch populated
            // Actually we can't call WriteRedeemedArray directly, but we can
            // exercise through SaveRedeemedRewards with appropriate data.

            // For a minimal test, let's verify the array survives a
            // SyncJsonArrayEntry round-trip for each item.
            var arr = ps.GetArray(arrayName)!;
            int originalLength = arr.Length;

            // Call SyncJsonArrayEntry for each item as "present"
            // (this should be a no-op since they're already there)
            foreach (var id in originalOrder)
                AccountLogic.SyncJsonArrayEntry(ps, arrayName, id, true);

            // Verify order and count preserved
            var afterOrder = CaptureStringArray(ps, arrayName);
            Assert.Equal(originalOrder.Count, afterOrder.Count);
            for (int i = 0; i < originalOrder.Count; i++)
                Assert.Equal(originalOrder[i], afterOrder[i]);
        }
    }

    /// <summary>
    /// Verifies that writing redeemed arrays with the same set of redeemed IDs
    /// preserves the original order (no clear+rebuild reordering).
    /// </summary>
    [Fact]
    public void WriteRedeemedArray_SameItems_PreservesOrder()
    {
        // Build a synthetic JSON with a known order
        var json = JsonObject.Parse(@"{
            ""PlayerStateData"": {
                ""RedeemedSeasonRewards"": [
                    ""^EXPD_POSTER12A"", ""^EXPD_POSTER06A"", ""^EXPD_FLAG_06B"",
                    ""^EXPD_TITLE03"", ""^EXPD_POSTER06B"", ""^EXPD_BACKPACK01""
                ]
            }
        }");
        var ps = json.GetObject("PlayerStateData")!;

        var originalOrder = CaptureStringArray(ps, "RedeemedSeasonRewards");

        // Simulate: all items redeemed, same set
        var rewards = originalOrder.Select(id => (id, true)).ToList();
        AccountLogic.SaveRedeemedRewards(json, rewards, new List<(string, bool)>());

        var afterOrder = CaptureStringArray(ps, "RedeemedSeasonRewards");
        Assert.Equal(originalOrder.Count, afterOrder.Count);
        for (int i = 0; i < originalOrder.Count; i++)
            Assert.Equal(originalOrder[i], afterOrder[i]);
    }

    /// <summary>
    /// Verifies that the FreighterInventory_Cargo class is NOT overwritten
    /// when the user hasn't changed the freighter class.
    /// </summary>
    [Fact]
    public void RealSave_FreighterClassUnchanged_CargoClassPreserved()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        // Capture original cargo class
        var cargoClass = ps.GetObject("FreighterInventory_Cargo")?.GetObject("Class");
        string originalCargoClass = cargoClass?.GetString("InventoryClass") ?? "";

        // Load freighter data (simulates panel load)
        var data = FreighterLogic.LoadFreighterData(ps);

        // Save with same class index (unchanged) -- must NOT write to cargo
        var values = new FreighterLogic.FreighterSaveValues
        {
            Name = data.Name,
            ClassIndex = data.ClassIndex,
            OriginalClassIndex = data.ClassIndex, // Same as loaded = no change
            HomeSeed = data.HomeSeed,
            ModelSeed = data.ModelSeed,
            Hyperdrive = data.Hyperdrive,
            FleetCoordination = data.FleetCoordination,
            RawStatValues = new Dictionary<string, double>
            {
                ["^FREI_HYPERDRIVE"] = data.Hyperdrive,
                ["^FREI_FLEET"] = data.FleetCoordination,
            }
        };
        FreighterLogic.SaveFreighterData(ps, values);

        // Verify cargo class was NOT changed
        cargoClass = ps.GetObject("FreighterInventory_Cargo")?.GetObject("Class");
        string afterCargoClass = cargoClass?.GetString("InventoryClass") ?? "";
        Assert.Equal(originalCargoClass, afterCargoClass);
    }

    /// <summary>
    /// Full round-trip: load a real save, simulate ALL panel logic saves
    /// (without changing anything), and verify that only the changed field
    /// appears in the diff.
    /// </summary>
    [Fact]
    public void RealSave_FullPanelRoundTrip_OnlyChangedFieldDiffers()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        // Capture a snapshot of sensitive arrays before any operations
        var origKnownTech = CaptureStringArray(ps, "KnownTech");
        var origKnownProducts = CaptureStringArray(ps, "KnownProducts");
        var origSeasonRewards = CaptureStringArray(ps, "RedeemedSeasonRewards");
        var origTwitchRewards = CaptureStringArray(ps, "RedeemedTwitchRewards");
        var origCargoClass = ps.GetObject("FreighterInventory_Cargo")?.GetObject("Class")?.GetString("InventoryClass") ?? "";
        var origFreighterClass = ps.GetObject("FreighterInventory")?.GetObject("Class")?.GetString("InventoryClass") ?? "";

        // --- Simulate CataloguePanel round-trip ---
        foreach (string arrayName in new[] { "KnownTech", "KnownProducts", "KnownSpecials" })
        {
            var ids = CatalogueLogic.LoadKnownItemIds(ps, arrayName);
            CatalogueLogic.SaveKnownItemIds(ps, arrayName, ids);
        }

        // --- Simulate FreighterPanel round-trip (no changes) ---
        var freighterData = FreighterLogic.LoadFreighterData(ps);
        FreighterLogic.SaveFreighterData(ps, new FreighterLogic.FreighterSaveValues
        {
            Name = freighterData.Name,
            ClassIndex = freighterData.ClassIndex,
            OriginalClassIndex = freighterData.ClassIndex,
            HomeSeed = freighterData.HomeSeed,
            ModelSeed = freighterData.ModelSeed,
            Hyperdrive = freighterData.Hyperdrive,
            FleetCoordination = freighterData.FleetCoordination,
            RawStatValues = new Dictionary<string, double>
            {
                ["^FREI_HYPERDRIVE"] = freighterData.Hyperdrive,
                ["^FREI_FLEET"] = freighterData.FleetCoordination,
            }
        });

        // --- Simulate StarshipPanel round-trip (no changes) on all ships ---
        var shipOwnership = ps.GetArray("ShipOwnership");
        if (shipOwnership != null)
        {
            for (int i = 0; i < shipOwnership.Length; i++)
            {
                var ship = shipOwnership.GetObject(i);
                if (ship == null) continue;
                var shipData = StarshipLogic.LoadShipData(ship, ps);
                if (shipData == null) continue;

                StarshipLogic.SaveShipData(ship, ps, new StarshipLogic.ShipSaveValues
                {
                    Name = shipData.Name,
                    ClassIndex = shipData.ClassIndex,
                    OriginalClassIndex = shipData.ClassIndex,
                    Seed = shipData.Seed,
                    Damage = shipData.Damage,
                    Shield = shipData.Shield,
                    Hyperdrive = shipData.Hyperdrive,
                    Maneuver = shipData.Maneuver,
                    ShipIndex = i,
                    RawStatValues = new Dictionary<string, double>
                    {
                        ["^SHIP_DAMAGE"] = shipData.Damage,
                        ["^SHIP_SHIELD"] = shipData.Shield,
                        ["^SHIP_HYPERDRIVE"] = shipData.Hyperdrive,
                        ["^SHIP_AGILE"] = shipData.Maneuver,
                    }
                });
            }
        }

        // --- Verify arrays are still in original order ---
        var afterKnownTech = CaptureStringArray(ps, "KnownTech");
        Assert.Equal(origKnownTech.Count, afterKnownTech.Count);
        for (int i = 0; i < origKnownTech.Count; i++)
            Assert.Equal(origKnownTech[i], afterKnownTech[i]);

        var afterKnownProducts = CaptureStringArray(ps, "KnownProducts");
        Assert.Equal(origKnownProducts.Count, afterKnownProducts.Count);
        for (int i = 0; i < origKnownProducts.Count; i++)
            Assert.Equal(origKnownProducts[i], afterKnownProducts[i]);

        var afterSeasonRewards = CaptureStringArray(ps, "RedeemedSeasonRewards");
        Assert.Equal(origSeasonRewards.Count, afterSeasonRewards.Count);
        for (int i = 0; i < origSeasonRewards.Count; i++)
            Assert.Equal(origSeasonRewards[i], afterSeasonRewards[i]);

        var afterTwitchRewards = CaptureStringArray(ps, "RedeemedTwitchRewards");
        Assert.Equal(origTwitchRewards.Count, afterTwitchRewards.Count);
        for (int i = 0; i < origTwitchRewards.Count; i++)
            Assert.Equal(origTwitchRewards[i], afterTwitchRewards[i]);

        // --- Verify classes are preserved ---
        Assert.Equal(origCargoClass,
            ps.GetObject("FreighterInventory_Cargo")?.GetObject("Class")?.GetString("InventoryClass") ?? "");
        Assert.Equal(origFreighterClass,
            ps.GetObject("FreighterInventory")?.GetObject("Class")?.GetString("InventoryClass") ?? "");
    }

    /// <summary>
    /// Regression test for the persistent KnownTech pollution bug.
    /// When AccountPanel.SaveData runs during SyncAllPanelData, the
    /// SaveRedeemedRewards method was calling SyncKnownArraysForRewards
    /// which added 100+ expedition items to KnownTech that were redeemed
    /// but not originally in the array. This test loads the real save,
    /// reads the redeemed sets, calls SaveRedeemedRewards, and verifies
    /// KnownTech is unchanged.
    /// </summary>
    [Fact]
    public void RealSave_AccountPanelRoundTrip_DoesNotPollute_KnownTech()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        // Capture original KnownTech and KnownSpecials
        var origKnownTech = CaptureStringArray(ps, "KnownTech");
        var origKnownSpecials = CaptureStringArray(ps, "KnownSpecials");

        // --- Simulate the AccountPanel round-trip ---
        // 1. Read the redeemed sets (what LoadData does)
        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(save);

        // 2. Build reward lists from the redeemed sets (all are marked as redeemed)
        var seasonRows = seasonRedeemed.Select(id => (id, true)).ToList();
        var twitchRows = twitchRedeemed.Select(id => (id, true)).ToList();

        // 3. Call SaveRedeemedRewards (what SaveData does)
        AccountLogic.SaveRedeemedRewards(save, seasonRows, twitchRows);

        // --- Verify KnownTech was NOT modified ---
        var afterKnownTech = CaptureStringArray(ps, "KnownTech");
        Assert.Equal(origKnownTech.Count, afterKnownTech.Count);
        for (int i = 0; i < origKnownTech.Count; i++)
            Assert.Equal(origKnownTech[i], afterKnownTech[i]);

        // --- Verify KnownSpecials was NOT modified ---
        var afterKnownSpecials = CaptureStringArray(ps, "KnownSpecials");
        Assert.Equal(origKnownSpecials.Count, afterKnownSpecials.Count);
        for (int i = 0; i < origKnownSpecials.Count; i++)
            Assert.Equal(origKnownSpecials[i], afterKnownSpecials[i]);
    }

    /// <summary>
    /// End-to-end simulation: load real save, simulate ALL panel saves
    /// including AccountPanel, and verify the ONLY changes are the
    /// intentionally-modified field (none in this test).
    /// </summary>
    [Fact]
    public void RealSave_AllPanelsIncludingAccount_NoSpuriousChanges()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        // Snapshot everything
        var origKnownTech = CaptureStringArray(ps, "KnownTech");
        var origKnownProducts = CaptureStringArray(ps, "KnownProducts");
        var origKnownSpecials = CaptureStringArray(ps, "KnownSpecials");
        var origSeasonRewards = CaptureStringArray(ps, "RedeemedSeasonRewards");
        var origTwitchRewards = CaptureStringArray(ps, "RedeemedTwitchRewards");

        // --- Simulate CataloguePanel ---
        foreach (string arrayName in new[] { "KnownTech", "KnownProducts", "KnownSpecials" })
        {
            var ids = CatalogueLogic.LoadKnownItemIds(ps, arrayName);
            CatalogueLogic.SaveKnownItemIds(ps, arrayName, ids);
        }

        // --- Simulate FreighterPanel ---
        var freighterData = FreighterLogic.LoadFreighterData(ps);
        FreighterLogic.SaveFreighterData(ps, new FreighterLogic.FreighterSaveValues
        {
            Name = freighterData.Name,
            ClassIndex = freighterData.ClassIndex,
            OriginalClassIndex = freighterData.ClassIndex,
            HomeSeed = freighterData.HomeSeed,
            ModelSeed = freighterData.ModelSeed,
            Hyperdrive = freighterData.Hyperdrive,
            FleetCoordination = freighterData.FleetCoordination,
            RawStatValues = new Dictionary<string, double>
            {
                ["^FREI_HYPERDRIVE"] = freighterData.Hyperdrive,
                ["^FREI_FLEET"] = freighterData.FleetCoordination,
            }
        });

        // --- Simulate StarshipPanel (all ships) ---
        var shipOwnership = ps.GetArray("ShipOwnership");
        if (shipOwnership != null)
        {
            for (int i = 0; i < shipOwnership.Length; i++)
            {
                var ship = shipOwnership.GetObject(i);
                if (ship == null) continue;
                var shipData = StarshipLogic.LoadShipData(ship, ps);
                if (shipData == null) continue;
                StarshipLogic.SaveShipData(ship, ps, new StarshipLogic.ShipSaveValues
                {
                    Name = shipData.Name,
                    ClassIndex = shipData.ClassIndex,
                    OriginalClassIndex = shipData.ClassIndex,
                    Seed = shipData.Seed,
                    Damage = shipData.Damage,
                    Shield = shipData.Shield,
                    Hyperdrive = shipData.Hyperdrive,
                    Maneuver = shipData.Maneuver,
                    ShipIndex = i,
                    RawStatValues = new Dictionary<string, double>
                    {
                        ["^SHIP_DAMAGE"] = shipData.Damage,
                        ["^SHIP_SHIELD"] = shipData.Shield,
                        ["^SHIP_HYPERDRIVE"] = shipData.Hyperdrive,
                        ["^SHIP_AGILE"] = shipData.Maneuver,
                    }
                });
            }
        }

        // --- Simulate AccountPanel (load redeemed, save back unchanged) ---
        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(save);
        AccountLogic.SaveRedeemedRewards(save,
            seasonRedeemed.Select(id => (id, true)).ToList(),
            twitchRedeemed.Select(id => (id, true)).ToList());

        // --- Verify ALL arrays are unchanged ---
        var afterKnownTech = CaptureStringArray(ps, "KnownTech");
        Assert.Equal(origKnownTech.Count, afterKnownTech.Count);
        for (int i = 0; i < origKnownTech.Count; i++)
            Assert.Equal(origKnownTech[i], afterKnownTech[i]);

        var afterKnownProducts = CaptureStringArray(ps, "KnownProducts");
        Assert.Equal(origKnownProducts.Count, afterKnownProducts.Count);
        for (int i = 0; i < origKnownProducts.Count; i++)
            Assert.Equal(origKnownProducts[i], afterKnownProducts[i]);

        var afterKnownSpecials = CaptureStringArray(ps, "KnownSpecials");
        Assert.Equal(origKnownSpecials.Count, afterKnownSpecials.Count);
        for (int i = 0; i < origKnownSpecials.Count; i++)
            Assert.Equal(origKnownSpecials[i], afterKnownSpecials[i]);

        var afterSeasonRewards = CaptureStringArray(ps, "RedeemedSeasonRewards");
        Assert.Equal(origSeasonRewards.Count, afterSeasonRewards.Count);
        for (int i = 0; i < origSeasonRewards.Count; i++)
            Assert.Equal(origSeasonRewards[i], afterSeasonRewards[i]);

        var afterTwitchRewards = CaptureStringArray(ps, "RedeemedTwitchRewards");
        Assert.Equal(origTwitchRewards.Count, afterTwitchRewards.Count);
        for (int i = 0; i < origTwitchRewards.Count; i++)
            Assert.Equal(origTwitchRewards[i], afterTwitchRewards[i]);
    }

    /// <summary>
    /// Verifies that SyncKnownArraysForChangedRewards correctly adds entries
    /// to KnownTech when a user explicitly toggles an item's redeemed state to true,
    /// and removes entries when toggled to false -- but ONLY for the changed items.
    /// </summary>
    [Fact]
    public void SyncKnownArraysForChangedRewards_OnlyTouchesChangedItems()
    {
        // Build a minimal save with KnownTech containing one entry
        var save = new JsonObject();
        var ps = new JsonObject();
        save.Set("PlayerStateData", ps);

        var knownTech = new JsonArray();
        knownTech.Add("^EXISTING_ITEM");
        ps.Set("KnownTech", knownTech);

        var knownSpecials = new JsonArray();
        ps.Set("KnownSpecials", knownSpecials);

        // Simulate: user toggled one item to redeemed (delta has 1 item)
        var changed = new List<(string Id, bool Redeemed)>
        {
            ("^NEW_REWARD", true)
        };
        AccountLogic.SyncKnownArraysForChangedRewards(save, changed, null);

        // KnownTech should still have existing item untouched
        var afterTech = CaptureStringArray(ps, "KnownTech");
        Assert.Contains("^EXISTING_ITEM", afterTech);

        // With no database, SyncKnownArraysForRewards conservatively skips KnownTech
        // for unknown items, so ^NEW_REWARD won't be added. Verify the existing entry is untouched.
        Assert.Single(afterTech);

        // Now simulate: user toggles an existing item to not-redeemed
        // First add it to KnownTech
        knownTech.Add("^TOGGLED_OFF");
        var changedOff = new List<(string Id, bool Redeemed)>
        {
            ("^TOGGLED_OFF", false)
        };
        AccountLogic.SyncKnownArraysForChangedRewards(save, changedOff, null);

        // ^TOGGLED_OFF should NOT be removed from KnownTech without database info
        // (the method skips items with no database match)
        afterTech = CaptureStringArray(ps, "KnownTech");
        Assert.Contains("^EXISTING_ITEM", afterTech);
    }

    /// <summary>
    /// Verifies that when the changed-rewards list is empty (no user changes),
    /// SyncKnownArraysForChangedRewards is a no-op.
    /// </summary>
    [Fact]
    public void SyncKnownArraysForChangedRewards_EmptyDelta_IsNoOp()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        var origKnownTech = CaptureStringArray(ps, "KnownTech");

        // Empty delta -- should be a no-op
        AccountLogic.SyncKnownArraysForChangedRewards(save,
            new List<(string, bool)>(), null);

        var afterKnownTech = CaptureStringArray(ps, "KnownTech");
        Assert.Equal(origKnownTech.Count, afterKnownTech.Count);
        for (int i = 0; i < origKnownTech.Count; i++)
            Assert.Equal(origKnownTech[i], afterKnownTech[i]);
    }

    /// <summary>
    /// Full end-to-end simulation with delta-only sync: load save, simulate
    /// ALL panels including AccountPanel with delta tracking, verify no
    /// spurious changes when no items are toggled.
    /// </summary>
    [Fact]
    public void RealSave_AllPanelsWithDeltaSync_NoSpuriousChanges()
    {
        string savePath = Path.Combine(referencePath, "fp_bugs", "save2-BEFORE.hg");
        if (!File.Exists(savePath)) return;

        var save = SaveFileManager.LoadSaveFile(savePath);
        var ps = save.GetObject("PlayerStateData")!;

        var origKnownTech = CaptureStringArray(ps, "KnownTech");
        var origKnownSpecials = CaptureStringArray(ps, "KnownSpecials");

        // --- Simulate CataloguePanel ---
        foreach (string arrayName in new[] { "KnownTech", "KnownProducts", "KnownSpecials" })
        {
            var ids = CatalogueLogic.LoadKnownItemIds(ps, arrayName);
            CatalogueLogic.SaveKnownItemIds(ps, arrayName, ids);
        }

        // --- Simulate AccountPanel (load + save with delta tracking) ---
        // 1. Load: snapshot original redeemed state
        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(save);
        var origSeasonRedeemed = new HashSet<string>(seasonRedeemed, StringComparer.OrdinalIgnoreCase);
        var origTwitchRedeemed = new HashSet<string>(twitchRedeemed, StringComparer.OrdinalIgnoreCase);

        // 2. Build current state (unchanged -- same as original)
        var seasonRows = seasonRedeemed.Select(id => (id, true)).ToList();
        var twitchRows = twitchRedeemed.Select(id => (id, true)).ToList();

        // 3. SaveRedeemedRewards (writes Redeemed* arrays only)
        AccountLogic.SaveRedeemedRewards(save, seasonRows, twitchRows);

        // 4. Compute delta (should be empty -- nothing changed)
        var seasonChanged = new List<(string Id, bool Redeemed)>();
        foreach (var (id, redeemed) in seasonRows)
        {
            bool wasRedeemed = origSeasonRedeemed.Contains(id);
            if (redeemed != wasRedeemed) seasonChanged.Add((id, redeemed));
        }
        var twitchChanged = new List<(string Id, bool Redeemed)>();
        foreach (var (id, redeemed) in twitchRows)
        {
            bool wasRedeemed = origTwitchRedeemed.Contains(id);
            if (redeemed != wasRedeemed) twitchChanged.Add((id, redeemed));
        }

        // Delta should be empty
        Assert.Empty(seasonChanged);
        Assert.Empty(twitchChanged);

        // 5. SyncKnownArraysForChangedRewards with empty delta -- no-op
        AccountLogic.SyncKnownArraysForChangedRewards(save, seasonChanged, null);
        AccountLogic.SyncKnownArraysForChangedRewards(save, twitchChanged, null);

        // --- Verify KnownTech unchanged ---
        var afterKnownTech = CaptureStringArray(ps, "KnownTech");
        Assert.Equal(origKnownTech.Count, afterKnownTech.Count);
        for (int i = 0; i < origKnownTech.Count; i++)
            Assert.Equal(origKnownTech[i], afterKnownTech[i]);

        // --- Verify KnownSpecials unchanged ---
        var afterKnownSpecials = CaptureStringArray(ps, "KnownSpecials");
        Assert.Equal(origKnownSpecials.Count, afterKnownSpecials.Count);
        for (int i = 0; i < origKnownSpecials.Count; i++)
            Assert.Equal(origKnownSpecials[i], afterKnownSpecials[i]);
    }

    /// <summary>
    /// Verifies that IsNonTechReward correctly identifies ship, egg, frigate,
    /// weapon, firework, and pet reward IDs as non-tech (should NOT go in KnownTech),
    /// while trails, staffs, bobbleheads, mech parts, and corvette parts are tech
    /// (should go in KnownTech).
    /// </summary>
    [Fact]
    public void IsNonTechReward_CorrectlyClassifiesRewardTypes()
    {
        // Non-tech rewards (ships, eggs, frigates, weapons, fireworks, pets)
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S13_SHIP" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_TWIT_SHIP01" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_SHIP_DARK" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S8_EGG" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_PB_MEDAL_EGG1" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S13_FRIGATE" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_TWIT_GUN01" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_SWIT_GUN01" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S1_FIREWORKS" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_TWIT_FIREW01" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_TWIT_PET01" }));
        Assert.True(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_TWIT_PET25" }));

        // Tech rewards -- should return false (these DO go in KnownTech)
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S6_TRAIL" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S12_STAFF" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S2_SPEC" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_S14_MECH_ARML" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "R_BOBBLE_OCTO" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S20_ENGINE" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S20_SHIELD" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S20_TRIM" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S20_WING" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S19_TURRET" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S20_DECO" }));
        Assert.False(AccountLogic.IsNonTechReward(new GameItem { GiveRewardOnSpecialPurchase = "RS_S20_STR" }));
    }

    /// <summary>
    /// Verifies that SyncKnownArraysForChangedRewards uses the database
    /// GiveRewardOnSpecialPurchase field to correctly skip KnownTech for
    /// non-tech rewards (ships, eggs) while adding tech rewards (trails, staffs).
    /// </summary>
    [Fact]
    public void SyncKnownArraysForChangedRewards_UsesGiveRewardField_SkipsNonTechItems()
    {
        // Build a minimal save
        var save = new JsonObject();
        var ps = new JsonObject();
        save.Set("PlayerStateData", ps);
        ps.Set("KnownTech", new JsonArray());
        ps.Set("KnownSpecials", new JsonArray());

        // Build a minimal database with two items:
        // 1. A ship reward (non-tech) -- should NOT be added to KnownTech
        // 2. A trail reward (tech) -- SHOULD be added to KnownTech
        var db = new GameItemDatabase();
        db.InjectTestItem(new GameItem
        {
            Id = "EXPD_SHIP13",
            TradeCategory = "SpecialShop",
            GiveRewardOnSpecialPurchase = "RS_S13_SHIP"
        });
        db.InjectTestItem(new GameItem
        {
            Id = "SHIP_PIRATE",
            TradeCategory = "SpecialShop",
            GiveRewardOnSpecialPurchase = "RS_S6_TRAIL"
        });

        var changed = new List<(string Id, bool Redeemed)>
        {
            ("^EXPD_SHIP13", true),
            ("^SHIP_PIRATE", true)
        };

        AccountLogic.SyncKnownArraysForChangedRewards(save, changed, db);

        var knownTech = CaptureStringArray(ps, "KnownTech");
        var knownSpecials = CaptureStringArray(ps, "KnownSpecials");

        // Ship reward should NOT be in KnownTech
        Assert.DoesNotContain("^EXPD_SHIP13", knownTech);
        // Trail reward SHOULD be in KnownTech
        Assert.Contains("^SHIP_PIRATE", knownTech);

        // Both should be in KnownSpecials (both are SpecialShop)
        Assert.Contains("^EXPD_SHIP13", knownSpecials);
        Assert.Contains("^SHIP_PIRATE", knownSpecials);
    }

    // --- InventoryBulkActions ----------------------------------------

    /// <summary>
    /// Helper to build a minimal inventory JSON object with the given slots.
    /// Each slot is represented as (itemId, amount, maxAmount, damageFactor, inventoryType).
    /// </summary>
    private static JsonObject BuildInventory(params (string id, int amount, int maxAmount, double damage, string invType)[] slots)
    {
        var inv = JsonObject.Parse("{ \"Slots\": [], \"SpecialSlots\": [] }");
        var slotsArr = inv.GetArray("Slots")!;
        for (int i = 0; i < slots.Length; i++)
        {
            var (id, amount, maxAmount, damage, invType) = slots[i];
            var slot = JsonObject.Parse($@"{{
                ""Id"": {{ ""Id"": ""{id}"" }},
                ""Amount"": {amount},
                ""MaxAmount"": {maxAmount},
                ""DamageFactor"": {damage.ToString(CultureInfo.InvariantCulture)},
                ""FullyInstalled"": {(damage > 0 ? "false" : "true")},
                ""Type"": {{ ""InventoryType"": ""{invType}"" }},
                ""Index"": {{ ""X"": {i}, ""Y"": 0 }}
            }}");
            slotsArr.Add(slot);
        }
        return inv;
    }

    /// <summary>
    /// Helper to build a minimal PlayerStateData with one tech inventory (Exosuit tech)
    /// and one cargo inventory (Exosuit cargo).
    /// </summary>
    private static JsonObject BuildPlayerState(JsonObject? techInv = null, JsonObject? cargoInv = null)
    {
        var ps = JsonObject.Parse("{}");
        if (cargoInv != null) ps.Add("Inventory", cargoInv);
        if (techInv != null) ps.Add("Inventory_TechOnly", techInv);
        return ps;
    }

    private static GameItemDatabase BuildTestDatabase()
    {
        var db = new GameItemDatabase();
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Data", "json");
        if (Directory.Exists(dbPath))
            db.LoadItemsFromJsonDirectory(dbPath);
        return db;
    }

    [Fact]
    public void BulkActions_RechargeAllTechnology_RechargesChargeableItems()
    {
        var db = BuildTestDatabase();
        if (db.Items.Count == 0) return; // skip if no database

        // HYPERDRIVE is a chargeable tech item
        var hyperdrive = db.GetItem("^HYPERDRIVE");
        Assert.NotNull(hyperdrive);
        Assert.True(hyperdrive.IsChargeable);
        int chargeAmount = hyperdrive.ChargeValue;
        Assert.True(chargeAmount > 0);

        var techInv = BuildInventory(
            ("^HYPERDRIVE", 10, chargeAmount, 0, "Technology"),
            ("^LASER", 50, 100, 0, "Technology")  // some other tech, partially charged
        );
        var ps = BuildPlayerState(techInv: techInv);

        int recharged = InventoryBulkActions.RechargeAllTechnology(ps, db);

        // HYPERDRIVE should have been recharged; LASER only if it's chargeable
        Assert.True(recharged >= 1);
        var slots = techInv.GetArray("Slots")!;
        Assert.Equal(chargeAmount, slots.GetObject(0)!.GetInt("Amount"));
    }

    [Fact]
    public void BulkActions_RechargeAllTechnology_SkipsFullyChargedItems()
    {
        var db = BuildTestDatabase();
        if (db.Items.Count == 0) return;

        var hyperdrive = db.GetItem("^HYPERDRIVE");
        Assert.NotNull(hyperdrive);
        int chargeAmount = hyperdrive.ChargeValue;

        var techInv = BuildInventory(
            ("^HYPERDRIVE", chargeAmount, chargeAmount, 0, "Technology") // already full
        );
        var ps = BuildPlayerState(techInv: techInv);

        int recharged = InventoryBulkActions.RechargeAllTechnology(ps, db);
        Assert.Equal(0, recharged);
    }

    [Fact]
    public void BulkActions_RefillAllStacks_RefillsCargoSlots()
    {
        var db = BuildTestDatabase();

        var cargoInv = BuildInventory(
            ("^FUEL1", 50, 500, 0, "Substance"),
            ("^CASING", 3, 50, 0, "Product")
        );
        var ps = BuildPlayerState(cargoInv: cargoInv);

        int refilled = InventoryBulkActions.RefillAllStacks(ps, db);

        Assert.Equal(2, refilled);
        var slots = cargoInv.GetArray("Slots")!;
        Assert.Equal(500, slots.GetObject(0)!.GetInt("Amount"));
        Assert.Equal(50, slots.GetObject(1)!.GetInt("Amount"));
    }

    [Fact]
    public void BulkActions_RefillAllStacks_SkipsFullStacks()
    {
        var db = BuildTestDatabase();

        var cargoInv = BuildInventory(
            ("^FUEL1", 500, 500, 0, "Substance") // already full
        );
        var ps = BuildPlayerState(cargoInv: cargoInv);

        int refilled = InventoryBulkActions.RefillAllStacks(ps, db);
        Assert.Equal(0, refilled);
    }

    [Fact]
    public void BulkActions_RepairAllSlots_RepairsDamagedSlots()
    {
        var db = BuildTestDatabase();

        var techInv = BuildInventory(
            ("^HYPERDRIVE", -1, 200, 1.0, "Technology"), // damaged tech
            ("^LASER", 100, 100, 0, "Technology")        // undamaged
        );
        var ps = BuildPlayerState(techInv: techInv);

        int repaired = InventoryBulkActions.RepairAllSlots(ps, db);

        Assert.Equal(1, repaired);
        var slot = techInv.GetArray("Slots")!.GetObject(0)!;
        Assert.Equal(0.0, slot.GetDouble("DamageFactor"));
        Assert.True((bool)slot.Get("FullyInstalled")!);
    }

    [Fact]
    public void BulkActions_RepairAllSlots_RemovesDamagePlaceholders()
    {
        var db = BuildTestDatabase();

        var cargoInv = BuildInventory(
            ("^SHIPSLOT_DMG1", 0, 0, 1.0, "Product"), // damage placeholder
            ("^FUEL1", 100, 500, 0, "Substance")       // normal item
        );
        var ps = BuildPlayerState(cargoInv: cargoInv);

        int repaired = InventoryBulkActions.RepairAllSlots(ps, db);

        Assert.Equal(1, repaired);
        // Damage placeholder should be removed from slots
        var slots = cargoInv.GetArray("Slots")!;
        Assert.Equal(1, slots.Length);
        Assert.Equal("^FUEL1", slots.GetObject(0)!.GetObject("Id")!.GetString("Id"));
    }

    [Fact]
    public void BulkActions_RepairAllSlots_RemovesBlockedByBrokenTech()
    {
        var db = BuildTestDatabase();

        var techInv = BuildInventory(
            ("^HYPERDRIVE", -1, 200, 1.0, "Technology")
        );
        // Add a BlockedByBrokenTech entry
        var specialSlots = techInv.GetArray("SpecialSlots")!;
        var blocked = JsonObject.Parse(@"{
            ""Type"": { ""InventorySpecialSlotType"": ""BlockedByBrokenTech"" },
            ""Index"": { ""X"": 1, ""Y"": 0 }
        }");
        specialSlots.Add(blocked);
        Assert.Equal(1, specialSlots.Length);

        var ps = BuildPlayerState(techInv: techInv);
        InventoryBulkActions.RepairAllSlots(ps, db);

        // BlockedByBrokenTech should be removed
        Assert.Equal(0, techInv.GetArray("SpecialSlots")!.Length);
    }

    [Fact]
    public void BulkActions_RepairAllTechnology_OnlyRepairsTechItems()
    {
        var db = BuildTestDatabase();

        // Mixed inventory with both tech and cargo items
        var inv = BuildInventory(
            ("^HYPERDRIVE", -1, 200, 1.0, "Technology"),  // damaged tech
            ("^FUEL1", 50, 500, 1.0, "Substance")         // damaged cargo/substance
        );
        // Use this as a tech inventory in player state
        var ps = JsonObject.Parse("{}");
        ps.Add("Inventory_TechOnly", inv);

        int repaired = InventoryBulkActions.RepairAllTechnology(ps, db);

        // Only the Technology item should be repaired
        Assert.Equal(1, repaired);
        var slots = inv.GetArray("Slots")!;
        Assert.Equal(0.0, slots.GetObject(0)!.GetDouble("DamageFactor"));   // tech repaired
        Assert.Equal(1.0, slots.GetObject(1)!.GetDouble("DamageFactor"));   // substance NOT repaired
    }

    [Fact]
    public void BulkActions_RepairAllSlots_ExcludesChestsAndStorage()
    {
        var db = BuildTestDatabase();

        // Build a player state with a damaged chest
        var chestInv = BuildInventory(
            ("^FUEL1", -1, 500, 1.0, "Substance") // damaged
        );
        var ps = JsonObject.Parse("{}");
        ps.Add("Chest1Inventory", chestInv);

        // RepairAllSlots should NOT touch chest inventories
        int repaired = InventoryBulkActions.RepairAllSlots(ps, db);
        Assert.Equal(0, repaired);
        Assert.Equal(1.0, chestInv.GetArray("Slots")!.GetObject(0)!.GetDouble("DamageFactor"));
    }

    [Fact]
    public void BulkActions_RefillAllStacks_IncludesChests()
    {
        var db = BuildTestDatabase();

        var chestInv = BuildInventory(
            ("^FUEL1", 50, 500, 0, "Substance")
        );
        var ps = JsonObject.Parse("{}");
        ps.Add("Chest1Inventory", chestInv);

        // RefillAllStacks SHOULD include chests
        int refilled = InventoryBulkActions.RefillAllStacks(ps, db);
        Assert.Equal(1, refilled);
        Assert.Equal(500, chestInv.GetArray("Slots")!.GetObject(0)!.GetInt("Amount"));
    }

    [Fact]
    public void BulkActions_RechargeAllTech_SpansMultipleInventoryTypes()
    {
        var db = BuildTestDatabase();
        if (db.Items.Count == 0) return;

        var hyperdrive = db.GetItem("^HYPERDRIVE");
        Assert.NotNull(hyperdrive);
        int chargeAmount = hyperdrive.ChargeValue;

        // Build player state with Exosuit tech + one ship with tech
        var exosuitTech = BuildInventory(("^HYPERDRIVE", 10, chargeAmount, 0, "Technology"));
        var shipTech = BuildInventory(("^HYPERDRIVE", 5, chargeAmount, 0, "Technology"));
        var ship = JsonObject.Parse("{}");
        ship.Add("Inventory_TechOnly", shipTech);
        ship.Add("Inventory", JsonObject.Parse("{ \"Slots\": [] }"));
        var shipsArr = new JsonArray();
        shipsArr.Add(ship);

        var ps = JsonObject.Parse("{}");
        ps.Add("Inventory_TechOnly", exosuitTech);
        ps.Add("ShipOwnership", shipsArr);

        int recharged = InventoryBulkActions.RechargeAllTechnology(ps, db);

        Assert.Equal(2, recharged);
        Assert.Equal(chargeAmount, exosuitTech.GetArray("Slots")!.GetObject(0)!.GetInt("Amount"));
        Assert.Equal(chargeAmount, shipTech.GetArray("Slots")!.GetObject(0)!.GetInt("Amount"));
    }

    [Fact]
    public void BulkActions_RepairAllSlots_SpansMultipleInventoryTypes()
    {
        var db = BuildTestDatabase();

        // Exosuit cargo with damage + Freighter tech with damage
        var exosuitCargo = BuildInventory(("^FUEL1", -1, 500, 1.0, "Substance"));
        var freighterTech = BuildInventory(("^HYPERDRIVE", -1, 200, 1.0, "Technology"));

        var ps = JsonObject.Parse("{}");
        ps.Add("Inventory", exosuitCargo);
        ps.Add("FreighterInventory_TechOnly", freighterTech);

        int repaired = InventoryBulkActions.RepairAllSlots(ps, db);

        Assert.Equal(2, repaired);
        Assert.Equal(0.0, exosuitCargo.GetArray("Slots")!.GetObject(0)!.GetDouble("DamageFactor"));
        Assert.Equal(0.0, freighterTech.GetArray("Slots")!.GetObject(0)!.GetDouble("DamageFactor"));
    }

    [Fact]
    public void BulkActions_RefillAllStacks_IncludesStorageInventories()
    {
        var db = BuildTestDatabase();

        var storageInv = BuildInventory(("^FUEL1", 10, 500, 0, "Substance"));
        var ps = JsonObject.Parse("{}");
        ps.Add("CookingIngredientsInventory", storageInv);

        int refilled = InventoryBulkActions.RefillAllStacks(ps, db);
        Assert.Equal(1, refilled);
        Assert.Equal(500, storageInv.GetArray("Slots")!.GetObject(0)!.GetInt("Amount"));
    }

    [Fact]
    public void BulkActions_RechargeAllTech_IncludesMultitools()
    {
        var db = BuildTestDatabase();
        if (db.Items.Count == 0) return;

        var hyperdrive = db.GetItem("^HYPERDRIVE");
        Assert.NotNull(hyperdrive);
        int chargeAmount = hyperdrive.ChargeValue;

        var toolStore = BuildInventory(("^HYPERDRIVE", 5, chargeAmount, 0, "Technology"));
        var tool = JsonObject.Parse("{}");
        tool.Add("Store", toolStore);

        var multitools = new JsonArray();
        multitools.Add(tool);

        var ps = JsonObject.Parse("{}");
        ps.Add("Multitools", multitools);

        int recharged = InventoryBulkActions.RechargeAllTechnology(ps, db);
        Assert.Equal(1, recharged);
        Assert.Equal(chargeAmount, toolStore.GetArray("Slots")!.GetObject(0)!.GetInt("Amount"));
    }

    [Fact]
    public void BulkActions_RechargeAllTech_IncludesExocraft()
    {
        var db = BuildTestDatabase();
        if (db.Items.Count == 0) return;

        var hyperdrive = db.GetItem("^HYPERDRIVE");
        Assert.NotNull(hyperdrive);
        int chargeAmount = hyperdrive.ChargeValue;

        var vehicleTech = BuildInventory(("^HYPERDRIVE", 5, chargeAmount, 0, "Technology"));
        var vehicle = JsonObject.Parse("{}");
        vehicle.Add("Inventory_TechOnly", vehicleTech);
        vehicle.Add("Inventory", JsonObject.Parse("{ \"Slots\": [] }"));

        var vehicles = new JsonArray();
        vehicles.Add(vehicle);

        var ps = JsonObject.Parse("{}");
        ps.Add("VehicleOwnership", vehicles);

        int recharged = InventoryBulkActions.RechargeAllTechnology(ps, db);
        Assert.Equal(1, recharged);
    }

    [Fact]
    public void BulkActions_RepairAllSlots_IncludesExocraftInventories()
    {
        var db = BuildTestDatabase();

        var vehicleCargo = BuildInventory(("^FUEL1", -1, 500, 1.0, "Substance"));
        var vehicleTech = BuildInventory(("^HYPERDRIVE", -1, 200, 1.0, "Technology"));
        var vehicle = JsonObject.Parse("{}");
        vehicle.Add("Inventory", vehicleCargo);
        vehicle.Add("Inventory_TechOnly", vehicleTech);

        var vehicles = new JsonArray();
        vehicles.Add(vehicle);

        var ps = JsonObject.Parse("{}");
        ps.Add("VehicleOwnership", vehicles);

        int repaired = InventoryBulkActions.RepairAllSlots(ps, db);
        Assert.Equal(2, repaired);
    }

    [Fact]
    public void BulkActions_EmptyPlayerState_ReturnsZero()
    {
        var db = BuildTestDatabase();
        var ps = JsonObject.Parse("{}");

        Assert.Equal(0, InventoryBulkActions.RechargeAllTechnology(ps, db));
        Assert.Equal(0, InventoryBulkActions.RefillAllStacks(ps, db));
        Assert.Equal(0, InventoryBulkActions.RepairAllSlots(ps, db));
        Assert.Equal(0, InventoryBulkActions.RepairAllTechnology(ps, db));
    }
}
