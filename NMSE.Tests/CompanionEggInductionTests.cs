using NMSE.Models;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace NMSE.Tests;

/// <summary>
/// Tests for companion egg induction logic, verifying that the reference egg-jimmy.json
/// file correctly represents an induced egg from a pet.
/// These tests gracefully skip if the reference files are not available.
/// </summary>
public class CompanionEggInductionTests
{
    private readonly ITestOutputHelper _output;
    private const string RefPath = "../../../../../_ref/eggs/egg-jimmy.json";

    public CompanionEggInductionTests(ITestOutputHelper output) { _output = output; }

    private JsonObject? LoadReference()
    {
        if (!File.Exists(RefPath)) return null;
        string json = File.ReadAllText(RefPath);
        return JsonParser.ParseObject(json);
    }

    [Fact]
    public void EggReference_PetAndEgg_ShareCreatureIdentity()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        // Creature identity should match between pet and induced egg
        Assert.Equal(pet.GetString("CreatureID"), egg.GetString("CreatureID"));
        Assert.Equal(pet.GetString("CustomName"), egg.GetString("CustomName"));
        Assert.Equal(pet.GetString("SpeciesSeed"), egg.GetString("SpeciesSeed"));
        Assert.Equal(pet.GetString("GenusSeed"), egg.GetString("GenusSeed"));

        _output.WriteLine($"CreatureID: {pet.GetString("CreatureID")}");
        _output.WriteLine($"CustomName: {pet.GetString("CustomName")}");
    }

    [Fact]
    public void EggReference_PetAndEgg_ShareScale()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        Assert.Equal(pet.GetDouble("Scale"), egg.GetDouble("Scale"), 6);
        _output.WriteLine($"Scale: {pet.GetDouble("Scale")}");
    }

    [Fact]
    public void EggReference_PetAndEgg_ShareBiome()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        var petBiome = pet.GetObject("Biome")!.GetString("Biome");
        var eggBiome = egg.GetObject("Biome")!.GetString("Biome");
        Assert.Equal(petBiome, eggBiome);
        _output.WriteLine($"Biome: {petBiome}");
    }

    [Fact]
    public void EggReference_PetAndEgg_ShareDescriptors()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        var petDesc = pet.GetArray("Descriptors")!;
        var eggDesc = egg.GetArray("Descriptors")!;
        Assert.Equal(petDesc.Length, eggDesc.Length);

        for (int i = 0; i < petDesc.Length; i++)
        {
            Assert.Equal(petDesc.GetString(i), eggDesc.GetString(i));
        }
        _output.WriteLine($"Descriptors count: {petDesc.Length}");
    }

    [Fact]
    public void EggReference_PetAndEgg_ShareBattleData()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        // Battle treats should match
        var petTreats = pet.GetArray("PetBattlerTreatsEaten")!;
        var eggTreats = egg.GetArray("PetBattlerTreatsEaten")!;
        Assert.Equal(petTreats.Length, eggTreats.Length);
        for (int i = 0; i < petTreats.Length; i++)
        {
            Assert.Equal(petTreats.GetInt(i), eggTreats.GetInt(i));
        }

        // Move list should match
        var petMoves = pet.GetArray("PetBattlerMoveList")!;
        var eggMoves = egg.GetArray("PetBattlerMoveList")!;
        Assert.Equal(petMoves.Length, eggMoves.Length);
        for (int i = 0; i < petMoves.Length; i++)
        {
            var pm = petMoves.GetObject(i)!;
            var em = eggMoves.GetObject(i)!;
            Assert.Equal(pm.GetString("MoveTemplateID"), em.GetString("MoveTemplateID"));
        }

        _output.WriteLine($"Treats: {petTreats.Length}, Moves: {petMoves.Length}");
    }

    [Fact]
    public void EggReference_EggHasBeenSummoned_IsFalse()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        Assert.False(egg.GetBool("HasBeenSummoned"));
        _output.WriteLine("HasBeenSummoned = false (correct for egg)");
    }

    [Fact]
    public void EggReference_PetAndEgg_ShareTraits()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        var petTraits = pet.GetArray("Traits")!;
        var eggTraits = egg.GetArray("Traits")!;
        Assert.Equal(petTraits.Length, eggTraits.Length);
        for (int i = 0; i < petTraits.Length; i++)
        {
            Assert.Equal(petTraits.GetDouble(i), eggTraits.GetDouble(i), 6);
        }
        _output.WriteLine($"Traits matched ({petTraits.Length} elements)");
    }

    [Fact]
    public void EggReference_EggBirthTime_IsAfterPetBirthTime()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        long petBirth = pet.GetLong("BirthTime");
        long eggBirth = egg.GetLong("BirthTime");

        // Egg birth time should be after pet birth time (egg was induced later)
        Assert.True(eggBirth > petBirth,
            $"Egg BirthTime ({eggBirth}) should be after Pet BirthTime ({petBirth})");

        _output.WriteLine($"Pet BirthTime: {petBirth}, Egg BirthTime: {eggBirth}");
    }

    [Fact]
    public void EggReference_EggLastEggTime_EqualsPetBirthTime()
    {
        var save = LoadReference();
        if (save == null) { _output.WriteLine("Reference file not found, skipping."); return; }

        var psd = save.GetObject("BaseContext")!.GetObject("PlayerStateData")!;
        var pet = psd.GetArray("Pets")!.GetObject(1)!;
        var egg = psd.GetArray("Eggs")!.GetObject(10)!;

        long petBirth = pet.GetLong("BirthTime");
        long eggLastEgg = egg.GetLong("LastEggTime");

        // Egg's LastEggTime should equal the pet's BirthTime
        Assert.Equal(petBirth, eggLastEgg);

        _output.WriteLine($"Pet BirthTime: {petBirth}, Egg LastEggTime: {eggLastEgg}");
    }
}
