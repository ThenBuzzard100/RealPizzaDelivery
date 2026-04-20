using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Tracks whether the player has seen the intro before using PlayerPrefs.
/// First launch  → intro is completely unskippable.
/// Later launches → skip unlocks after skipPromptDelay seconds.
/// On finish, plays a pizza-spin transition then loads the next scene.
/// </summary>
public class IntroSkipManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Auto-found if left empty.")]
    [SerializeField] private IntroCameraManager introCameraManager;

    [Header("Skip Settings")]
    [SerializeField] private string seenIntroKey   = "HasSeenIntro";
    [SerializeField] private KeyCode skipKey        = KeyCode.Space;
    [SerializeField] private float skipPromptDelay  = 3f;

    [Header("Pizza Transition")]
    [SerializeField] private float pizzaGrowDuration   = 1.0f;
    [SerializeField] private float pizzaSpinRotations  = 2.5f;
    [SerializeField] private float pizzaHoldDuration   = 0.3f;
    [SerializeField] private float pizzaShrinkDuration = 0.8f;
    [SerializeField] private Color pizzaTint = new Color(0.96f, 0.72f, 0.18f);

    [Header("Events")]
    public UnityEvent onSkipAllowed;
    public UnityEvent onIntroFinished;

    // ── Public State ───────────────────────────────────────────

    public bool IsFirstPlay   { get; private set; }
    public bool SkipAllowed   { get; private set; }
    public bool IntroFinished { get; private set; }

    // ── Private ────────────────────────────────────────────────

    private float elapsed = 0f;

    // ── Public API ─────────────────────────────────────────────

    public void SkipIntro()
    {
        if (!SkipAllowed || IntroFinished) return;
        if (introCameraManager != null)
            introCameraManager.TeleportToProgress(1f);
        FinishIntro();
    }

    // ── Lifecycle ──────────────────────────────────────────────

    private void Awake()
    {
        if (introCameraManager == null)
            introCameraManager = FindObjectOfType<IntroCameraManager>();

        IsFirstPlay = !PlayerPrefs.HasKey(seenIntroKey);
        if (IsFirstPlay)
        {
            PlayerPrefs.SetInt(seenIntroKey, 1);
            PlayerPrefs.Save();
            Debug.Log("[IntroSkipManager] First play — intro locked.");
        }
        else
        {
            Debug.Log("[IntroSkipManager] Repeat visit — skip will unlock after delay.");
        }
    }

    private void Update()
    {
        if (IntroFinished) return;

        if (introCameraManager != null && !introCameraManager.IsPlaying)
        {
            FinishIntro();
            return;
        }

        if (IsFirstPlay) return;

        if (!SkipAllowed)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= skipPromptDelay)
            {
                SkipAllowed = true;
                onSkipAllowed?.Invoke();
                Debug.Log("[IntroSkipManager] Skip unlocked.");
            }
        }

        if (SkipAllowed && Input.GetKeyDown(skipKey))
            SkipIntro();
    }

    // ── Finish ─────────────────────────────────────────────────

    private void FinishIntro()
    {
        if (IntroFinished) return;
        IntroFinished = true;
        onIntroFinished?.Invoke();
        Debug.Log("[IntroSkipManager] Intro complete — starting pizza transition.");

        // Build the persistent runner FIRST, then start the coroutine on it.
        // This means the coroutine survives the scene load destroying this GameObject.
        var runner = new GameObject("PizzaTransitionRunner");
        DontDestroyOnLoad(runner);
        var helper = runner.AddComponent<PizzaTransitionHelper>();
        helper.Begin(
            pizzaGrowDuration,
            pizzaSpinRotations,
            pizzaHoldDuration,
            pizzaShrinkDuration,
            pizzaTint
        );
    }

    // ── Editor Utility ─────────────────────────────────────────

    [ContextMenu("Reset Seen Flag (re-test first play)")]
    public void ResetSeenFlag()
    {
        PlayerPrefs.DeleteKey(seenIntroKey);
        PlayerPrefs.Save();
        Debug.Log("[IntroSkipManager] Flag cleared — next play is treated as first play.");
    }
}


/// <summary>
/// Spawned at transition time and marked DontDestroyOnLoad so its coroutine
/// keeps running across the scene boundary. Self-destructs when done.
/// </summary>
public class PizzaTransitionHelper : MonoBehaviour
{
    public void Begin(float growDur, float spinRots, float holdDur, float shrinkDur, Color tint)
    {
        StartCoroutine(Run(growDur, spinRots, holdDur, shrinkDur, tint));
    }

