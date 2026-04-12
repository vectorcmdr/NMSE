using NMSE.Data;
using NMSE.Models;
using NMSE.Core;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

public partial class FrigatePanel : UserControl
{
    private JsonArray? _frigates;
    private JsonArray? _expeditions;
    private bool _loading;
    private readonly Random _rng = new();

    /// <summary>Raw (unclamped) stat values for the currently-selected frigate, keyed by stat index (0-10).</summary>
    private readonly Dictionary<int, int> _rawStatValues = new();

    // Max 30 frigates per fleet (game engine limit)
    private const int MaxFrigates = 30;

    private static string[] FrigateTypes => FrigateLogic.FrigateTypes;
    private static string[] FrigateGrades => FrigateLogic.FrigateGrades;
    private static string[] FrigateRaces => FrigateLogic.FrigateRaces;
    private static string[] StatNames => FrigateLogic.StatNames;

    public FrigatePanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void SetDatabase(GameItemDatabase? database) { }

    /// <summary>
    /// Repopulates the trait combo boxes from FrigateTraitDatabase.
    /// Must be called after LoadDatabase() since the panels are constructed
    /// before the JSON databases are loaded.
    /// </summary>
    public void RefreshTraitCombos()
    {
        foreach (var cb in _traitFields)
        {
            object? selected = cb.SelectedItem;
            cb.Items.Clear();
            cb.Items.Add(FrigateTraitDatabase.None);
            foreach (var t in FrigateTraitDatabase.Traits)
                cb.Items.Add(t);
            if (selected != null && cb.Items.Contains(selected))
                cb.SelectedItem = selected;
        }
    }

