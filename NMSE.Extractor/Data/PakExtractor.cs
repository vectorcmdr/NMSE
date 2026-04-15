using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NMSE.Extractor;

public static partial class PakExtractor
{
    /// <summary>
    /// Regex to parse "Unpacked N files" from hgpaktool stderr output.
    /// </summary>
    [GeneratedRegex(@"Unpacked\s+(\d+)\s+files?", RegexOptions.IgnoreCase)]
    private static partial Regex UnpackedCountRegex();

    /// <summary>
    /// Pak file type prefixes that are known to never contain MBIN game data or
    /// needed DDS textures. These are mesh, audio, animation, font, shader,
    /// pipeline, scene, and miscellaneous asset paks. Skipping them avoids
    /// copying multi-GB files only to find zero matching filters.
    /// </summary>
    private static readonly string[] IrrelevantPakPrefixes =
    [
        "ANIMMBIN",
        "AUDIO",
        "AUDIOBNK",
        "FONTS",
        "MESH",
        "MISC",
        "PIPELINES",
        "SCENES",
        "SHADERS",
        "UI",
    ];

    /// <summary>
    /// Determines whether a pak file could contain data we need (MBINs or DDS textures).
    /// Paks whose type segment matches a known irrelevant prefix are skipped.
    /// Everything else (MetadataEtc, Precache, globals, Tex*, hex-named, etc.) is processed.
    /// </summary>
    public static bool IsPakRelevant(string pakFileName)
    {
        string name = (Path.GetFileNameWithoutExtension(pakFileName) ?? "").ToUpperInvariant();
        // Extract the type segment after the first dot (e.g. "NMSARC.MeshCommon" -> "MESHCOMMON")
        int dot = name.IndexOf('.');
        if (dot < 0) return true; // no dot = unknown format, process it
        string type = name[(dot + 1)..];
        foreach (string prefix in IrrelevantPakPrefixes)
        {
            if (type.StartsWith(prefix, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Parse the "Unpacked N files" count from hgpaktool stderr output.
    /// Returns 0 if the pattern is not found.
    /// </summary>
    public static int ParseUnpackedCount(string? stderr)
    {
        if (string.IsNullOrEmpty(stderr)) return 0;
        var match = UnpackedCountRegex().Match(stderr);
        return match.Success && int.TryParse(match.Groups[1].Value, out int count) ? count : 0;
    }

    /// <summary>
    /// Write an in-place progress line, clearing any previous content on the line.
    /// Pads with spaces to the console width so shorter messages overwrite longer ones.
    /// </summary>
    public static void WriteProgress(string message)
    {
        int width;
        try { width = Console.WindowWidth - 1; }
        catch { width = 79; }
        if (width < 20) width = 79; // minimum usable width for progress messages
        Console.Write($"\r{message.PadRight(width)}");
    }

    /// <summary>
    /// Finish an in-place progress block by moving to a new line.
    /// </summary>
    public static void FinishProgress()
    {
        Console.WriteLine();
    }

    /// <summary>
    /// Extract filtered files from game .pak files, processing one pak at a time.
    /// For each pak: checks relevance, copies to banksDir, runs hgpaktool with pak-specific filters, removes the copy.
    /// Extracted content accumulates in banksDir/extracted/.
    /// Irrelevant paks (audio, mesh, fonts, shaders, etc.) are skipped entirely.
    /// Each pak receives only the filters relevant to its type (via getFiltersForPak) to avoid
    /// cross-contamination where unrelated filters could cause hgpaktool errors.
    /// </summary>
    public static void ExtractPerPak(string hgpaktoolPath, string pcbanksPath, string banksDir,
        Func<string, string[]> getFiltersForPak)
    {
        if (!File.Exists(hgpaktoolPath))
            throw new FileNotFoundException($"hgpaktool not found: {hgpaktoolPath}");
        if (!Directory.Exists(pcbanksPath))
            throw new DirectoryNotFoundException($"PCBANKS directory not found: {pcbanksPath}");

        Directory.CreateDirectory(banksDir);

        string[] allPakFiles = Directory.GetFiles(pcbanksPath, "*.pak");
        string[] pakFiles = allPakFiles.Where(p => IsPakRelevant(Path.GetFileName(p))).ToArray();
        int skippedCount = allPakFiles.Length - pakFiles.Length;
        int total = pakFiles.Length;
        int paksWithContent = 0;
        int totalEntries = 0;

        Console.WriteLine($"[INFO] Found {allPakFiles.Length} .pak files, {skippedCount} irrelevant skipped, processing {total}...");

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < pakFiles.Length; i++)
        {
            string srcPak = pakFiles[i];
            string pakName = Path.GetFileName(srcPak);
            long sizeMB = new FileInfo(srcPak).Length / (1024 * 1024);

            // Get pak-specific filters
            string[] pakFilters = getFiltersForPak(pakName);
            if (pakFilters.Length == 0)
            {
                WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) -> skipped (no applicable filters)");
                FinishProgress();
                continue;
            }

            WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) - extracting ({pakFilters.Length} filters)...");

            // Build filter args for this pak
            string filterArgStr = string.Join(" ", pakFilters.Select(f => $"-f=\"{f}\""));

            // Copy single pak to working directory
            string destPak = Path.Combine(banksDir, pakName);
            File.Copy(srcPak, destPak, overwrite: true);

            // Run hgpaktool with filters on just this pak
            string arguments = $"-U {filterArgStr} \"{banksDir}\"";
            var psi = new ProcessStartInfo
            {
                FileName = hgpaktoolPath,
                Arguments = arguments,
                WorkingDirectory = banksDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            int pakEntries = 0;
            using (var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start hgpaktool."))
            {
                var stderrTask = process.StandardError.ReadToEndAsync();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();

                string stdout = stdoutTask.GetAwaiter().GetResult();
                string stderr = stderrTask.GetAwaiter().GetResult();
                process.WaitForExit();

                // Parse actual count from hgpaktool output ("Unpacked N files")
                // Check both stderr and stdout as different hgpaktool versions vary
                pakEntries = ParseUnpackedCount(stderr);
                if (pakEntries == 0)
                    pakEntries = ParseUnpackedCount(stdout);

                if (process.ExitCode != 0 && pakEntries == 0)
                {
                    FinishProgress();
                    Console.WriteLine($"  [WARN] hgpaktool error (code {process.ExitCode}) for {pakName}");
                }
            }

            // Remove the pak file copy immediately (saves disk space)
            try { File.Delete(destPak); } catch { /* best-effort */ }

            totalEntries += pakEntries;
            if (pakEntries > 0)
            {
                paksWithContent++;
                WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) -> {pakEntries} files");
            }
            else
            {
                WriteProgress($"  [{i + 1}/{total}] {pakName} ({sizeMB} MB) -> no matching files");
            }
            FinishProgress();
        }

        Console.WriteLine($"[OK] Extracted {totalEntries} entries from {paksWithContent}/{total} .pak files ({sw.Elapsed.TotalSeconds:F0}s)");
    }

    /// <summary>
    /// Delete all .pak files from the local banks working directory.
    /// </summary>
    public static void CleanupPakFiles(string banksDir)
    {
        if (!Directory.Exists(banksDir)) return;

        string[] pakFiles = Directory.GetFiles(banksDir, "*.pak");
        if (pakFiles.Length == 0) return;

        int total = pakFiles.Length;
        int removed = 0;
        Console.WriteLine($"[INFO] Cleaning up {total} .pak files from banks directory...");

        for (int i = 0; i < pakFiles.Length; i++)
        {
            try
            {
                File.Delete(pakFiles[i]);
                removed++;
            }
            catch { /* best-effort cleanup */ }

            if ((i + 1) % 10 == 0 || i + 1 == total)
                WriteProgress($"  [{i + 1}/{total}] deleted");
        }

        FinishProgress();
        Console.WriteLine($"[OK] Cleaned up {removed}/{total} .pak files from banks directory");
    }

    /// <summary>
    /// Delete the entire banks working directory including all contents (pak files, extracted/, etc.).
    /// </summary>
    public static void CleanupBanksDir(string banksDir)
    {
        if (!Directory.Exists(banksDir)) return;

        try
        {
            Directory.Delete(banksDir, recursive: true);
            Console.WriteLine("[OK] Cleaned up banks directory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Could not fully clean banks directory: {ex.Message}");
            // Fallback: try to at least remove .pak files
            CleanupPakFiles(banksDir);
        }
    }

    /// <summary>
    /// Calculate the total size of .pak files in the PCBANKS directory.
    /// </summary>
    public static long GetPakFilesSize(string pcbanksPath)
    {
        return GetPakFilesSize(pcbanksPath, filter: null);
    }

    /// <summary>
    /// Calculate the total size of .pak files in the PCBANKS directory,
    /// optionally filtering to only include matching files.
    /// </summary>
    public static long GetPakFilesSize(string pcbanksPath, Func<string, bool>? filter)
    {
        if (!Directory.Exists(pcbanksPath)) return 0;

        long total = 0;
        foreach (string pakFile in Directory.GetFiles(pcbanksPath, "*.pak"))
        {
            if (filter != null && !filter(Path.GetFileName(pakFile))) continue;
            total += new FileInfo(pakFile).Length;
        }
        return total;
    }

    /// <summary>
    /// Estimate the maximum disk space consumed during extraction.
    /// With per-pak extraction, peak = largest single pak (temp copy) + filtered extracted content.
    /// Uses half the pak total size as a conservative upper bound for extracted content,
    /// since we extract less than 1% of content from most paks.
    /// </summary>
    public static long EstimateMaxStorageBytes(long pakFilesSize, long largestPakSize)
    {
        // Per-pak approach: only 1 pak on disk at a time + extracted content
        // Extracted content with filters is much smaller than total; use 0.5x pak size as safe upper bound
        return largestPakSize + (pakFilesSize / 2);
    }

    /// <summary>
    /// Get the size of the largest .pak file in the directory.
    /// </summary>
    public static long GetLargestPakFileSize(string pcbanksPath)
    {
        return GetLargestPakFileSize(pcbanksPath, filter: null);
    }

    /// <summary>
    /// Get the size of the largest .pak file in the directory,
    /// optionally filtering to only include matching files.
    /// </summary>
    public static long GetLargestPakFileSize(string pcbanksPath, Func<string, bool>? filter)
    {
        if (!Directory.Exists(pcbanksPath)) return 0;

        long largest = 0;
        foreach (string pakFile in Directory.GetFiles(pcbanksPath, "*.pak"))
        {
            if (filter != null && !filter(Path.GetFileName(pakFile))) continue;
            long size = new FileInfo(pakFile).Length;
            if (size > largest) largest = size;
        }
        return largest;
    }

}
