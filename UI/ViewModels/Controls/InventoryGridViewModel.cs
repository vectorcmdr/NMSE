using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMSE.Core;
using NMSE.Data;
using NMSE.Models;

namespace NMSE.UI.ViewModels.Controls;

public partial class InventorySlotViewModel : ObservableObject
{
    [ObservableProperty] private string _itemId = "";
    [ObservableProperty] private string _itemName = "";
    [ObservableProperty] private int _amount;
    [ObservableProperty] private int _maxAmount;
    [ObservableProperty] private decimal _damageFactor;
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private bool _isSupercharged;
    [ObservableProperty] private bool _isDamaged;
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isDragOver;
    [ObservableProperty] private Bitmap? _icon;
    [ObservableProperty] private string _displayAmount = "";
    [ObservableProperty] private string _chargeDisplay = "";
    [ObservableProperty] private bool _showCharge;
    [ObservableProperty] private int _gridRow;
    [ObservableProperty] private int _gridCol;
    [ObservableProperty] private string _itemType = "";
    [ObservableProperty] private string _itemCategory = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _position = "";

    // Adjacency border
    [ObservableProperty] private Avalonia.Thickness _adjBorderThickness;
    [ObservableProperty] private string _adjBorderColor = "Transparent";
    [ObservableProperty] private bool _hasAdjacencyBorder;

    // Cell visual enhancements
    [ObservableProperty] private Bitmap? _classMiniIcon;
    [ObservableProperty] private bool _hasClassMiniIcon;
    [ObservableProperty] private string _elementSymbol = "";
    [ObservableProperty] private bool _hasElementSymbol;
    [ObservableProperty] private string _slotBackground = "SemiColorBackground1";
    [ObservableProperty] private double _slotOpacity = 1.0;
    [ObservableProperty] private string _superchargeColor = "#FFD700";
    [ObservableProperty] private string _superchargeBorderColor = "#FFD700";

    public JsonObject? SlotData { get; set; }
    public int SlotIndex { get; set; }
}

public partial class InventoryGridViewModel : ObservableObject
{
    private const int GridColumns = 10;
    private const double SlotVisualSize = 84;

    private JsonArray? _slots;
    private JsonObject? _currentInventory;
    private GameItemDatabase? _database;
    private IconManager? _iconManager;

    private string _inventoryGroup = "Default";
    public string InventoryGroup => _inventoryGroup;
    private bool _isTechInventory;
    private bool _isCargoInventory;
    private bool _isStorageInventory;
    private string _inventoryOwnerType = "";
    private bool _superchargeDisabled;
    private bool _slotToggleDisabled;
    private int _maxSuperchargedSlots = -1;
    private int _maxSuperchargeRow = -1;
    private string _exportFileName = "inventory.json";

    [ObservableProperty] private ObservableCollection<InventorySlotViewModel> _slotCells = new();
    [ObservableProperty] private InventorySlotViewModel? _selectedSlot;
    [ObservableProperty] private int _gridWidth = 10;
    [ObservableProperty] private int _gridHeight = 6;
    [ObservableProperty] private double _gridPixelWidth = GridColumns * SlotVisualSize;
    [ObservableProperty] private double _gridPixelHeight = 6 * SlotVisualSize;
    [ObservableProperty] private int _resizeWidth = 10;
    [ObservableProperty] private int _resizeHeight = 6;
    [ObservableProperty] private string _maxSupportedText = "";

    // Detail panel
    [ObservableProperty] private string _detailItemName = "";
    [ObservableProperty] private string _detailItemId = "";
    [ObservableProperty] private string _detailPosition = "";
    [ObservableProperty] private string _detailType = "";
    [ObservableProperty] private string _detailCategory = "";
    [ObservableProperty] private int _detailAmount;
    [ObservableProperty] private int _detailMaxAmount;
    [ObservableProperty] private decimal _detailDamageFactor;
    [ObservableProperty] private string _detailDescription = "";
    [ObservableProperty] private Bitmap? _detailIcon;
    [ObservableProperty] private bool _isSlotSelected;
    [ObservableProperty] private bool _isDetailPanelExpanded = true;
    [ObservableProperty] private double _detailPanelWidth = 300;
    [ObservableProperty] private string _detailToggleIcon = "M15,19L9,12L15,5";

