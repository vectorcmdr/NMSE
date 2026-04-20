using NMSE.Data;
using NMSE.Models;
using NMSE.Core;
using NMSE.Config;

namespace NMSE.UI.Panels;

public partial class ExosuitPanel : UserControl
{
    private JsonObject? _playerState;
    private string _saveScopeKey = "unknown";

    /// <summary>Raised when inventory data is modified by the user.</summary>
    public event EventHandler? DataModified;

    /// <summary>
    /// Raised after auto-stack moves cargo into another inventory so destination
    /// panels can refresh their grids immediately.
    /// </summary>
    public event EventHandler? CrossInventoryTransferCompleted;

    public ExosuitPanel()
    {
        InitializeComponent();
        SetupLayout();
    }

    public void SetDatabase(GameItemDatabase? database)
    {
        _generalGrid.SetDatabase(database);
        _techGrid.SetDatabase(database);
    }

    public void SetIconManager(IconManager? iconManager)
    {
        _generalGrid.SetIconManager(iconManager);
        _techGrid.SetIconManager(iconManager);
    }

    public void SetSaveScopeKey(string saveScopeKey)
    {
        _saveScopeKey = string.IsNullOrWhiteSpace(saveScopeKey) ? "unknown" : saveScopeKey;
        ApplyPinnedSlots();
    }

    public void LoadData(JsonObject saveData)
    {
        try
        {
            _playerState = saveData.GetObject("PlayerStateData");
            if (_playerState == null) return;

            _generalGrid.LoadInventory(_playerState.GetObject(ExosuitLogic.CargoInventoryKey));
            _techGrid.LoadInventory(_playerState.GetObject(ExosuitLogic.TechInventoryKey));
            ApplyPinnedSlots();
        }
        catch { /* Save structure varies between versions */ }
    }

    public void SaveData(JsonObject saveData)
    {
        try
        {
            var playerState = saveData.GetObject("PlayerStateData");
            if (playerState == null) return;

            _generalGrid.SaveInventory(playerState.GetObject(ExosuitLogic.CargoInventoryKey));
            _techGrid.SaveInventory(playerState.GetObject(ExosuitLogic.TechInventoryKey));
        }
        catch { }
    }

    private void OnAutoStackToStorageRequested(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var cargoInventory = _generalGrid.GetLoadedInventory() ?? _playerState.GetObject(ExosuitLogic.CargoInventoryKey);
        if (cargoInventory == null) return;

        var pinned = new HashSet<(int x, int y)>(_generalGrid.GetPinnedSlots());

        bool changed = InventoryBulkActions.AutoStackCargoToChests(cargoInventory, _playerState, out _, out _, pinned);
        if (!changed) return;

        _generalGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackToStarshipRequested(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var cargoInventory = _generalGrid.GetLoadedInventory() ?? _playerState.GetObject(ExosuitLogic.CargoInventoryKey);
        if (cargoInventory == null) return;

        var pinned = new HashSet<(int x, int y)>(_generalGrid.GetPinnedSlots());

        bool changed = InventoryBulkActions.AutoStackCargoToStarship(cargoInventory, _playerState, out _, out _, pinned);
        if (!changed) return;

        _generalGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackToFreighterRequested(object? sender, EventArgs e)
    {
        if (_playerState == null) return;

        var cargoInventory = _generalGrid.GetLoadedInventory() ?? _playerState.GetObject(ExosuitLogic.CargoInventoryKey);
        if (cargoInventory == null) return;

        var pinned = new HashSet<(int x, int y)>(_generalGrid.GetPinnedSlots());

        bool changed = InventoryBulkActions.AutoStackCargoToFreighter(cargoInventory, _playerState, out _, out _, pinned);
        if (!changed) return;

        _generalGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackSelectedSlotToStorageRequested(object? sender, InventoryGridPanel.AutoStackSlotRequestEventArgs e)
    {
        if (!TryGetContextAutoStackCargo(out var cargoInventory, out var pinned, e, out var sourceSlotFilter, out var sourceItemIdFilter))
            return;

        bool changed = InventoryBulkActions.AutoStackCargoToChests(
            cargoInventory,
            _playerState!,
            out _,
            out _,
            pinned,
            sourceSlotFilter,
            sourceItemIdFilter);

        if (!changed) return;

        _generalGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackSelectedSlotToStarshipRequested(object? sender, InventoryGridPanel.AutoStackSlotRequestEventArgs e)
    {
        if (!TryGetContextAutoStackCargo(out var cargoInventory, out var pinned, e, out var sourceSlotFilter, out var sourceItemIdFilter))
            return;

        bool changed = InventoryBulkActions.AutoStackCargoToStarship(
            cargoInventory,
            _playerState!,
            out _,
            out _,
            pinned,
            sourceSlotFilter,
            sourceItemIdFilter);

        if (!changed) return;

        _generalGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnAutoStackSelectedSlotToFreighterRequested(object? sender, InventoryGridPanel.AutoStackSlotRequestEventArgs e)
    {
        if (!TryGetContextAutoStackCargo(out var cargoInventory, out var pinned, e, out var sourceSlotFilter, out var sourceItemIdFilter))
            return;

        bool changed = InventoryBulkActions.AutoStackCargoToFreighter(
            cargoInventory,
            _playerState!,
            out _,
            out _,
            pinned,
            sourceSlotFilter,
            sourceItemIdFilter);

        if (!changed) return;

        _generalGrid.LoadInventory(cargoInventory);
        DataModified?.Invoke(this, EventArgs.Empty);
        CrossInventoryTransferCompleted?.Invoke(this, EventArgs.Empty);
    }

    private bool TryGetContextAutoStackCargo(
        out JsonObject cargoInventory,
        out HashSet<(int x, int y)> pinned,
        InventoryGridPanel.AutoStackSlotRequestEventArgs request,
        out (int x, int y) sourceSlotFilter,
        out string sourceItemIdFilter)
    {
        cargoInventory = null!;
        pinned = null!;
        sourceSlotFilter = default;
        sourceItemIdFilter = request.ItemId;

        if (_playerState == null)
            return false;

        cargoInventory = _generalGrid.GetLoadedInventory() ?? _playerState.GetObject(ExosuitLogic.CargoInventoryKey)!;
        if (cargoInventory == null)
            return false;

        pinned = new HashSet<(int x, int y)>(_generalGrid.GetPinnedSlots());
        sourceSlotFilter = (request.X, request.Y);

        if (pinned.Contains(sourceSlotFilter))
        {
            MessageBox.Show(this, 
                UiStrings.Get("inventory.auto_stack_pinned_slot_blocked"),
                UiStrings.Get("dialog.info"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    private void ApplyPinnedSlots()
    {
        var pinned = AppConfig.Instance.GetPinnedSlots(_saveScopeKey, "ExosuitCargo");
        _generalGrid.SetPinnedSlots(pinned);
    }

    private void OnPinnedSlotsChanged(object? sender, EventArgs e)
    {
        AppConfig.Instance.SetPinnedSlots(_saveScopeKey, "ExosuitCargo", _generalGrid.GetPinnedSlots());
    }

    public void ApplyUiLocalisation()
    {
        _titleLabel.Text = UiStrings.Get("exosuit.title");
        _generalPage.Text = UiStrings.Get("common.cargo");
        _techPage.Text = UiStrings.Get("common.technology");
        _generalGrid.SetMaxSupportedLabel(ExosuitLogic.CargoMaxLabel);
        _techGrid.SetMaxSupportedLabel(ExosuitLogic.TechMaxLabel);
        _generalGrid.ApplyUiLocalisation();
        _techGrid.ApplyUiLocalisation();
    }
}
