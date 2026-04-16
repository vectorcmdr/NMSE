using NMSE.Extractor.Config;
using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class ParsersIntegrationTests
{
    private string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"nmse_parser_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, ExtractorConfig.JsonSubfolder));
        return dir;
    }

    [Fact]
    public void ParseAllRecipes_ReturnsRecipesFromMxml()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcRecipeTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcRefinerRecipe.xml"">
      <Property name=""Id"" value=""REFINERECIPE_1"" />
      <Property name=""RecipeName"" value=""UI_REFINE_NAME_1"" />
      <Property name=""RecipeType"" value=""Standard"" />
      <Property name=""RecipeCategory"" value=""Refining"" />
      <Property name=""Cooking"" value=""False"" />
      <Property name=""TimeToMake"" value=""60"" />
      <Property name=""Result"">
        <Property name=""Id"" value=""FUEL1"" />
        <Property name=""Type""><Property name=""InventoryType"" value=""Product"" /></Property>
        <Property name=""Amount"" value=""2"" />
      </Property>
      <Property name=""Ingredients"">
        <Property value=""GcRefinerRecipeElement.xml"">
          <Property name=""Id"" value=""CARBON"" />
          <Property name=""Type""><Property name=""InventoryType"" value=""Substance"" /></Property>
          <Property name=""Amount"" value=""1"" />
        </Property>
      </Property>
    </Property>
    <Property name=""Table"" value=""GcRefinerRecipe.xml"">
      <Property name=""Id"" value=""COOKRECIPE_1"" />
      <Property name=""RecipeName"" value=""UI_COOK_NAME_1"" />
      <Property name=""RecipeType"" value=""Cooking"" />
      <Property name=""RecipeCategory"" value=""Cooking"" />
      <Property name=""Cooking"" value=""True"" />
      <Property name=""TimeToMake"" value=""30"" />
      <Property name=""Result"">
        <Property name=""Id"" value=""FOOD1"" />
        <Property name=""Type""><Property name=""InventoryType"" value=""Product"" /></Property>
        <Property name=""Amount"" value=""1"" />
      </Property>
      <Property name=""Ingredients"">
        <Property value=""GcRefinerRecipeElement.xml"">
          <Property name=""Id"" value=""PLANT1"" />
          <Property name=""Type""><Property name=""InventoryType"" value=""Substance"" /></Property>
          <Property name=""Amount"" value=""3"" />
        </Property>
      </Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "nms_reality_gcrecipetable.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var recipes = Parsers.ParseAllRecipes(file);

            Assert.Equal(2, recipes.Count);
            Assert.Equal("REFINERECIPE_1", recipes[0]["Id"]);
            Assert.Equal("COOKRECIPE_1", recipes[1]["Id"]);
            Assert.Equal(false, recipes[0]["Cooking"]);
            Assert.Equal(true, recipes[1]["Cooking"]);
            Assert.Equal(60, recipes[0]["TimeToMake"]);
            Assert.Equal(30, recipes[1]["TimeToMake"]);

            // Verify Result parsing
            var result = recipes[0]["Result"] as Dictionary<string, object?>;
            Assert.NotNull(result);
            Assert.Equal("FUEL1", result["Id"]);
            Assert.Equal(2, result["Amount"]);

            // Verify Ingredients parsing
            var ingredients = recipes[0]["Ingredients"] as List<Dictionary<string, object?>>;
            Assert.NotNull(ingredients);
            Assert.Single(ingredients);
            Assert.Equal("CARBON", ingredients[0]["Id"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseTitles_ReturnsTitlesFromMxml()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcPlayerTitleData"">
  <Property name=""Titles"">
    <Property name=""Titles"" value=""GcPlayerTitle"">
      <Property name=""ID"" value=""TITLE_1"" />
      <Property name=""Title"" value=""UI_TITLE_1"" />
      <Property name=""UnlockDescription"" value=""UI_TITLE_DESC_1"" />
      <Property name=""AlreadyUnlockedDescription"" value=""UI_TITLE_ALREADY_1"" />
      <Property name=""UnlockedByStatValue"" value=""10"" />
    </Property>
    <Property name=""Titles"" value=""GcPlayerTitle"">
      <Property name=""ID"" value=""TITLE_2"" />
      <Property name=""Title"" value=""UI_TITLE_2"" />
      <Property name=""UnlockDescription"" value=""UI_TITLE_DESC_2"" />
      <Property name=""AlreadyUnlockedDescription"" value=""UI_TITLE_ALREADY_2"" />
      <Property name=""UnlockedByStatValue"" value=""25"" />
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "nms_reality_gcplayertitletable.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var titles = Parsers.ParseTitles(file);

            Assert.Equal(2, titles.Count);
            Assert.Equal("TITLE_1", titles[0]["Id"]);
            Assert.Equal("TITLE_2", titles[1]["Id"]);
            Assert.Equal(10L, titles[0]["UnlockedByStatValue"]);
            Assert.Equal(25L, titles[1]["UnlockedByStatValue"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseAllRecipes_RequiresNameTableAttribute()
    {
        // ParseAllRecipes now uses the same traversal as ParseRefinery:
        // only matching child elements with name="Table". Elements without
        // name="Table" are skipped (this matches actual NMS MXML format).
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcRecipeTable"">
  <Property name=""Table"">
    <Property value=""GcRefinerRecipe.xml"">
      <Property name=""Id"" value=""RECIPE_ALT_1"" />
      <Property name=""RecipeName"" value="""" />
      <Property name=""RecipeType"" value=""Standard"" />
      <Property name=""RecipeCategory"" value=""Refining"" />
      <Property name=""Cooking"" value=""False"" />
      <Property name=""TimeToMake"" value=""45"" />
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "nms_reality_gcrecipetable.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var recipes = Parsers.ParseAllRecipes(file);

            // Without name="Table" on inner elements, no recipes are parsed
            // (consistent with ParseRefinery's traversal pattern)
            Assert.Empty(recipes);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseProducts_IncludesIsCraftableAndProcedural()
    {
        string tmpDir = CreateTempDir();
        try
        {
            // Minimal product MXML with IsCraftable and Procedural fields
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcProductTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""CRAFTPROD1"" />
      <Property name=""Name"" value=""Craftable Product"" />
      <Property name=""Subtitle"" value=""Test Group"" />
      <Property name=""Description"" value=""A craftable product"" />
      <Property name=""IsCraftable"" value=""True"" />
      <Property name=""Procedural"" value=""False"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/UI/ITEMS/CRAFTPROD1.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
    </Property>
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""PROCPROD1"" />
      <Property name=""Name"" value=""Procedural Product"" />
      <Property name=""Subtitle"" value=""Proc Group"" />
      <Property name=""Description"" value=""A procedural product"" />
      <Property name=""IsCraftable"" value=""False"" />
      <Property name=""Procedural"" value=""True"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/UI/ITEMS/PROCPROD1.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "nms_reality_gcproducttable.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var products = Parsers.ParseProducts(file);

            Assert.Equal(2, products.Count);

            // First product: craftable, not procedural
            Assert.Equal("CRAFTPROD1", products[0]["Id"]);
            Assert.Equal(true, products[0]["IsCraftable"]);
            Assert.Equal(false, products[0]["Procedural"]);

            // Second product: not craftable, procedural
            Assert.Equal("PROCPROD1", products[1]["Id"]);
            Assert.Equal(false, products[1]["IsCraftable"]);
            Assert.Equal(true, products[1]["Procedural"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ProductLookup_ParseProductElement_IncludesIsCraftableAndProcedural()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcProductTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""TEST_ITEM"" />
      <Property name=""Name"" value=""Test Item"" />
      <Property name=""Subtitle"" value=""Test"" />
      <Property name=""Description"" value=""Desc"" />
      <Property name=""IsCraftable"" value=""True"" />
      <Property name=""Procedural"" value=""False"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/UI/ITEMS/TEST.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "nms_reality_gcproducttable.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var localisation = new Dictionary<string, string>();
            var lookup = ProductLookup.LoadProductLookup(localisation, file);

            Assert.True(lookup.ContainsKey("TEST_ITEM"));
            var item = lookup["TEST_ITEM"];
            Assert.True(item.ContainsKey("IsCraftable"));
            Assert.True(item.ContainsKey("Procedural"));
            Assert.Equal(true, item["IsCraftable"]);
            Assert.Equal(false, item["Procedural"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseBuildings_ExtractsCanPickUpAndIsTemporary()
    {
        string tmpDir = CreateTempDir();
        try
        {
            // Minimal basebuildingobjectstable MXML with CanPickUp/IsTemporary
            string buildingXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcBaseBuildingTable"">
  <Property name=""Objects"">
    <Property name=""Objects"" value=""GcBaseBuildingEntry"" _id=""WALL1"">
      <Property name=""ID"" value=""WALL1"" />
      <Property name=""IsTemporary"" value=""false"" />
      <Property name=""BuildableOnPlanetBase"" value=""true"" />
      <Property name=""BuildableOnSpaceBase"" value=""false"" />
      <Property name=""BuildableOnFreighter"" value=""false"" />
      <Property name=""CanPickUp"" value=""true"" />
      <Property name=""IconOverrideProductID"" value=""WALL1"" />
      <Property name=""Groups"">
        <Property name=""Groups"" value=""GcBaseBuildingEntryGroup"">
          <Property name=""Group"" value=""BASIC_WALLS"" />
          <Property name=""SubGroupName"" value=""WALLS"" />
        </Property>
      </Property>
      <Property name=""LinkGridData"" />
    </Property>
    <Property name=""Objects"" value=""GcBaseBuildingEntry"" _id=""FIREWORK1"">
      <Property name=""ID"" value=""FIREWORK1"" />
      <Property name=""IsTemporary"" value=""true"" />
      <Property name=""BuildableOnPlanetBase"" value=""true"" />
      <Property name=""BuildableOnSpaceBase"" value=""false"" />
      <Property name=""BuildableOnFreighter"" value=""false"" />
      <Property name=""CanPickUp"" value=""false"" />
      <Property name=""IconOverrideProductID"" value=""FIREWORK1"" />
      <Property name=""Groups"">
        <Property name=""Groups"" value=""GcBaseBuildingEntryGroup"">
          <Property name=""Group"" value=""PLANET_TECH"" />
          <Property name=""SubGroupName"" value=""PLANETPORTABLE"" />
        </Property>
      </Property>
      <Property name=""LinkGridData"" />
    </Property>
    <Property name=""Objects"" value=""GcBaseBuildingEntry"" _id=""PAVING1"">
      <Property name=""ID"" value=""PAVING1"" />
      <Property name=""IsTemporary"" value=""false"" />
      <Property name=""BuildableOnPlanetBase"" value=""true"" />
      <Property name=""BuildableOnSpaceBase"" value=""false"" />
      <Property name=""BuildableOnFreighter"" value=""false"" />
      <Property name=""CanPickUp"" value=""false"" />
      <Property name=""IconOverrideProductID"" value=""PAVING1"" />
      <Property name=""Groups"" />
      <Property name=""LinkGridData"" />
    </Property>
  </Property>
</Data>";

            // Minimal product table for icon lookup
            string productXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcProductTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""WALL1"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/UI/WALL1.DDS"" /></Property>
    </Property>
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""FIREWORK1"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/UI/FIREWORK1.DDS"" /></Property>
    </Property>
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""PAVING1"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/UI/PAVING1.DDS"" /></Property>
    </Property>
  </Property>
</Data>";

            // Write MXML files into a fake mbin dir structure
            string mbinDir = Path.Combine(tmpDir, "mbin");
            Directory.CreateDirectory(mbinDir);
            File.WriteAllText(Path.Combine(mbinDir, "basebuildingobjectstable.MXML"), buildingXml);
            File.WriteAllText(Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML"), productXml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var buildings = Parsers.ParseBuildings(Path.Combine(mbinDir, "basebuildingobjectstable.MXML"));

            Assert.Equal(3, buildings.Count);

            // WALL1: CanPickUp=true, IsTemporary=false
            var wall = buildings.First(b => b["Id"]?.ToString() == "WALL1");
            Assert.Equal(true, wall["CanPickUp"]);
            Assert.Equal(false, wall["IsTemporary"]);

            // FIREWORK1: CanPickUp=false, IsTemporary=true
            var firework = buildings.First(b => b["Id"]?.ToString() == "FIREWORK1");
            Assert.Equal(false, firework["CanPickUp"]);
            Assert.Equal(true, firework["IsTemporary"]);

            // PAVING1: CanPickUp=false, IsTemporary=false (non-pickupable permanent structure)
            var paving = buildings.First(b => b["Id"]?.ToString() == "PAVING1");
            Assert.Equal(false, paving["CanPickUp"]);
            Assert.Equal(false, paving["IsTemporary"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseRewards_ParsesSeasonTwitchPlatformFromMxml()
    {
        string tmpDir = CreateTempDir();
        try
        {
            // Create a minimal products table for name resolution
            string productsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcProductTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""VAULT_ARMOUR"" />
      <Property name=""Name"" value=""UI_VAULT_ARMOUR_NAME"" />
      <Property name=""NameLower"" value=""UI_VAULT_ARMOUR_NAME_L"" />
      <Property name=""Subtitle"" value="""" />
      <Property name=""Description"" value="""" />
      <Property name=""Hint"" value="""" />
      <Property name=""BaseValue"" value=""100"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/VAULT.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
      <Property name=""Category""><Property name=""GcRealitySubstanceCategory"" value=""None"" /></Property>
      <Property name=""Type""><Property name=""GcProductCategory"" value=""None"" /></Property>
      <Property name=""Rarity""><Property name=""GcRarity"" value=""Common"" /></Property>
      <Property name=""Legality""><Property name=""GcLegality"" value=""Legal"" /></Property>
      <Property name=""ChargeValue"" value=""0"" />
      <Property name=""StackMultiplier"" value=""1"" />
      <Property name=""DefaultCraftAmount"" value=""1"" />
      <Property name=""Requirements"" />
    </Property>
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""EXPD_POSTER11A"" />
      <Property name=""Name"" value=""UI_POSTER11A_NAME"" />
      <Property name=""NameLower"" value=""UI_POSTER11A_NAME_L"" />
      <Property name=""Subtitle"" value="""" />
      <Property name=""Description"" value="""" />
      <Property name=""Hint"" value="""" />
      <Property name=""BaseValue"" value=""50"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/POSTER.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
      <Property name=""Category""><Property name=""GcRealitySubstanceCategory"" value=""None"" /></Property>
      <Property name=""Type""><Property name=""GcProductCategory"" value=""None"" /></Property>
      <Property name=""Rarity""><Property name=""GcRarity"" value=""Common"" /></Property>
      <Property name=""Legality""><Property name=""GcLegality"" value=""Legal"" /></Property>
      <Property name=""ChargeValue"" value=""0"" />
      <Property name=""StackMultiplier"" value=""1"" />
      <Property name=""DefaultCraftAmount"" value=""1"" />
      <Property name=""Requirements"" />
    </Property>
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""TGA_SHIP01"" />
      <Property name=""Name"" value=""UI_TGA_SHIP_NAME"" />
      <Property name=""NameLower"" value=""UI_TGA_SHIP_NAME_L"" />
      <Property name=""Subtitle"" value="""" />
      <Property name=""Description"" value="""" />
      <Property name=""Hint"" value="""" />
      <Property name=""BaseValue"" value=""200"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/TGA.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
      <Property name=""Category""><Property name=""GcRealitySubstanceCategory"" value=""None"" /></Property>
      <Property name=""Type""><Property name=""GcProductCategory"" value=""None"" /></Property>
      <Property name=""Rarity""><Property name=""GcRarity"" value=""Common"" /></Property>
      <Property name=""Legality""><Property name=""GcLegality"" value=""Legal"" /></Property>
      <Property name=""ChargeValue"" value=""0"" />
      <Property name=""StackMultiplier"" value=""1"" />
      <Property name=""DefaultCraftAmount"" value=""1"" />
      <Property name=""Requirements"" />
    </Property>
  </Property>
</Data>";
            File.WriteAllText(Path.Combine(tmpDir, "nms_reality_gcproducttable.MXML"), productsXml);

            // Season rewards MXML
            string seasonXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcUnlockableSeasonRewards"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcUnlockableSeasonReward"" _id=""VAULT_ARMOUR"">
      <Property name=""ID"" value=""VAULT_ARMOUR"" />
      <Property name=""MustBeUnlocked"" value=""false"" />
      <Property name=""SeasonIds"">
        <Property name=""SeasonIds"" value=""21"" _index=""0"" />
      </Property>
      <Property name=""StageIds"">
        <Property name=""StageIds"" value=""-1"" _index=""0"" />
      </Property>
    </Property>
  </Property>
</Data>";
            File.WriteAllText(Path.Combine(tmpDir, "UNLOCKABLESEASONREWARDS.MXML"), seasonXml);

            // Twitch rewards MXML
            string twitchXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcUnlockableTwitchRewards"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcUnlockableTwitchReward"" _index=""0"">
      <Property name=""TwitchId"" value=""TWITCH_376"" />
      <Property name=""ProductId"" value=""EXPD_POSTER11A"" />
      <Property name=""LinkedGroupId"" value="""" />
    </Property>
  </Property>
</Data>";
            File.WriteAllText(Path.Combine(tmpDir, "UNLOCKABLETWITCHREWARDS.MXML"), twitchXml);

            // Platform rewards MXML
            string platformXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcUnlockablePlatformRewards"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcUnlockablePlatformReward"" _index=""0"">
      <Property name=""RewardId"" value=""TGA_SHIP1"" />
      <Property name=""ProductId"" value=""TGA_SHIP01"" />
    </Property>
  </Property>
</Data>";
            File.WriteAllText(Path.Combine(tmpDir, "UNLOCKABLEPLATFORMREWARDS.MXML"), platformXml);

            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var rewards = Parsers.ParseRewards(
                Path.Combine(tmpDir, "UNLOCKABLESEASONREWARDS.MXML"));

            // Should have 3 rewards total (1 season + 1 twitch + 1 platform)
            Assert.Equal(3, rewards.Count);

            // Season reward
            var season = rewards.First(r => r["Category"]?.ToString() == "season");
            Assert.Equal("^VAULT_ARMOUR", season["Id"]);
            Assert.Equal("season", season["Category"]);
            Assert.Equal("VAULT_ARMOUR", season["ProductId"]);
            Assert.Equal(false, season["MustBeUnlocked"]);
            Assert.Equal(21, season["SeasonId"]);
            Assert.Equal(-1, season["StageId"]);

            // Twitch reward
            var twitch = rewards.First(r => r["Category"]?.ToString() == "twitch");
            Assert.Equal("^TWITCH_376", twitch["Id"]);
            Assert.Equal("twitch", twitch["Category"]);
            Assert.Equal("EXPD_POSTER11A", twitch["ProductId"]);

            // Platform reward
            var platform = rewards.First(r => r["Category"]?.ToString() == "platform");
            Assert.Equal("^TGA_SHIP1", platform["Id"]);
            Assert.Equal("platform", platform["Category"]);
            Assert.Equal("TGA_SHIP01", platform["ProductId"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseRewards_MissingFilesStillReturnsAvailableRewards()
    {
        string tmpDir = CreateTempDir();
        try
        {
            // Only create the products table and season rewards, skip twitch/platform
            string productsXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""GcProductTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcProductData.xml"">
      <Property name=""ID"" value=""VAULT_BOOTS"" />
      <Property name=""Name"" value="""" />
      <Property name=""NameLower"" value="""" />
      <Property name=""Subtitle"" value="""" />
      <Property name=""Description"" value="""" />
      <Property name=""Hint"" value="""" />
      <Property name=""BaseValue"" value=""100"" />
      <Property name=""Icon""><Property name=""Filename"" value=""TEXTURES/BOOTS.DDS"" /></Property>
      <Property name=""Colour""><Property name=""R"" value=""1"" /><Property name=""G"" value=""1"" /><Property name=""B"" value=""1"" /><Property name=""A"" value=""1"" /></Property>
      <Property name=""Category""><Property name=""GcRealitySubstanceCategory"" value=""None"" /></Property>
      <Property name=""Type""><Property name=""GcProductCategory"" value=""None"" /></Property>
      <Property name=""Rarity""><Property name=""GcRarity"" value=""Common"" /></Property>
      <Property name=""Legality""><Property name=""GcLegality"" value=""Legal"" /></Property>
      <Property name=""ChargeValue"" value=""0"" />
      <Property name=""StackMultiplier"" value=""1"" />
      <Property name=""DefaultCraftAmount"" value=""1"" />
      <Property name=""Requirements"" />
    </Property>
  </Property>
</Data>";
            File.WriteAllText(Path.Combine(tmpDir, "nms_reality_gcproducttable.MXML"), productsXml);

            string seasonXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcUnlockableSeasonRewards"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcUnlockableSeasonReward"" _id=""VAULT_BOOTS"">
      <Property name=""ID"" value=""VAULT_BOOTS"" />
    </Property>
  </Property>
</Data>";
            File.WriteAllText(Path.Combine(tmpDir, "UNLOCKABLESEASONREWARDS.MXML"), seasonXml);

            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var rewards = Parsers.ParseRewards(
                Path.Combine(tmpDir, "UNLOCKABLESEASONREWARDS.MXML"));

            // Only season reward should be present; twitch/platform missing files are skipped
            Assert.Single(rewards);
            Assert.Equal("^VAULT_BOOTS", rewards[0]["Id"]);
            Assert.Equal("season", rewards[0]["Category"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseRewards_RefMxml_ParsesRealGameData()
    {
        // Uses the reference MXML files from _ref/game_mbin_mxml to verify
        // parsing against actual game data structures.
        string repoRoot = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        string refDir = Path.Combine(repoRoot, "_ref", "game_mbin_mxml", "METADATA", "REALITY", "TABLES");

        string seasonPath = Path.Combine(refDir, "UNLOCKABLESEASONREWARDS.MXML");
        if (!File.Exists(seasonPath))
        {
            // Skip if ref files aren't available (e.g. CI without submodules)
            return;
        }

        MxmlParser.ClearXmlCache();
        MxmlParser.ClearLocalisationCache();

        var rewards = Parsers.ParseRewards(seasonPath);

        // Should have rewards from all three categories
        int seasonCount = rewards.Count(r => r["Category"]?.ToString() == "season");
        int twitchCount = rewards.Count(r => r["Category"]?.ToString() == "twitch");
        int platformCount = rewards.Count(r => r["Category"]?.ToString() == "platform");

        Assert.True(seasonCount > 0, $"Expected season rewards, got {seasonCount}");
        Assert.True(twitchCount > 0, $"Expected twitch rewards, got {twitchCount}");
        Assert.True(platformCount > 0, $"Expected platform rewards, got {platformCount}");

        // Verify well-known entries from game data exist (stable IDs present since early game versions)
        Assert.Contains(rewards, r => r["Id"]?.ToString() == "^VAULT_ARMOUR");
        Assert.Contains(rewards, r => r["Id"]?.ToString() == "^TGA_SHIP1");

        // Every reward should have an Id starting with ^
        Assert.All(rewards, r =>
        {
            string? id = r["Id"]?.ToString();
            Assert.NotNull(id);
            Assert.StartsWith("^", id);
        });
    }

    [Fact]
    public void ParseWords_ReturnsWordsFromMxml()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcAlienSpeechTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcAlienSpeechEntry"" _id=""ATLAS"">
      <Property name=""Id"" value=""ATLAS"" />
      <Property name=""Text"" value=""ATLAS_ATLAS"" />
      <Property name=""Group"" value=""ATLAS_ATLAS"" />
      <Property name=""Category"" value=""GcWordCategoryTableEnum"">
        <Property name=""wordcategorytableEnum"" value=""MISC"" />
      </Property>
      <Property name=""Race"" value=""GcAlienRace"">
        <Property name=""AlienRace"" value=""Atlas"" />
      </Property>
    </Property>
    <Property name=""Table"" value=""GcAlienSpeechEntry"" _id=""ATLAS"">
      <Property name=""Id"" value=""ATLAS"" />
      <Property name=""Text"" value=""EXP_ATLAS"" />
      <Property name=""Group"" value=""EXP_ATLAS"" />
      <Property name=""Category"" value=""GcWordCategoryTableEnum"">
        <Property name=""wordcategorytableEnum"" value=""MISC"" />
      </Property>
      <Property name=""Race"" value=""GcAlienRace"">
        <Property name=""AlienRace"" value=""Explorers"" />
      </Property>
    </Property>
    <Property name=""Table"" value=""GcAlienSpeechEntry"" _id=""ATLAS"">
      <Property name=""Id"" value=""ATLAS"" />
      <Property name=""Text"" value=""TRA_ATLAS"" />
      <Property name=""Group"" value=""TRA_ATLAS"" />
      <Property name=""Category"" value=""GcWordCategoryTableEnum"">
        <Property name=""wordcategorytableEnum"" value=""MISC"" />
      </Property>
      <Property name=""Race"" value=""GcAlienRace"">
        <Property name=""AlienRace"" value=""Traders"" />
      </Property>
    </Property>
    <Property name=""Table"" value=""GcAlienSpeechEntry"" _id=""THE"">
      <Property name=""Id"" value=""THE"" />
      <Property name=""Text"" value=""ATLAS_THE"" />
      <Property name=""Group"" value=""ATLAS_THE"" />
      <Property name=""Category"" value=""GcWordCategoryTableEnum"">
        <Property name=""wordcategorytableEnum"" value=""MISC"" />
      </Property>
      <Property name=""Race"" value=""GcAlienRace"">
        <Property name=""AlienRace"" value=""Atlas"" />
      </Property>
    </Property>
    <Property name=""Table"" value=""GcAlienSpeechEntry"" _id=""NONE_WORD"">
      <Property name=""Id"" value=""NONE_WORD"" />
      <Property name=""Text"" value=""NONE_TEXT"" />
      <Property name=""Group"" value=""NONE_GROUP"" />
      <Property name=""Category"" value=""GcWordCategoryTableEnum"">
        <Property name=""wordcategorytableEnum"" value=""MISC"" />
      </Property>
      <Property name=""Race"" value=""GcAlienRace"">
        <Property name=""AlienRace"" value=""None"" />
      </Property>
    </Property>
  </Property>
</Data>";
            string mxmlPath = Path.Combine(tmpDir, "nms_dialog_gcalienspeechtable.MXML");
            File.WriteAllText(mxmlPath, xml);

            Parsers.ResetCaches();
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var words = Parsers.ParseWords(mxmlPath);

            // Should produce 2 unique words (ATLAS and THE); NONE_WORD has race=None so is skipped
            Assert.Equal(2, words.Count);

            // ATLAS word
            var atlas = words.First(w => w["Id"]?.ToString() == "^ATLAS");
            Assert.Equal("atlas", atlas["Text"]?.ToString());
            Assert.Equal("TRA_ATLAS", atlas["Text_LocStr"]?.ToString());
            var groups = (Dictionary<string, object?>)atlas["Groups"]!;
            Assert.Equal(0, groups["^TRA_ATLAS"]);
            Assert.Equal(2, groups["^EXP_ATLAS"]);
            Assert.Equal(4, groups["^ATLAS_ATLAS"]);

            // THE word
            var the = words.First(w => w["Id"]?.ToString() == "^THE");
            Assert.Equal("the", the["Text"]?.ToString());
            Assert.Equal("ATLAS_THE", the["Text_LocStr"]?.ToString());
            var theGroups = (Dictionary<string, object?>)the["Groups"]!;
            Assert.Equal(4, theGroups["^ATLAS_THE"]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ParseWords_EmptyTable_ReturnsEmptyList()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcAlienSpeechTable"">
  <Property name=""Table"">
  </Property>
</Data>";
            string mxmlPath = Path.Combine(tmpDir, "nms_dialog_gcalienspeechtable.MXML");
            File.WriteAllText(mxmlPath, xml);

            Parsers.ResetCaches();
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var words = Parsers.ParseWords(mxmlPath);
            Assert.Empty(words);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ParseWords_NoTableProperty_ReturnsEmptyList()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcAlienSpeechTable"">
</Data>";
            string mxmlPath = Path.Combine(tmpDir, "nms_dialog_gcalienspeechtable.MXML");
            File.WriteAllText(mxmlPath, xml);

            Parsers.ResetCaches();
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var words = Parsers.ParseWords(mxmlPath);
            Assert.Empty(words);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ParseFrigateTraits_ReturnsParsedTraits()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcFrigateTraitTable"">
  <Property name=""Traits"">
    <Property name=""Traits"" value=""GcFrigateTraitData"">
      <Property name=""ID"" value=""FUEL_PRI"" />
      <Property name=""DisplayName"" value=""FLEET_TRAIT_PRI_FUEL_1"" />
      <Property name=""FrigateStatType"" value=""GcFrigateStatType"">
        <Property name=""FrigateStatType"" value=""FuelCapacity"" />
      </Property>
      <Property name=""Strength"" value=""GcFrigateTraitStrength"">
        <Property name=""FrigateTraitStrength"" value=""Primary"" />
      </Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "FRIGATETRAITTABLE.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var traits = Parsers.ParseFrigateTraits(file);

            Assert.NotEmpty(traits);
            Assert.Equal("^FUEL_PRI", traits[0]["Id"]);
            Assert.Equal("FuelCapacity", traits[0]["Type"]);
            Assert.Equal(-15, traits[0]["Strength"]);
            Assert.True((bool)traits[0]["Beneficial"]!);
            Assert.NotNull(traits[0]["Name_LocStr"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseSettlementPerks_ReturnsParsedPerks()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcSettlementPerksTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcSettlementPerkData"">
      <Property name=""ID"" value=""STARTING_NEG1"" />
      <Property name=""Name"" value=""UI_PERK_NEGATIVE_TITLE_1"" />
      <Property name=""Description"" value=""UI_PERK_NEGATIVE_DESC_COST"" />
      <Property name=""IsNegative"" value=""True"" />
      <Property name=""IsStarter"" value=""True"" />
      <Property name=""IsProc"" value=""False"" />
      <Property name=""StatChanges"">
        <Property value=""GcSettlementStatChange"">
          <Property name=""Stat"" value=""GcSettlementStatType"">
            <Property name=""SettlementStatType"" value=""Upkeep"" />
          </Property>
          <Property name=""Strength"" value=""GcSettlementStatStrength"">
            <Property name=""SettlementStatStrength"" value=""NegativeMedium"" />
          </Property>
        </Property>
      </Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "SETTLEMENTPERKSTABLE.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var perks = Parsers.ParseSettlementPerks(file);

            Assert.NotEmpty(perks);
            Assert.Equal("^STARTING_NEG1", perks[0]["Id"]);
            Assert.Equal(false, perks[0]["Beneficial"]); // IsNegative=True -> Beneficial=false
            Assert.Equal(true, perks[0]["Starter"]);
            Assert.NotNull(perks[0]["Name_LocStr"]);
            Assert.NotNull(perks[0]["Description_LocStr"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseWikiGuide_ReturnsParsedTopics()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcWiki"">
  <Property name=""Categories"">
    <Property name=""Categories"" value=""GcWikiCategory"">
      <Property name=""CategoryID"" value=""UI_GUIDE_HEADING_SURVIVAL"" />
      <Property name=""Topics"">
        <Property name=""Topics"" value=""GcWikiTopic"">
          <Property name=""TopicID"" value=""UI_GUIDE_TOPIC_SURVIVAL_BASICS"" />
        </Property>
      </Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "WIKI.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var topics = Parsers.ParseWikiGuide(file);

            Assert.NotEmpty(topics);
            Assert.Equal("^UI_GUIDE_TOPIC_SURVIVAL_BASICS", topics[0]["Id"]);
            Assert.NotNull(topics[0]["Category_LocStr"]);
            Assert.NotNull(topics[0]["Name_LocStr"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseAllRecipes_IncludesLocStrFields()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcRecipeTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcRefinerRecipe"">
      <Property name=""Id"" value=""RECIPE_1"" />
      <Property name=""RecipeType"" value=""UI_YEAST_PROCESS"" />
      <Property name=""RecipeName"" value=""UI_YEAST_PROCESS_R"" />
      <Property name=""TimeToMake"" value=""30"" />
      <Property name=""Cooking"" value=""True"" />
      <Property name=""Result"" value=""GcRefinerRecipeElement"">
        <Property name=""Id"" value=""FOOD_P_POOP"" />
        <Property name=""Type"" value=""GcInventoryType"">
          <Property name=""InventoryType"" value=""Product"" />
        </Property>
        <Property name=""Amount"" value=""1"" />
      </Property>
      <Property name=""Ingredients"">
        <Property value=""GcRefinerRecipeElement"">
          <Property name=""Id"" value=""FOOD_P_POOP"" />
          <Property name=""Type"" value=""GcInventoryType"">
            <Property name=""InventoryType"" value=""Product"" />
          </Property>
          <Property name=""Amount"" value=""1"" />
        </Property>
      </Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "NMS_REALITY_GCRECIPETABLE.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();
            MxmlParser.ClearLocalisationCache();

            var recipes = Parsers.ParseAllRecipes(file);

            Assert.NotEmpty(recipes);
            Assert.Equal("RECIPE_1", recipes[0]["Id"]);
            // Verify _LocStr fields are present
            Assert.Equal("UI_YEAST_PROCESS_R", recipes[0]["RecipeName_LocStr"]);
            Assert.Equal("UI_YEAST_PROCESS", recipes[0]["RecipeType_LocStr"]);
            Assert.Equal(true, recipes[0]["Cooking"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void ParseCreatureSpecies_ExtractsAccessoryGroups()
    {
        string tmpDir = CreateTempDir();
        try
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Data template=""cGcCreatureDataTable"">
  <Property name=""Table"">
    <Property name=""Table"" value=""GcCreatureData"" _id=""TESTCREATURE"">
      <Property name=""Id"" value=""TESTCREATURE"" />
      <Property name=""OnlySpawnWhenIdIsForced"" value=""false"" />
      <Property name=""ForceType"" value=""GcCreatureTypes""><Property name=""CreatureType"" value=""None"" /></Property>
      <Property name=""RealType"" value=""GcCreatureTypes""><Property name=""CreatureType"" value=""Tyrannosaurus"" /></Property>
      <Property name=""EcoSystemCreature"" value=""true"" />
      <Property name=""CanBeFemale"" value=""true"" />
      <Property name=""Tags"" />
      <Property name=""MoveArea"" value=""Ground"" />
      <Property name=""MinScale"" value=""0.5"" />
      <Property name=""MaxScale"" value=""5.0"" />
      <Property name=""FurChance"" value=""0.0"" />
      <Property name=""Rarity"" value=""GcCreatureRarity""><Property name=""CreatureRarity"" value=""Common"" /></Property>
      <Property name=""PredatorProbabilityModifier"" value=""GcCreatureRoleFrequencyModifier""><Property name=""CreatureRoleFrequencyModifier"" value=""Normal"" /></Property>
      <Property name=""HerbivoreProbabilityModifier"" value=""GcCreatureRoleFrequencyModifier""><Property name=""CreatureRoleFrequencyModifier"" value=""Normal"" /></Property>
      <Property name=""KillStatID"" value="""" />
      <Property name=""KillingBlowMessageID"" value="""" />
      <Property name=""EggType"" value=""DEFAULT"" />
      <Property name=""Data"">
        <Property name=""Data"" value=""GcCreatureMovementData"" _index=""0"">
          <Property name=""GcCreatureMovementData""><Property name=""Anims"" /></Property>
        </Property>
        <Property name=""Data"" value=""GcCreaturePetData"" _index=""1"">
          <Property name=""GcCreaturePetData"">
            <Property name=""AccessorySlots"">
              <Property name=""AccessorySlots"" value=""GcCreaturePetAccessory"" _index=""0"">
                <Property name=""RequiredDescriptor"" value=""_BODY_TEST"" />
                <Property name=""Slots"">
                  <Property name=""Slots"" value=""GcCreaturePetAccessorySlot"" _index=""0"">
                    <Property name=""AttachLocator"" value=""PETACC_TestR"" />
                    <Property name=""AccessoryGroup"" value=""RIGHT"" />
                  </Property>
                  <Property name=""Slots"" value=""GcCreaturePetAccessorySlot"" _index=""1"">
                    <Property name=""AttachLocator"" value=""PETACC_TestL"" />
                    <Property name=""AccessoryGroup"" value=""LEFT"" />
                  </Property>
                  <Property name=""Slots"" value=""GcCreaturePetAccessorySlot"" _index=""2"">
                    <Property name=""AttachLocator"" value=""PETACC_TestF"" />
                    <Property name=""AccessoryGroup"" value=""FRONT"" />
                  </Property>
                </Property>
                <Property name=""HideParts"" />
              </Property>
              <Property name=""AccessorySlots"" value=""GcCreaturePetAccessory"" _index=""1"">
                <Property name=""RequiredDescriptor"" value=""_BODY_ALT"" />
                <Property name=""Slots"">
                  <Property name=""Slots"" value=""GcCreaturePetAccessorySlot"" _index=""0"">
                    <Property name=""AttachLocator"" value=""PETACC_AltB"" />
                    <Property name=""AccessoryGroup"" value=""BACK"" />
                  </Property>
                </Property>
                <Property name=""HideParts"" />
              </Property>
            </Property>
          </Property>
        </Property>
      </Property>
      <Property name=""CanBeUsedInPetBattler"" value=""false"" />
      <Property name=""PetBattlerForcedAffinity"" value=""GcPetBattlerAffinity""><Property name=""PetBattlerAffinity"" value=""Normal"" /></Property>
    </Property>
    <Property name=""Table"" value=""GcCreatureData"" _id=""NOSLOTS"">
      <Property name=""Id"" value=""NOSLOTS"" />
      <Property name=""OnlySpawnWhenIdIsForced"" value=""true"" />
      <Property name=""ForceType"" value=""GcCreatureTypes""><Property name=""CreatureType"" value=""None"" /></Property>
      <Property name=""RealType"" value=""GcCreatureTypes""><Property name=""CreatureType"" value=""None"" /></Property>
      <Property name=""EcoSystemCreature"" value=""false"" />
      <Property name=""CanBeFemale"" value=""false"" />
      <Property name=""Tags"" />
      <Property name=""MoveArea"" value=""Ground"" />
      <Property name=""MinScale"" value=""1.0"" />
      <Property name=""MaxScale"" value=""1.0"" />
      <Property name=""FurChance"" value=""0.0"" />
      <Property name=""Rarity"" value=""GcCreatureRarity""><Property name=""CreatureRarity"" value=""Rare"" /></Property>
      <Property name=""PredatorProbabilityModifier"" value=""GcCreatureRoleFrequencyModifier""><Property name=""CreatureRoleFrequencyModifier"" value=""Normal"" /></Property>
      <Property name=""HerbivoreProbabilityModifier"" value=""GcCreatureRoleFrequencyModifier""><Property name=""CreatureRoleFrequencyModifier"" value=""Normal"" /></Property>
      <Property name=""KillStatID"" value="""" />
      <Property name=""KillingBlowMessageID"" value="""" />
      <Property name=""EggType"" value=""DEFAULT"" />
      <Property name=""Data"">
        <Property name=""Data"" value=""GcCreaturePetData"" _index=""0"">
          <Property name=""GcCreaturePetData"">
            <Property name=""AccessorySlots"" />
          </Property>
        </Property>
      </Property>
      <Property name=""CanBeUsedInPetBattler"" value=""false"" />
      <Property name=""PetBattlerForcedAffinity"" value=""GcPetBattlerAffinity""><Property name=""PetBattlerAffinity"" value=""Normal"" /></Property>
    </Property>
  </Property>
</Data>";

            string file = Path.Combine(tmpDir, "METADATA_CREATUREDATATABLE.MXML");
            File.WriteAllText(file, xml);
            MxmlParser.ClearXmlCache();

            var results = Parsers.ParseCreatureSpecies(file);

            Assert.Equal(2, results.Count);

            // TESTCREATURE
            var tc = results.First(r => (string?)r["Id"] == "TESTCREATURE");
            Assert.Equal("None", tc["ForceType"]);
            Assert.Equal("Tyrannosaurus", tc["RealType"]);
            Assert.Equal(true, tc["EcoSystemCreature"]);
            Assert.Equal(true, tc["CanBeFemale"]);
            Assert.Equal(false, tc["OnlySpawnWhenIdIsForced"]);

            var accSlots = tc["PetAccessorySlots"] as List<Dictionary<string, object?>>;
            Assert.NotNull(accSlots);
            Assert.Equal(2, accSlots.Count);

            Assert.Equal("_BODY_TEST", accSlots[0]["RequiredDescriptor"]);
            var groups0 = accSlots[0]["AccessoryGroups"] as List<string>;
            Assert.NotNull(groups0);
            Assert.Equal(new[] { "RIGHT", "LEFT", "FRONT" }, groups0);

            Assert.Equal("_BODY_ALT", accSlots[1]["RequiredDescriptor"]);
            var groups1 = accSlots[1]["AccessoryGroups"] as List<string>;
            Assert.NotNull(groups1);
            Assert.Equal(new[] { "BACK" }, groups1);

            // NOSLOTS -- empty AccessorySlots yields null
            var ns = results.First(r => (string?)r["Id"] == "NOSLOTS");
            Assert.Equal("None", ns["ForceType"]);
            Assert.Equal("None", ns["RealType"]);
            Assert.Equal(true, ns["OnlySpawnWhenIdIsForced"]);
            Assert.Null(ns["PetAccessorySlots"]);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }
}
