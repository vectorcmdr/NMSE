using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles multitool data operations including loading, saving, type/class management, and inventory export.
/// </summary>
internal static class MultitoolLogic
{
    /// <summary>
    /// Available multitool class grades, ordered from lowest to highest.
    /// </summary>
    internal static readonly string[] ToolClasses = { "C", "B", "A", "S" };

    /// <summary>
    /// Known multitool types with their display names and corresponding resource filenames.
    /// </summary>
    internal static readonly (string Name, string Filename)[] ToolTypes = new[]
    {
        ("Standard", "MODELS/COMMON/WEAPONS/MULTITOOL/MULTITOOL.SCENE.MBIN"),
        ("Rifle", "MODELS/COMMON/WEAPONS/MULTITOOL/YOURRIFLETEST.SCENE.MBIN"),
        ("Royal", "MODELS/COMMON/WEAPONS/MULTITOOL/ROYALMULTITOOL.SCENE.MBIN"),
        ("Alien", "MODELS/COMMON/WEAPONS/MULTITOOL/YOURALIENMULTITOOL.SCENE.MBIN"),
        ("Pristine", "MODELS/COMMON/WEAPONS/MULTITOOL/YOURPRISTINEMULTITOOL.SCENE.MBIN"),
        ("Sentinel", "MODELS/COMMON/WEAPONS/MULTITOOL/SENTINELMULTITOOL.SCENE.MBIN"),
        ("Sentinel B", "MODELS/COMMON/WEAPONS/MULTITOOL/SENTINELMULTITOOLB.SCENE.MBIN"),
        ("Switch", "MODELS/COMMON/WEAPONS/MULTITOOL/SWITCHMULTITOOL.SCENE.MBIN"),
        ("Staff", "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOL.SCENE.MBIN"),
        ("Staff NPC", "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFNPCMULTITOOL.SCENE.MBIN"),
        ("Staff Ruin", "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOLRUIN.SCENE.MBIN"),
        ("Staff Bone", "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOLBONE.SCENE.MBIN"),
        ("Atlas", "MODELS/COMMON/WEAPONS/MULTITOOL/ATLASMULTITOOL.SCENE.MBIN"),
        ("Atlas Scepter", "MODELS/COMMON/WEAPONS/MULTITOOL/STAFFMULTITOOLATLAS.SCENE.MBIN"),
    };

