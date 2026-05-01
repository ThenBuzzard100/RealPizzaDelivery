using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ItemPickup.cs
// Attach this to any item GameObject in the scene.
// Compatible with Mirror — retries finding the local player every frame
// until it spawns, instead of failing silently in Start().
// ─────────────────────────────────────────────────────────────────────────────
public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    [Tooltip("The item this pickup represents.")]
    public InventoryItemData ItemData;

    [Header("Pickup Settings")]
    [Tooltip("How close the player must be to see the prompt.")]
    public float PickupRadius = 2.5f;

    [Tooltip("Key the player presses to pick up.")]
    public KeyCode PickupKey = KeyCode.E;

    [Header("Prompt (optional)")]
    [Tooltip("A world-space UI GameObject showing '[E] Pick up'. Leave null to skip.")]
    public GameObject PromptObject;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Transform          _playerTransform;
    private InventoryBehaviour _inventory;
    private bool               _playerInRange = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (PromptObject != null)
            PromptObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        // Keep retrying until the local player spawns (handles Mirror network delay)
        if (_playerTransform == null || _inventory == null)
        {
            TryFindPlayer();
            return;
        }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        _playerInRange = dist <= PickupRadius;

        if (PromptObject != null)
            PromptObject.SetActive(_playerInRange);

        if (_playerInRange && Input.GetKeyDown(PickupKey))
            TryPickup();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void TryFindPlayer()
    {
        var candidates = GameObject.FindGameObjectsWithTag("Player");
        if (candidates.Length == 0) return;

        GameObject localPlayer = null;

#if MIRROR
        // With Mirror, only interact with the local player
        foreach (var candidate in candidates)
        {
            var ni = candidate.GetComponent<Mirror.NetworkIdentity>();
            if (ni != null && ni.isLocalPlayer)
            {
                localPlayer = candidate;
                break;
            }
        }
#else
        // No Mirror — just grab the first tagged player
        localPlayer = candidates[0];
#endif

        if (localPlayer == null) return;

        _playerTransform = localPlayer.transform;
        _inventory       = localPlayer.GetComponent<InventoryBehaviour>();

        if (_inventory == null)
            Debug.LogWarning($"[ItemPickup] Local player has no InventoryBehaviour.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void TryPickup()
    {
        if (ItemData == null)
        {
            Debug.LogWarning($"[ItemPickup] {gameObject.name} has no ItemData assigned.");
            return;
        }

        var item   = ItemData.CreateItem();
        var result = _inventory.Inventory.AutoEquip(item);
        Debug.Log($"[ItemPickup] Picked up: {item.DisplayName} — equip result: {result}");

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, PickupRadius);
    }
}