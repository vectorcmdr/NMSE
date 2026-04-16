using System.Globalization;
using NMSE.Extractor.Config;

namespace NMSE.Extractor.Tests;

public class ExtractorConfigTests
{
    [Fact]
    public void MbinFilters_ContainsExpectedFilters()
    {
        Assert.Contains(ExtractorConfig.MbinFilters,
            f => f.Contains("nms_reality_gcproducttable.mbin"));
        // Locale MBIN filters use wildcards to capture all languages (e.g. nms_loc1_*.mbin)
        Assert.Contains(ExtractorConfig.MbinFilters,
            f => f.Contains("nms_loc1_"));
        Assert.Contains(ExtractorConfig.MbinFilters,
            f => f.Contains("nms_dialog_gcalienspeechtable.mbin"));
    }

    [Fact]
    public void MbinFilters_ContainsGcGameTableGlobalsFilter()
    {
        // GCGAMETABLEGLOBALS.mbin uses only the * form (bare form caused hgpaktool errors)
        Assert.Contains("*GCGAMETABLEGLOBALS.mbin", ExtractorConfig.MbinFilters);
        Assert.DoesNotContain("GCGAMETABLEGLOBALS.mbin", ExtractorConfig.MbinFilters);
    }

    [Fact]
    public void MbinFilters_ContainsPetBattleFilters()
    {
        Assert.Contains(ExtractorConfig.MbinFilters,
            f => f.Contains("PETBATTLERMOVESTABLE.mbin"));
        Assert.Contains(ExtractorConfig.MbinFilters,
            f => f.Contains("PETBATTLERMOVESETSTABLE.mbin"));
        Assert.Contains(ExtractorConfig.MbinFilters,
            f => f.Contains("CHARACTERCUSTOMISATIONDESCRIPTORGROUPSDATA.mbin"));
    }

