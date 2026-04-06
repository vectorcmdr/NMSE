using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Data;

namespace NMSE.UI.ViewModels.Dialogs;

public partial class ItemPickerItemViewModel : ObservableObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Type { get; set; } = "";
    public Bitmap? Icon { get; set; }
}

public partial class ItemPickerViewModel : ViewModelBase
{
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private List<GameItem> _allItems = new();
    private string? _filterCategory;
    private string? _filterType;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ObservableCollection<string> _typeFilters = new();
    [ObservableProperty] private int _selectedTypeIndex = -1;
    [ObservableProperty] private ObservableCollection<string> _categoryFilters = new();
    [ObservableProperty] private int _selectedCategoryIndex = -1;
    [ObservableProperty] private ObservableCollection<ItemPickerItemViewModel> _filteredItems = new();
    [ObservableProperty] private ItemPickerItemViewModel? _selectedItem;
    [ObservableProperty] private string _manualItemId = "";
    [ObservableProperty] private string _resultItemId = "";
    [ObservableProperty] private bool _hasResult;
    [ObservableProperty] private bool _allowMultiSelect;

    public List<string> ResultItemIds { get; } = new();
    public List<ItemPickerItemViewModel> SelectedItems { get; } = new();

    public void Initialize(GameItemDatabase database, IconManager? iconManager,
        string? filterCategory = null, string? filterType = null)
    {
        _database = database;
        _iconManager = iconManager;
        _filterCategory = filterCategory;
        _filterType = filterType;

        _allItems = database.Items.Values.ToList();

        if (!string.IsNullOrEmpty(filterCategory))
            _allItems = _allItems.Where(i => i.Category == filterCategory).ToList();

        if (!string.IsNullOrEmpty(filterType))
            _allItems = _allItems.Where(i => i.ItemType == filterType).ToList();

        var types = _allItems.Select(i => i.ItemType ?? "").Distinct().OrderBy(t => t).ToList();
        types.Insert(0, "(All)");
        TypeFilters = new ObservableCollection<string>(types);
        SelectedTypeIndex = 0;

        ApplyFilters();
    }

    partial void OnSelectedTypeIndexChanged(int value)
    {
        if (value <= 0) return;

        var selectedType = value > 0 && value < TypeFilters.Count ? TypeFilters[value] : null;
        var categories = _allItems
            .Where(i => selectedType == null || selectedType == "(All)" || i.ItemType == selectedType)
            .Select(i => i.Category ?? "").Distinct().OrderBy(c => c).ToList();
        categories.Insert(0, "(All)");
        CategoryFilters = new ObservableCollection<string>(categories);
        SelectedCategoryIndex = 0;

        ApplyFilters();
    }

    partial void OnSelectedCategoryIndexChanged(int value) => ApplyFilters();

    [RelayCommand]
    private void Search() => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (SelectedTypeIndex > 0 && SelectedTypeIndex < TypeFilters.Count)
        {
            string type = TypeFilters[SelectedTypeIndex];
            if (type != "(All)")
                filtered = filtered.Where(i => i.ItemType == type);
        }

        if (SelectedCategoryIndex > 0 && SelectedCategoryIndex < CategoryFilters.Count)
        {
            string cat = CategoryFilters[SelectedCategoryIndex];
            if (cat != "(All)")
                filtered = filtered.Where(i => i.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string search = SearchText.Trim();
            filtered = filtered.Where(i =>
                (i.Name ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (i.Id ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredItems = new ObservableCollection<ItemPickerItemViewModel>(
            filtered.OrderBy(i => i.Name).Take(200).Select(i => new ItemPickerItemViewModel
            {
                Id = i.Id ?? "",
                Name = i.Name ?? i.Id ?? "",
                Category = i.Category ?? "",
                Type = i.ItemType ?? "",
                Icon = _iconManager?.GetIcon(i.Icon)
            }));
    }

    [RelayCommand]
    private void ConfirmSelection()
    {
        if (AllowMultiSelect && SelectedItems.Count > 0)
        {
            ResultItemIds.Clear();
            ResultItemIds.AddRange(SelectedItems.Select(i => i.Id));
            ResultItemId = ResultItemIds[0];
            HasResult = true;
        }
        else if (SelectedItem != null)
        {
            ResultItemIds.Clear();
            ResultItemIds.Add(SelectedItem.Id);
            ResultItemId = SelectedItem.Id;
            HasResult = true;
        }
        else if (!string.IsNullOrWhiteSpace(ManualItemId))
        {
            ResultItemIds.Clear();
            ResultItemIds.Add(ManualItemId.Trim());
            ResultItemId = ManualItemId.Trim();
            HasResult = true;
        }
    }

    [RelayCommand]
    private void ConfirmManualId()
    {
        if (!string.IsNullOrWhiteSpace(ManualItemId))
        {
            ResultItemId = ManualItemId.Trim();
            HasResult = true;
        }
    }
}
