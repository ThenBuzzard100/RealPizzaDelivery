using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────
// Tags — add as many as you want on specific item.
// The off-hand slot checks for a specific tag.
// ─────────────────────────────────────────────
[Flags]
public enum ItemTag
{
    None = 0,
    Weapon = 1 << 0,
    Shield = 1 << 1,
    Pizza = 1 << 2, // <── required tag for the off-hand slot
    Tool = 1 << 3,
    Consumable = 1 << 4,
    Magic = 1 << 5,
}

// ─────────────────────────────────────────────
// Base item — extend this for Sword, Pizza, etc.
// ─────────────────────────────────────────────
public class InventoryItem
{
    public string Id { get; }
    public string DisplayName { get; }
    public ItemTag Tags { get; }

    public InventoryItem(string id, string displayName, ItemTag tags)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Item ID cannot be empty.", nameof(id));

        Id = id;
        DisplayName = displayName;
        Tags = tags;
    }

    /// <summary>Returns true if this item has all of the specified tags.</summary>
    public bool HasTag(ItemTag tag) => (Tags & tag) == tag;

    public override string ToString() => $"[{Id}] {DisplayName} (Tags: {Tags})";
}