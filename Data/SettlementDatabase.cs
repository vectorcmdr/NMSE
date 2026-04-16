namespace NMSE.Data;

/// <summary>Stat type affected by a perk.</summary>
public enum PerkStatType { Population, Happiness, Production, Upkeep, Sentinels, Debt, Alert, BugAttack }

/// <summary>Strength/direction of a perk's stat change.</summary>
public enum PerkStatStrength { PositiveWide, PositiveLarge, PositiveMedium, PositiveSmall, NegativeSmall, NegativeMedium, NegativeLarge }

/// <summary>A single stat change caused by a perk.</summary>
public class PerkStatChange
{
    /// <summary>The settlement stat affected by this change.</summary>
    public PerkStatType Type { get; init; }
    /// <summary>Magnitude and direction of the stat change.</summary>
    public PerkStatStrength Strength { get; init; }

    /// <summary>Human-readable range string for this stat change (e.g. "+10000..30000" or "-5..3").</summary>
    public string RangeText
    {
        get
        {
            if (!SettlementDatabase.PerkStatRanges.TryGetValue(Type, out var ranges)) return "";
            int[] r = ranges[(int)Strength];
            string sign = Strength.ToString().StartsWith("Negative", StringComparison.Ordinal) ? "" : "+";
            return $"{sign}{r[0]}..{r[1]}";
        }
    }
}

/// <summary>Represents a settlement perk with its stat effects.</summary>
public class SettlementPerk
{
    /// <summary>Unique perk identifier (e.g. "^STARTING_NEG1").</summary>
    public string Id { get; set; } = "";
    /// <summary>Human-readable perk name.</summary>
    public string Name { get; set; } = "";
    /// <summary>Localisation lookup key for Name. Null when not available.</summary>
    public string? NameLocStr { get; set; }
    /// <summary>Short description of the perk's effect.</summary>
    public string Description { get; set; } = "";
    /// <summary>Localisation lookup key for Description. Null when not available.</summary>
    public string? DescriptionLocStr { get; set; }
    /// <summary>Whether this perk is beneficial to the settlement.</summary>
    public bool Beneficial { get; set; }
    /// <summary>Whether this perk is procedurally generated.</summary>
    public bool Procedural { get; set; }
    /// <summary>Whether this is a starter perk assigned at settlement creation.</summary>
    public bool Starter { get; set; }
    /// <summary>Array of stat changes this perk applies to the settlement.</summary>
    public PerkStatChange[] StatChanges { get; set; } = Array.Empty<PerkStatChange>();

    /// <summary>Summary of stat effects, e.g. "Happiness +2..3, Production -30000..10000".</summary>
    public string StatEffectSummary => StatChanges.Length == 0 ? ""
        : string.Join(", ", StatChanges.Select(sc => $"{sc.Type} {sc.RangeText}"));

    /// <summary>Returns a string representation in "Name (Id)" format.</summary>
    public override string ToString() => $"{Name} ({Id})";
}

/// <summary>Static database of all known settlement perks, stat effects, and building-state milestones.</summary>
public static class SettlementDatabase
{
    /// <summary>
    /// Known milestone values from empirical game data.
    /// The int32 is the canonical value stored in the save file.
    /// </summary>
    internal static readonly (int Value, string LocKey)[] KnownMilestones =
    {
        (0,           "settlement.bs_empty_lot"),
        (127,         "settlement.bs_c_construction_complete"),
        (67108991,    "settlement.bs_c_system_activated"),
        (68157567,    "settlement.bs_c_to_b_upgrade"),
        (202375295,   "settlement.bs_c_to_b_awaiting"),
        (204472447,   "settlement.bs_b_complete"),
        (208666751,   "settlement.bs_b_to_a_upgrade"),
        (477102207,   "settlement.bs_b_to_a_awaiting"),
        (485490815,   "settlement.bs_a_complete"),
        (502268031,   "settlement.bs_a_to_s_upgrade"),
        (1039138943,  "settlement.bs_a_to_s_awaiting"),
        (1072693375,  "settlement.bs_s_complete"),
        (1073740927,  "settlement.bs_s_full"),
    };

    /// <summary>Per-stat perk strength ranges from database. Indexed by PerkStatStrength ordinal (0=PositiveWide..6=NegativeLarge).</summary>
    public static readonly Dictionary<PerkStatType, int[][]> PerkStatRanges = new()
    {
        { PerkStatType.Population,  new[] { new[]{1,5}, new[]{3,5}, new[]{2,3}, new[]{1,1}, new[]{-1,-1}, new[]{-3,-2}, new[]{-5,-3} } },
        { PerkStatType.Happiness,   new[] { new[]{2,6}, new[]{4,6}, new[]{3,4}, new[]{2,3}, new[]{-2,-1}, new[]{-4,-2}, new[]{-6,-4} } },
        { PerkStatType.Production,  new[] { new[]{20000,100000}, new[]{50000,100000}, new[]{30000,50000}, new[]{10000,30000}, new[]{-30000,-10000}, new[]{-50000,-30000}, new[]{-100000,-50000} } },
        { PerkStatType.Upkeep,      new[] { new[]{-100000,-10000}, new[]{-100000,-50000}, new[]{-50000,-30000}, new[]{-30000,-10000}, new[]{10000,30000}, new[]{30000,50000}, new[]{50000,100000} } },
        { PerkStatType.Sentinels,   new[] { new[]{-6,-1}, new[]{-5,-3}, new[]{-3,-2}, new[]{-2,-1}, new[]{2,3}, new[]{3,5}, new[]{5,10} } },
        { PerkStatType.Debt,        new[] { new[]{-100000,-5000}, new[]{-100000,-50000}, new[]{-50000,-10000}, new[]{-10000,-5000}, new[]{5000,10000}, new[]{10000,50000}, new[]{50000,100000} } },
        { PerkStatType.Alert,       new[] { new[]{-900,-100}, new[]{-1000,-1000}, new[]{-500,-300}, new[]{-300,-100}, new[]{50,0}, new[]{100,200}, new[]{200,300} } },
        { PerkStatType.BugAttack,   new[] { new[]{-900,-100}, new[]{-1000,-1000}, new[]{-500,-300}, new[]{-300,-100}, new[]{50,0}, new[]{100,200}, new[]{200,300} } },
    };

