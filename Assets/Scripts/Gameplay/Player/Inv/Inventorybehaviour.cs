using UnityEngine;
using Mirror;

// ─────────────────────────────────────────────────────────────────────────────
// InventoryBehaviour.cs
// Attach to the Player GameObject. Uses Mirror's OnStartLocalPlayer so the
// Inventory is only ever initialized on the local player — never on remotes.
// ─────────────────────────────────────────────────────────────────────────────
public class InventoryBehaviour : NetworkBehaviour
{
    public Inventory Inventory { get; private set; }

    // Called by Mirror only on the local player — guaranteed after isLocalPlayer is set
    public override void OnStartLocalPlayer()
    {
        Inventory = new Inventory();
        Inventory.OnBagChanged += OnBagChanged;
    }

    private void OnBagChanged()
    {
        // Hook your UI refresh here
    }

    public EquipResult Equip(InventoryItem item, InventorySlot slot)
        => Inventory.Equip(item, slot);

    public bool Unequip(InventorySlot slot)
        => Inventory.Unequip(slot);

    public EquipResult AutoEquip(InventoryItem item)
        => Inventory.AutoEquip(item);
}

// ─────────────────────────────────────────────────────────────────────────────
// InventorySlotUI.cs
// ─────────────────────────────────────────────────────────────────────────────
public class InventorySlotUI : MonoBehaviour
{
    [Header("Slot Identity")]
    [Tooltip("Set by the editor builder — matches the InventorySlot name.")]
    public string SlotName;

    private UnityEngine.UI.Image _iconImage;
    [HideInInspector] public InventorySlot LinkedSlot;
    private bool _initialized = false;

    private void Start()
    {
        var iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            _iconImage = iconTransform.GetComponent<UnityEngine.UI.Image>();
        else
            Debug.LogWarning($"[SlotUI] {SlotName} has no child named 'Icon'. Rebuild the hotbar.");
    }

    private void Update()
    {
        if (_initialized) return;
        TryInitialize();
    }

    private void TryInitialize()
    {
        // Find the local player's InventoryBehaviour — only one will have Inventory initialized
        InventoryBehaviour inv = null;
        foreach (var candidate in FindObjectsByType<InventoryBehaviour>(FindObjectsSortMode.None))
        {
            if (candidate.Inventory == null) continue;   // remote players have null Inventory
            inv = candidate;
            break;
        }

        if (inv == null) return;

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
        RefreshIcon(LinkedSlot.Item);
        _initialized = true;
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
            Debug.LogWarning($"[SlotUI] No icon found for '{item.Id}'.");
            _iconImage.enabled = false;
        }
    }

    private InventoryItemData FindItemData(string itemId)
    {
        var all = Resources.LoadAll<InventoryItemData>("");
        foreach (var data in all)
            if (data != null && data.Id == itemId) return data;

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:InventoryItemData");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var data = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
            if (data != null && data.Id == itemId) return data;
        }
#endif
        Debug.LogWarning($"[SlotUI] Could not find InventoryItemData with Id '{itemId}'.");
        return null;
    }
}