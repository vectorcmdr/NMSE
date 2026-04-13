using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles squadron pilot data operations including race/ship type lookups, seed management, and pilot deletion.
/// </summary>
internal static class SquadronLogic
{
    /// <summary>
    /// Selectable pilot race names for the squadron editor.
    /// </summary>
    internal static readonly string[] PilotRaces = { "Gek", "Vy'keen", "Korvax" };

    internal static readonly Dictionary<string, string> PilotRaceLocKeys = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["Gek"] = "common.race_gek",
        ["Vy'keen"] = "common.race_vykeen",
        ["Korvax"] = "common.race_korvax",
    };

    internal static string GetLocalisedPilotRaceName(string internalName)
    {
        if (PilotRaceLocKeys.TryGetValue(internalName, out var key))
            return UiStrings.Get(key);
        return internalName;
    }

    /// <summary>
    /// Pilot rank grades, ordered from lowest to highest.
    /// </summary>
    internal static readonly string[] PilotRanks = { "C", "B", "A", "S" };

    // NPC resource filename to race name mapping
    private static readonly Dictionary<string, string> NpcResourceToRace = new(StringComparer.OrdinalIgnoreCase)
    {
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN", "Gek" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCVYKEEN.SCENE.MBIN", "Vy'keen" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCKORVAX.SCENE.MBIN", "Korvax" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCFOURTH.SCENE.MBIN", "Traveller" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCFIFTH.SCENE.MBIN", "Iteration" },
    };

    // Race name to NPC resource filename mapping (for writing)
    private static readonly Dictionary<string, string> RaceToNpcResource = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Gek", "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN" },
        { "Vy'keen", "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCVYKEEN.SCENE.MBIN" },
        { "Korvax", "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCKORVAX.SCENE.MBIN" },
    };

    /// <summary>
    /// Maps ship model resource filenames to ship type display names.
    /// </summary>
    internal static readonly Dictionary<string, string> ShipResourceToType = new(StringComparer.OrdinalIgnoreCase)
    {
        { "MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN", "Hauler" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN", "Fighter" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERCLASSICGOLD.SCENE.MBIN", "Golden Vector" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERSPECIALSWITCH.SCENE.MBIN", "Horizon Omega" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/VRSPEEDER.SCENE.MBIN", "Honmatan LS7" },
        { "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN", "Explorer" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/SPOOKSHIP.SCENE.MBIN", "Boundary Herald" },
        { "MODELS/COMMON/SPACECRAFT/SHUTTLE/SHUTTLE_PROC.SCENE.MBIN", "Shuttle" },
        { "MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN", "Exotic" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACER.SCENE.MBIN", "Starborn Runner" },
        { "MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACERSE.SCENE.MBIN", "Starborn Phoenix" },
        { "MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOSHIP_PROC.SCENE.MBIN", "Living Ship" },
        { "MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOFIGHTER.SCENE.MBIN", "Wraith" },
        { "MODELS/COMMON/SPACECRAFT/SAILSHIP/SAILSHIP_PROC.SCENE.MBIN", "Solar" },
        { "MODELS/COMMON/SPACECRAFT/SENTINELSHIP/SENTINELSHIP_PROC.SCENE.MBIN", "Interceptor" },
        { "MODELS/COMMON/SPACECRAFT/BIGGS/BIGGS.SCENE.MBIN", "Corvette" },
    };

    /// <summary>
    /// Maps ship type display names to their primary resource filenames, for setting a pilot's ship type.
    /// </summary>
    internal static readonly Dictionary<string, string> ShipTypeToResource = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Hauler", "MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN" },
        { "Fighter", "MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN" },
        { "Explorer", "MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN" },
        { "Shuttle", "MODELS/COMMON/SPACECRAFT/SHUTTLE/SHUTTLE_PROC.SCENE.MBIN" },
        { "Exotic", "MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN" },
        { "Living Ship", "MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOSHIP_PROC.SCENE.MBIN" },
        { "Solar", "MODELS/COMMON/SPACECRAFT/SAILSHIP/SAILSHIP_PROC.SCENE.MBIN" },
        { "Interceptor", "MODELS/COMMON/SPACECRAFT/SENTINELSHIP/SENTINELSHIP_PROC.SCENE.MBIN" },
        { "Corvette", "MODELS/COMMON/SPACECRAFT/BIGGS/BIGGS.SCENE.MBIN" },
    };

    /// <summary>
    /// Gets an array of ShipTypeItem wrappers for squadron ship type combo boxes.
    /// Each item displays a localised name but carries the English name for data lookups.
    /// </summary>
    internal static StarshipLogic.ShipTypeItem[] GetShipTypeItems()
    {
        return ShipTypeToResource.Keys
            .OrderBy(n => StarshipLogic.GetLocalisedShipTypeName(n))
            .Select(n => new StarshipLogic.ShipTypeItem(n, StarshipLogic.GetLocalisedShipTypeName(n)))
            .ToArray();
    }

    /// <summary>
    /// Builds a display name string for a pilot showing their index, race, ship type, and rank.
    /// Returns an "(Empty)" label if the pilot slot has no NPC or ship seed.
    /// </summary>
    /// <param name="pilot">The pilot JSON object.</param>
    /// <param name="index">The pilot's position index in the squadron.</param>
    /// <returns>A formatted display string for the pilot.</returns>
    internal static string GetPilotDisplayName(JsonObject pilot, int index)
    {
        try
        {
            string npcSeed = ReadSeed(pilot, "NPCResource");
            string shipSeed = ReadSeed(pilot, "ShipResource");
            bool hasPilot = npcSeed != "0x0" && shipSeed != "0x0";
            if (!hasPilot) return UiStrings.Format("squadron.empty_slot", index);

            string race = GetPilotRace(pilot);
            string shipType = GetShipType(pilot);
            int rank = 0;
            try { rank = pilot.GetInt("PilotRank"); } catch { }
            string rankStr = rank >= 0 && rank < PilotRanks.Length ? PilotRanks[rank] : "C";
            return UiStrings.Format("squadron.pilot_display", index, race, shipType, rankStr);
        }
        catch { return UiStrings.Format("squadron.pilot_fallback", index); }
    }

    /// <summary>
    /// Gets the race name for a pilot based on their NPC resource filename.
    /// </summary>
    /// <param name="pilot">The pilot JSON object.</param>
    /// <returns>The race name (e.g. "Gek"), or an empty string if unavailable.</returns>
    internal static string GetPilotRace(JsonObject pilot)
    {
        try
        {
            var npcResource = pilot.GetObject("NPCResource");
            string filename = npcResource?.GetString("Filename") ?? "";
            if (string.IsNullOrEmpty(filename)) return "";

            if (NpcResourceToRace.TryGetValue(filename, out string? race))
                return race;

            // Fallback: keyword matching
            if (filename.Contains("GEK", StringComparison.OrdinalIgnoreCase)) return "Gek";
            if (filename.Contains("VYKEEN", StringComparison.OrdinalIgnoreCase)) return "Vy'keen";
            if (filename.Contains("KORVAX", StringComparison.OrdinalIgnoreCase)) return "Korvax";

            return filename;
        }
        catch { return ""; }
    }

    /// <summary>
    /// Gets the ship type display name for a pilot based on their ship resource filename.
    /// </summary>
    /// <param name="pilot">The pilot JSON object.</param>
    /// <returns>The ship type name (e.g. "Fighter"), or an empty string if unavailable.</returns>
    internal static string GetShipType(JsonObject pilot)
    {
        try
        {
            var shipResource = pilot.GetObject("ShipResource");
            string filename = shipResource?.GetString("Filename") ?? "";
            if (string.IsNullOrEmpty(filename)) return "";

            if (ShipResourceToType.TryGetValue(filename, out string? type))
                return type;

            return filename;
        }
        catch { return ""; }
    }

    /// <summary>
    /// Sets a pilot's race by updating their NPC resource filename.
    /// </summary>
    /// <param name="pilot">The pilot JSON object.</param>
    /// <param name="raceName">The race name to set (e.g. "Gek").</param>
    internal static void SetPilotRace(JsonObject pilot, string raceName)
    {
        if (RaceToNpcResource.TryGetValue(raceName, out string? resource))
        {
            try { pilot.GetObject("NPCResource")?.Set("Filename", resource); } catch { }
        }
    }

    /// <summary>
    /// Reads the seed hex string from a nested resource object's Seed array.
    /// </summary>
    /// <param name="parent">The parent JSON object containing the resource.</param>
    /// <param name="objectKey">The key of the nested resource object (e.g. "NPCResource").</param>
    /// <returns>The seed hex string, or "0x0" if not found.</returns>
    internal static string ReadSeed(JsonObject parent, string objectKey)
    {
        try
        {
            var obj = parent.GetObject(objectKey);
            if (obj == null) return "0x0";
            var seedArr = obj.GetArray("Seed");
            if (seedArr != null && seedArr.Length >= 2)
                return seedArr.GetString(1) ?? "0x0";
        }
        catch { }
        return "0x0";
    }

    /// <summary>
    /// Writes a seed hex string to a nested resource object's Seed array.
    /// The seed is validated and normalized before writing.
    /// </summary>
    /// <param name="parent">The parent JSON object containing the resource.</param>
    /// <param name="objectKey">The key of the nested resource object (e.g. "NPCResource").</param>
    /// <param name="value">The seed hex string to write.</param>
    internal static void WriteSeed(JsonObject parent, string objectKey, string value)
    {
        try
        {
            var normalized = SeedHelper.NormalizeSeed(value);
            if (normalized == null) return;
            var obj = parent.GetObject(objectKey);
            if (obj == null) return;
            var seedArr = obj.GetArray("Seed");
            if (seedArr != null && seedArr.Length >= 2)
                seedArr.Set(1, normalized);
        }
        catch { }
    }

    /// <summary>
    /// Deletes a squadron pilot by clearing both NPC and ship resource filenames and seeds.
    /// </summary>
    /// <param name="pilot">The pilot JSON object to clear.</param>
    internal static void DeletePilot(JsonObject pilot)
    {
        try
        {
            var npc = pilot.GetObject("NPCResource");
            if (npc != null)
            {
                npc.Set("Filename", "");
                var npcSeed = npc.GetArray("Seed");
                if (npcSeed != null && npcSeed.Length >= 2)
                {
                    npcSeed.Set(0, false);
                    npcSeed.Set(1, "0x0");
                }
            }

            var ship = pilot.GetObject("ShipResource");
            if (ship != null)
            {
                ship.Set("Filename", "");
                var shipSeed = ship.GetArray("Seed");
                if (shipSeed != null && shipSeed.Length >= 2)
                {
                    shipSeed.Set(0, false);
                    shipSeed.Set(1, "0x0");
                }
            }
        }
        catch { }
    }
}
