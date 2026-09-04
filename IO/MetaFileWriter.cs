using System.Text;

namespace NMSE.IO;

/// <summary>
/// Metadata about a save file for meta file writing.
/// </summary>
public class SaveMetaInfo
{
    /// <summary>Gets or sets the save format base version number.</summary>
    public int BaseVersion { get; set; }
    /// <summary>Gets or sets the game mode identifier (e.g., 1=Normal, 2=Survival).</summary>
    public int GameMode { get; set; }
    /// <summary>Gets or sets the expedition/season number, or 0 if none.</summary>
    public int Season { get; set; }
    /// <summary>Gets or sets the total play time in seconds.</summary>
    public ulong TotalPlayTime { get; set; }
    /// <summary>Gets or sets the player-assigned save name.</summary>
    public string? SaveName { get; set; }
    /// <summary>Gets or sets the auto-generated save summary text.</summary>
    public string? SaveSummary { get; set; }
    /// <summary>Gets or sets the difficulty preset identifier.</summary>
    public int DifficultyPreset { get; set; }
    /// <summary>Gets or sets the difficulty preset type tag string (e.g., "Normal", "Custom").</summary>
    public string? DifficultyPresetTag { get; set; }
}

/// <summary>
/// Writes platform-specific meta (companion) files alongside save data files.
///
/// Meta files contain save metadata (game mode, season, play time, save name, etc.)
/// used by the game's save slot browser. Without valid meta files, the game cannot
/// display save slot previews and may refuse to load saves on some platforms.
/// </summary>
public static class MetaFileWriter
{
    // Magic header value found at the start of Steam/GOG meta files (decrypted).
    /// <summary>Steam/GOG meta file magic header (0xEEEEEEBE).</summary>
    internal const uint META_HEADER = 0xEEEEEEBE; // Steam/GOG: 4,008,636,094

    // Magic header for Switch meta files.
    /// <summary>Switch meta file magic header (0xCA55E77E).</summary>
    internal const uint META_HEADER_SWITCH = 0xCA55E77E;  // 3,394,627,454

    // Magic header for PS4 HTOS streaming meta files.
    /// <summary>PS4 HTOS manifest magic header (0xCA55E77E).</summary>
    internal const uint META_HEADER_PS4 = 0xCA55E77E;     // 3,394,627,454

    // Meta format version identifiers - must match NMS game values (2001-2004).
    /// <summary>Meta format version for pre-Frontiers saves.</summary>
    internal const uint META_FORMAT_1 = 2001; // Pre-Frontiers
    /// <summary>Meta format version for Frontiers 3.60+ saves.</summary>
    internal const uint META_FORMAT_2 = 2002; // Frontiers 3.60+
    /// <summary>Meta format version for Worlds Part I 5.00+ saves.</summary>
    internal const uint META_FORMAT_3 = 2003; // Worlds Part I 5.00+
    /// <summary>Meta format version for Worlds Part II 5.50+ saves.</summary>
    internal const uint META_FORMAT_4 = 2004; // Worlds Part II 5.50+

    // Steam/GOG meta file sizes (in bytes)
    /// <summary>Steam/GOG meta file size for vanilla saves (104 bytes).</summary>
    internal const int STEAM_META_LENGTH_VANILLA = 104;    // 0x68
    /// <summary>Steam/GOG meta file size for Waypoint saves (360 bytes).</summary>
    internal const int STEAM_META_LENGTH_WAYPOINT = 360;   // 0x168
    /// <summary>Steam/GOG meta file size for Worlds Part I saves (384 bytes).</summary>
    internal const int STEAM_META_LENGTH_WORLDS_I = 384;   // 0x180
    /// <summary>Steam/GOG meta file size for Worlds Part II saves (432 bytes).</summary>
    internal const int STEAM_META_LENGTH_WORLDS_II = 432;  // 0x1B0

    // Switch meta file sizes (in bytes)
    /// <summary>Switch meta file size for vanilla saves (100 bytes).</summary>
    internal const int SWITCH_META_LENGTH_VANILLA = 100;   // 0x64
    /// <summary>Switch meta file size for Waypoint saves (356 bytes).</summary>
    internal const int SWITCH_META_LENGTH_WAYPOINT = 356;  // 0x164
    /// <summary>Switch meta file size for Worlds Part I saves (372 bytes).</summary>
    internal const int SWITCH_META_LENGTH_WORLDS_I = 372;  // 0x174
    /// <summary>Switch meta file size for Worlds Part II saves (380 bytes).</summary>
    internal const int SWITCH_META_LENGTH_WORLDS_II = 380; // 0x17C

