using System.Text.Json;
using NMSE.Core;
using NMSE.Core.Utilities;

namespace NMSE.Data;

/// <summary>
/// Describes the accessory groups supported by a single body variant of a creature.
/// A creature may have multiple variants, each supporting different groups.
/// </summary>
public class CreatureAccessoryVariant
{
    /// <summary>Body descriptor that selects this variant (e.g. "_BODY_DEER").</summary>
    public string RequiredDescriptor { get; init; } = "";

    /// <summary>
    /// Ordered list of accessory group names supported by this variant.
    /// Values are "RIGHT", "LEFT", "FRONT", "BACK".
    /// </summary>
    public IReadOnlyList<string> AccessoryGroups { get; init; } = Array.Empty<string>();
}

/// <summary>Represents a companion creature entry with an ID and species name.</summary>
public class CompanionEntry
{
    /// <summary>Internal creature type identifier (e.g. "^CAT").</summary>
    public string Id { get; init; } = "";
    /// <summary>Display name for the species. May be blank if not yet populated from game data.</summary>
    public string Species { get; init; } = "";

    /// <summary>
    /// Per-body-variant accessory configurations. Null when the creature has no pet data
    /// or has empty accessory slots (no accessories are supported at all).
    /// </summary>
    public IReadOnlyList<CreatureAccessoryVariant>? AccessoryVariants { get; init; }

    /// <summary>Returns a string representation showing species name and ID, or just the ID.</summary>
    public override string ToString() => !string.IsNullOrEmpty(Species) ? $"{Species} ({Id})" : Id;
}

/// <summary>Static database of all known companion creature species.</summary>
public static class CompanionDatabase
{
    /// <summary>
    /// Known planetary biome types where companions can be found.
    /// Matches the Creature Builder biome list.
    /// </summary>
    public static readonly string[] BiomeTypes =
    {
        "Lush", "Toxic", "Scorched", "Radioactive", "Frozen", "Barren",
        "Dead", "Weird", "Red", "Green", "Blue", "Test",
        "Swamp", "Lava", "Waterworld", "GasGiant", "All"
    };

    /// <summary>
    /// Mapping of biome type names to their UI localisation keys.
    /// </summary>
    public static readonly Dictionary<string, string> BiomeTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
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
        ["GasGiant"] = "companion.biome_gasgiant",
        ["All"] = "companion.biome_all",
    };

    /// <summary>
    /// All known companion creature entries. Populated at runtime from the Creature Species JSON file
    /// via LoadFromFile. Empty until data is loaded.
    /// </summary>
    public static readonly IReadOnlyList<CompanionEntry> Entries = new List<CompanionEntry>();

    /// <summary>Lookup dictionary mapping companion ID to its entry (case-insensitive).</summary>
    public static readonly Dictionary<string, CompanionEntry> ById = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Known creature archetype classifications.
    /// Matches the Creature Builder creature type list.
    /// </summary>
    public static readonly string[] CreatureTypes =
    {
        "None", "Antelope", "Bear", "Beetle", "Bird", "Blob", "Bones", "Brainless",
        "BugFiend", "BugQueen", "Butterfly", "Cat", "Cow", "Crab", "Digger", "Dino",
        "Drill", "Drone", "Fiend", "FiendFishBig", "FiendFishSmall", "Fish",
        "FishPredator", "FishPrey", "Floater", "FloatingGasbag", "FlyingBeetle",
        "FlyingLizard", "FlyingSnake", "GiantRobot", "JellyBoss", "JellyBossBrood",
        "Jellyfish", "LandJellyfish", "LandSquid", "MiniDrone", "MiniFiend", "MiniRobo",
        "Passive", "Pet", "PlayerPredator", "Plough", "PlowBig", "PlowSmall",
        "Predator", "Prey", "ProtoDigger", "ProtoFlyer", "ProtoRoller", "Protodigger",
        "Quad", "Robot", "RockCreature", "Rodent", "SandWorm", "Scuttler", "SeaSnake",
        "SeaWorm", "Shark", "Slug", "Snake", "SpaceFloater", "Spider", "Strider",
        "Striderglow", "Triceratops", "Tyrannosaurus", "Walker", "Weird"
    };

    /// <summary>
    /// Loads creature species data from Creature Species.json.
    /// Returns true if data was loaded successfully.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            var loaded = new List<CompanionEntry>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                string id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(id)) continue;

                // Derive species display name from the ID
                string species = id.Replace('_', ' ');
                species = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(species.ToLowerInvariant());

                // Parse per-variant accessory configurations
                List<CreatureAccessoryVariant>? variants = null;
                if (elem.TryGetProperty("PetAccessorySlots", out var slotsP) &&
                    slotsP.ValueKind == JsonValueKind.Array)
                {
                    variants = new List<CreatureAccessoryVariant>();
                    foreach (var slotElem in slotsP.EnumerateArray())
                    {
                        string reqDesc = slotElem.TryGetProperty("RequiredDescriptor", out var rdP)
                            ? rdP.GetString() ?? "" : "";

                        var groups = new List<string>();
                        if (slotElem.TryGetProperty("AccessoryGroups", out var agP) &&
                            agP.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var g in agP.EnumerateArray())
                            {
                                string gv = g.GetString() ?? "";
                                if (!string.IsNullOrEmpty(gv))
                                    groups.Add(gv);
                            }
                        }

                        variants.Add(new CreatureAccessoryVariant
                        {
                            RequiredDescriptor = reqDesc,
                            AccessoryGroups = groups,
                        });
                    }

                    if (variants.Count == 0)
                        variants = null;
                }

                loaded.Add(new CompanionEntry
                {
                    Id = $"^{id}",
                    Species = species,
                    AccessoryVariants = variants,
                });
            }

            if (loaded.Count > 0)
            {
                var list = (List<CompanionEntry>)Entries;
                list.Clear();
                list.AddRange(loaded);

                ById.Clear();
                foreach (var e in loaded) ById[e.Id] = e;
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Represents a single descriptor option within a creature part group.
/// </summary>
public class DescriptorOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Child groups that become available when this descriptor is selected.</summary>
    public List<DescriptorGroup> Children { get; set; } = new();

    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : Name;
}

