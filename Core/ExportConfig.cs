namespace NMSE.Core;

using System.Text.Json;
using NMSE.Models;

/// <summary>
/// Configuration for import/export file extensions and naming templates.
/// Supports user customizable defaults that can be saved/loaded from a JSON settings file.
/// </summary>
public class ExportConfig
{
    // File Extensions
    /// <summary>File extension for exosuit exports.</summary>
    public string ExosuitExt { get; set; } = ".nmssuit";
    /// <summary>File extension for multitool exports.</summary>
    public string MultitoolExt { get; set; } = ".nmstool";
    /// <summary>File extension for starship exports.</summary>
    public string StarshipExt { get; set; } = ".nmsship";
    /// <summary>File extension for corvette exports.</summary>
    public string CorvetteExt { get; set; } = ".nmscorv";
    /// <summary>File extension for corvette snapshot exports.</summary>
    public string CorvetteSnapshotExt { get; set; } = ".nmssnap";
    /// <summary>File extension for starship cargo exports.</summary>
    public string StarshipCargoExt { get; set; } = ".nmssc";
    /// <summary>File extension for starship tech exports.</summary>
    public string StarshipTechExt { get; set; } = ".nmsst";
    /// <summary>File extension for freighter exports.</summary>
    public string FreighterExt { get; set; } = ".nmsfreight";
    /// <summary>File extension for freighter cargo exports.</summary>
    public string FreighterCargoExt { get; set; } = ".nmsfc";
    /// <summary>File extension for freighter tech exports.</summary>
    public string FreighterTechExt { get; set; } = ".nmsft";
    /// <summary>File extension for frigate exports.</summary>
    public string FrigateExt { get; set; } = ".nmsfrig";
    /// <summary>File extension for squadron exports.</summary>
    public string SquadronExt { get; set; } = ".nmssquad";
    /// <summary>File extension for exocraft exports.</summary>
    public string ExocraftExt { get; set; } = ".nmscraft";
    /// <summary>File extension for exocraft cargo exports.</summary>
    public string ExocraftCargoExt { get; set; } = ".nmscc";
    /// <summary>File extension for exocraft tech exports.</summary>
    public string ExocraftTechExt { get; set; } = ".nmsct";
    /// <summary>File extension for companion exports.</summary>
    public string CompanionExt { get; set; } = ".nmspet";
    /// <summary>File extension for base exports.</summary>
    public string BaseExt { get; set; } = ".nmsbase";
    /// <summary>File extension for chest exports.</summary>
    public string ChestExt { get; set; } = ".nmschest";
    /// <summary>File extension for storage container exports.</summary>
    public string StorageExt { get; set; } = ".nmsstore";
    /// <summary>File extension for discovery exports.</summary>
    public string DiscoveryExt { get; set; } = ".nmsdiscover";
    /// <summary>File extension for settlement exports.</summary>
    public string SettlementExt { get; set; } = ".nmssettle";
    /// <summary>File extension for ByteBeat song exports.</summary>
    public string ByteBeatExt { get; set; } = ".nmssong";

    // Naming Templates
    // Template variables:
    //   {player_name}    – player name
    //   {ship_name}      – ship / freighter name
    //   {multitool_name} – multitool name
    //   {type}           – type display name
    //   {class}          – class letter (S/A/B/C)
    //   {race}           – NPC race
    //   {rank}           – pilot rank
    //   {seed}           – seed value
    //   {name}           – generic name
    //   {species}        – companion species
    //   {creature_seed}  – companion creature seed
    //   {vehicle_name}   – exocraft name
    //   {vehicle_type}   – exocraft type
    //   {base_name}      – base name
    //   {chest_number}   – chest slot number
    //   {timestamp}      – epoch timestamp
    //   {frigate_name}   – frigate name
    //   {settlement_name} – settlement name

