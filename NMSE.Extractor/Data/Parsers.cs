using NMSE.Extractor.Config;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NMSE.Extractor.Data;

/// <summary>
/// All MXML parsers. Each returns List<Dictionary<string, object?>>.
/// </summary>
public static class Parsers
{
    // Helper to get a value or null
    private static object? Val(object? v) => v is string s && s == "" ? null : v;
    private static string? NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

    /// <summary>
    /// Attempts to find a localisation key for a category enum value.
    /// Tries patterns like UI_TECHTYPE_{VALUE}, UI_SUBCAT_{VALUE}, UI_CAT_{VALUE}.
    /// Returns the loc key if found in localisation data, or null otherwise.
    /// </summary>
    private static string? TryGetCategoryLocKey(Dictionary<string, string> localisation, string? categoryValue)
    {
        if (string.IsNullOrEmpty(categoryValue) || categoryValue == "None") return null;
        string upper = categoryValue.ToUpperInvariant();
        string[] prefixes = ["UI_TECHTYPE_", "UI_SUBCAT_", "UI_CAT_"];
        foreach (var prefix in prefixes)
        {
            string candidate = $"{prefix}{upper}";
            if (localisation.ContainsKey(candidate)) return candidate;
        }
        return null;
    }

    /// <summary>
    /// Build a product-derived dictionary in the canonical key order.
    /// Takes a ProductLookup row and optional extra fields to insert at specific positions.
    /// includeNameLower: whether to include NameLower after Name (Trade/Fish yes, Cooking no)
    /// includeCookingValue: whether to include CookingValue in the base section (Fish yes, Trade/Cooking no)
    /// extraAfterHint: fields to insert after Hint (e.g. fish-specific Quality, FishSize, etc.)
    /// extraAfterCanSend: fields to append after CanSendToOtherPlayers (e.g. cooking NameLower, CookingValue, etc.)
    /// </summary>
    private static Dictionary<string, object?> BuildCanonicalProductDict(
        Dictionary<string, object?> product,
        string itemId,
        bool includeNameLower = true,
        bool includeCookingValue = true,
        bool includeCdnUrl = false,
        IReadOnlyList<KeyValuePair<string, object?>>? extraAfterHint = null,
        IReadOnlyList<KeyValuePair<string, object?>>? extraAfterCanSend = null)
    {
        var d = new Dictionary<string, object?>();
        d["Id"] = product.GetValueOrDefault("Id") ?? itemId;
        d["Icon"] = $"{itemId}.png";
        d["IconPath"] = product.GetValueOrDefault("IconPath");
        d["Name"] = product.GetValueOrDefault("Name");
        d["Name_LocStr"] = product.GetValueOrDefault("Name_LocStr");
        if (includeNameLower)
        {
            d["NameLower"] = product.GetValueOrDefault("NameLower");
            d["NameLower_LocStr"] = product.GetValueOrDefault("NameLower_LocStr");
        }
        d["Group"] = product.GetValueOrDefault("Group");
        d["Subtitle_LocStr"] = product.GetValueOrDefault("Subtitle_LocStr");
        d["Description"] = product.GetValueOrDefault("Description");
        d["Description_LocStr"] = product.GetValueOrDefault("Description_LocStr");
        d["AltDescription"] = product.GetValueOrDefault("AltDescription");
        d["Hint"] = product.GetValueOrDefault("Hint");
        if (extraAfterHint != null)
            foreach (var kv in extraAfterHint)
                d[kv.Key] = kv.Value;
        d["BaseValueUnits"] = product.GetValueOrDefault("BaseValueUnits");
        d["CurrencyType"] = "Credits";
        d["Level"] = product.GetValueOrDefault("Level");
        d["ChargeValue"] = product.GetValueOrDefault("ChargeValue");
        d["MaxStackSize"] = product.GetValueOrDefault("MaxStackSize");
        d["DefaultCraftAmount"] = product.GetValueOrDefault("DefaultCraftAmount");
        d["CraftAmountStepSize"] = product.GetValueOrDefault("CraftAmountStepSize");
        d["CraftAmountMultiplier"] = product.GetValueOrDefault("CraftAmountMultiplier");
        d["Colour"] = product.GetValueOrDefault("Colour");
        d["WorldColour"] = product.GetValueOrDefault("WorldColour");
        if (includeCdnUrl)
            d["CdnUrl"] = "";
        if (includeCookingValue)
            d["CookingValue"] = product.GetValueOrDefault("CookingValue");
        d["Usages"] = product.GetValueOrDefault("Usages");
        d["BlueprintCost"] = product.GetValueOrDefault("BlueprintCost");
        d["BlueprintCostType"] = "None";
        d["BlueprintSource"] = 0;
        d["RequiredItems"] = product.GetValueOrDefault("RequiredItems");
        d["StatBonuses"] = new List<object>();
        d["ConsumableRewardTexts"] = new List<object>();
        d["HeroIconPath"] = product.GetValueOrDefault("HeroIconPath");
        d["PriceModifiers"] = product.GetValueOrDefault("PriceModifiers");
        d["SpecificChargeOnly"] = product.GetValueOrDefault("SpecificChargeOnly");
        d["NormalisedValueOnWorld"] = product.GetValueOrDefault("NormalisedValueOnWorld");
        d["NormalisedValueOffWorld"] = product.GetValueOrDefault("NormalisedValueOffWorld");
        d["EconomyInfluenceMultiplier"] = product.GetValueOrDefault("EconomyInfluenceMultiplier");
        d["Rarity"] = product.GetValueOrDefault("Rarity");
        d["Legality"] = product.GetValueOrDefault("Legality");
        d["TradeCategory"] = product.GetValueOrDefault("TradeCategory");
        d["ProductCategory"] = product.GetValueOrDefault("ProductCategory");
        d["SubstanceCategory"] = product.GetValueOrDefault("SubstanceCategory");
        d["WikiCategory"] = product.GetValueOrDefault("WikiCategory");
        d["WikiEnabled"] = product.GetValueOrDefault("WikiEnabled");
        d["FossilCategory"] = product.GetValueOrDefault("FossilCategory");
        d["CorvettePartCategory"] = product.GetValueOrDefault("CorvettePartCategory");
        d["CorvetteRewardFrequency"] = product.GetValueOrDefault("CorvetteRewardFrequency");
        d["Consumable"] = product.GetValueOrDefault("Consumable");
        d["CookingIngredient"] = product.GetValueOrDefault("CookingIngredient");
        d["GoodForSelling"] = product.GetValueOrDefault("GoodForSelling");
        d["EggModifierIngredient"] = product.GetValueOrDefault("EggModifierIngredient");
        d["DeploysInto"] = product.GetValueOrDefault("DeploysInto");
        d["BuildableShipTechID"] = product.GetValueOrDefault("BuildableShipTechID");
        d["GroupID"] = product.GetValueOrDefault("GroupID");
        d["PinObjective"] = product.GetValueOrDefault("PinObjective");
        d["PinObjectiveTip"] = product.GetValueOrDefault("PinObjectiveTip");
        d["PinObjectiveMessage"] = product.GetValueOrDefault("PinObjectiveMessage");
        d["PinObjectiveScannableType"] = product.GetValueOrDefault("PinObjectiveScannableType");
        d["PinObjectiveEasyToRefine"] = product.GetValueOrDefault("PinObjectiveEasyToRefine");
        d["NeverPinnable"] = product.GetValueOrDefault("NeverPinnable");
        d["GiveRewardOnSpecialPurchase"] = product.GetValueOrDefault("GiveRewardOnSpecialPurchase");
        d["FoodBonusStat"] = product.GetValueOrDefault("FoodBonusStat");
        d["FoodBonusStatAmount"] = product.GetValueOrDefault("FoodBonusStatAmount");
        d["IsTechbox"] = product.GetValueOrDefault("IsTechbox");
        d["CanSendToOtherPlayers"] = product.GetValueOrDefault("CanSendToOtherPlayers");
        if (extraAfterCanSend != null)
            foreach (var kv in extraAfterCanSend)
                d[kv.Key] = kv.Value;
        // Inherit SourceTable from the source product dictionary if present;
        // callers that use BuildCanonicalProductDict always parse from the product table.
        d["SourceTable"] = product.GetValueOrDefault("SourceTable") ?? "Product";
        return d;
    }

    // Products
    public static List<Dictionary<string, object?>> ParseProducts(string mxmlPath, bool includeSubtitleKey = false)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var products = new List<Dictionary<string, object?>>();
        int counter = 1;

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return products;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string fallbackId = $"PRODUCT_{counter}";
                string nameKey = MxmlParser.GetPropertyValue(elem, "Name");
                string subtitleKey = MxmlParser.GetPropertyValue(elem, "Subtitle");
                string descKey = MxmlParser.GetPropertyValue(elem, "Description");

                var row = ProductLookup.ParseProductElement(localisation, elem,
                    includeRequirements: true, includeRawKeys: includeSubtitleKey,
                    requireIcon: true, fallbackId: fallbackId,
                    nameDefault: nameKey, groupDefault: subtitleKey, descriptionDefault: descKey);
                if (row == null) continue;

                string pid = row["Id"]?.ToString() ?? fallbackId;

                // Build product in reference property order.
                // New properties must be inserted at the correct position.
                var product = new Dictionary<string, object?>
                {
                    ["Id"] = row["Id"],
                    ["Icon"] = $"{pid}.png",
                    ["IconPath"] = row["IconPath"],
                    ["Name"] = row["Name"],
                    ["Name_LocStr"] = row.GetValueOrDefault("Name_LocStr"),
                    ["Group"] = row["Group"],
                    ["Subtitle_LocStr"] = row.GetValueOrDefault("Subtitle_LocStr"),
                    ["Description"] = row["Description"],
                    ["Description_LocStr"] = row.GetValueOrDefault("Description_LocStr"),
                    ["AltDescription"] = row.GetValueOrDefault("AltDescription"),
                    ["Hint"] = row.GetValueOrDefault("Hint"),
                    ["BaseValueUnits"] = row["BaseValueUnits"],
                    ["CurrencyType"] = "Credits",
                    ["Level"] = row["Level"],
                    ["ChargeValue"] = row["ChargeValue"],
                    ["MaxStackSize"] = row["MaxStackSize"],
                    ["DefaultCraftAmount"] = row["DefaultCraftAmount"],
                    ["CraftAmountStepSize"] = row["CraftAmountStepSize"],
                    ["CraftAmountMultiplier"] = row["CraftAmountMultiplier"],
                    ["Colour"] = row["Colour"],
                    ["WorldColour"] = row.GetValueOrDefault("WorldColour"),
                    ["CdnUrl"] = "",
                    ["Usages"] = row["Usages"],
                    ["BlueprintCost"] = row["BlueprintCost"],
                    ["BlueprintCostType"] = "None",
                    ["BlueprintSource"] = 0,
                    ["RequiredItems"] = row["RequiredItems"],
                    ["StatBonuses"] = new List<object>(),
                    ["ConsumableRewardTexts"] = new List<object>(),
                    ["HeroIconPath"] = row.GetValueOrDefault("HeroIconPath"),
                    ["PriceModifiers"] = row["PriceModifiers"],
                    ["SpecificChargeOnly"] = row["SpecificChargeOnly"],
                    ["NormalisedValueOnWorld"] = row["NormalisedValueOnWorld"],
                    ["NormalisedValueOffWorld"] = row["NormalisedValueOffWorld"],
                    ["EconomyInfluenceMultiplier"] = row["EconomyInfluenceMultiplier"],
                    ["Rarity"] = row.GetValueOrDefault("Rarity"),
                    ["Legality"] = row.GetValueOrDefault("Legality"),
                    ["TradeCategory"] = row.GetValueOrDefault("TradeCategory"),
                    ["ProductCategory"] = row.GetValueOrDefault("ProductCategory"),
                    ["SubstanceCategory"] = row.GetValueOrDefault("SubstanceCategory"),
                    ["WikiCategory"] = row.GetValueOrDefault("WikiCategory"),
                    ["WikiEnabled"] = row["WikiEnabled"],
                    ["FossilCategory"] = row.GetValueOrDefault("FossilCategory"),
                    ["CorvettePartCategory"] = row.GetValueOrDefault("CorvettePartCategory"),
                    ["CorvetteRewardFrequency"] = row["CorvetteRewardFrequency"],
                    ["Consumable"] = row["Consumable"],
                    ["CookingIngredient"] = row["CookingIngredient"],
                    ["GoodForSelling"] = row["GoodForSelling"],
                    ["EggModifierIngredient"] = row["EggModifierIngredient"],
                    ["DeploysInto"] = row.GetValueOrDefault("DeploysInto"),
                    ["BuildableShipTechID"] = row.GetValueOrDefault("BuildableShipTechID"),
                    ["GroupID"] = row.GetValueOrDefault("GroupID"),
                    ["PinObjective"] = row.GetValueOrDefault("PinObjective"),
                    ["PinObjectiveTip"] = row.GetValueOrDefault("PinObjectiveTip"),
                    ["PinObjectiveMessage"] = row.GetValueOrDefault("PinObjectiveMessage"),
                    ["PinObjectiveScannableType"] = row.GetValueOrDefault("PinObjectiveScannableType"),
                    ["PinObjectiveEasyToRefine"] = row["PinObjectiveEasyToRefine"],
                    ["NeverPinnable"] = row["NeverPinnable"],
                    ["GiveRewardOnSpecialPurchase"] = row.GetValueOrDefault("GiveRewardOnSpecialPurchase"),
                    ["FoodBonusStat"] = row.GetValueOrDefault("FoodBonusStat"),
                    ["FoodBonusStatAmount"] = row["FoodBonusStatAmount"],
                    ["IsTechbox"] = row["IsTechbox"],
                    ["CanSendToOtherPlayers"] = row["CanSendToOtherPlayers"],
                    ["IsCraftable"] = row["IsCraftable"],
                    ["Procedural"] = row["Procedural"],
                };

                // Tag source game table for downstream classification (SeenProducts vs SeenTechnologies).
                // Products parsed from gcproducttable are Product-table items.
                product["SourceTable"] = "Product";
                if (includeSubtitleKey)
                {
                    if (row.TryGetValue("SubtitleKey", out var sk)) product["SubtitleKey"] = sk;
                }