/// <summary>
/// Represents a group of descriptor options (e.g. _HEAD_, _BODY_, _TAIL_).
/// The user picks one descriptor per group.
/// </summary>
public class DescriptorGroup
{
    public string GroupId { get; set; } = "";
    public List<DescriptorOption> Descriptors { get; set; } = new();

    public override string ToString() => GroupId;
}

/// <summary>
/// Represents a creature type with its available part groups.
/// </summary>
public class CreaturePartEntry
{
    public string CreatureId { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public List<DescriptorGroup> Details { get; set; } = new();
}

/// <summary>
/// Static database of creature part/descriptor data for the companion panel.
/// Contains descriptor trees for 55 creature types with 3056 descriptor nodes.
/// Each creature type has groups of descriptor options; some options expose
/// child groups, forming a recursive tree of part selections.
/// </summary>
public static class CreaturePartDatabase
{
    private static Dictionary<string, CreaturePartEntry>? _byId;

    /// <summary>All creature part entries.</summary>
    public static IReadOnlyList<CreaturePartEntry> Entries => AllEntries;

    /// <summary>Lookup creature part data by CreatureId (case-insensitive, without ^ prefix).</summary>
    public static IReadOnlyDictionary<string, CreaturePartEntry> ById
    {
        get
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, CreaturePartEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in AllEntries)
                    if (!string.IsNullOrEmpty(entry.CreatureId))
                        _byId.TryAdd(entry.CreatureId, entry);
            }
            return _byId;
        }
    }

    /// <summary>
    /// Gets the creature part entry for a save-file CreatureID (e.g. "^CAT" -> "CAT").
    /// Returns null if the creature has no part data available.
    /// </summary>
    public static CreaturePartEntry? GetForCreatureId(string? creatureId)
    {
        if (string.IsNullOrEmpty(creatureId) || creatureId == "^") return null;
        string stripped = creatureId.TrimStart('^');
        if (ById.TryGetValue(stripped, out var entry))
            return entry;
        return null;
    }

    /// <summary>
    /// Flattens the descriptor tree for a creature type based on current selections.
    /// Returns the ordered list of groups where each group's available options depend
    /// on which descriptors are currently selected in parent groups.
    /// </summary>
    public static List<DescriptorGroup> GetFlatGroups(CreaturePartEntry entry, IReadOnlyList<string> selectedDescriptors)
    {
        var result = new List<DescriptorGroup>();
        var selectedSet = new HashSet<string>(selectedDescriptors, StringComparer.OrdinalIgnoreCase);

        foreach (var group in entry.Details)
            CollectGroups(group, selectedSet, result);

        return result;
    }

    private static void CollectGroups(DescriptorGroup group, HashSet<string> selectedSet, List<DescriptorGroup> result)
    {
        result.Add(group);

        // Find which descriptor in this group is selected
        DescriptorOption? selected = null;
        foreach (var desc in group.Descriptors)
        {
            if (selectedSet.Contains(desc.Id))
            {
                selected = desc;
                break;
            }
        }

        // If a descriptor is selected and it has children, recurse into those child groups
        if (selected?.Children != null)
        {
            foreach (var childGroup in selected.Children)
                CollectGroups(childGroup, selectedSet, result);
        }
    }

    /// <summary>
    /// Generates a random 10-digit descriptor ID (matching the Creature Builder format).
    /// </summary>
    public static string NewDescriptorId()
    {
        var rng = new Random();
        var chars = new char[10];
        for (int i = 0; i < 10; i++)
            chars[i] = (char)('0' + rng.Next(10));
        return new string(chars);
    }

    /// <summary>
    /// Loads descriptor data from Creature Descriptors.json, replacing the hardcoded
    /// fallback entries. Returns true if data was loaded successfully.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            var loaded = new List<CreaturePartEntry>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                string creatureId = elem.TryGetProperty("CreatureId", out var cidP)
                    ? cidP.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(creatureId)) continue;

                var groups = new List<DescriptorGroup>();
                if (elem.TryGetProperty("Groups", out var groupsArr)
                    && groupsArr.ValueKind == JsonValueKind.Array)
                {
                    groups = ParseDescriptorGroups(groupsArr);
                }

                loaded.Add(new CreaturePartEntry
                {
                    CreatureId = creatureId,
                    FriendlyName = creatureId,
                    Details = groups,
                });
            }

            if (loaded.Count > 0)
            {
                AllEntries.Clear();
                AllEntries.AddRange(loaded);
                _byId = null; // force rebuild of lookup dictionary
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses descriptor groups from a JSON array element.
    /// </summary>
    private static List<DescriptorGroup> ParseDescriptorGroups(JsonElement groupsArr)
    {
        var groups = new List<DescriptorGroup>();
        foreach (var gElem in groupsArr.EnumerateArray())
        {
            string groupId = gElem.TryGetProperty("GroupId", out var gidP)
                ? gidP.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(groupId)) continue;

            var descriptors = new List<DescriptorOption>();
            if (gElem.TryGetProperty("Options", out var optsArr)
                && optsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var optElem in optsArr.EnumerateArray())
                {
                    string optId = optElem.TryGetProperty("Id", out var oidP)
                        ? oidP.GetString() ?? "" : "";
                    string optName = optElem.TryGetProperty("Name", out var onP)
                        ? onP.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(optId)) continue;

                    var children = new List<DescriptorGroup>();
                    if (optElem.TryGetProperty("Children", out var childArr)
                        && childArr.ValueKind == JsonValueKind.Array)
                    {
                        children = ParseDescriptorGroups(childArr);
                    }

                    descriptors.Add(new DescriptorOption
                    {
                        Id = optId,
                        Name = optName,
                        Children = children,
                    });
                }
            }

            groups.Add(new DescriptorGroup
            {
                GroupId = groupId,
                Descriptors = descriptors,
            });
        }
        return groups;
    }

    // ============================================
    // Creature part data is loaded at runtime from
	// Creature Descriptors.json via LoadFromFile.
	// Empty until data is loaded.
    // ============================================

    private static readonly List<CreaturePartEntry> AllEntries = new();
}

