using NMSE.Core.Utilities;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Unit tests for <see cref="RawNumberGuard"/> JSON preservation behavior.
/// </summary>
public class RawNumberGuardTests
{
    /// <summary>
    /// Verifies that <see cref="JsonObject.GetInt(string)"/> rounds a <see cref="RawDouble"/>
    /// value to the expected integer.
    /// </summary>
    [Fact]
    public void JsonObject_GetInt_RawDouble_UsesRoundedConversion()
    {
        var obj = new JsonObject();
        obj.Add("Value", new RawDouble(4.999999999999999, "4.999999999999999"));

        Assert.Equal(5, obj.GetInt("Value"));
    }

    /// <summary>
    /// Verifies that <see cref="JsonArray.GetLong(int)"/> rounds a <see cref="RawDouble"/>
    /// value to the expected long integer.
    /// </summary>
    [Fact]
    public void JsonArray_GetLong_RawDouble_UsesRoundedConversion()
    {
        var arr = new JsonArray();
        arr.Add(new RawDouble(8.999999999999998, "8.999999999999998"));

        Assert.Equal(9L, arr.GetLong(0));
    }

    /// <summary>
    /// Verifies that <see cref="RawNumberGuard.SetInt(JsonObject?, string, int)"/>
    /// preserves a <see cref="RawDouble"/> source value when the rounded integer matches.
    /// </summary>
    [Fact]
    public void RawNumberGuard_SetInt_PreservesRawDoubleWhenRoundedValueMatches()
    {
        var obj = JsonObject.Parse("""{"Value":4.999999999999999}""");

        RawNumberGuard.SetInt(obj, "Value", 5);

        var value = obj.Get("Value");
        Assert.IsType<RawDouble>(value);
        Assert.Equal("4.999999999999999", ((RawDouble)value).Text);
    }

    /// <summary>
    /// Verifies that <see cref="RawNumberGuard.SetLong(JsonObject?, string, long)"/>
    /// preserves a <see cref="RawDouble"/> source value when the rounded long matches.
    /// </summary>
    [Fact]
    public void RawNumberGuard_SetLong_PreservesRawDoubleWhenRoundedValueMatches()
    {
        var obj = JsonObject.Parse("""{"Value":8.999999999999998}""");

        RawNumberGuard.SetLong(obj, "Value", 9);

        var value = obj.Get("Value");
        Assert.IsType<RawDouble>(value);
        Assert.Equal("8.999999999999998", ((RawDouble)value).Text);
    }

    /// <summary>
    /// Verifies that <see cref="RawNumberGuard.SetInt(JsonArray?, int, int)"/>
    /// preserves a <see cref="RawDouble"/> source value in an array when the rounded integer matches.
    /// </summary>
    [Fact]
    public void RawNumberGuard_SetIntOnArray_PreservesRawDoubleWhenRoundedValueMatches()
    {
        var arr = JsonArray.Parse("""[2.9999999999999996]""");

        RawNumberGuard.SetInt(arr, 0, 3);

        var value = arr.Get(0);
        Assert.IsType<RawDouble>(value);
        Assert.Equal("2.9999999999999996", ((RawDouble)value).Text);
    }
}
