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

    // Magic header for Switch and PlayStation meta files.
    /// <summary>Switch and PlayStation meta file magic header (0x000007D0).</summary>
    internal const uint META_HEADER_SWITCH_PS = 0x000007D0; // 2000 decimal

    // Meta format version identifiers - must match NMS game values (2001–2004).
    /// <summary>Meta format version for pre-Frontiers saves.</summary>
    internal const uint META_FORMAT_1 = 2001; // Pre-Frontiers
    /// <summary>Meta format version for Frontiers 3.60+ saves.</summary>
    internal const uint META_FORMAT_2 = 2002; // Frontiers 3.60+
    /// <summary>Meta format version for Worlds Part I 5.00+ saves.</summary>
    internal const uint META_FORMAT_3 = 2003; // Worlds Part I 5.00+
    /// <summary>Meta format version for Worlds Part II 5.50+ saves.</summary>
    internal const uint META_FORMAT_4 = 2004; // Worlds Part II 5.50+

    // Steam/GOG meta file sizes (in bytes) - different from Switch/PS
    /// <summary>Steam/GOG meta file size for vanilla saves (104 bytes).</summary>
    internal const int STEAM_META_LENGTH_VANILLA = 104;    // 0x68
    /// <summary>Steam/GOG meta file size for Waypoint saves (360 bytes).</summary>
    internal const int STEAM_META_LENGTH_WAYPOINT = 360;   // 0x168
    /// <summary>Steam/GOG meta file size for Worlds Part I saves (384 bytes).</summary>
    internal const int STEAM_META_LENGTH_WORLDS_I = 384;   // 0x180
    /// <summary>Steam/GOG meta file size for Worlds Part II saves (432 bytes).</summary>
    internal const int STEAM_META_LENGTH_WORLDS_II = 432;  // 0x1B0

    // Switch meta file sizes
    /// <summary>Switch meta file size for vanilla saves (100 bytes).</summary>
    internal const int SWITCH_META_LENGTH_VANILLA = 100;   // 0x64
    /// <summary>Switch meta file size for Waypoint saves (356 bytes).</summary>
    internal const int SWITCH_META_LENGTH_WAYPOINT = 356;  // 0x164
    /// <summary>Switch meta file size for Worlds Part I saves (372 bytes).</summary>
    internal const int SWITCH_META_LENGTH_WORLDS_I = 372;  // 0x174
    /// <summary>Switch meta file size for Worlds Part II saves (380 bytes).</summary>
    internal const int SWITCH_META_LENGTH_WORLDS_II = 380; // 0x17C

    // Offsets for Steam layout
    private const int STEAM_META_AFTER_VANILLA = 84;       // 0x54 - end of known vanilla fields
    private const int STEAM_META_BEFORE_NAME = 88;         // 0x58 - start of save name
    private const int STEAM_META_BEFORE_SUMMARY = 216;     // 0x58 + 128
    private const int STEAM_META_BEFORE_DIFFICULTY = 344;  // 0x58 + 128 + 128
    // Worlds Part I/II extended offsets (after difficulty @ 344)
    private const int STEAM_META_SLOT_ID = 348;            // 0x15C - slot identifier (8 bytes)
    private const int STEAM_META_TIMESTAMP = 356;          // 0x164 - unix timestamp (4 bytes)
    private const int STEAM_META_FORMAT_COPY = 360;        // 0x168 - copy of meta format (4 bytes)
    private const int STEAM_META_DIFFICULTY_TAG = 364;     // 0x16C - difficulty preset type string (64 bytes)

    // Offsets for Switch layout
    private const int SWITCH_META_BEFORE_NAME = 40;        // after header(4)+format(4)+size(4)+index(4)+timestamp(4)+version(4)+mode(2)+season(2)+playtime(8)+padding(4)
    private const int SWITCH_META_BEFORE_SUMMARY = 168;    // 40 + 128
    private const int SWITCH_META_BEFORE_DIFFICULTY = 296; // 40 + 128 + 128

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
        // Base version thresholds from libNOM.io's Meta.GameVersion
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

        writer.Write(META_HEADER);  // 4  -> offset 0
        writer.Write(metaFormat);   // 4  -> offset 4

        if (metaFormat >= META_FORMAT_2)
        {
            // Hashes: write zeroes - the game writes zero hashes here.
            // Non-zero hashes are not required and may confuse
            // older game builds or cross-save validation?
            writer.Write(new byte[48]); // 48 bytes of zeros -> offset 8..55

            writer.Write(decompressedSize); // 4 -> offset 56

            // Compressed size (used from Worlds Part I 5.00)
            if (metaFormat >= META_FORMAT_3)
                writer.Write((uint)compressedData.Length); // offset 60
            else
                writer.Write((uint)0); // placeholder

            writer.Write((uint)0); // profile hash placeholder -> offset 64

            writer.Write(metaBaseVersion); // 4 -> offset 68
            writer.Write((ushort)info.GameMode); // 2 -> offset 72
            writer.Write((ushort)info.Season); // 2 -> offset 74
            writer.Write(info.TotalPlayTime); // 8 -> offset 76

            // Offset 84: repeat decompressed size (matches game-written layout)
            writer.Write(decompressedSize); // 4 -> offset 84

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
            // Pre-Frontiers format: zero hashes + decompressed size only
            writer.Write(new byte[48]); // zero hashes
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
    /// </summary>
    public static void WriteSwitchMeta(string saveFilePath, uint decompressedSize, SaveMetaInfo info, int metaIndex)
    {
        string metaPath = GetSwitchMetaPath(saveFilePath, metaIndex);

        uint metaFormat = GetMetaFormat(info.BaseVersion);
        int bufferLen = GetSwitchMetaLength(metaFormat);
        byte[] buffer = new byte[bufferLen];

        using var ms = new MemoryStream(buffer);
        using var writer = new BinaryWriter(ms);

        writer.Write(META_HEADER_SWITCH_PS); // 4
        writer.Write(metaFormat);            // 4
        writer.Write(decompressedSize);      // 4
        writer.Write(metaIndex);             // 4
        writer.Write((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()); // 4
        writer.Write(info.BaseVersion);      // 4
        writer.Write((ushort)info.GameMode); // 2
        writer.Write((ushort)info.Season);   // 2
        writer.Write(info.TotalPlayTime);    // 8
        // Total so far: 36 bytes

        if (bufferLen > SWITCH_META_BEFORE_NAME)
        {
            // Pad to name position
            ms.Position = SWITCH_META_BEFORE_NAME;
            WriteSaveNameAndSummary(writer, info, ms, SWITCH_META_BEFORE_DIFFICULTY, bufferLen);
        }

        File.WriteAllBytes(metaPath, buffer);
    }

    /// <summary>
    /// Write a PS4 streaming meta file (manifest*.hg) next to the save data file.
    /// This is for savedata*.hg format (non-memory.dat).
    /// </summary>
    public static void WritePlaystationStreamingMeta(string saveFilePath, uint decompressedSize, SaveMetaInfo info, int metaIndex)
    {
        string dir = Path.GetDirectoryName(saveFilePath)!;
        string metaPath = Path.Combine(dir, $"manifest{metaIndex:D2}.hg");

        uint metaFormat = GetMetaFormat(info.BaseVersion);

        // PS4 streaming account meta: header + format + decompressedSize
        if (metaIndex == 0)
        {
            int bufLen = metaFormat >= META_FORMAT_2 ? SWITCH_META_LENGTH_WAYPOINT : SWITCH_META_LENGTH_VANILLA;
            byte[] buffer = new byte[bufLen];
            using var ms = new MemoryStream(buffer);
            using var writer = new BinaryWriter(ms);
            writer.Write(META_HEADER_SWITCH_PS);
            writer.Write(metaFormat);
            writer.Write(decompressedSize);
            File.WriteAllBytes(metaPath, buffer);
            return;
        }

        // PS4 streaming save meta is not written for non-SaveWizard files
        // (the game reads metadata from the data file itself in homebrew mode)
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

        // Detect game mode from PresetGameMode or DifficultyState
        var pgm = saveData.GetValue("PlayerStateData.PresetGameMode");
        if (pgm != null)
        {
            if (pgm is string modeStr && modeStr != "Unspecified")
                info.GameMode = GameModeStringToInt(modeStr);
            else if (pgm is long ml) info.GameMode = (int)ml;
            else if (pgm is int mi) info.GameMode = mi;
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