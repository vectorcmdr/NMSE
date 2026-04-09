using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace NMSE.UI.Util;

/// <summary>
/// Manages the embedded NMS GeoSans font for use in the application.
/// Loads the TTF from embedded resources via PrivateFontCollection and
/// exposes helper methods to create Font instances at various sizes.
/// </summary>
public static class FontManager
{
    private static PrivateFontCollection? _fontCollection;
    private static FontFamily? _nmsFont;
    private static bool _initialized;
    
    private static PrivateFontCollection? _glyphFontCollection;
    private static FontFamily? _glyphFont;
    private static bool _glyphInitialized;

    private const string FontResourceName = "NMSE.Resources.app.NMSGeoSans_Kerned.ttf";
    private const string GlyphFontResourceName = "NMSE.Resources.app.NMS_Glyphs_Mono.ttf";

    /// <summary>The loaded NMS GeoSans font family, or null if loading failed.</summary>
    public static FontFamily? NmsFont
    {
        get
        {
            EnsureInitialized();
            return _nmsFont;
        }
    }

    /// <summary>
    /// Creates a Font using the embedded NMS GeoSans font at the given size and style.
    /// Falls back to the default control font family if the embedded font could not be loaded.
    /// </summary>
    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        EnsureInitialized();
        var family = _nmsFont ?? Control.DefaultFont.FontFamily;
        return new Font(family, size, style);
    }

    /// <summary>The loaded NMS portal glyph font family, or null if loading failed.</summary>
    public static FontFamily? GlyphFont
    {
        get
        {
            EnsureGlyphInitialized();
            return _glyphFont;
        }
    }

    /// <summary>
    /// Creates a Font using the embedded NMS portal glyph font at the given size.
    /// Falls back to Consolas if the embedded font could not be loaded.
    /// </summary>
    public static Font CreateGlyphFont(float size)
    {
        EnsureGlyphInitialized();
        var family = _glyphFont ?? FontFamily.GenericMonospace;
        return new Font(family, size, FontStyle.Regular);
    }

    /// <summary>
    /// Creates a heading font (bold) at the specified size using the NMS GeoSans font.
    /// </summary>
    public static Font CreateHeadingFont(float size)
        => CreateFont(size, FontStyle.Bold);

    /// <summary>
    /// Applies the NMS heading font to a Label and enables GDI+ rendering
    /// (UseCompatibleTextRendering) so the embedded PrivateFontCollection font
    /// is used without requiring a system-level install.
    /// </summary>
    public static void ApplyHeadingFont(Label label, float size)
    {
        label.Font = CreateHeadingFont(size);
        label.UseCompatibleTextRendering = true;
    }

    /// <summary>
    /// Applies the NMS font to a Label with the given style and enables GDI+ rendering.
    /// </summary>
    public static void ApplyFont(Label label, float size, FontStyle style = FontStyle.Regular)
    {
        label.Font = CreateFont(size, style);
        label.UseCompatibleTextRendering = true;
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            var assembly = typeof(FontManager).Assembly;
            using var stream = assembly.GetManifestResourceStream(FontResourceName);
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"FontManager: embedded resource '{FontResourceName}' not found.");
                return;
            }

            byte[] fontData = new byte[stream.Length];
            stream.ReadExactly(fontData);

            _fontCollection = new PrivateFontCollection();
            var handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
            try
            {
                _fontCollection.AddMemoryFont(handle.AddrOfPinnedObject(), fontData.Length);
            }
            finally
            {
                handle.Free();
            }

            if (_fontCollection.Families.Length > 0)
                _nmsFont = _fontCollection.Families[0];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FontManager: failed to load font: {ex.Message}");
        }
    }

        private static void EnsureGlyphInitialized()
    {
        if (_glyphInitialized) return;
        _glyphInitialized = true;

        try
        {
            var assembly = typeof(FontManager).Assembly;
            using var stream = assembly.GetManifestResourceStream(GlyphFontResourceName);
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"FontManager: embedded resource '{GlyphFontResourceName}' not found.");
                return;
            }

            byte[] fontData = new byte[stream.Length];
            stream.ReadExactly(fontData);

            _glyphFontCollection = new PrivateFontCollection();
            var handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
            try
            {
                _glyphFontCollection.AddMemoryFont(handle.AddrOfPinnedObject(), fontData.Length);
            }
            finally
            {
                handle.Free();
            }

            if (_glyphFontCollection.Families.Length > 0)
                _glyphFont = _glyphFontCollection.Families[0];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FontManager: failed to load glyph font: {ex.Message}");
        }
    }
}
