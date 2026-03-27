using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NMSE.Data;

/// <summary>
/// Loads and caches item icon images from the Resources/images/ directory.
/// Icon filenames in the JSON data match image filenames directly (e.g., "CASING.png").
/// Images are downscaled to a maximum dimension when cached to keep memory
/// usage reasonable (~300 MB vs ~1.2 GB for full-size 256x256 originals).
/// </summary>
public class IconManager : IDisposable
{
    /// <summary>
    /// Maximum width/height for cached images. Icons larger than this are
    /// downscaled on load.  128 px is more than sufficient for the largest
    /// display (96x96 detail icon with Zoom) while using 4x less memory
    /// than the 256x256 originals.
    /// </summary>
    private const int MaxCacheDimension = 128;

    private readonly Dictionary<string, Image?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _iconsDirectory;
    private bool _disposed;

    /// <summary>Initializes a new <see cref="IconManager"/> loading icons from the specified directory.</summary>
    /// <param name="iconsDirectory">Path to the directory containing icon image files.</param>
    public IconManager(string iconsDirectory)
    {
        _iconsDirectory = iconsDirectory;
    }

    /// <summary>
    /// Gets an icon image for the given filename. Returns null if not found.
    /// Results are cached for performance.
    /// </summary>
    public Image? GetIcon(string? iconFilename)
    {
        if (string.IsNullOrEmpty(iconFilename)) return null;

        if (_cache.TryGetValue(iconFilename, out var cached))
            return cached;

        Image? image = null;
        try
        {
            string path = Path.Combine(_iconsDirectory, iconFilename);
            if (File.Exists(path))
            {
                // Load into memory so file isn't locked
                using var stream = File.OpenRead(path);
                using var original = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: false);
                image = Downscale(original);
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
    /// Reads image files from disk on background threads, then merges
    /// into the main cache. Dramatically reduces per-icon disk I/O
    /// during panel loading since most icons will be cache hits.
    /// Must be called during startup before any panels call GetIcon().
    /// </summary>
    public void PreloadIcons(GameItemDatabase database)
    {
        var iconNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in database.Items.Values)
        {
            if (!string.IsNullOrEmpty(item.Icon))
                iconNames.Add(item.Icon);
        }

        // Remove already-cached entries
        foreach (var name in _cache.Keys)
            iconNames.Remove(name);

        if (iconNames.Count == 0) return;

        // Load images in parallel on background threads.
        // Cap parallelism to Environment.ProcessorCount to avoid excessive
        // thread-pool contention and GDI+ lock contention.
        var results = new ConcurrentDictionary<string, Image?>(StringComparer.OrdinalIgnoreCase);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
        Parallel.ForEach(iconNames, options, iconFilename =>
        {
            Image? image = null;
            try
            {
                string path = Path.Combine(_iconsDirectory, iconFilename);
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    using var ms = new MemoryStream(bytes);
                    using var original = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
                    image = Downscale(original);
                }
            }
            catch
            {
                image = null;
            }
            results[iconFilename] = image;
        });

        // Merge into main cache (single-threaded)
        foreach (var kvp in results)
            _cache[kvp.Key] = kvp.Value;
    }

    /// <summary>
    /// Gets an icon for an item by looking up the icon filename from the database.
    /// </summary>
    public Image? GetIconForItem(string? itemId, GameItemDatabase? database)
    {
        if (database == null || string.IsNullOrEmpty(itemId)) return null;

        var item = database.GetItem(itemId) ?? database.GetItem("^" + itemId);
        return item != null ? GetIcon(item.Icon) : null;
    }

    /// <summary>
    /// Returns a downscaled copy of <paramref name="original"/> if either
    /// dimension exceeds <see cref="MaxCacheDimension"/>; otherwise returns
    /// a copy at the original size. The caller owns the returned image.
    /// Always renders through <see cref="Graphics.DrawImage"/> so that
    /// corrupt source data (loaded with <c>validateImageData: false</c>)
    /// is caught here rather than later when the cached image is drawn.
    /// </summary>
    private static Image Downscale(Image original)
    {
        int ow = original.Width, oh = original.Height;
        int maxDim = Math.Max(ow, oh);

        int nw, nh;
        if (maxDim <= MaxCacheDimension)
        {
            nw = ow;
            nh = oh;
        }
        else
        {
            float scale = (float)MaxCacheDimension / maxDim;
            nw = Math.Max(1, (int)(ow * scale));
            nh = Math.Max(1, (int)(oh * scale));
        }

        var bmp = new Bitmap(nw, nh, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            // Bilinear is much faster than HighQualityBicubic and visually
            // indistinguishable at 128 px.  This significantly speeds up the
            // parallel icon pre-load during startup.
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
            g.DrawImage(original, 0, 0, nw, nh);
        }
        return bmp;
    }

    /// <summary>Disposes all cached icon images and clears the cache.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var img in _cache.Values)
            img?.Dispose();

        _cache.Clear();
    }
}
