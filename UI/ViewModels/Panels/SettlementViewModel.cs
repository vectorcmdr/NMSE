using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class SettlementViewModel : PanelViewModelBase
{
    private JsonArray? _settlements;
    private readonly List<int> _filteredIndices = new();
    private GameItemDatabase? _database;

    [ObservableProperty] private ObservableCollection<string> _settlementNames = new();
    [ObservableProperty] private int _selectedSettlementIndex = -1;
    [ObservableProperty] private string _infoLabel = "";

    [ObservableProperty] private string _settlementName = "";
    [ObservableProperty] private string _seedValue = "";
    [ObservableProperty] private bool _hasSelection;

    [ObservableProperty] private int _population;
    [ObservableProperty] private int _happiness;
    [ObservableProperty] private int _productivity;
    [ObservableProperty] private int _upkeep;
    [ObservableProperty] private int _sentinels;
    [ObservableProperty] private int _debt;
    [ObservableProperty] private int _alert;
    [ObservableProperty] private int _bugAttack;

    [ObservableProperty] private int _decisionTypeIndex = -1;
    [ObservableProperty] private List<string> _decisionTypes = new(SettlementLogic.DecisionTypes);

    [ObservableProperty] private ObservableCollection<ProductionItemViewModel> _productionItems = new();

    partial void OnSelectedSettlementIndexChanged(int value)
    {
        if (value < 0 || value >= _filteredIndices.Count || _settlements == null)
        {
            HasSelection = false;
            return;
        }

        HasSelection = true;
        int dataIdx = _filteredIndices[value];
        if (dataIdx >= _settlements.Length) return;

        var settlement = _settlements.GetObject(dataIdx);
        var sdata = SettlementLogic.LoadSettlementData(settlement);

        SettlementName = sdata.Name;
        SeedValue = sdata.SeedValue;
        Population = sdata.Stats[0];
        Happiness = sdata.Stats[1];
        Productivity = sdata.Stats[2];
        Upkeep = sdata.Stats[3];
        Sentinels = sdata.Stats[4];
        Debt = sdata.Stats[5];
        Alert = sdata.Stats[6];
        BugAttack = sdata.Stats[7];
        DecisionTypeIndex = sdata.DecisionTypeIndex;

        LoadProductionState(settlement);
    }

    private void LoadProductionState(JsonObject settlement)
    {
        ProductionItems.Clear();
        var prodArr = settlement.GetArray("ProductionState");
        if (prodArr == null) return;

        for (int i = 0; i < prodArr.Length; i++)
        {
            try
            {
                var prodObj = prodArr.GetObject(i);
                string elementId = prodObj.GetString("ElementId") ?? prodObj.Get("ElementId")?.ToString() ?? "";
                string lookupId = elementId.StartsWith('^') ? elementId[1..] : elementId;
                var dbItem = string.IsNullOrEmpty(lookupId) ? null : _database?.GetItem(lookupId);
                string itemName = dbItem?.Name ?? lookupId;
                int amount = 0;
                try { amount = prodObj.GetInt("Amount"); } catch { }

                ProductionItems.Add(new ProductionItemViewModel
                {
                    ElementId = elementId,
                    ItemName = itemName,
                    Amount = amount
                });
            }
            catch { }
        }
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        SettlementNames.Clear();
        _filteredIndices.Clear();
        HasSelection = false;

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _settlements = playerState.GetArray("SettlementStatesV2");
            if (_settlements == null || _settlements.Length == 0)
            {
                InfoLabel = "No settlements found.";
                return;
            }

            var filtered = SettlementLogic.FilterSettlements(saveData, playerState, _settlements);
            foreach (int i in filtered)
            {
                try
                {
                    _filteredIndices.Add(i);
                    var settlement = _settlements.GetObject(i);
                    string name = settlement.GetString("Name") ?? $"Settlement {i + 1}";
                    SettlementNames.Add(name);
                }
                catch
                {
                    _filteredIndices.Add(i);
                    SettlementNames.Add($"Settlement {i + 1}");
                }
            }

            InfoLabel = $"Found {_filteredIndices.Count} settlement(s).";
            if (SettlementNames.Count > 0)
                SelectedSettlementIndex = 0;
        }
        catch { InfoLabel = "Failed to load settlements."; }
    }

    [RelayCommand]
    private void GenerateSeed()
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        SeedValue = "0x" + BitConverter.ToString(bytes).Replace("-", "");
    }

    [RelayCommand]
    private void DeleteSettlement()
    {
        if (_settlements == null || SelectedSettlementIndex < 0 || _filteredIndices.Count == 0) return;

        int selIdx = SelectedSettlementIndex;
        int dataIdx = _filteredIndices[selIdx];
        if (dataIdx >= _settlements.Length) return;

        SettlementLogic.RemoveSettlement(_settlements, dataIdx);

        _filteredIndices.RemoveAt(selIdx);
        for (int i = 0; i < _filteredIndices.Count; i++)
        {
            if (_filteredIndices[i] > dataIdx)
                _filteredIndices[i]--;
        }

        SettlementNames.RemoveAt(selIdx);
        if (SettlementNames.Count > 0)
            SelectedSettlementIndex = Math.Min(selIdx, SettlementNames.Count - 1);
        else
            HasSelection = false;
    }

    [RelayCommand]
    private async Task ExportSettlement()
    {
        if (_settlements == null || SelectedSettlementIndex < 0 || _filteredIndices.Count == 0 || SaveFilePickerFunc == null) return;
        int dataIdx = _filteredIndices[SelectedSettlementIndex];
        if (dataIdx >= _settlements.Length) return;

        var settlement = _settlements.GetObject(dataIdx);
        var cfg = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["settlement_name"] = SettlementName,
            ["seed"] = SeedValue
        };
        string defaultName = ExportConfig.BuildFileName(cfg.SettlementTemplate, "", vars);
        string? path = await SaveFilePickerFunc("Export Settlement", cfg.SettlementExt.TrimStart('.'),
            ExportConfig.BuildDialogFilter(cfg.SettlementExt, "Settlement files"));
        if (string.IsNullOrEmpty(path)) return;
        try { settlement.ExportToFile(path); } catch { }
    }

    [RelayCommand]
    private async Task ImportSettlement()
    {
        if (_settlements == null || OpenFilePickerFunc == null) return;
        string? path = await OpenFilePickerFunc("Import Settlement",
            ExportConfig.BuildImportFilter(ExportConfig.Instance.SettlementExt, "Settlement files", ".stl"));
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var imported = JsonObject.ImportFromFile(path);
            imported = InventoryImportHelper.UnwrapNomNom(imported, "Settlement");

            int selectedDataIdx = (SelectedSettlementIndex >= 0 && _filteredIndices.Count > 0)
                ? _filteredIndices[SelectedSettlementIndex] : -1;
            int target = SettlementLogic.FindImportTargetIndex(_settlements, selectedDataIdx);

            if (target == -2)
                target = selectedDataIdx >= 0 ? selectedDataIdx : 0;

            if (target == -1)
            {
                _settlements.Add(imported);
            }
            else if (target >= 0 && target < _settlements.Length)
            {
                var existing = _settlements.GetObject(target);
                foreach (var propName in imported.Names())
                    existing.Set(propName, imported.Get(propName));
            }

            ReloadSettlementList();
        }
        catch { }
    }

    private void ReloadSettlementList()
    {
        SettlementNames.Clear();
        _filteredIndices.Clear();
        HasSelection = false;
        if (_settlements == null) return;

        for (int i = 0; i < _settlements.Length; i++)
        {
            try
            {
                _filteredIndices.Add(i);
                var settlement = _settlements.GetObject(i);
                string name = settlement.GetString("Name") ?? $"Settlement {i + 1}";
                SettlementNames.Add(name);
            }
            catch
            {
                _filteredIndices.Add(i);
                SettlementNames.Add($"Settlement {i + 1}");
            }
        }

        InfoLabel = $"Found {_filteredIndices.Count} settlement(s).";
        if (SettlementNames.Count > 0)
            SelectedSettlementIndex = 0;
    }

    public override void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var settlements = playerState.GetArray("SettlementStatesV2");
            if (settlements == null || SelectedSettlementIndex < 0 || _filteredIndices.Count == 0) return;

            int dataIdx = _filteredIndices[SelectedSettlementIndex];
            if (dataIdx >= settlements.Length) return;

            var settlement = settlements.GetObject(dataIdx);

            var saveValues = new SettlementLogic.SettlementSaveValues
            {
                Name = SettlementName,
                SeedValue = SeedValue,
                DecisionTypeIndex = DecisionTypeIndex,
            };
            saveValues.Stats[0] = Population;
            saveValues.Stats[1] = Happiness;
            saveValues.Stats[2] = Productivity;
            saveValues.Stats[3] = Upkeep;
            saveValues.Stats[4] = Sentinels;
            saveValues.Stats[5] = Debt;
            saveValues.Stats[6] = Alert;
            saveValues.Stats[7] = BugAttack;

            SettlementLogic.SaveSettlementData(settlement, saveValues);

            var prodArr = settlement.GetArray("ProductionState");
            if (prodArr != null)
            {
                for (int i = 0; i < ProductionItems.Count && i < prodArr.Length; i++)
                {
                    var prodObj = prodArr.GetObject(i);
                    prodObj.Set("ElementId", ProductionItems[i].ElementId);
                    prodObj.Set("Amount", Math.Clamp(ProductionItems[i].Amount, 0, SettlementLogic.ProductionMaxAmount));
                }
            }
        }
        catch { }
    }
}

public partial class ProductionItemViewModel : ObservableObject
{
    [ObservableProperty] private string _elementId = "";
    [ObservableProperty] private string _itemName = "";
    [ObservableProperty] private int _amount;
}
