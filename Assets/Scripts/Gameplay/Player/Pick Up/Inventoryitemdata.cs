using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// InventoryItemData.cs
// A ScriptableObject that defines an item's identity
// Create one per item type via
//  Right-click in Project → Create → Inventory (very bottom) → Iten Data
// Then drag it onto an ItemPickup in the scene
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(menuName = "Inventory/Item Data", fileName = "NewItemData")]
public class InventoryItemData : ScriptableObject
{
    [Header("ID")]
    public string Id = "item_01";
    public string DisplayName = "New Item";
    public ItemTag Tags = ItemTag.None;

    [Header("Visuals")]
    public Sprite Icon; // Shown in the hotbar slot

    /// <summary>
    /// Creates a fresh runtime InventoryItem from this definition.
    /// Called by ItemPickup when the player collects the object.
    /// </summary>
    public InventoryItem CreateItem()
    {
        return new InventoryItem(Id, DisplayName, Tags);
    }
}