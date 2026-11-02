using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for the unified catalogue item list and its separate Known/Seen state.
/// </summary>
[Collection("MutableStaticDatabases")]
public class CatalogueItemCatalogTests
{
    [Fact]
    public void TechnologyCatalog_UsesCompletionTotalsAndValidExtras()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Technology, new JsonObject(), null, db, pack);

        var (have, total) = catalog.GetCatalogueCompletion();
        Assert.Equal(205, total);
        Assert.Equal(0, have);
        Assert.Contains(catalog.Entries, entry => entry.IsCatalogue);
        Assert.True(catalog.Entries.Count > total);
        Assert.DoesNotContain(catalog.Entries, entry => entry.Id == "OBSOLETE");
    }

    [Fact]
    public void ProductCatalog_AddsExpeditionNumberForSeasonRewards()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        RewardDatabase.LoadFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, new JsonObject(), null, db, pack);

        var entry = catalog.Entries.FirstOrDefault(e => e.ExpeditionNumber.HasValue);
        Assert.NotNull(entry);
        Assert.True(entry.ExpeditionNumber > 0);
    }

    [Fact]
    public void ProductCatalog_IncludesNonCatalogueValidItems()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, new JsonObject(), null, db, pack);

        var grav = catalog.Entries.Single(entry => entry.Id == "GRAV_FLOOR");
        Assert.False(grav.IsCatalogue);
        Assert.Equal("BuildingPart", db.GetItem("GRAV_FLOOR")?.ProductCategory);
        Assert.DoesNotContain(catalog.Entries, entry =>
            string.Equals(db.GetItem(entry.Id)?.SourceTable, "Substance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProductCatalog_ExcludesNonBuildingSpecialShopItems()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, new JsonObject(), null, db, pack);

        Assert.DoesNotContain(catalog.Entries, entry => entry.Id == "GRUB_HAT");
        var expdFlag = catalog.Entries.Single(entry => entry.Id == "EXPD_FLAG_06E");
        Assert.True(expdFlag.CanAddKnown);
        Assert.True(expdFlag.Item?.IsBuilding);
        Assert.True(expdFlag.IsCatalogue);
    }

    [Fact]
    public void SpecialCatalog_IncludesNonCatalogueSpecialShopItems()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Special, new JsonObject(), null, db, pack);

        var grubHat = catalog.Entries.Single(entry => entry.Id == "GRUB_HAT");
        Assert.False(grubHat.IsCatalogue);
        Assert.Equal("SpecialShop", db.GetItem("GRUB_HAT")?.TradeCategory);
        Assert.Equal(652, catalog.GetCatalogueCompletion().Total);
    }

    [Fact]
    public void SetKnown_TechnologyWritesKnownTechAndSeenTechnologies()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Technology, playerState, userSettings, db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "HYPERDRIVE");
        Assert.True(catalog.SetKnown(entry, true));
        Assert.True(catalog.IsKnown(entry));
        Assert.True(ArrayContains(playerState.GetArray("KnownTech")!, "^HYPERDRIVE"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenTechnologies")!, "^HYPERDRIVE"));

        Assert.True(catalog.SetKnown(entry, false));
        Assert.False(ArrayContains(playerState.GetArray("KnownTech")!, "^HYPERDRIVE"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenTechnologies")!, "^HYPERDRIVE"));
        Assert.True(catalog.SetSeen(entry, false));
        Assert.False(ArrayContains(userSettings.GetArray("SeenTechnologies")!, "^HYPERDRIVE"));
    }

    [Fact]
    public void SetKnown_ProductWritesKnownProductsAndSeenProducts()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, playerState, userSettings, db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "BASE_AQUARIUM");
        Assert.True(catalog.SetKnown(entry, true));
        Assert.True(catalog.IsKnown(entry));
        Assert.True(ArrayContains(playerState.GetArray("KnownProducts")!, "^BASE_AQUARIUM"));
        Assert.Null(playerState.GetArray("KnownSpecials"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^BASE_AQUARIUM"));

        Assert.True(catalog.SetKnown(entry, false));
        Assert.False(catalog.IsKnown(entry));
        Assert.False(ArrayContains(playerState.GetArray("KnownProducts")!, "^BASE_AQUARIUM"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^BASE_AQUARIUM"));
    }

    [Fact]
    public void SetKnown_SpecialNonBuildingWritesKnownSpecialsSeenAndUnlocked()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Special, playerState, userSettings, db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "GRUB_HAT");
        Assert.True(catalog.SetKnown(entry, true));
        Assert.True(catalog.IsKnown(entry));
        Assert.True(ArrayContains(playerState.GetArray("KnownSpecials")!, "^GRUB_HAT"));
        Assert.Null(playerState.GetArray("KnownProducts"));
        Assert.True(ArrayContains(userSettings.GetArray("UnlockedSpecials")!, "GRUB_HAT"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^GRUB_HAT"));

        Assert.True(catalog.SetKnown(entry, false));
        Assert.False(ArrayContains(playerState.GetArray("KnownSpecials")!, "^GRUB_HAT"));
        Assert.True(ArrayContains(userSettings.GetArray("UnlockedSpecials")!, "GRUB_HAT"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^GRUB_HAT"));
    }

    [Fact]
    public void SetKnown_SpecialBuildingWritesBothKnownArrays()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Special, playerState, userSettings, db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "EXPD_FLAG_06E");
        Assert.True(entry.Item?.IsBuilding);
        Assert.True(catalog.SetKnown(entry, true));
        Assert.True(ArrayContains(playerState.GetArray("KnownSpecials")!, "^EXPD_FLAG_06E"));
        Assert.True(ArrayContains(playerState.GetArray("KnownProducts")!, "^EXPD_FLAG_06E"));
        Assert.True(ArrayContains(userSettings.GetArray("UnlockedSpecials")!, "EXPD_FLAG_06E"));
    }

    [Fact]
    public void ProductCatalog_LegacySpecialOnlyEntryIsRemovableButNotReAddable()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var knownProducts = new JsonArray();
        knownProducts.Add("^JETS_WORM");
        playerState.Set("KnownProducts", knownProducts);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, playerState, new JsonObject(), db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "JETS_WORM");
        Assert.False(entry.CanAddKnown);
        Assert.True(catalog.IsKnown(entry));
        Assert.True(catalog.SetKnown(entry, false));
        Assert.False(ArrayContains(playerState.GetArray("KnownProducts")!, "^JETS_WORM"));
        Assert.False(catalog.SetKnown(entry, true));
        Assert.False(ArrayContains(playerState.GetArray("KnownProducts")!, "^JETS_WORM"));
    }

    [Fact]
    public void SetSeen_SpecialWritesSeenProductsOnly()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Special, playerState, userSettings, db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "GRUB_HAT");
        Assert.True(catalog.SetSeen(entry, true));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^GRUB_HAT"));
        Assert.Null(playerState.GetArray("KnownSpecials"));
        Assert.Null(playerState.GetArray("KnownProducts"));
    }

    [Fact]
    public void SetKnownBulk_UpdatesAllSelectedEntriesInOnePass()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, playerState, userSettings, db, pack);

        var entries = new[]
        {
            catalog.Entries.Single(e => e.Id == "GRAV_FLOOR"),
            catalog.Entries.Single(e => e.Id == "BASE_AQUARIUM"),
        };

        Assert.Equal(2, catalog.SetKnown(entries, true));
        Assert.True(catalog.IsKnown(entries[0]));
        Assert.True(catalog.IsKnown(entries[1]));
        Assert.True(ArrayContains(playerState.GetArray("KnownProducts")!, "^GRAV_FLOOR"));
        Assert.True(ArrayContains(playerState.GetArray("KnownProducts")!, "^BASE_AQUARIUM"));
        Assert.Null(playerState.GetArray("KnownSpecials"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^GRAV_FLOOR"));
        Assert.True(ArrayContains(userSettings.GetArray("SeenProducts")!, "^BASE_AQUARIUM"));
    }

    [Fact]
    public void SetKnownBulk_RemovesPartialEntries()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var knownSpecials = new JsonArray();
        knownSpecials.Add("^BASE_AQUARIUM");
        playerState.Set("KnownSpecials", knownSpecials);
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Special, playerState, new JsonObject(), db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "BASE_AQUARIUM");
        Assert.True(catalog.IsPartiallyKnown(entry));
        Assert.False(catalog.IsKnown(entry));

        Assert.Equal(1, catalog.SetKnown(new[] { entry }, false));
        Assert.False(ArrayContains(playerState.GetArray("KnownSpecials")!, "^BASE_AQUARIUM"));
    }

    [Fact]
    public void SetKnown_ProductNeverWritesSeenSubstances()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();
        var catalog = new CatalogueItemCatalog(CatalogueItemKind.Product, playerState, userSettings, db, pack);

        var entry = catalog.Entries.Single(e => e.Id == "GRAV_FLOOR");
        Assert.True(catalog.SetKnown(entry, true));
        Assert.Null(userSettings.GetArray("SeenSubstances"));
    }

    [Fact]
    public void CompleteCatalogue_WritesAllSeenSubstancesAndClearsMissingCount()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();

        Assert.True(CatalogueCompletionLogic.CountMissingCatalogue(playerState, userSettings, db, pack) > 0);
        Assert.True(CatalogueCompletionLogic.CompleteCatalogue(playerState, userSettings, db, pack) > 0);
        Assert.Equal(pack.SeenSubstances.Count, userSettings.GetArray("SeenSubstances")!.Length);
        Assert.Equal(0, CatalogueCompletionLogic.CountMissingCatalogue(playerState, userSettings, db, pack));
    }

    [Fact]
    public void CompleteCatalogue_WritesBuildingSpecialsToBothButNotSpecialOnly()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();
        var userSettings = new JsonObject();

        Assert.True(CatalogueCompletionLogic.CompleteCatalogue(
            playerState, userSettings, db, pack) > 0);

        Assert.True(ArrayContains(playerState.GetArray("KnownSpecials")!, "^JETS_WORM"));
        Assert.False(ArrayContains(playerState.GetArray("KnownProducts")!, "^JETS_WORM"));
        Assert.True(ArrayContains(playerState.GetArray("KnownSpecials")!, "^EXPD_FLAG_06E"));
        Assert.True(ArrayContains(playerState.GetArray("KnownProducts")!, "^EXPD_FLAG_06E"));
    }

    [Fact]
    public void CatalogueCompletionSet_ContainsNoProductInSeenSubstances()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        var pack = new CatalogueDatabase(jsonDir);

        foreach (string id in pack.SeenSubstances)
        {
            Assert.Equal("Substance", db.GetItem(id)?.SourceTable);
        }
    }

    private static bool ArrayContains(JsonArray array, string value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (string.Equals(array.GetString(i), value, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
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
}
