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

    /// <summary>
    /// Snapshot of the redeemed reward IDs at the time the save was loaded.
    /// Used to compute the delta (items the user actually changed) so that
    /// Known* array sync only touches items the user explicitly toggled,
    /// rather than every redeemed reward in the save.
    /// </summary>
    private HashSet<string> _originalSeasonRedeemed = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _originalTwitchRedeemed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Snapshot of the platform/entitlement rewards whose product ID was present in the
    /// save's Known* arrays at load time. Platform rewards have no dedicated Redeemed*
    /// array, so Known* presence is used as the redeemed state. Used to compute the
    /// delta of items the user actually toggled.
    /// </summary>
    private HashSet<string> _originalPlatformRedeemed = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reward IDs in the "entitlement" category. These must not be written to
    /// UnlockedPlatformRewards (the game validates that list against the platform reward
    /// table and strips everything else); they are handled through their product IDs in
    /// the account-level UnlockedSpecials / SeenTechnologies lists.
    /// </summary>
    private readonly HashSet<string> _entitlementRewardIds = new(StringComparer.OrdinalIgnoreCase);

    private bool _loading;

    /// <summary>The loaded account data object, or null if not loaded.</summary>
    public JsonObject? AccountData => _accountData;
    /// <summary>The file path of the loaded account data.</summary>
    public string? AccountFilePath => _accountFilePath;
    /// <summary>True when the loaded account data came from the macOS GcUserSettingsData.mxml file.</summary>
    public bool IsMxmlSource { get; private set; }
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

        RebuildEntitlementRewardIds();
    }

    /// <summary>
    /// Rebuilds the set of entitlement reward IDs from the rewards database.
    /// </summary>
    private void RebuildEntitlementRewardIds()
    {
        _entitlementRewardIds.Clear();
        foreach (var reward in RewardDatabase.EntitlementRewards)
            _entitlementRewardIds.Add(reward.Id);
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
        RebuildEntitlementRewardIds();

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
        _loading = true;
        try
        {
            _seasonGrid.Rows.Clear();
            _twitchGrid.Rows.Clear();
            _platformGrid.Rows.Clear();
            _accountData = null;
            _accountFilePath = null;
            IsMxmlSource = false;

            var data = AccountLogic.LoadAccountData(saveDirectory);
            if (data.ErrorMessage != null)
            {
                _statusLabel.Text = data.ErrorMessage;
                return;
            }

            _accountData = data.AccountObject;
            _accountFilePath = data.AccountFilePath;
            IsMxmlSource = data.IsMxmlSource;

            // macOS loads account data directly from GcUserSettingsData.mxml; use that path.
            if (data.IsMxmlSource && !string.IsNullOrEmpty(data.AccountFilePath))
                SetMxmlPath(data.AccountFilePath);

            // Auto-detect MXML path if not already set (PC platforms only).
            // Console platforms (Xbox, PS4, Switch) do not use MXML files.
            if (UsesMxml && string.IsNullOrEmpty(_mxmlFilePath))
            {
                var detected = MxmlRewardEditor.FindMxmlPath(saveDirectory);
                if (detected != null)
                    SetMxmlPath(detected);
            }

            // Platform rewards require BOTH accountdata AND MXML to be present (PC only).
            // Only show as unlocked if the reward exists in both sources. When the account
            // data itself came from the MXML (macOS) there is only one source to use.
            var platformUnlocked = data.PlatformUnlocked;
            var entitlementProducts = NormaliseProductSet(data.SpecialsUnlocked);
            if (UsesMxml && !data.IsMxmlSource && !string.IsNullOrEmpty(_mxmlFilePath))
            {
                var mxmlRewards = MxmlRewardEditor.ReadUnlockedRewards(_mxmlFilePath);
                // Intersect: only keep rewards that are in both accountdata and MXML
                platformUnlocked = new HashSet<string>(
                    platformUnlocked.Where(id => mxmlRewards.Contains(id)),
                    StringComparer.OrdinalIgnoreCase);

                // Entitlement rewards are stored by product ID; intersect the accountdata
                // and MXML product sets for the same both-sources policy.
                entitlementProducts.IntersectWith(
                    NormaliseProductSet(MxmlRewardEditor.ReadUnlockedSpecials(_mxmlFilePath)));
            }

            PopulateRewardGrid(_seasonGrid, _seasonRewardsDb, data.SeasonUnlocked);
            PopulateRewardGrid(_twitchGrid, _twitchRewardsDb, data.TwitchUnlocked);
            PopulatePlatformGrid(platformUnlocked, MapEntitlementProductsToRewardIds(entitlementProducts));

            _statusLabel.Text = data.StatusMessage ?? "";
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Loads account data from an Xbox Game Pass AccountData blob.
    /// This is the Xbox equivalent of <see cref="LoadAccountFile"/>.
    /// Xbox is a console platform so MXML is never used.
    /// </summary>
    /// <param name="accountSlot">The Xbox slot info for the AccountData entry.</param>
    public void LoadXboxAccountData(XboxSlotInfo accountSlot)
    {
        _loading = true;
        try
        {
            _seasonGrid.Rows.Clear();
            _twitchGrid.Rows.Clear();
            _platformGrid.Rows.Clear();
            _accountData = null;
            _accountFilePath = null;
            IsMxmlSource = false;

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
            PopulatePlatformGrid(data.PlatformUnlocked,
                MapEntitlementProductsToRewardIds(NormaliseProductSet(data.SpecialsUnlocked)));

            _statusLabel.Text = data.StatusMessage ?? "";
        }
        finally
        {
            _loading = false;
        }
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
                object expValue = row.SeasonId >= 0 ? (object)row.SeasonId : DBNull.Value;
                idx = grid.Rows.Add((object?)icon ?? DBNull.Value, row.Id, row.Name,
                    expValue, row.Unlocked, row.Redeemed);
            }
            else
            {
                idx = grid.Rows.Add((object?)icon ?? DBNull.Value, row.Id, row.Name,
                    row.Unlocked, row.Redeemed);
            }
        }
    }

    /// <summary>
    /// Populates the platform rewards grid from the platform unlock set and the
    /// entitlement reward set (both are displayed in the same grid).
    /// </summary>
    private void PopulatePlatformGrid(HashSet<string> platformUnlocked, HashSet<string> entitlementUnlocked)
    {
        var combined = new HashSet<string>(platformUnlocked, StringComparer.OrdinalIgnoreCase);
        combined.UnionWith(entitlementUnlocked);
        PopulateRewardGrid(_platformGrid, _platformRewardsDb, combined);
    }

    /// <summary>
    /// Strips caret prefixes from a set of account-level IDs so they can be compared
    /// against raw product IDs (the game stores UnlockedSpecials values without caret).
    /// </summary>
    private static HashSet<string> NormaliseProductSet(IEnumerable<string> ids)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            string stripped = CatalogueLogic.StripCaretPrefix(id);
            if (!string.IsNullOrEmpty(stripped))
                set.Add(stripped);
        }
        return set;
    }

    /// <summary>
    /// Maps a set of special product IDs to the entitlement reward IDs that produce them,
    /// so the platform grid can display entitlement unlock state from the account-level
    /// UnlockedSpecials list (which is keyed by product ID).
    /// </summary>
    private HashSet<string> MapEntitlementProductsToRewardIds(HashSet<string> products)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (products.Count == 0) return result;

        foreach (var id in _entitlementRewardIds)
        {
            string productId = _productIdMap.TryGetValue(id, out var mapped) && !string.IsNullOrEmpty(mapped)
                ? mapped
                : CatalogueLogic.StripCaretPrefix(id);
            if (products.Contains(CatalogueLogic.StripCaretPrefix(productId)))
                result.Add(id);
        }
        return result;
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

        // Snapshot the original redeemed state so SaveData can compute the delta
        // (only items the user actually toggled need Known* array sync).
        _originalSeasonRedeemed = new HashSet<string>(seasonRedeemed, StringComparer.OrdinalIgnoreCase);
        _originalTwitchRedeemed = new HashSet<string>(twitchRedeemed, StringComparer.OrdinalIgnoreCase);

        // Platform/entitlement rewards have no dedicated Redeemed* array. Their redeemed
        // state is represented by the Known* arrays, resolved through each reward's
        // product ID. Capture that state so SaveData can compute a clean delta.
        _originalPlatformRedeemed = BuildPlatformRedeemedSet(saveData);

        // Update the redeemed column in each grid
        _loading = true;
        try
        {
            UpdateRedeemedColumn(_seasonGrid, seasonRedeemed);
            UpdateRedeemedColumn(_twitchGrid, twitchRedeemed);
            UpdateRedeemedColumn(_platformGrid, _originalPlatformRedeemed);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Builds the set of platform/entitlement reward IDs to display as "Redeemed in Save".
    /// Platform rewards (TGA/SW) use the save's <c>RedeemedPlatformRewards</c> array, which
    /// the game writes when a platform reward is collected (<c>RedeemPlatformReward</c>).
    /// Entitlement rewards have no redeem array; their owned state is derived from the save's
    /// Known* arrays via each reward's product ID (KnownTech for technology products,
    /// KnownSpecials for specials), mirroring the game's AwardTechnology/RedeemSpecial.
    /// </summary>
    private HashSet<string> BuildPlatformRedeemedSet(JsonObject saveData)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return result;

        var redeemedPlatform = AccountLogic.GetUnlockedSet(playerState.GetArray("RedeemedPlatformRewards"));
        var knownTech = AccountLogic.GetUnlockedSet(playerState.GetArray("KnownTech"));
        var knownSpecials = AccountLogic.GetUnlockedSet(playerState.GetArray("KnownSpecials"));

        foreach (DataGridViewRow row in _platformGrid.Rows)
        {
            var rewardId = row.Cells["RewardId"].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(rewardId)) continue;

            // Platform rewards are collected through the game's RedeemedPlatformRewards array.
            if (redeemedPlatform.Contains(rewardId))
            {
                result.Add(rewardId);
                continue;
            }

            // Entitlement rewards: owned when the resolved product is already known in the save.
            string productId = _productIdMap.TryGetValue(rewardId, out var mapped) && !string.IsNullOrEmpty(mapped)
                ? mapped
                : CatalogueLogic.StripCaretPrefix(rewardId);
            string saveId = CatalogueLogic.EnsureCaretPrefix(productId);

            if (knownTech.Contains(saveId) || knownSpecials.Contains(saveId))
                result.Add(rewardId);
        }
        return result;
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
    /// Known* array sync and stale-entry cleanup are performed ONLY for items
    /// whose redeemed state was explicitly changed by the user (delta-only),
    /// so pre-existing entries the player may have out of sync for their own
    /// in-game reasons are not touched.
    /// Platform rewards are written to UnlockedPlatformRewards; entitlement rewards are
    /// instead written by product ID to the account-level UnlockedSpecials and
    /// SeenTechnologies lists (the game strips entitlement IDs from UnlockedPlatformRewards).
    /// The "Redeemed in Save" column writes the save's RedeemedPlatformRewards array for
    /// platform rewards and the save's Known* arrays (via resolved product IDs) for
    /// entitlement rewards.
    /// Additionally writes both to the MXML file if configured.
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

        // Split the platform grid into genuine platform rewards and entitlement rewards.
        // Entitlement IDs are NOT valid in UnlockedPlatformRewards; the game validates
        // that list against the platform reward table and strips everything else.
        var platformOnlyRows = new List<(string Id, bool Unlocked, bool Redeemed)>();
        var entitlementRows = new List<(string Id, bool Unlocked, bool Redeemed)>();
        foreach (var row in platformRows)
        {
            if (_entitlementRewardIds.Contains(row.Id))
                entitlementRows.Add(row);
            else
                platformOnlyRows.Add(row);
        }

        // Save account-level unlocks (to accountdata.hg in memory)
        AccountLogic.SaveRewardList(
            seasonRows.Select(r => (r.Id, r.Unlocked)).ToList(),
            userSettings, "UnlockedSeasonRewards");
        AccountLogic.SaveRewardList(
            twitchRows.Select(r => (r.Id, r.Unlocked)).ToList(),
            userSettings, "UnlockedTwitchRewards");
        AccountLogic.SaveRewardList(
            platformOnlyRows.Select(r => (r.Id, r.Unlocked)).ToList(),
            userSettings, "UnlockedPlatformRewards");

        // Entitlement rewards are account-level lists keyed by PRODUCT ID:
        //  - UnlockedSpecials receives every entitlement product (also used to read back
        //    the unlock state for display).
        //  - SeenTechnologies additionally receives technology-table products, matching
        //    the in-game special reward collection path for technology entitlements.
        var (entitlementSpecials, entitlementSeenTechs) = BuildEntitlementAccountRows(entitlementRows);
        AccountLogic.SaveManagedRewardList(userSettings, "UnlockedSpecials", entitlementSpecials);
        if (entitlementSeenTechs.Count > 0)
            AccountLogic.SaveManagedRewardList(userSettings, "SeenTechnologies", entitlementSeenTechs);

        // Save per-save redeemed state (writes RedeemedSeasonRewards / RedeemedTwitchRewards).
        // Known* sync is done ONLY for items the user actually changed (see below).
        AccountLogic.SaveRedeemedRewards(saveData,
            seasonRows.Select(r => (r.Id, r.Redeemed)).ToList(),
            twitchRows.Select(r => (r.Id, r.Redeemed)).ToList(),
            _database);

        // Delta-only Known* sync: only touch KnownSpecials/KnownProducts/KnownTech for items
        // whose redeemed state was explicitly changed by the user. Items the user
        // didn't touch are left as-is, preserving any out-of-sync state the player
        // may have for their own in-game reasons.
        // Pass _productIdMap so that twitch rewards are resolved to their product ID
        // before writing KnownSpecials / KnownProducts (those arrays store product IDs,
        // not TwitchIds).
        var seasonChanged = GetChangedRewards(seasonRows, _originalSeasonRedeemed);
        var twitchChanged = GetChangedRewards(twitchRows, _originalTwitchRedeemed);
        AccountLogic.SyncKnownArraysForChangedRewards(saveData, seasonChanged, _database, _productIdMap);
        AccountLogic.SyncKnownArraysForChangedRewards(saveData, twitchChanged, _database, _productIdMap);

        // Platform rewards are collected through the save's RedeemedPlatformRewards array
        // (the game writes RewardIds there via cGcPlayerState::RedeemPlatformReward).
        var platformChanged = GetChangedRewards(platformOnlyRows, _originalPlatformRedeemed);
        if (platformChanged.Count > 0)
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState != null)
                AccountLogic.SaveManagedRewardList(playerState, "RedeemedPlatformRewards",
                    platformChanged.Select(r => (r.Id, r.Redeemed)).ToList());
        }

        // Entitlement rewards have no redeem array; their owned state is represented by the
        // save's Known* arrays, so a changed tick adds or removes the resolved product entry.
        // Mirrors the game: AwardTechnology writes KnownTech (+ SeenTechnologies) for
        // technology rewards, RedeemSpecial writes KnownSpecials (+ UnlockedSpecials) for specials.
        var entitlementChanged = GetChangedRewards(entitlementRows, _originalPlatformRedeemed);
        AccountLogic.SyncKnownArraysForChangedRewards(saveData, entitlementChanged, _database, _productIdMap);

        // Delta-only stale cleanup: only clean Known* entries for items the user
        // explicitly un-redeemed (was redeemed at load, now not redeemed).
        var seasonStaleChanged = GetChangedStaleRows(seasonRows, _originalSeasonRedeemed);
        var twitchStaleChanged = GetChangedStaleRows(twitchRows, _originalTwitchRedeemed);
        AccountLogic.CleanStaleKnownEntries(saveData, seasonStaleChanged, twitchStaleChanged, _database, _productIdMap);

        // Additionally write to the MXML file (PC platforms only).
        // Console platforms (Xbox, PS4, Switch) do not use MXML files.
        if (UsesMxml)
        {
            // Older NMSE versions wrote entitlement reward IDs into UnlockedPlatformRewards,
            // which the game strips on load. Pass them as managed-but-absent so any leftover
            // entries are cleaned up, while unmanaged platform entries are preserved.
            var platformMxmlRows = platformOnlyRows.Select(r => (r.Id, r.Unlocked)).ToList();
            foreach (var id in _entitlementRewardIds)
                platformMxmlRows.Add((id, false));

            MxmlRewardEditor.SyncPlatformRewards(_mxmlFilePath, platformMxmlRows);
            MxmlRewardEditor.SyncManagedRewards(_mxmlFilePath, "UnlockedSeasonRewards",
                seasonRows.Select(r => (r.Id, r.Unlocked)).ToList());
            MxmlRewardEditor.SyncManagedRewards(_mxmlFilePath, "UnlockedTwitchRewards",
                twitchRows.Select(r => (r.Id, r.Unlocked)).ToList());
            MxmlRewardEditor.SyncManagedRewards(_mxmlFilePath, "UnlockedSpecials", entitlementSpecials);
            if (entitlementSeenTechs.Count > 0)
                MxmlRewardEditor.SyncManagedRewards(_mxmlFilePath, "SeenTechnologies", entitlementSeenTechs);
        }

        // Refresh the load-time redeem snapshots. Without this, checking and then
        // unchecking Redeemed in the same session would compare against the original
        // false state and skip the Known* cleanup, leaving the item looking redeemed.
        _originalSeasonRedeemed = new HashSet<string>(
            seasonRows.Where(r => r.Redeemed).Select(r => r.Id),
            StringComparer.OrdinalIgnoreCase);
        _originalTwitchRedeemed = new HashSet<string>(
            twitchRows.Where(r => r.Redeemed).Select(r => r.Id),
            StringComparer.OrdinalIgnoreCase);
        _originalPlatformRedeemed = BuildPlatformRedeemedSet(saveData);

        // Raise the DataModified event so other panels can react to reward changes.
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Builds the account-level product ID lists for entitlement reward rows:
    /// every entitlement product goes into UnlockedSpecials; technology-table products
    /// additionally go into SeenTechnologies.
    /// </summary>
    private (List<(string Id, bool Present)> Specials, List<(string Id, bool Present)> SeenTechnologies)
        BuildEntitlementAccountRows(List<(string Id, bool Unlocked, bool Redeemed)> rows)
    {
        var specials = new List<(string Id, bool Present)>();
        var seenTechs = new List<(string Id, bool Present)>();

        foreach (var (id, unlocked, _) in rows)
        {
            if (string.IsNullOrEmpty(id)) continue;

            string productId = _productIdMap.TryGetValue(id, out var mapped) && !string.IsNullOrEmpty(mapped)
                ? mapped
                : CatalogueLogic.StripCaretPrefix(id);

            specials.Add((productId, unlocked));

            var item = _database?.GetItem(productId);
            string? seenArrayName = AccountLogic.GetEntitlementSeenArrayName(item);
            if (!string.IsNullOrEmpty(seenArrayName))
                seenTechs.Add((productId, unlocked));
        }

        return (specials, seenTechs);
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
    /// Returns only the rewards whose redeemed state was explicitly changed by the user.
    /// Compares the current grid state against the snapshot taken at load time.
    /// Items that haven't been toggled are excluded, so their Known* entries are untouched.
    /// </summary>
    private static List<(string Id, bool Redeemed)> GetChangedRewards(
        List<(string Id, bool Unlocked, bool Redeemed)> rows,
        HashSet<string> originalRedeemed)
    {
        var changed = new List<(string Id, bool Redeemed)>();
        foreach (var (id, _, redeemed) in rows)
        {
            if (string.IsNullOrEmpty(id)) continue;
            bool wasRedeemed = originalRedeemed.Contains(id);
            if (redeemed != wasRedeemed)
                changed.Add((id, redeemed));
        }
        return changed;
    }

    /// <summary>
    /// Returns rows that need stale Known* cleanup - items the user explicitly
    /// changed to be unlocked-but-not-redeemed (was redeemed at load, now not redeemed
    /// but still unlocked). Items that were already unlocked-but-not-redeemed at
    /// load time are excluded since their Known* state is the player's own choice.
    /// </summary>
    private static List<(string Id, bool Unlocked, bool Redeemed)> GetChangedStaleRows(
        List<(string Id, bool Unlocked, bool Redeemed)> rows,
        HashSet<string> originalRedeemed)
    {
        var changed = new List<(string Id, bool Unlocked, bool Redeemed)>();
        foreach (var (id, unlocked, redeemed) in rows)
        {
            if (string.IsNullOrEmpty(id)) continue;
            bool wasRedeemed = originalRedeemed.Contains(id);
            // Only include if the user toggled redeemed OFF (was redeemed, now not).
            // The CleanStaleKnownEntries logic will then check unlocked && !redeemed.
            if (wasRedeemed && !redeemed)
                changed.Add((id, unlocked, redeemed));
        }
        return changed;
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
            ? (ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.SuccessGreen : System.Drawing.Color.Green)
            : (ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.ErrorRed : System.Drawing.Color.Red);
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

        if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
        {
            SetMxmlPath(dlg.FileName);

            // Reload platform rewards grid to merge MXML data
            if (_accountData != null)
            {
                var userSettings = _accountData.GetObject("UserSettingsData") ?? _accountData;
                var platformUnlocked = AccountLogic.GetUnlockedSet(userSettings.GetArray("UnlockedPlatformRewards"));
                var entitlementProducts = NormaliseProductSet(
                    AccountLogic.GetUnlockedSet(userSettings.GetArray("UnlockedSpecials")));

                var mxmlRewards = MxmlRewardEditor.ReadUnlockedRewards(_mxmlFilePath!);
                foreach (var id in mxmlRewards)
                    platformUnlocked.Add(id);
                entitlementProducts.UnionWith(
                    NormaliseProductSet(MxmlRewardEditor.ReadUnlockedSpecials(_mxmlFilePath!)));

                PopulatePlatformGrid(platformUnlocked, MapEntitlementProductsToRewardIds(entitlementProducts));
                UpdateRedeemedColumn(_platformGrid, _originalPlatformRedeemed);
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

        var userSettings = _accountData?.GetObject("UserSettingsData") ?? _accountData;
        var issues = AccountLogic.CheckConsistencyStructured(_currentSaveData, _database, userSettings);
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
            // Mark save as modified only if the user actually resolved any issues.
            if (dialog.HasChanges)
                DataModified?.Invoke(this, EventArgs.Empty);
        }
    }
}
