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
}
