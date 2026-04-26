using NMSE.Data;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles settlement data operations including loading, saving, filtering by player ownership, and stat management.
/// </summary>
internal static class SettlementLogic
{
    // Stat order: MaxPopulation=0, Happiness=1, Production=2, Upkeep=3, Sentinels=4, Debt=5, Alert=6, BugAttack=7
    // Max values from database; min values in StatMinValues
    /// <summary>Maximum values for each settlement stat (MaxPopulation, Happiness, Production, Upkeep, Sentinels, Debt, Alert, BugAttack). Used for color warning indication and not clamping.</summary>
    internal static readonly int[] StatMaxValues = { 175, 180, 1500000, 1000000, 100, 10000000, 1000, 1000 };
    /// <summary>Minimum values for each settlement stat. Used for color warning indication and not clamping.</summary>
    internal static readonly int[] StatMinValues = { 0, -30, 0, 0, 0, 0, 0, 0 };
    /// <summary>Display labels for each settlement stat.</summary>
    internal static readonly string[] StatLabels = { "Max Population", "Happiness", "Production", "Upkeep", "Sentinels", "Debt", "Alert", "Bug Attack" };
    /// <summary>Maximum value for the top-level Population field (hard cap).</summary>
    internal const int PopulationMax = 400;
    /// <summary>Soft-cap value for the Population field used for colour warning indication (matches original game default max).</summary>
    internal const int PopulationSoftMax = 200;
    /// <summary>Total number of tracked settlement stats.</summary>
    internal const int StatCount = 8;
    /// <summary>Maximum allowed production output amount.</summary>
    internal const int ProductionMaxAmount = 999;
    /// <summary>Maximum number of settlement slots in the save file.</summary>
    internal const int MaxSettlementSlots = 100;

    /// <summary>
    /// Item names that are valid settlement production outputs (from game data).
    /// Used to filter the production item picker dialog.
    /// </summary>
    internal static readonly string[] AllowedProductionNames =
    {
        "Amino Chamber", "Atlantideum", "Cactus Flesh", "Carbon Nanotubes",
        "Comet Droplets", "Convergence Cube", "Cryogenic chamber", "Decomm. Circuits",
        "Dirt", "Enriched Carbon", "Faecium", "Frigate Fuel", "Frost Crystal",
        "Fungal Mould", "Fusion Accelerant", "Fusion Core", "Gamma Root",
        "Gek Relic", "GekNip", "Glass", "Heat Capacitor", "Hermetic Seal",
        "Hot Ice", "Hydraulic Wiring", "Inverted Mirror", "Ion Battery",
        "Ion Capacitor", "Kelp Sac", "Korvax Casing", "Life Support Gel",
        "Living Glass", "Magnetic Resonator", "Marrow Bulb", "Metal Plating",
        "Nanotube Crate", "Neural Duct", "Nitrogen", "Nitrogen Salt",
        "Optical Solvent", "Organic Catalyst", "Portable Reactor", "Pugneum",
        "Quantum Computer", "Quantum Processor", "Radiant Shard", "Radon",
        "Re-latticed Arc Crystal", "Rusted Metal", "Salvaged Glass",
        "Semiconductor", "Solanium", "Solar Mirror", "Star Bulb",
        "Starship Launch Fuel", "Sulphurine", "Superconduct. Fibre",
        "Teleport Coordinator", "Thermic Condensate", "Vector Compressors",
        "Vy'keen Dagger", "Vy'keen Effigy", "Warp Cell", "Warp Hypercore"
    };

    internal static readonly string[] DecisionTypes =
    {
        "None", "StrangerVisit", "Policy", "NewBuilding", "BuildingChoice",
        "Conflict", "Request", "BlessingPerkRelated", "JobPerkRelated",
        "ProcPerkRelated", "UpgradeBuilding", "UpgradeBuildingChoice"
    };

