using System.Globalization;
using System.IO.Compression;
using NMSE.Models;
using NMSE.Core;
using NMSE.Data;

namespace NMSE.UI.Panels;

/// <summary>
/// Provides a raw JSON editor panel with tree, text and split views for save and account data.
/// </summary>

// The panel maintains an in-memory JsonObject for the loaded data and synchronises
// edits between the tree and text views. Edits in tree view update the JsonObject
// directly, while edits in text view are parsed and applied to the JsonObject when
// switching back to tree or when explicitly refreshing.
public partial class RawJsonPanel : UserControl
{
    private bool _cancelExpand;

    private JsonObject? _saveData;
    private JsonObject? _accountData;
    private string? _saveFilePath;
    private string? _accountFilePath;

    /// <summary>Current view mode: Tree, Text, or Split.</summary>
    private ViewMode _viewMode = ViewMode.Tree;
    private bool _treeModified;
    private bool _isShowingAccount;
    /// <summary>
    /// Gzip-compressed bytes of the original JSON baseline string.
    /// Stored compressed to reduce long-lived memory overhead (JSON text
    /// compresses ~5-10x). Decompressed on demand when computing diffs.
    /// </summary>
    private byte[]? _originalJsonCompressed;
    private bool _textModifiedSinceSwitch;

    /// <summary>
    /// Cached compact diff result from the last "Show Changes" computation.
    /// Cleared whenever the data is modified so the next click recomputes.
    /// </summary>
    private List<RawJsonLogic.DiffLine>? _cachedDiffLines;
    /// <summary>
    /// When true, the data has changed since the last diff computation
    /// and <see cref="_cachedDiffLines"/> must be recomputed.
    /// </summary>
    private bool _diffCacheDirty = true;

    /// <summary>
    /// Cached result of ToDisplayString for the active data object.
    /// Invalidated when the data reference changes (file switch, import, etc.)
    /// so that view switches do not re-serialize the full JSON tree.
    /// </summary>
    private string? _cachedDisplayString;
    private JsonObject? _cachedDisplayDataRef;

    private enum ViewMode { Tree, Text, Split }

    private readonly Stack<UndoAction> _undoStack = new();
    private readonly Stack<UndoAction> _redoStack = new();
    private TextBox? _inlineEditBox;

    private record UndoAction(
        UndoActionType Type,
        object? Parent,
        string Key,
        object? OldValue,
        object? NewValue
    );

    private enum UndoActionType { Edit, Add, Delete }

    /// <summary>
    /// Returns the display string for the given data, using a cache to avoid
    /// re-serializing when the same object reference is requested repeatedly
    /// (e.g. switching between Text and Split view without data changes).
    /// </summary>
    private string GetDisplayString(JsonObject data)
    {
        if (!ReferenceEquals(data, _cachedDisplayDataRef) || _cachedDisplayString == null)
        {
            _cachedDisplayString = RawJsonLogic.ToDisplayString(data);
            _cachedDisplayDataRef = data;
        }
        return _cachedDisplayString;
    }

    /// <summary>Invalidates the cached display string so the next call re-serializes.</summary>
    private void InvalidateDisplayCache()
    {
        _cachedDisplayString = null;
        _cachedDisplayDataRef = null;
        InvalidateDiffCache();
    }

    /// <summary>Marks the diff cache as stale so the next "Show Changes" click recomputes.</summary>
    private void InvalidateDiffCache()
    {
        _cachedDiffLines = null;
        _diffCacheDirty = true;
    }

    /// <summary>
    /// Compresses a string using GZip and returns the compressed bytes.
    /// JSON text typically compresses 5-10x, significantly reducing long-lived memory.
    /// </summary>
    private static byte[] CompressString(string text)
    {
        var raw = System.Text.Encoding.UTF8.GetBytes(text);
        using var ms = new MemoryStream();
        // Optimal compression: this runs once at file load so the extra CPU cost
        // is negligible, while the better ratio further reduces long-lived memory.
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
            gz.Write(raw);
        return ms.ToArray();
    }

