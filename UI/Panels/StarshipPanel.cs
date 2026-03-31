using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class StarshipPanel : UserControl
{
    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    private JsonArray? _shipOwnership;
    private JsonObject? _playerState;
    private JsonObject? _saveData;
    private GameItemDatabase? _database;
    private int _primaryShipIndex;
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
        string filename = StarshipLogic.LookupFilenameForType(selectedType);
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
    /// </summary>
    private void SelectShipTypeByName(string? englishTypeName)
    {
        if (string.IsNullOrEmpty(englishTypeName)) { _shipType.SelectedIndex = -1; return; }
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
        _exportCorvetteBtn.Text = UiStrings.Get("starship.export_corvette");
        _importCorvetteBtn.Text = UiStrings.Get("starship.import_corvette");
        _importEmptySlotBtn.Text = UiStrings.Get("starship.import_empty_slot");
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
    /// preserving the currently selected type.
    /// </summary>
    private void RefreshShipTypeCombo()
    {
        string? currentType = (_shipType.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName;
        _shipType.Items.Clear();
        _shipType.Items.AddRange(StarshipLogic.GetShipTypeItems());
        if (currentType != null)
            SelectShipTypeByName(currentType);
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

            var values = new StarshipLogic.ShipSaveValues
            {
                Name = _shipName.Text,
                SelectedTypeName = (_shipType.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName,
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
            SelectShipTypeByName(data.ShipTypeName);
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

            // Toggle corvette vs normal export/import buttons and warning
            _exportBtn.Visible = !isCorvette;
            _importBtn.Visible = !isCorvette;
            _exportCorvetteBtn.Visible = isCorvette;
            _importCorvetteBtn.Visible = isCorvette;
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
            // No custom name — use ship type
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
                MessageBox.Show(UiStrings.Get("starship.cannot_delete_only"), UiStrings.Get("starship.delete_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                UiStrings.Get("starship.delete_confirm"),
                UiStrings.Get("starship.delete_title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            // Invalidate the ship in place - do NOT remove from array.
            // The slot stays in the ShipOwnership array (preserving index alignment
            // with the parallel ShipUsesLegacyColours array) but is
            // filtered out by BuildShipList().
            StarshipLogic.DeleteShipData(_shipOwnership.GetObject(idx));

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

            var resource = ship.GetObject("Resource");
            string filename = resource?.GetString("Filename") ?? "";
            bool isCorvette = StarshipLogic.IsCorvette(filename);

            string template = isCorvette ? cfg.CorvetteTemplate : cfg.StarshipTemplate;
            string ext = isCorvette ? cfg.CorvetteExt : cfg.StarshipExt;
            string label = isCorvette ? "Corvette files" : "Starship files";

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(ext, label),
                DefaultExt = ext.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(template, ext, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Embed CharacterCustomisationData so it can be restored on import.
                var ccdArray = _playerState?.GetArray("CharacterCustomisationData");
                var ccdEntry = StarshipLogic.GetShipCustomisation(ccdArray, idx);
                if (ccdEntry != null)
                    ship.Set("__ShipCustomisation", ccdEntry);

                ship.ExportToFile(dialog.FileName);

                // Remove the transient key so it doesn't persist in the live save
                ship.Remove("__ShipCustomisation");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportShip(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0) return;

            var cfg = ExportConfig.Instance;

            // Determine if current selection is a corvette
            var currentItem = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int currentIdx = currentItem.DataIndex;
            if (currentIdx >= _shipOwnership.Length) return;
            var currentShip = _shipOwnership.GetObject(currentIdx);
            var resource = currentShip.GetObject("Resource");
            string filename = resource?.GetString("Filename") ?? "";
            bool isCorvette = StarshipLogic.IsCorvette(filename);

            string ext = isCorvette ? cfg.CorvetteExt : cfg.StarshipExt;
            string label = isCorvette ? "Corvette files" : "Starship files";

            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildOpenFilter(ext, label)
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            // Try ZIP format first (other editor's .nmsship format)
            var zipResult = StarshipLogic.TryReadNmsshipZip(dialog.FileName);

            JsonObject imported;
            JsonObject? zipCcd = null;
            JsonArray? zipObjects = null;

            if (zipResult != null)
            {
                imported = zipResult.Value.ship;
                zipCcd = zipResult.Value.ccd;
                zipObjects = zipResult.Value.objects;
            }
            else
            {
                imported = JsonObject.ImportFromFile(dialog.FileName);

                // Unwrap NomNom wrapper if present (Data -> Starship or Data -> Vehicle)
                if (InventoryImportHelper.IsNomNomWrapper(imported))
                {
                    var data = imported.GetObject("Data");
                    if (data != null)
                    {
                        // NomNom may use "Starship" or "Ship" as the key depending on
                        // the export version; try both for maximum compatibility.
                        var entity = data.GetObject("Starship") ?? data.GetObject("Ship");
                        if (entity != null) imported = entity;
                    }
                }
            }

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            // Extract the embedded CharacterCustomisationData before copying
            // properties (so the transient key doesn't end up in the save).
            var importedCcd = imported.GetObject("__ShipCustomisation");

            // Copy all properties from imported ship to current slot
            foreach (var name in imported.Names())
            {
                if (name == "__ShipCustomisation") continue; // skip transient key
                ship.Set(name, imported.Get(name));
            }

            // Remove the transient key from the live ship object if it leaked
            ship.Remove("__ShipCustomisation");

            // Determine CCD source: prefer ZIP CCD if present and non-default
            JsonObject? ccdToApply = importedCcd;
            if (zipCcd != null && !StarshipLogic.IsCcdDefault(zipCcd))
                ccdToApply = zipCcd;

            var ccdArray = _playerState?.GetArray("CharacterCustomisationData");
            StarshipLogic.SetShipCustomisation(ccdArray, idx, ccdToApply);

            // Import base building objects for corvette ships from ZIP
            if (zipObjects != null && _saveData != null)
            {
                var importedResource = imported.GetObject("Resource");
                string importedFilename = importedResource?.GetString("Filename") ?? "";
                bool importedIsCorvette = StarshipLogic.IsCorvette(importedFilename);
                bool targetIsCorvette = StarshipLogic.IsCorvette(
                    ship.GetObject("Resource")?.GetString("Filename") ?? "");

                if (importedIsCorvette || targetIsCorvette)
                {
                    string seed = "";
                    try
                    {
                        var res = ship.GetObject("Resource");
                        seed = res?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
                    }
                    catch { }

                    long seedDecimal = StarshipLogic.SeedToDecimal(seed);
                    if (seedDecimal > 0)
                    {
                        var playerState = _saveData.GetObject("PlayerStateData");
                        var bases = playerState?.GetArray("PersistentPlayerBases");
                        int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx, seedDecimal);
                        if (baseIdx >= 0)
                        {
                            var existingBase = bases!.GetObject(baseIdx);
                            existingBase.Set("Objects", zipObjects);
                        }
                    }
                }
            }

            // Refresh display
            OnShipSelected(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportToEmptySlot(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _playerState == null || _saveData == null) return;

            int emptyIdx = StarshipLogic.FindEmptySlot(_shipOwnership);
            if (emptyIdx < 0)
            {
                MessageBox.Show(
                    UiStrings.Get("starship.no_empty_slots"),
                    UiStrings.Get("starship.import_ship_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cfg = ExportConfig.Instance;

            // Accept both starship and corvette file formats
            string filter = ExportConfig.BuildImportFilter(cfg.StarshipExt, "Starship files",
                cfg.CorvetteExt, cfg.CorvetteSnapshotExt, ".nmsship");

            using var dialog = new OpenFileDialog { Filter = filter };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            // Try ZIP format first (other editor's .nmsship format)
            var zipResult = StarshipLogic.TryReadNmsshipZip(dialog.FileName);

            JsonObject? importedShip;
            JsonObject? importedBase = null;
            JsonObject? zipCcd = null;

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

                // Check for corvette format (Ship + Base wrapper)
                importedShip = imported.GetObject("Ship");
                if (importedShip != null)
                {
                    importedBase = imported.GetObject("Base");
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
                MessageBox.Show(
                    UiStrings.Get("starship.no_valid_ship"),
                    UiStrings.Get("common.error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var ship = _shipOwnership.GetObject(emptyIdx);

            // Extract the embedded CharacterCustomisationData before copying
            var importedCcd = importedShip.GetObject("__ShipCustomisation");

            // Copy all properties from imported ship to the empty slot
            foreach (var name in importedShip.Names())
            {
                if (name == "__ShipCustomisation") continue;
                ship.Set(name, importedShip.Get(name));
            }

            // Remove the transient key from the live ship object if it leaked
            ship.Remove("__ShipCustomisation");

            // Determine CCD source: prefer ZIP CCD if present and non-default
            JsonObject? ccdToApply = importedCcd;
            if (zipCcd != null && !StarshipLogic.IsCcdDefault(zipCcd))
                ccdToApply = zipCcd;

            var ccdArray = _playerState.GetArray("CharacterCustomisationData");
            StarshipLogic.SetShipCustomisation(ccdArray, emptyIdx, ccdToApply);

            // Import base building objects for corvette ships
            if (importedBase != null)
            {
                var importedResource = importedShip.GetObject("Resource");
                string importedFilename = importedResource?.GetString("Filename") ?? "";
                if (StarshipLogic.IsCorvette(importedFilename))
                {
                    string seed = "";
                    try
                    {
                        var res = ship.GetObject("Resource");
                        seed = res?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
                    }
                    catch { }

                    long seedDecimal = StarshipLogic.SeedToDecimal(seed);
                    if (seedDecimal > 0)
                    {
                        var bases = _playerState.GetArray("PersistentPlayerBases");
                        int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, emptyIdx, seedDecimal);
                        if (baseIdx >= 0)
                        {
                            var existingBase = bases!.GetObject(baseIdx);
                            foreach (var name in importedBase.Names())
                                existingBase.Set(name, importedBase.Get(name));
                        }
                    }
                }
            }

            // Rebuild the ship list and select the newly imported ship
            _shipSelector.Items.Clear();
            var shipList = StarshipLogic.BuildShipList(_shipOwnership);
            foreach (var shipItem in shipList)
                _shipSelector.Items.Add(shipItem);

            // Find and select the newly imported slot
            for (int i = 0; i < _shipSelector.Items.Count; i++)
            {
                if (((StarshipLogic.ShipListItem)_shipSelector.Items[i]!).DataIndex == emptyIdx)
                {
                    _shipSelector.SelectedIndex = i;
                    break;
                }
            }

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            var resource = ship?.GetObject("Resource");
            string filename = resource?.GetString("Filename") ?? "";
            if (StarshipLogic.IsCorvette(filename))
            {
                var result = MessageBox.Show(
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
            string seed = "";
            try
            {
                var resource = ship.GetObject("Resource");
                seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
            }
            catch { }

            long seedDecimal = StarshipLogic.SeedToDecimal(seed);
            JsonObject? baseObj = null;
            {
                var playerState = _saveData.GetObject("PlayerStateData");
                var bases = playerState?.GetArray("PersistentPlayerBases");
                int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx, seedDecimal);
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
            MessageBox.Show(UiStrings.Format("starship.snapshot_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(UiStrings.Get("starship.no_valid_ship"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                string seed = "";
                try
                {
                    var resource = ship.GetObject("Resource");
                    seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
                }
                catch { }

                long seedDecimal = StarshipLogic.SeedToDecimal(seed);
                {
                    var playerState = _saveData.GetObject("PlayerStateData");
                    var bases = playerState?.GetArray("PersistentPlayerBases");
                    int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx, seedDecimal);
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
            MessageBox.Show(UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool CheckCorvettePrimarySafety(string action)
    {
        if (_shipSelector.SelectedIndex < 0) return false;
        var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
        if (item.DataIndex == _primaryShipIndex)
        {
            MessageBox.Show(
                UiStrings.Format("starship.corvette_primary_corruption", action),
                UiStrings.Get("starship.important_warning"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    private void OnExportCorvette(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0 || _saveData == null) return;
            if (!CheckCorvettePrimarySafety("exporting")) return;

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);
            string seed = "";
            try
            {
                var resource = ship.GetObject("Resource");
                seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
            }
            catch { }

            long seedDecimal = StarshipLogic.SeedToDecimal(seed);

            var playerState = _saveData.GetObject("PlayerStateData");
            var bases = playerState?.GetArray("PersistentPlayerBases");
            int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx, seedDecimal);
            if (baseIdx < 0)
            {
                MessageBox.Show(UiStrings.Get("starship.corvette_no_base"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var baseObj = bases!.GetObject(baseIdx);

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
                Filter = ExportConfig.BuildDialogFilter(cfg.CorvetteExt, "Corvette files"),
                DefaultExt = cfg.CorvetteExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(cfg.CorvetteTemplate, cfg.CorvetteExt, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Create combined export object
                var export = new JsonObject();
                export.Set("Ship", ship);
                export.Set("Base", baseObj);
                export.ExportToFile(dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportCorvette(object? sender, EventArgs e)
    {
        try
        {
            if (_shipOwnership == null || _shipSelector.SelectedIndex < 0 || _saveData == null) return;
            if (!CheckCorvettePrimarySafety("importing")) return;

            var cfg = ExportConfig.Instance;

            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildOpenFilter(cfg.CorvetteExt, "Corvette files")
            };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            // Try ZIP format first (other editor's .nmsship format)
            var zipResult = StarshipLogic.TryReadNmsshipZip(dialog.FileName);

            JsonObject? importedShip;
            JsonObject? importedBase = null;
            JsonObject? zipCcd = null;

            if (zipResult != null)
            {
                // ZIP format: so.json is the ship data (no wrapping "Ship" key)
                importedShip = zipResult.Value.ship;
                zipCcd = zipResult.Value.ccd;

                // objects.json maps to base Objects
                if (zipResult.Value.objects != null)
                {
                    importedBase = new JsonObject();
                    importedBase.Set("Objects", zipResult.Value.objects);
                }
            }
            else
            {
                var imported = JsonObject.ImportFromFile(dialog.FileName);
                importedShip = imported.GetObject("Ship");
                importedBase = imported.GetObject("Base");
            }

            if (importedShip == null)
            {
                MessageBox.Show(UiStrings.Get("starship.no_valid_ship"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var item = (StarshipLogic.ShipListItem)_shipSelector.Items[_shipSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _shipOwnership.Length) return;

            var ship = _shipOwnership.GetObject(idx);

            // Copy ship data
            foreach (var name in importedShip.Names())
                ship.Set(name, importedShip.Get(name));

            // Apply ZIP CCD if present and non-default
            if (zipCcd != null && !StarshipLogic.IsCcdDefault(zipCcd))
            {
                var ccdArray = _playerState?.GetArray("CharacterCustomisationData");
                StarshipLogic.SetShipCustomisation(ccdArray, idx, zipCcd);
            }

            // Import base data if present
            if (importedBase != null)
            {
                // Get the current ship's seed to find its base slot
                string seed = "";
                try
                {
                    var resource = ship.GetObject("Resource");
                    seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
                }
                catch { }

                long seedDecimal = StarshipLogic.SeedToDecimal(seed);
                {
                    var playerState = _saveData.GetObject("PlayerStateData");
                    var bases = playerState?.GetArray("PersistentPlayerBases");
                    int baseIdx = StarshipLogic.FindCorvetteBaseIndex(bases, idx, seedDecimal);
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
            MessageBox.Show(UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
            string seed = "";
            try
            {
                var resource = ship.GetObject("Resource");
                seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
            }
            catch { }

            long seedDecimal = StarshipLogic.SeedToDecimal(seed);

            var playerState = _saveData.GetObject("PlayerStateData");
            var bases = playerState?.GetArray("PersistentPlayerBases");
            int result = StarshipLogic.OptimiseCorvetteBase(bases, idx, seedDecimal);

            if (result < 0)
            {
                MessageBox.Show(UiStrings.Get("starship.corvette_no_base"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // After optimising, update indicator to green
            SetOptimiseIndicator(true);

            MessageBox.Show(
                UiStrings.Format("starship.optimise_done", result),
                UiStrings.Get("starship.optimise"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DataModified?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format("Optimisation failed: {0}", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            string seed = "";
            try
            {
                var resource = ship.GetObject("Resource");
                seed = resource?.GetArray("Seed")?.Get(1)?.ToString() ?? "";
            }
            catch { }

            long seedDecimal = StarshipLogic.SeedToDecimal(seed);
            var playerState = _saveData.GetObject("PlayerStateData");
            var bases = playerState?.GetArray("PersistentPlayerBases");
            bool optimised = StarshipLogic.IsCorvetteOptimised(bases, shipIndex, seedDecimal);
            SetOptimiseIndicator(optimised);
        }
        catch
        {
            SetOptimiseIndicator(false);
        }
    }

}