using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class CompanionViewModel : PanelViewModelBase
{
    private JsonObject? _playerState;

    [ObservableProperty] private ObservableCollection<CompanionEntryViewModel> _companions = new();
    [ObservableProperty] private CompanionEntryViewModel? _selectedCompanion;
    [ObservableProperty] private bool _hasSelection;
    [ObservableProperty] private string _countLabel = "";

    [ObservableProperty] private string _companionName = "";
    [ObservableProperty] private string _creatureId = "";
    [ObservableProperty] private string _creatureSeed = "";
    [ObservableProperty] private string _secondarySeed = "";
    [ObservableProperty] private string _speciesSeed = "";
    [ObservableProperty] private string _genusSeed = "";
    [ObservableProperty] private string _biome = "";
    [ObservableProperty] private string _creatureType = "";
    [ObservableProperty] private bool _predator;
    [ObservableProperty] private string _scale = "";
    [ObservableProperty] private string _trust = "";
    [ObservableProperty] private bool _hasFur;
    [ObservableProperty] private string _helpfulness = "0";
    [ObservableProperty] private string _aggression = "0";
    [ObservableProperty] private string _independence = "0";
    [ObservableProperty] private string _hungry = "0";
    [ObservableProperty] private string _lonely = "0";
    [ObservableProperty] private string _customSpeciesName = "";
    [ObservableProperty] private bool _eggModified;
    [ObservableProperty] private bool _hasBeenSummoned;
    [ObservableProperty] private string _boneScaleSeed = "";
    [ObservableProperty] private string _colourBaseSeed = "";

    partial void OnSelectedCompanionChanged(CompanionEntryViewModel? value)
    {
        HasSelection = value != null;
        if (value != null) LoadCompanionDetails(value);
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        try
        {
            Companions.Clear();
            _playerState = null;

            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;
            _playerState = playerState;

            LoadSlots(playerState.GetArray("Pets"), "Pet");
            LoadSlots(playerState.GetArray("Eggs"), "Egg");

            CountLabel = $"Total slots: {Companions.Count}";

            if (Companions.Count > 0)
                SelectedCompanion = Companions[0];
        }
        catch { }
    }

    private void LoadSlots(JsonArray? array, string prefix)
    {
        if (array == null) return;
        for (int i = 0; i < array.Length; i++)
        {
            try
            {
                var comp = array.GetObject(i);
                bool occupied = false;
                try
                {
                    var seedArr = comp.GetArray("CreatureSeed");
                    if (seedArr != null && seedArr.Length >= 2)
                        occupied = seedArr.GetBool(0);
                }
                catch { }

                string customName = "";
                try { customName = comp.GetString("CustomName") ?? ""; } catch { }

                string label;
                if (!occupied)
                    label = $"{prefix} {i} (Empty)";
                else if (string.IsNullOrEmpty(customName) || customName == "^")
                    label = $"{prefix} {i}";
                else
                    label = $"{prefix} {i} - {customName}";

                Companions.Add(new CompanionEntryViewModel
                {
                    Label = label,
                    CompanionData = comp,
                    Source = prefix,
                    OriginalIndex = i,
                    IsOccupied = occupied
                });
            }
            catch { }
        }
    }

    private void LoadCompanionDetails(CompanionEntryViewModel entry)
    {
        try
        {
            var comp = entry.CompanionData;
            if (comp == null) return;

            CreatureId = comp.GetString("CreatureID") ?? "";
            CompanionName = comp.GetString("CustomName") ?? "";

            try
            {
                var seedArr = comp.GetArray("CreatureSeed");
                CreatureSeed = seedArr != null && seedArr.Length >= 2 ? seedArr.GetString(1) ?? "" : "";
            }
            catch { CreatureSeed = ""; }

            try
            {
                var secArr = comp.GetArray("CreatureSecondarySeed");
                SecondarySeed = secArr != null && secArr.Length >= 2 ? secArr.GetString(1) ?? "" : "";
            }
            catch { SecondarySeed = ""; }

            SpeciesSeed = comp.GetString("SpeciesSeed") ?? "";
            GenusSeed = comp.GetString("GenusSeed") ?? "";

            try { Predator = comp.GetBool("Predator"); } catch { Predator = false; }

            try
            {
                var biomeObj = comp.GetObject("Biome");
                Biome = biomeObj?.GetString("Biome") ?? "";
            }
            catch { Biome = ""; }

            try
            {
                var ctObj = comp.GetObject("CreatureType");
                CreatureType = ctObj?.GetString("CreatureType") ?? "";
            }
            catch { CreatureType = ""; }

            try { Scale = comp.GetDouble("Scale").ToString(); } catch { Scale = ""; }
            try { Trust = comp.GetDouble("Trust").ToString(); } catch { Trust = ""; }
            try { HasFur = comp.GetBool("HasFur"); } catch { HasFur = false; }

            try
            {
                var traits = comp.GetArray("Traits");
                Helpfulness = traits != null && traits.Length > 0 ? traits.GetDouble(0).ToString() : "0";
                Aggression = traits != null && traits.Length > 1 ? traits.GetDouble(1).ToString() : "0";
                Independence = traits != null && traits.Length > 2 ? traits.GetDouble(2).ToString() : "0";
            }
            catch { Helpfulness = "0"; Aggression = "0"; Independence = "0"; }

            try
            {
                var moods = comp.GetArray("Moods");
                Hungry = moods != null && moods.Length > 0 ? moods.GetDouble(0).ToString() : "0";
                Lonely = moods != null && moods.Length > 1 ? moods.GetDouble(1).ToString() : "0";
            }
            catch { Hungry = "0"; Lonely = "0"; }

            try
            {
                string csn = comp.GetString("CustomSpeciesName") ?? "";
                CustomSpeciesName = csn == "^" ? "" : csn.TrimStart('^');
            }
            catch { CustomSpeciesName = ""; }

            try { EggModified = comp.GetBool("EggModified"); } catch { EggModified = false; }
            try { HasBeenSummoned = comp.GetBool("HasBeenSummoned"); } catch { HasBeenSummoned = false; }

            try
            {
                var bsArr = comp.GetArray("BoneScaleSeed");
                BoneScaleSeed = bsArr != null && bsArr.Length >= 2 ? bsArr.GetString(1) ?? "" : "";
            }
            catch { BoneScaleSeed = ""; }

            try
            {
                var cbArr = comp.GetArray("ColourBaseSeed");
                ColourBaseSeed = cbArr != null && cbArr.Length >= 2 ? cbArr.GetString(1) ?? "" : "";
            }
            catch { ColourBaseSeed = ""; }
        }
        catch { }
    }

    [RelayCommand]
    private void SaveCompanionChanges()
    {
        if (SelectedCompanion?.CompanionData == null) return;
        var comp = SelectedCompanion.CompanionData;

        comp.Set("CustomName", CompanionName);
        comp.Set("CreatureID", CreatureId);
        comp.Set("Predator", Predator);
        comp.Set("HasFur", HasFur);
        comp.Set("EggModified", EggModified);
        comp.Set("HasBeenSummoned", HasBeenSummoned);

        if (double.TryParse(Scale, out double scaleVal)) comp.Set("Scale", scaleVal);
        if (double.TryParse(Trust, out double trustVal)) comp.Set("Trust", trustVal);

        var csn = string.IsNullOrEmpty(CustomSpeciesName) ? "^" : $"^{CustomSpeciesName.TrimStart('^')}";
        comp.Set("CustomSpeciesName", csn);

        try
        {
            var traits = comp.GetArray("Traits");
            if (traits != null)
            {
                if (double.TryParse(Helpfulness, out double h) && traits.Length > 0) traits.Set(0, h);
                if (double.TryParse(Aggression, out double a) && traits.Length > 1) traits.Set(1, a);
                if (double.TryParse(Independence, out double ind) && traits.Length > 2) traits.Set(2, ind);
            }
        }
        catch { }

        try
        {
            var moods = comp.GetArray("Moods");
            if (moods != null)
            {
                if (double.TryParse(Hungry, out double hu) && moods.Length > 0) moods.Set(0, hu);
                if (double.TryParse(Lonely, out double lo) && moods.Length > 1) moods.Set(1, lo);
            }
        }
        catch { }

        WriteSeed(comp, "CreatureSeed", CreatureSeed);
        WriteSeed(comp, "CreatureSecondarySeed", SecondarySeed);
        WriteSeed(comp, "BoneScaleSeed", BoneScaleSeed);
        WriteSeed(comp, "ColourBaseSeed", ColourBaseSeed);

        var normalized = SeedHelper.NormalizeSeed(SpeciesSeed);
        if (normalized != null) comp.Set("SpeciesSeed", normalized);
        normalized = SeedHelper.NormalizeSeed(GenusSeed);
        if (normalized != null) comp.Set("GenusSeed", normalized);
    }

    private static void WriteSeed(JsonObject comp, string key, string value)
    {
        var normalized = SeedHelper.NormalizeSeed(value);
        var arr = comp.GetArray(key);
        if (arr != null && arr.Length >= 2)
        {
            bool hasValue = normalized != null && normalized != "0x0";
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    [RelayCommand]
    private void GenerateSeed(string fieldName)
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        string seed = "0x" + BitConverter.ToString(bytes).Replace("-", "");

        switch (fieldName)
        {
            case "Creature": CreatureSeed = seed; break;
            case "Secondary": SecondarySeed = seed; break;
            case "Species": SpeciesSeed = seed; break;
            case "Genus": GenusSeed = seed; break;
            case "BoneScale": BoneScaleSeed = seed; break;
            case "ColourBase": ColourBaseSeed = seed; break;
        }
    }

    [RelayCommand]
    private void DeleteCompanion()
    {
        if (SelectedCompanion?.CompanionData == null) return;
        CompanionLogic.DeleteCompanion(SelectedCompanion.CompanionData);
        SelectedCompanion.IsOccupied = false;
        SelectedCompanion.Label = $"{SelectedCompanion.Source} {SelectedCompanion.OriginalIndex} (Empty)";
        HasSelection = false;
    }

    [RelayCommand]
    private void ResetAccessory()
    {
        if (SelectedCompanion?.CompanionData == null) return;
        CompanionLogic.ResetAccessoryCustomisation(SelectedCompanion.CompanionData);
    }

    [RelayCommand]
    private async Task ExportCompanion()
    {
        if (SelectedCompanion?.CompanionData == null || SaveFilePickerFunc == null) return;
        SaveCompanionChanges();
        var cfg = ExportConfig.Instance;
        string? path = await SaveFilePickerFunc("Export Companion", cfg.CompanionExt.TrimStart('.'),
            ExportConfig.BuildDialogFilter(cfg.CompanionExt, "Companion files"));
        if (string.IsNullOrEmpty(path)) return;
        try { CompanionLogic.ExportCompanion(SelectedCompanion.CompanionData, path); }
        catch { }
    }

    [RelayCommand]
    private async Task ImportCompanion()
    {
        if (_playerState == null || OpenFilePickerFunc == null) return;
        var cfg = ExportConfig.Instance;
        string? path = await OpenFilePickerFunc("Import Companion",
            ExportConfig.BuildImportFilter(cfg.CompanionExt, "Companion files", ".pet", ".cmp"));
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var pets = _playerState.GetArray("Pets");
            var eggs = _playerState.GetArray("Eggs");
            var target = pets ?? eggs;
            if (target == null) return;

            try { CompanionLogic.ImportCompanion(target, path); }
            catch (InvalidOperationException)
            {
                var fallback = target == pets ? eggs : pets;
                if (fallback != null) CompanionLogic.ImportCompanion(fallback, path);
                else return;
            }
        }
        catch { }
    }

    public override void SaveData(JsonObject saveData)
    {
        SaveCompanionChanges();
    }
}

public partial class CompanionEntryViewModel : ObservableObject
{
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private bool _isOccupied;
    public JsonObject? CompanionData { get; set; }
    public string Source { get; set; } = "";
    public int OriginalIndex { get; set; }

    public override string ToString() => Label;
}