// =======================================================
// Companion Accessories (from Companion Accessories.json)
// =======================================================
// Replaces the previous hard coded implementation with
// data loaded from the game's JSON file, which is more complete
// and accurate. It's also not reliant on community part lists or
// other outdated sources.

/// <summary>Represents a single pet accessory entry from Companion Accessories.json.</summary>
public class CompanionAccessoryEntry
{
    /// <summary>Accessory group identifier (e.g. "PET_ACC_0", "PET_ACC_NULL").</summary>
    public string Id { get; set; } = "";
    /// <summary>Localised display name (e.g. "Cargo Drum").</summary>
    public string Name { get; set; } = "";
    /// <summary>Game localisation key for the name (e.g. "UI_TIP_PET_ACCESSORY_1").</summary>
    public string? NameLocStr { get; set; }
    /// <summary>Primary model descriptor (e.g. "_ACC_CARGOCYLINDER").</summary>
    public string? Descriptor { get; set; }
    /// <summary>Linked special product or unlock ID (e.g. "SPEC_PETCUST2").</summary>
    public string? LinkedProduct { get; set; }

    public override string ToString() => !string.IsNullOrEmpty(Name) ? Name : Id;
}

/// <summary>
/// Companion accessory slot index mapping: [0] = Right, [1] = Left, [2] = Front.
/// Save data stores exactly 3 Data entries per pet; each index corresponds to a fixed group.
/// Some creatures support a BACK group instead of or in addition to FRONT, which shares
/// the same save index (2) as Front.
/// </summary>
public enum AccessorySlot
{
    /// <summary>Right side accessory (index 0 in save).</summary>
    Right = 0,
    /// <summary>Left side accessory (index 1 in save).</summary>
    Left = 1,
    /// <summary>Front accessory (index 2 in save).</summary>
    Front = 2,
    /// <summary>Back accessory (shares index 2 in save with Front; used by BLOB, GRUNT, etc.).</summary>
    Back = 3,
}

/// <summary>Static database of companion pet accessories, loaded from Companion Accessories.json.</summary>
public static class CompanionAccessoryDatabase
{
    /// <summary>All loaded accessory entries.</summary>
    public static readonly IReadOnlyList<CompanionAccessoryEntry> Entries = new List<CompanionAccessoryEntry>();

    /// <summary>Lookup dictionary mapping accessory ID to entry.</summary>
    public static readonly Dictionary<string, CompanionAccessoryEntry> ById =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> _englishNameBackup = new();

    /// <summary>
    /// Per-slot allowed accessory IDs, derived from the game's CHARACTERCUSTOMISATIONUIDATA.MXML(MBIN)
    /// and PETACCESSORYTABLE.MXML. The same shared accessories (PET_ACC_NULL, PET_ACC_0-11)
    /// appear in all slots; slot-specific accessories are unique to Right, Left, Front, or Back.
    /// </summary>
    private static readonly Dictionary<AccessorySlot, HashSet<string>> SlotFilter = new()
    {
        [AccessorySlot.Right] = new(StringComparer.OrdinalIgnoreCase)
        {
            "PET_ACC_NULL",
            "PET_ACC_0", "PET_ACC_1", "PET_ACC_2", "PET_ACC_3", "PET_ACC_4",
            "PET_ACC_5", "PET_ACC_6", "PET_ACC_7", "PET_ACC_8", "PET_ACC_9",
            "PET_ACC_10", "PET_ACC_11",
            // Right-specific (R-prefix descriptors)
            "PET_ACC_19", "PET_ACC_20", "PET_ACC_21", "PET_ACC_22",
            "PET_ACC_23", "PET_ACC_24", "PET_ACC_25",
            "PET_ACC_27", "PET_ACC_28",
        },
        [AccessorySlot.Left] = new(StringComparer.OrdinalIgnoreCase)
        {
            "PET_ACC_NULL",
            "PET_ACC_0", "PET_ACC_1", "PET_ACC_2", "PET_ACC_3", "PET_ACC_4",
            "PET_ACC_5", "PET_ACC_6", "PET_ACC_7", "PET_ACC_8", "PET_ACC_9",
            "PET_ACC_10", "PET_ACC_11",
            // Left-specific (L-prefix descriptors)
            "PET_ACC_12", "PET_ACC_13", "PET_ACC_14", "PET_ACC_15",
            "PET_ACC_16", "PET_ACC_17", "PET_ACC_18",
            "PET_ACC_26", "PET_ACC_29",
        },
        [AccessorySlot.Front] = new(StringComparer.OrdinalIgnoreCase)
        {
            "PET_ACC_NULL",
            "PET_ACC_0", "PET_ACC_1", "PET_ACC_2", "PET_ACC_3", "PET_ACC_4",
            "PET_ACC_5", "PET_ACC_6", "PET_ACC_7", "PET_ACC_8", "PET_ACC_9",
            "PET_ACC_10", "PET_ACC_11",
            // Front-specific
            "PET_ACC_30",
        },
        [AccessorySlot.Back] = new(StringComparer.OrdinalIgnoreCase)
        {
            "PET_ACC_NULL",
            // Back allows shared accessories only (no L/R-specific, no MechanicalPaw variants)
            "PET_ACC_0", "PET_ACC_1", "PET_ACC_2", "PET_ACC_3", "PET_ACC_4",
            "PET_ACC_5", "PET_ACC_6", "PET_ACC_7", "PET_ACC_8", "PET_ACC_9",
            "PET_ACC_10", "PET_ACC_11",
        },
    };

