using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Panels;

public partial class RawJsonViewModel : PanelViewModelBase
{
    private JsonObject? _saveData;

    [ObservableProperty] private bool _isTreeView = true;
    [ObservableProperty] private string _jsonText = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _searchQuery = "";

    [ObservableProperty] private ObservableCollection<JsonTreeNodeViewModel> _treeNodes = new();

    public override void LoadData(JsonObject saveData, GameItemDatabase database, IconManager? iconManager)
    {
        _saveData = saveData;
        if (IsTreeView)
            BuildTree(saveData);
        else
            JsonText = RawJsonLogic.ToDisplayString(saveData);
        StatusText = $"Loaded: {saveData.Size():N0} keys";
    }

    public void RefreshTree()
    {
        if (_saveData == null) return;
        if (IsTreeView)
            BuildTree(_saveData);
        else
            JsonText = RawJsonLogic.ToDisplayString(_saveData);
    }

    private void BuildTree(JsonObject root)
    {
        TreeNodes.Clear();
        var rootNode = new JsonTreeNodeViewModel("Root", root, true);
        PopulateNode(rootNode, root, 2, 0);
        TreeNodes.Add(rootNode);
    }

    private void PopulateNode(JsonTreeNodeViewModel parentNode, object container, int maxDepth, int currentDepth)
    {
        if (container is JsonObject obj)
        {
            var names = obj.Names();
            for (int i = 0; i < names.Count; i++)
            {
                string key = names[i];
                object? val = obj.Get(key);
                var child = CreateNode(key, val, maxDepth, currentDepth);
                parentNode.Children.Add(child);
            }
        }
        else if (container is JsonArray arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                object? val = arr.Get(i);
                var child = CreateNode($"[{i}]", val, maxDepth, currentDepth);
                parentNode.Children.Add(child);
            }
        }
    }

    private JsonTreeNodeViewModel CreateNode(string key, object? value, int maxDepth, int currentDepth)
    {
        if (value is JsonObject childObj)
        {
            var node = new JsonTreeNodeViewModel($"{key}  {{...}}  ({childObj.Size()} properties)", childObj, true);
            if (currentDepth < maxDepth)
                PopulateNode(node, childObj, maxDepth, currentDepth + 1);
            return node;
        }
        if (value is JsonArray childArr)
        {
            var node = new JsonTreeNodeViewModel($"{key}  [...]  ({childArr.Length} items)", childArr, true);
            if (currentDepth < maxDepth)
                PopulateNode(node, childArr, maxDepth, currentDepth + 1);
            return node;
        }

        string displayVal = FormatValue(value);
        return new JsonTreeNodeViewModel($"{key} : {displayVal}", value, false);
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        bool b => b ? "true" : "false",
        _ => value.ToString() ?? "null"
    };

    [RelayCommand]
    private void SwitchToTreeView()
    {
        IsTreeView = true;
        if (_saveData != null)
        {
            try
            {
                if (!string.IsNullOrEmpty(JsonText))
                {
                    var parsed = RawJsonLogic.ParseJson(JsonText);
                    _saveData = parsed;
                }
                BuildTree(_saveData);
                StatusText = "Tree view rebuilt.";
            }
            catch (Exception ex)
            {
                StatusText = $"Parse error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void SwitchToTextView()
    {
        IsTreeView = false;
        if (_saveData != null)
            JsonText = RawJsonLogic.ToDisplayString(_saveData);
    }

    [RelayCommand]
    private void FormatJson()
    {
        try
        {
            JsonText = RawJsonLogic.FormatJson(JsonText);
            StatusText = "JSON formatted.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ValidateJson()
    {
        try
        {
            RawJsonLogic.ParseJson(JsonText);
            StatusText = "JSON is valid.";
        }
        catch (Exception ex)
        {
            StatusText = $"Invalid: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var node in TreeNodes)
            ExpandNode(node);
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var node in TreeNodes)
            CollapseNode(node);
    }

    private static void ExpandNode(JsonTreeNodeViewModel node)
    {
        node.IsExpanded = true;
        foreach (var child in node.Children)
            ExpandNode(child);
    }

    private static void CollapseNode(JsonTreeNodeViewModel node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children)
            CollapseNode(child);
    }

    public override void SaveData(JsonObject saveData)
    {
        // Tree edits are applied directly to the JsonObject
    }
}

public partial class JsonTreeNodeViewModel : ObservableObject
{
    [ObservableProperty] private string _displayText = "";
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private ObservableCollection<JsonTreeNodeViewModel> _children = new();

    public object? Value { get; set; }
    public bool IsContainer { get; set; }

    public JsonTreeNodeViewModel(string displayText, object? value, bool isContainer)
    {
        DisplayText = displayText;
        Value = value;
        IsContainer = isContainer;
    }
}
