using System.ComponentModel;
using NMSE.Data;

namespace NMSE.UI.Localization;

/// <summary>
/// Singleton that wraps <see cref="UiStrings"/> with INotifyPropertyChanged
/// so that compiled-binding-based MarkupExtensions refresh automatically
/// when the UI language changes.
/// </summary>
public sealed class LocaleManager : INotifyPropertyChanged
{
    public static LocaleManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocaleManager() { }

    /// <summary>
    /// Indexer used by <see cref="LocaleExtension"/> compiled bindings.
    /// The <paramref name="key"/> is resolved through <see cref="UiStrings.Get"/>.
    /// WinForms-style ampersand mnemonics are converted to Avalonia underscores.
    /// </summary>
    public string this[string key] => UiStrings.Get(key).Replace("&", "_");

    /// <summary>
    /// Call after <see cref="UiStrings.Load"/> to refresh every bound UI string.
    /// Raising PropertyChanged with "Item" causes Avalonia to re-read the indexer.
    /// </summary>
    public void NotifyLanguageChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
    }
}