    // PS4 HTOS manifest file sizes (in bytes)
    /// <summary>PS4 HTOS manifest file size for vanilla saves (100 bytes).</summary>
    internal const int PS4_META_LENGTH_VANILLA = 100;      // 0x64
    /// <summary>PS4 HTOS manifest file size for Waypoint saves (356 bytes).</summary>
    internal const int PS4_META_LENGTH_WAYPOINT = 356;     // 0x164
    /// <summary>PS4 HTOS manifest file size for Worlds Part I saves (372 bytes).</summary>
    internal const int PS4_META_LENGTH_WORLDS_I = 372;     // 0x174
    /// <summary>PS4 HTOS manifest file size for Worlds Part II saves (380 bytes).</summary>
    internal const int PS4_META_LENGTH_WORLDS_II = 380;    // 0x17C

    // Offsets for Steam/GOG manifest layout
    private const int STEAM_META_AFTER_VANILLA = 84;       // 0x54 - end of known vanilla fields
    private const int STEAM_META_BEFORE_NAME = 88;         // 0x58 - start of save name
    private const int STEAM_META_BEFORE_SUMMARY = 216;     // 0x58 + 128
    private const int STEAM_META_BEFORE_DIFFICULTY = 344;  // 0x58 + 128 + 128
    // Worlds Part I/II extended offsets (after difficulty at 344)
    private const int STEAM_META_SLOT_ID = 348;            // 0x15C - slot identifier (8 bytes)
    private const int STEAM_META_TIMESTAMP = 356;          // 0x164 - unix timestamp (4 bytes)
    private const int STEAM_META_FORMAT_COPY = 360;        // 0x168 - copy of meta format (4 bytes)
    private const int STEAM_META_DIFFICULTY_TAG = 364;     // 0x16C - difficulty preset type string (64 bytes)

    // Offsets for Switch manifest layout
    private const int SWITCH_META_BEFORE_NAME = 40;        // start of save name field (after 40 bytes of header fields)
    private const int SWITCH_META_BEFORE_SUMMARY = 168;    // 40 + 128
    private const int SWITCH_META_BEFORE_DIFFICULTY = 296; // 40 + 128 + 128

    // Offsets for PS4 HTOS manifest layout
    private const int PS4_META_BEFORE_NAME = 40;           // start of save name field (after 40 bytes of header fields)
    private const int PS4_META_BEFORE_SUMMARY = 168;       // 40 + 128
    private const int PS4_META_BEFORE_DIFFICULTY = 296;    // 40 + 128 + 128

    // Helpers

    private static void WriteSaveNameAndSummary(BinaryWriter writer, SaveMetaInfo info,
        MemoryStream ms, int difficultyOffset, int bufferLen)
    {
        // Write save name (128 bytes, null-terminated)
        byte[] nameBytes = GetNullTerminatedBytes(info.SaveName ?? "", 128);
        writer.Write(nameBytes);

        // Write save summary (128 bytes, null-terminated)
        byte[] summaryBytes = GetNullTerminatedBytes(info.SaveSummary ?? "", 128);
        writer.Write(summaryBytes);

        // Difficulty preset
        ms.Position = difficultyOffset;
        if (bufferLen >= difficultyOffset + 4)
            writer.Write((uint)info.DifficultyPreset);
        else if (bufferLen >= difficultyOffset + 1)
            writer.Write((byte)info.DifficultyPreset);
    }

    private static byte[] GetNullTerminatedBytes(string text, int maxBytes)
    {
        byte[] result = new byte[maxBytes];
        byte[] encoded = Encoding.UTF8.GetBytes(text);
        int copyLen = Math.Min(encoded.Length, maxBytes - 1);
        Buffer.BlockCopy(encoded, 0, result, 0, copyLen);
        return result;
    }

    internal static string GetSteamMetaPath(string saveFilePath)
    {
        string dir = Path.GetDirectoryName(saveFilePath)!;
        string name = Path.GetFileName(saveFilePath);
        // save.hg -> mf_save.hg, save2.hg -> mf_save2.hg
        return Path.Combine(dir, "mf_" + name);
    }

    internal static string GetSwitchMetaPath(string saveFilePath, int metaIndex)
    {
        string dir = Path.GetDirectoryName(saveFilePath)!;
        return Path.Combine(dir, $"manifest{metaIndex:D2}.hg");
    }

    private static uint GetMetaFormat(int baseVersion)
    {
        // Base version thresholds
        if (baseVersion >= 4145) return META_FORMAT_4; // Worlds Part II 5.50+
        if (baseVersion >= 4135) return META_FORMAT_3; // Worlds Part I 5.00+
        if (baseVersion >= 4115) return META_FORMAT_2; // Frontiers 3.60+
        return META_FORMAT_1;
    }

