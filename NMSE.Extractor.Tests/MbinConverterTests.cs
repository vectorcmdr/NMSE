using NMSE.Extractor.Data;

namespace NMSE.Extractor.Tests;

public class MbinConverterTests
{
    /// <summary>
    /// Simulates hgpaktool extracting files into banks/EXTRACTED/ subdirectory.
    /// ConsolidateMbins should find them there and move to the mbin/ folder.
    /// banks/EXTRACTED/ is NOT deleted by ConsolidateMbins (cleanup handled later).
    /// </summary>
    [Fact]
    public void ConsolidateMbins_FindsFilesInBanksExtracted()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string resourcesDir = Path.Combine(tempRoot, "Resources");
        string banksDir = Path.Combine(tempRoot, "banks");
        string extractedDir = Path.Combine(banksDir, "EXTRACTED");

        try
        {
            // Simulate hgpaktool output: banks/EXTRACTED/METADATA/REALITY/TABLES/
            string tablesDir = Path.Combine(extractedDir, "METADATA", "REALITY", "TABLES");
            string langDir = Path.Combine(extractedDir, "LANGUAGE");
            Directory.CreateDirectory(tablesDir);
            Directory.CreateDirectory(langDir);
            Directory.CreateDirectory(resourcesDir);

            File.WriteAllBytes(Path.Combine(tablesDir, "nms_reality_gcproducttable.MBIN"), [0x01]);
            File.WriteAllBytes(Path.Combine(tablesDir, "consumableitemtable.MBIN"), [0x02]);
            File.WriteAllBytes(Path.Combine(langDir, "nms_loc1_english.MBIN"), [0x03]);

            // Act
            MbinConverter.ConsolidateMbins(resourcesDir, banksDir);

            // Assert: files should be in resourcesDir/mbin/
            string mbinDir = Path.Combine(resourcesDir, "mbin");
            Assert.True(Directory.Exists(mbinDir));
            Assert.True(File.Exists(Path.Combine(mbinDir, "nms_reality_gcproducttable.MBIN")));
            Assert.True(File.Exists(Path.Combine(mbinDir, "consumableitemtable.MBIN")));
            Assert.True(File.Exists(Path.Combine(mbinDir, "nms_loc1_english.MBIN")));

            // Assert: source files should be gone (File.Move, not Copy)
            Assert.False(File.Exists(Path.Combine(tablesDir, "nms_reality_gcproducttable.MBIN")));
            Assert.False(File.Exists(Path.Combine(tablesDir, "consumableitemtable.MBIN")));
            Assert.False(File.Exists(Path.Combine(langDir, "nms_loc1_english.MBIN")));

            // Assert: EXTRACTED/ dir still exists (cleanup handled by PakExtractor.CleanupBanksDir)
            Assert.True(Directory.Exists(extractedDir));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests the fallback case where files end up in the resourcesDir.
    /// </summary>
    [Fact]
    public void ConsolidateMbins_FindsFilesInResourcesDir()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string resourcesDir = Path.Combine(tempRoot, "Resources");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            // Simulate files in resourcesDir/METADATA/ (fallback scenario)
            string tablesDir = Path.Combine(resourcesDir, "METADATA", "REALITY", "TABLES");
            Directory.CreateDirectory(tablesDir);
            Directory.CreateDirectory(banksDir);

            File.WriteAllBytes(Path.Combine(tablesDir, "basebuildingobjectstable.MBIN"), [0x04]);

            // Act
            MbinConverter.ConsolidateMbins(resourcesDir, banksDir);

            // Assert: file should be in resourcesDir/mbin/
            string mbinDir = Path.Combine(resourcesDir, "mbin");
            Assert.True(File.Exists(Path.Combine(mbinDir, "basebuildingobjectstable.MBIN")));

            // Assert: staging dir should be cleaned up from resourcesDir
            Assert.False(Directory.Exists(Path.Combine(resourcesDir, "METADATA")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests that files directly in banks dir (not EXTRACTED subdir) are also found.
    /// </summary>
    [Fact]
    public void ConsolidateMbins_FindsFilesDirectlyInBanksDir()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string resourcesDir = Path.Combine(tempRoot, "Resources");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            // Simulate files directly in banks/METADATA/
            string tablesDir = Path.Combine(banksDir, "METADATA", "REALITY", "TABLES");
            Directory.CreateDirectory(tablesDir);
            Directory.CreateDirectory(resourcesDir);

            File.WriteAllBytes(Path.Combine(tablesDir, "fishdatatable.MBIN"), [0x05]);

            // Act
            MbinConverter.ConsolidateMbins(resourcesDir, banksDir);

            // Assert
            string mbinDir = Path.Combine(resourcesDir, "mbin");
            Assert.True(File.Exists(Path.Combine(mbinDir, "fishdatatable.MBIN")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Ensures duplicate filenames across search locations don't result in duplicates.
    /// First occurrence wins and is moved; second is skipped.
    /// </summary>
    [Fact]
    public void ConsolidateMbins_DeduplicatesAcrossLocations()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string resourcesDir = Path.Combine(tempRoot, "Resources");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            // Same file in both banks/EXTRACTED/ and resourcesDir
            string extractedDir = Path.Combine(banksDir, "EXTRACTED", "LANGUAGE");
            string resourceLangDir = Path.Combine(resourcesDir, "LANGUAGE");
            Directory.CreateDirectory(extractedDir);
            Directory.CreateDirectory(resourceLangDir);

            File.WriteAllBytes(Path.Combine(extractedDir, "nms_loc1_english.MBIN"), [0xAA]);
            File.WriteAllBytes(Path.Combine(resourceLangDir, "nms_loc1_english.MBIN"), [0xBB]);

            // Act
            MbinConverter.ConsolidateMbins(resourcesDir, banksDir);

            // Assert: only one copy in mbin/
            string mbinDir = Path.Combine(resourcesDir, "mbin");
            string[] mbinFiles = Directory.GetFiles(mbinDir, "nms_loc1_english.MBIN");
            Assert.Single(mbinFiles);

            // Assert: first occurrence (from EXTRACTED/) was moved (content = 0xAA)
            byte[] content = File.ReadAllBytes(mbinFiles[0]);
            Assert.Equal([0xAA], content);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Ensures existing mbin/ directory files are not duplicated.
    /// </summary>
    [Fact]
    public void ConsolidateMbins_SkipsFilesAlreadyInMbinDir()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string resourcesDir = Path.Combine(tempRoot, "Resources");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            string mbinDir = Path.Combine(resourcesDir, "mbin");
            Directory.CreateDirectory(mbinDir);
            Directory.CreateDirectory(banksDir);

            // Pre-existing file in mbin/
            File.WriteAllBytes(Path.Combine(mbinDir, "existing.MBIN"), [0xFF]);

            // Act
            MbinConverter.ConsolidateMbins(resourcesDir, banksDir);

            // Assert: the existing file should still be there
            Assert.True(File.Exists(Path.Combine(mbinDir, "existing.MBIN")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests CleanupPakFiles removes .pak files from a directory.
    /// </summary>
    [Fact]
    public void CleanupPakFiles_WorksCorrectly()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            Directory.CreateDirectory(banksDir);

            // Simulate .pak files in banks dir
            File.WriteAllBytes(Path.Combine(banksDir, "test1.pak"), [0x01, 0x02]);
            File.WriteAllBytes(Path.Combine(banksDir, "test2.pak"), [0x03, 0x04]);
            // Non-.pak file should not be deleted
            File.WriteAllBytes(Path.Combine(banksDir, "readme.txt"), [0x05]);

            // Act: cleanup
            PakExtractor.CleanupPakFiles(banksDir);

            // Assert: .pak files removed
            Assert.False(File.Exists(Path.Combine(banksDir, "test1.pak")));
            Assert.False(File.Exists(Path.Combine(banksDir, "test2.pak")));
            // Non-pak file should remain
            Assert.True(File.Exists(Path.Combine(banksDir, "readme.txt")));
            // Directory still exists
            Assert.True(Directory.Exists(banksDir));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests that CleanupPakFiles handles non-existent directory gracefully.
    /// </summary>
    [Fact]
    public void CleanupPakFiles_NonExistentDir_DoesNotThrow()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}", "nope");
        PakExtractor.CleanupPakFiles(nonExistent);
        // Should not throw
    }

    /// <summary>
    /// Tests GetPakFilesSize correctly sums .pak file sizes.
    /// </summary>
    [Fact]
    public void GetPakFilesSize_ReturnsTotalPakSize()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string pcbanksDir = Path.Combine(tempRoot, "PCBANKS");

        try
        {
            Directory.CreateDirectory(pcbanksDir);

            // Create test .pak files with known sizes
            File.WriteAllBytes(Path.Combine(pcbanksDir, "a.pak"), new byte[1000]);
            File.WriteAllBytes(Path.Combine(pcbanksDir, "b.pak"), new byte[2000]);
            // Non-.pak files should not be counted
            File.WriteAllBytes(Path.Combine(pcbanksDir, "readme.txt"), new byte[500]);

            long size = PakExtractor.GetPakFilesSize(pcbanksDir);
            Assert.Equal(3000, size);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests GetPakFilesSize returns 0 for non-existent directory.
    /// </summary>
    [Fact]
    public void GetPakFilesSize_NonExistentDir_ReturnsZero()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}", "nope");
        Assert.Equal(0, PakExtractor.GetPakFilesSize(nonExistent));
    }

    /// <summary>
    /// Tests EstimateMaxStorageBytes: largest pak + 0.5x total pak size.
    /// </summary>
    [Fact]
    public void EstimateMaxStorageBytes_ReturnsLargestPakPlusHalfTotal()
    {
        long pakSize = 10_000_000_000; // 10 GB total
        long largestPak = 2_000_000_000; // 2 GB largest
        long estimate = PakExtractor.EstimateMaxStorageBytes(pakSize, largestPak);
        Assert.Equal(largestPak + (pakSize / 2), estimate);
    }

    /// <summary>
    /// Tests GetLargestPakFileSize returns the size of the largest .pak file.
    /// </summary>
    [Fact]
    public void GetLargestPakFileSize_ReturnsLargest()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string pcbanksDir = Path.Combine(tempRoot, "PCBANKS");

        try
        {
            Directory.CreateDirectory(pcbanksDir);

            File.WriteAllBytes(Path.Combine(pcbanksDir, "small.pak"), new byte[1000]);
            File.WriteAllBytes(Path.Combine(pcbanksDir, "large.pak"), new byte[5000]);
            File.WriteAllBytes(Path.Combine(pcbanksDir, "medium.pak"), new byte[3000]);
            // Non-.pak files should not be considered
            File.WriteAllBytes(Path.Combine(pcbanksDir, "readme.txt"), new byte[9999]);

            long largest = PakExtractor.GetLargestPakFileSize(pcbanksDir);
            Assert.Equal(5000, largest);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests GetLargestPakFileSize returns 0 for non-existent directory.
    /// </summary>
    [Fact]
    public void GetLargestPakFileSize_NonExistentDir_ReturnsZero()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}", "nope");
        Assert.Equal(0, PakExtractor.GetLargestPakFileSize(nonExistent));
    }

    /// <summary>
    /// Tests ExtractPerPak validates hgpaktool exists.
    /// </summary>
    [Fact]
    public void ExtractPerPak_MissingTool_ThrowsFileNotFound()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string pcbanksDir = Path.Combine(tempRoot, "PCBANKS");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            Directory.CreateDirectory(pcbanksDir);

            Assert.Throws<FileNotFoundException>(() =>
                PakExtractor.ExtractPerPak(
                    Path.Combine(tempRoot, "nonexistent.exe"),
                    pcbanksDir,
                    banksDir,
                    _ => ["*.mbin"]));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests ExtractPerPak validates PCBANKS directory exists.
    /// </summary>
    [Fact]
    public void ExtractPerPak_MissingPcbanks_ThrowsDirectoryNotFound()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string banksDir = Path.Combine(tempRoot, "banks");
        // Create a fake exe file
        Directory.CreateDirectory(tempRoot);
        string fakeExe = Path.Combine(tempRoot, "fake.exe");
        File.WriteAllBytes(fakeExe, [0x00]);

        try
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
                PakExtractor.ExtractPerPak(
                    fakeExe,
                    Path.Combine(tempRoot, "nonexistent_pcbanks"),
                    banksDir,
                    _ => ["*.mbin"]));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests that CleanupBanksDir removes the entire banks directory.
    /// </summary>
    [Fact]
    public void CleanupBanksDir_RemovesEntireDirectory()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string banksDir = Path.Combine(tempRoot, "banks");

        try
        {
            // Set up banks dir with pak files, EXTRACTED/ subdirectory, etc.
            Directory.CreateDirectory(banksDir);
            string extractedDir = Path.Combine(banksDir, "EXTRACTED", "TEXTURES");
            Directory.CreateDirectory(extractedDir);

            File.WriteAllBytes(Path.Combine(banksDir, "test1.pak"), [0x01, 0x02]);
            File.WriteAllBytes(Path.Combine(extractedDir, "icon.DDS"), [0x03, 0x04]);

            // Act
            PakExtractor.CleanupBanksDir(banksDir);

            // Assert: entire directory should be gone
            Assert.False(Directory.Exists(banksDir));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests that CleanupBanksDir handles non-existent directory gracefully.
    /// </summary>
    [Fact]
    public void CleanupBanksDir_NonExistentDir_DoesNotThrow()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}", "nope");
        PakExtractor.CleanupBanksDir(nonExistent);
        // Should not throw
    }

    /// <summary>
    /// Tests that WriteProgress does not throw in any environment.
    /// </summary>
    [Fact]
    public void WriteProgress_DoesNotThrow()
    {
        // WriteProgress should handle console width detection gracefully
        PakExtractor.WriteProgress("test message");
        PakExtractor.WriteProgress("a very long message that might exceed normal console width: " + new string('x', 200));
        PakExtractor.WriteProgress("");
        PakExtractor.FinishProgress();
    }

    // --- IsPakRelevant tests ---

    /// <summary>
    /// Data paks (metadata, precache, globals, hex-named) are relevant.
    /// </summary>
    [Theory]
    [InlineData("NMSARC.MetadataEtc.pak")]
    [InlineData("NMSARC.Precache.pak")]
    [InlineData("NMSARC.globals.pak")]
    [InlineData("NMSARC.59AABAC1.pak")]
    [InlineData("NMSARC.EntitySceneMBIN.pak")]
    public void IsPakRelevant_DataPaks_ReturnsTrue(string pakName)
    {
        Assert.True(PakExtractor.IsPakRelevant(pakName));
    }

    /// <summary>
    /// Texture paks are relevant (needed for DDS icon extraction).
    /// </summary>
    [Theory]
    [InlineData("NMSARC.TexUI.pak")]
    [InlineData("NMSARC.TexMisc.pak")]
    [InlineData("NMSARC.TexBiomesCOMMON.pak")]
    public void IsPakRelevant_TexturePaks_ReturnsTrue(string pakName)
    {
        Assert.True(PakExtractor.IsPakRelevant(pakName));
    }

    /// <summary>
    /// Audio, mesh, animation, font, shader, pipeline, scene, and misc paks are irrelevant.
    /// </summary>
    [Theory]
    [InlineData("NMSARC.AnimMBIN.pak")]
    [InlineData("NMSARC.audio.pak")]
    [InlineData("NMSARC.audioBNK.pak")]
    [InlineData("NMSARC.fonts.pak")]
    [InlineData("NMSARC.MeshCommon.pak")]
    [InlineData("NMSARC.MeshMisc.pak")]
    [InlineData("NMSARC.MeshPlanetBIOMES.pak")]
    [InlineData("NMSARC.MeshPlanetCOMMON.pak")]
    [InlineData("NMSARC.MeshPlanetCREATURES.pak")]
    [InlineData("NMSARC.MeshPlanetDESERT.pak")]
    [InlineData("NMSARC.MeshPlanetENEMIES.pak")]
    [InlineData("NMSARC.MeshPlanetNPCS.pak")]
    [InlineData("NMSARC.MeshPlanetOCEAN.pak")]
    [InlineData("NMSARC.MeshPlanetSKY.pak")]
    [InlineData("NMSARC.MeshPlanetSNOW.pak")]
    [InlineData("NMSARC.misc.pak")]
    [InlineData("NMSARC.pipelines.pak")]
    [InlineData("NMSARC.Scenes.pak")]
    [InlineData("NMSARC.Shaders.pak")]
    [InlineData("NMSARC.UI.pak")]
    public void IsPakRelevant_IrrelevantPaks_ReturnsFalse(string pakName)
    {
        Assert.False(PakExtractor.IsPakRelevant(pakName));
    }

    // --- ParseUnpackedCount tests ---

    /// <summary>
    /// Parses count from standard hgpaktool stderr output.
    /// </summary>
    [Theory]
    [InlineData("Unpacked 8 files from 1 .pak's in 0.863s", 8)]
    [InlineData("Unpacked 12 files from 1 .pak's in 0.213s", 12)]
    [InlineData("Unpacked 8830 files from 1 .pak's in 27.343s", 8830)]
    [InlineData("Unpacked 0 files from 1 .pak's in 0.053s", 0)]
    [InlineData("Unpacked 1 file from 1 .pak's in 0.001s", 1)]
    public void ParseUnpackedCount_ValidStderr_ReturnsCount(string stderr, int expected)
    {
        Assert.Equal(expected, PakExtractor.ParseUnpackedCount(stderr));
    }

    /// <summary>
    /// Returns 0 for empty or null stderr.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("some other output")]
    public void ParseUnpackedCount_InvalidStderr_ReturnsZero(string? stderr)
    {
        Assert.Equal(0, PakExtractor.ParseUnpackedCount(stderr));
    }

    // --- GetPakFilesSize with filter tests ---

    /// <summary>
    /// Tests that GetPakFilesSize with filter only counts matching files.
    /// </summary>
    [Fact]
    public void GetPakFilesSize_WithFilter_OnlyCountsMatchingFiles()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string pcbanksDir = Path.Combine(tempRoot, "PCBANKS");

        try
        {
            Directory.CreateDirectory(pcbanksDir);

            // Relevant paks
            File.WriteAllBytes(Path.Combine(pcbanksDir, "NMSARC.MetadataEtc.pak"), new byte[1000]);
            File.WriteAllBytes(Path.Combine(pcbanksDir, "NMSARC.TexUI.pak"), new byte[2000]);
            // Irrelevant paks
            File.WriteAllBytes(Path.Combine(pcbanksDir, "NMSARC.audio.pak"), new byte[5000]);

            // Without filter: counts all
            long totalSize = PakExtractor.GetPakFilesSize(pcbanksDir);
            Assert.Equal(8000, totalSize);

            // IsPakRelevant excludes audio.pak (irrelevant), so only MetadataEtc + TexUI
            long filteredSize = PakExtractor.GetPakFilesSize(pcbanksDir, PakExtractor.IsPakRelevant);
            Assert.Equal(3000, filteredSize);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>
    /// Tests that GetLargestPakFileSize with filter only considers matching files.
    /// </summary>
    [Fact]
    public void GetLargestPakFileSize_WithFilter_OnlyConsidersMatchingFiles()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid()}");
        string pcbanksDir = Path.Combine(tempRoot, "PCBANKS");

        try
        {
            Directory.CreateDirectory(pcbanksDir);

            // Relevant paks
            File.WriteAllBytes(Path.Combine(pcbanksDir, "NMSARC.MetadataEtc.pak"), new byte[1000]);
            File.WriteAllBytes(Path.Combine(pcbanksDir, "NMSARC.TexUI.pak"), new byte[2000]);
            // Irrelevant pak (largest overall)
            File.WriteAllBytes(Path.Combine(pcbanksDir, "NMSARC.audio.pak"), new byte[9000]);

            // Without filter: audio.pak is largest
            long largest = PakExtractor.GetLargestPakFileSize(pcbanksDir);
            Assert.Equal(9000, largest);

            // IsPakRelevant excludes audio.pak, so TexUI.pak is largest relevant
            long filteredLargest = PakExtractor.GetLargestPakFileSize(pcbanksDir, PakExtractor.IsPakRelevant);
            Assert.Equal(2000, filteredLargest);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
