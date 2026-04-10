using System.Text.RegularExpressions;

namespace NMSE.Extractor.Data;

/// <summary>
/// Categorization rules for organizing NMS items into final JSON DB files.
/// </summary>
public static class Categorizer
{
    private static readonly Regex TechModuleClassPattern = new(@"^[CBSA]-Class .+ (Upgrade|Node)$", RegexOptions.Compiled);
    private static readonly Regex DeployableSalvageClassPattern = new(@"^[CBSA]-Class Deployable Salvage$", RegexOptions.Compiled);
    private static readonly Regex StarshipComponentGroupPattern = new(@"^.+ Starship Component$", RegexOptions.Compiled);

    private static readonly HashSet<string> StarshipExactGroups = new()
    {
        "Spacecraft", "Exclusive Spacecraft", "Living Ship Component",
        "Starship Core Component", "Starship Customisation Option"
    };

    private static readonly HashSet<string> StarshipExcludedGroups = new()
    {
        "Starship Interior Adornment", "Living Ship Component", "Starship Core Component",
        "Damaged Starship Component", "Starship Exhaust Override"
    };

    private static readonly HashSet<string> StarshipUpgradeGroups = new() { "Starship Core Component" };

    // All categorization rules - exact group matches per output file
    // ORDER MATTERS: Earlier rules take precedence
    private static readonly Dictionary<string, HashSet<string>> ExactRules = new()
    {
        ["Food.json"] = new()
        {
            "Carnivore Bait", "Compressed Nutrients", "Edible Product", "Herbivore Bait",
            "Vile Larva", "Raw Ingredient"
        },
        ["Constructed Technology.json"] = new()
        {
            "Access Card", "Advanced Research Module", "Agricultural Robot",
            "Atmosphere Processing Device", "Audio Device", "Audio Synthesiser",
            "Auto-Expanding Freighter Module", "Automated Refinery Unit",
            "Automatic Harvesting Robot", "Autonomous Gas Compressor", "Baryogenesis Unit",
            "ByteBeat Extension", "Capital Ship Wiring Platform", "Communications terminal",
            "Deep Water Survival Module", "Discovery Display Hologram",
            "Encrypted Navigation Data", "Environmental scanner", "Experimental Fabricator",
            "Hazard Protection Charging System", "Health recharge module",
            "Heavy Loader Storage Bay", "Incomplete Data Sequence",
            "Instantaneous Transport Network", "Long-Distance Matter Transfer Device",
            "Portable Exocraft Research Station", "Portable Exosuit Research Station",
            "Portable Multi-Tool Research Station", "Portable Starship Research Station",
            "Portable Sustenance Unit", "Portable mining device", "Portable waypoint marker",
            "Portal Construction Blueprint Terminal", "Power Distribution Module",
            "Powerful Minotaur Weapon Upgrade", "Pulsating Yolk Sac", "Race Part",
            "Salvaged Technology", "Self-Sustaining Auto Creel",
            "Sentient Vessel Storage Sac", "Significant Minotaur Weapon Upgrade",
            "Site Evaluation Device", "Specialist terminal", "Starship Expansion Module",
            "Storage unit", "Submarine Docking Bay", "Substance Distribution Module",
            "Supreme Minotaur Weapon Upgrade", "System Authority Payment Substitute",
            "Technology Echo", "Technology Recovery Device", "Trading terminal",
            "Upgrade Unit", "Vehicle Translocation Device"
        },
        ["Buildings.json"] = new()
        {
            "Construction module", "Environmental decoration", "Autonomous Agriculture Unit",
            "Recycled Construction Component", "Advanced Freighter Module",
            "Agricultural Module", "Alloy Construction Component",
            "Aquatic Construction Module", "Aquatic Observation Module",
            "Audio Containment Crystal", "Autonomous Storage Device", "Betentacled Steward",
            "Boundary Integrity Totem", "Concrete Construction Component",
            "Construction item", "Consumable Pyrotechnics Device", "Core Freighter Module",
            "Custom Freighter Module", "Customise Personal Visuals", "Decoration",
            "Decorative Aqueduct", "Dynamic Signpost", "Exclusive Flag",
            "Expedition Management Terminal", "Experimental Summoning Technology",
            "Fabricator", "Farming tech", "Fitting", "Floating Primitive",
            "Freighter Interior Module", "Furniture", "Inefficient Heat Source",
            "Light Device", "Metal Construction Component", "Organic Decoration",
            "Organic Lighting", "Ornamental Base Companion", "Ornamental Skull",
            "Personal Drone Valet", "Personal Reality Flux Monitor", "Placeable Sludge",
            "Planetary Probe", "Rapid Deployment Freighter Module", "Salvaged Base Part",
            "Secure Map Containment", "Simulated Trader Lifeform",
            "Space Anomaly Replica Part", "Specialised Creature Enclosure",
            "Specialist Planetary Chart", "Stabilised Reality Glitch",
            "Stone Construction Component", "Synthetic Space Station Decoration",
            "System Simulation Device", "Timber Construction Component",
            "Unlockable Decoration", "Wooden Construction Component", "Worker Terminal",
            "Fossil Display Case", "Wall Access Route", "Constructable Relic"
        },
        ["Curiosities.json"] = new()
        {
            "Living Ship Component", "A Last Link Home", "AI Master Key",
            "Advanced Crafted Product", "Agitated Curiosity", "Anomalous Data Unit",
            "Anomalous Homing Device", "Anomalous Material", "Aquatic Relic",
            "Battleship Core Remnant", "Burning Glassy Remnant", "Classified Dossier",
            "Communion of Hirk", "Compressed Mineral", "Concentrated Element Shard",
            "Contained Reality Glitch", "Contraband", "Coordinate Data",
            "Corrupted Aeronic Core", "Cosmic Leviathan", "Cultist Document",
            "Energetic Fragment", "Fishing Bait", "Freighter Customisation Option",
            "Glassy Reality Intrusion", "Glorious Trophy", "Harmonic Location Tracker",
            "Harmonic Planetary Chart", "High value curiosity", "Horrific Sample",
            "Hungering Embryo", "Illicit Geographic Data", "Intestinal Fragment",
            "Medicinal Substance", "Medium value curiosity", "Monstrosity",
            "Overwritten Digital Record", "Pre-assembled Fossil Display",
            "Precious Material", "Precious Spawn", "Recovered Sentinel Components",
            "Salvaged Identification Device", "Semi-Conscious Gemstone",
            "Sentience Vessel", "Sentinel Communicator", "Starship Component",
            "Subconscious Access Device", "Timeless Artifact", "Trade commodity",
            "Unblinking Eye", "Unique valuable curiosity",
            "Utopia Foundation Settlement Chart", "Utopia Foundation Supply Cache",
            "Valuable Mineral Deposit", "Valuable Ore", "Valuable Rock Sample",
            "Very high value curiosity"
        },
        ["Technology Module.json"] = new()
        {
            "A-Class Analysis Visor Upgrade", "A-Class Blaze Javelin Upgrade",
            "A-Class Boltcaster Upgrade", "A-Class Cyclotron Ballista Upgrade",
            "A-Class Daedalus Engine Upgrade", "A-Class Defence Systems Upgrade",
            "A-Class Deflector Shield Upgrade", "A-Class Exocraft Boosters Upgrade",
            "A-Class Exocraft Mining Laser Upgrade", "A-Class Fusion Engine Upgrade",
            "A-Class Geology Cannon Upgrade", "A-Class Grafted Eyes Node",
            "A-Class Humboldt Drive Upgrade", "A-Class Hyperdrive Upgrade",
            "A-Class Infra-Knife Accelerator Upgrade", "A-Class Launch Thruster Upgrade",
            "A-Class Life Support Upgrade", "A-Class Mining Beam Upgrade",
            "A-Class Minotaur Cannon Upgrade", "A-Class Minotaur Flamethrower Upgrade",
            "A-Class Minotaur Laser Upgrade", "A-Class Mounted Cannon Upgrade",
            "A-Class Movement System Upgrade", "A-Class Nautilon Cannon Upgrade",
            "A-Class Neural Assembly Node", "A-Class Phase Beam Upgrade",
            "A-Class Photon Cannon Upgrade", "A-Class Plasma Launcher Upgrade",
            "A-Class Positron Ejector Upgrade", "A-Class Pulse Engine Upgrade",
            "A-Class Pulse Spitter Upgrade", "A-Class Pulsing Heart Node",
            "A-Class Radiation Protection Upgrade", "A-Class Scatter Blaster Upgrade",
            "A-Class Scream Suppressor Node", "A-Class Singularity Cortex Node",
            "A-Class Spewing Vents Node", "A-Class Thermal Protection Upgrade",
            "A-Class Toxic Protection Upgrade",
            "B-Class Analysis Visor Upgrade", "B-Class Blaze Javelin Upgrade",
            "B-Class Boltcaster Upgrade", "B-Class Cyclotron Ballista Upgrade",
            "B-Class Daedalus Engine Upgrade", "B-Class Defence Systems Upgrade",
            "B-Class Deflector Shield Upgrade", "B-Class Exocraft Boosters Upgrade",
            "B-Class Exocraft Mining Laser Upgrade", "B-Class Fusion Engine Upgrade",
            "B-Class Geology Cannon Upgrade", "B-Class Grafted Eyes Node",
            "B-Class Humboldt Drive Upgrade", "B-Class Hyperdrive Upgrade",
            "B-Class Infra-Knife Accelerator Upgrade", "B-Class Launch Thruster Upgrade",
            "B-Class Life Support Upgrade", "B-Class Mining Beam Upgrade",
            "B-Class Minotaur Cannon Upgrade", "B-Class Minotaur Flamethrower Upgrade",
            "B-Class Minotaur Laser Upgrade", "B-Class Mounted Cannon Upgrade",
            "B-Class Movement System Upgrade", "B-Class Nautilon Cannon Upgrade",
            "B-Class Neural Assembly Node", "B-Class Phase Beam Upgrade",
            "B-Class Photon Cannon Upgrade", "B-Class Plasma Launcher Upgrade",
            "B-Class Positron Ejector Upgrade", "B-Class Pulse Engine Upgrade",
            "B-Class Pulse Spitter Upgrade", "B-Class Pulsing Heart Node",
            "B-Class Radiation Protection Upgrade", "B-Class Scatter Blaster Upgrade",
            "B-Class Scream Suppressor Node", "B-Class Singularity Cortex Node",
            "B-Class Spewing Vents Node", "B-Class Thermal Protection Upgrade",
            "B-Class Toxic Protection Upgrade",
            "C-Class Analysis Visor Upgrade", "C-Class Blaze Javelin Upgrade",
            "C-Class Boltcaster Upgrade", "C-Class Cyclotron Ballista Upgrade",
            "C-Class Defence Systems Upgrade", "C-Class Deflector Shield Upgrade",
            "C-Class Exocraft Boosters Upgrade", "C-Class Exocraft Mining Laser Upgrade",
            "C-Class Fusion Engine Upgrade", "C-Class Geology Cannon Upgrade",
            "C-Class Grafted Eyes Node", "C-Class Humboldt Drive Upgrade",
            "C-Class Hyperdrive Upgrade", "C-Class Infra-Knife Accelerator Upgrade",
            "C-Class Launch Thruster Upgrade", "C-Class Mining Beam Upgrade",
            "C-Class Mounted Cannon Upgrade", "C-Class Movement System Upgrade",
            "C-Class Nautilon Cannon Upgrade", "C-Class Neural Assembly Node",
            "C-Class Phase Beam Upgrade", "C-Class Photon Cannon Upgrade",
            "C-Class Plasma Launcher Upgrade", "C-Class Positron Ejector Upgrade",
            "C-Class Pulse Engine Upgrade", "C-Class Pulse Spitter Upgrade",
            "C-Class Pulsing Heart Node", "C-Class Scatter Blaster Upgrade",
            "C-Class Scream Suppressor Node", "C-Class Singularity Cortex Node",
            "C-Class Spewing Vents Node",
            "Forbidden Exosuit Module", "Forbidden Multi-Tool Module",
            "Illegal Analysis Visor Upgrade", "Illegal Blaze Javelin Upgrade",
            "Illegal Boltcaster Upgrade", "Illegal Cyclotron Ballista Upgrade",
            "Illegal Defence Systems Upgrade", "Illegal Deflector Shield Upgrade",
            "Illegal Geology Cannon Upgrade", "Illegal Hazard Protection Upgrade",
            "Illegal Hyperdrive Upgrade", "Illegal Infra-Knife Accelerator Upgrade",
            "Illegal Launch Thruster Upgrade", "Illegal Life Support Upgrade",
            "Illegal Mining Beam Upgrade", "Illegal Movement System Upgrade",
            "Illegal Phase Beam Upgrade", "Illegal Photon Cannon Upgrade",
            "Illegal Plasma Launcher Upgrade", "Illegal Positron Ejector Upgrade",
            "Illegal Pulse Engine Upgrade", "Illegal Pulse Spitter Upgrade",
            "Illegal Scatter Blaster Upgrade",
            "Powerful Underwater Oxygen Upgrade",
            "S-Class Analysis Visor Upgrade", "S-Class Blaze Javelin Upgrade",
            "S-Class Boltcaster Upgrade", "S-Class Cyclotron Ballista Upgrade",
            "S-Class Daedalus Engine Upgrade", "S-Class Defence Systems Upgrade",
            "S-Class Deflector Shield Upgrade", "S-Class Exocraft Boosters Upgrade",
            "S-Class Exocraft Mining Laser Upgrade", "S-Class Fusion Engine Upgrade",
            "S-Class Geology Cannon Upgrade", "S-Class Grafted Eyes Node",
            "S-Class Humboldt Drive Upgrade", "S-Class Hyperdrive Upgrade",
            "S-Class Infra-Knife Accelerator Upgrade", "S-Class Launch Thruster Upgrade",
            "S-Class Life Support Upgrade", "S-Class Mining Beam Upgrade",
            "S-Class Minotaur Cannon Upgrade", "S-Class Minotaur Flamethrower Upgrade",
            "S-Class Minotaur Laser Upgrade", "S-Class Mounted Cannon Upgrade",
            "S-Class Movement System Upgrade", "S-Class Nautilon Cannon Upgrade",
            "S-Class Neural Assembly Node", "S-Class Phase Beam Upgrade",
            "S-Class Photon Cannon Upgrade", "S-Class Plasma Launcher Upgrade",
            "S-Class Positron Ejector Upgrade", "S-Class Pulse Engine Upgrade",
            "S-Class Pulse Spitter Upgrade", "S-Class Pulsing Heart Node",
            "S-Class Radiation Protection Upgrade", "S-Class Scatter Blaster Upgrade",
            "S-Class Scream Suppressor Node", "S-Class Singularity Cortex Node",
            "S-Class Spewing Vents Node", "S-Class Thermal Protection Upgrade",
            "S-Class Toxic Protection Upgrade",
            "Salvaged Freighter Module", "Significant Underwater Oxygen Upgrade",
            "Underwater Oxygen Upgrade"
        },
        ["Others.json"] = new()
        {
            "Reward Item", "Technological Currency", "%NAME%'s Genetic Material",
            "Damaged Starship Component", "Alien Cartographic Data",
            "Anomalous Face Transformation", "Autophage Staff Backbone",
            "Autophage Staff Core", "Autophage Staff Head",
            "Bioluminescent Spawning Sac", "Bone Cradle", "Captured Space Protozoa",
            "Cartographic Data", "Consciousness Housing", "Cosmic Melody Instrument",
            "Crystalline Breach", "Crystalline Sentience Cage", "Cursed Frigate",
            "Customised Fishing Equipment", "Dig Site Cartographic Data",
            "Emergency Cartographic Data", "Exclusive Companion Egg",
            "Exclusive Frigate", "Exclusive Multifunction Survival Device",
            "Exclusive Spacecraft", "Explorer Starship Component",
            "Fighter Starship Component", "Fleshy Scroll", "Forgotten Void Data",
            "Fragment of the Atlas", "Harnessed Sentience",
            "Hauler Starship Component", "High Voltage Energy Pack",
            "Horrific Spawn", "Ingredient Category", "Legendary Fish",
            "Loop Artifact", "Loop Manifestation Technology",
            "Minotaur Limb Replacement", "Multi-Tool Subcomponent",
            "Multifunction Survival Device Upgrade", "Navigational Data Recorder",
            "Otherworldly Concoction", "Personal Visual Accessory",
            "Planetary Cultural Chart", "Portal Restoration Device", "Prepared Meal",
            "Starship Subcomponent", "Psychedelic Spore Device", "Pulsing Blue Sac",
            "Rebound Particle", "Roaming Gek", "Salvaged Frigate Module",
            "Self-Aware Alloy", "Solar Starship Component", "Spacecraft",
            "Stabilised Glitch", "Starship Bobblehead", "Starship Core Component",
            "Starship Trails", "Stellar Guidance Instrument",
            "Submerged Cultural Sample", "Supercharged Glitter",
            "Synthetic Starbirth Egg", "Targeted Gek Relic",
            "Targeted Korvax Relic", "Targeted Vy'keen Relic",
            "Timeworn Coordinate Charts", "Tormented Korvax", "Unstable Plasma",
            "Vy'keen Effigy",
            "Autophage Appearance Modification", "Bespoke Personal Identification",
            "Collected Flotsam", "Commercial Cartographic Data",
            "Exosuit Visual Enhancement", "Freighter Engine Flare Override",
            "Specialist Exosuit Visuals",
            "Jetpack Exhaust Override", "Living Curiosity",
            "Pyrotechnics Multi-Pack", "Robotic Spawn Capsule",
            "Rogue Technology Echo", "Salvaged Autophage Component",
            "Salvaged Upgrade Components", "Scrambled Geographic Data",
            "Secret Cartographic Data", "Secure System Pass",
            "Sentinel Spawn Capsule", "Ship-summoning beacon", "Spacetime Tether",
            "Spawning Sac", "Starship Customisation Option",
            "Starship Exhaust Override", "Starship Interior Adornment",
            "Starship Subcomponent", "Teleport Network Tunnel", "Titanic Spawn",
            "Unique Korvax Technology?", "Unlockable Armour", "Unlockable Boots",
            "Unlockable Cape", "Unlockable Companion Accessory", "Unlockable Gesture",
            "Unlockable Gloves", "Unlockable Head", "Unlockable Helmet",
            "Unlockable Hood", "Unlockable Jetpack", "Unlockable Leg Customisation",
            "Unlockable Torso Customisation", "Vile Larval Sac",
            "Voice of the Wordless Atlas", "Voltaic Component"
        },
        ["Products.json"] = new()
        {
            "Advanced Agricultural Product", "Advanced Mineral Product",
            "Bio-luminescent Material", "Chemical Curiosity",
            "Compressed Atmospheric Gas", "Concentrated Metal Deposit",
            "Crafted Technology Component", "Earth", "Enhanced Gas Product",
            "Enriched Enriched Sub", "Farmer's Product", "Fusion Accelerant",
            "Highly Refined Technology", "Ignition Fuel", "Manufactured Alloy",
            "Manufactured Gas Product", "Metal Alloy", "Organic Catalyst",
            "Portable Sustenance", "Processed Agricultural Product",
            "Refined Industrial Product", "Refined Stellar Metal",
            "Technical Blueprints", "Tradeable Sub",
            "A-Class Deployable Salvage", "Alloy Metal",
            "Anomalous Planetary Location Data", "Anomaly Attraction Device",
            "Automatic Patching Unit", "B-Class Deployable Salvage",
            "Banned Neutron Cannon Upgrade", "Bionic Ark",
            "C-Class Deployable Salvage", "Concentrated Carbon Deposit",
            "Concentrated Chlorine Deposit", "Concentrated Cobalt Deposit",
            "Concentrated Oxygen Deposit", "Concentrated Sodium Deposit",
            "Consumable Frigate Upgrade", "Drone Component",
            "Enriched Alloy Metal", "Fragment of Life", "Fragile Breath",
            "Frozen Archive", "Glassy Indeterminance",
            "Hyperdrive Charging Unit", "Installable Technology Package",
            "Living Starship Organ", "Orb Plant", "Plasma Launcher Recharge",
            "Plantable Seed", "Portable Energy Storage",
            "Portable Life Support Power", "Powerful Neutron Cannon Upgrade",
            "Reactivated Neural Ladder", "S-Class Deployable Salvage",
            "Sentinel Debris", "Significant Neutron Cannon Upgrade",
            "Stabilised Di-hydrogen Fuel", "Submarine Reactor Fuel",
            "Superheated Life Fragment", "Supreme Neutron Cannon Upgrade",
            "Universal Ammo Module", "Universal Technology Platform",
            "Auto-Regenerating Lifeform"
        },
        ["Technology.json"] = new()
        {
            "Industrial Waste Manipulation Device", "Chargeable Power Unit",
            "Fuel Incineration Device", "Generator Coupling", "Homeworld Repeater",
            "Deep-Space Salvage Locator", "Priceless Fragment",
            "Self-Mounted Refiner Unit", "A relic of another place",
            "Active Camouflage Unit", "Adaptive Camouflage Suit",
            "Advanced Crafted Technology", "Advanced Engine Technology",
            "Advanced Exocraft Mining Laser", "Advanced Minotaur Mining Laser",
            "Advanced Starship Pilot Module", "Aerial Propulsion Booster",
            "Aeron Sub-Orbital Engine", "Alchemical Laser", "Analysis Device",
            "Analysis Visor Upgrade", "Ancient Technology",
            "Anti-Electronics Starship Weapon", "Anti-Probe Countermeasures",
            "Antiparticle Diffuser", "Aquatic Landing System",
            "Aquatic Resource Collection Unit", "Aquatic Respiration Aid",
            "Automated Breach Integrity Device", "Automatic Bait Dispenser",
            "Automatic Charging System", "Automatic Feeding Unit",
            "Automatic Language Parser", "Autonomous Control Unit",
            "Autophage Resonance Key", "Bespoke Landing Gear",
            "Biological Scanning Aid", "Binocular Tagging Device",
            "Blaze Javelin Upgrade", "Boltcaster Companion Unit",
            "Boltcaster Upgrade", "Close Range Starship Weapon",
            "Companion Customisation", "Consumable Technology", "Cosmic Brand",
            "Crafted Component", "Crafted Technology", "Crafted Technology Sub",
            "Damage Overload Module", "Damaged Autophage Component",
            "Damaged Component", "Damaged Multi-Tool Component",
            "Defensive Shield Technology", "Deployable Angling Device",
            "Deployable Fishing Platform", "Echo Seed Detection Device",
            "Economy Scanner", "Emergency Life Support System",
            "Energy Beam Weapon", "Energy Projectile Weapon",
            "Energy Redistribution Routing", "Ethereal Incubator",
            "Exocraft Catalyst Processor", "Exocraft Engine Upgrade",
            "Exocraft Handling Modification", "Exocraft Hazard Protection Unit",
            "Exocraft Ignition Weapon", "Exocraft Mining Attachment",
            "Exocraft Mining Upgrade", "Exocraft Power System",
            "Exocraft Scan Equipment", "Exocraft Scanning Aid",
            "Exocraft Signal Detection Upgrade", "Exocraft Tech",
            "Exocraft Weapon Attachment", "Exosuit Augmentation",
            "Exosuit Cargo Slot Expansion", "Exosuit Environmental Shielding",
            "Exosuit Inventory Slot Expansion", "Exosuit Movement Upgrade",
            "Exosuit Tech", "Exosuit Tech Slot Expansion", "Exosuit Upgrade",
            "Explosive Energy Weapon", "Explosive Grenades",
            "Freighter Life Support Module", "Freighter Tech",
            "Freighter-Mounted Teleportation Device", "Fused Organic Technology",
            "Geometric Anomaly", "Glass / Boundary Terminal",
            "Hazard Protection Unit", "Hazard Protection Upgrade",
            "Heavy Locking Mechanism", "Heavy Mounted Weapon",
            "Highly Customised Starship Engine", "Hostile Probe Deflector",
            "Humboldt Drive Upgrade", "Hyperdrive Augmentation",
            "Hyperdrive Companion Unit", "Hyperdrive Upgrade: Blue Systems",
            "Hyperdrive Upgrade: Green Systems", "Hyperdrive Upgrade: Purple Systems",
            "Hyperdrive Upgrade: Red Systems", "Illegally Modified Warp Drive",
            "Incomplete Sentinel Salvage", "Ingredient Scanning Aid",
            "Interstellar Atlas Propulsion", "Inventory Expansion Device",
            "Jetpack Augmentation", "Jetpack Upgrade Unit",
            "Landscape Shaping Tool", "Life Support Upgrade",
            "Lightspeed Warp Drive", "Long Range Sensor Technology",
            "Long Range Starship Weapon", "Long-range Sensor Technology",
            "Material Gain Amplifier", "Mercantile Missile Summoning Device",
            "Mind Ark Notch", "Mineral Extraction Laser",
            "Mining Beam Companion Unit", "Mining Beam Upgrade",
            "Minotaur AI Pilot", "Minotaur Core Replacement",
            "Minotaur Digging Laser", "Minotaur Engine Upgrade",
            "Minotaur Mining Utility", "Minotaur Power Unit",
            "Minotaur Scan Attachment", "Minotaur Tech",
            "Minotaur Thruster System", "Motor Efficiency Upgrade",
            "Multi-Tool Upgrade", "Nautilon Terrain Manipulator",
            "Nautilus Tech", "Neutron Cannon Upgrade", "Offering to the Sea",
            "Old, unusable technology", "Oxygen Recycler", "Phase Beam Upgrade",
            "Photon Cannon Upgrade", "Protective Tech",
            "Pulse Engine Augmentation", "Pulse Engine Companion Unit",
            "Pulse Spitter Upgrade", "Radiant Defensive Barrier",
            "Rapid Fire Projectile Weapon", "Rapid Fire Starship Weapon",
            "Reinforced Barometric Stabiliser", "Repurposed High-Energy Beam",
            "Repurposed Sub-Light Drive", "Resource Scanning Technology",
            "Scatter Blaster Upgrade", "Scatter Shot Projectile Weapon",
            "Self-Mounted Advanced Refiner", "Sentient Vessel Node",
            "Sentinel Ship Gun", "Ship Tech",
            "Ship-Mounted Teleportation Device", "Short-Range Fire Delivery Unit",
            "Solar Recharge Unit", "Soul Extractor",
            "Spacecraft Hull Protection", "Squadron Tech",
            "Starship Cargo Slot Expansion", "Starship Cyclotron Upgrade",
            "Starship Deflection System", "Starship Energy Recycling System",
            "Starship Energy Weapon", "Starship Flight Booster",
            "Starship Infra-Knife Upgrade", "Starship Inventory Slot Expansion",
            "Starship Launch System Upgrade", "Starship Life Support Unit",
            "Starship Positron Ejector Upgrade", "Starship Projectile Weapon",
            "Starship Rockets Upgrade", "Starship Scanner",
            "Starship Shield Upgrade", "Starship Tech",
            "Starship Tech Slot Expansion", "Stellar Extractor Core",
            "Storage Hopper", "Stun Grenades", "Subcutaneous Access Circuit",
            "Submarine Analysis Device", "Submarine Mining Laser",
            "Submarine Weapon", "Suit Survival Power Pack",
            "Targeted Delivery Unit", "Targeted Solar Recharge Unit",
            "Temporal Delivery Unit", "Terrain Destruction Device",
            "Topographic Survey", "Unique Angling Device",
            "Vehicle-Borne Refiner", "Vertical Take-off System",
            "Weapon Precision Enhancement", "Weapon Tech",
            "Wormhole Propulsion Drive"
        },
        ["Corvette.json"] = new() { "Mission Location System" },
        ["Fish.json"] = new() { "Fragile Medusoid", "Common Fish", "Rare Fish", "Uncommon Fish" },
        ["Trade.json"] = new()
        {
            "Smuggled Goods", "Trade Goods", "Trade Goods (Construction)",
            "Trade Goods (Energy Source)", "Trade Goods (Industrial)",
            "Trade Goods (Minerals)", "Trade Goods (Scientific)",
            "Trade Goods (Technology)"
        },
        ["Raw Materials.json"] = new()
        {
            "Abundant Mineral", "Aquatic Mineral Extract", "Ashes of Despair",
            "Charged Metallic Element", "Concentrated Liquid Fuel",
            "Decayed Spacetime Remnant", "Disharmonic Metal", "Essence of Atlantid",
            "Harvested Agricultural Substance", "Harvested Substance",
            "High Energy Substance", "Highly Refined Stellar Metal",
            "Localised Earth Element", "Metallic Mineral Extract",
            "Neutron-Rich Fuel Element", "Organic Compound",
            "Processed Aquatic Mineral", "Processed Metallic Minerals",
            "Processed Subterranean Mineral", "Recessive Creature Genes",
            "Recycled Minerals", "Refined Catalytic Element",
            "Refined Organic Element", "Refined Stellar Metal: Blue",
            "Refined Stellar Metal: Green", "Refined Stellar Metal: Purple",
            "Refined Stellar Metal: Red", "Refined Stellar Metal: Yellow",
            "Salvaged Scrap", "Soul Fragment", "Subterranean Mineral",
            "Transmuted Metal", "Unrefined Catalytic Element",
            "Unrefined Organic Element", "Valuable Asteroid Mineral"
        },
    };

