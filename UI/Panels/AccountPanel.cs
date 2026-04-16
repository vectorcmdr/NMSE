using System.Globalization;
using NMSE.Data;
using NMSE.Models;
using NMSE.Core;
using NMSE.IO;
using NMSE.UI.Dialogs;

namespace NMSE.UI.Panels;

public partial class AccountPanel : UserControl
{
    /// <summary>Raised when reward data is modified by the user (save/sync).
    /// Other panels (e.g. CataloguePanel) can subscribe to refresh their state.</summary>
    public event EventHandler? DataModified;

    private JsonObject? _accountData;
    private string? _accountFilePath;
    private string? _mxmlFilePath;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private SaveFileManager.Platform _currentPlatform = SaveFileManager.Platform.Unknown;
    private JsonObject? _currentSaveData;

    /// <summary>The loaded account data object, or null if not loaded.</summary>
    public JsonObject? AccountData => _accountData;
    /// <summary>The file path of the loaded account data.</summary>
    public string? AccountFilePath => _accountFilePath;
    /// <summary>The file path of the MXML settings file for platform rewards, or null if not set.</summary>
    public string? MxmlFilePath => _mxmlFilePath;

    /// <summary>
    /// Returns true if the current platform uses MXML files for platform rewards.
    /// Only Steam and GOG (PC) use MXML; console platforms (Xbox, PS4, Switch) do not.
    /// </summary>
    private bool UsesMxml => _currentPlatform is SaveFileManager.Platform.Steam
                                               or SaveFileManager.Platform.GOG
                                               or SaveFileManager.Platform.Unknown;

