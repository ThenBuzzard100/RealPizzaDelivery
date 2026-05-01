using UnityEngine;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// Itemuser.cs
// Attach to the player alongside InventoryBehaviour and Hotbarselector.
// Handles actually executing item use based on the ItemType in InventoryItemData.
// No more subclasses needed - everything is driven by the ScriptableObject.
// ─────────────────────────────────────────────────────────────────────────────
public class Itemuser : MonoBehaviour
{
    private InventoryBehaviour _inventory;
    private HotbarSelector _selector;
    private Animator _animator;
    private HealthComponent _health;

    // Cooldown tracking
    private float _lastAttackTime = 0f;

    // Active buff tracking
    private Coroutine _activeSpeedBuff;
    private Coroutine _activeDamageBuff;
    private int _currentDamageBonus = 0;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _inventory = GetComponent<InventoryBehaviour>();
        _selector = GetComponent<HotbarSelector>();
        _animator = GetComponent<Animator>();
        _health = GetComponent<HealthComponent>();

        if (_inventory == null) Debug.LogError("[ItemUser] Missing InventoryBehaviour.");
        if (_selector == null) Debug.LogError("[ItemUser] Missing HotbarSelector.");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryUseSelected();
    }

    // ── Use ───────────────────────────────────────────────────────────────────

    private void TryUseSelected()
    {
        var slot = _selector?.SelectedSlot;
        if (slot == null || slot.IsEmpty)
        {
            Debug.Log("[ItemUser] Nothing selected or slot is empty.");
            return;
        }

        var data = FindItemData(slot.Item.Id);
        if (data == null)
        {
            Debug.LogWarning($"[ItemUser] No InventoryItemData found for '{slot.Item.Id}'.");
            return;
        }

        UseItem(data, slot);
    }

    private void UseItem(InventoryItemData data, InventorySlot slot)
    {
        switch (data.Type)
        {
            case ItemType.Weapon: UseWeapon(data); break;
            case ItemType.Food: UseFood(data, slot); break;
            case ItemType.Throwable: UseThrowable(data); break;
            case ItemType.Tool: UseTool(data); break;
            case ItemType.Consumable: UseConsumable(data, slot); break;
            case ItemType.Passive: break; // passives are always-on, not triggered
        }
    }

    // ── Weapon ────────────────────────────────────────────────────────────────

    private void UseWeapon(InventoryItemData data)
    {
        if (Time.time - _lastAttackTime < data.AttackCooldown)
            return;

        _lastAttackTime = Time.time;

        PlayAnim(data.SwingAnimTrigger);

        int totalDamage = data.Damage + _currentDamageBonus;

        var hits = Physics.SphereCastAll(
            transform.position,
            data.AttackRadius,
            transform.forward,
            data.AttackRange);
        
        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;

            var health = hit.collider.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(totalDamage);
                Debug.Log($"[ItemUser] Hit {hit.collider.name} for {totalDamage} damage.");
            }
        }
    }

    // ── Food ──────────────────────────────────────────────────────────────────

    private void UseFood(InventoryItemData data, InventorySlot slot)
    {
        if (_health == null) return;

        PlayAnim(data.EatAnimTrigger);

        if (data.HealOverTime)
            StartCoroutine(HealOverTime(data, slot));
        else
        {
            _health.Heal(data.HealAmount);
            ConsumeIfNeeded(data, slot);
        }
    }

    private IEnumerator HealOverTime(InventoryItemData data, InventorySlot slot)
    {
        float elapsed = 0f;
        int ticksTotal = Mathf.FloorToInt(data.EatDuration / data.HealTickRate);
        int healPerTick = Mathf.Max(1, data.HealAmount / ticksTotal);

        while (elapsed < data.EatDuration)
        {
            yield return new WaitForSeconds(data.HealTickRate);
            _health.Heal(healPerTick);
            elapsed += data.HealTickRate;
        }

        ConsumeIfNeeded(data, slot);
    }

    // ── Throwable ─────────────────────────────────────────────────────────────

    private void UseThrowable(InventoryItemData data)
    {
        PlayAnim(data.ThrowAnimTrigger);
        
        // Spawn the held prefab and throw it - hook into your projectile system herer
        Debug.Log($"[ItemUser] Threw {data.DisplayName} with force {data.ThrowForce}.");

        // Ex: find a ThrowableProjectile component and launch it
        // var proj = Instantiate(data.HeldPrefab, transform.position + transform.forward, transform.rotation);
        // proj.GetComponent<Rigidbody>()?.AddForce(transform.forward * data.ThrowForce, ForceMode.Impulse);
    }

    // ── Tool ──────────────────────────────────────────────────────────────────

    private void UseTool(InventoryItemData data)
    {
        PlayAnim(data.UseAnimTrigger);
        Debug.Log($"[ItemUser] Used tool: {data.DisplayName}.");
        // Hook your tool logic here - e.g. phone interactability, toggle a flashlight
    }

    // ── Consumable ────────────────────────────────────────────────────────────

    private void UseConsumable(InventoryItemData data, InventorySlot slot)
    {
        PlayAnim(data.ConsumeAnimTrigger);

        if (data.BoostsSpeed)
        {
            if (_activeSpeedBuff != null) StopCoroutine(_activeSpeedBuff);
            _activeSpeedBuff = StartCoroutine(SpeedBuff(data));
        }

        if (data.BoostsDamage)
        {
            if (_activeDamageBuff != null) StopCoroutine(_activeDamageBuff);
            _activeDamageBuff = StartCoroutine(DamageBuff(data));
        }

        ConsumeIfNeeded(data, slot);
    }

    private IEnumerator SpeedBuff(InventoryItemData data)
    {
        // Hook into PlayerMovement script here when needed
        Debug.Log($"[ItemUser] Speed boosted by {data.SpeedBoostAmount} for {data.EffectDuration}s.");
        // e.g. GetComponent<PlayerMovement>().AddSpeedBonus(data.SpeedBoostAmount);
        yield return new WaitForSeconds(data.EffectDuration);
        // e.g. GetComponent<PlayerMovement>().RemoveSpeedBonus(data.SpeedBoostAmount);
        Debug.Log("[ItemUser] Speed buff expired.");
    }

    private IEnumerator DamageBuff(InventoryItemData data)
    {
        _currentDamageBonus += data.DamageBoostAmount;
        Debug.Log($"[ItemUser] Damage boosted by {data.DamageBoostAmount} for {data.EffectDuration}s.");
        yield return new WaitForSeconds(data.EffectDuration);
        _currentDamageBonus -= data.DamageBoostAmount;
        Debug.Log("[ItemUser] Damage buff expired.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ConsumeIfNeeded(InventoryItemData data, InventorySlot slot)
    {
        if (!data.IsDestroyedOnUse) return;
        _inventory.Inventory.Unequip(slot);
        // Also remove from bag since it was just used
        if (_inventory.Inventory.BagContains(slot.Item))
            _inventory.Inventory.RemoveFromBag(slot.Item);
        Debug.Log($"[ItemUser] {data.DisplayName} consumed and removed.");
    }

    private void PlayAnim(string trigger)
    {
        if (_animator != null && !string.IsNullOrEmpty(trigger))
            _animator.SetTrigger(trigger);
    }

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