    private List<GameItem> _allItems = new();
    private InventorySlotViewModel? _copiedSlot;

    public GameItemDatabase? Database => _database;
    public IconManager? IconMgr => _iconManager;

    /// <summary>
    /// Set by the View to provide item picker dialog functionality.
    /// Returns the selected item ID, or null if cancelled.
    /// </summary>
    public Func<Task<string?>>? PickItemFunc { get; set; }

    public event EventHandler? DataModified;
    private void RaiseDataModified() => DataModified?.Invoke(this, EventArgs.Empty);

    public InventoryGridViewModel()
    {
        DetailItemName = UiStrings.Get("inventory.no_slot_selected");
    }

    public void SetDatabase(GameItemDatabase database) => _database = database;
    public void SetIconManager(IconManager? iconManager) => _iconManager = iconManager;
    public void SetInventoryGroup(string group) => _inventoryGroup = group;
    public void SetIsTechInventory(bool isTech) => _isTechInventory = isTech;
    public void SetIsCargoInventory(bool isCargo) => _isCargoInventory = isCargo;
    public void SetIsStorageInventory(bool isStorage) => _isStorageInventory = isStorage;
    public void SetSuperchargeDisabled(bool disabled) => _superchargeDisabled = disabled;
    public void SetSlotToggleDisabled(bool disabled) => _slotToggleDisabled = disabled;
    public void SetExportFileName(string fileName) => _exportFileName = fileName;
    public void SetInventoryOwnerType(string ownerType) => _inventoryOwnerType = ownerType;
    public void SetSuperchargeConstraints(int maxSlots, int maxRow)
    {
        _maxSuperchargedSlots = maxSlots;
        _maxSuperchargeRow = maxRow;
    }

    public void LoadInventory(JsonObject? inventory)
    {
        _currentInventory = inventory;
        SlotCells.Clear();
        SelectedSlot = null;
        IsSlotSelected = false;
        DetailItemName = UiStrings.Get("inventory.no_slot_selected");

        if (inventory == null) return;

        _slots = inventory.GetArray("Slots") ?? new JsonArray();

        var (validWidth, validHeight) = DetermineGridDimensions(inventory, _slots);
        GridWidth = validWidth;
        GridHeight = validHeight;
        GridPixelWidth = validWidth * SlotVisualSize;
        GridPixelHeight = validHeight * SlotVisualSize;
        ResizeWidth = validWidth;
        ResizeHeight = validHeight;

        var slotMap = BuildSlotMap(_slots, validWidth);
        var validSet = BuildValidSlotSet(inventory);
        var superchargedSet = BuildSuperchargedSlotSet(inventory);

        for (int row = 0; row < validHeight; row++)
        {
            for (int col = 0; col < validWidth; col++)
            {
                bool isEnabled = validSet.Count == 0 || validSet.Contains((col, row));
                bool isSupercharged = superchargedSet.Contains((col, row));

                if (slotMap.TryGetValue((col, row), out var slotEntry))
                {
                    var slotVm = CreateSlotViewModel(slotEntry.slot, slotEntry.index, col, row, isEnabled, isSupercharged);
                    SlotCells.Add(slotVm);
                    continue;
                }

                SlotCells.Add(CreateEmptySlotViewModel(col, row, isEnabled, isSupercharged));
            }
        }

        if (_isTechInventory)
            ComputeAdjacencyBorders();
    }

    private void ComputeAdjacencyBorders()
    {
        var slotGrid = new Dictionary<(int col, int row), InventorySlotViewModel>();
        foreach (var cell in SlotCells)
            slotGrid[(cell.GridCol, cell.GridRow)] = cell;

        foreach (var cell in SlotCells)
        {
            cell.HasAdjacencyBorder = false;
            cell.AdjBorderThickness = default;
            cell.AdjBorderColor = "Transparent";

            if (cell.IsEmpty || string.IsNullOrEmpty(cell.ItemId)) continue;

            var adjInfo = TechAdjacencyDatabase.GetAdjacencyInfo(cell.ItemId);
            if (adjInfo == null) continue;

            bool CheckNeighbor(int nc, int nr)
            {
                if (!slotGrid.TryGetValue((nc, nr), out var neighbor)) return false;
                if (neighbor.IsEmpty || string.IsNullOrEmpty(neighbor.ItemId)) return false;
                var nAdj = TechAdjacencyDatabase.GetAdjacencyInfo(neighbor.ItemId);
                return nAdj != null && nAdj.BaseStatType == adjInfo.BaseStatType;
            }

            double top = CheckNeighbor(cell.GridCol, cell.GridRow - 1) ? 2 : 0;
            double bottom = CheckNeighbor(cell.GridCol, cell.GridRow + 1) ? 2 : 0;
            double left = CheckNeighbor(cell.GridCol - 1, cell.GridRow) ? 2 : 0;
            double right = CheckNeighbor(cell.GridCol + 1, cell.GridRow) ? 2 : 0;

            if (top > 0 || bottom > 0 || left > 0 || right > 0)
            {
                cell.AdjBorderThickness = new Avalonia.Thickness(left, top, right, bottom);
                cell.AdjBorderColor = adjInfo.LinkColourHex;
                cell.HasAdjacencyBorder = true;
            }
        }
    }

