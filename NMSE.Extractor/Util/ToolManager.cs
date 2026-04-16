using NMSE.Extractor.Config;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;

namespace NMSE.Extractor.Util;

public static class ToolManager
{
    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NMSE-Extractor/1.0");
        return client;
    }

    // A separate client that follows redirects for actual downloads
    private static readonly HttpClient DownloadClient = new(new HttpClientHandler { AllowAutoRedirect = true })
    {
        DefaultRequestHeaders = { { "User-Agent", "NMSE-Extractor/1.0" } }
    };

    /// <summary>
    /// Resolves the latest release tag from a GitHub "/releases/latest/" URL
    /// by reading the Location header from the redirect response.
    /// </summary>
    public static async Task<string?> GetLatestReleaseTagAsync(string latestUrl)
    {
        try
        {
            var response = await Http.GetAsync(latestUrl);
            if (response.StatusCode is HttpStatusCode.Redirect
                or HttpStatusCode.MovedPermanently
                or HttpStatusCode.Found)
            {
                var location = response.Headers.Location?.ToString();
                if (!string.IsNullOrEmpty(location))
                {
                    // Location is like: https://github.com/.../releases/tag/v1.2.3
                    var tag = location.Split('/').Last();
                    return tag;
                }
            }
        }
        catch { /* swallow */ }
        return null;
    }

    private static string? ReadVersionFile(string versionFilePath)
    {
        return File.Exists(versionFilePath) ? File.ReadAllText(versionFilePath).Trim() : null;
    }

    private static void WriteVersionFile(string versionFilePath, string tag)
    {
        File.WriteAllText(versionFilePath, tag);
    }

    /// <summary>
    /// Ensures hgpaktool.exe is present and up-to-date in the tools directory.
    /// Downloads and extracts from the zip if needed.
    /// </summary>
    public static async Task EnsureHgPakToolAsync(string toolsDir)
    {
        Directory.CreateDirectory(toolsDir);
        string exePath = Path.Combine(toolsDir, "hgpaktool.exe");
        string versionFile = Path.Combine(toolsDir, "hgpaktool.version");

        string? latestTag = await GetLatestReleaseTagAsync(ExtractorConfig.HgPakToolLatestUrl);
        string? currentTag = ReadVersionFile(versionFile);

        if (File.Exists(exePath) && latestTag != null && latestTag == currentTag)
        {
            Console.WriteLine($"[OK] hgpaktool is up to date ({currentTag})");
            return;
        }

        Console.WriteLine($"[INFO] Downloading hgpaktool ({latestTag ?? "latest"})...");
        byte[] zipBytes = await DownloadClient.GetByteArrayAsync(ExtractorConfig.HgPakToolZipUrl);

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                string destPath = Path.Combine(toolsDir, entry.Name);
                entry.ExtractToFile(destPath, overwrite: true);
                Console.WriteLine($"  Extracted {entry.Name}");
            }
        }

        if (latestTag != null)
            WriteVersionFile(versionFile, latestTag);

        Console.WriteLine("[OK] hgpaktool ready.");
    }

    /// <summary>
    /// Ensures MBINCompiler.exe is present and up-to-date in the tools directory.
    /// </summary>
    public static async Task EnsureMbinCompilerAsync(string toolsDir)
    {
        Directory.CreateDirectory(toolsDir);
        string exePath = Path.Combine(toolsDir, "MBINCompiler.exe");
        string versionFile = Path.Combine(toolsDir, "MBINCompiler.version");

        string? latestTag = await GetLatestReleaseTagAsync(ExtractorConfig.MbinCompilerLatestUrl);
        string? currentTag = ReadVersionFile(versionFile);

        if (File.Exists(exePath) && latestTag != null && latestTag == currentTag)
        {
            Console.WriteLine($"[OK] MBINCompiler is up to date ({currentTag})");
            return;
        }

        Console.WriteLine($"[INFO] Downloading MBINCompiler ({latestTag ?? "latest"})...");
        byte[] exeBytes = await DownloadClient.GetByteArrayAsync(ExtractorConfig.MbinCompilerUrl);
        await File.WriteAllBytesAsync(exePath, exeBytes);

        if (latestTag != null)
            WriteVersionFile(versionFile, latestTag);

        Console.WriteLine("[OK] MBINCompiler ready.");
    }

    /// <summary>
    /// Ensures 7zr.exe is present in the tools directory.
    /// Downloads from the official 7-Zip GitHub releases if needed.
    /// </summary>
    public static async Task Ensure7zrAsync(string toolsDir)
    {
        Directory.CreateDirectory(toolsDir);
        string exePath = Path.Combine(toolsDir, "7zr.exe");
        string versionFile = Path.Combine(toolsDir, "7zr.version");

        string? latestTag = await GetLatestReleaseTagAsync(ExtractorConfig.SevenZipLatestUrl);
        string? currentTag = ReadVersionFile(versionFile);

        if (File.Exists(exePath) && latestTag != null && latestTag == currentTag)
        {
            Console.WriteLine($"[OK] 7zr is up to date ({currentTag})");
            return;
        }

        if (latestTag == null)
        {
            if (File.Exists(exePath))
            {
                Console.WriteLine("[OK] 7zr.exe already present (could not check latest version)");
                return;
            }
            throw new InvalidOperationException("Could not resolve latest 7-Zip release tag.");
        }

        string downloadUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, ExtractorConfig.SevenZipDownloadPattern, latestTag);
        Console.WriteLine($"[INFO] Downloading 7zr.exe ({latestTag})...");
        byte[] exeBytes = await DownloadClient.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync(exePath, exeBytes);

        WriteVersionFile(versionFile, latestTag);
        Console.WriteLine("[OK] 7zr.exe ready.");
    }

    /// <summary>
    /// Ensures ImageMagick portable is present and up-to-date in tools/imagemagick/.
    /// Downloads the portable .7z from GitHub releases and extracts using 7zr.exe.
    /// </summary>
    public static async Task EnsureImageMagickAsync(string toolsDir)
    {
        string imDir = Path.Combine(toolsDir, ExtractorConfig.ImageMagickSubfolder);
        Directory.CreateDirectory(imDir);
        string magickPath = Path.Combine(imDir, "magick.exe");
        string versionFile = Path.Combine(imDir, "imagemagick.version");

        string? latestTag = await GetLatestReleaseTagAsync(ExtractorConfig.ImageMagickLatestUrl);
        string? currentTag = ReadVersionFile(versionFile);

        if (File.Exists(magickPath) && latestTag != null && latestTag == currentTag)
        {
            Console.WriteLine($"[OK] ImageMagick is up to date ({currentTag})");
            return;
        }

        if (latestTag == null)
        {
            if (File.Exists(magickPath))
            {
                Console.WriteLine("[OK] ImageMagick already present (could not check latest version)");
                return;
            }
            throw new InvalidOperationException("Could not resolve latest ImageMagick release tag.");
        }

        // Ensure 7zr.exe is available for extraction
        string sevenZrPath = Path.Combine(toolsDir, "7zr.exe");
        if (!File.Exists(sevenZrPath))
            await Ensure7zrAsync(toolsDir);

        string downloadUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, ExtractorConfig.ImageMagickDownloadPattern, latestTag);
        string archivePath = Path.Combine(toolsDir, "imagemagick_portable.7z");

        Console.WriteLine($"[INFO] Downloading ImageMagick portable ({latestTag})...");
        byte[] archiveBytes = await DownloadClient.GetByteArrayAsync(downloadUrl);
        await File.WriteAllBytesAsync(archivePath, archiveBytes);

        // Extract using 7zr.exe
        Console.WriteLine("[INFO] Extracting ImageMagick...");
        var psi = new ProcessStartInfo
        {
            FileName = sevenZrPath,
            Arguments = $"x \"{archivePath}\" -o\"{imDir}\" -y",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start 7zr.exe");

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(120_000);

        // Clean up the downloaded archive
        try { File.Delete(archivePath); } catch { }

        if (!File.Exists(magickPath))
        {
            string errorDetail = !string.IsNullOrWhiteSpace(stderr) ? $" stderr: {stderr}" : "";
            throw new InvalidOperationException(
                $"ImageMagick extraction failed - magick.exe not found at {magickPath}.{errorDetail}");
        }

        WriteVersionFile(versionFile, latestTag);
        Console.WriteLine("[OK] ImageMagick ready.");
    }

    /// <summary>
    /// Downloads mapping.json from the MBINCompiler releases and saves to the map output directory.
    /// </summary>
    public static async Task DownloadMappingJsonAsync(string mapDir)
    {
        Directory.CreateDirectory(mapDir);
        string destPath = Path.Combine(mapDir, "mapping.json");

        Console.WriteLine("[INFO] Downloading mapping.json...");
        byte[] data = await DownloadClient.GetByteArrayAsync(ExtractorConfig.MappingJsonUrl);
        await File.WriteAllBytesAsync(destPath, data);
        Console.WriteLine("[OK] mapping.json saved.");
    }
}
