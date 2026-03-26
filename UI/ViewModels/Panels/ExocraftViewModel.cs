using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class ExocraftViewModel : PanelViewModelBase
{
    private JsonArray? _vehicleOwnership;
    private JsonObject? _savedPlayerState;
    private readonly List<int> _addedVehicleIndices = new();

    [ObservableProperty] private ObservableCollection<string> _vehicleList = new();
    [ObservableProperty] private int _selectedVehicleIndex = -1;

    [ObservableProperty] private string _vehicleName = "";
    [ObservableProperty] private bool _thirdPersonCamera;
    [ObservableProperty] private bool _minotaurAI;
    [ObservableProperty] private bool _isPrimaryVehicle;

    [ObservableProperty] private InventoryGridViewModel _cargoGrid = new();
    [ObservableProperty] private InventoryGridViewModel _techGrid = new();

    private JsonObject? _saveDataRef;

    public ExocraftViewModel()
    {
        CargoGrid.SetIsCargoInventory(true);
        CargoGrid.SetSuperchargeDisabled(true);
        CargoGrid.SetInventoryOwnerType("Vehicle");
        CargoGrid.SetInventoryGroup("Vehicle");

        TechGrid.SetIsTechInventory(true);
        TechGrid.SetSuperchargeDisabled(true);
        TechGrid.SetSlotToggleDisabled(true);
        TechGrid.SetInventoryOwnerType("Vehicle");
        TechGrid.SetInventoryGroup("Vehicle");
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _saveDataRef = saveData;
        CargoGrid.SetDatabase(database);
        CargoGrid.SetIconManager(iconManager);
        TechGrid.SetDatabase(database);
        TechGrid.SetIconManager(iconManager);

        try
        {
            VehicleList.Clear();
            _addedVehicleIndices.Clear();
            CargoGrid.LoadInventory(null);
            TechGrid.LoadInventory(null);

            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _savedPlayerState = playerState;
            _vehicleOwnership = playerState.GetArray("VehicleOwnership");
            if (_vehicleOwnership == null) return;

            foreach (var (index, name) in ExocraftLogic.VehicleTypes)
            {
                if (index < _vehicleOwnership.Length)
                {
                    VehicleList.Add(ExocraftLogic.GetLocalisedVehicleTypeName(name));
                    _addedVehicleIndices.Add(index);
                }
            }

            if (VehicleList.Count > 0)
                SelectedVehicleIndex = 0;

            try { ThirdPersonCamera = saveData.GetObject("CommonStateData")?.GetBool("UsesThirdPersonVehicleCam") ?? false; } catch { ThirdPersonCamera = false; }
            try { MinotaurAI = playerState.GetBool("VehicleAIControlEnabled"); } catch { MinotaurAI = false; }
        }
        catch { }
    }

    public override void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var vehicles = playerState.GetArray("VehicleOwnership");
            if (vehicles == null || SelectedVehicleIndex < 0) return;

            int selIdx = SelectedVehicleIndex;
            if (selIdx >= _addedVehicleIndices.Count) return;
            int arrIdx = _addedVehicleIndices[selIdx];

            var vehicle = vehicles.GetObject(arrIdx);
            try { vehicle.Set("Name", VehicleName); } catch { }

            try { saveData.GetObject("CommonStateData")?.Set("UsesThirdPersonVehicleCam", ThirdPersonCamera); } catch { }
            try { playerState.Set("VehicleAIControlEnabled", MinotaurAI); } catch { }
        }
        catch { }
    }

    partial void OnSelectedVehicleIndexChanged(int value)
    {
        if (_vehicleOwnership == null || value < 0 || value >= _addedVehicleIndices.Count) return;

        try
        {
            int arrIdx = _addedVehicleIndices[value];
            var vehicle = _vehicleOwnership.GetObject(arrIdx);

            string vehicleName = GetSelectedVehicleInternalName();
            string ownerType = ExocraftLogic.GetOwnerTypeForVehicle(vehicleName);
            CargoGrid.SetInventoryOwnerType(ownerType);
            TechGrid.SetInventoryOwnerType(ownerType);

            CargoGrid.LoadInventory(vehicle.GetObject("Inventory"));
            TechGrid.LoadInventory(vehicle.GetObject("Inventory_TechOnly"));

            try { VehicleName = vehicle.GetString("Name") ?? ""; } catch { VehicleName = ""; }

            try
            {
                int primaryIdx = _savedPlayerState?.GetInt("PrimaryVehicle") ?? -1;
                IsPrimaryVehicle = (arrIdx == primaryIdx);
            }
            catch { IsPrimaryVehicle = false; }
        }
        catch { }
    }

    private string GetSelectedVehicleInternalName()
    {
        int selIdx = SelectedVehicleIndex;
        if (selIdx < 0 || selIdx >= _addedVehicleIndices.Count) return "vehicle";
        int arrIdx = _addedVehicleIndices[selIdx];
        foreach (var (index, name) in ExocraftLogic.VehicleTypes)
        {
            if (index == arrIdx) return name;
        }
        return "vehicle";
    }

    [RelayCommand]
    private void Export() { /* TODO: implement exocraft export */ }

    [RelayCommand]
    private void Import() { /* TODO: implement exocraft import */ }

    [RelayCommand]
    private void Delete() { /* TODO: implement exocraft delete */ }
}
