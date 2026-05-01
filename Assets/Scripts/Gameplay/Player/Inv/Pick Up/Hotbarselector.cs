using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// HotbarSelector.cs
// Attach to the player GameObject alongside InventoryBehaviour.
// Handles ONLY slot selection — actual item use is handled by ItemUser.cs.
//
// Key bindings:
//   1 — select Main Hand 1
//   2 — select Main Hand 2
// ─────────────────────────────────────────────────────────────────────────────
public class HotbarSelector : MonoBehaviour
{
    [Header("Selection Highlight")]
    public Color HighlightColor = new Color(1f, 1f, 1f, 0.35f);

    // ── Internal ──────────────────────────────────────────────────────────────
    private InventoryBehaviour    _inventory;
    private List<InventorySlot>   _slots;
    private List<InventorySlotUI> _slotUIs;
    private int                   _selectedIndex = -1;

    public InventorySlot SelectedSlot
        => _selectedIndex >= 0 ? _slots[_selectedIndex] : null;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        _inventory = GetComponent<InventoryBehaviour>();
        if (_inventory == null)
        {
            Debug.LogError("[HotbarSelector] No InventoryBehaviour found.");
            return;
        }

        // Only main-hand slots are selectable — off-hand is always-on
        _slots = new List<InventorySlot>
        {
            _inventory.Inventory.MainHand1,
            _inventory.Inventory.MainHand2,
        };

        // Match SlotUI components by name
        _slotUIs = new List<InventorySlotUI>();
        var allUIs = FindObjectsByType<InventorySlotUI>(FindObjectsSortMode.None);
        foreach (var slot in _slots)
        {
            InventorySlotUI match = null;
            foreach (var ui in allUIs)
                if (ui.SlotName == slot.Name) { match = ui; break; }
            _slotUIs.Add(match);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_inventory == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void SelectSlot(int index)
    {
        // Same key again = deselect
        if (_selectedIndex == index)
        {
            ClearHighlight(_selectedIndex);
            _selectedIndex = -1;
            return;
        }

        if (_selectedIndex >= 0)
            ClearHighlight(_selectedIndex);

        _selectedIndex = index;
        ApplyHighlight(_selectedIndex);

        var slot = _slots[_selectedIndex];
        Debug.Log($"[HotbarSelector] Selected: {slot.Name}" +
                  (slot.IsEmpty ? " (empty)" : $" — {slot.Item.DisplayName}"));
    }

    // ── Highlight ─────────────────────────────────────────────────────────────

    private void ApplyHighlight(int index)
    {
        var highlight = GetOrCreateHighlight(index);
        if (highlight != null) highlight.enabled = true;
    }

    private void ClearHighlight(int index)
    {
        var highlight = GetOrCreateHighlight(index);
        if (highlight != null) highlight.enabled = false;
    }

    private Image GetOrCreateHighlight(int index)
    {
        if (_slotUIs == null || index < 0 || index >= _slotUIs.Count) return null;
        var ui = _slotUIs[index];
        if (ui == null) return null;

        var existing = ui.transform.Find("Highlight");
        if (existing != null)
            return existing.GetComponent<Image>();

        var go = new GameObject("Highlight");
        go.transform.SetParent(ui.transform, false);

        var rect       = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img   = go.AddComponent<Image>();
        img.color = HighlightColor;
        return img;
    }
}