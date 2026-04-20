using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Controls;

namespace NMSE.UI.Panels;

public partial class SettlementPanel : UserControl
{
    private static int[] StatMaxValues => SettlementLogic.StatMaxValues;
    private static string[] StatLabels => SettlementLogic.StatLabels;
    private const int StatCount = SettlementLogic.StatCount;
    private const int ProductionMaxAmount = SettlementLogic.ProductionMaxAmount;
    private const int PerkSlotCount = 18;

    // Building-state bit-field region size aliases (from SettlementBuildingState)
    private const int InitPhaseCount = SettlementLogic.SettlementBuildingState.InitPhaseCount;
    private const int UpgradePhaseCount = SettlementLogic.SettlementBuildingState.UpgradePhaseCount;
    private const int TierBitCount = SettlementLogic.SettlementBuildingState.TierBitCount;
    private const int FlagBitCount = SettlementLogic.SettlementBuildingState.FlagBitCount;

    // Filtered settlement data: indices into SettlementStatesV2
    private readonly List<int> _filteredIndices = new();
    private JsonArray? _settlements;
    private readonly Random _rng = new();
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private Dictionary<string, string>? _allowedProductionItems;

    /// <summary>Raw (unclamped) settlement stat values read from JSON for the currently-selected settlement.</summary>
    private int[]? _rawSettlementStats;
    /// <summary>Raw (unclamped) population value for preservation.</summary>
    private int? _rawPopulation;
    /// <summary>Whether the current settlement has a top-level Population key (NMS 5.70+ saves).</summary>
    private bool _hasPopulationKey;

    /// <summary>Raw (unclamped) production amounts read from JSON, keyed by row index.</summary>
    private readonly Dictionary<int, int> _rawProductionAmounts = new();

