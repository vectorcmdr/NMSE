using System.Globalization;
using System.Text.RegularExpressions;
using NMSE.Core;
using NMSE.Core.Utilities;

namespace NMSE.Tests;

/// <summary>
/// Tests for procedural tech item seed generation, stripping, and formatting.
/// Procedural items (UP_*) use a 5-digit zero-padded seed appended as #NNNNN.
/// </summary>
public class ProceduralSeedTests
{
    [Fact]
    public void GenerateProceduralSeed_Returns5DigitString()
    {
        for (int i = 0; i < 100; i++)
        {
            string seed = ProceduralSeedHelper.Generate();
            Assert.Equal(5, seed.Length);
            Assert.Matches(@"^\d{5}$", seed);
        }
    }

    [Fact]
    public void GenerateProceduralSeed_AllDigitsAreNumeric()
    {
        string seed = ProceduralSeedHelper.Generate();
        Assert.True(int.TryParse(seed, out int val));
        Assert.InRange(val, 0, 99999);
    }

    [Fact]
    public void StripProceduralSeed_WithSeed_SplitsCorrectly()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^UP_LAUNX#91934");
        Assert.Equal("^UP_LAUNX", baseId);
        Assert.Equal("91934", seed);
    }

    [Fact]
    public void StripProceduralSeed_WithoutSeed_ReturnsOriginal()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^HYPERDRIVE");
        Assert.Equal("^HYPERDRIVE", baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void StripProceduralSeed_EmptyString_ReturnsEmpty()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("");
        Assert.Equal("", baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void StripProceduralSeed_NullString_ReturnsNull()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip(null!);
        Assert.Null(baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void StripProceduralSeed_ZeroPaddedSeed_SplitsCorrectly()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^UP_SCAN4#03495");
        Assert.Equal("^UP_SCAN4", baseId);
        Assert.Equal("03495", seed);
    }

    [Fact]
    public void StripProceduralSeed_DoubleSeed_StripsLastSeedOnly()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^UP_LAUNX#91934#1917224334");
        Assert.Equal("^UP_LAUNX#91934", baseId);
        Assert.Equal("1917224334", seed);
    }

    [Fact]
    public void StripProceduralSeed_ShortHash_NotStripped()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^ITEM#1234");
        Assert.Equal("^ITEM#1234", baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void StripProceduralSeed_CaretOnlyString_ReturnsCaretOnly()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^");
        Assert.Equal("^", baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void StripProceduralSeed_LongSeed_StillStrips()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^UP_JETX#1917224334");
        Assert.Equal("^UP_JETX", baseId);
        Assert.Equal("1917224334", seed);
    }

    [Fact]
    public void GenerateProceduralSeed_ZeroPaddingWorks()
    {
        string zeroPadded = 0.ToString("D5", CultureInfo.InvariantCulture);
        Assert.Equal("00000", zeroPadded);

        string ninePadded = 123.ToString("D5", CultureInfo.InvariantCulture);
        Assert.Equal("00123", ninePadded);
    }

    [Fact]
    public void StripProceduralSeed_TechPackHash_NotStripped()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^808497C54986");
        Assert.Equal("^808497C54986", baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void StripProceduralSeed_TechPackHashWithSeed_StripsCorrectly()
    {
        var (baseId, seed) = ProceduralSeedHelper.Strip("^808497C54986#12345");
        Assert.Equal("^808497C54986", baseId);
        Assert.Equal("12345", seed);
    }

    [Fact]
    public void StripAndReassemble_ProceduralId_RoundTrips()
    {
        string original = "^UP_ENGY3#66802";
        var (baseId, seed) = ProceduralSeedHelper.Strip(original);
        string reassembled = $"{baseId}#{seed}";
        Assert.Equal(original, reassembled);
    }

    [Fact]
    public void StripAndReassemble_NonProceduralId_PreservesBaseId()
    {
        string original = "^HYPERDRIVE";
        var (baseId, seed) = ProceduralSeedHelper.Strip(original);
        Assert.Equal(original, baseId);
        Assert.Equal("", seed);
    }

    [Fact]
    public void GenerateProceduralSeed_ProducesVariedValues()
    {
        var seeds = new HashSet<string>();
        for (int i = 0; i < 50; i++)
            seeds.Add(ProceduralSeedHelper.Generate());
        Assert.True(seeds.Count >= 40, $"Only {seeds.Count} unique seeds in 50 draws");
    }

    [Fact]
    public void IsValidSeed_Valid5DigitSeed_ReturnsTrue()
    {
        Assert.True(ProceduralSeedHelper.IsValidSeed("12345"));
        Assert.True(ProceduralSeedHelper.IsValidSeed("00000"));
        Assert.True(ProceduralSeedHelper.IsValidSeed("99999"));
    }

    [Fact]
    public void IsValidSeed_InvalidInputs_ReturnsFalse()
    {
        Assert.False(ProceduralSeedHelper.IsValidSeed(null));
        Assert.False(ProceduralSeedHelper.IsValidSeed(""));
        Assert.False(ProceduralSeedHelper.IsValidSeed("1234"));  // too short
        Assert.False(ProceduralSeedHelper.IsValidSeed("123456")); // too long
        Assert.False(ProceduralSeedHelper.IsValidSeed("abcde")); // non-numeric
        Assert.False(ProceduralSeedHelper.IsValidSeed("1234a")); // mixed
    }

    [Fact]
    public void Strip_Then_Generate_ProducesCleanId()
    {
        // Simulates the fix: user has "^UP_LAUNX#91934", we strip, then re-add new seed
        string inputId = "^UP_LAUNX#91934";
        var (baseId, _) = ProceduralSeedHelper.Strip(inputId);
        string newSeed = ProceduralSeedHelper.Generate();
        string result = $"{baseId}#{newSeed}";

        // Verify: exactly one # separator, seed is 5 digits
        Assert.Single(result.Split('#').Skip(1));
        Assert.Matches(@"^.+#\d{5}$", result);
        Assert.StartsWith("^UP_LAUNX#", result);
    }

    [Fact]
    public void Strip_PreventsDuplicateSeed_Scenario()
    {
        // Regression test: the old code would produce "^UP_LAUNX#91934#1917224334"
        // by reading the ID with seed from the text field and appending the slot's existing seed.
        // The fix strips the seed first, then only adds from the dedicated seed field.
        string textFieldId = "^UP_LAUNX#91934";
        var (baseId, _) = ProceduralSeedHelper.Strip(textFieldId);
        Assert.Equal("^UP_LAUNX", baseId);
        // Now combining with a new seed produces a clean result
        string result = $"{baseId}#91934";
        Assert.Equal("^UP_LAUNX#91934", result);
        Assert.DoesNotContain("#91934#", result);
    }
}
