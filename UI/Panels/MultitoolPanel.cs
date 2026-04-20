using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class MultitoolPanel : UserControl
{
    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    private JsonArray? _multitools;
    private JsonObject? _playerState;
    private GameItemDatabase? _database;
    private readonly Random _rng = new();
    private int _activeToolIndex;

    /// <summary>Raw (unclamped) tool stat values read from JSON for the currently selected tool.</summary>
    private Dictionary<string, double>? _rawToolStatValues;

    /// <summary>Class index loaded from the save for the current tool, used to detect user changes.</summary>
    private int _originalClassIndex = -1;

    public MultitoolPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        _storeGrid.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _storeGrid.SetIconManager(iconManager);
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
        _titleLabel.Text = UiStrings.Get("multitool.title");
        _detailsLabel.Text = UiStrings.Get("multitool.details");
        _statsLabel.Text = UiStrings.Get("multitool.base_stats");
        _selectLabel.Text = UiStrings.Get("multitool.select");
        _nameLabel.Text = UiStrings.Get("multitool.name");
        _toolName.PlaceholderText = UiStrings.Get("common.procedural_no_name");
        _typeLabel.Text = UiStrings.Get("multitool.type");
        _classLabel.Text = UiStrings.Get("multitool.class");
        _seedLabel.Text = UiStrings.Get("multitool.seed");
        _damageLabel.Text = UiStrings.Get("multitool.damage");
        _miningLabel.Text = UiStrings.Get("multitool.mining");
        _scanLabel.Text = UiStrings.Get("multitool.scan");
        _generateSeedBtn.Text = UiStrings.Get("common.generate");
        _deleteBtn.Text = UiStrings.Get("multitool.delete");
        _exportBtn.Text = UiStrings.Get("multitool.export");
        _importBtn.Text = UiStrings.Get("multitool.import");
        _makePrimaryBtn.Text = UiStrings.Get("multitool.make_primary");
        _storeGrid.SetMaxSupportedLabel(UiStrings.Format("common.max_supported", "10x6"));
        RefreshToolTypeCombo();
        _storeGrid.ApplyUiLocalisation();
    }

    /// <summary>Rebuilds the selector from the current _multitools array.</summary>
    private void RefreshToolList()
    {
        _toolSelector.BeginUpdate();
        try
        {
        _toolSelector.Items.Clear();
        if (_multitools == null) return;

        var toolList = MultitoolLogic.BuildToolList(_multitools);
        foreach (var item in toolList)
            _toolSelector.Items.Add(item);
        }
        finally
        {
            _toolSelector.EndUpdate();
        }
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _toolSelector.BeginUpdate();
        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _multitools = _playerState.GetArray("Multitools");
            _toolSelector.Items.Clear();

            if (_multitools != null && _multitools.Length > 0)
            {
                RefreshToolList();

                _activeToolIndex = 0;
                try { _activeToolIndex = _playerState.GetInt("ActiveMultioolIndex"); } catch { }
                _primaryToolLabel.Text = UiStrings.Format("multitool.primary_label", MultitoolLogic.GetPrimaryToolName(_multitools, _activeToolIndex));

                if (_toolSelector.Items.Count > 0)
                {
                    int selectIdx = 0;
                    for (int i = 0; i < _toolSelector.Items.Count; i++)
                    {
                        if (((MultitoolLogic.ToolListItem)_toolSelector.Items[i]!).DataIndex == _activeToolIndex)
                        {
                            selectIdx = i;
                            break;
                        }
                    }
                    _toolSelector.SelectedIndex = Math.Clamp(selectIdx, 0, _toolSelector.Items.Count - 1);
                }
            }
            else
            {
                // Older saves without Multitools array use WeaponInventory directly
                _multitools = null;
                var weaponInv = _playerState.GetObject("WeaponInventory");
                if (weaponInv != null)
                {
                    string name = _playerState.GetString("PlayerWeaponName") ?? "Primary Weapon";
                    _toolSelector.Items.Add(name);
                    _toolName.Text = name;

                    // Load seed from CurrentWeapon.GenerationSeed[1]
                    try
                    {
                        var genSeed = _playerState.GetObject("CurrentWeapon")?.GetArray("GenerationSeed");
                        if (genSeed != null && genSeed.Length > 1)
                            _toolSeed.Text = genSeed.Get(1)?.ToString() ?? "";
                    }
                    catch { }

                    _storeGrid.LoadInventory(weaponInv);
                    _toolSelector.SelectedIndex = 0;
                }
            }
        }
        catch { }
        finally
        {
            _toolSelector.EndUpdate();
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var multitools = playerState.GetArray("Multitools");
            if (multitools != null && _toolSelector.SelectedIndex >= 0 && _toolSelector.Items.Count > 0)
            {
                var item = (MultitoolLogic.ToolListItem)_toolSelector.Items[_toolSelector.SelectedIndex]!;
                int idx = item.DataIndex;
                if (idx >= multitools.Length) return;

                // Save active multitool index (use tracked value, not current selection)
                try { RawNumberGuard.SetInt(playerState, "ActiveMultioolIndex", _activeToolIndex); } catch { }

                var tool = multitools.GetObject(idx);

                var values = new MultitoolLogic.ToolSaveValues
                {
                    Name = _toolName.Text,
                    ClassIndex = _toolClass.SelectedIndex,
                    OriginalClassIndex = _originalClassIndex,
                    TypeIndex = GetSelectedToolTypeIndex(),
                    Seed = _toolSeed.Text,
                    // Use raw values for unmodified fields to prevent any
                    // precision loss from the UI control text round-trip.
                    Damage = _damageField.UserModified
                        ? (_damageField.NumericValue ?? 0.0)
                        : (_rawToolStatValues?.GetValueOrDefault("^WEAPON_DAMAGE") ?? _damageField.NumericValue ?? 0.0),
                    Mining = _miningField.UserModified
                        ? (_miningField.NumericValue ?? 0.0)
                        : (_rawToolStatValues?.GetValueOrDefault("^WEAPON_MINING") ?? _miningField.NumericValue ?? 0.0),
                    Scan = _scanField.UserModified
                        ? (_scanField.NumericValue ?? 0.0)
                        : (_rawToolStatValues?.GetValueOrDefault("^WEAPON_SCAN") ?? _scanField.NumericValue ?? 0.0),
                    DamageText = _damageField.UserModified ? _damageField.DisplayText : null,
                    MiningText = _miningField.UserModified ? _miningField.DisplayText : null,
                    ScanText = _scanField.UserModified ? _scanField.DisplayText : null,
                    RawStatValues = _rawToolStatValues
                };

                // Determine if this is the primary tool for syncing purposes
                bool isPrimary = (idx == _activeToolIndex);

                MultitoolLogic.SaveToolData(tool, playerState, values, isPrimary);
                _storeGrid.SaveInventory(tool.GetObject("Store"));
            }
            else
            {
                // Old-format save
                var weaponInv = playerState.GetObject("WeaponInventory");
                _storeGrid.SaveInventory(weaponInv);
            }
        }
        catch { }
    }

    private void OnToolSelected(object? sender, EventArgs e)
    {
        RedrawHelper.Suspend(this);
        SuspendLayout();
        try
        {
            if (_toolSelector.SelectedIndex < 0) return;

            // New-format multitools
            if (_multitools != null && _toolSelector.Items.Count > 0)
            {
                var item = (MultitoolLogic.ToolListItem)_toolSelector.Items[_toolSelector.SelectedIndex]!;
                int idx = item.DataIndex;
                if (idx >= _multitools.Length) return;

                var tool = _multitools.GetObject(idx);
                var data = MultitoolLogic.LoadToolData(tool);

                _toolName.Text = data.Name;
                SelectToolTypeByIndex(data.TypeIndex);
                _toolClass.SelectedIndex = data.ClassIndex;
                _originalClassIndex = data.ClassIndex;
                _toolSeed.Text = data.Seed;

                _storeGrid.LoadInventory(data.Store);
                _storeGrid.SetExportFileName(data.ExportFileName);
                var cfg = ExportConfig.Instance;
                // Multitool has a single inventory (the "store") - use the tool extension for inventory export
                string exportFilter = ExportConfig.BuildDialogFilter(cfg.MultitoolExt, "Multitool inventory");
                string importFilter = ExportConfig.BuildImportFilter(cfg.MultitoolExt, "Multitool inventory", ".wp0", ".mlt");
                _storeGrid.SetExportFileFilter(exportFilter, importFilter, cfg.MultitoolExt.TrimStart('.'));

                try { _damageField.SetValueWithText(data.Damage, data.DamageText); } catch { _damageField.NumericValue = 0; }
                try { _miningField.SetValueWithText(data.Mining, data.MiningText); } catch { _miningField.NumericValue = 0; }
                try { _scanField.SetValueWithText(data.Scan, data.ScanText); } catch { _scanField.NumericValue = 0; }

                // Store raw stat values for preservation before limits clamp the NUDs
                _rawToolStatValues = new Dictionary<string, double>
                {
                    ["^WEAPON_DAMAGE"] = data.Damage,
                    ["^WEAPON_MINING"] = data.Mining,
                    ["^WEAPON_SCAN"] = data.Scan,
                };

            }
        }
        catch { }
        finally
        {
            ResumeLayout(true);
            RedrawHelper.Resume(this);
        }
    }

    private void OnToolNameChanged(object? sender, EventArgs e)
    {
        if (_toolSelector.SelectedIndex < 0 || _toolSelector.Items.Count == 0) return;
        var item = (MultitoolLogic.ToolListItem)_toolSelector.Items[_toolSelector.SelectedIndex]!;
        string newName = string.IsNullOrWhiteSpace(_toolName.Text) ? $"Multitool {item.DataIndex + 1}" : _toolName.Text;
        item.DisplayName = newName;
        int idx = _toolSelector.SelectedIndex;
        _toolSelector.SelectedIndexChanged -= OnToolSelected;
        _toolSelector.Items.RemoveAt(idx);
        _toolSelector.Items.Insert(idx, item);
        _toolSelector.SelectedIndex = idx;
        _toolSelector.SelectedIndexChanged += OnToolSelected;
    }

    private void OnDeleteTool(object? sender, EventArgs e)
    {
        try
        {
            if (_multitools == null || _playerState == null ||
                _toolSelector.SelectedIndex < 0 || _toolSelector.Items.Count == 0) return;

            // Prevent deleting the last valid multitool
            if (MultitoolLogic.CountValidTools(_multitools) <= 1)
            {
                MessageBox.Show(this, UiStrings.Get("multitool.cannot_delete_only"), UiStrings.Get("multitool.delete_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(this, 
                UiStrings.Get("multitool.delete_confirm"),
                UiStrings.Get("multitool.delete_title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            var item = (MultitoolLogic.ToolListItem)_toolSelector.Items[_toolSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _multitools.Length) return;

            // Invalidate the tool in place - do NOT remove from array.
            // This preserves index alignment, matching the ship deletion approach.
            MultitoolLogic.DeleteToolData(_multitools.GetObject(idx));

            // If the deleted tool was the active multitool, reassign to the first valid tool
            if (idx == _activeToolIndex)
            {
                _activeToolIndex = MultitoolLogic.FindFirstValidToolIndex(_multitools);
                if (_activeToolIndex < 0) _activeToolIndex = 0;
                try { RawNumberGuard.SetInt(_playerState, "ActiveMultioolIndex", _activeToolIndex); } catch { }
            }
            _primaryToolLabel.Text = UiStrings.Format("multitool.primary_label", MultitoolLogic.GetPrimaryToolName(_multitools, _activeToolIndex));

            // Rebuild the tool list (BuildToolList skips invalidated slots)
            int selIdx = _toolSelector.SelectedIndex;
            _toolSelector.Items.Clear();
            var toolList = MultitoolLogic.BuildToolList(_multitools);
            foreach (var toolItem in toolList)
                _toolSelector.Items.Add(toolItem);

            if (_toolSelector.Items.Count > 0)
                _toolSelector.SelectedIndex = Math.Min(selIdx, _toolSelector.Items.Count - 1);
            else
                _storeGrid.LoadInventory(null);
        }
        catch { }
    }

    private void OnMakePrimary(object? sender, EventArgs e)
    {
        try
        {
            if (_multitools == null || _playerState == null || _toolSelector.SelectedIndex < 0) return;
            var item = (MultitoolLogic.ToolListItem)_toolSelector.Items[_toolSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _multitools.Length) return;

            _activeToolIndex = idx;
            try { RawNumberGuard.SetInt(_playerState, "ActiveMultioolIndex", _activeToolIndex); } catch { }
            _primaryToolLabel.Text = UiStrings.Format("multitool.primary_label", MultitoolLogic.GetPrimaryToolName(_multitools, _activeToolIndex));
        }
        catch { }
    }

    private void OnExportTool(object? sender, EventArgs e)
    {
        try
        {
            if (_multitools == null || _toolSelector.SelectedIndex < 0 || _toolSelector.Items.Count == 0) return;

            var item = (MultitoolLogic.ToolListItem)_toolSelector.Items[_toolSelector.SelectedIndex]!;
            int idx = item.DataIndex;
            if (idx >= _multitools.Length) return;

            var tool = _multitools.GetObject(idx);
            var config = ExportConfig.Instance;

            string typeName = (_toolType.SelectedItem as MultitoolLogic.ToolTypeItem)?.InternalName ?? "Unknown";
            string className = _toolClass.SelectedIndex >= 0 && _toolClass.SelectedIndex < MultitoolLogic.ToolClasses.Length
                ? MultitoolLogic.ToolClasses[_toolClass.SelectedIndex]
                : "C";

            var vars = new Dictionary<string, string>
            {
                ["multitool_name"] = _toolName.Text ?? "",
                ["type"] = typeName,
                ["class"] = className
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(config.MultitoolExt, "Multitool files"),
                DefaultExt = config.MultitoolExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(config.MultitoolTemplate, config.MultitoolExt, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                tool.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportTool(object? sender, EventArgs e)
    {
        try
        {
            if (_multitools == null || _playerState == null) return;

            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildImportFilter(ExportConfig.Instance.MultitoolExt, "Multitool files", ".wp0", ".mlt")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Unwrap Data envelope if present (Data -> Multitool)
            imported = InventoryImportHelper.UnwrapNomNom(imported, "Multitool");

            int emptyIdx = MultitoolLogic.FindEmptySlot(_multitools);

            if (emptyIdx < 0)
            {
                MessageBox.Show(this, UiStrings.Get("multitool.no_empty_slots"), UiStrings.Get("multitool.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var target = _multitools.GetObject(emptyIdx);
            foreach (var name in imported.Names())
                target.Set(name, imported.Get(name));

            // Refresh the list by reloading
            int prevSel = _toolSelector.SelectedIndex;
            RefreshToolList();

            if (_toolSelector.Items.Count > 0)
            {
                int newSelIdx = -1;
                for (int i = 0; i < _toolSelector.Items.Count; i++)
                {
                    if (((MultitoolLogic.ToolListItem)_toolSelector.Items[i]!).DataIndex == emptyIdx)
                    {
                        newSelIdx = i;
                        break;
                    }
                }
                _toolSelector.SelectedIndex = newSelIdx >= 0 ? newSelIdx : Math.Clamp(prevSel, 0, _toolSelector.Items.Count - 1);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshToolTypeCombo()
    {
        int currentTypeIndex = GetSelectedToolTypeIndex();
        _toolType.Items.Clear();
        _toolType.Items.AddRange(MultitoolLogic.GetToolTypeItems());
        if (currentTypeIndex >= 0)
            SelectToolTypeByIndex(currentTypeIndex);
    }

    private int GetSelectedToolTypeIndex()
    {
        if (_toolType.SelectedItem is MultitoolLogic.ToolTypeItem item)
        {
            int idx = Array.FindIndex(MultitoolLogic.ToolTypes, t => t.Name.Equals(item.InternalName, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? idx : _toolType.SelectedIndex;
        }
        return _toolType.SelectedIndex;
    }

    private void SelectToolTypeByIndex(int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= MultitoolLogic.ToolTypes.Length) { _toolType.SelectedIndex = -1; return; }
        string targetName = MultitoolLogic.ToolTypes[typeIndex].Name;
        for (int i = 0; i < _toolType.Items.Count; i++)
        {
            if (_toolType.Items[i] is MultitoolLogic.ToolTypeItem item &&
                item.InternalName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                _toolType.SelectedIndex = i;
                return;
            }
        }
        _toolType.SelectedIndex = -1;
    }

}
