using NMSE.Core;
using NMSE.Data;

namespace NMSE.UI.Panels;

/// <summary>
/// Panel for configuring custom export file extensions and naming templates.
/// Edits the <see cref="ExportConfig"/> singleton and persists to a JSON file.
/// </summary>
public partial class ExportConfigPanel : UserControl
{
    /// <summary>Path to persist export config. Set by MainForm before loading.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string? ConfigFilePath { get; set; }

    public ExportConfigPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Loads the current ExportConfig values into the UI fields.
    /// </summary>
    public void LoadConfig()
    {
        var cfg = ExportConfig.Instance;

        // Extensions
        SetField(_extFields, "Exosuit", cfg.ExosuitExt);
        SetField(_extFields, "Multi-tool", cfg.MultitoolExt);
        SetField(_extFields, "Starship", cfg.StarshipExt);
        SetField(_extFields, "Corvette", cfg.CorvetteExt);
        SetField(_extFields, "Corvette Snapshot", cfg.CorvetteSnapshotExt);
        SetField(_extFields, "Starship Cargo", cfg.StarshipCargoExt);
        SetField(_extFields, "Starship Tech", cfg.StarshipTechExt);
        SetField(_extFields, "Freighter", cfg.FreighterExt);
        SetField(_extFields, "Freighter Cargo", cfg.FreighterCargoExt);
        SetField(_extFields, "Freighter Tech", cfg.FreighterTechExt);
        SetField(_extFields, "Frigate", cfg.FrigateExt);
        SetField(_extFields, "Squadron", cfg.SquadronExt);
        SetField(_extFields, "Exocraft", cfg.ExocraftExt);
        SetField(_extFields, "Exocraft Cargo", cfg.ExocraftCargoExt);
        SetField(_extFields, "Exocraft Tech", cfg.ExocraftTechExt);
        SetField(_extFields, "Companion", cfg.CompanionExt);
        SetField(_extFields, "Base", cfg.BaseExt);
        SetField(_extFields, "Chest", cfg.ChestExt);
        SetField(_extFields, "Storage", cfg.StorageExt);
        SetField(_extFields, "Discovery", cfg.DiscoveryExt);
        SetField(_extFields, "Settlement", cfg.SettlementExt);
        SetField(_extFields, "ByteBeat", cfg.ByteBeatExt);

        // Templates
        SetField(_templateFields, "Exosuit Cargo", cfg.ExosuitCargoTemplate);
        SetField(_templateFields, "Exosuit Tech", cfg.ExosuitTechTemplate);
        SetField(_templateFields, "Multi-tool", cfg.MultitoolTemplate);
        SetField(_templateFields, "Starship", cfg.StarshipTemplate);
        SetField(_templateFields, "Corvette", cfg.CorvetteTemplate);
        SetField(_templateFields, "Corvette Snapshot", cfg.CorvetteSnapshotTemplate);
        SetField(_templateFields, "Starship Cargo", cfg.StarshipCargoTemplate);
        SetField(_templateFields, "Starship Tech", cfg.StarshipTechTemplate);
        SetField(_templateFields, "Freighter", cfg.FreighterTemplate);
        SetField(_templateFields, "Freighter Cargo", cfg.FreighterCargoTemplate);
        SetField(_templateFields, "Freighter Tech", cfg.FreighterTechTemplate);
        SetField(_templateFields, "Frigate", cfg.FrigateTemplate);
        SetField(_templateFields, "Squadron", cfg.SquadronTemplate);
        SetField(_templateFields, "Exocraft", cfg.ExocraftTemplate);
        SetField(_templateFields, "Exocraft Cargo", cfg.ExocraftCargoTemplate);
        SetField(_templateFields, "Exocraft Tech", cfg.ExocraftTechTemplate);
        SetField(_templateFields, "Companion", cfg.CompanionTemplate);
        SetField(_templateFields, "Base", cfg.BaseTemplate);
        SetField(_templateFields, "Chest", cfg.ChestTemplate);
        SetField(_templateFields, "Storage", cfg.StorageTemplate);
        SetField(_templateFields, "Discovery", cfg.DiscoveryTemplate);
        SetField(_templateFields, "Settlement", cfg.SettlementTemplate);
        SetField(_templateFields, "ByteBeat", cfg.ByteBeatTemplate);
    }

