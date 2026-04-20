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
    /// Keyword fragments that, when found in a <c>GiveRewardOnSpecialPurchase</c> reward
    /// table ID (e.g. "RS_S13_SHIP", "R_TWIT_GUN01"), indicate the reward is
    /// a non-technology item (ship, frigate, egg, weapon, firework, pet) that
    /// should NOT be added to <c>KnownTech</c>.
    ///
    /// Our database stores the raw MBIN reward table IDs, so we use keyword matching on those IDs.
    /// </summary>
    private static readonly string[] NonTechRewardKeywords =
    {
        "SHIP", "EGG", "FRIG", "FIREW", "FIREWORK", "GUN", "PET"
    };

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

        // Build the desired set of unlocked IDs.
        var desiredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, unlocked) in rewards)
        {
            if (unlocked && !string.IsNullOrEmpty(id))
                desiredSet.Add(id);
        }

        // Remove entries that should no longer be present, preserving order.
        for (int i = array.Length - 1; i >= 0; i--)
        {
            var existing = array.GetString(i);
            if (string.IsNullOrEmpty(existing) || !desiredSet.Contains(existing))
                array.RemoveAt(i);
        }

        // Build current set after removals.
        var currentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < array.Length; i++)
        {
            var existing = array.GetString(i);
            if (!string.IsNullOrEmpty(existing))
                currentSet.Add(existing);
        }

        // Append any newly-unlocked entries.
        foreach (var id in desiredSet)
        {
            if (!currentSet.Contains(id))
                array.Add(id);
        }
    }

    /// <summary>
    /// Synchronises the account-level Seen* arrays (<c>SeenProducts</c>,
    /// <c>SeenTechnologies</c>, <c>SeenSubstances</c>) in <c>UserSettingsData</c>
    /// based on the <b>redeemed</b> state of rewards.
    /// <para>
    /// The Seen* arrays track which reward items the player has <b>redeemed</b>
    /// (claimed) in the game. This is separate from the Unlocked* arrays which
    /// track which rewards are available to redeem. The two are mutually
    /// exclusive: unlock checkboxes write to Unlocked* arrays, while redeem
    /// checkboxes write to Seen* arrays and save-level Redeemed*/Known* arrays.
    /// </para>
    /// <para>
    /// When <b>removing</b> (present=false), the entry is removed from <b>all three</b>
    /// Seen* arrays. The game may place the same item in multiple arrays (e.g.
    /// expedition cosmetics often appear in both SeenProducts and SeenTechnologies),
    /// and items from mixed-type JSON files like Others.json cannot be reliably
    /// mapped to a single array. Removing from all arrays ensures complete cleanup.
    /// </para>
    /// <para>
    /// When <b>adding</b> (present=true), the entry is added to the best-guess
    /// array based on item type (defaulting to SeenProducts for rewards).
    /// </para>
    /// </summary>
    /// <param name="userSettings">The <c>UserSettingsData</c> JSON object from account data.</param>
    /// <param name="rewards">Reward entries with their current redeem/presence states.</param>
    /// <param name="database">Optional game item database for determining item types.</param>
    internal static void SyncAccountSeenArrays(JsonObject userSettings,
        List<(string Id, bool Present)> rewards, GameItemDatabase? database = null)
    {
        foreach (var (id, present) in rewards)
        {
            if (string.IsNullOrEmpty(id)) continue;

            string lookupId = CatalogueLogic.StripCaretPrefix(id);
            string saveId = CatalogueLogic.EnsureCaretPrefix(id);

            if (!present)
            {
                // Remove from ALL Seen* arrays to ensure complete cleanup.
                // The game may store an item in multiple arrays (e.g. expedition
                // cosmetics appear in both SeenProducts and SeenTechnologies),
                // and mixed-type JSON files (Others, Exocraft) mean we cannot
                // reliably predict which array(s) the game used.
                SyncJsonArrayEntry(userSettings, "SeenProducts", saveId, false);
                SyncJsonArrayEntry(userSettings, "SeenTechnologies", saveId, false);
                SyncJsonArrayEntry(userSettings, "SeenSubstances", saveId, false);
            }
            else
            {
                // When adding, place in the best-guess array based on item type.
                var item = database?.GetItem(lookupId);
                string seenArrayName = GetSeenArrayName(item);
                SyncJsonArrayEntry(userSettings, seenArrayName, saveId, true);
            }
        }
    }

    /// <summary>
    /// Determines the Seen* array name for an item when adding.
    /// Uses the item's <see cref="GameItem.SourceTable"/> field which tracks
    /// which NMS game table the item was extracted from (Product, Technology, Substance).
    /// This correctly classifies items from mixed-type JSON files like Others.json
    /// and Exocraft.json, where items from both the product and technology game tables
    /// may coexist under the same ItemType.
    /// Defaults to SeenProducts for unknown items (rewards are nearly always products).
    /// </summary>
    private static string GetSeenArrayName(GameItem? item)
    {
        if (item == null)
            return "SeenProducts"; // Safe default for rewards

        // Prefer SourceTable (set by the Extractor from the game's own table classification).
        if (!string.IsNullOrEmpty(item.SourceTable))
        {
            return item.SourceTable switch
            {
                "Technology" => "SeenTechnologies",
                "Substance" => "SeenSubstances",
                _ => "SeenProducts", // "Product" and any unknown value
            };
        }

        // Fallback for items extracted before SourceTable was added:
        // use substance detection; everything else defaults to SeenProducts.
        if (string.Equals(item.ItemType, "substance", StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.ItemType, "Raw Materials", StringComparison.OrdinalIgnoreCase))
            return "SeenSubstances";

        return "SeenProducts";
    }

    /// <summary>
    /// Reads the redeemed reward sets from the game save data.
    /// Returns sets for season and Twitch rewards that have been redeemed in
    /// this particular save slot (from <c>PlayerStateData.RedeemedSeasonRewards</c>
    /// and <c>PlayerStateData.RedeemedTwitchRewards</c>).
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <returns>A tuple of (seasonRedeemed, twitchRedeemed) hash sets.</returns>
    internal static (HashSet<string> SeasonRedeemed, HashSet<string> TwitchRedeemed) GetRedeemedSets(JsonObject? saveData)
    {
        var seasonRedeemed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var twitchRedeemed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var playerState = saveData?.GetObject("PlayerStateData");
        if (playerState == null) return (seasonRedeemed, twitchRedeemed);

        seasonRedeemed = GetUnlockedSet(playerState.GetArray("RedeemedSeasonRewards"));
        twitchRedeemed = GetUnlockedSet(playerState.GetArray("RedeemedTwitchRewards"));
        return (seasonRedeemed, twitchRedeemed);
    }

    /// <summary>
    /// Saves the redeemed rewards arrays in the game save to match the user's
    /// explicit per-save redemption choices. Unlike the old approach, this does
    /// NOT mirror the account unlock state; it writes only rewards the user
    /// has explicitly ticked as "Redeemed in Save".
    ///
    /// Does NOT synchronise Known* arrays (KnownSpecials, KnownTech). The caller
    /// is responsible for computing the delta of changed items and calling
    /// <see cref="SyncKnownArraysForChangedRewards"/> with only those items.
    /// This prevents spurious modifications to Known* arrays for items the user
    /// didn't touch.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="seasonRedeemed">Season reward entries with explicit redeem states.</param>
    /// <param name="twitchRedeemed">Twitch reward entries with explicit redeem states.</param>
    /// <param name="database">Optional game item database (reserved for future use).</param>
    internal static void SaveRedeemedRewards(JsonObject saveData,
        List<(string Id, bool Redeemed)> seasonRedeemed,
        List<(string Id, bool Redeemed)> twitchRedeemed,
        GameItemDatabase? database = null)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        WriteRedeemedArray(playerState, "RedeemedSeasonRewards", seasonRedeemed);
        WriteRedeemedArray(playerState, "RedeemedTwitchRewards", twitchRedeemed);

        // NOTE: Known* array synchronisation (KnownSpecials, KnownTech) is NOT
        // performed here for the full list. The caller is expected to compute the
        // delta (items whose redeemed state actually changed) and call
        // SyncKnownArraysForChangedRewards with only those items. This prevents
        // massive spurious diffs when the save legitimately has redeemed rewards
        // that are not in KnownTech.
    }

    /// <summary>
    /// Synchronises Known* arrays (KnownSpecials, KnownTech) for a delta-only
    /// list of rewards whose redeemed state was explicitly changed by the user.
    /// Items the user did not touch are left as-is, preserving any out-of-sync
    /// state the player may have for their own in-game reasons.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="changedRewards">Only the rewards whose redeemed state changed.</param>
    /// <param name="database">Optional game item database for item lookups.</param>
    internal static void SyncKnownArraysForChangedRewards(JsonObject saveData,
        List<(string Id, bool Redeemed)> changedRewards, GameItemDatabase? database)
    {
        if (changedRewards.Count == 0) return;
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;
        SyncKnownArraysForRewards(playerState, changedRewards, database);
    }

    /// <summary>
    /// For each reward in the list, adds or removes entries in the Known* arrays
    /// (KnownSpecials, KnownTech) based on the redeemed state.
    /// </summary>
    private static void SyncKnownArraysForRewards(JsonObject playerState,
        List<(string Id, bool Redeemed)> rewards, GameItemDatabase? database)
    {
        foreach (var (id, redeemed) in rewards)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Strip leading "^" for database lookups, add it back for save arrays.
            string lookupId = CatalogueLogic.StripCaretPrefix(id);
            string saveId = CatalogueLogic.EnsureCaretPrefix(id);

            var item = database?.GetItem(lookupId);

            // Determine which Known* arrays this reward should appear in.
            bool isSpecial = item != null
                && string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase);

            // Unless the item's GiveRewardOnSpecialPurchase contains a non-tech
            // keyword (ships, eggs, frigates, weapons, fireworks, pets), it also
            // goes in KnownTech. This matches NomNom's NON_TECH_REWARD check which
            // uses the resolved reward type.
            // When we don't have database info, we conservatively skip KnownTech.
            bool addToKnownTech = item != null && !IsNonTechReward(item);

            if (isSpecial)
                SyncJsonArrayEntry(playerState, "KnownSpecials", saveId, redeemed);

            if (addToKnownTech)
                SyncJsonArrayEntry(playerState, "KnownTech", saveId, redeemed);
        }
    }

    /// <summary>
    /// Determines whether an item's <c>GiveRewardOnSpecialPurchase</c> value indicates
    /// a non-technology reward (ship, egg, frigate, weapon, firework, pet) that should
    /// NOT be added to KnownTech.
    /// <para>
    /// NomNom resolves reward table IDs to type codes ("^SHIP", "^EGG", etc.) and checks
    /// against <c>NON_TECH_REWARD</c>. Our database stores the raw MBIN reward table IDs
    /// (e.g. "RS_S13_SHIP", "R_TWIT_GUN01"), so we use keyword matching instead.
    /// </para>
    /// </summary>
    internal static bool IsNonTechReward(GameItem item)
    {
        string reward = item.GiveRewardOnSpecialPurchase;
        if (string.IsNullOrEmpty(reward))
            return false; // No reward → not non-tech; item goes to KnownTech

        foreach (string keyword in NonTechRewardKeywords)
        {
            if (reward.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Adds or removes a single string entry in a JSON array (e.g. KnownSpecials,
    /// SeenProducts, KnownTech). Prevents duplicates when adding and removes all
    /// occurrences when removing. Used for both save-level arrays (Known*, Redeemed*)
    /// and account-level arrays (Seen*, Unlocked*).
    /// </summary>
    internal static void SyncJsonArrayEntry(JsonObject container, string arrayName,
        string id, bool present)
    {
        var array = container.GetArray(arrayName);
        if (array == null)
        {
            if (!present) return;
            array = new JsonArray();
            container.Set(arrayName, array);
        }

        if (present)
        {
            // Only add if not already present (case-insensitive).
            bool found = false;
            for (int i = 0; i < array.Length; i++)
            {
                if (string.Equals(array.GetString(i), id, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                array.Add(id);
        }
        else
        {
            // Remove all occurrences (case-insensitive).
            for (int i = array.Length - 1; i >= 0; i--)
            {
                if (string.Equals(array.GetString(i), id, StringComparison.OrdinalIgnoreCase))
                    array.RemoveAt(i);
            }
        }
    }

    private static void WriteRedeemedArray(JsonObject playerState, string key,
        List<(string Id, bool Redeemed)> rewards)
    {
        var array = playerState.GetArray(key);
        if (array == null)
        {
            array = new JsonArray();
            playerState.Set(key, array);
        }

        // Build the desired set of redeemed IDs from the grid state.
        var desiredSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, redeemed) in rewards)
        {
            if (redeemed && !string.IsNullOrEmpty(id))
                desiredSet.Add(id);
        }

        // Build a set of all IDs managed by the grid (whether redeemed or not).
        var managedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, _) in rewards)
        {
            if (!string.IsNullOrEmpty(id))
                managedIds.Add(id);
        }

        // Remove existing entries that should no longer be present, preserving order
        // for all other entries (including unknown/unmanaged entries).
        for (int i = array.Length - 1; i >= 0; i--)
        {
            var existing = array.GetString(i);
            if (string.IsNullOrEmpty(existing)) continue;

            // If managed by grid and NOT in the desired set, remove it.
            if (managedIds.Contains(existing) && !desiredSet.Contains(existing))
                array.RemoveAt(i);
        }

        // Build the current set (after removals) to know what's already present.
        var currentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < array.Length; i++)
        {
            var existing = array.GetString(i);
            if (!string.IsNullOrEmpty(existing))
                currentSet.Add(existing);
        }

        // Append any newly-redeemed entries that aren't already in the array.
        foreach (var id in desiredSet)
        {
            if (!currentSet.Contains(id))
                array.Add(id);
        }
    }

    /// <summary>
    /// Cleans stale entries from Known* arrays for rewards that are unlocked on the account
    /// but NOT redeemed in the save. When a user only ticks "Unlocked on Account" without
    /// "Redeemed in Save", any leftover Known* entries from previous gameplay would cause the
    /// game to show the reward as already claimed. This method removes those stale entries.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="seasonRows">Season reward rows with unlock and redeem states.</param>
    /// <param name="twitchRows">Twitch reward rows with unlock and redeem states.</param>
    /// <param name="database">Optional game item database for item lookups.</param>
    internal static void CleanStaleKnownEntries(JsonObject saveData,
        List<(string Id, bool Unlocked, bool Redeemed)> seasonRows,
        List<(string Id, bool Unlocked, bool Redeemed)> twitchRows,
        GameItemDatabase? database = null)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        CleanStaleForRewardList(playerState, seasonRows, database);
        CleanStaleForRewardList(playerState, twitchRows, database);
    }

    private static void CleanStaleForRewardList(JsonObject playerState,
        List<(string Id, bool Unlocked, bool Redeemed)> rows, GameItemDatabase? database)
    {
        foreach (var (id, unlocked, redeemed) in rows)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Only clean entries that are unlocked but NOT redeemed.
            // If redeemed, the Known* entries should be present (handled by SyncKnownArraysForRewards).
            if (!unlocked || redeemed) continue;

            string lookupId = CatalogueLogic.StripCaretPrefix(id);
            string saveId = CatalogueLogic.EnsureCaretPrefix(id);
            var item = database?.GetItem(lookupId);

            bool isSpecial = item != null
                && string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase);

            // Remove from KnownSpecials if present.
            if (isSpecial)
                SyncJsonArrayEntry(playerState, "KnownSpecials", saveId, false);

            // Remove from KnownTech if present (same logic as SyncKnownArraysForRewards).
            if (item != null && !IsNonTechReward(item))
                SyncJsonArrayEntry(playerState, "KnownTech", saveId, false);
        }
    }

    /// <summary>
    /// Describes a single consistency issue found between Redeemed and Known arrays.
    /// </summary>
    internal sealed class ConsistencyIssue
    {
        /// <summary>The item ID (with caret prefix as stored in save data).</summary>
        public string Id { get; init; } = "";
        /// <summary>Human-readable display name for the item.</summary>
        public string Name { get; init; } = "";
        /// <summary>The array the item is currently in (e.g. "RedeemedSeasonRewards").</summary>
        public string CurrentArray { get; init; } = "";
        /// <summary>The array the item is missing from (e.g. "KnownSpecials").</summary>
        public string MissingArray { get; init; } = "";
        /// <summary>Localised description of the issue for display.</summary>
        public string Description { get; init; } = "";
    }

    /// <summary>
    /// Performs a consistency check on the save data, comparing the Redeemed* arrays
    /// against the Known* arrays to find mismatches.
    /// Returns a list of structured issue records.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="database">Optional game item database for resolving display names.</param>
    /// <returns>A list of consistency issues. Empty if the save is consistent.</returns>
    internal static List<ConsistencyIssue> CheckConsistencyStructured(JsonObject saveData, GameItemDatabase? database = null)
    {
        var issues = new List<ConsistencyIssue>();
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return issues;

        var redeemedSeason = GetUnlockedSet(playerState.GetArray("RedeemedSeasonRewards"));
        var redeemedTwitch = GetUnlockedSet(playerState.GetArray("RedeemedTwitchRewards"));
        var knownSpecials = GetUnlockedSet(playerState.GetArray("KnownSpecials"));
        var knownTech = GetUnlockedSet(playerState.GetArray("KnownTech"));

        // Check: redeemed season rewards should have matching Known* entries.
        CheckRedeemedAgainstKnown(issues, redeemedSeason, "RedeemedSeasonRewards",
            knownSpecials, knownTech, database);

        // Check: redeemed Twitch rewards should have matching Known* entries.
        CheckRedeemedAgainstKnown(issues, redeemedTwitch, "RedeemedTwitchRewards",
            knownSpecials, knownTech, database);

        // Check: Known* entries that correspond to rewards but are NOT in any Redeemed* array.
        var allRedeemed = new HashSet<string>(redeemedSeason, StringComparer.OrdinalIgnoreCase);
        foreach (var id in redeemedTwitch)
            allRedeemed.Add(id);

        foreach (var id in knownSpecials)
        {
            if (allRedeemed.Contains(id)) continue;

            string lookupId = CatalogueLogic.StripCaretPrefix(id);
            var item = database?.GetItem(lookupId);

            bool isSpecial = item != null
                && string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase);
            if (isSpecial)
            {
                string name = item?.Name ?? id;
                issues.Add(new ConsistencyIssue
                {
                    Id = id,
                    Name = name,
                    CurrentArray = "KnownSpecials",
                    MissingArray = "RedeemedSeasonRewards",
                    Description = UiStrings.Format("account.consistency_stale_entry", name, id, "Known Specials"),
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Checks a set of redeemed IDs against the Known* arrays and records any mismatches.
    /// </summary>
    private static void CheckRedeemedAgainstKnown(
        List<ConsistencyIssue> issues,
        HashSet<string> redeemed,
        string redeemedArrayName,
        HashSet<string> knownSpecials,
        HashSet<string> knownTech,
        GameItemDatabase? database)
    {
        foreach (var id in redeemed)
        {
            string lookupId = CatalogueLogic.StripCaretPrefix(id);
            var item = database?.GetItem(lookupId);
            string name = item?.Name ?? id;

            bool isSpecial = item != null
                && string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase);

            if (isSpecial && !knownSpecials.Contains(id))
            {
                issues.Add(new ConsistencyIssue
                {
                    Id = id,
                    Name = name,
                    CurrentArray = redeemedArrayName,
                    MissingArray = "KnownSpecials",
                    Description = UiStrings.Get("account.consistency_missing_specials"),
                });
            }

            bool shouldBeInTech = item != null && !IsNonTechReward(item);
            if (shouldBeInTech && !knownTech.Contains(id))
            {
                issues.Add(new ConsistencyIssue
                {
                    Id = id,
                    Name = name,
                    CurrentArray = redeemedArrayName,
                    MissingArray = "KnownTech",
                    Description = UiStrings.Get("account.consistency_missing_tech"),
                });
            }
        }
    }

    /// <summary>
    /// Performs a consistency check and returns human-readable issue descriptions.
    /// Convenience wrapper around <see cref="CheckConsistencyStructured"/>.
    /// </summary>
    internal static List<string> CheckConsistency(JsonObject saveData, GameItemDatabase? database = null)
    {
        var structured = CheckConsistencyStructured(saveData, database);
        var issues = new List<string>(structured.Count);
        foreach (var issue in structured)
            issues.Add(issue.Description);
        return issues;
    }

    /// <summary>
    /// Resolves a single consistency issue by either adding the item to the missing
    /// array or removing it from the current array.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="issue">The consistency issue to resolve.</param>
    /// <param name="addToMissing">
    /// When true, adds the item to the missing array.
    /// When false, removes the item from the current array.
    /// </param>
    internal static void ResolveConsistencyIssue(JsonObject saveData, ConsistencyIssue issue, bool addToMissing)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        if (addToMissing)
        {
            SyncJsonArrayEntry(playerState, issue.MissingArray, issue.Id, present: true);
        }
        else
        {
            SyncJsonArrayEntry(playerState, issue.CurrentArray, issue.Id, present: false);
        }
    }

    /// <summary>
    /// Builds display rows from a rewards database and the currently unlocked and
    /// redeemed sets, including any unlocked rewards not found in the database.
    /// </summary>
    /// <param name="rewardsDb">The known rewards database entries with IDs, names, and metadata.</param>
    /// <param name="unlocked">The set of currently unlocked reward IDs (account level).</param>
    /// <param name="redeemed">The set of currently redeemed reward IDs (save level). May be empty.</param>
    /// <returns>A list of reward row data for display in the UI.</returns>
    internal static List<RewardRowData> BuildRewardRows(
        List<RewardDbEntry> rewardsDb,
        HashSet<string> unlocked,
        HashSet<string>? redeemed = null)
    {
        redeemed ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<RewardRowData>();

        if (rewardsDb.Count > 0)
        {
            foreach (var entry in rewardsDb)
            {
                bool isUnlocked = unlocked.Contains(entry.Id);
                bool isRedeemed = redeemed.Contains(entry.Id);
                rows.Add(new RewardRowData(entry.Id, entry.Name,
                    unlocked: isUnlocked, redeemed: isRedeemed,
                    seasonId: entry.SeasonId, stageId: entry.StageId,
                    mustBeUnlocked: entry.MustBeUnlocked));
            }

            foreach (var id in unlocked)
            {
                bool found = false;
                foreach (var entry in rewardsDb)
                {
                    if (string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    rows.Add(new RewardRowData(id, "(unknown)", true, redeemed.Contains(id)));
            }
        }
        else
        {
            foreach (var id in unlocked)
                rows.Add(new RewardRowData(id, "", true, redeemed.Contains(id)));
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
    /// A lightweight entry from the rewards database for use in <see cref="BuildRewardRows"/>.
    /// </summary>
    internal sealed class RewardDbEntry
    {
        public string Id { get; init; } = "";
        public string Name { get; init; } = "";
        /// <summary>Expedition/season number (-1 if not applicable).</summary>
        public int SeasonId { get; init; } = -1;
        /// <summary>Progression stage within the expedition (-1 if not applicable).</summary>
        public int StageId { get; init; } = -1;
        /// <summary>Whether this reward requires explicit account-level unlocking.</summary>
        public bool MustBeUnlocked { get; init; }
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
        /// <summary>Whether this reward is unlocked on the account.</summary>
        public bool Unlocked { get; }
        /// <summary>Whether this reward is redeemed in the current save slot.</summary>
        public bool Redeemed { get; }
        /// <summary>Expedition/season number this reward belongs to (-1 if not applicable).</summary>
        public int SeasonId { get; }
        /// <summary>Progression stage within the expedition (-1 if not applicable).</summary>
        public int StageId { get; }
        /// <summary>Whether this reward requires explicit account-level unlocking.</summary>
        public bool MustBeUnlocked { get; }

        /// <summary>
        /// Initializes a new reward row.
        /// </summary>
        public RewardRowData(string id, string name, bool unlocked, bool redeemed = false,
            int seasonId = -1, int stageId = -1, bool mustBeUnlocked = false)
        {
            Id = id;
            Name = name;
            Unlocked = unlocked;
            Redeemed = redeemed;
            SeasonId = seasonId;
            StageId = stageId;
            MustBeUnlocked = mustBeUnlocked;
        }
    }
}
