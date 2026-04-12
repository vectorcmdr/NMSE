using NMSE.Data;
using NMSE.Models;
using NMSE.Core;

namespace NMSE.UI.Panels;

public partial class CataloguePanel : UserControl
{
    /// <summary>Raised when discovery data is modified by the user.</summary>
    public event EventHandler? DataModified;

    private void RaiseDataModified() => DataModified?.Invoke(this, EventArgs.Empty);

    private static readonly Bitmap PlaceholderIcon = new(24, 24);

    private readonly Dictionary<string, Image> _scaledIconCache = new(StringComparer.OrdinalIgnoreCase);
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private WordDatabase? _wordDatabase;

    // Reference to save data's KnownWordGroups for word state operations
    private JsonArray? _knownWordGroups;

    private JsonObject? _fishingRecord;

    private static readonly (string Name, int Index)[] RaceColumns = CatalogueLogic.RaceColumns;

    private static readonly (string Prefix, int RaceIndex)[] RacePrefixes = CatalogueLogic.RacePrefixes;

    private const int TotalRaceCount = CatalogueLogic.TotalRaceCount;

    private static readonly Dictionary<string, string> LocationTypeLocKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Base"] = "location_type.base",
        ["Spacestation"] = "location_type.spacestation",
        ["Atlas"] = "location_type.atlas",
        ["PlanetAwayFromShip"] = "location_type.planet_away_from_ship",
        ["ExternalBase"] = "location_type.external_base",
        ["EmergencyGalaxyFix"] = "location_type.emergency_galaxy_fix",
        ["OnNexus"] = "location_type.on_nexus",
        ["SpacestationFixPosition"] = "location_type.spacestation_fix_position",
        ["Settlement"] = "location_type.settlement",
        ["Freighter"] = "location_type.freighter",
        ["Frigate"] = "location_type.frigate",
        ["BaseBuildingObject"] = "location_type.base_building_object",
    };

    private static string GetLocalisedLocationType(string rawType)
    {
        if (string.IsNullOrEmpty(rawType)) return rawType;
        return LocationTypeLocKeys.TryGetValue(rawType, out var locKey)
            ? UiStrings.Get(locKey)
            : rawType;
    }

    public CataloguePanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    /// <summary>
    /// Adds the given RecipePanel as a sub-tab within this Discoveries panel.
    /// </summary>
    public void AddRecipeTab(RecipePanel recipePanel)
    {
        _recipeTab = new TabPage("Recipes");
        recipePanel.Dock = DockStyle.Fill;
        _recipeTab.Controls.Add(recipePanel);
        _tabControl.TabPages.Add(_recipeTab);
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
    }

    public void SetWordDatabase(WordDatabase? wordDatabase)
    {
        _wordDatabase = wordDatabase;
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _iconManager = iconManager;
        LoadGlyphIcons();
        LoadRaceIcons();
    }

    private void LoadRaceIcons()
    {
        if (_iconManager == null || _raceIcons.Length == 0) return;
        string[] raceIconFiles = { "UI-GEK.PNG", "UI-VYKEEN.PNG", "UI-KORVAX.PNG", "UI-ATLAS.PNG", "UI-KORVAX.PNG" };
        for (int i = 0; i < _raceIcons.Length && i < raceIconFiles.Length; i++)
        {
            var icon = _iconManager.GetIcon(raceIconFiles[i]);
            if (icon != null)
                _raceIcons[i].Image = icon;
        }
    }

    /// <summary>
    /// Align race icons and labels over their corresponding DataGridView column headers.
    /// </summary>
    private void AlignRaceIcons()
    {
        if (_wordGrid == null || _raceIcons.Length == 0) return;

        // Column order: Word, IndvWordId, Gek, Vy'keen, Korvax, Atlas, Autophage
        // Race columns start at index 2
        for (int i = 0; i < _raceIcons.Length && i < _raceLabels.Length; i++)
        {
            int colIdx = i + 2; // Skip Word and IndvWordId columns
            if (colIdx >= _wordGrid.Columns.Count) break;

            var rect = _wordGrid.GetColumnDisplayRectangle(colIdx, true);
            if (rect.Width == 0) continue;

            int centerX = _wordGrid.Left + rect.Left + (rect.Width / 2);

            // Position icon centered above column
            _raceIcons[i].Left = centerX - _raceIcons[i].Width / 2;
            _raceIcons[i].Top = 2;

            // Position label below icon, also centered
            _raceLabels[i].Left = centerX - _raceLabels[i].Width / 2;
            _raceLabels[i].Top = 36;

            // Position learn/unlearn buttons side by side below label
            if (_raceLearnButtons != null && i < _raceLearnButtons.Length)
            {
                int btnPairWidth = _raceLearnButtons[i].Width + _raceUnlearnButtons[i].Width + 2;
                int btnLeft = centerX - btnPairWidth / 2;
                _raceLearnButtons[i].Left = btnLeft;
                _raceLearnButtons[i].Top = 55;
                _raceUnlearnButtons[i].Left = btnLeft + _raceLearnButtons[i].Width + 2;
                _raceUnlearnButtons[i].Top = 55;
            }
        }
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        try
        {
        _savedSaveData = saveData;
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        LoadKnownItems(playerState, "KnownTech", _techGrid);
        LoadKnownItems(playerState, "KnownProducts", _productGrid);
        LoadKnownItems(playerState, "KnownSpecials", _specialsGrid);
        LoadKnownWords(playerState);
        LoadKnownGlyphs(playerState);
        LoadKnownLocations(playerState);
        LoadKnownFish(playerState);
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        SaveKnownItems(playerState, "KnownTech", _techGrid);
        SaveKnownItems(playerState, "KnownProducts", _productGrid);
        SaveKnownItems(playerState, "KnownSpecials", _specialsGrid);
        SaveKnownWords(playerState);
        SaveKnownGlyphs(playerState);
        SaveKnownFish(playerState);

        // Sync word stat counters to match current KnownWordGroups (required by game)
        if (_knownWordGroups != null)
            CatalogueLogic.SyncWordStats(saveData, _knownWordGroups);
    }

    // --- Shared helpers ---

    private static DataGridView CreateItemGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false,
            ReadOnly = true,
        };
        var iconCol = new DataGridViewImageColumn
        {
            Name = "Icon",
            HeaderText = "⚙️",
            Width = 30,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        };
        grid.Columns.Add(iconCol);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID" });
        grid.RowTemplate.Height = 28;
        return grid;
    }

    private void LoadKnownItems(JsonObject playerState, string arrayName, DataGridView grid)
    {
        grid.SuspendLayout();
        try
        {
        grid.Rows.Clear();
        var ids = CatalogueLogic.LoadKnownItemIds(playerState, arrayName);

        var rows = new DataGridViewRow[ids.Count];
        for (int i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            var dbItem = _database?.GetItem(id);
            string name = dbItem?.Name ?? id;
            string category = dbItem?.ItemType ?? "";
            Image? icon = GetScaledIcon(id);

            var row = new DataGridViewRow();
            row.CreateCells(grid, icon ?? (object)PlaceholderIcon, name, category, id);
            rows[i] = row;
        }
        grid.Rows.AddRange(rows);
        }
        finally
        {
            grid.ResumeLayout(true);
        }
    }

    private static void SaveKnownItems(JsonObject playerState, string arrayName, DataGridView grid)
    {
        var ids = new List<string>();
        foreach (DataGridViewRow row in grid.Rows)
            ids.Add(row.Cells["ID"].Value as string ?? "");

        CatalogueLogic.SaveKnownItemIds(playerState, arrayName, ids);
    }

    private Image? GetScaledIcon(string itemId)
    {
        if (_iconManager == null) return null;
        if (_scaledIconCache.TryGetValue(itemId, out var cached)) return cached;

        var icon = _iconManager.GetIconForItem(itemId, _database);
        if (icon == null) return null;

        try
        {
            var scaled = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(icon, 0, 0, 24, 24);
            }
            _scaledIconCache[itemId] = scaled;
            return scaled;
        }
        catch
        {
            return null;
        }
    }

    private static void RemoveSelectedFromGrid(DataGridView grid)
    {
        if (grid.SelectedRows.Count == 0) return;
        var indices = new List<int>();
        foreach (DataGridViewRow row in grid.SelectedRows)
            indices.Add(row.Index);
        indices.Sort();
        for (int i = indices.Count - 1; i >= 0; i--)
            grid.Rows.RemoveAt(indices[i]);
    }

    // --- Tab 1: Known Technologies events ---

    private static readonly HashSet<string> TechItemTypes = CatalogueLogic.TechItemTypes;

    private static readonly HashSet<string> ProductItemTypes = CatalogueLogic.ProductItemTypes;

    private void AddTech_Click(object? sender, EventArgs e)
    {
        if (_database == null) return;

        List<(Image? icon, string name, string id, string category)>? unknownTechs = null;

        // Show loading dialog while building item list and resolving icons
        using var loadingDialog = CreateLoadingDialog("Loading Technologies...");
        loadingDialog.Shown += (s, ev) =>
        {
            var knownIds = new HashSet<string>(
                _techGrid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells["ID"].Value as string ?? ""),
                StringComparer.OrdinalIgnoreCase);

            var items = _database.Items.Values
                .Where(item => TechItemTypes.Contains(item.ItemType)
                            && !knownIds.Contains(item.Id)
                            && !GameItemDatabase.IsPickerExcluded(item.Id)
                            && CatalogueLogic.IsLearnableTechnology(item))
                .OrderBy(item => item.Name)
                .ToList();

            unknownTechs = new List<(Image? icon, string name, string id, string category)>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                unknownTechs.Add((GetScaledIcon(item.Id) ?? (Image)PlaceholderIcon, item.Name, item.Id, item.ItemType));
                if (i % 50 == 0) Application.DoEvents();
            }

            loadingDialog.Close();
        };
        loadingDialog.ShowDialog(this);

        if (unknownTechs == null || unknownTechs.Count == 0) return;

        using var picker = new ItemPickerDialog("Add Technology", unknownTechs);
        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedIds.Count > 0)
        {
            foreach (var selectedId in picker.SelectedIds)
            {
                var item = _database.GetItem(selectedId);
                if (item != null)
                {
                    _techGrid.Rows.Add(GetScaledIcon(item.Id) ?? (object)PlaceholderIcon, item.Name, item.Subtitle, item.Id);
                }
            }
            RaiseDataModified();
        }
    }
    private void RemoveTech_Click(object? sender, EventArgs e) { RemoveSelectedFromGrid(_techGrid); RaiseDataModified(); }

    // --- Tab 2: Known Products events ---

    private void AddProduct_Click(object? sender, EventArgs e)
    {
        if (_database == null) return;

        List<(Image? icon, string name, string id, string category)>? unknownProducts = null;

        // Show loading dialog while building item list and resolving icons
        using var loadingDialog = CreateLoadingDialog("Loading Products...");
        loadingDialog.Shown += (s, ev) =>
        {
            var knownIds = new HashSet<string>(
                _productGrid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells["ID"].Value as string ?? ""),
                StringComparer.OrdinalIgnoreCase);

            var items = _database.Items.Values
                .Where(item => ProductItemTypes.Contains(item.ItemType)
                            && !knownIds.Contains(item.Id)
                            && !GameItemDatabase.IsPickerExcluded(item.Id)
                            && CatalogueLogic.IsLearnableProduct(item))
                .OrderBy(item => item.Name)
                .ToList();

            unknownProducts = new List<(Image? icon, string name, string id, string category)>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                unknownProducts.Add((GetScaledIcon(item.Id) ?? (Image)PlaceholderIcon, item.Name, item.Id, item.ItemType));
                if (i % 50 == 0) Application.DoEvents();
            }

            loadingDialog.Close();
        };
        loadingDialog.ShowDialog(this);

        if (unknownProducts == null || unknownProducts.Count == 0) return;

        using var picker = new ItemPickerDialog("Add Product", unknownProducts);
        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedIds.Count > 0)
        {
            foreach (var selectedId in picker.SelectedIds)
            {
                var item = _database.GetItem(selectedId);
                if (item != null)
                {
                    _productGrid.Rows.Add(GetScaledIcon(item.Id) ?? (object)PlaceholderIcon, item.Name, item.Subtitle, item.Id);
                }
            }
            RaiseDataModified();
        }
    }
    private void RemoveProduct_Click(object? sender, EventArgs e) { RemoveSelectedFromGrid(_productGrid); RaiseDataModified(); }

    // --- Tab 3: Known Specials (Quicksilver/SpecialShop) events ---

    private void AddSpecials_Click(object? sender, EventArgs e)
    {
        if (_database == null) return;

        List<(Image? icon, string name, string id, string category)>? unknownSpecials = null;

        using var loadingDialog = CreateLoadingDialog(UiStrings.Get("discovery.loading_specials"));
        loadingDialog.Shown += (s, ev) =>
        {
            var knownIds = new HashSet<string>(
                _specialsGrid.Rows.Cast<DataGridViewRow>().Select(r => r.Cells["ID"].Value as string ?? ""),
                StringComparer.OrdinalIgnoreCase);

            // SpecialShop items are products with TradeCategory == "SpecialShop"
            var items = _database.Items.Values
                .Where(item => string.Equals(item.TradeCategory, "SpecialShop", StringComparison.OrdinalIgnoreCase)
                            && !knownIds.Contains(item.Id)
                            && !GameItemDatabase.IsPickerExcluded(item.Id))
                .OrderBy(item => item.Name)
                .ToList();

            unknownSpecials = new List<(Image? icon, string name, string id, string category)>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                unknownSpecials.Add((GetScaledIcon(item.Id) ?? (Image)PlaceholderIcon, item.Name, item.Id, item.ItemType));
                if (i % 50 == 0) Application.DoEvents();
            }

            loadingDialog.Close();
        };
        loadingDialog.ShowDialog(this);

        if (unknownSpecials == null || unknownSpecials.Count == 0) return;

        using var picker = new ItemPickerDialog(UiStrings.Get("discovery.add_special"), unknownSpecials);
        if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedIds.Count > 0)
        {
            foreach (var selectedId in picker.SelectedIds)
            {
                var item = _database.GetItem(selectedId);
                if (item != null)
                {
                    _specialsGrid.Rows.Add(GetScaledIcon(item.Id) ?? (object)PlaceholderIcon, item.Name, item.Subtitle, item.Id);
                }
            }
            RaiseDataModified();
        }
    }

    private void RemoveSpecials_Click(object? sender, EventArgs e) { RemoveSelectedFromGrid(_specialsGrid); RaiseDataModified(); }

    // --- Tab 4: Known Words ---

    private bool IsWordKnown(string groupName, int raceOrdinal)
    {
        if (_knownWordGroups == null) return false;
        return CatalogueLogic.IsWordKnown(_knownWordGroups, groupName, raceOrdinal);
    }

    private void SetWordKnown(string groupName, int raceOrdinal, bool known)
    {
        if (_knownWordGroups == null) return;
        CatalogueLogic.SetWordKnown(_knownWordGroups, groupName, raceOrdinal, known);
    }

    private void LoadKnownWords(JsonObject playerState)
    {
        _wordGrid.SuspendLayout();
        try
        {
        _wordGrid.Rows.Clear();
        _wordGrid.CellValueChanged -= WordGrid_CellValueChanged;

        _knownWordGroups = playerState.GetArray("KnownWordGroups");
        if (_knownWordGroups == null)
        {
            _knownWordGroups = new JsonArray();
            playerState.Set("KnownWordGroups", _knownWordGroups);
        }

        if (_wordDatabase == null || _wordDatabase.Words.Count == 0) return;

        // Pre-create all rows then add in batch for performance
        var wordList = _wordDatabase.Words;
        var rows = new DataGridViewRow[wordList.Count];
        for (int w = 0; w < wordList.Count; w++)
        {
            var word = wordList[w];
            var rowValues = new object[2 + RaceColumns.Length];
            rowValues[0] = word.Text;
            rowValues[1] = word.Id;
            for (int c = 0; c < RaceColumns.Length; c++)
            {
                int raceOrdinal = RaceColumns[c].Index;
                string? groupForRace = word.GetGroupForRace(raceOrdinal);
                bool known = groupForRace != null && IsWordKnown(groupForRace, raceOrdinal);
                rowValues[2 + c] = known;
            }
            var row = new DataGridViewRow();
            row.CreateCells(_wordGrid, rowValues);
            row.Tag = word;
            rows[w] = row;
        }
        _wordGrid.Rows.AddRange(rows);

        // Apply per-cell styling after batch insert
        for (int w = 0; w < wordList.Count; w++)
        {
            var word = wordList[w];
            var row = _wordGrid.Rows[w];
            for (int c = 0; c < RaceColumns.Length; c++)
            {
                int raceOrdinal = RaceColumns[c].Index;
                bool hasGroup = word.HasRace(raceOrdinal);
                row.Cells[RaceColumns[c].Name].ReadOnly = !hasGroup;
                if (!hasGroup)
                {
                    row.Cells[RaceColumns[c].Name].Style.BackColor = Color.FromArgb(240, 240, 240);
                    row.Cells[RaceColumns[c].Name].Style.ForeColor = Color.LightGray;
                }
            }
        }

        _wordGrid.CellValueChanged += WordGrid_CellValueChanged;
        }
        finally
        {
            _wordGrid.ResumeLayout(true);
        }
    }

    /// <summary>
    /// When a race checkbox is toggled, immediately update KnownWordGroups in the save data.
    /// Changes are written immediately.
    /// </summary>
    private void WordGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 2) return;

        var row = _wordGrid.Rows[e.RowIndex];
        if (row.Tag is not WordEntry word) return;

        int colOffset = e.ColumnIndex - 2;
        if (colOffset < 0 || colOffset >= RaceColumns.Length) return;

        int raceOrdinal = RaceColumns[colOffset].Index;
        string? groupName = word.GetGroupForRace(raceOrdinal);
        if (groupName == null) return;

        bool value = row.Cells[e.ColumnIndex].Value is true;
        SetWordKnown(groupName, raceOrdinal, value);
        RaiseDataModified();
    }

    private void SaveKnownWords(JsonObject playerState)
    {
        // Changes are written immediately via WordGrid_CellValueChanged,
        // but ensure the reference is set
        if (_knownWordGroups != null)
            playerState.Set("KnownWordGroups", _knownWordGroups);
    }

    private void SetAllWordFlags(bool value)
    {
        _wordGrid.CellValueChanged -= WordGrid_CellValueChanged;
        try
        {
            foreach (DataGridViewRow row in _wordGrid.Rows)
            {
                if (row.Tag is not WordEntry word) continue;
                for (int c = 0; c < RaceColumns.Length; c++)
                {
                    int raceOrdinal = RaceColumns[c].Index;
                    string? groupName = word.GetGroupForRace(raceOrdinal);
                    if (groupName != null)
                    {
                        row.Cells[RaceColumns[c].Name].Value = value;
                        SetWordKnown(groupName, raceOrdinal, value);
                    }
                }
            }
        }
        finally
        {
            _wordGrid.CellValueChanged += WordGrid_CellValueChanged;
        }
        RaiseDataModified();
    }

    private void LearnAllWords_Click(object? sender, EventArgs e) => SetAllWordFlags(true);
    private void UnlearnAllWords_Click(object? sender, EventArgs e) => SetAllWordFlags(false);

    /// <summary>
    /// Learns all words for a specific race, updating both UI and save data.
    /// </summary>
    private void LearnAllForRace(int raceOrdinal) => SetWordFlagsForRace(raceOrdinal, true);

    /// <summary>
    /// Unlearns all words for a specific race, updating both UI and save data.
    /// </summary>
    private void UnlearnAllForRace(int raceOrdinal) => SetWordFlagsForRace(raceOrdinal, false);

    /// <summary>
    /// Sets the known state for all words belonging to a specific race.
    /// Updates the grid checkboxes and the save data.
    /// </summary>
    private void SetWordFlagsForRace(int raceOrdinal, bool value)
    {
        if (_knownWordGroups == null || _wordDatabase == null) return;

        _wordGrid.CellValueChanged -= WordGrid_CellValueChanged;
        try
        {
            // Find which column index corresponds to this race ordinal
            int colOffset = -1;
            for (int c = 0; c < RaceColumns.Length; c++)
            {
                if (RaceColumns[c].Index == raceOrdinal) { colOffset = c; break; }
            }
            if (colOffset < 0) return;

            // Update save data in bulk
            CatalogueLogic.SetWordFlagsForRace(_knownWordGroups, _wordDatabase.Words, raceOrdinal, value);

            // Update grid checkboxes
            string colName = RaceColumns[colOffset].Name;
            foreach (DataGridViewRow row in _wordGrid.Rows)
            {
                if (row.Tag is not WordEntry word) continue;
                string? groupName = word.GetGroupForRace(raceOrdinal);
                if (groupName != null)
                    row.Cells[colName].Value = value;
            }
        }
        finally
        {
            _wordGrid.CellValueChanged += WordGrid_CellValueChanged;
        }
        RaiseDataModified();
    }

    /// <summary>
    /// Learns all words for the currently selected rows across all races.
    /// </summary>
    private void LearnSelectedWords_Click(object? sender, EventArgs e) => SetSelectedWordFlags(true);

    /// <summary>
    /// Unlearns all words for the currently selected rows across all races.
    /// </summary>
    private void UnlearnSelectedWords_Click(object? sender, EventArgs e) => SetSelectedWordFlags(false);

    /// <summary>
    /// Sets the known state for words in the currently selected grid rows,
    /// across all races that each word supports.
    /// </summary>
    private void SetSelectedWordFlags(bool value)
    {
        if (_knownWordGroups == null) return;
        if (_wordGrid.SelectedRows.Count == 0) return;

        _wordGrid.CellValueChanged -= WordGrid_CellValueChanged;
        try
        {
            // Collect WordEntry objects from selected rows
            var selectedWords = new List<WordEntry>();
            foreach (DataGridViewRow row in _wordGrid.SelectedRows)
            {
                if (row.Tag is WordEntry word)
                    selectedWords.Add(word);
            }

            if (selectedWords.Count == 0) return;

            // Update save data in bulk
            CatalogueLogic.SetWordFlagsForEntries(_knownWordGroups, selectedWords, RaceColumns, value);

            // Update grid checkboxes for selected rows
            foreach (DataGridViewRow row in _wordGrid.SelectedRows)
            {
                if (row.Tag is not WordEntry word) continue;
                for (int c = 0; c < RaceColumns.Length; c++)
                {
                    int raceOrdinal = RaceColumns[c].Index;
                    string? groupName = word.GetGroupForRace(raceOrdinal);
                    if (groupName != null)
                        row.Cells[RaceColumns[c].Name].Value = value;
                }
            }
        }
        finally
        {
            _wordGrid.CellValueChanged += WordGrid_CellValueChanged;
        }
        RaiseDataModified();
    }

    // --- Tab 4: Known Glyphs ---

    private void LoadGlyphIcons()
    {
        if (_iconManager == null) return;
        for (int i = 0; i < 16; i++)
        {
            string filename = $"UI-GLYPH{i + 1}.PNG";
            var icon = _iconManager.GetIcon(filename);
            if (icon != null)
            {
                // Dispose previous image to prevent GDI resource leaks
                _glyphIcons[i].Image?.Dispose();

                // Draw glyph icon on dark grey circle background for visibility
                int size = 64;
                var composite = new Bitmap(size, size);
                using (var g = Graphics.FromImage(composite))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    // Draw dark grey filled circle
                    using var brush = new SolidBrush(Color.FromArgb(60, 60, 60));
                    g.FillEllipse(brush, 0, 0, size - 1, size - 1);
                    // Draw the glyph icon centered on the circle
                    int iconSize = 48;
                    int offset = (size - iconSize) / 2;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(icon, offset, offset, iconSize, iconSize);
                }
                _glyphIcons[i].Image = composite;
                _glyphIcons[i].Size = new Size(size, size);
            }
        }
    }

    private void LoadKnownGlyphs(JsonObject playerState)
    {
        int runesBitfield = CatalogueLogic.LoadGlyphBitfield(playerState);

        for (int i = 0; i < 16; i++)
        {
            int mask = 1 << i;
            _glyphCheckBoxes[i].Checked = (runesBitfield & mask) == mask;
        }
    }

    private void SaveKnownGlyphs(JsonObject playerState)
    {
        int runesBitfield = 0;
        for (int i = 0; i < 16; i++)
        {
            if (_glyphCheckBoxes[i].Checked)
                runesBitfield |= (1 << i);
        }
        CatalogueLogic.SaveGlyphBitfield(playerState, runesBitfield);
    }

    private void SetAllGlyphs(bool value)
    {
        for (int i = 0; i < 16; i++)
            _glyphCheckBoxes[i].Checked = value;
        RaiseDataModified();
    }

    private void LearnAllGlyphs_Click(object? sender, EventArgs e) => SetAllGlyphs(true);
    private void UnlearnAllGlyphs_Click(object? sender, EventArgs e) => SetAllGlyphs(false);

    // --- Filtering ---

    private static void ApplyFilter(DataGridView grid, string filterText)
    {
        var filter = filterText.Trim();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (string.IsNullOrEmpty(filter))
            {
                row.Visible = true;
                continue;
            }
            string name = row.Cells["Name"].Value as string ?? "";
            string category = row.Cells["Category"].Value as string ?? "";
            string id = row.Cells["ID"].Value as string ?? "";
            row.Visible = name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || category.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || id.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Form CreateLoadingDialog(string message)
    {
        var dialog = new Form
        {
            Text = message,
            Size = new Size(300, 100),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ControlBox = false,
        };
        var progress = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
        };
        dialog.Controls.Add(progress);
        return dialog;
    }

    // --- Tab 5: Known Locations ---

    private JsonArray? _teleportEndpoints;
    private JsonObject? _savedPlayerState;
    private JsonObject? _savedSaveData;

    private void LoadKnownLocations(JsonObject playerState)
    {
        _locationsGrid.SuspendLayout();
        try
        {
        _locationsGrid.Rows.Clear();
        _teleportEndpoints = null;
        _savedPlayerState = playerState;
        try
        {
            _teleportEndpoints = playerState.GetArray("TeleportEndpoints");
            if (_teleportEndpoints == null) return;

            var rowList = new List<DataGridViewRow>(_teleportEndpoints.Length);
            for (int i = 0; i < _teleportEndpoints.Length; i++)
            {
                try
                {
                    var endpoint = _teleportEndpoints.GetObject(i);
                    string name = endpoint.GetString("Name") ?? "";
                    string type = "";
                    try { type = endpoint.GetString("TeleporterType") ?? ""; } catch { }

                    string portalCode = "";
                    string signalBooster = "";
                    string galaxyName = "";
                    int realityIndex = 0;
                    try
                    {
                        var addr = endpoint.GetObject("UniverseAddress");
                        if (addr != null)
                        {
                            try { realityIndex = addr.GetInt("RealityIndex"); } catch { }
                            galaxyName = GalaxyDatabase.GetGalaxyDisplayName(realityIndex);

                            var gal = addr.GetObject("GalacticAddress");
                            if (gal != null)
                            {
                                int vx = gal.GetInt("VoxelX");
                                int vy = gal.GetInt("VoxelY");
                                int vz = gal.GetInt("VoxelZ");
                                int si = gal.GetInt("SolarSystemIndex");
                                int pi = 0;
                                try { pi = gal.GetInt("PlanetIndex"); } catch { }
                                portalCode = CoordinateHelper.VoxelToPortalCode(vx, vy, vz, si, pi);
                                signalBooster = CoordinateHelper.VoxelToSignalBooster(vx, vy, vz, si);
                            }
                        }
                    }
                    catch { }

                    var row = new DataGridViewRow();
                    row.CreateCells(_locationsGrid, i, name, GetLocalisedLocationType(type), galaxyName, portalCode, CoordinateHelper.PortalHexToDec(portalCode), signalBooster);
                    // Store raw type in the Type cell's Tag for re-localisation
                    row.Cells[2].Tag = type;
                    // Store reality index in the Galaxy cell's Tag for color painting
                    row.Cells[3].Tag = realityIndex;
                    rowList.Add(row);
                }
                catch { }
            }
            _locationsGrid.Rows.AddRange(rowList.ToArray());
        }
        catch { }
        }
        finally
        {
            _locationsGrid.ResumeLayout(true);
        }
    }

    private void OnLocationSelectionChanged(object? sender, EventArgs e)
    {
        if (_locationsGrid.SelectedRows.Count == 0 || _teleportEndpoints == null)
        {
            CoordinateHelper.UpdateGlyphPanel(_locGlyphPanel, "");
            _locGalaxyLabel.Text = "";
            _locGalaxyDot.Text = "";
            return;
        }

        int rowIdx = _locationsGrid.SelectedRows[0].Index;
        string portalCode = _locationsGrid.Rows[rowIdx].Cells["PortalCode"].Value?.ToString() ?? "";
        string galaxy = _locationsGrid.Rows[rowIdx].Cells["Galaxy"].Value?.ToString() ?? "";
        CoordinateHelper.UpdateGlyphPanel(_locGlyphPanel, portalCode);
        _locGalaxyLabel.Text = galaxy;

        int realityIndex = _locationsGrid.Rows[rowIdx].Cells["Galaxy"].Tag is int ri ? ri : 0;
        string galaxyType = GalaxyDatabase.GetGalaxyType(realityIndex);
        _locGalaxyDot.Text = string.IsNullOrEmpty(galaxy) ? "" : " \u25CF";
        _locGalaxyDot.ForeColor = GalaxyDatabase.GetGalaxyTypeColor(galaxyType);
    }

    /// <summary>
    /// Custom paint handler for the Galaxy column to append a colored ● indicator
    /// representing the galaxy type (Normal=blue, Lush=green, Harsh=red, Empty=cyan).
    /// </summary>
    private void OnLocationGalaxyCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_locationsGrid.Columns[e.ColumnIndex].Name != "Galaxy") return;

        e.PaintBackground(e.ClipBounds, e.State.HasFlag(DataGridViewElementStates.Selected));
        string text = e.Value?.ToString() ?? "";
        if (!string.IsNullOrEmpty(text) && e.Graphics != null)
        {
            int realityIndex = _locationsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag is int ri ? ri : 0;
            string galaxyType = GalaxyDatabase.GetGalaxyType(realityIndex);
            Color dotColor = GalaxyDatabase.GetGalaxyTypeColor(galaxyType);

            var font = e.CellStyle?.Font ?? _locationsGrid.DefaultCellStyle.Font ?? _locationsGrid.Font;
            var textColor = e.State.HasFlag(DataGridViewElementStates.Selected)
                ? (e.CellStyle?.SelectionForeColor ?? SystemColors.HighlightText)
                : (e.CellStyle?.ForeColor ?? SystemColors.ControlText);

            using var textBrush = new SolidBrush(textColor);
            using var dotBrush = new SolidBrush(dotColor);
            using var sf = new StringFormat { LineAlignment = StringAlignment.Center };

            var textSize = e.Graphics.MeasureString(text + " ", font);
            var rect = e.CellBounds;
            rect.X += 2;
            e.Graphics.DrawString(text + " ", font, textBrush, rect.X, rect.Y + (rect.Height - textSize.Height) / 2);
            e.Graphics.DrawString("\u25CF", font, dotBrush, rect.X + textSize.Width - 2, rect.Y + (rect.Height - textSize.Height) / 2);
        }
        e.Handled = true;
    }

    private void DeleteLocation_Click(object? sender, EventArgs e)
    {
        if (_teleportEndpoints == null || _locationsGrid.SelectedRows.Count == 0) return;

        int count = _locationsGrid.SelectedRows.Count;
        string msg = count == 1
            ? UiStrings.Get("discovery.delete_location_single")
            : UiStrings.Format("discovery.delete_location_multi", count);
        var result = MessageBox.Show(this, msg, UiStrings.Get("discovery.delete_location_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        // Collect indices and sort descending to avoid index shifting during deletion
        var indices = new List<int>();
        foreach (DataGridViewRow row in _locationsGrid.SelectedRows)
        {
            if (row.Index >= 0 && row.Index < _teleportEndpoints.Length)
                indices.Add(row.Index);
        }
        indices.Sort();
        indices.Reverse();

        foreach (int idx in indices)
        {
            _teleportEndpoints.RemoveAt(idx);
            _locationsGrid.Rows.RemoveAt(idx);
        }

        // Re-index remaining rows
        for (int i = 0; i < _locationsGrid.Rows.Count; i++)
            _locationsGrid.Rows[i].Cells[0].Value = i;
        RaiseDataModified();
    }

    private void TravelToSystem_Click(object? sender, EventArgs e)
    {
        if (_teleportEndpoints == null || _savedPlayerState == null || _locationsGrid.SelectedRows.Count == 0) return;

        int rowIdx = _locationsGrid.SelectedRows[0].Index;
        if (rowIdx < 0 || rowIdx >= _teleportEndpoints.Length) return;

        var result = MessageBox.Show(this, UiStrings.Get("discovery.travel_confirm"),
            UiStrings.Get("discovery.travel_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            var endpoint = _teleportEndpoints.GetObject(rowIdx);
            var endpointAddr = endpoint.GetObject("UniverseAddress");
            if (endpointAddr == null) return;

            // Get target coordinates
            int targetRealityIndex = endpointAddr.GetInt("RealityIndex");
            var targetGal = endpointAddr.GetObject("GalacticAddress");
            if (targetGal == null) return;

            int targetVoxelX = targetGal.GetInt("VoxelX");
            int targetVoxelY = targetGal.GetInt("VoxelY");
            int targetVoxelZ = targetGal.GetInt("VoxelZ");
            int targetSystemIdx = targetGal.GetInt("SolarSystemIndex");

            // Update player UniverseAddress
            var playerAddr = _savedPlayerState.GetObject("UniverseAddress");
            if (playerAddr != null)
            {
                playerAddr.Set("RealityIndex", targetRealityIndex);
                var playerGal = playerAddr.GetObject("GalacticAddress");
                if (playerGal != null)
                {
                    playerGal.Set("VoxelX", targetVoxelX);
                    playerGal.Set("VoxelY", targetVoxelY);
                    playerGal.Set("VoxelZ", targetVoxelZ);
                    playerGal.Set("SolarSystemIndex", targetSystemIdx);
                    playerGal.Set("PlanetIndex", 0); // System-level travel; reset to first planet
                }
            }

            // Update SpawnStateData so the game respawns the player at the new location
            if (_savedSaveData != null)
            {
                try
                {
                    var spawnState = _savedSaveData.GetObject("SpawnStateData");
                    if (spawnState != null)
                    {
                        spawnState.Set("LastKnownPlayerState", "InShip");
                    }
                }
                catch { }
            }

            MessageBox.Show(this, UiStrings.Get("discovery.travel_complete"),
                UiStrings.Get("discovery.travel_complete_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            RaiseDataModified();
        }
        catch
        {
            MessageBox.Show(this, UiStrings.Get("discovery.travel_failed"), UiStrings.Get("common.error"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // --- Tab 6: Known Fish ---

    private void LoadKnownFish(JsonObject playerState)
    {
        _fishGrid.SuspendLayout();
        try
        {
        _fishGrid.Rows.Clear();
        _fishingRecord = null;
        try
        {
            _fishingRecord = playerState.GetObject("FishingRecord");
            if (_fishingRecord == null) return;

            var productList = _fishingRecord.GetArray("ProductList");
            var countList = _fishingRecord.GetArray("ProductCountList");
            var largestList = _fishingRecord.GetArray("LargestCatchList");
            if (productList == null) return;

            int count = productList.Length;
            var rowList = new List<DataGridViewRow>(count);
            for (int i = 0; i < count; i++)
            {
                string productId = productList.GetString(i) ?? "";
                if (string.IsNullOrEmpty(productId) || productId == "^") continue;

                int catchCount = 0;
                double largestCatch = 0;
                if (countList != null && i < countList.Length)
                    try { catchCount = countList.GetInt(i); } catch { }
                if (largestList != null && i < largestList.Length)
                    try { largestCatch = largestList.GetDouble(i); } catch { }

                // Strip "^" prefix for database lookup
                string lookupId = productId.StartsWith('^') ? productId[1..] : productId;
                var dbItem = string.IsNullOrEmpty(lookupId) ? null : _database?.GetItem(lookupId);
                string name = dbItem?.NameLower ?? dbItem?.Name ?? lookupId;
                Image? icon = GetScaledIcon(lookupId);

                var row = new DataGridViewRow();
                row.CreateCells(_fishGrid, icon ?? (object)PlaceholderIcon, productId, name,
                    catchCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    largestCatch.ToString(System.Globalization.CultureInfo.InvariantCulture));
                row.Tag = i; // store array index for saving
                rowList.Add(row);
            }
            _fishGrid.Rows.AddRange(rowList.ToArray());
        }
        catch { }
        }
        finally
        {
            _fishGrid.ResumeLayout(true);
        }
    }

    private void SaveKnownFish(JsonObject playerState)
    {
        try
        {
            var fishingRecord = playerState.GetObject("FishingRecord");
            if (fishingRecord == null) return;

            var productList = fishingRecord.GetArray("ProductList");
            var countList = fishingRecord.GetArray("ProductCountList");
            var largestList = fishingRecord.GetArray("LargestCatchList");
            if (productList == null) return;

            foreach (DataGridViewRow row in _fishGrid.Rows)
            {
                if (row.Tag is not int idx) continue;
                if (idx < 0 || idx >= productList.Length) continue;

                string productId = row.Cells["CaughtFish"].Value?.ToString() ?? "";
                productList.Set(idx, productId);

                if (countList != null && idx < countList.Length)
                {
                    if (int.TryParse(row.Cells["Count"].Value?.ToString(),
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out int c))
                        countList.Set(idx, c);
                }
                if (largestList != null && idx < largestList.Length)
                {
                    if (double.TryParse(row.Cells["LargestCatch"].Value?.ToString(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double lc))
                        largestList.Set(idx, lc);
                }
            }
        }
        catch { }
    }

    private void AddFish_Click(object? sender, EventArgs e)
    {
        if (_fishingRecord == null || _database == null) return;

        var productList = _fishingRecord.GetArray("ProductList");
        if (productList == null) return;

        // Find first empty slot (value "^")
        int emptySlot = -1;
        for (int i = 0; i < productList.Length; i++)
        {
            try
            {
                string val = productList.GetString(i) ?? "";
                if (string.IsNullOrEmpty(val) || val == "^") { emptySlot = i; break; }
            }
            catch { emptySlot = i; break; }
        }
        if (emptySlot < 0)
        {
            MessageBox.Show(this, UiStrings.Get("discovery.no_fish_slots"), UiStrings.Get("discovery.add_fish_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Build list of Fish-category items
        var items = _database.Items.Values
            .Where(item => string.Equals(item.ItemType, "Fish", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name)
            .Select(item =>
            {
                Image? icon = GetScaledIcon(item.Id);
                return (icon: (Image?)(icon ?? (Image)PlaceholderIcon), name: item.Name, id: item.Id, category: item.Subtitle ?? "Fish");
            })
            .ToList();

        using var picker = new ItemPickerDialog("Add Fish", items);
        if (picker.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(picker.SelectedId))
        {
            string newId = picker.SelectedId!;
            // Save format uses "^" prefix
            string saveId = newId.StartsWith('^') ? newId : "^" + newId;

            productList.Set(emptySlot, saveId);
            var countList = _fishingRecord.GetArray("ProductCountList");
            if (countList != null && emptySlot < countList.Length)
                countList.Set(emptySlot, 0);
            var largestList = _fishingRecord.GetArray("LargestCatchList");
            if (largestList != null && emptySlot < largestList.Length)
                largestList.Set(emptySlot, 0.0);

            // Add row
            string lookupId = saveId.StartsWith('^') ? saveId[1..] : saveId;
            var dbItem = _database.GetItem(lookupId);
            string name = dbItem?.NameLower ?? dbItem?.Name ?? lookupId;
            Image? fishIcon = GetScaledIcon(lookupId);
            _fishGrid.Rows.Add(fishIcon ?? (object)PlaceholderIcon, saveId, name, 0, 0.0);
            _fishGrid.Rows[^1].Tag = emptySlot;
            RaiseDataModified();
        }
    }

    private void RemoveFish_Click(object? sender, EventArgs e)
    {
        if (_fishingRecord == null || _fishGrid.SelectedRows.Count == 0) return;

        var row = _fishGrid.SelectedRows[0];
        if (row.Tag is not int idx) return;

        var productList = _fishingRecord.GetArray("ProductList");
        if (productList == null || idx >= productList.Length) return;

        productList.Set(idx, "^");
        var countList = _fishingRecord.GetArray("ProductCountList");
        if (countList != null && idx < countList.Length)
            countList.Set(idx, 0);
        var largestList = _fishingRecord.GetArray("LargestCatchList");
        if (largestList != null && idx < largestList.Length)
            largestList.Set(idx, 0.0);

        _fishGrid.Rows.Remove(row);
        RaiseDataModified();
    }

    private void ApplyFishFilter()
    {
        string filter = _fishFilterBox.Text.Trim();
        foreach (DataGridViewRow row in _fishGrid.Rows)
        {
            if (string.IsNullOrEmpty(filter))
            {
                row.Visible = true;
                continue;
            }
            string caughtFish = row.Cells["CaughtFish"].Value?.ToString() ?? "";
            string name = row.Cells["Name"].Value?.ToString() ?? "";
            row.Visible = caughtFish.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || name.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void OnFishCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        var colName = _fishGrid.Columns[e.ColumnIndex].Name;
        if (colName == "Count")
        {
            if (!int.TryParse(e.FormattedValue?.ToString(),
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                e.Cancel = true;
        }
        else if (colName == "LargestCatch")
        {
            if (!double.TryParse(e.FormattedValue?.ToString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out _))
                e.Cancel = true;
        }
    }

    // --- Filters ---

    private void ApplyWordFilter()
    {
        string filter = _wordFilterBox.Text.Trim();
        foreach (DataGridViewRow row in _wordGrid.Rows)
        {
            if (string.IsNullOrEmpty(filter))
            {
                row.Visible = true;
                continue;
            }
            string word = row.Cells["Word"].Value?.ToString() ?? "";
            string wordId = row.Cells["IndvWordId"].Value?.ToString() ?? "";
            row.Visible = word.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || wordId.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ApplyLocationFilter()
    {
        string filter = _locFilterBox.Text.Trim();
        foreach (DataGridViewRow row in _locationsGrid.Rows)
        {
            if (string.IsNullOrEmpty(filter))
            {
                row.Visible = true;
                continue;
            }
            string name = row.Cells["Name"].Value?.ToString() ?? "";
            string type = row.Cells["Type"].Value?.ToString() ?? "";
            string galaxy = row.Cells["Galaxy"].Value?.ToString() ?? "";
            string portalCode = row.Cells["PortalCode"].Value?.ToString() ?? "";
            row.Visible = name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || type.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || galaxy.Contains(filter, StringComparison.OrdinalIgnoreCase)
                       || portalCode.Contains(filter, StringComparison.OrdinalIgnoreCase);
        }
    }

    // --- Export/Import helpers ---

    private void ExportDiscoveryList(string title, DataGridView grid, string idColumnName)
    {
        var config = ExportConfig.Instance;
        var strippedName = title.StartsWith("Known ", StringComparison.OrdinalIgnoreCase)
            ? title["Known ".Length..]
            : title;
        var vars = new Dictionary<string, string> { ["name"] = strippedName.Replace(" ", "_") };
        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.DiscoveryExt, "Discovery files"),
            DefaultExt = config.DiscoveryExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.DiscoveryTemplate, config.DiscoveryExt, vars)
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var ids = new List<string>();
            foreach (DataGridViewRow row in grid.Rows)
                ids.Add(row.Cells[idColumnName].Value?.ToString() ?? "");

            var root = new JsonObject();
            var arr = new JsonArray();
            foreach (var id in ids)
                arr.Add(id);
            root.Set(title.Replace(" ", ""), arr);
            root.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportItemList(DataGridView grid, string arrayName)
    {
        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildOpenFilter(config.DiscoveryExt, "Discovery files")
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);
            // Find the first array value in the imported JSON
            JsonArray? arr = null;
            foreach (var name in imported.Names())
            {
                try { arr = imported.GetArray(name); break; } catch { }
            }
            if (arr == null || arr.Length == 0)
            {
                MessageBox.Show(this, UiStrings.Get("discovery.import_no_items"), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int added = 0;
            var existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in grid.Rows)
                existingIds.Add(row.Cells["ID"].Value?.ToString() ?? "");

            for (int i = 0; i < arr.Length; i++)
            {
                string id = arr.GetString(i) ?? "";
                if (string.IsNullOrEmpty(id) || existingIds.Contains(id)) continue;
                existingIds.Add(id);

                var dbItem = _database?.GetItem(id);
                string name = dbItem?.Name ?? id;
                string category = dbItem?.ItemType ?? "";
                Image? icon = GetScaledIcon(id);
                grid.Rows.Add(icon ?? (object)PlaceholderIcon, name, category, id);
                added++;
            }

            MessageBox.Show(this, UiStrings.Format("discovery.import_success_items", added), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            RaiseDataModified();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportWordsList()
    {
        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildOpenFilter(config.DiscoveryExt, "Discovery files")
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);
            JsonArray? arr = null;
            foreach (var name in imported.Names())
            {
                try { arr = imported.GetArray(name); break; } catch { }
            }
            if (arr == null || arr.Length == 0)
            {
                MessageBox.Show(this, UiStrings.Get("discovery.import_no_words"), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Import format: each entry is a word ID. Set all race columns to true for imported words.
            int added = 0;
            var existingWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _wordGrid.Rows)
                existingWords.Add(row.Cells["Word"].Value?.ToString() ?? "");

            for (int i = 0; i < arr.Length; i++)
            {
                string wordId = arr.GetString(i) ?? "";
                if (string.IsNullOrEmpty(wordId) || existingWords.Contains(wordId)) continue;

                var checkValues = new object[RaceColumns.Length];
                for (int j = 0; j < checkValues.Length; j++)
                    checkValues[j] = true;
                var cells = new object[2 + checkValues.Length];
                cells[0] = wordId;
                cells[1] = wordId;
                Array.Copy(checkValues, 0, cells, 2, checkValues.Length);
                _wordGrid.Rows.Add(cells);
                existingWords.Add(wordId);
                added++;
            }

            MessageBox.Show(this, UiStrings.Format("discovery.import_words_success", added), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            RaiseDataModified();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportGlyphsList()
    {
        var config = ExportConfig.Instance;
        var vars = new Dictionary<string, string> { ["name"] = "Glyphs" };
        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.DiscoveryExt, "Discovery files"),
            DefaultExt = config.DiscoveryExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.DiscoveryTemplate, config.DiscoveryExt, vars)
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var root = new JsonObject();
            var arr = new JsonArray();
            for (int i = 0; i < 16; i++)
                arr.Add(_glyphCheckBoxes[i].Checked);
            root.Set("KnownGlyphs", arr);
            root.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportGlyphsList()
    {
        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildOpenFilter(config.DiscoveryExt, "Discovery files")
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);
            JsonArray? arr = null;
            foreach (var name in imported.Names())
            {
                try { arr = imported.GetArray(name); break; } catch { }
            }
            if (arr == null) return;

            for (int i = 0; i < 16 && i < arr.Length; i++)
            {
                try { _glyphCheckBoxes[i].Checked = arr.GetBool(i); } catch { }
            }
            RaiseDataModified();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportLocationsList()
    {
        if (_teleportEndpoints == null) return;

        var config = ExportConfig.Instance;
        var vars = new Dictionary<string, string> { ["name"] = "Locations" };
        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.DiscoveryExt, "Discovery files"),
            DefaultExt = config.DiscoveryExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.DiscoveryTemplate, config.DiscoveryExt, vars)
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var root = new JsonObject();
            root.Set("TeleportEndpoints", _teleportEndpoints);
            root.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportLocationsList()
    {
        if (_teleportEndpoints == null || _savedPlayerState == null) return;

        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildOpenFilter(config.DiscoveryExt, "Discovery files")
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);
            var arr = imported.GetArray("TeleportEndpoints");
            if (arr == null || arr.Length == 0)
            {
                MessageBox.Show(this, UiStrings.Get("discovery.import_no_locations"), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            for (int i = 0; i < arr.Length; i++)
            {
                try
                {
                    var endpoint = arr.GetObject(i);
                    _teleportEndpoints.Add(endpoint);
                }
                catch { }
            }

            // Refresh the grid
            LoadKnownLocations(_savedPlayerState);
            MessageBox.Show(this, UiStrings.Format("discovery.import_locations_success", arr.Length), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            RaiseDataModified();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportFishList()
    {
        if (_fishingRecord == null) return;

        var config = ExportConfig.Instance;
        var vars = new Dictionary<string, string> { ["name"] = "Fish" };
        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.DiscoveryExt, "Discovery files"),
            DefaultExt = config.DiscoveryExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.DiscoveryTemplate, config.DiscoveryExt, vars)
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var root = new JsonObject();
            root.Set("FishingRecord", _fishingRecord);
            root.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportFishList()
    {
        if (_fishingRecord == null || _savedPlayerState == null) return;

        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildOpenFilter(config.DiscoveryExt, "Discovery files")
        };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);
            var record = imported.GetObject("FishingRecord");
            if (record == null)
            {
                MessageBox.Show(this, UiStrings.Get("discovery.import_no_fish"), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Overwrite the entire fishing record
            foreach (var name in record.Names())
                _fishingRecord.Set(name, record.Get(name));

            // Refresh
            LoadKnownFish(_savedPlayerState);
            MessageBox.Show(this, UiStrings.Get("discovery.import_fish_success"), UiStrings.Get("discovery.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            RaiseDataModified();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("discovery.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ApplyUiLocalisation()
    {
        // Tab pages
        if (_tabControl.TabPages.Count >= 7)
        {
            _tabControl.TabPages[0].Text = UiStrings.Get("discovery.tab_tech");
            _tabControl.TabPages[1].Text = UiStrings.Get("discovery.tab_products");
            _tabControl.TabPages[2].Text = UiStrings.Get("discovery.tab_specials");
            _tabControl.TabPages[3].Text = UiStrings.Get("discovery.tab_words");
            _tabControl.TabPages[4].Text = UiStrings.Get("discovery.tab_glyphs");
            _tabControl.TabPages[5].Text = UiStrings.Get("discovery.tab_locations");
            _tabControl.TabPages[6].Text = UiStrings.Get("discovery.tab_fish");
        }

        // Buttons
        _addTechButton.Text = UiStrings.Get("discovery.add_technology");
        _removeTechButton.Text = UiStrings.Get("discovery.remove_selected");
        _addProductButton.Text = UiStrings.Get("discovery.add_product");
        _removeProductButton.Text = UiStrings.Get("discovery.remove_selected");
        _addSpecialsButton.Text = UiStrings.Get("discovery.add_special");
        _removeSpecialsButton.Text = UiStrings.Get("discovery.remove_selected");
        _learnAllWordsButton.Text = UiStrings.Get("discovery.learn_all");
        _unlearnAllWordsButton.Text = UiStrings.Get("discovery.unlearn_all");
        _learnSelectedWordsButton.Text = UiStrings.Get("discovery.learn_selected");
        _unlearnSelectedWordsButton.Text = UiStrings.Get("discovery.unlearn_selected");
        _learnAllGlyphsButton.Text = UiStrings.Get("discovery.learn_all");
        _unlearnAllGlyphsButton.Text = UiStrings.Get("discovery.unlearn_all");

        // Glyph labels
        for (int i = 0; i < 16; i++)
            _glyphCheckBoxes[i].Text = UiStrings.Format("discovery.glyph_n", i + 1);

        _deleteLocationBtn.Text = UiStrings.Get("discovery.delete_selected");
        _travelToBtn.Text = UiStrings.Get("discovery.travel_to_system");
        _addFishBtn.Text = UiStrings.Get("discovery.add_fish_title");
        _removeFishBtn.Text = UiStrings.Get("discovery.remove_selected");

        // Filter placeholders
        _techFilterBox.PlaceholderText = UiStrings.Get("discovery.filter_items");
        _productFilterBox.PlaceholderText = UiStrings.Get("discovery.filter_items");
        _specialsFilterBox.PlaceholderText = UiStrings.Get("discovery.filter_items");
        _wordFilterBox.PlaceholderText = UiStrings.Get("discovery.filter_words");
        _locFilterBox.PlaceholderText = UiStrings.Get("discovery.filter_locations");
        _fishFilterBox.PlaceholderText = UiStrings.Get("discovery.filter_fish");

        // Location detail caption labels
        _portalGlyphsCaptionLabel.Text = UiStrings.Get("discovery.portal_glyphs");
        _galaxyCaptionLabel.Text = UiStrings.Get("discovery.galaxy");

        // Tech grid columns
        if (_techGrid.Columns["Name"] is DataGridViewColumn tName) tName.HeaderText = UiStrings.Get("discovery.col_name");
        if (_techGrid.Columns["Category"] is DataGridViewColumn tCat) tCat.HeaderText = UiStrings.Get("discovery.col_category");
        if (_techGrid.Columns["ID"] is DataGridViewColumn tId) tId.HeaderText = UiStrings.Get("discovery.col_id");

        // Product grid columns
        if (_productGrid.Columns["Name"] is DataGridViewColumn pName) pName.HeaderText = UiStrings.Get("discovery.col_name");
        if (_productGrid.Columns["Category"] is DataGridViewColumn pCat) pCat.HeaderText = UiStrings.Get("discovery.col_category");
        if (_productGrid.Columns["ID"] is DataGridViewColumn pId) pId.HeaderText = UiStrings.Get("discovery.col_id");

        // Specials grid columns
        if (_specialsGrid.Columns["Name"] is DataGridViewColumn sName) sName.HeaderText = UiStrings.Get("discovery.col_name");
        if (_specialsGrid.Columns["Category"] is DataGridViewColumn sCat) sCat.HeaderText = UiStrings.Get("discovery.col_category");
        if (_specialsGrid.Columns["ID"] is DataGridViewColumn sId) sId.HeaderText = UiStrings.Get("discovery.col_id");

        // Word grid columns
        if (_wordGrid.Columns["Word"] is DataGridViewColumn wCol) wCol.HeaderText = UiStrings.Get("discovery.col_word");
        if (_wordGrid.Columns["IndvWordId"] is DataGridViewColumn wIdCol) wIdCol.HeaderText = UiStrings.Get("discovery.col_word_id");

        // Location grid columns
        if (_locationsGrid.Columns["Index"] is DataGridViewColumn lIdx) lIdx.HeaderText = UiStrings.Get("discovery.col_index");
        if (_locationsGrid.Columns["Name"] is DataGridViewColumn lName) lName.HeaderText = UiStrings.Get("discovery.col_name");
        if (_locationsGrid.Columns["Type"] is DataGridViewColumn lType) lType.HeaderText = UiStrings.Get("discovery.col_type");
        if (_locationsGrid.Columns["Galaxy"] is DataGridViewColumn lGal) lGal.HeaderText = UiStrings.Get("discovery.col_galaxy");
        if (_locationsGrid.Columns["PortalCode"] is DataGridViewColumn lPC) lPC.HeaderText = UiStrings.Get("discovery.col_portal_hex");
        if (_locationsGrid.Columns["PortalCodeDec"] is DataGridViewColumn lPD) lPD.HeaderText = UiStrings.Get("discovery.col_portal_dec");
        if (_locationsGrid.Columns["SignalBooster"] is DataGridViewColumn lSB) lSB.HeaderText = UiStrings.Get("discovery.col_signal_booster");

        // Fish grid columns
        if (_fishGrid.Columns["CaughtFish"] is DataGridViewColumn fCaught) fCaught.HeaderText = UiStrings.Get("discovery.col_caught_fish");
        if (_fishGrid.Columns["Name"] is DataGridViewColumn fName) fName.HeaderText = UiStrings.Get("discovery.col_name");
        if (_fishGrid.Columns["Count"] is DataGridViewColumn fCount) fCount.HeaderText = UiStrings.Get("discovery.col_count");
        if (_fishGrid.Columns["LargestCatch"] is DataGridViewColumn fLargest) fLargest.HeaderText = UiStrings.Get("discovery.col_largest_catch");

        // Export/Import buttons
        _exportTechBtn.Text = UiStrings.Get("common.export");
        _importTechBtn.Text = UiStrings.Get("common.import");
        _exportProductBtn.Text = UiStrings.Get("common.export");
        _importProductBtn.Text = UiStrings.Get("common.import");
        _exportSpecialsBtn.Text = UiStrings.Get("common.export");
        _importSpecialsBtn.Text = UiStrings.Get("common.import");
        _exportWordsBtn.Text = UiStrings.Get("common.export");
        _importWordsBtn.Text = UiStrings.Get("common.import");
        _exportGlyphsBtn.Text = UiStrings.Get("common.export");
        _importGlyphsBtn.Text = UiStrings.Get("common.import");
        _exportLocBtn.Text = UiStrings.Get("common.export");
        _importLocBtn.Text = UiStrings.Get("common.import");
        _exportFishBtn.Text = UiStrings.Get("common.export");
        _importFishBtn.Text = UiStrings.Get("common.import");

        // Recipe tab
        if (_recipeTab != null) _recipeTab.Text = UiStrings.Get("discovery.tab_recipes");

        // Race labels in known words
        string[] raceLocKeys = { "common.race_gek", "common.race_vykeen", "common.race_korvax", "discovery.race_atlas", "discovery.race_autophage" };
        if (_raceLabels != null)
        {
            for (int i = 0; i < _raceLabels.Length && i < raceLocKeys.Length; i++)
                _raceLabels[i].Text = UiStrings.Get(raceLocKeys[i]);
        }

        // Per-race learn/unlearn button tooltips
        if (_raceLearnButtons != null && _raceUnlearnButtons != null)
        {
            for (int i = 0; i < _raceLearnButtons.Length && i < raceLocKeys.Length; i++)
            {
                string raceName = UiStrings.Get(raceLocKeys[i]);
                _raceLearnButtons[i].Tag = raceName;
                _raceUnlearnButtons[i].Tag = raceName;
            }
        }

        // Re-localise location type display values from stored raw Tag values
        foreach (DataGridViewRow row in _locationsGrid.Rows)
        {
            if (row.Cells.Count > 2 && row.Cells[2].Tag is string rawType)
                row.Cells[2].Value = GetLocalisedLocationType(rawType);
        }
    }
}
