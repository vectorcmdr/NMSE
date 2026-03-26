using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class DiscoveryViewModel : PanelViewModelBase
{
    private JsonObject? _playerState;
    private GameItemDatabase? _database;

    [ObservableProperty] private int _selectedTabIndex;

    public RecipeViewModel Recipe { get; } = new();

    [ObservableProperty] private ObservableCollection<DiscoveryItemViewModel> _knownTechs = new();
    [ObservableProperty] private ObservableCollection<DiscoveryItemViewModel> _knownProducts = new();
    [ObservableProperty] private ObservableCollection<GlyphViewModel> _glyphs = new();
    [ObservableProperty] private ObservableCollection<FishEntryViewModel> _fishEntries = new();

    [ObservableProperty] private string _techFilter = "";
    [ObservableProperty] private string _productFilter = "";

    [ObservableProperty] private DiscoveryItemViewModel? _selectedTech;
    [ObservableProperty] private DiscoveryItemViewModel? _selectedProduct;

    [ObservableProperty] private string _statusText = "";

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _database = database;
        Recipe.LoadData(saveData, database, iconManager);
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;
            _playerState = playerState;

            LoadKnownItems(playerState, "KnownTech", KnownTechs);
            LoadKnownItems(playerState, "KnownProducts", KnownProducts);
            LoadGlyphs(playerState);
            LoadFish(playerState);
        }
        catch { }
    }

    private void LoadKnownItems(JsonObject playerState, string arrayName, ObservableCollection<DiscoveryItemViewModel> target)
    {
        target.Clear();
        var ids = DiscoveryLogic.LoadKnownItemIds(playerState, arrayName);
        foreach (var id in ids)
        {
            var dbItem = _database?.GetItem(id);
            target.Add(new DiscoveryItemViewModel
            {
                Id = id,
                Name = dbItem?.Name ?? id,
                Category = dbItem?.ItemType ?? ""
            });
        }
    }

    private void LoadGlyphs(JsonObject playerState)
    {
        Glyphs.Clear();
        int runesBitfield = DiscoveryLogic.LoadGlyphBitfield(playerState);
        for (int i = 0; i < 16; i++)
        {
            int mask = 1 << i;
            Glyphs.Add(new GlyphViewModel
            {
                Index = i,
                Label = $"Glyph {i + 1}",
                IsKnown = (runesBitfield & mask) == mask
            });
        }
    }

    private void LoadFish(JsonObject playerState)
    {
        FishEntries.Clear();
        try
        {
            var fishingRecord = playerState.GetObject("FishingRecord");
            if (fishingRecord == null) return;

            var productList = fishingRecord.GetArray("ProductList");
            var countList = fishingRecord.GetArray("ProductCountList");
            var largestList = fishingRecord.GetArray("LargestCatchList");
            if (productList == null) return;

            for (int i = 0; i < productList.Length; i++)
            {
                string productId = productList.GetString(i) ?? "";
                if (string.IsNullOrEmpty(productId) || productId == "^") continue;

                int catchCount = 0;
                double largestCatch = 0;
                if (countList != null && i < countList.Length)
                    try { catchCount = countList.GetInt(i); } catch { }
                if (largestList != null && i < largestList.Length)
                    try { largestCatch = largestList.GetDouble(i); } catch { }

                string lookupId = productId.StartsWith('^') ? productId[1..] : productId;
                var dbItem = string.IsNullOrEmpty(lookupId) ? null : _database?.GetItem(lookupId);

                FishEntries.Add(new FishEntryViewModel
                {
                    ProductId = productId,
                    Name = dbItem?.Name ?? lookupId,
                    CatchCount = catchCount,
                    LargestCatch = largestCatch,
                    ArrayIndex = i
                });
            }
        }
        catch { }
    }

    [RelayCommand]
    private void RemoveTech()
    {
        if (SelectedTech != null)
        {
            KnownTechs.Remove(SelectedTech);
            SelectedTech = null;
        }
    }

    [RelayCommand]
    private void RemoveProduct()
    {
        if (SelectedProduct != null)
        {
            KnownProducts.Remove(SelectedProduct);
            SelectedProduct = null;
        }
    }

    [RelayCommand]
    private void LearnAllGlyphs()
    {
        foreach (var g in Glyphs) g.IsKnown = true;
    }

    [RelayCommand]
    private void UnlearnAllGlyphs()
    {
        foreach (var g in Glyphs) g.IsKnown = false;
    }

    public override void SaveData(JsonObject saveData)
    {
        var playerState = saveData.GetObject("PlayerStateData");
        if (playerState == null) return;

        SaveKnownItems(playerState, "KnownTech", KnownTechs);
        SaveKnownItems(playerState, "KnownProducts", KnownProducts);
        SaveGlyphs(playerState);
    }

    private static void SaveKnownItems(JsonObject playerState, string arrayName, ObservableCollection<DiscoveryItemViewModel> items)
    {
        var ids = items.Select(i => i.Id).ToList();
        DiscoveryLogic.SaveKnownItemIds(playerState, arrayName, ids);
    }

    private void SaveGlyphs(JsonObject playerState)
    {
        int runesBitfield = 0;
        for (int i = 0; i < Glyphs.Count && i < 16; i++)
        {
            if (Glyphs[i].IsKnown)
                runesBitfield |= (1 << i);
        }
        DiscoveryLogic.SaveGlyphBitfield(playerState, runesBitfield);
    }

    [RelayCommand]
    private void ClearTechFilter() { TechFilter = ""; }

    [RelayCommand]
    private void ClearProductFilter() { ProductFilter = ""; }

    [RelayCommand]
    private void AddTech() { /* TODO: open item picker for tech */ }

    [RelayCommand]
    private void AddProduct() { /* TODO: open item picker for products */ }

    [RelayCommand]
    private void ExportTech() { /* TODO: export known tech */ }

    [RelayCommand]
    private void ImportTech() { /* TODO: import known tech */ }

    [RelayCommand]
    private void ExportProduct() { /* TODO: export known products */ }

    [RelayCommand]
    private void ImportProduct() { /* TODO: import known products */ }
}

public partial class DiscoveryItemViewModel : ObservableObject
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _category = "";
}

public partial class GlyphViewModel : ObservableObject
{
    [ObservableProperty] private int _index;
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private bool _isKnown;
}

public partial class FishEntryViewModel : ObservableObject
{
    [ObservableProperty] private string _productId = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _catchCount;
    [ObservableProperty] private double _largestCatch;
    [ObservableProperty] private int _arrayIndex;
}
