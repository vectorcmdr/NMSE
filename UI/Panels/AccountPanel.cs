using NMSE.Data;
using NMSE.Models;
using NMSE.Core;
using NMSE.IO;

namespace NMSE.UI.Panels;

public partial class AccountPanel : UserControl
{
    private JsonObject? _accountData;
    private string? _accountFilePath;
    private string? _mxmlFilePath;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    /// <summary>The loaded account data object, or null if not loaded.</summary>
    public JsonObject? AccountData => _accountData;
    /// <summary>The file path of the loaded account data.</summary>
    public string? AccountFilePath => _accountFilePath;
    /// <summary>The file path of the MXML settings file for platform rewards, or null if not set.</summary>
    public string? MxmlFilePath => _mxmlFilePath;

    // Rewards database from rewards.xml
    private readonly List<(string Id, string Name)> _seasonRewardsDb = new();
    private readonly List<(string Id, string Name)> _twitchRewardsDb = new();
    private readonly List<(string Id, string Name)> _platformRewardsDb = new();
    // Maps reward Id -> ProductId for item database lookups (icon, name, description)
    private readonly Dictionary<string, string> _productIdMap = new(StringComparer.OrdinalIgnoreCase);

    public AccountPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void SetDatabase(GameItemDatabase db) => _database = db;
    public void SetIconManager(IconManager? mgr) => _iconManager = mgr;

    /// <summary>
    /// Load the rewards database. Tries to load from Rewards.json in the database directory
    /// first; falls back to inline static data if not found.
    /// When a GameItemDatabase is available, resolves display names via ProductId lookups.
    /// </summary>
    public void LoadRewardsDatabase(string? jsonDirectory = null)
    {
        _seasonRewardsDb.Clear();
        _twitchRewardsDb.Clear();
        _platformRewardsDb.Clear();
        _productIdMap.Clear();

        // Try loading from JSON directory if provided
        if (!string.IsNullOrEmpty(jsonDirectory))
            RewardDatabase.LoadFromJsonDirectory(jsonDirectory);

        foreach (var reward in RewardDatabase.SeasonRewards)
        {
            _seasonRewardsDb.Add((reward.Id, ResolveDisplayName(reward)));
            StoreProductId(reward);
        }
        foreach (var reward in RewardDatabase.TwitchRewards)
        {
            _twitchRewardsDb.Add((reward.Id, ResolveDisplayName(reward)));
            StoreProductId(reward);
        }
        foreach (var reward in RewardDatabase.PlatformRewards)
        {
            _platformRewardsDb.Add((reward.Id, ResolveDisplayName(reward)));
            StoreProductId(reward);
        }
    }

    /// <summary>
    /// Re-resolves display names for all cached rewards using the current (potentially
    /// localised) GameItemDatabase and RewardEntry data. Call this after a language
    /// change so that the reward grids display localised names.
    /// </summary>
    public void RefreshRewardNames()
    {
        RefreshList(_seasonRewardsDb, RewardDatabase.SeasonRewards);
        RefreshList(_twitchRewardsDb, RewardDatabase.TwitchRewards);
        RefreshList(_platformRewardsDb, RewardDatabase.PlatformRewards);

        void RefreshList(List<(string Id, string Name)> cache, IEnumerable<RewardEntry> source)
        {
            cache.Clear();
            foreach (var reward in source)
                cache.Add((reward.Id, ResolveDisplayName(reward)));
        }
    }

    /// <summary>
    /// Resolves a display name for a reward entry by looking up the ProductId
    /// in the game item database. Falls back to the reward's own Name field.
    /// </summary>
    private string ResolveDisplayName(RewardEntry reward)
    {
        if (_database != null && !string.IsNullOrEmpty(reward.ProductId))
        {
            // Use ProductId directly - GetItem handles ^-prefix stripping internally.
            // ProductId references the actual game item (e.g. "B_STR_AA_N" in Corvette.json).
            var item = _database.GetItem(reward.ProductId);
            if (item != null && !string.IsNullOrEmpty(item.Name))
                return item.Name;
        }
        return reward.Name;
    }

