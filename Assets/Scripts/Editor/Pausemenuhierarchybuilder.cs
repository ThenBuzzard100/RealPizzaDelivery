// Place in: Assets/Scripts/Editor/PauseMenuHierarchyBuilder.cs
// Access via: Tools/MainMenuHierarchy/Build Pause Menu

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds the pause menu Canvas hierarchy in the currently open scene.
/// Run via Tools → MainMenuHierarchy → Build Pause Menu
/// After building, assign the references to PauseMenuManager in the Inspector.
/// </summary>
public static class PauseMenuHierarchyBuilder
{
    private static TMP_FontAsset _font;

    // ── Colour Palette — dark industrial matching the pod/game aesthetic ──────
    private static readonly Color COL_BG         = new Color(0.04f, 0.04f, 0.05f, 0.96f);
    private static readonly Color COL_PANEL       = new Color(0.07f, 0.07f, 0.08f, 1.00f);
    private static readonly Color COL_BORDER      = new Color(0.18f, 0.17f, 0.16f, 1.00f);
    private static readonly Color COL_BTN_NORMAL  = new Color(0.10f, 0.10f, 0.11f, 1.00f);
    private static readonly Color COL_BTN_HOVER   = new Color(0.20f, 0.20f, 0.22f, 1.00f);
    private static readonly Color COL_BTN_PRESS   = new Color(0.05f, 0.05f, 0.06f, 1.00f);
    private static readonly Color COL_RED         = new Color(0.80f, 0.10f, 0.05f, 1.00f);
    private static readonly Color COL_RED_HOVER   = new Color(1.00f, 0.15f, 0.08f, 1.00f);
    private static readonly Color COL_ACCENT      = new Color(0.20f, 0.55f, 1.00f, 1.00f);
    private static readonly Color COL_TEXT        = new Color(0.90f, 0.88f, 0.85f, 1.00f);
    private static readonly Color COL_TEXT_DIM    = new Color(0.55f, 0.53f, 0.50f, 1.00f);
    private static readonly Color COL_HOST        = new Color(0.10f, 0.45f, 0.20f, 1.00f);
    private static readonly Color COL_HOST_HOVER  = new Color(0.15f, 0.60f, 0.28f, 1.00f);

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/MainMenuHierarchy/Build Pause Menu")]
    public static void BuildPauseMenu()
    {
        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // ── EventSystem ───────────────────────────────────────────────────────
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Root Canvas ───────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("PauseMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // above game UI

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.SetActive(false); // hidden by default — PauseMenuManager activates it

        // ── Full-screen dark backdrop ─────────────────────────────────────────
        GameObject backdrop = MakeImage(canvasGO, "Backdrop", new Color(0f, 0f, 0f, 0.65f));
        Anchor(backdrop, Vector2.zero, Vector2.one);

        // ── Centre panel container ────────────────────────────────────────────
        // Narrow vertical panel, left of centre (leaves game visible on right)
        GameObject panelContainer = new GameObject("PanelContainer");
        panelContainer.transform.SetParent(canvasGO.transform, false);
        RectTransform pcRT = panelContainer.AddComponent<RectTransform>();
        pcRT.anchorMin = new Vector2(0.28f, 0.15f);
        pcRT.anchorMax = new Vector2(0.72f, 0.85f);
        pcRT.offsetMin = pcRT.offsetMax = Vector2.zero;

        // ── Main Pause Panel ──────────────────────────────────────────────────
        GameObject mainPanel = BuildMainPausePanel(panelContainer);

        // ── Settings Panel ────────────────────────────────────────────────────
        GameObject settingsPanel = BuildSettingsPanel(panelContainer);

        // ── Host Panel ────────────────────────────────────────────────────────
        GameObject hostPanel = BuildHostPanel(panelContainer);

        // ── Return Confirm Panel ──────────────────────────────────────────────
        GameObject returnConfirmPanel = BuildReturnConfirmPanel(panelContainer);

        // ── PauseMenuManager on a persistent GO ───────────────────────────────
        GameObject managerGO = new GameObject("PauseMenuManager");
        PauseMenuManager pmm = managerGO.AddComponent<PauseMenuManager>();
        pmm.pauseCanvas        = canvas;
        pmm.pauseMainPanel     = mainPanel;
        pmm.settingsPanel      = settingsPanel;
        pmm.hostPanel          = hostPanel;
        pmm.returnConfirmPanel = returnConfirmPanel;

        // ── Wire main panel buttons ───────────────────────────────────────────
        WireButton(mainPanel,         "ResumeButton",   pmm, "OnResumeClicked");
        WireButton(mainPanel,         "SettingsButton", pmm, "OnSettingsClicked");
        WireButton(mainPanel,         "HostButton",     pmm, "OnHostClicked");
        WireButton(mainPanel,         "MainMenuButton", pmm, "OnReturnToMainMenuClicked");

        // ── Wire sub-panel back buttons ───────────────────────────────────────
        WireButton(settingsPanel,     "BackButton",     pmm, "OnBackClicked");
        WireButton(hostPanel,         "StartHostButton",pmm, "OnStartHostClicked");
        WireButton(hostPanel,         "JoinButton",     pmm, "OnJoinAsClientClicked");
        WireButton(hostPanel,         "BackButton",     pmm, "OnBackClicked");
        WireButton(returnConfirmPanel,"YesButton",      pmm, "OnReturnConfirmed");
        WireButton(returnConfirmPanel,"NoButton",       pmm, "OnReturnCancelled");

        Undo.RegisterCreatedObjectUndo(canvasGO,   "Build Pause Menu");
        Undo.RegisterCreatedObjectUndo(managerGO,  "Build Pause Menu");

        Selection.activeGameObject = canvasGO;
        Debug.Log("[PauseMenuBuilder] Pause menu built. All references wired to PauseMenuManager.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Panel Builders

    static GameObject BuildMainPausePanel(GameObject parent)
    {
        GameObject panel = MakeDarkPanel(parent, "MainPausePanel");

        // Title — "PAUSED"
        GameObject title = MakeLabel(panel, "TitleLabel", "PAUSED", 36);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.82f); tRT.anchorMax = new Vector2(1f, 0.97f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        title.GetComponent<TextMeshProUGUI>().color = COL_ACCENT;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Accent divider under title
        GameObject divider = MakeImage(panel, "TitleDivider", COL_ACCENT);
        RectTransform dRT = divider.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.1f, 0.80f); dRT.anchorMax = new Vector2(0.9f, 0.812f);
        dRT.offsetMin = dRT.offsetMax = Vector2.zero;

        // Multiplayer notice (shown at runtime by PauseMenuManager if needed)
        GameObject notice = MakeLabel(panel, "MultiplayerNotice",
            "Game continues for other players", 13);
        RectTransform nRT = notice.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0f, 0.74f); nRT.anchorMax = new Vector2(1f, 0.80f);
        nRT.offsetMin = nRT.offsetMax = Vector2.zero;
        notice.GetComponent<TextMeshProUGUI>().color = COL_TEXT_DIM;
        notice.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

        // Button stack — fixed anchor layout, no ContentSizeFitter
        GameObject btnGroup = new GameObject("ButtonGroup");
        btnGroup.transform.SetParent(panel.transform, false);
        RectTransform bgRT = btnGroup.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.08f, 0.08f);
        bgRT.anchorMax = new Vector2(0.92f, 0.72f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = btnGroup.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.spacing               = 12;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = true; // force buttons to fill the group evenly
        vlg.padding = new RectOffset(0, 0, 8, 8);

        MakePauseButton(btnGroup, "ResumeButton",   "RESUME",           COL_BTN_NORMAL, COL_BTN_HOVER);
        MakePauseButton(btnGroup, "SettingsButton", "SETTINGS",         COL_BTN_NORMAL, COL_BTN_HOVER);
        MakePauseButton(btnGroup, "HostButton",     "HOST / INVITE",    COL_HOST,       COL_HOST_HOVER);
        MakePauseButton(btnGroup, "MainMenuButton", "RETURN TO MENU",   COL_RED,        COL_RED_HOVER);

        return panel;
    }

    static GameObject BuildSettingsPanel(GameObject parent)
    {
        GameObject panel = MakeDarkPanel(parent, "SettingsPanel");
        panel.SetActive(false);

        // Title
        GameObject title = MakeLabel(panel, "TitleLabel", "SETTINGS", 28);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.91f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        title.GetComponent<TextMeshProUGUI>().color = COL_ACCENT;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Tab bar
        GameObject tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(panel.transform, false);
        Anchor(tabBar, new Vector2(0f, 0.82f), new Vector2(1f, 0.91f));
        HorizontalLayoutGroup tabHLG = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHLG.childForceExpandWidth  = true;
        tabHLG.childForceExpandHeight = true;
        tabHLG.spacing = 3;

        Button audioTabBtn    = MakeTabButton(tabBar, "AudioTabButton",    "AUDIO");
        Button graphicsTabBtn = MakeTabButton(tabBar, "GraphicsTabButton", "GRAPHICS");
        Button keybindsTabBtn = MakeTabButton(tabBar, "KeybindsTabButton", "KEYBINDS");

        // Tab content area
        GameObject tabArea = new GameObject("TabArea");
        tabArea.transform.SetParent(panel.transform, false);
        Anchor(tabArea, new Vector2(0f, 0.13f), new Vector2(1f, 0.82f));

        // ── Audio Tab ─────────────────────────────────────────────────────────
        GameObject audioTab = MakeTabContent(tabArea, "AudioTabContent");
        MakeSliderRow(audioTab, "MasterSlider", "Master Volume");
        MakeSliderRow(audioTab, "MusicSlider",  "Music Volume");
        MakeSliderRow(audioTab, "SFXSlider",    "SFX Volume");

        // ── Graphics Tab ──────────────────────────────────────────────────────
        GameObject graphicsTab = MakeTabContent(tabArea, "GraphicsTabContent");
        graphicsTab.SetActive(false);
        MakeDropdownRow(graphicsTab, "ResolutionDropdown", "Resolution");
        MakeToggleRow(graphicsTab,   "FullscreenToggle",   "Fullscreen");
        MakeDropdownRow(graphicsTab, "QualityDropdown",    "Quality");
        MakeToggleRow(graphicsTab,   "VSyncToggle",        "VSync");

        // ── Keybinds Tab ──────────────────────────────────────────────────────
        GameObject keybindsTab = MakeTabContent(tabArea, "KeybindsTabContent");
        keybindsTab.SetActive(false);
        MakeKeybindRow(keybindsTab, "Jump",      "Space");
        MakeKeybindRow(keybindsTab, "Sprint",    "LeftShift");
        MakeKeybindRow(keybindsTab, "Interact",  "E");
        MakeKeybindRow(keybindsTab, "Inventory", "Tab");

        // Bottom buttons
        GameObject applyBtn = MakePauseButton(panel, "ApplyButton", "APPLY", COL_HOST, COL_HOST_HOVER);
        RectTransform applyRT = applyBtn.GetComponent<RectTransform>();
        applyRT.anchorMin = new Vector2(0.08f, 0.02f); applyRT.anchorMax = new Vector2(0.50f, 0.12f);
        applyRT.offsetMin = applyRT.offsetMax = Vector2.zero;

        GameObject backBtn = MakePauseButton(panel, "BackButton", "← BACK", COL_BTN_NORMAL, COL_BTN_HOVER);
        RectTransform bRT = backBtn.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.52f, 0.02f); bRT.anchorMax = new Vector2(0.92f, 0.12f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;

        // Attach SettingsPanelController
        SettingsPanelController spc = panel.AddComponent<SettingsPanelController>();
        spc.audioTabContent    = audioTab;
        spc.graphicsTabContent = graphicsTab;
        spc.keybindsTabContent = keybindsTab;
        spc.audioTabButton     = audioTabBtn;
        spc.graphicsTabButton  = graphicsTabBtn;
        spc.keybindsTabButton  = keybindsTabBtn;

        // Wire tab buttons to SettingsPanelController
        WireButtonToComponent(tabBar, "AudioTabButton",    spc, "ShowAudioTab");
        WireButtonToComponent(tabBar, "GraphicsTabButton", spc, "ShowGraphicsTab");
        WireButtonToComponent(tabBar, "KeybindsTabButton", spc, "ShowKeybindsTab");
        WireButtonToComponent(panel,  "ApplyButton",       spc, "OnApplySettings");

        return panel;
    }

    static GameObject BuildHostPanel(GameObject parent)
    {
        GameObject panel = MakeDarkPanel(parent, "HostPanel");
        panel.SetActive(false);

        // Title
        GameObject title = MakeLabel(panel, "TitleLabel", "MULTIPLAYER", 28);
        RectTransform tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.85f); tRT.anchorMax = new Vector2(1f, 0.98f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        title.GetComponent<TextMeshProUGUI>().color = COL_ACCENT;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Description
        GameObject desc = MakeLabel(panel, "DescLabel",
            "Start hosting to invite Steam friends.\nThey can join via the Steam overlay.", 14);
        RectTransform dRT = desc.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.05f, 0.65f); dRT.anchorMax = new Vector2(0.95f, 0.84f);
        dRT.offsetMin = dRT.offsetMax = Vector2.zero;
        desc.GetComponent<TextMeshProUGUI>().color = COL_TEXT_DIM;

        // Status label (updated at runtime)
        GameObject status = MakeLabel(panel, "StatusLabel", "", 13);
        RectTransform sRT = status.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.05f, 0.55f); sRT.anchorMax = new Vector2(0.95f, 0.65f);
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;
        status.GetComponent<TextMeshProUGUI>().color = COL_HOST_HOVER;

