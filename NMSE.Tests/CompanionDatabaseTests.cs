using NMSE.Data;
using Xunit;

namespace NMSE.Tests;

/// <summary>
/// Tests for the PetBiomeAffinityMap static methods including
/// game-correct affinity name resolution, emoji lookup, display names,
/// and weak/strong matchup data.
/// </summary>
public class CompanionDatabaseTests
{
    // --- GetAffinityGameName ---

    [Theory]
    [InlineData("Toxic", "Toxic")]
    [InlineData("Radioactive", "Radioactive")]
    [InlineData("Fire", "Fire")]
    [InlineData("Cold", "Frost")]
    [InlineData("Frozen", "Frost")]
    [InlineData("Lush", "Tropical")]
    [InlineData("Barren", "Desert")]
    [InlineData("Weird", "Anomalous")]
    [InlineData("Mech", "Mechanical")]
    [InlineData("Normal", "Normal")]
    public void GetAffinityGameName_MapsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, PetBiomeAffinityMap.GetAffinityGameName(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetAffinityGameName_EmptyOrNull_ReturnsEmpty(string? input)
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityGameName(input!));
    }

    [Fact]
    public void GetAffinityGameName_CaseInsensitive()
    {
        Assert.Equal("Frost", PetBiomeAffinityMap.GetAffinityGameName("cold"));
        Assert.Equal("Tropical", PetBiomeAffinityMap.GetAffinityGameName("LUSH"));
        Assert.Equal("Desert", PetBiomeAffinityMap.GetAffinityGameName("bArReN")); //Spongebob.png
    }

    [Fact]
    public void GetAffinityGameName_UnknownInput_ReturnsInputUnchanged()
    {
        Assert.Equal("SomeUnknownType", PetBiomeAffinityMap.GetAffinityGameName("SomeUnknownType"));
    }

    // --- GetAffinityEmoji ---

    [Theory]
    [InlineData("Toxic", "☠️")]
    [InlineData("Radioactive", "☢️")]
    [InlineData("Fire", "🔥")]
    [InlineData("Cold", "❄️")]
    [InlineData("Frozen", "❄️")]
    [InlineData("Frost", "❄️")]
    [InlineData("Lush", "🌿")]
    [InlineData("Tropical", "🌿")]
    [InlineData("Barren", "☀️")]
    [InlineData("Desert", "☀️")]
    [InlineData("Weird", "🔮")]
    [InlineData("Anomalous", "🔮")]
    [InlineData("Mech", "⚙️")]
    [InlineData("Mechanical", "⚙️")]
    [InlineData("Normal", "⭐")]
    public void GetAffinityEmoji_AllTypes_ReturnExpected(string input, string expected)
    {
        Assert.Equal(expected, PetBiomeAffinityMap.GetAffinityEmoji(input));
    }

    [Fact]
    public void GetAffinityEmoji_UnknownType_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityEmoji("SomeUnknown"));
    }

    [Fact]
    public void GetAffinityEmoji_Null_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityEmoji(null!));
    }

    // --- GetAffinityDisplayName ---

    [Theory]
    [InlineData("Toxic", "Toxic")]
    [InlineData("Cold", "Frost")]
    [InlineData("Lush", "Tropical")]
    [InlineData("Barren", "Desert")]
    [InlineData("Weird", "Anomalous")]
    [InlineData("Mech", "Mechanical")]
    public void GetAffinityDisplayName_ContainsGameName(string input, string expectedName)
    {
        string result = PetBiomeAffinityMap.GetAffinityDisplayName(input);
        Assert.Contains(expectedName, result);
    }

    [Fact]
    public void GetAffinityDisplayName_ContainsEmoji()
    {
        string result = PetBiomeAffinityMap.GetAffinityDisplayName("Fire");
        Assert.Contains("\U0001F525", result);
        Assert.Contains("Fire", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetAffinityDisplayName_EmptyOrNull_ReturnsEmpty(string? input)
    {
        Assert.Equal("", PetBiomeAffinityMap.GetAffinityDisplayName(input!));
    }

    // --- GetAffinityMatchup ---

    [Fact]
    public void GetAffinityMatchup_Toxic_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Toxic");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Desert", "Frost" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Tropical", "Radioactive" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Desert_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Desert");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Tropical", "Mechanical" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Toxic", "Fire" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Frost_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Frost");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Radioactive", "Fire" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Toxic", "Mechanical" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Anomalous_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Anomalous");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Fire", "Tropical" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Radioactive", "Mechanical" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Mechanical_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Mechanical");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Frost", "Anomalous" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Desert", "Tropical" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Tropical_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Tropical");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Toxic", "Mechanical" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Desert", "Anomalous" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Radioactive_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Radioactive");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Toxic", "Anomalous" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Fire", "Frost" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Fire_ReturnsCorrectWeakStrong()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("Fire");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Desert", "Radioactive" }, matchup!.Value.Weak);
        Assert.Equal(new[] { "Frost", "Anomalous" }, matchup.Value.Strong);
    }

    [Fact]
    public void GetAffinityMatchup_Normal_ReturnsNull()
    {
        Assert.Null(PetBiomeAffinityMap.GetAffinityMatchup("Normal"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GetAffinityMatchup_EmptyOrNull_ReturnsNull(string? input)
    {
        Assert.Null(PetBiomeAffinityMap.GetAffinityMatchup(input!));
    }

    [Fact]
    public void GetAffinityMatchup_CaseInsensitive()
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup("TOXIC");
        Assert.NotNull(matchup);
        Assert.Equal(new[] { "Desert", "Frost" }, matchup!.Value.Weak);
    }

    // --- FormatAffinityList ---

    [Fact]
    public void FormatAffinityList_FormatsWithEmojis()
    {
        var result = PetBiomeAffinityMap.FormatAffinityList(new[] { "Desert", "Frost" });
        Assert.Contains("Desert", result);
        Assert.Contains("Frost", result);
        Assert.Contains("☀️", result); // Desert emoji
        Assert.Contains("❄️", result); // Frost emoji
    }

    [Fact]
    public void FormatAffinityList_EmptyArray_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.FormatAffinityList(Array.Empty<string>()));
    }

    [Fact]
    public void FormatAffinityList_NullArray_ReturnsEmpty()
    {
        Assert.Equal("", PetBiomeAffinityMap.FormatAffinityList(null!));
    }

    [Fact]
    public void FormatAffinityList_CustomSeparator()
    {
        var result = PetBiomeAffinityMap.FormatAffinityList(new[] { "Fire", "Toxic" }, " | ");
        Assert.Contains(" | ", result);
    }

    // --- AllMatchupsHaveTwoEntries ---

    [Theory]
    [InlineData("Toxic")]
    [InlineData("Desert")]
    [InlineData("Frost")]
    [InlineData("Anomalous")]
    [InlineData("Mechanical")]
    [InlineData("Tropical")]
    [InlineData("Radioactive")]
    [InlineData("Fire")]
    public void GetAffinityMatchup_AllTypes_HaveTwoWeakAndTwoStrong(string affinity)
    {
        var matchup = PetBiomeAffinityMap.GetAffinityMatchup(affinity);
        Assert.NotNull(matchup);
        Assert.Equal(2, matchup!.Value.Weak.Length);
        Assert.Equal(2, matchup.Value.Strong.Length);
    }
}