    /// <summary>
    /// All known settlement perks. Populated at startup from Settlement Perks.json
    /// via <see cref="LoadFromFile"/>. Empty until loaded.
    /// </summary>
    public static readonly IReadOnlyList<SettlementPerk> Perks = new List<SettlementPerk>();

    // --- Hardcoded fallback data removed ---
    // Perk data is now loaded from Resources/json/Settlement Perks.json at startup.
    // The JSON file is produced by the extractor's ParseSettlementPerks() method
    // from SETTLEMENTPERKSTABLE.MXML and contains perk IDs, names, descriptions,
    // loc keys, beneficial/starter/procedural flags, and stat changes.

    /// <summary>Lookup dictionary mapping perk ID to its entry.</summary>
    public static readonly Dictionary<string, SettlementPerk> ById =
        Perks.ToDictionary(p => p.Id, StringComparer.Ordinal);

    private static readonly Dictionary<string, (string Name, string Description)> _englishBackup = new();

    /// <summary>
    /// Loads settlement perks from a Settlement Perks.json file.
    /// Falls back to hardcoded data if the file is missing or invalid.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!System.IO.File.Exists(jsonPath)) return false;

        try
        {
            var content = System.IO.File.ReadAllBytes(jsonPath);
            using var doc = System.Text.Json.JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array) return false;

            var loaded = new List<SettlementPerk>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var statChanges = new List<PerkStatChange>();
                if (elem.TryGetProperty("StatChanges", out var scProp) && scProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var sc in scProp.EnumerateArray())
                    {
                        var typeStr = sc.TryGetProperty("Type", out var tP) ? tP.GetString() ?? "" : "";
                        var strengthStr = sc.TryGetProperty("Strength", out var sP) ? sP.GetString() ?? "" : "";
                        if (Enum.TryParse<PerkStatType>(typeStr, true, out var statType) &&
                            Enum.TryParse<PerkStatStrength>(strengthStr, true, out var statStrength))
                        {
                            statChanges.Add(new PerkStatChange { Type = statType, Strength = statStrength });
                        }
                    }
                }

                loaded.Add(new SettlementPerk
                {
                    Id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "",
                    Name = elem.TryGetProperty("Name", out var nP) ? nP.GetString() ?? "" : "",
                    NameLocStr = elem.TryGetProperty("Name_LocStr", out var nlP) ? nlP.GetString() : null,
                    Description = elem.TryGetProperty("Description", out var dP) ? dP.GetString() ?? "" : "",
                    DescriptionLocStr = elem.TryGetProperty("Description_LocStr", out var dlP) ? dlP.GetString() : null,
                    Beneficial = elem.TryGetProperty("Beneficial", out var bP) && bP.ValueKind == System.Text.Json.JsonValueKind.True,
                    Procedural = elem.TryGetProperty("Procedural", out var pP) && pP.ValueKind == System.Text.Json.JsonValueKind.True,
                    Starter = elem.TryGetProperty("Starter", out var sP2) && sP2.ValueKind == System.Text.Json.JsonValueKind.True,
                    StatChanges = statChanges.ToArray(),
                });
            }

            if (loaded.Count > 0)
            {
                var list = (List<SettlementPerk>)Perks;
                list.Clear();
                list.AddRange(loaded);

                ById.Clear();
                foreach (var p in loaded) ById[p.Id] = p;
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Applies localisation to perk names and descriptions using the specified service.
    /// </summary>
    public static int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive) { RevertLocalisation(); return 0; }

        int count = 0;
        foreach (var perk in Perks)
        {
            bool changed = false;

            if (!_englishBackup.ContainsKey(perk.Id))
                _englishBackup[perk.Id] = (perk.Name, perk.Description);

            var backup = _englishBackup[perk.Id];
            perk.Name = backup.Name;
            perk.Description = backup.Description;

            if (!string.IsNullOrEmpty(perk.NameLocStr))
            {
                var loc = service.Lookup(perk.NameLocStr);
                if (loc != null) { perk.Name = loc; changed = true; }
            }

            if (!string.IsNullOrEmpty(perk.DescriptionLocStr))
            {
                var loc = service.Lookup(perk.DescriptionLocStr);
                if (loc != null) { perk.Description = loc; changed = true; }
            }

            if (changed) count++;
        }
        return count;
    }

    /// <summary>
    /// Reverts all perk names and descriptions to their original English values.
    /// </summary>
    public static void RevertLocalisation()
    {
        foreach (var kvp in _englishBackup)
        {
            if (ById.TryGetValue(kvp.Key, out var perk))
            {
                perk.Name = kvp.Value.Name;
                perk.Description = kvp.Value.Description;
            }
        }
        _englishBackup.Clear();
    }
}
