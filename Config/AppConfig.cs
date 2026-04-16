using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using NMSE.Models;

namespace NMSE.Config;

/// <summary>
/// Application configuration manager.
/// Manages app settings, window state, and user preferences via JSON config file.
/// </summary>
public class AppConfig
{
    private const string ConfigFileName = "NMSE.conf";
    private static AppConfig? _instance;
    private readonly Dictionary<string, string> _properties = new();
    private string? _configPath;

    public static AppConfig Instance => _instance ??= new AppConfig();

    /// <summary>Maximum number of recent directories to store in the MRU list.</summary>
    public const int MaxRecentDirectories = 5;

    public string? LastDirectory
    {
        get => GetProperty("LastDirectory");
        set => SetProperty("LastDirectory", value);
    }

    /// <summary>
    /// Gets or sets the recent directories MRU list, stored as pipe-separated paths.
    /// </summary>
    public List<string> RecentDirectories
    {
        get
        {
            var raw = GetProperty("RecentDirectories");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return raw.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        set
        {
            if (value is null || value.Count == 0)
                SetProperty("RecentDirectories", null);
            else
                SetProperty("RecentDirectories", string.Join("|", value));
        }
    }

    /// <summary>
    /// Adds a directory to the recent directories MRU list.
    /// Moves it to the front if already present, trims to <see cref="MaxRecentDirectories"/>,
    /// and ensures <paramref name="defaultDir"/> (if provided) is always present.
    /// </summary>
    /// <param name="directory">The directory to add/promote to the front.</param>
    /// <param name="defaultDir">The OS-detected default save directory (always kept in the list).</param>
    public void AddRecentDirectory(string directory, string? defaultDir = null)
    {
        var list = RecentDirectories;

        // Remove existing entry (case-insensitive on Windows, case-sensitive elsewhere)
        list.RemoveAll(d => string.Equals(d, directory, PathComparison));

        // Insert at front (most recent)
        list.Insert(0, directory);

        // Ensure default directory is always in the list
        if (!string.IsNullOrEmpty(defaultDir) &&
            !list.Any(d => string.Equals(d, defaultDir, PathComparison)))
        {
            list.Add(defaultDir);
        }

        // Trim to max size, but never evict the default directory
        while (list.Count > MaxRecentDirectories)
        {
            int removeIdx = list.FindLastIndex(d => !string.Equals(d, defaultDir, PathComparison));
            if (removeIdx >= 0)
                list.RemoveAt(removeIdx);
            else
                break; // Only default entries remain
        }

        RecentDirectories = list;
        LastDirectory = directory;
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public string? Theme
    {
        get => GetProperty("Theme");
        set => SetProperty("Theme", value);
    }

    /// <summary>
    /// Last selected BCP 47 language tag (e.g. "en-GB", "ja-JP").
    /// Defaults to "en-GB" if not set.
    /// </summary>
    public string Language
    {
        get => GetProperty("Language") ?? "en-GB";
        set => SetProperty("Language", value);
    }

    public int MainFrameX
    {
        get => int.TryParse(GetProperty("MainFrame.X"), out int v) ? v : 100;
        set => SetProperty("MainFrame.X", value.ToString(CultureInfo.InvariantCulture));
    }

    public int MainFrameY
    {
        get => int.TryParse(GetProperty("MainFrame.Y"), out int v) ? v : 100;
        set => SetProperty("MainFrame.Y", value.ToString(CultureInfo.InvariantCulture));
    }

    public int MainFrameWidth
    {
        get => int.TryParse(GetProperty("MainFrame.Width"), out int v) ? v : 1200;
        set => SetProperty("MainFrame.Width", value.ToString(CultureInfo.InvariantCulture));
    }

    public int MainFrameHeight
    {
        get => int.TryParse(GetProperty("MainFrame.Height"), out int v) ? v : 800;
        set => SetProperty("MainFrame.Height", value.ToString(CultureInfo.InvariantCulture));
    }

    public static string BuildSaveScopeKey(string? saveFilePath)
    {
        if (string.IsNullOrWhiteSpace(saveFilePath))
            return "unknown";

        string normalized;
        try
        {
            normalized = Path.GetFullPath(saveFilePath).Trim();
        }
        catch
        {
            normalized = saveFilePath.Trim();
        }

        if (OperatingSystem.IsWindows())
            normalized = normalized.ToLowerInvariant();

        byte[] data = Encoding.UTF8.GetBytes(normalized);
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash[..8]).ToLowerInvariant();
    }

    private static string BuildPinnedSlotsPropertyKey(string saveScopeKey, string inventoryKey)
        => $"PinnedSlots.{saveScopeKey}.{inventoryKey}";

    public HashSet<(int x, int y)> GetPinnedSlots(string saveScopeKey, string inventoryKey)
    {
        var result = new HashSet<(int x, int y)>();
        string? raw = GetProperty(BuildPinnedSlotsPropertyKey(saveScopeKey, inventoryKey));
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        var entries = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var entry in entries)
        {
            var parts = entry.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                continue;

            if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                result.Add((x, y));
        }

        return result;
    }

    public void SetPinnedSlots(string saveScopeKey, string inventoryKey, IEnumerable<(int x, int y)> pinnedSlots)
    {
        string key = BuildPinnedSlotsPropertyKey(saveScopeKey, inventoryKey);
        string value = string.Join(";",
            pinnedSlots
                .Distinct()
                .OrderBy(p => p.y)
                .ThenBy(p => p.x)
                .Select(p => $"{p.x},{p.y}"));

        SetProperty(key, string.IsNullOrEmpty(value) ? null : value);
    }

    /// <summary>Creates the config directory and loads settings from disk if available.</summary>
    public void Initialize()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string configDir = Path.Combine(appData, "NMSE");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, ConfigFileName);

        if (File.Exists(_configPath))
            Load();
    }

    public string? GetProperty(string key) =>
        _properties.GetValueOrDefault(key);

    /// <summary>Sets or removes a configuration property by key.</summary>
    public void SetProperty(string key, string? value)
    {
        if (value is null)
            _properties.Remove(key);
        else
            _properties[key] = value;
    }

    /// <summary>Loads configuration properties from the JSON config file on disk.</summary>
    public void Load()
    {
        if (_configPath is null || !File.Exists(_configPath)) return;

        try
        {
            var json = File.ReadAllText(_configPath);
            var obj = JsonObject.Parse(json);
            foreach (var name in obj.Names())
            {
                var value = obj.Get(name);
                if (value is not null)
                    _properties[name] = value.ToString()!;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
        }
    }

    /// <summary>Persists the current configuration properties to the JSON config file.</summary>
    public void Save()
    {
        if (_configPath is null) return;

        try
        {
            var obj = new JsonObject();
            foreach (var kvp in _properties.OrderBy(k => k.Key))
                obj.Add(kvp.Key, kvp.Value);
            File.WriteAllText(_configPath, obj.ToFormattedString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
        }
    }
}