    /// <summary>
    /// Stores the ProductId mapping for a reward, used for icon lookups.
    /// </summary>
    private void StoreProductId(RewardEntry reward)
    {
        if (!string.IsNullOrEmpty(reward.ProductId))
            _productIdMap[reward.Id] = reward.ProductId;
    }

    private static void SetAllUnlocked(DataGridView grid, bool value)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (!row.Visible) continue;
            row.Cells["Unlocked"].Value = value;
        }
    }

    private static void ApplyFilter(DataGridView grid, string filterText)
    {
        try
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (string.IsNullOrWhiteSpace(filterText))
                {
                    row.Visible = true;
                    continue;
                }

                string id = row.Cells["RewardId"].Value?.ToString() ?? "";
                string name = row.Cells["RewardName"].Value?.ToString() ?? "";
                row.Visible = id.Contains(filterText, StringComparison.OrdinalIgnoreCase)
                           || name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (InvalidOperationException)
        {
            // Grid may not be fully initialised yet (silently ignore)
        }
    }

    public void LoadAccountFile(string saveDirectory)
    {
        _seasonGrid.Rows.Clear();
        _twitchGrid.Rows.Clear();
        _platformGrid.Rows.Clear();
        _accountData = null;
        _accountFilePath = null;

        var data = AccountLogic.LoadAccountData(saveDirectory);
        if (data.ErrorMessage != null)
        {
            _statusLabel.Text = data.ErrorMessage;
            return;
        }

        _accountData = data.AccountObject;
        _accountFilePath = data.AccountFilePath;

        // Auto-detect MXML path if not already set
        if (string.IsNullOrEmpty(_mxmlFilePath))
        {
            var detected = MxmlRewardEditor.AutoDetectMxmlPath();
            if (detected != null)
                SetMxmlPath(detected);
        }

        // Platform rewards require BOTH accountdata AND MXML to be present.
        // Only show as unlocked if the reward exists in both sources.
        var platformUnlocked = data.PlatformUnlocked;
        if (!string.IsNullOrEmpty(_mxmlFilePath))
        {
            var mxmlRewards = MxmlRewardEditor.ReadUnlockedRewards(_mxmlFilePath);
            // Intersect: only keep rewards that are in both accountdata and MXML
            platformUnlocked = new HashSet<string>(
                platformUnlocked.Where(id => mxmlRewards.Contains(id)),
                StringComparer.OrdinalIgnoreCase);
        }

        PopulateRewardGrid(_seasonGrid, _seasonRewardsDb, data.SeasonUnlocked);
        PopulateRewardGrid(_twitchGrid, _twitchRewardsDb, data.TwitchUnlocked);
        PopulateRewardGrid(_platformGrid, _platformRewardsDb, platformUnlocked);

        _statusLabel.Text = data.StatusMessage ?? "";
    }

    /// <summary>
    /// Loads account data from an Xbox Game Pass AccountData blob.
    /// This is the Xbox equivalent of <see cref="LoadAccountFile"/>.
    /// </summary>
    /// <param name="accountSlot">The Xbox slot info for the AccountData entry.</param>
    public void LoadXboxAccountData(XboxSlotInfo accountSlot)
    {
        _seasonGrid.Rows.Clear();
        _twitchGrid.Rows.Clear();
        _platformGrid.Rows.Clear();
        _accountData = null;
        _accountFilePath = null;

        var data = AccountLogic.LoadXboxAccountData(accountSlot);
        if (data.ErrorMessage != null)
        {
            _statusLabel.Text = data.ErrorMessage;
            return;
        }

        _accountData = data.AccountObject;
        _accountFilePath = data.AccountFilePath;

        // Auto-detect MXML path if not already set
        if (string.IsNullOrEmpty(_mxmlFilePath))
        {
            var detected = MxmlRewardEditor.AutoDetectMxmlPath();
            if (detected != null)
                SetMxmlPath(detected);
        }

        var platformUnlocked = data.PlatformUnlocked;
        if (!string.IsNullOrEmpty(_mxmlFilePath))
        {
            var mxmlRewards = MxmlRewardEditor.ReadUnlockedRewards(_mxmlFilePath);
            platformUnlocked = new HashSet<string>(
                platformUnlocked.Where(id => mxmlRewards.Contains(id)),
                StringComparer.OrdinalIgnoreCase);
        }

        PopulateRewardGrid(_seasonGrid, _seasonRewardsDb, data.SeasonUnlocked);
        PopulateRewardGrid(_twitchGrid, _twitchRewardsDb, data.TwitchUnlocked);
        PopulateRewardGrid(_platformGrid, _platformRewardsDb, platformUnlocked);

        _statusLabel.Text = data.StatusMessage ?? "";
    }

    private void PopulateRewardGrid(DataGridView grid, List<(string Id, string Name)> rewardsDb, HashSet<string> unlocked)
    {
        grid.Rows.Clear();
        var rows = AccountLogic.BuildRewardRows(rewardsDb, unlocked);
        foreach (var row in rows)
        {
            Image? icon = GetRewardIcon(row.Id, row.Name);
            grid.Rows.Add((object?)icon ?? DBNull.Value, row.Id, row.Name, row.Unlocked);
        }
    }

    private Image? GetRewardIcon(string rewardId, string rewardName)
    {
        if (_iconManager == null) return null;

        // Try lookup by ProductId first (the actual game item ID for display)
        if (_productIdMap.TryGetValue(rewardId, out var productId) && !string.IsNullOrEmpty(productId))
        {
            var icon = _iconManager.GetIconForItem(productId, _database);
            if (icon != null) return icon;
        }

        // Try direct lookup by reward ID
        var directIcon = _iconManager.GetIconForItem(rewardId, _database);
        if (directIcon != null) return directIcon;

        // Search items by name match
        if (_database != null && !string.IsNullOrEmpty(rewardName))
        {
            foreach (var item in _database.Items.Values)
            {
                if (item.Name.Equals(rewardName, StringComparison.OrdinalIgnoreCase)
                    || item.NameLower.Equals(rewardName, StringComparison.OrdinalIgnoreCase))
                {
                    var icon = _iconManager.GetIconForItem(item.Id, _database);
                    if (icon != null) return icon;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// LoadData stub.
    /// The real loading is done via LoadAccountFile().
    /// </summary>
    public void LoadData(JsonObject saveData)
    {
        // Stub for uniformity.
        // Account rewards are in accountdata.hg, not in the save file.
        // LoadAccountFile() handles the actual loading.
    }

    /// <summary>
    /// Syncs the current grid state to the in-memory account data object.
    /// Also syncs redeemed rewards to the game save data.
    /// Additionally writes platform rewards to the MXML file if configured.
    /// Does NOT write to disk for account data - that happens in MainForm.OnSave().
    /// </summary>
    public void SaveData(JsonObject saveData)
    {
        if (_accountData == null) return;

        var userSettings = _accountData.GetObject("UserSettingsData")
                        ?? _accountData;

        var seasonRows = CollectRewardRows(_seasonGrid);
        var twitchRows = CollectRewardRows(_twitchGrid);
        var platformRows = CollectRewardRows(_platformGrid);

        AccountLogic.SaveRewardList(seasonRows, userSettings, "UnlockedSeasonRewards");
        AccountLogic.SaveRewardList(twitchRows, userSettings, "UnlockedTwitchRewards");
        AccountLogic.SaveRewardList(platformRows, userSettings, "UnlockedPlatformRewards");

        // Sync redeemed rewards to the game save (required by game alongside account unlock)
        AccountLogic.SyncRedeemedRewards(saveData, seasonRows, twitchRows);

        // Additionally write platform rewards to MXML file (if configured)
        MxmlRewardEditor.SyncPlatformRewards(_mxmlFilePath, platformRows);
    }

    private static List<(string Id, bool Unlocked)> CollectRewardRows(DataGridView grid)
    {
        var result = new List<(string Id, bool Unlocked)>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            var rewardId = row.Cells["RewardId"].Value?.ToString() ?? "";
            bool unlocked = row.Cells["Unlocked"].Value is true;
            result.Add((rewardId, unlocked));
        }
        return result;
    }

    /// <summary>
    /// Sets the MXML file path and updates the UI.
    /// </summary>
    private void SetMxmlPath(string path)
    {
        _mxmlFilePath = path;
        _platformMxmlPathBox.Text = path;
        _platformMxmlStatusLabel.Text = File.Exists(path) ? "✓ File found" : "✗ File not found";
        _platformMxmlStatusLabel.ForeColor = File.Exists(path)
            ? System.Drawing.Color.Green
            : System.Drawing.Color.Red;
    }

    /// <summary>
    /// Opens a file dialog to select the GCUSERSETTINGSDATA.MXML file.
    /// </summary>
    private void OnBrowseMxml(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select GCUSERSETTINGSDATA.MXML",
            Filter = "MXML Files (*.MXML)|*.MXML|All Files (*.*)|*.*",
            FileName = "GCUSERSETTINGSDATA.MXML",
        };

        // Try to start in a sensible directory
        if (!string.IsNullOrEmpty(_mxmlFilePath))
        {
            string? dir = Path.GetDirectoryName(_mxmlFilePath);
            if (dir != null && Directory.Exists(dir))
                dlg.InitialDirectory = dir;
        }

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            SetMxmlPath(dlg.FileName);

            // Reload platform rewards grid to merge MXML data
            if (_accountData != null)
            {
                var userSettings = _accountData.GetObject("UserSettingsData") ?? _accountData;
                var platformUnlocked = AccountLogic.GetUnlockedSet(userSettings.GetArray("UnlockedPlatformRewards"));
                var mxmlRewards = MxmlRewardEditor.ReadUnlockedRewards(_mxmlFilePath!);
                foreach (var id in mxmlRewards)
                    platformUnlocked.Add(id);
                PopulateRewardGrid(_platformGrid, _platformRewardsDb, platformUnlocked);
            }
        }
    }

    public void ApplyUiLocalisation()
    {
        _statusLabel.Text = UiStrings.Get("account.status_not_loaded");
        _warningLabel.Text = UiStrings.Get("account.twitch_warning");

        // Tab pages
        _seasonPage.Text = UiStrings.Get("account.tab_season");
        _twitchPage.Text = UiStrings.Get("account.tab_twitch");
        _platformPage.Text = UiStrings.Get("account.tab_platform");

        // Buttons
        _seasonUnlockAllBtn.Text = UiStrings.Get("common.unlock_all");
        _seasonLockAllBtn.Text = UiStrings.Get("common.lock_all");
        _twitchUnlockAllBtn.Text = UiStrings.Get("common.unlock_all");
        _twitchLockAllBtn.Text = UiStrings.Get("common.lock_all");
        _platformUnlockAllBtn.Text = UiStrings.Get("common.unlock_all");
        _platformLockAllBtn.Text = UiStrings.Get("common.lock_all");
        _platformMxmlBrowseBtn.Text = UiStrings.Get("common.browse");

        // Filter labels
        _seasonFilterLabel.Text = UiStrings.Get("common.filter");
        _twitchFilterLabel.Text = UiStrings.Get("common.filter");
        _platformFilterLabel.Text = UiStrings.Get("common.filter");

        // Platform MXML info
        _platformMxmlInfoLabel.Text = UiStrings.Get("account.platform_mxml_info");

        // Column headers
        _seasonRewardIdColumn.HeaderText = UiStrings.Get("account.col_reward_id");
        _seasonRewardNameColumn.HeaderText = UiStrings.Get("account.col_name");
        _seasonUnlockedColumn.HeaderText = UiStrings.Get("account.col_unlocked");
        _twitchRewardIdColumn.HeaderText = UiStrings.Get("account.col_reward_id");
        _twitchRewardNameColumn.HeaderText = UiStrings.Get("account.col_name");
        _twitchUnlockedColumn.HeaderText = UiStrings.Get("account.col_unlocked");
        _platformRewardIdColumn.HeaderText = UiStrings.Get("account.col_reward_id");
        _platformRewardNameColumn.HeaderText = UiStrings.Get("account.col_name");
        _platformUnlockedColumn.HeaderText = UiStrings.Get("account.col_unlocked");
    }
}