    private static (int width, int height) DetermineGridDimensions(JsonObject inventory, JsonArray slots)
    {
        int width = 0;
        int height = 0;

        try { width = inventory.GetInt("Width"); } catch { }
        if (width <= 0) try { width = inventory.GetInt("ValidSlotWidth"); } catch { }

        try { height = inventory.GetInt("Height"); } catch { }
        if (height <= 0) try { height = inventory.GetInt("ValidSlotHeight"); } catch { }

        if (width > 0 && height > 0)
            return (width, height);

        int maxX = 0;
        int maxY = 0;

        var validSlots = inventory.GetArray("ValidSlotIndices");
        if (validSlots != null)
        {
            for (int i = 0; i < validSlots.Length; i++)
            {
                var idx = validSlots.GetObject(i);
                if (idx == null) continue;
                try { maxX = Math.Max(maxX, idx.GetInt("X")); } catch { }
                try { maxY = Math.Max(maxY, idx.GetInt("Y")); } catch { }
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots.GetObject(i);
            var idx = slot?.GetObject("Index");
            if (idx == null) continue;
            try { maxX = Math.Max(maxX, idx.GetInt("X")); } catch { }
            try { maxY = Math.Max(maxY, idx.GetInt("Y")); } catch { }
        }

        if (maxX > 0 || maxY > 0)
            return (maxX + 1, maxY + 1);

        return (GridColumns, Math.Max(1, (slots.Length + GridColumns - 1) / GridColumns));
    }

    private static Dictionary<(int col, int row), (int index, JsonObject slot)> BuildSlotMap(JsonArray slots, int width)
    {
        var slotMap = new Dictionary<(int col, int row), (int index, JsonObject slot)>();

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots.GetObject(i);
            if (slot == null) continue;

            int col = i % width;
            int row = i / width;
            var indexObj = slot.GetObject("Index");
            if (indexObj != null)
            {
                try { col = indexObj.GetInt("X"); } catch { }
                try { row = indexObj.GetInt("Y"); } catch { }
            }

            slotMap[(col, row)] = (i, slot);
        }

        return slotMap;
    }

    private static HashSet<(int col, int row)> BuildValidSlotSet(JsonObject inventory)
    {
        var validSet = new HashSet<(int col, int row)>();
        var validSlots = inventory.GetArray("ValidSlotIndices");
        if (validSlots == null) return validSet;

        for (int i = 0; i < validSlots.Length; i++)
        {
            var idx = validSlots.GetObject(i);
            if (idx == null) continue;

            try
            {
                validSet.Add((idx.GetInt("X"), idx.GetInt("Y")));
            }
            catch
            {
            }
        }

        return validSet;
    }

    private static HashSet<(int col, int row)> BuildSuperchargedSlotSet(JsonObject inventory)
    {
        var superchargedSet = new HashSet<(int col, int row)>();
        var specialSlots = inventory.GetArray("SpecialSlots");
        if (specialSlots == null) return superchargedSet;

        for (int i = 0; i < specialSlots.Length; i++)
        {
            var entry = specialSlots.GetObject(i);
            var indexObj = entry?.GetObject("Index");
            if (indexObj == null) continue;

            try
            {
                superchargedSet.Add((indexObj.GetInt("X"), indexObj.GetInt("Y")));
            }
            catch
            {
            }
        }

        return superchargedSet;
    }

