namespace NMSE.Data;

/// <summary>Represents a single game item with its metadata from the JSON database.</summary>
public class GameItem
{
    /// <summary>Unique item identifier (e.g. "FUEL1").</summary>
    public string Id { get; set; } = "";
    /// <summary>Display name of the item.</summary>
    public string Name { get; set; } = "";
    /// <summary>Lowercase version of the display name for case-insensitive matching.</summary>
    public string NameLower { get; set; } = "";
    /// <summary>Subtitle or group label displayed under the item name.</summary>
    public string Subtitle { get; set; } = "";
    /// <summary>Full description text of the item.</summary>
    public string Description { get; set; } = "";

    /// <summary>Localisation lookup key for Name (e.g. "UI_FUEL_1_NAME"). Null when not available.</summary>
    public string? NameLocStr { get; set; }
    /// <summary>Localisation lookup key for NameLower (e.g. "UI_FUEL_1_NAME_L"). Null when not available.</summary>
    public string? NameLowerLocStr { get; set; }
    /// <summary>Localisation lookup key for Subtitle (e.g. "UI_FUEL1_SUB"). Null when not available.</summary>
    public string? SubtitleLocStr { get; set; }
    /// <summary>Localisation lookup key for Description (e.g. "UI_FUEL_1_DESC"). Null when not available.</summary>
    public string? DescriptionLocStr { get; set; }
    /// <summary>Item category (e.g. "Maintenance", "Emote").</summary>
    public string Category { get; set; } = "";
    /// <summary>Icon filename used to display this item (e.g. "FUEL1.png").</summary>
    public string Icon { get; set; } = "";
    /// <summary>Symbol identifier for the item's element symbol.</summary>
    public string Symbol { get; set; } = "";
    /// <summary>
    /// Raw MaxStackSize value from the JSON database.
    /// For products this is the StackMultiplier used in the formula 10 * MaxStackSize.
    /// For substances this value is typically 1 (substances always stack to 9999).
    /// For technology this is 0 (technology items use ChargeValue instead).
    /// </summary>
    public int MaxStackSize { get; set; }
    /// <summary>
    /// Charge capacity for technology items. Populated from ChargeAmount
    /// (tech-parsed items) or ChargeValue (product-parsed items) in the JSON database.
    /// For ALL technology items (chargeable and non-chargeable alike) this is the
    /// MXML ChargeAmount value — the game uses it as MaxAmount in save slots.
    /// For procedural items (UP_*) this value is inherited from the Template
    /// entry in the main GcTechnologyTable.
    /// </summary>
    public int ChargeValue { get; set; }
    /// <summary>
    /// Whether the technology item can be recharged by the player with resources.
    /// Only meaningful for technology-type items.
    /// </summary>
    public bool IsChargeable { get; set; }
    /// <summary>
    /// Whether this technology starts at full charge when first built/installed.
    /// From the MXML <c>BuildFullyCharged</c> field. When true the initial Amount
    /// equals <see cref="ChargeValue"/>; when false the initial Amount is 0.
    /// For procedural items (UP_*) this value is inherited from the Template
    /// entry in the main GcTechnologyTable.
    /// </summary>
    public bool BuildFullyCharged { get; set; }
    /// <summary>Whether this item is a cooking ingredient.</summary>
    public bool IsCooking { get; set; }
    /// <summary>Item type classification (e.g. "substance", "product", "Technology").</summary>
    public string ItemType { get; set; } = "";
    /// <summary>Corvette buildable ship tech ID this product corresponds to; empty if not a ship part.</summary>
    public string BuildableShipTechID { get; set; } = "";
    /// <summary>Rarity classification (e.g. "Normal", "Rare", "Epic", "Legendary").</summary>
    public string Rarity { get; set; } = "";
    /// <summary>Quality classification (e.g. "Normal", "Rare", "Epic", "Legendary").</summary>
    public string Quality { get; set; } = "";
    /// <summary>
    /// Technology category determining which entity type this tech belongs to.
    /// Values: Suit, Ship, AllShips, AllShipsExceptAlien, AlienShip, RobotShip,
    /// Corvette, Weapon, Freighter, AllVehicles, Exocraft, Colossus, Mech,
    /// Submarine, Maintenance, None. Only populated for Technology items.
    /// </summary>
    public string TechnologyCategory { get; set; } = "";
    /// <summary>Whether this technology is an upgrade module (true) vs base technology (false).</summary>
    public bool IsUpgrade { get; set; }
    /// <summary>Whether this is a core (non-removable) technology.</summary>
    public bool IsCore { get; set; }
    /// <summary>Whether this technology generates procedurally.</summary>
    public bool IsProcedural { get; set; }
    /// <summary>
    /// Whether this product item can be crafted by the player.
    /// Populated from the IsCraftable field in the game product table.
    /// Learnable products must be craftable.
    /// </summary>
    public bool IsCraftable { get; set; }
    /// <summary>
    /// Trade category for this product (e.g. "None", "SpecialShop").
    /// Products with TradeCategory "SpecialShop" are always considered learnable.
    /// </summary>
    public string TradeCategory { get; set; } = "";
    /// <summary>
    /// Reward table ID triggered when this SpecialShop product is purchased
    /// (e.g. "RS_S13_SHIP", "RS_S8_EGG"). Null/empty for items with no special
    /// purchase reward. Used to determine whether the item should be added to
    /// KnownTech when redeemed — ship, egg, frigate, firework, and weapon rewards
    /// are non-technology and should NOT appear in KnownTech.
    /// Populated from the MBIN field GiveRewardOnSpecialPurchase.
    /// </summary>
    public string GiveRewardOnSpecialPurchase { get; set; } = "";
    /// <summary>
    /// Whether this base building product can be picked up after placement.
    /// From BaseBuildingData in the game's basebuildingobjectstable.
    /// (Filtering for inventory add dialogs).
    /// </summary>
    public bool CanPickUp { get; set; }
    /// <summary>
    /// Whether this base building is a temporary structure.
    /// Temporary buildings are always considered pickupable.
    /// From BaseBuildingData in the game's basebuildingobjectstable.
    /// </summary>
    public bool IsTemporary { get; set; }
    /// <summary>
    /// For "Technology Module" product items, the technology ID this module deploys into
    /// when installed (e.g. "UP_SENGUN"). Used to look up TechnologyCategory for filtering.
    /// </summary>
    public string DeploysInto { get; set; } = "";