        // Buttons
        GameObject startHostBtn = MakePauseButton(panel, "StartHostButton",
            "START HOSTING", COL_HOST, COL_HOST_HOVER);
        RectTransform shRT = startHostBtn.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0.08f, 0.38f); shRT.anchorMax = new Vector2(0.92f, 0.52f);
        shRT.offsetMin = shRT.offsetMax = Vector2.zero;

        GameObject joinBtn = MakePauseButton(panel, "JoinButton",
            "JOIN VIA STEAM INVITE", COL_BTN_NORMAL, COL_BTN_HOVER);
        RectTransform jRT = joinBtn.GetComponent<RectTransform>();
        jRT.anchorMin = new Vector2(0.08f, 0.22f); jRT.anchorMax = new Vector2(0.92f, 0.36f);
        jRT.offsetMin = jRT.offsetMax = Vector2.zero;

        GameObject backBtn = MakePauseButton(panel, "BackButton",
            "← BACK", COL_BTN_NORMAL, COL_BTN_HOVER);
        RectTransform bRT = backBtn.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.08f, 0.02f); bRT.anchorMax = new Vector2(0.92f, 0.14f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;

        return panel;
    }

    static GameObject BuildReturnConfirmPanel(GameObject parent)
    {
        GameObject panel = MakeDarkPanel(parent, "ReturnConfirmPanel");
        panel.SetActive(false);

        // Warning icon area
        GameObject warning = MakeLabel(panel, "WarningIcon", "⚠", 48);
        RectTransform wRT = warning.GetComponent<RectTransform>();
        wRT.anchorMin = new Vector2(0f, 0.68f); wRT.anchorMax = new Vector2(1f, 0.92f);
        wRT.offsetMin = wRT.offsetMax = Vector2.zero;
        warning.GetComponent<TextMeshProUGUI>().color = COL_RED;

        // Question
        GameObject question = MakeLabel(panel, "QuestionLabel",
            "Return to Main Menu?", 22);
        RectTransform qRT = question.GetComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0f, 0.52f); qRT.anchorMax = new Vector2(1f, 0.68f);
        qRT.offsetMin = qRT.offsetMax = Vector2.zero;
        question.GetComponent<TextMeshProUGUI>().color = COL_TEXT;
        question.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Sub-text warning for multiplayer
        GameObject sub = MakeLabel(panel, "SubLabel",
            "This will disconnect you from\nany active session.", 13);
        RectTransform sRT = sub.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.05f, 0.38f); sRT.anchorMax = new Vector2(0.95f, 0.52f);
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;
        sub.GetComponent<TextMeshProUGUI>().color = COL_TEXT_DIM;

        // Yes / No buttons side by side
        GameObject yesBtn = MakePauseButton(panel, "YesButton",
            "YES, LEAVE", COL_RED, COL_RED_HOVER);
        RectTransform yRT = yesBtn.GetComponent<RectTransform>();
        yRT.anchorMin = new Vector2(0.08f, 0.16f); yRT.anchorMax = new Vector2(0.48f, 0.34f);
        yRT.offsetMin = yRT.offsetMax = Vector2.zero;

        GameObject noBtn = MakePauseButton(panel, "NoButton",
            "NO, STAY", COL_BTN_NORMAL, COL_BTN_HOVER);
        RectTransform nRT = noBtn.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0.52f, 0.16f); nRT.anchorMax = new Vector2(0.92f, 0.34f);
        nRT.offsetMin = nRT.offsetMax = Vector2.zero;

        return panel;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Factory Helpers

    static GameObject MakeDarkPanel(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Dark background
        Image bg = go.AddComponent<Image>();
        bg.color = COL_BG;

        // Border outline using a child image with padding
        GameObject border = new GameObject("Border");
        border.transform.SetParent(go.transform, false);
        RectTransform bRT = border.AddComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(2, 2); bRT.offsetMax = new Vector2(-2, -2);
        Image bImg = border.AddComponent<Image>();
        bImg.color = COL_BORDER;
        bImg.raycastTarget = false;

        // Inner fill over border
        GameObject inner = new GameObject("Inner");
        inner.transform.SetParent(go.transform, false);
        RectTransform iRT = inner.AddComponent<RectTransform>();
        iRT.anchorMin = Vector2.zero; iRT.anchorMax = Vector2.one;
        iRT.offsetMin = new Vector2(3, 3); iRT.offsetMax = new Vector2(-3, -3);
        Image iImg = inner.AddComponent<Image>();
        iImg.color = COL_PANEL;
        iImg.raycastTarget = false;

        return go;
    }

    static GameObject MakeImage(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject MakeLabel(GameObject parent, string name, string text, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = _font;
        tmp.fontSize  = fontSize;
        tmp.color     = COL_TEXT;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static GameObject MakePauseButton(GameObject parent, string name,
        string label, Color normalColor, Color hoverColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        // Tell the layout group how tall this button wants to be
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight       = 60;
        le.preferredHeight = 60;
        le.flexibleHeight  = 1;

        Image img = go.AddComponent<Image>();
        img.color = normalColor;

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = normalColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor     = COL_BTN_PRESS;
        cb.selectedColor    = normalColor;
        btn.colors = cb;
        btn.targetGraphic = img;

        // Left accent bar
        GameObject accent = new GameObject("AccentBar");
        accent.transform.SetParent(go.transform, false);
        RectTransform aRT = accent.AddComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0f, 0.15f); aRT.anchorMax = new Vector2(0f, 0.85f);
        aRT.offsetMin = new Vector2(0, 0); aRT.offsetMax = new Vector2(4, 0);
        Image aImg = accent.AddComponent<Image>();
        aImg.color = hoverColor;
        aImg.raycastTarget = false;

        // Label
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0); tRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.font      = _font;
        tmp.fontSize  = 17;
        tmp.color     = COL_TEXT;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    static GameObject MakeVerticalGroup(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, int spacing)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.spacing               = spacing;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 8, 8);
        ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void WireButton(GameObject parent, string buttonName,
        PauseMenuManager target, string methodName)
    {
        Transform found = FindDeep(parent.transform, buttonName);
        if (found == null)
        {
            Debug.LogWarning($"[PauseMenuBuilder] Button not found: '{buttonName}' under '{parent.name}'");
            return;
        }
        Button btn = found.GetComponent<Button>();
        if (btn == null) return;

        var method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (method == null)
        {
            Debug.LogWarning($"[PauseMenuBuilder] Method not found: '{methodName}'");
            return;
        }

        var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), target, method);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static GameObject MakeTabContent(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.spacing               = 8;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        return go;
    }

    static Button MakeTabButton(GameObject parent, string name, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.color = COL_BTN_NORMAL;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = COL_BTN_NORMAL;
        cb.highlightedColor = COL_BTN_HOVER;
        cb.pressedColor     = COL_BTN_PRESS;
        btn.colors = cb;
        btn.targetGraphic = img;
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.font = _font; tmp.fontSize = 13;
        tmp.color = COL_TEXT; tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        return btn;
    }

    static void MakeSliderRow(GameObject parent, string name, string label)
    {
        GameObject row = new GameObject(name + "_Row");
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 32; rowLE.preferredHeight = 32;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        LayoutElement le = lbl.AddComponent<LayoutElement>();
        le.preferredWidth = 110; le.preferredHeight = 28;
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = label; lblT.font = _font; lblT.fontSize = 13;
        lblT.color = COL_TEXT; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(row.transform, false);
        LayoutElement sLE = sliderGO.AddComponent<LayoutElement>();
        sLE.preferredWidth = 130; sLE.preferredHeight = 20; sLE.flexibleWidth = 1;
        Slider s = sliderGO.AddComponent<Slider>();
        s.minValue = 0f; s.maxValue = 1f; s.value = 0.75f;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.3f); bgRT.anchorMax = new Vector2(1, 0.7f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.3f); faRT.anchorMax = new Vector2(1, 0.7f);
        faRT.offsetMin = new Vector2(5, 0); faRT.offsetMax = new Vector2(-5, 0);
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRT = fill.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(0.75f, 1);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;
        fill.AddComponent<Image>().color = COL_ACCENT;
        s.fillRect = fRT;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        handle.AddComponent<RectTransform>().sizeDelta = new Vector2(16, 16);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        s.handleRect = handle.GetComponent<RectTransform>();
        s.targetGraphic = handleImg;

        GameObject valGO = new GameObject(name + "_Val");
        valGO.transform.SetParent(row.transform, false);
        LayoutElement vLE = valGO.AddComponent<LayoutElement>();
        vLE.preferredWidth = 38; vLE.preferredHeight = 28;
        TextMeshProUGUI valT = valGO.AddComponent<TextMeshProUGUI>();
        valT.text = "75%"; valT.font = _font; valT.fontSize = 12;
        valT.color = COL_TEXT_DIM; valT.alignment = TextAlignmentOptions.MidlineLeft;
    }

    static void MakeDropdownRow(GameObject parent, string name, string label)
    {
        GameObject row = new GameObject(name + "_Row");
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 32; rowLE.preferredHeight = 32;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        LayoutElement le = lbl.AddComponent<LayoutElement>();
        le.preferredWidth = 110; le.preferredHeight = 28;
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = label; lblT.font = _font; lblT.fontSize = 13;
        lblT.color = COL_TEXT; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject ddGO = new GameObject(name);
        ddGO.transform.SetParent(row.transform, false);
        LayoutElement ddLE = ddGO.AddComponent<LayoutElement>();
        ddLE.preferredWidth = 150; ddLE.preferredHeight = 28; ddLE.flexibleWidth = 1;
        ddGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.16f);
        TMP_Dropdown dd = ddGO.AddComponent<TMP_Dropdown>();

        GameObject captionGO = new GameObject("Label");
        captionGO.transform.SetParent(ddGO.transform, false);
        RectTransform capRT = captionGO.AddComponent<RectTransform>();
        capRT.anchorMin = Vector2.zero; capRT.anchorMax = Vector2.one;
        capRT.offsetMin = new Vector2(8, 0); capRT.offsetMax = new Vector2(-24, 0);
        TextMeshProUGUI capTMP = captionGO.AddComponent<TextMeshProUGUI>();
        capTMP.font = _font; capTMP.fontSize = 12; capTMP.color = COL_TEXT;
        capTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText = capTMP;

        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(ddGO.transform, false);
        RectTransform arRT = arrow.AddComponent<RectTransform>();
        arRT.anchorMin = new Vector2(1, 0.5f); arRT.anchorMax = new Vector2(1, 0.5f);
        arRT.sizeDelta = new Vector2(16, 16); arRT.anchoredPosition = new Vector2(-12, 0);
        arrow.AddComponent<Image>().color = COL_TEXT_DIM;

        GameObject template = new GameObject("Template");
        template.transform.SetParent(ddGO.transform, false);
        RectTransform templateRT = template.AddComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0); templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f); templateRT.sizeDelta = new Vector2(0, 80);
        template.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.13f);
        ScrollRect templateSR = template.AddComponent<ScrollRect>();
        templateSR.horizontal = false;

        GameObject vp = new GameObject("Viewport");
        vp.transform.SetParent(template.transform, false);
        RectTransform vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<Image>(); vp.AddComponent<Mask>().showMaskGraphic = false;
        templateSR.viewport = vpRT;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(vp.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f); contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        templateSR.content = contentRT;

        GameObject item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        RectTransform itemRT = item.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f); itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 24);
        Toggle itemToggle = item.AddComponent<Toggle>();
        GameObject itemBg = new GameObject("Item Background");
        itemBg.transform.SetParent(item.transform, false);
        RectTransform ibRT = itemBg.AddComponent<RectTransform>();
        ibRT.anchorMin = Vector2.zero; ibRT.anchorMax = Vector2.one; ibRT.offsetMin = ibRT.offsetMax = Vector2.zero;
        Image ibImg = itemBg.AddComponent<Image>(); ibImg.color = new Color(0.18f, 0.18f, 0.19f);
        GameObject itemCheck = new GameObject("Item Checkmark");
        itemCheck.transform.SetParent(item.transform, false);
        RectTransform icRT = itemCheck.AddComponent<RectTransform>();
        icRT.anchorMin = new Vector2(0, 0.5f); icRT.anchorMax = new Vector2(0, 0.5f);
        icRT.sizeDelta = new Vector2(16, 16); icRT.anchoredPosition = new Vector2(10, 0);
        Image icImg = itemCheck.AddComponent<Image>(); icImg.color = COL_ACCENT;
        GameObject itemLbl = new GameObject("Item Label");
        itemLbl.transform.SetParent(item.transform, false);
        RectTransform ilRT = itemLbl.AddComponent<RectTransform>();
        ilRT.anchorMin = Vector2.zero; ilRT.anchorMax = Vector2.one;
        ilRT.offsetMin = new Vector2(20, 0); ilRT.offsetMax = Vector2.zero;
        TextMeshProUGUI ilTMP = itemLbl.AddComponent<TextMeshProUGUI>();
        ilTMP.font = _font; ilTMP.fontSize = 12; ilTMP.color = COL_TEXT;
        ilTMP.alignment = TextAlignmentOptions.MidlineLeft;
        itemToggle.targetGraphic = ibImg; itemToggle.graphic = icImg;
        dd.itemText = ilTMP; dd.template = templateRT;
        template.SetActive(false);
        dd.ClearOptions();
        dd.options.Add(new TMP_Dropdown.OptionData("Option A"));
        dd.options.Add(new TMP_Dropdown.OptionData("Option B"));
        dd.RefreshShownValue();
    }

    static void MakeToggleRow(GameObject parent, string name, string label)
    {
        GameObject row = new GameObject(name + "_Row");
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 32; rowLE.preferredHeight = 32;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        LayoutElement le = lbl.AddComponent<LayoutElement>();
        le.preferredWidth = 110; le.preferredHeight = 28;
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = label; lblT.font = _font; lblT.fontSize = 13;
        lblT.color = COL_TEXT; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject togGO = new GameObject(name);
        togGO.transform.SetParent(row.transform, false);
        RectTransform togRT = togGO.AddComponent<RectTransform>();
        togRT.sizeDelta = new Vector2(22, 22);
        LayoutElement togLE = togGO.AddComponent<LayoutElement>();
        togLE.preferredWidth = 22; togLE.preferredHeight = 22; togLE.flexibleWidth = 0;
        Image togBg = togGO.AddComponent<Image>(); togBg.color = new Color(0.2f, 0.2f, 0.22f);
        Toggle tog = togGO.AddComponent<Toggle>();
        GameObject check = new GameObject("Checkmark");
        check.transform.SetParent(togGO.transform, false);
        RectTransform cRT = check.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.15f, 0.15f); cRT.anchorMax = new Vector2(0.85f, 0.85f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        Image checkImg = check.AddComponent<Image>(); checkImg.color = COL_ACCENT;
        tog.targetGraphic = togBg; tog.graphic = checkImg; tog.isOn = false;
    }

    static void MakeKeybindRow(GameObject parent, string actionName, string defaultKey)
    {
        GameObject row = new GameObject(actionName + "_Row");
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 32);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 32; rowLE.preferredHeight = 32;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = 10; hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject lbl = new GameObject("ActionLabel");
        lbl.transform.SetParent(row.transform, false);
        LayoutElement le = lbl.AddComponent<LayoutElement>();
        le.preferredWidth = 110; le.preferredHeight = 28;
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = actionName; lblT.font = _font; lblT.fontSize = 13;
        lblT.color = COL_TEXT; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject keyLbl = new GameObject("KeyLabel");
        keyLbl.transform.SetParent(row.transform, false);
        LayoutElement kLE = keyLbl.AddComponent<LayoutElement>();
        kLE.preferredWidth = 80; kLE.preferredHeight = 28;
        TextMeshProUGUI keyT = keyLbl.AddComponent<TextMeshProUGUI>();
        keyT.text = defaultKey; keyT.font = _font; keyT.fontSize = 13;
        keyT.color = COL_ACCENT; keyT.alignment = TextAlignmentOptions.Center;

        GameObject rebindGO = new GameObject("RebindButton");
        rebindGO.transform.SetParent(row.transform, false);
        LayoutElement rLE = rebindGO.AddComponent<LayoutElement>();
        rLE.preferredWidth = 70; rLE.preferredHeight = 26; rLE.flexibleWidth = 0;
        rebindGO.AddComponent<Image>().color = COL_BTN_NORMAL;
        Button rebindBtn = rebindGO.AddComponent<Button>();
        ColorBlock cb = rebindBtn.colors;
        cb.normalColor = COL_BTN_NORMAL; cb.highlightedColor = COL_BTN_HOVER;
        rebindBtn.colors = cb;
        GameObject rbText = new GameObject("Text");
        rbText.transform.SetParent(rebindGO.transform, false);
        RectTransform rbRT = rbText.AddComponent<RectTransform>();
        rbRT.anchorMin = Vector2.zero; rbRT.anchorMax = Vector2.one;
        rbRT.offsetMin = rbRT.offsetMax = Vector2.zero;
        TextMeshProUGUI rbTMP = rbText.AddComponent<TextMeshProUGUI>();
        rbTMP.text = "REBIND"; rbTMP.font = _font; rbTMP.fontSize = 11;
        rbTMP.color = COL_TEXT; rbTMP.alignment = TextAlignmentOptions.Center;

        KeybindRow kr = row.AddComponent<KeybindRow>();
        kr.actionName   = actionName;
        kr.defaultKey   = defaultKey;
        kr.actionLabel  = lblT;
        kr.keyLabel     = keyT;
        kr.rebindButton = rebindBtn;
    }

    // Overload for wiring to a MonoBehaviour other than PauseMenuManager
    static void WireButtonToComponent(GameObject parent, string buttonName,
        MonoBehaviour target, string methodName)
    {
        Transform found = FindDeep(parent.transform, buttonName);
        if (found == null) { Debug.LogWarning($"[PauseMenuBuilder] Button not found: '{buttonName}'"); return; }
        Button btn = found.GetComponent<Button>();
        if (btn == null) return;
        var method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (method == null) { Debug.LogWarning($"[PauseMenuBuilder] Method not found: '{methodName}'"); return; }
        var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), target, method);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    #endregion
}
#endif