using CommunityToolkit.Mvvm.ComponentModel;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class ExosuitViewModel : PanelViewModelBase
{
    [ObservableProperty] private InventoryGridViewModel _cargoGrid = new();
    [ObservableProperty] private InventoryGridViewModel _techGrid = new();

    public ExosuitViewModel()
    {
        ConfigureGrids();
    }

    private void ConfigureGrids()
    {
        CargoGrid.SetIsCargoInventory(true);
        CargoGrid.SetInventoryOwnerType("Suit");
        CargoGrid.SetInventoryGroup("PersonalCargo");
        CargoGrid.SetSuperchargeDisabled(true);

        TechGrid.SetIsTechInventory(true);
        TechGrid.SetInventoryOwnerType("Suit");
        TechGrid.SetInventoryGroup("Personal");

        var cfg = ExportConfig.Instance;
        CargoGrid.SetExportFileName($"exosuit_cargo_inv{cfg.ExosuitExt}");
        TechGrid.SetExportFileName($"exosuit_tech_inv{cfg.ExosuitExt}");
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        CargoGrid.SetDatabase(database);
        CargoGrid.SetIconManager(iconManager);
        TechGrid.SetDatabase(database);
        TechGrid.SetIconManager(iconManager);

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            CargoGrid.LoadInventory(playerState.GetObject(ExosuitLogic.CargoInventoryKey));
            TechGrid.LoadInventory(playerState.GetObject(ExosuitLogic.TechInventoryKey));

            CargoGrid.MaxSupportedText = ExosuitLogic.CargoMaxLabel;
            TechGrid.MaxSupportedText = ExosuitLogic.TechMaxLabel;
        }
        catch { }
    }

    public override void SaveData(JsonObject saveData)
    {
        // Inventory changes are saved in-place via the grid's SlotData references
    }
}
