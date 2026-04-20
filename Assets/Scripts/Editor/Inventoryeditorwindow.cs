#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// UnityEngine.UI lives in the "Unity UI" package.
// If still missing: Package Manager → install "Unity UI" (com.unity.ugui)
using UnityEngine.UI;

// TMPro — if missing: Package Manager → install "TextMeshPro",
// then run Window → TextMeshPro → Import TMP Essential Resources.
// Once installed, add TMP_PRESENT to Project Settings → Player → Scripting Define Symbols.
#if TMP_PRESENT
using TMPro;
#endif

// ─────────────────────────────────────────────────────────────────────────────
// Place this file inside any folder named Editor/ in your project.
// Access via: Unity menu → Tools → Inventory → Setup Inventory System
// ─────────────────────────────────────────────────────────────────────────────
public class InventoryEditorWindow : EditorWindow
{
    // ── Settings ──────────────────────────────────────────────────────────────
    private GameObject _playerTarget;
    private Canvas     _targetCanvas;
    private bool       _createCanvas    = true;
    private bool       _attachInventory = true;
    private string     _canvasName      = "InventoryCanvas";

    // Peak = warm earthy tones. Wobbly Life = chunky saturated colours.
    private Color _mainHandColor = new Color(0.95f, 0.82f, 0.44f, 1f);
    private Color _offHandColor  = new Color(0.42f, 0.82f, 0.58f, 1f);
    private Color _bgColor       = new Color(0.18f, 0.15f, 0.22f, 0.94f);
    private float _slotSize      = 76f;
    private float _padding       = 10f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Vector2 _scroll;
    private string  _statusMessage = "";
    private bool    _statusIsError = false;

    // ── Menu — Tools section ──────────────────────────────────────────────────
    [MenuItem("Tools/Inventory/Setup Inventory System")]
    public static void Open()
    {
        var window = GetWindow<InventoryEditorWindow>("Inventory Setup");
        window.minSize = new Vector2(340, 500);
        window.Show();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GUI
    // ─────────────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawHeader();
        EditorGUILayout.Space(8);

        DrawSection("Target",       DrawTargetSection);
        DrawSection("Canvas",       DrawCanvasSection);
        DrawSection("Hotbar Style", DrawAppearanceSection);
        DrawSection("Scripts",      DrawScriptsSection);

        EditorGUILayout.Space(12);
        DrawBuildButton();

        if (!string.IsNullOrEmpty(_statusMessage))
            DrawStatus();

        EditorGUILayout.EndScrollView();
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    private void DrawHeader()
    {
        EditorGUILayout.Space(6);
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
        };
        EditorGUILayout.LabelField("Inventory System Builder", titleStyle, GUILayout.Height(26));

        var subStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        EditorGUILayout.LabelField("Peak / Wobbly Life hotbar style", subStyle, GUILayout.Height(16));
        EditorGUILayout.Space(4);
        DrawHorizontalLine();
    }

