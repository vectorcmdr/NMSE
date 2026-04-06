using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class RecipeRowViewModel : ObservableObject
{
    [ObservableProperty] private string _inputs = "";
    [ObservableProperty] private string _output = "";
    [ObservableProperty] private int _time;
    [ObservableProperty] private string _type = "";
    [ObservableProperty] private string _recipeName = "";

    public Recipe? Source { get; init; }
}

public partial class RecipeViewModel : PanelViewModelBase
{
    private RecipeDatabase? _recipeDb;
    private GameItemDatabase? _itemDb;

    [ObservableProperty] private ObservableCollection<RecipeRowViewModel> _recipes = new();
    [ObservableProperty] private RecipeRowViewModel? _selectedRecipe;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private int _selectedFilterIndex;
    [ObservableProperty] private string _detailText = "Select a recipe to see details.";

    public string[] FilterOptions { get; } = ["All", "Refining", "Cooking"];

    partial void OnSearchTextChanged(string value) => PopulateGrid();
    partial void OnSelectedFilterIndexChanged(int value) => PopulateGrid();

    partial void OnSelectedRecipeChanged(RecipeRowViewModel? value)
    {
        if (value?.Source == null)
        {
            DetailText = "Select a recipe to see details.";
            return;
        }

        var recipe = value.Source;
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(recipe.RecipeName))
            parts.Add($"Recipe: {recipe.RecipeName}");
        parts.Add($"Type: {(recipe.Cooking ? "Cooking" : "Refining")}");
        parts.Add($"Time: {recipe.TimeToMake}s");

        if (recipe.Ingredients.Length > 0)
        {
            var ingNames = recipe.Ingredients.Select(i =>
            {
                string name = _itemDb?.GetItem(i.Id)?.Name ?? i.Id;
                return $"{i.Amount}x {name} ({i.Id})";
            });
            parts.Add($"Ingredients: {string.Join(", ", ingNames)}");
        }

        if (recipe.Result != null)
        {
            string resultName = _itemDb?.GetItem(recipe.Result.Id)?.Name ?? recipe.Result.Id;
            parts.Add($"Result: {recipe.Result.Amount}x {resultName} ({recipe.Result.Id})");
        }

        DetailText = string.Join("  |  ", parts);
    }

    public void SetDatabases(RecipeDatabase? recipeDb, GameItemDatabase? itemDb)
    {
        _recipeDb = recipeDb;
        _itemDb = itemDb;
        PopulateGrid();
    }

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _itemDb = database;
    }

    public override void SaveData(JsonObject saveData) { }

    private void PopulateGrid()
    {
        Recipes.Clear();
        SelectedRecipe = null;
        if (_recipeDb == null) return;

        string filterType = SelectedFilterIndex >= 0 && SelectedFilterIndex < FilterOptions.Length
            ? FilterOptions[SelectedFilterIndex]
            : "All";
        string search = SearchText?.Trim() ?? "";

        foreach (var recipe in _recipeDb.Recipes)
        {
            if (filterType == "Refining" && recipe.Cooking) continue;
            if (filterType == "Cooking" && !recipe.Cooking) continue;

            string inputs = string.Join(" + ", recipe.Ingredients.Select(i =>
            {
                string name = _itemDb?.GetItem(i.Id)?.Name ?? i.Id;
                return $"{i.Amount}x {name}";
            }));
            string output = recipe.Result != null
                ? $"{recipe.Result.Amount}x {(_itemDb?.GetItem(recipe.Result.Id)?.Name ?? recipe.Result.Id)}"
                : "";

            if (!string.IsNullOrEmpty(search))
            {
                bool match = inputs.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || output.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || recipe.RecipeName.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!match) continue;
            }

            Recipes.Add(new RecipeRowViewModel
            {
                Inputs = inputs,
                Output = output,
                Time = recipe.TimeToMake,
                Type = recipe.Cooking ? "Cooking" : "Refining",
                RecipeName = recipe.RecipeName,
                Source = recipe
            });
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        SelectedFilterIndex = 0;
    }

    [RelayCommand]
    private void OpenNmsRecipes()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://nomansskyrecipes.com/",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
