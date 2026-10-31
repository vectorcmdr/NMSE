namespace NMSE.Data;

/// <summary>Represents stack size limits for a specific inventory group.</summary>
public class StackSizeEntry
{
    /// <summary>Inventory group name (e.g. "Personal", "Ship", "Freighter").</summary>
    public string Group { get; init; } = "";
    /// <summary>Maximum product stack multiplier for this group.</summary>
    public int Product { get; init; }
    /// <summary>Maximum substance stack size for this group.</summary>
    public int Substance { get; init; }

    /// <summary>Returns a summary string of the group's stack limits.</summary>
    public override string ToString() => $"{Group}: product={Product}, substance={Substance}";
}

/// <summary>Provides inventory stack size data per difficulty level and inventory group.</summary>
public static class InventoryStackDatabase
{
    /// <summary>Stack size entries indexed by difficulty name ("High", "Normal", "Low").</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<StackSizeEntry>> ByDifficulty =
        new Dictionary<string, IReadOnlyList<StackSizeEntry>>(StringComparer.Ordinal)
        {
            ["High"] = new List<StackSizeEntry>
            {
                new() { Group = "Default", Product = 5, Substance = 9999 },
                new() { Group = "Personal", Product = 10, Substance = 9999 },
                new() { Group = "PersonalCargo", Product = 10, Substance = 9999 },
                new() { Group = "Ship", Product = 10, Substance = 9999 },
                new() { Group = "ShipCargo", Product = 10, Substance = 9999 },
                new() { Group = "Freighter", Product = 20, Substance = 9999 },
                new() { Group = "FreighterCargo", Product = 20, Substance = 9999 },
                new() { Group = "Vehicle", Product = 10, Substance = 9999 },
                new() { Group = "Chest", Product = 20, Substance = 9999 },
                new() { Group = "BaseCapsule", Product = 100, Substance = 9999 },
                new() { Group = "MaintenanceObject", Product = 10, Substance = 9999 },
                new() { Group = "UIPopup", Product = 1, Substance = 9999 },
                new() { Group = "SeasonTransfer", Product = 20, Substance = 9999 },
            },
            ["Normal"] = new List<StackSizeEntry>
            {
                new() { Group = "Default", Product = 5, Substance = 500 },
                new() { Group = "Personal", Product = 10, Substance = 500 },
                new() { Group = "PersonalCargo", Product = 10, Substance = 500 },
                new() { Group = "Ship", Product = 10, Substance = 1000 },
                new() { Group = "ShipCargo", Product = 10, Substance = 1000 },
                new() { Group = "Freighter", Product = 10, Substance = 2000 },
                new() { Group = "FreighterCargo", Product = 20, Substance = 2000 },
                new() { Group = "Vehicle", Product = 10, Substance = 1000 },
                new() { Group = "Chest", Product = 20, Substance = 1000 },
                new() { Group = "BaseCapsule", Product = 100, Substance = 2000 },
                new() { Group = "MaintenanceObject", Product = 10, Substance = 250 },
                new() { Group = "UIPopup", Product = 1, Substance = 250 },
                new() { Group = "SeasonTransfer", Product = 20, Substance = 9999 },
            },
            ["Low"] = new List<StackSizeEntry>
            {
                new() { Group = "Default", Product = 3, Substance = 150 },
                new() { Group = "Personal", Product = 3, Substance = 300 },
                new() { Group = "PersonalCargo", Product = 5, Substance = 300 },
                new() { Group = "Ship", Product = 3, Substance = 300 },
                new() { Group = "ShipCargo", Product = 5, Substance = 750 },
                new() { Group = "Freighter", Product = 5, Substance = 750 },
                new() { Group = "FreighterCargo", Product = 10, Substance = 750 },
                new() { Group = "Vehicle", Product = 3, Substance = 300 },
                new() { Group = "Chest", Product = 10, Substance = 1000 },
                new() { Group = "BaseCapsule", Product = 50, Substance = 1250 },
                new() { Group = "MaintenanceObject", Product = 5, Substance = 150 },
                new() { Group = "UIPopup", Product = 1, Substance = 150 },
                new() { Group = "SeasonTransfer", Product = 20, Substance = 9999 },
            },
        };

