using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Provides self-update functionality by checking GitHub Releases for newer
/// versions of the application, downloading the release zip, and applying
/// the update in-place via a helper script that replaces the running binary
/// and Resources folder then relaunches.
/// </summary>
public static class UpdateService
{
    // Configurable release source
    // Change these constants when migrating to the final release repo.
    public const string GitHubOwner = "vectorcmdr";
    public const string GitHubRepo  = "NMSE";

    /// <summary>GitHub API URL for the latest published release.</summary>
    public static string ReleasesApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    // HttpClient (shared, long-lived)
    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var client  = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NMSE-Updater/1.0");
        // GitHub API requires Accept header
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    // Pure-logic helpers (unit-testable)

    /// <summary>
    /// Parses a semantic version from a tag string such as "v1.2.3",
    /// "1.2.3", or a release title like "NMSE v1.2.3".
    /// Returns <c>null</c> if the string contains no recognisable version.
    /// </summary>
    public static Version? ParseVersion(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Walk the string looking for the first digit sequence that
        // can be interpreted as major.minor.patch.
        var span = input.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            if (!char.IsDigit(span[i]))
                continue;

            // Found a digit – try to consume major.minor.patch
            int start = i;
            while (i < span.Length && (char.IsDigit(span[i]) || span[i] == '.'))
                i++;

            var candidate = span[start..i].ToString();
            var parts = candidate.Split('.');
            if (parts.Length >= 3
                && int.TryParse(parts[0], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int major)
                && int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int minor)
                && int.TryParse(parts[2], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int patch))
            {
                return new Version(major, minor, patch);
            }
        }

        return null;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="remote"/> is strictly
    /// newer than <paramref name="current"/>.
    /// </summary>
    public static bool IsNewer(Version current, Version remote)
        => remote.CompareTo(current) > 0;

    /// <summary>
    /// Extracts the first <c>.zip</c> asset download URL from a GitHub
    /// Releases API JSON response parsed with the built-in JSON engine.
    /// </summary>
    public static string? FindAssetDownloadUrl(JsonObject release)
    {
        var assets = release.GetArray("assets");
        if (assets == null)
            return null;

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets.Get(i) is not JsonObject asset)
                continue;

            var name = asset.GetString("name");
            var url  = asset.GetString("browser_download_url");

            if (name != null && url != null
                && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the release title (name) from a GitHub Releases API
    /// JSON response. Falls back to tag_name if name is empty.
    /// </summary>
    public static string? FindReleaseVersion(JsonObject release)
    {
        var name = release.GetString("name");
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return release.GetString("tag_name");
    }

    /// <summary>
    /// Extracts the release body (notes) from a GitHub Releases API
    /// JSON response.
    /// </summary>
    public static string? FindReleaseNotes(JsonObject release)
        => release.GetString("body");

    // Network methods

    /// <summary>
    /// Queries the GitHub Releases API and returns an <see cref="UpdateInfo"/>
    /// if a version newer than <paramref name="currentVersion"/> is available.
    /// Returns <c>null</c> when up-to-date or on any network/parse error.
    /// </summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync(Version currentVersion)
    {
        try
        {
            string json = await Http.GetStringAsync(ReleasesApiUrl).ConfigureAwait(false);
            var release = JsonObject.Parse(json);

            string? titleOrTag  = FindReleaseVersion(release);
            Version? remote     = ParseVersion(titleOrTag);
            string?  downloadUrl = FindAssetDownloadUrl(release);

            if (remote == null || downloadUrl == null)
                return null;

            if (!IsNewer(currentVersion, remote))
                return null;

            return new UpdateInfo(
                titleOrTag ?? remote.ToString(),
                remote,
                downloadUrl,
                FindReleaseNotes(release));
        }
        catch
        {
            // Swallow – update checks must never crash the app.
            return null;
        }
    }

    /// <summary>
    /// Downloads a file from <paramref name="url"/> to <paramref name="destPath"/>,
    /// reporting byte-level progress through <paramref name="progress"/>.
    /// Only allows downloads from GitHub (github.com) for security.
    /// </summary>
    public static async Task DownloadFileAsync(
        string url,
        string destPath,
        IProgress<(long received, long? total)>? progress = null)
    {
        // Security: only allow downloads from known GitHub domains to prevent redirect attacks.
        // EndsWith alone is insufficient (e.g. "fakegithub.com" would pass), so use exact matching.
        var uri = new Uri(url);
        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Download blocked: URL host '{uri.Host}' is not a known GitHub domain");

        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                                       .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        long  received   = 0;

        await using var httpStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write,
                                                     FileShare.None, 81920, useAsync: true);

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await httpStream.ReadAsync(buffer).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead)).ConfigureAwait(false);
            received += bytesRead;
            progress?.Report((received, totalBytes));
        }
    }

    // Update-in-place

    /// <summary>
    /// Generates the updater batch script content that will replace the
    /// running application with the contents of the extracted update.
    /// </summary>
    /// <remarks>Exposed as internal for unit testing.</remarks>
    internal static string GenerateUpdaterScript(
        int processId,
        string extractDir,
        string appDir,
        string exeName)
    {
        // The script:
        // 1. Waits for the current process to exit
        // 2. Removes old Resources/ folder to avoid stale assets
        // 3. Copies new files over the app directory
        // 4. Relaunches the application
        // 5. Cleans up temp files and self-deletes
        return $"""
            @echo off
            title NMSE Updater
            echo Waiting for NMSE to close...
            :wait
            tasklist /fi "PID eq {processId}" 2>nul | find /i "{processId}" >nul
            if not errorlevel 1 (
                timeout /t 1 /nobreak >nul
                goto wait
            )
            echo Applying update...
            if exist "{appDir}\Resources" rmdir /s /q "{appDir}\Resources"
            xcopy /e /y /q "{extractDir}\*" "{appDir}\" >nul
            echo Starting updated NMSE...
            start "" "{appDir}\{exeName}"
            echo Cleaning up...
            rmdir /s /q "{extractDir}" 2>nul
            (goto) 2>nul & del "%~f0"
            """;
    }

    /// <summary>
    /// Extracts the downloaded update zip, creates an updater batch script,
    /// launches it, then signals the caller to exit the application.
    /// </summary>
    /// <param name="zipPath">Path to the downloaded release zip.</param>
    /// <param name="appDir">
    /// Application directory (typically <c>AppContext.BaseDirectory</c>).
    /// </param>
    /// <returns><c>true</c> if the updater was launched successfully.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the update zip cannot be extracted or the updater script
    /// cannot be created/launched, with a user-friendly message.
    /// </exception>
    public static bool ApplyUpdateAndRelaunch(string zipPath, string? appDir = null)
    {
        appDir ??= AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        // Determine the EXE name from the running assembly, falling back to
        // Environment.ProcessPath and then a hardcoded default.  Deriving it
        // from assembly metadata means renaming the executable in a rebrand
        // or different distribution won't silently break the updater.
        string exeName = Path.GetFileName(
            Environment.ProcessPath
            ?? "NMSE.exe");

        // Extract zip to a temporary directory
        string extractDir = Path.Combine(Path.GetTempPath(), $"NMSE-update-{Guid.NewGuid():N}");
        try
        {
            ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException(
                $"The downloaded update archive is corrupt or incomplete: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Failed to extract update (disk full or permission denied): {ex.Message}", ex);
        }

        // Write the updater script
        string scriptPath = Path.Combine(Path.GetTempPath(), $"nmse-updater-{Guid.NewGuid():N}.bat");
        int pid = Environment.ProcessId;
        string script = GenerateUpdaterScript(pid, extractDir, appDir, exeName);
        File.WriteAllText(scriptPath, script);

        // Launch the updater script hidden.
        // If Process.Start returns null the updater failed to launch, and
        // the caller will exit the app.
        // This leavies the user with nothing running and no update applied.
        // Throw exception so the caller can show an error.
        // Will need to be fixed if we want to support non-Windows platforms (soon).
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName        = "cmd.exe",
            Arguments       = $"/c \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow  = true
        });
        if (proc == null)
            throw new InvalidOperationException(
                "Failed to launch the updater script. The update was not applied.");

        return true;
    }
}

/// <summary>Describes an available update from GitHub Releases.</summary>
public record UpdateInfo(
    string   Title,
    Version  RemoteVersion,
    string   DownloadUrl,
    string?  ReleaseNotes);
