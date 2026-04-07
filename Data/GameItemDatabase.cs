using System.Collections.Concurrent;
using System.Text.Json;

namespace NMSE.Data;

/// <summary>
/// Loads and manages game item data from JSON database files.
/// </summary>
public class GameItemDatabase
{
    private readonly Dictionary<string, GameItem> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _corvetteBasePartTechMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Stores original English values for items that have been localised,
    /// keyed by item ID. Used by RevertLocalisation to restore defaults.
    /// </summary>
    private readonly Dictionary<string, (string Name, string NameLower, string Subtitle, string Description)> _englishBackup
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>All loaded game items keyed by their ID (case-insensitive).</summary>
    public IReadOnlyDictionary<string, GameItem> Items => _items;

    /// <summary>
    /// Maps corvette tech IDs (e.g. CV_INV2) to lists of base part product IDs
    /// (e.g. [B_HAB_A, B_HAB_B, B_HAB_C]) that can fulfil that tech slot.
    /// Built from items with non-empty BuildableShipTechID.
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> CorvetteBasePartTechMap => _corvetteBasePartTechMap;

    /// <summary>
    /// Loads game items from all JSON files in the specified directory.
    /// Each JSON file is an array of item objects. The filename (without extension)
    /// becomes the ItemType for all items in that file.
    /// Returns true if at least one JSON file was loaded successfully.
    /// </summary>
    public bool LoadItemsFromJsonDirectory(string jsonDirectory)
    {
        if (!Directory.Exists(jsonDirectory)) return false;

        var jsonFiles = Directory.GetFiles(jsonDirectory, "*.json");

        if (jsonFiles.Length > 0)
        {
            // Parse all JSON files in parallel for faster startup
            var bag = new ConcurrentBag<(string itemType, List<GameItem> items)>();

            Parallel.ForEach(jsonFiles, jsonFile =>
            {
                try
                {
                    string itemType = Path.GetFileNameWithoutExtension(jsonFile);

                    // Skip non-item-database files that have different schemas
                    // and are loaded separately (e.g. RewardDatabase, RecipeDatabase,
                    // FrigateTraitDatabase, SettlementDatabase, WikiGuideDatabase, etc.)
                    if (itemType.Equals("Rewards", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("Recipes", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("Words", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("FrigateTraits", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("SettlementPerks", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("WikiGuide", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("Titles", StringComparison.OrdinalIgnoreCase) ||
                        itemType.Equals("none", StringComparison.OrdinalIgnoreCase))
                        return;
                    var content = File.ReadAllBytes(jsonFile);
                    using var doc = JsonDocument.Parse(content);

                    if (doc.RootElement.ValueKind != JsonValueKind.Array) return;

                    var fileItems = new List<GameItem>();
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        if (!element.TryGetProperty("Id", out var idProp)) continue;
                        string id = idProp.GetString() ?? "";
                        if (string.IsNullOrEmpty(id)) continue;
                        if (!element.TryGetProperty("Name", out var nameProp)) continue;

                        var item = new GameItem
                        {
                            ItemType = itemType,
                            Id = id,
                            Name = nameProp.GetString() ?? "",
                            NameLower = element.TryGetProperty("NameLower", out var nameLowerProp) ? nameLowerProp.GetString() ?? "" : "",
                            Subtitle = element.TryGetProperty("Group", out var groupProp) ? groupProp.GetString() ?? "" : "",
                            Icon = element.TryGetProperty("Icon", out var iconProp) ? iconProp.GetString() ?? "" : "",
                            Symbol = element.TryGetProperty("Symbol", out var symbolProp) ? symbolProp.GetString() ?? "" : "",
                            Description = element.TryGetProperty("Description", out var descProp) ? descProp.GetString() ?? "" : "",
                            NameLocStr = element.TryGetProperty("Name_LocStr", out var nls) ? nls.GetString() : null,
                            NameLowerLocStr = element.TryGetProperty("NameLower_LocStr", out var nlls) ? nlls.GetString() : null,
                            SubtitleLocStr = element.TryGetProperty("Subtitle_LocStr", out var sls) ? sls.GetString() : null,
                            DescriptionLocStr = element.TryGetProperty("Description_LocStr", out var dls) ? dls.GetString() : null,
                        };

                        if (element.TryGetProperty("CookingIngredient", out var cookProp) && cookProp.ValueKind == JsonValueKind.True)
                            item.IsCooking = true;

                        if (element.TryGetProperty("MaxStackSize", out var stackProp) && stackProp.TryGetInt32(out int stackVal))
                            item.MaxStackSize = stackVal;

                        // Technology items store charge capacity as "ChargeAmount",
                        // product items store it as "ChargeValue". Read both and use
                        // whichever is present (ChargeAmount takes priority).
                        if (element.TryGetProperty("ChargeAmount", out var chargeAmtProp) && chargeAmtProp.TryGetInt32(out int chargeAmt))
                            item.ChargeValue = chargeAmt;
                        else if (element.TryGetProperty("ChargeValue", out var chargeProp) && chargeProp.TryGetInt32(out int chargeVal))
                            item.ChargeValue = chargeVal;

                        if (element.TryGetProperty("Chargeable", out var chargeableProp) && chargeableProp.ValueKind == JsonValueKind.True)
                            item.IsChargeable = true;

                        if (element.TryGetProperty("BuildFullyCharged", out var bfcProp) && bfcProp.ValueKind == JsonValueKind.True)
                            item.BuildFullyCharged = true;

                        if (element.TryGetProperty("BuildableShipTechID", out var bstProp))
                            item.BuildableShipTechID = bstProp.GetString() ?? "";

                        if (element.TryGetProperty("Rarity", out var rarProp))
                            item.Rarity = rarProp.GetString() ?? "";

                        if (element.TryGetProperty("Quality", out var qualProp))
                            item.Quality = qualProp.GetString() ?? "";

                        // NOTE:
                        // Category field is dual-purpose in the JSON:
                        // - For technology items it's the TechnologyCategory (Suit, Ship, Weapon, etc.)
                        // - For substances it's the substance category (Fuel, Metal, etc.)
                        // - For other items it may be a general grouping
                        // Always read into Category (for picker filtering).
                        // Only set TechnologyCategory for technology-related item types
                        // where the Category value is a real Technology Category
                        // (Suit, Ship, Weapon, etc.). Raw Materials and other products use
                        // Category for substance/product grouping (Fuel, Metal, Earth, etc.)
                        // which must NOT be treated as technology categories - otherwise
                        // the owner filter incorrectly rejects substances from cargo inventories.
                        if (element.TryGetProperty("Category", out var catProp))
                        {
                            string catVal = catProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(catVal) && catVal != "None")
                            {
                                item.Category = catVal;
                                if (IsTechnologyRelatedType(itemType))
                                    item.TechnologyCategory = catVal;
                            }
                        }

                        // Corvette items use CorvettePartCategory instead of Category
                        // for their grouping (Cockpit, Wing, Engine, etc.)
                        if (string.IsNullOrEmpty(item.Category) &&
                            element.TryGetProperty("CorvettePartCategory", out var cpcProp))
                        {
                            string cpcVal = cpcProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(cpcVal) && !cpcVal.Equals("None", StringComparison.OrdinalIgnoreCase))
                                item.Category = cpcVal;
                        }

                        // Cosmetic items from "Others" (figurines, etc.) that have a
                        // Group indicating they belong on starships but no Category
                        // need an implied Category so they appear in the item picker.
                        if ((string.IsNullOrEmpty(item.Category) || item.Category.Equals("None", StringComparison.OrdinalIgnoreCase)) &&
                            itemType.Equals("Others", StringComparison.OrdinalIgnoreCase) &&
                            element.TryGetProperty("Group", out var grpProp))
                        {
                            string grpVal = grpProp.GetString() ?? "";
                            if (grpVal.Contains("Starship", StringComparison.OrdinalIgnoreCase))
                            {
                                item.Category = "AllShips";
                                item.TechnologyCategory = "AllShips";
                            }
                        }

                        if (element.TryGetProperty("Upgrade", out var upgProp) && upgProp.ValueKind == JsonValueKind.True)
                            item.IsUpgrade = true;

                        if (element.TryGetProperty("Core", out var coreProp) && coreProp.ValueKind == JsonValueKind.True)
                            item.IsCore = true;

                        if (element.TryGetProperty("DeploysInto", out var deployProp))
                            item.DeploysInto = deployProp.GetString() ?? "";

                        if (element.TryGetProperty("Procedural", out var procProp) && procProp.ValueKind == JsonValueKind.True)
                            item.IsProcedural = true;

                        if (element.TryGetProperty("IsCraftable", out var craftProp) && craftProp.ValueKind == JsonValueKind.True)
                            item.IsCraftable = true;

                        if (element.TryGetProperty("TradeCategory", out var tradeCatProp))
                            item.TradeCategory = tradeCatProp.GetString() ?? "";

                        if (element.TryGetProperty("CanPickUp", out var canPickUpProp) && canPickUpProp.ValueKind == JsonValueKind.True)
                            item.CanPickUp = true;

                        if (element.TryGetProperty("IsTemporary", out var isTempProp) && isTempProp.ValueKind == JsonValueKind.True)
                            item.IsTemporary = true;

                        // Procedural technology detection for Upgrades/Technology Module:
                        // Items with StatLevels AND no DeploysInto are procedural tech.
                        // Items WITH DeploysInto are consumable products (techpacks) that
                        // deploy into the procedural tech - they stay as Product.
                        bool hasStatLevels = element.TryGetProperty("StatLevels", out var slProp)
                            && slProp.ValueKind == JsonValueKind.Array
                            && slProp.GetArrayLength() > 0;
                        bool hasDeploysInto = !string.IsNullOrEmpty(item.DeploysInto);

                        if (hasStatLevels && !hasDeploysInto
                            && (itemType.Equals("Upgrades", StringComparison.OrdinalIgnoreCase)
                                || itemType.Equals("Technology Module", StringComparison.OrdinalIgnoreCase)))
                        {
                            item.IsProcedural = true;
                        }

                        fileItems.Add(item);
                    }

                    if (fileItems.Count > 0)
                        bag.Add((itemType, fileItems));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to parse JSON file {Path.GetFileName(jsonFile)}: {ex.Message}");
                }
            });

            // Merge results into the single-threaded dictionary
            foreach (var (_, items) in bag)
            {
                foreach (var item in items)
                    _items[item.Id] = item;
            }
        }

        // Load procedural stubs so items like ^UP_FRHYP, ^PROC_LOOT etc. are resolvable
        LoadProceduralStubs();

        // Build mapping from corvette tech IDs to their possible base part product IDs
        BuildCorvetteBasePartTechMap();

        // Resolve TechnologyCategory for Technology Module items via their DeploysInto link
        ResolveTechModuleCategories();

        return _items.Count > 0;
    }

    /// <summary>
    /// Loads procedural product stubs into the item dictionary.
    /// These cover items that appear in save data but are not in the JSON files
    /// (e.g. ^UP_FRHYP, ^PROC_LOOT). Stored without the ^ prefix.
    /// </summary>
    private void LoadProceduralStubs()
    {
        foreach (var stub in ProceduralStubs.Items)
        {
            // Only add if not already present from JSON files (don't overwrite)
            if (!_items.ContainsKey(stub.Id))
            {
                _items[stub.Id] = new GameItem
                {
                    Id = stub.Id,
                    Name = stub.Name,
                    Icon = stub.Icon,
                    Category = stub.Category,
                    Subtitle = stub.Subtitle,
                    Description = stub.Description,
                    ItemType = "ProceduralProduct",
                };
            }
        }
    }

    /// <summary>
    /// For "Technology Module" product items that have a DeploysInto field,
    /// resolves the TechnologyCategory from the target technology item.
    /// This allows inventory filtering to work for tech module items too.
    /// </summary>
    private void ResolveTechModuleCategories()
    {
        foreach (var item in _items.Values)
        {
            if (string.IsNullOrEmpty(item.DeploysInto)) continue;
            if (!string.IsNullOrEmpty(item.TechnologyCategory)) continue;

            // Look up the technology this module deploys into
            if (_items.TryGetValue(item.DeploysInto, out var targetTech) &&
                !string.IsNullOrEmpty(targetTech.TechnologyCategory))
            {
                item.TechnologyCategory = targetTech.TechnologyCategory;
            }
        }
    }

    /// <summary>
    /// Builds a mapping from corvette tech IDs (e.g. CV_INV2) to lists of
    /// base part product IDs (e.g. [B_HAB_A, B_HAB_B, B_HAB_C]).
    /// This is used to resolve which actual base part icon to display for
    /// a ^CV_ tech item in a corvette's technology inventory.
    /// </summary>
    private void BuildCorvetteBasePartTechMap()
    {
        _corvetteBasePartTechMap.Clear();
        foreach (var item in _items.Values)
        {
            if (string.IsNullOrEmpty(item.BuildableShipTechID)) continue;
            if (!_corvetteBasePartTechMap.TryGetValue(item.BuildableShipTechID, out var list))
            {
                list = new List<string>();
                _corvetteBasePartTechMap[item.BuildableShipTechID] = list;
            }
            list.Add(item.Id);
        }
    }

    /// <summary>Looks up a game item by ID, handling ^-prefixed and T_-prefixed variants.</summary>
    /// <param name="id">The item ID to look up.</param>
    /// <returns>The matching <see cref="GameItem"/>, or null if not found.</returns>
    public GameItem? GetItem(string id)
    {
        if (_items.TryGetValue(id, out var item))
            return item;

        // Save files use ^-prefixed IDs (e.g. ^FUEL1), but JSON data stores
        // IDs without the prefix (e.g. FUEL1). Try stripping the ^ prefix.
        if (id.Length > 1 && id[0] == '^')
        {
            string stripped = id[1..];
            if (_items.TryGetValue(stripped, out item))
                return item;

            // IDs like T_BOBBLE_APOLLO -> look up BOBBLE_APOLLO (strip T_ prefix)
            if (stripped.StartsWith("T_", StringComparison.OrdinalIgnoreCase) && stripped.Length > 2)
            {
                if (_items.TryGetValue(stripped[2..], out item))
                    return item;
            }
        }

        // Also handle non-^ prefixed T_ IDs
        if (id.StartsWith("T_", StringComparison.OrdinalIgnoreCase) && id.Length > 2)
        {
            if (_items.TryGetValue(id[2..], out item))
                return item;
        }

        return null;
    }

    /// <summary>
    /// Returns true if an item ID should be excluded from picker/dialog lists.
    /// Items with IDs starting with "U_TECH" are format references and not usable items.
    /// </summary>
        /// <summary>
    /// Items excluded from the item picker (blacklists):
    /// <list type="bullet">
    ///   <item>Item ID prefixes that are special/internal.</item>
    ///   <item>Categories that aren't real inventory items.</item>
    /// </list>
    /// </summary>
    private static readonly string[] PickerExcludedPrefixes =
    [
        "U_TECH",          // Internal tech template
        "SPEC_HOOD01",     // Special cosmetic
        "SPEC_XOHELMET",   // Special cosmetic
        "SPEC_DIVEHELMET", // Special cosmetic
        "TWITCH",          // Twitch rewards
        "SPEC_BB",         // Special byte beat
        "EXPD",            // Expedition rewards
        "TITLE_UNLOCK",    // Title unlocks
        "SWITCH",          // Switch-specific items
        "OBSOLETE",        // Obsolete items
    ];

    /// <summary>
    /// Exact item IDs to exclude from pickers (season items that don't match prefix patterns).
    /// </summary>
    private static readonly HashSet<string> PickerExcludedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "^LAUNCHER_SPEC", "^SHIPJUMP_SPEC", "^HYPERDRIVE_SPEC", "^SHIP_LIFESUP",
        "^WORMTECH", "^WORMREADER", "^PIRATE_MAPPROD0", "^PIRATE_MAPPROD1",
        "^PIRATE_MAPPROD2", "^PIRATE_MAPWHOLE", "^PIRATE_BEACON", "^PIRATE_INVITE",
        "^GAS_BRAIN_LOC", "^ROGUE_BEACON", "^ROGUE_FINDER", "^ROGUE_CRAFTBOX",
        "^F_LIFESUPP", "^S8_BEACON",
    };

    /// <summary>Checks whether an item ID should be excluded from the item picker.</summary>
    public static bool IsPickerExcluded(string id)
    {
        if (PickerExcludedIds.Contains(id))
            return true;

        // Check for _DMG suffix (damage variants)
        if (id.Contains("_DMG", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var prefix in PickerExcludedPrefixes)
        {
            if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the item type (JSON filename) corresponds to a technology-related
    /// classification where the Category field represents a Technology Category
    /// (Suit, Ship, Weapon, Freighter, etc.) rather than a substance/product category
    /// (Fuel, Metal, Earth, Special, etc.).
    /// </summary>
    internal static bool IsTechnologyRelatedType(string itemType)
    {
        return itemType.Equals("Technology", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Upgrades", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Technology Module", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Constructed Technology", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Exocraft", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Starships", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Others", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns all items matching the specified category.</summary>
    /// <param name="category">The category to filter by (case-insensitive).</param>
    /// <returns>An enumerable of matching items.</returns>
    public IEnumerable<GameItem> GetItemsByCategory(string category) =>
        _items.Values.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns all items matching the specified item type.</summary>
    /// <param name="type">The item type to filter by (case-insensitive).</param>
    /// <returns>An enumerable of matching items.</returns>
    public IEnumerable<GameItem> GetItemsByType(string type) =>
        _items.Values.Where(i => i.ItemType.Equals(type, StringComparison.OrdinalIgnoreCase));

    /// <summary>Searches items by name, ID, or description containing the query string.</summary>
    /// <param name="query">The search text (case-insensitive).</param>
    /// <returns>An enumerable of matching items.</returns>
    public IEnumerable<GameItem> Search(string query)
    {
        var q = query.ToLowerInvariant();
        return _items.Values.Where(i =>
            i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            i.Description.Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Applies localised display values from the specified localisation service to all items.
    /// Backs up the original English values so they can be restored via <see cref="RevertLocalisation"/>.
    /// Items without _LocStr keys or without a matching translation are left unchanged.
    /// </summary>
    /// <param name="service">The localisation service with a loaded language.</param>
    /// <returns>The number of items that had at least one field localised.</returns>
    public int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive)
        {
            RevertLocalisation();
            return 0;
        }

        int count = 0;
        foreach (var item in _items.Values)
        {
            bool changed = false;

            // Back up originals if not already backed up
            if (!_englishBackup.ContainsKey(item.Id))
                _englishBackup[item.Id] = (item.Name, item.NameLower, item.Subtitle, item.Description);

            // Restore English baseline first (in case a previous language was applied)
            var backup = _englishBackup[item.Id];
            item.Name = backup.Name;
            item.NameLower = backup.NameLower;
            item.Subtitle = backup.Subtitle;
            item.Description = backup.Description;

            // Apply localised values where available.
            // Upgrade modules use a base loc key (e.g. "UP_SHIELDBOOST") while the
            // actual lang-file entries are numbered (UP_SHIELDBOOST3_NAME) or suffixed
            // for black-market variants (UP_SHIELDBOOST_X_NAME). We extract the level
            // suffix from the item ID and try the most specific key first.
            //
            // When the Name_LocStr chain fails (common for procedural tech whose
            // Name_LocStr base differs from the lang-file base, e.g. "UP_HYPERDRIVE"
            // vs lang key "UP_HYPER4_NAME"), we fall back to deriving the name key
            // from the DescriptionLocStr by replacing the _DESC suffix with _NAME.
            if (!string.IsNullOrEmpty(item.NameLocStr))
            {
                var loc = service.Lookup(item.NameLocStr)
                    ?? service.Lookup(item.NameLocStr + "_NAME");

                // Try level-specific key by extracting suffix from item ID
                if (loc == null && item.Id.Length > 0)
                {
                    char lastChar = item.Id[^1];
                    if (lastChar == 'X' || lastChar == 'x')
                        loc = service.Lookup(item.NameLocStr + "_X_NAME");
                    else if (char.IsDigit(lastChar))
                        loc = service.Lookup(item.NameLocStr + lastChar + "_NAME");
                }

                loc ??= service.Lookup(item.NameLocStr + "1_NAME");

                // Fallback: derive name key from DescriptionLocStr when available.
                // e.g. DescriptionLocStr "UP_HYPER4_DESC" -> try "UP_HYPER4_NAME".
                // For X-suffixed keys without underscore (e.g. "UP_SHOTGUNX_DESC"),
                // also try inserting "_" before X -> "UP_SHOTGUN_X_NAME".
                if (loc == null && !string.IsNullOrEmpty(item.DescriptionLocStr)
                    && item.DescriptionLocStr.EndsWith("_DESC", StringComparison.Ordinal))
                {
                    string descBase = item.DescriptionLocStr[..^5]; // strip "_DESC"
                    loc = service.Lookup(descBase + "_NAME");
                    if (loc == null && descBase.EndsWith("X", StringComparison.OrdinalIgnoreCase))
                    {
                        // "UP_SHOTGUNX" -> "UP_SHOTGUN_X"
                        string withUnderscore = descBase[..^1] + "_X";
                        loc = service.Lookup(withUnderscore + "_NAME");
                    }
                }

                if (loc != null) { item.Name = loc; changed = true; }
            }

            if (!string.IsNullOrEmpty(item.NameLowerLocStr))
            {
                var loc = service.Lookup(item.NameLowerLocStr)
                    ?? service.Lookup(item.NameLowerLocStr + "_L");
                if (loc != null) { item.NameLower = loc; changed = true; }
            }
            // Fallback: derive name-lower key from DescriptionLocStr
            if (string.IsNullOrEmpty(item.NameLowerLocStr)
                && !string.IsNullOrEmpty(item.DescriptionLocStr)
                && item.DescriptionLocStr.EndsWith("_DESC", StringComparison.Ordinal))
            {
                string descBase = item.DescriptionLocStr[..^5];
                var loc = service.Lookup(descBase + "_NAME_L");
                if (loc == null && descBase.EndsWith("X", StringComparison.OrdinalIgnoreCase))
                    loc = service.Lookup(descBase[..^1] + "_X_NAME_L");
                if (loc != null) { item.NameLower = loc; changed = true; }
            }

            if (!string.IsNullOrEmpty(item.SubtitleLocStr))
            {
                var loc = service.Lookup(item.SubtitleLocStr);
                if (loc != null) { item.Subtitle = loc; changed = true; }
            }
            // Fallback: derive subtitle key from DescriptionLocStr
            if (string.IsNullOrEmpty(item.SubtitleLocStr)
                && !string.IsNullOrEmpty(item.DescriptionLocStr)
                && item.DescriptionLocStr.EndsWith("_DESC", StringComparison.Ordinal))
            {
                string descBase = item.DescriptionLocStr[..^5];
                var loc = service.Lookup(descBase + "_SUB");
                if (loc == null && descBase.EndsWith("X", StringComparison.OrdinalIgnoreCase))
                    loc = service.Lookup(descBase[..^1] + "_X_SUB");
                if (loc != null) { item.Subtitle = loc; changed = true; }
            }

            if (!string.IsNullOrEmpty(item.DescriptionLocStr))
            {
                var loc = service.Lookup(item.DescriptionLocStr);
                // Also handle X-underscore variant for description
                if (loc == null && item.DescriptionLocStr.EndsWith("_DESC", StringComparison.Ordinal))
                {
                    string descBase = item.DescriptionLocStr[..^5];
                    if (descBase.EndsWith("X", StringComparison.OrdinalIgnoreCase))
                        loc = service.Lookup(descBase[..^1] + "_X_DESC");
                }
                if (loc != null) { item.Description = loc; changed = true; }
            }

            if (changed) count++;
        }

        return count;
    }

    /// <summary>
    /// Restores all item display values to their original English defaults
    /// from the DB JSON files, undoing any localisation applied by <see cref="ApplyLocalisation"/>.
    /// </summary>
    public void RevertLocalisation()
    {
        foreach (var kvp in _englishBackup)
        {
            if (_items.TryGetValue(kvp.Key, out var item))
            {
                item.Name = kvp.Value.Name;
                item.NameLower = kvp.Value.NameLower;
                item.Subtitle = kvp.Value.Subtitle;
                item.Description = kvp.Value.Description;
            }
        }

        _englishBackup.Clear();
    }
}