    /// <summary>
    /// Returns the accessory entries valid for a given slot.
    /// </summary>
    public static IReadOnlyList<CompanionAccessoryEntry> GetEntriesForSlot(AccessorySlot slot)
    {
        if (!SlotFilter.TryGetValue(slot, out var allowed))
            return (IReadOnlyList<CompanionAccessoryEntry>)Entries;

        return Entries.Where(e => allowed.Contains(e.Id)).ToList();
    }

    /// <summary>Default slot layout used when a creature's supported groups are unknown.</summary>
    private static readonly AccessorySlot[] DefaultSlotLayout = { AccessorySlot.Right, AccessorySlot.Left, AccessorySlot.Front };

    /// <summary>
    /// Maps a game AccessoryGroup name (e.g. "RIGHT", "LEFT", "FRONT", "BACK") to an AccessorySlot value.
    /// Returns null for unrecognised group names.
    /// </summary>
    public static AccessorySlot? GroupNameToSlot(string groupName)
    {
        return groupName.ToUpperInvariant() switch
        {
            "RIGHT" => AccessorySlot.Right,
            "LEFT" => AccessorySlot.Left,
            "FRONT" => AccessorySlot.Front,
            "BACK" => AccessorySlot.Back,
            _ => null,
        };
    }

    /// <summary>
    /// Returns the save data index for a given AccessorySlot when using the default
    /// [Right, Left, Front] layout. For non-default layouts (e.g. GRUNT with [Back, Front]),
    /// use the positional index from <see cref="GetSlotLayoutForCreature"/> instead, since
    /// save data stores accessories at Data[0], Data[1], Data[2] in the order they appear
    /// in the creature's AccessoryGroups definition.
    /// </summary>
    [Obsolete("Save indices are positional per creature layout. Use the UI row index directly.")]
    public static int SlotToSaveIndex(AccessorySlot slot)
    {
        return slot switch
        {
            AccessorySlot.Right => 0,
            AccessorySlot.Left => 1,
            AccessorySlot.Front => 2,
            AccessorySlot.Back => 2,
            _ => -1,
        };
    }

    /// <summary>
    /// Returns the ordered slot layout (up to 3 slots) for a creature based on its type ID.
    /// When the creature type is known and has accessory variant data, this returns the
    /// distinct groups from the first variant that has them (most creatures share the same
    /// groups across all variants). Falls back to [Right, Left, Front] when unknown.
    /// </summary>
    public static AccessorySlot[] GetSlotLayoutForCreature(string? creatureId)
    {
        if (string.IsNullOrEmpty(creatureId))
            return DefaultSlotLayout;

        if (!CompanionDatabase.ById.TryGetValue(creatureId, out var entry))
            return DefaultSlotLayout;

        if (entry.AccessoryVariants == null || entry.AccessoryVariants.Count == 0)
            return Array.Empty<AccessorySlot>();

        // Use the first variant with groups as the representative layout.
        // Most creatures use the same groups across all variants.
        var firstWithGroups = entry.AccessoryVariants
            .FirstOrDefault(v => v.AccessoryGroups.Count > 0);
        if (firstWithGroups == null)
            return Array.Empty<AccessorySlot>();

        var slots = new List<AccessorySlot>();
        foreach (var g in firstWithGroups.AccessoryGroups)
        {
            var slot = GroupNameToSlot(g);
            if (slot.HasValue && !slots.Contains(slot.Value))
                slots.Add(slot.Value);
        }

        return slots.ToArray();
    }

    /// <summary>
    /// Returns the localisation key for the label of a given AccessorySlot.
    /// </summary>
    public static string GetSlotLabelLocKey(AccessorySlot slot)
    {
        return slot switch
        {
            AccessorySlot.Right => "companion.accessory_slot_right",
            AccessorySlot.Left => "companion.accessory_slot_left",
            AccessorySlot.Front => "companion.accessory_slot_front",
            AccessorySlot.Back => "companion.accessory_slot_back",
            _ => "companion.accessory_slot_right",
        };
    }

    /// <summary>
    /// Loads accessory data from Companion Accessories.json.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            var loaded = new List<CompanionAccessoryEntry>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                loaded.Add(new CompanionAccessoryEntry
                {
                    Id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "",
                    Name = elem.TryGetProperty("Name", out var nP) ? nP.GetString() ?? "" : "",
                    NameLocStr = elem.TryGetProperty("Name_LocStr", out var nlP) ? nlP.GetString() : null,
                    Descriptor = elem.TryGetProperty("Descriptor", out var dP) ? dP.GetString() : null,
                    LinkedProduct = elem.TryGetProperty("LinkedProduct", out var lP) ? lP.GetString() : null,
                });
            }

            if (loaded.Count > 0)
            {
                var list = (List<CompanionAccessoryEntry>)Entries;
                list.Clear();
                list.AddRange(loaded);

                ById.Clear();
                foreach (var e in loaded) ById[e.Id] = e;
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Applies localisation to accessory names.</summary>
    public static int ApplyLocalisation(LocalisationService service)
    {
        if (!service.IsActive) { RevertLocalisation(); return 0; }

        int count = 0;
        foreach (var entry in Entries)
        {
            if (!_englishNameBackup.ContainsKey(entry.Id))
                _englishNameBackup[entry.Id] = entry.Name;

            entry.Name = _englishNameBackup[entry.Id];

            if (!string.IsNullOrEmpty(entry.NameLocStr))
            {
                var loc = service.Lookup(entry.NameLocStr);
                if (loc != null) { entry.Name = loc; count++; }
            }
        }
        return count;
    }

    /// <summary>Reverts accessory names to English.</summary>
    public static void RevertLocalisation()
    {
        foreach (var kvp in _englishNameBackup)
        {
            if (ById.TryGetValue(kvp.Key, out var entry))
                entry.Name = kvp.Value;
        }
        _englishNameBackup.Clear();
    }
}