    /// <summary>Returns the stack size entry for the given difficulty and inventory group.</summary>
    /// <param name="difficulty">Difficulty level ("High", "Normal", or "Low").</param>
    /// <param name="group">Inventory group name (e.g. "Personal", "Ship").</param>
    /// <returns>The matching entry, or null if not found.</returns>
    public static StackSizeEntry? GetStackSize(string difficulty, string group)
    {
        if (ByDifficulty.TryGetValue(difficulty, out var entries))
        {
            foreach (var entry in entries)
            {
                if (string.Equals(entry.Group, group, StringComparison.Ordinal))
                    return entry;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the default stack size for an item type and inventory group.
    /// Uses "High" difficulty (most permissive) and falls back to the "Default"
    /// group when the requested group is not found.
    /// Technology and Product types both use the Product column because
    /// technology items are installed (not stacked) and share the same
    /// max-count limits as products.
    /// </summary>
    /// <param name="inventoryType">"Substance", "Product", or "Technology".</param>
    /// <param name="group">Inventory group (e.g. "Personal", "Ship", "Freighter").</param>
    public static int GetDefaultStackSize(string inventoryType, string group)
    {
        const string DefaultDifficulty = "High";

        var entry = GetStackSize(DefaultDifficulty, group)
                 ?? GetStackSize(DefaultDifficulty, "Default");

        if (entry == null)
            return inventoryType == "Substance" ? 9999 : 1;

        return inventoryType == "Substance" ? entry.Substance : entry.Product;
    }

    /// <summary>
    /// Calculates the correct MaxAmount for an item when adding it to an inventory.
    /// <list type="bullet">
    ///   <item>Substances: always 9999 (MaxAmountLimit)</item>
    ///   <item>Technology (chargeable): ChargeValue (charge capacity)</item>
    ///   <item>Technology (non-chargeable): 0 (installed, no amount bar)</item>
    ///   <item>Products: <c>ProductMaxStorageMultiplier x MaxStackSize</c></item>
    /// </list>
    /// The <c>ProductMaxStorageMultiplier</c> varies by inventory group
    /// (10 for personal/ship, 20 for chest/freighter-cargo, etc.).
    /// When MaxStackSize is 0 or missing for a product, falls back to 1.
    /// </summary>
    /// <param name="item">The game item to calculate the max amount for.</param>
    /// <param name="inventoryType">"Substance", "Product", or "Technology".</param>
    /// <param name="inventoryGroup">Optional inventory group (e.g. "Chest", "Ship").
    /// When provided, uses the group's Product multiplier from the High difficulty
    /// table.  Defaults to 10 when omitted.</param>
    public static int GetMaxAmount(GameItem item, string inventoryType, string? inventoryGroup = null)
    {
        const int MaxAmountLimit = 9999;

        if (inventoryType == "Substance")
            return MaxAmountLimit;

        // Technology: MaxAmount is ALWAYS the ChargeAmount from MXML, regardless
        // of IsChargeable.  Verified against the game save where EVERY technology
        // slot's MaxAmount equals the item's ChargeAmount (100, 80, 120, 200, etc.)
        // and confirmed by cross referencing other editor item databases which also
        // uses the same ChargeAmount value.  For procedural items (UP_*) ChargeAmount
        // is inherited from the Template entry in the main GcTechnologyTable.
        if (inventoryType == "Technology")
            return item.ChargeValue;

        // Products: multiplier * MaxStackSize
        // The multiplier comes from the inventory group's ProductMaxStorageMultiplier.
        // We derive it from the ByDifficulty table using the "High" tier for max permissiveness.
        int multiplier = 10;
        if (!string.IsNullOrEmpty(inventoryGroup))
        {
            var entry = GetStackSize("High", inventoryGroup);
            if (entry != null)
                multiplier = entry.Product;
        }

        if (item.MaxStackSize > 0)
            return multiplier * item.MaxStackSize;

        // Fallback for products with no MaxStackSize data
        return 1;
    }

    /// <summary>
    /// Maps an item's <c>ItemType</c> from the JSON database to the save-file
    /// <c>InventoryType</c> enum value used in inventory slot JSON.
    /// <para>
    /// Only items from the "Technology" JSON file are classified as Technology.
    /// "Constructed Technology", "Technology Module", and "Upgrades" are default-Product;
    /// individual items may override via <see cref="ResolveInventoryTypeForItem"/>
    /// when <c>IsProcedural</c> is set.
    /// </para>
    /// </summary>
    public static string ResolveInventoryType(string? itemType)
    {
        if (string.IsNullOrEmpty(itemType))
            return "Product";

        if (itemType.Equals("Technology", StringComparison.OrdinalIgnoreCase))
            return "Technology";

        if (itemType.Equals("Raw Materials", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("substance", StringComparison.OrdinalIgnoreCase))
            return "Substance";

        return "Product";
    }

    /// <summary>
    /// Resolves the save-file <c>InventoryType</c> for a specific <see cref="GameItem"/>.
    /// <para>
    /// Procedural items are always Technology.  Non-procedural items from
    /// technology-source JSON files ("Technology", "Upgrades",
    /// "Constructed Technology") are Technology when they carry charge data
    /// (<c>ChargeValue &gt; 0</c>, populated from the game's <c>ChargeAmount</c>
    /// field).  Items in those same files without charge data are products
    /// that happen to live in the technology table (e.g. NAV_DATA_DROP).
    /// </para>
    /// </summary>
    public static string ResolveInventoryTypeForItem(GameItem item)
    {
        return ResolveInventoryTypeForItem(item, isTechInventory: false);
    }

    /// <summary>
    /// Item IDs of the four atmospheric gases (Sulphurine, Radon, Nitrogen, Methane).
    /// These are entries in the game's substance table and are stored in saves with
    /// InventoryType "Substance".
    /// </summary>
    private static readonly HashSet<string> GasItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "GAS1", "GAS2", "GAS3", "GAS4",
    };

    /// <summary>
    /// Returns whether the given item ID is one of the four atmospheric gases.
    /// </summary>
    private static bool IsGasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return GasItemIds.Contains(itemId)
            || GasItemIds.Contains(itemId.TrimStart('^'));
    }

    /// <summary>
    /// Resolves the save-file <c>InventoryType</c> for a specific <see cref="GameItem"/>
    /// within the context of the target inventory.
    /// <para>
    /// Procedural items are always Technology.  For non-procedural items,
    /// <c>SourceTable</c> is authoritative: Product and Substance entries stay
    /// Product/Substance even if they carry <c>ChargeValue</c> (for example the
    /// product fuel cell SUBFUEL).  Technology-table entries are Technology when
    /// the target inventory is tech-only, or when they carry charge data.
    /// </para>
    /// <para>
    /// For legacy data without a <c>SourceTable</c>, items from technology-source
    /// JSON files ("Technology", "Upgrades", "Constructed Technology",
    /// "Exocraft") with charge data fall back to Technology.
    /// </para>
    /// </summary>
    public static string ResolveInventoryTypeForItem(GameItem item, bool isTechInventory)
    {
        // Atmospheric gases are substances in the game regardless of how they
        // are classified in the item database (they historically lived in
        // Products.json due to extractor categorisation, which would otherwise
        // resolve them as Product and produce slots the game silently drops).
        if (IsGasItem(item.Id))
            return "Substance";

        // Procedural items are always technology in the save file, regardless
        // of the JSON file they came from.
        if (item.IsProcedural)
            return "Technology";

        // SourceTable is authoritative for mixed-type JSON files such as
        // Exocraft.json, Technology.json and Others.json.  Products and
        // substances may carry ChargeValue for gameplay purposes (for example
        // SUBFUEL, a product fuel cell) but they are not technology items.
        if (!string.IsNullOrEmpty(item.SourceTable))
        {
            if (item.SourceTable.Equals("Product", StringComparison.OrdinalIgnoreCase))
                return "Product";
            if (item.SourceTable.Equals("Substance", StringComparison.OrdinalIgnoreCase))
                return "Substance";
            if (isTechInventory
                && item.SourceTable.Equals("Technology", StringComparison.OrdinalIgnoreCase))
                return "Technology";
        }

        // Items from technology-source JSON files that carry charge data
        // (ChargeValue > 0, from ChargeAmount in the game data) are real
        // technology items.  This covers SourceTable Technology entries and
        // legacy data without a SourceTable (HDRIVEBOOST*, UT_SHIP*, etc.).
        if (item.ChargeValue > 0 && IsTechnologySourceType(item.ItemType))
            return "Technology";

        return ResolveInventoryType(item.ItemType);
    }

    /// <summary>
    /// Returns true for JSON file types that originate from the game's
    /// GcTechnologyTable.  Items in these files <em>may</em> be real
    /// technology (when they carry charge data) or stray products.
    /// </summary>
    private static bool IsTechnologySourceType(string itemType)
    {
        return itemType.Equals("Technology", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Upgrades", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Constructed Technology", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Exocraft", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves the save-file <c>InventoryType</c> for an item being written to an
    /// inventory slot.  Delegates to <see cref="ResolveInventoryTypeForItem"/> when
    /// a <see cref="GameItem"/> is available, falling back to the string-only
    /// <see cref="ResolveInventoryType"/> otherwise.
    /// </summary>
    /// <param name="itemType">The item's <c>ItemType</c> from the database.</param>
    /// <param name="isTechInventory">Whether the target inventory is a tech-only inventory (unused - kept for API compatibility).</param>
    public static string ResolveSaveInventoryType(string? itemType, bool isTechInventory)
    {
        return ResolveInventoryType(itemType);
    }

    /// <summary>
    /// Resolves the save-file <c>InventoryType</c> for a known <see cref="GameItem"/>
    /// being written to an inventory slot.  Prefers the item-aware overload so that
    /// Upgrades / Constructed Technology with charge data are correctly classified.
    /// </summary>
    public static string ResolveSaveInventoryType(GameItem item)
    {
        return ResolveSaveInventoryType(item, isTechInventory: false);
    }

    /// <summary>
    /// Resolves the save-file <c>InventoryType</c> for a known <see cref="GameItem"/>
    /// being written to an inventory slot, with knowledge of the target inventory context.
    /// </summary>
    public static string ResolveSaveInventoryType(GameItem item, bool isTechInventory)
    {
        return ResolveInventoryTypeForItem(item, isTechInventory);
    }

    /// <summary>
    /// Determines whether an item can be added to a specific inventory category:
    /// <list type="bullet">
    ///   <item>Tech-only inventories accept Technology items (including Upgrades
    ///     and procedural Technology Modules) and cosmetic items with a valid
    ///     TechnologyCategory.</item>
    ///   <item>Cargo inventories accept everything except Technology.</item>
    ///   <item>General inventories accept all item types.</item>
    ///   <item>Maintenance-category technology and Emote-category items are
    ///     always excluded.  Creature eggs are products and are not excluded.</item>
    /// </list>
    /// Base building products are treated like any other product.  The game's
    /// <c>CanPickUp</c>/<c>IsTemporary</c> flags describe whether a placed structure
    /// can be retrieved, not whether the item can be stored in an inventory, so they
    /// do not affect this filter.
    /// </summary>
    /// <param name="item">The game item to check.</param>
    /// <param name="isTechOnly">True if the target inventory is tech-only.</param>
    /// <param name="isCargo">True if the target inventory is cargo (no tech allowed).</param>
    public static bool CanAddItemToInventory(GameItem item, bool isTechOnly, bool isCargo)
    {
        string invType = ResolveInventoryTypeForItem(item, isTechOnly);

        // CanPickUp excludes maintenance-category technology
        if (invType == "Technology"
            && item.Category.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
            return false;

        // Emote unlock items are not storable and stay blacklisted.  Creature eggs
        // are ordinary products (ProductCategory "CreatureEgg") and must remain
        // addable to cargo and general inventories.
        if (item.Category.Equals("Emote", StringComparison.OrdinalIgnoreCase))
            return false;

        if (isTechOnly)
        {
            // Technology Module items (cargo-holdable fragments that deploy into
            // tech slots, e.g. U_SHIPSHIELD3) must never be placed directly into
            // technology inventories - they belong in cargo and unpack via
            // DeploysInto.  Exclude them before the TechnologyCategory check,
            // which would otherwise let them pass because they carry a Category
            // value like "Weapon" or "Ship".
            if (item.ItemType.Equals("Technology Module", StringComparison.OrdinalIgnoreCase))
                return false;

            // Tech-only inventories accept all Technology items (base tech,
            // upgrades, procedural tech modules).
            if (invType == "Technology")
                return true;
            // Cosmetic items from "Others" (trails, figurines), "Starships", and
            // "Exocraft" that have a TechnologyCategory can be installed in tech
            // slots matching their category (the game allows this).
            if (!string.IsNullOrEmpty(item.TechnologyCategory)
                && item.TechnologyCategory != "None")
                return true;
            return false;
        }

        if (isCargo)
        {
            // Cargo rejects true Technology items.  Items in tech-source JSON files
            // with charge data are always Technology.  Items whose SourceTable is
            // "Technology" are also true tech that should not go in cargo, even
            // though they resolve as Product for save-file purposes (e.g. ^S22_LINK
            // in Others.json).
            if (invType == "Technology")
                return false;
            if (!string.IsNullOrEmpty(item.SourceTable)
                && item.SourceTable.Equals("Technology", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        return true;
    }

    /// <summary>Total number of stack size entries across all difficulty levels.</summary>
    public static int Count
    {
        get
        {
            int total = 0;
            foreach (var kvp in ByDifficulty)
                total += kvp.Value.Count;
            return total;
        }
    }
}