    // Rewards database from Rewards.json
    private readonly List<AccountLogic.RewardDbEntry> _seasonRewardsDb = new();
    private readonly List<AccountLogic.RewardDbEntry> _twitchRewardsDb = new();
    private readonly List<AccountLogic.RewardDbEntry> _platformRewardsDb = new();
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
    /// Sets the detected platform for the currently loaded save.
    /// Controls whether MXML-related UI and logic is active (PC-only).
    /// </summary>
    public void SetPlatform(SaveFileManager.Platform platform)
    {
        _currentPlatform = platform;

        // MXML controls are only relevant for PC platforms (Steam/GOG)
        bool showMxml = UsesMxml;
        _platformMxmlInfoLabel.Visible = showMxml;
        _platformMxmlFilePanel.Visible = showMxml;

        // Clear MXML state when switching to a console platform
        if (!showMxml)
        {
            _mxmlFilePath = null;
            _platformMxmlPathBox.Text = "";
            _platformMxmlStatusLabel.Text = "";
        }
    }

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
            _seasonRewardsDb.Add(new AccountLogic.RewardDbEntry
            {
                Id = reward.Id, Name = ResolveDisplayName(reward),
                SeasonId = reward.SeasonId, StageId = reward.StageId,
                MustBeUnlocked = reward.Unlock,
            });
            StoreProductId(reward);
        }
        foreach (var reward in RewardDatabase.TwitchRewards)
        {
            _twitchRewardsDb.Add(new AccountLogic.RewardDbEntry
                { Id = reward.Id, Name = ResolveDisplayName(reward) });
            StoreProductId(reward);
        }
        foreach (var reward in RewardDatabase.PlatformRewards)
        {
            _platformRewardsDb.Add(new AccountLogic.RewardDbEntry
                { Id = reward.Id, Name = ResolveDisplayName(reward) });
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

        void RefreshList(List<AccountLogic.RewardDbEntry> cache, IEnumerable<RewardEntry> source)
        {
            cache.Clear();
            foreach (var reward in source)
            {
                cache.Add(new AccountLogic.RewardDbEntry
                {
                    Id = reward.Id, Name = ResolveDisplayName(reward),
                    SeasonId = reward.SeasonId, StageId = reward.StageId,
                    MustBeUnlocked = reward.Unlock,
                });
            }
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

    private static void SetAllRedeemed(DataGridView grid, bool value)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (!row.Visible) continue;
            row.Cells["RedeemedInSave"].Value = value;
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

        // Auto-detect MXML path if not already set (PC platforms only).
        // Console platforms (Xbox, PS4, Switch) do not use MXML files.
        if (UsesMxml && string.IsNullOrEmpty(_mxmlFilePath))
        {
            var detected = MxmlRewardEditor.AutoDetectMxmlPath();
            if (detected != null)
                SetMxmlPath(detected);
        }

        // Platform rewards require BOTH accountdata AND MXML to be present (PC only).
        // Only show as unlocked if the reward exists in both sources.
        var platformUnlocked = data.PlatformUnlocked;
        if (UsesMxml && !string.IsNullOrEmpty(_mxmlFilePath))
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
    /// Xbox is a console platform so MXML is never used.
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

        // Xbox is a console platform - do NOT auto-detect or use MXML files.
        // Platform rewards for Xbox are stored in the AccountData blob only.

        PopulateRewardGrid(_seasonGrid, _seasonRewardsDb, data.SeasonUnlocked);
        PopulateRewardGrid(_twitchGrid, _twitchRewardsDb, data.TwitchUnlocked);
        PopulateRewardGrid(_platformGrid, _platformRewardsDb, data.PlatformUnlocked);

        _statusLabel.Text = data.StatusMessage ?? "";
    }

    private void PopulateRewardGrid(DataGridView grid, List<AccountLogic.RewardDbEntry> rewardsDb,
        HashSet<string> unlocked, HashSet<string>? redeemed = null)
    {
        grid.Rows.Clear();
        var rows = AccountLogic.BuildRewardRows(rewardsDb, unlocked, redeemed);
        bool hasExpeditionCol = grid.Columns.Contains("Expedition");
        foreach (var row in rows)
        {
            Image? icon = GetRewardIcon(row.Id, row.Name);
            int idx;
            if (hasExpeditionCol)
            {
                // Season grid: include Expedition column
                string expLabel = row.SeasonId >= 0 ? row.SeasonId.ToString(CultureInfo.CurrentCulture) : "";
                idx = grid.Rows.Add((object?)icon ?? DBNull.Value, row.Id, row.Name,
                    expLabel, row.Unlocked, row.Redeemed);
            }
            else
            {
                idx = grid.Rows.Add((object?)icon ?? DBNull.Value, row.Id, row.Name,
                    row.Unlocked, row.Redeemed);
            }
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
    /// Reads the per-save redeemed reward state from the loaded save data.
    /// Call this after the save data is loaded so the "Redeemed in Save" column
    /// reflects the current save's state.
    /// </summary>
    public void LoadData(JsonObject saveData)
    {
        _currentSaveData = saveData;

        // Read redeemed sets from save
        var (seasonRedeemed, twitchRedeemed) = AccountLogic.GetRedeemedSets(saveData);

        // Update the redeemed column in each grid
        UpdateRedeemedColumn(_seasonGrid, seasonRedeemed);
        UpdateRedeemedColumn(_twitchGrid, twitchRedeemed);
        // Platform rewards do not have per-save redemption arrays
        UpdateRedeemedColumn(_platformGrid, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates the "Redeemed in Save" column for all rows in the given grid
    /// based on the provided set of redeemed reward IDs.
    /// </summary>
    private static void UpdateRedeemedColumn(DataGridView grid, HashSet<string> redeemed)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            var rewardId = row.Cells["RewardId"].Value?.ToString() ?? "";
            row.Cells["RedeemedInSave"].Value = redeemed.Contains(rewardId);
        }
    }

    /// <summary>
    /// Syncs the current grid state to the in-memory account data object.
    /// Saves redeemed rewards to the game save data independently of account unlock state.
    /// Additionally synchronises Known* arrays for redeemed rewards and cleans stale
    /// entries for rewards that are unlocked but not redeemed.
    /// Additionally writes platform rewards to the MXML file if configured.
    /// Does NOT write to disk for account data; that happens in MainForm.OnSave().
    /// </summary>
    public void SaveData(JsonObject saveData)
    {
        if (_accountData == null) return;

        var userSettings = _accountData.GetObject("UserSettingsData")
                        ?? _accountData;

        var seasonRows = CollectRewardRows(_seasonGrid);
        var twitchRows = CollectRewardRows(_twitchGrid);
        var platformRows = CollectRewardRows(_platformGrid);

        // Save account-level unlocks (to accountdata.hg in memory)
        AccountLogic.SaveRewardList(
            seasonRows.Select(r => (r.Id, r.Unlocked)).ToList(),
            userSettings, "UnlockedSeasonRewards");
        AccountLogic.SaveRewardList(
            twitchRows.Select(r => (r.Id, r.Unlocked)).ToList(),
            userSettings, "UnlockedTwitchRewards");
        AccountLogic.SaveRewardList(
            platformRows.Select(r => (r.Id, r.Unlocked)).ToList(),
            userSettings, "UnlockedPlatformRewards");

        // Sync account-level Seen* arrays (SeenProducts, SeenTechnologies) to match
        // REDEEMED state (not unlock state). The Seen* arrays track items the player
        // has redeemed/claimed. Unlock checkboxes only affect Unlocked* arrays;
        // redeem checkboxes affect Seen* arrays. The two are mutually exclusive.
        // IMPORTANT: Seen* arrays store the actual PRODUCT ID (e.g. ^EXPD_POSTER11A),
        // not the reward identifier (e.g. ^TWITCH_376). For season rewards these
        // happen to be the same, but for twitch/platform rewards they differ.
        AccountLogic.SyncAccountSeenArrays(userSettings,
            ResolveProductIdsForSeen(seasonRows), _database);
        AccountLogic.SyncAccountSeenArrays(userSettings,
            ResolveProductIdsForSeen(twitchRows), _database);
        AccountLogic.SyncAccountSeenArrays(userSettings,
            ResolveProductIdsForSeen(platformRows), _database);

        // Save per-save redeemed state and synchronise Known* arrays.
        // The database is passed so SaveRedeemedRewards can determine which
        // Known* arrays each reward should appear in (KnownSpecials, KnownTech).
        AccountLogic.SaveRedeemedRewards(saveData,
            seasonRows.Select(r => (r.Id, r.Redeemed)).ToList(),
            twitchRows.Select(r => (r.Id, r.Redeemed)).ToList(),
            _database);

        // Clean stale Known* entries for rewards that are unlocked on account
        // but not redeemed in this save. Prevents the game from treating
        // unlocked-only items as already claimed due to leftover Known* entries.
        AccountLogic.CleanStaleKnownEntries(saveData, seasonRows, twitchRows, _database);

        // Additionally write platform rewards to MXML file (PC platforms only).
        // Console platforms (Xbox, PS4, Switch) do not use MXML files.
        if (UsesMxml)
            MxmlRewardEditor.SyncPlatformRewards(_mxmlFilePath,
                platformRows.Select(r => (r.Id, r.Unlocked)).ToList());

        // Raise the DataModified event so other panels can react to reward changes.
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    private static List<(string Id, bool Unlocked, bool Redeemed)> CollectRewardRows(DataGridView grid)
    {
        var result = new List<(string Id, bool Unlocked, bool Redeemed)>();
        foreach (DataGridViewRow row in grid.Rows)
        {
            var rewardId = row.Cells["RewardId"].Value?.ToString() ?? "";
            bool unlocked = row.Cells["Unlocked"].Value is true;
            bool redeemed = row.Cells["RedeemedInSave"].Value is true;
            result.Add((rewardId, unlocked, redeemed));
        }
        return result;
    }

    /// <summary>
    /// Maps reward rows to (ProductId, Redeemed) tuples for Seen* array sync.
    /// The Seen* arrays in account data store the actual product IDs (e.g.
    /// <c>^EXPD_POSTER11A</c>), not the reward identifiers (e.g. <c>^TWITCH_376</c>).
    /// For season rewards these happen to be the same, but for twitch and platform
    /// rewards they differ. This method resolves each reward to its product ID
    /// via <see cref="_productIdMap"/>, falling back to the reward ID when no
    /// separate ProductId is defined.
    /// Uses <c>Redeemed</c> state (not Unlocked) because Seen* arrays track
    /// redeemed/claimed items, not just unlocked ones.
    /// </summary>
    private List<(string Id, bool Present)> ResolveProductIdsForSeen(
        List<(string Id, bool Unlocked, bool Redeemed)> rows)
    {
        return rows.Select(r =>
        {
            string productId = r.Id;
            if (_productIdMap.TryGetValue(r.Id, out var pid) && !string.IsNullOrEmpty(pid))
                productId = CatalogueLogic.EnsureCaretPrefix(pid);
            return (productId, r.Redeemed);
        }).ToList();
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
    /// Only available for PC platforms (Steam/GOG).
    /// </summary>
    private void OnBrowseMxml(object? sender, EventArgs e)
    {
        if (!UsesMxml) return;

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
        _seasonUnlockAllBtn.Text = UiStrings.Get("account.unlock_all");
        _seasonLockAllBtn.Text = UiStrings.Get("account.lock_all");
        _seasonRedeemAllBtn.Text = UiStrings.Get("account.redeem_all");
        _seasonRemoveAllBtn.Text = UiStrings.Get("account.remove_all");
        _twitchUnlockAllBtn.Text = UiStrings.Get("account.unlock_all");
        _twitchLockAllBtn.Text = UiStrings.Get("account.lock_all");
        _twitchRedeemAllBtn.Text = UiStrings.Get("account.redeem_all");
        _twitchRemoveAllBtn.Text = UiStrings.Get("account.remove_all");
        _platformUnlockAllBtn.Text = UiStrings.Get("account.unlock_all");
        _platformLockAllBtn.Text = UiStrings.Get("account.lock_all");
        _platformRedeemAllBtn.Text = UiStrings.Get("account.redeem_all");
        _platformRemoveAllBtn.Text = UiStrings.Get("account.remove_all");
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
        _seasonExpeditionColumn.HeaderText = UiStrings.Get("account.col_expedition");
        _seasonUnlockedColumn.HeaderText = UiStrings.Get("account.col_unlocked");
        _seasonRedeemedColumn.HeaderText = UiStrings.Get("account.col_redeemed");
        _twitchRewardIdColumn.HeaderText = UiStrings.Get("account.col_reward_id");
        _twitchRewardNameColumn.HeaderText = UiStrings.Get("account.col_name");
        _twitchUnlockedColumn.HeaderText = UiStrings.Get("account.col_unlocked");
        _twitchRedeemedColumn.HeaderText = UiStrings.Get("account.col_redeemed");
        _platformRewardIdColumn.HeaderText = UiStrings.Get("account.col_reward_id");
        _platformRewardNameColumn.HeaderText = UiStrings.Get("account.col_name");
        _platformUnlockedColumn.HeaderText = UiStrings.Get("account.col_unlocked");
        _platformRedeemedColumn.HeaderText = UiStrings.Get("account.col_redeemed");

        // Consistency check button
        _consistencyCheckBtn.Text = UiStrings.Get("account.consistency_check");
    }

    /// <summary>
    /// Runs a consistency check between Redeemed* and Known* arrays in the save data.
    /// Shows a scrollable dialog with per-item and bulk fix actions.
    /// </summary>
    private void OnConsistencyCheck(object? sender, EventArgs e)
    {
        if (_currentSaveData == null)
        {
            MessageBox.Show(this, UiStrings.Get("account.consistency_no_save"),
                UiStrings.Get("account.consistency_check"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var issues = AccountLogic.CheckConsistencyStructured(_currentSaveData, _database);
        if (issues.Count == 0)
        {
            MessageBox.Show(this, UiStrings.Get("account.consistency_ok"),
                UiStrings.Get("account.consistency_check"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            using var dialog = new ConsistencyDialog(issues, _currentSaveData, _database, _iconManager);
            dialog.ShowDialog(this);
            // Mark save as modified since the user may have resolved issues.
            DataModified?.Invoke(this, EventArgs.Empty);
        }
    }
}
