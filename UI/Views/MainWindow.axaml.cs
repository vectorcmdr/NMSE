using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NMSE.UI.ViewModels;

namespace NMSE.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            var (x, y, w, h) = _viewModel.GetWindowState();
            if (w > 0 && h > 0)
            {
                Width = w;
                Height = h;
                Position = new PixelPoint(x, y);
            }

            _viewModel.SaveFilePickerFunc = async () =>
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export JSON",
                    DefaultExtension = "json",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });
                return file?.TryGetLocalPath();
            };

            _viewModel.OpenFilePickerFunc = async () =>
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Import JSON",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });
                return files.Count > 0 ? files[0].TryGetLocalPath() : null;
            };

            _viewModel.MainStats.PickFolderFunc = async () =>
            {
                var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "Select Destination Directory",
                    AllowMultiple = false
                });
                return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            };

            WirePanelFilePickers(_viewModel.Companion);
            WirePanelFilePickers(_viewModel.Settlement);
            WirePanelFilePickers(_viewModel.ByteBeat);
            WirePanelFilePickers(_viewModel.Base);

            _viewModel.Base.ShowObjectPickerFunc = async (items) =>
            {
                var tcs = new TaskCompletionSource<int>();
                var dialog = new Window
                {
                    Title = NMSE.Data.UiStrings.Get("base.select_target"),
                    Width = 400,
                    Height = 350,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var listBox = new ListBox
                {
                    ItemsSource = items,
                    SelectedIndex = 0,
                    Margin = new Thickness(8)
                };

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(8),
                    MinWidth = 80
                };

                okButton.Click += (_, _) =>
                {
                    tcs.TrySetResult(listBox.SelectedIndex);
                    dialog.Close();
                };

                dialog.Closing += (_, _) => tcs.TrySetResult(-1);

                var panel = new DockPanel();
                DockPanel.SetDock(okButton, Avalonia.Controls.Dock.Bottom);
                panel.Children.Add(okButton);
                panel.Children.Add(listBox);
                dialog.Content = panel;

                await dialog.ShowDialog(this);
                return await tcs.Task;
            };

            _viewModel.ShutdownApp = () =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            };

            await _viewModel.InitializeAsync();
            BuildLanguageMenu();
        };

        Closing += (_, _) =>
        {
            _viewModel.SaveWindowState(
                Position.X, Position.Y,
                (int)Bounds.Width, (int)Bounds.Height);
        };
    }

    private async void OnOpenDirectory(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Save Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].TryGetLocalPath();
            if (path != null)
                _viewModel.RecordRecentDirectory(path);
        }
    }

    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Save File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("NMS Save Files") { Patterns = new[] { "*.hg", "*.dat" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].TryGetLocalPath();
            if (path != null)
                await _viewModel.LoadSaveDataAsync(path);
        }
    }

    private async void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            DefaultExtension = "hg",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("NMS Save Files") { Patterns = new[] { "*.hg" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
            }
        });

        if (file != null)
        {
            var path = file.TryGetLocalPath();
            if (path != null)
            {
                _viewModel.SetSaveFilePath(path);
                _viewModel.SaveCommand.Execute(null);
            }
        }
    }

    private void OnBrowseDirectory(object? sender, RoutedEventArgs e)
    {
        OnOpenDirectory(sender, e);
    }

    private void BuildLanguageMenu()
    {
        var languageMenu = this.FindControl<MenuItem>("LanguageMenu");
        if (languageMenu == null) return;

        languageMenu.Items.Clear();
        foreach (var lang in _viewModel.Languages)
        {
            var tag = lang.Tag;
            var item = new MenuItem { Header = tag };
            item.Click += (_, _) => _viewModel.SelectLanguageCommand.Execute(tag);
            languageMenu.Items.Add(item);
        }
    }

    private void WirePanelFilePickers(ViewModels.Panels.PanelViewModelBase panel)
    {
        panel.SaveFilePickerFunc = async (title, defaultExt, filterDesc) =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                DefaultExtension = defaultExt,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(filterDesc) { Patterns = new[] { $"*.{defaultExt}" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });
            return file?.TryGetLocalPath();
        };

        panel.OpenFilePickerFunc = async (title, filterDesc) =>
        {
            var patterns = new List<string>();
            foreach (var part in filterDesc.Split('|'))
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("*.")) patterns.Add(trimmed);
                else if (trimmed.Contains("*."))
                {
                    foreach (var token in trimmed.Split(';'))
                    {
                        var t = token.Trim();
                        if (t.StartsWith("*.")) patterns.Add(t);
                    }
                }
            }
            if (patterns.Count == 0) patterns.Add("*");

            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(title) { Patterns = patterns },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            });
            return files.Count > 0 ? files[0].TryGetLocalPath() : null;
        };
    }

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var aboutWindow = new Window
        {
            Title = "About",
            Width = 380,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{MainWindowViewModel.AppName}",
                        FontSize = 16, FontWeight = Avalonia.Media.FontWeight.Bold
                    },
                    new TextBlock
                    {
                        Text = $"Build {BuildInfo.VerMajor}.{BuildInfo.VerMinor}.{BuildInfo.VerPatch} ({MainWindowViewModel.SuppGameRel})"
                    },
                    new TextBlock { Text = "" },
                    new TextBlock { Text = "by vector_cmdr, TowaNoah" },
                    new TextBlock
                    {
                        Text = MainWindowViewModel.GitHubCreatorUrl,
                        Foreground = this.TryFindResource("SemiColorLink", ActualThemeVariant, out var linkRes) && linkRes is Avalonia.Media.IBrush linkBrush
                            ? linkBrush
                            : Avalonia.Media.Brushes.CornflowerBlue,
                        Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    }
                }
            }
        };

        await aboutWindow.ShowDialog(this);
    }

}