    private InventorySlotViewModel CreateSlotViewModel(JsonObject slotData, int index, int col, int row, bool isEnabled, bool isSupercharged)
    {
        var typeObj = slotData.GetObject("Type");
        string typeId = (string?)typeObj?.Get("InventoryType") ?? "";
        string itemId = ExtractItemId(slotData);

        int amount = 0;
        int maxAmount = 1;
        double damage = 0;
        try { amount = slotData.GetInt("Amount"); } catch { }
        try { maxAmount = slotData.GetInt("MaxAmount"); } catch { }
        try { damage = slotData.GetDouble("DamageFactor"); } catch { }

        bool isEmpty = string.IsNullOrEmpty(itemId) || itemId == "^" || itemId == "^YOURSLOTITEM";
        string itemName = "";
        string itemType = typeId;
        string category = "";
        string description = "";
        Bitmap? icon = null;
        GameItem? resolvedItem = null;

        if (!isEmpty)
        {
            var (gi, displayName, techPackIcon) = ResolveItemAndDisplayName(itemId);
            resolvedItem = gi;
            if (gi != null)
            {
                itemName = displayName;
                itemType = gi.ItemType;
                category = gi.Category ?? "";
                description = gi.Description ?? "";
                icon = _iconManager?.GetIcon(techPackIcon ?? gi.Icon);
            }
            else
            {
                itemName = DisplayNameFromId(itemId);
            }
        }

        bool slotIsSupercharged = isSupercharged || slotData.GetBool("SuperCharged");
        bool isDamaged = damage > 0;

        bool isChargeable = resolvedItem?.IsChargeable ?? false;
        bool isTechChargeable = _isTechInventory && isChargeable && amount >= 0 && maxAmount > 0;

        string displayAmount = "";
        string chargeDisplay = "";
        bool showCharge = false;

        if (!isEmpty)
        {
            if (isTechChargeable)
            {
                int pct = maxAmount > 0 ? (int)Math.Ceiling((double)amount / maxAmount * 100) : 0;
                displayAmount = $"{pct}%";
                showCharge = true;
                chargeDisplay = displayAmount;
            }
            else if (_isTechInventory && !isChargeable)
            {
                // Non-chargeable installed tech: no amount display
            }
            else if (amount > 0 || maxAmount > 0)
            {
                displayAmount = maxAmount > 0 ? $"{amount:N0}/{maxAmount:N0}" : $"{amount:N0}";
            }
        }

        // Element symbol for raw materials / substances
        string elementSymbol = "";
        bool hasElementSymbol = false;
        if (!isEmpty && resolvedItem != null)
        {
            var elemSym = ElementDatabase.GetSymbol(resolvedItem.Id);
            if (!string.IsNullOrEmpty(elemSym))
            {
                elementSymbol = elemSym;
                hasElementSymbol = true;
            }
        }

        // Class mini icon badge - derived from Quality or Rarity
        Bitmap? classMiniIcon = null;
        bool hasClassMiniIcon = false;
        if (!isEmpty && resolvedItem != null && _iconManager != null)
        {
            string? itemClass = resolvedItem.QualityToClass();
            if (string.IsNullOrEmpty(itemClass))
                itemClass = resolvedItem.RarityToClass();

            if (!string.IsNullOrEmpty(itemClass) && itemClass != "NONE"
                && ShouldShowClassMiniIcon(resolvedItem.ItemType, itemId))
            {
                if (itemClass == "?") itemClass = "Sentinel";
                classMiniIcon = _iconManager.GetIcon($"CLASSMINI.{itemClass}.png");
                hasClassMiniIcon = classMiniIcon != null;
            }
        }

        // Slot background color based on state
        string slotBg = "SemiColorBackground1";
        double slotOpacity = 1.0;
        if (!isEnabled)
        {
            slotOpacity = 0.4;
        }
        else if (isEmpty)
        {
            slotOpacity = 0.5;
        }

        // Supercharge colors
        string superchargeColor = "#FFD700"; // gold
        string superchargeBorderColor = "#FFD700";

        return new InventorySlotViewModel
        {
            ItemId = itemId,
            ItemName = itemName,
            Amount = amount,
            MaxAmount = maxAmount,
            DamageFactor = (decimal)damage,
            IsEnabled = isEnabled,
            IsSupercharged = slotIsSupercharged,
            IsDamaged = isDamaged,
            IsEmpty = isEmpty,
            Icon = icon,
            DisplayAmount = displayAmount,
            ChargeDisplay = chargeDisplay,
            ShowCharge = showCharge,
            GridRow = row,
            GridCol = col,
            ItemType = itemType,
            ItemCategory = category,
            Description = description,
            Position = $"({col}, {row})",
            SlotData = slotData,
            SlotIndex = index,
            ElementSymbol = elementSymbol,
            HasElementSymbol = hasElementSymbol,
            ClassMiniIcon = classMiniIcon,
            HasClassMiniIcon = hasClassMiniIcon,
            SlotBackground = slotBg,
            SlotOpacity = slotOpacity,
            SuperchargeColor = superchargeColor,
            SuperchargeBorderColor = superchargeBorderColor
        };
    }

