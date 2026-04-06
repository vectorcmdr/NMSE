using CommunityToolkit.Mvvm.ComponentModel;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class FleetViewModel : PanelViewModelBase
{
    [ObservableProperty] private string _frigateTabHeader = "Frigates";
    [ObservableProperty] private string _squadronTabHeader = "Squadron";

    public FrigateViewModel Frigates { get; } = new();
    public SquadronViewModel Squadron { get; } = new();

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        Frigates.LoadData(saveData, database, iconManager);
        Squadron.LoadData(saveData);
    }

    public override void SaveData(JsonObject saveData)
    {
        Frigates.SaveData(saveData);
        Squadron.SaveData();
    }
}
