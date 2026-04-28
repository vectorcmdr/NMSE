using NMSE.Models;

namespace NMSE.IO;

/// <summary>
/// File paths for a save slot.
/// </summary>
public class SlotFiles
{
    /// <summary>Gets or sets the path to the save data file.</summary>
    public string? DataFile { get; set; }
    /// <summary>Gets or sets the path to the companion metadata file.</summary>
    public string? MetaFile { get; set; }
}

/// <summary>
/// Options for cross-platform save transfer.
/// Controls which ownership references to rewrite and the destination user identity.
/// </summary>
public class TransferOptions
{
    /// <summary>Source user UID to match (leave empty to transfer all).</summary>
    public string? SourceUID { get; set; }
    /// <summary>Destination user UID.</summary>
    public string? DestUID { get; set; }
    /// <summary>Destination user LID (lobby ID).</summary>
    public string? DestLID { get; set; }
    /// <summary>Destination user USN (username).</summary>
    public string? DestUSN { get; set; }
    /// <summary>Destination platform token (PC, XBX, PS4, NX).</summary>
    public string? DestPTK { get; set; }
    /// <summary>Transfer base ownership references.</summary>
    public bool TransferBases { get; set; } = true;
    /// <summary>Transfer discovery ownership references.</summary>
    public bool TransferDiscoveries { get; set; } = true;
    /// <summary>Transfer settlement ownership references.</summary>
    public bool TransferSettlements { get; set; } = true;
    /// <summary>Transfer ByteBeat song authorship.</summary>
    public bool TransferByteBeat { get; set; } = true;
}

/// <summary>
/// Save slot operations: copy, move, swap within a platform, and cross-platform transfer.
///
/// Slot copy/move/swap operates within the same save directory.
/// Cross-platform transfer converts ownership UIDs, platform tokens, as well as base,
/// settlement, discovery and ByteBeat author references so saves work correctly on the
/// destination platform.
///
/// Each NMS save "slot" (as shown in the game's UI) contains TWO files: an auto save
/// and a manual save.  The slot index is 0-based (slot 0 = game "Slot 1").
///
/// Steam/GOG file layout (15 slots × 2 files each = 30 files):
///   Slot 0: save.hg   (auto)  + save2.hg   (manual)
///   Slot 1: save3.hg  (auto)  + save4.hg   (manual)
///   Slot N: save(2N+1).hg     + save(2N+2).hg   (with special case: slot 0 auto = save.hg)
///
/// Switch / PS4 streaming file layout (15 slots × 2 files each = 30 files):
///   Slot 0: savedata00.hg (auto) + savedata01.hg (manual)
///   Slot 1: savedata02.hg (auto) + savedata03.hg (manual)
///   Slot N: savedata(2N).hg      + savedata(2N+1).hg
/// </summary>
public static class SaveSlotManager
{
    // Helpers

    /// <summary>
    /// Get the token for a given platform.
    /// </summary>
    private static string GetPlatformToken(SaveFileManager.Platform platform) => platform switch
    {
        SaveFileManager.Platform.Steam => "PC",
        SaveFileManager.Platform.GOG => "PC",
        SaveFileManager.Platform.XboxGamePass => "XBX",
        SaveFileManager.Platform.PS4 => "PS4",
        SaveFileManager.Platform.Switch => "NX",
        _ => "PC",
    };

    /// <summary>
    /// Returns both file pairs (auto save and manual save) for a given slot index on a
    /// given platform.  Index 0 is the auto save, index 1 is the manual save.
    /// </summary>
    public static SlotFiles[] GetAllSlotFiles(string saveDirectory, int slotIndex,
        SaveFileManager.Platform platform)
    {
        return platform switch
        {
            SaveFileManager.Platform.Steam or SaveFileManager.Platform.GOG =>
                GetSteamAllSlotFiles(saveDirectory, slotIndex),
            SaveFileManager.Platform.Switch =>
                GetSwitchAllSlotFiles(saveDirectory, slotIndex),
            SaveFileManager.Platform.PS4 =>
                GetPlaystationAllSlotFiles(saveDirectory, slotIndex),
            _ => Array.Empty<SlotFiles>()
        };
    }

