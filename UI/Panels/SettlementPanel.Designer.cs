#nullable enable
using NMSE.Core;
using NMSE.Data;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

partial class SettlementPanel
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
        // SettlementPanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private static DateTimePicker CreateTimestampPicker()
    {
        return new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowCheckBox = true,
            Checked = false,
            MinDate = new DateTime(1970, 1, 1),
            MaxDate = new DateTime(2099, 12, 31),
        };
    }

    private void SetupLayout()
    {
        SuspendLayout();

        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(0)
        };

        // Main layout: Top controls | Middle tabs | Bottom info
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Top controls
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // TabControl fills remaining
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // Bottom info

        // Top section: Title, selector, warning
        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(0),
            Margin = new Padding(0, 0, 0, 5)
        };

        int topRow = 0;

        // Title
        _titleLabel = new Label
        {
            Text = "Settlements",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.Controls.Add(_titleLabel, 0, topRow++);

        // Top: Settlement selector + Delete button
        var selectorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 5)
        };
        _settlementSelector = new ComboBox { Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
        _settlementSelector.SelectedIndexChanged += OnSettlementSelected;
        _deleteSettlementBtn = new Button { Text = "Delete Settlement", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _deleteSettlementBtn.Click += OnDeleteSettlement;
        _exportSettlementBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportSettlementBtn.Click += OnExportSettlement;
        _importSettlementBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importSettlementBtn.Click += OnImportSettlement;
        selectorPanel.Controls.Add(_settlementSelector);
        selectorPanel.Controls.Add(_deleteSettlementBtn);
        selectorPanel.Controls.Add(_exportSettlementBtn);
        selectorPanel.Controls.Add(_importSettlementBtn);

        // Warning
        _settleWarnLabel = new Label
        {
            Text = UiStrings.Get("⚠ Deleting a settlement doesn't remove the teleporter entry. You can remove it from Catalogue -> Teleport Destinations."),
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            Font = new System.Drawing.Font("Segoe UI Emoji", 9F, System.Drawing.FontStyle.Bold),
            Padding = new Padding(8, 4, 0, 2),
            Margin = new Padding(12, 4, 0, 0),
        };
        // Add selector row, then place the warning label inline in the same FlowLayoutPanel so it appears next to the buttons
        topLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topLayout.Controls.Add(selectorPanel, 0, topRow++);
        selectorPanel.Controls.Add(_settleWarnLabel);

        topLayout.RowCount = topRow;
        mainLayout.Controls.Add(topLayout, 0, 0);

        // ===== TabControl with three tabs =====
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new System.Drawing.Point(12, 4),
        };

        // --- Tab 1: Stats & Perks ---
        var statsPerksTab = new TabPage("Stats && Perks") { AutoScroll = true };

        // 2-column form layout: narrow left (Name/Seed/Stats/Decision) + wide right (Perks 1-18)
        var formPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 10)
        };
        formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
        formPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // === Left Column ===
        var leftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 12, 0)
        };
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int leftRow = 0;

        // 1. Name
        _settlementName = new TextBox { Dock = DockStyle.Fill };
        _settlementName.Leave += OnSettlementNameChanged;
        _nameLabel = AddRow(leftPanel, "Name:", _settlementName, leftRow++);

        // 2. Seed (with Generate button)
        var seedPanel = new Panel { Dock = DockStyle.Fill, Height = 23 };
        _seedField = new TextBox { Dock = DockStyle.Fill };
        _generateSeedBtn = new Button { Text = "Generate", Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 23) };
        _generateSeedBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            _seedField.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
        };
        seedPanel.Controls.Add(_seedField);
        seedPanel.Controls.Add(_generateSeedBtn);
        _seedLabel = AddRow(leftPanel, "Seed:", seedPanel, leftRow++);

        // 3. NPC Race
        _raceField = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DrawMode = DrawMode.OwnerDrawFixed
        };
        _raceField.DrawItem += OnRaceComboDrawItem;
        PopulateRaceCombo();
        _raceLabel = AddRow(leftPanel, "NPC Race:", _raceField, leftRow++);

        // Stat & timestamp fields - initialise arrays
        _statFields = new InvariantNumericTextBox[StatCount];
        _statRowLabels = new Label[StatCount];

        // 4. Max Population (Stats[0])
        _statFields[0] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[0].NumericValueChanged += (s, e) => ApplyStatColor(0);
        _statRowLabels[0] = AddRow(leftPanel, StatLabels[0] + ":", _statFields[0], leftRow++);

        // 5. Population
        _populationField = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = SettlementLogic.PopulationMax,
            Minimum = 0,
        };
        _populationField.NumericValueChanged += (s, e) => ApplyPopulationColor();
        _populationLabel = AddRow(leftPanel, "Population:", _populationField, leftRow++);

        // 6. Last Population Change Time
        _lastPopulationTimeField = CreateTimestampPicker();
        _lastPopulationTimeLabel = AddRow(leftPanel, "Last Population Change Time:", _lastPopulationTimeField, leftRow++);

        // 7. Happiness (Stats[1])
        _statFields[1] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[1].NumericValueChanged += (s, e) => ApplyStatColor(1);
        _statRowLabels[1] = AddRow(leftPanel, StatLabels[1] + ":", _statFields[1], leftRow++);

        // 8. Production (Stats[2])
        _statFields[2] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[2].NumericValueChanged += (s, e) => ApplyStatColor(2);
        _statRowLabels[2] = AddRow(leftPanel, StatLabels[2] + ":", _statFields[2], leftRow++);

        // 9. Upkeep (Stats[3])
        _statFields[3] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[3].NumericValueChanged += (s, e) => ApplyStatColor(3);
        _statRowLabels[3] = AddRow(leftPanel, StatLabels[3] + ":", _statFields[3], leftRow++);

        // 10. Last Upkeep Debt Check Time
        _lastUpkeepTimeField = CreateTimestampPicker();
        _lastUpkeepTimeLabel = AddRow(leftPanel, "Last Upkeep Debt Check Time:", _lastUpkeepTimeField, leftRow++);

        // 11. Sentinels (Stats[4])
        _statFields[4] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[4].NumericValueChanged += (s, e) => ApplyStatColor(4);
        _statRowLabels[4] = AddRow(leftPanel, StatLabels[4] + ":", _statFields[4], leftRow++);

        // 12. Debt (Stats[5])
        _statFields[5] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[5].NumericValueChanged += (s, e) => ApplyStatColor(5);
        _statRowLabels[5] = AddRow(leftPanel, StatLabels[5] + ":", _statFields[5], leftRow++);

        // 13. Last Debt Change Time
        _lastDebtTimeField = CreateTimestampPicker();
        _lastDebtTimeLabel = AddRow(leftPanel, "Last Debt Change Time:", _lastDebtTimeField, leftRow++);

        // 14. Alert (Stats[6])
        _statFields[6] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[6].NumericValueChanged += (s, e) => ApplyStatColor(6);
        _statRowLabels[6] = AddRow(leftPanel, StatLabels[6] + ":", _statFields[6], leftRow++);

        // 15. Last Alert Change Time
        _lastAlertTimeField = CreateTimestampPicker();
        _lastAlertTimeLabel = AddRow(leftPanel, "Last Alert Change Time:", _lastAlertTimeField, leftRow++);

        // 16. Bug Attack (Stats[7])
        _statFields[7] = new InvariantNumericTextBox
        {
            Dock = DockStyle.Fill,
            Maximum = int.MaxValue,
            Minimum = int.MinValue,
        };
        _statFields[7].NumericValueChanged += (s, e) => ApplyStatColor(7);
        _statRowLabels[7] = AddRow(leftPanel, StatLabels[7] + ":", _statFields[7], leftRow++);

        // 17. Last Bug Attack Change Time
        _lastBugAttackTimeField = CreateTimestampPicker();
        _lastBugAttackTimeLabel = AddRow(leftPanel, "Last Bug Attack Change Time:", _lastBugAttackTimeField, leftRow++);

        // 18. Decision Type
        _decisionTypeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _decisionTypeField.Items.AddRange(SettlementLogic.DecisionTypes);
        _decisionTypeLabel = AddRow(leftPanel, "Decision Type:", _decisionTypeField, leftRow++);

        // 19. Last Decision (now with ShowCheckBox zero-guard)
        _lastDecisionTimeField = CreateTimestampPicker();
        _lastDecisionLabel = AddRow(leftPanel, "Last Decision:", _lastDecisionTimeField, leftRow++);

        // 20. Mission Seed (with Generate button)
        var missionSeedPanel = new Panel { Dock = DockStyle.Fill, Height = 23 };
        _missionSeedField = new TextBox { Dock = DockStyle.Fill };
        _generateMissionSeedBtn = new Button { Text = "Generate", Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(70, 23) };
        _generateMissionSeedBtn.Click += (s, e) =>
        {
            _missionSeedField.Text = _rng.Next(0, int.MaxValue)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        };
        missionSeedPanel.Controls.Add(_missionSeedField);
        missionSeedPanel.Controls.Add(_generateMissionSeedBtn);
        _missionSeedLabel = AddRow(leftPanel, "Mission Seed:", missionSeedPanel, leftRow++);

        // 21. Mini Mission Start Time
        _miniMissionStartTimeField = CreateTimestampPicker();
        _miniMissionStartTimeLabel = AddRow(leftPanel, "Mini Mission Start Time:", _miniMissionStartTimeField, leftRow++);

        formPanel.Controls.Add(leftPanel, 0, 0);

        // === Right Column: Perks 1-18 ===
        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true
        };
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        int rightRow = 0;

        // Perk slots (all 18)
        _perkCombos = new ComboBox[PerkSlotCount];
        _perkSeedFields = new TextBox[PerkSlotCount];
        _perkSeedPanels = new Panel[PerkSlotCount];
        _perkSeedGenerateButtons = new Button[PerkSlotCount];
        _perkRemoveButtons = new Button[PerkSlotCount];
        _perkLabels = new Label[PerkSlotCount];

        for (int i = 0; i < PerkSlotCount; i++)
        {
            var perkRowPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                AutoSize = true,
                Margin = new Padding(0)
            };
            perkRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            perkRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            perkRowPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            PopulatePerkCombo(combo);
            int slot = i;
            combo.SelectedIndexChanged += (s, e) => OnPerkChanged(slot);
            combo.DrawItem += OnPerkComboDrawItem;
            _perkCombos[i] = combo;

            // Width accommodates seed TextBox (~130px) + Generate Button (~60px)
            var perkSeedContainer = new Panel { Width = 190, Height = 23, Visible = false };
            var perkSeedField = new TextBox { Dock = DockStyle.Fill };
            var perkGenBtn = new Button { Text = "Generate", Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(60, 23) };
            int genSlot = i;
            perkGenBtn.Click += (s, e) => GeneratePerkSeed(genSlot);
            perkSeedContainer.Controls.Add(perkSeedField);
            perkSeedContainer.Controls.Add(perkGenBtn);
            _perkSeedFields[i] = perkSeedField;
            _perkSeedPanels[i] = perkSeedContainer;
            _perkSeedGenerateButtons[i] = perkGenBtn;

            var removeBtn = new Button { Text = "×", Width = 25, Height = 23 };
            removeBtn.AccessibleName = $"Remove Perk {i + 1}";
            int removeSlot = i;
            removeBtn.Click += (s, e) => { _perkCombos[removeSlot].SelectedIndex = 0; };
            _perkRemoveButtons[i] = removeBtn;

            perkRowPanel.Controls.Add(combo, 0, 0);
            perkRowPanel.Controls.Add(perkSeedContainer, 1, 0);
            perkRowPanel.Controls.Add(removeBtn, 2, 0);

            _perkLabels[i] = AddRow(rightPanel, $"Perk {i + 1}:", perkRowPanel, rightRow++);
        }

        formPanel.Controls.Add(rightPanel, 1, 0);

        statsPerksTab.Controls.Add(formPanel);
        _tabControl.TabPages.Add(statsPerksTab);

        // --- Tab 2: Production ---
        var productionTab = new TabPage("Production") { AutoScroll = true };

        var productionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(5)
        };

        _productionHeaderLabel = new Label
        {
            Text = "Production",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4)
        };
        FontManager.ApplyHeadingFont(_productionHeaderLabel, 10);
        productionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        productionLayout.Controls.Add(_productionHeaderLabel, 0, 0);

        _productionGrid = new DataGridView
        {
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            Dock = DockStyle.Top,
            Height = 150,
        };
        _productionGrid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "Icon",
            HeaderText = "🏭",
            Width = 30,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        });
        _productionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ItemName",
            HeaderText = "Name",
            ReadOnly = true,
            FillWeight = 30,
        });
        _productionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ElementId",
            HeaderText = "Element ID",
            ReadOnly = false,
            FillWeight = 25,
        });
        _productionGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "ChangeElement",
            HeaderText = "Edit",
            Text = "...",
            UseColumnTextForButtonValue = true,
            FillWeight = 8,
        });
        _productionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Amount",
            HeaderText = "Amount",
            ReadOnly = false,
            FillWeight = 20,
        });
        _productionGrid.CellContentClick += OnProductionGridCellClick;
        productionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        productionLayout.Controls.Add(_productionGrid, 0, 1);
        productionLayout.RowCount = 2;

        productionTab.Controls.Add(productionLayout);
        _tabControl.TabPages.Add(productionTab);

        // --- Tab 3: Building States ---
        var buildingStatesTab = new TabPage("Building States") { AutoScroll = true };

        var buildingLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(5)
        };
        int bRow = 0;

        _buildingStatesHeaderLabel = new Label
        {
            Text = "Building States",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        FontManager.ApplyHeadingFont(_buildingStatesHeaderLabel, 10);

        _buildingStatesExperimentalWarningLabel = new Label
        {
            Text = UiStrings.Get("Warning: This panel functionality is currently experimental!"),
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            Padding = new Padding(0, 0, 0, 5),
            Margin = new Padding(12, 4, 0, 0)
        };

        var buildingHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 5)
        };
        buildingHeaderPanel.Controls.Add(_buildingStatesHeaderLabel);
        buildingHeaderPanel.Controls.Add(_buildingStatesExperimentalWarningLabel);

        buildingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        buildingLayout.Controls.Add(buildingHeaderPanel, 0, bRow++);

        // 6 columns × 8 rows grid of building state entries (column-first ordering)
        const int bCols = 6;
        const int bRows = 8;
        _buildingStateFields = new ComboBox[SettlementLogic.BuildingStateSlotCount];
        _buildingStateNuds = new InvariantNumericTextBox[SettlementLogic.BuildingStateSlotCount];
        _buildingStateLabels = new Label[SettlementLogic.BuildingStateSlotCount];
        _buildingStateInfoLabels = new Label[SettlementLogic.BuildingStateSlotCount];

        var bsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = bCols,
            RowCount = bRows * 2,
            AutoSize = true,
            Padding = new Padding(0)
        };
        for (int c = 0; c < bCols; c++)
            bsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / bCols));

        for (int r = 0; r < bRows; r++)
        {
            bsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bsGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        // Column-first ordering (fits better).
        for (int c = 0; c < bCols; c++)
        {
            for (int r = 0; r < bRows; r++)
            {
                int slotIdx = c * bRows + r;
                if (slotIdx >= SettlementLogic.BuildingStateSlotCount) break;

                var cellPanel = new TableLayoutPanel
                {
                    ColumnCount = 1,
                    RowCount = 3,
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Margin = new Padding(2),
                };
                cellPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                cellPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                cellPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                cellPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var slotLabel = new Label
                {
                    Text = $"{slotIdx + 1:D2}:",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Padding = new Padding(0, 3, 0, 0)
                };
                _buildingStateLabels[slotIdx] = slotLabel;

                var comboField = new ComboBox
                {
                    Dock = DockStyle.Top,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DropDownWidth = 360,
                };
                PopulateBuildingStateCombo(comboField);
                int capturedSlot = slotIdx;
                comboField.SelectedIndexChanged += (s, e) => OnBuildingStateComboChanged(capturedSlot);
                _buildingStateFields[slotIdx] = comboField;

                var nudField = new InvariantNumericTextBox
                {
                    Dock = DockStyle.Top,
                    Minimum = int.MinValue,
                    Maximum = int.MaxValue,
                    TextAlign = HorizontalAlignment.Right,
                };
                nudField.NumericValueChanged += (s, e) => OnBuildingStateNudChanged(capturedSlot);
                _buildingStateNuds[slotIdx] = nudField;

                cellPanel.Controls.Add(slotLabel, 0, 0);
                cellPanel.Controls.Add(comboField, 0, 1);
                cellPanel.Controls.Add(nudField, 0, 2);
                bsGrid.Controls.Add(cellPanel, c, r * 2);

                var infoLabel = new Label
                {
                    Text = "",
                    AutoSize = true,
                    ForeColor = Color.Gray,
                    Font = new System.Drawing.Font(SystemFonts.DefaultFont.FontFamily, 7f),
                    Padding = new Padding(2, 0, 0, 2)
                };
                _buildingStateInfoLabels[slotIdx] = infoLabel;
                bsGrid.Controls.Add(infoLabel, c, r * 2 + 1);
            }
        }

        buildingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        buildingLayout.Controls.Add(bsGrid, 0, bRow++);

        buildingLayout.RowCount = bRow;
        buildingStatesTab.Controls.Add(buildingLayout);
        _tabControl.TabPages.Add(buildingStatesTab);

        // --- Tab 4: Building Editor ---
        var buildingEditorTab = new TabPage("Building Editor") { AutoScroll = true };

        var editorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(5)
        };
        int eRow = 0;

        _editorHeaderLabel = new Label
        {
            Text = "Building Editor",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        FontManager.ApplyHeadingFont(_editorHeaderLabel, 12);

        _editorExperimentalWarningLabel = new Label
        {
            Text = UiStrings.Get("settlement.experimental_warning"),
            AutoSize = true,
            ForeColor = Color.DarkOrange,
            Padding = new Padding(0, 0, 0, 5),
            Margin = new Padding(12, 4, 0, 0)
        };

        var editorHeaderPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 5)
        };
        editorHeaderPanel.Controls.Add(_editorHeaderLabel);
        editorHeaderPanel.Controls.Add(_editorExperimentalWarningLabel);

        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(editorHeaderPanel, 0, eRow++);

        // Slot selector + Raw value + Apply button row
        var editorTopPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 5)
        };
        _editorSlotLabel = new Label { Text = "Slot:", AutoSize = true, Padding = new Padding(0, 5, 4, 0) };
        editorTopPanel.Controls.Add(_editorSlotLabel);
        _editorSlotSelector = new InvariantNumericTextBox { Minimum = 1, Maximum = SettlementLogic.BuildingStateSlotCount, NumericValue = 1, Width = 60 };
        editorTopPanel.Controls.Add(_editorSlotSelector);

        _editorRawValueLabel = new Label { Text = "Raw Value:", AutoSize = true, Padding = new Padding(12, 5, 4, 0) };
        editorTopPanel.Controls.Add(_editorRawValueLabel);
        _editorRawValueField = new InvariantNumericTextBox { Width = 120, Minimum = int.MinValue, Maximum = int.MaxValue, TextAlign = HorizontalAlignment.Right };
        _editorRawValueField.NumericValueChanged += OnEditorRawValueChanged;
        editorTopPanel.Controls.Add(_editorRawValueField);

        _editorApplyBtn = new Button { Text = "Apply", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _editorApplyBtn.Click += OnEditorApply;
        editorTopPanel.Controls.Add(_editorApplyBtn);

        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(editorTopPanel, 0, eRow++);

        // Class + State display row
        var classStatePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 5)
        };
        _editorClassDisplayLabel = new Label { Text = "Class:", AutoSize = true, Padding = new Padding(0, 0, 4, 0) };
        _editorClassValueLabel = new Label { Text = "-", AutoSize = true, Font = new System.Drawing.Font(SystemFonts.DefaultFont, FontStyle.Bold), Padding = new Padding(0, 0, 16, 0) };
        _editorStateDisplayLabel = new Label { Text = "State:", AutoSize = true, Padding = new Padding(0, 0, 4, 0) };
        _editorStateValueLabel = new Label { Text = "-", AutoSize = true, Font = new System.Drawing.Font(SystemFonts.DefaultFont, FontStyle.Bold) };
        classStatePanel.Controls.AddRange(new Control[] { _editorClassDisplayLabel, _editorClassValueLabel, _editorStateDisplayLabel, _editorStateValueLabel });
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(classStatePanel, 0, eRow++);

        // Init Construction Phases (bits 0-6)
        _editorInitPhasesLabel = new Label { Text = "Construction Phases (bit 0-6):", AutoSize = true, Padding = new Padding(0, 4, 0, 2) };
        FontManager.ApplyHeadingFont(_editorInitPhasesLabel, 10);
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(_editorInitPhasesLabel, 0, eRow++);

        var initPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(16, 0, 0, 5)
        };
        _editorInitCheckboxes = new CheckBox[SettlementLogic.SettlementBuildingState.InitPhaseCount];
        for (int i = 0; i < SettlementLogic.SettlementBuildingState.InitPhaseCount; i++)
        {
            _editorInitCheckboxes[i] = new CheckBox { Text = $"Phase {i + 1}", AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            _editorInitCheckboxes[i].CheckedChanged += OnEditorCheckboxChanged;
            initPanel.Controls.Add(_editorInitCheckboxes[i]);
        }
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(initPanel, 0, eRow++);

        // Upgrade Sub-phases (bits 10-19)
        _editorUpgradePhasesLabel = new Label { Text = "Upgrade Phases (bit 10-19):", AutoSize = true, Padding = new Padding(0, 4, 0, 2) };
        FontManager.ApplyHeadingFont(_editorUpgradePhasesLabel, 10);
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(_editorUpgradePhasesLabel, 0, eRow++);

        var upgradePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(16, 0, 0, 5)
        };
        _editorUpgradeCheckboxes = new CheckBox[SettlementLogic.SettlementBuildingState.UpgradePhaseCount];
        for (int i = 0; i < SettlementLogic.SettlementBuildingState.UpgradePhaseCount; i++)
        {
            _editorUpgradeCheckboxes[i] = new CheckBox { Text = $"Sub {i + 1}", AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            _editorUpgradeCheckboxes[i].CheckedChanged += OnEditorCheckboxChanged;
            upgradePanel.Controls.Add(_editorUpgradeCheckboxes[i]);
        }
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(upgradePanel, 0, eRow++);

        // Tier Progression (bits 20-25)
        _editorTierLabel = new Label { Text = "Tier Progression (bit 20-25):", AutoSize = true, Padding = new Padding(0, 4, 0, 2) };
        FontManager.ApplyHeadingFont(_editorTierLabel, 10);
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(_editorTierLabel, 0, eRow++);

        // Panel that holds three tier groups side-by-side. Each group stacks its label above its two checkboxes.
        var tierPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(16, 0, 0, 5)
        };
        _editorTierCheckboxes = new CheckBox[SettlementLogic.SettlementBuildingState.TierBitCount];
        _editorTierInlineLabels = new Label[3];
        string[] tierGroupLabels = { "B Tier:", "A Tier:", "S Tier:" };
        string[] tierCheckLabels = { "Awaiting Upgrade", "Complete" };

        for (int g = 0; g < 3; g++)
        {
            // Create a vertical group panel: label on top, checkboxes below
            var groupPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Margin = new Padding(g > 0 ? 12 : 0, 0, 8, 0)
            };

            _editorTierInlineLabels[g] = new Label { Text = tierGroupLabels[g], AutoSize = true, Padding = new Padding(0, 5, 4, 0) };
            groupPanel.Controls.Add(_editorTierInlineLabels[g]);

            for (int j = 0; j < 2; j++)
            {
                int idx = g * 2 + j;
                _editorTierCheckboxes[idx] = new CheckBox { Text = tierCheckLabels[j], AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
                _editorTierCheckboxes[idx].CheckedChanged += OnEditorCheckboxChanged;
                groupPanel.Controls.Add(_editorTierCheckboxes[idx]);
            }

            tierPanel.Controls.Add(groupPanel);
        }

        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(tierPanel, 0, eRow++);

        // Unveil Flags (bits 26-29)
        _editorFlagsLabel = new Label { Text = "Unveil Flags (bit 26-29):", AutoSize = true, Padding = new Padding(0, 4, 0, 2) };
        FontManager.ApplyHeadingFont(_editorFlagsLabel, 10);
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(_editorFlagsLabel, 0, eRow++);

        var flagsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(16, 0, 0, 5)
        };
        _editorFlagCheckboxes = new CheckBox[SettlementLogic.SettlementBuildingState.FlagBitCount];
        string[] flagLabels = { "Class Active", "B Class Unveiled", "A Class Unveiled", "S Class Unveiled" };
        for (int i = 0; i < 4; i++)
        {
            _editorFlagCheckboxes[i] = new CheckBox { Text = flagLabels[i], AutoSize = true, Margin = new Padding(0, 0, 8, 0) };
            _editorFlagCheckboxes[i].CheckedChanged += OnEditorCheckboxChanged;
            flagsPanel.Controls.Add(_editorFlagCheckboxes[i]);
        }
        editorLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editorLayout.Controls.Add(flagsPanel, 0, eRow++);

        // Wire up slot selector event after all editor controls are created
        _editorSlotSelector.NumericValueChanged += OnEditorSlotChanged;

        editorLayout.RowCount = eRow;
        buildingEditorTab.Controls.Add(editorLayout);
        _tabControl.TabPages.Add(buildingEditorTab);

        mainLayout.Controls.Add(_tabControl, 0, 1);

        // Bottom section: Info label
        var bottomLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
            Padding = new Padding(0),
            Margin = new Padding(0, 5, 0, 0)
        };

        _infoLabel = new Label
        {
            Text = "Load a save file to view settlement data.",
            AutoSize = true,
            Padding = new Padding(0)
        };
        bottomLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bottomLayout.Controls.Add(_infoLabel, 0, 0);
        bottomLayout.RowCount = 1;

        mainLayout.Controls.Add(bottomLayout, 0, 2);

        scrollPanel.Controls.Add(mainLayout);
        Controls.Add(scrollPanel);

        ResumeLayout(false);
        PerformLayout();
    }

    // Existing fields
    private ComboBox _settlementSelector = null!;
    private Button _deleteSettlementBtn = null!;
    private Button _exportSettlementBtn = null!;
    private Button _importSettlementBtn = null!;
    private TextBox _settlementName = null!;
    private TextBox _seedField = null!;
    private Button _generateSeedBtn = null!;
    private InvariantNumericTextBox[] _statFields = null!;
    private InvariantNumericTextBox _populationField = null!;
    private ComboBox _decisionTypeField = null!;
    private DateTimePicker _lastDecisionTimeField = null!;
    private ComboBox[] _perkCombos = null!;
    private TextBox[] _perkSeedFields = null!;
    private Panel[] _perkSeedPanels = null!;
    private Button[] _perkSeedGenerateButtons = null!;
    private Button[] _perkRemoveButtons = null!;
    private DataGridView _productionGrid = null!;
    private Label _infoLabel = null!;
    private Label _titleLabel = null!;
    private Label _nameLabel = null!;
    private Label _seedLabel = null!;
    private Label[] _perkLabels = null!;
    private Label _decisionTypeLabel = null!;
    private Label _lastDecisionLabel = null!;
    private Label[] _statRowLabels = null!;
    private Label _populationLabel = null!;
    private Label _productionHeaderLabel = null!;
    private Label _settleWarnLabel = null!;

    // NPC Race
    private ComboBox _raceField = null!;
    private Label _raceLabel = null!;

    // Timestamp fields
    private DateTimePicker _lastBugAttackTimeField = null!;
    private DateTimePicker _lastAlertTimeField = null!;
    private DateTimePicker _lastDebtTimeField = null!;
    private DateTimePicker _lastUpkeepTimeField = null!;
    private DateTimePicker _lastPopulationTimeField = null!;
    private DateTimePicker _miniMissionStartTimeField = null!;

    // Labels for timestamps
    private Label _lastBugAttackTimeLabel = null!;
    private Label _lastAlertTimeLabel = null!;
    private Label _lastDebtTimeLabel = null!;
    private Label _lastUpkeepTimeLabel = null!;
    private Label _lastPopulationTimeLabel = null!;
    private Label _miniMissionStartTimeLabel = null!;

    // Mission seed
    private TextBox _missionSeedField = null!;
    private Button _generateMissionSeedBtn = null!;
    private Label _missionSeedLabel = null!;

    // Tab control
    private TabControl _tabControl = null!;

    // Building States tab
    private ComboBox[] _buildingStateFields = null!;
    private InvariantNumericTextBox[] _buildingStateNuds = null!;
    private Label[] _buildingStateLabels = null!;
    private Label[] _buildingStateInfoLabels = null!;
    private Label _buildingStatesHeaderLabel = null!;
    private Label _buildingStatesExperimentalWarningLabel = null!;

    // Building Editor tab
    private Label _editorHeaderLabel = null!;
    private Label _editorExperimentalWarningLabel = null!;
    private Label _editorSlotLabel = null!;
    private InvariantNumericTextBox _editorSlotSelector = null!;
    private Label _editorRawValueLabel = null!;
    private InvariantNumericTextBox _editorRawValueField = null!;
    private Button _editorApplyBtn = null!;
    private Label _editorClassDisplayLabel = null!;
    private Label _editorClassValueLabel = null!;
    private Label _editorStateDisplayLabel = null!;
    private Label _editorStateValueLabel = null!;
    private Label _editorInitPhasesLabel = null!;
    private CheckBox[] _editorInitCheckboxes = null!;
    private Label _editorUpgradePhasesLabel = null!;
    private CheckBox[] _editorUpgradeCheckboxes = null!;
    private Label _editorTierLabel = null!;
    private Label[] _editorTierInlineLabels = null!;
    private CheckBox[] _editorTierCheckboxes = null!;
    private Label _editorFlagsLabel = null!;
    private CheckBox[] _editorFlagCheckboxes = null!;
}
