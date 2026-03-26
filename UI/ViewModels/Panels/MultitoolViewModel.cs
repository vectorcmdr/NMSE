using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.ViewModels.Controls;

namespace NMSE.UI.ViewModels.Panels;

public partial class MultitoolViewModel : PanelViewModelBase
{
    private JsonArray? _multitools;
    private JsonObject? _playerState;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private int _activeToolIndex;

    [ObservableProperty] private ObservableCollection<string> _toolList = new();
    [ObservableProperty] private int _selectedToolIndex = -1;
    [ObservableProperty] private string _primaryToolLabel = "";

    private readonly List<int> _toolDataIndices = new();

    [ObservableProperty] private string _toolName = "";
    [ObservableProperty] private string _toolSeed = "";
    [ObservableProperty] private ObservableCollection<string> _toolTypes = new();
    [ObservableProperty] private int _selectedTypeIndex = -1;
    [ObservableProperty] private ObservableCollection<string> _toolClasses = new(MultitoolLogic.ToolClasses);
    [ObservableProperty] private int _selectedClassIndex = -1;

    [ObservableProperty] private double _damage;
    [ObservableProperty] private double _mining;
    [ObservableProperty] private double _scan;

    [ObservableProperty] private InventoryGridViewModel _storeGrid = new();

    private MultitoolLogic.ToolTypeItem[] _typeItems = [];

    public MultitoolViewModel()
    {
        StoreGrid.SetIsTechInventory(true);
        StoreGrid.SetInventoryOwnerType("Weapon");
        StoreGrid.SetInventoryGroup("Weapon");
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        _iconManager = iconManager;
        StoreGrid.SetDatabase(database);
        StoreGrid.SetIconManager(iconManager);

        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _multitools = _playerState.GetArray("Multitools");

            RefreshTypeItems();

            if (_multitools != null && _multitools.Length > 0)
            {
                _activeToolIndex = 0;
                try { _activeToolIndex = _playerState.GetInt("ActiveMultioolIndex"); } catch { }
                PrimaryToolLabel = MultitoolLogic.GetPrimaryToolName(_multitools, _activeToolIndex);

                RefreshToolList();

                if (ToolList.Count > 0)
                {
                    int selectIdx = 0;
                    for (int i = 0; i < _toolDataIndices.Count; i++)
                    {
                        if (_toolDataIndices[i] == _activeToolIndex)
                        {
                            selectIdx = i;
                            break;
                        }
                    }
                    SelectedToolIndex = Math.Clamp(selectIdx, 0, ToolList.Count - 1);
                }
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

            var multitools = playerState.GetArray("Multitools");
            if (multitools != null && SelectedToolIndex >= 0 && SelectedToolIndex < _toolDataIndices.Count)
            {
                int idx = _toolDataIndices[SelectedToolIndex];
                if (idx >= multitools.Length) return;

                try { playerState.Set("ActiveMultioolIndex", _activeToolIndex); } catch { }

                var tool = multitools.GetObject(idx);
                bool isPrimary = (idx == _activeToolIndex);

                var values = new MultitoolLogic.ToolSaveValues
                {
                    Name = ToolName,
                    ClassIndex = SelectedClassIndex,
                    TypeIndex = GetSelectedTypeDataIndex(),
                    Seed = ToolSeed,
                    Damage = Damage,
                    Mining = Mining,
                    Scan = Scan
                };

                MultitoolLogic.SaveToolData(tool, playerState, values, isPrimary);
            }
        }
        catch { }
    }

    partial void OnSelectedToolIndexChanged(int value)
    {
        if (value < 0 || value >= _toolDataIndices.Count) return;
        if (_multitools == null) return;

        try
        {
            int idx = _toolDataIndices[value];
            if (idx >= _multitools.Length) return;

            var tool = _multitools.GetObject(idx);
            var data = MultitoolLogic.LoadToolData(tool);

            ToolName = data.Name;
            SelectTypeByDataIndex(data.TypeIndex);
            SelectedClassIndex = data.ClassIndex;
            ToolSeed = data.Seed;
            Damage = data.Damage;
            Mining = data.Mining;
            Scan = data.Scan;

            StoreGrid.LoadInventory(data.Store);
        }
        catch { }
    }

