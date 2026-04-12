using System.Diagnostics;
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
        // Wire up global exception handlers for crash logging
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        ApplicationConfiguration.Initialize();

        // Show a lightweight splash screen so the user gets immediate
        // visual feedback while the main form performs heavy initialisation
        // (database loading, icon preloading, panel construction).
        using var splash = new SplashForm();
        splash.Show();
        Application.DoEvents(); // Process WM_SETICON and taskbar updates before heavy startup.
        splash.Refresh();

        var mainForm = new MainFormResources();
        mainForm.SetSplash(splash);
        mainForm.PerformStartup();

        Application.Run(mainForm);
    }

    private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails have been written to crash.log in the application directory.",
            "NMSE – Unexpected Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
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