using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class ConfigFieldViewModel : ObservableObject
{
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private string _value = "";

    public string Key { get; init; } = "";
}

public partial class ExportConfigViewModel : PanelViewModelBase
{
    public ObservableCollection<ConfigFieldViewModel> ExtensionFields { get; } = new();
    public ObservableCollection<ConfigFieldViewModel> TemplateFields { get; } = new();

    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isStatusSuccess;

    public string? ConfigFilePath { get; set; }

    private static readonly string[] ExtensionLabels =
    [
        "Exosuit", "Multi-tool", "Starship", "Corvette", "Corvette Snapshot",
        "Starship Cargo", "Starship Tech",
        "Freighter", "Freighter Cargo", "Freighter Tech",
        "Frigate", "Squadron",
        "Exocraft", "Exocraft Cargo", "Exocraft Tech",
        "Companion", "Base", "Chest", "Storage",
        "Discovery", "Settlement", "ByteBeat"
    ];

    private static readonly string[] TemplateLabels =
    [
        "Exosuit Cargo", "Exosuit Tech",
        "Multi-tool", "Starship", "Corvette", "Corvette Snapshot",
        "Starship Cargo", "Starship Tech",
        "Freighter", "Freighter Cargo", "Freighter Tech",
        "Frigate", "Squadron",
        "Exocraft", "Exocraft Cargo", "Exocraft Tech",
        "Companion", "Base", "Chest", "Storage",
        "Discovery", "Settlement", "ByteBeat"
    ];

    public static string HelpText { get; } = """
        Template Variables:

            {player_name}       - Player name
            {ship_name}         - Ship / freighter name
            {multitool_name}    - Multi-tool name
            {type}              - Type display name (ship type, frigate type, etc.)
            {class}             - Class letter (S/A/B/C)
            {race}              - NPC race
            {rank}              - Pilot rank
            {seed}              - Seed value
            {name}              - Generic name
            {species}           - Companion species
            {creature_seed}     - Companion creature seed
            {vehicle_name}      - Exocraft name
            {vehicle_type}      - Exocraft type
            {base_name}         - Base name
            {chest_number}      - Chest slot number
            {timestamp}         - Epoch timestamp
            {frigate_name}      - Frigate name
            {freighter_name}    - Freighter name
            {settlement_name}   - Settlement name

        File Extensions:

            Extensions must start with a dot (e.g. ".nmsship").
            These are used for Save/Open file dialogs alongside
            standard .json and all-files filters.

            The naming template is combined with the extension to
            produce the default filename shown in export dialogs.
            Invalid filename characters are automatically removed.
        """;

    public ExportConfigViewModel()
    {
        foreach (var label in ExtensionLabels)
            ExtensionFields.Add(new ConfigFieldViewModel { Label = label, Key = label });

        foreach (var label in TemplateLabels)
            TemplateFields.Add(new ConfigFieldViewModel { Label = label, Key = label });
    }