    /// <summary>
    /// Get file paths for the primary (manual) save in a slot on a given platform.
    /// Used by <see cref="TransferCrossPlatform"/> to locate the destination file.
    /// </summary>
    public static SlotFiles GetSlotFiles(string saveDirectory, int slotIndex,
        SaveFileManager.Platform platform)
    {
        var all = GetAllSlotFiles(saveDirectory, slotIndex, platform);
        // Return the manual save (index 1) when available; fall back to index 0 or empty.
        return all.Length > 1 ? all[1] : (all.Length == 1 ? all[0] : new SlotFiles());
    }

    // === Steam / GOG ===

    private static SlotFiles[] GetSteamAllSlotFiles(string dir, int slotIndex)
    {
        // Slot N:  auto  = save.hg (N==0) or save{2N+1}.hg (N>0)
        //          manual = save{2N+2}.hg
        string autoDataName   = slotIndex == 0 ? "save.hg" : $"save{slotIndex * 2 + 1}.hg";
        string manualDataName = $"save{slotIndex * 2 + 2}.hg";

        string autoDataPath   = Path.Combine(dir, autoDataName);
        string manualDataPath = Path.Combine(dir, manualDataName);

        return
        [
            new SlotFiles { DataFile = autoDataPath,   MetaFile = MetaFileWriter.GetSteamMetaPath(autoDataPath)   },
            new SlotFiles { DataFile = manualDataPath, MetaFile = MetaFileWriter.GetSteamMetaPath(manualDataPath) },
        ];
    }

    // === Switch ===

    private static SlotFiles[] GetSwitchAllSlotFiles(string dir, int slotIndex)
    {
        // Slot N:  auto   = savedata{2N:D2}.hg  + manifest{2N:D2}.hg
        //          manual = savedata{2N+1:D2}.hg + manifest{2N+1:D2}.hg
        int autoIdx   = slotIndex * 2;
        int manualIdx = slotIndex * 2 + 1;

        return
        [
            new SlotFiles
            {
                DataFile = Path.Combine(dir, $"savedata{autoIdx:D2}.hg"),
                MetaFile = Path.Combine(dir, $"manifest{autoIdx:D2}.hg"),
            },
            new SlotFiles
            {
                DataFile = Path.Combine(dir, $"savedata{manualIdx:D2}.hg"),
                MetaFile = Path.Combine(dir, $"manifest{manualIdx:D2}.hg"),
            },
        ];
    }

    // === PS4 streaming ===

    private static SlotFiles[] GetPlaystationAllSlotFiles(string dir, int slotIndex)
    {
        // Same two file per slot layout as Switch.
        int autoIdx   = slotIndex * 2;
        int manualIdx = slotIndex * 2 + 1;

        return
        [
            new SlotFiles
            {
                DataFile = Path.Combine(dir, $"savedata{autoIdx:D2}.hg"),
                MetaFile = Path.Combine(dir, $"manifest{autoIdx:D2}.hg"),
            },
            new SlotFiles
            {
                DataFile = Path.Combine(dir, $"savedata{manualIdx:D2}.hg"),
                MetaFile = Path.Combine(dir, $"manifest{manualIdx:D2}.hg"),
            },
        ];
    }

    private static void WriteMetaForPlatform(SlotFiles files, JsonObject saveData,
        SaveFileManager.Platform platform, int slotIndex)
    {
        if (files.DataFile == null) return;

        var metaInfo = MetaFileWriter.ExtractMetaInfo(saveData);

        switch (platform)
        {
            case SaveFileManager.Platform.Steam:
            case SaveFileManager.Platform.GOG:
                if (File.Exists(files.DataFile))
                {
                    byte[] compressedData = File.ReadAllBytes(files.DataFile);
                    // Calculate decompressed size from the save data
                    string json = saveData.ToString();
                    uint decompressedSize = (uint)(System.Text.Encoding.GetEncoding(28591).GetByteCount(json) + 1);
                    int storageSlot = StorageSlotFromFileName(files.DataFile);
                    MetaFileWriter.WriteSteamMeta(files.DataFile, compressedData, decompressedSize, metaInfo, storageSlot);
                }
                break;

            case SaveFileManager.Platform.Switch:
                {
                    string json = saveData.ToString();
                    uint decompressedSize = (uint)(System.Text.Encoding.GetEncoding(28591).GetByteCount(json) + 1);
                    // Derive the manifest index from the savedata file name (savedata01.hg -> 1).
                    // Falls back to the raw slotIndex when the file name cannot be parsed.
                    int manifestIdx = ExtractSwitchManifestIndex(files.DataFile);
                    if (manifestIdx < 0) manifestIdx = slotIndex;
                    MetaFileWriter.WriteSwitchMeta(files.DataFile, decompressedSize, metaInfo, manifestIdx);
                    break;
                }

            case SaveFileManager.Platform.PS4:
                {
                    string json = saveData.ToString();
                    uint decompressedSize = (uint)(System.Text.Encoding.GetEncoding(28591).GetByteCount(json) + 1);
                    int manifestIdx = ExtractSwitchManifestIndex(files.DataFile);
                    if (manifestIdx < 0) manifestIdx = slotIndex;
                    MetaFileWriter.WritePlaystationStreamingMeta(files.DataFile, decompressedSize, metaInfo, manifestIdx);
                    break;
                }
        }
    }

