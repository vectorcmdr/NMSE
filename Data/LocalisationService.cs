using System.Text.Json;
using NMSE.Core;

namespace NMSE.Data;

/// <summary>
/// Provides localised string lookups from per-language JSON files produced by the extractor.
/// Each JSON file (e.g. lang/ja-JP.json) is a flat key-value dictionary mapping
/// localisation keys (like "UI_FUEL_1_NAME") to translated strings.
///
/// The service loads one language at a time. When a language is active, GameItem
/// display properties (Name, Description, etc.) can be resolved via their _LocStr
/// keys against the loaded dictionary.
///
/// All internal editor logic always runs against the default English values from
/// the DB JSON files. Localised values are used for display only.
/// </summary>
public class LocalisationService
{
    /// <summary>
    /// Maps NMS internal language names to IETF BCP 47 tags.
    /// Matches ExtractorConfig.SupportedLanguages.
    /// </summary>
    public static readonly Dictionary<string, string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["English"]              = "en-GB",
        ["French"]               = "fr-FR",
        ["Italian"]              = "it-IT",
        ["German"]               = "de-DE",
        ["Spanish"]              = "es-ES",
        ["Russian"]              = "ru-RU",
        ["Polish"]               = "pl-PL",
        ["Dutch"]                = "nl-NL",
        ["Portuguese"]           = "pt-PT",
        ["LatinAmericanSpanish"] = "es-419",
        ["BrazilianPortuguese"]  = "pt-BR",
        ["SimplifiedChinese"]    = "zh-CN",
        ["TraditionalChinese"]   = "zh-TW",
        ["Korean"]               = "ko-KR",
        ["Japanese"]             = "ja-JP",
        ["USEnglish"]            = "en-US",
    };

    private Dictionary<string, string>? _translations;
    private string? _activeTag;
    private string? _langDir;

    /// <summary>Currently active BCP 47 language tag, or null if using default English from DB.</summary>
    public string? ActiveLanguageTag => _activeTag;

    /// <summary>Whether a non-default language is currently active.</summary>
    public bool IsActive => _translations != null && _activeTag != null;

    /// <summary>
    /// Sets the lang/ directory path. Must be called before LoadLanguage.
    /// </summary>
    public void SetLangDirectory(string langDir)
    {
        _langDir = langDir;
    }

    /// <summary>
    /// Loads the specified language. Pass null or empty to revert to default (English from DB).
    /// Returns true if the language was loaded successfully.
    /// </summary>
    public bool LoadLanguage(string? bcp47Tag)
    {
        if (string.IsNullOrEmpty(bcp47Tag))
        {
            _translations = null;
            _activeTag = null;
            return true;
        }

        if (_langDir == null)
            return false;

        string jsonPath = Path.Combine(_langDir, $"{bcp47Tag}.json");
        if (!File.Exists(jsonPath))
            return false;

        try
        {
            string json = File.ReadAllText(jsonPath);
            _translations = JsonSerializer.Deserialize(json, AppJsonContext.Default.DictionaryStringString);
            _activeTag = bcp47Tag;
            return _translations != null && _translations.Count > 0;
        }
        catch
        {
            _translations = null;
            _activeTag = null;
            return false;
        }
    }

    /// <summary>
    /// Looks up a localisation key in the active language dictionary.
    /// Returns the localised string, or null if the key is not found or no language is active.
    /// </summary>
    public string? Lookup(string? locKey)
    {
        if (string.IsNullOrEmpty(locKey) || _translations == null)
            return null;

        return _translations.TryGetValue(locKey, out string? value) ? value : null;
    }

    /// <summary>
    /// Returns the localised name for a GameItem, falling back to the item's default Name.
    /// Tries {NameLocStr}, then {NameLocStr}_NAME, then {NameLocStr}1_NAME as fallbacks
    /// for upgrade modules whose loc keys use numbered suffixes.
    /// </summary>
    public string GetName(GameItem item)
    {
        if (_translations == null || string.IsNullOrEmpty(item.NameLocStr))
            return item.Name;

        try
        {
            return Lookup(item.NameLocStr)
                ?? Lookup(item.NameLocStr + "_NAME")
                ?? Lookup(item.NameLocStr + "1_NAME")
                ?? item.Name;
        }
        catch
        {
            return item.Name;
        }
    }

    /// <summary>
    /// Returns the localised description for a GameItem, falling back to the item's default Description.
    /// </summary>
    public string GetDescription(GameItem item)
    {
        if (_translations == null || string.IsNullOrEmpty(item.DescriptionLocStr))
            return item.Description;

        try
        {
            return Lookup(item.DescriptionLocStr) ?? item.Description;
        }
        catch
        {
            return item.Description;
        }
    }

    /// <summary>
    /// Returns the localised subtitle for a GameItem, falling back to the item's default Subtitle.
    /// </summary>
    public string GetSubtitle(GameItem item)
    {
        if (_translations == null || string.IsNullOrEmpty(item.SubtitleLocStr))
            return item.Subtitle;

        try
        {
            return Lookup(item.SubtitleLocStr) ?? item.Subtitle;
        }
        catch
        {
            return item.Subtitle;
        }
    }
}
