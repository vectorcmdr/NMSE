using System.ComponentModel;
using System.Text.RegularExpressions;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;
using NMSE.UI.Util;

namespace NMSE.UI.Panels;

/// <summary>
/// A reusable inventory grid panel that displays slots as a visual grid of cells
/// with item icons and a side detail panel for viewing and editing slot properties.
/// Supports right-click context menu for adding/removing items, and a cascading
/// item picker (Type -> Category -> Item) for selecting items from the game database.
/// </summary>
public partial class InventoryGridPanel : UserControl
{
    public sealed class AutoStackSlotRequestEventArgs : EventArgs
    {
        public AutoStackSlotRequestEventArgs(int x, int y, string itemId)
        {
            X = x;
            Y = y;
            ItemId = itemId;
        }

        public int X { get; }
        public int Y { get; }
        public string ItemId { get; }
    }

    private enum InventorySortMode
    {
        None,
        Name,
        Category,
    }

    private sealed class SortModeOption
    {
        public required InventorySortMode Mode { get; init; }
        public required string Label { get; init; }

        public override string ToString() => Label;
    }

    // Cells / grid items
    private const int GridColumns = 10;
    private const int CellWidth = 72;
    private const int CellHeight = 104;
    private const int CellPadding = 4;
    private const string SuperchargeIndicator = "⚡ ";

    // State
    private readonly ToolTip _sharedToolTip = new();
    private readonly List<SlotCell> _cells = new();
    private SlotCell? _selectedCell;
    private SlotCell? _contextCell; // Cell that was right-clicked
    private SlotCell? _copiedItemCell;
    private JsonArray? _slots;
    private JsonObject? _currentInventory;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;
    private Label? _maxSupportedLabel;

    // Drag-and-drop state
    private const int DragThreshold = 6; // Minimum pixels before a drag starts
    private SlotCell? _dragSourceCell;
    private Point _dragStartPoint;
    private bool _isDragging;
    private SlotCell? _dragHighlightCell; // Currently highlighted drop target

    // Cached item lists for filtering
    private List<GameItem> _allItems = new();
    private bool _suppressFilterEvents;
    private bool _filtersPopulated;

    // Export filename for this inventory
    private string _exportFileName = "inventory.json";

    // Context-aware file dialog filter and default extension for export/import
    private string _exportFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
    private string _importFilter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
    private string _exportDefaultExt = "json";

    // Corvette tech resolution context
    private List<CorvettePartEntry>? _corvettePartCollection;

    // Inventory group for stack-size lookups (e.g. "Personal", "Ship", "Freighter")
    private string _inventoryGroup = "Default";
    /// <summary>
    /// Sets the inventory group used to look up default stack sizes from
    /// <see cref="InventoryStackDatabase"/> when the user picks an item.
    /// Valid groups: Default, Personal, PersonalCargo, Ship, ShipCargo,
    /// Freighter, FreighterCargo, Vehicle, Chest, BaseCapsule,
    /// MaintenanceObject, UIPopup, SeasonTransfer.
    /// </summary>
    public void SetInventoryGroup(string group)
    {
        _inventoryGroup = group;
    }

    private bool _pinSlotFeatureEnabled;
    private readonly HashSet<(int x, int y)> _pinnedSlots = new();

    /// <summary>
    /// Raised when pinned-slot coordinates change.
    /// </summary>
    public event EventHandler? PinnedSlotsChanged;

    public void SetPinSlotFeatureEnabled(bool enabled)
    {
        _pinSlotFeatureEnabled = enabled;
        UpdatePinnedVisuals();
    }

    public void SetPinnedSlots(IEnumerable<(int x, int y)> pinnedSlots)
    {
        _pinnedSlots.Clear();
        foreach (var pos in pinnedSlots)
            _pinnedSlots.Add(pos);
        UpdatePinnedVisuals();
    }

    public IReadOnlyCollection<(int x, int y)> GetPinnedSlots() => _pinnedSlots.ToArray();

    private bool IsPinnedSlot(int x, int y) => _pinnedSlots.Contains((x, y));

    private void RaisePinnedSlotsChanged() => PinnedSlotsChanged?.Invoke(this, EventArgs.Empty);

    private void UpdatePinnedVisuals()
    {
        if (_cells.Count == 0)
            return;

        foreach (var cell in _cells)
        {
            cell.ShowPinToggle = _pinSlotFeatureEnabled && cell.IsActivated;
            cell.IsPinnedForAutoStack = cell.IsActivated && IsPinnedSlot(cell.GridX, cell.GridY);
            cell.UpdateDisplay();
        }
    }

    private bool _sortingEnabled;
    private InventorySortMode _currentSortMode;
    private bool _suppressSortModeEvents;
    private bool _isApplyingSort;

    public void SetSortingEnabled(bool enabled)
    {
        _sortingEnabled = enabled;
        UpdateToolbarActionVisibility();
    }

    // Identify storages that can't be resized
    private bool _isStorageInventory = false;
    public void SetIsStorageInventory(bool isStorage)
    {
        _isStorageInventory = isStorage;
        UpdateToolbarActionVisibility();
    }

    // Identify chest inventories that can be resized (up to 10x12)
    private bool _isChestInventory = false;
    public void SetIsChestInventory(bool isChest)
    {
        _isChestInventory = isChest;
    }

    // Identify technology-only inventories for charge display and context menu
    private bool _isTechInventory = false;
    public void SetIsTechInventory(bool isTech)
    {
        _isTechInventory = isTech;
    }

    // Whether this inventory is a cargo/general inventory (no technology allowed).
    // Tech items cannot be added to non-tech inventories (post-Waypoint).
    private bool _isCargoInventory = false;
    /// <summary>
    /// Mark this inventory as a cargo/general inventory that should not accept
    /// technology items. This prevents users from corrupting their save by
    /// adding technology to a slot that only supports products/substances.
    /// </summary>
    public void SetIsCargoInventory(bool isCargo)
    {
        _isCargoInventory = isCargo;
        UpdateToolbarActionVisibility();
    }

    // Owner type for TechnologyCategory-based item filtering.
    // Values: "Suit", "Ship"/"Starship", "Weapon"/"Multitool", "Freighter", "Vehicle"/"Exocraft".
    private string _inventoryOwnerType = "";

    // When > 0, suppress automatic filter refresh from SetInventoryOwnerType.
    // Call EndBatchUpdate() to apply deferred refreshes.
    private int _batchUpdateCount;
    private bool _filterRefreshDeferred;

    /// <summary>
    /// Begins a batch update.  While in batch mode, <see cref="SetInventoryOwnerType"/>
    /// will not automatically refresh item filters.  Call <see cref="EndBatchUpdate"/>
    /// when done to apply any deferred filter refreshes in a single pass.
    /// Calls may be nested; only the outermost EndBatchUpdate triggers the refresh.
    /// </summary>
    public void BeginBatchUpdate()
    {
        _batchUpdateCount++;
    }

    /// <summary>
    /// Ends a batch update started by <see cref="BeginBatchUpdate"/>. If any
    /// filter-refresh was deferred during the batch, it is applied now.
    /// </summary>
    public void EndBatchUpdate()
    {
        if (_batchUpdateCount > 0)
            _batchUpdateCount--;

        if (_batchUpdateCount == 0 && _filterRefreshDeferred)
        {
            _filterRefreshDeferred = false;
            if (_filtersPopulated)
                PopulateTypeFilter();
        }
    }

    /// <summary>
    /// Sets the owner type for this inventory, used to filter the item picker
    /// to only show technologies valid for this entity type.
    /// </summary>
    public void SetInventoryOwnerType(string ownerType)
    {
        string previous = _inventoryOwnerType;
        _inventoryOwnerType = ownerType;

        // Auto-refresh the item picker filters when the owner type actually
        // changes and they have already been built.  This ensures switching
        // between vehicles / ships of different types immediately updates the
        // available categories without requiring callers to call RefreshItemFilters.
        if (_filtersPopulated && !string.Equals(previous, ownerType, StringComparison.Ordinal))
        {
            if (_batchUpdateCount > 0)
                _filterRefreshDeferred = true;
            else
                PopulateTypeFilter();
        }
    }

    /// <summary>
    /// Re-populates the item picker type/category/item filters using the current
    /// owner type and inventory flags. Call after changing the owner type so
    /// the picker reflects the updated filtering (e.g. switching to Corvette).
    /// </summary>
    public void RefreshItemFilters()
    {
        if (_filtersPopulated)
            PopulateTypeFilter();
    }

    // Disable supercharge modifications (e.g. Exocraft tech slots are fixed in game)
    private bool _superchargeDisabled = false;
    public void SetSuperchargeDisabled(bool disabled)
    {
        _superchargeDisabled = disabled;
    }

    // Disable slot enable/disable toggle (e.g. Exocraft tech slots have fixed layout in game)
    private bool _slotToggleDisabled = false;
    /// <summary>
    /// Disables the ability to enable/disable slots in this inventory grid.
    /// Used for inventories where the slot layout is fixed in-game (e.g. Exocraft tech).
    /// </summary>
    public void SetSlotToggleDisabled(bool disabled)
    {
        _slotToggleDisabled = disabled;
    }

    // Supercharge constraints (e.g. Multitool/Starship/Freighter tech: max 4, rows 0-3 only)
    private int _maxSuperchargedSlots = -1; // -1 = unlimited
    private int _maxSuperchargeRow = -1;    // -1 = no row restriction (0-based)
    public void SetSuperchargeConstraints(int maxSlots, int maxRow)
    {
        _maxSuperchargedSlots = maxSlots;
        _maxSuperchargeRow = maxRow;
    }

    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;
    private void RaiseDataModified() => DataModified?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Raised when the user requests moving Exosuit cargo items into matching chest stacks.
    /// The parent panel executes the cross-inventory transfer.
    /// </summary>
    public event EventHandler? AutoStackToStorageRequested;

    /// <summary>
    /// Raised when the user requests moving Exosuit cargo items into the current Starship cargo inventory.
    /// </summary>
    public event EventHandler? AutoStackToStarshipRequested;

    /// <summary>
    /// Raised when the user requests moving Exosuit cargo items into the Freighter cargo inventory.
    /// </summary>
    public event EventHandler? AutoStackToFreighterRequested;

    /// <summary>
    /// Raised when the user requests a context-menu auto-stack operation for the selected slot to chests.
    /// </summary>
    public event EventHandler<AutoStackSlotRequestEventArgs>? AutoStackSelectedSlotToStorageRequested;

    /// <summary>
    /// Raised when the user requests a context-menu auto-stack operation for the selected slot to starship.
    /// </summary>
    public event EventHandler<AutoStackSlotRequestEventArgs>? AutoStackSelectedSlotToStarshipRequested;

    /// <summary>
    /// Raised when the user requests a context-menu auto-stack operation for the selected slot to freighter.
    /// </summary>
    public event EventHandler<AutoStackSlotRequestEventArgs>? AutoStackSelectedSlotToFreighterRequested;

    public void RefreshToolbarActions()
    {
        UpdateToolbarActionVisibility();
        UpdateToolbarActionEnabledState();
    }

    /// <summary>
    /// Sets the default filename used when exporting this inventory.
    /// </summary>
    public void SetExportFileName(string fileName)
    {
        _exportFileName = fileName;
    }

    /// <summary>
    /// Sets the file dialog filter and default extension for context-aware export/import.
    /// Call this to override the default .json filter based on the parent panel context.
    /// </summary>
    /// <param name="exportFilter">SaveFileDialog filter string using pipe-delimited format
    /// (e.g. "Ship files (*.nmssc)|*.nmssc|JSON files (*.json)|*.json|All files (*.*)|*.*").
    /// Use <see cref="ExportConfig.BuildDialogFilter"/> to build this.</param>
    /// <param name="importFilter">OpenFileDialog filter string using pipe-delimited format.
    /// Use <see cref="ExportConfig.BuildImportFilter"/> to build this.</param>
    /// <param name="defaultExt">Default extension without leading dot (e.g. "nmssc").
    /// Used for both export and import dialogs.</param>
    public void SetExportFileFilter(string exportFilter, string importFilter, string defaultExt)
    {
        _exportFilter = exportFilter;
        _importFilter = importFilter;
        _exportDefaultExt = defaultExt;
    }

    /// <summary>
    /// Initialises corvette tech resolution context for this inventory.
    /// Call this before LoadInventory when loading a corvette's tech inventory.
    /// Finds the PlayerShipBase entry matching the ship index and collects its
    /// base part objects for matching against CV_ tech items.
    /// </summary>
    public void SetCorvetteContext(JsonObject? saveRoot, int shipIndex)
    {
        _corvettePartCollection = null;
        if (saveRoot == null || _database == null) return;

        try
        {
            var playerState = saveRoot.GetObject("PlayerStateData");
            if (playerState == null) return;
            var bases = playerState.GetArray("PersistentPlayerBases");
            if (bases == null) return;

            for (int i = 0; i < bases.Length; i++)
            {
                var baseObj = bases.GetObject(i);
                if (baseObj == null) continue;
                var baseType = baseObj.GetObject("BaseType");
                if (baseType == null) continue;
                if ((string?)baseType.Get("PersistentBaseTypes") != "PlayerShipBase") continue;
                if (baseObj.GetInt("UserData") != shipIndex) continue;

                // Found the matching base - collect all objects
                var objects = baseObj.GetArray("Objects");
                if (objects == null) break;

                _corvettePartCollection = new List<CorvettePartEntry>();
                for (int j = 0; j < objects.Length; j++)
                {
                    var obj = objects.GetObject(j);
                    if (obj == null) continue;
                    string objectId = (string?)obj.Get("ObjectID") ?? "";
                    if (objectId.Length > 1 && objectId[0] == '^')
                        objectId = objectId[1..];
                    _corvettePartCollection.Add(new CorvettePartEntry { Id = objectId, Found = false });
                }
                break;
            }
        }
        catch { }
    }

    /// <summary>
    /// Clears corvette tech resolution context.
    /// </summary>
    public void ClearCorvetteContext()
    {
        _corvettePartCollection = null;
    }