    /// <summary>Naming template for exosuit cargo exports.</summary>
    public string ExosuitCargoTemplate { get; set; } = "{player_name}_cargo";
    /// <summary>Naming template for exosuit tech exports.</summary>
    public string ExosuitTechTemplate { get; set; } = "{player_name}_tech";
    /// <summary>Naming template for multitool exports.</summary>
    public string MultitoolTemplate { get; set; } = "{multitool_name}_{type}_{class}";
    /// <summary>Naming template for starship exports.</summary>
    public string StarshipTemplate { get; set; } = "{ship_name}_{type}_{class}";
    /// <summary>Naming template for corvette exports.</summary>
    public string CorvetteTemplate { get; set; } = "{ship_name}_{type}_{class}";
    /// <summary>Naming template for corvette snapshot exports.</summary>
    public string CorvetteSnapshotTemplate { get; set; } = "{ship_name}_{type}_{class}";
    /// <summary>Naming template for starship cargo exports.</summary>
    public string StarshipCargoTemplate { get; set; } = "{ship_name}_cargo";
    /// <summary>Naming template for starship tech exports.</summary>
    public string StarshipTechTemplate { get; set; } = "{ship_name}_tech";
    /// <summary>Naming template for freighter exports.</summary>
    public string FreighterTemplate { get; set; } = "{freighter_name}_{type}_{class}";
    /// <summary>Naming template for freighter cargo exports.</summary>
    public string FreighterCargoTemplate { get; set; } = "{freighter_name}_cargo";
    /// <summary>Naming template for freighter tech exports.</summary>
    public string FreighterTechTemplate { get; set; } = "{freighter_name}_tech";
    /// <summary>Naming template for frigate exports.</summary>
    public string FrigateTemplate { get; set; } = "{frigate_name}_{type}_{class}";
    /// <summary>Naming template for squadron exports.</summary>
    public string SquadronTemplate { get; set; } = "{race}_{type}_{rank}_{seed}";
    /// <summary>Naming template for exocraft exports.</summary>
    public string ExocraftTemplate { get; set; } = "{vehicle_name}_{vehicle_type}";
    /// <summary>Naming template for exocraft cargo exports.</summary>
    public string ExocraftCargoTemplate { get; set; } = "{vehicle_name}_{vehicle_type}_cargo";
    /// <summary>Naming template for exocraft tech exports.</summary>
    public string ExocraftTechTemplate { get; set; } = "{vehicle_name}_{vehicle_type}_tech";
    /// <summary>Naming template for companion exports.</summary>
    public string CompanionTemplate { get; set; } = "{name}_{species}_{creature_seed}";
    /// <summary>Naming template for base exports.</summary>
    public string BaseTemplate { get; set; } = "{base_name}";
    /// <summary>Naming template for chest exports.</summary>
    public string ChestTemplate { get; set; } = "{chest_number}";
    /// <summary>Naming template for storage container exports.</summary>
    public string StorageTemplate { get; set; } = "{name}";
    /// <summary>Naming template for discovery exports.</summary>
    public string DiscoveryTemplate { get; set; } = "{name}";
    /// <summary>Naming template for settlement exports.</summary>
    public string SettlementTemplate { get; set; } = "{settlement_name}_{seed}";
    /// <summary>Naming template for ByteBeat song exports.</summary>
    public string ByteBeatTemplate { get; set; } = "{name}_{timestamp}";

    // --- Singleton -------------------------------------------------
    private static ExportConfig? _instance;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the current export configuration instance. Thread-safe singleton.
    /// </summary>
    public static ExportConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ExportConfig();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Replaces the singleton instance (e.g. after loading from file).
    /// </summary>
    internal static void SetInstance(ExportConfig config)
    {
        lock (_lock) { _instance = config; }
    }

    // --- Persistence -----------------------------------------------

