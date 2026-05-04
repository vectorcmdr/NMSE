using System.Text.Json;
using NMSE.Core;

namespace NMSE.Tests;

public class UpdateServiceTests
{
    // ParseVersion

    [Theory]
    [InlineData("v1.2.3",       1, 2, 3)]
    [InlineData("1.2.3",        1, 2, 3)]
    [InlineData("NMSE v1.1.139", 1, 1, 139)]
    [InlineData("v0.0.1",       0, 0, 1)]
    [InlineData("v10.20.300",   10, 20, 300)]
    [InlineData("release-2.5.0", 2, 5, 0)]
    public void ParseVersion_ValidInput_ReturnsParsedVersion(
        string input, int major, int minor, int patch)
    {
        var v = UpdateService.ParseVersion(input);
        Assert.NotNull(v);
        Assert.Equal(major, v.Major);
        Assert.Equal(minor, v.Minor);
        Assert.Equal(patch, v.Build); // Version uses Build for third component
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-version-here")]
    [InlineData("v1")]
    [InlineData("v1.2")]
    public void ParseVersion_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(UpdateService.ParseVersion(input));
    }

    // IsNewer

    [Fact]
    public void IsNewer_RemoteIsNewer_ReturnsTrue()
    {
        var current = new Version(1, 1, 139);
        var remote  = new Version(1, 1, 140);
        Assert.True(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_RemoteIsSame_ReturnsFalse()
    {
        var current = new Version(1, 1, 139);
        var remote  = new Version(1, 1, 139);
        Assert.False(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_RemoteIsOlder_ReturnsFalse()
    {
        var current = new Version(1, 1, 139);
        var remote  = new Version(1, 1, 138);
        Assert.False(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_MajorVersionBump_ReturnsTrue()
    {
        var current = new Version(1, 9, 999);
        var remote  = new Version(2, 0, 0);
        Assert.True(UpdateService.IsNewer(current, remote));
    }

    [Fact]
    public void IsNewer_MinorVersionBump_ReturnsTrue()
    {
        var current = new Version(1, 1, 999);
        var remote  = new Version(1, 2, 0);
        Assert.True(UpdateService.IsNewer(current, remote));
    }

    // FindAssetDownloadUrl

    [Fact]
    public void FindAssetDownloadUrl_ValidRelease_ReturnsZipUrl()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140",
            "body": "Automated release",
            "assets": [
                {
                    "name": "NMSE-1.1.140-Release.zip",
                    "browser_download_url": "https://github.com/vectorcmdr/NMSE/releases/download/v1.1.140/NMSE-1.1.140-Release.zip"
                }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);
        string? url = UpdateService.FindAssetDownloadUrl(doc.RootElement);
        Assert.Equal(
            "https://github.com/vectorcmdr/NMSE/releases/download/v1.1.140/NMSE-1.1.140-Release.zip",
            url);
    }

    [Fact]
    public void FindAssetDownloadUrl_NoZipAsset_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140",
            "assets": [
                {
                    "name": "source.tar.gz",
                    "browser_download_url": "https://example.com/source.tar.gz"
                }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Null(UpdateService.FindAssetDownloadUrl(doc.RootElement));
    }

    [Fact]
    public void FindAssetDownloadUrl_EmptyAssets_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140",
            "assets": []
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Null(UpdateService.FindAssetDownloadUrl(doc.RootElement));
    }

    [Fact]
    public void FindAssetDownloadUrl_NoAssetsKey_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140"
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Null(UpdateService.FindAssetDownloadUrl(doc.RootElement));
    }

    [Fact]
    public void FindAssetDownloadUrl_MultipleAssets_ReturnsFirstZip()
    {
        string json = """
        {
            "tag_name": "v2.0.0",
            "assets": [
                {
                    "name": "checksums.txt",
                    "browser_download_url": "https://example.com/checksums.txt"
                },
                {
                    "name": "NMSE-2.0.0-Release.zip",
                    "browser_download_url": "https://example.com/NMSE-2.0.0-Release.zip"
                },
                {
                    "name": "NMSE-2.0.0-Debug.zip",
                    "browser_download_url": "https://example.com/NMSE-2.0.0-Debug.zip"
                }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("https://example.com/NMSE-2.0.0-Release.zip",
            UpdateService.FindAssetDownloadUrl(doc.RootElement));
    }

    // FindReleaseVersion

    [Fact]
    public void FindReleaseVersion_PrefersName()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": "NMSE v1.1.140"
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("NMSE v1.1.140", UpdateService.FindReleaseVersion(doc.RootElement));
    }

    [Fact]
    public void FindReleaseVersion_FallsBackToTagName()
    {
        string json = """
        {
            "tag_name": "v1.1.140",
            "name": ""
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("v1.1.140", UpdateService.FindReleaseVersion(doc.RootElement));
    }

    // FindReleaseNotes

    [Fact]
    public void FindReleaseNotes_ReturnsBody()
    {
        string json = """
        {
            "body": "Bug fixes and improvements."
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Bug fixes and improvements.", UpdateService.FindReleaseNotes(doc.RootElement));
    }

    [Fact]
    public void FindReleaseNotes_NoBody_ReturnsNull()
    {
        string json = """
        {
            "tag_name": "v1.1.140"
        }
        """;
        using var doc = JsonDocument.Parse(json);
        Assert.Null(UpdateService.FindReleaseNotes(doc.RootElement));
    }

    [Fact]
    public void FindReleaseNotes_BodyWithUnicodeChars_ReturnsString()
    {
        // System.Text.Json handles all Unicode correctly (emoji, accented letters,
        // en/em dashes, etc.) without any workarounds.
        string json = "{\"body\": \"Fix f\\u00FCr B\\u00FCg: includes \\u00E9l\\u00E9ment \\uD83D\\uDE80\"}";
        using var doc = JsonDocument.Parse(json);
        string? notes = UpdateService.FindReleaseNotes(doc.RootElement);
        Assert.NotNull(notes);
        Assert.Contains("f\u00FCr B\u00FCg", notes);
        Assert.Contains("\u00E9l\u00E9ment", notes);
        Assert.Contains("\U0001F680", notes); // 🚀 rocket emoji (SMP codepoint)
    }

    // CleanupOldExeIfPresent

    [Fact]
    public void CleanupOldExeIfPresent_StaleOldFileExists_DeletesIt()
    {
        string dir     = Path.Combine(Path.GetTempPath(), $"nmse-test-{Guid.NewGuid():N}");
        string oldFile = UpdateService.GetOldExePath(dir);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(oldFile, "fake old exe");

            UpdateService.CleanupOldExeIfPresent(dir);

            Assert.False(File.Exists(oldFile));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CleanupOldExeIfPresent_NoStaleFile_DoesNotThrow()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"nmse-test-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(dir);
            // Should not throw even when there is no .old file.
            UpdateService.CleanupOldExeIfPresent(dir);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void CleanupOldExeIfPresent_OtherFilesUntouched()
    {
        string dir       = Path.Combine(Path.GetTempPath(), $"nmse-test-{Guid.NewGuid():N}");
        string oldFile   = UpdateService.GetOldExePath(dir);
        // Create a sibling file that must NOT be deleted.
        string otherFile = oldFile.Replace(".old", ".keep", StringComparison.Ordinal);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(otherFile, "should stay");
            File.WriteAllText(oldFile,   "stale old exe");

            UpdateService.CleanupOldExeIfPresent(dir);

            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(otherFile));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    // ReleasesApiUrl

    [Fact]
    public void ReleasesApiUrl_ContainsOwnerAndRepo()
    {
        string url = UpdateService.ReleasesApiUrl;
        Assert.Contains(UpdateService.GitHubOwner, url);
        Assert.Contains(UpdateService.GitHubRepo, url);
        Assert.Contains("/releases/latest", url);
        Assert.StartsWith("https://api.github.com/repos/", url);
    }

    // Constants

    [Fact]
    public void Constants_AreConfigurable()
    {
        // Verify the constants exist and have non-empty values.
        // These are the values that would be changed for the final release repo.
        Assert.False(string.IsNullOrEmpty(UpdateService.GitHubOwner));
        Assert.False(string.IsNullOrEmpty(UpdateService.GitHubRepo));
    }

    // MarkdownToPlainText

    [Fact]
    public void MarkdownToPlainText_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, UpdateService.MarkdownToPlainText(null));
        Assert.Equal(string.Empty, UpdateService.MarkdownToPlainText(""));
        Assert.Equal(string.Empty, UpdateService.MarkdownToPlainText("   "));
    }

    [Fact]
    public void MarkdownToPlainText_H1Heading_UsesEqualsUnderline()
    {
        string result = UpdateService.MarkdownToPlainText("# My Heading");
        Assert.Contains("My Heading", result);
        Assert.Contains("==========", result);
    }

    [Fact]
    public void MarkdownToPlainText_H2Heading_UsesDashUnderline()
    {
        string result = UpdateService.MarkdownToPlainText("## Section");
        Assert.Contains("Section", result);
        Assert.Contains("-------", result);
    }

    [Fact]
    public void MarkdownToPlainText_H3Heading_UsesDashUnderline()
    {
        string result = UpdateService.MarkdownToPlainText("### Subsection");
        Assert.Contains("Subsection", result);
        Assert.Contains("----------", result);
    }

    [Fact]
    public void MarkdownToPlainText_UnorderedListDash_UsesBullet()
    {
        string result = UpdateService.MarkdownToPlainText("- Item one\n- Item two");
        Assert.Contains("• Item one", result);
        Assert.Contains("• Item two", result);
    }

    [Fact]
    public void MarkdownToPlainText_UnorderedListAsterisk_UsesBullet()
    {
        string result = UpdateService.MarkdownToPlainText("* Item");
        Assert.Contains("• Item", result);
    }

    [Fact]
    public void MarkdownToPlainText_Blockquote_Indented()
    {
        string result = UpdateService.MarkdownToPlainText("> Note text");
        Assert.Contains("  Note text", result);
        Assert.DoesNotContain("> ", result);
    }

    [Fact]
    public void MarkdownToPlainText_BoldText_Stripped()
    {
        string result = UpdateService.MarkdownToPlainText("Some **bold** text");
        Assert.Contains("Some bold text", result);
        Assert.DoesNotContain("**", result);
    }

    [Fact]
    public void MarkdownToPlainText_ItalicText_Stripped()
    {
        string result = UpdateService.MarkdownToPlainText("Some *italic* text");
        Assert.Contains("Some italic text", result);
        Assert.DoesNotContain("*italic*", result);
    }

    [Fact]
    public void MarkdownToPlainText_InlineCode_Stripped()
    {
        string result = UpdateService.MarkdownToPlainText("Run `dotnet build`");
        Assert.Contains("Run dotnet build", result);
        Assert.DoesNotContain("`", result);
    }

    [Fact]
    public void MarkdownToPlainText_Link_KeepsTextAndUrl()
    {
        string result = UpdateService.MarkdownToPlainText("[GitHub](https://github.com)");
        Assert.Contains("GitHub", result);
        Assert.Contains("https://github.com", result);
        Assert.DoesNotContain("](", result);
    }

    [Fact]
    public void MarkdownToPlainText_Link_UrlNotWrappedInParentheses()
    {
        // Win32 Rich Edit DetectUrls requires a bare URL - a leading '(' prevents detection.
        // The URL must appear as a plain token, not "(https://...)".
        string result = UpdateService.MarkdownToPlainText("[See issue](https://github.com/owner/repo/issues/1)");
        // The URL must appear without a leading '(' immediately before it.
        int urlIndex = result.IndexOf("https://", StringComparison.Ordinal);
        Assert.True(urlIndex >= 0);
        Assert.True(urlIndex == 0 || result[urlIndex - 1] != '(');
    }

    [Fact]
    public void MarkdownToPlainText_Link_LabelSameAsUrl_EmitsOnlyUrl()
    {
        // Auto-links where label == URL should not duplicate the URL.
        const string url = "https://github.com";
        string result = UpdateService.MarkdownToPlainText($"[{url}]({url})");
        Assert.Equal(url, result.Trim());
    }

    [Fact]
    public void MarkdownToPlainText_Image_KeepsAltOnly()
    {
        string result = UpdateService.MarkdownToPlainText("![logo](https://example.com/img.png)");
        Assert.Contains("logo", result);
        Assert.DoesNotContain("https://", result);
        Assert.DoesNotContain("](", result);
    }

    [Fact]
    public void MarkdownToPlainText_BrTag_BecomesNewline()
    {
        string result = UpdateService.MarkdownToPlainText("Line one<br />Line two");
        Assert.Contains("Line one", result);
        Assert.Contains("Line two", result);
        Assert.DoesNotContain("<br", result);
    }

    [Fact]
    public void MarkdownToPlainText_DetailsBlock_IsStripped()
    {
        string input = "Before\n<details>\n<summary>Old notes</summary>\nSecret content\n</details>\nAfter";
        string result = UpdateService.MarkdownToPlainText(input);
        Assert.Contains("Before", result);
        Assert.Contains("After", result);
        Assert.DoesNotContain("Secret content", result);
        Assert.DoesNotContain("<details>", result);
        Assert.DoesNotContain("<summary>", result);
    }

    [Fact]
    public void MarkdownToPlainText_GettingStartedSection_IsStripped()
    {
        string input = "## Changelog\n- Fix\n\n### Getting Started\nBoilerplate text here.";
        string result = UpdateService.MarkdownToPlainText(input);
        Assert.DoesNotContain("Boilerplate text here", result);
        Assert.DoesNotContain("Getting Started", result);
    }

    [Fact]
    public void MarkdownToPlainText_GettingStartedSection_KeepsContentBefore()
    {
        string input = "## Changelog\n- Fix important bug\n\n### Getting Started\nBoilerplate.";
        string result = UpdateService.MarkdownToPlainText(input);
        Assert.Contains("Fix important bug", result);
    }

    [Fact]
    public void MarkdownToPlainText_HtmlTags_AreStripped()
    {
        string result = UpdateService.MarkdownToPlainText("Text <b>bold</b> and <em>italic</em> here.");
        Assert.Contains("Text bold and italic here.", result);
        Assert.DoesNotContain("<b>", result);
        Assert.DoesNotContain("<em>", result);
    }

    [Fact]
    public void MarkdownToPlainText_HtmlComment_Stripped()
    {
        string result = UpdateService.MarkdownToPlainText("Before\n<!-- hidden -->\nAfter");
        Assert.Contains("Before", result);
        Assert.Contains("After", result);
        Assert.DoesNotContain("hidden", result);
        Assert.DoesNotContain("<!--", result);
    }

    [Fact]
    public void MarkdownToPlainText_MultilineHtmlComment_Stripped()
    {
        string input = "Line1\n<!--\nThis is\nhidden\n-->\nLine2";
        string result = UpdateService.MarkdownToPlainText(input);
        Assert.Contains("Line1", result);
        Assert.Contains("Line2", result);
        Assert.DoesNotContain("hidden", result);
    }

    [Fact]
    public void MarkdownToPlainText_ConsecutiveBlanks_Collapsed()
    {
        string input = "A\n\n\n\nB";
        string result = UpdateService.MarkdownToPlainText(input);
        // Should not contain more than one consecutive blank line
        Assert.DoesNotContain("\n\n\n", result);
    }

    [Fact]
    public void MarkdownToPlainText_Strikethrough_Stripped()
    {
        string result = UpdateService.MarkdownToPlainText("~~old text~~");
        Assert.Contains("old text", result);
        Assert.DoesNotContain("~~", result);
    }

    [Fact]
    public void MarkdownToPlainText_Trimmed_NoLeadingOrTrailingNewlines()
    {
        string result = UpdateService.MarkdownToPlainText("\n\n## Heading\n\n");
        Assert.False(result.StartsWith('\n'));
        Assert.False(result.EndsWith('\n'));
    }
}

public class BuildRtfWithIssueLinksTests
{
    private static string IssueUrl(int n)
        => $"https://github.com/{UpdateService.GitHubOwner}/{UpdateService.GitHubRepo}/issues/{n}";

    [Fact]
    public void BuildRtfWithIssueLinks_EmptyInput_ReturnsStub()
    {
        string result = UpdateService.BuildRtfWithIssueLinks(string.Empty);
        Assert.Contains("rtf1", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_NoReferences_ContainsOriginalText()
    {
        string result = UpdateService.BuildRtfWithIssueLinks("No issue references here.");
        Assert.Contains("No issue references here.", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_IssueReference_ContainsDisplayLabel()
    {
        // The displayed label (#64) must appear in the \fldrslt part.
        string result = UpdateService.BuildRtfWithIssueLinks("Fixed bug (Per Issue #64)");
        Assert.Contains("#64", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_IssueReference_ContainsFullUrl()
    {
        // The full URL must appear in the \fldinst HYPERLINK instruction.
        string result = UpdateService.BuildRtfWithIssueLinks("Fixed bug (Per Issue #64)");
        Assert.Contains(IssueUrl(64), result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_IssueReference_ContainsHyperlinkField()
    {
        string result = UpdateService.BuildRtfWithIssueLinks("See #1");
        Assert.Contains(@"\field", result);
        Assert.Contains("HYPERLINK", result);
        Assert.Contains(IssueUrl(1), result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_MultipleReferences_AllPresent()
    {
        string result = UpdateService.BuildRtfWithIssueLinks("See #1 and #2");
        Assert.Contains(IssueUrl(1), result);
        Assert.Contains(IssueUrl(2), result);
        Assert.Contains("#1", result);
        Assert.Contains("#2", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_HashInMiddleOfWord_NotTreatedAsRef()
    {
        // "#1test" - word-char after digits — must not produce a hyperlink field.
        string result = UpdateService.BuildRtfWithIssueLinks("tag v#1test");
        Assert.DoesNotContain("HYPERLINK", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_RtfSpecialChars_AreEscaped()
    {
        // Backslash and braces in plain text must be RTF-escaped.
        string result = UpdateService.BuildRtfWithIssueLinks(@"path\to\file {note}");
        Assert.Contains(@"\\", result);
        Assert.Contains(@"\{", result);
        Assert.Contains(@"\}", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_NonAsciiChar_IsUnicodeEscaped()
    {
        // Bullet U+2022 (8226) should be emitted as \u8226?
        string result = UpdateService.BuildRtfWithIssueLinks("• item");
        Assert.Contains(@"\u8226?", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_Newline_BecomesParBreak()
    {
        string result = UpdateService.BuildRtfWithIssueLinks("Line1\nLine2");
        Assert.Contains(@"\par", result);
    }

    [Fact]
    public void BuildRtfWithIssueLinks_Output_StartsWithRtfHeader()
    {
        string result = UpdateService.BuildRtfWithIssueLinks("hello");
        Assert.StartsWith(@"{\rtf1", result);
    }
}