    private static string ExtractItemId(JsonObject slotData)
    {
        try
        {
            var idObj = slotData.GetObject("Id");
            if (idObj != null)
                return ExtractItemId(idObj.Get("Id"));

            return ExtractItemId(slotData.Get("Id"));
        }
        catch
        {
            return "";
        }
    }

    private static string ExtractItemId(object? rawId)
    {
        if (rawId is BinaryData binaryData)
            return BinaryDataToItemId(binaryData);

        return rawId as string ?? "";
    }

    private static string BinaryDataToItemId(BinaryData data)
    {
        var bytes = data.ToByteArray();
        var builder = new StringBuilder();
        bool afterHash = false;

        for (int i = 0; i < bytes.Length; i++)
        {
            int value = bytes[i] & 0xFF;
            if (i == 0)
            {
                if (value != 0x5E)
                    return data.ToString();

                builder.Append('^');
            }
            else if (value == 0x23)
            {
                builder.Append('#');
                afterHash = true;
            }
            else if (afterHash)
            {
                builder.Append((char)value);
            }
            else
            {
                const string hexChars = "0123456789ABCDEF";
                builder.Append(hexChars[(value >> 4) & 0xF]);
                builder.Append(hexChars[value & 0xF]);
            }
        }

        return builder.ToString();
    }

    private static string DisplayNameFromId(string itemId)
    {
        var match = Regex.Match(itemId, @"#\d{5,}$");
        if (!match.Success)
            return itemId;

        string baseId = itemId[..match.Index];
        return $"{baseId} [{match.Value}]";
    }

    private static bool IsTechPackHash(string itemId) =>
        itemId.Length == 13 && itemId[0] == '^' && Regex.IsMatch(itemId, @"^\^[0-9A-Fa-f]{12}$");

    private (GameItem? gameItem, string? techPackIcon) ResolveGameItem(string itemId)
    {
        if (_database == null)
            return (null, null);

        var match = Regex.Match(itemId, @"#\d{5,}$");
        string baseId = match.Success ? itemId[..match.Index] : itemId;

        GameItem? gameItem = _database.GetItem(itemId) ?? _database.GetItem(baseId);
        if (gameItem != null)
            return (gameItem, null);

        if (IsTechPackHash(baseId) && TechPacks.Dictionary.TryGetValue(baseId, out var techPack))
        {
            gameItem = _database.GetItem(techPack.Id);
            if (gameItem != null)
                return (gameItem, techPack.Icon);
        }

        return (null, null);
    }

    private (GameItem? gameItem, string displayName, string? techPackIcon) ResolveItemAndDisplayName(string itemId)
    {
        if (_database == null)
            return (null, DisplayNameFromId(itemId), null);

        var match = Regex.Match(itemId, @"#\d{5,}$");
        string variantSuffix = match.Success ? match.Value : "";
        var (gameItem, techPackIcon) = ResolveGameItem(itemId);

        if (gameItem == null)
            return (null, DisplayNameFromId(itemId), techPackIcon);

        string displayName = _isTechInventory && !string.IsNullOrEmpty(gameItem.NameLower)
            ? gameItem.NameLower
            : gameItem.Name;

        if (!string.IsNullOrEmpty(variantSuffix))
            displayName = $"{displayName} [{variantSuffix}]";

        return (gameItem, displayName, techPackIcon);
    }

