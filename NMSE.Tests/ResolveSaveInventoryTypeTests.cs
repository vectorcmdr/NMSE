using NMSE.Data;
using NMSE.IO;

namespace NMSE.Tests;

public class ResolveSaveInventoryTypeTests
{
    [Theory]
    [InlineData("Technology", false, "Technology")]
    [InlineData("Upgrades", false, "Product")]
    [InlineData("Technology Module", false, "Product")]
    [InlineData("Constructed Technology", false, "Product")]
    [InlineData("Others", false, "Product")]
    [InlineData("Raw Materials", false, "Substance")]
    [InlineData("Products", false, "Product")]
    public void NonTechInventory_UsesStandardMapping(string itemType, bool isTech, string expected)
    {
        Assert.Equal(expected, InventoryStackDatabase.ResolveSaveInventoryType(itemType, isTech));
    }

    [Theory]
    [InlineData("Technology", true, "Technology")]
    [InlineData("Upgrades", true, "Product")]
    [InlineData("Technology Module", true, "Product")]
    [InlineData("Constructed Technology", true, "Product")]
    [InlineData("Others", true, "Product")]
    [InlineData("Products", true, "Product")]
    [InlineData("Raw Materials", true, "Substance")]
    public void TechInventory_UsesNativeInventoryType(string itemType, bool isTech, string expected)
    {
        Assert.Equal(expected, InventoryStackDatabase.ResolveSaveInventoryType(itemType, isTech));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsConstructedTechnology()
    {
        // "Constructed Technology" items are Products (e.g. ACCESS3).
        // They are NOT accepted in tech-only inventories unless they have a TechnologyCategory.
        var item = new GameItem { Id = "UT_SHIPMINI", ItemType = "Constructed Technology", Category = "AllShips", TechnologyCategory = "AllShips" };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_AcceptsOthersWithTechCategory()
    {
        var item = new GameItem
        {
            Id = "T_SHIP_RAINBOW",
            ItemType = "Others",
            Category = "AllShips",
            TechnologyCategory = "AllShips"
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void CanAddItem_TechOnly_RejectsOthersWithoutTechCategory()
    {
        var item = new GameItem { Id = "TEST", ItemType = "Others", Category = "Curiosity" };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_SubstanceSourceTable_AlwaysResolvesAsSubstance()
    {
        // Substances in non-standard JSON files (e.g. Others.json) must resolve
        // as Substance regardless of the target inventory context.
        var item = new GameItem
        {
            Id = "^SWARMDUST",
            ItemType = "Others",
            SourceTable = "Substance"
        };
        Assert.Equal("Substance", InventoryStackDatabase.ResolveInventoryTypeForItem(item));
        Assert.Equal("Substance", InventoryStackDatabase.ResolveInventoryTypeForItem(item, isTechInventory: true));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_TechnologySourceTable_ResolvesAsTechnologyInTechInventory()
    {
        // Technology items in non-standard JSON files must resolve as Technology
        // when the target inventory is tech-only.
        var item = new GameItem
        {
            Id = "^S22_LINK",
            ItemType = "Others",
            SourceTable = "Technology"
        };
        Assert.Equal("Technology", InventoryStackDatabase.ResolveInventoryTypeForItem(item, isTechInventory: true));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_TechnologySourceTable_ResolvesAsProductInCargoInventory()
    {
        // Technology items in non-standard JSON files must resolve as Product
        // when the target inventory is cargo (non-tech).
        var item = new GameItem
        {
            Id = "^S22_LINK",
            ItemType = "Others",
            SourceTable = "Technology"
        };
        Assert.Equal("Product", InventoryStackDatabase.ResolveInventoryTypeForItem(item, isTechInventory: false));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_EmptySourceTable_FallsThroughToItemType()
    {
        // Items with no SourceTable should fall through to standard ItemType mapping.
        var item = new GameItem
        {
            Id = "TEST",
            ItemType = "Others",
            SourceTable = ""
        };
        Assert.Equal("Product", InventoryStackDatabase.ResolveInventoryTypeForItem(item));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_ChargeValueOverrideTakesPrecedence()
    {
        // Items with ChargeValue > 0 from tech source files must resolve as Technology
        // regardless of SourceTable.
        var item = new GameItem
        {
            Id = "HDRIVEBOOST",
            ItemType = "Technology",
            SourceTable = "Technology",
            ChargeValue = 25
        };
        Assert.Equal("Technology", InventoryStackDatabase.ResolveInventoryTypeForItem(item));
    }

    [Fact]
    public void CanAddItem_SubstanceInOthersJson_AcceptedByCargoInventory()
    {
        var item = new GameItem
        {
            Id = "^SWARMDUST",
            ItemType = "Others",
            Category = "Substance",
            SourceTable = "Substance"
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Theory]
    [InlineData("GAS1")]
    [InlineData("GAS2")]
    [InlineData("GAS3")]
    [InlineData("GAS4")]
    [InlineData("^GAS1")]
    public void ResolveInventoryTypeForItem_AtmosphericGases_AlwaysResolveAsSubstance(string itemId)
    {
        // The four atmospheric gases are entries in the game's substance table and
        // must be written to saves with InventoryType "Substance".  This guard must
        // hold even if the item database classifies them otherwise (e.g. as Products,
        // as they historically were due to extractor categorisation).
        var item = new GameItem
        {
            Id = itemId,
            ItemType = "Products",
            SourceTable = ""
        };
        Assert.Equal("Substance", InventoryStackDatabase.ResolveInventoryTypeForItem(item));
        Assert.Equal("Substance", InventoryStackDatabase.ResolveInventoryTypeForItem(item, isTechInventory: true));
    }

    [Fact]
    public void CanAddItem_AtmosphericGas_AcceptedByCargoInventory()
    {
        var item = new GameItem
        {
            Id = "GAS1",
            ItemType = "Products",
            Category = "Earth",
            SourceTable = "Substance"
        };
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void CanAddItem_TechnologyInOthersJson_RejectedByCargoInventory()
    {
        var item = new GameItem
        {
            Id = "^S22_LINK",
            ItemType = "Others",
            Category = "Technology",
            SourceTable = "Technology"
        };
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_ProductSourceTableWithCharge_ResolvesAsProduct()
    {
        // SUBFUEL is a product fuel cell. Its ChargeValue is gameplay data, not
        // evidence that it is a technology item.
        var item = new GameItem
        {
            Id = "SUBFUEL",
            ItemType = "Exocraft",
            SourceTable = "Product",
            ChargeValue = 400,
            MaxStackSize = 4,
            ProductCategory = "Consumable"
        };

        Assert.Equal("Product", InventoryStackDatabase.ResolveInventoryTypeForItem(item));
        Assert.Equal("Product", InventoryStackDatabase.ResolveInventoryTypeForItem(item, isTechInventory: true));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
        Assert.False(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: true, isCargo: false));
        Assert.Equal(40, InventoryStackDatabase.GetMaxAmount(item, "Product", "Vehicle"));
    }

    [Fact]
    public void ResolveInventoryTypeForItem_ProductSourceTableInTechnologyJson_ResolvesAsProduct()
    {
        // Products can live in Technology.json (e.g. BIGGS_POI_LOC).  SourceTable
        // must win over the JSON file's ItemType.
        var item = new GameItem
        {
            Id = "BIGGS_POI_LOC",
            ItemType = "Technology",
            SourceTable = "Product"
        };

        Assert.Equal("Product", InventoryStackDatabase.ResolveInventoryTypeForItem(item));
        Assert.True(InventoryStackDatabase.CanAddItemToInventory(item, isTechOnly: false, isCargo: true));
    }
}