    /// <summary>
    /// Applies UI field values back to the ExportConfig singleton.
    /// </summary>
    public void ApplyConfig()
    {
        var cfg = ExportConfig.Instance;

        // Extensions
        cfg.ExosuitExt = GetField(_extFields, "Exosuit", cfg.ExosuitExt);
        cfg.MultitoolExt = GetField(_extFields, "Multi-tool", cfg.MultitoolExt);
        cfg.StarshipExt = GetField(_extFields, "Starship", cfg.StarshipExt);
        cfg.CorvetteExt = GetField(_extFields, "Corvette", cfg.CorvetteExt);
        cfg.CorvetteSnapshotExt = GetField(_extFields, "Corvette Snapshot", cfg.CorvetteSnapshotExt);
        cfg.StarshipCargoExt = GetField(_extFields, "Starship Cargo", cfg.StarshipCargoExt);
        cfg.StarshipTechExt = GetField(_extFields, "Starship Tech", cfg.StarshipTechExt);
        cfg.FreighterExt = GetField(_extFields, "Freighter", cfg.FreighterExt);
        cfg.FreighterCargoExt = GetField(_extFields, "Freighter Cargo", cfg.FreighterCargoExt);
        cfg.FreighterTechExt = GetField(_extFields, "Freighter Tech", cfg.FreighterTechExt);
        cfg.FrigateExt = GetField(_extFields, "Frigate", cfg.FrigateExt);
        cfg.SquadronExt = GetField(_extFields, "Squadron", cfg.SquadronExt);
        cfg.ExocraftExt = GetField(_extFields, "Exocraft", cfg.ExocraftExt);
        cfg.ExocraftCargoExt = GetField(_extFields, "Exocraft Cargo", cfg.ExocraftCargoExt);
        cfg.ExocraftTechExt = GetField(_extFields, "Exocraft Tech", cfg.ExocraftTechExt);
        cfg.CompanionExt = GetField(_extFields, "Companion", cfg.CompanionExt);
        cfg.BaseExt = GetField(_extFields, "Base", cfg.BaseExt);
        cfg.ChestExt = GetField(_extFields, "Chest", cfg.ChestExt);
        cfg.StorageExt = GetField(_extFields, "Storage", cfg.StorageExt);
        cfg.DiscoveryExt = GetField(_extFields, "Discovery", cfg.DiscoveryExt);
        cfg.SettlementExt = GetField(_extFields, "Settlement", cfg.SettlementExt);
        cfg.ByteBeatExt = GetField(_extFields, "ByteBeat", cfg.ByteBeatExt);

        // Templates
        cfg.ExosuitCargoTemplate = GetField(_templateFields, "Exosuit Cargo", cfg.ExosuitCargoTemplate);
        cfg.ExosuitTechTemplate = GetField(_templateFields, "Exosuit Tech", cfg.ExosuitTechTemplate);
        cfg.MultitoolTemplate = GetField(_templateFields, "Multi-tool", cfg.MultitoolTemplate);
        cfg.StarshipTemplate = GetField(_templateFields, "Starship", cfg.StarshipTemplate);
        cfg.CorvetteTemplate = GetField(_templateFields, "Corvette", cfg.CorvetteTemplate);
        cfg.CorvetteSnapshotTemplate = GetField(_templateFields, "Corvette Snapshot", cfg.CorvetteSnapshotTemplate);
        cfg.StarshipCargoTemplate = GetField(_templateFields, "Starship Cargo", cfg.StarshipCargoTemplate);
        cfg.StarshipTechTemplate = GetField(_templateFields, "Starship Tech", cfg.StarshipTechTemplate);
        cfg.FreighterTemplate = GetField(_templateFields, "Freighter", cfg.FreighterTemplate);
        cfg.FreighterCargoTemplate = GetField(_templateFields, "Freighter Cargo", cfg.FreighterCargoTemplate);
        cfg.FreighterTechTemplate = GetField(_templateFields, "Freighter Tech", cfg.FreighterTechTemplate);
        cfg.FrigateTemplate = GetField(_templateFields, "Frigate", cfg.FrigateTemplate);
        cfg.SquadronTemplate = GetField(_templateFields, "Squadron", cfg.SquadronTemplate);
        cfg.ExocraftTemplate = GetField(_templateFields, "Exocraft", cfg.ExocraftTemplate);
        cfg.ExocraftCargoTemplate = GetField(_templateFields, "Exocraft Cargo", cfg.ExocraftCargoTemplate);
        cfg.ExocraftTechTemplate = GetField(_templateFields, "Exocraft Tech", cfg.ExocraftTechTemplate);
        cfg.CompanionTemplate = GetField(_templateFields, "Companion", cfg.CompanionTemplate);
        cfg.BaseTemplate = GetField(_templateFields, "Base", cfg.BaseTemplate);
        cfg.ChestTemplate = GetField(_templateFields, "Chest", cfg.ChestTemplate);
        cfg.StorageTemplate = GetField(_templateFields, "Storage", cfg.StorageTemplate);
        cfg.DiscoveryTemplate = GetField(_templateFields, "Discovery", cfg.DiscoveryTemplate);
        cfg.SettlementTemplate = GetField(_templateFields, "Settlement", cfg.SettlementTemplate);
        cfg.ByteBeatTemplate = GetField(_templateFields, "ByteBeat", cfg.ByteBeatTemplate);
    }

    // --- Event Handlers --------------------------------------------