    private IEnumerator Run(float growDur, float spinRots, float holdDur, float shrinkDur, Color tint)
    {
        // ── Build canvas (also DontDestroyOnLoad via parenting) ──
        var canvasGO = new GameObject("PizzaTransitionCanvas");
        DontDestroyOnLoad(canvasGO);

        var canvas          = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Black background
        var bgGO  = new GameObject("PizzaBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bg    = bgGO.AddComponent<UnityEngine.UI.Image>();
        bg.color  = new Color(0f, 0f, 0f, 0f);
        StretchToFill(bgGO.GetComponent<RectTransform>());

        // Pizza disc
        var pizzaGO = new GameObject("PizzaDisc");
        pizzaGO.transform.SetParent(canvasGO.transform, false);
        var pizzaRT       = pizzaGO.AddComponent<RectTransform>();
        var pizzaBase     = pizzaGO.AddComponent<UnityEngine.UI.Image>();
        pizzaBase.color   = tint;

        // Sauce
        MakeChildCircle(pizzaGO, "Sauce",  new Color(0.82f, 0.15f, 0.10f), 0.72f, Vector2.zero);
        // Cheese
        MakeChildCircle(pizzaGO, "Cheese", new Color(0.97f, 0.88f, 0.45f), 0.60f, Vector2.zero);
        // Pepperoni ring
        for (int i = 0; i < 8; i++)
        {
            float a  = i * 45f * Mathf.Deg2Rad;
            var pep  = MakeChildCircle(pizzaGO, $"Pep_{i}", new Color(0.60f, 0.10f, 0.08f), 0.13f,
                           new Vector2(Mathf.Cos(a) * 55f, Mathf.Sin(a) * 55f));
        }
        // Centre olive
        MakeChildCircle(pizzaGO, "Olive", new Color(0.15f, 0.15f, 0.12f), 0.10f, Vector2.zero);

        // Crust slice lines
        for (int i = 0; i < 8; i++)
        {
            var lineGO        = new GameObject($"CrustLine_{i}");
            lineGO.transform.SetParent(pizzaGO.transform, false);
            var lineImg       = lineGO.AddComponent<UnityEngine.UI.Image>();
            lineImg.color     = new Color(0.60f, 0.38f, 0.10f, 0.45f);
            var lineRT        = lineGO.GetComponent<RectTransform>();
            lineRT.sizeDelta  = new Vector2(4f, 90f);
            lineRT.pivot      = new Vector2(0.5f, 0f);
            lineRT.anchoredPosition = Vector2.zero;
            lineGO.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
        }

        float screenDiag = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        float targetSize = screenDiag * 1.1f;
        float startSize  = 160f;

        pizzaRT.sizeDelta               = Vector2.one * startSize;
        pizzaGO.transform.localPosition = Vector3.zero;

        // ── Phase 1: Grow & spin ──────────────────────────────
        float t = 0f;
        while (t < growDur)
        {
            t += Time.unscaledDeltaTime;
            float ease         = EaseInOutCubic(Mathf.Clamp01(t / growDur));
            pizzaRT.sizeDelta  = Vector2.one * Mathf.Lerp(startSize, targetSize, ease);
            pizzaGO.transform.localRotation = Quaternion.Euler(0f, 0f, spinRots * 360f * ease);
            bg.color           = new Color(0f, 0f, 0f, ease);
            yield return null;
        }

        pizzaRT.sizeDelta = Vector2.one * targetSize;
        bg.color          = Color.black;

        // ── Phase 2: Load scene while holding on pizza ────────
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            // Start async load but DON'T hold it back — let it activate naturally.
            // We wait for it to finish before shrinking.
            var load = SceneManager.LoadSceneAsync(nextIndex);

            // Wait for the hold duration AND for the scene to be done loading.
            float held = 0f;
            while (!load.isDone || held < holdDur)
            {
                held += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("[PizzaTransitionHelper] No next scene in Build Settings!");
            yield return new WaitForSecondsRealtime(holdDur);
        }

        // ── Phase 3: Shrink away on the new scene ─────────────
        t = 0f;
        while (t < shrinkDur)
        {
            t += Time.unscaledDeltaTime;
            float ease        = EaseInCubic(Mathf.Clamp01(t / shrinkDur));
            pizzaRT.sizeDelta = Vector2.one * Mathf.Lerp(targetSize, 0f, ease);
            pizzaGO.transform.localRotation = Quaternion.Euler(0f, 0f, spinRots * 360f - ease * 180f);
            bg.color          = new Color(0f, 0f, 0f, 1f - ease);
            yield return null;
        }

        Destroy(canvasGO);
        Destroy(gameObject);
    }

    // ── Helpers ────────────────────────────────────────────────

    private static UnityEngine.UI.Image MakeChildCircle(GameObject parent, string name, Color color, float sizeFraction, Vector2 anchoredPos)
    {
        var go              = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img             = go.AddComponent<UnityEngine.UI.Image>();
        img.color           = color;
        var rt              = go.GetComponent<RectTransform>();
        rt.sizeDelta        = Vector2.one * (200f * sizeFraction);
        rt.anchoredPosition = anchoredPos;
        return img;
    }

    private static void StretchToFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    private static float EaseInCubic(float t) => t * t * t;
}