using NMSE.Data;

namespace NMSE.UI.Panels;

partial class CataloguePanel
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
        // CataloguePanel
        // 
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
    }
    #endregion

    private void SetupLayout()
    {
        SuspendLayout();

        _tabControl = new DoubleBufferedTabControl { Dock = DockStyle.Fill };

        // --- Tab 1: Known Technologies ---
        var techTab = new TabPage("Known Technologies");
        var techLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        techLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        techLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        techLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var techFilterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _techFilterBox = new TextBox { Width = 200, PlaceholderText = "Filter by name, category or ID..." };
        _techFilterBox.TextChanged += (s, e) => ApplyFilter(_techGrid!, _techFilterBox.Text);
        _techFilterClearButton = new Button { Text = "X", Width = 28, Height = 23 };
        _techFilterClearButton.Click += (s, e) => { _techFilterBox.Text = ""; };
        techFilterPanel.Controls.Add(_techFilterBox);
        techFilterPanel.Controls.Add(_techFilterClearButton);
        techLayout.Controls.Add(techFilterPanel, 0, 0);

        _techGrid = CreateItemGrid();
        techLayout.Controls.Add(_techGrid, 0, 1);

        var techButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _addTechButton = new Button { Text = "Add Technology", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _removeTechButton = new Button { Text = "Remove Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _addTechButton.Click += AddTech_Click;
        _removeTechButton.Click += RemoveTech_Click;
        _exportTechBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportTechBtn.Click += (s, e) => ExportDiscoveryList("Known Technologies", _techGrid, "ID");
        _importTechBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importTechBtn.Click += (s, e) => ImportItemList(_techGrid, "KnownTech");
        techButtonPanel.Controls.Add(_addTechButton);
        techButtonPanel.Controls.Add(_removeTechButton);
        techButtonPanel.Controls.Add(_exportTechBtn);
        techButtonPanel.Controls.Add(_importTechBtn);
        techLayout.Controls.Add(techButtonPanel, 0, 2);

        techTab.Controls.Add(techLayout);
        _tabControl.TabPages.Add(techTab);

        // --- Tab 2: Known Products ---
        var productTab = new TabPage("Known Products");
        var productLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        productLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        productLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        productLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var productFilterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _productFilterBox = new TextBox { Width = 200, PlaceholderText = "Filter by name, category or ID..." };
        _productFilterBox.TextChanged += (s, e) => ApplyFilter(_productGrid!, _productFilterBox.Text);
        _productFilterClearButton = new Button { Text = "X", Width = 28, Height = 23 };
        _productFilterClearButton.Click += (s, e) => { _productFilterBox.Text = ""; };
        productFilterPanel.Controls.Add(_productFilterBox);
        productFilterPanel.Controls.Add(_productFilterClearButton);
        productLayout.Controls.Add(productFilterPanel, 0, 0);

        _productGrid = CreateItemGrid();
        productLayout.Controls.Add(_productGrid, 0, 1);

        var productButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _addProductButton = new Button { Text = "Add Product", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _removeProductButton = new Button { Text = "Remove Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _addProductButton.Click += AddProduct_Click;
        _removeProductButton.Click += RemoveProduct_Click;
        _exportProductBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportProductBtn.Click += (s, e) => ExportDiscoveryList("Known Products", _productGrid, "ID");
        _importProductBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importProductBtn.Click += (s, e) => ImportItemList(_productGrid, "KnownProducts");
        productButtonPanel.Controls.Add(_addProductButton);
        productButtonPanel.Controls.Add(_removeProductButton);
        productButtonPanel.Controls.Add(_exportProductBtn);
        productButtonPanel.Controls.Add(_importProductBtn);
        productLayout.Controls.Add(productButtonPanel, 0, 2);

        productTab.Controls.Add(productLayout);
        _tabControl.TabPages.Add(productTab);

        // --- Tab 3: Known Specials (Quicksilver/SpecialShop items) ---
        var specialsTab = new TabPage(UiStrings.Get("discovery.tab_specials"));
        _specialsTabPage = specialsTab;
        var specialsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        specialsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        specialsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        specialsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var specialsFilterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _specialsFilterBox = new TextBox { Width = 200, PlaceholderText = UiStrings.Get("discovery.filter_placeholder") };
        _specialsFilterBox.TextChanged += (s, e) => ApplyFilter(_specialsGrid!, _specialsFilterBox.Text);
        _specialsFilterClearButton = new Button { Text = "X", Width = 28, Height = 23 };
        _specialsFilterClearButton.Click += (s, e) => { _specialsFilterBox.Text = ""; };
        specialsFilterPanel.Controls.Add(_specialsFilterBox);
        specialsFilterPanel.Controls.Add(_specialsFilterClearButton);
        specialsLayout.Controls.Add(specialsFilterPanel, 0, 0);

        _specialsGrid = CreateItemGrid();
        specialsLayout.Controls.Add(_specialsGrid, 0, 1);

        var specialsButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _addSpecialsButton = new Button { Text = UiStrings.Get("discovery.add_special"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _removeSpecialsButton = new Button { Text = UiStrings.Get("common.remove_selected"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _addSpecialsButton.Click += AddSpecials_Click;
        _removeSpecialsButton.Click += RemoveSpecials_Click;
        _exportSpecialsBtn = new Button { Text = UiStrings.Get("common.export"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportSpecialsBtn.Click += (s, e) => ExportDiscoveryList(UiStrings.Get("discovery.tab_specials"), _specialsGrid, "ID");
        _importSpecialsBtn = new Button { Text = UiStrings.Get("common.import"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importSpecialsBtn.Click += (s, e) => ImportItemList(_specialsGrid, "KnownSpecials");
        specialsButtonPanel.Controls.Add(_addSpecialsButton);
        specialsButtonPanel.Controls.Add(_removeSpecialsButton);
        specialsButtonPanel.Controls.Add(_exportSpecialsBtn);
        specialsButtonPanel.Controls.Add(_importSpecialsBtn);
        specialsLayout.Controls.Add(specialsButtonPanel, 0, 2);

        specialsTab.Controls.Add(specialsLayout);
        _tabControl.TabPages.Add(specialsTab);

        // --- Tab 4: Known Words ---
        var wordTab = new TabPage("Known Words");
        var wordLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
        };
        wordLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wordLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        wordLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        wordLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Filter row
        var wordFilterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _wordFilterBox = new TextBox { Width = 200, PlaceholderText = "Filter by word..." };
        _wordFilterBox.TextChanged += (s, e) => ApplyWordFilter();
        var wordFilterClearBtn = new Button { Text = "X", Width = 28, Height = 23 };
        wordFilterClearBtn.Click += (s, e) => { _wordFilterBox.Text = ""; };
        wordFilterPanel.Controls.Add(_wordFilterBox);
        wordFilterPanel.Controls.Add(wordFilterClearBtn);
        wordLayout.Controls.Add(wordFilterPanel, 0, 0);

        // Race icons header panel - uses absolute positioning to align over grid columns
        var raceIconPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 100,
        };
        string[] raceIconFiles = { "UI-GEK.PNG", "UI-VYKEEN.PNG", "UI-KORVAX.PNG", "UI-ATLAS.PNG", "UI-KORVAX.PNG" };
        string[] raceLabels = { "Gek", "Vy'keen", "Korvax", "Atlas", "Autophage" };
        _raceIcons = new PictureBox[raceLabels.Length];
        _raceLabels = new Label[raceLabels.Length];
        _raceLearnButtons = new Button[raceLabels.Length];
        _raceUnlearnButtons = new Button[raceLabels.Length];
        for (int i = 0; i < raceLabels.Length; i++)
        {
            _raceIcons[i] = new PictureBox
            {
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.Zoom,
            };
            _raceLabels[i] = new Label
            {
                Text = raceLabels[i],
                AutoSize = true,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold),
            };
            int raceOrdinal = RaceColumns[i].Index;
            _raceLearnButtons[i] = new Button
            {
                Text = "✓",
                Width = 28, Height = 22,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 140, 60),
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 7),
            };
            _raceLearnButtons[i].FlatAppearance.BorderSize = 0;
            _raceLearnButtons[i].Click += (s, e) => LearnAllForRace(raceOrdinal);

            _raceUnlearnButtons[i] = new Button
            {
                Text = "✗",
                Width = 28, Height = 22,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(160, 60, 60),
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 7),
            };
            _raceUnlearnButtons[i].FlatAppearance.BorderSize = 0;
            _raceUnlearnButtons[i].Click += (s, e) => UnlearnAllForRace(raceOrdinal);

            raceIconPanel.Controls.Add(_raceIcons[i]);
            raceIconPanel.Controls.Add(_raceLabels[i]);
            raceIconPanel.Controls.Add(_raceLearnButtons[i]);
            raceIconPanel.Controls.Add(_raceUnlearnButtons[i]);
        }
        wordLayout.Controls.Add(raceIconPanel, 0, 1);

        _wordGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            ReadOnly = false,
        };
        _wordGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Word", HeaderText = "Word", ReadOnly = true, FillWeight = 40 });
        _wordGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "IndvWordId", HeaderText = "Indv Word ID", ReadOnly = true, FillWeight = 40 });
        foreach (var (name, _) in RaceColumns)
        {
            _wordGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = name, HeaderText = name, FillWeight = 20 });
        }
        // Align race icons over their column headers when layout changes
        _wordGrid.Layout += (_, _) => AlignRaceIcons();
        _wordGrid.ColumnWidthChanged += (_, _) => AlignRaceIcons();
        wordLayout.Controls.Add(_wordGrid, 0, 2);

        var wordButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _learnAllWordsButton = new Button { Text = "Learn All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _unlearnAllWordsButton = new Button { Text = "Unlearn All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _learnAllWordsButton.Click += LearnAllWords_Click;
        _unlearnAllWordsButton.Click += UnlearnAllWords_Click;
        _learnSelectedWordsButton = new Button { Text = "Learn Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _unlearnSelectedWordsButton = new Button { Text = "Unlearn Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _learnSelectedWordsButton.Click += LearnSelectedWords_Click;
        _unlearnSelectedWordsButton.Click += UnlearnSelectedWords_Click;
        _exportWordsBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportWordsBtn.Click += (s, e) => ExportDiscoveryList("Known Words", _wordGrid, "Word");
        _importWordsBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importWordsBtn.Click += (s, e) => ImportWordsList();
        wordButtonPanel.Controls.Add(_learnAllWordsButton);
        wordButtonPanel.Controls.Add(_unlearnAllWordsButton);
        wordButtonPanel.Controls.Add(_learnSelectedWordsButton);
        wordButtonPanel.Controls.Add(_unlearnSelectedWordsButton);
        wordButtonPanel.Controls.Add(_exportWordsBtn);
        wordButtonPanel.Controls.Add(_importWordsBtn);
        wordLayout.Controls.Add(wordButtonPanel, 0, 3);

        wordTab.Controls.Add(wordLayout);
        _tabControl.TabPages.Add(wordTab);

        // --- Tab 4: Known Glyphs ---
        var glyphTab = new TabPage("Known Glyphs");
        var glyphLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        glyphLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        glyphLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // 4x4 grid layout for glyphs
        var glyphGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(20),
        };
        for (int c = 0; c < 4; c++)
            glyphGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (int r = 0; r < 4; r++)
            glyphGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

        _glyphCheckBoxes = new CheckBox[16];
        _glyphIcons = new PictureBox[16];
        for (int i = 0; i < 16; i++)
        {
            var container = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(5),
            };
            _glyphIcons[i] = new PictureBox
            {
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(4, 4, 4, 2),
            };
            _glyphCheckBoxes[i] = new CheckBox
            {
                Text = UiStrings.Format("discovery.glyph_n", i + 1),
                AutoSize = true,
                Margin = new Padding(8, 0, 0, 0),
            };
            container.Controls.Add(_glyphIcons[i]);
            container.Controls.Add(_glyphCheckBoxes[i]);
            int row = i / 4;
            int col = i % 4;
            glyphGrid.Controls.Add(container, col, row);
        }
        glyphLayout.Controls.Add(glyphGrid, 0, 0);

        var glyphButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _learnAllGlyphsButton = new Button { Text = "Learn All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _unlearnAllGlyphsButton = new Button { Text = "Unlearn All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _learnAllGlyphsButton.Click += LearnAllGlyphs_Click;
        _unlearnAllGlyphsButton.Click += UnlearnAllGlyphs_Click;
        _exportGlyphsBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportGlyphsBtn.Click += (s, e) => ExportGlyphsList();
        _importGlyphsBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importGlyphsBtn.Click += (s, e) => ImportGlyphsList();
        glyphButtonPanel.Controls.Add(_learnAllGlyphsButton);
        glyphButtonPanel.Controls.Add(_unlearnAllGlyphsButton);
        glyphButtonPanel.Controls.Add(_exportGlyphsBtn);
        glyphButtonPanel.Controls.Add(_importGlyphsBtn);
        glyphLayout.Controls.Add(glyphButtonPanel, 0, 1);

        glyphTab.Controls.Add(glyphLayout);
        _tabControl.TabPages.Add(glyphTab);

        // --- Tab 5: Known Locations ---
        var locationsTab = new TabPage("Known Locations");
        var locationsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        locationsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // filter
        locationsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // buttons
        locationsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // grid
        locationsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // detail
        locationsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // extra

        // Filter row
        var locFilterPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _locFilterBox = new TextBox { Width = 200, PlaceholderText = "Filter by name, portal code..." };
        _locFilterBox.TextChanged += (s, e) => ApplyLocationFilter();
        var locFilterClearBtn = new Button { Text = "X", Width = 28, Height = 23 };
        locFilterClearBtn.Click += (s, e) => { _locFilterBox.Text = ""; };
        locFilterPanel.Controls.Add(_locFilterBox);
        locFilterPanel.Controls.Add(locFilterClearBtn);
        locationsLayout.Controls.Add(locFilterPanel, 0, 0);

        var locBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        _deleteLocationBtn = new Button { Text = "Delete Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _deleteLocationBtn.Click += DeleteLocation_Click;
        locBtnPanel.Controls.Add(_deleteLocationBtn);
        _travelToBtn = new Button { Text = "Travel to System", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(75, 0) };
        _travelToBtn.Click += TravelToSystem_Click;
        locBtnPanel.Controls.Add(_travelToBtn);
        _exportLocBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportLocBtn.Click += (s, e) => ExportLocationsList();
        _importLocBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importLocBtn.Click += (s, e) => ImportLocationsList();
        locBtnPanel.Controls.Add(_exportLocBtn);
        locBtnPanel.Controls.Add(_importLocBtn);
        locationsLayout.Controls.Add(locBtnPanel, 0, 1);

        _locationsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            ReadOnly = true
        };
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "#", FillWeight = 3 });
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", FillWeight = 35 });
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", FillWeight = 15 });
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Galaxy", HeaderText = "Galaxy", FillWeight = 20 });
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PortalCode", HeaderText = "Portal Code (Hex)", FillWeight = 15 });
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PortalCodeDec", HeaderText = "Portal Code (Dec)", FillWeight = 30 });
        _locationsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SignalBooster", HeaderText = "Signal Booster", FillWeight = 30 });
        _locationsGrid.SelectionChanged += OnLocationSelectionChanged;
        _locationsGrid.CellPainting += OnLocationGalaxyCellPainting;
        locationsLayout.Controls.Add(_locationsGrid, 0, 2);

        // Bottom detail: glyph panel + galaxy label
        var locDetailPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(5)
        };
        _portalGlyphsCaptionLabel = new Label { Text = "Portal Glyphs:", AutoSize = true, Padding = new Padding(0, 5, 5, 0) };
        locDetailPanel.Controls.Add(_portalGlyphsCaptionLabel);
        _locGlyphPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        locDetailPanel.Controls.Add(_locGlyphPanel);
        _galaxyCaptionLabel = new Label { Text = "  Galaxy:", AutoSize = true, Padding = new Padding(10, 5, 5, 0) };
        locDetailPanel.Controls.Add(_galaxyCaptionLabel);
        _locGalaxyLabel = new Label { AutoSize = true, Padding = new Padding(0, 4, 0, 0), Font = new Font(DefaultFont.FontFamily, 9, FontStyle.Bold) };
        locDetailPanel.Controls.Add(_locGalaxyLabel);
        _locGalaxyCoreCaptionLabel = new Label { AutoSize = true, Padding = new Padding(0, 4, 0, 0), Font = new Font(DefaultFont.FontFamily, 9, FontStyle.Bold), Margin = new Padding(2, 0, 0, 0) };
        locDetailPanel.Controls.Add(_locGalaxyCoreCaptionLabel);
        _locGalaxyDot = new Label
        {
            AutoSize = false,
            Text = string.Empty,
            Padding = Padding.Empty,
            Margin = new Padding(0, 6, 0, 0),
            Font = new Font(DefaultFont.FontFamily, 9, FontStyle.Bold),
            ImageAlign = ContentAlignment.MiddleCenter,
            Size = new Size(12, 12)
        };
        locDetailPanel.Controls.Add(_locGalaxyDot);
        locationsLayout.Controls.Add(locDetailPanel, 0, 3);

        locationsTab.Controls.Add(locationsLayout);
        _tabControl.TabPages.Add(locationsTab);

        // --- Tab 6: Known Fish ---
        var fishTab = new TabPage("Known Fish");
        var fishLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        fishLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fishLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        fishLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var fishFilterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _fishFilterBox = new TextBox { Width = 200, PlaceholderText = "Filter by name or ID..." };
        _fishFilterBox.TextChanged += (s, e) => ApplyFishFilter();
        _fishFilterClearBtn = new Button { Text = "X", Width = 28, Height = 23 };
        _fishFilterClearBtn.Click += (s, e) => { _fishFilterBox.Text = ""; };
        fishFilterPanel.Controls.Add(_fishFilterBox);
        fishFilterPanel.Controls.Add(_fishFilterClearBtn);
        fishLayout.Controls.Add(fishFilterPanel, 0, 0);

        _fishGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            RowTemplate = { Height = 28 }
        };
        _fishGrid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "Icon",
            HeaderText = "🐟",
            Width = 30,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        });
        _fishGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CaughtFish", HeaderText = "Caught Fish", ReadOnly = true, FillWeight = 20 });
        _fishGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", ReadOnly = true, FillWeight = 25 });
        _fishGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Count", ReadOnly = false, FillWeight = 12 });
        _fishGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LargestCatch", HeaderText = "Largest Catch", ReadOnly = false, FillWeight = 15 });
        _fishGrid.CellValidating += OnFishCellValidating;
        fishLayout.Controls.Add(_fishGrid, 0, 1);

        var fishButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _addFishBtn = new Button { Text = "Add Fish", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _addFishBtn.Click += AddFish_Click;
        _removeFishBtn = new Button { Text = "Remove Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _removeFishBtn.Click += RemoveFish_Click;
        _exportFishBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportFishBtn.Click += (s, e) => ExportFishList();
        _importFishBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importFishBtn.Click += (s, e) => ImportFishList();
        fishButtonPanel.Controls.Add(_addFishBtn);
        fishButtonPanel.Controls.Add(_removeFishBtn);
        fishButtonPanel.Controls.Add(_exportFishBtn);
        fishButtonPanel.Controls.Add(_importFishBtn);
        fishLayout.Controls.Add(fishButtonPanel, 0, 2);

        fishTab.Controls.Add(fishLayout);
        _tabControl.TabPages.Add(fishTab);

        Controls.Add(_tabControl);
        ResumeLayout(false);
        PerformLayout();
    }

    // Tab 1: Known Technologies
    private DoubleBufferedTabControl _tabControl = null!;
    private DataGridView _techGrid = null!;
    private Button _addTechButton = null!;
    private Button _removeTechButton = null!;
    private TextBox _techFilterBox = null!;
    private Button _techFilterClearButton = null!;

    // Tab 2: Known Products
    private DataGridView _productGrid = null!;
    private Button _addProductButton = null!;
    private Button _removeProductButton = null!;
    private TextBox _productFilterBox = null!;
    private Button _productFilterClearButton = null!;

    // Tab 3: Known Specials (Quicksilver/SpecialShop)
    private TabPage _specialsTabPage = null!;
    private DataGridView _specialsGrid = null!;
    private Button _addSpecialsButton = null!;
    private Button _removeSpecialsButton = null!;
    private TextBox _specialsFilterBox = null!;
    private Button _specialsFilterClearButton = null!;
    private Button _exportSpecialsBtn = null!;
    private Button _importSpecialsBtn = null!;

    // Tab 4: Known Words
    private DataGridView _wordGrid = null!;
    private Button _learnAllWordsButton = null!;
    private Button _unlearnAllWordsButton = null!;
    private Button _learnSelectedWordsButton = null!;
    private Button _unlearnSelectedWordsButton = null!;
    private Button[] _raceLearnButtons = null!;
    private Button[] _raceUnlearnButtons = null!;
    private TextBox _wordFilterBox = null!;
    private PictureBox[] _raceIcons = null!;
    private Label[] _raceLabels = null!;

    // Tab 4: Known Glyphs
    private CheckBox[] _glyphCheckBoxes = null!;
    private PictureBox[] _glyphIcons = null!;
    private Button _learnAllGlyphsButton = null!;
    private Button _unlearnAllGlyphsButton = null!;

    // Tab 5: Known Locations
    private DataGridView _locationsGrid = null!;
    private Button _deleteLocationBtn = null!;
    private Button _travelToBtn = null!;
    private TextBox _locFilterBox = null!;
    private FlowLayoutPanel _locGlyphPanel = null!;
    private Label _portalGlyphsCaptionLabel = null!;
    private Label _galaxyCaptionLabel = null!;
    private Label _locGalaxyLabel = null!;
    private Label _locGalaxyCoreCaptionLabel = null!;
    private Label _locGalaxyDot = null!;

    // Tab 6: Known Fish
    private DataGridView _fishGrid = null!;
    private TextBox _fishFilterBox = null!;
    private Button _fishFilterClearBtn = null!;
    private Button _addFishBtn = null!;
    private Button _removeFishBtn = null!;

    // Export/Import buttons
    private Button _exportTechBtn = null!;
    private Button _importTechBtn = null!;
    private Button _exportProductBtn = null!;
    private Button _importProductBtn = null!;
    private Button _exportWordsBtn = null!;
    private Button _importWordsBtn = null!;
    private Button _exportGlyphsBtn = null!;
    private Button _importGlyphsBtn = null!;
    private Button _exportLocBtn = null!;
    private Button _importLocBtn = null!;
    private Button _exportFishBtn = null!;
    private Button _importFishBtn = null!;

    // Recipe tab
    private TabPage _recipeTab;
}
