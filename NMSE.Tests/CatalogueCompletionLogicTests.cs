using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for <see cref="CatalogueCompletionLogic"/> and the verified
/// <see cref="CatalogueDatabase"/> loader: id completion, word groups, fishing records,
/// wonders, discovery stats, account Seen arrays, Collected Knowledge and pack integration.
/// </summary>
[Collection("MutableStaticDatabases")]
public class CatalogueCompletionLogicTests
{
    private static JsonObject NewPlayerState() => new();

    private static double AsDouble(object? value) => value is double d ? d : 0;

    // --- AddMissingIds / GetCompletion ---

    [Fact]
    public void AddMissingIds_CreatesArray_AndAddsCaretPrefix()
    {
        var playerState = NewPlayerState();

        int added = CatalogueCompletionLogic.AddMissingIds(
            playerState, "KnownProducts", new[] { "AMMO", "NANOTUBES" });

        Assert.Equal(2, added);
        var array = playerState.GetArray("KnownProducts")!;
        Assert.Equal(2, array.Length);
        Assert.Equal("^AMMO", array.GetString(0));
        Assert.Equal("^NANOTUBES", array.GetString(1));
    }

    [Fact]
    public void AddMissingIds_SkipsExisting_CaseInsensitive()
    {
        var playerState = NewPlayerState();
        var existing = new JsonArray();
        existing.Add("^ammo");
        playerState.Set("KnownProducts", existing);

        int added = CatalogueCompletionLogic.AddMissingIds(
            playerState, "KnownProducts", new[] { "AMMO", "NANOTUBES" });

        Assert.Equal(1, added);
        Assert.Equal(2, existing.Length);
        Assert.Equal("^NANOTUBES", existing.GetString(1));
    }

    [Fact]
    public void AddMissingIds_DedupesPackIds()
    {
        var playerState = NewPlayerState();

        int added = CatalogueCompletionLogic.AddMissingIds(
            playerState, "KnownTech", new[] { "BOLT", "bolt", "^BOLT", "BOLT" });

        Assert.Equal(1, added);
        Assert.Equal(1, playerState.GetArray("KnownTech")!.Length);
    }

    [Fact]
    public void AddMissingIds_HandlesObjectEntries()
    {
        var playerState = NewPlayerState();
        var existing = new JsonArray();
        var entry = new JsonObject();
        entry.Set("Id", "^AMMO");
        existing.Add(entry);
        playerState.Set("KnownProducts", existing);

        int added = CatalogueCompletionLogic.AddMissingIds(
            playerState, "KnownProducts", new[] { "AMMO", "NANOTUBES" });

        Assert.Equal(1, added);
    }

    [Fact]
    public void GetCompletion_CountsOnlyUniquePackIds()
    {
        var playerState = NewPlayerState();
        var array = new JsonArray();
        array.Add("^AMMO");
        array.Add("^EXTRA_NOT_IN_PACK");
        playerState.Set("KnownProducts", array);

        var (have, total) = CatalogueCompletionLogic.GetCompletion(
            playerState, "KnownProducts", new[] { "AMMO", "NANOTUBES", "ammo" });

        Assert.Equal(1, have);
        Assert.Equal(2, total);
    }

    [Fact]
    public void GetCombinedCompletion_RequiresSaveAndAccountEntry()
    {
        var playerState = NewPlayerState();
        var array = new JsonArray();
        array.Add("^AMMO");
        array.Add("^NANOTUBES");
        playerState.Set("KnownProducts", array);

        var settings = new JsonObject();
        var seen = new JsonArray();
        seen.Add("^AMMO");
        settings.Set("SeenProducts", seen);

        var (have, total) = CatalogueCompletionLogic.GetCombinedCompletion(
            playerState, "KnownProducts", settings, "SeenProducts", new[] { "AMMO", "NANOTUBES", "CARBON" });

        Assert.Equal(1, have); // AMMO is in both lists, NANOTUBES only in the save array
        Assert.Equal(3, total);

        var (withoutAccount, _) = CatalogueCompletionLogic.GetCombinedCompletion(
            playerState, "KnownProducts", null, null, new[] { "AMMO", "NANOTUBES", "CARBON" });
        Assert.Equal(2, withoutAccount);
    }

    // --- Word groups ---

    [Fact]
    public void AddMissingWordGroups_AddsGroupWithRaceFlags()
    {
        var playerState = NewPlayerState();
        var packGroups = new[]
        {
            new CatalogueDatabase.WordGroup("TRA_A", new[] { true, false, false })
        };

        var (added, upgraded) = CatalogueCompletionLogic.AddMissingWordGroups(playerState, packGroups);

        Assert.Equal(1, added);
        Assert.Equal(0, upgraded);
        var groups = playerState.GetArray("KnownWordGroups")!;
        Assert.Equal(1, groups.Length);
        var entry = groups.GetObject(0)!;
        Assert.Equal("^TRA_A", entry.GetString("Group"));
        var races = entry.GetArray("Races")!;
        Assert.True(races.GetBool(0));
        Assert.False(races.GetBool(1));
    }

    [Fact]
    public void AddMissingWordGroups_UpgradesExistingFlags()
    {
        var playerState = NewPlayerState();
        var existing = new JsonArray();
        var entry = new JsonObject();
        entry.Set("Group", "^TRA_A");
        var races = new JsonArray();
        races.Add(false);
        races.Add(true);
        entry.Set("Races", races);
        existing.Add(entry);
        playerState.Set("KnownWordGroups", existing);

        var packGroups = new[]
        {
            new CatalogueDatabase.WordGroup("TRA_A", new[] { true, true, false })
        };

        var (added, upgraded) = CatalogueCompletionLogic.AddMissingWordGroups(playerState, packGroups);

        Assert.Equal(0, added);
        Assert.Equal(1, upgraded);
        Assert.Equal(1, existing.Length);
        var updated = existing.GetObject(0)!.GetArray("Races")!;
        Assert.True(updated.GetBool(0));
        Assert.True(updated.GetBool(1));
        Assert.False(updated.GetBool(2));
    }

    [Fact]
    public void NormalizeRaceFlags_MapsLegacyCompactLayoutByOrdinal()
    {
        var gek = CatalogueDatabase.NormalizeRaceFlags([true, false, false, false, false]);
        Assert.Equal(9, gek.Length);
        Assert.True(gek[0]);

        var atlas = CatalogueDatabase.NormalizeRaceFlags([false, false, false, true, false]);
        Assert.True(atlas[4]);   // Atlas ordinal 4 -> index 4, not 3
        Assert.False(atlas[3]);

        var autophage = CatalogueDatabase.NormalizeRaceFlags([false, false, false, false, true]);
        Assert.True(autophage[8]); // Autophage ordinal 8 -> index 8, not 4
        Assert.False(autophage[4]);
    }

