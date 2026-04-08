using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class CategorizerTests
{
    private static Dictionary<string, object?> MakeItem(string id, string name, string group) => new()
    {
        ["Id"] = id, ["Name"] = name, ["Group"] = group
    };

    [Fact]
    public void CategorizeItem_EmptyGroup_ReturnsNull()
    {
        var item = MakeItem("TEST", "Test Item", "");
        Assert.Null(Categorizer.CategorizeItem(item));
    }

    [Theory]
    [InlineData("Edible Product", "Food.json")]
    [InlineData("Carnivore Bait", "Food.json")]
    [InlineData("Crafted Technology Component", "Products.json")]
    [InlineData("Mineral", "Raw Materials.json")]
    [InlineData("Fuel", "Raw Materials.json")]
    [InlineData("Common Fish", "Fish.json")]
    [InlineData("Trade Goods", "Trade.json")]
    [InlineData("Construction module", "Buildings.json")]
    [InlineData("Decoration", "Buildings.json")]
    [InlineData("Wall Access Route", "Buildings.json")]
    [InlineData("Constructable Relic", "Buildings.json")]
    [InlineData("Access Card", "Constructed Technology.json")]
    [InlineData("High value curiosity", "Curiosities.json")]
    [InlineData("Mission Location System", "Corvette.json")]
    [InlineData("Self-Mounted Refiner Unit", "Technology.json")]
    [InlineData("Priceless Fragment", "Technology.json")]
    public void CategorizeItem_ExactGroupMatches(string group, string expectedFile)
    {
        var item = MakeItem("TEST", "Test Item", group);
        Assert.Equal(expectedFile, Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_UpgradeInGroup_GoesToUpgrades()
    {
        var item = MakeItem("TEST", "Test Item", "A-Class Hyperdrive Upgrade");
        Assert.Equal("Upgrades.json", Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_UpgradeInName_GoesToUpgrades()
    {
        var item = MakeItem("TEST", "Some Upgrade Module", "SomeGroup");
        Assert.Equal("Upgrades.json", Categorizer.CategorizeItem(item));
    }

    [Theory]
    [InlineData("Corvette Hull", "Corvette.json")]
    [InlineData("Corvette Engine", "Corvette.json")]
    public void CategorizeItem_CorvettePrefixMatches(string group, string expectedFile)
    {
        var item = MakeItem("TEST", "Test Item", group);
        Assert.Equal(expectedFile, Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_ExocraftInGroup_GoesToExocraft()
    {
        var item = MakeItem("TEST", "Test Item", "Exocraft Tech");
        Assert.Equal("Exocraft.json", Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_DynamicTechModulePattern_GoesToTechModule()
    {
        var item = MakeItem("TEST", "Test Item", "S-Class Mining Beam Upgrade");
        // "upgrade" keyword sends it to Upgrades.json (higher priority)
        Assert.Equal("Upgrades.json", Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_JunkGroup_ReturnsNull()
    {
        var item = MakeItem("TEST", "Test Item", "Biggs Test Group");
        Assert.Null(Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_UntranslatedName_ReturnsNull()
    {
        var item = MakeItem("UI_TEST", "UI_TEST", "Some Valid Group");
        Assert.Null(Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_UnknownGroup_GoesToOthers()
    {
        var item = MakeItem("TEST", "Test Item", "Never Before Seen Category");
        Assert.Equal("Others.json", Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_TechPackException_ReturnsNull()
    {
        var item = MakeItem("U_TECHPACK_CORE", "Core Package", "Archived Technology Package");
        Assert.Null(Categorizer.CategorizeItem(item));
    }

    [Fact]
    public void CategorizeItem_NonTechPackFallthrough_GoesToOthers()
    {
        var item = MakeItem("U_TECHBOX_ALIEN", "Alien Implant", "Potent Nodule");
        Assert.Equal("Others.json", Categorizer.CategorizeItem(item));
    }

    // Verify Raw Materials re-routing categories match expected
    [Theory]
    [InlineData("Reward Item", "Others.json")]
    [InlineData("Technological Currency", "Others.json")]
    [InlineData("Anomalous Material", "Curiosities.json")]
    [InlineData("Compressed Atmospheric Gas", "Products.json")]
    public void CategorizeItem_RawMaterialReRoutingGroups(string group, string expectedFile)
    {
        var item = MakeItem("TEST", "Test Material", group);
        Assert.Equal(expectedFile, Categorizer.CategorizeItem(item));
    }

    // Verify T_BOBBLE items go to Others (Starship Interior Adornment)
    [Fact]
    public void CategorizeItem_StarshipInteriorAdornment_GoesToOthers()
    {
        var item = MakeItem("BOBBLE_ATLAS", "Atlas Bobblehead", "Starship Interior Adornment");
        Assert.Equal("Others.json", Categorizer.CategorizeItem(item));
    }

    // Verify "Exclusive Spacecraft" goes to Starships (Starship routing runs before exact rules)
    [Fact]
    public void CategorizeItem_ExclusiveSpacecraft_GoesToStarships()
    {
        var item = MakeItem("TEST", "Test Ship", "Exclusive Spacecraft");
        Assert.Equal("Starships.json", Categorizer.CategorizeItem(item));
    }

    // Verify Starship Core Component goes to Upgrades (via StarshipUpgradeGroups)
    [Fact]
    public void CategorizeItem_StarshipCoreComponent_GoesToUpgrades()
    {
        var item = MakeItem("TEST", "Test Core", "Starship Core Component");
        Assert.Equal("Upgrades.json", Categorizer.CategorizeItem(item));
    }

    // Verify ship component groups go to Others (excluded from Starships)
    [Theory]
    [InlineData("Starship Exhaust Override", "Others.json")]
    [InlineData("Damaged Starship Component", "Others.json")]
    public void CategorizeItem_ExcludedStarshipGroups_GoToOthers(string group, string expectedFile)
    {
        var item = MakeItem("TEST", "Test Component", group);
        Assert.Equal(expectedFile, Categorizer.CategorizeItem(item));
    }
}
