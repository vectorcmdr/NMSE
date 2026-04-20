using NMSE.Core;
using NMSE.Data;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class MultitoolPanel
{
    private System.ComponentModel.IContainer components = null;

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
        // MultitoolPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        // Main layout: 1 column, stack everything vertically
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
            AutoSize = true
        };

        // Title
        _titleLabel = new Label
        {
            Text = "Multi-tools",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 1)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);

        _primaryToolLabel = new Label
        {
            Text = "Primary Multi-tool: (none)",
            AutoSize = true,
            Padding = new Padding(8, 6, 0, 0),
            ForeColor = SystemColors.GrayText
        };

        var titlePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        titlePanel.Controls.Add(_titleLabel);
        titlePanel.Controls.Add(_primaryToolLabel);
        rootLayout.Controls.Add(titlePanel, 0, 0);

        // Main two-column panel
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = 50 });
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = 50 });

        // Left panel: selector, name, class, seed
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true
        };
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        int leftRow = 0;

        _detailsLabel = new Label
        {
            Text = "Multitool Details",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 1)
        };
        FontManager.ApplyHeadingFont(_detailsLabel, 10);
        leftPanel.Controls.Add(_detailsLabel, 0, leftRow);
        leftPanel.SetColumnSpan(_detailsLabel, 2);
        leftRow++;

        _toolSelector = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _toolSelector.SelectedIndexChanged += OnToolSelected;
        _selectLabel = AddRow(leftPanel, "Select Multi-tool:", _toolSelector, leftRow++);

        _toolName = new TextBox { Dock = DockStyle.Fill };
        _toolName.Leave += OnToolNameChanged;
        _nameLabel = AddRow(leftPanel, "Name:", _toolName, leftRow++);

        _toolType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _toolType.Items.AddRange(MultitoolLogic.GetToolTypeItems());
        _typeLabel = AddRow(leftPanel, "Type:", _toolType, leftRow++);

        _toolClass = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _toolClass.Items.AddRange(MultitoolLogic.ToolClasses);
        _classLabel = AddRow(leftPanel, "Class:", _toolClass, leftRow++);

        var seedPanel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        _toolSeed = new TextBox { Dock = DockStyle.Fill };
        _generateSeedBtn = new Button
        {
            Text = "Generate",
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(70, 0)
        };
        _generateSeedBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            _toolSeed.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        };
        seedPanel.Controls.Add(_toolSeed);
        seedPanel.Controls.Add(_generateSeedBtn);
        _seedLabel = AddRow(leftPanel, "Seed:", seedPanel, leftRow++);

        // Right panel: Base Stats
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true
        };
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        int rightRow = 0;

        _statsLabel = new Label
        {
            Text = "Base Stats",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 1)
        };
        FontManager.ApplyHeadingFont(_statsLabel, 10);
        rightPanel.Controls.Add(_statsLabel, 0, rightRow);
        rightPanel.SetColumnSpan(_statsLabel, 2);
        rightRow++;

        _damageField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _damageLabel = AddRow(rightPanel, "Damage:", _damageField, rightRow++);

        _miningField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _miningLabel = AddRow(rightPanel, "Mining:", _miningField, rightRow++);

        _scanField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _scanLabel = AddRow(rightPanel, "Scan:", _scanField, rightRow++);

        mainPanel.Controls.Add(leftPanel, 0, 0);
        mainPanel.Controls.Add(rightPanel, 1, 0);

        rootLayout.Controls.Add(mainPanel, 0, 1);

        // Buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        _deleteBtn = new Button { Text = "Delete Multitool", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(110, 0) };
        _deleteBtn.Click += OnDeleteTool;
        _exportBtn = new Button { Text = "Export Multitool", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _exportBtn.Click += OnExportTool;
        _importBtn = new Button { Text = "Import Multitool", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _importBtn.Click += OnImportTool;
        _makePrimaryBtn = new Button { Text = "Make Primary", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(88, 0) };
        _makePrimaryBtn.Click += OnMakePrimary;
        buttonPanel.Controls.Add(_deleteBtn);
        buttonPanel.Controls.Add(_exportBtn);
        buttonPanel.Controls.Add(_importBtn);
        buttonPanel.Controls.Add(_makePrimaryBtn);

        rootLayout.Controls.Add(buttonPanel, 0, 2);

        // Inventory grid (fills remaining space)
        _storeGrid = new InventoryGridPanel { Dock = DockStyle.Fill };
        _storeGrid.SetIsTechInventory(true);
        _storeGrid.SetInventoryOwnerType("Weapon");
        _storeGrid.SetInventoryGroup("Personal");
        _storeGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);
        rootLayout.Controls.Add(_storeGrid);

        Controls.Add(rootLayout);

        // Set Max Supported label for multitool technology
        _storeGrid.SetMaxSupportedLabel(UiStrings.Format("common.max_supported", "10x6"));

        ResumeLayout(false);
        PerformLayout();
    }

    private ComboBox _toolSelector;
    private TextBox _toolName;
    private ComboBox _toolClass;
    private TextBox _toolSeed;
    private Button _generateSeedBtn;
    private Button _deleteBtn;
    private Button _exportBtn;
    private Button _importBtn;
    private Button _makePrimaryBtn;
    private Label _primaryToolLabel;
    private Label _titleLabel;
    private Label _detailsLabel;
    private Label _statsLabel;
    private Label _selectLabel;
    private Label _nameLabel;
    private Label _typeLabel;
    private Label _classLabel;
    private Label _seedLabel;
    private Label _damageLabel;
    private Label _miningLabel;
    private Label _scanLabel;
    private InvariantNumericTextBox _damageField;
    private InvariantNumericTextBox _miningField;
    private InvariantNumericTextBox _scanField;
    private InventoryGridPanel _storeGrid;
    private ComboBox _toolType;
}
