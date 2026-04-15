using NMSE.Core;

namespace NMSE.UI.Panels;

partial class MilestonePanel
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        DoubleBuffered = true;
        SuspendLayout();

        _tabControl = new DoubleBufferedTabControl { Dock = DockStyle.Fill };

        // === Tab 1: Main Stats (Milestones | Kills | Alien Factions | Guilds) ===
        var tab1 = new TabPage("Main Stats");
        var scroll1 = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var section1 = CreateFourColumnSection();
        var s1c1 = GetColumnPanel(section1, 0);
        var s1c2 = GetColumnPanel(section1, 1);
        var s1c3 = GetColumnPanel(section1, 2);
        var s1c4 = GetColumnPanel(section1, 3);

        // Column 1: Milestones
        AddSectionTitle(s1c1, "Milestones", "milestone.section_milestones");
        AddField(s1c1, "milestone.on_foot_exploration", "^DIST_WALKED");
        AddField(s1c1, "milestone.alien_encounters", "^ALIENS_MET");
        AddField(s1c1, "milestone.words_collected", "^WORDS_LEARNT");
        AddField(s1c1, "milestone.most_units_accrued", "^MONEY");
        AddField(s1c1, "milestone.ships_destroyed", "^ENEMIES_KILLED");
        AddField(s1c1, "milestone.sentinels_destroyed", "^SENTINEL_KILLS");
        AddField(s1c1, "milestone.space_exploration", "^DIST_WARP");
        AddField(s1c1, "milestone.planet_zoology_scanned", "^DISC_ALL_CREATU");

        // Column 2: Kills
        AddSectionTitle(s1c2, "Kills", "milestone.section_kills");
        AddField(s1c2, "milestone.ammo_fired", "^AMMO_FIRED");
        AddField(s1c2, "milestone.predators", "^PREDS_KILLED");
        AddField(s1c2, "milestone.sentinel_drones", "^DRONES_KILLED");
        AddField(s1c2, "milestone.sentinel_quads", "^QUADS_KILLED");
        AddField(s1c2, "milestone.sentinel_walkers", "^WALKERS_KILLED");
        AddField(s1c2, "milestone.pirates", "^PIRATES_KILLED");
        AddField(s1c2, "milestone.police", "^POLICE_KILLED");
        AddField(s1c2, "milestone.civilian_freighters", "^CIV_FREI_KILLS");
        AddField(s1c2, "milestone.fiends", "^FIENDS_KILLED");
        AddField(s1c2, "milestone.fish_killed", "^FISH_KILLS");
        AddField(s1c2, "milestone.flora_killed", "^FLORA_KILLED");
        AddField(s1c2, "milestone.grubs", "^GRUBS_KILLED");
        AddField(s1c2, "milestone.jellyfish_boss_1", "^JELLYBOSS");
        AddField(s1c2, "milestone.kills_in_mech", "^KILLS_IN_MECH");
        AddField(s1c2, "milestone.mechs", "^MECHS_KILLED");
        AddField(s1c2, "milestone.miniworms", "^MINIWORM_KILL");
        AddField(s1c2, "milestone.pirate_freighters_destroyed", "^PIR_FREI_WINS");
        AddField(s1c2, "milestone.queens", "^QUEENS_KILLED");
        AddField(s1c2, "milestone.road_kill", "^ROAD_KILL");
        AddField(s1c2, "milestone.jellyfish_boss_2", "^S20_JELLYBOSS");
        AddField(s1c2, "milestone.sentinel_freighters", "^SENTFREI_KILLED");
        AddField(s1c2, "milestone.spiders", "^SPIDERS_KILLED");
        AddField(s1c2, "milestone.spookfiend_boss", "^SPOOKBOSS");
        AddField(s1c2, "milestone.spookfiend_juice", "^SPOOK_JUICE");
        AddField(s1c2, "milestone.spookfiends", "^SPOOK_KILLS");
        AddField(s1c2, "milestone.stone_guardians", "^STONE_KILLS");
        AddField(s1c2, "milestone.traders_killed", "^TRADERS_KILLED");

        // Column 3: Alien Factions
        AddSectionTitle(s1c3, "Alien Factions", "milestone.section_alien_factions");
        AddSectionTitle(s1c3, "Gek", "milestone.gek");
        AddField(s1c3, "milestone.standing", "^TRA_STANDING");
        AddField(s1c3, "milestone.missions", "^TDONE_MISSIONS");
        AddField(s1c3, "milestone.systems_visited", "^TSEEN_SYSTEMS");
        AddField(s1c3, "milestone.gek_met", "^TRA_MET");
        AddSectionTitle(s1c3, "Vy'keen", "milestone.vykeen");
        AddField(s1c3, "milestone.standing", "^WAR_STANDING");
        AddField(s1c3, "milestone.missions", "^WDONE_MISSIONS");
        AddField(s1c3, "milestone.systems_visited", "^WSEEN_SYSTEMS");
        AddField(s1c3, "milestone.vykeen_met", "^WAR_MET");
        AddSectionTitle(s1c3, "Korvax", "milestone.korvax");
        AddField(s1c3, "milestone.standing", "^EXP_STANDING");
        AddField(s1c3, "milestone.missions", "^EDONE_MISSIONS");
        AddField(s1c3, "milestone.systems_visited", "^ESEEN_SYSTEMS");
        AddField(s1c3, "milestone.korvax_met", "^EXP_MET");
        AddSectionTitle(s1c3, "Autophage", "milestone.autophage");
        AddField(s1c3, "milestone.standing", "^BUI_STANDING");
        AddField(s1c3, "milestone.missions", "^BDONE_MISSIONS");
        AddField(s1c3, "milestone.autophage_met", "^BUI_MET");

        // Column 4: Guilds
        AddSectionTitle(s1c4, "Guilds", "milestone.section_guilds");
        AddSectionTitle(s1c4, "Traders", "milestone.traders");
        AddField(s1c4, "milestone.standing", "^TGUILD_STAND");
        AddField(s1c4, "milestone.missions", "^TGDONE_MISSIONS");
        AddField(s1c4, "milestone.plants_farmed", "^PLANTS_PLANTED");
        AddSectionTitle(s1c4, "Warriors", "milestone.warriors");
        AddField(s1c4, "milestone.standing", "^WGUILD_STAND");
        AddField(s1c4, "milestone.missions", "^WGDONE_MISSIONS");
        AddSectionTitle(s1c4, "Explorers", "milestone.explorers");
        AddField(s1c4, "milestone.standing", "^EGUILD_STAND");
        AddField(s1c4, "milestone.missions", "^EGDONE_MISSIONS");
        AddField(s1c4, "milestone.rare_creatures", "^RARE_SCANNED");
        AddSectionTitle(s1c4, "Pirate", "milestone.pirate");
        AddField(s1c4, "milestone.standing", "^PIRATE_STAND");
        AddField(s1c4, "milestone.missions", "^PDONE_MISSIONS");
        AddField(s1c4, "milestone.systems_visited", "^PIRATE_SYSTEMS");
        AddField(s1c4, "milestone.pirate_missions_req", "^MISSION_PIRATES");
        AddField(s1c4, "milestone.pirate_missions", "^PIRATE_MISSIONS");
        AddField(s1c4, "milestone.pirate_mysteries", "^PIRATE_MYSTERY");
        AddField(s1c4, "milestone.pirate_freighters_seen", "^PIR_FREI_SEEN");
        AddField(s1c4, "milestone.smuggled_value", "^SMUGGLE_VALUE");

        scroll1.Controls.Add(section1);
        tab1.Controls.Add(scroll1);
        _tabControl.TabPages.Add(tab1);

        // === Tab 2: Other Stats ===
        var tab2 = new TabPage("Other Stats");
        var scroll2 = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        var otherStacker = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0),
        };

        // Row 1: Other Milestones/Stats — all 95 fields distributed evenly (24/24/24/23)
        var section2 = CreateFourColumnSection();
        var s2c1 = GetColumnPanel(section2, 0);
        var s2c2 = GetColumnPanel(section2, 1);
        var s2c3 = GetColumnPanel(section2, 2);
        var s2c4 = GetColumnPanel(section2, 3);

        // Column 1 (24 fields)
        AddSectionTitle(s2c1, "Other Milestones / Stats", "milestone.section_other");
        AddField(s2c1, "milestone.total_play_time", "^TIME");
        AddField(s2c1, "milestone.play_sessions", "^PLAY_SESSIONS");
        AddField(s2c1, "milestone.total_deaths", "^DEATHS");
        AddField(s2c1, "milestone.longest_life", "^LONGEST_LIFE");
        AddField(s2c1, "milestone.units_all_time", "^MONEY_EVER");
        AddField(s2c1, "milestone.nanites", "^NANITES");
        AddField(s2c1, "milestone.nanites_all_time", "^NANITES_EVER");
        AddField(s2c1, "milestone.ships_bought", "^SHIPS_BOUGHT");
        AddField(s2c1, "milestone.distance_jetpack", "^DIST_JETPACK");
        AddField(s2c1, "milestone.distance_flying", "^DIST_FLY");
        AddField(s2c1, "milestone.distance_exocraft", "^DIST_EXO");
        AddField(s2c1, "milestone.distance_pulse", "^DIST_PULSE");
        AddField(s2c1, "milestone.distance_submarine", "^DIST_SUB");
        AddField(s2c1, "milestone.distance_in_space", "^DIST_SPACE");
        AddField(s2c1, "milestone.planets_discovered", "^DISC_PLANETS");
        AddField(s2c1, "milestone.systems_discovered", "^DISC_SYSTEMS");
        AddField(s2c1, "milestone.creatures_discovered", "^DISC_CREATURES");
        AddField(s2c1, "milestone.flora_discovered", "^DISC_FLORA");
        AddField(s2c1, "milestone.minerals_discovered", "^DISC_MINERALS");
        AddField(s2c1, "milestone.waypoints_discovered", "^DISC_WAYPOINTS");
        AddField(s2c1, "milestone.planets_visited", "^VISIT_PLANETS");
        AddField(s2c1, "milestone.creatures_fed", "^CREATURES_FED");
        AddField(s2c1, "milestone.creatures_killed", "^CREATURES_KILL");
        AddField(s2c1, "milestone.extreme_survival", "^EXTREME_WALK");

        // Column 2 (24 fields)
        AddSectionTitle(s2c2, "");
        AddField(s2c2, "milestone.storm_survival", "^STORM_WALK");
        AddField(s2c2, "milestone.cave_exploration", "^CAVE_WALK");
        AddField(s2c2, "milestone.time_in_space", "^SPACE_TIME");
        AddField(s2c2, "milestone.space_battles", "^SPACE_BATTLES");
        AddField(s2c2, "milestone.fish_caught", "^FISH_CAUGHT");
        AddField(s2c2, "milestone.fish_released", "^FISH_RELEASED");
        AddField(s2c2, "milestone.bones_found", "^BONES_FOUND");
        AddField(s2c2, "milestone.fossils_made", "^FOS_MADE");
        AddField(s2c2, "milestone.salvage_looted", "^SALVAGE_LOOTED");
        AddField(s2c2, "milestone.ruins_looted", "^RUINS_LOOTED");
        AddField(s2c2, "milestone.bounties", "^BOUNTIES");
        AddField(s2c2, "milestone.gifts_given", "^GIFTS_GIVEN");
        AddField(s2c2, "milestone.parts_placed", "^PARTS_PLACED");
        AddField(s2c2, "milestone.base_parts_got", "^BASEPARTS_GOT");
        AddField(s2c2, "milestone.pets_adopted", "^PETS_ADOPTED");
        AddField(s2c2, "milestone.photo_mode_used", "^PHOTO_MODE_USED");
        AddField(s2c2, "milestone.portal_warps", "^PORTAL_WARPS");
        AddField(s2c2, "milestone.items_teleported", "^ITEMS_TELEPRT");
        AddField(s2c2, "milestone.abandoned_freighters", "^ABAND_FREIGHTER");
        AddField(s2c2, "milestone.acrobat", "^ACROBAT");
        AddField(s2c2, "milestone.props_analysed", "^ANALYSE_PROP");
        AddField(s2c2, "milestone.app_sessions", "^APP_SESSIONS");
        AddField(s2c2, "milestone.artifact_hints", "^ARTIFACT_HINTS");
        AddField(s2c2, "milestone.asteroids_destroyed", "^ASTEROIDS");

        // Column 3 (24 fields)
        AddSectionTitle(s2c3, "");
        AddField(s2c3, "milestone.atlas_loops", "^ATLAS_LOOPS");
        AddField(s2c3, "milestone.basecamp_lore", "^BASECOMP_LORE");
        AddField(s2c3, "milestone.corvette_parts", "^BIGGS_PART_GOT");
        AddField(s2c3, "milestone.black_hole_walks", "^BLACKHOLE_WALKS");
        AddField(s2c3, "milestone.black_hole_warps", "^BLACKHOLE_WARPS");
        AddField(s2c3, "milestone.dice_games_lost", "^DICE_GAME_LOST");
        AddField(s2c3, "milestone.dice_games_won", "^DICE_GAME_WON");
        AddField(s2c3, "milestone.early_warps", "^EARLY_WARPS");
        AddField(s2c3, "milestone.eggs_received", "^EGGS_GOT");
        AddField(s2c3, "milestone.eggs_hatched", "^EGGS_HATCHED");
        AddField(s2c3, "milestone.eggs_modified", "^EGGS_MODDED");
        AddField(s2c3, "milestone.egg_pods", "^EGG_PODS");
        AddField(s2c3, "milestone.excavated", "^EXCAVATED");
        AddField(s2c3, "milestone.walked_in_toxic", "^EX_TOX_WALK");
        AddField(s2c3, "milestone.fiend_eggs", "^FIEND_EGG");
        AddField(s2c3, "milestone.boots_fished", "^FISH_BOOT");
        AddField(s2c3, "milestone.fish_cash", "^FISH_CASH");
        AddField(s2c3, "milestone.legendary_fish", "^FISH_LEGEND");
        AddField(s2c3, "milestone.fish_trapped", "^FISH_TRAPPED");
        AddField(s2c3, "milestone.pods_broken", "^FPODS_BROKEN");
        AddField(s2c3, "milestone.frigates", "^FRIGATES");
        AddField(s2c3, "milestone.gravitino_balls", "^GRAVBALLS");
        AddField(s2c3, "milestone.gravity_grabs", "^GRAV_GRAB");
        AddField(s2c3, "milestone.gravity_pushes", "^GRAV_PUSH");

        // Column 4 (23 fields)
        AddSectionTitle(s2c4, "");
        AddField(s2c4, "milestone.grav_throws", "^GRAV_THROW");
        AddField(s2c4, "milestone.weapon_repairs", "^GUNSLOTREPAIRS");
        AddField(s2c4, "milestone.head_repairs", "^HEAD_REPAIRS");
        AddField(s2c4, "milestone.junk_metal", "^JM");
        AddField(s2c4, "milestone.junk_metal_banked", "^JM_BANKED");
        AddField(s2c4, "milestone.settlement_judgements", "^JUDGEMENTS");
        AddField(s2c4, "milestone.longest_life_ex", "^LONGEST_LIFE_EX");
        AddField(s2c4, "milestone.meditation", "^MEDITATION");
        AddField(s2c4, "milestone.npcs_rescued", "^NPCS_RESCUED");
        AddField(s2c4, "milestone.plants_gathered", "^PLANTS_GATHERED");
        AddField(s2c4, "milestone.police_summons", "^POLICE_SUMMON");
        AddField(s2c4, "milestone.poop_collected", "^POOP_COLLECTED");
        AddField(s2c4, "milestone.quicksilver_spent", "^QS_SPENT");
        AddField(s2c4, "milestone.resources_extracted", "^RES_EXTRACTED");
        AddField(s2c4, "milestone.space_pois", "^SPACE_POI");
        AddField(s2c4, "milestone.space_walks", "^SPACE_WALK");
        AddField(s2c4, "milestone.storm_crystals", "^STORM_CRYSTALS");
        AddField(s2c4, "milestone.times_in_space", "^TIMES_IN_SPACE");
        AddField(s2c4, "milestone.treasure_found", "^TREASURE_FOUND");
        AddField(s2c4, "milestone.tunnelled_distance", "^TUNNELLED");
        AddField(s2c4, "milestone.vr_grabs", "^VR_GRABS");
        AddField(s2c4, "milestone.vr_inits", "^VR_INIT");
        AddField(s2c4, "milestone.vr_snapturns", "^VR_SNAPTURNS");

        otherStacker.Controls.Add(section2);

        // Row 2: [Discoveries (Planets)] [Discoveries (Creatures)] [Multiplayer] [Pet Battles + Travel]
        var section3 = CreateFourColumnSection();
        var s3c1 = GetColumnPanel(section3, 0);
        var s3c2 = GetColumnPanel(section3, 1);
        var s3c3 = GetColumnPanel(section3, 2);
        var s3c4 = GetColumnPanel(section3, 3);

        // Column 1: Discoveries (Planets)
        AddSectionTitle(s3c1, "Discoveries (Planets)", "milestone.section_disc_planets");
        AddField(s3c1, "milestone.disc_abandoned", "^DISC_ABAND");
        AddField(s3c1, "milestone.disc_cold", "^DISC_P_COLD");
        AddField(s3c1, "milestone.disc_dead", "^DISC_P_DEAD");
        AddField(s3c1, "milestone.disc_dust", "^DISC_P_DUST");
        AddField(s3c1, "milestone.disc_gas", "^DISC_P_GAS");
        AddField(s3c1, "milestone.disc_hot", "^DISC_P_HOT");
        AddField(s3c1, "milestone.disc_lava", "^DISC_P_LAVA");
        AddField(s3c1, "milestone.disc_lush", "^DISC_P_LUSH");
        AddField(s3c1, "milestone.disc_radioactive", "^DISC_P_RAD");
        AddField(s3c1, "milestone.disc_rgb", "^DISC_P_RGB");
        AddField(s3c1, "milestone.disc_swamp", "^DISC_P_SWAMP");
        AddField(s3c1, "milestone.disc_toxic", "^DISC_P_TOX");
        AddField(s3c1, "milestone.disc_water", "^DISC_P_WATER");
        AddField(s3c1, "milestone.disc_weird", "^DISC_P_WEIRD");
        AddField(s3c1, "milestone.disc_rare_system", "^DISC_RARE_SYS");
        AddField(s3c1, "milestone.visit_cold", "^VISIT_COLD");
        AddField(s3c1, "milestone.visit_dead", "^VISIT_DEAD");
        AddField(s3c1, "milestone.visit_dust", "^VISIT_DUST");
        AddField(s3c1, "milestone.visit_gas", "^VISIT_GAS");
        AddField(s3c1, "milestone.visit_hot", "^VISIT_HOT");
        AddField(s3c1, "milestone.visit_lava", "^VISIT_LAVA");
        AddField(s3c1, "milestone.visit_lush", "^VISIT_LUSH");
        AddField(s3c1, "milestone.visit_radioactive", "^VISIT_RAD");
        AddField(s3c1, "milestone.visit_rgb", "^VISIT_RGB");
        AddField(s3c1, "milestone.visit_swamp", "^VISIT_SWAMP");
        AddField(s3c1, "milestone.visit_toxic", "^VISIT_TOX");
        AddField(s3c1, "milestone.visit_water", "^VISIT_WATER");
        AddField(s3c1, "milestone.visit_weird", "^VISIT_WEIRD");

        // Column 2: Discoveries (Creatures)
        AddSectionTitle(s3c2, "Discoveries (Creatures)", "milestone.section_disc_creatures");
        AddField(s3c2, "milestone.disc_cre_aggressive", "^DISC_CRE_AGGRO");
        AddField(s3c2, "milestone.disc_cre_flying", "^DISC_CRE_AIR");
        AddField(s3c2, "milestone.disc_cre_cave", "^DISC_CRE_CAVE");
        AddField(s3c2, "milestone.disc_cre_dissonant", "^DISC_CRE_DISS");
        AddField(s3c2, "milestone.disc_cre_land", "^DISC_CRE_LAND");
        AddField(s3c2, "milestone.disc_cre_robot", "^DISC_CRE_ROBOT");
        AddField(s3c2, "milestone.disc_cre_water", "^DISC_CRE_WATER");
        AddField(s3c2, "milestone.disc_cre_weird", "^DISC_CRE_WEIRD");
        AddField(s3c2, "milestone.disc_glowing_strider", "^DISC_STRIDERGLO");

        // Column 3: Multiplayer
        AddSectionTitle(s3c3, "Multiplayer", "milestone.section_multiplayer");
        AddField(s3c3, "milestone.mp_depots_done", "^MP_DEPOT_DONE");
        AddField(s3c3, "milestone.mp_depots_hacked", "^MP_DEPOT_HACK");
        AddField(s3c3, "milestone.mp_events", "^MP_EVENT_COUNT");
        AddField(s3c3, "milestone.mp_fish", "^MP_FISH_COUNT");
        AddField(s3c3, "milestone.mp_full_session_count", "^MP_FULL_COUNT");
        AddField(s3c3, "milestone.mp_full_time_spent", "^MP_FULL_TIME");
        AddField(s3c3, "milestone.mp_missions_accessed", "^MP_MIS_ACCESS");
        AddField(s3c3, "milestone.mp_missions_started", "^MP_MIS_STARTED");
        AddField(s3c3, "milestone.mp_orb_count", "^MP_ORB_COUNT");
        AddField(s3c3, "milestone.mp_orb_time", "^MP_ORB_TIME");
        AddField(s3c3, "milestone.mp_pirate_waves", "^MP_PIRATES_WAVE");
        AddField(s3c3, "milestone.mp_planet_quest_markers", "^MP_PQ_RMARKER");
        AddField(s3c3, "milestone.mp_planet_quest_stones", "^MP_PQ_WSTONES");
        AddField(s3c3, "milestone.mp_rep_fails", "^MP_REP_FAILS");
        AddField(s3c3, "milestone.mp_sessions", "^MP_SESSIONS");
        AddField(s3c3, "milestone.nexus_missions", "^NEXUS_MISSIONS");
        AddField(s3c3, "milestone.nexus_planet_quests", "^NEXUS_MISS_PQ");
        AddField(s3c3, "milestone.nexus_qs_missions", "^NEXUS_MISS_QS");
        AddField(s3c3, "milestone.nexus_standing", "^NEXUS_STAND");

        // Column 4: Pet Battles (top) + Travel (bottom, with spacer)
        AddSectionTitle(s3c4, "Pet Battles", "milestone.section_pet_battles");
        AddField(s3c4, "milestone.pb_boss_wins", "^PB_BOSS_WINS");
        AddField(s3c4, "milestone.pb_challenge_hall_wins", "^PB_CHALL_WINS");
        AddField(s3c4, "milestone.pb_nexus", "^PB_D_NEXUS");
        AddField(s3c4, "milestone.pb_losses", "^PB_LOSSES");
        AddField(s3c4, "milestone.pb_maxed_pets", "^PB_PETS_MAXED");
        AddField(s3c4, "milestone.pb_wins", "^PB_WINS");
        AddField(s3c4, "milestone.pets_owned", "^PETS_OWNED");
        AddField(s3c4, "milestone.pet_levels_spent", "^PET_LEVEL_SPENT");
        AddVerticalSpacer(s3c4);
        AddSectionTitle(s3c4, "Travel", "milestone.section_travel");
        AddField(s3c4, "milestone.dist_any_corvette", "^DIST_BIGGS");
        AddField(s3c4, "milestone.dist_creature", "^DIST_CRE_RIDE");
        AddField(s3c4, "milestone.dist_own_corvette", "^DIST_MY_BIGGS");
        AddField(s3c4, "milestone.dist_other_corvette", "^DIST_OTH_BIGGS");
        AddField(s3c4, "milestone.dist_flying_pet", "^DIST_PET_FLY");
        AddField(s3c4, "milestone.dist_pet", "^DIST_PET_RIDE");
        AddField(s3c4, "milestone.dist_swam", "^DIST_SWAM");
        AddField(s3c4, "milestone.walked_in_cold", "^EX_COLD_WALK");
        AddField(s3c4, "milestone.walked_in_heat", "^EX_HOT_WALK");
        AddField(s3c4, "milestone.walked_in_radiation", "^EX_RAD_WALK");

        otherStacker.Controls.Add(section3);
        scroll2.Controls.Add(otherStacker);
        tab2.Controls.Add(scroll2);
        _tabControl.TabPages.Add(tab2);

        Controls.Add(_tabControl);

        ResumeLayout(false);
        PerformLayout();
    }

    private DoubleBufferedTabControl _tabControl = null!;
}