    private int[]? _rawBuildingStates;
    private bool _hasBuildingStates;
    private bool _editorUpdating;

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        _allowedProductionItems = database != null
            ? SettlementLogic.BuildAllowedProductionItems(database)
            : null;
    }
    public void SetIconManager(IconManager? iconManager) => _iconManager = iconManager;

    public SettlementPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    // --- Perk ComboBox support ---

    private sealed class PerkComboItem
    {
        public SettlementPerk? Perk { get; }
        public string DisplayText { get; }

        public PerkComboItem(SettlementPerk? perk)
        {
            Perk = perk;
            DisplayText = perk == null
                ? "(None)"
                : $"{perk.Name} ({perk.StatEffectSummary})";
        }

        public override string ToString() => DisplayText;
    }

    private sealed class RaceComboItem
    {
        public string InternalId { get; }
        public string DisplayText { get; }
        public RaceComboItem(string internalId, string displayText)
        {
            InternalId = internalId;
            DisplayText = displayText;
        }
        public override string ToString() => DisplayText;
    }

    private static void PopulatePerkCombo(ComboBox combo)
    {
        combo.Items.Add(new PerkComboItem(null));
        foreach (var perk in SettlementDatabase.Perks)
            combo.Items.Add(new PerkComboItem(perk));
        combo.SelectedIndex = 0;
    }

    /// <summary>
    /// Repopulates the perk combo boxes from SettlementDatabase.
    /// Must be called after LoadDatabase() since the panels are constructed
    /// before the JSON databases are loaded.
    /// </summary>
    public void RefreshPerkCombos()
    {
        if (_perkCombos == null) return;
        foreach (var combo in _perkCombos)
        {
            int prevIdx = combo.SelectedIndex;
            combo.Items.Clear();
            PopulatePerkCombo(combo);
            if (prevIdx >= 0 && prevIdx < combo.Items.Count)
                combo.SelectedIndex = prevIdx;
        }
    }

    private static void OnPerkComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var combo = (ComboBox)sender!;
        if (combo.Items[e.Index] is not PerkComboItem item) return;

        e.DrawBackground();
        Color textColor;
        if ((e.State & DrawItemState.Selected) != 0)
            textColor = SystemColors.HighlightText;
        else if (item.Perk == null)
            textColor = combo.ForeColor;
        else
            textColor = item.Perk.Beneficial ? Color.ForestGreen : Color.Crimson;

        var font = e.Font ?? combo.Font;
        using var brush = new SolidBrush(textColor);
        e.Graphics.DrawString(item.DisplayText, font, brush, e.Bounds);
        e.DrawFocusRectangle();
    }

    private void OnPerkChanged(int slot)
    {
        if (slot < 0 || slot >= PerkSlotCount) return;
        var item = _perkCombos[slot].SelectedItem as PerkComboItem;
        bool showSeed = item?.Perk?.Procedural == true;
        _perkSeedPanels[slot].Visible = showSeed;
        if (!showSeed)
            _perkSeedFields[slot].Text = "";
    }

    /// <summary>
    /// Generates a random integer seed for the specified perk slot.
    /// The seed is a non-negative 32-bit integer stored as a decimal string after the '#'.
    /// </summary>
    private void GeneratePerkSeed(int slot)
    {
        if (slot < 0 || slot >= PerkSlotCount) return;
        _perkSeedFields[slot].Text = _rng.Next(0, int.MaxValue)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void LoadPerkSlot(int slot, string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw == "^")
        {
            _perkCombos[slot].SelectedIndex = 0;
            return;
        }

        string perkId = raw;
        string seed = "";
        int hashIdx = raw.IndexOf('#');
        if (hashIdx >= 0)
        {
            perkId = raw[..hashIdx];
            seed = raw[(hashIdx + 1)..];
        }

        if (SettlementDatabase.ById.TryGetValue(perkId, out var perk))
        {
            for (int j = 0; j < _perkCombos[slot].Items.Count; j++)
            {
                if (_perkCombos[slot].Items[j] is PerkComboItem item && item.Perk?.Id == perk.Id)
                {
                    _perkCombos[slot].SelectedIndex = j;
                    break;
                }
            }

            if (perk.Procedural && !string.IsNullOrEmpty(seed))
            {
                _perkSeedPanels[slot].Visible = true;
                _perkSeedFields[slot].Text = seed;
            }
        }
        else
        {
            _perkCombos[slot].SelectedIndex = 0;
        }
    }

    // --- Delete Settlement ---

    private void OnDeleteSettlement(object? sender, EventArgs e)
    {
        if (_settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;

        var result = MessageBox.Show(this, UiStrings.Get("settlement.delete_confirm"),
            UiStrings.Get("settlement.delete_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        int selIdx = _settlementSelector.SelectedIndex;
        int dataIdx = _filteredIndices[selIdx];
        if (dataIdx >= _settlements.Length) return;

        // Remove the settlement entry from the array
        SettlementLogic.RemoveSettlement(_settlements, dataIdx);

        // Adjust all filtered indices that were after the removed entry
        _filteredIndices.RemoveAt(selIdx);
        for (int i = 0; i < _filteredIndices.Count; i++)
        {
            if (_filteredIndices[i] > dataIdx)
                _filteredIndices[i]--;
        }

        _settlementSelector.Items.RemoveAt(selIdx);

        if (_settlementSelector.Items.Count > 0)
            _settlementSelector.SelectedIndex = Math.Min(selIdx, _settlementSelector.Items.Count - 1);
        else
            ClearFields();
    }

    // --- Load / Save / Clear ---

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _settlementSelector.BeginUpdate();
        try
        {
        _settlementSelector.Items.Clear();
        _filteredIndices.Clear();
        ClearFields();
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _settlements = playerState.GetArray("SettlementStatesV2");
            if (_settlements == null || _settlements.Length == 0)
            {
                _infoLabel.Text = UiStrings.Get("settlement.no_settlements");
                return;
            }

            var filtered = SettlementLogic.FilterSettlements(saveData, playerState, _settlements);
            foreach (int i in filtered)
            {
                try
                {
                    _filteredIndices.Add(i);
                    var settlement = _settlements.GetObject(i);
                    string name = settlement.GetString("Name") ?? UiStrings.Format("settlement.fallback_name", i + 1);
                    _settlementSelector.Items.Add($"[{i}] {name}");
                }
                catch
                {
                    _filteredIndices.Add(i);
                    _settlementSelector.Items.Add($"[{i}] {UiStrings.Format("settlement.fallback_name", i + 1)}");
                }
            }

            if (_settlementSelector.Items.Count > 0)
                _settlementSelector.SelectedIndex = 0;

            _infoLabel.Text = UiStrings.Format("settlement.found_count", _filteredIndices.Count);
        }
        catch { _infoLabel.Text = UiStrings.Get("settlement.load_failed"); }
        }
        finally
        {
            _settlementSelector.EndUpdate();
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var settlements = playerState.GetArray("SettlementStatesV2");
            if (settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;

            int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
            if (dataIdx >= settlements.Length) return;

            var settlement = settlements.GetObject(dataIdx);

            var saveValues = new SettlementLogic.SettlementSaveValues
            {
                Name = _settlementName.Text,
                SeedValue = _seedField.Text,
                DecisionTypeIndex = _decisionTypeField.SelectedIndex,
                LastDecisionTime = _lastDecisionTimeField.Checked ? _lastDecisionTimeField.Value : (DateTime?)null,
                RawStats = _rawSettlementStats,
                HasPopulationKey = _hasPopulationKey,
                AlienRace = GetSelectedRace(),
                LastBugAttackChangeTime = ReadTimestamp(_lastBugAttackTimeField),
                LastAlertChangeTime = ReadTimestamp(_lastAlertTimeField),
                LastDebtChangeTime = ReadTimestamp(_lastDebtTimeField),
                LastUpkeepDebtCheckTime = ReadTimestamp(_lastUpkeepTimeField),
                LastPopulationChangeTime = ReadTimestamp(_lastPopulationTimeField),
                MiniMissionStartTime = ReadTimestamp(_miniMissionStartTimeField),
                HasBuildingStates = _hasBuildingStates,
                RawBuildingStates = _rawBuildingStates,
            };
            if (_hasPopulationKey)
            {
                saveValues.Population = (int)(_populationField.NumericValue ?? 0);
                saveValues.RawPopulation = _rawPopulation;
            }
            for (int i = 0; i < StatCount; i++)
                saveValues.Stats[i] = (int)(_statFields[i].NumericValue ?? 0);

            // Mission seed
            if (int.TryParse(_missionSeedField.Text.Trim(), System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int missionSeed))
                saveValues.MiniMissionSeed = missionSeed;

            // Building states
            for (int i = 0; i < SettlementLogic.BuildingStateSlotCount; i++)
            {
                saveValues.BuildingStates[i] = (int)(_buildingStateNuds[i].NumericValue ?? 0);
            }

            SettlementLogic.SaveSettlementData(settlement, saveValues);

            // Save perks - grow the JSON array if it has fewer than PerkSlotCount entries
            var perksArr = settlement.GetArray("Perks");
            if (perksArr == null)
            {
                perksArr = new Models.JsonArray();
                settlement.Set("Perks", perksArr);
            }
            for (int i = 0; i < PerkSlotCount; i++)
            {
                var item = _perkCombos[i].SelectedItem as PerkComboItem;
                string val;
                if (item?.Perk == null)
                {
                    val = "^";
                }
                else if (item.Perk.Procedural && !string.IsNullOrEmpty(_perkSeedFields[i].Text))
                {
                    // Validate seed is a valid integer
                    string seedText = _perkSeedFields[i].Text.Trim();
                    if (int.TryParse(seedText, System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out _))
                    {
                        val = $"{item.Perk.Id}#{seedText}";
                    }
                    else
                    {
                        // Invalid seed - save perk without seed
                        val = item.Perk.Id;
                    }
                }
                else
                {
                    val = item.Perk.Id;
                }

                if (i < perksArr.Length)
                    perksArr.Set(i, val);
                else
                    perksArr.Add(val);
            }
            // Trim trailing empty "^" entries back to save file cleanliness
            while (perksArr.Length > 0 && (perksArr.GetString(perksArr.Length - 1) ?? "") == "^")
                perksArr.RemoveAt(perksArr.Length - 1);

            // Save production state – preserve raw values outside 0..ProductionMaxAmount when unchanged
            var prodArr = settlement.GetArray("ProductionState");
            if (prodArr != null)
            {
                for (int i = 0; i < _productionGrid.Rows.Count && i < prodArr.Length; i++)
                {
                    var prodObj = prodArr.GetObject(i);
                    var elementId = _productionGrid.Rows[i].Cells["ElementId"].Value?.ToString() ?? "";
                    prodObj.Set("ElementId", elementId);

                    if (int.TryParse(_productionGrid.Rows[i].Cells["Amount"].Value?.ToString(),
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture, out int amount))
                    {
                        // If the displayed value still matches what we loaded from JSON,
                        // preserve the original raw value (which may be outside 0..ProductionMaxAmount)
                        if (_rawProductionAmounts.TryGetValue(i, out int raw) && amount == raw)
                        {
                            RawNumberGuard.SetInt(prodObj, "Amount", raw);
                            continue;
                        }
                        amount = Math.Clamp(amount, 0, ProductionMaxAmount);
                        RawNumberGuard.SetInt(prodObj, "Amount", amount);
                    }
                }
            }
        }
        catch { }
    }

    private void OnSettlementSelected(object? sender, EventArgs e)
    {
        ClearFields();
        try
        {
            if (_settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;
            int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
            if (dataIdx >= _settlements.Length) return;

            var settlement = _settlements.GetObject(dataIdx);

            var sdata = SettlementLogic.LoadSettlementData(settlement);
            _settlementName.Text = sdata.Name;
            _seedField.Text = sdata.SeedValue;

            for (int i = 0; i < StatCount; i++)
            {
                _statFields[i].NumericValue = sdata.Stats[i];
                ApplyStatColor(i);
            }

            // Population field (separate from Stats[0] MaxPopulation)
            _hasPopulationKey = sdata.HasPopulationKey;
            _populationField.Enabled = sdata.HasPopulationKey;
            if (sdata.HasPopulationKey)
            {
                _populationField.NumericValue = sdata.Population;
                _rawPopulation = sdata.RawPopulation;
                ApplyPopulationColor();
            }
            else
            {
                _populationField.NumericValue = 0;
                _rawPopulation = null;
            }

            // Store raw stat values for preservation
            _rawSettlementStats = (int[])sdata.RawStats.Clone();

            _decisionTypeField.SelectedIndex = sdata.DecisionTypeIndex;
            if (sdata.LastDecisionTime.HasValue)
                LoadTimestamp(_lastDecisionTimeField, ((DateTimeOffset)sdata.LastDecisionTime.Value.ToUniversalTime()).ToUnixTimeSeconds());
            else
                _lastDecisionTimeField.Checked = false;

            // NPC Race
            LoadRaceCombo(sdata.AlienRace);

            // Timestamp fields
            LoadTimestamp(_lastBugAttackTimeField, sdata.LastBugAttackChangeTime);
            LoadTimestamp(_lastAlertTimeField, sdata.LastAlertChangeTime);
            LoadTimestamp(_lastDebtTimeField, sdata.LastDebtChangeTime);
            LoadTimestamp(_lastUpkeepTimeField, sdata.LastUpkeepDebtCheckTime);
            LoadTimestamp(_lastPopulationTimeField, sdata.LastPopulationChangeTime);

            // Mission seed & start time
            _missionSeedField.Text = sdata.MiniMissionSeed != 0
                ? sdata.MiniMissionSeed.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "";
            LoadTimestamp(_miniMissionStartTimeField, sdata.MiniMissionStartTime);

            // Building states
            _rawBuildingStates = (int[])sdata.RawBuildingStates.Clone();
            _hasBuildingStates = sdata.HasBuildingStates;
            for (int i = 0; i < SettlementLogic.BuildingStateSlotCount; i++)
            {
                _editorUpdating = true;
                try
                {
                    SetBuildingStateComboValue(i, sdata.BuildingStates[i]);
                    _buildingStateInfoLabels[i].Text = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(sdata.BuildingStates[i]);
                }
                finally { _editorUpdating = false; }
            }
            LoadEditorFromSlot();

            // Perks
            var perksArr = settlement.GetArray("Perks");
            int perksLoaded = 0;
            if (perksArr != null)
            {
                for (int i = 0; i < PerkSlotCount && i < perksArr.Length; i++)
                {
                    try
                    {
                        string raw = perksArr.GetString(i) ?? "";
                        LoadPerkSlot(i, raw);
                    }
                    catch { _perkCombos[i].SelectedIndex = 0; }
                    perksLoaded++;
                }
            }
            // Clear any remaining perk slots beyond the array length
            for (int i = perksLoaded; i < PerkSlotCount; i++)
                _perkCombos[i].SelectedIndex = 0;

            // Production state – store raw amounts for preservation
            _productionGrid.Rows.Clear();
            _rawProductionAmounts.Clear();
            var prodArr = settlement.GetArray("ProductionState");
            if (prodArr != null)
            {
                for (int i = 0; i < prodArr.Length; i++)
                {
                    try
                    {
                        var prodObj = prodArr.GetObject(i);
                        string elementId = prodObj.GetString("ElementId") ?? prodObj.Get("ElementId")?.ToString() ?? "";
                        // Strip "^" prefix for database lookup (save format: "^GAS1", db key: "GAS1")
                        string lookupId = elementId.StartsWith('^') ? elementId[1..] : elementId;
                        var dbItem = string.IsNullOrEmpty(lookupId) ? null : _database?.GetItem(lookupId);
                        string itemName = dbItem?.Name ?? lookupId;
                        Image? icon = GetProductionIcon(lookupId);
                        int amount = 0;
                        try { amount = prodObj.GetInt("Amount"); } catch { }
                        _rawProductionAmounts[i] = amount;
                        _productionGrid.Rows.Add(icon ?? (object)_placeholderIcon, itemName, elementId,
                            amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private void OnSettlementNameChanged(object? sender, EventArgs e)
    {
        if (_settlementSelector.SelectedIndex < 0 || _settlementSelector.Items.Count == 0) return;
        int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
        string newName = string.IsNullOrWhiteSpace(_settlementName.Text)
            ? UiStrings.Format("settlement.fallback_name", dataIdx + 1)
            : _settlementName.Text;
        int idx = _settlementSelector.SelectedIndex;
        _settlementSelector.SelectedIndexChanged -= OnSettlementSelected;
        _settlementSelector.Items.RemoveAt(idx);
        _settlementSelector.Items.Insert(idx, $"[{dataIdx}] {newName}");
        _settlementSelector.SelectedIndex = idx;
        _settlementSelector.SelectedIndexChanged += OnSettlementSelected;
    }

    private void ClearFields()
    {
        _settlementName.Text = "";
        _seedField.Text = "";
        for (int i = 0; i < StatCount; i++)
        {
            _statFields[i].NumericValue = 0;
            _statFields[i].ForeColor = SystemColors.WindowText;
        }
        _populationField.NumericValue = 0;
        _populationField.Enabled = false;
        _hasPopulationKey = false;
        _rawPopulation = null;
        _decisionTypeField.SelectedIndex = -1;
        _lastDecisionTimeField.Checked = false;
        for (int i = 0; i < PerkSlotCount; i++)
        {
            _perkCombos[i].SelectedIndex = 0;
            _perkSeedFields[i].Text = "";
            _perkSeedPanels[i].Visible = false;
        }
        _productionGrid.Rows.Clear();
        _rawProductionAmounts.Clear();

        // New fields
        _raceField.SelectedIndex = -1;
        _lastBugAttackTimeField.Checked = false;
        _lastAlertTimeField.Checked = false;
        _lastDebtTimeField.Checked = false;
        _lastUpkeepTimeField.Checked = false;
        _lastPopulationTimeField.Checked = false;
        _miniMissionStartTimeField.Checked = false;
        _missionSeedField.Text = "";
        _rawBuildingStates = null;
        _hasBuildingStates = false;
        for (int i = 0; i < SettlementLogic.BuildingStateSlotCount; i++)
        {
            _editorUpdating = true;
            try
            {
                SetBuildingStateComboValue(i, 0);
            }
            finally { _editorUpdating = false; }
            _buildingStateInfoLabels[i].Text = "";
        }

        // Clear building editor
        _editorUpdating = true;
        try
        {
            _editorRawValueField.NumericValue = 0;
            _editorClassValueLabel.Text = "-";
            _editorStateValueLabel.Text = "-";
            for (int i = 0; i < InitPhaseCount; i++) _editorInitCheckboxes[i].Checked = false;
            for (int i = 0; i < UpgradePhaseCount; i++) _editorUpgradeCheckboxes[i].Checked = false;
            for (int i = 0; i < TierBitCount; i++) _editorTierCheckboxes[i].Checked = false;
            for (int i = 0; i < FlagBitCount; i++) _editorFlagCheckboxes[i].Checked = false;
        }
        finally { _editorUpdating = false; }
    }

    // --- Production icon support ---

    private static readonly Bitmap _placeholderIcon = new(24, 24);
    private readonly Dictionary<string, Image> _scaledIconCache = new(StringComparer.OrdinalIgnoreCase);

    private Image? GetProductionIcon(string itemId)
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

    private void OnProductionGridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_productionGrid.Columns[e.ColumnIndex].Name != "ChangeElement") return;
        if (_database == null) return;

        var items = _database.Items.Values
            .Where(item =>
            {
                // If we have a curated allowed-production list, use it
                if (_allowedProductionItems is { Count: > 0 })
                    return _allowedProductionItems.ContainsKey(item.Id);

                // Fallback: broad filter by type
                return (string.Equals(item.ItemType, "Products", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.ItemType, "Raw Materials", StringComparison.OrdinalIgnoreCase))
                     && !GameItemDatabase.IsPickerExcluded(item.Id);
            })
            .OrderBy(item => item.Name)
            .Select(item =>
            {
                Image? icon = GetProductionIcon(item.Id);
                return (icon: (Image?)(icon ?? (Image)_placeholderIcon), name: item.Name, id: item.Id, category: item.ItemType);
            })
            .ToList();

        using var picker = new ItemPickerDialog("Select Production Element", items);
        if (picker.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(picker.SelectedId))
        {
            var gridRow = _productionGrid.Rows[e.RowIndex];
            string rawId = picker.SelectedId!;
            // Save format uses "^" prefix for element IDs (e.g. "^GAS1")
            string saveId = rawId.StartsWith('^') ? rawId : "^" + rawId;
            gridRow.Cells["ElementId"].Value = saveId;
            var dbItem = _database.GetItem(rawId);
            gridRow.Cells["ItemName"].Value = dbItem?.Name ?? rawId;
            gridRow.Cells["Icon"].Value = GetProductionIcon(rawId) ?? (object)_placeholderIcon;
        }
    }

    private void OnExportSettlement(object? sender, EventArgs e)
    {
        try
        {
            if (_settlements == null || _settlementSelector.SelectedIndex < 0 || _filteredIndices.Count == 0) return;
            int dataIdx = _filteredIndices[_settlementSelector.SelectedIndex];
            if (dataIdx >= _settlements.Length) return;

            var settlement = _settlements.GetObject(dataIdx);
            var cfg = ExportConfig.Instance;
            var vars = new Dictionary<string, string>
            {
                ["settlement_name"] = _settlementName.Text ?? "",
                ["seed"] = _seedField.Text ?? ""
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(cfg.SettlementExt, "Settlement files"),
                DefaultExt = cfg.SettlementExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(cfg.SettlementTemplate, cfg.SettlementExt, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                settlement.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("settlement.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportSettlement(object? sender, EventArgs e)
    {
        try
        {
            if (_settlements == null) return;

            var cfg = ExportConfig.Instance;
            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildImportFilter(cfg.SettlementExt, "Settlement files", ".stl")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Unwrap Data envelope if present (Data -> Settlement)
            imported = InventoryImportHelper.UnwrapNomNom(imported, "Settlement");

            int selectedDataIdx = (_settlementSelector.SelectedIndex >= 0 && _filteredIndices.Count > 0)
                ? _filteredIndices[_settlementSelector.SelectedIndex]
                : -1;

            int target = SettlementLogic.FindImportTargetIndex(_settlements, selectedDataIdx);

            if (target == -2)
            {
                // Array full and no selection – ask user which slot to overwrite
                target = ShowSlotPickerDialog();
                if (target < 0) return; // cancelled
            }

            if (target == -1)
            {
                // Append new entry
                _settlements.Add(imported);
                int newIdx = _settlements.Length - 1;
                _filteredIndices.Add(newIdx);
                string name = imported.GetString("Name") ?? UiStrings.Format("settlement.fallback_name", newIdx + 1);
                _settlementSelector.Items.Add(name);
                _settlementSelector.SelectedIndex = _settlementSelector.Items.Count - 1;
            }
            else
            {
                // Overwrite existing entry
                var settlement = _settlements.GetObject(target);
                foreach (var name in imported.Names())
                    settlement.Set(name, imported.Get(name));

                OnSettlementSelected(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("settlement.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Shows a dialog letting the user pick which settlement slot to overwrite
    /// when the array is full and no settlement is currently selected.
    /// </summary>
    private int ShowSlotPickerDialog()
    {
        if (_settlements == null) return -1;

        using var form = new Form
        {
            Text = UiStrings.Get("settlement.slot_picker_title"),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ClientSize = new Size(400, 140)
        };

        var label = new Label
        {
            Text = UiStrings.Get("settlement.slot_picker_message"),
            AutoSize = true,
            Location = new Point(12, 12)
        };

        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 40),
            Width = 370
        };

        for (int i = 0; i < _settlements.Length; i++)
        {
            try
            {
                var s = _settlements.GetObject(i);
                string name = s.GetString("Name") ?? "";
                combo.Items.Add(string.IsNullOrEmpty(name) ? UiStrings.Format("settlement.slot_label", i + 1) : UiStrings.Format("settlement.slot_label_named", i + 1, name));
            }
            catch
            {
                combo.Items.Add(UiStrings.Format("settlement.slot_label", i + 1));
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;

        var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(220, 80), Width = 75 };
        var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(305, 80), Width = 75 };
        form.AcceptButton = okBtn;
        form.CancelButton = cancelBtn;

        form.Controls.AddRange(new Control[] { label, combo, okBtn, cancelBtn });

        return form.ShowDialog(this) == DialogResult.OK && combo.SelectedIndex >= 0
            ? combo.SelectedIndex
            : -1;
    }

    // --- Timestamp helpers ---

    private static void LoadTimestamp(DateTimePicker picker, long unixSeconds)
    {
        if (unixSeconds > 0)
        {
            picker.Value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().DateTime;
            picker.Checked = true;
        }
        else
        {
            picker.Checked = false;
        }
    }

    private static long ReadTimestamp(DateTimePicker picker)
    {
        if (!picker.Checked) return 0;
        return ((DateTimeOffset)picker.Value.ToUniversalTime()).ToUnixTimeSeconds();
    }

    // --- Race ComboBox support ---

    private static void OnRaceComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var combo = (ComboBox)sender!;
        if (combo.Items[e.Index] is not RaceComboItem item) return;

        e.DrawBackground();
        var font = e.Font ?? combo.Font;
        using var brush = new SolidBrush(e.ForeColor);
        e.Graphics.DrawString(item.DisplayText, font, brush, e.Bounds);
        e.DrawFocusRectangle();
    }

    private void PopulateRaceCombo()
    {
        _raceField.Items.Clear();
        foreach (var race in SettlementLogic.AlienRaces)
        {
            string display = SettlementLogic.AlienRaceDisplayNames.TryGetValue(race, out var dn) ? dn : race;
            if (SettlementLogic.AlienRaceLocKeys.TryGetValue(race, out var locKey))
            {
                var localised = UiStrings.Get(locKey);
                if (!string.IsNullOrEmpty(localised) && localised != locKey)
                    display = localised;
            }
            _raceField.Items.Add(new RaceComboItem(race, $"{display} ({race})"));
        }
    }

    private void LoadRaceCombo(string alienRace)
    {
        for (int i = 0; i < _raceField.Items.Count; i++)
        {
            if (_raceField.Items[i] is RaceComboItem item &&
                string.Equals(item.InternalId, alienRace, StringComparison.OrdinalIgnoreCase))
            {
                _raceField.SelectedIndex = i;
                return;
            }
        }
        _raceField.SelectedIndex = _raceField.Items.Count > 0 ? 0 : -1;
    }

    private string GetSelectedRace()
    {
        if (_raceField.SelectedItem is RaceComboItem item)
            return item.InternalId;
        return "None";
    }

    private void RefreshRaceCombo()
    {
        int savedIdx = _raceField.SelectedIndex;
        string? savedRace = (_raceField.SelectedItem as RaceComboItem)?.InternalId;
        PopulateRaceCombo();
        if (savedRace != null)
            LoadRaceCombo(savedRace);
        else if (savedIdx >= 0 && savedIdx < _raceField.Items.Count)
            _raceField.SelectedIndex = savedIdx;
    }

    // --- Building state support ---

    private void OnBuildingStateChanged(int slot)
    {
        if (slot < 0 || slot >= SettlementLogic.BuildingStateSlotCount) return;
        int value = (int)(_buildingStateNuds[slot].NumericValue ?? 0);
        _buildingStateInfoLabels[slot].Text = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(value);
        // Sync editor if showing the same slot
        if (_editorSlotSelector != null && (int)(_editorSlotSelector.NumericValue ?? 1) - 1 == slot && !_editorUpdating)
        {
            _editorUpdating = true;
            try
            {
                _editorRawValueField.NumericValue = value;
                UpdateEditorCheckboxesFromValue(value);
                UpdateEditorClassState(value);
            }
            finally { _editorUpdating = false; }
        }
    }

    private void OnBuildingStateNudChanged(int slot)
    {
        if (_editorUpdating) return;
        int value = (int)(_buildingStateNuds[slot].NumericValue ?? 0);
        _editorUpdating = true;
        try
        {
            SyncComboToValue(slot, value);

            _buildingStateInfoLabels[slot].Text = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(value);

            if (_editorSlotSelector != null && (int)(_editorSlotSelector.NumericValue ?? 1) - 1 == slot)
            {
                _editorRawValueField.NumericValue = value;
                UpdateEditorCheckboxesFromValue(value);
                UpdateEditorClassState(value);
            }
        }
        finally { _editorUpdating = false; }
    }

    private void OnBuildingStateComboChanged(int slot)
    {
        if (_editorUpdating) return;
        int selectedIdx = _buildingStateFields[slot].SelectedIndex;
        if (selectedIdx < 0) return;

        _editorUpdating = true;
        try
        {
            if (selectedIdx < SettlementDatabase.KnownMilestones.Length)
            {
                int value = SettlementDatabase.KnownMilestones[selectedIdx].Value;
                _buildingStateNuds[slot].NumericValue = value;
                _buildingStateInfoLabels[slot].Text = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(value);

                if (_editorSlotSelector != null && (int)(_editorSlotSelector.NumericValue ?? 1) - 1 == slot)
                {
                    _editorRawValueField.NumericValue = value;
                    UpdateEditorCheckboxesFromValue(value);
                    UpdateEditorClassState(value);
                }
            }
            else
            {
                _buildingStateNuds[slot].NumericValue = 0;
                _buildingStateInfoLabels[slot].Text = SettlementLogic.SettlementBuildingState.GetBuildingSlotDescription(0);

                if (_editorSlotSelector != null && (int)(_editorSlotSelector.NumericValue ?? 1) - 1 == slot)
                {
                    _editorRawValueField.NumericValue = 0;
                    UpdateEditorCheckboxesFromValue(0);
                    UpdateEditorClassState(0);
                }
            }
        }
        finally { _editorUpdating = false; }
    }

    /// <summary>Populates a building state ComboBox with known milestone values.</summary>
    private static void PopulateBuildingStateCombo(ComboBox combo)
    {
        foreach (var (value, locKey) in SettlementDatabase.KnownMilestones)
        {
            string label = UiStrings.Get(locKey);
            combo.Items.Add($"{value} - {label}");
        }
        combo.Items.Add(UiStrings.Get("settlement.bs_custom"));
    }

    /// <summary>Returns the ComboBox index for a milestone value, or -1 if not a known milestone.</summary>
    private static int FindMilestoneIndex(int value)
    {
        for (int j = 0; j < SettlementDatabase.KnownMilestones.Length; j++)
        {
            if (SettlementDatabase.KnownMilestones[j].Value == value)
                return j;
        }
        return -1;
    }

    /// <summary>Selects the matching milestone in the combo or falls back to the "Custom" entry.</summary>
    private void SyncComboToValue(int slot, int value)
    {
        int idx = FindMilestoneIndex(value);
        _buildingStateFields[slot].SelectedIndex = idx >= 0 ? idx : _buildingStateFields[slot].Items.Count - 1;
    }

    /// <summary>Sets the value of a building state slot, syncing both NUD and ComboBox.</summary>
    private void SetBuildingStateComboValue(int slot, int value)
    {
        _buildingStateNuds[slot].NumericValue = value;
        SyncComboToValue(slot, value);
    }

    /// <summary>Re-populates all building state ComboBoxes with localised milestone labels.</summary>
    private void RefreshBuildingStateCombos()
    {
        if (_buildingStateFields == null) return;
        for (int i = 0; i < _buildingStateFields.Length; i++)
        {
            int currentValue = (int)(_buildingStateNuds[i].NumericValue ?? 0);
            _buildingStateFields[i].Items.Clear();
            PopulateBuildingStateCombo(_buildingStateFields[i]);
            SyncComboToValue(i, currentValue);
        }
    }

    // --- Building Editor support ---

    private void OnEditorSlotChanged(object? sender, EventArgs e)
    {
        if (_editorRawValueField == null) return;
        LoadEditorFromSlot();
    }

    private void LoadEditorFromSlot()
    {
        if (_editorRawValueField == null || _editorInitCheckboxes == null) return;
        int slot = (int)(_editorSlotSelector.NumericValue ?? 1) - 1;
        if (slot < 0 || slot >= SettlementLogic.BuildingStateSlotCount) return;

        int value = (int)(_buildingStateNuds[slot].NumericValue ?? 0);

        _editorUpdating = true;
        try
        {
            _editorRawValueField.NumericValue = value;
            UpdateEditorCheckboxesFromValue(value);
            UpdateEditorClassState(value);
        }
        finally { _editorUpdating = false; }
    }

    private void UpdateEditorCheckboxesFromValue(int value)
    {
        for (int i = 0; i < InitPhaseCount; i++)
            _editorInitCheckboxes[i].Checked = ((value >> i) & 1) != 0;
        for (int i = 0; i < UpgradePhaseCount; i++)
            _editorUpgradeCheckboxes[i].Checked = ((value >> (10 + i)) & 1) != 0;
        for (int i = 0; i < TierBitCount; i++)
            _editorTierCheckboxes[i].Checked = ((value >> (20 + i)) & 1) != 0;
        for (int i = 0; i < FlagBitCount; i++)
            _editorFlagCheckboxes[i].Checked = ((value >> (26 + i)) & 1) != 0;
    }

    private void UpdateEditorClassState(int value)
    {
        if (SettlementLogic.SettlementBuildingState.IsEmpty(value))
        {
            _editorClassValueLabel.Text = "-";
            _editorStateValueLabel.Text = UiStrings.Get("settlement.bs_empty");
            return;
        }
        var (classKey, stateKey) = SettlementLogic.SettlementBuildingState.DetermineClassAndState(value);
        _editorClassValueLabel.Text = UiStrings.Get(classKey);
        _editorStateValueLabel.Text = UiStrings.Get(stateKey);
    }

    private int ComputeValueFromEditorCheckboxes()
    {
        int value = 0;
        for (int i = 0; i < InitPhaseCount; i++)
            if (_editorInitCheckboxes[i].Checked) value |= (1 << i);
        for (int i = 0; i < UpgradePhaseCount; i++)
            if (_editorUpgradeCheckboxes[i].Checked) value |= (1 << (10 + i));
        for (int i = 0; i < TierBitCount; i++)
            if (_editorTierCheckboxes[i].Checked) value |= (1 << (20 + i));
        for (int i = 0; i < FlagBitCount; i++)
            if (_editorFlagCheckboxes[i].Checked) value |= (1 << (26 + i));
        return value;
    }

    private void OnEditorCheckboxChanged(object? sender, EventArgs e)
    {
        if (_editorUpdating || _editorRawValueField == null) return;
        int value = ComputeValueFromEditorCheckboxes();
        _editorUpdating = true;
        try
        {
            _editorRawValueField.NumericValue = value;
            UpdateEditorClassState(value);
        }
        finally { _editorUpdating = false; }
    }

    private void OnEditorRawValueChanged(object? sender, EventArgs e)
    {
        if (_editorUpdating) return;
        int value = (int)(_editorRawValueField.NumericValue ?? 0);
        _editorUpdating = true;
        try
        {
            UpdateEditorCheckboxesFromValue(value);
            UpdateEditorClassState(value);
        }
        finally { _editorUpdating = false; }
    }

    private void OnEditorApply(object? sender, EventArgs e)
    {
        int slot = (int)(_editorSlotSelector.NumericValue ?? 1) - 1;
        if (slot < 0 || slot >= SettlementLogic.BuildingStateSlotCount) return;

        int value = ComputeValueFromEditorCheckboxes();
        _editorUpdating = true;
        try
        {
            SetBuildingStateComboValue(slot, value);
            OnBuildingStateChanged(slot);
        }
        finally { _editorUpdating = false; }
    }

    /// <summary>
    /// Applies colour coding to the stat NUD at the given index.
    /// Crimson when value falls below the min threshold, Tomato when value rises above the max threshold, default otherwise.
    /// Comparisons are exclusive: colour is applied only when values strictly exceed the soft-cap thresholds.
    /// </summary>
    private void ApplyStatColor(int index)
    {
        if (index < 0 || index >= StatCount) return;
        int value = (int)(_statFields[index].NumericValue ?? 0);
        if (value < SettlementLogic.StatMinValues[index])
            _statFields[index].ForeColor = Color.Crimson;
        else if (value > SettlementLogic.StatMaxValues[index])
            _statFields[index].ForeColor = Color.Tomato;
        else
            _statFields[index].ForeColor = SystemColors.WindowText;
    }

    /// <summary>
    /// Applies colour coding to the population NUD.
    /// Crimson when value drops below 0, Tomato when value exceeds the soft-cap (PopulationSoftMax).
    /// Comparisons are exclusive: colour is applied only when values strictly exceed the thresholds.
    /// </summary>
    private void ApplyPopulationColor()
    {
        int value = (int)(_populationField.NumericValue ?? 0);
        if (value < 0)
            _populationField.ForeColor = Color.Crimson;
        else if (value > SettlementLogic.PopulationSoftMax)
            _populationField.ForeColor = Color.Tomato;
        else
            _populationField.ForeColor = SystemColors.WindowText;
    }

    private static readonly string[] StatLocKeys =
    {
        "settlement.max_population", "settlement.happiness", "settlement.productivity",
        "settlement.upkeep", "settlement.sentinels", "settlement.debt",
        "settlement.alert", "settlement.bug_attack"
    };

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("settlement.title");
        _deleteSettlementBtn.Text = UiStrings.Get("settlement.delete");
        _exportSettlementBtn.Text = UiStrings.Get("common.export");
        _importSettlementBtn.Text = UiStrings.Get("common.import");
        _generateSeedBtn.Text = UiStrings.Get("common.generate");
        for (int i = 0; i < _perkSeedGenerateButtons.Length; i++)
            _perkSeedGenerateButtons[i].Text = UiStrings.Get("common.generate");

        // Form row labels
        _nameLabel.Text = UiStrings.Get("settlement.name");
        _seedLabel.Text = UiStrings.Get("settlement.seed");
        for (int i = 0; i < _perkLabels.Length; i++)
            _perkLabels[i].Text = UiStrings.Format("settlement.perk", i + 1);
        _decisionTypeLabel.Text = UiStrings.Get("settlement.decision_type");
        _lastDecisionLabel.Text = UiStrings.Get("settlement.last_decision");

        // Stat row labels
        for (int i = 0; i < StatCount && i < StatLocKeys.Length; i++)
            _statRowLabels[i].Text = UiStrings.Get(StatLocKeys[i]);
        _populationLabel.Text = UiStrings.Get("settlement.population");

        // Deletion warning label
        _settleWarnLabel.Text = UiStrings.Get("settlement.delete_warning");

        // Production section
        _productionHeaderLabel.Text = UiStrings.Get("settlement.production_header");

        // Production grid column headers
        if (_productionGrid.Columns["Name"] is DataGridViewColumn nameCol) nameCol.HeaderText = UiStrings.Get("settlement.col_name");
        if (_productionGrid.Columns["ElementID"] is DataGridViewColumn elemCol) elemCol.HeaderText = UiStrings.Get("settlement.col_element_id");
        if (_productionGrid.Columns["Edit"] is DataGridViewColumn editCol) editCol.HeaderText = UiStrings.Get("settlement.col_edit");
        if (_productionGrid.Columns["Amount"] is DataGridViewColumn amtCol) amtCol.HeaderText = UiStrings.Get("settlement.col_amount");

        // Tab labels
        _tabControl.TabPages[0].Text = UiStrings.Get("settlement.tab_stats_perks");
        _tabControl.TabPages[1].Text = UiStrings.Get("settlement.tab_production");
        _tabControl.TabPages[2].Text = UiStrings.Get("settlement.tab_building_states");
        _tabControl.TabPages[3].Text = UiStrings.Get("settlement.tab_building_editor");

        // Experimental warning labels
        _buildingStatesExperimentalWarningLabel.Text = UiStrings.Get("settlement.experimental_warning");
        _editorExperimentalWarningLabel.Text = UiStrings.Get("settlement.experimental_warning");

        // New field labels
        _raceLabel.Text = UiStrings.Get("settlement.race");
        _lastBugAttackTimeLabel.Text = UiStrings.Get("settlement.last_bug_attack_time");
        _lastAlertTimeLabel.Text = UiStrings.Get("settlement.last_alert_time");
        _lastDebtTimeLabel.Text = UiStrings.Get("settlement.last_debt_time");
        _lastUpkeepTimeLabel.Text = UiStrings.Get("settlement.last_upkeep_time");
        _lastPopulationTimeLabel.Text = UiStrings.Get("settlement.last_population_time");
        _missionSeedLabel.Text = UiStrings.Get("settlement.mission_seed");
        _generateMissionSeedBtn.Text = UiStrings.Get("common.generate");
        _miniMissionStartTimeLabel.Text = UiStrings.Get("settlement.mini_mission_start_time");

        // Refresh race combo with localised names
        RefreshRaceCombo();

        // Refresh decision type combo with localised display names
        RefreshDecisionTypeCombo();

        // Refresh building state combos with localised milestone labels
        RefreshBuildingStateCombos();

        // Building Editor tab labels
        _editorHeaderLabel.Text = UiStrings.Get("settlement.tab_building_editor");
        _editorRawValueLabel.Text = UiStrings.Get("settlement.bs_editor_raw_value");
        _editorApplyBtn.Text = UiStrings.Get("settlement.bs_editor_apply");
        _editorClassDisplayLabel.Text = UiStrings.Get("settlement.bs_editor_class");
        _editorStateDisplayLabel.Text = UiStrings.Get("settlement.bs_editor_state");
        _editorInitPhasesLabel.Text = UiStrings.Get("settlement.bs_editor_init_phases");
        _editorUpgradePhasesLabel.Text = UiStrings.Get("settlement.bs_editor_upgrade_phases");
        _editorTierLabel.Text = UiStrings.Get("settlement.bs_editor_tier_heading");
        _editorTierInlineLabels[0].Text = UiStrings.Get("settlement.bs_editor_tier_b");
        _editorTierInlineLabels[1].Text = UiStrings.Get("settlement.bs_editor_tier_a");
        _editorTierInlineLabels[2].Text = UiStrings.Get("settlement.bs_editor_tier_s");
        _editorFlagsLabel.Text = UiStrings.Get("settlement.bs_editor_flags_heading");
        _editorFlagCheckboxes[0].Text = UiStrings.Get("settlement.bs_editor_class_active");
        _editorFlagCheckboxes[1].Text = UiStrings.Get("settlement.bs_editor_b_arrived");
        _editorFlagCheckboxes[2].Text = UiStrings.Get("settlement.bs_editor_a_arrived");
        _editorFlagCheckboxes[3].Text = UiStrings.Get("settlement.bs_editor_s_arrived");
    }

    private void RefreshDecisionTypeCombo()
    {
        int savedIdx = _decisionTypeField.SelectedIndex;
        _decisionTypeField.BeginUpdate();
        _decisionTypeField.Items.Clear();
        foreach (var dt in SettlementLogic.DecisionTypes)
        {
            if (SettlementLogic.DecisionTypeLocKeys.TryGetValue(dt, out var key))
                _decisionTypeField.Items.Add(UiStrings.Get(key));
            else
                _decisionTypeField.Items.Add(dt);
        }
        if (savedIdx >= 0 && savedIdx < _decisionTypeField.Items.Count)
            _decisionTypeField.SelectedIndex = savedIdx;
        _decisionTypeField.EndUpdate();
    }
}