    /// <summary>
    /// Decompresses GZip-compressed bytes back to a string.
    /// </summary>
    private static string DecompressString(byte[] compressed)
    {
        using var ms = new MemoryStream(compressed);
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gz, System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Initialises a new instance of the RawJsonPanel control.
    /// </summary>
    public RawJsonPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

	// Split into regions to help digest the large amount of code
	// in this panel and make maintenance / porting easier later.
	// It's tidy at least, right?

	#region Public API

	/// <summary>
	/// Loads a save file JSON object into the raw editor and initialises the selected view.
	/// </summary>
	/// <param name="saveData">The JSON object representing the save file.</param>
	public void LoadData(JsonObject saveData)
    {
        _saveData = saveData;
        _isShowingAccount = false;
        _treeModified = false;
        InvalidateDisplayCache();
        // Capture the original JSON as a compressed snapshot for the diff baseline.
        // Compression reduces the long-lived memory footprint by ~5-10x for JSON text.
        var originalJson = RawJsonLogic.ToDisplayString(saveData);
        _originalJsonCompressed = CompressString(originalJson);
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateFileSelector();
        if (_viewMode == ViewMode.Tree)
            BuildTree(saveData);
        else if (_viewMode == ViewMode.Text)
            _syntaxTextBox.JsonText = GetDisplayString(saveData);
        else
            LoadSplitView(saveData);
        _statusLabel.Text = UiStrings.Format("raw_json.loaded_keys", saveData.Size().ToString("N0", CultureInfo.CurrentCulture));
        _statusLabel.ForeColor = Color.Gray;
    }

    /// <summary>
    /// Sets the account data that can be edited via the file selector.
    /// </summary>
    public void SetAccountData(JsonObject? accountData, string? accountFilePath)
    {
        _accountData = accountData;
        _accountFilePath = accountFilePath;
        UpdateFileSelector();
    }

    /// <summary>
    /// Sets the save file path for display in the file selector.
    /// </summary>
    /// <param name="filePath">The path to the loaded save file.</param>
    public void SetSaveFilePath(string? filePath)
    {
        _saveFilePath = filePath;
        UpdateFileSelector();
    }

    /// <summary>
    /// Updates the file selector entries for save and account data.
    /// </summary>
    private void UpdateFileSelector()
    {
        _fileSelector.SelectedIndexChanged -= OnFileSelectorChanged;
        _fileSelector.Items.Clear();
        string saveName = !string.IsNullOrEmpty(_saveFilePath) ? Path.GetFileName(_saveFilePath) : "Save File";
        _fileSelector.Items.Add(saveName);
        if (_accountData != null)
        {
            string accountName = !string.IsNullOrEmpty(_accountFilePath) ? Path.GetFileName(_accountFilePath) : "accountdata.hg";
            _fileSelector.Items.Add(accountName);
        }
        _fileSelector.SelectedIndex = _isShowingAccount && _accountData != null ? 1 : 0;
        _fileSelector.SelectedIndexChanged += OnFileSelectorChanged;
    }

    /// <summary>
    /// Handles switching between save data and account data when the selector changes.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments for the selection change.</param>
    private void OnFileSelectorChanged(object? sender, EventArgs e)
    {
        if (_fileSelector.SelectedIndex == 1 && _accountData != null)
        {
            // Switch to account data
            _isShowingAccount = true;
            _treeModified = false;
            InvalidateDisplayCache();
            _originalJsonCompressed = CompressString(RawJsonLogic.ToDisplayString(_accountData));
            _undoStack.Clear();
            _redoStack.Clear();
            if (_viewMode == ViewMode.Tree)
                BuildTree(_accountData);
            else if (_viewMode == ViewMode.Text)
                _syntaxTextBox.JsonText = GetDisplayString(_accountData);
            else
                LoadSplitView(_accountData);
            _statusLabel.Text = UiStrings.Format("raw_json.edited_account", _accountData.Size().ToString("N0", CultureInfo.CurrentCulture));
            _statusLabel.ForeColor = Color.DarkBlue;
        }
        else if (_saveData != null)
        {
            // Switch back to save data
            _isShowingAccount = false;
            _treeModified = false;
            InvalidateDisplayCache();
            _originalJsonCompressed = CompressString(RawJsonLogic.ToDisplayString(_saveData));
            _undoStack.Clear();
            _redoStack.Clear();
            if (_viewMode == ViewMode.Tree)
                BuildTree(_saveData);
            else if (_viewMode == ViewMode.Text)
                _syntaxTextBox.JsonText = GetDisplayString(_saveData);
            else
                LoadSplitView(_saveData);
            _statusLabel.Text = UiStrings.Format("raw_json.loaded_keys", _saveData.Size().ToString("N0", CultureInfo.CurrentCulture));
            _statusLabel.ForeColor = Color.Gray;
        }
    }

    /// <summary>
    /// Persists the current modified state after a save operation.
    /// </summary>
    /// <param name="saveData">The save data object to confirm save state for.</param>
    public void SaveData(JsonObject saveData)
    {
        // Tree edits are applied directly to the JsonObject in real-time via Set/Add/Remove.
        // SaveData only needs to clear the modified flag when in tree mode.
        if (_treeModified && _viewMode == ViewMode.Tree)
            _treeModified = false;
    }

    /// <summary>
    /// Rebuilds the tree/text view from the current in-memory JSON data.
    /// Call this after syncing panel data to the JsonObject so the Raw JSON
    /// editor reflects the latest state of all editable fields.
    /// </summary>
    /// <param name="latestSaveData">
    /// When provided, re-links the internal <c>_saveData</c> reference to
    /// the authoritative object from MainForm. This is needed because
    /// <c>TryRebuildTreeFromText</c> may have replaced <c>_saveData</c>
    /// with a parsed copy, disconnecting it from <c>_currentSaveData</c>.
    /// </param>
    public void RefreshTree(JsonObject? latestSaveData = null)
    {
        // Re-link _saveData to the authoritative reference so that future
        // diffs and edits use the same object that SyncAllPanelData writes to.
        if (latestSaveData != null && !_isShowingAccount)
            _saveData = latestSaveData;

        var data = _isShowingAccount ? _accountData : _saveData;
        if (data == null) return;

        InvalidateDisplayCache();
        if (_viewMode == ViewMode.Tree)
            BuildTree(data);
        else if (_viewMode == ViewMode.Text)
            _syntaxTextBox.JsonText = GetDisplayString(data);
        else
            LoadSplitView(data);
    }

    /// <summary>
    /// Returns the currently edited JSON object, or null if the current text is invalid.
    /// </summary>
    /// <returns>The edited JSON object, or null when the data cannot be parsed.</returns>
    public JsonObject? GetEditedData()
    {
        if (_viewMode == ViewMode.Tree)
        {
            if (_saveData == null) return null;
            _treeModified = false;
            return _saveData;
        }
        string jsonText = _viewMode == ViewMode.Split ? _splitSyntaxTextBox.JsonText : _syntaxTextBox.JsonText;
        try
        {
            return RawJsonLogic.ParseJson(jsonText);
        }
        catch (JsonException ex)
        {
            MessageBox.Show(this, UiStrings.Format("raw_json.invalid_json", ex.Message), UiStrings.Get("raw_json.validation_error"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    #endregion

    #region View Switching

    /// <summary>
    /// Updates the state of the view mode buttons and related controls.
    /// </summary>
    /// <param name="mode">The active view mode.</param>
    private void SetViewModeButtons(ViewMode mode)
    {
        _treeViewButton.Enabled = mode != ViewMode.Tree;
        _textViewButton.Enabled = mode != ViewMode.Text;
        _splitViewButton.Enabled = mode != ViewMode.Split;

        bool showTreeControls = mode != ViewMode.Text;
        _expandAllButton.Visible = showTreeControls;
        _collapseAllButton.Visible = showTreeControls;
        _formatButton.Visible = mode == ViewMode.Text;
        _validateButton.Visible = mode == ViewMode.Text;
        _searchBox.Visible = showTreeControls;
        _searchBackButton.Visible = showTreeControls;
        _searchButton.Visible = showTreeControls;
        _clearSearchButton.Visible = showTreeControls;
        _breadcrumbPanel.Visible = showTreeControls;
    }

    /// <summary>
    /// Switches the panel to tree view and attempts to preserve any pending text edits.
    /// </summary>
    private void ShowTreeView()
    {
        if (_viewMode == ViewMode.Tree) return;
        var previousMode = _viewMode;
        _viewMode = ViewMode.Tree;
        SetViewModeButtons(ViewMode.Tree);
        _textPanel.Visible = false;
        _splitPanel.Visible = false;
        _treePanel.Visible = true;
        _treePanel.BringToFront();

        if (previousMode == ViewMode.Text && _textModifiedSinceSwitch && _syntaxTextBox.HasContent)
        {
            InvalidateDisplayCache();
            TryRebuildTreeFromText(_syntaxTextBox.JsonText);
        }
        else if (previousMode == ViewMode.Split && _textModifiedSinceSwitch && _splitSyntaxTextBox.HasContent)
        {
            InvalidateDisplayCache();
            TryRebuildTreeFromText(_splitSyntaxTextBox.JsonText);
        }
        _textModifiedSinceSwitch = false;

        // Free memory held by hidden text views
        _syntaxTextBox.ClearContent();
        _splitSyntaxTextBox.ClearContent();
    }

    /// <summary>
    /// Switches the panel to text view and displays the current JSON as text.
    /// </summary>
    private void ShowTextView()
    {
        if (_viewMode == ViewMode.Text) return;
        _viewMode = ViewMode.Text;
        _textModifiedSinceSwitch = false;
        SetViewModeButtons(ViewMode.Text);
        _treePanel.Visible = false;
        _splitPanel.Visible = false;
        _textPanel.Visible = true;
        _textPanel.BringToFront();

        var data = _isShowingAccount ? _accountData : _saveData;
        if (data != null)
            _syntaxTextBox.JsonText = GetDisplayString(data);

        // Free memory held by the hidden split text view
        _splitSyntaxTextBox.ClearContent();
    }

    /// <summary>
    /// Switches the panel to split view and synchronises tree and text content.
    /// </summary>
    private void ShowSplitView()
    {
        if (_viewMode == ViewMode.Split) return;
        var previousMode = _viewMode;
        _viewMode = ViewMode.Split;
        _textModifiedSinceSwitch = false;
        SetViewModeButtons(ViewMode.Split);
        _treePanel.Visible = false;
        _textPanel.Visible = false;
        _splitPanel.Visible = true;
        _splitPanel.BringToFront();

        // If coming from text view with unsaved text edits, parse them first
        if (previousMode == ViewMode.Text && _textModifiedSinceSwitch && _syntaxTextBox.HasContent)
        {
            InvalidateDisplayCache();
            TryRebuildTreeFromText(_syntaxTextBox.JsonText);
        }

        var data = _isShowingAccount ? _accountData : _saveData;
        if (data != null)
            LoadSplitView(data);

        // Free memory held by the hidden text view
        _syntaxTextBox.ClearContent();
    }

    /// <summary>
    /// Loads the split view tree and text panels from the given JSON object.
    /// </summary>
    /// <param name="data">The JSON object to display in split view.</param>
    private void LoadSplitView(JsonObject data)
    {
        // Build tree in split tree view
        _splitTreeView.BeginUpdate();
        _splitTreeView.Nodes.Clear();
        var rootNode = new TreeNode("Root") { Tag = new NodeTag(data, null, null), ImageIndex = 0, SelectedImageIndex = 0 };
        PopulateObjectNode(rootNode, data, maxDepth: 2, currentDepth: 0);
        _splitTreeView.Nodes.Add(rootNode);
        rootNode.Expand();
        _splitTreeView.EndUpdate();

        // Set split ratio: tree = 1/3, text = 2/3
        if (_splitContainer.Width > 0)
            _splitContainer.SplitterDistance = _splitContainer.Width / 3;

        // Load text in split syntax text box
        _splitSyntaxTextBox.JsonText = GetDisplayString(data);
    }

    /// <summary>
    /// Handles selection changes in the split view tree and synchronises the text location.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the tree selection.</param>
    private void OnSplitTreeNodeSelected(object? sender, TreeViewEventArgs e)
    {
        UpdateBreadcrumb(e.Node);

        // Sync text view to the selected node's location by searching lines
        // directly. This avoids materializing the entire document string.
        if (e.Node?.Tag is NodeTag tag && tag.Key != null)
        {
            string searchKey = tag.Key.StartsWith('[') ? tag.Key : $"\"{tag.Key}\"";
            int lineNum = _splitSyntaxTextBox.FindLineContaining(searchKey);
            if (lineNum >= 0)
                _splitSyntaxTextBox.ScrollToLine(lineNum);
        }
    }

    /// <summary>
    /// Marks split view text as modified when the user edits it.
    /// </summary>
    private void OnSplitTextModified()
    {
        _textModifiedSinceSwitch = true;
        InvalidateDiffCache();
    }

    /// <summary>
    /// Attempts to parse JSON text and rebuild the tree view from the result.
    /// </summary>
    /// <param name="jsonText">The JSON text to parse.</param>
    private void TryRebuildTreeFromText(string jsonText)
    {
        try
        {
            var parsed = RawJsonLogic.ParseJson(jsonText);
            if (_isShowingAccount)
                _accountData = parsed;
            else
                _saveData = parsed;
            InvalidateDisplayCache();
            BuildTree(parsed);
            _statusLabel.Text = UiStrings.Get("raw_json.tree_rebuilt");
            _statusLabel.ForeColor = Color.Green;
        }
        catch (JsonException ex)
        {
            _statusLabel.Text = UiStrings.Format("raw_json.parse_error", ex.Message);
            _statusLabel.ForeColor = Color.Red;
        }
    }

    #endregion

    #region Tree Building

    /// <summary>
    /// Builds the tree view nodes from the provided JSON object.
    /// </summary>
    /// <param name="root">The JSON object to visualise in the tree.</param>
    private void BuildTree(JsonObject root)
    {
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        var rootNode = new TreeNode("Root") { Tag = new NodeTag(root, null, null), ImageIndex = 0, SelectedImageIndex = 0 };
        PopulateObjectNode(rootNode, root, maxDepth: 2, currentDepth: 0);
        _treeView.Nodes.Add(rootNode);
        rootNode.Expand();

        _treeView.EndUpdate();
    }

    /// <summary>
    /// Expand all tree nodes asynchronously in batches to prevent UI freeze and reduce peak memory.
    /// Uses Task.Delay to yield to the UI thread between batches, keeping the application responsive.
    /// </summary>
    private async Task ExpandAllBatchedAsync()
    {
        var result = MessageBox.Show(this, 
            UiStrings.Get("raw_json.expand_confirm"),
            UiStrings.Get("raw_json.expand_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        _cancelExpand = false;
        _expandAllButton.Enabled = false;
        _collapseAllButton.Enabled = false;
        _stopExpandBtn.Visible = true;
        _statusLabel.Text = UiStrings.Get("raw_json.expanding");
        _statusLabel.ForeColor = Color.Blue;

        int count = 0;
        var stack = new Stack<TreeNode>();
        if (_treeView.Nodes.Count > 0)
            stack.Push(_treeView.Nodes[0]);

        _treeView.BeginUpdate();

        while (stack.Count > 0)
        {
            if (_cancelExpand) break;

            var node = stack.Pop();

            // Force lazy loading by expanding (triggers OnBeforeExpand)
            if (!node.IsExpanded)
                node.Expand();

            // Queue children for expansion
            for (int i = node.Nodes.Count - 1; i >= 0; i--)
                stack.Push(node.Nodes[i]);

            count++;
            if (count % 500 == 0)
            {
                _treeView.EndUpdate();
                _statusLabel.Text = UiStrings.Format("raw_json.expanding_count", count.ToString("N0", CultureInfo.CurrentCulture));
                await Task.Delay(1); // Yield to UI thread
                _treeView.BeginUpdate();
            }
        }

        if (_treeView.Nodes.Count > 0) _treeView.Nodes[0].EnsureVisible();
        _treeView.EndUpdate();
        _expandAllButton.Enabled = true;
        _collapseAllButton.Enabled = true;
        _stopExpandBtn.Visible = false;
        _statusLabel.Text = _cancelExpand ? UiStrings.Format("raw_json.stopped_at", count.ToString("N0", CultureInfo.CurrentCulture)) : UiStrings.Format("raw_json.expanded_nodes", count.ToString("N0", CultureInfo.CurrentCulture));
        _statusLabel.ForeColor = Color.Gray;
    }

    /// <summary>
    /// Adds child nodes for each property in a JSON object.
    /// </summary>
    /// <param name="parentNode">The parent tree node to populate.</param>
    /// <param name="obj">The JSON object whose properties are added.</param>
    /// <param name="maxDepth">The maximum expansion depth for lazy loading.</param>
    /// <param name="currentDepth">The current recursion depth.</param>
    private void PopulateObjectNode(TreeNode parentNode, JsonObject obj, int maxDepth, int currentDepth)
    {
        var names = obj.Names();
        for (int i = 0; i < names.Count; i++)
        {
            string key = names[i];
            object? val = obj.Get(key);
            parentNode.Nodes.Add(CreateValueNode(key, val, obj, maxDepth, currentDepth));
        }
    }

    /// <summary>
    /// Adds child nodes for each element in a JSON array.
    /// </summary>
    /// <param name="parentNode">The parent tree node to populate.</param>
    /// <param name="arr">The JSON array whose elements are added.</param>
    /// <param name="maxDepth">The maximum expansion depth for lazy loading.</param>
    /// <param name="currentDepth">The current recursion depth.</param>
    private void PopulateArrayNode(TreeNode parentNode, JsonArray arr, int maxDepth, int currentDepth)
    {
        for (int i = 0; i < arr.Length; i++)
        {
            object? val = arr.Get(i);
            parentNode.Nodes.Add(CreateValueNode($"[{i}]", val, arr, maxDepth, currentDepth));
        }
    }

    /// <summary>
    /// Creates a tree node for a named value, array or object entry.
    /// </summary>
    /// <param name="key">The key or index label for the node.</param>
    /// <param name="value">The JSON value represented by the node.</param>
    /// <param name="parent">The parent container object or array.</param>
    /// <param name="maxDepth">The maximum recursion depth for lazy loading.</param>
    /// <param name="currentDepth">The current depth within the tree hierarchy.</param>
    /// <returns>A configured tree node for the value.</returns>
    private TreeNode CreateValueNode(string key, object? value, object parent, int maxDepth, int currentDepth)
    {
        if (value is JsonObject childObj)
        {
            int count = childObj.Size();
            var node = new TreeNode($"{key}  {{...}}  ({count} properties)")
            {
                Tag = new NodeTag(childObj, parent, key),
                ForeColor = Color.DarkBlue,
                ImageIndex = 0,
                SelectedImageIndex = 0
            };
            if (currentDepth < maxDepth)
                PopulateObjectNode(node, childObj, maxDepth, currentDepth + 1);
            else if (count > 0)
                node.Nodes.Add(new TreeNode("Loading...") { Tag = LazyTag.Instance });
            return node;
        }
        if (value is JsonArray childArr)
        {
            int count = childArr.Length;
            var node = new TreeNode($"{key}  [...]  ({count} items)")
            {
                Tag = new NodeTag(childArr, parent, key),
                ForeColor = Color.DarkGreen,
                ImageIndex = 1,
                SelectedImageIndex = 1
            };
            if (currentDepth < maxDepth)
                PopulateArrayNode(node, childArr, maxDepth, currentDepth + 1);
            else if (count > 0)
                node.Nodes.Add(new TreeNode("Loading...") { Tag = LazyTag.Instance });
            return node;
        }

        string displayVal = FormatValue(value);
        int iconIdx = GetTypeIconIndex(value);
        return new TreeNode($"{key} : {displayVal}")
        {
            Tag = new NodeTag(value, parent, key),
            ForeColor = GetValueColor(value),
            ImageIndex = iconIdx,
            SelectedImageIndex = iconIdx
        };
    }

    /// <summary>
    /// Formats a JSON value for display in the tree view.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A display string for the value.</returns>
    private static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{EscapeString(s)}\"",
        bool b => b ? "true" : "false",
        BinaryData bd => $"<binary:{bd.ToHexString()}>",
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "null",
        _ => value.ToString() ?? "null"
    };

    /// <summary>
    /// Escapes control characters and quotes for display inside JSON strings.
    /// </summary>
    /// <param name="s">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    private static string EscapeString(string s)
    {
        if (s.Length > 200) return s[..200] + "...";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    /// <summary>
    /// Returns the display colour for a JSON value type.
    /// </summary>
    /// <param name="value">The value whose colour is determined.</param>
    /// <returns>The colour to use for the value text.</returns>
    private static Color GetValueColor(object? value) => value switch
    {
        string => Color.DarkRed,
        bool => Color.DarkMagenta,
        null => Color.Gray,
        _ => Color.DarkOrange
    };

    /// <summary>
    /// Returns the icon index for a JSON value type.
    /// </summary>
    /// <param name="value">The value whose icon is determined.</param>
    /// <returns>The index of the icon for the value type.</returns>
    private static int GetTypeIconIndex(object? value) => value switch
    {
        JsonObject => 0,
        JsonArray => 1,
        string => 2,
        int or long or double or decimal => 3,
        bool => 4,
        null => 5,
        _ => 2
    };

    /// <summary>
    /// Creates the icon list used for JSON node types in the tree view.
    /// </summary>
    /// <returns>An ImageList containing icons for objects, arrays and primitive values.</returns>
    private static ImageList CreateTypeIconList()
    {
        var list = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        list.Images.Add("obj", DrawTextIcon("{}", Color.RoyalBlue));
        list.Images.Add("arr", DrawTextIcon("[]", Color.Green));
        list.Images.Add("str", DrawTextIcon("A", Color.DarkRed));
        list.Images.Add("num", DrawTextIcon("#", Color.DarkOrange));
        list.Images.Add("bool", DrawTextIcon("✓", Color.DarkMagenta));
        list.Images.Add("null", DrawTextIcon("∅", Color.Gray));
        return list;
    }

    /// <summary>
    /// Draws a small text-based icon for JSON node types.
    /// </summary>
    /// <param name="text">The text to render inside the icon.</param>
    /// <param name="color">The colour to use for the icon text.</param>
    /// <returns>A Bitmap containing the rendered icon.</returns>
    private static Bitmap DrawTextIcon(string text, Color color)
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        using var font = new Font("Consolas", 9f, FontStyle.Bold);
        using var brush = new SolidBrush(color);
        var size = g.MeasureString(text, font);
        float x = (16 - size.Width) / 2;
        float y = (16 - size.Height) / 2;
        g.DrawString(text, font, brush, x, y);
        return bmp;
    }

    #endregion

    #region Lazy Loading

    /// <summary>
    /// Handles lazy loading for a tree node before it expands.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments for the expand operation.</param>
    private void OnBeforeExpand(object? sender, TreeViewCancelEventArgs e)
    {
        if (e.Node == null) return;
        if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag is LazyTag)
        {
            e.Node.Nodes.Clear();
            var tag = e.Node.Tag as NodeTag;
            if (tag?.Value is JsonObject obj)
                PopulateObjectNode(e.Node, obj, maxDepth: 2, currentDepth: 0);
            else if (tag?.Value is JsonArray arr)
                PopulateArrayNode(e.Node, arr, maxDepth: 2, currentDepth: 0);
        }
    }

    #endregion

    #region Editing

    /// <summary>
    /// Begins inline editing for a node value when a tree node is double-clicked.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The mouse click event arguments.</param>
    private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is NodeTag tag && tag.Value is not JsonObject && tag.Value is not JsonArray)
            BeginEditSelectedNode();
    }

    /// <summary>
    /// Starts inline editing for the currently selected tree node value.
    /// </summary>
    private void BeginEditSelectedNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag) return;
        if (tag.Value is JsonObject || tag.Value is JsonArray) return;

        CancelInlineEdit();

        string currentVal = RawJsonLogic.FormatValueForEdit(tag.Value);
        string key = tag.Key ?? "";

        var bounds = node.Bounds;
        string prefix = $"{key} : ";
        int prefixWidth;
        using (var g = _treeView.CreateGraphics())
        {
            prefixWidth = (int)g.MeasureString(prefix, _treeView.Font).Width;
        }

        _inlineEditBox = new TextBox
        {
            Text = currentVal,
            Font = _treeView.Font,
            Location = new Point(bounds.Left + prefixWidth - 4, bounds.Top),
            Width = Math.Max(bounds.Width - prefixWidth + 4, 200),
            Height = bounds.Height + 2,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightYellow
        };

        _inlineEditBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                CommitInlineEdit(node, tag, key);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                CancelInlineEdit();
                e.SuppressKeyPress = true;
            }
        };

