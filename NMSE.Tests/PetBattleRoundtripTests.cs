using NMSE.Models;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace NMSE.Tests;

/// <summary>
/// Manual roundtrip test to verify pet battle data integrity against actual save files.
/// These tests load real save JSON exports and verify the exact key names and data structures.
/// </summary>
public class PetBattleRoundtripTests
{
    private readonly ITestOutputHelper _output;
    private const string BasePath = "../../../../../_ref/pet_new";
    private const string NewKeyBasePath = "../../../../../_ref/pet_new_key";

    public PetBattleRoundtripTests(ITestOutputHelper output) { _output = output; }

    private JsonObject? LoadSave(string filename, string basePath = BasePath)
    {
        var path = Path.Combine(basePath, filename);
        if (!File.Exists(path))
            return null; // Skip if reference save not available
        _output.WriteLine($"Loading: {filename}");
        string json = File.ReadAllText(path);
        var result = JsonParser.ParseObject(json);
        Assert.NotNull(result);
        return result;
    }

    [Fact]
    public void ModifiedBattles_AllBattleKeysReadCorrectly()
    {
        var save = LoadSave("modified_pet_battles.json");
        if (save == null) return; // Skip if reference save not available
        var pets = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!.GetArray("Pets")!;
        Assert.True(pets.Length >= 5, $"Pets array should have entries, got {pets.Length}");

        var pet0 = pets.GetObject(0)!;

        // 1. PetBattlerUseCoreStatClassOverrides (bool)
        bool useOverrides = pet0.GetBool("PetBattlerUseCoreStatClassOverrides");
        _output.WriteLine($"PetBattlerUseCoreStatClassOverrides = {useOverrides}");

        // 2. PetBattlerCoreStatClassOverrides (array of 3 InventoryClass objects)
        var overrides = pet0.GetArray("PetBattlerCoreStatClassOverrides");
        Assert.NotNull(overrides);
        Assert.Equal(3, overrides!.Length);
        for (int i = 0; i < 3; i++)
        {
            var obj = overrides.GetObject(i);
            Assert.NotNull(obj);
            string? invClass = obj!.GetString("InventoryClass");
            Assert.NotNull(invClass);
            Assert.Contains(invClass, new[] { "C", "B", "A", "S" });
            _output.WriteLine($"  Override[{i}].InventoryClass = \"{invClass}\"");
        }

        // 3. Verify key WITHOUT 's' does NOT resolve
        var noS = pet0.GetArray("PetBattlerCoreStatClassOverride");
        Assert.Null(noS);
        _output.WriteLine("PetBattlerCoreStatClassOverride (no 's') correctly returns null");

        // 4. PetBattlerTreatsEaten (array of 3 ints)
        var treats = pet0.GetArray("PetBattlerTreatsEaten");
        Assert.NotNull(treats);
        Assert.Equal(3, treats!.Length);

        // 5. PetBattlerTreatsAvailable (int)
        int treatsAvail = pet0.GetInt("PetBattlerTreatsAvailable");
        _output.WriteLine($"PetBattlerTreatsAvailable = {treatsAvail}");

        // 6. PetBattleProgressToTreat (double)
        double progress = pet0.GetDouble("PetBattleProgressToTreat");
        _output.WriteLine($"PetBattleProgressToTreat = {progress}");

        // 7. PetBattlerVictories (int)
        int victories = pet0.GetInt("PetBattlerVictories");
        _output.WriteLine($"PetBattlerVictories = {victories}");

        // 8. PetBattlerMoveList (legacy, array of 5 move objects)
        var moveList = pet0.GetArray("PetBattlerMoveList");
        Assert.NotNull(moveList);
        Assert.Equal(5, moveList!.Length);
        for (int i = 0; i < 5; i++)
        {
            var moveObj = moveList.GetObject(i);
            Assert.NotNull(moveObj);
            string? tid = moveObj!.GetString("MoveTemplateID");
            Assert.NotNull(tid);
            Assert.StartsWith("^", tid);
            int cd = moveObj.GetInt("Cooldown");
            double sb = moveObj.GetDouble("ScoreBoost");
            _output.WriteLine($"  MoveList[{i}]: MoveTemplateID=\"{tid}\", Cooldown={cd}, ScoreBoost={sb}");
        }
    }

    [Fact]
    public void ModifiedAccessories_PetAccessoryCustomisationReadCorrectly()
    {
        var save = LoadSave("modified_pet_accesories.json");
        if (save == null) return; // Skip if reference save not available
        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;

        var pac = psd.GetArray("PetAccessoryCustomisation");
        Assert.NotNull(pac);
        Assert.Equal(30, pac!.Length);

        // Check first entry (default)
        var pac0 = pac.GetObject(0)!;
        var data0 = pac0.GetArray("Data")!;
        Assert.Equal(3, data0.Length);

        for (int s = 0; s < 3; s++)
        {
            var slot = data0.GetObject(s)!;
            string? preset = slot.GetString("SelectedPreset");
            Assert.NotNull(preset);
            _output.WriteLine($"  PAC[0].Slot[{s}].SelectedPreset = \"{preset}\"");

            var customData = slot.GetObject("CustomData")!;
            Assert.NotNull(customData);
            var dg = customData.GetArray("DescriptorGroups");
            Assert.NotNull(dg);
            var colours = customData.GetArray("Colours");
            Assert.NotNull(colours);
            double scale = customData.GetDouble("Scale");
            _output.WriteLine($"    DescriptorGroups.Len={dg!.Length}, Colours.Len={colours!.Length}, Scale={scale}");
        }

        // Check PAC[5] which should have colour data
        var pac5 = pac.GetObject(5)!;
        var data5 = pac5.GetArray("Data")!;
        var slot0 = data5.GetObject(0)!;
        var cd5 = slot0.GetObject("CustomData")!;
        var colours5 = cd5.GetArray("Colours")!;
        Assert.True(colours5.Length > 0, "PAC[5].Slot[0] should have colour data");
        _output.WriteLine($"  PAC[5].Slot[0].Colours.Length = {colours5.Length}");

        var colourEntry = colours5.GetObject(0)!;
        var palette = colourEntry.GetObject("Palette")!;
        Assert.NotNull(palette);
        string? palName = palette.GetString("Palette");
        Assert.NotEmpty(palName!);
        _output.WriteLine($"    Palette.Palette = \"{palName}\"");
    }

