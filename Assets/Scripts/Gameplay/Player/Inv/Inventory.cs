using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────
// Result type for equip operations
// ─────────────────────────────────────────────
public enum EquipResult
{
    Success,
    SlotOccupied,
    TagMismatch,
    ItemNotInInventory,
    SlotNotFound,
}

// ─────────────────────────────────────────────
// Inventory — owns the bag + equipment slots
// ─────────────────────────────────────────────
public class Inventory
{
    public InventorySlot MainHand1 { get; }
    public InventorySlot MainHand2 { get; }
    public InventorySlot OffHand   { get; }

    public IReadOnlyList<InventorySlot> EquipSlots { get; }

    private readonly List<InventoryItem> _bag = new();
    public  IReadOnlyList<InventoryItem> Bag  => _bag;

    public event Action? OnBagChanged;

    public Inventory()
    {
        MainHand1 = new InventorySlot("Main Hand 1", SlotType.MainHand);
        MainHand2 = new InventorySlot("Main Hand 2", SlotType.MainHand);
        OffHand   = new InventorySlot("Off Hand",    SlotType.OffHand, ItemTag.Pizza);

        EquipSlots = new List<InventorySlot> { OffHand, MainHand1, MainHand2 };

        foreach (var slot in EquipSlots)
            slot.OnChanged += (old, @new) => OnSlotChanged(slot, old, @new);

        Debug.Log("[Inventory] Created fresh — all slots empty.");
    }

    // ── Bag operations ────────────────────────

    public void AddToBag(InventoryItem item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _bag.Add(item);
        OnBagChanged?.Invoke();
    }

    public bool RemoveFromBag(InventoryItem item)
    {
        bool removed = _bag.Remove(item);
        if (removed) OnBagChanged?.Invoke();
        return removed;
    }

    public bool BagContains(InventoryItem item) => _bag.Contains(item);

    // ── Equip / Unequip ───────────────────────

    public EquipResult Equip(InventoryItem item, InventorySlot slot)
    {
        if (!BagContains(item))
            return EquipResult.ItemNotInInventory;

        if (!slot.IsEmpty)
            return EquipResult.SlotOccupied;

        if (!slot.CanAccept(item))
            return EquipResult.TagMismatch;

        RemoveFromBag(item);
        slot.TrySetItem(item);
        return EquipResult.Success;
    }

    public bool Unequip(InventorySlot slot)
    {
        if (slot.IsEmpty) return false;
        var item = slot.Clear()!;
        AddToBag(item);
        return true;
    }

    public EquipResult SwapWithBag(InventoryItem bagItem, InventorySlot slot)
    {
        if (!BagContains(bagItem))
            return EquipResult.ItemNotInInventory;

        if (!slot.CanAccept(bagItem))
            return EquipResult.TagMismatch;

        var equipped = slot.Clear();
        if (equipped is not null)
            AddToBag(equipped);

        RemoveFromBag(bagItem);
        slot.TrySetItem(bagItem);
        return EquipResult.Success;
    }

    public EquipResult SwapSlots(InventorySlot slotA, InventorySlot slotB)
    {
        if (!slotA.CanAccept(slotB.Item) || !slotB.CanAccept(slotA.Item))
            return EquipResult.TagMismatch;

        var itemA = slotA.Item;
        var itemB = slotB.Item;

        slotA.TrySetItem(null);
        slotB.TrySetItem(null);
        slotA.TrySetItem(itemB);
        slotB.TrySetItem(itemA);
        return EquipResult.Success;
    }

    // ── Auto-equip ────────────────────────────

    public EquipResult AutoEquip(InventoryItem item)
    {
        if (!BagContains(item))
            AddToBag(item);

        Debug.Log($"[AutoEquip] Item: {item.DisplayName} Tag: {item.Tags}");
        foreach (var s in EquipSlots)
            Debug.Log($"[AutoEquip] Slot: {s.Name} Empty: {s.IsEmpty} RequiredTag: {s.RequiredTag} CanAccept: {s.CanAccept(item)}");

        // First pass — slots with a specific tag requirement
        foreach (var slot in EquipSlots)
        {
            if (slot.IsEmpty && slot.RequiredTag != ItemTag.None && slot.CanAccept(item))
                return Equip(item, slot);
        }

        // Second pass — unrestricted slots, block items that belong in a restricted slot
        foreach (var slot in EquipSlots)
        {
            if (slot.IsEmpty && slot.RequiredTag == ItemTag.None && slot.CanAccept(item))
            {
                bool hasRestrictedSlot = false;
                foreach (var other in EquipSlots)
                {
                    if (other.RequiredTag != ItemTag.None && item.HasTag(other.RequiredTag))
                    {
                        hasRestrictedSlot = true;
                        break;
                    }
                }
                if (hasRestrictedSlot) continue;
                return Equip(item, slot);
            }
        }

        return EquipResult.SlotOccupied;
    }

    // ── Internal ──────────────────────────────

    private void OnSlotChanged(InventorySlot slot, InventoryItem? old, InventoryItem? @new)
    {
        Debug.Log(
            old is null
                ? $"[Inventory] {slot.Name}: equipped {@new?.DisplayName}"
                : @new is null
                    ? $"[Inventory] {slot.Name}: unequipped {old.DisplayName}"
                    : $"[Inventory] {slot.Name}: swapped {old.DisplayName} for {@new.DisplayName}"
        );
    }

    public void PrintState()
    {
        Debug.Log("=== Equipment Slots ===");
        foreach (var slot in EquipSlots)
            Debug.Log($"  {slot}");

        Debug.Log($"=== Bag ({_bag.Count} items) ===");
        foreach (var item in _bag)
            Debug.Log($"  {item}");
    }
}