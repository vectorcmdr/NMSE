using Avalonia.Media;

namespace NMSE.UI.Views.Util;

/// <summary>
/// Manages the embedded NMS GeoSans font for use in the Avalonia application.
/// Loads the TTF from embedded resources and exposes FontFamily for XAML binding.
/// </summary>
public static class FontManager
{
    private static FontFamily? _nmsFont;
    private static bool _initialized;

    private const string FontResourceUri = "avares://NMSE/Resources/app/NMSGeoSans_Kerned.ttf#NMS GeoSans";

    /// <summary>The NMS GeoSans font family for use in Avalonia controls.</summary>
    public static FontFamily NmsFont
    {
        get
        {
            EnsureInitialized();
            return _nmsFont ?? FontFamily.Default;
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            _nmsFont = new FontFamily(FontResourceUri);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FontManager: failed to load font: {ex.Message}");
        }
    }
}
