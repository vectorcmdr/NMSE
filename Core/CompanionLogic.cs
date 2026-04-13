using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles companion (pet) data operations including species lookup, deletion, import/export, and accessory management.
/// </summary>
internal static class CompanionLogic
{
    /// <summary>
    /// Known planetary biome types where companions can be found.
    /// Delegates to <see cref="CompanionDatabase.BiomeTypes"/>.
    /// </summary>
    internal static string[] BiomeTypes => CompanionDatabase.BiomeTypes;

    /// <summary>
    /// Mapping of biome type names to their UI localisation keys.
    /// Delegates to <see cref="CompanionDatabase.BiomeTypeLocKeys"/>.
    /// </summary>
    internal static Dictionary<string, string> BiomeTypeLocKeys => CompanionDatabase.BiomeTypeLocKeys;

    /// <summary>
    /// Looks up the species display name for a given species identifier using the companion database.
    /// </summary>
    /// <param name="speciesId">The species identifier string.</param>
    /// <returns>The species name, or an empty string if not found.</returns>
    internal static string LookupSpeciesName(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId) || speciesId == "^") return "";
        if (CompanionDatabase.ById.TryGetValue(speciesId, out var entry))
            return entry.Species;
        return "";
    }

    /// <summary>
    /// Fully resets a companion slot by clearing all fields to default values.
    /// We achieve this by resetting every field in-place so no remnant data is left behind.
    /// </summary>
    /// <param name="companion">The companion JSON object to clear.</param>
    internal static void DeleteCompanion(JsonObject companion)
    {
        // Identification fields
        try { companion.Set("CreatureID", "^"); } catch { }
        try { companion.Set("CustomName", ""); } catch { }
        try { companion.Set("CustomSpeciesName", "^"); } catch { }

        // Creature seed arrays [false, "0x0"]
        ClearSeedArray(companion, "CreatureSeed");
        ClearSeedArray(companion, "CreatureSecondarySeed");
        ClearSeedArray(companion, "ColourBaseSeed");
        ClearSeedArray(companion, "BoneScaleSeed");

        // SpeciesSeed and GenusSeed are plain integers, not seed arrays
        try { companion.Set("SpeciesSeed", 0); } catch { }
        try { companion.Set("GenusSeed", 0); } catch { }

        // Numeric/boolean fields
        try { companion.Set("Scale", 1.0); } catch { }
        try { companion.Set("Trust", 0.0); } catch { }
        try { companion.Set("Predator", false); } catch { }
        try { companion.Set("HasFur", false); } catch { }
        // UA (Universal Address) defaults to 1111111111111111 per the NMSCD Creature Builder
        try { companion.Set("UA", 1111111111111111L); } catch { }
        try { companion.Set("AllowUnmodifiedReroll", true); } catch { }
        try { companion.Set("EggModified", false); } catch { }
        try { companion.Set("HasBeenSummoned", true); } catch { }

        // Timestamps
        try { companion.Set("BirthTime", 0); } catch { }
        try { companion.Set("LastEggTime", 0); } catch { }
        try { companion.Set("LastTrustIncreaseTime", 0); } catch { }
        try { companion.Set("LastTrustDecreaseTime", 0); } catch { }

        // Nested objects
        try
        {
            var biome = companion.GetObject("Biome");
            biome?.Set("Biome", "Lush");
        }
        catch { }

        try
        {
            var creatureType = companion.GetObject("CreatureType");
            creatureType?.Set("CreatureType", "None");
        }
        catch { }

        // SenderData - clear all network fields
        try
        {
            var sender = companion.GetObject("SenderData");
            if (sender != null)
            {
                sender.Set("LID", "");
                sender.Set("UID", "");
                sender.Set("USN", "");
                sender.Set("PTK", "");
                sender.Set("TS", 0);
            }
        }
        catch { }

        // Descriptors array - clear all entries
        ClearJsonArray(companion.GetArray("Descriptors"));

        // Trait values -> 0.0 (3 elements)
        try
        {
            var traits = companion.GetArray("Traits");
            if (traits != null)
            {
                for (int i = 0; i < traits.Length; i++)
                    traits.Set(i, 0.0);
            }
        }
        catch { }

        // Mood values -> 0.0 (2 elements)
        try
        {
            var moods = companion.GetArray("Moods");
            if (moods != null)
            {
                for (int i = 0; i < moods.Length; i++)
                    moods.Set(i, 0.0);
            }
        }
        catch { }

        // Reset accessory customisation
        ResetAccessoryCustomisation(companion);

        // Reset battle data fields
        ResetBattleData(companion);
    }

    /// <summary>
    /// Removes all elements from a JSON array.
    /// </summary>
    private static void ClearJsonArray(JsonArray? arr)
    {
        if (arr == null) return;
        for (int i = arr.Length - 1; i >= 0; i--)
            arr.RemoveAt(i);
    }

    /// <summary>
    /// Clears a seed array to its default empty state [false, "0x0"].
    /// </summary>
    private static void ClearSeedArray(JsonObject companion, string key)
    {
        try
        {
            var seedArray = companion.GetArray(key);
            if (seedArray != null && seedArray.Length >= 2)
            {
                seedArray.Set(0, false);
                seedArray.Set(1, "0x0");
            }
        }
        catch { }
    }

    /// <summary>
    /// Maximum number of pet slots supported by the game (indices 0–29).
    /// </summary>
    internal const int MaxPetSlots = 30;

    /// <summary>
    /// Sets the unlocked state for a companion slot in the UnlockedPetSlots array.
    /// </summary>
    /// <param name="playerState">The PlayerStateData JSON object.</param>
    /// <param name="slotIndex">The pet slot index (0–29).</param>
    /// <param name="unlocked">Whether the slot should be unlocked.</param>
    internal static void SetSlotUnlocked(JsonObject playerState, int slotIndex, bool unlocked)
    {
        if (slotIndex < 0 || slotIndex >= MaxPetSlots) return;
        try
        {
            var unlockedSlots = playerState.GetArray("UnlockedPetSlots");
            if (unlockedSlots == null)
            {
                unlockedSlots = new JsonArray();
                playerState.Set("UnlockedPetSlots", unlockedSlots);
            }
            // Grow the array with false values if the slot index exceeds the current length
            while (unlockedSlots.Length <= slotIndex)
                unlockedSlots.Add(false);
            unlockedSlots.Set(slotIndex, unlocked);
        }
        catch { }
    }

    /// <summary>
    /// Resets all accessory customisation fields on a companion to empty strings.
    /// </summary>
    /// <param name="companion">The companion JSON object to reset.</param>
    internal static void ResetAccessoryCustomisation(JsonObject companion)
    {
        try
        {
            var accessory = companion.GetObject("PetAccessoryCustomisation");
            if (accessory != null)
            {
                foreach (var name in accessory.Names())
                    accessory.Set(name, "");
            }
        }
        catch { }
    }

    //  Pet Battle Data Helpers

    /// <summary>Names of all battle-related keys stored in a companion JSON object.</summary>
    internal static readonly string[] BattleKeys =
    {
        "PetBattlerUseCoreStatClassOverrides",
        "PetBattlerCoreStatClassOverrides",
        "PetBattlerTreatsEaten",
        "PetBattlerTreatsAvailable",
        "PetBattleProgressToTreat",
        "PetBattlerVictories",
        "PetBattlerMoveList",
    };

    /// <summary>
    /// Resets all pet battle data fields to their default state.
    /// </summary>
    internal static void ResetBattleData(JsonObject companion)
    {
        try { companion.Set("PetBattlerUseCoreStatClassOverrides", false); } catch { }

        // PetBattlerCoreStatClassOverrides: array of 3 InventoryClass objects -> "C"
        try
        {
            var overrides = companion.GetArray("PetBattlerCoreStatClassOverrides");
            if (overrides != null)
                for (int i = 0; i < overrides.Length; i++)
                {
                    var obj = overrides.GetObject(i);
                    obj?.Set("InventoryClass", "C");
                }
        }
        catch { }

        // PetBattlerTreatsEaten: array of 3 integers -> 0
        try
        {
            var treats = companion.GetArray("PetBattlerTreatsEaten");
            if (treats != null)
                for (int i = 0; i < treats.Length; i++)
                    treats.Set(i, 0);
        }
        catch { }

        try { companion.Set("PetBattlerTreatsAvailable", 0); } catch { }
        try { companion.Set("PetBattleProgressToTreat", 0.0); } catch { }
        try { companion.Set("PetBattlerVictories", 0); } catch { }

        // PetBattlerMoveList: array of 5 move objects -> reset MoveTemplateID/Cooldown/ScoreBoost
        try
        {
            var moveList = companion.GetArray("PetBattlerMoveList");
            if (moveList != null)
                for (int i = 0; i < moveList.Length; i++)
                {
                    var obj = moveList.GetObject(i);
                    if (obj != null)
                    {
                        obj.Set("MoveTemplateID", "^");
                        obj.Set("Cooldown", 0);
                        obj.Set("ScoreBoost", 0.0);
                    }
                }
        }
        catch { }
    }

    /// <summary>
    /// Exports a companion to file, including all battle and accessory data.
    /// The exported JSON will contain the full companion object as-is.
    /// </summary>
    internal static void ExportCompanion(JsonObject companion, string filePath,
        JsonArray? petAccessoryCustomisationSlots = null)
    {
        // If we have accessory customisation data, include it in the export
        if (petAccessoryCustomisationSlots != null)
        {
            try
            {
                companion.Set("PetAccessoryCustomisation", petAccessoryCustomisationSlots);
            }
            catch { }
        }

        companion.ExportToFile(filePath);
    }

    /// <summary>
    /// Imports a companion from a file into the first empty companion slot.
    /// Handles all battle keys and accessory customisation data.
    /// </summary>
    internal static int ImportCompanion(JsonArray companions, string filePath,
        JsonArray? petAccessoryCustomisationArray = null)
    {
        var imported = JsonObject.ImportFromFile(filePath);

        // Unwrap NomNom wrapper if present (Data -> Pet + AccessoryCustomisation)
        imported = InventoryImportHelper.UnwrapNomNomCompanion(imported);

        for (int i = 0; i < companions.Length; i++)
        {
            var comp = companions.GetObject(i);
            var seedArray = comp.GetArray("CreatureSeed");
            if (seedArray != null && seedArray.Length >= 2)
            {
                bool occupied;
                try { occupied = seedArray.GetBool(0); } catch { occupied = false; }
                if (!occupied)
                {
                    // Merge all fields from the imported companion
                    foreach (var name in imported.Names())
                    {
                        // PetAccessoryCustomisation is handled separately
                        if (name == "PetAccessoryCustomisation") continue;
                        comp.Set(name, imported.Get(name));
                    }

                    // If the imported data has PetAccessoryCustomisation and we have the target array
                    if (petAccessoryCustomisationArray != null)
                    {
                        try
                        {
                            var importedAcc = imported.GetArray("PetAccessoryCustomisation");
                            if (importedAcc == null)
                                importedAcc = imported.Get("PetAccessoryCustomisation") as JsonArray;

                            if (importedAcc != null && i < petAccessoryCustomisationArray.Length)
                            {
                                petAccessoryCustomisationArray.Set(i, importedAcc);
                            }
                        }
                        catch { }
                    }

                    return i;
                }
            }
        }

        throw new InvalidOperationException("No empty companion slot available.");
    }
}
