#nullable enable
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.IO;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class MainStatsPanel
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
        // MainStatsPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        _healthField = new NumericUpDown { Maximum = 999999, Width = 150, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _shieldField = new NumericUpDown { Maximum = 999999, Width = 150, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _energyField = new NumericUpDown { Maximum = 999999, Width = 150, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _unitsField = new NumericUpDown { Maximum = uint.MaxValue, Width = 150, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _nanitesField = new NumericUpDown { Maximum = uint.MaxValue, Width = 150, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _quicksilverField = new NumericUpDown { Maximum = uint.MaxValue, Width = 150, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _saveNameField = new TextBox { Width = 250 };
        _saveSummaryField = new TextBox { Width = 250 };
        _playTimeField = new TextBox { Width = 150, ReadOnly = true };
        _thirdPersonCharCam = new CheckBox { Text = "Third Person Camera", AutoSize = true };
        _lastSaveDateLabel = new Label { AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
        _currentPresetCombo = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        _currentPresetCombo.Items.AddRange(DifficultyPresets);
        _easiestPresetCombo = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        _easiestPresetCombo.Items.AddRange(DifficultyPresets);
        _hardestPresetCombo = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
        _hardestPresetCombo.Items.AddRange(DifficultyPresets);
        _accountNameField = new TextBox { Width = 250, ReadOnly = true };

        _galaxyField = new Label { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
        _galaxyCoreCaptionLabel = new Label { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
        _galaxyDotLabel = new Label
        {
            AutoSize = false,
            Size = new Size(12, 12),
            Text = string.Empty,
            Padding = Padding.Empty,
            Margin = new Padding(0, 6, 0, 0),
            ImageAlign = ContentAlignment.MiddleCenter
        };
        _galaxyPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        _galaxyPanel.Controls.Add(_galaxyField);
        _galaxyPanel.Controls.Add(_galaxyCoreCaptionLabel);
        _galaxyPanel.Controls.Add(_galaxyDotLabel);
        _portalCodeField = new TextBox { Width = 200, ReadOnly = true };
        _portalCodeDecField = new TextBox { Width = 200, ReadOnly = true };
        _signalBoosterField = new TextBox { Width = 200, ReadOnly = true };
        _playerStateField = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        _playerStateField.Items.AddRange(CoordinateHelper.PlayerStates);
        _distanceToCenterField = new TextBox { Width = 150, ReadOnly = true };
        _jumpsToCenterField = new TextBox { Width = 150, ReadOnly = true };
        _freighterInSystemField = new TextBox { Width = 100, ReadOnly = true };
        _nexusInSystemField = new TextBox { Width = 100, ReadOnly = true };
        _planetsInSystemField = new TextBox { Width = 100, ReadOnly = true };
        _portalInterference = new CheckBox { Text = "Portal Interference Active", AutoSize = true };

        // Save Utilities controls
        _slotSourceCombo = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        _slotDestCombo = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        for (int i = 1; i <= 15; i++)
        {
            _slotSourceCombo.Items.Add($"Slot {i}");
            _slotDestCombo.Items.Add($"Slot {i}");
        }
        if (_slotSourceCombo.Items.Count > 0) _slotSourceCombo.SelectedIndex = 0;
        if (_slotDestCombo.Items.Count > 1) _slotDestCombo.SelectedIndex = 1;

        _copySlotBtn = new Button { Text = "Copy Slot", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _copySlotBtn.Click += OnCopySlot;
        _moveSlotBtn = new Button { Text = "Move Slot", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _moveSlotBtn.Click += OnMoveSlot;
        _swapSlotBtn = new Button { Text = "Swap Slots", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _swapSlotBtn.Click += OnSwapSlots;
        _deleteSlotBtn = new Button { Text = "Delete Slot", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _deleteSlotBtn.Click += OnDeleteSlot;

        _transferPlatformCombo = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
        _transferPlatformCombo.Items.AddRange(new object[] { "Steam", "GOG", "Xbox Game Pass", "PS4", "Switch" });
        if (_transferPlatformCombo.Items.Count > 0) _transferPlatformCombo.SelectedIndex = 0;
        _transferBtn = new Button { Text = "Transfer to Platform…", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _transferBtn.Click += OnTransferPlatform;

        _portalGlyphPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };

        _galaxyNud = new NumericUpDown { Width = 150, Minimum = 1, Maximum = 257 };
        _voxelXNud = new NumericUpDown { Width = 150, Minimum = -2048, Maximum = 2047 };
        _voxelYNud = new NumericUpDown { Width = 150, Minimum = -128, Maximum = 127 };
        _voxelZNud = new NumericUpDown { Width = 150, Minimum = -2048, Maximum = 2047 };
        _solarSystemNud = new NumericUpDown { Width = 150, Minimum = 0, Maximum = 600 };
        _planetNud = new NumericUpDown { Width = 150, Minimum = 0, Maximum = 15 };
        _applyCoordinatesBtn = new Button { Text = "Apply Coordinates", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _applyCoordinatesBtn.Click += OnApplyCoordinates;

        _portalHexInput = new TextBox { Width = 150, MaxLength = 12 };
        _convertPortalBtn = new Button { Text = "Convert to Coords", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _convertPortalBtn.Click += OnConvertPortalCode;

        _coordinateRouletteBtn = new Button { Text = "Coordinate Roulette!", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _coordinateRouletteBtn.Click += OnCoordinateRoulette;

        _warpsToNextBattleField = new NumericUpDown { Width = 100, Minimum = 0, Maximum = 999, ReadOnly = true };
        _timeToNextBattleField = new TextBox { Width = 150, ReadOnly = true };
        _triggerBattleBtn = new Button { Text = "Trigger Space Battle", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _triggerBattleBtn.Click += OnTriggerSpaceBattle;

        _guidesFilter = new TextBox { Width = 250 };
        _guidesFilter.TextChanged += OnGuidesFilterChanged;

        InitializeLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private void InitializeLayout()
    {
        _tabs = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        var tabs = _tabs;

        // -- General Tab --
        var generalPage = new TabPage("General");
        var generalPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        // Main 3-column layout: left=Stats+SaveInfo, center=CurrentCoords+SpaceBattle, right=EditCoords
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            Padding = new Padding(8)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39f));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // -- Left column: Player Statistics + Save Info --
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int leftRow = 0;

        _playerStatsHeader = AddSectionHeader(leftLayout, "Player Statistics", leftRow++);
        _healthLabel = AddRow(leftLayout, "Health:", _healthField, leftRow++);
        _shieldLabel = AddRow(leftLayout, "Shield:", _shieldField, leftRow++);
        _energyLabel = AddRow(leftLayout, "Energy:", _energyField, leftRow++);
        _unitsLabel = AddRow(leftLayout, "Units:", _unitsField, leftRow++);
        _nanitesLabel = AddRow(leftLayout, "Nanites:", _nanitesField, leftRow++);
        _quicksilverLabel = AddRow(leftLayout, "Quicksilver:", _quicksilverField, leftRow++);

        _saveInfoHeader = AddSectionHeader(leftLayout, "Save Info", leftRow++);
        _saveNameLabel = AddRow(leftLayout, "Save Name:", _saveNameField, leftRow++);
        _saveSummaryLabel = AddRow(leftLayout, "Save Summary:", _saveSummaryField, leftRow++);
        _playTimeLabel = AddRow(leftLayout, "Total Play Time:", _playTimeField, leftRow++);
        leftLayout.Controls.Add(_thirdPersonCharCam, 1, leftRow++);
        _lastSaveLabel = AddRow(leftLayout, "Last Save Date:", _lastSaveDateLabel, leftRow++);
        _currentPresetLabel = AddRow(leftLayout, "Current Preset:", _currentPresetCombo, leftRow++);
        _easiestPresetLabel = AddRow(leftLayout, "Easiest Used:", _easiestPresetCombo, leftRow++);
        _hardestPresetLabel = AddRow(leftLayout, "Hardest Used:", _hardestPresetCombo, leftRow++);
        _accountNameLabel = AddRow(leftLayout, "Account Name:", _accountNameField, leftRow++);

        leftLayout.RowCount = leftRow;
        for (int i = 0; i < leftRow; i++)
            leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        mainLayout.Controls.Add(leftLayout, 0, 0);

        // -- Center column: Current Coordinates + Space Battle --
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int rightRow = 0;

        _currentCoordsHeader = AddSectionHeader(rightLayout, "Current Coordinates", rightRow++);
        _galaxyLabel = AddRow(rightLayout, "Galaxy:", _galaxyPanel, rightRow++);
        _portalHexLabel = AddRow(rightLayout, "Portal Code (Hex):", _portalCodeField, rightRow++);
        _portalDecLabel = AddRow(rightLayout, "Portal Code (Dec):", _portalCodeDecField, rightRow++);

        // Portal glyph rendering row
        _portalGlyphsLabel = new Label { Text = "Portal Glyphs:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 5, 10, 0) };
        rightLayout.Controls.Add(_portalGlyphsLabel, 0, rightRow);
        rightLayout.Controls.Add(_portalGlyphPanel, 1, rightRow++);

        _signalBoosterLabel = AddRow(rightLayout, "Signal Booster:", _signalBoosterField, rightRow++);
        _playerStateLabel = AddRow(rightLayout, "Player State:", _playerStateField, rightRow++);
        _distanceToCenterLabel = AddRow(rightLayout, "Distance to Center:", _distanceToCenterField, rightRow++);
        _jumpsToCenterLabel = AddRow(rightLayout, "Jumps to Center:", _jumpsToCenterField, rightRow++);
        _freighterInSystemLabel = AddRow(rightLayout, "Freighter in System:", _freighterInSystemField, rightRow++);
        _nexusInSystemLabel = AddRow(rightLayout, "Nexus in System:", _nexusInSystemField, rightRow++);
        _planetsInSystemLabel = AddRow(rightLayout, "Planets in System:", _planetsInSystemField, rightRow++);
        rightLayout.Controls.Add(_portalInterference, 1, rightRow++);

        _spaceBattleHeader = AddSectionHeader(rightLayout, "Space Battle", rightRow++);
        _warpsToNextLabel = AddRow(rightLayout, "Warps to Next:", _warpsToNextBattleField, rightRow++);
        _timeToNextLabel = AddRow(rightLayout, "Time to Next:", _timeToNextBattleField, rightRow++);
        rightLayout.Controls.Add(_triggerBattleBtn, 1, rightRow++);

        rightLayout.RowCount = rightRow;
        for (int i = 0; i < rightRow; i++)
            rightLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        mainLayout.Controls.Add(rightLayout, 1, 0);

        // -- Right column: Edit Coordinates --
        var editLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        editLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int editRow = 0;

        _editCoordsHeader = AddSectionHeader(editLayout, "Edit Coordinates", editRow++);
        _galaxyRangeLabel = AddRow(editLayout, "Galaxy (1–257):", _galaxyNud, editRow++);
        _voxelXLabel = AddRow(editLayout, "Voxel X (-2048–2047):", _voxelXNud, editRow++);
        _voxelYLabel = AddRow(editLayout, "Voxel Y (-128–127):", _voxelYNud, editRow++);
        _voxelZLabel = AddRow(editLayout, "Voxel Z (-2048–2047):", _voxelZNud, editRow++);
        _solarSystemLabel = AddRow(editLayout, "Solar System (0–600):", _solarSystemNud, editRow++);
        _planetLabel = AddRow(editLayout, "Planet (0–15):", _planetNud, editRow++);
        editLayout.Controls.Add(_applyCoordinatesBtn, 1, editRow++);

        // Portal Code to Coordinates converter
        _portalToCoordsHeader = AddSectionHeader(editLayout, "Portal Code → Coordinates", editRow++);

        // Portal glyph buttons row - clicking inserts hex digit into the portal code field
        _glyphButtonLabel = new Label { Text = "Glyphs:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Padding = new Padding(0, 5, 10, 0) };
        _glyphButtonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
            MaximumSize = new Size(8 * 24, 0), // 8 buttons per row (26px width + 1px margin each side = 28px per button)
        };
        var glyphToolTip = new ToolTip();
        for (int g = 0; g < 16; g++)
        {
            char hexChar = "0123456789ABCDEF"[g];
            var btn = new Button
            {
                Width = 22,
                Height = 22,
                Margin = new Padding(1),
                FlatStyle = FlatStyle.Flat,
                Tag = hexChar,
            };
            btn.FlatAppearance.BorderSize = 0;
            var glyphImg = CoordinateHelper.GetGlyphImage(hexChar);
            if (glyphImg != null)
            {
                btn.Image = CreateGlyphButtonImage(glyphImg, 20);
                btn.ImageAlign = ContentAlignment.MiddleCenter;
            }
            else
            {
                btn.Text = hexChar.ToString();
            }
            glyphToolTip.SetToolTip(btn, $"Glyph {g + 1} (Hex {hexChar})");
            btn.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is char c)
                {
                    if (_portalHexInput.Text.Length < 12)
                    {
                        int sel = _portalHexInput.SelectionStart;
                        _portalHexInput.Text = _portalHexInput.Text.Insert(sel, c.ToString());
                        _portalHexInput.SelectionStart = sel + 1;
                        _portalHexInput.Focus();
                    }
                }
            };
            _glyphButtonPanel.Controls.Add(btn);
        }
        editLayout.Controls.Add(_glyphButtonLabel, 0, editRow);
        editLayout.Controls.Add(_glyphButtonPanel, 1, editRow++);

        _editPortalHexLabel = AddRow(editLayout, "Portal Code (Hex):", _portalHexInput, editRow++);
        editLayout.Controls.Add(_convertPortalBtn, 1, editRow++);

        // Small gap before roulette button
        editLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        editRow++;
        editLayout.Controls.Add(_coordinateRouletteBtn, 1, editRow++);

        editLayout.RowCount = editRow;
        for (int i = 0; i < editRow; i++)
            editLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        mainLayout.Controls.Add(editLayout, 2, 0);

        // -- Save Utilities section (spans both columns below) --
        mainLayout.RowCount = 2;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var utilitiesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        utilitiesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        utilitiesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int utilRow = 0;

        _utilitiesHeader = AddSectionHeader(utilitiesLayout, "Advanced Save Utilities", utilRow++);

        // Warning label
        _saveUtilsWarning = new Label
        {
            Text = UiStrings.Get("player.save_utils_warning"),
            ForeColor = Color.Red,
            Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 2, 0, 8)
        };
        utilitiesLayout.Controls.Add(_saveUtilsWarning, 0, utilRow);
        utilitiesLayout.SetColumnSpan(_saveUtilsWarning, 2);
        utilRow++;

        // Slot operations row
        var slotOpsPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0) };
        _sourceLabel = new Label { Text = "Source:", AutoSize = true, Padding = new Padding(0, 5, 2, 0) };
        slotOpsPanel.Controls.Add(_sourceLabel);
        slotOpsPanel.Controls.Add(_slotSourceCombo);
        _destLabel = new Label { Text = "Dest:", AutoSize = true, Padding = new Padding(8, 5, 2, 0) };
        slotOpsPanel.Controls.Add(_destLabel);
        slotOpsPanel.Controls.Add(_slotDestCombo);
        slotOpsPanel.Controls.Add(_copySlotBtn);
        slotOpsPanel.Controls.Add(_moveSlotBtn);
        slotOpsPanel.Controls.Add(_swapSlotBtn);
        slotOpsPanel.Controls.Add(_deleteSlotBtn);
        utilitiesLayout.Controls.Add(slotOpsPanel, 0, utilRow);
        utilitiesLayout.SetColumnSpan(slotOpsPanel, 2);
        utilRow++;

        // Cross-platform transfer row
        var transferPanel = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 6, 0, 0) };
        _destPlatformLabel = new Label { Text = "Dest Platform:", AutoSize = true, Padding = new Padding(0, 5, 2, 0) };
        transferPanel.Controls.Add(_destPlatformLabel);
        transferPanel.Controls.Add(_transferPlatformCombo);
        transferPanel.Controls.Add(_transferBtn);
        utilitiesLayout.Controls.Add(transferPanel, 0, utilRow);
        utilitiesLayout.SetColumnSpan(transferPanel, 2);
        utilRow++;

        utilitiesLayout.RowCount = utilRow;
        for (int i = 0; i < utilRow; i++)
            utilitiesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        mainLayout.Controls.Add(utilitiesLayout, 0, 1);
        mainLayout.SetColumnSpan(utilitiesLayout, 3);

        generalPanel.Controls.Add(mainLayout);
        generalPage.Controls.Add(generalPanel);
        tabs.TabPages.Add(generalPage);

        // -- Guides Tab --
        var guidesPage = new TabPage("Guides");
        var guidesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        guidesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        guidesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        guidesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _guidesTitle = new Label { Text = "Wiki / Guide Topics", AutoSize = true };
        FontManager.ApplyHeadingFont(_guidesTitle, 12);
        guidesLayout.Controls.Add(_guidesTitle, 0, 0);

        var filterPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _guidesFilterLabel = new Label { Text = "Filter:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        filterPanel.Controls.Add(_guidesFilterLabel);
        filterPanel.Controls.Add(_guidesFilter);
        var unlockAllBtn = new Button { Text = UiStrings.Get("player.guide_unlock_all"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        unlockAllBtn.Click += OnUnlockAllGuides;
        var lockAllBtn = new Button { Text = UiStrings.Get("player.guide_lock_all"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        lockAllBtn.Click += OnLockAllGuides;
        filterPanel.Controls.Add(unlockAllBtn);
        filterPanel.Controls.Add(lockAllBtn);
        _guideUnlockAllBtn = unlockAllBtn;
        _guideLockAllBtn = lockAllBtn;
        guidesLayout.Controls.Add(filterPanel, 0, 1);

        // Scrollable container with per-category grids
        _guidesContentPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0),
        };

        _guidesGrids.Clear();
        _guideCategoryLabels.Clear();
        for (int catIdx = 0; catIdx < GuideCategories.Length; catIdx++)
        {
            string category = GuideCategories[catIdx];
            var catLabel = new Label
            {
                Text = UiStrings.GetOrNull($"player.guide_cat_{catIdx}") ?? category,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 2),
            };
            FontManager.ApplyHeadingFont(catLabel, 10);
            flowPanel.Controls.Add(catLabel);
            _guideCategoryLabels.Add(catLabel);

            int topicCount = WikiGuideDatabase.Topics.Count(t => WikiGuideDatabase.GetEnglishCategory(t.Id) == category);
            var grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                ScrollBars = ScrollBars.None,
                Width = 700,
                Tag = category,
            };
            grid.Columns.Add(new DataGridViewImageColumn { Name = "Icon", HeaderText = "", ReadOnly = true, FillWeight = 5, ImageLayout = DataGridViewImageCellLayout.Zoom });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TopicId", HeaderText = "Topic ID", ReadOnly = true, FillWeight = 25 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TopicName", HeaderText = "Name", ReadOnly = true, FillWeight = 25 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Seen", HeaderText = "Seen", FillWeight = 10 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Unlocked", HeaderText = "Unlocked", FillWeight = 10 });
            grid.CellValueChanged += OnGuideCellChanged;
            grid.CurrentCellDirtyStateChanged += (s, e) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            // Auto-size height to fit rows
            grid.RowsAdded += (s, e) => AutoSizeGridHeight(grid);
            grid.RowsRemoved += (s, e) => AutoSizeGridHeight(grid);
            flowPanel.Controls.Add(grid);
            _guidesGrids.Add(grid);
        }

        _guidesContentPanel.Controls.Add(flowPanel);
        guidesLayout.Controls.Add(_guidesContentPanel, 0, 2);

        guidesPage.Controls.Add(guidesLayout);
        tabs.TabPages.Add(guidesPage);

        // -- Titles Tab --
        var titlesPage = new TabPage("Titles");
        var titlesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        titlesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titlesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titlesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _titlesTitle = new Label { Text = "Player Titles", AutoSize = true };
        FontManager.ApplyHeadingFont(_titlesTitle, 12);
        titlesLayout.Controls.Add(_titlesTitle, 0, 0);

        var titlesButtonPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var unlockAllTitlesBtn = new Button { Text = UiStrings.Get("player.titles_unlock_all"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        unlockAllTitlesBtn.Click += OnUnlockAllTitles;
        var lockAllTitlesBtn = new Button { Text = UiStrings.Get("player.titles_lock_all"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        lockAllTitlesBtn.Click += OnLockAllTitles;
        titlesButtonPanel.Controls.Add(unlockAllTitlesBtn);
        titlesButtonPanel.Controls.Add(lockAllTitlesBtn);
        _titlesUnlockAllBtn = unlockAllTitlesBtn;
        _titlesLockAllBtn = lockAllTitlesBtn;
        titlesLayout.Controls.Add(titlesButtonPanel, 0, 1);

        _titlesGrid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            Dock = DockStyle.Fill,
            Width = 700,
        };
        _titlesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TitleId", HeaderText = "Title ID", ReadOnly = true, FillWeight = 20 });
        _titlesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TitleName", HeaderText = "Title", ReadOnly = true, FillWeight = 25 });
        _titlesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", ReadOnly = true, FillWeight = 35 });
        _titlesGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Unlocked", HeaderText = "Unlocked", FillWeight = 10 });
        _titlesGrid.CellValueChanged += OnTitleCellChanged;
        _titlesGrid.CurrentCellDirtyStateChanged += (s, e) => { if (_titlesGrid.IsCurrentCellDirty) _titlesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        titlesLayout.Controls.Add(_titlesGrid, 0, 2);

        titlesPage.Controls.Add(titlesLayout);
        tabs.TabPages.Add(titlesPage);

        Controls.Add(tabs);
    }

    // General tab fields
    private NumericUpDown _healthField = null!;
    private NumericUpDown _shieldField = null!;
    private NumericUpDown _energyField = null!;
    private NumericUpDown _unitsField = null!;
    private NumericUpDown _nanitesField = null!;
    private NumericUpDown _quicksilverField = null!;
    private TextBox _saveNameField = null!;
    private TextBox _saveSummaryField = null!;
    private TextBox _playTimeField = null!;
    private CheckBox _thirdPersonCharCam = null!;
    private Label _lastSaveDateLabel = null!;
    private ComboBox _currentPresetCombo = null!;
    private ComboBox _easiestPresetCombo = null!;
    private ComboBox _hardestPresetCombo = null!;
    private TextBox _accountNameField = null!;

    // Coordinates
    private FlowLayoutPanel _galaxyPanel = null!;
    private Label _galaxyField = null!;
    private Label _galaxyCoreCaptionLabel = null!;
    private Label _galaxyDotLabel = null!;
    private TextBox _portalCodeField = null!;
    private TextBox _portalCodeDecField = null!;
    private TextBox _signalBoosterField = null!;
    private ComboBox _playerStateField = null!;
    private TextBox _distanceToCenterField = null!;
    private TextBox _jumpsToCenterField = null!;
    private TextBox _freighterInSystemField = null!;
    private TextBox _nexusInSystemField = null!;
    private TextBox _planetsInSystemField = null!;
    private CheckBox _portalInterference = null!;

    // Editable coordinate NUDs
    private NumericUpDown _galaxyNud = null!;
    private NumericUpDown _voxelXNud = null!;
    private NumericUpDown _voxelYNud = null!;
    private NumericUpDown _voxelZNud = null!;
    private NumericUpDown _solarSystemNud = null!;
    private NumericUpDown _planetNud = null!;
    private Button _applyCoordinatesBtn = null!;

    // Portal code -> coordinates converter
    private TextBox _portalHexInput = null!;
    private Button _convertPortalBtn = null!;
    private Button _coordinateRouletteBtn = null!;

    // Save Utilities
    private ComboBox _slotSourceCombo = null!;
    private ComboBox _slotDestCombo = null!;
    private Button _copySlotBtn = null!;
    private Button _moveSlotBtn = null!;
    private Button _swapSlotBtn = null!;
    private Button _deleteSlotBtn = null!;
    private ComboBox _transferPlatformCombo = null!;
    private Button _transferBtn = null!;

    // Space battle
    private NumericUpDown _warpsToNextBattleField = null!;
    private TextBox _timeToNextBattleField = null!;
    private Button _triggerBattleBtn = null!;

    // Guides tab
    private List<DataGridView> _guidesGrids = new();
    private List<Label> _guideCategoryLabels = new();
    private TextBox _guidesFilter = null!;
    private Panel? _guidesContentPanel;
    private Button _guideUnlockAllBtn = null!;
    private Button _guideLockAllBtn = null!;

    // Titles tab
    private DataGridView _titlesGrid = null!;
    private Button _titlesUnlockAllBtn = null!;
    private Button _titlesLockAllBtn = null!;

    // Portal glyph panel for coordinates
    private FlowLayoutPanel _portalGlyphPanel = null!;
    private FlowLayoutPanel _glyphButtonPanel = null!;

    // Tab control
    private DoubleBufferedTabControl _tabs = null!;

    // Label fields for UI localisation
    private Label _playerStatsHeader = null!;
    private Label _healthLabel = null!;
    private Label _shieldLabel = null!;
    private Label _energyLabel = null!;
    private Label _unitsLabel = null!;
    private Label _nanitesLabel = null!;
    private Label _quicksilverLabel = null!;
    private Label _saveInfoHeader = null!;
    private Label _saveNameLabel = null!;
    private Label _saveSummaryLabel = null!;
    private Label _playTimeLabel = null!;
    private Label _lastSaveLabel = null!;
    private Label _currentPresetLabel = null!;
    private Label _easiestPresetLabel = null!;
    private Label _hardestPresetLabel = null!;
    private Label _accountNameLabel = null!;
    private Label _currentCoordsHeader = null!;
    private Label _galaxyLabel = null!;
    private Label _portalHexLabel = null!;
    private Label _portalDecLabel = null!;
    private Label _portalGlyphsLabel = null!;
    private Label _signalBoosterLabel = null!;
    private Label _playerStateLabel = null!;
    private Label _distanceToCenterLabel = null!;
    private Label _jumpsToCenterLabel = null!;
    private Label _freighterInSystemLabel = null!;
    private Label _nexusInSystemLabel = null!;
    private Label _planetsInSystemLabel = null!;
    private Label _spaceBattleHeader = null!;
    private Label _warpsToNextLabel = null!;
    private Label _timeToNextLabel = null!;
    private Label _editCoordsHeader = null!;
    private Label _galaxyRangeLabel = null!;
    private Label _voxelXLabel = null!;
    private Label _voxelYLabel = null!;
    private Label _voxelZLabel = null!;
    private Label _solarSystemLabel = null!;
    private Label _planetLabel = null!;
    private Label _portalToCoordsHeader = null!;
    private Label _glyphButtonLabel = null!;
    private Label _editPortalHexLabel = null!;
    private Label _utilitiesHeader = null!;
    private Label _sourceLabel = null!;
    private Label _destLabel = null!;
    private Label _destPlatformLabel = null!;
    private Label _guidesTitle = null!;
    private Label _guidesFilterLabel = null!;
    private Label _titlesTitle = null!;
    private Label _saveUtilsWarning = null!;
}