    // Prefix rules (only Corvette currently)
    private static readonly Dictionary<string, string[]> PrefixRules = new()
    {
        ["Corvette.json"] = new[] { "Corvette " },
    };

    private static readonly string[] JunkKeywords =
    {
        "Biggs", "Basic F", "Basic B", "Basic S", "Basic T", "Basic Legacy",
        "Wall Art", "Planet Tech", "Base Tech", "Rooms"
    };

    private static readonly HashSet<string> ShipComponentGroups = new()
    {
        "Hauler Starship Component", "Fighter Starship Component",
        "Solar Starship Component", "Explorer Starship Component",
        "Living Ship Component", "Starship Core Component"
    };

    private static readonly HashSet<string> NameFilterExemptGroups = new()
    {
        "Edible Product", "Exclusive Companion Egg",
        "Exclusive Multifunction Survival Device", "Exclusive Spacecraft",
        "Pyrotechnics Multi-Pack", "Specialist Exosuit Visuals",
        "Unlockable Armour", "Unlockable Boots",
        "Unlockable Cape", "Unlockable Companion Accessory", "Unlockable Gloves",
        "Unlockable Head", "Unlockable Helmet", "Unlockable Hood",
        "Unlockable Jetpack", "Unlockable Leg Customisation",
        "Unlockable Torso Customisation"
    };

