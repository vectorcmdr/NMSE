using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for starship-related logic: corvette optimisation, .nmsship ZIP import,
/// CCD default detection, and building object reordering.
/// </summary>
public class StarshipLogicTests
{
    // --- IsCcdDefault tests ---

    [Fact]
    public void IsCcdDefault_BlankCcd_ReturnsTrue()
    {
        var ccd = JsonObject.Parse("""
        {
            "SelectedPreset":"^",
            "CustomData":{
                "DescriptorGroups":[],
                "PaletteID":"^",
                "Colours":[],
                "TextureOptions":[],
                "BoneScales":[],
                "Scale":1.0
            }
        }
        """);
        Assert.True(StarshipLogic.IsCcdDefault(ccd));
    }

    [Fact]
    public void IsCcdDefault_EmptyCcd_ReturnsTrue()
    {
        var ccd = new JsonObject();
        Assert.True(StarshipLogic.IsCcdDefault(ccd));
    }

    [Fact]
    public void IsCcdDefault_NonDefaultPreset_ReturnsFalse()
    {
        var ccd = JsonObject.Parse("""
        {
            "SelectedPreset":"CustomPreset",
            "CustomData":{
                "DescriptorGroups":[],
                "PaletteID":"^",
                "Colours":[],
                "TextureOptions":[],
                "BoneScales":[],
                "Scale":1.0
            }
        }
        """);
        Assert.False(StarshipLogic.IsCcdDefault(ccd));
    }

    [Fact]
    public void IsCcdDefault_NonEmptyColours_ReturnsFalse()
    {
        var ccd = JsonObject.Parse("""
        {
            "SelectedPreset":"^",
            "CustomData":{
                "DescriptorGroups":[],
                "PaletteID":"^",
                "Colours":["red"],
                "TextureOptions":[],
                "BoneScales":[],
                "Scale":1.0
            }
        }
        """);
        Assert.False(StarshipLogic.IsCcdDefault(ccd));
    }

    // --- ReorderBuildingObjects tests ---

    [Fact]
    public void ReorderBuildingObjects_GroupsByPriority()
    {
        // Create objects with different categories in scrambled order
        // Without database loaded, only prefix fallback is used
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        AddBuildingObject(objects, "^B_COK_A", 5);   // Cockpit (priority 6)
        AddBuildingObject(objects, "^B_GEN_1", 1);   // Reactor (priority 1)
        AddBuildingObject(objects, "^B_ALK_B", 5);   // Access/LandingBay (priority 5)
        AddBuildingObject(objects, "^B_TRU_C", 1);   // Engine/Thruster (priority 2)
        AddBuildingObject(objects, "^BUILDTABLE", 3); // Other

        int count = StarshipLogic.ReorderBuildingObjects(objects);
        Assert.Equal(5, count);

        // Sorted by priority: Reactor, Engine, Access, Cockpit, Other
        Assert.Equal("^B_GEN_1", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^B_TRU_C", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^B_ALK_B", objects.GetObject(2).GetString("ObjectID"));
        Assert.Equal("^B_COK_A", objects.GetObject(3).GetString("ObjectID"));
        Assert.Equal("^BUILDTABLE", objects.GetObject(4).GetString("ObjectID"));
    }

    [Fact]
    public void ReorderBuildingObjects_PreservesRelativeOrder()
    {
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        AddBuildingObject(objects, "^FIRST", 1);
        AddBuildingObject(objects, "^SECOND", 1);
        AddBuildingObject(objects, "^THIRD", 1);

        StarshipLogic.ReorderBuildingObjects(objects);

        // All are Other category - relative order preserved
        Assert.Equal("^FIRST", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^SECOND", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^THIRD", objects.GetObject(2).GetString("ObjectID"));
    }

