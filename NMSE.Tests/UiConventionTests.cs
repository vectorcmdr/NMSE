using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace NMSE.Tests;

/// <summary>
/// Source-level convention tests that scan the UI code to enforce consistency
/// rules that are easy to regress on. These verify code patterns rather than
/// runtime behaviour.
/// </summary>
public class UiConventionTests
{
	// TODO: This class needs to be built upon over time as new conventions are added
	//
	// 16/04/26: Added decimal indicator invariant culture check explicit for Roslyn
	// because I am sick of it being mentioned as a "regression fear" - so now Roz and
	// the tests will error it so I can paste a test/build output into tickets and move on.

	private readonly ITestOutputHelper _output;
    private const string UiDir = "../../../../../UI";

    public UiConventionTests(ITestOutputHelper output) { _output = output; }

    /// <summary>
    /// Every MessageBox.Show call in the UI layer should pass an owner window
    /// (typically 'this') as the first parameter so that the dialog centres on
    /// the parent form instead of the screen. The only exceptions are calls
    /// inside static methods where 'this' is unavailable.
    /// </summary>
    [Fact]
    public void AllMessageBoxShowCalls_ShouldUseOwnerParameter()
    {
        if (!Directory.Exists(UiDir))
        {
            _output.WriteLine("UI directory not found, skipping.");
            return;
        }

        var csFiles = Directory.GetFiles(UiDir, "*.cs", SearchOption.AllDirectories);
        var violations = new System.Collections.Generic.List<string>();

        // Matches MessageBox.Show( NOT followed by this, or FindForm
        var pattern = new Regex(@"MessageBox\.Show\((?!this[,\s])(?!FindForm)", RegexOptions.Compiled);

        foreach (var file in csFiles)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (pattern.IsMatch(lines[i]))
                {
                    // Allow static methods where 'this' is not available
                    bool inStatic = IsInsideStaticMethod(lines, i);
                    if (!inStatic)
                    {
                        string relative = Path.GetRelativePath(UiDir, file);
                        violations.Add($"{relative}:{i + 1}: {lines[i].Trim()}");
                    }
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.Empty(violations);
    }

    /// <summary>
    /// CompanionPanel.cs must declare an ExosuitCargoModified event so that
    /// MainForm can refresh the exosuit inventory grid after an egg is placed.
    /// </summary>
    [Fact]
    public void CompanionPanel_DeclaresExosuitCargoModifiedEvent()
    {
        string panelPath = Path.Combine(UiDir, "Panels", "CompanionPanel.cs");
        if (!File.Exists(panelPath))
        {
            _output.WriteLine("CompanionPanel.cs not found, skipping.");
            return;
        }

        string content = File.ReadAllText(panelPath);
        Assert.Contains("public event EventHandler? ExosuitCargoModified", content);
    }

    /// <summary>
    /// MainForm.cs must subscribe to CompanionPanel.ExosuitCargoModified so
    /// the exosuit inventory grid refreshes when an egg is placed in cargo.
    /// </summary>
    [Fact]
    public void MainForm_SubscribesToExosuitCargoModified()
    {
        string mainFormPath = Path.Combine(UiDir, "MainForm.cs");
        if (!File.Exists(mainFormPath))
        {
            _output.WriteLine("MainForm.cs not found, skipping.");
            return;
        }

        string content = File.ReadAllText(mainFormPath);
        Assert.Contains("ExosuitCargoModified", content);
        Assert.Contains("_exosuitPanel.LoadData", content);
    }

    /// <summary>
    /// Every floating-point TryParse/Parse call in the UI layer must specify
    /// <c>CultureInfo.InvariantCulture</c> to prevent locale-dependent decimal
    /// separators from corrupting JSON save data.
    ///
    /// Preferred: use <c>InvariantNumericTextBox</c> for TextBox fields that hold
    /// decimal numbers - it handles culture normalisation automatically.
    ///
    /// If raw parsing is unavoidable (e.g. DataGridView cell validation), always
    /// pass <c>CultureInfo.InvariantCulture</c>.
    /// </summary>
    [Fact]
    public void AllFloatingPointParses_UseInvariantCulture()
    {
        if (!Directory.Exists(UiDir))
        {
            _output.WriteLine("UI directory not found, skipping.");
            return;
        }

        var csFiles = Directory.GetFiles(UiDir, "*.cs", SearchOption.AllDirectories);
        var violations = new System.Collections.Generic.List<string>();

        // Matches double.TryParse / double.Parse / float.TryParse / float.Parse /
        // decimal.TryParse / decimal.Parse calls.
        var parsePattern = new Regex(
            @"\b(double|float|decimal)\.(Try)?Parse\s*\(",
            RegexOptions.Compiled);

        foreach (var file in csFiles)
        {
            // Skip Designer.cs files - they don't contain parsing logic
            if (file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Skip comment lines
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal) || trimmed.StartsWith("*", StringComparison.Ordinal) || trimmed.StartsWith("///", StringComparison.Ordinal))
                    continue;

                if (!parsePattern.IsMatch(line))
                    continue;

                // The call must mention InvariantCulture somewhere on this line
                // or the next line (multi-line calls are common in this codebase).
                bool hasCulture = line.Contains("InvariantCulture");
                if (!hasCulture && i + 1 < lines.Length)
                    hasCulture = lines[i + 1].Contains("InvariantCulture");
                if (!hasCulture && i + 2 < lines.Length)
                    hasCulture = lines[i + 2].Contains("InvariantCulture");

                if (!hasCulture)
                {
                    string relative = Path.GetRelativePath(UiDir, file);
                    violations.Add($"{relative}:{i + 1}: {trimmed}");
                }
            }
        }

        foreach (var v in violations)
            _output.WriteLine(v);

        Assert.Empty(violations);
    }

    /// <summary>
    /// Heuristic check: scans backwards from the given line looking for a
    /// 'static' keyword in a method signature before hitting a closing brace
    /// at column 0-4 (indicating a prior method boundary).
    /// </summary>
    private static bool IsInsideStaticMethod(string[] lines, int lineIndex)
    {
        for (int i = lineIndex; i >= 0; i--)
        {
            string trimmed = lines[i].TrimStart();
            // Look for method-like signatures
            if (trimmed.Contains("static ") && (trimmed.Contains("void ") || trimmed.Contains("bool ")
                || trimmed.Contains("string ") || trimmed.Contains("int ") || trimmed.Contains("Task ")))
                return true;
            // Stop scanning at class/struct boundary
            if (trimmed.StartsWith("public class ", StringComparison.Ordinal) || trimmed.StartsWith("internal class ", StringComparison.Ordinal)
                || trimmed.StartsWith("private class ", StringComparison.Ordinal) || trimmed.StartsWith("public partial class ", StringComparison.Ordinal))
                return false;
        }
        return false;
    }
}
