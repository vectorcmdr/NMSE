using System.Text.Json;
using NMSE.Core;

namespace NMSE.Data;

/// <summary>Represents a companion creature entry with an ID and species name.</summary>
public class CompanionEntry
{
    /// <summary>Internal creature type identifier (e.g. "^QUAD_PET").</summary>
    public string Id { get; init; } = "";
    /// <summary>Display name for the species. May be blank if not yet populated from game data.</summary>
    public string Species { get; init; } = "";

    /// <summary>Returns a string representation using the species name if available, otherwise the ID.</summary>
    public override string ToString() => !string.IsNullOrEmpty(Species) ? Species : Id;
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
        "Swamp", "Lava", "Waterworld", "All"
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
        ["All"] = "companion.biome_all",
    };

    /// <summary>
    /// All known companion creature entries.
    /// Species names are intentionally blank for now; they need populating from game data
    /// later with user-friendly display names.
    /// </summary>
    public static readonly IReadOnlyList<CompanionEntry> Entries = new List<CompanionEntry>
    {
        new() { Id = "^ANTELOPE", Species = "Antelope" },
        new() { Id = "^ARTHROPOD", Species = "Arthropod" },
        new() { Id = "^BEETLE", Species = "Beetle" },
        new() { Id = "^FIENDFISHBIG", Species = "Big Fiend Fish" },
        new() { Id = "^BIRD", Species = "Bird" },
        new() { Id = "^BLOB", Species = "Blob" },
        new() { Id = "^BONECAT", Species = "Bone Cat" },
        new() { Id = "^BUGFIEND", Species = "Bug Fiend" },
        new() { Id = "^BUGQUEEN", Species = "Bug Queen" },
        new() { Id = "^BUTTERFLY", Species = "Butterfly" },
        new() { Id = "^BUTTERFLOCK", Species = "Butterfly Flock" },
        new() { Id = "^CAT", Species = "Cat" },
        new() { Id = "^COW", Species = "Cow" },
        new() { Id = "^CRAB", Species = "Crab" },
        new() { Id = "^DIGGER", Species = "Digger" },
        new() { Id = "^DRILL", Species = "Drill" },
        new() { Id = "^DRONE", Species = "Drone" },
        new() { Id = "^FIEND", Species = "Fiend" },
        new() { Id = "^FISH", Species = "Fish" },
        new() { Id = "^FISHFLOCK", Species = "Fish Flock" },
        new() { Id = "^FLOATSPIDER", Species = "Float Spider" },
        new() { Id = "^FLOATER", Species = "Floater" },
        new() { Id = "^FLYINGBEETLE", Species = "Flying Beetle" },
        new() { Id = "^FLYINGLIZARD", Species = "Flying Lizard" },
        new() { Id = "^FLYINGSNAKE", Species = "Flying Snake" },
        new() { Id = "^GRUNT", Species = "Grunt" },
        new() { Id = "^HOVER_PET", Species = "Hover Pet" },
        new() { Id = "^JELLYFISH", Species = "Jellyfish" },
        new() { Id = "^LAND_JELLYFISH", Species = "Land Jellyfish" },
        new() { Id = "^LARGEBUTTERFLY", Species = "Large Butterfly" },
        new() { Id = "^MINIDRONE", Species = "Mini Drone" },
        new() { Id = "^MINIFIEND", Species = "Mini Fiend" },
        new() { Id = "^MOLE", Species = "Mole" },
        new() { Id = "^PLANTCAT", Species = "Plant Cat" },
        new() { Id = "^PLOUGH", Species = "Plough" },
        new() { Id = "^PROTODIGGER", Species = "Proto Digger" },
        new() { Id = "^PROTOFLYER", Species = "Proto Flyer" },
        new() { Id = "^PROTOROLLER", Species = "Proto Roller" },
        new() { Id = "^QUAD", Species = "Quad" },
        new() { Id = "^QUAD_PET", Species = "Quad Pet" },
        new() { Id = "^QUADRUPED", Species = "Quadruped" },
        new() { Id = "^ROBO_PET", Species = "Robo Pet" },
        new() { Id = "^ROBOTANTELOPE", Species = "Robot Antelope" },
        new() { Id = "^ROCKCREATURE", Species = "Rock Creature" },
        new() { Id = "^RODENT", Species = "Rodent" },
        new() { Id = "^SANDWORM", Species = "Sandworm" },
        new() { Id = "^SCUTTLER", Species = "Scuttler" },
        new() { Id = "^SCUTTLER_PET", Species = "Scuttler Pet" },
        new() { Id = "^SEASNAKE", Species = "Sea Snake" },
        new() { Id = "^SHARK", Species = "Shark" },
        new() { Id = "^SIXLEGCAT", Species = "Six Leg Cat" },
        new() { Id = "^SIXLEGCOW", Species = "Six Leg Cow" },
        new() { Id = "^SLUG", Species = "Slug" },
        new() { Id = "^SMALLBIRD", Species = "Small Bird" },
        new() { Id = "^FIENDFISHSMALL", Species = "Small Fiend Fish" },
        new() { Id = "^SPACE_FLOATER", Species = "Space Floater" },
        new() { Id = "^SPIDER", Species = "Spider" },
        new() { Id = "^STRIDER", Species = "Strider" },
        new() { Id = "^SWIMCOW", Species = "Swim Cow" },
        new() { Id = "^SWIMRODENT", Species = "Swim Rodent" },
        new() { Id = "^TREX", Species = "T-Rex" },
        new() { Id = "^TRICERATOPS", Species = "Triceratops" },
        new() { Id = "^TWOLEGANTELOPE", Species = "Two Leg Antelope" },
        new() { Id = "^WALKER", Species = "Walker" },
        new() { Id = "^WALKINGBUILDING", Species = "Walking Building" },
        new() { Id = "^WEIRDBUTTERFLY", Species = "Weird Butterfly" },
        new() { Id = "^WEIRDCRYSTAL", Species = "Weird Crystal" },
        new() { Id = "^WEIRDFLOAT", Species = "Weird Floater" },
        new() { Id = "^WEIRDROLL", Species = "Weird Roller" },
    };

    /// <summary>Lookup dictionary mapping companion ID to its entry (case-insensitive).</summary>
    public static readonly Dictionary<string, CompanionEntry> ById =
        Entries.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Known creature archetype classifications.
    /// Matches the Creature Builder creature type list.
    /// </summary>
    public static readonly string[] CreatureTypes =
    {
        "None", "Antelope", "Bear", "Beetle", "Bird", "Blob", "Bones", "Brainless",
        "Butterfly", "Cat", "Cow", "Crab", "Digger", "Dino", "Drill", "Drone",
        "Fiend", "FiendFishBig", "FiendFishSmall", "Fish", "FishPredator", "FishPrey",
        "Floater", "FloatingGasbag", "FlyingBeetle", "FlyingLizard", "FlyingSnake",
        "GiantRobot", "Jellyfish", "MiniDrone", "MiniFiend", "Passive", "Pet",
        "PlayerPredator", "Plough", "PlowBig", "PlowSmall", "Predator", "Prey",
        "ProtoDigger", "ProtoFlyer", "ProtoRoller", "Protodigger", "Quad", "Robot",
        "RockCreature", "Rodent", "SandWorm", "Scuttler", "SeaSnake", "SeaWorm",
        "Shark", "Slug", "Snake", "Spider", "Strider", "Triceratops", "Tyrannosaurus",
        "Walker", "Weird"
    };
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

    // =======================================================================
    // Static creature part data
    // 55 creature types, 918 groups, 3056 descriptors - eventually these need
    // populating from game data, but for now they are hardcoded.
    // A problem for another time.
    // =======================================================================

    private static readonly List<CreaturePartEntry> AllEntries = new()
    {
        new() { CreatureId = "ANTELOPE", FriendlyName = "ANTELOPE", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIEN", Name = "_Head_Alien", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_2", Name = "_Head_2", Children = new()
                        {
                            new() { GroupId = "_HEADALT_", Descriptors = new()
                            {
                                new() { Id = "_HEADALT_1", Name = "_Headalt_1", Children = new()
                                {
                                    new() { GroupId = "_CENBASE_", Descriptors = new()
                                    {
                                        new() { Id = "_CENBASE_NONE", Name = "_CenBase_none" },
                                        new() { Id = "_CENBASE_1", Name = "_CenBase_1" },
                                        new() { Id = "_CENBASE_2", Name = "_CenBase_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEADALT_2", Name = "_Headalt_2", Children = new()
                                {
                                    new() { GroupId = "_BBALLS_", Descriptors = new()
                                    {
                                        new() { Id = "_BBALLS_1", Name = "_Bballs_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_1", Name = "_Head_1", Children = new()
                        {
                            new() { GroupId = "_ALTA_", Descriptors = new()
                            {
                                new() { Id = "_ALTA_1", Name = "_AltA_1", Children = new()
                                {
                                    new() { GroupId = "_CENTERA_", Descriptors = new()
                                    {
                                        new() { Id = "_CENTERA_1", Name = "_centerA_1" },
                                    }
                                    },
                                    new() { GroupId = "_BROWB_", Descriptors = new()
                                    {
                                        new() { Id = "_BROWB_1", Name = "_BrowB_1", Children = new()
                                        {
                                            new() { GroupId = "_EYESBA_", Descriptors = new()
                                            {
                                                new() { Id = "_EYESBA_1", Name = "_EyesBa_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_COLLAR_", Descriptors = new()
                                    {
                                        new() { Id = "_COLLAR_1", Name = "_Collar_1" },
                                    }
                                    },
                                    new() { GroupId = "_CENTFING_", Descriptors = new()
                                    {
                                        new() { Id = "_CENTFING_1", Name = "_CentFing_1" },
                                    }
                                    },
                                    new() { GroupId = "_ANTENNASA_", Descriptors = new()
                                    {
                                        new() { Id = "_ANTENNASA_1", Name = "_Antennasa_1" },
                                    }
                                    },
                                    new() { GroupId = "_RIMC_", Descriptors = new()
                                    {
                                        new() { Id = "_RIMC_1", Name = "_RimC_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_ALTA_2", Name = "_AltA_2", Children = new()
                                {
                                    new() { GroupId = "_NECKZ_", Descriptors = new()
                                    {
                                        new() { Id = "_NECKZ_1", Name = "_neckz_1", Children = new()
                                        {
                                            new() { GroupId = "_FINGERS_", Descriptors = new()
                                            {
                                                new() { Id = "_FINGERS_1", Name = "_Fingers_1" },
                                                new() { Id = "_FINGERS_NONE", Name = "_Fingers_none" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_ROUND_", Descriptors = new()
                                    {
                                        new() { Id = "_ROUND_2", Name = "_Round_2" },
                                        new() { Id = "_ROUND_1", Name = "_Round_1", Children = new()
                                        {
                                            new() { GroupId = "_EBS_", Descriptors = new()
                                            {
                                                new() { Id = "_EBS_NONE", Name = "_Ebs_none" },
                                                new() { Id = "_EBS_1", Name = "_Ebs_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUFF", Name = "_Head_Buff", Children = new()
                {
                    new() { GroupId = "_BHHORNS_", Descriptors = new()
                    {
                        new() { Id = "_BHHORNS_NULL", Name = "_BHHorns_NULL" },
                        new() { Id = "_BHHORNS_1", Name = "_BHHorns_1" },
                        new() { Id = "_BHHORNS_2", Name = "_BHHorns_2" },
                        new() { Id = "_BHHORNS_3", Name = "_BHHorns_3" },
                        new() { Id = "_BHHORNS_4", Name = "_BHHorns_4" },
                    }
                    },
                    new() { GroupId = "_BHNOSE_", Descriptors = new()
                    {
                        new() { Id = "_BHNOSE_2XRARE", Name = "_BHNose_2xRARE" },
                        new() { Id = "_BHNOSE_NULL", Name = "_BHNose_NULL" },
                    }
                    },
                    new() { GroupId = "_BHEARS_", Descriptors = new()
                    {
                        new() { Id = "_BHEARS_4", Name = "_BHEars_4" },
                        new() { Id = "_BHEARS_3", Name = "_BHEars_3" },
                        new() { Id = "_BHEARS_1N", Name = "_BHEars_1N" },
                    }
                    },
                    new() { GroupId = "_HDACC_", Descriptors = new()
                    {
                        new() { Id = "_HDACC_NONE", Name = "_HDAcc_none" },
                    }
                    },
                    new() { GroupId = "_HBTEETH_", Descriptors = new()
                    {
                        new() { Id = "_HBTEETH_1", Name = "_HBTeeth_1" },
                        new() { Id = "_HBTEETH_2", Name = "_HBTeeth_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_DEER", Name = "_Head_Deer", Children = new()
                {
                    new() { GroupId = "_HDTEETH_", Descriptors = new()
                    {
                        new() { Id = "_HDTEETH_1", Name = "_HDTeeth_1" },
                        new() { Id = "_HDTEETH_2", Name = "_HDTeeth_2" },
                    }
                    },
                    new() { GroupId = "_HDEARS_", Descriptors = new()
                    {
                        new() { Id = "_HDEARS_1", Name = "_HDEars_1" },
                        new() { Id = "_HDEARS_4", Name = "_HDEars_4" },
                        new() { Id = "_HDEARS_3", Name = "_HDEars_3" },
                        new() { Id = "_HDEARS_9", Name = "_HDEars_9" },
                        new() { Id = "_HDEARS_2XRARE", Name = "_HDEars_2xRARE" },
                    }
                    },
                    new() { GroupId = "_HDHORNS_", Descriptors = new()
                    {
                        new() { Id = "_HDHORNS_NULL", Name = "_HDHorns_NULL" },
                        new() { Id = "_HDHORNS_2", Name = "_HDHorns_2" },
                        new() { Id = "_HDHORNS_1", Name = "_HDHorns_1" },
                        new() { Id = "_HDHORNS_3", Name = "_HDHorns_3" },
                        new() { Id = "_HDHORNS_4", Name = "_HDHorns_4" },
                        new() { Id = "_HDHORNS_5", Name = "_HDHorns_5" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_GDANE", Name = "_Head_GDane", Children = new()
                {
                    new() { GroupId = "_GDPLATEGRP_", Descriptors = new()
                    {
                        new() { Id = "_GDPLATEGRP_1", Name = "_GDPlateGRP_1", Children = new()
                        {
                            new() { GroupId = "_GD1EAR_", Descriptors = new()
                            {
                                new() { Id = "_GD1EAR_1N", Name = "_GD1Ear_1N" },
                                new() { Id = "_GD1EAR_2N", Name = "_GD1Ear_2N" },
                                new() { Id = "_GD1EAR_NULL", Name = "_GD1Ear_NULL" },
                            }
                            },
                            new() { GroupId = "_GD1PLATE_", Descriptors = new()
                            {
                                new() { Id = "_GD1PLATE_3", Name = "_GD1Plate_3" },
                                new() { Id = "_GD1PLATE_2", Name = "_GD1Plate_2" },
                                new() { Id = "_GD1PLATE_1", Name = "_GD1Plate_1" },
                                new() { Id = "_GD1PLATE_NULL", Name = "_GD1Plate_NULL" },
                            }
                            },
                        }
                        },
                        new() { Id = "_GDPLATEGRP_2", Name = "_GDPlateGRP_2", Children = new()
                        {
                            new() { GroupId = "_GD2EAR_", Descriptors = new()
                            {
                                new() { Id = "_GD2EAR_7", Name = "_GD2Ear_7" },
                                new() { Id = "_GD2EAR_6", Name = "_GD2Ear_6" },
                                new() { Id = "_GD2EAR_8N", Name = "_GD2Ear_8N" },
                                new() { Id = "_GD2EAR_1N1", Name = "_GD2Ear_1N1" },
                                new() { Id = "_GD2EAR_2N1", Name = "_GD2Ear_2N1" },
                                new() { Id = "_GD2EAR_NULL", Name = "_GD2Ear_NULL" },
                            }
                            },
                            new() { GroupId = "_GD2PLATE_", Descriptors = new()
                            {
                                new() { Id = "_GD2PLATE_3N", Name = "_GD2Plate_3N" },
                                new() { Id = "_GD2PLATE_NULL", Name = "_GD2Plate_NULL" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_HGDTEETH_", Descriptors = new()
                    {
                        new() { Id = "_HGDTEETH_1", Name = "_HGDTeeth_1" },
                        new() { Id = "_HGDTEETH_2", Name = "_HGDTeeth_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LONG", Name = "_Head_Long", Children = new()
                {
                    new() { GroupId = "_HLACC1_", Descriptors = new()
                    {
                        new() { Id = "_HLACC1_E1", Name = "_HLAcc1_E1" },
                        new() { Id = "_HLACC1_E2", Name = "_HLAcc1_E2" },
                        new() { Id = "_HLACC1_E3", Name = "_HLAcc1_E3" },
                        new() { Id = "_HLACC1_E4", Name = "_HLAcc1_E4" },
                        new() { Id = "_HLACC1_H1", Name = "_HLAcc1_H1", Children = new()
                        {
                            new() { GroupId = "_HLA_", Descriptors = new()
                            {
                                new() { Id = "_HLA_HORN1", Name = "_HLA_Horn1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HLACC1_H2", Name = "_HLAcc1_H2", Children = new()
                        {
                            new() { GroupId = "_HLA_", Descriptors = new()
                            {
                                new() { Id = "_HLA_HORN2", Name = "_HLA_Horn2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HLACC1_H3", Name = "_HLAcc1_H3", Children = new()
                        {
                            new() { GroupId = "_HLA_", Descriptors = new()
                            {
                                new() { Id = "_HLA_HORN3", Name = "_HLA_Horn3" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HLACC1_H4", Name = "_HLAcc1_H4", Children = new()
                        {
                            new() { GroupId = "_HLA_", Descriptors = new()
                            {
                                new() { Id = "_HLA_HORN4", Name = "_HLA_Horn4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HLACC1_H5", Name = "_HLAcc1_H5", Children = new()
                        {
                            new() { GroupId = "_HLA_", Descriptors = new()
                            {
                                new() { Id = "_HLA_HORN5", Name = "_HLA_Horn5" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HLACC1_F1", Name = "_HLAcc1_F1" },
                        new() { Id = "_HLACC1_F2", Name = "_HLAcc1_F2" },
                        new() { Id = "_HLACC1_F3", Name = "_HLAcc1_F3" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_DEER", Name = "_Body_Deer", Children = new()
                {
                    new() { GroupId = "_DEERACC_", Descriptors = new()
                    {
                        new() { Id = "_DEERACC_NONE", Name = "_DeerAcc_none" },
                        new() { Id = "_DEERACC_1N2", Name = "_DeerAcc_1N2" },
                        new() { Id = "_DEERACC_2N2", Name = "_DeerAcc_2N2" },
                        new() { Id = "_DEERACC_3N2", Name = "_DeerAcc_3N2" },
                        new() { Id = "_DEERACC_4N2", Name = "_DeerAcc_4N2" },
                        new() { Id = "_DEERACC_5N2", Name = "_DeerAcc_5N2" },
                        new() { Id = "_DEERACC_6N2", Name = "_DeerAcc_6N2" },
                        new() { Id = "_DEERACC_22", Name = "_DeerAcc_22" },
                        new() { Id = "_DEERACC_23", Name = "_DeerAcc_23" },
                        new() { Id = "_DEERACC_24", Name = "_DeerAcc_24" },
                        new() { Id = "_DEERACC_25", Name = "_DeerAcc_25" },
                        new() { Id = "_DEERACC_2XRARE", Name = "_DeerAcc_2xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_FAT", Name = "_Body_Fat", Children = new()
                {
                    new() { GroupId = "_FATACC_", Descriptors = new()
                    {
                        new() { Id = "_FATACC_NULL", Name = "_FatAcc_NULL" },
                        new() { Id = "_FATACC_1N", Name = "_FatAcc_1N" },
                        new() { Id = "_FATACC_2N", Name = "_FatAcc_2N" },
                        new() { Id = "_FATACC_3N", Name = "_FatAcc_3N", Children = new()
                        {
                            new() { GroupId = "_FA3N_", Descriptors = new()
                            {
                                new() { Id = "_FA3N_1", Name = "_FA3N_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_FATACC_4N", Name = "_FatAcc_4N", Children = new()
                        {
                            new() { GroupId = "_FA4N_", Descriptors = new()
                            {
                                new() { Id = "_FA4N_1", Name = "_FA4N_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_FATACC_5N", Name = "_FatAcc_5N" },
                        new() { Id = "_FATACC_6N", Name = "_FatAcc_6N" },
                        new() { Id = "_FATACC_12OK", Name = "_FatAcc_12OK" },
                        new() { Id = "_FATACC_11OK", Name = "_FatAcc_11OK" },
                        new() { Id = "_FATACC_14OK", Name = "_FatAcc_14OK" },
                        new() { Id = "_FATACC_24OK", Name = "_FatAcc_24OK" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_SKINNY", Name = "_Body_Skinny", Children = new()
                {
                    new() { GroupId = "_SKINNYACC_", Descriptors = new()
                    {
                        new() { Id = "_SKINNYACC_NULL", Name = "_SkinnyAcc_NULL" },
                        new() { Id = "_SKINNYACC_1N1", Name = "_SkinnyAcc_1N1" },
                        new() { Id = "_SKINNYACC_2N1", Name = "_SkinnyAcc_2N1" },
                        new() { Id = "_SKINNYACC_3N1", Name = "_SkinnyAcc_3N1" },
                        new() { Id = "_SKINNYACC_4N1", Name = "_SkinnyAcc_4N1" },
                        new() { Id = "_SKINNYACC_5N1", Name = "_SkinnyAcc_5N1" },
                        new() { Id = "_SKINNYACC_6N1", Name = "_SkinnyAcc_6N1" },
                        new() { Id = "_SKINNYACC_12", Name = "_SkinnyAcc_12" },
                        new() { Id = "_SKINNYACC_14", Name = "_SkinnyAcc_14" },
                        new() { Id = "_SKINNYACC_20", Name = "_SkinnyAcc_20" },
                        new() { Id = "_SKINNYACC_21", Name = "_SkinnyAcc_21" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_ALIEN0", Name = "_Tail_Alien0" },
                new() { Id = "_TAIL_ALIEN1", Name = "_Tail_Alien1" },
                new() { Id = "_TAIL_ALIEN2", Name = "_Tail_Alien2" },
                new() { Id = "_TAIL_ALIEN3", Name = "_Tail_Alien3" },
                new() { Id = "_TAIL_ALIEN4", Name = "_Tail_Alien4" },
                new() { Id = "_TAIL_ALIEN5", Name = "_Tail_Alien5" },
            }
            },
        }
        },
        new() { CreatureId = "BIRD", FriendlyName = "BIRD", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_BAT", Name = "_Head_Bat", Children = new()
                {
                    new() { GroupId = "_HBAT_", Descriptors = new()
                    {
                        new() { Id = "_HBAT_0", Name = "_HBat_0" },
                        new() { Id = "_HBAT_1", Name = "_HBat_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BIRD", Name = "_Head_Bird", Children = new()
                {
                    new() { GroupId = "_HBACC_", Descriptors = new()
                    {
                        new() { Id = "_HBACC_1", Name = "_HBAcc_1" },
                        new() { Id = "_HBACC_2", Name = "_HBAcc_2" },
                        new() { Id = "_HBACC_3", Name = "_HBAcc_3" },
                        new() { Id = "_HBACC_4", Name = "_HBAcc_4" },
                        new() { Id = "_HBACC_5", Name = "_HBAcc_5" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_WINGS_", Descriptors = new()
            {
                new() { Id = "_WINGS_1", Name = "_Wings_1" },
                new() { Id = "_WINGS_2", Name = "_Wings_2", Children = new()
                {
                    new() { GroupId = "_W2ACC_", Descriptors = new()
                    {
                        new() { Id = "_W2ACC_0", Name = "_W2Acc_0" },
                        new() { Id = "_W2ACC_1", Name = "_W2Acc_1" },
                    }
                    },
                }
                },
                new() { Id = "_WINGS_3", Name = "_Wings_3" },
                new() { Id = "_WINGS_4", Name = "_Wings_4" },
                new() { Id = "_WINGS_5", Name = "_Wings_5" },
                new() { Id = "_WINGS_6", Name = "_Wings_6" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_BIRD", Name = "_Body_Bird" },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_BAT", Name = "_Tail_Bat" },
                new() { Id = "_TAIL_BIRD", Name = "_Tail_Bird" },
                new() { Id = "_TAIL_BUG", Name = "_Tail_Bug" },
                new() { Id = "_TAIL_LONG", Name = "_Tail_Long" },
                new() { Id = "_TAIL_THIN", Name = "_Tail_Thin", Children = new()
                {
                    new() { GroupId = "_TTHINACC_", Descriptors = new()
                    {
                        new() { Id = "_TTHINACC_0", Name = "_TThinAcc_0" },
                        new() { Id = "_TTHINACC_1", Name = "_TThinAcc_1" },
                        new() { Id = "_TTHINACC_2", Name = "_TThinAcc_2" },
                        new() { Id = "_TTHINACC_3", Name = "_TThinAcc_3" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "BIRDFLOCK", FriendlyName = "BIRDFLOCK", Details = new()
        {
            new() { GroupId = "_WEIRD_", Descriptors = new()
            {
                new() { Id = "_WEIRD_CRYSTAL", Name = "_Weird_Crystal" },
            }
            },
        }
        },
        new() { CreatureId = "BLOB", FriendlyName = "BLOB", Details = new()
        {
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_BLOB", Name = "_Body_Blob", Children = new()
                {
                    new() { GroupId = "_BOC_", Descriptors = new()
                    {
                        new() { Id = "_BOC_NULL2", Name = "_BOC_Null2" },
                        new() { Id = "_BOC_ARMTENT", Name = "_BOC_ArmTent" },
                    }
                    },
                    new() { GroupId = "_BOB_", Descriptors = new()
                    {
                        new() { Id = "_BOB_NULL2", Name = "_BOB_Null2" },
                        new() { Id = "_BOB_MHAWK", Name = "_BOB_Mhawk" },
                        new() { Id = "_BOB_GILLS", Name = "_BOB_Gills" },
                        new() { Id = "_BOB_BROWS", Name = "_BOB_brows" },
                        new() { Id = "_BOB_EYES2", Name = "_BOB_Eyes2", Children = new()
                        {
                            new() { GroupId = "_EYES2_", Descriptors = new()
                            {
                                new() { Id = "_EYES2_1", Name = "_eyes2_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BOB_FRILLFLAPS", Name = "_BOB_FrillFlaps" },
                        new() { Id = "_BOB_EARS", Name = "_BOB_Ears", Children = new()
                        {
                            new() { GroupId = "_STASHE_", Descriptors = new()
                            {
                                new() { Id = "_STASHE_1", Name = "_Stashe_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BOB_HARDSCALP", Name = "_BOB_HardScalp" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_EEL", Name = "_Body_Eel", Children = new()
                {
                    new() { GroupId = "_ACC_", Descriptors = new()
                    {
                        new() { Id = "_ACC_GILLS2", Name = "_Acc_Gills2" },
                        new() { Id = "_ACC_SNAILJAW", Name = "_Acc_SnailJaw" },
                        new() { Id = "_ACC_GILLS", Name = "_Acc_Gills" },
                        new() { Id = "_ACC_EARS", Name = "_Acc_Ears" },
                        new() { Id = "_ACC_EARFLAPS", Name = "_Acc_EarFlaps" },
                        new() { Id = "_ACC_FINS", Name = "_Acc_Fins" },
                        new() { Id = "_ACC_FRILLFLAPS", Name = "_Acc_FrillFlaps" },
                        new() { Id = "_ACC_FINS2", Name = "_Acc_Fins2" },
                        new() { Id = "_ACC_TENTACLES", Name = "_Acc_Tentacles" },
                    }
                    },
                    new() { GroupId = "_ACC2_", Descriptors = new()
                    {
                        new() { Id = "_ACC2_ANTENNAS", Name = "_Acc2_Antennas" },
                        new() { Id = "_ACC2_EELEYES", Name = "_Acc2_EelEyes" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_EYEBALLPLANT", Name = "_Body_EyeballPlant", Children = new()
                {
                    new() { GroupId = "_BEA_", Descriptors = new()
                    {
                        new() { Id = "_BEA_ARMTENT", Name = "_BEA_ArmTent" },
                        new() { Id = "_BEA_NULL", Name = "_BEA_Null" },
                    }
                    },
                    new() { GroupId = "_BE_", Descriptors = new()
                    {
                        new() { Id = "_BE_NULL", Name = "_BE_Null" },
                    }
                    },
                    new() { GroupId = "_PLANTTEETH_", Descriptors = new()
                    {
                        new() { Id = "_PLANTTEETH_3", Name = "_PlantTeeth_3" },
                        new() { Id = "_PLANTTEETH_2", Name = "_PlantTeeth_2" },
                        new() { Id = "_PLANTTEETH_4", Name = "_PlantTeeth_4", Children = new()
                        {
                            new() { GroupId = "_FLOWER_", Descriptors = new()
                            {
                                new() { Id = "_FLOWER_4", Name = "_Flower_4" },
                                new() { Id = "_FLOWER_3", Name = "_Flower_3" },
                            }
                            },
                            new() { GroupId = "_PLANT_", Descriptors = new()
                            {
                                new() { Id = "_PLANT_6", Name = "_Plant_6" },
                                new() { Id = "_PLANT_5", Name = "_Plant_5" },
                                new() { Id = "_PLANT_4", Name = "_Plant_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_PLANTTEETH_1", Name = "_PlantTeeth_1" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_JELLY", Name = "_Body_Jelly", Children = new()
                {
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_FINGERS", Name = "_Body_Fingers", Children = new()
                        {
                            new() { GroupId = "_EYESS_", Descriptors = new()
                            {
                                new() { Id = "_EYESS_1", Name = "_Eyess_1" },
                            }
                            },
                            new() { GroupId = "_HATS_", Descriptors = new()
                            {
                                new() { Id = "_HATS_1", Name = "_Hats_1" },
                                new() { Id = "_HATS_2", Name = "_Hats_2" },
                                new() { Id = "_HATS_NONE", Name = "_Hats_none" },
                            }
                            },
                            new() { GroupId = "_ACCX_", Descriptors = new()
                            {
                                new() { Id = "_ACCX_1", Name = "_AccX_1" },
                                new() { Id = "_ACCX_NONE", Name = "_AccX_none" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_BLOBBY", Name = "_Body_blobby", Children = new()
                        {
                            new() { GroupId = "_EYES_", Descriptors = new()
                            {
                                new() { Id = "_EYES_1", Name = "_Eyes_1" },
                                new() { Id = "_EYES_2", Name = "_Eyes_2" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_BODY_OCTO", Name = "_Body_Octo", Children = new()
                {
                    new() { GroupId = "_BOA2_", Descriptors = new()
                    {
                        new() { Id = "_BOA2_ARMTENT", Name = "_BOA2_ArmTent" },
                        new() { Id = "_BOA2_0", Name = "_BOA2_0" },
                    }
                    },
                    new() { GroupId = "_BOA_", Descriptors = new()
                    {
                        new() { Id = "_BOA_0", Name = "_BOA_0" },
                        new() { Id = "_BOA_HEAD", Name = "_BOA_Head", Children = new()
                        {
                            new() { GroupId = "_HAT_", Descriptors = new()
                            {
                                new() { Id = "_HAT_NONE", Name = "_HAT_none" },
                                new() { Id = "_HAT_ANTENNAS", Name = "_HAT_Antennas" },
                                new() { Id = "_HAT_1", Name = "_HAT_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BOA_GILLS", Name = "_BOA_Gills" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_PLANT", Name = "_Body_Plant", Children = new()
                {
                    new() { GroupId = "_FLOWER_", Descriptors = new()
                    {
                        new() { Id = "_FLOWER_2", Name = "_Flower_2" },
                        new() { Id = "_FLOWER_1", Name = "_Flower_1" },
                    }
                    },
                    new() { GroupId = "_PLANT_", Descriptors = new()
                    {
                        new() { Id = "_PLANT_3", Name = "_Plant_3" },
                        new() { Id = "_PLANT_1", Name = "_Plant_1" },
                        new() { Id = "_PLANT_2", Name = "_Plant_2" },
                    }
                    },
                    new() { GroupId = "_EYEBS_", Descriptors = new()
                    {
                        new() { Id = "_EYEBS_1", Name = "_Eyebs_1" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_SHROOM", Name = "_Body_Shroom", Children = new()
                {
                    new() { GroupId = "_BELLY_", Descriptors = new()
                    {
                        new() { Id = "_BELLY_1", Name = "_Belly_1" },
                    }
                    },
                    new() { GroupId = "_LEGS_", Descriptors = new()
                    {
                        new() { Id = "_LEGS_1", Name = "_Legs_1" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "BONECAT", FriendlyName = "BONECAT", Details = new()
        {
            new() { GroupId = "_BONECAT_", Descriptors = new()
            {
                new() { Id = "_BONECAT_", Name = "_BoneCat_", Children = new()
                {
                    new() { GroupId = "_TYPE_", Descriptors = new()
                    {
                        new() { Id = "_TYPE_ROUND", Name = "_Type_Round", Children = new()
                        {
                            new() { GroupId = "_ACC_", Descriptors = new()
                            {
                                new() { Id = "_ACC_SPIKES", Name = "_Acc_Spikes" },
                                new() { Id = "_ACC_CRYSTALS", Name = "_Acc_Crystals" },
                                new() { Id = "_ACC_NONE", Name = "_Acc_None" },
                            }
                            },
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_A", Name = "_Head_A", Children = new()
                                {
                                    new() { GroupId = "_EYES_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES_A", Name = "_Eyes_A" },
                                        new() { Id = "_EYES_B", Name = "_Eyes_B" },
                                    }
                                    },
                                    new() { GroupId = "_PLATE_", Descriptors = new()
                                    {
                                        new() { Id = "_PLATE_A", Name = "_Plate_A" },
                                        new() { Id = "_PLATE_FIN", Name = "_Plate_Fin" },
                                        new() { Id = "_PLATE_HORN", Name = "_Plate_Horn" },
                                        new() { Id = "_PLATE_HOLES", Name = "_Plate_Holes", Children = new()
                                        {
                                            new() { GroupId = "_HEADHOLES_", Descriptors = new()
                                            {
                                                new() { Id = "_HEADHOLES_SHROOM", Name = "_HeadHoles_Shroom" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_B", Name = "_Head_B", Children = new()
                                {
                                    new() { GroupId = "_EYESB_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESB_A", Name = "_EyesB_A" },
                                        new() { Id = "_EYESB_B", Name = "_EyesB_B" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_BODY_", Descriptors = new()
                            {
                                new() { Id = "_BODY_CURVESPIKE", Name = "_Body_CurveSpike" },
                                new() { Id = "_BODY_ROUNDA", Name = "_Body_RoundA", Children = new()
                                {
                                    new() { GroupId = "_BACK_", Descriptors = new()
                                    {
                                        new() { Id = "_BACK_PLATEHOLES", Name = "_Back_PlateHoles", Children = new()
                                        {
                                            new() { GroupId = "_BACKHOLES_", Descriptors = new()
                                            {
                                                new() { Id = "_BACKHOLES_WORM", Name = "_BackHoles_Worm" },
                                                new() { Id = "_BACKHOLES_CRYSTALSA", Name = "_BackHoles_CrystalsA" },
                                                new() { Id = "_BACKHOLES_SHROOMS", Name = "_BackHoles_Shrooms" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_BACK_PLATE", Name = "_Back_Plate" },
                                        new() { Id = "_BACK_PLATETALL", Name = "_Back_PlateTall" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_LIMBS_", Descriptors = new()
                            {
                                new() { Id = "_LIMBS_A", Name = "_Limbs_A" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "BUTTERFLY", FriendlyName = "BUTTERFLY", Details = new()
        {
            new() { GroupId = "_INSECT_", Descriptors = new()
            {
                new() { Id = "_INSECT_ALIEN", Name = "_Insect_Alien", Children = new()
                {
                    new() { GroupId = "_CWINGS_", Descriptors = new()
                    {
                        new() { Id = "_CWINGS_3", Name = "_CWings_3" },
                    }
                    },
                    new() { GroupId = "_CBODY_", Descriptors = new()
                    {
                        new() { Id = "_CBODY_1", Name = "_CBody_1" },
                        new() { Id = "_CBODY_2", Name = "_CBody_2" },
                    }
                    },
                }
                },
                new() { Id = "_INSECT_LONG", Name = "_Insect_Long", Children = new()
                {
                    new() { GroupId = "_BHEAD_", Descriptors = new()
                    {
                        new() { Id = "_BHEAD_1", Name = "_BHead_1" },
                        new() { Id = "_BHEAD_2", Name = "_BHead_2", Children = new()
                        {
                            new() { GroupId = "_BH2_", Descriptors = new()
                            {
                                new() { Id = "_BH2_0", Name = "_BH2_0" },
                                new() { Id = "_BH2_1", Name = "_BH2_1" },
                                new() { Id = "_BH2_2", Name = "_BH2_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BHEAD_3", Name = "_BHead_3", Children = new()
                        {
                            new() { GroupId = "_BH3_", Descriptors = new()
                            {
                                new() { Id = "_BH3_0", Name = "_BH3_0" },
                                new() { Id = "_BH3_1", Name = "_BH3_1" },
                                new() { Id = "_BH3_2", Name = "_BH3_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BHEAD_4", Name = "_BHead_4", Children = new()
                        {
                            new() { GroupId = "_BH4_", Descriptors = new()
                            {
                                new() { Id = "_BH4_0", Name = "_BH4_0" },
                                new() { Id = "_BH4_1", Name = "_BH4_1" },
                                new() { Id = "_BH4_2", Name = "_BH4_2" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_BWINGS_", Descriptors = new()
                    {
                        new() { Id = "_BWINGS_2", Name = "_BWings_2" },
                    }
                    },
                    new() { GroupId = "_BBODY_", Descriptors = new()
                    {
                        new() { Id = "_BBODY_1", Name = "_BBody_1", Children = new()
                        {
                            new() { GroupId = "_BB1_", Descriptors = new()
                            {
                                new() { Id = "_BB1_0", Name = "_BB1_0" },
                                new() { Id = "_BB1_1", Name = "_BB1_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_INSECT_SHORT", Name = "_Insect_Short", Children = new()
                {
                    new() { GroupId = "_ABODY_", Descriptors = new()
                    {
                        new() { Id = "_ABODY_1", Name = "_ABody_1", Children = new()
                        {
                            new() { GroupId = "_AB1_", Descriptors = new()
                            {
                                new() { Id = "_AB1_0", Name = "_AB1_0" },
                                new() { Id = "_AB1_1", Name = "_AB1_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ABODY_2", Name = "_ABody_2", Children = new()
                        {
                            new() { GroupId = "_AB2_", Descriptors = new()
                            {
                                new() { Id = "_AB2_0", Name = "_AB2_0" },
                                new() { Id = "_AB2_1", Name = "_AB2_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ABODY_3", Name = "_ABody_3", Children = new()
                        {
                            new() { GroupId = "_AB3_", Descriptors = new()
                            {
                                new() { Id = "_AB3_0", Name = "_AB3_0" },
                                new() { Id = "_AB3_1", Name = "_AB3_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ABODY_4", Name = "_ABody_4", Children = new()
                        {
                            new() { GroupId = "_AB4_", Descriptors = new()
                            {
                                new() { Id = "_AB4_0", Name = "_AB4_0" },
                                new() { Id = "_AB4_1", Name = "_AB4_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_AWINGS_", Descriptors = new()
                    {
                        new() { Id = "_AWINGS_1", Name = "_AWings_1" },
                    }
                    },
                    new() { GroupId = "_AHEAD_", Descriptors = new()
                    {
                        new() { Id = "_AHEAD_1", Name = "_AHead_1", Children = new()
                        {
                            new() { GroupId = "_AH1_", Descriptors = new()
                            {
                                new() { Id = "_AH1_0", Name = "_AH1_0" },
                                new() { Id = "_AH1_1", Name = "_AH1_1" },
                                new() { Id = "_AH1_2", Name = "_AH1_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_AHEAD_2", Name = "_AHead_2", Children = new()
                        {
                            new() { GroupId = "_AH2_", Descriptors = new()
                            {
                                new() { Id = "_AH2_0", Name = "_AH2_0" },
                                new() { Id = "_AH2_1", Name = "_AH2_1" },
                                new() { Id = "_AH2_2", Name = "_AH2_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_AHEAD_3", Name = "_AHead_3", Children = new()
                        {
                            new() { GroupId = "_AH3_", Descriptors = new()
                            {
                                new() { Id = "_AH3_0", Name = "_AH3_0" },
                                new() { Id = "_AH3_1", Name = "_AH3_1" },
                                new() { Id = "_AH3_2", Name = "_AH3_2" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "BUTTERFLYFLOCK", FriendlyName = "BUTTERFLYFLOCK", Details = new()
        {
            new() { GroupId = "_WEIRD_", Descriptors = new()
            {
                new() { Id = "_WEIRD_CRYSTAL", Name = "_Weird_Crystal" },
            }
            },
        }
        },
        new() { CreatureId = "CAT", FriendlyName = "CAT", Details = new()
        {
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_CAT", Name = "_Body_Cat" },
                new() { Id = "_BODY_CATHINDXRARE", Name = "_Body_CatHindxRARE" },
            }
            },
            new() { GroupId = "_SHAPE_", Descriptors = new()
            {
                new() { Id = "_SHAPE_1", Name = "_Shape_1", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_HOG", Name = "_Head_Hog", Children = new()
                        {
                            new() { GroupId = "_HHACCS_", Descriptors = new()
                            {
                                new() { Id = "_HHACCS_A", Name = "_HHAccs_A", Children = new()
                                {
                                    new() { GroupId = "_HHNOSE_", Descriptors = new()
                                    {
                                        new() { Id = "_HHNOSE_NULL", Name = "_HHNose_NULL" },
                                        new() { Id = "_HHNOSE_CXRARE", Name = "_HHNose_CxRARE" },
                                        new() { Id = "_HHNOSE_A", Name = "_HHNose_A" },
                                        new() { Id = "_HHNOSE_B", Name = "_HHNose_B" },
                                    }
                                    },
                                    new() { GroupId = "_HHEARS_", Descriptors = new()
                                    {
                                        new() { Id = "_HHEARS_A", Name = "_HHEars_A", Children = new()
                                        {
                                            new() { GroupId = "_EAR_", Descriptors = new()
                                            {
                                                new() { Id = "_EAR_1", Name = "_Ear_1" },
                                                new() { Id = "_EAR_2", Name = "_Ear_2" },
                                                new() { Id = "_EAR_3", Name = "_Ear_3" },
                                                new() { Id = "_EAR_4", Name = "_Ear_4" },
                                            }
                                            },
                                            new() { GroupId = "_HORNB_", Descriptors = new()
                                            {
                                                new() { Id = "_HORNB_0", Name = "_HornB_0" },
                                                new() { Id = "_HORNB_1", Name = "_HornB_1" },
                                                new() { Id = "_HORNB_5", Name = "_HornB_5" },
                                                new() { Id = "_HORNB_6", Name = "_HornB_6" },
                                                new() { Id = "_HORNB_7", Name = "_HornB_7" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_HHEARS_B", Name = "_HHEars_B", Children = new()
                                        {
                                            new() { GroupId = "_HORNC_", Descriptors = new()
                                            {
                                                new() { Id = "_HORNC_1", Name = "_HornC_1" },
                                                new() { Id = "_HORNC_2", Name = "_HornC_2" },
                                                new() { Id = "_HORNC_0", Name = "_HornC_0" },
                                            }
                                            },
                                            new() { GroupId = "_EARB_", Descriptors = new()
                                            {
                                                new() { Id = "_EARB_1", Name = "_Earb_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HHACCS_B", Name = "_HHAccs_B", Children = new()
                                {
                                    new() { GroupId = "_HORN_", Descriptors = new()
                                    {
                                        new() { Id = "_HORN_0", Name = "_Horn_0" },
                                        new() { Id = "_HORN_2", Name = "_Horn_2" },
                                        new() { Id = "_HORN_3", Name = "_Horn_3" },
                                        new() { Id = "_HORN_1", Name = "_Horn_1" },
                                        new() { Id = "_HORN_4", Name = "_Horn_4" },
                                    }
                                    },
                                    new() { GroupId = "_HHTOP_", Descriptors = new()
                                    {
                                        new() { Id = "_HHTOP_G", Name = "_HHToP_G" },
                                        new() { Id = "_HHTOP_0", Name = "_HHToP_0" },
                                    }
                                    },
                                    new() { GroupId = "_HHBEAR_", Descriptors = new()
                                    {
                                        new() { Id = "_HHBEAR_C", Name = "_HHBEar_C" },
                                        new() { Id = "_HHBEAR_D", Name = "_HHBEar_D" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_HHTUSK_", Descriptors = new()
                            {
                                new() { Id = "_HHTUSK_1", Name = "_HHTusk_1" },
                                new() { Id = "_HHTUSK_2", Name = "_HHTusk_2" },
                                new() { Id = "_HHTUSK_3", Name = "_HHTusk_3" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_CAT", Name = "_Head_Cat", Children = new()
                        {
                            new() { GroupId = "_HCACC_", Descriptors = new()
                            {
                                new() { Id = "_HCACC_A", Name = "_HCAcc_A", Children = new()
                                {
                                    new() { GroupId = "_HCNOSE_", Descriptors = new()
                                    {
                                        new() { Id = "_HCNOSE_NULL", Name = "_HCNose_NULL" },
                                        new() { Id = "_HCNOSE_BXRARE", Name = "_HCNose_BxRARE" },
                                    }
                                    },
                                    new() { GroupId = "_CHEARS_", Descriptors = new()
                                    {
                                        new() { Id = "_CHEARS_F", Name = "_CHEars_F" },
                                        new() { Id = "_CHEARS_E", Name = "_CHEars_E" },
                                        new() { Id = "_CHEARS_D", Name = "_CHEars_D" },
                                        new() { Id = "_CHEARS_C", Name = "_CHEars_C" },
                                        new() { Id = "_CHEARS_C1", Name = "_CHEars_C1" },
                                        new() { Id = "_CHEARS_C2", Name = "_CHEars_C2" },
                                        new() { Id = "_CHEARS_B", Name = "_CHEars_B" },
                                        new() { Id = "_CHEARS_A", Name = "_CHEars_A" },
                                    }
                                    },
                                    new() { GroupId = "_HCTOP_", Descriptors = new()
                                    {
                                        new() { Id = "_HCTOP_NULL", Name = "_HCTop_NULL" },
                                        new() { Id = "_HCTOP_A", Name = "_HCTop_A" },
                                        new() { Id = "_HCTOP_B", Name = "_HCTop_B" },
                                        new() { Id = "_HCTOP_C", Name = "_HCTop_C" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HCACC_B", Name = "_HCAcc_B", Children = new()
                                {
                                    new() { GroupId = "_CATTOP_", Descriptors = new()
                                    {
                                        new() { Id = "_CATTOP_2XRARE", Name = "_CatTop_2xRARE" },
                                        new() { Id = "_CATTOP_1", Name = "_CatTop_1", Children = new()
                                        {
                                            new() { GroupId = "_HCTOPB_", Descriptors = new()
                                            {
                                                new() { Id = "_HCTOPB_NULL", Name = "_HCTopB_NULL" },
                                                new() { Id = "_HCTOPB_B", Name = "_HCTopB_B" },
                                                new() { Id = "_HCTOPB_C", Name = "_HCTopB_C" },
                                                new() { Id = "_HCTOPB_D", Name = "_HCTopB_D" },
                                                new() { Id = "_HCTOPB_E", Name = "_HCTopB_E" },
                                                new() { Id = "_HCTOPB_F", Name = "_HCTopB_F" },
                                                new() { Id = "_HCTOPB_G", Name = "_HCTopB_G" },
                                            }
                                            },
                                            new() { GroupId = "_CHEARS2_", Descriptors = new()
                                            {
                                                new() { Id = "_CHEARS2_C5", Name = "_CHEars2_C5" },
                                                new() { Id = "_CHEARS2_B1", Name = "_CHEars2_B1" },
                                                new() { Id = "_CHEARS2_C3", Name = "_CHEars2_C3" },
                                                new() { Id = "_CHEARS2_C4", Name = "_CHEars2_C4" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_WOLF", Name = "_Head_Wolf", Children = new()
                        {
                            new() { GroupId = "_HWACC_", Descriptors = new()
                            {
                                new() { Id = "_HWACC_A", Name = "_HWAcc_A", Children = new()
                                {
                                    new() { GroupId = "_WOLFTOP_", Descriptors = new()
                                    {
                                        new() { Id = "_WOLFTOP_1XRARE", Name = "_WolfTop_1xRARE" },
                                        new() { Id = "_WOLFTOP_2", Name = "_WolfTop_2", Children = new()
                                        {
                                            new() { GroupId = "_HWTOP_", Descriptors = new()
                                            {
                                                new() { Id = "_HWTOP_NULL", Name = "_HWTop_NULL" },
                                                new() { Id = "_HWTOP_B", Name = "_HWTop_B" },
                                                new() { Id = "_HWTOP_C", Name = "_HWTop_C" },
                                                new() { Id = "_HWTOP_D", Name = "_HWTop_D" },
                                                new() { Id = "_HWTOP_E", Name = "_HWTop_E" },
                                                new() { Id = "_HWTOP_F", Name = "_HWTop_F" },
                                            }
                                            },
                                            new() { GroupId = "_HWEARS_", Descriptors = new()
                                            {
                                                new() { Id = "_HWEARS_D3", Name = "_HWEars_D3" },
                                                new() { Id = "_HWEARS_D2", Name = "_HWEars_D2" },
                                                new() { Id = "_HWEARS_D1", Name = "_HWEars_D1" },
                                                new() { Id = "_HWEARS_E1", Name = "_HWEars_E1" },
                                                new() { Id = "_HWEARS_F1", Name = "_HWEars_F1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HWACC_B", Name = "_HWAcc_B", Children = new()
                                {
                                    new() { GroupId = "_HWMIDDLE_", Descriptors = new()
                                    {
                                        new() { Id = "_HWMIDDLE_NULL", Name = "_HWMiddle_NULL" },
                                        new() { Id = "_HWMIDDLE_A", Name = "_HWMiddle_A" },
                                        new() { Id = "_HWMIDDLE_B", Name = "_HWMiddle_B" },
                                        new() { Id = "_HWMIDDLE_C", Name = "_HWMiddle_C" },
                                    }
                                    },
                                    new() { GroupId = "_HWNOSE_", Descriptors = new()
                                    {
                                        new() { Id = "_HWNOSE_NULL", Name = "_HWNose_NULL" },
                                        new() { Id = "_HWNOSE_AXRARE", Name = "_HWNose_AxRARE" },
                                        new() { Id = "_HWNOSE_BXRARE", Name = "_HWNose_BxRARE" },
                                    }
                                    },
                                    new() { GroupId = "_HWEARS_", Descriptors = new()
                                    {
                                        new() { Id = "_HWEARS_NULL", Name = "_HWEars_NULL" },
                                        new() { Id = "_HWEARS_D5", Name = "_HWEars_D5" },
                                        new() { Id = "_HWEARS_D4", Name = "_HWEars_D4" },
                                        new() { Id = "_HWEARS_A", Name = "_HWEars_A" },
                                        new() { Id = "_HWEARS_B", Name = "_HWEars_B" },
                                        new() { Id = "_HWEARS_D", Name = "_HWEars_D" },
                                        new() { Id = "_HWEARS_E", Name = "_HWEars_E" },
                                        new() { Id = "_HWEARS_F", Name = "_HWEars_F" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_LIZARD", Name = "_Head_Lizard", Children = new()
                        {
                            new() { GroupId = "_HLACC_", Descriptors = new()
                            {
                                new() { Id = "_HLACC_NULLXRARE", Name = "_HLAcc_NULLxRARE" },
                                new() { Id = "_HLACC_A", Name = "_HLAcc_A", Children = new()
                                {
                                    new() { GroupId = "_HLMIDDLE_", Descriptors = new()
                                    {
                                        new() { Id = "_HLMIDDLE_NULL", Name = "_HLMiddle_NULL" },
                                        new() { Id = "_HLMIDDLE_A", Name = "_HLMiddle_A" },
                                        new() { Id = "_HLMIDDLE_B", Name = "_HLMiddle_B" },
                                        new() { Id = "_HLMIDDLE_C", Name = "_HLMiddle_C" },
                                    }
                                    },
                                    new() { GroupId = "_HLEARS_", Descriptors = new()
                                    {
                                        new() { Id = "_HLEARS_NULL", Name = "_HLEars_NULL" },
                                        new() { Id = "_HLEARS_A", Name = "_HLEars_A" },
                                        new() { Id = "_HLEARS_B", Name = "_HLEars_B" },
                                        new() { Id = "_HLEARS_C", Name = "_HLEars_C" },
                                        new() { Id = "_HLEARS_D", Name = "_HLEars_D" },
                                        new() { Id = "_HLEARS_E", Name = "_HLEars_E" },
                                    }
                                    },
                                    new() { GroupId = "_LHNOSE_", Descriptors = new()
                                    {
                                        new() { Id = "_LHNOSE_NULL", Name = "_LHNose_NULL" },
                                        new() { Id = "_LHNOSE_AXRARE", Name = "_LHNose_AxRARE" },
                                        new() { Id = "_LHNOSE_CXRARE", Name = "_LHNose_CxRARE" },
                                        new() { Id = "_LHNOSE_BXRARE", Name = "_LHNose_BxRARE" },
                                        new() { Id = "_LHNOSE_DXRARE", Name = "_LHNose_DxRARE" },
                                        new() { Id = "_LHNOSE_EXRARE", Name = "_LHNose_ExRARE" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HLACC_B", Name = "_HLAcc_B", Children = new()
                                {
                                    new() { GroupId = "_HLTOP_", Descriptors = new()
                                    {
                                        new() { Id = "_HLTOP_A", Name = "_HLTop_A" },
                                        new() { Id = "_HLTOP_2XRARE", Name = "_HLTop_2xRARE" },
                                        new() { Id = "_HLTOP_B", Name = "_HLTop_B" },
                                        new() { Id = "_HLTOP_C", Name = "_HLTop_C" },
                                        new() { Id = "_HLTOP_D", Name = "_HLTop_D" },
                                        new() { Id = "_HLTOP_E", Name = "_HLTop_E" },
                                        new() { Id = "_HLTOP_F", Name = "_HLTop_F" },
                                        new() { Id = "_HLTOP_G", Name = "_HLTop_G" },
                                        new() { Id = "_HLTOP_H", Name = "_HLTop_H" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_ALIEN", Name = "_Head_Alien", Children = new()
                        {
                            new() { GroupId = "_VAR_", Descriptors = new()
                            {
                                new() { Id = "_VAR_2", Name = "_Var_2", Children = new()
                                {
                                    new() { GroupId = "_TOUNGEIN_", Descriptors = new()
                                    {
                                        new() { Id = "_TOUNGEIN_1", Name = "_Toungein_1" },
                                    }
                                    },
                                    new() { GroupId = "_EYEBROW_", Descriptors = new()
                                    {
                                        new() { Id = "_EYEBROW_2", Name = "_Eyebrow_2" },
                                        new() { Id = "_EYEBROW_1", Name = "_Eyebrow_1", Children = new()
                                        {
                                            new() { GroupId = "_EEYEBLZ_", Descriptors = new()
                                            {
                                                new() { Id = "_EEYEBLZ_1", Name = "_Eeyeblz_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_VAR_1", Name = "_Var_1", Children = new()
                                {
                                    new() { GroupId = "_SCALP_", Descriptors = new()
                                    {
                                        new() { Id = "_SCALP_1", Name = "_Scalp_1", Children = new()
                                        {
                                            new() { GroupId = "_SOCKETS_", Descriptors = new()
                                            {
                                                new() { Id = "_SOCKETS_1", Name = "_Sockets_1", Children = new()
                                                {
                                                    new() { GroupId = "_EARGR_", Descriptors = new()
                                                    {
                                                        new() { Id = "_EARGR_2", Name = "_eargr_2" },
                                                        new() { Id = "_EARGR_1", Name = "_eargr_1" },
                                                    }
                                                    },
                                                }
                                                },
                                                new() { Id = "_SOCKETS_NONE", Name = "_Sockets_none" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_TOPGUM_", Descriptors = new()
                                    {
                                        new() { Id = "_TOPGUM_1", Name = "_Topgum_1", Children = new()
                                        {
                                            new() { GroupId = "_ANTENNAS_", Descriptors = new()
                                            {
                                                new() { Id = "_ANTENNAS_NONE", Name = "_Antennas_none" },
                                                new() { Id = "_ANTENNAS_1", Name = "_Antennas_1" },
                                            }
                                            },
                                            new() { GroupId = "_JAWGUM_", Descriptors = new()
                                            {
                                                new() { Id = "_JAWGUM_1", Name = "_Jawgum_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_TOPGUM_2", Name = "_Topgum_2", Children = new()
                                        {
                                            new() { GroupId = "_UPPERGUM_", Descriptors = new()
                                            {
                                                new() { Id = "_UPPERGUM_1", Name = "_UpperGum_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_CHEEKS_", Descriptors = new()
                                    {
                                        new() { Id = "_CHEEKS_1", Name = "_Cheeks_1", Children = new()
                                        {
                                            new() { GroupId = "_INCHEEKS_", Descriptors = new()
                                            {
                                                new() { Id = "_INCHEEKS_1", Name = "_inCheeks_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CHEEKS_NONE", Name = "_Cheeks_none" },
                                        new() { Id = "_CHEEKS_2", Name = "_Cheeks_2" },
                                    }
                                    },
                                    new() { GroupId = "_SIDES_", Descriptors = new()
                                    {
                                        new() { Id = "_SIDES_1", Name = "_Sides_1", Children = new()
                                        {
                                            new() { GroupId = "_BROWS_", Descriptors = new()
                                            {
                                                new() { Id = "_BROWS_1", Name = "_Brows_1", Children = new()
                                                {
                                                    new() { GroupId = "_EYEBALLS_", Descriptors = new()
                                                    {
                                                        new() { Id = "_EYEBALLS_1", Name = "_Eyeballs_1" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_SIDES_2", Name = "_Sides_2", Children = new()
                                        {
                                            new() { GroupId = "_EYESA_", Descriptors = new()
                                            {
                                                new() { Id = "_EYESA_1", Name = "_EyesA_1" },
                                                new() { Id = "_EYESA_2", Name = "_EyesA_2", Children = new()
                                                {
                                                    new() { GroupId = "_EBALLSB_", Descriptors = new()
                                                    {
                                                        new() { Id = "_EBALLSB_1", Name = "_EballsB_1" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_TAIL_", Descriptors = new()
                    {
                        new() { Id = "_TAIL_LIZ", Name = "_Tail_Liz" },
                        new() { Id = "_TAIL_HOG", Name = "_Tail_Hog", Children = new()
                        {
                            new() { GroupId = "_THA_", Descriptors = new()
                            {
                                new() { Id = "_THA_X1XRARE", Name = "_THA_X1xRARE" },
                                new() { Id = "_THA_X2", Name = "_THA_X2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_CAT", Name = "_Tail_Cat" },
                        new() { Id = "_TAIL_WOLF", Name = "_Tail_Wolf" },
                    }
                    },
                    new() { GroupId = "_MESH_", Descriptors = new()
                    {
                        new() { Id = "_MESH_CAT", Name = "_Mesh_Cat", Children = new()
                        {
                            new() { GroupId = "_BCA_", Descriptors = new()
                            {
                                new() { Id = "_BCA_NULL", Name = "_BCA_NULL" },
                                new() { Id = "_BCA_3", Name = "_BCA_3" },
                                new() { Id = "_BCA_13", Name = "_BCA_13" },
                                new() { Id = "_BCA_11", Name = "_BCA_11" },
                                new() { Id = "_BCA_4", Name = "_BCA_4" },
                                new() { Id = "_BCA_14", Name = "_BCA_14" },
                                new() { Id = "_BCA_X6", Name = "_BCA_X6" },
                                new() { Id = "_BCA_X7", Name = "_BCA_X7" },
                                new() { Id = "_BCA_12", Name = "_BCA_12", Children = new()
                                {
                                    new() { GroupId = "_EXT_", Descriptors = new()
                                    {
                                        new() { Id = "_EXT_A", Name = "_Ext_A" },
                                        new() { Id = "_EXT_NULL", Name = "_Ext_NULL" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BCA_9", Name = "_BCA_9" },
                                new() { Id = "_BCA_10", Name = "_BCA_10" },
                                new() { Id = "_BCA_8", Name = "_BCA_8" },
                                new() { Id = "_BCA_5", Name = "_BCA_5", Children = new()
                                {
                                    new() { GroupId = "_SHELLACC_", Descriptors = new()
                                    {
                                        new() { Id = "_SHELLACC_1", Name = "_ShellAcc_1" },
                                        new() { Id = "_SHELLACC_2", Name = "_ShellAcc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BCA_X1", Name = "_BCA_X1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_MESH_WOLF", Name = "_Mesh_Wolf", Children = new()
                        {
                            new() { GroupId = "_BCW_", Descriptors = new()
                            {
                                new() { Id = "_BCW_NULL", Name = "_BCW_NULL" },
                                new() { Id = "_BCW_X8", Name = "_BCW_X8" },
                                new() { Id = "_BCW_1", Name = "_BCW_1" },
                                new() { Id = "_BCW_2", Name = "_BCW_2" },
                                new() { Id = "_BCW_3", Name = "_BCW_3", Children = new()
                                {
                                    new() { GroupId = "_EXT1_", Descriptors = new()
                                    {
                                        new() { Id = "_EXT1_MINE", Name = "_Ext1_Mine" },
                                        new() { Id = "_EXT1_0", Name = "_Ext1_0" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BCW_4", Name = "_BCW_4" },
                                new() { Id = "_BCW_5", Name = "_BCW_5" },
                                new() { Id = "_BCW_7", Name = "_BCW_7" },
                                new() { Id = "_BCW_X9", Name = "_BCW_X9" },
                                new() { Id = "_BCW_11", Name = "_BCW_11" },
                                new() { Id = "_BCW_12", Name = "_BCW_12" },
                                new() { Id = "_BCW_X14", Name = "_BCW_X14" },
                                new() { Id = "_BCW_6", Name = "_BCW_6" },
                                new() { Id = "_BCW_10", Name = "_BCW_10" },
                                new() { Id = "_BCW_15", Name = "_BCW_15", Children = new()
                                {
                                    new() { GroupId = "_SHELLACC4_", Descriptors = new()
                                    {
                                        new() { Id = "_SHELLACC4_1", Name = "_ShellAcc4_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_MESH_LIZ", Name = "_Mesh_Liz", Children = new()
                        {
                            new() { GroupId = "_BLBACK_", Descriptors = new()
                            {
                                new() { Id = "_BLBACK_NULL", Name = "_BLBack_NULL" },
                                new() { Id = "_BLBACK_1", Name = "_BLBack_1" },
                                new() { Id = "_BLBACK_2", Name = "_BLBack_2" },
                                new() { Id = "_BLBACK_3", Name = "_BLBack_3" },
                                new() { Id = "_BLBACK_4", Name = "_BLBack_4" },
                                new() { Id = "_BLBACK_5", Name = "_BLBack_5" },
                                new() { Id = "_BLBACK_6", Name = "_BLBack_6", Children = new()
                                {
                                    new() { GroupId = "_EXT2_", Descriptors = new()
                                    {
                                        new() { Id = "_EXT2_NULL", Name = "_Ext2_NULL" },
                                        new() { Id = "_EXT2_1", Name = "_Ext2_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BLBACK_7", Name = "_BLBack_7" },
                                new() { Id = "_BLBACK_8", Name = "_BLBack_8" },
                                new() { Id = "_BLBACK_9", Name = "_BLBack_9" },
                                new() { Id = "_BLBACK_10", Name = "_BLBack_10" },
                                new() { Id = "_BLBACK_X11", Name = "_BLBack_X11" },
                                new() { Id = "_BLBACK_X12", Name = "_BLBack_X12" },
                                new() { Id = "_BLBACK_13", Name = "_BLBack_13", Children = new()
                                {
                                    new() { GroupId = "_SHELLACC3_", Descriptors = new()
                                    {
                                        new() { Id = "_SHELLACC3_1", Name = "_ShellAcc3_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BLBACK_SPINSXRARE", Name = "_BLBack_SpinsxRARE" },
                                new() { Id = "_BLBACK_SPINSLOTSXRARE", Name = "_BLBack_SpinsLotsxRARE" },
                                new() { Id = "_BLBACK_XTRIPLEFINXRARE", Name = "_BLBack_XTripleFinxRARE" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_SHAPE_XRARE", Name = "_Shape_xRARE", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_PREDATOR", Name = "_Head_Predator" },
                    }
                    },
                    new() { GroupId = "_TAIL_", Descriptors = new()
                    {
                        new() { Id = "_TAIL_PREDATOR", Name = "_Tail_Predator" },
                    }
                    },
                    new() { GroupId = "_MESH_", Descriptors = new()
                    {
                        new() { Id = "_MESH_PREDATOR", Name = "_Mesh_Predator" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "COW", FriendlyName = "COW", Details = new()
        {
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_ALIEN", Name = "_Tail_Alien", Children = new()
                {
                    new() { GroupId = "_TAACC_", Descriptors = new()
                    {
                        new() { Id = "_TAACC_1", Name = "_TAacc_1" },
                        new() { Id = "_TAACC_0", Name = "_TAacc_0" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_COW", Name = "_Tail_Cow" },
                new() { Id = "_TAIL_THIN", Name = "_Tail_Thin" },
                new() { Id = "_TAIL_TURTLE", Name = "_Tail_Turtle" },
            }
            },
            new() { GroupId = "_COW_", Descriptors = new()
            {
                new() { Id = "_COW_FLOATXRARE", Name = "_Cow_FloatxRARE", Children = new()
                {
                    new() { GroupId = "_WINGS_", Descriptors = new()
                    {
                        new() { Id = "_WINGS_A", Name = "_Wings_A" },
                    }
                    },
                }
                },
                new() { Id = "_COW_HINDXRARE", Name = "_Cow_HindxRARE" },
                new() { Id = "_COW_NORMAL", Name = "_Cow_Normal" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_COW", Name = "_Body_Cow", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK1", Name = "_BCA_Blank1" },
                        new() { Id = "_BCA_COWHUMP", Name = "_BCA_CowHump", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_BLANKYO3", Name = "_Ext_BlankYo3" },
                                new() { Id = "_EXT_BACKSPINESXRARE", Name = "_Ext_BackSpinesxRARE" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_ROCKSXRARE", Name = "_BCA_RocksxRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE", Name = "_Ext_Mine" },
                                new() { Id = "_EXT_BLANKYO", Name = "_Ext_BlankYo" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_BACKSPINESXRARE", Name = "_BCA_BackSpinesxRARE" },
                        new() { Id = "_BCA_STEGSPIKESXRARE", Name = "_BCA_StegSpikesxRARE" },
                        new() { Id = "_BCA_TURTSHELLXRARE", Name = "_BCA_TurtShellxRARE" },
                        new() { Id = "_BCA_BACKFINXRARE", Name = "_BCA_BackFinxRARE" },
                        new() { Id = "_BCA_LUMPXRARE", Name = "_BCA_LumpxRARE" },
                    }
                    },
                    new() { GroupId = "_TYPE_", Descriptors = new()
                    {
                        new() { Id = "_TYPE_COW", Name = "_Type_Cow", Children = new()
                        {
                            new() { GroupId = "_ACC_", Descriptors = new()
                            {
                                new() { Id = "_ACC_NONE", Name = "_Acc_none" },
                                new() { Id = "_ACC_FEATHERS", Name = "_Acc_Feathers" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TYPE_ORBCOW", Name = "_Type_OrbCow", Children = new()
                        {
                            new() { GroupId = "_ORBS_", Descriptors = new()
                            {
                                new() { Id = "_ORBS_1", Name = "_Orbs_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TYPE_TREXFEET", Name = "_Type_TRexFeet", Children = new()
                        {
                            new() { GroupId = "_FEET_", Descriptors = new()
                            {
                                new() { Id = "_FEET_1", Name = "_Feet_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TYPE_SPLITCOW", Name = "_Type_SplitCow" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_ROCK", Name = "_Body_Rock", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK2", Name = "_BCA_Blank2" },
                    }
                    },
                    new() { GroupId = "_TYPE_", Descriptors = new()
                    {
                        new() { Id = "_TYPE_ROCK", Name = "_Type_Rock" },
                        new() { Id = "_TYPE_EQLEGS", Name = "_Type_EqLegs" },
                    }
                    },
                    new() { GroupId = "_BRA_", Descriptors = new()
                    {
                        new() { Id = "_BRA_ROCKS1XRARE", Name = "_BRA_Rocks1xRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE2", Name = "_Ext_Mine2" },
                                new() { Id = "_EXT_BLANKYO2", Name = "_Ext_BlankYo2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BRA_ARMSPINESXRARE", Name = "_BRA_ArmSpinesxRARE" },
                        new() { Id = "_BRA_BACKSPINESXRARE", Name = "_BRA_BackSpinesxRARE" },
                        new() { Id = "_BRA_STEGSPIKESXRARE", Name = "_BRA_StegSpikesxRARE" },
                        new() { Id = "_BRA_TURTSHELLXRARE", Name = "_BRA_TurtShellxRARE" },
                        new() { Id = "_BRA_BACKFINXRARE", Name = "_BRA_BackFinxRARE" },
                        new() { Id = "_BRA_LUMP1XRARE", Name = "_BRA_Lump1xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIENBIRD", Name = "_Head_AlienBird", Children = new()
                {
                    new() { GroupId = "_HABACC_", Descriptors = new()
                    {
                        new() { Id = "_HABACC_BLANK", Name = "_HABAcc_Blank" },
                        new() { Id = "_HABACC_1", Name = "_HABAcc_1" },
                        new() { Id = "_HABACC_2", Name = "_HABAcc_2" },
                        new() { Id = "_HABACC_3", Name = "_HABAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_ALIENTYPE", Name = "_Head_AlienType", Children = new()
                {
                    new() { GroupId = "_WEIRD_", Descriptors = new()
                    {
                        new() { Id = "_WEIRD_CONNECT", Name = "_Weird_Connect", Children = new()
                        {
                            new() { GroupId = "_SHAPEN_", Descriptors = new()
                            {
                                new() { Id = "_SHAPEN_1", Name = "_Shapen_1", Children = new()
                                {
                                    new() { GroupId = "_FACESH_", Descriptors = new()
                                    {
                                        new() { Id = "_FACESH_1", Name = "_FaceSH_1", Children = new()
                                        {
                                            new() { GroupId = "_EARSSH1_", Descriptors = new()
                                            {
                                                new() { Id = "_EARSSH1_A", Name = "_EarsSH1_A" },
                                                new() { Id = "_EARSSH1_B", Name = "_EarsSH1_B" },
                                                new() { Id = "_EARSSH1_NONE", Name = "_EarsSH1_None" },
                                            }
                                            },
                                            new() { GroupId = "_EYESSH1_", Descriptors = new()
                                            {
                                                new() { Id = "_EYESSH1_A", Name = "_EyesSH1_A" },
                                                new() { Id = "_EYESSH1_C", Name = "_EyesSH1_C" },
                                                new() { Id = "_EYESSH1_D", Name = "_EyesSH1_D" },
                                                new() { Id = "_EYESSH1_E", Name = "_EyesSH1_E" },
                                                new() { Id = "_EYESSH1_F", Name = "_EyesSH1_F" },
                                                new() { Id = "_EYESSH1_G", Name = "_EyesSH1_G" },
                                                new() { Id = "_EYESSH1_H", Name = "_EyesSH1_H" },
                                                new() { Id = "_EYESSH1_NONE", Name = "_EyesSH1_None" },
                                                new() { Id = "_EYESSH1_I", Name = "_EyesSH1_I" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_NOSE1_", Descriptors = new()
                                    {
                                        new() { Id = "_NOSE1_A", Name = "_Nose1_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_FLARED", Name = "_Weird_Flared", Children = new()
                        {
                            new() { GroupId = "_FRUITF_", Descriptors = new()
                            {
                                new() { Id = "_FRUITF_NONE", Name = "_FruitF_None" },
                                new() { Id = "_FRUITF_A", Name = "_FruitF_A" },
                            }
                            },
                            new() { GroupId = "_FLARETOP_", Descriptors = new()
                            {
                                new() { Id = "_FLARETOP_4", Name = "_FlareTop_4" },
                                new() { Id = "_FLARETOP_2", Name = "_FlareTop_2", Children = new()
                                {
                                    new() { GroupId = "_EYES2_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES2_A", Name = "_Eyes2_A" },
                                        new() { Id = "_EYES2_B", Name = "_Eyes2_B" },
                                        new() { Id = "_EYES2_NONE", Name = "_Eyes2_None" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_FLARETOP_1", Name = "_FlareTop_1" },
                                new() { Id = "_FLARETOP_NONE", Name = "_FlareTop_None" },
                                new() { Id = "_FLARETOP_3", Name = "_FlareTop_3", Children = new()
                                {
                                    new() { GroupId = "_EYES3_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES3_A", Name = "_Eyes3_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_EYESF_", Descriptors = new()
                            {
                                new() { Id = "_EYESF_NONE", Name = "_EyesF_None" },
                                new() { Id = "_EYESF_A", Name = "_EyesF_A" },
                                new() { Id = "_EYESF_B", Name = "_EyesF_B" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_LONG", Name = "_Weird_Long", Children = new()
                        {
                            new() { GroupId = "_FACELONG_", Descriptors = new()
                            {
                                new() { Id = "_FACELONG_1", Name = "_FaceLong_1", Children = new()
                                {
                                    new() { GroupId = "_EARSL1_", Descriptors = new()
                                    {
                                        new() { Id = "_EARSL1_A", Name = "_EarsL1_A" },
                                        new() { Id = "_EARSL1_B", Name = "_EarsL1_B" },
                                        new() { Id = "_EARSL1_NONE", Name = "_EarsL1_None" },
                                    }
                                    },
                                    new() { GroupId = "_EYESL1_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESL1_A", Name = "_EyesL1_A" },
                                        new() { Id = "_EYESL1_B", Name = "_EyesL1_B" },
                                        new() { Id = "_EYESL1_C", Name = "_EyesL1_C" },
                                        new() { Id = "_EYESL1_D", Name = "_EyesL1_D" },
                                        new() { Id = "_EYESL1_NONE", Name = "_EyesL1_None" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_FACELONG_2", Name = "_FaceLong_2", Children = new()
                                {
                                    new() { GroupId = "_EYESL2_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESL2_A", Name = "_EyesL2_A" },
                                        new() { Id = "_EYESL2_B", Name = "_EyesL2_B" },
                                        new() { Id = "_EYESL2_C", Name = "_EyesL2_C" },
                                        new() { Id = "_EYESL2_D", Name = "_EyesL2_D" },
                                        new() { Id = "_EYESL2_NONE", Name = "_EyesL2_None" },
                                    }
                                    },
                                    new() { GroupId = "_EARSL2_", Descriptors = new()
                                    {
                                        new() { Id = "_EARSL2_A", Name = "_EarsL2_A" },
                                        new() { Id = "_EARSL2_NONE", Name = "_EarsL2_None" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_STUMP", Name = "_Weird_Stump", Children = new()
                        {
                            new() { GroupId = "_FACESTUMP_", Descriptors = new()
                            {
                                new() { Id = "_FACESTUMP_1", Name = "_FaceStump_1", Children = new()
                                {
                                    new() { GroupId = "_EYESS1_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESS1_A", Name = "_EyesS1_A" },
                                        new() { Id = "_EYESS1_B", Name = "_EyesS1_B" },
                                        new() { Id = "_EYESS1_C", Name = "_EyesS1_C" },
                                        new() { Id = "_EYESS1_D", Name = "_EyesS1_D" },
                                        new() { Id = "_EYESS1_E", Name = "_EyesS1_E" },
                                        new() { Id = "_EYESS1_F", Name = "_EyesS1_F" },
                                        new() { Id = "_EYESS1_G", Name = "_EyesS1_G" },
                                        new() { Id = "_EYESS1_H", Name = "_EyesS1_H" },
                                        new() { Id = "_EYESS1_I", Name = "_EyesS1_I" },
                                        new() { Id = "_EYESS1_NONE", Name = "_EyesS1_None" },
                                    }
                                    },
                                    new() { GroupId = "_EARSS1_", Descriptors = new()
                                    {
                                        new() { Id = "_EARSS1_A", Name = "_EarsS1_A" },
                                        new() { Id = "_EARSS1_B", Name = "_EarsS1_B" },
                                        new() { Id = "_EARSS1_C", Name = "_EarsS1_C" },
                                        new() { Id = "_EARSS1_D", Name = "_EarsS1_D" },
                                        new() { Id = "_EARSS1_E", Name = "_EarsS1_E" },
                                        new() { Id = "_EARSS1_NONE", Name = "_EarsS1_None" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL1", Name = "_Weird_HTail1", Children = new()
                        {
                            new() { GroupId = "_EYETAIL1_", Descriptors = new()
                            {
                                new() { Id = "_EYETAIL1_A", Name = "_EyeTail1_A" },
                                new() { Id = "_EYETAIL1_B", Name = "_EyeTail1_B" },
                                new() { Id = "_EYETAIL1_C", Name = "_EyeTail1_C" },
                                new() { Id = "_EYETAIL1_D", Name = "_EyeTail1_D" },
                                new() { Id = "_EYETAIL1_E", Name = "_EyeTail1_E" },
                                new() { Id = "_EYETAIL1_F", Name = "_EyeTail1_F" },
                                new() { Id = "_EYETAIL1_G", Name = "_EyeTail1_G" },
                                new() { Id = "_EYETAIL1_NONE", Name = "_EyeTail1_None" },
                                new() { Id = "_EYETAIL1_NONE1", Name = "_EyeTail1_None1" },
                                new() { Id = "_EYETAIL1_NONE2", Name = "_EyeTail1_None2" },
                                new() { Id = "_EYETAIL1_NONE3", Name = "_EyeTail1_None3" },
                            }
                            },
                            new() { GroupId = "_NOSET1_", Descriptors = new()
                            {
                                new() { Id = "_NOSET1_A", Name = "_NoseT1_A" },
                                new() { Id = "_NOSET1_NONE", Name = "_NoseT1_None" },
                                new() { Id = "_NOSET1_NONE1", Name = "_NoseT1_None1" },
                                new() { Id = "_NOSET1_NONE2", Name = "_NoseT1_None2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL2", Name = "_Weird_HTail2", Children = new()
                        {
                            new() { GroupId = "_EYESTAIL2_", Descriptors = new()
                            {
                                new() { Id = "_EYESTAIL2_B", Name = "_EyesTail2_B" },
                                new() { Id = "_EYESTAIL2_C", Name = "_EyesTail2_C" },
                                new() { Id = "_EYESTAIL2_D", Name = "_EyesTail2_D" },
                                new() { Id = "_EYESTAIL2_E", Name = "_EyesTail2_E" },
                                new() { Id = "_EYESTAIL2_F", Name = "_EyesTail2_F" },
                                new() { Id = "_EYESTAIL2_G", Name = "_EyesTail2_G" },
                                new() { Id = "_EYESTAIL2_H", Name = "_EyesTail2_H" },
                                new() { Id = "_EYESTAIL2_NONE", Name = "_EyesTail2_None" },
                                new() { Id = "_EYESTAIL2_NONE1", Name = "_EyesTail2_None1" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_DIVIDE_", Descriptors = new()
                    {
                        new() { Id = "_DIVIDE_1", Name = "_Divide_1" },
                        new() { Id = "_DIVIDE_2", Name = "_Divide_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUG", Name = "_Head_Bug", Children = new()
                {
                    new() { GroupId = "_HBUGACC_", Descriptors = new()
                    {
                        new() { Id = "_HBUGACC_BLANK", Name = "_HBugAcc_Blank" },
                        new() { Id = "_HBUGACC_1", Name = "_HBugAcc_1" },
                        new() { Id = "_HBUGACC_2", Name = "_HBugAcc_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COW", Name = "_Head_Cow", Children = new()
                {
                    new() { GroupId = "_HCE_", Descriptors = new()
                    {
                        new() { Id = "_HCE_0XRARE", Name = "_HCE_0xRARE" },
                        new() { Id = "_HCE_COWEARS5", Name = "_HCE_CowEars5" },
                        new() { Id = "_HCE_COWEARS4", Name = "_HCE_CowEars4" },
                        new() { Id = "_HCE_COWEARS3", Name = "_HCE_CowEars3" },
                        new() { Id = "_HCE_COWEARS2", Name = "_HCE_CowEars2" },
                        new() { Id = "_HCE_COWEARS1", Name = "_HCE_CowEars1" },
                        new() { Id = "_HCE_COWEARS", Name = "_HCE_CowEars" },
                    }
                    },
                    new() { GroupId = "_HCH_", Descriptors = new()
                    {
                        new() { Id = "_HCH_BLANK", Name = "_HCH_Blank" },
                        new() { Id = "_HCH_COWHORN", Name = "_HCH_CowHorn" },
                        new() { Id = "_HCH_LORISHORN", Name = "_HCH_LorisHorn" },
                        new() { Id = "_HCH_ANTHORN", Name = "_HCH_AntHorn" },
                        new() { Id = "_HCH_NOSEBONE", Name = "_HCH_NoseBone" },
                        new() { Id = "_HCH_HEADBONE", Name = "_HCH_HeadBone" },
                        new() { Id = "_HCH_HEADPLATE", Name = "_HCH_HeadPlate" },
                        new() { Id = "_HCH_MULTIHORN", Name = "_HCH_MultiHorn" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COWSKEW", Name = "_Head_CowSkew", Children = new()
                {
                    new() { GroupId = "_HCSH_", Descriptors = new()
                    {
                        new() { Id = "_HCSH_HEADACC", Name = "_HCSH_HeadAcc" },
                        new() { Id = "_HCSH_HEADBONE", Name = "_HCSH_HeadBone" },
                        new() { Id = "_HCSH_ANTHORN", Name = "_HCSH_AntHorn" },
                        new() { Id = "_HCSH_LORISHORN", Name = "_HCSH_LorisHorn" },
                        new() { Id = "_HCSH_COWHORN", Name = "_HCSH_CowHorn" },
                        new() { Id = "_HCSH_BLANK", Name = "_HCSH_Blank" },
                    }
                    },
                    new() { GroupId = "_COWEARS_", Descriptors = new()
                    {
                        new() { Id = "_COWEARS_6", Name = "_CowEars_6" },
                        new() { Id = "_COWEARS_5", Name = "_CowEars_5" },
                        new() { Id = "_COWEARS_4", Name = "_CowEars_4" },
                        new() { Id = "_COWEARS_3", Name = "_CowEars_3" },
                        new() { Id = "_COWEARS_2", Name = "_CowEars_2" },
                        new() { Id = "_COWEARS_1", Name = "_CowEars_1" },
                        new() { Id = "_COWEARS_0XRARE", Name = "_CowEars_0xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LORIS", Name = "_Head_Loris", Children = new()
                {
                    new() { GroupId = "_HLH_", Descriptors = new()
                    {
                        new() { Id = "_HLH_MULTIHORN3", Name = "_HLH_MultiHorn3" },
                        new() { Id = "_HLH_NOSEBONE", Name = "_HLH_NoseBone" },
                        new() { Id = "_HLH_HEADPLATE", Name = "_HLH_HeadPlate" },
                        new() { Id = "_HLH_ANTHORN", Name = "_HLH_AntHorn" },
                        new() { Id = "_HLH_COWHORN", Name = "_HLH_CowHorn" },
                        new() { Id = "_HLH_LORISHORN", Name = "_HLH_LorisHorn" },
                        new() { Id = "_HLH_BLANK", Name = "_HLH_Blank" },
                    }
                    },
                    new() { GroupId = "_HLE_", Descriptors = new()
                    {
                        new() { Id = "_HLE_3", Name = "_HLE_3" },
                        new() { Id = "_HLE_2", Name = "_HLE_2" },
                        new() { Id = "_HLE_1", Name = "_HLE_1" },
                        new() { Id = "_HLE_0XRARE", Name = "_HLE_0xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_REX", Name = "_Head_Rex", Children = new()
                {
                    new() { GroupId = "_HREXACC_", Descriptors = new()
                    {
                        new() { Id = "_HREXACC_0", Name = "_HRexAcc_0" },
                        new() { Id = "_HREXACC_1", Name = "_HRexAcc_1" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "COWFLOATING", FriendlyName = "COWFLOATING", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIENBIRD", Name = "_Head_AlienBird", Children = new()
                {
                    new() { GroupId = "_HABACC_", Descriptors = new()
                    {
                        new() { Id = "_HABACC_BLANK", Name = "_HABAcc_Blank" },
                        new() { Id = "_HABACC_1", Name = "_HABAcc_1" },
                        new() { Id = "_HABACC_2", Name = "_HABAcc_2" },
                        new() { Id = "_HABACC_3", Name = "_HABAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_ALIENTYPE", Name = "_Head_AlienType", Children = new()
                {
                    new() { GroupId = "_WEIRD_", Descriptors = new()
                    {
                        new() { Id = "_WEIRD_CONNECT", Name = "_Weird_Connect", Children = new()
                        {
                            new() { GroupId = "_SHAPEN_", Descriptors = new()
                            {
                                new() { Id = "_SHAPEN_1", Name = "_Shapen_1", Children = new()
                                {
                                    new() { GroupId = "_NOSE1_", Descriptors = new()
                                    {
                                        new() { Id = "_NOSE1_A", Name = "_Nose1_A" },
                                    }
                                    },
                                    new() { GroupId = "_EYES1_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES1_A", Name = "_Eyes1_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_FLARED", Name = "_Weird_Flared", Children = new()
                        {
                            new() { GroupId = "_FRUITF_", Descriptors = new()
                            {
                                new() { Id = "_FRUITF_NONE", Name = "_FruitF_None" },
                                new() { Id = "_FRUITF_A", Name = "_FruitF_A" },
                            }
                            },
                            new() { GroupId = "_FLARETOP_", Descriptors = new()
                            {
                                new() { Id = "_FLARETOP_4", Name = "_FlareTop_4" },
                                new() { Id = "_FLARETOP_2", Name = "_FlareTop_2", Children = new()
                                {
                                    new() { GroupId = "_EYES2_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES2_A", Name = "_Eyes2_A" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_FLARETOP_1", Name = "_FlareTop_1" },
                                new() { Id = "_FLARETOP_NONE", Name = "_FlareTop_None" },
                                new() { Id = "_FLARETOP_3", Name = "_FlareTop_3", Children = new()
                                {
                                    new() { GroupId = "_EYES3_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES3_A", Name = "_Eyes3_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_EYESF_", Descriptors = new()
                            {
                                new() { Id = "_EYESF_NONE", Name = "_EyesF_None" },
                                new() { Id = "_EYESF_A", Name = "_EyesF_A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_LONG", Name = "_Weird_Long", Children = new()
                        {
                            new() { GroupId = "_EARSL_", Descriptors = new()
                            {
                                new() { Id = "_EARSL_A", Name = "_EarsL_A" },
                                new() { Id = "_EARSL_NONE", Name = "_EarsL_None" },
                            }
                            },
                            new() { GroupId = "_EYESL_", Descriptors = new()
                            {
                                new() { Id = "_EYESL_A", Name = "_EyesL_A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_STUMP", Name = "_Weird_Stump", Children = new()
                        {
                            new() { GroupId = "_EYESS_", Descriptors = new()
                            {
                                new() { Id = "_EYESS_A", Name = "_EyesS_A" },
                            }
                            },
                            new() { GroupId = "_HORNSS_", Descriptors = new()
                            {
                                new() { Id = "_HORNSS_A", Name = "_HornsS_A" },
                                new() { Id = "_HORNSS_NONE", Name = "_HornsS_None" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL1", Name = "_Weird_HTail1", Children = new()
                        {
                            new() { GroupId = "_EYETAIL1_", Descriptors = new()
                            {
                                new() { Id = "_EYETAIL1_A", Name = "_EyeTail1_A" },
                                new() { Id = "_EYETAIL1_NONE", Name = "_EyeTail1_None" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL2", Name = "_Weird_HTail2" },
                    }
                    },
                    new() { GroupId = "_DIVIDE_", Descriptors = new()
                    {
                        new() { Id = "_DIVIDE_1", Name = "_Divide_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUG", Name = "_Head_Bug", Children = new()
                {
                    new() { GroupId = "_HBUGACC_", Descriptors = new()
                    {
                        new() { Id = "_HBUGACC_BLANK", Name = "_HBugAcc_Blank" },
                        new() { Id = "_HBUGACC_1", Name = "_HBugAcc_1" },
                        new() { Id = "_HBUGACC_2", Name = "_HBugAcc_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COW", Name = "_Head_Cow", Children = new()
                {
                    new() { GroupId = "_HCE_", Descriptors = new()
                    {
                        new() { Id = "_HCE_BLANK", Name = "_HCE_Blank" },
                        new() { Id = "_HCE_COWEARS", Name = "_HCE_CowEars" },
                    }
                    },
                    new() { GroupId = "_HCH_", Descriptors = new()
                    {
                        new() { Id = "_HCH_BLANK", Name = "_HCH_Blank" },
                        new() { Id = "_HCH_COWHORN", Name = "_HCH_CowHorn" },
                        new() { Id = "_HCH_LORISHORN", Name = "_HCH_LorisHorn" },
                        new() { Id = "_HCH_ANTHORN", Name = "_HCH_AntHorn" },
                        new() { Id = "_HCH_NOSEBONE", Name = "_HCH_NoseBone" },
                        new() { Id = "_HCH_HEADBONE", Name = "_HCH_HeadBone" },
                        new() { Id = "_HCH_HEADPLATE", Name = "_HCH_HeadPlate" },
                        new() { Id = "_HCH_MULTIHORN", Name = "_HCH_MultiHorn" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COWSKEW", Name = "_Head_CowSkew", Children = new()
                {
                    new() { GroupId = "_HCSE_", Descriptors = new()
                    {
                        new() { Id = "_HCSE_COWEARS", Name = "_HCSE_CowEars" },
                        new() { Id = "_HCSE_BLANK", Name = "_HCSE_Blank" },
                    }
                    },
                    new() { GroupId = "_HCSH_", Descriptors = new()
                    {
                        new() { Id = "_HCSH_HEADACC", Name = "_HCSH_HeadAcc" },
                        new() { Id = "_HCSH_HEADBONE", Name = "_HCSH_HeadBone" },
                        new() { Id = "_HCSH_ANTHORN", Name = "_HCSH_AntHorn" },
                        new() { Id = "_HCSH_LORISHORN", Name = "_HCSH_LorisHorn" },
                        new() { Id = "_HCSH_COWHORN", Name = "_HCSH_CowHorn" },
                        new() { Id = "_HCSH_BLANK", Name = "_HCSH_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LORIS", Name = "_Head_Loris", Children = new()
                {
                    new() { GroupId = "_HLH_", Descriptors = new()
                    {
                        new() { Id = "_HLH_MULTIHORN3", Name = "_HLH_MultiHorn3" },
                        new() { Id = "_HLH_NOSEBONE", Name = "_HLH_NoseBone" },
                        new() { Id = "_HLH_HEADPLATE", Name = "_HLH_HeadPlate" },
                        new() { Id = "_HLH_ANTHORN", Name = "_HLH_AntHorn" },
                        new() { Id = "_HLH_COWHORN", Name = "_HLH_CowHorn" },
                        new() { Id = "_HLH_LORISHORN", Name = "_HLH_LorisHorn" },
                        new() { Id = "_HLH_BLANK", Name = "_HLH_Blank" },
                    }
                    },
                    new() { GroupId = "_HLE_", Descriptors = new()
                    {
                        new() { Id = "_HLE_1", Name = "_HLE_1" },
                        new() { Id = "_HLE_BLANK", Name = "_HLE_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_REX", Name = "_Head_Rex", Children = new()
                {
                    new() { GroupId = "_HREXACC_", Descriptors = new()
                    {
                        new() { Id = "_HREXACC_1", Name = "_HRexAcc_1" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_WINGS_", Descriptors = new()
            {
                new() { Id = "_WINGS_A", Name = "_Wings_A" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_COW", Name = "_Body_Cow", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK1", Name = "_BCA_Blank1" },
                        new() { Id = "_BCA_COWHUMP", Name = "_BCA_CowHump", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_BLANKYO3", Name = "_Ext_BlankYo3" },
                                new() { Id = "_EXT_BACKSPINES", Name = "_Ext_BackSpinesxRARE" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_ROCKSXRARE", Name = "_BCA_RocksxRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE", Name = "_Ext_Mine" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_BACKSPINES", Name = "_BCA_BackSpinesxRARE" },
                        new() { Id = "_BCA_STEGSPIKES", Name = "_BCA_StegSpikesxRARE" },
                        new() { Id = "_BCA_TURTSHELLX", Name = "_BCA_TurtShellxRARE" },
                        new() { Id = "_BCA_BACKFINXRA", Name = "_BCA_BackFinxRARE" },
                        new() { Id = "_BCA_LUMPXRARE", Name = "_BCA_LumpxRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_ROCK", Name = "_Body_Rock", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK2", Name = "_BCA_Blank2" },
                    }
                    },
                    new() { GroupId = "_BRA_", Descriptors = new()
                    {
                        new() { Id = "_BRA_ROCKS1XRAR", Name = "_BRA_Rocks1xRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE2", Name = "_Ext_Mine2" },
                                new() { Id = "_EXT_BLANKYO2", Name = "_Ext_BlankYo2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BRA_ARMSPINESX", Name = "_BRA_ArmSpinesxRARE" },
                        new() { Id = "_BRA_BACKSPINES", Name = "_BRA_BackSpinesxRARE" },
                        new() { Id = "_BRA_STEGSPIKES", Name = "_BRA_StegSpikesxRARE" },
                        new() { Id = "_BRA_TURTSHELLX", Name = "_BRA_TurtShellxRARE" },
                        new() { Id = "_BRA_BACKFINXRA", Name = "_BRA_BackFinxRARE" },
                        new() { Id = "_BRA_LUMP1XRARE", Name = "_BRA_Lump1xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_ALIEN", Name = "_Tail_Alien", Children = new()
                {
                    new() { GroupId = "_TAACC_", Descriptors = new()
                    {
                        new() { Id = "_TAACC_0", Name = "_TAacc_0" },
                        new() { Id = "_TAACC_1", Name = "_TAacc_1" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_COW", Name = "_Tail_Cow" },
                new() { Id = "_TAIL_TURTLE", Name = "_Tail_Turtle" },
            }
            },
        }
        },
        new() { CreatureId = "COWHINDLEGS", FriendlyName = "COWHINDLEGS", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIENBIRD", Name = "_Head_AlienBird", Children = new()
                {
                    new() { GroupId = "_HABACC_", Descriptors = new()
                    {
                        new() { Id = "_HABACC_BLANK", Name = "_HABAcc_Blank" },
                        new() { Id = "_HABACC_1", Name = "_HABAcc_1" },
                        new() { Id = "_HABACC_2", Name = "_HABAcc_2" },
                        new() { Id = "_HABACC_3", Name = "_HABAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_ALIENTYPE", Name = "_Head_AlienType", Children = new()
                {
                    new() { GroupId = "_WEIRD_", Descriptors = new()
                    {
                        new() { Id = "_WEIRD_CONNECT", Name = "_Weird_Connect", Children = new()
                        {
                            new() { GroupId = "_SHAPEN_", Descriptors = new()
                            {
                                new() { Id = "_SHAPEN_1", Name = "_Shapen_1", Children = new()
                                {
                                    new() { GroupId = "_NOSE1_", Descriptors = new()
                                    {
                                        new() { Id = "_NOSE1_A", Name = "_Nose1_A" },
                                    }
                                    },
                                    new() { GroupId = "_EYES1_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES1_A", Name = "_Eyes1_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_FLARED", Name = "_Weird_Flared", Children = new()
                        {
                            new() { GroupId = "_FRUITF_", Descriptors = new()
                            {
                                new() { Id = "_FRUITF_NONE", Name = "_FruitF_None" },
                                new() { Id = "_FRUITF_A", Name = "_FruitF_A" },
                            }
                            },
                            new() { GroupId = "_FLARETOP_", Descriptors = new()
                            {
                                new() { Id = "_FLARETOP_4", Name = "_FlareTop_4" },
                                new() { Id = "_FLARETOP_2", Name = "_FlareTop_2", Children = new()
                                {
                                    new() { GroupId = "_EYES2_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES2_A", Name = "_Eyes2_A" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_FLARETOP_1", Name = "_FlareTop_1" },
                                new() { Id = "_FLARETOP_NONE", Name = "_FlareTop_None" },
                                new() { Id = "_FLARETOP_3", Name = "_FlareTop_3", Children = new()
                                {
                                    new() { GroupId = "_EYES3_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES3_A", Name = "_Eyes3_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_EYESF_", Descriptors = new()
                            {
                                new() { Id = "_EYESF_NONE", Name = "_EyesF_None" },
                                new() { Id = "_EYESF_A", Name = "_EyesF_A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_LONG", Name = "_Weird_Long", Children = new()
                        {
                            new() { GroupId = "_EARSL_", Descriptors = new()
                            {
                                new() { Id = "_EARSL_A", Name = "_EarsL_A" },
                                new() { Id = "_EARSL_NONE", Name = "_EarsL_None" },
                            }
                            },
                            new() { GroupId = "_EYESL_", Descriptors = new()
                            {
                                new() { Id = "_EYESL_A", Name = "_EyesL_A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_STUMP", Name = "_Weird_Stump", Children = new()
                        {
                            new() { GroupId = "_EYESS_", Descriptors = new()
                            {
                                new() { Id = "_EYESS_A", Name = "_EyesS_A" },
                            }
                            },
                            new() { GroupId = "_HORNSS_", Descriptors = new()
                            {
                                new() { Id = "_HORNSS_A", Name = "_HornsS_A" },
                                new() { Id = "_HORNSS_NONE", Name = "_HornsS_None" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL1", Name = "_Weird_HTail1", Children = new()
                        {
                            new() { GroupId = "_EYETAIL1_", Descriptors = new()
                            {
                                new() { Id = "_EYETAIL1_A", Name = "_EyeTail1_A" },
                                new() { Id = "_EYETAIL1_NONE", Name = "_EyeTail1_None" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL2", Name = "_Weird_HTail2" },
                    }
                    },
                    new() { GroupId = "_DIVIDE_", Descriptors = new()
                    {
                        new() { Id = "_DIVIDE_1", Name = "_Divide_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUG", Name = "_Head_Bug", Children = new()
                {
                    new() { GroupId = "_HBUGACC_", Descriptors = new()
                    {
                        new() { Id = "_HBUGACC_BLANK", Name = "_HBugAcc_Blank" },
                        new() { Id = "_HBUGACC_1", Name = "_HBugAcc_1" },
                        new() { Id = "_HBUGACC_2", Name = "_HBugAcc_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COW", Name = "_Head_Cow", Children = new()
                {
                    new() { GroupId = "_HCE_", Descriptors = new()
                    {
                        new() { Id = "_HCE_BLANK", Name = "_HCE_Blank" },
                        new() { Id = "_HCE_COWEARS", Name = "_HCE_CowEars" },
                    }
                    },
                    new() { GroupId = "_HCH_", Descriptors = new()
                    {
                        new() { Id = "_HCH_BLANK", Name = "_HCH_Blank" },
                        new() { Id = "_HCH_COWHORN", Name = "_HCH_CowHorn" },
                        new() { Id = "_HCH_LORISHORN", Name = "_HCH_LorisHorn" },
                        new() { Id = "_HCH_ANTHORN", Name = "_HCH_AntHorn" },
                        new() { Id = "_HCH_NOSEBONE", Name = "_HCH_NoseBone" },
                        new() { Id = "_HCH_HEADBONE", Name = "_HCH_HeadBone" },
                        new() { Id = "_HCH_HEADPLATE", Name = "_HCH_HeadPlate" },
                        new() { Id = "_HCH_MULTIHORN", Name = "_HCH_MultiHorn" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COWSKEW", Name = "_Head_CowSkew", Children = new()
                {
                    new() { GroupId = "_HCSE_", Descriptors = new()
                    {
                        new() { Id = "_HCSE_COWEARS", Name = "_HCSE_CowEars" },
                        new() { Id = "_HCSE_BLANK", Name = "_HCSE_Blank" },
                    }
                    },
                    new() { GroupId = "_HCSH_", Descriptors = new()
                    {
                        new() { Id = "_HCSH_HEADACC", Name = "_HCSH_HeadAcc" },
                        new() { Id = "_HCSH_HEADBONE", Name = "_HCSH_HeadBone" },
                        new() { Id = "_HCSH_ANTHORN", Name = "_HCSH_AntHorn" },
                        new() { Id = "_HCSH_LORISHORN", Name = "_HCSH_LorisHorn" },
                        new() { Id = "_HCSH_COWHORN", Name = "_HCSH_CowHorn" },
                        new() { Id = "_HCSH_BLANK", Name = "_HCSH_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LORIS", Name = "_Head_Loris", Children = new()
                {
                    new() { GroupId = "_HLH_", Descriptors = new()
                    {
                        new() { Id = "_HLH_MULTIHORN3", Name = "_HLH_MultiHorn3" },
                        new() { Id = "_HLH_NOSEBONE", Name = "_HLH_NoseBone" },
                        new() { Id = "_HLH_HEADPLATE", Name = "_HLH_HeadPlate" },
                        new() { Id = "_HLH_ANTHORN", Name = "_HLH_AntHorn" },
                        new() { Id = "_HLH_COWHORN", Name = "_HLH_CowHorn" },
                        new() { Id = "_HLH_LORISHORN", Name = "_HLH_LorisHorn" },
                        new() { Id = "_HLH_BLANK", Name = "_HLH_Blank" },
                    }
                    },
                    new() { GroupId = "_HLE_", Descriptors = new()
                    {
                        new() { Id = "_HLE_1", Name = "_HLE_1" },
                        new() { Id = "_HLE_BLANK", Name = "_HLE_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_REX", Name = "_Head_Rex", Children = new()
                {
                    new() { GroupId = "_HREXACC_", Descriptors = new()
                    {
                        new() { Id = "_HREXACC_1", Name = "_HRexAcc_1" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_COW", Name = "_Body_Cow", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK1", Name = "_BCA_Blank1" },
                        new() { Id = "_BCA_COWHUMP", Name = "_BCA_CowHump", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_BLANKYO3", Name = "_Ext_BlankYo3" },
                                new() { Id = "_EXT_BACKSPINESXRARE", Name = "_Ext_BackSpinesxRARE" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_ROCKSXRARE", Name = "_BCA_RocksxRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE", Name = "_Ext_Mine" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_BACKSPINESXRARE", Name = "_BCA_BackSpinesxRARE" },
                        new() { Id = "_BCA_STEGSPIKESXRARE", Name = "_BCA_StegSpikesxRARE" },
                        new() { Id = "_BCA_TURTSHELLXRARE", Name = "_BCA_TurtShellxRARE" },
                        new() { Id = "_BCA_BACKFINXRARE", Name = "_BCA_BackFinxRARE" },
                        new() { Id = "_BCA_LUMPXRARE", Name = "_BCA_LumpxRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_ROCK", Name = "_Body_Rock", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK2", Name = "_BCA_Blank2" },
                    }
                    },
                    new() { GroupId = "_BRA_", Descriptors = new()
                    {
                        new() { Id = "_BRA_ROCKS1XRARE", Name = "_BRA_Rocks1xRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE2", Name = "_Ext_Mine2" },
                                new() { Id = "_EXT_BLANKYO2", Name = "_Ext_BlankYo2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BRA_ARMSPINESXRARE", Name = "_BRA_ArmSpinesxRARE" },
                        new() { Id = "_BRA_BACKSPINESXRARE", Name = "_BRA_BackSpinesxRARE" },
                        new() { Id = "_BRA_STEGSPIKESXRARE", Name = "_BRA_StegSpikesxRARE" },
                        new() { Id = "_BRA_TURTSHELLXRARE", Name = "_BRA_TurtShellxRARE" },
                        new() { Id = "_BRA_BACKFINXRARE", Name = "_BRA_BackFinxRARE" },
                        new() { Id = "_BRA_LUMP1XRARE", Name = "_BRA_Lump1xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_ALIEN", Name = "_Tail_Alien", Children = new()
                {
                    new() { GroupId = "_TAACC_", Descriptors = new()
                    {
                        new() { Id = "_TAACC_0", Name = "_TAacc_0" },
                        new() { Id = "_TAACC_1", Name = "_TAacc_1" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_COW", Name = "_Tail_Cow" },
                new() { Id = "_TAIL_TURTLE", Name = "_Tail_Turtle" },
            }
            },
        }
        },
        new() { CreatureId = "DRILL", FriendlyName = "DRILL", Details = new()
        {
            new() { GroupId = "_DRILL_", Descriptors = new()
            {
                new() { Id = "_DRILL_A", Name = "_Drill_A", Children = new()
                {
                    new() { GroupId = "_DRILLSPIN_", Descriptors = new()
                    {
                        new() { Id = "_DRILLSPIN_A", Name = "_DrillSpin_A", Children = new()
                        {
                            new() { GroupId = "_DRILLV_", Descriptors = new()
                            {
                                new() { Id = "_DRILLV_A", Name = "_DrillV_A" },
                                new() { Id = "_DRILLV_B", Name = "_DrillV_B" },
                                new() { Id = "_DRILLV_C", Name = "_DrillV_C" },
                                new() { Id = "_DRILLV_D", Name = "_DrillV_D" },
                                new() { Id = "_DRILLV_E", Name = "_DrillV_E" },
                                new() { Id = "_DRILLV_F", Name = "_DrillV_F" },
                                new() { Id = "_DRILLV_G", Name = "_DrillV_G" },
                                new() { Id = "_DRILLV_H", Name = "_DrillV_H" },
                                new() { Id = "_DRILLV_I", Name = "_DrillV_I" },
                                new() { Id = "_DRILLV_J", Name = "_DrillV_J" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "FIEND", FriendlyName = "FIEND", Details = new()
        {
            new() { GroupId = "_FIEND_", Descriptors = new()
            {
                new() { Id = "_FIEND_BODY", Name = "_Fiend_Body" },
            }
            },
        }
        },
        new() { CreatureId = "FISHFIEND", FriendlyName = "FISHFIEND", Details = new()
        {
            new() { GroupId = "_FISHFIEND_", Descriptors = new()
            {
                new() { Id = "_FISHFIEND_1", Name = "_FishFiend_1" },
            }
            },
        }
        },
        new() { CreatureId = "FISHFIENDSMALL", FriendlyName = "FISHFIENDSMALL", Details = new()
        {
            new() { GroupId = "_FISH_", Descriptors = new()
            {
                new() { Id = "_FISH_B", Name = "_Fish_B", Children = new()
                {
                    new() { GroupId = "_FBBODY_", Descriptors = new()
                    {
                        new() { Id = "_FBBODY_1", Name = "_FbBody_1", Children = new()
                        {
                            new() { GroupId = "_TYPE_", Descriptors = new()
                            {
                                new() { Id = "_TYPE_1", Name = "_Type_1", Children = new()
                                {
                                    new() { GroupId = "_EYESTALKS_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESTALKS_3", Name = "_Eyestalks_3", Children = new()
                                        {
                                            new() { GroupId = "_BALLZ_", Descriptors = new()
                                            {
                                                new() { Id = "_BALLZ_1", Name = "_Ballz_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_EYESTALKS_1", Name = "_Eyestalks_1" },
                                        new() { Id = "_EYESTALKS_2", Name = "_Eyestalks_2" },
                                    }
                                    },
                                    new() { GroupId = "_TENDRILS_", Descriptors = new()
                                    {
                                        new() { Id = "_TENDRILS_2", Name = "_Tendrils_2" },
                                        new() { Id = "_TENDRILS_3", Name = "_Tendrils_3", Children = new()
                                        {
                                            new() { GroupId = "_PLINGERSS_", Descriptors = new()
                                            {
                                                new() { Id = "_PLINGERSS_3", Name = "_Plingerss_3" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_TENDRILS_1", Name = "_Tendrils_1" },
                                    }
                                    },
                                    new() { GroupId = "_COLLAR_", Descriptors = new()
                                    {
                                        new() { Id = "_COLLAR_1", Name = "_Collar_1" },
                                        new() { Id = "_COLLAR_2", Name = "_Collar_2" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "FISHFLOCK", FriendlyName = "FISHFLOCK", Details = new()
        {
            new() { GroupId = "_WEIRD_", Descriptors = new()
            {
                new() { Id = "_WEIRD_CRYSTAL", Name = "_Weird_Crystal" },
            }
            },
        }
        },
        new() { CreatureId = "SPIDERFLOAT", FriendlyName = "FLOATSPIDER", Details = new()
        {
            new() { GroupId = "_MANTIS_", Descriptors = new()
            {
                new() { Id = "_MANTIS_A", Name = "_Mantis_A", Children = new()
                {
                    new() { GroupId = "_ARMS_", Descriptors = new()
                    {
                        new() { Id = "_ARMS_NULL", Name = "_Arms_NULL" },
                        new() { Id = "_ARMS_1", Name = "_Arms_1" },
                        new() { Id = "_ARMS_2", Name = "_Arms_2" },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_1XRARE", Name = "_Body_1xRARE", Children = new()
                        {
                            new() { GroupId = "_TRUNK_", Descriptors = new()
                            {
                                new() { Id = "_TRUNK_NULL", Name = "_Trunk_NULL" },
                                new() { Id = "_TRUNK_NULL2", Name = "_Trunk_NULL2" },
                                new() { Id = "_TRUNK_1XRARE", Name = "_Trunk_1xRARE", Children = new()
                                {
                                    new() { GroupId = "_CAP1_", Descriptors = new()
                                    {
                                        new() { Id = "_CAP1_3", Name = "_Cap1_3", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILL5_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILL5_1", Name = "_CapFill5_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CAP1_2", Name = "_Cap1_2", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILL2_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILL2_1", Name = "_CapFill2_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CAP1_1", Name = "_Cap1_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TRUNK_2XRARE", Name = "_Trunk_2xRare", Children = new()
                                {
                                    new() { GroupId = "_CAPB_", Descriptors = new()
                                    {
                                        new() { Id = "_CAPB_11", Name = "_CapB_11", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILLB_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILLB_1", Name = "_CapFillB_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CAPB_12", Name = "_CapB_12", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILL2B_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILL2B_1", Name = "_CapFill2B_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_2", Name = "_Body_2", Children = new()
                        {
                            new() { GroupId = "_NECK_", Descriptors = new()
                            {
                                new() { Id = "_NECK_BUG", Name = "_Neck_Bug", Children = new()
                                {
                                    new() { GroupId = "_ACC1_", Descriptors = new()
                                    {
                                        new() { Id = "_ACC1_FINFLAPS", Name = "_Acc1_finFlaps" },
                                        new() { Id = "_ACC1_MANTISTAIL", Name = "_Acc1_MantisTail" },
                                        new() { Id = "_ACC1_BORDER", Name = "_Acc1_Border" },
                                        new() { Id = "_ACC1_BULBSIDE", Name = "_Acc1_BulbSide", Children = new()
                                        {
                                            new() { GroupId = "_ACC5_", Descriptors = new()
                                            {
                                                new() { Id = "_ACC5_SPIKES", Name = "_Acc5_Spikes" },
                                                new() { Id = "_ACC5_NONE", Name = "_Acc5_none" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ACC1_BULBTOP", Name = "_Acc1_BulbTop", Children = new()
                                        {
                                            new() { GroupId = "_ACC2_", Descriptors = new()
                                            {
                                                new() { Id = "_ACC2_BULBHAT", Name = "_Acc2_BulbHat", Children = new()
                                                {
                                                    new() { GroupId = "_ACC3_", Descriptors = new()
                                                    {
                                                        new() { Id = "_ACC3_NONE", Name = "_Acc3_none" },
                                                        new() { Id = "_ACC3_SPIKES", Name = "_Acc3_Spikes" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_ALTH_", Descriptors = new()
                                    {
                                        new() { Id = "_ALTH_MANTS", Name = "_AltH_Mants", Children = new()
                                        {
                                            new() { GroupId = "_MANTNA_", Descriptors = new()
                                            {
                                                new() { Id = "_MANTNA_1", Name = "_MAntna_1" },
                                            }
                                            },
                                            new() { GroupId = "_MEYE_", Descriptors = new()
                                            {
                                                new() { Id = "_MEYE_2", Name = "_MEye_2" },
                                                new() { Id = "_MEYE_1", Name = "_MEye_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ALTH_MANTB", Name = "_AltH_MantB", Children = new()
                                        {
                                            new() { GroupId = "_MANTNA_", Descriptors = new()
                                            {
                                                new() { Id = "_MANTNA_2", Name = "_MAntna_2" },
                                            }
                                            },
                                            new() { GroupId = "_MEYYE_", Descriptors = new()
                                            {
                                                new() { Id = "_MEYYE_2", Name = "_MEyye_2" },
                                            }
                                            },
                                            new() { GroupId = "_MOUT_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUT_A", Name = "_Mout_A" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ALTH_3", Name = "_AltH_3" },
                                        new() { Id = "_ALTH_2", Name = "_AltH_2" },
                                        new() { Id = "_ALTH_1", Name = "_AltH_1", Children = new()
                                        {
                                            new() { GroupId = "_EYES_", Descriptors = new()
                                            {
                                                new() { Id = "_EYES_1", Name = "_Eyes_1" },
                                            }
                                            },
                                            new() { GroupId = "_MOUTH_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUTH_1", Name = "_Mouth_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ALTH_5", Name = "_AltH_5" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_NECK_ALT", Name = "_Neck_Alt", Children = new()
                                {
                                    new() { GroupId = "_TAIL_", Descriptors = new()
                                    {
                                        new() { Id = "_TAIL_NULL1", Name = "_Tail_Null1" },
                                        new() { Id = "_TAIL_MANTISTAIL", Name = "_Tail_MantisTail" },
                                    }
                                    },
                                    new() { GroupId = "_HEAD_", Descriptors = new()
                                    {
                                        new() { Id = "_HEAD_MUFFIN", Name = "_Head_Muffin", Children = new()
                                        {
                                            new() { GroupId = "_ALTH2_", Descriptors = new()
                                            {
                                                new() { Id = "_ALTH2_BULBB", Name = "_AltH2_BulbB", Children = new()
                                                {
                                                    new() { GroupId = "_EYES_", Descriptors = new()
                                                    {
                                                        new() { Id = "_EYES_B", Name = "_Eyes_B" },
                                                    }
                                                    },
                                                    new() { GroupId = "_AT_", Descriptors = new()
                                                    {
                                                        new() { Id = "_AT_SPIKES", Name = "_AT_Spikes" },
                                                    }
                                                    },
                                                    new() { GroupId = "_MOUTH_", Descriptors = new()
                                                    {
                                                        new() { Id = "_MOUTH_B", Name = "_Mouth_B" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_HEAD_STALKS", Name = "_Head_Stalks", Children = new()
                                        {
                                            new() { GroupId = "_MOUTHS_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUTHS_1", Name = "_Mouths_1" },
                                            }
                                            },
                                            new() { GroupId = "_ATC_", Descriptors = new()
                                            {
                                                new() { Id = "_ATC_BORDER", Name = "_ATC_Border", Children = new()
                                                {
                                                    new() { GroupId = "_JELLYFINGERS_", Descriptors = new()
                                                    {
                                                        new() { Id = "_JELLYFINGERS_1", Name = "_JellyFingers_1" },
                                                        new() { Id = "_JELLYFINGERS_NONE", Name = "_JellyFingers_none" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                            new() { GroupId = "_EYESD_", Descriptors = new()
                                            {
                                                new() { Id = "_EYESD_1", Name = "_EyesD_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_NECK_CRAB", Name = "_Neck_Crab", Children = new()
                                {
                                    new() { GroupId = "_ACCR_", Descriptors = new()
                                    {
                                        new() { Id = "_ACCR_BORDER", Name = "_ACCR_Border" },
                                        new() { Id = "_ACCR_NONE", Name = "_ACCR_none" },
                                        new() { Id = "_ACCR_MANTISTAIL", Name = "_ACCR_MantisTail" },
                                        new() { Id = "_ACCR_FINFLAPS", Name = "_ACCR_FinFLaps" },
                                        new() { Id = "_ACCR_BULBSIDE", Name = "_ACCR_BulbSide", Children = new()
                                        {
                                            new() { GroupId = "_ACCRC_", Descriptors = new()
                                            {
                                                new() { Id = "_ACCRC_SPIKES", Name = "_ACCRc_Spikes" },
                                                new() { Id = "_ACCRC_NONE", Name = "_ACCRc_none" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ACCR_BULBTOP", Name = "_ACCR_BulbTop", Children = new()
                                        {
                                            new() { GroupId = "_ACCR_", Descriptors = new()
                                            {
                                                new() { Id = "_ACCR_BULBHAT", Name = "_ACCR_BulbHat", Children = new()
                                                {
                                                    new() { GroupId = "_ACCRB_", Descriptors = new()
                                                    {
                                                        new() { Id = "_ACCRB_NONE", Name = "_ACCRb_none" },
                                                        new() { Id = "_ACCRB_SPIKES", Name = "_ACCRb_Spikes" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_LEGS_", Descriptors = new()
                    {
                        new() { Id = "_LEGS_2", Name = "_Legs_2" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "BEETLE", FriendlyName = "FLYINGBEETLE", Details = new()
        {
            new() { GroupId = "_SHELL_", Descriptors = new()
            {
                new() { Id = "_SHELL_BIG", Name = "_Shell_Big" },
                new() { Id = "_SHELL_REG", Name = "_Shell_Reg" },
                new() { Id = "_SHELL_SPIKY", Name = "_Shell_Spiky" },
            }
            },
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_RHINO", Name = "_Head_Rhino" },
                new() { Id = "_HEAD_ROUND", Name = "_Head_Round" },
            }
            },
        }
        },
        new() { CreatureId = "FLYINGLIZARD", FriendlyName = "FLYINGLIZARD", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_BIRD", Name = "_Head_Bird", Children = new()
                {
                    new() { GroupId = "_HBACC_", Descriptors = new()
                    {
                        new() { Id = "_HBACC_0", Name = "_HBAcc_0" },
                        new() { Id = "_HBACC_1", Name = "_HBAcc_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BIRDV2", Name = "_Head_Birdv2", Children = new()
                {
                    new() { GroupId = "_HB1ACC_", Descriptors = new()
                    {
                        new() { Id = "_HB1ACC_0", Name = "_HB1Acc_0" },
                        new() { Id = "_HB1ACC_1", Name = "_HB1Acc_1" },
                        new() { Id = "_HB1ACC_2", Name = "_HB1Acc_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_EAGLE", Name = "_Head_Eagle" },
                new() { Id = "_HEAD_LIZARD", Name = "_Head_Lizard", Children = new()
                {
                    new() { GroupId = "_HLACC_", Descriptors = new()
                    {
                        new() { Id = "_HLACC_1", Name = "_HLAcc_1" },
                        new() { Id = "_HLACC_2", Name = "_HLAcc_2" },
                        new() { Id = "_HLACC_3", Name = "_HLAcc_3" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_WINGS_", Descriptors = new()
            {
                new() { Id = "_WINGS_BIRD", Name = "_Wings_Bird", Children = new()
                {
                    new() { GroupId = "_BWACC_", Descriptors = new()
                    {
                        new() { Id = "_BWACC_1", Name = "_BWAcc_1" },
                        new() { Id = "_BWACC_0", Name = "_BWAcc_0" },
                    }
                    },
                }
                },
                new() { Id = "_WINGS_EAGLE", Name = "_Wings_Eagle" },
                new() { Id = "_WINGS_LIZARD", Name = "_Wings_Lizard" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_EAGLE", Name = "_Body_Eagle" },
                new() { Id = "_BODY_LIZARD", Name = "_Body_Lizard" },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_EAGLE", Name = "_Tail_Eagle" },
                new() { Id = "_TAIL_LIZARD", Name = "_Tail_Lizard" },
            }
            },
        }
        },
        new() { CreatureId = "FLYINGSNAKE", FriendlyName = "FLYINGSNAKE", Details = new()
        {
            new() { GroupId = "_SNAKE_", Descriptors = new()
            {
                new() { Id = "_SNAKE_A", Name = "_Snake_A", Children = new()
                {
                    new() { GroupId = "_SET_", Descriptors = new()
                    {
                        new() { Id = "_SET_A", Name = "_Set_A", Children = new()
                        {
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_1", Name = "_Head_1" },
                                new() { Id = "_HEAD_2", Name = "_Head_2" },
                                new() { Id = "_HEAD_3", Name = "_Head_3" },
                                new() { Id = "_HEAD_4", Name = "_Head_4", Children = new()
                                {
                                    new() { GroupId = "_HWACC_", Descriptors = new()
                                    {
                                        new() { Id = "_HWACC_1", Name = "_HWAcc_1" },
                                        new() { Id = "_HWACC_NULL", Name = "_HWAcc_NULL" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_WINGS_", Descriptors = new()
                            {
                                new() { Id = "_WINGS_1", Name = "_Wings_1" },
                                new() { Id = "_WINGS_NULL", Name = "_Wings_NULL" },
                                new() { Id = "_WINGS_12", Name = "_Wings_12" },
                                new() { Id = "_WINGS_9", Name = "_Wings_9" },
                                new() { Id = "_WINGS_10", Name = "_Wings_10" },
                                new() { Id = "_WINGS_11", Name = "_Wings_11" },
                            }
                            },
                            new() { GroupId = "_BODY_", Descriptors = new()
                            {
                                new() { Id = "_BODY_1", Name = "_Body_1", Children = new()
                                {
                                    new() { GroupId = "_B1ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_B1ACC_0", Name = "_B1Acc_0" },
                                        new() { Id = "_B1ACC_1", Name = "_B1Acc_1" },
                                        new() { Id = "_B1ACC_2", Name = "_B1Acc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BODY_2", Name = "_Body_2", Children = new()
                                {
                                    new() { GroupId = "_BEELACC_", Descriptors = new()
                                    {
                                        new() { Id = "_BEELACC_NULL", Name = "_BEelAcc_NULL" },
                                        new() { Id = "_BEELACC_5", Name = "_BEelAcc_5" },
                                        new() { Id = "_BEELACC_6", Name = "_BEelAcc_6" },
                                        new() { Id = "_BEELACC_7", Name = "_BEelAcc_7" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BODY_3", Name = "_Body_3", Children = new()
                                {
                                    new() { GroupId = "_BWACC_", Descriptors = new()
                                    {
                                        new() { Id = "_BWACC_NULL", Name = "_BWAcc_NULL" },
                                        new() { Id = "_BWACC_1", Name = "_BWAcc_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_TAIL_", Descriptors = new()
                            {
                                new() { Id = "_TAIL_1", Name = "_Tail_1", Children = new()
                                {
                                    new() { GroupId = "_T1ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_T1ACC_2", Name = "_T1Acc_2" },
                                        new() { Id = "_T1ACC_1", Name = "_T1Acc_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TAIL_2", Name = "_Tail_2", Children = new()
                                {
                                    new() { GroupId = "_TAILFIN_", Descriptors = new()
                                    {
                                        new() { Id = "_TAILFIN_3", Name = "_TailFin_3" },
                                        new() { Id = "_TAILFIN_4", Name = "_TailFin_4" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TAIL_3", Name = "_Tail_3", Children = new()
                                {
                                    new() { GroupId = "_TAILFIN3_", Descriptors = new()
                                    {
                                        new() { Id = "_TAILFIN3_3", Name = "_TailFin3_3" },
                                        new() { Id = "_TAILFIN3_4", Name = "_TailFin3_4" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TAIL_4", Name = "_Tail_4", Children = new()
                                {
                                    new() { GroupId = "_TWACC_", Descriptors = new()
                                    {
                                        new() { Id = "_TWACC_NULL", Name = "_TWAcc_NULL" },
                                        new() { Id = "_TWACC_3", Name = "_TWAcc_3" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TAIL_5", Name = "_Tail_5" },
                                new() { Id = "_TAIL_6", Name = "_Tail_6" },
                                new() { Id = "_TAIL_7", Name = "_Tail_7" },
                            }
                            },
                        }
                        },
                        new() { Id = "_SET_B", Name = "_Set_B", Children = new()
                        {
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_CENTIPEDE", Name = "_Head_Centipede" },
                                new() { Id = "_HEAD_LEECH", Name = "_Head_Leech" },
                                new() { Id = "_HEAD_SANDWORM", Name = "_Head_SandWorm" },
                                new() { Id = "_HEAD_SANDSNAKE", Name = "_Head_SandSnake" },
                            }
                            },
                            new() { GroupId = "_BODY_", Descriptors = new()
                            {
                                new() { Id = "_BODY_CENTIPEDE", Name = "_Body_Centipede" },
                                new() { Id = "_BODY_LEECH", Name = "_Body_Leech" },
                                new() { Id = "_BODY_SANDWORM", Name = "_Body_SandWorm" },
                                new() { Id = "_BODY_SANDSNAKE", Name = "_Body_SandSnake" },
                            }
                            },
                            new() { GroupId = "_TAIL_", Descriptors = new()
                            {
                                new() { Id = "_TAIL_CENTIPEDE", Name = "_Tail_Centipede" },
                                new() { Id = "_TAIL_LEECH", Name = "_Tail_Leech" },
                                new() { Id = "_TAIL_SANDWORM", Name = "_Tail_SandWorm" },
                                new() { Id = "_TAIL_SANDSNAKE", Name = "_Tail_SandSnake" },
                            }
                            },
                        }
                        },
                        new() { Id = "_SET_GPXRARE", Name = "_Set_GPxRARE" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "GROUNDCREATURE", FriendlyName = "GROUNDCREATURE", Details = new()
        {
            new() { GroupId = "_GROUND_", Descriptors = new()
            {
                new() { Id = "_GROUND_EYESTALK", Name = "_Ground_EyeStalk" },
                new() { Id = "_GROUND_RODENT", Name = "_Ground_Rodent" },
                new() { Id = "_GROUND_SMOKEBURST", Name = "_Ground_SmokeBurst" },
                new() { Id = "_GROUND_SPORE", Name = "_Ground_Spore" },
                new() { Id = "_GROUND_TENTACLES", Name = "_Ground_Tentacles" },
            }
            },
        }
        },
        new() { CreatureId = "GRUNT", FriendlyName = "GRUNT", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_GRUNT", Name = "_Head_Grunt", Children = new()
                {
                    new() { GroupId = "_GHACCS_", Descriptors = new()
                    {
                        new() { Id = "_GHACCS_2", Name = "_GHAccs_2", Children = new()
                        {
                            new() { GroupId = "_GH2ACCS_", Descriptors = new()
                            {
                                new() { Id = "_GH2ACCS_E", Name = "_GH2Accs_E" },
                                new() { Id = "_GH2ACCS_A", Name = "_GH2Accs_A" },
                                new() { Id = "_GH2ACCS_B", Name = "_GH2Accs_B" },
                                new() { Id = "_GH2ACCS_C", Name = "_GH2Accs_C" },
                                new() { Id = "_GH2ACCS_D", Name = "_GH2Accs_D" },
                            }
                            },
                        }
                        },
                        new() { Id = "_GHACCS_1", Name = "_GHAccs_1", Children = new()
                        {
                            new() { GroupId = "_GH1ACCS_", Descriptors = new()
                            {
                                new() { Id = "_GH1ACCS_A", Name = "_GH1Accs_A" },
                                new() { Id = "_GH1ACCS_B", Name = "_GH1Accs_B" },
                                new() { Id = "_GH1ACCS_C", Name = "_GH1Accs_C" },
                                new() { Id = "_GH1ACCS_D", Name = "_GH1Accs_D" },
                                new() { Id = "_GH1ACCS_E", Name = "_GH1Accs_E" },
                                new() { Id = "_GH1ACCS_F", Name = "_GH1Accs_F" },
                                new() { Id = "_GH1ACCS_G", Name = "_GH1Accs_G" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TURTLE", Name = "_Head_Turtle", Children = new()
                {
                    new() { GroupId = "_ACC_", Descriptors = new()
                    {
                        new() { Id = "_ACC_NONE", Name = "_Acc_none" },
                        new() { Id = "_ACC_5", Name = "_Acc_5", Children = new()
                        {
                            new() { GroupId = "_A_", Descriptors = new()
                            {
                                new() { Id = "_A_4", Name = "_A_4" },
                                new() { Id = "_A_3", Name = "_A_3" },
                                new() { Id = "_A_2", Name = "_A_2" },
                                new() { Id = "_A_1", Name = "_A_1" },
                                new() { Id = "_A_5", Name = "_A_5" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_4", Name = "_Acc_4" },
                        new() { Id = "_ACC_3", Name = "_Acc_3" },
                        new() { Id = "_ACC_1", Name = "_Acc_1", Children = new()
                        {
                            new() { GroupId = "_ACC3_", Descriptors = new()
                            {
                                new() { Id = "_ACC3_1", Name = "_Acc3_1" },
                                new() { Id = "_ACC3_NONE", Name = "_Acc3_none" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_2", Name = "_Acc_2", Children = new()
                        {
                            new() { GroupId = "_ACC2_", Descriptors = new()
                            {
                                new() { Id = "_ACC2_1", Name = "_Acc2_1" },
                                new() { Id = "_ACC2_NONE", Name = "_Acc2_none" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_ACB_", Descriptors = new()
                    {
                        new() { Id = "_ACB_NONE", Name = "_AcB_none" },
                        new() { Id = "_ACB_EARS3", Name = "_AcB_Ears3" },
                        new() { Id = "_ACB_EARS2", Name = "_AcB_Ears2" },
                        new() { Id = "_ACB_EARS", Name = "_AcB_Ears" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_GRUNT", Name = "_Body_Grunt" },
                new() { Id = "_BODY_GRUNTFLOATXRARE", Name = "_Body_GruntFloatxRARE", Children = new()
                {
                    new() { GroupId = "_WING_", Descriptors = new()
                    {
                        new() { Id = "_WING_1", Name = "_Wing_1" },
                        new() { Id = "_WING_2", Name = "_Wing_2" },
                        new() { Id = "_WING_3", Name = "_Wing_3" },
                        new() { Id = "_WING_4", Name = "_Wing_4" },
                        new() { Id = "_WING_5", Name = "_Wing_5" },
                        new() { Id = "_WING_6", Name = "_Wing_6" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_MESH_", Descriptors = new()
            {
                new() { Id = "_MESH_GRUNT", Name = "_Mesh_Grunt", Children = new()
                {
                    new() { GroupId = "_BGH_", Descriptors = new()
                    {
                        new() { Id = "_BGH_CLAWSLONG", Name = "_BGH_ClawsLong" },
                        new() { Id = "_BGH_CLAWSSHORT", Name = "_BGH_ClawsShort" },
                    }
                    },
                    new() { GroupId = "_BGF_", Descriptors = new()
                    {
                        new() { Id = "_BGF_CLAWSLONG", Name = "_BGF_ClawsLong" },
                        new() { Id = "_BGF_CLAWSSHORT", Name = "_BGF_ClawsShort" },
                    }
                    },
                    new() { GroupId = "_BGM_", Descriptors = new()
                    {
                        new() { Id = "_BGM_SHELLXRARE", Name = "_BGM_ShellxRARE" },
                        new() { Id = "_BGM_SHOULDERSPIKESXRARE", Name = "_BGM_ShoulderSpikesxRARE" },
                        new() { Id = "_BGM_SHAWL", Name = "_BGM_Shawl" },
                        new() { Id = "_BGM_NONE", Name = "_BGM_none" },
                    }
                    },
                }
                },
                new() { Id = "_MESH_TURTLE", Name = "_Mesh_Turtle", Children = new()
                {
                    new() { GroupId = "_ACCB_", Descriptors = new()
                    {
                        new() { Id = "_ACCB_BLONGCLW", Name = "_AccB_BLongClw" },
                        new() { Id = "_ACCB_BFEATHERS", Name = "_AccB_BFeathers" },
                        new() { Id = "_ACCB_FEATHERS", Name = "_AccB_Feathers" },
                        new() { Id = "_ACCB_NONE", Name = "_AccB_none" },
                    }
                    },
                    new() { GroupId = "_TBACC_", Descriptors = new()
                    {
                        new() { Id = "_TBACC_NONE", Name = "_TBAcc_none" },
                        new() { Id = "_TBACC_2", Name = "_TBAcc_2" },
                        new() { Id = "_TBACC_1", Name = "_TBAcc_1" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "GRUNTCREATETEST", FriendlyName = "GRUNTCREATETEST", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_GRUNT", Name = "_Head_Grunt", Children = new()
                {
                    new() { GroupId = "_GHACCS_", Descriptors = new()
                    {
                        new() { Id = "_GHACCS_1", Name = "_GHAccs_1", Children = new()
                        {
                            new() { GroupId = "_GH1ACCS_", Descriptors = new()
                            {
                                new() { Id = "_GH1ACCS_E", Name = "_GH1Accs_E" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_GRUNT", Name = "_Body_Grunt" },
            }
            },
            new() { GroupId = "_MESH_", Descriptors = new()
            {
                new() { Id = "_MESH_GRUNT", Name = "_Mesh_Grunt", Children = new()
                {
                    new() { GroupId = "_BGH_", Descriptors = new()
                    {
                        new() { Id = "_BGH_CLAWSLONG", Name = "_BGH_ClawsLong" },
                    }
                    },
                    new() { GroupId = "_BGF_", Descriptors = new()
                    {
                        new() { Id = "_BGF_CLAWSLONG", Name = "_BGF_ClawsLong" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "GRUNTTEST", FriendlyName = "GRUNTTEST", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_GRUNT", Name = "_Head_Grunt", Children = new()
                {
                    new() { GroupId = "_GHACCS_", Descriptors = new()
                    {
                        new() { Id = "_GHACCS_1", Name = "_GHAccs_1", Children = new()
                        {
                            new() { GroupId = "_GH1ACCS_", Descriptors = new()
                            {
                                new() { Id = "_GH1ACCS_E", Name = "_GH1Accs_E" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_GRUNT", Name = "_Body_Grunt" },
            }
            },
            new() { GroupId = "_MESH_", Descriptors = new()
            {
                new() { Id = "_MESH_GRUNT", Name = "_Mesh_Grunt", Children = new()
                {
                    new() { GroupId = "_BGH_", Descriptors = new()
                    {
                        new() { Id = "_BGH_CLAWSLONG", Name = "_BGH_ClawsLong" },
                    }
                    },
                    new() { GroupId = "_BGF_", Descriptors = new()
                    {
                        new() { Id = "_BGF_CLAWSLONG", Name = "_BGF_ClawsLong" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "PROC_JELLYFISH", FriendlyName = "JELLYFISH", Details = new()
        {
            new() { GroupId = "_PROC_", Descriptors = new()
            {
                new() { Id = "_PROC_JELLYFISH", Name = "_Proc_Jellyfish", Children = new()
                {
                    new() { GroupId = "_FBBODY_", Descriptors = new()
                    {
                        new() { Id = "_FBBODY_1", Name = "_FbBody_1", Children = new()
                        {
                            new() { GroupId = "_TYPE_", Descriptors = new()
                            {
                                new() { Id = "_TYPE_1", Name = "_Type_1", Children = new()
                                {
                                    new() { GroupId = "_EYESTALKS_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESTALKS_3", Name = "_Eyestalks_3", Children = new()
                                        {
                                            new() { GroupId = "_BALLZ_", Descriptors = new()
                                            {
                                                new() { Id = "_BALLZ_1", Name = "_Ballz_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_EYESTALKS_1", Name = "_Eyestalks_1" },
                                        new() { Id = "_EYESTALKS_2", Name = "_Eyestalks_2" },
                                    }
                                    },
                                    new() { GroupId = "_TENDRILS_", Descriptors = new()
                                    {
                                        new() { Id = "_TENDRILS_2", Name = "_Tendrils_2" },
                                        new() { Id = "_TENDRILS_1", Name = "_Tendrils_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_FBBODY_2", Name = "_FbBody_2", Children = new()
                        {
                            new() { GroupId = "_BOTTOM_", Descriptors = new()
                            {
                                new() { Id = "_BOTTOM_4", Name = "_Bottom_4" },
                                new() { Id = "_BOTTOM_2", Name = "_Bottom_2" },
                                new() { Id = "_BOTTOM_5", Name = "_Bottom_5" },
                                new() { Id = "_BOTTOM_1", Name = "_Bottom_1" },
                                new() { Id = "_BOTTOM_3", Name = "_Bottom_3", Children = new()
                                {
                                    new() { GroupId = "_PLINGERS_", Descriptors = new()
                                    {
                                        new() { Id = "_PLINGERS_1", Name = "_Plingers_1" },
                                        new() { Id = "_PLINGERS_2", Name = "_Plingers_2" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_TOP_", Descriptors = new()
                            {
                                new() { Id = "_TOP_1", Name = "_Top_1" },
                                new() { Id = "_TOP_2", Name = "_Top_2", Children = new()
                                {
                                    new() { GroupId = "_EYESD_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESD_2", Name = "_EyesD_2" },
                                        new() { Id = "_EYESD_1", Name = "_EyesD_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TOP_3", Name = "_Top_3", Children = new()
                                {
                                    new() { GroupId = "_FILL_", Descriptors = new()
                                    {
                                        new() { Id = "_FILL_1", Name = "_Fill_1" },
                                        new() { Id = "_FILL_2", Name = "_Fill_2" },
                                        new() { Id = "_FILL_3", Name = "_Fill_3", Children = new()
                                        {
                                            new() { GroupId = "_TIP_", Descriptors = new()
                                            {
                                                new() { Id = "_TIP_1", Name = "_Tip_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "LARGEBUTTERFLY", FriendlyName = "LARGEBUTTERFLY", Details = new()
        {
            new() { GroupId = "_FWINGS_", Descriptors = new()
            {
                new() { Id = "_FWINGS_01", Name = "_FWings_01" },
                new() { Id = "_FWINGS_02", Name = "_FWings_02" },
                new() { Id = "_FWINGS_03", Name = "_FWings_03" },
            }
            },
            new() { GroupId = "_MOTHBODY_", Descriptors = new()
            {
                new() { Id = "_MOTHBODY_01", Name = "_MothBody_01" },
                new() { Id = "_MOTHBODY_L01", Name = "_MothBody_L01", Children = new()
                {
                    new() { GroupId = "_TWINGS_", Descriptors = new()
                    {
                        new() { Id = "_TWINGS_01", Name = "_TWings_01" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BWINGS_", Descriptors = new()
            {
                new() { Id = "_BWINGS_01", Name = "_BWings_01" },
                new() { Id = "_BWINGS_02", Name = "_BWings_02" },
                new() { Id = "_BWINGS_03", Name = "_BWings_03" },
                new() { Id = "_BWINGS_04", Name = "_BWings_04" },
                new() { Id = "_BWINGS_NULL", Name = "_BWings_Null" },
            }
            },
            new() { GroupId = "_MWINGS_", Descriptors = new()
            {
                new() { Id = "_MWINGS_01", Name = "_MWings_01" },
                new() { Id = "_MWINGS_02", Name = "_MWings_02" },
                new() { Id = "_MWINGS_NULL", Name = "_MWings_Null" },
            }
            },
        }
        },
        new() { CreatureId = "DIGGER", FriendlyName = "MOLE", Details = new()
        {
            new() { GroupId = "_DIGGER_", Descriptors = new()
            {
                new() { Id = "_DIGGER_A", Name = "_Digger_A" },
                new() { Id = "_DIGGER_B", Name = "_Digger_B" },
                new() { Id = "_DIGGER_C", Name = "_Digger_C", Children = new()
                {
                    new() { GroupId = "_SPIKES_", Descriptors = new()
                    {
                        new() { Id = "_SPIKES_NULL", Name = "_Spikes_Null" },
                        new() { Id = "_SPIKES_A", Name = "_Spikes_A" },
                    }
                    },
                }
                },
                new() { Id = "_DIGGER_D", Name = "_Digger_D" },
            }
            },
        }
        },
        new() { CreatureId = "PETACCESSORIES", FriendlyName = "PETACCESSORIES", Details = new()
        {
            new() { GroupId = "_GRP_", Descriptors = new()
            {
                new() { Id = "_GRP_1", Name = "_GRP_1", Children = new()
                {
                    new() { GroupId = "_ACC_", Descriptors = new()
                    {
                        new() { Id = "_ACC_CARGOCYLINDER", Name = "_ACC_CargoCylinder" },
                        new() { Id = "_ACC_CONTAINERS", Name = "_ACC_Containers" },
                        new() { Id = "_ACC_SHIELDARMOUR", Name = "_ACC_ShieldArmour", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL", Name = "_Decal_Null" },
                                new() { Id = "_DECAL_LGA", Name = "_Decal_LgA" },
                                new() { Id = "_DECAL_LGB", Name = "_Decal_LgB" },
                                new() { Id = "_DECAL_NMS", Name = "_Decal_NMS" },
                                new() { Id = "_DECAL_NUMA", Name = "_Decal_NumA" },
                                new() { Id = "_DECAL_NUMB", Name = "_Decal_NumB" },
                                new() { Id = "_DECAL_NUMC", Name = "_Decal_NumC" },
                                new() { Id = "_DECAL_NUMD", Name = "_Decal_NumD" },
                                new() { Id = "_DECAL_NUME", Name = "_Decal_NumE" },
                                new() { Id = "_DECAL_NUMF", Name = "_Decal_NumF" },
                                new() { Id = "_DECAL_NUMG", Name = "_Decal_NumG" },
                                new() { Id = "_DECAL_NUMH", Name = "_Decal_NumH" },
                                new() { Id = "_DECAL_NUMI", Name = "_Decal_NumI" },
                                new() { Id = "_DECAL_NUMJ", Name = "_Decal_NumJ" },
                                new() { Id = "_DECAL_SPA", Name = "_Decal_SpA" },
                                new() { Id = "_DECAL_SPB", Name = "_Decal_SpB" },
                                new() { Id = "_DECAL_SPC", Name = "_Decal_SpC" },
                                new() { Id = "_DECAL_SPD", Name = "_Decal_SpD" },
                                new() { Id = "_DECAL_SPE", Name = "_Decal_SpE" },
                                new() { Id = "_DECAL_SPF", Name = "_Decal_SpF" },
                                new() { Id = "_DECAL_SIMPLEA", Name = "_Decal_SimpleA" },
                                new() { Id = "_DECAL_SIMPLEB", Name = "_Decal_SimpleB" },
                                new() { Id = "_DECAL_SIMPLEC", Name = "_Decal_SimpleC" },
                                new() { Id = "_DECAL_SIMPLED", Name = "_Decal_SimpleD" },
                                new() { Id = "_DECAL_VISA", Name = "_Decal_VisA" },
                                new() { Id = "_DECAL_VISB", Name = "_Decal_VisB" },
                                new() { Id = "_DECAL_VISC", Name = "_Decal_VisC" },
                                new() { Id = "_DECAL_VISD", Name = "_Decal_VisD" },
                                new() { Id = "_DECAL_VISE", Name = "_Decal_VisE" },
                                new() { Id = "_DECAL_SPG", Name = "_Decal_SpG" },
                                new() { Id = "_DECAL_SPH", Name = "_Decal_SpH" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_SOLARBATTERY", Name = "_ACC_SolarBattery" },
                        new() { Id = "_ACC_TANK", Name = "_ACC_Tank", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL3", Name = "_Decal_Null3" },
                                new() { Id = "_DECAL_LGA3", Name = "_Decal_LgA3" },
                                new() { Id = "_DECAL_LGB3", Name = "_Decal_LgB3" },
                                new() { Id = "_DECAL_NMS3", Name = "_Decal_NMS3" },
                                new() { Id = "_DECAL_NUMA3", Name = "_Decal_NumA3" },
                                new() { Id = "_DECAL_NUMB3", Name = "_Decal_NumB3" },
                                new() { Id = "_DECAL_NUMC3", Name = "_Decal_NumC3" },
                                new() { Id = "_DECAL_NUMD3", Name = "_Decal_NumD3" },
                                new() { Id = "_DECAL_NUME3", Name = "_Decal_NumE3" },
                                new() { Id = "_DECAL_NUMF3", Name = "_Decal_NumF3" },
                                new() { Id = "_DECAL_NUMG3", Name = "_Decal_NumG3" },
                                new() { Id = "_DECAL_NUMH3", Name = "_Decal_NumH3" },
                                new() { Id = "_DECAL_NUMI3", Name = "_Decal_NumI3" },
                                new() { Id = "_DECAL_NUMJ3", Name = "_Decal_NumJ3" },
                                new() { Id = "_DECAL_SPA3", Name = "_Decal_SpA3" },
                                new() { Id = "_DECAL_SPB3", Name = "_Decal_SpB3" },
                                new() { Id = "_DECAL_SPC3", Name = "_Decal_SpC3" },
                                new() { Id = "_DECAL_SPD3", Name = "_Decal_SpD3" },
                                new() { Id = "_DECAL_SPE3", Name = "_Decal_SpE3" },
                                new() { Id = "_DECAL_SPF3", Name = "_Decal_SpF3" },
                                new() { Id = "_DECAL_SIMPLEA3", Name = "_Decal_SimpleA3" },
                                new() { Id = "_DECAL_SIMPLEB3", Name = "_Decal_SimpleB3" },
                                new() { Id = "_DECAL_SIMPLEC3", Name = "_Decal_SimpleC3" },
                                new() { Id = "_DECAL_SIMPLED3", Name = "_Decal_SimpleD3" },
                                new() { Id = "_DECAL_VISA3", Name = "_Decal_VisA3" },
                                new() { Id = "_DECAL_VISB3", Name = "_Decal_VisB3" },
                                new() { Id = "_DECAL_VISC3", Name = "_Decal_VisC3" },
                                new() { Id = "_DECAL_VISD3", Name = "_Decal_VisD3" },
                                new() { Id = "_DECAL_VISE3", Name = "_Decal_VisE3" },
                                new() { Id = "_DECAL_SPG3", Name = "_Decal_SpG3" },
                                new() { Id = "_DECAL_SPH3", Name = "_Decal_SpH3" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_WINGPANEL", Name = "_ACC_WingPanel", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL2", Name = "_Decal_Null2" },
                                new() { Id = "_DECAL_LGA2", Name = "_Decal_LgA2" },
                                new() { Id = "_DECAL_LGB2", Name = "_Decal_LgB2" },
                                new() { Id = "_DECAL_NMS2", Name = "_Decal_NMS2" },
                                new() { Id = "_DECAL_NUMA2", Name = "_Decal_NumA2" },
                                new() { Id = "_DECAL_NUMB2", Name = "_Decal_NumB2" },
                                new() { Id = "_DECAL_NUMC2", Name = "_Decal_NumC2" },
                                new() { Id = "_DECAL_NUMD2", Name = "_Decal_NumD2" },
                                new() { Id = "_DECAL_NUME2", Name = "_Decal_NumE2" },
                                new() { Id = "_DECAL_NUMF2", Name = "_Decal_NumF2" },
                                new() { Id = "_DECAL_NUMG2", Name = "_Decal_NumG2" },
                                new() { Id = "_DECAL_NUMH2", Name = "_Decal_NumH2" },
                                new() { Id = "_DECAL_NUMI2", Name = "_Decal_NumI2" },
                                new() { Id = "_DECAL_NUMJ2", Name = "_Decal_NumJ2" },
                                new() { Id = "_DECAL_SPA2", Name = "_Decal_SpA2" },
                                new() { Id = "_DECAL_SPB2", Name = "_Decal_SpB2" },
                                new() { Id = "_DECAL_SPC2", Name = "_Decal_SpC2" },
                                new() { Id = "_DECAL_SPD2", Name = "_Decal_SpD2" },
                                new() { Id = "_DECAL_SPE2", Name = "_Decal_SpE2" },
                                new() { Id = "_DECAL_SPF2", Name = "_Decal_SpF2" },
                                new() { Id = "_DECAL_SIMPLEA2", Name = "_Decal_SimpleA2" },
                                new() { Id = "_DECAL_SIMPLEB2", Name = "_Decal_SimpleB2" },
                                new() { Id = "_DECAL_SIMPLEC2", Name = "_Decal_SimpleC2" },
                                new() { Id = "_DECAL_SIMPLED2", Name = "_Decal_SimpleD2" },
                                new() { Id = "_DECAL_VISA2", Name = "_Decal_VisA2" },
                                new() { Id = "_DECAL_VISB2", Name = "_Decal_VisB2" },
                                new() { Id = "_DECAL_VISC2", Name = "_Decal_VisC2" },
                                new() { Id = "_DECAL_VISD2", Name = "_Decal_VisD2" },
                                new() { Id = "_DECAL_VISE2", Name = "_Decal_VisE2" },
                                new() { Id = "_DECAL_SPG2", Name = "_Decal_SpG2" },
                                new() { Id = "_DECAL_SPH2", Name = "_Decal_SpH2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_TRAVELPACK", Name = "_ACC_TravelPack" },
                        new() { Id = "_ACC_SPACEPACK", Name = "_ACC_SpacePack", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL4", Name = "_Decal_Null4" },
                                new() { Id = "_DECAL_LGA4", Name = "_Decal_LgA4" },
                                new() { Id = "_DECAL_LGB4", Name = "_Decal_LgB4" },
                                new() { Id = "_DECAL_NMS4", Name = "_Decal_NMS4" },
                                new() { Id = "_DECAL_NUMA4", Name = "_Decal_NumA4" },
                                new() { Id = "_DECAL_NUMB4", Name = "_Decal_NumB4" },
                                new() { Id = "_DECAL_NUMC4", Name = "_Decal_NumC4" },
                                new() { Id = "_DECAL_NUMD4", Name = "_Decal_NumD4" },
                                new() { Id = "_DECAL_NUME4", Name = "_Decal_NumE4" },
                                new() { Id = "_DECAL_NUMF4", Name = "_Decal_NumF4" },
                                new() { Id = "_DECAL_NUMG4", Name = "_Decal_NumG4" },
                                new() { Id = "_DECAL_NUMH4", Name = "_Decal_NumH4" },
                                new() { Id = "_DECAL_NUMI4", Name = "_Decal_NumI4" },
                                new() { Id = "_DECAL_NUMJ4", Name = "_Decal_NumJ4" },
                                new() { Id = "_DECAL_SPA4", Name = "_Decal_SpA4" },
                                new() { Id = "_DECAL_SPB4", Name = "_Decal_SpB4" },
                                new() { Id = "_DECAL_SPC4", Name = "_Decal_SpC4" },
                                new() { Id = "_DECAL_SPD4", Name = "_Decal_SpD4" },
                                new() { Id = "_DECAL_SPE4", Name = "_Decal_SpE4" },
                                new() { Id = "_DECAL_SPF4", Name = "_Decal_SpF4" },
                                new() { Id = "_DECAL_SIMPLEA4", Name = "_Decal_SimpleA4" },
                                new() { Id = "_DECAL_SIMPLEB4", Name = "_Decal_SimpleB4" },
                                new() { Id = "_DECAL_SIMPLEC4", Name = "_Decal_SimpleC4" },
                                new() { Id = "_DECAL_SIMPLED4", Name = "_Decal_SimpleD4" },
                                new() { Id = "_DECAL_VISA4", Name = "_Decal_VisA4" },
                                new() { Id = "_DECAL_VISB4", Name = "_Decal_VisB4" },
                                new() { Id = "_DECAL_VISC4", Name = "_Decal_VisC4" },
                                new() { Id = "_DECAL_VISD4", Name = "_Decal_VisD4" },
                                new() { Id = "_DECAL_VISE4", Name = "_Decal_VisE4" },
                                new() { Id = "_DECAL_SPG4", Name = "_Decal_SpG4" },
                                new() { Id = "_DECAL_SPH4", Name = "_Decal_SpH4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_CARGOLONG", Name = "_ACC_CargoLong" },
                        new() { Id = "_ACC_ANTENNAE", Name = "_ACC_Antennae", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL5", Name = "_Decal_Null5" },
                                new() { Id = "_DECAL_LGA5", Name = "_Decal_LgA5" },
                                new() { Id = "_DECAL_LGB5", Name = "_Decal_LgB5" },
                                new() { Id = "_DECAL_NMS5", Name = "_Decal_NMS5" },
                                new() { Id = "_DECAL_NUMA5", Name = "_Decal_NumA5" },
                                new() { Id = "_DECAL_NUMB5", Name = "_Decal_NumB5" },
                                new() { Id = "_DECAL_NUMC5", Name = "_Decal_NumC5" },
                                new() { Id = "_DECAL_NUMD5", Name = "_Decal_NumD5" },
                                new() { Id = "_DECAL_NUME5", Name = "_Decal_NumE5" },
                                new() { Id = "_DECAL_NUMF5", Name = "_Decal_NumF5" },
                                new() { Id = "_DECAL_NUMG5", Name = "_Decal_NumG5" },
                                new() { Id = "_DECAL_NUMH5", Name = "_Decal_NumH5" },
                                new() { Id = "_DECAL_NUMI5", Name = "_Decal_NumI5" },
                                new() { Id = "_DECAL_NUMJ5", Name = "_Decal_NumJ5" },
                                new() { Id = "_DECAL_SPA5", Name = "_Decal_SpA5" },
                                new() { Id = "_DECAL_SPB5", Name = "_Decal_SpB5" },
                                new() { Id = "_DECAL_SPC5", Name = "_Decal_SpC5" },
                                new() { Id = "_DECAL_SPD5", Name = "_Decal_SpD5" },
                                new() { Id = "_DECAL_SPE5", Name = "_Decal_SpE5" },
                                new() { Id = "_DECAL_SPF5", Name = "_Decal_SpF5" },
                                new() { Id = "_DECAL_SIMPLEA5", Name = "_Decal_SimpleA5" },
                                new() { Id = "_DECAL_SIMPLEB5", Name = "_Decal_SimpleB5" },
                                new() { Id = "_DECAL_SIMPLEC5", Name = "_Decal_SimpleC5" },
                                new() { Id = "_DECAL_SIMPLED5", Name = "_Decal_SimpleD5" },
                                new() { Id = "_DECAL_VISA5", Name = "_Decal_VisA5" },
                                new() { Id = "_DECAL_VISB5", Name = "_Decal_VisB5" },
                                new() { Id = "_DECAL_VISC5", Name = "_Decal_VisC5" },
                                new() { Id = "_DECAL_VISD5", Name = "_Decal_VisD5" },
                                new() { Id = "_DECAL_VISE5", Name = "_Decal_VisE5" },
                                new() { Id = "_DECAL_SPG5", Name = "_Decal_SpG5" },
                                new() { Id = "_DECAL_SPH5", Name = "_Decal_SpH5" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_TOOLBELT", Name = "_ACC_Toolbelt" },
                        new() { Id = "_ACC_COMPUTER", Name = "_ACC_Computer" },
                        new() { Id = "_ACC_LCANISTERS", Name = "_ACC_LCanisters" },
                        new() { Id = "_ACC_LENERGYCOIL", Name = "_ACC_LEnergyCoil" },
                        new() { Id = "_ACC_LFRIGATETURRET", Name = "_ACC_LFrigateTurret", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL8", Name = "_Decal_Null8" },
                                new() { Id = "_DECAL_LGA8", Name = "_Decal_LgA8" },
                                new() { Id = "_DECAL_LGB8", Name = "_Decal_LgB8" },
                                new() { Id = "_DECAL_NMS8", Name = "_Decal_NMS8" },
                                new() { Id = "_DECAL_NUMA8", Name = "_Decal_NumA8" },
                                new() { Id = "_DECAL_NUMB8", Name = "_Decal_NumB8" },
                                new() { Id = "_DECAL_NUMC8", Name = "_Decal_NumC8" },
                                new() { Id = "_DECAL_NUMD8", Name = "_Decal_NumD8" },
                                new() { Id = "_DECAL_NUME8", Name = "_Decal_NumE8" },
                                new() { Id = "_DECAL_NUMF8", Name = "_Decal_NumF8" },
                                new() { Id = "_DECAL_NUMG8", Name = "_Decal_NumG8" },
                                new() { Id = "_DECAL_NUMH8", Name = "_Decal_NumH8" },
                                new() { Id = "_DECAL_NUMI8", Name = "_Decal_NumI8" },
                                new() { Id = "_DECAL_NUMJ8", Name = "_Decal_NumJ8" },
                                new() { Id = "_DECAL_SPA8", Name = "_Decal_SpA8" },
                                new() { Id = "_DECAL_SPB8", Name = "_Decal_SpB8" },
                                new() { Id = "_DECAL_SPC8", Name = "_Decal_SpC8" },
                                new() { Id = "_DECAL_SPD8", Name = "_Decal_SpD8" },
                                new() { Id = "_DECAL_SPE8", Name = "_Decal_SpE8" },
                                new() { Id = "_DECAL_SPF8", Name = "_Decal_SpF8" },
                                new() { Id = "_DECAL_SIMPLEA8", Name = "_Decal_SimpleA8" },
                                new() { Id = "_DECAL_SIMPLEB8", Name = "_Decal_SimpleB8" },
                                new() { Id = "_DECAL_SIMPLEC8", Name = "_Decal_SimpleC8" },
                                new() { Id = "_DECAL_SIMPLED8", Name = "_Decal_SimpleD8" },
                                new() { Id = "_DECAL_VISA8", Name = "_Decal_VisA8" },
                                new() { Id = "_DECAL_VISB8", Name = "_Decal_VisB8" },
                                new() { Id = "_DECAL_VISC8", Name = "_Decal_VisC8" },
                                new() { Id = "_DECAL_VISD8", Name = "_Decal_VisD8" },
                                new() { Id = "_DECAL_VISE8", Name = "_Decal_VisE8" },
                                new() { Id = "_DECAL_SPG8", Name = "_Decal_SpG8" },
                                new() { Id = "_DECAL_SPH8", Name = "_Decal_SpH8" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_LHEADLIGHTS", Name = "_ACC_LHeadLights" },
                        new() { Id = "_ACC_LARMOURPLATE", Name = "_ACC_LArmourPlate", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL6", Name = "_Decal_Null6" },
                                new() { Id = "_DECAL_LGA6", Name = "_Decal_LgA6" },
                                new() { Id = "_DECAL_LGB6", Name = "_Decal_LgB6" },
                                new() { Id = "_DECAL_NMS6", Name = "_Decal_NMS6" },
                                new() { Id = "_DECAL_NUMA6", Name = "_Decal_NumA6" },
                                new() { Id = "_DECAL_NUMB6", Name = "_Decal_NumB6" },
                                new() { Id = "_DECAL_NUMC6", Name = "_Decal_NumC6" },
                                new() { Id = "_DECAL_NUMD6", Name = "_Decal_NumD6" },
                                new() { Id = "_DECAL_NUME6", Name = "_Decal_NumE6" },
                                new() { Id = "_DECAL_NUMF6", Name = "_Decal_NumF6" },
                                new() { Id = "_DECAL_NUMG6", Name = "_Decal_NumG6" },
                                new() { Id = "_DECAL_NUMH6", Name = "_Decal_NumH6" },
                                new() { Id = "_DECAL_NUMI6", Name = "_Decal_NumI6" },
                                new() { Id = "_DECAL_NUMJ6", Name = "_Decal_NumJ6" },
                                new() { Id = "_DECAL_SPA6", Name = "_Decal_SpA6" },
                                new() { Id = "_DECAL_SPB6", Name = "_Decal_SpB6" },
                                new() { Id = "_DECAL_SPC6", Name = "_Decal_SpC6" },
                                new() { Id = "_DECAL_SPD6", Name = "_Decal_SpD6" },
                                new() { Id = "_DECAL_SPE6", Name = "_Decal_SpE6" },
                                new() { Id = "_DECAL_SPF6", Name = "_Decal_SpF6" },
                                new() { Id = "_DECAL_SIMPLEA6", Name = "_Decal_SimpleA6" },
                                new() { Id = "_DECAL_SIMPLEB6", Name = "_Decal_SimpleB6" },
                                new() { Id = "_DECAL_SIMPLEC6", Name = "_Decal_SimpleC6" },
                                new() { Id = "_DECAL_SIMPLED6", Name = "_Decal_SimpleD6" },
                                new() { Id = "_DECAL_VISA6", Name = "_Decal_VisA6" },
                                new() { Id = "_DECAL_VISB6", Name = "_Decal_VisB6" },
                                new() { Id = "_DECAL_VISC6", Name = "_Decal_VisC6" },
                                new() { Id = "_DECAL_VISD6", Name = "_Decal_VisD6" },
                                new() { Id = "_DECAL_VISE6", Name = "_Decal_VisE6" },
                                new() { Id = "_DECAL_SPG6", Name = "_Decal_SpG6" },
                                new() { Id = "_DECAL_SPH6", Name = "_Decal_SpH6" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_LTURRET", Name = "_ACC_LTurret" },
                        new() { Id = "_ACC_LSUPPORTSYSTEM", Name = "_ACC_LSupportSystem" },
                        new() { Id = "_ACC_LROYALARMOUR", Name = "_ACC_LRoyalArmour" },
                        new() { Id = "_ACC_RCANISTERS", Name = "_ACC_RCanisters" },
                        new() { Id = "_ACC_RENERGYCOIL", Name = "_ACC_REnergyCoil" },
                        new() { Id = "_ACC_RFRIGATETURRET", Name = "_ACC_RFrigateTurret", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL9", Name = "_Decal_Null9" },
                                new() { Id = "_DECAL_LGA9", Name = "_Decal_LgA9" },
                                new() { Id = "_DECAL_LGB9", Name = "_Decal_LgB9" },
                                new() { Id = "_DECAL_NMS9", Name = "_Decal_NMS9" },
                                new() { Id = "_DECAL_NUMA9", Name = "_Decal_NumA9" },
                                new() { Id = "_DECAL_NUMB9", Name = "_Decal_NumB9" },
                                new() { Id = "_DECAL_NUMC9", Name = "_Decal_NumC9" },
                                new() { Id = "_DECAL_NUMD9", Name = "_Decal_NumD9" },
                                new() { Id = "_DECAL_NUME9", Name = "_Decal_NumE9" },
                                new() { Id = "_DECAL_NUMF9", Name = "_Decal_NumF9" },
                                new() { Id = "_DECAL_NUMG9", Name = "_Decal_NumG9" },
                                new() { Id = "_DECAL_NUMH9", Name = "_Decal_NumH9" },
                                new() { Id = "_DECAL_NUMI9", Name = "_Decal_NumI9" },
                                new() { Id = "_DECAL_NUMJ9", Name = "_Decal_NumJ9" },
                                new() { Id = "_DECAL_SPA9", Name = "_Decal_SpA9" },
                                new() { Id = "_DECAL_SPB9", Name = "_Decal_SpB9" },
                                new() { Id = "_DECAL_SPC9", Name = "_Decal_SpC9" },
                                new() { Id = "_DECAL_SPD9", Name = "_Decal_SpD9" },
                                new() { Id = "_DECAL_SPE9", Name = "_Decal_SpE9" },
                                new() { Id = "_DECAL_SPF9", Name = "_Decal_SpF9" },
                                new() { Id = "_DECAL_SIMPLEA9", Name = "_Decal_SimpleA9" },
                                new() { Id = "_DECAL_SIMPLEB9", Name = "_Decal_SimpleB9" },
                                new() { Id = "_DECAL_SIMPLEC9", Name = "_Decal_SimpleC9" },
                                new() { Id = "_DECAL_SIMPLED9", Name = "_Decal_SimpleD9" },
                                new() { Id = "_DECAL_VISA9", Name = "_Decal_VisA9" },
                                new() { Id = "_DECAL_VISB9", Name = "_Decal_VisB9" },
                                new() { Id = "_DECAL_VISC9", Name = "_Decal_VisC9" },
                                new() { Id = "_DECAL_VISD9", Name = "_Decal_VisD9" },
                                new() { Id = "_DECAL_VISE9", Name = "_Decal_VisE9" },
                                new() { Id = "_DECAL_SPG9", Name = "_Decal_SpG9" },
                                new() { Id = "_DECAL_SPH9", Name = "_Decal_SpH9" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_RHEADLIGHTS", Name = "_ACC_RHeadLights" },
                        new() { Id = "_ACC_RARMOURPLATE", Name = "_ACC_RArmourPlate", Children = new()
                        {
                            new() { GroupId = "_DECAL_", Descriptors = new()
                            {
                                new() { Id = "_DECAL_NULL7", Name = "_Decal_Null7" },
                                new() { Id = "_DECAL_LGA7", Name = "_Decal_LgA7" },
                                new() { Id = "_DECAL_LGB7", Name = "_Decal_LgB7" },
                                new() { Id = "_DECAL_NMS7", Name = "_Decal_NMS7" },
                                new() { Id = "_DECAL_NUMA7", Name = "_Decal_NumA7" },
                                new() { Id = "_DECAL_NUMB7", Name = "_Decal_NumB7" },
                                new() { Id = "_DECAL_NUMC7", Name = "_Decal_NumC7" },
                                new() { Id = "_DECAL_NUMD7", Name = "_Decal_NumD7" },
                                new() { Id = "_DECAL_NUME7", Name = "_Decal_NumE7" },
                                new() { Id = "_DECAL_NUMF7", Name = "_Decal_NumF7" },
                                new() { Id = "_DECAL_NUMG7", Name = "_Decal_NumG7" },
                                new() { Id = "_DECAL_NUMH7", Name = "_Decal_NumH7" },
                                new() { Id = "_DECAL_NUMI7", Name = "_Decal_NumI7" },
                                new() { Id = "_DECAL_NUMJ7", Name = "_Decal_NumJ7" },
                                new() { Id = "_DECAL_SPA7", Name = "_Decal_SpA7" },
                                new() { Id = "_DECAL_SPB7", Name = "_Decal_SpB7" },
                                new() { Id = "_DECAL_SPC7", Name = "_Decal_SpC7" },
                                new() { Id = "_DECAL_SPD7", Name = "_Decal_SpD7" },
                                new() { Id = "_DECAL_SPE7", Name = "_Decal_SpE7" },
                                new() { Id = "_DECAL_SPF7", Name = "_Decal_SpF7" },
                                new() { Id = "_DECAL_SIMPLEA7", Name = "_Decal_SimpleA7" },
                                new() { Id = "_DECAL_SIMPLEB7", Name = "_Decal_SimpleB7" },
                                new() { Id = "_DECAL_SIMPLEC7", Name = "_Decal_SimpleC7" },
                                new() { Id = "_DECAL_SIMPLED7", Name = "_Decal_SimpleD7" },
                                new() { Id = "_DECAL_VISA7", Name = "_Decal_VisA7" },
                                new() { Id = "_DECAL_VISB7", Name = "_Decal_VisB7" },
                                new() { Id = "_DECAL_VISC7", Name = "_Decal_VisC7" },
                                new() { Id = "_DECAL_VISD7", Name = "_Decal_VisD7" },
                                new() { Id = "_DECAL_VISE7", Name = "_Decal_VisE7" },
                                new() { Id = "_DECAL_SPG7", Name = "_Decal_SpG7" },
                                new() { Id = "_DECAL_SPH7", Name = "_Decal_SpH7" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ACC_RTURRET", Name = "_ACC_RTurret" },
                        new() { Id = "_ACC_RSUPPORTSYSTEM", Name = "_ACC_RSupportSystem" },
                        new() { Id = "_ACC_RROYALARMOUR", Name = "_ACC_RRoyalArmour" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "PLOW", FriendlyName = "PLOUGH", Details = new()
        {
            new() { GroupId = "_PLOW_", Descriptors = new()
            {
                new() { Id = "_PLOW_A", Name = "_Plow_A" },
                new() { Id = "_PLOW_B", Name = "_Plow_B" },
                new() { Id = "_PLOW_C", Name = "_Plow_C" },
                new() { Id = "_PLOW_D", Name = "_Plow_D" },
                new() { Id = "_PLOW_E", Name = "_Plow_E" },
                new() { Id = "_PLOW_F", Name = "_Plow_F" },
                new() { Id = "_PLOW_G", Name = "_Plow_G" },
                new() { Id = "_PLOW_H", Name = "_Plow_H" },
                new() { Id = "_PLOW_I", Name = "_Plow_I" },
                new() { Id = "_PLOW_J", Name = "_Plow_J" },
            }
            },
        }
        },
        new() { CreatureId = "WEIRDDIGGER", FriendlyName = "PROTODIGGER", Details = new()
        {
            new() { GroupId = "_WEIRD_", Descriptors = new()
            {
                new() { Id = "_WEIRD_CRYSTAL", Name = "_Weird_Crystal" },
            }
            },
        }
        },
        new() { CreatureId = "FLOATERCREATURE", FriendlyName = "PROTOFLYER", Details = new()
        {
            new() { GroupId = "_STRANGE_", Descriptors = new()
            {
                new() { Id = "_STRANGE_FLOAT", Name = "_Strange_Float", Children = new()
                {
                    new() { GroupId = "_FLOAT_", Descriptors = new()
                    {
                        new() { Id = "_FLOAT_RADIALWAVE", Name = "_Float_RadialWave" },
                        new() { Id = "_FLOAT_TRAILORB", Name = "_Float_TrailOrb" },
                        new() { Id = "_FLOAT_METALORB", Name = "_Float_Metalorb" },
                        new() { Id = "_FLOAT_EYEFISH", Name = "_Float_EyeFish" },
                        new() { Id = "_FLOAT_ROSEFISH", Name = "_Float_RoseFish" },
                        new() { Id = "_FLOAT_FLATCREATURE", Name = "_Float_FlatCreature" },
                        new() { Id = "_FLOAT_RIDGEEEL", Name = "_Float_RidgeEel" },
                        new() { Id = "_FLOAT_ELEPHLOATER", Name = "_Float_Elephloater" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "ROLLERCREATURE", FriendlyName = "PROTOROLLER", Details = new()
        {
            new() { GroupId = "_STRANGE_", Descriptors = new()
            {
                new() { Id = "_STRANGE_ROLL", Name = "_Strange_Roll", Children = new()
                {
                    new() { GroupId = "_ROLL_", Descriptors = new()
                    {
                        new() { Id = "_ROLL_SINGLEJOINT", Name = "_Roll_SingleJoint", Children = new()
                        {
                            new() { GroupId = "_ROLL_", Descriptors = new()
                            {
                                new() { Id = "_ROLL_BEETLE", Name = "_Roll_Beetle" },
                                new() { Id = "_ROLL_TUBES", Name = "_Roll_Tubes" },
                                new() { Id = "_ROLL_BLOB", Name = "_Roll_Blob" },
                                new() { Id = "_ROLL_EYES", Name = "_Roll_Eyes" },
                                new() { Id = "_ROLL_TENDRIL", Name = "_Roll_Tendril", Children = new()
                                {
                                    new() { GroupId = "_TENDRILS_", Descriptors = new()
                                    {
                                        new() { Id = "_TENDRILS_A", Name = "_Tendrils_A" },
                                        new() { Id = "_TENDRILS_B", Name = "_Tendrils_B" },
                                        new() { Id = "_TENDRILS_C", Name = "_Tendrils_C" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_ROLL_FATBUG", Name = "_Roll_FatBug" },
                                new() { Id = "_ROLL_CLOCKBALL", Name = "_Roll_ClockBall" },
                                new() { Id = "_ROLL_ENGERYBALL", Name = "_Roll_Engeryball", Children = new()
                                {
                                    new() { GroupId = "_INTERNAL_", Descriptors = new()
                                    {
                                        new() { Id = "_INTERNAL_0", Name = "_Internal_0" },
                                        new() { Id = "_INTERNAL_A", Name = "_Internal_A" },
                                        new() { Id = "_INTERNAL_B", Name = "_Internal_B" },
                                        new() { Id = "_INTERNAL_C", Name = "_Internal_C" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_ROLL_BUBBLE", Name = "_Roll_Bubble" },
                                new() { Id = "_ROLL_LINKS", Name = "_Roll_Links" },
                                new() { Id = "_ROLL_SLIME", Name = "_Roll_Slime" },
                                new() { Id = "_ROLL_CURL", Name = "_Roll_Curl" },
                                new() { Id = "_ROLL_WHEEL", Name = "_Roll_Wheel" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "ROBOTANTELOPE", FriendlyName = "ROBOTANTELOPE", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_CURVEB", Name = "_Head_CurveB", Children = new()
                {
                    new() { GroupId = "_CURVEBACC_", Descriptors = new()
                    {
                        new() { Id = "_CURVEBACC_2", Name = "_CurveBAcc_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_DISC", Name = "_Head_Disc" },
                new() { Id = "_HEAD_WEDGEINVERTED", Name = "_Head_WedgeInverted" },
            }
            },
            new() { GroupId = "_ACC_", Descriptors = new()
            {
                new() { Id = "_ACC_BACKCANISTERS", Name = "_Acc_BackCanisters" },
                new() { Id = "_ACC_BACKDISC", Name = "_Acc_BackDisc" },
                new() { Id = "_ACC_BACKMAGNET", Name = "_Acc_BackMagnet" },
                new() { Id = "_ACC_BACKORB", Name = "_Acc_BackOrb" },
                new() { Id = "_ACC_BACKSHROOMS", Name = "_Acc_BackShrooms" },
                new() { Id = "_ACC_NONE", Name = "_Acc_None" },
            }
            },
            new() { GroupId = "_RIBCAGE_", Descriptors = new()
            {
                new() { Id = "_RIBCAGE_1", Name = "_Ribcage_1" },
            }
            },
            new() { GroupId = "_LEGSBACK_", Descriptors = new()
            {
                new() { Id = "_LEGSBACK_1", Name = "_LegsBack_1" },
            }
            },
            new() { GroupId = "_PLATING_", Descriptors = new()
            {
                new() { Id = "_PLATING_1", Name = "_Plating_1" },
                new() { Id = "_PLATING_2", Name = "_Plating_2" },
            }
            },
            new() { GroupId = "_CORDSLEGBACK_", Descriptors = new()
            {
                new() { Id = "_CORDSLEGBACK_", Name = "_CordsLegBack_" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_1", Name = "_Body_1" },
            }
            },
            new() { GroupId = "_LEGSFRONT_", Descriptors = new()
            {
                new() { Id = "_LEGSFRONT_1", Name = "_LegsFront_1" },
            }
            },
            new() { GroupId = "_CORDSLEGFRONT_", Descriptors = new()
            {
                new() { Id = "_CORDSLEGFRONT_", Name = "_CordsLegFront_" },
            }
            },
            new() { GroupId = "_SHOULDERACC_", Descriptors = new()
            {
                new() { Id = "_SHOULDERACC_CANISTERS", Name = "_ShoulderAcc_Canisters" },
                new() { Id = "_SHOULDERACC_DISCS", Name = "_ShoulderAcc_Discs" },
                new() { Id = "_SHOULDERACC_HEX", Name = "_ShoulderAcc_Hex" },
                new() { Id = "_SHOULDERACC_NONE", Name = "_ShoulderAcc_None" },
            }
            },
        }
        },
        new() { CreatureId = "ROCKCREATURE", FriendlyName = "ROCKCREATURE", Details = new()
        {
            new() { GroupId = "_MANTIS_", Descriptors = new()
            {
                new() { Id = "_MANTIS_A", Name = "_Mantis_A", Children = new()
                {
                    new() { GroupId = "_LEGS_", Descriptors = new()
                    {
                        new() { Id = "_LEGS_1", Name = "_Legs_1", Children = new()
                        {
                            new() { GroupId = "_LEGA_", Descriptors = new()
                            {
                                new() { Id = "_LEGA_1", Name = "_LegA_1" },
                            }
                            },
                            new() { GroupId = "_LEGC_", Descriptors = new()
                            {
                                new() { Id = "_LEGC_1", Name = "_LegC_1" },
                                new() { Id = "_LEGC_NULL", Name = "_LegC_NULL" },
                            }
                            },
                            new() { GroupId = "_LEGB_", Descriptors = new()
                            {
                                new() { Id = "_LEGB_1", Name = "_LegB_1" },
                                new() { Id = "_LEGB_NULL", Name = "_LegB_NULL" },
                            }
                            },
                        }
                        },
                        new() { Id = "_LEGS_3", Name = "_Legs_3", Children = new()
                        {
                            new() { GroupId = "_LEGGC_", Descriptors = new()
                            {
                                new() { Id = "_LEGGC_NONE", Name = "_LeggC_none" },
                                new() { Id = "_LEGGC_1", Name = "_LeggC_1" },
                            }
                            },
                            new() { GroupId = "_LEGGA_", Descriptors = new()
                            {
                                new() { Id = "_LEGGA_1", Name = "_LeggA_1" },
                            }
                            },
                            new() { GroupId = "_LEGGD_", Descriptors = new()
                            {
                                new() { Id = "_LEGGD_1", Name = "_LeggD_1" },
                            }
                            },
                            new() { GroupId = "_LEGGB_", Descriptors = new()
                            {
                                new() { Id = "_LEGGB_NONE", Name = "_LeggB_none" },
                                new() { Id = "_LEGGB_1", Name = "_LeggB_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_LEGS_2", Name = "_Legs_2" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "ROCKSPIDER", FriendlyName = "ROCKSPIDER", Details = new()
        {
            new() { GroupId = "_ROCKSPIDER_", Descriptors = new()
            {
                new() { Id = "_ROCKSPIDER_A", Name = "_RockSpider_A", Children = new()
                {
                    new() { GroupId = "_ARMS_", Descriptors = new()
                    {
                        new() { Id = "_ARMS_1", Name = "_Arms_1" },
                        new() { Id = "_ARMS_2", Name = "_Arms_2" },
                    }
                    },
                    new() { GroupId = "_LEGS_", Descriptors = new()
                    {
                        new() { Id = "_LEGS_1", Name = "_Legs_1", Children = new()
                        {
                            new() { GroupId = "_LEGA_", Descriptors = new()
                            {
                                new() { Id = "_LEGA_1", Name = "_LegA_1" },
                            }
                            },
                            new() { GroupId = "_LEGC_", Descriptors = new()
                            {
                                new() { Id = "_LEGC_1", Name = "_LegC_1" },
                                new() { Id = "_LEGC_NULL", Name = "_LegC_NULL" },
                            }
                            },
                            new() { GroupId = "_LEGB_", Descriptors = new()
                            {
                                new() { Id = "_LEGB_1", Name = "_LegB_1" },
                                new() { Id = "_LEGB_NULL", Name = "_LegB_NULL" },
                            }
                            },
                        }
                        },
                        new() { Id = "_LEGS_3", Name = "_Legs_3", Children = new()
                        {
                            new() { GroupId = "_LEGGC_", Descriptors = new()
                            {
                                new() { Id = "_LEGGC_NONE", Name = "_LeggC_none" },
                                new() { Id = "_LEGGC_1", Name = "_LeggC_1" },
                            }
                            },
                            new() { GroupId = "_LEGGA_", Descriptors = new()
                            {
                                new() { Id = "_LEGGA_1", Name = "_LeggA_1" },
                            }
                            },
                            new() { GroupId = "_LEGGD_", Descriptors = new()
                            {
                                new() { Id = "_LEGGD_1", Name = "_LeggD_1" },
                            }
                            },
                            new() { GroupId = "_LEGGB_", Descriptors = new()
                            {
                                new() { Id = "_LEGGB_NONE", Name = "_LeggB_none" },
                                new() { Id = "_LEGGB_1", Name = "_LeggB_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_LEGS_2", Name = "_Legs_2" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "RODENT", FriendlyName = "RODENT", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_BIRD", Name = "_Head_Bird", Children = new()
                {
                    new() { GroupId = "_HBACC_", Descriptors = new()
                    {
                        new() { Id = "_HBACC_0", Name = "_HBAcc_0" },
                        new() { Id = "_HBACC_1", Name = "_HBAcc_1" },
                    }
                    },
                    new() { GroupId = "_HBEARS_", Descriptors = new()
                    {
                        new() { Id = "_HBEARS_0", Name = "_HBEars_0" },
                        new() { Id = "_HBEARS_1", Name = "_HBEars_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_GDANE", Name = "_Head_GDane", Children = new()
                {
                    new() { GroupId = "_GDAC_", Descriptors = new()
                    {
                        new() { Id = "_GDAC_0", Name = "_GDAc_0" },
                        new() { Id = "_GDAC_1", Name = "_GDAc_1" },
                        new() { Id = "_GDAC_2", Name = "_GDAc_2", Children = new()
                        {
                            new() { GroupId = "_BULB_", Descriptors = new()
                            {
                                new() { Id = "_BULB_1", Name = "_Bulb_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_HGDEARS_", Descriptors = new()
                    {
                        new() { Id = "_HGDEARS_4", Name = "_HGDEars_4" },
                        new() { Id = "_HGDEARS_3", Name = "_HGDEars_3" },
                        new() { Id = "_HGDEARS_2", Name = "_HGDEars_2" },
                        new() { Id = "_HGDEARS_1", Name = "_HGDEars_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LIZARD", Name = "_Head_Lizard", Children = new()
                {
                    new() { GroupId = "__HLACC_", Descriptors = new()
                    {
                        new() { Id = "__HLACC_0", Name = "__HLAcc_0" },
                    }
                    },
                    new() { GroupId = "_HLNECK_", Descriptors = new()
                    {
                        new() { Id = "_HLNECK_0", Name = "_HLNeck_0" },
                        new() { Id = "_HLNECK_1", Name = "_HLNeck_1" },
                    }
                    },
                    new() { GroupId = "_HLEARS_", Descriptors = new()
                    {
                        new() { Id = "_HLEARS_0", Name = "_HLEars_0" },
                        new() { Id = "_HLEARS_1", Name = "_HLEars_1" },
                        new() { Id = "_HLEARS_2", Name = "_HLEars_2" },
                    }
                    },
                    new() { GroupId = "_HLACC_", Descriptors = new()
                    {
                        new() { Id = "_HLACC_1", Name = "_HLAcc_1" },
                        new() { Id = "_HLACC_2", Name = "_HLAcc_2" },
                        new() { Id = "_HLACC_3", Name = "_HLAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_RODENT", Name = "_Head_Rodent", Children = new()
                {
                    new() { GroupId = "_RHACC_", Descriptors = new()
                    {
                        new() { Id = "_RHACC_0", Name = "_RHAcc_0" },
                        new() { Id = "_RHACC_2", Name = "_RHAcc_2" },
                        new() { Id = "_RHACC_1", Name = "_RHAcc_1" },
                    }
                    },
                    new() { GroupId = "_HREARS_", Descriptors = new()
                    {
                        new() { Id = "_HREARS_0XRARE", Name = "_HREars_0xRARE" },
                        new() { Id = "_HREARS_7", Name = "_HREars_7" },
                        new() { Id = "_HREARS_6", Name = "_HREars_6" },
                        new() { Id = "_HREARS_5", Name = "_HREars_5" },
                        new() { Id = "_HREARS_1", Name = "_HREars_1" },
                        new() { Id = "_HREARS_3", Name = "_HREars_3" },
                        new() { Id = "_HREARS_4", Name = "_HREars_4" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_SLOTH", Name = "_Head_Sloth", Children = new()
                {
                    new() { GroupId = "_HS1EAR_", Descriptors = new()
                    {
                        new() { Id = "_HS1EAR_0XRARE", Name = "_HS1Ear_0xRARE" },
                        new() { Id = "_HS1EAR_1", Name = "_HS1Ear_1" },
                        new() { Id = "_HS1EAR_2", Name = "_HS1Ear_2" },
                        new() { Id = "_HS1EAR_3", Name = "_HS1Ear_3" },
                        new() { Id = "_HS1EAR_4", Name = "_HS1Ear_4" },
                        new() { Id = "_HS1EAR_5", Name = "_HS1Ear_5" },
                    }
                    },
                    new() { GroupId = "_HSACC1_", Descriptors = new()
                    {
                        new() { Id = "_HSACC1_0", Name = "_HSAcc1_0" },
                        new() { Id = "_HSACC1_1", Name = "_HSAcc1_1" },
                        new() { Id = "_HSACC1_3", Name = "_HSAcc1_3" },
                        new() { Id = "_HSACC1_4", Name = "_HSAcc1_4" },
                        new() { Id = "_HSACC1_5", Name = "_HSAcc1_5" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TAPIR", Name = "_Head_Tapir", Children = new()
                {
                    new() { GroupId = "_TPHEARS_", Descriptors = new()
                    {
                        new() { Id = "_TPHEARS_0XRARE", Name = "_TPHEars_0xRARE" },
                        new() { Id = "_TPHEARS_2", Name = "_TPHEars_2" },
                        new() { Id = "_TPHEARS_1", Name = "_TPHEars_1" },
                    }
                    },
                    new() { GroupId = "_TPHACC_", Descriptors = new()
                    {
                        new() { Id = "_TPHACC_0", Name = "_TPHAcc_0" },
                        new() { Id = "_TPHACC_2", Name = "_TPHAcc_2" },
                        new() { Id = "_TPHACC_1", Name = "_TPHAcc_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TAZ", Name = "_Head_Taz", Children = new()
                {
                    new() { GroupId = "_HTAZEARS_", Descriptors = new()
                    {
                        new() { Id = "_HTAZEARS_0XRARE", Name = "_HTazEars_0xRARE" },
                        new() { Id = "_HTAZEARS_3", Name = "_HTazEars_3" },
                        new() { Id = "_HTAZEARS_2", Name = "_HTazEars_2" },
                        new() { Id = "_HTAZEARS_1", Name = "_HTazEars_1" },
                    }
                    },
                    new() { GroupId = "_HTAZACC_", Descriptors = new()
                    {
                        new() { Id = "_HTAZACC_0", Name = "_HTazAcc_0" },
                        new() { Id = "_HTAZACC_1", Name = "_HTazAcc_1" },
                        new() { Id = "_HTAZACC_2", Name = "_HTazAcc_2" },
                        new() { Id = "_HTAZACC_3", Name = "_HTazAcc_3" },
                        new() { Id = "_HTAZACC_4", Name = "_HTazAcc_4" },
                        new() { Id = "_HTAZACC_5", Name = "_HTazAcc_5" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TURTLE", Name = "_Head_Turtle", Children = new()
                {
                    new() { GroupId = "_THACC_", Descriptors = new()
                    {
                        new() { Id = "_THACC_1", Name = "_THAcc_1" },
                        new() { Id = "_THACC_2", Name = "_THAcc_2" },
                        new() { Id = "_THACC_3", Name = "_THAcc_3" },
                        new() { Id = "_THACC_4", Name = "_THAcc_4" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_LIZARD", Name = "_Body_Lizard", Children = new()
                {
                    new() { GroupId = "_LIZBOD_", Descriptors = new()
                    {
                        new() { Id = "_LIZBOD_0", Name = "_LizBod_0" },
                        new() { Id = "_LIZBOD_1", Name = "_LizBod_1" },
                        new() { Id = "_LIZBOD_2", Name = "_LizBod_2" },
                        new() { Id = "_LIZBOD_3", Name = "_LizBod_3" },
                        new() { Id = "_LIZBOD_4", Name = "_LizBod_4" },
                        new() { Id = "_LIZBOD_5", Name = "_LizBod_5" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_RODENT", Name = "_Body_Rodent", Children = new()
                {
                    new() { GroupId = "_TAZBACK_", Descriptors = new()
                    {
                        new() { Id = "_TAZBACK_0", Name = "_TazBack_0" },
                        new() { Id = "_TAZBACK_3", Name = "_TazBack_3" },
                        new() { Id = "_TAZBACK_4", Name = "_TazBack_4", Children = new()
                        {
                            new() { GroupId = "_BULBS_", Descriptors = new()
                            {
                                new() { Id = "_BULBS_1", Name = "_Bulbs_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAZBACK_2", Name = "_TazBack_2" },
                        new() { Id = "_TAZBACK_1", Name = "_TazBack_1" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_SLOTH", Name = "_Body_Sloth", Children = new()
                {
                    new() { GroupId = "_SBACK_", Descriptors = new()
                    {
                        new() { Id = "_SBACK_0", Name = "_SBack_0" },
                        new() { Id = "_SBACK_1", Name = "_SBack_1" },
                        new() { Id = "_SBACK_6", Name = "_SBack_6" },
                        new() { Id = "_SBACK_5", Name = "_SBack_5" },
                        new() { Id = "_SBACK_2", Name = "_SBack_2" },
                        new() { Id = "_SBACK_3", Name = "_SBack_3" },
                        new() { Id = "_SBACK_4", Name = "_SBack_4" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_BIRD", Name = "_Tail_Bird" },
                new() { Id = "_TAIL_LIZARD", Name = "_Tail_Lizard", Children = new()
                {
                    new() { GroupId = "_TAILACC_", Descriptors = new()
                    {
                        new() { Id = "_TAILACC_0", Name = "_TailAcc_0" },
                        new() { Id = "_TAILACC_1", Name = "_TailAcc_1" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_MONKEY", Name = "_Tail_Monkey" },
                new() { Id = "_TAIL_RODENT", Name = "_Tail_Rodent" },
                new() { Id = "_TAIL_SLOTH", Name = "_Tail_Sloth" },
            }
            },
        }
        },
        new() { CreatureId = "SANDWORM", FriendlyName = "SANDWORM", Details = new()
        {
            new() { GroupId = "_SNAKE_", Descriptors = new()
            {
                new() { Id = "_SNAKE_A", Name = "_Snake_A", Children = new()
                {
                    new() { GroupId = "_SET_", Descriptors = new()
                    {
                        new() { Id = "_SET_A", Name = "_Set_A", Children = new()
                        {
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_1", Name = "_Head_1" },
                                new() { Id = "_HEAD_2", Name = "_Head_2" },
                                new() { Id = "_HEAD_3", Name = "_Head_3" },
                                new() { Id = "_HEAD_4", Name = "_Head_4", Children = new()
                                {
                                    new() { GroupId = "_HWACC_", Descriptors = new()
                                    {
                                        new() { Id = "_HWACC_1", Name = "_HWAcc_1" },
                                        new() { Id = "_HWACC_NULL", Name = "_HWAcc_NULL" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_WINGS_", Descriptors = new()
                            {
                                new() { Id = "_WINGS_1", Name = "_Wings_1" },
                                new() { Id = "_WINGS_NULL", Name = "_Wings_NULL" },
                                new() { Id = "_WINGS_12", Name = "_Wings_12" },
                                new() { Id = "_WINGS_9", Name = "_Wings_9" },
                                new() { Id = "_WINGS_10", Name = "_Wings_10" },
                                new() { Id = "_WINGS_11", Name = "_Wings_11" },
                            }
                            },
                            new() { GroupId = "_BODY_", Descriptors = new()
                            {
                                new() { Id = "_BODY_1", Name = "_Body_1", Children = new()
                                {
                                    new() { GroupId = "_B1ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_B1ACC_0", Name = "_B1Acc_0" },
                                        new() { Id = "_B1ACC_1", Name = "_B1Acc_1" },
                                        new() { Id = "_B1ACC_2", Name = "_B1Acc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BODY_2", Name = "_Body_2", Children = new()
                                {
                                    new() { GroupId = "_BEELACC_", Descriptors = new()
                                    {
                                        new() { Id = "_BEELACC_NULL", Name = "_BEelAcc_NULL" },
                                        new() { Id = "_BEELACC_5", Name = "_BEelAcc_5" },
                                        new() { Id = "_BEELACC_6", Name = "_BEelAcc_6" },
                                        new() { Id = "_BEELACC_7", Name = "_BEelAcc_7" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BODY_3", Name = "_Body_3", Children = new()
                                {
                                    new() { GroupId = "_BWACC_", Descriptors = new()
                                    {
                                        new() { Id = "_BWACC_NULL", Name = "_BWAcc_NULL" },
                                        new() { Id = "_BWACC_1", Name = "_BWAcc_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_TAIL_", Descriptors = new()
                            {
                                new() { Id = "_TAIL_4", Name = "_Tail_4", Children = new()
                                {
                                    new() { GroupId = "_TWACC_", Descriptors = new()
                                    {
                                        new() { Id = "_TWACC_NULL", Name = "_TWAcc_NULL" },
                                        new() { Id = "_TWACC_3", Name = "_TWAcc_3" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_SET_B", Name = "_Set_B", Children = new()
                        {
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_CENTIPEDE", Name = "_Head_Centipede" },
                                new() { Id = "_HEAD_LEECH", Name = "_Head_Leech" },
                                new() { Id = "_HEAD_SANDWORM", Name = "_Head_SandWorm" },
                                new() { Id = "_HEAD_SANDSNAKE", Name = "_Head_SandSnake" },
                            }
                            },
                            new() { GroupId = "_BODY_", Descriptors = new()
                            {
                                new() { Id = "_BODY_CENTIPEDE", Name = "_Body_Centipede" },
                                new() { Id = "_BODY_LEECH", Name = "_Body_Leech" },
                                new() { Id = "_BODY_SANDWORM", Name = "_Body_SandWorm" },
                                new() { Id = "_BODY_SANDSNAKE", Name = "_Body_SandSnake" },
                            }
                            },
                            new() { GroupId = "_TAIL_", Descriptors = new()
                            {
                                new() { Id = "_TAIL_CENTIPEDE", Name = "_Tail_Centipede" },
                                new() { Id = "_TAIL_LEECH", Name = "_Tail_Leech" },
                                new() { Id = "_TAIL_SANDWORM", Name = "_Tail_SandWorm" },
                                new() { Id = "_TAIL_SANDSNAKE", Name = "_Tail_SandSnake" },
                            }
                            },
                        }
                        },
                        new() { Id = "_SET_GPXRARE", Name = "_Set_GPxRARE" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "FREIGHTERFIEND", FriendlyName = "SCUTTLER", Details = new()
        {
            new() { GroupId = "_FIEND_", Descriptors = new()
            {
                new() { Id = "_FIEND_BODY", Name = "_Fiend_Body" },
            }
            },
        }
        },
        new() { CreatureId = "SEASNAKE", FriendlyName = "SEASNAKE", Details = new()
        {
            new() { GroupId = "_SNAKE_", Descriptors = new()
            {
                new() { Id = "_SNAKE_A", Name = "_Snake_A", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_1", Name = "_Head_1" },
                        new() { Id = "_HEAD_2", Name = "_Head_2" },
                        new() { Id = "_HEAD_3", Name = "_Head_3" },
                        new() { Id = "_HEAD_4", Name = "_Head_4", Children = new()
                        {
                            new() { GroupId = "_HWACC_", Descriptors = new()
                            {
                                new() { Id = "_HWACC_1", Name = "_HWAcc_1" },
                                new() { Id = "_HWACC_NULL", Name = "_HWAcc_NULL" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_WINGS_", Descriptors = new()
                    {
                        new() { Id = "_WINGS_1", Name = "_Wings_1" },
                        new() { Id = "_WINGS_NULL", Name = "_Wings_NULL" },
                        new() { Id = "_WINGS_12", Name = "_Wings_12" },
                        new() { Id = "_WINGS_9", Name = "_Wings_9" },
                        new() { Id = "_WINGS_10", Name = "_Wings_10" },
                        new() { Id = "_WINGS_11", Name = "_Wings_11" },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_1", Name = "_Body_1", Children = new()
                        {
                            new() { GroupId = "_B1ACC_", Descriptors = new()
                            {
                                new() { Id = "_B1ACC_0", Name = "_B1Acc_0" },
                                new() { Id = "_B1ACC_1", Name = "_B1Acc_1" },
                                new() { Id = "_B1ACC_2", Name = "_B1Acc_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_2", Name = "_Body_2", Children = new()
                        {
                            new() { GroupId = "_BEELACC_", Descriptors = new()
                            {
                                new() { Id = "_BEELACC_NULL", Name = "_BEelAcc_NULL" },
                                new() { Id = "_BEELACC_5", Name = "_BEelAcc_5" },
                                new() { Id = "_BEELACC_6", Name = "_BEelAcc_6" },
                                new() { Id = "_BEELACC_7", Name = "_BEelAcc_7" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_3", Name = "_Body_3", Children = new()
                        {
                            new() { GroupId = "_BWACC_", Descriptors = new()
                            {
                                new() { Id = "_BWACC_NULL", Name = "_BWAcc_NULL" },
                                new() { Id = "_BWACC_1", Name = "_BWAcc_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_TAIL_", Descriptors = new()
                    {
                        new() { Id = "_TAIL_1", Name = "_Tail_1", Children = new()
                        {
                            new() { GroupId = "_T1ACC_", Descriptors = new()
                            {
                                new() { Id = "_T1ACC_2", Name = "_T1Acc_2" },
                                new() { Id = "_T1ACC_1", Name = "_T1Acc_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_2", Name = "_Tail_2", Children = new()
                        {
                            new() { GroupId = "_TAILFIN_", Descriptors = new()
                            {
                                new() { Id = "_TAILFIN_3", Name = "_TailFin_3" },
                                new() { Id = "_TAILFIN_4", Name = "_TailFin_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_3", Name = "_Tail_3", Children = new()
                        {
                            new() { GroupId = "_TAILFIN3_", Descriptors = new()
                            {
                                new() { Id = "_TAILFIN3_3", Name = "_TailFin3_3" },
                                new() { Id = "_TAILFIN3_4", Name = "_TailFin3_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_4", Name = "_Tail_4", Children = new()
                        {
                            new() { GroupId = "_TWACC_", Descriptors = new()
                            {
                                new() { Id = "_TWACC_NULL", Name = "_TWAcc_NULL" },
                                new() { Id = "_TWACC_3", Name = "_TWAcc_3" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_5", Name = "_Tail_5" },
                        new() { Id = "_TAIL_6", Name = "_Tail_6" },
                        new() { Id = "_TAIL_7", Name = "_Tail_7" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "SHARK", FriendlyName = "SHARK", Details = new()
        {
            new() { GroupId = "_SHARK_", Descriptors = new()
            {
                new() { Id = "_SHARK_1", Name = "_Shark_1", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_CROC", Name = "_Head_Croc", Children = new()
                        {
                            new() { GroupId = "_CROCACC_", Descriptors = new()
                            {
                                new() { Id = "_CROCACC_NULL", Name = "_CrocAcc_NULL" },
                                new() { Id = "_CROCACC_01", Name = "_CrocAcc_01" },
                                new() { Id = "_CROCACC_02", Name = "_CrocAcc_02" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_SF", Name = "_Head_SF", Children = new()
                        {
                            new() { GroupId = "_SFACC_", Descriptors = new()
                            {
                                new() { Id = "_SFACC_0", Name = "_SFAcc_0" },
                                new() { Id = "_SFACC_01", Name = "_SFAcc_01" },
                                new() { Id = "_SFACC_02", Name = "_SFAcc_02" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_WHALE", Name = "_Head_Whale", Children = new()
                        {
                            new() { GroupId = "_MOUTHACC_", Descriptors = new()
                            {
                                new() { Id = "_MOUTHACC_0", Name = "_MouthAcc_0" },
                                new() { Id = "_MOUTHACC_01XRA", Name = "_MouthAcc_01xRARE" },
                            }
                            },
                            new() { GroupId = "_HEADACC_", Descriptors = new()
                            {
                                new() { Id = "_HEADACC_0", Name = "_HeadAcc_0" },
                                new() { Id = "_HEADACC_1", Name = "_HeadAcc_1" },
                                new() { Id = "_HEADACC_2", Name = "_HeadAcc_2" },
                                new() { Id = "_HEADACC_3", Name = "_HeadAcc_3" },
                                new() { Id = "_HEADACC_4", Name = "_HeadAcc_4" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_WHALE", Name = "_Body_Whale", Children = new()
                        {
                            new() { GroupId = "_TAIL_", Descriptors = new()
                            {
                                new() { Id = "_TAIL_WHALE", Name = "_Tail_Whale", Children = new()
                                {
                                    new() { GroupId = "_TWFINS_", Descriptors = new()
                                    {
                                        new() { Id = "_TWFINS_1", Name = "_TWFins_1" },
                                    }
                                    },
                                    new() { GroupId = "_TWTOP_", Descriptors = new()
                                    {
                                        new() { Id = "_TWTOP_1", Name = "_TWTop_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TAIL_SF", Name = "_Tail_SF" },
                                new() { Id = "_TAIL_CROC", Name = "_Tail_Croc", Children = new()
                                {
                                    new() { GroupId = "_TCFINS_", Descriptors = new()
                                    {
                                        new() { Id = "_TCFINS_1", Name = "_TCFins_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TAIL_WHALE1", Name = "_Tail_Whale1" },
                                new() { Id = "_TAIL_SF1", Name = "_Tail_SF1" },
                                new() { Id = "_TAIL_CROC1", Name = "_Tail_Croc1" },
                                new() { Id = "_TAIL_SF2", Name = "_Tail_SF2" },
                            }
                            },
                            new() { GroupId = "_TAILFIN_", Descriptors = new()
                            {
                                new() { Id = "_TAILFIN_NULL", Name = "_TailFin_NULL" },
                                new() { Id = "_TAILFIN_1", Name = "_TailFin_1" },
                                new() { Id = "_TAILFIN_2", Name = "_TailFin_2" },
                                new() { Id = "_TAILFIN_3", Name = "_TailFin_3" },
                                new() { Id = "_TAILFIN_4", Name = "_TailFin_4" },
                            }
                            },
                            new() { GroupId = "_FINBOT_", Descriptors = new()
                            {
                                new() { Id = "_FINBOT_NULL", Name = "_FinBot_NULL" },
                                new() { Id = "_FINBOT_1", Name = "_FinBot_1" },
                                new() { Id = "_FINBOT_2", Name = "_FinBot_2" },
                            }
                            },
                            new() { GroupId = "_TOPACC_", Descriptors = new()
                            {
                                new() { Id = "_TOPACC_NULL", Name = "_TopAcc_NULL" },
                                new() { Id = "_TOPACC_A", Name = "_TopAcc_A", Children = new()
                                {
                                    new() { GroupId = "_SIDEFIN_", Descriptors = new()
                                    {
                                        new() { Id = "_SIDEFIN_0", Name = "_SideFin_0" },
                                        new() { Id = "_SIDEFIN_1", Name = "_SideFin_1" },
                                    }
                                    },
                                    new() { GroupId = "_TOPFIN_", Descriptors = new()
                                    {
                                        new() { Id = "_TOPFIN_5", Name = "_TopFin_5" },
                                        new() { Id = "_TOPFIN_3", Name = "_TopFin_3" },
                                        new() { Id = "_TOPFIN_1", Name = "_TopFin_1" },
                                        new() { Id = "_TOPFIN_2", Name = "_TopFin_2" },
                                        new() { Id = "_TOPFIN_4", Name = "_TopFin_4" },
                                        new() { Id = "_TOPFIN_6", Name = "_TopFin_6", Children = new()
                                        {
                                            new() { GroupId = "_SHELLACC_", Descriptors = new()
                                            {
                                                new() { Id = "_SHELLACC_1", Name = "_ShellAcc_1" },
                                                new() { Id = "_SHELLACC_2", Name = "_ShellAcc_2" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_TOPFIN_7", Name = "_TopFin_7" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TOPACC_B", Name = "_TopAcc_B", Children = new()
                                {
                                    new() { GroupId = "_BSIDEFIN_", Descriptors = new()
                                    {
                                        new() { Id = "_BSIDEFIN_0", Name = "_BSideFin_0" },
                                        new() { Id = "_BSIDEFIN_5", Name = "_BSideFin_5" },
                                        new() { Id = "_BSIDEFIN_1", Name = "_BSideFin_1" },
                                        new() { Id = "_BSIDEFIN_2", Name = "_BSideFin_2" },
                                        new() { Id = "_BSIDEFIN_3", Name = "_BSideFin_3" },
                                        new() { Id = "_BSIDEFIN_4", Name = "_BSideFin_4" },
                                    }
                                    },
                                    new() { GroupId = "_BTOPFIN_", Descriptors = new()
                                    {
                                        new() { Id = "_BTOPFIN_3", Name = "_BTopFin_3" },
                                        new() { Id = "_BTOPFIN_5", Name = "_BTopFin_5" },
                                        new() { Id = "_BTOPFIN_4", Name = "_BTopFin_4" },
                                        new() { Id = "_BTOPFIN_1", Name = "_BTopFin_1" },
                                        new() { Id = "_BTOPFIN_2", Name = "_BTopFin_2" },
                                        new() { Id = "_BTOPFIN_6", Name = "_BTopFin_6" },
                                        new() { Id = "_BTOPFIN_7", Name = "_BTopFin_7" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_FLIPPERS_", Descriptors = new()
                            {
                                new() { Id = "_FLIPPERS_NULL", Name = "_Flippers_NULL" },
                                new() { Id = "_FLIPPERS_1", Name = "_Flippers_1" },
                                new() { Id = "_FLIPPERS_2", Name = "_Flippers_2" },
                                new() { Id = "_FLIPPERS_3", Name = "_Flippers_3" },
                                new() { Id = "_FLIPPERS_4", Name = "_Flippers_4" },
                                new() { Id = "_FLIPPERS_5", Name = "_Flippers_5" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_SHARK_2", Name = "_Shark_2", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_1", Name = "_Head_1" },
                        new() { Id = "_HEAD_EEL", Name = "_Head_Eel" },
                        new() { Id = "_HEAD_WORM", Name = "_Head_Worm" },
                        new() { Id = "_HEAD_DRAGON", Name = "_Head_Dragon", Children = new()
                        {
                            new() { GroupId = "_HWACC_", Descriptors = new()
                            {
                                new() { Id = "_HWACC_NONE", Name = "_HWAcc_none" },
                                new() { Id = "_HWACC_1XRARE", Name = "_HWAcc_1xRARE" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_WINGS_", Descriptors = new()
                    {
                        new() { Id = "_WINGS_1", Name = "_Wings_1" },
                        new() { Id = "_WINGS_2", Name = "_Wings_2" },
                        new() { Id = "_WINGS_3", Name = "_Wings_3" },
                        new() { Id = "_WINGS_4", Name = "_Wings_4" },
                        new() { Id = "_WINGS_5", Name = "_Wings_5" },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_1", Name = "_Body_1", Children = new()
                        {
                            new() { GroupId = "_B1ACC_", Descriptors = new()
                            {
                                new() { Id = "_B1ACC_0", Name = "_B1Acc_0" },
                                new() { Id = "_B1ACC_1", Name = "_B1Acc_1" },
                                new() { Id = "_B1ACC_2", Name = "_B1Acc_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_EEL1", Name = "_Body_Eel1", Children = new()
                        {
                            new() { GroupId = "_BEELACC_", Descriptors = new()
                            {
                                new() { Id = "_BEELACC_1", Name = "_BEelAcc_1" },
                                new() { Id = "_BEELACC_3", Name = "_BEelAcc_3" },
                                new() { Id = "_BEELACC_4", Name = "_BEelAcc_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_WORM", Name = "_Body_Worm", Children = new()
                        {
                            new() { GroupId = "_BWACC_", Descriptors = new()
                            {
                                new() { Id = "_BWACC_1", Name = "_BWAcc_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_TAIL_", Descriptors = new()
                    {
                        new() { Id = "_TAIL_3", Name = "_Tail_3", Children = new()
                        {
                            new() { GroupId = "_TAILFIN3_", Descriptors = new()
                            {
                                new() { Id = "_TAILFIN3_1", Name = "_TailFin3_1" },
                                new() { Id = "_TAILFIN3_2", Name = "_TailFin3_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_WORM2", Name = "_Tail_Worm2" },
                        new() { Id = "_TAIL_EEL2", Name = "_Tail_Eel2" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "SIXLEGCAT", FriendlyName = "SIXLEGCAT", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_CAT", Name = "_Head_Cat", Children = new()
                {
                    new() { GroupId = "_HCACC_", Descriptors = new()
                    {
                        new() { Id = "_HCACC_A", Name = "_HCAcc_A", Children = new()
                        {
                            new() { GroupId = "_HCNOSE_", Descriptors = new()
                            {
                                new() { Id = "_HCNOSE_NULL", Name = "_HCNose_NULL" },
                                new() { Id = "_HCNOSE_A", Name = "_HCNose_A" },
                                new() { Id = "_HCNOSE_B", Name = "_HCNose_B" },
                            }
                            },
                            new() { GroupId = "_CHEARS_", Descriptors = new()
                            {
                                new() { Id = "_CHEARS_NULL", Name = "_CHEars_NULL" },
                                new() { Id = "_CHEARS_F", Name = "_CHEars_F" },
                                new() { Id = "_CHEARS_E", Name = "_CHEars_E" },
                                new() { Id = "_CHEARS_D", Name = "_CHEars_D" },
                                new() { Id = "_CHEARS_C", Name = "_CHEars_C" },
                                new() { Id = "_CHEARS_B", Name = "_CHEars_B" },
                                new() { Id = "_CHEARS_A", Name = "_CHEars_A" },
                            }
                            },
                            new() { GroupId = "_HCTOP_", Descriptors = new()
                            {
                                new() { Id = "_HCTOP_NULL", Name = "_HCTop_NULL" },
                                new() { Id = "_HCTOP_A", Name = "_HCTop_A" },
                                new() { Id = "_HCTOP_B", Name = "_HCTop_B" },
                                new() { Id = "_HCTOP_C", Name = "_HCTop_C" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HCACC_B", Name = "_HCAcc_B", Children = new()
                        {
                            new() { GroupId = "_HCTOPB_", Descriptors = new()
                            {
                                new() { Id = "_HCTOPB_NULL", Name = "_HCTopB_NULL" },
                                new() { Id = "_HCTOPB_A", Name = "_HCTopB_A" },
                                new() { Id = "_HCTOPB_B", Name = "_HCTopB_B" },
                                new() { Id = "_HCTOPB_C", Name = "_HCTopB_C" },
                                new() { Id = "_HCTOPB_D", Name = "_HCTopB_D" },
                                new() { Id = "_HCTOPB_E", Name = "_HCTopB_E" },
                                new() { Id = "_HCTOPB_F", Name = "_HCTopB_F" },
                                new() { Id = "_HCTOPB_G", Name = "_HCTopB_G" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HCACC_NULLXRARE", Name = "_HCAcc_NULLxRARE" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_HOG", Name = "_Head_Hog", Children = new()
                {
                    new() { GroupId = "_HHACCS_", Descriptors = new()
                    {
                        new() { Id = "_HHACCS_A", Name = "_HHAccs_A", Children = new()
                        {
                            new() { GroupId = "_HHNOSE_", Descriptors = new()
                            {
                                new() { Id = "_HHNOSE_NULL", Name = "_HHNose_NULL" },
                                new() { Id = "_HHNOSE_B", Name = "_HHNose_B" },
                                new() { Id = "_HHNOSE_C", Name = "_HHNose_C" },
                            }
                            },
                            new() { GroupId = "_HHEARS_", Descriptors = new()
                            {
                                new() { Id = "_HHEARS_NULL", Name = "_HHEars_NULL" },
                                new() { Id = "_HHEARS_A", Name = "_HHEars_A" },
                                new() { Id = "_HHEARS_B", Name = "_HHEars_B" },
                                new() { Id = "_HHEARS_C", Name = "_HHEars_C" },
                                new() { Id = "_HHEARS_D", Name = "_HHEars_D" },
                                new() { Id = "_HHEARS_E", Name = "_HHEars_E" },
                                new() { Id = "_HHEARS_F", Name = "_HHEars_F" },
                                new() { Id = "_HHEARS_G", Name = "_HHEars_G" },
                                new() { Id = "_HHEARS_H", Name = "_HHEars_H" },
                                new() { Id = "_HHEARS_I", Name = "_HHEars_I" },
                                new() { Id = "_HHEARS_J", Name = "_HHEars_J" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HHACCS_B", Name = "_HHAccs_B", Children = new()
                        {
                            new() { GroupId = "_HHTOP_", Descriptors = new()
                            {
                                new() { Id = "_HHTOP_G", Name = "_HHToP_G" },
                            }
                            },
                            new() { GroupId = "_HHBEAR_", Descriptors = new()
                            {
                                new() { Id = "_HHBEAR_NULL", Name = "_HHBEar_NULL" },
                                new() { Id = "_HHBEAR_A", Name = "_HHBEar_A" },
                                new() { Id = "_HHBEAR_B", Name = "_HHBEar_B" },
                                new() { Id = "_HHBEAR_C", Name = "_HHBEar_C" },
                                new() { Id = "_HHBEAR_D", Name = "_HHBEar_D" },
                                new() { Id = "_HHBEAR_E", Name = "_HHBEar_E" },
                                new() { Id = "_HHBEAR_F", Name = "_HHBEar_F" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HHACCS_NULLXRARE", Name = "_HHAccs_NULLxRARE" },
                    }
                    },
                    new() { GroupId = "_HHTUSK_", Descriptors = new()
                    {
                        new() { Id = "_HHTUSK_1", Name = "_HHTusk_1" },
                        new() { Id = "_HHTUSK_2", Name = "_HHTusk_2" },
                        new() { Id = "_HHTUSK_3", Name = "_HHTusk_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LIZARD", Name = "_Head_Lizard", Children = new()
                {
                    new() { GroupId = "_HLACC_", Descriptors = new()
                    {
                        new() { Id = "_HLACC_A", Name = "_HLAcc_A", Children = new()
                        {
                            new() { GroupId = "_HLMIDDLE_", Descriptors = new()
                            {
                                new() { Id = "_HLMIDDLE_NULL", Name = "_HLMiddle_NULL" },
                                new() { Id = "_HLMIDDLE_A", Name = "_HLMiddle_A" },
                                new() { Id = "_HLMIDDLE_B", Name = "_HLMiddle_B" },
                                new() { Id = "_HLMIDDLE_C", Name = "_HLMiddle_C" },
                            }
                            },
                            new() { GroupId = "_HLEARS_", Descriptors = new()
                            {
                                new() { Id = "_HLEARS_NULL", Name = "_HLEars_NULL" },
                                new() { Id = "_HLEARS_A", Name = "_HLEars_A" },
                                new() { Id = "_HLEARS_B", Name = "_HLEars_B" },
                                new() { Id = "_HLEARS_C", Name = "_HLEars_C" },
                                new() { Id = "_HLEARS_D", Name = "_HLEars_D" },
                                new() { Id = "_HLEARS_E", Name = "_HLEars_E" },
                            }
                            },
                            new() { GroupId = "_HLACC_", Descriptors = new()
                            {
                                new() { Id = "_HLACC_NULL", Name = "_HLAcc_NULL" },
                            }
                            },
                            new() { GroupId = "_LHNOSE_", Descriptors = new()
                            {
                                new() { Id = "_LHNOSE_NULL", Name = "_LHNose_NULL" },
                                new() { Id = "_LHNOSE_A", Name = "_LHNose_A" },
                                new() { Id = "_LHNOSE_B", Name = "_LHNose_B" },
                                new() { Id = "_LHNOSE_C", Name = "_LHNose_C" },
                                new() { Id = "_LHNOSE_D", Name = "_LHNose_D" },
                                new() { Id = "_LHNOSE_E", Name = "_LHNose_E" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HLACC_B", Name = "_HLAcc_B", Children = new()
                        {
                            new() { GroupId = "_HLTOP_", Descriptors = new()
                            {
                                new() { Id = "_HLTOP_NULL", Name = "_HLTop_NULL" },
                                new() { Id = "_HLTOP_A", Name = "_HLTop_A" },
                                new() { Id = "_HLTOP_B", Name = "_HLTop_B" },
                                new() { Id = "_HLTOP_C", Name = "_HLTop_C" },
                                new() { Id = "_HLTOP_D", Name = "_HLTop_D" },
                                new() { Id = "_HLTOP_E", Name = "_HLTop_E" },
                                new() { Id = "_HLTOP_F", Name = "_HLTop_F" },
                                new() { Id = "_HLTOP_G", Name = "_HLTop_G" },
                                new() { Id = "_HLTOP_H", Name = "_HLTop_H" },
                                new() { Id = "_HLTOP_I", Name = "_HLTop_I" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_WOLF", Name = "_Head_Wolf", Children = new()
                {
                    new() { GroupId = "_HWACC_", Descriptors = new()
                    {
                        new() { Id = "_HWACC_NULLXRARE", Name = "_HWAcc_NULLxRARE" },
                        new() { Id = "_HWACC_B", Name = "_HWAcc_B", Children = new()
                        {
                            new() { GroupId = "_HWMIDDLE_", Descriptors = new()
                            {
                                new() { Id = "_HWMIDDLE_NULL", Name = "_HWMiddle_NULL" },
                                new() { Id = "_HWMIDDLE_A", Name = "_HWMiddle_A" },
                                new() { Id = "_HWMIDDLE_B", Name = "_HWMiddle_B" },
                                new() { Id = "_HWMIDDLE_C", Name = "_HWMiddle_C" },
                            }
                            },
                            new() { GroupId = "_HWNOSE_", Descriptors = new()
                            {
                                new() { Id = "_HWNOSE_NULL", Name = "_HWNose_NULL" },
                                new() { Id = "_HWNOSE_A", Name = "_HWNose_A" },
                                new() { Id = "_HWNOSE_B", Name = "_HWNose_B" },
                            }
                            },
                            new() { GroupId = "_HWEARS_", Descriptors = new()
                            {
                                new() { Id = "_HWEARS_NULL", Name = "_HWEars_NULL" },
                                new() { Id = "_HWEARS_A", Name = "_HWEars_A" },
                                new() { Id = "_HWEARS_B", Name = "_HWEars_B" },
                                new() { Id = "_HWEARS_D", Name = "_HWEars_D" },
                                new() { Id = "_HWEARS_E", Name = "_HWEars_E" },
                                new() { Id = "_HWEARS_F", Name = "_HWEars_F" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HWACC_A", Name = "_HWAcc_A", Children = new()
                        {
                            new() { GroupId = "_HWTOP_", Descriptors = new()
                            {
                                new() { Id = "_HWTOP_NULL", Name = "_HWTop_NULL" },
                                new() { Id = "_HWTOP_A", Name = "_HWTop_A" },
                                new() { Id = "_HWTOP_B", Name = "_HWTop_B" },
                                new() { Id = "_HWTOP_C", Name = "_HWTop_C" },
                                new() { Id = "_HWTOP_D", Name = "_HWTop_D" },
                                new() { Id = "_HWTOP_E", Name = "_HWTop_E" },
                                new() { Id = "_HWTOP_F", Name = "_HWTop_F" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_SIXLEGCAT", Name = "_Body_SixLegCat", Children = new()
                {
                    new() { GroupId = "_B6CA_", Descriptors = new()
                    {
                        new() { Id = "_B6CA_NULL", Name = "_B6CA_Null" },
                        new() { Id = "_B6CA_BACKFUR1", Name = "_B6CA_BackFur1" },
                        new() { Id = "_B6CA_TURTSHELL1XRARE", Name = "_B6CA_TurtShell1xRARE" },
                        new() { Id = "_B6CA_ROCKS1XRARE", Name = "_B6CA_Rocks1xRARE", Children = new()
                        {
                            new() { GroupId = "_CEXT_", Descriptors = new()
                            {
                                new() { Id = "_CEXT_MINE", Name = "_CExt_Mine" },
                                new() { Id = "_CEXT_BLANKYO", Name = "_CExt_BlankYo" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6CA_BACKSTEGXRARE", Name = "_B6CA_BackStegxRARE" },
                        new() { Id = "_B6CA_EGGSXRARE", Name = "_B6CA_EggsxRARE" },
                        new() { Id = "_B6CA_FINLARGEXRARE", Name = "_B6CA_FinLargexRARE" },
                        new() { Id = "_B6CA_CRAGS", Name = "_B6CA_Crags" },
                        new() { Id = "_B6CA_XLARGEFINSXRARE", Name = "_B6CA_XLargeFinsxRARE" },
                        new() { Id = "_B6CA_XSHARKFINXRARE", Name = "_B6CA_XSharkFinxRARE" },
                        new() { Id = "_B6CA_FISHSHELLXRARE", Name = "_B6CA_FishShellxRARE", Children = new()
                        {
                            new() { GroupId = "_CSHELLACC_", Descriptors = new()
                            {
                                new() { Id = "_CSHELLACC_1", Name = "_CShellAcc_1" },
                                new() { Id = "_CSHELLACC_2", Name = "_CShellAcc_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6CA_SPINSXRARE", Name = "_B6CA_SpinsxRARE" },
                        new() { Id = "_B6CA_XTRIPLEFIN2XRARE", Name = "_B6CA_XTripleFin2xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_SIXLEGLIZ", Name = "_Body_SixLegLiz", Children = new()
                {
                    new() { GroupId = "_B6LA_", Descriptors = new()
                    {
                        new() { Id = "_B6LA_NULL2", Name = "_B6LA_Null2" },
                        new() { Id = "_B6LA_BACKFUR2", Name = "_B6LA_BackFur2" },
                        new() { Id = "_B6LA_TURTSHELL2XRARE", Name = "_B6LA_TurtShell2xRARE" },
                        new() { Id = "_B6LA_ROCKS2XRARE", Name = "_B6LA_Rocks2xRARE", Children = new()
                        {
                            new() { GroupId = "_EXTL2_", Descriptors = new()
                            {
                                new() { Id = "_EXTL2_MINE", Name = "_ExtL2_Mine" },
                                new() { Id = "_EXTL2_BLANKYO", Name = "_ExtL2_BlankYo" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6LA_BACKSTEG1XRARE", Name = "_B6LA_BackSteg1xRARE" },
                        new() { Id = "_B6LA_EGGS1XRARE", Name = "_B6LA_Eggs1xRARE" },
                        new() { Id = "_B6LA_FINLARGE1XRARE", Name = "_B6LA_FinLarge1xRARE" },
                        new() { Id = "_B6LA_CRAGS1", Name = "_B6LA_Crags1" },
                        new() { Id = "_B6LA_XLARGEFINS1XRARE", Name = "_B6LA_XLargeFins1xRARE" },
                        new() { Id = "_B6LA_XSHARKFIN1XRARE", Name = "_B6LA_XSharkFin1xRARE" },
                        new() { Id = "_B6LA_FISHSHELL1XRARE", Name = "_B6LA_FishShell1xRARE", Children = new()
                        {
                            new() { GroupId = "_LSHELLACC3_", Descriptors = new()
                            {
                                new() { Id = "_LSHELLACC3_1", Name = "_LShellAcc3_1" },
                                new() { Id = "_LSHELLACC3_2", Name = "_LShellAcc3_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6LA_SPINS1XRARE", Name = "_B6LA_Spins1xRARE" },
                        new() { Id = "_B6LA_XTRIPLEFIN4XRARE", Name = "_B6LA_XTripleFin4xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_SIXLEGWOLF", Name = "_Body_SixLegWolf", Children = new()
                {
                    new() { GroupId = "_B6CW_", Descriptors = new()
                    {
                        new() { Id = "_B6CW_NULL1", Name = "_B6CW_Null1" },
                        new() { Id = "_B6CW_BACKFUR3", Name = "_B6CW_BackFur3" },
                        new() { Id = "_B6CW_TURTSHELL3XRARE", Name = "_B6CW_TurtShell3xRARE" },
                        new() { Id = "_B6CW_ROCKS3XRARE", Name = "_B6CW_Rocks3xRARE", Children = new()
                        {
                            new() { GroupId = "_WEXT1_", Descriptors = new()
                            {
                                new() { Id = "_WEXT1_BLANKYO", Name = "_WExt1_BlankYo" },
                                new() { Id = "_WEXT1_MINE", Name = "_WExt1_Mine" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6CW_BACKSTEG2XRARE", Name = "_B6CW_BackSteg2xRARE" },
                        new() { Id = "_B6CW_EGGS2XRARE", Name = "_B6CW_Eggs2xRARE" },
                        new() { Id = "_B6CW_FINLARGE2XRARE", Name = "_B6CW_FinLarge2xRARE" },
                        new() { Id = "_B6CW_CRAGS2", Name = "_B6CW_Crags2" },
                        new() { Id = "_B6CW_XLARGEFINS2XRARE", Name = "_B6CW_XLargeFins2xRARE" },
                        new() { Id = "_B6CW_XSHARKFIN2XRARE", Name = "_B6CW_XSharkFin2xRARE" },
                        new() { Id = "_B6CW_FISHSHELL2XRARE", Name = "_B6CW_FishShell2xRARE", Children = new()
                        {
                            new() { GroupId = "_WSHELLACC1_", Descriptors = new()
                            {
                                new() { Id = "_WSHELLACC1_1", Name = "_WShellAcc1_1" },
                                new() { Id = "_WSHELLACC1_2", Name = "_WShellAcc1_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6CW_XTRIPLEFIN4XRARE", Name = "_B6CW_XTripleFin4xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_CAT", Name = "_Tail_Cat" },
                new() { Id = "_TAIL_HOG", Name = "_Tail_Hog", Children = new()
                {
                    new() { GroupId = "_THA_", Descriptors = new()
                    {
                        new() { Id = "_THA_X1", Name = "_THA_X1" },
                        new() { Id = "_THA_X2", Name = "_THA_X2" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_LIZ1", Name = "_Tail_Liz1" },
                new() { Id = "_TAIL_WOLF", Name = "_Tail_Wolf" },
            }
            },
        }
        },
        new() { CreatureId = "SIXLEGGEDCOW", FriendlyName = "SIXLEGGEDCOW", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIENBIRD", Name = "_Head_AlienBird", Children = new()
                {
                    new() { GroupId = "_HABACC_", Descriptors = new()
                    {
                        new() { Id = "_HABACC_1", Name = "_HABAcc_1" },
                        new() { Id = "_HABACC_2", Name = "_HABAcc_2" },
                        new() { Id = "_HABACC_3", Name = "_HABAcc_3" },
                        new() { Id = "_HABACC_BLANK", Name = "_HABAcc_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUG", Name = "_Head_Bug", Children = new()
                {
                    new() { GroupId = "_HBUGACC_", Descriptors = new()
                    {
                        new() { Id = "_HBUGACC_2", Name = "_HBugAcc_2" },
                        new() { Id = "_HBUGACC_1", Name = "_HBugAcc_1" },
                        new() { Id = "_HBUGACC_BLANK", Name = "_HBugAcc_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COW", Name = "_Head_Cow", Children = new()
                {
                    new() { GroupId = "_HCE_", Descriptors = new()
                    {
                        new() { Id = "_HCE_BLANK", Name = "_HCE_Blank" },
                        new() { Id = "_HCE_COWEARS", Name = "_HCE_CowEars" },
                    }
                    },
                    new() { GroupId = "_HCH_", Descriptors = new()
                    {
                        new() { Id = "_HCH_BLANK", Name = "_HCH_Blank" },
                        new() { Id = "_HCH_LORISHORN", Name = "_HCH_LorisHorn" },
                        new() { Id = "_HCH_COWHORN", Name = "_HCH_CowHorn" },
                        new() { Id = "_HCH_ANTHORN", Name = "_HCH_AntHorn" },
                        new() { Id = "_HCH_NOSEBONE", Name = "_HCH_NoseBone" },
                        new() { Id = "_HCH_HEADBONE", Name = "_HCH_HeadBone" },
                        new() { Id = "_HCH_HEADPLATE", Name = "_HCH_HeadPlate" },
                        new() { Id = "_HCH_MULTIHORN", Name = "_HCH_MultiHorn" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COWSKEW", Name = "_Head_CowSkew", Children = new()
                {
                    new() { GroupId = "_HCSE_", Descriptors = new()
                    {
                        new() { Id = "_HCSE_COWEARS", Name = "_HCSE_CowEars" },
                        new() { Id = "_HCSE_BLANK", Name = "_HCSE_Blank" },
                    }
                    },
                    new() { GroupId = "_HCSH_", Descriptors = new()
                    {
                        new() { Id = "_HCSH_HEADACC", Name = "_HCSH_HeadAcc" },
                        new() { Id = "_HCSH_HEADBONE", Name = "_HCSH_HeadBone" },
                        new() { Id = "_HCSH_ANTHORN", Name = "_HCSH_AntHorn" },
                        new() { Id = "_HCSH_LORISHORN", Name = "_HCSH_LorisHorn" },
                        new() { Id = "_HCSH_COWHORN", Name = "_HCSH_CowHorn" },
                        new() { Id = "_HCSH_BLANK", Name = "_HCSH_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LORIS", Name = "_Head_Loris", Children = new()
                {
                    new() { GroupId = "_HLH_", Descriptors = new()
                    {
                        new() { Id = "_HLH_BLANK", Name = "_HLH_Blank" },
                        new() { Id = "_HLH_LORISHORN", Name = "_HLH_LorisHorn" },
                        new() { Id = "_HLH_COWHORN", Name = "_HLH_CowHorn" },
                        new() { Id = "_HLH_ANTHORN", Name = "_HLH_AntHorn" },
                        new() { Id = "_HLH_HEADPLATE", Name = "_HLH_HeadPlate" },
                        new() { Id = "_HLH_NOSEBONE", Name = "_HLH_NoseBone" },
                        new() { Id = "_HLH_MULTIHORN3", Name = "_HLH_MultiHorn3" },
                    }
                    },
                    new() { GroupId = "_HLE_", Descriptors = new()
                    {
                        new() { Id = "_HLE_BLANK", Name = "_HLE_Blank" },
                        new() { Id = "_HLE_1", Name = "_HLE_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_REX", Name = "_Head_Rex", Children = new()
                {
                    new() { GroupId = "_HREXACC_", Descriptors = new()
                    {
                        new() { Id = "_HREXACC_1", Name = "_HRexAcc_1" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_6LEGCOW1", Name = "_Body_6LegCow1", Children = new()
                {
                    new() { GroupId = "_B6CA_", Descriptors = new()
                    {
                        new() { Id = "_B6CA_LUMP", Name = "_B6CA_Lump" },
                        new() { Id = "_B6CA_BACKFIN", Name = "_B6CA_BackFin" },
                        new() { Id = "_B6CA_TURTSHELL", Name = "_B6CA_TurtShell" },
                        new() { Id = "_B6CA_STEGSPIKES", Name = "_B6CA_StegSpikes" },
                        new() { Id = "_B6CA_BACKSPINES", Name = "_B6CA_BackSpines" },
                        new() { Id = "_B6CA_ROCKS", Name = "_B6CA_Rocks", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE6", Name = "_Ext_Mine6" },
                                new() { Id = "_EXT_BLANKYO6", Name = "_Ext_BlankYo6" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6CA_COWHUMP", Name = "_B6CA_CowHump", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_BACKSPINES6", Name = "_Ext_BackSpines6" },
                                new() { Id = "_EXT_BLANKYO36", Name = "_Ext_BlankYo36" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6CA_BLANK1", Name = "_B6CA_Blank1" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_6LEGROCK1", Name = "_Body_6LegRock1", Children = new()
                {
                    new() { GroupId = "_B6RA_", Descriptors = new()
                    {
                        new() { Id = "_B6RA_BLANK2", Name = "_B6RA_Blank2" },
                        new() { Id = "_B6RA_ROCKS1", Name = "_B6RA_Rocks1", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE226", Name = "_Ext_Mine226" },
                                new() { Id = "_EXT_BLANKYO226", Name = "_Ext_BlankYo226" },
                            }
                            },
                        }
                        },
                        new() { Id = "_B6RA_ARMSPINES", Name = "_B6RA_ArmSpines" },
                        new() { Id = "_B6RA_BACKSPINES", Name = "_B6RA_BackSpines" },
                        new() { Id = "_B6RA_STEGSPIKES", Name = "_B6RA_StegSpikes" },
                        new() { Id = "_B6RA_TURTSHELL", Name = "_B6RA_TurtShell" },
                        new() { Id = "_B6RA_BACKFIN", Name = "_B6RA_BackFin" },
                        new() { Id = "_B6RA_LUMP1", Name = "_B6RA_Lump1" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_ALIEN1", Name = "_Tail_Alien1", Children = new()
                {
                    new() { GroupId = "_TAACC_", Descriptors = new()
                    {
                        new() { Id = "_TAACC_0", Name = "_TAacc_0" },
                        new() { Id = "_TAACC_1", Name = "_TAacc_1" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_COW1", Name = "_Tail_Cow1" },
                new() { Id = "_TAIL_TURTLE1", Name = "_Tail_Turtle1" },
            }
            },
        }
        },
        new() { CreatureId = "SMALLBIRD", FriendlyName = "SMALLBIRD", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_BAT", Name = "_Head_Bat", Children = new()
                {
                    new() { GroupId = "_HBAT_", Descriptors = new()
                    {
                        new() { Id = "_HBAT_0", Name = "_HBat_0" },
                        new() { Id = "_HBAT_1", Name = "_HBat_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BIRD", Name = "_Head_Bird", Children = new()
                {
                    new() { GroupId = "_HBACC_", Descriptors = new()
                    {
                        new() { Id = "_HBACC_1", Name = "_HBAcc_1" },
                        new() { Id = "_HBACC_2", Name = "_HBAcc_2" },
                        new() { Id = "_HBACC_3", Name = "_HBAcc_3" },
                        new() { Id = "_HBACC_4", Name = "_HBAcc_4" },
                        new() { Id = "_HBACC_5", Name = "_HBAcc_5" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_WINGS_", Descriptors = new()
            {
                new() { Id = "_WINGS_1", Name = "_Wings_1" },
                new() { Id = "_WINGS_2", Name = "_Wings_2", Children = new()
                {
                    new() { GroupId = "_W2ACC_", Descriptors = new()
                    {
                        new() { Id = "_W2ACC_0", Name = "_W2Acc_0" },
                        new() { Id = "_W2ACC_1", Name = "_W2Acc_1" },
                    }
                    },
                }
                },
                new() { Id = "_WINGS_3", Name = "_Wings_3" },
                new() { Id = "_WINGS_4", Name = "_Wings_4" },
                new() { Id = "_WINGS_5", Name = "_Wings_5" },
                new() { Id = "_WINGS_6", Name = "_Wings_6" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_BIRD", Name = "_Body_Bird" },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_BAT", Name = "_Tail_Bat" },
                new() { Id = "_TAIL_BIRD", Name = "_Tail_Bird" },
                new() { Id = "_TAIL_BUG", Name = "_Tail_Bug" },
                new() { Id = "_TAIL_LONG", Name = "_Tail_Long" },
                new() { Id = "_TAIL_THIN", Name = "_Tail_Thin", Children = new()
                {
                    new() { GroupId = "_TTHINACC_", Descriptors = new()
                    {
                        new() { Id = "_TTHINACC_0", Name = "_TThinAcc_0" },
                        new() { Id = "_TTHINACC_1", Name = "_TThinAcc_1" },
                        new() { Id = "_TTHINACC_2", Name = "_TThinAcc_2" },
                        new() { Id = "_TTHINACC_3", Name = "_TThinAcc_3" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "SMALLFISH", FriendlyName = "SMALLFISH", Details = new()
        {
            new() { GroupId = "_FISH_", Descriptors = new()
            {
                new() { Id = "_FISH_A", Name = "_Fish_A", Children = new()
                {
                    new() { GroupId = "_TAIL_", Descriptors = new()
                    {
                        new() { Id = "_TAIL_1", Name = "_Tail_1" },
                        new() { Id = "_TAIL_2", Name = "_Tail_2" },
                        new() { Id = "_TAIL_3", Name = "_Tail_3", Children = new()
                        {
                            new() { GroupId = "_T3ACC_", Descriptors = new()
                            {
                                new() { Id = "_T3ACC_1", Name = "_T3Acc_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_4", Name = "_Tail_4" },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_1", Name = "_Body_1", Children = new()
                        {
                            new() { GroupId = "_SIDEFIN_", Descriptors = new()
                            {
                                new() { Id = "_SIDEFIN_1", Name = "_SideFin_1" },
                                new() { Id = "_SIDEFIN_2", Name = "_SideFin_2" },
                                new() { Id = "_SIDEFIN_3", Name = "_SideFin_3" },
                                new() { Id = "_SIDEFIN_4", Name = "_SideFin_4" },
                                new() { Id = "_SIDEFIN_5", Name = "_SideFin_5" },
                            }
                            },
                            new() { GroupId = "_TAILFIN_", Descriptors = new()
                            {
                                new() { Id = "_TAILFIN_1", Name = "_TailFin_1" },
                                new() { Id = "_TAILFIN_2", Name = "_TailFin_2" },
                            }
                            },
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_1", Name = "_Head_1", Children = new()
                                {
                                    new() { GroupId = "_H1ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H1ACC_NULL", Name = "_H1Acc_NULL" },
                                        new() { Id = "_H1ACC_1", Name = "_H1Acc_1" },
                                        new() { Id = "_H1ACC_2", Name = "_H1Acc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_2", Name = "_Head_2", Children = new()
                                {
                                    new() { GroupId = "_H2ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H2ACC_NULL", Name = "_H2Acc_NULL" },
                                        new() { Id = "_H2ACC_1", Name = "_H2Acc_1" },
                                        new() { Id = "_H2ACC_2", Name = "_H2Acc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_3", Name = "_Head_3", Children = new()
                                {
                                    new() { GroupId = "_H3ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H3ACC_NULL", Name = "_H3Acc_NULL" },
                                        new() { Id = "_H3ACC_1", Name = "_H3Acc_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_4", Name = "_Head_4", Children = new()
                                {
                                    new() { GroupId = "_H4ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H4ACC_NULL", Name = "_H4Acc_NULL" },
                                        new() { Id = "_H4ACC_1", Name = "_H4Acc_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_5", Name = "_Head_5", Children = new()
                                {
                                    new() { GroupId = "_H5ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H5ACC_NULL", Name = "_H5Acc_NULL" },
                                        new() { Id = "_H5ACC_1", Name = "_H5Acc_1" },
                                        new() { Id = "_H5ACC_2", Name = "_H5Acc_2" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_TOPFIN_", Descriptors = new()
                            {
                                new() { Id = "_TOPFIN_1", Name = "_TopFin_1" },
                                new() { Id = "_TOPFIN_2", Name = "_TopFin_2" },
                                new() { Id = "_TOPFIN_3", Name = "_TopFin_3" },
                                new() { Id = "_TOPFIN_4", Name = "_TopFin_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_2", Name = "_Body_2", Children = new()
                        {
                            new() { GroupId = "_SIDEFIN_", Descriptors = new()
                            {
                                new() { Id = "_SIDEFIN_6", Name = "_SideFin_6" },
                                new() { Id = "_SIDEFIN_7", Name = "_SideFin_7" },
                                new() { Id = "_SIDEFIN_8", Name = "_SideFin_8" },
                            }
                            },
                            new() { GroupId = "_TAILFIN_", Descriptors = new()
                            {
                                new() { Id = "_TAILFIN_3", Name = "_TailFin_3" },
                                new() { Id = "_TAILFIN_4", Name = "_TailFin_4" },
                            }
                            },
                            new() { GroupId = "_HEAD_", Descriptors = new()
                            {
                                new() { Id = "_HEAD_6", Name = "_Head_6", Children = new()
                                {
                                    new() { GroupId = "_H6ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H6ACC_NULL", Name = "_H6Acc_NULL" },
                                        new() { Id = "_H6ACC_1", Name = "_H6Acc_1" },
                                        new() { Id = "_H6ACC_2", Name = "_H6Acc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_7", Name = "_Head_7", Children = new()
                                {
                                    new() { GroupId = "_H7ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H7ACC_NULL", Name = "_H7Acc_NULL" },
                                        new() { Id = "_H7ACC_1", Name = "_H7Acc_1" },
                                        new() { Id = "_H7ACC_2", Name = "_H7Acc_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_8", Name = "_Head_8", Children = new()
                                {
                                    new() { GroupId = "_H8ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H8ACC_NULL", Name = "_H8Acc_NULL" },
                                        new() { Id = "_H8ACC_1", Name = "_H8Acc_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_9", Name = "_Head_9", Children = new()
                                {
                                    new() { GroupId = "_H9ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H9ACC_NULL", Name = "_H9Acc_NULL" },
                                        new() { Id = "_H9ACC_1", Name = "_H9Acc_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_HEAD_10", Name = "_Head_10", Children = new()
                                {
                                    new() { GroupId = "_H10ACC_", Descriptors = new()
                                    {
                                        new() { Id = "_H10ACC_NULL", Name = "_H10Acc_NULL" },
                                        new() { Id = "_H10ACC_1", Name = "_H10Acc_1" },
                                        new() { Id = "_H10ACC_2", Name = "_H10Acc_2" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_TOPFIN_", Descriptors = new()
                            {
                                new() { Id = "_TOPFIN_5", Name = "_TopFin_5" },
                                new() { Id = "_TOPFIN_6", Name = "_TopFin_6" },
                                new() { Id = "_TOPFIN_7", Name = "_TopFin_7" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "SPIDER", FriendlyName = "SPIDER", Details = new()
        {
            new() { GroupId = "_MANTIS_", Descriptors = new()
            {
                new() { Id = "_MANTIS_A", Name = "_Mantis_A", Children = new()
                {
                    new() { GroupId = "_ARMS_", Descriptors = new()
                    {
                        new() { Id = "_ARMS_NULL", Name = "_Arms_NULL" },
                        new() { Id = "_ARMS_1", Name = "_Arms_1" },
                        new() { Id = "_ARMS_2", Name = "_Arms_2" },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_1XRARE", Name = "_Body_1xRARE", Children = new()
                        {
                            new() { GroupId = "_TRUNK_", Descriptors = new()
                            {
                                new() { Id = "_TRUNK_NULL", Name = "_Trunk_NULL" },
                                new() { Id = "_TRUNK_NULL2", Name = "_Trunk_NULL2" },
                                new() { Id = "_TRUNK_1XRARE", Name = "_Trunk_1xRARE", Children = new()
                                {
                                    new() { GroupId = "_CAP1_", Descriptors = new()
                                    {
                                        new() { Id = "_CAP1_3", Name = "_Cap1_3", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILL5_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILL5_1", Name = "_CapFill5_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CAP1_2", Name = "_Cap1_2", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILL2_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILL2_1", Name = "_CapFill2_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CAP1_1", Name = "_Cap1_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_TRUNK_2XRARE", Name = "_Trunk_2xRare", Children = new()
                                {
                                    new() { GroupId = "_CAPB_", Descriptors = new()
                                    {
                                        new() { Id = "_CAPB_11", Name = "_CapB_11", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILLB_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILLB_1", Name = "_CapFillB_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_CAPB_12", Name = "_CapB_12", Children = new()
                                        {
                                            new() { GroupId = "_CAPFILL2B_", Descriptors = new()
                                            {
                                                new() { Id = "_CAPFILL2B_1", Name = "_CapFill2B_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_2", Name = "_Body_2", Children = new()
                        {
                            new() { GroupId = "_NECK_", Descriptors = new()
                            {
                                new() { Id = "_NECK_MANTIS", Name = "_Neck_Mantis", Children = new()
                                {
                                    new() { GroupId = "_HEAD_", Descriptors = new()
                                    {
                                        new() { Id = "_HEAD_MANTIS", Name = "_Head_Mantis", Children = new()
                                        {
                                            new() { GroupId = "_MHACC_", Descriptors = new()
                                            {
                                                new() { Id = "_MHACC_NULL", Name = "_MHAcc_NULL" },
                                                new() { Id = "_MHACC_1", Name = "_MHAcc_1" },
                                            }
                                            },
                                            new() { GroupId = "_MEYES_", Descriptors = new()
                                            {
                                                new() { Id = "_MEYES_1", Name = "_MEyes_1" },
                                                new() { Id = "_MEYES_2", Name = "_MEyes_2" },
                                            }
                                            },
                                            new() { GroupId = "_MANTENNA_", Descriptors = new()
                                            {
                                                new() { Id = "_MANTENNA_NULL", Name = "_MAntenna_NULL" },
                                                new() { Id = "_MANTENNA_1", Name = "_MAntenna_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_MARM_", Descriptors = new()
                                    {
                                        new() { Id = "_MARM_A", Name = "_MArm_A" },
                                        new() { Id = "_MARM_B", Name = "_MArm_B" },
                                    }
                                    },
                                    new() { GroupId = "_TAIL_", Descriptors = new()
                                    {
                                        new() { Id = "_TAIL_NULL", Name = "_Tail_NULL" },
                                        new() { Id = "_TAIL_MANTIS", Name = "_Tail_Mantis" },
                                        new() { Id = "_TAIL_FINFLAPS", Name = "_Tail_FinFlaps" },
                                    }
                                    },
                                    new() { GroupId = "_ACM_", Descriptors = new()
                                    {
                                        new() { Id = "_ACM_NONE", Name = "_ACM_none" },
                                        new() { Id = "_ACM_BULBSIDE", Name = "_ACM_BulbSide", Children = new()
                                        {
                                            new() { GroupId = "_ACMB_", Descriptors = new()
                                            {
                                                new() { Id = "_ACMB_SPIKES", Name = "_ACMb_Spikes" },
                                                new() { Id = "_ACMB_NONE", Name = "_ACMb_none" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_NECK_BUG", Name = "_Neck_Bug", Children = new()
                                {
                                    new() { GroupId = "_ACC1_", Descriptors = new()
                                    {
                                        new() { Id = "_ACC1_FINFLAPS", Name = "_Acc1_finFlaps" },
                                        new() { Id = "_ACC1_MANTISTAIL", Name = "_Acc1_MantisTail" },
                                        new() { Id = "_ACC1_BORDER", Name = "_Acc1_Border" },
                                        new() { Id = "_ACC1_BULBSIDE", Name = "_Acc1_BulbSide", Children = new()
                                        {
                                            new() { GroupId = "_ACC5_", Descriptors = new()
                                            {
                                                new() { Id = "_ACC5_SPIKES", Name = "_Acc5_Spikes" },
                                                new() { Id = "_ACC5_NONE", Name = "_Acc5_none" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ACC1_BULBTOP", Name = "_Acc1_BulbTop", Children = new()
                                        {
                                            new() { GroupId = "_ACC2_", Descriptors = new()
                                            {
                                                new() { Id = "_ACC2_BULBHAT", Name = "_Acc2_BulbHat", Children = new()
                                                {
                                                    new() { GroupId = "_ACC3_", Descriptors = new()
                                                    {
                                                        new() { Id = "_ACC3_NONE", Name = "_Acc3_none" },
                                                        new() { Id = "_ACC3_SPIKES", Name = "_Acc3_Spikes" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_ALTH_", Descriptors = new()
                                    {
                                        new() { Id = "_ALTH_MANTS", Name = "_AltH_Mants", Children = new()
                                        {
                                            new() { GroupId = "_MANTNA_", Descriptors = new()
                                            {
                                                new() { Id = "_MANTNA_1", Name = "_MAntna_1" },
                                            }
                                            },
                                            new() { GroupId = "_MEYE_", Descriptors = new()
                                            {
                                                new() { Id = "_MEYE_2", Name = "_MEye_2" },
                                                new() { Id = "_MEYE_1", Name = "_MEye_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ALTH_MANTB", Name = "_AltH_MantB", Children = new()
                                        {
                                            new() { GroupId = "_MANTNA_", Descriptors = new()
                                            {
                                                new() { Id = "_MANTNA_2", Name = "_MAntna_2" },
                                            }
                                            },
                                            new() { GroupId = "_MEYYE_", Descriptors = new()
                                            {
                                                new() { Id = "_MEYYE_2", Name = "_MEyye_2" },
                                            }
                                            },
                                            new() { GroupId = "_MOUT_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUT_A", Name = "_Mout_A" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ALTH_3", Name = "_AltH_3" },
                                        new() { Id = "_ALTH_2", Name = "_AltH_2" },
                                        new() { Id = "_ALTH_1", Name = "_AltH_1", Children = new()
                                        {
                                            new() { GroupId = "_EYES_", Descriptors = new()
                                            {
                                                new() { Id = "_EYES_1", Name = "_Eyes_1" },
                                            }
                                            },
                                            new() { GroupId = "_MOUTH_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUTH_1", Name = "_Mouth_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ALTH_5", Name = "_AltH_5" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_NECK_ALT", Name = "_Neck_Alt", Children = new()
                                {
                                    new() { GroupId = "_TAIL_", Descriptors = new()
                                    {
                                        new() { Id = "_TAIL_NULL1", Name = "_Tail_Null1" },
                                        new() { Id = "_TAIL_MANTISTAIL", Name = "_Tail_MantisTail" },
                                    }
                                    },
                                    new() { GroupId = "_HEAD_", Descriptors = new()
                                    {
                                        new() { Id = "_HEAD_MUFFIN", Name = "_Head_Muffin", Children = new()
                                        {
                                            new() { GroupId = "_ALTH2_", Descriptors = new()
                                            {
                                                new() { Id = "_ALTH2_BULBB", Name = "_AltH2_BulbB", Children = new()
                                                {
                                                    new() { GroupId = "_EYES_", Descriptors = new()
                                                    {
                                                        new() { Id = "_EYES_B", Name = "_Eyes_B" },
                                                    }
                                                    },
                                                    new() { GroupId = "_AT_", Descriptors = new()
                                                    {
                                                        new() { Id = "_AT_SPIKES", Name = "_AT_Spikes" },
                                                    }
                                                    },
                                                    new() { GroupId = "_MOUTH_", Descriptors = new()
                                                    {
                                                        new() { Id = "_MOUTH_B", Name = "_Mouth_B" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_HEAD_STALKS", Name = "_Head_Stalks", Children = new()
                                        {
                                            new() { GroupId = "_MOUTHS_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUTHS_1", Name = "_Mouths_1" },
                                            }
                                            },
                                            new() { GroupId = "_ATC_", Descriptors = new()
                                            {
                                                new() { Id = "_ATC_BORDER", Name = "_ATC_Border", Children = new()
                                                {
                                                    new() { GroupId = "_JELLYFINGERS_", Descriptors = new()
                                                    {
                                                        new() { Id = "_JELLYFINGERS_1", Name = "_JellyFingers_1" },
                                                        new() { Id = "_JELLYFINGERS_NONE", Name = "_JellyFingers_none" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                            new() { GroupId = "_EYESD_", Descriptors = new()
                                            {
                                                new() { Id = "_EYESD_1", Name = "_EyesD_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_NECK_CRAB", Name = "_Neck_Crab", Children = new()
                                {
                                    new() { GroupId = "_ACCR_", Descriptors = new()
                                    {
                                        new() { Id = "_ACCR_BORDER", Name = "_ACCR_Border" },
                                        new() { Id = "_ACCR_NONE", Name = "_ACCR_none" },
                                        new() { Id = "_ACCR_MANTISTAIL", Name = "_ACCR_MantisTail" },
                                        new() { Id = "_ACCR_FINFLAPS", Name = "_ACCR_FinFLaps" },
                                        new() { Id = "_ACCR_BULBSIDE", Name = "_ACCR_BulbSide", Children = new()
                                        {
                                            new() { GroupId = "_ACCRC_", Descriptors = new()
                                            {
                                                new() { Id = "_ACCRC_SPIKES", Name = "_ACCRc_Spikes" },
                                                new() { Id = "_ACCRC_NONE", Name = "_ACCRc_none" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_ACCR_BULBTOP", Name = "_ACCR_BulbTop", Children = new()
                                        {
                                            new() { GroupId = "_ACCR_", Descriptors = new()
                                            {
                                                new() { Id = "_ACCR_BULBHAT", Name = "_ACCR_BulbHat", Children = new()
                                                {
                                                    new() { GroupId = "_ACCRB_", Descriptors = new()
                                                    {
                                                        new() { Id = "_ACCRB_NONE", Name = "_ACCRb_none" },
                                                        new() { Id = "_ACCRB_SPIKES", Name = "_ACCRb_Spikes" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_LEGS_", Descriptors = new()
                    {
                        new() { Id = "_LEGS_1", Name = "_Legs_1", Children = new()
                        {
                            new() { GroupId = "_LEGA_", Descriptors = new()
                            {
                                new() { Id = "_LEGA_1", Name = "_LegA_1" },
                            }
                            },
                            new() { GroupId = "_LEGC_", Descriptors = new()
                            {
                                new() { Id = "_LEGC_1", Name = "_LegC_1" },
                                new() { Id = "_LEGC_NULL", Name = "_LegC_NULL" },
                            }
                            },
                            new() { GroupId = "_LEGB_", Descriptors = new()
                            {
                                new() { Id = "_LEGB_1", Name = "_LegB_1" },
                                new() { Id = "_LEGB_NULL", Name = "_LegB_NULL" },
                            }
                            },
                        }
                        },
                        new() { Id = "_LEGS_3", Name = "_Legs_3", Children = new()
                        {
                            new() { GroupId = "_LEGGC_", Descriptors = new()
                            {
                                new() { Id = "_LEGGC_NONE", Name = "_LeggC_none" },
                                new() { Id = "_LEGGC_1", Name = "_LeggC_1" },
                            }
                            },
                            new() { GroupId = "_LEGGA_", Descriptors = new()
                            {
                                new() { Id = "_LEGGA_1", Name = "_LeggA_1" },
                            }
                            },
                            new() { GroupId = "_LEGGD_", Descriptors = new()
                            {
                                new() { Id = "_LEGGD_1", Name = "_LeggD_1" },
                            }
                            },
                            new() { GroupId = "_LEGGB_", Descriptors = new()
                            {
                                new() { Id = "_LEGGB_NONE", Name = "_LeggB_none" },
                                new() { Id = "_LEGGB_1", Name = "_LeggB_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_LEGS_2", Name = "_Legs_2" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "STRIDER", FriendlyName = "STRIDER", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_BUG", Name = "_Head_Bug", Children = new()
                {
                    new() { GroupId = "_SH2ACC_", Descriptors = new()
                    {
                        new() { Id = "_SH2ACC_1XRARE", Name = "_SH2Acc_1xRARE" },
                        new() { Id = "_SH2ACC_NONE", Name = "_SH2Acc_none" },
                    }
                    },
                    new() { GroupId = "_BUGARM_", Descriptors = new()
                    {
                        new() { Id = "_BUGARM_0", Name = "_BugArm_0" },
                    }
                    },
                    new() { GroupId = "_ACCBACK_", Descriptors = new()
                    {
                        new() { Id = "_ACCBACK_1", Name = "_AccBack_1" },
                        new() { Id = "_ACCBACK_0", Name = "_AccBack_0" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_FISH", Name = "_Head_Fish", Children = new()
                {
                    new() { GroupId = "_FISH_", Descriptors = new()
                    {
                        new() { Id = "_FISH_1", Name = "_Fish_1", Children = new()
                        {
                            new() { GroupId = "_FISHEYES_", Descriptors = new()
                            {
                                new() { Id = "_FISHEYES_1", Name = "_FishEyes_1" },
                            }
                            },
                            new() { GroupId = "_FIN_", Descriptors = new()
                            {
                                new() { Id = "_FIN_NONE", Name = "_Fin_none" },
                                new() { Id = "_FIN_1XRARE", Name = "_Fin_1xRARE", Children = new()
                                {
                                    new() { GroupId = "_FILLE_", Descriptors = new()
                                    {
                                        new() { Id = "_FILLE_NONE", Name = "_FillE_none" },
                                        new() { Id = "_FILLE_1", Name = "_FillE_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_FISH_2XRARE", Name = "_Fish_2xRARE", Children = new()
                        {
                            new() { GroupId = "_FILLE2_", Descriptors = new()
                            {
                                new() { Id = "_FILLE2_NONE", Name = "_FillE2_none" },
                                new() { Id = "_FILLE2_1", Name = "_FillE2_1" },
                            }
                            },
                            new() { GroupId = "_NECK_", Descriptors = new()
                            {
                                new() { Id = "_NECK_1", Name = "_Neck_1" },
                            }
                            },
                            new() { GroupId = "_MOUTH_", Descriptors = new()
                            {
                                new() { Id = "_MOUTH_1", Name = "_Mouth_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_FLATHEAD", Name = "_Head_FlatHead", Children = new()
                {
                    new() { GroupId = "_FHACC_", Descriptors = new()
                    {
                        new() { Id = "_FHACC_7XRARE", Name = "_FHAcc_7xRARE" },
                        new() { Id = "_FHACC_3XRARE", Name = "_FHAcc_3xRARE" },
                        new() { Id = "_FHACC_5XRARE", Name = "_FHAcc_5xRARE" },
                        new() { Id = "_FHACC_6", Name = "_FHAcc_6" },
                        new() { Id = "_FHACC_4", Name = "_FHAcc_4" },
                        new() { Id = "_FHACC_3", Name = "_FHAcc_3" },
                        new() { Id = "_FHACC_0", Name = "_FHAcc_0" },
                        new() { Id = "_FHACC_2", Name = "_FHAcc_2" },
                        new() { Id = "_FHACC_1", Name = "_FHAcc_1" },
                        new() { Id = "_FHACC_2XRARE", Name = "_FHAcc_2xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_STRIDER", Name = "_Head_Strider", Children = new()
                {
                    new() { GroupId = "_SHACC_", Descriptors = new()
                    {
                        new() { Id = "_SHACC_0", Name = "_SHAcc_0" },
                        new() { Id = "_SHACC_5XRARE", Name = "_SHAcc_5xRARE" },
                        new() { Id = "_SHACC_1", Name = "_SHAcc_1" },
                        new() { Id = "_SHACC_2", Name = "_SHAcc_2" },
                        new() { Id = "_SHACC_4", Name = "_SHAcc_4" },
                        new() { Id = "_SHACC_3", Name = "_SHAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_STRIDER2", Name = "_Head_Strider2", Children = new()
                {
                    new() { GroupId = "_SH1ACC_", Descriptors = new()
                    {
                        new() { Id = "_SH1ACC_0", Name = "_SH1Acc_0" },
                        new() { Id = "_SH1ACC_4XRARE", Name = "_SH1Acc_4xRARE" },
                        new() { Id = "_SH1ACC_1", Name = "_SH1Acc_1" },
                        new() { Id = "_SH1ACC_2", Name = "_SH1Acc_2" },
                        new() { Id = "_SH1ACC_3", Name = "_SH1Acc_3" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_BUG", Name = "_Body_Bug", Children = new()
                {
                    new() { GroupId = "_BBACC_", Descriptors = new()
                    {
                        new() { Id = "_BBACC_1XRARE", Name = "_BBAcc_1xRARE" },
                        new() { Id = "_BBACC_4XRARE", Name = "_BBAcc_4xRARE" },
                        new() { Id = "_BBACC_3XRARE", Name = "_BBAcc_3xRARE" },
                    }
                    },
                    new() { GroupId = "_BBARM_", Descriptors = new()
                    {
                        new() { Id = "_BBARM_1", Name = "_BBArm_1" },
                        new() { Id = "_BBARM_NONE", Name = "_BBArm_none" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_STRIDER", Name = "_Body_Strider", Children = new()
                {
                    new() { GroupId = "_SBACC_", Descriptors = new()
                    {
                        new() { Id = "_SBACC_1", Name = "_SBAcc_1" },
                        new() { Id = "_SBACC_2XRARE", Name = "_SBAcc_2xRARE" },
                        new() { Id = "_SBACC_4XRARE", Name = "_SBAcc_4xRARE" },
                        new() { Id = "_SBACC_5XRARE", Name = "_SBAcc_5xRARE" },
                        new() { Id = "_SBACC_6", Name = "_SBAcc_6" },
                        new() { Id = "_SBACC_7XRARE", Name = "_SBAcc_7xRARE" },
                        new() { Id = "_SBACC_8XRARE", Name = "_SBAcc_8xRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_STRIDER2", Name = "_Body_Strider2", Children = new()
                {
                    new() { GroupId = "_ACC_", Descriptors = new()
                    {
                        new() { Id = "_ACC_ARM1", Name = "_Acc_Arm1" },
                        new() { Id = "_ACC_ARM2XRARE", Name = "_Acc_Arm2xRARE" },
                        new() { Id = "_ACC_NONE", Name = "_Acc_none" },
                    }
                    },
                    new() { GroupId = "_S1ACC_", Descriptors = new()
                    {
                        new() { Id = "_S1ACC_7XRARE", Name = "_S1Acc_7xRARE" },
                        new() { Id = "_S1ACC_2XRARE", Name = "_S1Acc_2xRARE" },
                        new() { Id = "_S1ACC_3XRARE", Name = "_S1Acc_3xRARE" },
                        new() { Id = "_S1ACC_4", Name = "_S1Acc_4" },
                        new() { Id = "_S1ACC_6XRARE", Name = "_S1Acc_6xRARE" },
                        new() { Id = "_S1ACC_5", Name = "_S1Acc_5" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_1", Name = "_Tail_1" },
                new() { Id = "_TAIL_2", Name = "_Tail_2" },
                new() { Id = "_TAIL_4", Name = "_Tail_4" },
                new() { Id = "_TAIL_5", Name = "_Tail_5" },
                new() { Id = "_TAIL_6", Name = "_Tail_6", Children = new()
                {
                    new() { GroupId = "_FINC_", Descriptors = new()
                    {
                        new() { Id = "_FINC_1", Name = "_FinC_1", Children = new()
                        {
                            new() { GroupId = "_FILLC_", Descriptors = new()
                            {
                                new() { Id = "_FILLC_1", Name = "_FillC_1" },
                                new() { Id = "_FILLC_NONE", Name = "_FillC_none" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_7XRARE", Name = "_Tail_7xRARE", Children = new()
                {
                    new() { GroupId = "_TAILS_", Descriptors = new()
                    {
                        new() { Id = "_TAILS_1", Name = "_Tails_1" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "COWSWIM", FriendlyName = "SWIMCOW", Details = new()
        {
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_ALIEN", Name = "_Tail_Alien", Children = new()
                {
                    new() { GroupId = "_TAACC_", Descriptors = new()
                    {
                        new() { Id = "_TAACC_0", Name = "_TAacc_0" },
                        new() { Id = "_TAACC_1", Name = "_TAacc_1" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_COW", Name = "_Tail_Cow" },
                new() { Id = "_TAIL_TURTLE", Name = "_Tail_Turtle" },
            }
            },
            new() { GroupId = "_COW_", Descriptors = new()
            {
                new() { Id = "_COW_SWIM", Name = "_Cow_Swim" },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_COW", Name = "_Body_Cow", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK1", Name = "_BCA_Blank1" },
                        new() { Id = "_BCA_COWHUMP", Name = "_BCA_CowHump", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_BLANKYO3", Name = "_Ext_BlankYo3" },
                                new() { Id = "_EXT_BACKSPINES", Name = "_Ext_BackSpinesxRARE" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_ROCKSXRARE", Name = "_BCA_RocksxRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE", Name = "_Ext_Mine" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BCA_BACKSPINES", Name = "_BCA_BackSpinesxRARE" },
                        new() { Id = "_BCA_STEGSPIKES", Name = "_BCA_StegSpikesxRARE" },
                        new() { Id = "_BCA_TURTSHELLX", Name = "_BCA_TurtShellxRARE" },
                        new() { Id = "_BCA_BACKFINXRA", Name = "_BCA_BackFinxRARE" },
                        new() { Id = "_BCA_LUMPXRARE", Name = "_BCA_LumpxRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_ROCK", Name = "_Body_Rock", Children = new()
                {
                    new() { GroupId = "_BCA_", Descriptors = new()
                    {
                        new() { Id = "_BCA_BLANK2", Name = "_BCA_Blank2" },
                    }
                    },
                    new() { GroupId = "_BRA_", Descriptors = new()
                    {
                        new() { Id = "_BRA_ROCKS1XRAR", Name = "_BRA_Rocks1xRARE", Children = new()
                        {
                            new() { GroupId = "_EXT_", Descriptors = new()
                            {
                                new() { Id = "_EXT_MINE2", Name = "_Ext_Mine2" },
                                new() { Id = "_EXT_BLANKYO2", Name = "_Ext_BlankYo2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BRA_ARMSPINESX", Name = "_BRA_ArmSpinesxRARE" },
                        new() { Id = "_BRA_BACKSPINES", Name = "_BRA_BackSpinesxRARE" },
                        new() { Id = "_BRA_STEGSPIKES", Name = "_BRA_StegSpikesxRARE" },
                        new() { Id = "_BRA_TURTSHELLX", Name = "_BRA_TurtShellxRARE" },
                        new() { Id = "_BRA_BACKFINXRA", Name = "_BRA_BackFinxRARE" },
                        new() { Id = "_BRA_LUMP1XRARE", Name = "_BRA_Lump1xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIENBIRD", Name = "_Head_AlienBird", Children = new()
                {
                    new() { GroupId = "_HABACC_", Descriptors = new()
                    {
                        new() { Id = "_HABACC_BLANK", Name = "_HABAcc_Blank" },
                        new() { Id = "_HABACC_1", Name = "_HABAcc_1" },
                        new() { Id = "_HABACC_2", Name = "_HABAcc_2" },
                        new() { Id = "_HABACC_3", Name = "_HABAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_ALIENTYPE", Name = "_Head_AlienType", Children = new()
                {
                    new() { GroupId = "_WEIRD_", Descriptors = new()
                    {
                        new() { Id = "_WEIRD_CONNECT", Name = "_Weird_Connect", Children = new()
                        {
                            new() { GroupId = "_SHAPEN_", Descriptors = new()
                            {
                                new() { Id = "_SHAPEN_1", Name = "_Shapen_1", Children = new()
                                {
                                    new() { GroupId = "_NOSE1_", Descriptors = new()
                                    {
                                        new() { Id = "_NOSE1_A", Name = "_Nose1_A" },
                                    }
                                    },
                                    new() { GroupId = "_EYES1_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES1_A", Name = "_Eyes1_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_FLARED", Name = "_Weird_Flared", Children = new()
                        {
                            new() { GroupId = "_FRUITF_", Descriptors = new()
                            {
                                new() { Id = "_FRUITF_NONE", Name = "_FruitF_None" },
                                new() { Id = "_FRUITF_A", Name = "_FruitF_A" },
                            }
                            },
                            new() { GroupId = "_FLARETOP_", Descriptors = new()
                            {
                                new() { Id = "_FLARETOP_4", Name = "_FlareTop_4" },
                                new() { Id = "_FLARETOP_2", Name = "_FlareTop_2", Children = new()
                                {
                                    new() { GroupId = "_EYES2_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES2_A", Name = "_Eyes2_A" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_FLARETOP_1", Name = "_FlareTop_1" },
                                new() { Id = "_FLARETOP_NONE", Name = "_FlareTop_None" },
                                new() { Id = "_FLARETOP_3", Name = "_FlareTop_3", Children = new()
                                {
                                    new() { GroupId = "_EYES3_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES3_A", Name = "_Eyes3_A" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_EYESF_", Descriptors = new()
                            {
                                new() { Id = "_EYESF_NONE", Name = "_EyesF_None" },
                                new() { Id = "_EYESF_A", Name = "_EyesF_A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_LONG", Name = "_Weird_Long", Children = new()
                        {
                            new() { GroupId = "_EARSL_", Descriptors = new()
                            {
                                new() { Id = "_EARSL_A", Name = "_EarsL_A" },
                                new() { Id = "_EARSL_NONE", Name = "_EarsL_None" },
                            }
                            },
                            new() { GroupId = "_EYESL_", Descriptors = new()
                            {
                                new() { Id = "_EYESL_A", Name = "_EyesL_A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_STUMP", Name = "_Weird_Stump", Children = new()
                        {
                            new() { GroupId = "_EYESS_", Descriptors = new()
                            {
                                new() { Id = "_EYESS_A", Name = "_EyesS_A" },
                            }
                            },
                            new() { GroupId = "_HORNSS_", Descriptors = new()
                            {
                                new() { Id = "_HORNSS_A", Name = "_HornsS_A" },
                                new() { Id = "_HORNSS_NONE", Name = "_HornsS_None" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL1", Name = "_Weird_HTail1", Children = new()
                        {
                            new() { GroupId = "_EYETAIL1_", Descriptors = new()
                            {
                                new() { Id = "_EYETAIL1_A", Name = "_EyeTail1_A" },
                                new() { Id = "_EYETAIL1_NONE", Name = "_EyeTail1_None" },
                            }
                            },
                        }
                        },
                        new() { Id = "_WEIRD_HTAIL2", Name = "_Weird_HTail2" },
                    }
                    },
                    new() { GroupId = "_DIVIDE_", Descriptors = new()
                    {
                        new() { Id = "_DIVIDE_1", Name = "_Divide_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUG", Name = "_Head_Bug", Children = new()
                {
                    new() { GroupId = "_HBUGACC_", Descriptors = new()
                    {
                        new() { Id = "_HBUGACC_BLANK", Name = "_HBugAcc_Blank" },
                        new() { Id = "_HBUGACC_1", Name = "_HBugAcc_1" },
                        new() { Id = "_HBUGACC_2", Name = "_HBugAcc_2" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COW", Name = "_Head_Cow", Children = new()
                {
                    new() { GroupId = "_HCE_", Descriptors = new()
                    {
                        new() { Id = "_HCE_BLANK", Name = "_HCE_Blank" },
                        new() { Id = "_HCE_COWEARS", Name = "_HCE_CowEars" },
                    }
                    },
                    new() { GroupId = "_HCH_", Descriptors = new()
                    {
                        new() { Id = "_HCH_BLANK", Name = "_HCH_Blank" },
                        new() { Id = "_HCH_COWHORN", Name = "_HCH_CowHorn" },
                        new() { Id = "_HCH_LORISHORN", Name = "_HCH_LorisHorn" },
                        new() { Id = "_HCH_ANTHORN", Name = "_HCH_AntHorn" },
                        new() { Id = "_HCH_NOSEBONE", Name = "_HCH_NoseBone" },
                        new() { Id = "_HCH_HEADBONE", Name = "_HCH_HeadBone" },
                        new() { Id = "_HCH_HEADPLATE", Name = "_HCH_HeadPlate" },
                        new() { Id = "_HCH_MULTIHORN", Name = "_HCH_MultiHorn" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_COWSKEW", Name = "_Head_CowSkew", Children = new()
                {
                    new() { GroupId = "_HCSE_", Descriptors = new()
                    {
                        new() { Id = "_HCSE_COWEARS", Name = "_HCSE_CowEars" },
                        new() { Id = "_HCSE_BLANK", Name = "_HCSE_Blank" },
                    }
                    },
                    new() { GroupId = "_HCSH_", Descriptors = new()
                    {
                        new() { Id = "_HCSH_HEADACC", Name = "_HCSH_HeadAcc" },
                        new() { Id = "_HCSH_HEADBONE", Name = "_HCSH_HeadBone" },
                        new() { Id = "_HCSH_ANTHORN", Name = "_HCSH_AntHorn" },
                        new() { Id = "_HCSH_LORISHORN", Name = "_HCSH_LorisHorn" },
                        new() { Id = "_HCSH_COWHORN", Name = "_HCSH_CowHorn" },
                        new() { Id = "_HCSH_BLANK", Name = "_HCSH_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LORIS", Name = "_Head_Loris", Children = new()
                {
                    new() { GroupId = "_HLH_", Descriptors = new()
                    {
                        new() { Id = "_HLH_MULTIHORN3", Name = "_HLH_MultiHorn3" },
                        new() { Id = "_HLH_NOSEBONE", Name = "_HLH_NoseBone" },
                        new() { Id = "_HLH_HEADPLATE", Name = "_HLH_HeadPlate" },
                        new() { Id = "_HLH_ANTHORN", Name = "_HLH_AntHorn" },
                        new() { Id = "_HLH_COWHORN", Name = "_HLH_CowHorn" },
                        new() { Id = "_HLH_LORISHORN", Name = "_HLH_LorisHorn" },
                        new() { Id = "_HLH_BLANK", Name = "_HLH_Blank" },
                    }
                    },
                    new() { GroupId = "_HLE_", Descriptors = new()
                    {
                        new() { Id = "_HLE_1", Name = "_HLE_1" },
                        new() { Id = "_HLE_BLANK", Name = "_HLE_Blank" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_REX", Name = "_Head_Rex", Children = new()
                {
                    new() { GroupId = "_HREXACC_", Descriptors = new()
                    {
                        new() { Id = "_HREXACC_1", Name = "_HRexAcc_1" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "RODENTSWIM", FriendlyName = "SWIMRODENT", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_BIRD", Name = "_Head_Bird", Children = new()
                {
                    new() { GroupId = "_HBACC_", Descriptors = new()
                    {
                        new() { Id = "_HBACC_0", Name = "_HBAcc_0" },
                        new() { Id = "_HBACC_1", Name = "_HBAcc_1" },
                    }
                    },
                    new() { GroupId = "_HBEARS_", Descriptors = new()
                    {
                        new() { Id = "_HBEARS_0", Name = "_HBEars_0" },
                        new() { Id = "_HBEARS_1", Name = "_HBEars_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_GDANE", Name = "_Head_GDane", Children = new()
                {
                    new() { GroupId = "_HGDEARS_", Descriptors = new()
                    {
                        new() { Id = "_HGDEARS_1", Name = "_HGDEars_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_LIZARD", Name = "_Head_Lizard", Children = new()
                {
                    new() { GroupId = "__HLACC_", Descriptors = new()
                    {
                        new() { Id = "__HLACC_0", Name = "__HLAcc_0" },
                    }
                    },
                    new() { GroupId = "_HLNECK_", Descriptors = new()
                    {
                        new() { Id = "_HLNECK_0", Name = "_HLNeck_0" },
                        new() { Id = "_HLNECK_1", Name = "_HLNeck_1" },
                    }
                    },
                    new() { GroupId = "_HLEARS_", Descriptors = new()
                    {
                        new() { Id = "_HLEARS_0", Name = "_HLEars_0" },
                        new() { Id = "_HLEARS_1", Name = "_HLEars_1" },
                        new() { Id = "_HLEARS_2", Name = "_HLEars_2" },
                    }
                    },
                    new() { GroupId = "_HLACC_", Descriptors = new()
                    {
                        new() { Id = "_HLACC_1", Name = "_HLAcc_1" },
                        new() { Id = "_HLACC_2", Name = "_HLAcc_2" },
                        new() { Id = "_HLACC_3", Name = "_HLAcc_3" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_RODENT", Name = "_Head_Rodent", Children = new()
                {
                    new() { GroupId = "_RHACC_", Descriptors = new()
                    {
                        new() { Id = "_RHACC_0", Name = "_RHAcc_0" },
                        new() { Id = "_RHACC_1", Name = "_RHAcc_1" },
                    }
                    },
                    new() { GroupId = "_HREARS_", Descriptors = new()
                    {
                        new() { Id = "_HREARS_0", Name = "_HREars_0" },
                        new() { Id = "_HREARS_1", Name = "_HREars_1" },
                        new() { Id = "_HREARS_2", Name = "_HREars_2" },
                        new() { Id = "_HREARS_3", Name = "_HREars_3" },
                        new() { Id = "_HREARS_4", Name = "_HREars_4" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_SLOTH", Name = "_Head_Sloth", Children = new()
                {
                    new() { GroupId = "_HSACCA_", Descriptors = new()
                    {
                        new() { Id = "_HSACCA_1", Name = "_HSAccA_1", Children = new()
                        {
                            new() { GroupId = "_HS1EAR_", Descriptors = new()
                            {
                                new() { Id = "_HS1EAR_0", Name = "_HS1Ear_0" },
                                new() { Id = "_HS1EAR_1", Name = "_HS1Ear_1" },
                            }
                            },
                            new() { GroupId = "_HSACC1_", Descriptors = new()
                            {
                                new() { Id = "_HSACC1_0", Name = "_HSAcc1_0" },
                                new() { Id = "_HSACC1_1", Name = "_HSAcc1_1" },
                                new() { Id = "_HSACC1_2", Name = "_HSAcc1_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HSACCA_2", Name = "_HSAccA_2", Children = new()
                        {
                            new() { GroupId = "_SH2EAR_", Descriptors = new()
                            {
                                new() { Id = "_SH2EAR_1", Name = "_SH2Ear_1" },
                                new() { Id = "_SH2EAR_2", Name = "_SH2Ear_2" },
                                new() { Id = "_SH2EAR_3", Name = "_SH2Ear_3" },
                                new() { Id = "_SH2EAR_4", Name = "_SH2Ear_4" },
                                new() { Id = "_SH2EAR_5", Name = "_SH2Ear_5" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TAPIR", Name = "_Head_Tapir", Children = new()
                {
                    new() { GroupId = "_TPHEARS_", Descriptors = new()
                    {
                        new() { Id = "_TPHEARS_0", Name = "_TPHEars_0" },
                        new() { Id = "_TPHEARS_1", Name = "_TPHEars_1" },
                    }
                    },
                    new() { GroupId = "_TPHACC_", Descriptors = new()
                    {
                        new() { Id = "_TPHACC_0", Name = "_TPHAcc_0" },
                        new() { Id = "_TPHACC_1", Name = "_TPHAcc_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TAZ", Name = "_Head_Taz", Children = new()
                {
                    new() { GroupId = "_HTAZEARS_", Descriptors = new()
                    {
                        new() { Id = "_HTAZEARS_0", Name = "_HTazEars_0" },
                        new() { Id = "_HTAZEARS_1", Name = "_HTazEars_1" },
                    }
                    },
                    new() { GroupId = "_HTAZACC_", Descriptors = new()
                    {
                        new() { Id = "_HTAZACC_0", Name = "_HTazAcc_0" },
                        new() { Id = "_HTAZACC_1", Name = "_HTazAcc_1" },
                        new() { Id = "_HTAZACC_2", Name = "_HTazAcc_2" },
                        new() { Id = "_HTAZACC_3", Name = "_HTazAcc_3" },
                        new() { Id = "_HTAZACC_4", Name = "_HTazAcc_4" },
                        new() { Id = "_HTAZACC_5", Name = "_HTazAcc_5" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TURTLE", Name = "_Head_Turtle", Children = new()
                {
                    new() { GroupId = "_THACC_", Descriptors = new()
                    {
                        new() { Id = "_THACC_1", Name = "_THAcc_1" },
                        new() { Id = "_THACC_2", Name = "_THAcc_2" },
                        new() { Id = "_THACC_3", Name = "_THAcc_3" },
                        new() { Id = "_THACC_4", Name = "_THAcc_4" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_LIZARD", Name = "_Body_Lizard", Children = new()
                {
                    new() { GroupId = "_LIZBOD_", Descriptors = new()
                    {
                        new() { Id = "_LIZBOD_0", Name = "_LizBod_0" },
                        new() { Id = "_LIZBOD_1", Name = "_LizBod_1" },
                        new() { Id = "_LIZBOD_2", Name = "_LizBod_2" },
                        new() { Id = "_LIZBOD_3", Name = "_LizBod_3" },
                        new() { Id = "_LIZBOD_4", Name = "_LizBod_4" },
                        new() { Id = "_LIZBOD_5", Name = "_LizBod_5" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_RODENT", Name = "_Body_Rodent", Children = new()
                {
                    new() { GroupId = "_TAZBACK_", Descriptors = new()
                    {
                        new() { Id = "_TAZBACK_0", Name = "_TazBack_0" },
                        new() { Id = "_TAZBACK_1", Name = "_TazBack_1" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_SLOTH", Name = "_Body_Sloth", Children = new()
                {
                    new() { GroupId = "_SBACK_", Descriptors = new()
                    {
                        new() { Id = "_SBACK_0", Name = "_SBack_0" },
                        new() { Id = "_SBACK_1", Name = "_SBack_1" },
                        new() { Id = "_SBACK_2", Name = "_SBack_2" },
                        new() { Id = "_SBACK_3", Name = "_SBack_3" },
                        new() { Id = "_SBACK_4", Name = "_SBack_4" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_BIRD", Name = "_Tail_Bird" },
                new() { Id = "_TAIL_LIZARD", Name = "_Tail_Lizard", Children = new()
                {
                    new() { GroupId = "_TAILACC_", Descriptors = new()
                    {
                        new() { Id = "_TAILACC_0", Name = "_TailAcc_0" },
                        new() { Id = "_TAILACC_1", Name = "_TailAcc_1" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_MONKEY", Name = "_Tail_Monkey" },
                new() { Id = "_TAIL_RODENT", Name = "_Tail_Rodent" },
                new() { Id = "_TAIL_SLOTH", Name = "_Tail_Sloth" },
            }
            },
        }
        },
        new() { CreatureId = "TREX", FriendlyName = "TREX", Details = new()
        {
            new() { GroupId = "_TREX_", Descriptors = new()
            {
                new() { Id = "_TREX_3XRARE", Name = "_TRex_3xRARE", Children = new()
                {
                    new() { GroupId = "_TAILB_", Descriptors = new()
                    {
                        new() { Id = "_TAILB_ALIEN1", Name = "_TailB_Alien1", Children = new()
                        {
                            new() { GroupId = "_VERS_", Descriptors = new()
                            {
                                new() { Id = "_VERS_1", Name = "_Vers_1" },
                                new() { Id = "_VERS_2", Name = "_Vers_2" },
                                new() { Id = "_VERS_3A", Name = "_Vers_3A" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_TOPB_", Descriptors = new()
                    {
                        new() { Id = "_TOPB_1", Name = "_TopB_1", Children = new()
                        {
                            new() { GroupId = "_HEADB_", Descriptors = new()
                            {
                                new() { Id = "_HEADB_1", Name = "_HeadB_1", Children = new()
                                {
                                    new() { GroupId = "_JELLYFINGERS_", Descriptors = new()
                                    {
                                        new() { Id = "_JELLYFINGERS_3", Name = "_JellyFingers_3" },
                                    }
                                    },
                                    new() { GroupId = "_MOUTHE_", Descriptors = new()
                                    {
                                        new() { Id = "_MOUTHE_1", Name = "_MouthE_1" },
                                    }
                                    },
                                    new() { GroupId = "_EYESE_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESE_1", Name = "_EyesE_1" },
                                    }
                                    },
                                    new() { GroupId = "_ACCH_", Descriptors = new()
                                    {
                                        new() { Id = "_ACCH_1", Name = "_AccH_1", Children = new()
                                        {
                                            new() { GroupId = "_EBALLSD_", Descriptors = new()
                                            {
                                                new() { Id = "_EBALLSD_1", Name = "_EballsD_1" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_BACKPACK_", Descriptors = new()
                            {
                                new() { Id = "_BACKPACK_1", Name = "_BackPack_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_TREX_4", Name = "_TRex_4", Children = new()
                {
                    new() { GroupId = "_HEAD_", Descriptors = new()
                    {
                        new() { Id = "_HEAD_BIRDREX", Name = "_Head_BirdRex", Children = new()
                        {
                            new() { GroupId = "_BRHACC_", Descriptors = new()
                            {
                                new() { Id = "_BRHACC_9A", Name = "_BRHAcc_9A" },
                                new() { Id = "_BRHACC_8", Name = "_BRHAcc_8" },
                                new() { Id = "_BRHACC_7A", Name = "_BRHAcc_7A" },
                                new() { Id = "_BRHACC_6", Name = "_BRHAcc_6" },
                                new() { Id = "_BRHACC_4", Name = "_BRHAcc_4" },
                                new() { Id = "_BRHACC_3", Name = "_BRHAcc_3" },
                                new() { Id = "_BRHACC_2", Name = "_BRHAcc_2" },
                                new() { Id = "_BRHACC_1", Name = "_BRHAcc_1" },
                                new() { Id = "_BRHACC_NULL", Name = "_BRHAcc_NULL" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_LIZ", Name = "_Head_Liz", Children = new()
                        {
                            new() { GroupId = "_LHACC_", Descriptors = new()
                            {
                                new() { Id = "_LHACC_0", Name = "_LHAcc_0" },
                                new() { Id = "_LHACC_1", Name = "_LHAcc_1" },
                                new() { Id = "_LHACC_2", Name = "_LHAcc_2" },
                                new() { Id = "_LHACC_3", Name = "_LHAcc_3" },
                                new() { Id = "_LHACC_4", Name = "_LHAcc_4" },
                                new() { Id = "_LHACC_5A", Name = "_LHAcc_5A" },
                                new() { Id = "_LHACC_6", Name = "_LHAcc_6" },
                                new() { Id = "_LHACC_7A", Name = "_LHAcc_7A" },
                                new() { Id = "_LHACC_8", Name = "_LHAcc_8" },
                                new() { Id = "_LHACC_9", Name = "_LHAcc_9" },
                                new() { Id = "_LHACC_10A", Name = "_LHAcc_10A" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_RHINO", Name = "_Head_Rhino", Children = new()
                        {
                            new() { GroupId = "_RHIHACC_", Descriptors = new()
                            {
                                new() { Id = "_RHIHACC_12", Name = "_RhiHAcc_12", Children = new()
                                {
                                    new() { GroupId = "_RHIHACC_", Descriptors = new()
                                    {
                                        new() { Id = "_RHIHACC_10", Name = "_RhiHAcc_10" },
                                        new() { Id = "_RHIHACC_11", Name = "_RhiHAcc_11" },
                                    }
                                    },
                                    new() { GroupId = "_RHINOHEARS_", Descriptors = new()
                                    {
                                        new() { Id = "_RHINOHEARS_1", Name = "_RhinoHEars_1" },
                                        new() { Id = "_RHINOHEARS_2", Name = "_RhinoHEars_2" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_RHIHACC_5", Name = "_RhiHAcc_5" },
                                new() { Id = "_RHIHACC_8", Name = "_RhiHAcc_8" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_CROC", Name = "_Head_Croc", Children = new()
                        {
                            new() { GroupId = "_CHACC_", Descriptors = new()
                            {
                                new() { Id = "_CHACC_3A1", Name = "_CHAcc_3A1" },
                                new() { Id = "_CHACC_4", Name = "_CHAcc_4" },
                                new() { Id = "_CHACC_2", Name = "_CHAcc_2" },
                                new() { Id = "_CHACC_1", Name = "_CHAcc_1" },
                                new() { Id = "_CHACC_0", Name = "_CHAcc_0" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_TREX", Name = "_Head_TRex", Children = new()
                        {
                            new() { GroupId = "_TRTEETH_", Descriptors = new()
                            {
                                new() { Id = "_TRTEETH_1", Name = "_TRTeeth_1" },
                                new() { Id = "_TRTEETH_2", Name = "_TRTeeth_2" },
                                new() { Id = "_TRTEETH_3", Name = "_TRTeeth_3" },
                            }
                            },
                            new() { GroupId = "_REXSET_", Descriptors = new()
                            {
                                new() { Id = "_REXSET_1", Name = "_RexSet_1", Children = new()
                                {
                                    new() { GroupId = "_REXS1_", Descriptors = new()
                                    {
                                        new() { Id = "_REXS1_3", Name = "_RexS1_3" },
                                        new() { Id = "_REXS1_4", Name = "_RexS1_4" },
                                    }
                                    },
                                    new() { GroupId = "_REXS1J_", Descriptors = new()
                                    {
                                        new() { Id = "_REXS1J_2", Name = "_RexS1J_2" },
                                        new() { Id = "_REXS1J_NULL1", Name = "_RexS1J_NULL1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_REXSET_2", Name = "_RexSet_2", Children = new()
                                {
                                    new() { GroupId = "_REXHEAD_", Descriptors = new()
                                    {
                                        new() { Id = "_REXHEAD_7", Name = "_RexHead_7" },
                                        new() { Id = "_REXHEAD_6", Name = "_RexHead_6" },
                                        new() { Id = "_REXHEAD_1", Name = "_RexHead_1" },
                                        new() { Id = "_REXHEAD_9", Name = "_RexHead_9" },
                                    }
                                    },
                                    new() { GroupId = "_REXHEADJ_", Descriptors = new()
                                    {
                                        new() { Id = "_REXHEADJ_NULL", Name = "_RexHeadJ_NULL" },
                                        new() { Id = "_REXHEADJ_1", Name = "_RexHeadJ_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_REXSET_3", Name = "_RexSet_3" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_RAT", Name = "_Head_Rat", Children = new()
                        {
                            new() { GroupId = "_RHACC_", Descriptors = new()
                            {
                                new() { Id = "_RHACC_0", Name = "_RHAcc_0" },
                                new() { Id = "_RHACC_3A", Name = "_RHAcc_3A" },
                                new() { Id = "_RHACC_5A", Name = "_RHAcc_5A" },
                                new() { Id = "_RHACC_6A", Name = "_RHAcc_6A" },
                                new() { Id = "_RHACC_7A", Name = "_RHAcc_7A" },
                            }
                            },
                            new() { GroupId = "_REAR_", Descriptors = new()
                            {
                                new() { Id = "_REAR_0XRARE", Name = "_Rear_0xRARE" },
                                new() { Id = "_REAR_1B", Name = "_Rear_1B" },
                                new() { Id = "_REAR_2A", Name = "_Rear_2A" },
                                new() { Id = "_REAR_3A", Name = "_Rear_3A" },
                                new() { Id = "_REAR_4", Name = "_Rear_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_TOUCANA", Name = "_Head_ToucanA", Children = new()
                        {
                            new() { GroupId = "_THACC_", Descriptors = new()
                            {
                                new() { Id = "_THACC_6B", Name = "_THAcc_6B" },
                                new() { Id = "_THACC_1", Name = "_THAcc_1" },
                                new() { Id = "_THACC_4", Name = "_THAcc_4" },
                                new() { Id = "_THACC_5", Name = "_THAcc_5" },
                                new() { Id = "_THACC_3A", Name = "_THAcc_3A" },
                                new() { Id = "_THACC_2A", Name = "_THAcc_2A" },
                                new() { Id = "_THACC_0", Name = "_THAcc_0" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HEAD_ALIEN", Name = "_Head_Alien", Children = new()
                        {
                            new() { GroupId = "_BLOB_", Descriptors = new()
                            {
                                new() { Id = "_BLOB_2B", Name = "_Blob_2B", Children = new()
                                {
                                    new() { GroupId = "_EYES_", Descriptors = new()
                                    {
                                        new() { Id = "_EYES_2", Name = "_Eyes_2", Children = new()
                                        {
                                            new() { GroupId = "_ORBS_", Descriptors = new()
                                            {
                                                new() { Id = "_ORBS_1", Name = "_Orbs_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_EYES_1", Name = "_Eyes_1" },
                                        new() { Id = "_EYES_5", Name = "_Eyes_5", Children = new()
                                        {
                                            new() { GroupId = "_ORBSC_", Descriptors = new()
                                            {
                                                new() { Id = "_ORBSC_1", Name = "_OrbsC_1" },
                                            }
                                            },
                                            new() { GroupId = "_MOUTHW_", Descriptors = new()
                                            {
                                                new() { Id = "_MOUTHW_2", Name = "_MouthW_2" },
                                                new() { Id = "_MOUTHW_1", Name = "_MouthW_1" },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_EYES_3", Name = "_Eyes_3", Children = new()
                                        {
                                            new() { GroupId = "_JELLYFINGERS_", Descriptors = new()
                                            {
                                                new() { Id = "_JELLYFINGERS_2", Name = "_JellyFingers_2" },
                                                new() { Id = "_JELLYFINGERS_1", Name = "_JellyFingers_1", Children = new()
                                                {
                                                    new() { GroupId = "_MOUTHF_", Descriptors = new()
                                                    {
                                                        new() { Id = "_MOUTHF_1", Name = "_MouthF_1" },
                                                        new() { Id = "_MOUTHF_2", Name = "_MouthF_2" },
                                                    }
                                                    },
                                                }
                                                },
                                            }
                                            },
                                        }
                                        },
                                        new() { Id = "_EYES_4", Name = "_Eyes_4", Children = new()
                                        {
                                            new() { GroupId = "_ORBSB_", Descriptors = new()
                                            {
                                                new() { Id = "_ORBSB_2", Name = "_OrbsB_2" },
                                            }
                                            },
                                        }
                                        },
                                    }
                                    },
                                    new() { GroupId = "_ANTENNAS_", Descriptors = new()
                                    {
                                        new() { Id = "_ANTENNAS_1", Name = "_Antennas_1" },
                                        new() { Id = "_ANTENNAS_2", Name = "_Antennas_2" },
                                        new() { Id = "_ANTENNAS_3", Name = "_Antennas_3" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BLOB_1C1", Name = "_Blob_1C1", Children = new()
                                {
                                    new() { GroupId = "_EYESB_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESB_2", Name = "_EyesB_2" },
                                        new() { Id = "_EYESB_1", Name = "_EyesB_1" },
                                    }
                                    },
                                    new() { GroupId = "_MOUTH_", Descriptors = new()
                                    {
                                        new() { Id = "_MOUTH_1", Name = "_Mouth_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_BODY_", Descriptors = new()
                    {
                        new() { Id = "_BODY_BIRDREX", Name = "_Body_BirdRex", Children = new()
                        {
                            new() { GroupId = "_BBRACC_", Descriptors = new()
                            {
                                new() { Id = "_BBRACC_1N", Name = "_BBRAcc_1N" },
                                new() { Id = "_BBRACC_2N", Name = "_BBRAcc_2N" },
                                new() { Id = "_BBRACC_3N", Name = "_BBRAcc_3N" },
                                new() { Id = "_BBRACC_4N", Name = "_BBRAcc_4N" },
                                new() { Id = "_BBRACC_5N", Name = "_BBRAcc_5N" },
                                new() { Id = "_BBRACC_6N", Name = "_BBRAcc_6N" },
                                new() { Id = "_BBRACC_12OK", Name = "_BBRAcc_12OK" },
                                new() { Id = "_BBRACC_11OK", Name = "_BBRAcc_11OK" },
                                new() { Id = "_BBRACC_24OK", Name = "_BBRAcc_24OK" },
                                new() { Id = "_BBRACC_0", Name = "_BBRAcc_0" },
                                new() { Id = "_BBRACC_2", Name = "_BBRAcc_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_RAT", Name = "_Body_Rat", Children = new()
                        {
                            new() { GroupId = "_RATBACK_", Descriptors = new()
                            {
                                new() { Id = "_RATBACK_25", Name = "_RatBack_25" },
                                new() { Id = "_RATBACK_2XRARE", Name = "_RatBack_2xRARE" },
                                new() { Id = "_RATBACK_4XRARE", Name = "_RatBack_4xRARE" },
                                new() { Id = "_RATBACK_3A", Name = "_RatBack_3A" },
                                new() { Id = "_RATBACK_1N", Name = "_RatBack_1N" },
                                new() { Id = "_RATBACK_2N", Name = "_RatBack_2N" },
                                new() { Id = "_RATBACK_3N", Name = "_RatBack_3N" },
                                new() { Id = "_RATBACK_4N", Name = "_RatBack_4N" },
                                new() { Id = "_RATBACK_5N", Name = "_RatBack_5N" },
                                new() { Id = "_RATBACK_6N", Name = "_RatBack_6N" },
                                new() { Id = "_RATBACK_12OK", Name = "_RatBack_12OK" },
                                new() { Id = "_RATBACK_11OK", Name = "_RatBack_11OK" },
                                new() { Id = "_RATBACK_24OK", Name = "_RatBack_24OK" },
                                new() { Id = "_RATBACK_2", Name = "_RatBack_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_TREX", Name = "_Body_TRex", Children = new()
                        {
                            new() { GroupId = "_REXBACK_", Descriptors = new()
                            {
                                new() { Id = "_REXBACK_0", Name = "_RexBack_0" },
                                new() { Id = "_REXBACK_2XRARE", Name = "_RexBack_2xRARE" },
                                new() { Id = "_REXBACK_4XRARE", Name = "_RexBack_4xRARE" },
                                new() { Id = "_REXBACK_3A", Name = "_RexBack_3A" },
                                new() { Id = "_REXBACK_1N", Name = "_RexBack_1N" },
                                new() { Id = "_REXBACK_2N", Name = "_RexBack_2N" },
                                new() { Id = "_REXBACK_3N", Name = "_RexBack_3N" },
                                new() { Id = "_REXBACK_4N", Name = "_RexBack_4N" },
                                new() { Id = "_REXBACK_5N", Name = "_RexBack_5N" },
                                new() { Id = "_REXBACK_6N", Name = "_RexBack_6N" },
                                new() { Id = "_REXBACK_12OK", Name = "_RexBack_12OK" },
                                new() { Id = "_REXBACK_11OK", Name = "_RexBack_11OK" },
                                new() { Id = "_REXBACK_24OK", Name = "_RexBack_24OK" },
                                new() { Id = "_REXBACK_2", Name = "_RexBack_2" },
                            }
                            },
                        }
                        },
                        new() { Id = "_BODY_HOLESXRARE", Name = "_Body_HolesxRARE", Children = new()
                        {
                            new() { GroupId = "_BACKACC_", Descriptors = new()
                            {
                                new() { Id = "_BACKACC_4", Name = "_BackAcc_4" },
                                new() { Id = "_BACKACC_3", Name = "_BackAcc_3" },
                                new() { Id = "_BACKACC_1A", Name = "_BackAcc_1A" },
                                new() { Id = "_BACKACC_2A", Name = "_BackAcc_2A" },
                            }
                            },
                            new() { GroupId = "_LIMBS_", Descriptors = new()
                            {
                                new() { Id = "_LIMBS_1A", Name = "_Limbs_1A" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_TAIL_", Descriptors = new()
                    {
                        new() { Id = "_TAIL_TREX", Name = "_Tail_TRex", Children = new()
                        {
                            new() { GroupId = "_REXTAILACC_", Descriptors = new()
                            {
                                new() { Id = "_REXTAILACC_1XRARE", Name = "_RexTailAcc_1xRARE" },
                                new() { Id = "_REXTAILACC_2XRARE", Name = "_RexTailAcc_2xRARE" },
                                new() { Id = "_REXTAILACC_0", Name = "_RexTailAcc_0" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_BIRDREX", Name = "_Tail_BirdRex", Children = new()
                        {
                            new() { GroupId = "_TBRACC_", Descriptors = new()
                            {
                                new() { Id = "_TBRACC_0", Name = "_TBRAcc_0" },
                                new() { Id = "_TBRACC_1", Name = "_TBRAcc_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_RAT", Name = "_Tail_Rat" },
                        new() { Id = "_TAIL_TOUCAN", Name = "_Tail_Toucan", Children = new()
                        {
                            new() { GroupId = "_TTACC_", Descriptors = new()
                            {
                                new() { Id = "_TTACC_0", Name = "_TTAcc_0" },
                                new() { Id = "_TTACC_1", Name = "_TTAcc_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_TAIL_ALIENXRARE", Name = "_Tail_AlienxRARE", Children = new()
                        {
                            new() { GroupId = "_VERSION_", Descriptors = new()
                            {
                                new() { Id = "_VERSION_1", Name = "_Version_1" },
                                new() { Id = "_VERSION_2", Name = "_Version_2" },
                                new() { Id = "_VERSION_3A", Name = "_Version_3A" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "TRICERATOPS", FriendlyName = "TRICERATOPS", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_ALIEN", Name = "_Head_Alien", Children = new()
                {
                    new() { GroupId = "_ALHEAD_", Descriptors = new()
                    {
                        new() { Id = "_ALHEAD_2", Name = "_AlHead_2", Children = new()
                        {
                            new() { GroupId = "_BROWS_", Descriptors = new()
                            {
                                new() { Id = "_BROWS_1", Name = "_Brows_1", Children = new()
                                {
                                    new() { GroupId = "_EBALLZ_", Descriptors = new()
                                    {
                                        new() { Id = "_EBALLZ_1", Name = "_Eballz_1" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_BROWS_NONE", Name = "_Brows_none" },
                            }
                            },
                            new() { GroupId = "_RIM_", Descriptors = new()
                            {
                                new() { Id = "_RIM_1", Name = "_Rim_1", Children = new()
                                {
                                    new() { GroupId = "_CENTER_", Descriptors = new()
                                    {
                                        new() { Id = "_CENTER_2", Name = "_center_2" },
                                        new() { Id = "_CENTER_1", Name = "_center_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_INMOUTHA_", Descriptors = new()
                            {
                                new() { Id = "_INMOUTHA_1", Name = "_InMouthA_1" },
                            }
                            },
                            new() { GroupId = "_SIDE_", Descriptors = new()
                            {
                                new() { Id = "_SIDE_1", Name = "_Side_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ALHEAD_3", Name = "_AlHead_3", Children = new()
                        {
                            new() { GroupId = "_JAWX_", Descriptors = new()
                            {
                                new() { Id = "_JAWX_1", Name = "_JawX_1" },
                            }
                            },
                            new() { GroupId = "_EYESR_", Descriptors = new()
                            {
                                new() { Id = "_EYESR_NONE", Name = "_eyesR_none" },
                                new() { Id = "_EYESR_1", Name = "_eyesR_1", Children = new()
                                {
                                    new() { GroupId = "_EBALLSR_", Descriptors = new()
                                    {
                                        new() { Id = "_EBALLSR_1", Name = "_eballsR_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_TOPGUM_", Descriptors = new()
                            {
                                new() { Id = "_TOPGUM_1", Name = "_TopGum_1", Children = new()
                                {
                                    new() { GroupId = "_TEETHX_", Descriptors = new()
                                    {
                                        new() { Id = "_TEETHX_1", Name = "_TeethX_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_ANTENNASS_", Descriptors = new()
                            {
                                new() { Id = "_ANTENNASS_NONE", Name = "_Antennass_none" },
                                new() { Id = "_ANTENNASS_2", Name = "_Antennass_2" },
                            }
                            },
                            new() { GroupId = "_TOP_", Descriptors = new()
                            {
                                new() { Id = "_TOP_1", Name = "_Top_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_ALHEAD_1", Name = "_AlHead_1", Children = new()
                        {
                            new() { GroupId = "_MOUTH_", Descriptors = new()
                            {
                                new() { Id = "_MOUTH_B", Name = "_Mouth_b" },
                                new() { Id = "_MOUTH_A", Name = "_Mouth_a" },
                            }
                            },
                            new() { GroupId = "_NOSE_", Descriptors = new()
                            {
                                new() { Id = "_NOSE_NONE", Name = "_Nose_none" },
                                new() { Id = "_NOSE_1", Name = "_Nose_1" },
                                new() { Id = "_NOSE_2", Name = "_Nose_2" },
                            }
                            },
                            new() { GroupId = "_ANTENNAS_", Descriptors = new()
                            {
                                new() { Id = "_ANTENNAS_NONE", Name = "_Antennas_none" },
                                new() { Id = "_ANTENNAS_1", Name = "_Antennas_1" },
                            }
                            },
                            new() { GroupId = "_EYEST_", Descriptors = new()
                            {
                                new() { Id = "_EYEST_1", Name = "_eyesT_1" },
                                new() { Id = "_EYEST_2", Name = "_eyesT_2", Children = new()
                                {
                                    new() { GroupId = "_EBALLS_", Descriptors = new()
                                    {
                                        new() { Id = "_EBALLS_1", Name = "_eballs_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_DIPLO", Name = "_Head_Diplo", Children = new()
                {
                    new() { GroupId = "_NECK_", Descriptors = new()
                    {
                        new() { Id = "_NECK_DIPLO", Name = "_Neck_Diplo", Children = new()
                        {
                            new() { GroupId = "_NECK1_", Descriptors = new()
                            {
                                new() { Id = "_NECK1_1", Name = "_Neck1_1" },
                            }
                            },
                            new() { GroupId = "_BEAR_", Descriptors = new()
                            {
                                new() { Id = "_BEAR_NONE", Name = "_BEar_none" },
                                new() { Id = "_BEAR_2", Name = "_BEar_2" },
                                new() { Id = "_BEAR_1", Name = "_BEar_1" },
                            }
                            },
                            new() { GroupId = "_HDACC_", Descriptors = new()
                            {
                                new() { Id = "_HDACC_NULL", Name = "_HDAcc_NULL" },
                                new() { Id = "_HDACC_3", Name = "_HDAcc_3" },
                                new() { Id = "_HDACC_6", Name = "_HDAcc_6" },
                                new() { Id = "_HDACC_5", Name = "_HDAcc_5" },
                                new() { Id = "_HDACC_4", Name = "_HDAcc_4" },
                                new() { Id = "_HDACC_1", Name = "_HDAcc_1" },
                                new() { Id = "_HDACC_9", Name = "_HDAcc_9", Children = new()
                                {
                                    new() { GroupId = "_HDA_", Descriptors = new()
                                    {
                                        new() { Id = "_HDA_1", Name = "_HDA_1" },
                                        new() { Id = "_HDA_2", Name = "_HDA_2" },
                                        new() { Id = "_HDA_3", Name = "_HDA_3" },
                                        new() { Id = "_HDA_NONE", Name = "_HDA_none" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_HAC_", Descriptors = new()
                            {
                                new() { Id = "_HAC_NONE", Name = "_HAC_none" },
                                new() { Id = "_HAC_1", Name = "_HAC_1" },
                            }
                            },
                        }
                        },
                        new() { Id = "_NECK_ALIENDIPLO", Name = "_Neck_AlienDiplo", Children = new()
                        {
                            new() { GroupId = "_ADIPACC_", Descriptors = new()
                            {
                                new() { Id = "_ADIPACC_NULL", Name = "_ADipAcc_Null" },
                                new() { Id = "_ADIPACC_4", Name = "_ADipAcc_4" },
                                new() { Id = "_ADIPACC_1", Name = "_ADipAcc_1" },
                                new() { Id = "_ADIPACC_6", Name = "_ADipAcc_6", Children = new()
                                {
                                    new() { GroupId = "_CEAR_", Descriptors = new()
                                    {
                                        new() { Id = "_CEAR_1", Name = "_CEar_1" },
                                        new() { Id = "_CEAR_NONE", Name = "_CEar_none" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_ADIPACC_2", Name = "_ADipAcc_2", Children = new()
                                {
                                    new() { GroupId = "_DEAR_", Descriptors = new()
                                    {
                                        new() { Id = "_DEAR_1", Name = "_DEar_1" },
                                        new() { Id = "_DEAR_NONE", Name = "_DEar_none" },
                                    }
                                    },
                                }
                                },
                                new() { Id = "_ADIPACC_5", Name = "_ADipAcc_5", Children = new()
                                {
                                    new() { GroupId = "_EEAR_", Descriptors = new()
                                    {
                                        new() { Id = "_EEAR_NONE", Name = "_EEar_none" },
                                        new() { Id = "_EEAR_1", Name = "_EEar_1" },
                                    }
                                    },
                                    new() { GroupId = "_BNEHRN_", Descriptors = new()
                                    {
                                        new() { Id = "_BNEHRN_NONE", Name = "_BneHrn_none" },
                                        new() { Id = "_BNEHRN_1", Name = "_BneHrn_1" },
                                    }
                                    },
                                    new() { GroupId = "_EYESF_", Descriptors = new()
                                    {
                                        new() { Id = "_EYESF_1", Name = "_EyesF_1" },
                                    }
                                    },
                                }
                                },
                            }
                            },
                            new() { GroupId = "_BDIPACC_", Descriptors = new()
                            {
                                new() { Id = "_BDIPACC_NONE", Name = "_BDipAcc_none" },
                                new() { Id = "_BDIPACC_1", Name = "_BDipAcc_1" },
                            }
                            },
                            new() { GroupId = "_CDIPACC_", Descriptors = new()
                            {
                                new() { Id = "_CDIPACC_2", Name = "_CDipAcc_2" },
                                new() { Id = "_CDIPACC_1", Name = "_CDipAcc_1" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_HIPPO", Name = "_Head_Hippo", Children = new()
                {
                    new() { GroupId = "_HIPEARS_", Descriptors = new()
                    {
                        new() { Id = "_HIPEARS_4", Name = "_HipEars_4" },
                        new() { Id = "_HIPEARS_3", Name = "_HipEars_3" },
                        new() { Id = "_HIPEARS_2", Name = "_HipEars_2" },
                        new() { Id = "_HIPEARS_1", Name = "_HipEars_1" },
                    }
                    },
                    new() { GroupId = "_HHACC_", Descriptors = new()
                    {
                        new() { Id = "_HHACC_0", Name = "_HHAcc_0" },
                    }
                    },
                    new() { GroupId = "_HHEAD_", Descriptors = new()
                    {
                        new() { Id = "_HHEAD_1", Name = "_HHead_1", Children = new()
                        {
                            new() { GroupId = "_HHACC_", Descriptors = new()
                            {
                                new() { Id = "_HHACC_2", Name = "_HHAcc_2" },
                                new() { Id = "_HHACC_1", Name = "_HHAcc_1" },
                                new() { Id = "_HHACC_4XRARE", Name = "_HHAcc_4xRARE" },
                            }
                            },
                        }
                        },
                        new() { Id = "_HHEAD_2", Name = "_HHead_2", Children = new()
                        {
                            new() { GroupId = "_HHHORNS_", Descriptors = new()
                            {
                                new() { Id = "_HHHORNS_5XRARE", Name = "_HHHorns_5xRARE" },
                                new() { Id = "_HHHORNS_1", Name = "_HHHorns_1" },
                                new() { Id = "_HHHORNS_2XRARE", Name = "_HHHorns_2xRARE" },
                                new() { Id = "_HHHORNS_3", Name = "_HHHorns_3" },
                                new() { Id = "_HHHORNS_4", Name = "_HHHorns_4" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_RHINO", Name = "_Head_Rhino", Children = new()
                {
                    new() { GroupId = "_RHIHORNS_", Descriptors = new()
                    {
                        new() { Id = "_RHIHORNS_0", Name = "_RhiHorns_0" },
                        new() { Id = "_RHIHORNS_1", Name = "_RhiHorns_1" },
                    }
                    },
                    new() { GroupId = "_RHIHEARS_", Descriptors = new()
                    {
                        new() { Id = "_RHIHEARS_0XRARE", Name = "_RhiHEars_0xRARE" },
                        new() { Id = "_RHIHEARS_2", Name = "_RhiHEars_2" },
                        new() { Id = "_RHIHEARS_1", Name = "_RhiHEars_1" },
                    }
                    },
                    new() { GroupId = "_RHIHACC_", Descriptors = new()
                    {
                        new() { Id = "_RHIHACC_0", Name = "_RhiHAcc_0" },
                        new() { Id = "_RHIHACC_3", Name = "_RhiHAcc_3" },
                        new() { Id = "_RHIHACC_4", Name = "_RhiHAcc_4" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_STEG", Name = "_Head_Steg", Children = new()
                {
                    new() { GroupId = "_SHEARS_", Descriptors = new()
                    {
                        new() { Id = "_SHEARS_1", Name = "_SHEars_1" },
                        new() { Id = "_SHEARS_0", Name = "_SHEars_0" },
                    }
                    },
                    new() { GroupId = "_SHNECK_", Descriptors = new()
                    {
                        new() { Id = "_SHNECK_1XRARE", Name = "_SHNeck_1xRARE" },
                        new() { Id = "_SHNECK_0", Name = "_SHNeck_0" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TAPIR", Name = "_Head_Tapir", Children = new()
                {
                    new() { GroupId = "_THACC_", Descriptors = new()
                    {
                        new() { Id = "_THACC_0", Name = "_THAcc_0" },
                        new() { Id = "_THACC_5", Name = "_THAcc_5" },
                        new() { Id = "_THACC_3", Name = "_THAcc_3" },
                        new() { Id = "_THACC_6", Name = "_THAcc_6" },
                        new() { Id = "_THACC_2", Name = "_THAcc_2" },
                        new() { Id = "_THACC_4", Name = "_THAcc_4" },
                    }
                    },
                    new() { GroupId = "_THEAR_", Descriptors = new()
                    {
                        new() { Id = "_THEAR_0", Name = "_THEar_0" },
                        new() { Id = "_THEAR_1", Name = "_THEar_1" },
                        new() { Id = "_THEAR_2", Name = "_THEar_2" },
                        new() { Id = "_THEAR_3", Name = "_THEar_3" },
                        new() { Id = "_THEAR_4", Name = "_THEar_4" },
                        new() { Id = "_THEAR_5", Name = "_THEar_5" },
                        new() { Id = "_THEAR_6", Name = "_THEar_6" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TRI", Name = "_Head_Tri", Children = new()
                {
                    new() { GroupId = "_TRIHNOSE_", Descriptors = new()
                    {
                        new() { Id = "_TRIHNOSE_0XRARE", Name = "_TriHNose_0xRARE" },
                        new() { Id = "_TRIHNOSE_1", Name = "_TriHNose_1" },
                        new() { Id = "_TRIHNOSE_9XRARE", Name = "_TriHNose_9xRARE" },
                    }
                    },
                    new() { GroupId = "_TRIHHORN_", Descriptors = new()
                    {
                        new() { Id = "_TRIHHORN_0XRARE", Name = "_TriHHorn_0xRARE" },
                        new() { Id = "_TRIHHORN_1XRARE", Name = "_TriHHorn_1xRARE" },
                        new() { Id = "_TRIHHORN_2", Name = "_TriHHorn_2" },
                        new() { Id = "_TRIHHORN_5", Name = "_TriHHorn_5" },
                        new() { Id = "_TRIHHORN_8", Name = "_TriHHorn_8" },
                        new() { Id = "_TRIHHORN_6", Name = "_TriHHorn_6" },
                        new() { Id = "_TRIHHORN_7", Name = "_TriHHorn_7" },
                        new() { Id = "_TRIHHORN_4", Name = "_TriHHorn_4" },
                        new() { Id = "_TRIHHORN_3", Name = "_TriHHorn_3" },
                    }
                    },
                    new() { GroupId = "_TRIHNECK_", Descriptors = new()
                    {
                        new() { Id = "_TRIHNECK_NULL", Name = "_TriHNeck_NULL" },
                        new() { Id = "_TRIHNECK_1", Name = "_TriHNeck_1" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_TURTLE", Name = "_Head_Turtle", Children = new()
                {
                    new() { GroupId = "_TTLEHNECK_", Descriptors = new()
                    {
                        new() { Id = "_TTLEHNECK_0", Name = "_TtleHNeck_0" },
                        new() { Id = "_TTLEHNECK_1XRARE", Name = "_TtleHNeck_1xRARE" },
                    }
                    },
                    new() { GroupId = "_TTLEEAR_", Descriptors = new()
                    {
                        new() { Id = "_TTLEEAR_0", Name = "_TtleEar_0" },
                        new() { Id = "_TTLEEAR_1XRARE", Name = "_TtleEar_1xRARE" },
                    }
                    },
                    new() { GroupId = "_TTLEHACC_", Descriptors = new()
                    {
                        new() { Id = "_TTLEHACC_0", Name = "_TtleHAcc_0" },
                        new() { Id = "_TTLEHACC_2", Name = "_TtleHAcc_2" },
                        new() { Id = "_TTLEHACC_3", Name = "_TtleHAcc_3" },
                        new() { Id = "_TTLEHACC_4XRARE", Name = "_TtleHAcc_4xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_DIPLO", Name = "_Body_Diplo", Children = new()
                {
                    new() { GroupId = "_DBACC_", Descriptors = new()
                    {
                        new() { Id = "_DBACC_0", Name = "_DBAcc_0" },
                        new() { Id = "_DBACC_3", Name = "_DBAcc_3" },
                        new() { Id = "_DBACC_4", Name = "_DBAcc_4" },
                        new() { Id = "_DBACC_7", Name = "_DBAcc_7" },
                        new() { Id = "_DBACC_6", Name = "_DBAcc_6" },
                        new() { Id = "_DBACC_1", Name = "_DBAcc_1" },
                        new() { Id = "_DBACC_5", Name = "_DBAcc_5" },
                        new() { Id = "_DBACC_2", Name = "_DBAcc_2" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_RHINO", Name = "_Body_Rhino", Children = new()
                {
                    new() { GroupId = "_RHB1_", Descriptors = new()
                    {
                        new() { Id = "_RHB1_0", Name = "_RHB1_0" },
                        new() { Id = "_RHB1_BLOBSXRARE", Name = "_RHB1_BlobsxRARE" },
                        new() { Id = "_RHB1_ROCKSXRARE", Name = "_RHB1_RocksxRARE" },
                        new() { Id = "_RHB1_PRONGSXRARE", Name = "_RHB1_ProngsxRARE" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_TAPIR", Name = "_Body_Tapir", Children = new()
                {
                    new() { GroupId = "_DECOR_", Descriptors = new()
                    {
                        new() { Id = "_DECOR_1", Name = "_Decor_1" },
                        new() { Id = "_DECOR_4", Name = "_Decor_4" },
                        new() { Id = "_DECOR_3", Name = "_Decor_3" },
                        new() { Id = "_DECOR_2", Name = "_Decor_2" },
                        new() { Id = "_DECOR_0", Name = "_Decor_0" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_TRI", Name = "_Body_Tri", Children = new()
                {
                    new() { GroupId = "_TRIBACK_", Descriptors = new()
                    {
                        new() { Id = "_TRIBACK_1XRARE", Name = "_TriBack_1xRARE" },
                        new() { Id = "_TRIBACK_4XRARE", Name = "_TriBack_4xRARE" },
                        new() { Id = "_TRIBACK_7XRARE1", Name = "_TriBack_7xRARE1", Children = new()
                        {
                            new() { GroupId = "_FRONTLEGS_", Descriptors = new()
                            {
                                new() { Id = "_FRONTLEGS_1", Name = "_Frontlegs_1" },
                                new() { Id = "_FRONTLEGS_0", Name = "_Frontlegs_0" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_BODY_TURTLE", Name = "_Body_Turtle", Children = new()
                {
                    new() { GroupId = "_TLB1_", Descriptors = new()
                    {
                        new() { Id = "_TLB1_0", Name = "_TLB1_0" },
                        new() { Id = "_TLB1_BLOBS1XRARE", Name = "_TLB1_Blobs1xRARE" },
                        new() { Id = "_TLB1_4", Name = "_TLB1_4" },
                        new() { Id = "_TLB1_2", Name = "_TLB1_2" },
                        new() { Id = "_TLB1_3", Name = "_TLB1_3" },
                        new() { Id = "_TLB1_1", Name = "_TLB1_1" },
                        new() { Id = "_TLB1_SHELL1XRARE", Name = "_TLB1_Shell1xRARE" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_DIPLO", Name = "_Tail_Diplo" },
                new() { Id = "_TAIL_HIPPO", Name = "_Tail_Hippo" },
                new() { Id = "_TAIL_RHINO", Name = "_Tail_Rhino" },
                new() { Id = "_TAIL_TAPIR", Name = "_Tail_Tapir" },
                new() { Id = "_TAIL_TRI", Name = "_Tail_Tri" },
                new() { Id = "_TAIL_TURTLE", Name = "_Tail_Turtle" },
            }
            },
        }
        },
        new() { CreatureId = "ANTELOPETWOLEGS", FriendlyName = "TWOLEGANTELOPE", Details = new()
        {
            new() { GroupId = "_HEAD_", Descriptors = new()
            {
                new() { Id = "_HEAD_AEATER", Name = "_Head_AEater", Children = new()
                {
                    new() { GroupId = "_AEHACCS_", Descriptors = new()
                    {
                        new() { Id = "_AEHACCS_1", Name = "_AEHAccs_1", Children = new()
                        {
                            new() { GroupId = "_AEHEARA_", Descriptors = new()
                            {
                                new() { Id = "_AEHEARA_NULL", Name = "_AEHEarA_NULL" },
                                new() { Id = "_AEHEARA_1", Name = "_AEHEarA_1" },
                                new() { Id = "_AEHEARA_2", Name = "_AEHEarA_2" },
                                new() { Id = "_AEHEARA_3", Name = "_AEHEarA_3" },
                                new() { Id = "_AEHEARA_4", Name = "_AEHEarA_4" },
                                new() { Id = "_AEHEARA_6", Name = "_AEHEarA_6" },
                                new() { Id = "_AEHEARA_5", Name = "_AEHEarA_5" },
                            }
                            },
                            new() { GroupId = "_AEHTOPA_", Descriptors = new()
                            {
                                new() { Id = "_AEHTOPA_NULL", Name = "_AEHTopA_NULL" },
                                new() { Id = "_AEHTOPA_1", Name = "_AEHTopA_1" },
                                new() { Id = "_AEHTOPA_2", Name = "_AEHTopA_2" },
                                new() { Id = "_AEHTOPA_4", Name = "_AEHTopA_4" },
                            }
                            },
                        }
                        },
                        new() { Id = "_AEHACCS_2", Name = "_AEHAccs_2", Children = new()
                        {
                            new() { GroupId = "_AEHACCSB_", Descriptors = new()
                            {
                                new() { Id = "_AEHACCSB_1", Name = "_AEHAccsB_1" },
                                new() { Id = "_AEHACCSB_2", Name = "_AEHAccsB_2" },
                                new() { Id = "_AEHACCSB_3", Name = "_AEHAccsB_3" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_BUFF", Name = "_Head_Buff", Children = new()
                {
                    new() { GroupId = "_HBTEETH_", Descriptors = new()
                    {
                        new() { Id = "_HBTEETH_1", Name = "_HBTeeth_1" },
                        new() { Id = "_HBTEETH_2", Name = "_HBTeeth_2" },
                    }
                    },
                    new() { GroupId = "_BHHORNS_", Descriptors = new()
                    {
                        new() { Id = "_BHHORNS_NULL", Name = "_BHHorns_NULL" },
                        new() { Id = "_BHHORNS_1", Name = "_BHHorns_1" },
                        new() { Id = "_BHHORNS_2", Name = "_BHHorns_2" },
                        new() { Id = "_BHHORNS_3", Name = "_BHHorns_3" },
                        new() { Id = "_BHHORNS_4", Name = "_BHHorns_4" },
                    }
                    },
                    new() { GroupId = "_BHEARS_", Descriptors = new()
                    {
                        new() { Id = "_BHEARS_4", Name = "_BHEars_4" },
                        new() { Id = "_BHEARS_3", Name = "_BHEars_3" },
                        new() { Id = "_BHEARS_1", Name = "_BHEars_1" },
                    }
                    },
                    new() { GroupId = "_HDACC_", Descriptors = new()
                    {
                        new() { Id = "_HDACC_4XRARE", Name = "_HDAcc_4xRARE" },
                        new() { Id = "_HDACC_NONE", Name = "_HDAcc_none" },
                    }
                    },
                    new() { GroupId = "_BHNOSE_", Descriptors = new()
                    {
                        new() { Id = "_BHNOSE_2XRARE", Name = "_BHNose_2xRARE" },
                        new() { Id = "_BHNOSE_NULL", Name = "_BHNose_NULL" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_DEER", Name = "_Head_Deer", Children = new()
                {
                    new() { GroupId = "_HDTEETH_", Descriptors = new()
                    {
                        new() { Id = "_HDTEETH_1", Name = "_HDTeeth_1" },
                        new() { Id = "_HDTEETH_2", Name = "_HDTeeth_2" },
                    }
                    },
                    new() { GroupId = "_HDEARS_", Descriptors = new()
                    {
                        new() { Id = "_HDEARS_NULL", Name = "_HDEars_NULL" },
                        new() { Id = "_HDEARS_1", Name = "_HDEars_1" },
                        new() { Id = "_HDEARS_9", Name = "_HDEars_9" },
                        new() { Id = "_HDEARS_3", Name = "_HDEars_3" },
                        new() { Id = "_HDEARS_4", Name = "_HDEars_4" },
                    }
                    },
                    new() { GroupId = "_HDHORNS_", Descriptors = new()
                    {
                        new() { Id = "_HDHORNS_NULL", Name = "_HDHorns_NULL" },
                        new() { Id = "_HDHORNS_1", Name = "_HDHorns_1" },
                        new() { Id = "_HDHORNS_2", Name = "_HDHorns_2" },
                        new() { Id = "_HDHORNS_3", Name = "_HDHorns_3" },
                        new() { Id = "_HDHORNS_4", Name = "_HDHorns_4" },
                        new() { Id = "_HDHORNS_5", Name = "_HDHorns_5" },
                    }
                    },
                }
                },
                new() { Id = "_HEAD_GDANE", Name = "_Head_GDane", Children = new()
                {
                    new() { GroupId = "_GDACCS_", Descriptors = new()
                    {
                        new() { Id = "_GDACCS_1", Name = "_GDAccs_1", Children = new()
                        {
                            new() { GroupId = "_GDNOSE_", Descriptors = new()
                            {
                                new() { Id = "_GDNOSE_NULL", Name = "_GDNose_NULL" },
                            }
                            },
                            new() { GroupId = "_GDEAR_", Descriptors = new()
                            {
                                new() { Id = "_GDEAR_NULL", Name = "_GDEar_NULL" },
                                new() { Id = "_GDEAR_1", Name = "_GDEar_1" },
                                new() { Id = "_GDEAR_2", Name = "_GDEar_2" },
                                new() { Id = "_GDEAR_3", Name = "_GDEar_3" },
                                new() { Id = "_GDEAR_4", Name = "_GDEar_4" },
                                new() { Id = "_GDEAR_5", Name = "_GDEar_5" },
                            }
                            },
                        }
                        },
                        new() { Id = "_GDACCS_2", Name = "_GDAccs_2", Children = new()
                        {
                            new() { GroupId = "_GDPLATE_", Descriptors = new()
                            {
                                new() { Id = "_GDPLATE_1", Name = "_GDPlate_1" },
                                new() { Id = "_GDPLATE_2", Name = "_GDPlate_2" },
                                new() { Id = "_GDPLATE_3", Name = "_GDPlate_3" },
                            }
                            },
                        }
                        },
                    }
                    },
                    new() { GroupId = "_HGDTEETH_", Descriptors = new()
                    {
                        new() { Id = "_HGDTEETH_1", Name = "_HGDTeeth_1" },
                        new() { Id = "_HGDTEETH_2", Name = "_HGDTeeth_2" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_BODY_", Descriptors = new()
            {
                new() { Id = "_BODY_BUFF", Name = "_Body_Buff", Children = new()
                {
                    new() { GroupId = "_B2LB_", Descriptors = new()
                    {
                        new() { Id = "_B2LB_NULL", Name = "_B2LB_NULL" },
                        new() { Id = "_B2LB_2", Name = "_B2LB_2" },
                        new() { Id = "_B2LB_11", Name = "_B2LB_11" },
                        new() { Id = "_B2LB_3", Name = "_B2LB_3" },
                        new() { Id = "_B2LB_9", Name = "_B2LB_9" },
                        new() { Id = "_B2LB_5", Name = "_B2LB_5" },
                        new() { Id = "_B2LB_8", Name = "_B2LB_8" },
                    }
                    },
                }
                },
                new() { Id = "_BODY_CHICKEN", Name = "_Body_Chicken" },
                new() { Id = "_BODY_DEER", Name = "_Body_Deer", Children = new()
                {
                    new() { GroupId = "_B2LD_", Descriptors = new()
                    {
                        new() { Id = "_B2LD_2", Name = "_B2LD_2" },
                        new() { Id = "_B2LD_3", Name = "_B2LD_3" },
                        new() { Id = "_B2LD_8", Name = "_B2LD_8" },
                        new() { Id = "_B2LD_4", Name = "_B2LD_4" },
                        new() { Id = "_B2LD_5", Name = "_B2LD_5" },
                        new() { Id = "_B2LD_7", Name = "_B2LD_7" },
                    }
                    },
                }
                },
            }
            },
            new() { GroupId = "_TAIL_", Descriptors = new()
            {
                new() { Id = "_TAIL_1", Name = "_Tail_1" },
                new() { Id = "_TAIL_2", Name = "_Tail_2", Children = new()
                {
                    new() { GroupId = "_FINB_", Descriptors = new()
                    {
                        new() { Id = "_FINB_0", Name = "_finB_0" },
                        new() { Id = "_FINB_1XRARE", Name = "_finB_1xRARE" },
                        new() { Id = "_FINB_3XRARE", Name = "_finB_3xRARE" },
                        new() { Id = "_FINB_2XRARE", Name = "_finB_2xRARE" },
                        new() { Id = "_FINB_0B", Name = "_finB_0B" },
                    }
                    },
                }
                },
                new() { Id = "_TAIL_3", Name = "_Tail_3", Children = new()
                {
                    new() { GroupId = "_FIN_", Descriptors = new()
                    {
                        new() { Id = "_FIN_1XRARE", Name = "_Fin_1xRARE" },
                        new() { Id = "_FIN_0", Name = "_Fin_0" },
                        new() { Id = "_FIN_2XRARE", Name = "_Fin_2xRARE" },
                        new() { Id = "_FIN_0B", Name = "_Fin_0B" },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "WEIRDBUTTERFLY", FriendlyName = "WEIRDBUTTERFLY", Details = new()
        {
            new() { GroupId = "_SHELLFLY1_", Descriptors = new()
            {
                new() { Id = "_SHELLFLY1_1", Name = "_ShellFly1_1" },
                new() { Id = "_SHELLFLY1_2", Name = "_ShellFly1_2" },
            }
            },
        }
        },
        new() { CreatureId = "WEIRDRIG", FriendlyName = "WEIRDFLOAT or WEIRDROLL", Details = new()
        {
            new() { GroupId = "_WEIRD_", Descriptors = new()
            {
                new() { Id = "_WEIRD_CRYSTAL", Name = "_Weird_Crystal" },
                new() { Id = "_WEIRD_FLOAT", Name = "_Weird_Float", Children = new()
                {
                    new() { GroupId = "_FLOAT_", Descriptors = new()
                    {
                        new() { Id = "_FLOAT_FRACTCUB", Name = "_Float_FractCube" },
                        new() { Id = "_FLOAT_BUBBLEXO", Name = "_Float_BubblexOR" },
                        new() { Id = "_FLOAT_SHARDS", Name = "_Float_Shards" },
                        new() { Id = "_FLOAT_WIRECELL", Name = "_Float_WireCell" },
                        new() { Id = "_FLOAT_HYDROGAR", Name = "_Float_HydroGardenxOR" },
                    }
                    },
                }
                },
                new() { Id = "_WEIRD_ROLL", Name = "_Weird_Roll", Children = new()
                {
                    new() { GroupId = "_ROLL_", Descriptors = new()
                    {
                        new() { Id = "_ROLL_SINGLEJOI", Name = "_Roll_SingleJoint", Children = new()
                        {
                            new() { GroupId = "_ROLL_", Descriptors = new()
                            {
                                new() { Id = "_ROLL_STRUCTURE", Name = "_Roll_Structure" },
                                new() { Id = "_ROLL_CONTOUR", Name = "_Roll_Contour" },
                                new() { Id = "_ROLL_BEAMSTONE", Name = "_Roll_BeamStone" },
                            }
                            },
                        }
                        },
                    }
                    },
                }
                },
            }
            },
        }
        },
        new() { CreatureId = "WEIRDFLOCK", FriendlyName = "WEIRDFLOCK", Details = new()
        {
            new() { GroupId = "_WEIRD_", Descriptors = new()
            {
                new() { Id = "_WEIRD_CRYSTAL", Name = "_Weird_Crystal" },
            }
            },
        }
        },
        new() { CreatureId = "WEIRDRIGGROUND", FriendlyName = "WEIRDRIGGROUND", Details = new()
        {
            new() { GroupId = "_CRYSTAL_", Descriptors = new()
            {
                new() { Id = "_CRYSTAL_BEAM", Name = "_Crystal_Beam" },
                new() { Id = "_CRYSTAL_BONESP", Name = "_Crystal_BoneSpire" },
                new() { Id = "_CRYSTAL_HEXAGO", Name = "_Crystal_Hexagon" },
                new() { Id = "_CRYSTAL_PILLAR", Name = "_Crystal_Pillar" },
                new() { Id = "_CRYSTAL_SHARDS", Name = "_Crystal_ShardSingle" },
            }
            },
        }
        },
    };
}

// ==================================================================
//  Companion Accessories (from Companion Accessories.json)
// ==================================================================

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
/// Companion accessory slot index mapping: [0] = Right, [1] = Left, [2] = Chest.
/// </summary>
public enum AccessorySlot
{
    /// <summary>Right side accessory (index 0 in save).</summary>
    Right = 0,
    /// <summary>Left side accessory (index 1 in save).</summary>
    Left = 1,
    /// <summary>Chest/front accessory (index 2 in save).</summary>
    Chest = 2,
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
    /// Per-slot allowed accessory IDs, derived from the game's CHARACTERCUSTOMISATIONUIDATA.MXML(MBIN).
    /// The same shared accessories (PET_ACC_NULL, PET_ACC_0–11) appear in all slots;
    /// slot-specific accessories are unique to Left, Right, or Chest.
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
        [AccessorySlot.Chest] = new(StringComparer.OrdinalIgnoreCase)
        {
            "PET_ACC_NULL",
            "PET_ACC_0", "PET_ACC_1", "PET_ACC_2", "PET_ACC_3", "PET_ACC_4",
            "PET_ACC_5", "PET_ACC_6", "PET_ACC_7", "PET_ACC_8", "PET_ACC_9",
            "PET_ACC_10", "PET_ACC_11",
            // Chest-specific
            "PET_ACC_30",
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

// ==================================================================
//  Pet Battle Moves (from Pet Battle Moves.json)
// ==================================================================

/// <summary>Represents a single phase within a pet battle move.</summary>
public class PetBattleMovePhase
{
    /// <summary>Phase strength level (e.g. "VeryLight", "Medium").</summary>
    public string Strength { get; set; } = "";
    /// <summary>Visual effect type (e.g. "Projectile", "Beam").</summary>
    public string Effect { get; set; } = "";

    /// <summary>Normalised display string for Strength.</summary>
    public string StrengthDisplay => DisplayStringHelper.NormalizeDisplayString(Strength);
    /// <summary>Normalised display string for Effect.</summary>
    public string EffectDisplay => Effect == "DoTDamage"
        ? "Damage over Time"
        : DisplayStringHelper.NormalizeDisplayString(Effect);
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

    /// <summary>Normalised display string for Target.</summary>
    public string TargetDisplay => DisplayStringHelper.NormalizeDisplayString(Target);

    /// <summary>Normalised display string for IconStyle.</summary>
    public string IconStyleDisplay => DisplayStringHelper.NormalizeDisplayString(IconStyle);

    /// <summary>
    /// Returns an emoji/symbol matching the icon style.
    /// </summary>
    public string IconEmoji => IconStyle?.ToLowerInvariant() switch
    {
        // Yeah, emojis - I know... leave me alone lol.
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

// ==================================================================
//  Pet Battle Movesets (from Pet Battle Movesets.json)
// ==================================================================

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

    /// <summary>Normalised display name (e.g. "BASIC" -> "Basic", "DOT_BOMBER" -> "Dot Bomber").</summary>
    public string DisplayName => DisplayStringHelper.NormalizeDisplayString(Id);

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

// ==================================================================
//  Pet Biome Affinity Map (from Game Table Globals.json)
// ==================================================================

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
    /// </summary>
    public static string GetAffinityEmoji(string affinity)
    {
        return affinity?.ToLowerInvariant() switch
        {
            "toxic" => "\U0001F480",   // skull
            "radioactive" => "\u2622\uFE0F", // radioactive
            "fire" => "\U0001F525",    // fire
            "cold" or "frozen" => "\u2744\uFE0F", // snowflake
            "lush" => "\U0001F33F",    // herb/leaf
            "barren" => "\U0001F3DC\uFE0F", // desert
            "weird" => "\U0001F52E",   // crystal ball
            "mech" => "\u2699\uFE0F",  // gear
            "normal" => "\u2B50",      // star
            _ => "",
        };
    }

    /// <summary>
    /// Returns a normalised display name for an affinity value, prefixed with an emoji.
    /// Uses the loc stub if available, otherwise normalises the raw affinity string.
    /// For example: "Fire" -> "Fire" (already normalised), "Normal" -> "Normal".
    /// The loc-resolved value (e.g. "FIRE") is also normalised: "FIRE" -> "Fire".
    /// </summary>
    public static string GetAffinityDisplayName(string affinity, LocalisationService? service = null)
    {
        if (string.IsNullOrEmpty(affinity)) return "";

        string emoji = GetAffinityEmoji(affinity);
        string name;

        // Try to resolve via game localisation
        if (service != null && _affinityLoc.TryGetValue(affinity, out var locKey))
        {
            var locValue = service.Lookup(locKey);
            if (locValue != null)
            {
                name = DisplayStringHelper.NormalizeDisplayString(locValue);
                return !string.IsNullOrEmpty(emoji) ? $"{emoji}{name}" : name;
            }
        }

        // Fallback: normalise the raw affinity string
        name = DisplayStringHelper.NormalizeDisplayString(affinity);
        return !string.IsNullOrEmpty(emoji) ? $"{emoji}{name}" : name;
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