    [Fact]
    public void ExpectedMxmlFiles_ContainAllRequiredFiles()
    {
        // All game data MXMLs must be in ExpectedMxmlFiles (no optional fallthrough)
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("nms_loc1_english.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("nms_update3_english.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("GCGAMETABLEGLOBALS.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("PETBATTLERMOVESTABLE.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("PETBATTLERMOVESETSTABLE.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("CHARACTERCUSTOMISATIONDESCRIPTORGROUPSDATA.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("UNLOCKABLESEASONREWARDS.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("FRIGATETRAITTABLE.MXML"));
        Assert.Contains(ExtractorConfig.ExpectedMxmlFiles,
            f => f.Contains("WIKI.MXML"));
    }

    [Fact]
    public void OptionalMxmlFiles_OnlyContainNonEnglishLocaleFiles()
    {
        // Only non-English locale MXML files should be optional
        Assert.Contains(ExtractorConfig.OptionalMxmlFiles,
            f => f.Contains("japanese"));
        Assert.Contains(ExtractorConfig.OptionalMxmlFiles,
            f => f.Contains("french"));
        // No game data MXMLs should be optional
        Assert.DoesNotContain(ExtractorConfig.OptionalMxmlFiles,
            f => f.Contains("GCGAMETABLEGLOBALS"));
        Assert.DoesNotContain(ExtractorConfig.OptionalMxmlFiles,
            f => f.Contains("WIKI"));
    }

    [Fact]
    public void LocaleMxmlFiles_ContainsAllLocaleFiles()
    {
        Assert.Equal(8, ExtractorConfig.LocaleMxmlFiles.Length);
        Assert.All(ExtractorConfig.LocaleMxmlFiles,
            f => Assert.EndsWith(".MXML", f));
    }

    [Fact]
    public void ExpectedMxmlFiles_DoNotOverlapWithOptional()
    {
        var overlap = ExtractorConfig.ExpectedMxmlFiles
            .Intersect(ExtractorConfig.OptionalMxmlFiles)
            .ToList();
        Assert.Empty(overlap);
    }

    [Fact]
    public void ToolUrls_AreValid()
    {
        Assert.StartsWith("https://", ExtractorConfig.HgPakToolZipUrl);
        Assert.StartsWith("https://", ExtractorConfig.MbinCompilerUrl);
        Assert.StartsWith("https://", ExtractorConfig.MappingJsonUrl);
        Assert.StartsWith("https://", ExtractorConfig.ImageMagickLatestUrl);
        Assert.StartsWith("https://", ExtractorConfig.SevenZipLatestUrl);
    }

    [Fact]
    public void ImageMagickDownloadPattern_ContainsPlaceholder()
    {
        Assert.Contains("{0}", ExtractorConfig.ImageMagickDownloadPattern);
        string url = string.Format(CultureInfo.InvariantCulture, ExtractorConfig.ImageMagickDownloadPattern, "7.1.2-15");
        Assert.Contains("7.1.2-15", url);
        Assert.EndsWith(".7z", url);
    }

    [Fact]
    public void SevenZipDownloadPattern_ContainsPlaceholder()
    {
        Assert.Contains("{0}", ExtractorConfig.SevenZipDownloadPattern);
        string url = string.Format(CultureInfo.InvariantCulture, ExtractorConfig.SevenZipDownloadPattern, "24.09");
        Assert.Contains("24.09", url);
        Assert.EndsWith("7zr.exe", url);
    }

    [Fact]
    public void TextureFilters_ContainsDdsPattern()
    {
        Assert.NotEmpty(ExtractorConfig.TextureFilters);
        Assert.Contains(ExtractorConfig.TextureFilters, f => f.Contains("*.DDS"));
    }

    [Fact]
    public void AllExtractionFilters_CombinesMbinAndTextureFilters()
    {
        Assert.Equal(
            ExtractorConfig.MbinFilters.Length + ExtractorConfig.TextureFilters.Length,
            ExtractorConfig.AllExtractionFilters.Length);

        // Should contain MBIN filters
        Assert.Contains(ExtractorConfig.AllExtractionFilters,
            f => f.Contains("nms_reality_gcproducttable.mbin"));

        // Should contain texture filters
        Assert.Contains(ExtractorConfig.AllExtractionFilters,
            f => f.Contains("*.DDS"));
    }

    [Fact]
    public void GetFiltersForPak_Globals_ReturnsMbinFilters()
    {
        // All non-Tex paks get all MBIN filters since NMS distributes data across many paks
        string[] filters = ExtractorConfig.GetFiltersForPak("NMSARC.globals.pak");
        Assert.Equal(ExtractorConfig.MbinFilters, filters);
        Assert.Contains(filters, f => f.Contains("GCGAMETABLEGLOBALS"));
        Assert.Contains(filters, f => f.Contains("nms_reality_gcproducttable.mbin"));
    }

    [Fact]
    public void GetFiltersForPak_MetadataEtc_ReturnsMbinFilters()
    {
        // MetadataEtc gets all MBIN filters (same as any non-Tex pak)
        string[] filters = ExtractorConfig.GetFiltersForPak("NMSARC.MetadataEtc.pak");
        Assert.Equal(ExtractorConfig.MbinFilters, filters);
        Assert.Contains(filters, f => f.Contains("nms_reality_gcproducttable.mbin"));
        Assert.Contains(filters, f => f.Contains("LANGUAGE/nms_loc1_"));
        Assert.DoesNotContain(filters, f => f.Contains("*.DDS"));
    }

    [Fact]
    public void GetFiltersForPak_Precache_ReturnsMbinFilters()
    {
        string[] filters = ExtractorConfig.GetFiltersForPak("NMSARC.Precache.pak");
        Assert.Equal(ExtractorConfig.MbinFilters, filters);
    }

    [Fact]
    public void GetFiltersForPak_HexNamedPak_ReturnsMbinFilters()
    {
        // Hex-named paks may contain REALITY/TABLES or SIMULATION data
        string[] filters = ExtractorConfig.GetFiltersForPak("NMSARC.59AABAC1.pak");
        Assert.Equal(ExtractorConfig.MbinFilters, filters);
        Assert.Contains(filters, f => f.Contains("nms_reality_gcproducttable.mbin"));
    }

    [Fact]
    public void GetFiltersForPak_TexPak_ReturnsTextureOnly()
    {
        string[] filters = ExtractorConfig.GetFiltersForPak("NMSARC.TexUI.pak");
        Assert.Equal(ExtractorConfig.TextureFilters, filters);
        Assert.Contains(filters, f => f.Contains("*.DDS"));
    }

    [Fact]
    public void GetFiltersForPak_NoBareGlobalsFilter()
    {
        // Bare GCGAMETABLEGLOBALS.mbin (no * prefix) was causing hgpaktool to error out
        // for paks that don't have a root-level match. Only the * form should exist.
        Assert.DoesNotContain(ExtractorConfig.GlobalsMbinFilters, f => f == "GCGAMETABLEGLOBALS.mbin");
        Assert.Contains(ExtractorConfig.GlobalsMbinFilters, f => f == "*GCGAMETABLEGLOBALS.mbin");
    }

    [Fact]
    public void SupportedLanguages_Contains16Languages()
    {
        Assert.Equal(16, ExtractorConfig.SupportedLanguages.Count);
        Assert.Equal("en-GB", ExtractorConfig.SupportedLanguages["English"]);
        Assert.Equal("ja-JP", ExtractorConfig.SupportedLanguages["Japanese"]);
        Assert.Equal("es-419", ExtractorConfig.SupportedLanguages["LatinAmericanSpanish"]);
        // TencentChinese should NOT be in supported languages
        Assert.False(ExtractorConfig.SupportedLanguages.ContainsKey("TencentChinese"));
    }

    [Fact]
    public void GetLocaleMxmlFiles_ReturnsCorrectFileNamesForLanguage()
    {
        string[] jpFiles = ExtractorConfig.GetLocaleMxmlFiles("Japanese");
        Assert.Equal(8, jpFiles.Length);
        Assert.Contains("nms_loc1_japanese.MXML", jpFiles);
        Assert.Contains("nms_update3_japanese.MXML", jpFiles);

        string[] enFiles = ExtractorConfig.GetLocaleMxmlFiles("English");
        Assert.Equal(8, enFiles.Length);
        Assert.Contains("nms_loc1_english.MXML", enFiles);
    }

    [Fact]
    public void LocaleFileStems_Has8Entries()
    {
        Assert.Equal(8, ExtractorConfig.LocaleFileStems.Length);
        Assert.Contains("nms_loc1", ExtractorConfig.LocaleFileStems);
        Assert.Contains("nms_update3", ExtractorConfig.LocaleFileStems);
    }
}