    [RelayCommand]
    private void GenerateSeed()
    {
        ToolSeed = $"0x{Random.Shared.NextInt64():X16}";
    }

    [RelayCommand]
    private void MakePrimary()
    {
        if (_multitools == null || _playerState == null || SelectedToolIndex < 0 || SelectedToolIndex >= _toolDataIndices.Count) return;
        int idx = _toolDataIndices[SelectedToolIndex];
        if (idx >= _multitools.Length) return;

        _activeToolIndex = idx;
        try { _playerState.Set("ActiveMultioolIndex", _activeToolIndex); } catch { }
        PrimaryToolLabel = MultitoolLogic.GetPrimaryToolName(_multitools, _activeToolIndex);
    }

    private void RefreshToolList()
    {
        ToolList.Clear();
        _toolDataIndices.Clear();
        if (_multitools == null) return;
        foreach (var item in MultitoolLogic.BuildToolList(_multitools))
        {
            ToolList.Add(item.DisplayName);
            _toolDataIndices.Add(item.DataIndex);
        }
    }

    private void RefreshTypeItems()
    {
        _typeItems = MultitoolLogic.GetToolTypeItems();
        ToolTypes.Clear();
        foreach (var item in _typeItems)
            ToolTypes.Add(item.DisplayName);
    }

    private int GetSelectedTypeDataIndex()
    {
        if (SelectedTypeIndex < 0 || SelectedTypeIndex >= _typeItems.Length) return -1;
        string internalName = _typeItems[SelectedTypeIndex].InternalName;
        return Array.FindIndex(MultitoolLogic.ToolTypes, t => t.Name.Equals(internalName, StringComparison.OrdinalIgnoreCase));
    }

    private void SelectTypeByDataIndex(int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= MultitoolLogic.ToolTypes.Length) { SelectedTypeIndex = -1; return; }
        string targetName = MultitoolLogic.ToolTypes[typeIndex].Name;
        for (int i = 0; i < _typeItems.Length; i++)
        {
            if (_typeItems[i].InternalName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
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
    private async Task ExportTool()
    {
        if (_multitools == null || SelectedToolIndex < 0 || SaveFileFunc == null) return;
        int idx = _toolDataIndices[SelectedToolIndex];
        if (idx >= _multitools.Length) return;

        var tool = _multitools.GetObject(idx);
        if (tool == null) return;

        var cfg = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["multitool_name"] = ToolName,
            ["type"] = ToolTypes.Count > 0 && SelectedTypeIndex >= 0 ? ToolTypes[SelectedTypeIndex] : "",
            ["class"] = ToolClasses.Count > 0 && SelectedClassIndex >= 0 ? ToolClasses[SelectedClassIndex] : ""
        };
        string fileName = ExportConfig.BuildFileName(cfg.MultitoolTemplate, cfg.MultitoolExt, vars);
        var path = await SaveFileFunc(fileName, cfg.MultitoolExt);
        if (path != null)
            tool.ExportToFile(path);
    }

    [RelayCommand]
    private async Task ImportTool()
    {
        if (_multitools == null || SelectedToolIndex < 0 || OpenFileFunc == null) return;
        int idx = _toolDataIndices[SelectedToolIndex];
        if (idx >= _multitools.Length) return;

        var path = await OpenFileFunc(ExportConfig.Instance.MultitoolExt);
        if (path == null) return;

        var imported = JsonObject.ImportFromFile(path);
        if (imported == null) return;

        _multitools.Set(idx, imported);
        OnSelectedToolIndexChanged(SelectedToolIndex);
    }

    [RelayCommand]
    private void DeleteTool()
    {
        if (_multitools == null || SelectedToolIndex < 0) return;
        int idx = _toolDataIndices[SelectedToolIndex];
        if (idx >= _multitools.Length) return;

        // Clear the tool by removing it from array
        _multitools.RemoveAt(idx);
        RefreshToolList();
        if (ToolList.Count > 0)
            SelectedToolIndex = 0;
    }
}