    [Fact]
    public void ReorderBuildingObjects_OtherPartsAtEnd()
    {
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        AddBuildingObject(objects, "^TURRET", 0);       // Other (no B_ prefix match)
        AddBuildingObject(objects, "^B_GEN_0", 1);      // Reactor
        AddBuildingObject(objects, "^WIRE", 0);         // Other
        AddBuildingObject(objects, "^B_LND_A", 5);      // Gear/LandingGear

        StarshipLogic.ReorderBuildingObjects(objects);

        // Reactor first, then LandingGear, then Other parts (preserving their relative order)
        Assert.Equal("^B_GEN_0", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^B_LND_A", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^TURRET", objects.GetObject(2).GetString("ObjectID"));
        Assert.Equal("^WIRE", objects.GetObject(3).GetString("ObjectID"));
    }

    [Fact]
    public void ReorderBuildingObjects_SingleObject_NoChange()
    {
        StarshipDatabase.Clear();
        var objects = new JsonArray();
        AddBuildingObject(objects, "^ONLY", 42);

        int count = StarshipLogic.ReorderBuildingObjects(objects);
        Assert.Equal(1, count);
        Assert.Equal("^ONLY", objects.GetObject(0).GetString("ObjectID"));
    }

    [Fact]
    public void ReorderBuildingObjects_AlreadySorted_Unchanged()
    {
        StarshipDatabase.Clear();
        var objects = new JsonArray();
        AddBuildingObject(objects, "^B_GEN_1", 1);      // Reactor
        AddBuildingObject(objects, "^B_TRU_C", 2);      // Engine
        AddBuildingObject(objects, "^B_LND_A", 3);      // Gear
        AddBuildingObject(objects, "^BUILDTABLE", 0);   // Other

        StarshipLogic.ReorderBuildingObjects(objects);

        Assert.Equal("^B_GEN_1", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^B_TRU_C", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^B_LND_A", objects.GetObject(2).GetString("ObjectID"));
        Assert.Equal("^BUILDTABLE", objects.GetObject(3).GetString("ObjectID"));
    }

    [Fact]
    public void ReorderBuildingObjects_NonFunctionalPartsNotSorted()
    {
        // B_HAB (Hab), B_STR (Hull), B_WNG_D (without database = no match) are all non-functional
        // and should remain in their original relative order at the end.
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        AddBuildingObject(objects, "^B_HAB_C", 50331758);  // Other (Hab, no prefix match)
        AddBuildingObject(objects, "^B_LND_A", 1);         // Gear
        AddBuildingObject(objects, "^U_PARAGON", 0);       // Other

        StarshipLogic.ReorderBuildingObjects(objects);

        // Gear first, then Others in original relative order
        Assert.Equal("^B_LND_A", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^B_HAB_C", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^U_PARAGON", objects.GetObject(2).GetString("ObjectID"));
    }

    // --- TryReadNmsshipZip tests ---

    [Fact]
    public void TryReadNmsshipZip_ValidZip_ReturnsParsedData()
    {
        string refPath = Path.Combine("_ref", "imports", "corvette.nmsship");
        if (!File.Exists(refPath)) return; // Skip if reference file not available

        var result = StarshipLogic.TryReadNmsshipZip(refPath);
        Assert.NotNull(result);

        var (ship, ccd, objects) = result.Value;

        // so.json should have a Name field
        Assert.NotNull(ship.GetString("Name"));
        Assert.NotEmpty(ship.GetString("Name")!);

        // so.json should have a Resource with a BIGGS filename
        var resource = ship.GetObject("Resource");
        Assert.NotNull(resource);
        Assert.Contains("BIGGS", resource!.GetString("Filename")!, StringComparison.OrdinalIgnoreCase);

        // ccd.json should be present (blank/default)
        Assert.NotNull(ccd);
        Assert.True(StarshipLogic.IsCcdDefault(ccd!));

        // objects.json should have building objects
        Assert.NotNull(objects);
        Assert.True(objects!.Length > 0);
    }

