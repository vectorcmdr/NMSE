using System.Linq;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles freighter data operations including loading, saving, type/class management, and crew race lookups.
/// </summary>
internal static class FreighterLogic
{
    /// <summary>
    /// Available freighter class grades, ordered from lowest to highest.
    /// </summary>
    internal static readonly string[] FreighterClasses = { "C", "B", "A", "S" };

    /// <summary>
    /// Known freighter crew race names.
    /// </summary>
    internal static readonly string[] CrewRaces = { "Gek", "Vy'keen", "Korvax" };

    internal static readonly Dictionary<string, string> CrewRaceLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gek"] = "common.race_gek",
        ["Vy'keen"] = "common.race_vykeen",
        ["Korvax"] = "common.race_korvax",
    };

    internal static string GetLocalisedCrewRaceName(string internalName)
    {
        if (CrewRaceLocKeys.TryGetValue(internalName, out var key))
            return UiStrings.Get(key);
        return internalName;
    }

    internal sealed class CrewRaceItem
    {
        public string InternalName { get; }
        public string DisplayName { get; }
        public CrewRaceItem(string internalName, string displayName) { InternalName = internalName; DisplayName = displayName; }
        public override string ToString() => DisplayName;
    }

    internal static CrewRaceItem[] GetCrewRaceItems()
    {
        return CrewRaces.Select(r => new CrewRaceItem(r, GetLocalisedCrewRaceName(r))).ToArray();
    }

    /// <summary>
    /// Maps NPC model resource filenames to their race display name.
    /// </summary>
    internal static readonly Dictionary<string, string> NpcResourceToRace = new(StringComparer.OrdinalIgnoreCase)
    {
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN", "Gek" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCVYKEEN.SCENE.MBIN", "Vy'keen" },
        { "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCKORVAX.SCENE.MBIN", "Korvax" },
    };

    /// <summary>
    /// Maps race display names to their NPC model resource filenames.
    /// </summary>
    internal static readonly Dictionary<string, string> RaceToNpcResource = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Gek", "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCGEK.SCENE.MBIN" },
        { "Vy'keen", "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCVYKEEN.SCENE.MBIN" },
        { "Korvax", "MODELS/COMMON/PLAYER/PLAYERCHARACTER/NPCKORVAX.SCENE.MBIN" },
    };

    /// <summary>
    /// Maps freighter type display names to their model resource filenames.
    /// </summary>
    internal static readonly Dictionary<string, string> FreighterTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Tiny",    "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/FREIGHTERTINY_PROC.SCENE.MBIN" },
        { "Small",   "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/FREIGHTERSMALL_PROC.SCENE.MBIN" },
        { "Normal",  "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/FREIGHTER_PROC.SCENE.MBIN" },
        { "Capital", "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/CAPITALFREIGHTER_PROC.SCENE.MBIN" },
        { "Pirate",  "MODELS/COMMON/SPACECRAFT/INDUSTRIAL/PIRATEFREIGHTER.SCENE.MBIN" }
    };

    internal static readonly Dictionary<string, string> FreighterTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Tiny"] = "freighter.type_tiny",
        ["Small"] = "freighter.type_small",
        ["Normal"] = "freighter.type_normal",
        ["Capital"] = "freighter.type_capital",
        ["Pirate"] = "freighter.type_pirate",
    };

    internal static string GetLocalisedFreighterTypeName(string internalName)
    {
        if (FreighterTypeLocKeys.TryGetValue(internalName, out var key))
            return UiStrings.Get(key);
        return internalName;
    }

    internal sealed class FreighterTypeItem
    {
        public string InternalName { get; }
        public string DisplayName { get; }
        public FreighterTypeItem(string internalName, string displayName) { InternalName = internalName; DisplayName = displayName; }
        public override string ToString() => DisplayName;
    }

    internal static FreighterTypeItem[] GetFreighterTypeItems()
    {
        return FreighterTypes.Keys.Select(k => new FreighterTypeItem(k, GetLocalisedFreighterTypeName(k))).ToArray();
    }

    /// <summary>
    /// Reads a stat bonus value from a freighter inventory.
    /// </summary>
    /// <param name="inventory">The inventory JSON object to read from.</param>
    /// <param name="statId">The stat identifier (e.g. "^FREI_HYPERDRIVE").</param>
    /// <returns>The numeric stat value.</returns>
    internal static double ReadStatBonus(JsonObject? inventory, string statId) =>
        StatHelper.ReadBaseStatValue(inventory, statId);

    /// <summary>
    /// Reads the text representation of a stat bonus value from a freighter inventory.
    /// </summary>
    internal static string ReadStatBonusText(JsonObject? inventory, string statId) =>
        StatHelper.ReadBaseStatText(inventory, statId);

    /// <summary>
    /// Writes a stat bonus value to a freighter inventory.
    /// </summary>
    /// <param name="inventory">The inventory JSON object to write to.</param>
    /// <param name="statId">The stat identifier (e.g. "^FREI_HYPERDRIVE").</param>
    /// <param name="value">The value to write.</param>
    /// <param name="displayText">Optional display text to preserve as RawDouble.</param>
    internal static void WriteStatBonus(JsonObject? inventory, string statId, double value, string? displayText = null) =>
        StatHelper.WriteBaseStatValue(inventory, statId, value, displayText);

    /// <summary>
    /// Finds the freighter base object in the player's persistent bases (version 3+).
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <returns>The freighter base JSON object, or <c>null</c> if not found.</returns>
    internal static JsonObject? FindFreighterBase(JsonObject playerState)
    {
        try
        {
            var bases = playerState.GetArray("PersistentPlayerBases");
            if (bases == null) return null;
            for (int i = 0; i < bases.Length; i++)
            {
                var b = bases.GetObject(i);
                try
                {
                    var baseType = b.GetObject("BaseType");
                    if (baseType != null
                        && "FreighterBase" == baseType.GetString("PersistentBaseTypes")
                        && b.GetInt("BaseVersion") >= 3)
                        return b;
                }
                catch { }
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Sets the inventory class on a freighter inventory object.
    /// </summary>
    /// <param name="inventory">The inventory JSON object.</param>
    /// <param name="cls">The class grade string (e.g. "S").</param>
    internal static void SetInventoryClass(JsonObject? inventory, string cls)
    {
        if (inventory == null) return;
        try
        {
            var classObj = inventory.GetObject("Class");
            classObj?.Set("InventoryClass", cls);
        }
        catch { }
    }



    /// <summary>
    /// Loads freighter data from the player state JSON for display and editing.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <returns>A populated <see cref="FreighterData"/> instance.</returns>
    internal static FreighterData LoadFreighterData(JsonObject playerState)
    {
        var data = new FreighterData();

        data.Name = playerState.GetString("PlayerFreighterName") ?? "";

        try
        {
            var currentFreighter = playerState.GetObject("CurrentFreighter");
            string filename = currentFreighter?.GetString("Filename") ?? "";
            data.TypeDisplayName = FreighterTypes.FirstOrDefault(x => x.Value.Equals(filename, StringComparison.OrdinalIgnoreCase)).Key;
        }
        catch { }

        try
        {
            var freighterInv = playerState.GetObject("FreighterInventory");
            var classObj = freighterInv?.GetObject("Class");
            string cls = classObj?.GetString("InventoryClass") ?? "";
            data.ClassIndex = Array.IndexOf(FreighterClasses, cls);
        }
        catch { }

        try
        {
            var homeSeedArr = playerState.GetArray("CurrentFreighterHomeSystemSeed");
            if (homeSeedArr != null && homeSeedArr.Length > 1)
                data.HomeSeed = homeSeedArr.Get(1)?.ToString() ?? "";
        }
        catch { }

        try
        {
            var currentFreighter = playerState.GetObject("CurrentFreighter");
            var seedArr = currentFreighter?.GetArray("Seed");
            if (seedArr != null && seedArr.Length > 1)
                data.ModelSeed = seedArr.Get(1)?.ToString() ?? "";
        }
        catch { }

        data.Hyperdrive = ReadStatBonus(playerState.GetObject("FreighterInventory"), "^FREI_HYPERDRIVE");
        data.FleetCoordination = ReadStatBonus(playerState.GetObject("FreighterInventory"), "^FREI_FLEET");
        data.HyperdriveText = ReadStatBonusText(playerState.GetObject("FreighterInventory"), "^FREI_HYPERDRIVE");
        data.FleetCoordinationText = ReadStatBonusText(playerState.GetObject("FreighterInventory"), "^FREI_FLEET");

        data.FreighterBase = FindFreighterBase(playerState);
        if (data.FreighterBase != null)
        {
            try
            {
                var objects = data.FreighterBase.GetArray("Objects");
                data.BaseItemCount = objects?.Length ?? 0;
            }
            catch { data.BaseItemCount = 0; }
        }

        data.CargoInventory = playerState.GetObject("FreighterInventory");
        data.TechInventory = playerState.GetObject("FreighterInventory_TechOnly");

        return data;
    }

    /// <summary>
    /// Saves freighter data back to the player state JSON object.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="values">The values to write.</param>
    internal static void SaveFreighterData(JsonObject playerState, FreighterSaveValues values)
    {
        // Always write name (allow empty string to clear name)
        playerState.Set("PlayerFreighterName", values.Name ?? "");

        // Sync freighter name to TeleportEndpoints (type 9 = FreighterTeleport)
        // Keeps the teleporter list name in sync
        try
        {
            var endpoints = playerState.GetArray("TeleportEndpoints");
            if (endpoints != null)
            {
                for (int i = 0; i < endpoints.Length; i++)
                {
                    var ep = endpoints.GetObject(i);
                    if (ep != null)
                    {
                        var teleType = ep.GetString("TeleporterType");
                        if (teleType == "9" || teleType == "FreighterTeleport")
                        {
                            ep.Set("Name", values.Name ?? "");
                        }
                    }
                }
            }
        }
        catch { }

        if (!string.IsNullOrEmpty(values.SelectedTypeName) && FreighterTypes.TryGetValue(values.SelectedTypeName, out var filename))
        {
            var currentFreighter = playerState.GetObject("CurrentFreighter");
            if (currentFreighter != null)
                currentFreighter.Set("Filename", filename);
        }

        if (values.ClassIndex >= 0 && values.ClassIndex != values.OriginalClassIndex)
        {
            string cls = FreighterClasses[values.ClassIndex];
            SetInventoryClass(playerState.GetObject("FreighterInventory"), cls);
            SetInventoryClass(playerState.GetObject("FreighterInventory_TechOnly"), cls);
            // Some save versions also have a Cargo inventory
            SetInventoryClass(playerState.GetObject("FreighterInventory_Cargo"), cls);
        }

        try
        {
            var homeSeedArr = playerState.GetArray("CurrentFreighterHomeSystemSeed");
            var normalizedHome = SeedHelper.NormalizeSeed(values.HomeSeed);
            if (homeSeedArr != null && homeSeedArr.Length > 1 && normalizedHome != null)
                homeSeedArr.Set(1, normalizedHome);
        }
        catch { }

        try
        {
            var currentFreighter = playerState.GetObject("CurrentFreighter");
            var seedArr = currentFreighter?.GetArray("Seed");
            var normalizedModel = SeedHelper.NormalizeSeed(values.ModelSeed);
            if (seedArr != null && seedArr.Length > 1 && normalizedModel != null)
                seedArr.Set(1, normalizedModel);
        }
        catch { }

        // Write base stats to FreighterInventory and FreighterInventory_TechOnly.
        // For freighters on v400+ Waypoint the game uses these two inventories;
        // FreighterInventory_Cargo is NOT included for modern saves.
        foreach (string invKey in new[] { "FreighterInventory", "FreighterInventory_TechOnly" })
        {
            var inv = playerState.GetObject(invKey);
            if (inv == null) continue;
            double writeHyperdrive = Data.BaseStatLimits.ConditionalClampStatValue("Normal", "^FREI_HYPERDRIVE", values.Hyperdrive, Data.StatCategory.Freighter, values.RawStatValues);
            double writeFleet = Data.BaseStatLimits.ConditionalClampStatValue("Normal", "^FREI_FLEET", values.FleetCoordination, Data.StatCategory.Freighter, values.RawStatValues);
            string? hyperdriveText = values.Hyperdrive == writeHyperdrive ? values.HyperdriveText : null;
            string? fleetText = values.FleetCoordination == writeFleet ? values.FleetCoordinationText : null;
            WriteStatBonus(inv, "^FREI_HYPERDRIVE", writeHyperdrive, hyperdriveText);
            WriteStatBonus(inv, "^FREI_FLEET", writeFleet, fleetText);
        }
    }

    /// <summary>
    /// Builds a sanitized export filename from the freighter's name, type, and class.
    /// </summary>
    /// <param name="name">The freighter name.</param>
    /// <param name="typeName">The freighter type display name.</param>
    /// <param name="classIndex">Index into <see cref="FreighterClasses"/>.</param>
    /// <returns>A filename-safe string combining name, type, and class.</returns>
    internal static string BuildExportFileName(string name, string? typeName, int classIndex)
    {
        string safeName = StringHelper.SanitizeFileName(name);
        string safeType = StringHelper.SanitizeFileName(typeName ?? "unknown");
        string safeClass = classIndex >= 0 ? FreighterClasses[classIndex] : "C";
        return $"{safeName}_{safeType}_{safeClass}";
    }

    /// <summary>
    /// Known freighter room ObjectIDs mapped to their display names.
    /// </summary>
    internal static readonly Dictionary<string, string> KnownRooms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "^FRE_ROOM_SCAN",   "Scanner Room" },
        { "^FRE_ROOM_VEHICL", "Orbital Exocraft Materialiser" },
        { "^FRE_ROOM_PLANT0", "Double Cultivation Chamber" },
        { "^FRE_ROOM_PLANT1", "Cultivation Chamber" },
        { "^FRE_ROOM_COOK",   "Nutrition Room" },
        { "^FRE_ROOM_REFINE", "Refiner Room" },
        { "^FRE_ROOM_FLEET",  "Fleet Command Room" },
        { "^FRE_ROOM_SHOP",   "Galactic Trade Room" },
        { "^FRE_ROOM_DRESS",  "Appearance Modifier Room" },
        { "^FRE_ROOM_TELEPO", "Teleport Chamber" },
        { "^FRE_ROOM_TECH",   "Technology Room" },
        { "^FRE_ROOM_BIO",    "Biological Room" },
        { "^FRE_ROOM_IND",    "Industrial Room" },
        { "^FRE_ROOM_EXTR",   "Stellar Extractor Room" },
        { "^FRE_ROOM_ROCLOC", "Storage Shuttle Room" },
        { "^FRE_ROOM_NPCSCI", "Science Specialist's Room" },
        { "^FRE_ROOM_NPCBUI", "Construction Specialist's Room" },
        { "^FRE_ROOM_NPCFAR", "Agricultural Specialist's Room" },
        { "^FRE_ROOM_NPCWEA", "Weapons Specialist Room" },
        { "^FRE_ROOM_NPCVEH", "Exocraft Specialist's Room" },
        { "^FRE_ROOM_STORE0", "Storage Room 0" },
        { "^FRE_ROOM_STORE1", "Storage Room 1" },
        { "^FRE_ROOM_STORE2", "Storage Room 2" },
        { "^FRE_ROOM_STORE3", "Storage Room 3" },
        { "^FRE_ROOM_STORE4", "Storage Room 4" },
        { "^FRE_ROOM_STORE5", "Storage Room 5" },
        { "^FRE_ROOM_STORE6", "Storage Room 6" },
        { "^FRE_ROOM_STORE7", "Storage Room 7" },
        { "^FRE_ROOM_STORE8", "Storage Room 8" },
        { "^FRE_ROOM_STORE9", "Storage Room 9" },
    };

    private static readonly Dictionary<string, string> RoomLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        { "^FRE_ROOM_SCAN",   "freighter.room_scanner" },
        { "^FRE_ROOM_VEHICL", "freighter.room_orbital_exocraft" },
        { "^FRE_ROOM_PLANT0", "freighter.room_double_cultivation" },
        { "^FRE_ROOM_PLANT1", "freighter.room_cultivation" },
        { "^FRE_ROOM_COOK",   "freighter.room_nutrition" },
        { "^FRE_ROOM_REFINE", "freighter.room_refiner" },
        { "^FRE_ROOM_FLEET",  "freighter.room_fleet_command" },
        { "^FRE_ROOM_SHOP",   "freighter.room_galactic_trade" },
        { "^FRE_ROOM_DRESS",  "freighter.room_appearance" },
        { "^FRE_ROOM_TELEPO", "freighter.room_teleport" },
        { "^FRE_ROOM_TECH",   "freighter.room_technology" },
        { "^FRE_ROOM_BIO",    "freighter.room_biological" },
        { "^FRE_ROOM_IND",    "freighter.room_industrial" },
        { "^FRE_ROOM_EXTR",   "freighter.room_stellar_extractor" },
        { "^FRE_ROOM_ROCLOC", "freighter.room_storage_shuttle" },
        { "^FRE_ROOM_NPCSCI", "freighter.room_science_specialist" },
        { "^FRE_ROOM_NPCBUI", "freighter.room_construction_specialist" },
        { "^FRE_ROOM_NPCFAR", "freighter.room_agricultural_specialist" },
        { "^FRE_ROOM_NPCWEA", "freighter.room_weapons_specialist" },
        { "^FRE_ROOM_NPCVEH", "freighter.room_exocraft_specialist" },
        { "^FRE_ROOM_STORE0", "freighter.room_storage_0" },
        { "^FRE_ROOM_STORE1", "freighter.room_storage_1" },
        { "^FRE_ROOM_STORE2", "freighter.room_storage_2" },
        { "^FRE_ROOM_STORE3", "freighter.room_storage_3" },
        { "^FRE_ROOM_STORE4", "freighter.room_storage_4" },
        { "^FRE_ROOM_STORE5", "freighter.room_storage_5" },
        { "^FRE_ROOM_STORE6", "freighter.room_storage_6" },
        { "^FRE_ROOM_STORE7", "freighter.room_storage_7" },
        { "^FRE_ROOM_STORE8", "freighter.room_storage_8" },
        { "^FRE_ROOM_STORE9", "freighter.room_storage_9" },
    };

    /// <summary>
    /// Returns the localised display name for a freighter room ObjectID.
    /// Falls back to the English name from <see cref="KnownRooms"/>, then to the raw objectId.
    /// </summary>
    /// <param name="objectId">The room ObjectID (e.g. "^FRE_ROOM_SCAN").</param>
    /// <returns>The localised room name, English fallback, or the raw objectId if unknown.</returns>
    internal static string GetLocalisedRoomName(string objectId)
    {
        if (RoomLocKeys.TryGetValue(objectId, out var key))
        {
            var loc = UiStrings.Get(key);
            if (!string.IsNullOrEmpty(loc) && loc != key)
                return loc;
        }
        return KnownRooms.TryGetValue(objectId, out var english) ? english : objectId;
    }

    /// <summary>
    /// Detects which known freighter rooms are installed by scanning the freighter base's Objects array.
    /// </summary>
    /// <param name="freighterBase">The freighter base JSON object (from <see cref="FindFreighterBase"/>).</param>
    /// <returns>A list of display strings with check/cross marks indicating installed status.</returns>
    internal static List<string> DetectFreighterRooms(JsonObject? freighterBase)
    {
        var rooms = new List<string>();
        if (freighterBase == null)
            return rooms;

        var foundIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var objects = freighterBase.GetArray("Objects");
            if (objects != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    try
                    {
                        var obj = objects.GetObject(i);
                        string? objId = obj.GetString("ObjectID");
                        if (objId != null)
                            foundIds.Add(objId);
                    }
                    catch { }
                }
            }
        }
        catch { }

        foreach (var kvp in KnownRooms)
        {
            string displayName = GetLocalisedRoomName(kvp.Key);
            rooms.Add(foundIds.Contains(kvp.Key)
                ? $"\u2705 {displayName}"
                : $"\u274C {displayName}");
        }

        return rooms;
    }

    /// <summary>
    /// Reads the Width and Height of a freighter inventory.
    /// </summary>
    /// <param name="inventory">The inventory JSON object.</param>
    /// <returns>A tuple of (width, height). Returns (0, 0) if not found.</returns>
    internal static (int Width, int Height) ReadInventorySize(JsonObject? inventory)
    {
        if (inventory == null) return (0, 0);
        int w = 0, h = 0;
        try { w = inventory.GetInt("Width"); } catch { }
        try { h = inventory.GetInt("Height"); } catch { }
        return (w, h);
    }

    /// <summary>
    /// Resizes all three freighter inventory objects to the specified dimensions.
    /// </summary>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="width">The new width (1–15).</param>
    /// <param name="height">The new height (1–13).</param>
    internal static void ResizeFreighterInventories(JsonObject playerState, int width, int height)
    {
        foreach (string invKey in new[] { "FreighterInventory", "FreighterInventory_TechOnly", "FreighterInventory_Cargo" })
        {
            var inv = playerState.GetObject(invKey);
            if (inv != null)
            {
                inv.Set("Width", width);
                inv.Set("Height", height);
            }
        }
    }

    /// <summary>
    /// Holds loaded freighter data for display and editing in the UI.
    /// </summary>
    internal sealed class FreighterData
    {
        /// <summary>The player-assigned freighter name.</summary>
        public string Name { get; set; } = "";
        /// <summary>The freighter type display name (e.g. "Capital", "Small").</summary>
        public string? TypeDisplayName { get; set; }
        /// <summary>Index into <see cref="FreighterClasses"/> for the class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>The freighter's home system seed as a hex string.</summary>
        public string HomeSeed { get; set; } = "";
        /// <summary>The freighter's model seed as a hex string.</summary>
        public string ModelSeed { get; set; } = "";
        /// <summary>The freighter's base hyperdrive stat.</summary>
        public double Hyperdrive { get; set; }
        /// <summary>The freighter's fleet coordination stat.</summary>
        public double FleetCoordination { get; set; }
        /// <summary>The original text representation of the hyperdrive stat from the save file.</summary>
        public string HyperdriveText { get; set; } = "0";
        /// <summary>The original text representation of the fleet coordination stat from the save file.</summary>
        public string FleetCoordinationText { get; set; } = "0";
        /// <summary>The freighter base JSON object, if found.</summary>
        public JsonObject? FreighterBase { get; set; }
        /// <summary>Number of items placed in the freighter base, or -1 if unknown.</summary>
        public int BaseItemCount { get; set; } = -1;
        /// <summary>The freighter's cargo inventory JSON object.</summary>
        public JsonObject? CargoInventory { get; set; }
        /// <summary>The freighter's tech inventory JSON object.</summary>
        public JsonObject? TechInventory { get; set; }
    }

    /// <summary>
    /// Holds values to be saved back to the freighter's JSON data.
    /// </summary>
    internal sealed class FreighterSaveValues
    {
        /// <summary>The freighter name to set.</summary>
        public string Name { get; set; } = "";
        /// <summary>The selected freighter type display name, or <c>null</c> to leave unchanged.</summary>
        public string? SelectedTypeName { get; set; }
        /// <summary>Index into <see cref="FreighterClasses"/> for the desired class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>Original class index loaded from save; used to skip class writes when unchanged.</summary>
        public int OriginalClassIndex { get; set; } = -1;
        /// <summary>The home system seed hex string to set.</summary>
        public string HomeSeed { get; set; } = "";
        /// <summary>The model seed hex string to set.</summary>
        public string ModelSeed { get; set; } = "";
        /// <summary>The hyperdrive stat value to write.</summary>
        public double Hyperdrive { get; set; }
        /// <summary>The fleet coordination stat value to write.</summary>
        public double FleetCoordination { get; set; }
        /// <summary>Display text for the hyperdrive stat, used to create a RawDouble on save.</summary>
        public string? HyperdriveText { get; set; }
        /// <summary>Display text for the fleet coordination stat, used to create a RawDouble on save.</summary>
        public string? FleetCoordinationText { get; set; }

        /// <summary>Raw (unclamped) stat values read from JSON at load time.
        /// When set, each stat is only written if the UI value differs from
        /// the clamped raw value - preserving externally-edited values.</summary>
        public Dictionary<string, double>? RawStatValues { get; set; }
    }
}