    internal static readonly Dictionary<string, string> DecisionTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["None"] = "settlement.decision_none",
        ["StrangerVisit"] = "settlement.decision_stranger_visit",
        ["Policy"] = "settlement.decision_policy",
        ["NewBuilding"] = "settlement.decision_new_building",
        ["BuildingChoice"] = "settlement.decision_building_choice",
        ["Conflict"] = "settlement.decision_conflict",
        ["Request"] = "settlement.decision_request",
        ["BlessingPerkRelated"] = "settlement.decision_blessing_perk",
        ["JobPerkRelated"] = "settlement.decision_job_perk",
        ["ProcPerkRelated"] = "settlement.decision_proc_perk",
        ["UpgradeBuilding"] = "settlement.decision_upgrade_building",
        ["UpgradeBuildingChoice"] = "settlement.decision_upgrade_choice",
    };

    /// <summary>Internal save values for settlement alien races.</summary>
    internal static readonly string[] AlienRaces =
    {
        "Traders", "Warriors", "Explorers", "Robots", "Atlas",
        "Diplomats", "Exotics", "None", "Builders"
    };

    /// <summary>Maps internal race IDs to user-friendly display names.</summary>
    internal static readonly Dictionary<string, string> AlienRaceDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Traders"] = "Gek",
        ["Warriors"] = "Vy'keen",
        ["Explorers"] = "Korvax",
        ["Robots"] = "Robots",
        ["Atlas"] = "Atlas",
        ["Diplomats"] = "Diplomats",
        ["Exotics"] = "Exotics",
        ["None"] = "None",
        ["Builders"] = "Autophage",
    };

    /// <summary>Localisation keys for alien race display names.</summary>
    internal static readonly Dictionary<string, string> AlienRaceLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Traders"] = "settlement.race_gek",
        ["Warriors"] = "settlement.race_vykeen",
        ["Explorers"] = "settlement.race_korvax",
        ["Robots"] = "settlement.race_robots",
        ["Atlas"] = "settlement.race_atlas",
        ["Diplomats"] = "settlement.race_diplomats",
        ["Exotics"] = "settlement.race_exotics",
        ["None"] = "settlement.race_none",
        ["Builders"] = "settlement.race_autophage",
    };

    /// <summary>Maximum number of building state slots.</summary>
    internal const int BuildingStateSlotCount = 48;

    /// <summary>
    /// Builds a dictionary of allowed production items (ID -> Name) by matching
    /// <see cref="AllowedProductionNames"/> against the game item database.
    /// </summary>
    internal static Dictionary<string, string> BuildAllowedProductionItems(Data.GameItemDatabase database)
    {
        var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nameSet = new HashSet<string>(AllowedProductionNames, StringComparer.OrdinalIgnoreCase);

        foreach (var item in database.Items.Values)
        {
            if (nameSet.Contains(item.Name))
                allowed[item.Id] = item.Name;
        }

        return allowed;
    }

    /// <summary>
    /// Filters the settlements array to only include settlements owned by the current player.
    /// Matching is done by comparing LID, UID, or USN from the discovery owners.
    /// </summary>
    /// <param name="saveData">The top-level save data JSON object containing CommonStateData.</param>
    /// <param name="playerState">The player state JSON object.</param>
    /// <param name="settlements">The JSON array of all settlements.</param>
    /// <returns>A list of indices into the settlements array that belong to this player.</returns>
    internal static List<int> FilterSettlements(JsonObject saveData, JsonObject playerState, JsonArray settlements)
    {
        var filteredIndices = new List<int>();

        // Get the player's owner identifiers from CommonStateData.UsedDiscoveryOwnersV2
        // Note: UsedDiscoveryOwnersV2 is a direct child of CommonStateData, NOT under SeasonData
		var playerLIDs = new List<string>();
		var playerUIDs = new List<string>();
		var playerUSNs = new List<string>();

		try
        {
            var commonState = saveData.GetObject("CommonStateData");
            var owners = commonState?.GetArray("UsedDiscoveryOwnersV2");

			if (owners != null && owners.Length > 0)
			{
				for (int i = 0; i < owners.Length; i++)
				{
					playerLIDs.Add(owners.GetObject(i)?.GetString("LID") ?? "");
					playerUIDs.Add(owners.GetObject(i)?.GetString("UID") ?? "");
					playerUSNs.Add(owners.GetObject(i)?.GetString("USN") ?? "");
				}
			}
        }
        catch { }

		// Check if we have any valid identifiers to match against
		bool hasIdentifier = playerLIDs.Count > 0 || playerUIDs.Count > 0 || playerUSNs.Count > 0;

		// Filter settlements by matching Owner LIDs, UIDs, or USNs
		// (any IDs in Owners including multiple same platform IDs in one save, etc.)
		if (hasIdentifier)
        {
            // Convert to HashSets for O(1) lookup instead of O(n) loops
            var lidSet = new HashSet<string>(playerLIDs.Where(lid => !string.IsNullOrEmpty(lid)), StringComparer.Ordinal);
            var uidSet = new HashSet<string>(playerUIDs.Where(uid => !string.IsNullOrEmpty(uid)), StringComparer.Ordinal);
            var usnSet = new HashSet<string>(playerUSNs.Where(usn => !string.IsNullOrEmpty(usn)), StringComparer.Ordinal);

            for (int i = 0; i < settlements.Length; i++)
            {
                try
                {
                    var settlement = settlements.GetObject(i);
                    var owner = settlement?.GetObject("Owner");
                    if (owner == null) continue;

                    // Get owner identifiers once
                    var ownerLid = owner.GetString("LID");
                    var ownerUid = owner.GetString("UID");
                    var ownerUsn = owner.GetString("USN");

                    // Check if any identifier matches using O(1) HashSet lookup with short circuit eval
                    if ((!string.IsNullOrEmpty(ownerLid) && lidSet.Contains(ownerLid)) ||
                        (!string.IsNullOrEmpty(ownerUid) && uidSet.Contains(ownerUid)) ||
                        (!string.IsNullOrEmpty(ownerUsn) && usnSet.Contains(ownerUsn)))
                    {
                        filteredIndices.Add(i);
                    }
                }
                catch { }
            }
        }

        return filteredIndices;
    }

    /// <summary>
    /// Loads settlement data from a settlement JSON object for display and editing.
    /// </summary>
    /// <param name="settlement">The settlement JSON object.</param>
    /// <returns>A populated <see cref="SettlementData"/> instance.</returns>
    internal static SettlementData LoadSettlementData(JsonObject settlement)
    {
        var data = new SettlementData();

        data.Name = settlement.GetString("Name") ?? "";

        var ownerObj = settlement.GetObject("Owner");
        data.OwnerUsn = ownerObj?.GetString("USN") ?? "";
        data.OwnerUid = ownerObj?.GetString("UID") ?? "";

        data.SeedValue = settlement.GetString("SeedValue") ?? "";

        var statsArr = settlement.GetArray("Stats");
        if (statsArr != null)
        {
            for (int i = 0; i < StatCount && i < statsArr.Length; i++)
            {
                try
                {
                    int raw = statsArr.GetInt(i);
                    data.RawStats[i] = raw;
                    data.Stats[i] = raw; // Unclamped - UI shows raw values with colour coding
                }
                catch { data.Stats[i] = 0; }
            }
        }

        // Read top-level Population field separately (NMS 5.70+ "Beacon" saves)
        if (settlement.Contains("Population"))
        {
            try
            {
                int pop = settlement.GetInt("Population");
                data.HasPopulationKey = true;
                data.RawPopulation = pop;
                data.Population = Math.Clamp(pop, 0, PopulationMax);
            }
            catch
            {
                // Key exists but value is unparseable - treat as missing
            }
        }

        // Decision type
        try
        {
            var decisionObj = settlement.GetObject("PendingJudgementType");
            string decType = decisionObj?.GetString("SettlementJudgementType") ?? "None";
            data.DecisionTypeIndex = Array.IndexOf(DecisionTypes, decType);
            if (data.DecisionTypeIndex < 0) data.DecisionTypeIndex = 0;
        }
        catch { data.DecisionTypeIndex = 0; }

        // Last decision time
        try
        {
            long unixTime = settlement.GetLong("LastJudgementTime");
            if (unixTime > 0)
                data.LastDecisionTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToLocalTime().DateTime;
        }
        catch { }

        // Alien Race
        try
        {
            var raceObj = settlement.GetObject("Race");
            data.AlienRace = raceObj?.GetString("AlienRace") ?? "None";
        }
        catch { data.AlienRace = "None"; }

        // Timestamp fields - stored as longs (unix timestamps in seconds)
        try { data.LastBugAttackChangeTime = settlement.GetLong("LastBugAttackChangeTime"); } catch { }
        try { data.LastAlertChangeTime = settlement.GetLong("LastAlertChangeTime"); } catch { }
        try { data.LastDebtChangeTime = settlement.GetLong("LastDebtChangeTime"); } catch { }
        try { data.LastUpkeepDebtCheckTime = settlement.GetLong("LastUpkeepDebtCheckTime"); } catch { }
        try { data.LastPopulationChangeTime = settlement.GetLong("LastPopulationChangeTime"); } catch { }

        // Mission seed & start time
        try { data.MiniMissionSeed = settlement.GetInt("MiniMissionSeed"); } catch { }
        try { data.MiniMissionStartTime = settlement.GetLong("MiniMissionStartTime"); } catch { }

        // Building states
        var buildArr = settlement.GetArray("BuildingStates");
        if (buildArr != null)
        {
            data.HasBuildingStates = true;
            for (int i = 0; i < BuildingStateSlotCount && i < buildArr.Length; i++)
            {
                try
                {
                    int raw = buildArr.GetInt(i);
                    data.RawBuildingStates[i] = raw;
                    data.BuildingStates[i] = raw;
                }
                catch { }
            }
        }

        return data;
    }

    /// <summary>
    /// Saves settlement data back to the settlement JSON object.
    /// When raw values are provided, stats are only written if the user actually changed
    /// them from their clamped display value - this preserves externally-edited values
    /// that fall outside the editor's UI range.
    /// </summary>
    /// <param name="settlement">The settlement JSON object to update.</param>
    /// <param name="values">The values to write.</param>
    internal static void SaveSettlementData(JsonObject settlement, SettlementSaveValues values)
    {
        settlement.Set("Name", values.Name);

        if (!string.IsNullOrEmpty(values.SeedValue))
            settlement.Set("SeedValue", values.SeedValue);

        var statsArr = settlement.GetArray("Stats");
        if (statsArr != null)
        {
            for (int i = 0; i < StatCount && i < statsArr.Length; i++)
            {
                if (ShouldWriteStat(i, values.Stats[i], values.RawStats))
                    statsArr.Set(i, values.Stats[i]);
            }
        }

        // NMS 5.70+ also has a separate Population field - write only if present in save
        if (values.HasPopulationKey)
        {
            bool shouldWritePop = true;
            if (values.RawPopulation.HasValue)
            {
                int rawPop = values.RawPopulation.Value;
                int clampedPop = Math.Clamp(rawPop, 0, PopulationMax);
                shouldWritePop = values.Population != clampedPop; // Only write if user actually changed it
            }
            if (shouldWritePop)
            {
                try { settlement.Set("Population", values.Population); } catch { }
            }
        }

        // Decision type
        if (values.DecisionTypeIndex >= 0 && values.DecisionTypeIndex < DecisionTypes.Length)
        {
            try
            {
                var decisionObj = settlement.GetObject("PendingJudgementType");
                decisionObj?.Set("SettlementJudgementType", DecisionTypes[values.DecisionTypeIndex]);
            }
            catch { }
        }

        // Last decision time
        if (values.LastDecisionTime.HasValue)
        {
            try
            {
                long unixTime = ((DateTimeOffset)values.LastDecisionTime.Value.ToUniversalTime()).ToUnixTimeSeconds();
                settlement.Set("LastJudgementTime", unixTime);
            }
            catch { }
        }

        // Alien Race
        try
        {
            var raceObj = settlement.GetObject("Race");
            if (raceObj != null)
                raceObj.Set("AlienRace", values.AlienRace);
        }
        catch { }

        // Timestamp fields
        try { settlement.Set("LastBugAttackChangeTime", values.LastBugAttackChangeTime); } catch { }
        try { settlement.Set("LastAlertChangeTime", values.LastAlertChangeTime); } catch { }
        try { settlement.Set("LastDebtChangeTime", values.LastDebtChangeTime); } catch { }
        try { settlement.Set("LastUpkeepDebtCheckTime", values.LastUpkeepDebtCheckTime); } catch { }
        try { settlement.Set("LastPopulationChangeTime", values.LastPopulationChangeTime); } catch { }

        // Mission seed & start time
        try { settlement.Set("MiniMissionSeed", values.MiniMissionSeed); } catch { }
        try { settlement.Set("MiniMissionStartTime", values.MiniMissionStartTime); } catch { }

        // Building states - only write changed values (Path E preservation)
        if (values.HasBuildingStates)
        {
            var buildArr = settlement.GetArray("BuildingStates");
            if (buildArr != null)
            {
                for (int i = 0; i < BuildingStateSlotCount && i < buildArr.Length; i++)
                {
                    bool shouldWrite = true;
                    if (values.RawBuildingStates != null)
                    {
                        // Only write if user actually changed the value
                        shouldWrite = values.BuildingStates[i] != values.RawBuildingStates[i];
                    }
                    if (shouldWrite)
                        buildArr.Set(i, values.BuildingStates[i]);
                }
            }
        }
    }

    /// <summary>
    /// Returns true if the stat at <paramref name="index"/> should be written to JSON.
    /// When raw values are available, skips the write if the UI value matches the
    /// raw value - i.e. the user didn't change it. Stats are unclamped in the UI
    /// so this is a direct comparison.
    /// </summary>
    internal static bool ShouldWriteStat(int index, int uiValue, int[]? rawStats)
    {
        if (rawStats == null || index < 0 || index >= StatCount) return true;
        return uiValue != rawStats[index]; // Write only if user actually changed it
    }

    /// <summary>
    /// Removes a settlement from the array by index.
    /// </summary>
    /// <param name="settlements">The SettlementStatesV2 JSON array.</param>
    /// <param name="index">The zero-based index of the settlement to remove.</param>
    internal static void RemoveSettlement(JsonArray settlements, int index)
    {
        if (index < 0 || index >= settlements.Length) return;
        settlements.RemoveAt(index);
    }

    /// <summary>
    /// Finds the best target index for importing a settlement.
    /// Returns -1 to signal a new entry should be appended, -2 if no slot is available,
    /// or a valid index to overwrite.
    /// </summary>
    /// <param name="settlements">The SettlementStatesV2 JSON array.</param>
    /// <param name="selectedDataIndex">The currently-selected settlement array index, or -1 if none.</param>
    /// <returns>
    /// > = 0 : overwrite that index.
    /// -1 : append a new entry (spare capacity exists).
    /// -2 : array full and no selection – caller must ask the user to choose.
    /// </returns>
    internal static int FindImportTargetIndex(JsonArray settlements, int selectedDataIndex)
    {
        // If there's a selected settlement, overwrite it
        if (selectedDataIndex >= 0 && selectedDataIndex < settlements.Length)
            return selectedDataIndex;

        // If spare capacity, append
        if (settlements.Length < MaxSettlementSlots)
            return -1;

        // Array full and no selection
        return -2;
    }

    /// <summary>
    /// Holds loaded settlement data for display and editing in the UI.
    /// </summary>
    internal sealed class SettlementData
    {
        /// <summary>The settlement name.</summary>
        public string Name { get; set; } = "";
        /// <summary>The owner's USN (username) identifier.</summary>
        public string OwnerUsn { get; set; } = "";
        /// <summary>The owner's UID identifier.</summary>
        public string OwnerUid { get; set; } = "";
        /// <summary>The settlement's procedural seed value.</summary>
        public string SeedValue { get; set; } = "";
        /// <summary>Array of settlement stat values (clamped for UI display) indexed by stat type.</summary>
        public int[] Stats { get; set; } = new int[StatCount];
        /// <summary>Raw (unclamped) stat values read from JSON at load time. Used to
        /// detect whether the user changed a value from its clamped display.</summary>
        public int[] RawStats { get; set; } = new int[StatCount];
        /// <summary>The top-level Population value (clamped for UI display). Separate from Stats[0] (MaxPopulation).</summary>
        public int Population { get; set; }
        /// <summary>Raw (unclamped) population value read from JSON.</summary>
        public int RawPopulation { get; set; }
        /// <summary>Whether the save had a top-level Population key (NMS 5.70+ "Beacon" saves).</summary>
        public bool HasPopulationKey { get; set; }
        /// <summary>Index into <see cref="DecisionTypes"/> for the pending decision.</summary>
        public int DecisionTypeIndex { get; set; }
        /// <summary>The timestamp of the last settlement judgement decision.</summary>
        public DateTime? LastDecisionTime { get; set; }
        /// <summary>The alien race internal ID (e.g. "Traders", "Warriors").</summary>
        public string AlienRace { get; set; } = "None";
        /// <summary>Timestamp for last bug attack change.</summary>
        public long LastBugAttackChangeTime { get; set; }
        /// <summary>Timestamp for last alert change.</summary>
        public long LastAlertChangeTime { get; set; }
        /// <summary>Timestamp for last debt change.</summary>
        public long LastDebtChangeTime { get; set; }
        /// <summary>Timestamp for last upkeep debt check.</summary>
        public long LastUpkeepDebtCheckTime { get; set; }
        /// <summary>Timestamp for last population change.</summary>
        public long LastPopulationChangeTime { get; set; }
        /// <summary>Mission seed (32-bit integer).</summary>
        public int MiniMissionSeed { get; set; }
        /// <summary>Mission start time (unix timestamp).</summary>
        public long MiniMissionStartTime { get; set; }
        /// <summary>Building state values (48 slots).</summary>
        public int[] BuildingStates { get; set; } = new int[BuildingStateSlotCount];
        /// <summary>Raw building states read from JSON for preservation.</summary>
        public int[] RawBuildingStates { get; set; } = new int[BuildingStateSlotCount];
        /// <summary>Whether the settlement has BuildingStates in JSON.</summary>
        public bool HasBuildingStates { get; set; }
    }
    internal sealed class SettlementSaveValues
    {
        /// <summary>The settlement name to set.</summary>
        public string Name { get; set; } = "";
        /// <summary>The seed value to set.</summary>
        public string SeedValue { get; set; } = "";
        /// <summary>Array of stat values to write (from UI NUDs).</summary>
        public int[] Stats { get; set; } = new int[StatCount];
        /// <summary>Raw (unclamped) stat values loaded from JSON. When provided,
        /// each stat is only written if the UI value differs from what clamping would produce.</summary>
        public int[]? RawStats { get; set; }
        /// <summary>The top-level Population value to write.</summary>
        public int Population { get; set; }
        /// <summary>Raw (unclamped) population value for comparison.</summary>
        public int? RawPopulation { get; set; }
        /// <summary>Whether the save had a top-level Population key (NMS 5.70+ "Beacon" saves).</summary>
        public bool HasPopulationKey { get; set; }
        /// <summary>Index into <see cref="DecisionTypes"/> for the desired pending decision.</summary>
        public int DecisionTypeIndex { get; set; }
        /// <summary>The last decision timestamp to set, or <c>null</c> to leave unchanged.</summary>
        public DateTime? LastDecisionTime { get; set; }
        /// <summary>The alien race internal ID to set.</summary>
        public string AlienRace { get; set; } = "None";
        /// <summary>Timestamps to set.</summary>
        public long LastBugAttackChangeTime { get; set; }
        public long LastAlertChangeTime { get; set; }
        public long LastDebtChangeTime { get; set; }
        public long LastUpkeepDebtCheckTime { get; set; }
        public long LastPopulationChangeTime { get; set; }
        /// <summary>Mission seed.</summary>
        public int MiniMissionSeed { get; set; }
        /// <summary>Mission start time.</summary>
        public long MiniMissionStartTime { get; set; }
        /// <summary>Building states to save.</summary>
        public int[] BuildingStates { get; set; } = new int[BuildingStateSlotCount];
        /// <summary>Raw building states for preservation.</summary>
        public int[]? RawBuildingStates { get; set; }
        /// <summary>Whether the settlement has BuildingStates.</summary>
        public bool HasBuildingStates { get; set; }
    }

    /// <summary>
    /// Decodes settlement BuildingStates int32 values as bit-flag composites.
    /// <para>Bit layout (verified against 29 empirical data points, int32 as canonical):</para>
    /// <list type="bullet">
    /// <item>Bits 0–6 (7): Initial construction phase flags</item>
    /// <item>Bits 7–9 (3): Reserved</item>
    /// <item>Bits 10–19 (10): Upgrade construction sub-phase flags</item>
    /// <item>Bits 20–25 (6): Tier progression (2 bits per B/A/S: Started + Confirmed)</item>
    /// <item>Bit 26: Class system active</item>
    /// <item>Bit 27: B-class unveiled ("Fancy" visual)</item>
    /// <item>Bit 28: A-class unveiled</item>
    /// <item>Bit 29: S-class unveiled</item>
    /// <item>Bits 30–31 (2): Unknown/Reserved</item>
    /// </list>
    /// </summary>
    internal static class SettlementBuildingState
    {
        // --- Masks ---
        /// <summary>Bits 0–6: seven initial construction phase flags.</summary>
        public const int InitConstructionMask = 0x0000_007F;
        /// <summary>Bits 10–19: ten upgrade sub-phase flags.</summary>
        public const int UpgradeProgressMask  = 0x000F_FC00;
        /// <summary>Bits 20–25: six tier-progression bits (2 per B/A/S).</summary>
        public const int TierProgressionMask  = 0x03F0_0000;
        /// <summary>Bits 26–29: system/arrival flags.</summary>
        public const int ArrivalFlagsMask     = 0x3C00_0000;

        // --- Bit positions ---
        public const int Bit_B_Started         = 20;
        public const int Bit_B_Confirmed       = 21;
        public const int Bit_A_Started         = 22;
        public const int Bit_A_Confirmed       = 23;
        public const int Bit_S_Started         = 24;
        public const int Bit_S_Confirmed       = 25;
        public const int Bit_ClassSystemActive = 26;
        public const int Bit_B_Arrived         = 27;
        public const int Bit_A_Arrived         = 28;
        public const int Bit_S_Arrived         = 29;

        // --- Bit-field region sizes ---
        /// <summary>Number of initial construction phase bits (0–6).</summary>
        public const int InitPhaseCount    = 7;
        /// <summary>Number of upgrade sub-phase bits (10–19).</summary>
        public const int UpgradePhaseCount = 10;
        /// <summary>Number of tier progression bits (20–25, 2 per B/A/S).</summary>
        public const int TierBitCount      = 6;
        /// <summary>Number of system/arrival flag bits (26–29).</summary>
        public const int FlagBitCount      = 4;

        public static bool IsEmpty(int state) => state == 0;

        /// <summary>Returns true for the initial C-class construction phase values (bits 0–6 only, no higher bits).</summary>
        public static bool IsInitialConstruction(int state) => state > 0 && state <= 0x7F && (state & ~InitConstructionMask) == 0;

        public static int GetInitConstruction(int state) => state & InitConstructionMask;
        public static int GetUpgradeProgress(int state) => (state & UpgradeProgressMask) >> 10;
        public static int GetTierProgression(int state) => (state & TierProgressionMask) >> 20;
        public static int GetArrivalFlags(int state) => (state & ArrivalFlagsMask) >> 26;
        public static bool GetBit(int state, int bit) => ((state >> bit) & 1) != 0;

        /// <summary>Count of set bits in the initial construction field (bits 0–6).</summary>
        public static int InitConstructionCount(int state) =>
            System.Numerics.BitOperations.PopCount((uint)(state & InitConstructionMask));

        /// <summary>Count of set bits in the upgrade progress field (bits 10–19).</summary>
        public static int UpgradeProgressCount(int state) =>
            System.Numerics.BitOperations.PopCount((uint)(state & UpgradeProgressMask));

        /// <summary>
        /// Determines the building class and state from a raw building state integer.
        /// Uses a priority-ordered check (highest class first) per the verified analysis.
        /// </summary>
        /// <returns>A tuple of (Class, State) localisation keys.</returns>
        public static (string ClassLocKey, string StateLocKey) DetermineClassAndState(int rawState)
        {
            bool bit29 = GetBit(rawState, Bit_S_Arrived);
            bool bit28 = GetBit(rawState, Bit_A_Arrived);
            bool bit27 = GetBit(rawState, Bit_B_Arrived);
            bool bit25 = GetBit(rawState, Bit_S_Confirmed);
            bool bit24 = GetBit(rawState, Bit_S_Started);
            bool bit23 = GetBit(rawState, Bit_A_Confirmed);
            bool bit22 = GetBit(rawState, Bit_A_Started);
            bool bit21 = GetBit(rawState, Bit_B_Confirmed);
            bool bit20 = GetBit(rawState, Bit_B_Started);

            if (bit29 && bit25) return ("settlement.bs_class_s", "settlement.bs_state_complete");
            if (bit29 && bit24) return ("settlement.bs_class_a_to_s", "settlement.bs_state_awaiting_unveil");
            if (bit24 && !bit29) return ("settlement.bs_class_a_to_s", "settlement.bs_state_upgrade_0");
            if (bit28 && bit23) return ("settlement.bs_class_a", "settlement.bs_state_complete");
            if (bit28 && bit22) return ("settlement.bs_class_b_to_a", "settlement.bs_state_awaiting_unveil");
            if (bit22 && !bit28 && !bit23) return ("settlement.bs_class_b_to_a", "settlement.bs_state_upgrade_0");
            if (bit27 && bit21) return ("settlement.bs_class_b", "settlement.bs_state_complete");
            if (bit27 && bit20) return ("settlement.bs_class_c_to_b", "settlement.bs_state_awaiting_unveil");
            if (bit20 && !bit27 && !bit21) return ("settlement.bs_class_c_to_b", "settlement.bs_state_upgrade_0");
            return ("settlement.bs_class_c", "settlement.bs_state_complete");
        }

        /// <summary>
        /// Returns a compact 3-line description for a building state slot.
        /// </summary>
        public static string GetBuildingSlotDescription(int rawState)
        {
            if (IsEmpty(rawState))
                return UiStrings.Get("settlement.bs_empty");

            if (IsInitialConstruction(rawState))
            {
                int count = InitConstructionCount(rawState);
                return UiStrings.Format("settlement.bs_init_construction", count);
            }

            var (classKey, stateKey) = DetermineClassAndState(rawState);
            string classLabel = UiStrings.Get(classKey);
            string stateLabel = UiStrings.Get(stateKey);
            int initCount = InitConstructionCount(rawState);
            int upgradeCount = UpgradeProgressCount(rawState);

            string rawHex = UiStrings.Format("settlement.bs_decode_raw", rawState);
            string l1 = $"{classLabel} ({stateLabel}) {rawHex}";
            string l2 = UiStrings.Format("settlement.bs_detail_line", initCount, InitPhaseCount, upgradeCount, UpgradePhaseCount);
            //string l3 = UiStrings.Format("settlement.bs_status_line", stateLabel);

            //return $"{l1}\n{l2}\n{l3}";
            return $"{l1}\n{l2}";
        }

        /// <summary>
        /// Returns a detailed multi-line bit-field breakdown for a building state value.
        /// Shows class/state, construction/upgrade progress, tier progression, system/arrival flags, and raw hex value.
        /// </summary>
        public static string GetDetailedBitFieldDescription(int rawState)
        {
            if (IsEmpty(rawState))
                return UiStrings.Get("settlement.bs_empty");

            var (classKey, stateKey) = DetermineClassAndState(rawState);
            string classLabel = UiStrings.Get(classKey);
            string stateLabel = UiStrings.Get(stateKey);

            int initCount = InitConstructionCount(rawState);
            int upgradeCount = UpgradeProgressCount(rawState);

            string TierStatus(int startBit, int confirmBit) =>
                GetBit(rawState, confirmBit) ? UiStrings.Get("settlement.bs_tier_confirmed") :
                GetBit(rawState, startBit)   ? UiStrings.Get("settlement.bs_tier_started") :
                                               UiStrings.Get("settlement.bs_tier_not_started");

            string yes = UiStrings.Get("settlement.bs_flag_yes");
            string no  = UiStrings.Get("settlement.bs_flag_no");
            string Flag(int bit) => GetBit(rawState, bit) ? yes : no;

            string rawHex = UiStrings.Format("settlement.bs_decode_raw", rawState);
            string l1 = $"{classLabel} ({stateLabel}) {rawHex}";
            string l2 = UiStrings.Format("settlement.bs_decode_init", initCount, InitPhaseCount) + " | " +
                        UiStrings.Format("settlement.bs_decode_upgrade", upgradeCount, UpgradePhaseCount);
            string l3 = UiStrings.Format("settlement.bs_decode_tier_b", TierStatus(Bit_B_Started, Bit_B_Confirmed)) + " | " +
                        UiStrings.Format("settlement.bs_decode_tier_a", TierStatus(Bit_A_Started, Bit_A_Confirmed)) + " | " +
                        UiStrings.Format("settlement.bs_decode_tier_s", TierStatus(Bit_S_Started, Bit_S_Confirmed));
            string l4 = UiStrings.Format("settlement.bs_decode_class_active", Flag(Bit_ClassSystemActive)) + " | " +
                        UiStrings.Format("settlement.bs_decode_b_arrived", Flag(Bit_B_Arrived)) + " | " +
                        UiStrings.Format("settlement.bs_decode_a_arrived", Flag(Bit_A_Arrived)) + " | " +
                        UiStrings.Format("settlement.bs_decode_s_arrived", Flag(Bit_S_Arrived));
            string l5 = UiStrings.Format("settlement.bs_status_line", stateLabel);

            return $"{l1}\n{l2}\n{l3}\n{l4}\n{l5}";
        }
    }
}
