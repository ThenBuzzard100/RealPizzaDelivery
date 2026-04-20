using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Displays a "Press SPACE to Skip" prompt during the intro on repeat visits.
/// When the intro ends the prompt immediately hides — the pizza transition
/// takes over the screen from that point.
/// </summary>
public class IntroSkipUI : MonoBehaviour
{
    [Header("References (auto-found if blank)")]
    [SerializeField] private IntroSkipManager skipManager;

    [Header("Prompt Text")]
    [SerializeField] private string promptText    = "Press SPACE to Skip";
    [SerializeField] private string firstPlayText = "";

    [Header("Style")]
    [SerializeField] private Color promptColor   = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private int   fontSize      = 22;
    [SerializeField] private float fadeInDuration = 0.6f;

    // Runtime
    private GameObject  canvasRoot;
    private CanvasGroup canvasGroup;
    private Text        label;
    private Coroutine   activeFade;
    private bool        skipWasAllowed = false;

    // ── Lifecycle ──────────────────────────────────────────────

    private void Awake()
    {
        if (skipManager == null)
            skipManager = FindObjectOfType<IntroSkipManager>();

        BuildUI();
        HideImmediate();
    }

    private void Start()
    {
        if (skipManager == null)
        {
            Debug.LogWarning("[IntroSkipUI] No IntroSkipManager found.");
            return;
        }

        // Hide instantly the moment the intro ends — no fade race with pizza
        skipManager.onIntroFinished.AddListener(HideImmediate);

        if (skipManager.IsFirstPlay && !string.IsNullOrEmpty(firstPlayText))
        {
            label.text = firstPlayText;
            SwapFade(FadeIn());
        }
    }

    private void Update()
    {
        if (skipManager == null) return;

        if (!skipWasAllowed && skipManager.SkipAllowed)
        {
            skipWasAllowed = true;
            label.text     = promptText;
            SwapFade(FadeIn());
        }
    }

    private void OnDestroy()
    {
        if (skipManager != null)
            skipManager.onIntroFinished.RemoveListener(HideImmediate);
    }

    // ── Callbacks ──────────────────────────────────────────────

    private void OnSkipClicked() => skipManager?.SkipIntro();

    // ── Fade Control ───────────────────────────────────────────

    private void SwapFade(IEnumerator newFade)
    {
        if (activeFade != null) StopCoroutine(activeFade);
        activeFade = StartCoroutine(newFade);
    }

    private void HideImmediate()
    {
        // Stop any running fade and snap to invisible right now
        if (activeFade != null)
        {
            StopCoroutine(activeFade);
            activeFade = null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.interactable   = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;

        float start = canvasGroup.alpha;
        float t     = 0f;
        while (t < fadeInDuration)
        {
            // Stop if intro already finished while we were fading in
            if (skipManager != null && skipManager.IntroFinished)
            {
                HideImmediate();
                yield break;
            }
            t                += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 1f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        activeFade        = null;
    }

    // ── UI Construction ────────────────────────────────────────

    private void BuildUI()
    {
        canvasRoot = new GameObject("IntroSkipCanvas");
        canvasRoot.transform.SetParent(transform);

        var canvas          = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler                 = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasRoot.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasRoot.AddComponent<CanvasGroup>();

        // Panel — bottom-right
        var panel = new GameObject("SkipPanel");
        panel.transform.SetParent(canvasRoot.transform, false);

        var rt              = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-30f, 30f);
        rt.sizeDelta        = new Vector2(280f, 56f);

        var bg   = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);

        var btn           = panel.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(OnSkipClicked);

        var labelGO = new GameObject("SkipLabel");
        labelGO.transform.SetParent(panel.transform, false);

        var labelRT          = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin    = Vector2.zero;
        labelRT.anchorMax    = Vector2.one;
        labelRT.offsetMin    = new Vector2(12f, 0f);
        labelRT.offsetMax    = new Vector2(-12f, 0f);

        label           = labelGO.AddComponent<Text>();
        label.text      = promptText;
        label.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize  = fontSize;
        label.color     = promptColor;
        label.alignment = TextAnchor.MiddleCenter;
    }
}