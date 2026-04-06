using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class RewardRowViewModel : ObservableObject
{
    [ObservableProperty] private string _rewardId = "";
    [ObservableProperty] private string _rewardName = "";
    [ObservableProperty] private bool _isUnlocked;

    public RewardRowViewModel(string id, string name, bool unlocked)
    {
        _rewardId = id;
        _rewardName = name;
        _isUnlocked = unlocked;
    }
}

public partial class AccountViewModel : PanelViewModelBase
{
    private JsonObject? _accountData;
    private string? _accountFilePath;
    private GameItemDatabase? _database;
    private string? _saveDirectory;
    private bool _rewardsDbLoaded;

    private readonly List<(string Id, string Name)> _seasonRewardsDb = new();
    private readonly List<(string Id, string Name)> _twitchRewardsDb = new();
    private readonly List<(string Id, string Name)> _platformRewardsDb = new();

    [ObservableProperty] private string _statusText = "Account rewards not loaded";
    [ObservableProperty] private ObservableCollection<RewardRowViewModel> _seasonRewards = new();
    [ObservableProperty] private ObservableCollection<RewardRowViewModel> _twitchRewards = new();
    [ObservableProperty] private ObservableCollection<RewardRowViewModel> _platformRewards = new();

    [ObservableProperty] private string _seasonFilter = "";
    [ObservableProperty] private string _twitchFilter = "";
    [ObservableProperty] private string _platformFilter = "";

    [ObservableProperty] private ObservableCollection<RewardRowViewModel> _filteredSeasonRewards = new();
    [ObservableProperty] private ObservableCollection<RewardRowViewModel> _filteredTwitchRewards = new();
    [ObservableProperty] private ObservableCollection<RewardRowViewModel> _filteredPlatformRewards = new();

    public JsonObject? AccountData => _accountData;
    public string? AccountFilePath => _accountFilePath;

    public void SetDatabase(GameItemDatabase db) => _database = db;
    public void SetSaveDirectory(string? dir) => _saveDirectory = dir;

    public void LoadRewardsDatabase(string? jsonDirectory = null)
    {
        _seasonRewardsDb.Clear();
        _twitchRewardsDb.Clear();
        _platformRewardsDb.Clear();

        if (!string.IsNullOrEmpty(jsonDirectory))
            RewardDatabase.LoadFromJsonDirectory(jsonDirectory);

        foreach (var reward in RewardDatabase.SeasonRewards)
            _seasonRewardsDb.Add((reward.Id, ResolveDisplayName(reward)));
        foreach (var reward in RewardDatabase.TwitchRewards)
            _twitchRewardsDb.Add((reward.Id, ResolveDisplayName(reward)));
        foreach (var reward in RewardDatabase.PlatformRewards)
            _platformRewardsDb.Add((reward.Id, ResolveDisplayName(reward)));

        _rewardsDbLoaded = true;
    }

    private string ResolveDisplayName(RewardEntry reward)
    {
        if (_database != null && !string.IsNullOrEmpty(reward.ProductId))
        {
            var item = _database.GetItem(reward.ProductId);
            if (item != null && !string.IsNullOrEmpty(item.Name))
                return item.Name;
        }
        return reward.Name;
    }

    public void LoadAccountFile(string saveDirectory)
    {
        SeasonRewards.Clear();
        TwitchRewards.Clear();
        PlatformRewards.Clear();
        _accountData = null;
        _accountFilePath = null;

        var data = AccountLogic.LoadAccountData(saveDirectory);
        if (data.ErrorMessage != null)
        {
            StatusText = data.ErrorMessage;
            return;
        }

        _accountData = data.AccountObject;
        _accountFilePath = data.AccountFilePath;

        PopulateRewardList(SeasonRewards, _seasonRewardsDb, data.SeasonUnlocked);
        PopulateRewardList(TwitchRewards, _twitchRewardsDb, data.TwitchUnlocked);
        PopulateRewardList(PlatformRewards, _platformRewardsDb, data.PlatformUnlocked);

        ApplyFilters();
        StatusText = data.StatusMessage ?? "";
    }

