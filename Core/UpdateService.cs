using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

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
    public const string GitHubOwner = "vectorcmdr";
    public const string GitHubRepo  = "NMSE";

    /// <summary>GitHub API URL for the latest published release.</summary>
    public static string ReleasesApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    // HttpClient (shared, long-lived)
    private static readonly HttpClient Http = CreateHttpClient();

    /// <summary>
    /// Creates and configures the shared <see cref="HttpClient"/> used for
    /// all GitHub API calls. Sets the User-Agent and Accept headers required
    /// by the GitHub REST API.
    /// </summary>
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
    /// Releases API JSON element parsed with <see cref="System.Text.Json"/>.
    /// </summary>
    public static string? FindAssetDownloadUrl(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets)
            || assets.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            string? name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            string? url  = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;

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
    /// JSON element. Falls back to tag_name if name is empty.
    /// </summary>
    public static string? FindReleaseVersion(JsonElement release)
    {
        string? name = release.TryGetProperty("name", out var n) ? n.GetString() : null;
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        return release.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
    }

    /// <summary>
    /// Extracts the release body (notes) from a GitHub Releases API JSON element.
    /// </summary>
    public static string? FindReleaseNotes(JsonElement release)
    {
        return release.TryGetProperty("body", out var b) ? b.GetString() : null;
    }

    // Maximum number of characters for a heading underline produced by MarkdownToPlainText.
    // Caps very long headings to keep the output readable in a fixed-width RichTextBox.
    private const int MaxHeadingUnderlineLength = 60;

    /// <summary>
    /// Converts a Markdown string to readable plain text suitable for display
    /// in a WinForms <see cref="System.Windows.Forms.RichTextBox"/>.
    /// Handles the common patterns present in GitHub release notes without any
    /// third-party dependencies: ATX headings, unordered lists, blockquotes,
    /// inline emphasis, links (with URLs shown inline), inline code, strikethrough,
    /// HTML block comments, <c>&lt;details&gt;</c> blocks, and inline HTML tags.
    /// Also trims the boilerplate "Getting Started" section that appears in every release.
    /// </summary>
    /// <param name="markdown">Markdown text to convert, or <c>null</c>.</param>
    /// <returns>
    /// Readable plain text with Markdown syntax stripped, or an empty string
    /// if <paramref name="markdown"/> is <c>null</c> or whitespace.
    /// </returns>
    public static string MarkdownToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        string text = markdown;

        // Strip <details>...</details> blocks (contain folded older release notes)
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"<details[\s\S]*?</details>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Strip HTML block comments <!-- ... --> (including multiline)
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"<!--[\s\S]*?-->",
            string.Empty);

        // Trim the boilerplate "Getting Started" section and everything after it.
        // This heading (at any ATX level) marks the start of per-release boilerplate
        // that is redundant in the in-app dialog.
        var gsMatch = System.Text.RegularExpressions.Regex.Match(
            text,
            @"^#{1,6} Getting Started",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        if (gsMatch.Success)
            text = text[..gsMatch.Index];

        // Convert <br> / <br /> to a blank line
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"<br\s*/?>",
            "\n",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Strip remaining HTML tags (e.g. stray <summary>, <b>, <em>)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", string.Empty);

        var lines = text.Split('\n');
        var sb = new System.Text.StringBuilder(text.Length);
        bool prevBlank = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r', ' ');

            // ATX headings: one or more leading '#' followed by a space
            int hashes = 0;
            while (hashes < line.Length && line[hashes] == '#') hashes++;
            if (hashes > 0 && hashes < line.Length && line[hashes] == ' ')
            {
                string headingText = StripInlineMarkup(line[(hashes + 1)..].Trim());
                if (headingText.Length > 0)
                {
                    sb.AppendLine(headingText);
                    sb.AppendLine(new string(hashes == 1 ? '=' : '-', Math.Min(headingText.Length, MaxHeadingUnderlineLength)));
                }
                prevBlank = false;
                continue;
            }

            // Blockquotes: > text
            if (line.StartsWith("> ", StringComparison.Ordinal) || line == ">")
            {
                string quoteText = line.Length > 2 ? StripInlineMarkup(line[2..]) : string.Empty;
                sb.AppendLine("  " + quoteText);
                prevBlank = false;
                continue;
            }

            // Unordered list items: -, *, or + followed by a space (any indent)
            string trimmed = line.TrimStart();
            if (trimmed.Length >= 2
                && (trimmed[0] == '-' || trimmed[0] == '*' || trimmed[0] == '+')
                && trimmed[1] == ' ')
            {
                int indent = line.Length - trimmed.Length;
                string itemText = StripInlineMarkup(trimmed[2..]);
                sb.AppendLine(new string(' ', indent) + "• " + itemText);
                prevBlank = false;
                continue;
            }

            // Blank lines: collapse consecutive blanks to a single blank
            if (string.IsNullOrWhiteSpace(line))
            {
                if (!prevBlank)
                    sb.AppendLine();
                prevBlank = true;
                continue;
            }

            // Normal text line
            sb.AppendLine(StripInlineMarkup(line));
            prevBlank = false;
        }

        return sb.ToString().Trim('\r', '\n');
    }

    /// <summary>
    /// Strips inline Markdown markup from a single line of text, leaving only
    /// the visible content. Handles bold, italic, bold+italic, inline code,
    /// links (URL shown inline), images, and strikethrough.
    /// </summary>
    private static string StripInlineMarkup(string text)
    {
        // Inline code: `code`
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`([^`]*)`", "$1");
        // Bold+italic: ***text*** or ___text___
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{3}(.+?)\*{3}", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"_{3}(.+?)_{3}", "$1");
        // Bold: **text** or __text__
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{2}(.+?)\*{2}", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"_{2}(.+?)_{2}", "$1");
        // Italic: *text* or _text_ (word-boundary guard on _ to avoid false matches)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "$1");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\w)_(.+?)_(?!\w)", "$1");
        // Images: ![alt](url) -> alt only (image not displayable in a RichTextBox)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"!\[([^\]]*)\]\([^)]*\)", "$1");
        // Links: [text](url) -> "text url" (space-separated, no parentheses around the URL).
        // Win32 Rich Edit URL detection (DetectUrls) requires a bare URL not preceded by '(' -
        // wrapping in parens prevents it from being recognised as a clickable hyperlink.
        // If the label equals the URL (common for auto-links) or is empty, emit just the URL.
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\[([^\]]*)\]\(([^)]*)\)",
            m =>
            {
                string label = m.Groups[1].Value;
                string url   = m.Groups[2].Value;
                return string.IsNullOrWhiteSpace(label) || label == url ? url : $"{label} {url}";
            });
        // Strikethrough: ~~text~~
        text = System.Text.RegularExpressions.Regex.Replace(text, @"~~(.+?)~~", "$1");
        return text;
    }

    /// <summary>
    /// Converts plain-text release notes (output of <see cref="MarkdownToPlainText"/>) to an
    /// RTF document string in which <c>#N</c> issue/PR references are rendered as clickable
    /// hyperlinks that <em>display</em> as <c>#N</c> but navigate to the full GitHub issue
    /// URL when clicked.
    /// <para>
    /// GitHub's web UI auto-links bare <c>#N</c> tokens, but the raw API body contains only
    /// the token text.  This method embeds RTF <c>\field</c> hyperlink records so the short
    /// label is preserved in the rendered output while the full URL is stored in the hidden
    /// <c>\fldinst{HYPERLINK …}</c> instruction.  <see cref="System.Windows.Forms.RichTextBox"/>
    /// fires its <c>LinkClicked</c> event when such a field is clicked; the event argument
    /// carries the <em>displayed</em> text (e.g. <c>#64</c>), so the caller should resolve
    /// <c>#N</c> patterns back to a URL in the handler.
    /// </para>
    /// </summary>
    /// <param name="plainText">
    /// Plain text already converted by <see cref="MarkdownToPlainText"/>.
    /// </param>
    /// <returns>
    /// A complete RTF 1 document string with Segoe UI 9 pt body text and blue-underlined
    /// <c>#N</c> hyperlink fields, or a minimal <c>{\rtf1}</c> stub when
    /// <paramref name="plainText"/> is empty.
    /// </returns>
    public static string BuildRtfWithIssueLinks(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return @"{\rtf1}";

        var sb = new System.Text.StringBuilder(plainText.Length * 2);

        // RTF header: font table (Segoe UI) + color table.
        // Color index 0 = automatic/default; index 1 = Windows accent blue (#0078D4).
        sb.Append(@"{\rtf1\ansi\ansicpg1252\deff0");
        sb.Append(@"{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}}");
        sb.Append(@"{\colortbl ;\red0\green120\blue212;}");
        sb.Append(@"\f0\fs18 "); // Segoe UI, 9 pt (18 half-points)

        int i = 0;
        while (i < plainText.Length)
        {
            char c = plainText[i];

            // Detect standalone #N issue/PR references.
            // Guard: not preceded by a word character, followed by one or more digits,
            // and not followed by a word character.
            if (c == '#'
                && (i == 0 || !char.IsLetterOrDigit(plainText[i - 1]))
                && i + 1 < plainText.Length
                && char.IsDigit(plainText[i + 1]))
            {
                int numStart = i + 1;
                int j = numStart;
                while (j < plainText.Length && char.IsDigit(plainText[j]))
                    j++;

                if (j >= plainText.Length || !char.IsLetterOrDigit(plainText[j]))
                {
                    string issueNum = plainText[numStart..j];
                    string url      = $"https://github.com/{GitHubOwner}/{GitHubRepo}/issues/{issueNum}";
                    string display  = $"#{issueNum}";

                    // RTF hyperlink field: the \fldinst stores the destination URL;
                    // \fldrslt is the visible display text rendered as blue+underline.
                    sb.Append(@"{\field{\*\fldinst{HYPERLINK """);
                    sb.Append(url);   // URL contains only ASCII, no RTF escaping needed
                    sb.Append(@"""}}{\fldrslt{\cf1\ul ");
                    AppendRtfText(sb, display);
                    sb.Append(@"}}}");

                    i = j;
                    continue;
                }
            }

            // Line endings -> RTF paragraph break
            if (c == '\n')
            {
                sb.Append("\\par\n");
                i++;
                continue;
            }
            if (c == '\r')
            {
                i++;
                continue;
            }

            AppendRtfChar(sb, c);
            i++;
        }

        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>Appends a string to an RTF <see cref="System.Text.StringBuilder"/>, escaping special characters.</summary>
    private static void AppendRtfText(System.Text.StringBuilder sb, string text)
    {
        foreach (char c in text)
            AppendRtfChar(sb, c);
    }

    /// <summary>
    /// Appends a single character to an RTF <see cref="System.Text.StringBuilder"/>.
    /// RTF control characters (<c>\</c>, <c>{</c>, <c>}</c>) are escaped; non-ASCII
    /// code points are emitted as signed 16-bit Unicode escapes (<c>\uN?</c>).
    /// </summary>
    private static void AppendRtfChar(System.Text.StringBuilder sb, char c)
    {
        switch (c)
        {
            case '\\': sb.Append(@"\\");  break;
            case '{':  sb.Append(@"\{");  break;
            case '}':  sb.Append(@"\}");  break;
            default:
                if (c < 0x80)
                {
                    sb.Append(c);
                }
                else
                {
                    // RTF \uN escape: N is a signed 16-bit integer.
                    int signed = c < 0x8000 ? c : c - 0x10000;
                    sb.Append("\\u");
                    sb.Append(signed);
                    sb.Append('?'); // ASCII fallback character
                }
                break;
        }
    }

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
            using var doc = JsonDocument.Parse(json);
            var release = doc.RootElement;

            string? titleOrTag   = FindReleaseVersion(release);
            Version? remote      = ParseVersion(titleOrTag);
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
    /// Deletes the stale <c>.old</c> copy of the executable left behind by a
    /// previous self-update, if one exists, and removes any <c>.old</c>
    /// sentinel files left in the application directory tree by the rename-
    /// before-copy strategy used during update.  Safe to call unconditionally
    /// on every application startup; silently ignores failures.
    /// </summary>
    /// <param name="appDir">
    /// Application directory to inspect.  Defaults to
    /// <c>AppContext.BaseDirectory</c> when <c>null</c>.
    /// </param>
    public static void CleanupOldExeIfPresent(string? appDir = null)
    {
        string oldExePath = GetOldExePath(appDir);

        try
        {
            if (File.Exists(oldExePath))
                File.Delete(oldExePath);
        }
        catch
        {
            // Non-fatal.
        }

        // Also remove .old files left by the rename-then-copy pass
        // (e.g. Resources\UI-GLYPH1.PNG.old).
        appDir ??= AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        CleanupOldFilesRecursive(appDir);
    }

    /// <summary>
    /// Recursively deletes every <c>*.old</c> file under
    /// <paramref name="dir"/>, silently skipping any that cannot be removed.
    /// </summary>
    private static void CleanupOldFilesRecursive(string dir)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(dir, "*.old",
                         SearchOption.AllDirectories))
            {
                try { File.Delete(file); }
                catch (IOException) { /* File still locked: skip. */ }
            }
        }
        catch (IOException) { /* Directory not accessible: skip. */ }
    }

    /// <summary>
    /// Returns the full path of the <c>.old</c> file that
    /// <see cref="CleanupOldExeIfPresent"/> targets.  Exposed as
    /// <c>internal</c> so unit tests can create a sentinel file at the
    /// exact same path without duplicating the derivation logic.
    /// </summary>
    internal static string GetOldExePath(string? appDir = null)
    {
        appDir ??= AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string exeName = Path.GetFileName(Environment.ProcessPath ?? "NMSE.exe");
        return Path.Combine(appDir, exeName + ".old");
    }
    /// <summary>
    /// Extracts the downloaded update zip and applies it in-place using a
    /// pure BCL rename-and-copy approach, then relaunches the updated
    /// executable.  No batch scripts, no child shell processes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Windows (and WINE) allow renaming a running EXE because the OS holds
    /// a reference to the file data through a memory-mapped section object,
    /// not through the directory entry.  The running process continues
    /// executing after <c>NMSE.exe -> NMSE.exe.old</c>; the freed name is
    /// then used to place the new binary.  <see cref="CleanupOldExeIfPresent"/>
    /// removes the <c>.old</c> file on the next successful startup.
    /// </para>
    /// <para>Two-phase write: the new EXE is first staged as
    /// <c>NMSE.exe.new</c> before the rename swap, so a power loss between
    /// steps leaves the old binary intact at its original path.</para>
    /// </remarks>
    /// <param name="zipPath">Path to the downloaded release zip.</param>
    /// <param name="appDir">
    /// Application directory (typically <c>AppContext.BaseDirectory</c>).
    /// </param>
    /// <returns><c>true</c> if the updated application was relaunched.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the zip is corrupt, a file operation fails, or the
    /// relaunched process cannot be started.
    /// </exception>
    public static bool ApplyUpdateAndRelaunch(string zipPath, string? appDir = null)
    {
        appDir ??= AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        // Determine the EXE name from the running process.  Environment.ProcessPath
        // is the most reliable source in Native AOT scenarios (Assembly.Location
        // is empty for single-file/AOT apps).
        string exeName = Path.GetFileName(Environment.ProcessPath ?? "NMSE.exe");

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

        string exePath     = Path.Combine(appDir, exeName);
        string exePathOld  = exePath + ".old";
        string exePathNew  = exePath + ".new";
        string newExeInZip = Path.Combine(extractDir, exeName);
        bool   hasNewExe   = File.Exists(newExeInZip);

        // NOTE: Directory.Delete is intentionally NOT used here.  Deleting
        // the Resources folder while the process is running risks losing assets
        // mid-copy on error.  CopyDirectory overwrites files individually;
        // the rename-then-copy pattern is kept as a safety net for any file
        // that may be opened with a sharing restriction.

        if (hasNewExe)
        {
            // Two-phase swap: stage -> rename old -> rename new into place.
            // If power is lost after staging but before the rename, the old
            // binary is still intact at its original path.
            try
            {
                File.Copy(newExeInZip, exePathNew, overwrite: true);
                File.Move(exePath, exePathOld, overwrite: true);
                File.Move(exePathNew, exePath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Update could not be applied: failed to swap the executable. " +
                    $"The original executable may have been preserved at '{exePathOld}' " +
                    $"or remains at its original location. Reason: {ex.Message}", ex);
            }
        }

        // Copy all remaining files (skip the exe - already handled above).
        CopyDirectory(extractDir, appDir, skipFileName: hasNewExe ? exeName : null);

        // Relaunch the updated application then let the caller exit.
        Process? proc;
        try
        {
            proc = Process.Start(new ProcessStartInfo
            {
                FileName        = exePath,
                UseShellExecute = false
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"The update was applied but the application could not be relaunched: {ex.Message}", ex);
        }

        if (proc == null)
            throw new InvalidOperationException(
                "The update was applied but the application could not be relaunched.");

        // Best-effort: clean up the temp extract directory.
        try { Directory.Delete(extractDir, recursive: true); }
        catch (IOException) { /* Non-fatal: OS will reclaim temp space. */ }

        return true;
    }

    /// <summary>
    /// Recursively copies <paramref name="sourceDir"/> into
    /// <paramref name="destDir"/>, overwriting existing files.
    /// An optional <paramref name="skipFileName"/> (compared
    /// case-insensitively) is excluded at the top level only.
    /// </summary>
    /// <remarks>
    /// Uses a rename-then-copy approach for each destination file:
    /// if the destination already exists it is first renamed to
    /// <c>filename.old</c>, then the new file is copied in.  Renaming
    /// before overwriting is a safety net for files that may have a
    /// sharing restriction (e.g. the running EXE, or any file another
    /// process has opened without <c>FILE_SHARE_WRITE</c>).
    /// Leftover <c>.old</c> files are cleaned up on the next startup
    /// by <see cref="CleanupOldExeIfPresent"/>.
    /// </remarks>
    private static void CopyDirectory(string sourceDir, string destDir, string? skipFileName = null)
    {
        Directory.CreateDirectory(destDir);

        foreach (string srcFile in Directory.EnumerateFiles(sourceDir))
        {
            string fileName = Path.GetFileName(srcFile);
            if (skipFileName != null
                && fileName.Equals(skipFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            string destFile = Path.Combine(destDir, fileName);

            // Rename any existing file to .old before overwriting.
            // This works even when the file has open handles (locked images,
            // etc.) because Windows resolves open handles via the file object,
            // not the directory entry.  The renamed .old copy is cleaned up by
            // CleanupOldFilesRecursive on the next startup.
            if (File.Exists(destFile))
            {
                try { File.Move(destFile, destFile + ".old", overwrite: true); }
                catch (IOException) { /* If rename also fails, let Copy throw. */ }
            }

            File.Copy(srcFile, destFile, overwrite: true);
        }

        foreach (string dir in Directory.EnumerateDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(dir);
            CopyDirectory(dir, Path.Combine(destDir, dirName));
        }
    }
}

/// <summary>Describes an available update from GitHub Releases.</summary>
public record UpdateInfo(
    string   Title,
    Version  RemoteVersion,
    string   DownloadUrl,
    string?  ReleaseNotes);
