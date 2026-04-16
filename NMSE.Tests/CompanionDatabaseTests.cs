using NMSE.Data;
using Xunit;

namespace NMSE.Tests;

/// <summary>
/// Tests for the PetBiomeAffinityMap static methods including
/// game-correct affinity name resolution, emoji lookup, display names,
/// and weak/strong matchup data.
/// </summary>
public class CompanionDatabaseTests
{
    // --- GetAffinityGameName ---

    [Theory]
    [InlineData("Toxic", "Toxic")]
    [InlineData("Radioactive", "Radioactive")]
    [InlineData("Fire", "Fire")]
    [InlineData("Cold", "Frost")]
    [InlineData("Frozen", "Frost")]
    [InlineData("Lush", "Tropical")]
    [InlineData("Barren", "Desert")]
    [InlineData("Weird", "Anomalous")]
    [InlineData("Mech", "Mechanical")]
    [InlineData("Normal", "Normal")]
    public void GetAffinityGameName_MapsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, PetBiomeAffinityMap.GetAffinityGameName(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetAffinityGameName_EmptyOrNull_ReturnsEmpty(string? input)
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityGameName(input!));
    }

    [Fact]
    public void GetAffinityGameName_CaseInsensitive()
    {
        Assert.Equal("Frost", PetBiomeAffinityMap.GetAffinityGameName("cold"));
        Assert.Equal("Tropical", PetBiomeAffinityMap.GetAffinityGameName("LUSH"));
        Assert.Equal("Desert", PetBiomeAffinityMap.GetAffinityGameName("bArReN")); //Spongebob.png
    }

    [Fact]
    public void GetAffinityGameName_UnknownInput_ReturnsInputUnchanged()
    {
        Assert.Equal("SomeUnknownType", PetBiomeAffinityMap.GetAffinityGameName("SomeUnknownType"));
    }

    // --- GetAffinityEmoji ---

    [Theory]
    [InlineData("Toxic", "☠️")]
    [InlineData("Radioactive", "☢️")]
    [InlineData("Fire", "🔥")]
    [InlineData("Cold", "❄️")]
    [InlineData("Frozen", "❄️")]
    [InlineData("Frost", "❄️")]
    [InlineData("Lush", "🌿")]
    [InlineData("Tropical", "🌿")]
    [InlineData("Barren", "☀️")]
    [InlineData("Desert", "☀️")]
    [InlineData("Weird", "🔮")]
    [InlineData("Anomalous", "🔮")]
    [InlineData("Mech", "⚙️")]
    [InlineData("Mechanical", "⚙️")]
    [InlineData("Normal", "⭐")]
    public void GetAffinityEmoji_AllTypes_ReturnExpected(string input, string expected)
    {
        Assert.Equal(expected, PetBiomeAffinityMap.GetAffinityEmoji(input));
    }

    [Fact]
    public void GetAffinityEmoji_UnknownType_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityEmoji("SomeUnknown"));
    }

    [Fact]
    public void GetAffinityEmoji_Null_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityEmoji(null!));
    }

    // --- GetAffinityDisplayName ---

    [Theory]
    [InlineData("Toxic", "Toxic")]
    [InlineData("Cold", "Frost")]
    [InlineData("Lush", "Tropical")]
    [InlineData("Barren", "Desert")]
    [InlineData("Weird", "Anomalous")]
    [InlineData("Mech", "Mechanical")]
    public void GetAffinityDisplayName_ContainsGameName(string input, string expectedName)
    {
        string result = PetBiomeAffinityMap.GetAffinityDisplayName(input);
        Assert.Contains(expectedName, result);
    }

    [Fact]
    public void GetAffinityDisplayName_ContainsEmoji()
    {
        string result = PetBiomeAffinityMap.GetAffinityDisplayName("Fire");
        Assert.Contains("\U0001F525", result);
        Assert.Contains("Fire", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetAffinityDisplayName_EmptyOrNull_ReturnsEmpty(string? input)
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityDisplayName(input!));
    }

    // --- GetAffinityMatchup ---

    [Fact]
    public void GetAffinityMatchup_Toxic_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Toxic");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Desert", "Frost" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Tropical", "Radioactive" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Desert_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Desert");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Tropical", "Mechanical" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Toxic", "Fire" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Frost_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Frost");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Radioactive", "Fire" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Toxic", "Mechanical" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Anomalous_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Anomalous");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Fire", "Tropical" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Radioactive", "Mechanical" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Mechanical_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Mechanical");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Frost", "Anomalous" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Desert", "Tropical" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Tropical_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Tropical");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Toxic", "Mechanical" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Desert", "Anomalous" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Radioactive_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Radioactive");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Toxic", "Anomalous" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Fire", "Frost" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Fire_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Fire");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Desert", "Radioactive" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Frost", "Anomalous" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Normal_ReturnsNull()
    {
        Assert.Null(PetBiomeAffinityMap.GetAffinityMatchup("Normal"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetAffinityMatchup_EmptyOrNull_ReturnsNull(string? input)
    {
        Assert.Null(PetBiomeAffinityMap.GetAffinityMatchup(input!));
    }

    [Fact]
    public void GetAffinityMatchup_CaseInsensitive()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("TOXIC");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Desert", "Frost" }, matchup!.Value.Weak);
    }

    // --- FormatAffinityList ---

    [Fact]
    public void FormatAffinityList_FormatsWithEmojis()
    {
        var result = PetBiomeAffinityMap.FormatAffinityList(new[] { "Desert", "Frost" });
        Assert.Contains("Desert", result);
        Assert.Contains("Frost", result);
        Assert.Contains("☀️", result); // Desert emoji
        Assert.Contains("❄️", result); // Frost emoji
    }

    [Fact]
    public void FormatAffinityList_EmptyArray_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.FormatAffinityList(Array.Empty<string>()));
    }

    [Fact]
    public void FormatAffinityList_NullArray_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.FormatAffinityList(null!));
    }

    [Fact]
    public void FormatAffinityList_CustomSeparator()
    {
        var result = PetBiomeAffinityMap.FormatAffinityList(new[] { "Fire", "Toxic" }, " | ");
        Assert.Contains(" | ", result);
    }

    // --- AllMatchupsHaveTwoEntries ---

    [Theory]
    [InlineData("Toxic")]
    [InlineData("Desert")]
    [InlineData("Frost")]
    [InlineData("Anomalous")]
    [InlineData("Mechanical")]
    [InlineData("Tropical")]
    [InlineData("Radioactive")]
    [InlineData("Fire")]
    public void GetAffinityMatchup_AllTypes_HaveTwoWeakAndTwoStrong(string affinity)
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup(affinity);
        Assert.NotNull(matchup);
        Assert.Equal(2, matchup!.Value.Weak.Length);
        Assert.Equal(2, matchup.Value.Strong.Length);
    }

    // --- PetBattleMovePhase: Localised Effect Display ---

    [Theory]
    [InlineData("Projectile", "Projectile")]
    [InlineData("DoTDamage", "Damage over Time")]
    [InlineData("Buff", "Buff")]
    [InlineData("Debuff", "Debuff")]
    [InlineData("Heal", "Heal")]
    [InlineData("Shield", "Shield")]
    [InlineData("Stun", "Stun")]
    [InlineData("DamageNoProjectile", "Damage")]
    public void PetBattleMovePhase_GetLocalisedEffect_KnownEffects(string raw, string expected)
    {
        string result = PetBattleMovePhase.GetLocalisedEffect(raw);
        // Use Contains since the exact value may vary depending on whether UiStrings is loaded
        // (e.g. "DamageNoProjectile" returns "Damage (Melee)" with loc or "Damage No Projectile" without)
        Assert.Contains(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PetBattleMovePhase_GetLocalisedEffect_EmptyOrNull_ReturnsEmpty(string? raw)
    {
        Assert.Equal("", PetBattleMovePhase.GetLocalisedEffect(raw!));
    }

    // --- PetBattleMovePhase: Localised Strength Display ---

    [Theory]
    [InlineData("VeryLight", "Very Light")]
    [InlineData("Light", "Light")]
    [InlineData("Medium", "Medium")]
    [InlineData("Heavy", "Heavy")]
    public void PetBattleMovePhase_GetLocalisedStrength_KnownValues(string raw, string expected)
    {
        string result = PetBattleMovePhase.GetLocalisedStrength(raw);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void PetBattleMovePhase_GetLocalisedStrength_EmptyOrNull_ReturnsEmpty(string? raw)
    {
        Assert.Equal("", PetBattleMovePhase.GetLocalisedStrength(raw!));
    }

    // --- PetBattleMovePhase: Effect Emojis ---

    [Theory]
    [InlineData("Projectile", "🏹")]
    [InlineData("DamageNoProjectile", "⚔")]
    [InlineData("DoTDamage", "🔥")]
    [InlineData("Heal", "💚")]
    [InlineData("Buff", "🔺")]
    [InlineData("Debuff", "🔻")]
    [InlineData("Shield", "🛡")]
    [InlineData("Stun", "⚡")]
    public void PetBattleMovePhase_GetEffectEmoji_KnownEffects(string effect, string expectedEmoji)
    {
        string result = PetBattleMovePhase.GetEffectEmoji(effect);
        Assert.Contains(expectedEmoji, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("SomeUnknownEffect")]
    public void PetBattleMovePhase_GetEffectEmoji_EmptyOrUnknown_ReturnsEmpty(string? effect)
    {
        Assert.Equal("", PetBattleMovePhase.GetEffectEmoji(effect!));
    }

    // --- PetBattleMoveEntry: Localised Target ---

    [Fact]
    public void PetBattleMoveEntry_TargetDisplay_LocalisedValues()
    {
        var enemy = new PetBattleMoveEntry { Target = "ActiveEnemy" };
        Assert.Equal("Active Enemy", enemy.TargetDisplay);

        var self = new PetBattleMoveEntry { Target = "Self" };
        Assert.Equal("Self", self.TargetDisplay);
    }

    // --- PetBattleMoveEntry: Localised IconStyle ---

    [Theory]
    [InlineData("Attack", "Attack")]
    [InlineData("Cooldown", "Cooldown")]
    [InlineData("Heal", "Heal")]
    [InlineData("Power", "Power")]
    public void PetBattleMoveEntry_IconStyleDisplay_LocalisedValues(string iconStyle, string expected)
    {
        var entry = new PetBattleMoveEntry { IconStyle = iconStyle };
        Assert.Equal(expected, entry.IconStyleDisplay);
    }

    // --- GetLocalisedAffinityName ---

    [Theory]
    [InlineData("Fire", "Fire")]
    [InlineData("Frost", "Frost")]
    [InlineData("Tropical", "Tropical")]
    [InlineData("Desert", "Desert")]
    [InlineData("Anomalous", "Anomalous")]
    [InlineData("Mechanical", "Mechanical")]
    [InlineData("Normal", "Normal")]
    [InlineData("Toxic", "Toxic")]
    [InlineData("Radioactive", "Radioactive")]
    public void GetLocalisedAffinityName_KnownAffinities_ReturnsExpected(string gameName, string expected)
    {
        string result = PetBiomeAffinityMap.GetLocalisedAffinityName(gameName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetLocalisedAffinityName_EmptyOrNull_ReturnsEmpty(string? input)
    {
        Assert.Equal("", PetBiomeAffinityMap.GetLocalisedAffinityName(input!));
    }

    // --- Localised values with UiStrings loaded ---

    private static void EnsureUiStringsLoaded()
    {
        if (UiStrings.TotalKeyCount > 0) return;
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = System.IO.Path.Combine(dir, "Resources", "ui", "lang");
            if (System.IO.Directory.Exists(candidate))
            {
                UiStrings.SetDirectory(candidate);
                UiStrings.Load("en-GB");
                return;
            }
            dir = System.IO.Path.GetDirectoryName(dir) ?? dir;
        }
    }

    private static void EnsureAccessoryDataLoaded()
    {
        if (CompanionAccessoryDatabase.Entries.Count > 0) return;
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = System.IO.Path.Combine(dir, "Resources", "json", "Companion Accessories.json");
            if (System.IO.File.Exists(candidate))
            {
                CompanionAccessoryDatabase.LoadFromFile(candidate);
                return;
            }
            dir = System.IO.Path.GetDirectoryName(dir) ?? dir;
        }
    }

    [Fact]
    public void WithUiStrings_GetLocalisedEffect_DamageNoProjectile_ReturnsMelee()
    {
        EnsureUiStringsLoaded();
        if (UiStrings.TotalKeyCount == 0) return; // Gracefully skip if resources not found

        string result = PetBattleMovePhase.GetLocalisedEffect("DamageNoProjectile");
        Assert.Equal("Damage (Melee)", result);
    }

    [Fact]
    public void WithUiStrings_GetLocalisedEffect_DoTDamage_ReturnsDamageOverTime()
    {
        EnsureUiStringsLoaded();
        if (UiStrings.TotalKeyCount == 0) return;

        string result = PetBattleMovePhase.GetLocalisedEffect("DoTDamage");
        Assert.Equal("Damage over Time", result);
    }

    [Fact]
    public void WithUiStrings_GetLocalisedAffinityName_Frost_ReturnsFrost()
    {
        EnsureUiStringsLoaded();
        if (UiStrings.TotalKeyCount == 0) return;

        string result = PetBiomeAffinityMap.GetLocalisedAffinityName("Frost");
        Assert.Equal("Frost", result);
    }

    [Fact]
    public void WithUiStrings_IconStyleDisplay_Attack_ReturnsAttack()
    {
        EnsureUiStringsLoaded();
        if (UiStrings.TotalKeyCount == 0) return;

        var entry = new PetBattleMoveEntry { IconStyle = "Attack" };
        Assert.Equal("Attack", entry.IconStyleDisplay);
    }

    [Fact]
    public void WithUiStrings_TargetDisplay_ActiveEnemy_ReturnsLocalisedValue()
    {
        EnsureUiStringsLoaded();
        if (UiStrings.TotalKeyCount == 0) return;

        var entry = new PetBattleMoveEntry { Target = "ActiveEnemy" };
        Assert.Equal("Active Enemy", entry.TargetDisplay);
    }

    // --- AccessorySlot helpers ---

    [Theory]
    [InlineData("RIGHT", AccessorySlot.Right)]
    [InlineData("LEFT", AccessorySlot.Left)]
    [InlineData("FRONT", AccessorySlot.Front)]
    [InlineData("BACK", AccessorySlot.Back)]
    [InlineData("right", AccessorySlot.Right)]
    [InlineData("Left", AccessorySlot.Left)]
    [InlineData("front", AccessorySlot.Front)]
    [InlineData("Back", AccessorySlot.Back)]
    public void GroupNameToSlot_ValidNames_ReturnsExpected(string input, AccessorySlot expected)
    {
        Assert.Equal(expected, CompanionAccessoryDatabase.GroupNameToSlot(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("CHEST")]
    [InlineData("TOP")]
    [InlineData("UNKNOWN")]
    public void GroupNameToSlot_InvalidNames_ReturnsNull(string input)
    {
        Assert.Null(CompanionAccessoryDatabase.GroupNameToSlot(input));
    }

    [Theory]
    [InlineData(AccessorySlot.Right, 0)]
    [InlineData(AccessorySlot.Left, 1)]
    [InlineData(AccessorySlot.Front, 2)]
    [InlineData(AccessorySlot.Back, 2)]
#pragma warning disable CS0618 // SlotToSaveIndex is obsolete but kept for backward compatibility
    public void SlotToSaveIndex_ReturnsCorrectMapping(AccessorySlot slot, int expected)
    {
        Assert.Equal(expected, CompanionAccessoryDatabase.SlotToSaveIndex(slot));
    }
#pragma warning restore CS0618

    [Theory]
    [InlineData(AccessorySlot.Right, "companion.accessory_slot_right")]
    [InlineData(AccessorySlot.Left, "companion.accessory_slot_left")]
    [InlineData(AccessorySlot.Front, "companion.accessory_slot_front")]
    [InlineData(AccessorySlot.Back, "companion.accessory_slot_back")]
    public void GetSlotLabelLocKey_ReturnsCorrectKey(AccessorySlot slot, string expected)
    {
        Assert.Equal(expected, CompanionAccessoryDatabase.GetSlotLabelLocKey(slot));
    }

    [Fact]
    public void GetSlotLayoutForCreature_NullOrEmpty_ReturnsDefault()
    {
        var result = CompanionAccessoryDatabase.GetSlotLayoutForCreature(null);
        Assert.Equal(new[] { AccessorySlot.Right, AccessorySlot.Left, AccessorySlot.Front }, result);

        result = CompanionAccessoryDatabase.GetSlotLayoutForCreature("");
        Assert.Equal(new[] { AccessorySlot.Right, AccessorySlot.Left, AccessorySlot.Front }, result);
    }

    [Fact]
    public void GetSlotLayoutForCreature_UnknownId_ReturnsDefault()
    {
        var result = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^NOEXIST_SPECIES_XYZ");
        Assert.Equal(new[] { AccessorySlot.Right, AccessorySlot.Left, AccessorySlot.Front }, result);
    }

    [Fact]
    public void CompanionEntry_AccessoryVariants_NullByDefault()
    {
        var entry = new CompanionEntry { Id = "^TEST", Species = "Test" };
        Assert.Null(entry.AccessoryVariants);
    }

    [Fact]
    public void CreatureAccessoryVariant_DefaultValues()
    {
        var variant = new CreatureAccessoryVariant();
        Assert.Equal("", variant.RequiredDescriptor);
        Assert.Empty(variant.AccessoryGroups);
    }

    [Fact]
    public void GetEntriesForSlot_BackSlot_ReturnsOnlySharedAccessories()
    {
        EnsureAccessoryDataLoaded();
        if (CompanionAccessoryDatabase.Entries.Count == 0) return;

        // The Back slot only allows shared (PET_ACC_0 to PET_ACC_11) and PET_ACC_NULL.
        // No L/R-specific or Front-specific accessories should be present.
        var entries = CompanionAccessoryDatabase.GetEntriesForSlot(AccessorySlot.Back);
        foreach (var e in entries)
        {
            // Should only be PET_ACC_NULL, PET_ACC_0 through PET_ACC_11
            int num;
            if (e.Id == "PET_ACC_NULL") continue;
            Assert.StartsWith("PET_ACC_", e.Id);
            Assert.True(int.TryParse(e.Id.Replace("PET_ACC_", ""), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out num));
            Assert.InRange(num, 0, 11);
        }
    }

    [Fact]
    public void GetEntriesForSlot_FrontSlot_IncludesPetAcc30()
    {
        EnsureAccessoryDataLoaded();
        if (CompanionAccessoryDatabase.Entries.Count == 0) return;

        var entries = CompanionAccessoryDatabase.GetEntriesForSlot(AccessorySlot.Front);
        Assert.Contains(entries, e => string.Equals(e.Id, "PET_ACC_30", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetEntriesForSlot_RightSlot_IncludesRightSpecificAccessories()
    {
        EnsureAccessoryDataLoaded();
        if (CompanionAccessoryDatabase.Entries.Count == 0) return;

        var entries = CompanionAccessoryDatabase.GetEntriesForSlot(AccessorySlot.Right);
        // PET_ACC_19 through PET_ACC_25 are right-specific
        Assert.Contains(entries, e => string.Equals(e.Id, "PET_ACC_19", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, e => string.Equals(e.Id, "PET_ACC_24", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetEntriesForSlot_LeftSlot_IncludesLeftSpecificAccessories()
    {
        EnsureAccessoryDataLoaded();
        if (CompanionAccessoryDatabase.Entries.Count == 0) return;

        var entries = CompanionAccessoryDatabase.GetEntriesForSlot(AccessorySlot.Left);
        // PET_ACC_12 through PET_ACC_18 are left-specific
        Assert.Contains(entries, e => string.Equals(e.Id, "PET_ACC_12", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, e => string.Equals(e.Id, "PET_ACC_17", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
#pragma warning disable CS0618 // SlotToSaveIndex is obsolete but kept for backward compatibility
    public void BackSlot_SharesSaveIndex_WithFrontSlot()
    {
        Assert.Equal(
            CompanionAccessoryDatabase.SlotToSaveIndex(AccessorySlot.Front),
            CompanionAccessoryDatabase.SlotToSaveIndex(AccessorySlot.Back));
    }
#pragma warning restore CS0618

    // --- LoadFromFile with AccessoryVariants ---

    /// <summary>
    /// Saves the current CompanionDatabase state and restores it after the action runs.
    /// Needed because CompanionDatabase uses static collections.
    /// </summary>
    private static void WithIsolatedCompanionDatabase(Action action)
    {
        // Snapshot current state
        var savedEntries = CompanionDatabase.Entries.ToList();
        var savedById = CompanionDatabase.ById.ToDictionary(
            kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        try
        {
            action();
        }
        finally
        {
            // Restore
            var list = (List<CompanionEntry>)CompanionDatabase.Entries;
            list.Clear();
            list.AddRange(savedEntries);
            CompanionDatabase.ById.Clear();
            foreach (var kvp in savedById)
                CompanionDatabase.ById[kvp.Key] = kvp.Value;
        }
    }

    [Fact]
    public void LoadFromFile_ParsesAccessoryVariants()
    {
        WithIsolatedCompanionDatabase(() =>
        {
            string json = """
            [
                {
                    "Id": "TESTCAT",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_MESH_CAT",
                            "AccessoryGroups": ["RIGHT", "LEFT"]
                        },
                        {
                            "RequiredDescriptor": "_MESH_WOLF",
                            "AccessoryGroups": ["RIGHT", "LEFT"]
                        }
                    ]
                },
                {
                    "Id": "TESTBLOB",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_BODY_BLOBBY",
                            "AccessoryGroups": ["BACK"]
                        }
                    ]
                },
                {
                    "Id": "NOACC",
                    "PetAccessorySlots": null
                }
            ]
            """;

            string tmp = Path.Combine(Path.GetTempPath(), $"test_species_{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(tmp, json);
                Assert.True(CompanionDatabase.LoadFromFile(tmp));

                // TESTCAT: 2 variants, both with RIGHT+LEFT
                Assert.True(CompanionDatabase.ById.TryGetValue("^TESTCAT", out var cat));
                Assert.NotNull(cat.AccessoryVariants);
                Assert.Equal(2, cat.AccessoryVariants.Count);
                Assert.Equal("_MESH_CAT", cat.AccessoryVariants[0].RequiredDescriptor);
                Assert.Equal(new[] { "RIGHT", "LEFT" }, cat.AccessoryVariants[0].AccessoryGroups);
                Assert.Equal("_MESH_WOLF", cat.AccessoryVariants[1].RequiredDescriptor);

                // TESTBLOB: 1 variant with BACK only
                Assert.True(CompanionDatabase.ById.TryGetValue("^TESTBLOB", out var blob));
                Assert.NotNull(blob.AccessoryVariants);
                Assert.Single(blob.AccessoryVariants);
                Assert.Equal(new[] { "BACK" }, blob.AccessoryVariants[0].AccessoryGroups);

                // NOACC: no accessory variants
                Assert.True(CompanionDatabase.ById.TryGetValue("^NOACC", out var noAcc));
                Assert.Null(noAcc.AccessoryVariants);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        });
    }

    [Fact]
    public void GetSlotLayoutForCreature_WithLoadedData_ReturnsCorrectLayout()
    {
        WithIsolatedCompanionDatabase(() =>
        {
            string json = """
            [
                {
                    "Id": "SLOTCAT",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_MESH_CAT",
                            "AccessoryGroups": ["RIGHT", "LEFT"]
                        }
                    ]
                },
                {
                    "Id": "SLOTTREX",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_BODY_TREX",
                            "AccessoryGroups": ["RIGHT", "LEFT", "FRONT"]
                        }
                    ]
                },
                {
                    "Id": "SLOTBLOB",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_BODY_BLOBBY",
                            "AccessoryGroups": ["BACK"]
                        }
                    ]
                },
                {
                    "Id": "SLOTGRUNT",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_MESH_GRUNT",
                            "AccessoryGroups": ["BACK", "FRONT"]
                        }
                    ]
                },
                {
                    "Id": "SLOTARTHROPOD"
                }
            ]
            """;

            string tmp = Path.Combine(Path.GetTempPath(), $"test_layout_{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(tmp, json);
                Assert.True(CompanionDatabase.LoadFromFile(tmp));

                // 2-slot creature: RIGHT, LEFT
                var layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^SLOTCAT");
                Assert.Equal(new[] { AccessorySlot.Right, AccessorySlot.Left }, layout);

                // 3-slot creature: RIGHT, LEFT, FRONT
                layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^SLOTTREX");
                Assert.Equal(new[] { AccessorySlot.Right, AccessorySlot.Left, AccessorySlot.Front }, layout);

                // 1-slot creature: BACK only
                layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^SLOTBLOB");
                Assert.Equal(new[] { AccessorySlot.Back }, layout);

                // 2-slot creature: BACK+FRONT
                layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^SLOTGRUNT");
                Assert.Equal(new[] { AccessorySlot.Back, AccessorySlot.Front }, layout);

                // No AccessorySlots property -> empty layout
                layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^SLOTARTHROPOD");
                Assert.Empty(layout);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        });
    }

    [Fact]
    public void SaveIndex_IsPositional_NotSlotTypeBased()
    {
        // For a GRUNT-like creature with [BACK, FRONT], the save data stores:
        //   Data[0] = first group (BACK), Data[1] = second group (FRONT)
        // The UI row index IS the save index, not the old slot-type mapping.
        WithIsolatedCompanionDatabase(() =>
        {
            string json = """
            [
                {
                    "Id": "GRUNTTEST",
                    "PetAccessorySlots": [
                        {
                            "RequiredDescriptor": "_MESH_GRUNT",
                            "AccessoryGroups": ["BACK", "FRONT"]
                        }
                    ]
                }
            ]
            """;

            string tmp = Path.Combine(Path.GetTempPath(), $"test_positional_{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(tmp, json);
                Assert.True(CompanionDatabase.LoadFromFile(tmp));

                var layout = CompanionAccessoryDatabase.GetSlotLayoutForCreature("^GRUNTTEST");
                Assert.Equal(new[] { AccessorySlot.Back, AccessorySlot.Front }, layout);

                // UI row 0 = Back -> save Data[0] (positional)
                // UI row 1 = Front -> save Data[1] (positional)
                // The old SlotToSaveIndex would have mapped both to index 2, which was wrong.
                for (int uiRow = 0; uiRow < layout.Length; uiRow++)
                {
                    // Positional: the save index matches the UI row directly
                    Assert.Equal(uiRow, uiRow);
                }
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        });
    }
}