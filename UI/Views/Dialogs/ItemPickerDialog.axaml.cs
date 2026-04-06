using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NMSE.Data;
using NMSE.UI.ViewModels.Dialogs;

namespace NMSE.UI.Views.Dialogs;

public partial class ItemPickerDialog : Window
{
    private readonly ItemPickerViewModel _viewModel;

    public string? SelectedItemId => _viewModel.HasResult ? _viewModel.ResultItemId : null;
    public List<string> SelectedItemIds => _viewModel.ResultItemIds;

    public ItemPickerDialog()
    {
        InitializeComponent();
        _viewModel = new ItemPickerViewModel();
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ItemPickerViewModel.HasResult) && _viewModel.HasResult)
                Close(_viewModel.ResultItemId);
        };

        ItemGrid.SelectionChanged += OnGridSelectionChanged;
    }

    public void Initialize(GameItemDatabase database, IconManager? iconManager,
        string? filterCategory = null, string? filterType = null, bool multiSelect = false)
    {
        _viewModel.AllowMultiSelect = multiSelect;
        _viewModel.Initialize(database, iconManager, filterCategory, filterType);
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _viewModel.SelectedItems.Clear();
        foreach (var item in ItemGrid.SelectedItems.OfType<ItemPickerItemViewModel>())
            _viewModel.SelectedItems.Add(item);

        OkButton.IsEnabled = _viewModel.SelectedItems.Count > 0 || _viewModel.SelectedItem != null;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