    private static JsonArray? GetJsonArray(JsonObject root, string path)
    {
        object? value = root.GetValue(path);
        return value as JsonArray;
    }

    private static void SetJsonValueByPath(JsonObject root, string key, object value)
    {
        root.Set(key, value);
    }

    /// <summary>
    /// Copy all files in a save slot (auto save + manual save) to another slot within the
    /// same save directory.  Creates a <c>.backup</c> of each destination file that exists.
    /// For Steam/GOG, the companion meta file is re-keyed to the destination storage slot
    /// so that the game can decrypt it correctly.
    /// </summary>
    /// <param name="saveDirectory">Path to the save directory.</param>
    /// <param name="sourceSlotIndex">Source slot index (0-based; 0 = game "Slot 1").</param>
    /// <param name="destSlotIndex">Destination slot index.</param>
    /// <param name="platform">Platform type for the saves.</param>
    public static void CopySlot(string saveDirectory, int sourceSlotIndex, int destSlotIndex,
        SaveFileManager.Platform platform)
    {
        if (sourceSlotIndex == destSlotIndex) return;

        var sourcePairs = GetAllSlotFiles(saveDirectory, sourceSlotIndex, platform);
        var destPairs   = GetAllSlotFiles(saveDirectory, destSlotIndex,   platform);

        bool anySourceFound = sourcePairs.Any(f => f.DataFile != null && File.Exists(f.DataFile));
        if (!anySourceFound)
            throw new FileNotFoundException($"Source save slot {sourceSlotIndex} not found.");

        int count = Math.Min(sourcePairs.Length, destPairs.Length);
        for (int i = 0; i < count; i++)
        {
            var src = sourcePairs[i];
            var dst = destPairs[i];

            if (src.DataFile == null || !File.Exists(src.DataFile))
                continue;

            // Backup destination data file if it exists
            if (dst.DataFile != null && File.Exists(dst.DataFile))
                File.Copy(dst.DataFile, dst.DataFile + ".backup", true);

            // Copy data file
            File.Copy(src.DataFile, dst.DataFile!, true);

            // Copy meta file with re-keying for Steam/GOG
            if (src.MetaFile != null && File.Exists(src.MetaFile) && dst.MetaFile != null)
                CopyMetaFile(src.DataFile, src.MetaFile, dst.DataFile!, dst.MetaFile, platform);
        }
    }

    /// <summary>
    /// Move all files in a save slot to another slot (copy then delete source).
    /// </summary>
    public static void MoveSlot(string saveDirectory, int sourceSlotIndex, int destSlotIndex,
        SaveFileManager.Platform platform)
    {
        CopySlot(saveDirectory, sourceSlotIndex, destSlotIndex, platform);
        DeleteSlot(saveDirectory, sourceSlotIndex, platform);
    }

