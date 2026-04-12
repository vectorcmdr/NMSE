using NMSE.Data;
using NMSE.Models;
using NMSE.Core;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class ExocraftPanel : UserControl
{
    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    private static (int Index, string Name)[] VehicleTypes => ExocraftLogic.VehicleTypes;

    private JsonArray? _vehicleOwnership;
    private JsonObject? _saveData;
    private JsonObject? _savedPlayerState;
    private readonly List<int> _addedVehicleIndices = new();

    public ExocraftPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    private void SetupLayout()
    {
        _inventoryGrid.SetIsCargoInventory(true);
        _inventoryGrid.SetSuperchargeDisabled(true);
        _inventoryGrid.SetInventoryOwnerType("Vehicle");
        _inventoryGrid.SetInventoryGroup("Vehicle");
        _inventoryGrid.SetMaxSupportedLabel("");
        _inventoryGrid.DataModified += OnInventoryDataModified;

        _techGrid.SetIsTechInventory(true);
        _techGrid.SetSuperchargeDisabled(true);
        _techGrid.SetSlotToggleDisabled(true);
        _techGrid.SetInventoryOwnerType("Vehicle");
        _techGrid.SetInventoryGroup("Vehicle");
        _techGrid.SetMaxSupportedLabel("");
        _techGrid.DataModified += OnTechDataModified;

        _invTabs.SelectedIndexChanged += OnInvTabSelectedIndexChanged;
    }

    private void OnInventoryDataModified(object? sender, EventArgs e)
    {
        DataModified?.Invoke(this, e);
    }

    private void OnTechDataModified(object? sender, EventArgs e)
    {
        DataModified?.Invoke(this, e);
    }

    private void OnInvTabSelectedIndexChanged(object? sender, EventArgs e)
    {
        _techNoteLabel.Visible = _invTabs.SelectedIndex == 1;
    }

    public void SetDatabase(GameItemDatabase? database)
    {
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
        _vehicleSelector.BeginUpdate();
        try
        {
        _vehicleSelector.Items.Clear();
        _addedVehicleIndices.Clear();
        _inventoryGrid.LoadInventory(null);
        _techGrid.LoadInventory(null);
        _saveData = saveData;
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _savedPlayerState = playerState;

            _vehicleOwnership = playerState.GetArray("VehicleOwnership");
            if (_vehicleOwnership == null) return;

            foreach (var (index, name) in VehicleTypes)
            {
                if (index < _vehicleOwnership.Length)
                {
                    _vehicleSelector.Items.Add(ExocraftLogic.GetLocalisedVehicleTypeName(name));
                    _addedVehicleIndices.Add(index);
                }
            }

            if (_vehicleSelector.Items.Count > 0)
                _vehicleSelector.SelectedIndex = 0;

            // Third person camera (stored in CommonStateData)
            try { _thirdPersonCam.Checked = saveData.GetObject("CommonStateData")?.GetBool("UsesThirdPersonVehicleCam") ?? false; } catch { _thirdPersonCam.Checked = false; }
            // Minotaur AI pilot
            try { _minotaurAI.Checked = playerState.GetBool("VehicleAIControlEnabled"); } catch { _minotaurAI.Checked = false; }
        }
        catch { }
        }
        finally
        {
            _vehicleSelector.EndUpdate();
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            var vehicles = playerState.GetArray("VehicleOwnership");
            if (vehicles == null || _vehicleSelector.SelectedIndex < 0) return;

            int selIdx = _vehicleSelector.SelectedIndex;
            if (selIdx < 0 || selIdx >= _addedVehicleIndices.Count) return;
            int arrIdx = _addedVehicleIndices[selIdx];

            var vehicle = vehicles.GetObject(arrIdx);
            _inventoryGrid.SaveInventory(vehicle.GetObject("Inventory"));
            _techGrid.SaveInventory(vehicle.GetObject("Inventory_TechOnly"));

            // Save vehicle name
            try { vehicle.Set("Name", _nameField.Text); } catch { }

            // Third person camera
            try { saveData.GetObject("CommonStateData")?.Set("UsesThirdPersonVehicleCam", _thirdPersonCam.Checked); } catch { }
            // Minotaur AI pilot
            try { playerState.Set("VehicleAIControlEnabled", _minotaurAI.Checked); } catch { }
        }
        catch { }
    }

    private void OnVehicleSelected(object? sender, EventArgs e)
    {
        RedrawHelper.Suspend(this);
        SuspendLayout();
        try
        {
            if (_vehicleOwnership == null || _vehicleSelector.SelectedIndex < 0) return;
            int selIdx = _vehicleSelector.SelectedIndex;
            if (selIdx >= _addedVehicleIndices.Count) return;
            int arrIdx = _addedVehicleIndices[selIdx];

            var vehicle = _vehicleOwnership.GetObject(arrIdx);

            // Dynamically set owner type based on vehicle subtype for proper tech filtering.
            // Must be set BEFORE LoadInventory so the item picker filters reflect the correct owner.
            string vehicleName = GetSelectedVehicleInternalName();
            string ownerType = ExocraftLogic.GetOwnerTypeForVehicle(vehicleName);
            _inventoryGrid.BeginBatchUpdate();
            _techGrid.BeginBatchUpdate();
            try
            {
                _inventoryGrid.SetInventoryOwnerType(ownerType);
                _techGrid.SetInventoryOwnerType(ownerType);
            }
            finally
            {
                _techGrid.EndBatchUpdate();
                _inventoryGrid.EndBatchUpdate();
            }

            _inventoryGrid.LoadInventory(vehicle.GetObject("Inventory"));
            _techGrid.LoadInventory(vehicle.GetObject("Inventory_TechOnly"));

            // Load vehicle name
            try { _nameField.Text = vehicle.GetString("Name") ?? ""; } catch { _nameField.Text = ""; }

            // Primary vehicle check
            try
            {
                int primaryIdx = _savedPlayerState?.GetInt("PrimaryVehicle") ?? -1;
                _primaryVehicleCheck.Checked = (arrIdx == primaryIdx);
            }
            catch { _primaryVehicleCheck.Checked = false; }

            // Deployed status
            try
            {
                long location = 0;
                try { location = vehicle.GetLong("Location"); } catch { }
                bool deployed = location != 0;
                _deployedLabel.Text = deployed ? UiStrings.Get("exocraft.status_deployed") : UiStrings.Get("exocraft.status_not_deployed");
                _deployedLabel.ForeColor = deployed ? System.Drawing.Color.Green : System.Drawing.Color.Gray;
                _undeployBtn.Enabled = deployed;
            }
            catch { _deployedLabel.Text = ""; _undeployBtn.Enabled = false; }

            // Set export filenames based on vehicle name
            string exportName = vehicleName.Replace(' ', '_');
            var cfg = ExportConfig.Instance;
            _inventoryGrid.SetExportFileName($"{exportName}_cargo_inv{cfg.ExocraftCargoExt}");
            _techGrid.SetExportFileName($"{exportName}_tech_inv{cfg.ExocraftTechExt}");
            string cargoExportFilter = ExportConfig.BuildDialogFilter(cfg.ExocraftCargoExt, "Exocraft cargo inventory");
            string cargoImportFilter = ExportConfig.BuildImportFilter(cfg.ExocraftCargoExt, "Exocraft cargo inventory", ".exo");
            _inventoryGrid.SetExportFileFilter(cargoExportFilter, cargoImportFilter, cfg.ExocraftCargoExt.TrimStart('.'));
            string techExportFilter = ExportConfig.BuildDialogFilter(cfg.ExocraftTechExt, "Exocraft tech inventory");
            string techImportFilter = ExportConfig.BuildImportFilter(cfg.ExocraftTechExt, "Exocraft tech inventory", ".exo");
            _techGrid.SetExportFileFilter(techExportFilter, techImportFilter, cfg.ExocraftTechExt.TrimStart('.'));
        }
        catch { }
        finally
        {
            ResumeLayout(true);
            RedrawHelper.Resume(this);
        }
    }

    private void OnExportVehicle(object? sender, EventArgs e)
    {
        try
        {
            if (_vehicleOwnership == null || _vehicleSelector.SelectedIndex < 0)
            {
                MessageBox.Show(this, UiStrings.Get("exocraft.no_vehicle_selected"), UiStrings.Get("common.export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int selIdx = _vehicleSelector.SelectedIndex;
            if (selIdx >= _addedVehicleIndices.Count) return;
            int arrIdx = _addedVehicleIndices[selIdx];

            var vehicle = _vehicleOwnership.GetObject(arrIdx);

            var config = ExportConfig.Instance;
            var vars = new Dictionary<string, string>
            {
                ["vehicle_name"] = _nameField.Text ?? "",
                ["vehicle_type"] = GetSelectedVehicleInternalName()
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(config.ExocraftExt, "Exocraft files"),
                DefaultExt = config.ExocraftExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(config.ExocraftTemplate, config.ExocraftExt, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                vehicle.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportVehicle(object? sender, EventArgs e)
    {
        try
        {
            if (_vehicleOwnership == null || _vehicleSelector.SelectedIndex < 0)
            {
                MessageBox.Show(this, UiStrings.Get("exocraft.no_vehicle_selected"), UiStrings.Get("common.import"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int selIdx = _vehicleSelector.SelectedIndex;
            if (selIdx >= _addedVehicleIndices.Count) return;
            int arrIdx = _addedVehicleIndices[selIdx];

            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildImportFilter(ExportConfig.Instance.ExocraftExt, "Exocraft files", ".exo")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Unwrap NomNom wrapper if present (Data -> Vehicle)
            imported = InventoryImportHelper.UnwrapNomNom(imported, "Vehicle");

            var vehicle = _vehicleOwnership.GetObject(arrIdx);

            // Replace all properties from imported vehicle
            foreach (var name in imported.Names())
                vehicle.Set(name, imported.Get(name));

            // Reload the inventories from the updated vehicle
            _inventoryGrid.LoadInventory(vehicle.GetObject("Inventory"));
            _techGrid.LoadInventory(vehicle.GetObject("Inventory_TechOnly"));

            MessageBox.Show(this, UiStrings.Get("exocraft.import_success"), UiStrings.Get("common.import"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnPrimaryVehicleChanged(object? sender, EventArgs e)
    {
        if (_savedPlayerState == null || _vehicleSelector.SelectedIndex < 0) return;
        int selIdx = _vehicleSelector.SelectedIndex;
        if (selIdx >= _addedVehicleIndices.Count) return;
        int arrIdx = _addedVehicleIndices[selIdx];

        if (_primaryVehicleCheck.Checked)
        {
            try { _savedPlayerState.Set("PrimaryVehicle", arrIdx); } catch { }
        }
        else
        {
            try
            {
                int currentPrimary = _savedPlayerState.GetInt("PrimaryVehicle");
                if (currentPrimary == arrIdx)
                    _savedPlayerState.Set("PrimaryVehicle", -1);
            }
            catch { }
        }
    }

    private void OnUndeploy(object? sender, EventArgs e)
    {
        if (_vehicleOwnership == null || _vehicleSelector.SelectedIndex < 0) return;
        int selIdx = _vehicleSelector.SelectedIndex;
        if (selIdx >= _addedVehicleIndices.Count) return;
        int arrIdx = _addedVehicleIndices[selIdx];

        var result = MessageBox.Show(this, UiStrings.Get("exocraft.undeploy_confirm"), UiStrings.Get("exocraft.undeploy_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        try
        {
            var vehicle = _vehicleOwnership.GetObject(arrIdx);
            vehicle.Set("Location", 0);

            try
            {
                var dir = vehicle.GetArray("Direction");
                if (dir != null && dir.Length >= 4)
                {
                    dir.Set(0, 0.0);
                    dir.Set(1, 0.0);
                    dir.Set(2, 0.0);
                    dir.Set(3, -1.0);
                }
            }
            catch { }

            try
            {
                var pos = vehicle.GetArray("Position");
                if (pos != null && pos.Length >= 4)
                {
                    pos.Set(0, 0.0);
                    pos.Set(1, 0.0);
                    pos.Set(2, 0.0);
                    pos.Set(3, -1.0);
                }
            }
            catch { }

            _deployedLabel.Text = UiStrings.Get("exocraft.status_not_deployed");
            _deployedLabel.ForeColor = System.Drawing.Color.Gray;
            _undeployBtn.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("exocraft.undeploy_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("exocraft.title");
        _vehicleLabel.Text = UiStrings.Get("exocraft.vehicle");
        _nameLabel.Text = UiStrings.Get("exocraft.name");
        _exportBtn.Text = UiStrings.Get("exocraft.export");
        _importBtn.Text = UiStrings.Get("exocraft.import");
        _thirdPersonCam.Text = UiStrings.Get("exocraft.third_person");
        _minotaurAI.Text = UiStrings.Get("exocraft.minotaur_ai");
        _techNoteLabel.Text = UiStrings.Get("exocraft.supercharge_note");
        _invPage.Text = UiStrings.Get("common.cargo");
        _techPage.Text = UiStrings.Get("common.technology");
        _primaryVehicleCheck.Text = UiStrings.Get("exocraft.primary_vehicle");
        _undeployBtn.Text = UiStrings.Get("exocraft.undeploy_title");
        RefreshVehicleCombo();
        _inventoryGrid.ApplyUiLocalisation();
        _techGrid.ApplyUiLocalisation();
    }

    private string GetSelectedVehicleInternalName()
    {
        int selIdx = _vehicleSelector.SelectedIndex;
        if (selIdx < 0 || selIdx >= _addedVehicleIndices.Count) return "vehicle";
        int arrIdx = _addedVehicleIndices[selIdx];
        foreach (var (index, name) in VehicleTypes)
        {
            if (index == arrIdx) return name;
        }
        return "vehicle";
    }

    private void RefreshVehicleCombo()
    {
        int currentSel = _vehicleSelector.SelectedIndex;
        _vehicleSelector.Items.Clear();
        foreach (var idx in _addedVehicleIndices)
        {
            string internalName = "vehicle";
            foreach (var (vIdx, name) in VehicleTypes)
            {
                if (vIdx == idx) { internalName = name; break; }
            }
            _vehicleSelector.Items.Add(ExocraftLogic.GetLocalisedVehicleTypeName(internalName));
        }
        if (currentSel >= 0 && currentSel < _vehicleSelector.Items.Count)
            _vehicleSelector.SelectedIndex = currentSel;
    }
}
