using System.IO.Compression;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles starship data operations including loading, saving, type lookups, and inventory management.
/// </summary>
internal static class StarshipLogic
{
    /// <summary>
    /// Available ship class grades, ordered from lowest to highest.
    /// </summary>
    internal static readonly string[] ShipClasses = { "C", "B", "A", "S" };

    /// <summary>
    /// Maps English ship type display names to their UI localisation keys.
    /// </summary>
    private static readonly Dictionary<string, string> ShipTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hauler"] = "starship.type_hauler",
        ["Explorer"] = "starship.type_explorer",
        ["Shuttle"] = "starship.type_shuttle",
        ["Fighter"] = "starship.type_fighter",
        ["Exotic"] = "starship.type_exotic",
        ["Living Ship"] = "starship.type_living_ship",
        ["Solar"] = "starship.type_solar",
        ["Utopia Speeder"] = "starship.type_utopia_speeder",
        ["Golden Vector"] = "starship.type_golden_vector",
        ["Horizon Vector NX (Switch)"] = "starship.type_horizon_vector",
        ["Sentinel"] = "starship.type_sentinel",
        ["Starborn Runner"] = "starship.type_starborn_runner",
        ["Starborn Phoenix"] = "starship.type_starborn_phoenix",
        ["Corvette"] = "starship.type_corvette",
        ["Boundary Herald"] = "starship.type_boundary_herald",
        ["The Wraith"] = "starship.type_the_wraith",
        ["Interceptor"] = "starship.type_interceptor",
    };

    /// <summary>
    /// Gets the localised display name for a ship type given its English display name.
    /// Falls back to the English name if no localisation key is found.
    /// </summary>
    internal static string GetLocalisedShipTypeName(string englishName)
    {
        return ShipTypeLocKeys.TryGetValue(englishName, out var key) ? UiStrings.Get(key) : englishName;
    }

    /// <summary>
    /// Gets an array of ShipTypeItem wrappers for populating combo boxes.
    /// Each item displays a localised name but carries the English name for data lookups.
    /// </summary>
    internal static ShipTypeItem[] GetShipTypeItems()
    {
        return ShipInfo.Values
            .Select(info => info.DisplayName)
            .Distinct()
            .OrderBy(n => GetLocalisedShipTypeName(n))
            .Select(n => new ShipTypeItem(n, GetLocalisedShipTypeName(n)))
            .ToArray();
    }

    /// <summary>
    /// Maps ship model resource filenames to their display info (name, keywords, cargo dimensions, tech dimensions).
    /// </summary>
    internal static readonly Dictionary<string, (string DisplayName, string[] Keywords, string CargoDimensions, string TechDimensions)> ShipInfo =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["MODELS/COMMON/SPACECRAFT/DROPSHIPS/DROPSHIP_PROC.SCENE.MBIN"] = ("Hauler", new[] { "DROPSHIP" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SCIENTIFIC/SCIENTIFIC_PROC.SCENE.MBIN"] = ("Explorer", new[] { "SCIENTIFIC" }, "10x11", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SHUTTLE/SHUTTLE_PROC.SCENE.MBIN"] = ("Shuttle", new[] { "SHUTTLE" }, "10x11", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTER_PROC.SCENE.MBIN"] = ("Fighter", new[] { "FIGHTER" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/S-CLASS/S-CLASS_PROC.SCENE.MBIN"] = ("Exotic", new[] { "EXOTIC" }, "10x10 + 5", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOSHIP_PROC.SCENE.MBIN"] = ("Living Ship", new[] { "BIOSHIP" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SAILSHIP/SAILSHIP_PROC.SCENE.MBIN"] = ("Solar", new[] { "SAILSHIP" }, "10x11", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/VRSPEEDER.SCENE.MBIN"] = ("Utopia Speeder", new[] { "VRSPEEDER" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERCLASSICGOLD.SCENE.MBIN"] = ("Golden Vector", new[] { "FIGHTERCLASSICGOLD" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/FIGHTERSPECIALSWITCH.SCENE.MBIN"] = ("Horizon Vector NX (Switch)", new[] { "FIGHTERSPECIALSWITCH" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/SENTINELSHIP/SENTINELSHIP_PROC.SCENE.MBIN"] = ("Sentinel", new[] { "SENTINEL" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACER.SCENE.MBIN"] = ("Starborn Runner", new[] { "WRACER.SCENE" }, "10x10 + 5", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/WRACERSE.SCENE.MBIN"] = ("Starborn Phoenix", new[] { "WRACERSE" }, "10x10 + 5", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/BIGGS/BIGGS.SCENE.MBIN"] = ("Corvette", new[] { "BIGGS" }, "10x12", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/FIGHTERS/SPOOKSHIP.SCENE.MBIN"] = ("Boundary Herald", new[] { "SPOOKSHIP" }, "10x10", "10x6"),
            ["MODELS/COMMON/SPACECRAFT/S-CLASS/BIOPARTS/BIOFIGHTER.SCENE.MBIN"] = ("The Wraith", new[] { "BIOFIGHTER" }, "10x12", "10x6"),
        };

    /// <summary>
    /// Retrieves the display name, cargo label, and tech label for a ship given its resource filename.
    /// Falls back to keyword matching if an exact filename match is not found.
    /// </summary>
    /// <param name="filename">The ship model resource filename.</param>
    /// <returns>A tuple containing the display name, cargo max label, and tech max label.</returns>
    internal static (string DisplayName, string CargoLabel, string TechLabel) GetShipInfo(string filename)
    {
        if (!string.IsNullOrEmpty(filename) && ShipInfo.TryGetValue(filename, out var info))
            return (info.DisplayName, UiStrings.Format("common.max_supported", info.CargoDimensions), UiStrings.Format("common.max_supported", info.TechDimensions));

        if (!string.IsNullOrEmpty(filename))
        {
            foreach (var entry in ShipInfo.Values)
            {
                if (entry.Keywords.Any(k => filename.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    return (entry.DisplayName, UiStrings.Format("common.max_supported", entry.CargoDimensions), UiStrings.Format("common.max_supported", entry.TechDimensions));
            }
        }
        return (UiStrings.Get("common.unknown"), UiStrings.Format("common.max_supported", "?"), UiStrings.Format("common.max_supported", "10x6"));
    }

    /// <summary>
    /// Gets the display name for a ship type given its resource filename.
    /// </summary>
    /// <param name="filename">The ship model resource filename.</param>
    /// <returns>The ship type display name, or "Unknown" if not found.</returns>
    internal static string LookupShipTypeName(string filename)
    {
        var (displayName, _, _) = GetShipInfo(filename);
        return displayName;
    }

    /// <summary>
    /// Gets a sorted, distinct list of all known ship type display names.
    /// </summary>
    /// <returns>An array of unique ship type names in alphabetical order.</returns>
    internal static string[] GetShipTypeNames()
    {
        return ShipInfo.Values.Select(info => info.DisplayName).Distinct().OrderBy(n => n).ToArray();
    }

    /// <summary>
    /// Gets the resource filename for a given ship type display name.
    /// </summary>
    /// <param name="displayName">The ship type display name to look up.</param>
    /// <returns>The corresponding resource filename, or an empty string if not found.</returns>
    internal static string LookupFilenameForType(string displayName)
    {
        return ShipInfo.FirstOrDefault(
            kvp => kvp.Value.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase)
        ).Key ?? "";
    }



    /// <summary>
    /// Builds a list of owned ships from the ship ownership JSON array, skipping empty slots.
    /// </summary>
    /// <param name="shipOwnership">The JSON array of ship ownership entries.</param>
    /// <returns>A list of ship items with display names and data indices.</returns>
    internal static List<ShipListItem> BuildShipList(JsonArray shipOwnership)
    {
        var list = new List<ShipListItem>();
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                bool hasSeed = false;
                try
                {
                    var seedArr = resource?.GetArray("Seed");
                    if (seedArr != null && seedArr.Length > 0)
                        hasSeed = seedArr.GetBool(0);
                }
                catch { }

                if (!hasSeed) continue;

                string name = ship.GetString("Name") ?? "";

                // Resolve ship type from resource filename
                string filename = "";
                try { filename = resource?.GetString("Filename") ?? ""; } catch { }
                string shipType = LookupShipTypeName(filename);

                // Resolve class from inventory
                string cls = "";
                try
                {
                    var inv = ship.GetObject("Inventory");
                    var classObj = inv?.GetObject("Class");
                    cls = classObj?.GetString("InventoryClass") ?? "";
                }
                catch { }

                string displayName;
                if (string.IsNullOrEmpty(name))
                {
                    // No custom name — show slot, type, class: "[1] Hauler - C"
                    string typeLabel = string.IsNullOrEmpty(shipType) ? "Ship" : shipType;
                    string clsLabel = string.IsNullOrEmpty(cls) ? "?" : cls;
                    displayName = $"[{i + 1}] {typeLabel} - {clsLabel}";
                }
                else
                {
                    // Named ship — show slot, name, class: "[5] VCF Blackbird - S"
                    string clsLabel = string.IsNullOrEmpty(cls) ? "?" : cls;
                    displayName = $"[{i + 1}] {name} - {clsLabel}";
                }
                list.Add(new ShipListItem(displayName, i));
            }
            catch
            {
                list.Add(new ShipListItem($"[{i + 1}] Ship - ?", i));
            }
        }
        return list;
    }

    /// <summary>
    /// Loads ship data from a JSON ship object and optional player state for display and editing.
    /// </summary>
    /// <param name="ship">The JSON object representing the ship.</param>
    /// <param name="playerState">The player state JSON object, used for legacy colour settings.</param>
    /// <returns>A populated <see cref="ShipData"/> instance.</returns>
    internal static ShipData LoadShipData(JsonObject ship, JsonObject? playerState, int shipIndex = -1)
    {
        string name = ship.GetString("Name") ?? "";

        string filename = "";
        string seed = "";
        try
        {
            var resource = ship.GetObject("Resource");
            filename = resource?.GetString("Filename") ?? "";
            seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
        }
        catch { }

        string shipTypeName = LookupShipTypeName(filename);
        var (_, cargoLabel, techLabel) = GetShipInfo(filename);

        string cls = "";
        try
        {
            var inv = ship.GetObject("Inventory");
            var classObj = inv?.GetObject("Class");
            cls = classObj?.GetString("InventoryClass") ?? "";
        }
        catch { }
        int classIndex = Array.IndexOf(ShipClasses, cls);

        bool useOldColours = false;
        try
        {
            if (playerState != null)
            {
                // ShipUsesLegacyColours is an array indexed per-ship
                var legacyArr = playerState.GetArray("ShipUsesLegacyColours");
                if (legacyArr != null && shipIndex >= 0 && shipIndex < legacyArr.Length)
                {
                    var val = legacyArr.Get(shipIndex);
                    if (val is bool b) useOldColours = b;
                }
            }
        }
        catch { }

        var shipInv = ship.GetObject("Inventory");
        double damage = 0, shield = 0, hyperdrive = 0, maneuver = 0;
        try { damage = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_DAMAGE"); } catch { }
        try { shield = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_SHIELD"); } catch { }
        try { hyperdrive = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_HYPERDRIVE"); } catch { }
        try { maneuver = StatHelper.ReadBaseStatValue(shipInv, "^SHIP_AGILE"); } catch { }

        string safeName = FileNameHelper.SanitizeFileName(name);
        string safeTypeName = FileNameHelper.SanitizeFileName(shipTypeName);
        string cls2 = classIndex >= 0 ? ShipClasses[classIndex] : "C";

        var cfg = ExportConfig.Instance;
        var invVars = new Dictionary<string, string> { ["ship_name"] = safeName, ["type"] = safeTypeName, ["class"] = cls2 };

        return new ShipData
        {
            Name = name,
            Filename = filename,
            ShipTypeName = shipTypeName,
            Seed = seed,
            ClassIndex = classIndex,
            UseOldColours = useOldColours,
            Damage = damage,
            Shield = shield,
            Hyperdrive = hyperdrive,
            Maneuver = maneuver,
            Inventory = shipInv,
            TechInventory = ship.GetObject("Inventory_TechOnly"),
            CargoMaxLabel = cargoLabel,
            TechMaxLabel = techLabel,
            InvExportFileName = ExportConfig.BuildFileName(cfg.StarshipCargoTemplate, cfg.StarshipCargoExt, invVars),
            TechExportFileName = ExportConfig.BuildFileName(cfg.StarshipTechTemplate, cfg.StarshipTechExt, invVars)
        };
    }

    /// <summary>
    /// Saves ship data back to the JSON ship and player state objects.
    /// </summary>
    /// <param name="ship">The JSON object representing the ship.</param>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="values">The values to write.</param>
    internal static void SaveShipData(JsonObject ship, JsonObject playerState, ShipSaveValues values)
    {
        // Always write name (allow empty string to clear a ship name)
        ship.Set("Name", values.Name ?? "");

        if (!string.IsNullOrEmpty(values.SelectedTypeName))
        {
            string filename = LookupFilenameForType(values.SelectedTypeName);
            var resource = ship.GetObject("Resource");
            if (resource != null && !string.IsNullOrEmpty(filename))
                resource.Set("Filename", filename);
        }

        if (values.ClassIndex >= 0)
        {
            string cls = ShipClasses[values.ClassIndex];
            // Set class on all ship inventories (Inventory, Inventory_TechOnly, Inventory_Cargo)
            // Sets class on all inventory objects.
            foreach (string invKey in new[] { "Inventory", "Inventory_TechOnly", "Inventory_Cargo" })
            {
                var inventory = ship.GetObject(invKey);
                var classObj = inventory?.GetObject("Class");
                classObj?.Set("InventoryClass", cls);
            }
        }

        try
        {
            var resource = ship.GetObject("Resource");
            var seedArr = resource?.GetArray("Seed");
            var normalizedSeed = SeedHelper.NormalizeSeed(values.Seed);
            if (seedArr != null && seedArr.Length > 1 && normalizedSeed != null)
                seedArr.Set(1, normalizedSeed);
        }
        catch { }

        // Write base stats to ALL inventories to keep them in sync
        // Determine ship category for clamping: "Alien" if the selected type contains it, else "Normal"
        string shipCategory = (values.SelectedTypeName ?? "").Contains("Alien", StringComparison.OrdinalIgnoreCase) ? "Alien" : "Normal";

        double writeDamage = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_DAMAGE", values.Damage, Data.StatCategory.Ship, values.RawStatValues);
        double writeShield = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_SHIELD", values.Shield, Data.StatCategory.Ship, values.RawStatValues);
        double writeHyperdrive = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_HYPERDRIVE", values.Hyperdrive, Data.StatCategory.Ship, values.RawStatValues);
        double writeManeuver = Data.BaseStatLimits.ConditionalClampStatValue(shipCategory, "^SHIP_AGILE", values.Maneuver, Data.StatCategory.Ship, values.RawStatValues);

        foreach (string invKey in new[] { "Inventory", "Inventory_TechOnly", "Inventory_Cargo" })
        {
            var inv = ship.GetObject(invKey);
            if (inv == null) continue;
            StatHelper.WriteBaseStatValue(inv, "^SHIP_DAMAGE", writeDamage);
            StatHelper.WriteBaseStatValue(inv, "^SHIP_SHIELD", writeShield);
            StatHelper.WriteBaseStatValue(inv, "^SHIP_HYPERDRIVE", writeHyperdrive);
            StatHelper.WriteBaseStatValue(inv, "^SHIP_AGILE", writeManeuver);
        }

        // ShipUsesLegacyColours is an array indexed per-ship; update the correct element
        try
        {
            if (values.ShipIndex >= 0)
            {
                var legacyArr = playerState.GetArray("ShipUsesLegacyColours");
                if (legacyArr != null && values.ShipIndex < legacyArr.Length)
                    legacyArr.Set(values.ShipIndex, values.UseOldColours);
            }
        }
        catch { }

        try { playerState.Set("PrimaryShip", values.PrimaryShipIndex); }
        catch { }
    }

    /// <summary>
    /// Fully resets a ship slot by clearing its resource, name, inventories,
    /// and all associated data. The entry remains in the ShipOwnership array to
    /// preserve index alignment with parallel arrays such as ShipUsesLegacyColours.
    /// The slot is filtered out by BuildShipList() because Seed[0] becomes false.
    /// </summary>
    /// <param name="ship">The JSON object representing the ship to delete.</param>
    internal static void DeleteShipData(JsonObject ship)
    {
        // Clear the resource (filename + seed) - this is what marks the slot as empty
        var resource = ship.GetObject("Resource");
        if (resource != null)
        {
            resource.Set("Filename", "");
            var seedArr = resource.GetArray("Seed");
            if (seedArr != null && seedArr.Length > 1)
            {
                seedArr.Set(0, false);
                seedArr.Set(1, "0x0");
            }
        }

        // Clear the ship name
        ship.Set("Name", "");

        // Clear all three inventory types (Slots, BaseStatValues, ValidSlotIndices)
        ResetInventoryObject(ship.GetObject("Inventory"));
        ResetInventoryObject(ship.GetObject("Inventory_TechOnly"));
        ResetInventoryObject(ship.GetObject("Inventory_Cargo"));
    }

    /// <summary>
    /// Resets an inventory JSON object by clearing its Slots, ValidSlotIndices,
    /// BaseStatValues, and SpecialSlots arrays while preserving the object structure.
    /// </summary>
    private static void ResetInventoryObject(JsonObject? inventory)
    {
        if (inventory == null) return;

        ClearJsonArray(inventory.GetArray("Slots"));
        ClearJsonArray(inventory.GetArray("ValidSlotIndices"));
        ClearJsonArray(inventory.GetArray("BaseStatValues"));
        ClearJsonArray(inventory.GetArray("SpecialSlots"));
    }

    // --- Ship Customisation Data (CharacterCustomisationData) --------

    /// <summary>
    /// The CharacterCustomisationData array contains 26 entries. Entries at indices
    /// 3–8 correspond to ship slots 0–5 and entries at indices 17–22 correspond to
    /// ship slots 6–11. This method converts a ShipOwnership index to the matching
    /// CharacterCustomisationData index.
    /// </summary>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    /// <returns>The corresponding CharacterCustomisationData index, or -1 if out of range.</returns>
    internal static int ShipIndexToCcdIndex(int shipIndex)
    {
        if (shipIndex < 0 || shipIndex > 11) return -1;
        return shipIndex < 6 ? shipIndex + 3 : shipIndex - 6 + 17;
    }

    /// <summary>
    /// Resets the CharacterCustomisationData entry for a specific ship slot
    /// by clearing its DescriptorGroups, Colours, TextureOptions, and BoneScales
    /// arrays, resetting PaletteID/FCx to "^" and Scale to 1.0.
    /// </summary>
    /// <param name="ccdArray">The CharacterCustomisationData JSON array (expected 26 entries).</param>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    internal static void ResetShipCustomisation(JsonArray? ccdArray, int shipIndex)
    {
        if (ccdArray == null) return;
        int ccdIdx = ShipIndexToCcdIndex(shipIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return;

        try
        {
            var entry = ccdArray.GetObject(ccdIdx);
            entry.Set("SelectedPreset", "^");
            var cd = entry.GetObject("CustomData");
            if (cd != null)
                ResetCustomDataObject(cd);
        }
        catch { }
    }

    /// <summary>
    /// Resets a CustomData object to its empty/default state.
    /// </summary>
    private static void ResetCustomDataObject(JsonObject cd)
    {
        ClearJsonArray(cd.GetArray("DescriptorGroups"));
        cd.Set("PaletteID", "^");
        ClearJsonArray(cd.GetArray("Colours"));
        ClearJsonArray(cd.GetArray("TextureOptions"));
        ClearJsonArray(cd.GetArray("BoneScales"));
        cd.Set("Scale", 1.0);
    }

    /// <summary>
    /// Removes all elements from a JSON array (if it exists).
    /// </summary>
    private static void ClearJsonArray(JsonArray? arr)
    {
        if (arr == null) return;
        for (int i = arr.Length - 1; i >= 0; i--)
            arr.RemoveAt(i);
    }

    /// <summary>
    /// Retrieves the CharacterCustomisationData entry for a specific ship slot.
    /// Returns <c>null</c> if the array is missing or the index is out of range.
    /// </summary>
    /// <param name="ccdArray">The CharacterCustomisationData JSON array.</param>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    /// <returns>A deep-clone of the CCD entry, or <c>null</c>.</returns>
    internal static JsonObject? GetShipCustomisation(JsonArray? ccdArray, int shipIndex)
    {
        if (ccdArray == null) return null;
        int ccdIdx = ShipIndexToCcdIndex(shipIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return null;
        try
        {
            return ccdArray.GetObject(ccdIdx).DeepClone();
        }
        catch { return null; }
    }

    /// <summary>
    /// Writes a CharacterCustomisationData entry into the CCD array for a specific
    /// ship slot. All properties from <paramref name="ccdEntry"/> are copied into
    /// the target slot. If <paramref name="ccdEntry"/> is <c>null</c>, the slot is
    /// reset to default values instead.
    /// </summary>
    /// <param name="ccdArray">The CharacterCustomisationData JSON array.</param>
    /// <param name="shipIndex">Zero-based index in the ShipOwnership array (0–11).</param>
    /// <param name="ccdEntry">The CCD entry to write, or <c>null</c> to reset.</param>
    internal static void SetShipCustomisation(JsonArray? ccdArray, int shipIndex, JsonObject? ccdEntry)
    {
        if (ccdArray == null) return;
        int ccdIdx = ShipIndexToCcdIndex(shipIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return;

        if (ccdEntry == null)
        {
            ResetShipCustomisation(ccdArray, shipIndex);
            return;
        }

        try
        {
            var target = ccdArray.GetObject(ccdIdx);
            foreach (var name in ccdEntry.Names())
                target.Set(name, ccdEntry.Get(name));
        }
        catch { }
    }

    /// <summary>
    /// Counts the number of valid (non-invalidated) ships in the ownership array.
    /// A ship is valid when its Resource.Seed[0] is true.
    /// </summary>
    internal static int CountValidShips(JsonArray shipOwnership)
    {
        int count = 0;
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                var seedArr = resource?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0))
                    count++;
            }
            catch { }
        }
        return count;
    }

    /// <summary>
    /// Finds the first empty (invalidated) ship slot in the ownership array.
    /// A slot is empty when its Seed[0] is false.
    /// </summary>
    /// <param name="shipOwnership">The JSON array of ship ownership entries.</param>
    /// <returns>The index of the first empty slot, or -1 if all slots are occupied.</returns>
    internal static int FindEmptySlot(JsonArray shipOwnership)
    {
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                var seedArr = resource?.GetArray("Seed");
                bool hasSeed = false;
                try { hasSeed = seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0); }
                catch { }
                if (!hasSeed) return i;
            }
            catch { }
        }
        return -1;
    }

    /// <summary>
    /// Returns the array index of the first valid (non-invalidated) ship, or -1 if none.
    /// </summary>
    internal static int FindFirstValidShipIndex(JsonArray shipOwnership)
    {
        for (int i = 0; i < shipOwnership.Length; i++)
        {
            try
            {
                var ship = shipOwnership.GetObject(i);
                var resource = ship.GetObject("Resource");
                var seedArr = resource?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0))
                    return i;
            }
            catch { }
        }
        return -1;
    }

    /// <summary>
    /// Determines whether a ship filename corresponds to a Corvette type.
    /// </summary>
    /// <param name="filename">The ship model resource filename.</param>
    /// <returns><c>true</c> if the filename indicates a Corvette; otherwise <c>false</c>.</returns>
    internal static bool IsCorvette(string filename)
    {
        return !string.IsNullOrEmpty(filename) &&
               filename.Contains("BIGGS", StringComparison.OrdinalIgnoreCase);
    }

    // Per ship type:
    //   Normal ships   -> [Ship, AllShips, AllShipsExceptAlien]
    //   Living Ship    -> [AlienShip, AllShips]
    //   Robot/Sentinel -> [RobotShip, AllShips, AllShipsExceptAlien]
    //   Corvette       -> [Corvette, AllShips, AllShipsExceptAlien]
    /// <summary>
    /// Maps a ship type display name to the Technology Category owner type
    /// used for inventory tech filtering. This determines which technology items
    /// can be installed in the ship's tech inventory.
    /// </summary>
    /// <param name="shipTypeName">The ship type display name (e.g. "Fighter", "Living Ship", "Corvette").</param>
    /// <returns>The Technology Category owner string for inventory filtering.</returns>
    internal static string GetOwnerTypeForShip(string shipTypeName)
    {
        return shipTypeName switch
        {
            "Living Ship" => "AlienShip",
            "Sentinel" => "RobotShip",
            "Corvette" => "Corvette",
            _ => "Ship" // Fighter, Hauler, Explorer, Shuttle, Exotic, Solar, etc.
        };
    }

    /// <summary>
    /// Converts a hexadecimal seed string (e.g. "0x1A2B..") to its decimal representation.
    /// </summary>
    /// <param name="hexSeed">The hex seed string, optionally prefixed with "0x".</param>
    /// <returns>The decimal value, or 0 if the string is empty or invalid.</returns>
    internal static long SeedToDecimal(string hexSeed)
    {
        if (string.IsNullOrEmpty(hexSeed)) return 0;
        var s = hexSeed.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        if (long.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out long result))
            return result;
        return 0;
    }

    /// <summary>
    /// Finds the index of a corvette's player ship base entry in the PersistentPlayerBases array.
    /// Uses two strategies in order:
    ///   1. UserData matching - the base's UserData field stores the ShipOwnership index directly.
    ///      This is a more reliable method than the previous community TS understanding.
    ///   2. TS/seed matching - falls back to matching the base Owner.TS against the ship seed
    ///      with tolerance tiers (exact, +/-1s, +/-60s, +/-120s) for saves where UserData
    ///      may not align. Effectively obsolete.
    /// Only bases with BaseType "PlayerShipBase" are considered.
    /// </summary>
    /// <param name="bases">The persistent player bases JSON array.</param>
    /// <param name="shipIndex">The ship's index in the ShipOwnership array.</param>
    /// <param name="seedDecimal">The decimal seed value for TS fallback matching.</param>
    /// <returns>The base index, or -1 if not found.</returns>
    internal static int FindCorvetteBaseIndex(JsonArray? bases, int shipIndex, long seedDecimal)
    {
        if (bases == null) return -1;

        // Strategy 1: match by base UserData == shipIndex
        for (int i = 0; i < bases.Length; i++)
        {
            try
            {
                var b = bases.GetObject(i);
                var baseType = b.GetObject("BaseType");
                if (baseType == null) continue;
                string bt = baseType.GetString("PersistentBaseTypes") ?? "";
                if (!bt.Equals("PlayerShipBase", StringComparison.OrdinalIgnoreCase))
                    continue;

                long ud = 0;
                try { ud = (long)b.GetDouble("UserData"); } catch { }
                if (ud == shipIndex)
                    return i;
            }
            catch { }
        }

        // Strategy 2: TS/seed matching with tolerance tiers (obsolete).
        if (seedDecimal == 0) return -1;

        ReadOnlySpan<long> tolerances = [0, 1, 60, 120];

        foreach (long tol in tolerances)
        {
            int bestIndex = -1;
            long bestDelta = long.MaxValue;

            for (int i = 0; i < bases.Length; i++)
            {
                try
                {
                    var b = bases.GetObject(i);
                    var baseType = b.GetObject("BaseType");
                    if (baseType == null) continue;
                    string bt = baseType.GetString("PersistentBaseTypes") ?? "";
                    if (!bt.Equals("PlayerShipBase", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var owner = b.GetObject("Owner");
                    if (owner == null) continue;
                    long ts = 0;
                    try { ts = (long)owner.GetDouble("TS"); } catch { }

                    long delta = Math.Abs(ts - seedDecimal);
                    if (delta > tol) continue;

                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestIndex = i;
                    }
                }
                catch { }
            }

            if (bestIndex >= 0) return bestIndex;
        }

        return -1;
    }

    /// <summary>
    /// Legacy overload that uses seed-only matching.
    /// Retained for backwards compatibility with callers that do not have the ship index.
    /// </summary>
    internal static int FindCorvetteBaseIndex(JsonArray? bases, long seedDecimal)
    {
        return FindCorvetteBaseIndex(bases, -1, seedDecimal);
    }

    /// <summary>
    /// Gets the display name of the primary ship from the ship ownership array.
    /// </summary>
    /// <param name="shipOwnership">The JSON array of ship ownership entries.</param>
    /// <param name="primaryIndex">The index of the primary ship.</param>
    /// <returns>The ship name, or "Unknown" if unavailable.</returns>
    internal static string GetPrimaryShipName(JsonArray? shipOwnership, int primaryIndex)
    {
        if (shipOwnership == null || primaryIndex < 0 || primaryIndex >= shipOwnership.Length)
            return "Unknown";
        try
        {
            var ship = shipOwnership.GetObject(primaryIndex);
            string name = ship.GetString("Name") ?? "";
            return string.IsNullOrEmpty(name) ? $"Ship {primaryIndex + 1}" : name;
        }
        catch { return "Unknown"; }
    }

    /// <summary>
    /// Checks whether the CCD entry represents the default/blank customisation
    /// (all empty collections, Scale 1.0, SelectedPreset "^", PaletteID "^").
    /// </summary>
    internal static bool IsCcdDefault(JsonObject ccd)
    {
        try
        {
            string preset = ccd.GetString("SelectedPreset") ?? "";
            if (preset != "^" && preset != "") return false;

            var custom = ccd.GetObject("CustomData");
            if (custom == null) return true;

            string palette = custom.GetString("PaletteID") ?? "";
            if (palette != "^" && palette != "") return false;

            foreach (var name in new[] { "DescriptorGroups", "Colours", "TextureOptions", "BoneScales" })
            {
                var arr = custom.GetArray(name);
                if (arr != null && arr.Length > 0) return false;
            }

            return true;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Attempts to read a .nmsship file as a ZIP archive containing so.json, ccd.json, and objects.json.
    /// Returns null if the file is not a ZIP or does not contain so.json.
    /// This is to support IO Tool exports which are ZIPs containing the ship data in JSON files within.
    /// </summary>
    internal static (JsonObject ship, JsonObject? ccd, JsonArray? objects)? TryReadNmsshipZip(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] header = new byte[4];
            if (fs.Read(header, 0, 4) < 4) return null;
            // PK header check (ZIP magic: 0x50 0x4B 0x03 0x04)
            // WHO'S TOES ARE THOSE???
            if (header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
                return null;

            fs.Position = 0;
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

            var soEntry = archive.GetEntry("so.json");
            if (soEntry == null) return null;

            JsonObject ship;
            using (var reader = new StreamReader(soEntry.Open()))
            {
                string json = reader.ReadToEnd();
                ship = JsonObject.Parse(json);
            }

            JsonObject? ccd = null;
            var ccdEntry = archive.GetEntry("ccd.json");
            if (ccdEntry != null)
            {
                using var reader = new StreamReader(ccdEntry.Open());
                string json = reader.ReadToEnd();
                ccd = JsonObject.Parse(json);
            }

            JsonArray? objects = null;
            var objectsEntry = archive.GetEntry("objects.json");
            if (objectsEntry != null)
            {
                using var reader = new StreamReader(objectsEntry.Open());
                string json = reader.ReadToEnd();
                objects = JsonArray.Parse(json);
            }

            return (ship, ccd, objects);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Represents an item in the ship selection list.
    /// </summary>
    internal sealed class ShipListItem
    {
        /// <summary>The display name shown for this ship.</summary>
        public string DisplayName { get; set; }
        /// <summary>The index of this ship in the ship ownership array.</summary>
        public int DataIndex { get; }

        /// <summary>
        /// Initializes a new ship list item.
        /// </summary>
        /// <param name="displayName">The display name for the ship.</param>
        /// <param name="dataIndex">The index in the ownership array.</param>
        public ShipListItem(string displayName, int dataIndex)
        {
            DisplayName = displayName;
            DataIndex = dataIndex;
        }

        /// <inheritdoc/>
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Represents a ship type in the type selection combo box.
    /// Carries the English internal name for data lookups while displaying a localised name.
    /// </summary>
    internal sealed class ShipTypeItem
    {
        /// <summary>The English ship type name used for data lookups via LookupFilenameForType.</summary>
        public string InternalName { get; }
        /// <summary>The localised display name shown in the combo box.</summary>
        public string DisplayName { get; }

        public ShipTypeItem(string internalName, string displayName)
        {
            InternalName = internalName;
            DisplayName = displayName;
        }

        /// <inheritdoc/>
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Holds loaded ship data for display and editing in the UI.
    /// </summary>
    internal sealed class ShipData
    {
        /// <summary>The player-assigned ship name.</summary>
        public string Name { get; set; } = "";
        /// <summary>The ship model resource filename.</summary>
        public string Filename { get; set; } = "";
        /// <summary>The resolved ship type display name (e.g. "Fighter", "Hauler").</summary>
        public string ShipTypeName { get; set; } = "";
        /// <summary>The ship's procedural generation seed as a hex string.</summary>
        public string Seed { get; set; } = "";
        /// <summary>Index into <see cref="ShipClasses"/> for the ship's class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>Whether the ship uses legacy colour rendering.</summary>
        public bool UseOldColours { get; set; }
        /// <summary>The ship's base damage stat.</summary>
        public double Damage { get; set; }
        /// <summary>The ship's base shield stat.</summary>
        public double Shield { get; set; }
        /// <summary>The ship's base hyperdrive stat.</summary>
        public double Hyperdrive { get; set; }
        /// <summary>The ship's base maneuverability stat.</summary>
        public double Maneuver { get; set; }
        /// <summary>The ship's cargo inventory JSON object.</summary>
        public JsonObject? Inventory { get; set; }
        /// <summary>The ship's tech-only inventory JSON object.</summary>
        public JsonObject? TechInventory { get; set; }
        /// <summary>Label describing the maximum supported cargo inventory size.</summary>
        public string CargoMaxLabel { get; set; } = "";
        /// <summary>Label describing the maximum supported tech inventory size.</summary>
        public string TechMaxLabel { get; set; } = "";
        /// <summary>Suggested filename for exporting the cargo inventory.</summary>
        public string InvExportFileName { get; set; } = "";
        /// <summary>Suggested filename for exporting the tech inventory.</summary>
        public string TechExportFileName { get; set; } = "";
    }

    /// <summary>
    /// Holds values to be saved back to a ship's JSON data.
    /// </summary>
    internal sealed class ShipSaveValues
    {
        /// <summary>The ship name to set.</summary>
        public string Name { get; set; } = "";
        /// <summary>The selected ship type display name, or <c>null</c> to leave unchanged.</summary>
        public string? SelectedTypeName { get; set; }
        /// <summary>Index into <see cref="ShipClasses"/> for the desired class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>The seed hex string to set.</summary>
        public string Seed { get; set; } = "";
        /// <summary>The damage stat value to write.</summary>
        public double Damage { get; set; }
        /// <summary>The shield stat value to write.</summary>
        public double Shield { get; set; }
        /// <summary>The hyperdrive stat value to write.</summary>
        public double Hyperdrive { get; set; }
        /// <summary>The maneuverability stat value to write.</summary>
        public double Maneuver { get; set; }
        /// <summary>Whether to use legacy colour rendering.</summary>
        public bool UseOldColours { get; set; }
        /// <summary>The zero-based index of this ship in the ShipOwnership array.</summary>
        public int ShipIndex { get; set; } = -1;
        /// <summary>The index of the ship to set as primary.</summary>
        public int PrimaryShipIndex { get; set; }

        /// <summary>Raw (unclamped) stat values read from JSON at load time.
        /// When set, each stat is only written if the UI value differs from
        /// the clamped raw value - preserving externally-edited values.</summary>
        public Dictionary<string, double>? RawStatValues { get; set; }
    }

    // --- Corvette optimisation ---

    /// <summary>Minimum scale for the beam-up landing bay.</summary>
    private const double MinBeamUpScale = 0.058;

    /// <summary>
    /// Returns the optimiser sort priority for a corvette building object.
    ///
    /// Uses the game's own <c>CorvettePartCategory</c> data (loaded from
    /// <c>Corvette.json</c> via <see cref="Data.StarshipDatabase"/>) to
    /// determine the correct category.  Only five functional categories
    /// are sorted; everything else is left unsorted at the end.
    ///
    /// The priority order is:
    /// Reactors -> Engines -> Landing Gears -> Landing Bays -> Cockpit -> Other
    /// </summary>
    internal static int GetPartPriority(string objectId)
        => Data.StarshipDatabase.GetOptimizerPriority(objectId);

    /// <summary>
    /// Optimises a corvette's base building objects by reordering them to improve
    /// the ships stats/handling in game.
    ///
    /// The priority order is determined by the game's own CorvettePartCategory data:
    /// Reactors -> Engines (Thrusters + Wings) -> Landing Gears -> Landing Bays -> Cockpit -> Other
    ///
    /// Only functional parts (Reactor, Engine, Gear, Access, Cockpit) are sorted.
    /// Non-functional parts (Wing, Shield, Hull, Connector, Interior, Decor, Gun,
    /// Hab, etc.) are left unsorted at the end, preserving original save order.
    ///
    /// Within each sorted category, parts are in ordered (descending) by
    /// Y-position (height) i.e. highest parts first.
    ///
    /// The optimisation also enforces these Corvette game rules:
    ///   - The first cockpit becomes the camera cockpit
    ///   - The second cockpit becomes the boarding cockpit
    ///   - The highest landing bay becomes the active beam-up destination
    ///   - The beam-up landing bay must be at least 0.058 in scale
    ///   
    /// Enforcements are derived from community testing and input.
    /// 
    /// </summary>
    /// <param name="bases">The PersistentPlayerBases array from the save.</param>
    /// <param name="shipIndex">The ship's index in the ShipOwnership array.</param>
    /// <param name="seedDecimal">Decimal seed value of the corvette (used for TS fallback).</param>
    /// <returns>The number of objects reordered, or -1 if the base was not found.</returns>
    internal static int OptimiseCorvetteBase(JsonArray? bases, int shipIndex, long seedDecimal)
    {
        if (bases == null) return -1;

        int baseIdx = FindCorvetteBaseIndex(bases, shipIndex, seedDecimal);
        if (baseIdx < 0) return -1;

        var baseObj = bases.GetObject(baseIdx);
        var objectsArr = baseObj.GetArray("Objects");
        if (objectsArr == null || objectsArr.Length <= 1) return 0;

        return ReorderBuildingObjects(objectsArr);
    }

    /// <summary>
    /// Legacy overload for callers that only have the seed (no ship index).
    /// </summary>
    internal static int OptimiseCorvetteBase(JsonArray? bases, long seedDecimal)
    {
        return OptimiseCorvetteBase(bases, -1, seedDecimal);
    }

    /// <summary>
    /// Checks whether a corvette's building objects are already in the order
    /// that <see cref="OptimiseCorvetteBase"/> would produce. This is a
    /// non-destructive read-only check used to display an indicator dot.
    /// </summary>
    /// <param name="bases">The PersistentPlayerBases array.</param>
    /// <param name="shipIndex">The ship's slot index in ShipOwnership.</param>
    /// <param name="seedDecimal">The ship's seed as a decimal long.</param>
    /// <returns>
    /// <c>true</c> if the objects are already in optimised order (or the base
    /// has 0-1 objects); <c>false</c> if they would be reordered; also
    /// <c>true</c> if the base cannot be found (nothing to optimise).
    /// </returns>
    internal static bool IsCorvetteOptimised(JsonArray? bases, int shipIndex, long seedDecimal)
    {
        if (bases == null) return true;

        int baseIdx = FindCorvetteBaseIndex(bases, shipIndex, seedDecimal);
        if (baseIdx < 0) return true;

        var baseObj = bases.GetObject(baseIdx);
        var objectsArr = baseObj.GetArray("Objects");
        if (objectsArr == null || objectsArr.Length <= 1) return true;

        // Build the expected order by extracting priorities and sort keys,
        // then comparing against the current order without mutating anything.
        int count = objectsArr.Length;
        var items = new List<(int origIndex, int priority, string objectId)>(count);
        for (int i = 0; i < count; i++)
        {
            var obj = objectsArr.GetObject(i);
            string objectId = "";
            try { objectId = obj.GetString("ObjectID") ?? ""; } catch { }
            int priority = GetPartPriority(objectId);
            items.Add((i, priority, objectId));
        }

        // Create a sorted copy of indices using the same comparator as ReorderBuildingObjects
        var sortedIndices = new List<int>(count);
        for (int i = 0; i < count; i++) sortedIndices.Add(i);

        sortedIndices.Sort((a, b) =>
        {
            var ia = items[a];
            var ib = items[b];

            int cmp = ia.priority.CompareTo(ib.priority);
            if (cmp != 0) return cmp;

            string keyA = GetSortKey(ia.priority, ia.objectId, ia.origIndex);
            string keyB = GetSortKey(ib.priority, ib.objectId, ib.origIndex);
            cmp = string.Compare(keyA, keyB, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;

            return ia.origIndex.CompareTo(ib.origIndex);
        });

        // If the sorted order matches the original order, the array is already optimised
        for (int i = 0; i < count; i++)
        {
            if (sortedIndices[i] != i) return false;
        }

        return true;
    }

    /// <summary>
    /// Reads the Y-component (height) from an object's Position array.
    /// Returns 0 if Position is missing or malformed.
    /// </summary>
    private static double GetPositionY(JsonObject obj)
    {
        try
        {
            var pos = obj.GetArray("Position");
            if (pos != null && pos.Length >= 2)
                return pos.GetDouble(1); // Y is index 1 in [X, Y, Z]
        }
        catch { }
        return 0.0;
    }

    /// <summary>
    /// Reads the Scale value from an object.
    /// Returns 1.0 if Scale is missing or malformed.
    /// </summary>
    private static double GetScale(JsonObject obj)
    {
        try { return obj.GetDouble("Scale"); }
        catch { return 1.0; }
    }

    /// <summary>
    /// Reorders a building objects array in place by part category priority.
    /// The priority order is driven by the game's own CorvettePartCategory data:
    /// Reactors -> Engines -> Landing Gears -> Landing Bays -> Cockpit -> Other
    ///
    /// Within each sorted category, parts are ordered by Y-position descending
    /// (highest parts first). Unsorted parts preserve their original relative order.
    ///
    /// Corvette rules enforced:
    /// The beam-up landing bay (last in Access group = highest Y) must have
    /// Scale >= 0.058f - if below, it is clamped up.
    /// </summary>
    /// <param name="objects">The Objects array from a PersistentPlayerBase entry.</param>
    /// <returns>The number of objects in the reordered array.</returns>
    internal static int ReorderBuildingObjects(JsonArray objects)
    {
        int count = objects.Length;
        if (count <= 1) return count;

        // Extract all objects with their original indices and priority
        var items = new List<(int origIndex, int priority, string objectId, JsonObject obj)>(count);
        for (int i = 0; i < count; i++)
        {
            var obj = objects.GetObject(i);
            string objectId = "";
            try { objectId = obj.GetString("ObjectID") ?? ""; } catch { }
            int priority = GetPartPriority(objectId);
            items.Add((i, priority, objectId, obj));
        }

        // Sort matching algorithm:
        //   Primary: by category priority (Reactor -> Engine -> Gear -> Access -> Cockpit -> Other)
        //   Secondary (then by with StringComparison.OrdinalIgnoreCase):
        //     - Categories 1->4 (Reactor, Thruster, Wing, Gear) by fixed sub-order from priority map
        //     - Categories 5->6 (Access, Cockpit) preserve original array index
        //     - Other (int.MaxValue): alphabetically by object list display name, fallback to ObjectID
        //   Tertiary: origIndex tiebreaker (in place of LINQ)
        items.Sort((a, b) =>
        {
            int cmp = a.priority.CompareTo(b.priority);
            if (cmp != 0) return cmp;

            string keyA = GetSortKey(a.priority, a.objectId, a.origIndex);
            string keyB = GetSortKey(b.priority, b.objectId, b.origIndex);
            cmp = string.Compare(keyA, keyB, StringComparison.OrdinalIgnoreCase);
            if (cmp != 0) return cmp;

            // Stable tiebreaker: preserve original array order for equal keys
            return a.origIndex.CompareTo(b.origIndex);
        });

        // Enforce the beam-up landing bay scale >= 0.058f.
        // The last Access category object is the beam-up destination
        int lastAccessIdx = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].priority == Data.StarshipDatabase.AccessPriority)
                lastAccessIdx = i;
        }
        if (lastAccessIdx >= 0)
        {
            var bayObj = items[lastAccessIdx].obj;
            double scale = GetScale(bayObj);
            if (scale < MinBeamUpScale)
            {
                bayObj.Set("Scale", MinBeamUpScale);
            }
        }

        // Rebuild the array in sorted order
        objects.Clear();
        foreach (var (_, _, _, obj) in items)
            objects.Add(obj);

        return count;
    }

    /// <summary>
    /// Computes the secondary sort key for a corvette building object.
    /// </summary>
    private static string GetSortKey(int priority, string objectId, int origIndex)
    {
        // Access (5) and Cockpit (6) preserve original index
        if (priority == Data.StarshipDatabase.AccessPriority ||
            priority == Data.StarshipDatabase.CockpitPriority)
        {
            return origIndex.ToString("D6");
        }

        // Other (int.MaxValue): alphabetical by object list display name, fallback to ObjectID
        if (priority == Data.StarshipDatabase.OtherPriority)
        {
            string displayName = Data.StarshipDatabase.GetDisplayName(objectId);
            return !string.IsNullOrEmpty(displayName) ? displayName : objectId;
        }

        // Categories 1 -> 4 (Reactor, Thruster, Wing, Gear) by fixed sub-order
        return Data.StarshipDatabase.GetSubOrder(objectId).ToString("D6");
    }
}
