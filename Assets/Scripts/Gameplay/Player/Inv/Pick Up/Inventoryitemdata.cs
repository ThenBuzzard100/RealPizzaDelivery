using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// What kind of item this is.
// Add new types here and the custom Inspector will pick them up automatically.
// ─────────────────────────────────────────────────────────────────────────────
public enum ItemType
{
    Weapon,
    Food,
    Throwable,
    Tool,
    Consumable,
    Passive,
}

// ─────────────────────────────────────────────────────────────────────────────
// InventoryItemData.cs
// One ScriptableObject per item type in your game.
// Create via: Right-click in Project → Create → Inventory → Item Data
// ─────────────────────────────────────────────────────────────────────────────
[CreateAssetMenu(menuName = "Inventory/Item Data", fileName = "NewItemData")]
public class InventoryItemData : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────────
    [Header("Identity")]
    public string   Id          = "item_01";
    public string   DisplayName = "New Item";
    public ItemTag  Tags        = ItemTag.None;
    public ItemType Type        = ItemType.Weapon;

    [TextArea(2, 4)]
    public string Description = "";

    // ── Visuals ───────────────────────────────────────────────────────────────
    [Header("Visuals")]
    public Sprite     Icon;
    public GameObject HeldPrefab;
    public Vector3    HeldRotationOffset = Vector3.zero;
    public Vector3    HeldScaleOverride  = Vector3.zero;

    // ── Weapon ────────────────────────────────────────────────────────────────
    [Header("Weapon")]
    public int    Damage           = 10;
    public float  AttackRange      = 2f;
    public float  AttackRadius     = 1.5f;
    public float  AttackCooldown   = 0.5f;
    public string SwingAnimTrigger = "SwordSwing";

    // ── Food ──────────────────────────────────────────────────────────────────
    [Header("Food")]
    public int    HealAmount     = 25;
    public float  EatDuration    = 1.5f;
    public bool   HealOverTime   = false;
    public float  HealTickRate   = 0.5f;
    public string EatAnimTrigger = "Eat";

    // ── Throwable ─────────────────────────────────────────────────────────────
    [Header("Throwable")]
    public int    ThrowDamage      = 20;
    public float  ThrowForce       = 15f;
    public float  ThrowRadius      = 3f;
    public bool   ExplodesOnImpact = false;
    public string ThrowAnimTrigger = "Throw";

    // ── Tool ──────────────────────────────────────────────────────────────────
    [Header("Tool")]
    public float  LightRadius    = 5f;
    public bool   RequiresTarget = false;
    public string UseAnimTrigger = "Use";

    // ── Consumable ────────────────────────────────────────────────────────────
    [Header("Consumable")]
    public bool   BoostsSpeed       = false;
    public int    SpeedBoostAmount  = 2;
    public bool   BoostsDamage      = false;
    public int    DamageBoostAmount = 5;
    public float  EffectDuration    = 5f;
    public string ConsumeAnimTrigger = "Drink";

    // ── Passive ───────────────────────────────────────────────────────────────
    [Header("Passive")]
    public bool   EmitsLight        = false;
    public float  PassiveLightRadius = 4f;
    public bool   GrantsPassiveBuff = false;
    public float  PassiveBuffAmount = 0f;

    // ── Shared ────────────────────────────────────────────────────────────────
    [Header("Shared")]
    public bool  IsStackable      = false;
    public int   MaxStackSize     = 1;
    public float Weight           = 1f;
    public bool  IsDestroyedOnUse = false;

    // ─────────────────────────────────────────────────────────────────────────
    public InventoryItem CreateItem() => new InventoryItem(Id, DisplayName, Tags);
}