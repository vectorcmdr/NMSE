using System.Text;
using System.Text.RegularExpressions;

namespace NMSE.Core;

/// <summary>
/// Provides helpers for normalising raw game enum/ID strings into human-readable display text.
/// For example "VeryLight" → "Very Light", "ATTACK_NORM" → "Attack Norm", "ActiveEnemy" → "Active Enemy".
/// </summary>
internal static partial class DisplayStringHelper
{
    /// <summary>
    /// Normalises a raw game string for display. Handles:
    /// <list type="bullet">
    ///   <item>PascalCase → space-separated ("VeryLight" → "Very Light")</item>
    ///   <item>UPPER_SNAKE_CASE → Title Case ("ATTACK_NORM" → "Attack Norm")</item>
    ///   <item>Mixed → space-separated ("ActiveEnemy" → "Active Enemy")</item>
    ///   <item>Null/empty → empty string</item>
    /// </list>
    /// </summary>
    internal static string NormalizeDisplayString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        // If all-uppercase with underscores: ATTACK_NORM → Attack Norm
        if (value == value.ToUpperInvariant() && value.Contains('_'))
        {
            return string.Join(' ', value.Split('_')
                .Where(p => p.Length > 0)
                .Select(ToTitleWord));
        }

        // If all-uppercase without underscores: ATTACK → Attack
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
                // Insert space when uppercase is followed by lowercase (acronym boundary: "HTMLParser" → "HTML Parser")
                else if (i + 1 < value.Length && char.IsLower(value[i + 1]) && char.IsUpper(prev) && prev != '_')
                    sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts an uppercase word to title case: "ATTACK" → "Attack".
    /// </summary>
    private static string ToTitleWord(string word)
    {
        if (word.Length == 0) return "";
        if (word.Length == 1) return word.ToUpperInvariant();
        return char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();
    }
}
