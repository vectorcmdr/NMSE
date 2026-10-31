using System.Diagnostics;

namespace NMSE.UI;

/// <summary>
/// Hosts the startup splash screen on a dedicated STA thread so that its
/// window continues to process X11 paint and reparent events while the main
/// thread performs synchronous startup work. Without its own message loop the
/// splash can remain black when a window manager starts after the splash has
/// already been mapped, which is what the Linux AppImage test does.
/// </summary>
internal sealed class SplashHost : IDisposable
{
    private readonly TaskCompletionSource<SplashForm> _formSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private SplashForm? _form;
    private bool _disposed;

    /// <summary>
    /// Starts the splash thread and waits until the splash form has been
    /// created. The splash form itself is shown by its own message loop.
    /// </summary>
    internal SplashHost()
    {
        _thread = new Thread(RunSplash)
        {
            IsBackground = true,
            Name = "NMSE startup splash",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        try
        {
            _form = _formSource.Task.GetAwaiter().GetResult();
        }
        catch
        {
            _thread.Join(TimeSpan.FromSeconds(2));
            throw;
        }
    }

    /// <summary>The splash form owned by the splash thread.</summary>
    internal SplashForm Form => _form
        ?? throw new InvalidOperationException("The splash form is not available.");

    private void RunSplash()
    {
        try
        {
            using var form = new SplashForm();
            form.Shown += (_, _) => _formSource.TrySetResult(form);
            Application.Run(form);
        }
        catch (Exception ex)
        {
            _formSource.TrySetException(ex);
            Debug.WriteLine($"Splash thread failed: {ex}");
        }
    }

    /// <summary>
    /// Requests that the splash close and waits for its message loop to end.
    /// Safe to call from the main thread more than once.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_form != null && _form.IsHandleCreated)
                _form.CloseFromOwner();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Closing splash failed: {ex.Message}");
        }

        _thread.Join(TimeSpan.FromSeconds(5));
    }
}