// ===================================================
// Pet Battle Moves (from Pet Battle Moves.json)
// Needs further processing to resolve display strings
// ===================================================

/// <summary>Represents a single phase within a pet battle move.</summary>
public class PetBattleMovePhase
{
    /// <summary>Phase strength level (e.g. "VeryLight", "Medium").</summary>
    public string Strength { get; set; } = "";
    /// <summary>Visual effect type (e.g. "Projectile", "Beam").</summary>
    public string Effect { get; set; } = "";

    /// <summary>Normalised display string for Strength, using localisation if available.</summary>
    public string StrengthDisplay => GetLocalisedStrength(Strength);
    /// <summary>Normalised display string for Effect, using localisation if available.</summary>
    public string EffectDisplay => GetLocalisedEffect(Effect);

    /// <summary>Returns an emoji matching the effect type for visual display.</summary>
    public string EffectEmoji => GetEffectEmoji(Effect);

    /// <summary>Resolves a strength raw value to a localised display string.</summary>
    internal static string GetLocalisedStrength(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string suffix = raw.ToLowerInvariant() switch
        {
            "verylight" => "very_light",
            "light" => "light",
            "medium" => "medium",
            "heavy" => "heavy",
            _ => "",
        };
        if (string.IsNullOrEmpty(suffix)) return StringHelper.NormalizeDisplayString(raw);
        string key = "companion.battle_val_strength_" + suffix;
        return UiStrings.GetOrNull(key) ?? StringHelper.NormalizeDisplayString(raw);
    }

    /// <summary>Resolves an effect raw value to a localised display string.</summary>
    internal static string GetLocalisedEffect(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        string suffix = raw.ToLowerInvariant() switch
        {
            "buff" => "buff",
            "damagenoprojectile" => "damage_no_projectile",
            "debuff" => "debuff",
            "dotdamage" => "dot_damage",
            "heal" => "heal",
            "projectile" => "projectile",
            "shield" => "shield",
            "stun" => "stun",
            _ => "",
        };
        if (string.IsNullOrEmpty(suffix)) return StringHelper.NormalizeDisplayString(raw);
        string key = "companion.battle_val_effect_" + suffix;
        return UiStrings.GetOrNull(key) ?? (raw == "DoTDamage" ? "Damage over Time" : StringHelper.NormalizeDisplayString(raw));
    }

    /// <summary>Returns an emoji matching the effect type for visual display.</summary>
    internal static string GetEffectEmoji(string effect)
    {
        return effect?.ToLowerInvariant() switch
        {
			// These really should change to UI elements,
			// just need time to find and add them to NMSE.Extractor
            "projectile" => "🏹",
            "damagenoprojectile" => "⚔️",
            "dotdamage" => "🔥",
            "heal" => "💚",
            "buff" => "🔺",
            "debuff" => "🔻",
            "shield" => "🛡️",
            "stun" => "⚡",
            "" or null => "",
            _ => "",
        };
    }
}

/// <summary>Represents a pet battle move from Pet Battle Moves.json.</summary>
public class PetBattleMoveEntry
{
    /// <summary>Unique move identifier (e.g. "ATTACK_NORM").</summary>
    public string Id { get; set; } = "";
    /// <summary>Human-readable description of the move.</summary>
    public string DebugDescription { get; set; } = "";
    /// <summary>Primary target type (e.g. "ActiveEnemy", "Self").</summary>
    public string Target { get; set; } = "";
    /// <summary>Whether this is a multi-turn move.</summary>
    public bool MultiTurnMove { get; set; }
    /// <summary>Whether this is a basic/default move.</summary>
    public bool BasicMove { get; set; }
    /// <summary>Icon style enum (e.g. "Attack", "Buff", "Heal").</summary>
    public string IconStyle { get; set; } = "";
    /// <summary>Internal name stub for loc key derivation.</summary>
    public string NameStub { get; set; } = "";
    /// <summary>Game localisation key for the stat this move describes (e.g. "UI_PB_STAT_ATTACK").</summary>
    public string? LocIDToDescribeStat { get; set; }
    /// <summary>Phases of this move.</summary>
    public IReadOnlyList<PetBattleMovePhase> Phases { get; set; } = Array.Empty<PetBattleMovePhase>();

    /// <summary>Normalised display string for Target, using localisation if available.</summary>
    public string TargetDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(Target)) return "";
            string suffix = Target.ToLowerInvariant() switch
            {
                "activeenemy" => "active_enemy",
                "self" => "self",
                _ => "",
            };
            if (string.IsNullOrEmpty(suffix)) return StringHelper.NormalizeDisplayString(Target);
            string key = "companion.battle_val_target_" + suffix;
            return UiStrings.GetOrNull(key) ?? StringHelper.NormalizeDisplayString(Target);
        }
    }

    /// <summary>Normalised display string for IconStyle, using localisation if available.</summary>
    public string IconStyleDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(IconStyle)) return "";
            string suffix = IconStyle.ToLowerInvariant() switch
            {
                "attack" => "attack",
                "cooldown" => "cooldown",
                "heal" => "heal",
                "power" => "power",
                _ => "",
            };
            if (string.IsNullOrEmpty(suffix)) return StringHelper.NormalizeDisplayString(IconStyle);
            string key = "companion.battle_val_type_" + suffix;
            return UiStrings.GetOrNull(key) ?? StringHelper.NormalizeDisplayString(IconStyle);
        }
    }

    /// <summary>
    /// Returns an emoji/symbol matching the icon style.
    /// </summary>
    public string IconEmoji => IconStyle?.ToLowerInvariant() switch
    {
		// Yeah, emojis - I know... leave me alone lol.
		// These really should change to UI elements,
		// just need time to find and add them to NMSE.Extractor
		"attack" => "⚔️",
        "buff" => "🛡️",
        "heal" => "💚",
        "debuff" => "💀",
        "speed" => "⚡",
        "dot" => "🔥",
        "special" => "✨",
        "shield" => "🛡️",
        "none" or "" or null => "",
        _ => "❓",
    };

    /// <summary>ComboBox display: "ID - Description".</summary>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(DebugDescription))
            return Id;
        // Capitalise first letter of description
        string desc = char.ToUpperInvariant(DebugDescription[0]) + DebugDescription[1..];
        return $"{Id} - {desc}";
    }
}

