using NMSE.Core;
using NMSE.Models;
using System.Buffers;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace NMSE.IO;

/// <summary>
/// Manages loading and saving NMS save files.
/// Handles Steam, GOG, Xbox Game Pass, and PS4 save locations.
/// </summary>
public class SaveFileManager
{
    /// <summary>
    /// ISO-8859-1 (Latin-1) encoding which maps bytes 0x00-0xFF to Unicode code points 1:1.
    /// Used instead of UTF-8 when reading save files so that binary data embedded in JSON
    /// string values (e.g. TechBox item IDs) is preserved as individual characters rather
    /// than being corrupted by invalid-UTF-8 replacement.  The JSON parser then detects
    /// characters ≥ 0x80 inside string tokens and produces BinaryData objects.
    /// </summary>
    private static readonly Encoding Latin1 = Encoding.GetEncoding(28591);
    /// <summary>
    /// Supported save file platform types.
    /// </summary>
    public enum Platform { Steam, XboxGamePass, PS4, GOG, Switch, Unknown }

    /// <summary>
    /// Represents a single save slot with its file paths and metadata.
    /// </summary>
    public class SaveSlot
    {
        /// <summary>Gets or sets the zero-based slot index.</summary>
        public int Index { get; set; }
        /// <summary>Gets or sets the path to the save data file.</summary>
        public string? FilePath { get; set; }
        /// <summary>Gets or sets the path to the companion metadata file.</summary>
        public string? MetadataPath { get; set; }
        /// <summary>Gets or sets whether this slot has no save data.</summary>
        public bool IsEmpty { get; set; } = true;
        /// <summary>Gets or sets the last modification time of the save file.</summary>
        public DateTime LastModified { get; set; }
        /// <summary>Gets or sets the platform this save slot belongs to.</summary>
        public Platform Platform { get; set; }
    }

    private static readonly byte[] Lz4Magic = { 0xE5, 0xA1, 0xED, 0xFE };

    /// <summary>
    /// Detects the platform type of saves in the specified directory.
    /// </summary>
    /// <param name="directory">The save directory to inspect.</param>
    /// <returns>The detected platform, or <see cref="Platform.Unknown"/> if unrecognized.</returns>
    public static Platform DetectPlatform(string directory)
    {
        if (File.Exists(Path.Combine(directory, "containers.index")))
            return Platform.XboxGamePass;
        // Switch saves carry manifestaccountdata.hg alongside the manifest*.hg companion
        // files (manifest00.hg for savedata00.hg settings, manifest02.hg for the first
        // game slot, etc.).  Some exports use manifest*.dat names, so keep that check too.
        if (File.Exists(Path.Combine(directory, "manifestaccountdata.hg")) ||
            Directory.GetFiles(directory, "manifest*.dat").Length > 0)
            return Platform.Switch;
        if (File.Exists(Path.Combine(directory, "memory.dat")) ||
            Directory.GetFiles(directory, "savedata*.hg").Length > 0)
            return Platform.PS4;
        if (Directory.GetFiles(directory, "save*.hg").Length > 0 ||
            File.Exists(Path.Combine(directory, "accountdata.hg")))
        {
            // GOG uses DefaultUser directory name; Steam uses st_<SteamID>
            string dirName = new DirectoryInfo(directory).Name;
            if (string.Equals(dirName, "DefaultUser", StringComparison.OrdinalIgnoreCase))
                return Platform.GOG;
            return Platform.Steam;
        }
        return Platform.Unknown;
    }

