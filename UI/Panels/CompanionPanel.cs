using NMSE.Data;
using NMSE.Models;
using NMSE.Core;
using System.Globalization;

namespace NMSE.UI.Panels;

public partial class CompanionPanel : UserControl
{
    private readonly Random _rng = new();

    private JsonObject? _playerState;
    private readonly List<(JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty)> _entries = new();
    private bool _loading;

    /// <summary>Cached per-slot allowed move IDs, gathered from all movesets. Index 0-4.</summary>
    private readonly List<PetBattleMoveEntry>[] _allowedMovesPerSlot = new List<PetBattleMoveEntry>[5];

    private bool _moveSlotDataInitialised;

    /// <summary>Raw (unclamped) battle int values read from JSON at load time, keyed by field name.</summary>
    private readonly Dictionary<string, int> _rawBattleIntValues = new();

    /// <summary>Raw (unclamped) battle decimal values read from JSON at load time, keyed by field name.</summary>
    private readonly Dictionary<string, decimal> _rawBattleDecimalValues = new();

    /// <summary>Raw (unclamped) battle double values read from JSON at load time, keyed by field name.</summary>
    private readonly Dictionary<string, double> _rawBattleDoubleValues = new();

    public CompanionPanel()
    {
        InitializeComponent();
        SetupLayout();
        // InitialiseMoveSlotData is deferred until first LoadBattleData
        // because PetBattleMovesetDatabase/PetBattleMoveDatabase are loaded
        // AFTER panel construction in MainForm.
    }

    /// <summary>Builds cached per-slot allowed move lists from all movesets.</summary>
    private void InitialiseMoveSlotData()
    {
        for (int i = 0; i < 5; i++)
        {
            int slotNumber = i + 1; // Movesets use 1-based slot numbers
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allowed = new List<PetBattleMoveEntry>();

            foreach (var moveset in PetBattleMovesetDatabase.Movesets)
            {
                var slot = moveset.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
                if (slot == null) continue;
                foreach (var opt in slot.Options)
                {
                    if (seen.Add(opt.Template) && PetBattleMoveDatabase.ById.TryGetValue(opt.Template, out var move))
                        allowed.Add(move);
                }
            }

            _allowedMovesPerSlot[i] = allowed;
        }
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
            Padding = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        seedField.Dock = DockStyle.Fill;
        panel.Controls.Add(seedField, 0, 0);

        var genBtn = new Button { Text = UiStrings.Get("companion.gen"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(40, 23) };
        genBtn.Click += (s, e) =>
        {
            byte[] bytes = new byte[8];
            _rng.NextBytes(bytes);
            seedField.Text = "0x" + BitConverter.ToString(bytes).Replace("-", "");
            onSeedChanged?.Invoke();
        };
        _seedGenButtons.Add(genBtn);
        panel.Controls.Add(genBtn, 1, 0);

        layout.Controls.Add(panel, 1, row);
        return lbl;
    }

    public void LoadData(JsonObject saveData)
    {
        _loading = true;
        SuspendLayout();
        _companionList.BeginUpdate();
        try
        {
            _companionList.Items.Clear();
            _entries.Clear();
            _detailPanel.Visible = false;
            _playerState = null;

            try
            {
                var playerState = saveData.GetObject("PlayerStateData");
                if (playerState == null) return;
                _playerState = playerState;

                LoadAllSlots(playerState.GetArray("Pets"), "Pet");
                LoadAllSlots(playerState.GetArray("Eggs"), "Egg");

                _countLabel.Text = UiStrings.Format("companion.total_slots", _entries.Count);

                if (_entries.Count > 0 && _companionList.Items.Count > 0)
                    _companionList.SelectedIndex = 0;
            }
            catch { }
        }
        finally
        {
            _companionList.EndUpdate();
            ResumeLayout(true);
            _loading = false;
        }
    }

    /// <summary>
    /// Loads ALL slots from the array (including empty ones).
    /// </summary>
    private void LoadAllSlots(JsonArray? array, string prefix)
    {
        if (array == null) return;
        for (int i = 0; i < array.Length; i++)
        {
            try
            {
                var comp = array.GetObject(i);

                bool occupied = IsSlotOccupied(comp);

                bool locked = false;
                if (prefix == "Pet" && _playerState != null)
                {
                    var unlockedSlots = _playerState.GetArray("UnlockedPetSlots");
                    locked = unlockedSlots == null || i >= unlockedSlots.Length || !unlockedSlots.GetBool(i);
                }

                string label = GetSlotLabel(comp, prefix, i, occupied, locked);
                _entries.Add((comp, label, prefix, i, !occupied));
                _companionList.Items.Add(label);
            }
            catch
            {
                string errLabel = UiStrings.Format("companion.error_format", prefix, i);
                _entries.Add((new JsonObject(), errLabel, prefix, i, true));
                _companionList.Items.Add(errLabel);
            }
        }
    }

    private static bool IsSlotOccupied(JsonObject comp)
    {
        try
        {
            var seedArray = comp.GetArray("CreatureSeed");
            if (seedArray != null && seedArray.Length >= 2)
                return seedArray.GetBool(0);
        }
        catch { }
        return false;
    }

    private static string GetSlotLabel(JsonObject comp, string prefix, int index, bool occupied, bool locked)
    {
        if (!occupied)
        {
            return locked ? $"{prefix} {index} (Locked)" : $"{prefix} {index} (Empty)";
        }

        string customName = "";
        try { customName = comp.GetString("CustomName") ?? ""; } catch { }
        if (string.IsNullOrEmpty(customName) || customName == "^")
            return $"{prefix} {index}";
        return $"{prefix} {index} - {customName}";
    }

    /// <summary>
    /// Returns the companion JsonObject for the currently selected slot.
    /// Returns the object even for empty/blank slots so the user can populate them from scratch.
    /// </summary>
    private JsonObject? SelectedCompanion
    {
        get
        {
            int idx = _companionList.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count) return null;
            return _entries[idx].Companion;
        }
    }

    /// <summary>
    /// Returns the raw entry at the current list index (even for empty slots).
    /// </summary>
    private (JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty) SelectedEntry
    {
        get
        {
            int idx = _companionList.SelectedIndex;
            if (idx < 0 || idx >= _entries.Count)
                return (new JsonObject(), "", "", -1, true);
            return _entries[idx];
        }
    }

    private void OnCompanionSelected(object? sender, EventArgs e)
    {
        int idx = _companionList.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count)
        {
            _detailPanel.Visible = false;
            return;
        }