                products.Add(product);
                counter++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Skipped product due to error: {ex.Message}");
            }
        }
        Console.WriteLine($"[OK] Parsed {products.Count} products");
        return products;
    }

    // RawMaterials
    public static List<Dictionary<string, object?>> ParseRawMaterials(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var materials = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return materials;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string itemId = MxmlParser.GetPropertyValue(elem, "ID");
                string nameKey = MxmlParser.GetPropertyValue(elem, "Name");
                string nameLowerKey = MxmlParser.GetPropertyValue(elem, "NameLower");
                string subtitleKey = MxmlParser.GetPropertyValue(elem, "Subtitle");
                string descKey = MxmlParser.GetPropertyValue(elem, "Description");

                if (string.IsNullOrEmpty(itemId)) continue;
                if (MxmlParser.UnresolvedLocalisationKeyCount(localisation, nameKey, subtitleKey, descKey) >= 2) continue;

                var iconProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                string iconFilename = iconProp != null ? MxmlParser.GetPropertyValue(iconProp, "Filename") : "";
                string iconPath = !string.IsNullOrEmpty(iconFilename) ? MxmlParser.NormalizeGameIconPath(iconFilename) : "";
                if (string.IsNullOrEmpty(iconPath)) continue;

                var colourElem = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Colour");

                bool cookingIngredient = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CookingIngredient", "false")) is true;
                bool goodForSelling = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "GoodForSelling", "false")) is true;
                bool easyToRefine = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "EasyToRefine", "false")) is true;
                bool eggModifier = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "EggModifierIngredient", "false")) is true;
                bool onlyFoundInPurpleSystems = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "OnlyFoundInPurpleSystems", "false")) is true;

                var usages = new List<string>();
                if (cookingIngredient) usages.Add("HasCookingProperties");
                if (goodForSelling) usages.Add("HasDevProperties");
                if (easyToRefine) usages.Add("HasRefinerProperties");
                if (eggModifier) usages.Add("IsEggIngredient");

                var costProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Cost");
                Dictionary<string, object>? priceModifiers = null;
                if (costProp != null)
                {
                    priceModifiers = new()
                    {
                        ["SpaceStationMarkup"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "SpaceStationMarkup", "0"))),
                        ["LowPriceMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "LowPriceMod", "0"))),
                        ["HighPriceMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "HighPriceMod", "0"))),
                        ["BuyBaseMarkup"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyBaseMarkup", "0"))),
                        ["BuyMarkupMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyMarkupMod", "0"))),
                    };
                }

                string symbolKey = MxmlParser.GetPropertyValue(elem, "Symbol");
                string symbol = !string.IsNullOrEmpty(symbolKey) ? MxmlParser.Translate(symbolKey, "") : "";

                materials.Add(new Dictionary<string, object?>
                {
                    ["Id"] = itemId,
                    ["Icon"] = $"{itemId}.png",
                    ["IconPath"] = iconPath,
                    ["Name"] = MxmlParser.Translate(nameKey, nameKey),
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["NameLower"] = !string.IsNullOrEmpty(nameLowerKey) ? MxmlParser.Translate(nameLowerKey, nameLowerKey) : null,
                    ["NameLower_LocStr"] = NullIfEmpty(nameLowerKey),
                    ["Group"] = MxmlParser.Translate(subtitleKey, subtitleKey),
                    ["Subtitle_LocStr"] = NullIfEmpty(subtitleKey),
                    ["Description"] = MxmlParser.Translate(descKey, descKey),
                    ["Description_LocStr"] = NullIfEmpty(descKey),
                    ["BaseValueUnits"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BaseValue", "0")),
                    ["CurrencyType"] = "Credits",
                    ["Colour"] = MxmlParser.ParseColour(colourElem),
                    ["CdnUrl"] = "",
                    ["Usages"] = usages,
                    ["BlueprintCost"] = 0,
                    ["BlueprintCostType"] = "None",
                    ["BlueprintSource"] = 0,
                    ["RequiredItems"] = new List<object>(),
                    ["StatBonuses"] = new List<object>(),
                    ["ConsumableRewardTexts"] = new List<object>(),
                    ["Category"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Category", "SubstanceCategory")),
                    ["Category_LocStr"] = TryGetCategoryLocKey(localisation, MxmlParser.GetNestedEnum(elem, "Category", "SubstanceCategory")),
                    ["Rarity"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Rarity", "Rarity")),
                    ["Legality"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Legality", "Legality")),
                    ["TradeCategory"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "TradeCategory", "TradeCategory")),
                    ["CookingIngredient"] = cookingIngredient,
                    ["GoodForSelling"] = goodForSelling,
                    ["EasyToRefine"] = easyToRefine,
                    ["EggModifierIngredient"] = eggModifier,
                    ["WikiEnabled"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "WikiEnabled", "false")),
                    ["OnlyFoundInPurpleSystems"] = onlyFoundInPurpleSystems,
                    ["ChargeValue"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ChargeValue", "0")),
                    ["MaxStackSize"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "StackMultiplier", "1")),
                    ["EconomyInfluenceMultiplier"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "EconomyInfluenceMultiplier", "0"))),
                    ["PriceModifiers"] = priceModifiers,
                    ["PinObjectiveScannableType"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "PinObjectiveScannableType", "ScanIconType")),
                    ["WikiMissionID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "WikiMissionID")),
                    ["Symbol"] = NullIfEmpty(symbol),
                    ["SourceTable"] = "Substance",
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped material: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {materials.Count} raw materials");
        return materials;
    }

    // Technology
    public static List<Dictionary<string, object?>> ParseTechnology(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var technologies = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return technologies;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string techId = MxmlParser.GetPropertyValue(elem, "ID", $"TECH_{technologies.Count + 1}");
                string nameKey = MxmlParser.GetPropertyValue(elem, "Name");
                string subtitleKey = MxmlParser.GetPropertyValue(elem, "Subtitle");
                string descKey = MxmlParser.GetPropertyValue(elem, "Description");

                if (MxmlParser.UnresolvedLocalisationKeyCount(localisation, nameKey, subtitleKey, descKey) >= 2) continue;

                var iconProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                string iconFilename = iconProp != null ? MxmlParser.GetPropertyValue(iconProp, "Filename") : "";
                string iconPath = !string.IsNullOrEmpty(iconFilename) ? MxmlParser.NormalizeGameIconPath(iconFilename) : "";
                if (string.IsNullOrEmpty(iconPath)) continue;

                // StatBonuses
                var statBonuses = new List<Dictionary<string, object>>();
                var statProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "StatBonuses");
                if (statProp != null)
                {
                    foreach (var statElem in statProp.Elements("Property"))
                    {
                        var statTypeProp = statElem.Descendants("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "StatsType");
                        string statType = statTypeProp?.Attribute("value")?.Value ?? "";
                        string bonus = MxmlParser.GetPropertyValue(statElem, "Bonus", "0");
                        if (!string.IsNullOrEmpty(statType))
                        {
                            statBonuses.Add(new()
                            {
                                ["Name"] = MxmlParser.FormatStatTypeName(statType, "Suit_"),
                                ["LocaleKeyTemplate"] = "enabled",
                                ["Image"] = statType.Contains('_') ? statType.ToLower().Split('_').Last() : "enabled",
                                ["Value"] = ((int)double.Parse(bonus, System.Globalization.CultureInfo.InvariantCulture)).ToString(System.Globalization.CultureInfo.InvariantCulture)
                            });
                        }
                    }
                }

                // Requirements
                var requiredItems = new List<Dictionary<string, object>>();
                var reqProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Requirements");
                if (reqProp != null)
                {
                    foreach (var reqElem in reqProp.Elements("Property"))
                    {
                        string reqId = MxmlParser.GetPropertyValue(reqElem, "ID");
                        if (!string.IsNullOrEmpty(reqId))
                            requiredItems.Add(new() { ["Id"] = reqId, ["Quantity"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(reqElem, "Amount", "1")) });
                    }
                }

                bool isChargeable = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Chargeable", "false")) is true;
                var usages = new List<string>();
                if (isChargeable) usages.Add("HasChargedBy");
                usages.Add("HasDevProperties");

                // ChargeBy array reads value attr from Property[@name="ChargeBy"] children
                var chargeByList = new List<string>();
                var chargeByProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "ChargeBy");
                if (chargeByProp != null)
                {
                    foreach (var child in chargeByProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "ChargeBy"))
                    {
                        string val = child.Attribute("value")?.Value ?? "";
                        if (!string.IsNullOrEmpty(val))
                            chargeByList.Add(val);
                    }
                }

                // PriceModifiers from Cost element
                var costProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Cost");
                Dictionary<string, object>? priceModifiers = null;
                if (costProp != null)
                {
                    priceModifiers = new()
                    {
                        ["SpaceStationMarkup"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "SpaceStationMarkup", "0"))),
                        ["LowPriceMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "LowPriceMod", "0"))),
                        ["HighPriceMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "HighPriceMod", "0"))),
                        ["BuyBaseMarkup"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyBaseMarkup", "0"))),
                        ["BuyMarkupMod"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(costProp, "BuyMarkupMod", "0"))),
                    };
                }

                // Translated optional fields
                string nameLowerKey = MxmlParser.GetPropertyValue(elem, "NameLower");
                string hintStartKey = MxmlParser.GetPropertyValue(elem, "HintStart");
                string hintEndKey = MxmlParser.GetPropertyValue(elem, "HintEnd");
                string damagedDescKey = MxmlParser.GetPropertyValue(elem, "DamagedDescription");

                // DispensingRace
                string dispensingRaceRaw = MxmlParser.GetNestedEnum(elem, "DispensingRace", "AlienRace");
                string? dispensingRace = NullIfEmpty(dispensingRaceRaw);

                technologies.Add(new Dictionary<string, object?>
                {
                    ["Id"] = techId,
                    ["Icon"] = $"{techId}.png",
                    ["IconPath"] = iconPath,
                    ["Name"] = MxmlParser.Translate(nameKey, techId),
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["NameLower"] = NullIfEmpty(MxmlParser.Translate(nameLowerKey, "")),
                    ["NameLower_LocStr"] = NullIfEmpty(nameLowerKey),
                    ["Group"] = MxmlParser.Translate(subtitleKey, ""),
                    ["Subtitle_LocStr"] = NullIfEmpty(subtitleKey),
                    ["Description"] = MxmlParser.Translate(descKey, ""),
                    ["Description_LocStr"] = NullIfEmpty(descKey),
                    ["HintStart"] = NullIfEmpty(MxmlParser.Translate(hintStartKey, "")),
                    ["HintEnd"] = NullIfEmpty(MxmlParser.Translate(hintEndKey, "")),
                    ["DamagedDescription"] = NullIfEmpty(MxmlParser.Translate(damagedDescKey, "")),
                    ["BaseValueUnits"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BaseValue", "1")),
                    ["CurrencyType"] = "None",
                    ["Level"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Level", "0")),
                    ["Value"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Value", "0")),
                    ["Colour"] = MxmlParser.ParseColour(elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Colour")),
                    ["Usages"] = usages,
                    ["BlueprintCost"] = 1,
                    ["BlueprintCostType"] = "Nanites",
                    ["BlueprintSource"] = 0,
                    ["RequiredItems"] = requiredItems,
                    ["StatBonuses"] = statBonuses,
                    ["ConsumableRewardTexts"] = new List<object>(),
                    ["Category"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Category", "TechnologyCategory")),
                    ["Category_LocStr"] = TryGetCategoryLocKey(localisation, MxmlParser.GetNestedEnum(elem, "Category", "TechnologyCategory")),
                    ["Rarity"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "Rarity", "TechnologyRarity")),
                    ["Teach"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "Teach")),
                    ["Chargeable"] = isChargeable,
                    ["ChargeAmount"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ChargeAmount", "0")),
                    ["ChargeType"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "ChargeType", "SubstanceCategory")),
                    ["ChargeBy"] = chargeByList,
                    ["ChargeMultiplier"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ChargeMultiplier", "1"))),
                    ["BuildFullyCharged"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BuildFullyCharged", "false")),
                    ["UsesAmmo"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "UsesAmmo", "false")),
                    ["AmmoId"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "AmmoId")),
                    ["PrimaryItem"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PrimaryItem")),
                    ["Upgrade"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Upgrade", "false")),
                    ["Core"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Core", "false")),
                    ["RepairTech"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "RepairTech", "false")),
                    ["Procedural"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Procedural", "false")),
                    ["BrokenSlotTech"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "BrokenSlotTech")),
                    ["ParentTechId"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "ParentTechId")),
                    ["RequiredTech"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "RequiredTech")),
                    ["RequiredLevel"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "RequiredLevel", "0")),
                    ["FocusLocator"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "FocusLocator")),
                    ["UpgradeColour"] = MxmlParser.ParseColour(elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "UpgradeColour")),
                    ["LinkColour"] = MxmlParser.ParseColour(elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "LinkColour")),
                    ["RewardGroup"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "RewardGroup")),
                    ["PriceModifiers"] = priceModifiers,
                    ["RequiredRank"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "RequiredRank", "0")),
                    ["DispensingRace"] = dispensingRace,
                    ["FragmentCost"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "FragmentCost", "0")),
                    ["TechShopRarity"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "TechShopRarity", "Rarity")),
                    ["WikiEnabled"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "WikiEnabled", "false")),
                    ["NeverPinnable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NeverPinnable", "false")),
                    ["IsTemplate"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsTemplate", "false")),
                    ["ExclusivePrimaryStat"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ExclusivePrimaryStat", "false")),
                    ["SourceTable"] = "Technology",
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped technology: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {technologies.Count} technologies");
        return technologies;
    }

    // Refinery / NutrientProcessor
    private static Dictionary<string, string>? _itemNamesCache;
    private static readonly object _itemNamesCacheLock = new();

    private static Dictionary<string, string> LoadItemNames(string mbinDir)
    {
        if (_itemNamesCache != null) return _itemNamesCache;

        lock (_itemNamesCacheLock)
        {
            if (_itemNamesCache != null) return _itemNamesCache;
            var cache = new Dictionary<string, string>();

            string productsPath = Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML");
            if (File.Exists(productsPath))
            {
                var root = MxmlParser.LoadXml(productsPath);
                var table = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
                if (table != null)
                    foreach (var item in table.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
                    {
                        string id = MxmlParser.GetPropertyValue(item, "ID");
                        string nameKey = MxmlParser.GetPropertyValue(item, "Name");
                        if (!string.IsNullOrEmpty(id)) cache[id] = MxmlParser.Translate(nameKey, id);
                    }
            }

            string substancesPath = Path.Combine(mbinDir, "nms_reality_gcsubstancetable.MXML");
            if (File.Exists(substancesPath))
            {
                var root = MxmlParser.LoadXml(substancesPath);
                var table = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
                if (table != null)
                    foreach (var item in table.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
                    {
                        string id = MxmlParser.GetPropertyValue(item, "ID");
                        string nameKey = MxmlParser.GetPropertyValue(item, "Name");
                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nameKey))
                            cache[id] = MxmlParser.Translate(nameKey, id);
                    }
            }
            _itemNamesCache = cache;
            return _itemNamesCache;
        }
    }

    public static List<Dictionary<string, object?>> ParseRefinery(string mxmlPath, bool onlyRefinery = true)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        MxmlParser.LoadLocalisation(Path.Combine(Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));
        var itemNames = LoadItemNames(mbinDir);
        var recipes = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return recipes;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string recipeId = MxmlParser.GetPropertyValue(elem, "Id", $"RECIPE_{recipes.Count + 1}");
                string nameKey = MxmlParser.GetPropertyValue(elem, "RecipeName");
                string recipeType = MxmlParser.GetPropertyValue(elem, "RecipeType");
                string opKey = !string.IsNullOrEmpty(nameKey) ? nameKey : recipeType;
                string operation = MxmlParser.Translate(opKey, opKey);

                bool isCooking = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Cooking", "false")) is true;
                if (onlyRefinery && isCooking) continue;
                if (!onlyRefinery && !isCooking) continue;

                string timeStr = MxmlParser.GetPropertyValue(elem, "TimeToMake", "0");

                var inputs = new List<Dictionary<string, object>>();
                var ingredientsProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Ingredients");
                if (ingredientsProp != null)
                    foreach (var ing in ingredientsProp.Elements("Property"))
                    {
                        string ingId = MxmlParser.GetPropertyValue(ing, "Id");
                        if (!string.IsNullOrEmpty(ingId))
                            inputs.Add(new() {
                                ["Id"] = ingId,
                                ["Name"] = itemNames.GetValueOrDefault(ingId, ingId),
                                ["Quantity"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(ing, "Amount", "1"))
                            });
                    }

                var output = new Dictionary<string, object>();
                var resultProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Result");
                if (resultProp != null)
                {
                    string outId = MxmlParser.GetPropertyValue(resultProp, "Id");
                    output["Id"] = outId;
                    output["Name"] = itemNames.GetValueOrDefault(outId, outId);
                    output["Quantity"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(resultProp, "Amount", "1"));
                }

                recipes.Add(new()
                {
                    ["Id"] = recipeId,
                    ["Inputs"] = inputs,
                    ["Output"] = output,
                    ["Time"] = double.Parse(timeStr, System.Globalization.CultureInfo.InvariantCulture).ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture),
                    ["Operation"] = operation,
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped recipe: {ex.Message}"); }
        }
        string type = onlyRefinery ? "refinery" : "cooking";
        Console.WriteLine($"[OK] Parsed {recipes.Count} {type} recipes");
        return recipes;
    }

    public static List<Dictionary<string, object?>> ParseNutrientProcessor(string mxmlPath)
        => ParseRefinery(mxmlPath, onlyRefinery: false);

    // Buildings
    public static List<Dictionary<string, object?>> ParseBuildings(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        MxmlParser.LoadLocalisation(Path.Combine(Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));

        // Load product icon lookup
        var productIcons = new Dictionary<string, string>();
        string productsPath = Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML");
        if (File.Exists(productsPath))
        {
            var prodRoot = MxmlParser.LoadXml(productsPath);
            var prodTable = prodRoot.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
            if (prodTable != null)
                foreach (var item in prodTable.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
                {
                    string pid = MxmlParser.GetPropertyValue(item, "ID");
                    var iprop = item.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                    string fn = iprop != null ? MxmlParser.GetPropertyValue(iprop, "Filename") : "";
                    if (!string.IsNullOrEmpty(fn)) productIcons[pid] = MxmlParser.NormalizeGameIconPath(fn);
                }
        }

        var buildings = new List<Dictionary<string, object?>>();
        var objectsProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Objects");
        if (objectsProp == null) return buildings;

        foreach (var elem in objectsProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Objects"))
        {
            try
            {
                string buildingId = MxmlParser.GetPropertyValue(elem, "ID");
                if (string.IsNullOrEmpty(buildingId)) continue;

                string name = MxmlParser.Translate(buildingId, buildingId.Replace('_', ' '));

                string group = "Base Building Part";
                var groupsProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Groups");
                if (groupsProp != null)
                {
                    var firstGroup = groupsProp.Elements("Property")
                        .Where(e => e.Attribute("name")?.Value == "Groups").FirstOrDefault();
                    if (firstGroup != null)
                    {
                        string g = MxmlParser.GetPropertyValue(firstGroup, "Group");
                        if (!string.IsNullOrEmpty(g)) group = MxmlParser.Translate(g, g.Replace('_', ' '));
                    }
                }

                string iconOverride = MxmlParser.GetPropertyValue(elem, "IconOverrideProductID");
                string iconPath = "";
                if (!string.IsNullOrEmpty(iconOverride) && productIcons.TryGetValue(iconOverride, out string? ov))
                    iconPath = ov;
                if (string.IsNullOrEmpty(iconPath)) continue;

                // BuildableOn flags
                bool buildableOnPlanet = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BuildableOnPlanetBase", "true")) is true;
                bool buildableOnSpace = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BuildableOnSpaceBase", "false")) is true;
                bool buildableOnFreighter = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BuildableOnFreighter", "false")) is true;

                // CanPickUp / IsTemporary flags (BaseBuildingData)
                bool canPickUp = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CanPickUp", "false")) is true;
                bool isTemporary = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsTemporary", "false")) is true;

                // Groups list
                var groupsList = new List<Dictionary<string, object?>>();
                if (groupsProp != null)
                    foreach (var grpElem in groupsProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Groups"))
                    {
                        string g = MxmlParser.GetPropertyValue(grpElem, "Group");
                        string sub = MxmlParser.GetPropertyValue(grpElem, "SubGroupName");
                        if (!string.IsNullOrEmpty(g))
                            groupsList.Add(new Dictionary<string, object?> { ["Group"] = g, ["SubGroupName"] = NullIfEmpty(sub) });
                    }

                // LinkGridData
                Dictionary<string, object?>? linkGridData = null;
                var linkElem = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "LinkGridData");
                if (linkElem != null)
                {
                    var networkElem = linkElem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Network");
                    string linkType = networkElem != null ? MxmlParser.GetNestedEnum(networkElem, "LinkNetworkType", "LinkNetworkType") : "";
                    object rate = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(linkElem, "Rate", "0"));
                    object storage = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(linkElem, "Storage", "0"));
                    bool hasValues = !string.IsNullOrEmpty(linkType)
                        || (rate is int ri && ri != 0) || (rate is double rd && rd != 0)
                        || (storage is int si && si != 0) || (storage is double sd && sd != 0);
                    if (hasValues)
                        linkGridData = new Dictionary<string, object?> { ["Network"] = NullIfEmpty(linkType), ["Rate"] = rate, ["Storage"] = storage };
                }

                buildings.Add(new Dictionary<string, object?>
                {
                    ["Id"] = buildingId,
                    ["Icon"] = $"{buildingId}.png",
                    ["IconPath"] = iconPath,
                    ["Name"] = name,
                    ["Group"] = group,
                    ["Description"] = "",
                    ["BaseValueUnits"] = 1,
                    ["CurrencyType"] = "None",
                    ["Colour"] = "CCCCCC",
                    ["CdnUrl"] = "",
                    ["Usages"] = new List<string> { "HasDevProperties" },
                    ["BlueprintCost"] = 1,
                    ["BlueprintCostType"] = "None",
                    ["BlueprintSource"] = 0,
                    ["RequiredItems"] = new List<object>(),
                    ["StatBonuses"] = new List<object>(),
                    ["ConsumableRewardTexts"] = new List<object>(),
                    ["IconOverrideProductID"] = NullIfEmpty(iconOverride),
                    ["BuildableOnPlanetBase"] = buildableOnPlanet,
                    ["BuildableOnSpaceBase"] = buildableOnSpace,
                    ["BuildableOnFreighter"] = buildableOnFreighter,
                    ["CanPickUp"] = canPickUp,
                    ["IsTemporary"] = isTemporary,
                    ["Groups"] = groupsList.Count > 0 ? groupsList : null,
                    ["LinkGridData"] = linkGridData,
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped building: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {buildings.Count} buildings");
        return buildings;
    }

    // Cooking

    private static string MapEffectCategory(string rewardId)
    {
        if (string.IsNullOrEmpty(rewardId)) return "Unknown";
        var rid = rewardId.ToUpperInvariant();
        if (rid.StartsWith("DE_FOOD_JETPACK", StringComparison.Ordinal)) return "Jetpack";
        if (rid.StartsWith("DE_FOOD_HAZ", StringComparison.Ordinal)) return "Hazard Protection";
        if (rid.StartsWith("DE_FOOD_ENERGY", StringComparison.Ordinal)) return "Life Support";
        if (rid.StartsWith("DE_FOOD_HEALTH", StringComparison.Ordinal)) return "Health";
        if (rid.StartsWith("DE_FOOD_STAMINA", StringComparison.Ordinal)) return "Stamina";
        return "Unknown";
    }

    private static readonly HashSet<string> RewardStatMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "amount", "value", "chance", "duration", "time", "bonus", "mult", "min", "max"
    };

    private static readonly Dictionary<string, string> RewardStatKeyMap = new()
    {
        ["GcRewardEnergy.Amount"] = "LifeSupportRechargeAmount",
        ["GcRewardRefreshHazProt.Amount"] = "HazardProtectionRechargeAmount",
        ["GcRewardStamina.Amount"] = "StaminaRechargeAmount",
        ["GcRewardHealth.Amount"] = "HealthRechargeAmount",
    };

    private static Dictionary<string, Dictionary<string, object?>?> LoadRewardEffectLookup(string mbinDir)
    {
        var lookup = new Dictionary<string, Dictionary<string, object?>?>();
        string[] candidates = { "rewardtable.MXML", "nms_reality_gcrewardtable.MXML" };
        string? rewardPath = null;
        foreach (var c in candidates)
        {
            string p = Path.Combine(mbinDir, c);
            if (File.Exists(p)) { rewardPath = p; break; }
        }
        if (rewardPath == null) return lookup;

        var root = MxmlParser.LoadXml(rewardPath);
        string[] tableNames = { "GenericTable", "DestructionTable", "Table" };
        var tableBlocks = tableNames.SelectMany(n =>
            root.Descendants("Property").Where(e => e.Attribute("name")?.Value == n));

        foreach (var tableProp in tableBlocks)
        {
            foreach (var rewardEntry in tableProp.Elements("Property"))
            {
                string rewardId = MxmlParser.GetPropertyValue(rewardEntry, "Id");
                if (string.IsNullOrEmpty(rewardId))
                    rewardId = MxmlParser.GetPropertyValue(rewardEntry, "ID");
                if (string.IsNullOrEmpty(rewardId)) continue;

                var stats = ExtractRewardEffectStats(rewardEntry);
                lookup[rewardId] = stats.Count > 0 ? stats : null;
            }
        }
        return lookup;
    }

    private static Dictionary<string, object?> ExtractRewardEffectStats(XElement entry)
    {
        var stats = new Dictionary<string, object?>();
        var usedKeys = new HashSet<string>();
        FlattenPropertyLeaves(entry, "", (path, rawValue) =>
        {
            if (string.IsNullOrEmpty(path)) return;
            bool hasMarker = false;
            foreach (var marker in RewardStatMarkers)
            {
                if (path.Contains(marker, StringComparison.OrdinalIgnoreCase))
                { hasMarker = true; break; }
            }
            if (!hasMarker) return;

            var parsed = MxmlParser.ParseValue(rawValue);
            if (parsed is not (int or double or bool)) return;

            string shortKey = path.Contains(".Reward.")
                ? path.Split(".Reward.", 2)[1]
                : path.Split('.').Last();

            if (RewardStatKeyMap.TryGetValue(shortKey, out var mappedKey))
                shortKey = mappedKey;

            if (usedKeys.Contains(shortKey))
            {
                int i = 2;
                while (usedKeys.Contains($"{shortKey}_{i}")) i++;
                shortKey = $"{shortKey}_{i}";
            }
            usedKeys.Add(shortKey);

            // Ensure numeric stats are doubles
            if (parsed is int intVal) parsed = (double)intVal;
            stats[shortKey] = parsed;
        });
        return stats;
    }

    private static void FlattenPropertyLeaves(XElement elem, string prefix, Action<string, string> onLeaf)
    {
        string name = elem.Attribute("name")?.Value ?? "";
        string value = elem.Attribute("value")?.Value ?? "";
        string current = !string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(name)
            ? $"{prefix}.{name}" : (!string.IsNullOrEmpty(name) ? name : prefix);

        var children = elem.Elements("Property").ToList();
        if (children.Count > 0)
        {
            foreach (var child in children)
                FlattenPropertyLeaves(child, current, onLeaf);
        }
        else if (!string.IsNullOrEmpty(name))
        {
            onLeaf(current, value);
        }
    }

    public static List<Dictionary<string, object?>> ParseCooking(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));
        string productsPath = Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML");
        var productsLookup = ProductLookup.LoadProductLookup(localisation, productsPath, includeRequirements: true);
        var rewardLookup = LoadRewardEffectLookup(mbinDir);

        var items = new List<Dictionary<string, object?>>();
        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return items;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string itemId = MxmlParser.GetPropertyValue(elem, "ID");
                if (string.IsNullOrEmpty(itemId)) continue;
                if (!productsLookup.TryGetValue(itemId, out var product)) continue;
                if (string.IsNullOrEmpty(product["IconPath"]?.ToString())) continue;

                string rewardId = MxmlParser.GetPropertyValue(elem, "RewardID");
                rewardLookup.TryGetValue(rewardId, out var rewardStats);

                // Cooking-specific fields go after CanSendToOtherPlayers (and after Slug once applied).
                // NameLower is NOT in the base section for cooking items; it's a cooking-specific trailing field.
                var cookingExtra = new List<KeyValuePair<string, object?>>
                {
                    new("NameLower", product.GetValueOrDefault("NameLower")),
                    new("CookingValue", product.GetValueOrDefault("CookingValue")),
                    new("RewardID", NullIfEmpty(rewardId)),
                    new("EffectCategory", MapEffectCategory(rewardId)),
                    new("RewardEffectStats", rewardStats),
                };

                var cooking = BuildCanonicalProductDict(product, itemId,
                    includeNameLower: false,
                    includeCookingValue: false,
                    includeCdnUrl: true,
                    extraAfterCanSend: cookingExtra);
                items.Add(cooking);
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped cooking item: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {items.Count} cooking items");
        return items;
    }

    // Fish

    private static readonly Dictionary<string, string> FishSizeSuffix = new()
    {
        ["Small"] = "S", ["Medium"] = "M", ["Large"] = "L", ["ExtraLarge"] = "XL",
    };

    private static readonly Dictionary<string, string> FishRaritySuffix = new()
    {
        ["Common"] = "COM", ["Rare"] = "RARE", ["Epic"] = "EPIC", ["Legendary"] = "EPIC",
    };

    private static readonly Dictionary<string, string> BiomeToSuffix = new()
    {
        ["All"] = "ALL", ["Lush"] = "LUSH", ["Scorched"] = "HOT", ["Lava"] = "HOT",
        ["Frozen"] = "COLD", ["Radioactive"] = "RAD", ["Toxic"] = "TOX", ["Swamp"] = "TOX",
        ["Barren"] = "DUST", ["Dead"] = "DUST", ["Weird"] = "ODD", ["Red"] = "ODD",
        ["Green"] = "ODD", ["Blue"] = "ODD", ["Test"] = "ODD",
        ["Waterworld"] = "DEEP", ["GasGiant"] = "GAS",
    };

    private static string BuildFishFallbackDescription(string quality, string fishSize, List<string> biomes)
    {
        string sizeSuffix = FishSizeSuffix.GetValueOrDefault(fishSize, "M");
        FishRaritySuffix.TryGetValue(quality, out string? raritySuffix);

        string sizeWord = MxmlParser.Translate($"UI_FISH_SIZE_{sizeSuffix}", "");
        string rarityDesc = !string.IsNullOrEmpty(raritySuffix)
            ? MxmlParser.Translate($"UI_FISH_RARITY_{raritySuffix}_{sizeSuffix}_DESC", "")
            : "";

        string biomeDesc = "";
        foreach (var biome in biomes)
        {
            if (BiomeToSuffix.TryGetValue(biome, out var suffix))
            {
                string candidate = MxmlParser.Translate($"UI_FISH_BIOME_{suffix}_DESC", "");
                if (!string.IsNullOrEmpty(candidate))
                {
                    biomeDesc = candidate;
                    break;
                }
            }
        }

        // Replace %SIZE% token
        if (!string.IsNullOrEmpty(biomeDesc))
        {
            biomeDesc = string.IsNullOrEmpty(sizeWord)
                ? biomeDesc.Replace("%SIZE%", "").Trim()
                : System.Text.RegularExpressions.Regex.Replace(
                    biomeDesc, @"%SIZE%(?=[A-Za-z])", sizeWord + " ")
                    .Replace("%SIZE%", sizeWord).Trim();
        }

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(biomeDesc)) parts.Add(biomeDesc);
        if (!string.IsNullOrEmpty(rarityDesc)) parts.Add(rarityDesc);
        if (quality == "Legendary")
        {
            string legend = MxmlParser.Translate("UI_FISH_LEGEND_EXTRA", "");
            if (!string.IsNullOrEmpty(legend)) parts.Add(legend);
        }
        return string.Join("\n\n", parts).Trim();
    }

    public static List<Dictionary<string, object?>> ParseFish(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));
        string productsPath = Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML");
        var productsLookup = ProductLookup.LoadProductLookup(localisation, productsPath, includeRequirements: false);

        var fishList = new List<Dictionary<string, object?>>();
        var fishProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Fish");
        if (fishProp == null) return fishList;

        foreach (var elem in fishProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Fish"))
        {
            try
            {
                string productId = MxmlParser.GetPropertyValue(elem, "ProductID");
                if (string.IsNullOrEmpty(productId)) continue;
                if (!productsLookup.TryGetValue(productId, out var product)) continue;
                if (string.IsNullOrEmpty(product["IconPath"]?.ToString())) continue;

                string quality = MxmlParser.GetNestedEnum(elem, "Quality", "ItemQuality");
                string fishSize = MxmlParser.GetNestedEnum(elem, "Size", "FishSize");
                string fishingTime = MxmlParser.GetNestedEnum(elem, "Time", "FishingTime");

                var biomes = new List<string>();
                var biomeProp = elem.Elements("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Biome");
                if (biomeProp != null)
                    foreach (var b in biomeProp.Elements("Property"))
                    {
                        string bname = b.Attribute("name")?.Value ?? "";
                        if (MxmlParser.ParseValue(b.Attribute("value")?.Value ?? "false") is true)
                            biomes.Add(bname);
                    }

                var fishAfterHint = new List<KeyValuePair<string, object?>>
                {
                    new("Quality", quality),
                    new("FishSize", fishSize),
                    new("FishingTime", fishingTime),
                    new("Biomes", biomes),
                    new("NeedsStorm", MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NeedsStorm", "false"))),
                    new("RequiresMissionActive", NullIfEmpty(MxmlParser.GetPropertyValue(elem, "RequiresMissionActive"))),
                    new("MissionSeed", NullIfEmpty(MxmlParser.GetPropertyValue(elem, "MissionSeed"))),
                    new("MissionMustAlsoBeSelected", MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "MissionMustAlsoBeSelected", "false"))),
                    new("MissionCatchChanceOverride", MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "MissionCatchChanceOverride", "0"))),
                    new("CatchIncrementsStat", NullIfEmpty(MxmlParser.GetPropertyValue(elem, "CatchIncrementsStat"))),
                };

                var fish = BuildCanonicalProductDict(product, productId,
                    includeNameLower: true,
                    includeCookingValue: true,
                    includeCdnUrl: false,
                    extraAfterHint: fishAfterHint);

                // Build fallback description if product has no description
                string desc = fish.GetValueOrDefault("Description")?.ToString() ?? "";
                if (string.IsNullOrEmpty(desc.Trim()))
                {
                    fish["Description"] = BuildFishFallbackDescription(quality, fishSize, biomes);
                }

                fishList.Add(fish);
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped fish: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {fishList.Count} fish");
        return fishList;
    }

    // Trade
    public static List<Dictionary<string, object?>> ParseTrade(string mxmlPath)
    {
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));
        var productsLookup = ProductLookup.LoadProductLookup(localisation, mxmlPath, includeRequirements: false);

        var tradeItems = new List<Dictionary<string, object?>>();
        foreach (var (itemId, product) in productsLookup)
        {
            string subtitle = product["Group"]?.ToString() ?? "";
            bool isTradeGoods = subtitle.StartsWith("Trade Goods", StringComparison.Ordinal);
            bool isSmuggledGoods = subtitle.StartsWith("Smuggled Goods", StringComparison.Ordinal);
            if (!isTradeGoods && !isSmuggledGoods) continue;

            string tradeCategory = product["TradeCategory"]?.ToString() ?? "";
            if (isTradeGoods && (string.IsNullOrEmpty(tradeCategory) || tradeCategory == "None")) continue;

            string iconPath = product["IconPath"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(iconPath)) continue;

            var trade = BuildCanonicalProductDict(product, itemId,
                includeNameLower: true,
                includeCookingValue: false,
                includeCdnUrl: true);
            tradeItems.Add(trade);
        }
        Console.WriteLine($"[OK] Parsed {tradeItems.Count} trade items");
        return tradeItems;
    }

    // ShipComponents
    private static readonly Dictionary<string, string> SubtitleToGroup = new()
    {
        ["UI_DROPSHIP_PART_SUB"] = "Hauler Starship Component",
        ["UI_FIGHTER_PART_SUB"] = "Fighter Starship Component",
        ["UI_SAIL_PART_SUB"] = "Solar Starship Component",
        ["UI_SCIENTIFIC_PART_SUB"] = "Explorer Starship Component",
        ["UI_FOS_BI_BODY_SUB"] = "Living Ship Component",
        ["UI_FOS_BI_TAIL_SUB"] = "Living Ship Component",
        ["UI_FOS_HEAD_SUB"] = "Living Ship Component",
        ["UI_FOS_LIMBS_SUB"] = "Living Ship Component",
        ["UI_SHIP_CORE_A_SUB"] = "Starship Core Component",
        ["UI_SHIP_CORE_B_SUB"] = "Starship Core Component",
        ["UI_SHIP_CORE_C_SUB"] = "Starship Core Component",
        ["UI_SHIP_CORE_S_SUB"] = "Starship Core Component",
    };

    public static List<Dictionary<string, object?>> ParseShipComponents(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var components = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return components;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string itemId = MxmlParser.GetPropertyValue(elem, "ID");
                string nameKey = MxmlParser.GetPropertyValue(elem, "Name");
                string subtitleKey = MxmlParser.GetPropertyValue(elem, "Subtitle");
                string descKey = MxmlParser.GetPropertyValue(elem, "Description");

                var iconElem = elem.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                var filenameElem = iconElem?.Elements("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Filename");
                string iconRaw = filenameElem?.Attribute("value")?.Value ?? "";
                string iconPath = !string.IsNullOrEmpty(iconRaw) ? MxmlParser.NormalizeGameIconPath(iconRaw) : "";
                if (string.IsNullOrEmpty(iconPath)) continue;

                string group = SubtitleToGroup.GetValueOrDefault(subtitleKey, "Starship Component");

                var heroIconProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "HeroIcon");
                string heroIconFilename = heroIconProp != null ? MxmlParser.GetPropertyValue(heroIconProp, "Filename") : "";
                string heroIconPath = !string.IsNullOrEmpty(heroIconFilename) ? MxmlParser.NormalizeGameIconPath(heroIconFilename) : "";

                object baseValue = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "BaseValue", "0"));
                string rarity = MxmlParser.GetNestedEnum(elem, "Rarity", "Rarity");
                string legality = MxmlParser.GetNestedEnum(elem, "Legality", "Legality");
                string tradeCategory = MxmlParser.GetNestedEnum(elem, "TradeCategory", "TradeCategory");
                string productCategory = MxmlParser.GetNestedEnum(elem, "Type", "ProductCategory");
                string substanceCategory = MxmlParser.GetNestedEnum(elem, "Category", "SubstanceCategory");
                string colour = MxmlParser.ParseColour(elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Colour"));

                components.Add(new Dictionary<string, object?>
                {
                    ["Id"] = itemId,
                    ["Name"] = MxmlParser.Translate(nameKey) ?? nameKey,
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["Group"] = group,
                    ["Description"] = MxmlParser.Translate(descKey) ?? "",
                    ["Description_LocStr"] = NullIfEmpty(descKey),
                    ["BaseValue"] = baseValue,
                    ["BaseValueUnits"] = baseValue,
                    ["CurrencyType"] = "Credits",
                    ["Icon"] = $"{itemId}.png",
                    ["IconPath"] = iconPath,
                    ["HeroIconPath"] = !string.IsNullOrEmpty(heroIconPath) ? heroIconPath : null,
                    ["BuildableShipTechID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "BuildableShipTechID")),
                    ["GroupID"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "GroupID")),
                    ["Colour"] = colour,
                    ["Rarity"] = NullIfEmpty(rarity),
                    ["Legality"] = NullIfEmpty(legality),
                    ["TradeCategory"] = NullIfEmpty(tradeCategory),
                    ["WikiCategory"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "WikiCategory")),
                    ["SubstanceCategory"] = NullIfEmpty(substanceCategory),
                    ["ProductCategory"] = NullIfEmpty(productCategory),
                    ["MaxStackSize"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "StackMultiplier", "1")),
                    ["BlueprintCost"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "RecipeCost", "0")),
                    ["ChargeValue"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "ChargeValue", "0")),
                    ["DefaultCraftAmount"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "DefaultCraftAmount", "1")),
                    ["CraftAmountStepSize"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CraftAmountStepSize", "1")),
                    ["CraftAmountMultiplier"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CraftAmountMultiplier", "1")),
                    ["SpecificChargeOnly"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "SpecificChargeOnly", "false")),
                    ["NormalisedValueOnWorld"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NormalisedValueOnWorld", "0"))),
                    ["NormalisedValueOffWorld"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NormalisedValueOffWorld", "0"))),
                    ["EconomyInfluenceMultiplier"] = ProductLookup.AsDouble(MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "EconomyInfluenceMultiplier", "0"))),
                    ["IsCraftable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsCraftable", "false")),
                    ["DeploysInto"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "DeploysInto")),
                    ["PinObjective"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjective")),
                    ["PinObjectiveTip"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjectiveTip")),
                    ["PinObjectiveMessage"] = NullIfEmpty(MxmlParser.GetPropertyValue(elem, "PinObjectiveMessage")),
                    ["PinObjectiveScannableType"] = NullIfEmpty(MxmlParser.GetNestedEnum(elem, "PinObjectiveScannableType", "ScanIconType")),
                    ["PinObjectiveEasyToRefine"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "PinObjectiveEasyToRefine", "false")),
                    ["NeverPinnable"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NeverPinnable", "false")),
                    ["CanSendToOtherPlayers"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "CanSendToOtherPlayers", "true")),
                    ["SourceTable"] = "Product",
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped ship component: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {components.Count} ship components");
        return components;
    }

    // BaseParts
    public static List<Dictionary<string, object?>> ParseBaseParts(string mxmlPath)
    {
        var parts = ParseProducts(mxmlPath, includeSubtitleKey: true);
        foreach (var part in parts)
        {
            string partId = part["Id"]?.ToString() ?? "";
            string subtitleKey = part.GetValueOrDefault("SubtitleKey")?.ToString() ?? "";
            if ((subtitleKey.Contains("SPACE", StringComparison.OrdinalIgnoreCase)) ||
                (partId.Contains("FREI", StringComparison.OrdinalIgnoreCase)))
            {
                part["Group"] = "Freighter Interior Module";
            }
            part.Remove("SubtitleKey");
        }
        Console.WriteLine($"[OK] Parsed {parts.Count} base building parts");
        return parts;
    }

    // ProceduralTech
    private static readonly Dictionary<string, string> QualityPrefix = new()
    {
        ["Normal"] = "C-Class",
        ["Rare"] = "B-Class",
        ["Epic"] = "A-Class",
        ["Legendary"] = "S-Class",
    };

    public static List<Dictionary<string, object?>> ParseProceduralTech(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));

        // Template icon, category, and charge maps from technology table.
        // Procedural items inherit Chargeable/ChargeAmount/BuildFullyCharged from their
        // Template entry in the main GcTechnologyTable (matching other editor behaviour).
        var templateIcons = new Dictionary<string, string>();
        var techCategories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var templateChargeable = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var templateChargeAmount = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var templateBuildFullyCharged = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        string techTablePath = Path.Combine(mbinDir, "nms_reality_gctechnologytable.MXML");
        if (File.Exists(techTablePath))
        {
            var techRoot = MxmlParser.LoadXml(techTablePath);
            var tTable = techRoot.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
            if (tTable != null)
                foreach (var te in tTable.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
                {
                    string tid = MxmlParser.GetPropertyValue(te, "ID");
                    var ip = te.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                    string fn = ip != null ? MxmlParser.GetPropertyValue(ip, "Filename") : "";
                    if (!string.IsNullOrEmpty(fn)) templateIcons[tid] = MxmlParser.NormalizeGameIconPath(fn);
                    string cat = MxmlParser.GetNestedEnum(te, "Category", "TechnologyCategory");
                    if (!string.IsNullOrEmpty(cat)) techCategories[tid] = cat;

                    bool isChargeable = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(te, "Chargeable", "false")) is true;
                    templateChargeable[tid] = isChargeable;
                    templateChargeAmount[tid] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(te, "ChargeAmount", "0"));
                    bool buildFC = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(te, "BuildFullyCharged", "false")) is true;
                    templateBuildFullyCharged[tid] = buildFC;
                }
        }

        var technologies = new List<Dictionary<string, object?>>();
        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null) return technologies;

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string techId = MxmlParser.GetPropertyValue(elem, "ID");
                string groupKey = MxmlParser.GetPropertyValue(elem, "Group");
                string nameKey = MxmlParser.GetPropertyValue(elem, "Name");
                string descKey = MxmlParser.GetPropertyValue(elem, "Description");
                string templateId = MxmlParser.GetPropertyValue(elem, "Template");
                string quality = MxmlParser.GetPropertyValue(elem, "Quality", "Normal");

                bool hasNameTranslation = !string.IsNullOrEmpty(nameKey) && localisation.ContainsKey(nameKey);
                string name = MxmlParser.Translate(nameKey) ?? nameKey;
                string description = MxmlParser.Translate(descKey) ?? "";
                string groupName = MxmlParser.Translate(groupKey) ?? techId;

                string qualityPrefix = QualityPrefix.GetValueOrDefault(quality, quality);
                string fullGroup = $"{qualityPrefix} {groupName}";

                // Determine Upgrade or Node suffix
                bool isNode = groupName.Contains("Node") ||
                    new[] { "Eyes", "Assembly", "Heart", "Suppressor", "Cortex", "Vents" }
                        .Any(x => groupName.Contains(x));
                string group = isNode ? $"{fullGroup} Node" : $"{fullGroup} Upgrade";

                if (!hasNameTranslation && !string.IsNullOrEmpty(groupName) && groupName != techId)
                    name = groupName;

                var iconProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                string iconFilename = iconProp != null ? MxmlParser.GetPropertyValue(iconProp, "Filename") : "";
                string iconPath = !string.IsNullOrEmpty(iconFilename) ? MxmlParser.NormalizeGameIconPath(iconFilename) : "";
                if (string.IsNullOrEmpty(iconPath) && !string.IsNullOrEmpty(templateId))
                    templateIcons.TryGetValue(templateId, out iconPath!);

                // StatLevels
                var statLevels = new List<Dictionary<string, object>>();
                var slProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "StatLevels");
                if (slProp != null)
                    foreach (var se in slProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "StatLevels"))
                    {
                        string statType = MxmlParser.GetNestedEnum(se, "Stat", "StatsType");
                        if (!string.IsNullOrEmpty(statType))
                            statLevels.Add(new()
                            {
                                ["StatType"] = statType,
                                ["Name"] = MxmlParser.FormatStatTypeName(statType),
                                ["ValueMin"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(se, "ValueMin", "0")),
                                ["ValueMax"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(se, "ValueMax", "0")),
                                ["WeightingCurve"] = MxmlParser.GetNestedEnum(se, "WeightingCurve", "WeightingCurve"),
                                ["AlwaysChoose"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(se, "AlwaysChoose", "false")),
                            });
                    }

                // Resolve TechnologyCategory from the technology table via template or own ID
                string? category = null;
                if (!string.IsNullOrEmpty(templateId))
                    techCategories.TryGetValue(templateId, out category);
                if (string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(techId))
                    techCategories.TryGetValue(techId, out category);

                // Resolve Chargeable/ChargeAmount/BuildFullyCharged from template
                // (matching NomNom's DB and NMSSaveEditor.jar behaviour).
                bool chargeable = false;
                object? chargeAmount = 0;
                bool buildFullyCharged = true; // Default true; most templates are BuildFullyCharged=true
                if (!string.IsNullOrEmpty(templateId))
                {
                    templateChargeable.TryGetValue(templateId, out chargeable);
                    templateChargeAmount.TryGetValue(templateId, out chargeAmount);
                    templateBuildFullyCharged.TryGetValue(templateId, out buildFullyCharged);
                }

                technologies.Add(new Dictionary<string, object?>
                {
                    ["Id"] = techId,
                    ["Icon"] = $"{techId}.png",
                    ["IconPath"] = iconPath ?? "",
                    ["Name"] = name,
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["Group"] = group,
                    ["Description"] = description,
                    ["Description_LocStr"] = NullIfEmpty(descKey),
                    ["Quality"] = quality,
                    ["Category"] = NullIfEmpty(category ?? ""),
                    ["Category_LocStr"] = TryGetCategoryLocKey(localisation, category),
                    ["Chargeable"] = chargeable,
                    ["ChargeAmount"] = chargeAmount,
                    ["BuildFullyCharged"] = buildFullyCharged,
                    ["NumStatsMin"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NumStatsMin", "0")),
                    ["NumStatsMax"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "NumStatsMax", "0")),
                    ["WeightingCurve"] = MxmlParser.GetNestedEnum(elem, "WeightingCurve", "WeightingCurve"),
                    ["StatLevels"] = statLevels,
                    ["SourceTable"] = "Technology",
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped procedural tech: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {technologies.Count} procedural technology upgrades");
        return technologies;
    }

    // PetEggTraitModifiers
    private static readonly Dictionary<string, string> TraitToInputType = new()
    {
        ["Helpfulness"] = "Neural Calibrator",
        ["Aggression"] = "Neural Calibrator",
        ["Independence"] = "Neural Calibrator",
    };

    public static List<Dictionary<string, object?>> ParsePetEggTraitModifiers(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var modifiers = new List<Dictionary<string, object?>>();

        // Build item lookup from substance and product tables for Name/Group/IconPath
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        var itemLookup = BuildEggItemLookup(mbinDir);

        var table = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "TraitModifiers");
        if (table == null) return modifiers;

        foreach (var row in table.Elements("Property").Where(e => e.Attribute("name")?.Value == "TraitModifiers"))
        {
            string productId = MxmlParser.GetPropertyValue(row, "ProductID");
            string substanceId = MxmlParser.GetPropertyValue(row, "SubstanceID");
            string itemId = !string.IsNullOrEmpty(productId) ? productId : substanceId;
            if (string.IsNullOrEmpty(itemId)) continue;

            string trait = MxmlParser.GetNestedEnum(row, "Trait", "PetTrait");
            bool increasesTrait = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(row, "IncreasesTrait", "false")) is true;
            string direction = increasesTrait ? "Increase" : "Decrease";
            string directionSymbol = increasesTrait ? "+" : "-";
            string traitDisplay = !string.IsNullOrEmpty(trait) ? trait : "Unknown";

            itemLookup.TryGetValue(itemId, out var lookup);
            string itemType = !string.IsNullOrEmpty(productId) ? "Product" : "Substance";
            if (lookup != null && lookup.TryGetValue("ItemType", out var lt) && lt is string lts)
                itemType = lts;

            modifiers.Add(new Dictionary<string, object?>
            {
                ["Id"] = $"{itemId}:{traitDisplay}",
                ["ItemId"] = itemId,
                ["ItemType"] = itemType,
                ["Name"] = lookup?.GetValueOrDefault("Name"),
                ["Group"] = lookup?.GetValueOrDefault("Group"),
                ["IconPath"] = lookup?.GetValueOrDefault("IconPath"),
                ["ProductID"] = NullIfEmpty(productId),
                ["SubstanceID"] = NullIfEmpty(substanceId),
                ["Trait"] = NullIfEmpty(trait),
                ["IncreasesTrait"] = increasesTrait,
                ["Direction"] = direction,
                ["DirectionSymbol"] = directionSymbol,
                ["EffectLabel"] = $"{direction} {traitDisplay}",
                ["EffectShort"] = $"{directionSymbol}{traitDisplay}",
                ["InputType"] = TraitToInputType.GetValueOrDefault(traitDisplay, "Neural Calibrator"),
                ["BaseValueOverride"] = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(row, "BaseValueOverride", "-1")),
            });
        }
        Console.WriteLine($"[OK] Parsed {modifiers.Count} pet egg trait modifiers");
        return modifiers;
    }

    private static Dictionary<string, Dictionary<string, object?>> BuildEggItemLookup(string mbinDir)
    {
        var lookup = new Dictionary<string, Dictionary<string, object?>>();
        string jsonDir = Path.Combine(Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder);
        MxmlParser.LoadLocalisation(jsonDir);

        // Load substances
        string substancePath = Path.Combine(mbinDir, "nms_reality_gcsubstancetable.MXML");
        if (File.Exists(substancePath))
        {
            var subRoot = MxmlParser.LoadXml(substancePath);
            var tableProp = subRoot.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
            if (tableProp != null)
            {
                foreach (var row in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
                {
                    string id = MxmlParser.GetPropertyValue(row, "ID");
                    if (string.IsNullOrEmpty(id) || lookup.ContainsKey(id)) continue;
                    string nameKey = MxmlParser.GetPropertyValue(row, "Name");
                    string subtitleKey = MxmlParser.GetPropertyValue(row, "Subtitle");
                    var iconProp = row.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                    string iconRaw = iconProp != null ? MxmlParser.GetPropertyValue(iconProp, "Filename") : "";
                    lookup[id] = new Dictionary<string, object?>
                    {
                        ["ItemType"] = "Substance",
                        ["Name"] = MxmlParser.Translate(nameKey, id),
                        ["IconPath"] = !string.IsNullOrEmpty(iconRaw) ? MxmlParser.NormalizeGameIconPath(iconRaw) : null,
                        ["Group"] = !string.IsNullOrEmpty(subtitleKey) ? MxmlParser.Translate(subtitleKey, subtitleKey) : null,
                    };
                }
            }
        }

        // Load products
        string productPath = Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML");
        if (File.Exists(productPath))
        {
            var localisation = MxmlParser.LoadLocalisation(jsonDir);
            var prodLookup = ProductLookup.LoadProductLookup(localisation, productPath, includeRequirements: false);
            foreach (var (id, product) in prodLookup)
            {
                if (lookup.ContainsKey(id)) continue;
                lookup[id] = new Dictionary<string, object?>
                {
                    ["ItemType"] = "Product",
                    ["Name"] = product.GetValueOrDefault("Name"),
                    ["IconPath"] = product.GetValueOrDefault("IconPath"),
                    ["Group"] = product.GetValueOrDefault("Group"),
                };
            }
        }

        return lookup;
    }

    /// <summary>Reset static caches (call before a fresh extraction run).</summary>
    public static void ResetCaches() => _itemNamesCache = null;

    /// <summary>
    /// Parses ALL recipes (refining + cooking) into a unified Recipes.json format.
    /// Uses the same proven XML traversal as ParseRefinery to ensure robust extraction.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseAllRecipes(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        MxmlParser.LoadLocalisation(Path.Combine(Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));
        var itemNames = LoadItemNames(mbinDir);
        var recipes = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null)
        {
            Console.WriteLine("[WARN] ParseAllRecipes: No <Property name=\"Table\"> found in MXML");
            return recipes;
        }

        // Use the same iteration pattern as ParseRefinery (proven to work)
        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string recipeId = MxmlParser.GetPropertyValue(elem, "Id", $"RECIPE_{recipes.Count + 1}");
                string nameKey = MxmlParser.GetPropertyValue(elem, "RecipeName");
                string recipeName = !string.IsNullOrEmpty(nameKey) ? MxmlParser.Translate(nameKey, nameKey) : "";
                string recipeType = MxmlParser.GetPropertyValue(elem, "RecipeType");
                bool isCooking = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "Cooking", "false")) is true;
                int timeToMake = 0;
                if (int.TryParse(MxmlParser.GetPropertyValue(elem, "TimeToMake", "0"), out int t))
                    timeToMake = t;

                // Parse result
                Dictionary<string, object?>? result = null;
                var resultProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Result");
                if (resultProp != null)
                {
                    string outId = MxmlParser.GetPropertyValue(resultProp, "Id");
                    string outType = NullIfEmpty(MxmlParser.GetNestedEnum(resultProp, "Type", "InventoryType")) ?? "Product";
                    int outAmt = 1;
                    int.TryParse(MxmlParser.GetPropertyValue(resultProp, "Amount", "1"), out outAmt);
                    result = new() { ["Id"] = outId, ["Type"] = outType, ["Amount"] = outAmt };
                }

                // Parse ingredients
                var ingredients = new List<Dictionary<string, object?>>();
                var ingredientsProp = elem.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Ingredients");
                if (ingredientsProp != null)
                {
                    foreach (var ing in ingredientsProp.Elements("Property"))
                    {
                        string ingId = MxmlParser.GetPropertyValue(ing, "Id");
                        if (string.IsNullOrEmpty(ingId)) continue;
                        string ingType = NullIfEmpty(MxmlParser.GetNestedEnum(ing, "Type", "InventoryType")) ?? "Product";
                        int ingAmt = 1;
                        int.TryParse(MxmlParser.GetPropertyValue(ing, "Amount", "1"), out ingAmt);
                        ingredients.Add(new() { ["Id"] = ingId, ["Type"] = ingType, ["Amount"] = ingAmt });
                    }
                }

                recipes.Add(new()
                {
                    ["Id"] = recipeId,
                    ["Category"] = isCooking ? "Cooking" : "Refining",
                    ["RecipeType"] = NullIfEmpty(recipeType) ?? "Standard",
                    ["RecipeType_LocStr"] = NullIfEmpty(recipeType),
                    ["RecipeName"] = recipeName,
                    ["RecipeName_LocStr"] = NullIfEmpty(nameKey),
                    ["Result"] = result,
                    ["Ingredients"] = ingredients,
                    ["TimeToMake"] = timeToMake,
                    ["Cooking"] = isCooking,
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped recipe: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {recipes.Count} total recipes for Recipes.json");
        return recipes;
    }

    /// <summary>
    /// Parses player title data from the title MXML file.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseTitles(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var titles = new List<Dictionary<string, object?>>();

        var titlesProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Titles");
        if (titlesProp == null)
        {
            Console.WriteLine("[WARN] ParseTitles: No <Property name=\"Titles\"> found in MXML");
            return titles;
        }

        foreach (var elem in titlesProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Titles"))
        {
            try
            {
                string titleId = MxmlParser.GetPropertyValue(elem, "ID", $"TITLE_{titles.Count + 1}");
                string nameKey = MxmlParser.GetPropertyValue(elem, "Title");
                string unlockDescKey = MxmlParser.GetPropertyValue(elem, "UnlockDescription");
                string alreadyDescKey = MxmlParser.GetPropertyValue(elem, "AlreadyUnlockedDescription");
                string unlockedByStat = MxmlParser.GetPropertyValue(elem, "UnlockedByStat");
                long statValue = 0;
                string statValStr = MxmlParser.GetPropertyValue(elem, "UnlockedByStatValue", "0");
                if (double.TryParse(statValStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double statValDbl))
                    statValue = (long)statValDbl;

                // Translate and replace %NAME% placeholder with {0}
                string translatedName = MxmlParser.Translate(nameKey, nameKey);
                translatedName = translatedName.Replace("%NAME%", "{0}");

                titles.Add(new()
                {
                    ["Id"] = titleId,
                    ["Name"] = translatedName,
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["UnlockDescription"] = MxmlParser.Translate(unlockDescKey, ""),
                    ["UnlockDescription_LocStr"] = NullIfEmpty(unlockDescKey),
                    ["AlreadyUnlockedDescription"] = MxmlParser.Translate(alreadyDescKey, ""),
                    ["AlreadyUnlockedDescription_LocStr"] = NullIfEmpty(alreadyDescKey),
                    ["UnlockedByStat"] = NullIfEmpty(unlockedByStat),
                    ["UnlockedByStatValue"] = statValue,
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped title: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {titles.Count} titles");
        return titles;
    }

    /// <summary>
    /// Parses frigate trait data from the frigate trait table MXML file.
    /// </summary>
    /// <summary>Strength enum -> numeric value mapping for frigate traits.</summary>
    private static readonly Dictionary<string, int> FrigateStrengthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Primary"] = -15,
        ["SecondarySmall"] = -3,  ["SecondaryMedium"] = -6,  ["SecondaryLarge"] = -9,
        ["TertiarySmall"] = -2,   ["TertiaryMedium"] = -4,   ["TertiaryLarge"] = -6,
        ["NegativeSmall"] = 1,    ["NegativeMedium"] = 2,    ["NegativeLarge"] = 4,
    };

    /// <summary>Fleet type names used in ChanceOfBeingOffered array (order matters).</summary>
    private static readonly string[] FleetTypeNames =
        { "Combat", "Exploration", "Mining", "Diplomacy", "Support", "Normandy", "DeepSpace", "DeepSpaceCommon", "Pirate", "GhostShip" };

    public static List<Dictionary<string, object?>> ParseFrigateTraits(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var traits = new List<Dictionary<string, object?>>();

        var traitsProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Traits");
        if (traitsProp == null)
        {
            Console.WriteLine("[WARN] ParseFrigateTraits: No <Property name=\"Traits\"> found in MXML");
            return traits;
        }

        foreach (var elem in traitsProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Traits"))
        {
            try
            {
                string traitId = MxmlParser.GetPropertyValue(elem, "ID", $"TRAIT_{traits.Count + 1}");
                string nameKey = MxmlParser.GetPropertyValue(elem, "DisplayName");
                string statType = MxmlParser.GetNestedEnum(elem, "FrigateStatType", "FrigateStatType");
                string strengthEnum = MxmlParser.GetNestedEnum(elem, "Strength", "FrigateTraitStrength");

                // Derive numeric strength from the enum
                int numericStrength = FrigateStrengthMap.TryGetValue(strengthEnum, out int s) ? s : 0;

                // Beneficial: Negative* strength enums are harmful
                bool beneficial = !strengthEnum.StartsWith("Negative", StringComparison.OrdinalIgnoreCase);

                // For fuel-type beneficial traits, strength is negative (reduces fuel consumption)
                // For negative traits, strength is positive (increases cost/penalty)
                // Adjust sign: positive stat types use positive strength for beneficial, negative for harmful
                // FuelCapacity and FuelBurnRate follow opposite convention from the base mapping
                if (statType.Equals("FuelCapacity", StringComparison.OrdinalIgnoreCase) && beneficial)
                    numericStrength = -Math.Abs(numericStrength);
                else if (statType.Equals("FuelBurnRate", StringComparison.OrdinalIgnoreCase) && !beneficial)
                    numericStrength = Math.Abs(numericStrength);
                else if (beneficial)
                    numericStrength = Math.Abs(numericStrength);
                else
                    numericStrength = -Math.Abs(numericStrength);

                // Primary: if strength is "Primary", the stat type tells us which fleet class
                string primary = "";
                string secondary = "";
                if (strengthEnum.Equals("Primary", StringComparison.OrdinalIgnoreCase))
                {
                    primary = statType.ToUpperInvariant() switch
                    {
                        "FUELCAPACITY" or "FUELBURNRATE" => "SUPPORT",
                        "COMBAT" => "COMBAT",
                        "EXPLORATION" => "EXPLORATION",
                        "MINING" => "MINING",
                        "DIPLOMATIC" => "DIPLOMACY",
                        "INVULNERABLE" or "SPEED" => "DEEPSPACE",
                        _ => statType.ToUpperInvariant()
                    };
                }
                else
                {
                    // Secondary: derive from ChanceOfBeingOffered
                    var chanceProp = elem.Elements("Property")
                        .FirstOrDefault(e => e.Attribute("name")?.Value == "ChanceOfBeingOffered");
                    if (chanceProp != null)
                    {
                        var offeredTypes = new List<string>();
                        foreach (var c in chanceProp.Elements("Property"))
                        {
                            string typeName = c.Attribute("name")?.Value ?? "";
                            string chance = c.Attribute("value")?.Value ?? "0";
                            if (int.TryParse(chance, out int cv) && cv > 0 &&
                                !string.IsNullOrEmpty(typeName))
                            {
                                offeredTypes.Add(typeName.ToUpperInvariant());
                            }
                        }
                        secondary = string.Join(",", offeredTypes);
                    }
                }

                traits.Add(new()
                {
                    ["Id"] = $"^{traitId}",
                    ["Name"] = MxmlParser.Translate(nameKey, traitId),
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["Type"] = NullIfEmpty(statType),
                    ["Strength"] = numericStrength,
                    ["Beneficial"] = beneficial,
                    ["Primary"] = NullIfEmpty(primary),
                    ["Secondary"] = NullIfEmpty(secondary),
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped frigate trait: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {traits.Count} frigate traits");
        return traits;
    }

    /// <summary>
    /// Parses settlement perk data from the settlement perks table MXML file.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseSettlementPerks(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var perks = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null)
        {
            Console.WriteLine("[WARN] ParseSettlementPerks: No <Property name=\"Table\"> found in MXML");
            return perks;
        }

        foreach (var elem in tableProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Table"))
        {
            try
            {
                string perkId = MxmlParser.GetPropertyValue(elem, "ID", $"PERK_{perks.Count + 1}");
                string nameKey = MxmlParser.GetPropertyValue(elem, "Name");
                string descKey = MxmlParser.GetPropertyValue(elem, "Description");
                bool isNegative = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsNegative", "false")) is true;
                bool isStarter = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsStarter", "false")) is true;
                bool isProc = MxmlParser.ParseValue(MxmlParser.GetPropertyValue(elem, "IsProc", "false")) is true;

                // Parse StatChanges array
                var statChanges = new List<Dictionary<string, object?>>();
                var statChangesProp = elem.Elements("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "StatChanges");
                if (statChangesProp != null)
                {
                    foreach (var sc in statChangesProp.Elements("Property"))
                    {
                        string statType = MxmlParser.GetNestedEnum(sc, "Stat", "SettlementStatType");
                        string strength = MxmlParser.GetNestedEnum(sc, "Strength", "SettlementStatStrength");
                        if (!string.IsNullOrEmpty(statType) || !string.IsNullOrEmpty(strength))
                        {
                            statChanges.Add(new()
                            {
                                ["Type"] = NullIfEmpty(statType),
                                ["Strength"] = NullIfEmpty(strength),
                            });
                        }
                    }
                }

                perks.Add(new()
                {
                    ["Id"] = $"^{perkId}",
                    ["Name"] = MxmlParser.Translate(nameKey, perkId),
                    ["Name_LocStr"] = NullIfEmpty(nameKey),
                    ["Description"] = MxmlParser.Translate(descKey, ""),
                    ["Description_LocStr"] = NullIfEmpty(descKey),
                    ["Beneficial"] = !isNegative,
                    ["Starter"] = isStarter,
                    ["Procedural"] = isProc,
                    ["StatChanges"] = statChanges,
                });
            }
            catch (Exception ex) { Console.WriteLine($"Warning: Skipped settlement perk: {ex.Message}"); }
        }
        Console.WriteLine($"[OK] Parsed {perks.Count} settlement perks");
        return perks;
    }

    /// <summary>
    /// Parses wiki guide topics from the wiki MXML file.
    /// Iterates through categories and their topics to produce a flat list of guide entries.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseWikiGuide(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var guides = new List<Dictionary<string, object?>>();

        var categoriesProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Categories");
        if (categoriesProp == null)
        {
            Console.WriteLine("[WARN] ParseWikiGuide: No <Property name=\"Categories\"> found in MXML");
            return guides;
        }

        foreach (var catElem in categoriesProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Categories"))
        {
            string categoryId = MxmlParser.GetPropertyValue(catElem, "CategoryID");
            string categoryName = MxmlParser.Translate(categoryId, categoryId);

            var topicsProp = catElem.Elements("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Topics");
            if (topicsProp == null) continue;

            foreach (var topicElem in topicsProp.Elements("Property").Where(e => e.Attribute("name")?.Value == "Topics"))
            {
                try
                {
                    string topicId = MxmlParser.GetPropertyValue(topicElem, "TopicID");
                    if (string.IsNullOrEmpty(topicId)) continue;

                    // Extract icon key from the Icon > Filename texture path
                    // e.g. "TEXTURES/UI/FRONTEND/ICONS/WIKI/SURVIVALBASICS.DDS" -> "SURVIVALBASICS"
                    string iconKey = "";
                    var iconProp = topicElem.Elements("Property")
                        .FirstOrDefault(e => e.Attribute("name")?.Value == "Icon");
                    if (iconProp != null)
                    {
                        var filenameProp = iconProp.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "Filename");
                        if (filenameProp != null)
                        {
                            string texturePath = filenameProp.Attribute("value")?.Value ?? "";
                            if (!string.IsNullOrEmpty(texturePath))
                            {
                                // Extract filename without extension
                                iconKey = Path.GetFileNameWithoutExtension(texturePath);
                            }
                        }
                    }

                    guides.Add(new()
                    {
                        ["Id"] = $"^{topicId}",
                        ["Name"] = MxmlParser.Translate(topicId, topicId),
                        ["Name_LocStr"] = NullIfEmpty(topicId),
                        ["Category"] = categoryName,
                        ["Category_LocStr"] = NullIfEmpty(categoryId),
                        ["IconKey"] = NullIfEmpty(iconKey),
                    });
                }
                catch (Exception ex) { Console.WriteLine($"Warning: Skipped wiki topic: {ex.Message}"); }
            }
        }
        Console.WriteLine($"[OK] Parsed {guides.Count} wiki guide topics");
        return guides;
    }

    /// <summary>
    /// Parses all unlockable rewards (season, twitch, platform) from their respective MXML files.
    /// The <paramref name="mxmlPath"/> should point to one of the reward MXML files; the other
    /// two are loaded from the same directory. Product names are resolved via the products table.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseRewards(string mxmlPath)
    {
        string mbinDir = Path.GetDirectoryName(mxmlPath)!;
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(mbinDir)!, ExtractorConfig.JsonSubfolder));
        string productsPath = Path.Combine(mbinDir, "nms_reality_gcproducttable.MXML");
        var productsLookup = ProductLookup.LoadProductLookup(localisation, productsPath,
            includeRequirements: false);

        var rewards = new List<Dictionary<string, object?>>();

        // --- Season rewards ---
        string seasonPath = Path.Combine(mbinDir, "UNLOCKABLESEASONREWARDS.MXML");
        if (File.Exists(seasonPath))
        {
            try
            {
                var root = MxmlParser.LoadXml(seasonPath);
                var tableProp = root.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
                if (tableProp != null)
                {
                    foreach (var elem in tableProp.Elements("Property")
                        .Where(e => e.Attribute("name")?.Value == "Table"))
                    {
                        string id = MxmlParser.GetPropertyValue(elem, "ID");
                        if (string.IsNullOrEmpty(id))
                            id = elem.Attribute("_id")?.Value ?? "";
                        if (string.IsNullOrEmpty(id)) continue;

                        string name = ResolveProductName(productsLookup, localisation, id);
                        productsLookup.TryGetValue(id, out var seasonProduct);

                        // Parse MustBeUnlocked flag
                        string mustUnlockRaw = MxmlParser.GetPropertyValue(elem, "MustBeUnlocked");
                        bool mustBeUnlocked = string.Equals(mustUnlockRaw, "true", StringComparison.OrdinalIgnoreCase);

                        // Parse SeasonIds array (take first value)
                        int seasonId = -1;
                        var seasonIdsProp = elem.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "SeasonIds");
                        if (seasonIdsProp != null)
                        {
                            var firstId = seasonIdsProp.Elements("Property")
                                .FirstOrDefault(e => e.Attribute("name")?.Value == "SeasonIds");
                            if (firstId != null && int.TryParse(firstId.Attribute("value")?.Value, out int sid))
                                seasonId = sid;
                        }

                        // Parse StageIds array (take first value)
                        int stageId = -1;
                        var stageIdsProp = elem.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "StageIds");
                        if (stageIdsProp != null)
                        {
                            var firstStage = stageIdsProp.Elements("Property")
                                .FirstOrDefault(e => e.Attribute("name")?.Value == "StageIds");
                            if (firstStage != null && int.TryParse(firstStage.Attribute("value")?.Value, out int stid))
                                stageId = stid;
                        }

                        rewards.Add(new Dictionary<string, object?>
                        {
                            ["Id"] = $"^{id}",
                            ["Name"] = name,
                            ["Name_LocStr"] = seasonProduct?.GetValueOrDefault("Name_LocStr"),
                            ["Subtitle_LocStr"] = seasonProduct?.GetValueOrDefault("Subtitle_LocStr"),
                            ["Category"] = "season",
                            ["ProductId"] = id,
                            ["MustBeUnlocked"] = mustBeUnlocked,
                            ["SeasonId"] = seasonId,
                            ["StageId"] = stageId,
                        });
                    }
                    Console.WriteLine($"  [OK] Season rewards: {rewards.Count}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"  [WARN] Season rewards: {ex.Message}"); }
        }
        else
        {
            Console.WriteLine($"  [SKIP] Season rewards: UNLOCKABLESEASONREWARDS.MXML not found");
        }

        // --- Twitch rewards ---
        int twitchCount = 0;
        string twitchPath = Path.Combine(mbinDir, "UNLOCKABLETWITCHREWARDS.MXML");
        if (File.Exists(twitchPath))
        {
            try
            {
                var root = MxmlParser.LoadXml(twitchPath);
                var tableProp = root.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
                if (tableProp != null)
                {
                    foreach (var elem in tableProp.Elements("Property")
                        .Where(e => e.Attribute("name")?.Value == "Table"))
                    {
                        string twitchId = MxmlParser.GetPropertyValue(elem, "TwitchId");
                        if (string.IsNullOrEmpty(twitchId)) continue;

                        string productId = MxmlParser.GetPropertyValue(elem, "ProductId");
                        string name = ResolveProductName(productsLookup, localisation, productId);
                        productsLookup.TryGetValue(productId, out var twitchProduct);

                        rewards.Add(new Dictionary<string, object?>
                        {
                            ["Id"] = $"^{twitchId}",
                            ["Name"] = name,
                            ["Name_LocStr"] = twitchProduct?.GetValueOrDefault("Name_LocStr"),
                            ["Subtitle_LocStr"] = twitchProduct?.GetValueOrDefault("Subtitle_LocStr"),
                            ["Category"] = "twitch",
                            ["ProductId"] = NullIfEmpty(productId),
                        });
                        twitchCount++;
                    }
                    Console.WriteLine($"  [OK] Twitch rewards: {twitchCount}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"  [WARN] Twitch rewards: {ex.Message}"); }
        }
        else
        {
            Console.WriteLine($"  [SKIP] Twitch rewards: UNLOCKABLETWITCHREWARDS.MXML not found");
        }

        // --- Platform rewards ---
        int platformCount = 0;
        string platformPath = Path.Combine(mbinDir, "UNLOCKABLEPLATFORMREWARDS.MXML");
        if (File.Exists(platformPath))
        {
            try
            {
                var root = MxmlParser.LoadXml(platformPath);
                var tableProp = root.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
                if (tableProp != null)
                {
                    foreach (var elem in tableProp.Elements("Property")
                        .Where(e => e.Attribute("name")?.Value == "Table"))
                    {
                        string rewardId = MxmlParser.GetPropertyValue(elem, "RewardId");
                        if (string.IsNullOrEmpty(rewardId)) continue;

                        string productId = MxmlParser.GetPropertyValue(elem, "ProductId");
                        string name = ResolveProductName(productsLookup, localisation, productId);
                        productsLookup.TryGetValue(productId, out var platformProduct);

                        rewards.Add(new Dictionary<string, object?>
                        {
                            ["Id"] = $"^{rewardId}",
                            ["Name"] = name,
                            ["Name_LocStr"] = platformProduct?.GetValueOrDefault("Name_LocStr"),
                            ["Subtitle_LocStr"] = platformProduct?.GetValueOrDefault("Subtitle_LocStr"),
                            ["Category"] = "platform",
                            ["ProductId"] = NullIfEmpty(productId),
                        });
                        platformCount++;
                    }
                    Console.WriteLine($"  [OK] Platform rewards: {platformCount}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"  [WARN] Platform rewards: {ex.Message}"); }
        }
        else
        {
            Console.WriteLine($"  [SKIP] Platform rewards: UNLOCKABLEPLATFORMREWARDS.MXML not found");
        }

        // --- Entitlement rewards (from RewardTable EntitlementTable) ---
        // These rewards are defined in REWARDTABLE.MXML under the EntitlementTable key.
        // They reference products via GcRewardSpecificSpecial -> ID.
        string rewardTablePath = Path.Combine(mbinDir, "REWARDTABLE.MXML");
        if (!File.Exists(rewardTablePath))
            rewardTablePath = Path.Combine(mbinDir, "nms_reality_gcrewardtable.MXML");
        int entitlementCount = 0;
        if (File.Exists(rewardTablePath))
        {
            try
            {
                var root = MxmlParser.LoadXml(rewardTablePath);
                var entTableProp = root.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "EntitlementTable");
                if (entTableProp != null)
                {
                    foreach (var elem in entTableProp.Elements("Property")
                        .Where(e => e.Attribute("name")?.Value == "EntitlementTable"))
                    {
                        string rewardId = MxmlParser.GetPropertyValue(elem, "RewardId");
                        if (string.IsNullOrEmpty(rewardId)) continue;

                        // Try to find the product ID from the reward sub-element
                        // GcRewardSpecificSpecial -> ID, GcRewardSpecificProduct -> ID, etc.
                        string productId = "";
                        var rewardProp = elem.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "Reward");
                        if (rewardProp != null)
                        {
                            // Look inside the nested reward type for an ID property
                            productId = rewardProp.Descendants("Property")
                                .Where(e => e.Attribute("name")?.Value == "ID")
                                .Select(e => e.Attribute("value")?.Value ?? "")
                                .FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";

                            // Fallback: try TechId for tech rewards
                            if (string.IsNullOrEmpty(productId))
                                productId = rewardProp.Descendants("Property")
                                    .Where(e => e.Attribute("name")?.Value == "TechId")
                                    .Select(e => e.Attribute("value")?.Value ?? "")
                                    .FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";
                        }

                        if (string.IsNullOrEmpty(productId)) continue;

                        string name = ResolveProductName(productsLookup, localisation, productId);
                        productsLookup.TryGetValue(productId, out var entProduct);

                        rewards.Add(new Dictionary<string, object?>
                        {
                            ["Id"] = $"^{rewardId}",
                            ["Name"] = name,
                            ["Name_LocStr"] = entProduct?.GetValueOrDefault("Name_LocStr"),
                            ["Subtitle_LocStr"] = entProduct?.GetValueOrDefault("Subtitle_LocStr"),
                            ["Category"] = "entitlement",
                            ["ProductId"] = NullIfEmpty(productId),
                        });
                        entitlementCount++;
                    }
                    Console.WriteLine($"  [OK] Entitlement rewards: {entitlementCount}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"  [WARN] Entitlement rewards: {ex.Message}"); }
        }

        Console.WriteLine($"[OK] Parsed {rewards.Count} total rewards");
        return rewards;
    }

    /// <summary>
    /// Race name from MXML AlienRace value to the ordinal used in NMS save files.
    /// Traders=Gek(0), Warriors=Vy'keen(1), Explorers=Korvax(2), Atlas(4), Builders=Autophage(8).
    /// </summary>
    private static readonly Dictionary<string, int> RaceOrdinalMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Traders"] = 0,
        ["Warriors"] = 1,
        ["Explorers"] = 2,
        ["Atlas"] = 4,
        ["Builders"] = 8,
    };

    /// <summary>
    /// Parses the alien speech table MXML into a Words.json format.
    /// Groups entries by _id to produce one word entry per unique word,
    /// with per-race group mappings matching the NMS save format.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseWords(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);

        // Collect all speech entries grouped by _id
        var wordGroups = new Dictionary<string, List<(string Group, int RaceOrdinal)>>(StringComparer.Ordinal);

        var tableProp = root.Descendants("Property").FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        if (tableProp == null)
        {
            Console.WriteLine("[WARN] ParseWords: No <Property name=\"Table\"> found in MXML");
            return new();
        }

        foreach (var elem in tableProp.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "GcAlienSpeechEntry"))
        {
            string? wordId = elem.Attribute("_id")?.Value;
            if (string.IsNullOrEmpty(wordId)) continue;

            string groupValue = MxmlParser.GetPropertyValue(elem, "Group");
            string raceName = MxmlParser.GetNestedEnum(elem, "Race", "AlienRace") ?? "";

            if (!RaceOrdinalMap.TryGetValue(raceName, out int raceOrdinal))
                continue; // Skip "None" or unknown races

            if (!wordGroups.ContainsKey(wordId))
                wordGroups[wordId] = new();

            wordGroups[wordId].Add(($"^{groupValue}", raceOrdinal));
        }

        // Build output sorted alphabetically by display text.
        // Display text is the _id lowercased (the _id IS the English word).
        var words = new List<Dictionary<string, object?>>(wordGroups.Count);
        foreach (var kvp in wordGroups.OrderBy(k => k.Key.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase))
        {
            string wordId = kvp.Key;
            string displayText = wordId.ToLowerInvariant();

            // Build race groups, ordered by race ordinal for determinism
            var groups = new Dictionary<string, object?>();
            foreach (var (group, raceOrdinal) in kvp.Value.OrderBy(g => g.RaceOrdinal))
            {
                if (!groups.ContainsKey(group))
                    groups[group] = raceOrdinal;
            }

            // Derive Text_LocStr from the first group key (by race ordinal).
            // e.g., for "ABOMINATION" with groups {"^TRA_ABOMINATION": 0}, Text_LocStr = "TRA_ABOMINATION"
            string? textLocStr = null;
            var sortedGroups = kvp.Value.OrderBy(g => g.RaceOrdinal).ToList();
            if (sortedGroups.Count > 0)
            {
                string firstGroup = sortedGroups[0].Group;
                textLocStr = firstGroup.StartsWith("^", StringComparison.Ordinal) ? firstGroup.Substring(1) : firstGroup;
            }

            words.Add(new Dictionary<string, object?>
            {
                ["Id"] = $"^{wordId}",
                ["Text"] = displayText,
                ["Text_LocStr"] = textLocStr != null ? NullIfEmpty(textLocStr) : null,
                ["Groups"] = groups,
            });
        }

        return words;
    }

    /// <summary>
    /// Resolves a product ID to a human-readable name using the product lookup and localisation.
    /// Falls back to the raw product ID if no name is found.
    /// </summary>
    private static string ResolveProductName(
        Dictionary<string, Dictionary<string, object?>> productsLookup,
        Dictionary<string, string> localisation,
        string productId)
    {
        if (string.IsNullOrEmpty(productId)) return "";

        // Try direct product lookup
        if (productsLookup.TryGetValue(productId, out var product))
        {
            string? name = product.GetValueOrDefault("Name")?.ToString();
            if (!string.IsNullOrEmpty(name)) return name;
        }

        // Try localisation key patterns
        foreach (string prefix in new[] { "", "UI_" })
        {
            string key = $"{prefix}{productId}_NAME";
            if (localisation.TryGetValue(key, out string? locName) && !string.IsNullOrEmpty(locName))
                return locName;
        }

        return productId;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Pet Accessories
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses PET_ACCESSORY descriptor groups from CHARACTERCUSTOMISATIONDESCRIPTORGROUPSDATA.MXML.
    /// Produces Companion Accessories.json with per-slot filtering data.
    /// </summary>
    public static List<Dictionary<string, object?>> ParsePetAccessories(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var accessories = new List<Dictionary<string, object?>>();

        // Find the PET_ACCESSORY descriptor group set
        var descriptorGroupSets = root.Descendants("Property")
            .Where(e => e.Attribute("name")?.Value == "DescriptorGroupSets"
                     && e.Attribute("_id")?.Value == "PET_ACCESSORY");

        var petAccSet = descriptorGroupSets.FirstOrDefault();
        if (petAccSet == null)
        {
            Console.WriteLine("[WARN] ParsePetAccessories: No PET_ACCESSORY descriptor group set found");
            return accessories;
        }

        var groupsProp = petAccSet.Elements("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "DescriptorGroups");
        if (groupsProp == null) return accessories;

        foreach (var grp in groupsProp.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "GcCustomisationDescriptorGroup"))
        {
            string groupId = MxmlParser.GetPropertyValue(grp, "GroupID");
            if (string.IsNullOrEmpty(groupId)) continue;

            string tipKey = MxmlParser.GetPropertyValue(grp, "Tip");
            string tipName = !string.IsNullOrEmpty(tipKey) ? MxmlParser.Translate(tipKey, "") : "";
            string linkedProduct = MxmlParser.GetPropertyValue(grp, "LinkedProductOrSpecialID");

            // Extract first descriptor value
            string descriptor = "";
            var descriptorsProp = grp.Elements("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Descriptors");
            if (descriptorsProp != null)
            {
                var firstDesc = descriptorsProp.Elements("Property")
                    .FirstOrDefault(e => e.Attribute("_index")?.Value == "0");
                if (firstDesc != null)
                    descriptor = firstDesc.Attribute("value")?.Value ?? "";
            }

            accessories.Add(new()
            {
                ["Id"] = groupId,
                ["Name"] = !string.IsNullOrEmpty(tipName) ? tipName : groupId,
                ["Name_LocStr"] = NullIfEmpty(tipKey),
                ["Descriptor"] = NullIfEmpty(descriptor),
                ["LinkedProduct"] = NullIfEmpty(linkedProduct),
            });
        }

        Console.WriteLine($"[OK] Parsed {accessories.Count} pet accessories");
        return accessories;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Pet Battle Moves
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses pet battler moves from petbattlermovestable.MXML.
    /// Produces Pet Battle Moves.json with full move detail.
    /// </summary>
    public static List<Dictionary<string, object?>> ParsePetBattleMoves(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var localisation = MxmlParser.LoadLocalisation(Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(mxmlPath))!, ExtractorConfig.JsonSubfolder));
        var moves = new List<Dictionary<string, object?>>();

        var movesProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Moves"
                && e.Parent?.Name.LocalName == "Data");
        if (movesProp == null)
        {
            // Try alternative: direct child of root Data element
            movesProp = root.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Moves");
        }
        if (movesProp == null)
        {
            Console.WriteLine("[WARN] ParsePetBattleMoves: No <Property name=\"Moves\"> found in MXML");
            return moves;
        }

        foreach (var elem in movesProp.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "GcPetBattlerMoveTemplate"))
        {
            try
            {
                string moveId = MxmlParser.GetPropertyValue(elem, "ID");
                if (string.IsNullOrEmpty(moveId)) continue;

                string debugDesc = MxmlParser.GetPropertyValue(elem, "DebugDescription");
                string target = MxmlParser.GetNestedEnum(elem, "PrimaryTarget", "PetBattlerTarget") ?? "";
                bool multiTurn = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(elem, "MultiTurnMove", "false")) is true;
                bool basicMove = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(elem, "BasicMove", "false")) is true;
                string iconStyle = MxmlParser.GetNestedEnum(elem, "OverrideMoveIcon", "PetBattlerIcon") ?? "";
                string nameStub = MxmlParser.GetPropertyValue(elem, "NameStub");

                // Parse Phases
                var phases = new List<Dictionary<string, object?>>();
                var phasesProp = elem.Elements("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Phases");
                if (phasesProp != null)
                {
                    foreach (var phase in phasesProp.Elements("Property")
                        .Where(e => e.Attribute("value")?.Value == "GcPetBattlerMovePhase"))
                    {
                        string strength = MxmlParser.GetNestedEnum(phase, "Strength", "PetPayloadStrength") ?? "";
                        string effect = MxmlParser.GetNestedEnum(phase, "Effect", "PetBattlerMoveEffect") ?? "";

                        phases.Add(new()
                        {
                            ["Strength"] = NullIfEmpty(strength),
                            ["Effect"] = NullIfEmpty(effect),
                        });
                    }
                }

                // Resolve LocIDToDescribeStat from NameStub
                string? locStatKey = null;
                if (!string.IsNullOrEmpty(nameStub))
                {
                    string candidate = $"UI_PB_STAT_{nameStub.ToUpperInvariant()}";
                    if (localisation.ContainsKey(candidate))
                        locStatKey = candidate;
                }

                moves.Add(new()
                {
                    ["Id"] = moveId,
                    ["DebugDescription"] = NullIfEmpty(debugDesc),
                    ["Target"] = NullIfEmpty(target),
                    ["MultiTurnMove"] = multiTurn,
                    ["BasicMove"] = basicMove,
                    ["IconStyle"] = NullIfEmpty(iconStyle),
                    ["NameStub"] = NullIfEmpty(nameStub),
                    ["LocIDToDescribeStat"] = locStatKey,
                    ["Phases"] = phases.Count > 0 ? phases : null,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Skipped pet battle move: {ex.Message}");
            }
        }

        Console.WriteLine($"[OK] Parsed {moves.Count} pet battle moves");
        return moves;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Pet Battle Movesets
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses pet battler movesets from petbattlermovesetstable.MXML.
    /// Produces Pet Battle Movesets.json with per-slot options.
    /// </summary>
    public static List<Dictionary<string, object?>> ParsePetBattleMovesets(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var movesets = new List<Dictionary<string, object?>>();

        var moveSetsProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "MoveSets"
                && e.Parent?.Name.LocalName == "Data");
        if (moveSetsProp == null)
        {
            moveSetsProp = root.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "MoveSets");
        }
        if (moveSetsProp == null)
        {
            Console.WriteLine("[WARN] ParsePetBattleMovesets: No <Property name=\"MoveSets\"> found");
            return movesets;
        }

        foreach (var elem in moveSetsProp.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "GcPetBattlerMoveSet"))
        {
            try
            {
                string setId = MxmlParser.GetPropertyValue(elem, "ID");
                if (string.IsNullOrEmpty(setId)) continue;

                var slots = new List<Dictionary<string, object?>>();
                for (int s = 1; s <= 5; s++)
                {
                    string slotName = $"Slot{s}";
                    var slotProp = elem.Elements("Property")
                        .FirstOrDefault(e => e.Attribute("name")?.Value == slotName);
                    if (slotProp == null) continue;

                    var optionsProp = slotProp.Descendants("Property")
                        .FirstOrDefault(e => e.Attribute("name")?.Value == "AllowedMoveTemplates"
                            && e.Elements("Property").Any());
                    if (optionsProp == null) continue;

                    var options = new List<Dictionary<string, object?>>();
                    foreach (var opt in optionsProp.Elements("Property")
                        .Where(e => e.Attribute("value")?.Value == "GcPetBattlerMoveSlotOption"))
                    {
                        string template = MxmlParser.GetPropertyValue(opt, "Template");
                        int cooldownMin = MxmlParser.ParseValue(
                            MxmlParser.GetPropertyValue(opt, "CooldownMin", "0")) is int cMin ? cMin : 0;
                        int cooldownMax = MxmlParser.ParseValue(
                            MxmlParser.GetPropertyValue(opt, "CooldownMax", "0")) is int cMax ? cMax : 0;
                        object weightVal = MxmlParser.ParseValue(
                            MxmlParser.GetPropertyValue(opt, "Weighting", "0"));
                        double weighting = weightVal is double d ? d : weightVal is int i ? i : 0.0;

                        options.Add(new()
                        {
                            ["Template"] = NullIfEmpty(template),
                            ["CooldownMin"] = cooldownMin,
                            ["CooldownMax"] = cooldownMax,
                            ["Weighting"] = weighting,
                        });
                    }

                    slots.Add(new()
                    {
                        ["Slot"] = s,
                        ["Options"] = options,
                    });
                }

                movesets.Add(new()
                {
                    ["Id"] = setId,
                    ["Slots"] = slots,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Skipped pet battle moveset: {ex.Message}");
            }
        }

        Console.WriteLine($"[OK] Parsed {movesets.Count} pet battle movesets");
        return movesets;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Game Table Globals (Pet Battle sections)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses pet-battle-relevant sections from gcgametableglobals.MXML:
    /// PetBiomeAffinities, PetAffinityLoc, PetAffinityLocStub, PetTargetLoc.
    /// Produces Game Table Globals.json.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseGameTableGlobals(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var result = new List<Dictionary<string, object?>>();

        // PetBiomeAffinities
        var affinities = new Dictionary<string, object?>();
        var affinityProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "PetBiomeAffinities");
        if (affinityProp != null)
        {
            foreach (var child in affinityProp.Elements("Property"))
            {
                string biome = child.Attribute("name")?.Value ?? "";
                string affinity = MxmlParser.GetPropertyValue(child, "PetBattlerAffinity");
                if (!string.IsNullOrEmpty(biome))
                    affinities[biome] = affinity;
            }
        }

        // PetAffinityLoc
        var affinityLoc = new Dictionary<string, object?>();
        var affinityLocProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "PetAffinityLoc");
        if (affinityLocProp != null)
        {
            foreach (var child in affinityLocProp.Elements("Property"))
            {
                string key = child.Attribute("name")?.Value ?? "";
                string val = child.Attribute("value")?.Value ?? "";
                if (!string.IsNullOrEmpty(key))
                    affinityLoc[key] = val;
            }
        }

        // PetAffinityLocStub
        var affinityLocStub = new Dictionary<string, object?>();
        var stubProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "PetAffinityLocStub");
        if (stubProp != null)
        {
            foreach (var child in stubProp.Elements("Property"))
            {
                string key = child.Attribute("name")?.Value ?? "";
                string val = child.Attribute("value")?.Value ?? "";
                if (!string.IsNullOrEmpty(key))
                    affinityLocStub[key] = val;
            }
        }

        // PetTargetLoc
        var targetLoc = new Dictionary<string, object?>();
        var targetLocProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "PetTargetLoc");
        if (targetLocProp != null)
        {
            foreach (var child in targetLocProp.Elements("Property"))
            {
                string key = child.Attribute("name")?.Value ?? "";
                string val = child.Attribute("value")?.Value ?? "";
                if (!string.IsNullOrEmpty(key))
                    targetLoc[key] = val;
            }
        }

        result.Add(new()
        {
            ["PetBiomeAffinities"] = affinities.Count > 0 ? affinities : null,
            ["PetAffinityLoc"] = affinityLoc.Count > 0 ? affinityLoc : null,
            ["PetAffinityLocStub"] = affinityLocStub.Count > 0 ? affinityLocStub : null,
            ["PetTargetLoc"] = targetLoc.Count > 0 ? targetLoc : null,
        });

        Console.WriteLine($"[OK] Parsed Game Table Globals (Biome affinities: {affinities.Count}, Affinity locs: {affinityLoc.Count})");
        return result;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Creature Species (from creaturedatatable.MXML)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses creature species data from creaturedatatable.MXML.
    /// Produces Creature Species.json with creature IDs, types, scales and pet metadata.
    /// </summary>
    public static List<Dictionary<string, object?>> ParseCreatureSpecies(string mxmlPath)
    {
        var root = MxmlParser.LoadXml(mxmlPath);
        var species = new List<Dictionary<string, object?>>();

        var tableProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Table"
                && e.Parent?.Name.LocalName == "Data");
        if (tableProp == null)
        {
            tableProp = root.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        }
        if (tableProp == null)
        {
            Console.WriteLine("[WARN] ParseCreatureSpecies: No <Property name=\"Table\"> found");
            return species;
        }

        foreach (var elem in tableProp.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "GcCreatureData"))
        {
            try
            {
                string creatureId = MxmlParser.GetPropertyValue(elem, "Id");
                if (string.IsNullOrEmpty(creatureId)) continue;

                string forceType = MxmlParser.GetNestedEnum(elem, "ForceType", "CreatureType") ?? "";
                string realType = MxmlParser.GetNestedEnum(elem, "RealType", "CreatureType") ?? "";
                string moveArea = MxmlParser.GetPropertyValue(elem, "MoveArea");
                bool ecoSystem = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(elem, "EcoSystemCreature", "false")) is true;
                bool canBeFemale = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(elem, "CanBeFemale", "false")) is true;
                bool onlyForced = MxmlParser.ParseValue(
                    MxmlParser.GetPropertyValue(elem, "OnlySpawnWhenIdIsForced", "false")) is true;

                string minScaleStr = MxmlParser.GetPropertyValue(elem, "MinScale", "1");
                string maxScaleStr = MxmlParser.GetPropertyValue(elem, "MaxScale", "1");
                double.TryParse(minScaleStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double minScale);
                double.TryParse(maxScaleStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double maxScale);

                string rarity = MxmlParser.GetNestedEnum(elem, "Rarity", "CreatureRarity") ?? "";
                string eggType = MxmlParser.GetPropertyValue(elem, "EggType");

                // Parse PetData section (accessory slots and riding modifiers)
                var accessorySlots = new List<Dictionary<string, object?>>();
                var ridingParts = new List<Dictionary<string, object?>>();

                var dataSection = elem.Elements("Property")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Data");
                if (dataSection != null)
                {
                    // GcCreaturePetData is nested inside a Data array element
                    var petData = dataSection.Descendants("Property")
                        .FirstOrDefault(e => e.Attribute("value")?.Value == "GcCreaturePetData");
                    if (petData != null)
                    {
                        var petInner = petData.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "GcCreaturePetData")
                            ?? petData;

                        var accSlotsProp = petInner.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "AccessorySlots");
                        if (accSlotsProp != null)
                        {
                            foreach (var accSlot in accSlotsProp.Elements("Property")
                                .Where(e => e.Attribute("value")?.Value == "GcCreaturePetAccessory"))
                            {
                                string reqDesc = MxmlParser.GetPropertyValue(accSlot, "RequiredDescriptor");

                                // Extract the AccessoryGroup from each Slots child element
                                var groups = new List<string>();
                                var slotsContainer = accSlot.Elements("Property")
                                    .FirstOrDefault(e => e.Attribute("name")?.Value == "Slots");
                                if (slotsContainer != null)
                                {
                                    foreach (var slotElem in slotsContainer.Elements("Property")
                                        .Where(e => e.Attribute("value")?.Value == "GcCreaturePetAccessorySlot"))
                                    {
                                        string group = MxmlParser.GetPropertyValue(slotElem, "AccessoryGroup");
                                        if (!string.IsNullOrEmpty(group))
                                            groups.Add(group);
                                    }
                                }

                                if (!string.IsNullOrEmpty(reqDesc) || groups.Count > 0)
                                {
                                    var entry = new Dictionary<string, object?> { ["RequiredDescriptor"] = reqDesc };
                                    if (groups.Count > 0)
                                        entry["AccessoryGroups"] = groups;
                                    accessorySlots.Add(entry);
                                }
                            }
                        }
                    }

                    // GcCreatureRidingData is also nested inside the Data array
                    var ridingData = dataSection.Descendants("Property")
                        .FirstOrDefault(e => e.Attribute("value")?.Value == "GcCreatureRidingData");
                    if (ridingData != null)
                    {
                        var ridingInner = ridingData.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "GcCreatureRidingData")
                            ?? ridingData;

                        var partMods = ridingInner.Elements("Property")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == "PartModifiers");
                        if (partMods != null)
                        {
                            foreach (var mod in partMods.Elements("Property")
                                .Where(e => e.Attribute("value")?.Value == "GcCreatureRidingDataPartModifier"))
                            {
                                string partName = MxmlParser.GetPropertyValue(mod, "PartName");
                                if (!string.IsNullOrEmpty(partName))
                                    ridingParts.Add(new() { ["PartName"] = partName });
                            }
                        }
                    }
                }

                // Pet battler data
                bool canBattle = false;
                string battleAffinity = "";
                var petBattleData = dataSection?.Descendants("Property")
                    .FirstOrDefault(e => e.Attribute("value")?.Value == "GcCreaturePetBattleData");
                if (petBattleData != null)
                {
                    var battleInner = petBattleData.Elements("Property")
                        .FirstOrDefault(e => e.Attribute("name")?.Value == "GcCreaturePetBattleData")
                        ?? petBattleData;
                    canBattle = MxmlParser.ParseValue(
                        MxmlParser.GetPropertyValue(battleInner, "CanBeUsedInPetBattler", "false")) is true;
                    battleAffinity = MxmlParser.GetNestedEnum(
                        battleInner, "ForcedAffinity", "PetBattleAffinity") ?? "";
                }

                species.Add(new()
                {
                    ["Id"] = creatureId,
                    ["ForceType"] = NullIfEmpty(forceType),
                    ["RealType"] = NullIfEmpty(realType),
                    ["MoveArea"] = NullIfEmpty(moveArea),
                    ["EcoSystemCreature"] = ecoSystem,
                    ["CanBeFemale"] = canBeFemale,
                    ["OnlySpawnWhenIdIsForced"] = onlyForced,
                    ["MinScale"] = minScale,
                    ["MaxScale"] = maxScale,
                    ["Rarity"] = NullIfEmpty(rarity),
                    ["EggType"] = NullIfEmpty(eggType),
                    ["CanBeUsedInPetBattler"] = canBattle,
                    ["PetBattlerForcedAffinity"] = NullIfEmpty(battleAffinity),
                    ["PetAccessorySlots"] = accessorySlots.Count > 0 ? accessorySlots : null,
                    ["RidingPartModifiers"] = ridingParts.Count > 0 ? ridingParts : null,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Skipped creature species entry: {ex.Message}");
            }
        }

        Console.WriteLine($"[OK] Parsed {species.Count} creature species");
        return species;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Creature Descriptors (from SCENE.MXML files + creaturefilenametable.MXML)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses creature descriptor trees from SCENE MXML files.
    /// Uses creaturefilenametable.MXML to map game creature IDs to their model scene files.
    /// Produces Creature Descriptors.json with hierarchical group/option descriptor trees.
    /// </summary>
    /// <param name="filenameTablePath">Path to creaturefilenametable.MXML.</param>
    /// <param name="mbinDir">
    /// Directory containing MBIN/MXML output. Scene MXML files are expected in
    /// subdirectories matching the original pak paths (e.g. models/planets/creatures/...).
    /// </param>
    public static List<Dictionary<string, object?>> ParseCreatureDescriptors(
        string filenameTablePath, string mbinDir)
    {
        var results = new List<Dictionary<string, object?>>();

        // Build creature ID to scene file path mapping
        var idToScenePath = BuildCreatureSceneMapping(filenameTablePath);
        if (idToScenePath.Count == 0)
        {
            Console.WriteLine("[WARN] ParseCreatureDescriptors: No creature-to-scene mappings found");
            return results;
        }

        int parsed = 0;
        int skipped = 0;

        foreach (var (creatureId, scenePath) in idToScenePath.OrderBy(kv => kv.Key))
        {
            // Convert NMS path (e.g. MODELS/PLANETS/CREATURES/CATRIG/CAT.SCENE.MBIN)
            // to local MXML filename (e.g. cat.scene.MXML)
            string sceneFileName = Path.GetFileName(scenePath)
                .Replace(".MBIN", ".MXML", StringComparison.OrdinalIgnoreCase);

            string mxmlPath = Path.Combine(mbinDir, sceneFileName);
            if (!File.Exists(mxmlPath))
            {
                // Scene MXML not available (may not have been extracted)
                skipped++;
                continue;
            }

            try
            {
                var sceneRoot = MxmlParser.LoadXml(mxmlPath);
                var groups = ParseSceneDescriptorTree(sceneRoot);

                if (groups.Count > 0)
                {
                    results.Add(new()
                    {
                        ["CreatureId"] = creatureId,
                        ["Groups"] = groups,
                    });
                    parsed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [WARN] Failed to parse descriptors for {creatureId} ({sceneFileName}): {ex.Message}");
            }
        }

        Console.WriteLine($"[OK] Parsed descriptors for {parsed} creatures ({skipped} scene files not found)");
        return results;
    }

    /// <summary>
    /// Builds a mapping from game creature ID to SCENE.MBIN path
    /// using creaturefilenametable.MXML.
    /// </summary>
    private static Dictionary<string, string> BuildCreatureSceneMapping(string filenameTablePath)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var root = MxmlParser.LoadXml(filenameTablePath);

        var tableProp = root.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Table"
                && e.Parent?.Name.LocalName == "Data");
        if (tableProp == null)
        {
            tableProp = root.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Table");
        }
        if (tableProp == null) return mapping;

        foreach (var elem in tableProp.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "GcCreatureFilename"))
        {
            string id = MxmlParser.GetPropertyValue(elem, "ID");
            string filename = MxmlParser.GetPropertyValue(elem, "Filename");
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(filename))
                mapping.TryAdd(id, filename);
        }

        return mapping;
    }

    /// <summary>
    /// Parses the descriptor tree from a creature SCENE MXML file.
    /// Descriptors are identified by node names starting with underscore.
    /// Returns hierarchical groups in the format expected by Creature Descriptors.json.
    /// </summary>
    private static List<Dictionary<string, object?>> ParseSceneDescriptorTree(XElement sceneRoot)
    {
        // Find the top-level MODEL node's Children
        var modelChildren = sceneRoot.Elements("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "Children");

        if (modelChildren == null) return new();

        return CollectDescriptorGroups(modelChildren);
    }

    /// <summary>
    /// Recursively collects descriptor groups from scene node children.
    /// Groups are collections of sibling nodes whose names start with a shared
    /// underscore-prefixed pattern (e.g. _HEAD_, _BODY_, _TAIL_).
    /// </summary>
    private static List<Dictionary<string, object?>> CollectDescriptorGroups(XElement childrenElement)
    {
        var groups = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var groupOrder = new List<string>();

        foreach (var childNode in childrenElement.Elements("Property")
            .Where(e => e.Attribute("value")?.Value == "TkSceneNodeData"))
        {
            string name = MxmlParser.GetPropertyValue(childNode, "Name");
            if (string.IsNullOrEmpty(name) || !name.StartsWith('_'))
                continue;

            // Skip *Shape mesh duplicates (engine artefacts, not real descriptors)
            if (name.EndsWith("Shape", StringComparison.OrdinalIgnoreCase))
                continue;

            // Determine group ID from name pattern (e.g. _Head_Hog becomes _HEAD_)
            string groupId = DeriveGroupId(name);

            if (!groups.ContainsKey(groupId))
            {
                groups[groupId] = new();
                groupOrder.Add(groupId);
            }

            // Build option entry
            var option = new Dictionary<string, object?>
            {
                ["Id"] = name.ToUpperInvariant().Replace(" ", ""),
                ["Name"] = name,
            };

            // Recurse into children for nested descriptor groups
            var nestedChildren = childNode.Elements("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == "Children");
            if (nestedChildren != null)
            {
                var childGroups = CollectDescriptorGroups(nestedChildren);
                if (childGroups.Count > 0)
                    option["Children"] = childGroups;
            }

            groups[groupId].Add(option);
        }

        // Build result maintaining discovery order
        var result = new List<Dictionary<string, object?>>();
        foreach (string gid in groupOrder)
        {
            result.Add(new()
            {
                ["GroupId"] = gid,
                ["Options"] = groups[gid],
            });
        }
        return result;
    }

    /// <summary>
    /// Derives a group ID from a descriptor node name.
    /// E.g. "_Head_Hog" becomes "_HEAD_", "_Body_Cat" becomes "_BODY_".
    /// For names with only one segment (e.g. "_Body_"), uses the full name.
    /// </summary>
    private static string DeriveGroupId(string name)
    {
        // Names are like _Head_Hog, _Body_Cat, _Tail_Alien0, _Shape_1
        // Group ID is the first segment: _HEAD_, _BODY_, _TAIL_, _SHAPE_
        int secondUnderscore = name.IndexOf('_', 1);
        if (secondUnderscore < 0)
            return name.ToUpperInvariant() + "_";

        return name[..(secondUnderscore + 1)].ToUpperInvariant();
    }
}