    [Fact]
    public void WriteBack_ClassOverridesRoundTrip()
    {
        var save = LoadSave("modified_pet_battles.json");
        if (save == null) return; // Skip if reference save not available
        var pet0 = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!.GetArray("Pets")!.GetObject(0)!;
        var overrides = pet0.GetArray("PetBattlerCoreStatClassOverrides")!;

        // Read original
        var obj0 = overrides.GetObject(0)!;
        string? origVal = obj0.GetString("InventoryClass");
        _output.WriteLine($"Before write: {origVal}");

        // Write
        obj0.Set("InventoryClass", "S");
        Assert.Equal("S", obj0.GetString("InventoryClass"));
        _output.WriteLine("After write: S");

        // Restore
        obj0.Set("InventoryClass", origVal ?? "C");
        Assert.Equal(origVal ?? "C", obj0.GetString("InventoryClass"));
        _output.WriteLine($"After restore: {origVal}");
    }

    [Fact]
    public void WriteBack_MoveListRoundTrip()
    {
        var save = LoadSave("modified_pet_battles.json");
        if (save == null) return; // Skip if reference save not available
        var pet0 = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!.GetArray("Pets")!.GetObject(0)!;
        var moveList = pet0.GetArray("PetBattlerMoveList")!;

        var move0 = moveList.GetObject(0)!;
        string? origTid = move0.GetString("MoveTemplateID");
        int origCd = move0.GetInt("Cooldown");
        double origSb = move0.GetDouble("ScoreBoost");
        _output.WriteLine($"Before: TID={origTid}, CD={origCd}, SB={origSb}");

        // Write new values
        move0.Set("MoveTemplateID", "^TEST_MOVE");
        move0.Set("Cooldown", 5);
        move0.Set("ScoreBoost", 3.14);

        Assert.Equal("^TEST_MOVE", move0.GetString("MoveTemplateID"));
        Assert.Equal(5, move0.GetInt("Cooldown"));
        Assert.Equal(3.14, move0.GetDouble("ScoreBoost"), 2);

        // Restore
        move0.Set("MoveTemplateID", origTid ?? "^");
        move0.Set("Cooldown", origCd);
        move0.Set("ScoreBoost", origSb);
        _output.WriteLine("Roundtrip OK");
    }

    [Fact]
    public void NewKey_PetBattlerMovesIsStringArray()
    {
        var save = LoadSave("new_key.json", NewKeyBasePath);
        if (save == null) return; // Skip if reference save not available
        var pets = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!.GetArray("Pets")!;
        Assert.True(pets.Length >= 1, "Expected at least one pet");

        var pet0 = pets.GetObject(0)!;

        // PetBattlerMoves should be an array of 5 strings
        var moves = pet0.GetArray("PetBattlerMoves");
        Assert.NotNull(moves);
        Assert.Equal(5, moves!.Length);

        for (int i = 0; i < 5; i++)
        {
            string val = moves.GetString(i);
            Assert.NotNull(val);
            Assert.StartsWith("^", val);
            _output.WriteLine($"  PetBattlerMoves[{i}] = \"{val}\"");
        }
    }

    [Fact]
    public void NewKey_LegacyMoveListDefaulted()
    {
        var save = LoadSave("new_key.json", NewKeyBasePath);
        if (save == null) return; // Skip if reference save not available
        var pets = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!.GetArray("Pets")!;

        var pet0 = pets.GetObject(0)!;

        // Legacy PetBattlerMoveList should still exist but with default values
        var moveList = pet0.GetArray("PetBattlerMoveList");
        Assert.NotNull(moveList);
        Assert.Equal(5, moveList!.Length);

        for (int i = 0; i < 5; i++)
        {
            var moveObj = moveList.GetObject(i);
            Assert.NotNull(moveObj);
            Assert.Equal("^", moveObj!.GetString("MoveTemplateID"));
            Assert.Equal(0, moveObj.GetInt("Cooldown"));
            Assert.Equal(0.0, moveObj.GetDouble("ScoreBoost"));
        }

        _output.WriteLine("Legacy PetBattlerMoveList is reset to defaults as expected");
    }

    [Fact]
    public void NewKey_PetBattlerMovesRoundTrip()
    {
        var save = LoadSave("new_key.json", NewKeyBasePath);
        if (save == null) return; // Skip if reference save not available
        var pet0 = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!.GetArray("Pets")!.GetObject(0)!;

        var moves = pet0.GetArray("PetBattlerMoves")!;
        string orig0 = moves.GetString(0);
        _output.WriteLine($"Before: PetBattlerMoves[0] = \"{orig0}\"");

        // Write new value
        moves.Set(0, "^TEST_MOVE");
        Assert.Equal("^TEST_MOVE", moves.GetString(0));
        _output.WriteLine("After write: ^TEST_MOVE");

        // Restore
        moves.Set(0, orig0);
        Assert.Equal(orig0, moves.GetString(0));
        _output.WriteLine($"After restore: \"{orig0}\"");
    }
}