        var entry = _entries[idx];
        _loading = true;
        try
        {
            _detailPanel.Visible = true;

            _deleteBtn.Enabled = !entry.IsEmpty;

            var comp = entry.Companion;

            // Type
            string creatureId = comp.GetString("CreatureID") ?? "";
            int typeIdx = -1;
            for (int i = 0; i < _typeField.Items.Count; i++)
            {
                var ce = _typeField.Items[i] as CompanionEntry;
                if (ce != null && string.Equals(ce.Id, creatureId, StringComparison.OrdinalIgnoreCase))
                { typeIdx = i; break; }
            }
            _typeField.SelectedIndex = typeIdx;

            // Name
            _nameField.Text = comp.GetString("CustomName") ?? "";

            // Creature Seed
            try
            {
                var seedArr = comp.GetArray("CreatureSeed");
                _creatureSeedField.Text = seedArr != null && seedArr.Length >= 2 ? seedArr.GetString(1) ?? "" : "";
            }
            catch { _creatureSeedField.Text = ""; }

            // Secondary Seed
            try
            {
                var secArr = comp.GetArray("CreatureSecondarySeed");
                _secondarySeedField.Text = secArr != null && secArr.Length >= 2 ? secArr.GetString(1) ?? "" : "";
            }
            catch { _secondarySeedField.Text = ""; }

            // Species Seed
            _speciesSeedField.Text = comp.GetString("SpeciesSeed") ?? "";

            // Genus Seed
            _genusSeedField.Text = comp.GetString("GenusSeed") ?? "";

            // Predator
            try { _predatorField.Checked = comp.GetBool("Predator"); } catch { _predatorField.Checked = false; }

            // Biome
            try
            {
                var biomeObj = comp.GetObject("Biome");
                string biome = biomeObj?.GetString("Biome") ?? "";
                _biomeField.SelectedItem = biome;
            }
            catch { _biomeField.SelectedIndex = -1; }

            // CreatureType
            try
            {
                var ctObj = comp.GetObject("CreatureType");
                string ct = ctObj?.GetString("CreatureType") ?? "";
                _creatureTypeField.SelectedItem = ct;
            }
            catch { _creatureTypeField.SelectedIndex = -1; }

            // Scale
            try { _scaleField.Text = comp.GetDouble("Scale").ToString(System.Globalization.CultureInfo.InvariantCulture); } catch { _scaleField.Text = ""; }

            // Trust
            try { _trustField.Text = comp.GetDouble("Trust").ToString(System.Globalization.CultureInfo.InvariantCulture); } catch { _trustField.Text = ""; }

            // Bone Scale Seed
            try
            {
                var bsArr = comp.GetArray("BoneScaleSeed");
                _boneScaleSeedField.Text = bsArr != null && bsArr.Length >= 2 ? bsArr.GetString(1) ?? "" : "";
            }
            catch { _boneScaleSeedField.Text = ""; }

            // Colour Base Seed
            try
            {
                var cbArr = comp.GetArray("ColourBaseSeed");
                _colourBaseSeedField.Text = cbArr != null && cbArr.Length >= 2 ? cbArr.GetString(1) ?? "" : "";
            }
            catch { _colourBaseSeedField.Text = ""; }

            // Has Fur
            try { _hasFurField.Checked = comp.GetBool("HasFur"); } catch { _hasFurField.Checked = false; }

            // Traits
            try
            {
                var traits = comp.GetArray("Traits");
                _helpfulnessField.Text = traits != null && traits.Length > 0 ? traits.GetDouble(0).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
                _aggressionField.Text = traits != null && traits.Length > 1 ? traits.GetDouble(1).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
                _independenceField.Text = traits != null && traits.Length > 2 ? traits.GetDouble(2).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            }
            catch
            {
                _helpfulnessField.Text = "0";
                _aggressionField.Text = "0";
                _independenceField.Text = "0";
            }

            // Moods
            try
            {
                var moods = comp.GetArray("Moods");
                _hungryField.Text = moods != null && moods.Length > 0 ? moods.GetDouble(0).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
                _lonelyField.Text = moods != null && moods.Length > 1 ? moods.GetDouble(1).ToString(System.Globalization.CultureInfo.InvariantCulture) : "0";
            }
            catch
            {
                _hungryField.Text = "0";
                _lonelyField.Text = "0";
            }

            // BirthTime
            try
            {
                long birthUnix = comp.GetLong("BirthTime");
                _birthTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(birthUnix).ToLocalTime().DateTime;
            }
            catch { _birthTimePicker.Value = DateTime.Now; }

            // LastEggTime
            try
            {
                long eggUnix = comp.GetLong("LastEggTime");
                _lastEggTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(eggUnix).ToLocalTime().DateTime;
            }
            catch { _lastEggTimePicker.Value = DateTime.Now; }

            // CustomSpeciesName
            try
            {
                string csn = comp.GetString("CustomSpeciesName") ?? "";
                _customSpeciesNameField.Text = csn == "^" ? "" : csn.TrimStart('^');
            }
            catch { _customSpeciesNameField.Text = ""; }

            // EggModified
            try { _eggModifiedField.Checked = comp.GetBool("EggModified"); } catch { _eggModifiedField.Checked = false; }

            // HasBeenSummoned
            try { _hasBeenSummonedField.Checked = comp.GetBool("HasBeenSummoned"); } catch { _hasBeenSummonedField.Checked = false; }

            // AllowUnmodifiedReroll
            try { _allowUnmodifiedRerollField.Checked = comp.GetBool("AllowUnmodifiedReroll"); } catch { _allowUnmodifiedRerollField.Checked = false; }

            // UA
            try { _uaField.Text = comp.GetLong("UA").ToString(System.Globalization.CultureInfo.InvariantCulture); } catch { _uaField.Text = "0"; }

            // LastTrustIncreaseTime
            try
            {
                long trustInc = comp.GetLong("LastTrustIncreaseTime");
                _lastTrustIncreaseTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(trustInc).ToLocalTime().DateTime;
            }
            catch { _lastTrustIncreaseTimePicker.Value = DateTime.Now; }

            // LastTrustDecreaseTime
            try
            {
                long trustDec = comp.GetLong("LastTrustDecreaseTime");
                _lastTrustDecreaseTimePicker.Value = DateTimeOffset.FromUnixTimeSeconds(trustDec).ToLocalTime().DateTime;
            }
            catch { _lastTrustDecreaseTimePicker.Value = DateTime.Now; }

            // Descriptors (Parts)
            LoadDescriptors(comp);

            // Slot Unlocked
            LoadUnlockStatus(entry);

            // Accessory Customisation
            LoadAccessories(entry);

            // Battle data
            LoadBattleData(comp, entry);

            // Disable accessory and battle controls for eggs
            bool isEgg = entry.Source == "Egg";
            SetAccessoryControlsEnabled(!isEgg && !entry.IsEmpty);
            SetBattleControlsEnabled(!isEgg && !entry.IsEmpty);
        }
        finally { _loading = false; }
    }

    private void LoadUnlockStatus((JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty) entry)
    {
        try
        {
            if (entry.Source == "Pet" && _playerState != null)
            {
                var unlockedSlots = _playerState.GetArray("UnlockedPetSlots");
                int origIdx = entry.OriginalIndex;
                _unlockedCheck.Checked = unlockedSlots != null && origIdx < unlockedSlots.Length && unlockedSlots.GetBool(origIdx);
                _unlockedCheck.Enabled = true;
            }
            else
            {
                _unlockedCheck.Checked = false;
                _unlockedCheck.Enabled = false;
            }
        }
        catch { _unlockedCheck.Checked = false; _unlockedCheck.Enabled = false; }
    }

    private void RefreshListEntry()
    {
        int idx = _companionList.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;
        var entry = _entries[idx];
        var comp = entry.Companion;
        bool occupied = !entry.IsEmpty;

        bool locked = false;
        if (entry.Source == "Pet" && _playerState != null)
        {
            var unlockedSlots = _playerState.GetArray("UnlockedPetSlots");
            locked = unlockedSlots == null || entry.OriginalIndex >= unlockedSlots.Length || !unlockedSlots.GetBool(entry.OriginalIndex);
        }

        string newLabel = GetSlotLabel(comp, entry.Source, entry.OriginalIndex, occupied, locked);
        _entries[idx] = (comp, newLabel, entry.Source, entry.OriginalIndex, entry.IsEmpty);
        _companionList.Items[idx] = newLabel;
    }

    // Write-back helpers (all write directly to the underlying JsonObject)
    private void WriteType()
    {
        var comp = SelectedCompanion;
        if (comp == null || _typeField.SelectedItem == null) return;
        var entry = _typeField.SelectedItem as CompanionEntry;
        if (entry != null)
        {
            comp.Set("CreatureID", entry.Id);

            // Mark the slot as occupied so the game recognises it
            ActivateSlotIfEmpty(comp);
        }
    }

    /// <summary>
    /// When editing a previously-empty slot, flips CreatureSeed[0] to true and
    /// updates the internal entry so subsequent writes and descriptor loading work.
    /// </summary>
    private void ActivateSlotIfEmpty(JsonObject comp)
    {
        int idx = _companionList.SelectedIndex;
        if (idx < 0 || idx >= _entries.Count) return;
        var e = _entries[idx];
        if (!e.IsEmpty) return;

        // Activate the seed flag
        var seedArr = comp.GetArray("CreatureSeed");
        if (seedArr != null && seedArr.Length >= 2)
            seedArr.Set(0, true);

        // Mark entry as no longer empty
        _entries[idx] = (e.Companion, e.Label, e.Source, e.OriginalIndex, false);
        _deleteBtn.Enabled = true;
        RefreshListEntry();
    }

    private void WriteName()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("CustomName", _nameField.Text);
        RefreshListEntry();
    }

    private void WriteCreatureSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var normalized = SeedHelper.NormalizeSeed(_creatureSeedField.Text);
        if (normalized == null) return;
        var arr = comp.GetArray("CreatureSeed");
        if (arr != null && arr.Length >= 2)
            arr.Set(1, normalized);
    }

    private void WriteSecondarySeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var arr = comp.GetArray("CreatureSecondarySeed");
        if (arr != null && arr.Length >= 2)
        {
            var normalized = SeedHelper.NormalizeSeed(_secondarySeedField.Text);
            bool hasValue = normalized != null;
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    private void WriteSpeciesSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var normalized = SeedHelper.NormalizeSeed(_speciesSeedField.Text);
        if (normalized != null)
            comp.Set("SpeciesSeed", normalized);
    }

    private void WriteGenusSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var normalized = SeedHelper.NormalizeSeed(_genusSeedField.Text);
        if (normalized != null)
            comp.Set("GenusSeed", normalized);
    }

    private void WritePredator()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("Predator", _predatorField.Checked);
    }

    private void WriteBiome()
    {
        var comp = SelectedCompanion;
        if (comp == null || _biomeField.SelectedItem == null) return;
        var biomeObj = comp.GetObject("Biome");
        biomeObj?.Set("Biome", (string)_biomeField.SelectedItem);
    }

    private void WriteCreatureType()
    {
        var comp = SelectedCompanion;
        if (comp == null || _creatureTypeField.SelectedItem == null) return;
        var ctObj = comp.GetObject("CreatureType");
        ctObj?.Set("CreatureType", (string)_creatureTypeField.SelectedItem);
    }

    private void WriteScale()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (double.TryParse(_scaleField.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            comp.Set("Scale", val);
    }

    private void WriteTrust()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (double.TryParse(_trustField.Text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            comp.Set("Trust", val);
    }

    private void WriteBoneScaleSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var arr = comp.GetArray("BoneScaleSeed");
        if (arr != null && arr.Length >= 2)
        {
            var normalized = SeedHelper.NormalizeSeed(_boneScaleSeedField.Text);
            bool hasValue = normalized != null && normalized != "0x0";
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    private void WriteColourBaseSeed()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        var arr = comp.GetArray("ColourBaseSeed");
        if (arr != null && arr.Length >= 2)
        {
            var normalized = SeedHelper.NormalizeSeed(_colourBaseSeedField.Text);
            bool hasValue = normalized != null && normalized != "0x0";
            arr.Set(0, hasValue);
            arr.Set(1, hasValue ? normalized! : "0x0");
        }
    }

    private void WriteHasFur()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("HasFur", _hasFurField.Checked);
    }

    private void WriteTrait(int index, TextBox field)
    {
        if (_loading) return;
        var comp = SelectedCompanion;
        if (comp == null) return;
        var traits = comp.GetArray("Traits");
        if (traits != null && index < traits.Length && double.TryParse(field.Text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            traits.Set(index, val);
    }

    private void WriteMood(int index, TextBox field)
    {
        if (_loading) return;
        var comp = SelectedCompanion;
        if (comp == null) return;
        var moods = comp.GetArray("Moods");
        if (moods != null && index < moods.Length && double.TryParse(field.Text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
            moods.Set(index, val);
    }

    private void WriteCustomSpeciesName()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        string text = _customSpeciesNameField.Text;
        // Save format uses ^ prefix for species names
        comp.Set("CustomSpeciesName", string.IsNullOrEmpty(text) ? "^" : $"^{text.TrimStart('^')}");
    }

    private void WriteEggModified()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("EggModified", _eggModifiedField.Checked);
    }

    private void WriteHasBeenSummoned()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("HasBeenSummoned", _hasBeenSummonedField.Checked);
    }

    private void WriteAllowUnmodifiedReroll()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("AllowUnmodifiedReroll", _allowUnmodifiedRerollField.Checked);
    }

    private void WriteUA()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (long.TryParse(_uaField.Text, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out long val))
            comp.Set("UA", val);
    }

    /// <summary>
    /// Reloads descriptor dropdowns when the creature type changes.
    /// Clears old descriptors (they belong to the old creature type) and shows the
    /// new creature's part groups from the Creature Builder database.
    /// </summary>
    private void ReloadDescriptorsForCurrentCompanion()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        // Clear existing descriptors since they belong to the old creature type
        var descArr = comp.GetArray("Descriptors");
        if (descArr != null)
        {
            for (int i = descArr.Length - 1; i >= 0; i--)
                descArr.RemoveAt(i);
            // Add a fresh descriptor ID for the new creature
            descArr.Add($"^{CreaturePartDatabase.NewDescriptorId()}");
        }

        _loading = true;
        try { LoadDescriptors(comp); }
        finally { _loading = false; }
    }

    /// <summary>
    /// Loads descriptor part dropdowns for the selected companion based on its CreatureID.
    /// Each creature type has a tree of part groups from the Creature Builder database.
    /// </summary>
    private void LoadDescriptors(JsonObject comp)
    {
        _descriptorPanel.SuspendLayout();
        _descriptorPanel.Controls.Clear();

        string creatureId = comp.GetString("CreatureID") ?? "";
        var partEntry = CreaturePartDatabase.GetForCreatureId(creatureId);

        // Read current descriptors from save (stored as ["^DESC1", "^DESC2", ..., "^0123456789"])
        var currentDescriptors = new List<string>();
        try
        {
            var descArr = comp.GetArray("Descriptors");
            if (descArr != null)
            {
                for (int i = 0; i < descArr.Length; i++)
                {
                    string d = descArr.GetString(i) ?? "";
                    currentDescriptors.Add(d.TrimStart('^'));
                }
            }
        }
        catch { }

        if (partEntry == null || partEntry.Details.Count == 0)
        {
            // No part data available - show raw descriptor list for manual editing
            var rawLabel = new Label
            {
                Text = currentDescriptors.Count > 0
                    ? UiStrings.Format("companion.raw_descriptors", currentDescriptors.Count) + string.Join(", ", currentDescriptors)
                    : UiStrings.Get("companion.no_part_data"),
                AutoSize = true,
                Padding = new Padding(0, 2, 0, 2),
            };
            _descriptorPanel.Controls.Add(rawLabel);
            _descriptorPanel.ResumeLayout(true);
            return;
        }

        // Get flattened groups based on current selections
        var flatGroups = CreaturePartDatabase.GetFlatGroups(partEntry, currentDescriptors);

        foreach (var group in flatGroups)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2),
            };

            var lbl = new Label
            {
                Text = group.GroupId.Trim('_') + ":",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 5, 0),
                Width = 120,
            };

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 200,
                Tag = group.GroupId,
            };

            // Add a "(none)" option
            combo.Items.Add(UiStrings.Get("companion.none"));
            int selectedIdx = 0;

            for (int i = 0; i < group.Descriptors.Count; i++)
            {
                var desc = group.Descriptors[i];
                combo.Items.Add(desc);
                if (currentDescriptors.Contains(desc.Id, StringComparer.OrdinalIgnoreCase))
                    selectedIdx = i + 1; // +1 for (none)
            }

            combo.SelectedIndex = selectedIdx;
            combo.SelectedIndexChanged += OnDescriptorChanged;

            row.Controls.Add(lbl);
            row.Controls.Add(combo);
            _descriptorPanel.Controls.Add(row);
        }

        // Add a "Regen Descriptor ID" button
        _regenDescriptorBtn = new Button { Text = UiStrings.Get("companion.regen_descriptor_id"), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        _regenDescriptorBtn.Click += (s, e) =>
        {
            if (_loading) return;
            WriteDescriptors();
        };
        _descriptorPanel.Controls.Add(_regenDescriptorBtn);

        _descriptorPanel.ResumeLayout(true);
    }

    /// <summary>
    /// Handles descriptor dropdown changes - writes descriptors and refreshes child groups.
    /// </summary>
    private void OnDescriptorChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        WriteDescriptors();
        // Reload descriptors to show/hide child groups based on new selection
        var comp = SelectedCompanion;
        if (comp != null)
        {
            _loading = true;
            try { LoadDescriptors(comp); }
            finally { _loading = false; }
        }
    }

    /// <summary>
    /// Collects current descriptor selections from the UI and writes them back to the companion JSON.
    /// Always appends a descriptor ID as the last element.
    /// </summary>
    private void WriteDescriptors()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var descriptors = new List<string>();
        foreach (Control ctrl in _descriptorPanel.Controls)
        {
            if (ctrl is FlowLayoutPanel row)
            {
                foreach (Control rowChild in row.Controls)
                {
                    if (rowChild is ComboBox combo && combo.SelectedItem is DescriptorOption opt)
                    {
                        descriptors.Add(opt.Id);
                    }
                }
            }
        }

        // Always append a descriptor ID (10-digit random number)
        descriptors.Add(CreaturePartDatabase.NewDescriptorId());

        // Write to the Descriptors array on the companion
        var descArr = comp.GetArray("Descriptors");
        if (descArr != null)
        {
            // Clear existing
            for (int i = descArr.Length - 1; i >= 0; i--)
                descArr.RemoveAt(i);
            // Add new values (with ^ prefix)
            foreach (var d in descriptors)
                descArr.Add($"^{d}");
        }
    }

    // Accessory Customisation
    // Management for pet accessory customisation, based on initial loose testing in game
    // Colours probably need to be set to the actual in game palette entries rather than arbitrary RGBA values,
    // but I think these work like ship customisation where it works for the player (mostly)

    /// <summary>Gets the PetAccessoryCustomisation entry for a given pet index, or null.</summary>
    private JsonObject? GetPetAccessoryCustomisationEntry(int petIndex)
    {
        if (_playerState == null) return null;
        try
        {
            var pac = _playerState.GetArray("PetAccessoryCustomisation");
            if (pac != null && petIndex < pac.Length)
                return pac.GetObject(petIndex);
        }
        catch { }
        return null;
    }

    /// <summary>Gets the Data[slotIndex] object within a PetAccessoryCustomisation entry.</summary>
    private static JsonObject? GetAccessorySlotData(JsonObject pacEntry, int slotIndex)
    {
        try
        {
            var data = pacEntry.GetArray("Data");
            if (data != null && slotIndex < data.Length)
                return data.GetObject(slotIndex);
        }
        catch { }
        return null;
    }

    /// <summary>Loads accessory data for the selected companion into the 3 slot controls.</summary>
    private void LoadAccessories((JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty) entry)
    {
        if (entry.Source != "Pet" || entry.IsEmpty)
        {
            for (int i = 0; i < 3; i++)
            {
                _accessoryCombos[i].Items.Clear();
                _accessoryCombos[i].SelectedIndex = -1;
                _accessoryDescriptorLabels[i].Text = "";
                _accessoryPrimarySwatches[i].BackColor = SystemColors.Control;
                _accessoryAltSwatches[i].BackColor = SystemColors.Control;
                _accessoryScaleFields[i].Text = "1.0";
            }
            return;
        }

        var pacEntry = GetPetAccessoryCustomisationEntry(entry.OriginalIndex);

        for (int slot = 0; slot < 3; slot++)
        {
            // Populate combo with slot-filtered entries
            _accessoryCombos[slot].Items.Clear();
            _accessoryCombos[slot].Items.Add(UiStrings.GetOrNull("companion.accessory_none") ?? "None");
            var slotEntries = CompanionAccessoryDatabase.GetEntriesForSlot((AccessorySlot)slot);
            foreach (var accEntry in slotEntries)
            {
                if (accEntry.Id == "PET_ACC_NULL") continue; // Skip NULL entry, we have "None"
                _accessoryCombos[slot].Items.Add(accEntry);
            }

            // Read current slot data
            int selectedIdx = 0;
            _accessoryDescriptorLabels[slot].Text = "";
            _accessoryPrimarySwatches[slot].BackColor = SystemColors.Control;
            _accessoryAltSwatches[slot].BackColor = SystemColors.Control;
            _accessoryScaleFields[slot].Text = "1.0";

            if (pacEntry != null)
            {
                var slotData = GetAccessorySlotData(pacEntry, slot);
                if (slotData != null)
                {
                    string preset = slotData.GetString("SelectedPreset") ?? "^DEFAULT_PET";
                    if (preset != "^DEFAULT_PET")
                    {
                        // Look for the accessory ID in DescriptorGroups[0]
                        try
                        {
                            var customData = slotData.GetObject("CustomData");
                            if (customData != null)
                            {
                                var descGroups = customData.GetArray("DescriptorGroups");
                                if (descGroups != null && descGroups.Length > 0)
                                {
                                    string accId = (descGroups.GetString(0) ?? "").TrimStart('^');
                                    // Find in combo
                                    for (int ci = 1; ci < _accessoryCombos[slot].Items.Count; ci++)
                                    {
                                        if (_accessoryCombos[slot].Items[ci] is CompanionAccessoryEntry cae &&
                                            string.Equals(cae.Id, accId, StringComparison.OrdinalIgnoreCase))
                                        {
                                            selectedIdx = ci;
                                            _accessoryDescriptorLabels[slot].Text = cae.Descriptor ?? "";
                                            break;
                                        }
                                    }
                                }

                                // Colours
                                var colours = customData.GetArray("Colours");
                                if (colours != null)
                                {
                                    if (colours.Length > 0)
                                        _accessoryPrimarySwatches[slot].BackColor = ReadColourFromArray(colours, 0);
                                    if (colours.Length > 1)
                                        _accessoryAltSwatches[slot].BackColor = ReadColourFromArray(colours, 1);
                                }

                                // Scale
                                try
                                {
                                    double scale = customData.GetDouble("Scale");
                                    _accessoryScaleFields[slot].Text = scale.ToString(CultureInfo.InvariantCulture);
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }
            }

            _accessoryCombos[slot].SelectedIndex = selectedIdx;
        }
    }

    /// <summary>Reads an RGBA colour from a Colours array at a given index.</summary>
    private static Color ReadColourFromArray(JsonArray coloursArray, int index)
    {
        try
        {
            var colourEntry = coloursArray.GetObject(index);
            var colArr = colourEntry?.GetArray("Colour");
            if (colArr != null && colArr.Length >= 3)
            {
                int r = (int)Math.Clamp(colArr.GetDouble(0) * 255, 0, 255);
                int g = (int)Math.Clamp(colArr.GetDouble(1) * 255, 0, 255);
                int b = (int)Math.Clamp(colArr.GetDouble(2) * 255, 0, 255);
                return Color.FromArgb(r, g, b);
            }
        }
        catch { }
        return SystemColors.Control;
    }

    /// <summary>Handles accessory combo selection change for a given slot.</summary>
    private void OnAccessoryChanged(int slotIndex)
    {
        var entry = SelectedEntry;
        if (entry.Source != "Pet" || _playerState == null) return;

        var pacEntry = GetPetAccessoryCustomisationEntry(entry.OriginalIndex);
        if (pacEntry == null) return;
        var slotData = GetAccessorySlotData(pacEntry, slotIndex);
        if (slotData == null) return;

        var selectedItem = _accessoryCombos[slotIndex].SelectedItem;
        if (selectedItem is not CompanionAccessoryEntry accEntry)
        {
            // "None" selected — reset to default
            slotData.Set("SelectedPreset", "^DEFAULT_PET");
            var cd = slotData.GetObject("CustomData");
            if (cd != null)
            {
                ClearJsonArrayContents(cd.GetArray("DescriptorGroups"));
                ClearJsonArrayContents(cd.GetArray("Colours"));
                cd.Set("Scale", 1.0);
            }
            _accessoryDescriptorLabels[slotIndex].Text = "";
            _accessoryPrimarySwatches[slotIndex].BackColor = SystemColors.Control;
            _accessoryAltSwatches[slotIndex].BackColor = SystemColors.Control;
            _accessoryScaleFields[slotIndex].Text = "1.0";
            return;
        }

        // Set accessory
        slotData.Set("SelectedPreset", "^");
        var customData = slotData.GetObject("CustomData");
        if (customData != null)
        {
            var descGroups = customData.GetArray("DescriptorGroups");
            if (descGroups != null)
            {
                // Preserve existing decal at index 1 if present
                string? existingDecal = null;
                if (descGroups.Length > 1)
                {
                    try { existingDecal = descGroups.GetString(1); } catch { }
                }
                ClearJsonArrayContents(descGroups);
                descGroups.Add($"^{accEntry.Id}");
                if (!string.IsNullOrEmpty(existingDecal))
                    descGroups.Add(existingDecal);
            }
        }
        _accessoryDescriptorLabels[slotIndex].Text = accEntry.Descriptor ?? "";
    }

    /// <summary>Handles colour button click for an accessory slot.</summary>
    private void OnAccessoryColourClick(int slotIndex, int colourIndex)
    {
        var entry = SelectedEntry;
        if (entry.Source != "Pet" || _playerState == null) return;

        var pacEntry = GetPetAccessoryCustomisationEntry(entry.OriginalIndex);
        if (pacEntry == null) return;
        var slotData = GetAccessorySlotData(pacEntry, slotIndex);
        if (slotData == null) return;

        var swatch = colourIndex == 0 ? _accessoryPrimarySwatches[slotIndex] : _accessoryAltSwatches[slotIndex];

        using var colorDialog = new ColorDialog { Color = swatch.BackColor, FullOpen = true };
        if (colorDialog.ShowDialog() != DialogResult.OK) return;

        swatch.BackColor = colorDialog.Color;

        // Write back to save
        var customData = slotData.GetObject("CustomData");
        if (customData == null) return;

        var colours = customData.GetArray("Colours");
        if (colours == null) return;

        EnsureColourArraySize(colours, colourIndex + 1);

        try
        {
            var colourEntry = colours.GetObject(colourIndex);
            var colArr = colourEntry?.GetArray("Colour");
            if (colArr != null && colArr.Length >= 4)
            {
                double r = colorDialog.Color.R / 255.0;
                double g = colorDialog.Color.G / 255.0;
                double b = colorDialog.Color.B / 255.0;
                colArr.Set(0, r);
                colArr.Set(1, g);
                colArr.Set(2, b);
                colArr.Set(3, 1.0);
            }
        }
        catch { }
    }

    /// <summary>Ensures the Colours array has at least the specified number of entries.</summary>
    private static void EnsureColourArraySize(JsonArray colours, int minCount)
    {
        // If the array is smaller, we can't easily add new complex objects without a template.
        // The game always pre-populates these, so this is a safety check.
    }

    /// <summary>Handles accessory scale change for a given slot.</summary>
    private void OnAccessoryScaleChanged(int slotIndex)
    {
        if (_loading) return;
        var entry = SelectedEntry;
        if (entry.Source != "Pet" || _playerState == null) return;

        var pacEntry = GetPetAccessoryCustomisationEntry(entry.OriginalIndex);
        if (pacEntry == null) return;
        var slotData = GetAccessorySlotData(pacEntry, slotIndex);
        if (slotData == null) return;

        if (double.TryParse(_accessoryScaleFields[slotIndex].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
            var customData = slotData.GetObject("CustomData");
            customData?.Set("Scale", val);
        }
    }

    /// <summary>Handles per-slot accessory reset.</summary>
    private void OnAccessoryReset(int slotIndex)
    {
        var entry = SelectedEntry;
        if (entry.Source != "Pet" || _playerState == null) return;

        var pacEntry = GetPetAccessoryCustomisationEntry(entry.OriginalIndex);
        if (pacEntry == null) return;
        var slotData = GetAccessorySlotData(pacEntry, slotIndex);
        if (slotData == null) return;

        slotData.Set("SelectedPreset", "^DEFAULT_PET");
        var customData = slotData.GetObject("CustomData");
        if (customData != null)
        {
            ClearJsonArrayContents(customData.GetArray("DescriptorGroups"));
            ClearJsonArrayContents(customData.GetArray("Colours"));
            customData.Set("Scale", 1.0);
        }

        // Refresh display
        _loading = true;
        try
        {
            _accessoryCombos[slotIndex].SelectedIndex = 0; // "None"
            _accessoryDescriptorLabels[slotIndex].Text = "";
            _accessoryPrimarySwatches[slotIndex].BackColor = SystemColors.Control;
            _accessoryAltSwatches[slotIndex].BackColor = SystemColors.Control;
            _accessoryScaleFields[slotIndex].Text = "1.0";
        }
        finally { _loading = false; }
    }

    /// <summary>Enables or disables all accessory controls.</summary>
    private void SetAccessoryControlsEnabled(bool enabled)
    {
        for (int i = 0; i < 3; i++)
        {
            _accessoryCombos[i].Enabled = enabled;
            _accessoryPrimarySwatches[i].Enabled = enabled;
            _accessoryAltSwatches[i].Enabled = enabled;
            _accessoryPrimarySwatches[i].Cursor = enabled ? Cursors.Hand : Cursors.Default;
            _accessoryAltSwatches[i].Cursor = enabled ? Cursors.Hand : Cursors.Default;
            _accessoryScaleFields[i].Enabled = enabled;
            _accessoryResetBtns[i].Enabled = enabled;
        }
    }

    /// <summary>Clears all elements from a JsonArray without removing the array itself.</summary>
    private static void ClearJsonArrayContents(JsonArray? arr)
    {
        if (arr == null) return;
        for (int i = arr.Length - 1; i >= 0; i--)
            arr.RemoveAt(i);
    }

    // Pet Battle (Xeno Arena) Companion Data
    // Might expand later to include all "game table" content once the dice game and other potential
    // features are added and datamined/reversed if they are even companion/pet related.

    /// <summary>Class letter to integer value mapping.</summary>
    private static int ClassToInt(string cls) => cls switch
    {
        "S" => 3, "A" => 2, "B" => 1, _ => 0
    };

    /// <summary>Integer value to class letter mapping.</summary>
    private static string IntToClass(int val) => val switch
    {
        >= 3 => "S", 2 => "A", 1 => "B", _ => "C"
    };

    /// <summary>Reads the InventoryClass string from a stat class override array element.</summary>
    private static string ReadClassOverride(JsonArray? overrides, int index)
    {
        try
        {
            if (overrides != null && index < overrides.Length)
            {
                var obj = overrides.GetObject(index);
                return obj?.GetString("InventoryClass") ?? "C";
            }
        }
        catch { }
        return "C";
    }

    /// <summary>Loads all battle data for the selected companion.</summary>
    private void LoadBattleData(JsonObject comp, (JsonObject Companion, string Label, string Source, int OriginalIndex, bool IsEmpty) entry)
    {
        // Lazily initialise per-slot move data on first call (databases not loaded at construction time)
        if (!_moveSlotDataInitialised)
        {
            InitialiseMoveSlotData();
            _moveSlotDataInitialised = true;
        }
        // Affinity display
        try
        {
            var biomeObj = comp.GetObject("Biome");
            string biome = biomeObj?.GetString("Biome") ?? "";
            string affinity = PetBiomeAffinityMap.BiomeToAffinity(biome);
            string display = !string.IsNullOrEmpty(affinity)
                ? PetBiomeAffinityMap.GetAffinityDisplayName(affinity)
                : "";
            _battleAffinityValue.Text = display;
        }
        catch { _battleAffinityValue.Text = ""; }

        // Stat Class Overrides
        try { _battleOverrideCheck.Checked = comp.GetBool("PetBattlerUseCoreStatClassOverrides"); } catch { _battleOverrideCheck.Checked = false; }

        try
        {
            var overrides = comp.GetArray("PetBattlerCoreStatClassOverrides");
            _battleHealthClass.SelectedItem = ReadClassOverride(overrides, 0);
            _battleAgilityClass.SelectedItem = ReadClassOverride(overrides, 1);
            _battleCombatClass.SelectedItem = ReadClassOverride(overrides, 2);
        }
        catch
        {
            _battleHealthClass.SelectedItem = "C";
            _battleAgilityClass.SelectedItem = "C";
            _battleCombatClass.SelectedItem = "C";
        }

        UpdateClassOverrideEnabled();
        UpdateAverageClass();

        // Clear raw battle values for the newly selected companion
        _rawBattleIntValues.Clear();
        _rawBattleDecimalValues.Clear();
        _rawBattleDoubleValues.Clear();

        // Treats
        try
        {
            var treats = comp.GetArray("PetBattlerTreatsEaten");
            int rawHealth = treats != null && treats.Length > 0 ? treats.GetInt(0) : 0;
            int rawAgility = treats != null && treats.Length > 1 ? treats.GetInt(1) : 0;
            int rawCombat = treats != null && treats.Length > 2 ? treats.GetInt(2) : 0;
            _rawBattleIntValues["TreatHealth"] = rawHealth;
            _rawBattleIntValues["TreatAgility"] = rawAgility;
            _rawBattleIntValues["TreatCombat"] = rawCombat;
            _battleTreatHealth.Value = Math.Clamp(rawHealth, 0, 10);
            _battleTreatAgility.Value = Math.Clamp(rawAgility, 0, 10);
            _battleTreatCombat.Value = Math.Clamp(rawCombat, 0, 10);
        }
        catch
        {
            _rawBattleIntValues["TreatHealth"] = 0;
            _rawBattleIntValues["TreatAgility"] = 0;
            _rawBattleIntValues["TreatCombat"] = 0;
            _battleTreatHealth.Value = 0;
            _battleTreatAgility.Value = 0;
            _battleTreatCombat.Value = 0;
        }
        UpdateGenesLevel();

        try
        {
            int rawGenes = comp.GetInt("PetBattlerTreatsAvailable");
            _rawBattleIntValues["GenesAvailable"] = rawGenes;
            _battleGenesAvailable.Value = Math.Clamp(rawGenes, 0, 100);
        }
        catch { _rawBattleIntValues["GenesAvailable"] = 0; _battleGenesAvailable.Value = 0; }

        try
        {
            double rawMutation = comp.GetDouble("PetBattleProgressToTreat");
            _rawBattleDoubleValues["MutationProgress"] = rawMutation;
            _battleMutationProgress.Text = rawMutation.ToString(CultureInfo.InvariantCulture);
        }
        catch { _rawBattleDoubleValues["MutationProgress"] = 0; _battleMutationProgress.Text = "0"; }

        try
        {
            int rawVictories = comp.GetInt("PetBattlerVictories");
            _rawBattleIntValues["Victories"] = rawVictories;
            _battleVictories.Value = Math.Clamp(rawVictories, 0, 999999);
        }
        catch { _rawBattleIntValues["Victories"] = 0; _battleVictories.Value = 0; }

        // Move list
        LoadMoveSlots(comp);
    }

    /// <summary>Loads move slot data from the companion's PetBattlerMoveList.</summary>
    private void LoadMoveSlots(JsonObject comp)
    {
        JsonArray? moveList = null;
        try { moveList = comp.GetArray("PetBattlerMoveList"); } catch { }

        for (int i = 0; i < 5; i++)
        {
            // Populate combo
            _moveSlotCombos[i].Items.Clear();
            _moveSlotCombos[i].Items.Add(UiStrings.GetOrNull("companion.battle_move_none") ?? "None");

            foreach (var move in _allowedMovesPerSlot[i])
                _moveSlotCombos[i].Items.Add(move);

            // Read current move
            string moveId = "";
            int cooldown = 0;
            double scoreBoost = 0;

            if (moveList != null && i < moveList.Length)
            {
                try
                {
                    var moveObj = moveList.GetObject(i);
                    if (moveObj != null)
                    {
                        moveId = (moveObj.GetString("MoveTemplateID") ?? "").TrimStart('^');
                        try { cooldown = moveObj.GetInt("Cooldown"); } catch { }
                        try { scoreBoost = moveObj.GetDouble("ScoreBoost"); } catch { }
                    }
                }
                catch { }
            }

            // Select move in combo
            int selectedIdx = 0;
            if (!string.IsNullOrEmpty(moveId))
            {
                for (int ci = 1; ci < _moveSlotCombos[i].Items.Count; ci++)
                {
                    if (_moveSlotCombos[i].Items[ci] is PetBattleMoveEntry moveEntry &&
                        string.Equals(moveEntry.Id, moveId, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIdx = ci;
                        break;
                    }
                }
            }
            _moveSlotCombos[i].SelectedIndex = selectedIdx;

            // Cooldown and score boost
            var (cdMin, cdMax) = GetCooldownRange(moveId, i);
            _moveSlotCooldowns[i].Minimum = cdMin;
            _moveSlotCooldowns[i].Maximum = cdMax;
            _rawBattleIntValues[$"Cooldown_{i}"] = cooldown;
            _moveSlotCooldowns[i].Value = Math.Clamp(cooldown, cdMin, cdMax);
            _rawBattleDecimalValues[$"ScoreBoost_{i}"] = (decimal)scoreBoost;
            _moveSlotScoreBoosts[i].Value = Math.Clamp((decimal)scoreBoost, 0, 10);

            // Moveset label and detail
            UpdateMoveSlotInfo(i, moveId);
        }
    }

    /// <summary>Gets cooldown min/max range for a move in a given slot across all movesets.</summary>
    private static (int Min, int Max) GetCooldownRange(string moveId, int slotIndex)
    {
        if (string.IsNullOrEmpty(moveId)) return (0, 20);

        int slotNumber = slotIndex + 1;
        int globalMin = int.MaxValue, globalMax = int.MinValue;

        foreach (var ms in PetBattleMovesetDatabase.Movesets)
        {
            var slot = ms.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
            if (slot == null) continue;
            foreach (var opt in slot.Options)
            {
                if (string.Equals(opt.Template, moveId, StringComparison.OrdinalIgnoreCase))
                {
                    if (opt.CooldownMin < globalMin) globalMin = opt.CooldownMin;
                    if (opt.CooldownMax > globalMax) globalMax = opt.CooldownMax;
                }
            }
        }

        return globalMin <= globalMax ? (globalMin, globalMax) : (0, 20);
    }

    /// <summary>Updates the moveset label and move detail panel for a slot.</summary>
    private void UpdateMoveSlotInfo(int slotIndex, string moveId)
    {
        if (string.IsNullOrEmpty(moveId))
        {
            _moveSlotMovesetLabels[slotIndex].Text = "";
            _moveSlotDetailPanels[slotIndex].Visible = false;
            return;
        }

        // Find which movesets contain this move
        var movesets = PetBattleMovesetDatabase.FindMovesetsContainingMove(moveId);
        _moveSlotMovesetLabels[slotIndex].Text = movesets.Count > 0
            ? string.Join(Environment.NewLine, movesets.Select(ms => ms.DisplayName))
            : "";

        // Move detail panel
        if (PetBattleMoveDatabase.ById.TryGetValue(moveId, out var move))
        {
            var panel = _moveSlotDetailPanels[slotIndex];
            panel.SuspendLayout();
            panel.Controls.Clear();
            panel.RowStyles.Clear();

            // Left column: base fields (Type, Target, Multi-Turn, Basic Move, Stat Affected, Strength, Effect)
            var leftEntries = new List<(string Label, string Value)>();
            leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_type") ?? "Type:", $"{move.IconEmoji} {move.IconStyleDisplay}"));
            leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_target") ?? "Target:", move.TargetDisplay));
            leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_multiturn") ?? "Multi-Turn:",
                move.MultiTurnMove ? (UiStrings.GetOrNull("companion.battle_move_detail_yes") ?? "Yes") : (UiStrings.GetOrNull("companion.battle_move_detail_no") ?? "No")));
            leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_basic") ?? "Basic Move:",
                move.BasicMove ? (UiStrings.GetOrNull("companion.battle_move_detail_yes") ?? "Yes") : (UiStrings.GetOrNull("companion.battle_move_detail_no") ?? "No")));

            if (!string.IsNullOrEmpty(move.LocIDToDescribeStat))
            {
                string statDesc = DisplayStringHelper.NormalizeDisplayString(
                    move.LocIDToDescribeStat.Replace("UI_PB_STAT_", ""));
                leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_stat") ?? "Stat Affected:", statDesc));
            }

            // Single-phase moves: Strength/Effect go in left column, no phase prefix
            // Multi-phase moves: all phase entries go in right column with phase prefix
            var rightEntries = new List<(string Label, string Value)>();
            if (move.Phases.Count == 1)
            {
                var phase = move.Phases[0];
                leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_strength") ?? "Strength:", phase.StrengthDisplay));
                leftEntries.Add((UiStrings.GetOrNull("companion.battle_move_detail_effect") ?? "Effect:", phase.EffectDisplay));
            }
            else
            {
                for (int p = 0; p < move.Phases.Count; p++)
                {
                    var phase = move.Phases[p];
                    string prefix = UiStrings.Format("companion.battle_move_detail_phase", p + 1) + " ";
                    rightEntries.Add(($"{prefix}{UiStrings.GetOrNull("companion.battle_move_detail_strength") ?? "Strength:"}", phase.StrengthDisplay));
                    rightEntries.Add(($"{prefix}{UiStrings.GetOrNull("companion.battle_move_detail_effect") ?? "Effect:"}", phase.EffectDisplay));
                }
            }

            int totalRows = Math.Max(leftEntries.Count, rightEntries.Count);

            for (int r = 0; r < totalRows; r++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                // Left column pair
                if (r < leftEntries.Count)
                {
                    var leftEntry = leftEntries[r];
                    panel.Controls.Add(new Label { Text = leftEntry.Label, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Padding = new Padding(0, 1, 5, 1) }, 0, r);
                    panel.Controls.Add(new Label { Text = leftEntry.Value, AutoSize = true, Padding = new Padding(0, 1, 0, 1) }, 1, r);
                }

                // Right column pair (phase fields)
                if (r < rightEntries.Count)
                {
                    var rightEntry = rightEntries[r];
                    panel.Controls.Add(new Label { Text = rightEntry.Label, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Padding = new Padding(0, 1, 5, 1) }, 3, r);
                    panel.Controls.Add(new Label { Text = rightEntry.Value, AutoSize = true, Padding = new Padding(0, 1, 0, 1) }, 4, r);
                }
            }

            panel.RowCount = totalRows;
            panel.ResumeLayout(true);
            panel.Visible = true;
        }
        else
        {
            _moveSlotDetailPanels[slotIndex].Visible = false;
        }
    }

    /// <summary>Handles move combo selection change.</summary>
    private void OnMoveSlotChanged(int slotIndex)
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var selectedItem = _moveSlotCombos[slotIndex].SelectedItem;
        string moveId = selectedItem is PetBattleMoveEntry moveEntry ? moveEntry.Id : "";

        // Write to save
        try
        {
            var moveList = comp.GetArray("PetBattlerMoveList");
            if (moveList != null && slotIndex < moveList.Length)
            {
                var moveObj = moveList.GetObject(slotIndex);
                if (moveObj != null)
                {
                    moveObj.Set("MoveTemplateID", string.IsNullOrEmpty(moveId) ? "^" : $"^{moveId}");

                    if (!string.IsNullOrEmpty(moveId))
                    {
                        // Set default cooldown and score boost from moveset data
                        var (cdMin, cdMax) = GetCooldownRange(moveId, slotIndex);
                        int defaultCd = cdMin;
                        double defaultWeight = GetDefaultWeighting(moveId, slotIndex);

                        moveObj.Set("Cooldown", defaultCd);
                        moveObj.Set("ScoreBoost", defaultWeight);

                        _loading = true;
                        try
                        {
                            _moveSlotCooldowns[slotIndex].Minimum = cdMin;
                            _moveSlotCooldowns[slotIndex].Maximum = cdMax;
                            _moveSlotCooldowns[slotIndex].Value = defaultCd;
                            _moveSlotScoreBoosts[slotIndex].Value = Math.Clamp((decimal)defaultWeight, 0, 10);
                        }
                        finally { _loading = false; }
                    }
                    else
                    {
                        moveObj.Set("Cooldown", 0);
                        moveObj.Set("ScoreBoost", 0.0);

                        _loading = true;
                        try
                        {
                            _moveSlotCooldowns[slotIndex].Minimum = 0;
                            _moveSlotCooldowns[slotIndex].Maximum = 20;
                            _moveSlotCooldowns[slotIndex].Value = 0;
                            _moveSlotScoreBoosts[slotIndex].Value = 0;
                        }
                        finally { _loading = false; }
                    }
                }
            }
        }
        catch { }

        UpdateMoveSlotInfo(slotIndex, moveId);
    }

    /// <summary>Gets the default weighting for a move in a slot from moveset data.</summary>
    private static double GetDefaultWeighting(string moveId, int slotIndex)
    {
        int slotNumber = slotIndex + 1;
        foreach (var ms in PetBattleMovesetDatabase.Movesets)
        {
            var slot = ms.Slots.FirstOrDefault(s => s.SlotNumber == slotNumber);
            if (slot == null) continue;
            foreach (var opt in slot.Options)
            {
                if (string.Equals(opt.Template, moveId, StringComparison.OrdinalIgnoreCase))
                    return opt.Weighting;
            }
        }
        return 1.0;
    }

    /// <summary>Handles cooldown value change for a move slot.</summary>
    private void OnMoveSlotCooldownChanged(int slotIndex)
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        // Skip write if user didn't change it from the clamped display value
        if (!WasBattleIntChangedByUser($"Cooldown_{slotIndex}", (int)_moveSlotCooldowns[slotIndex].Value, _moveSlotCooldowns[slotIndex]))
            return;

        try
        {
            var moveList = comp.GetArray("PetBattlerMoveList");
            if (moveList != null && slotIndex < moveList.Length)
            {
                var moveObj = moveList.GetObject(slotIndex);
                moveObj?.Set("Cooldown", (int)_moveSlotCooldowns[slotIndex].Value);
            }
        }
        catch { }
    }

    /// <summary>Handles score boost value change for a move slot.
    /// Snaps the value to the nearest 0.1 increment so that e.g. 1.660000
    /// with an up-arrow press results in 1.700000 rather than 1.760000.</summary>
    private void OnMoveSlotScoreBoostChanged(int slotIndex)
    {
        if (_loading) return;
        var comp = SelectedCompanion;
        if (comp == null) return;

        var nud = _moveSlotScoreBoosts[slotIndex];
        decimal rounded = Math.Round(nud.Value, 1, MidpointRounding.AwayFromZero);
        if (nud.Value != rounded)
        {
            _loading = true;
            try { nud.Value = rounded; }
            finally { _loading = false; }
        }

        // Skip write if user didn't change it from the clamped display value
        if (!WasBattleDecimalChangedByUser($"ScoreBoost_{slotIndex}", rounded, nud))
            return;

        double val = Math.Clamp((double)rounded, 0, 9.999999);
        try
        {
            var moveList = comp.GetArray("PetBattlerMoveList");
            if (moveList != null && slotIndex < moveList.Length)
            {
                var moveObj = moveList.GetObject(slotIndex);
                moveObj?.Set("ScoreBoost", val);
            }
        }
        catch { }
    }

    /// <summary>Handles the Override Pet Classes checkbox.</summary>
    private void OnBattleOverrideChanged()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;
        comp.Set("PetBattlerUseCoreStatClassOverrides", _battleOverrideCheck.Checked);
        UpdateClassOverrideEnabled();
        UpdateAverageClass();
    }

    /// <summary>Enables/disables the class combo boxes based on override checkbox.</summary>
    private void UpdateClassOverrideEnabled()
    {
        bool enabled = _battleOverrideCheck.Checked;
        _battleHealthClass.Enabled = enabled;
        _battleAgilityClass.Enabled = enabled;
        _battleCombatClass.Enabled = enabled;
    }

    /// <summary>Handles stat class combo change.</summary>
    private void OnBattleClassChanged()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        try
        {
            var overrides = comp.GetArray("PetBattlerCoreStatClassOverrides");
            if (overrides != null)
            {
                WriteClassOverride(overrides, 0, _battleHealthClass.SelectedItem as string ?? "C");
                WriteClassOverride(overrides, 1, _battleAgilityClass.SelectedItem as string ?? "C");
                WriteClassOverride(overrides, 2, _battleCombatClass.SelectedItem as string ?? "C");
            }
        }
        catch { }

        UpdateAverageClass();
    }

    /// <summary>Writes a class letter to a stat class override array element.</summary>
    private static void WriteClassOverride(JsonArray overrides, int index, string classLetter)
    {
        if (index >= overrides.Length) return;
        try
        {
            var obj = overrides.GetObject(index);
            obj?.Set("InventoryClass", classLetter);
        }
        catch { }
    }

    /// <summary>Calculates and displays the average stat class.</summary>
    private void UpdateAverageClass()
    {
        if (!_battleOverrideCheck.Checked)
        {
            _battleAverageClassValue.Text = "-";
            return;
        }

        int sum = ClassToInt(_battleHealthClass.SelectedItem as string ?? "C")
                + ClassToInt(_battleAgilityClass.SelectedItem as string ?? "C")
                + ClassToInt(_battleCombatClass.SelectedItem as string ?? "C");
        _battleAverageClassValue.Text = IntToClass(sum / 3);
    }

    /// <summary>Handles treat NumericUpDown changes.</summary>
    private void OnBattleTreatChanged()
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        try
        {
            var treats = comp.GetArray("PetBattlerTreatsEaten");
            if (treats != null)
            {
                if (treats.Length > 0 && WasBattleIntChangedByUser("TreatHealth", (int)_battleTreatHealth.Value, _battleTreatHealth))
                    treats.Set(0, (int)_battleTreatHealth.Value);
                if (treats.Length > 1 && WasBattleIntChangedByUser("TreatAgility", (int)_battleTreatAgility.Value, _battleTreatAgility))
                    treats.Set(1, (int)_battleTreatAgility.Value);
                if (treats.Length > 2 && WasBattleIntChangedByUser("TreatCombat", (int)_battleTreatCombat.Value, _battleTreatCombat))
                    treats.Set(2, (int)_battleTreatCombat.Value);
            }
        }
        catch { }

        UpdateGenesLevel();
    }

    /// <summary>Updates the genes level display label.</summary>
    private void UpdateGenesLevel()
    {
        int total = (int)_battleTreatHealth.Value + (int)_battleTreatAgility.Value + (int)_battleTreatCombat.Value;
        _battleGenesLevelValue.Text = $"{total} / 30";
    }

    /// <summary>Writes the treats available value.</summary>
    private void WriteBattleTreatsAvailable()
    {
        var comp = SelectedCompanion;
        if (comp == null || _loading) return;
        if (!WasBattleIntChangedByUser("GenesAvailable", (int)_battleGenesAvailable.Value, _battleGenesAvailable))
            return;
        comp.Set("PetBattlerTreatsAvailable", (int)_battleGenesAvailable.Value);
    }

    /// <summary>Writes the mutation progress value.</summary>
    private void WriteBattleMutationProgress()
    {
        if (_loading) return;
        var comp = SelectedCompanion;
        if (comp == null) return;
        if (double.TryParse(_battleMutationProgress.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
            // Skip write if user didn't change the value from what was loaded
            if (_rawBattleDoubleValues.TryGetValue("MutationProgress", out double raw) && val == raw)
                return;
            comp.Set("PetBattleProgressToTreat", val);
        }
    }

    /// <summary>Writes the victories value.</summary>
    private void WriteBattleVictories()
    {
        var comp = SelectedCompanion;
        if (comp == null || _loading) return;
        if (!WasBattleIntChangedByUser("Victories", (int)_battleVictories.Value, _battleVictories))
            return;
        comp.Set("PetBattlerVictories", (int)_battleVictories.Value);
    }

    /// <summary>Enables or disables all battle controls.</summary>
    private void SetBattleControlsEnabled(bool enabled)
    {
        _battleOverrideCheck.Enabled = enabled;
        _battleHealthClass.Enabled = enabled && _battleOverrideCheck.Checked;
        _battleAgilityClass.Enabled = enabled && _battleOverrideCheck.Checked;
        _battleCombatClass.Enabled = enabled && _battleOverrideCheck.Checked;
        _battleTreatHealth.Enabled = enabled;
        _battleTreatAgility.Enabled = enabled;
        _battleTreatCombat.Enabled = enabled;
        _battleGenesAvailable.Enabled = enabled;
        _battleMutationProgress.Enabled = enabled;
        _battleVictories.Enabled = enabled;
        for (int i = 0; i < 5; i++)
        {
            _moveSlotCombos[i].Enabled = enabled;
            _moveSlotCooldowns[i].Enabled = enabled;
            _moveSlotScoreBoosts[i].Enabled = enabled;
        }
    }

    private void OnDelete(object? sender, EventArgs e)
    {
        var comp = SelectedCompanion;
        if (comp == null) return;

        var result = MessageBox.Show(this, UiStrings.Get("companion.delete_confirm"),
            UiStrings.Get("companion.delete_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        CompanionLogic.DeleteCompanion(comp);

        int idx = _companionList.SelectedIndex;
        var entry = _entries[idx];

        // Lock the slot when deleting a Pet companion
        if (entry.Source == "Pet" && _playerState != null)
            CompanionLogic.SetSlotUnlocked(_playerState, entry.OriginalIndex, false);

        // Mark entry as empty and refresh
        _entries[idx] = (entry.Companion, entry.Label, entry.Source, entry.OriginalIndex, true);

        // Refresh the whole list to update labels
        var saveData = FindSaveDataRoot();
        if (saveData != null) LoadData(saveData);
        else
        {
            RefreshListEntry();
            OnCompanionSelected(null, EventArgs.Empty);
        }
    }

    private void OnExport(object? sender, EventArgs e)
    {
        var entry = SelectedEntry;
        var comp = entry.Companion;
        if (comp == null || entry.IsEmpty) return;

        var config = ExportConfig.Instance;
        var typeEntry = _typeField.SelectedItem as CompanionEntry;
        var vars = new Dictionary<string, string>
        {
            ["name"] = _nameField.Text ?? "",
            ["species"] = typeEntry?.Species ?? typeEntry?.Id ?? "",
            ["creature_seed"] = _creatureSeedField.Text ?? ""
        };

        using var dialog = new SaveFileDialog
        {
            Filter = ExportConfig.BuildDialogFilter(config.CompanionExt, "Companion files"),
            DefaultExt = config.CompanionExt.TrimStart('.'),
            FileName = ExportConfig.BuildFileName(config.CompanionTemplate, config.CompanionExt, vars)
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                // Get PAC data for this pet if available
                JsonArray? pacSlots = null;
                if (entry.Source == "Pet" && _playerState != null)
                {
                    var pacEntry = GetPetAccessoryCustomisationEntry(entry.OriginalIndex);
                    pacSlots = pacEntry?.GetArray("Data");
                }
                CompanionLogic.ExportCompanion(comp, dialog.FileName, pacSlots);
            }
            catch (Exception ex)
            {
                MessageBox.Show(UiStrings.Format("companion.export_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnImport(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var config = ExportConfig.Instance;
        using var dialog = new OpenFileDialog
        {
            Filter = ExportConfig.BuildImportFilter(config.CompanionExt, "Companion files", ".pet", ".cmp")
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            // Try Pets first, then Eggs
            var pets = _playerState.GetArray("Pets");
            var eggs = _playerState.GetArray("Eggs");
            JsonArray? target = null;
            if (pets != null) target = pets;
            else if (eggs != null) target = eggs;

            if (target == null)
            {
                MessageBox.Show(UiStrings.Get("companion.no_arrays_found"), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Get PAC array for accessory data import
            var pacArray = _playerState.GetArray("PetAccessoryCustomisation");

            int importedIdx;
            bool importedToPets;
            try
            {
                importedIdx = CompanionLogic.ImportCompanion(target, dialog.FileName, pacArray);
                importedToPets = (target == pets);
            }
            catch (InvalidOperationException)
            {
                // First array full, try the other
                JsonArray? fallback = target == pets ? eggs : pets;
                if (fallback != null)
                {
                    importedIdx = CompanionLogic.ImportCompanion(fallback, dialog.FileName, pacArray);
                    importedToPets = (fallback == pets);
                }
                else
                    throw new InvalidOperationException("No empty companion slot available in Pets or Eggs.");
            }

            // Unlock the slot when importing to Pets
            if (importedToPets)
                CompanionLogic.SetSlotUnlocked(_playerState, importedIdx, true);

            // Refresh list
            var saveData = FindSaveDataRoot();
            if (saveData != null) LoadData(saveData);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("companion.import_failed", ex.Message), UiStrings.Get("common.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Returns true if the user actually changed an int battle value from its clamped display.
    /// When the displayed (clamped) value still matches what we'd show for the raw value,
    /// we know the user hasn't interacted, so we preserve the original JSON value.
    /// </summary>
    private bool WasBattleIntChangedByUser(string key, int displayValue, NumericUpDown nud)
    {
        if (_rawBattleIntValues.TryGetValue(key, out int raw))
        {
            int clamped = Math.Clamp(raw, (int)nud.Minimum, (int)nud.Maximum);
            return displayValue != clamped;
        }
        return true; // No raw value recorded – assume user set it
    }

    /// <summary>
    /// Returns true if the user actually changed a decimal battle value from its clamped display.
    /// </summary>
    private bool WasBattleDecimalChangedByUser(string key, decimal displayValue, NumericUpDown nud)
    {
        if (_rawBattleDecimalValues.TryGetValue(key, out decimal raw))
        {
            decimal clamped = Math.Clamp(raw, nud.Minimum, nud.Maximum);
            return displayValue != clamped;
        }
        return true;
    }

    private JsonObject? FindSaveDataRoot()
    {
        if (_playerState?.Parent is JsonObject root) return root;
        return null;
    }

    public void SaveData(JsonObject saveData)
    {
        // No-op: edits are applied directly to the underlying JsonObjects.
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("companion.title");
        _creatureBuilderBtn.Text = UiStrings.Get("companion.creature_builder");
        _deleteBtn.Text = UiStrings.Get("common.delete");

        // Tab pages
        _statsPage.Text = UiStrings.GetOrNull("companion.tab_stats") ?? "Stats";
        _battlePage.Text = UiStrings.GetOrNull("companion.tab_battle") ?? "Battle";

        // Left column labels
        _slotUnlockedLabel.Text = UiStrings.GetOrNull("companion.slot_unlocked") ?? "Slot Unlocked:";
        _speciesLabel.Text = UiStrings.GetOrNull("companion.species") ?? "Species:";
        _nameLabel.Text = UiStrings.GetOrNull("companion.name") ?? "Name:";
        _nameField.PlaceholderText = UiStrings.Get("common.procedural_no_name");
        _typeLabel.Text = UiStrings.GetOrNull("companion.type") ?? "Type:";
        _biomeLabel.Text = UiStrings.GetOrNull("companion.biome") ?? "Biome:";
        _predatorLabel.Text = UiStrings.GetOrNull("companion.predator") ?? "Predator:";
        _hasFurLabel.Text = UiStrings.GetOrNull("companion.has_fur") ?? "Has Fur:";
        _scaleLabel.Text = UiStrings.GetOrNull("companion.scale") ?? "Scale:";
        _trustLabel.Text = UiStrings.GetOrNull("companion.trust") ?? "Trust:";
        _birthTimeLabel.Text = UiStrings.GetOrNull("companion.birth_time") ?? "Birth Time:";
        _lastEggTimeLabel.Text = UiStrings.GetOrNull("companion.last_egg_time") ?? "Last Egg Time:";
        _customSpeciesNameLabel.Text = UiStrings.GetOrNull("companion.custom_species_name") ?? "Custom Species Name:";
        _eggModifiedLabel.Text = UiStrings.GetOrNull("companion.egg_modified") ?? "Egg Modified:";
        _summonedLabel.Text = UiStrings.GetOrNull("companion.summoned") ?? "Summoned:";
        _allowRerollLabel.Text = UiStrings.GetOrNull("companion.allow_reroll") ?? "Allow Reroll:";
        _uaLabel.Text = UiStrings.GetOrNull("companion.ua") ?? "UA:";

        // Right column seed labels
        _creatureSeedLabel.Text = UiStrings.GetOrNull("companion.creature_seed") ?? "Creature Seed:";
        _secondarySeedLabel.Text = UiStrings.GetOrNull("companion.secondary_seed") ?? "Secondary Seed:";
        _speciesSeedLabel.Text = UiStrings.GetOrNull("companion.species_seed") ?? "Species Seed:";
        _genusSeedLabel.Text = UiStrings.GetOrNull("companion.genus_seed") ?? "Genus Seed:";
        _boneScaleSeedLabel.Text = UiStrings.GetOrNull("companion.bone_scale_seed") ?? "Bone Scale Seed:";
        _colourBaseSeedLabel.Text = UiStrings.GetOrNull("companion.colour_base_seed") ?? "Colour Base Seed:";

        // Right column trait & mood labels
        _helpfulnessLabel.Text = UiStrings.GetOrNull("companion.helpfulness") ?? "Helpfulness:";
        _aggressionLabel.Text = UiStrings.GetOrNull("companion.aggression") ?? "Aggression:";
        _independenceLabel.Text = UiStrings.GetOrNull("companion.independence") ?? "Independence:";
        _hungryLabel.Text = UiStrings.GetOrNull("companion.hungry") ?? "Hungry:";
        _lonelyLabel.Text = UiStrings.GetOrNull("companion.lonely") ?? "Lonely:";
        _trustIncreaseLabel.Text = UiStrings.GetOrNull("companion.trust_increase") ?? "Trust Increase:";
        _trustDecreaseLabel.Text = UiStrings.GetOrNull("companion.trust_decrease") ?? "Trust Decrease:";

        // Descriptors heading
        _descriptorsHeading.Text = UiStrings.GetOrNull("companion.descriptors") ?? "Descriptors (Parts)";

        // Button labels
        _exportCompanionBtn.Text = UiStrings.Get("common.export");
        _importCompanionBtn.Text = UiStrings.Get("common.import");

        // Seed "Gen" buttons
        foreach (var btn in _seedGenButtons)
            btn.Text = UiStrings.Get("companion.gen");

        // Regen Descriptor ID button
        if (_regenDescriptorBtn != null)
            _regenDescriptorBtn.Text = UiStrings.Get("companion.regen_descriptor_id");

        // Accessory Customisation section
        _accessoryHeading.Text = UiStrings.GetOrNull("companion.accessory_customisation") ?? "Accessory Customisation";
        _accessorySlotLabels[0].Text = UiStrings.GetOrNull("companion.accessory_slot_right") ?? "Right:";
        _accessorySlotLabels[1].Text = UiStrings.GetOrNull("companion.accessory_slot_left") ?? "Left:";
        _accessorySlotLabels[2].Text = UiStrings.GetOrNull("companion.accessory_slot_chest") ?? "Chest:";
        for (int i = 0; i < 3; i++)
        {
            _accessoryResetBtns[i].Text = UiStrings.GetOrNull("companion.accessory_reset") ?? "Reset";
            _accessoryScaleLabels[i].Text = UiStrings.GetOrNull("companion.accessory_scale") ?? "Scale:";
        }

        // Battle tab labels
        _battleAffinityLabel.Text = UiStrings.GetOrNull("companion.battle_affinity") ?? "Affinity:";
        _battleOverrideClassesLabel.Text = UiStrings.GetOrNull("companion.battle_stat_overrides") ?? "Stat Class Overrides";
        _battleOverrideCheck.Text = UiStrings.GetOrNull("companion.battle_override_classes") ?? "Override Pet Classes";
        _battleHealthClassLabel.Text = UiStrings.GetOrNull("companion.battle_health") ?? "Health:";
        _battleAgilityClassLabel.Text = UiStrings.GetOrNull("companion.battle_agility") ?? "Agility:";
        _battleCombatClassLabel.Text = UiStrings.GetOrNull("companion.battle_combat_effectiveness") ?? "Combat Effectiveness:";
        _battleAverageClassLabel.Text = UiStrings.GetOrNull("companion.battle_average_class") ?? "Average Class:";
        _battleTreatsHeadingLabel.Text = UiStrings.GetOrNull("companion.battle_treats_heading") ?? "Gene Edits";
        _battleTreatHealthLabel.Text = UiStrings.GetOrNull("companion.battle_treats_health") ?? "Health:";
        _battleTreatAgilityLabel.Text = UiStrings.GetOrNull("companion.battle_treats_agility") ?? "Agility:";
        _battleTreatCombatLabel.Text = UiStrings.GetOrNull("companion.battle_treats_combat") ?? "Combat:";
        _battleGenesLevelLabel.Text = UiStrings.GetOrNull("companion.battle_genes_level") ?? "Genes Improved / Level:";
        _battleGenesAvailableLabel.Text = UiStrings.GetOrNull("companion.battle_genes_available") ?? "Gene Edits Available:";
        _battleMutationProgressLabel.Text = UiStrings.GetOrNull("companion.battle_mutation_progress") ?? "Mutation Progress:";
        _battleVictoriesLabel.Text = UiStrings.GetOrNull("companion.battle_victories") ?? "Holo-Arena Victories:";
        _battleMoveListLabel.Text = UiStrings.GetOrNull("companion.battle_move_list") ?? "Move List";

        for (int i = 0; i < 5; i++)
        {
            _moveSlotLabels[i].Text = UiStrings.Format("companion.battle_move_slot", i + 1);
            _moveSlotCooldownLabels[i].Text = UiStrings.GetOrNull("companion.battle_cooldown") ?? "Cooldown:";
            _moveSlotScoreBoostLabels[i].Text = UiStrings.GetOrNull("companion.battle_score_boost") ?? "Score Boost:";
        }
    }
}