    /// <summary>
    /// For a CV_ tech item, guesses which actual base part it corresponds to
    /// by matching the tech's possible base part refs against the corvette's
    /// actual Objects. Returns the matched GameItem (base part product) or null.
    /// Each base object is only matched once (greedy first-match).
    /// </summary>
    private GameItem? GuessCorvetteBasePart(string techId)
    {
        if (_corvettePartCollection == null || _database == null) return null;

        // Strip ^ and #variant from the tech ID to get base CV_ name
        string baseId = techId;
        if (baseId.Length > 0 && baseId[0] == '^') baseId = baseId[1..];
        var hashIdx = baseId.IndexOf('#');
        if (hashIdx >= 0) baseId = baseId[..hashIdx];

        // Look up possible base part refs for this CV_ tech
        if (!_database.CorvetteBasePartTechMap.TryGetValue(baseId, out var possibleParts))
            return null;

        // Iterate through base objects in order, find first unfound match
        foreach (var entry in _corvettePartCollection)
        {
            if (entry.Found) continue;
            foreach (var partId in possibleParts)
            {
                if (string.Equals(entry.Id, partId, StringComparison.OrdinalIgnoreCase))
                {
                    entry.Found = true;
                    return _database.GetItem(partId);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true if the item ID is a corvette tech (starts with ^CV_ or CV_).
    /// </summary>
    private static bool IsCorvetteTechId(string id)
    {
        var raw = id.StartsWith('^') ? id.AsSpan(1) : id.AsSpan();
        return raw.StartsWith("CV_", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets or updates the Max Supported label next to the Resize button.
    /// </summary>
    public void SetMaxSupportedLabel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            if (_maxSupportedLabel != null)
                _maxSupportedLabel.Visible = false;
            return;
        }

        if (_maxSupportedLabel == null)
        {
            _maxSupportedLabel = new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(8, 8, 0, 0)
            };

            _toolbarPanel?.Controls.Add(_maxSupportedLabel);
        }
        else
        {
            _maxSupportedLabel.Text = text;
            _maxSupportedLabel.Visible = true;
        }
    }

    public InventoryGridPanel()
    {
        InitializeComponent();
        SetupLayout();
        PopulateSortModeOptions();
        RefreshToolbarActions();
    }

    private void DisableControlsOnInit()
    {
        // Disable NUDs and buttons before inventory is loaded
        _resizeWidth.Enabled = false;
        _resizeHeight.Enabled = false;
        _resizeButton.Enabled = false;
        _detailAmount.Enabled = false;
        _detailMaxAmount.Enabled = false;
        _detailDamageFactor.Enabled = false;
        _applyButton.Enabled = false;
        _searchButton.Enabled = false;
        _typeFilter.Enabled = false;
        _categoryFilter.Enabled = false;
        _itemPicker.Enabled = false;
        UpdateToolbarActionEnabledState();
    }

    private void PopulateSortModeOptions()
    {
        _suppressSortModeEvents = true;
        _sortModeCombo.BeginUpdate();
        _sortModeCombo.Items.Clear();
        _sortModeCombo.Items.Add(new SortModeOption { Mode = InventorySortMode.None, Label = UiStrings.Get("inventory.sort_none") });
        _sortModeCombo.Items.Add(new SortModeOption { Mode = InventorySortMode.Name, Label = UiStrings.Get("inventory.sort_name") });
        _sortModeCombo.Items.Add(new SortModeOption { Mode = InventorySortMode.Category, Label = UiStrings.Get("inventory.sort_category") });
        _sortModeCombo.SelectedIndex = (int)_currentSortMode;
        _sortModeCombo.EndUpdate();
        _suppressSortModeEvents = false;
    }

    private void UpdateToolbarActionVisibility()
    {
        bool showSortControls = _sortingEnabled;
        bool showAutoStackControl = _isCargoInventory && !_isStorageInventory
            && (AutoStackToStorageRequested != null || AutoStackToStarshipRequested != null || AutoStackToFreighterRequested != null);

        _sortModeLabel.Visible = showSortControls;
        _sortModeCombo.Visible = showSortControls;
        _autoStackToolStrip.Visible = showAutoStackControl;

        _autoStackToChestsButtonMenuItem.Visible = AutoStackToStorageRequested != null;
        _autoStackToStarshipButtonMenuItem.Visible = AutoStackToStarshipRequested != null;
        _autoStackToFreighterButtonMenuItem.Visible = AutoStackToFreighterRequested != null;
    }

    private void UpdateToolbarActionEnabledState()
    {
        bool inventoryLoaded = _currentInventory != null;
        _sortModeCombo.Enabled = _sortingEnabled && inventoryLoaded;
        _autoStackToolStrip.Enabled = _isCargoInventory && !_isStorageInventory
            && (AutoStackToStorageRequested != null || AutoStackToStarshipRequested != null || AutoStackToFreighterRequested != null)
            && inventoryLoaded;

        _autoStackToChestsButtonMenuItem.Enabled = AutoStackToStorageRequested != null && inventoryLoaded;
        _autoStackToStarshipButtonMenuItem.Enabled = AutoStackToStarshipRequested != null && inventoryLoaded;
        _autoStackToFreighterButtonMenuItem.Enabled = AutoStackToFreighterRequested != null && inventoryLoaded;
    }

    private void SetSortMode(InventorySortMode mode, bool applySort, bool raiseModified)
    {
        _currentSortMode = mode;

        if (_sortModeCombo.SelectedIndex != (int)mode)
        {
            _suppressSortModeEvents = true;
            _sortModeCombo.SelectedIndex = (int)mode;
            _suppressSortModeEvents = false;
        }

        if (!applySort)
            return;

        ApplyCurrentSortMode(raiseModified);
    }

    private void ApplyCurrentSortMode(bool raiseModified)
    {
        switch (_currentSortMode)
        {
            case InventorySortMode.Name:
                SortInventory(CompareByName, raiseModified);
                break;
            case InventorySortMode.Category:
                SortInventory(CompareByCategory, raiseModified);
                break;
        }
    }

    private static int CompareByName(SlotSortEntry a, SlotSortEntry b)
    {
        int byName = string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
        if (byName != 0) return byName;
        int byCategory = string.Compare(a.SortCategory, b.SortCategory, StringComparison.OrdinalIgnoreCase);
        if (byCategory != 0) return byCategory;
        return string.Compare(a.ItemId, b.ItemId, StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareByCategory(SlotSortEntry a, SlotSortEntry b)
    {
        int byCategory = string.Compare(a.SortCategory, b.SortCategory, StringComparison.OrdinalIgnoreCase);
        if (byCategory != 0) return byCategory;
        int byName = string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
        if (byName != 0) return byName;
        return string.Compare(a.ItemId, b.ItemId, StringComparison.OrdinalIgnoreCase);
    }

    private static Label CreateLabel(string text) =>
        new Label { Text = text, AutoSize = true, Padding = new Padding(0, 4, 6, 0) };

    public void SetDatabase(GameItemDatabase? database)
    {
        _database = database;
        _filtersPopulated = false; // Reset so filters repopulate on next LoadInventory
        if (database != null)
        {
            _allItems = database.Items.Values
                .Where(i => !GameItemDatabase.IsPickerExcluded(i.Id))
                .ToList();
            // Defer PopulateTypeFilter until LoadInventory - avoids adding ~4800
            // items to three ComboBoxes for every InventoryGridPanel instance at
            // startup when the panel hasn't been displayed yet. The call happens
            // at the end of LoadInventory() which runs on first tab selection.
        }
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _iconManager = iconManager;
    }

    public JsonObject? GetLoadedInventory()
    {
        return _currentInventory;
    }

    private void PopulateTypeFilter()
    {
        _suppressFilterEvents = true;
        _typeFilter.BeginUpdate();
        _typeFilter.Items.Clear();
        _typeFilter.Items.Add(UiStrings.Get("common.all_types"));

        // Only include types that have at least one item passing the inventory
        // filter (tech-only / cargo / owner-type restrictions).  This prevents
        // showing irrelevant types (e.g. "Raw Materials" in a tech-only grid,
        // or "Exocraft" in a freighter tech grid) that lead to empty category
        // and item lists when selected.
        var types = _allItems
            .Where(CanAddItemToInventory)
            .Select(i => i.ItemType)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .Select(t => new TypeDisplayItem(t))
            .ToArray();
        _typeFilter.Items.AddRange(types);

        _typeFilter.SelectedIndex = 0;
        _typeFilter.EndUpdate();
        _suppressFilterEvents = false;
        UpdateCategoryFilter();
    }

    private void UpdateCategoryFilter()
    {
        _suppressFilterEvents = true;
        _categoryFilter.BeginUpdate();
        _categoryFilter.Items.Clear();
        _categoryFilter.Items.Add(UiStrings.Get("common.all_categories"));

        var filtered = GetTypeFilteredItems();
        var categories = filtered
            .Select(i => i.Category)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .Select(c => new CategoryDisplayItem(c))
            .ToArray();
        _categoryFilter.Items.AddRange(categories);

        _categoryFilter.SelectedIndex = 0;
        _categoryFilter.EndUpdate();
        _suppressFilterEvents = false;
        UpdateItemPicker();
    }

    private void UpdateItemPicker()
    {
        _suppressFilterEvents = true;
        _itemPicker.BeginUpdate();
        _itemPicker.Items.Clear();
        _itemPicker.Items.Add(UiStrings.Get("common.select_item"));

        var items = GetFilteredItems().OrderBy(i => i.Name).ToArray();
        _itemPicker.Items.AddRange(items);

        _itemPicker.SelectedIndex = 0;
        _itemPicker.EndUpdate();
        _suppressFilterEvents = false;
    }

    private IEnumerable<GameItem> GetTypeFilteredItems()
    {
        var items = (IEnumerable<GameItem>)_allItems;

        // Apply inventory type restrictions
        items = items.Where(CanAddItemToInventory);

        if (_typeFilter.SelectedIndex > 0)
        {
            string type;
            if (_typeFilter.SelectedItem is TypeDisplayItem tdi)
                type = tdi.InternalName;
            else
                type = _typeFilter.SelectedItem?.ToString() ?? "";
            items = items.Where(i => i.ItemType.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
        return items;
    }

    /// <summary>
    /// Determines whether an item can be added to this inventory:
    /// <list type="bullet">
    ///   <item>Tech-only inventories only accept technology items matching OwnerEnums.</item>
    ///   <item>Cargo inventories reject technology items entirely but accept all
    ///     products and substances regardless of TechnologyCategory.</item>
    ///   <item>General inventories accept all item types (tech items must match OwnerEnums).</item>
    /// </list>
    /// Also applies a filter to exclude maintenance-category technology items and
    /// non-pickupable base building products that cannot be picked up in-game.
    /// </summary>
    private bool CanAddItemToInventory(GameItem item)
    {
        if (!InventoryStackDatabase.CanAddItemToInventory(item, _isTechInventory, _isCargoInventory))
            return false;

        if (_isCargoInventory)
            return true;

        // For tech-only and general inventories, filter technology-related items by
        // TechnologyCategory / owner. This prevents e.g. ship tech appearing in
        // exosuit technology inventory. Only apply to technology-related items  - 
        // substances and products use Category for grouping (Fuel, Metal, etc.)
        // which is NOT a TechnologyCategory.
        if (!string.IsNullOrEmpty(_inventoryOwnerType) &&
            !string.IsNullOrEmpty(item.TechnologyCategory) &&
            item.TechnologyCategory != "None" &&
            GameItemDatabase.IsTechnologyRelatedType(item.ItemType))
        {
            if (!item.IsValidForOwner(_inventoryOwnerType))
                return false;
        }

        return true;
    }

    private IEnumerable<GameItem> GetFilteredItems()
    {
        var items = GetTypeFilteredItems();
        if (_categoryFilter.SelectedIndex > 0)
        {
            string cat;
            if (_categoryFilter.SelectedItem is CategoryDisplayItem cdi)
                cat = cdi.RawValue;
            else
                cat = _categoryFilter.SelectedItem?.ToString() ?? "";
            items = items.Where(i => i.Category.Equals(cat, StringComparison.OrdinalIgnoreCase));
        }
        return items;
    }

    private void OnSearch(object? sender, EventArgs e)
    {
        string raw = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(raw) || _database == null)
        {
            // Restore the full item list and refresh filters
            if (_database != null)
            {
                _allItems = _database.Items.Values
                    .Where(i => !GameItemDatabase.IsPickerExcluded(i.Id))
                    .ToList();
            }
            PopulateTypeFilter();
            return;
        }

        // Allow users to prefix a search with '^' (e.g. ^FUEL1) but strip it for the actual search.
        // TrimStart supports multiple carets but that's harmless.
        string query = raw.StartsWith("^") ? raw.TrimStart('^').Trim() : raw;

        // If stripping the caret produced an empty query, restore full list
        if (string.IsNullOrEmpty(query))
        {
            _allItems = _database.Items.Values
                .Where(i => !GameItemDatabase.IsPickerExcluded(i.Id))
                .ToList();
            PopulateTypeFilter();
            return;
        }

        _allItems = _database.Search(query)
            .Where(i => !GameItemDatabase.IsPickerExcluded(i.Id))
            .ToList();
        PopulateTypeFilter();
    }

    private void OnTypeFilterChanged(object? sender, EventArgs e)
    {
        if (_suppressFilterEvents) return;
        UpdateCategoryFilter();
    }

    private void OnCategoryFilterChanged(object? sender, EventArgs e)
    {
        if (_suppressFilterEvents) return;
        UpdateItemPicker();
    }

    private void OnItemPickerChanged(object? sender, EventArgs e)
    {
        if (_suppressFilterEvents) return;
        if (_itemPicker.SelectedItem is not GameItem selectedItem) return;

        // Populate detail fields with selected item - show ID with ^ prefix for consistency
        _detailItemId.Text = EnsureCaretPrefix(selectedItem.Id);
        _detailItemName.Text = selectedItem.Name;
        _detailItemType.Text = selectedItem.ItemType;
        _detailItemCategory.Text = GetLocalisedCategoryName(selectedItem.Category);
        _detailDescription.Text = selectedItem.Description;

        // Set icon
        if (_iconManager != null && !string.IsNullOrEmpty(selectedItem.Icon))
            _detailIcon.Image = _iconManager.GetIcon(selectedItem.Icon);

        // Calculate per-item max amount using the inventory-group-aware formula:
        // Substance -> 9999, Technology -> ChargeAmount always, Product -> multiplier x MaxStackSize
        // Technology MaxAmount = ChargeAmount for ALL tech items (confirmed via game save + editor cross ref + MXML).
        // Technology Amount = BuildFullyCharged ? ChargeAmount : 0 (fresh-insert default).
        string invTypeForDefaults = ResolveInventoryTypeForItem(selectedItem);
        int maxAmount = InventoryStackDatabase.GetMaxAmount(selectedItem, invTypeForDefaults, _inventoryGroup);
        if (invTypeForDefaults == "Substance")
        {
            _detailAmount.Value = maxAmount;
            _detailMaxAmount.Value = maxAmount;
        }
        else if (invTypeForDefaults == "Technology")
        {
            // Game behaviour (verified against MXML BuildFullyCharged field):
            //   MaxAmount = ChargeAmount (always, for ALL tech: core, UT_, UP_, HDRIVEBOOST, etc.)
            //   Amount (fresh insert) = BuildFullyCharged ? ChargeAmount : 0
            // Examples: HYPERDRIVE → 0/120 (BuildFC=false), UT_QUICKWARP → 100/100 (BuildFC=true)
            _detailMaxAmount.Value = maxAmount;
            _detailAmount.Value = selectedItem.BuildFullyCharged ? maxAmount : 0;
        }
        else
        {
            // Products: default to full stack
            _detailAmount.Value = maxAmount;
            _detailMaxAmount.Value = maxAmount;
        }
        _detailAmount.Enabled = true;
        _detailMaxAmount.Enabled = true;

        _applyButton.Enabled = _selectedCell != null;
    }

    private void EnableControlsAfterInventoryLoad()
    {
        // Enable controls after inventory is loaded
        bool hideResize = _isStorageInventory && !_isChestInventory;
        _resizeWidth.Enabled = !hideResize;
        _resizeHeight.Enabled = !hideResize;
        _resizeButton.Enabled = !hideResize;
        _detailAmount.Enabled = true;
        _detailMaxAmount.Enabled = true;
        _detailDamageFactor.Enabled = true;
        _applyButton.Enabled = false;
        _searchButton.Enabled = true;
        _typeFilter.Enabled = true;
        _categoryFilter.Enabled = true;
        _itemPicker.Enabled = true;
        UpdateToolbarActionEnabledState();
    }

    public void LoadInventory(JsonObject? inventory)
    {
        // Freeze painting on the grid container to prevent visible intermediate
        // redraws as cells are disposed and re-created.  Hiding the container
        // is more thorough than SuspendLayout alone - it suppresses all paint
        // messages, eliminating the visible "rebuild" glitch.
        RedrawHelper.Suspend(_gridContainer);
        SuspendLayout();
        _gridContainer.SuspendLayout();

        // Reset scroll position so the grid isn't offset after a rebuild
        // (e.g. when switching languages while scrolled down).
        _gridContainer.AutoScrollPosition = Point.Empty;

        // Dispose old cells (ToolTips, bitmaps) before removing them
        foreach (var cell in _cells)
        {
            _gridContainer.Controls.Remove(cell);
            cell.Dispose();
        }
        _gridContainer.Controls.Clear();
        _cells.Clear();
        _selectedCell = null;
        ClearDetailPanel();
        _slots = null;
        _currentInventory = inventory;

        EnableControlsAfterInventoryLoad();

        if (inventory == null)
        {
            UpdateToolbarActionEnabledState();
            _gridContainer.ResumeLayout(false);
            ResumeLayout(true);
            RedrawHelper.Resume(_gridContainer);
            return;
        }
        _slots = inventory.GetArray("Slots");
        if (_slots == null)
        {
            UpdateToolbarActionEnabledState();
            _gridContainer.ResumeLayout(false);
            ResumeLayout(true);
            RedrawHelper.Resume(_gridContainer);
            return;
        }

        // Determine grid dimensions from inventory Width/Height fields
        int width = GridColumns;
        int height = 1;
        try
        {
            int invWidth = 0, invHeight = 0;
            try { invWidth = inventory.GetInt("Width"); } catch { }
            try { invHeight = inventory.GetInt("Height"); } catch { }
            if (invWidth > 0 && invHeight > 0)
            {
                width = invWidth;
                height = invHeight;
            }
            else
            {
                // Fallback: infer from ValidSlotIndices + Slots
                var validSlots = inventory.GetArray("ValidSlotIndices");
                int maxX = 0, maxY = 0;
                if (validSlots != null)
                {
                    for (int i = 0; i < validSlots.Length; i++)
                    {
                        var idx = validSlots.GetObject(i);
                        if (idx != null)
                        {
                            try { maxX = Math.Max(maxX, idx.GetInt("X")); } catch { }
                            try { maxY = Math.Max(maxY, idx.GetInt("Y")); } catch { }
                        }
                    }
                }
                // Also check Slots for items beyond ValidSlotIndices
                for (int i = 0; i < _slots.Length; i++)
                {
                    try
                    {
                        var slot = _slots.GetObject(i);
                        var si = slot?.GetObject("Index");
                        if (si != null)
                        {
                            maxX = Math.Max(maxX, si.GetInt("X"));
                            maxY = Math.Max(maxY, si.GetInt("Y"));
                        }
                    }
                    catch { }
                }
                if (maxX > 0 || maxY > 0)
                {
                    width = maxX + 1;
                    height = maxY + 1;
                }
                else
                {
                    height = (_slots.Length + width - 1) / width;
                }
            }
            // Set NUDs to current inventory dimensions
            _resizeWidth.Value = Math.Clamp(width, _resizeWidth.Minimum, _resizeWidth.Maximum);
            _resizeHeight.Value = Math.Clamp(height, _resizeHeight.Minimum, _resizeHeight.Maximum);
            bool hideResize = _isStorageInventory && !_isChestInventory;
            _resizeWidthLabel.Visible = !hideResize;
            _resizeHeightLabel.Visible = !hideResize;
            _resizeWidth.Visible = !hideResize;
            _resizeHeight.Visible = !hideResize;
            _resizeButton.Visible = !hideResize;
            _resizeWidthLabel.Enabled = !hideResize;
            _resizeHeightLabel.Enabled = !hideResize;
            _resizeWidth.Enabled = !hideResize;
            _resizeHeight.Enabled = !hideResize;
            _resizeButton.Enabled = !hideResize;
            // Chest inventories support up to 10x12 (120 slots)
            if (_isChestInventory)
            {
                _resizeWidth.Maximum = 10;
                _resizeHeight.Maximum = 12;
            }
        }
        catch { }

        // Build slot lookup by X,Y position
        var slotMap = new Dictionary<(int x, int y), (int index, JsonObject slot)>();
        for (int i = 0; i < _slots.Length; i++)
        {
            try
            {
                var slot = _slots.GetObject(i);
                int x = 0, y = 0;
                try
                {
                    var idx = slot.GetObject("Index");
                    if (idx != null)
                    {
                        x = idx.GetInt("X");
                        y = idx.GetInt("Y");
                    }
                }
                catch { x = i % width; y = i / width; }
                slotMap[(x, y)] = (i, slot);
            }
            catch { }
        }

        // Build valid slot set
        var validSet = new HashSet<(int, int)>();
        try
        {
            var validSlots = inventory.GetArray("ValidSlotIndices");
            if (validSlots != null)
            {
                for (int i = 0; i < validSlots.Length; i++)
                {
                    var idx = validSlots.GetObject(i);
                    if (idx != null)
                    {
                        try { validSet.Add((idx.GetInt("X"), idx.GetInt("Y"))); } catch { }
                    }
                }
            }
        }
        catch { }

        // Create grid cells (layout already suspended from method start)
        // Collect all cells and batch-add via AddRange to avoid per-cell overhead
        var cellsToAdd = new Control[width * height];
        int cellIdx = 0;
        for (int r = 0; r < height; r++)
        {
            for (int col = 0; col < width; col++)
            {
                var cell = new SlotCell(col, r, CellWidth, CellHeight, _sharedToolTip);
                cell.IsInTechInventory = _isTechInventory;
                cell.Location = new Point(
                    col * (CellWidth + CellPadding) + CellPadding,
                    r * (CellHeight + CellPadding) + CellPadding
                );
                cell.ShowPinToggle = _pinSlotFeatureEnabled;

                if (slotMap.TryGetValue((col, r), out var slotData))
                {
                    cell.SlotIndex = slotData.index;
                    cell.SlotData = slotData.slot;
                    LoadCellData(cell);
                }
                else if (validSet.Contains((col, r)) || validSet.Count == 0)
                {
                    // Valid slot but empty - can add items here
                    cell.IsValidEmpty = true;
                    cell.IsActivated = true; // In ValidSlotIndices = enabled
                    cell.IsSupercharged = IsSlotSupercharged(col, r);
                    cell.IsPinnedForAutoStack = IsPinnedSlot(col, r);
                    cell.UpdateDisplay();
                }
                else
                {
                    // Not in ValidSlotIndices and no data = disabled slot
                    cell.IsEmpty = true;
                    cell.IsActivated = false;
                    cell.IsPinnedForAutoStack = false;
                    cell.UpdateDisplay();
                }

                cell.Click += OnCellClicked;
                cell.PinToggleClicked += OnCellPinToggleClicked;
                AttachRightClickHandler(cell);
                AttachDragHandlers(cell);
                cellsToAdd[cellIdx++] = cell;
                _cells.Add(cell);
            }
        }
        _gridContainer.Controls.AddRange(cellsToAdd);

        _gridContainer.ResumeLayout(false);

        // Compute adjacency highlighting for tech items
        ComputeAdjacency(width, height);

        // Populate type/category/item filter ComboBoxes once per database.
        // _allItems is already built by SetDatabase, so we only need to
        // populate the filter controls on the first LoadInventory call.
        if (_database != null && !_filtersPopulated)
        {
            _filtersPopulated = true;
            PopulateTypeFilter();
        }

        // Resume the outer layout that was suspended at the start of this method,
        // then re-enable painting for one flicker-free repaint of the whole grid.
        ResumeLayout(true);
        RedrawHelper.Resume(_gridContainer);
        UpdateToolbarActionEnabledState();

        if (_sortingEnabled && _currentSortMode != InventorySortMode.None && !_isApplyingSort)
            ApplyCurrentSortMode(false);
    }

    /// <summary>
    /// Convert a BinaryData item ID to its display string form,
    /// Binary format: ^(hex-encoded-bytes)#(variant-digits)
    /// </summary>
    private static string BinaryDataToItemId(BinaryData data)
    {
        var bytes = data.ToByteArray();
        var sb = new System.Text.StringBuilder();
        bool afterHash = false;
        for (int i = 0; i < bytes.Length; i++)
        {
            int b = bytes[i] & 0xFF;
            if (i == 0)
            {
                if (b != 0x5E) // '^'
                    return data.ToString();
                sb.Append('^');
            }
            else if (b == 0x23) // '#'
            {
                sb.Append('#');
                afterHash = true;
            }
            else if (afterHash)
            {
                sb.Append((char)b);
            }
            else
            {
                const string hexChars = "0123456789ABCDEF";
                sb.Append(hexChars[(b >> 4) & 0xF]);
                sb.Append(hexChars[b & 0xF]);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Extract an item ID string from a raw value that may be a string or BinaryData.
    /// </summary>
    private static string ExtractItemId(object? rawId)
    {
        if (rawId is JsonObject idObject)
            rawId = idObject.Get("Id");

        if (rawId is BinaryData binData)
            return BinaryDataToItemId(binData);
        return rawId as string ?? "";
    }

    /// <summary>
    /// Ensure an item ID has the required '^' prefix for the NMS save format.
    /// All item IDs in the save file are prefixed with '^' (e.g. "^EYEBALL", "^FUEL1").
    /// Empty/cleared slots use just "^".
    /// </summary>
    private static string EnsureCaretPrefix(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return "^";
        if (itemId.StartsWith("^"))
            return itemId;
        return "^" + itemId;
    }

    /// <summary>
    /// Returns true for figurine/bobblehead items that need T_ prefix when installed.
    /// In the save file, installed figurines use "^T_BOBBLE_*" IDs (Technology)
    /// while the product form is "^BOBBLE_*".
    /// </summary>
    private static bool IsFigurineItem(string itemId)
    {
        string stripped = itemId.TrimStart('^');
        // Already has T_ prefix (e.g. from save data) - not a bare figurine ID
        if (stripped.StartsWith("T_", StringComparison.OrdinalIgnoreCase))
            return false;
        return stripped.StartsWith("BOBBLE_", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Maps the database ItemType (derived from JSON file names) to the NMS save InventoryType value.
    /// Database ItemTypes are file names like "Technology", "Raw Materials", "Upgrades", "Curiosities".
    /// Save InventoryType must be one of: "Substance", "Product", or "Technology".
    /// </summary>
    private static string ResolveInventoryType(string? itemType)
        => InventoryStackDatabase.ResolveInventoryType(itemType);

    /// <summary>
    /// Resolves the save-file InventoryType for a specific game item,
    /// using <c>IsProcedural</c> to correctly classify procedural tech modules.
    /// </summary>
    private static string ResolveInventoryTypeForItem(GameItem item)
        => InventoryStackDatabase.ResolveInventoryTypeForItem(item);

    /// <summary>
    /// Resolves the save-file InventoryType accounting for the target inventory.
    /// </summary>
    private string ResolveSaveInventoryType(string? itemType)
        => InventoryStackDatabase.ResolveSaveInventoryType(itemType, _isTechInventory);

    /// <summary>
    /// Compute adjacency highlighting for all cells in the grid.
    /// Items with the same BaseStatType that are cardinally adjacent
    /// get a colored border using the canonical colour for that BaseStatType group.
    /// </summary>
    private void ComputeAdjacency(int gridWidth, int gridHeight)
    {
        // Build a quick lookup: (x, y) -> cell
        var grid = new Dictionary<(int, int), SlotCell>();
        foreach (var cell in _cells)
        {
            if (!string.IsNullOrEmpty(cell.ItemId))
                grid[(cell.GridX, cell.GridY)] = cell;
        }

        // For each cell, check cardinal neighbors
        foreach (var cell in _cells)
        {
            cell.ShowAdjacencyBorder = false;
            cell.AdjacencyBorderColor = Color.Transparent;

            if (string.IsNullOrEmpty(cell.ItemId)) continue;

            var info = TechAdjacencyDatabase.GetAdjacencyInfo(cell.ItemId);
            if (info == null || info.BaseStatType == 0) continue;

            // Check 4 cardinal neighbors
            var neighbors = new[] { (cell.GridX - 1, cell.GridY), (cell.GridX + 1, cell.GridY),
                                    (cell.GridX, cell.GridY - 1), (cell.GridX, cell.GridY + 1) };

            foreach (var (nx, ny) in neighbors)
            {
                if (!grid.TryGetValue((nx, ny), out var neighbor)) continue;
                var nInfo = TechAdjacencyDatabase.GetAdjacencyInfo(neighbor.ItemId);
                if (nInfo != null && nInfo.BaseStatType == info.BaseStatType)
                {
                    cell.ShowAdjacencyBorder = true;
                    try
                    {
                        cell.AdjacencyBorderColor = ColorTranslator.FromHtml(
                            TechAdjacencyDatabase.GetBaseStatTypeColour(info.BaseStatType));
                    }
                    catch
                    {
                        cell.AdjacencyBorderColor = Color.Yellow;
                    }
                    break;
                }
            }

            if (cell.ShowAdjacencyBorder)
                cell.UpdateBorderOverlay();
        }
    }

    private void LoadCellData(SlotCell cell)
    {
        if (cell.SlotData == null) return;

        string itemId = "";
        try
        {
            var idObj = cell.SlotData.GetObject("Id");
            if (idObj != null)
                itemId = ExtractItemId(idObj.Get("Id"));
            else
                itemId = ExtractItemId(cell.SlotData.Get("Id"));
        }
        catch { }

        cell.ItemId = itemId;

        int amount = 0, maxAmount = 0;
        double damageFactor = 0;
        try { amount = cell.SlotData.GetInt("Amount"); } catch { }
        try { maxAmount = cell.SlotData.GetInt("MaxAmount"); } catch { }
        try { damageFactor = cell.SlotData.GetDouble("DamageFactor"); } catch { }
        cell.Amount = amount;
        cell.MaxAmount = maxAmount;
        cell.DamageFactor = damageFactor;

        // Check activation status (whether position is in ValidSlotIndices)
        cell.IsActivated = IsSlotInValidIndices(cell.GridX, cell.GridY);
        cell.ShowPinToggle = _pinSlotFeatureEnabled && cell.IsActivated;
        cell.IsPinnedForAutoStack = cell.IsActivated && IsPinnedSlot(cell.GridX, cell.GridY);

        // Check supercharged status (SpecialSlots entry with matching X,Y and TechBonus type)
        cell.IsSupercharged = IsSlotSupercharged(cell.GridX, cell.GridY);

        // Check if slot has an actual item or is empty-with-position
        if (string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM")
        {
            cell.IsValidEmpty = true;
            cell.SlotData = cell.SlotData; // Keep slot data for position
            cell.UpdateDisplay();
            return;
        }

        // Look up display name and type from database, handling variant IDs like ^UP_JETX#76842
        if (_database != null && !string.IsNullOrEmpty(itemId))
        {
            var (gameItem, displayName, techPackIcon, techPackClass) = ResolveItemAndDisplayName(itemId);
            if (gameItem != null)
            {
                cell.DisplayName = displayName;
                cell.ItemType = gameItem.ItemType;
                cell.Category = gameItem.Category;
                cell.IsChargeable = gameItem.IsChargeable;

                // Set item class from TechPack if available, else from Quality field, else from Rarity
                if (!string.IsNullOrEmpty(techPackClass))
                    cell.ItemClass = techPackClass;
                else if (!string.IsNullOrEmpty(gameItem.Quality))
                {
                    var qClass = gameItem.QualityToClass();
                    if (!string.IsNullOrEmpty(qClass))
                        cell.ItemClass = qClass;
                }

                // Fallback: derive class from Rarity for upgrades (U_, SHIP_) where Quality is absent
                if (string.IsNullOrEmpty(cell.ItemClass) && !string.IsNullOrEmpty(gameItem.Rarity))
                {
                    var rClass = gameItem.RarityToClass();
                    if (!string.IsNullOrEmpty(rClass))
                        cell.ItemClass = rClass;
                }

                // Use TechPack icon when the item was resolved via TechPacks, otherwise use the game item icon
                string iconName = techPackIcon ?? gameItem.Icon;

                // For CV_ corvette tech items, try to guess the actual base part from the corvette's Objects.
                // Use cached corvette override when available (preserved across drag/drop moves).
                if (_corvettePartCollection != null && IsCorvetteTechId(itemId))
                {
                    // Both CorvetteDisplayName and CorvetteIconName are always set/cleared together.
                    if (cell.CorvetteDisplayName != null && cell.CorvetteIconName != null)
                    {
                        // Reuse cached corvette resolution from a previous LoadCellData
                        iconName = cell.CorvetteIconName;
                        cell.DisplayName = cell.CorvetteDisplayName;
                    }
                    else
                    {
                        var basePart = GuessCorvetteBasePart(itemId);
                        if (basePart != null)
                        {
                            // Override icon and name with the actual base part's info
                            iconName = basePart.Icon;
                            string variant = "";
                            var vm = Regex.Match(itemId, @"#\d{5,}$");
                            if (vm.Success) variant = vm.Value;
                            cell.DisplayName = string.IsNullOrEmpty(variant) ? basePart.Name : $"{basePart.Name} [{variant}]";

                            // Cache for drag/drop preservation
                            cell.CorvetteDisplayName = cell.DisplayName;
                            cell.CorvetteIconName = iconName;
                        }
                    }
                }

                if (_iconManager != null && !string.IsNullOrEmpty(iconName))
                    cell.IconImage = _iconManager.GetIcon(iconName);

                // Load class mini icon overlay only for Technology, Technology Module, and Upgrades types
                if (_iconManager != null && !string.IsNullOrEmpty(cell.ItemClass) && cell.ItemClass != "NONE"
                    && ShouldShowClassMiniIcon(gameItem.ItemType, itemId))
                {
                    if(cell.ItemClass == "?") cell.ItemClass = "Sentinel";
                    cell.ClassMiniIcon = _iconManager.GetIcon($"CLASSMINI.{cell.ItemClass}.png");
                }
                    
            }
            else
            {
                // Fallback: show raw ID (with variant appended)
                cell.DisplayName = displayNameFromId(itemId);
            }
        }

        cell.UpdateDisplay();
    }

    /// <summary>
    /// Returns true when the item type and ID indicate that a class mini icon
    /// should be displayed. Only Technology, Technology Module, and Upgrades
    /// items are eligible, and Upgrades whose IDs end with "INV_TOKEN" are excluded.
    /// </summary>
    private static bool ShouldShowClassMiniIcon(string itemType, string itemId)
    {
        if (string.IsNullOrEmpty(itemType))
            return false;
        if (itemType.Equals("Technology", StringComparison.OrdinalIgnoreCase)
            || itemType.Equals("Technology Module", StringComparison.OrdinalIgnoreCase))
            return true;
        if (itemType.Equals("Upgrades", StringComparison.OrdinalIgnoreCase))
        {
            // Exclude inventory token items (e.g. WEAP_INV_TOKEN, SUIT_INV_TOKEN)
            string baseId = itemId.StartsWith("^") ? itemId[1..] : itemId;
            if (baseId.EndsWith("INV_TOKEN", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
        return false;
    }

    // Helper: build a simple display when no DB entry exists
    private static string displayNameFromId(string id)
    {
        var m = Regex.Match(id, @"#\d{5,}$");
        if (m.Success)
        {
            var baseId = id.Substring(0, m.Index);
            var variant = m.Value;
            return baseId + " [" + variant + "]";
        }
        return id;
    }

    /// <summary>
    /// Checks whether the given ID (without any #variant suffix) is a TechPack hash.
    /// TechPack hashes are a '^' prefix followed by exactly 12 uppercase hex characters.
    /// </summary>
    private static bool IsTechPackHash(string baseId) =>
        baseId.Length == 13 && baseId[0] == '^' && Regex.IsMatch(baseId, @"^\^[0-9A-Fa-f]{12}$");

    /// <summary>
    /// Resolves a GameItem from an item ID, checking the TechPacks dictionary as a fallback
    /// for hash-based IDs (^+12 hex chars, with optional #variant suffix).
    /// Returns the resolved GameItem and, when the item was resolved via TechPacks, the TechPack icon filename.
    /// </summary>
    private (GameItem? gameItem, string? techPackIcon, string? techPackClass) ResolveGameItem(string itemId)
    {
        if (_database == null) return (null, null, null);

        // Detect variant suffix (#12345)
        var m = Regex.Match(itemId, @"#\d{5,}$");
        string baseId = itemId;
        if (m.Success)
            baseId = itemId.Substring(0, m.Index);

        // Try direct database lookup first
        GameItem? gi = _database.GetItem(itemId) ?? _database.GetItem("^" + itemId);
        if (gi == null && m.Success)
            gi = _database.GetItem(baseId) ?? _database.GetItem("^" + baseId);

        if (gi != null)
        {
            // Item found directly - check TechPack for class info by item ID
            var cls = TechPacks.GetClassById(baseId);
            return (gi, null, cls);
        }

        // Fallback: check TechPacks for hash-based IDs
        if (IsTechPackHash(baseId) && TechPacks.Dictionary.TryGetValue(baseId, out var techPack))
        {
            gi = _database.GetItem(techPack.Id);
            if (gi != null)
                return (gi, techPack.Icon, techPack.Class);
        }

        return (null, null, null);
    }

    /// <summary>
    /// Resolve a GameItem and a display name for an item id that may include a variant suffix.
    /// Variant format recognized: '#'+5+digits at the end (e.g. ^UP_JETX#76842).
    /// Lookup tries the exact id first, then falls back to the base id (without the #digits),
    /// and finally checks TechPacks for hash-based IDs.
    /// The returned display name uses the base GameItem.Name with " [#digits]" appended when a variant is present.
    /// Also returns a TechPack icon filename override when the item was resolved via TechPacks.
    /// </summary>
    private (GameItem? gameItem, string displayName, string? techPackIcon, string? techPackClass) ResolveItemAndDisplayName(string itemId)
    {
        if (_database == null) return (null, displayNameFromId(itemId), null, null);

        // Detect variant suffix (#12345)
        var m = Regex.Match(itemId, @"#\d{5,}$");
        string baseId = itemId;
        string variant = "";
        if (m.Success)
        {
            variant = m.Value; // includes the leading '#'
            baseId = itemId.Substring(0, m.Index);
        }

        var (gi, techPackIcon, techPackClass) = ResolveGameItem(itemId);

        // For tech inventories, prefer NameLower over Name
        string displayBase;
        if (_isTechInventory && gi != null && !string.IsNullOrEmpty(gi.NameLower))
            displayBase = gi.NameLower;
        else
            displayBase = gi?.Name ?? baseId;
        string displayName = string.IsNullOrEmpty(variant) ? displayBase : $"{displayBase} [{variant}]";
        return (gi, displayName, techPackIcon, techPackClass);
    }

    private bool IsSlotInValidIndices(int x, int y)
    {
        if (_currentInventory == null) return false;
        try
        {
            var validSlots = _currentInventory.GetArray("ValidSlotIndices");
            if (validSlots == null) return false;
            for (int i = 0; i < validSlots.Length; i++)
            {
                var idx = validSlots.GetObject(i);
                if (idx != null && idx.GetInt("X") == x && idx.GetInt("Y") == y)
                    return true;
            }
        }
        catch { }
        return false;
    }

    private bool IsSlotSupercharged(int x, int y)
    {
        if (_currentInventory == null) return false;
        try
        {
            var specialSlots = _currentInventory.GetArray("SpecialSlots");
            if (specialSlots == null) return false;
            for (int i = 0; i < specialSlots.Length; i++)
            {
                var entry = specialSlots.GetObject(i);
                if (entry == null) continue;
                var typeObj = entry.GetObject("Type");
                if (typeObj == null) continue;
                string slotType = typeObj.GetString("InventorySpecialSlotType") ?? "";
                if (slotType != "TechBonus") continue;
                var idxObj = entry.GetObject("Index");
                if (idxObj == null) continue;
                if (idxObj.GetInt("X") == x && idxObj.GetInt("Y") == y)
                    return true;
            }
        }
        catch { }
        return false;
    }

    private int CountSuperchargedSlots()
    {
        if (_currentInventory == null) return 0;
        int count = 0;
        try
        {
            var specialSlots = _currentInventory.GetArray("SpecialSlots");
            if (specialSlots == null) return 0;
            for (int i = 0; i < specialSlots.Length; i++)
            {
                var entry = specialSlots.GetObject(i);
                if (entry == null) continue;
                var typeObj = entry.GetObject("Type");
                if (typeObj == null) continue;
                string slotType = typeObj.GetString("InventorySpecialSlotType") ?? "";
                if (slotType == "TechBonus") count++;
            }
        }
        catch { }
        return count;
    }

    private void OnCellClicked(object? sender, EventArgs e)
    {
        if (sender is not SlotCell cell) return;
        SelectCell(cell);
    }

    /// <summary>
    /// Attach explicit MouseUp handlers to a cell and all its children so that
    /// right-click reliably shows the context menu regardless of which child
    /// control the cursor is over.
    /// </summary>
    private void AttachRightClickHandler(SlotCell cell)
    {
        void Handler(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            _contextCell = cell;
            ConfigureContextMenuItems(cell);
            var screenPoint = (s as Control)?.PointToScreen(e.Location) ?? Cursor.Position;
            _cellContextMenu.Show(screenPoint);
        }

        // Attach to the cell and recursively to all descendants so inner elements (like marquee inner label) are covered
        void Attach(Control ctrl)
        {
            ctrl.MouseUp += Handler;
            foreach (Control child in ctrl.Controls)
                Attach(child);
        }

        Attach(cell);
    }

    /// <summary>
    /// Attach mouse handlers to a cell and all its children to support
    /// drag-and-drop of items between inventory slots.
    /// Left-click + drag moves/swaps items; Ctrl + drag duplicates to empty slots.
    /// </summary>
    private void AttachDragHandlers(SlotCell cell)
    {
        void OnMouseDown(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            // Only start a potential drag from a cell that has an item
            if (cell.SlotData == null || string.IsNullOrEmpty(cell.ItemId) || cell.IsValidEmpty) return;
            _dragSourceCell = cell;
            _dragStartPoint = (s as Control)?.PointToScreen(e.Location) ?? Cursor.Position;
            _isDragging = false;
        }

        void OnMouseMove(object? s, MouseEventArgs e)
        {
            if (_dragSourceCell == null || _dragSourceCell != cell) return;
            if (e.Button != MouseButtons.Left)
            {
                // Mouse button released elsewhere, cancel potential drag
                CancelDrag();
                return;
            }

            var currentScreen = (s as Control)?.PointToScreen(e.Location) ?? Cursor.Position;
            if (!_isDragging)
            {
                // Check if mouse has moved past drag threshold
                int dx = Math.Abs(currentScreen.X - _dragStartPoint.X);
                int dy = Math.Abs(currentScreen.Y - _dragStartPoint.Y);
                if (dx > DragThreshold || dy > DragThreshold)
                {
                    _isDragging = true;
                    Cursor = Cursors.Hand;
                }
            }

            if (_isDragging)
            {
                // Highlight potential drop target
                var target = GetCellAtScreenPoint(currentScreen);
                UpdateDragHighlight(target);
            }
        }

        void OnMouseUp(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragSourceCell == null)
            {
                CancelDrag();
                return;
            }

            if (!_isDragging)
            {
                // No drag occurred, just a normal click
                CancelDrag();
                return;
            }

            var screenPoint = (s as Control)?.PointToScreen(e.Location) ?? Cursor.Position;
            var targetCell = GetCellAtScreenPoint(screenPoint);

            if (targetCell != null && targetCell != _dragSourceCell)
                ExecuteDrop(_dragSourceCell, targetCell, ModifierKeys.HasFlag(Keys.Control));
            CancelDrag();
        }

        void Attach(Control ctrl)
        {
            ctrl.MouseDown += OnMouseDown;
            ctrl.MouseMove += OnMouseMove;
            ctrl.MouseUp += OnMouseUp;
            foreach (Control child in ctrl.Controls)
                Attach(child);
        }

        Attach(cell);
    }

    /// <summary>
    /// Find a SlotCell at the given screen-coordinate point by hit-testing the grid container.
    /// </summary>
    private SlotCell? GetCellAtScreenPoint(Point screenPoint)
    {
        var clientPoint = _gridContainer.PointToClient(screenPoint);
        foreach (var cell in _cells)
        {
            if (cell.Bounds.Contains(clientPoint))
                return cell;
        }
        return null;
    }

    /// <summary>
    /// Update visual highlight on the current drop target cell.
    /// </summary>
    private void UpdateDragHighlight(SlotCell? target)
    {
        if (target == _dragHighlightCell) return;

        // Remove highlight from previous target
        if (_dragHighlightCell != null)
        {
            _dragHighlightCell.BackColor = GetDefaultCellBackColor(_dragHighlightCell);
            _dragHighlightCell.Invalidate();
        }

        _dragHighlightCell = target;

        // Apply highlight to new target (but not the source itself)
        if (_dragHighlightCell != null && _dragHighlightCell != _dragSourceCell)
        {
            // Only highlight valid drop targets (activated slots, not completely disabled)
            if (_dragHighlightCell.IsActivated || _dragHighlightCell.IsValidEmpty ||
                (_dragHighlightCell.SlotData != null && !string.IsNullOrEmpty(_dragHighlightCell.ItemId)))
            {
                _dragHighlightCell.BackColor = Color.FromArgb(80, 100, 180, 255);
                _dragHighlightCell.Invalidate();
            }
        }
    }

    /// <summary>
    /// Returns the default background colour for a cell based on its state.
    /// </summary>
    private static Color GetDefaultCellBackColor(SlotCell cell)
    {
        return Color.FromArgb(50, 50, 50);
    }

    /// <summary>
    /// Reset all drag state and visual feedback.
    /// </summary>
    private void CancelDrag()
    {
        if (_dragHighlightCell != null)
        {
            _dragHighlightCell.BackColor = GetDefaultCellBackColor(_dragHighlightCell);
            _dragHighlightCell.Invalidate();
        }
        _dragSourceCell = null;
        _isDragging = false;
        _dragHighlightCell = null;
        Cursor = Cursors.Default;
    }

    /// <summary>
    /// Execute a drag-drop operation between two cells.
    /// Normal drag: swap items if target has an item, move if target is empty.
    /// Ctrl+drag: duplicate item to target only if the target is empty.
    /// </summary>
    private void ExecuteDrop(SlotCell source, SlotCell target, bool ctrlHeld)
    {
        if (_slots == null) return;

        // Target must be a valid slot (activated or already contains data)
        bool targetValid = target.IsActivated || target.IsValidEmpty ||
            (target.SlotData != null && !string.IsNullOrEmpty(target.ItemId) && !target.IsValidEmpty);
        if (!targetValid) return;

        bool sourceHasItem = source.SlotData != null && !string.IsNullOrEmpty(source.ItemId) && !source.IsValidEmpty;
        bool targetHasItem = target.SlotData != null && !string.IsNullOrEmpty(target.ItemId) && !target.IsValidEmpty;

        if (!sourceHasItem) return; // Nothing to move

        if (ctrlHeld)
        {
            // Ctrl+drag: duplicate to empty slot only
            if (targetHasItem) return; // Ignore if target already has an item
            DuplicateSlotToCell(source, target);
        }
        else
        {
            // Normal drag: swap if target has item, move if target is empty
            if (targetHasItem)
                SwapSlotsBetweenCells(source, target);
            else
                MoveSlotToCell(source, target);
        }

        // Recompute adjacency for the entire grid
        int maxY = 0;
        foreach (var c in _cells)
            if (c.GridY > maxY) maxY = c.GridY;
        ComputeAdjacency(GridColumns, maxY + 1);
        foreach (var c in _cells)
            c.UpdateDisplay();

        // Select the target cell and raise data modified
        SelectCell(target);
        RaiseDataModified();
    }

    /// <summary>
    /// Move the item from source cell to an empty target cell.
    /// Updates the slot's Index coordinates to the target position.
    /// </summary>
    private void MoveSlotToCell(SlotCell source, SlotCell target)
    {
        if (source.SlotData == null) return;

        // Update Index coordinates in the JSON data to the new position
        InventorySlotHelper.UpdateSlotIndex(source.SlotData, target.GridX, target.GridY);

        // Transfer slot data from source to target
        target.SlotData = source.SlotData;
        target.SlotIndex = source.SlotIndex;
        target.IsValidEmpty = false;
        target.IsEmpty = false;

        // Preserve corvette override cache so it survives LoadCellData re-resolution
        target.CorvetteDisplayName = source.CorvetteDisplayName;
        target.CorvetteIconName = source.CorvetteIconName;

        // Clear source cell
        source.SlotData = null;
        source.SlotIndex = -1;
        ClearCellDisplay(source);
        source.IsValidEmpty = true;

        // Reload target cell display from its new slot data
        LoadCellData(target);
        target.UpdateDisplay();
        source.UpdateDisplay();
    }

    /// <summary>
    /// Swap items between two cells that both contain items.
    /// Updates Index coordinates in both slot JSON objects.
    /// </summary>
    private void SwapSlotsBetweenCells(SlotCell cellA, SlotCell cellB)
    {
        if (cellA.SlotData == null || cellB.SlotData == null) return;

        // Swap the Index coordinates in each JSON object
        InventorySlotHelper.SwapSlotIndices(cellA.SlotData, cellA.GridX, cellA.GridY,
            cellB.SlotData, cellB.GridX, cellB.GridY);

        // Swap slot data references
        (cellA.SlotData, cellB.SlotData) = (cellB.SlotData, cellA.SlotData);
        (cellA.SlotIndex, cellB.SlotIndex) = (cellB.SlotIndex, cellA.SlotIndex);

        // Swap corvette override caches so they follow their respective items
        (cellA.CorvetteDisplayName, cellB.CorvetteDisplayName) = (cellB.CorvetteDisplayName, cellA.CorvetteDisplayName);
        (cellA.CorvetteIconName, cellB.CorvetteIconName) = (cellB.CorvetteIconName, cellA.CorvetteIconName);

        // Reload both cells
        LoadCellData(cellA);
        LoadCellData(cellB);
        cellA.UpdateDisplay();
        cellB.UpdateDisplay();
    }

    /// <summary>
    /// Duplicate (copy) the item from source cell to an empty target cell.
    /// Creates a new JSON slot object with the target's coordinates.
    /// </summary>
    private void DuplicateSlotToCell(SlotCell source, SlotCell target)
    {
        if (source.SlotData == null || _slots == null) return;

        // Read values from source
        string itemId = source.ItemId;
        int amount = source.Amount;
        int maxAmount = source.MaxAmount;
        double damageFactor = source.DamageFactor;

        // Determine inventory type
        string invType = "Product";
        if (_database != null)
        {
            var (gameItem, _, _) = ResolveGameItem(itemId);
            if (gameItem != null)
                invType = ResolveInventoryTypeForItem(gameItem);
        }

        // Build a new slot JSON object for the target position
        var newSlot = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", invType);
        newSlot.Add("Type", typeObj);
        newSlot.Add("Id", EnsureCaretPrefix(itemId));
        newSlot.Add("Amount", amount);
        newSlot.Add("MaxAmount", maxAmount);
        newSlot.Add("DamageFactor", damageFactor);
        newSlot.Add("FullyInstalled", true);
        newSlot.Add("AddedAutomatically", false);

        var indexObj = new JsonObject();
        indexObj.Add("X", target.GridX);
        indexObj.Add("Y", target.GridY);
        newSlot.Add("Index", indexObj);

        // Add or replace slot data in the array
        if (target.SlotData != null && target.SlotIndex >= 0)
        {
            _slots.Set(target.SlotIndex, newSlot);
        }
        else
        {
            _slots.Add(newSlot);
            target.SlotIndex = _slots.Length - 1;
        }

        target.SlotData = newSlot;
        target.IsValidEmpty = false;
        target.IsEmpty = false;

        // Preserve corvette override cache for duplicated items
        target.CorvetteDisplayName = source.CorvetteDisplayName;
        target.CorvetteIconName = source.CorvetteIconName;

        LoadCellData(target);
        target.UpdateDisplay();
    }

    /// <summary>
    /// Clear display properties on a cell after its item has been moved away.
    /// </summary>
    private static void ClearCellDisplay(SlotCell cell)
    {
        cell.ItemId = "";
        cell.DisplayName = "";
        cell.ItemType = "";
        cell.Category = "";
        cell.Amount = 0;
        cell.MaxAmount = 0;
        cell.DamageFactor = 0;
        cell.IconImage = null;
        cell.ClassMiniIcon = null;
        cell.ShowAdjacencyBorder = false;
        cell.AdjacencyBorderColor = Color.Transparent;
        cell.ItemClass = "";
        cell.IsChargeable = false;
        cell.CorvetteDisplayName = null;
        cell.CorvetteIconName = null;
    }
    private void ConfigureContextMenuItems(SlotCell cell)
    {
        bool hasItem = cell.SlotData != null && !string.IsNullOrEmpty(cell.ItemId) && !cell.IsValidEmpty;
        bool canAdd = cell.IsValidEmpty || (!cell.IsEmpty);
        bool isActivated = cell.IsActivated;

        _addItemMenuItem.Visible = canAdd;
        _addItemMenuItem.Text = hasItem ? UiStrings.Get("inventory.ctx_replace_item") : UiStrings.Get("inventory.ctx_add_item");
        _removeItemMenuItem.Visible = hasItem;

        _enableSlotMenuItem.Visible = !_slotToggleDisabled;
        _enableSlotMenuItem.Text = isActivated ? UiStrings.Get("inventory.ctx_disable_slot") : UiStrings.Get("inventory.ctx_enable_slot");
        _enableAllSlotsMenuItem.Visible = !_slotToggleDisabled && _currentInventory != null;
        _pinSlotMenuItem.Visible = _pinSlotFeatureEnabled && isActivated;
        _pinSlotMenuItem.Text = cell.IsPinnedForAutoStack ? UiStrings.Get("inventory.ctx_unpin_slot") : UiStrings.Get("inventory.ctx_pin_slot");

        _repairSlotMenuItem.Visible = hasItem;
        _repairAllSlotsMenuItem.Visible = _currentInventory != null;

        _superchargeSlotMenuItem.Visible = !_superchargeDisabled;
        _superchargeSlotMenuItem.Text = cell.IsSupercharged ? UiStrings.Get("inventory.ctx_remove_supercharge") : UiStrings.Get("inventory.ctx_supercharge");
        _superchargeAllSlotsMenuItem.Visible = !_superchargeDisabled && _currentInventory != null;

        // Fill Stack: only shown when there's a measurable stack to fill
        _fillStackMenuItem.Visible = hasItem && cell.MaxAmount > 0;

        // Show "Recharge All Technology" only in tech inventories
        _rechargeAllTechMenuItem.Visible = _isTechInventory && _currentInventory != null;
        // Show "Refill All Stacks" only in non-tech (cargo) inventories
        _refillAllStacksMenuItem.Visible = !_isTechInventory && _currentInventory != null;

        _sortByNameMenuItem.Visible = false;
        _sortByCategoryMenuItem.Visible = false;
        bool canAutoStack = _isCargoInventory && !_isStorageInventory && _currentInventory != null;
        _autoStackToStorageMenuItem.Visible = canAutoStack && (AutoStackToStorageRequested != null || AutoStackSelectedSlotToStorageRequested != null);
        _autoStackToStarshipMenuItem.Visible = canAutoStack && (AutoStackToStarshipRequested != null || AutoStackSelectedSlotToStarshipRequested != null);
        _autoStackToFreighterMenuItem.Visible = canAutoStack && (AutoStackToFreighterRequested != null || AutoStackSelectedSlotToFreighterRequested != null);

        _copyItemMenuItem.Visible = cell.SlotData != null && !string.IsNullOrEmpty(cell.ItemId) && !cell.IsValidEmpty;
        _pasteItemMenuItem.Visible = _copiedItemCell != null && (cell.IsValidEmpty || !cell.IsEmpty);
    }

    private void OnContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (_contextCell == null || _currentInventory == null)
        {
            e.Cancel = true;
            return;
        }
    }

    private void OnCellPinToggleClicked(object? sender, EventArgs e)
    {
        if (sender is not SlotCell cell)
            return;
        TogglePinnedSlot(cell);
    }

    private void OnTogglePinnedSlot(object? sender, EventArgs e)
    {
        if (_contextCell == null)
            return;
        TogglePinnedSlot(_contextCell);
    }

    private void TogglePinnedSlot(SlotCell cell)
    {
        if (!_pinSlotFeatureEnabled || !cell.IsActivated)
            return;

        var pos = (cell.GridX, cell.GridY);
        if (!_pinnedSlots.Add(pos))
            _pinnedSlots.Remove(pos);

        cell.IsPinnedForAutoStack = IsPinnedSlot(cell.GridX, cell.GridY);
        cell.UpdateDisplay();
        RaisePinnedSlotsChanged();
    }

    private void SelectCell(SlotCell cell)
    {
        // Deselect previous
        if (_selectedCell != null)
            _selectedCell.IsSelected = false;

        cell.IsSelected = true;
        _selectedCell = cell;

        if (cell.IsEmpty)
        {
            ClearDetailPanel();
            return;
        }

        if (cell.IsValidEmpty && string.IsNullOrEmpty(cell.ItemId))
        {
            // Empty valid slot - show position only
            _detailItemName.Text = UiStrings.Get("inventory.empty_slot");
            _detailSlotPosition.Text = $"({cell.GridX}, {cell.GridY})";
            _detailItemType.Text = "";
            _detailItemCategory.Text = "";
            _detailItemId.Text = "";
            _detailAmount.Value = 0;
            _detailMaxAmount.Value = 0;
            _detailDamageFactor.Value = 0;
            _detailDescription.Text = UiStrings.Get("inventory.right_click_hint");
            _detailIcon.Image = null;
            _applyButton.Enabled = false;
            return;
        }

        // Populate detail panel with slot data
        _detailItemId.Text = cell.ItemId;

        // Update labels based on whether this is a chargeable tech item
        if (cell.IsTechChargeable)
        {
            _detailAmountLabel.Text = UiStrings.Get("inventory.charge");
            _detailMaxLabel.Text = UiStrings.Get("inventory.max_charge");
        }
        else
        {
            _detailAmountLabel.Text = UiStrings.Get("inventory.amount");
            _detailMaxLabel.Text = UiStrings.Get("inventory.max");
        }

        // Always show actual values and keep controls enabled so the user can freely edit them.
        _detailAmount.Value = Math.Clamp(cell.Amount, (int)_detailAmount.Minimum, (int)_detailAmount.Maximum);
        _detailMaxAmount.Value = Math.Clamp(cell.MaxAmount, (int)_detailMaxAmount.Minimum, (int)_detailMaxAmount.Maximum);
        _detailAmount.Enabled = true;
        _detailMaxAmount.Enabled = true;

        _detailDamageFactor.Value = (decimal)Math.Clamp(cell.DamageFactor, 0, 1);
        _detailSlotPosition.Text = $"({cell.GridX}, {cell.GridY})";
        _applyButton.Enabled = true;

        if (!string.IsNullOrEmpty(cell.DisplayName))
            _detailItemName.Text = cell.DisplayName;
        else if (!string.IsNullOrEmpty(cell.ItemId))
            _detailItemName.Text = cell.ItemId;
        else
            _detailItemName.Text = UiStrings.Get("inventory.empty_slot");

        _detailItemType.Text = cell.ItemType;
        _detailItemCategory.Text = GetLocalisedCategoryName(cell.Category);

        // Show icon in detail panel
        _detailIcon.Image = cell.IconImage;

        // Show description from database (use base item when variant present)
        if (_database != null && !string.IsNullOrEmpty(cell.ItemId))
        {
            var (item, _, _, _) = ResolveItemAndDisplayName(cell.ItemId);
            _detailDescription.Text = item?.Description ?? "";
        }
        else
        {
            _detailDescription.Text = "";
        }
    }

    private void OnApplyChanges(object? sender, EventArgs e)
    {
        if (_selectedCell == null || _slots == null) return;

        string newItemId = _detailItemId.Text.Trim();
        if (string.IsNullOrEmpty(newItemId))
        {
            MessageBox.Show(UiStrings.Get("inventory.apply_enter_id"), UiStrings.Get("inventory.apply_changes_title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Read values from detail controls - respect user-specified values.
        int amount = (int)_detailAmount.Value;
        int maxAmount = (int)_detailMaxAmount.Value;

        // Determine inventory type early so we can handle tech items correctly.
        string invType = "Product";
        GameItem? gameItem = null;
        if (_database != null)
        {
            (gameItem, _, _) = ResolveGameItem(newItemId);
            if (gameItem != null)
                invType = ResolveInventoryTypeForItem(gameItem);
        }

        // Figurines (BOBBLE_*) installed in tech slots need the T_ prefix.
        // The game stores them as ^T_BOBBLE_APOLLO (technology form) vs
        // ^BOBBLE_APOLLO (product/cargo form).
        string saveItemId = newItemId;
        if (gameItem != null && IsFigurineItem(gameItem.Id) && _isTechInventory)
        {
            saveItemId = "T_" + gameItem.Id;
            invType = "Technology";
        }

        // Technology: MaxAmount = ChargeAmount (always). Amount defaults using BuildFullyCharged.
        // Verified against MXML, game save, and other editor behaviour.
        if (invType == "Technology")
        {
            int techMaxAmount = gameItem != null
                ? InventoryStackDatabase.GetMaxAmount(gameItem, "Technology", _inventoryGroup)
                : 100;
            if (maxAmount == 0) maxAmount = techMaxAmount;
            if (amount == 0)
                amount = (gameItem != null && gameItem.BuildFullyCharged)
                    ? techMaxAmount
                    : 0;
        }
        else
        {
            // For non-technology items, zero amounts are treated as unset - default to 1.
            // Negative values are intentionally preserved as they are valid game data.
            if (amount == 0 && maxAmount == 0) { amount = 1; maxAmount = 1; }
            if (maxAmount == 0) maxAmount = amount;
        }

        // If the cell has no existing slot data (valid empty slot), create a new slot
        if (_selectedCell.SlotData == null)
        {
            var newSlot = new JsonObject();
            var typeObj = new JsonObject();
            typeObj.Add("InventoryType", invType);
            newSlot.Add("Type", typeObj);

            // Procedural items store Id as "itemId#seed"
            string newSlotId = EnsureCaretPrefix(saveItemId);
            if (gameItem != null && gameItem.IsProcedural)
            {
                int seed = Random.Shared.Next(1, int.MaxValue);
                newSlotId = $"{newSlotId}#{seed}";
            }
            newSlot.Add("Id", newSlotId);
            newSlot.Add("Amount", amount);
            newSlot.Add("MaxAmount", maxAmount);
            newSlot.Add("DamageFactor", (double)_detailDamageFactor.Value);
            newSlot.Add("FullyInstalled", true);
            newSlot.Add("AddedAutomatically", false);
            var indexObj = new JsonObject();
            indexObj.Add("X", _selectedCell.GridX);
            indexObj.Add("Y", _selectedCell.GridY);
            newSlot.Add("Index", indexObj);

            _slots.Add(newSlot);
            _selectedCell.SlotIndex = _slots.Length - 1;
            _selectedCell.SlotData = newSlot;
        }
        else
        {
            var slot = _selectedCell.SlotData;

            // Update Item ID - "Id" must be a plain string (e.g. "^EYEBALL"), not a nested object.
            // Procedural items store Id as "itemId#seed".
            try
            {
                string newId = EnsureCaretPrefix(saveItemId);
                if (gameItem != null && gameItem.IsProcedural)
                {
                    string existingId = slot.GetString("Id") ?? "";
                    int hashIdx = existingId.IndexOf('#');
                    if (hashIdx >= 0)
                    {
                        // Preserve existing procedural seed
                        newId = $"{newId}{existingId.Substring(hashIdx)}";
                    }
                    else
                    {
                        int seed = Random.Shared.Next(1, int.MaxValue);
                        newId = $"{newId}#{seed}";
                    }
                }
                slot.Set("Id", newId);
            }
            catch { }

            // Update Amount / MaxAmount
            slot.Set("Amount", amount);
            slot.Set("MaxAmount", maxAmount);

            // Update DamageFactor
            try { slot.Set("DamageFactor", (double)_detailDamageFactor.Value); } catch { }

            // Update inventory type based on item
            try
            {
                var typeObj = slot.GetObject("Type");
                if (typeObj != null)
                    typeObj.Set("InventoryType", invType);
            }
            catch { }
        }

        // Refresh cell display
        _selectedCell.ItemId = newItemId;
        _selectedCell.Amount = amount;
        _selectedCell.MaxAmount = maxAmount;
        _selectedCell.DamageFactor = (double)_detailDamageFactor.Value;
        _selectedCell.IsValidEmpty = false;
        _selectedCell.IsEmpty = false;

        if (_database != null && !string.IsNullOrEmpty(newItemId))
        {
            var (resolvedItem, displayName, techPackIcon, techPackClass) = ResolveItemAndDisplayName(newItemId);
            if (resolvedItem != null)
            {
                _selectedCell.DisplayName = displayName;
                _selectedCell.ItemType = resolvedItem.ItemType;
                _selectedCell.Category = resolvedItem.Category;
                _detailItemName.Text = displayName;
                _detailItemType.Text = resolvedItem.ItemType;
                _detailItemCategory.Text = GetLocalisedCategoryName(resolvedItem.Category);
                _detailDescription.Text = resolvedItem.Description;

                // Set item class from TechPack if available, else from Quality field
                if (!string.IsNullOrEmpty(techPackClass))
                    _selectedCell.ItemClass = techPackClass;
                else if (!string.IsNullOrEmpty(resolvedItem.Quality))
                {
                    var qClass = resolvedItem.QualityToClass();
                    if (!string.IsNullOrEmpty(qClass))
                        _selectedCell.ItemClass = qClass;
                }

                // Fallback: derive class from Rarity for upgrades where Quality is absent
                if (string.IsNullOrEmpty(_selectedCell.ItemClass) && !string.IsNullOrEmpty(resolvedItem.Rarity))
                {
                    var rClass = resolvedItem.RarityToClass();
                    if (!string.IsNullOrEmpty(rClass))
                        _selectedCell.ItemClass = rClass;
                }

                string iconName = techPackIcon ?? resolvedItem.Icon;
                if (_iconManager != null && !string.IsNullOrEmpty(iconName))
                {
                    _selectedCell.IconImage = _iconManager.GetIcon(iconName);
                    _detailIcon.Image = _selectedCell.IconImage;
                }

                // Load class mini icon overlay only for Technology, Technology Module, and Upgrades types
                if (_iconManager != null && !string.IsNullOrEmpty(_selectedCell.ItemClass) && _selectedCell.ItemClass != "NONE"
                    && ShouldShowClassMiniIcon(resolvedItem.ItemType, newItemId))
                    _selectedCell.ClassMiniIcon = _iconManager.GetIcon($"CLASSMINI.{_selectedCell.ItemClass}.png");
                else
                    _selectedCell.ClassMiniIcon = null;
            }
            else
            {
                _selectedCell.DisplayName = displayNameFromId(newItemId);
                _detailItemName.Text = _selectedCell.DisplayName;
                _detailDescription.Text = "";
            }
        }

        _selectedCell.UpdateDisplay();
        RaiseDataModified();
    }

    private void OnAddItem(object? sender, EventArgs e)
    {
        if (_contextCell == null || _currentInventory == null) return;

        // Get the selected item from the picker or the ID field
        string itemId = _detailItemId.Text.Trim();

        if (_itemPicker.SelectedItem is GameItem pickedItem)
            itemId = pickedItem.Id;

        if (string.IsNullOrEmpty(itemId))
        {
            MessageBox.Show(UiStrings.Get("inventory.add_select_first"),
                UiStrings.Get("inventory.add_item_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Read the numeric values from detail controls - always use user-specified values.
        int amount = (int)_detailAmount.Value;
        int maxAmount = (int)_detailMaxAmount.Value;

        // Determine inventory type
        string invType = "Product";
        GameItem? gameItem = null;
        if (_database != null)
        {
            (gameItem, _, _) = ResolveGameItem(itemId);
            if (gameItem != null)
                invType = ResolveInventoryTypeForItem(gameItem);
        }

        // Figurines (BOBBLE_*) installed in tech slots need the T_ prefix.
        string saveItemId = itemId;
        if (gameItem != null && IsFigurineItem(gameItem.Id) && _isTechInventory)
        {
            saveItemId = "T_" + gameItem.Id;
            invType = "Technology";
        }

        // Technology: MaxAmount = ChargeAmount (always). Amount defaults using BuildFullyCharged.
        // Verified against MXML, game save, and other editor behaviour.
        if (invType == "Technology")
        {
            int techMaxAmount = gameItem != null
                ? InventoryStackDatabase.GetMaxAmount(gameItem, "Technology", _inventoryGroup)
                : 100;
            if (maxAmount == 0) maxAmount = techMaxAmount;
            if (amount == 0)
                amount = (gameItem != null && gameItem.BuildFullyCharged)
                    ? techMaxAmount
                    : 0;
        }
        else
        {
            // For non-technology items, zero amounts are treated as unset - default to 1.
            // Negative values are intentionally preserved as they are valid game data.
            if (amount == 0 && maxAmount == 0) { amount = 1; maxAmount = 1; }
            if (maxAmount == 0) maxAmount = amount;
        }

        // Create a new slot JSON object
        var newSlot = new JsonObject();

        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", invType);
        newSlot.Add("Type", typeObj);

        // Procedural items store Id as "itemId#seed"
        string itemIdToWrite = EnsureCaretPrefix(saveItemId);
        if (gameItem != null && gameItem.IsProcedural)
        {
            int seed = Random.Shared.Next(1, int.MaxValue);
            itemIdToWrite = $"{itemIdToWrite}#{seed}";
        }
        newSlot.Add("Id", itemIdToWrite);

        newSlot.Add("Amount", amount);
        newSlot.Add("MaxAmount", maxAmount);
        newSlot.Add("DamageFactor", 0.0);
        newSlot.Add("FullyInstalled", true);
        newSlot.Add("AddedAutomatically", false);

        var indexObj = new JsonObject();
        indexObj.Add("X", _contextCell.GridX);
        indexObj.Add("Y", _contextCell.GridY);
        newSlot.Add("Index", indexObj);

        // If cell already has slot data, replace it in the array
        if (_contextCell.SlotData != null && _contextCell.SlotIndex >= 0 && _slots != null)
        {
            _slots.Set(_contextCell.SlotIndex, newSlot);
        }
        else if (_slots != null)
        {
            // Add new slot to the array
            _slots.Add(newSlot);
            _contextCell.SlotIndex = _slots.Length - 1;
        }

        // Update cell
        _contextCell.SlotData = newSlot;
        _contextCell.IsValidEmpty = false;
        _contextCell.IsEmpty = false;
        LoadCellData(_contextCell);
        _contextCell.UpdateDisplay();

        // Select the newly added cell
        SelectCell(_contextCell);
        RaiseDataModified();
    }

    private void OnRemoveItem(object? sender, EventArgs e)
    {
        if (_contextCell?.SlotData == null || _slots == null) return;

        var result = MessageBox.Show(
            UiStrings.Format("inventory.remove_confirm", _contextCell.DisplayName ?? _contextCell.ItemId, _contextCell.GridX, _contextCell.GridY),
            UiStrings.Get("inventory.remove_title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        // Remove the slot entry from the Slots array entirely and reindex
        int removedIndex = _contextCell.SlotIndex;
        if (removedIndex >= 0 && removedIndex < _slots.Length)
        {
            _slots.RemoveAt(removedIndex);

            // Reindex cells whose SlotIndex shifted down after the removal
            foreach (var c in _cells)
            {
                if (c.SlotIndex > removedIndex)
                    c.SlotIndex--;
            }
        }

        // Clear the removed cell's slot reference
        _contextCell.SlotData = null;
        _contextCell.SlotIndex = -1;

        // Update cell display
        _contextCell.ItemId = "";
        _contextCell.DisplayName = "";
        _contextCell.ItemType = "";
        _contextCell.Category = "";
        _contextCell.Amount = 0;
        _contextCell.MaxAmount = 0;
        _contextCell.DamageFactor = 0;
        _contextCell.IconImage = null;
        _contextCell.ClassMiniIcon = null;
        _contextCell.ShowAdjacencyBorder = false;
        _contextCell.AdjacencyBorderColor = Color.Transparent;
        _contextCell.IsValidEmpty = true;
        _contextCell.UpdateDisplay();

        // Recompute adjacency for the entire grid since neighbors may have changed
        int maxY = 0;
        foreach (var c in _cells)
            if (c.GridY > maxY) maxY = c.GridY;
        ComputeAdjacency(GridColumns, maxY + 1);
        foreach (var c in _cells)
            c.UpdateDisplay();

        // Clear detail panel
        if (_selectedCell == _contextCell)
            ClearDetailPanel();
        RaiseDataModified();
    }

    private void OnEnableSlot(object? sender, EventArgs e)
    {
        if (_contextCell == null || _currentInventory == null) return;
        var validSlots = _currentInventory.GetArray("ValidSlotIndices");
        if (validSlots == null)
        {
            validSlots = new JsonArray();
            _currentInventory.Set("ValidSlotIndices", validSlots);
        }

        int x = _contextCell.GridX, y = _contextCell.GridY;

        if (_contextCell.IsActivated)
        {
            // Disable: only allowed if slot has no item
            if (_contextCell.SlotData != null && !string.IsNullOrEmpty(_contextCell.ItemId)
                && _contextCell.ItemId != "^" && _contextCell.ItemId != "^YOURSLOTITEM")
            {
                MessageBox.Show(UiStrings.Get("inventory.cannot_disable"),
                    UiStrings.Get("inventory.cannot_disable_title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Remove from ValidSlotIndices
            for (int i = 0; i < validSlots.Length; i++)
            {
                var idx = validSlots.GetObject(i);
                if (idx != null && idx.GetInt("X") == x && idx.GetInt("Y") == y)
                {
                    validSlots.RemoveAt(i);
                    break;
                }
            }
            _pinnedSlots.Remove((x, y));
            _contextCell.IsActivated = false;
            _contextCell.ShowPinToggle = false;
            _contextCell.IsPinnedForAutoStack = false;
            RaisePinnedSlotsChanged();
        }
        else
        {
            // Enable: add to ValidSlotIndices
            var newIdx = new JsonObject();
            newIdx.Add("X", x);
            newIdx.Add("Y", y);
            validSlots.Add(newIdx);
            _contextCell.IsActivated = true;
            _contextCell.IsEmpty = false;
            _contextCell.IsValidEmpty = true;
            _contextCell.ShowPinToggle = _pinSlotFeatureEnabled;
            _contextCell.IsPinnedForAutoStack = IsPinnedSlot(x, y);
        }
        _contextCell.UpdateDisplay();
    }

    private void OnEnableAllSlots(object? sender, EventArgs e)
    {
        if (_currentInventory == null) return;
        var validSlots = _currentInventory.GetArray("ValidSlotIndices");
        if (validSlots == null)
        {
            validSlots = new JsonArray();
            _currentInventory.Set("ValidSlotIndices", validSlots);
        }

        var existing = new HashSet<(int, int)>();
        for (int i = 0; i < validSlots.Length; i++)
        {
            try
            {
                var idx = validSlots.GetObject(i);
                if (idx != null) existing.Add((idx.GetInt("X"), idx.GetInt("Y")));
            }
            catch { }
        }

        foreach (var cell in _cells)
        {
            if (!existing.Contains((cell.GridX, cell.GridY)))
            {
                var newIdx = new JsonObject();
                newIdx.Add("X", cell.GridX);
                newIdx.Add("Y", cell.GridY);
                validSlots.Add(newIdx);
            }
            cell.IsActivated = true;
            cell.UpdateDisplay();
        }
    }

    private void RepairSlotData(SlotCell cell)
    {
        if (cell.SlotData == null || _currentInventory == null) return;
        try { cell.SlotData.Set("DamageFactor", 0.0); } catch { }
        cell.DamageFactor = 0;

        // Remove BlockedByBrokenTech from SpecialSlots
        try
        {
            var specialSlots = _currentInventory.GetArray("SpecialSlots");
            if (specialSlots != null)
            {
                for (int i = specialSlots.Length - 1; i >= 0; i--)
                {
                    var entry = specialSlots.GetObject(i);
                    if (entry == null) continue;
                    var typeObj = entry.GetObject("Type");
                    if (typeObj == null) continue;
                    string slotType = typeObj.GetString("InventorySpecialSlotType") ?? "";
                    if (slotType != "BlockedByBrokenTech") continue;
                    var idxObj = entry.GetObject("Index");
                    if (idxObj == null) continue;
                    if (idxObj.GetInt("X") == cell.GridX && idxObj.GetInt("Y") == cell.GridY)
                        specialSlots.RemoveAt(i);
                }
            }
        }
        catch { }
    }

    private void OnRepairSlot(object? sender, EventArgs e)
    {
        if (_contextCell == null) return;
        RepairSlotData(_contextCell);
        _contextCell.UpdateDisplay();
    }

    private void OnRepairAllSlots(object? sender, EventArgs e)
    {
        foreach (var cell in _cells)
        {
            RepairSlotData(cell);
            cell.UpdateDisplay();
        }
    }

    /// <summary>
    /// Toggles or adds a supercharge on the given cell.
    /// Returns true if a supercharge was added or removed, false if blocked by constraints.
    /// </summary>
    private bool ToggleSupercharge(SlotCell cell, bool forceAdd, bool showWarnings = false)
    {
        if (_currentInventory == null || _superchargeDisabled) return false;
        var specialSlots = _currentInventory.GetArray("SpecialSlots");
        if (specialSlots == null)
        {
            specialSlots = new JsonArray();
            _currentInventory.Set("SpecialSlots", specialSlots);
        }

        int x = cell.GridX, y = cell.GridY;
        // Check if already supercharged - allow removal without constraints
        if (!forceAdd)
        {
            for (int i = specialSlots.Length - 1; i >= 0; i--)
            {
                try
                {
                    var entry = specialSlots.GetObject(i);
                    if (entry == null) continue;
                    var typeObj = entry.GetObject("Type");
                    if (typeObj == null) continue;
                    string slotType = typeObj.GetString("InventorySpecialSlotType") ?? "";
                    if (slotType != "TechBonus") continue;
                    var idxObj = entry.GetObject("Index");
                    if (idxObj == null) continue;
                    if (idxObj.GetInt("X") == x && idxObj.GetInt("Y") == y)
                    {
                        specialSlots.RemoveAt(i);
                        cell.IsSupercharged = false;
                        return true;
                    }
                }
                catch { }
            }
        }

        // Not found or forceAdd - check not already present when forceAdd
        if (forceAdd && IsSlotSupercharged(x, y))
        {
            cell.IsSupercharged = true;
            return false;
        }

        // Enforce row constraint before adding
        if (_maxSuperchargeRow >= 0 && y > _maxSuperchargeRow)
        {
            if (showWarnings)
                MessageBox.Show(
                    UiStrings.Format("inventory.supercharge_max_msg", _maxSuperchargeRow + 1),
                    UiStrings.Get("inventory.supercharge_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        // Enforce max slot count before adding
        if (_maxSuperchargedSlots >= 0 && CountSuperchargedSlots() >= _maxSuperchargedSlots)
        {
            if (showWarnings)
                MessageBox.Show(
                    UiStrings.Format("inventory.supercharge_added_msg", _maxSuperchargedSlots),
                    UiStrings.Get("inventory.supercharge_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        var newEntry = new JsonObject();
        var newType = new JsonObject();
        newType.Add("InventorySpecialSlotType", "TechBonus");
        newEntry.Add("Type", newType);
        var newIndex = new JsonObject();
        newIndex.Add("X", x);
        newIndex.Add("Y", y);
        newEntry.Add("Index", newIndex);
        specialSlots.Add(newEntry);
        cell.IsSupercharged = true;
        return true;
    }

    private void OnSuperchargeSlot(object? sender, EventArgs e)
    {
        if (_contextCell == null) return;
        ToggleSupercharge(_contextCell, false, showWarnings: true);
        _contextCell.UpdateDisplay();
    }

    private void OnSuperchargeAllSlots(object? sender, EventArgs e)
    {
        if (_superchargeDisabled || _currentInventory == null) return;
        var specialSlots = _currentInventory.GetArray("SpecialSlots");
        if (specialSlots == null)
        {
            specialSlots = new JsonArray();
            _currentInventory.Set("SpecialSlots", specialSlots);
        }

        // Build a cached set of already-supercharged positions for O(1) lookups
        var alreadySupercharged = new HashSet<(int, int)>();
        for (int i = 0; i < specialSlots.Length; i++)
        {
            try
            {
                var entry = specialSlots.GetObject(i);
                if (entry == null) continue;
                var typeObj = entry.GetObject("Type");
                if (typeObj == null) continue;
                if ((typeObj.GetString("InventorySpecialSlotType") ?? "") != "TechBonus") continue;
                var idxObj = entry.GetObject("Index");
                if (idxObj != null)
                    alreadySupercharged.Add((idxObj.GetInt("X"), idxObj.GetInt("Y")));
            }
            catch { }
        }

        int currentCount = alreadySupercharged.Count;

        foreach (var cell in _cells)
        {
            // Skip if already supercharged
            if (alreadySupercharged.Contains((cell.GridX, cell.GridY)))
            {
                cell.IsSupercharged = true;
                cell.UpdateDisplay();
                continue;
            }

            // Enforce row constraint
            if (_maxSuperchargeRow >= 0 && cell.GridY > _maxSuperchargeRow)
            {
                cell.UpdateDisplay();
                continue;
            }

            // Enforce max slot count
            if (_maxSuperchargedSlots >= 0 && currentCount >= _maxSuperchargedSlots)
            {
                cell.UpdateDisplay();
                continue;
            }

            // Add supercharge entry
            var newEntry = new JsonObject();
            var newType = new JsonObject();
            newType.Add("InventorySpecialSlotType", "TechBonus");
            newEntry.Add("Type", newType);
            var newIndex = new JsonObject();
            newIndex.Add("X", cell.GridX);
            newIndex.Add("Y", cell.GridY);
            newEntry.Add("Index", newIndex);
            specialSlots.Add(newEntry);
            cell.IsSupercharged = true;
            currentCount++;
            cell.UpdateDisplay();
        }
    }

    private void OnFillStack(object? sender, EventArgs e)
    {
        if (_contextCell?.SlotData == null) return;
        int max = _contextCell.MaxAmount;
        if (max <= 0) return;
        _contextCell.SlotData.Set("Amount", max);
        _contextCell.Amount = max;
        _contextCell.UpdateDisplay();
        if (_selectedCell == _contextCell)
            _detailAmount.Value = max;
        RaiseDataModified();
    }

    private void OnRechargeAllTech(object? sender, EventArgs e)
    {
        int recharged = 0;
        foreach (var cell in _cells)
        {
            if (cell.SlotData == null || string.IsNullOrEmpty(cell.ItemId)) continue;
            if (!cell.IsTechChargeable) continue;
            if (cell.MaxAmount <= 0) continue;
            cell.SlotData.Set("Amount", cell.MaxAmount);
            cell.Amount = cell.MaxAmount;
            cell.UpdateDisplay();
            recharged++;
        }
        if (_selectedCell != null && _selectedCell.IsTechChargeable)
            _detailAmount.Value = Math.Clamp(_selectedCell.Amount, (int)_detailAmount.Minimum, (int)_detailAmount.Maximum);
        if (recharged > 0) RaiseDataModified();
    }

    private void OnRefillAllStacks(object? sender, EventArgs e)
    {
        int refilled = 0;
        foreach (var cell in _cells)
        {
            if (cell.SlotData == null || string.IsNullOrEmpty(cell.ItemId)) continue;
            if (cell.MaxAmount <= 0) continue;
            cell.SlotData.Set("Amount", cell.MaxAmount);
            cell.Amount = cell.MaxAmount;
            cell.UpdateDisplay();
            refilled++;
        }
        if (_selectedCell != null && _selectedCell.MaxAmount > 0)
            _detailAmount.Value = Math.Clamp(_selectedCell.Amount, (int)_detailAmount.Minimum, (int)_detailAmount.Maximum);
        if (refilled > 0) RaiseDataModified();
    }

    private sealed class SlotSortEntry
    {
        public required JsonObject Slot { get; init; }
        public required string ItemId { get; init; }
        public required string SortName { get; init; }
        public required string SortCategory { get; init; }
        public int OriginalX { get; init; }
        public int OriginalY { get; init; }
    }

    private void OnSortModeChanged(object? sender, EventArgs e)
    {
        if (_suppressSortModeEvents) return;
        if (_sortModeCombo.SelectedItem is not SortModeOption option) return;

        _currentSortMode = option.Mode;
        ApplyCurrentSortMode(true);
    }

    private void OnSortByName(object? sender, EventArgs e)
    {
        SetSortMode(InventorySortMode.Name, applySort: true, raiseModified: true);
    }

    private void OnSortByCategory(object? sender, EventArgs e)
    {
        SetSortMode(InventorySortMode.Category, applySort: true, raiseModified: true);
    }

    private void OnAutoStackToStorage(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _autoStackToStorageMenuItem)
            && AutoStackSelectedSlotToStorageRequested != null
            && TryBuildAutoStackSlotRequest(out var requestArgs))
        {
            AutoStackSelectedSlotToStorageRequested?.Invoke(this, requestArgs);
            return;
        }

        AutoStackToStorageRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackToStarship(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _autoStackToStarshipMenuItem)
            && AutoStackSelectedSlotToStarshipRequested != null
            && TryBuildAutoStackSlotRequest(out var requestArgs))
        {
            AutoStackSelectedSlotToStarshipRequested?.Invoke(this, requestArgs);
            return;
        }

        AutoStackToStarshipRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackToFreighter(object? sender, EventArgs e)
    {
        if (ReferenceEquals(sender, _autoStackToFreighterMenuItem)
            && AutoStackSelectedSlotToFreighterRequested != null
            && TryBuildAutoStackSlotRequest(out var requestArgs))
        {
            AutoStackSelectedSlotToFreighterRequested?.Invoke(this, requestArgs);
            return;
        }

        AutoStackToFreighterRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool TryBuildAutoStackSlotRequest(out AutoStackSlotRequestEventArgs requestArgs)
    {
        requestArgs = null!;

        if (_contextCell == null || _contextCell.SlotData == null || string.IsNullOrEmpty(_contextCell.ItemId) || _contextCell.IsValidEmpty)
            return false;

        requestArgs = new AutoStackSlotRequestEventArgs(_contextCell.GridX, _contextCell.GridY, _contextCell.ItemId);
        return true;
    }

    private void SortInventory(Comparison<SlotSortEntry> comparison, bool raiseModified)
    {
        if (_currentInventory == null || _slots == null) return;

        _isApplyingSort = true;
        try
        {
            var entries = new List<SlotSortEntry>();
            var unsortedSlots = new List<JsonObject>();
            for (int i = 0; i < _slots.Length; i++)
            {
                JsonObject? slot;
                try { slot = _slots.GetObject(i); }
                catch { continue; }
                if (slot == null) continue;

                string itemId;
                try { itemId = ExtractItemId(slot.Get("Id")); }
                catch
                {
                    unsortedSlots.Add(slot);
                    continue;
                }

                if (string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM")
                {
                    unsortedSlots.Add(slot);
                    continue;
                }

                int x = 0;
                int y = 0;
                try
                {
                    var index = slot.GetObject("Index");
                    if (index != null)
                    {
                        x = index.GetInt("X");
                        y = index.GetInt("Y");
                    }
                }
                catch { }

                string sortName = itemId;
                string sortCategory = "";
                if (_database != null)
                {
                    var (gameItem, displayName, _, _) = ResolveItemAndDisplayName(itemId);
                    if (gameItem != null)
                    {
                        sortName = string.IsNullOrEmpty(displayName) ? itemId : displayName;
                        sortCategory = gameItem.Category ?? "";
                    }
                }

                entries.Add(new SlotSortEntry
                {
                    Slot = slot,
                    ItemId = itemId,
                    SortName = sortName,
                    SortCategory = sortCategory,
                    OriginalX = x,
                    OriginalY = y,
                });
            }

            if (entries.Count < 2) return;

            var targetPositions = GetSortablePositions(_currentInventory);
            if (targetPositions.Count == 0) return;

            targetPositions.Sort((a, b) =>
            {
                int byY = a.y.CompareTo(b.y);
                return byY != 0 ? byY : a.x.CompareTo(b.x);
            });

            entries.Sort((a, b) =>
            {
                int byRule = comparison(a, b);
                if (byRule != 0) return byRule;
                int byY = a.OriginalY.CompareTo(b.OriginalY);
                return byY != 0 ? byY : a.OriginalX.CompareTo(b.OriginalX);
            });

            int assignCount = Math.Min(entries.Count, targetPositions.Count);
            for (int i = 0; i < assignCount; i++)
                InventorySlotHelper.UpdateSlotIndex(entries[i].Slot, targetPositions[i].x, targetPositions[i].y);

            var newSlots = new JsonArray();
            foreach (var entry in entries)
                newSlots.Add(entry.Slot);
            foreach (var unsorted in unsortedSlots)
                newSlots.Add(unsorted);

            _currentInventory.Set("Slots", newSlots);
            _slots = newSlots;
            LoadInventory(_currentInventory);
            if (raiseModified)
                RaiseDataModified();
        }
        finally
        {
            _isApplyingSort = false;
        }
    }

    private static List<(int x, int y)> GetSortablePositions(JsonObject inventory)
    {
        var positions = new List<(int x, int y)>();
        var seen = new HashSet<(int x, int y)>();

        try
        {
            var validSlots = inventory.GetArray("ValidSlotIndices");
            if (validSlots != null)
            {
                for (int i = 0; i < validSlots.Length; i++)
                {
                    try
                    {
                        var idx = validSlots.GetObject(i);
                        if (idx == null) continue;
                        var pos = (idx.GetInt("X"), idx.GetInt("Y"));
                        if (seen.Add(pos)) positions.Add(pos);
                    }
                    catch { }
                }
            }
        }
        catch { }

        if (positions.Count > 0) return positions;

        int width = GridColumns;
        int height = 1;
        try
        {
            int invWidth = inventory.GetInt("Width");
            int invHeight = inventory.GetInt("Height");
            if (invWidth > 0) width = invWidth;
            if (invHeight > 0) height = invHeight;
        }
        catch { }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                positions.Add((x, y));
        }
        return positions;
    }

    private void OnCopyItem(object? sender, EventArgs e)
    {
        if (_contextCell == null || _contextCell.SlotData == null || string.IsNullOrEmpty(_contextCell.ItemId)) return;
        // Store a reference to the copied cell (deep copy not needed, values will be read on paste)
        _copiedItemCell = _contextCell;
    }

    private void OnPasteItem(object? sender, EventArgs e)
    {
        if (_contextCell == null || _copiedItemCell == null || _slots == null) return;

        // Read item values from copied cell
        string itemId = _copiedItemCell.ItemId;
        int amount = _copiedItemCell.Amount;
        int maxAmount = _copiedItemCell.MaxAmount;
        double damageFactor = _copiedItemCell.DamageFactor;

        // Determine inventory type from database
        string invType = "Product";
        if (_database != null)
        {
            var (gameItem, _, _) = ResolveGameItem(itemId);
            if (gameItem != null)
            {
                invType = ResolveInventoryTypeForItem(gameItem);
            }
        }

        // Create new slot object for paste
        var newSlot = new JsonObject();
        var typeObj = new JsonObject();
        typeObj.Add("InventoryType", invType);
        newSlot.Add("Type", typeObj);

        newSlot.Add("Id", EnsureCaretPrefix(itemId));

        newSlot.Add("Amount", amount);
        newSlot.Add("MaxAmount", maxAmount);
        newSlot.Add("DamageFactor", damageFactor);
        newSlot.Add("FullyInstalled", true);
        newSlot.Add("AddedAutomatically", false);

        var indexObj = new JsonObject();
        indexObj.Add("X", _contextCell.GridX);
        indexObj.Add("Y", _contextCell.GridY);
        newSlot.Add("Index", indexObj);

        // Replace or add slot in inventory
        if (_contextCell.SlotData != null && _contextCell.SlotIndex >= 0)
        {
            _slots.Set(_contextCell.SlotIndex, newSlot);
        }
        else
        {
            _slots.Add(newSlot);
            _contextCell.SlotIndex = _slots.Length - 1;
        }

        // Update cell
        _contextCell.SlotData = newSlot;
        _contextCell.IsValidEmpty = false;
        _contextCell.IsEmpty = false;
        LoadCellData(_contextCell);
        _contextCell.UpdateDisplay();

        // Select the pasted cell
        SelectCell(_contextCell);
        RaiseDataModified();
    }

    private void OnResizeInventory(object? sender, EventArgs e)
    {
        if (_currentInventory == null) return;

        int newWidth = (int)_resizeWidth.Value;
        int newHeight = (int)_resizeHeight.Value;

        var validSlots = _currentInventory.GetArray("ValidSlotIndices");
        if (validSlots == null)
        {
            validSlots = new JsonArray();
            _currentInventory.Set("ValidSlotIndices", validSlots);
        }

        // Build set of existing valid indices
        var existingValid = new HashSet<(int, int)>();
        for (int i = 0; i < validSlots.Length; i++)
        {
            try
            {
                var idx = validSlots.GetObject(i);
                if (idx != null) existingValid.Add((idx.GetInt("X"), idx.GetInt("Y")));
            }
            catch { }
        }

        // Remove ValidSlotIndices outside new dimensions
        for (int i = validSlots.Length - 1; i >= 0; i--)
        {
            try
            {
                var idx = validSlots.GetObject(i);
                if (idx != null)
                {
                    int x = idx.GetInt("X"), y = idx.GetInt("Y");
                    if (x >= newWidth || y >= newHeight)
                        validSlots.RemoveAt(i);
                }
            }
            catch { }
        }

        // Add ValidSlotIndices for new positions
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                if (!existingValid.Contains((x, y)))
                {
                    var newIdx = new JsonObject();
                    newIdx.Add("X", x);
                    newIdx.Add("Y", y);
                    validSlots.Add(newIdx);
                }
            }
        }

        // Update inventory Width and Height so LoadInventory reads the new dimensions
        _currentInventory.Set("Width", newWidth);
        _currentInventory.Set("Height", newHeight);

        // Reset corvette part matches so they can be re-resolved during reload
        if (_corvettePartCollection != null)
            foreach (var entry in _corvettePartCollection)
                entry.Found = false;

        // Reload the grid
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    private void OnExportInventory(object? sender, EventArgs e)
    {
        if (_currentInventory == null)
        {
            MessageBox.Show(UiStrings.Get("inventory.no_inventory_export"), UiStrings.Get("inventory.export_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = _exportFilter,
            DefaultExt = _exportDefaultExt,
            FileName = _exportFileName
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var json = JsonParser.Serialize(_currentInventory, true, skipReverseMapping: true);
            File.WriteAllText(dialog.FileName, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("inventory.export_error", ex.Message), UiStrings.Get("inventory.export_error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnImportInventory(object? sender, EventArgs e)
    {
        if (_currentInventory == null)
        {
            MessageBox.Show(UiStrings.Get("inventory.no_inventory_import"), UiStrings.Get("inventory.import_title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = _importFilter,
            DefaultExt = _exportDefaultExt
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            var imported = JsonParser.ParseObject(json);

            // Try to locate inventory data inside the parsed object.
            // Supports raw inventory files (Slots at top level) as well as
            // NMSSaveEditor / NomNom wrapper formats where inventory is nested.
            var inventory = Core.InventoryImportHelper.FindInventoryObject(imported);
            if (inventory == null)
            {
                MessageBox.Show(UiStrings.Get("inventory.import_bad_format"),
                    UiStrings.Get("inventory.import_error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Copy relevant fields from imported inventory into current inventory
            var importedSlots = inventory.GetArray("Slots");
            _currentInventory.Set("Slots", importedSlots!);

            var importedValid = inventory.GetArray("ValidSlotIndices");
            if (importedValid != null)
                _currentInventory.Set("ValidSlotIndices", importedValid);

            var importedSpecial = inventory.GetArray("SpecialSlots");
            if (importedSpecial != null)
                _currentInventory.Set("SpecialSlots", importedSpecial);

            // Update dimensions if present in the imported data
            try
            {
                int w = inventory.GetInt("Width");
                int h = inventory.GetInt("Height");
                if (w > 0 && h > 0)
                {
                    _currentInventory.Set("Width", w);
                    _currentInventory.Set("Height", h);
                }
            }
            catch { }

            // Reload the grid with the imported data
            LoadInventory(_currentInventory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(UiStrings.Format("inventory.import_failed", ex.Message), UiStrings.Get("inventory.import_error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearDetailPanel()
    {
        _detailItemName.Text = UiStrings.Get("inventory.no_slot_selected");
        _detailSlotPosition.Text = "";
        _detailItemType.Text = "";
        _detailItemCategory.Text = "";
        _detailItemId.Text = "";
        _detailAmount.Value = 0;
        _detailMaxAmount.Value = 0;
        _detailDamageFactor.Value = 0;
        _detailDescription.Text = "";
        _detailIcon.Image = null;
        _detailAmount.Enabled = true;
        _detailMaxAmount.Enabled = true;
        _applyButton.Enabled = false;
    }

    /// <summary>
    /// Changes are applied immediately via OnApplyChanges/OnAddItem/OnRemoveItem.
    /// </summary>
    public void SaveInventory(JsonObject? inventory)
    {
        // Changes are written directly to slot JsonObjects
    }

    public void ApplyUiLocalisation()
    {
        // Resize controls
        _resizeWidthLabel.Text = UiStrings.Get("inventory.width");
        _resizeHeightLabel.Text = UiStrings.Get("inventory.height");
        _resizeButton.Text = UiStrings.Get("inventory.resize");
        _sortModeLabel.Text = UiStrings.Get("inventory.toolbar_sort");
        _autoStackDropDownButton.Text = UiStrings.Get("inventory.toolbar_auto_stack");
        _autoStackToChestsButtonMenuItem.Text = UiStrings.Get("inventory.toolbar_auto_stack_chests");
        _autoStackToStarshipButtonMenuItem.Text = UiStrings.Get("inventory.toolbar_auto_stack_starship");
        _autoStackToFreighterButtonMenuItem.Text = UiStrings.Get("inventory.toolbar_auto_stack_freighter");
        PopulateSortModeOptions();

        // Detail panel labels
        _detailAmountLabel.Text = UiStrings.Get("inventory.amount");
        _detailMaxLabel.Text = UiStrings.Get("inventory.max");
        _applyButton.Text = UiStrings.Get("inventory.apply_changes_title");

        // Search
        _searchBox.PlaceholderText = UiStrings.Get("inventory.search_placeholder");
        _searchButton.Text = UiStrings.Get("inventory.search");

        // Context menu
        _addItemMenuItem.Text = UiStrings.Get("inventory.ctx_add_item");
        _removeItemMenuItem.Text = UiStrings.Get("inventory.ctx_remove_item");
        _enableSlotMenuItem.Text = UiStrings.Get("inventory.ctx_enable_slot");
        _enableAllSlotsMenuItem.Text = UiStrings.Get("inventory.ctx_enable_all");
        _repairSlotMenuItem.Text = UiStrings.Get("inventory.ctx_repair_slot");
        _repairAllSlotsMenuItem.Text = UiStrings.Get("inventory.ctx_repair_all");
        _superchargeSlotMenuItem.Text = UiStrings.Get("inventory.ctx_supercharge");
        _superchargeAllSlotsMenuItem.Text = UiStrings.Get("inventory.ctx_supercharge_all");
        _fillStackMenuItem.Text = UiStrings.Get("inventory.ctx_fill_stack");
        _rechargeAllTechMenuItem.Text = UiStrings.Get("inventory.ctx_recharge_all");
        _refillAllStacksMenuItem.Text = UiStrings.Get("inventory.ctx_refill_all");
        _copyItemMenuItem.Text = UiStrings.Get("inventory.ctx_copy");
        _pasteItemMenuItem.Text = UiStrings.Get("inventory.ctx_paste");
        _sortByNameMenuItem.Text = UiStrings.Get("inventory.ctx_sort_name");
        _sortByCategoryMenuItem.Text = UiStrings.Get("inventory.ctx_sort_category");
        _autoStackToStorageMenuItem.Text = UiStrings.Get("inventory.ctx_auto_stack_chests");
        _autoStackToStarshipMenuItem.Text = UiStrings.Get("inventory.ctx_auto_stack_starship");
        _autoStackToFreighterMenuItem.Text = UiStrings.Get("inventory.ctx_auto_stack_freighter");
        _pinSlotMenuItem.Text = UiStrings.Get("inventory.ctx_pin_slot");

        // Import/Export buttons
        _importButton.Text = UiStrings.Get("common.import");
        _exportButton.Text = UiStrings.Get("common.export");

        // Section headers
        _slotDetailHeader.Text = UiStrings.Get("inventory.slot_details");
        _itemPickerHeader.Text = UiStrings.Get("inventory.item_picker");

        // Detail panel labels
        _detailNameLabel.Text = UiStrings.Get("inventory.label_name");
        _detailPositionLabel.Text = UiStrings.Get("inventory.label_position");
        _detailTypeLabel.Text = UiStrings.Get("inventory.label_type");
        _detailCategoryLabel.Text = UiStrings.Get("inventory.label_category");
        _detailItemIdLabel.Text = UiStrings.Get("inventory.label_item_id");
        _detailDamageLabel.Text = UiStrings.Get("inventory.label_damage");

        // Search/filter labels
        _searchLabel.Text = UiStrings.Get("inventory.label_search");
        _typeFilterLabel.Text = UiStrings.Get("inventory.label_filter_type");
        _categoryFilterLabel.Text = UiStrings.Get("inventory.label_filter_category");
        _itemFilterLabel.Text = UiStrings.Get("inventory.label_filter_item");

        // Re-populate type filter to pick up localised display names
        PopulateTypeFilter();
    }

    /// <summary>
    /// MarqueeLabel.
    /// Provides a label that horizontally scrolls when the text width exceeds the control width.
    /// </summary>
    public class MarqueeLabel : Label
    {
        private System.Windows.Forms.Timer? _timer;
        private int _offset;
        private bool _shouldScroll;

        [DefaultValue(90)]
        public int ScrollSpeed { get; set; } = 90; // ms per tick

        public MarqueeLabel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = ScrollSpeed;
            _timer.Tick += (s, e) =>
            {
                if (IsDisposed || !IsHandleCreated || !Visible) { _timer?.Stop(); return; }
                _offset += 2;
                Invalidate();
            };
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            CheckScroll();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            CheckScroll();
        }

        private void CheckScroll()
        {
            if (IsDisposed || !IsHandleCreated) return;
            using var g = CreateGraphics();
            var textWidth = (int)g.MeasureString(Text, Font).Width;
            _shouldScroll = textWidth > Width * 0.9;
            if (_shouldScroll && Visible)
            {
                _offset = 0;
                _timer?.Start();
            }
            else
            {
                _timer?.Stop();
                _offset = 0;
            }
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!Visible) _timer?.Stop();
            else if (IsHandleCreated) CheckScroll();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _timer?.Stop();
            base.OnHandleDestroyed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            var textWidth = (int)e.Graphics.MeasureString(Text, Font).Width;
            if (_shouldScroll)
            {
                using var brush = new SolidBrush(ForeColor);
                int x = -_offset;
                while (x < Width)
                {
                    e.Graphics.DrawString(Text, Font, brush, x, 0);
                    x += textWidth + 40; // gap between repeats
                }
                if (_offset > textWidth + 40)
                    _offset = 0;
            }
            else
            {
                // Use Graphics.DrawString (GDI+) for color emoji rendering via COLR table 0
                using var brush = new SolidBrush(ForeColor);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter };
                e.Graphics.DrawString(Text, Font, brush, ClientRectangle, sf);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _timer?.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Tracks a base object in a corvette's PlayerShipBase for matching against CV_ tech items.
    /// </summary>
    private class CorvettePartEntry
    {
        public string Id { get; set; } = "";
        public bool Found { get; set; }
    }

    /// <summary>
    /// Wraps a raw category string with a human-readable display name.
    /// PascalCase values like "AllShips" are shown as "All Ships" in the picker
    /// while keeping the raw value for filtering.
    /// </summary>
    private sealed class CategoryDisplayItem
    {
        public string RawValue { get; }
        public string DisplayName { get; }

        public CategoryDisplayItem(string rawValue)
        {
            RawValue = rawValue;
            DisplayName = NormalizeCategoryDisplay(rawValue);
        }

        public override string ToString() => DisplayName;

        /// <summary>
        /// Convert PascalCase or ALL-CAPS category names to space-separated readable text.
        /// e.g. "AllShips" -> "All Ships", "AllShipsExceptAlien" -> "All Ships Except Alien",
        /// "CONSUMABLE" -> "Consumable", "PROCEDURAL" -> "Procedural".
        /// Single-word names like "Suit", "Ship", "Weapon" are returned unchanged.
        /// </summary>
        internal static string NormalizeCategoryDisplay(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // Handle ALL-CAPS words: title-case them first (e.g. "CONSUMABLE" -> "Consumable")
            if (raw.Length > 1 && raw == raw.ToUpperInvariant())
                return char.ToUpper(raw[0]) + raw[1..].ToLower();

            // Insert space before each uppercase letter that follows a lowercase letter
            // or before an uppercase letter followed by a lowercase letter after another uppercase
            var spaced = System.Text.RegularExpressions.Regex.Replace(raw, @"(?<=[a-z])([A-Z])|(?<=[A-Z])([A-Z][a-z])", " $1$2");
            return spaced;
        }
    }

    /// <summary>
    /// Wraps a raw item-type name (JSON filename) with a localised display name.
    /// The InternalName is used for data filtering; DisplayName is shown in the UI.
    /// </summary>
    private sealed class TypeDisplayItem
    {
        public string InternalName { get; }
        public string DisplayName { get; }

        public TypeDisplayItem(string internalName)
        {
            InternalName = internalName;
            string locKey = "item_type." + internalName
                .Replace(" ", "_")
                .ToLowerInvariant();
            string locValue = UiStrings.Get(locKey);
            // Fall back to the raw name if the loc key returns the key itself
            DisplayName = locValue == locKey ? internalName : locValue;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// Returns a localised category name for display in the detail panel.
    /// Looks up "item_category.{normalised_key}" and falls back to NormalizeCategoryDisplay.
    /// </summary>
    private static string GetLocalisedCategoryName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        string locKey = "item_category." + CategoryDisplayItem.NormalizeCategoryDisplay(raw)
            .ToLowerInvariant()
            .Replace(" & ", "_")
            .Replace("'", "")
            .Replace(" ", "_");
        string locValue = UiStrings.Get(locKey);
        return locValue == locKey
            ? CategoryDisplayItem.NormalizeCategoryDisplay(raw)
            : locValue;
    }

    /// <summary>
    /// Individual slot cell in the inventory grid.
    /// </summary>
    private class SlotCell : Panel
    {
        // Shared fonts reduce GDI object allocation per cell.
        // These live for the application lifetime (3 GDI objects) and must NOT be disposed.
        private static readonly Font SharedNameFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        private static readonly Font SharedAmountFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        private static readonly Font SharedElementBadgeFont = new Font("Segoe UI", 16f, FontStyle.Bold);

        private readonly Panel _iconContainer;
        private readonly PictureBox _iconBox; // inner PictureBox that we offset upward
        private readonly MarqueeLabel _nameLabel;
        private readonly Label _amountLabel;
        private readonly Label _pinLabel;
        private readonly ToolTip _toolTip;
        private Image? _compositeImage; // tracks composite bitmaps we create so only they are disposed

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int GridX { get; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int GridY { get; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SlotIndex { get; set; } = -1;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public JsonObject? SlotData { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ItemId { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DisplayName { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ItemType { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Category { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Amount { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int MaxAmount { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double DamageFactor { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsEmpty { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsValidEmpty { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? IconImage { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsActivated { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSupercharged { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ItemClass { get; set; } = "";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image? ClassMiniIcon { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowAdjacencyBorder { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AdjacencyBorderColor { get; set; } = Color.Transparent;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsInTechInventory { get; set; }
        /// <summary>
        /// Whether the game data flags this item as rechargeable (Chargeable == true).
        /// Set from <see cref="GameItem.IsChargeable"/> when the cell is populated.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsChargeable { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowPinToggle { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsPinnedForAutoStack { get; set; }

        public event EventHandler? PinToggleClicked;

        /// <summary>Cached corvette-resolved display name. Preserved across drag/drop so
        /// the greedy GuessCorvetteBasePart match is not lost when cells move.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CorvetteDisplayName { get; set; }
        /// <summary>Cached corvette-resolved icon name for re-loading after drag/drop.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? CorvetteIconName { get; set; }

        /// <summary>Triggers a repaint to show/hide adjacency border.</summary>
        public void UpdateBorderOverlay()
        {
            Invalidate();
        }

        /// <summary>
        /// True when this is a chargeable technology item that displays charge as
        /// a percentage. For technology items this is only true when
        /// the game data's <c>Chargeable</c> flag is set.
        /// Items like <c>Advanced Mining Laser</c>, <c>Analysis Visor</c>, and <c>Combat Scope</c> have
        /// <c>Chargeable: false</c> despite having non-zero Amount/MaxAmount in the
        /// save file, so they must not show a charge bar.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsTechChargeable =>
            IsInTechInventory
            && IsChargeable
            && Amount >= 0 && MaxAmount > 0;

        private bool _isSelected;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; UpdateDisplay(); }
        }

        public SlotCell(int x, int y, int width, int height, ToolTip sharedToolTip)
        {
            GridX = x;
            GridY = y;
            Size = new Size(width, height);
            BorderStyle = BorderStyle.FixedSingle;
            Cursor = Cursors.Hand;
            BackColor = Color.FromArgb(50, 50, 50);
            DoubleBuffered = true;

            // Suspend layout during child control setup to avoid per-add recalculations
            SuspendLayout();

            // Name label replaced with a MarqueeLabel that scrolls long text
            _nameLabel = new MarqueeLabel
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(160, 0, 0, 0),
                Font = SharedNameFont,
                ForeColor = Color.White,
                Height = 18,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            // Container for icon (fills middle area)
            _iconContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Inner PictureBox that we will size slightly taller and shift up
            _iconBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Location = new Point(0, -16) // initial upward offset
            };

            // Ensure inner picturebox resizes when the container changes
            _iconContainer.Resize += (s, e) => UpdateIconLayout();
            _iconContainer.Controls.Add(_iconBox);

            _amountLabel = new Label
            {
                Dock = DockStyle.Bottom,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(128, 0, 0, 0),
                Font = SharedAmountFont,
                TextAlign = ContentAlignment.TopCenter,
                Height = 16,
                AutoEllipsis = true
            };

            _pinLabel = new Label
            {
                AutoSize = false,
                Size = new Size(16, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(128, 0, 0, 0),
                ForeColor = Color.White,
                Font = SharedNameFont,
                Padding = new Padding(0, 0, 0, 0),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _pinLabel.Location = new Point(Math.Max(0, Width - _pinLabel.Width), Math.Max(2, Height - _amountLabel.Height - _pinLabel.Height - 3));
            _pinLabel.Click += (s, e) => PinToggleClicked?.Invoke(this, EventArgs.Empty);
            _pinLabel.MouseEnter += (s, e) => _pinLabel.ForeColor = Color.White;
            _pinLabel.MouseLeave += (s, e) => _pinLabel.ForeColor = IsPinnedForAutoStack ? Color.Gold : Color.Gainsboro;
            Resize += (s, e) => _pinLabel.Location = new Point(Math.Max(0, Width - _pinLabel.Width), Math.Max(2, Height - _amountLabel.Height - _pinLabel.Height - 3));

            _toolTip = sharedToolTip;

            // Add controls in z-order: amount (bottom), icon container (middle), name (top)
            Controls.Add(_amountLabel);
            Controls.Add(_iconContainer);
            Controls.Add(_nameLabel);
            Controls.Add(_pinLabel);
            _pinLabel.BringToFront();

            // Forward child clicks to this panel for cell selection
            _iconBox.Click += (s, e) => OnClick(e);
            _amountLabel.Click += (s, e) => OnClick(e);
            _nameLabel.Click += (s, e) => OnClick(e);

            // Draw adjacency border segments on top of each child control
            _nameLabel.Paint += OnChildPaintBorder;
            _iconContainer.Paint += OnChildPaintBorder;
            _amountLabel.Paint += OnChildPaintBorder;
            _iconBox.Paint += OnChildPaintBorder;

            // Initial layout
            UpdateIconLayout();

            ResumeLayout(false);
        }

        private void UpdateIconLayout()
        {
            const int lift = 16; // how many pixels to lift icon up
            if (_iconContainer == null || _iconBox == null) return;

            // Make the inner picturebox slightly taller than the container and move it up
            var w = Math.Max(1, _iconContainer.ClientSize.Width);
            var h = Math.Max(1, _iconContainer.ClientSize.Height + lift);
            _iconBox.Size = new Size(w, h);
            _iconBox.Location = new Point(0, -lift);
        }

        /// <summary>
        /// Draws the adjacency border segment on a child control, translated to
        /// the child's coordinate system so the border appears on top of the child.
        /// </summary>
        private void OnChildPaintBorder(object? sender, PaintEventArgs e)
        {
            if (!ShowAdjacencyBorder || AdjacencyBorderColor == Color.Transparent) return;
            if (sender is not Control child) return;

            // Calculate the child's position relative to this SlotCell
            Point offset = child.Parent == this
                ? child.Location
                : new Point(child.Location.X + child.Parent!.Location.X,
                            child.Location.Y + child.Parent!.Location.Y);

            // Border rectangle in child's coordinate space
            var borderRect = new Rectangle(
                -offset.X, -offset.Y,
                ClientSize.Width, ClientSize.Height);

            using var pen = new Pen(AdjacencyBorderColor, 2f);
            pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
            e.Graphics.DrawRectangle(pen, borderRect);
        }

        /// <summary>
        /// Draw adjacency border on the parent background (visible in any gaps
        /// between child controls).
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (ShowAdjacencyBorder && AdjacencyBorderColor != Color.Transparent)
            {
                var r = ClientRectangle;
                using var pen = new Pen(AdjacencyBorderColor, 2f);
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        public void UpdateDisplay()
        {
            if (IsEmpty)
            {
                // Disabled slot - not in ValidSlotIndices and no data
                Color bg = _isSelected ? Color.FromArgb(70, 80, 100) : Color.FromArgb(25, 25, 25);
                if (!IsActivated) bg = Color.FromArgb(80, 20, 20); // Red tint for disabled
                BackColor = bg;
                BorderStyle = BorderStyle.FixedSingle;
                _iconBox.Image = null;
                ClassMiniIcon = null;
                _nameLabel.Text = "";
                _nameLabel.Visible = false;
                _amountLabel.Text = "";
                _amountLabel.Visible = false;
                Cursor = Cursors.Hand; // Allow right-click to enable
                string tip = IsActivated ? UiStrings.Format("inventory.tooltip_empty", GridX, GridY) : UiStrings.Format("inventory.tooltip_disabled", GridX, GridY);
                _toolTip.SetToolTip(this, tip);
                _toolTip.SetToolTip(_iconBox, tip);
                UpdatePinIndicator();
                return;
            }

            if (IsValidEmpty && string.IsNullOrEmpty(ItemId))
            {
                // Valid empty slot - available for adding items
                Color bg;
                if (IsSupercharged)
                    bg = _isSelected ? Color.FromArgb(180, 160, 60) : Color.Gold;
                else if (_isSelected)
                    bg = Color.FromArgb(70, 100, 160);
                else
                    bg = Color.FromArgb(45, 45, 50);
                if (!IsActivated) bg = Color.FromArgb(80, 30, 30); // Inactive = transparent red tint
                BackColor = bg;
                //BorderStyle = IsSupercharged ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
                _iconBox.Image = null;
                ClassMiniIcon = null;
                _nameLabel.Text = IsSupercharged ? SuperchargeIndicator : "";
                _nameLabel.ForeColor = IsSupercharged ? Color.Gold : Color.White;
                _nameLabel.Visible = IsSupercharged;
                _amountLabel.Text = "";
                _amountLabel.Visible = false;
                Cursor = Cursors.Hand;
                string tip = UiStrings.Format("inventory.tooltip_empty_slot", GridX, GridY);
                _toolTip.SetToolTip(this, tip);
                _toolTip.SetToolTip(_iconBox, tip);
                _toolTip.SetToolTip(_nameLabel, tip);
                UpdatePinIndicator();
                return;
            }

            // Set background color by item type
            Color baseColor;
            if (IsSupercharged)
                baseColor = Color.Gold; // Match the supercharged text color
            else if (_isSelected)
                baseColor = Color.FromArgb(80, 120, 200);
            else if (ItemType.Contains("technology", StringComparison.OrdinalIgnoreCase))
                baseColor = Color.FromArgb(40, 60, 120);
            else if (ItemType.Contains("product", StringComparison.OrdinalIgnoreCase))
                baseColor = Color.FromArgb(120, 80, 30);
            else if (ItemType.Contains("substance", StringComparison.OrdinalIgnoreCase))
                baseColor = Color.FromArgb(30, 100, 100);
            else if (!string.IsNullOrEmpty(ItemId))
                baseColor = Color.FromArgb(60, 60, 60);
            else
                baseColor = Color.FromArgb(50, 50, 50);

            // Non-activated slots get a red tint overlay for clear visual distinction
            if (!IsActivated && !_isSelected)
                baseColor = Color.FromArgb(
                    Math.Min(255, baseColor.R / 2 + 60),
                    baseColor.G / 4,
                    baseColor.B / 4);

            // Adjacency border: drawn on top of children via Paint event handlers
            if (ShowAdjacencyBorder && AdjacencyBorderColor != Color.Transparent)
            {
                BackColor = baseColor;
                _iconContainer.BackColor = baseColor;
                BorderStyle = BorderStyle.None;
            }
            else
            {
                BackColor = baseColor;
                _iconContainer.BackColor = Color.Transparent;
                //BorderStyle = IsSupercharged ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
            }

            // Composite class mini icon directly onto item icon bitmap (avoids
            // WinForms transparency issues with overlapping PictureBox controls)
            // Also renders element symbol badge if applicable
            string? elementSymbol = !string.IsNullOrEmpty(DisplayName) ? ElementDatabase.GetSymbol(DisplayName) : null;
            bool needsComposite = (IconImage != null && ClassMiniIcon != null) || elementSymbol != null;

            // Dispose previous composite bitmap (if any) to avoid memory leaks.
            // Only dispose composites we created - never shared cached icons from IconManager.
            var oldComposite = _compositeImage;

            if (needsComposite && IconImage != null)
            {
                var composite = new Bitmap(IconImage.Width, IconImage.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(composite))
                {
                    g.DrawImage(IconImage, 0, 0, IconImage.Width, IconImage.Height);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    if (ClassMiniIcon != null)
                        g.DrawImage(ClassMiniIcon, -IconImage.Width*0.05f, 0, IconImage.Width*0.5f, IconImage.Height*0.5f);

                    // Draw element symbol badge (capsule: rectangle with rounded left/right sides)
                    if (elementSymbol != null)
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        var font = SharedElementBadgeFont;
                        var textSize = g.MeasureString(elementSymbol, font);
                        float pad = 2f;
                        float buffer = 10f; // Adjustable buffer for capsule length
                        float bh = textSize.Height + pad;
                        float bw = textSize.Width + pad * 2 + buffer;
                        float bx = IconImage.Width * 0.04f;
                        float by = IconImage.Height * 0.04f;
                        float radius = bh / 2f; // Capsule: radius = half height

                        // Ensure capsule is always at least a bit longer than tall
                        if (bw < bh * 1.5f)
                            bw = bh * 1.5f;

                        using var badgePath = new System.Drawing.Drawing2D.GraphicsPath();
                        // Left end
                        badgePath.AddArc(bx, by, bh, bh, 90, 180);
                        // Top edge
                        badgePath.AddLine(bx + radius, by, bx + bw - radius, by);
                        // Right end
                        badgePath.AddArc(bx + bw - bh, by, bh, bh, 270, 180);
                        // Bottom edge
                        badgePath.AddLine(bx + bw - radius, by + bh, bx + radius, by + bh);
                        badgePath.CloseFigure();

                        using var bgBrush = new SolidBrush(Color.FromArgb(209, 227, 226, 200));
                        g.FillPath(bgBrush, badgePath);
                        using var textBrush = new SolidBrush(Color.FromArgb(53, 57, 57));
                        // Center text horizontally in the capsule
                        float textX = bx + (bw - textSize.Width) / 2f;
                        float textY = by + pad * 0.5f;
                        g.DrawString(elementSymbol, font, textBrush, textX, textY);
                    }
                }
                _iconBox.Image = composite;
                _compositeImage = composite;
            }
            else
            {
                _iconBox.Image = IconImage;
                _compositeImage = null;
            }

            // Dispose old composite only - never dispose shared cached icons
            if (oldComposite != null)
                oldComposite.Dispose();

            // Display item name at top (with supercharge indicator)
            string nameText = !string.IsNullOrEmpty(DisplayName) ? DisplayName : ItemId;
            if (IsSupercharged && !string.IsNullOrEmpty(nameText))
                nameText = SuperchargeIndicator + nameText;
            if (!string.IsNullOrEmpty(nameText))
            {
                _nameLabel.Text = nameText;
                _nameLabel.ForeColor = IsSupercharged ? Color.Gold : Color.White;
                _nameLabel.Visible = true;
            }
            else
            {
                _nameLabel.Text = "";
                _nameLabel.Visible = false;
            }

            // Display amount overlay at bottom
            // Chargeable tech items show charge as a percentage.
            // Non-chargeable tech items in tech inventories have fixed
            // Amount/MaxAmount values that are meaningless - hide them.
            // Products and substances show "Amount/MaxAmount".
            if (IsTechChargeable && MaxAmount > 0)
            {
                int pct = (int)Math.Ceiling((double)Amount / MaxAmount * 100);
                _amountLabel.Text = $"{pct}%";
                _amountLabel.Visible = true;
            }
            else if (IsInTechInventory && !IsChargeable)
            {
                // Non-chargeable installed technology: hide amount label
                _amountLabel.Text = "";
                _amountLabel.Visible = false;
            }
            else
            {
                _amountLabel.Text = $"{Amount}/{MaxAmount}";
                _amountLabel.Visible = true;
            }

            // Tooltip
            string tip2 = !string.IsNullOrEmpty(DisplayName) ? DisplayName : ItemId;
            if (IsSupercharged) tip2 = SuperchargeIndicator + " " + tip2;
            if (!IsActivated) tip2 += " [disabled]";
            if (IsTechChargeable && MaxAmount > 0)
            {
                int pct2 = (int)Math.Ceiling((double)Amount / MaxAmount * 100);
                tip2 += $" ({pct2}%)";
            }
            else if (IsInTechInventory && !IsChargeable)
            {
                // No amount info in tooltip for non-chargeable installed tech
            }
            else
                tip2 += $" ({Amount}/{MaxAmount})";
            _toolTip.SetToolTip(this, tip2);
            _toolTip.SetToolTip(_iconBox, tip2);
            _toolTip.SetToolTip(_amountLabel, tip2);
            _toolTip.SetToolTip(_nameLabel, tip2);
            UpdatePinIndicator();
        }

        private void UpdatePinIndicator()
        {
            bool show = ShowPinToggle && IsActivated;
            _pinLabel.Visible = show;
            if (!show)
                return;

            _pinLabel.Text = IsPinnedForAutoStack ? "🔒" : "🔓";
            _pinLabel.ForeColor = IsPinnedForAutoStack ? Color.Gold : Color.Gainsboro;
            _toolTip.SetToolTip(_pinLabel,
                IsPinnedForAutoStack
                    ? UiStrings.Get("inventory.tooltip_pinned_slot")
                    : UiStrings.Get("inventory.tooltip_unpinned_slot"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Do NOT dispose _toolTip - it is shared across all cells and
                // owned by the parent InventoryGridPanel.
                // Only dispose our composite bitmap, never shared cached icons.
                if (_compositeImage != null)
                {
                    _iconBox!.Image = null;
                    _compositeImage.Dispose();
                    _compositeImage = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}