    /// <summary>
    /// Attempts to find the default NMS save directory for the current OS.
    /// <list type="bullet">
    /// <item><description>Windows (Steam): <c>%APPDATA%\HelloGames\NMS\{profile}</c></description></item>
    /// <item><description>Windows (Xbox GP): <c>%LOCALAPPDATA%\Packages\HelloGames*</c></description></item>
    /// <item><description>macOS: <c>~/Library/Application Support/HelloGames/NMS/{profile}</c></description></item>
    /// <item><description>Linux (Steam/Proton): <c>~/.local/share/Steam/steamapps/compatdata/275850/pfx/drive_c/users/steamuser/AppData/Roaming/HelloGames/NMS/{profile}</c></description></item>
    /// </list>
    /// </summary>
    /// <returns>The path to the first discovered save profile directory, or null if not found.</returns>
    public static string? FindDefaultSaveDirectory()
    {
        // Windows: Steam default location (%APPDATA%\HelloGames\NMS)
        string steamPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HelloGames", "NMS");

        if (Directory.Exists(steamPath))
        {
            var dirs = Directory.GetDirectories(steamPath);
            if (dirs.Length > 0)
                return dirs[0]; // Return first profile directory
        }

        // Windows: Xbox Game Pass location
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string xboxPath = Path.Combine(localAppData, "Packages");
        if (Directory.Exists(xboxPath))
        {
            var nmsDirs = Directory.GetDirectories(xboxPath, "HelloGames*");
            foreach (var nmsDir in nmsDirs)
            {
                // Xbox Game Pass saves live under SystemAppData/wgs/{SaveId}/ which
                // contains the containers.index file.
                string wgsPath = Path.Combine(nmsDir, "SystemAppData", "wgs");
                if (Directory.Exists(wgsPath))
                {
                    foreach (var saveIdDir in Directory.GetDirectories(wgsPath))
                    {
                        if (File.Exists(Path.Combine(saveIdDir, "containers.index")))
                            return saveIdDir;
                    }
                }
            }
        }

        // macOS: ~/Library/Application Support/HelloGames/NMS
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (OperatingSystem.IsMacOS())
        {
            string macPath = Path.Combine(home, "Library", "Application Support", "HelloGames", "NMS");
            if (Directory.Exists(macPath))
            {
                var dirs = Directory.GetDirectories(macPath);
                if (dirs.Length > 0)
                    return dirs[0];
            }
        }

        // Linux: Steam/Proton compatibility data
        if (OperatingSystem.IsLinux())
        {
            string protonPath = Path.Combine(home, ".local", "share", "Steam", "steamapps",
                "compatdata", "275850", "pfx", "drive_c", "users", "steamuser",
                "AppData", "Roaming", "HelloGames", "NMS");
            if (Directory.Exists(protonPath))
            {
                var dirs = Directory.GetDirectories(protonPath);
                if (dirs.Length > 0)
                    return dirs[0];
            }

            // Flatpak Steam location
            string flatpakPath = Path.Combine(home, ".var", "app", "com.valvesoftware.Steam",
                "data", "Steam", "steamapps", "compatdata", "275850", "pfx", "drive_c",
                "users", "steamuser", "AppData", "Roaming", "HelloGames", "NMS");
            if (Directory.Exists(flatpakPath))
            {
                var dirs = Directory.GetDirectories(flatpakPath);
                if (dirs.Length > 0)
                    return dirs[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the backup root directory. Priority:
    /// 1. User-configured path (<see cref="Config.AppConfig.BackupDirectory"/>)
    /// 2. EXE-relative "Save Backups" folder
    /// 3. %TEMP%\NMSE\Save Backups (fallback)
    /// Creates the directory if it doesn't exist. If the configured path cannot be
    /// created (e.g. the drive is unavailable or permissions are insufficient),
    /// falls back to the EXE-relative folder and then to TEMP so backups are never
    /// silently skipped.
    /// </summary>
    public static string ResolveBackupRoot()
    {
        // 1. User-configured path (fall through on failure)
        string? configured = Config.AppConfig.Instance.BackupDirectory;
        if (!string.IsNullOrEmpty(configured))
        {
            try
            {
                Directory.CreateDirectory(configured);
                return configured;
            }
            catch
            {
                // Fall through to the EXE-relative location
            }
        }

        // 2. EXE-relative
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string exeRoot = Path.Combine(exeDir, "Save Backups");
        try
        {
            Directory.CreateDirectory(exeRoot);
            return exeRoot;
        }
        catch
        {
            // 3. TEMP fallback
            string tempRoot = Path.Combine(Path.GetTempPath(), "NMSE", "Save Backups");
            Directory.CreateDirectory(tempRoot);
            return tempRoot;
        }
    }

    /// <summary>
    /// Returns all existing backup root directories that may contain backup ZIPs,
    /// in priority order. Does not create directories. Used by restore UI to find
    /// backups regardless of where they were written.
    /// The same directory is never returned twice, even when the user-configured
    /// path overlaps the EXE-relative or TEMP fallback (e.g. when the default
    /// backup folder was selected from the toolbar combo).
    /// </summary>
    public static List<string> FindExistingBackupRoots()
    {
        var roots = new List<string>();
        var seen = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        void AddIfMissing(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            string normalized = NormalizeRootPath(path);
            if (seen.Add(normalized))
                roots.Add(normalized);
        }

        // 1. User-configured path (if set and exists)
        string? configured = Config.AppConfig.Instance.BackupDirectory;
        if (!string.IsNullOrEmpty(configured) && Directory.Exists(configured))
            AddIfMissing(configured);

        // 2. EXE-relative (if exists)
        string exeRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Save Backups");
        if (Directory.Exists(exeRoot))
            AddIfMissing(exeRoot);

        // 3. TEMP fallback (if exists)
        string tempRoot = Path.Combine(Path.GetTempPath(), "NMSE", "Save Backups");
        if (Directory.Exists(tempRoot))
            AddIfMissing(tempRoot);

        return roots;
    }

    /// <summary>
    /// Returns a fully-qualified path without trailing directory separators,
    /// so equivalent spellings of the same directory compare equal.
    /// </summary>
    private static string NormalizeRootPath(string path)
    {
        try
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path;
        }
    }

    /// <summary>
    /// Creates a timestamped zip backup of the files in the save directory,
    /// retaining up to 10 backups.  Uses <see cref="ResolveBackupRoot"/> to
    /// determine the backup location.
    /// The file set depends on the platform detected in the directory:
    /// PC platforms back up *.hg files plus meta.json; Xbox Game Pass backs up
    /// all files (GUID-named blobs, containers.index); PS4 with a monolithic
    /// memory.dat backs up memory.dat plus any *.hg files.
    /// </summary>
    /// <param name="saveDirectory">The save directory to back up.</param>
    public static void BackupSaveDirectory(string saveDirectory)
    {
        string backupRoot = ResolveBackupRoot();

        string dirName = new DirectoryInfo(saveDirectory).Name;
        string backupPattern = $"{dirName}_*.zip";
        var existingBackups = Directory.GetFiles(backupRoot, backupPattern)
            .OrderBy(f => File.GetCreationTimeUtc(f))
            .ToList();

        // If there are 10 or more backups, delete the oldest one
        if (existingBackups.Count >= 10)
        {
            File.Delete(existingBackups[0]);
            existingBackups.RemoveAt(0);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string backupName = $"{dirName}_{timestamp}.zip";
        string backupPath = Path.Combine(backupRoot, backupName);

        // Avoid zipping if already exists for this second
        if (!File.Exists(backupPath))
        {
            CreateFilteredZip(saveDirectory, backupPath);
        }
    }

    /// <summary>
    /// Returns all backup ZIP paths for the given save directory, across every
    /// existing backup root (configured, EXE-relative, TEMP), newest first.
    /// Each backup file is returned at most once even if a directory appears
    /// in multiple roots (e.g. the configured path overlapping a fallback).
    /// </summary>
    /// <param name="saveDirectory">The save directory whose backups are sought.</param>
    /// <returns>The matching ZIP paths ordered by creation time (newest first).</returns>
    public static List<string> FindBackupZips(string saveDirectory)
    {
        string dirName = new DirectoryInfo(saveDirectory).Name;
        string backupPattern = $"{dirName}_*.zip";

        var results = new List<string>();
        var seen = new HashSet<string>(
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (string root in FindExistingBackupRoots())
        {
            try
            {
                foreach (string file in Directory.GetFiles(root, backupPattern))
                {
                    if (seen.Add(file))
                        results.Add(file);
                }
            }
            catch
            {
                // Skip roots that cannot be enumerated (e.g. unavailable drive)
            }
        }

        return results
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .ToList();
    }

    /// <summary>
    /// Returns the entry names (as stored in the ZIP, including any directory
    /// prefixes) contained in a backup ZIP.
    /// </summary>
    /// <param name="zipPath">Path to the backup ZIP.</param>
    /// <returns>The full entry names of the ZIP, in archive order.</returns>
    public static List<string> GetBackupEntryNames(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return zip.Entries.Select(e => e.FullName).Where(n => n.Length > 0).ToList();
    }

    /// <summary>
    /// Returns whether a backup ZIP contains an entry whose file name matches
    /// <paramref name="fileName"/> (case-insensitive, matched against the final
    /// path segment of every entry so nested layouts are found).
    /// </summary>
    /// <param name="zipPath">Path to the backup ZIP.</param>
    /// <param name="fileName">The file name to look for.</param>
    /// <returns><c>true</c> if a matching entry exists.</returns>
    public static bool BackupContainsFile(string zipPath, string fileName)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return FindEntryByName(zip, fileName) != null;
    }

    /// <summary>
    /// Restores a single file from a backup ZIP to the given destination path.
    /// The ZIP entry is located by file name (case-insensitive match against the
    /// final path segment of every entry), which handles backups whose entries
    /// are stored under directory prefixes.
    /// </summary>
    /// <param name="zipPath">Path to the backup ZIP.</param>
    /// <param name="fileName">The file name to restore.</param>
    /// <param name="destinationPath">The destination file path (overwritten).</param>
    /// <returns><c>true</c> if the entry was found and extracted; <c>false</c> otherwise.</returns>
    public static bool RestoreFileFromBackup(string zipPath, string fileName, string destinationPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var entry = FindEntryByName(zip, fileName);
        if (entry == null) return false;

        entry.ExtractToFile(destinationPath, overwrite: true);
        return true;
    }

    /// <summary>
    /// Restores every entry of a backup ZIP into the given destination directory,
    /// preserving the entry directory structure. Entry paths that would escape
    /// the destination directory (e.g. absolute or parent-relative paths) are
    /// skipped as a safety measure.
    /// </summary>
    /// <param name="zipPath">Path to the backup ZIP.</param>
    /// <param name="destinationDirectory">The directory to extract into (overwrites existing files).</param>
    /// <returns>The full paths of the files that were written.</returns>
    public static List<string> RestoreBackupToDirectory(string zipPath, string destinationDirectory)
    {
        var written = new List<string>();
        string basePath = Path.GetFullPath(destinationDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string basePrefix = basePath + Path.DirectorySeparatorChar;

        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries)
        {
            string entryPath = entry.FullName;
            if (string.IsNullOrEmpty(entryPath)) continue;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(basePath, entryPath));
            }
            catch
            {
                continue;
            }

            if (!fullPath.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string? parent = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);

            entry.ExtractToFile(fullPath, overwrite: true);
            written.Add(fullPath);
        }

        return written;
    }

    /// <summary>
    /// Finds the ZIP entry whose file name (final path segment) matches the
    /// given name case-insensitively. Returns the first match in archive order.
    /// </summary>
    private static ZipArchiveEntry? FindEntryByName(ZipArchive zip, string fileName)
    {
        foreach (var entry in zip.Entries)
        {
            string entryName = Path.GetFileName(entry.FullName.Replace('/', Path.DirectorySeparatorChar));
            if (string.Equals(entryName, fileName, StringComparison.OrdinalIgnoreCase))
                return entry;
        }
        return null;
    }

    /// <summary>
    /// Creates a zip containing the backup-relevant files of the given directory
    /// tree. The file set depends on the platform detected in the directory:
    /// PC platforms back up *.hg files plus meta.json; Xbox Game Pass backs up
    /// all files (GUID-named blobs, containers.index); PS4 with a monolithic
    /// memory.dat backs up memory.dat plus any *.hg files.
    /// </summary>
    private static void CreateFilteredZip(string sourceDir, string zipPath)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        string basePath = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (string filePath in EnumerateBackupFilesSafe(sourceDir))
        {
            string fullFilePath = Path.GetFullPath(filePath);
            if (!fullFilePath.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                continue;

            string relativePath = fullFilePath[(basePath.Length + 1)..];
            zip.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Fastest);
        }
    }

    /// <summary>
    /// Enumerates the files that should be included in a backup of the given
    /// directory, skipping subdirectories that cannot be accessed. This prevents
    /// the backup from failing on platforms (e.g. Wine) where permission checks
    /// behave differently, or when the source directory contains protected system
    /// folders (e.g. loading a .hg from the desktop).
    /// </summary>
    private static IEnumerable<string> EnumerateBackupFilesSafe(string root)
    {
        string[] patterns = BackupFilePatterns(root);
        var dirs = new Queue<string>();
        dirs.Enqueue(root);

        while (dirs.Count > 0)
        {
            string dir = dirs.Dequeue();

            string[] files;
            try
            {
                files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (string file in files)
            {
                if (patterns.Any(p => p == "*" || file.EndsWith(p, StringComparison.OrdinalIgnoreCase)))
                    yield return file;
            }

            string[] subDirs;
            try
            {
                subDirs = Directory.GetDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (string subDir in subDirs)
                dirs.Enqueue(subDir);
        }
    }

    /// <summary>
    /// Determines the file suffix patterns included in backups of the given
    /// directory based on the platform detected there.
    /// </summary>
    private static string[] BackupFilePatterns(string root)
    {
        switch (DetectPlatform(root))
        {
            case Platform.XboxGamePass:
                // Xbox save data are GUID-named blobs plus containers.index - no .hg files
                return new[] { "*" };
            case Platform.PS4 when File.Exists(Path.Combine(root, "memory.dat")):
                // PS4 monolithic memory.dat holds all slots in a single binary file
                return new[] { ".hg", ".dat", "meta.json" };
            default:
                // PC platforms: save files, meta files, manifests and slot metadata
                return new[] { ".hg", "meta.json" };
        }
    }

    /// <summary>
    /// Load a save file and return the JSON data.
    /// Handles compressed (LZ4), NOMANSKY-header (PS4/PS5), and uncompressed save files.
    /// Uses streaming I/O and string.Create to minimize intermediate memory allocations.
    /// </summary>
    public static JsonObject LoadSaveFile(string filePath)
    {
        string json;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 65536, FileOptions.SequentialScan))
        {
            byte[] header = new byte[8];
            int headerRead = fs.Read(header, 0, Math.Min(8, (int)fs.Length));
            fs.Position = 0;

            if (headerRead >= 8 && IsNomanSkyHeader(header))
            {
                // PS4/PS5 save: NOMANSKY header, JSON data after header
                json = ReadNomanSkySave(fs);
            }
            else if (headerRead >= 4 && IsLz4Compressed(header))
            {
                json = DecompressLz4SaveStreamed(fs);
            }
            else
            {
                json = ReadPlainSave(fs);
            }
        }

        var result = JsonObject.Parse(json);

        // Register the PlayerStateData and SpawnStateData context transforms.
        RegisterContextTransforms(result);

        return result;
    }

    /// <summary>
    /// Save JSON data back to a file with LZ4 compression.
    /// Optionally writes a platform-appropriate meta file alongside the save.
    /// </summary>
    /// <param name="filePath">Path to the save file.</param>
    /// <param name="data">The JSON save data to write.</param>
    /// <param name="compress">Whether to LZ4-compress the output.</param>
    /// <param name="writeMeta">Whether to also write a platform meta file (mf_*.hg, manifest*.hg).</param>
    /// <param name="platform">Platform to determine meta format. Defaults to auto-detect from directory.</param>
    /// <param name="slotIndex">Slot index for meta file naming and encryption key.</param>
    public static void SaveToFile(string filePath, JsonObject data, bool compress = true,
        bool writeMeta = false, Platform? platform = null, int slotIndex = 0)
    {
        // NMS save files use compact JSON (no whitespace) with a null terminator byte.
        string json = data.ToString();
        byte[] jsonBytes = Latin1.GetBytes(json);

        // Append null terminator (NMS expects \0 after JSON data)
        byte[] dataBytes = new byte[jsonBytes.Length + 1];
        Buffer.BlockCopy(jsonBytes, 0, dataBytes, 0, jsonBytes.Length);
        // dataBytes[jsonBytes.Length] is already 0 (null terminator)

        byte[]? compressedBytes = null;
        if (compress)
        {
            // Compress to memory first, then write to file (avoids double compression when writeMeta is true)
            using var ms = new MemoryStream();
            using (var compressor = new Lz4CompressorStream(ms))
            {
                compressor.Write(dataBytes, 0, dataBytes.Length);
            }
            compressedBytes = ms.ToArray();
            File.WriteAllBytes(filePath, compressedBytes);
        }
        else
        {
            File.WriteAllBytes(filePath, dataBytes);
            compressedBytes = dataBytes;
        }

        // Write platform meta file if requested
        if (writeMeta && compressedBytes != null)
        {
            var detectedPlatform = platform ?? DetectPlatform(Path.GetDirectoryName(filePath)!);
            var metaInfo = MetaFileWriter.ExtractMetaInfo(data);
            uint decompressedSize = (uint)dataBytes.Length;
            // Derive storage slot from the file name for correct encryption key.
            // Using the wrong slot produces garbled meta data (e.g. save name
            // shows as random characters like ","  in the game's slot browser).
            int storageSlot = SaveSlotManager.StorageSlotFromFileName(filePath);

            switch (detectedPlatform)
            {
                case Platform.Steam:
                case Platform.GOG:
                    MetaFileWriter.WriteSteamMeta(filePath, compressedBytes, decompressedSize, metaInfo, storageSlot);
                    break;
                case Platform.Switch:
                    MetaFileWriter.WriteSwitchMeta(filePath, decompressedSize, metaInfo, slotIndex);
                    break;
                case Platform.PS4:
                    MetaFileWriter.WritePlaystationStreamingMeta(filePath, decompressedSize, metaInfo, slotIndex);
                    break;
            }
        }
    }

/// <summary>
    /// Save JSON data back to a PS4 SaveWizard streaming (.hg with NOMANSKY header) file.
    /// Preserves the original 0x70-byte header and writes the new JSON data after it.
    /// Uses Latin-1 encoding consistent with all other save formats.
    /// </summary>
    /// <param name="filePath">Path to the NOMANSKY-header save file.</param>
    /// <param name="data">The JSON save data to write.</param>
    public static void SaveNomanSkyFile(string filePath, JsonObject data)
    {
        // Serialize JSON to Latin-1 bytes, consistent with all other save formats.
        // Append a NUL terminator to match the original file format.
        string json = data.ToString();
        byte[] jsonBytes = Latin1.GetBytes(json);
        byte[] dataBytes = new byte[jsonBytes.Length + 1];
        Buffer.BlockCopy(jsonBytes, 0, dataBytes, 0, jsonBytes.Length);
        // dataBytes[jsonBytes.Length] is already 0 (null terminator)

        // Read the original header to preserve it
        byte[] header;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (fs.Length < 0x70)
                throw new IOException("Corrupt NOMANSKY file: header too small");
            header = new byte[0x70];
            int read = fs.Read(header, 0, 0x70);
            if (read < 0x70)
                throw new IOException("Corrupt NOMANSKY file: could not read full header");
        }

        // Update JSON size field at offset 0x5C with the actual data size
        int dataSize = dataBytes.Length;
        header[0x5C] = (byte)(dataSize & 0xFF);
        header[0x5D] = (byte)((dataSize >> 8) & 0xFF);
        header[0x5E] = (byte)((dataSize >> 16) & 0xFF);
        header[0x5F] = (byte)((dataSize >> 24) & 0xFF);

        // Write header + JSON data
using var outFs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        outFs.Write(header, 0, 0x70);
        outFs.Write(dataBytes, 0, dataBytes.Length);
    }

