using NMSE.Data;
using NMSE.Models;
using NMSE.Core;

namespace NMSE.UI.Panels;

public partial class FreighterPanel : UserControl
{
    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    private static string[] FreighterClasses => FreighterLogic.FreighterClasses;

    private JsonObject? _playerState;
    private JsonObject? _freighterBase;
    private readonly Random _rng = new();

    /// <summary>Raw (unclamped) freighter stat values read from JSON at load time.</summary>
    private Dictionary<string, double>? _rawFreighterStatValues;

    private static Dictionary<string, string> FreighterTypes => FreighterLogic.FreighterTypes;

    public FreighterPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _generalGrid.SetDatabase(database);
        _techGrid.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _generalGrid.SetIconManager(iconManager);
        _techGrid.SetIconManager(iconManager);
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

    private static void AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 4, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            var data = FreighterLogic.LoadFreighterData(_playerState);

            _freighterName.Text = data.Name;

            if (data.TypeDisplayName != null)
                SelectFreighterTypeByName(data.TypeDisplayName);
            else
                _freighterType.SelectedIndex = -1;

            _freighterClass.SelectedIndex = data.ClassIndex >= 0 ? data.ClassIndex : -1;

            _homeSeed.Text = data.HomeSeed;
            _modelSeed.Text = data.ModelSeed;

            _hyperdriveField.Value = (decimal)data.Hyperdrive;
            _fleetField.Value = (decimal)data.FleetCoordination;

            // Store raw stat values for preservation before limits clamp the NUDs
            _rawFreighterStatValues = new Dictionary<string, double>
            {
                ["^FREI_HYPERDRIVE"] = data.Hyperdrive,
                ["^FREI_FLEET"] = data.FleetCoordination,
            };

            // Apply BaseStatLimits to the NumericUpDown controls
            ApplyStatLimits(_hyperdriveField, "Normal", "^FREI_HYPERDRIVE", StatCategory.Freighter);
            ApplyStatLimits(_fleetField, "Normal", "^FREI_FLEET", StatCategory.Freighter);

            _freighterBase = data.FreighterBase;
            if (_freighterBase != null)
                _baseItemsField.Text = data.BaseItemCount.ToString();
            else
                _baseItemsField.Text = UiStrings.Get("common.na");

            _exportBtn.Enabled = _freighterBase != null;

            _generalGrid.LoadInventory(data.CargoInventory);
            _techGrid.LoadInventory(data.TechInventory);

            // Freighter rooms
            _roomList.Items.Clear();
            var rooms = FreighterLogic.DetectFreighterRooms(data.FreighterBase);
            foreach (var room in rooms)
                _roomList.Items.Add(room);

            // Crew NPC
            try
            {
                var npc = _playerState.GetObject("CurrentFreighterNPC");
                if (npc != null)
                {
                    string filename = npc.GetString("Filename") ?? "";
                    if (FreighterLogic.NpcResourceToRace.TryGetValue(filename, out string? race))
                        SelectCrewRaceByInternalName(race);
                    else
                        _crewRaceCombo.SelectedIndex = -1;

                    try
                    {
                        var seedArr = npc.GetArray("Seed");
                        _crewSeedField.Text = (seedArr != null && seedArr.Length >= 2) ? (seedArr.GetString(1) ?? "") : "";
                    }
                    catch { _crewSeedField.Text = ""; }
                }
            }
            catch { }

            string exportBase = FreighterLogic.BuildExportFileName(
                _freighterName.Text, (_freighterType.SelectedItem as FreighterLogic.FreighterTypeItem)?.InternalName, _freighterClass.SelectedIndex);
            var cfg = ExportConfig.Instance;
            _generalGrid.SetExportFileName($"{exportBase}_inv{cfg.FreighterCargoExt}");
            _techGrid.SetExportFileName($"{exportBase}_tech_inv{cfg.FreighterTechExt}");
            string cargoExportFilter = ExportConfig.BuildDialogFilter(cfg.FreighterCargoExt, "Freighter cargo inventory");
            string cargoImportFilter = ExportConfig.BuildImportFilter(cfg.FreighterCargoExt, "Freighter cargo inventory");
            _generalGrid.SetExportFileFilter(cargoExportFilter, cargoImportFilter, cfg.FreighterCargoExt.TrimStart('.'));
            string techExportFilter = ExportConfig.BuildDialogFilter(cfg.FreighterTechExt, "Freighter tech inventory");
            string techImportFilter = ExportConfig.BuildImportFilter(cfg.FreighterTechExt, "Freighter tech inventory");
            _techGrid.SetExportFileFilter(techExportFilter, techImportFilter, cfg.FreighterTechExt.TrimStart('.'));

