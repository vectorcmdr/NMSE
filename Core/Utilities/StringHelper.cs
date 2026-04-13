using System.Text;

namespace NMSE.Core.Utilities;

/// <summary>
/// Provides helper methods for normalising raw game enum/ID strings into human-readable
/// display text and sanitising values for file-system usage.
/// </summary>
internal static class StringHelper
{
    /// <summary>
    /// Collapse repeated whitespace characters to a single space and trim the result.
    /// </summary>
    internal static string CollapseWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var sb = new StringBuilder(value.Length);
        bool previousWasWhitespace = false;

        foreach (char c in value)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }
            }
            else
            {
                sb.Append(c);
                previousWasWhitespace = false;
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Sanitize a name for use in file names by replacing invalid chars and spaces with underscores.
    /// Returns "unnamed" for null, empty, or whitespace-only input.
    /// </summary>
    internal static string SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "unnamed";

        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Replace(' ', '_');
    }

    /// <summary>
    /// Joins the provided text fragments with the given separator, excluding null or whitespace-only segments.
    /// </summary>
    internal static string JoinNonEmpty(string separator, params string?[] values)
    {
        if (values == null || values.Length == 0)
            return "";

        return string.Join(separator, values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim()));
    }

    /// <summary>
    /// Performs an ordinal case-insensitive comparison.
    /// </summary>
    internal static bool EqualsOrdinalIgnoreCase(string? left, string? right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts an uppercase word to title case: "ATTACK" -> "Attack".
    /// </summary>
    private static string ToTitleWord(string word)
    {
        if (word.Length == 0) return "";
        if (word.Length == 1) return word.ToUpperInvariant();
        return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
    }

    /// <summary>
    /// Converts a text containing words separated by spaces, underscores, hyphens, or dots into title case.
    /// </summary>
    internal static string ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return string.Join(' ', value
            .Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ToTitleWord));
    }

    /// <summary>
    /// Normalises a raw game string for display. Handles:
    /// <list type="bullet">
    ///   <item>PascalCase: space-separated ("VeryLight" to "Very Light")</item>
    ///   <item>UPPER_SNAKE_CASE: Title Case ("ATTACK_NORM" to "Attack Norm")</item>
    ///   <item>MixedCase: space-separated ("ActiveEnemy" to "Active Enemy")</item>
    ///   <item>Null/empty: empty string</item>
    /// </list>
    /// </summary>
    internal static string NormalizeDisplayString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        // If all-uppercase with underscores: ATTACK_NORM -> Attack Norm
        if (value == value.ToUpperInvariant() && value.Contains('_'))
        {
            return string.Join(' ', value.Split('_')
                .Where(p => p.Length > 0)
                .Select(ToTitleWord));
        }

        // If all-uppercase without underscores: ATTACK -> Attack
        if (value == value.ToUpperInvariant() && value.Length > 1)
        {
            return ToTitleWord(value);
        }

        // PascalCase / camelCase: insert spaces before uppercase runs
        var sb = new StringBuilder(value.Length + 4);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '_')
            {
                sb.Append(' ');
                continue;
            }

            if (i > 0 && char.IsUpper(c))
            {
                char prev = value[i - 1];
                // Insert space before uppercase letter when preceded by lowercase
                if (char.IsLower(prev))
                    sb.Append(' ');
                // Insert space when uppercase is followed by lowercase
                else if (i + 1 < value.Length && char.IsLower(value[i + 1]) && char.IsUpper(prev) && prev != '_')
                    sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Truncates a string to the requested maximum length, optionally appending an ellipsis.
    /// </summary>
    internal static string Truncate(string? value, int maxLength, bool addEllipsis = false)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return "";

        if (value.Length <= maxLength)
            return value;

        if (!addEllipsis || maxLength <= 3)
            return value.Substring(0, maxLength);

        return value.Substring(0, maxLength - 3) + "...";
    }

}
