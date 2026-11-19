using System.Globalization;
using NMSE.Config;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Controls;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class StarshipPanel : UserControl
{
    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    /// <summary>Raised when the user requests navigation to a JSON path in the Raw JSON Editor.</summary>
    public event EventHandler<GoToJsonEventArgs>? GoToJsonRequested;

    /// <summary>
    /// Raised after auto-stack moves cargo into another inventory so destination
    /// panels can refresh their grids immediately.
    /// </summary>
    public event EventHandler? CrossInventoryTransferCompleted;

    private JsonArray? _shipOwnership;
    private JsonObject? _playerState;
    private JsonObject? _saveData;
    private GameItemDatabase? _database;
    private int _primaryShipIndex;
    private string _saveScopeKey = "unknown";
    private readonly Random _rng = new();

    /// <summary>Raw (unclamped) ship stat values read from JSON for the currently selected ship.</summary>
    private Dictionary<string, double>? _rawShipStatValues;

    /// <summary>True while the panel is performing an initial data load; suppresses UI side-effects.</summary>
    private bool _loading;

    /// <summary>Suppresses the pre-refresh detail commit after an import has already written the ship.</summary>
    private bool _suppressDetailCommit;

    /// <summary>Suppresses ship-detail dirty tracking while controls are populated programmatically.</summary>
    private bool _suppressShipDetailChanged;

    /// <summary>Class index loaded from the save for the current ship, used to detect user changes.</summary>
    private int _originalClassIndex = -1;

    /// <summary>The data index of the ship currently displayed in the detail controls, or -1.</summary>
    private int _currentShipIndex = -1;

    /// <summary>The data index of the ship currently loaded in the Customisation tab.</summary>
    private int _currentCustomisationShipIndex = -1;

    /// <summary>The customisation config for the ship currently loaded in the Customisation tab.</summary>
    private ShipCustomisationConfig? _currentCustomisationConfig;

    /// <summary>Per-slot combobox controls currently shown in the Customisation tab.</summary>
    private readonly List<(Label Lbl, ComboBox Combo, ShipCustomisationSlot Slot)> _slotControls = new();

    /// <summary>Per-texture-group combobox controls currently shown in the Customisation tab.</summary>
    private readonly List<(Label Lbl, ComboBox Combo, string GroupID)> _textureControls = new();

    /// <summary>Palette label and combobox shown in the Customisation tab (null when not present).</summary>
    private Label? _paletteLabelCtrl;
    private ComboBox? _paletteCombo;

    /// <summary>
    /// Colour channel swatch controls in the Customisation tab.
    /// Each entry holds the channel name (e.g. "Paint"), the colour alt (e.g. "Primary"),
    /// the display palette ID (empty for combo-selected palette, or a specific palette like
    /// "SailShip_Sails"), and the clickable Panel swatch.
    /// </summary>
    private readonly List<(string PaletteName, string ColourAlt, string DisplayPaletteId, Panel Swatch)> _colourSwatches = new();

    /// <summary>Active colour picker dropdown, disposed when a new one is opened.</summary>
    private ToolStripDropDown? _activeColourMenu;

    private void SetStarshipMaxSupportedLabels(string filename)
    {
        var (_, cargoLabel, techLabel) = StarshipLogic.GetShipInfo(filename);
        _inventoryGrid.SetMaxSupportedLabel(cargoLabel);
        _techGrid.SetMaxSupportedLabel(techLabel);
    }

    private void OnShipTypeChanged(object? sender, EventArgs e)
    {
        var typeItem = _shipType.SelectedItem as StarshipLogic.ShipTypeItem;
        if (typeItem == null)
            return;

        string selectedType = typeItem.InternalName;

        // For modified types, use the custom filename for label lookup;
        // otherwise resolve from the canonical type name.
        string filename = typeItem.CustomFilename ?? StarshipLogic.LookupFilenameForType(selectedType);
        SetStarshipMaxSupportedLabels(string.IsNullOrEmpty(filename) ? UiStrings.Get("common.unknown") : filename);

        // Update inventory owner type so tech filtering reflects the new ship subtype.
        // Distinguishes Ship/AlienShip/RobotShip/Corvette as separate owner types
        // with different owner Enums that control which tech items are valid.
        // SetInventoryOwnerType auto-refreshes filters when the type actually changes,
        // so no additional RefreshItemFilters call is needed.
        string ownerType = StarshipLogic.GetOwnerTypeForShip(selectedType);
        _techGrid.SetInventoryOwnerType(ownerType);
        _inventoryGrid.SetInventoryOwnerType(ownerType);

        // Update the underlying ship data and dependent UI controls.
        if (_shipOwnership != null && _shipSelector.SelectedIndex >= 0)
        {
            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx < _shipOwnership.Length)
            {
                var ship = _shipOwnership.GetObject(idx);
                if (ship != null)
                {
                    var resource = ship?.GetObject("Resource");
                    string? currentFilename = resource?.GetString("Filename");
                    bool filenameChanged = resource != null
                        && !string.IsNullOrEmpty(filename)
                        && !string.Equals(currentFilename, filename, StringComparison.Ordinal);
                    if (filenameChanged)
                        resource!.Set("Filename", filename);

                    if (!_loading)
                    {
                        // Refresh the ship selector display name so the type prefix updates
                        OnShipNameChanged(null, EventArgs.Empty);

                        // Reload the customisation tab with the new scene resource
                        bool isCorvette = StarshipLogic.IsCorvette(filename);
                        LoadCustomisationTab(isCorvette, filename, idx);

                        if (filenameChanged)
                            DataModified?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }
    }

    public StarshipPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    /// <summary>
    /// Selects the ship type combo item matching the given English ship type name.
    /// If <paramref name="isModified"/> is true and <paramref name="customFilename"/>
    /// is provided, adds a "(Modified)" variant to the combo box and selects it.
    /// </summary>
    private void SelectShipTypeByName(string? englishTypeName, bool isModified = false, string? customFilename = null)
    {
        if (string.IsNullOrEmpty(englishTypeName)) { _shipType.SelectedIndex = -1; return; }

        // Remove any previously added "(Modified)" items before selecting
        RemoveModifiedTypeItems();

        if (isModified && !string.IsNullOrEmpty(customFilename))
        {
            // Insert a "(Modified)" variant for this type
            string localisedBase = StarshipLogic.GetLocalisedShipTypeName(englishTypeName);
            string modifiedDisplay = UiStrings.Format("starship.type_modified", localisedBase);
            var modifiedItem = new StarshipLogic.ShipTypeItem(englishTypeName, modifiedDisplay, customFilename);
            _shipType.Items.Add(modifiedItem);
            _shipType.SelectedIndex = _shipType.Items.Count - 1;
            return;
        }

        for (int i = 0; i < _shipType.Items.Count; i++)
        {
            if (_shipType.Items[i] is StarshipLogic.ShipTypeItem item &&
                item.InternalName.Equals(englishTypeName, StringComparison.OrdinalIgnoreCase))
            {
                _shipType.SelectedIndex = i;
                return;
            }
        }
        _shipType.SelectedIndex = -1;
    }

    /// <summary>
    /// Removes any "(Modified)" type items from the ship type combo box.
    /// These are identified by having a non-null <see cref="StarshipLogic.ShipTypeItem.CustomFilename"/>.
    /// </summary>
    private void RemoveModifiedTypeItems()
    {
        for (int i = _shipType.Items.Count - 1; i >= 0; i--)
        {
            if (_shipType.Items[i] is StarshipLogic.ShipTypeItem item && item.CustomFilename != null)
                _shipType.Items.RemoveAt(i);
        }
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("starship.title");
        _detailsLabel.Text = UiStrings.Get("starship.details");
        _statsLabel.Text = UiStrings.Get("starship.base_stats");
        _selectLabel.Text = UiStrings.Get("starship.select");
        _nameLabel.Text = UiStrings.Get("starship.name");
        _shipName.PlaceholderText = UiStrings.Get("common.procedural_no_name");
        _typeLabel.Text = UiStrings.Get("starship.type");
        _classLabel.Text = UiStrings.Get("starship.class");
        _seedLabel.Text = UiStrings.Get("starship.seed");
        _damageLabel.Text = UiStrings.Get("starship.damage");
        _shieldLabel.Text = UiStrings.Get("starship.shield");
        _hyperdriveLabel.Text = UiStrings.Get("starship.hyperdrive");
        _maneuverLabel.Text = UiStrings.Get("starship.maneuverability");
        _generateSeedBtn.Text = UiStrings.Get("common.generate");
        _deleteBtn.Text = UiStrings.Get("starship.delete");
        _exportBtn.Text = UiStrings.Get("starship.export");
        _importBtn.Text = UiStrings.Get("starship.import");
        _makePrimaryBtn.Text = UiStrings.Get("starship.make_primary");
        _archiveMoveBtn.Text = UiStrings.Get("starship.archive_move_btn");
        _archiveImportBtn.Text = UiStrings.Get("starship.archive_import_btn");
        _snapshotTechBtn.Text = UiStrings.Get("starship.export_snapshot");
        _importSnapshotBtn.Text = UiStrings.Get("starship.import_snapshot");
        _optimiseBtn.Text = UiStrings.Get("starship.optimise");
        _useOldColours.Text = UiStrings.Get("starship.use_old_colour");
        _corvetteWarningLabel.Text = UiStrings.Get("starship.corvette_warning");
        _cargoTabPage.Text = UiStrings.Get("starship.tab_cargo");
        _techTabPage.Text = UiStrings.Get("starship.tab_tech");
        _shipDetailsTabPage.Text = UiStrings.Get("starship.tab_ship_details");
        _customisationTabPage.Text = UiStrings.Get("starship.tab_customisation");
        _sceneLabelCtrl.Text = UiStrings.Get("starship.customisation_scene_label");
        if (_sailColourWarningLabel != null)
            _sailColourWarningLabel.Text = UiStrings.Get("starship.customisation_sail_warning");

        // Refresh ship type combo with localised display names
        RefreshShipTypeCombo();
        _inventoryGrid.ApplyUiLocalisation();
        _techGrid.ApplyUiLocalisation();
        new ToolTip().SetToolTip(_gotoListBtn, UiStrings.Format("goto_json.tooltip_section", _titleLabel.Text));
        new ToolTip().SetToolTip(_gotoSelectedBtn, UiStrings.Format("goto_json.tooltip_section", _shipDetailsTabPage.Text));
        new ToolTip().SetToolTip(_gotoStoreBtn, UiStrings.Format("goto_json.tooltip_section", _cargoTabPage.Text));
        new ToolTip().SetToolTip(_gotoCustBtn, UiStrings.Format("goto_json.tooltip_section", _customisationTabPage.Text));
    }

    /// <summary>
    /// Refreshes the ship type combo box with localised display names,
    /// preserving the currently selected type (including modified variants).
    /// </summary>
    private void RefreshShipTypeCombo()
    {
        var currentItem = _shipType.SelectedItem as StarshipLogic.ShipTypeItem;
        string? currentType = currentItem?.InternalName;
        string? currentCustomFilename = currentItem?.CustomFilename;
        _shipType.Items.Clear();
        _shipType.Items.AddRange(StarshipLogic.GetShipTypeItems());
        if (currentType != null)
            SelectShipTypeByName(currentType, currentCustomFilename != null, currentCustomFilename);
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        _inventoryGrid.SetDatabase(database);
        _techGrid.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _inventoryGrid.SetIconManager(iconManager);
        _techGrid.SetIconManager(iconManager);
    }

    public void SetSaveScopeKey(string saveScopeKey)
    {
        _saveScopeKey = string.IsNullOrWhiteSpace(saveScopeKey) ? "unknown" : saveScopeKey;
        ApplyPinnedSlotsForSelectedShip();
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _shipSelector.BeginUpdate();
        _shipType.BeginUpdate();
        try
        {
            _saveData = saveData;
            _currentShipIndex = -1;
            _shipType.Items.Clear();
            _shipType.Items.AddRange(StarshipLogic.GetShipTypeItems());

            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _shipOwnership = _playerState.GetArray("ShipOwnership");
            _shipSelector.Items.Clear();

            if (_shipOwnership == null) return;

            _primaryShipIndex = 0;
            try { _primaryShipIndex = _playerState.GetInt("PrimaryShip"); } catch { }

            _primaryShipLabel.Text = UiStrings.Format("starship.primary_label", StarshipLogic.GetPrimaryShipName(_shipOwnership, _primaryShipIndex));

            var shipList = StarshipLogic.BuildShipList(_shipOwnership);
            foreach (var shipItem in shipList)
                _shipSelector.Items.Add(shipItem);

            if (_shipSelector.Items.Count > 0)
            {
                // Find the item matching PrimaryShip index
                int selectIdx = 0;
                for (int i = 0; i < _shipSelector.Items.Count; i++)
                {
                    if (((StarshipLogic.ShipListItem)_shipSelector.Items[i]!).DataIndex == _primaryShipIndex)
                    {
                        selectIdx = i;
                        break;
                    }
                }
                _loading = true;
                try
                {
                    _shipSelector.SelectedIndex = selectIdx;
                }
                finally
                {
                    _loading = false;
                }
            }
        }
        catch { }
        finally
        {
            _shipType.EndUpdate();
            _shipSelector.EndUpdate();
            ResumeLayout(true);
        }
    }

    /// <summary>
    /// Writes the current ship-detail controls into the in-memory save tree so
    /// operations that serialise the ship (export, snapshot, archive) include edits
    /// the user has not saved yet. In-memory only - no disk I/O.
    /// </summary>
    private void SyncCurrentShipToModel()
    {
        if (_saveData == null || _loading || _shipSelector.SelectedIndex < 0) return;
        SaveData(_saveData);
    }

    /// <summary>
    /// Raises <see cref="DataModified"/> when a ship-detail control changes so pending
    /// edits are tracked and prompt to be saved instead of being lost silently.
    /// </summary>
    private void OnShipDetailChanged(object? sender, EventArgs e)
    {
        if (_loading || _suppressShipDetailChanged) return;
        DataModified?.Invoke(this, EventArgs.Empty);
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var ships = playerState.GetArray("ShipOwnership");
            if (ships == null || _shipSelector.SelectedIndex < 0) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= ships.Length) return;

            var ship = ships.GetObject(idx);
            WriteShipDetail(ship, playerState, idx);

            _inventoryGrid.SaveInventory(ship.GetObject("Inventory"));
            _techGrid.SaveInventory(ship.GetObject("Inventory_TechOnly"));

            // Save customisation tab data (scene resource and CCD fields) when
            // the customisation tab is enabled and has been loaded for this ship.
            if (_customisationTabEnabled && _currentCustomisationShipIndex == idx)
                SaveCustomisationToCcd(playerState, idx);
        }
        catch { }
    }

    /// <summary>
    /// Commits the currently displayed ship-detail controls into the in-memory save for the
    /// displayed ship. Called before the selection changes so unsaved edits are not
    /// overwritten when the user switches to another ship. In-memory only, no disk I/O.
    /// </summary>
    private void CommitCurrentShipDetail()
    {
        if (_saveData == null || _playerState == null || _shipOwnership == null) return;
        if (_currentShipIndex < 0 || _currentShipIndex >= _shipOwnership.Length) return;
        try
        {
            var ship = _shipOwnership.GetObject(_currentShipIndex);
            if (ship == null) return;

            WriteShipDetail(ship, _playerState, _currentShipIndex);

            if (_customisationTabEnabled && _currentCustomisationShipIndex == _currentShipIndex)
                SaveCustomisationToCcd(_playerState, _currentShipIndex);
        }
        catch { }
    }

    /// <summary>
    /// Writes the current ship-detail control values into the given ship object.
    /// </summary>
    private void WriteShipDetail(JsonObject ship, JsonObject playerState, int idx)
    {
        var selectedTypeItem = _shipType.SelectedItem as StarshipLogic.ShipTypeItem;

        var values = new StarshipLogic.ShipSaveValues
        {
            Name = _shipName.Text,
            SelectedTypeName = selectedTypeItem?.InternalName,
            CustomFilename = selectedTypeItem?.CustomFilename,
            ClassIndex = _shipClass.SelectedIndex,
            OriginalClassIndex = _originalClassIndex,
            Seed = _shipSeed.Text,
            // Use raw values for unmodified fields to prevent any
            // precision loss from the UI control text round-trip.
            Damage = _damageField.UserModified
                ? (_damageField.NumericValue ?? 0.0)
                : (_rawShipStatValues?.GetValueOrDefault("^SHIP_DAMAGE") ?? _damageField.NumericValue ?? 0.0),
            Shield = _shieldField.UserModified
                ? (_shieldField.NumericValue ?? 0.0)
                : (_rawShipStatValues?.GetValueOrDefault("^SHIP_SHIELD") ?? _shieldField.NumericValue ?? 0.0),
            Hyperdrive = _hyperdriveField.UserModified
                ? (_hyperdriveField.NumericValue ?? 0.0)
                : (_rawShipStatValues?.GetValueOrDefault("^SHIP_HYPERDRIVE") ?? _hyperdriveField.NumericValue ?? 0.0),
            Maneuver = _maneuverField.UserModified
                ? (_maneuverField.NumericValue ?? 0.0)
                : (_rawShipStatValues?.GetValueOrDefault("^SHIP_AGILE") ?? _maneuverField.NumericValue ?? 0.0),
            // Pass display text so the saved JSON reproduces the exact text
            // the user sees (or the original save file text if unmodified).
            DamageText = _damageField.UserModified ? _damageField.DisplayText : null,
            ShieldText = _shieldField.UserModified ? _shieldField.DisplayText : null,
            HyperdriveText = _hyperdriveField.UserModified ? _hyperdriveField.DisplayText : null,
            ManeuverText = _maneuverField.UserModified ? _maneuverField.DisplayText : null,
            UseOldColours = _useOldColours.Checked,
            ShipIndex = idx,
            PrimaryShipIndex = _primaryShipIndex,
            RawStatValues = _rawShipStatValues
        };

        StarshipLogic.SaveShipData(ship, playerState, values);
    }

    private void OnShipSelected(object? sender, EventArgs e)
    {
        // Commit the previously displayed ship's edits before switching away, so unsaved
        // changes are not overwritten by the new ship's data. Skipped during programmatic
        // loads (the selection is set while _loading is true).
        if (!_loading && !_suppressDetailCommit)
            CommitCurrentShipDetail();

        // Freeze painting on the entire panel to prevent visible intermediate
        // redraws while grids are torn down and rebuilt. Without this, switching
        // between corvette and non-corvette ships (which have different layouts)
        // causes a visible glitch as controls are removed and re-added.
        RedrawHelper.Suspend(this);
        SuspendLayout();
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0) return;
            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;
            _currentShipIndex = idx;

            var ship = _shipOwnership.GetObject(idx);
            var data = StarshipLogic.LoadShipData(ship, _playerState, idx);

            _suppressShipDetailChanged = true;
            try
            {
                _shipName.Text = data.Name;
                SelectShipTypeByName(data.ShipTypeName, data.IsResourceModified, data.IsResourceModified ? data.Filename : null);
                SetStarshipMaxSupportedLabels(data.Filename);
                _shipSeed.Text = data.Seed;
                _shipClass.SelectedIndex = data.ClassIndex;
                _originalClassIndex = data.ClassIndex;
                _useOldColours.Checked = data.UseOldColours;
            }
            finally { _suppressShipDetailChanged = false; }

            // Set owner type BEFORE loading inventories so the item picker
            // filters reflect the correct ship type on the very first load
            // (avoids a redundant auto-refresh cycle).
            // Batch both grids' owner-type changes so the expensive
            // PopulateTypeFilter runs at most once per grid (after
            // LoadInventory) instead of eagerly on each SetInventoryOwnerType.
            string ownerType = StarshipLogic.GetOwnerTypeForShip(data.ShipTypeName);
            _techGrid.BeginBatchUpdate();
            _inventoryGrid.BeginBatchUpdate();
            try
            {
                _techGrid.SetInventoryOwnerType(ownerType);
                _inventoryGrid.SetInventoryOwnerType(ownerType);
            }
            finally
            {
                _inventoryGrid.EndBatchUpdate();
                _techGrid.EndBatchUpdate();
            }

            _inventoryGrid.LoadInventory(data.Inventory);
            ApplyPinnedSlotsForSelectedShip();

            // Set corvette context for tech grid so CV_ items resolve to actual base parts
            bool isCorvette = StarshipLogic.IsCorvette(data.Filename);
            if (isCorvette && _saveData != null)
                _techGrid.SetCorvetteContext(_saveData, idx);
            else
                _techGrid.ClearCorvetteContext();

            _techGrid.LoadInventory(data.TechInventory);

            _inventoryGrid.SetMaxSupportedLabel(data.CargoMaxLabel);
            _techGrid.SetMaxSupportedLabel(data.TechMaxLabel);
            _inventoryGrid.SetExportFileName(data.InvExportFileName);
            _techGrid.SetExportFileName(data.TechExportFileName);
            var cfg = ExportConfig.Instance;
            string cargoExportFilter = ExportConfig.BuildDialogFilter(cfg.StarshipCargoExt, "Ship cargo inventory");
            string cargoImportFilter = ExportConfig.BuildImportFilter(cfg.StarshipCargoExt, "Ship cargo inventory");
            _inventoryGrid.SetExportFileFilter(cargoExportFilter, cargoImportFilter, cfg.StarshipCargoExt.TrimStart('.'));
            string techExportFilter = ExportConfig.BuildDialogFilter(cfg.StarshipTechExt, "Ship tech inventory");
            string techImportFilter = ExportConfig.BuildImportFilter(cfg.StarshipTechExt, "Ship tech inventory");
            _techGrid.SetExportFileFilter(techExportFilter, techImportFilter, cfg.StarshipTechExt.TrimStart('.'));

            _suppressShipDetailChanged = true;
            try
            {
                try { _damageField.SetValueWithText(data.Damage, data.DamageText); } catch { _damageField.NumericValue = 0; }
                try { _shieldField.SetValueWithText(data.Shield, data.ShieldText); } catch { _shieldField.NumericValue = 0; }
                try { _hyperdriveField.SetValueWithText(data.Hyperdrive, data.HyperdriveText); } catch { _hyperdriveField.NumericValue = 0; }
                try { _maneuverField.SetValueWithText(data.Maneuver, data.ManeuverText); } catch { _maneuverField.NumericValue = 0; }
            }
            finally { _suppressShipDetailChanged = false; }

            // Store raw stat values for preservation before limits clamp the NUDs
            _rawShipStatValues = new Dictionary<string, double>
            {
                ["^SHIP_DAMAGE"] = data.Damage,
                ["^SHIP_SHIELD"] = data.Shield,
                ["^SHIP_HYPERDRIVE"] = data.Hyperdrive,
                ["^SHIP_AGILE"] = data.Maneuver,
            };

            // Toggle corvette extras panel and warning
            _corvetteExtrasPanel.Visible = isCorvette;
            _corvetteWarningLabel.Visible = isCorvette;

            // Update optimise indicator for corvettes
            if (isCorvette)
                UpdateOptimiseIndicator(idx);

            // Load customisation tab (enable/disable based on corvette status,
            // populate scene combo and dynamic part controls from CCD).
            LoadCustomisationTab(isCorvette, data.Filename, idx);
        }
        catch { }
        finally
        {
            ResumeLayout(true);
            RedrawHelper.Resume(this);
        }
    }

    private void OnShipNameChanged(object? sender, EventArgs e)
    {
        if (_shipSelector.SelectedIndex < 0 || _shipSelector.Items.Count == 0) return;
        var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;

        // Resolve class label from the class combo
        string cls = _shipClass.SelectedIndex >= 0 && _shipClass.SelectedIndex < StarshipLogic.ShipClasses.Length
            ? StarshipLogic.ShipClasses[_shipClass.SelectedIndex] : "?";

        string newName;
        if (string.IsNullOrWhiteSpace(_shipName.Text))
        {
            // No custom name stored, drop to ship type for naming
            string shipType = (_shipType.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName ?? "Ship";
            newName = $"[{item.DataIndex + 1}] {shipType} - {cls}";
        }
        else
        {
            newName = $"[{item.DataIndex + 1}] {_shipName.Text} - {cls}";
        }

        item.DisplayName = newName;
        int idx = _shipSelector.SelectedIndex;
        _shipSelector.SelectedIndexChanged -= OnShipSelected;
        _shipSelector.Items.RemoveAt(idx);
        _shipSelector.Items.Insert(idx, item);
        _shipSelector.SelectedIndex = idx;
        _shipSelector.SelectedIndexChanged += OnShipSelected;
    }

    private string GetCurrentPinnedInventoryKey()
    {
        if (_shipSelector.SelectedIndex < 0)
            return "StarshipCargo:none";

        var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
        return $"StarshipCargo:{item.DataIndex}";
    }

    private void ApplyPinnedSlotsForSelectedShip()
    {
        if (_shipSelector.SelectedIndex < 0)
        {
            _inventoryGrid.SetPinnedSlots([]);
            return;
        }

        var pinned = AppConfig.Instance.GetPinnedSlots(_saveScopeKey, GetCurrentPinnedInventoryKey());
        _inventoryGrid.SetPinnedSlots(pinned);
    }

    private void OnPinnedSlotsChanged(object? sender, EventArgs e)
    {
        if (_shipSelector.SelectedIndex < 0)
            return;

        AppConfig.Instance.SetPinnedSlots(_saveScopeKey, GetCurrentPinnedInventoryKey(), _inventoryGrid.GetPinnedSlots());
    }

    private void OnAutoStackToStorageRequested(object? sender, EventArgs e)
    {
        if (!TryGetSelectedShipCargoInventory(out var cargoInventory, out _))
            return;

        var pinned = new HashSet<(int x, int y)>(_inventoryGrid.GetPinnedSlots());
        bool changed = InventoryBulkActions.AutoStackCargoToChests(cargoInventory, _playerState!, out _, out _, pinned);
        if (!changed)
            return;

        _inventoryGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackToFreighterRequested(object? sender, EventArgs e)
    {
        if (!TryGetSelectedShipCargoInventory(out var cargoInventory, out _))
            return;

        if (_playerState?.GetObject("FreighterInventory") is not JsonObject freighterInventory)
            return;

        var pinned = new HashSet<(int x, int y)>(_inventoryGrid.GetPinnedSlots());
        bool changed = InventoryBulkActions.AutoStackFromInventoryToInventory(
            cargoInventory,
            freighterInventory,
            out _,
            out _,
            pinned);

        if (!changed)
            return;

        _inventoryGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackSelectedSlotToStorageRequested(object? sender, InventoryGridPanel.AutoStackSlotRequestEventArgs e)
    {
        if (!TryGetContextAutoStackCargo(e, out var cargoInventory, out var pinned, out var sourceSlotFilter, out var sourceItemIdFilter))
            return;

        bool changed = InventoryBulkActions.AutoStackCargoToChests(
            cargoInventory,
            _playerState!,
            out _,
            out _,
            pinned,
            sourceSlotFilter,
            sourceItemIdFilter);

        if (!changed)
            return;

        _inventoryGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackSelectedSlotToFreighterRequested(object? sender, InventoryGridPanel.AutoStackSlotRequestEventArgs e)
    {
        if (!TryGetContextAutoStackCargo(e, out var cargoInventory, out var pinned, out var sourceSlotFilter, out var sourceItemIdFilter))
            return;

        if (_playerState?.GetObject("FreighterInventory") is not JsonObject freighterInventory)
            return;

        bool changed = InventoryBulkActions.AutoStackFromInventoryToInventory(
            cargoInventory,
            freighterInventory,
            out _,
            out _,
            pinned,
            sourceSlotFilter,
            sourceItemIdFilter);

        if (!changed)
            return;

        _inventoryGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private bool TryGetSelectedShipCargoInventory(out JsonObject cargoInventory, out int shipIndex)
    {
        cargoInventory = null!;
        shipIndex = -1;

        if (_shipOwnership == null || _shipSelector.SelectedIndex < 0)
            return false;

        var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
        shipIndex = item.DataIndex;
        if (shipIndex < 0 || shipIndex >= _shipOwnership.Length)
            return false;

        var ship = _shipOwnership.GetObject(shipIndex);
        cargoInventory = _inventoryGrid.GetLoadedInventory() ?? ship?.GetObject("Inventory")!;
        return cargoInventory != null;
    }

    private bool TryGetContextAutoStackCargo(
        InventoryGridPanel.AutoStackSlotRequestEventArgs request,
        out JsonObject cargoInventory,
        out HashSet<(int x, int y)> pinned,
        out (int x, int y) sourceSlotFilter,
        out string sourceItemIdFilter)
    {
        cargoInventory = null!;
        pinned = null!;
        sourceSlotFilter = default;
        sourceItemIdFilter = request.ItemId;

        if (!TryGetSelectedShipCargoInventory(out cargoInventory, out _))
            return false;

        pinned = new HashSet<(int x, int y)>(_inventoryGrid.GetPinnedSlots());
        sourceSlotFilter = (request.X, request.Y);

        if (pinned.Contains(sourceSlotFilter))
        {
            MessageBox.Show(this, 
                UiStrings.Get("inventory.auto_stack_pinned_slot_blocked"),
                UiStrings.Get("dialog.info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    private void OnDeleteShip(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _playerState == null || _shipSelector.SelectedIndex < 0) return;

            // Prevent deleting the last valid ship.
            // Use CountValidShips because the array may contain invalidated slots.
            if (StarshipLogic.CountValidShips(_shipOwnership) <= 1)
            {
                MessageBox.Show(this, UiStrings.Get("starship.cannot_delete_only"), UiStrings.Get("starship.delete_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(this, 
                UiStrings.Get("starship.delete_confirm"),
                UiStrings.Get("starship.delete_title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            // If the ship is a corvette, invalidate its PlayerShipBase entry
            // so that building objects don't remain orphaned in the save.
            if (IsShipCorvette(ship) && _saveData != null)
                InvalidateCorvetteBaseForShip(ship, idx);

            // Invalidate the ship in place - do NOT remove from array.
            // The slot stays in the ShipOwnership array (preserving index alignment
            // with the parallel ShipUsesLegacyColours array) but is
            // filtered out by BuildShipList().
            StarshipLogic.DeleteShipData(ship);

            // Clear the corresponding CharacterCustomisationData entry so that
            // player-built ship customisation (DescriptorGroups, colours, etc.)
            // does not leak into a future ship that re-uses this slot.
            StarshipLogic.ResetShipCustomisation(
                _playerState.GetArray("CharacterCustomisationData"), idx);

            // Reset the slot's legacy-colour flag so a future import that cannot carry
            // the flag (old NMSE exports, IO Tool ZIPs) does not inherit the deleted
            // ship's colour mode.
            StarshipLogic.SetShipUsesLegacyColours(_playerState, idx, false);

            // If the deleted ship was the primary ship, reassign to the first valid ship.
            // Since we don't remove from the array, non-primary indices remain correct.
            if (idx == _primaryShipIndex)
            {
                _primaryShipIndex = StarshipLogic.FindFirstValidShipIndex(_shipOwnership);
                if (_primaryShipIndex < 0) _primaryShipIndex = 0;
                RawNumberGuard.SetInt(_playerState, "PrimaryShip", _primaryShipIndex);
            }
            _primaryShipLabel.Text = UiStrings.Format("starship.primary_label", StarshipLogic.GetPrimaryShipName(_shipOwnership, _primaryShipIndex));

            // Rebuild the ship list (BuildShipList skips invalidated slots)
            int selIdx = _shipSelector.SelectedIndex;
            _shipSelector.Items.Clear();
            var shipList = StarshipLogic.BuildShipList(_shipOwnership);
            foreach (var shipItem in shipList)
                _shipSelector.Items.Add(shipItem);

            if (_shipSelector.Items.Count > 0)
                _shipSelector.SelectedIndex = Math.Min(selIdx, _shipSelector.Items.Count - 1);
        }
        catch { }
    }

    private void OnMoveShipToArchive(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _playerState == null || _shipSelector.SelectedIndex < 0) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            // Flush unsaved ship-detail edits into the in-memory save before archiving
            SyncCurrentShipToModel();

            var ship = _shipOwnership.GetObject(idx);

            // Block Corvettes
            if (IsShipCorvette(ship))
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_corvette_blocked"),
                    UiStrings.Get("starship.archive_move_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Block Primary ship
            if (idx == _primaryShipIndex)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_primary_blocked"),
                    UiStrings.Get("starship.archive_move_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Require at least one remaining valid ship after the move
            if (StarshipLogic.CountValidShips(_shipOwnership) <= 1)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.cannot_delete_only"),
                    UiStrings.Get("starship.archive_move_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Find empty archive slot
            var archivedShips = _playerState.GetArray("ArchivedShipOwnership");
            if (archivedShips == null)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_no_slots"),
                    UiStrings.Get("starship.archive_move_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int archIdx = StarshipLogic.FindEmptyArchivedShipSlot(archivedShips);
            if (archIdx < 0)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_no_slots"),
                    UiStrings.Get("starship.archive_move_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirm
            var result = MessageBox.Show(this,
                UiStrings.Get("starship.archive_move_confirm"),
                UiStrings.Get("starship.archive_move_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            // Get legacy colours flag for this ship
            bool usesLegacyColours = StarshipLogic.GetShipUsesLegacyColours(_playerState, idx);

            var archivedSlot = archivedShips.GetObject(archIdx);
            var ccdArray = _playerState.GetArray("CharacterCustomisationData");
            StarshipLogic.MoveShipToArchive(ship, idx, archivedSlot, ccdArray, usesLegacyColours);

            _primaryShipLabel.Text = UiStrings.Format("starship.primary_label", StarshipLogic.GetPrimaryShipName(_shipOwnership, _primaryShipIndex));

            // Rebuild the ship list and select the next available ship
            int selIdx = _shipSelector.SelectedIndex;
            _shipSelector.Items.Clear();
            var shipList = StarshipLogic.BuildShipList(_shipOwnership);
            foreach (var shipItem in shipList)
                _shipSelector.Items.Add(shipItem);

            if (_shipSelector.Items.Count > 0)
                _shipSelector.SelectedIndex = Math.Clamp(selIdx, 0, _shipSelector.Items.Count - 1);

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportShipFromArchive(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _playerState == null) return;

            var archivedShips = _playerState.GetArray("ArchivedShipOwnership");
            if (archivedShips == null)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_empty"),
                    UiStrings.Get("starship.archive_import_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build list of archived ships
            var archivedList = StarshipLogic.BuildArchivedShipList(archivedShips);
            if (archivedList.Count == 0)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_empty"),
                    UiStrings.Get("starship.archive_import_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Find empty list slot
            int emptyListIdx = StarshipLogic.FindEmptySlot(_shipOwnership);
            if (emptyListIdx < 0)
            {
                MessageBox.Show(this,
                    UiStrings.Get("starship.archive_no_list_slots"),
                    UiStrings.Get("starship.archive_import_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show selection dialog
            int selectedArchiveIdx = ShowArchiveSelectionDialog(
                archivedList.Select(a => a.DisplayName).ToList(),
                UiStrings.Get("starship.archive_import_title"));
            if (selectedArchiveIdx < 0) return;

            var selectedItem = archivedList[selectedArchiveIdx];

            // Confirm
            var result = MessageBox.Show(this,
                UiStrings.Get("starship.archive_import_confirm"),
                UiStrings.Get("starship.archive_import_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            var archivedSlot = archivedShips.GetObject(selectedItem.ArchiveIndex);
            var targetShip = _shipOwnership.GetObject(emptyListIdx);
            var ccdArray = _playerState.GetArray("CharacterCustomisationData");
            StarshipLogic.ImportShipFromArchive(archivedSlot, targetShip, emptyListIdx, ccdArray, _playerState);

            // Rebuild ship list and select the newly imported ship
            _shipSelector.Items.Clear();
            var shipList = StarshipLogic.BuildShipList(_shipOwnership);
            foreach (var shipItem in shipList)
                _shipSelector.Items.Add(shipItem);

            for (int i = 0; i < _shipSelector.Items.Count; i++)
            {
                if (((StarshipLogic.ShipListItem)_shipSelector.Items[i]!).DataIndex == emptyListIdx)
                {
                    _shipSelector.SelectedIndex = i;
                    break;
                }
            }

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Shows a modal dialog presenting a list of items for the user to select from.
    /// Returns the index of the selected item in <paramref name="items"/>, or -1 if cancelled.
    /// </summary>
    private int ShowArchiveSelectionDialog(List<string> items, string title)
    {
        using var form = new Form
        {
            Text = title,
            Width = 420,
            Height = 320,
            MinimumSize = new Size(320, 240),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.Sizable,
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = title + ":",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 4),
        };
        layout.Controls.Add(label, 0, 0);

        var listBox = new ListBox
        {
            Dock = DockStyle.Fill,
            SelectionMode = SelectionMode.One,
        };
        foreach (var item in items)
            listBox.Items.Add(item);
        if (listBox.Items.Count > 0)
            listBox.SelectedIndex = 0;
        layout.Controls.Add(listBox, 0, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 0),
        };
        var cancelBtn = new Button { Text = UiStrings.Get("common.cancel"), DialogResult = DialogResult.Cancel, AutoSize = true };
        var importBtn = new Button { Text = UiStrings.Get("common.import"), DialogResult = DialogResult.OK, AutoSize = true };
        form.AcceptButton = importBtn;
        form.CancelButton = cancelBtn;
        buttonPanel.Controls.Add(cancelBtn);
        buttonPanel.Controls.Add(importBtn);
        layout.Controls.Add(buttonPanel, 0, 2);

        form.Controls.Add(layout);

        // Double-click selects
        listBox.DoubleClick += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };

        if (form.ShowDialog(FindForm()) != DialogResult.OK) return -1;
        return listBox.SelectedIndex;
    }

    private void OnExportShip(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0) return;

            // Flush unsaved ship-detail edits into the in-memory save before serialising
            SyncCurrentShipToModel();

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            bool isCorvette = IsShipCorvette(ship);

            // For corvettes, prevent export if the corvette is the primary ship
            if (isCorvette)
            {
                if (!CheckCorvettePrimarySafety("exporting")) return;
            }

            var cfg = ExportConfig.Instance;
            string shipName = _shipName.Text ?? "";
            string type = (_shipType.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName ?? "";
            string cls = _shipClass.SelectedIndex >= 0 && _shipClass.SelectedIndex < StarshipLogic.ShipClasses.Length
                ? StarshipLogic.ShipClasses[_shipClass.SelectedIndex] : "C";
            var vars = new Dictionary<string, string>
            {
                ["ship_name"] = shipName,
                ["type"] = type,
                ["class"] = cls
            };

            string template = isCorvette ? cfg.CorvetteTemplate : cfg.StarshipTemplate;
            string ext = isCorvette ? cfg.CorvetteExt : cfg.StarshipExt;
            string label = isCorvette ? "Corvette files" : "Starship files";

            // For corvettes, find the base data before showing the dialog
            JsonObject? baseObj = null;
            if (isCorvette && _saveData != null)
            {
                var playerState = _saveData.GetObject("PlayerStateData");
                var bases = playerState?.GetArray("PersistentPlayerBases");
                int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx);
                if (baseIdx < 0)
                {
                    MessageBox.Show(this, UiStrings.Get("starship.corvette_no_base"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                baseObj = bases!.GetObject(baseIdx);
            }

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(ext, label),
                DefaultExt = ext.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(template, ext, vars)
            };

            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                // Build a wrapper object with the ship data and CCD as siblings.
                // CCD is stored externally under its original game key name,
                // NOT inside the ship JSON block.
                var export = new JsonObject();
                export.Set("Ship", ship);
                if (isCorvette && baseObj != null)
                    export.Set("Base", baseObj);

                var ccdArray = _playerState?.GetArray("CharacterCustomisationData");
                var ccdEntry = StarshipLogic.GetShipCustomisation(ccdArray, idx);
                if (ccdEntry != null)
                    export.Set("CharacterCustomisationData", ccdEntry);

                // ShipUsesLegacyColours lives in PlayerStateData as an array parallel
                // to ShipOwnership, so it has to be carried alongside the ship block.
                // Always written, so the file is unambiguous about which mode it wants.
                export.Set("UsesLegacyColours", StarshipLogic.GetShipUsesLegacyColours(_playerState, idx));

                export.ExportToFile(dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportShip(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _playerState == null || _saveData == null) return;
            if (_shipSelector.SelectedIndex < 0) return;

            var cfg = ExportConfig.Instance;

            // Accept all ship file formats in one import dialog
            string filter = ExportConfig.BuildImportFilter(cfg.StarshipExt, "Ship files",
                cfg.CorvetteExt, ".nmsship");

            using var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            // --- Parse the imported file ---
            var zipResult = StarshipLogic.TryReadNmsshipZip(dialog.FileName);

            JsonObject? importedShip;
            JsonObject? importedBase = null;
            JsonObject? zipCcd = null;
            // CCD from the wrapper-level "CharacterCustomisationData" key (new format)
            JsonObject? wrapperCcd = null;
            // Legacy-colour flag from the wrapper-level "UsesLegacyColours" key.
            // Stays null for formats that cannot carry it (IO Tool ZIPs, plain ship
            // JSON, NomNom envelopes) and for files exported before it was added.
            bool? importedLegacyColours = null;

            if (zipResult != null)
            {
                importedShip = zipResult.Value.ship;
                zipCcd = zipResult.Value.ccd;
                if (zipResult.Value.objects != null)
                {
                    importedBase = new JsonObject();
                    importedBase.Set("Objects", zipResult.Value.objects);
                }
            }
            else
            {
                var imported = JsonObject.ImportFromFile(dialog.FileName);

                // Check for wrapper format: { Ship, [Base], [CharacterCustomisationData], [UsesLegacyColours] }
                importedShip = imported.GetObject("Ship");
                if (importedShip != null)
                {
                    importedBase = imported.GetObject("Base");
                    wrapperCcd = imported.GetObject("CharacterCustomisationData");
                    importedLegacyColours = StarshipLogic.TryGetExportedLegacyColours(imported);
                }
                else
                {
                    // Plain ship file or Data envelope wrapper
                    if (InventoryImportHelper.IsNomNomWrapper(imported))
                    {
                        var data = imported.GetObject("Data");
                        if (data != null)
                        {
                            var entity = data.GetObject("Starship") ?? data.GetObject("Ship");
                            if (entity != null) importedShip = entity;
                        }
                    }
                    else
                    {
                        importedShip = imported;
                    }
                }
            }

            if (importedShip == null)
            {
                MessageBox.Show(this, 
                    UiStrings.Get("starship.no_valid_ship"),
                    UiStrings.Get("common.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Determine if the imported ship is a corvette
            bool importedIsCorvette = IsShipCorvette(importedShip);

            // --- Decide target slot: empty slot if available, otherwise current ---
            int emptyIdx = StarshipLogic.FindEmptySlot(_shipOwnership);
            bool importToEmpty = emptyIdx >= 0;
            int targetIdx;

            if (importToEmpty)
            {
                targetIdx = emptyIdx;
            }
            else
            {
                // Import over the currently selected ship
                var currentItem = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
                targetIdx = currentItem.DataIndex;
                if (targetIdx >= _shipOwnership.Length) return;

                // Confirm before overwriting an existing ship
                var overwriteResult = MessageBox.Show(this, 
                    UiStrings.Get("starship.import_overwrite_confirm"),
                    UiStrings.Get("starship.import_overwrite_title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (overwriteResult != DialogResult.Yes) return;
            }

            // For corvette imports over an existing slot, enforce primary safety
            if (importedIsCorvette && !importToEmpty)
            {
                if (!CheckCorvettePrimarySafety("importing")) return;
            }

            var targetShip = _shipOwnership.GetObject(targetIdx);

            // If we're overwriting an existing ship, clean up the old data first
            if (!importToEmpty)
            {
                // If the current ship is a corvette, clean up its base data
                if (IsShipCorvette(targetShip))
                    InvalidateCorvetteBaseForShip(targetShip, targetIdx);
            }

            // Extract CCD from the legacy __ShipCustomisation key (backwards compat)
            var legacyCcd = ExtractLegacyShipCustomisation(importedShip);

            // Copy all properties from imported ship to target slot.
            // UsesLegacyColours is skipped: it belongs in the PlayerStateData array,
            // not on the ship, and copying it would write a stray key into the save.
            foreach (var name in importedShip.Names())
            {
                if (name == "__ShipCustomisation") continue;
                if (name == "UsesLegacyColours") continue;
                targetShip.Set(name, importedShip.Get(name));
            }

            // Remove the keys that don't belong on a live ship object if they leaked
            targetShip.Remove("__ShipCustomisation");
            targetShip.Remove("UsesLegacyColours");

            // Determine CCD source (priority order):
            //   1. ZIP ccd.json (if present and non-default)
            //   2. Wrapper-level CharacterCustomisationData (new format)
            //   3. Legacy __ShipCustomisation embedded in ship JSON (old format)
            JsonObject? ccdToApply = legacyCcd;
            if (wrapperCcd != null && !StarshipLogic.IsCcdDefault(wrapperCcd))
                ccdToApply = wrapperCcd;
            if (zipCcd != null && !StarshipLogic.IsCcdDefault(zipCcd))
                ccdToApply = zipCcd;

            var ccdArray = _playerState.GetArray("CharacterCustomisationData");
            StarshipLogic.SetShipCustomisation(ccdArray, targetIdx, ccdToApply);

            // Apply the legacy-colour flag only when the file actually carried one.
            // Files that cannot express it (older NMSE exports, IO Tool ZIPs, plain
            // ship JSON) leave the destination slot's existing flag untouched.
            if (importedLegacyColours.HasValue)
                StarshipLogic.SetShipUsesLegacyColours(_playerState, targetIdx, importedLegacyColours.Value);

            // Import base building objects for corvette ships
            if (importedBase != null && importedIsCorvette)
            {
                var bases = _playerState.GetArray("PersistentPlayerBases");
                int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, targetIdx);
                if (baseIdx >= 0)
                {
                    // Overwrite the existing base with the imported data
                    var existingBase = bases!.GetObject(baseIdx);
                    foreach (var name in importedBase.Names())
                        existingBase.Set(name, importedBase.Get(name));
                    // Ensure UserData points to the correct target slot
                    existingBase.Set("UserData", targetIdx);
                }
                else if (bases != null)
                {
                    // No existing base for this slot. When importing from a NMS
					// Model IO Tool ZIP the imported base only contains Objects,
					// so it lacks BaseType and all other required fields.
					// Why don't they include this and instead manually re-write it
					// in the tool? No idea. So:
					// We build a complete PlayerShipBase entry from scratch for the
					// import, so that the game can properly recognise it.
                    JsonObject baseToAdd;
                    if (importedBase.GetObject("BaseType") == null)
                    {
                        var objects = importedBase.GetArray("Objects") ?? new JsonArray();
                        baseToAdd = StarshipLogic.CreatePlayerShipBase(targetIdx, objects);
                    }
                    else
                    {
                        baseToAdd = importedBase;
                    }

                    baseToAdd.Set("UserData", targetIdx);
                    bases.Add(baseToAdd);
                }
            }

            _suppressDetailCommit = true;
            try
            {
                if (importToEmpty)
                {
                    // Rebuild ship list and select the newly imported ship
                    _shipSelector.Items.Clear();
                    var shipList = StarshipLogic.BuildShipList(_shipOwnership);
                    foreach (var shipItem in shipList)
                        _shipSelector.Items.Add(shipItem);

                    for (int i = 0; i < _shipSelector.Items.Count; i++)
                    {
                        if (((StarshipLogic.ShipListItem)_shipSelector.Items[i]!).DataIndex == targetIdx)
                        {
                            _shipSelector.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    // Refresh display for in-place import
                    OnShipSelected(this, EventArgs.Empty);
                }
            }
            finally
            {
                _suppressDetailCommit = false;
            }

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void SetInventoryClass(JsonObject? inventory, string cls)
    {
        if (inventory == null) return;
        try
        {
            var classObj = inventory.GetObject("Class");
            classObj?.Set("InventoryClass", cls);
        }
        catch { }
    }

    private void OnMakePrimary(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0) return;
            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            // Warn if the selected ship is a Corvette
            var ship = _shipOwnership.GetObject(idx);
            if (IsShipCorvette(ship))
            {
                var result = MessageBox.Show(this, 
                    UiStrings.Get("starship.corvette_primary_warning"),
                    UiStrings.Get("starship.corvette_warning_title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes) return;
            }

            _primaryShipIndex = idx;
            _primaryShipLabel.Text = UiStrings.Format("starship.primary_label", StarshipLogic.GetPrimaryShipName(_shipOwnership, _primaryShipIndex));
        }
        catch { }
    }

    private void OnSnapshotTech(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0 || _saveData == null) return;

            // Flush unsaved ship-detail edits into the in-memory save before serialising
            SyncCurrentShipToModel();

            if (!CheckCorvettePrimarySafety("snapshotting tech for")) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            // Build a ship snapshot that includes everything EXCEPT the cargo inventory
            var shipSnapshot = new JsonObject();
            foreach (var key in ship.Names())
            {
                if (key == "Inventory") continue; // Skip cargo inventory
                shipSnapshot.Set(key, ship.Get(key));
            }

            // Find the corvette's matching PlayerShipBase (same as export)
            JsonObject? baseObj = null;
            {
                var playerState = _saveData.GetObject("PlayerStateData");
                var bases = playerState?.GetArray("PersistentPlayerBases");
                int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx);
                if (baseIdx >= 0)
                    baseObj = bases!.GetObject(baseIdx);
            }

            var cfg = ExportConfig.Instance;
            string shipName = _shipName.Text ?? "";
            string type = (_shipType.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName ?? "";
            string cls = _shipClass.SelectedIndex >= 0 && _shipClass.SelectedIndex < StarshipLogic.ShipClasses.Length
                ? StarshipLogic.ShipClasses[_shipClass.SelectedIndex] : "C";
            var vars = new Dictionary<string, string>
            {
                ["ship_name"] = shipName,
                ["type"] = type,
                ["class"] = cls
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(cfg.CorvetteSnapshotExt, "Corvette snapshot files"),
                DefaultExt = cfg.CorvetteSnapshotExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(cfg.CorvetteSnapshotTemplate, cfg.CorvetteSnapshotExt, vars)
            };

            if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            {
                // Create combined export: Ship (without cargo) + Base
                var export = new JsonObject();
                export.Set("Ship", shipSnapshot);
                if (baseObj != null)
                    export.Set("Base", baseObj);
                export.Set("UsesLegacyColours", StarshipLogic.GetShipUsesLegacyColours(_playerState, idx));
                export.ExportToFile(dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("starship.snapshot_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportSnapshot(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0 || _saveData == null) return;
            if (!CheckCorvettePrimarySafety("importing snapshot for")) return;

            var cfg = ExportConfig.Instance;

            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildOpenFilter(cfg.CorvetteSnapshotExt, "Corvette snapshot files")
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);

            var importedShip = imported.GetObject("Ship");
            if (importedShip == null)
            {
                MessageBox.Show(this, UiStrings.Get("starship.no_valid_ship"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            // Apply ship properties excluding cargo inventory - snapshots only
            // capture tech configuration, so we preserve the existing cargo slots.
            foreach (var name in importedShip.Names())
            {
                if (name == "Inventory") continue;
                if (name == "UsesLegacyColours") continue;
                ship.Set(name, importedShip.Get(name));
            }
            ship.Remove("UsesLegacyColours");

            // Apply the legacy-colour flag only when the snapshot carried one
            var snapshotLegacyColours = StarshipLogic.TryGetExportedLegacyColours(imported);
            if (snapshotLegacyColours.HasValue)
                StarshipLogic.SetShipUsesLegacyColours(_playerState, idx, snapshotLegacyColours.Value);

            // Import base data if present
            var importedBase = imported.GetObject("Base");
            if (importedBase != null)
            {
                {
                    var playerState = _saveData.GetObject("PlayerStateData");
                    var bases = playerState?.GetArray("PersistentPlayerBases");
                    int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx);
                    if (baseIdx >= 0)
                    {
                        var existingBase = bases!.GetObject(baseIdx);
                        foreach (var name in importedBase.Names())
                            existingBase.Set(name, importedBase.Get(name));
                    }
                }
            }

            // Refresh display without committing the pre-import control values back
            // over the snapshot we just applied.
            _suppressDetailCommit = true;
            try
            {
                OnShipSelected(this, EventArgs.Empty);
            }
            finally
            {
                _suppressDetailCommit = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool CheckCorvettePrimarySafety(string action)
    {
        if (_shipSelector.SelectedIndex < 0) return false;
        var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
        if (item.DataIndex == _primaryShipIndex)
        {
            MessageBox.Show(this, 
                UiStrings.Format("starship.corvette_primary_corruption", action),
                UiStrings.Get("starship.important_warning"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    private void OnOptimiseCorvette(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0 || _saveData == null) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            var playerState = _saveData.GetObject("PlayerStateData");
            var bases = playerState?.GetArray("PersistentPlayerBases");
            int result = StarshipLogic.OptimiseCorvetteBase(bases, idx);

            if (result < 0)
            {
                MessageBox.Show(this, UiStrings.Get("starship.corvette_no_base"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // After optimising, update indicator to green
            SetOptimiseIndicator(true);

            if (result == 0)
            {
                MessageBox.Show(this, 
                    UiStrings.Get("starship.optimise_already"),
                    UiStrings.Get("starship.optimise"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(this, 
                    UiStrings.Format("starship.optimise_done", result),
                    UiStrings.Get("starship.optimise"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, "Optimisation failed: {0}", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Sets the optimise indicator: green tick if optimised, orangered cross if not.
    /// </summary>
    private void SetOptimiseIndicator(bool isOptimised)
    {
        _optimiseIndicator.Text = isOptimised ? "\u2714" : "\u2718";
        _optimiseIndicator.ForeColor = isOptimised
            ? (ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.SuccessGreen : Color.Green)
            : (ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.ErrorRed : Color.OrangeRed);
    }

    /// <summary>
    /// Checks whether the corvette at the given ship index is already in
    /// optimised order and updates the indicator accordingly.
    /// </summary>
    private void UpdateOptimiseIndicator(int shipIndex)
    {
        if (_saveData == null || _shipOwnership == null) return;
        try
        {
            var ship = _shipOwnership.GetObject(shipIndex);
            var playerState = _saveData.GetObject("PlayerStateData");
            var bases = playerState?.GetArray("PersistentPlayerBases");
            bool optimised = StarshipLogic.IsCorvetteOptimised(bases, shipIndex);
            SetOptimiseIndicator(optimised);
        }
        catch
        {
            SetOptimiseIndicator(false);
        }
    }

    /// <summary>
    /// Returns whether a ship object represents a Corvette based on its resource filename.
    /// </summary>
    private static bool IsShipCorvette(JsonObject ship)
    {
        var resource = ship.GetObject("Resource");
        string filename = resource?.GetString("Filename") ?? "";
        return StarshipLogic.IsCorvette(filename);
    }

    /// <summary>
    /// Extracts the legacy <c>__ShipCustomisation</c> CCD entry from an imported ship
    /// object and removes it. This supports backwards compatibility with exports from
    /// older versions that embedded the CCD inside the ship JSON block with a
    /// non-standard key name.
    /// </summary>
    /// <param name="importedShip">The ship JSON object being imported.</param>
    /// <returns>The extracted CCD object, or <c>null</c> if not present.</returns>
    private static JsonObject? ExtractLegacyShipCustomisation(JsonObject importedShip)
    {
        try
        {
            return importedShip.GetObject("__ShipCustomisation");
        }
        catch { return null; }
    }

    /// <summary>
    /// Extracts the seed string (Seed[1]) from a ship object's Resource.
    /// Returns an empty string if the seed cannot be read.
    /// </summary>
    private static string GetShipSeed(JsonObject ship)
    {
        try
        {
            var resource = ship.GetObject("Resource");
            return resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Invalidates the corvette base entry associated with the given ship,
    /// clearing orphaned building objects from the save.
    /// </summary>
    private void InvalidateCorvetteBaseForShip(JsonObject ship, int shipIndex)
    {
        if (_playerState == null) return;
        var bases = _playerState.GetArray("PersistentPlayerBases");
        StarshipLogic.InvalidateCorvetteBase(bases, shipIndex);
    }

    // -------------------
    //  Customisation tab
    // -------------------

    /// <summary>
    /// Loads and displays the Customisation tab for the given ship.
    /// Disables the tab entirely for Corvette ships.
    /// </summary>
    private void LoadCustomisationTab(bool isCorvette, string resourceFilename, int shipIndex)
    {
        _currentCustomisationShipIndex = shipIndex;

        // Populate the scene combo with all known resource paths (do it once,
        // or whenever the database has been loaded after an initial empty state).
        if (_sceneCombo.Items.Count == 0)
            PopulateSceneCombo();

        if (isCorvette)
        {
            // Disable tab: corvettes have a completely different customisation system.
            SetCustomisationTabEnabled(false);
            _sceneCombo.Text = resourceFilename;
            RebuildCustomisationDynamicControls(null);
            ShowCustomisationInfoLabel(UiStrings.Get("starship.customisation_corvette_disabled"));
            return;
        }

        SetCustomisationTabEnabled(true);
        _sceneCombo.Text = resourceFilename;

        var config = ShipCustomisationDatabase.GetConfigByResource(resourceFilename);
        _currentCustomisationConfig = config;

        var ccdArray = _playerState?.GetArray("CharacterCustomisationData");
        var ccd = StarshipLogic.GetShipCustomisation(ccdArray, shipIndex);

        RebuildCustomisationDynamicControls(config);
        LoadCustomisationFromCcd(ccd, config);

        if (config == null)
            ShowCustomisationInfoLabel(UiStrings.Get("starship.customisation_no_config"));
        else
            HideCustomisationInfoLabel();
    }

    /// <summary>
    /// Enables or disables the Customisation tab page, switching back to
    /// Ship Details when the tab is being disabled while selected.
    /// </summary>
    private void SetCustomisationTabEnabled(bool enabled)
    {
        _customisationTabEnabled = enabled;
        if (!enabled && _outerTabs.SelectedTab == _customisationTabPage)
            _outerTabs.SelectedTab = _shipDetailsTabPage;
    }

    /// <summary>
    /// Populates the scene combo with all resource paths known to
    /// ShipCustomisationDatabase plus those from StarshipLogic.ShipInfo.
    /// </summary>
    private void PopulateSceneCombo()
    {
        _sceneCombo.BeginUpdate();
        _sceneCombo.Items.Clear();

        // Add paths from the customisation database first (proc ships with part data)
        foreach (var path in ShipCustomisationDatabase.AllResourcePaths)
            _sceneCombo.Items.Add(path);

        // Add remaining canonical paths from StarshipLogic that are not already listed
        foreach (var path in StarshipLogic.ShipInfo.Keys)
        {
            if (!_sceneCombo.Items.Contains(path))
                _sceneCombo.Items.Add(path);
        }

        _sceneCombo.EndUpdate();
    }

    /// <summary>
    /// Removes all dynamically created controls from the customisation content panel
    /// (all rows after row 0) and rebuilds them for the given config.
    /// </summary>
    private void RebuildCustomisationDynamicControls(ShipCustomisationConfig? config)
    {
        _customisationContent.SuspendLayout();

        // Detach the permanent info label before the disposal sweep so it is
        // not caught and disposed along with dynamic controls.
        if (_customisationInfoLabel.Parent != null)
            _customisationContent.Controls.Remove(_customisationInfoLabel);

        // Remove all rows after row 0 (the static scene row).
        // Dispose controls from row 1 onwards to avoid resource leaks.
        var toRemove = _customisationContent.Controls.Cast<Control>()
            .Where(c =>
            {
                var pos = _customisationContent.GetPositionFromControl(c);
                return pos.Row > 0;
            })
            .ToList();

        foreach (var ctrl in toRemove)
        {
            _customisationContent.Controls.Remove(ctrl);
            ctrl.Dispose();
        }

        // Shrink the row collection back to just row 0
        while (_customisationContent.RowStyles.Count > 1)
            _customisationContent.RowStyles.RemoveAt(_customisationContent.RowStyles.Count - 1);
        _customisationContent.RowCount = 1;

        _slotControls.Clear();
        _textureControls.Clear();
        _colourSwatches.Clear();
        _paletteLabelCtrl = null;
        _paletteCombo = null;
        _sailColourWarningLabel = null;

        // Dispose any open colour picker menu from previous config
        _activeColourMenu?.Dispose();
        _activeColourMenu = null;

        if (config == null)
        {
            // Show the info label in the panel (no dynamic controls to add)
            if (_customisationInfoLabel.Parent == null)
                _customisationContent.Controls.Add(_customisationInfoLabel);

            _customisationContent.ResumeLayout(true);
            return;
        }

        int nextRow = 1;

        // Warning label - always shown at the top of the dynamic customisation area
        var warningLabel = new ColorEmojiLabel
        {
            Text = UiStrings.Get("starship.customisation_combo_warning"),
            AutoSize = true,
            ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.WarningOrange : Color.DarkOrange,
            Font = new Font("Segoe UI Emoji", 8.5f, FontStyle.Regular),
            Padding = new Padding(0, 4, 0, 6),
        };
        _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _customisationContent.RowCount = nextRow + 1;
        _customisationContent.Controls.Add(warningLabel, 0, nextRow);
        _customisationContent.SetColumnSpan(warningLabel, 2);
        nextRow++;

        // Parts heading
        if (config.Slots.Count > 0)
        {
            var partsHeading = new Label
            {
                Text = UiStrings.Get("starship.customisation_parts_heading"),
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 2),
            };
            FontManager.ApplyHeadingFont(partsHeading, 10);
            _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _customisationContent.RowCount = nextRow + 1;
            _customisationContent.Controls.Add(partsHeading, 0, nextRow);
            _customisationContent.SetColumnSpan(partsHeading, 2);
            nextRow++;

            foreach (var slot in config.Slots)
            {
                var lbl = new Label
                {
                    Text = slot.Label + ":",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Padding = new Padding(0, 5, 10, 0),
                };
                var combo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };

                // Populate with (None) + slot items
                string noneLabel = UiStrings.Get("starship.customisation_none");
                combo.Items.Add(new SlotItemEntry(noneLabel, null));
                foreach (var item in slot.Items)
                    combo.Items.Add(new SlotItemEntry(item.ItemID, item));

                _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _customisationContent.RowCount = nextRow + 1;
                _customisationContent.Controls.Add(lbl, 0, nextRow);
                _customisationContent.Controls.Add(combo, 1, nextRow);
                _slotControls.Add((lbl, combo, slot));
                nextRow++;
            }
        }

        // Texture options (placed before colours so paint style choices appear first)
        if (config.TextureGroups.Count > 0)
        {
            var texHeading = new Label
            {
                Text = UiStrings.Get("starship.customisation_texture_heading"),
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 2),
            };
            FontManager.ApplyHeadingFont(texHeading, 10);
            _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _customisationContent.RowCount = nextRow + 1;
            _customisationContent.Controls.Add(texHeading, 0, nextRow);
            _customisationContent.SetColumnSpan(texHeading, 2);
            nextRow++;

            foreach (var tg in config.TextureGroups)
            {
                var lbl = new Label
                {
                    Text = UiStrings.Format("starship.customisation_paint_style_label", tg.GroupID),
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Padding = new Padding(0, 5, 10, 0),
                };
                var combo = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                };
                string noneLabel = UiStrings.Get("starship.customisation_none");
                combo.Items.Add(noneLabel);
                foreach (var opt in tg.Options)
                {
                    var friendly = GetTextureOptionFriendlyName(opt);
                    if (friendly != opt)
                        combo.Items.Add(new TextureOptionEntry(opt, friendly));
                    else
                        combo.Items.Add(opt);
                }

                _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _customisationContent.RowCount = nextRow + 1;
                _customisationContent.Controls.Add(lbl, 0, nextRow);
                _customisationContent.Controls.Add(combo, 1, nextRow);
                _textureControls.Add((lbl, combo, tg.GroupID));
                nextRow++;
            }
        }

        // Paint palette
        if (config.PaletteIDs.Count > 0)
        {
            var paletteLbl = new Label
            {
                Text = UiStrings.Get("starship.customisation_paint_palette"),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 10, 0),
            };
            var paletteCombo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            string noneLabel = UiStrings.Get("starship.customisation_none");
            paletteCombo.Items.Add(noneLabel);
            foreach (var pid in config.PaletteIDs)
            {
                if (!string.IsNullOrEmpty(pid))
                {
                    var friendly = GetPaletteFriendlyName(pid);
                    if (friendly != pid)
                        paletteCombo.Items.Add(new PaletteOptionEntry(pid, friendly));
                    else
                        paletteCombo.Items.Add(pid);
                }
            }

            _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _customisationContent.RowCount = nextRow + 1;
            _customisationContent.Controls.Add(paletteLbl, 0, nextRow);
            _customisationContent.Controls.Add(paletteCombo, 1, nextRow);
            _paletteLabelCtrl = paletteLbl;
            _paletteCombo = paletteCombo;
            nextRow++;

            // Colour channel swatches - shown after the palette selector, one per row with labels.
            // The three main ship colours plus two decal colours span two game channels (Paint, Undercoat).
            var coloursHeading = new Label
            {
                Text = UiStrings.Get("starship.customisation_colours_heading"),
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 2),
            };
            FontManager.ApplyHeadingFont(coloursHeading, 10);
            _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _customisationContent.RowCount = nextRow + 1;
            _customisationContent.Controls.Add(coloursHeading, 0, nextRow);
            _customisationContent.SetColumnSpan(coloursHeading, 2);
            nextRow++;

            // Each entry: (channel, colourAlt, displayPaletteId, localisationKey).
            // Colour 3 targets Undercoat/Alternative1 for dense saves (320-entry arrays);
            // the read/write methods fall back to Undercoat/Primary for sparse saves.
            (string Channel, string AltId, string DisplayPaletteId, string LocKey)[] colourDefs =
            [
                ("Paint",     "Primary",      "", "starship.customisation_colour1"),
                ("Paint",     "Alternative3", "", "starship.customisation_colour2"),
                ("Undercoat", "Alternative1", "", "starship.customisation_colour3"),
                ("Paint",     "Alternative1", "", "starship.customisation_decal1"),
                ("Paint",     "Alternative2", "", "starship.customisation_decal2"),
            ];
            const int swatchSize = 28;

            foreach (var (channel, altId, displayPaletteId, locKey) in colourDefs)
            {
                var swatchLbl = new Label
                {
                    Text = UiStrings.Get(locKey) + ":",
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Padding = new Padding(0, 5, 10, 0),
                };
                var swatch = new Panel
                {
                    Size = new Size(swatchSize, swatchSize),
                    BackColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.InputBackground : SystemColors.Control,
                    BorderStyle = BorderStyle.FixedSingle,
                    Anchor = AnchorStyles.Left,
                    Cursor = Cursors.Hand,
                };
                string capturedChannel = channel;
                string capturedAlt = altId;
                string capturedDisplayPalette = displayPaletteId;
                swatch.Click += (_, _) => OnShipColourSwatchClick(swatch, capturedChannel, capturedAlt, capturedDisplayPalette);
                _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _customisationContent.RowCount = nextRow + 1;
                _customisationContent.Controls.Add(swatchLbl, 0, nextRow);
                _customisationContent.Controls.Add(swatch, 1, nextRow);
                _colourSwatches.Add((channel, altId, displayPaletteId, swatch));
                nextRow++;
            }

            // Extra colour channels (e.g. SailShip_Sails for Solar ships)
            if (config.ExtraColourChannels.Count > 0)
            {
                foreach (var ec in config.ExtraColourChannels)
                {
                    var swatchLbl = new Label
                    {
                        Text = UiStrings.Get(ec.LabelKey) + ":",
                        AutoSize = true,
                        Anchor = AnchorStyles.Left,
                        Padding = new Padding(0, 5, 10, 0),
                    };
                    var swatch = new Panel
                    {
                        Size = new Size(swatchSize, swatchSize),
                    BackColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.InputBackground : SystemColors.Control,
                        BorderStyle = BorderStyle.FixedSingle,
                        Anchor = AnchorStyles.Left,
                        Cursor = Cursors.Hand,
                    };
                    string capturedChannel = ec.PaletteName;
                    string capturedAlt = ec.ColourAlt;
                    string capturedDisplayPalette = ec.DisplayPaletteId;
                    swatch.Click += (_, _) => OnShipColourSwatchClick(swatch, capturedChannel, capturedAlt, capturedDisplayPalette);
                    _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _customisationContent.RowCount = nextRow + 1;
                    _customisationContent.Controls.Add(swatchLbl, 0, nextRow);

                    if (string.Equals(ec.LabelKey, "starship.customisation_sail_colour", StringComparison.Ordinal))
                    {
                        // Wrap swatch + warning label in a FlowLayoutPanel for the sail colour row
                        var rowPanel = new FlowLayoutPanel
                        {
                            AutoSize = true,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                            FlowDirection = FlowDirection.LeftToRight,
                            WrapContents = false,
                            Margin = Padding.Empty,
                        };
                        rowPanel.Controls.Add(swatch);
                        _sailColourWarningLabel = new Label
                        {
                            Text = "⚠️Note: Sail colour changes are local only and not seen by other players.",
                            AutoSize = true,
                            ForeColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.WarningOrange : Color.DarkOrange,
                            Padding = new Padding(6, 5, 0, 0),
                        };
                        rowPanel.Controls.Add(_sailColourWarningLabel);
                        _customisationContent.Controls.Add(rowPanel, 1, nextRow);
                    }
                    else
                    {
                        _customisationContent.Controls.Add(swatch, 1, nextRow);
                    }

                    _colourSwatches.Add((ec.PaletteName, ec.ColourAlt, ec.DisplayPaletteId, swatch));
                    nextRow++;
                }
            }
        }

        _customisationContent.ResumeLayout(true);
    }

    /// <summary>
    /// Populates the Customisation tab controls from the given CCD entry.
    /// </summary>
    private void LoadCustomisationFromCcd(JsonObject? ccd, ShipCustomisationConfig? config)
    {
        if (config == null) return;

        // Build a set of current descriptor group IDs (without ^ prefix) for slot matching
        var currentDgIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ccd != null)
        {
            var customData = ccd.GetObject("CustomData");
            var dg = customData?.GetArray("DescriptorGroups");
            if (dg != null)
            {
                for (int i = 0; i < dg.Length; i++)
                {
                    string val = dg.Get(i)?.ToString() ?? "";
                    if (val.StartsWith("^", StringComparison.Ordinal)) val = val[1..];
                    if (!string.IsNullOrEmpty(val)) currentDgIds.Add(val);
                }
            }
        }

        // Set each slot combo to the matching item (or None)
        foreach (var (_, combo, slot) in _slotControls)
        {
            int matchIdx = 0; // default: (None)
            for (int i = 1; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is SlotItemEntry entry && entry.Item != null)
                {
                    bool anyMatch = entry.Item.DescriptorGroupIDs
                        .Any(dgId => currentDgIds.Contains(dgId));
                    if (anyMatch)
                    {
                        matchIdx = i;
                        break;
                    }
                }
            }
            combo.SelectedIndex = matchIdx;
        }

        // Set palette combo
        if (_paletteCombo != null && ccd != null)
        {
            var customData = ccd.GetObject("CustomData");
            string paletteId = customData?.GetString("PaletteID") ?? "";
            if (paletteId.StartsWith("^", StringComparison.Ordinal)) paletteId = paletteId[1..];

            // An empty PaletteID in the save means the ship uses the default SHIP palette.
            // Map empty -> "SHIP" so the combo shows the correct entry and the colour picker
            // uses the right palette grid.
            if (string.IsNullOrEmpty(paletteId))
                paletteId = "SHIP";

            int idx = -1;
            for (int i = 0; i < _paletteCombo.Items.Count; i++)
            {
                string itemKey = _paletteCombo.Items[i] is PaletteOptionEntry poe
                    ? poe.RawId
                    : _paletteCombo.Items[i]?.ToString() ?? "";
                if (string.Equals(itemKey, paletteId, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
            _paletteCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        // Set texture option combos
        if (ccd != null)
        {
            var customData = ccd.GetObject("CustomData");
            var texOptions = customData?.GetArray("TextureOptions");
            var texMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (texOptions != null)
            {
                for (int i = 0; i < texOptions.Length; i++)
                {
                    var entry = texOptions.GetObject(i);
                    if (entry == null) continue;
                    string grp = entry.GetString("TextureOptionGroupName") ?? "";
                    string opt = entry.GetString("TextureOptionName") ?? "";
                    if (grp.StartsWith("^", StringComparison.Ordinal)) grp = grp[1..];
                    if (opt.StartsWith("^", StringComparison.Ordinal)) opt = opt[1..];
                    if (!string.IsNullOrEmpty(grp))
                        texMap[grp] = opt;
                }
            }

            foreach (var (_, combo, groupId) in _textureControls)
            {
                string noneLabel = UiStrings.Get("starship.customisation_none");
                if (texMap.TryGetValue(groupId, out string? optValue) && !string.IsNullOrEmpty(optValue))
                {
                    int idx = -1;
                    for (int i = 0; i < combo.Items.Count; i++)
                    {
                        string itemKey = combo.Items[i] is TextureOptionEntry toe
                            ? toe.RawId
                            : combo.Items[i]?.ToString() ?? "";
                        if (string.Equals(itemKey, optValue, StringComparison.OrdinalIgnoreCase))
                        {
                            idx = i;
                            break;
                        }
                    }
                    combo.SelectedIndex = idx >= 0 ? idx : 0;
                }
                else
                {
                    int noneIdx = combo.FindStringExact(noneLabel);
                    combo.SelectedIndex = noneIdx >= 0 ? noneIdx : 0;
                }
            }
        }

        // Load colour channel swatches from the CCD Colours array
        if (ccd != null && _colourSwatches.Count > 0)
        {
            var customData = ccd.GetObject("CustomData");
            var coloursArr = customData?.GetArray("Colours");
            foreach (var (paletteName, colourAlt, _, swatch) in _colourSwatches)
            {
                Color swatchColour = ReadShipColourFromCcd(coloursArr, paletteName, colourAlt);
                // Colour 3 targets Undercoat/Alternative1 for dense saves but sparse saves use
                // Undercoat/Primary. Fall back to Primary if Alternative1 was not found.
                if (swatchColour == SystemColors.Control
                    && string.Equals(paletteName, "Undercoat", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(colourAlt, "Alternative1", StringComparison.OrdinalIgnoreCase))
                {
                    swatchColour = ReadShipColourFromCcd(coloursArr, "Undercoat", "Primary");
                }
                swatch.BackColor = swatchColour;
            }
        }
    }

    /// <summary>
    /// Writes the current Customisation tab control values back to the CCD array
    /// for the given ship index. Also updates the ship's resource filename if the
    /// scene combo has been changed.
    /// </summary>
    private void SaveCustomisationToCcd(JsonObject playerState, int shipIndex)
    {
        try
        {
            var ccdArray = playerState.GetArray("CharacterCustomisationData");
            if (ccdArray == null) return;

            // Get the actual CCD index and entry (not a clone - modify in-place)
            int ccdIdx = StarshipLogic.ShipIndexToCcdIndex(shipIndex);
            if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return;

            var ccd = ccdArray.GetObject(ccdIdx);
            if (ccd == null) return;

            var customData = ccd.GetObject("CustomData");
            if (customData == null) return;

            // Rebuild DescriptorGroups from slot selections
            var dgArray = customData.GetArray("DescriptorGroups");
            if (dgArray != null)
            {
                // Clear existing entries
                for (int i = dgArray.Length - 1; i >= 0; i--)
                    dgArray.RemoveAt(i);

                foreach (var (_, combo, _) in _slotControls)
                {
                    if (combo.SelectedItem is SlotItemEntry entry && entry.Item != null)
                    {
                        foreach (var dgId in entry.Item.DescriptorGroupIDs)
                            dgArray.Add("^" + dgId);
                    }
                }
            }

            // Write PaletteID
            // "SHIP" is the game's default palette represented as an empty value (^).
            if (_paletteCombo != null)
            {
                string palette;
                if (_paletteCombo.SelectedItem is PaletteOptionEntry poe)
                    palette = poe.RawId;
                else
                    palette = _paletteCombo.SelectedItem?.ToString() ?? "";
                string noneLabel = UiStrings.Get("starship.customisation_none");
                bool isDefault = palette == noneLabel
                    || string.IsNullOrEmpty(palette)
                    || palette.Equals("SHIP", StringComparison.OrdinalIgnoreCase);
                customData.Set("PaletteID", isDefault ? "^" : "^" + palette);
            }

            // Rebuild TextureOptions array in-place
            if (_textureControls.Count > 0)
            {
                var texOptions = customData.GetArray("TextureOptions");
                if (texOptions != null)
                {
                    // Clear existing entries
                    for (int i = texOptions.Length - 1; i >= 0; i--)
                        texOptions.RemoveAt(i);

                    string noneLabel = UiStrings.Get("starship.customisation_none");
                    foreach (var (_, combo, groupId) in _textureControls)
                    {
                        string opt;
                        if (combo.SelectedItem is TextureOptionEntry toe)
                            opt = toe.RawId;
                        else
                            opt = combo.SelectedItem?.ToString() ?? "";
                        if (opt == noneLabel || string.IsNullOrEmpty(opt)) continue;
                        var texEntry = new JsonObject();
                        texEntry.Set("TextureOptionGroupName", "^" + groupId);
                        texEntry.Set("TextureOptionName", "^" + opt);
                        texOptions.Add(texEntry);
                    }
                }
            }
        }
        catch { }
    }

    /// <summary>
    /// Called when the user leaves the scene combo (or presses Enter).
    /// Updates the ship type selector to reflect the new resource path.
    /// </summary>
    private void OnSceneComboLeave(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0) return;

            string newResource = _sceneCombo.Text.Trim();
            if (string.IsNullOrEmpty(newResource)) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);
            var resource = ship.GetObject("Resource");
            if (resource == null) return;

            string currentFilename = resource.GetString("Filename") ?? "";
            if (string.Equals(currentFilename, newResource, StringComparison.OrdinalIgnoreCase))
                return;

            // Apply the new filename to the ship resource
            resource.Set("Filename", newResource);

            // Update the ship type combo to reflect the new path
            var (displayName, _, _) = StarshipLogic.GetShipInfo(newResource);
            bool isModified = StarshipLogic.IsFilenameModified(newResource);
            SelectShipTypeByName(displayName, isModified, isModified ? newResource : null);
            SetStarshipMaxSupportedLabels(newResource);

            // Reload customisation tab for the new resource
            bool isCorvette = StarshipLogic.IsCorvette(newResource);
            LoadCustomisationTab(isCorvette, newResource, idx);

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    /// <summary>Shows the info label with the given message, hiding dynamic controls.</summary>
    private void ShowCustomisationInfoLabel(string message)
    {
        _customisationInfoLabel.Text = message;
        _customisationInfoLabel.Visible = true;

        if (_customisationInfoLabel.Parent == null)
        {
            // Add info label below the scene row (row 1)
            int row = 1;
            _customisationContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _customisationContent.RowCount = row + 1;
            _customisationContent.Controls.Add(_customisationInfoLabel, 0, row);
            _customisationContent.SetColumnSpan(_customisationInfoLabel, 2);
        }
    }

    /// <summary>Hides the info label.</summary>
    private void HideCustomisationInfoLabel()
    {
        _customisationInfoLabel.Visible = false;
    }

    /// <summary>
    /// Reads the RGBA colour for the given palette channel name and colour alt
    /// from the ship CCD Colours array. Returns SystemColors.Control if not found.
    /// </summary>
    private static Color ReadShipColourFromCcd(JsonArray? coloursArr, string paletteName, string colourAlt)
    {
        if (coloursArr == null) return SystemColors.Control;

        try
        {
            for (int i = 0; i < coloursArr.Length; i++)
            {
                var entry = coloursArr.GetObject(i);
                if (entry == null) continue;

                var paletteObj = entry.GetObject("Palette");
                if (paletteObj == null) continue;

                string entryPalette = paletteObj.GetString("Palette") ?? "";
                string entryAlt = paletteObj.GetString("ColourAlt") ?? "";
                if (!entryPalette.Equals(paletteName, StringComparison.OrdinalIgnoreCase) ||
                    !entryAlt.Equals(colourAlt, StringComparison.OrdinalIgnoreCase))
                    continue;

                var colArr = entry.GetArray("Colour");
                if (colArr == null || colArr.Length < 3) return SystemColors.Control;

                double r = colArr.GetDouble(0);
                double g = colArr.GetDouble(1);
                double b = colArr.GetDouble(2);
                return Color.FromArgb(
                    (int)Math.Round(r * 255.0),
                    (int)Math.Round(g * 255.0),
                    (int)Math.Round(b * 255.0));
            }
        }
        catch { }

        return SystemColors.Control;
    }

    /// <summary>
    /// Writes a colour to the CCD Colours array entry matching the given palette name
    /// and colour alt. No-op if the entry does not exist.
    /// </summary>
    private void WriteShipColourToCcd(string paletteName, string colourAlt, Color colour)
    {
        if (_playerState == null || _shipSelector.SelectedIndex < 0) return;

        var ccdArray = _playerState.GetArray("CharacterCustomisationData");
        if (ccdArray == null) return;

        var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
        int ccdIdx = StarshipLogic.ShipIndexToCcdIndex(item.DataIndex);
        if (ccdIdx < 0 || ccdIdx >= ccdArray.Length) return;

        var ccd = ccdArray.GetObject(ccdIdx);
        var customData = ccd?.GetObject("CustomData");
        var coloursArr = customData?.GetArray("Colours");
        if (coloursArr == null) return;

        try
        {
            bool found = false;
            for (int i = 0; i < coloursArr.Length; i++)
            {
                var entry = coloursArr.GetObject(i);
                if (entry == null) continue;

                var paletteObj = entry.GetObject("Palette");
                if (paletteObj == null) continue;

                string entryPalette = paletteObj.GetString("Palette") ?? "";
                string entryAlt = paletteObj.GetString("ColourAlt") ?? "";
                if (!entryPalette.Equals(paletteName, StringComparison.OrdinalIgnoreCase) ||
                    !entryAlt.Equals(colourAlt, StringComparison.OrdinalIgnoreCase))
                    continue;

                var colArr = entry.GetArray("Colour");
                if (colArr == null || colArr.Length < 4) return;

                var rgba = NmsColourPalette.ToNormalisedRgba(colour);
                colArr.Set(0, rgba[0]);
                colArr.Set(1, rgba[1]);
                colArr.Set(2, rgba[2]);
                colArr.Set(3, rgba[3]);
                DataModified?.Invoke(this, EventArgs.Empty);
                found = true;
                break;
            }

            // For Undercoat/Alternative1, also fall back to Undercoat/Primary for sparse saves
            // (older format where only the three main colour entries exist).
            if (!found
                && string.Equals(paletteName, "Undercoat", StringComparison.OrdinalIgnoreCase)
                && string.Equals(colourAlt, "Alternative1", StringComparison.OrdinalIgnoreCase))
            {
                WriteShipColourToCcd("Undercoat", "Primary", colour);
            }
        }
        catch { }
    }

    /// <summary>
    /// Handles a click on a ship paint colour swatch. Shows a popup grid of the
    /// available colours from the appropriate palette. For standard paint channels
    /// (Paint, Undercoat), the palette selected in the combo box is used.
    /// For extra colour channels (e.g. SailShip_Sails), the <paramref name="displayPaletteId"/>
    /// overrides the combo selection.
    /// Selecting a colour updates the swatch and writes the value to the CCD.
    /// </summary>
    private void OnShipColourSwatchClick(Panel swatch, string paletteName, string colourAlt, string displayPaletteId = "")
    {
        // Determine which palette colours to display.
        // If the swatch has a specific display palette (e.g. SailShip_Sails), use that;
        // otherwise use the palette selected in the combo box.
        string paletteId;
        NmsColourPalette.PaletteEntry[]? palette;
        if (!string.IsNullOrEmpty(displayPaletteId))
        {
            paletteId = displayPaletteId;
            palette = NmsColourPalette.GetPaletteColours(paletteId);
        }
        else
        {
            paletteId = _paletteCombo?.SelectedItem?.ToString() ?? "";
            palette = NmsColourPalette.GetPaletteColours(paletteId);
        }

        if (palette == null || palette.Length == 0)
            palette = NmsColourPalette.PaintPalette;
        if (palette.Length == 0) return;

        _activeColourMenu?.Dispose();
        _activeColourMenu = null;

        const int cols = 10;
        const int cellSize = 24;
        const int cellMargin = 1;
        int rows = (palette.Length + cols - 1) / cols;

        var grid = new TableLayoutPanel
        {
            ColumnCount = cols,
            RowCount = rows,
            AutoSize = true,
            Padding = new Padding(2),
            Margin = Padding.Empty,
            BackColor = ThemeManager.Effective == AppTheme.Dark ? ThemeColors.Dark.InputBackground : SystemColors.Control,
        };

        var tip = new ToolTip();
        foreach (var pe in palette)
        {
            var cell = new Panel
            {
                Size = new Size(cellSize, cellSize),
                BackColor = pe.Colour,
                Margin = new Padding(cellMargin),
                Cursor = Cursors.Hand,
            };
            tip.SetToolTip(cell, pe.Name);
            var capturedColour = pe.Colour;
            cell.Click += (_, _) =>
            {
                swatch.BackColor = capturedColour;
                WriteShipColourToCcd(paletteName, colourAlt, capturedColour);
                _activeColourMenu?.Close();
            };
            grid.Controls.Add(cell);
        }

        var host = new ToolStripControlHost(grid)
        {
            Padding = Padding.Empty,
            Margin = Padding.Empty,
        };

        var dropdown = new ToolStripDropDown { Padding = Padding.Empty };
        dropdown.Items.Add(host);

        _activeColourMenu = dropdown;
        dropdown.Show(swatch, new Point(0, swatch.Height));
    }

    /// <summary>
    /// Represents a selectable item in a slot combo box.
    /// Displays the item ID; holds a reference to the full item data.
    /// </summary>
    private sealed class SlotItemEntry
    {
        private readonly string _display;
        internal ShipCustomisationItem? Item { get; }

        internal SlotItemEntry(string display, ShipCustomisationItem? item)
        {
            _display = display;
            Item = item;
        }

        public override string ToString() => _display;
    }

    /// <summary>
    /// Wraps a palette option raw ID with a user-friendly display name.
    /// </summary>
    private sealed class PaletteOptionEntry
    {
        private readonly string _display;
        internal string RawId { get; }

        internal PaletteOptionEntry(string rawId, string display)
        {
            RawId = rawId;
            _display = display;
        }

        public override string ToString() => _display;
    }

    /// <summary>
    /// Wraps a texture option raw ID with a user-friendly display name.
    /// </summary>
    private sealed class TextureOptionEntry
    {
        private readonly string _display;
        internal string RawId { get; }

        internal TextureOptionEntry(string rawId, string display)
        {
            RawId = rawId;
            _display = display;
        }

        public override string ToString() => _display;
    }

    /// <summary>Returns a localised user-friendly name for a paint palette raw ID.</summary>
    private static string GetPaletteFriendlyName(string rawId)
    {
        return rawId switch
        {
            "SHIP" => UiStrings.Get("starship.palette_default"),
            "SHIP_METALLIC" => UiStrings.Get("starship.palette_metallic"),
            _ => rawId,
        };
    }

    /// <summary>Returns a localised user-friendly name for a texture option raw ID.</summary>
    private static string GetTextureOptionFriendlyName(string rawId)
    {
        return rawId switch
        {
            "COATING" => UiStrings.Get("starship.texture_coating"),
            "PANELS" => UiStrings.Get("starship.texture_panels"),
            "STEALTH" => UiStrings.Get("starship.texture_stealth"),
            "METALBOLT" => UiStrings.Get("starship.texture_metalbolt"),
            _ => rawId,
        };
    }

    private void OnGoToJsonListClicked(object? sender, EventArgs e)
    {
        GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "ShipOwnership"));
    }

    private void OnGoToJsonSelectedClicked(object? sender, EventArgs e)
    {
        int idx = GetSelectedShipDataIndex();
        if (idx < 0) return;
        GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "ShipOwnership", $"[{idx}]"));
    }

    private void OnGoToJsonCargoClicked(object? sender, EventArgs e)
    {
        int idx = GetSelectedShipDataIndex();
        if (idx < 0) return;
        GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "ShipOwnership", $"[{idx}]", "Inventory"));
    }

    private void OnGoToJsonCustomisationClicked(object? sender, EventArgs e)
    {
        int idx = GetSelectedShipDataIndex();
        if (idx < 0) return;
        GoToJsonRequested?.Invoke(this, new GoToJsonEventArgs("PlayerStateData", "ShipOwnership", $"[{idx}]", "Resource"));
    }

    private int GetSelectedShipDataIndex()
    {
        if (_shipSelector.SelectedIndex < 0 || _shipSelector.SelectedItem is not StarshipLogic.ShipListItem item)
            return -1;
        return item.DataIndex;
    }

}
