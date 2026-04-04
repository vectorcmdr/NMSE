using NMSE.Data;

namespace NMSE.Tests;

/// <summary>
/// Tests for data layer classes: WordDatabase, CompanionDatabase, GalaxyDatabase,
/// TechAdjacencyDatabase, JsonNameMapper, and CoordinateHelper.
/// Shares the MutableStaticDatabases collection to prevent parallel execution
/// with UiStringsTests / DatabaseLocalisationTests which mutate UiStrings state.
/// </summary>
[Collection("MutableStaticDatabases")]
public class DataLayerTests
{
    public DataLayerTests()
    {
        EnsureUiStringsLoaded();
    }

    private static void EnsureUiStringsLoaded()
    {
        if (UiStrings.TotalKeyCount > 0) return;
        var langDir = FindResourceLangDir();
        if (langDir == null) return;
        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");
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

    // --- WordDatabase ------------------------------------------------

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

    private static WordDatabase LoadWordDb()
    {
        var db = new WordDatabase();
        var jsonDir = FindResourceJsonDir();
        Assert.NotNull(jsonDir);
        Assert.True(db.LoadFromFile(Path.Combine(jsonDir!, "Words.json")),
            "WordDatabase should load from Words.json");
        return db;
    }

    [Fact]
    public void WordDatabase_LoadFromFile_PopulatesWords()
    {
        var db = LoadWordDb();
        Assert.True(db.Words.Count > 0, "WordDatabase should contain entries after LoadFromFile()");
    }

    [Fact]
    public void WordDatabase_Count_MatchesWordsList()
    {
        var db = LoadWordDb();
        Assert.Equal(db.Words.Count, db.Count);
    }

    [Fact]
    public void WordDatabase_Entries_HaveValidData()
    {
        var db = LoadWordDb();
        foreach (var word in db.Words)
        {
            Assert.False(string.IsNullOrEmpty(word.Id), "Word Id should not be empty");
            Assert.False(string.IsNullOrEmpty(word.Text), "Word Text should not be empty");
        }
    }

    [Fact]
    public void WordDatabase_KnownWord_Atlas_HasExpectedGroups()
    {
        var db = LoadWordDb();
        var atlas = db.Words.FirstOrDefault(w => w.Id == "^ATLAS");
        Assert.NotNull(atlas);
        Assert.Equal("atlas", atlas.Text);
        Assert.True(atlas.Groups.Count > 0, "Atlas word should have group mappings");
        Assert.True(atlas.HasRace(4), "Atlas word should have Atlas race (ordinal 4)");
    }

    [Fact]
    public void WordDatabase_Words_AreSortedAlphabetically()
    {
        var db = LoadWordDb();
        for (int i = 1; i < db.Words.Count; i++)
        {
            Assert.True(
                string.Compare(db.Words[i - 1].Text, db.Words[i].Text, StringComparison.OrdinalIgnoreCase) <= 0,
                $"Words should be sorted: '{db.Words[i - 1].Text}' should come before '{db.Words[i].Text}'");
        }
    }

    [Fact]
    public void WordDatabase_LoadFromFile_MissingFile_ReturnsFalse()
    {
        var db = new WordDatabase();
        Assert.False(db.LoadFromFile("/nonexistent/Words.json"));
        Assert.Equal(0, db.Count);
    }

    [Fact]
    public void WordDatabase_LoadFromFile_InvalidJson_ReturnsFalse()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "not valid json");
            var db = new WordDatabase();
            Assert.False(db.LoadFromFile(tmp));
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void WordDatabase_LoadFromFile_EmptyArray_ReturnsFalse()
    {
        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "[]");
            var db = new WordDatabase();
            Assert.False(db.LoadFromFile(tmp));
        }
        finally { File.Delete(tmp); }
    }

    // --- CompanionDatabase -------------------------------------------

    [Fact]
    public void CompanionDatabase_Entries_AreNotEmpty()
    {
        Assert.True(CompanionDatabase.Entries.Count > 0);
    }

    [Fact]
    public void CompanionDatabase_ById_ContainsAllEntries()
    {
        Assert.Equal(CompanionDatabase.Entries.Count, CompanionDatabase.ById.Count);
    }

    [Theory]
    [InlineData("^QUAD_PET")]
    [InlineData("^TREX")]
    [InlineData("^FISH")]
    [InlineData("^SANDWORM")]
    public void CompanionDatabase_ById_ContainsKnownEntries(string id)
    {
        Assert.True(CompanionDatabase.ById.ContainsKey(id));
        Assert.Equal(id, CompanionDatabase.ById[id].Id);
    }

    [Fact]
    public void CompanionDatabase_ById_IsCaseInsensitive()
    {
        Assert.True(CompanionDatabase.ById.ContainsKey("^quad_pet"));
        Assert.True(CompanionDatabase.ById.ContainsKey("^QUAD_PET"));
    }

    [Fact]
    public void CompanionDatabase_Entries_HaveValidIds()
    {
        foreach (var entry in CompanionDatabase.Entries)
        {
            Assert.False(string.IsNullOrEmpty(entry.Id), "Companion Id should not be empty");
            Assert.StartsWith("^", entry.Id);
        }
    }

    // --- GalaxyDatabase ----------------------------------------------

    [Fact]
    public void GalaxyDatabase_Has256Galaxies()
    {
        Assert.Equal(257, GalaxyDatabase.Galaxies.Length);
    }

    [Theory]
    [InlineData(0, "Euclid")]
    [InlineData(1, "Hilbert Dimension")]
    [InlineData(9, "Eissentam")]
    [InlineData(255, "Odyalutai")]
    public void GalaxyDatabase_GetGalaxyName_ReturnsCorrectName(int index, string expected)
    {
        Assert.Equal(expected, GalaxyDatabase.GetGalaxyName(index));
    }

    [Theory]
    [InlineData(-1, "Unknown")]
    [InlineData(257, "Unknown")]
    [InlineData(999, "Unknown")]
    public void GalaxyDatabase_GetGalaxyName_OutOfRange_ReturnsUnknown(int index, string expected)
    {
        Assert.Equal(expected, GalaxyDatabase.GetGalaxyName(index));
    }

    [Theory]
    [InlineData(0, "Normal")]
    [InlineData(2, "Harsh")]
    [InlineData(6, "Empty")]
    [InlineData(9, "Lush")]
    public void GalaxyDatabase_GetGalaxyType_ReturnsCorrectType(int index, string expected)
    {
        Assert.Equal(expected, GalaxyDatabase.GetGalaxyType(index));
    }

    [Fact]
    public void GalaxyDatabase_GetGalaxyType_OutOfRange_ReturnsNormal()
    {
        Assert.Equal("Normal", GalaxyDatabase.GetGalaxyType(-1));
        Assert.Equal("Normal", GalaxyDatabase.GetGalaxyType(257));
    }

    [Theory]
    [InlineData(0, "Euclid (1)")]
    [InlineData(9, "Eissentam (10)")]
    [InlineData(255, "Odyalutai (256)")]
    public void GalaxyDatabase_GetGalaxyDisplayName_ReturnsNameWithNumber(int index, string expected)
    {
        Assert.Equal(expected, GalaxyDatabase.GetGalaxyDisplayName(index));
    }

    [Fact]
    public void GalaxyDatabase_GetGalaxyDisplayName_OutOfRange_ReturnsUnknown()
    {
        Assert.Equal("Unknown", GalaxyDatabase.GetGalaxyDisplayName(-1));
        Assert.Equal("Unknown", GalaxyDatabase.GetGalaxyDisplayName(257));
    }

    [Fact]
    public void GalaxyDatabase_AllGalaxies_HaveValidTypes()
    {
        var validTypes = new HashSet<string> { "Normal", "Harsh", "Empty", "Lush" };
        foreach (var galaxy in GalaxyDatabase.Galaxies)
        {
            Assert.Contains(galaxy.Type, validTypes);
        }
    }

    // --- TechAdjacencyDatabase ---------------------------------------

    [Fact]
    public void TechAdjacencyDatabase_Dictionary_IsNotEmpty()
    {
        Assert.True(TechAdjacencyDatabase.Dictionary.Count > 0);
    }

    [Theory]
    [InlineData("^BOLT", 12)]
    [InlineData("BOLT", 12)]
    [InlineData("^LASER", 1)]
    public void TechAdjacencyDatabase_GetAdjacencyInfo_ReturnsValidData(string itemId, int expectedBaseStatType)
    {
        var info = TechAdjacencyDatabase.GetAdjacencyInfo(itemId);
        Assert.NotNull(info);
        Assert.Equal(expectedBaseStatType, info.BaseStatType);
    }

    [Fact]
    public void TechAdjacencyDatabase_GetAdjacencyInfo_StripsVariantSuffix()
    {
        var info = TechAdjacencyDatabase.GetAdjacencyInfo("^BOLT#12345");
        Assert.NotNull(info);
        Assert.Equal(12, info.BaseStatType);
    }

    [Fact]
    public void TechAdjacencyDatabase_GetAdjacencyInfo_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(TechAdjacencyDatabase.GetAdjacencyInfo(null!));
        Assert.Null(TechAdjacencyDatabase.GetAdjacencyInfo(""));
    }

    [Fact]
    public void TechAdjacencyDatabase_GetAdjacencyInfo_UnknownItem_ReturnsNull()
    {
        Assert.Null(TechAdjacencyDatabase.GetAdjacencyInfo("^NONEXISTENT_ITEM_XYZ"));
    }

    [Fact]
    public void TechAdjacencyDatabase_AllEntries_HaveValidLinkColour()
    {
        foreach (var kvp in TechAdjacencyDatabase.Dictionary)
        {
            Assert.False(string.IsNullOrEmpty(kvp.Value.LinkColourHex),
                $"Entry '{kvp.Key}' should have a non-empty LinkColourHex");
            Assert.StartsWith("#", kvp.Value.LinkColourHex);
        }
    }

    // --- JsonNameMapper ----------------------------------------------

    [Fact]
    public void JsonNameMapper_Load_FromStream_PopulatesMappings()
    {
        var mapper = new JsonNameMapper();
        string json = """{"Mapping":[{"Key":"F2P","Value":"BaseVersion"},{"Key":"8bB","Value":"GameMode"}]}""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        mapper.Load(stream);
        Assert.Equal(2, mapper.Count);
    }

    [Fact]
    public void JsonNameMapper_ToName_ReturnsHumanReadableName()
    {
        var mapper = CreateTestMapper();
        Assert.Equal("BaseVersion", mapper.ToName("F2P"));
    }

    [Fact]
    public void JsonNameMapper_ToName_UnknownKey_ReturnsKeyUnchanged()
    {
        var mapper = CreateTestMapper();
        Assert.Equal("UNKNOWN", mapper.ToName("UNKNOWN"));
    }

    [Fact]
    public void JsonNameMapper_ToKey_ReturnsObfuscatedKey()
    {
        var mapper = CreateTestMapper();
        Assert.Equal("F2P", mapper.ToKey("BaseVersion"));
    }

    [Fact]
    public void JsonNameMapper_ToKey_UnknownName_ReturnsNameUnchanged()
    {
        var mapper = CreateTestMapper();
        Assert.Equal("NoSuchName", mapper.ToKey("NoSuchName"));
    }

    [Fact]
    public void JsonNameMapper_IsObfuscatedKey_ReturnsTrueForKnownKeys()
    {
        var mapper = CreateTestMapper();
        Assert.True(mapper.IsObfuscatedKey("F2P"));
        Assert.True(mapper.IsObfuscatedKey("8bB"));
    }

    [Fact]
    public void JsonNameMapper_IsObfuscatedKey_ReturnsFalseForUnknownKeys()
    {
        var mapper = CreateTestMapper();
        Assert.False(mapper.IsObfuscatedKey("BaseVersion"));
        Assert.False(mapper.IsObfuscatedKey("UNKNOWN"));
    }

    [Fact]
    public void JsonNameMapper_Load_InvalidFormat_Throws()
    {
        var mapper = new JsonNameMapper();
        string content = "not json at all";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        Assert.Throws<InvalidDataException>(() => mapper.Load(stream));
    }

    private static JsonNameMapper CreateTestMapper()
    {
        var mapper = new JsonNameMapper();
        string json = """{"Mapping":[{"Key":"F2P","Value":"BaseVersion"},{"Key":"8bB","Value":"GameMode"}]}""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        mapper.Load(stream);
        return mapper;
    }

    // --- CoordinateHelper --------------------------------------------

    [Fact]
    public void CoordinateHelper_VoxelToPortalCode_Origin_Returns12Chars()
    {
        string code = CoordinateHelper.VoxelToPortalCode(0, 0, 0, 0, 0);
        Assert.Equal(12, code.Length);
        Assert.Equal("000000000000", code);
    }

    [Fact]
    public void CoordinateHelper_VoxelToPortalCode_NonZero_Returns12Chars()
    {
        string code = CoordinateHelper.VoxelToPortalCode(100, 50, -200, 42, 3);
        Assert.Equal(12, code.Length);
    }

    [Theory]
    [InlineData("00E4FF91310A", "1,1,15,5,16,16,10,2,4,2,1,11")]
    [InlineData("000000000000", "1,1,1,1,1,1,1,1,1,1,1,1")]
    public void CoordinateHelper_PortalHexToDec_ConvertsCorrectly(string portalCode, string expected)
    {
        Assert.Equal(expected, CoordinateHelper.PortalHexToDec(portalCode));
    }

    [Fact]
    public void CoordinateHelper_PortalHexToDec_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", CoordinateHelper.PortalHexToDec(""));
        Assert.Equal("", CoordinateHelper.PortalHexToDec(null!));
    }

    [Fact]
    public void CoordinateHelper_GetDistanceToCenter_AtOrigin_IsZero()
    {
        Assert.Equal(0.0, CoordinateHelper.GetDistanceToCenter(0, 0, 0));
    }

    [Fact]
    public void CoordinateHelper_GetDistanceToCenter_KnownVector()
    {
        // Distance of (3,4,0) = 5, * 100 = 500 ly
        Assert.Equal(500.0, CoordinateHelper.GetDistanceToCenter(3, 4, 0));
    }

    [Theory]
    [InlineData(1000.0, 100.0, 10)]
    [InlineData(500.0, 200.0, 3)]
    [InlineData(0.0, 100.0, 0)]
    public void CoordinateHelper_GetJumpsToCenter_CalculatesCorrectly(
        double distance, double perJump, int expected)
    {
        Assert.Equal(expected, CoordinateHelper.GetJumpsToCenter(distance, perJump));
    }

    [Fact]
    public void CoordinateHelper_GetJumpsToCenter_ZeroRange_ReturnsZero()
    {
        Assert.Equal(0, CoordinateHelper.GetJumpsToCenter(1000.0, 0.0));
    }

    [Fact]
    public void CoordinateHelper_GetJumpsToCenter_NegativeRange_ReturnsZero()
    {
        Assert.Equal(0, CoordinateHelper.GetJumpsToCenter(1000.0, -50.0));
    }
}
