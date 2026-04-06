using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using NMSE.UI.ViewModels.Controls;
using NMSE.UI.Views.Dialogs;

namespace NMSE.UI.Views.Controls;

public partial class InventoryGridControl : UserControl
{
    private static readonly DataFormat<string> SlotFormat =
        DataFormat.CreateStringApplicationFormat("nmse-inventory-slot");

    private Point _dragStartPoint;
    private bool _isDragging;
    private InventorySlotViewModel? _dragSourceSlot;

    public InventoryGridControl()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, true);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave, RoutingStrategies.Bubble, true);

        AddHandler(PointerPressedEvent, OnSlotPointerPressed, RoutingStrategies.Tunnel, true);
        AddHandler(PointerMovedEvent, OnSlotPointerMoved, RoutingStrategies.Tunnel, true);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is InventoryGridViewModel vm)
        {
            vm.PickItemFunc = async () =>
            {
                if (vm.Database == null) return null;
                var picker = new ItemPickerDialog();
                picker.Initialize(vm.Database, vm.IconMgr);
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window window)
                    return await picker.ShowDialog<string?>(window);
                return null;
            };
        }
    }

    private void OnSlotPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;

        var slotButton = FindSlotButton(e.Source as Visual);
        if (slotButton?.Tag is InventorySlotViewModel slot && !slot.IsEmpty)
            _dragSourceSlot = slot;
        else
            _dragSourceSlot = null;
    }

    private async void OnSlotPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragSourceSlot == null || _isDragging) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var currentPos = e.GetPosition(this);
        var dx = currentPos.X - _dragStartPoint.X;
        var dy = currentPos.Y - _dragStartPoint.Y;

        if (Math.Sqrt(dx * dx + dy * dy) < 8) return;

        _isDragging = true;

        // Release any pointer capture held by the Button so DragDrop can take over
        if (e.Pointer.Captured != null)
            e.Pointer.Capture(null);

        var data = new DataTransfer();
        data.Add(DataTransferItem.Create(SlotFormat, _dragSourceSlot.SlotIndex.ToString()));

        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);

        ClearAllDragOver();
        _isDragging = false;
        _dragSourceSlot = null;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Formats.Contains(SlotFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;

        var targetSlot = FindSlotFromDragEvent(e);
        if (targetSlot == null) return;

        if (DataContext is InventoryGridViewModel vm)
        {
            foreach (var cell in vm.SlotCells)
                cell.IsDragOver = false;
            targetSlot.IsDragOver = true;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        ClearAllDragOver();
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        ClearAllDragOver();

        if (_dragSourceSlot == null) return;
        if (!e.DataTransfer.Formats.Contains(SlotFormat)) return;

        var targetSlot = FindSlotFromDragEvent(e);
        if (targetSlot == null || targetSlot == _dragSourceSlot) return;

        if (DataContext is InventoryGridViewModel vm)
            vm.SwapSlots(_dragSourceSlot, targetSlot);
    }

    private void ClearAllDragOver()
    {
        if (DataContext is InventoryGridViewModel vm)
        {
            foreach (var cell in vm.SlotCells)
                cell.IsDragOver = false;
        }
    }

    private InventorySlotViewModel? FindSlotFromDragEvent(DragEventArgs e)
    {
        var target = (e.Source as Visual)?.GetSelfAndVisualAncestors()
            .OfType<Button>()
            .FirstOrDefault(b => b.Tag is InventorySlotViewModel);

        return target?.Tag as InventorySlotViewModel;
    }

    private static Button? FindSlotButton(Visual? visual)
    {
        return visual?.GetSelfAndVisualAncestors()
            .OfType<Button>()
            .FirstOrDefault(b => b.Tag is InventorySlotViewModel);
    }

    private void OnSlotContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not ContextMenu contextMenu) return;
        if (DataContext is not InventoryGridViewModel vm) return;

        var slot = contextMenu.Tag as InventorySlotViewModel;
        if (slot == null)
        {
            e.Cancel = true;
            return;
        }

        // Select the slot when right-clicking
        vm.SelectSlotCommand.Execute(slot);

        bool hasItem = slot.SlotData != null && !string.IsNullOrEmpty(slot.ItemId) && slot.ItemId != "^" && slot.ItemId != "^YOURSLOTITEM";

        foreach (var item in contextMenu.Items)
        {
            if (item is not MenuItem menuItem) continue;

            var header = menuItem.Header?.ToString() ?? "";

            // Dynamically show/hide menu items based on slot state
            if (menuItem.Name == "EnableSlotMenuItem")
            {
                menuItem.IsVisible = !vm.IsSlotToggleDisabled;
                menuItem.Header = slot.IsEnabled
                    ? Data.UiStrings.Get("inventory.ctx_disable_slot")
                    : Data.UiStrings.Get("inventory.ctx_enable_slot");
            }
            else if (menuItem.Name == "SuperchargeMenuItem")
            {
                menuItem.IsVisible = !vm.IsSuperchargeDisabled;
                menuItem.Header = slot.IsSupercharged
                    ? Data.UiStrings.Get("inventory.ctx_remove_supercharge")
                    : Data.UiStrings.Get("inventory.ctx_supercharge");
            }
        }
    }
}
