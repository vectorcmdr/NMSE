using NMSE.Config;
using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class StarshipPanel : UserControl
{
    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

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
        _snapshotTechBtn.Text = UiStrings.Get("starship.export_snapshot");
        _importSnapshotBtn.Text = UiStrings.Get("starship.import_snapshot");
        _optimiseBtn.Text = UiStrings.Get("starship.optimise");
        _useOldColours.Text = UiStrings.Get("starship.use_old_colour");
        _corvetteWarningLabel.Text = UiStrings.Get("starship.corvette_warning");
        _cargoTabPage.Text = UiStrings.Get("starship.tab_cargo");
        _techTabPage.Text = UiStrings.Get("starship.tab_tech");

        // Refresh ship type combo with localised display names
        RefreshShipTypeCombo();
        _inventoryGrid.ApplyUiLocalisation();
        _techGrid.ApplyUiLocalisation();
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
                _shipSelector.SelectedIndex = selectIdx;
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

            var selectedTypeItem = _shipType.SelectedItem as StarshipLogic.ShipTypeItem;

            var values = new StarshipLogic.ShipSaveValues
            {
                Name = _shipName.Text,
                SelectedTypeName = selectedTypeItem?.InternalName,
                CustomFilename = selectedTypeItem?.CustomFilename,
                ClassIndex = _shipClass.SelectedIndex,
                Seed = _shipSeed.Text,
                Damage = (double)_damageField.Value,
                Shield = (double)_shieldField.Value,
                Hyperdrive = (double)_hyperdriveField.Value,
                Maneuver = (double)_maneuverField.Value,
                UseOldColours = _useOldColours.Checked,
                ShipIndex = idx,
                PrimaryShipIndex = _primaryShipIndex,
                RawStatValues = _rawShipStatValues
            };

            StarshipLogic.SaveShipData(ship, playerState, values);

            _inventoryGrid.SaveInventory(ship.GetObject("Inventory"));
            _techGrid.SaveInventory(ship.GetObject("Inventory_TechOnly"));
        }
        catch { }
    }

    private void OnShipSelected(object? sender, EventArgs e)
    {
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

            var ship = _shipOwnership.GetObject(idx);
            var data = StarshipLogic.LoadShipData(ship, _playerState, idx);

            _shipName.Text = data.Name;
            SelectShipTypeByName(data.ShipTypeName, data.IsResourceModified, data.IsResourceModified ? data.Filename : null);
            SetStarshipMaxSupportedLabels(data.Filename);
            _shipSeed.Text = data.Seed;
            _shipClass.SelectedIndex = data.ClassIndex;
            _useOldColours.Checked = data.UseOldColours;

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

            try { _damageField.Value = (decimal)data.Damage; } catch { _damageField.Value = 0; }
            try { _shieldField.Value = (decimal)data.Shield; } catch { _shieldField.Value = 0; }
            try { _hyperdriveField.Value = (decimal)data.Hyperdrive; } catch { _hyperdriveField.Value = 0; }
            try { _maneuverField.Value = (decimal)data.Maneuver; } catch { _maneuverField.Value = 0; }

            // Store raw stat values for preservation before limits clamp the NUDs
            _rawShipStatValues = new Dictionary<string, double>
            {
                ["^SHIP_DAMAGE"] = data.Damage,
                ["^SHIP_SHIELD"] = data.Shield,
                ["^SHIP_HYPERDRIVE"] = data.Hyperdrive,
                ["^SHIP_AGILE"] = data.Maneuver,
            };

            // Apply BaseStatLimits to the NumericUpDown controls.
            // Use "Alien" limits when the ship type contains "Alien", otherwise "Normal".
            string shipCategory = (data.ShipTypeName ?? "").Contains("Alien", StringComparison.OrdinalIgnoreCase) ? "Alien" : "Normal";
            ApplyStatLimits(_damageField, shipCategory, "^SHIP_DAMAGE", StatCategory.Ship);
            ApplyStatLimits(_shieldField, shipCategory, "^SHIP_SHIELD", StatCategory.Ship);
            ApplyStatLimits(_hyperdriveField, shipCategory, "^SHIP_HYPERDRIVE", StatCategory.Ship);
            ApplyStatLimits(_maneuverField, shipCategory, "^SHIP_AGILE", StatCategory.Ship);

            // Toggle corvette extras panel and warning
            _corvetteExtrasPanel.Visible = isCorvette;
            _corvetteWarningLabel.Visible = isCorvette;

            // Update optimise indicator for corvettes
            if (isCorvette)
                UpdateOptimiseIndicator(idx);
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
        bool changed = ExosuitAutoStackLogic.AutoStackCargoToChests(cargoInventory, _playerState!, out _, out _, pinned);
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
        bool changed = ExosuitAutoStackLogic.AutoStackFromInventoryToInventory(
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

        bool changed = ExosuitAutoStackLogic.AutoStackCargoToChests(
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

        bool changed = ExosuitAutoStackLogic.AutoStackFromInventoryToInventory(
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

    /// <summary>Applies BaseStatLimits min/max to a NumericUpDown control.</summary>
    private static void ApplyStatLimits(NumericUpDown nud, string entityType, string statId, StatCategory category)
    {
        var range = BaseStatLimits.GetRange(entityType, statId, category);
        if (range != null)
        {
            nud.Minimum = range.MinValue;
            nud.Maximum = range.MaxValue;
        }
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

            // If the deleted ship was the primary ship, reassign to the first valid ship.
            // Since we don't remove from the array, non-primary indices remain correct.
            if (idx == _primaryShipIndex)
            {
                _primaryShipIndex = StarshipLogic.FindFirstValidShipIndex(_shipOwnership);
                if (_primaryShipIndex < 0) _primaryShipIndex = 0;
                _playerState.Set("PrimaryShip", _primaryShipIndex);
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

    private void OnExportShip(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0) return;

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

            if (dialog.ShowDialog() == DialogResult.OK)
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
            if (dialog.ShowDialog() != DialogResult.OK) return;

            // --- Parse the imported file ---
            var zipResult = StarshipLogic.TryReadNmsshipZip(dialog.FileName);

            JsonObject? importedShip;
            JsonObject? importedBase = null;
            JsonObject? zipCcd = null;
            // CCD from the wrapper-level "CharacterCustomisationData" key (new format)
            JsonObject? wrapperCcd = null;

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

                // Check for wrapper format: { Ship, [Base], [CharacterCustomisationData] }
                importedShip = imported.GetObject("Ship");
                if (importedShip != null)
                {
                    importedBase = imported.GetObject("Base");
                    wrapperCcd = imported.GetObject("CharacterCustomisationData");
                }
                else
                {
                    // Plain ship file or NomNom wrapper
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

            // Copy all properties from imported ship to target slot
            foreach (var name in importedShip.Names())
            {
                if (name == "__ShipCustomisation") continue;
                targetShip.Set(name, importedShip.Get(name));
            }

            // Remove the legacy key from the live ship object if it leaked
            targetShip.Remove("__ShipCustomisation");

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
                    // No existing base for this slot - add the imported base
                    // as a new entry in PersistentPlayerBases.
                    importedBase.Set("UserData", targetIdx);
                    bases.Add(importedBase);
                }
            }

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

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Create combined export: Ship (without cargo) + Base
                var export = new JsonObject();
                export.Set("Ship", shipSnapshot);
                if (baseObj != null)
                    export.Set("Base", baseObj);
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
            if (dialog.ShowDialog() != DialogResult.OK) return;

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
                ship.Set(name, importedShip.Get(name));
            }

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

            // Refresh display
            OnShipSelected(this, EventArgs.Empty);
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

            MessageBox.Show(this, 
                UiStrings.Format("starship.optimise_done", result),
                UiStrings.Get("starship.optimise"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format("Optimisation failed: {0}", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Sets the optimise indicator: green tick if optimised, orangered cross if not.
    /// </summary>
    private void SetOptimiseIndicator(bool isOptimised)
    {
        _optimiseIndicator.Text = isOptimised ? "\u2714" : "\u2718";
        _optimiseIndicator.ForeColor = isOptimised
            ? Color.Green
            : Color.OrangeRed;
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

}