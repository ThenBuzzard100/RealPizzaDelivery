#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// ─────────────────────────────────────────────────────────────────────────────
// InventoryItemDataEditor.cs
// Place inside any Editor/ folder.
// Shows only the fields relevant to the selected ItemType in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────
[CustomEditor(typeof(InventoryItemData))]
public class InventoryItemDataEditor : Editor
{
    // Foldout states
    private bool _showIdentity  = true;
    private bool _showVisuals   = true;
    private bool _showTypeStats = true;
    private bool _showShared    = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var data = (InventoryItemData)target;

        // ── Header ────────────────────────────────────────────────────────────
        DrawHeader(data);
        EditorGUILayout.Space(4);

        // ── Identity ──────────────────────────────────────────────────────────
        _showIdentity = DrawFoldout(_showIdentity, "Identity", DrawIdentity);

        // ── Visuals ───────────────────────────────────────────────────────────
        _showVisuals = DrawFoldout(_showVisuals, "Visuals", DrawVisuals);

        // ── Type-specific stats ───────────────────────────────────────────────
        string statsLabel = $"{data.Type} Stats";
        _showTypeStats = DrawFoldout(_showTypeStats, statsLabel, () => DrawTypeStats(data.Type));

        // ── Shared ────────────────────────────────────────────────────────────
        _showShared = DrawFoldout(_showShared, "Shared Settings", DrawShared);

        serializedObject.ApplyModifiedProperties();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void DrawHeader(InventoryItemData data)
    {
        var bgColor = TypeColor(data.Type);

        // Coloured banner
        var bannerRect = EditorGUILayout.GetControlRect(false, 40);
        EditorGUI.DrawRect(bannerRect, bgColor);

        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };
        EditorGUI.LabelField(bannerRect, $"{data.DisplayName}  [{data.Type}]", labelStyle);

        EditorGUILayout.Space(2);