    /// <summary>
    /// Save JSON data back to an Xbox Game Pass save slot.
    /// Writes the compressed save data and meta to the blob directory,
    /// then updates the containers.index file.
    /// </summary>
    /// <param name="containersIndexPath">Path to the containers.index file.</param>
    /// <param name="slotIdentifier">Slot identifier (e.g., "Slot1Auto").</param>
    /// <param name="data">The JSON save data to write.</param>
    public static void SaveXboxSave(string containersIndexPath, string slotIdentifier, JsonObject data)
    {
        // Parse the full containers.index to get header info and all slots
        var indexData = ContainersIndexManager.ParseContainersIndexFull(containersIndexPath);
        if (!indexData.Slots.TryGetValue(slotIdentifier, out var slotInfo))
            throw new InvalidOperationException($"Xbox slot '{slotIdentifier}' not found in containers.index");

        // Serialize JSON to bytes with null terminator
        string json = data.ToString();
        byte[] jsonBytes = Latin1.GetBytes(json);
        byte[] dataBytes = new byte[jsonBytes.Length + 1];
        Buffer.BlockCopy(jsonBytes, 0, dataBytes, 0, jsonBytes.Length);

        // Compress save data using NMS LZ4 streaming format
        byte[] compressedData;
        using (var ms = new MemoryStream())
        using (var compressor = new Lz4CompressorStream(ms))
        {
            compressor.Write(dataBytes, 0, dataBytes.Length);
            compressor.Flush();
            compressedData = ms.ToArray();
        }

        // Read existing meta or create minimal placeholder
        byte[] metaData = ContainersIndexManager.LoadXboxMeta(slotInfo) ?? new byte[24];

        // Write save data blob, meta blob, and blob container
        ContainersIndexManager.WriteXboxSave(slotInfo, compressedData, metaData);

        // Update the slot's last modified time
        slotInfo.LastModified = DateTimeOffset.UtcNow;

        // Rewrite the containers.index file with all slots
        ContainersIndexManager.WriteContainersIndex(
            containersIndexPath,
            indexData.Slots.Values,
            indexData.ProcessIdentifier,
            indexData.AccountGuid,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Save account data back to the Xbox Game Pass AccountData blob.
    /// Uses raw LZ4 block compression (not NMS streaming), matching the format
    /// the game uses for AccountData and Settings blobs.
    /// </summary>
    /// <param name="containersIndexPath">Path to the containers.index file.</param>
    /// <param name="accountData">The account data JSON object to write.</param>
    public static void SaveXboxAccountData(string containersIndexPath, JsonObject accountData)
    {
        // Parse the full containers.index to get header info and all slots
        var indexData = ContainersIndexManager.ParseContainersIndexFull(containersIndexPath);
        if (!indexData.Slots.TryGetValue(ContainersIndexManager.AccountDataIdentifier, out var accountSlot))
            throw new InvalidOperationException("Xbox AccountData slot not found in containers.index");

        // Serialize JSON to bytes with null terminator
        string json = accountData.ToString();
        byte[] jsonBytes = Latin1.GetBytes(json);
        byte[] dataBytes = new byte[jsonBytes.Length + 1];
        Buffer.BlockCopy(jsonBytes, 0, dataBytes, 0, jsonBytes.Length);

        // Compress account data using raw LZ4 block compression.
        // AccountData/Settings use raw LZ4, not NMS streaming (0xE5A1EDFE).
        byte[] compressedBuffer = new byte[Lz4Compressor.MaxCompressedLength(dataBytes.Length)];
        int compressedLen = Lz4Compressor.Compress(dataBytes, 0, dataBytes.Length,
            compressedBuffer, 0, compressedBuffer.Length);
        byte[] compressedData = new byte[compressedLen];
        Buffer.BlockCopy(compressedBuffer, 0, compressedData, 0, compressedLen);

        // Read existing meta or create minimal account meta placeholder.
        // Account meta is 20 bytes: version(4) + padding(12) + decompressedSize(4)
        byte[] metaData = ContainersIndexManager.LoadXboxMeta(accountSlot) ?? CreateAccountMeta((uint)dataBytes.Length);

        // Update the decompressed size in the meta if we have existing meta
        if (metaData.Length >= 20)
        {
            byte[] sizeBytes = BitConverter.GetBytes((uint)dataBytes.Length);
            Buffer.BlockCopy(sizeBytes, 0, metaData, 16, 4);
        }

        // Write account data blob, meta blob, and blob container
        ContainersIndexManager.WriteXboxSave(accountSlot, compressedData, metaData);

        // Update the slot's last modified time
        accountSlot.LastModified = DateTimeOffset.UtcNow;

        // Rewrite the containers.index file with all slots
        ContainersIndexManager.WriteContainersIndex(
            containersIndexPath,
            indexData.Slots.Values,
            indexData.ProcessIdentifier,
            indexData.AccountGuid,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates a minimal Xbox account meta blob (20 bytes).
    /// Format: version(4, always 1) + padding(12, zeros) + decompressedSize(4).
    /// </summary>
    private static byte[] CreateAccountMeta(uint decompressedSize)
    {
        byte[] meta = new byte[20];
        // Version = 1
        meta[0] = 1;
        // Decompressed size at offset 16
        byte[] sizeBytes = BitConverter.GetBytes(decompressedSize);
        Buffer.BlockCopy(sizeBytes, 0, meta, 16, 4);
        return meta;
    }

    /// <summary>
    /// Load a save from an Xbox Game Pass containers.index directory.
    /// </summary>
    /// <param name="containersIndexPath">Path to the containers.index file.</param>
    /// <param name="saveIdentifier">Slot identifier (e.g., "Slot1Auto").</param>
    /// <returns>Parsed JSON object, or null if the slot doesn't exist.</returns>
    public static JsonObject? LoadXboxSave(string containersIndexPath, string saveIdentifier)
    {
        var slots = ContainersIndexManager.ParseContainersIndex(containersIndexPath);
        if (!slots.TryGetValue(saveIdentifier, out var slotInfo)) return null;

        string? json = ContainersIndexManager.LoadXboxSave(slotInfo);
        if (json == null) return null;

        var result = JsonObject.Parse(json);
        RegisterContextTransforms(result);
        return result;
    }

    /// <summary>
    /// Load a save from a PS4 memory.dat file.
    /// </summary>
    /// <param name="memoryDatPath">Path to memory.dat.</param>
    /// <param name="slotIndex">Slot index within memory.dat.</param>
    /// <returns>Parsed JSON object, or null if the slot doesn't exist.</returns>
    public static JsonObject? LoadPS4MemoryDatSave(string memoryDatPath, int slotIndex)
    {
        string? json = MemoryDatManager.ExtractSlotData(memoryDatPath, slotIndex);
        if (json == null) return null;

        var result = JsonObject.Parse(json);
        RegisterContextTransforms(result);
        return result;
    }

    /// <summary>
    /// Register the context-based path transforms on a loaded save.
    /// Uses shape-driven logic: prefers ExpeditionContext when ActiveContext is "Season"
    /// (or when ExpeditionContext exists and BaseContext does not), otherwise prefers BaseContext.
    /// </summary>
    internal static void RegisterContextTransforms(JsonObject result)
    {
        if (result.Get("PlayerStateData") == null)
        {
            result.RegisterTransform("PlayerStateData", obj =>
            {
                if (obj is not JsonObject root) return "PlayerStateData";
                // Prefer ExpeditionContext if ActiveContext says "Season" (or if ExpeditionContext exists
                // and BaseContext does not).
                if (root.GetValue("ExpeditionContext.PlayerStateData") != null
                    && (root.GetValue("BaseContext.PlayerStateData") == null
                        || string.Equals(root.Get("ActiveContext") as string, "Season", StringComparison.Ordinal)))
                    return "ExpeditionContext.PlayerStateData";
                if (root.GetValue("BaseContext.PlayerStateData") != null)
                    return "BaseContext.PlayerStateData";
                return "PlayerStateData";
            });
        }

        if (result.Get("SpawnStateData") == null)
        {
            result.RegisterTransform("SpawnStateData", obj =>
            {
                if (obj is not JsonObject root) return "SpawnStateData";
                if (root.GetValue("ExpeditionContext.SpawnStateData") != null
                    && (root.GetValue("BaseContext.SpawnStateData") == null
                        || string.Equals(root.Get("ActiveContext") as string, "Season", StringComparison.Ordinal)))
                    return "ExpeditionContext.SpawnStateData";
                if (root.GetValue("BaseContext.SpawnStateData") != null)
                    return "BaseContext.SpawnStateData";
                return "SpawnStateData";
            });
        }
    }

    private static bool IsLz4Compressed(byte[] data)
    {
        if (data.Length < 4) return false;
        return data[0] == Lz4Magic[0] && data[1] == Lz4Magic[1] &&
               data[2] == Lz4Magic[2] && data[3] == Lz4Magic[3];
    }

    /// <summary>
    /// HGSAVEV2 header: "HGSAVEV2\0" (9 bytes), used by post-Omega Xbox/Microsoft saves.
    /// </summary>
    private static readonly byte[] Hgsv2Header = new byte[] { 0x48, 0x47, 0x53, 0x41, 0x56, 0x45, 0x56, 0x32, 0x00 }; // "HGSAVEV2\0"

    private static bool IsHgsv2Header(byte[] data, int length)
    {
        if (length < Hgsv2Header.Length) return false;
        for (int i = 0; i < Hgsv2Header.Length; i++)
            if (data[i] != Hgsv2Header[i]) return false;
        return true;
    }

    /// <summary>
    /// Decompress the first HGSAVEV2 frame to get a text prefix for fast scanning.
    /// HGSAVEV2 format: 9-byte header, then frames of [decompressedSize(4)] [compressedSize(4)] [LZ4 data].
    /// </summary>
    private static string? DecompressHgsv2FirstFrame(FileStream fs)
    {
        fs.Position = Hgsv2Header.Length;
        byte[] frameHeader = new byte[8];
        if (fs.Read(frameHeader, 0, 8) < 8) return null;

        int decompressedLen = frameHeader[0] | (frameHeader[1] << 8) | (frameHeader[2] << 16) | (frameHeader[3] << 24);
        int compressedLen = frameHeader[4] | (frameHeader[5] << 8) | (frameHeader[6] << 16) | (frameHeader[7] << 24);
        if (decompressedLen <= 0 || compressedLen <= 0) return null;
        if (decompressedLen > 256 * 1024 * 1024 || compressedLen > 256 * 1024 * 1024) return null;

        byte[] block = new byte[compressedLen];
        int totalRead = 0;
        while (totalRead < compressedLen)
        {
            int n = fs.Read(block, totalRead, compressedLen - totalRead);
            if (n <= 0) break;
            totalRead += n;
        }

        byte[] decompressed = new byte[decompressedLen];
        int written = Lz4Compressor.Decompress(block, 0, totalRead, decompressed, 0, decompressedLen);
        return Latin1.GetString(decompressed, 0, written);
    }

    /// <summary>
    /// NOMANSKY magic header for PS4/PS5 save files.
    /// </summary>
    private static readonly byte[] NomanSkyMagic = "NOMANSKY"u8.ToArray();

    /// <summary>
    /// Checks if a file has the NOMANSKY header (PS4 SaveWizard streaming format).
    /// </summary>
    /// <param name="filePath">Path to the file to check.</param>
    /// <returns>True if the file starts with the NOMANSKY magic bytes.</returns>
    public static bool IsNomanSkyFile(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 8) return false;
            byte[] header = new byte[8];
            int read = fs.Read(header, 0, 8);
            return read >= 8 && IsNomanSkyHeader(header);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNomanSkyHeader(byte[] data)
    {
        if (data.Length < 8) return false;
        for (int i = 0; i < 8; i++)
            if (data[i] != NomanSkyMagic[i]) return false;
        return true;
    }

    /// <summary>
    /// Read a PS4/PS5 NOMANSKY-header save file.
    /// The header contains a fixed preamble followed by the JSON data.
    /// The JSON data size is stored at offset 0x5C (little-endian uint32).
    /// </summary>
    private static string ReadNomanSkySave(FileStream fs)
    {
        // Read full header to find JSON offset and size
        byte[] headerBuf = new byte[0x70]; // Max header size we expect
        int read = 0;
        while (read < headerBuf.Length && read < fs.Length)
        {
            int n = fs.Read(headerBuf, read, headerBuf.Length - read);
            if (n <= 0) break;
            read += n;
        }

        // JSON data size at offset 0x5C (little-endian)
        int jsonSize = headerBuf[0x5C] | (headerBuf[0x5D] << 8) |
                       (headerBuf[0x5E] << 16) | (headerBuf[0x5F] << 24);

        // Seek to JSON start at offset 0x70
        fs.Position = 0x70;

        // If jsonSize is unreasonable, read everything after header
        if (jsonSize <= 0 || jsonSize > fs.Length - fs.Position)
            jsonSize = (int)(fs.Length - fs.Position);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(jsonSize);
        try
        {
            read = 0;
            while (read < jsonSize)
            {
                int n = fs.Read(buffer, read, jsonSize - read);
                if (n <= 0) break;
                read += n;
            }

            // PS4/PS5 saves use UTF-8 encoding for the JSON data.
            // Trim trailing NUL bytes that may pad the JSON region.
            int jsonEnd = read;
            while (jsonEnd > 0 && buffer[jsonEnd - 1] == 0)
                jsonEnd--;
            return Encoding.UTF8.GetString(buffer, 0, jsonEnd);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Read an uncompressed save file from a stream using Latin1 encoding.
    /// Uses ArrayPool to avoid a long-lived byte[] allocation and string.Create
    /// to avoid the intermediate char[] that Encoding.GetString would allocate.
    /// </summary>
    private static string ReadPlainSave(FileStream fs)
    {
        int length = (int)fs.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            int read = 0;
            while (read < length)
            {
                int n = fs.Read(buffer, read, length - read);
                if (n <= 0) break;
                read += n;
            }
            return BytesToLatin1String(buffer, read);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Decompress an LZ4 save file from a stream.
    /// Streams compressed blocks directly from the FileStream instead of loading
    /// the entire file into memory first, and uses pooled buffers for compressed blocks.
    /// </summary>
    private static string DecompressLz4SaveStreamed(FileStream fs)
    {
        byte[] header = new byte[16];

        // First pass: calculate total decompressed size by scanning block headers
        int totalSize = 0;
        long scanPos = 0;
        while (scanPos + 16 <= fs.Length)
        {
            fs.Position = scanPos;
            if (fs.Read(header, 0, 16) < 16) break;
            if (header[0] != Lz4Magic[0] || header[1] != Lz4Magic[1] ||
                header[2] != Lz4Magic[2] || header[3] != Lz4Magic[3])
                break;

            int compressedLen = header[4] | (header[5] << 8) |
                               (header[6] << 16) | (header[7] << 24);
            int uncompressedLen = header[8] | (header[9] << 8) |
                                 (header[10] << 16) | (header[11] << 24);

            if (compressedLen < 0 || uncompressedLen < 0)
                throw new IOException("Corrupt save file: negative length values");
            if (compressedLen > 256 * 1024 * 1024 || uncompressedLen > 256 * 1024 * 1024)
                throw new IOException("Corrupt save file: block size exceeds 256MB limit");

            totalSize += uncompressedLen;
            scanPos += 16 + compressedLen;
        }

        // Single allocation for all decompressed data
        byte[] result = new byte[totalSize];
        int writePos = 0;
        fs.Position = 0;

        while (fs.Position + 16 <= fs.Length)
        {
            if (fs.Read(header, 0, 16) < 16) break;
            if (header[0] != Lz4Magic[0] || header[1] != Lz4Magic[1] ||
                header[2] != Lz4Magic[2] || header[3] != Lz4Magic[3])
                break;

            int compressedLen = header[4] | (header[5] << 8) |
                               (header[6] << 16) | (header[7] << 24);
            int uncompressedLen = header[8] | (header[9] << 8) |
                                 (header[10] << 16) | (header[11] << 24);

            if (fs.Position + compressedLen > fs.Length)
                throw new IOException("Corrupt save file: compressed data exceeds file length");

            // Read compressed block using pooled buffer
            byte[] compressedBlock = ArrayPool<byte>.Shared.Rent(compressedLen);
            try
            {
                int totalRead = 0;
                while (totalRead < compressedLen)
                {
                    int n = fs.Read(compressedBlock, totalRead, compressedLen - totalRead);
                    if (n <= 0) break;
                    totalRead += n;
                }

                // Decompress directly from byte array into result (no stream overhead)
                int decompressed = Lz4Compressor.Decompress(
                    compressedBlock, 0, totalRead,
                    result, writePos, uncompressedLen);
                writePos += decompressed;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(compressedBlock);
            }
        }

        return BytesToLatin1String(result, writePos);
    }

    /// <summary>
    /// Convert bytes to a Latin1 string using the framework's optimized Latin1 encoder.
    /// Latin1 maps bytes 0x00-0xFF to Unicode code points U+0000-U+00FF one-to-one.
    /// The .NET runtime uses SIMD-optimized widening for this encoding, which is
    /// significantly faster than a manual byte-to-char loop.
    /// </summary>
    private static string BytesToLatin1String(byte[] bytes, int length)
    {
        return Latin1.GetString(bytes, 0, length);
    }

    /// <summary>
    /// Format play time as MM:SS or H:MM:SS string.
    /// </summary>
    public static string FormatPlayTime(long seconds)
    {
        long hours = seconds / 3600;
        long minutes = (seconds % 3600) / 60;
        long secs = seconds % 60;
        return hours > 0 ? $"{hours}:{minutes:D2}:{secs:D2}" : $"{minutes}:{secs:D2}";
    }

    /// <summary>
    /// Quickly detect the game mode from a save file without fully parsing it.
    /// Only reads and decompresses the first LZ4 block to scan for PresetGameMode.
    /// </summary>
    public static int DetectGameModeFast(string filePath)
    {
        try
        {
            string? text = ReadFirstJsonBlock(filePath);
            if (string.IsNullOrEmpty(text)) return 0;

            // Try PresetGameMode first (human-readable or obfuscated key "pwt")
            int result = ScanKeyForGameMode(text, "\"PresetGameMode\"");
            if (result <= 0) result = ScanKeyForGameMode(text, "\"pwt\"");
            if (result > 0) return result;

            // Modern saves store the mode as an integer on the context GameMode field
            // (BaseContext/ExpeditionContext).  Obfuscated saves use the key "idA";
            // plain-text saves use "GameMode".  Both keys are also used as container
            // objects (e.g. "idA":{"pwt":"Unspecified"}), so scan past those.
            if (result <= 0) result = ScanKeyForGameModeAll(text, "\"idA\"");
            if (result > 0) return result;
            if (result <= 0) result = ScanKeyForGameModeAll(text, "\"GameMode\"");
            if (result > 0) return result;

            // PresetGameMode may be "Unspecified" - try DifficultyState.Preset.DifficultyPresetType
            // Obfuscated: "LyC" = DifficultyState, "7ND" = DifficultyPresetType
            int dsIdx = text.IndexOf("\"DifficultyState\"", StringComparison.Ordinal);
            if (dsIdx < 0) dsIdx = text.IndexOf("\"LyC\"", StringComparison.Ordinal);
            if (dsIdx >= 0)
            {
                int dpIdx = text.IndexOf("\"DifficultyPresetType\"", dsIdx, StringComparison.Ordinal);
                if (dpIdx < 0) dpIdx = text.IndexOf("\"7ND\"", dsIdx, StringComparison.Ordinal);
                if (dpIdx >= 0)
                {
                    // Skip past the key to find the colon and then the value
                    int colonIdx = text.IndexOf(':', dpIdx + 1);
                    if (colonIdx >= 0)
                    {
                        result = ScanValueForGameMode(text, colonIdx + 1);
                        if (result > 0) return result;
                    }
                }
            }
        }
        catch { }
        return 0;
    }

    /// <summary>
    /// Quickly extract the SaveName from a save file without fully parsing it.
    /// Only reads and decompresses the first LZ4 block to scan for the SaveName key.
    /// Returns empty string if not found or on error.
    /// </summary>
    public static string DetectSaveNameFast(string filePath)
    {
        try
        {
            string? text = ReadFirstJsonBlock(filePath);
            if (string.IsNullOrEmpty(text)) return "";

            // Try both deobfuscated and obfuscated SaveName keys
            // "SaveName" or "Pk4" (obfuscated key for SaveName)
            return ExtractJsonStringValue(text, "\"SaveName\"")
                ?? ExtractJsonStringValue(text, "\"Pk4\"")
                ?? "";
        }
        catch { }
        return "";
    }

    /// <summary>
    /// Detect the game mode from an already-extracted JSON string (e.g. from a PS4 memory.dat slot).
    /// Returns 0 if the mode cannot be determined.
    /// </summary>
    public static int DetectGameModeFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        int result = ScanKeyForGameMode(json, "\"PresetGameMode\"");
        if (result <= 0) result = ScanKeyForGameMode(json, "\"pwt\"");
        if (result > 0) return result;

        // Modern saves store the mode as an integer on the context GameMode field
        // (BaseContext/ExpeditionContext).  Obfuscated saves use the key "idA";
        // plain-text saves use "GameMode".  Both keys are also used as container
        // objects (e.g. "idA":{"pwt":"Unspecified"}), so scan past those.
        if (result <= 0) result = ScanKeyForGameModeAll(json, "\"idA\"");
        if (result > 0) return result;
        if (result <= 0) result = ScanKeyForGameModeAll(json, "\"GameMode\"");
        if (result > 0) return result;

        int dsIdx = json.IndexOf("\"DifficultyState\"", StringComparison.Ordinal);
        if (dsIdx < 0) dsIdx = json.IndexOf("\"LyC\"", StringComparison.Ordinal);
        if (dsIdx >= 0)
        {
            int dpIdx = json.IndexOf("\"DifficultyPresetType\"", dsIdx, StringComparison.Ordinal);
            if (dpIdx < 0) dpIdx = json.IndexOf("\"7ND\"", dsIdx, StringComparison.Ordinal);
            if (dpIdx >= 0)
            {
                int colonIdx = json.IndexOf(':', dpIdx + 1);
                if (colonIdx >= 0)
                {
                    result = ScanValueForGameMode(json, colonIdx + 1);
                    if (result > 0) return result;
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// Extract the save name from an already-extracted JSON string (e.g. from a PS4 memory.dat slot).
    /// Returns empty string if not found.
    /// </summary>
    public static string DetectSaveNameFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";
        return ExtractJsonStringValue(json, "\"SaveName\"")
            ?? ExtractJsonStringValue(json, "\"Pk4\"")
            ?? "";
    }

    /// <summary>
    /// Reads and decompresses the first JSON block of a save file for fast header scanning.
    /// Handles LZ4-compressed, HGSAVEV2 (Xbox), PS4 NOMANSKY, and uncompressed formats.
    /// Returns the raw text content of the first block, or null on failure.
    /// </summary>
    private static string? ReadFirstJsonBlock(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
        try
        {
            byte[] header = new byte[16];
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Read(header, 0, 16) < 16) return null;

            if (header[0] == Lz4Magic[0] && header[1] == Lz4Magic[1] &&
                header[2] == Lz4Magic[2] && header[3] == Lz4Magic[3])
            {
                // LZ4 compressed - read only the first block
                int compressedLen = header[4] | (header[5] << 8) | (header[6] << 16) | (header[7] << 24);
                int uncompressedLen = header[8] | (header[9] << 8) | (header[10] << 16) | (header[11] << 24);
                if (compressedLen <= 0 || uncompressedLen <= 0) return null;
                if (compressedLen > 256 * 1024 * 1024 || uncompressedLen > 256 * 1024 * 1024) return null;

                byte[] compressedBlock = new byte[compressedLen];
                int totalRead = 0;
                while (totalRead < compressedLen)
                {
                    int n = fs.Read(compressedBlock, totalRead, compressedLen - totalRead);
                    if (n <= 0) break;
                    totalRead += n;
                }

                using var blockStream = new MemoryStream(compressedBlock, 0, totalRead);
                using var lz4Stream = new Lz4DecompressorStream(blockStream, uncompressedLen);
                byte[] decompressed = new byte[uncompressedLen];
                int read = 0;
                while (read < uncompressedLen)
                {
                    int n = lz4Stream.Read(decompressed, read, uncompressedLen - read);
                    if (n <= 0) break;
                    read += n;
                }
                return Latin1.GetString(decompressed, 0, read);
            }
            else if (IsHgsv2Header(header, 16))
            {
                // HGSAVEV2 format (post-Omega Xbox): decompress first frame
                return DecompressHgsv2FirstFrame(fs);
            }
            else if (IsNomanSkyHeader(header))
            {
                // PS4/PS5 NOMANSKY header. JSON starts at offset 0x70
                fs.Position = 0x70;
                int limit = (int)Math.Min(fs.Length - fs.Position, 64 * 1024);
                byte[] prefix = new byte[limit];
                int read = 0;
                while (read < limit)
                {
                    int n = fs.Read(prefix, read, limit - read);
                    if (n <= 0) break;
                    read += n;
                }
                return Encoding.UTF8.GetString(prefix, 0, read);
            }
            else
            {
                // Uncompressed - read a limited prefix
                int limit = (int)Math.Min(fs.Length, 64 * 1024);
                byte[] prefix = new byte[limit];
                fs.Position = 0;
                int read = 0;
                while (read < limit)
                {
                    int n = fs.Read(prefix, read, limit - read);
                    if (n <= 0) break;
                    read += n;
                }
                return Latin1.GetString(prefix, 0, read);
            }
        }
        catch { return null; }
    }

    /// <summary>
    /// Quickly detect whether a save file is an Expedition (Season) save.
    /// Returns true and sets isExpedition=true only if the first JSON block contains
    /// "ActiveContext":"Season" (or obfuscated form). Returns false otherwise.
    /// </summary>
    public static bool DetectActiveContextFast(string filePath, out bool isExpedition)
    {
        isExpedition = false;
        string? text = ReadFirstJsonBlock(filePath);
        if (string.IsNullOrEmpty(text)) return false;
        return TryDetectActiveContext(text, out isExpedition);
    }

    /// <summary>
    /// Detect the expedition flag from an already-extracted JSON string (used for PS4 memory.dat).
    /// </summary>
    public static bool DetectActiveContextFromJson(string json, out bool isExpedition)
    {
        isExpedition = false;
        if (string.IsNullOrEmpty(json)) return false;
        return TryDetectActiveContext(json, out isExpedition);
    }

    /// <summary>
    /// Common logic: scan text for ActiveContext (obfuscated key "XTp") and check if value is "Season".
    /// </summary>
    private static bool TryDetectActiveContext(string text, out bool isExpedition)
    {
        isExpedition = false;
        try
        {
            int idx = text.IndexOf("\"ActiveContext\"", StringComparison.Ordinal);
            if (idx < 0) idx = text.IndexOf("\"XTp\"", StringComparison.Ordinal);
            if (idx < 0) return false;
            int colonIdx = text.IndexOf(':', idx);
            if (colonIdx < 0) return false;
            int quoteStart = text.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) return false;
            int quoteEnd = text.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return false;
            string value = text.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
            isExpedition = string.Equals(value, "Season", StringComparison.Ordinal);
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Sets <see cref="SaveContext.IsExpeditionSave"/> based on the ActiveContext in an already-parsed
    /// JSON tree.  Resets to false when ActiveContext is not "Season".
    /// </summary>
    public static void TryDetectActiveContext(JsonObject data)
    {
        bool isExpedition = data != null && string.Equals(data.Get("ActiveContext") as string, "Season", StringComparison.Ordinal);
        SaveContext.SetExpedition(isExpedition);
    }

    /// <summary>
    /// Extract a JSON string value following a key in raw text.
    /// Returns null if the key is not found.
    /// </summary>
    private static string? ExtractJsonStringValue(string text, string key)
    {
        int idx = text.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return null;

        // Skip past key, find colon, then opening quote
        int colonIdx = text.IndexOf(':', idx + key.Length);
        if (colonIdx < 0) return null;

        int quoteStart = text.IndexOf('"', colonIdx + 1);
        if (quoteStart < 0) return null;

        // Find the closing quote, handling escape sequences
        int pos = quoteStart + 1;
        var sb = new StringBuilder();
        while (pos < text.Length)
        {
            char c = text[pos];
            if (c == '\\' && pos + 1 < text.Length)
            {
                char next = text[pos + 1];
                if (next == 'u' && pos + 5 < text.Length)
                {
                    // Unicode escape: \uXXXX
                    string hex = text.Substring(pos + 2, 4);
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                        sb.Append((char)codePoint);
                    else
                        sb.Append("\\u").Append(hex);
                    pos += 6;
                }
                else
                {
                    sb.Append(next switch { 'n' => '\n', 'r' => '\r', 't' => '\t', _ => next });
                    pos += 2;
                }
            }
            else if (c == '"')
            {
                break;
            }
            else
            {
                sb.Append(c);
                pos++;
            }
        }
        return sb.ToString();
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

    /// <summary>
    /// Scan for a JSON key in text and return its game mode integer value.
    /// Handles both numeric values (1-9) and string values ("Normal", etc).
    /// </summary>
    private static int ScanKeyForGameMode(string text, string key, int startFrom = 0)
    {
        int idx = startFrom > 0 ? startFrom : text.IndexOf(key, StringComparison.Ordinal);
        if (idx < 0) return 0;
        return ScanValueForGameMode(text, idx + key.Length);
    }

    /// <summary>
    /// Scans every occurrence of a JSON key and returns the first usable game mode
    /// integer value.  Modern saves use the GameMode key both as an integer field
    /// (e.g. "idA":1 on the context) and as a container key (e.g. "idA":{"pwt":
    /// "Unspecified"}), so a single first-occurrence scan can hit the container and
    /// miss the real mode.  Containers always yield 0, so the loop skips them.
    /// </summary>
    private static int ScanKeyForGameModeAll(string text, string key)
    {
        int idx = 0;
        while ((idx = text.IndexOf(key, idx, StringComparison.Ordinal)) >= 0)
        {
            int result = ScanValueForGameMode(text, idx + key.Length);
            if (result > 0) return result;
            idx += key.Length;
        }
        return 0;
    }

    /// <summary>
    /// Scan from a position past a JSON key colon for a game mode value (numeric or string).
    /// </summary>
    private static int ScanValueForGameMode(string text, int searchStart)
    {
        int valStart = -1;
        for (int i = searchStart; i < text.Length && i < searchStart + 20; i++)
        {
            char c = text[i];
            if (c >= '1' && c <= '9')
                return c - '0';
            if (c == '"')
            {
                valStart = i + 1;
                break;
            }
        }
        if (valStart >= 0)
        {
            int valEnd = text.IndexOf('"', valStart);
            if (valEnd > valStart)
            {
                string mode = text.Substring(valStart, valEnd - valStart);
                return GameModeStringToInt(mode);
            }
        }
        return 0;
    }
}