    private static void PopulateRewardList(ObservableCollection<RewardRowViewModel> target,
        List<(string Id, string Name)> rewardsDb, HashSet<string> unlocked)
    {
        target.Clear();
        var rows = AccountLogic.BuildRewardRows(rewardsDb, unlocked);
        foreach (var row in rows)
            target.Add(new RewardRowViewModel(row.Id, row.Name, row.Unlocked));
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;

        if (!_rewardsDbLoaded)
        {
            string jsonDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "json");
            LoadRewardsDatabase(jsonDir);
        }

        if (!string.IsNullOrEmpty(_saveDirectory))
            LoadAccountFile(_saveDirectory);
    }

    public override void SaveData(JsonObject saveData)
    {
        if (_accountData == null) return;

        var userSettings = _accountData.GetObject("UserSettingsData") ?? _accountData;

        var seasonRows = CollectRewardRows(SeasonRewards);
        var twitchRows = CollectRewardRows(TwitchRewards);
        var platformRows = CollectRewardRows(PlatformRewards);

        AccountLogic.SaveRewardList(seasonRows, userSettings, "UnlockedSeasonRewards");
        AccountLogic.SaveRewardList(twitchRows, userSettings, "UnlockedTwitchRewards");
        AccountLogic.SaveRewardList(platformRows, userSettings, "UnlockedPlatformRewards");

        AccountLogic.SyncRedeemedRewards(saveData, seasonRows, twitchRows);
    }

    private static List<(string Id, bool Unlocked)> CollectRewardRows(ObservableCollection<RewardRowViewModel> rewards)
    {
        var result = new List<(string Id, bool Unlocked)>();
        foreach (var row in rewards)
            result.Add((row.RewardId, row.IsUnlocked));
        return result;
    }

    [RelayCommand]
    private void UnlockAllSeason() => SetAll(SeasonRewards, true);

    [RelayCommand]
    private void LockAllSeason() => SetAll(SeasonRewards, false);

    [RelayCommand]
    private void UnlockAllTwitch() => SetAll(TwitchRewards, true);

    [RelayCommand]
    private void LockAllTwitch() => SetAll(TwitchRewards, false);

    [RelayCommand]
    private void UnlockAllPlatform() => SetAll(PlatformRewards, true);

    [RelayCommand]
    private void LockAllPlatform() => SetAll(PlatformRewards, false);

    private static void SetAll(ObservableCollection<RewardRowViewModel> rewards, bool value)
    {
        foreach (var row in rewards)
            row.IsUnlocked = value;
    }

    partial void OnSeasonFilterChanged(string value) => ApplyFilter(SeasonRewards, FilteredSeasonRewards, value);
    partial void OnTwitchFilterChanged(string value) => ApplyFilter(TwitchRewards, FilteredTwitchRewards, value);
    partial void OnPlatformFilterChanged(string value) => ApplyFilter(PlatformRewards, FilteredPlatformRewards, value);

    private void ApplyFilters()
    {
        ApplyFilter(SeasonRewards, FilteredSeasonRewards, SeasonFilter);
        ApplyFilter(TwitchRewards, FilteredTwitchRewards, TwitchFilter);
        ApplyFilter(PlatformRewards, FilteredPlatformRewards, PlatformFilter);
    }

    private static void ApplyFilter(ObservableCollection<RewardRowViewModel> source,
        ObservableCollection<RewardRowViewModel> target, string filterText)
    {
        target.Clear();
        foreach (var row in source)
        {
            if (string.IsNullOrWhiteSpace(filterText) ||
                row.RewardId.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                row.RewardName.Contains(filterText, StringComparison.OrdinalIgnoreCase))
            {
                target.Add(row);
            }
        }
    }
}