        _inlineEditBox.LostFocus += (_, _) => CommitInlineEdit(node, tag, key);

        _treeView.Controls.Add(_inlineEditBox);
        _inlineEditBox.Focus();
        _inlineEditBox.SelectAll();
    }

    /// <summary>
    /// Commits an inline edit and updates the underlying JSON value.
    /// </summary>
    /// <param name="node">The tree node being edited.</param>
    /// <param name="tag">The tag data for the edited node.</param>
    /// <param name="key">The key label of the edited node.</param>
    private void CommitInlineEdit(TreeNode node, NodeTag tag, string key)
    {
        if (_inlineEditBox == null) return;
        var editBox = _inlineEditBox;
        _inlineEditBox = null;

        string newVal = editBox.Text.Trim();
        object? parsed = RawJsonLogic.ParseInputValue(newVal, tag.Value);
        _undoStack.Push(new UndoAction(UndoActionType.Edit, tag.Parent, tag.Key!, tag.Value, parsed));
        _redoStack.Clear();
        tag.Value = parsed;

        if (tag.Parent is JsonObject parentObj && tag.Key != null && !tag.Key.StartsWith('['))
            parentObj.Set(tag.Key, parsed);
        else if (tag.Parent is JsonArray parentArr && tag.Key != null && tag.Key.StartsWith('['))
        {
            int idx = int.Parse(tag.Key.Trim('[', ']'), System.Globalization.CultureInfo.InvariantCulture);
            parentArr.Set(idx, parsed);
        }

        node.Text = $"{key} : {FormatValue(parsed)}";
        node.ForeColor = GetValueColor(parsed);
        node.ImageIndex = GetTypeIconIndex(parsed);
        node.SelectedImageIndex = node.ImageIndex;
        _treeModified = true;
        InvalidateDiffCache();
        _statusLabel.Text = UiStrings.Get("raw_json.value_modified");
        _statusLabel.ForeColor = Color.DarkOrange;

        _treeView.Controls.Remove(editBox);
        editBox.Dispose();
        _treeView.Focus();
    }

    /// <summary>
    /// Cancels the current inline edit and removes the edit textbox.
    /// </summary>
    private void CancelInlineEdit()
    {
        if (_inlineEditBox != null)
        {
            var box = _inlineEditBox;
            _inlineEditBox = null;
            _treeView.Controls.Remove(box);
            box.Dispose();
        }
    }

    /// <summary>
    /// Opens a dialog to edit the selected node value.
    /// </summary>
    private void BeginDialogEditNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag) return;
        if (tag.Value is JsonObject || tag.Value is JsonArray) return;

        string currentVal = RawJsonLogic.FormatValueForEdit(tag.Value);
        string key = tag.Key ?? "";

        using var dialog = new Form
        {
            Text = UiStrings.Format("raw_json.edit_title", key),
            Size = new Size(450, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };

        var label = new Label { Text = UiStrings.Get("raw_json.label_value"), Location = new Point(10, 15), AutoSize = true };
        var textBox = new TextBox { Text = currentVal, Location = new Point(60, 12), Width = 360 };
        var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Location = new Point(260, 80), Width = 75 };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(345, 80), Width = 75 };
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Controls.AddRange([label, textBox, okBtn, cancelBtn]);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            string newVal = textBox.Text.Trim();
            object? parsed = RawJsonLogic.ParseInputValue(newVal, tag.Value);
            _undoStack.Push(new UndoAction(UndoActionType.Edit, tag.Parent, tag.Key!, tag.Value, parsed));
            _redoStack.Clear();
            tag.Value = parsed;

            if (tag.Parent is JsonObject parentObj && tag.Key != null && !tag.Key.StartsWith('['))
                parentObj.Set(tag.Key, parsed);
            else if (tag.Parent is JsonArray parentArr && tag.Key != null && tag.Key.StartsWith('['))
            {
                int idx = int.Parse(tag.Key.Trim('[', ']'), System.Globalization.CultureInfo.InvariantCulture);
                parentArr.Set(idx, parsed);
            }

            node.Text = $"{key} : {FormatValue(parsed)}";
            node.ForeColor = GetValueColor(parsed);
            node.ImageIndex = GetTypeIconIndex(parsed);
            node.SelectedImageIndex = node.ImageIndex;
            _treeModified = true;
            InvalidateDiffCache();
            _statusLabel.Text = UiStrings.Get("raw_json.value_modified");
            _statusLabel.ForeColor = Color.DarkOrange;
        }
    }

    /// <summary>
    /// Cancels the tree view label edit event to prevent direct label changes.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event arguments for the label edit.</param>
    private void OnAfterLabelEdit(object? sender, NodeLabelEditEventArgs e) => e.CancelEdit = true;

    // FormatValueForEdit and ParseInputValue are in RawJsonLogic for testability.

    #endregion

    #region Add / Delete

    /// <summary>
    /// Adds a new property to the selected JSON object node.
    /// </summary>
    private void AddProperty()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Value is not JsonObject obj) return;

        using var dialog = new Form
        {
            Text = UiStrings.Get("raw_json.add_property_title"),
            Size = new Size(400, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };
        var keyLabel = new Label { Text = UiStrings.Get("raw_json.label_key"), Location = new Point(10, 15), AutoSize = true };
        var keyBox = new TextBox { Location = new Point(60, 12), Width = 310 };
        var valLabel = new Label { Text = UiStrings.Get("raw_json.label_value"), Location = new Point(10, 50), AutoSize = true };
        var valBox = new TextBox { Location = new Point(60, 47), Width = 310 };
        var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Location = new Point(210, 120), Width = 75 };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(295, 120), Width = 75 };
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Controls.AddRange([keyLabel, keyBox, valLabel, valBox, okBtn, cancelBtn]);

        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(keyBox.Text))
        {
            string newKey = keyBox.Text.Trim();
            if (obj.Contains(newKey))
            {
                MessageBox.Show(this, UiStrings.Format("raw_json.duplicate_key", newKey), UiStrings.Get("raw_json.duplicate_key_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            object? newVal = RawJsonLogic.ParseInputValue(valBox.Text.Trim());
            obj.Add(newKey, newVal);
            _undoStack.Push(new UndoAction(UndoActionType.Add, obj, newKey, null, newVal));
            _redoStack.Clear();
            node.Nodes.Add(CreateValueNode(newKey, newVal, obj, 0, 0));
            UpdateContainerNodeText(node);
            _treeModified = true;
            InvalidateDiffCache();
            _statusLabel.Text = UiStrings.Format("raw_json.added_property", newKey);
            _statusLabel.ForeColor = Color.Green;
        }
    }

    /// <summary>
    /// Adds a new item to the selected JSON array node.
    /// </summary>
    private void AddArrayItem()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Value is not JsonArray arr) return;

        using var dialog = new Form
        {
            Text = UiStrings.Get("raw_json.add_array_item_title"),
            Size = new Size(400, 160),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };
        var valLabel = new Label { Text = UiStrings.Get("raw_json.label_value"), Location = new Point(10, 15), AutoSize = true };
        var valBox = new TextBox { Location = new Point(60, 12), Width = 310 };
        var okBtn = new Button { Text = UiStrings.Get("common.ok"), DialogResult = DialogResult.OK, Location = new Point(210, 80), Width = 75 };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(295, 80), Width = 75 };
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Controls.AddRange([valLabel, valBox, okBtn, cancelBtn]);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            object? newVal = RawJsonLogic.ParseInputValue(valBox.Text.Trim());
            arr.Add(newVal);
            int idx = arr.Length - 1;
            _undoStack.Push(new UndoAction(UndoActionType.Add, arr, $"[{idx}]", null, newVal));
            _redoStack.Clear();
            node.Nodes.Add(CreateValueNode($"[{idx}]", newVal, arr, 0, 0));
            UpdateContainerNodeText(node);
            _treeModified = true;
            InvalidateDiffCache();
            _statusLabel.Text = UiStrings.Format("raw_json.added_array_item", idx);
            _statusLabel.ForeColor = Color.Green;
        }
    }

    /// <summary>
    /// Deletes the currently selected node from the JSON tree and data model.
    /// </summary>
    private void DeleteSelectedNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Parent == null || tag.Key == null) return;

        if (MessageBox.Show(this, UiStrings.Format("raw_json.confirm_delete", tag.Key), UiStrings.Get("raw_json.confirm_delete_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _undoStack.Push(new UndoAction(UndoActionType.Delete, tag.Parent, tag.Key, tag.Value, null));
        _redoStack.Clear();

        if (tag.Parent is JsonObject parentObj && !tag.Key.StartsWith('['))
        {
            parentObj.Remove(tag.Key);
            var parent2 = node.Parent;
            node.Remove();
            if (parent2 != null) UpdateContainerNodeText(parent2);
        }
        else if (tag.Parent is JsonArray parentArr && tag.Key.StartsWith('['))
        {
            int idx = int.Parse(tag.Key.Trim('[', ']'), System.Globalization.CultureInfo.InvariantCulture);
            parentArr.RemoveAt(idx);
            var parentNode = node.Parent;
            node.Remove();
            // Re-index remaining sibling nodes after removal
            if (parentNode != null)
            {
                for (int i = idx; i < parentNode.Nodes.Count; i++)
                {
                    if (parentNode.Nodes[i].Tag is NodeTag sibTag)
                    {
                        sibTag.Key = $"[{i}]";
                        string text = parentNode.Nodes[i].Text;
                        int bracketEnd = text.IndexOf(']');
                        if (bracketEnd >= 0)
                            parentNode.Nodes[i].Text = $"[{i}" + text[bracketEnd..];
                    }
                }
                UpdateContainerNodeText(parentNode);
            }
        }
        else
        {
            var parent2 = node.Parent;
            node.Remove();
            if (parent2 != null) UpdateContainerNodeText(parent2);
        }
        _treeModified = true;
        InvalidateDiffCache();
        _statusLabel.Text = UiStrings.Format("raw_json.deleted", tag.Key);
        _statusLabel.ForeColor = Color.DarkOrange;
    }

    /// <summary>
    /// Updates a container node text to reflect its current size and type.
    /// </summary>
    /// <param name="node">The tree node whose label is updated.</param>
    private static void UpdateContainerNodeText(TreeNode node)
    {
        if (node.Tag is not NodeTag tag) return;
        string key = tag.Key ?? "Root";
        if (tag.Value is JsonObject obj)
            node.Text = $"{key}  {{...}}  ({obj.Size()} properties)";
        else if (tag.Value is JsonArray arr)
            node.Text = $"{key}  [...]  ({arr.Length} items)";
    }

    #endregion

    #region Copy Operations

    /// <summary>
    /// Copies the selected node key to the clipboard.
    /// </summary>
    private void CopyKey()
    {
        if (_treeView.SelectedNode?.Tag is NodeTag tag && tag.Key != null)
            Clipboard.SetText(tag.Key);
    }

    /// <summary>
    /// Copies the selected node value to the clipboard.
    /// </summary>
    private void CopyValue()
    {
        if (_treeView.SelectedNode?.Tag is not NodeTag tag) return;
        string val = tag.Value switch
        {
            JsonObject obj => obj.ToFormattedString(),
            JsonArray arr => JsonParser.Serialize(arr, true),
            _ => FormatValue(tag.Value)
        };
        Clipboard.SetText(val);
    }

    /// <summary>
    /// Copies the JSON path of the selected node to the clipboard.
    /// </summary>
    private void CopyPath()
    {
        if (_treeView.SelectedNode == null) return;
        var parts = new List<string>();
        var current = _treeView.SelectedNode;
        while (current?.Parent != null)
        {
            if (current.Tag is NodeTag tag && tag.Key != null)
                parts.Add(tag.Key.StartsWith('[') ? tag.Key : $".{tag.Key}");
            current = current.Parent;
        }
        parts.Reverse();
        string path = string.Join("", parts).TrimStart('.');
        if (!string.IsNullOrEmpty(path))
            Clipboard.SetText(path);
    }

    #endregion

    #region Context Menu

    /// <summary>
    /// Updates context menu visibility based on the selected node type.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the opening event.</param>
    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var node = _treeView.SelectedNode;
        bool isObj = node?.Tag is NodeTag { Value: JsonObject };
        bool isArr = node?.Tag is NodeTag { Value: JsonArray };
        bool isLeaf = node?.Tag is NodeTag t && t.Value is not JsonObject && t.Value is not JsonArray;
        bool isRoot = node?.Parent == null;
        bool hasNode = node != null;
        bool hasParent = node?.Tag is NodeTag { Parent: not null };

        _contextMenu.Items[0].Visible = isLeaf;     // Edit Value
        _contextMenu.Items[2].Visible = isObj;       // Add Property
        _contextMenu.Items[3].Visible = isArr;       // Add Array Item
        _contextMenu.Items[5].Visible = !isRoot;     // Delete
        _contextMenu.Items[11].Visible = hasNode;    // Export Node
        _contextMenu.Items[12].Visible = hasParent;  // Import Node (needs parent to replace into)
    }

    #endregion

    #region Search

    private readonly List<TreeNode> _searchResults = new();
    private int _searchIndex;
    private string _lastSearchQuery = "";

    private readonly List<List<string>> _searchPaths = new();

    /// <summary>
    /// Executes a search over the current JSON data and selects the first match.
    /// </summary>
    private void OnSearch()
    {
        string query = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        // If the query hasn't changed and we have results, advance to next
        if (query == _lastSearchQuery && _searchPaths.Count > 0)
        {
            FindNext();
            return;
        }

        _lastSearchQuery = query;
        ClearHighlights();
        _searchResults.Clear();
        _searchPaths.Clear();
        _searchIndex = 0;

        if (_saveData == null) return;

        // Search the JSON data structure directly (not the tree nodes)
        // to avoid force-expanding the entire tree.
        var path = new List<string>();
        SearchJsonData(_saveData, query.ToLowerInvariant(), path);

        if (_searchPaths.Count > 0)
        {
            // Navigate to first result by expanding the tree path
            NavigateToSearchResult(0);
            _statusLabel.Text = UiStrings.Format("raw_json.search_found", _searchPaths.Count);
            _statusLabel.ForeColor = Color.Green;
        }
        else
        {
            _statusLabel.Text = UiStrings.Get("raw_json.no_matches_found");
            _statusLabel.ForeColor = Color.Red;
        }
    }

    /// <summary>
    /// Moves selection to the next search result in the tree.
    /// </summary>
    private void FindNext()
    {
        if (_searchPaths.Count == 0) return;

        // Dim previous result
        if (_searchIndex >= 0 && _searchIndex < _searchResults.Count)
            _searchResults[_searchIndex].BackColor = Color.LightYellow;

        _searchIndex = (_searchIndex + 1) % _searchPaths.Count;
        NavigateToSearchResult(_searchIndex);
        _statusLabel.Text = UiStrings.Format("raw_json.match_position", _searchIndex + 1, _searchPaths.Count);
    }

    /// <summary>
    /// Moves selection to the previous search result in the tree.
    /// </summary>
    private void FindPrevious()
    {
        if (_searchPaths.Count == 0) return;
        if (_searchIndex >= 0 && _searchIndex < _searchResults.Count)
            _searchResults[_searchIndex].BackColor = Color.LightYellow;
        _searchIndex = (_searchIndex - 1 + _searchPaths.Count) % _searchPaths.Count;
        NavigateToSearchResult(_searchIndex);
        _statusLabel.Text = UiStrings.Format("raw_json.match_position", _searchIndex + 1, _searchPaths.Count);
    }

    /// <summary>
    /// Recursively searches JSON data for keys and values matching the query.
    /// </summary>
    /// <param name="value">The current JSON value under inspection.</param>
    /// <param name="query">The lowercase query string to match.</param>
    /// <param name="path">The path to the current value within the JSON structure.</param>
    private void SearchJsonData(object? value, string query, List<string> path)
    {
        const int maxResults = 500;
        if (_searchPaths.Count >= maxResults) return;

        if (value is JsonObject obj)
        {
            var names = obj.Names();
            for (int i = 0; i < names.Count; i++)
            {
                if (_searchPaths.Count >= maxResults) return;
                string key = names[i];
                object? child = obj.Get(key);
                path.Add(key);

                // Check if key matches
                if (key.ToLowerInvariant().Contains(query))
                    _searchPaths.Add(new List<string>(path));

                // Check leaf value
                if (child is not JsonObject && child is not JsonArray)
                {
                    string display = FormatValue(child);
                    if (display.ToLowerInvariant().Contains(query))
                        if (_searchPaths.Count == 0 || !PathsEqual(_searchPaths[^1], path))
                            _searchPaths.Add(new List<string>(path));
                }

                // Recurse into children
                if (child is JsonObject || child is JsonArray)
                    SearchJsonData(child, query, path);

                path.RemoveAt(path.Count - 1);
            }
        }
        else if (value is JsonArray arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (_searchPaths.Count >= maxResults) return;
                object? child = arr.Get(i);
                path.Add($"[{i}]");

                if (child is not JsonObject && child is not JsonArray)
                {
                    string display = FormatValue(child);
                    if (display.ToLowerInvariant().Contains(query))
                        _searchPaths.Add(new List<string>(path));
                }

                if (child is JsonObject || child is JsonArray)
                    SearchJsonData(child, query, path);

                path.RemoveAt(path.Count - 1);
            }
        }
    }

    /// <summary>
    /// Compares two JSON path segments for equality.
    /// </summary>
    /// <param name="a">The first path segment list.</param>
    /// <param name="b">The second path segment list.</param>
    /// <returns>True when the paths are identical.</returns>
    private static bool PathsEqual(List<string> a, List<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    /// <summary>
    /// Navigates the tree view to the specified search result index.
    /// </summary>
    /// <param name="index">The zero-based search result index.</param>
    private void NavigateToSearchResult(int index)
    {
        if (index < 0 || index >= _searchPaths.Count) return;

        var path = _searchPaths[index];
        TreeNode? current = _treeView.Nodes.Count > 0 ? _treeView.Nodes[0] : null; // Root node

        _treeView.BeginUpdate();
        for (int p = 0; p < path.Count && current != null; p++)
        {
            // Ensure lazy children are expanded
            if (current.Nodes.Count == 1 && current.Nodes[0].Tag is LazyTag)
            {
                var tag = current.Tag as NodeTag;
                current.Nodes.Clear();
                if (tag?.Value is JsonObject obj)
                    PopulateObjectNode(current, obj, maxDepth: 2, currentDepth: 0);
                else if (tag?.Value is JsonArray arr)
                    PopulateArrayNode(current, arr, maxDepth: 2, currentDepth: 0);
            }

            current.Expand();
            string segment = path[p];

            // Find matching child node
            TreeNode? found = null;
            foreach (TreeNode child in current.Nodes)
            {
                if (child.Tag is NodeTag childTag && childTag.Key == segment)
                {
                    found = child;
                    break;
                }
            }
            current = found;
        }
        _treeView.EndUpdate();

        if (current != null)
        {
            _searchResults.Add(current);
            current.BackColor = Color.Yellow;
            _treeView.SelectedNode = current;
            current.EnsureVisible();
        }
    }

    /// <summary>
    /// Clears the highlighted search result nodes.
    /// </summary>
    private void ClearHighlights()
    {
        foreach (var node in _searchResults)
            node.BackColor = Color.Empty;
        _searchResults.Clear();
    }

    /// <summary>
    /// Handles key commands for tree navigation and edit operations.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The key event arguments.</param>
    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedNode();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.F2)
        {
            BeginEditSelectedNode();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F3 && e.Shift)
        {
            FindPrevious();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.F3 || (e.KeyCode == Keys.Enter && _searchPaths.Count > 0))
        {
            FindNext();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.C)
        {
            CopyValue();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.F)
        {
            _searchBox.Focus();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            _searchBox.Text = "";
            ClearHighlights();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Z)
        {
            Undo();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Y)
        {
            Redo();
            e.Handled = true;
        }
    }

    #endregion

    #region Breadcrumb

    /// <summary>
    /// Cancels inline edit and updates breadcrumb navigation on tree selection.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The tree selection event arguments.</param>
    private void OnTreeNodeSelected(object? sender, TreeViewEventArgs e)
    {
        CancelInlineEdit();
        UpdateBreadcrumb(e.Node);
    }

    /// <summary>
    /// Rebuilds breadcrumb controls for the selected tree node path.
    /// </summary>
    /// <param name="node">The currently selected tree node.</param>
    private void UpdateBreadcrumb(TreeNode? node)
    {
        _breadcrumbPanel.SuspendLayout();
        _breadcrumbPanel.Controls.Clear();

        if (node == null) { _breadcrumbPanel.ResumeLayout(); return; }

        var parts = new List<(string Text, TreeNode Node)>();
        var current = node;
        while (current != null)
        {
            string text = current.Parent == null ? "Root" : (current.Tag is NodeTag tag ? tag.Key ?? "?" : "?");
            parts.Add((text, current));
            current = current.Parent;
        }
        parts.Reverse();

        for (int i = 0; i < parts.Count; i++)
        {
            if (i > 0)
            {
                var sep = new Label { Text = " > ", AutoSize = true, ForeColor = Color.Gray,
                    Margin = new Padding(0, 4, 0, 0) };
                _breadcrumbPanel.Controls.Add(sep);
            }
            var targetNode = parts[i].Node;
            var link = new LinkLabel { Text = parts[i].Text, AutoSize = true,
                Margin = new Padding(0, 3, 0, 0) };
            link.LinkClicked += (_, _) => {
                _treeView.SelectedNode = targetNode;
                targetNode.EnsureVisible();
            };
            _breadcrumbPanel.Controls.Add(link);
        }
        _breadcrumbPanel.ResumeLayout();
    }

    #endregion

    #region Export / Import / Diff

    /// <summary>
    /// Exports the active JSON document to a file.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the export action.</param>
    private void OnExport(object? sender, EventArgs e)
    {
        var data = _isShowingAccount ? _accountData : _saveData;
        if (data == null) return;
        using var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            Title = UiStrings.Get("raw_json.export_title")
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, RawJsonLogic.ToDisplayString(data));
            _statusLabel.Text = UiStrings.Format("raw_json.exported", Path.GetFileName(dialog.FileName));
            _statusLabel.ForeColor = Color.Green;
        }
    }

    /// <summary>
    /// Imports a JSON file and replaces the current editor document.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the import action.</param>
    private void OnImport(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = UiStrings.Get("raw_json.import_title")
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string json = File.ReadAllText(dialog.FileName);
                var parsed = RawJsonLogic.ParseJson(json);
                if (_isShowingAccount)
                    _accountData = parsed;
                else
                    _saveData = parsed;

                InvalidateDisplayCache();
                if (_viewMode == ViewMode.Tree)
                    BuildTree(parsed);
                else if (_viewMode == ViewMode.Text)
                    _syntaxTextBox.JsonText = GetDisplayString(parsed);
                else
                    LoadSplitView(parsed);

                _treeModified = true;
                InvalidateDiffCache();
                _statusLabel.Text = UiStrings.Format("raw_json.imported", Path.GetFileName(dialog.FileName));
                _statusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UiStrings.Format("raw_json.import_error", ex.Message),
                    UiStrings.Get("raw_json.import_error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Exports the selected tree node's value (with all children) to a JSON file
    /// using deobfuscated (human-readable) keys.
    /// </summary>
    private void ExportSelectedNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag) return;

        string nodeName = tag.Key ?? "root";
        string safeName = nodeName.Trim('[', ']');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "node";

        using var dialog = new SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = $"{safeName}.json",
            Title = UiStrings.Get("raw_json.export_node_title")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            string json = RawJsonLogic.SerializeValue(tag.Value);
            File.WriteAllText(dialog.FileName, json);
            _statusLabel.Text = UiStrings.Format("raw_json.exported_node", Path.GetFileName(dialog.FileName));
            _statusLabel.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("raw_json.import_error", ex.Message),
                UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Imports a JSON file and replaces the selected tree node's value with the
    /// parsed content, preserving the node's key/position within its parent container.
    /// </summary>
    private void ImportSelectedNode()
    {
        var node = _treeView.SelectedNode;
        if (node?.Tag is not NodeTag tag || tag.Parent == null || tag.Key == null) return;

        using var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = UiStrings.Get("raw_json.import_node_title")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            object? newValue = RawJsonLogic.ParseValue(json);

            // Record undo before modifying
            _undoStack.Push(new UndoAction(UndoActionType.Edit, tag.Parent, tag.Key, tag.Value, newValue));
            _redoStack.Clear();

            // Replace the value in the parent container
            if (tag.Parent is JsonObject parentObj && !tag.Key.StartsWith('['))
            {
                parentObj.Set(tag.Key, newValue);
            }
            else if (tag.Parent is JsonArray parentArr && tag.Key.StartsWith('['))
            {
                int idx = int.Parse(tag.Key.Trim('[', ']'), System.Globalization.CultureInfo.InvariantCulture);
                parentArr.Set(idx, newValue);
            }

            // Rebuild the tree node in place
            var parentNode = node.Parent;
            if (parentNode != null)
            {
                int nodeIndex = parentNode.Nodes.IndexOf(node);
                var replacement = CreateValueNode(tag.Key, newValue, tag.Parent, 2, 0);
                _treeView.BeginUpdate();
                parentNode.Nodes.RemoveAt(nodeIndex);
                parentNode.Nodes.Insert(nodeIndex, replacement);
                UpdateContainerNodeText(parentNode);
                _treeView.SelectedNode = replacement;
                _treeView.EndUpdate();
            }

            _treeModified = true;
            InvalidateDiffCache();
            _statusLabel.Text = UiStrings.Format("raw_json.imported_node", Path.GetFileName(dialog.FileName));
            _statusLabel.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("raw_json.import_error", ex.Message),
                UiStrings.Get("raw_json.import_error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Displays a colour-coded diff of the current JSON against the original loaded JSON.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the diff action.</param>
    private async void OnShowDiff(object? sender, EventArgs e)
    {
        var data = _isShowingAccount ? _accountData : _saveData;
        if (data == null || _originalJsonCompressed == null) return;

        // If we have a cached diff and nothing has changed, reuse it.
        if (!_diffCacheDirty && _cachedDiffLines != null)
        {
            ShowDiffDialog(_cachedDiffLines);
            return;
        }

        // Show wait cursor while computing
        Cursor = Cursors.WaitCursor;
        _statusLabel.Text = UiStrings.Get("raw_json.diff_computing");
        _statusLabel.ForeColor = Color.Blue;

        List<RawJsonLogic.DiffLine> diffLines;
        try
        {
            // Run the expensive serialization + diff on a background thread.
            // Decompress the original baseline on the background thread to avoid
            // holding the full uncompressed string in long-lived memory.
            var compressedOrig = _originalJsonCompressed;
            diffLines = await Task.Run(() =>
            {
                string origJson = DecompressString(compressedOrig);
                string currentJson = RawJsonLogic.ToDisplayString(data);
                return RawJsonLogic.ComputeCompactDiff(origJson, currentJson);
            });
        }
        catch (Exception ex)
        {
            Cursor = Cursors.Default;
            _statusLabel.Text = UiStrings.Format("raw_json.diff_error", ex.Message);
            _statusLabel.ForeColor = Color.Red;
            return;
        }
        finally
        {
            Cursor = Cursors.Default;
        }

        // Cache the result so repeated clicks without data changes are free.
        _cachedDiffLines = diffLines;
        _diffCacheDirty = false;

        if (diffLines.Count == 0)
        {
            _statusLabel.Text = UiStrings.Get("raw_json.diff_no_changes");
            _statusLabel.ForeColor = Color.Gray;
            MessageBox.Show(this, UiStrings.Get("raw_json.diff_no_changes"), UiStrings.Get("raw_json.diff_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _statusLabel.Text = "";
        ShowDiffDialog(diffLines);
    }

    /// <summary>
    /// Creates and shows the diff dialog form with coloured diff output and navigation.
    /// Separated from <see cref="OnShowDiff"/> so it can be reused for cached results.
    /// </summary>
    private void ShowDiffDialog(List<RawJsonLogic.DiffLine> diffLines)
    {
        using var diffForm = new Form
        {
            Text = UiStrings.Get("raw_json.diff_title"),
            Size = new Size(900, 700),
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false
        };

        // RichTextBox for coloured diff output
        var richDiff = new RichTextBox
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 10),
            WordWrap = false,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        // Efficiently populate the RichTextBox: build all text first, then colour ranges.
        // This is MUCH faster than line-by-line AppendText with per-line colour changes.
        PopulateDiffRichTextBox(richDiff, diffLines);

        // Count changed lines for navigation
        var changeLineIndices = new List<int>();
        for (int idx = 0; idx < diffLines.Count; idx++)
        {
            if (diffLines[idx].Type is RawJsonLogic.DiffLineType.Added or RawJsonLogic.DiffLineType.Removed)
                changeLineIndices.Add(idx);
        }
        int currentChangeIdx = -1;

        // Toolbar with navigation
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5, 3, 5, 3)
        };

        var changeCountLabel = new Label
        {
            Text = UiStrings.Format("raw_json.diff_change_count", changeLineIndices.Count),
            AutoSize = true,
            Margin = new Padding(0, 6, 10, 0)
        };

        var prevChangeBtn = new Button
        {
            Text = UiStrings.Get("raw_json.diff_prev_change"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(90, 0)
        };

        var nextChangeBtn = new Button
        {
            Text = UiStrings.Get("raw_json.diff_next_change"),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(90, 0)
        };

        // Jump to line helper
        void JumpToLine(int lineIdx)
        {
            if (lineIdx < 0 || lineIdx >= richDiff.Lines.Length) return;
            int charIdx = richDiff.GetFirstCharIndexFromLine(lineIdx);
            richDiff.SelectionStart = charIdx;
            richDiff.SelectionLength = 0;
            richDiff.ScrollToCaret();
        }

        prevChangeBtn.Click += (_, _) =>
        {
            if (changeLineIndices.Count == 0) return;
            currentChangeIdx = (currentChangeIdx - 1 + changeLineIndices.Count) % changeLineIndices.Count;
            JumpToLine(changeLineIndices[currentChangeIdx]);
            changeCountLabel.Text = UiStrings.Format("raw_json.diff_change_position", currentChangeIdx + 1, changeLineIndices.Count);
        };

        nextChangeBtn.Click += (_, _) =>
        {
            if (changeLineIndices.Count == 0) return;
            currentChangeIdx = (currentChangeIdx + 1) % changeLineIndices.Count;
            JumpToLine(changeLineIndices[currentChangeIdx]);
            changeCountLabel.Text = UiStrings.Format("raw_json.diff_change_position", currentChangeIdx + 1, changeLineIndices.Count);
        };

        toolbar.Controls.AddRange([changeCountLabel, prevChangeBtn, nextChangeBtn]);

        var closeBtn = new Button
        {
            Text = UiStrings.Get("common.close"),
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Bottom,
            Height = 35
        };
        diffForm.AcceptButton = closeBtn;
        diffForm.Controls.Add(richDiff);
        diffForm.Controls.Add(toolbar);
        diffForm.Controls.Add(closeBtn);
        diffForm.ShowDialog(this);

        // Explicitly clear the RichTextBox content before disposal to help
        // release the RTF document's internal memory promptly.
        richDiff.Clear();
    }

    /// <summary>
    /// Populates a RichTextBox with coloured diff lines efficiently, including line numbers
    /// and JSON path context headers above each hunk.
    /// Builds all text in a StringBuilder first, sets it in one shot, then applies
    /// colour ranges - this is orders of magnitude faster than line-by-line AppendText.
    /// </summary>
    private static void PopulateDiffRichTextBox(RichTextBox rtb, List<RawJsonLogic.DiffLine> lines)
    {
        var sb = new System.Text.StringBuilder();
        // Record which line indices need colouring
        var lineColors = new List<(int LineIdx, Color Bg, Color Fg)>();

        // Calculate max line number width for gutter alignment
        int maxOldLine = 0, maxNewLine = 0;
        foreach (var dl in lines)
        {
            if (dl.OldLineNum > maxOldLine) maxOldLine = dl.OldLineNum;
            if (dl.NewLineNum > maxNewLine) maxNewLine = dl.NewLineNum;
        }
        int gutterWidth = Math.Max(maxOldLine.ToString(CultureInfo.InvariantCulture).Length, maxNewLine.ToString(CultureInfo.InvariantCulture).Length);
        if (gutterWidth < 3) gutterWidth = 3;
        string gutterFmt = new(' ', gutterWidth);

        for (int i = 0; i < lines.Count; i++)
        {
            var dl = lines[i];
            switch (dl.Type)
            {
                case RawJsonLogic.DiffLineType.Header:
                    sb.Append(new string(' ', gutterWidth * 2 + 5));
                    sb.AppendLine("@@ " + dl.Text + " @@");
                    lineColors.Add((i, Color.FromArgb(235, 235, 255), Color.FromArgb(80, 80, 160)));
                    break;
                case RawJsonLogic.DiffLineType.Added:
                    sb.Append(new string(' ', gutterWidth + 1));
                    sb.Append(dl.NewLineNum.ToString(CultureInfo.InvariantCulture).PadLeft(gutterWidth));
                    sb.Append("  + ");
                    sb.AppendLine(dl.Text);
                    lineColors.Add((i, Color.FromArgb(220, 255, 220), Color.DarkGreen));
                    break;
                case RawJsonLogic.DiffLineType.Removed:
                    sb.Append(dl.OldLineNum.ToString(CultureInfo.InvariantCulture).PadLeft(gutterWidth));
                    sb.Append(new string(' ', gutterWidth + 1));
                    sb.Append("  - ");
                    sb.AppendLine(dl.Text);
                    lineColors.Add((i, Color.FromArgb(255, 220, 220), Color.DarkRed));
                    break;
                case RawJsonLogic.DiffLineType.Separator:
                    sb.Append(new string(' ', gutterWidth * 2 + 3));
                    sb.AppendLine("  ---");
                    lineColors.Add((i, Color.FromArgb(240, 240, 240), Color.Gray));
                    break;
                default:
                    sb.Append(dl.OldLineNum.ToString(CultureInfo.InvariantCulture).PadLeft(gutterWidth));
                    sb.Append(' ');
                    sb.Append(dl.NewLineNum.ToString(CultureInfo.InvariantCulture).PadLeft(gutterWidth));
                    sb.Append("    ");
                    sb.AppendLine(dl.Text);
                    break;
            }
        }

        // Set all text in one shot (fast)
        rtb.Text = sb.ToString();

        // Now apply colours to changed/separator/header lines only (skip unchanged lines)
        rtb.SuspendLayout();
        foreach (var (lineIdx, bg, fg) in lineColors)
        {
            int startChar = rtb.GetFirstCharIndexFromLine(lineIdx);
            if (startChar < 0) continue;
            int lineEnd = (lineIdx + 1 < rtb.Lines.Length)
                ? rtb.GetFirstCharIndexFromLine(lineIdx + 1)
                : rtb.TextLength;
            int length = lineEnd - startChar;
            if (length <= 0) continue;

            rtb.Select(startChar, length);
            rtb.SelectionBackColor = bg;
            rtb.SelectionColor = fg;
        }
        rtb.SelectionStart = 0;
        rtb.SelectionLength = 0;
        rtb.ResumeLayout();
    }

    #endregion

    #region Undo / Redo

    /// <summary>
    /// Undoes the last JSON edit action in the raw editor.
    /// </summary>
    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        ApplyUndoRedo(action, isUndo: true);
        _redoStack.Push(action);
        _treeModified = true;
        InvalidateDiffCache();
        RebuildCurrentTree();
        _statusLabel.Text = UiStrings.Get("raw_json.undone");
        _statusLabel.ForeColor = Color.DarkOrange;
    }

    /// <summary>
    /// Redoes the last undone JSON edit action.
    /// </summary>
    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        var action = _redoStack.Pop();
        ApplyUndoRedo(action, isUndo: false);
        _undoStack.Push(action);
        _treeModified = true;
        InvalidateDiffCache();
        RebuildCurrentTree();
        _statusLabel.Text = UiStrings.Get("raw_json.redone");
        _statusLabel.ForeColor = Color.DarkOrange;
    }

    /// <summary>
    /// Parses an array index from a node key label such as [0].
    /// </summary>
    /// <param name="key">The key label containing the array index.</param>
    /// <returns>The parsed integer index.</returns>
    private static int ParseArrayIndex(string key) =>
        int.Parse(key.Trim('[', ']'), System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Applies an undo or redo action to the underlying JSON container.
    /// </summary>
    /// <param name="action">The action to apply.</param>
    /// <param name="isUndo">True when applying an undo, false when redoing.</param>
    private void ApplyUndoRedo(UndoAction action, bool isUndo)
    {
        var value = isUndo ? action.OldValue : action.NewValue;

        switch (action.Type)
        {
            case UndoActionType.Edit:
                if (action.Parent is JsonObject editObj && !action.Key.StartsWith('['))
                    editObj.Set(action.Key, value);
                else if (action.Parent is JsonArray editArr && action.Key.StartsWith('['))
                    editArr.Set(ParseArrayIndex(action.Key), value);
                break;
            case UndoActionType.Add:
                if (isUndo)
                {
                    if (action.Parent is JsonObject addObj && !action.Key.StartsWith('['))
                        addObj.Remove(action.Key);
                    else if (action.Parent is JsonArray addArr && action.Key.StartsWith('['))
                        addArr.RemoveAt(ParseArrayIndex(action.Key));
                }
                else
                {
                    if (action.Parent is JsonObject addObj && !action.Key.StartsWith('['))
                        addObj.Add(action.Key, action.NewValue);
                    else if (action.Parent is JsonArray addArr)
                        addArr.Add(action.NewValue);
                }
                break;
            case UndoActionType.Delete:
                if (isUndo)
                {
                    if (action.Parent is JsonObject delObj && !action.Key.StartsWith('['))
                        delObj.Add(action.Key, action.OldValue);
                    else if (action.Parent is JsonArray delArr && action.Key.StartsWith('['))
                        delArr.Insert(ParseArrayIndex(action.Key), action.OldValue);
                }
                else
                {
                    if (action.Parent is JsonObject delObj && !action.Key.StartsWith('['))
                        delObj.Remove(action.Key);
                    else if (action.Parent is JsonArray delArr && action.Key.StartsWith('['))
                        delArr.RemoveAt(ParseArrayIndex(action.Key));
                }
                break;
        }
    }

    /// <summary>
    /// Rebuilds the current tree view from the active JSON document.
    /// </summary>
    private void RebuildCurrentTree()
    {
        var data = _isShowingAccount ? _accountData : _saveData;
        if (data != null && _viewMode != ViewMode.Text)
            BuildTree(data);
    }

    #endregion

    #region Text View Handlers

    /// <summary>
    /// Formats the JSON text in the editor and updates the status message.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the format action.</param>
    private void OnFormat(object? sender, EventArgs e)
    {
        try
        {
            _syntaxTextBox.JsonText = RawJsonLogic.FormatJson(_syntaxTextBox.JsonText);
            _statusLabel.Text = UiStrings.Get("raw_json.formatted");
            _statusLabel.ForeColor = Color.Green;
        }
        catch (JsonException ex)
        {
            _statusLabel.Text = $"Error: {ex.Message}";
            _statusLabel.ForeColor = Color.Red;
        }
    }

    /// <summary>
    /// Validates the JSON text in the editor and reports the result.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">Event arguments for the validation action.</param>
    private void OnValidate(object? sender, EventArgs e)
    {
        try
        {
            RawJsonLogic.ParseJson(_syntaxTextBox.JsonText);
            _statusLabel.Text = UiStrings.Get("raw_json.json_valid");
            _statusLabel.ForeColor = Color.Green;
        }
        catch (JsonException ex)
        {
            _statusLabel.Text = UiStrings.Format("raw_json.invalid_json", ex.Message);
            _statusLabel.ForeColor = Color.Red;
        }
    }

    #endregion

    #region Drag & Drop

    /// <summary>
    /// Begins a drag operation for the selected tree item.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The item drag event arguments.</param>
    private void OnTreeItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is not TreeNode node || node.Tag is not NodeTag) return;
        if (node.Parent == null) return;
        DoDragDrop(node, DragDropEffects.Move);
    }

    /// <summary>
    /// Determines whether the dragged tree item may be dropped on the current target.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The drag event arguments.</param>
    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        e.Effect = DragDropEffects.None;
        if (!e.Data!.GetDataPresent(typeof(TreeNode))) return;

        var draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode))!;
        var pt = _treeView.PointToClient(new Point(e.X, e.Y));
        var targetNode = _treeView.GetNodeAt(pt);

        if (targetNode == null || targetNode == draggedNode) return;
        if (targetNode.Parent != draggedNode.Parent) return;
        if (draggedNode.Parent?.Tag is not NodeTag parentTag) return;
        if (parentTag.Value is not JsonArray && parentTag.Value is not JsonObject) return;

        e.Effect = DragDropEffects.Move;
        _treeView.SelectedNode = targetNode;
    }

    /// <summary>
    /// Handles dropping a dragged tree item within the same parent container.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The drag event arguments.</param>
    private void OnTreeDragDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data!.GetDataPresent(typeof(TreeNode))) return;
        var draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode))!;
        var pt = _treeView.PointToClient(new Point(e.X, e.Y));
        var targetNode = _treeView.GetNodeAt(pt);

        if (targetNode == null || targetNode == draggedNode) return;
        if (targetNode.Parent != draggedNode.Parent) return;
        if (draggedNode.Parent?.Tag is not NodeTag parentTag) return;

        int fromIndex = draggedNode.Index;
        int toIndex = targetNode.Index;
        if (fromIndex == toIndex) return;

        var parentTreeNode = draggedNode.Parent;

        if (parentTag.Value is JsonArray arr)
        {
            var item = arr.Get(fromIndex);
            arr.RemoveAt(fromIndex);
            arr.Insert(toIndex, item);

            _treeView.BeginUpdate();
            parentTreeNode!.Nodes.Remove(draggedNode);
            parentTreeNode.Nodes.Insert(toIndex, draggedNode);

            for (int i = 0; i < parentTreeNode.Nodes.Count; i++)
            {
                if (parentTreeNode.Nodes[i].Tag is NodeTag childTag)
                {
                    childTag.Key = $"[{i}]";
                    string text = parentTreeNode.Nodes[i].Text;
                    int bracketEnd = text.IndexOf(']');
                    if (bracketEnd >= 0)
                        parentTreeNode.Nodes[i].Text = $"[{i}" + text[bracketEnd..];
                }
            }
            _treeView.EndUpdate();
            _treeView.SelectedNode = draggedNode;
            _treeModified = true;
            InvalidateDiffCache();
            _statusLabel.Text = UiStrings.Format("raw_json.reordered", fromIndex, toIndex);
            _statusLabel.ForeColor = Color.DarkOrange;
        }
        else if (parentTag.Value is JsonObject obj)
        {
            obj.Reorder(fromIndex, toIndex);

            _treeView.BeginUpdate();
            parentTreeNode!.Nodes.Remove(draggedNode);
            parentTreeNode.Nodes.Insert(toIndex, draggedNode);
            _treeView.EndUpdate();
            _treeView.SelectedNode = draggedNode;
            _treeModified = true;
            InvalidateDiffCache();
            _statusLabel.Text = UiStrings.Format("raw_json.reordered", fromIndex, toIndex);
            _statusLabel.ForeColor = Color.DarkOrange;
        }
    }

    #endregion

    #region Helper Types

    /// <summary>
    /// Stores the value, parent container and key information for a tree node.
    /// </summary>
    private class NodeTag
    {
        public object? Value { get; set; }
        public object? Parent { get; }
        public string? Key { get; set; }

        public NodeTag(object? value, object? parent, string? key)
        {
            Value = value;
            Parent = parent;
            Key = key;
        }
    }

    /// <summary>
    /// Marker object used to indicate that a tree node has lazy loaded children.
    /// </summary>
    private class LazyTag
    {
        public static readonly LazyTag Instance = new();
    }

    #endregion

    /// <summary>
    /// Applies localisation strings to the control text and context menu items.
    /// </summary>
    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("raw_json.title");
        _treeViewButton.Text = UiStrings.Get("raw_json.tree_view");
        _textViewButton.Text = UiStrings.Get("raw_json.text_view");
        _splitViewButton.Text = UiStrings.Get("raw_json.split_view");
        _formatButton.Text = UiStrings.Get("raw_json.format");
        _validateButton.Text = UiStrings.Get("raw_json.validate");
        _expandAllButton.Text = UiStrings.Get("raw_json.expand_all");
        _stopExpandBtn.Text = UiStrings.Get("raw_json.stop");
        _collapseAllButton.Text = UiStrings.Get("raw_json.collapse_all");
        _searchBox.PlaceholderText = UiStrings.Get("raw_json.search_placeholder");
        _searchButton.Text = UiStrings.Get("raw_json.find");
        _searchBackButton.Text = UiStrings.Get("raw_json.find_prev");
        _exportButton.Text = UiStrings.Get("raw_json.export");
        _importButton.Text = UiStrings.Get("raw_json.import");
        _diffButton.Text = UiStrings.Get("raw_json.diff");

        // Context menu items (by position, skipping separators)
        if (_contextMenu.Items.Count >= 13)
        {
            _contextMenu.Items[0].Text = UiStrings.Get("raw_json.ctx_edit_value");
            _contextMenu.Items[2].Text = UiStrings.Get("raw_json.ctx_add_property");
            _contextMenu.Items[3].Text = UiStrings.Get("raw_json.ctx_add_array_item");
            _contextMenu.Items[5].Text = UiStrings.Get("raw_json.ctx_delete");
            _contextMenu.Items[7].Text = UiStrings.Get("raw_json.ctx_copy_key");
            _contextMenu.Items[8].Text = UiStrings.Get("raw_json.ctx_copy_value");
            _contextMenu.Items[9].Text = UiStrings.Get("raw_json.ctx_copy_path");
            _contextMenu.Items[11].Text = UiStrings.Get("raw_json.ctx_export_node");
            _contextMenu.Items[12].Text = UiStrings.Get("raw_json.ctx_import_node");
        }
    }
}
