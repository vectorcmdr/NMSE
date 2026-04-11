using NMSE.Models;

namespace NMSE.Core;

/// <summary>
/// Provides utilities for parsing, formatting, and displaying raw JSON save data.
/// </summary>
internal static class RawJsonLogic
{
    /// <summary>
    /// Parses and reformats a JSON string with consistent indentation.
    /// </summary>
    /// <param name="jsonText">The raw JSON text to format.</param>
    /// <returns>The formatted JSON string.</returns>
    internal static string FormatJson(string jsonText)
    {
        var obj = JsonObject.Parse(jsonText);
        return obj.ToFormattedString();
    }

    /// <summary>
    /// Parses a JSON string into a <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="jsonText">The raw JSON text to parse.</param>
    /// <returns>The parsed JSON object.</returns>
    internal static JsonObject ParseJson(string jsonText)
    {
        return JsonObject.Parse(jsonText);
    }

    /// <summary>
    /// Converts save data to a human-readable display string.
    /// </summary>
    /// <param name="saveData">The save data JSON object.</param>
    /// <returns>A display-friendly string representation of the save data.</returns>
    internal static string ToDisplayString(JsonObject saveData)
    {
        return saveData.ToDisplayString();
    }

    /// <summary>
    /// Serializes any JSON value (object, array, or primitive) to a formatted
    /// display string with human-readable (deobfuscated) keys.
    /// </summary>
    internal static string SerializeValue(object? value)
    {
        return JsonParser.Serialize(value, formatted: true, skipReverseMapping: true);
    }

    /// <summary>
    /// Parses a JSON string into a typed value (JsonObject, JsonArray, or primitive).
    /// </summary>
    internal static object? ParseValue(string jsonText)
    {
        return JsonParser.ParseValue(jsonText);
    }

    /// <summary>
    /// Formats a JSON value for display in an edit dialog.
    /// Unlike tree-display formatting, this does NOT wrap strings in quotation marks
    /// so users edit the raw value without needing to handle surrounding quotes.
    /// </summary>
    internal static string FormatValueForEdit(object? value) => value switch
    {
        null => "null",
        string s => s,
        bool b => b ? "true" : "false",
        BinaryData bd => $"<binary:{bd.ToHexString()}>",
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "null",
        _ => value.ToString() ?? "null"
    };

    /// <summary>
    /// Parses user input from the edit dialog back into a typed value.
    /// When <paramref name="originalValue"/> is a <see cref="string"/>, the input is always
    /// returned as a string to preserve the original type and prevent corruption from
    /// values like "true", "42", or "null" being reinterpreted as non-string types.
    /// </summary>
    internal static object? ParseInputValue(string input, object? originalValue)
    {
        // If the original value was a string, always return the input as a string.
        // This prevents type corruption when editing string values that happen to
        // look like booleans, numbers, or null (e.g. "true", "42", "null").
        if (originalValue is string)
            return input;

