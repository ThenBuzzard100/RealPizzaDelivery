using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// ItemPickup.cs
// Attach this to any item GameObject in the scene.
// Set the Item field in the Inspector to the InventoryItem you want it to hold.
// Player walks near it, presses E, item goes into the bag.
// ─────────────────────────────────────────────────────────────────────────────
public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    [Tooltip("The item this pickup represents.")]
    public InventoryItemData ItemData;   // ScriptableObject — see InventoryItemData.cs

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
        // Find the player — tag your player GameObject as "Player" in Unity
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"[ItemPickup] No GameObject tagged 'Player' found. " +
                              "Tag your player in the Inspector.");
            return;
        }

        _playerTransform = player.transform;
        _inventory       = player.GetComponent<InventoryBehaviour>();

        if (_inventory == null)
            Debug.LogWarning($"[ItemPickup] Player has no InventoryBehaviour component.");

        if (PromptObject != null)
            PromptObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_playerTransform == null || _inventory == null) return;

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        _playerInRange = dist <= PickupRadius;

        // Show / hide prompt
        if (PromptObject != null)
            PromptObject.SetActive(_playerInRange);

        // Pickup input
        if (_playerInRange && Input.GetKeyDown(PickupKey))
            TryPickup();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void TryPickup()
    {
        if (ItemData == null)
        {
            Debug.LogWarning($"[ItemPickup] {gameObject.name} has no ItemData assigned.");
            return;
        }

        // Build a runtime InventoryItem from the ScriptableObject definition
        var item = ItemData.CreateItem();

        // Auto-equip into the first valid empty slot — falls back to bag if all slots full
        var result = _inventory.Inventory.AutoEquip(item);
        Debug.Log($"[ItemPickup] Picked up: {item.DisplayName} — equip result: {result}");

        // Destroy the world object after pickup
        Destroy(gameObject);
    }

    // ── Draw pickup radius in the editor so you can see it ───────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.92f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, PickupRadius);
    }
}