/// <summary>Static database of pet battle moves, loaded from Pet Battle Moves.json.</summary>
public static class PetBattleMoveDatabase
{
    /// <summary>All loaded battle moves.</summary>
    public static readonly IReadOnlyList<PetBattleMoveEntry> Moves = new List<PetBattleMoveEntry>();

    /// <summary>Lookup dictionary mapping move ID to entry.</summary>
    public static readonly Dictionary<string, PetBattleMoveEntry> ById =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads move data from Pet Battle Moves.json.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            var loaded = new List<PetBattleMoveEntry>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var phases = new List<PetBattleMovePhase>();
                if (elem.TryGetProperty("Phases", out var phaseArr) && phaseArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ph in phaseArr.EnumerateArray())
                    {
                        phases.Add(new PetBattleMovePhase
                        {
                            Strength = ph.TryGetProperty("Strength", out var sP) ? sP.GetString() ?? "" : "",
                            Effect = ph.TryGetProperty("Effect", out var eP) ? eP.GetString() ?? "" : "",
                        });
                    }
                }

                loaded.Add(new PetBattleMoveEntry
                {
                    Id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "",
                    DebugDescription = elem.TryGetProperty("DebugDescription", out var ddP) ? ddP.GetString() ?? "" : "",
                    Target = elem.TryGetProperty("Target", out var tP) ? tP.GetString() ?? "" : "",
                    MultiTurnMove = elem.TryGetProperty("MultiTurnMove", out var mtP) && mtP.ValueKind == JsonValueKind.True,
                    BasicMove = elem.TryGetProperty("BasicMove", out var bmP) && bmP.ValueKind == JsonValueKind.True,
                    IconStyle = elem.TryGetProperty("IconStyle", out var isP) ? isP.GetString() ?? "" : "",
                    NameStub = elem.TryGetProperty("NameStub", out var nsP) ? nsP.GetString() ?? "" : "",
                    LocIDToDescribeStat = elem.TryGetProperty("LocIDToDescribeStat", out var lsP) ? lsP.GetString() : null,
                    Phases = phases,
                });
            }

            if (loaded.Count > 0)
            {
                var list = (List<PetBattleMoveEntry>)Moves;
                list.Clear();
                list.AddRange(loaded);

                ById.Clear();
                foreach (var m in loaded) ById[m.Id] = m;
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}

// ===================================================
// Pet Battle Movesets (from Pet Battle Movesets.json)
// These also need further processing to resolve
// display strings to a more useful format
// ===================================================

/// <summary>A single allowed move option within a moveset slot.</summary>
public class PetBattleMoveSlotOption
{
    /// <summary>Move template ID (e.g. "ATTACK_AFF").</summary>
    public string Template { get; set; } = "";
    /// <summary>Minimum cooldown turns.</summary>
    public int CooldownMin { get; set; }
    /// <summary>Maximum cooldown turns.</summary>
    public int CooldownMax { get; set; }
    /// <summary>Relative weighting for random selection.</summary>
    public double Weighting { get; set; }
}

/// <summary>One of 5 move slots in a moveset, containing the allowed move options.</summary>
public class PetBattleMoveSlot
{
    /// <summary>Slot number (1-5).</summary>
    public int SlotNumber { get; set; }
    /// <summary>Allowed move options for this slot.</summary>
    public IReadOnlyList<PetBattleMoveSlotOption> Options { get; set; } = Array.Empty<PetBattleMoveSlotOption>();
}

/// <summary>Represents a pet battle moveset from Pet Battle Movesets.json.</summary>
public class PetBattleMovesetEntry
{
    /// <summary>Moveset identifier (e.g. "BASIC", "DOT_BOMBER").</summary>
    public string Id { get; set; } = "";
    /// <summary>The 5 move slots for this moveset.</summary>
    public IReadOnlyList<PetBattleMoveSlot> Slots { get; set; } = Array.Empty<PetBattleMoveSlot>();

    /// <summary>
    /// Localised display name. Looks up "companion.battle_moveset_{id_lower}" first,
    /// falling back to NormalizeDisplayString if no loc key exists.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(Id)) return "";
            string key = "companion.battle_moveset_" + Id.ToLowerInvariant().Replace('_', '_');
            return UiStrings.GetOrNull(key) ?? StringHelper.NormalizeDisplayString(Id);
        }
    }

    public override string ToString() => DisplayName;
}

/// <summary>Static database of pet battle movesets, loaded from Pet Battle Movesets.json.</summary>
public static class PetBattleMovesetDatabase
{
    /// <summary>All loaded movesets.</summary>
    public static readonly IReadOnlyList<PetBattleMovesetEntry> Movesets = new List<PetBattleMovesetEntry>();

