using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class MilestoneStatField : ObservableObject
{
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private int _value;

    public string StatId { get; init; } = "";
}

public partial class MilestoneSection : ObservableObject
{
    [ObservableProperty] private string _title = "";
    public ObservableCollection<MilestoneStatField> Fields { get; } = new();
}

public partial class MilestoneViewModel : PanelViewModelBase
{
    public ObservableCollection<MilestoneSection> Tab1Column1 { get; } = new();
    public ObservableCollection<MilestoneSection> Tab1Column2 { get; } = new();
    public ObservableCollection<MilestoneSection> Tab1Column3 { get; } = new();
    public ObservableCollection<MilestoneSection> Tab2Column1 { get; } = new();
    public ObservableCollection<MilestoneSection> Tab2Column2 { get; } = new();
    public ObservableCollection<MilestoneSection> Tab2Column3 { get; } = new();

    private readonly Dictionary<string, MilestoneStatField> _fieldMap = new();

    public MilestoneViewModel()
    {
        BuildLayout();
    }

    private MilestoneStatField AddField(MilestoneSection section, string label, string statId)
    {
        var field = new MilestoneStatField { Label = label, StatId = statId };
        section.Fields.Add(field);
        _fieldMap[statId] = field;
        return field;
    }

    private void BuildLayout()
    {
        // Tab 1, Column 1: Milestones + Kills
        var milestones = new MilestoneSection { Title = "Milestones" };
        AddField(milestones, "On Foot Exploration", "^DIST_WALKED");
        AddField(milestones, "Alien Encounters", "^ALIENS_MET");
        AddField(milestones, "Words Collected", "^WORDS_LEARNT");
        AddField(milestones, "Most Units Accrued", "^MONEY");
        AddField(milestones, "Ships Destroyed", "^ENEMIES_KILLED");
        AddField(milestones, "Sentinels Destroyed", "^SENTINEL_KILLS");
        AddField(milestones, "Space Exploration", "^DIST_WARP");
        AddField(milestones, "Planet Zoology Scanned", "^DISC_ALL_CREATU");
        Tab1Column1.Add(milestones);

        var kills = new MilestoneSection { Title = "Kills" };
        AddField(kills, "Predators", "^PREDS_KILLED");
        AddField(kills, "Sentinel Drones", "^DRONES_KILLED");
        AddField(kills, "Sentinel Quads", "^QUADS_KILLED");
        AddField(kills, "Sentinel Walkers", "^WALKERS_KILLED");
        AddField(kills, "Pirates", "^PIRATES_KILLED");
        AddField(kills, "Police", "^POLICE_KILLED");
        Tab1Column1.Add(kills);

        // Tab 1, Column 2: Alien Factions
        var alienTitle = new MilestoneSection { Title = "Alien Factions" };
        Tab1Column2.Add(alienTitle);

        var gek = new MilestoneSection { Title = "Gek" };
        AddField(gek, "Standing", "^TRA_STANDING");
        AddField(gek, "Missions", "^TDONE_MISSIONS");
        AddField(gek, "Systems Visited", "^TSEEN_SYSTEMS");
        Tab1Column2.Add(gek);

        var vykeen = new MilestoneSection { Title = "Vy'keen" };
        AddField(vykeen, "Standing", "^WAR_STANDING");
        AddField(vykeen, "Missions", "^WDONE_MISSIONS");
        AddField(vykeen, "Systems Visited", "^WSEEN_SYSTEMS");
        Tab1Column2.Add(vykeen);

        var korvax = new MilestoneSection { Title = "Korvax" };
        AddField(korvax, "Standing", "^EXP_STANDING");
        AddField(korvax, "Missions", "^EDONE_MISSIONS");
        AddField(korvax, "Systems Visited", "^ESEEN_SYSTEMS");
        Tab1Column2.Add(korvax);

        var autophage = new MilestoneSection { Title = "Autophage" };
        AddField(autophage, "Standing", "^BUI_STANDING");
        AddField(autophage, "Missions", "^BDONE_MISSIONS");
        Tab1Column2.Add(autophage);

        // Tab 1, Column 3: Guilds
        var guildsTitle = new MilestoneSection { Title = "Guilds" };
        Tab1Column3.Add(guildsTitle);

        var traders = new MilestoneSection { Title = "Traders" };
        AddField(traders, "Standing", "^TGUILD_STAND");
        AddField(traders, "Missions", "^TGDONE_MISSIONS");
        AddField(traders, "Plants Farmed", "^PLANTS_PLANTED");
        Tab1Column3.Add(traders);

        var warriors = new MilestoneSection { Title = "Warriors" };
        AddField(warriors, "Standing", "^WGUILD_STAND");
        AddField(warriors, "Missions", "^WGDONE_MISSIONS");
        Tab1Column3.Add(warriors);

        var explorers = new MilestoneSection { Title = "Explorers" };
        AddField(explorers, "Standing", "^EGUILD_STAND");
        AddField(explorers, "Missions", "^EGDONE_MISSIONS");
        AddField(explorers, "Rare Creatures", "^RARE_SCANNED");
        Tab1Column3.Add(explorers);

        var pirate = new MilestoneSection { Title = "Pirate" };
        AddField(pirate, "Standing", "^PIRATE_STAND");
        AddField(pirate, "Missions", "^PDONE_MISSIONS");
        AddField(pirate, "Systems Visited", "^PIRATE_SYSTEMS");
        Tab1Column3.Add(pirate);

        // Tab 2, Column 1: Other Milestones / Stats
        var otherStats = new MilestoneSection { Title = "Other Milestones / Stats" };
        AddField(otherStats, "Total Play Time", "^TIME");
        AddField(otherStats, "Play Sessions", "^PLAY_SESSIONS");
        AddField(otherStats, "Total Deaths", "^DEATHS");
        AddField(otherStats, "Longest Life", "^LONGEST_LIFE");
        AddField(otherStats, "Units (All Time)", "^MONEY_EVER");
        AddField(otherStats, "Nanites", "^NANITES");
        AddField(otherStats, "Nanites (All Time)", "^NANITES_EVER");
        AddField(otherStats, "Ships Bought", "^SHIPS_BOUGHT");
        AddField(otherStats, "Distance Jetpack", "^DIST_JETPACK");
        AddField(otherStats, "Distance Flying", "^DIST_FLY");
        AddField(otherStats, "Distance Exocraft", "^DIST_EXO");
        AddField(otherStats, "Distance Pulse", "^DIST_PULSE");
        AddField(otherStats, "Distance Submarine", "^DIST_SUB");
        AddField(otherStats, "Distance In Space", "^DIST_SPACE");
        Tab2Column1.Add(otherStats);

        // Tab 2, Column 2: Discoveries
        var discoveries = new MilestoneSection { Title = "Discoveries" };
        AddField(discoveries, "Planets Discovered", "^DISC_PLANETS");
        AddField(discoveries, "Systems Discovered", "^DISC_SYSTEMS");
        AddField(discoveries, "Creatures Discovered", "^DISC_CREATURES");
        AddField(discoveries, "Flora Discovered", "^DISC_FLORA");
        AddField(discoveries, "Minerals Discovered", "^DISC_MINERALS");
        AddField(discoveries, "Waypoints Discovered", "^DISC_WAYPOINTS");
        AddField(discoveries, "Planets Visited", "^VISIT_PLANETS");
        AddField(discoveries, "Creatures Fed", "^CREATURES_FED");
        AddField(discoveries, "Creatures Killed", "^CREATURES_KILL");
        AddField(discoveries, "Extreme Survival", "^EXTREME_WALK");
        AddField(discoveries, "Storm Survival", "^STORM_WALK");
        AddField(discoveries, "Cave Exploration", "^CAVE_WALK");
        AddField(discoveries, "Time In Space", "^SPACE_TIME");
        AddField(discoveries, "Space Battles", "^SPACE_BATTLES");
        Tab2Column2.Add(discoveries);

        // Tab 2, Column 3: Activities
        var activities = new MilestoneSection { Title = "Activities" };
        AddField(activities, "Fish Caught", "^FISH_CAUGHT");
        AddField(activities, "Fish Released", "^FISH_RELEASED");
        AddField(activities, "Bones Found", "^BONES_FOUND");
        AddField(activities, "Fossils Made", "^FOS_MADE");
        AddField(activities, "Salvage Looted", "^SALVAGE_LOOTED");
        AddField(activities, "Ruins Looted", "^RUINS_LOOTED");
        AddField(activities, "Bounties", "^BOUNTIES");
        AddField(activities, "Gifts Given", "^GIFTS_GIVEN");
        AddField(activities, "Parts Placed", "^PARTS_PLACED");
        AddField(activities, "Base Parts Got", "^BASEPARTS_GOT");
        AddField(activities, "Pets Adopted", "^PETS_ADOPTED");
        AddField(activities, "Photo Mode Used", "^PHOTO_MODE_USED");
        AddField(activities, "Portal Warps", "^PORTAL_WARPS");
        AddField(activities, "Items Teleported", "^ITEMS_TELEPRT");
        Tab2Column3.Add(activities);
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        foreach (var field in _fieldMap.Values)
            field.Value = 0;

        var entries = MilestoneLogic.FindGlobalStats(saveData);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries.GetObject(i);
            if (entry == null) continue;
            string? id = entry.GetString("Id");
            if (id == null || !_fieldMap.TryGetValue(id, out var field)) continue;

            field.Value = MilestoneLogic.ReadStatEntryValue(entry);
        }
    }

    public override void SaveData(JsonObject saveData)
    {
        var entries = MilestoneLogic.FindGlobalStats(saveData);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries.GetObject(i);
            if (entry == null) continue;
            string? id = entry.GetString("Id");
            if (id == null || !_fieldMap.TryGetValue(id, out var field)) continue;

            MilestoneLogic.WriteStatEntryValue(entry, field.Value);
        }
    }
}
