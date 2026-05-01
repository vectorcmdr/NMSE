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
    /// a non-technology item (ship, frigate, egg, weapon, firework, pet, trail, bobble,
    /// staff, laser attachment, or special redeemable) that should NOT be added to
    /// <c>KnownTech</c>.
    ///
    /// Corvette parts are handled separately via an <c>ItemType</c> check in
    /// <see cref="IsNonTechReward"/> rather than keywords, because their reward table
    /// IDs use many varied suffixes (ENGINE, TURRET, SHIELD, WING, DECO, TRIM, STR, etc.).
    ///
    /// Our database stores the raw MBIN reward table IDs, so we use keyword matching on those IDs.
    /// </summary>
    private static readonly string[] NonTechRewardKeywords =
    {
        "SHIP", "EGG", "FRIG", "FIREW", "FIREWORK", "GUN", "PET",
        "TRAIL",   // starship exhaust trail cosmetics  (RS_S6_TRAIL, RS_S7_TRAIL, RS_S19_TRAIL, RS_S20_TRAIL)
        "BOBBLE",  // bobblehead / figurine cosmetics   (R_BOBBLE_ATLAS, R_BOBBLE_OCTO, etc.)
        "STAFF",   // staff-type multitools             (RS_S12_STAFF, RS_S17_STAFF, RS_S18_STAFF)
        "LASER",   // laser-type tool attachments       (RS_S15_FSHLASER)
        "SPEC",    // exclusive special redeemables     (RS_S2_SPEC e.g. Normandy frigate)
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
    /// Synchronises Known* arrays (KnownSpecials, KnownProducts, KnownTech) for a
    /// delta-only list of rewards whose redeemed state was explicitly changed by the user.
    /// Items the user did not touch are left as-is, preserving any out-of-sync
    /// state the player may have for their own in-game reasons.
    /// </summary>
    /// <param name="saveData">The game save data object.</param>
    /// <param name="changedRewards">Only the rewards whose redeemed state changed.</param>
    /// <param name="database">Optional game item database for item lookups.</param>
    /// <param name="productIdMap">
    /// Optional map from reward ID (e.g. <c>^TWITCH_376</c>) to bare product ID
    /// (e.g. <c>EXPD_POSTER11A</c>). Required for twitch rewards: the game stores
    /// the product ID in <c>KnownSpecials</c> and <c>KnownProducts</c>, not the TwitchId.
    /// When absent, the reward ID is used as the product ID (correct for season rewards).
    /// </param>
    internal static void SyncKnownArraysForChangedRewards(JsonObject saveData,
        List<(string Id, bool Redeemed)> changedRewards, GameItemDatabase? database,
        IReadOnlyDictionary<string, string>? productIdMap = null)
    {
        if (changedRewards.Count == 0) return;
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;
        SyncKnownArraysForRewards(playerState, changedRewards, database, productIdMap);
    }

    /// <summary>
    /// For each reward in the list, adds or removes entries in the Known* arrays
    /// (KnownSpecials, KnownProducts, KnownTech) based on the redeemed state.
    /// </summary>
    private static void SyncKnownArraysForRewards(JsonObject playerState,
        List<(string Id, bool Redeemed)> rewards, GameItemDatabase? database,
        IReadOnlyDictionary<string, string>? productIdMap = null)
    {
        foreach (var (id, redeemed) in rewards)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Resolve to product ID (e.g. ^TWITCH_376 -> ^EXPD_POSTER11A for twitch rewards).
            // KnownSpecials and KnownProducts store product IDs, not TwitchIds.
            var (lookupId, saveId) = ResolveProductId(id, productIdMap);

            var item = database?.GetItem(lookupId);

            // Determine which Known* arrays this reward should appear in.
            bool isSpecial = item != null && IsKnownSpecialsItem(item);

            if (isSpecial)
                SyncJsonArrayEntry(playerState, "KnownSpecials", saveId, redeemed);

            // Building-type rewards (decorations, posters, decals) are also tracked in
            // KnownProducts in addition to KnownSpecials, matching the game's behaviour.
            if (item?.IsBuilding == true)
                SyncJsonArrayEntry(playerState, "KnownProducts", saveId, redeemed);

            // Only add to KnownTech if the item actually gives a technology reward.
            // Cosmetic-only items (empty GiveRewardOnSpecialPurchase) are NOT tech rewards.
            // When we don't have database info, we conservatively skip KnownTech.
            bool addToKnownTech = item != null && !IsNonTechReward(item);

            if (addToKnownTech)
                SyncJsonArrayEntry(playerState, "KnownTech", saveId, redeemed);
        }
    }

    /// <summary>
    /// Resolves a reward ID to its product ID for use in Known* array operations.
    /// For season rewards the reward ID already is the product ID (e.g. <c>^VAULT_ARMOUR</c>).
    /// For twitch rewards the TwitchId (e.g. <c>^TWITCH_376</c>) must be mapped to the
    /// product ID (e.g. <c>^EXPD_POSTER11A</c>) via <paramref name="productIdMap"/>.
    /// </summary>
    /// <returns>
    /// A tuple of (lookupId, saveId) where lookupId is the bare ID for database lookups
    /// (no caret) and saveId is the caret-prefixed ID for JSON array operations.
    /// </returns>
    private static (string LookupId, string SaveId) ResolveProductId(
        string rewardId, IReadOnlyDictionary<string, string>? productIdMap)
    {
        if (productIdMap != null
            && productIdMap.TryGetValue(rewardId, out var rawProductId)
            && !string.IsNullOrEmpty(rawProductId))
        {
            return (rawProductId, CatalogueLogic.EnsureCaretPrefix(rawProductId));
        }
        return (CatalogueLogic.StripCaretPrefix(rewardId), CatalogueLogic.EnsureCaretPrefix(rewardId));
    }

    /// <summary>
    /// Determines whether an item should NOT be added to <c>KnownTech</c> when redeemed.
    /// Returns <c>true</c> (non-tech) for:
    /// <list type="bullet">
    ///   <item>Cosmetic-only items (empty <c>GiveRewardOnSpecialPurchase</c>).</item>
    ///   <item>Corvette parts (wings, turrets, shields, decorations, etc.) - detected via
    ///         <c>ItemType == "Corvette"</c> because their reward IDs use many varied suffixes.</item>
    ///   <item>Items whose reward table ID contains a non-tech keyword
    ///         (ship, egg, frigate, weapon, firework, pet, trail, bobble, staff, laser, spec).</item>
    /// </list>
    /// Returns <c>false</c> only when the item has a non-empty reward ID that does not
    /// match any known non-tech keyword or item type.
    /// <para>
    /// An empty <c>GiveRewardOnSpecialPurchase</c> means the item is cosmetic-only
    /// (the Extractor leaves it empty for all purely decorative rewards).
    /// </para>
    /// <para>
    /// When <c>GiveRewardOnSpecialPurchase</c> is non-empty, we keyword-match against
    /// the raw MBIN reward table ID (e.g. "RS_S13_SHIP", "R_TWIT_GUN01") to identify
    /// non-technology reward types, mirroring NomNom's NON_TECH_REWARD set.
    /// </para>
    /// </summary>
    internal static bool IsNonTechReward(GameItem item)
    {
        // Corvette parts (B_WNG_*, B_TUR_*, B_SHL_*, B_STR_*, B_DECO_*, etc.) are never
        // added to KnownTech - they are tracked through the corvette part system.
        // Their reward IDs use many varied suffixes (ENGINE, TURRET, SHIELD, WING, DECO,
        // TRIM, STR, etc.), so we detect them via ItemType rather than keyword matching.
        if (item.ItemType.Equals("Corvette", StringComparison.OrdinalIgnoreCase))
            return true;

        string reward = item.GiveRewardOnSpecialPurchase;
        if (string.IsNullOrEmpty(reward))
            return true; // No reward specified -> cosmetic item; do NOT add to KnownTech

        foreach (string keyword in NonTechRewardKeywords)
        {
            if (reward.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> when a redeemed <paramref name="item"/> should be tracked in
    /// <c>KnownSpecials</c>. All <c>SpecialShop</c> products are tracked there, with the
    /// exception of Exocraft cosmetic customisation parts (cabin, body, wheels, paint)
    /// which use the vehicle visual system and are not stored in <c>KnownSpecials</c>.
    /// </summary>
    private static bool IsKnownSpecialsItem(GameItem item) =>
        string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase)
        && !item.Subtitle.Contains("Exocraft Customisation", StringComparison.OrdinalIgnoreCase);

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
    /// <param name="productIdMap">
    /// Optional map from reward ID (e.g. <c>^TWITCH_376</c>) to bare product ID
    /// (e.g. <c>EXPD_POSTER11A</c>). Required for twitch rewards: the game stores
    /// the product ID in <c>KnownSpecials</c> and <c>KnownProducts</c>, not the TwitchId.
    /// </param>
    internal static void CleanStaleKnownEntries(JsonObject saveData,
        List<(string Id, bool Unlocked, bool Redeemed)> seasonRows,
        List<(string Id, bool Unlocked, bool Redeemed)> twitchRows,
        GameItemDatabase? database = null,
        IReadOnlyDictionary<string, string>? productIdMap = null)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        CleanStaleForRewardList(playerState, seasonRows, database, productIdMap);
        CleanStaleForRewardList(playerState, twitchRows, database, productIdMap);
    }

    private static void CleanStaleForRewardList(JsonObject playerState,
        List<(string Id, bool Unlocked, bool Redeemed)> rows, GameItemDatabase? database,
        IReadOnlyDictionary<string, string>? productIdMap = null)
    {
        foreach (var (id, unlocked, redeemed) in rows)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Only clean entries that are unlocked but NOT redeemed.
            // If redeemed, the Known* entries should be present (handled by SyncKnownArraysForRewards).
            if (!unlocked || redeemed) continue;

            // Resolve to product ID (e.g. ^TWITCH_376 -> ^EXPD_POSTER11A for twitch rewards).
            var (lookupId, saveId) = ResolveProductId(id, productIdMap);
            var item = database?.GetItem(lookupId);

            bool isSpecial = item != null && IsKnownSpecialsItem(item);

            // Remove from KnownSpecials if present.
            if (isSpecial)
                SyncJsonArrayEntry(playerState, "KnownSpecials", saveId, false);

            // Building-type rewards: also remove from KnownProducts.
            if (item?.IsBuilding == true)
                SyncJsonArrayEntry(playerState, "KnownProducts", saveId, false);

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
        var knownProducts = GetUnlockedSet(playerState.GetArray("KnownProducts"));
        var knownTech = GetUnlockedSet(playerState.GetArray("KnownTech"));

        // Build a map from TwitchId (^TWITCH_NNN) -> caret-prefixed product ID (^EXPD_POSTER11A)
        // from the RewardDatabase. Used for KnownSpecials and KnownProducts checks which store
        // product IDs, not TwitchIds.
        var twitchToProduct = BuildTwitchProductMap();

        // Check: redeemed season rewards should have matching Known* entries.
        // Season reward IDs are already product IDs (no translation needed).
        CheckRedeemedAgainstKnown(issues, redeemedSeason, "RedeemedSeasonRewards",
            null, knownSpecials, knownProducts, knownTech, database);

        // Check: redeemed Twitch rewards should have matching Known* entries.
        // TwitchIds must be resolved to product IDs for KnownSpecials/KnownProducts checks.
        CheckRedeemedAgainstKnown(issues, redeemedTwitch, "RedeemedTwitchRewards",
            twitchToProduct, knownSpecials, knownProducts, knownTech, database);

        // --- Reverse / stale check ---
        // An item is stale in KnownSpecials if it is a KNOWN REWARD product ID that is NOT
        // currently redeemed. Quicksilver-vendor items (purchasable specials) legitimately
        // live in KnownSpecials without a Redeemed* entry; those are NOT flagged.

        // Build set of all known redeemable product IDs (from our reward database).
        var redeemableProductIds = BuildRedeemableProductIds();

        // Build set of currently-redeemed product IDs.
        // Season rewards: reward ID == product ID.
        // Twitch rewards: resolve TwitchId -> product ID via twitchToProduct map.
        var redeemedProductIds = new HashSet<string>(redeemedSeason, StringComparer.OrdinalIgnoreCase);
        foreach (var twitchId in redeemedTwitch)
        {
            if (twitchToProduct.TryGetValue(twitchId, out var pid))
                redeemedProductIds.Add(pid);
        }

        foreach (var id in knownSpecials)
        {
            // Skip if this is not a known redeemable reward product ID
            // (it's a Quicksilver-vendor or other non-redeemable special).
            if (!redeemableProductIds.Contains(id)) continue;

            // Skip if correctly redeemed.
            if (redeemedProductIds.Contains(id)) continue;

            string lookupId = CatalogueLogic.StripCaretPrefix(id);
            var item = database?.GetItem(lookupId);
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

        return issues;
    }

    /// <summary>
    /// Builds a map from TwitchId (e.g. <c>^TWITCH_376</c>) to caret-prefixed product ID
    /// (e.g. <c>^EXPD_POSTER11A</c>) using the <see cref="RewardDatabase"/>.
    /// Used so that KnownSpecials and KnownProducts checks can look up the product ID
    /// for twitch rewards (those arrays store product IDs, not TwitchIds).
    /// </summary>
    private static Dictionary<string, string> BuildTwitchProductMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reward in RewardDatabase.TwitchRewards)
        {
            if (!string.IsNullOrEmpty(reward.ProductId))
                map[reward.Id] = CatalogueLogic.EnsureCaretPrefix(reward.ProductId);
        }
        return map;
    }

    /// <summary>
    /// Builds a set of all known redeemable product IDs from the <see cref="RewardDatabase"/>.
    /// Used by the stale check to distinguish known reward product IDs (which should appear
    /// in a Redeemed* array) from Quicksilver-vendor items (which legitimately appear only
    /// in KnownSpecials).
    /// </summary>
    private static HashSet<string> BuildRedeemableProductIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Season reward IDs are already caret-prefixed product IDs.
        foreach (var reward in RewardDatabase.SeasonRewards)
            set.Add(reward.Id);
        // Twitch rewards: add the product ID (which is what's stored in KnownSpecials).
        foreach (var reward in RewardDatabase.TwitchRewards)
        {
            if (!string.IsNullOrEmpty(reward.ProductId))
                set.Add(CatalogueLogic.EnsureCaretPrefix(reward.ProductId));
        }
        return set;
    }

    /// <summary>
    /// Checks a set of redeemed IDs against the Known* arrays and records any mismatches.
    /// </summary>
    /// <param name="twitchToProduct">
    /// When non-null, maps TwitchIds to product IDs for KnownSpecials/KnownProducts lookups.
    /// Pass null for season rewards (their IDs are already product IDs).
    /// </param>
    private static void CheckRedeemedAgainstKnown(
        List<ConsistencyIssue> issues,
        HashSet<string> redeemed,
        string redeemedArrayName,
        Dictionary<string, string>? twitchToProduct,
        HashSet<string> knownSpecials,
        HashSet<string> knownProducts,
        HashSet<string> knownTech,
        GameItemDatabase? database)
    {
        foreach (var id in redeemed)
        {
            // Resolve to product ID for KnownSpecials/KnownProducts/database lookups.
            // Twitch rewards store TwitchId in RedeemedTwitchRewards but product ID in
            // KnownSpecials and KnownProducts.
            string productId = id;
            if (twitchToProduct != null && twitchToProduct.TryGetValue(id, out var pid))
                productId = pid;

            string lookupId = CatalogueLogic.StripCaretPrefix(productId);
            var item = database?.GetItem(lookupId);
            string name = item?.Name ?? productId;

            bool isSpecial = item != null && IsKnownSpecialsItem(item);

            if (isSpecial && !knownSpecials.Contains(productId))
            {
                issues.Add(new ConsistencyIssue
                {
                    Id = productId,
                    Name = name,
                    CurrentArray = redeemedArrayName,
                    MissingArray = "KnownSpecials",
                    Description = UiStrings.Get("account.consistency_missing_specials"),
                });
            }

            // Building-type rewards are also tracked in KnownProducts.
            if (item?.IsBuilding == true && !knownProducts.Contains(productId))
            {
                issues.Add(new ConsistencyIssue
                {
                    Id = productId,
                    Name = name,
                    CurrentArray = redeemedArrayName,
                    MissingArray = "KnownProducts",
                    Description = UiStrings.Get("account.consistency_missing_products"),
                });
            }

            bool shouldBeInTech = item != null && !IsNonTechReward(item);
            if (shouldBeInTech && !knownTech.Contains(productId))
            {
                issues.Add(new ConsistencyIssue
                {
                    Id = productId,
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
