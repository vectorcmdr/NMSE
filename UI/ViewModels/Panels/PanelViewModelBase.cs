using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public abstract class PanelViewModelBase : ViewModelBase
{
    public Func<string, string, string, Task<string?>>? SaveFilePickerFunc { get; set; }
    public Func<string, string, Task<string?>>? OpenFilePickerFunc { get; set; }

    public virtual void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager) { }
    public virtual void SaveData(JsonObject saveData) { }
}