    private void DrawTargetSection()
    {
        _playerTarget = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Player GameObject", "The player that will own the Inventory component."),
            _playerTarget, typeof(GameObject), true);
    }

    private void DrawCanvasSection()
    {
        _createCanvas = EditorGUILayout.Toggle(
            new GUIContent("Create New Canvas", "Uncheck to use an existing canvas."),
            _createCanvas);

        if (_createCanvas)
            _canvasName = EditorGUILayout.TextField("Canvas Name", _canvasName);
        else
            _targetCanvas = (Canvas)EditorGUILayout.ObjectField(
                "Existing Canvas", _targetCanvas, typeof(Canvas), true);
    }

    private void DrawAppearanceSection()
    {
        EditorGUILayout.HelpBox(
            "Warm earthy tones from Peak + chunky saturated palette from Wobbly Life.",
            MessageType.None);

        _slotSize      = EditorGUILayout.FloatField("Slot Size (px)",    _slotSize);
        _padding       = EditorGUILayout.FloatField("Slot Padding (px)", _padding);
        _bgColor       = EditorGUILayout.ColorField("Panel Background",  _bgColor);
        _mainHandColor = EditorGUILayout.ColorField("Main Hand Colour",  _mainHandColor);
        _offHandColor  = EditorGUILayout.ColorField("Off Hand Colour",   _offHandColor);
    }

    private void DrawScriptsSection()
    {
        _attachInventory = EditorGUILayout.Toggle(
            new GUIContent("Attach Inventory Script", "Adds InventoryBehaviour to the player."),
            _attachInventory);
    }

    private void DrawBuildButton()
    {
        GUI.backgroundColor = new Color(0.28f, 0.75f, 0.42f);
        if (GUILayout.Button("Build Inventory Hotbar", GUILayout.Height(38)))
            Build();
        GUI.backgroundColor = Color.white;
    }

    private void DrawStatus()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            _statusMessage,
            _statusIsError ? MessageType.Error : MessageType.Info);
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void Build()
    {
        _statusMessage = "";
        _statusIsError = false;

        if (_attachInventory && _playerTarget == null)
        { SetError("Assign a Player GameObject first."); return; }

        if (!_createCanvas && _targetCanvas == null)
        { SetError("Assign an existing Canvas or enable 'Create New Canvas'."); return; }

        Undo.SetCurrentGroupName("Build Inventory Hotbar");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas     canvas   = _createCanvas ? CreateCanvas() : _targetCanvas;
        float      offSize  = _slotSize * 1.5f;
        GameObject panel    = CreateHotbarPanel(canvas.transform, offSize);

        // All slots same size — off-hand leftmost, HorizontalLayoutGroup handles spacing
        CreateSlot(panel.transform, "Off Hand",    _offHandColor,  "OFF", offSize);
        CreateSlot(panel.transform, "Main Hand 1", _mainHandColor, "MH1", offSize);
        CreateSlot(panel.transform, "Main Hand 2", _mainHandColor, "MH2", offSize);

        if (_attachInventory && _playerTarget != null)
        {
            if (_playerTarget.GetComponent<InventoryBehaviour>() == null)
                Undo.AddComponent<InventoryBehaviour>(_playerTarget);
            else
                Debug.LogWarning("[Inventory Builder] InventoryBehaviour already attached — skipped.");
        }

        Undo.CollapseUndoOperations(undoGroup);
        string canvasName = _createCanvas ? _canvasName : _targetCanvas.name;
        SetSuccess($"Hotbar built under '{canvasName}' in the Hierarchy.");
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    private Canvas CreateCanvas()
    {
        var go = new GameObject(_canvasName);
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");

        var canvas          = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler                 = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private GameObject CreateHotbarPanel(Transform parent, float offSize)
    {
        var go = new GameObject("HotbarPanel");
        Undo.RegisterCreatedObjectUndo(go, "Create HotbarPanel");
        go.transform.SetParent(parent, false);

        // Anchor to bottom-right
        var rect              = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(1f, 0f);
        rect.anchorMax        = new Vector2(1f, 0f);
        rect.pivot            = new Vector2(1f, 0f);
        float totalW          = (offSize * 3f) + (_padding * 4f);
        float totalH          = offSize + (_padding * 2f);
        rect.sizeDelta        = new Vector2(totalW, totalH);
        rect.anchoredPosition = new Vector2(-24f, 24f);

        var img   = go.AddComponent<Image>();
        img.color = _bgColor;

        // HorizontalLayoutGroup places slots cleanly left-to-right with no manual offset math
        var layout                  = go.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment       = TextAnchor.MiddleCenter;
        layout.spacing              = _padding;
        layout.padding              = new RectOffset(
            (int)_padding, (int)_padding,
            (int)_padding, (int)_padding);
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth      = false;
        layout.childControlHeight     = false;

        // Rim highlight
        var rimGO = new GameObject("Rim");
        Undo.RegisterCreatedObjectUndo(rimGO, "Create Rim");
        rimGO.transform.SetParent(go.transform, false);

        var rimRect              = rimGO.AddComponent<RectTransform>();
        rimRect.anchorMin        = new Vector2(0f, 1f);
        rimRect.anchorMax        = new Vector2(1f, 1f);
        rimRect.pivot            = new Vector2(0.5f, 1f);
        rimRect.sizeDelta        = new Vector2(-16f, 3f);
        rimRect.anchoredPosition = new Vector2(0f, -5f);

        var rimImg   = rimGO.AddComponent<Image>();
        rimImg.color = new Color(1f, 1f, 1f, 0.09f);

        return go;
    }

    private GameObject CreateSlot(Transform parent, string slotName,
                                  Color accent, string shortLabel, float size)
    {
        var go = new GameObject(slotName);
        Undo.RegisterCreatedObjectUndo(go, $"Create {slotName}");
        go.transform.SetParent(parent, false);

        var rect         = go.AddComponent<RectTransform>();
        rect.sizeDelta   = new Vector2(size, size);

        // Slot background
        var bg    = go.AddComponent<Image>();
        bg.color  = new Color(accent.r, accent.g, accent.b, 0.18f);

        // Accent border
        var borderGO = new GameObject("AccentBorder");
        Undo.RegisterCreatedObjectUndo(borderGO, "Create AccentBorder");
        borderGO.transform.SetParent(go.transform, false);

        var bRect       = borderGO.AddComponent<RectTransform>();
        bRect.anchorMin = Vector2.zero;
        bRect.anchorMax = Vector2.one;
        bRect.offsetMin = new Vector2(2f, 2f);
        bRect.offsetMax = new Vector2(-2f, -2f);

        var bImg   = borderGO.AddComponent<Image>();
        bImg.color = new Color(accent.r, accent.g, accent.b, 0.65f);

        // Icon — scales proportionally with slot size (90% of slot)
        var iconGO = new GameObject("Icon");
        Undo.RegisterCreatedObjectUndo(iconGO, "Create Icon");
        iconGO.transform.SetParent(go.transform, false);

        float iconSize           = size * 0.9f;
        var iconRect             = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin       = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax       = new Vector2(0.5f, 0.5f);
        iconRect.pivot           = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta       = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = Vector2.zero;

        var iconImg             = iconGO.AddComponent<Image>();
        iconImg.color           = Color.white;
        iconImg.preserveAspect  = true;
        iconImg.enabled         = false;

        // Key label at bottom
        var keyGO = new GameObject("KeyLabel");
        Undo.RegisterCreatedObjectUndo(keyGO, "Create KeyLabel");
        keyGO.transform.SetParent(go.transform, false);

        var keyRect              = keyGO.AddComponent<RectTransform>();
        keyRect.anchorMin        = new Vector2(0f,   0f);
        keyRect.anchorMax        = new Vector2(1f,   0f);
        keyRect.pivot            = new Vector2(0.5f, 0f);
        keyRect.sizeDelta        = new Vector2(0f,   16f);
        keyRect.anchoredPosition = new Vector2(0f,   4f);

        AddText(keyGO, shortLabel, 8, new Color(accent.r, accent.g, accent.b, 0.9f));

        // Slot name above
        var nameGO = new GameObject("SlotName");
        Undo.RegisterCreatedObjectUndo(nameGO, "Create SlotName");
        nameGO.transform.SetParent(go.transform, false);

        var nameRect              = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin        = new Vector2(0f,   1f);
        nameRect.anchorMax        = new Vector2(1f,   1f);
        nameRect.pivot            = new Vector2(0.5f, 0f);
        nameRect.sizeDelta        = new Vector2(0f,   14f);
        nameRect.anchoredPosition = new Vector2(0f,   4f);

        AddText(nameGO, slotName, 7, new Color(1f, 1f, 1f, 0.4f));

        var slotUI = go.AddComponent<InventorySlotUI>();
        slotUI.SlotName = slotName;

        return go;
    }

    private void CreateSeparator(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Separator");
        Undo.RegisterCreatedObjectUndo(go, "Create Separator");
        go.transform.SetParent(parent, false);

        var rect              = go.AddComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(2f, _slotSize * 0.55f);
        rect.anchoredPosition = pos;

        var img   = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.11f);
    }

    // ── Text helper ───────────────────────────────────────────────────────────

    private static void AddText(GameObject go, string content, int size, Color color)
    {
#if TMP_PRESENT
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = content;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
#else
        var txt       = go.AddComponent<Text>();
        txt.text      = content;
        txt.fontSize  = size;
        txt.alignment = TextAnchor.UpperCenter;
        txt.color     = color;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#endif
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private void DrawSection(string title, System.Action draw)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        DrawHorizontalLine(0.5f);
        EditorGUI.indentLevel++;
        draw();
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);
    }

    private static void DrawHorizontalLine(float h = 1f)
    {
        var r = EditorGUILayout.GetControlRect(false, h);
        EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.35f));
    }

    private void SetError(string msg)   { _statusMessage = msg; _statusIsError = true;  Repaint(); }
    private void SetSuccess(string msg) { _statusMessage = msg; _statusIsError = false; Repaint(); }
}
#endif