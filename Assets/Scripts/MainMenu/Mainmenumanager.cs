using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// MainMenuManager: Core controller for the main menu
/// Attach to a GameObject named "MenuManager" in your Main Menu scene to work.
/// 
/// SCENE SETUP REQUIRED
/// - Canvas (Screen Space - Camera) with a Camera reference
/// - A Camera in the scene that can pan around to show the background
/// - All panel references assigned in the inspector
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    // ── Panel References ──────────────────────────────────────────────────────
    [Header("Panels")]
    [Tooltip("Root panel containing PLay/Settings/Quit buttons")]
    public GameObject mainPanel;

    [Tooltip("Panel Shown when Play is clicked (New World / Load World)")]
    public GameObject playPanel;

    [Tooltip("Panel shown when Settings is clicked")]
    public GameObject settingsPanel;

    [Tooltip("Confirmation popup for qutting")]
    public GameObject quitConfirmPanel;

    // ── Fade Overlay ──────────────────────────────────────────────────────────
    [Header("Fade Overlay")]
    [Tooltip("CanvasGroup on a full-screen Image used to fade between panels")]
    public CanvasGroup fadeOverlay;

    [Tooltip("Seconds the cross-fade takes")]
    public float fadeDuration = 0.3f;

    // ── Left-side Gradient ────────────────────────────────────────────────────
    [Header("Side Gradient")]
    [Tooltip("Image that covers the left half and fades to transparent on the right." + "Use a sprite that is opaque black on the left edge and alpha-0 on the right edge")]
    public Image sideGradientImage;

    // ── Camera Background ─────────────────────────────────────────────────────
    [Header("Background Camera")]
    [Tooltip("The scene camera whose view is shown on the right side of the screen")]
    public Camera backgroundCamera;

    [Tooltip("Optional: RawImage that renders the camera's RenderTexture (if used)")]
    public RawImage backgroundRawImage;

    // ── Internal State ────────────────────────────────────────────────────────
    private GameObject _activePanel;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Start()
    {
        // Ensure only the main panel is visible at startup
        ShowPanelImmediate(mainPanel);

        // Hide the fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public Button Callbacks

    // ── Main Menu ─────────────────────────────────────────────────────────────
    
    /// <summary>Called by the Play button.</summary>
    public void OnPlayClicked()
    {
        StartCoroutine(TransitionToPanel(playPanel));
    }
    
    /// <summary>Called by the Settings button.</summary>
    public void OnSettingsClicked()
    {
        StartCoroutine(TransitionToPanel(settingsPanel));
    }

    /// <summary>Called by the Quit button.</summary>
    public void OnQuitClicked()
    {
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(true);
    }

    // ── Quit Confirmation ──────────────────────────────────────────────────────────

    /// <summary>Called by "Yes" in the quit confirmation popup.</summary>
    public void OnQuitConfirmed()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>Called by "No / Cancel" in the quit confirmation popup.</summary>
    public void OnQuitCancelled()
    {
        if (quitConfirmPanel != null)
            quitConfirmPanel.SetActive(false);
    }

    // ── Back Buttons ──────────────────────────────────────────────────────────

    /// <summary>Generic "Back to Main" button used by sub-panels.</summary>
    public void OnBackToMainClicked()
    {
        StartCoroutine(TransitionToPanel(mainPanel));
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Panel Transitions

    private void ShowPanelImmediate(GameObject panel)
    {
        HideAllPanels();
        if (panel != null)
        {
            panel.SetActive(true);
            _activePanel = panel;
        }
    }

    private IEnumerator TransitionToPanel(GameObject targetPanel)
    {
        if (targetPanel == _activePanel) yield break;

        // Fade Out
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeDuration));
        }

        HideAllPanels();
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            _activePanel = targetPanel;
        }

        // Fade back in
        if (fadeOverlay != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 1f, 0f, fadeDuration));
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    private void HideAllPanels()
    {
        SetActive(mainPanel, false);
        SetActive(playPanel, false);
        SetActive(settingsPanel, false);
        // Keep quitConfirmPanel managed sparately because it overlays
    }

    private static void SetActive(GameObject go, bool state)
    {
        if (go != null) go.SetActive(state);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    #endregion
}