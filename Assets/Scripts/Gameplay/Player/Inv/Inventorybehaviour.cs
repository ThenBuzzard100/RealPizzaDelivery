using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// InventoryBehaviour.cs
// Attach to the Player GameObject. Wraps the pure-C# Inventory class so it
// lives in Unity's component system.
// ─────────────────────────────────────────────────────────────────────────────
public class InventoryBehaviour : MonoBehaviour
{
    public Inventory Inventory { get; private set; }

    private void Awake()
    {
        Inventory = new Inventory();

        // Subscribe to bag changes if you want to drive UI from here
        Inventory.OnBagChanged += OnBagChanged;
    }

    private void OnBagChanged()
    {
        // Hook your UI refresh here, e.g.:
        // InventoryUIManager.Instance.RefreshBag(Inventory.Bag);
    }

    // ── Public helpers so other components can talk to the inventory ──────────

    public EquipResult Equip(InventoryItem item, InventorySlot slot)
        => Inventory.Equip(item, slot);

    public bool Unequip(InventorySlot slot)
        => Inventory.Unequip(slot);

    public EquipResult AutoEquip(InventoryItem item)
        => Inventory.AutoEquip(item);
}

// ─────────────────────────────────────────────────────────────────────────────
// InventorySlotUI.cs
// Sits on each slot GameObject. Hook this up to your drag-and-drop / click
// system. The editor window adds this automatically to every slot it creates.
// ─────────────────────────────────────────────────────────────────────────────
public class InventorySlotUI : MonoBehaviour
{
    [Header("Slot Identity")]
    [Tooltip("Set by the editor builder — matches the InventorySlot name.")]
    public string SlotName;

    private UnityEngine.UI.Image _iconImage;
    [HideInInspector] public InventorySlot LinkedSlot;

    private void Start()
    {
        // Find the Icon Image the builder created as a child
        var iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            _iconImage = iconTransform.GetComponent<UnityEngine.UI.Image>();
        else
            Debug.LogWarning($"[SlotUI] {SlotName} has no child named 'Icon'. Rebuild the hotbar via Tools → Inventory → Setup Inventory System.");

        var inv = FindFirstObjectByType<InventoryBehaviour>();
        if (inv == null)
        {
            Debug.LogWarning($"[SlotUI] No InventoryBehaviour found in scene.");
            return;
        }

        LinkedSlot = SlotName switch
        {
            "Main Hand 1" => inv.Inventory.MainHand1,
            "Main Hand 2" => inv.Inventory.MainHand2,
            "Off Hand"    => inv.Inventory.OffHand,
            _             => null
        };

        if (LinkedSlot == null)
        {
            Debug.LogWarning($"[SlotUI] Could not find slot named '{SlotName}'.");
            return;
        }

        LinkedSlot.OnChanged += OnSlotChanged;

        // Refresh immediately in case an item was already in this slot
        RefreshIcon(LinkedSlot.Item);
    }

    private void OnDestroy()
    {
        if (LinkedSlot != null)
            LinkedSlot.OnChanged -= OnSlotChanged;
    }

    private void OnSlotChanged(InventoryItem old, InventoryItem @new)
    {
        RefreshIcon(@new);
    }

    private void RefreshIcon(InventoryItem item)
    {
        if (_iconImage == null) return;

        if (item == null)
        {
            _iconImage.sprite  = null;
            _iconImage.enabled = false;
            return;
        }

        var data = FindItemData(item.Id);
        if (data != null && data.Icon != null)
        {
            _iconImage.sprite         = data.Icon;
            _iconImage.preserveAspect = true;
            _iconImage.enabled        = true;
        }
        else
        {
            Debug.LogWarning($"[SlotUI] No icon found for '{item.Id}'. " +
                              "Check the InventoryItemData Id matches exactly and has an Icon assigned.");
            _iconImage.enabled = false;
        }
    }

    private static InventoryItemData FindItemData(string itemId)
    {
#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:InventoryItemData");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
            if (data != null && data.Id == itemId)
                return data;
        }
#else
        // At runtime in a build, put your InventoryItemData assets in Resources/Items/
        var all = Resources.LoadAll<InventoryItemData>("Items");
        foreach (var data in all)
            if (data.Id == itemId) return data;
#endif
        return null;
    }
}