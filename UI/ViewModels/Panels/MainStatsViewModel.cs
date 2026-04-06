using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.IO;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class MainStatsViewModel : PanelViewModelBase
{
    private JsonObject? _saveData;
    private JsonObject? _playerState;
    private JsonObject? _accountData;
    private string? _saveFilePath;
    private IconManager? _iconManager;

    public event EventHandler? ReloadRequested;

    private static readonly string[] DifficultyPresets =
        { "Invalid", "Custom", "Normal", "Creative", "Relaxed", "Survival", "Permadeath" };

    private static readonly string[] GuideCategories =
        { "Survival Basics", "Getting Around", "Making Discoveries", "Upgrades & Crafting",
          "Construction", "Making Money", "Alien Lifeforms", "Combat" };

    [ObservableProperty] private decimal _health;
    [ObservableProperty] private decimal _shield;
    [ObservableProperty] private decimal _energy;
    [ObservableProperty] private decimal _units;
    [ObservableProperty] private decimal _nanites;
    [ObservableProperty] private decimal _quicksilver;

    [ObservableProperty] private string _saveName = "";
    [ObservableProperty] private string _saveSummary = "";
    [ObservableProperty] private string _playTime = "";
    [ObservableProperty] private string _lastSaveDate = "";
    [ObservableProperty] private string _accountName = "";
    [ObservableProperty] private bool _thirdPersonCamera;

    [ObservableProperty] private int _currentPresetIndex = -1;
    [ObservableProperty] private int _easiestPresetIndex = -1;
    [ObservableProperty] private int _hardestPresetIndex = -1;
    [ObservableProperty] private List<string> _presetItems = new(DifficultyPresets);

    [ObservableProperty] private string _galaxyDisplay = "";
    [ObservableProperty] private string _portalCode = "";
    [ObservableProperty] private string _portalCodeDec = "";
    [ObservableProperty] private string _signalBooster = "";
    [ObservableProperty] private string _distanceToCenter = "";
    [ObservableProperty] private string _jumpsToCenter = "";
    [ObservableProperty] private string _freighterInSystem = "";
    [ObservableProperty] private string _nexusInSystem = "";
    [ObservableProperty] private string _planetsInSystem = "";

    [ObservableProperty] private int _playerStateIndex = -1;
    [ObservableProperty] private List<string> _playerStateItems = new(CoordinateHelper.PlayerStates);
    [ObservableProperty] private bool _portalInterference;

    [ObservableProperty] private int _galaxyIndex;
    [ObservableProperty] private int _voxelX;
    [ObservableProperty] private int _voxelY;
    [ObservableProperty] private int _voxelZ;
    [ObservableProperty] private int _solarSystemIndex;
    [ObservableProperty] private int _planetIndex;
    [ObservableProperty] private string _portalHexInput = "";

    [ObservableProperty] private string _timeToNextBattle = "";
    [ObservableProperty] private int _warpsToNextBattle;

    [ObservableProperty] private string _statusText = "";

    // Save Utilities
    [ObservableProperty] private int _sourceSlotIndex;
    [ObservableProperty] private int _destSlotIndex = 1;
    [ObservableProperty] private int _transferPlatformIndex;
    public List<string> SlotItems { get; } = Enumerable.Range(1, 15).Select(i => $"Slot {i}").ToList();
    public List<string> PlatformItems { get; } = new() { "Steam", "GOG", "Xbox Game Pass", "PS4", "Switch" };

    // Guides
    [ObservableProperty] private ObservableCollection<GuideTopicViewModel> _guideTopics = new();
    [ObservableProperty] private string _guideFilter = "";

    // Titles
    [ObservableProperty] private ObservableCollection<TitleRowViewModel> _titleRows = new();

    public string PlayerName { get; private set; } = "Explorer";

    public void SetSaveFilePath(string? path) => _saveFilePath = path;

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _saveData = saveData;
        _iconManager = iconManager;
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;
            _playerState = playerState;

            Health = MainStatsLogic.ReadStatValue(playerState, "Health", 0, 999999);
            Shield = MainStatsLogic.ReadStatValue(playerState, "Shield", 0, 999999);
            Energy = MainStatsLogic.ReadStatValue(playerState, "Energy", 0, 999999);
            Units = MainStatsLogic.ReadStatValue(playerState, "Units", 0, uint.MaxValue);
            Nanites = MainStatsLogic.ReadStatValue(playerState, "Nanites", 0, uint.MaxValue);
            Quicksilver = MainStatsLogic.ReadStatValue(playerState, "Specials", 0, uint.MaxValue);

            try { SaveName = saveData.GetObject("CommonStateData")?.GetString("SaveName") ?? ""; } catch { }
            try { SaveSummary = playerState.GetString("SaveSummary") ?? ""; } catch { }
            try
            {
                int totalSeconds = saveData.GetObject("CommonStateData")?.GetInt("TotalPlayTime") ?? 0;
                var ts = TimeSpan.FromSeconds(totalSeconds);
                PlayTime = $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            }
            catch { PlayTime = ""; }

            try { ThirdPersonCamera = saveData.GetObject("CommonStateData")?.GetBool("UsesThirdPersonCharacterCam") ?? false; } catch { }

            try
            {
                string usn = "";
                var commonState = saveData.GetObject("CommonStateData");
                var owners = commonState?.GetArray("UsedDiscoveryOwnersV2");
                if (owners != null && owners.Length > 0)
                    usn = owners.GetObject(0)?.GetString("USN") ?? "";
                string displayName = string.IsNullOrEmpty(usn) ? "Explorer" : usn;
                AccountName = displayName;
                PlayerName = displayName;
            }
            catch { AccountName = "Explorer"; PlayerName = "Explorer"; }

            try
            {
                var diffState = playerState.GetObject("DifficultyState");
                if (diffState != null)
                {
                    CurrentPresetIndex = FindPresetIndex(diffState.GetObject("Preset")?.GetString("DifficultyPresetType"));
                    EasiestPresetIndex = FindPresetIndex(diffState.GetObject("EasiestUsedPreset")?.GetString("DifficultyPresetType"));
                    HardestPresetIndex = FindPresetIndex(diffState.GetObject("HardestUsedPreset")?.GetString("DifficultyPresetType"));
                }
            }
            catch { }

            LoadCoordinates(playerState, saveData);
            LoadSpaceBattle(playerState, saveData);
        }
        catch { }
    }

    private static int FindPresetIndex(string? value)
    {
        if (string.IsNullOrEmpty(value)) return -1;
        int idx = Array.IndexOf(DifficultyPresets, value);
        return idx >= 0 ? idx : -1;
    }

    private void LoadCoordinates(JsonObject playerState, JsonObject saveData)
    {
        try
        {
            var addr = playerState.GetObject("UniverseAddress");
            if (addr == null) return;

            int realityIndex = addr.GetInt("RealityIndex");
            string galaxyType = GalaxyDatabase.GetGalaxyType(realityIndex);
            GalaxyDisplay = $"{GalaxyDatabase.GetGalaxyDisplayName(realityIndex)} ({galaxyType})";

            var galactic = addr.GetObject("GalacticAddress");
            if (galactic == null) return;

            int vx = galactic.GetInt("VoxelX");
            int vy = galactic.GetInt("VoxelY");
            int vz = galactic.GetInt("VoxelZ");
            int si = galactic.GetInt("SolarSystemIndex");
            int pi = 0;
            try { pi = galactic.GetInt("PlanetIndex"); } catch { }

            PortalCode = CoordinateHelper.VoxelToPortalCode(vx, vy, vz, si, pi);
            PortalCodeDec = CoordinateHelper.PortalHexToDec(PortalCode);
            SignalBooster = CoordinateHelper.VoxelToSignalBooster(vx, vy, vz, si);

            GalaxyIndex = realityIndex;
            VoxelX = vx;
            VoxelY = vy;
            VoxelZ = vz;
            SolarSystemIndex = si;
            PlanetIndex = pi;

            try
            {
                var spawnState = saveData.GetObject("SpawnStateData");
                string lastState = spawnState?.GetString("LastKnownPlayerState") ?? "";
                int stateIdx = Array.IndexOf(CoordinateHelper.PlayerStates, lastState);
                PlayerStateIndex = stateIdx >= 0 ? stateIdx : -1;
            }
            catch { PlayerStateIndex = -1; }

            double dist = CoordinateHelper.GetDistanceToCenter(vx, vy, vz);
            DistanceToCenter = $"{dist:F0} ly";
            JumpsToCenter = CoordinateHelper.GetJumpsToCenter(dist, CoordinateHelper.DefaultHyperdriveRange).ToString();

            try
            {
                var freighterAddr = playerState.GetObject("FreighterUniverseAddress");
                bool freighterHere = false;
                if (freighterAddr != null)
                {
                    int fRealIdx = freighterAddr.GetInt("RealityIndex");
                    var fGal = freighterAddr.GetObject("GalacticAddress");
                    if (fGal != null && fRealIdx == realityIndex)
                        freighterHere = fGal.GetInt("VoxelX") == vx && fGal.GetInt("VoxelY") == vy
                            && fGal.GetInt("VoxelZ") == vz && fGal.GetInt("SolarSystemIndex") == si;
                }
                FreighterInSystem = freighterHere ? "Yes" : "No";
            }
            catch { FreighterInSystem = "Unknown"; }

            try
            {
                var nexusAddr = playerState.GetObject("NexusUniverseAddress");
                bool nexusHere = false;
                if (nexusAddr != null)
                {
                    int nRealIdx = nexusAddr.GetInt("RealityIndex");
                    var nGal = nexusAddr.GetObject("GalacticAddress");
                    if (nGal != null && nRealIdx == realityIndex)
                        nexusHere = nGal.GetInt("VoxelX") == vx && nGal.GetInt("VoxelY") == vy
                            && nGal.GetInt("VoxelZ") == vz && nGal.GetInt("SolarSystemIndex") == si;
                }
                NexusInSystem = nexusHere ? "Yes" : "No";
            }
            catch { NexusInSystem = "Unknown"; }

            try
            {
                var planetSeeds = playerState.GetArray("PlanetSeeds");
                int count = 0;
                if (planetSeeds != null)
                {
                    for (int i = 0; i < planetSeeds.Length; i++)
                    {
                        try
                        {
                            var seed = planetSeeds.GetArray(i);
                            if (seed != null && seed.Length >= 2 && seed.Get(1)?.ToString() != "0x0")
                                count++;
                        }
                        catch { }
                    }
                }
                PlanetsInSystem = count.ToString();
            }
            catch { PlanetsInSystem = "0"; }

            try { PortalInterference = playerState.GetBool("OnOtherSideOfPortal"); } catch { }
        }
        catch { }
    }

    private void LoadSpaceBattle(JsonObject playerState, JsonObject saveData)
    {
        try
        {
            int totalPlayTime = 0;
            try { totalPlayTime = saveData.GetObject("CommonStateData")?.GetInt("TotalPlayTime") ?? 0; } catch { }
            int timeLastBattle = 0;
            try { timeLastBattle = playerState.GetInt("TimeLastSpaceBattle"); } catch { }

            int timeRemaining = Math.Max(0, Math.Min(
                CoordinateHelper.SpaceBattleIntervalSeconds - (totalPlayTime - timeLastBattle),
                CoordinateHelper.SpaceBattleIntervalSeconds));
            var ts = TimeSpan.FromSeconds(timeRemaining);
            TimeToNextBattle = $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";

            int warpsLastBattle = 0;
            try { warpsLastBattle = playerState.GetInt("WarpsLastSpaceBattle"); } catch { }
            int totalWarps = 0;
            try
            {
                var statsGroups = playerState.GetArray("Stats");
                if (statsGroups != null)
                {
                    for (int i = 0; i < statsGroups.Length; i++)
                    {
                        var group = statsGroups.GetObject(i);
                        if (group.GetString("GroupId") == "^GLOBAL_STATS")
                        {
                            var stats = group.GetArray("Stats");
                            if (stats != null)
                            {
                                for (int j = 0; j < stats.Length; j++)
                                {
                                    var stat = stats.GetObject(j);
                                    if (stat.GetString("Id") == "^DIST_WARP")
                                    {
                                        totalWarps = stat.GetObject("Value")?.GetInt("IntValue") ?? 0;
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            catch { }

            WarpsToNextBattle = Math.Max(0, CoordinateHelper.SpaceBattleIntervalWarps - (totalWarps - warpsLastBattle));
        }
        catch { }
    }

    [RelayCommand]
    private void ApplyCoordinates()
    {
        if (_playerState == null) return;
        try
        {
            var addr = _playerState.GetObject("UniverseAddress");
            if (addr == null) return;

            addr.Set("RealityIndex", GalaxyIndex);
            var galactic = addr.GetObject("GalacticAddress");
            if (galactic == null) return;

            galactic.Set("VoxelX", VoxelX);
            galactic.Set("VoxelY", VoxelY);
            galactic.Set("VoxelZ", VoxelZ);
            galactic.Set("SolarSystemIndex", SolarSystemIndex);
            galactic.Set("PlanetIndex", PlanetIndex);
        }
        catch { }
        RefreshCoordinateDisplay();
    }

    [RelayCommand]
    private void ConvertPortalCode()
    {
        string portalCode = PortalHexInput.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(portalCode) || portalCode.Length != 12) return;

        if (!CoordinateHelper.PortalCodeToVoxel(portalCode, out int vx, out int vy, out int vz, out int si, out int pi))
            return;

        VoxelX = vx;
        VoxelY = vy;
        VoxelZ = vz;
        SolarSystemIndex = si;
        PlanetIndex = pi;
    }

    [RelayCommand]
    private void CoordinateRoulette()
    {
        const string hexChars = "0123456789ABCDEF";
        var portalChars = new char[12];
        for (int i = 0; i < 12; i++)
            portalChars[i] = hexChars[Random.Shared.Next(16)];
        string portalCode = new string(portalChars);
        int galaxy = Random.Shared.Next(256);

        if (!CoordinateHelper.PortalCodeToVoxel(portalCode, out int vx, out int vy, out int vz, out int si, out int pi))
            return;

        GalaxyIndex = galaxy;
        VoxelX = vx;
        VoxelY = vy;
        VoxelZ = vz;
        SolarSystemIndex = si;
        PlanetIndex = pi;

        ApplyCoordinates();
    }

    [RelayCommand]
    private void TriggerSpaceBattle()
    {
        if (_playerState == null) return;
        try
        {
            _playerState.Set("TimeLastSpaceBattle", 0);
            _playerState.Set("WarpsLastSpaceBattle", 0);
            WarpsToNextBattle = 0;
            TimeToNextBattle = "0h 0m 0s";
            StatusText = "Space battle triggered - warp to trigger!";
        }
        catch { }
    }

    private void RefreshCoordinateDisplay()
    {
        string galaxyType = GalaxyDatabase.GetGalaxyType(GalaxyIndex);
        GalaxyDisplay = $"{GalaxyDatabase.GetGalaxyDisplayName(GalaxyIndex)} ({galaxyType})";
        PortalCode = CoordinateHelper.VoxelToPortalCode(VoxelX, VoxelY, VoxelZ, SolarSystemIndex, PlanetIndex);
        PortalCodeDec = CoordinateHelper.PortalHexToDec(PortalCode);
        SignalBooster = CoordinateHelper.VoxelToSignalBooster(VoxelX, VoxelY, VoxelZ, SolarSystemIndex);

        double dist = CoordinateHelper.GetDistanceToCenter(VoxelX, VoxelY, VoxelZ);
        DistanceToCenter = $"{dist:F0} ly";
        JumpsToCenter = CoordinateHelper.GetJumpsToCenter(dist, CoordinateHelper.DefaultHyperdriveRange).ToString();
    }

    public override void SaveData(JsonObject saveData)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        MainStatsLogic.WriteStatValues(playerState, Health, Shield, Energy, Units, Nanites, Quicksilver);

        try { saveData.GetObject("CommonStateData")?.Set("SaveName", SaveName); } catch { }
        try { playerState.Set("SaveSummary", SaveSummary); } catch { }
        try { saveData.GetObject("CommonStateData")?.Set("UsesThirdPersonCharacterCam", ThirdPersonCamera); } catch { }

        try
        {
            var diffState = playerState.GetObject("DifficultyState");
            if (diffState != null)
            {
                if (CurrentPresetIndex >= 0 && CurrentPresetIndex < DifficultyPresets.Length)
                    diffState.GetObject("Preset")?.Set("DifficultyPresetType", DifficultyPresets[CurrentPresetIndex]);
                if (EasiestPresetIndex >= 0 && EasiestPresetIndex < DifficultyPresets.Length)
                    diffState.GetObject("EasiestUsedPreset")?.Set("DifficultyPresetType", DifficultyPresets[EasiestPresetIndex]);
                if (HardestPresetIndex >= 0 && HardestPresetIndex < DifficultyPresets.Length)
                    diffState.GetObject("HardestUsedPreset")?.Set("DifficultyPresetType", DifficultyPresets[HardestPresetIndex]);
            }
        }
        catch { }

        if (PlayerStateIndex >= 0)
        {
            try
            {
                var spawnState = saveData.GetObject("SpawnStateData");
                spawnState?.Set("LastKnownPlayerState", CoordinateHelper.PlayerStates[PlayerStateIndex]);
            }
            catch { }
        }

        try { playerState.Set("OnOtherSideOfPortal", PortalInterference); } catch { }

        try
        {
            var addr = playerState.GetObject("UniverseAddress");
            if (addr != null)
            {
                addr.Set("RealityIndex", GalaxyIndex);
                var galactic = addr.GetObject("GalacticAddress");
                if (galactic != null)
                {
                    galactic.Set("VoxelX", VoxelX);
                    galactic.Set("VoxelY", VoxelY);
                    galactic.Set("VoxelZ", VoxelZ);
                    galactic.Set("SolarSystemIndex", SolarSystemIndex);
                    galactic.Set("PlanetIndex", PlanetIndex);
                }
            }
        }
        catch { }
    }

    // --- Save Utilities ---

    private string? GetSaveDirectory() =>
        _saveFilePath != null ? Path.GetDirectoryName(_saveFilePath) : null;

    private SaveFileManager.Platform GetDetectedPlatform()
    {
        string? dir = GetSaveDirectory();
        return dir != null ? SaveFileManager.DetectPlatform(dir) : SaveFileManager.Platform.Unknown;
    }

    private static SaveFileManager.Platform TransferPlatformFromIndex(int index) => index switch
    {
        0 => SaveFileManager.Platform.Steam,
        1 => SaveFileManager.Platform.GOG,
        2 => SaveFileManager.Platform.XboxGamePass,
        3 => SaveFileManager.Platform.PS4,
        4 => SaveFileManager.Platform.Switch,
        _ => SaveFileManager.Platform.Unknown,
    };

    [RelayCommand]
    private void CopySlot()
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { StatusText = UiStrings.Get("player.no_save_loaded"); return; }
        if (SourceSlotIndex == DestSlotIndex) { StatusText = UiStrings.Get("player.slots_must_differ"); return; }
        try
        {
            SaveSlotManager.CopySlot(dir, SourceSlotIndex, DestSlotIndex, GetDetectedPlatform());
            StatusText = UiStrings.Format("player.copy_slot_success", SourceSlotIndex + 1, DestSlotIndex + 1);
        }
        catch (Exception ex) { StatusText = UiStrings.Format("player.copy_slot_failed", ex.Message); }
    }

    [RelayCommand]
    private void MoveSlot()
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { StatusText = UiStrings.Get("player.no_save_loaded"); return; }
        if (SourceSlotIndex == DestSlotIndex) { StatusText = UiStrings.Get("player.slots_must_differ"); return; }
        try
        {
            SaveSlotManager.MoveSlot(dir, SourceSlotIndex, DestSlotIndex, GetDetectedPlatform());
            StatusText = UiStrings.Format("player.move_slot_success", SourceSlotIndex + 1, DestSlotIndex + 1);
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { StatusText = UiStrings.Format("player.move_slot_failed", ex.Message); }
    }

    [RelayCommand]
    private void SwapSlots()
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { StatusText = UiStrings.Get("player.no_save_loaded"); return; }
        if (SourceSlotIndex == DestSlotIndex) { StatusText = UiStrings.Get("player.slots_must_differ"); return; }
        try
        {
            SaveSlotManager.SwapSlots(dir, SourceSlotIndex, DestSlotIndex, GetDetectedPlatform());
            StatusText = UiStrings.Format("player.swap_slot_success", SourceSlotIndex + 1, DestSlotIndex + 1);
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { StatusText = UiStrings.Format("player.swap_slot_failed", ex.Message); }
    }

    [RelayCommand]
    private void DeleteSlot()
    {
        string? dir = GetSaveDirectory();
        if (dir == null) { StatusText = UiStrings.Get("player.no_save_loaded"); return; }
        try
        {
            SaveSlotManager.DeleteSlot(dir, SourceSlotIndex, GetDetectedPlatform());
            StatusText = UiStrings.Format("player.delete_slot_success", SourceSlotIndex + 1);
            ReloadRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { StatusText = UiStrings.Format("player.delete_slot_failed", ex.Message); }
    }

    public Func<Task<string?>>? PickFolderFunc { get; set; }

    [RelayCommand]
    private async Task TransferPlatform()
    {
        if (_saveFilePath == null) { StatusText = UiStrings.Get("player.no_save_loaded"); return; }
        if (PickFolderFunc == null) return;

        string? destDir = await PickFolderFunc();
        if (string.IsNullOrEmpty(destDir)) return;

        var destPlatform = TransferPlatformFromIndex(TransferPlatformIndex);
        try
        {
            SaveSlotManager.TransferCrossPlatform(_saveFilePath, destDir, DestSlotIndex, destPlatform);
            StatusText = UiStrings.Get("player.transfer_cross_complete");
        }
        catch (Exception ex) { StatusText = UiStrings.Format("player.transfer_cross_failed", ex.Message); }
    }

    // --- Guides ---

    public void LoadAccountData(JsonObject accountData)
    {
        _accountData = accountData;
        LoadGuides(accountData);
        LoadTitles(accountData);
    }

    private void LoadGuides(JsonObject accountData)
    {
        GuideTopics.Clear();
        try
        {
            var userData = accountData.GetObject("UserSettingsData");
            if (userData == null) return;

            var seenSet = new HashSet<string>(StringComparer.Ordinal);
            var unlockedSet = new HashSet<string>(StringComparer.Ordinal);

            var seenTopics = userData.GetArray("SeenWikiTopics");
            var unlockedTopics = userData.GetArray("UnlockedWikiTopics");

            if (seenTopics != null)
                for (int i = 0; i < seenTopics.Length; i++)
                    try { seenSet.Add(seenTopics.GetString(i)); } catch { }
            if (unlockedTopics != null)
                for (int i = 0; i < unlockedTopics.Length; i++)
                    try { unlockedSet.Add(unlockedTopics.GetString(i)); } catch { }

            var shown = new HashSet<string>(StringComparer.Ordinal);
            foreach (var topic in WikiGuideDatabase.Topics)
            {
                shown.Add(topic.Id);
                string category = WikiGuideDatabase.GetEnglishCategory(topic.Id);
                GuideTopics.Add(new GuideTopicViewModel
                {
                    TopicId = topic.Id,
                    Name = topic.Name,
                    Category = category,
                    IsSeen = seenSet.Contains(topic.Id),
                    IsUnlocked = unlockedSet.Contains(topic.Id)
                });
            }

            foreach (string topicId in seenSet.Union(unlockedSet))
            {
                if (!shown.Contains(topicId) && !string.IsNullOrEmpty(topicId))
                {
                    shown.Add(topicId);
                    GuideTopics.Add(new GuideTopicViewModel
                    {
                        TopicId = topicId,
                        Name = WikiGuideDatabase.GetTopicName(topicId),
                        Category = WikiGuideDatabase.GetEnglishCategory(topicId),
                        IsSeen = seenSet.Contains(topicId),
                        IsUnlocked = unlockedSet.Contains(topicId)
                    });
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private void UnlockAllGuides()
    {
        foreach (var t in GuideTopics) { t.IsSeen = true; t.IsUnlocked = true; }
        SyncGuidesToAccount();
    }

    [RelayCommand]
    private void LockAllGuides()
    {
        foreach (var t in GuideTopics) { t.IsSeen = false; t.IsUnlocked = false; }
        SyncGuidesToAccount();
    }

    public void SyncGuidesToAccount()
    {
        if (_accountData == null) return;
        var userData = _accountData.GetObject("UserSettingsData");
        if (userData == null) return;

        var seenArr = userData.GetArray("SeenWikiTopics");
        var unlockedArr = userData.GetArray("UnlockedWikiTopics");
        if (seenArr == null || unlockedArr == null) return;

        while (seenArr.Length > 0) seenArr.RemoveAt(seenArr.Length - 1);
        while (unlockedArr.Length > 0) unlockedArr.RemoveAt(unlockedArr.Length - 1);

        foreach (var topic in GuideTopics)
        {
            if (topic.IsSeen) seenArr.Add(topic.TopicId);
            if (topic.IsUnlocked) unlockedArr.Add(topic.TopicId);
        }
    }

    // --- Titles ---

    private void LoadTitles(JsonObject accountData)
    {
        TitleRows.Clear();
        if (!TitleDatabase.IsLoaded) return;

        try
        {
            var userData = accountData.GetObject("UserSettingsData") ?? accountData;
            var unlockedTitles = userData.GetArray("UnlockedTitles");
            var unlockedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (unlockedTitles != null)
            {
                for (int i = 0; i < unlockedTitles.Length; i++)
                {
                    string? titleId = ExtractStringValue(unlockedTitles.Get(i));
                    if (!string.IsNullOrEmpty(titleId))
                    {
                        if (titleId.StartsWith('^'))
                            titleId = titleId[1..];
                        unlockedSet.Add(titleId);
                    }
                }
            }

            foreach (var title in TitleDatabase.Titles)
            {
                TitleRows.Add(new TitleRowViewModel
                {
                    TitleId = title.Id,
                    TitleName = string.Format(title.Name, PlayerName),
                    Description = title.UnlockDescription,
                    IsUnlocked = unlockedSet.Contains(title.Id)
                });
            }
        }
        catch { }
    }

    private static string? ExtractStringValue(object? value)
    {
        if (value is string s) return s;
        if (value is BinaryData bin) return Encoding.Latin1.GetString(bin.ToByteArray());
        return value?.ToString();
    }

    [RelayCommand]
    private void UnlockAllTitles()
    {
        foreach (var t in TitleRows) t.IsUnlocked = true;
        SyncTitlesToAccount();
    }

    [RelayCommand]
    private void LockAllTitles()
    {
        foreach (var t in TitleRows) t.IsUnlocked = false;
        SyncTitlesToAccount();
    }

    public void SyncTitlesToAccount()
    {
        if (_accountData == null) return;
        var userData = _accountData.GetObject("UserSettingsData") ?? _accountData;
        var unlockedTitles = userData.GetArray("UnlockedTitles");
        if (unlockedTitles == null) return;

        while (unlockedTitles.Length > 0) unlockedTitles.RemoveAt(unlockedTitles.Length - 1);

        foreach (var row in TitleRows)
        {
            if (row.IsUnlocked)
                unlockedTitles.Add("^" + row.TitleId);
        }
    }
}

public partial class GuideTopicViewModel : ObservableObject
{
    [ObservableProperty] private string _topicId = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _category = "";
    [ObservableProperty] private bool _isSeen;
    [ObservableProperty] private bool _isUnlocked;
}

public partial class TitleRowViewModel : ObservableObject
{
    [ObservableProperty] private string _titleId = "";
    [ObservableProperty] private string _titleName = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private bool _isUnlocked;
}
