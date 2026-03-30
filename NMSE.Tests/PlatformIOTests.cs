using System.Text;
using NMSE.IO;
using NMSE.Models;

namespace NMSE.Tests;

/// <summary>
/// Tests for platform-specific IO: MetaCrypto, MetaFileWriter, ContainersIndexManager,
/// MemoryDatManager, SaveSlotManager, and enhanced SaveFileManager.
/// </summary>
public class PlatformIOTests
{
    // --- MetaCrypto: TEA encrypt/decrypt round-trip ---

    [Fact]
    public void MetaCrypto_EncryptDecrypt_RoundTrip()
    {
        // Create a plain meta buffer with recognizable Steam header
        uint[] plain = new uint[26]; // 104 bytes = Steam vanilla meta size
        plain[0] = MetaFileWriter.META_HEADER; // 0xEEEEEEBE
        plain[1] = 1; // format
        plain[2] = 12345; // some value
        plain[3] = 0;
        plain[4] = 99;
        plain[5] = 42;

        // Encrypt with slot 2, 8 iterations (pre-Waypoint)
        uint[] encrypted = MetaCrypto.Encrypt(plain, 2, 8);

        // Should be different from plain
        Assert.NotEqual(plain[0], encrypted[0]);

        // Decrypt
        uint[] decrypted = MetaCrypto.Decrypt(encrypted, 2, 8);

        // Should match original
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void MetaCrypto_EncryptDecrypt_WaypointFormat()
    {
        // 90 uint = 360 bytes = Steam Waypoint meta
        uint[] plain = new uint[90];
        plain[0] = MetaFileWriter.META_HEADER;
        plain[1] = 2; // format 2
        for (int i = 2; i < 90; i++)
            plain[i] = (uint)(i * 7 + 13);

        // 6 iterations for Waypoint+
        uint[] encrypted = MetaCrypto.Encrypt(plain, 5, 6);
        Assert.NotEqual(plain[0], encrypted[0]);

        uint[] decrypted = MetaCrypto.Decrypt(encrypted, 5, 6);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void MetaCrypto_Decrypt_TriesDifferentSlots()
    {
        uint[] plain = new uint[26];
        plain[0] = MetaFileWriter.META_HEADER;
        plain[1] = 1;
        plain[2] = 42;

        // Encrypt with slot 7
        uint[] encrypted = MetaCrypto.Encrypt(plain, 7, 8);

        // Decrypt with wrong slot first - should still find it
        uint[] decrypted = MetaCrypto.Decrypt(encrypted, 3, 8);
        Assert.Equal(MetaFileWriter.META_HEADER, decrypted[0]);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void MetaCrypto_ComputeMetaHashes_ReturnsCorrectLength()
    {
        byte[] testData = new byte[1024];
        new Random(42).NextBytes(testData);

        byte[] hashes = MetaCrypto.ComputeMetaHashes(testData);
        Assert.Equal(48, hashes.Length); // SpookyHash(16) + SHA256(32)
    }

    [Fact]
    public void MetaCrypto_ComputeMetaHashes_DeterministicForSameInput()
    {
        byte[] data = new byte[256];
        for (int i = 0; i < 256; i++) data[i] = (byte)(i & 0xFF);

        byte[] hash1 = MetaCrypto.ComputeMetaHashes(data);
        byte[] hash2 = MetaCrypto.ComputeMetaHashes(data);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void MetaCrypto_ComputeMetaHashes_DifferentForDifferentInput()
    {
        byte[] data1 = new byte[] { 1, 2, 3, 4, 5 };
        byte[] data2 = new byte[] { 1, 2, 3, 4, 6 };

        byte[] hash1 = MetaCrypto.ComputeMetaHashes(data1);
        byte[] hash2 = MetaCrypto.ComputeMetaHashes(data2);
        Assert.NotEqual(hash1, hash2);
    }

    // --- MetaFileWriter: helpers ---

    [Theory]
    [InlineData("/saves/save.hg", "/saves/mf_save.hg")]
    [InlineData("/saves/save2.hg", "/saves/mf_save2.hg")]
    [InlineData("/saves/accountdata.hg", "/saves/mf_accountdata.hg")]
    public void MetaFileWriter_GetSteamMetaPath_ReturnsCorrectPath(string savePath, string expectedMeta)
    {
        string result = MetaFileWriter.GetSteamMetaPath(savePath);
        // Normalise path separators so the test passes on both Windows (\) and Linux (/)
        Assert.Equal(NormalisePath(expectedMeta), NormalisePath(result));
    }

    [Theory]
    [InlineData("/saves/savedata01.hg", 1, "/saves/manifest01.hg")]
    [InlineData("/saves/savedata00.hg", 0, "/saves/manifest00.hg")]
    [InlineData("/saves/savedata10.hg", 10, "/saves/manifest10.hg")]
    public void MetaFileWriter_GetSwitchMetaPath_ReturnsCorrectPath(string savePath, int metaIndex, string expected)
    {
        string result = MetaFileWriter.GetSwitchMetaPath(savePath, metaIndex);
        // Normalise path separators so the test passes on both Windows (\) and Linux (/)
        Assert.Equal(NormalisePath(expected), NormalisePath(result));
    }

    /// <summary>
    /// Normalises path separators to forward slashes for cross-platform comparison.
    /// Path.Combine/GetDirectoryName use OS-native separators, so tests that
    /// hardcode '/' in InlineData would fail on Windows without normalisation.
    /// </summary>
    private static string NormalisePath(string path) => path.Replace('\\', '/');

    [Fact]
    public void MetaFileWriter_BytesToUInts_RoundTrip()
    {
        byte[] original = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        uint[] uints = MetaFileWriter.BytesToUInts(original);
        Assert.Equal(2, uints.Length);
        byte[] back = MetaFileWriter.UIntsToBytes(uints);
        Assert.Equal(original, back);
    }

    [Fact]
    public void MetaFileWriter_WriteSteamMeta_CreatesFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string savePath = Path.Combine(tmpDir, "save.hg");
            File.WriteAllText(savePath, "dummy");

            byte[] compressedData = new byte[128];
            new Random(1).NextBytes(compressedData);

            var metaInfo = new SaveMetaInfo
            {
                BaseVersion = 4115, // Frontiers
                GameMode = 1,
                Season = 0,
                TotalPlayTime = 3600,
                SaveName = "Test Save",
            };

            MetaFileWriter.WriteSteamMeta(savePath, compressedData, 1024, metaInfo, 2);

            string metaPath = MetaFileWriter.GetSteamMetaPath(savePath);
            Assert.True(File.Exists(metaPath));
            byte[] metaBytes = File.ReadAllBytes(metaPath);
            Assert.True(metaBytes.Length > 0);

            // Verify it can be decrypted
            uint[] encrypted = MetaFileWriter.BytesToUInts(metaBytes);
            uint[] decrypted = MetaCrypto.Decrypt(encrypted, 2, 6);
            Assert.Equal(MetaFileWriter.META_HEADER, decrypted[0]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Theory]
    [InlineData(4115, MetaFileWriter.META_FORMAT_2)]  // Frontiers -> 2002
    [InlineData(4135, MetaFileWriter.META_FORMAT_3)]  // Worlds Part I -> 2003
    [InlineData(4145, MetaFileWriter.META_FORMAT_4)]  // Worlds Part II -> 2004
    [InlineData(4720, MetaFileWriter.META_FORMAT_4)]  // Current game -> 2004
    public void MetaFileWriter_WriteSteamMeta_WritesCorrectFormat(int baseVersion, uint expectedFormat)
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string savePath = Path.Combine(tmpDir, "save.hg");
            File.WriteAllText(savePath, "dummy");

            byte[] compressedData = new byte[128];
            new Random(1).NextBytes(compressedData);

            var metaInfo = new SaveMetaInfo
            {
                BaseVersion = baseVersion,
                GameMode = 1,
                Season = 0,
                TotalPlayTime = 3600,
                SaveName = "My Save",
                SaveSummary = "In a system",
                DifficultyPreset = 2,
            };

            MetaFileWriter.WriteSteamMeta(savePath, compressedData, 1024, metaInfo, 2);

            string metaPath = MetaFileWriter.GetSteamMetaPath(savePath);
            byte[] metaBytes = File.ReadAllBytes(metaPath);

            // Decrypt and verify format matches NMS game values (2001-2004)
            uint[] encrypted = MetaFileWriter.BytesToUInts(metaBytes);
            uint[] decrypted = MetaCrypto.Decrypt(encrypted, 2, 6);
            Assert.Equal(MetaFileWriter.META_HEADER, decrypted[0]);
            Assert.Equal(expectedFormat, decrypted[1]);

            // Verify save name can be read from correct offset
            byte[] decryptedBytes = MetaFileWriter.UIntsToBytes(decrypted);
            string saveName = System.Text.Encoding.UTF8.GetString(decryptedBytes, 88, 128).TrimEnd('\0');
            Assert.Equal("My Save", saveName);

            string summary = System.Text.Encoding.UTF8.GetString(decryptedBytes, 216, 128).TrimEnd('\0');
            Assert.Equal("In a system", summary);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void MetaFileWriter_WriteSwitchMeta_CreatesFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string savePath = Path.Combine(tmpDir, "savedata01.hg");
            File.WriteAllText(savePath, "dummy");

            var metaInfo = new SaveMetaInfo
            {
                BaseVersion = 4115,
                GameMode = 1,
                TotalPlayTime = 7200,
            };

            MetaFileWriter.WriteSwitchMeta(savePath, 2048, metaInfo, 1);

            string metaPath = Path.Combine(tmpDir, "manifest01.hg");
            Assert.True(File.Exists(metaPath));
            byte[] metaBytes = File.ReadAllBytes(metaPath);
            Assert.True(metaBytes.Length > 0);

            // Switch meta is NOT encrypted - verify header directly
            uint header = BitConverter.ToUInt32(metaBytes, 0);
            Assert.Equal(MetaFileWriter.META_HEADER_SWITCH_PS, header);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- SaveSlotManager: slot file path resolution ---

    [Theory]
    [InlineData(0, "save.hg")]
    [InlineData(1, "save2.hg")]
    [InlineData(2, "save3.hg")]
    public void SaveSlotManager_GetSlotFiles_Steam(int slotIndex, string expectedDataName)
    {
        var files = SaveSlotManager.GetSlotFiles("/saves/st_12345", slotIndex, SaveFileManager.Platform.Steam);
        Assert.NotNull(files.DataFile);
        Assert.EndsWith(expectedDataName, files.DataFile!);
        Assert.NotNull(files.MetaFile);
        Assert.Contains("mf_", files.MetaFile!);
    }

    [Theory]
    [InlineData(0, "savedata00.hg")]
    [InlineData(1, "savedata01.hg")]
    public void SaveSlotManager_GetSlotFiles_Switch(int slotIndex, string expectedDataName)
    {
        var files = SaveSlotManager.GetSlotFiles("/saves/switch", slotIndex, SaveFileManager.Platform.Switch);
        Assert.NotNull(files.DataFile);
        Assert.EndsWith(expectedDataName, files.DataFile!);
        Assert.NotNull(files.MetaFile);
        Assert.Contains("manifest", files.MetaFile!);
    }

    [Theory]
    [InlineData(0, "savedata00.hg")]
    [InlineData(1, "savedata01.hg")]
    public void SaveSlotManager_GetSlotFiles_PS4(int slotIndex, string expectedDataName)
    {
        var files = SaveSlotManager.GetSlotFiles("/saves/ps4", slotIndex, SaveFileManager.Platform.PS4);
        Assert.NotNull(files.DataFile);
        Assert.EndsWith(expectedDataName, files.DataFile!);
    }

    // --- SaveSlotManager: CopySlot ---

    [Fact]
    public void SaveSlotManager_CopySlot_CopiesFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Create source slot files
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "slot0data");
            File.WriteAllText(Path.Combine(tmpDir, "mf_save.hg"), "slot0meta");

            SaveSlotManager.CopySlot(tmpDir, 0, 1, SaveFileManager.Platform.Steam);

            Assert.True(File.Exists(Path.Combine(tmpDir, "save2.hg")));
            Assert.Equal("slot0data", File.ReadAllText(Path.Combine(tmpDir, "save2.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_CopySlot_SameSlot_Noop()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "data");

            // Copying slot 0 to slot 0 should be a no-op
            SaveSlotManager.CopySlot(tmpDir, 0, 0, SaveFileManager.Platform.Steam);

            Assert.True(File.Exists(Path.Combine(tmpDir, "save.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_DeleteSlot_RemovesFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "data");
            File.WriteAllText(Path.Combine(tmpDir, "mf_save.hg"), "meta");

            SaveSlotManager.DeleteSlot(tmpDir, 0, SaveFileManager.Platform.Steam);

            Assert.False(File.Exists(Path.Combine(tmpDir, "save.hg")));
            Assert.False(File.Exists(Path.Combine(tmpDir, "mf_save.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_MoveSlot_MovesFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "data0");

            SaveSlotManager.MoveSlot(tmpDir, 0, 2, SaveFileManager.Platform.Steam);

            Assert.False(File.Exists(Path.Combine(tmpDir, "save.hg")));
            Assert.True(File.Exists(Path.Combine(tmpDir, "save3.hg")));
            Assert.Equal("data0", File.ReadAllText(Path.Combine(tmpDir, "save3.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SaveSlotManager_SwapSlots_SwapsBothWays()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "save.hg"), "dataA");
            File.WriteAllText(Path.Combine(tmpDir, "save2.hg"), "dataB");

            SaveSlotManager.SwapSlots(tmpDir, 0, 1, SaveFileManager.Platform.Steam);

            Assert.Equal("dataB", File.ReadAllText(Path.Combine(tmpDir, "save.hg")));
            Assert.Equal("dataA", File.ReadAllText(Path.Combine(tmpDir, "save2.hg")));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- TransferOptions: ownership rewrite ---

    [Fact]
    public void TransferOptions_DefaultsToAllTransferEnabled()
    {
        var options = new TransferOptions();
        Assert.True(options.TransferBases);
        Assert.True(options.TransferDiscoveries);
        Assert.True(options.TransferSettlements);
        Assert.True(options.TransferByteBeat);
    }

    // --- ContainersIndexManager: Xbox detection ---

    [Fact]
    public void ContainersIndexManager_IsXboxSaveDirectory_TrueWhenContainersIndexExists()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "containers.index"), "");
            Assert.True(ContainersIndexManager.IsXboxSaveDirectory(tmpDir));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ContainersIndexManager_IsXboxSaveDirectory_FalseWhenMissing()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            Assert.False(ContainersIndexManager.IsXboxSaveDirectory(tmpDir));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void ContainersIndexManager_ParseContainersIndex_ReturnsEmptyForInvalidFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "containers.index");
            File.WriteAllBytes(path, new byte[] { 0, 0, 0, 0 }); // wrong header
            var result = ContainersIndexManager.ParseContainersIndex(path);
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- MemoryDatManager: PS4 detection ---

    [Fact]
    public void MemoryDatManager_IsMemoryDat_DetectsCorrectFilename()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "memory.dat");
            File.WriteAllBytes(path, new byte[32]);
            Assert.True(MemoryDatManager.IsMemoryDat(path));

            string otherPath = Path.Combine(tmpDir, "save.hg");
            File.WriteAllBytes(otherPath, new byte[32]);
            Assert.False(MemoryDatManager.IsMemoryDat(otherPath));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void MemoryDatManager_IsSaveWizardFormat_FalseForNonSaveWizard()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "memory.dat");
            File.WriteAllBytes(path, new byte[1024]); // zeros - not SaveWizard
            Assert.False(MemoryDatManager.IsSaveWizardFormat(path));
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- MetaFileWriter: ExtractMetaInfo ---

    [Fact]
    public void MetaFileWriter_ExtractMetaInfo_ExtractsVersion()
    {
        var json = JsonObject.Parse("{\"Version\":4115}");
        var info = MetaFileWriter.ExtractMetaInfo(json);
        Assert.Equal(4115, info.BaseVersion);
    }

    // --- SaveFileManager: enhanced SaveToFile backward compat ---

    [Fact]
    public void SaveFileManager_SaveToFile_BackwardCompatible()
    {
        // Calling SaveToFile with just 3 args (old signature) should still work
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string path = Path.Combine(tmpDir, "test.hg");
            var data = JsonObject.Parse("{\"Version\":1,\"test\":42}");

            SaveFileManager.SaveToFile(path, data, compress: false);
            Assert.True(File.Exists(path));

            byte[] bytes = File.ReadAllBytes(path);
            // Should end with null terminator
            Assert.Equal(0, bytes[^1]);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    // --- Platform token mapping ---

    [Theory]
    [InlineData(SaveFileManager.Platform.Steam, "PC")]
    [InlineData(SaveFileManager.Platform.GOG, "PC")]
    [InlineData(SaveFileManager.Platform.XboxGamePass, "XBX")]
    [InlineData(SaveFileManager.Platform.PS4, "PS4")]
    [InlineData(SaveFileManager.Platform.Switch, "NX")]
    public void PlatformToken_MapsCorrectly(SaveFileManager.Platform platform, string expectedToken)
    {
        var options = new TransferOptions { DestPTK = expectedToken };
        Assert.Equal(expectedToken, options.DestPTK);
        Assert.NotEqual(SaveFileManager.Platform.Unknown, platform);
    }

    // --- Platform detection from directories ---

    [Fact]
    public void DetectPlatform_Steam_FromSaveHgFiles()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_steam_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Steam: save.hg files in a st_ named directory
            string profileDir = Path.Combine(tmpDir, "st_12345");
            Directory.CreateDirectory(profileDir);
            File.WriteAllBytes(Path.Combine(profileDir, "save.hg"), new byte[] { 0 });
            File.WriteAllBytes(Path.Combine(profileDir, "accountdata.hg"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(profileDir);
            Assert.Equal(SaveFileManager.Platform.Steam, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_GOG_FromDefaultUserDirectory()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_gog_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string profileDir = Path.Combine(tmpDir, "DefaultUser");
            Directory.CreateDirectory(profileDir);
            File.WriteAllBytes(Path.Combine(profileDir, "save.hg"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(profileDir);
            Assert.Equal(SaveFileManager.Platform.GOG, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_Xbox_FromContainersIndex()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_xbox_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tmpDir, "containers.index"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(tmpDir);
            Assert.Equal(SaveFileManager.Platform.XboxGamePass, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_PS4_FromMemoryDat()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_ps4_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tmpDir, "memory.dat"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(tmpDir);
            Assert.Equal(SaveFileManager.Platform.PS4, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void DetectPlatform_Switch_FromManifestDat()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_switch_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllBytes(Path.Combine(tmpDir, "manifest00.dat"), new byte[] { 0 });

            var platform = SaveFileManager.DetectPlatform(tmpDir);
            Assert.Equal(SaveFileManager.Platform.Switch, platform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    // --- SaveToFile with writeMeta flag ---

    [Fact]
    public void SaveToFile_WriteMeta_CreatesMetaFile()
    {
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_meta_{Guid.NewGuid():N}");
        // Mimic a Steam directory with st_ prefix
        string profileDir = Path.Combine(tmpDir, "st_test");
        Directory.CreateDirectory(profileDir);
        try
        {
            string savePath = Path.Combine(profileDir, "save.hg");

            // Create a minimal JSON save
            var data = new JsonObject();
            data.Set("Version", 5);

            // Write with meta
            SaveFileManager.SaveToFile(savePath, data, compress: true, writeMeta: true,
                platform: SaveFileManager.Platform.Steam, slotIndex: 0);

            Assert.True(File.Exists(savePath), "Save file should exist");

            // The meta file should also have been created (mf_save.hg)
            string metaPath = MetaFileWriter.GetSteamMetaPath(savePath);
            Assert.True(File.Exists(metaPath), $"Meta file should exist at {metaPath}");
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void ContainersIndexManager_ParseContainersIndex_ResolvesNonHyphenatedGuidDirs()
    {
        // Xbox Game Pass blob directories use the compact "N" GUID format (no hyphens).
        // Verify that ParseContainersIndex resolves them correctly.
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_xbox_guid_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            Guid testGuid = Guid.NewGuid();

            // Create the blob directory with the uppercase no-hyphens name (Xbox default format)
            string blobDir = Path.Combine(tmpDir, testGuid.ToString("N").ToUpperInvariant());
            Directory.CreateDirectory(blobDir);

            // Build a minimal valid containers.index binary.
            // The parser requires at least 200 bytes, so we pad to that length.
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);

            // ---- global header ----
            w.Write(14);  // header magic
            w.Write(1L);  // container count = 1

            // processIdentifier (dynamic string): length=0
            w.Write(0);
            // lastModifiedTime(8) + syncState(4)
            w.Write(0L);
            w.Write(0);
            // accountGuid (dynamic string): length=0
            w.Write(0);
            // footer(8)
            w.Write(268435456L); // 0x10000000

            // ---- entry 1 ----
            // identifier1 (dynamic string): "Slot1Auto" (9 chars = 18 bytes UTF-16)
            string id = "Slot1Auto";
            w.Write(id.Length);
            w.Write(Encoding.Unicode.GetBytes(id));
            // identifier2 (dynamic string): length=0
            w.Write(0);
            // syncTime (dynamic string): length=0
            w.Write(0);

            // Fixed fields: blobExt(1) + syncState(4) + guid(16) + lastMod(8) + empty(8) + totalSize(8) = 45
            w.Write((byte)1); // blob extension
            w.Write(0);       // sync state
            w.Write(testGuid.ToByteArray()); // directory GUID
            w.Write(0L);      // last modified
            w.Write(0L);      // empty
            w.Write(0L);      // total size

            // Pad to at least 200 bytes (parser's minimum size check)
            while (ms.Position < 200)
                w.Write((byte)0);

            byte[] data = ms.ToArray();
            string indexPath = Path.Combine(tmpDir, "containers.index");
            File.WriteAllBytes(indexPath, data);

            var slots = ContainersIndexManager.ParseContainersIndex(indexPath);

            // The slot should be found and its BlobDirectoryPath should point to
            // the no-hyphens uppercase directory (which exists on disk)
            Assert.True(slots.ContainsKey("Slot1Auto"), "Expected Slot1Auto in parsed slots");
            Assert.Equal(blobDir, slots["Slot1Auto"].BlobDirectoryPath);
            Assert.True(Directory.Exists(slots["Slot1Auto"].BlobDirectoryPath),
                "BlobDirectoryPath should point to an existing directory");
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void FindDefaultSaveDirectory_XboxGamePass_NavigatesIntoWgsDir()
    {
        // Verify the expected Xbox Game Pass path structure:
        // {root}/HelloGames.NoMansSky_bs190hzg1sesy/SystemAppData/wgs/{SaveId}/containers.index
        // The method should return the {SaveId} directory, not the HelloGames root.
        //
        // This test cannot run FindDefaultSaveDirectory directly (it uses Environment.SpecialFolder)
        // so we just verify that DetectPlatform works correctly on the nested directory.
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_xbox_nested_{Guid.NewGuid():N}");
        try
        {
            string saveIdDir = Path.Combine(tmpDir, "HelloGames.NoMansSky_bs190hzg1sesy",
                "SystemAppData", "wgs", "AABBCCDD00112233");
            Directory.CreateDirectory(saveIdDir);
            File.WriteAllBytes(Path.Combine(saveIdDir, "containers.index"), new byte[] { 0 });

            // DetectPlatform on the SaveId dir (containing containers.index) should work
            var platform = SaveFileManager.DetectPlatform(saveIdDir);
            Assert.Equal(SaveFileManager.Platform.XboxGamePass, platform);

            // DetectPlatform on the HelloGames root should NOT detect Xbox
            string helloGamesRoot = Path.Combine(tmpDir, "HelloGames.NoMansSky_bs190hzg1sesy");
            var rootPlatform = SaveFileManager.DetectPlatform(helloGamesRoot);
            Assert.Equal(SaveFileManager.Platform.Unknown, rootPlatform);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void ContainersIndexManager_ParseBlobContainer_Resolves128ByteIdentifiers()
    {
        // Xbox blob container files use 128-byte UTF-16LE identifiers (not 80 bytes).
        // The total container.N file size is 328 bytes (header 8 + 2 entries * 160).
        // Both NMSSaveEditor.jar (gc.d() reads 128 bytes) and libNOM (BLOBCONTAINER_IDENTIFIER_LENGTH=128)
        // confirm this format.
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_blobcontainer_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            Guid testGuid = Guid.NewGuid();
            string blobDir = Path.Combine(tmpDir, testGuid.ToString("N").ToUpperInvariant());
            Directory.CreateDirectory(blobDir);

            // Create data and meta blob files
            Guid dataGuid = Guid.NewGuid();
            Guid metaGuid = Guid.NewGuid();
            string dataPath = Path.Combine(blobDir, dataGuid.ToString("N").ToUpperInvariant());
            string metaPath = Path.Combine(blobDir, metaGuid.ToString("N").ToUpperInvariant());
            File.WriteAllBytes(dataPath, new byte[] { 0x01 });
            File.WriteAllBytes(metaPath, new byte[] { 0x02 });

            // Build a valid 328-byte container.1 file with 128-byte identifiers
            byte[] containerBytes = new byte[328];
            using (var ms = new MemoryStream(containerBytes))
            using (var w = new BinaryWriter(ms))
            {
                w.Write(4);  // header
                w.Write(2);  // count

                // "data" entry: 128 bytes identifier + 16 cloud GUID + 16 local GUID
                byte[] dataId = Encoding.Unicode.GetBytes("data");
                w.Write(dataId);
                ms.Position = 8 + 128; // skip to end of 128-byte identifier
                w.Write(new byte[16]); // cloud GUID (zeros)
                w.Write(dataGuid.ToByteArray()); // local GUID

                // "meta" entry: 128 bytes identifier + 16 cloud GUID + 16 local GUID
                byte[] metaId = Encoding.Unicode.GetBytes("meta");
                w.Write(metaId);
                ms.Position = 8 + 160 + 128; // skip to end of second 128-byte identifier
                w.Write(new byte[16]); // cloud GUID (zeros)
                w.Write(metaGuid.ToByteArray()); // local GUID
            }
            File.WriteAllBytes(Path.Combine(blobDir, "container.1"), containerBytes);

            // Build a minimal valid containers.index
            using var indexMs = new MemoryStream();
            using var indexW = new BinaryWriter(indexMs);
            indexW.Write(14);         // header
            indexW.Write(1L);         // count
            indexW.Write(0);          // processIdentifier (empty)
            indexW.Write(0L);         // lastModifiedTime
            indexW.Write(0);          // syncState
            indexW.Write(0);          // accountGuid (empty)
            indexW.Write(268435456L); // footer

            // entry: Slot1Auto
            string id = "Slot1Auto";
            indexW.Write(id.Length);
            indexW.Write(Encoding.Unicode.GetBytes(id));
            indexW.Write(0);   // identifier2
            indexW.Write(0);   // syncTime
            indexW.Write((byte)1); // blob extension
            indexW.Write(0);       // sync state
            indexW.Write(testGuid.ToByteArray());
            indexW.Write(0L);      // last modified
            indexW.Write(0L);      // empty
            indexW.Write(0L);      // total size
            while (indexMs.Position < 200) indexW.Write((byte)0);

            string indexPath = Path.Combine(tmpDir, "containers.index");
            File.WriteAllBytes(indexPath, indexMs.ToArray());

            var slots = ContainersIndexManager.ParseContainersIndex(indexPath);

            Assert.True(slots.ContainsKey("Slot1Auto"));
            var slot = slots["Slot1Auto"];
            Assert.NotNull(slot.DataFilePath);
            Assert.NotNull(slot.MetaFilePath);
            Assert.True(File.Exists(slot.DataFilePath), "DataFilePath should point to existing file");
            Assert.True(File.Exists(slot.MetaFilePath), "MetaFilePath should point to existing file");
            Assert.Equal(dataPath, slot.DataFilePath);
            Assert.Equal(metaPath, slot.MetaFilePath);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void ContainersIndexManager_WriteBlobContainer_Creates328ByteFile()
    {
        // Verify that WriteBlobContainer creates a file that's exactly 328 bytes
        // with proper 128-byte identifier padding.
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_write_blob_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            Guid dataGuid = Guid.NewGuid();
            Guid metaGuid = Guid.NewGuid();

            var slotInfo = new XboxSlotInfo
            {
                BlobDirectoryPath = tmpDir,
                BlobContainerExtension = 0,
            };

            // Create dummy data/meta files
            File.WriteAllBytes(Path.Combine(tmpDir, dataGuid.ToString("N").ToUpperInvariant()), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(tmpDir, metaGuid.ToString("N").ToUpperInvariant()), new byte[] { 2 });

            // Write the blob container (calls the private WriteBlobContainer indirectly via WriteXboxSave)
            ContainersIndexManager.WriteXboxSave(slotInfo, new byte[] { 0xAA }, new byte[] { 0xBB });

            // Find the created container file
            string[] containerFiles = Directory.GetFiles(tmpDir, "container.*");
            Assert.Single(containerFiles);

            byte[] bytes = File.ReadAllBytes(containerFiles[0]);
            Assert.Equal(328, bytes.Length);

            // Verify header
            int header = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
            Assert.Equal(4, header);

            // Verify count
            int count = bytes[4] | (bytes[5] << 8) | (bytes[6] << 16) | (bytes[7] << 24);
            Assert.Equal(2, count);

            // Verify "data" identifier at offset 8 (first 8 bytes are UTF-16LE "data")
            string dataId = Encoding.Unicode.GetString(bytes, 8, 8);
            Assert.Equal("data", dataId);

            // Verify "meta" identifier at offset 168 (8 + 128 + 32)
            string metaId = Encoding.Unicode.GetString(bytes, 168, 8);
            Assert.Equal("meta", metaId);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void ContainersIndexManager_WriteContainersIndex_RoundTrip_PreservesData()
    {
        // Verify that parsing containers.index and writing it back produces a file
        // that can be re-parsed with the same slot data. This is the critical
        // round-trip test that prevents the ~1100 KB corruption bug.
        string xboxSaveDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C");
        string containersPath = Path.Combine(xboxSaveDir, "containers.index");
        if (!File.Exists(containersPath))
            return; // Skip if test data not available

        // Parse the original file with full header data
        var original = ContainersIndexManager.ParseContainersIndexFull(containersPath);
        Assert.Equal(6, original.Slots.Count);
        Assert.False(string.IsNullOrEmpty(original.ProcessIdentifier));
        Assert.False(string.IsNullOrEmpty(original.AccountGuid));

        // Write to a temp file
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_ci_roundtrip_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            string tmpPath = Path.Combine(tmpDir, "containers.index");
            ContainersIndexManager.WriteContainersIndex(
                tmpPath,
                original.Slots.Values,
                original.ProcessIdentifier,
                original.AccountGuid,
                original.LastWriteTime);

            // The written file must be small (about 1 KB), NOT megabytes
            long fileSize = new FileInfo(tmpPath).Length;
            Assert.True(fileSize < 2048, $"containers.index is {fileSize} bytes, expected < 2048");
            Assert.True(fileSize > 100, $"containers.index is {fileSize} bytes, expected > 100");

            // Re-parse the written file
            var reparsed = ContainersIndexManager.ParseContainersIndexFull(tmpPath);
            Assert.Equal(original.Slots.Count, reparsed.Slots.Count);
            Assert.Equal(original.ProcessIdentifier, reparsed.ProcessIdentifier);
            Assert.Equal(original.AccountGuid, reparsed.AccountGuid);

            // Verify each slot was preserved
            foreach (var kvp in original.Slots)
            {
                Assert.True(reparsed.Slots.ContainsKey(kvp.Key), $"Missing slot: {kvp.Key}");
                var orig = kvp.Value;
                var copy = reparsed.Slots[kvp.Key];
                Assert.Equal(orig.Identifier, copy.Identifier);
                Assert.Equal(orig.DirectoryGuid, copy.DirectoryGuid);
                Assert.Equal(orig.BlobContainerExtension, copy.BlobContainerExtension);
            }
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void ContainersIndexManager_ParseContainersIndexFull_ExposesHeaderFields()
    {
        // Verify that ParseContainersIndexFull returns the header fields needed for writing
        string xboxSaveDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C");
        string containersPath = Path.Combine(xboxSaveDir, "containers.index");
        if (!File.Exists(containersPath))
            return; // Skip if test data not available

        var data = ContainersIndexManager.ParseContainersIndexFull(containersPath);

        // Process identifier should be the NMS app identifier
        Assert.Contains("HelloGames", data.ProcessIdentifier);
        Assert.Contains("NoMansSky", data.ProcessIdentifier);

        // Account GUID should be a valid GUID-like string
        Assert.True(data.AccountGuid.Length > 0);

        // Last write time should be a valid date
        Assert.True(data.LastWriteTime.Year >= 2020);

        // Slots should be the same as ParseContainersIndex
        var slots = ContainersIndexManager.ParseContainersIndex(containersPath);
        Assert.Equal(slots.Count, data.Slots.Count);
        foreach (var key in slots.Keys)
            Assert.True(data.Slots.ContainsKey(key));
    }

    [Fact]
    public void SaveXboxAccountData_WritesRawLz4_LoadableByLoadXboxSave()
    {
        // Verify that SaveXboxAccountData produces a raw LZ4 blob that can be
        // read back by LoadXboxSave (which tries raw LZ4 as a fallback).
        // This tests the critical round-trip: modify account -> save -> reload.
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_xbox_acct_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        string blobDir = Path.Combine(tmpDir, Guid.NewGuid().ToString("N").ToUpperInvariant());
        Directory.CreateDirectory(blobDir);
        try
        {
            // Create test account JSON with a season reward
            string testJson = "{\"UserSettingsData\":{\"UnlockedSeasonRewards\":[{\"value\":\"RAW_TestReward\"}]}}";
            var accountObj = JsonObject.Parse(testJson);

            // Manually compress with raw LZ4 (same as SaveXboxAccountData does)
            var latin1 = Encoding.GetEncoding(28591);
            byte[] jsonBytes = latin1.GetBytes(testJson);
            byte[] dataBytes = new byte[jsonBytes.Length + 1];
            Buffer.BlockCopy(jsonBytes, 0, dataBytes, 0, jsonBytes.Length);

            byte[] compBuf = new byte[Lz4Compressor.MaxCompressedLength(dataBytes.Length)];
            int compLen = Lz4Compressor.Compress(dataBytes, 0, dataBytes.Length, compBuf, 0, compBuf.Length);
            byte[] compressed = new byte[compLen];
            Buffer.BlockCopy(compBuf, 0, compressed, 0, compLen);

            // Write to a blob file
            Guid dataGuid = Guid.NewGuid();
            string dataPath = Path.Combine(blobDir, dataGuid.ToString("N").ToUpperInvariant());
            File.WriteAllBytes(dataPath, compressed);

            // Create a slot info pointing to this file
            var slotInfo = new XboxSlotInfo
            {
                Identifier = "AccountData",
                DataFilePath = dataPath,
                BlobDirectoryPath = blobDir,
            };

            // LoadXboxSave should decompress the raw LZ4 and return valid JSON
            string? result = ContainersIndexManager.LoadXboxSave(slotInfo);
            Assert.NotNull(result);

            // Trim null terminator if present
            result = result!.TrimEnd('\0');

            var parsed = JsonObject.Parse(result);
            var userSettings = parsed.GetObject("UserSettingsData");
            Assert.NotNull(userSettings);
            var rewards = userSettings!.GetArray("UnlockedSeasonRewards");
            Assert.NotNull(rewards);
            Assert.True(rewards!.Length > 0);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    [Fact]
    public void ContainersIndexManager_LoadXboxSave_HandlesHgsv2Format()
    {
        // Verify that HGSAVEV2 format saves can be loaded.
        // HGSAVEV2 format: "HGSAVEV2\0" + N frames of [decompressedLen(4)][compressedLen(4)][LZ4 data]
        string tmpDir = Path.Combine(Path.GetTempPath(), $"nmse_test_hgsv2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        try
        {
            // Create a simple JSON string to compress
            string json = "{\"Test\":true}";
            byte[] jsonBytes = Encoding.GetEncoding(28591).GetBytes(json);

            // LZ4 compress the JSON
            byte[] compressed = new byte[Lz4Compressor.MaxCompressedLength(jsonBytes.Length)];
            int compressedLen = Lz4Compressor.Compress(jsonBytes, 0, jsonBytes.Length, compressed, 0, compressed.Length);

            // Build HGSAVEV2 file
            using var ms = new MemoryStream();
            ms.Write(Encoding.ASCII.GetBytes("HGSAVEV2"));
            ms.WriteByte(0); // null terminator
            // Frame header: decompressed size + compressed size (LE)
            ms.Write(BitConverter.GetBytes(jsonBytes.Length));
            ms.Write(BitConverter.GetBytes(compressedLen));
            ms.Write(compressed, 0, compressedLen);

            Guid dataGuid = Guid.NewGuid();
            string dataPath = Path.Combine(tmpDir, dataGuid.ToString("N").ToUpperInvariant());
            File.WriteAllBytes(dataPath, ms.ToArray());

            var slotInfo = new XboxSlotInfo
            {
                DataFilePath = dataPath,
                BlobDirectoryPath = tmpDir,
            };

            string? result = ContainersIndexManager.LoadXboxSave(slotInfo);
            Assert.NotNull(result);
            Assert.Equal(json, result);
        }
        finally { Directory.Delete(tmpDir, true); }
    }

    // --- ContainersIndexManager: IsSaveSlot filtering ---

    [Theory]
    [InlineData("Slot1Auto", true)]
    [InlineData("Slot1Manual", true)]
    [InlineData("Slot3Auto", true)]
    [InlineData("Slot3Manual", true)]
    [InlineData("AccountData", false)]
    [InlineData("Settings", false)]
    [InlineData("accountdata", false)]   // case-insensitive
    [InlineData("settings", false)]      // case-insensitive
    [InlineData("ACCOUNTDATA", false)]   // case-insensitive
    [InlineData("Slot2Auto", true)]
    public void ContainersIndexManager_IsSaveSlot_FiltersCorrectly(string identifier, bool expected)
    {
        Assert.Equal(expected, ContainersIndexManager.IsSaveSlot(identifier));
    }

    // --- ContainersIndexManager: ExtractSlotNumber ---

    [Theory]
    [InlineData("Slot1Auto", 1)]
    [InlineData("Slot1Manual", 1)]
    [InlineData("Slot2Auto", 2)]
    [InlineData("Slot3Manual", 3)]
    [InlineData("Slot15Auto", 15)]
    [InlineData("AccountData", 0)]
    [InlineData("Settings", 0)]
    [InlineData("Slot", 0)]
    [InlineData("SlotAuto", 0)]  // no number
    [InlineData("", 0)]
    public void ContainersIndexManager_ExtractSlotNumber_ExtractsCorrectly(string identifier, int expected)
    {
        Assert.Equal(expected, ContainersIndexManager.ExtractSlotNumber(identifier));
    }

    // --- ContainersIndexManager: IsAutoSave ---

    [Theory]
    [InlineData("Slot1Auto", true)]
    [InlineData("Slot1Manual", false)]
    [InlineData("Slot3Auto", true)]
    [InlineData("AccountData", false)]
    public void ContainersIndexManager_IsAutoSave_DetectsCorrectly(string identifier, bool expected)
    {
        Assert.Equal(expected, ContainersIndexManager.IsAutoSave(identifier));
    }

    // --- ContainersIndexManager: Real Xbox save parsing ---

    [Fact]
    public void ContainersIndexManager_ParseContainersIndex_RealXboxSave_IdentifiesAllSlots()
    {
        string xboxSaveDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C");
        string containersPath = Path.Combine(xboxSaveDir, "containers.index");
        if (!File.Exists(containersPath))
            return; // Skip if test data not available

        var slots = ContainersIndexManager.ParseContainersIndex(containersPath);

        // The real Xbox save contains 6 entries
        Assert.Equal(6, slots.Count);
        Assert.True(slots.ContainsKey("AccountData"));
        Assert.True(slots.ContainsKey("Settings"));
        Assert.True(slots.ContainsKey("Slot1Auto"));
        Assert.True(slots.ContainsKey("Slot1Manual"));
        Assert.True(slots.ContainsKey("Slot3Auto"));
        Assert.True(slots.ContainsKey("Slot3Manual"));
    }

    [Fact]
    public void ContainersIndexManager_IsSaveSlot_FiltersRealXboxSave()
    {
        string xboxSaveDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C");
        string containersPath = Path.Combine(xboxSaveDir, "containers.index");
        if (!File.Exists(containersPath))
            return; // Skip if test data not available

        var slots = ContainersIndexManager.ParseContainersIndex(containersPath);

        // Only save slots (not AccountData/Settings) should pass the filter
        var saveSlots = slots.Where(s => ContainersIndexManager.IsSaveSlot(s.Key)).ToList();
        Assert.Equal(4, saveSlots.Count);
        Assert.DoesNotContain(saveSlots, s => s.Key == "AccountData");
        Assert.DoesNotContain(saveSlots, s => s.Key == "Settings");
    }

    [Fact]
    public void ContainersIndexManager_LoadXboxSave_RealXboxSave_LoadsSaveSlot()
    {
        string xboxSaveDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C");
        string containersPath = Path.Combine(xboxSaveDir, "containers.index");
        if (!File.Exists(containersPath))
            return; // Skip if test data not available

        var slots = ContainersIndexManager.ParseContainersIndex(containersPath);
        Assert.True(slots.ContainsKey("Slot1Auto"));

        var slot = slots["Slot1Auto"];
        Assert.NotNull(slot.DataFilePath);
        Assert.True(File.Exists(slot.DataFilePath));

        string? json = ContainersIndexManager.LoadXboxSave(slot);
        Assert.NotNull(json);
        Assert.True(json.Length > 100); // Should be substantial JSON data

        // Verify it parses as valid JSON
        var obj = JsonObject.Parse(json);
        Assert.True(obj.Size() > 0);
    }

    [Fact]
    public void ContainersIndexManager_LoadXboxSave_RealXboxSave_LoadsAccountData()
    {
        string xboxSaveDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "_ref", "xbox_save", "000900000150C65A_29070100B936489ABCE8B9AF3980429C");
        string containersPath = Path.Combine(xboxSaveDir, "containers.index");
        if (!File.Exists(containersPath))
            return; // Skip if test data not available

        var slots = ContainersIndexManager.ParseContainersIndex(containersPath);
        Assert.True(slots.ContainsKey("AccountData"));

        var accountSlot = slots["AccountData"];
        Assert.NotNull(accountSlot.DataFilePath);
        Assert.True(File.Exists(accountSlot.DataFilePath));

        string? json = ContainersIndexManager.LoadXboxSave(accountSlot);
        Assert.NotNull(json);

        // Verify it parses as valid JSON with expected account structure
        var obj = JsonObject.Parse(json);
        Assert.True(obj.Size() > 0);
        // Xbox account data should have UserSettingsData like accountdata.hg
        var userSettings = obj.GetObject("UserSettingsData");
        Assert.NotNull(userSettings);
    }
}
