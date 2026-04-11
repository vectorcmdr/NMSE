#nullable enable
using NMSE.Core;
using NMSE.Data;
using NMSE.UI.Util;
using System.Diagnostics;

namespace NMSE.UI.Panels;

partial class CompanionPanel
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
        // CompanionPanel
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        // -- Main two-column layout: list (left) | detail (right) --
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(10)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // -- Left column: title + listbox + buttons --
        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _titleLabel = new Label
        {
            Text = "Companions",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        leftLayout.Controls.Add(_titleLabel, 0, 0);

        _companionList = new ListBox { Dock = DockStyle.Fill };
        _companionList.SelectedIndexChanged += OnCompanionSelected;
        leftLayout.Controls.Add(_companionList, 0, 1);

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight
        };
        _countLabel = new Label { Text = "Total: 0 slots", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 0, 0) };
        _deleteBtn = new Button { Text = "Delete", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _deleteBtn.Click += OnDelete;
        _exportCompanionBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _exportCompanionBtn.Click += OnExport;
        _importCompanionBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _importCompanionBtn.Click += OnImport;
        btnPanel.Controls.Add(_deleteBtn);
        btnPanel.Controls.Add(_exportCompanionBtn);
        btnPanel.Controls.Add(_importCompanionBtn);
        btnPanel.Controls.Add(_countLabel);
        leftLayout.Controls.Add(btnPanel, 0, 2);

        mainLayout.Controls.Add(leftLayout, 0, 0);

        // -- Right column: detail panel with TabControl --
        _detailPanel = new Panel { Dock = DockStyle.Fill, Visible = false };

        _tabControl = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        _statsPage = new TabPage("Stats");
        _battlePage = new TabPage("Battle");
        _tabControl.TabPages.Add(_statsPage);
        _tabControl.TabPages.Add(_battlePage);
        _detailPanel.Controls.Add(_tabControl);

        // ======== Stats Tab Content ========
        var statsScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0) };
        _statsPage.Padding = new Padding(2, 0, 2, 2);
        _statsPage.Controls.Add(statsScroll);

        _formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0),
        };
        _formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _formLayout.RowCount = 1;
        _formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Left detail column: Identity & Properties
        _leftColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(0, 0, 8, 0),
        };
        _leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _leftColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Right detail column: Seeds, Traits & Moods
        _rightColumn = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(8, 0, 0, 0),
        };
        _rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _rightColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _formLayout.Controls.Add(_leftColumn, 0, 0);
        _formLayout.Controls.Add(_rightColumn, 1, 0);

        // -- Create all field controls --

        _typeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var entry in CompanionDatabase.Entries)
            _typeField.Items.Add(entry);
        _typeField.SelectedIndexChanged += (s, e) => { if (!_loading) { WriteType(); ReloadDescriptorsForCurrentCompanion(); } };

        _nameField = new TextBox { Dock = DockStyle.Fill };
        _nameField.Leave += (s, e) => WriteName();

        _creatureSeedField = new TextBox { Dock = DockStyle.Fill };
        _creatureSeedField.Leave += (s, e) => WriteCreatureSeed();

        _secondarySeedField = new TextBox { Dock = DockStyle.Fill };
        _secondarySeedField.Leave += (s, e) => WriteSecondarySeed();

        _speciesSeedField = new TextBox { Dock = DockStyle.Fill };
        _speciesSeedField.Leave += (s, e) => WriteSpeciesSeed();

        _genusSeedField = new TextBox { Dock = DockStyle.Fill };
        _genusSeedField.Leave += (s, e) => WriteGenusSeed();

        _predatorField = new CheckBox { Text = "", AutoSize = true };
        _predatorField.CheckedChanged += (s, e) => { if (!_loading) WritePredator(); };

        _biomeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _biomeField.Items.AddRange(CompanionLogic.BiomeTypes);
        _biomeField.SelectedIndexChanged += (s, e) => { if (!_loading) WriteBiome(); };

        _creatureTypeField = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _creatureTypeField.Items.AddRange(CompanionDatabase.CreatureTypes);
        _creatureTypeField.SelectedIndexChanged += (s, e) => { if (!_loading) WriteCreatureType(); };

        _scaleField = new TextBox { Dock = DockStyle.Fill };
        _scaleField.Leave += (s, e) => WriteScale();

        _trustField = new TextBox { Dock = DockStyle.Fill };
        _trustField.Leave += (s, e) => WriteTrust();

        _boneScaleSeedField = new TextBox { Dock = DockStyle.Fill };
        _boneScaleSeedField.Leave += (s, e) => WriteBoneScaleSeed();

        _colourBaseSeedField = new TextBox { Dock = DockStyle.Fill };
        _colourBaseSeedField.Leave += (s, e) => WriteColourBaseSeed();

        _hasFurField = new CheckBox { Text = "", AutoSize = true };
        _hasFurField.CheckedChanged += (s, e) => { if (!_loading) WriteHasFur(); };

        _helpfulnessField = new TextBox { Dock = DockStyle.Fill };
        _helpfulnessField.Leave += (s, e) => WriteTrait(0, _helpfulnessField);

        _aggressionField = new TextBox { Dock = DockStyle.Fill };
        _aggressionField.Leave += (s, e) => WriteTrait(1, _aggressionField);

        _independenceField = new TextBox { Dock = DockStyle.Fill };
        _independenceField.Leave += (s, e) => WriteTrait(2, _independenceField);

        _hungryField = new TextBox { Dock = DockStyle.Fill };
        _hungryField.Leave += (s, e) => WriteMood(0, _hungryField);

        _lonelyField = new TextBox { Dock = DockStyle.Fill };
        _lonelyField.Leave += (s, e) => WriteMood(1, _lonelyField);

        // Slot Unlocked checkbox
        _unlockedCheck = new CheckBox { Text = "", AutoSize = true };
        _unlockedCheck.CheckedChanged += (s, e) =>
        {
            if (_loading || _playerState == null) return;
            int idx = _companionList.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return;
            var entry = _entries[idx];
            if (entry.Source != "Pet") return;
            CompanionLogic.SetSlotUnlocked(_playerState, entry.OriginalIndex, _unlockedCheck.Checked);
            RefreshListEntry();
        };

        // DateTimePickers
        _birthTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _birthTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_birthTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("BirthTime", unix);
            }
            catch { }
        };

        _lastEggTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _lastEggTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_lastEggTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("LastEggTime", unix);
            }
            catch { }
        };

        _customSpeciesNameField = new TextBox { Dock = DockStyle.Fill };
        _customSpeciesNameField.Leave += (s, e) => WriteCustomSpeciesName();

        _eggModifiedField = new CheckBox { Text = "", AutoSize = true };
        _eggModifiedField.CheckedChanged += (s, e) => { if (!_loading) WriteEggModified(); };

        _hasBeenSummonedField = new CheckBox { Text = "", AutoSize = true };
        _hasBeenSummonedField.CheckedChanged += (s, e) => { if (!_loading) WriteHasBeenSummoned(); };

        _allowUnmodifiedRerollField = new CheckBox { Text = "", AutoSize = true };
        _allowUnmodifiedRerollField.CheckedChanged += (s, e) => { if (!_loading) WriteAllowUnmodifiedReroll(); };

        _uaField = new TextBox { Dock = DockStyle.Fill };
        _uaField.Leave += (s, e) => WriteUA();

        _lastTrustIncreaseTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _lastTrustIncreaseTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_lastTrustIncreaseTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("LastTrustIncreaseTime", unix);
            }
            catch { }
        };

        _lastTrustDecreaseTimePicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowUpDown = true,
        };
        _lastTrustDecreaseTimePicker.ValueChanged += (s, e) =>
        {
            if (_loading) return;
            var comp = SelectedCompanion;
            if (comp == null) return;
            try
            {
                long unix = ((DateTimeOffset)_lastTrustDecreaseTimePicker.Value.ToUniversalTime()).ToUnixTimeSeconds();
                comp.Set("LastTrustDecreaseTime", unix);
            }
            catch { }
        };

        _descriptorPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 5, 0, 5),
        };

        // -- Left column rows: Identity & Properties --
        int lRow = 0;
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _slotUnlockedLabel = AddRow(_leftColumn, "Slot Unlocked:", _unlockedCheck, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _speciesLabel = AddRow(_leftColumn, "Species:", _typeField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _nameLabel = AddRow(_leftColumn, "Name:", _nameField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _typeLabel = AddRow(_leftColumn, "Type:", _creatureTypeField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _biomeLabel = AddRow(_leftColumn, "Biome:", _biomeField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _predatorLabel = AddRow(_leftColumn, "Predator:", _predatorField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _hasFurLabel = AddRow(_leftColumn, "Has Fur:", _hasFurField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _scaleLabel = AddRow(_leftColumn, "Scale:", _scaleField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trustLabel = AddRow(_leftColumn, "Trust:", _trustField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _birthTimeLabel = AddRow(_leftColumn, "Birth Time:", _birthTimePicker, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lastEggTimeLabel = AddRow(_leftColumn, "Last Egg Time:", _lastEggTimePicker, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _customSpeciesNameLabel = AddRow(_leftColumn, "Custom Species Name:", _customSpeciesNameField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _eggModifiedLabel = AddRow(_leftColumn, "Egg Modified:", _eggModifiedField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _summonedLabel = AddRow(_leftColumn, "Summoned:", _hasBeenSummonedField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _allowRerollLabel = AddRow(_leftColumn, "Allow Reroll:", _allowUnmodifiedRerollField, lRow++);
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _uaLabel = AddRow(_leftColumn, "UA:", _uaField, lRow++);
        // Blank spacer row at bottom of left column for alignment
        _leftColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _leftColumn.Controls.Add(new Label { Text = "", AutoSize = true }, 0, lRow++);

        // -- Right column rows: Seeds, Traits & Moods --
        int rRow = 0;
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _creatureSeedLabel = AddSeedRow(_rightColumn, "Creature Seed:", _creatureSeedField, rRow++, WriteCreatureSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _secondarySeedLabel = AddSeedRow(_rightColumn, "Secondary Seed:", _secondarySeedField, rRow++, WriteSecondarySeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _speciesSeedLabel = AddSeedRow(_rightColumn, "Species Seed:", _speciesSeedField, rRow++, WriteSpeciesSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _genusSeedLabel = AddSeedRow(_rightColumn, "Genus Seed:", _genusSeedField, rRow++, WriteGenusSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _boneScaleSeedLabel = AddSeedRow(_rightColumn, "Bone Scale Seed:", _boneScaleSeedField, rRow++, WriteBoneScaleSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _colourBaseSeedLabel = AddSeedRow(_rightColumn, "Colour Base Seed:", _colourBaseSeedField, rRow++, WriteColourBaseSeed);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _helpfulnessLabel = AddRow(_rightColumn, "Helpfulness:", _helpfulnessField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _aggressionLabel = AddRow(_rightColumn, "Aggression:", _aggressionField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _independenceLabel = AddRow(_rightColumn, "Independence:", _independenceField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _hungryLabel = AddRow(_rightColumn, "Hungry:", _hungryField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _lonelyLabel = AddRow(_rightColumn, "Lonely:", _lonelyField, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trustIncreaseLabel = AddRow(_rightColumn, "Trust Increase:", _lastTrustIncreaseTimePicker, rRow++);
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _trustDecreaseLabel = AddRow(_rightColumn, "Trust Decrease:", _lastTrustDecreaseTimePicker, rRow++);
        // Blank spacer row at bottom of right column for alignment
        _rightColumn.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rightColumn.Controls.Add(new Label { Text = "", AutoSize = true }, 0, rRow++);

        // -- Descriptors heading with Creature Builder button --
        var descriptorsRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
        };
        _descriptorsHeading = new Label
        {
            Text = "Descriptors (Parts)",
            AutoSize = true,
            Padding = new Padding(0, 10, 10, 5)
        };
        FontManager.ApplyHeadingFont(_descriptorsHeading, 11);

        _creatureBuilderBtn = new Button { Text = "Creature Builder (Web)", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _creatureBuilderBtn.Click += (s, e) =>
        {
            try { Process.Start(new ProcessStartInfo("https://creature.nmscd.com/#/builder") { UseShellExecute = true }); }
            catch { }
        };

        descriptorsRow.Controls.Add(_descriptorsHeading);
        descriptorsRow.Controls.Add(_creatureBuilderBtn);

        // ======== Accessory Customisation Section ========
        _accessoryHeading = new Label
        {
            Text = "Accessory Customisation",
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 5)
        };
        FontManager.ApplyHeadingFont(_accessoryHeading, 11);

        _accessoryPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 6,
            Padding = new Padding(0, 5, 0, 5),
        };
        _accessoryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Slot label
        _accessoryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); // Combo
        _accessoryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Primary colour swatch
        _accessoryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Alt colour swatch
        _accessoryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Scale label + field
        _accessoryPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));   // Reset

        string[] slotLabels = { "Right:", "Left:", "Chest:" };
        _accessorySlotLabels = new Label[3];
        _accessoryCombos = new ComboBox[3];
        _accessoryDescriptorLabels = new Label[3];
        _accessoryPrimaryColourBtns = new Button[3];
        _accessoryAltColourBtns = new Button[3];
        _accessoryPrimarySwatches = new Panel[3];
        _accessoryAltSwatches = new Panel[3];
        _accessoryScaleFields = new TextBox[3];
        _accessoryScaleLabels = new Label[3];
        _accessoryResetBtns = new Button[3];

        for (int slot = 0; slot < 3; slot++)
        {
            _accessoryPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            int s = slot; // capture

            _accessorySlotLabels[slot] = new Label
            {
                Text = slotLabels[slot],
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 5, 0),
            };

            _accessoryCombos[slot] = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            _accessoryCombos[slot].SelectedIndexChanged += (sender, e) => { if (!_loading) OnAccessoryChanged(s); };

            _accessoryDescriptorLabels[slot] = new Label
            {
                Text = "",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(5, 5, 5, 0),
                ForeColor = SystemColors.GrayText,
            };

            // Primary colour: label + clickable swatch
            var primaryRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            var primaryLbl = new Label { Text = "P:", AutoSize = true, Padding = new Padding(0, 5, 2, 0) };
            _accessoryPrimarySwatches[slot] = new Panel
            {
                Size = new Size(18, 18),
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 0, 0),
                Cursor = Cursors.Hand,
            };
            _accessoryPrimarySwatches[slot].Click += (sender, e) => OnAccessoryColourClick(s, 0);
            // Keep hidden button for programmatic access
            _accessoryPrimaryColourBtns[slot] = new Button { Visible = false, Size = new Size(0, 0) };
            primaryRow.Controls.Add(primaryLbl);
            primaryRow.Controls.Add(_accessoryPrimarySwatches[slot]);

            // Alt colour: label + clickable swatch
            var altRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            var altLbl = new Label { Text = "A:", AutoSize = true, Padding = new Padding(5, 5, 2, 0) };
            _accessoryAltSwatches[slot] = new Panel
            {
                Size = new Size(18, 18),
                BackColor = SystemColors.Control,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 4, 0, 0),
                Cursor = Cursors.Hand,
            };
            _accessoryAltSwatches[slot].Click += (sender, e) => OnAccessoryColourClick(s, 1);
            _accessoryAltColourBtns[slot] = new Button { Visible = false, Size = new Size(0, 0) };
            altRow.Controls.Add(altLbl);
            altRow.Controls.Add(_accessoryAltSwatches[slot]);

            // Scale with label
            var scaleRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0) };
            _accessoryScaleLabels[slot] = new Label { Text = "Scale:", AutoSize = true, Padding = new Padding(5, 5, 2, 0) };
            _accessoryScaleFields[slot] = new TextBox { Width = 50, Text = "1.0" };
            _accessoryScaleFields[slot].Leave += (sender, e) => OnAccessoryScaleChanged(s);
            scaleRow.Controls.Add(_accessoryScaleLabels[slot]);
            scaleRow.Controls.Add(_accessoryScaleFields[slot]);

            _accessoryResetBtns[slot] = new Button
            {
                Text = "Reset",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(50, 23),
            };
            _accessoryResetBtns[slot].Click += (sender, e) => OnAccessoryReset(s);

            _accessoryPanel.Controls.Add(_accessorySlotLabels[slot], 0, slot);
            _accessoryPanel.Controls.Add(_accessoryCombos[slot], 1, slot);
            _accessoryPanel.Controls.Add(primaryRow, 2, slot);
            _accessoryPanel.Controls.Add(altRow, 3, slot);
            _accessoryPanel.Controls.Add(scaleRow, 4, slot);
            _accessoryPanel.Controls.Add(_accessoryResetBtns[slot], 5, slot);
        }

        // Assemble Stats tab inner layout
        // Two-column row for Descriptors (left) and Accessories (right)
        var descAccessoryRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
        };
        descAccessoryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        descAccessoryRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        descAccessoryRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var descColumn = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        descColumn.Controls.Add(descriptorsRow);
        descColumn.Controls.Add(_descriptorPanel);
        descAccessoryRow.Controls.Add(descColumn, 0, 0);

        var accColumn = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
        };
        accColumn.Controls.Add(_accessoryHeading);
        accColumn.Controls.Add(_accessoryPanel);
        descAccessoryRow.Controls.Add(accColumn, 1, 0);

        var innerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
        };
        innerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        innerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        innerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        innerLayout.Controls.Add(_formLayout, 0, 0);
        innerLayout.Controls.Add(descAccessoryRow, 0, 1);

        statsScroll.Controls.Add(innerLayout);

        // ======== Battle Tab Content ========
        BuildBattleTab();

        mainLayout.Controls.Add(_detailPanel, 1, 0);

        Controls.Add(mainLayout);
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// Builds all controls for the Battle tab.
    /// Layout from top to bottom:
    ///   Row 0: [Stat Class Overrides (left) | Genetic Profile (right)]
    ///   Row 1: Affinity
    ///   Row 2+: Ability Slots (single column, per-slot two-column: left=controls, right=detail panel)
    /// </summary>
    private void BuildBattleTab()
    {
        var battleScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0) };
        _battlePage.Padding = new Padding(2, 0, 2, 2);
        _battlePage.Controls.Add(battleScroll);

        var battleLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(5),
        };
        battleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int bRow = 0;

        // == Row 0: Two-column top section ==
        battleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var topRow = new TableLayoutPanel { Dock = DockStyle.Left, AutoSize = true, ColumnCount = 2, RowCount = 1 };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // == Left column: Stat Class Overrides ==
        var statClassPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, MinimumSize = new Size(330, 0), ColumnCount = 2, Padding = new Padding(0, 0, 8, 0) };
        statClassPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        statClassPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int scRow = 0;
        _battleOverrideClassesLabel = new Label { Text = "Stat Class Overrides", AutoSize = true, Padding = new Padding(0, 0, 0, 3) };
        FontManager.ApplyHeadingFont(_battleOverrideClassesLabel, 10);
        statClassPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statClassPanel.Controls.Add(_battleOverrideClassesLabel, 0, scRow);
        statClassPanel.SetColumnSpan(_battleOverrideClassesLabel, 2);
        scRow++;

        // Override checkbox + average class on the same row
        statClassPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var overrideRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 3, 0, 0) };
        _battleOverrideCheck = new CheckBox { Text = "Override Pet Classes", AutoSize = true, Margin = new Padding(0, 3, 0, 0) };
        _battleOverrideCheck.CheckedChanged += (s, e) => { if (!_loading) OnBattleOverrideChanged(); };
        _battleAverageClassLabel = new Label { Text = "Average Class:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(15, 3, 5, 0) };
        _battleAverageClassValue = new Label { Text = "-", AutoSize = true, Margin = new Padding(0, 3, 0, 0) };
        overrideRow.Controls.Add(_battleOverrideCheck);
        overrideRow.Controls.Add(_battleAverageClassLabel);
        overrideRow.Controls.Add(_battleAverageClassValue);
        statClassPanel.Controls.Add(overrideRow, 0, scRow);
        statClassPanel.SetColumnSpan(overrideRow, 2);
        scRow++;

        string[] classItems = { "C", "B", "A", "S" };

        // Health class
        statClassPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleHealthClassLabel = new Label { Text = "Health:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 5, 0) };
        _battleHealthClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 50 };
        _battleHealthClass.Items.AddRange(classItems);
        _battleHealthClass.SelectedIndexChanged += (s, e) => { if (!_loading) OnBattleClassChanged(); };
        statClassPanel.Controls.Add(_battleHealthClassLabel, 0, scRow);
        statClassPanel.Controls.Add(_battleHealthClass, 1, scRow);
        scRow++;

        // Agility class
        statClassPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleAgilityClassLabel = new Label { Text = "Agility:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 5, 0) };
        _battleAgilityClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 50 };
        _battleAgilityClass.Items.AddRange(classItems);
        _battleAgilityClass.SelectedIndexChanged += (s, e) => { if (!_loading) OnBattleClassChanged(); };
        statClassPanel.Controls.Add(_battleAgilityClassLabel, 0, scRow);
        statClassPanel.Controls.Add(_battleAgilityClass, 1, scRow);
        scRow++;

        // Combat class
        statClassPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleCombatClassLabel = new Label { Text = "Combat Effectiveness:", AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 5, 0) };
        _battleCombatClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 50 };
        _battleCombatClass.Items.AddRange(classItems);
        _battleCombatClass.SelectedIndexChanged += (s, e) => { if (!_loading) OnBattleClassChanged(); };
        statClassPanel.Controls.Add(_battleCombatClassLabel, 0, scRow);
        statClassPanel.Controls.Add(_battleCombatClass, 1, scRow);
        scRow++;

        // Holo-Arena Victories (moved to bottom of Stat Class Overrides)
        statClassPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleVictoriesLabel = new Label { Text = "Holo-Arena Victories:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleVictories = new NumericUpDown { Minimum = 0, Maximum = 999999, Width = 70 };
        _battleVictories.ValueChanged += (s, e) => { if (!_loading) WriteBattleVictories(); };
        statClassPanel.Controls.Add(_battleVictoriesLabel, 0, scRow);
        statClassPanel.Controls.Add(_battleVictories, 1, scRow);

        topRow.Controls.Add(statClassPanel, 0, 0);

        // == Right column: Genetic Profile ==
        var mutationPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 0, 0, 0), Margin = new Padding(12, 0, 0, 0) };
        mutationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mutationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        int mpRow = 0;
        _battleMutationHeadingLabel = new Label { Text = "Genetic Profile", AutoSize = true, Padding = new Padding(0, 0, 0, 3) };
        FontManager.ApplyHeadingFont(_battleMutationHeadingLabel, 10);
        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mutationPanel.Controls.Add(_battleMutationHeadingLabel, 0, mpRow);
        mutationPanel.SetColumnSpan(_battleMutationHeadingLabel, 2);
        mpRow++;

        // Genes Improved (read-only)
        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleGenesLevelLabel = new Label { Text = "Genes Improved:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleGenesLevelValue = new Label { Text = "0 / 30", AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
        mutationPanel.Controls.Add(_battleGenesLevelLabel, 0, mpRow);
        mutationPanel.Controls.Add(_battleGenesLevelValue, 1, mpRow);
        mpRow++;

        // Mutation Progress
        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleMutationProgressLabel = new Label { Text = "Mutation Progress:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleMutationProgress = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 1.0m,
            DecimalPlaces = 16,
            Increment = 0.1m,
            Width = 170,
            Value = 0,
        };
        _battleMutationProgress.ValueChanged += (s, e) => OnMutationProgressChanged();
        mutationPanel.Controls.Add(_battleMutationProgressLabel, 0, mpRow);
        mutationPanel.Controls.Add(_battleMutationProgress, 1, mpRow);
        mpRow++;

        // Gene Edits Available
        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleGenesAvailableLabel = new Label { Text = "Gene Edits Available:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleGenesAvailable = new NumericUpDown { Minimum = 0, Maximum = 1000, Width = 55 };
        _battleGenesAvailable.ValueChanged += (s, e) => { if (!_loading) WriteBattleTreatsAvailable(); };
        mutationPanel.Controls.Add(_battleGenesAvailableLabel, 0, mpRow);
        mutationPanel.Controls.Add(_battleGenesAvailable, 1, mpRow);
        mpRow++;

        // Gene edits (Health / Agility / Combat) - no heading, directly under Gene Edits Available
        _battleTreatsHeadingLabel = new Label { Text = "", AutoSize = true, Visible = false };

        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleTreatHealthLabel = new Label { Text = "Health:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleTreatHealth = new NumericUpDown { Minimum = 0, Maximum = 10, Width = 55 };
        _battleTreatHealth.ValueChanged += (s, e) => { if (!_loading) OnBattleTreatChanged(); };
        mutationPanel.Controls.Add(_battleTreatHealthLabel, 0, mpRow);
        mutationPanel.Controls.Add(_battleTreatHealth, 1, mpRow);
        mpRow++;

        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleTreatAgilityLabel = new Label { Text = "Agility:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleTreatAgility = new NumericUpDown { Minimum = 0, Maximum = 10, Width = 55 };
        _battleTreatAgility.ValueChanged += (s, e) => { if (!_loading) OnBattleTreatChanged(); };
        mutationPanel.Controls.Add(_battleTreatAgilityLabel, 0, mpRow);
        mutationPanel.Controls.Add(_battleTreatAgility, 1, mpRow);
        mpRow++;

        mutationPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _battleTreatCombatLabel = new Label { Text = "Combat:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        _battleTreatCombat = new NumericUpDown { Minimum = 0, Maximum = 10, Width = 55 };
        _battleTreatCombat.ValueChanged += (s, e) => { if (!_loading) OnBattleTreatChanged(); };
        mutationPanel.Controls.Add(_battleTreatCombatLabel, 0, mpRow);
        mutationPanel.Controls.Add(_battleTreatCombat, 1, mpRow);

        topRow.Controls.Add(mutationPanel, 1, 0);
        battleLayout.Controls.Add(topRow, 0, bRow++);

        // == Affinity row ==
        battleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var affinityRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill, Padding = new Padding(0, 1, 0, 1) };
        _battleAffinityLabel = new Label { Text = "Affinity:", AutoSize = true, Padding = new Padding(0, 5, 10, 0) };
        FontManager.ApplyHeadingFont(_battleAffinityLabel, 10);
        _battleAffinityValue = new Label { Text = "", AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
        affinityRow.Controls.Add(_battleAffinityLabel);
        affinityRow.Controls.Add(_battleAffinityValue);
        battleLayout.Controls.Add(affinityRow, 0, bRow++);

        // == Ability Slots list ==
        // (two-column grid, left to right order)
        // Move slot controls on the left, per-slot detail panels on the right
        // Details split by type (phase on right)
        // Removed header per tester feedback
        _battleMoveListLabel = new Label { Text = "", AutoSize = true, Visible = false };

        _moveSlotPanels = new Panel[5];
        _moveSlotLabels = new Label[5];
        _moveSlotCombos = new ComboBox[5];
        _moveSlotMovesetLabels = new Label[5];
        _moveSlotCooldowns = new NumericUpDown[5];
        _moveSlotCooldownLabels = new Label[5];
        _moveSlotScoreBoosts = new NumericUpDown[5];
        _moveSlotScoreBoostLabels = new Label[5];
        _moveSlotDetailPanels = new TableLayoutPanel[5];

        // Single-column container for all move slots (top to bottom)
        for (int i = 0; i < 5; i++)
        {
            int slotIdx = i;
            battleLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Move slot heading
            _moveSlotLabels[i] = new Label { Text = $"Ability Slot {i + 1}:", AutoSize = true, Padding = new Padding(0, 2, 5, 0) };
            FontManager.ApplyHeadingFont(_moveSlotLabels[i], 9);

            _moveSlotCombos[i] = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 320,
            };
            _moveSlotCombos[i].SelectedIndexChanged += (s, e) => { if (!_loading) OnMoveSlotChanged(slotIdx); };
            // Auto-size dropdown width to fit longest item
            _moveSlotCombos[i].DropDown += (s, e) =>
            {
                var combo = (ComboBox)s!;
                int maxWidth = combo.Width;
                using var g = combo.CreateGraphics();
                foreach (var item in combo.Items)
                {
                    int w = (int)g.MeasureString(item.ToString() ?? "", combo.Font).Width + SystemInformation.VerticalScrollBarWidth;
                    if (w > maxWidth) maxWidth = w;
                }
                combo.DropDownWidth = maxWidth;
            };

            var controlRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            _moveSlotCooldownLabels[i] = new Label { Text = "Cooldown:", AutoSize = true, Padding = new Padding(0, 5, 3, 0) };
            _moveSlotCooldowns[i] = new NumericUpDown { Minimum = 0, Maximum = 20, Width = 50 };
            _moveSlotCooldowns[i].ValueChanged += (s, e) => { if (!_loading) OnMoveSlotCooldownChanged(slotIdx); };
            _moveSlotScoreBoostLabels[i] = new Label { Text = "Score Boost:", AutoSize = true, Padding = new Padding(8, 5, 3, 0) };
            _moveSlotScoreBoosts[i] = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 10,
                DecimalPlaces = 6,
                Increment = 0.1m,
                Width = 90,
                Value = 0,
            };
            _moveSlotScoreBoosts[i].ValueChanged += (s, e) => { if (!_loading) OnMoveSlotScoreBoostChanged(slotIdx); };
            controlRow.Controls.Add(_moveSlotCooldownLabels[i]);
            controlRow.Controls.Add(_moveSlotCooldowns[i]);
            controlRow.Controls.Add(_moveSlotScoreBoostLabels[i]);
            controlRow.Controls.Add(_moveSlotScoreBoosts[i]);

            _moveSlotMovesetLabels[i] = new Label
            {
                Text = "",
                AutoSize = true,
                Padding = new Padding(0, 2, 0, 0),
                ForeColor = SystemColors.GrayText,
            };

            // Detail panel: two-column label-value grid (populated at runtime)
            _moveSlotDetailPanels[i] = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 5,
                Visible = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(5, 0, 0, 2),
            };
            _moveSlotDetailPanels[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // left label
            _moveSlotDetailPanels[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // left value
            _moveSlotDetailPanels[i].ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20)); // spacer
            _moveSlotDetailPanels[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // right label
            _moveSlotDetailPanels[i].ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // right value

            // Left column: combo, cooldown/scoreboost, moveset label
            var leftCol = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Fill,
            };
            leftCol.Controls.Add(_moveSlotCombos[i]);
            leftCol.Controls.Add(controlRow);
            leftCol.Controls.Add(_moveSlotMovesetLabels[i]);

            // Per-slot two-column layout: left = controls, right = detail panel
            var slotLayout = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 4, 4),
            };
            slotLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            slotLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            slotLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            slotLayout.Controls.Add(leftCol, 0, 0);
            slotLayout.Controls.Add(_moveSlotDetailPanels[i], 1, 0);

            // Wrap heading + slot layout in a container
            var slotContainer = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 1, 0, 0),
            };
            slotContainer.Controls.Add(_moveSlotLabels[i]);
            slotContainer.Controls.Add(slotLayout);
            _moveSlotPanels[i] = slotContainer;

            battleLayout.Controls.Add(slotContainer, 0, bRow++);
        }

        battleScroll.Controls.Add(battleLayout);
    }

    private ListBox _companionList = null!;
    private Label _countLabel = null!;
    private Panel _detailPanel = null!;
    private DoubleBufferedTabControl _tabControl = null!;
    private TabPage _statsPage = null!;
    private TabPage _battlePage = null!;
    private TableLayoutPanel _formLayout = null!;
    private TableLayoutPanel _leftColumn = null!;
    private TableLayoutPanel _rightColumn = null!;
    private Button _deleteBtn = null!;
    private Button _creatureBuilderBtn = null!;
    private ComboBox _typeField = null!;
    private TextBox _nameField = null!;
    private TextBox _creatureSeedField = null!;
    private TextBox _secondarySeedField = null!;
    private TextBox _speciesSeedField = null!;
    private TextBox _genusSeedField = null!;
    private CheckBox _predatorField = null!;
    private ComboBox _biomeField = null!;
    private ComboBox _creatureTypeField = null!;
    private TextBox _scaleField = null!;
    private TextBox _trustField = null!;
    private TextBox _boneScaleSeedField = null!;
    private TextBox _colourBaseSeedField = null!;
    private CheckBox _hasFurField = null!;
    private TextBox _helpfulnessField = null!;
    private TextBox _aggressionField = null!;
    private TextBox _independenceField = null!;
    private TextBox _hungryField = null!;
    private TextBox _lonelyField = null!;
    private DateTimePicker _birthTimePicker = null!;
    private DateTimePicker _lastEggTimePicker = null!;
    private CheckBox _unlockedCheck = null!;
    private TextBox _customSpeciesNameField = null!;
    private CheckBox _eggModifiedField = null!;
    private CheckBox _hasBeenSummonedField = null!;
    private CheckBox _allowUnmodifiedRerollField = null!;
    private TextBox _uaField = null!;
    private DateTimePicker _lastTrustIncreaseTimePicker = null!;
    private DateTimePicker _lastTrustDecreaseTimePicker = null!;
    private FlowLayoutPanel _descriptorPanel = null!;
    private Label _titleLabel = null!;
    private Label _descriptorsHeading = null!;

    // Left column labels
    private Label _slotUnlockedLabel = null!;
    private Label _speciesLabel = null!;
    private Label _nameLabel = null!;
    private Label _typeLabel = null!;
    private Label _biomeLabel = null!;
    private Label _predatorLabel = null!;
    private Label _hasFurLabel = null!;
    private Label _scaleLabel = null!;
    private Label _trustLabel = null!;
    private Label _birthTimeLabel = null!;
    private Label _lastEggTimeLabel = null!;
    private Label _customSpeciesNameLabel = null!;
    private Label _eggModifiedLabel = null!;
    private Label _summonedLabel = null!;
    private Label _allowRerollLabel = null!;
    private Label _uaLabel = null!;

    // Right column seed labels
    private Label _creatureSeedLabel = null!;
    private Label _secondarySeedLabel = null!;
    private Label _speciesSeedLabel = null!;
    private Label _genusSeedLabel = null!;
    private Label _boneScaleSeedLabel = null!;
    private Label _colourBaseSeedLabel = null!;

    // Right column trait & mood labels
    private Label _helpfulnessLabel = null!;
    private Label _aggressionLabel = null!;
    private Label _independenceLabel = null!;
    private Label _hungryLabel = null!;
    private Label _lonelyLabel = null!;
    private Label _trustIncreaseLabel = null!;
    private Label _trustDecreaseLabel = null!;

    // Button fields for localisation
    private Button _exportCompanionBtn = null!;
    private Button _importCompanionBtn = null!;

    // Seed "Gen" buttons created by AddSeedRow - stored for re-localisation
    private readonly List<Button> _seedGenButtons = new();
    // Regen Descriptor ID button - stored for re-localisation
    private Button? _regenDescriptorBtn;

    // Accessory Customisation section
    private Label _accessoryHeading = null!;
    private TableLayoutPanel _accessoryPanel = null!;
    private Label[] _accessorySlotLabels = null!;
    private ComboBox[] _accessoryCombos = null!;
    private Label[] _accessoryDescriptorLabels = null!;
    private Button[] _accessoryPrimaryColourBtns = null!;
    private Button[] _accessoryAltColourBtns = null!;
    private Panel[] _accessoryPrimarySwatches = null!;
    private Panel[] _accessoryAltSwatches = null!;
    private TextBox[] _accessoryScaleFields = null!;
    private Label[] _accessoryScaleLabels = null!;
    private Button[] _accessoryResetBtns = null!;

    // Battle tab fields
    private Label _battleAffinityLabel = null!;
    private Label _battleAffinityValue = null!;
    private Label _battleOverrideClassesLabel = null!;
    private CheckBox _battleOverrideCheck = null!;
    private Label _battleHealthClassLabel = null!;
    private ComboBox _battleHealthClass = null!;
    private Label _battleAgilityClassLabel = null!;
    private ComboBox _battleAgilityClass = null!;
    private Label _battleCombatClassLabel = null!;
    private ComboBox _battleCombatClass = null!;
    private Label _battleAverageClassLabel = null!;
    private Label _battleAverageClassValue = null!;
    private Label _battleTreatsHeadingLabel = null!;
    private Label _battleTreatHealthLabel = null!;
    private NumericUpDown _battleTreatHealth = null!;
    private Label _battleTreatAgilityLabel = null!;
    private NumericUpDown _battleTreatAgility = null!;
    private Label _battleTreatCombatLabel = null!;
    private NumericUpDown _battleTreatCombat = null!;
    private Label _battleGenesLevelLabel = null!;
    private Label _battleGenesLevelValue = null!;
    private Label _battleGenesAvailableLabel = null!;
    private NumericUpDown _battleGenesAvailable = null!;
    private Label _battleMutationHeadingLabel = null!;
    private Label _battleMutationProgressLabel = null!;
    private NumericUpDown _battleMutationProgress = null!;
    private Label _battleVictoriesLabel = null!;
    private NumericUpDown _battleVictories = null!;
    private Label _battleMoveListLabel = null!;

    // Per-move-slot controls (5 slots)
    private Panel[] _moveSlotPanels = null!;
    private Label[] _moveSlotLabels = null!;
    private ComboBox[] _moveSlotCombos = null!;
    private Label[] _moveSlotMovesetLabels = null!;
    private NumericUpDown[] _moveSlotCooldowns = null!;
    private Label[] _moveSlotCooldownLabels = null!;
    private NumericUpDown[] _moveSlotScoreBoosts = null!;
    private Label[] _moveSlotScoreBoostLabels = null!;
    private TableLayoutPanel[] _moveSlotDetailPanels = null!;
}
