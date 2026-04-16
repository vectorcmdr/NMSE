using System.Drawing;
using NMSE.Core.Utilities;
using Xunit;

namespace NMSE.Tests;

/// <summary>
/// Tests for the NmsColourPalette utility class covering palette contents,
/// closest-colour matching, and normalised RGBA conversion.
/// </summary>
public class NmsColourPaletteTests
{
    [Fact]
    public void PaintPalette_Contains20Colours()
    {
        Assert.Equal(20, NmsColourPalette.PaintPalette.Length);
    }

    [Fact]
    public void PaintPalette_AllColoursHaveNames()
    {
        foreach (var entry in NmsColourPalette.PaintPalette)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Name),
                $"Palette entry at index {Array.IndexOf(NmsColourPalette.PaintPalette, entry)} has no name.");
        }
    }

    [Fact]
    public void PaintPalette_NamesAreUnique()
    {
        var names = NmsColourPalette.PaintPalette.Select(e => e.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void PaintPalette_ContainsWhiteAndBlack()
    {
        Assert.Contains(NmsColourPalette.PaintPalette, e => e.Colour == Color.FromArgb(255, 255, 255));
        Assert.Contains(NmsColourPalette.PaintPalette, e => e.Colour == Color.FromArgb(0, 0, 0));
    }

    [Theory]
    [InlineData(255, 255, 255, 8)]   // White is ninth (index 8)
    [InlineData(0, 0, 0, 19)]        // Black is last (index 19)
    [InlineData(255, 133, 0, 1)]     // Orange is second (index 1)
    public void FindClosestPaletteIndex_ExactMatch_ReturnsCorrectIndex(int r, int g, int b, int expectedIndex)
    {
        var colour = Color.FromArgb(r, g, b);
        Assert.Equal(expectedIndex, NmsColourPalette.FindClosestPaletteIndex(colour));
    }

    [Fact]
    public void FindClosestPaletteIndex_NearWhite_ReturnsWhite()
    {
        // Almost white should match White (index 8)
        var nearWhite = Color.FromArgb(250, 252, 253);
        Assert.Equal(8, NmsColourPalette.FindClosestPaletteIndex(nearWhite));
    }

    [Fact]
    public void FindClosestPaletteIndex_NearBlack_ReturnsBlack()
    {
        // Almost black should match Black (index 19)
        var nearBlack = Color.FromArgb(5, 3, 2);
        Assert.Equal(19, NmsColourPalette.FindClosestPaletteIndex(nearBlack));
    }

    [Theory]
    [InlineData(255, 255, 255)]
    [InlineData(0, 0, 0)]
    [InlineData(128, 57, 57)]
    public void ToNormalisedRgba_ReturnsCorrectValues(int r, int g, int b)
    {
        var colour = Color.FromArgb(r, g, b);
        var rgba = NmsColourPalette.ToNormalisedRgba(colour);

        Assert.Equal(4, rgba.Length);
        Assert.Equal(Math.Round(r / 255.0, 4), rgba[0]);
        Assert.Equal(Math.Round(g / 255.0, 4), rgba[1]);
        Assert.Equal(Math.Round(b / 255.0, 4), rgba[2]);
        Assert.Equal(1.0, rgba[3]); // Alpha always 1.0
    }

    [Fact]
    public void ToNormalisedRgba_White_AllOnes()
    {
        var rgba = NmsColourPalette.ToNormalisedRgba(Color.White);
        Assert.Equal(1.0, rgba[0]);
        Assert.Equal(1.0, rgba[1]);
        Assert.Equal(1.0, rgba[2]);
        Assert.Equal(1.0, rgba[3]);
    }

    [Fact]
    public void ToNormalisedRgba_Black_AllZerosExceptAlpha()
    {
        var rgba = NmsColourPalette.ToNormalisedRgba(Color.Black);
        Assert.Equal(0.0, rgba[0]);
        Assert.Equal(0.0, rgba[1]);
        Assert.Equal(0.0, rgba[2]);
        Assert.Equal(1.0, rgba[3]);
    }

    [Fact]
    public void PaintPalette_AllColoursHaveFullAlpha()
    {
        // Verify all palette colours have A=255 (fully opaque)
        foreach (var entry in NmsColourPalette.PaintPalette)
        {
            Assert.Equal(255, entry.Colour.A);
        }
    }
}