    private static readonly Dictionary<string, Action<ExportConfig, string>> PropertySetters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ExosuitExt"] = (c, v) => c.ExosuitExt = v,
        ["MultitoolExt"] = (c, v) => c.MultitoolExt = v,
        ["StarshipExt"] = (c, v) => c.StarshipExt = v,
        ["CorvetteExt"] = (c, v) => c.CorvetteExt = v,
        ["CorvetteSnapshotExt"] = (c, v) => c.CorvetteSnapshotExt = v,
        ["StarshipCargoExt"] = (c, v) => c.StarshipCargoExt = v,
        ["StarshipTechExt"] = (c, v) => c.StarshipTechExt = v,
        ["FreighterExt"] = (c, v) => c.FreighterExt = v,
        ["FreighterCargoExt"] = (c, v) => c.FreighterCargoExt = v,
        ["FreighterTechExt"] = (c, v) => c.FreighterTechExt = v,
        ["FrigateExt"] = (c, v) => c.FrigateExt = v,
        ["SquadronExt"] = (c, v) => c.SquadronExt = v,
        ["ExocraftExt"] = (c, v) => c.ExocraftExt = v,
        ["ExocraftCargoExt"] = (c, v) => c.ExocraftCargoExt = v,
        ["ExocraftTechExt"] = (c, v) => c.ExocraftTechExt = v,
        ["CompanionExt"] = (c, v) => c.CompanionExt = v,
        ["BaseExt"] = (c, v) => c.BaseExt = v,
        ["ChestExt"] = (c, v) => c.ChestExt = v,
        ["StorageExt"] = (c, v) => c.StorageExt = v,
        ["DiscoveryExt"] = (c, v) => c.DiscoveryExt = v,
        ["SettlementExt"] = (c, v) => c.SettlementExt = v,
        ["ByteBeatExt"] = (c, v) => c.ByteBeatExt = v,
        ["ExosuitCargoTemplate"] = (c, v) => c.ExosuitCargoTemplate = v,
        ["ExosuitTechTemplate"] = (c, v) => c.ExosuitTechTemplate = v,
        ["MultitoolTemplate"] = (c, v) => c.MultitoolTemplate = v,
        ["StarshipTemplate"] = (c, v) => c.StarshipTemplate = v,
        ["CorvetteTemplate"] = (c, v) => c.CorvetteTemplate = v,
        ["CorvetteSnapshotTemplate"] = (c, v) => c.CorvetteSnapshotTemplate = v,
        ["StarshipCargoTemplate"] = (c, v) => c.StarshipCargoTemplate = v,
        ["StarshipTechTemplate"] = (c, v) => c.StarshipTechTemplate = v,
        ["FreighterTemplate"] = (c, v) => c.FreighterTemplate = v,
        ["FreighterCargoTemplate"] = (c, v) => c.FreighterCargoTemplate = v,
        ["FreighterTechTemplate"] = (c, v) => c.FreighterTechTemplate = v,
        ["FrigateTemplate"] = (c, v) => c.FrigateTemplate = v,
        ["SquadronTemplate"] = (c, v) => c.SquadronTemplate = v,
        ["ExocraftTemplate"] = (c, v) => c.ExocraftTemplate = v,
        ["ExocraftCargoTemplate"] = (c, v) => c.ExocraftCargoTemplate = v,
        ["ExocraftTechTemplate"] = (c, v) => c.ExocraftTechTemplate = v,
        ["CompanionTemplate"] = (c, v) => c.CompanionTemplate = v,
        ["BaseTemplate"] = (c, v) => c.BaseTemplate = v,
        ["ChestTemplate"] = (c, v) => c.ChestTemplate = v,
        ["StorageTemplate"] = (c, v) => c.StorageTemplate = v,
        ["DiscoveryTemplate"] = (c, v) => c.DiscoveryTemplate = v,
        ["SettlementTemplate"] = (c, v) => c.SettlementTemplate = v,
        ["ByteBeatTemplate"] = (c, v) => c.ByteBeatTemplate = v,
    };

    private static readonly Dictionary<string, Func<ExportConfig, string>> PropertyGetters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ExosuitExt"] = c => c.ExosuitExt,
        ["MultitoolExt"] = c => c.MultitoolExt,
        ["StarshipExt"] = c => c.StarshipExt,
        ["CorvetteExt"] = c => c.CorvetteExt,
        ["CorvetteSnapshotExt"] = c => c.CorvetteSnapshotExt,
        ["StarshipCargoExt"] = c => c.StarshipCargoExt,
        ["StarshipTechExt"] = c => c.StarshipTechExt,
        ["FreighterExt"] = c => c.FreighterExt,
        ["FreighterCargoExt"] = c => c.FreighterCargoExt,
        ["FreighterTechExt"] = c => c.FreighterTechExt,
        ["FrigateExt"] = c => c.FrigateExt,
        ["SquadronExt"] = c => c.SquadronExt,
        ["ExocraftExt"] = c => c.ExocraftExt,
        ["ExocraftCargoExt"] = c => c.ExocraftCargoExt,
        ["ExocraftTechExt"] = c => c.ExocraftTechExt,
        ["CompanionExt"] = c => c.CompanionExt,
        ["BaseExt"] = c => c.BaseExt,
        ["ChestExt"] = c => c.ChestExt,
        ["StorageExt"] = c => c.StorageExt,
        ["DiscoveryExt"] = c => c.DiscoveryExt,
        ["SettlementExt"] = c => c.SettlementExt,
        ["ByteBeatExt"] = c => c.ByteBeatExt,
        ["ExosuitCargoTemplate"] = c => c.ExosuitCargoTemplate,
        ["ExosuitTechTemplate"] = c => c.ExosuitTechTemplate,
        ["MultitoolTemplate"] = c => c.MultitoolTemplate,
        ["StarshipTemplate"] = c => c.StarshipTemplate,
        ["CorvetteTemplate"] = c => c.CorvetteTemplate,
        ["CorvetteSnapshotTemplate"] = c => c.CorvetteSnapshotTemplate,
        ["StarshipCargoTemplate"] = c => c.StarshipCargoTemplate,
        ["StarshipTechTemplate"] = c => c.StarshipTechTemplate,
        ["FreighterTemplate"] = c => c.FreighterTemplate,
        ["FreighterCargoTemplate"] = c => c.FreighterCargoTemplate,
        ["FreighterTechTemplate"] = c => c.FreighterTechTemplate,
        ["FrigateTemplate"] = c => c.FrigateTemplate,
        ["SquadronTemplate"] = c => c.SquadronTemplate,
        ["ExocraftTemplate"] = c => c.ExocraftTemplate,
        ["ExocraftCargoTemplate"] = c => c.ExocraftCargoTemplate,
        ["ExocraftTechTemplate"] = c => c.ExocraftTechTemplate,
        ["CompanionTemplate"] = c => c.CompanionTemplate,
        ["BaseTemplate"] = c => c.BaseTemplate,
        ["ChestTemplate"] = c => c.ChestTemplate,
        ["StorageTemplate"] = c => c.StorageTemplate,
        ["DiscoveryTemplate"] = c => c.DiscoveryTemplate,
        ["SettlementTemplate"] = c => c.SettlementTemplate,
        ["ByteBeatTemplate"] = c => c.ByteBeatTemplate,
    };

    /// <summary>
    /// Saves the current configuration to a JSON file using JsonObject (AOT-safe).
    /// </summary>
    public void SaveToFile(string filePath)
    {
        var obj = new JsonObject();
        foreach (var (key, getter) in PropertyGetters)
            obj.Add(key, getter(this));
        File.WriteAllText(filePath, obj.ToFormattedString());
    }

    /// <summary>
    /// Loads configuration from a JSON file. Falls back to defaults on error.
    /// </summary>
    public static ExportConfig LoadFromFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                var config = new ExportConfig();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String &&
                        PropertySetters.TryGetValue(prop.Name, out var setter))
                    {
                        setter(config, prop.Value.GetString()!);
                    }
                }
                SetInstance(config);
                return config;
            }
        }
        catch { /* fall through to defaults */ }
        var defaultConfig = new ExportConfig();
        SetInstance(defaultConfig);
        return defaultConfig;
    }

    // --- Template Expansion ----------------------------------------

    /// <summary>
    /// Builds a file name from a template and variable values.
    /// Sanitizes the result for filesystem safety.
    /// </summary>
    /// <param name="template">The naming template with {variable} placeholders.</param>
    /// <param name="ext">The file extension (including leading dot).</param>
    /// <param name="vars">Dictionary of variable name -> value replacements.</param>
    /// <returns>A sanitized file name with extension.</returns>
    public static string BuildFileName(string template, string ext, Dictionary<string, string> vars)
    {
        string result = template;
        foreach (var kv in vars)
            result = result.Replace($"{{{kv.Key}}}", kv.Value ?? "", StringComparison.OrdinalIgnoreCase);

        // Sanitize for filesystem
        result = FileNameHelper.SanitizeFileName(result);
        return result + ext;
    }

    /// <summary>
    /// Builds a combined SaveFileDialog filter string that includes the custom extension
    /// plus JSON and all-files as fallbacks.
    /// </summary>
    /// <param name="ext">The custom extension including leading dot (e.g. ".nmsship").</param>
    /// <param name="label">Human-readable label for the extension (e.g. "Ship files").</param>
    /// <returns>A filter string suitable for SaveFileDialog.Filter.</returns>
    public static string BuildDialogFilter(string ext, string label)
    {
        string wildcard = $"*{ext}";
        return $"{label} ({wildcard})|{wildcard}|JSON files (*.json)|*.json|All files (*.*)|*.*";
    }

    /// <summary>
    /// Builds a combined OpenFileDialog filter string that accepts the custom extension,
    /// JSON files, and all files.
    /// </summary>
    public static string BuildOpenFilter(string ext, string label)
    {
        string wildcard = $"*{ext}";
        return $"{label} ({wildcard})|{wildcard}|JSON files (*.json)|*.json|All files (*.*)|*.*";
    }

    /// <summary>
    /// Builds an OpenFileDialog filter that accepts our custom extension plus external tool
    /// extensions (NMSSaveEditor / NomNom). The first filter entry shows all supported types.
    /// </summary>
    /// <param name="ext">Our custom extension including leading dot (e.g. ".nmssc").</param>
    /// <param name="label">Human-readable label (e.g. "Ship cargo inventory").</param>
    /// <param name="externalExts">Additional extensions from external tools (e.g. ".wp0", ".mlt").</param>
    public static string BuildImportFilter(string ext, string label, params string[] externalExts)
    {
        var allExts = new List<string> { $"*{ext}", "*.json" };
        foreach (var e in externalExts)
            allExts.Add($"*{e}");
        string allWildcards = string.Join(";", allExts);
        string ownWildcard = $"*{ext}";
        var parts = new List<string>
        {
            $"All supported ({allWildcards})|{allWildcards}",
            $"{label} ({ownWildcard})|{ownWildcard}",
            "JSON files (*.json)|*.json"
        };
        foreach (var e in externalExts)
        {
            string w = $"*{e}";
            parts.Add($"{e.TrimStart('.')} files ({w})|{w}");
        }
        parts.Add("All files (*.*)|*.*");
        return string.Join("|", parts);
    }
}
