using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class SquadronPanel : UserControl
{
    private readonly Random _rng = new();

    private JsonArray? _pilots;
    private JsonArray? _unlockedSlots;
    private bool _loading;

    public SquadronPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _pilotList.BeginUpdate();
        try
        {
        _pilotList.Items.Clear();
        _detailPanel.Visible = false;
        _pilots = null;
        _unlockedSlots = null;

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _pilots = playerState.GetArray("SquadronPilots");
            _unlockedSlots = playerState.GetArray("SquadronUnlockedPilotSlots");

            if (_pilots == null || _pilots.Length == 0)
            {
                _countLabel.Text = UiStrings.Get("squadron.no_pilots_found");
                return;
            }

            RefreshList();
        }
        catch { _countLabel.Text = UiStrings.Get("squadron.load_failed"); }
        }
        finally
        {
            _pilotList.EndUpdate();
            ResumeLayout(true);
        }
    }

    // No-op: edits are applied directly to the underlying JsonObjects.
    public void SaveData(JsonObject saveData) { }

    private JsonObject? SelectedPilot()
    {
        int idx = _pilotList.SelectedIndex;
        if (idx < 0 || _pilots == null || idx >= _pilots.Length) return null;
        try { return _pilots.GetObject(idx); } catch { return null; }
    }

    private void RefreshList()
    {
        int sel = _pilotList.SelectedIndex;
        _pilotList.BeginUpdate();
        try
        {
        _pilotList.Items.Clear();
        if (_pilots == null) return;

        for (int i = 0; i < _pilots.Length; i++)
        {
            try
            {
                var p = _pilots.GetObject(i);
                _pilotList.Items.Add(SquadronLogic.GetPilotDisplayName(p, i));
            }
            catch { _pilotList.Items.Add(UiStrings.Format("squadron.pilot_format", i)); }
        }

        _countLabel.Text = UiStrings.Format("squadron.total_pilots", _pilots.Length);
        if (sel >= 0 && sel < _pilotList.Items.Count)
            _pilotList.SelectedIndex = sel;
        }
        finally
        {
            _pilotList.EndUpdate();
        }
    }

    private void OnPilotSelected(object? sender, EventArgs e)
    {
        var pilot = SelectedPilot();
        if (pilot == null)
        {
            _detailPanel.Visible = false;
            return;
        }

        _loading = true;
        try
        {
            _detailPanel.Visible = true;

            // Race
            string race = SquadronLogic.GetPilotRace(pilot);
            int raceIdx = Array.IndexOf(SquadronLogic.PilotRaces, race);
            _raceField.SelectedIndex = raceIdx >= 0 ? raceIdx : -1;

            // Rank (as ComboBox: C=0, B=1, A=2, S=3)
            try
            {
                int rankVal = Math.Min(3, Math.Max(0, pilot.GetInt("PilotRank")));
                _rankField.SelectedIndex = rankVal;
            }
            catch { _rankField.SelectedIndex = 0; }

            // Ship type (ComboBox from resource filename)
            string shipType = SquadronLogic.GetShipType(pilot);
            int shipIdx = -1;
            for (int j = 0; j < _shipTypeCombo.Items.Count; j++)
            {
                if (_shipTypeCombo.Items[j] is StarshipLogic.ShipTypeItem item &&
                    item.InternalName.Equals(shipType, StringComparison.OrdinalIgnoreCase))
                {
                    shipIdx = j;
                    break;
                }
            }
            _shipTypeCombo.SelectedIndex = shipIdx;

            // Seeds
            _npcSeedField.Text = SquadronLogic.ReadSeed(pilot, "NPCResource");
            _shipSeedField.Text = SquadronLogic.ReadSeed(pilot, "ShipResource");
            _traitsSeedField.Text = pilot.GetString("TraitsSeed") ?? "";

            // Resources
            try { _npcResourceField.Text = pilot.GetObject("NPCResource")?.GetString("Filename") ?? ""; }
            catch { _npcResourceField.Text = ""; }
            try { _shipResourceField.Text = pilot.GetObject("ShipResource")?.GetString("Filename") ?? ""; }
            catch { _shipResourceField.Text = ""; }

            // Slot unlocked status
            try
            {
                int slotIdx = _pilotList.SelectedIndex;
                _unlockedCheck.Checked = _unlockedSlots != null && slotIdx >= 0 && slotIdx < _unlockedSlots.Length
                    && _unlockedSlots.GetBool(slotIdx);
            }
            catch { _unlockedCheck.Checked = false; }
        }
        catch { }
        finally { _loading = false; }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        var pilot = SelectedPilot();
        if (pilot == null) return;

        var result = MessageBox.Show(this, UiStrings.Get("squadron.delete_confirm"), UiStrings.Get("squadron.delete_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        SquadronLogic.DeletePilot(pilot);
        RefreshList();
        OnPilotSelected(null, EventArgs.Empty);
    }

    private void RefreshListEntry()
    {
        int idx = _pilotList.SelectedIndex;
        if (idx < 0 || _pilots == null || idx >= _pilots.Length) return;
        try
        {
            var p = _pilots.GetObject(idx);
            _pilotList.Items[idx] = SquadronLogic.GetPilotDisplayName(p, idx);
        }
        catch { }
    }

    private void WriteNpcSeed()
    {
        var pilot = SelectedPilot();
        if (pilot != null) SquadronLogic.WriteSeed(pilot, "NPCResource", _npcSeedField.Text);
    }

    private void WriteShipSeed()
    {
        var pilot = SelectedPilot();
        if (pilot != null) SquadronLogic.WriteSeed(pilot, "ShipResource", _shipSeedField.Text);
    }

    private void WriteTraitsSeed()
    {
        var pilot = SelectedPilot();
        if (pilot != null)
        {
            var normalized = SeedHelper.NormalizeSeed(_traitsSeedField.Text);
            if (normalized != null)
                try { pilot.Set("TraitsSeed", normalized); } catch { }
        }
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    private Label AddSeedRow(TableLayoutPanel layout, string label, TextBox seedField, Action writeSeed, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            AutoSize = true,
        };
        seedField.Dock = DockStyle.None;
        seedField.Width = 200;
        panel.Controls.Add(seedField);
        var btn = new Button { Text = "Generate", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        btn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            string seed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
            seedField.Text = seed;
            if (!_loading) writeSeed();
        };
        panel.Controls.Add(btn);
        layout.Controls.Add(panel, 1, row);
        return lbl;
    }

    private static Label AddSectionHeader(TableLayoutPanel layout, string text, int row)
    {
        var lbl = new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 2)
        };
        FontManager.ApplyHeadingFont(lbl, 10);
        layout.Controls.Add(lbl, 0, row);
        layout.SetColumnSpan(lbl, 2);
        return lbl;
    }

    private void OnExport(object? sender, EventArgs e)
    {
        var pilot = SelectedPilot();
        if (pilot == null) return;

        var config = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["race"] = _raceField.SelectedIndex >= 0 ? SquadronLogic.PilotRaces[_raceField.SelectedIndex] : UiStrings.Get("common.unknown"),
            ["type"] = (_shipTypeCombo.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName ?? UiStrings.Get("common.unknown"),
            ["rank"] = _rankField.SelectedItem as string ?? "C",
            ["seed"] = _npcSeedField.Text ?? ""
        };

        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.SquadronExt, "Squadron files"),
            DefaultExt = config.SquadronExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.SquadronTemplate, config.SquadronExt, vars)
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try { pilot.ExportToFile(dialog.FileName); }
            catch (Exception ex)
            {
                MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        if (_pilots == null) return;

        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildImportFilter(ExportConfig.Instance.SquadronExt, "Squadron files", ".sqd")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Unwrap NomNom wrapper if present (Data -> Pilot)
            imported = InventoryImportHelper.UnwrapNomNomPilot(imported);

            // Find first empty slot (where NPCResource.Filename is empty or "^")
            int targetIdx = -1;
            for (int i = 0; i < _pilots.Length; i++)
            {
                try
                {
                    var p = _pilots.GetObject(i);
                    string npcRes = p.GetObject("NPCResource")?.GetString("Filename") ?? "";
                    if (string.IsNullOrEmpty(npcRes) || npcRes == "^")
                    {
                        targetIdx = i;
                        break;
                    }
                }
                catch { }
            }

            if (targetIdx < 0)
            {
                MessageBox.Show(this, UiStrings.Get("squadron.no_empty_slot"), UiStrings.Get("squadron.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var target = _pilots.GetObject(targetIdx);
            foreach (var name in imported.Names())
                target.Set(name, imported.Get(name));

            RefreshList();
            _pilotList.SelectedIndex = targetIdx;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("squadron.pilots_title");
        _countLabel.Text = UiStrings.Get("squadron.no_pilots");
        _deleteBtn.Text = UiStrings.Get("common.delete");
        _exportBtn.Text = UiStrings.Get("squadron.export");
        _importBtn.Text = UiStrings.Get("squadron.import");
        _pilotInfoLabel.Text = UiStrings.Get("squadron.pilot_info");
        _raceLabel.Text = UiStrings.Get("squadron.race");
        _rankLabel.Text = UiStrings.Get("squadron.rank");
        _shipTypeLabel.Text = UiStrings.Get("squadron.ship_type");
        _npcSeedLabel.Text = UiStrings.Get("squadron.npc_seed");
        _shipSeedLabel.Text = UiStrings.Get("squadron.ship_seed");
        _traitsSeedLabel.Text = UiStrings.Get("squadron.traits_seed");
        _npcResourceLabel.Text = UiStrings.Get("squadron.npc_resource");
        _shipResourceLabel.Text = UiStrings.Get("squadron.ship_resource");
        _slotUnlockedLabel.Text = UiStrings.Get("squadron.slot_unlocked");

        // Refresh ship type combo with localised display names
        string? currentType = (_shipTypeCombo.SelectedItem as StarshipLogic.ShipTypeItem)?.InternalName;
        _shipTypeCombo.Items.Clear();
        _shipTypeCombo.Items.AddRange(SquadronLogic.GetShipTypeItems());
        if (currentType != null)
        {
            for (int i = 0; i < _shipTypeCombo.Items.Count; i++)
            {
                if (_shipTypeCombo.Items[i] is StarshipLogic.ShipTypeItem item &&
                    item.InternalName.Equals(currentType, StringComparison.OrdinalIgnoreCase))
                {
                    _shipTypeCombo.SelectedIndex = i;
                    break;
                }
            }
        }

        // Refresh race combo with localised display names
        int currentRaceIdx = _raceField.SelectedIndex;
        _raceField.BeginUpdate();
        _raceField.Items.Clear();
        foreach (var r in SquadronLogic.PilotRaces)
            _raceField.Items.Add(SquadronLogic.GetLocalisedPilotRaceName(r));
        if (currentRaceIdx >= 0 && currentRaceIdx < _raceField.Items.Count)
            _raceField.SelectedIndex = currentRaceIdx;
        _raceField.EndUpdate();
    }
}