        if (input == "null") return null;
        if (input == "true") return true;
        if (input == "false") return false;
        if (long.TryParse(input, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out long l))
            return l >= int.MinValue && l <= int.MaxValue ? (int)l : l;
        if (double.TryParse(input, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
            return d;
        return input;
    }

    /// <summary>
    /// Overload for adding new values where there is no original value to preserve.
    /// Applies standard type inference (null, bool, number, string).
    /// </summary>
    internal static object? ParseInputValue(string input) => ParseInputValue(input, null);

    /// <summary>
    /// Computes a simple line-by-line diff between two JSON strings.
    /// Lines only in the original are prefixed with "- ", lines only in the
    /// current version are prefixed with "+ ", unchanged lines with "  ".
    /// Uses the Myers diff algorithm for minimal, correct diffs.
    /// </summary>
    internal static string ComputeSimpleDiff(string original, string current)
    {
        if (original == current)
            return "No changes detected.";

        var rawDiff = ComputeRawDiff(original.Split('\n'), current.Split('\n'));

        var sb = new System.Text.StringBuilder();
        foreach (var (type, text) in rawDiff)
        {
            switch (type)
            {
                case DiffLineType.Added:   sb.Append("+ ").AppendLine(text); break;
                case DiffLineType.Removed: sb.Append("- ").AppendLine(text); break;
                default:                   sb.Append("  ").AppendLine(text); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>The type of a diff line: unchanged context, added, removed, or a hunk separator.</summary>
    internal enum DiffLineType { Context, Added, Removed, Separator }

    /// <summary>A single line in a compact diff output.</summary>
    internal readonly record struct DiffLine(DiffLineType Type, string Text);

    /// <summary>
    /// Computes a compact diff showing only changed hunks with surrounding context lines.
    /// Unchanged regions between hunks are collapsed into a single separator line.
    /// This avoids building a huge string for large files where only a few values changed.
    /// Uses the Myers diff algorithm for minimal, correct diffs.
    /// </summary>
    /// <param name="original">The original JSON text.</param>
    /// <param name="current">The current (modified) JSON text.</param>
    /// <param name="contextLines">Number of unchanged lines to show before/after each change.</param>
    /// <returns>A list of diff lines. Empty list means no changes.</returns>
    internal static List<DiffLine> ComputeCompactDiff(string original, string current, int contextLines = 3)
    {
        if (original == current)
            return [];

        var rawDiff = ComputeRawDiff(original.Split('\n'), current.Split('\n'));

        // Phase 2: Collapse unchanged regions, keeping only contextLines around changes
        return CollapseContext(rawDiff, contextLines);
    }

    /// <summary>
    /// Collapses large unchanged regions in a diff, keeping only <paramref name="contextLines"/>
    /// lines before and after each changed hunk. Collapsed regions become a Separator line.
    /// </summary>
    internal static List<DiffLine> CollapseContext(List<(DiffLineType Type, string Text)> rawDiff, int contextLines)
    {
        // Mark which lines are "near" a change
        var nearChange = new bool[rawDiff.Count];
        for (int k = 0; k < rawDiff.Count; k++)
        {
            if (rawDiff[k].Type != DiffLineType.Context)
            {
                // Mark contextLines before and after this change line
                int start = Math.Max(0, k - contextLines);
                int end = Math.Min(rawDiff.Count - 1, k + contextLines);
                for (int m = start; m <= end; m++)
                    nearChange[m] = true;
            }
        }

        var result = new List<DiffLine>();
        bool inSkip = false;

        for (int k = 0; k < rawDiff.Count; k++)
        {
            if (nearChange[k])
            {
                if (inSkip)
                {
                    result.Add(new DiffLine(DiffLineType.Separator, ""));
                    inSkip = false;
                }
                result.Add(new DiffLine(rawDiff[k].Type, rawDiff[k].Text));
            }
            else
            {
                inSkip = true;
            }
        }

        return result;
    }

    #region Myers Diff Algorithm

    /// <summary>
    /// Maximum edit distance before falling back to a simple all-removed/all-added diff.
    /// Guards against pathological inputs where the two files are completely different.
    /// </summary>
    private const int MaxDiffDistance = 5000;

    /// <summary>
    /// Computes the raw diff between two line arrays using the Myers diff algorithm
    /// with common prefix/suffix trimming for efficiency.
    /// Produces the minimal edit script, correctly handling duplicate lines like
    /// <c>},</c>, <c>{</c>, <c>]</c> that are common in JSON.
    /// </summary>
    internal static List<(DiffLineType Type, string Text)> ComputeRawDiff(string[] oldLines, string[] newLines)
    {
        // Normalize line endings once
        var oldNorm = new string[oldLines.Length];
        var newNorm = new string[newLines.Length];
        for (int i = 0; i < oldLines.Length; i++) oldNorm[i] = oldLines[i].TrimEnd('\r');
        for (int i = 0; i < newLines.Length; i++) newNorm[i] = newLines[i].TrimEnd('\r');

        // Trim common prefix
        int prefix = 0;
        while (prefix < oldNorm.Length && prefix < newNorm.Length && oldNorm[prefix] == newNorm[prefix])
            prefix++;

        // Trim common suffix
        int suffix = 0;
        while (suffix < oldNorm.Length - prefix && suffix < newNorm.Length - prefix &&
               oldNorm[oldNorm.Length - 1 - suffix] == newNorm[newNorm.Length - 1 - suffix])
            suffix++;

        var result = new List<(DiffLineType, string)>();

        // Add common prefix as context
        for (int i = 0; i < prefix; i++)
            result.Add((DiffLineType.Context, oldNorm[i]));

        // Compute diff on the middle (non-matching) section
        int oldMidLen = oldNorm.Length - prefix - suffix;
        int newMidLen = newNorm.Length - prefix - suffix;

        if (oldMidLen > 0 || newMidLen > 0)
        {
            var oldMid = new string[oldMidLen];
            var newMid = new string[newMidLen];
            Array.Copy(oldNorm, prefix, oldMid, 0, oldMidLen);
            Array.Copy(newNorm, prefix, newMid, 0, newMidLen);

            result.AddRange(MyersDiff(oldMid, newMid));
        }

        // Add common suffix as context
        for (int i = oldNorm.Length - suffix; i < oldNorm.Length; i++)
            result.Add((DiffLineType.Context, oldNorm[i]));

        return result;
    }

    /// <summary>
    /// Myers diff algorithm - computes the shortest edit script (minimal diff) between
    /// two arrays of pre-normalized lines. O(N*D) time where D is the edit distance.
    /// </summary>
    private static List<(DiffLineType Type, string Text)> MyersDiff(string[] oldArr, string[] newArr)
    {
        int N = oldArr.Length;
        int M = newArr.Length;

        if (N == 0 && M == 0) return [];
        if (N == 0) return newArr.Select(l => (DiffLineType.Added, l)).ToList();
        if (M == 0) return oldArr.Select(l => (DiffLineType.Removed, l)).ToList();

        int max = N + M;
        int offset = max + 1;
        int vSize = 2 * max + 3;

        var V = new int[vSize];
        Array.Fill(V, -1);
        V[1 + offset] = 0;

        // Save snapshots of V at the start of each d-step for backtracking
        var history = new List<int[]>();

        for (int d = 0; d <= Math.Min(max, MaxDiffDistance); d++)
        {
            history.Add((int[])V.Clone());

            for (int k = -d; k <= d; k += 2)
            {
                int x;
                if (k == -d || (k != d && V[k - 1 + offset] < V[k + 1 + offset]))
                    x = V[k + 1 + offset];       // Down (insert from new)
                else
                    x = V[k - 1 + offset] + 1;   // Right (delete from old)

                int y = x - k;

                // Follow diagonal (matching lines)
                while (x < N && y < M && oldArr[x] == newArr[y])
                {
                    x++;
                    y++;
                }

                V[k + offset] = x;

                if (x >= N && y >= M)
                    return BacktrackMyers(history, d, oldArr, newArr, offset);
            }
        }

        // Exceeded MaxDiffDistance - fall back to all-removed + all-added
        var fallback = new List<(DiffLineType, string)>();
        foreach (var l in oldArr) fallback.Add((DiffLineType.Removed, l));
        foreach (var l in newArr) fallback.Add((DiffLineType.Added, l));
        return fallback;
    }

    /// <summary>
    /// Backtracks through saved Myers algorithm states to reconstruct the edit script.
    /// </summary>
    private static List<(DiffLineType Type, string Text)> BacktrackMyers(
        List<int[]> history, int D, string[] oldArr, string[] newArr, int offset)
    {
        var result = new List<(DiffLineType, string)>();
        int x = oldArr.Length, y = newArr.Length;

        for (int d = D; d > 0; d--)
        {
            var Vprev = history[d]; // V state at start of step d (= end of step d-1)
            int k = x - y;

            // Determine which diagonal we came from in step d-1
            int prevK;
            if (k == -d || (k != d && Vprev[k - 1 + offset] < Vprev[k + 1 + offset]))
                prevK = k + 1; // Came from above (down = insert)
            else
                prevK = k - 1; // Came from left (right = delete)

            int prevX = Vprev[prevK + offset];
            int prevY = prevX - prevK;

            // Compute where the edit step lands
            int editX, editY;
            if (prevK == k + 1)
            {
                // Down move: x unchanged, y increased by 1
                editX = prevX;
                editY = prevY + 1;
            }
            else
            {
                // Right move: x increased by 1, y unchanged
                editX = prevX + 1;
                editY = prevY;
            }

            // Add diagonal matches from (editX, editY) to (x, y) in reverse
            while (x > editX && y > editY)
            {
                x--;
                y--;
                result.Add((DiffLineType.Context, oldArr[x]));
            }

            // Add the edit operation
            if (prevK == k + 1)
            {
                // Insert: added line from new
                y--;
                result.Add((DiffLineType.Added, newArr[y]));
            }
            else
            {
                // Delete: removed line from old
                x--;
                result.Add((DiffLineType.Removed, oldArr[x]));
            }
        }

        // Remaining diagonal from (0, 0) to current position
        while (x > 0 && y > 0)
        {
            x--;
            y--;
            result.Add((DiffLineType.Context, oldArr[x]));
        }

        // Any remaining lines (shouldn't happen for well-formed input but handle gracefully)
        while (x > 0)
        {
            x--;
            result.Add((DiffLineType.Removed, oldArr[x]));
        }
        while (y > 0)
        {
            y--;
            result.Add((DiffLineType.Added, newArr[y]));
        }

        result.Reverse();
        return result;
    }

    #endregion
}
