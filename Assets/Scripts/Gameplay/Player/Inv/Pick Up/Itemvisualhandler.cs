using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ItemVisualHandler.cs
// Attach to your player GameObject alongside InventoryBehaviour.
//
// Works in two modes:
//  1. Rigged character - assign the LeftHandBone transform in the inspector
//  (e.g. drag the LeftHandBone from your character's hierarchy)
//  2. Capsule / no rig = leave LeftHandBone empty, it will create an anchor automatically positioned to the left of the player.
//  Swap it for the real bone later without changing any code.
// ─────────────────────────────────────────────────────────────────────────────
public class ItemVisualHandler : MonoBehaviour
{
    [Header("Hand Anchor - Rigged Cjaracter")]
    [Tooltip("Drag the LeftHand bone here when you have a rigged character. " + "Leave empty to use the auto-generated anchor instead.")]
    public Transform LeftHandBone;

    [Header("Capsule / No Rig Fallback")]
    [Tooltip("Position offset from the player centre for the left hand anchor.")]
    public Vector3 LeftHandOffset = new Vector3(-0.6f, 0.8f, 0.3f);

    [Header("Item Spawn Settings")]
    [Tooltip("Scale applied to the item prefab when held. Adjust per-item via InventoryItemData.")]
    public Vector3 HeldScale = Vector3.one;
    public Vector3 HeldRotation = Vector3.zero; // Euler offset so item faces the right way

    // ── Internal ──────────────────────────────────────────────────────────────
    private InventoryBehaviour _inventory;
    private Transform _leftAnchor;
    private GameObject _leftHandVisual; // Currently spawned off-hand prefab

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _inventory = GetComponent<InventoryBehaviour>();
        if (_inventory == null)
        {
            Debug.LogError("[ItemVisualHandler] No InventoryBehaviour on this GameObject.");
            return;
        }

        SetupLeftAnchor();

        // Subscribe to off-hand slot changes
        _inventory.Inventory.OffHand.OnChanged += OnOffHandChanged;
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.Inventory.OffHand.OnChanged -= OnOffHandChanged;
    }

    // ── Anchor setup ──────────────────────────────────────────────────────────

    private void SetupLeftAnchor()
    {
        if (LeftHandBone != null)
        {
            // Use the real bone directly
            _leftAnchor = LeftHandBone;
            Debug.Log("[ItemVisualHandler] Using rigged LeftHandBone as anchor.");
        }
        else
        {
            // Create a placeholder anchor - swap LeftHandBone in later and it'll automatically switch without needing to change anything else
            var anchorGO = new GameObject("LeftHandAnchor");
            anchorGO.transform.SetParent(transform, false);
            anchorGO.transform.localPosition = LeftHandOffset;
            _leftAnchor = anchorGO.transform;
            Debug.Log("[ItemVisualHandler] No LeftHandBone assigned - created LeftHandAnchor. " + "Assign the bone in the Inspector when your character is rigged.");
        }
    }

    // ── Slot change handler ───────────────────────────────────────────────────

    private void OnOffHandChanged(InventoryItem old, InventoryItem @new)
    {
        // Destroy the old visual
        if (_leftHandVisual != null)
        {
            Destroy(_leftHandVisual);
            _leftHandVisual = null;
        }

        // Spawn the new one
        if (@new != null)
            SpawnVisual(@new);
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnVisual(InventoryItem item)
    {
        // Find the matching InventoryItemData to get the prefab
        var data = FindItemData(item.Id);
        if (data == null)
        {
            Debug.LogWarning($"[ItemVisualHandler] '{item.Id}' has no HeldPrefab assigned in its InventoryItemData.");
            return;
        }

        _leftHandVisual = Instantiate(data.HeldPrefab, _leftAnchor);
        _leftHandVisual.transform.localPosition = Vector3.zero;
        _leftHandVisual.transform.localRotation = Quaternion.Euler(data.HeldRotationOffset != Vector3.zero ? data.HeldRotationOffset : HeldRotation);
        _leftHandVisual.transform.localScale = data.HeldScaleOverride != Vector3.zero ? data.HeldScaleOverride : HeldScale;

        // Make sure the visual can't be interacted with as a pickup
        var pickup = _leftHandVisual.GetComponent<ItemPickup>();
        if (pickup != null) Destroy(pickup);

        // Disable any colliders so it doesn't block movement
        foreach (var col in _leftHandVisual.GetComponentsInChildren<Collider>())
            col.enabled = false;

            Debug.Log($"[ItemVisualHandler] Spawned visual for {item.DisplayName} in left hand.");
    }

    // ── Item data lookup ──────────────────────────────────────────────────────

    private static InventoryItemData FindItemData(string itemId)
    {
        #if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:InventoryItemData");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
                if (data != null && data.Id == itemId) return data;
            }
        #else
            var all = Resources.LoadAll<InventoryItemData>("Items");
            foreach (var data in all)
                if (data.Id == itemId) return data;
        #endif
        return null;
    }
}