    /// <summary>
    /// The NMS game table this item was extracted from: "Product", "Technology", or "Substance".
    /// Used to determine which account-level Seen* array the item belongs in
    /// (SeenProducts, SeenTechnologies, or SeenSubstances), matching the game's own
    /// classification independent of which JSON file the Categorizer placed the item in.
    /// </summary>
    public string SourceTable { get; set; } = "";

    /// <summary>
    /// Maps the Quality field (Normal, Rare, Epic, Legendary, Illegal, Sentinel)
    /// to a class letter (C, B, A, S, X, ?). Returns null if no mapping.
    /// </summary>
    public string? QualityToClass() => Quality switch
    {
        "Normal" => "C",
        "Rare" => "B",
        "Epic" => "A",
        "Legendary" => "S",
        "Illegal" => "X",
        "Sentinel" => "?",
        _ => null
    };

    /// <summary>
    /// Maps the Rarity field to a class letter, using the same mapping as Quality.
    /// Used as a fallback for Upgrade items where Quality is "None" but Rarity
    /// carries the correct class information.
    /// </summary>
    public string? RarityToClass() => Rarity switch
    {
        "Normal" => "C",
        "Rare" => "B",
        "Epic" => "A",
        "Legendary" => "S",
        "Illegal" => "X",
        "Sentinel" => "?",
        _ => null
    };