    [Fact]
    public void TryReadNmsshipZip_PlainJsonFile_ReturnsNull()
    {
        // Create a temporary plain JSON file
        string tempPath = Path.Combine(Path.GetTempPath(), "test_plain.nmsship");
        try
        {
            File.WriteAllText(tempPath, """{"Name":"Test","Resource":{"Filename":"TEST"}}""");
            var result = StarshipLogic.TryReadNmsshipZip(tempPath);
            Assert.Null(result);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void TryReadNmsshipZip_NonExistentFile_ReturnsNull()
    {
        var result = StarshipLogic.TryReadNmsshipZip("/nonexistent/path/file.nmsship");
        Assert.Null(result);
    }

    // --- JsonArray.Parse test ---

    [Fact]
    public void JsonArray_Parse_ReturnsArray()
    {
        var arr = JsonArray.Parse("""[{"ObjectID":"^TEST","UserData":1}]""");
        Assert.Equal(1, arr.Length);
        Assert.Equal("^TEST", arr.GetObject(0).GetString("ObjectID"));
    }

    // --- Helper ---

    private static void AddBuildingObject(JsonArray objects, string objectId, long userData)
    {
        var obj = new JsonObject();
        obj.Set("ObjectID", objectId);
        obj.Set("UserData", (double)userData);
        obj.Set("Timestamp", 0.0);
        objects.Add(obj);
    }

    // --- Priority-based categorisation tests ---

    [Fact]
    public void GetPartPriority_PrefixFallback_ReactorParts()
    {
        StarshipDatabase.Clear();
        // B_GEN parts are Reactor (priority 1) via priority map
        Assert.Equal(1, StarshipLogic.GetPartPriority("B_GEN_0"));
        Assert.Equal(1, StarshipLogic.GetPartPriority("^B_GEN_1"));
        // B_GEN_3 is NOT in Priority.Map → OtherPriority
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("B_GEN_3"));
    }

    [Fact]
    public void GetPartPriority_PrefixFallback_ShieldParts_AreUnsorted()
    {
        StarshipDatabase.Clear();
        // B_SHL parts are NOT sorted (no prefix fallback for Shield)
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("^B_SHL_A"));
        Assert.Equal(StarshipDatabase.OtherPriority, StarshipLogic.GetPartPriority("B_SHL_E"));
    }

    [Fact]
    public void ReorderBuildingObjects_ShieldsAreUnsorted()
    {
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        // Shield parts are NOT sorted - they stay in original order in the Other group
        AddBuildingObject(objects, "^B_GEN_0", 1); // Reactor
        AddBuildingObject(objects, "^B_SHL_A", 2); // Shield -> unsorted
        AddBuildingObject(objects, "^B_GEN_2", 3); // Reactor
        AddBuildingObject(objects, "^B_TRU_A", 4); // Engine

        StarshipLogic.ReorderBuildingObjects(objects);

        // Reactor first, then engine, then shield (unsorted, original index preserved)
        Assert.Equal("^B_GEN_0", objects.GetObject(0).GetString("ObjectID")); // Reactor
        Assert.Equal("^B_GEN_2", objects.GetObject(1).GetString("ObjectID")); // Reactor
        Assert.Equal("^B_TRU_A", objects.GetObject(2).GetString("ObjectID")); // Engine
        Assert.Equal("^B_SHL_A", objects.GetObject(3).GetString("ObjectID")); // Shield -> Other
    }