    /// <summary>Lookup dictionary mapping moveset ID to entry.</summary>
    public static readonly Dictionary<string, PetBattleMovesetEntry> ById =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the allowed move IDs for a specific slot in a given moveset.
    /// </summary>
    public static IReadOnlyList<string> GetAllowedMovesForSlot(string movesetId, int slotNumber)
    {
        if (!ById.TryGetValue(movesetId, out var entry)) return Array.Empty<string>();
        var slot = entry.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
        return slot?.Options.Select(o => o.Template).ToList() ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>
    /// Finds which moveset(s) a given move belongs to (across all slots).
    /// </summary>
    public static IReadOnlyList<PetBattleMovesetEntry> FindMovesetsContainingMove(string moveId)
    {
        return Movesets.Where(ms =>
            ms.Slots.Any(slot =>
                slot.Options.Any(o => string.Equals(o.Template, moveId, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }

    /// <summary>
    /// Loads moveset data from Pet Battle Movesets.json.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            var loaded = new List<PetBattleMovesetEntry>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var slots = new List<PetBattleMoveSlot>();
                if (elem.TryGetProperty("Slots", out var slotsArr) && slotsArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var slotElem in slotsArr.EnumerateArray())
                    {
                        int slotNum = slotElem.TryGetProperty("Slot", out var snP) && snP.TryGetInt32(out int sn) ? sn : 0;
                        var options = new List<PetBattleMoveSlotOption>();
                        if (slotElem.TryGetProperty("Options", out var optsArr) && optsArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var optElem in optsArr.EnumerateArray())
                            {
                                options.Add(new PetBattleMoveSlotOption
                                {
                                    Template = optElem.TryGetProperty("Template", out var tP) ? tP.GetString() ?? "" : "",
                                    CooldownMin = optElem.TryGetProperty("CooldownMin", out var cMinP) && cMinP.TryGetInt32(out int cMin) ? cMin : 0,
                                    CooldownMax = optElem.TryGetProperty("CooldownMax", out var cMaxP) && cMaxP.TryGetInt32(out int cMax) ? cMax : 0,
                                    Weighting = optElem.TryGetProperty("Weighting", out var wP) && wP.TryGetDouble(out double w) ? w : 0.0,
                                });
                            }
                        }
                        slots.Add(new PetBattleMoveSlot { SlotNumber = slotNum, Options = options });
                    }
                }

                loaded.Add(new PetBattleMovesetEntry
                {
                    Id = elem.TryGetProperty("Id", out var idP) ? idP.GetString() ?? "" : "",
                    Slots = slots,
                });
            }

            if (loaded.Count > 0)
            {
                var list = (List<PetBattleMovesetEntry>)Movesets;
                list.Clear();
                list.AddRange(loaded);

                ById.Clear();
                foreach (var ms in loaded) ById[ms.Id] = ms;
            }

            return loaded.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}

// ===========================================================
// Pet Biome Affinity Map (from Game Table Globals.json)
// This should move to an extracted JSON file when time allows
// ===========================================================

/// <summary>
/// Maps biome types to pet battler affinities and their localisation keys.
/// Loaded from Game Table Globals.json.
/// </summary>
public static class PetBiomeAffinityMap
{
    /// <summary>Biome -> Affinity mapping (e.g. "Scorched" -> "Fire").</summary>
    private static readonly Dictionary<string, string> _biomeToAffinity = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Affinity -> Game loc key mapping (e.g. "Fire" -> "UI_PB_AFFINITY_HOT").</summary>
    private static readonly Dictionary<string, string> _affinityLoc = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Affinity -> Short stub mapping (e.g. "Fire" -> "HOT").</summary>
    private static readonly Dictionary<string, string> _affinityLocStub = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Target -> Game loc key mapping (e.g. "ActiveEnemy" -> "UI_PB_MOVE_TARGET_ENEMY").</summary>
    private static readonly Dictionary<string, string> _targetLoc = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps UI biome strings to game-correct affinity names.
    /// The biome to affinity data uses lookup/temp names (e.g. "Cold", "Lush", "Barren")
    /// but the in-game affinity names differ (e.g. "Frost", "Tropical", "Desert").
    /// This dictionary provides the corrected game names for display instead of using the MXML values.
    /// </summary>
    private static readonly Dictionary<string, string> _affinityGameNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Toxic", "Toxic" },
        { "Radioactive", "Radioactive" },
        { "Fire", "Fire" },
        { "Cold", "Frost" },
        { "Frozen", "Frost" },
        { "Lush", "Tropical" },
        { "Barren", "Desert" },
        { "Weird", "Anomalous" },
        { "Mech", "Mechanical" },
        { "Normal", "Normal" },
    };

    /// <summary>
    /// Weak/strong type matchup data. Each affinity maps to a tuple of
    /// (WeakAgainst, StrongAgainst) affinity lists using game-correct names.
    /// </summary>
    private static readonly Dictionary<string, (string[] Weak, string[] Strong)> _affinityMatchups
        = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Toxic", (new[] { "Desert", "Frost" }, new[] { "Tropical", "Radioactive" }) },
        { "Desert", (new[] { "Tropical", "Mechanical" }, new[] { "Toxic", "Fire" }) },
        { "Frost", (new[] { "Radioactive", "Fire" }, new[] { "Toxic", "Mechanical" }) },
        { "Anomalous", (new[] { "Fire", "Tropical" }, new[] { "Radioactive", "Mechanical" }) },
        { "Mechanical", (new[] { "Frost", "Anomalous" }, new[] { "Desert", "Tropical" }) },
        { "Tropical", (new[] { "Toxic", "Mechanical" }, new[] { "Desert", "Anomalous" }) },
        { "Radioactive", (new[] { "Toxic", "Anomalous" }, new[] { "Fire", "Frost" }) },
        { "Fire", (new[] { "Desert", "Radioactive" }, new[] { "Frost", "Anomalous" }) },
    };

    /// <summary>
    /// Resolves a biome type to its pet battler affinity.
    /// Returns empty string if not found.
    /// </summary>
    public static string BiomeToAffinity(string biome)
    {
        if (string.IsNullOrEmpty(biome)) return "";
        return _biomeToAffinity.TryGetValue(biome, out var aff) ? aff : "";
    }

