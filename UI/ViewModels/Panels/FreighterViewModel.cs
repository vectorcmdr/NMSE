using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class FreighterViewModel : PanelViewModelBase
{
    private JsonObject? _playerState;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    [ObservableProperty] private string _freighterName = "";
    [ObservableProperty] private ObservableCollection<string> _freighterTypes = new();
    [ObservableProperty] private int _selectedTypeIndex = -1;
    [ObservableProperty] private ObservableCollection<string> _freighterClasses = new(FreighterLogic.FreighterClasses);
    [ObservableProperty] private int _selectedClassIndex = -1;

    [ObservableProperty] private string _homeSeed = "";
    [ObservableProperty] private string _modelSeed = "";

    [ObservableProperty] private double _hyperdrive;
    [ObservableProperty] private double _fleetCoordination;

    [ObservableProperty] private string _baseItemsText = "";

    [ObservableProperty] private ObservableCollection<string> _crewRaces = new();
    [ObservableProperty] private int _selectedCrewRaceIndex = -1;
    [ObservableProperty] private string _crewSeed = "";

    [ObservableProperty] private ObservableCollection<string> _roomList = new();

    [ObservableProperty] private InventoryGridViewModel _cargoGrid = new();
    [ObservableProperty] private InventoryGridViewModel _techGrid = new();

    private FreighterLogic.FreighterTypeItem[] _typeItems = [];
    private FreighterLogic.CrewRaceItem[] _crewRaceItems = [];

    public FreighterViewModel()
    {
        CargoGrid.SetIsCargoInventory(true);
        CargoGrid.SetInventoryOwnerType("Freighter");
        CargoGrid.SetInventoryGroup("Freighter");

        TechGrid.SetIsTechInventory(true);
        TechGrid.SetInventoryOwnerType("Freighter");
        TechGrid.SetInventoryGroup("Freighter");
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        _iconManager = iconManager;
        CargoGrid.SetDatabase(database);
        CargoGrid.SetIconManager(iconManager);
        TechGrid.SetDatabase(database);
        TechGrid.SetIconManager(iconManager);

        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            RefreshTypeItems();
            RefreshCrewRaceItems();

            var data = FreighterLogic.LoadFreighterData(_playerState);

            FreighterName = data.Name;

            if (data.TypeDisplayName != null)
                SelectTypeByName(data.TypeDisplayName);
            else
                SelectedTypeIndex = -1;

            SelectedClassIndex = data.ClassIndex >= 0 ? data.ClassIndex : -1;
            HomeSeed = data.HomeSeed;
            ModelSeed = data.ModelSeed;
            Hyperdrive = data.Hyperdrive;
            FleetCoordination = data.FleetCoordination;

            BaseItemsText = data.FreighterBase != null ? data.BaseItemCount.ToString() : "N/A";

            CargoGrid.LoadInventory(data.CargoInventory);
            TechGrid.LoadInventory(data.TechInventory);

            RoomList.Clear();
            foreach (var room in FreighterLogic.DetectFreighterRooms(data.FreighterBase))
                RoomList.Add(room);

            try
            {
                var npc = _playerState.GetObject("CurrentFreighterNPC");
                if (npc != null)
                {
                    string filename = npc.GetString("Filename") ?? "";
                    if (FreighterLogic.NpcResourceToRace.TryGetValue(filename, out string? race))
                        SelectCrewRaceByName(race);
                    else
                        SelectedCrewRaceIndex = -1;

                    try
                    {
                        var seedArr = npc.GetArray("Seed");
                        CrewSeed = (seedArr != null && seedArr.Length >= 2) ? (seedArr.GetString(1) ?? "") : "";
                    }
                    catch { CrewSeed = ""; }
                }
            }
            catch { }
        }
        catch { }
    }

    public override void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            FreighterLogic.SaveFreighterData(playerState, new FreighterLogic.FreighterSaveValues
            {
                Name = FreighterName,
                SelectedTypeName = GetSelectedTypeInternalName(),
                ClassIndex = SelectedClassIndex,
                HomeSeed = HomeSeed,
                ModelSeed = ModelSeed,
                Hyperdrive = Hyperdrive,
                FleetCoordination = FleetCoordination,
            });

            try
            {
                var npc = playerState.GetObject("CurrentFreighterNPC");
                if (npc != null)
                {
                    string? selectedRace = GetSelectedCrewRaceInternalName();
                    if (!string.IsNullOrEmpty(selectedRace) && FreighterLogic.RaceToNpcResource.TryGetValue(selectedRace, out string? resource))
                        npc.Set("Filename", resource);

                    var seedArr = npc.GetArray("Seed");
                    var normalizedCrewSeed = SeedHelper.NormalizeSeed(CrewSeed);
                    if (seedArr != null && seedArr.Length >= 2 && normalizedCrewSeed != null)
                        seedArr.Set(1, normalizedCrewSeed);
                }
            }
            catch { }
        }
        catch { }
    }

    [RelayCommand]
    private void GenerateHomeSeed()
    {
        HomeSeed = $"0x{Random.Shared.NextInt64():X16}";
    }

    [RelayCommand]
    private void GenerateModelSeed()
    {
        ModelSeed = $"0x{Random.Shared.NextInt64():X16}";
    }

    [RelayCommand]
    private void GenerateCrewSeed()
    {
        CrewSeed = $"0x{Random.Shared.NextInt64():X16}";
    }

    private void RefreshTypeItems()
    {
        _typeItems = FreighterLogic.GetFreighterTypeItems();
        FreighterTypes.Clear();
        foreach (var item in _typeItems)
            FreighterTypes.Add(item.DisplayName);
    }

    private void RefreshCrewRaceItems()
    {
        _crewRaceItems = FreighterLogic.GetCrewRaceItems();
        CrewRaces.Clear();
        foreach (var item in _crewRaceItems)
            CrewRaces.Add(item.DisplayName);
    }

    private string? GetSelectedTypeInternalName()
    {
        if (SelectedTypeIndex < 0 || SelectedTypeIndex >= _typeItems.Length) return null;
        return _typeItems[SelectedTypeIndex].InternalName;
    }

    private string? GetSelectedCrewRaceInternalName()
    {
        if (SelectedCrewRaceIndex < 0 || SelectedCrewRaceIndex >= _crewRaceItems.Length) return null;
        return _crewRaceItems[SelectedCrewRaceIndex].InternalName;
    }

    private void SelectTypeByName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName)) { SelectedTypeIndex = -1; return; }
        for (int i = 0; i < _typeItems.Length; i++)
        {
            if (_typeItems[i].InternalName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            {
                SelectedTypeIndex = i;
                return;
            }
        }
        SelectedTypeIndex = -1;
    }

    private void SelectCrewRaceByName(string? raceName)
    {
        if (string.IsNullOrEmpty(raceName)) { SelectedCrewRaceIndex = -1; return; }
        for (int i = 0; i < _crewRaceItems.Length; i++)
        {
            if (_crewRaceItems[i].InternalName.Equals(raceName, StringComparison.OrdinalIgnoreCase))
            {
                SelectedCrewRaceIndex = i;
                return;
            }
        }
        SelectedCrewRaceIndex = -1;
    }

    [RelayCommand]
    private void Export() { /* TODO: implement freighter export */ }

    [RelayCommand]
    private void Import() { /* TODO: implement freighter import */ }

    [RelayCommand]
    private void Delete() { /* TODO: implement freighter delete */ }
}
