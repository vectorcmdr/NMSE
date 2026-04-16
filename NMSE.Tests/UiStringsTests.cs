using NMSE.Data;

namespace NMSE.Tests;

/// <summary>
/// Tests for <see cref="UiStrings"/> static UI string table localisation.
/// Each test creates its own temp directory with JSON files to avoid
/// cross-test interference on the shared static state.
/// </summary>
[Collection("MutableStaticDatabases")]
public class UiStringsTests : IDisposable
{
    private readonly string _tmpDir;

    public UiStringsTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), "nmse_uistr_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        UiStrings.Reset();
        try { Directory.Delete(_tmpDir, recursive: true); } catch { }
    }

    [Fact]
    public void Get_ReturnsRawKey_WhenNoDataLoaded()
    {
        UiStrings.Reset();
        Assert.Equal("menu.file", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Load_EnglishFallback_ReturnsEnglishStrings()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");

        UiStrings.SetDirectory(_tmpDir);
        bool result = UiStrings.Load("en-GB");

        Assert.True(result);
        Assert.Equal("&File", UiStrings.Get("menu.file"));
        Assert.Equal("Player", UiStrings.Get("tab.player"));
    }

    [Fact]
    public void Load_TranslatedLanguage_ReturnsTranslatedStrings()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル", "tab.player": "プレイヤー"}""");

        UiStrings.SetDirectory(_tmpDir);
        bool result = UiStrings.Load("ja-JP");

        Assert.True(result);
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));
        Assert.Equal("プレイヤー", UiStrings.Get("tab.player"));
    }

    [Fact]
    public void Get_FallsBackToEnglish_WhenKeyMissingInTranslation()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player", "tab.exosuit": "Exosuit"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("ja-JP");

        // Translated key
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));
        // Missing in ja-JP, falls back to English
        Assert.Equal("Player", UiStrings.Get("tab.player"));
        Assert.Equal("Exosuit", UiStrings.Get("tab.exosuit"));
    }

    [Fact]
    public void Get_ReturnsRawKey_WhenKeyMissingEverywhere()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal("nonexistent.key", UiStrings.Get("nonexistent.key"));
    }

    [Fact]
    public void GetOrNull_ReturnsNull_WhenKeyMissing()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Null(UiStrings.GetOrNull("nonexistent.key"));
        Assert.Equal("&File", UiStrings.GetOrNull("menu.file"));
    }

    [Fact]
    public void Format_SubstitutesArguments()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"status.loaded": "Loaded {0} items, {1} mappings"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Loaded 42 items, 10 mappings", UiStrings.Format("status.loaded", 42, 10));
    }

    [Fact]
    public void Format_HandlesInvalidPlaceholders_Gracefully()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"bad.format": "Value is {999}"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        // Should not throw - returns unformatted template
        string result = UiStrings.Format("bad.format", "a");
        Assert.Equal("Value is {999}", result);
    }

    [Fact]
    public void TranslatedCount_IsZero_WhenEnglishActive()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal(0, UiStrings.TranslatedCount);
        Assert.False(UiStrings.IsActive);
    }

    [Fact]
    public void TranslatedCount_ReturnsCount_WhenOtherLanguageActive()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File", "tab.player": "Player"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "de-DE.json"),
            """{"menu.file": "&Datei", "tab.player": "Spieler"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("de-DE");

        Assert.Equal(2, UiStrings.TranslatedCount);
        Assert.True(UiStrings.IsActive);
    }

    [Fact]
    public void TotalKeyCount_ReflectsEnglishFallback()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"a": "1", "b": "2", "c": "3"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");

        Assert.Equal(3, UiStrings.TotalKeyCount);
    }

    [Fact]
    public void Load_ReturnsFalse_WhenLanguageFileMissing()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        bool result = UiStrings.Load("xx-XX");

        Assert.False(result);
        // Should still have English fallback available
        Assert.Equal("&File", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Load_ReturnsFalse_WhenDirectoryNotSet()
    {
        UiStrings.Reset();
        bool result = UiStrings.Load("en-GB");
        Assert.False(result);
    }

    [Fact]
    public void Load_SwitchLanguage_ReplacesStrings()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "de-DE.json"),
            """{"menu.file": "&Datei"}""");

        UiStrings.SetDirectory(_tmpDir);

        UiStrings.Load("ja-JP");
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));

        UiStrings.Load("de-DE");
        Assert.Equal("&Datei", UiStrings.Get("menu.file"));

        // Switch back to English
        UiStrings.Load("en-GB");
        Assert.Equal("&File", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Load_NullTag_RevertsToEnglish()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("ja-JP");
        Assert.Equal("&ファイル", UiStrings.Get("menu.file"));

        UiStrings.Load(null);
        Assert.Equal("&File", UiStrings.Get("menu.file"));
        Assert.False(UiStrings.IsActive);
    }

    [Fact]
    public void Load_EmptyTag_RevertsToEnglish()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");
        File.WriteAllText(Path.Combine(_tmpDir, "ja-JP.json"),
            """{"menu.file": "&ファイル"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("ja-JP");
        UiStrings.Load("");
        Assert.Equal("&File", UiStrings.Get("menu.file"));
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        File.WriteAllText(Path.Combine(_tmpDir, "en-GB.json"),
            """{"menu.file": "&File"}""");

        UiStrings.SetDirectory(_tmpDir);
        UiStrings.Load("en-GB");
        Assert.Equal("&File", UiStrings.Get("menu.file"));

        UiStrings.Reset();
        Assert.Equal(0, UiStrings.TotalKeyCount);
        Assert.Equal("menu.file", UiStrings.Get("menu.file"));
    }

    // --- Locale key existence tests using real en-GB data ---

    private static string? FindRealLangDir()
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

    [Fact]
    public void UiStrings_DiscoveryTabLocations_HasExpectedValue()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Teleport Destinations", UiStrings.Get("discovery.tab_locations"));
    }

    [Fact]
    public void UiStrings_CommonProceduralNoName_ExistsInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("common.procedural_no_name");
        // Should resolve to a real string, not fall back to the raw key
        Assert.NotEqual("common.procedural_no_name", value);
    }

    [Fact]
    public void UiStrings_SettlementDeleteWarning_ContainsTeleportDestinations()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("settlement.delete_warning");
        Assert.Contains("Teleport Destinations", value);
    }

    // --- Battle tab string tests using real locale data ---

    [Fact]
    public void UiStrings_BattleMoveSlot_IsAbilitySlotInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Ability Slot {0}:", UiStrings.Get("companion.battle_move_slot"));
        Assert.Equal("Ability Slot 1:", UiStrings.Format("companion.battle_move_slot", 1));
        Assert.Equal("Ability Slot 5:", UiStrings.Format("companion.battle_move_slot", 5));
    }

    [Fact]
    public void UiStrings_BattleGenesLevel_IsGenesImprovedInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Genes Improved:", UiStrings.Get("companion.battle_genes_level"));
    }

    [Fact]
    public void UiStrings_BattleMutationHeading_IsGeneticProfileInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Genetic Profile", UiStrings.Get("companion.battle_mutation_heading"));
    }

    private static readonly string[] AllLanguages =
    [
        "en-GB", "en-US", "de-DE", "fr-FR", "es-ES", "es-419",
        "it-IT", "pt-PT", "pt-BR", "nl-NL", "pl-PL", "ru-RU",
        "ja-JP", "ko-KR", "zh-CN", "zh-TW"
    ];

    [Fact]
    public void UiStrings_BattleKeys_ExistInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        string[] requiredKeys =
        [
            "companion.battle_move_slot",
            "companion.battle_genes_level",
            "companion.battle_mutation_heading"
        ];

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            foreach (var key in requiredKeys)
            {
                string value = UiStrings.Get(key);
                Assert.NotEqual(key, value); // Should resolve to a real string, not fall back to raw key
            }
        }
    }

    [Fact]
    public void UiStrings_BattleMoveSlot_NoLongerContainsMoveInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("companion.battle_move_slot");
        Assert.DoesNotContain("Move Slot", value);
        Assert.Contains("Ability Slot", value);
    }

    [Fact]
    public void UiStrings_BattleGenesLevel_NoLongerContainsLevelInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("companion.battle_genes_level");
        Assert.DoesNotContain("/ Level", value);
    }

    [Fact]
    public void UiStrings_BattleMutationHeading_NoLongerContainsMutationProgressInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        string value = UiStrings.Get("companion.battle_mutation_heading");
        Assert.DoesNotContain("Mutation Progress", value);
    }

    // --- Base panel export/import locale tests ---

    [Fact]
    public void UiStrings_BaseExportImport_KeysExistInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Export", UiStrings.Get("base.export"));
        Assert.Equal("Import", UiStrings.Get("base.import"));
        Assert.Equal("Export", UiStrings.Get("base.export_title"));
        Assert.Equal("Import", UiStrings.Get("base.import_title"));
        Assert.Equal("Confirm Import", UiStrings.Get("base.confirm_import_title"));
    }

    [Fact]
    public void UiStrings_BaseExportImport_OldBackupRestoreKeysRemovedFromEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        // Old keys should fall back to the raw key name (indicating they are missing)
        Assert.Equal("base.backup", UiStrings.Get("base.backup"));
        Assert.Equal("base.restore", UiStrings.Get("base.restore"));
    }

    [Fact]
    public void UiStrings_BaseExportImport_KeysExistInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        string[] requiredKeys =
        [
            "base.export", "base.export_failed", "base.export_success", "base.export_title",
            "base.confirm_import_title", "base.import", "base.import_confirm",
            "base.import_failed", "base.import_success", "base.import_title"
        ];

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            foreach (var key in requiredKeys)
            {
                string value = UiStrings.Get(key);
                Assert.NotEqual(key, value);
            }
        }
    }

    // --- Raw JSON Editor node export/import locale tests ---

    [Fact]
    public void UiStrings_RawJsonNodeExportImport_KeysExistInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Export Node...", UiStrings.Get("raw_json.ctx_export_node"));
        Assert.Equal("Import Node...", UiStrings.Get("raw_json.ctx_import_node"));
        Assert.Equal("Export Node", UiStrings.Get("raw_json.export_node_title"));
        Assert.Equal("Import Node", UiStrings.Get("raw_json.import_node_title"));
    }

    [Fact]
    public void UiStrings_RawJsonNodeExportImport_KeysExistInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        string[] requiredKeys =
        [
            "raw_json.ctx_export_node", "raw_json.ctx_import_node",
            "raw_json.exported_node", "raw_json.export_node_title",
            "raw_json.imported_node", "raw_json.import_node_title"
        ];

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            foreach (var key in requiredKeys)
            {
                string value = UiStrings.Get(key);
                Assert.NotEqual(key, value);
            }
        }
    }

    // --- Inventory panel picker detail and slot label locale tests ---

    [Fact]
    public void UiStrings_InventoryPickerKeys_ExistInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Slot:", UiStrings.Get("inventory.label_slot"));
        Assert.Equal("N/A", UiStrings.Get("inventory.category_na"));
        Assert.Equal("Add Item", UiStrings.Get("inventory.picker_add_item"));
        Assert.Equal("Replace Item", UiStrings.Get("inventory.picker_replace_item"));
        Assert.Equal("(no item selected)", UiStrings.Get("inventory.picker_no_item"));
        Assert.Equal("Select an item from the picker or enter an Item ID.", UiStrings.Get("inventory.picker_enter_id"));
    }

    [Fact]
    public void UiStrings_InventoryPickerKeys_ExistInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        string[] requiredKeys =
        [
            "inventory.add_item",
            "inventory.category_na",
            "inventory.label_slot",
            "inventory.picker_add_item",
            "inventory.picker_enter_id",
            "inventory.picker_no_item",
            "inventory.picker_replace_item"
        ];

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            foreach (var key in requiredKeys)
            {
                string value = UiStrings.Get(key);
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void UiStrings_InventoryLabelSlot_ReplacedLabelPosition()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        // The label_slot key should exist and return "Slot:" (not "Position:")
        string slot = UiStrings.Get("inventory.label_slot");
        Assert.Equal("Slot:", slot);

        // The old label_position key should still exist for backward compatibility
        string position = UiStrings.Get("inventory.label_position");
        Assert.NotEqual("inventory.label_position", position);
    }

    [Fact]
    public void UiStrings_InventoryHoverInfoAndClassKeys_ExistInEnGB()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        UiStrings.SetDirectory(langDir);
        UiStrings.Load("en-GB");

        Assert.Equal("Hover for info.", UiStrings.Get("inventory.hover_info"));
        Assert.Equal("Class:", UiStrings.Get("inventory.label_class"));
    }

    [Fact]
    public void UiStrings_InventoryHoverInfoAndClassKeys_ExistInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        string[] requiredKeys =
        [
            "inventory.hover_info",
            "inventory.label_class"
        ];

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            foreach (var key in requiredKeys)
            {
                string value = UiStrings.Get(key);
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void UiStrings_AccessorySlotFrontBack_ExistInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        string[] requiredKeys =
        [
            "companion.accessory_slot_front",
            "companion.accessory_slot_back",
        ];

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            foreach (var key in requiredKeys)
            {
                string value = UiStrings.Get(key);
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void UiStrings_NoAccessories_ExistsInAllLanguages()
    {
        var langDir = FindRealLangDir();
        if (langDir == null) return;

        foreach (var lang in AllLanguages)
        {
            UiStrings.SetDirectory(langDir);
            UiStrings.Load(lang);

            string value = UiStrings.Get("companion.no_accessories");
            Assert.NotEqual("companion.no_accessories", value);
        }
    }
}
