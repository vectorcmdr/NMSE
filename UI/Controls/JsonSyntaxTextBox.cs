#nullable enable
using System.Drawing.Drawing2D;
using System.Globalization;

namespace NMSE.UI.Controls;

/// <summary>
/// A fully owner-drawn JSON text viewer with line numbers, syntax colouring,
/// and node-level folding. No <see cref="RichTextBox"/> -- text is stored as a
/// plain <c>List&lt;string&gt;</c> of lines and only the visible lines are
/// rendered via GDI+ <c>DrawString</c> during <see cref="OnPaint"/>.
///
/// <para>Memory is flat: only the raw line data plus a small scroll state.
/// There is zero overhead for off-screen content.  Scrolling is handled by
/// integer line-index scroll bars, so even million-line files scroll smoothly.</para>
///
/// <para><b>Editing</b> is supported via keyboard input, caret tracking,
/// clipboard, and an undo/redo stack.</para>
///
/// <para><b>Handle budget:</b> only 3 HWNDs per instance (the UserControl
/// itself plus two scroll bars).  All GDI objects (brushes, pens, fonts) are
/// cached as long-lived fields and disposed once in <see cref="Dispose"/>,
/// eliminating per-frame handle churn.  The caret blink timer uses
/// <see cref="System.Threading.Timer"/> (thread-pool, zero HWNDs) instead
/// of <c>System.Windows.Forms.Timer</c> (1 HWND each).</para>
/// </summary>
internal sealed class JsonSyntaxTextBox : UserControl
{
    private const int GutterLineNumberWidth = 56;
    private const int FoldMarginWidth = 18;
    private const int TotalGutterWidth = GutterLineNumberWidth + FoldMarginWidth;
    private const int MaxFoldPreviewLength = 80;
    private const int HScrollUnit = 8;   // pixels per horizontal scroll unit
    private const int MaxUndoHistory = 50;
    private const int CaretBlinkMs = 530;
    private const int LeftTextPadding = 4; // px gap between gutter and text
    private const int TabSize = 4;

    // Cached GDI objects (created once, disposed of in Dispose)
    // Brushes: one per syntax colour
    private readonly SolidBrush _keyBrush = new(Color.FromArgb(0, 51, 179));
    private readonly SolidBrush _stringBrush = new(Color.FromArgb(163, 21, 21));
    private readonly SolidBrush _numberBrush = new(Color.FromArgb(9, 134, 88));
    private readonly SolidBrush _boolNullBrush = new(Color.FromArgb(128, 0, 128));
    private readonly SolidBrush _braceBrush = new(Color.FromArgb(60, 60, 60));
    private readonly SolidBrush _defaultBrush = new(Color.FromArgb(30, 30, 30));
    private readonly SolidBrush _gutterBgBrush = new(Color.FromArgb(245, 245, 245));
    private readonly SolidBrush _gutterNumBrush = new(Color.FromArgb(130, 130, 130));
    private readonly SolidBrush _selBrush = new(Color.FromArgb(173, 214, 255));
    private readonly SolidBrush _currentLineBrush = new(Color.FromArgb(255, 255, 228));
    private readonly SolidBrush _bgBrush = new(Color.White);
    // Pens
    private readonly Pen _foldPen = new(Color.FromArgb(160, 160, 160), 1f);
    private readonly Pen _foldLinePen;
    private readonly Pen _sepPen = new(Color.FromArgb(220, 220, 220), 1f);
    private readonly Pen _caretPen = new(Color.Black, 1.5f);

    // Child controls (3 HWNDs total: UserControl + 2 scrollbars)
    private readonly VScrollBar _vScroll;
    private readonly HScrollBar _hScroll;

    // Caret blink timer (System.Threading.Timer = 0 HWNDs)
    private System.Threading.Timer? _caretTimer;

    // Document
    private List<string> _lines = new() { "" };
    private bool _readOnly;

    // Caret & selection
    private int _caretLine;          // 0-based line
    private int _caretCol;           // 0-based column
    private int _selStartLine = -1;  // -1 = no selection
    private int _selStartCol;
    private bool _caretVisible = true;
    private bool _mouseSelecting;

    // Scroll state
    private int _scrollLine;         // first visible line index
    private int _scrollX;            // horizontal scroll offset in pixels

    // Font metrics (cached)
    private Font _codeFont = null!;
    private Font _gutterFont = null!;   // smaller font for line numbers
    private int _lineHeight;
    private float _charWidth;        // monospace char width

    // Fold state
    private readonly List<FoldRegion> _foldRegions = new();
    private readonly Dictionary<int, FoldRegion> _foldStartLookup = new();
    /// <summary>Maps a display line index -> source line index when folds are active.</summary>
    private List<int>? _displayToSource;
    /// <summary>
    /// Cached join of _lines. Built lazily by the JsonText getter and
    /// invalidated whenever the document is modified or new text is loaded.
    /// This avoids keeping a redundant full-text copy alongside _lines.
    /// </summary>
    private string? _cachedFullText;

    // Undo / redo
    private readonly List<UndoEntry> _undoStack = new();
    private readonly List<UndoEntry> _redoStack = new();

    /// <summary>Raised when the user modifies the text.</summary>
    public event EventHandler? TextModified;

    // Public API (matches old JsonSyntaxTextBox for backward compatibility - modify carefully, future coder)

    /// <summary>Gets or sets the full JSON text (with folds expanded).</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string JsonText
    {
        get
        {
            _cachedFullText ??= string.Join("\n", _lines);
            return _cachedFullText;
        }
        set => SetText(value ?? "");
    }

    /// <summary>Returns true if the control holds any meaningful content.</summary>
    public bool HasContent => _lines.Count > 1 || (_lines.Count == 1 && _lines[0].Length > 0);