    [Fact]
    public void ReorderBuildingObjects_FullPriorityOrder()
    {
        // Test the complete priority chain using priority map:
        // Reactor(1) -> Thruster(2) -> Wing(3) -> Gear(4) -> Access(5) -> Cockpit(6) -> Other
        // Shield (B_SHL) is NOT in map (goes to Other group).
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        AddBuildingObject(objects, "^BUILDTABLE", 0);   // Other -> "Round Table"
        AddBuildingObject(objects, "^B_COK_A", 1);      // Cockpit (6)
        AddBuildingObject(objects, "^B_ALK_B", 2);      // Access (5)
        AddBuildingObject(objects, "^B_LND_C", 3);      // Gear (4)
        AddBuildingObject(objects, "^B_TRU_E", 5);      // Thruster (2)
        AddBuildingObject(objects, "^B_GEN_0", 6);      // Reactor (1)
        AddBuildingObject(objects, "^B_SHL_D", 7);      // Shield -> Other -> "Defence Field"

        StarshipLogic.ReorderBuildingObjects(objects);

        Assert.Equal("^B_GEN_0", objects.GetObject(0).GetString("ObjectID")); // Reactor (1)
        Assert.Equal("^B_TRU_E", objects.GetObject(1).GetString("ObjectID")); // Thruster (2)
        Assert.Equal("^B_LND_C", objects.GetObject(2).GetString("ObjectID")); // Gear (4)
        Assert.Equal("^B_ALK_B", objects.GetObject(3).GetString("ObjectID")); // Access (5)
        Assert.Equal("^B_COK_A", objects.GetObject(4).GetString("ObjectID")); // Cockpit (6)
        // Other group sorted alphabetically by object list name:
        // "Defence Field" (B_SHL_D) < "Round Table" (BUILDTABLE)
        Assert.Equal("^B_SHL_D", objects.GetObject(5).GetString("ObjectID"));   // "Defence Field"
        Assert.Equal("^BUILDTABLE", objects.GetObject(6).GetString("ObjectID")); // "Round Table"
    }

    // --- Corvette display name tests ---

    [Fact]
    public void GetPartDisplayName_ReturnsCorrectNames()
    {
        // Priority map entries have names in priority map, not object list
        // but object list also has them for ... reasons...
        Assert.Equal("Industrial Barrel", StarshipDatabase.GetPartDisplayName("^ABAND_BARREL"));
        Assert.Equal("Hexagonal Table", StarshipDatabase.GetPartDisplayName("^BUILDTABLE2"));
        Assert.Equal("Round Table", StarshipDatabase.GetPartDisplayName("^BUILDTABLE"));
        Assert.Equal("Defence Field", StarshipDatabase.GetPartDisplayName("^B_SHL_D"));
    }

    [Fact]
    public void GetPartDisplayName_NormalizesPrefix()
    {
        // Should work with or without ^ prefix
        Assert.Equal("Industrial Barrel", StarshipDatabase.GetPartDisplayName("ABAND_BARREL"));
        Assert.Equal("Industrial Barrel", StarshipDatabase.GetPartDisplayName("^ABAND_BARREL"));
    }

    [Fact]
    public void GetPartDisplayName_UnknownId_ReturnsEmpty()
    {
        Assert.Equal("", StarshipDatabase.GetPartDisplayName("^NONEXISTENT_OBJECT"));
        Assert.Equal("", StarshipDatabase.GetPartDisplayName(""));
    }

    [Fact]
    public void GetDisplayName_UsesPartMapWithoutDatabase()
    {
        // Even without game database loaded, GetDisplayName should return names
        StarshipDatabase.Clear();
        Assert.Equal("Round Table", StarshipDatabase.GetDisplayName("^BUILDTABLE"));
        Assert.Equal("Defence Field", StarshipDatabase.GetDisplayName("^B_SHL_D"));
    }

    [Fact]
    public void ReorderBuildingObjects_OtherGroupSortsAlphabeticallyByPartName()
    {
        // Verify Other group items sort by object list display name, not original index
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        // All "Other" category items, with display names:
        // ^BUILDTABLE -> "Round Table" (R)
        // ^ABAND_BARREL -> "Industrial Barrel" (I)
        // ^B_SHL_D -> "Defence Field" (D)
        AddBuildingObject(objects, "^BUILDTABLE", 0);
        AddBuildingObject(objects, "^ABAND_BARREL", 1);
        AddBuildingObject(objects, "^B_SHL_D", 2);

        StarshipLogic.ReorderBuildingObjects(objects);

        // Should be alphabetical: Defence Field, Industrial Barrel, Round Table
        Assert.Equal("^B_SHL_D", objects.GetObject(0).GetString("ObjectID"));
        Assert.Equal("^ABAND_BARREL", objects.GetObject(1).GetString("ObjectID"));
        Assert.Equal("^BUILDTABLE", objects.GetObject(2).GetString("ObjectID"));
    }

