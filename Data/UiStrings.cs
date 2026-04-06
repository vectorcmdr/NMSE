using System.Text.Json;

namespace NMSE.Data;

/// <summary>
/// Provides localised UI string lookups from per-language JSON files.
/// Each JSON file (e.g. <c>Resources/ui/lang/ja-JP.json</c>) is a flat key-value
/// dictionary mapping dot-delimited keys (like <c>"menu.file"</c>) to translated
/// strings.
///
/// <para>
/// The class always keeps an English fallback dictionary loaded so that any missing
/// key in the active language gracefully degrades to the English original. If even
/// the English fallback is missing the key, the raw key string is returned - this
/// makes it easy to spot untranslated strings during development.
/// </para>
///
/// <para>
/// <b>Thread safety:</b> The dictionaries are replaced atomically via reference
/// assignment, so concurrent reads are safe. Callers should not cache dictionary
/// references across language switches.
/// </para>
///
/// <para>
/// <b>Accelerator keys:</b> Translated strings may include <c>&amp;</c> prefixes
/// for WinForms accelerator/mnemonic keys (e.g. <c>"&amp;Datei"</c> for German
/// <c>"&amp;File"</c>). These are preserved as-is in the JSON and applied
/// directly to control <c>.Text</c> properties.
/// </para>
/// </summary>
public static class UiStrings
{
    /// <summary>The active (possibly translated) string dictionary.</summary>
    private static Dictionary<string, string> _strings = new();

    /// <summary>English fallback dictionary, always loaded.</summary>
    private static Dictionary<string, string> _fallback = new();

    /// <summary>Base directory for UI string JSON files (e.g. <c>Resources/ui/lang</c>).</summary>
    private static string? _uiLangDir;

    /// <summary>The BCP 47 tag of the currently loaded UI language, or null if defaulting to English.</summary>
    private static string? _activeTag;

    /// <summary>
    /// Gets the number of translated UI strings in the currently active language.
    /// Returns 0 when English is active (since all strings are the fallback).
    /// </summary>
    public static int TranslatedCount
    {
        get
        {
            // When active language equals fallback, nothing is "translated"
            if (_activeTag == null || ReferenceEquals(_strings, _fallback))
                return 0;
            return _strings.Count;
        }
    }

    /// <summary>
    /// Gets the total number of UI string keys defined in the English fallback.
    /// </summary>
    public static int TotalKeyCount => _fallback.Count;

    /// <summary>
    /// Gets whether a non-English UI language is currently active.
    /// </summary>
    public static bool IsActive => _activeTag != null && !ReferenceEquals(_strings, _fallback);

    /// <summary>
    /// Sets the directory that contains the per-language UI string JSON files.
    /// Must be called before <see cref="Load"/>.
    /// </summary>
    /// <param name="uiLangDir">
    /// Absolute or relative path to the directory containing UI string JSONs
    /// (e.g. <c>Resources/ui/lang</c>).
    /// </param>
    public static void SetDirectory(string uiLangDir)
    {
        _uiLangDir = uiLangDir;
    }

    /// <summary>
    /// Loads the UI string tables for the specified language.
    /// Always loads <c>en-GB.json</c> as the fallback first, then overlays
    /// the requested language on top (if different from <c>en-GB</c>).
    /// </summary>
    /// <param name="bcp47Tag">
    /// BCP 47 language tag (e.g. <c>"ja-JP"</c>). Pass <c>null</c>,
    /// empty, or <c>"en-GB"</c> to use English defaults only.
    /// </param>
    /// <returns>
    /// <c>true</c> if the language was loaded (or English was selected);
    /// <c>false</c> if the requested language file could not be found.
    /// </returns>
    public static bool Load(string? bcp47Tag)
    {
        if (_uiLangDir == null)
            return false;

        // Always (re-)load English fallback
        var fallback = LoadFile(Path.Combine(_uiLangDir, "en-GB.json"));
        if (fallback != null)
            _fallback = fallback;

        // If caller wants English (or no language), just point active at fallback
        if (string.IsNullOrEmpty(bcp47Tag) || bcp47Tag == "en-GB")
        {
            _strings = _fallback;
            _activeTag = null;
            return true;
        }

        // Try to load the requested language
        var translated = LoadFile(Path.Combine(_uiLangDir, $"{bcp47Tag}.json"));
        if (translated == null || translated.Count == 0)
        {
            _strings = _fallback;
            _activeTag = null;
            return false;
        }

        _strings = translated;
        _activeTag = bcp47Tag;
        return true;
    }

    /// <summary>
    /// Returns the localised UI string for the given key.
    /// Falls back to the English value if the key is missing in the active language,
    /// and returns the raw <paramref name="key"/> if even the English fallback is missing.
    /// </summary>
    /// <param name="key">
    /// Dot-delimited string key (e.g. <c>"menu.file"</c>, <c>"tab.player"</c>).
    /// </param>
    /// <returns>The localised string, English fallback, or the raw key if not found.</returns>
    public static string Get(string key)
    {
        if (_strings.TryGetValue(key, out var value))
            return value;
        if (_fallback.TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    /// <summary>
    /// Returns the localised UI string for the given key, or <c>null</c> if the
    /// key is not found in either the active language or the English fallback.
    /// Useful when callers need to distinguish "key not defined" from an empty string.
    /// </summary>
    /// <param name="key">Dot-delimited string key.</param>
    /// <returns>The localised string, or <c>null</c> if the key does not exist.</returns>
    public static string? GetOrNull(string key)
    {
        if (_strings.TryGetValue(key, out var value))
            return value;
        if (_fallback.TryGetValue(key, out var fallback))
            return fallback;
        return null;
    }

    /// <summary>
    /// Formats a localised UI string using <see cref="string.Format(string, object[])"/>.
    /// The format string is retrieved via <see cref="Get"/>, then the supplied
    /// arguments are substituted.
    /// </summary>
    /// <param name="key">Dot-delimited string key whose value contains format placeholders.</param>
    /// <param name="args">Format arguments to substitute into the retrieved string.</param>
    /// <returns>The formatted localised string.</returns>
    /// <example>
    /// <code>
    /// // en-GB.json: "status.loaded_items": "Loaded {0} game items, {1} name mappings"
    /// string msg = UiStrings.Format("status.loaded_items", itemCount, mapCount);
    /// </code>
    /// </example>
    public static string Format(string key, params object[] args)
    {
        string template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // If the translated string has mismatched placeholders, return unformatted
            return template;
        }
    }

    /// <summary>
    /// Resets the UI string tables to empty state. Primarily used in unit tests.
    /// </summary>
    internal static void Reset()
    {
        _strings = new Dictionary<string, string>();
        _fallback = new Dictionary<string, string>();
        _activeTag = null;
        _uiLangDir = null;
    }

    /// <summary>
    /// Loads a single JSON file into a string dictionary.
    /// Returns <c>null</c> if the file does not exist or cannot be parsed.
    /// </summary>
    private static Dictionary<string, string>? LoadFile(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                    dict[prop.Name] = prop.Value.GetString()!;
            }
            return dict;
        }
        catch
        {
            return null;
        }
    }
}
