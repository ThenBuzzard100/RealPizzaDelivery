using System;

// ─────────────────────────────────────────────
// Slot types — used for labelling & rule logic
// ─────────────────────────────────────────────
public enum SlotType
{
    MainHand,
    OffHand,
}

// ─────────────────────────────────────────────
// A single equipment slot.
// If requiredTag != Item.Tag.None, only items
// that carry that tag may be placed here
// ─────────────────────────────────────────────
public class InventorySlot
{
    public string Name { get; }
    public SlotType Type { get; }
    public ItemTag RequiredTag { get; } // ItemTag.None = accept anything
    public InventoryItem? Item { get; private set; }
    public bool IsEmpty => Item is null;

    // Fired whenever the slot's item changed (old, new) — hook into UI here
    public event Action<InventoryItem?, InventoryItem?>? OnChanged;

    public InventorySlot(string name, SlotType type, ItemTag requiredTag = ItemTag.None)
    {
        Name = name;
        Type = type;
        RequiredTag = requiredTag;
    }

        // ── Validation ────────────────────────────

        /// <summary>
        /// Returns true if the given item is allowed in this slot.
        /// A null item is always valid (represents clearing the slot).
        /// </summary>
        public bool CanAccept(InventoryItem? item)
        {
            if (item is null) return true;
            if (RequiredTag == ItemTag.None) return true;
            return item.HasTag(RequiredTag);
        }

        // ── Mutation ──────────────────────────────

        /// <summary>
        /// Attempt to place an item in this slot.
        /// Returns false (and leaves the slot unchanged) if the item is rejected.
        /// </summary>
        public bool TrySetItem(InventoryItem? newItem)
        {
            if (!CanAccept(newItem))
                return false;

            var old = Item;
            Item = newItem;
            OnChanged?.Invoke(old, Item);
            return true;
        }
        
        /// <summary>Clears the slot and returns whatever was in it (or null).</summary>
        public InventoryItem? Clear()
        {
            var removed = Item;
            TrySetItem(null);
            return removed;
        }

        public override string ToString()
            => IsEmpty
                ? $"{Name} [{Type}] — empty"
                : $"{Name} [{Type}] — {Item}";
}