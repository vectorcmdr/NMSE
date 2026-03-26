using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class SquadronPilotViewModel : ObservableObject
{
    [ObservableProperty] private string _displayText = "";
    public int Index { get; set; }
    public JsonObject? Data { get; set; }
    public override string ToString() => DisplayText;
}

public partial class SquadronViewModel : ObservableObject
{
    private JsonArray? _pilots;
    private JsonArray? _unlockedSlots;
    private bool _loading;

    private static readonly string[] ShipTypeNames = SquadronLogic.ShipTypeToResource.Keys.ToArray();

    [ObservableProperty] private ObservableCollection<SquadronPilotViewModel> _pilotList = new();
    [ObservableProperty] private SquadronPilotViewModel? _selectedPilot;
    [ObservableProperty] private string _countLabel = "";
    [ObservableProperty] private bool _hasSelection;

    [ObservableProperty] private int _raceIndex = -1;
    [ObservableProperty] private int _rankIndex = -1;
    [ObservableProperty] private int _shipTypeIndex = -1;
    [ObservableProperty] private bool _slotUnlocked;

    [ObservableProperty] private string _npcSeed = "";
    [ObservableProperty] private string _shipSeed = "";
    [ObservableProperty] private string _traitsSeed = "";
    [ObservableProperty] private string _npcResource = "";
    [ObservableProperty] private string _shipResource = "";

    [ObservableProperty] private List<string> _raceItems = new(
        SquadronLogic.PilotRaces.Select(SquadronLogic.GetLocalisedPilotRaceName));
    [ObservableProperty] private List<string> _rankItems = new(SquadronLogic.PilotRanks);
    [ObservableProperty] private List<string> _shipTypeItems = new(
        SquadronLogic.GetShipTypeItems().Select(i => i.DisplayName));

    private StarshipLogic.ShipTypeItem[] _shipTypeItemData = SquadronLogic.GetShipTypeItems();

    partial void OnSelectedPilotChanged(SquadronPilotViewModel? value)
    {
        HasSelection = value != null;
        if (value != null) LoadPilotDetails(value);
    }

    partial void OnRaceIndexChanged(int value)
    {
        if (_loading) return;
        var pilot = SelectedPilot?.Data;
        if (pilot == null || value < 0 || value >= SquadronLogic.PilotRaces.Length) return;
        SquadronLogic.SetPilotRace(pilot, SquadronLogic.PilotRaces[value]);
        RefreshListEntry();
    }

    partial void OnRankIndexChanged(int value)
    {
        if (_loading) return;
        var pilot = SelectedPilot?.Data;
        if (pilot == null || value < 0) return;
        try { pilot.Set("PilotRank", value); } catch { }
        RefreshListEntry();
    }

    partial void OnShipTypeIndexChanged(int value)
    {
        if (_loading) return;
        var pilot = SelectedPilot?.Data;
        if (pilot == null || value < 0 || value >= _shipTypeItemData.Length) return;
        string internalName = _shipTypeItemData[value].InternalName;
        if (SquadronLogic.ShipTypeToResource.TryGetValue(internalName, out string? resource))
        {
            try { pilot.GetObject("ShipResource")?.Set("Filename", resource); } catch { }
            ShipResource = resource ?? "";
        }
        RefreshListEntry();
    }

    partial void OnSlotUnlockedChanged(bool value)
    {
        if (_loading || SelectedPilot == null || _unlockedSlots == null) return;
        int idx = SelectedPilot.Index;
        if (idx >= 0 && idx < _unlockedSlots.Length)
            _unlockedSlots.Set(idx, value);
    }