    [Fact]
    public void ReorderBuildingObjects_SubOrderWithinCategories()
    {
        // Verify sub-order within Reactor category matches priority map order
        StarshipDatabase.Clear();
        var objects = new JsonArray();

        // Reactor order: B_GEN_1 (subOrder 0), B_GEN_0 (subOrder 1), B_GEN_2 (subOrder 2)
        AddBuildingObject(objects, "^B_GEN_2", 0);  // subOrder 2
        AddBuildingObject(objects, "^B_GEN_0", 1);  // subOrder 1
        AddBuildingObject(objects, "^B_GEN_1", 2);  // subOrder 0

        StarshipLogic.ReorderBuildingObjects(objects);

        Assert.Equal("^B_GEN_1", objects.GetObject(0).GetString("ObjectID")); // subOrder 0
        Assert.Equal("^B_GEN_0", objects.GetObject(1).GetString("ObjectID")); // subOrder 1
        Assert.Equal("^B_GEN_2", objects.GetObject(2).GetString("ObjectID")); // subOrder 2
    }

    // --- IsCorvetteOptimised tests ---

    [Fact]
    public void IsCorvetteOptimised_AlreadySorted_ReturnsTrue()
    {
        StarshipDatabase.Clear();
        var bases = BuildBaseArray(0, 0x123,
            ("^B_GEN_1", 1), ("^B_TRU_C", 2), ("^B_LND_A", 3), ("^BUILDTABLE", 0));

        Assert.True(StarshipLogic.IsCorvetteOptimised(bases, 0));
    }

    [Fact]
    public void IsCorvetteOptimised_Unsorted_ReturnsFalse()
    {
        StarshipDatabase.Clear();
        var bases = BuildBaseArray(0, 0x123,
            ("^BUILDTABLE", 0), ("^B_GEN_1", 1), ("^B_TRU_C", 2));

        Assert.False(StarshipLogic.IsCorvetteOptimised(bases, 0));
    }

    [Fact]
    public void IsCorvetteOptimised_NullBases_ReturnsTrue()
    {
        Assert.True(StarshipLogic.IsCorvetteOptimised(null, 0));
    }

    [Fact]
    public void IsCorvetteOptimised_SingleObject_ReturnsTrue()
    {
        StarshipDatabase.Clear();
        var bases = BuildBaseArray(0, 0x123, ("^B_GEN_0", 0));

        Assert.True(StarshipLogic.IsCorvetteOptimised(bases, 0));
    }

    [Fact]
    public void IsCorvetteOptimised_NoMatchingBase_ReturnsTrue()
    {
        StarshipDatabase.Clear();
        var bases = BuildBaseArray(0, 0x123,
            ("^B_GEN_1", 1), ("^B_TRU_C", 2));

        // Different ship index, so base is not found -> returns true (nothing to optimise)
        Assert.True(StarshipLogic.IsCorvetteOptimised(bases, 99));
    }

    /// <summary>
    /// Builds a PersistentPlayerBases array with a single corvette base entry
    /// for use in IsCorvetteOptimised tests.
    /// </summary>
    private static JsonArray BuildBaseArray(int shipIndex, long seed, params (string objectId, long userData)[] parts)
    {
        var objects = new JsonArray();
        foreach (var (objectId, userData) in parts)
            AddBuildingObject(objects, objectId, userData);

        // Build a base object matching FindCorvetteBaseIndex expectations:
        // Strategy 1 matches by UserData == shipIndex and BaseType == "PlayerShipBase"
        var baseObj = new JsonObject();
        baseObj.Set("BaseType", new JsonObject());
        baseObj.GetObject("BaseType")!.Set("PersistentBaseTypes", "PlayerShipBase");
        baseObj.Set("Objects", objects);
        baseObj.Set("UserData", shipIndex);

        var bases = new JsonArray();
        bases.Add(baseObj);
        return bases;
    }

