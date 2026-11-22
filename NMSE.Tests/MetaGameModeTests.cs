using NMSE.IO;
using NMSE.Data;
using NMSE.Core;
using NMSE.Models;

namespace NMSE.Tests;

public class MetaGameModeTests
{
    private static string? GetResourcePath(params string[] parts)
    {
        var basePath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(new[] { basePath }.Concat(parts).ToArray());
        return File.Exists(path) || Directory.Exists(path) ? path : null;
    }

    [Fact]
    public void ExtractMetaInfo_FallsBackToDifficultyState()
    {
        var savePath = GetResourcePath("_ref", "save.hg");
        if (savePath == null) return; // Skip if reference save not available

        var mapperPath = GetResourcePath("Resources", "map", "mapping.json");
        if (mapperPath == null) return;

        var mapper = new JsonNameMapper();
        mapper.Load(mapperPath);
        JsonParser.SetDefaultMapper(mapper);

        var save = SaveFileManager.LoadSaveFile(savePath);
        var metaInfo = MetaFileWriter.ExtractMetaInfo(save);

        // Should detect game mode from DifficultyState when PresetGameMode is absent
        Assert.True(metaInfo.GameMode > 0,
            $"GameMode should be detected from DifficultyState, got {metaInfo.GameMode}");
    }

