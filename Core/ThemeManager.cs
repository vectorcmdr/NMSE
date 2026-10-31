using NMSE.Config;

namespace NMSE.Core;

/// <summary>
/// Manages the current application theme state. Fires <see cref="ThemeChanged"/>
/// when the theme is toggled so that UI components can re-apply colours.
/// </summary>
internal static class ThemeManager
{
	/// <summary>
	/// The currently active theme.
	/// </summary>
	internal static AppTheme Current { get; private set; } = AppTheme.Light;

	/// <summary>
	/// Resolves the effective theme, expanding <see cref="AppTheme.System"/> to either
	/// <see cref="AppTheme.Light"/> or <see cref="AppTheme.Dark"/> based on the OS preference.
	/// Use this for all theme comparisons and palette lookups.
	/// </summary>
	internal static AppTheme Effective =>
		Current == AppTheme.System ? DetectSystemTheme() : Current;

	/// <summary>
	/// Raised when <see cref="Current"/> changes. Handlers should re-apply
	/// theme colours to their controls.
	/// </summary>
	internal static event Action? ThemeChanged;

	/// <summary>
	/// Sets the theme. No-op if the theme is unchanged.
	/// </summary>
    internal static void SetTheme(AppTheme theme)
    {
        if (Current == theme)
            return;

        Current = theme;
        ThemeChanged?.Invoke();
    }

    /// <summary>
    /// Applies the theme saved in the application configuration. If no valid
    /// theme has been saved, the existing default (Light) is retained.
    /// </summary>
    internal static void ApplySavedTheme()
    {
        string? saved = AppConfig.Instance.Theme;
        if (string.IsNullOrEmpty(saved))
            return;

        if (Enum.TryParse<AppTheme>(saved, ignoreCase: true, out var theme)
            && Enum.IsDefined(theme))
        {
            SetTheme(theme);
        }
    }

    /// <summary>
    /// Detects the OS-level dark/light preference.
	/// On Windows reads the registry. On non-Windows defaults to Light
	/// because WINE does not reliably surface the host OS preference.
	/// </summary>
	internal static AppTheme DetectSystemTheme()
	{
		if (!OperatingSystem.IsWindows())
			return AppTheme.Light;

		try
		{
			const string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
			object? value = Microsoft.Win32.Registry.GetValue(keyPath, "AppsUseLightTheme", 1);
			return value is int i && i == 0 ? AppTheme.Dark : AppTheme.Light;
		}
		catch
		{
			return AppTheme.Light;
		}
	}
}

/// <summary>
/// Application theme options.
/// </summary>
internal enum AppTheme
{
	/// <summary>Follow the operating system light/dark preference.</summary>
	System,
	/// <summary>Force light theme regardless of OS preference.</summary>
	Light,
	/// <summary>Force dark theme regardless of OS preference.</summary>
	Dark
}