    // --- InvalidateCorvetteBase tests ---

    [Fact]
    public void InvalidateCorvetteBase_ClearsObjects()
    {
        var bases = BuildBaseArray(0, 0x123,
            ("YOURSHIP_REACTOR", 0),
            ("YOURSHIP_ENGINE", 0),
            ("YOURSHIP_COCKPIT", 0));

        // Base should have 3 objects before invalidation
        var baseObj = bases.GetObject(0);
        Assert.Equal(3, baseObj.GetArray("Objects")!.Length);

        StarshipLogic.InvalidateCorvetteBase(bases, 0);

        // After invalidation, the Objects array should be empty
        Assert.Equal(0, baseObj.GetArray("Objects")!.Length);
    }

    [Fact]
    public void InvalidateCorvetteBase_NullBases_DoesNotThrow()
    {
        // Should not throw when bases is null
        StarshipLogic.InvalidateCorvetteBase(null, 0);
    }

    [Fact]
    public void InvalidateCorvetteBase_NoMatchingBase_DoesNothing()
    {
        var bases = BuildBaseArray(0, 0x123,
            ("YOURSHIP_REACTOR", 0));

        // Try to invalidate with a non-matching ship index
        StarshipLogic.InvalidateCorvetteBase(bases, 5);

        // Objects should remain intact since no base matched
        var baseObj = bases.GetObject(0);
        Assert.Equal(1, baseObj.GetArray("Objects")!.Length);
    }

    // --- Corvette base import: empty slot (no existing base) ---

