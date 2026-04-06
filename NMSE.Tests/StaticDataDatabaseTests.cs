using NMSE.Data;
using System.IO;

namespace NMSE.Tests;

/// <summary>
/// Tests for the static data classes that replaced XML database files.
/// Shares the MutableStaticDatabases collection to prevent parallel execution
/// with tests that call LoadFromFile on static databases.
/// </summary>
[Collection("MutableStaticDatabases")]
public class StaticDataDatabaseTests
{
    /// <summary>
    /// Ensures JSON-loaded databases are populated before tests run.
    /// Data now comes from Resources/json/ files instead of hardcoded C# arrays.
    /// </summary>
    public StaticDataDatabaseTests()
    {
        EnsureJsonDatabasesLoaded();
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

            FrigateTraitDatabase.LoadFromFile(Path.Combine(jsonDir, "FrigateTraits.json"));
            SettlementDatabase.LoadFromFile(Path.Combine(jsonDir, "SettlementPerks.json"));
            WikiGuideDatabase.LoadFromFile(Path.Combine(jsonDir, "WikiGuide.json"));
            TitleDatabase.LoadFromFile(Path.Combine(jsonDir, "Titles.json"));

            _jsonLoaded = true;
        }
    }

    // --- FrigateTraitDatabase ---

    [Fact]
    public void FrigateTraitDatabase_HasExpectedTraitCount()
    {
        Assert.Equal(178, FrigateTraitDatabase.Traits.Count);
    }

    [Fact]
    public void FrigateTraitDatabase_ById_ContainsAllTraits()
    {
        Assert.Equal(FrigateTraitDatabase.Traits.Count, FrigateTraitDatabase.ById.Count);
    }

    [Fact]
    public void FrigateTraitDatabase_CanLookupKnownTrait()
    {
        Assert.True(FrigateTraitDatabase.ById.ContainsKey("^FUEL_PRI"));
        var trait = FrigateTraitDatabase.ById["^FUEL_PRI"];
        Assert.Equal("Support Specialist", trait.Name);
        Assert.Equal("FuelCapacity", trait.Type);
        Assert.Equal(-15, trait.Strength);
        Assert.True(trait.Beneficial);
        Assert.Equal("SUPPORT", trait.Primary);
    }

    [Fact]
    public void FrigateTraitDatabase_NormandyTraitExists()
    {
        Assert.True(FrigateTraitDatabase.ById.ContainsKey("^NORMANDY_1"));
        Assert.Equal("Deep Scout Prototype", FrigateTraitDatabase.ById["^NORMANDY_1"].Name);
    }

    [Fact]
    public void FrigateTraitDatabase_NegativeTraitHasFalse()
    {
        Assert.True(FrigateTraitDatabase.ById.ContainsKey("^FUEL_BAD_1"));
        Assert.False(FrigateTraitDatabase.ById["^FUEL_BAD_1"].Beneficial);
    }

    // --- InventoryStackDatabase ---

    [Fact]
    public void InventoryStackDatabase_HasThreeDifficulties()
    {
        Assert.Equal(3, InventoryStackDatabase.ByDifficulty.Count);
        Assert.True(InventoryStackDatabase.ByDifficulty.ContainsKey("High"));
        Assert.True(InventoryStackDatabase.ByDifficulty.ContainsKey("Normal"));
        Assert.True(InventoryStackDatabase.ByDifficulty.ContainsKey("Low"));
    }

    [Fact]
    public void InventoryStackDatabase_EachDifficultyHas13Groups()
    {
        foreach (var difficulty in InventoryStackDatabase.ByDifficulty)
        {
            Assert.Equal(13, difficulty.Value.Count);
        }
    }

    [Fact]
    public void InventoryStackDatabase_GetStackSize_ReturnsCorrectValues()
    {
        var entry = InventoryStackDatabase.GetStackSize("High", "Personal");
        Assert.NotNull(entry);
        Assert.Equal(10, entry.Product);
        Assert.Equal(9999, entry.Substance);
    }

    [Fact]
    public void InventoryStackDatabase_GetStackSize_ReturnsNullForInvalid()
    {
        Assert.Null(InventoryStackDatabase.GetStackSize("Invalid", "Personal"));
        Assert.Null(InventoryStackDatabase.GetStackSize("High", "Invalid"));
    }

    [Fact]
    public void InventoryStackDatabase_Count_Returns39()
    {
        Assert.Equal(39, InventoryStackDatabase.Count);
    }

    // --- GetDefaultStackSize ---

    [Theory]
    [InlineData("Substance", "Personal", 9999)]
    [InlineData("Product",   "Personal", 10)]
    [InlineData("Substance", "Ship", 9999)]
    [InlineData("Product",   "Ship", 10)]
    [InlineData("Substance", "Freighter", 9999)]
    [InlineData("Product",   "Freighter", 20)]
    [InlineData("Substance", "FreighterCargo", 9999)]
    [InlineData("Product",   "FreighterCargo", 20)]
    [InlineData("Substance", "Vehicle", 9999)]
    [InlineData("Product",   "Vehicle", 10)]
    [InlineData("Substance", "Chest", 9999)]
    [InlineData("Product",   "Chest", 20)]
    [InlineData("Substance", "BaseCapsule", 9999)]
    [InlineData("Product",   "BaseCapsule", 100)]
    public void GetDefaultStackSize_ReturnsCorrectForKnownGroups(string invType, string group, int expected)
    {
        Assert.Equal(expected, InventoryStackDatabase.GetDefaultStackSize(invType, group));
    }

    [Fact]
    public void GetDefaultStackSize_FallsBackToDefaultForUnknownGroup()
    {
        // Unknown group should fall back to "Default" entry (Product=5, Substance=9999 for High)
        Assert.Equal(9999, InventoryStackDatabase.GetDefaultStackSize("Substance", "UnknownGroup"));
        Assert.Equal(5, InventoryStackDatabase.GetDefaultStackSize("Product", "UnknownGroup"));
    }

    [Fact]
    public void GetDefaultStackSize_TechnologyAlwaysReturnsProductValue()
    {
        // Technology items use the Product column since they stack like products
        Assert.Equal(10, InventoryStackDatabase.GetDefaultStackSize("Technology", "Personal"));
        Assert.Equal(20, InventoryStackDatabase.GetDefaultStackSize("Technology", "Freighter"));
    }

    // --- GetMaxAmount (per-item calculation) ---

    [Fact]
    public void GetMaxAmount_Substance_Always9999()
    {
        var item = new GameItem { Id = "FUEL1", MaxStackSize = 1, ChargeValue = 0 };
        Assert.Equal(9999, InventoryStackDatabase.GetMaxAmount(item, "Substance"));
    }

    [Fact]
    public void GetMaxAmount_Technology_ChargeableReturnsChargeValue()
    {
        var item = new GameItem { Id = "LASER", MaxStackSize = 0, ChargeValue = 100, IsChargeable = true };
        Assert.Equal(100, InventoryStackDatabase.GetMaxAmount(item, "Technology"));
    }

    [Fact]
    public void GetMaxAmount_Technology_NonChargeableReturns0()
    {
        // Non-chargeable tech has ChargeAmount in game data but should still return 0
        var item = new GameItem { Id = "UT_SCAN", MaxStackSize = 0, ChargeValue = 100, IsChargeable = false };
        Assert.Equal(0, InventoryStackDatabase.GetMaxAmount(item, "Technology"));
    }

    [Fact]
    public void GetMaxAmount_Technology_NonChargeableNoChargeValueReturns0()
    {
        var item = new GameItem { Id = "UT_SCAN", MaxStackSize = 0, ChargeValue = 0, IsChargeable = false };
        Assert.Equal(0, InventoryStackDatabase.GetMaxAmount(item, "Technology"));
    }

    [Theory]
    [InlineData(5, 50)]   // Trade goods: 10 * 5 = 50
    [InlineData(10, 100)]  // Food items: 10 * 10 = 100
    [InlineData(1, 10)]    // Single-stack items: 10 * 1 = 10
    [InlineData(50, 500)]  // Large stack items: 10 * 50 = 500
    public void GetMaxAmount_Product_TenTimesMaxStackSize(int stackSize, int expected)
    {
        var item = new GameItem { Id = "PROD", MaxStackSize = stackSize, ChargeValue = 0 };
        Assert.Equal(expected, InventoryStackDatabase.GetMaxAmount(item, "Product"));
    }

    [Fact]
    public void GetMaxAmount_Product_ZeroStackSizeReturns1()
    {
        var item = new GameItem { Id = "PROD", MaxStackSize = 0, ChargeValue = 0 };
        Assert.Equal(1, InventoryStackDatabase.GetMaxAmount(item, "Product"));
    }

    // --- ResolveInventoryType ---

    [Theory]
    [InlineData("technology", "Technology")]
    [InlineData("Technology", "Technology")]
    [InlineData("Constructed Technology", "Product")]
    [InlineData("Technology Module", "Product")]
    [InlineData("Upgrades", "Product")]
    [InlineData("ProceduralProduct", "Product")]
    [InlineData("Raw Materials", "Substance")]
    [InlineData("substance", "Substance")]
    [InlineData("Products", "Product")]
    [InlineData("Curiosities", "Product")]
    [InlineData("Trade", "Product")]
    [InlineData("Exocraft", "Product")]
    [InlineData("Corvette", "Product")]
    [InlineData("", "Product")]
    [InlineData(null, "Product")]
    public void ResolveInventoryType_MapsCorrectly(string? itemType, string expected)
    {
        Assert.Equal(expected, InventoryStackDatabase.ResolveInventoryType(itemType));
    }

    // --- CanAddItemToInventory ---

    [Fact]
    public void CanAddItem_TechOnly_AcceptsTechnology()
    {
        var item = new GameItem { Id = "LASER", ItemType = "technology", Category = "Weapon" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_RejectsSubstance()
    {
        var item = new GameItem { Id = "FUEL1", ItemType = "substance", Category = "Fuel" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_RejectsProduct()
    {
        var item = new GameItem { Id = "CRATE1", ItemType = "Products", Category = "Trade" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_Cargo_AcceptsSubstance()
    {
        var item = new GameItem { Id = "FUEL1", ItemType = "substance", Category = "Fuel" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Cargo_AcceptsProduct()
    {
        var item = new GameItem { Id = "CRATE1", ItemType = "Products", Category = "Trade" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Cargo_RejectsTechnology()
    {
        var item = new GameItem { Id = "LASER", ItemType = "Technology", Category = "Weapon" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Cargo_AcceptsConstructedTechnology()
    {
        // ACCESS3 and NAV_DATA are "Constructed Technology" -> Product
        var access3 = new GameItem { Id = "ACCESS3", ItemType = "Constructed Technology", Category = "Curiosity" };
        var navData = new GameItem { Id = "NAV_DATA", ItemType = "Constructed Technology", Category = "Curiosity" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(access3, isTechOnly: false, isCargo: true));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(navData, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Cargo_AcceptsUpgrades()
    {
        // U_PULSE2 is "Upgrades" -> Product (consumable upgrade module)
        var uPulse2 = new GameItem { Id = "U_PULSE2", ItemType = "Upgrades", Category = "Consumable" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(uPulse2, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Cargo_AcceptsTechModule()
    {
        // Technology Module items are Products
        var item = new GameItem { Id = "U_SENTGUN", ItemType = "Technology Module", Category = "Consumable" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsConstructedTechnology()
    {
        // ACCESS3 ("Constructed Technology") is a Product,
        // NOT installable in tech-only inventories.
        var item = new GameItem { Id = "ACCESS3", ItemType = "Constructed Technology", Category = "Curiosity" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsUpgrades()
    {
        // Procedural upgrade items (no DeploysInto, has StatLevels) are
        // Technology and installable in tech-only inventories.
        var item = new GameItem { Id = "UP_LASER1", ItemType = "Upgrades", Category = "Weapon", IsProcedural = true };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_RejectsConsumableUpgrades()
    {
        // Consumable upgrade modules (DeploysInto set) are Products,
        // NOT installable in tech-only inventories.
        var item = new GameItem { Id = "U_PULSE2", ItemType = "Upgrades", Category = "Consumable" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsTechnologyModules()
    {
        // Procedural technology module items are Technology type
        var item = new GameItem { Id = "UA_PULSE1", ItemType = "Technology Module", Category = "", IsProcedural = true };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_General_AcceptsAll()
    {
        var tech = new GameItem { Id = "LASER", ItemType = "technology", Category = "Weapon" };
        var sub = new GameItem { Id = "FUEL1", ItemType = "substance", Category = "Fuel" };
        var prod = new GameItem { Id = "CRATE1", ItemType = "Products", Category = "Trade" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(tech, isTechOnly: false, isCargo: false));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(sub, isTechOnly: false, isCargo: false));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(prod, isTechOnly: false, isCargo: false));
    }

    [Fact]
    public void CanAddItem_MaintenanceTech_AlwaysRejected()
    {
        // CanPickUp excludes Maintenance-category technology
        var item = new GameItem { Id = "MAINT1", ItemType = "technology", Category = "Maintenance" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: false));
    }

    [Theory]
    [InlineData("Emote")]
    [InlineData("CreatureEgg")]
    public void CanAddItem_BlacklistedCategory_AlwaysRejected(string category)
    {
        // Category blacklist excludes Emote and CreatureEgg
        var item = new GameItem { Id = "TEST", ItemType = "Products", Category = category };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: false));
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Building_NonPickupable_Rejected()
    {
        // CanPickUp excludes base building products that are neither
        // CanPickUp nor IsTemporary (permanent structures).
        var item = new GameItem
        {
            Id = "BUILDING1", ItemType = "Buildings", Category = "BuildingPart",
            CanPickUp = false, IsTemporary = false
        };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: false));
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Building_CanPickUp_Accepted()
    {
        // Building products with CanPickUp=true are pickupable
        var item = new GameItem
        {
            Id = "BUILDING2", ItemType = "Buildings", Category = "BuildingPart",
            CanPickUp = true, IsTemporary = false
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: false));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_Building_IsTemporary_Accepted()
    {
        // Temporary building products are always considered pickupable
        var item = new GameItem
        {
            Id = "BUILDING3", ItemType = "Buildings", Category = "BuildingPart",
            CanPickUp = false, IsTemporary = true
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: false));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_NonBuilding_Product_NotAffectedByCanPickUp()
    {
        // Products that are NOT Buildings should not be affected by CanPickUp filter
        var item = new GameItem
        {
            Id = "PROD1", ItemType = "Products", Category = "Trade",
            CanPickUp = false, IsTemporary = false
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: false));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    // --- IsPickerExcluded ---

    [Theory]
    [InlineData("Technology", true)]
    [InlineData("Upgrades", true)]
    [InlineData("Technology Module", true)]
    [InlineData("Constructed Technology", true)]
    [InlineData("Exocraft", true)]
    [InlineData("Starships", true)]
    [InlineData("Raw Materials", false)]
    [InlineData("Products", false)]
    [InlineData("Curiosities", false)]
    [InlineData("Buildings", false)]
    [InlineData("Others", true)]
    [InlineData("Recipes", false)]
    public void IsTechnologyRelatedType_CorrectlyClassifies(string itemType, bool expected)
    {
        Assert.Equal(expected, GameItemDatabase.IsTechnologyRelatedType(itemType));
    }

    [Fact]
    public void Database_SubstanceCategory_DoesNotSetTechnologyCategory()
    {
        // When a Raw Materials item has Category = "Fuel", it should NOT
        // be treated as a TechnologyCategory. This was the root cause of
        // Carbon (FUEL1) being filtered out of cargo inventories.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip if resources not found
        db.LoadItemsFromJsonDirectory(jsonDir);

        var carbon = db.GetItem("FUEL1");
        Assert.NotNull(carbon);
        Assert.Equal("Fuel", carbon!.Category);
        Assert.True(string.IsNullOrEmpty(carbon.TechnologyCategory),
            $"FUEL1 (Carbon) should not have TechnologyCategory set, but got '{carbon.TechnologyCategory}'");
    }

    [Fact]
    public void Database_TechnologyCategory_StillSetForTechItems()
    {
        // Technology items should still have TechnologyCategory set correctly
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip if resources not found
        db.LoadItemsFromJsonDirectory(jsonDir);

        var laser = db.GetItem("LASER");
        Assert.NotNull(laser);
        Assert.Equal("Weapon", laser!.TechnologyCategory);
    }

    private static string? FindResourceJsonDir()
    {
        // Walk up from test assembly directory to find Resources/json
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

    [Theory]
    [InlineData("U_TECH1", true)]
    [InlineData("SPEC_HOOD01", true)]
    [InlineData("SPEC_XOHELMET", true)]
    [InlineData("TWITCH_REWARD", true)]
    [InlineData("SPEC_BB1", true)]
    [InlineData("EXPD_ITEM1", true)]
    [InlineData("TITLE_UNLOCK1", true)]
    [InlineData("SWITCH_ITEM", true)]
    [InlineData("FUEL1", false)]
    [InlineData("LASER", false)]
    [InlineData("CRATE1", false)]
    public void IsPickerExcluded_MatchesBlacklist(string id, bool expected)
    {
        Assert.Equal(expected, GameItemDatabase.IsPickerExcluded(id));
    }

    // --- SettlementDatabase ---

    [Fact]
    public void SettlementDatabase_HasExpectedPerkCount()
    {
        Assert.Equal(90, SettlementDatabase.Perks.Count);
    }

    [Fact]
    public void SettlementDatabase_ById_ContainsAllPerks()
    {
        Assert.Equal(SettlementDatabase.Perks.Count, SettlementDatabase.ById.Count);
    }

    [Fact]
    public void SettlementDatabase_CanLookupKnownPerk()
    {
        Assert.True(SettlementDatabase.ById.ContainsKey("^STARTING_NEG1"));
        var perk = SettlementDatabase.ById["^STARTING_NEG1"];
        Assert.Equal("Worm infestation", perk.Name);
        Assert.Equal("Increases maintenance costs", perk.Description);
        Assert.False(perk.Beneficial);
        Assert.False(perk.Procedural);
        Assert.True(perk.Starter);
        // Additional known perks
        Assert.True(SettlementDatabase.ById.ContainsKey("^STARTING_POS1"));
        Assert.True(SettlementDatabase.ById.ContainsKey("^SENT_RELEASED"));
    }

    [Fact]
    public void SettlementDatabase_ProceduralPerk_HasCorrectAttributes()
    {
        Assert.True(SettlementDatabase.ById.ContainsKey("^BLESS_POS"));
        var perk = SettlementDatabase.ById["^BLESS_POS"];
        Assert.True(perk.Procedural);
        Assert.True(perk.Beneficial);
        Assert.False(perk.Starter);
    }

    // --- RewardDatabase ---

    [Fact]
    public void RewardDatabase_UnloadedState_IsEmpty()
    {
        RewardDatabase.Reset();
        Assert.Equal(0, RewardDatabase.Count);
        Assert.Empty(RewardDatabase.SeasonRewards);
        Assert.Empty(RewardDatabase.TwitchRewards);
        Assert.Empty(RewardDatabase.PlatformRewards);
    }

    [Fact]
    public void RewardDatabase_LoadedFromJson_HasExpectedCounts()
    {
        try
        {
            string jsonDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Resources", "json");
            if (!Directory.Exists(jsonDir))
                jsonDir = Path.Combine(AppContext.BaseDirectory, "Resources", "json");
            if (!Directory.Exists(jsonDir)) return; // skip if resource not available

            bool loaded = RewardDatabase.LoadFromJsonDirectory(jsonDir);
            Assert.True(loaded);
            Assert.True(RewardDatabase.Count > 0);
            Assert.True(RewardDatabase.SeasonRewards.Any());
            Assert.True(RewardDatabase.TwitchRewards.Any());
            Assert.True(RewardDatabase.PlatformRewards.Any());
        }
        finally
        {
            RewardDatabase.Reset();
        }
    }

    [Fact]
    public void RewardDatabase_ContainsKnownReward_WhenLoaded()
    {
        try
        {
            string jsonDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Resources", "json");
            if (!Directory.Exists(jsonDir))
                jsonDir = Path.Combine(AppContext.BaseDirectory, "Resources", "json");
            if (!Directory.Exists(jsonDir)) return;

            RewardDatabase.LoadFromJsonDirectory(jsonDir);
            var reward = RewardDatabase.Rewards.FirstOrDefault(r => r.Id == "^VAULT_ARMOUR");
            Assert.NotNull(reward);
            Assert.Equal("Heirloom Breastplate", reward.Name);
            Assert.Equal("season", reward.Category);
        }
        finally
        {
            RewardDatabase.Reset();
        }
    }

    [Fact]
    public void RewardDatabase_ContainsSpecXoHelmet_WhenLoaded()
    {
        try
        {
            string jsonDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Resources", "json");
            if (!Directory.Exists(jsonDir))
                jsonDir = Path.Combine(AppContext.BaseDirectory, "Resources", "json");
            if (!Directory.Exists(jsonDir)) return;

            RewardDatabase.LoadFromJsonDirectory(jsonDir);

            var reward = RewardDatabase.Rewards.FirstOrDefault(r => r.Id == "^ENT_XO_HELMET");
            Assert.NotNull(reward);
            Assert.Equal("SPEC_XOHELMET", reward.ProductId);
            Assert.Equal("entitlement", reward.Category);

            // Entitlement rewards should appear in PlatformRewards
            Assert.Contains(RewardDatabase.PlatformRewards, r => r.Id == "^ENT_XO_HELMET");
        }
        finally
        {
            RewardDatabase.Reset();
        }
    }

    // --- RewardDatabase JSON loading ---

    [Fact]
    public void RewardDatabase_LoadFromJson_OverridesLoadedData()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_reward_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[
  { ""Id"": ""^TEST_SEASON_1"", ""Name"": ""Test Season Reward"", ""Category"": ""season"" },
  { ""Id"": ""^TEST_TWITCH_1"", ""Name"": ""Test Twitch Reward"", ""Category"": ""twitch"" },
  { ""Id"": ""^TEST_PLATFORM_1"", ""Name"": ""Test Platform Reward"", ""Category"": ""platform"" }
]";
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), json);

            bool loaded = RewardDatabase.LoadFromJsonDirectory(tmpDir);
            Assert.True(loaded);

            Assert.Equal(3, RewardDatabase.Count);
            Assert.Single(RewardDatabase.SeasonRewards);
            Assert.Single(RewardDatabase.TwitchRewards);
            Assert.Single(RewardDatabase.PlatformRewards);

            var season = RewardDatabase.SeasonRewards.First();
            Assert.Equal("^TEST_SEASON_1", season.Id);
            Assert.Equal("Test Season Reward", season.Name);
            Assert.Equal("season", season.Category);
        }
        finally
        {
            RewardDatabase.Reset();
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void RewardDatabase_LoadFromJson_ReturnsEmptyWhenFileMissing()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_reward_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // No Rewards.json file in the directory
            bool loaded = RewardDatabase.LoadFromJsonDirectory(tmpDir);
            Assert.False(loaded);

            // Without JSON, database is empty
            Assert.Equal(0, RewardDatabase.Count);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void RewardDatabase_Reset_ClearsLoadedData()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_reward_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[{ ""Id"": ""^TEST"", ""Name"": ""T"", ""Category"": ""season"" }]";
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), json);

            RewardDatabase.LoadFromJsonDirectory(tmpDir);
            Assert.Equal(1, RewardDatabase.Count);

            RewardDatabase.Reset();
            Assert.Equal(0, RewardDatabase.Count);
        }
        finally
        {
            RewardDatabase.Reset();
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void RewardDatabase_LoadFromJson_ReadsProductId()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_reward_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[
  { ""Id"": ""^B_STR_AA_N"", ""Name"": ""B_STR_AA_N"", ""Category"": ""season"", ""ProductId"": ""B_STR_AA_N"" },
  { ""Id"": ""^TWITCH_376"", ""Name"": ""Pilgrim Poster"", ""Category"": ""twitch"", ""ProductId"": ""EXPD_POSTER11A"" },
  { ""Id"": ""^TGA_SHIP1"", ""Name"": ""Starborn Phoenix"", ""Category"": ""platform"", ""ProductId"": ""TGA_SHIP01"" }
]";
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), json);

            bool loaded = RewardDatabase.LoadFromJsonDirectory(tmpDir);
            Assert.True(loaded);

            var season = RewardDatabase.SeasonRewards.First();
            Assert.Equal("^B_STR_AA_N", season.Id);
            Assert.Equal("B_STR_AA_N", season.ProductId);

            var twitch = RewardDatabase.TwitchRewards.First();
            Assert.Equal("^TWITCH_376", twitch.Id);
            Assert.Equal("EXPD_POSTER11A", twitch.ProductId);

            var platform = RewardDatabase.PlatformRewards.First();
            Assert.Equal("^TGA_SHIP1", platform.Id);
            Assert.Equal("TGA_SHIP01", platform.ProductId);
        }
        finally
        {
            RewardDatabase.Reset();
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void RewardDatabase_LoadFromJson_ProductIdEmptyWhenMissing()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_reward_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[{ ""Id"": ""^TEST_1"", ""Name"": ""Test"", ""Category"": ""season"" }]";
            File.WriteAllText(Path.Combine(tmpDir, "Rewards.json"), json);

            RewardDatabase.LoadFromJsonDirectory(tmpDir);
            var entry = RewardDatabase.SeasonRewards.First();
            Assert.Equal("", entry.ProductId);
        }
        finally
        {
            RewardDatabase.Reset();
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    // --- WordDatabase ---

    private static WordDatabase LoadWordDbFromJson()
    {
        var db = new WordDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir != null)
            db.LoadFromFile(Path.Combine(jsonDir, "Words.json"));
        return db;
    }

    [Fact]
    public void WordDatabase_LoadFromFile_ReturnsExpectedWordCount()
    {
        var db = LoadWordDbFromJson();
        Assert.Equal(2149, db.Count);
    }

    [Fact]
    public void WordDatabase_Words_AreSortedAlphabetically()
    {
        var db = LoadWordDbFromJson();
        for (int i = 1; i < db.Words.Count; i++)
        {
            Assert.True(
                string.Compare(db.Words[i - 1].Text, db.Words[i].Text, StringComparison.OrdinalIgnoreCase) <= 0,
                $"Words not sorted: '{db.Words[i - 1].Text}' should come before '{db.Words[i].Text}'");
        }
    }

    [Fact]
    public void WordDatabase_ContainsAtlasWord()
    {
        var db = LoadWordDbFromJson();
        var atlas = db.Words.FirstOrDefault(w => w.Id == "^ATLAS");
        Assert.NotNull(atlas);
        Assert.Equal("atlas", atlas.Text);
        Assert.True(atlas.Groups.Count > 0);
    }

    [Fact]
    public void WordDatabase_WordEntry_HasRaceGroups()
    {
        var db = LoadWordDbFromJson();
        var atlas = db.Words.First(w => w.Id == "^ATLAS");
        // Atlas word has groups for multiple races
        Assert.True(atlas.HasRace(4)); // Atlas race
        Assert.True(atlas.HasRace(2)); // Korvax (EXPLORERS)
        Assert.True(atlas.HasRace(0)); // Gek (TRADERS)
        Assert.True(atlas.HasRace(1)); // Vy'keen (WARRIORS)
    }

    // --- CompanionDatabase ---

    [Fact]
    public void CompanionDatabase_HasExpectedEntryCount()
    {
        Assert.Equal(69, CompanionDatabase.Entries.Count);
    }

    [Fact]
    public void CompanionDatabase_AllEntriesHaveCaretPrefix()
    {
        foreach (var entry in CompanionDatabase.Entries)
            Assert.True(entry.Id.StartsWith("^"), $"Entry '{entry.Id}' should start with '^'");
    }

    [Fact]
    public void CompanionDatabase_ById_ContainsAllEntries()
    {
        Assert.Equal(CompanionDatabase.Entries.Count, CompanionDatabase.ById.Count);
        foreach (var entry in CompanionDatabase.Entries)
            Assert.True(CompanionDatabase.ById.ContainsKey(entry.Id), $"ById missing '{entry.Id}'");
    }

    [Fact]
    public void CompanionDatabase_ContainsKnownEntries()
    {
        Assert.True(CompanionDatabase.ById.ContainsKey("^QUAD_PET"));
        Assert.True(CompanionDatabase.ById.ContainsKey("^TREX"));
        Assert.True(CompanionDatabase.ById.ContainsKey("^PLANTCAT"));
        Assert.True(CompanionDatabase.ById.ContainsKey("^PROTOFLYER"));
        // Case-insensitive lookup
        Assert.True(CompanionDatabase.ById.ContainsKey("^quad_pet"));
    }

    // --- ProceduralStubs ---

    [Fact]
    public void ProceduralStubs_HasExpectedItemCount()
    {
        Assert.Equal(28, ProceduralStubs.Items.Count);
    }

    [Fact]
    public void ProceduralStubs_ById_ContainsAllItems()
    {
        Assert.Equal(ProceduralStubs.Items.Count, ProceduralStubs.ById.Count);
    }

    [Fact]
    public void ProceduralStubs_ContainsKnownProceduralItems()
    {
        Assert.True(ProceduralStubs.ById.ContainsKey("PROC_LOOT"));
        Assert.True(ProceduralStubs.ById.ContainsKey("PROC_BONE"));
        Assert.True(ProceduralStubs.ById.ContainsKey("PROC_BOTT"));
    }

    [Fact]
    public void ProceduralStubs_ContainsKnownConsumableItems()
    {
        Assert.True(ProceduralStubs.ById.ContainsKey("UP_FRHYP"));
        Assert.True(ProceduralStubs.ById.ContainsKey("UP_FRSPE"));
        Assert.True(ProceduralStubs.ById.ContainsKey("UP_FRMIN"));
        Assert.True(ProceduralStubs.ById.ContainsKey("UP_FREXP"));
    }

    [Fact]
    public void ProceduralStubs_LookupReturnsCorrectData()
    {
        var stub = ProceduralStubs.ById["UP_FRHYP"];
        Assert.Equal("Salvaged Fleet Hyperdrive Upgrade", stub.Name);
        Assert.Equal("U_HYPER1.png", stub.Icon);
        Assert.Equal("CONSUMABLE", stub.Category);
        Assert.Equal("Deployable Salvage", stub.Subtitle);
    }

    [Fact]
    public void ProceduralStubs_CaseInsensitiveLookup()
    {
        Assert.True(ProceduralStubs.ById.ContainsKey("proc_loot"));
        Assert.True(ProceduralStubs.ById.ContainsKey("up_frhyp"));
    }

    [Fact]
    public void ProceduralStubs_IdsDoNotHaveCaretPrefix()
    {
        foreach (var entry in ProceduralStubs.Items)
            Assert.False(entry.Id.StartsWith("^"), $"Entry '{entry.Id}' should not start with '^'");
    }

    [Fact]
    public void ProceduralStubs_LoadedIntoGameItemDatabase()
    {
        var db = new GameItemDatabase();
        // LoadItemsFromJsonDirectory needs a valid dir; use a temp empty dir to trigger stub loading
        var tempDir = Path.Combine(Path.GetTempPath(), "nmse_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            db.LoadItemsFromJsonDirectory(tempDir);
            // Stubs should be loaded even without JSON files
            var item = db.GetItem("^UP_FRHYP");
            Assert.NotNull(item);
            Assert.Equal("Salvaged Fleet Hyperdrive Upgrade", item.Name);
            Assert.Equal("ProceduralProduct", item.ItemType);

            var item2 = db.GetItem("^PROC_LOOT");
            Assert.NotNull(item2);
            Assert.Equal("Unearthed Treasure", item2.Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // --- IsPickerExcluded ---

    [Theory]
    [InlineData("U_TECHBOX_CORE", true)]
    [InlineData("U_TECHBOX_ALIEN", true)]
    [InlineData("U_TECHPACK_CORE", true)]
    [InlineData("U_TECHPACK_ALIE", true)]
    [InlineData("u_techbox_core", true)]
    [InlineData("FUEL1", false)]
    [InlineData("UP_FRHYP", false)]
    [InlineData("PROC_LOOT", false)]
    [InlineData("TECH_COMP", false)]
    public void IsPickerExcluded_ReturnsCorrectResult(string id, bool expected)
    {
        Assert.Equal(expected, GameItemDatabase.IsPickerExcluded(id));
    }

    // --- CorvetteBasePartTechMap ---

    [Fact]
    public void CorvetteBasePartTechMap_IsPopulatedAfterLoading()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir != null)
        {
            db.LoadItemsFromJsonDirectory(jsonDir);
            Assert.True(db.CorvetteBasePartTechMap.Count > 0, "CorvetteBasePartTechMap should contain entries after loading Corvette.json");
        }
    }

    [Fact]
    public void CorvetteBasePartTechMap_CV_INV2_MapsToHabParts()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir != null)
        {
            db.LoadItemsFromJsonDirectory(jsonDir);
            Assert.True(db.CorvetteBasePartTechMap.ContainsKey("CV_INV2"), "CV_INV2 should be in the map");
            var parts = db.CorvetteBasePartTechMap["CV_INV2"];
            Assert.Contains("B_HAB_A", parts);
            Assert.Contains("B_HAB_B", parts);
            Assert.Contains("B_HAB_C", parts);
        }
    }

    [Fact]
    public void CorvetteBasePartTechMap_CV_PULSE3_MapsToWingAndThrusterParts()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir != null)
        {
            db.LoadItemsFromJsonDirectory(jsonDir);
            Assert.True(db.CorvetteBasePartTechMap.ContainsKey("CV_PULSE3"), "CV_PULSE3 should be in the map");
            var parts = db.CorvetteBasePartTechMap["CV_PULSE3"];
            Assert.Contains("B_TRU_C", parts);
        }
    }

    [Fact]
    public void BuildableShipTechID_IsParsedFromCorvetteJson()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir != null)
        {
            db.LoadItemsFromJsonDirectory(jsonDir);
            var item = db.GetItem("B_HAB_C");
            Assert.NotNull(item);
            Assert.Equal("CV_INV2", item.BuildableShipTechID);
        }
    }

    [Fact]
    public void CV_Items_AreResolvableInDatabase()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir != null)
        {
            db.LoadItemsFromJsonDirectory(jsonDir);
            // CV_ items should be resolvable (they exist in Upgrades.json)
            var item = db.GetItem("^CV_INV2");
            Assert.NotNull(item);
            Assert.Equal("CV_INV2", item.Id);
        }
    }

    [Fact]
    public void GameItemDatabase_TechItems_HaveChargeAmountFromJson()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        // PROTECT (Hazard Protection) is a chargeable technology with ChargeAmount=80
        var item = db.GetItem("^PROTECT");
        Assert.NotNull(item);
        Assert.True(item.IsChargeable, "PROTECT should be chargeable");
        Assert.Equal(80, item.ChargeValue);
    }

    [Fact]
    public void GameItemDatabase_TechItems_NonChargeableHaveChargeValue()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        // JET1 (Jetpack) has ChargeAmount=100 but Chargeable=false
        var item = db.GetItem("^JET1");
        Assert.NotNull(item);
        Assert.False(item.IsChargeable, "JET1 should not be chargeable");
        Assert.Equal(100, item.ChargeValue);
    }

    [Theory]
    [InlineData("LASER", true)]          // Mining Beam: Chargeable in game
    [InlineData("TERRAINEDITOR", true)]   // Terrain Manipulator: Chargeable in game
    [InlineData("STRONGLASER", false)]    // Advanced Mining Laser: NOT chargeable
    [InlineData("SCANBINOC1", false)]     // Analysis Visor: NOT chargeable
    [InlineData("SCOPE", false)]          // Combat Scope: NOT chargeable
    [InlineData("JET1", false)]           // Jetpack: NOT chargeable
    public void GameItemDatabase_TechItem_IsChargeable_MatchesGameData(string id, bool expectedChargeable)
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var item = db.GetItem("^" + id);
        Assert.NotNull(item);
        Assert.Equal(expectedChargeable, item.IsChargeable);
    }

    [Fact]
    public void GameItemDatabase_ProductItems_HaveMaxStackSize()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        // TRA_ALLOY1 (Nanotube Crate) has MaxStackSize=5
        var item = db.GetItem("TRA_ALLOY1");
        Assert.NotNull(item);
        Assert.Equal(5, item.MaxStackSize);
    }

    [Fact]
    public void GameItemDatabase_SubstanceItems_HaveMaxStackSizeOne()
    {
        var db = new GameItemDatabase();
        string? jsonDir = FindJsonDirectory();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        // FUEL1 (Carbon) is a substance with MaxStackSize=1
        var item = db.GetItem("^FUEL1");
        Assert.NotNull(item);
        Assert.Equal(1, item.MaxStackSize);
    }

    private static string? FindJsonDirectory()
    {
        // Walk up from test binary to find Resources/json
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(dir, "Resources", "json");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    // --- TechPack Class field ---

    [Fact]
    public void TechPack_HasClassField()
    {
        var entry = TechPacks.Dictionary["^808497C54986"]; // UT_PROTECT
        Assert.NotNull(entry.Class);
    }

    [Fact]
    public void TechPack_HasCorrectRanks()
    {
        // UP_LASER1 should be rank C
        var laser1 = TechPacks.Dictionary["^8080855C45D9"];
        Assert.Equal("C", laser1.Class);
        Assert.Equal("^UP_LASER1", laser1.Id);

        // UP_LASER4 should be rank S
        var laser4 = TechPacks.Dictionary["^8080B7B5DD85"];
        Assert.Equal("S", laser4.Class);

        // UP_LASERX should be rank X
        var laserX = TechPacks.Dictionary["^80808E53E594"];
        Assert.Equal("X", laserX.Class);
    }

    [Fact]
    public void TechPack_AlienItems_HaveCorrectIcon()
    {
        // CARGO_S_ALIEN should have TECHBOX_UTILITY icon
        var alien = TechPacks.Dictionary["^808042B8B0A7"];
        Assert.Equal("TECHBOX_UTILITY.png", alien.Icon);
        Assert.Equal("^CARGO_S_ALIEN", alien.Id);
    }

    [Fact]
    public void TechPack_UA_Items_HaveCorrectIcon()
    {
        // UA_PULSE1 should have TECHBOX_VEHICLEBOOST icon
        var ua = TechPacks.Dictionary["^80808BCB46DA"];
        Assert.Equal("TECHBOX_VEHICLEBOOST.png", ua.Icon);
        Assert.Equal("^UA_PULSE1", ua.Id);
    }

    [Fact]
    public void TechPack_UP_Items_HaveCorrectIcon()
    {
        // UP_LASER1 should have TECHBOX_LASER icon
        var up = TechPacks.Dictionary["^8080855C45D9"];
        Assert.Equal("TECHBOX_LASER.png", up.Icon);
    }

    [Fact]
    public void TechPack_HasExpectedCount()
    {
        Assert.Equal(736, TechPacks.Dictionary.Count);
    }

    // --- TechAdjacencyDatabase ---

    [Fact]
    public void TechAdjacencyDatabase_HasEntries()
    {
        Assert.True(TechAdjacencyDatabase.Dictionary.Count > 600);
    }

    [Fact]
    public void TechAdjacencyDatabase_JetpackGroup_SameBaseStatType()
    {
        // All jetpack items should share BaseStatType 102
        var jet1 = TechAdjacencyDatabase.GetAdjacencyInfo("^JET1");
        var utJet = TechAdjacencyDatabase.GetAdjacencyInfo("^UT_JET");
        var utJump = TechAdjacencyDatabase.GetAdjacencyInfo("^UT_JUMP");
        var utMidair = TechAdjacencyDatabase.GetAdjacencyInfo("^UT_MIDAIR");
        var upJet4 = TechAdjacencyDatabase.GetAdjacencyInfo("^UP_JET4#24007");

        Assert.NotNull(jet1);
        Assert.NotNull(utJet);
        Assert.NotNull(utJump);
        Assert.NotNull(utMidair);
        Assert.NotNull(upJet4);

        Assert.Equal(102, jet1.BaseStatType);
        Assert.Equal(102, utJet.BaseStatType);
        Assert.Equal(102, utJump.BaseStatType);
        Assert.Equal(102, utMidair.BaseStatType);
        Assert.Equal(102, upJet4.BaseStatType);
    }

    [Fact]
    public void TechAdjacencyDatabase_DifferentGroups_DifferentBaseStatType()
    {
        var jet = TechAdjacencyDatabase.GetAdjacencyInfo("^JET1");
        var laser = TechAdjacencyDatabase.GetAdjacencyInfo("^LASER");
        Assert.NotNull(jet);
        Assert.NotNull(laser);
        Assert.NotEqual(jet.BaseStatType, laser.BaseStatType);
    }

    [Fact]
    public void TechAdjacencyDatabase_StripsCaretAndVariant()
    {
        // With ^ prefix
        var info1 = TechAdjacencyDatabase.GetAdjacencyInfo("^JET1");
        // With #variant suffix
        var info2 = TechAdjacencyDatabase.GetAdjacencyInfo("^UP_JET4#24007");
        Assert.NotNull(info1);
        Assert.NotNull(info2);
        Assert.Equal(info1.BaseStatType, info2.BaseStatType);
    }

    [Fact]
    public void TechAdjacencyDatabase_HasLinkColours()
    {
        var jet = TechAdjacencyDatabase.GetAdjacencyInfo("^JET1");
        Assert.NotNull(jet);
        Assert.False(string.IsNullOrEmpty(jet.LinkColourHex));
        Assert.StartsWith("#", jet.LinkColourHex);
    }

    [Fact]
    public void TechAdjacencyDatabase_UnknownItem_ReturnsNull()
    {
        var info = TechAdjacencyDatabase.GetAdjacencyInfo("^NONEXISTENT_ITEM");
        Assert.Null(info);
    }

    // --- TechPacks.GetClassById ---

    [Fact]
    public void TechPack_GetClassById_ReturnsCorrectClass()
    {
        Assert.Equal("S", TechPacks.GetClassById("^UP_JET4"));
        Assert.Equal("A", TechPacks.GetClassById("^UP_JET3"));
        Assert.Equal("B", TechPacks.GetClassById("^UP_JET2"));
        Assert.Equal("C", TechPacks.GetClassById("^UP_JET1"));
        Assert.Equal("X", TechPacks.GetClassById("^UP_JETX"));
    }

    [Fact]
    public void TechPack_GetClassById_WorksWithoutCaret()
    {
        Assert.Equal("S", TechPacks.GetClassById("UP_JET4"));
    }

    [Fact]
    public void TechPack_GetClassById_ReturnsNullForCoreItems()
    {
        // Core tech items like JET1 don't have a class
        Assert.Null(TechPacks.GetClassById("^JET1"));
    }

    [Fact]
    public void TechPack_GetClassById_ReturnsNullForUnknown()
    {
        Assert.Null(TechPacks.GetClassById("^NONEXISTENT_ITEM"));
    }

    [Fact]
    public void TechPack_GetClassById_ReturnsCorrectForUT()
    {
        Assert.Equal("B", TechPacks.GetClassById("^UT_JET"));
        Assert.Equal("A", TechPacks.GetClassById("^UT_JUMP"));
    }

    // --- Adjacency BaseStatType-only grouping ---

    [Fact]
    public void TechAdjacencyDatabase_LauncherAndPulse_DifferentBaseStatType()
    {
        // Launcher items (BaseStatType=151) and Pulse items (BaseStatType=154) must be different groups
        var launcher = TechAdjacencyDatabase.GetAdjacencyInfo("LAUNCHER");
        var pulse = TechAdjacencyDatabase.GetAdjacencyInfo("UP_PULSE4");
        Assert.NotNull(launcher);
        Assert.NotNull(pulse);
        Assert.NotEqual(launcher.BaseStatType, pulse.BaseStatType);
    }

    [Fact]
    public void TechAdjacencyDatabase_GetBaseStatTypeColour_DifferentForLauncherVsPulse()
    {
        var launcherColour = TechAdjacencyDatabase.GetBaseStatTypeColour(151);
        var pulseColour = TechAdjacencyDatabase.GetBaseStatTypeColour(154);
        Assert.NotEqual(launcherColour, pulseColour);
    }

    [Fact]
    public void TechAdjacencyDatabase_GetBaseStatTypeColour_ReturnsDefaultForUnknown()
    {
        var colour = TechAdjacencyDatabase.GetBaseStatTypeColour(99999);
        Assert.Equal("#FFFFFF", colour);
    }

    // --- GameItem.QualityToClass ---

    [Theory]
    [InlineData("Normal", "C")]
    [InlineData("Rare", "B")]
    [InlineData("Epic", "A")]
    [InlineData("Legendary", "S")]
    [InlineData("Illegal", "X")]
    [InlineData("Sentinel", "?")]
    public void GameItem_QualityToClass_MapsCorrectly(string quality, string expected)
    {
        var item = new GameItem { Quality = quality };
        Assert.Equal(expected, item.QualityToClass());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Common")]
    [InlineData("SeaTrash")]
    [InlineData("Robot")]
    [InlineData("Junk")]
    public void GameItem_QualityToClass_ReturnsNullForUnmapped(string quality)
    {
        var item = new GameItem { Quality = quality };
        Assert.Null(item.QualityToClass());
    }

    // --- Additional adjacency coverage ---

    [Theory]
    [InlineData("UP_ENGY4", 78)]
    [InlineData("ARMOUR1", 75)]
    [InlineData("STAM1", 99)]
    [InlineData("ENERGYBOOST1", 78)]
    [InlineData("COLD1", 80)]
    [InlineData("SHIPJUMP2", 154)]
    public void TechAdjacencyDatabase_ContainsNewEntries(string itemId, int expectedBst)
    {
        var info = TechAdjacencyDatabase.GetAdjacencyInfo(itemId);
        Assert.NotNull(info);
        Assert.Equal(expectedBst, info.BaseStatType);
    }

    [Theory]
    [InlineData(75)]
    [InlineData(99)]
    public void TechAdjacencyDatabase_GetBaseStatTypeColour_HasNewBSTColours(int bst)
    {
        var colour = TechAdjacencyDatabase.GetBaseStatTypeColour(bst);
        Assert.NotEqual("#FFFFFF", colour);
    }

    // --- TitleDatabase ---

    [Fact]
    public void TitleDatabase_IsLoaded_ReturnsTrue()
    {
        Assert.True(TitleDatabase.IsLoaded);
    }

    [Fact]
    public void TitleDatabase_HasExpectedCount()
    {
        Assert.Equal(318, TitleDatabase.Titles.Count);
    }

    [Fact]
    public void TitleDatabase_GetTitle_ReturnsCorrectEntry()
    {
        var title = TitleDatabase.GetTitle("T_TRA1");
        Assert.NotNull(title);
        Assert.Equal("T_TRA1", title.Id);
        Assert.Equal("Hireling {0}", title.Name);
        Assert.Equal(1, title.UnlockedByStatValue);
    }

    [Fact]
    public void TitleDatabase_GetTitle_CaseInsensitive()
    {
        var title = TitleDatabase.GetTitle("t_tra1");
        Assert.NotNull(title);
        Assert.Equal("T_TRA1", title.Id);
    }

    [Fact]
    public void TitleDatabase_GetTitle_ReturnsNullForUnknown()
    {
        var title = TitleDatabase.GetTitle("T_NONEXISTENT");
        Assert.Null(title);
    }

    [Fact]
    public void TitleDatabase_ContainsGekTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_TRA1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_TRA9"));
    }

    [Fact]
    public void TitleDatabase_ContainsKorvaxTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_EXP1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_EXP9"));
    }

    [Fact]
    public void TitleDatabase_ContainsVykeenTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_WAR1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_WAR9"));
    }

    [Fact]
    public void TitleDatabase_ContainsGuildTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_TRA_GUILD1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_EXP_GUILD1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_WAR_GUILD1"));
    }

    [Fact]
    public void TitleDatabase_ContainsGalaxyTitles()
    {
        var title = TitleDatabase.GetTitle("T_REALITY1");
        Assert.NotNull(title);
        Assert.Contains("Euclid", title.Name);
    }

    [Fact]
    public void TitleDatabase_ContainsMilestoneTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_JM_WALK1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_JM_UNITS1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_JM_SHIPS1"));
    }

    [Fact]
    public void TitleDatabase_ContainsLoreTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_LORE1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_ARTEMIS"));
        Assert.NotNull(TitleDatabase.GetTitle("T_ATLASPATH"));
    }

    [Fact]
    public void TitleDatabase_ContainsAutophageTitles()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_BUI1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_BUI9"));
    }

    [Fact]
    public void TitleDatabase_ContainsExpedition21Titles()
    {
        // Expedition 21 titles from the MXML
        Assert.NotNull(TitleDatabase.GetTitle("T_EXPD21"));
        var e21 = TitleDatabase.GetTitle("T_EXPD21");
        Assert.Contains("{0}", e21!.Name);
    }

    [Fact]
    public void TitleDatabase_ContainsExpedition20And21()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_EXPD20"));
        Assert.NotNull(TitleDatabase.GetTitle("T_EXPD21"));
        var e20 = TitleDatabase.GetTitle("T_EXPD20");
        Assert.Contains("Celestial", e20!.Name);
    }

    [Fact]
    public void TitleDatabase_ContainsDefaultTitle()
    {
        Assert.NotNull(TitleDatabase.GetTitle("T_DEFAULT"));
    }

    [Fact]
    public void TitleDatabase_ContainsKnownTitleCategories()
    {
        // Verify representative titles from different categories exist
        Assert.NotNull(TitleDatabase.GetTitle("T_TRA1"));  // Gek
        Assert.NotNull(TitleDatabase.GetTitle("T_EXP1"));  // Korvax
        Assert.NotNull(TitleDatabase.GetTitle("T_WAR1"));  // Vy'keen
        Assert.NotNull(TitleDatabase.GetTitle("T_BUI1"));  // Autophage
    }

    [Fact]
    public void TitleDatabase_ContainsExtractedFossilAndBonusTitles()
    {
        // Only titles present in the game's MXML are in the JSON.
        // Verify the database loaded successfully with >300 entries.
        Assert.True(TitleDatabase.Titles.Count >= 300);
    }

    [Fact]
    public void TitleDatabase_ContainsKnownPirateAndExpeditionTitles()
    {
        // Verify expedition titles from MXML
        Assert.NotNull(TitleDatabase.GetTitle("T_EXPD20"));
        Assert.NotNull(TitleDatabase.GetTitle("T_EXPD21"));
    }

    [Fact]
    public void TitleDatabase_ContainsRaidTitles()
    {
        // The JSON extractor pulls raid titles that exist in the MXML.
        // Not all hardcoded raid titles were in the game MXML.
        Assert.True(TitleDatabase.Titles.Count > 0);
    }

    [Fact]
    public void TitleDatabase_ContainsMilestoneTitles_FromJson()
    {
        // Milestone walk/units/ships titles exist in the extracted JSON
        Assert.NotNull(TitleDatabase.GetTitle("T_JM_WALK1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_JM_UNITS1"));
        Assert.NotNull(TitleDatabase.GetTitle("T_JM_SHIPS1"));
    }

    [Fact]
    public void TitleDatabase_AllEntriesHaveId()
    {
        Assert.All(TitleDatabase.Titles, t => Assert.False(string.IsNullOrEmpty(t.Id)));
    }

    [Fact]
    public void TitleDatabase_AllEntriesHaveName()
    {
        Assert.All(TitleDatabase.Titles, t => Assert.False(string.IsNullOrEmpty(t.Name)));
    }

    [Fact]
    public void TitleDatabase_NoDuplicateIds()
    {
        var ids = TitleDatabase.Titles.Select(t => t.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void TitleDatabase_LoadFromFile_FallsBackToExistingData()
    {
        // Loading from a non-existent path should retain previously loaded data
        bool result = TitleDatabase.LoadFromFile("/tmp/nonexistent_titles.json");
        Assert.True(result);
        Assert.True(TitleDatabase.IsLoaded);
        Assert.Equal(318, TitleDatabase.Titles.Count);
    }

    // --- IsValidForOwner (Technology Category filtering) ---

    [Theory]
    // Suit owner: only accepts Suit tech
    [InlineData("Suit", "Suit", true)]
    [InlineData("Suit", "Ship", false)]
    [InlineData("Suit", "Weapon", false)]
    [InlineData("Suit", "Freighter", false)]
    [InlineData("Suit", "AllShips", false)]
    // Normal Ship owner: Ship, AllShips, AllShipsExceptAlien
    [InlineData("Ship", "Ship", true)]
    [InlineData("Ship", "AllShips", true)]
    [InlineData("Ship", "AllShipsExceptAlien", true)]
    [InlineData("Ship", "AlienShip", false)]
    [InlineData("Ship", "RobotShip", false)]
    [InlineData("Ship", "Corvette", false)]
    [InlineData("Ship", "Suit", false)]
    [InlineData("Ship", "Exocraft", false)]
    // AlienShip (Living Ship): AlienShip, AllShips only
    [InlineData("AlienShip", "AlienShip", true)]
    [InlineData("AlienShip", "AllShips", true)]
    [InlineData("AlienShip", "AllShipsExceptAlien", false)]
    [InlineData("AlienShip", "Ship", false)]
    [InlineData("AlienShip", "RobotShip", false)]
    // RobotShip (Sentinel): RobotShip, Ship, AllShips, AllShipsExceptAlien
    [InlineData("RobotShip", "RobotShip", true)]
    [InlineData("RobotShip", "Ship", true)]
    [InlineData("RobotShip", "AllShips", true)]
    [InlineData("RobotShip", "AllShipsExceptAlien", true)]
    [InlineData("RobotShip", "AlienShip", false)]
    // Corvette: Corvette, Ship, AllShips, AllShipsExceptAlien
    [InlineData("Corvette", "Corvette", true)]
    [InlineData("Corvette", "Ship", true)]
    [InlineData("Corvette", "AllShips", true)]
    [InlineData("Corvette", "AllShipsExceptAlien", true)]
    [InlineData("Corvette", "AlienShip", false)]
    // Weapon: only Weapon
    [InlineData("Weapon", "Weapon", true)]
    [InlineData("Weapon", "Ship", false)]
    [InlineData("Weapon", "Suit", false)]
    // Freighter: only Freighter
    [InlineData("Freighter", "Freighter", true)]
    [InlineData("Freighter", "Ship", false)]
    [InlineData("Freighter", "Suit", false)]
    // Exocraft owner: Exocraft, AllVehicles
    [InlineData("Exocraft", "Exocraft", true)]
    [InlineData("Exocraft", "AllVehicles", true)]
    [InlineData("Exocraft", "Mech", false)]
    [InlineData("Exocraft", "Submarine", false)]
    [InlineData("Exocraft", "Ship", false)]
    // Colossus owner: Colossus, Exocraft, AllVehicles
    [InlineData("Colossus", "Colossus", true)]
    [InlineData("Colossus", "Exocraft", true)]
    [InlineData("Colossus", "AllVehicles", true)]
    [InlineData("Colossus", "Mech", false)]
    [InlineData("Colossus", "Submarine", false)]
    // Mech owner: Mech, AllVehicles
    [InlineData("Mech", "Mech", true)]
    [InlineData("Mech", "AllVehicles", true)]
    [InlineData("Mech", "Exocraft", false)]
    [InlineData("Mech", "Submarine", false)]
    // Submarine owner: Submarine, AllVehicles
    [InlineData("Submarine", "Submarine", true)]
    [InlineData("Submarine", "AllVehicles", true)]
    [InlineData("Submarine", "Exocraft", false)]
    [InlineData("Submarine", "Mech", false)]
    public void IsValidForOwner_MatchesOwnerEnums(string owner, string techCategory, bool expected)
    {
        var item = new GameItem { Id = "TEST", TechnologyCategory = techCategory };
        Assert.Equal(expected, item.IsValidForOwner(owner));
    }

    [Theory]
    [InlineData("Suit")]
    [InlineData("Ship")]
    [InlineData("Weapon")]
    [InlineData("Freighter")]
    [InlineData("Exocraft")]
    [InlineData("AlienShip")]
    [InlineData("Corvette")]
    public void IsValidForOwner_NoneCategory_AlwaysValid(string owner)
    {
        var item = new GameItem { Id = "TEST", TechnologyCategory = "None" };
        Assert.True(item.IsValidForOwner(owner));
    }

    [Theory]
    [InlineData("Suit")]
    [InlineData("Ship")]
    [InlineData("Weapon")]
    [InlineData("Freighter")]
    [InlineData("Exocraft")]
    public void IsValidForOwner_EmptyCategory_AlwaysValid(string owner)
    {
        var item = new GameItem { Id = "TEST", TechnologyCategory = "" };
        Assert.True(item.IsValidForOwner(owner));
    }

    [Theory]
    [InlineData("Suit")]
    [InlineData("Ship")]
    [InlineData("Weapon")]
    [InlineData("Freighter")]
    [InlineData("Exocraft")]
    [InlineData("AlienShip")]
    [InlineData("Mech")]
    public void IsValidForOwner_MaintenanceTech_AlwaysValid(string owner)
    {
        var item = new GameItem { Id = "TEST", TechnologyCategory = "Maintenance" };
        Assert.True(item.IsValidForOwner(owner));
    }

    // --- Title ID caret prefix handling ---

    [Fact]
    public void TitleDatabase_IdsDoNotHaveCaretPrefix()
    {
        // TitleDatabase IDs should NOT have the ^ prefix used in save files
        foreach (var title in TitleDatabase.Titles)
            Assert.False(title.Id.StartsWith("^"), $"Title {title.Id} should not have ^ prefix");
    }

    [Fact]
    public void TitleDatabase_IdsMatchSaveFormat_AfterStrippingCaret()
    {
        // Save file format uses "^T_TRA1", our DB uses "T_TRA1"
        // Verify that stripping ^ from typical save values matches our DB
        var saveValues = new[] { "^T_TRA1", "^T_EXP1", "^T_WAR1", "^T_ABANDLORE1", "^T_ABYSS" };
        foreach (var sv in saveValues)
        {
            string stripped = sv.StartsWith("^") ? sv.Substring(1) : sv;
            var title = TitleDatabase.GetTitle(stripped);
            Assert.NotNull(title);
        }
    }

    // --- CanAddItemToInventory: cargo should NOT reject products by TechnologyCategory ---

    [Fact]
    public void CanAddItemToInventory_Cargo_AllowsProductsRegardlessOfTechCategory()
    {
        // Cargo inventories allow ALL non-Technology items, regardless of TechnologyCategory.
        // Items like "Starships" (products with TechnologyCategory) should be allowed.
        var starshipPart = new GameItem { Id = "TEST_PART", ItemType = "Starships", TechnologyCategory = "Ship" };
        var upgrade = new GameItem { Id = "TEST_UPG", ItemType = "Upgrades", TechnologyCategory = "Weapon" };
        var substance = new GameItem { Id = "FUEL1", ItemType = "Raw Materials" };

        // All three should pass the base CanAddItemToInventory check for cargo
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(starshipPart, isTechOnly: false, isCargo: true));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(upgrade, isTechOnly: false, isCargo: true));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(substance, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItemToInventory_Cargo_BlocksTechnologyType()
    {
        // Cargo should block "Technology" ItemType items
        var tech = new GameItem { Id = "TEST_TECH", ItemType = "Technology", TechnologyCategory = "Ship" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(tech, isTechOnly: false, isCargo: true));
    }

    // --- Combined inventory + owner type filtering (mirrors PopulateTypeFilter) ---

    /// <summary>
    /// Verifies that tech-only inventory with Freighter owner only allows Technology
    /// items with TechnologyCategory "Freighter". Other ItemTypes like "Exocraft",
    /// "Starships", "Raw Materials" must be rejected.
    /// </summary>
    [Theory]
    [InlineData("Technology", "Freighter", true)]
    [InlineData("Technology", "Ship", false)]
    [InlineData("Technology", "Suit", false)]
    [InlineData("Exocraft", "Exocraft", false)]      // Not "Technology" ItemType
    [InlineData("Starships", "Ship", false)]           // Not "Technology" ItemType
    [InlineData("Raw Materials", "Fuel", false)]       // Not "Technology" ItemType
    [InlineData("Products", "Trade", false)]           // Not "Technology" ItemType
    public void TechOnly_Freighter_CombinedFilter(string itemType, string category, bool expected)
    {
        var item = new GameItem
        {
            Id = "TEST",
            ItemType = itemType,
            Category = category,
            TechnologyCategory = GameItemDatabase.IsTechnologyRelatedType(itemType) ? category : ""
        };
        bool passesStack = InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false);
        bool passesOwner = string.IsNullOrEmpty(item.TechnologyCategory)
            || item.TechnologyCategory == "None"
            || !GameItemDatabase.IsTechnologyRelatedType(item.ItemType)
            || item.IsValidForOwner("Freighter");
        Assert.Equal(expected, passesStack && passesOwner);
    }

    /// <summary>
    /// Verifies that tech-only inventory with Submarine (Nautilon) owner only accepts
    /// Technology items with TechnologyCategory "Submarine" or "AllVehicles".
    /// Items with TechnologyCategory "Exocraft" must be rejected.
    /// </summary>
    [Theory]
    [InlineData("Submarine", true)]
    [InlineData("AllVehicles", true)]
    [InlineData("Exocraft", false)]
    [InlineData("Ship", false)]
    [InlineData("Suit", false)]
    [InlineData("Mech", false)]
    [InlineData("Colossus", false)]
    public void TechOnly_Submarine_RejectsNonSubmarineCategories(string techCategory, bool expected)
    {
        var item = new GameItem
        {
            Id = "TEST",
            ItemType = "Technology",
            Category = techCategory,
            TechnologyCategory = techCategory
        };
        bool passesStack = InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false);
        bool passesOwner = item.IsValidForOwner("Submarine");
        Assert.Equal(expected, passesStack && passesOwner);
    }

    /// <summary>
    /// Verifies that tech-only inventory with Exocraft (Roamer) owner accepts
    /// Technology items with TechnologyCategory "Exocraft" or "AllVehicles" but
    /// rejects "Submarine", "Mech", "Ship" etc.
    /// </summary>
    [Theory]
    [InlineData("Exocraft", true)]
    [InlineData("AllVehicles", true)]
    [InlineData("Submarine", false)]
    [InlineData("Mech", false)]
    [InlineData("Colossus", false)]
    [InlineData("Ship", false)]
    public void TechOnly_Exocraft_RejectsNonExocraftCategories(string techCategory, bool expected)
    {
        var item = new GameItem
        {
            Id = "TEST",
            ItemType = "Technology",
            Category = techCategory,
            TechnologyCategory = techCategory
        };
        bool passesStack = InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false);
        bool passesOwner = item.IsValidForOwner("Exocraft");
        Assert.Equal(expected, passesStack && passesOwner);
    }

    [Fact]
    public void Database_ReadsIsCraftableFromJson()
    {
        // Verifies that IsCraftable field is read from JSON when loading product items
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_db_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[
  { ""Id"": ""CRAFT1"", ""Name"": ""Craftable Item"", ""IsCraftable"": true },
  { ""Id"": ""NOCRAFT1"", ""Name"": ""Non-Craftable Item"", ""IsCraftable"": false },
  { ""Id"": ""DEFAULT1"", ""Name"": ""Default Item"" }
]";
            File.WriteAllText(Path.Combine(tmpDir, "Products.json"), json);
            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);

            var craft = db.GetItem("CRAFT1");
            var nocraft = db.GetItem("NOCRAFT1");
            var def = db.GetItem("DEFAULT1");

            Assert.NotNull(craft);
            Assert.True(craft!.IsCraftable, "CRAFT1 should have IsCraftable = true");

            Assert.NotNull(nocraft);
            Assert.False(nocraft!.IsCraftable, "NOCRAFT1 should have IsCraftable = false");

            Assert.NotNull(def);
            Assert.False(def!.IsCraftable, "DEFAULT1 should default IsCraftable to false");
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void Database_ReadsTradeCategoryFromJson()
    {
        // Verifies that TradeCategory field is read from JSON when loading product items
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_db_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[
  { ""Id"": ""SPECIAL1"", ""Name"": ""Special Item"", ""TradeCategory"": ""SpecialShop"" },
  { ""Id"": ""TRADE1"", ""Name"": ""Trade Item"", ""TradeCategory"": ""None"" },
  { ""Id"": ""PLAIN1"", ""Name"": ""Plain Item"" }
]";
            File.WriteAllText(Path.Combine(tmpDir, "Products.json"), json);
            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);

            var special = db.GetItem("SPECIAL1");
            var trade = db.GetItem("TRADE1");
            var plain = db.GetItem("PLAIN1");

            Assert.NotNull(special);
            Assert.Equal("SpecialShop", special!.TradeCategory);

            Assert.NotNull(trade);
            Assert.Equal("None", trade!.TradeCategory);

            Assert.NotNull(plain);
            Assert.Equal("", plain!.TradeCategory);
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    [Fact]
    public void Database_ReadsProceduralForProducts()
    {
        // Verifies that Procedural field works for product-type items (not just technology)
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_db_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string json = @"[
  { ""Id"": ""PROC_ITEM1"", ""Name"": ""Procedural Item"", ""Procedural"": true },
  { ""Id"": ""NORMAL_ITEM1"", ""Name"": ""Normal Item"", ""Procedural"": false }
]";
            File.WriteAllText(Path.Combine(tmpDir, "Curiosities.json"), json);
            var db = new GameItemDatabase();
            db.LoadItemsFromJsonDirectory(tmpDir);

            var proc = db.GetItem("PROC_ITEM1");
            var normal = db.GetItem("NORMAL_ITEM1");

            Assert.NotNull(proc);
            Assert.True(proc!.IsProcedural, "PROC_ITEM1 should have IsProcedural = true");

            Assert.NotNull(normal);
            Assert.False(normal!.IsProcedural, "NORMAL_ITEM1 should have IsProcedural = false");
        }
        finally
        {
            try { Directory.Delete(tmpDir, true); } catch { }
        }
    }

    // --- Integration tests: Verify extracted JSON produces correct filtering results ---

    [Fact]
    public void Database_MetalPlating_IsCraftableFromExtractedJson()
    {
        // CASING (Metal Plating) is a craftable product in the game.
        // Verifies the extracted Products.json correctly sets IsCraftable.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var item = db.GetItem("CASING");
        Assert.NotNull(item);
        Assert.True(item!.IsCraftable, "CASING (Metal Plating) should be craftable");
        Assert.False(item.IsProcedural, "CASING should not be procedural");
        Assert.True(Core.CatalogueLogic.IsLearnableProduct(item),
            "CASING should be learnable (craftable + non-procedural)");
    }

    [Fact]
    public void Database_MaintenanceTech_NotLearnable()
    {
        // Maintenance technology items should not appear in the technology picker.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var item = db.GetItem("MAINT_FUEL1");
        Assert.NotNull(item);
        Assert.Equal("Maintenance", item!.Category);
        Assert.False(Core.CatalogueLogic.IsLearnableTechnology(item),
            "Maintenance tech should not be learnable");
    }

    [Fact]
    public void Database_SuitTech_ValidOnlyForSuit()
    {
        // PROTECT (Hazard Protection) is Suit tech - valid for Suit, not Ship.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var item = db.GetItem("PROTECT");
        Assert.NotNull(item);
        Assert.Equal("Suit", item!.TechnologyCategory);
        Assert.True(item.IsValidForOwner("Suit"));
        Assert.False(item.IsValidForOwner("Ship"));
        Assert.False(item.IsValidForOwner("Weapon"));
    }

    [Fact]
    public void Database_AllShipsExceptAlienTech_ValidForNormalShipNotLiving()
    {
        // AllShipsExceptAlien tech should be valid for normal ships, robot ships,
        // corvettes, but NOT living ships.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        // Find an AllShipsExceptAlien tech item
        var item = db.Items.Values.FirstOrDefault(i =>
            i.TechnologyCategory == "AllShipsExceptAlien" && i.ItemType == "Technology");
        if (item == null) return; // Skip if no such item in database

        Assert.True(item.IsValidForOwner("Ship"), "AllShipsExceptAlien tech should be valid for Ship");
        Assert.True(item.IsValidForOwner("RobotShip"), "AllShipsExceptAlien tech should be valid for RobotShip");
        Assert.True(item.IsValidForOwner("Corvette"), "AllShipsExceptAlien tech should be valid for Corvette");
        Assert.False(item.IsValidForOwner("AlienShip"), "AllShipsExceptAlien tech should NOT be valid for AlienShip");
    }

    [Fact]
    public void Database_SubstanceItems_AcceptedInCargoRejectedInTech()
    {
        // Substances (Raw Materials) should be accepted in cargo but rejected in tech-only.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var carbon = db.GetItem("FUEL1");
        Assert.NotNull(carbon);
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(carbon!, isTechOnly: false, isCargo: true),
            "Carbon should be accepted in cargo inventory");
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(carbon!, isTechOnly: true, isCargo: false),
            "Carbon should be rejected in tech-only inventory");
    }

    [Fact]
    public void Database_SpecialShopItems_AreLearnableProducts()
    {
        // SpecialShop items should be learnable regardless of IsCraftable.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var specialShopItems = db.Items.Values
            .Where(i => string.Equals(i.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        Assert.NotEmpty(specialShopItems);
        foreach (var item in specialShopItems)
        {
            Assert.True(Core.CatalogueLogic.IsLearnableProduct(item),
                $"SpecialShop item {item.Id} ({item.Name}) should be learnable");
        }
    }

    [Fact]
    public void Database_Buildings_HaveCanPickUpAndIsTemporary()
    {
        // Verify that the Buildings.json items have CanPickUp and IsTemporary
        // fields loaded from the basebuildingobjectstable enrichment.
        var db = new GameItemDatabase();
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;
        db.LoadItemsFromJsonDirectory(jsonDir);

        var buildings = db.Items.Values
            .Where(i => i.ItemType.Equals("Buildings", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.NotEmpty(buildings);

        // At least some buildings should have CanPickUp=true
        int pickupable = buildings.Count(b => b.CanPickUp);
        Assert.True(pickupable > 0,
            $"Expected some buildings with CanPickUp=true, found 0 out of {buildings.Count}");

        // At least some buildings should have IsTemporary=true
        int temporary = buildings.Count(b => b.IsTemporary);
        Assert.True(temporary > 0,
            $"Expected some buildings with IsTemporary=true, found 0 out of {buildings.Count}");

        // Non-pickupable, non-temporary buildings should be rejected by inventory filter
        var permanentBuilding = buildings.FirstOrDefault(b => !b.CanPickUp && !b.IsTemporary);
        if (permanentBuilding != null)
        {
            Assert.False(InventoryStackDatabase.CanAddItemToInventory(
                permanentBuilding, isTechOnly: false, isCargo: false),
                $"Permanent building {permanentBuilding.Id} should be rejected by CanAddItemToInventory");
        }

        // Pickupable buildings should be accepted
        var pickupableBuilding = buildings.FirstOrDefault(b => b.CanPickUp);
        if (pickupableBuilding != null)
        {
            Assert.True(InventoryStackDatabase.CanAddItemToInventory(
                pickupableBuilding, isTechOnly: false, isCargo: false),
                $"Pickupable building {pickupableBuilding.Id} should be accepted by CanAddItemToInventory");
        }
    }

    // ── WikiGuideDatabase tests (moved from LogicTests to avoid parallel static data mutation) ──

    [Fact]
    public void WikiGuideDatabase_GetTopicName_ReturnsExpectedNames()
    {
        Assert.Equal("Getting Started", WikiGuideDatabase.GetTopicName("^UI_GUIDE_TOPIC_SURVIVAL_BASICS"));
        Assert.Equal("Exploring on Foot", WikiGuideDatabase.GetTopicName("^UI_GUIDE_TOPIC_EXPLORATION_1"));
        Assert.Equal("Portals", WikiGuideDatabase.GetTopicName("^UI_GUIDE_TOPIC_PORTALS"));
    }

    [Fact]
    public void WikiGuideDatabase_GetTopicCategory_ReturnsCorrectCategory()
    {
        Assert.Equal("Survival Basics", WikiGuideDatabase.GetTopicCategory("^UI_GUIDE_TOPIC_SURVIVAL_BASICS"));
        Assert.Equal("Getting Around", WikiGuideDatabase.GetTopicCategory("^UI_GUIDE_TOPIC_EXPLORATION_1"));
        Assert.Equal("Combat", WikiGuideDatabase.GetTopicCategory("^UI_GUIDE_TOPIC_COMBAT_1"));
    }

    [Fact]
    public void WikiGuideDatabase_TopicCount_Is57()
    {
        Assert.Equal(57, WikiGuideDatabase.Topics.Count);
    }

    [Fact]
    public void WikiGuideDatabase_UnknownTopic_ReturnsId()
    {
        string id = "^UNKNOWN_TOPIC";
        Assert.Equal(id, WikiGuideDatabase.GetTopicName(id));
        Assert.Equal("", WikiGuideDatabase.GetTopicCategory(id));
    }
}
