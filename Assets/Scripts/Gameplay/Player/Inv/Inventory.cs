using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────
// Result type for equip operations
// ─────────────────────────────────────────────
public enum EquipResult
{
    Success,
    SlotOccupied,       // slot already has an item; call Swap instead
    TagMismatch,        // item doesn't meet the slot's required tag
    ItemNotInInventory, // item isn't in the player's bag
    SlotNotFound,
}

// ─────────────────────────────────────────────
// Inventory — owns the bag + equipment slots
// ─────────────────────────────────────────────
public class Inventory
{
    // ── Equipment slots ───────────────────────

    // Two independent main-hand slots (accept any item)
    public InventorySlot MainHand1 { get; }
    public InventorySlot MainHand2 { get; }

    // One off-hand slot — only accepts items tagged as Shield
    // Change ItemTag.Shield to whatever tag your game uses
    public InventorySlot OffHand   { get; }

    // Convenience list for iteration
    public IReadOnlyList<InventorySlot> EquipSlots { get; }

    // ── Bag (unequipped items) ─────────────────
    private readonly List<InventoryItem> _bag = new();
    public  IReadOnlyList<InventoryItem> Bag  => _bag;

    // Fired when the bag changes
    public event Action? OnBagChanged;

    // ─────────────────────────────────────────
    public Inventory()
    {
        MainHand1 = new InventorySlot("Main Hand 1", SlotType.MainHand);
        MainHand2 = new InventorySlot("Main Hand 2", SlotType.MainHand);
        OffHand   = new InventorySlot("Off Hand",    SlotType.OffHand, ItemTag.Pizza);

        EquipSlots = new List<InventorySlot> { MainHand1, MainHand2, OffHand };

        // Optional: wire slot events to a central handler
        foreach (var slot in EquipSlots)
            slot.OnChanged += (old, @new) => OnSlotChanged(slot, old, @new);
    }

    // ── Bag operations ────────────────────────

    /// <summary>Add an item to the bag (e.g. picked up from the world).</summary>
    public void AddToBag(InventoryItem item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        _bag.Add(item);
        OnBagChanged?.Invoke();
    }

    /// <summary>Remove an item from the bag. Returns false if not found.</summary>
    public bool RemoveFromBag(InventoryItem item)
    {
        bool removed = _bag.Remove(item);
        if (removed) OnBagChanged?.Invoke();
        return removed;
    }

    public bool BagContains(InventoryItem item) => _bag.Contains(item);

    // ── Equip / Unequip ───────────────────────

    /// <summary>
    /// Equip an item from the bag into a specific slot.
    /// The item must be in the bag and the slot must be empty.
    /// </summary>
    public EquipResult Equip(InventoryItem item, InventorySlot slot)
    {
        if (!BagContains(item))
            return EquipResult.ItemNotInInventory;

        if (!slot.IsEmpty)
            return EquipResult.SlotOccupied;

        if (!slot.CanAccept(item))
            return EquipResult.TagMismatch;

        RemoveFromBag(item);
        slot.TrySetItem(item);          // guaranteed to succeed after CanAccept check
        return EquipResult.Success;
    }

    /// <summary>
    /// Unequip a slot — item goes back to the bag.
    /// </summary>
    public bool Unequip(InventorySlot slot)
    {
        if (slot.IsEmpty) return false;
        var item = slot.Clear()!;
        AddToBag(item);
        return true;
    }

    /// <summary>
    /// Swap the item in a slot with an item in the bag.
    /// Respects tag restrictions — if the new item is rejected the swap is aborted.
    /// </summary>
    public EquipResult SwapWithBag(InventoryItem bagItem, InventorySlot slot)
    {
        if (!BagContains(bagItem))
            return EquipResult.ItemNotInInventory;

        if (!slot.CanAccept(bagItem))
            return EquipResult.TagMismatch;

        // Pull the currently equipped item back into the bag first
        var equipped = slot.Clear();
        if (equipped is not null)
            AddToBag(equipped);

        RemoveFromBag(bagItem);
        slot.TrySetItem(bagItem);
        return EquipResult.Success;
    }

    /// <summary>
    /// Swap items between two equipment slots (e.g. move from MainHand1 → MainHand2).
    /// Aborts if either slot rejects the other's item.
    /// </summary>
    public EquipResult SwapSlots(InventorySlot slotA, InventorySlot slotB)
    {
        // Validate both directions before touching anything
        if (!slotA.CanAccept(slotB.Item) || !slotB.CanAccept(slotA.Item))
            return EquipResult.TagMismatch;

        var itemA = slotA.Item;
        var itemB = slotB.Item;

        // Temporarily clear both, then set
        slotA.TrySetItem(null);
        slotB.TrySetItem(null);
        slotA.TrySetItem(itemB);
        slotB.TrySetItem(itemA);
        return EquipResult.Success;
    }

    // ── Auto-equip helper ─────────────────────

    /// <summary>
    /// Tries to equip an item into the best valid, empty slot automatically.
    /// Prefers slots with a specific tag restriction over unrestricted slots,
    /// so a pizza always lands in the off-hand before a main-hand slot.
    /// </summary>
    public EquipResult AutoEquip(InventoryItem item)
    {
        if (!BagContains(item))
            AddToBag(item);

        // First pass — prefer slots that have a specific tag requirement (most specific fit)
        foreach (var slot in EquipSlots)
        {
            if (slot.IsEmpty && slot.RequiredTag != ItemTag.None && slot.CanAccept(item))
                return Equip(item, slot);
        }

        // Second pass — fall back to unrestricted slots
        foreach (var slot in EquipSlots)
        {
            if (slot.IsEmpty && slot.RequiredTag == ItemTag.None && slot.CanAccept(item))
                return Equip(item, slot);
        }

        return EquipResult.SlotOccupied;
    }

    // ── Internal ──────────────────────────────

    private void OnSlotChanged(InventorySlot slot, InventoryItem? old, InventoryItem? @new)
    {
        // Hook your UI / animation system here.
        // Example: InventoryUI.Instance.Refresh(slot);
        Console.WriteLine(
            old is null
                ? $"  → {slot.Name}: equipped {@new?.DisplayName}"
                : @new is null
                    ? $"  → {slot.Name}: unequipped {old.DisplayName}"
                    : $"  → {slot.Name}: swapped {old.DisplayName} for {@new.DisplayName}"
        );
    }

    // ── Debug ─────────────────────────────────

    public void PrintState()
    {
        Console.WriteLine("=== Equipment Slots ===");
        foreach (var slot in EquipSlots)
            Console.WriteLine($"  {slot}");

        Console.WriteLine($"=== Bag ({_bag.Count} items) ===");
        foreach (var item in _bag)
            Console.WriteLine($"  {item}");
    }
}