    /// <summary>
    /// Gets the game localisation key for an affinity type.
    /// Returns null if not found.
    /// </summary>
    public static string? GetAffinityLocKey(string affinity)
    {
        if (string.IsNullOrEmpty(affinity)) return null;
        return _affinityLoc.TryGetValue(affinity, out var key) ? key : null;
    }

    /// <summary>
    /// Returns an emoji matching the affinity type, for visual consistency with move type icons.
    /// Accepts both JSON-sourced names (e.g. "Cold", "Lush") and game-correct names
    /// (e.g. "Frost", "Tropical").
    /// </summary>
    public static string GetAffinityEmoji(string affinity)
    {
        return affinity?.ToLowerInvariant() switch
        {
			// Updated with the new affinity name lookup and glyph vs unicode ID.
			// These really should change to UI elements,
			// just need time to find and add them to NMSE.Extractor
			"toxic" => "☠️",
            "radioactive" => "☢️",
            "fire" => "🔥",
            "cold" or "frozen" or "frost" => "❄️",
            "lush" or "tropical" => "🌿",
            "barren" or "desert" => "☀️",
            "weird" or "anomalous" => "🔮",
            "mech" or "mechanical" => "⚙️",
            "normal" => "⭐",
            _ => "",
        };
    }

    /// <summary>
    /// Resolves a JSON affinity name to its game-correct display name.
    /// For example: "Cold" becomes "Frost", "Lush" becomes "Tropical".
    /// If the affinity is already a game-correct name or unrecognised, returns it unchanged.
    /// </summary>
    public static string GetAffinityGameName(string affinity)
    {
        if (string.IsNullOrEmpty(affinity)) return "";
        return _affinityGameNames.TryGetValue(affinity, out var gameName) ? gameName : affinity;
    }

    /// <summary>
    /// Returns a localised display name for an affinity value, prefixed with an emoji.
    /// Uses the game-correct affinity name (e.g. "Frost" instead of "Cold") and
    /// resolves a loc key for the translated name.
    /// </summary>
    public static string GetAffinityDisplayName(string affinity, LocalisationService? service = null)
    {
        if (string.IsNullOrEmpty(affinity)) return "";

        string gameName = GetAffinityGameName(affinity);
        string emoji = GetAffinityEmoji(gameName);
        string displayName = GetLocalisedAffinityName(gameName);
        return !string.IsNullOrEmpty(emoji) ? $"{emoji}{displayName}" : displayName;
    }

    /// <summary>
    /// Returns the weak/strong matchup data for a game-correct affinity name.
    /// The returned tuple contains (Weak, Strong) arrays of game-correct affinity names.
    /// Returns null if no matchup data exists (e.g. for "Normal").
    /// </summary>
    public static (string[] Weak, string[] Strong)? GetAffinityMatchup(string gameAffinity)
    {
        if (string.IsNullOrEmpty(gameAffinity)) return null;
        return _affinityMatchups.TryGetValue(gameAffinity, out var matchup) ? matchup : null;
    }

    /// <summary>
    /// Formats a list of affinity names with their emoji prefixes, joined by a separator.
    /// Uses localised display names for the affinity values.
    /// </summary>
    public static string FormatAffinityList(string[] affinities, string separator = ", ")
    {
        if (affinities == null || affinities.Length == 0) return "";
        var parts = new string[affinities.Length];
        for (int i = 0; i < affinities.Length; i++)
        {
            string emoji = GetAffinityEmoji(affinities[i]);
            string displayName = GetLocalisedAffinityName(affinities[i]);
            parts[i] = !string.IsNullOrEmpty(emoji) ? $"{emoji}{displayName}" : displayName;
        }
        return string.Join(separator, parts);
    }

    /// <summary>
    /// Resolves a game-correct affinity name to its localised display string.
    /// Falls back to the raw name if no loc key exists.
    /// </summary>
    public static string GetLocalisedAffinityName(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return "";
        string key = "companion.battle_val_affinity_" + gameName.ToLowerInvariant();
        return UiStrings.GetOrNull(key) ?? gameName;
    }

    /// <summary>
    /// Gets the game localisation key for a battle target type.
    /// Returns null if not found.
    /// </summary>
    public static string? GetTargetLocKey(string target)
    {
        if (string.IsNullOrEmpty(target)) return null;
        return _targetLoc.TryGetValue(target, out var key) && !string.IsNullOrEmpty(key) ? key : null;
    }

    /// <summary>Whether any data has been loaded.</summary>
    public static bool IsLoaded => _biomeToAffinity.Count > 0 || _affinityLoc.Count > 0;

    /// <summary>
    /// Loads from Game Table Globals.json. Expects a single-element array
    /// containing PetBiomeAffinities, PetAffinityLoc, PetAffinityLocStub, PetTargetLoc objects.
    /// </summary>
    public static bool LoadFromFile(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return false;

        try
        {
            var content = File.ReadAllBytes(jsonPath);
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return false;

            // Expect a single-element array wrapping all sections
            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Object) return false;

            LoadDictSection(first, "PetBiomeAffinities", _biomeToAffinity);
            LoadDictSection(first, "PetAffinityLoc", _affinityLoc);
            LoadDictSection(first, "PetAffinityLocStub", _affinityLocStub);
            LoadDictSection(first, "PetTargetLoc", _targetLoc);

            return _biomeToAffinity.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void LoadDictSection(JsonElement root, string propertyName, Dictionary<string, string> target)
    {
        target.Clear();
        if (!root.TryGetProperty(propertyName, out var section) || section.ValueKind != JsonValueKind.Object)
            return;

        foreach (var prop in section.EnumerateObject())
        {
            string val = prop.Value.GetString() ?? "";
            if (!string.IsNullOrEmpty(prop.Name))
                target[prop.Name] = val;
        }
    }
}