    /// <summary>
    /// Returns a display string for the item picker.  Includes the resolved class
    /// label (e.g. "[B Class]") between the name and ID for items that have a
    /// quality or rarity classification.
    /// Format: "Name [X Class] (Id)" or "Name (Id)" when no class applies.
    /// </summary>
    public override string ToString()
    {
        string? cls = QualityToClass() ?? RarityToClass();
        if (cls != null)
        {
            string label = cls switch
            {
                "C" => "C Class",
                "B" => "B Class",
                "A" => "A Class",
                "S" => "S Class",
                "X" => "X Class",
                "?" => "Sentinel",
                _ => cls
            };
            return $"{Name} [{label}] ({Id})";
        }
        return $"{Name} ({Id})";
    }

    // Per inventory:
    //   Suit        -> [Suit]
    //   Ship        -> [Ship, AllShips, AllShipsExceptAlien]
    //   AlienShip   -> [AlienShip, AllShips]
    //   RobotShip   -> [RobotShip, Ship, AllShips, AllShipsExceptAlien]
    //   Corvette    -> [Corvette, Ship, AllShips, AllShipsExceptAlien]
    //   Weapon      -> [Weapon]
    //   Freighter   -> [Freighter]
    //   Exocraft    -> [Exocraft, AllVehicles]
    //   Colossus    -> [Colossus, Exocraft, AllVehicles]
    //   Mech        -> [Mech, AllVehicles]
    //   Submarine   -> [Submarine, AllVehicles]

    /// <summary>
    /// Checks if this technology item is valid for the specified inventory owner.
    /// Each inventory type defines which TechnologyCategory values it accepts.
    /// This method inverts that - given this item's TechnologyCategory,
    /// it returns true if the specified owner would accept it.
    /// 
    /// The editor sets owner type dynamically based on the selected entity:
    ///   Ships: "Ship", "AlienShip", "RobotShip", "Corvette"
    ///   Vehicles: "Exocraft", "Colossus", "Submarine", "Mech"
    ///   Others: "Suit", "Weapon", "Freighter"
    /// </summary>
    /// <param name="owner">The inventory owner type.</param>
    /// <returns>True if this tech can be installed in the given inventory.</returns>
    public bool IsValidForOwner(string owner)
    {
        if (string.IsNullOrEmpty(TechnologyCategory) || TechnologyCategory == "None")
            return true; // No category restriction

        // Maintenance tech can go anywhere.
        if (TechnologyCategory == "Maintenance")
            return true;

        return owner switch
        {
            // Suit accepts only Suit tech
            "Suit" => TechnologyCategory == "Suit",

            // Ship (normal: Fighter, Hauler, Explorer, Shuttle, Exotic, Solar, etc.)
            "Ship" or "Starship" => TechnologyCategory is "Ship" or "AllShips" or "AllShipsExceptAlien",

            // Weapon accepts only Weapon tech
            "Weapon" or "Multitool" => TechnologyCategory == "Weapon",

            // Freighter accepts only Freighter tech
            "Freighter" => TechnologyCategory == "Freighter",

            // Vehicle (legacy fallback: accepts all vehicle tech when specific type unknown)
            "Vehicle" => TechnologyCategory is "Exocraft" or "Colossus"
                or "Mech" or "Submarine" or "AllVehicles",

            // Specific ship subtypes (set dynamically per selected ship)
            "AlienShip" => TechnologyCategory is "AlienShip" or "AllShips",
            "RobotShip" => TechnologyCategory is "RobotShip" or "Ship" or "AllShips" or "AllShipsExceptAlien",
            "Corvette" => TechnologyCategory is "Corvette" or "Ship" or "AllShips" or "AllShipsExceptAlien",

            // Specific vehicle subtypes (set dynamically per selected vehicle)
            "Exocraft" => TechnologyCategory is "Exocraft" or "AllVehicles",
            "Colossus" => TechnologyCategory is "Colossus" or "Exocraft" or "AllVehicles",
            "Mech" => TechnologyCategory is "Mech" or "AllVehicles",
            "Submarine" => TechnologyCategory is "Submarine" or "AllVehicles",

            _ => true
        };
    }
}