    public void LoadConfig()
    {
        var cfg = ExportConfig.Instance;

        SetExt("Exosuit", cfg.ExosuitExt);
        SetExt("Multi-tool", cfg.MultitoolExt);
        SetExt("Starship", cfg.StarshipExt);
        SetExt("Corvette", cfg.CorvetteExt);
        SetExt("Corvette Snapshot", cfg.CorvetteSnapshotExt);
        SetExt("Starship Cargo", cfg.StarshipCargoExt);
        SetExt("Starship Tech", cfg.StarshipTechExt);
        SetExt("Freighter", cfg.FreighterExt);
        SetExt("Freighter Cargo", cfg.FreighterCargoExt);
        SetExt("Freighter Tech", cfg.FreighterTechExt);
        SetExt("Frigate", cfg.FrigateExt);
        SetExt("Squadron", cfg.SquadronExt);
        SetExt("Exocraft", cfg.ExocraftExt);
        SetExt("Exocraft Cargo", cfg.ExocraftCargoExt);
        SetExt("Exocraft Tech", cfg.ExocraftTechExt);
        SetExt("Companion", cfg.CompanionExt);
        SetExt("Base", cfg.BaseExt);
        SetExt("Chest", cfg.ChestExt);
        SetExt("Storage", cfg.StorageExt);
        SetExt("Discovery", cfg.DiscoveryExt);
        SetExt("Settlement", cfg.SettlementExt);
        SetExt("ByteBeat", cfg.ByteBeatExt);

        SetTpl("Exosuit Cargo", cfg.ExosuitCargoTemplate);
        SetTpl("Exosuit Tech", cfg.ExosuitTechTemplate);
        SetTpl("Multi-tool", cfg.MultitoolTemplate);
        SetTpl("Starship", cfg.StarshipTemplate);
        SetTpl("Corvette", cfg.CorvetteTemplate);
        SetTpl("Corvette Snapshot", cfg.CorvetteSnapshotTemplate);
        SetTpl("Starship Cargo", cfg.StarshipCargoTemplate);
        SetTpl("Starship Tech", cfg.StarshipTechTemplate);
        SetTpl("Freighter", cfg.FreighterTemplate);
        SetTpl("Freighter Cargo", cfg.FreighterCargoTemplate);
        SetTpl("Freighter Tech", cfg.FreighterTechTemplate);
        SetTpl("Frigate", cfg.FrigateTemplate);
        SetTpl("Squadron", cfg.SquadronTemplate);
        SetTpl("Exocraft", cfg.ExocraftTemplate);
        SetTpl("Exocraft Cargo", cfg.ExocraftCargoTemplate);
        SetTpl("Exocraft Tech", cfg.ExocraftTechTemplate);
        SetTpl("Companion", cfg.CompanionTemplate);
        SetTpl("Base", cfg.BaseTemplate);
        SetTpl("Chest", cfg.ChestTemplate);
        SetTpl("Storage", cfg.StorageTemplate);
        SetTpl("Discovery", cfg.DiscoveryTemplate);
        SetTpl("Settlement", cfg.SettlementTemplate);
        SetTpl("ByteBeat", cfg.ByteBeatTemplate);
    }

    private void ApplyConfig()
    {
        var cfg = ExportConfig.Instance;

        cfg.ExosuitExt = GetExt("Exosuit", cfg.ExosuitExt);
        cfg.MultitoolExt = GetExt("Multi-tool", cfg.MultitoolExt);
        cfg.StarshipExt = GetExt("Starship", cfg.StarshipExt);
        cfg.CorvetteExt = GetExt("Corvette", cfg.CorvetteExt);
        cfg.CorvetteSnapshotExt = GetExt("Corvette Snapshot", cfg.CorvetteSnapshotExt);
        cfg.StarshipCargoExt = GetExt("Starship Cargo", cfg.StarshipCargoExt);
        cfg.StarshipTechExt = GetExt("Starship Tech", cfg.StarshipTechExt);
        cfg.FreighterExt = GetExt("Freighter", cfg.FreighterExt);
        cfg.FreighterCargoExt = GetExt("Freighter Cargo", cfg.FreighterCargoExt);
        cfg.FreighterTechExt = GetExt("Freighter Tech", cfg.FreighterTechExt);
        cfg.FrigateExt = GetExt("Frigate", cfg.FrigateExt);
        cfg.SquadronExt = GetExt("Squadron", cfg.SquadronExt);
        cfg.ExocraftExt = GetExt("Exocraft", cfg.ExocraftExt);
        cfg.ExocraftCargoExt = GetExt("Exocraft Cargo", cfg.ExocraftCargoExt);
        cfg.ExocraftTechExt = GetExt("Exocraft Tech", cfg.ExocraftTechExt);
        cfg.CompanionExt = GetExt("Companion", cfg.CompanionExt);
        cfg.BaseExt = GetExt("Base", cfg.BaseExt);
        cfg.ChestExt = GetExt("Chest", cfg.ChestExt);
        cfg.StorageExt = GetExt("Storage", cfg.StorageExt);
        cfg.DiscoveryExt = GetExt("Discovery", cfg.DiscoveryExt);
        cfg.SettlementExt = GetExt("Settlement", cfg.SettlementExt);
        cfg.ByteBeatExt = GetExt("ByteBeat", cfg.ByteBeatExt);

        cfg.ExosuitCargoTemplate = GetTpl("Exosuit Cargo", cfg.ExosuitCargoTemplate);
        cfg.ExosuitTechTemplate = GetTpl("Exosuit Tech", cfg.ExosuitTechTemplate);
        cfg.MultitoolTemplate = GetTpl("Multi-tool", cfg.MultitoolTemplate);
        cfg.StarshipTemplate = GetTpl("Starship", cfg.StarshipTemplate);
        cfg.CorvetteTemplate = GetTpl("Corvette", cfg.CorvetteTemplate);
        cfg.CorvetteSnapshotTemplate = GetTpl("Corvette Snapshot", cfg.CorvetteSnapshotTemplate);
        cfg.StarshipCargoTemplate = GetTpl("Starship Cargo", cfg.StarshipCargoTemplate);
        cfg.StarshipTechTemplate = GetTpl("Starship Tech", cfg.StarshipTechTemplate);
        cfg.FreighterTemplate = GetTpl("Freighter", cfg.FreighterTemplate);
        cfg.FreighterCargoTemplate = GetTpl("Freighter Cargo", cfg.FreighterCargoTemplate);
        cfg.FreighterTechTemplate = GetTpl("Freighter Tech", cfg.FreighterTechTemplate);
        cfg.FrigateTemplate = GetTpl("Frigate", cfg.FrigateTemplate);
        cfg.SquadronTemplate = GetTpl("Squadron", cfg.SquadronTemplate);
        cfg.ExocraftTemplate = GetTpl("Exocraft", cfg.ExocraftTemplate);
        cfg.ExocraftCargoTemplate = GetTpl("Exocraft Cargo", cfg.ExocraftCargoTemplate);
        cfg.ExocraftTechTemplate = GetTpl("Exocraft Tech", cfg.ExocraftTechTemplate);
        cfg.CompanionTemplate = GetTpl("Companion", cfg.CompanionTemplate);
        cfg.BaseTemplate = GetTpl("Base", cfg.BaseTemplate);
        cfg.ChestTemplate = GetTpl("Chest", cfg.ChestTemplate);
        cfg.StorageTemplate = GetTpl("Storage", cfg.StorageTemplate);
        cfg.DiscoveryTemplate = GetTpl("Discovery", cfg.DiscoveryTemplate);
        cfg.SettlementTemplate = GetTpl("Settlement", cfg.SettlementTemplate);
        cfg.ByteBeatTemplate = GetTpl("ByteBeat", cfg.ByteBeatTemplate);
    }

