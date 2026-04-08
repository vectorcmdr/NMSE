using NMSE.Extractor.Config;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NMSE.Extractor.Data;

/// <summary>
/// Base EXML/MXML parser with utilities for property extraction, localisation, and value parsing.
/// </summary>
public class MxmlParser
{
    private static readonly HashSet<string> LowercaseWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "of", "in", "on", "at", "to", "for",
        "with", "by", "as", "from", "into", "onto", "upon", "nor", "so", "yet"
    };

    private static readonly Dictionary<string, string> MissingLocalisationOverrides = new()
    {
        ["UI_BRIDGECONNECT_NAME"] = "Bridge Connector"
    };

    private static readonly Regex MarkupTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex FeTokenRegex = new(@"\bFE_[A-Z0-9_]+\b", RegexOptions.Compiled);

    // FE_* control token -> keyboard/mouse readable label mapping (from controllerLookup.generated.json, Win platform).
    // Icon paths are converted to readable keys.
    private static readonly Dictionary<string, string> FeTokenMap = new()
    {
        ["FE_ALT1"] = "[E]",     // KEYBOARD/INTERACT.E.png
        ["FE_SELECT"] = "[LMB]", // MOUSE/KEY.MOUSELEFT.png
        // FE_BACK has no icon -> stays unchanged
    };

    private static Dictionary<string, string>? _localisation;
    private static readonly object _localisationLock = new();
    private static readonly ConcurrentDictionary<string, (DateTime Modified, XElement Root)> XmlCache = new();

    public static Dictionary<string, string> LoadLocalisation(string jsonDir)
    {
        if (_localisation != null) return _localisation;

        lock (_localisationLock)
        {
            if (_localisation != null) return _localisation;

            // Primary: lang/en-GB.json (replaces former Localisation-EN.json.tbc)
            string locPath = Path.Combine(jsonDir, ExtractorConfig.LangSubfolder, "en-GB.json");
            if (!File.Exists(locPath))
            {
                // Fallback: legacy Localisation-EN.json.tbc for older resource sets
                locPath = Path.Combine(jsonDir, "Localisation-EN.json.tbc");
            }

            if (File.Exists(locPath))
            {
                string json = File.ReadAllText(locPath);
                _localisation = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                Console.WriteLine($"[OK] Loaded {_localisation.Count} translations from {Path.GetFileName(locPath)}");
            }
            else
            {
                _localisation = new();
                Console.WriteLine("[WARN] No localisation file found (lang/en-GB.json)");
            }
            return _localisation;
        }
    }

    public static void ClearLocalisationCache() => _localisation = null;

    public static void ClearXmlCache() => XmlCache.Clear();

    public static string Translate(string key, string? defaultValue = null)
    {
        var loc = _localisation ?? new();
        string fallback = defaultValue ?? key;

        if (loc.TryGetValue(key, out string? translation))
            return PostProcessTranslation(key, translation);

        if (MissingLocalisationOverrides.TryGetValue(key, out string? overrideVal))
            return PostProcessTranslation(key, overrideVal);

        // Try alternate key (strip redundant "_NAME" segment, e.g. UI_X_NAME_SUB -> UI_X_SUB)
        int nameIdx = key.IndexOf("_NAME_", StringComparison.Ordinal);
        if (nameIdx > 0)
        {
            string altKey = string.Concat(key.AsSpan(0, nameIdx), key.AsSpan(nameIdx + 5));
            if (loc.TryGetValue(altKey, out string? altTranslation))
                return PostProcessTranslation(altKey, altTranslation);
        }

        // If looks like a key, make it readable
        if (fallback == key && key.Contains('_'))
        {
            string cleaned = key
                .Replace("_NAME", "")
                .Replace("_DESC", "")
                .Replace("_SUBTITLE", "");
            var words = cleaned.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 0 && words[0].Equals("UI", StringComparison.OrdinalIgnoreCase))
                words = words.Skip(1).ToArray();
            fallback = string.Join(" ", words.Select(w =>
                char.ToUpper(w[0]) + w[1..].ToLower()));
        }

        return PostProcessTranslation(key, fallback);
    }

    private static string PostProcessTranslation(string key, string text)
    {
        if (key.EndsWith("_NAME", StringComparison.Ordinal))
            text = TitleCaseName(text);
        text = StripMarkupTags(text);
        return text;
    }

    /// <summary>
    /// Convert FE_* control placeholders to readable keyboard/mouse labels.
    /// e.g. "Use FE_ALT1" -> "Use [E]"
    /// </summary>
    public static string NormalizeControlTokens(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("FE_"))
            return text;
        return FeTokenRegex.Replace(text, m =>
            FeTokenMap.TryGetValue(m.Value, out var label) ? label : m.Value);
    }

    public static string StripMarkupTags(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('<'))
            return text;
        return MarkupTagRegex.Replace(text, "");
    }

    public static string TitleCaseName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var words = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return s;

        var result = new string[words.Length];
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            string lower = word.ToLowerInvariant();
            bool isFirst = i == 0;
            bool isLast = i == words.Length - 1;
            bool forceCapitalize = isFirst || isLast || !LowercaseWords.Contains(lower);
            result[i] = CapitalizeWord(word, forceCapitalize);
        }
        return string.Join(" ", result);
    }

    private static string CapitalizeWord(string word, bool forceCapitalize)
    {
        if (word.Length >= 3 && word.StartsWith('\'') && word.EndsWith('\''))
        {
            string inner = word[1..^1];
            return "'" + (forceCapitalize
                ? char.ToUpper(inner[0]) + inner[1..].ToLower()
                : inner.ToLower()) + "'";
        }
        return forceCapitalize
            ? char.ToUpper(word[0]) + word[1..].ToLower()
            : word.ToLower();
    }

    public static string NormalizeGameIconPath(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath)) return "";
        return gamePath.Trim().Replace('\\', '/').ToLowerInvariant();
    }

    public static bool LooksLikeLocalisationKey(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('_'))
            return false;
        return Regex.IsMatch(value, @"^[A-Z0-9_]+$");
    }

    public static int UnresolvedLocalisationKeyCount(Dictionary<string, string> localisation, params string[] keys)
    {
        return keys.Count(key => LooksLikeLocalisationKey(key) &&
            !localisation.ContainsKey(key) &&
            !MissingLocalisationOverrides.ContainsKey(key) &&
            !TryAlternateLocKey(localisation, key));
    }

    /// <summary>
    /// Tries to find a localisation value by stripping a redundant "_NAME" segment from the key.
    /// Handles game data where keys were updated to include "_NAME" (e.g. UI_X_NAME_SUB)
    /// while the localisation file uses the original keys (e.g. UI_X_SUB).
    /// </summary>
    private static bool TryAlternateLocKey(Dictionary<string, string> localisation, string key)
    {
        int idx = key.IndexOf("_NAME_", StringComparison.Ordinal);
        if (idx > 0)
        {
            string alt = string.Concat(key.AsSpan(0, idx), key.AsSpan(idx + 5)); // strip "_NAME"
            return localisation.ContainsKey(alt);
        }
        return false;
    }

    public static string FormatStatTypeName(string statType, params string[] stripPrefixes)
    {
        if (string.IsNullOrEmpty(statType)) return "";
        string cleaned = statType;
        foreach (string prefix in stripPrefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.Ordinal))
                cleaned = cleaned[prefix.Length..];
        }

        var words = new List<string>();
        foreach (string token in cleaned.Split('_', StringSplitOptions.RemoveEmptyEntries))
        {
            // Split CamelCase
            string split = Regex.Replace(token, @"(?<=[a-z0-9])(?=[A-Z])", " ");
            split = Regex.Replace(split, @"(?<=[A-Z])(?=[A-Z][a-z])", " ");
            words.Add(split);
        }

        string result = string.Join(" ", words);
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower());
    }

    // XML loading with caching
    public static XElement LoadXml(string filepath)
    {
        string fullPath = Path.GetFullPath(filepath);
        var fileInfo = new FileInfo(fullPath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException($"MXML file not found: {fullPath}");

        if (XmlCache.TryGetValue(fullPath, out var cached) && cached.Modified == fileInfo.LastWriteTime)
            return cached.Root;

        var doc = XDocument.Load(fullPath);
        var root = doc.Root ?? throw new InvalidOperationException($"Empty XML: {fullPath}");
        XmlCache[fullPath] = (fileInfo.LastWriteTime, root);
        return root;
    }

    // Property value extraction
    public static string GetPropertyValue(XElement? element, string name, string defaultValue = "")
    {
        if (element == null) return defaultValue;

        // Direct child first, then deep search
        var prop = element.Elements("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == name);
        if (prop == null)
            prop = element.Descendants("Property")
                .FirstOrDefault(e => e.Attribute("name")?.Value == name);

        return prop?.Attribute("value")?.Value ?? defaultValue;
    }

    public static string GetNestedEnum(XElement element, string outerName,
        string? innerName = null, string defaultValue = "")
    {
        string name = innerName ?? outerName;
        var outer = element.Descendants("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == outerName);
        if (outer == null) return defaultValue;

        var inner = outer.Elements("Property")
            .FirstOrDefault(e => e.Attribute("name")?.Value == name);
        return inner?.Attribute("value")?.Value ?? defaultValue;
    }

    public static object ParseValue(string valueStr)
    {
        if (string.IsNullOrEmpty(valueStr)) return "";

        if (valueStr.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (valueStr.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;

        if (valueStr.Length > 0 && valueStr[0] != '-' && valueStr[0] != '+' && !char.IsDigit(valueStr[0]))
            return valueStr;

        if (double.TryParse(valueStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double num))
        {
            if (!valueStr.Contains('.') && num == Math.Floor(num) && num is >= int.MinValue and <= int.MaxValue)
                return (int)num;
            return num;
        }

        return valueStr;
    }

    public static string ParseColour(XElement? colourElement)
    {
        if (colourElement == null) return "FFFFFF";
        int r = (int)(double.Parse(GetPropertyValue(colourElement, "R", "1"),
            System.Globalization.CultureInfo.InvariantCulture) * 255);
        int g = (int)(double.Parse(GetPropertyValue(colourElement, "G", "1"),
            System.Globalization.CultureInfo.InvariantCulture) * 255);
        int b = (int)(double.Parse(GetPropertyValue(colourElement, "B", "1"),
            System.Globalization.CultureInfo.InvariantCulture) * 255);
        return $"{Math.Clamp(r, 0, 255):X2}{Math.Clamp(g, 0, 255):X2}{Math.Clamp(b, 0, 255):X2}";
    }
}
