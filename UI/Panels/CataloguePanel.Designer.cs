using NMSE.Core;
using NMSE.Data;
using NMSE.UI.Util;

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

        _tabControl = new DoubleBufferedTabControl { Dock = DockStyle.Fill };
        _tabControl.SuspendLayout();

        // --- Tab 1: Technologies ---
        _techPanel = new CatalogueItemsPanel(CatalogueItemKind.Technology) { Dock = DockStyle.Fill };
        var techTab = new TabPage(UiStrings.Get("discovery.tab_tech_items"));
        techTab.Controls.Add(_techPanel);
        _tabControl.TabPages.Add(techTab);

        // --- Tab 2: Products ---
        _productPanel = new CatalogueItemsPanel(CatalogueItemKind.Product) { Dock = DockStyle.Fill };
        var productTab = new TabPage(UiStrings.Get("discovery.tab_product_items"));
        productTab.Controls.Add(_productPanel);
        _tabControl.TabPages.Add(productTab);

        // --- Tab 3: Specials ---
        _specialsPanel = new CatalogueItemsPanel(CatalogueItemKind.Special) { Dock = DockStyle.Fill };
        var specialsTab = new TabPage(UiStrings.Get("discovery.tab_special_items"));
        specialsTab.Controls.Add(_specialsPanel);
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
        var wordFilterPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1 };
        wordFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        wordFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        wordFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        wordFilterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var wordFilterFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        _wordFilterBox = new TextBox { Width = 200, MinimumSize = new Size(0, 25), PlaceholderText = "Filter by word..." };
        _wordFilterBox.TextChanged += (s, e) => ApplyWordFilter();
        var wordFilterClearBtn = new Button { Text = "X", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(28, 25) };
        wordFilterClearBtn.Click += (s, e) => { _wordFilterBox.Text = ""; };
        wordFilterFlow.Controls.Add(_wordFilterBox);
        wordFilterFlow.Controls.Add(wordFilterClearBtn);
        wordFilterPanel.Controls.Add(wordFilterFlow, 0, 0);
        wordFilterPanel.Controls.Add(CreateGotoButton(["PlayerStateData", "KnownWordGroups"]), 2, 0);
        wordLayout.Controls.Add(wordFilterPanel, 0, 0);

        // Race icons header panel - uses absolute positioning to align over grid columns
        var raceIconPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            MinimumSize = new Size(0, 100),
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
            };
            FontManager.ApplyFont(_raceLabels[i], 9, FontStyle.Bold);
            int raceOrdinal = RaceColumns[i].Index;
            _raceLearnButtons[i] = new Button
            {
                Text = "\u2713",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(28, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 140, 60),
                ForeColor = Color.White,
                Font = FontManager.CreateFont(7),
            };
            _raceLearnButtons[i].FlatAppearance.BorderSize = 0;
            _raceLearnButtons[i].Click += (s, e) => LearnAllForRace(raceOrdinal);

            _raceUnlearnButtons[i] = new Button
            {
                Text = "\u2717",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(28, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(160, 60, 60),
                ForeColor = Color.White,
                Font = FontManager.CreateFont(7),
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
        _wordsCompletionLabel = CreateCompletionLabel();
        _addMissingWordsBtn = CreateAddMissingButton();
        _addMissingWordsBtn.Click += AddAllMissingWords_Click;
        wordButtonPanel.Controls.Add(_wordsCompletionLabel);
        wordButtonPanel.Controls.Add(_addMissingWordsBtn);
        _unlearnAllWordsButton = new Button { Text = "Unlearn All", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _unlearnAllWordsButton.Click += UnlearnAllWords_Click;
        _learnSelectedWordsButton = new Button { Text = "Learn Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _unlearnSelectedWordsButton = new Button { Text = "Unlearn Selected", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _learnSelectedWordsButton.Click += LearnSelectedWords_Click;
        _unlearnSelectedWordsButton.Click += UnlearnSelectedWords_Click;
        _exportWordsBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportWordsBtn.Click += (s, e) => ExportKnownWordsList();
        _importWordsBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importWordsBtn.Click += (s, e) => ImportWordsList();
        wordButtonPanel.Controls.Add(_unlearnAllWordsButton);
        wordButtonPanel.Controls.Add(_learnSelectedWordsButton);
        wordButtonPanel.Controls.Add(_unlearnSelectedWordsButton);
        wordButtonPanel.Controls.Add(_exportWordsBtn);
        wordButtonPanel.Controls.Add(_importWordsBtn);
        wordLayout.Controls.Add(wordButtonPanel, 0, 3);

        wordTab.Controls.Add(wordLayout);
        _tabControl.TabPages.Add(wordTab);

        // --- Tab 5: Known Glyphs ---
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
        _glyphsCompletionLabel = CreateCompletionLabel();
        glyphButtonPanel.Controls.Add(_glyphsCompletionLabel);
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

        glyphButtonPanel.Controls.Add(CreateGotoButton(["PlayerStateData", "KnownPortalRunes"]));
        glyphTab.Controls.Add(glyphLayout);
        _tabControl.TabPages.Add(glyphTab);

        // --- Tab 6: Known Locations ---
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
        var locFilterPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1 };
        locFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        locFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        locFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        locFilterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var locFilterFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        _locFilterBox = new TextBox { Width = 200, MinimumSize = new Size(0, 25), PlaceholderText = "Filter by name, portal code..." };
        _locFilterBox.TextChanged += (s, e) => ApplyLocationFilter();
        var locFilterClearBtn = new Button { Text = "X", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(28, 25) };
        locFilterClearBtn.Click += (s, e) => { _locFilterBox.Text = ""; };
        locFilterFlow.Controls.Add(_locFilterBox);
        locFilterFlow.Controls.Add(locFilterClearBtn);
        locFilterPanel.Controls.Add(locFilterFlow, 0, 0);
        locFilterPanel.Controls.Add(CreateGotoButton(["PlayerStateData", "TeleportEndpoints"]), 2, 0);
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

        // --- Tab 7: Known Fish ---
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

        var fishFilterPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1 };
        fishFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fishFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fishFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fishFilterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var fishFilterFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        _fishFilterBox = new TextBox { Width = 200, MinimumSize = new Size(0, 25), PlaceholderText = "Filter by name or ID..." };
        _fishFilterBox.TextChanged += (s, e) => ApplyFishFilter();
        _fishFilterClearBtn = new Button { Text = "X", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(28, 25) };
        _fishFilterClearBtn.Click += (s, e) => { _fishFilterBox.Text = ""; };
        fishFilterFlow.Controls.Add(_fishFilterBox);
        fishFilterFlow.Controls.Add(_fishFilterClearBtn);
        fishFilterPanel.Controls.Add(fishFilterFlow, 0, 0);
        fishFilterPanel.Controls.Add(CreateGotoButton(["PlayerStateData", "FishingRecord"]), 2, 0);
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
            HeaderText = "\U0001F41F",
            Width = 36,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
        });
        if (_fishGrid.Columns["Icon"] is DataGridViewColumn fishIconCol)
            fishIconCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
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
        _fishCompletionLabel = CreateCompletionLabel();
        _addMissingFishBtn = CreateAddMissingButton();
        _addMissingFishBtn.Click += AddAllMissingFish_Click;
        fishButtonPanel.Controls.Add(_fishCompletionLabel);
        fishButtonPanel.Controls.Add(_addMissingFishBtn);
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

        // --- Tab 8: Recipes (inner tabs) ---
        _recipeTab = new TabPage(UiStrings.Get("recipe.tab_recipes"));
        _recipeInnerTabs = new DoubleBufferedTabControl
        {
            Dock = DockStyle.Fill
        };

        // Inner Tab 0: Known Recipes
        _knownRecipesTab = new TabPage(UiStrings.Get("recipe.tab_known_recipes"));
        var knownRecipeLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        knownRecipeLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        knownRecipeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        knownRecipeLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var recipeFilterPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1 };
        recipeFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        recipeFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        recipeFilterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        recipeFilterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var recipeFilterFlow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        _recipeFilterBox = new TextBox { Width = 200, MinimumSize = new Size(0, 25), PlaceholderText = UiStrings.Get("recipe.filter_placeholder") };
        _recipeFilterBox.TextChanged += (s, e) => ApplyRecipeFilter();
        _recipeFilterClearBtn = new Button { Text = "X", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(28, 25) };
        _recipeFilterClearBtn.Click += (s, e) => { _recipeFilterBox.Text = ""; };
        recipeFilterFlow.Controls.Add(_recipeFilterBox);
        recipeFilterFlow.Controls.Add(_recipeFilterClearBtn);
        recipeFilterPanel.Controls.Add(recipeFilterFlow, 0, 0);
        recipeFilterPanel.Controls.Add(CreateGotoButton(["PlayerStateData", "KnownRefinerRecipes"]), 2, 0);
        knownRecipeLayout.Controls.Add(recipeFilterPanel, 0, 0);

        _recipeGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            ReadOnly = true,
            RowTemplate = { Height = 28 }
        };
        var recipeIconCol = new DataGridViewImageColumn
        {
            Name = "Icon",
            HeaderText = "\u2699\uFE0F",
            Width = 36,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        };
        recipeIconCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _recipeGrid.Columns.Add(recipeIconCol);
        _recipeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = UiStrings.Get("recipe.col_name"), FillWeight = 15 });
        _recipeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = UiStrings.Get("recipe.col_type"), FillWeight = 8 });
        var recipeResultIconCol = new DataGridViewImageColumn
        {
            Name = "ResultIcon",
            HeaderText = "\u2699\uFE0F",
            Width = 36,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        };
        recipeResultIconCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _recipeGrid.Columns.Add(recipeResultIconCol);
        _recipeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Result", HeaderText = UiStrings.Get("recipe.col_result"), FillWeight = 12 });
        var recipeIngIconCol = new DataGridViewImageColumn
        {
            Name = "IngredientsIcon",
            HeaderText = "\u2699\uFE0F",
            Width = 56,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        };
        recipeIngIconCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _recipeGrid.Columns.Add(recipeIngIconCol);
        _recipeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ingredients", HeaderText = UiStrings.Get("recipe.col_ingredients"), FillWeight = 22 });
        _recipeGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", HeaderText = UiStrings.Get("recipe.col_id"), FillWeight = 8 });
        knownRecipeLayout.Controls.Add(_recipeGrid, 0, 1);

        var recipeButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
        };
        _recipesCompletionLabel = CreateCompletionLabel();
        _addMissingRecipesBtn = CreateAddMissingButton();
        _addMissingRecipesBtn.Click += AddAllMissingRecipes_Click;
        recipeButtonPanel.Controls.Add(_recipesCompletionLabel);
        recipeButtonPanel.Controls.Add(_addMissingRecipesBtn);
        _addRecipeBtn = new Button { Text = UiStrings.Get("recipe.add_recipe"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _addRecipeBtn.Click += AddRecipe_Click;
        _removeRecipeBtn = new Button { Text = UiStrings.Get("recipe.remove_selected"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _removeRecipeBtn.Click += RemoveRecipe_Click;
        _exportRecipeBtn = new Button { Text = "Export", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _exportRecipeBtn.Click += (s, e) => ExportRecipeList();
        _importRecipeBtn = new Button { Text = "Import", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _importRecipeBtn.Click += (s, e) => ImportRecipeList();
        recipeButtonPanel.Controls.Add(_addRecipeBtn);
        recipeButtonPanel.Controls.Add(_removeRecipeBtn);
        recipeButtonPanel.Controls.Add(_exportRecipeBtn);
        recipeButtonPanel.Controls.Add(_importRecipeBtn);
        knownRecipeLayout.Controls.Add(recipeButtonPanel, 0, 2);

        _knownRecipesTab.Controls.Add(knownRecipeLayout);
        _recipeInnerTabs.TabPages.Add(_knownRecipesTab);

        // Inner Tab 1: Recipe Info
        _recipeInfoTab = new TabPage(UiStrings.Get("recipe.tab_recipe_info"));
        _recipeInnerTabs.TabPages.Add(_recipeInfoTab);

        _recipeTab.Controls.Add(_recipeInnerTabs);
        _tabControl.TabPages.Add(_recipeTab);

        // --- Tab 9: Wonders ---
        _wondersPanel = new WondersCompletionPanel { Dock = DockStyle.Fill };
        var wondersTab = new TabPage(UiStrings.Get("discovery.tab_wonders"));
        wondersTab.Controls.Add(_wondersPanel);
        _tabControl.TabPages.Add(wondersTab);

        // --- Tab 10: Collected Knowledge ---
        _knowledgePanel = new KnowledgeCompletionPanel { Dock = DockStyle.Fill };
        var knowledgeTab = new TabPage(UiStrings.Get("discovery.tab_knowledge"));
        knowledgeTab.Controls.Add(_knowledgePanel);
        _tabControl.TabPages.Add(knowledgeTab);

        // --- Tab 11: Fossils ---
        _fossilsPanel = new AccountCataloguePanel(fossils: true) { Dock = DockStyle.Fill };
        var fossilsTab = new TabPage(UiStrings.Get("discovery.group_fossils"));
        fossilsTab.Controls.Add(_fossilsPanel);
        _tabControl.TabPages.Add(fossilsTab);

        // --- Tab 12: Raw Materials ---
        _rawMaterialsPanel = new AccountCataloguePanel(fossils: false) { Dock = DockStyle.Fill };
        var rawMaterialsTab = new TabPage(UiStrings.Get("discovery.group_raw_materials"));
        rawMaterialsTab.Controls.Add(_rawMaterialsPanel);
        _tabControl.TabPages.Add(rawMaterialsTab);

        // --- Tab 13: Discovery Stats ---
        _discoveryStatsPanel = new DiscoveryStatsPanel { Dock = DockStyle.Fill };
        var statsTab = new TabPage(UiStrings.Get("discovery.tab_stats"));
        statsTab.Controls.Add(_discoveryStatsPanel);
        _tabControl.TabPages.Add(statsTab);


        _tabControl.ResumeLayout(false);
        _tabControl.PerformLayout();

        _curatedNameNoticeLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0),
            Font = FontManager.CreateFont(9, FontStyle.Italic),
        };

        _completeCatalogueButton = new Button
        {
            Text = "Complete Catalogue",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8, 2, 8, 2),
            Margin = new Padding(0, 0, 6, 0),
        };
        _completeCatalogueButton.Click += CompleteCatalogue_Click;

        var catalogueHeaderBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(6, 4, 6, 2),
        };
        catalogueHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        catalogueHeaderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        catalogueHeaderBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        catalogueHeaderBar.Controls.Add(_completeCatalogueButton, 0, 0);
        catalogueHeaderBar.Controls.Add(_curatedNameNoticeLabel, 1, 0);

        this.Controls.Add(_tabControl);
        this.Controls.Add(catalogueHeaderBar);

        //
        // CataloguePanel
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.DoubleBuffered = true;
        this.ResumeLayout(false);
        this.PerformLayout();
    }
    #endregion

    // Tabs 1-3: unified catalogue item lists
    private DoubleBufferedTabControl _tabControl = null!;
    private readonly List<Button> _gotoJsonBtns = [];
    private CatalogueItemsPanel _techPanel = null!;
    private CatalogueItemsPanel _productPanel = null!;
    private CatalogueItemsPanel _specialsPanel = null!;
    private Button _completeCatalogueButton = null!;

    // Tab 4: Known Words
    private DataGridView _wordGrid = null!;

    private Button _unlearnAllWordsButton = null!;
    private Button _learnSelectedWordsButton = null!;
    private Button _unlearnSelectedWordsButton = null!;
    private Button[] _raceLearnButtons = null!;
    private Button[] _raceUnlearnButtons = null!;
    private TextBox _wordFilterBox = null!;
    private PictureBox[] _raceIcons = null!;
    private Label[] _raceLabels = null!;

    // Tab 5: Known Glyphs
    private CheckBox[] _glyphCheckBoxes = null!;
    private PictureBox[] _glyphIcons = null!;
    private Button _learnAllGlyphsButton = null!;
    private Button _unlearnAllGlyphsButton = null!;

    // Tab 6: Known Locations
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

    // Tab 7: Known Fish
    private DataGridView _fishGrid = null!;
    private TextBox _fishFilterBox = null!;
    private Button _fishFilterClearBtn = null!;
    private Button _addFishBtn = null!;
    private Button _removeFishBtn = null!;

    // Export/Import buttons
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
    private DoubleBufferedTabControl _recipeInnerTabs = null!;
    private TabPage _knownRecipesTab = null!;
    private TabPage _recipeInfoTab = null!;
    private DataGridView _recipeGrid = null!;
    private TextBox _recipeFilterBox = null!;
    private Button _recipeFilterClearBtn = null!;
    private Button _addRecipeBtn = null!;
    private Button _removeRecipeBtn = null!;
    private Button _exportRecipeBtn = null!;
    private Button _importRecipeBtn = null!;

    // Catalogue completion controls (verified pack)
    private Label _curatedNameNoticeLabel = null!;
    private Label _wordsCompletionLabel = null!;
    private Button _addMissingWordsBtn = null!;
    private Label _glyphsCompletionLabel = null!;
    private Label _fishCompletionLabel = null!;
    private Button _addMissingFishBtn = null!;
    private Label _recipesCompletionLabel = null!;
    private Button _addMissingRecipesBtn = null!;

    // Part B completion sub-tabs
    private WondersCompletionPanel _wondersPanel = null!;
    private KnowledgeCompletionPanel _knowledgePanel = null!;
    private AccountCataloguePanel _fossilsPanel = null!;
    private AccountCataloguePanel _rawMaterialsPanel = null!;
    private DiscoveryStatsPanel _discoveryStatsPanel = null!;

    private static Label CreateCompletionLabel() => new()
    {
        AutoSize = true,
        Padding = new Padding(0, 8, 8, 0),
        Font = FontManager.CreateFont(9, FontStyle.Bold),
    };

    private static Button CreateAddMissingButton() => new()
    {
        Text = "Add All Missing",
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
    };

    private Button CreateGotoButton(string[] path)
    {
        var btn = new Button
        {
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderColor = ThemeManager.Effective == AppTheme.Dark ? Color.FromArgb(100, 100, 100) : SystemColors.ControlDark, BorderSize = 1 },
            Font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(28, 25),
            Text = "\U0001F4D1",
            Margin = new Padding(1, 3, 1, 1),
            Cursor = Cursors.Hand,
            Tag = path,
        };
        btn.Click += (s, e) =>
        {
            if (s is Button b && b.Tag is string[] p)
                GoToJsonRequested?.Invoke(b, new GoToJsonEventArgs(p));
        };

        _gotoJsonBtns.Add(btn);
        return btn;
    }
}