    [RelayCommand]
    private void SaveSettings()
    {
        var warnings = ValidateExtensions();
        ApplyConfig();

        if (ConfigFilePath != null)
        {
            try
            {
                ExportConfig.Instance.SaveToFile(ConfigFilePath);
                IsStatusSuccess = true;
                StatusText = warnings.Count > 0
                    ? $"Settings saved. {string.Join(" ", warnings)}"
                    : "Settings saved.";
            }
            catch (Exception ex)
            {
                IsStatusSuccess = false;
                StatusText = $"Save failed: {ex.Message}";
            }
        }
        else
        {
            IsStatusSuccess = true;
            StatusText = "Settings applied (no save path configured).";
        }
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        ExportConfig.SetInstance(new ExportConfig());
        LoadConfig();
        IsStatusSuccess = true;
        StatusText = "Settings reset to defaults.";
    }

    private List<string> ValidateExtensions()
    {
        var warnings = new List<string>();
        foreach (var field in ExtensionFields)
        {
            string val = field.Value.Trim();
            if (string.IsNullOrWhiteSpace(val)) continue;
            if (!val.StartsWith('.'))
            {
                field.Value = "." + val;
                warnings.Add($"{field.Label} extension was missing leading dot (auto-corrected).");
            }
        }
        return warnings;
    }

    private void SetExt(string key, string value)
    {
        var field = ExtensionFields.FirstOrDefault(f => f.Key == key);
        if (field != null) field.Value = value;
    }

    private string GetExt(string key, string fallback)
    {
        var field = ExtensionFields.FirstOrDefault(f => f.Key == key);
        return field != null && !string.IsNullOrWhiteSpace(field.Value) ? field.Value.Trim() : fallback;
    }

    private void SetTpl(string key, string value)
    {
        var field = TemplateFields.FirstOrDefault(f => f.Key == key);
        if (field != null) field.Value = value;
    }

    private string GetTpl(string key, string fallback)
    {
        var field = TemplateFields.FirstOrDefault(f => f.Key == key);
        return field != null && !string.IsNullOrWhiteSpace(field.Value) ? field.Value.Trim() : fallback;
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        LoadConfig();
    }

    public override void SaveData(JsonObject saveData) { }
}
