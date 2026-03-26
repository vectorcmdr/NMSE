using System.Collections.Concurrent;
using Avalonia.Media.Imaging;

namespace NMSE.Data;

/// <summary>
/// Loads and caches item icon images from the Resources/images/ directory.
/// Icon filenames in the JSON data match image filenames directly (e.g., "CASING.png").
/// Images are downscaled to a maximum dimension when cached to keep memory
/// usage reasonable (~300 MB vs ~1.2 GB for full-size 256x256 originals).
/// </summary>
public class IconManager : IDisposable
{
    private const int MaxCacheDimension = 128;

    private readonly Dictionary<string, Bitmap?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _iconsDirectory;
    private bool _disposed;

    public IconManager(string iconsDirectory)
    {
        _iconsDirectory = iconsDirectory;
    }

    public Bitmap? GetIcon(string? iconFilename)
    {
        if (string.IsNullOrEmpty(iconFilename)) return null;

        if (_cache.TryGetValue(iconFilename, out var cached))
            return cached;

        Bitmap? image = null;
        try
        {
            string path = Path.Combine(_iconsDirectory, iconFilename);
            if (File.Exists(path))
            {
                image = LoadAndScale(path);
            }
        }
        catch
        {
            image = null;
        }

        _cache[iconFilename] = image;
        return image;
    }

    /// <summary>
    /// Pre-loads icon images for all items in the database in parallel.
    /// </summary>
    public void PreloadIcons(GameItemDatabase database)
    {
        var iconNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in database.Items.Values)
        {
            if (!string.IsNullOrEmpty(item.Icon))
                iconNames.Add(item.Icon);
        }

        foreach (var name in _cache.Keys)
            iconNames.Remove(name);

        if (iconNames.Count == 0) return;

        var results = new ConcurrentDictionary<string, Bitmap?>(StringComparer.OrdinalIgnoreCase);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        Parallel.ForEach(iconNames, options, iconFilename =>
        {
            Bitmap? image = null;
            try
            {
                string path = Path.Combine(_iconsDirectory, iconFilename);
                if (File.Exists(path))
                {
                    image = LoadAndScale(path);
                }
            }
            catch
            {
                image = null;
            }
            results[iconFilename] = image;
        });

        foreach (var kvp in results)
            _cache[kvp.Key] = kvp.Value;
    }

    /// <summary>
    /// Gets a small class icon for item classification badges (e.g. "S", "A", "B", "C" class).
    /// </summary>
    public Bitmap? GetClassIcon(string? itemClass)
    {
        if (string.IsNullOrEmpty(itemClass)) return null;
        string filename = $"CLASS_{itemClass.ToUpperInvariant()}.png";
        return GetIcon(filename);
    }

    public Bitmap? GetIconForItem(string? itemId, GameItemDatabase? database)
    {
        if (database == null || string.IsNullOrEmpty(itemId)) return null;

        var item = database.GetItem(itemId) ?? database.GetItem("^" + itemId);
        return item != null ? GetIcon(item.Icon) : null;
    }

    private static Bitmap? LoadAndScale(string path)
    {
        using var stream = File.OpenRead(path);
        var original = new Bitmap(stream);

        int maxDim = Math.Max(original.PixelSize.Width, original.PixelSize.Height);
        if (maxDim <= MaxCacheDimension)
            return original;

        double scale = (double)MaxCacheDimension / maxDim;
        int nw = Math.Max(1, (int)(original.PixelSize.Width * scale));
        int nh = Math.Max(1, (int)(original.PixelSize.Height * scale));

        var scaled = original.CreateScaledBitmap(new Avalonia.PixelSize(nw, nh));
        original.Dispose();
        return scaled;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var img in _cache.Values)
            img?.Dispose();

        _cache.Clear();
    }
}
