using NMSE.Core;
using NMSE.Core.Utilities;
using NMSE.Data;
using NMSE.UI.Util;
using NMSE.UI.Controls;

namespace NMSE.UI.Panels;

partial class FrigatePanel
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        this._mainLayout = new System.Windows.Forms.TableLayoutPanel();
        this._leftLayout = new System.Windows.Forms.TableLayoutPanel();
        this._titleLabel = new System.Windows.Forms.Label();
        this._frigateList = new System.Windows.Forms.ListBox();
        this._btnPanel = new System.Windows.Forms.FlowLayoutPanel();
        this._countLabel = new System.Windows.Forms.Label();
        this._deleteBtn = new System.Windows.Forms.Button();
        this._copyBtn = new System.Windows.Forms.Button();
        this._exportBtn = new System.Windows.Forms.Button();
        this._importBtn = new System.Windows.Forms.Button();
        this._detailPanel = new System.Windows.Forms.Panel();
        this._detailLayout = new System.Windows.Forms.TableLayoutPanel();
        this._nameField = new System.Windows.Forms.TextBox();
        this._typeField = new System.Windows.Forms.ComboBox();
        this._classField = new System.Windows.Forms.ComboBox();
        this._raceField = new System.Windows.Forms.ComboBox();
        this._homeSeedField = new System.Windows.Forms.TextBox();
        this._modelSeedField = new System.Windows.Forms.TextBox();
        this._traitField0 = new System.Windows.Forms.ComboBox();
        this._traitField1 = new System.Windows.Forms.ComboBox();
        this._traitField2 = new System.Windows.Forms.ComboBox();
        this._traitField3 = new System.Windows.Forms.ComboBox();
        this._traitField4 = new System.Windows.Forms.ComboBox();
        this._damageRow = new System.Windows.Forms.FlowLayoutPanel();
        this._damageLabel = new System.Windows.Forms.Label();
        this._repairBtn = new System.Windows.Forms.Button();
        this._statsPanel = new System.Windows.Forms.Panel();
        this._statsLayout = new System.Windows.Forms.TableLayoutPanel();
        this._statField0 = new InvariantNumericTextBox();
        this._statField1 = new InvariantNumericTextBox();
        this._statField2 = new InvariantNumericTextBox();
        this._statField3 = new InvariantNumericTextBox();
        this._statField4 = new InvariantNumericTextBox();
        this._statField5 = new InvariantNumericTextBox();
        this._statField6 = new InvariantNumericTextBox();
        this._statField7 = new InvariantNumericTextBox();
        this._statField8 = new InvariantNumericTextBox();
        this._statField9 = new InvariantNumericTextBox();
        this._statField10 = new InvariantNumericTextBox();
        this._expeditionsField = new InvariantNumericTextBox();
        this._successfulField = new InvariantNumericTextBox();
        this._failedField = new InvariantNumericTextBox();
        this._damagedField = new InvariantNumericTextBox();
        this._stateField = new System.Windows.Forms.TextBox();
        this._levelUpInField = new System.Windows.Forms.TextBox();
        this._levelUpsRemainingField = new System.Windows.Forms.TextBox();
        this._missionTypeField = new System.Windows.Forms.TextBox();
        this._fastForwardBtn = new System.Windows.Forms.Button();
        this._finishExpeditionBtn = new System.Windows.Forms.Button();
        this._expeditionStartTimeField = new System.Windows.Forms.DateTimePicker();
        this._mainLayout.SuspendLayout();
        this._leftLayout.SuspendLayout();
        this._btnPanel.SuspendLayout();
        this._detailPanel.SuspendLayout();
        this._detailLayout.SuspendLayout();
        this._damageRow.SuspendLayout();
        this._statsPanel.SuspendLayout();
        this._statsLayout.SuspendLayout();
        this.SuspendLayout();
        //
        // _mainLayout
        //
        this._mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this._mainLayout.ColumnCount = 3;
        this._mainLayout.RowCount = 1;
        this._mainLayout.Padding = new System.Windows.Forms.Padding(10);
        this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 220F));
        this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
        this._mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
        this._mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._mainLayout.Controls.Add(this._leftLayout, 0, 0);
        this._mainLayout.Controls.Add(this._detailPanel, 1, 0);
        this._mainLayout.Controls.Add(this._statsPanel, 2, 0);
        //
        // _leftLayout
        //
        this._leftLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this._leftLayout.ColumnCount = 1;
        this._leftLayout.RowCount = 3;
        this._leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this._leftLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this._leftLayout.Controls.Add(this._titleLabel, 0, 0);
        this._leftLayout.Controls.Add(this._frigateList, 0, 1);
        this._leftLayout.Controls.Add(this._btnPanel, 0, 2);
        //
        // _titleLabel
        //
        this._titleLabel.Text = "Fleet Frigates";
        FontManager.ApplyHeadingFont(_titleLabel, 14);
        this._titleLabel.AutoSize = true;
        this._titleLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
        //
        // _frigateList
        //
        this._frigateList.Dock = System.Windows.Forms.DockStyle.Fill;
        //
        // _btnPanel
        //
        this._btnPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this._btnPanel.AutoSize = true;
        this._btnPanel.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
        this._btnPanel.Controls.Add(this._deleteBtn);
        this._btnPanel.Controls.Add(this._copyBtn);
        this._btnPanel.Controls.Add(this._exportBtn);
        this._btnPanel.Controls.Add(this._importBtn);
        this._btnPanel.Controls.Add(this._countLabel);
        //
        // _countLabel
        //
        this._countLabel.Text = "No frigates loaded.";
        this._countLabel.AutoSize = true;
        this._countLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._countLabel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
        //
        // _deleteBtn
        //
        this._deleteBtn.Text = "Delete";
        this._deleteBtn.AutoSize = true;
        this._deleteBtn.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._deleteBtn.MinimumSize = new System.Drawing.Size(75, 0);
        //
        // _copyBtn
        //
        this._copyBtn.Text = "Copy";
        this._copyBtn.AutoSize = true;
        this._copyBtn.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._copyBtn.MinimumSize = new System.Drawing.Size(75, 0);
        //
        // _exportBtn
        //
        this._exportBtn.Text = "Export";
        this._exportBtn.AutoSize = true;
        this._exportBtn.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._exportBtn.MinimumSize = new System.Drawing.Size(75, 0);
        //
        // _importBtn
        //
        this._importBtn.Text = "Import";
        this._importBtn.AutoSize = true;
        this._importBtn.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this._importBtn.MinimumSize = new System.Drawing.Size(75, 0);
        //
        // _detailPanel
        //
        this._detailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this._detailPanel.AutoScroll = true;
        this._detailPanel.Visible = false;
        this._detailPanel.Controls.Add(this._detailLayout);
        //
        // _detailLayout
        //
        this._detailLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._detailLayout.ColumnCount = 2;
        this._detailLayout.AutoSize = true;
        this._detailLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        this._detailLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        //
        // _nameField
        //
        this._nameField.Dock = System.Windows.Forms.DockStyle.Fill;
        //
        // _typeField
        //
        this._typeField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._typeField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _classField
        //
        this._classField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._classField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _raceField
        //
        this._raceField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._raceField.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _homeSeedField
        //
        this._homeSeedField.Dock = System.Windows.Forms.DockStyle.Fill;
        //
        // _modelSeedField
        //
        this._modelSeedField.Dock = System.Windows.Forms.DockStyle.Fill;
        //
        // _traitField0
        //
        this._traitField0.Dock = System.Windows.Forms.DockStyle.Fill;
        this._traitField0.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _traitField1
        //
        this._traitField1.Dock = System.Windows.Forms.DockStyle.Fill;
        this._traitField1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _traitField2
        //
        this._traitField2.Dock = System.Windows.Forms.DockStyle.Fill;
        this._traitField2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _traitField3
        //
        this._traitField3.Dock = System.Windows.Forms.DockStyle.Fill;
        this._traitField3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _traitField4
        //
        this._traitField4.Dock = System.Windows.Forms.DockStyle.Fill;
        this._traitField4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //
        // _damageRow
        //
        this._damageRow.Dock = System.Windows.Forms.DockStyle.Fill;
        this._damageRow.AutoSize = true;
        this._damageRow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
        this._damageRow.Controls.Add(this._damageLabel);
        this._damageRow.Controls.Add(this._repairBtn);
        //
        // _damageLabel
        //
        this._damageLabel.Text = "No damage";
        this._damageLabel.AutoSize = true;
        this._damageLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this._damageLabel.Padding = new System.Windows.Forms.Padding(0, 5, 5, 0);
        //
        // _repairBtn
        //
        this._repairBtn.Text = "Repair";
        this._repairBtn.Width = 60;
        //
        // _statsPanel
        //
        this._statsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statsPanel.AutoScroll = true;
        this._statsPanel.Visible = false;
        this._statsPanel.Controls.Add(this._statsLayout);
        //
        // _statsLayout
        //
        this._statsLayout.Dock = System.Windows.Forms.DockStyle.Top;
        this._statsLayout.ColumnCount = 2;
        this._statsLayout.AutoSize = true;
        this._statsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
        this._statsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        //
        // _statField0
        //
        this._statField0.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField0.Minimum = 0;
        this._statField0.Maximum = 999;
        //
        // _statField1
        //
        this._statField1.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField1.Minimum = 0;
        this._statField1.Maximum = 999;
        //
        // _statField2
        //
        this._statField2.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField2.Minimum = 0;
        this._statField2.Maximum = 999;
        //
        // _statField3
        //
        this._statField3.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField3.Minimum = 0;
        this._statField3.Maximum = 999;
        //
        // _statField4
        //
        this._statField4.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField4.Minimum = 0;
        this._statField4.Maximum = 999;
        //
        // _statField5
        //
        this._statField5.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField5.Minimum = 0;
        this._statField5.Maximum = 999;
        //
        // _statField6
        //
        this._statField6.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField6.Minimum = 0;
        this._statField6.Maximum = 999;
        //
        // _statField7
        //
        this._statField7.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField7.Minimum = 0;
        this._statField7.Maximum = 999;
        //
        // _statField8
        //
        this._statField8.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField8.Minimum = 0;
        this._statField8.Maximum = 999;
        //
        // _statField9
        //
        this._statField9.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField9.Minimum = 0;
        this._statField9.Maximum = 999;
        //
        // _statField10
        //
        this._statField10.Dock = System.Windows.Forms.DockStyle.Fill;
        this._statField10.Minimum = 0;
        this._statField10.Maximum = 999;
        //
        // _expeditionsField
        //
        this._expeditionsField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._expeditionsField.Minimum = 0;
        this._expeditionsField.Maximum = 999999;
        //
        // _successfulField
        //
        this._successfulField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._successfulField.Minimum = 0;
        this._successfulField.Maximum = 999999;
        //
        // _failedField
        //
        this._failedField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._failedField.Minimum = 0;
        this._failedField.Maximum = 999999;
        //
        // _damagedField
        //
        this._damagedField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._damagedField.Minimum = 0;
        this._damagedField.Maximum = 999999;
        //
        // _stateField
        //
        this._stateField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._stateField.ReadOnly = true;
        //
        // _levelUpInField
        //
        this._levelUpInField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._levelUpInField.ReadOnly = true;
        //
        // _levelUpsRemainingField
        //
        this._levelUpsRemainingField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._levelUpsRemainingField.ReadOnly = true;
        //
        // _missionTypeField
        //
        this._missionTypeField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._missionTypeField.ReadOnly = true;
        //
        // _fastForwardBtn
        //
        this._fastForwardBtn.Text = "Fast Forward to Next Level Up";
        this._fastForwardBtn.AutoSize = true;
        //
        // _finishExpeditionBtn
        //
        this._finishExpeditionBtn.Text = "Finish Expedition Successfully";
        this._finishExpeditionBtn.AutoSize = true;
        //
        // _expeditionStartTimeField
        //
        this._expeditionStartTimeField.Dock = System.Windows.Forms.DockStyle.Fill;
        this._expeditionStartTimeField.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        this._expeditionStartTimeField.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        this._expeditionStartTimeField.Enabled = false;
        //
        // FrigatePanel
        //
        this.DoubleBuffered = true;
        this.Controls.Add(this._mainLayout);
        this._mainLayout.ResumeLayout(false);
        this._leftLayout.ResumeLayout(false);
        this._btnPanel.ResumeLayout(false);
        this._detailPanel.ResumeLayout(false);
        this._detailLayout.ResumeLayout(false);
        this._damageRow.ResumeLayout(false);
        this._statsPanel.ResumeLayout(false);
        this._statsLayout.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private void SetupLayout()
    {
        // Pack individual fields into arrays
        _traitFields = new ComboBox[] { _traitField0, _traitField1, _traitField2, _traitField3, _traitField4 };
        _statFields = new InvariantNumericTextBox[] { _statField0, _statField1, _statField2, _statField3, _statField4,
            _statField5, _statField6, _statField7, _statField8, _statField9, _statField10 };

        // Populate combo items
        foreach (var t in FrigateTypes)
            _typeField.Items.Add(FrigateLogic.GetLocalisedFrigateTypeName(t));
        _classField.Items.AddRange(FrigateGrades);
        foreach (var r in FrigateRaces)
            _raceField.Items.Add(FrigateLogic.GetLocalisedFrigateRaceName(r));
        foreach (var cb in _traitFields)
        {
            cb.Items.Add(FrigateTraitDatabase.None);
            foreach (var t in FrigateTraitDatabase.Traits)
                cb.Items.Add(t);
        }

        // Wire event handlers
        _frigateList.SelectedIndexChanged += OnFrigateSelected;
        _deleteBtn.Click += OnDelete;
        _copyBtn.Click += OnCopy;
        _exportBtn.Click += OnExport;
        _importBtn.Click += OnImport;
        _repairBtn.Click += OnRepair;
        _fastForwardBtn.Click += OnFastForward;
        _finishExpeditionBtn.Click += OnFinishExpedition;
        _expeditionStartTimeField.ValueChanged += OnExpeditionStartTimeChanged;

        _nameField.Leave += (s, e) => SaveCurrentField("CustomName", _nameField.Text);
        _typeField.SelectedIndexChanged += (s, e) =>
        {
            if (_loading) return;
            var frigate = SelectedFrigate();
            if (frigate == null) return;
            try
            {
                string oldType = FrigateLogic.GetFrigateType(frigate);
                string newType = _typeField.SelectedIndex >= 0 && _typeField.SelectedIndex < FrigateTypes.Length ? FrigateTypes[_typeField.SelectedIndex] : "";
                frigate.GetObject("FrigateClass")?.Set("FrigateClass", newType);
                FrigateLogic.AutoAdjustTraitsForTypeChange(frigate, oldType, newType);
                // Refresh trait dropdowns and class display
                _loading = true;
                var traits = frigate.GetArray("TraitIDs");
                for (int ti = 0; ti < 5; ti++)
                {
                    string tid = "";
                    try { if (traits != null && ti < traits.Length) tid = traits.GetString(ti); } catch { }
                    SelectTrait(_traitFields[ti], tid);
                }
                string computedClass = FrigateLogic.ComputeClassFromTraits(frigate);
                int computedIdx = Array.IndexOf(FrigateGrades, computedClass);
                _classField.SelectedIndex = computedIdx >= 0 ? computedIdx : 0;
                try { frigate.GetObject("InventoryClass")?.Set("InventoryClass", computedClass); } catch { }
                _loading = false;
                RefreshListEntry();
            }
            catch { }
        };
        _classField.SelectedIndexChanged += (s, e) =>
        {
            if (_loading) return;
            var frigate = SelectedFrigate();
            if (frigate == null) return;
            try
            {
                string newClass = _classField.SelectedItem?.ToString() ?? "C";
                // Adjust traits to achieve the target class (the game derives class from traits)
                FrigateLogic.AdjustTraitsForTargetGrade(frigate, newClass);
                // Sync InventoryClass with the new computed class
                string computedClass = FrigateLogic.ComputeClassFromTraits(frigate);
                try { frigate.GetObject("InventoryClass")?.Set("InventoryClass", computedClass); } catch { }
                // Refresh trait dropdowns to reflect the changed traits
                _loading = true;
                var traits = frigate.GetArray("TraitIDs");
                for (int ti = 0; ti < 5; ti++)
                {
                    string tid = "";
                    try { if (traits != null && ti < traits.Length) tid = traits.GetString(ti); } catch { }
                    SelectTrait(_traitFields[ti], tid);
                }
                // Re-sync class combobox in case adjustment couldn't fully reach the target
                int computedIdx = Array.IndexOf(FrigateGrades, computedClass);
                _classField.SelectedIndex = computedIdx >= 0 ? computedIdx : 0;
                _loading = false;
                RefreshListEntry();
            }
            catch { }
        };
        _raceField.SelectedIndexChanged += (s, e) =>
        {
            if (_loading) return;
            var frigate = SelectedFrigate();
            if (frigate == null) return;
            string internalRace = _raceField.SelectedIndex >= 0 && _raceField.SelectedIndex < FrigateRaces.Length ? FrigateRaces[_raceField.SelectedIndex] : "";
            try { frigate.GetObject("Race")?.Set("AlienRace", internalRace); } catch { }
        };
        _homeSeedField.Leave += (s, e) => SaveSeedField("HomeSystemSeed", _homeSeedField.Text);
        _modelSeedField.Leave += (s, e) => SaveSeedField("ResourceSeed", _modelSeedField.Text);

        for (int i = 0; i < _traitFields.Length; i++)
        {
            int traitIdx = i;
            _traitFields[i].SelectedIndexChanged += (s, e) => OnTraitChanged(traitIdx);
        }

        for (int i = 0; i < _statFields.Length; i++)
        {
            int statIdx = i;
            var nud = _statFields[i];
            nud.NumericValueChanged += (s, e) =>
            {
                if (_loading) return;
                var frigate = SelectedFrigate();
                if (frigate == null) return;

                // Skip write if the user hasn't changed the value from its clamped display
                // (preserves raw JSON values outside the NUD range)
                if (_rawStatValues.TryGetValue(statIdx, out int raw))
                {
                    int clamped = Math.Min(999, Math.Max(0, raw));
                    if ((int)(nud.NumericValue ?? 0) == clamped)
                        return;
                }

                try
                {
                    var stats = frigate.GetArray("Stats");
                    if (stats != null && statIdx < stats.Length)
                        RawNumberGuard.SetInt(stats, statIdx, (int)(nud.NumericValue ?? 0));
                }
                catch { }
            };
        }

        _expeditionsField.NumericValueChanged += (s, e) => SaveIntField("TotalNumberOfExpeditions", (int)(_expeditionsField.NumericValue ?? 0));
        _successfulField.NumericValueChanged += (s, e) => SaveIntField("TotalNumberOfSuccessfulEvents", (int)(_successfulField.NumericValue ?? 0));
        _failedField.NumericValueChanged += (s, e) => SaveIntField("TotalNumberOfFailedEvents", (int)(_failedField.NumericValue ?? 0));
        _damagedField.NumericValueChanged += (s, e) => SaveIntField("NumberOfTimesDamaged", (int)(_damagedField.NumericValue ?? 0));

        // Arrange detail layout rows
        int row = 0;
        int totalRows = 1 + 6 + 1 + 5 + 1;
        _detailLayout.RowCount = totalRows;
        for (int i = 0; i < totalRows; i++)
            _detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _frigateInfoHeader = AddSectionHeader(_detailLayout, "Frigate Info", row++);
        _nameLabel = AddRow(_detailLayout, "Name:", _nameField, row++);
        _typeLabel = AddRow(_detailLayout, "Type:", _typeField, row++);
        _classLabel = AddRow(_detailLayout, "Class:", _classField, row++);
        _raceLabel = AddRow(_detailLayout, "NPC Race:", _raceField, row++);
        _homeSeedLabel = AddSeedRow(_detailLayout, "Home Seed:", _homeSeedField, row++, () => SaveSeedField("HomeSystemSeed", _homeSeedField.Text));
        _modelSeedLabel = AddSeedRow(_detailLayout, "Model Seed:", _modelSeedField, row++, () => SaveSeedField("ResourceSeed", _modelSeedField.Text));
        _traitsHeader = AddSectionHeader(_detailLayout, "Traits", row++);
        _traitLabels = new Label[5];
        for (int i = 0; i < 5; i++)
            _traitLabels[i] = AddRow(_detailLayout, "Trait " + (i + 1) + ":", _traitFields[i], row++);
        _detailLayout.Controls.Add(_damageRow, 0, row);
        _detailLayout.SetColumnSpan(_damageRow, 2);

        // Arrange stats layout rows
        int sr = 0;
        int statTotalRows = 1 + 11 + 1 + 4 + 1 + 7;
        _statsLayout.RowCount = statTotalRows;
        for (int i = 0; i < statTotalRows; i++)
            _statsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _statsHeader = AddSectionHeader(_statsLayout, "Stats", sr++);
        _statLabels = new Label[11];
        for (int i = 0; i < 11; i++)
            _statLabels[i] = AddRow(_statsLayout, StatNames[i] + ":", _statFields[i], sr++);
        _totalsHeader = AddSectionHeader(_statsLayout, "Totals", sr++);
        _expeditionsLabel = AddRow(_statsLayout, "Expeditions:", _expeditionsField, sr++);
        _successfulLabel = AddRow(_statsLayout, "Successful:", _successfulField, sr++);
        _failedLabel = AddRow(_statsLayout, "Failed:", _failedField, sr++);
        _timesDamagedLabel = AddRow(_statsLayout, "Damaged:", _damagedField, sr++);
        _progressHeader = AddSectionHeader(_statsLayout, "Progress / Mission", sr++);
        _stateLabel = AddRow(_statsLayout, "State:", _stateField, sr++);
        _levelUpInLabel = AddRow(_statsLayout, "Level Up In:", _levelUpInField, sr++);
        _levelsLeftLabel = AddRow(_statsLayout, "Levels Left:", _levelUpsRemainingField, sr++);
        _statsLayout.Controls.Add(_fastForwardBtn, 0, sr);
        _statsLayout.SetColumnSpan(_fastForwardBtn, 2);
        sr++;
        _missionTypeLabel = AddRow(_statsLayout, "Mission Type:", _missionTypeField, sr++);
        _expStartLabel = AddRow(_statsLayout, "Exp. Start:", _expeditionStartTimeField, sr++);
        _statsLayout.Controls.Add(_finishExpeditionBtn, 0, sr);
        _statsLayout.SetColumnSpan(_finishExpeditionBtn, 2);
    }

    private System.Windows.Forms.TableLayoutPanel _mainLayout;
    private System.Windows.Forms.TableLayoutPanel _leftLayout;
    private System.Windows.Forms.Label _titleLabel;
    private System.Windows.Forms.ListBox _frigateList;
    private System.Windows.Forms.FlowLayoutPanel _btnPanel;
    private System.Windows.Forms.Label _countLabel;
    private System.Windows.Forms.Button _deleteBtn;
    private System.Windows.Forms.Button _copyBtn;
    private System.Windows.Forms.Button _exportBtn;
    private System.Windows.Forms.Button _importBtn;
    private System.Windows.Forms.Panel _detailPanel;
    private System.Windows.Forms.TableLayoutPanel _detailLayout;
    private System.Windows.Forms.TextBox _nameField;
    private System.Windows.Forms.ComboBox _typeField;
    private System.Windows.Forms.ComboBox _classField;
    private System.Windows.Forms.ComboBox _raceField;
    private System.Windows.Forms.TextBox _homeSeedField;
    private System.Windows.Forms.TextBox _modelSeedField;
    private System.Windows.Forms.ComboBox _traitField0;
    private System.Windows.Forms.ComboBox _traitField1;
    private System.Windows.Forms.ComboBox _traitField2;
    private System.Windows.Forms.ComboBox _traitField3;
    private System.Windows.Forms.ComboBox _traitField4;
    private System.Windows.Forms.ComboBox[] _traitFields;
    private System.Windows.Forms.FlowLayoutPanel _damageRow;
    private System.Windows.Forms.Label _damageLabel;
    private System.Windows.Forms.Button _repairBtn;
    private System.Windows.Forms.Panel _statsPanel;
    private System.Windows.Forms.TableLayoutPanel _statsLayout;
    private InvariantNumericTextBox _statField0;
    private InvariantNumericTextBox _statField1;
    private InvariantNumericTextBox _statField2;
    private InvariantNumericTextBox _statField3;
    private InvariantNumericTextBox _statField4;
    private InvariantNumericTextBox _statField5;
    private InvariantNumericTextBox _statField6;
    private InvariantNumericTextBox _statField7;
    private InvariantNumericTextBox _statField8;
    private InvariantNumericTextBox _statField9;
    private InvariantNumericTextBox _statField10;
    private InvariantNumericTextBox[] _statFields;
    private InvariantNumericTextBox _expeditionsField;
    private InvariantNumericTextBox _successfulField;
    private InvariantNumericTextBox _failedField;
    private InvariantNumericTextBox _damagedField;
    private System.Windows.Forms.TextBox _stateField;
    private System.Windows.Forms.TextBox _levelUpInField;
    private System.Windows.Forms.TextBox _levelUpsRemainingField;
    private System.Windows.Forms.TextBox _missionTypeField;
    private System.Windows.Forms.Button _fastForwardBtn;
    private System.Windows.Forms.Button _finishExpeditionBtn;
    private System.Windows.Forms.DateTimePicker _expeditionStartTimeField;

    // Label fields for UI localisation (created by AddRow / AddSeedRow / AddSectionHeader)
    private System.Windows.Forms.Label _frigateInfoHeader = null!;
    private System.Windows.Forms.Label _nameLabel = null!;
    private System.Windows.Forms.Label _typeLabel = null!;
    private System.Windows.Forms.Label _classLabel = null!;
    private System.Windows.Forms.Label _raceLabel = null!;
    private System.Windows.Forms.Label _homeSeedLabel = null!;
    private System.Windows.Forms.Label _modelSeedLabel = null!;
    private System.Windows.Forms.Label _traitsHeader = null!;
    private System.Windows.Forms.Label[] _traitLabels = null!;
    private System.Windows.Forms.Label _statsHeader = null!;
    private System.Windows.Forms.Label[] _statLabels = null!;
    private System.Windows.Forms.Label _totalsHeader = null!;
    private System.Windows.Forms.Label _expeditionsLabel = null!;
    private System.Windows.Forms.Label _successfulLabel = null!;
    private System.Windows.Forms.Label _failedLabel = null!;
    private System.Windows.Forms.Label _timesDamagedLabel = null!;
    private System.Windows.Forms.Label _progressHeader = null!;
    private System.Windows.Forms.Label _stateLabel = null!;
    private System.Windows.Forms.Label _levelUpInLabel = null!;
    private System.Windows.Forms.Label _levelsLeftLabel = null!;
    private System.Windows.Forms.Label _missionTypeLabel = null!;
    private System.Windows.Forms.Label _expStartLabel = null!;
}
