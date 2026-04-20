#nullable enable
using NMSE.Core;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class StarshipPanel
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer? components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        // StarshipPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        // Main layout: 1 column, 3 rows (title, details/stats+buttons, inventory)
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
            AutoSize = true
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // title
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // details/stats+buttons
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // inventory

        var titlePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        _titleLabel = new Label
        {
            Text = "Starships",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 2)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        _primaryShipLabel = new Label
        {
            Text = "",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Padding = new Padding(15, 5, 0, 0),
            Font = new Font(Font.FontFamily, 9, FontStyle.Italic),
        };
        titlePanel.Controls.Add(_titleLabel);
        titlePanel.Controls.Add(_primaryShipLabel);
        mainLayout.Controls.Add(titlePanel, 0, 0);

        // Details+Stats layout: 2 columns, 3 rows (details/stats, buttons, corvette row)
        var detailsStatsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true
        };
        detailsStatsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        detailsStatsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        detailsStatsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // details/stats
        detailsStatsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // buttons
        detailsStatsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // corvette extras row

        // Left panel for selection and properties
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            AutoSize = true
        };
        for (int i = 0; i < 7; i++)
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;
        _detailsLabel = new Label
        {
            Text = "Details",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 2)
        };
        FontManager.ApplyHeadingFont(_detailsLabel, 10);
        leftPanel.Controls.Add(_detailsLabel, 0, row);
        leftPanel.SetColumnSpan(_detailsLabel, 2);
        row++;

        _shipSelector = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _shipSelector.SelectedIndexChanged += OnShipSelected;
        _selectLabel = AddRow(leftPanel, "Select Ship:", _shipSelector, row++);

        _shipName = new TextBox { Dock = DockStyle.Fill };
        _shipName.Leave += OnShipNameChanged;
        _nameLabel = AddRow(leftPanel, "Name:", _shipName, row++);

        _shipType = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _shipType.SelectedIndexChanged += OnShipTypeChanged;
        _typeLabel = AddRow(leftPanel, "Type:", _shipType, row++);

        _shipClass = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _shipClass.Items.AddRange(StarshipLogic.ShipClasses);
        _classLabel = AddRow(leftPanel, "Class:", _shipClass, row++);

        var seedPanel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        _shipSeed = new TextBox { Dock = DockStyle.Fill };
        _generateSeedBtn = new Button { Text = "Generate", Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 0) };
        _generateSeedBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            _shipSeed.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        };
        seedPanel.Controls.Add(_shipSeed);
        seedPanel.Controls.Add(_generateSeedBtn);
        _seedLabel = AddRow(leftPanel, "Seed:", seedPanel, row++);

        // Right panel for base stats
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true
        };
        for (int i = 0; i < 5; i++)
            rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int statRow = 0;
        _statsLabel = new Label
        {
            Text = "Base Stats",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 2)
        };
        FontManager.ApplyHeadingFont(_statsLabel, 10);
        rightPanel.Controls.Add(_statsLabel, 0, statRow);
        rightPanel.SetColumnSpan(_statsLabel, 2);
        statRow++;

        _damageField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _damageLabel = AddRow(rightPanel, "Damage:", _damageField, statRow++);

        _shieldField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _shieldLabel = AddRow(rightPanel, "Shield:", _shieldField, statRow++);

        _hyperdriveField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _hyperdriveLabel = AddRow(rightPanel, "Hyperdrive:", _hyperdriveField, statRow++);

        _maneuverField = new InvariantNumericTextBox { Dock = DockStyle.Fill };
        _maneuverLabel = AddRow(rightPanel, "Maneuverability:", _maneuverField, statRow++);

        _useOldColours = new CheckBox { Text = "Use Old Color", AutoSize = true };
        rightPanel.Controls.Add(new Label(), 0, statRow);
        rightPanel.Controls.Add(_useOldColours, 1, statRow++);

        detailsStatsLayout.Controls.Add(leftPanel, 0, 0);
        detailsStatsLayout.Controls.Add(rightPanel, 1, 0);

        // Buttons panel (row 1: common buttons)
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        _deleteBtn = new Button { Text = "Delete Ship", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(76, 0) };
        _deleteBtn.Click += OnDeleteShip;
        _exportBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _exportBtn.Click += OnExportShip;
        _importBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _importBtn.Click += OnImportShip;
        _makePrimaryBtn = new Button { Text = "Make Primary", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(88, 0) };
        _makePrimaryBtn.Click += OnMakePrimary;
        _corvetteWarningLabel = new ColorEmojiLabel
        {
            Text = "\u26A0 Saves only store full Technology slots for your last active Corvette.",
            Font = new Font("Segoe UI Emoji", 9, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.Red,
            Padding = new Padding(5, 5, 0, 0),
            Visible = false,
        };
        buttonPanel.Controls.Add(_deleteBtn);
        buttonPanel.Controls.Add(_exportBtn);
        buttonPanel.Controls.Add(_importBtn);
        buttonPanel.Controls.Add(_makePrimaryBtn);
        buttonPanel.Controls.Add(_corvetteWarningLabel);
        detailsStatsLayout.Controls.Add(buttonPanel, 0, 1);
        detailsStatsLayout.SetColumnSpan(buttonPanel, 2);

        // Corvette extras panel (row 2: Snapshot, Import Snapshot, Optimise + indicator)
        _corvetteExtrasPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0),
            Margin = new Padding(0),
            Visible = false,
        };
        _snapshotTechBtn = new Button { Text = "Snapshot Tech", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(100, 0) };
        _snapshotTechBtn.Click += OnSnapshotTech;
        _importSnapshotBtn = new Button { Text = "Import Snapshot", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(100, 0) };
        _importSnapshotBtn.Click += OnImportSnapshot;
        _optimiseBtn = new Button { Text = "Optimise Build", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(100, 0) };
        _optimiseBtn.Click += OnOptimiseCorvette;
        _optimiseIndicator = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 0),
        };
        SetOptimiseIndicator(false);
        _corvetteExtrasPanel.Controls.Add(_snapshotTechBtn);
        _corvetteExtrasPanel.Controls.Add(_importSnapshotBtn);
        _corvetteExtrasPanel.Controls.Add(_optimiseBtn);
        _corvetteExtrasPanel.Controls.Add(_optimiseIndicator);
        detailsStatsLayout.Controls.Add(_corvetteExtrasPanel, 0, 2);
        detailsStatsLayout.SetColumnSpan(_corvetteExtrasPanel, 2);

        mainLayout.Controls.Add(detailsStatsLayout, 0, 1);

        // Inventory tabs (fill remaining space)
        _inventoryGrid = new InventoryGridPanel { Dock = DockStyle.Fill };
        _techGrid = new InventoryGridPanel { Dock = DockStyle.Fill };
        _techGrid.SetIsTechInventory(true);
        _inventoryGrid.SetIsCargoInventory(true);
        _inventoryGrid.SetSortingEnabled(true);
        _techGrid.SetInventoryOwnerType("Ship");
        _inventoryGrid.SetInventoryOwnerType("Ship");
        _inventoryGrid.SetInventoryGroup("ShipCargo");
        _inventoryGrid.SetPinSlotFeatureEnabled(true);
        _techGrid.SetInventoryGroup("Ship");
        _inventoryGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);
        _techGrid.DataModified += (s, e) => DataModified?.Invoke(this, e);
        _inventoryGrid.PinnedSlotsChanged += OnPinnedSlotsChanged;
        _inventoryGrid.AutoStackToStorageRequested += OnAutoStackToStorageRequested;
        _inventoryGrid.AutoStackToFreighterRequested += OnAutoStackToFreighterRequested;
        _inventoryGrid.AutoStackSelectedSlotToStorageRequested += OnAutoStackSelectedSlotToStorageRequested;
        _inventoryGrid.AutoStackSelectedSlotToFreighterRequested += OnAutoStackSelectedSlotToFreighterRequested;
        _inventoryGrid.RefreshToolbarActions();

        _invTabs = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        _cargoTabPage = new TabPage("Cargo");
        _cargoTabPage.Controls.Add(_inventoryGrid);
        _techTabPage = new TabPage("Technology");
        _techTabPage.Controls.Add(_techGrid);
        _invTabs.TabPages.Add(_cargoTabPage);
        _invTabs.TabPages.Add(_techTabPage);

        mainLayout.Controls.Add(_invTabs, 0, 2);

        Controls.Add(mainLayout);

        _inventoryGrid.SetSuperchargeDisabled(true);

        // Set initial Max Supported labels (will be updated on ship selection)
        SetStarshipMaxSupportedLabels("Unknown");

        ResumeLayout(false);
        PerformLayout();
    }

    private ComboBox _shipSelector = null!;
    private TextBox _shipName = null!;
    private ComboBox _shipClass = null!;
    private ComboBox _shipType = null!;
    private TextBox _shipSeed = null!;
    private Button _generateSeedBtn = null!;
    private CheckBox _useOldColours = null!;
    private InvariantNumericTextBox _damageField = null!;
    private InvariantNumericTextBox _shieldField = null!;
    private InvariantNumericTextBox _hyperdriveField = null!;
    private InvariantNumericTextBox _maneuverField = null!;
    private Button _deleteBtn = null!;
    private Button _exportBtn = null!;
    private Button _importBtn = null!;
    private Button _makePrimaryBtn = null!;
    private Label _primaryShipLabel = null!;
    private ColorEmojiLabel _corvetteWarningLabel = null!;
    private FlowLayoutPanel _corvetteExtrasPanel = null!;
    private Button _snapshotTechBtn = null!;
    private Button _importSnapshotBtn = null!;
    private Button _optimiseBtn = null!;
    private Label _optimiseIndicator = null!;
    private DoubleBufferedTabControl _invTabs = null!;
    private InventoryGridPanel _inventoryGrid = null!;
    private InventoryGridPanel _techGrid = null!;
    private Label _titleLabel = null!;
    private Label _detailsLabel = null!;
    private Label _statsLabel = null!;
    private Label _selectLabel = null!;
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private Label _classLabel = null!;
    private Label _seedLabel = null!;
    private Label _damageLabel = null!;
    private Label _shieldLabel = null!;
    private Label _hyperdriveLabel = null!;
    private Label _maneuverLabel = null!;
    private TabPage _cargoTabPage = null!;
    private TabPage _techTabPage = null!;
}