    [Fact]
    public void FindCorvetteBaseIndex_EmptySlot_ReturnsNegative()
    {
        // When importing into an empty slot, no base matches the new ship index.
        var bases = BuildBaseArray(0, 0x123, ("REACTOR", 0));

        // A different slot (7) should not match
        int idx = StarshipLogic.FindCorvetteBaseIndex(bases, 7);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void FindCorvetteBaseIndex_SameSeedDifferentSlot_DoesNotStealBase()
    {
        // Reproduces the reported corvette import bug: importing a corvette with the
        // same seed as an existing corvette into a new empty slot should NOT find (and
        // thus steal) the existing corvette's base.
        var json = JsonObject.Parse("""
        {
            "Bases": [
                {
                    "Owner": { "TS": 1774919312 },
                    "BaseType": { "PersistentBaseTypes": "PlayerShipBase" },
                    "UserData": 5
                }
            ]
        }
        """);
        var bases = json.GetArray("Bases")!;

        // Looking for slot 7 — must NOT find slot 5's base
        int idx = StarshipLogic.FindCorvetteBaseIndex(bases, 7);
        Assert.Equal(-1, idx);

        // Slot 5 should still find its own base via UserData match
        int idx5 = StarshipLogic.FindCorvetteBaseIndex(bases, 5);
        Assert.Equal(0, idx5);
    }

    [Fact]
    public void AddingBaseToArray_IncreasesLength()
    {
        // Verifies that adding a new base to PersistentPlayerBases works correctly.
        // This is the mechanism used when importing a corvette into an empty slot
        // that has no existing base entry.
        var bases = new JsonArray();
        Assert.Equal(0, bases.Length);

        var newBase = new JsonObject();
        newBase.Set("BaseType", new JsonObject());
        newBase.GetObject("BaseType")!.Set("PersistentBaseTypes", "PlayerShipBase");
        newBase.Set("Objects", new JsonArray());
        newBase.Set("UserData", 5);
        bases.Add(newBase);

        Assert.Equal(1, bases.Length);
        Assert.Equal(5, bases.GetObject(0).GetInt("UserData"));
    }

    [Fact]
    public void FindCorvetteBaseIndex_AfterAddingBase_FindsNewEntry()
    {
        // Simulates the corvette import into an empty slot: after adding a new base
        // entry with the correct UserData, FindCorvetteBaseIndex should find it.
        var bases = new JsonArray();

        // No base exists yet
        Assert.Equal(-1, StarshipLogic.FindCorvetteBaseIndex(bases, 7));

        // Add a base for slot 7
        var newBase = new JsonObject();
        newBase.Set("BaseType", new JsonObject());
        newBase.GetObject("BaseType")!.Set("PersistentBaseTypes", "PlayerShipBase");
        newBase.Set("Objects", new JsonArray());
        newBase.Set("UserData", 7);
        bases.Add(newBase);

        // Now it should be found via UserData matching
        int idx = StarshipLogic.FindCorvetteBaseIndex(bases, 7);
        Assert.Equal(0, idx);
    }

    [Fact]
    public void SetShipCustomisation_CcdPriorityOrder()
    {
        // Verifies that SetShipCustomisation correctly writes CCD data to the array.
        // This tests the CCD application mechanism used during import.
        var ccdArray = BuildCcdArray(26);
        int shipIndex = 3; // Maps to CCD index 6 (3 + 3)

        var ccdEntry = JsonObject.Parse("""
        {
            "SelectedPreset":"TestPreset",
            "CustomData":{
                "DescriptorGroups":["group1"],
                "PaletteID":"test_palette",
                "Colours":["blue"],
                "TextureOptions":[],
                "BoneScales":[],
                "Scale":1.0
            }
        }
        """);

        StarshipLogic.SetShipCustomisation(ccdArray, shipIndex, ccdEntry);

        // Verify the CCD was written to the correct index
        var written = ccdArray.GetObject(6); // shipIndex 3 -> CCD index 6
        Assert.Equal("TestPreset", written.GetString("SelectedPreset"));
    }

    [Fact]
    public void IsCcdDefault_WrapperFormatCcd_DetectsNonDefault()
    {
        // CCD from the new wrapper-level CharacterCustomisationData key
        // should be correctly detected as non-default when it has data.
        var ccd = JsonObject.Parse("""
        {
            "SelectedPreset":"^",
            "CustomData":{
                "DescriptorGroups":["active_group"],
                "PaletteID":"^",
                "Colours":[],
                "TextureOptions":[],
                "BoneScales":[],
                "Scale":1.0
            }
        }
        """);
        Assert.False(StarshipLogic.IsCcdDefault(ccd));
    }

    /// <summary>
    /// Builds a minimal CharacterCustomisationData array of the given size,
    /// with each entry set to defaults.
    /// </summary>
    private static JsonArray BuildCcdArray(int size)
    {
        var arr = new JsonArray();
        for (int i = 0; i < size; i++)
        {
            arr.Add(JsonObject.Parse("""
            {
                "SelectedPreset":"^",
                "CustomData":{
                    "DescriptorGroups":[],
                    "PaletteID":"^",
                    "Colours":[],
                    "TextureOptions":[],
                    "BoneScales":[],
                    "Scale":1.0
                }
            }
            """));
        }
        return arr;
    }

    // --- IsFilenameModified tests ---

    [Fact]
    public void IsFilenameModified_CanonicalFilename_ReturnsFalse()
    {
        Assert.False(StarshipLogic.IsFilenameModified("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN"));
    }

    [Fact]
    public void IsFilenameModified_CustomFilename_ReturnsTrue()
    {
        Assert.True(StarshipLogic.IsFilenameModified("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN"));
    }

    [Fact]
    public void IsFilenameModified_EmptyFilename_ReturnsFalse()
    {
        Assert.False(StarshipLogic.IsFilenameModified(""));
    }

    [Fact]
    public void IsFilenameModified_NullFilename_ReturnsFalse()
    {
        Assert.False(StarshipLogic.IsFilenameModified(null!));
    }

    [Fact]
    public void IsFilenameModified_AllCanonicalFilenames_ReturnFalse()
    {
        foreach (var key in StarshipLogic.ShipInfo.Keys)
        {
            Assert.False(StarshipLogic.IsFilenameModified(key),
                $"Canonical filename '{key}' should not be detected as modified.");
        }
    }

    // --- LoadShipData modified detection ---

    [Fact]
    public void LoadShipData_CanonicalFilename_IsResourceModifiedFalse()
    {
        var ship = JsonObject.Parse("""
        {
            "Name": "TestExplorer",
            "Resource": {
                "Filename": "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN",
                "Seed": [true, "0x1234"]
            },
            "Inventory": { "Class": { "InventoryClass": "S" } }
        }
        """);
        var data = StarshipLogic.LoadShipData(ship, null);
        Assert.Equal("Explorer", data.ShipTypeName);
        Assert.False(data.IsResourceModified);
    }

    [Fact]
    public void LoadShipData_CustomFilename_IsResourceModifiedTrue()
    {
        var ship = JsonObject.Parse("""
        {
            "Name": "ModExplorer",
            "Resource": {
                "Filename": "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN",
                "Seed": [true, "0x5678"]
            },
            "Inventory": { "Class": { "InventoryClass": "A" } }
        }
        """);
        var data = StarshipLogic.LoadShipData(ship, null);
        Assert.Equal("Explorer", data.ShipTypeName);
        Assert.True(data.IsResourceModified);
        Assert.Equal("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN", data.Filename);
    }

    // --- SaveShipData with custom filename ---

    [Fact]
    public void SaveShipData_WithCustomFilename_PreservesCustomValue()
    {
        var ship = JsonObject.Parse("""
        {
            "Name": "",
            "Resource": {
                "Filename": "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN",
                "Seed": [true, "0x1234"]
            },
            "Inventory": { "Class": { "InventoryClass": "A" } }
        }
        """);
        var playerState = JsonObject.Parse("""{ "ShipUsesLegacyColours": [false] }""");

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "ModExplorer",
            SelectedTypeName = "Explorer",
            CustomFilename = "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN",
            ClassIndex = 3,
            Seed = "0x1234",
            ShipIndex = 0
        };

        StarshipLogic.SaveShipData(ship, playerState, values);

        var resource = ship.GetObject("Resource");
        Assert.Equal("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN",
            resource!.GetString("Filename"));
    }