    public void LoadData(JsonObject saveData)
    {
        PilotList.Clear();
        HasSelection = false;
        _pilots = null;
        _unlockedSlots = null;

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _pilots = playerState.GetArray("SquadronPilots");
            try { _unlockedSlots = playerState.GetArray("SquadronUnlockedPilotSlots"); } catch { }

            if (_pilots == null || _pilots.Length == 0)
            {
                CountLabel = UiStrings.Get("squadron.no_pilots_found");
                return;
            }

            RefreshList();
        }
        catch { CountLabel = UiStrings.Get("squadron.load_failed"); }
    }

    public void SaveData()
    {
        ApplyCurrentPilotEdits();
    }

    private void RefreshList()
    {
        if (_pilots == null) return;
        var sel = SelectedPilot;
        PilotList.Clear();

        for (int i = 0; i < _pilots.Length; i++)
        {
            try
            {
                var p = _pilots.GetObject(i);
                PilotList.Add(new SquadronPilotViewModel
                {
                    DisplayText = SquadronLogic.GetPilotDisplayName(p, i),
                    Index = i,
                    Data = p
                });
            }
            catch
            {
                PilotList.Add(new SquadronPilotViewModel
                {
                    DisplayText = $"Pilot {i + 1}",
                    Index = i
                });
            }
        }

        CountLabel = UiStrings.Format("squadron.total_pilots", _pilots.Length);
    }

    private void LoadPilotDetails(SquadronPilotViewModel item)
    {
        _loading = true;
        try
        {
            var pilot = item.Data;
            if (pilot == null) return;

            string race = SquadronLogic.GetPilotRace(pilot);
            RaceIndex = Array.IndexOf(SquadronLogic.PilotRaces, race);

            try { RankIndex = Math.Clamp(pilot.GetInt("PilotRank"), 0, 3); }
            catch { RankIndex = 0; }

            string shipType = SquadronLogic.GetShipType(pilot);
            ShipTypeIndex = -1;
            for (int j = 0; j < _shipTypeItemData.Length; j++)
            {
                if (_shipTypeItemData[j].InternalName.Equals(shipType, StringComparison.OrdinalIgnoreCase))
                {
                    ShipTypeIndex = j;
                    break;
                }
            }

            NpcSeed = SquadronLogic.ReadSeed(pilot, "NPCResource");
            ShipSeed = SquadronLogic.ReadSeed(pilot, "ShipResource");
            TraitsSeed = pilot.GetString("TraitsSeed") ?? "";

            try { NpcResource = pilot.GetObject("NPCResource")?.GetString("Filename") ?? ""; }
            catch { NpcResource = ""; }
            try { ShipResource = pilot.GetObject("ShipResource")?.GetString("Filename") ?? ""; }
            catch { ShipResource = ""; }

            try
            {
                int slotIdx = item.Index;
                SlotUnlocked = _unlockedSlots != null && slotIdx >= 0 && slotIdx < _unlockedSlots.Length
                    && _unlockedSlots.GetBool(slotIdx);
            }
            catch { SlotUnlocked = false; }
        }
        catch { }
        finally { _loading = false; }
    }

    private void ApplyCurrentPilotEdits()
    {
        if (_loading || SelectedPilot?.Data == null) return;
        var pilot = SelectedPilot.Data;

        SquadronLogic.WriteSeed(pilot, "NPCResource", NpcSeed);
        SquadronLogic.WriteSeed(pilot, "ShipResource", ShipSeed);

        var normalizedTraits = SeedHelper.NormalizeSeed(TraitsSeed);
        if (normalizedTraits != null)
            try { pilot.Set("TraitsSeed", normalizedTraits); } catch { }
    }

    private void RefreshListEntry()
    {
        if (SelectedPilot == null || _pilots == null) return;
        int idx = SelectedPilot.Index;
        if (idx < 0 || idx >= _pilots.Length || idx >= PilotList.Count) return;
        try
        {
            var p = _pilots.GetObject(idx);
            PilotList[idx].DisplayText = SquadronLogic.GetPilotDisplayName(p, idx);
        }
        catch { }
    }

    [RelayCommand]
    private void DeletePilot()
    {
        if (SelectedPilot?.Data == null) return;
        SquadronLogic.DeletePilot(SelectedPilot.Data);
        RefreshList();
        HasSelection = false;
        SelectedPilot = null;
    }

    [RelayCommand]
    private void GenerateNpcSeed()
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        NpcSeed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        if (!_loading && SelectedPilot?.Data != null)
            SquadronLogic.WriteSeed(SelectedPilot.Data, "NPCResource", NpcSeed);
    }

    [RelayCommand]
    private void GenerateShipSeed()
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        ShipSeed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        if (!_loading && SelectedPilot?.Data != null)
            SquadronLogic.WriteSeed(SelectedPilot.Data, "ShipResource", ShipSeed);
    }

    [RelayCommand]
    private void GenerateTraitsSeed()
    {
        byte[] bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        TraitsSeed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        if (!_loading && SelectedPilot?.Data != null)
        {
            var normalized = SeedHelper.NormalizeSeed(TraitsSeed);
            if (normalized != null)
                try { SelectedPilot.Data.Set("TraitsSeed", normalized); } catch { }
        }
    }

    [RelayCommand]
    private void Export() { /* TODO: implement squadron export */ }

    [RelayCommand]
    private void Import() { /* TODO: implement squadron import */ }
}
