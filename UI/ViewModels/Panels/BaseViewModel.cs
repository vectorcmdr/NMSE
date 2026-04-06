using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class BaseViewModel : PanelViewModelBase
{
    private JsonObject? _playerState;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ObservableCollection<BaseInfoViewModel> _bases = new();
    [ObservableProperty] private BaseInfoViewModel? _selectedBase;
    [ObservableProperty] private string _baseName = "";
    [ObservableProperty] private string _baseItemCount = "";
    [ObservableProperty] private bool _hasBaseSelection;

    [ObservableProperty] private ObservableCollection<NpcWorkerViewModel> _npcWorkers = new();
    [ObservableProperty] private NpcWorkerViewModel? _selectedNpc;
    [ObservableProperty] private string _npcSeed = "";
    [ObservableProperty] private string _npcRace = "";

    [ObservableProperty] private ObservableCollection<InventoryGridViewModel> _chestGrids = new();

    [ObservableProperty] private ObservableCollection<StorageTabViewModel> _storageTabs = new();

    partial void OnSelectedBaseChanged(BaseInfoViewModel? value)
    {
        HasBaseSelection = value != null;
        if (value == null) return;
        BaseName = value.Data?.GetString("Name") ?? "";
        int objectCount = 0;
        try
        {
            var objects = value.Data?.GetArray("Objects");
            if (objects != null) objectCount = objects.Length;
        }
        catch { }
        BaseItemCount = objectCount.ToString();
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        _iconManager = iconManager;

        Bases.Clear();
        NpcWorkers.Clear();
        ChestGrids.Clear();
        StorageTabs.Clear();

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;
            _playerState = playerState;

            LoadBases(playerState);
            LoadNpcWorkers(playerState);
            LoadChests(playerState);
            LoadStorage(playerState);
        }
        catch { }
    }

    private void LoadBases(JsonObject playerState)
    {
        var bases = playerState.GetArray("PersistentPlayerBases");
        if (bases == null) return;

        for (int i = 0; i < bases.Length; i++)
        {
            try
            {
                var baseObj = bases.GetObject(i);
                string? baseType = null;
                try { baseType = baseObj.GetString("BaseType.PersistentBaseTypes") ?? baseObj.GetString("BaseType"); }
                catch { try { baseType = baseObj.GetString("BaseType"); } catch { } }

                int baseVersion = 0;
                try { baseVersion = baseObj.GetInt("BaseVersion"); } catch { }

                if ("HomePlanetBase".Equals(baseType, StringComparison.OrdinalIgnoreCase) && baseVersion >= 3)
                {
                    string name = baseObj.GetString("Name") ?? $"Base {i + 1}";
                    int objectCount = 0;
                    try
                    {
                        var objects = baseObj.GetArray("Objects");
                        if (objects != null) objectCount = objects.Length;
                    }
                    catch { }

                    Bases.Add(new BaseInfoViewModel
                    {
                        DisplayName = name,
                        Data = baseObj,
                        DataIndex = i,
                        ObjectCount = objectCount
                    });
                }
            }
            catch { }
        }

        if (Bases.Count > 0)
            SelectedBase = Bases[0];
    }

    private void LoadNpcWorkers(JsonObject playerState)
    {
        var npcWorkers = playerState.GetArray("NPCWorkers");
        if (npcWorkers == null) return;

        string[] workerNames = { "Armorer", "Farmer", "Overseer", "Technician", "Scientist" };

        for (int i = 0; i < npcWorkers.Length && i < 5; i++)
        {
            try
            {
                var npc = npcWorkers.GetObject(i);
                bool hired = false;
                try { hired = npc.GetBool("HiredWorker"); } catch { }
                if (hired)
                {
                    NpcWorkers.Add(new NpcWorkerViewModel
                    {
                        Name = workerNames[i],
                        Data = npc,
                        Index = i
                    });
                }
            }
            catch { }
        }

        if (NpcWorkers.Count > 0)
            SelectedNpc = NpcWorkers[0];
    }

    private void LoadChests(JsonObject playerState)
    {
        for (int i = 0; i < 10; i++)
        {
            string key = $"Chest{i + 1}Inventory";
            var inv = playerState.GetObject(key);

            var grid = new InventoryGridViewModel();
            grid.SetIsCargoInventory(true);
            grid.SetInventoryOwnerType("Chest");
            grid.SetInventoryGroup($"Chest {i + 1}");
            grid.SetSuperchargeDisabled(true);
            if (_database != null) grid.SetDatabase(_database);
            grid.SetIconManager(_iconManager);
            grid.LoadInventory(inv);

            ChestGrids.Add(grid);
        }
    }

    private void LoadStorage(JsonObject playerState)
    {
        (string Label, string Key)[] storageKeys =
        {
            ("Ingredient Storage", "CookingIngredientsInventory"),
            ("Corvette Parts", "CorvetteStorageInventory"),
            ("Salvage Capsule", "ChestMagicInventory"),
            ("Rocket Locker", "RocketLockerInventory"),
            ("Fishing Platform", "FishPlatformInventory"),
            ("Fish Bait Box", "FishBaitBoxInventory"),
            ("Food Unit", "FoodUnitInventory"),
            ("Freighter Refund", "ChestMagic2Inventory"),
        };

        foreach (var (label, key) in storageKeys)
        {
            var inv = playerState.GetObject(key);
            var grid = new InventoryGridViewModel();
            grid.SetIsCargoInventory(true);
            grid.SetInventoryOwnerType("Storage");
            grid.SetInventoryGroup(label);
            grid.SetSuperchargeDisabled(true);
            if (_database != null) grid.SetDatabase(_database);
            grid.SetIconManager(_iconManager);
            grid.LoadInventory(inv);

            StorageTabs.Add(new StorageTabViewModel
            {
                Label = label,
                Grid = grid
            });
        }
    }

    public Func<List<string>, Task<int>>? ShowObjectPickerFunc { get; set; }

    [RelayCommand]
    private async Task MoveBaseComputer()
    {
        if (SelectedBase?.Data == null) return;
        try
        {
            var objects = SelectedBase.Data.GetArray("Objects");
            if (objects == null || objects.Length == 0)
            {
                return;
            }

            var candidates = new List<(string id, JsonObject data, int index)>();
            for (int i = 0; i < objects.Length; i++)
            {
                try
                {
                    var obj = objects.GetObject(i);
                    string objectId = obj.GetString("ObjectID") ?? "";
                    if (!string.IsNullOrEmpty(objectId) && objectId != "^BASE_FLAG")
                        candidates.Add((objectId, obj, i));
                }
                catch { }
            }

            if (candidates.Count == 0 || ShowObjectPickerFunc == null) return;

            var displayNames = candidates.Select(c => c.id).ToList();
            int selectedIdx = await ShowObjectPickerFunc(displayNames);
            if (selectedIdx < 0 || selectedIdx >= candidates.Count) return;

            var target = candidates[selectedIdx];

            JsonObject? baseFlag = null;
            for (int i = 0; i < objects.Length; i++)
            {
                try
                {
                    var obj = objects.GetObject(i);
                    if (obj.GetString("ObjectID") == "^BASE_FLAG")
                    {
                        baseFlag = obj;
                        break;
                    }
                }
                catch { }
            }

            if (baseFlag == null) return;

            BaseLogic.SwapPositions(baseFlag, target.data);

            int objectCount = 0;
            try
            {
                var objs = SelectedBase.Data.GetArray("Objects");
                if (objs != null) objectCount = objs.Length;
            }
            catch { }
            BaseItemCount = objectCount.ToString();
        }
        catch { }
    }

    [RelayCommand]
    private async Task ExportBase()
    {
        if (SelectedBase?.Data == null || SaveFilePickerFunc == null) return;
        var cfg = ExportConfig.Instance;
        string? path = await SaveFilePickerFunc("Backup Base", "json",
            "NMS Base Backup (*.json)|*.json|All Files (*.*)|*.*");
        if (string.IsNullOrEmpty(path)) return;
        try { SelectedBase.Data.ExportToFile(path); } catch { }
    }

    [RelayCommand]
    private async Task ImportBase()
    {
        if (SelectedBase?.Data == null || OpenFilePickerFunc == null) return;
        string? path = await OpenFilePickerFunc("Restore Base",
            "NMS Base Backup (*.json)|*.json|All Files (*.*)|*.*");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var imported = JsonObject.ImportFromFile(path);
            if (imported.Contains("Objects"))
                SelectedBase.Data.Set("Objects", imported.Get("Objects"));
            if (imported.Contains("BaseVersion"))
                SelectedBase.Data.Set("BaseVersion", imported.Get("BaseVersion"));
            if (imported.Contains("Name"))
            {
                SelectedBase.Data.Set("Name", imported.Get("Name"));
                BaseName = imported.GetString("Name") ?? BaseName;
                SelectedBase.DisplayName = BaseName;
            }

            int objectCount = 0;
            try
            {
                var objects = SelectedBase.Data.GetArray("Objects");
                if (objects != null) objectCount = objects.Length;
            }
            catch { }
            BaseItemCount = objectCount.ToString();
        }
        catch { }
    }

    [RelayCommand]
    private void SaveBaseName()
    {
        if (SelectedBase?.Data == null) return;
        SelectedBase.Data.Set("Name", BaseName);
        SelectedBase.DisplayName = BaseName;
    }

    [RelayCommand]
    private void GenerateNpcSeed()
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        NpcSeed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
    }

    public override void SaveData(JsonObject saveData)
    {
        if (SelectedBase?.Data != null && !string.IsNullOrEmpty(BaseName))
            SelectedBase.Data.Set("Name", BaseName);
    }
}

public partial class BaseInfoViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName = "";
    public JsonObject? Data { get; set; }
    public int DataIndex { get; set; }
    public int ObjectCount { get; set; }
    public override string ToString() => DisplayName;
}

public partial class NpcWorkerViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    public JsonObject? Data { get; set; }
    public int Index { get; set; }
    public override string ToString() => Name;
}

public partial class StorageTabViewModel : ObservableObject
{
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private InventoryGridViewModel _grid = new();
}