    internal static readonly Dictionary<string, string> ToolTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Standard"] = "multitool.type_standard",
        ["Rifle"] = "multitool.type_rifle",
        ["Royal"] = "multitool.type_royal",
        ["Alien"] = "multitool.type_alien",
        ["Pristine"] = "multitool.type_pristine",
        ["Sentinel"] = "multitool.type_sentinel",
        ["Sentinel B"] = "multitool.type_sentinel_b",
        ["Switch"] = "multitool.type_switch",
        ["Staff"] = "multitool.type_staff",
        ["Staff NPC"] = "multitool.type_staff_npc",
        ["Staff Ruin"] = "multitool.type_staff_ruin",
        ["Staff Bone"] = "multitool.type_staff_bone",
        ["Atlas"] = "multitool.type_atlas",
        ["Atlas Scepter"] = "multitool.type_atlas_scepter",
    };

    internal static string GetLocalisedToolTypeName(string internalName)
    {
        if (ToolTypeLocKeys.TryGetValue(internalName, out var key))
            return UiStrings.Get(key);
        return internalName;
    }

    internal sealed class ToolTypeItem
    {
        public string InternalName { get; }
        public string DisplayName { get; }
        public ToolTypeItem(string internalName, string displayName) { InternalName = internalName; DisplayName = displayName; }
        public override string ToString() => DisplayName;
    }

    internal static ToolTypeItem[] GetToolTypeItems()
    {
        return ToolTypes.Select(t => new ToolTypeItem(t.Name, GetLocalisedToolTypeName(t.Name))).ToArray();
    }

    /// <summary>
    /// Builds a list of owned multitools from the multitools JSON array, skipping empty slots.
    /// </summary>
    /// <param name="multitools">The JSON array of multitool entries.</param>
    /// <returns>A list of tool items with display names and data indices.</returns>
    internal static List<ToolListItem> BuildToolList(JsonArray multitools)
    {
        var list = new List<ToolListItem>();
        for (int i = 0; i < multitools.Length; i++)
        {
            try
            {
                var tool = multitools.GetObject(i);
                var seedArr = tool?.GetArray("Seed");
                bool hasSeed = false;
                try { hasSeed = seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0); }
                catch { }

                if (!hasSeed) continue;

                string name = tool?.GetString("Name") ?? "";
                if (string.IsNullOrEmpty(name))
                    name = UiStrings.Format("multitool.default_name", i + 1);
                list.Add(new ToolListItem(name, i));
            }
            catch
            {
                list.Add(new ToolListItem(UiStrings.Format("multitool.default_name", i + 1), i));
            }
        }
        return list;
    }

    /// <summary>
    /// Loads multitool data from a JSON tool object for display and editing.
    /// </summary>
    /// <param name="tool">The JSON object representing the multitool.</param>
    /// <returns>A populated <see cref="ToolData"/> instance.</returns>
    internal static ToolData LoadToolData(JsonObject tool)
    {
        string name = tool.GetString("Name") ?? "";

        string filename = "";
        try
        {
            // NMS 3.81+ (Sentinel): multitool resource is under Resource.Filename
            var resource = tool.GetObject("Resource");
            filename = resource?.GetString("Filename") ?? "";
        }
        catch { }
        int typeIndex = Array.FindIndex(ToolTypes, t => t.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase));

        string cls = "";
        try
        {
            var store = tool.GetObject("Store");
            var classObj = store?.GetObject("Class");
            cls = classObj?.GetString("InventoryClass") ?? "";
        }
        catch { }
        int classIndex = Array.IndexOf(ToolClasses, cls);

        string seed = "";
        try
        {
            var seedArr = tool.GetArray("Seed");
            if (seedArr != null && seedArr.Length > 1)
                seed = seedArr.Get(1)?.ToString() ?? "";
        }
        catch { }

        var toolStore = tool.GetObject("Store");
        double damage = 0, mining = 0, scan = 0;
        try { damage = StatHelper.ReadBaseStatValue(toolStore, "^WEAPON_DAMAGE"); } catch { }
        try { mining = StatHelper.ReadBaseStatValue(toolStore, "^WEAPON_MINING"); } catch { }
        try { scan = StatHelper.ReadBaseStatValue(toolStore, "^WEAPON_SCAN"); } catch { }

        string safeName = StringHelper.SanitizeFileName(name);
        string cls2 = classIndex >= 0 ? ToolClasses[classIndex] : "C";

        return new ToolData
        {
            Name = name,
            TypeIndex = typeIndex >= 0 ? typeIndex : 0,
            ClassIndex = classIndex,
            Seed = seed,
            Damage = damage,
            Mining = mining,
            Scan = scan,
            Store = toolStore,
            ExportFileName = ExportConfig.BuildFileName(
                ExportConfig.Instance.MultitoolTemplate,
                ExportConfig.Instance.MultitoolExt,
                new Dictionary<string, string>
                {
                    ["multitool_name"] = safeName,
                    ["type"] = typeIndex >= 0 && typeIndex < ToolTypes.Length ? ToolTypes[typeIndex].Name : "Unknown",
                    ["class"] = cls2
                })
        };
    }

    /// <summary>
    /// Saves multitool data back to the JSON tool object and player state.
    /// </summary>
    /// <param name="tool">The JSON object representing the multitool.</param>
    /// <param name="playerState">The player state for primary weapon syncing.</param>
    /// <param name="values">The values to write.</param>
    /// <param name="isPrimary">Whether this tool is the active/primary multitool.</param>
    internal static void SaveToolData(JsonObject tool, JsonObject? playerState, ToolSaveValues values, bool isPrimary)
    {
        // Always write name (allow empty string to clear name)
        tool.Set("Name", values.Name ?? "");

        if (values.ClassIndex >= 0)
        {
            string cls = ToolClasses[values.ClassIndex];
            // Set class on all multitool inventories (Store, Store_TechOnly)
            var store = tool.GetObject("Store");
            var classObj = store?.GetObject("Class");
            classObj?.Set("InventoryClass", cls);
            var techStore = tool.GetObject("Store_TechOnly");
            var techClassObj = techStore?.GetObject("Class");
            techClassObj?.Set("InventoryClass", cls);
        }

        if (values.TypeIndex >= 0)
        {
            // NMS 3.81+ (Sentinel): multitool resource is under Resource.Filename
            var resource = tool.GetObject("Resource");
            resource?.Set("Filename", ToolTypes[values.TypeIndex].Filename);
        }

        try
        {
            var seedArr = tool.GetArray("Seed");
            var normalizedSeed = SeedHelper.NormalizeSeed(values.Seed);
            if (seedArr != null && seedArr.Length > 1 && normalizedSeed != null)
            {
                seedArr.Set(1, normalizedSeed);

                // If primary tool, also sync seed to CurrentWeapon.GenerationSeed[1]
                if (isPrimary && playerState != null)
                {
                    try
                    {
                        var currentWeapon = playerState.GetObject("CurrentWeapon");
                        var genSeed = currentWeapon?.GetArray("GenerationSeed");
                        if (genSeed != null && genSeed.Length > 1)
                            genSeed.Set(1, normalizedSeed);
                    }
                    catch { }
                }
            }
        }
        catch { }

        var toolStore = tool.GetObject("Store");
        StatHelper.WriteBaseStatValue(toolStore, "^WEAPON_DAMAGE", Data.BaseStatLimits.ConditionalClampStatValue("Normal", "^WEAPON_DAMAGE", values.Damage, Data.StatCategory.Weapon, values.RawStatValues));
        StatHelper.WriteBaseStatValue(toolStore, "^WEAPON_MINING", Data.BaseStatLimits.ConditionalClampStatValue("Normal", "^WEAPON_MINING", values.Mining, Data.StatCategory.Weapon, values.RawStatValues));
        StatHelper.WriteBaseStatValue(toolStore, "^WEAPON_SCAN", Data.BaseStatLimits.ConditionalClampStatValue("Normal", "^WEAPON_SCAN", values.Scan, Data.StatCategory.Weapon, values.RawStatValues));

        // If primary tool, sync Store to WeaponInventory in PlayerStateData
        // This keeps the game's live inventory copy in sync with the tool data
        if (isPrimary && playerState != null && toolStore != null)
        {
            try { playerState.Set("WeaponInventory", toolStore); }
            catch { }
        }
    }

    /// <summary>
    /// Fully resets a multitool slot by clearing its seed, name, resource, and all
    /// inventory data. The entry remains in the Multitools array to preserve index
    /// alignment. We clear every field in-place so no remnant data is left behind.
    /// </summary>
    /// <param name="tool">The JSON object representing the multitool to delete.</param>
    internal static void DeleteToolData(JsonObject tool)
    {
        // 1. Invalidate the seed - marks the slot as empty
        var seedArr = tool.GetArray("Seed");
        if (seedArr != null && seedArr.Length > 1)
        {
            seedArr.Set(0, false);
            seedArr.Set(1, "0x0");
        }

        // 2. Clear the name
        tool.Set("Name", "");

        // 3. Clear the resource (filename + seed + AltId + ProceduralTexture)
        var resource = tool.GetObject("Resource");
        if (resource != null)
        {
            resource.Set("Filename", "");
            var resSeed = resource.GetArray("Seed");
            if (resSeed != null && resSeed.Length > 1)
            {
                resSeed.Set(0, false);
                resSeed.Set(1, "0x0");
            }
            try { resource.Set("AltId", ""); } catch { }
            try
            {
                var pt = resource.GetObject("ProceduralTexture");
                if (pt != null)
                    ClearJsonArray(pt.GetArray("Samplers"));
            }
            catch { }
        }

        // 4. Clear both inventory objects (Store and Store_TechOnly)
        ResetInventoryObject(tool.GetObject("Store"));
        ResetInventoryObject(tool.GetObject("Store_TechOnly"));
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

    private static void ClearJsonArray(JsonArray? arr)
    {
        if (arr == null) return;
        for (int i = arr.Length - 1; i >= 0; i--)
            arr.RemoveAt(i);
    }

    /// <summary>
    /// Counts the number of valid (non-invalidated) multitools in the array.
    /// A tool is valid when its Seed[0] is true.
    /// </summary>
    internal static int CountValidTools(JsonArray multitools)
    {
        int count = 0;
        for (int i = 0; i < multitools.Length; i++)
        {
            try
            {
                var tool = multitools.GetObject(i);
                var seedArr = tool?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0))
                    count++;
            }
            catch { }
        }
        return count;
    }

    /// <summary>
    /// Returns the array index of the first valid (non-invalidated) multitool, or -1 if none.
    /// </summary>
    internal static int FindFirstValidToolIndex(JsonArray multitools)
    {
        for (int i = 0; i < multitools.Length; i++)
        {
            try
            {
                var tool = multitools.GetObject(i);
                var seedArr = tool?.GetArray("Seed");
                if (seedArr != null && seedArr.Length > 0 && seedArr.GetBool(0))
                    return i;
            }
            catch { }
        }
        return -1;
    }

    /// <summary>
    /// Finds the first empty multitool slot in the multitools array.
    /// </summary>
    /// <param name="multitools">The JSON array of multitool entries.</param>
    /// <returns>The index of the first empty slot, or -1 if all slots are occupied.</returns>
    internal static int FindEmptySlot(JsonArray multitools)
    {
        for (int i = 0; i < multitools.Length; i++)
        {
            try
            {
                var slot = multitools.GetObject(i);
                var seedArr = slot?.GetArray("Seed");
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
    /// Returns the display name for the multitool at the given index.
    /// </summary>
    /// <param name="multitools">The JSON array of multitool entries.</param>
    /// <param name="primaryIndex">The index of the primary multitool.</param>
    /// <returns>The multitool name, or a fallback if unavailable.</returns>
    internal static string GetPrimaryToolName(JsonArray? multitools, int primaryIndex)
    {
        if (multitools == null || primaryIndex < 0 || primaryIndex >= multitools.Length)
            return UiStrings.Get("common.unknown");
        try
        {
            var tool = multitools.GetObject(primaryIndex);
            string name = tool.GetString("Name") ?? "";
            return string.IsNullOrEmpty(name) ? UiStrings.Format("multitool.default_name", primaryIndex + 1) : name;
        }
        catch { return UiStrings.Get("common.unknown"); }
    }

    /// <summary>
    /// Represents an item in the multitool selection list.
    /// </summary>
    internal sealed class ToolListItem
    {
        /// <summary>The display name shown for this multitool.</summary>
        public string DisplayName { get; set; }
        /// <summary>The index of this multitool in the multitools array.</summary>
        public int DataIndex { get; }

        /// <summary>
        /// Initializes a new tool list item.
        /// </summary>
        /// <param name="displayName">The display name for the multitool.</param>
        /// <param name="dataIndex">The index in the multitools array.</param>
        public ToolListItem(string displayName, int dataIndex)
        {
            DisplayName = displayName;
            DataIndex = dataIndex;
        }

        /// <inheritdoc/>
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Holds loaded multitool data for display and editing in the UI.
    /// </summary>
    internal sealed class ToolData
    {
        /// <summary>The player-assigned multitool name.</summary>
        public string Name { get; set; } = "";
        /// <summary>Index into <see cref="ToolTypes"/> for the multitool type.</summary>
        public int TypeIndex { get; set; }
        /// <summary>Index into <see cref="ToolClasses"/> for the class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>The multitool's procedural generation seed as a hex string.</summary>
        public string Seed { get; set; } = "";
        /// <summary>The multitool's base damage stat.</summary>
        public double Damage { get; set; }
        /// <summary>The multitool's base mining stat.</summary>
        public double Mining { get; set; }
        /// <summary>The multitool's base scan stat.</summary>
        public double Scan { get; set; }
        /// <summary>The multitool's store (inventory) JSON object.</summary>
        public JsonObject? Store { get; set; }
        /// <summary>Suggested filename for exporting the inventory.</summary>
        public string ExportFileName { get; set; } = "";
    }

    /// <summary>
    /// Holds values to be saved back to a multitool's JSON data.
    /// </summary>
    internal sealed class ToolSaveValues
    {
        /// <summary>The multitool name to set.</summary>
        public string Name { get; set; } = "";
        /// <summary>Index into <see cref="ToolTypes"/> for the desired type.</summary>
        public int TypeIndex { get; set; } = -1;
        /// <summary>Index into <see cref="ToolClasses"/> for the desired class grade.</summary>
        public int ClassIndex { get; set; } = -1;
        /// <summary>The seed hex string to set.</summary>
        public string Seed { get; set; } = "";
        /// <summary>The damage stat value to write.</summary>
        public double Damage { get; set; }
        /// <summary>The mining stat value to write.</summary>
        public double Mining { get; set; }
        /// <summary>The scan stat value to write.</summary>
        public double Scan { get; set; }

        /// <summary>Raw (unclamped) stat values read from JSON at load time.
        /// When set, each stat is only written if the UI value differs from
        /// the clamped raw value - preserving externally-edited values.</summary>
        public Dictionary<string, double>? RawStatValues { get; set; }
    }
}