    [Fact]
    public void AddMissingWordGroups_ExpandsLegacyFiveSlotEntry()
    {
        var playerState = NewPlayerState();
        var existing = new JsonArray();
        var entry = new JsonObject();
        entry.Set("Group", "^ATLAS_A");
        var legacy = new JsonArray();
        legacy.Add(false);
        legacy.Add(false);
        legacy.Add(false);
        legacy.Add(true);  // old compact layout: Atlas at index 3
        legacy.Add(false);
        entry.Set("Races", legacy);
        existing.Add(entry);
        playerState.Set("KnownWordGroups", existing);

        var packGroups = new[]
        {
            new CatalogueDatabase.WordGroup("ATLAS_A", [false, false, false, false, true, false, false, false, false])
        };

        var (added, upgraded) = CatalogueCompletionLogic.AddMissingWordGroups(playerState, packGroups);

        Assert.Equal(0, added);
        Assert.Equal(1, upgraded);
        var races = entry.GetArray("Races")!;
        Assert.Equal(9, races.Length);
        Assert.True(races.GetBool(4));
        Assert.False(races.GetBool(3));
    }

    [Fact]
    public void Pack_WordGroups_UseNineSlotOrdinalRaceLayout()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);

        Assert.All(pack.KnownWordGroups, g => Assert.Equal(9, g.Races.Length));

        var atlas = pack.KnownWordGroups.FirstOrDefault(g => string.Equals(g.Group, "ATLAS_A", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(atlas);
        Assert.True(atlas!.Races[4]);
    }

    [Fact]
    public void GetWordPageCompletion_AtlasWordsTrackRaceFlags()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var page = KnowledgeCatalogue.Pages.First(p => p.Id == "atlas_words");
        var playerState = NewPlayerState();

        var (haveBefore, total) = CatalogueCompletionLogic.GetWordPageCompletion(playerState, pack, page);
        Assert.Equal(0, haveBefore);
        Assert.True(total > 0);

        CatalogueCompletionLogic.AddMissingWordGroups(playerState, pack.KnownWordGroups);

        var (haveAfter, _) = CatalogueCompletionLogic.GetWordPageCompletion(playerState, pack, page);
        Assert.Equal(total, haveAfter);
    }

    [Fact]
    public void GetWordGroupCompletion_CountsGroupsPresent()
    {
        var playerState = NewPlayerState();
        var existing = new JsonArray();
        var entry = new JsonObject();
        entry.Set("Group", "^TRA_A");
        entry.Set("Races", new JsonArray());
        existing.Add(entry);
        playerState.Set("KnownWordGroups", existing);

        var packGroups = new[]
        {
            new CatalogueDatabase.WordGroup("TRA_A", new[] { true }),
            new CatalogueDatabase.WordGroup("TRA_B", new[] { true })
        };

        var (have, total) = CatalogueCompletionLogic.GetWordGroupCompletion(playerState, packGroups);

        Assert.Equal(1, have);
        Assert.Equal(2, total);
    }

    // --- Fishing ---

    [Fact]
    public void AddMissingFish_AddsAlignedLists()
    {
        var playerState = NewPlayerState();
        var packFish = new[]
        {
            new CatalogueDatabase.FishEntry("F_JELLYCHILD", 10, 25.5)
        };

        int added = CatalogueCompletionLogic.AddMissingFish(playerState, packFish);

        Assert.Equal(1, added);
        var record = playerState.GetObject("FishingRecord")!;
        var ids = record.GetArray("ProductList")!;
        var counts = record.GetArray("ProductCountList")!;
        var largest = record.GetArray("LargestCatchList")!;
        Assert.Equal(256, ids.Length);
        Assert.Equal(256, counts.Length);
        Assert.Equal(256, largest.Length);
        Assert.Equal("^F_JELLYCHILD", ids.GetString(0));
        Assert.Equal(10, counts.GetInt(0));
        Assert.Equal(25.5, AsDouble(largest.Get(0)));
        Assert.Equal("^", ids.GetString(1));
        Assert.Equal("^", ids.GetString(255));
    }

    [Fact]
    public void AddMissingFish_KeepsExistingSpecies()
    {
        var playerState = NewPlayerState();
        var record = new JsonObject();
        var ids = new JsonArray();
        ids.Add("^F_JELLYCHILD");
        var counts = new JsonArray();
        counts.Add(3);
        var largest = new JsonArray();
        largest.Add(5.0);
        record.Set("ProductList", ids);
        record.Set("ProductCountList", counts);
        record.Set("LargestCatchList", largest);
        playerState.Set("FishingRecord", record);

        int added = CatalogueCompletionLogic.AddMissingFish(playerState, new[]
        {
            new CatalogueDatabase.FishEntry("F_JELLYCHILD", 10, 25.5),
            new CatalogueDatabase.FishEntry("F_OTHER", 1, 2.0)
        });

        Assert.Equal(1, added);
        Assert.Equal(256, ids.Length);
        Assert.Equal("^F_JELLYCHILD", ids.GetString(0));
        Assert.Equal(3, counts.GetInt(0));
        Assert.Equal(5.0, AsDouble(largest.Get(0)));
        Assert.Equal("^F_OTHER", ids.GetString(1));
        Assert.Equal(1, counts.GetInt(1));
        Assert.Equal(2.0, AsDouble(largest.Get(1)));
    }

    [Fact]
    public void AddMissingFish_FillsStubSlotsInPlace()
    {
        var playerState = NewPlayerState();
        var record = new JsonObject();
        var ids = new JsonArray();
        var counts = new JsonArray();
        var largest = new JsonArray();
        for (int i = 0; i < 256; i++)
        {
            ids.Add("^");
            counts.Add(0);
            largest.Add(0.0);
        }
        record.Set("ProductList", ids);
        record.Set("ProductCountList", counts);
        record.Set("LargestCatchList", largest);
        playerState.Set("FishingRecord", record);

        int added = CatalogueCompletionLogic.AddMissingFish(playerState, new[]
        {
            new CatalogueDatabase.FishEntry("F_JELLYCHILD", 10, 25.5)
        });

        Assert.Equal(1, added);
        Assert.Equal(256, ids.Length);
        Assert.Equal("^F_JELLYCHILD", ids.GetString(0));
        Assert.Equal(10, counts.GetInt(0));
        Assert.Equal(25.5, AsDouble(largest.Get(0)));
        Assert.Equal("^", ids.GetString(1));
        Assert.Equal(0, counts.GetInt(1));
        Assert.Equal("^", ids.GetString(255));
    }

    [Fact]
    public void AddMissingFish_RecoversEntriesAppendedBeyondCapacity()
    {
        var playerState = NewPlayerState();
        var record = new JsonObject();
        var ids = new JsonArray();
        var counts = new JsonArray();
        var largest = new JsonArray();
        for (int i = 0; i < 256; i++)
        {
            ids.Add("^");
            counts.Add(0);
            largest.Add(0.0);
        }
        // Simulate the old build: species appended after the pre-allocated slots.
        ids.Add("^F_JELLYCHILD");
        counts.Add(7);
        largest.Add(12.5);
        record.Set("ProductList", ids);
        record.Set("ProductCountList", counts);
        record.Set("LargestCatchList", largest);
        playerState.Set("FishingRecord", record);

        int added = CatalogueCompletionLogic.AddMissingFish(playerState, new[]
        {
            new CatalogueDatabase.FishEntry("F_JELLYCHILD", 10, 25.5),
            new CatalogueDatabase.FishEntry("F_OTHER", 1, 2.0)
        });

        Assert.Equal(2, added);
        Assert.Equal(256, ids.Length);
        Assert.Equal("^F_JELLYCHILD", ids.GetString(0));
        Assert.Equal(10, counts.GetInt(0));
        Assert.Equal("^F_OTHER", ids.GetString(1));
        Assert.Equal(1, counts.GetInt(1));
        Assert.Equal("^", ids.GetString(2));
    }

    [Fact]
    public void AddMissingFish_RaisesFishGlobalStats()
    {
        var playerState = NewPlayerState();

        CatalogueCompletionLogic.AddMissingFish(playerState, new[]
        {
            new CatalogueDatabase.FishEntry("F_JELLYCHILD", 10, 25.5)
        });

        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.True(globals.ContainsKey("FISH_CAUGHT"));
        Assert.Equal(9738, CatalogueCompletionLogic.GetGlobalInt(globals["FISH_CAUGHT"]));
        Assert.True(globals.ContainsKey("FISH_LEGEND"));
        Assert.Equal(1, CatalogueCompletionLogic.GetGlobalInt(globals["FISH_LEGEND"]));
    }

    [Fact]
    public void GetFishingCompletion_CountsSpeciesPresent()
    {
        var playerState = NewPlayerState();
        var record = new JsonObject();
        var ids = new JsonArray();
        ids.Add("^F_JELLYCHILD");
        record.Set("ProductList", ids);
        playerState.Set("FishingRecord", record);

        var (have, total) = CatalogueCompletionLogic.GetFishingCompletion(playerState, new[]
        {
            new CatalogueDatabase.FishEntry("F_JELLYCHILD", 10, 25.5),
            new CatalogueDatabase.FishEntry("F_OTHER", 1, 2.0)
        });

        Assert.Equal(1, have);
        Assert.Equal(2, total);
    }

    // --- Verified pack integration ---

    [Fact]
    public void Pack_LoadsVerifiedTotals()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);

        Assert.True(pack.IsAvailable);
        Assert.Equal(3679, pack.KnownProducts.Count);
        Assert.Equal(392, pack.KnownTech.Count);
        Assert.DoesNotContain("OBSOLETE", pack.KnownTech);
        Assert.Equal(276, pack.KnownSpecials.Count);
        Assert.Equal(1684, pack.KnownRefinerRecipes.Count);
        Assert.Equal(3844, pack.KnownWordGroups.Count);
        Assert.Equal(220, pack.Fishing.Count);
        Assert.Equal(65535, pack.KnownPortalRunes);
    }

    [Fact]
    public void Pack_KnownTechForCompletion_ExcludesBrokenSlotTech()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);

        var completion = pack.KnownTechForCompletion(db);

        // Only real technologies remain: damaged/blocked-slot placeholders and
        // constructed-technology products (which the game drops from KnownTech on load).
        Assert.Equal(205, completion.Count);
        Assert.DoesNotContain("SHIPSLOT_DMG1", completion);
        Assert.DoesNotContain("WEAPSLOT_DMG12", completion);
        Assert.DoesNotContain("SHIPEASY_DMG4", completion);
        Assert.DoesNotContain("WEAPEASY_DMG1", completion);
        Assert.DoesNotContain("WEAPSENT_DMG2", completion);
        Assert.DoesNotContain("OBSOLETE", completion);
        Assert.DoesNotContain("U_HYPER1", completion);   // Product source, not technology
        Assert.Contains("HYPERDRIVE", completion);       // Technology source
        Assert.Contains("UT_STUNDMG", completion); // real stun upgrade, not a placeholder

        // The game flag drives the exclusion: no completion ID may be a broken-slot tech.
        foreach (string id in completion)
            Assert.False(db.GetItem(id)?.BrokenSlotTech == true, $"{id} is a broken-slot placeholder");
    }

    [Fact]
    public void Pack_KnownProductsForCompletion_ExcludesSubstancesAndSpecialOnly()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);

        var completion = pack.KnownProductsForCompletion(db);

        Assert.Equal(3358, completion.Count);
        Assert.DoesNotContain("FUEL1", completion);      // substance
        Assert.DoesNotContain("ASTBELT1", completion);   // substance
        Assert.DoesNotContain("GRUB_HAT", completion);   // non-building SpecialShop
        Assert.Contains("BASE_AQUARIUM", completion);    // building product
        Assert.Contains("EXPD_FLAG_06E", completion);    // SpecialShop building
    }

    [Fact]
    public void Pack_KnownSpecialsForCompletion_IncludesSpecialShopProducts()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);

        var completion = pack.KnownSpecialsForCompletion(db);

        // The residual editor-known list plus every SpecialShop catalogue product.
        Assert.Equal(652, completion.Count);
        Assert.Contains("BASE_AQUARIUM", completion);   // building parts tracked as specials
        Assert.Contains("BASE_FLOWER1", completion);
        Assert.Contains("TRUCK_BODY03", completion);    // exocraft customisation special
        Assert.Contains("SPEC_TITLE11", completion);    // residual special
        Assert.DoesNotContain("FUEL1", completion);     // raw substance, not a special
        Assert.DoesNotContain("IRON", completion);      // ordinary product, not a special

        // The reference tool writes 566 specials; the completion list must cover all of them.
        Assert.True(completion.Count >= 566 && completion.Count >= pack.KnownSpecials.Count);

        // Every product-derived entry must really be a SpecialShop item.
        var fromProducts = completion.Except(pack.KnownSpecials, StringComparer.OrdinalIgnoreCase);
        foreach (string id in fromProducts)
            Assert.Equal("SpecialShop", db.GetItem(id)?.TradeCategory);
    }

    [Fact]
    public void GameItemDatabase_ParsesBrokenSlotTechFlag()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);

        // The flag is serialised as a string ("true"/"false") and must round-trip.
        Assert.True(db.GetItem("^WEAPSLOT_DMG1")?.BrokenSlotTech == true);
        Assert.True(db.GetItem("^SHIPSLOT_DMG1")?.BrokenSlotTech == true);
        Assert.True(db.GetItem("^SHIPEASY_DMG1")?.BrokenSlotTech == true);
        Assert.True(db.GetItem("^WEAPEASY_DMG1")?.BrokenSlotTech == true);
        Assert.True(db.GetItem("^WEAPSENT_DMG1")?.BrokenSlotTech == true);
        Assert.False(db.GetItem("^UT_STUNDMG")?.BrokenSlotTech == true);
        Assert.False(db.GetItem("^OBSOLETE")?.BrokenSlotTech == true);
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

    [Fact]
    public void Pack_TBobbleIdsResolveViaPrefixFallback()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);

        // The extractor drops T_BOBBLE_* in favour of BOBBLE_*; the T_ prefix
        // fallback in GetItem keeps those save IDs resolvable for display.
        var item = db.GetItem("^T_BOBBLE_APOLLO");

        Assert.NotNull(item);
        Assert.Equal("BOBBLE_APOLLO", item!.Id);
    }

    // --- Wonders ---

    private static JsonObject MakeDiscoveryRecord(string discoveryType, string address, string value)
    {
        var vp = new JsonArray();
        vp.Add(value);

        var dd = new JsonObject();
        dd.Set("DT", discoveryType);
        dd.Set("UA", address);
        dd.Set("VP", vp);

        var record = new JsonObject();
        record.Set("DD", dd);
        return record;
    }

    private static JsonObject MakeDiscoverySurrogate(string wonderKey, string discoveryType)
    {
        var surrogate = new JsonObject();

        var gid = new JsonArray();
        gid.Add("0x1111");
        gid.Add("0x2222");
        surrogate.Set("GenerationID", gid);
        surrogate.Set("WonderStatValue", 12.5);

        var species = MakeDiscoveryRecord(discoveryType, "0xAAAA", "0xBBBB");
        if (discoveryType != "Planet")
            surrogate.Set(discoveryType, species);

        var planet = MakeDiscoveryRecord("Planet", "0xCCCC", "0xDDDD");
        surrogate.Set("Planet", planet);

        var planetStats = new JsonObject();
        planetStats.Set("GroupId", "^PLANET_STATS");
        planetStats.Set("Address", "0xCCCC");
        surrogate.Set("PlanetStats", planetStats);

        _ = wonderKey;
        return surrogate;
    }

    [Fact]
    public void GetWonderCompletion_DiscoveryCountsLiveFilledSlots()
    {
        var playerState = new JsonObject();
        var slots = new JsonArray();

        var filled = new JsonObject();
        var gid = new JsonArray();
        gid.Add("0x1");
        gid.Add("0x2");
        filled.Set("GenerationID", gid);
        slots.Add(filled);

        var empty = new JsonObject();
        var zeroGid = new JsonArray();
        zeroGid.Add(0);
        zeroGid.Add(0);
        empty.Set("GenerationID", zeroGid);
        slots.Add(empty);

        playerState.Set("WonderPlanetRecords", slots);

        var fallback = new Dictionary<string, IReadOnlyList<JsonObject>>(StringComparer.OrdinalIgnoreCase)
        {
            ["WonderPlanetRecords"] = new[] { MakeDiscoverySurrogate("WonderPlanetRecords", "Planet"), MakeDiscoverySurrogate("WonderPlanetRecords", "Planet") }
        };

        var (have, total) = CatalogueCompletionLogic.GetWonderCompletion(playerState, "WonderPlanetRecords", null, fallback);

        Assert.Equal(1, have);
        Assert.Equal(2, total);
    }

    [Fact]
    public void FillWonderSlots_DiscoversDefersDependencyInjectionToSave()
    {
        var saveRoot = new JsonObject();
        var playerState = new JsonObject();

        var surrogate = MakeDiscoverySurrogate("WonderCreatureRecords", "Animal");
        var fallback = new Dictionary<string, IReadOnlyList<JsonObject>>(StringComparer.OrdinalIgnoreCase)
        {
            ["WonderCreatureRecords"] = new[] { surrogate }
        };

        int filled = CatalogueCompletionLogic.FillWonderSlots(saveRoot, playerState, "WonderCreatureRecords", null, fallback);
        Assert.Equal(15, filled); // creature slots target 15; the single surrogate repeats

        var slots = playerState.GetArray("WonderCreatureRecords")!;
        Assert.Equal(15, slots.Length);
        Assert.True(CatalogueCompletionLogic.IsWonderSlotFilled(slots.GetObject(0)));
        Assert.True(CatalogueCompletionLogic.IsWonderSlotFilled(slots.GetObject(14)));

        // Dependencies are only injected when the save is written.
        Assert.Null(saveRoot.GetObject("DiscoveryManagerData"));

        int injected = CatalogueCompletionLogic.InjectWonderDependencies(saveRoot, playerState, fallback);
        Assert.Equal(15, injected);

        var records = saveRoot.GetObject("DiscoveryManagerData")!.GetObject("DiscoveryData-v1")!.GetObject("Store")!.GetArray("Record")!;
        Assert.Equal(2, records.Length); // Animal + Planet

        var stats = playerState.GetArray("Stats")!;
        Assert.Equal(1, stats.Length);
        Assert.Equal("^PLANET_STATS", stats.GetObject(0)!.GetString("GroupId"));

        // Repeated injection is idempotent.
        CatalogueCompletionLogic.InjectWonderDependencies(saveRoot, playerState, fallback);
        Assert.Equal(2, records.Length);
        Assert.Equal(1, stats.Length);
    }

    [Fact]
    public void FillWonderSlots_PackRecordsCopiedIntoEmptySlots()
    {
        var playerState = new JsonObject();
        var packRecord = new JsonObject();
        var gid = new JsonArray();
        gid.Add("0x4F4F");
        gid.Add("0x5245");
        packRecord.Set("GenerationID", gid);
        packRecord.Set("WonderStatValue", 681324.0);

        int filled = CatalogueCompletionLogic.FillWonderSlots(new JsonObject(), playerState, "WonderTreasureRecords",
            new[] { packRecord }, new Dictionary<string, IReadOnlyList<JsonObject>>());

        Assert.Equal(1, filled);
        var slots = playerState.GetArray("WonderTreasureRecords")!;
        Assert.True(slots.GetObject(0)!.GetBool("SeenInFrontend"));
    }

    [Fact]
    public void ClearWonderSlot_ResetsDiscoverySlot()
    {
        var playerState = new JsonObject();
        var slots = new JsonArray();
        var record = new JsonObject();
        var gid = new JsonArray();
        gid.Add("0x1");
        gid.Add("0x2");
        record.Set("GenerationID", gid);
        slots.Add(record);
        playerState.Set("WonderPlanetRecords", slots);

        Assert.True(CatalogueCompletionLogic.ClearWonderSlot(playerState, "WonderPlanetRecords", 0));
        Assert.False(CatalogueCompletionLogic.IsWonderSlotFilled(slots.GetObject(0)));
        Assert.False(CatalogueCompletionLogic.ClearWonderSlot(playerState, "WonderPlanetRecords", 0));
    }

    // --- Discovery stats ---

    [Fact]
    public void GlobalStatHelpers_CreateRaiseAndClear()
    {
        var playerState = new JsonObject();

        Assert.True(CatalogueCompletionLogic.SetGlobalStatAtLeast(playerState, "DISC_PLANETS", 794));
        var map = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.True(map.ContainsKey("DISC_PLANETS"));
        Assert.Equal(794, CatalogueCompletionLogic.GetGlobalInt(map["DISC_PLANETS"]));

        // Already at target: no change.
        Assert.False(CatalogueCompletionLogic.SetGlobalStatAtLeast(playerState, "DISC_PLANETS", 794));

        // Negative values are the game's "full" marker.
        CatalogueCompletionLogic.SetGlobalInt(map["DISC_PLANETS"], -1);
        Assert.False(CatalogueCompletionLogic.SetGlobalStatAtLeast(playerState, "DISC_PLANETS", 794));
        Assert.True(CatalogueCompletionLogic.IsGlobalStatComplete(-1, 794));

        Assert.True(CatalogueCompletionLogic.ClearGlobalStat(playerState, "DISC_PLANETS"));
        Assert.Equal(0, CatalogueCompletionLogic.GetGlobalInt(map["DISC_PLANETS"]));
    }

    [Fact]
    public void GetGlobalStatsCompletion_CountsCompleteTargets()
    {
        var playerState = new JsonObject();
        CatalogueCompletionLogic.SetGlobalStatAtLeast(playerState, "A", 10);
        CatalogueCompletionLogic.SetGlobalStatAtLeast(playerState, "B", 10);
        var map = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        CatalogueCompletionLogic.SetGlobalInt(map["B"], 99);

        var targets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 10,
            ["B"] = 10,
            ["C"] = 10,
        };

        var (have, total) = CatalogueCompletionLogic.GetGlobalStatsCompletion(playerState, targets);
        Assert.Equal(2, have);
        Assert.Equal(3, total);
    }

    // --- Account Seen arrays ---

    [Fact]
    public void AddMissingSeen_AndRemoveSeen()
    {
        var userSettings = new JsonObject();
        var ids = new[] { "FOS_BI_BODY_AA", "FOS_BI_BODY_AB", "FOS_BI_BODY_AC" };

        var (have, total) = CatalogueCompletionLogic.GetSeenCompletion(userSettings, "SeenProducts", ids);
        Assert.Equal(0, have);
        Assert.Equal(3, total);

        int added = CatalogueCompletionLogic.AddMissingSeen(userSettings, "SeenProducts", ids);
        Assert.Equal(3, added);

        (have, total) = CatalogueCompletionLogic.GetSeenCompletion(userSettings, "SeenProducts", ids);
        Assert.Equal(3, have);
        Assert.Equal("^FOS_BI_BODY_AA", userSettings.GetArray("SeenProducts")!.GetString(0));

        int removed = CatalogueCompletionLogic.RemoveSeen(userSettings, "SeenProducts", new[] { "FOS_BI_BODY_AB" });
        Assert.Equal(1, removed);
        Assert.Equal(2, userSettings.GetArray("SeenProducts")!.Length);
    }

    // --- Collected Knowledge ---

    [Fact]
    public void LastSeen_RoundTrips()
    {
        var playerState = new JsonObject();

        Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState, 0, 0));
        Assert.True(CatalogueCompletionLogic.UpsertLastSeen(playerState, 0, 0, 29));
        Assert.Equal(29, CatalogueCompletionLogic.GetLastSeen(playerState, 0, 0));
        Assert.False(CatalogueCompletionLogic.UpsertLastSeen(playerState, 0, 0, 29));
        Assert.True(CatalogueCompletionLogic.RemoveLastSeen(playerState, 0, 0));
        Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState, 0, 0));
    }

    [Fact]
    public void ApplyAndClearKnowledgePage_CounterPage()
    {
        var playerState = new JsonObject();
        var pack = new CatalogueDatabase.StoryCompleterPack(21, [], []);
        var page = KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_journey");

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePage(playerState, page, pack));
        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.Equal(51, CatalogueCompletionLogic.GetGlobalInt(globals["CORE_LORE"]));
        Assert.Equal(51, CatalogueCompletionLogic.GetLastSeen(playerState, page.Slot, page.PageIndex));

        Assert.True(CatalogueCompletionLogic.ClearKnowledgePage(playerState, page));
        Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState, page.Slot, page.PageIndex));
        Assert.Equal(0, CatalogueCompletionLogic.GetGlobalInt(globals["CORE_LORE"]));
    }

    [Fact]
    public void ApplyAndClearKnowledgePage_InteractionPatchesSii()
    {
        var playerState = new JsonObject();
        var pack = new CatalogueDatabase.StoryCompleterPack(21, [], []);
        var page = KnowledgeCatalogue.Pages.Single(p => p.Id == "gek_mono");

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePage(playerState, page, pack));
        var (races, looped) = CatalogueCompletionLogic.GetSiiStatus(playerState, 10);
        foreach (int race in KnowledgeCatalogue.DefaultRaces)
        {
            Assert.Equal(20, races[race]);
            Assert.True(looped[race]);
        }

        Assert.True(CatalogueCompletionLogic.ClearKnowledgePage(playerState, page));
        (races, looped) = CatalogueCompletionLogic.GetSiiStatus(playerState, 10);
        Assert.All(races, r => Assert.Equal(0, r));
        Assert.All(looped, l => Assert.False(l));
    }

    [Fact]
    public void GetKnowledgeStatus_TracksPageProgress()
    {
        var playerState = new JsonObject();
        var page = KnowledgeCatalogue.Pages.Single(p => p.Id == "atlas_iface");
        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);

        var missing = CatalogueCompletionLogic.GetKnowledgeStatus(playerState, page, globals);
        Assert.Equal("MISSING", missing.Status);

        CatalogueCompletionLogic.UpsertLastSeen(playerState, page.Slot, page.PageIndex, page.TableMax);
        globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        var complete = CatalogueCompletionLogic.GetKnowledgeStatus(playerState, page, globals);
        Assert.Equal("OK", complete.Status);
    }

    [Fact]
    public void GetPageTarget_UsesExtractorStoryEntryCounts()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"nmse_story_targets_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "Catalogue Pack.json"), """
            {
              "KnownRefinerRecipes": [], "KnownWordGroups": [], "Fossils": [],
              "KnownPortalRunes": 65535,
              "CatalogueMaterials": [], "CatalogueBuilding": [], "CatalogueCrafting": [],
              "StoryPages": [
                { "Slot": 5, "Page": 2, "Id": "UI_OVERSEER_TITLE", "Entries": 15, "Stat": "OVERSEER_LORE", "Bitmask": false },
                { "Slot": 0, "Page": 1, "Id": "UI_PORTAL_TRA_LIB_TITLE", "Entries": 12, "Stat": "LIB_TRA_LORE", "Bitmask": true }
              ]
            }
            """);

            var pack = new CatalogueDatabase(dir);
            Assert.Equal(15, pack.GetStoryPageEntryCount(5, 2));

            var counter = KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_overseer");
            Assert.Equal(12, CatalogueCompletionLogic.GetPageTarget(counter, null)); // compiled fallback
            Assert.Equal(14, CatalogueCompletionLogic.GetPageTarget(counter, pack)); // entries - 1

            var bitmask = KnowledgeCatalogue.Pages.Single(p => p.Id == "gek_archive");
            Assert.Equal(1023, CatalogueCompletionLogic.GetPageTarget(bitmask, null));
            Assert.Equal(4095, CatalogueCompletionLogic.GetPageTarget(bitmask, pack)); // (1 << 12) - 1

            // The pack re-aligns the page (compiled slot 5 page 3 -> game page 2) and raises
            // the target, so a value that passes the compiled target is under the pack one.
            var resolved = CatalogueCompletionLogic.ResolvePage(counter, pack);
            Assert.Equal(5, resolved.Slot);
            Assert.Equal(2, resolved.PageIndex);
            Assert.Equal(14, resolved.TableMax);

            var playerState = new JsonObject();
            CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, counter, 13, pack);
            Assert.Equal(13, CatalogueCompletionLogic.GetLastSeen(playerState, resolved.Slot, resolved.PageIndex));
            Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState, counter.Slot, counter.PageIndex));
            var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
            Assert.Equal("UNDER", CatalogueCompletionLogic.GetKnowledgeStatus(playerState, counter, globals, pack).Status);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Pack_StoryPages_AlignEveryCompiledPage()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        if (pack.StoryPages.Count == 0) return; // Pack predates story extraction

        foreach (var page in KnowledgeCatalogue.Pages)
        {
            Assert.False(string.IsNullOrEmpty(page.GameId), $"{page.Id} has no game page ID");
            var entry = pack.FindStoryPage(page.GameId!);
            Assert.NotNull(entry);

            var resolved = CatalogueCompletionLogic.ResolvePage(page, pack);
            Assert.Equal(entry!.Slot, resolved.Slot);
            Assert.Equal(entry.PageIndex, resolved.PageIndex);
        }

        // The current build puts Developer Commentary at page 1 and Under a Rebel Star at 13.
        var devNotes = CatalogueCompletionLogic.ResolvePage(KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_devnotes"), pack);
        Assert.Equal(5, devNotes.Slot);
        Assert.Equal(1, devNotes.PageIndex);

        var baseComp = CatalogueCompletionLogic.ResolvePage(KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_basecomp"), pack);
        Assert.Equal(5, baseComp.Slot);
        Assert.Equal(2, baseComp.PageIndex);

        var pirates = CatalogueCompletionLogic.ResolvePage(KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_pirate"), pack);
        Assert.Equal(5, pirates.Slot);
        Assert.Equal(13, pirates.PageIndex);
        Assert.Equal(7, pirates.TableMax);
    }

    [Fact]
    public void KnowledgeCompleters_ApplyAndClearEverything()
    {
        var playerState = new JsonObject();
        var patches = new[]
        {
            new CatalogueDatabase.SiiPatch(10, new[] { 20, 20, 20, 0, 0, 0, 20, 20, 4 }, new[] { true, true, true, false, false, false, true, true, true }),
            new CatalogueDatabase.SiiPatch(55, new[] { 16, 16, 16, 0, 0, 0, 17, 17, 0 }, new[] { true, true, true, false, false, false, true, true, false }),
        };
        var missions = new[]
        {
            new CatalogueDatabase.MissionCompleter("ATLAS1", 12, 0),
            new CatalogueDatabase.MissionCompleter("ATLAS2", 11, 0),
        };
        var pack = new CatalogueDatabase.StoryCompleterPack(21, patches, missions);

        int changed = CatalogueCompletionLogic.ApplyKnowledgeCompleters(playerState, pack);
        Assert.True(changed > 0);
        Assert.True(playerState.GetBool("BuildersKnown"));
        Assert.True(playerState.GetBool("HasDiscoveredPurpleSystems"));
        Assert.Equal(21, CatalogueCompletionLogic.GetLastSeen(playerState,
            KnowledgeCatalogue.BaseCompPageSlot, KnowledgeCatalogue.BaseCompPageIndex));
        Assert.Equal(39, CatalogueCompletionLogic.GetLastSeen(playerState,
            KnowledgeCatalogue.DevNotesPageSlot, KnowledgeCatalogue.DevNotesPageIndex));

        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.Equal(9, CatalogueCompletionLogic.GetGlobalInt(globals["ATLAS_PATH"]));
        Assert.Equal(38, CatalogueCompletionLogic.GetGlobalInt(globals["DEV_NOTES"])); // highest index, count 39
        Assert.Equal(1, CatalogueCompletionLogic.GetLastSeen(playerState, 0, 3)); // language page marked present

        var (races, _) = CatalogueCompletionLogic.GetSiiStatus(playerState, 55);
        Assert.Equal(17, races[6]);

        int cleared = CatalogueCompletionLogic.ClearKnowledgeCompleters(playerState, pack);
        Assert.True(cleared > 0);
        Assert.False(playerState.GetBool("BuildersKnown"));
        Assert.False(playerState.GetBool("HasDiscoveredPurpleSystems"));
        Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState,
            KnowledgeCatalogue.BaseCompPageSlot, KnowledgeCatalogue.BaseCompPageIndex));
        Assert.Equal(-1, CatalogueCompletionLogic.GetGlobalInt(globals["DEV_NOTES"])); // -1 is the empty marker
        Assert.Equal(0, CatalogueCompletionLogic.GetMissionProgress(playerState, "ATLAS1")); // cleared to not-started
    }

    [Fact]
    public void ApplyKnowledgePage_ReplacesNegativeGlobalWithPositiveTarget()
    {
        var playerState = new JsonObject();
        CatalogueCompletionLogic.SetGlobalStatValue(playerState, "ATLAS_LORE", -1);

        var page = KnowledgeCatalogue.Pages.Single(p => p.Id == "atlas_iface");
        var pack = new CatalogueDatabase.StoryCompleterPack(21, [], []);

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePage(playerState, page, pack));

        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        // The game's counter credits nothing for -1 stats, so the page forces a positive target.
        Assert.Equal(11, CatalogueCompletionLogic.GetGlobalInt(globals["ATLAS_LORE"]));
    }

    [Fact]
    public void GetKnowledgeCompleterStatuses_TreatsMinusOneMissionProgressAsComplete()
    {
        var playerState = new JsonObject();
        var missions = new JsonArray();
        var mission = new JsonObject();
        mission.Set("Mission", "^PIRATES_LORE");
        mission.Set("Progress", -1);
        missions.Add(mission);
        playerState.Set("MissionProgress", missions);

        var pack = new CatalogueDatabase.StoryCompleterPack(21, [],
            [new CatalogueDatabase.MissionCompleter("PIRATES_LORE", 2, 0)]);

        var status = CatalogueCompletionLogic.GetKnowledgeCompleterStatuses(playerState, pack)
            .Single(s => s.Id == "PIRATES_LORE");

        Assert.True(status.Complete); // -1 is the game's completed marker
    }

    [Fact]
    public void GetKnowledgeCompleterStatuses_IncludesMissionsAndFlags()
    {
        var playerState = new JsonObject();
        var missions = new[]
        {
            new CatalogueDatabase.MissionCompleter("ATLAS1", 12, 0),
        };
        var pack = new CatalogueDatabase.StoryCompleterPack(21, [], missions);

        var statuses = CatalogueCompletionLogic.GetKnowledgeCompleterStatuses(playerState, pack);

        Assert.Contains(statuses, s => s.Kind == KnowledgeCompleterKind.Flag && s.Id == "BuildersKnown");
        Assert.Contains(statuses, s => s.Kind == KnowledgeCompleterKind.Mission && s.Id == "ATLAS1" && !s.Complete);
    }

    [Fact]
    public void Pack_WonderWeirdRecords_CoverAllElevenGlitchSlots()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var records = pack.WonderWeirdBasePartRecords;

        // All eleven glitch slots must be fillable (issue #137: slots 7, 10 and 11 were empty).
        Assert.Equal(11, records.Count);
        Assert.All(records, r => Assert.True(CatalogueCompletionLogic.IsWonderSlotFilled(r)));
        Assert.All(records, r => Assert.True(r.GetBool("SeenInFrontend")));

        // The base part ID is packed into the two GenerationID chunks (little-endian ASCII).
        Assert.Equal(GenerationChunk("BASE_BON"), records[6].GetArray("GenerationID")!.GetString(0));
        Assert.Equal(GenerationChunk("BASE_SHE"), records[9].GetArray("GenerationID")!.GetString(0));
        Assert.Equal(GenerationChunk("BASE_WEI"), records[10].GetArray("GenerationID")!.GetString(0));
    }

    private static string GenerationChunk(string text)
    {
        ulong value = 0;
        for (int i = 0; i < text.Length; i++)
            value |= (ulong)(byte)text[i] << (8 * i);
        return "0x" + value.ToString("X", System.Globalization.CultureInfo.InvariantCulture);
    }

    // --- Extended pack integration ---

    [Fact]
    public void Pack_LoadsExtendedTotals()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);

        Assert.True(pack.DiscoveryStats.Count >= 40);
        Assert.Equal(9738, pack.DiscoveryStats["FISH_CAUGHT"]);

        Assert.True(pack.WonderFallbackSlots.ContainsKey("WonderPlanetRecords"));
        Assert.Equal(11, pack.WonderFallbackSlots["WonderPlanetRecords"].Count);
        Assert.Equal(15, pack.WonderFallbackSlots["WonderCreatureRecords"].Count);
        Assert.Equal(8, pack.WonderFallbackSlots["WonderFloraRecords"].Count);
        Assert.Equal(8, pack.WonderFallbackSlots["WonderMineralRecords"].Count);

        Assert.Equal(143, pack.Fossils.Count);
        Assert.Equal(103, pack.SeenSubstances.Count);

        Assert.Equal(21, pack.StoryCompleters.BaseCompMax);
        Assert.Equal(2, pack.StoryCompleters.SiiPatches.Count);
        Assert.Equal(94, pack.StoryCompleters.Missions.Count);
    }

    [Fact]
    public void KnowledgeCatalogue_MatchesReferencePageCount()
    {
        Assert.Equal(41, KnowledgeCatalogue.Pages.Length);
        Assert.Equal(35, KnowledgeCatalogue.Pages.Count(p => p.Recipe != KnowledgeRecipe.Words));
    }

    [Fact]
    public void Pack_AccessorIdsAreNormalisedWithoutCaret()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);

        foreach (var list in new IReadOnlyList<string>[]
        {
            pack.KnownProducts, pack.KnownTech, pack.KnownSpecials, pack.KnownRefinerRecipes,
            pack.SeenSubstances, pack.Fossils,
        })
        {
            Assert.All(list, id => Assert.False(id.StartsWith("^", StringComparison.Ordinal), $"Unexpected caret in {id}"));
        }

        Assert.All(pack.Fishing, fish => Assert.False(fish.Id.StartsWith("^", StringComparison.Ordinal), $"Unexpected caret in {fish.Id}"));
        Assert.All(pack.KnownWordGroups, group => Assert.False(group.Group.StartsWith("^", StringComparison.Ordinal), $"Unexpected caret in {group.Group}"));
    }

    [Fact]
    public void Pack_ApplyAndClearAllCompletersWithRealData()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();

        int changed = CatalogueCompletionLogic.ApplyKnowledgeCompleters(playerState, pack.StoryCompleters);
        Assert.True(changed >= pack.StoryCompleters.Missions.Count);

        var statuses = CatalogueCompletionLogic.GetKnowledgeCompleterStatuses(playerState, pack.StoryCompleters);
        Assert.Equal(5 + 2 + 2 + 2 + pack.StoryCompleters.Missions.Count, statuses.Count);
        Assert.All(statuses, s => Assert.True(s.Complete, $"{s.Id} should be complete"));

        int cleared = CatalogueCompletionLogic.ClearKnowledgeCompleters(playerState, pack.StoryCompleters);
        Assert.True(cleared > 0);
        // Mission targets of 0 or -1 are already-complete sentinels in the pack and stay that way.
        Assert.All(CatalogueCompletionLogic.GetKnowledgeCompleterStatuses(playerState, pack.StoryCompleters),
            s => Assert.True(!s.Complete || s.Target <= 0, $"{s.Id} should be cleared"));
    }

    [Fact]
    public void SetGlobalStatValue_SetsExactValue()
    {
        var playerState = new JsonObject();

        Assert.True(CatalogueCompletionLogic.SetGlobalStatValue(playerState, "DISC_PLANETS", 5));
        var map = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.Equal(5, CatalogueCompletionLogic.GetGlobalInt(map["DISC_PLANETS"]));

        Assert.False(CatalogueCompletionLogic.SetGlobalStatValue(playerState, "DISC_PLANETS", 5));
        Assert.True(CatalogueCompletionLogic.SetGlobalStatValue(playerState, "DISC_PLANETS", 0));
        Assert.Equal(0, CatalogueCompletionLogic.GetGlobalInt(map["DISC_PLANETS"]));
    }

    [Fact]
    public void ApplyKnowledgePageProgress_SetsPartialAndClears()
    {
        var playerState = new JsonObject();
        var page = KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_journey"); // counter page, target 51, GLOBAL CORE_LORE

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, page, 5));
        Assert.Equal(5, CatalogueCompletionLogic.GetLastSeen(playerState, page.Slot, page.PageIndex));
        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.Equal(5, CatalogueCompletionLogic.GetGlobalInt(globals["CORE_LORE"]));

        Assert.False(CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, page, 5));

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, page, 0));
        Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState, page.Slot, page.PageIndex));
    }

    [Fact]
    public void ApplyKnowledgePageProgress_DeveloperCommentaryWritesIndexAndCount()
    {
        var playerState = new JsonObject();
        var page = KnowledgeCatalogue.Pages.Single(p => p.Id == "jr_devnotes");

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, page, 5));
        Assert.Equal(5, CatalogueCompletionLogic.GetLastSeen(playerState, KnowledgeCatalogue.DevNotesPageSlot, KnowledgeCatalogue.DevNotesPageIndex));
        var globals = CatalogueCompletionLogic.GetGlobalStatsMap(playerState);
        Assert.Equal(4, CatalogueCompletionLogic.GetGlobalInt(globals["DEV_NOTES"])); // highest index, count 5

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, page, KnowledgeCatalogue.DevNotesTarget));
        Assert.Equal(39, CatalogueCompletionLogic.GetLastSeen(playerState, KnowledgeCatalogue.DevNotesPageSlot, KnowledgeCatalogue.DevNotesPageIndex));
        Assert.Equal(38, CatalogueCompletionLogic.GetGlobalInt(globals["DEV_NOTES"]));

        var status = CatalogueCompletionLogic.GetKnowledgeStatus(playerState, page,
            CatalogueCompletionLogic.GetGlobalStatsMap(playerState));
        Assert.Equal("OK", status.Status);

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgePageProgress(playerState, page, 0));
        Assert.Null(CatalogueCompletionLogic.GetLastSeen(playerState, KnowledgeCatalogue.DevNotesPageSlot, KnowledgeCatalogue.DevNotesPageIndex));
        Assert.Equal(-1, CatalogueCompletionLogic.GetGlobalInt(globals["DEV_NOTES"]));
    }

    [Fact]
    public void ApplyKnowledgeCompleterProgress_SetsPartialAndClears()
    {
        var playerState = new JsonObject();
        var pack = new CatalogueDatabase.StoryCompleterPack(21, [],
            [new CatalogueDatabase.MissionCompleter("ATLAS1", 12, 0)]);
        var status = CatalogueCompletionLogic.GetKnowledgeCompleterStatuses(playerState, pack)
            .Single(s => s.Kind == KnowledgeCompleterKind.Mission);

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgeCompleterProgress(playerState, status, 5));
        Assert.Equal(5, CatalogueCompletionLogic.GetMissionProgress(playerState, "ATLAS1"));
        Assert.False(status.Complete);

        Assert.True(CatalogueCompletionLogic.ApplyKnowledgeCompleterProgress(playerState, status, 0));
        Assert.Equal(0, CatalogueCompletionLogic.GetMissionProgress(playerState, "ATLAS1")); // cleared to not-started
    }

    [Fact]
    public void CatalogueDatabase_ReadsExtractedDataAndMergesWordFallbacks()
    {
        string extractedDir = Path.Combine(Path.GetTempPath(), $"nmse_pack_extracted_{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractedDir);
        try
        {
            File.WriteAllText(Path.Combine(extractedDir, "Catalogue Pack.json"), """
            {
              "KnownRefinerRecipes": ["RECIPE_A", "RECIPE_B"],
              "KnownWordGroups": [ { "Group": "^NEWGROUP", "Races": [true, false, false, false, false] } ],
              "Fossils": ["FOS_X"],
              "KnownPortalRunes": 65535
            }
            """);

            var pack = new CatalogueDatabase(extractedDir);

            Assert.True(pack.IsAvailable);
            Assert.Equal(2, pack.KnownRefinerRecipes.Count);
            Assert.Equal("RECIPE_A", pack.KnownRefinerRecipes[0]);
            Assert.Single(pack.Fossils);
            Assert.Equal("FOS_X", pack.Fossils[0]);
            Assert.Equal(65535, pack.KnownPortalRunes);

            // Extracted groups merge with the editor-known legacy groups.
            Assert.Contains(pack.KnownWordGroups, g => g.Group == "NEWGROUP");
            Assert.Contains(pack.KnownWordGroups, g => g.Group == "TRA_COLLECTION");
        }
        finally
        {
            try { Directory.Delete(extractedDir, true); } catch { }
        }
    }

    [Fact]
    public void Pack_CompleteAllDiscoveryStatsWithRealData()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return; // Skip when the working directory does not contain Resources

        var pack = new CatalogueDatabase(jsonDir);
        var playerState = new JsonObject();

        int changed = 0;
        foreach (var (id, target) in pack.DiscoveryStats)
        {
            if (CatalogueCompletionLogic.SetGlobalStatAtLeast(playerState, id, target)) changed++;
        }
        Assert.Equal(pack.DiscoveryStats.Count, changed);

        var (have, total) = CatalogueCompletionLogic.GetGlobalStatsCompletion(playerState, pack.DiscoveryStats);
        Assert.Equal(total, have);

        int cleared = 0;
        foreach (var (id, _) in pack.DiscoveryStats)
        {
            if (CatalogueCompletionLogic.ClearGlobalStat(playerState, id)) cleared++;
        }
        Assert.Equal(pack.DiscoveryStats.Count, cleared);
    }
}