    private static int GetSteamMetaLength(uint metaFormat)
    {
        return metaFormat switch
        {
            >= META_FORMAT_4 => STEAM_META_LENGTH_WORLDS_II,
            >= META_FORMAT_3 => STEAM_META_LENGTH_WORLDS_I,
            >= META_FORMAT_2 => STEAM_META_LENGTH_WAYPOINT,
            _ => STEAM_META_LENGTH_VANILLA,
        };
    }

    private static int GetSwitchMetaLength(uint metaFormat)
    {
        return metaFormat switch
        {
            >= META_FORMAT_4 => SWITCH_META_LENGTH_WORLDS_II,
            >= META_FORMAT_3 => SWITCH_META_LENGTH_WORLDS_I,
            >= META_FORMAT_2 => SWITCH_META_LENGTH_WAYPOINT,
            _ => SWITCH_META_LENGTH_VANILLA,
        };
    }

    private static int GetPs4MetaLength(uint metaFormat)
    {
        return metaFormat switch
        {
            >= META_FORMAT_4 => PS4_META_LENGTH_WORLDS_II,
            >= META_FORMAT_3 => PS4_META_LENGTH_WORLDS_I,
            >= META_FORMAT_2 => PS4_META_LENGTH_WAYPOINT,
            _ => PS4_META_LENGTH_VANILLA,
        };
    }