    private void OnSave(object? sender, EventArgs e)
    {
        var warnings = ValidateExtensions();
        ApplyConfig();
        if (ConfigFilePath != null)
        {
            try
            {
                ExportConfig.Instance.SaveToFile(ConfigFilePath);
                _statusLabel.ForeColor = Color.Green;
                _statusLabel.Text = warnings.Count > 0
                    ? $"Settings saved. {string.Join(" ", warnings)}"
                    : "Settings saved.";
            }
            catch (Exception ex)
            {
                _statusLabel.ForeColor = Color.Red;
                _statusLabel.Text = $"Save failed: {ex.Message}";
            }
        }
        else
        {
            _statusLabel.ForeColor = Color.Orange;
            _statusLabel.Text = UiStrings.Get("export_config.status_applied_no_path");
        }
    }

    private void OnReset(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(this, 
            UiStrings.Get("export_config.reset_confirm"),
            UiStrings.Get("export_config.reset_title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            ExportConfig.SetInstance(new ExportConfig());
            LoadConfig();
            _statusLabel.ForeColor = Color.Green;
            _statusLabel.Text = UiStrings.Get("export_config.status_reset");
        }
    }

    // --- Helpers ---------------------------------------------------

    private static void SetField(Dictionary<string, TextBox> fields, string key, string value)
    {
        if (fields.TryGetValue(key, out var tb))
            tb.Text = value;
    }

    private static string GetField(Dictionary<string, TextBox> fields, string key, string fallback)
    {
        if (fields.TryGetValue(key, out var tb) && !string.IsNullOrWhiteSpace(tb.Text))
            return tb.Text.Trim();
        return fallback;
    }

    /// <summary>
    /// Validates extension fields and ensures they start with a dot.
    /// Returns a list of warning messages for any issues found.
    /// </summary>
    private List<string> ValidateExtensions()
    {
        var warnings = new List<string>();
        foreach (var (label, tb) in _extFields)
        {
            string val = tb.Text.Trim();
            if (string.IsNullOrWhiteSpace(val)) continue;
            if (!val.StartsWith('.'))
            {
                tb.Text = "." + val;
                warnings.Add($"{label} extension was missing leading dot (auto-corrected).");
            }
        }
        return warnings;
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("export_config.title");
        _headerLabel.Text = UiStrings.Get("export_config.header");
        _saveBtn.Text = UiStrings.Get("export_config.save_settings");
        _resetBtn.Text = UiStrings.Get("export_config.reset_defaults");

        if (_sectionTabs.TabPages.Count >= 3)
        {
            _sectionTabs.TabPages[0].Text = UiStrings.Get("export_config.tab_extensions");
            _sectionTabs.TabPages[1].Text = UiStrings.Get("export_config.tab_templates");
            _sectionTabs.TabPages[2].Text = UiStrings.Get("export_config.tab_help");
        }

        // Template labels
        string[] templateKeys =
        {
            "export_config.template_exosuit_cargo", "export_config.template_exosuit_tech",
            "export_config.template_multitool", "export_config.template_starship",
            "export_config.template_corvette", "export_config.template_corvette_snapshot",
            "export_config.template_starship_cargo", "export_config.template_starship_tech",
            "export_config.template_freighter", "export_config.template_freighter_cargo",
            "export_config.template_freighter_tech", "export_config.template_frigate",
            "export_config.template_squadron", "export_config.template_exocraft",
            "export_config.template_exocraft_cargo", "export_config.template_exocraft_tech",
            "export_config.template_companion", "export_config.template_base",
            "export_config.template_chest", "export_config.template_storage",
            "export_config.template_discovery", "export_config.template_settlement",
            "export_config.template_bytebeat"
        };
        if (_templateLabels != null)
        {
            for (int i = 0; i < _templateLabels.Length && i < templateKeys.Length; i++)
                _templateLabels[i].Text = UiStrings.Get(templateKeys[i]);
        }

        // Help tab
        if (_helpHeadingLabel != null)
            _helpHeadingLabel.Text = UiStrings.Get("export_config.help_heading");

        if (_helpTextBox != null)
        {
            var vars = new[]
            {
                "export_config.help_var_player_name", "export_config.help_var_ship_name",
                "export_config.help_var_multitool_name", "export_config.help_var_type",
                "export_config.help_var_class", "export_config.help_var_race",
                "export_config.help_var_rank", "export_config.help_var_seed",
                "export_config.help_var_name", "export_config.help_var_species",
                "export_config.help_var_creature_seed", "export_config.help_var_vehicle_name",
                "export_config.help_var_vehicle_type", "export_config.help_var_base_name",
                "export_config.help_var_chest_number", "export_config.help_var_timestamp",
                "export_config.help_var_frigate_name", "export_config.help_var_freighter_name",
                "export_config.help_var_settlement_name"
            };
            var sb = new System.Text.StringBuilder();
            sb.AppendLine();
            foreach (var vk in vars)
                sb.AppendLine("    " + UiStrings.Get(vk));
            sb.AppendLine();
            sb.AppendLine(UiStrings.Get("export_config.help_extensions_heading"));
            sb.AppendLine();
            sb.Append(UiStrings.Get("export_config.help_extensions_info"));
            _helpTextBox.Text = sb.ToString();
        }
    }
}