            _generalGrid.SetMaxSupportedLabel(UiStrings.Format("common.max_supported", "10x12"));
            _techGrid.SetMaxSupportedLabel(UiStrings.Format("common.max_supported", "10x6"));
        }
        catch { }
        finally
        {
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            FreighterLogic.SaveFreighterData(playerState, new FreighterLogic.FreighterSaveValues
            {
                Name = _freighterName.Text,
                SelectedTypeName = (_freighterType.SelectedItem as FreighterLogic.FreighterTypeItem)?.InternalName,
                ClassIndex = _freighterClass.SelectedIndex,
                HomeSeed = _homeSeed.Text,
                ModelSeed = _modelSeed.Text,
                Hyperdrive = (double)_hyperdriveField.Value,
                FleetCoordination = (double)_fleetField.Value,
                RawStatValues = _rawFreighterStatValues
            });

            // Save inventories
            _generalGrid.SaveInventory(playerState.GetObject("FreighterInventory"));
            _techGrid.SaveInventory(playerState.GetObject("FreighterInventory_TechOnly"));

            // Crew NPC
            try
            {
                var npc = playerState.GetObject("CurrentFreighterNPC");
                if (npc != null)
                {
                    string? selectedRace = (_crewRaceCombo.SelectedItem as FreighterLogic.CrewRaceItem)?.InternalName;
                    if (!string.IsNullOrEmpty(selectedRace) && FreighterLogic.RaceToNpcResource.TryGetValue(selectedRace, out string? resource))
                        npc.Set("Filename", resource);

                    var seedArr = npc.GetArray("Seed");
                    var normalizedCrewSeed = SeedHelper.NormalizeSeed(_crewSeedField.Text);
                    if (seedArr != null && seedArr.Length >= 2 && normalizedCrewSeed != null)
                        seedArr.Set(1, normalizedCrewSeed);
                }
            }
            catch { }
        }
        catch { }
    }

    private void OnBackup(object? sender, EventArgs e)
    {
        try
        {
            if (_freighterBase == null)
            {
                MessageBox.Show(this, UiStrings.Get("freighter.backup_no_base"), UiStrings.Get("freighter.backup_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var config = ExportConfig.Instance;

            var vars = new Dictionary<string, string>
            {
                ["freighter_name"] = _freighterName.Text ?? "",
                ["type"] = (_freighterType.SelectedItem as FreighterLogic.FreighterTypeItem)?.InternalName ?? "",
                ["class"] = _freighterClass.SelectedItem as string ?? ""
            };

            using var dialog = new SaveFileDialog
            {
                Filter = ExportConfig.BuildDialogFilter(config.FreighterExt, "Freighter files"),
                DefaultExt = config.FreighterExt.TrimStart('.'),
                FileName = ExportConfig.BuildFileName(config.FreighterTemplate, config.FreighterExt, vars)
            };

            if (dialog.ShowDialog() == DialogResult.OK)
                _freighterBase.ExportToFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("freighter.backup_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnRestore(object? sender, EventArgs e)
    {
        try
        {
            if (_playerState == null) return;

            using var dialog = new OpenFileDialog
            {
                Filter = ExportConfig.BuildOpenFilter(ExportConfig.Instance.FreighterExt, "Freighter files")
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var imported = JsonObject.ImportFromFile(dialog.FileName);
            var bases = _playerState.GetArray("PersistentPlayerBases");
            if (bases == null) return;

            // Find existing freighter base and replace it
            for (int i = 0; i < bases.Length; i++)
            {
                try
                {
                    var b = bases.GetObject(i);
                    var baseType = b.GetObject("BaseType");
                    if (baseType != null
                        && "FreighterBase" == baseType.GetString("PersistentBaseTypes")
                        && b.GetInt("BaseVersion") >= 3)
                    {
                        // Replace all properties from imported base
                        foreach (var name in imported.Names())
                            b.Set(name, imported.Get(name));

                        _freighterBase = b;

                        // Update items count
                        try
                        {
                            var objects = _freighterBase.GetArray("Objects");
                            _baseItemsField.Text = objects != null ? objects.Length.ToString() : "0";
                        }
                        catch { _baseItemsField.Text = "0"; }

                        MessageBox.Show(this, UiStrings.Get("freighter.restore_success"), UiStrings.Get("freighter.restore_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                catch { }
            }

            MessageBox.Show(this, UiStrings.Get("freighter.restore_no_slot"), UiStrings.Get("freighter.restore_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("freighter.restore_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("freighter.title");
        _freighterSubPage.Text = UiStrings.Get("freighter.tab_freighter");
        _roomsSubPage.Text = UiStrings.Get("freighter.tab_rooms");
        _nameLabel.Text = UiStrings.Get("freighter.name");
        _freighterName.PlaceholderText = UiStrings.Get("common.procedural_no_name");
        _typeLabel.Text = UiStrings.Get("freighter.type");
        _classLabel.Text = UiStrings.Get("freighter.class");
        _homeSeedLabel.Text = UiStrings.Get("freighter.home_seed");
        _crewRaceLabel.Text = UiStrings.Get("freighter.crew_race");
        _hyperdriveLabel.Text = UiStrings.Get("freighter.hyperdrive");
        _fleetLabel.Text = UiStrings.Get("freighter.fleet_coord");
        _baseItemsLabel.Text = UiStrings.Get("freighter.items");
        _modelSeedLabel.Text = UiStrings.Get("freighter.model_seed");
        _crewSeedLabel.Text = UiStrings.Get("freighter.crew_seed");
        _generateHomeSeedBtn.Text = UiStrings.Get("common.generate");
        _generateModelSeedBtn.Text = UiStrings.Get("common.generate");
        _generateCrewSeedBtn.Text = UiStrings.Get("common.generate");
        _exportBtn.Text = UiStrings.Get("freighter.export");
        _importBtn.Text = UiStrings.Get("freighter.import");
        _generalPage.Text = UiStrings.Get("common.cargo");
        _techPage.Text = UiStrings.Get("common.technology");
        _generalGrid.SetMaxSupportedLabel(UiStrings.Format("common.max_supported", "10x12"));
        _techGrid.SetMaxSupportedLabel(UiStrings.Format("common.max_supported", "10x6"));
        _generalGrid.ApplyUiLocalisation();
        _techGrid.ApplyUiLocalisation();
        RefreshFreighterTypeCombo();
        RefreshCrewRaceCombo();
    }

    private void RefreshFreighterTypeCombo()
    {
        string? currentType = (_freighterType.SelectedItem as FreighterLogic.FreighterTypeItem)?.InternalName;
        _freighterType.Items.Clear();
        _freighterType.Items.AddRange(FreighterLogic.GetFreighterTypeItems());
        if (currentType != null)
            SelectFreighterTypeByName(currentType);
    }

    private void SelectFreighterTypeByName(string? englishTypeName)
    {
        if (string.IsNullOrEmpty(englishTypeName)) { _freighterType.SelectedIndex = -1; return; }
        for (int i = 0; i < _freighterType.Items.Count; i++)
        {
            if (_freighterType.Items[i] is FreighterLogic.FreighterTypeItem item &&
                item.InternalName.Equals(englishTypeName, StringComparison.OrdinalIgnoreCase))
            {
                _freighterType.SelectedIndex = i;
                return;
            }
        }
        _freighterType.SelectedIndex = -1;
    }

    private void SelectCrewRaceByInternalName(string? raceName)
    {
        if (string.IsNullOrEmpty(raceName)) { _crewRaceCombo.SelectedIndex = -1; return; }
        for (int i = 0; i < _crewRaceCombo.Items.Count; i++)
        {
            if (_crewRaceCombo.Items[i] is FreighterLogic.CrewRaceItem item &&
                item.InternalName.Equals(raceName, StringComparison.OrdinalIgnoreCase))
            {
                _crewRaceCombo.SelectedIndex = i;
                return;
            }
        }
        _crewRaceCombo.SelectedIndex = -1;
    }

    private void RefreshCrewRaceCombo()
    {
        string? currentRace = (_crewRaceCombo.SelectedItem as FreighterLogic.CrewRaceItem)?.InternalName;
        _crewRaceCombo.BeginUpdate();
        _crewRaceCombo.Items.Clear();
        _crewRaceCombo.Items.AddRange(FreighterLogic.GetCrewRaceItems());
        if (currentRace != null) SelectCrewRaceByInternalName(currentRace);
        _crewRaceCombo.EndUpdate();
    }

}