    /// <summary>
    /// Reads the meta format and base version from an adjacent game-save manifest in
    /// the same directory (manifest02.hg, manifest04.hg, ... manifest12.hg).
    /// Game-save manifests carry the platform's software version as their base version,
    /// which is not stored anywhere in the save JSON (the JSON "Version"/"F2P" field is
    /// the save format version and is always higher).  Returns zeros when no sibling
    /// manifest is found.
    /// </summary>
    /// <param name="dir">Directory containing the manifest files.</param>
    private static (uint Format, uint BaseVersion) ReadSiblingManifestInfo(string dir)
    {
        for (int i = 2; i <= 12; i += 2)
        {
            string path = Path.Combine(dir, $"manifest{i:D2}.hg");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length >= 24)
                    return (BitConverter.ToUInt32(bytes, 4), BitConverter.ToUInt32(bytes, 20));
            }
        }
        return (0, 0);
    }

    /// <summary>
    /// Reads the meta format from an adjacent game-save manifest in the same directory.
    /// Game-save manifests are named manifest02.hg, manifest04.hg, manifest06.hg ... manifest12.hg.
    /// The account manifest (manifest00.hg) must carry the same format value, but the
    /// account JSON Version field uses a different numbering scheme so it cannot be
    /// derived via <see cref="GetMetaFormat"/> directly.
    /// </summary>
    /// <param name="dir">Directory containing the manifest files.</param>
    /// <param name="fallback">Value to return when no sibling manifest is found.</param>
    private static uint ReadSiblingManifestFormat(string dir, uint fallback)
    {
        var info = ReadSiblingManifestInfo(dir);
        return info.Format != 0 ? info.Format : fallback;
    }

    private static int GameModeStringToInt(string mode) => mode switch
    {
        "Normal" => 1,
        "Survival" => 2,
        "Permadeath" => 3,
        "Creative" => 4,
        "Custom" => 5,
        "Seasonal" => 6,
        "Relaxed" => 7,
        "Hardcore" => 8,
        _ => 0
    };

    internal static uint[] BytesToUInts(byte[] bytes)
    {
        int count = bytes.Length / 4;
        uint[] result = new uint[count];
        Buffer.BlockCopy(bytes, 0, result, 0, count * 4);
        return result;
    }

    internal static byte[] UIntsToBytes(uint[] uints)
    {
        byte[] result = new byte[uints.Length * 4];
        Buffer.BlockCopy(uints, 0, result, 0, result.Length);
        return result;
    }

    /// <summary>
    /// Write a Steam/GOG meta file (mf_*.hg) next to the save data file.
    /// Note: Steam/GOG account data (accountdata.hg) does not use a meta file.
    /// The caller must ensure this is not called for account data by passing
    /// writeMeta=false in SaveToFile. Write meta for Steam account data even
    /// though it is not required for the game to function (it accommodates).
    /// </summary>
    /// <param name="saveFilePath">Path to the save data file (e.g., save.hg).</param>
    /// <param name="compressedData">The compressed save data bytes that were written.</param>
    /// <param name="decompressedSize">Size of the decompressed JSON data + null terminator.</param>
    /// <param name="info">Save metadata extracted from JSON.</param>
    /// <param name="storageSlot">Persistent storage slot index (0=account, 2+=saves).</param>
    public static void WriteSteamMeta(string saveFilePath, byte[] compressedData, uint decompressedSize, SaveMetaInfo info, int storageSlot)
    {
        string metaPath = GetSteamMetaPath(saveFilePath);

        // Preserve the BaseVersion from the existing meta file if available.
        // The meta BaseVersion is the game's software version at save time, which
        // may differ from the save-format "Version" field in the JSON (F2P).
        // Writing a Version higher than the game expects triggers the
        // "Cross-Save Version Incompatible" error on load.
        int metaBaseVersion = info.BaseVersion;
        uint[]? existingMeta = ReadSteamMeta(saveFilePath, storageSlot);
        if (existingMeta != null && existingMeta[0] == META_HEADER && existingMeta.Length >= 18)
        {
            int existingVersion = (int)existingMeta[17]; // offset 68 = uint index 17
            if (existingVersion > 0)
                metaBaseVersion = existingVersion;

            // Also preserve the slot identifier from the existing meta if present
            // (offset 348 = uint index 87+88 as a ulong)
        }

        uint metaFormat = GetMetaFormat(metaBaseVersion);
        int bufferLen = GetSteamMetaLength(metaFormat);
        byte[] buffer = new byte[bufferLen];

        using var ms = new MemoryStream(buffer);
        using var writer = new BinaryWriter(ms);

        writer.Write(META_HEADER);  // offset 0, 4 bytes: magic header
        writer.Write(metaFormat);   // offset 4, 4 bytes: meta format version

        if (metaFormat >= META_FORMAT_2)
        {
            // Hashes: Frontiers+ (META_FORMAT_2+) does not use
            // SpookyHash or SHA256 in the meta file. Write zeros.
            writer.Write(new byte[48]); // offset 8, 48 bytes: hash placeholder (zeros)

            writer.Write(decompressedSize); // offset 56, 4 bytes: decompressed size

            // Compressed size (used from Worlds Part I 5.00)
            if (metaFormat >= META_FORMAT_3)
                writer.Write((uint)compressedData.Length); // offset 60, 4 bytes: compressed size
            else
                writer.Write((uint)0); // offset 60, 4 bytes: compressed size placeholder

            writer.Write((uint)0); // offset 64, 4 bytes: profile hash placeholder

            writer.Write(metaBaseVersion);       // offset 68, 4 bytes: base version
            writer.Write((ushort)info.GameMode); // offset 72, 2 bytes: game mode
            writer.Write((ushort)info.Season);   // offset 74, 2 bytes: season
            writer.Write(info.TotalPlayTime);    // offset 76, 8 bytes: total play time

            // Offset 84: repeat decompressed size (matches game-written layout)
            writer.Write(decompressedSize); // offset 84, 4 bytes: decompressed size (duplicate)

            // Waypoint extensions: save name, save summary, difficulty
            // Position at STEAM_META_BEFORE_NAME (88)
            ms.Position = STEAM_META_BEFORE_NAME;
            WriteSaveNameAndSummary(writer, info, ms, STEAM_META_BEFORE_DIFFICULTY, bufferLen);

            // Worlds Part I/II extensions (format >= 2003): slot identifier, timestamp, format copy
            if (metaFormat >= META_FORMAT_3)
            {
                // Preserve existing slot identifier if available
                ulong slotId = 0;
                if (existingMeta != null && existingMeta.Length >= 89)
                {
                    slotId = existingMeta[87] | ((ulong)existingMeta[88] << 32);
                }
                ms.Position = STEAM_META_SLOT_ID;
                writer.Write(slotId); // slot identifier (8 bytes)

                ms.Position = STEAM_META_TIMESTAMP;
                writer.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // timestamp (4 bytes)

                ms.Position = STEAM_META_FORMAT_COPY;
                writer.Write(metaFormat); // copy of meta format (4 bytes)
            }

            // Worlds Part II extension (format >= 2004): difficulty tag string
            if (metaFormat >= META_FORMAT_4)
            {
                ms.Position = STEAM_META_DIFFICULTY_TAG;
                byte[] tagBytes = GetNullTerminatedBytes(info.DifficultyPresetTag ?? "", 64);
                writer.Write(tagBytes);
            }
        }
        else
        {
            // Pre-Frontiers format (META_FORMAT_1):
            // Write real SpookyHash + SHA256 hashes of the compressed data.
            // Pre-Frontiers meta includes actual hashes.
            byte[] metaHashes = MetaCrypto.ComputeMetaHashes(compressedData);
            writer.Write(metaHashes); // 48 bytes: spookyHash1(8) + spookyHash2(8) + sha256(32)
            writer.Write(decompressedSize);
        }

        // Encrypt
        int iterations = metaFormat <= META_FORMAT_1 ? 8 : 6;
        uint[] uintData = BytesToUInts(buffer);
        uint[] encrypted = MetaCrypto.Encrypt(uintData, storageSlot, iterations);
        byte[] encryptedBytes = UIntsToBytes(encrypted);

        File.WriteAllBytes(metaPath, encryptedBytes);
    }

    /// <summary>
    /// Write a Switch meta file (manifest*.hg) next to the save data file.
    /// Account meta (metaIndex 0) only updates the decompressed size at offset 8,
    /// preserving existing data. Save meta writes all fields from scratch.
    /// </summary>
    public static void WriteSwitchMeta(string saveFilePath, uint decompressedSize, SaveMetaInfo info, int metaIndex)
    {
        // Derive the manifest index from the save data file name (savedata03.hg gives
        // index 3).  This ensures the correct companion manifest (manifest03.hg) is
        // always written regardless of which slot index the caller passes, matching
        // the PS4 streaming writer.  Non-savedata names (e.g. accountdata.hg) fall
        // back to the caller's index.
        string fname = Path.GetFileNameWithoutExtension(saveFilePath);
        const string sdPrefix = "savedata";
        if (fname.StartsWith(sdPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(fname.AsSpan(sdPrefix.Length),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int derivedIdx))
        {
            metaIndex = derivedIdx;
        }

        string metaPath = GetSwitchMetaPath(saveFilePath, metaIndex);

        // Account meta (metaIndex 0): only write decompressed size at offsets 8 and 36.
        // All other bytes are preserved from the existing file.
        if (metaIndex == 0)
        {
            uint metaFormat = GetMetaFormat(info.BaseVersion);
            int bufLen = GetSwitchMetaLength(metaFormat);
            byte[] buffer;
            if (File.Exists(metaPath))
            {
                buffer = File.ReadAllBytes(metaPath);
                if (buffer.Length < bufLen)
                    Array.Resize(ref buffer, bufLen);
            }
            else
            {
                buffer = new byte[bufLen];
            }
            using var ms = new MemoryStream(buffer);
            using var writer = new BinaryWriter(ms);
            ms.Position = 8;
            writer.Write(decompressedSize);  // offset 8, 4 bytes: decompressed size
            ms.Position = 36;
            writer.Write(decompressedSize);  // offset 36, 4 bytes: decompressed size (duplicate)
            File.WriteAllBytes(metaPath, buffer);
            return;
        }

        // Save meta: write all fields.
        {
            // Preserve the base version and format from the existing manifest when
            // present.  The manifest base version is the game's software version at
            // save time (e.g. 4215) and is lower than the save-format "Version" field
            // in the JSON (e.g. 4727).  Overwriting it with the JSON Version makes the
            // save appear newer than the platform's deployed build, which triggers the
            // "Cross-Save Version Incompatible" error on load (same fix as Steam meta).
            // When the slot has no manifest of its own (e.g. writing into a new slot),
            // fall back to the base version of a sibling game-save manifest, which
            // carries the same platform software version.
            uint existingFormat = 0;
            uint existingBaseVersion = 0;
            if (File.Exists(metaPath))
            {
                byte[] existing = File.ReadAllBytes(metaPath);
                if (existing.Length >= 24 && BitConverter.ToUInt32(existing, 0) == META_HEADER_SWITCH)
                {
                    existingFormat = BitConverter.ToUInt32(existing, 4);
                    existingBaseVersion = BitConverter.ToUInt32(existing, 20);
                }
            }
            if (existingFormat == 0 || existingBaseVersion == 0)
            {
                var sibling = ReadSiblingManifestInfo(Path.GetDirectoryName(metaPath)!);
                if (existingFormat == 0) existingFormat = sibling.Format;
                if (existingBaseVersion == 0) existingBaseVersion = sibling.BaseVersion;
            }

            uint derivedFormat = GetMetaFormat(info.BaseVersion);
            uint metaFormat = existingFormat > derivedFormat ? existingFormat : derivedFormat;
            int bufferLen = GetSwitchMetaLength(metaFormat);
            byte[] buffer = new byte[bufferLen];

            using var ms = new MemoryStream(buffer);
            using var writer = new BinaryWriter(ms);

            writer.Write(META_HEADER_SWITCH);                              // offset 0, 4 bytes: magic header
            writer.Write(metaFormat);                                      // offset 4, 4 bytes: meta format version
            writer.Write(decompressedSize);                                // offset 8, 4 bytes: decompressed size
            writer.Write(metaIndex);                                       // offset 12, 4 bytes: manifest index
            writer.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // offset 16, 4 bytes: unix timestamp
            writer.Write(existingBaseVersion != 0 ? existingBaseVersion : (uint)info.BaseVersion); // offset 20, 4 bytes: base version
            writer.Write((ushort)info.GameMode);                           // offset 24, 2 bytes: game mode
            writer.Write((ushort)info.Season);                             // offset 26, 2 bytes: season
            writer.Write(info.TotalPlayTime);                              // offset 28, 8 bytes: total play time
            writer.Write(decompressedSize);                                // offset 36, 4 bytes: decompressed size (duplicate)
            // Total so far: 40 bytes (= SWITCH_META_BEFORE_NAME)

            if (bufferLen > SWITCH_META_BEFORE_NAME)
            {
                ms.Position = SWITCH_META_BEFORE_NAME;
                WriteSaveNameAndSummary(writer, info, ms, SWITCH_META_BEFORE_DIFFICULTY, bufferLen);
            }

            File.WriteAllBytes(metaPath, buffer);
        }
    }

    /// <summary>
    /// Write a PS4 HTOS streaming manifest file (manifest*.hg) next to the save data file.
    /// The manifest file number is derived from the save file name (savedata02.hg writes manifest02.hg)
    /// rather than from the <paramref name="metaIndex"/> parameter so that the correct companion
    /// manifest is always written regardless of which slot-combo index the caller passes.
    /// </summary>
    public static void WritePlaystationStreamingMeta(string saveFilePath, uint decompressedSize, SaveMetaInfo info, int metaIndex)
    {
        string dir = Path.GetDirectoryName(saveFilePath)!;

        // Derive the manifest index from the data file name (savedata02.hg gives index 2).
        // This ensures manifest02.hg is written even when metaIndex=0 is passed.
        string fname = Path.GetFileNameWithoutExtension(saveFilePath);
        const string sdPrefix = "savedata";
        if (fname.StartsWith(sdPrefix, StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(fname.AsSpan(sdPrefix.Length),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int derivedIdx))
        {
            metaIndex = derivedIdx;
        }

        string metaPath = Path.Combine(dir, $"manifest{metaIndex:D2}.hg");

        uint metaFormat = GetMetaFormat(info.BaseVersion);
        int bufferLen = GetPs4MetaLength(metaFormat);

        // For the account data file (metaIndex 0): preserve existing bytes where present
        // and only update the header fields. Account manifests don't carry save-name or
        // difficulty data so we leave the rest of the buffer as-is.
        if (metaIndex == 0)
        {
            // The account JSON uses a different Version numbering scheme (e.g. 4098) compared
            // to game-save slots (e.g. 4727). Feeding the account version through GetMetaFormat
            // always returns META_FORMAT_1 (2001) regardless of the actual game version, which
            // is wrong. The PS4 expects the account manifest to carry the same format value as
            // the adjacent game-save manifests. Read the format from an existing sibling
            // game-save manifest (manifest02.hg, manifest04.hg, etc.) to get the correct value,
            // falling back to the info-derived format only when no sibling is present yet.
            uint accountMetaFormat = ReadSiblingManifestFormat(dir, metaFormat);
            bufferLen = GetPs4MetaLength(accountMetaFormat);

            byte[] buffer;
            if (File.Exists(metaPath))
            {
                buffer = File.ReadAllBytes(metaPath);
                if (buffer.Length < bufferLen)
                    Array.Resize(ref buffer, bufferLen);
            }
            else
            {
                buffer = new byte[bufferLen];
            }
            using var ms = new MemoryStream(buffer);
            using var writer = new BinaryWriter(ms);
            writer.Write(META_HEADER_PS4);    // offset 0, 4 bytes: magic header
            writer.Write(accountMetaFormat);  // offset 4, 4 bytes: meta format version (from sibling game-save manifest)
            writer.Write(decompressedSize);   // offset 8, 4 bytes: decompressed size
            ms.Position = 36;
            writer.Write(decompressedSize);   // offset 36, 4 bytes: decompressed size (duplicate)
            File.WriteAllBytes(metaPath, buffer);
            return;
        }

        // Save-slot manifest: write all fields from scratch.
        {
            // Preserve the base version and format from the existing manifest when
            // present.  The manifest base version is the game's software version at
            // save time and is lower than the save-format "Version" field in the
            // JSON.  Overwriting it with the JSON Version makes the save appear newer
            // than the platform's deployed build, which triggers the
            // "Cross-Save Version Incompatible" error on load (same fix as Steam meta).
            // When the slot has no manifest of its own (e.g. writing into a new slot),
            // fall back to the base version of a sibling game-save manifest, which
            // carries the same platform software version.
            uint existingFormat = 0;
            uint existingBaseVersion = 0;
            if (File.Exists(metaPath))
            {
                byte[] existing = File.ReadAllBytes(metaPath);
                if (existing.Length >= 24 && BitConverter.ToUInt32(existing, 0) == META_HEADER_PS4)
                {
                    existingFormat = BitConverter.ToUInt32(existing, 4);
                    existingBaseVersion = BitConverter.ToUInt32(existing, 20);
                }
            }
            if (existingFormat == 0 || existingBaseVersion == 0)
            {
                var sibling = ReadSiblingManifestInfo(Path.GetDirectoryName(metaPath)!);
                if (existingFormat == 0) existingFormat = sibling.Format;
                if (existingBaseVersion == 0) existingBaseVersion = sibling.BaseVersion;
            }

            uint derivedFormat = GetMetaFormat(info.BaseVersion);
            uint saveFormat = existingFormat > derivedFormat ? existingFormat : derivedFormat;
            int saveLen = GetPs4MetaLength(saveFormat);
            byte[] buffer = new byte[saveLen];

            using var ms = new MemoryStream(buffer);
            using var writer = new BinaryWriter(ms);

            writer.Write(META_HEADER_PS4);                                 // offset 0, 4 bytes: magic header
            writer.Write(saveFormat);                                      // offset 4, 4 bytes: meta format version
            writer.Write(decompressedSize);                                // offset 8, 4 bytes: decompressed size
            writer.Write(metaIndex);                                       // offset 12, 4 bytes: manifest index
            writer.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // offset 16, 4 bytes: unix timestamp
            writer.Write(existingBaseVersion != 0 ? existingBaseVersion : (uint)info.BaseVersion); // offset 20, 4 bytes: base version
            writer.Write((ushort)info.GameMode);                           // offset 24, 2 bytes: game mode
            writer.Write((ushort)info.Season);                             // offset 26, 2 bytes: season
            writer.Write(info.TotalPlayTime);                              // offset 28, 8 bytes: total play time
            writer.Write(decompressedSize);                                // offset 36, 4 bytes: decompressed size (duplicate)
            // Total so far: 40 bytes (= PS4_META_BEFORE_NAME)

            if (saveLen > PS4_META_BEFORE_NAME)
            {
                ms.Position = PS4_META_BEFORE_NAME; // 40
                WriteSaveNameAndSummary(writer, info, ms, PS4_META_BEFORE_DIFFICULTY, saveLen);
            }

            // Worlds Part I/II extensions (format >= 2003): slot ID, timestamp, format copy, difficulty tag.
            if (saveFormat >= META_FORMAT_3)
            {
                // Slot ID at offset 300 (8 bytes). This is a platform-assigned opaque identifier,
                // not the save-slot index. The PS4 game generates it internally (presumably);
				// the editor does not track it, so assume zero is safe.
                ms.Position = PS4_META_BEFORE_DIFFICULTY + 4; // 300
                writer.Write((ulong)0); // offset 300, 8 bytes: slot identifier (platform-assigned, not tracked by editor)

                ms.Position = PS4_META_BEFORE_DIFFICULTY + 12; // 308
                writer.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // offset 308, 4 bytes: unix timestamp

                ms.Position = PS4_META_BEFORE_DIFFICULTY + 16; // 312
                writer.Write(saveFormat); // offset 312, 4 bytes: meta format version (copy)
            }

            // Worlds Part II extension (format >= 2004): difficulty tag string (64 bytes at offset 316).
            if (saveFormat >= META_FORMAT_4)
            {
                ms.Position = PS4_META_BEFORE_DIFFICULTY + 20; // 316
                byte[] tagBytes = GetNullTerminatedBytes(info.DifficultyPresetTag ?? "", 64);
                writer.Write(tagBytes); // offset 316, 64 bytes: difficulty preset type string
            }

            File.WriteAllBytes(metaPath, buffer);
        }
    }

    /// <summary>
    /// Read and decrypt a Steam/GOG meta file, returning the decrypted uint array.
    /// Returns null if the file doesn't exist.
    /// </summary>
    public static uint[]? ReadSteamMeta(string saveFilePath, int storageSlot)
    {
        string metaPath = GetSteamMetaPath(saveFilePath);
        if (!File.Exists(metaPath)) return null;

        byte[] raw = File.ReadAllBytes(metaPath);
        uint[] encrypted = BytesToUInts(raw);

        // Try both iteration counts
        int iterations = raw.Length == STEAM_META_LENGTH_VANILLA ? 8 : 6;
        return MetaCrypto.Decrypt(encrypted, storageSlot, iterations);
    }

    /// <summary>
    /// Extract save metadata from a parsed JSON save.
    /// </summary>
    public static SaveMetaInfo ExtractMetaInfo(Models.JsonObject saveData)
    {
        var info = new SaveMetaInfo();

        // Version
        var versionObj = saveData.Get("Version");
        if (versionObj is long vl) info.BaseVersion = (int)vl;
        else if (versionObj is int vi) info.BaseVersion = vi;
        else if (versionObj is Models.RawDouble rvd) info.BaseVersion = (int)rvd.Value;
        else if (versionObj is double vd) info.BaseVersion = (int)vd;

        // CommonStateData fields (SaveName, TotalPlayTime)
        var csd = saveData.GetValue("CommonStateData");
        if (csd is Models.JsonObject commonState)
        {
            // TotalPlayTime
            var playTime = commonState.Get("TotalPlayTime");
            if (playTime is long ptl) info.TotalPlayTime = (ulong)ptl;
            else if (playTime is int pti) info.TotalPlayTime = (ulong)pti;
            else if (playTime is Models.RawDouble rptd) info.TotalPlayTime = (ulong)rptd.Value;
            else if (playTime is double ptd) info.TotalPlayTime = (ulong)ptd;

            // SaveName
            var saveName = commonState.Get("SaveName");
            if (saveName is string sn) info.SaveName = sn;
        }

        // PlayerStateData fields (SaveSummary, DifficultyState)
        var psd = saveData.GetValue("PlayerStateData");
        if (psd is Models.JsonObject playerState)
        {
            // SaveSummary
            var saveSummary = playerState.Get("SaveSummary");
            if (saveSummary is string ss) info.SaveSummary = ss;

            // DifficultyPreset from DifficultyState.Preset.DifficultyPresetType
            var diffState = playerState.GetObject("DifficultyState");
            if (diffState != null)
            {
                var preset = diffState.GetObject("Preset");
                if (preset != null)
                {
                    var presetType = preset.GetString("DifficultyPresetType");
                    if (presetType != null)
                    {
                        info.DifficultyPreset = DifficultyPresetStringToInt(presetType);
                        info.DifficultyPresetTag = presetType;
                    }
                }
            }
        }

        // Detect game mode: modern saves store it as an integer on the active context
        // (BaseContext.GameMode for regular saves, ExpeditionContext.GameMode for
        // expeditions).  Older saves store a string on PlayerStateData.PresetGameMode.
        // Only fall back to the difficulty preset when neither exists.
        int contextMode = 0;
        if (string.Equals(saveData.Get("ActiveContext") as string, "Season", StringComparison.Ordinal))
        {
            var expeditionContext = saveData.GetObject("ExpeditionContext");
            if (expeditionContext != null) contextMode = ReadGameModeInt(expeditionContext.Get("GameMode"));
        }
        if (contextMode <= 0)
        {
            var baseContext = saveData.GetObject("BaseContext");
            if (baseContext != null) contextMode = ReadGameModeInt(baseContext.Get("GameMode"));
        }
        if (contextMode > 0)
        {
            info.GameMode = contextMode;
        }
        else
        {
            var pgm = saveData.GetValue("PlayerStateData.PresetGameMode");
            if (pgm != null)
            {
                if (pgm is string modeStr && modeStr != "Unspecified")
                    info.GameMode = GameModeStringToInt(modeStr);
                else if (pgm is long ml) info.GameMode = (int)ml;
                else if (pgm is int mi) info.GameMode = mi;
            }
        }

        // Fallback: derive game mode from DifficultyState.Preset.DifficultyPresetType
        // (same approach as DetectGameModeFast in SaveFileManager)
        if (info.GameMode <= 0 && info.DifficultyPreset > 0)
        {
            info.GameMode = DifficultyPresetToGameMode(info.DifficultyPreset);
        }

        return info;
    }

    /// <summary>
    /// Reads a game mode integer from the value of a context GameMode field.
    /// Returns 0 when the field is absent or holds no usable number.
    /// </summary>
    private static int ReadGameModeInt(object? value) => value switch
    {
        int i => i,
        long l => (int)l,
        Models.RawDouble rd => (int)rd.Value,
        double d => (int)d,
        _ => 0,
    };

    /// <summary>
    /// Maps a DifficultyPreset integer to the corresponding GameMode integer.
    /// DifficultyPresetType values: 0=Invalid, 1=Custom, 2=Normal, 3=Creative,
    /// 4=Relaxed, 5=Survival, 6=Permadeath.
    /// GameMode values: 0=Unknown, 1=Normal, 2=Survival, 3=Permadeath,
    /// 4=Creative, 5=Custom, 6=Seasonal, 7=Relaxed, 8=Hardcore.
    /// </summary>
    private static int DifficultyPresetToGameMode(int difficultyPreset) => difficultyPreset switch
    {
        1 => 5,  // Custom -> Custom
        2 => 1,  // Normal -> Normal
        3 => 4,  // Creative -> Creative
        4 => 7,  // Relaxed -> Relaxed
        5 => 2,  // Survival -> Survival
        6 => 3,  // Permadeath -> Permadeath
        _ => 0
    };

    private static int DifficultyPresetStringToInt(string preset) => preset switch
    {
        "Invalid" => 0,
        "Custom" => 1,
        "Normal" => 2,
        "Creative" => 3,
        "Relaxed" => 4,
        "Survival" => 5,
        "Permadeath" => 6,
        _ => 0
    };
}
