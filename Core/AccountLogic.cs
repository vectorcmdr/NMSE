using NMSE.Data;
using NMSE.IO;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Handles account-level data operations including loading/saving reward unlock states.
/// </summary>
internal static class AccountLogic
{
    /// <summary>
    /// Reads a JSON array of string values into a case-insensitive hash set.
    /// </summary>
    /// <param name="array">The JSON array of string values, or <c>null</c>.</param>
    /// <returns>A hash set containing the non-empty string values from the array.</returns>
    internal static HashSet<string> GetUnlockedSet(JsonArray? array)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (array == null) return set;
        for (int i = 0; i < array.Length; i++)
        {
            var val = array.GetString(i);
            if (!string.IsNullOrEmpty(val))
                set.Add(val);
        }
        return set;
    }

    /// <summary>
    /// Loads account data from an Xbox Game Pass containers.index AccountData blob.
    /// This is the Xbox equivalent of <see cref="LoadAccountData"/> which reads accountdata.hg.
    /// </summary>
    /// <param name="accountSlot">The Xbox slot info for the AccountData entry.</param>
    /// <returns>An <see cref="AccountData"/> with loaded reward sets, or an error message if loading failed.</returns>
    internal static AccountData LoadXboxAccountData(XboxSlotInfo accountSlot)
    {
        try
        {
            string? json = ContainersIndexManager.LoadXboxSave(accountSlot);
            if (json == null)
                return new AccountData { ErrorMessage = UiStrings.Get("account.not_found") };

            var accountObj = JsonObject.Parse(json);

            var userSettings = accountObj.GetObject("UserSettingsData") ?? accountObj;

            var seasonUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedSeasonRewards"));
            var twitchUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedTwitchRewards"));
            var platformUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedPlatformRewards"));

            int total = seasonUnlocked.Count + twitchUnlocked.Count + platformUnlocked.Count;

            return new AccountData
            {
                AccountObject = accountObj,
                AccountFilePath = accountSlot.DataFilePath,
                SeasonUnlocked = seasonUnlocked,
                TwitchUnlocked = twitchUnlocked,
                PlatformUnlocked = platformUnlocked,
                StatusMessage = UiStrings.Format("account.status_loaded", total),
            };
        }
        catch (Exception ex)
        {
            return new AccountData { ErrorMessage = UiStrings.Format("account.load_error", accountSlot.DataFilePath ?? "Xbox AccountData", ex.Message) };
        }
    }

    /// <summary>
    /// Loads account data from the accountdata.hg file in the specified save directory.
    /// </summary>
    /// <param name="saveDirectory">The path to the save directory containing accountdata.hg.</param>
    /// <returns>An <see cref="AccountData"/> with loaded reward sets, or an error message if loading failed.</returns>
    internal static AccountData LoadAccountData(string saveDirectory)
    {
        string accountPath = Path.Combine(saveDirectory, "accountdata.hg");
        if (!File.Exists(accountPath))
            return new AccountData { ErrorMessage = UiStrings.Get("account.not_found") };

        try
        {
            var accountObj = SaveFileManager.LoadSaveFile(accountPath);
            var userSettings = accountObj.GetObject("UserSettingsData") ?? accountObj;

            var seasonUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedSeasonRewards"));
            var twitchUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedTwitchRewards"));
            var platformUnlocked = GetUnlockedSet(userSettings.GetArray("UnlockedPlatformRewards"));

            int total = seasonUnlocked.Count + twitchUnlocked.Count + platformUnlocked.Count;

            return new AccountData
            {
                AccountObject = accountObj,
                AccountFilePath = accountPath,
                SeasonUnlocked = seasonUnlocked,
                TwitchUnlocked = twitchUnlocked,
                PlatformUnlocked = platformUnlocked,
                StatusMessage = UiStrings.Format("account.status_loaded", total),
            };
        }
        catch (Exception ex)
        {
            return new AccountData { ErrorMessage = UiStrings.Format("account.load_error", accountPath, ex.Message) };
        }
    }

    /// <summary>
    /// Saves a list of rewards to a JSON array under the specified key, keeping only unlocked entries.
    /// </summary>
    /// <param name="rewards">The reward entries with their unlock states.</param>
    /// <param name="userSettings">The user settings JSON object to update.</param>
    /// <param name="key">The JSON key for the reward array (e.g. "UnlockedSeasonRewards").</param>
    internal static void SaveRewardList(List<(string Id, bool Unlocked)> rewards, JsonObject userSettings, string key)
    {
        var array = userSettings.GetArray(key);
        if (array == null)
        {
            array = new JsonArray();
            userSettings.Set(key, array);
        }

        array.Clear();
        foreach (var (id, unlocked) in rewards)
        {
            if (unlocked && !string.IsNullOrEmpty(id))
                array.Add(id);
        }
    }

    /// <summary>
    /// Syncs the redeemed rewards arrays in the game save to match the unlocked state.
    /// The game requires both account-level unlock AND per-save redemption arrays.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="seasonRewards">Season reward entries with unlock states.</param>
    /// <param name="twitchRewards">Twitch reward entries with unlock states.</param>
    internal static void SyncRedeemedRewards(JsonObject saveData,
        List<(string Id, bool Unlocked)> seasonRewards,
        List<(string Id, bool Unlocked)> twitchRewards)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        SyncRedeemedArray(playerState, "RedeemedSeasonRewards", seasonRewards);
        SyncRedeemedArray(playerState, "RedeemedTwitchRewards", twitchRewards);
    }

    private static void SyncRedeemedArray(JsonObject playerState, string key, List<(string Id, bool Unlocked)> rewards)
    {
        var array = playerState.GetArray(key);
        if (array == null)
        {
            array = new JsonArray();
            playerState.Set(key, array);
        }

        // Build set of currently redeemed IDs
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < array.Length; i++)
        {
            var val = array.GetString(i);
            if (!string.IsNullOrEmpty(val))
                existing.Add(val);
        }

        // Add newly unlocked rewards that aren't in the redeemed list
        foreach (var (id, unlocked) in rewards)
        {
            if (unlocked && !string.IsNullOrEmpty(id) && !existing.Contains(id))
                array.Add(id);
        }

        // Remove redeemed entries that are no longer unlocked
        var unlockedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, unlocked) in rewards)
        {
            if (unlocked && !string.IsNullOrEmpty(id))
                unlockedSet.Add(id);
        }
        for (int i = array.Length - 1; i >= 0; i--)
        {
            var val = array.GetString(i);
            if (!string.IsNullOrEmpty(val) && !unlockedSet.Contains(val))
                array.RemoveAt(i);
        }
    }

    /// <summary>
    /// Builds display rows from a rewards database and the currently unlocked set,
    /// including any unlocked rewards not found in the database.
    /// </summary>
    /// <param name="rewardsDb">The known rewards database with IDs and display names.</param>
    /// <param name="unlocked">The set of currently unlocked reward IDs.</param>
    /// <returns>A list of reward row data for display in the UI.</returns>
    internal static List<RewardRowData> BuildRewardRows(List<(string Id, string Name)> rewardsDb, HashSet<string> unlocked)
    {
        var rows = new List<RewardRowData>();

        if (rewardsDb.Count > 0)
        {
            foreach (var (id, name) in rewardsDb)
            {
                bool isUnlocked = unlocked.Contains(id);
                rows.Add(new RewardRowData(id, name, isUnlocked));
            }

            foreach (var id in unlocked)
            {
                bool found = false;
                foreach (var (dbId, _) in rewardsDb)
                {
                    if (string.Equals(dbId, id, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    rows.Add(new RewardRowData(id, "(unknown)", true));
            }
        }
        else
        {
            foreach (var id in unlocked)
                rows.Add(new RewardRowData(id, "", true));
        }

        return rows;
    }

    /// <summary>
    /// Holds loaded account data including unlocked reward sets and status information.
    /// </summary>
    internal sealed class AccountData
    {
        /// <summary>The parsed account data JSON object.</summary>
        public JsonObject? AccountObject { get; set; }
        /// <summary>The file path to the account data file.</summary>
        public string? AccountFilePath { get; set; }
        /// <summary>Set of unlocked season reward IDs.</summary>
        public HashSet<string> SeasonUnlocked { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Set of unlocked Twitch reward IDs.</summary>
        public HashSet<string> TwitchUnlocked { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Set of unlocked platform reward IDs.</summary>
        public HashSet<string> PlatformUnlocked { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>A human-readable status message on successful load.</summary>
        public string? StatusMessage { get; set; }
        /// <summary>An error message if account data could not be loaded.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents a single reward row for display in the UI.
    /// </summary>
    internal sealed class RewardRowData
    {
        /// <summary>The reward identifier.</summary>
        public string Id { get; }
        /// <summary>The human-readable reward name.</summary>
        public string Name { get; }
        /// <summary>Whether this reward is currently unlocked.</summary>
        public bool Unlocked { get; }

        /// <summary>
        /// Initializes a new reward row.
        /// </summary>
        /// <param name="id">The reward identifier.</param>
        /// <param name="name">The human-readable name.</param>
        /// <param name="unlocked">Whether the reward is unlocked.</param>
        public RewardRowData(string id, string name, bool unlocked)
        {
            Id = id;
            Name = name;
            Unlocked = unlocked;
        }
    }
}
