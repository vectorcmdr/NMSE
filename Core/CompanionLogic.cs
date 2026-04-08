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
    /// Matches the Creature Builder biome list.
    /// </summary>
    internal static readonly string[] BiomeTypes =
    {
        "Lush", "Toxic", "Scorched", "Radioactive", "Frozen", "Barren",
        "Dead", "Weird", "Red", "Green", "Blue", "Test",
        "Swamp", "Lava", "Waterworld", "All"
    };

    internal static readonly Dictionary<string, string> BiomeTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Lush"] = "companion.biome_lush",
        ["Toxic"] = "companion.biome_toxic",
        ["Scorched"] = "companion.biome_scorched",
        ["Radioactive"] = "companion.biome_radioactive",
        ["Frozen"] = "companion.biome_frozen",
        ["Barren"] = "companion.biome_barren",
        ["Dead"] = "companion.biome_dead",
        ["Weird"] = "companion.biome_weird",
        ["Red"] = "companion.biome_red",
        ["Green"] = "companion.biome_green",
        ["Blue"] = "companion.biome_blue",
        ["Test"] = "companion.biome_test",
        ["Swamp"] = "companion.biome_swamp",
        ["Lava"] = "companion.biome_lava",
        ["Waterworld"] = "companion.biome_waterworld",
        ["All"] = "companion.biome_all",
    };

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
    /// Exports a companion's JSON data to a file.
    /// </summary>
    /// <param name="companion">The companion JSON object to export.</param>
    /// <param name="filePath">The destination file path.</param>
    internal static void ExportCompanion(JsonObject companion, string filePath)
    {
        companion.ExportToFile(filePath);
    }

    /// <summary>
    /// Imports a companion from a file into the first empty companion slot.
    /// </summary>
    /// <param name="companions">The JSON array of companion entries.</param>
    /// <param name="filePath">The source file path containing exported companion data.</param>
    /// <returns>The index of the slot the companion was imported into.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no empty companion slot is available.</exception>
    internal static int ImportCompanion(JsonArray companions, string filePath)
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
                    foreach (var name in imported.Names())
                        comp.Set(name, imported.Get(name));
                    return i;
                }
            }
        }

        throw new InvalidOperationException("No empty companion slot available.");
    }

    /// <summary>
    /// Maximum number of pet slots supported by the game (indices 0–29).
    /// </summary>
    internal const int MaxPetSlots = 30;

    /// <summary>
    /// Sets the unlocked state for a companion slot in the UnlockedPetSlots array.
    /// </summary>
    /// <param name="playerState">The PlayerStateData JSON object.</param>
    /// <param name="slotIndex">The pet slot index (0–17).</param>
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
}
