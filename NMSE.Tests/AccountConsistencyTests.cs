using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for the account reward consistency checker and its redeemed/known pairing.
/// </summary>
[Collection("MutableStaticDatabases")]
public class AccountConsistencyTests
{
    [Fact]
    public void StaleKnownSpecial_IsReportedAsAmbiguousWhenAccountUnlocked()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = LoadDatabase(jsonDir);
        var save = PlayerSave();
        Add(save, "KnownSpecials", "^SWARM_GUN");
        var account = new JsonObject();
        Add(account, "UnlockedSeasonRewards", "^SWARM_GUN");

        var issues = AccountLogic.CheckConsistencyStructured(save, db, account);

        var issue = Assert.Single(issues, i => i.CurrentArray == "KnownSpecials" && i.Id == "^SWARM_GUN");
        Assert.Equal("RedeemedSeasonRewards", issue.MissingArray);
        Assert.True(issue.IsAmbiguous);
    }

    [Fact]
    public void StaleKnownProduct_IsReportedForBuildingReward()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = LoadDatabase(jsonDir);
        var reward = RewardDatabase.SeasonRewards.FirstOrDefault(r =>
            string.Equals(r.ProductId, "EXPD_FLAG_06E", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Id, "^EXPD_FLAG_06E", StringComparison.OrdinalIgnoreCase));
        if (reward == null) return;

        var save = PlayerSave();
        Add(save, "KnownProducts", "^EXPD_FLAG_06E");

        var issues = AccountLogic.CheckConsistencyStructured(save, db);

        Assert.Contains(issues, i =>
            i.CurrentArray == "KnownProducts"
            && i.Id == "^EXPD_FLAG_06E"
            && i.MissingArray == "RedeemedSeasonRewards");
    }

    [Fact]
    public void EntitlementKnownSpecial_IsNotReportedAsStale()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = LoadDatabase(jsonDir);
        var entitlement = RewardDatabase.EntitlementRewards
            .FirstOrDefault(r => !string.IsNullOrEmpty(r.ProductId));
        if (entitlement == null) return;

        var save = PlayerSave();
        Add(save, "KnownSpecials", "^" + entitlement.ProductId);

        var issues = AccountLogic.CheckConsistencyStructured(save, db);

        Assert.DoesNotContain(issues, i =>
            i.CurrentArray == "KnownSpecials"
            && string.Equals(i.Id, "^" + entitlement.ProductId, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RedeemedSeasonRewardWithoutKnownSpecial_IsReported()
    {
        var jsonDir = FindResourceJsonDir();
        if (jsonDir == null) return;

        var db = LoadDatabase(jsonDir);
        var save = PlayerSave();
        Add(save, "RedeemedSeasonRewards", "^SWARM_GUN");

        var issues = AccountLogic.CheckConsistencyStructured(save, db);

        Assert.Contains(issues, i =>
            i.CurrentArray == "RedeemedSeasonRewards"
            && i.MissingArray == "KnownSpecials"
            && i.Id == "^SWARM_GUN");
    }

    private static GameItemDatabase LoadDatabase(string jsonDir)
    {
        RewardDatabase.LoadFromJsonDirectory(jsonDir);
        var db = new GameItemDatabase();
        db.LoadItemsFromJsonDirectory(jsonDir);
        return db;
    }

    private static JsonObject PlayerSave()
    {
        var save = new JsonObject();
        save.Set("PlayerStateData", new JsonObject());
        return save;
    }

    private static void Add(JsonObject save, string arrayName, string id)
    {
        var player = save.GetObject("PlayerStateData") ?? save;
        var array = player.GetArray(arrayName);
        if (array == null)
        {
            array = new JsonArray();
            player.Set(arrayName, array);
        }
        array.Add(id);
    }

    private static string? FindResourceJsonDir()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "Resources", "json");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }
}