    /// <summary>
    /// Swap all files in two save slots (auto save and manual save for each).
    /// For Steam/GOG, meta files are re-keyed to the swapped destination storage slots.
    /// </summary>
    public static void SwapSlots(string saveDirectory, int slotA, int slotB,
        SaveFileManager.Platform platform)
    {
        if (slotA == slotB) return;

        string tempDir = Path.Combine(Path.GetTempPath(), $"nmse_swap_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var filesA = GetAllSlotFiles(saveDirectory, slotA, platform);
            var filesB = GetAllSlotFiles(saveDirectory, slotB, platform);

            int count = Math.Min(filesA.Length, filesB.Length);
            for (int i = 0; i < count; i++)
            {
                var fa = filesA[i];
                var fb = filesB[i];

                // Capture the original file paths before any moves so that
                // StorageSlotFromFileName always operates on the correct filename,
                // even after the underlying files have been relocated.
                string? faDataPath = fa.DataFile;
                string? fbDataPath = fb.DataFile;

                string tmpData = Path.Combine(tempDir, $"data_{i}");
                string tmpMeta = Path.Combine(tempDir, $"meta_{i}");

                // Phase 1: move A's files to temp
                if (faDataPath != null && File.Exists(faDataPath))
                    File.Move(faDataPath, tmpData);
                if (fa.MetaFile != null && File.Exists(fa.MetaFile))
                    File.Move(fa.MetaFile, tmpMeta);

                // Phase 2: move B's files to A's location, re-key B's meta to A's slot
                if (fbDataPath != null && File.Exists(fbDataPath) && faDataPath != null)
                {
                    File.Move(fbDataPath, faDataPath);
                    // Re-key B's meta using the original B path (for srcSlot) -> A path (for dstSlot)
                    if (fb.MetaFile != null && File.Exists(fb.MetaFile) && fa.MetaFile != null)
                        CopyMetaFile(fbDataPath, fb.MetaFile, faDataPath, fa.MetaFile, platform);
                }
                // Delete B's meta separately (it was either copied above, or B had no meta)
                if (fb.MetaFile != null && File.Exists(fb.MetaFile))
                    File.Delete(fb.MetaFile);

                // Phase 3: move A's original files (from temp) to B's location, re-key to B's slot
                if (File.Exists(tmpData) && fbDataPath != null)
                {
                    File.Move(tmpData, fbDataPath);
                    // Re-key A's meta using the original A path (for srcSlot) -> B path (for dstSlot)
                    if (File.Exists(tmpMeta) && fb.MetaFile != null && faDataPath != null)
                        CopyMetaFile(faDataPath, tmpMeta, fbDataPath, fb.MetaFile, platform);
                }
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Delete all files in a save slot (auto save and manual save).
    /// </summary>
    public static void DeleteSlot(string saveDirectory, int slotIndex,
        SaveFileManager.Platform platform)
    {
        foreach (var files in GetAllSlotFiles(saveDirectory, slotIndex, platform))
        {
            if (files.DataFile != null && File.Exists(files.DataFile))
                File.Delete(files.DataFile);
            if (files.MetaFile != null && File.Exists(files.MetaFile))
                File.Delete(files.MetaFile);
        }
    }

    // === Meta file helpers ===

    /// <summary>
    /// Copy a meta file from source to destination.
    /// For Steam/GOG the meta is re-encrypted with the destination storage slot key.
    /// For Switch the slot-index field at offset 12 is updated.
    /// For other platforms the file is copied verbatim.
    /// </summary>
    private static void CopyMetaFile(
        string srcDataFile, string srcMetaFile,
        string dstDataFile, string dstMetaFile,
        SaveFileManager.Platform platform)
    {
        if (platform is SaveFileManager.Platform.Steam or SaveFileManager.Platform.GOG)
        {
            ReKeyMetaFile(srcDataFile, srcMetaFile, dstDataFile, dstMetaFile, platform);
        }
        else if (platform == SaveFileManager.Platform.Switch)
        {
            // Copy then patch the slot-index field at byte offset 12.
            byte[] bytes = File.ReadAllBytes(srcMetaFile);
            int dstIdx = ExtractSwitchManifestIndex(dstDataFile);
            if (dstIdx >= 0 && bytes.Length >= 16)
            {
                byte[] idx = BitConverter.GetBytes(dstIdx);
                Buffer.BlockCopy(idx, 0, bytes, 12, 4);
            }
            File.WriteAllBytes(dstMetaFile, bytes);
        }
        else
        {
            File.Copy(srcMetaFile, dstMetaFile, true);
        }
    }

    /// <summary>
    /// Decrypt a Steam/GOG meta file with the source storage slot key and
    /// re-encrypt it with the destination storage slot key, writing the result
    /// to <paramref name="dstMetaFile"/>.
    /// Falls back to a plain file copy if decryption fails.
    /// </summary>
    private static void ReKeyMetaFile(
        string srcDataFile, string srcMetaFile,
        string dstDataFile, string dstMetaFile,
        SaveFileManager.Platform platform)
    {
        if (platform is not (SaveFileManager.Platform.Steam or SaveFileManager.Platform.GOG))
        {
            File.Copy(srcMetaFile, dstMetaFile, true);
            return;
        }

        int srcSlot = StorageSlotFromFileName(srcDataFile);
        int dstSlot = StorageSlotFromFileName(dstDataFile);

        if (srcSlot == dstSlot)
        {
            File.Copy(srcMetaFile, dstMetaFile, true);
            return;
        }

        byte[] raw = File.ReadAllBytes(srcMetaFile);
        if (raw.Length < 4)
        {
            File.Copy(srcMetaFile, dstMetaFile, true);
            return;
        }

        uint[] encrypted  = MetaFileWriter.BytesToUInts(raw);
        int    iterations = raw.Length == MetaFileWriter.STEAM_META_LENGTH_VANILLA ? 8 : 6;
        uint[] decrypted  = MetaCrypto.Decrypt(encrypted, srcSlot, iterations);

        if (decrypted[0] != MetaFileWriter.META_HEADER)
        {
            // Decryption failed – copy verbatim as a best-effort fallback
            File.Copy(srcMetaFile, dstMetaFile, true);
            return;
        }

        uint[] reKeyed = MetaCrypto.Encrypt(decrypted, dstSlot, iterations);
        File.WriteAllBytes(dstMetaFile, MetaFileWriter.UIntsToBytes(reKeyed));
    }

    /// <summary>
    /// Derive the Switch manifest index from a savedata file path.
    /// e.g. "savedata03.hg" -> 3, "savedata00.hg" -> 0.
    /// Returns -1 if the name cannot be parsed.
    /// </summary>
    private static int ExtractSwitchManifestIndex(string dataFilePath)
    {
        string name = Path.GetFileNameWithoutExtension(dataFilePath);
        const string prefix = "savedata";
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(name.AsSpan(prefix.Length),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int idx))
            return idx;
        return -1;
    }

    /// <summary>
    /// Transfer a save file from one platform to another.
    /// Loads the source JSON, rewrites ownership UIDs for the destination platform,
    /// and saves in the destination format.
    /// </summary>
    /// <param name="sourceFilePath">Source save file path.</param>
    /// <param name="destDirectory">Destination save directory.</param>
    /// <param name="destSlotIndex">Destination slot index.</param>
    /// <param name="destPlatform">Destination platform type.</param>
    /// <param name="transferOptions">Options controlling what data to transfer.</param>
    public static void TransferCrossPlatform(string sourceFilePath, string destDirectory,
        int destSlotIndex, SaveFileManager.Platform destPlatform, TransferOptions? transferOptions = null)
    {
        var options = transferOptions ?? new TransferOptions();

        // Load source save
        var saveData = SaveFileManager.LoadSaveFile(sourceFilePath);

        // Update Platform token in the save
        string platformToken = GetPlatformToken(destPlatform);
        SetJsonValueByPath(saveData, "Platform", platformToken);

        // Transfer ownership references
        if (options.TransferBases)
            TransferBaseOwnership(saveData, options);

        if (options.TransferDiscoveries)
            TransferDiscoveryOwnership(saveData, options);

        if (options.TransferSettlements)
            TransferSettlementOwnership(saveData, options);

        if (options.TransferByteBeat)
            TransferByteBeatOwnership(saveData, options);

        // Save to destination
        var destFiles = GetSlotFiles(destDirectory, destSlotIndex, destPlatform);
        if (destFiles.DataFile == null)
            throw new InvalidOperationException("Cannot determine destination file path.");

        SaveFileManager.SaveToFile(destFiles.DataFile, saveData, compress: true);

        // Write platform-appropriate meta file
        WriteMetaForPlatform(destFiles, saveData, destPlatform, destSlotIndex);
    }

    /// <summary>
    /// Rewrite ownership UIDs in base objects.
    /// Bases have Owner.UID, Owner.LID, Owner.USN, Owner.PTK fields that need
    /// to match the destination platform user.
    /// </summary>
    private static void TransferBaseOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk PersistentPlayerBases array
        var bases = GetJsonArray(saveData, "PlayerStateData.PersistentPlayerBases");
        if (bases == null) return;

        for (int i = 0; i < bases.Length; i++)
        {
            if (bases.Get(i) is not JsonObject baseObj) continue;

            var owner = baseObj.Get("Owner") as JsonObject;
            if (owner == null) continue;

            // Only transfer bases owned by the source user (match UID)
            string? ownerUid = owner.Get("UID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && ownerUid != options.SourceUID)
                continue;

            // Rewrite ownership
            RewriteOwnership(owner, options);
        }
    }

    private static void TransferDiscoveryOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk DiscoveryManagerData.DiscoveryData-v1.Store.Record array
        var record = GetJsonArray(saveData, "DiscoveryManagerData.DiscoveryData-v1.Store.Record");
        if (record == null) return;

        for (int i = 0; i < record.Length; i++)
        {
            if (record.Get(i) is not JsonObject discoveryObj) continue;

            var ows = discoveryObj.Get("OWS") as JsonObject;
            if (ows == null) continue;

            string? ownerUid = ows.Get("UID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && ownerUid != options.SourceUID)
                continue;

            RewriteOwnership(ows, options);
        }
    }

    private static void TransferSettlementOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk PlayerStateData.SettlementStatesV2 array
        var settlements = GetJsonArray(saveData, "PlayerStateData.SettlementStatesV2");
        if (settlements == null) return;

        for (int i = 0; i < settlements.Length; i++)
        {
            if (settlements.Get(i) is not JsonObject settlementObj) continue;

            var owner = settlementObj.Get("Owner") as JsonObject;
            if (owner == null) continue;

            string? ownerUid = owner.Get("UID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && ownerUid != options.SourceUID)
                continue;

            RewriteOwnership(owner, options);
        }
    }

    private static void TransferByteBeatOwnership(JsonObject saveData, TransferOptions options)
    {
        // Walk PlayerStateData.ByteBeatLibrary.MySongs array
        var songs = GetJsonArray(saveData, "PlayerStateData.ByteBeatLibrary.MySongs");
        if (songs == null) return;

        for (int i = 0; i < songs.Length; i++)
        {
            if (songs.Get(i) is not JsonObject songObj) continue;

            string? authorId = songObj.Get("AuthorOnlineID") as string;
            if (!string.IsNullOrEmpty(options.SourceUID) && authorId != options.SourceUID)
                continue;

            if (!string.IsNullOrEmpty(options.DestUID))
                songObj.Set("AuthorOnlineID", options.DestUID);
            if (!string.IsNullOrEmpty(options.DestUSN))
                songObj.Set("AuthorUsername", options.DestUSN);
            if (!string.IsNullOrEmpty(options.DestPTK))
                songObj.Set("AuthorPlatform", options.DestPTK);
        }
    }

    private static void RewriteOwnership(JsonObject ownerObj, TransferOptions options)
    {
        if (!string.IsNullOrEmpty(options.DestUID))
            ownerObj.Set("UID", options.DestUID);

        string? lid = ownerObj.Get("LID") as string;
        if (!string.IsNullOrEmpty(lid) && !string.IsNullOrEmpty(options.DestLID))
            ownerObj.Set("LID", options.DestLID);

        string? usn = ownerObj.Get("USN") as string;
        if (!string.IsNullOrEmpty(usn) && !string.IsNullOrEmpty(options.DestUSN))
            ownerObj.Set("USN", options.DestUSN);

        if (!string.IsNullOrEmpty(options.DestPTK))
            ownerObj.Set("PTK", options.DestPTK);
    }

    /// <summary>
    /// Convert a save slot index to the persistent storage slot used in meta encryption.
    /// Slot 0 = AccountData (storage slot 0), slots 1+ = save data (storage slot 2+slotIndex).
    /// The gap at slot 1 is reserved for Settings data.
    /// </summary>
    internal static int SlotIndexToStorageSlot(int slotIndex)
    {
        return slotIndex == 0 ? 0 : 2 + slotIndex;
    }

    /// <summary>
    /// Derive the persistent storage slot from a save file name.
    /// NMS uses the following convention:
    /// <list type="bullet">
    ///   <item><c>accountdata.hg</c> -> storage slot 0</item>
    ///   <item><c>save.hg</c> -> storage slot 2 (first manual save)</item>
    ///   <item><c>saveN.hg</c> (N ≥ 2) -> storage slot N + 1</item>
    /// </list>
    /// The meta encryption key depends on the storage slot, so using the
    /// wrong slot produces a garbled meta file that the game cannot read.
    /// </summary>
    internal static int StorageSlotFromFileName(string filePath)
    {
        string name = Path.GetFileNameWithoutExtension(filePath);

        if (name.Equals("accountdata", StringComparison.OrdinalIgnoreCase))
            return 0;

        // save.hg -> storage slot 2
        if (name.Equals("save", StringComparison.OrdinalIgnoreCase))
            return 2;

        // saveN.hg -> storage slot N + 1  (save2.hg -> 3, save3.hg -> 4, etc.)
        if (name.StartsWith("save", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(name.AsSpan(4), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int n) && n >= 2)
            return n + 1;

        // Unknown file name - fall back to slot 2 as a safe default
        return 2;
    }
}
