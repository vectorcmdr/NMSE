using System.Diagnostics;
using NMSE.Config;
using NMSE.Core;
using NMSE.Data;
using NMSE.UI;

namespace NMSE;

static class Program
{
    /// <summary>
    /// The main entry point for the NMS Save Editor application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Remove the stale .old executable left by a previous self-update.
        UpdateService.CleanupOldExeIfPresent();

        // Initialise the UI string tables early so crash dialogs are localised
        // even when a failure happens before the main form finishes loading.
        InitializeUiStrings();

        // Wire up global exception handlers for crash logging
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Opt in to per-monitor DPI awareness so WinForms scales controls,
        // fonts, and images proportionally when the display scale factor changes.
        // Done as best effort - if not supported, default and silently degrade to default DPI.
        try
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
        }
        catch
        {
            // Not supported in compatibility layer or env
        }

        ApplicationConfiguration.Initialize();
        ThemeManager.ApplySavedTheme();

        // Show a lightweight splash screen so the user gets immediate
        // visual feedback while the main form performs heavy initialisation
        // (database loading, icon preloading, panel construction). The splash
        // runs its own message loop on a separate STA thread so it keeps
        // repainting if a window manager starts after it has been mapped.
        using var splashHost = new SplashHost();
        var splash = splashHost.Form;

        var mainForm = new MainFormResources();
        mainForm.SetSplash(splash);
        mainForm.PerformStartup();

        Application.Run(mainForm);
    }

    private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        MessageBox.Show(
            UiStrings.Format("dialog.crash_message", e.Exception.Message),
            UiStrings.Get("dialog.crash_title"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// Loads the UI string tables using the language saved in the configuration,
    /// so early startup failures (including the crash dialog) are localised.
    /// </summary>
    private static void InitializeUiStrings()
    {
        try
        {
            string uiLangDir = Path.Combine(AppContext.BaseDirectory, "Resources", "ui", "lang");
            UiStrings.SetDirectory(uiLangDir);
            var config = AppConfig.Instance;
            config.Initialize();
            UiStrings.Load(config.Language);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"UI string initialisation failed: {ex.Message}");
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogCrash(ex);
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            string entry = $"""
                === NMSE Crash Report ===
                Time (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
                Time (Local): {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                Exception: {ex.GetType().FullName}
                Message: {ex.Message}
                Stack Trace:
                {ex.StackTrace}
                {(ex.InnerException != null ? $"\nInner Exception: {ex.InnerException.GetType().FullName}\nMessage: {ex.InnerException.Message}\nStack Trace:\n{ex.InnerException.StackTrace}" : "")}
                ============================

                """;
            File.AppendAllText(logPath, entry);
        }
        catch
        {
            // Last resort - can't even log the crash
            Debug.WriteLine($"Failed to write crash log: {ex}");
        }
    }
}