        // Type selector right below the banner — most important field
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"),
            new GUIContent("Item Type", "Changing this updates which stats are shown below."));
    }

    // ── Identity ──────────────────────────────────────────────────────────────

    private void DrawIdentity()
    {
        DrawProp("Id",          "Unique ID — must match exactly in code and ItemPickup.");
        DrawProp("DisplayName", "Name shown in the hotbar and UI.");
        DrawProp("Tags",        "Controls which slots this item can go into.");
        DrawProp("Description", "Flavour text shown on hover or in menus.");
    }

    // ── Visuals ───────────────────────────────────────────────────────────────

    private void DrawVisuals()
    {
        DrawProp("Icon",               "Sprite shown in the hotbar slot.");
        DrawProp("HeldPrefab",         "3D prefab spawned in the player's hand when equipped.");
        DrawProp("HeldRotationOffset", "Rotation tweak so the item faces the right way in hand.");
        DrawProp("HeldScaleOverride",  "Scale tweak when held. Leave at (0,0,0) to use the global default.");
    }

    // ── Type stats ────────────────────────────────────────────────────────────

    private void DrawTypeStats(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:      DrawWeaponStats();     break;
            case ItemType.Food:        DrawFoodStats();       break;
            case ItemType.Throwable:   DrawThrowableStats();  break;
            case ItemType.Tool:        DrawToolStats();       break;
            case ItemType.Consumable:  DrawConsumableStats(); break;
            case ItemType.Passive:     DrawPassiveStats();    break;
        }
    }

    private void DrawWeaponStats()
    {
        HelpBox("How much damage it deals, how far it reaches, and its swing speed.");
        DrawProp("Damage",           "Damage dealt per hit.");
        DrawProp("AttackRange",      "Forward distance of the attack raycast/spherecast.");
        DrawProp("AttackRadius",     "Width of the attack — larger = more forgiving hit detection.");
        DrawProp("AttackCooldown",   "Seconds between attacks.");
        DrawProp("SwingAnimTrigger", "Animator trigger name for the swing animation.");
    }

    private void DrawFoodStats()
    {
        HelpBox("Restores health. Can heal instantly or over time.");
        DrawProp("HealAmount",    "How much health is restored.");
        DrawProp("EatDuration",   "How long the eat animation plays before the heal applies.");
        DrawProp("HealOverTime",  "Spread the heal across multiple ticks instead of all at once.");

        if (serializedObject.FindProperty("HealOverTime").boolValue)
            DrawProp("HealTickRate", "Seconds between each heal tick.");

        DrawProp("EatAnimTrigger", "Animator trigger name for the eat animation.");
    }

    private void DrawThrowableStats()
    {
        HelpBox("Thrown at a target. Can explode on impact.");
        DrawProp("ThrowDamage",      "Damage dealt on impact or explosion.");
        DrawProp("ThrowForce",       "How fast it flies.");
        DrawProp("ThrowRadius",      "Radius of impact damage.");
        DrawProp("ExplodesOnImpact", "If true, damages everything within ThrowRadius.");
        DrawProp("ThrowAnimTrigger", "Animator trigger name for the throw animation.");
    }

    private void DrawToolStats()
    {
        HelpBox("Utility items — torches, keys, etc.");
        DrawProp("LightRadius",    "If it emits light, how far it reaches.");
        DrawProp("RequiresTarget", "If true, the item needs something to interact with (e.g. a door).");
        DrawProp("UseAnimTrigger", "Animator trigger name for the use animation.");
    }

    private void DrawConsumableStats()
    {
        HelpBox("One-time use buffs — potions, power-ups, etc.");

        DrawProp("EffectDuration", "How long the buff lasts in seconds.");

        DrawProp("BoostsSpeed", "Grants a speed boost when consumed.");
        if (serializedObject.FindProperty("BoostsSpeed").boolValue)
            DrawProp("SpeedBoostAmount", "How much speed is added.");

        DrawProp("BoostsDamage", "Grants a damage boost when consumed.");
        if (serializedObject.FindProperty("BoostsDamage").boolValue)
            DrawProp("DamageBoostAmount", "How much extra damage is added.");

        DrawProp("ConsumeAnimTrigger", "Animator trigger name for the consume animation.");
    }

    private void DrawPassiveStats()
    {
        HelpBox("Always-active effects while held in the off-hand.");

        DrawProp("EmitsLight", "Emits a light source while held.");
        if (serializedObject.FindProperty("EmitsLight").boolValue)
            DrawProp("PassiveLightRadius", "Radius of the emitted light.");

        DrawProp("GrantsPassiveBuff", "Grants a passive stat buff while held.");
        if (serializedObject.FindProperty("GrantsPassiveBuff").boolValue)
            DrawProp("PassiveBuffAmount", "Size of the passive buff.");
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private void DrawShared()
    {
        HelpBox("Settings that apply to all item types.");
        DrawProp("IsStackable",     "Can multiple of this item stack in one bag slot.");

        if (serializedObject.FindProperty("IsStackable").boolValue)
            DrawProp("MaxStackSize", "Maximum items per stack.");

        DrawProp("Weight",           "Item weight — hook into a carry limit system if needed.");
        DrawProp("IsDestroyedOnUse", "Remove from inventory after being used once.");
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private bool DrawFoldout(bool state, string label, System.Action content)
    {
        EditorGUILayout.Space(2);
        var rect = EditorGUILayout.GetControlRect(false, 22);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
        state = EditorGUI.Foldout(rect, state, $"  {label}", true, EditorStyles.foldoutHeader);

        if (state)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);
            content();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        return state;
    }

    private void DrawProp(string propName, string tooltip = "")
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, new GUIContent(ObjectNames.NicifyVariableName(propName), tooltip));
        else
            EditorGUILayout.HelpBox($"Property '{propName}' not found.", MessageType.Warning);
    }

    private static void HelpBox(string msg)
    {
        EditorGUILayout.HelpBox(msg, MessageType.None);
        EditorGUILayout.Space(2);
    }

    private static Color TypeColor(ItemType type) => type switch
    {
        ItemType.Weapon     => new Color(0.65f, 0.15f, 0.15f, 1f),   // red
        ItemType.Food       => new Color(0.20f, 0.55f, 0.20f, 1f),   // green
        ItemType.Throwable  => new Color(0.65f, 0.45f, 0.10f, 1f),   // orange
        ItemType.Tool       => new Color(0.20f, 0.35f, 0.65f, 1f),   // blue
        ItemType.Consumable => new Color(0.45f, 0.20f, 0.65f, 1f),   // purple
        ItemType.Passive    => new Color(0.25f, 0.50f, 0.55f, 1f),   // teal
        _                   => new Color(0.30f, 0.30f, 0.30f, 1f),
    };
}
#endif