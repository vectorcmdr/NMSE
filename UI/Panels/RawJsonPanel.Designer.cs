#nullable enable
using NMSE.Core;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class RawJsonPanel
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code
    private void InitializeComponent()
    {
        this.SuspendLayout();
        // 
        // RawJsonPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(5)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // title
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // toolbar row 1 (file / search / export)
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // toolbar row 2 (view buttons)
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // breadcrumb
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // content

        _titleLabel = new Label
        {
            Text = "Raw JSON Editor",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 5)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);

        // -- Row 1: file selector, export, import, diff, search --
        var toolbarRow1 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };

        _fileSelector = new ComboBox
        {
            Width = 160,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 3, 3, 3)
        };
        _fileSelector.SelectedIndexChanged += OnFileSelectorChanged;

        var fileSep = new Label { Text = "|", AutoSize = true, Margin = new Padding(5, 6, 5, 0), ForeColor = Color.Gray };

        _exportButton = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(60, 0) };
        _exportButton.Click += OnExport;

        _importButton = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(60, 0) };
        _importButton.Click += OnImport;

        _diffButton = new Button { Text = "Show Changes", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(90, 0) };
        _diffButton.Click += OnShowDiff;

        var searchSep = new Label { Text = "|", AutoSize = true, Margin = new Padding(5, 6, 5, 0), ForeColor = Color.Gray };

        _searchBox = new TextBox { Width = 200, PlaceholderText = "Search keys or values..." };
        _searchBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { OnSearch(); e.SuppressKeyPress = true; } };

        _searchBackButton = new Button { Text = "◀", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(35, 0) };
        _searchBackButton.Click += (_, _) => FindPrevious();

        _searchButton = new Button { Text = "Find", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(50, 0) };
        _searchButton.Click += (_, _) => OnSearch();

        _clearSearchButton = new Button { Text = "X", Width = 30 };
        _clearSearchButton.Click += (_, _) => { _searchBox.Text = ""; ClearHighlights(); };

        toolbarRow1.Controls.AddRange([_fileSelector, fileSep, _exportButton, _importButton, _diffButton,
            searchSep, _searchBox, _clearSearchButton, _searchBackButton, _searchButton]);

        // -- Row 2: view buttons, expand/collapse, format, validate, status --
        var toolbarRow2 = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };

        _treeViewButton = new Button { Text = "Tree View", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0), Enabled = false };
        _treeViewButton.Click += (_, _) => ShowTreeView();

        _textViewButton = new Button { Text = "Text View", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _textViewButton.Click += (_, _) => ShowTextView();

        _splitViewButton = new Button { Text = "Split View", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _splitViewButton.Click += (_, _) => ShowSplitView();

        var viewSep = new Label { Text = "|", AutoSize = true, Margin = new Padding(5, 6, 5, 0), ForeColor = Color.Gray };

        _expandAllButton = new Button { Text = "Expand All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _expandAllButton.Click += async (_, _) => await ExpandAllBatchedAsync();

        _stopExpandBtn = new Button { Text = "Stop", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(50, 0), Visible = false };
        _stopExpandBtn.Click += (_, _) => _cancelExpand = true;

        _collapseAllButton = new Button { Text = "Collapse All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _collapseAllButton.Click += (_, _) =>
        {
            var tv = _treeView!;
            tv.BeginUpdate();
            tv.CollapseAll();
            if (tv.Nodes.Count > 0) tv.Nodes[0].Expand();
            tv.EndUpdate();
        };

        _formatButton = new Button { Text = "Format", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 0), Visible = false };
        _formatButton.Click += OnFormat;

        _validateButton = new Button { Text = "Validate", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 0), Visible = false };
        _validateButton.Click += OnValidate;

        _statusLabel = new Label { Text = "", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(10, 6, 0, 0) };

        toolbarRow2.Controls.AddRange([_treeViewButton, _textViewButton, _splitViewButton, viewSep,
            _expandAllButton, _stopExpandBtn, _collapseAllButton,
            _formatButton, _validateButton, _statusLabel]);

        // -- Tree view --
        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9.5f),
            LabelEdit = true,
            HideSelection = false,
            ShowNodeToolTips = true,
            FullRowSelect = true,
            ImageList = CreateTypeIconList(),
            AllowDrop = true
        };
        _treeView.AfterLabelEdit += OnAfterLabelEdit;
        _treeView.NodeMouseDoubleClick += OnNodeDoubleClick;
        _treeView.BeforeExpand += OnBeforeExpand;
        _treeView.KeyDown += OnTreeKeyDown;
        _treeView.AfterSelect += OnTreeNodeSelected;
        _treeView.ItemDrag += OnTreeItemDrag;
        _treeView.DragOver += OnTreeDragOver;
        _treeView.DragDrop += OnTreeDragDrop;

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("Edit Value", null, (_, _) => BeginEditSelectedNode());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Add Property", null, (_, _) => AddProperty());
        _contextMenu.Items.Add("Add Array Item", null, (_, _) => AddArrayItem());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Delete", null, (_, _) => DeleteSelectedNode());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Copy Key", null, (_, _) => CopyKey());
        _contextMenu.Items.Add("Copy Value", null, (_, _) => CopyValue());
        _contextMenu.Items.Add("Copy Path", null, (_, _) => CopyPath());
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("Export Node...", null, (_, _) => ExportSelectedNode());
        _contextMenu.Items.Add("Import Node...", null, (_, _) => ImportSelectedNode());
        _contextMenu.Opening += OnContextMenuOpening;
        _treeView.ContextMenuStrip = _contextMenu;

        _treePanel = new Panel { Dock = DockStyle.Fill };
        _treePanel.Controls.Add(_treeView);

        _syntaxTextBox = new JsonSyntaxTextBox { Dock = DockStyle.Fill };
        _syntaxTextBox.TextModified += (_, _) => { _textModifiedSinceSwitch = true; InvalidateDiffCache(); };
        _textPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
        _textPanel.Controls.Add(_syntaxTextBox);

        // Split view: tree on left, syntax text on right
        _splitSyntaxTextBox = new JsonSyntaxTextBox { Dock = DockStyle.Fill };
        _splitSyntaxTextBox.TextModified += (_, _) => OnSplitTextModified();

        _splitTreeView = new TreeView
        {
            Dock = DockStyle.Fill,
            Font = _treeView.Font, // share the same Font instance
            LabelEdit = true,
            HideSelection = false,
            ShowNodeToolTips = true,
            FullRowSelect = true,
            ImageList = _treeView.ImageList,
            AllowDrop = true
        };
        _splitTreeView.AfterLabelEdit += OnAfterLabelEdit;
        _splitTreeView.BeforeExpand += OnBeforeExpand;
        _splitTreeView.AfterSelect += OnSplitTreeNodeSelected;

        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 300 // overridden to 1/3 ratio in LoadSplitView
        };

        _splitContainer.Panel1.Controls.Add(_splitTreeView);
        _splitContainer.Panel2.Controls.Add(_splitSyntaxTextBox);

        _splitPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
        _splitPanel.Controls.Add(_splitContainer);

        _breadcrumbPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5, 0, 5, 0),
            MaximumSize = new Size(0, 28)
        };

        layout.Controls.Add(_titleLabel, 0, 0);
        layout.Controls.Add(toolbarRow1, 0, 1);
        layout.Controls.Add(toolbarRow2, 0, 2);
        layout.Controls.Add(_breadcrumbPanel, 0, 3);

        // Content panel hosts all three view panels; we show/hide + BringToFront
        var contentPanel = new Panel { Dock = DockStyle.Fill };
        contentPanel.Controls.Add(_splitPanel);
        contentPanel.Controls.Add(_textPanel);
        contentPanel.Controls.Add(_treePanel);
        layout.Controls.Add(contentPanel, 0, 4);

        Controls.Add(layout);
        ResumeLayout(false);
        PerformLayout();
    }

    private TreeView _treeView = null!;
    private JsonSyntaxTextBox _syntaxTextBox = null!;
    private Label _titleLabel = null!;
    private Button _treeViewButton = null!;
    private Button _textViewButton = null!;
    private Button _splitViewButton = null!;
    private Button _formatButton = null!;
    private Button _validateButton = null!;
    private Button _expandAllButton = null!;
    private Button _collapseAllButton = null!;
    private TextBox _searchBox = null!;
    private Button _searchButton = null!;
    private Button _clearSearchButton = null!;
    private Label _statusLabel = null!;
    private Panel _treePanel = null!;
    private Panel _textPanel = null!;
    private Panel _splitPanel = null!;
    private SplitContainer _splitContainer = null!;
    private TreeView _splitTreeView = null!;
    private JsonSyntaxTextBox _splitSyntaxTextBox = null!;
    private ContextMenuStrip _contextMenu = null!;
    private ComboBox _fileSelector = null!;
    private Button _stopExpandBtn = null!;
    private Button _searchBackButton = null!;
    private Button _exportButton = null!;
    private Button _importButton = null!;
    private Button _diffButton = null!;
    private FlowLayoutPanel _breadcrumbPanel = null!;
}