    /// <summary>
    /// Determine which output file an item belongs to based on its Group field.
    /// Returns the filename or null to skip the item.
    /// </summary>
    public static string? CategorizeItem(Dictionary<string, object?> item)
    {
        string group = (item.GetValueOrDefault("Group")?.ToString() ?? "").Trim();
        string name = (item.GetValueOrDefault("Name")?.ToString() ?? "").Trim();
        string itemId = (item.GetValueOrDefault("Id")?.ToString() ?? "").Trim();

        if (string.IsNullOrEmpty(group)) return null;

        // Name validation (with exemptions)
        if (!ShipComponentGroups.Contains(group) && !NameFilterExemptGroups.Contains(group))
        {
            if (string.IsNullOrEmpty(name) ||
                name.StartsWith("UI_") ||
                name.StartsWith("Ui ") ||
                name == itemId ||
                (name.StartsWith("Ui") && !name.Contains('_') && name.Split(' ').Length <= 4) ||
                (name.StartsWith("Food ") && new[] { "Bug", "Pcat", "Horror", "Bjam", "Pcatbut", "Pcatgek" }.Any(x => name.Contains(x))) ||
                (group.StartsWith("Ui ") && group.EndsWith(" Sub")))
                return null;
        }

        // Junk filter
        string groupLower = group.ToLowerInvariant();
        foreach (string junk in JunkKeywords)
            if (groupLower.Contains(junk.ToLowerInvariant()))
                return null;

        string nameLower = name.ToLowerInvariant();
        string itemIdLower = itemId.ToLowerInvariant();

        // Upgrade routing (high priority)
        if (groupLower.Contains("upgrade") || nameLower.Contains("upgrade"))
            return "Upgrades.json";
        if (DeployableSalvageClassPattern.IsMatch(group))
            return "Upgrades.json";
        if (StarshipUpgradeGroups.Contains(group))
            return "Upgrades.json";

        // Starship routing
        if (!StarshipExcludedGroups.Contains(group) &&
            (StarshipComponentGroupPattern.IsMatch(group) || StarshipExactGroups.Contains(group)))
            return "Starships.json";

        // Exocraft routing
        if (groupLower.Contains("exocraft") || nameLower.Contains("exocraft") ||
            groupLower.Contains("submarine") || nameLower.Contains("submarine") ||
            groupLower.Contains("nautilon") || nameLower.Contains("nautilon") ||
            itemIdLower.StartsWith("up_veh") || itemIdLower.StartsWith("u_exo"))
            return "Exocraft.json";

        // Dynamic TechnologyModule pattern
        if (TechModuleClassPattern.IsMatch(group))
            return "Technology Module.json";

        // Exact rules
        foreach (var (filename, exactSet) in ExactRules)
            if (exactSet.Contains(group))
                return filename;

        // Prefix rules
        foreach (var (filename, prefixes) in PrefixRules)
            foreach (string prefix in prefixes)
                if (group.StartsWith(prefix, StringComparison.Ordinal))
                    return filename;

        // SPACEGUNK items belong in Raw Materials (Group is "Junk" which doesn't match exact rules)
        if (itemId.StartsWith("SPACEGUNK", StringComparison.OrdinalIgnoreCase))
            return "Raw Materials.json";

        // U_TECHPACK_ and U_TECHBOX_ items are intentionally uncategorized (kept in none.json)
        if (itemId.StartsWith("U_TECHPACK_", StringComparison.OrdinalIgnoreCase) ||
            itemId.StartsWith("U_TECHBOX_", StringComparison.OrdinalIgnoreCase))
            return null;

        // Default: valid items with unrecognized groups go to Others.json
        return "Others.json";
    }
}