    public void LoadData(JsonObject saveData)
    {
        SuspendLayout();
        _frigateList.BeginUpdate();
        try
        {
        _frigateList.Items.Clear();
        _detailPanel.Visible = false;
        _statsPanel.Visible = false;
        _frigates = null;
        _expeditions = null;

        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _frigates = playerState.GetArray("FleetFrigates");
            try { _expeditions = playerState.GetArray("FleetExpeditions"); } catch { }
            if (_frigates == null || _frigates.Length == 0)
            {
                _countLabel.Text = UiStrings.Get("frigate.no_frigates_found");
                return;
            }

            RefreshList();
        }
        catch { _countLabel.Text = UiStrings.Get("frigate.failed_load"); }
        }
        finally
        {
            _frigateList.EndUpdate();
            ResumeLayout(true);
        }
    }

    public void SaveData(JsonObject saveData) { }

    // -- Helpers --

    private JsonObject? SelectedFrigate()
    {
        int idx = _frigateList.SelectedIndex;
        if (idx < 0 || _frigates == null || idx >= _frigates.Length) return null;
        try { return _frigates.GetObject(idx); } catch { return null; }
    }

    private void RefreshList()
    {
        int sel = _frigateList.SelectedIndex;
        _frigateList.BeginUpdate();
        try
        {
        _frigateList.Items.Clear();
        if (_frigates == null) return;

        for (int i = 0; i < _frigates.Length; i++)
        {
            try
            {
                var f = _frigates.GetObject(i);
                string name = FrigateLogic.GetFrigateName(f, i);
                string type = FrigateLogic.GetFrigateType(f);
                string cls = FrigateLogic.ComputeClassFromTraits(f);
                _frigateList.Items.Add($"{name}  [{type}] ({cls})");
            }
            catch { _frigateList.Items.Add(UiStrings.Format("frigate.list_format", i + 1)); }
        }

        _countLabel.Text = UiStrings.Format("frigate.total_frigates", _frigates.Length);
        if (sel >= 0 && sel < _frigateList.Items.Count)
            _frigateList.SelectedIndex = sel;
        }
        finally
        {
            _frigateList.EndUpdate();
        }
    }

    private void OnFrigateSelected(object? sender, EventArgs e)
    {
        var frigate = SelectedFrigate();
        if (frigate == null)
        {
            _detailPanel.Visible = false;
            _statsPanel.Visible = false;
            return;
        }

        _loading = true;
        try
        {
            _detailPanel.Visible = true;
            _statsPanel.Visible = true;

            _nameField.Text = frigate.GetString("CustomName") ?? "";

            string type = FrigateLogic.GetFrigateType(frigate);
            int typeIdx = Array.IndexOf(FrigateTypes, type);
            _typeField.SelectedIndex = typeIdx >= 0 ? typeIdx : -1;

            // Class: always compute from traits (traits are the source of truth for in-game class)
            string computedClass = FrigateLogic.ComputeClassFromTraits(frigate);
            int classIdx = Array.IndexOf(FrigateGrades, computedClass);
            _classField.SelectedIndex = classIdx >= 0 ? classIdx : 0;

            string race = "";
            try { race = frigate.GetObject("Race")?.GetString("AlienRace") ?? ""; } catch { }
            int raceIdx = Array.IndexOf(FrigateRaces, race);
            _raceField.SelectedIndex = raceIdx >= 0 ? raceIdx : -1;

            // Seeds are stored as [bool, "0x..."] arrays
            _homeSeedField.Text = ReadSeed(frigate, "HomeSystemSeed");
            _modelSeedField.Text = ReadSeed(frigate, "ResourceSeed");

            // Traits
            var traits = frigate.GetArray("TraitIDs");
            for (int i = 0; i < 5; i++)
            {
                string tid = "";
                try { if (traits != null && i < traits.Length) tid = traits.GetString(i); } catch { }
                SelectTrait(_traitFields[i], tid);
            }

            // Damage
            int dmg = 0;
            try { dmg = frigate.GetInt("DamageTaken"); } catch { }
            _damageLabel.Text = dmg > 0 ? UiStrings.Format("frigate.damage_format", dmg) : UiStrings.Get("frigate.no_damage");

            // Stats – store raw values for preservation, clamp for display
            _rawStatValues.Clear();
            var stats = frigate.GetArray("Stats");
            for (int i = 0; i < 11; i++)
            {
                int val = 0;
                try { if (stats != null && i < stats.Length) val = stats.GetInt(i); } catch { }
                _rawStatValues[i] = val;
                _statFields[i].Value = Math.Min(999, Math.Max(0, val));
            }

            // Totals
            try { _expeditionsField.Value = frigate.GetInt("TotalNumberOfExpeditions"); } catch { _expeditionsField.Value = 0; }
            try { _successfulField.Value = frigate.GetInt("TotalNumberOfSuccessfulEvents"); } catch { _successfulField.Value = 0; }
            try { _failedField.Value = frigate.GetInt("TotalNumberOfFailedEvents"); } catch { _failedField.Value = 0; }
            try { _damagedField.Value = frigate.GetInt("NumberOfTimesDamaged"); } catch { _damagedField.Value = 0; }

            // Level-up progress
            int numExp = (int)_expeditionsField.Value;
            int levelUpIn = FrigateLogic.GetLevelUpIn(numExp);
            _levelUpInField.Text = levelUpIn >= 0 ? levelUpIn.ToString() : UiStrings.Get("frigate.level_max");
            int levelsLeft = FrigateLogic.GetLevelUpsRemaining(numExp);
            _levelUpsRemainingField.Text = levelsLeft.ToString();

            // Expedition state
            int frigateIdx = _frigateList.SelectedIndex;
            int state = FrigateLogic.GetFrigateState(frigate, frigateIdx, _expeditions);
            _stateField.Text = state >= 0 && state < FrigateLogic.FrigateStateKeys.Length ? UiStrings.Get(FrigateLogic.FrigateStateKeys[state]) : UiStrings.Get("common.unknown");

            // Mission type (if on expedition)
            if (state == 1 || state == 3) // OnExpedition or AwaitingDebrief
            {
                int expIdx = _expeditions != null ? FrigateLogic.FindExpeditionIndex(frigateIdx, _expeditions) : -1;
                _missionTypeField.Text = expIdx >= 0 && _expeditions != null ? FrigateLogic.GetExpeditionCategory(_expeditions, expIdx) : "";
                _finishExpeditionBtn.Enabled = true;

                // ExpeditionStartTime
                _expeditionStartTimeField.Enabled = true;
                if (expIdx >= 0 && _expeditions != null)
                {
                    try
                    {
                        var exp = _expeditions.GetObject(expIdx);
                        long startUnix = exp.GetLong("StartTime");
                        _expeditionStartTimeField.Value = DateTimeOffset.FromUnixTimeSeconds(startUnix).LocalDateTime;
                    }
                    catch { _expeditionStartTimeField.Value = DateTime.Now; }
                }
            }
            else
            {
                _missionTypeField.Text = "";
                _finishExpeditionBtn.Enabled = false;
                _expeditionStartTimeField.Enabled = false;
                try { _expeditionStartTimeField.Value = DateTime.Now; } catch { }
            }
        }
        catch { }
        finally { _loading = false; }
    }

    private void OnTraitChanged(int traitIdx)
    {
        if (_loading) return;
        var frigate = SelectedFrigate();
        if (frigate == null) return;

        try
        {
            var traits = frigate.GetArray("TraitIDs");
            if (traits == null) return;

            var selected = _traitFields[traitIdx].SelectedItem as FrigateTrait;
            // NMS uses "^" for unassigned trait slots (not empty string)
            string id = selected == null || selected == FrigateTraitDatabase.None ? "^" : selected.Id;

            if (traitIdx < traits.Length)
                traits.Set(traitIdx, id);

            // Recompute class from the updated traits
            string computedClass = FrigateLogic.ComputeClassFromTraits(frigate);
            int computedIdx = Array.IndexOf(FrigateGrades, computedClass);
            // Guard with _loading to prevent the class SelectedIndexChanged handler
            // from firing AdjustTraitsForTargetGrade (which would re-modify traits)
            _loading = true;
            _classField.SelectedIndex = computedIdx >= 0 ? computedIdx : 0;
            _loading = false;
            // Update inventory class to match the trait-computed class
            try { frigate.GetObject("InventoryClass")?.Set("InventoryClass", computedClass); } catch { }
            // Refresh list entry to show the updated class in the fleet list
            RefreshListEntry();
        }
        catch { }
    }

    private void OnRepair(object? sender, EventArgs e)
    {
        var frigate = SelectedFrigate();
        if (frigate == null) return;
        try
        {
            frigate.Set("DamageTaken", 0);
            frigate.Set("RepairsMade", 0);
            _damageLabel.Text = UiStrings.Get("frigate.no_damage");
        }
        catch { }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        if (_frigates == null || _frigateList.SelectedIndex < 0) return;

        int idx = _frigateList.SelectedIndex;

        // Prevent deleting a frigate that is currently on an expedition
        // (checks AllFrigateIndices)
        if (_expeditions != null && FrigateLogic.FindExpeditionIndex(idx, _expeditions) >= 0)
        {
            MessageBox.Show(this, UiStrings.Get("frigate.delete_on_mission"),
                UiStrings.Get("frigate.delete_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show(this, UiStrings.Get("frigate.delete_confirm"), UiStrings.Get("frigate.delete_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        // Remove from array and adjust all expedition frigate index references
        // (walks FleetExpeditions)
        _frigates.RemoveAt(idx);
        if (_expeditions != null)
            FrigateLogic.AdjustExpeditionIndicesAfterRemoval(idx, _expeditions);

        RefreshList();
        if (_frigateList.Items.Count > 0)
            _frigateList.SelectedIndex = Math.Min(idx, _frigateList.Items.Count - 1);
        else
        {
            _detailPanel.Visible = false;
            _statsPanel.Visible = false;
        }
    }

    private void OnCopy(object? sender, EventArgs e)
    {
        if (_frigates == null || _frigateList.SelectedIndex < 0) return;
        if (_frigates.Length >= MaxFrigates)
        {
            MessageBox.Show(this, UiStrings.Get("frigate.max_reached"), UiStrings.Get("frigate.copy_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var frigate = SelectedFrigate();
        if (frigate == null) return;

        var clone = frigate.DeepClone();
        _frigates.Add(clone);
        RefreshList();
        _frigateList.SelectedIndex = _frigateList.Items.Count - 1;
    }

    private void SaveCurrentField(string key, string value)
    {
        if (_loading) return;
        var frigate = SelectedFrigate();
        if (frigate == null) return;
        try
        {
            frigate.Set(key, value);
            RefreshListEntry();
        }
        catch { }
    }

    private void SaveIntField(string key, int value)
    {
        if (_loading) return;
        var frigate = SelectedFrigate();
        if (frigate == null) return;
        try { frigate.Set(key, value); } catch { }
    }

    private void SaveSeedField(string key, string value)
    {
        if (_loading) return;
        var frigate = SelectedFrigate();
        if (frigate == null) return;
        var normalized = SeedHelper.NormalizeSeed(value);
        if (normalized == null) return;
        try
        {
            var arr = frigate.GetArray(key);
            if (arr != null && arr.Length >= 2)
                arr.Set(1, normalized);
        }
        catch { }
    }

    private void RefreshListEntry()
    {
        int idx = _frigateList.SelectedIndex;
        if (idx < 0 || _frigates == null || idx >= _frigates.Length) return;
        try
        {
            var f = _frigates.GetObject(idx);
            string name = FrigateLogic.GetFrigateName(f, idx);
            string type = FrigateLogic.GetFrigateType(f);
            string cls = FrigateLogic.ComputeClassFromTraits(f);
            _frigateList.Items[idx] = $"{name}  [{type}] ({cls})";
        }
        catch { }
    }

    private static string ReadSeed(JsonObject frigate, string key)
    {
        try
        {
            var arr = frigate.GetArray(key);
            if (arr != null && arr.Length >= 2)
                return arr.Get(1)?.ToString() ?? "";
        }
        catch { }
        return "";
    }

    private static void SelectTrait(ComboBox cb, string traitId)
    {
        if (string.IsNullOrEmpty(traitId) || traitId == "^")
        {
            cb.SelectedIndex = 0; // None
            return;
        }

        for (int i = 0; i < cb.Items.Count; i++)
        {
            if (cb.Items[i] is FrigateTrait t && t.Id == traitId)
            {
                cb.SelectedIndex = i;
                return;
            }
        }
        cb.SelectedIndex = 0;
    }

    private static Label AddRow(TableLayoutPanel layout, string label, Control field, int row)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(field, 1, row);
        return lbl;
    }

    private Label AddSeedRow(TableLayoutPanel layout, string label, TextBox seedField, int row, Action? onSeedChanged = null)
    {
        var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 5, 10, 0) };
        layout.Controls.Add(lbl, 0, row);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        seedField.Dock = DockStyle.Fill;
        panel.Controls.Add(seedField, 0, 0);

        var genBtn = new Button { Text = "Gen", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(40, 23) };
        genBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            string seed = "0x" + BitConverter.ToString(bytes).Replace("-", "");
            var normalized = SeedHelper.NormalizeSeed(seed);
            if (normalized != null)
            {
                seedField.Text = normalized;
                onSeedChanged?.Invoke();
            }
        };
        panel.Controls.Add(genBtn, 1, 0);

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

    private void OnFastForward(object? sender, EventArgs e)
    {
        if (_loading) return;
        var frigate = SelectedFrigate();
        if (frigate == null) return;
        try
        {
            int numExp = frigate.GetInt("TotalNumberOfExpeditions");
            foreach (int threshold in FrigateLogic.LevelVictoriesRequired)
            {
                if (numExp < threshold)
                {
                    frigate.Set("TotalNumberOfExpeditions", threshold - 1);
                    _loading = true;
                    _expeditionsField.Value = threshold - 1;
                    _levelUpInField.Text = "1";
                    _levelUpsRemainingField.Text = FrigateLogic.GetLevelUpsRemaining(threshold - 1).ToString();
                    _loading = false;
                    break;
                }
            }
        }
        catch { }
    }

    private void OnFinishExpedition(object? sender, EventArgs e)
    {
        if (_loading || _expeditions == null) return;
        int frigateIdx = _frigateList.SelectedIndex;
        if (frigateIdx < 0) return;

        int expIdx = FrigateLogic.FindExpeditionIndex(frigateIdx, _expeditions);
        if (expIdx < 0) return;

        try
        {
            var exp = _expeditions.GetObject(expIdx);
            // Clear damaged/destroyed indices (remove in reverse to avoid O(n²))
            var damagedArr = exp.GetArray("DamagedFrigateIndices");
            if (damagedArr != null)
                for (int j = damagedArr.Length - 1; j >= 0; j--) damagedArr.RemoveAt(j);
            var destroyedArr = exp.GetArray("DestroyedFrigateIndices");
            if (destroyedArr != null)
                for (int j = destroyedArr.Length - 1; j >= 0; j--) destroyedArr.RemoveAt(j);

            // Copy all frigate indices to active
            var allIndices = exp.GetArray("AllFrigateIndices");
            var activeIndices = exp.GetArray("ActiveFrigateIndices");
            if (allIndices != null && activeIndices != null)
            {
                for (int j = activeIndices.Length - 1; j >= 0; j--) activeIndices.RemoveAt(j);
                for (int j = 0; j < allIndices.Length; j++)
                    activeIndices.Add(allIndices.GetInt(j));
            }

            // Set events to success
            var events = exp.GetArray("Events");
            if (events != null)
            {
                int totalEvents = events.Length;
                exp.Set("NextEventToTrigger", totalEvents);
                try { exp.Set("NumberOfSuccessfulEventsThisExpedition", totalEvents); } catch { }
                try { exp.Set("NumberOfFailedEventsThisExpedition", 0); } catch { }
                for (int j = 0; j < totalEvents; j++)
                {
                    try { events.GetObject(j).Set("Success", true); } catch { }
                }
            }

            try { exp.Set("PauseTime", 0); } catch { }

            // Repair all frigates in this expedition
            if (allIndices != null && _frigates != null)
            {
                for (int j = 0; j < allIndices.Length; j++)
                {
                    try
                    {
                        int fIdx = allIndices.GetInt(j);
                        if (fIdx >= 0 && fIdx < _frigates.Length)
                        {
                            var f = _frigates.GetObject(fIdx);
                            f.Set("DamageTaken", 0);
                            f.Set("RepairsMade", 0);
                        }
                    }
                    catch { }
                }
            }

            // Refresh UI
            OnFrigateSelected(null, EventArgs.Empty);
        }
        catch { }
    }

    private void OnExpeditionStartTimeChanged(object? sender, EventArgs e)
    {
        if (_loading || _expeditions == null) return;
        int frigateIdx = _frigateList.SelectedIndex;
        if (frigateIdx < 0) return;

        int expIdx = FrigateLogic.FindExpeditionIndex(frigateIdx, _expeditions);
        if (expIdx < 0) return;

        try
        {
            var exp = _expeditions.GetObject(expIdx);
            var dt = DateTime.SpecifyKind(_expeditionStartTimeField.Value, DateTimeKind.Local);
            long unix = new DateTimeOffset(dt).ToUnixTimeSeconds();
            exp.Set("StartTime", unix);
        }
        catch { }
    }

    private void OnExport(object? sender, EventArgs e)
    {
        var frigate = SelectedFrigate();
        if (frigate == null) return;

        var config = ExportConfig.Instance;
        var vars = new Dictionary<string, string>
        {
            ["frigate_name"] = _nameField.Text ?? "",
            ["type"] = FrigateLogic.GetFrigateType(frigate),
            ["class"] = FrigateLogic.ComputeClassFromTraits(frigate)
        };

        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.FrigateExt, "Frigate files"),
            DefaultExt = config.FrigateExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.FrigateTemplate, config.FrigateExt, vars)
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try { frigate.ExportToFile(dialog.FileName); }
            catch (Exception ex)
            {
                MessageBox.Show(this, UiStrings.Format("common.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        if (_frigates == null) return;
        if (_frigates.Length >= MaxFrigates)
        {
            MessageBox.Show(this, UiStrings.Get("frigate.max_reached"), UiStrings.Get("frigate.import_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildImportFilter(ExportConfig.Instance.FrigateExt, "Frigate files", ".flt")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var imported = JsonObject.ImportFromFile(dialog.FileName);

            // Unwrap NomNom wrapper if present (Data -> Frigate)
            imported = InventoryImportHelper.UnwrapNomNomFrigate(imported);

            _frigates.Add(imported);
            RefreshList();
            _frigateList.SelectedIndex = _frigateList.Items.Count - 1;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, UiStrings.Format("common.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("frigate.title");
        _countLabel.Text = UiStrings.Get("frigate.no_frigates");
        _deleteBtn.Text = UiStrings.Get("common.delete");
        _copyBtn.Text = UiStrings.Get("common.copy");
        _exportBtn.Text = UiStrings.Get("common.export");
        _importBtn.Text = UiStrings.Get("common.import");
        _damageLabel.Text = UiStrings.Get("frigate.no_damage");
        _repairBtn.Text = UiStrings.Get("frigate.repair");
        _fastForwardBtn.Text = UiStrings.Get("frigate.fast_forward");
        _finishExpeditionBtn.Text = UiStrings.Get("frigate.finish_expedition");

        // Section headers
        _frigateInfoHeader.Text = UiStrings.Get("frigate.info_header");
        _traitsHeader.Text = UiStrings.Get("frigate.traits_header");
        _statsHeader.Text = UiStrings.Get("frigate.stats_header");
        _totalsHeader.Text = UiStrings.Get("frigate.totals_header");
        _progressHeader.Text = UiStrings.Get("frigate.progress_header");

        // Detail labels
        _nameLabel.Text = UiStrings.Get("frigate.name");
        _typeLabel.Text = UiStrings.Get("frigate.type");
        _classLabel.Text = UiStrings.Get("frigate.class");
        _raceLabel.Text = UiStrings.Get("frigate.npc_race");
        _homeSeedLabel.Text = UiStrings.Get("frigate.home_seed");
        _modelSeedLabel.Text = UiStrings.Get("frigate.model_seed");

        // Trait labels
        for (int i = 0; i < _traitLabels.Length; i++)
            _traitLabels[i].Text = UiStrings.Get($"frigate.trait_{i + 1}");

        // Stat labels
        string[] statKeys = {
            "frigate.stat_combat", "frigate.stat_exploration", "frigate.stat_industry", "frigate.stat_trading",
            "frigate.stat_cost_per_warp", "frigate.stat_fuel_cost", "frigate.stat_duration",
            "frigate.stat_loot", "frigate.stat_repair", "frigate.stat_damage_reduction", "frigate.stat_stealth"
        };
        for (int i = 0; i < _statLabels.Length; i++)
            _statLabels[i].Text = UiStrings.Get(statKeys[i]);

        // Totals labels
        _expeditionsLabel.Text = UiStrings.Get("frigate.expeditions");
        _successfulLabel.Text = UiStrings.Get("frigate.successful");
        _failedLabel.Text = UiStrings.Get("frigate.failed");
        _timesDamagedLabel.Text = UiStrings.Get("frigate.times_damaged");

        // Progress / Mission labels
        _stateLabel.Text = UiStrings.Get("frigate.state");
        _levelUpInLabel.Text = UiStrings.Get("frigate.level_up_in");
        _levelsLeftLabel.Text = UiStrings.Get("frigate.levels_left");
        _missionTypeLabel.Text = UiStrings.Get("frigate.mission_type");
        _expStartLabel.Text = UiStrings.Get("frigate.exp_start");

        // Refresh type and race combos with localised display names
        RefreshFrigateTypeCombos();
    }

    private void RefreshFrigateTypeCombos()
    {
        int currentType = _typeField.SelectedIndex;
        int currentRace = _raceField.SelectedIndex;
        _typeField.Items.Clear();
        foreach (var t in FrigateTypes)
            _typeField.Items.Add(FrigateLogic.GetLocalisedFrigateTypeName(t));
        _raceField.Items.Clear();
        foreach (var r in FrigateRaces)
            _raceField.Items.Add(FrigateLogic.GetLocalisedFrigateRaceName(r));
        if (currentType >= 0 && currentType < _typeField.Items.Count)
            _typeField.SelectedIndex = currentType;
        if (currentRace >= 0 && currentRace < _raceField.Items.Count)
            _raceField.SelectedIndex = currentRace;
    }
}