    private static InventorySlotViewModel CreateEmptySlotViewModel(int col, int row, bool isEnabled, bool isSupercharged)
    {
        return new InventorySlotViewModel
        {
            ItemId = "",
            ItemName = "",
            Amount = 0,
            MaxAmount = 0,
            DamageFactor = 0,
            IsEnabled = isEnabled,
            IsSupercharged = isSupercharged,
            IsDamaged = false,
            IsEmpty = true,
            Icon = null,
            DisplayAmount = "",
            ChargeDisplay = "",
            ShowCharge = false,
            GridRow = row,
            GridCol = col,
            ItemType = "",
            ItemCategory = "",
            Description = "",
            Position = $"({col}, {row})",
            SlotData = null,
            SlotIndex = -1
        };
    }

    [RelayCommand]
    private void SelectSlot(InventorySlotViewModel? slot)
    {
        if (SelectedSlot != null)
            SelectedSlot.IsSelected = false;

        SelectedSlot = slot;
        IsSlotSelected = slot != null;

        if (slot != null)
        {
            slot.IsSelected = true;
            DetailItemName = string.IsNullOrEmpty(slot.ItemName)
                ? UiStrings.Get("inventory.empty_slot")
                : slot.ItemName;
            DetailItemId = slot.ItemId;
            DetailPosition = slot.Position;
            DetailType = slot.ItemType;
            DetailCategory = slot.ItemCategory;
            DetailAmount = slot.Amount;
            DetailMaxAmount = slot.MaxAmount;
            DetailDamageFactor = slot.DamageFactor;
            DetailDescription = slot.Description;
            DetailIcon = slot.Icon;
        }
        else
        {
            DetailItemName = UiStrings.Get("inventory.no_slot_selected");
        }
    }

    private void RefreshSlotAtSelected(JsonObject slotData, int slotIndex)
    {
        if (SelectedSlot == null) return;

        bool isEnabled = SlotCells.Any(cell => cell.GridCol == SelectedSlot.GridCol &&
                                               cell.GridRow == SelectedSlot.GridRow &&
                                               cell.IsEnabled);
        bool isSupercharged = SlotCells.Any(cell => cell.GridCol == SelectedSlot.GridCol &&
                                                    cell.GridRow == SelectedSlot.GridRow &&
                                                    cell.IsSupercharged);
        var refreshed = CreateSlotViewModel(slotData, slotIndex, SelectedSlot.GridCol, SelectedSlot.GridRow, isEnabled, isSupercharged);
        int idx = SlotCells.IndexOf(SelectedSlot!);
        if (idx >= 0)
        {
            SlotCells[idx] = refreshed;
            SelectSlot(refreshed);
        }
    }

    private static string EnsureCaretPrefix(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return "^";

        return itemId.StartsWith("^", StringComparison.Ordinal) ? itemId : "^" + itemId;
    }

    private static void SetSlotItemId(JsonObject slotData, string itemId)
    {
        string normalized = string.IsNullOrWhiteSpace(itemId) ? "" : EnsureCaretPrefix(itemId);
        var idObj = slotData.GetObject("Id");
        if (idObj != null)
        {
            idObj.Set("Id", normalized);
            return;
        }

        slotData.Set("Id", normalized);
    }

    [RelayCommand]
    private void ApplyChanges()
    {
        if (SelectedSlot?.SlotData == null) return;

        var slotData = SelectedSlot.SlotData;
        SetSlotItemId(slotData, DetailItemId);

        slotData.Set("Amount", DetailAmount);
        slotData.Set("MaxAmount", DetailMaxAmount);
        slotData.Set("DamageFactor", (double)DetailDamageFactor);

        RefreshSlotAtSelected(slotData, SelectedSlot.SlotIndex);
        RaiseDataModified();
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (SelectedSlot?.SlotData == null) return;

        var slotData = SelectedSlot.SlotData;
        SetSlotItemId(slotData, "");
        slotData.Set("Amount", 0);
        slotData.Set("MaxAmount", 0);
        slotData.Set("DamageFactor", 0.0);

        RefreshSlotAtSelected(slotData, SelectedSlot.SlotIndex);
        RaiseDataModified();
    }

    [RelayCommand]
    private void FillStack()
    {
        if (SelectedSlot?.SlotData == null || SelectedSlot.IsEmpty) return;

        SelectedSlot.SlotData.Set("Amount", SelectedSlot.MaxAmount);
        RefreshSlotAtSelected(SelectedSlot.SlotData, SelectedSlot.SlotIndex);
        RaiseDataModified();
    }

