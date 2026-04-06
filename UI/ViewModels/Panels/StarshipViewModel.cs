using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class StarshipViewModel : PanelViewModelBase
{
    private JsonArray? _shipOwnership;
    private JsonObject? _playerState;
    private JsonObject? _saveData;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private int _primaryShipIndex;

    [ObservableProperty] private ObservableCollection<string> _shipList = new();
    [ObservableProperty] private int _selectedShipIndex = -1;
    [ObservableProperty] private string _primaryShipLabel = "";

    private readonly List<int> _shipDataIndices = new();

    [ObservableProperty] private string _shipName = "";
    [ObservableProperty] private string _shipSeed = "";
    [ObservableProperty] private ObservableCollection<string> _shipTypes = new();
    [ObservableProperty] private int _selectedTypeIndex = -1;
    [ObservableProperty] private ObservableCollection<string> _shipClasses = new(StarshipLogic.ShipClasses);
    [ObservableProperty] private int _selectedClassIndex = -1;
    [ObservableProperty] private bool _useOldColours;

    [ObservableProperty] private double _damage;
    [ObservableProperty] private double _shield;
    [ObservableProperty] private double _hyperdrive;
    [ObservableProperty] private double _maneuver;

    [ObservableProperty] private InventoryGridViewModel _cargoGrid = new();
    [ObservableProperty] private InventoryGridViewModel _techGrid = new();

    [ObservableProperty] private bool _isCorvette;

    private StarshipLogic.ShipTypeItem[] _typeItems = [];

    public StarshipViewModel()
    {
        CargoGrid.SetIsCargoInventory(true);
        CargoGrid.SetInventoryOwnerType("Ship");
        CargoGrid.SetInventoryGroup("Ship");

        TechGrid.SetIsTechInventory(true);
        TechGrid.SetInventoryOwnerType("Ship");
        TechGrid.SetInventoryGroup("Ship");
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        _iconManager = iconManager;
        _saveData = saveData;

        CargoGrid.SetDatabase(database);
        CargoGrid.SetIconManager(iconManager);
        TechGrid.SetDatabase(database);
        TechGrid.SetIconManager(iconManager);

        try
        {
            RefreshTypeItems();

            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _shipOwnership = _playerState.GetArray("ShipOwnership");
            if (_shipOwnership == null) return;

            _primaryShipIndex = 0;
            try { _primaryShipIndex = _playerState.GetInt("PrimaryShip"); } catch { }
            PrimaryShipLabel = StarshipLogic.GetPrimaryShipName(_shipOwnership, _primaryShipIndex);

            RefreshShipList();

            if (ShipList.Count > 0)
            {
                int selectIdx = 0;
                for (int i = 0; i < _shipDataIndices.Count; i++)
                {
                    if (_shipDataIndices[i] == _primaryShipIndex)
                    {
                        selectIdx = i;
                        break;
                    }
                }
                SelectedShipIndex = Math.Clamp(selectIdx, 0, ShipList.Count - 1);
            }
        }
        catch { }
    }

    public override void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var ships = playerState.GetArray("ShipOwnership");
            if (ships == null || SelectedShipIndex < 0 || SelectedShipIndex >= _shipDataIndices.Count) return;

            int idx = _shipDataIndices[SelectedShipIndex];
            if (idx >= ships.Length) return;

            var ship = ships.GetObject(idx);

            var values = new StarshipLogic.ShipSaveValues
            {
                Name = ShipName,
                SelectedTypeName = GetSelectedTypeInternalName(),
                ClassIndex = SelectedClassIndex,
                Seed = ShipSeed,
                Damage = Damage,
                Shield = Shield,
                Hyperdrive = Hyperdrive,
                Maneuver = Maneuver,
                UseOldColours = UseOldColours,
                ShipIndex = idx,
                PrimaryShipIndex = _primaryShipIndex
            };

            StarshipLogic.SaveShipData(ship, playerState, values);
        }
        catch { }
    }

    partial void OnSelectedShipIndexChanged(int value)
    {
        if (value < 0 || value >= ShipList.Count) return;
        if (_shipOwnership == null) return;

        try
        {
            int idx = _shipDataIndices[value];
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);
            var data = StarshipLogic.LoadShipData(ship, _playerState, idx);

            ShipName = data.Name;
            SelectTypeByName(data.ShipTypeName);
            SelectedClassIndex = data.ClassIndex;
            ShipSeed = data.Seed;
            UseOldColours = data.UseOldColours;
            Damage = data.Damage;
            Shield = data.Shield;
            Hyperdrive = data.Hyperdrive;
            Maneuver = data.Maneuver;

            IsCorvette = StarshipLogic.IsCorvette(data.Filename);

            string ownerType = StarshipLogic.GetOwnerTypeForShip(data.ShipTypeName);
            CargoGrid.SetInventoryOwnerType(ownerType);
            TechGrid.SetInventoryOwnerType(ownerType);

            CargoGrid.LoadInventory(data.Inventory);
            TechGrid.LoadInventory(data.TechInventory);

            CargoGrid.MaxSupportedText = data.CargoMaxLabel;
            TechGrid.MaxSupportedText = data.TechMaxLabel;
        }
        catch { }
    }

    [RelayCommand]
    private void GenerateSeed()
    {
        ShipSeed = $"0x{Random.Shared.NextInt64():X16}";
    }

    [RelayCommand]
    private void MakePrimary()
    {
        if (_shipOwnership == null || SelectedShipIndex < 0 || SelectedShipIndex >= _shipDataIndices.Count) return;
        int idx = _shipDataIndices[SelectedShipIndex];
        if (idx >= _shipOwnership.Length) return;

        _primaryShipIndex = idx;
        PrimaryShipLabel = StarshipLogic.GetPrimaryShipName(_shipOwnership, _primaryShipIndex);
    }

    private void RefreshShipList()
    {
        ShipList.Clear();
        _shipDataIndices.Clear();
        if (_shipOwnership == null) return;
        foreach (var item in StarshipLogic.BuildShipList(_shipOwnership))
        {
            ShipList.Add(item.DisplayName);
            _shipDataIndices.Add(item.DataIndex);
        }
    }

    private void RefreshTypeItems()
    {
        _typeItems = StarshipLogic.GetShipTypeItems();
        ShipTypes.Clear();
        foreach (var item in _typeItems)
            ShipTypes.Add(item.DisplayName);
    }

    private string? GetSelectedTypeInternalName()
    {
        if (SelectedTypeIndex < 0 || SelectedTypeIndex >= _typeItems.Length) return null;
        return _typeItems[SelectedTypeIndex].InternalName;
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

    /// <summary>File dialog funcs set by view code-behind.</summary>
    public Func<string, string, Task<string?>>? SaveFileFunc { get; set; }
    public Func<string, Task<string?>>? OpenFileFunc { get; set; }

    [RelayCommand]
    private async Task ExportShip()
    {
        if (_shipOwnership == null || SelectedShipIndex < 0 || SaveFileFunc == null) return;
        int idx = _shipDataIndices[SelectedShipIndex];
        if (idx >= _shipOwnership.Length) return;

        var ship = _shipOwnership.GetObject(idx);
        if (ship == null) return;

        var cfg = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["ship_name"] = ShipName,
            ["type"] = ShipTypes.Count > 0 && SelectedTypeIndex >= 0 ? ShipTypes[SelectedTypeIndex] : "",
            ["class"] = ShipClasses.Count > 0 && SelectedClassIndex >= 0 ? ShipClasses[SelectedClassIndex] : ""
        };
        string fileName = ExportConfig.BuildFileName(cfg.StarshipTemplate, cfg.StarshipExt, vars);
        var path = await SaveFileFunc(fileName, cfg.StarshipExt);
        if (path != null)
            ship.ExportToFile(path);
    }

    [RelayCommand]
    private async Task ImportShip()
    {
        if (_shipOwnership == null || SelectedShipIndex < 0 || OpenFileFunc == null) return;
        int idx = _shipDataIndices[SelectedShipIndex];
        if (idx >= _shipOwnership.Length) return;

        var path = await OpenFileFunc(ExportConfig.Instance.StarshipExt);
        if (path == null) return;

        var imported = JsonObject.ImportFromFile(path);
        if (imported == null) return;

        _shipOwnership.Set(idx, imported);
        OnSelectedShipIndexChanged(SelectedShipIndex);
    }

    [RelayCommand]
    private void DeleteShip()
    {
        if (_shipOwnership == null || SelectedShipIndex < 0) return;
        int idx = _shipDataIndices[SelectedShipIndex];
        if (idx >= _shipOwnership.Length) return;

        _shipOwnership.RemoveAt(idx);
        RefreshShipList();
        if (ShipList.Count > 0)
            SelectedShipIndex = 0;
    }
}
