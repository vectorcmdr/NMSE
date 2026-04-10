using NMSE.Extractor.Config;
using System.Diagnostics;

namespace NMSE.Extractor.Data;

public static class MbinConverter
{
    /// <summary>
    /// Enumerate files by extension (case-insensitive) to handle both .mbin and .MBIN.
    /// </summary>
    private static IEnumerable<string> EnumerateFilesByExtension(
        string directory, string extension, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(directory, "*", searchOption)
            .Where(f => f.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Consolidate extracted MBIN files into a single mbin/ directory.
    /// Uses File.Move (not Copy) to avoid duplicating data on disk.
    /// hgpaktool extracts files into an extracted/ subdirectory within the banks dir.
    /// Note: banks/extracted/ is NOT deleted here - textures remain for image extraction.
    /// Final cleanup of banks/ is handled by PakExtractor.CleanupBanksDir.
    /// </summary>
    public static void ConsolidateMbins(string resourcesDir, string banksDir)
    {
        string mbinDir = Path.Combine(resourcesDir, ExtractorConfig.MbinSubfolder);
        Directory.CreateDirectory(mbinDir);

        int moved = 0;
        var movedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // hgpaktool creates an extracted/ folder inside the banks directory.
        string banksExtracted = Path.Combine(banksDir, "extracted");

        // Search locations: banks/extracted/ first, then banks/ itself, then resourcesDir
        var searchRoots = new List<string>();

        if (Directory.Exists(banksExtracted))
            searchRoots.Add(banksExtracted);

        if (Directory.Exists(banksDir))
            searchRoots.Add(banksDir);

        // Fallback: check resourcesDir (in case files ended up here)
        searchRoots.Add(resourcesDir);

        foreach (string searchRoot in searchRoots)
        {
            if (!Directory.Exists(searchRoot)) continue;

            foreach (string file in EnumerateFilesByExtension(searchRoot, ".mbin", SearchOption.AllDirectories))
            {
                // Skip files already in our target mbin directory
                string? fileDir = Path.GetDirectoryName(file);
                if (fileDir != null && fileDir.Equals(mbinDir, StringComparison.OrdinalIgnoreCase))
                    continue;

                string filename = Path.GetFileName(file);
                if (!movedNames.Add(filename)) continue;

                string destFile = Path.Combine(mbinDir, filename);
                File.Move(file, destFile, overwrite: true);
                moved++;
                Console.WriteLine($"  [{moved}] {Path.GetRelativePath(searchRoot, file)} -> mbin/{filename}");
            }
        }

        Console.WriteLine(moved > 0
            ? $"[OK] Consolidated {moved} .mbin files into mbin/"
            : "[WARN] No .mbin files found after extraction");

        // Clean up known staging directories from resourcesDir (if files ended up here)
        string[] knownStagingDirs = ["METADATA", "LANGUAGE", "SIMULATION"];
        foreach (string sub in Directory.GetDirectories(resourcesDir))
        {
            string subName = Path.GetFileName(sub);
            if (!knownStagingDirs.Any(d => subName.Equals(d, StringComparison.OrdinalIgnoreCase)))
                continue;
            try
            {
                Directory.Delete(sub, recursive: true);
                Console.WriteLine($"  Removed staging dir {subName}/");
            }
            catch { /* best-effort cleanup */ }
        }
    }

    /// <summary>
    /// Convert all .mbin files in the mbin directory to .MXML using MBINCompiler.
    /// </summary>
    public static void ConvertMbinsToMxml(string mbinCompilerPath, string mbinDir)
    {
        if (!File.Exists(mbinCompilerPath))
            throw new FileNotFoundException($"MBINCompiler not found: {mbinCompilerPath}");

        var mbinFiles = EnumerateFilesByExtension(mbinDir, ".mbin", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Console.WriteLine($"[INFO] Converting {mbinFiles.Length} MBIN files to MXML...");

        int completed = 0;
        Parallel.ForEach(mbinFiles,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) },
            mbinFile =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = mbinCompilerPath,
                Arguments = $"\"{mbinFile}\"",
                WorkingDirectory = mbinDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.Error.WriteLine($"  [ERROR] Failed to start MBINCompiler for {Path.GetFileName(mbinFile)}");
                return;
            }
            process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            int count = Interlocked.Increment(ref completed);
            if (process.ExitCode != 0)
                Console.Error.WriteLine($"  [{count}/{mbinFiles.Length}] FAILED {Path.GetFileName(mbinFile)} (exit code {process.ExitCode}): {stderr.Trim()}");
            else
                Console.WriteLine($"  [{count}/{mbinFiles.Length}] Converted {Path.GetFileName(mbinFile)}");
        });

        // Verify expected outputs (case-insensitive check handles MBINCompiler output variations)
        var actualFiles = new HashSet<string>(
            Directory.GetFiles(mbinDir).Select(Path.GetFileName).OfType<string>(),
            StringComparer.OrdinalIgnoreCase);

        var missing = ExtractorConfig.ExpectedMxmlFiles
            .Where(name => !actualFiles.Contains(name))
            .ToList();

        if (missing.Count > 0)
        {
            Console.Error.WriteLine("[ERROR] Missing expected MXML outputs:");
            foreach (string name in missing)
                Console.Error.WriteLine($"  - {name}");
            throw new InvalidOperationException("MBIN to MXML conversion incomplete.");
        }

        // Check optional files (warn but don't abort)
        var optionalMissing = ExtractorConfig.OptionalMxmlFiles
            .Where(name => !actualFiles.Contains(name))
            .ToList();

        if (optionalMissing.Count > 0)
        {
            Console.WriteLine("[WARN] Optional MXML files not produced (MBINCompiler may not support these):");
            foreach (string name in optionalMissing)
                Console.WriteLine($"  - {name}");
        }

        Console.WriteLine("[OK] MBIN to MXML conversion complete.");
    }
}