    [Theory]
    [InlineData("Normal", 1)]
    [InlineData("Survival", 2)]
    [InlineData("Permadeath", 3)]
    [InlineData("Creative", 4)]
    [InlineData("Custom", 5)]
    [InlineData("Relaxed", 7)]
    [InlineData("Invalid", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void PresetToGameMode_MapsPresets(string? preset, int expected)
    {
        Assert.Equal(expected, MainStatsLogic.PresetToGameMode(preset));
    }

    [Fact]
    public void ApplyGameModeForPreset_WritesBaseContextGameMode()
    {
        // Modern save: mode lives on BaseContext.GameMode as an integer.
        string json = """{"BaseContext":{"GameMode":3,"PlayerStateData":{}},"PlayerStateData":{}}""";
        var save = JsonObject.Parse(json);

        Assert.True(MainStatsLogic.ApplyGameModeForPreset(save, "Creative"));
        Assert.Equal(4, save.GetValue("BaseContext.GameMode"));
    }

    [Fact]
    public void ApplyGameModeForPreset_WritesLegacyPresetGameMode()
    {
        // Legacy pre-context save: mode lives on PlayerStateData.PresetGameMode as a string.
        string json = """{"PlayerStateData":{"PresetGameMode":"Permadeath"}}""";
        var save = JsonObject.Parse(json);

        Assert.True(MainStatsLogic.ApplyGameModeForPreset(save, "Creative"));
        Assert.Equal("Creative", save.GetValue("PlayerStateData.PresetGameMode"));
    }

    [Fact]
    public void ApplyGameModeForPreset_InvalidPresetLeavesSaveUntouched()
    {
        string json = """{"BaseContext":{"GameMode":3}}""";
        var save = JsonObject.Parse(json);

        Assert.False(MainStatsLogic.ApplyGameModeForPreset(save, "Invalid"));
        Assert.Equal(3, save.GetValue("BaseContext.GameMode"));
    }

    [Fact]
    public void ExtractMetaInfo_PrefersContextGameModeOverDifficultyPreset()
    {
        // A Normal-mode save with a Creative difficulty preset must report mode 1,
        // matching what the game writes into its manifest (not the difficulty).
        string json = """{"BaseContext":{"GameMode":1},"PlayerStateData":{"DifficultyState":{"Preset":{"DifficultyPresetType":"Creative"}}}}""";
        var save = JsonObject.Parse(json);

        var metaInfo = MetaFileWriter.ExtractMetaInfo(save);
        Assert.Equal(1, metaInfo.GameMode);
    }

    [Fact]
    public void ExtractMetaInfo_ReadsGameModeFromExpeditionContext()
    {
        string json = """{"ActiveContext":"Season","ExpeditionContext":{"GameMode":6},"PlayerStateData":{}}""";
        var save = JsonObject.Parse(json);

        var metaInfo = MetaFileWriter.ExtractMetaInfo(save);
        Assert.Equal(6, metaInfo.GameMode);
        Assert.True(metaInfo.IsExpedition);
    }

    [Fact]
    public void ExtractMetaInfo_ExpeditionFlagIsFalseForMainContext()
    {
        string json = """{"ActiveContext":"Main","BaseContext":{"GameMode":1},"PlayerStateData":{}}""";
        var save = JsonObject.Parse(json);

        var metaInfo = MetaFileWriter.ExtractMetaInfo(save);
        Assert.False(metaInfo.IsExpedition);
    }

    [Fact]
    public void DetectGameModeFromJson_ScansContextIntPastContainers()
    {
        // The GameMode key is used both as a container (with PresetGameMode inside)
        // and as the integer mode field.  The scanner must skip the container.
        string obfuscated = """{"idA":{"pwt":"Unspecified"},"BaseContext":{"idA":3}}""";
        Assert.Equal(3, SaveFileManager.DetectGameModeFromJson(obfuscated));

        string plain = """{"GameMode":{"PresetGameMode":"Unspecified"},"BaseContext":{"GameMode":1}}""";
        Assert.Equal(1, SaveFileManager.DetectGameModeFromJson(plain));
    }

    [Fact]
    public void DetectGameModeFromJson_LegacyPresetGameModeStillWins()
    {
        // Legacy saves carry the mode as a string on PlayerStateData.PresetGameMode.
        string json = """{"PlayerStateData":{"PresetGameMode":"Survival"}}""";
        Assert.Equal(2, SaveFileManager.DetectGameModeFromJson(json));
    }

    // --- Meta difficulty tag preservation ---

    private const int MetaDifficultyOffset = 344;
    private const int MetaDifficultyTagOffset = 364;
    private const int MetaSlot = 10;
    private const int MetaBaseVersion = 5000; // META_FORMAT_4 (Worlds Part II) so the tag is written

    private static (int Preset, string Tag) WriteAndReadMeta(string savePath,
        int preset, string tag, bool isExpedition = false)
    {
        MetaFileWriter.WriteSteamMeta(savePath, [0xAA, 0xBB], 2,
            new SaveMetaInfo
            {
                BaseVersion = MetaBaseVersion,
                DifficultyPreset = preset,
                DifficultyPresetTag = tag,
                IsExpedition = isExpedition,
            },
            MetaSlot);

        var meta = MetaFileWriter.ReadSteamMeta(savePath, MetaSlot);
        Assert.NotNull(meta);
        byte[] bytes = MetaFileWriter.UIntsToBytes(meta!);
        int readPreset = BitConverter.ToInt32(bytes, MetaDifficultyOffset);
        string readTag = System.Text.Encoding.ASCII
            .GetString(bytes, MetaDifficultyTagOffset, 64).TrimEnd('\0');
        return (readPreset, readTag);
    }

    [Fact]
    public void WriteSteamMeta_PreservesNonStandardDifficultyTag()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "save9.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            // Original alternate mode (Abandoned Universe = preset 6 / Permadeath).
            WriteAndReadMeta(savePath, 6, "Abandoned Universe");

            // The save data only reports "Custom" (as alternate modes do); the meta tag must survive.
            var (preset, tag) = WriteAndReadMeta(savePath, 1, "Custom");

            Assert.Equal(6, preset);
            Assert.Equal("Abandoned Universe", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteSteamMeta_PreservesExpeditionTag()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "save9.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            WriteAndReadMeta(savePath, 258, "Community Expedition");

            // Expedition save data reports Normal, but the meta's mode tag must survive.
            var (preset, tag) = WriteAndReadMeta(savePath, 2, "Normal", isExpedition: true);

            Assert.Equal(258, preset);
            Assert.Equal("Community Expedition", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteSteamMeta_AllowsStandardDifficultyChange()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "save2.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            WriteAndReadMeta(savePath, 2, "Normal");

            // Changing a standard preset to Custom must still update the meta.
            var (preset, tag) = WriteAndReadMeta(savePath, 1, "Custom");

            Assert.Equal(1, preset);
            Assert.Equal("Custom", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WriteSteamMeta_AllowsChangeFromNonStandardToStandard()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "save9.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            WriteAndReadMeta(savePath, 6, "Abandoned Universe");

            // A deliberate change to a standard preset wins over preservation.
            var (preset, tag) = WriteAndReadMeta(savePath, 2, "Normal");

            Assert.Equal(2, preset);
            Assert.Equal("Normal", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // --- Switch / PS4 manifest difficulty preservation ---

    private static void WriteSwitchManifest(string savePath, int preset)
    {
        MetaFileWriter.WriteSwitchMeta(savePath, 1000,
            new SaveMetaInfo { BaseVersion = MetaBaseVersion, DifficultyPreset = preset },
            9);
    }

    private static int ReadSwitchManifestPreset(string savePath)
    {
        string dir = Path.GetDirectoryName(savePath)!;
        byte[] bytes = File.ReadAllBytes(Path.Combine(dir, "manifest09.hg"));
        Assert.True(bytes.Length >= 300);
        return BitConverter.ToInt32(bytes, 296);
    }

    private static void WritePs4Manifest(string savePath, int preset, string tag,
        bool isExpedition = false)
    {
        MetaFileWriter.WritePlaystationStreamingMeta(savePath, 1000,
            new SaveMetaInfo
            {
                BaseVersion = MetaBaseVersion,
                DifficultyPreset = preset,
                DifficultyPresetTag = tag,
                IsExpedition = isExpedition,
            },
            9);
    }

    private static (int Preset, string Tag) ReadPs4Manifest(string savePath)
    {
        string dir = Path.GetDirectoryName(savePath)!;
        byte[] bytes = File.ReadAllBytes(Path.Combine(dir, "manifest09.hg"));
        Assert.True(bytes.Length >= 380);
        int preset = BitConverter.ToInt32(bytes, 296);
        string tag = System.Text.Encoding.ASCII.GetString(bytes, 316, 64).TrimEnd('\0');
        return (preset, tag);
    }

    [Fact]
    public void WriteSwitchMeta_PreservesAlternateDifficultyPreset()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "savedata09.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            // Alternate modes record a standard-looking preset (Abandoned Universe = 6).
            WriteSwitchManifest(savePath, 6);

            // The save data only reports "Custom" (1); the manifest preset must survive.
            WriteSwitchManifest(savePath, 1);

            Assert.Equal(6, ReadSwitchManifestPreset(savePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WritePlaystationStreamingMeta_PreservesNonStandardDifficultyTag()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "savedata09.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            WritePs4Manifest(savePath, 6, "Abandoned Universe");

            // The save data only reports "Custom"; the manifest tag must survive.
            WritePs4Manifest(savePath, 1, "Custom");

            var (preset, tag) = ReadPs4Manifest(savePath);
            Assert.Equal(6, preset);
            Assert.Equal("Abandoned Universe", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WritePlaystationStreamingMeta_PreservesExpeditionTag()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "savedata09.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            WritePs4Manifest(savePath, 258, "Community Expedition");

            // Expedition save data reports Normal, but the manifest's mode tag must survive.
            WritePs4Manifest(savePath, 2, "Normal", isExpedition: true);

            var (preset, tag) = ReadPs4Manifest(savePath);
            Assert.Equal(258, preset);
            Assert.Equal("Community Expedition", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void WritePlaystationStreamingMeta_AllowsStandardDifficultyChange()
    {
        string root = Path.Combine(Path.GetTempPath(), $"nmse_meta_{Guid.NewGuid()}");
        Directory.CreateDirectory(root);
        try
        {
            string savePath = Path.Combine(root, "savedata09.hg");
            File.WriteAllBytes(savePath, [0x01, 0x02, 0x03]);

            WritePs4Manifest(savePath, 2, "Normal");

            // Changing a standard preset to Custom must still update the manifest.
            WritePs4Manifest(savePath, 1, "Custom");

            var (preset, tag) = ReadPs4Manifest(savePath);
            Assert.Equal(1, preset);
            Assert.Equal("Custom", tag);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