    [RelayCommand]
    private void SuperchargeSlot()
    {
        if (SelectedSlot?.SlotData == null) return;

        bool current = SelectedSlot.SlotData.GetBool("SuperCharged");
        SelectedSlot.SlotData.Set("SuperCharged", !current);
        RefreshSlotAtSelected(SelectedSlot.SlotData, SelectedSlot.SlotIndex);
        RaiseDataModified();
    }

    [RelayCommand]
    private void RepairSlot()
    {
        if (SelectedSlot?.SlotData == null) return;

        SelectedSlot.SlotData.Set("DamageFactor", 0.0);
        RefreshSlotAtSelected(SelectedSlot.SlotData, SelectedSlot.SlotIndex);
        RaiseDataModified();
    }

    [RelayCommand]
    private void RepairAllSlots()
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
            _slots.GetObject(i)?.Set("DamageFactor", 0.0);
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void SuperchargeAllSlots()
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            var slotData = _slots.GetObject(i);
            if (slotData != null)
            {
                string itemId = ExtractItemId(slotData);
                if (!string.IsNullOrEmpty(itemId))
                    slotData.Set("SuperCharged", true);
            }
        }
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void RechargeAllTech()
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            var slotData = _slots.GetObject(i);
            if (slotData == null) continue;
            int max = slotData.GetInt("MaxAmount");
            if (max > 0) slotData.Set("Amount", max);
        }
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void RefillAllStacks()
    {
        if (_slots == null) return;
        for (int i = 0; i < _slots.Length; i++)
        {
            var slotData = _slots.GetObject(i);
            if (slotData == null) continue;
            int max = slotData.GetInt("MaxAmount");
            if (max > 0) slotData.Set("Amount", max);
        }
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void CopyItem()
    {
        if (SelectedSlot != null && !SelectedSlot.IsEmpty)
            _copiedSlot = SelectedSlot;
    }

    [RelayCommand]
    private void PasteItem()
    {
        if (_copiedSlot?.SlotData == null || SelectedSlot?.SlotData == null) return;

        var source = _copiedSlot.SlotData;
        var target = SelectedSlot.SlotData;

        SetSlotItemId(target, ExtractItemId(source));
        target.Set("Amount", source.GetInt("Amount"));
        target.Set("MaxAmount", source.GetInt("MaxAmount"));
        target.Set("DamageFactor", source.GetDouble("DamageFactor"));

        RefreshSlotAtSelected(target, SelectedSlot.SlotIndex);
        RaiseDataModified();
    }

    [RelayCommand]
    private void ToggleDetailPanel()
    {
        IsDetailPanelExpanded = !IsDetailPanelExpanded;
        DetailPanelWidth = IsDetailPanelExpanded ? 300 : 36;
        DetailToggleIcon = IsDetailPanelExpanded
            ? "M15,19L9,12L15,5"
            : "M9,5L15,12L9,19";
    }

    [RelayCommand]
    private async Task ChangeItem()
    {
        if (SelectedSlot?.SlotData == null || PickItemFunc == null) return;

        var newItemId = await PickItemFunc();
        if (string.IsNullOrEmpty(newItemId)) return;

        DetailItemId = newItemId;
        ApplyChanges();
    }

    [RelayCommand]
    private void ResizeInventory()
    {
        if (_currentInventory == null) return;
        _currentInventory.Set("Width", ResizeWidth);
        _currentInventory.Set("Height", ResizeHeight);
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    public void SwapSlots(InventorySlotViewModel source, InventorySlotViewModel target)
    {
        if (source.SlotData == null || target.SlotData == null) return;
        InventorySlotHelper.SwapSlotIndices(source.SlotData, source.GridCol, source.GridRow,
            target.SlotData, target.GridCol, target.GridRow);
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    // --- Context menu commands (operate on a specific slot parameter) ---

    [RelayCommand]
    private async Task ContextAddItem(InventorySlotViewModel? slot)
    {
        if (slot == null || PickItemFunc == null) return;
        SelectSlot(slot);
        var newItemId = await PickItemFunc();
        if (string.IsNullOrEmpty(newItemId)) return;
        DetailItemId = newItemId;
        ApplyChanges();
    }

    [RelayCommand]
    private void RemoveItemAt(InventorySlotViewModel? slot)
    {
        if (slot?.SlotData == null) return;
        SelectSlot(slot);
        RemoveItem();
    }

    [RelayCommand]
    private void ToggleSlotEnabled(InventorySlotViewModel? slot)
    {
        if (slot == null || _currentInventory == null) return;

        var validSlots = _currentInventory.GetArray("ValidSlotIndices");
        if (validSlots == null)
        {
            validSlots = new JsonArray();
            _currentInventory.Set("ValidSlotIndices", validSlots);
        }

        int x = slot.GridCol, y = slot.GridRow;

        if (slot.IsEnabled)
        {
            // Disable: only if slot has no item
            if (slot.SlotData != null && !string.IsNullOrEmpty(slot.ItemId)
                && slot.ItemId != "^" && slot.ItemId != "^YOURSLOTITEM")
                return;

            for (int i = 0; i < validSlots.Length; i++)
            {
                var idx = validSlots.GetObject(i);
                if (idx != null && idx.GetInt("X") == x && idx.GetInt("Y") == y)
                {
                    validSlots.RemoveAt(i);
                    break;
                }
            }
        }
        else
        {
            // Enable: add to ValidSlotIndices
            var newIdx = new JsonObject();
            newIdx.Add("X", x);
            newIdx.Add("Y", y);
            validSlots.Add(newIdx);
        }

        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void EnableAllSlots()
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

        for (int row = 0; row < GridHeight; row++)
        {
            for (int col = 0; col < GridWidth; col++)
            {
                if (!existing.Contains((col, row)))
                {
                    var newIdx = new JsonObject();
                    newIdx.Add("X", col);
                    newIdx.Add("Y", row);
                    validSlots.Add(newIdx);
                }
            }
        }

        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void RepairSlotAt(InventorySlotViewModel? slot)
    {
        if (slot?.SlotData == null) return;
        slot.SlotData.Set("DamageFactor", 0.0);
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void SuperchargeSlotAt(InventorySlotViewModel? slot)
    {
        if (slot?.SlotData == null) return;
        bool current = slot.SlotData.GetBool("SuperCharged");
        slot.SlotData.Set("SuperCharged", !current);
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void FillStackAt(InventorySlotViewModel? slot)
    {
        if (slot?.SlotData == null || slot.IsEmpty) return;
        slot.SlotData.Set("Amount", slot.MaxAmount);
        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    [RelayCommand]
    private void CopyItemAt(InventorySlotViewModel? slot)
    {
        if (slot != null && !slot.IsEmpty)
            _copiedSlot = slot;
    }

    [RelayCommand]
    private void PasteItemAt(InventorySlotViewModel? slot)
    {
        if (_copiedSlot?.SlotData == null || slot?.SlotData == null) return;

        var source = _copiedSlot.SlotData;
        var target = slot.SlotData;

        SetSlotItemId(target, ExtractItemId(source));
        target.Set("Amount", source.GetInt("Amount"));
        target.Set("MaxAmount", source.GetInt("MaxAmount"));
        target.Set("DamageFactor", source.GetDouble("DamageFactor"));

        LoadInventory(_currentInventory);
        RaiseDataModified();
    }

    // Export / Import inventory commands (placeholder - to be wired to file dialogs by view)
    public Func<Task>? ExportInventoryFunc { get; set; }
    public Func<Task>? ImportInventoryFunc { get; set; }

    [RelayCommand]
    private async Task ExportInventory()
    {
        if (ExportInventoryFunc != null)
            await ExportInventoryFunc();
    }

    [RelayCommand]
    private async Task ImportInventory()
    {
        if (ImportInventoryFunc != null)
            await ImportInventoryFunc();
    }

    /// <summary>
    /// Properties to control context menu visibility.
    /// </summary>
    public bool IsSlotToggleDisabled => _slotToggleDisabled;
    public bool IsSuperchargeDisabled => _superchargeDisabled;
    public bool IsTechInventory => _isTechInventory;
    public bool HasCopiedSlot => _copiedSlot != null;

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
            string baseId = itemId.StartsWith("^") ? itemId[1..] : itemId;
            var hashIdx = baseId.IndexOf('#');
            if (hashIdx >= 0) baseId = baseId[..hashIdx];
            return !baseId.EndsWith("INV_TOKEN", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
