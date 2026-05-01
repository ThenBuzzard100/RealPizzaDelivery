using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ItemVisualHandler.cs
// Attach to the player GameObject alongside InventoryBehaviour.
//
// Works in two modes:
//   1. Rigged character — assign LeftHandBone in the Inspector
//   2. Capsule / no rig — leave LeftHandBone empty, creates an anchor automatically
//      Swap it for the real bone later with no code changes needed.
//
// Mirror compatible — retries finding the local inventory until it's ready.
// ─────────────────────────────────────────────────────────────────────────────
public class ItemVisualHandler : MonoBehaviour
{
    [Header("Hand Anchor — Rigged Character")]
    [Tooltip("Drag the LeftHand bone here when you have a rigged character. " +
             "Leave empty to use the auto-generated anchor instead.")]
    public Transform LeftHandBone;

    [Header("Capsule / No Rig Fallback")]
    [Tooltip("Position offset from the player centre for the left hand anchor.")]
    public Vector3 LeftHandOffset = new Vector3(-0.6f, 0.8f, 0.3f);

    [Header("Item Spawn Settings")]
    [Tooltip("Default scale applied to the held prefab. Override per-item in InventoryItemData.")]
    public Vector3 HeldScale    = Vector3.one;
    [Tooltip("Default rotation applied to the held prefab. Override per-item in InventoryItemData.")]
    public Vector3 HeldRotation = Vector3.zero;

    // ── Internal ──────────────────────────────────────────────────────────────
    private InventoryBehaviour _inventory;
    private Transform          _leftAnchor;
    private GameObject         _leftHandVisual;
    private bool               _isLocalPlayer = false;
    private bool               _initialized   = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Check if this is the local player
        var ni = GetComponent<Mirror.NetworkIdentity>();
        if (ni != null && !ni.isLocalPlayer)
            return;

        _isLocalPlayer = true;
        SetupLeftAnchor();
    }

    private void Update()
    {
        if (!_isLocalPlayer) return;
        if (_initialized) return;
        TryInitialize();
    }

    private void TryInitialize()
    {
        _inventory = GetComponent<InventoryBehaviour>();
        if (_inventory == null) return;
        if (_inventory.Inventory == null) return;

        // Subscribe to off-hand changes
        _inventory.Inventory.OffHand.OnChanged += OnOffHandChanged;

        // If something is already in the off-hand, spawn it
        if (!_inventory.Inventory.OffHand.IsEmpty)
            SpawnVisual(_inventory.Inventory.OffHand.Item);

        _initialized = true;
    }

    private void OnDestroy()
    {
        if (!_isLocalPlayer) return;
        if (_inventory?.Inventory == null) return;
        _inventory.Inventory.OffHand.OnChanged -= OnOffHandChanged;
    }

    // ── Anchor setup ──────────────────────────────────────────────────────────

    private void SetupLeftAnchor()
    {
        if (LeftHandBone != null)
        {
            _leftAnchor = LeftHandBone;
            Debug.Log("[ItemVisualHandler] Using rigged LeftHandBone as anchor.");
        }
        else
        {
            var anchorGO = new GameObject("LeftHandAnchor");
            anchorGO.transform.SetParent(transform, false);
            anchorGO.transform.localPosition = LeftHandOffset;
            _leftAnchor = anchorGO.transform;
            Debug.Log("[ItemVisualHandler] No LeftHandBone assigned — using LeftHandAnchor. " +
                      "Assign the real bone in the Inspector when your character is rigged.");
        }
    }

    // ── Slot change handler ───────────────────────────────────────────────────

    private void OnOffHandChanged(InventoryItem old, InventoryItem @new)
    {
        // Destroy the current visual
        if (_leftHandVisual != null)
        {
            Destroy(_leftHandVisual);
            _leftHandVisual = null;
        }

        // Spawn the new one if something was equipped
        if (@new != null)
            SpawnVisual(@new);
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    private void SpawnVisual(InventoryItem item)
    {
        if (_leftAnchor == null)
        {
            Debug.LogWarning("[ItemVisualHandler] No left anchor set up yet.");
            return;
        }

        var data = FindItemData(item.Id);
        if (data == null)
        {
            Debug.LogWarning($"[ItemVisualHandler] No InventoryItemData found for id '{item.Id}'.");
            return;
        }

        if (data.HeldPrefab == null)
        {
            Debug.LogWarning($"[ItemVisualHandler] '{item.DisplayName}' has no HeldPrefab assigned in its InventoryItemData. " +
                             "Assign the prefab in the Inspector.");
            return;
        }

        _leftHandVisual = Instantiate(data.HeldPrefab, _leftAnchor);
        _leftHandVisual.transform.localPosition = Vector3.zero;
        _leftHandVisual.transform.localRotation = Quaternion.Euler(
            data.HeldRotationOffset != Vector3.zero ? data.HeldRotationOffset : HeldRotation);
        _leftHandVisual.transform.localScale =
            data.HeldScaleOverride != Vector3.zero ? data.HeldScaleOverride : HeldScale;

        // Remove pickup component so it can't be picked up again
        var pickup = _leftHandVisual.GetComponent<ItemPickup>();
        if (pickup != null) Destroy(pickup);

        // Disable colliders so it doesn't block movement
        foreach (var col in _leftHandVisual.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Debug.Log($"[ItemVisualHandler] Spawned '{item.DisplayName}' in left hand.");
    }

    // ── Item data lookup ──────────────────────────────────────────────────────

    private static InventoryItemData FindItemData(string itemId)
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
        return null;
    }
}