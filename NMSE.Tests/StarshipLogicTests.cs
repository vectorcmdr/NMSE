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
}