    [Fact]
    public void SaveShipData_WithoutCustomFilename_WritesCanonicalValue()
    {
        var ship = JsonObject.Parse("""
        {
            "Name": "",
            "Resource": {
                "Filename": "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN",
                "Seed": [true, "0x1234"]
            },
            "Inventory": { "Class": { "InventoryClass": "A" } }
        }
        """);
        var playerState = JsonObject.Parse("""{ "ShipUsesLegacyColours": [false] }""");

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = "NormalExplorer",
            SelectedTypeName = "Explorer",
            CustomFilename = null,
            ClassIndex = 3,
            Seed = "0x1234",
            ShipIndex = 0
        };

        StarshipLogic.SaveShipData(ship, playerState, values);

        var resource = ship.GetObject("Resource");
        Assert.Equal("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN",
            resource!.GetString("Filename"));
    }

    // --- ShipTypeItem with custom filename ---

    [Fact]
    public void ShipTypeItem_DefaultHasNullCustomFilename()
    {
        var item = new StarshipLogic.ShipTypeItem("Explorer", "Explorer");
        Assert.Null(item.CustomFilename);
    }

    [Fact]
    public void ShipTypeItem_ModifiedCarriesCustomFilename()
    {
        var item = new StarshipLogic.ShipTypeItem("Explorer", "Explorer (Modified)",
            "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN");
        Assert.Equal("MODELS/COMMON/SPACECRAFT/SCIENTIFIC/CANOPY/CANOPYA/CANOPYA.SCENE.MBIN", item.CustomFilename);
        Assert.Equal("Explorer", item.InternalName);
    }
}
