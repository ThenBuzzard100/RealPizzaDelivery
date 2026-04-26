// Place this file in: Assets/Scripts/MainMenu/Editor/MainMenuHierarchyBuilder.cs
// Tools → MainMenuHierarchy → Build Hierarchy

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MainMenuHierarchyBuilder
{
    private static TMP_FontAsset _defaultFont;

    [MenuItem("Tools/MainMenuHierarchy/Build Hierarchy")]
    public static void BuildHierarchy()
    {
        // Load default TMP font
        _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // ── Clean up any existing hierarchy ──────────────────────────────────
        DestroyExisting("Canvas");
        DestroyExisting("BackgroundCamera");
        DestroyExisting("OrbitTarget");
        DestroyExisting("MenuManager");

        // ── Background Camera ─────────────────────────────────────────────────
        GameObject bgCamGO = new GameObject("BackgroundCamera");
        Camera bgCam = bgCamGO.AddComponent<Camera>();
        bgCam.depth = -2;
        bgCam.clearFlags = CameraClearFlags.SolidColor;
        bgCam.backgroundColor = new Color(0.08f, 0.15f, 0.35f, 1f); // deep blue
        bgCam.cullingMask = ~(1 << 5);
        bgCam.rect = new Rect(0.45f, 0f, 0.55f, 1f);
        bgCamGO.AddComponent<BackgroundCameraController>();
        new GameObject("OrbitTarget");

        // ── Menu Manager ──────────────────────────────────────────────────────
        GameObject menuManagerGO = new GameObject("MenuManager");

        // ── Canvas (Screen Space Overlay) ─────────────────────────────────────
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        // Note: SplitScreenLayout removed - layout handled by builder anchors directly

        // EventSystem
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Left Dark Panel - ends where gradient starts ───────────────────────
        GameObject leftPanel = MakeImage(canvasGO, "LeftDarkPanel", new Color(0.05f, 0.05f, 0.05f, 1f));
        Anchor(leftPanel, new Vector2(0, 0), new Vector2(0.37f, 1));

        // ── Gradient Strip - straddles the seam ───────────────────────────────
        // Left half overlaps dark panel, right half overlaps camera view
        GameObject gradientStrip = MakeImage(canvasGO, "GradientStrip", Color.white);
        Anchor(gradientStrip, new Vector2(0.37f, 0), new Vector2(0.52f, 1));
        gradientStrip.AddComponent<GradientStrip>();

        // ── Right Panel - transparent, shows camera behind it ────────────────
        GameObject rightPanel = MakeImage(canvasGO, "RightPanel", new Color(0, 0, 0, 0));
        Anchor(rightPanel, new Vector2(0.42f, 0), new Vector2(1, 1));
        rightPanel.GetComponent<Image>().raycastTarget = false;

        // Camera starts where left panel ends
        bgCam.rect = new Rect(0.42f, 0f, 0.58f, 1f);

        // ── Fade Overlay ──────────────────────────────────────────────────────
        GameObject fadeGO = MakeImage(canvasGO, "FadeOverlay", Color.black);
        Anchor(fadeGO, Vector2.zero, Vector2.one);
        CanvasGroup fadeGroup = fadeGO.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGO.SetActive(false);

        // ═════════════════════════════════════════════════════════════════════
        // MAIN PANEL
        // ═════════════════════════════════════════════════════════════════════
        GameObject mainPanel = MakePanel(canvasGO, "MainPanel");

        GameObject mainButtons = MakeVerticalGroup(mainPanel, "MainButtons",
            new Vector2(0f, 0.2f), new Vector2(0.42f, 0.8f), 24);

        MakeButton(mainButtons, "PlayButton",     "PLAY");
        MakeButton(mainButtons, "SettingsButton", "SETTINGS");
        MakeButton(mainButtons, "QuitButton",     "QUIT");

        // ═════════════════════════════════════════════════════════════════════
        // PLAY PANEL
        // ═════════════════════════════════════════════════════════════════════
        GameObject playPanel = MakePanel(canvasGO, "PlayPanel");
        playPanel.SetActive(false);

        GameObject choiceView = MakePanel(playPanel, "ChoiceView");
        GameObject choiceButtons = MakeVerticalGroup(choiceView, "ChoiceButtons",
            new Vector2(0f, 0.25f), new Vector2(0.45f, 0.75f), 20);
        MakeButton(choiceButtons, "NewWorldButton",  "NEW WORLD");
        MakeButton(choiceButtons, "LoadWorldButton", "LOAD WORLD");
        MakeButton(choiceButtons, "BackButton",      "BACK");

        GameObject newWorldView = MakePanel(playPanel, "NewWorldView");
        newWorldView.SetActive(false);

        // Title label
        GameObject nwTitle = MakeLabel(newWorldView, "WorldNameLabel", "Enter World Name");
        RectTransform nwTitleRT = nwTitle.GetComponent<RectTransform>();
        nwTitleRT.anchorMin = new Vector2(0.02f, 0.68f); nwTitleRT.anchorMax = new Vector2(0.43f, 0.76f);
        nwTitleRT.offsetMin = nwTitleRT.offsetMax = Vector2.zero;

        // Input field - explicitly anchored so layout group can't collapse it
        GameObject inputGO = new GameObject("WorldNameInput");
        inputGO.transform.SetParent(newWorldView.transform, false);
        RectTransform inputRT = inputGO.AddComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0.02f, 0.55f); inputRT.anchorMax = new Vector2(0.43f, 0.67f);
        inputRT.offsetMin = inputRT.offsetMax = Vector2.zero;
        inputGO.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.22f, 1f);
        TMP_InputField field = inputGO.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputGO.transform, false);
        RectTransform taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(10, 0); taRT.offsetMax = new Vector2(-10, 0);
        textArea.AddComponent<RectMask2D>();

        GameObject ph = new GameObject("Placeholder");
        ph.transform.SetParent(textArea.transform, false);
        RectTransform phRT = ph.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text = "Enter world name..."; phTMP.font = _defaultFont;
        phTMP.fontSize = 16; phTMP.color = new Color(0.5f, 0.5f, 0.5f);
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(textArea.transform, false);
        RectTransform txtRT = txt.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.font = _defaultFont; txtTMP.fontSize = 16;
        txtTMP.color = Color.white; txtTMP.alignment = TextAlignmentOptions.MidlineLeft;
        field.textViewport = taRT; field.textComponent = txtTMP; field.placeholder = phTMP;

        // Error text
        GameObject errorText = MakeLabel(newWorldView, "ErrorText", "Please enter a world name.");
        errorText.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.3f, 0.3f);
        RectTransform errRT = errorText.GetComponent<RectTransform>();
        errRT.anchorMin = new Vector2(0.02f, 0.47f); errRT.anchorMax = new Vector2(0.43f, 0.54f);
        errRT.offsetMin = errRT.offsetMax = Vector2.zero;

        // Create button
        GameObject createBtn = MakeButton(newWorldView, "CreateButton", "CREATE");
        RectTransform createRT = createBtn.GetComponent<RectTransform>();
        createRT.anchorMin = new Vector2(0.02f, 0.34f); createRT.anchorMax = new Vector2(0.25f, 0.45f);
        createRT.offsetMin = createRT.offsetMax = Vector2.zero;

        // Back button
        GameObject nwBackBtn = MakeButton(newWorldView, "BackButton", "BACK");
        RectTransform nwBackRT = nwBackBtn.GetComponent<RectTransform>();
        nwBackRT.anchorMin = new Vector2(0.26f, 0.34f); nwBackRT.anchorMax = new Vector2(0.43f, 0.45f);
        nwBackRT.offsetMin = nwBackRT.offsetMax = Vector2.zero;

        GameObject loadWorldView = MakePanel(playPanel, "LoadWorldView");
        loadWorldView.SetActive(false);
        GameObject scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(loadWorldView.transform, false);
        Anchor(scrollGO, new Vector2(0.02f, 0.15f), new Vector2(0.43f, 0.88f));
        scrollGO.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        ScrollRect sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = contentRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
        contentVLG.childForceExpandWidth  = true;
        contentVLG.childForceExpandHeight = false;
        contentVLG.spacing = 4;
        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRT;
        // NoSavesText centred inside the scroll area
        GameObject noSavesGO = MakeLabel(loadWorldView, "NoSavesText", "No saved worlds found.");
        RectTransform noSavesRT = noSavesGO.GetComponent<RectTransform>();
        noSavesRT.anchorMin = new Vector2(0.02f, 0.45f);
        noSavesRT.anchorMax = new Vector2(0.43f, 0.55f);
        noSavesRT.offsetMin = noSavesRT.offsetMax = Vector2.zero;

        // Back button anchored to the bottom-left of the left half
        GameObject loadBackBtn = MakeButton(loadWorldView, "BackButton", "BACK");
        RectTransform loadBackRT = loadBackBtn.GetComponent<RectTransform>();
        loadBackRT.anchorMin = new Vector2(0.02f, 0.01f);
        loadBackRT.anchorMax = new Vector2(0.02f, 0.01f);
        loadBackRT.pivot     = new Vector2(0f, 0f);
        loadBackRT.anchoredPosition = Vector2.zero;
        loadBackRT.sizeDelta = new Vector2(180, 48);

        PlayPanelController ppc = playPanel.AddComponent<PlayPanelController>();
        ppc.choiceView            = choiceView;
        ppc.newWorldView          = newWorldView;
        ppc.loadWorldView         = loadWorldView;
        ppc.savedWorldListContent = contentRT.transform;
        ppc.worldNameInput        = field;
        ppc.newWorldErrorText     = errorText.GetComponent<TextMeshProUGUI>();
        ppc.noSavesText           = noSavesGO.GetComponent<TextMeshProUGUI>();

        // ═════════════════════════════════════════════════════════════════════
        // SETTINGS PANEL
        // ═════════════════════════════════════════════════════════════════════
        GameObject settingsPanel = MakePanel(canvasGO, "SettingsPanel");
        settingsPanel.SetActive(false);

        GameObject tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(settingsPanel.transform, false);
        Anchor(tabBar, new Vector2(0f, 0.88f), new Vector2(0.45f, 1f));
        HorizontalLayoutGroup tabHLG = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHLG.childForceExpandWidth  = true;
        tabHLG.childForceExpandHeight = true;
        tabHLG.spacing = 4;

        Button audioTabBtn    = MakeButton(tabBar, "AudioTabButton",    "AUDIO").GetComponent<Button>();
        Button graphicsTabBtn = MakeButton(tabBar, "GraphicsTabButton", "GRAPHICS").GetComponent<Button>();
        Button keybindsTabBtn = MakeButton(tabBar, "KeybindsTabButton", "KEYBINDS").GetComponent<Button>();

        GameObject tabArea = new GameObject("TabArea");
        tabArea.transform.SetParent(settingsPanel.transform, false);
        Anchor(tabArea, new Vector2(0.0f, 0.08f), new Vector2(0.44f, 0.87f));

        GameObject audioTab = MakePanel(tabArea, "AudioTabContent");
        Anchor(audioTab, Vector2.zero, Vector2.one);
        VerticalLayoutGroup aVLG = audioTab.AddComponent<VerticalLayoutGroup>();
        aVLG.childAlignment = TextAnchor.UpperLeft;
        aVLG.spacing = 20; aVLG.padding = new RectOffset(10, 10, 10, 10);
        aVLG.childForceExpandWidth = true; aVLG.childForceExpandHeight = false;
        MakeSliderRow(audioTab, "MasterSlider", "Master Volume");
        MakeSliderRow(audioTab, "MusicSlider",  "Music Volume");
        MakeSliderRow(audioTab, "SFXSlider",    "SFX Volume");

        GameObject graphicsTab = MakePanel(tabArea, "GraphicsTabContent");
        Anchor(graphicsTab, Vector2.zero, Vector2.one);
        graphicsTab.SetActive(false);
        VerticalLayoutGroup gVLG = graphicsTab.AddComponent<VerticalLayoutGroup>();
        gVLG.childAlignment = TextAnchor.UpperLeft;
        gVLG.spacing = 8; gVLG.padding = new RectOffset(10, 10, 10, 10);
        gVLG.childForceExpandWidth = true; gVLG.childForceExpandHeight = false;
        MakeDropdownRow(graphicsTab, "ResolutionDropdown", "Resolution");
        MakeToggleRow(graphicsTab,   "FullscreenToggle",   "Fullscreen");
        MakeDropdownRow(graphicsTab, "QualityDropdown",    "Quality");
        MakeToggleRow(graphicsTab,   "VSyncToggle",        "VSync");

        GameObject keybindsTab = MakePanel(tabArea, "KeybindsTabContent");
        Anchor(keybindsTab, Vector2.zero, Vector2.one);
        keybindsTab.SetActive(false);
        VerticalLayoutGroup kVLG = keybindsTab.AddComponent<VerticalLayoutGroup>();
        kVLG.childAlignment = TextAnchor.UpperLeft;
        kVLG.spacing = 10; kVLG.padding = new RectOffset(10, 10, 10, 10);
        kVLG.childForceExpandWidth = true; kVLG.childForceExpandHeight = false;
        MakeKeybindRow(keybindsTab, "Jump",      "Space");
        MakeKeybindRow(keybindsTab, "Sprint",    "LeftShift");
        MakeKeybindRow(keybindsTab, "Interact",  "E");
        MakeKeybindRow(keybindsTab, "Inventory", "Tab");

        GameObject settingsButtons = MakeVerticalGroup(settingsPanel, "SettingsBottomButtons",
            new Vector2(0.05f, 0.01f), new Vector2(0.4f, 0.1f), 8);
        MakeButton(settingsButtons, "ApplyButton", "APPLY");
        MakeButton(settingsButtons, "BackButton",  "BACK");

        SettingsPanelController spc = settingsPanel.AddComponent<SettingsPanelController>();
        spc.audioTabContent    = audioTab;
        spc.graphicsTabContent = graphicsTab;
        spc.keybindsTabContent = keybindsTab;
        spc.audioTabButton     = audioTabBtn;
        spc.graphicsTabButton  = graphicsTabBtn;
        spc.keybindsTabButton  = keybindsTabBtn;

        // ═════════════════════════════════════════════════════════════════════
        // QUIT CONFIRM PANEL
        // ═════════════════════════════════════════════════════════════════════
        GameObject quitPanel = MakeImage(canvasGO, "QuitConfirmPanel", new Color(0.05f, 0.05f, 0.05f, 0.97f));
        Anchor(quitPanel, new Vector2(0.1f, 0.35f), new Vector2(0.4f, 0.65f));
        quitPanel.SetActive(false);
        VerticalLayoutGroup qVLG = quitPanel.AddComponent<VerticalLayoutGroup>();
        qVLG.childAlignment = TextAnchor.MiddleCenter;
        qVLG.spacing = 20; qVLG.padding = new RectOffset(20, 20, 20, 20);
        qVLG.childForceExpandWidth = false; qVLG.childForceExpandHeight = false;
        MakeLabel(quitPanel,  "QuitPromptText", "Are you sure you want to quit?");
        MakeButton(quitPanel, "YesButton",      "YES");
        MakeButton(quitPanel, "NoButton",       "NO");

        // ═════════════════════════════════════════════════════════════════════
        // WIRE REFERENCES
        // ═════════════════════════════════════════════════════════════════════
        MainMenuManager mmm = menuManagerGO.AddComponent<MainMenuManager>();
        mmm.mainPanel        = mainPanel;
        mmm.playPanel        = playPanel;
        mmm.settingsPanel    = settingsPanel;
        mmm.quitConfirmPanel = quitPanel;
        mmm.fadeOverlay      = fadeGroup;
        mmm.backgroundCamera = bgCam;

        // Camera viewport set directly on bgCam above

        // ── Wire all button OnClick events ────────────────────────────────────
        // Main
        WireButton(mainButtons,    "PlayButton",        mmm, "OnPlayClicked");
        WireButton(mainButtons,    "SettingsButton",    mmm, "OnSettingsClicked");
        WireButton(mainButtons,    "QuitButton",        mmm, "OnQuitClicked");
        // Quit confirm
        WireButton(quitPanel,      "YesButton",         mmm, "OnQuitConfirmed");
        WireButton(quitPanel,      "NoButton",          mmm, "OnQuitCancelled");
        // Play - choice
        WireButton(choiceButtons,  "NewWorldButton",    ppc, "ShowNewWorldView");
        WireButton(choiceButtons,  "LoadWorldButton",   ppc, "ShowLoadWorldView");
        WireButton(choiceButtons,  "BackButton",        mmm, "OnBackToMainClicked");
        // Play - new world
        WireButton(newWorldView, "CreateButton",      ppc, "OnCreateWorldConfirmed");
        WireButton(newWorldView, "BackButton",        ppc, "ShowChoiceView");
        // Play - load world
        WireButton(loadWorldView,  "BackButton",        ppc, "ShowChoiceView");
        // Settings tabs
        WireButton(tabBar,         "AudioTabButton",    spc, "ShowAudioTab");
        WireButton(tabBar,         "GraphicsTabButton", spc, "ShowGraphicsTab");
        WireButton(tabBar,         "KeybindsTabButton", spc, "ShowKeybindsTab");
        WireButton(settingsButtons,"ApplyButton",       spc, "OnApplySettings");
        WireButton(settingsButtons,"BackButton",        mmm, "OnBackToMainClicked");



        Undo.RegisterCreatedObjectUndo(canvasGO,      "Build Main Menu");
        Undo.RegisterCreatedObjectUndo(bgCamGO,       "Build Main Menu");
        Undo.RegisterCreatedObjectUndo(menuManagerGO, "Build Main Menu");

        Selection.activeGameObject = canvasGO;
        Debug.Log("[MainMenuHierarchy] Done. Assign your AudioMixer, gradient sprite, and SavedWorldEntry prefab.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Helpers

    static void WireButton(GameObject parent, string buttonName, MonoBehaviour target, string methodName)
    {
        Transform found = parent.transform.Find(buttonName);
        if (found == null)
        {
            // Try searching all children recursively as fallback
            found = FindDeep(parent.transform, buttonName);
        }
        if (found == null)
        {
            Debug.LogWarning($"[MainMenuHierarchy] Button not found: '{buttonName}' under '{parent.name}'");
            return;
        }

        Button btn = found.GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning($"[MainMenuHierarchy] No Button component on '{buttonName}'");
            return;
        }

        var method = target.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (method == null)
        {
            Debug.LogWarning($"[MainMenuHierarchy] Method not found: '{methodName}' on '{target.GetType().Name}'");
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

    static void DestroyExisting(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static GameObject MakePanel(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = false;
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

    static GameObject MakeVerticalGroup(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, int spacing)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Anchor(go, anchorMin, anchorMax);
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.spacing                = spacing;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(20, 20, 10, 10);
        ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220, 52);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.12f, 0.12f, 0.12f);
        cb.highlightedColor = new Color(0.25f, 0.25f, 0.25f);
        cb.pressedColor     = new Color(0.06f, 0.06f, 0.06f);
        btn.colors = cb;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.font      = _defaultFont;
        tmp.fontSize  = 20;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    static GameObject MakeLabel(GameObject parent, string name, string text)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.font      = _defaultFont;
        tmp.fontSize  = 17;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void MakeInputField(GameObject parent, string name, string placeholder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 52);
        go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 1f);

        TMP_InputField field = go.AddComponent<TMP_InputField>();

        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        RectTransform taRT = textArea.AddComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(8, 0); taRT.offsetMax = new Vector2(-8, 0);
        textArea.AddComponent<RectMask2D>();

        GameObject ph = new GameObject("Placeholder");
        ph.transform.SetParent(textArea.transform, false);
        RectTransform phRT = ph.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text      = placeholder;
        phTMP.font      = _defaultFont;
        phTMP.fontSize  = 16;
        phTMP.color     = new Color(0.5f, 0.5f, 0.5f);
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(textArea.transform, false);
        RectTransform txtRT = txt.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.font      = _defaultFont;
        txtTMP.fontSize  = 16;
        txtTMP.color     = Color.white;
        txtTMP.alignment = TextAlignmentOptions.MidlineLeft;

        field.textViewport  = taRT;
        field.textComponent = txtTMP;
        field.placeholder   = phTMP;
    }

    static void MakeSliderRow(GameObject parent, string name, string label)
    {
        GameObject row = new GameObject(name + "_Row");
        row.transform.SetParent(parent.transform, false);
        RectTransform rRT = row.AddComponent<RectTransform>();
        rRT.sizeDelta = new Vector2(0, 40);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true; hlg.spacing = 12;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        lbl.AddComponent<RectTransform>().sizeDelta = new Vector2(140, 0);
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = label; lblT.font = _defaultFont; lblT.fontSize = 15;
        lblT.color = Color.white; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(row.transform, false);
        sliderGO.AddComponent<RectTransform>().sizeDelta = new Vector2(150, 0);
        Slider s = sliderGO.AddComponent<Slider>();
        s.minValue = 0f; s.maxValue = 1f; s.value = 0.75f;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform, false);
        RectTransform bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f); bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f); faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.offsetMin = new Vector2(5, 0); faRT.offsetMax = new Vector2(-5, 0);
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRT = fill.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(0.75f, 1);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.6f, 1f);
        s.fillRect = fRT;

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform haRT = handleArea.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, 0); haRT.offsetMax = new Vector2(-10, 0);
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        handle.AddComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        s.handleRect = handle.GetComponent<RectTransform>();
        s.targetGraphic = handleImg;

        GameObject valGO = new GameObject(name + "_Val");
        valGO.transform.SetParent(row.transform, false);
        valGO.AddComponent<RectTransform>().sizeDelta = new Vector2(45, 0);
        TextMeshProUGUI valT = valGO.AddComponent<TextMeshProUGUI>();
        valT.text = "75%"; valT.font = _defaultFont; valT.fontSize = 14;
        valT.color = Color.white; valT.alignment = TextAlignmentOptions.MidlineLeft;
    }

    static void MakeDropdownRow(GameObject parent, string name, string label)
    {
        GameObject row = new GameObject(name + "_Row");
        row.transform.SetParent(parent.transform, false);
        RectTransform rRT = row.AddComponent<RectTransform>();
        rRT.sizeDelta = new Vector2(0, 30);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 30; rowLE.preferredHeight = 30;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false; hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Label
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        LayoutElement lblLE = lbl.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 100; lblLE.preferredHeight = 28; lblLE.flexibleWidth = 0;
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = label; lblT.font = _defaultFont; lblT.fontSize = 13;
        lblT.color = Color.white; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        // Dropdown
        var go = new GameObject(name);
        go.transform.SetParent(row.transform, false);
        LayoutElement ddLE = go.AddComponent<LayoutElement>();
        ddLE.preferredWidth = 140; ddLE.preferredHeight = 28; ddLE.minWidth = 100; ddLE.flexibleWidth = 1;

        // Add the required components exactly as Unity does internally
        go.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f);
        TMP_Dropdown dd = go.AddComponent<TMP_Dropdown>();

        // Caption Label
        var captionGO = new GameObject("Label");
        captionGO.transform.SetParent(go.transform, false);
        var captionRT = captionGO.AddComponent<RectTransform>();
        captionRT.anchorMin = Vector2.zero; captionRT.anchorMax = Vector2.one;
        captionRT.offsetMin = new Vector2(10, 6); captionRT.offsetMax = new Vector2(-25, -7);
        var captionTMP = captionGO.AddComponent<TextMeshProUGUI>();
        captionTMP.font = _defaultFont; captionTMP.fontSize = 14;
        captionTMP.color = Color.white;
        captionTMP.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText = captionTMP;

        // Arrow
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f); arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.sizeDelta = new Vector2(20, 20);
        arrowRT.anchoredPosition = new Vector2(-15, 0);
        arrowGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        // Template
        var templateGO = new GameObject("Template");
        templateGO.transform.SetParent(go.transform, false);
        var templateRT = templateGO.AddComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = Vector2.zero;
        templateRT.sizeDelta = new Vector2(0, 90);
        templateGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
        var templateSR = templateGO.AddComponent<ScrollRect>();
        templateSR.horizontal = false;

        // Viewport
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(templateGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = new Vector2(-18, 0); vpRT.pivot = new Vector2(0, 1);
        vpGO.AddComponent<Image>();
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        templateSR.viewport = vpRT;

        // Content
        var contentGO2 = new GameObject("Content");
        contentGO2.transform.SetParent(vpGO.transform, false);
        var contentRT2 = contentGO2.AddComponent<RectTransform>();
        contentRT2.anchorMin = new Vector2(0, 1); contentRT2.anchorMax = new Vector2(1, 1);
        contentRT2.pivot = new Vector2(0.5f, 1f);
        contentRT2.anchoredPosition = Vector2.zero;
        contentRT2.sizeDelta = new Vector2(0, 28);
        templateSR.content = contentRT2;

        // Item
        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(contentGO2.transform, false);
        var itemRT = itemGO.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f); itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 25);
        var itemToggle = itemGO.AddComponent<Toggle>();

        var itemBgGO = new GameObject("Item Background");
        itemBgGO.transform.SetParent(itemGO.transform, false);
        var itemBgRT = itemBgGO.AddComponent<RectTransform>();
        itemBgRT.anchorMin = Vector2.zero; itemBgRT.anchorMax = Vector2.one;
        itemBgRT.offsetMin = itemBgRT.offsetMax = Vector2.zero;
        var itemBgImg = itemBgGO.AddComponent<Image>();
        itemBgImg.color = new Color(0.18f, 0.18f, 0.18f);

        var itemCheckGO = new GameObject("Item Checkmark");
        itemCheckGO.transform.SetParent(itemGO.transform, false);
        var itemCheckRT = itemCheckGO.AddComponent<RectTransform>();
        itemCheckRT.anchorMin = new Vector2(0, 0.5f); itemCheckRT.anchorMax = new Vector2(0, 0.5f);
        itemCheckRT.sizeDelta = new Vector2(20, 20); itemCheckRT.anchoredPosition = new Vector2(10, 0);
        var itemCheckImg = itemCheckGO.AddComponent<Image>();
        itemCheckImg.color = Color.white;

        var itemLblGO = new GameObject("Item Label");
        itemLblGO.transform.SetParent(itemGO.transform, false);
        var itemLblRT = itemLblGO.AddComponent<RectTransform>();
        itemLblRT.anchorMin = Vector2.zero; itemLblRT.anchorMax = Vector2.one;
        itemLblRT.offsetMin = new Vector2(20, 1); itemLblRT.offsetMax = new Vector2(-10, -2);
        var itemLblTMP = itemLblGO.AddComponent<TextMeshProUGUI>();
        itemLblTMP.font = _defaultFont; itemLblTMP.fontSize = 14;
        itemLblTMP.color = Color.white;
        itemLblTMP.alignment = TextAlignmentOptions.MidlineLeft;

        itemToggle.targetGraphic = itemBgImg;
        itemToggle.graphic = itemCheckImg;
        dd.itemText = itemLblTMP;
        dd.template = templateRT;

        templateGO.SetActive(false);

        dd.ClearOptions();
        dd.options.Add(new TMP_Dropdown.OptionData("Option A"));
        dd.options.Add(new TMP_Dropdown.OptionData("Option B"));
        dd.RefreshShownValue();
    }

    static void MakeToggleRow(GameObject parent, string name, string label)
    {
        GameObject row = new GameObject(name + "_Row");
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 30; rowLE.preferredHeight = 30;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        hlg.spacing = 12; hlg.childAlignment = TextAnchor.MiddleLeft;

        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        LayoutElement lblLE = lbl.AddComponent<LayoutElement>();
        lblLE.preferredWidth = 140; lblLE.flexibleWidth = 0;
        lblLE.preferredHeight = 30;
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = label; lblT.font = _defaultFont; lblT.fontSize = 15;
        lblT.color = Color.white; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject togGO = new GameObject(name);
        togGO.transform.SetParent(row.transform, false);
        RectTransform togRT = togGO.AddComponent<RectTransform>();
        togRT.sizeDelta = new Vector2(24, 24);
        LayoutElement togLE = togGO.AddComponent<LayoutElement>();
        togLE.preferredWidth = 24; togLE.preferredHeight = 24;
        togLE.flexibleWidth = 0;
        Image togBg = togGO.AddComponent<Image>();
        togBg.color = new Color(0.25f, 0.25f, 0.25f);
        Toggle tog = togGO.AddComponent<Toggle>();

        GameObject check = new GameObject("Checkmark");
        check.transform.SetParent(togGO.transform, false);
        RectTransform cRT = check.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.15f, 0.15f);
        cRT.anchorMax = new Vector2(0.85f, 0.85f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.6f, 1f);
        tog.targetGraphic = togBg;
        tog.graphic = checkImg;
        tog.isOn = false;
    }

    static void MakeKeybindRow(GameObject parent, string actionName, string defaultKey)
    {
        GameObject row = new GameObject(actionName + "_Row");
        row.transform.SetParent(parent.transform, false);
        row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 44);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.spacing = 10; hlg.padding = new RectOffset(4, 4, 4, 4);

        GameObject lbl = new GameObject("ActionLabel");
        lbl.transform.SetParent(row.transform, false);
        lbl.AddComponent<RectTransform>().sizeDelta = new Vector2(120, 0);
        TextMeshProUGUI lblT = lbl.AddComponent<TextMeshProUGUI>();
        lblT.text = actionName; lblT.font = _defaultFont; lblT.fontSize = 15;
        lblT.color = Color.white; lblT.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject keyLbl = new GameObject("KeyLabel");
        keyLbl.transform.SetParent(row.transform, false);
        keyLbl.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 0);
        TextMeshProUGUI keyT = keyLbl.AddComponent<TextMeshProUGUI>();
        keyT.text = defaultKey; keyT.font = _defaultFont; keyT.fontSize = 15;
        keyT.color = new Color(0.8f, 0.8f, 0.2f); keyT.alignment = TextAlignmentOptions.Center;

        GameObject rebindGO = MakeButton(row, "RebindButton", "REBIND");
        rebindGO.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 36);

        KeybindRow kr = row.AddComponent<KeybindRow>();
        kr.actionName   = actionName;
        kr.defaultKey   = defaultKey;
        kr.actionLabel  = lblT;
        kr.keyLabel     = keyT;
        kr.rebindButton = rebindGO.GetComponent<Button>();
    }

    static void Anchor(GameObject go, Vector2 min, Vector2 max)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    #endregion
}
#endif