    /// <summary>
    /// Releases all document data so the control uses minimal memory while hidden.
    /// The control can be repopulated later via the JsonText setter.
    /// </summary>
    public void ClearContent()
    {
        _lines = new List<string> { "" };
        _cachedFullText = null;
        _foldRegions.Clear();
        _foldStartLookup.Clear();
        _displayToSource = null;
        _undoStack.Clear();
        _redoStack.Clear();
        _caretLine = 0;
        _caretCol = 0;
        _scrollLine = 0;
        _scrollX = 0;
        ClearSelection();
        UpdateScrollBars();
        Invalidate();
    }

    /// <summary>
    /// Searches _lines directly for a substring without materializing the
    /// full text string. Returns the 1-based line number, or -1 if not found.
    /// </summary>
    public int FindLineContaining(string text)
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].Contains(text, StringComparison.Ordinal))
                return i + 1; // 1-based
        }
        return -1;
    }

    /// <summary>Gets the underlying RichTextBox (no longer used - kept for backward compatibility).</summary>
    internal Control InnerRichTextBox => this;

    /// <summary>Gets or sets whether the text is read-only.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public bool ReadOnly
    {
        get => _readOnly;
        set => _readOnly = value;
    }

    /// <summary>
    /// Per-line background colour overlay used for diff/annotation display.
    /// Keys are 0-based source line indices. When set, each matching line is filled with
    /// the specified colour before text is painted, replacing the normal white background.
    /// </summary>
    private IReadOnlyDictionary<int, Color>? _lineBackgroundColors;

    /// <summary>
    /// Applies a per-line background colour map so the control can render coloured diff output.
    /// Keys are 0-based source line indices; values are the desired fill colour.
    /// Pass <c>null</c> to clear and revert to normal (white) line backgrounds.
    /// </summary>
    public void SetLineBackgroundColors(IReadOnlyDictionary<int, Color>? colors)
    {
        _lineBackgroundColors = colors;
        Invalidate();
    }

    /// <summary>Scrolls so that the specified 1-based line number is visible.</summary>
    public void ScrollToLine(int lineNumber)
    {
        int target = Math.Clamp(lineNumber - 1, 0, _lines.Count - 1);
        int displayLine = SourceToDisplay(target);
        _scrollLine = Math.Clamp(displayLine, 0, MaxScrollLine());
        _caretLine = target;
        _caretCol = 0;
        ClearSelection();
        UpdateScrollBars();
        Invalidate();
    }

    /// <summary>Returns the 1-based line number for the current caret position.</summary>
    public int CurrentLine => _caretLine + 1;

    /// <summary>Selects all text.</summary>
    public void SelectAll()
    {
        _selStartLine = 0;
        _selStartCol = 0;
        _caretLine = _lines.Count - 1;
        _caretCol = _lines[_caretLine].Length;
        Invalidate();
    }

    // Constructor

    public JsonSyntaxTextBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
               | ControlStyles.UserPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.Selectable, true);

        _codeFont = new Font("Consolas", 10f);
        _gutterFont = new Font(_codeFont.FontFamily, _codeFont.Size * 0.85f);
        _foldLinePen = new Pen(Color.FromArgb(200, 200, 200), 1f) { DashStyle = DashStyle.Dot };
        CacheFontMetrics();

        _vScroll = new VScrollBar { Dock = DockStyle.Right };
        _vScroll.Scroll += (_, _) => { _scrollLine = _vScroll.Value; Invalidate(); Update(); };

        _hScroll = new HScrollBar { Dock = DockStyle.Bottom };
        _hScroll.Scroll += (_, _) => { _scrollX = _hScroll.Value * HScrollUnit; Invalidate(); Update(); };

        // System.Threading.Timer uses the thread pool: zero HWNDs.
        // Callback marshals to UI thread via BeginInvoke.
        _caretTimer = new System.Threading.Timer(OnCaretTimerCallback,
            null, Timeout.Infinite, Timeout.Infinite);

        Controls.Add(_vScroll);
        Controls.Add(_hScroll);

        // Default cursor for the text area; dynamically changed in OnMouseMove
        // for gutter (Arrow) and fold margin (Hand).
        Cursor = Cursors.IBeam;

        // Scroll bars should always show the default arrow cursor, not IBeam
        _vScroll.Cursor = Cursors.Default;
        _hScroll.Cursor = Cursors.Default;
    }

    private void OnCaretTimerCallback(object? state)
    {
        // Marshal to UI thread: safe even if control is disposing
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(() =>
            {
                _caretVisible = !_caretVisible;
                Invalidate();
            });
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _caretTimer?.Dispose();
            _caretTimer = null;
            // Dispose all cached GDI objects
            _codeFont.Dispose();
            _gutterFont.Dispose();
            _keyBrush.Dispose();
            _stringBrush.Dispose();
            _numberBrush.Dispose();
            _boolNullBrush.Dispose();
            _braceBrush.Dispose();
            _defaultBrush.Dispose();
            _gutterBgBrush.Dispose();
            _gutterNumBrush.Dispose();
            _selBrush.Dispose();
            _currentLineBrush.Dispose();
            _bgBrush.Dispose();
            _foldPen.Dispose();
            _foldLinePen.Dispose();
            _sepPen.Dispose();
            _caretPen.Dispose();
        }
        base.Dispose(disposing);
    }

    // Font metrics

    private void CacheFontMetrics()
    {
        using var g = CreateGraphics();
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        _lineHeight = _codeFont.Height + 2;
        // Measure a representative character for monospace width
        var sz = g.MeasureString("M", _codeFont, int.MaxValue, StringFormat.GenericTypographic);
        _charWidth = sz.Width;
        if (_charWidth <= 0) _charWidth = 8f;
    }

    // Display lines (folding support)

    /// <summary>Number of display lines (accounts for collapsed folds).</summary>
    private int DisplayLineCount => _displayToSource?.Count ?? _lines.Count;

    /// <summary>Converts a display-line index to a source-line index.</summary>
    private int DisplayToSource(int displayLine)
    {
        if (_displayToSource == null) return displayLine;
        if (displayLine < 0 || displayLine >= _displayToSource.Count) return displayLine;
        return _displayToSource[displayLine];
    }

    /// <summary>Converts a source-line index to the nearest display-line index.</summary>
    private int SourceToDisplay(int sourceLine)
    {
        if (_displayToSource == null) return sourceLine;
        for (int i = 0; i < _displayToSource.Count; i++)
            if (_displayToSource[i] >= sourceLine) return i;
        return _displayToSource.Count - 1;
    }

    /// <summary>Rebuilds the display->source mapping from the current fold state.</summary>
    private void RebuildDisplayMap()
    {
        // Check if any fold is collapsed
        bool anyCollapsed = false;
        foreach (var f in _foldRegions)
            if (f.IsCollapsed) { anyCollapsed = true; break; }

        if (!anyCollapsed) { _displayToSource = null; return; }

        var map = new List<int>(_lines.Count);
        var collapsed = new SortedSet<int>(); // source lines hidden by folds
        foreach (var f in _foldRegions)
        {
            if (!f.IsCollapsed) continue;
            for (int l = f.StartLine + 1; l <= f.EndLine && l < _lines.Count; l++)
                collapsed.Add(l);
        }
        for (int i = 0; i < _lines.Count; i++)
            if (!collapsed.Contains(i)) map.Add(i);
        _displayToSource = map;
    }

    // Text management

    private void SetText(string text)
    {
        _cachedFullText = null; // invalidate; will be rebuilt lazily if needed
        _lines = new List<string>(text.Split('\n'));
        // Remove \r from line endings
        for (int i = 0; i < _lines.Count; i++)
            _lines[i] = _lines[i].TrimEnd('\r');

        _foldRegions.Clear();
        _foldStartLookup.Clear();
        _displayToSource = null;
        BuildFoldRegions();

        _caretLine = 0;
        _caretCol = 0;
        _scrollLine = 0;
        _scrollX = 0;
        ClearSelection();
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateScrollBars();
        Invalidate();
    }

    // Scroll bar management

    private int VisibleLineCount => Math.Max(1, (TextAreaHeight) / _lineHeight);
    private int TextAreaHeight => ClientSize.Height - _hScroll.Height;
    private int TextAreaWidth => ClientSize.Width - TotalGutterWidth - _vScroll.Width - LeftTextPadding;

    private int MaxScrollLine()
    {
        int total = DisplayLineCount;
        return Math.Max(0, total - VisibleLineCount);
    }

    private void UpdateScrollBars()
    {
        int maxV = MaxScrollLine();
        _vScroll.Maximum = Math.Max(0, maxV + VisibleLineCount - 1);
        _vScroll.LargeChange = Math.Max(1, VisibleLineCount);
        _vScroll.SmallChange = 1;
        _vScroll.Value = Math.Clamp(_scrollLine, 0, Math.Max(0, maxV));

        // Horizontal: based on max line length
        int maxLen = 0;
        foreach (var line in _lines)
            if (line.Length > maxLen) maxLen = line.Length;
        int maxPixels = (int)(maxLen * _charWidth) + 200;
        int hMax = Math.Max(0, (maxPixels - TextAreaWidth) / HScrollUnit);
        _hScroll.Maximum = Math.Max(0, hMax + 10);
        _hScroll.LargeChange = Math.Max(1, TextAreaWidth / HScrollUnit);
        _hScroll.SmallChange = 3;
        _hScroll.Value = Math.Clamp(_scrollX / HScrollUnit, 0, Math.Max(0, hMax));
    }

    // Painting

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode = SmoothingMode.None;

        int visibleLines = VisibleLineCount + 1;
        int displayCount = DisplayLineCount;
        int textX = TotalGutterWidth + LeftTextPadding;

        // Background
        g.FillRectangle(_bgBrush, textX, 0, ClientSize.Width - textX - _vScroll.Width, TextAreaHeight);

        // Gutter background
        g.FillRectangle(_gutterBgBrush, 0, 0, TotalGutterWidth, TextAreaHeight);

        // Precompute selection range in source-line coords
        GetOrderedSelection(out int selTopLine, out int selTopCol, out int selBotLine, out int selBotCol);

        for (int vi = 0; vi < visibleLines; vi++)
        {
            int displayIdx = _scrollLine + vi;
            if (displayIdx >= displayCount) break;

            int srcLine = DisplayToSource(displayIdx);
            if (srcLine < 0 || srcLine >= _lines.Count) continue;
            string line = _lines[srcLine];

            int y = vi * _lineHeight;
            if (y > TextAreaHeight) break;

            // Per-line background colour override (diff viewer mode)
            if (_lineBackgroundColors != null && _lineBackgroundColors.TryGetValue(srcLine, out Color lineBg))
            {
                using var lineBgBrush = new SolidBrush(lineBg);
                g.FillRectangle(lineBgBrush, textX, y, ClientSize.Width - textX - _vScroll.Width, _lineHeight);
            }
            else
            {
                // Current line highlighter (only when not in diff colour mode)
                int caretDisplayLine = SourceToDisplay(_caretLine);
                if (displayIdx == caretDisplayLine && !HasSelection())
                {
                    g.FillRectangle(_currentLineBrush, textX, y, ClientSize.Width - textX - _vScroll.Width, _lineHeight);
                }
            }

            // Selection highlighter
            if (HasSelection() && srcLine >= selTopLine && srcLine <= selBotLine)
            {
                int selStart = srcLine == selTopLine ? selTopCol : 0;
                int selEnd = srcLine == selBotLine ? selBotCol : line.Length;
                float x1 = textX + ColToPixelX(line, selStart) - _scrollX;
                float x2 = textX + ColToPixelX(line, selEnd) - _scrollX;
                if (srcLine < selBotLine) x2 = ClientSize.Width; // extend to edge for full-line selection
                g.FillRectangle(_selBrush, x1, y, x2 - x1, _lineHeight);
            }

            // Fold preview lines (collapsed fold on start line)
            bool isFoldStart = _foldStartLookup.TryGetValue(srcLine, out var fold);
            string drawLine = isFoldStart && fold!.IsCollapsed ? fold.CollapsedPreview : line;

            // Syntax coloured text
            DrawSyntaxLine(g, drawLine, textX - _scrollX, y);

            // Line number
            string numStr = (srcLine + 1).ToString(CultureInfo.InvariantCulture);
            var numSize = g.MeasureString(numStr, _gutterFont);
            float numX = GutterLineNumberWidth - numSize.Width - 4;
            float numY = y + (_lineHeight - numSize.Height) / 2f;
            g.DrawString(numStr, _gutterFont, _gutterNumBrush, numX, numY);

            // Fold markers
            if (isFoldStart)
            {
                int boxSize = 9;
                int boxX = GutterLineNumberWidth + (FoldMarginWidth - boxSize) / 2;
                int boxY = y + (_lineHeight - boxSize) / 2;
                g.DrawRectangle(_foldPen, boxX, boxY, boxSize, boxSize);
                int midX = boxX + boxSize / 2;
                int midY = boxY + boxSize / 2;
                g.DrawLine(_foldPen, boxX + 2, midY, boxX + boxSize - 2, midY);
                if (fold!.IsCollapsed)
                    g.DrawLine(_foldPen, midX, boxY + 2, midX, boxY + boxSize - 2);
            }
            else
            {
                var containingFold = FindInnermostFoldContaining(srcLine);
                if (containingFold != null && !containingFold.IsCollapsed)
                {
                    int midX = GutterLineNumberWidth + FoldMarginWidth / 2;
                    g.DrawLine(_foldLinePen, midX, y, midX, y + _lineHeight);
                    if (srcLine == containingFold.EndLine)
                        g.DrawLine(_foldLinePen, midX, y + _lineHeight / 2, midX + 4, y + _lineHeight / 2);
                }
            }
        }

        // Gutter separator
        g.DrawLine(_sepPen, TotalGutterWidth - 1, 0, TotalGutterWidth - 1, TextAreaHeight);

        // Caret
        if (Focused && _caretVisible && !_readOnly)
        {
            int caretDisplay = SourceToDisplay(_caretLine);
            int caretVi = caretDisplay - _scrollLine;
            if (caretVi >= 0 && caretVi <= visibleLines)
            {
                string caretLineText = _caretLine < _lines.Count ? _lines[_caretLine] : "";
                float cx = textX + ColToPixelX(caretLineText, _caretCol) - _scrollX;
                int cy = caretVi * _lineHeight;
                g.DrawLine(_caretPen, cx, cy, cx, cy + _lineHeight);
            }
        }
    }

    /// <summary>
    /// Draws a single line of JSON with syntax colouring. 
	/// Tokenises inline and calls DrawString for each token span.
	/// Only the visible line is processed, so this is always fast (enough).
    /// </summary>
    private void DrawSyntaxLine(Graphics g, string line, float x, int y)
    {
        if (line.Length == 0) return;

        var sf = StringFormat.GenericTypographic;
        sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

        int i = 0;
        float curX = x;

        while (i < line.Length)
        {
            char c = line[i];

            // Leading whitespace
            if (c == ' ' || c == '\t')
            {
                int wsStart = i;
                while (i < line.Length && (line[i] == ' ' || line[i] == '\t')) i++;
                float w = ColToPixelX(line, i) - ColToPixelX(line, wsStart);
                curX += w;
                continue;
            }

            // Strings
            if (c == '"')
            {
                int strStart = i;
                i++;
                while (i < line.Length)
                {
                    if (line[i] == '\\') { i += 2; continue; }
                    if (line[i] == '"') { i++; break; }
                    i++;
                }
                // Peek for key
                int peek = i;
                while (peek < line.Length && (line[peek] == ' ' || line[peek] == '\t')) peek++;
                bool isKey = peek < line.Length && line[peek] == ':';
                string span = line[strStart..i];
                g.DrawString(span, _codeFont, isKey ? _keyBrush : _stringBrush, curX, y, sf);
                curX += MeasureSpan(g, span);
                continue;
            }

            // Numbers
            if (c == '-' || (c >= '0' && c <= '9'))
            {
                int numStart = i;
                if (c == '-') i++;
                while (i < line.Length && line[i] >= '0' && line[i] <= '9') i++;
                if (i < line.Length && line[i] == '.') { i++; while (i < line.Length && line[i] >= '0' && line[i] <= '9') i++; }
                if (i < line.Length && (line[i] == 'e' || line[i] == 'E')) { i++; if (i < line.Length && (line[i] == '+' || line[i] == '-')) i++; while (i < line.Length && line[i] >= '0' && line[i] <= '9') i++; }
                if (i > numStart + (c == '-' ? 1 : 0))
                {
                    string span = line[numStart..i];
                    g.DrawString(span, _codeFont, _numberBrush, curX, y, sf);
                    curX += MeasureSpan(g, span);
                    continue;
                }
                i = numStart; // not a number, fall through
            }

            // Keywords (true/false/null)
            if (MatchKeyword(line, i, "true"))
            {
                g.DrawString("true", _codeFont, _boolNullBrush, curX, y, sf);
                curX += MeasureSpan(g, "true");
                i += 4; continue;
            }
            if (MatchKeyword(line, i, "false"))
            {
                g.DrawString("false", _codeFont, _boolNullBrush, curX, y, sf);
                curX += MeasureSpan(g, "false");
                i += 5; continue;
            }
            if (MatchKeyword(line, i, "null"))
            {
                g.DrawString("null", _codeFont, _boolNullBrush, curX, y, sf);
                curX += MeasureSpan(g, "null");
                i += 4; continue;
            }

            // Braces / brackets
            if (c == '{' || c == '}' || c == '[' || c == ']')
            {
                string s = c.ToString();
                g.DrawString(s, _codeFont, _braceBrush, curX, y, sf);
                curX += MeasureSpan(g, s);
                i++; continue;
            }

            // Default (colon, comma, etc.)
            {
                string s = c.ToString();
                g.DrawString(s, _codeFont, _defaultBrush, curX, y, sf);
                curX += MeasureSpan(g, s);
                i++;
            }
        }
    }

    private float MeasureSpan(Graphics g, string text)
    {
        if (text.Length == 0) return 0;
        var sf = StringFormat.GenericTypographic;
        sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        return g.MeasureString(text, _codeFont, int.MaxValue, sf).Width;
    }

    /// <summary>Converts a column index within a line to a pixel X offset,
    /// accounting for tab expansion.</summary>
    private float ColToPixelX(string line, int col)
    {
        float x = 0;
        int clamped = Math.Min(col, line.Length);
        for (int i = 0; i < clamped; i++)
        {
            if (line[i] == '\t')
            {
                int tabStop = TabSize - ((int)(x / _charWidth) % TabSize);
                x += tabStop * _charWidth;
            }
            else
            {
                x += _charWidth;
            }
        }
        return x;
    }

    /// <summary>Converts a pixel X offset to a column index within a line.</summary>
    private int PixelXToCol(string line, float px)
    {
        float x = 0;
        for (int i = 0; i < line.Length; i++)
        {
            float w;
            if (line[i] == '\t')
            {
                int tabStop = TabSize - ((int)(x / _charWidth) % TabSize);
                w = tabStop * _charWidth;
            }
            else
            {
                w = _charWidth;
            }
            if (x + w / 2 > px) return i;
            x += w;
        }
        return line.Length;
    }

    private static bool MatchKeyword(string text, int pos, string keyword)
    {
        if (pos + keyword.Length > text.Length) return false;
        for (int k = 0; k < keyword.Length; k++)
            if (text[pos + k] != keyword[k]) return false;
        int after = pos + keyword.Length;
        if (after < text.Length && char.IsLetterOrDigit(text[after])) return false;
        return true;
    }

    private (int line, int col) HitTest(int mouseX, int mouseY)
    {
        int vi = mouseY / _lineHeight;
        int displayIdx = _scrollLine + vi;
        if (displayIdx < 0) displayIdx = 0;
        if (displayIdx >= DisplayLineCount) displayIdx = DisplayLineCount - 1;
        int srcLine = DisplayToSource(Math.Max(0, displayIdx));
        srcLine = Math.Clamp(srcLine, 0, _lines.Count - 1);

        float px = mouseX - TotalGutterWidth - LeftTextPadding + _scrollX;
        int col = PixelXToCol(_lines[srcLine], px);
        return (srcLine, col);
    }

    // Mouse handling

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button == MouseButtons.Left)
        {
            // Check fold margin click
            if (e.X >= GutterLineNumberWidth && e.X < TotalGutterWidth)
            {
                int vi = e.Y / _lineHeight;
                int displayIdx = _scrollLine + vi;
                if (displayIdx >= 0 && displayIdx < DisplayLineCount)
                {
                    int srcLine = DisplayToSource(displayIdx);
                    if (_foldStartLookup.TryGetValue(srcLine, out var fold))
                    {
                        ToggleFold(fold);
                        return;
                    }
                }
            }

            var (line, col) = HitTest(e.X, e.Y);
            _caretLine = line;
            _caretCol = col;

            if ((ModifierKeys & Keys.Shift) != 0)
            {
                // Extend selection
                if (!HasSelection()) { _selStartLine = _caretLine; _selStartCol = _caretCol; }
            }
            else
            {
                _selStartLine = line;
                _selStartCol = col;
            }
            _mouseSelecting = true;
            ResetCaretBlink();
            Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        // Dynamic cursor:
		// - Arrow over gutter
		// - Hand over fold +/-
		// - IBeam over text
        if (e.X < GutterLineNumberWidth)
        {
            // Line-number gutter
            Cursor = Cursors.Default;
        }
        else if (e.X >= GutterLineNumberWidth && e.X < TotalGutterWidth)
        {
            // Fold margin: show hand if a fold marker is under the pointer
            int vi = e.Y / _lineHeight;
            int displayIdx = _scrollLine + vi;
            bool overFoldBox = false;
            if (displayIdx >= 0 && displayIdx < DisplayLineCount)
            {
                int srcLine = DisplayToSource(displayIdx);
                overFoldBox = _foldStartLookup.ContainsKey(srcLine);
            }
            Cursor = overFoldBox ? Cursors.Hand : Cursors.Default;
        }
        else
        {
            Cursor = Cursors.IBeam;
        }

        if (_mouseSelecting && e.Button == MouseButtons.Left)
        {
            var (line, col) = HitTest(e.X, e.Y);
            _caretLine = line;
            _caretCol = col;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _mouseSelecting = false;
        // If start == end, clear selection
        if (_selStartLine == _caretLine && _selStartCol == _caretCol)
            ClearSelection();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        int delta = -(e.Delta / 120) * 3; // 3 lines per wheel notch
        _scrollLine = Math.Clamp(_scrollLine + delta, 0, MaxScrollLine());
        UpdateScrollBars();
        // Force immediate repaint to prevent hitching during fast scrolls.
        Invalidate();
        Update();
    }

    // Keyboard handling

    protected override bool IsInputKey(Keys keyData)
    {
        // Ensure arrow keys etc. are processed by OnKeyDown
        return keyData switch
        {
            Keys.Left or Keys.Right or Keys.Up or Keys.Down
            or Keys.Tab or Keys.Enter or Keys.Home or Keys.End
            or Keys.PageUp or Keys.PageDown
            or (Keys.Shift | Keys.Left) or (Keys.Shift | Keys.Right)
            or (Keys.Shift | Keys.Up) or (Keys.Shift | Keys.Down)
            or (Keys.Shift | Keys.Home) or (Keys.Shift | Keys.End)
            => true,
            _ => base.IsInputKey(keyData)
        };
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        bool shift = e.Shift;
        bool ctrl = e.Control;

        switch (e.KeyCode)
        {
            case Keys.Up:
                MoveCaret(0, -1, shift);
                e.Handled = true;
                break;
            case Keys.Down:
                MoveCaret(0, 1, shift);
                e.Handled = true;
                break;
            case Keys.Left:
                if (ctrl) MoveCaretWordLeft(shift);
                else MoveCaret(-1, 0, shift);
                e.Handled = true;
                break;
            case Keys.Right:
                if (ctrl) MoveCaretWordRight(shift);
                else MoveCaret(1, 0, shift);
                e.Handled = true;
                break;
            case Keys.Home:
                if (ctrl) { _caretLine = 0; _caretCol = 0; }
                else _caretCol = 0;
                if (!shift) ClearSelection();
                else StartSelectionIfNeeded();
                EnsureCaretVisible();
                Invalidate();
                e.Handled = true;
                break;
            case Keys.End:
                if (ctrl) { _caretLine = _lines.Count - 1; _caretCol = _lines[_caretLine].Length; }
                else _caretCol = _lines[_caretLine].Length;
                if (!shift) ClearSelection();
                else StartSelectionIfNeeded();
                EnsureCaretVisible();
                Invalidate();
                e.Handled = true;
                break;
            case Keys.PageUp:
                _caretLine = Math.Max(0, _caretLine - VisibleLineCount);
                _caretCol = Math.Min(_caretCol, _lines[_caretLine].Length);
                _scrollLine = Math.Max(0, _scrollLine - VisibleLineCount);
                if (!shift) ClearSelection();
                else StartSelectionIfNeeded();
                UpdateScrollBars();
                Invalidate();
                e.Handled = true;
                break;
            case Keys.PageDown:
                _caretLine = Math.Min(_lines.Count - 1, _caretLine + VisibleLineCount);
                _caretCol = Math.Min(_caretCol, _lines[_caretLine].Length);
                _scrollLine = Math.Clamp(_scrollLine + VisibleLineCount, 0, MaxScrollLine());
                if (!shift) ClearSelection();
                else StartSelectionIfNeeded();
                UpdateScrollBars();
                Invalidate();
                e.Handled = true;
                break;
            case Keys.A when ctrl:
                SelectAll();
                e.Handled = true;
                break;
            case Keys.C when ctrl:
                CopyToClipboard();
                e.Handled = true;
                break;
            case Keys.X when ctrl:
                if (!_readOnly) CutToClipboard();
                e.Handled = true;
                break;
            case Keys.V when ctrl:
                if (!_readOnly) PasteFromClipboard();
                e.Handled = true;
                break;
            case Keys.Z when ctrl:
                if (!_readOnly) Undo();
                e.Handled = true;
                break;
            case Keys.Y when ctrl:
                if (!_readOnly) Redo();
                e.Handled = true;
                break;
            case Keys.Back:
                if (!_readOnly) HandleBackspace();
                e.Handled = true;
                break;
            case Keys.Delete:
                if (!_readOnly) HandleDelete();
                e.Handled = true;
                break;
            case Keys.Enter:
                if (!_readOnly) InsertText("\n");
                e.Handled = true;
                break;
            case Keys.Tab:
                if (!_readOnly)
                {
                    InsertText(new string(' ', TabSize));
                    e.SuppressKeyPress = true;
                }
                e.Handled = true;
                break;
        }
        ResetCaretBlink();
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (_readOnly) return;
        if (e.KeyChar < 32 && e.KeyChar != '\t') return; // skip control chars
        InsertText(e.KeyChar.ToString());
        e.Handled = true;
    }

    // Caret movement

    private void MoveCaret(int dx, int dy, bool shift)
    {
        if (shift) StartSelectionIfNeeded();

        if (dy != 0)
        {
            _caretLine = Math.Clamp(_caretLine + dy, 0, _lines.Count - 1);
            _caretCol = Math.Min(_caretCol, _lines[_caretLine].Length);
        }
        if (dx != 0)
        {
            _caretCol += dx;
            if (_caretCol < 0)
            {
                if (_caretLine > 0) { _caretLine--; _caretCol = _lines[_caretLine].Length; }
                else _caretCol = 0;
            }
            else if (_caretCol > _lines[_caretLine].Length)
            {
                if (_caretLine < _lines.Count - 1) { _caretLine++; _caretCol = 0; }
                else _caretCol = _lines[_caretLine].Length;
            }
        }

        if (!shift) ClearSelection();
        EnsureCaretVisible();
        Invalidate();
    }

    private void MoveCaretWordLeft(bool shift)
    {
        if (shift) StartSelectionIfNeeded();
        string line = _lines[_caretLine];
        if (_caretCol == 0 && _caretLine > 0)
        {
            _caretLine--;
            _caretCol = _lines[_caretLine].Length;
        }
        else
        {
            int p = _caretCol - 1;
            while (p > 0 && !char.IsLetterOrDigit(line[p - 1])) p--;
            while (p > 0 && char.IsLetterOrDigit(line[p - 1])) p--;
            _caretCol = Math.Max(0, p);
        }
        if (!shift) ClearSelection();
        EnsureCaretVisible();
        Invalidate();
    }

    private void MoveCaretWordRight(bool shift)
    {
        if (shift) StartSelectionIfNeeded();
        string line = _lines[_caretLine];
        if (_caretCol >= line.Length && _caretLine < _lines.Count - 1)
        {
            _caretLine++;
            _caretCol = 0;
        }
        else
        {
            int p = _caretCol;
            while (p < line.Length && char.IsLetterOrDigit(line[p])) p++;
            while (p < line.Length && !char.IsLetterOrDigit(line[p])) p++;
            _caretCol = p;
        }
        if (!shift) ClearSelection();
        EnsureCaretVisible();
        Invalidate();
    }

    private void EnsureCaretVisible()
    {
        int displayLine = SourceToDisplay(_caretLine);
        if (displayLine < _scrollLine) _scrollLine = displayLine;
        else if (displayLine >= _scrollLine + VisibleLineCount)
            _scrollLine = displayLine - VisibleLineCount + 1;
        _scrollLine = Math.Clamp(_scrollLine, 0, MaxScrollLine());

        // Horizontal
        string line = _caretLine < _lines.Count ? _lines[_caretLine] : "";
        float caretPx = ColToPixelX(line, _caretCol);
        if (caretPx - _scrollX < 0) _scrollX = (int)caretPx;
        else if (caretPx - _scrollX > TextAreaWidth - 20)
            _scrollX = (int)caretPx - TextAreaWidth + 40;
        if (_scrollX < 0) _scrollX = 0;

        UpdateScrollBars();
    }

    private void ResetCaretBlink()
    {
        _caretVisible = true;
        // Restart the blink cycle from now
        _caretTimer?.Change(CaretBlinkMs, CaretBlinkMs);
    }

    private void InvalidateCaretArea()
    {
        Invalidate();
    }

    // Selection helpers

    private bool HasSelection() => _selStartLine >= 0 &&
        (_selStartLine != _caretLine || _selStartCol != _caretCol);

    private void ClearSelection() { _selStartLine = -1; _selStartCol = 0; }

    private void StartSelectionIfNeeded()
    {
        if (_selStartLine < 0) { _selStartLine = _caretLine; _selStartCol = _caretCol; }
    }

    private void GetOrderedSelection(out int topLine, out int topCol, out int botLine, out int botCol)
    {
        if (!HasSelection())
        {
            topLine = topCol = botLine = botCol = -1;
            return;
        }
        if (_selStartLine < _caretLine || (_selStartLine == _caretLine && _selStartCol <= _caretCol))
        {
            topLine = _selStartLine; topCol = _selStartCol;
            botLine = _caretLine; botCol = _caretCol;
        }
        else
        {
            topLine = _caretLine; topCol = _caretCol;
            botLine = _selStartLine; botCol = _selStartCol;
        }
    }

    private string GetSelectedText()
    {
        GetOrderedSelection(out int topLine, out int topCol, out int botLine, out int botCol);
        if (topLine < 0) return "";
        if (topLine == botLine)
            return _lines[topLine][topCol..botCol];

        var sb = new System.Text.StringBuilder();
        sb.Append(_lines[topLine][topCol..]);
        for (int i = topLine + 1; i < botLine; i++)
        {
            sb.Append('\n');
            sb.Append(_lines[i]);
        }
        sb.Append('\n');
        sb.Append(_lines[botLine][..botCol]);
        return sb.ToString();
    }

    private void DeleteSelection()
    {
        GetOrderedSelection(out int topLine, out int topCol, out int botLine, out int botCol);
        if (topLine < 0) return;

        PushUndo();
        string before = _lines[topLine][..topCol];
        string after = _lines[botLine][botCol..];
        _lines[topLine] = before + after;
        if (botLine > topLine)
            _lines.RemoveRange(topLine + 1, botLine - topLine);

        _caretLine = topLine;
        _caretCol = topCol;
        ClearSelection();
        OnDocumentModified();
    }

    // Editing operations

    private void InsertText(string text)
    {
        if (HasSelection()) DeleteSelection();
        PushUndo();

        var insertLines = text.Split('\n');
        string before = _lines[_caretLine][.._caretCol];
        string after = _lines[_caretLine][_caretCol..];

        if (insertLines.Length == 1)
        {
            _lines[_caretLine] = before + insertLines[0] + after;
            _caretCol = (before + insertLines[0]).Length;
        }
        else
        {
            _lines[_caretLine] = before + insertLines[0];
            for (int i = 1; i < insertLines.Length - 1; i++)
                _lines.Insert(_caretLine + i, insertLines[i]);
            _lines.Insert(_caretLine + insertLines.Length - 1, insertLines[^1] + after);
            _caretLine += insertLines.Length - 1;
            _caretCol = insertLines[^1].Length;
        }

        ClearSelection();
        OnDocumentModified();
    }

    private void HandleBackspace()
    {
        if (HasSelection()) { DeleteSelection(); return; }
        if (_caretCol == 0 && _caretLine == 0) return;

        PushUndo();
        if (_caretCol > 0)
        {
            _lines[_caretLine] = _lines[_caretLine].Remove(_caretCol - 1, 1);
            _caretCol--;
        }
        else
        {
            // Merge with previous line
            _caretCol = _lines[_caretLine - 1].Length;
            _lines[_caretLine - 1] += _lines[_caretLine];
            _lines.RemoveAt(_caretLine);
            _caretLine--;
        }
        OnDocumentModified();
    }

    private void HandleDelete()
    {
        if (HasSelection()) { DeleteSelection(); return; }
        if (_caretLine >= _lines.Count) return;

        PushUndo();
        if (_caretCol < _lines[_caretLine].Length)
        {
            _lines[_caretLine] = _lines[_caretLine].Remove(_caretCol, 1);
        }
        else if (_caretLine < _lines.Count - 1)
        {
            _lines[_caretLine] += _lines[_caretLine + 1];
            _lines.RemoveAt(_caretLine + 1);
        }
        OnDocumentModified();
    }

    private void OnDocumentModified()
    {
        _cachedFullText = null; // invalidate cached string
        BuildFoldRegions();
        RebuildDisplayMap();
        EnsureCaretVisible();
        UpdateScrollBars();
        Invalidate();
        TextModified?.Invoke(this, EventArgs.Empty);
    }

    // Clipboard

    private void CopyToClipboard()
    {
        string text = HasSelection() ? GetSelectedText() : "";
        if (text.Length > 0)
        {
            try { Clipboard.SetText(text); } catch { /* clipboard locked */ }
        }
    }

    private void CutToClipboard()
    {
        CopyToClipboard();
        if (HasSelection()) DeleteSelection();
    }

    private void PasteFromClipboard()
    {
        try
        {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace("\r\n", "\n").Replace("\r", "\n");
                InsertText(text);
            }
        }
        catch { /* clipboard locked! */ }
    }

    // Undo / Redo

    private sealed class UndoEntry
    {
        public List<string> Lines { get; init; } = null!;
        public int CaretLine { get; init; }
        public int CaretCol { get; init; }
    }

    private void PushUndo()
    {
        if (_undoStack.Count >= MaxUndoHistory) _undoStack.RemoveAt(0);
        _undoStack.Add(new UndoEntry
        {
            Lines = new List<string>(_lines),
            CaretLine = _caretLine,
            CaretCol = _caretCol
        });
        _redoStack.Clear();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Add(new UndoEntry
        {
            Lines = new List<string>(_lines),
            CaretLine = _caretLine,
            CaretCol = _caretCol
        });
        var entry = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _lines = new List<string>(entry.Lines);
        _caretLine = entry.CaretLine;
        _caretCol = entry.CaretCol;
        ClearSelection();
        _cachedFullText = null;
        BuildFoldRegions();
        RebuildDisplayMap();
        UpdateScrollBars();
        EnsureCaretVisible();
        Invalidate();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Add(new UndoEntry
        {
            Lines = new List<string>(_lines),
            CaretLine = _caretLine,
            CaretCol = _caretCol
        });
        var entry = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        _lines = new List<string>(entry.Lines);
        _caretLine = entry.CaretLine;
        _caretCol = entry.CaretCol;
        ClearSelection();
        _cachedFullText = null;
        BuildFoldRegions();
        RebuildDisplayMap();
        UpdateScrollBars();
        EnsureCaretVisible();
        Invalidate();
    }

    // Folding

    private sealed class FoldRegion
    {
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public bool IsCollapsed { get; set; }
        public string CollapsedPreview { get; set; } = "";
    }

    private void BuildFoldRegions()
    {
        _foldRegions.Clear();
        _foldStartLookup.Clear();

        var stack = new Stack<int>();
        for (int i = 0; i < _lines.Count; i++)
        {
            string line = _lines[i];
            for (int c = 0; c < line.Length; c++)
            {
                char ch = line[c];
                if (ch == '"')
                {
                    c++;
                    while (c < line.Length)
                    {
                        if (line[c] == '\\') { c++; }
                        else if (line[c] == '"') break;
                        c++;
                    }
                    continue;
                }
                if (ch == '{' || ch == '[') stack.Push(i);
                else if (ch == '}' || ch == ']')
                {
                    if (stack.Count > 0)
                    {
                        int startLine = stack.Pop();
                        if (i > startLine)
                        {
                            string preview = _lines[startLine].TrimEnd();
                            if (preview.Length > MaxFoldPreviewLength)
                                preview = preview[..MaxFoldPreviewLength] + "...";
                            var region = new FoldRegion
                            {
                                StartLine = startLine,
                                EndLine = i,
                                CollapsedPreview = preview + " ... " + (ch == '}' ? "}" : "]")
                            };
                            _foldRegions.Add(region);
                            _foldStartLookup.TryAdd(startLine, region);
                        }
                    }
                }
            }
        }
        _foldRegions.Sort((a, b) => a.StartLine.CompareTo(b.StartLine));
        RebuildDisplayMap();
    }

    private FoldRegion? FindInnermostFoldContaining(int line)
    {
        FoldRegion? best = null;
        foreach (var fold in _foldRegions)
        {
            if (fold.StartLine < line && fold.EndLine >= line)
            {
                if (best == null || (fold.EndLine - fold.StartLine) < (best.EndLine - best.StartLine))
                    best = fold;
            }
        }
        return best;
    }

    private void ToggleFold(FoldRegion fold)
    {
        fold.IsCollapsed = !fold.IsCollapsed;
        RebuildDisplayMap();
        // Adjust scroll if needed
        _scrollLine = Math.Clamp(_scrollLine, 0, MaxScrollLine());
        UpdateScrollBars();
        Invalidate();
    }

    // Focus handling

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _caretTimer?.Change(CaretBlinkMs, CaretBlinkMs);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _caretTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateScrollBars();
        Invalidate();
    }
}
