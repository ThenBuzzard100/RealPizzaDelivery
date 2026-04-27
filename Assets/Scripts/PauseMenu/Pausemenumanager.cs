using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Steamworks;

/// <summary>
/// PauseMenuManager: Handles Escape key, pause logic, and pause canvas.
/// Requires Mirror and Steamworks.NET (FizzySteamworks transport).
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("Pause UI")]
    public Canvas     pauseCanvas;
    public GameObject pauseMainPanel;
    public GameObject settingsPanel;
    public GameObject hostPanel;
    public GameObject returnConfirmPanel;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Cursor")]
    public bool manageCursor = true;

    private bool       _isPaused       = false;
    private bool       _isMultiplayer  = false;
    private GameObject _activeSubPanel = null;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        SetCanvasVisible(false);
        Hide(pauseMainPanel);
        Hide(settingsPanel);
        Hide(hostPanel);
        Hide(returnConfirmPanel);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
            {
                if (_activeSubPanel != null)
                    ShowMainPanel();
                else
                    Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────

    public void Pause()
    {
        _isPaused      = true;
        _isMultiplayer = IsMultiplayerActive();

        // Only freeze time in single player
        if (!_isMultiplayer)
            Time.timeScale = 0f;

        SetCanvasVisible(true);
        ShowMainPanel();

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    public void Resume()
    {
        _isPaused       = false;
        _activeSubPanel = null;
        Time.timeScale  = 1f;

        SetCanvasVisible(false);
        Hide(pauseMainPanel);
        Hide(settingsPanel);
        Hide(hostPanel);
        Hide(returnConfirmPanel);

        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }

    // ── Button Callbacks ──────────────────────────────────────────────────────

    public void OnResumeClicked()            { Resume(); }
    public void OnSettingsClicked()          { ShowSubPanel(settingsPanel); }
    public void OnHostClicked()              { ShowSubPanel(hostPanel); }
    public void OnReturnToMainMenuClicked()  { ShowSubPanel(returnConfirmPanel); }
    public void OnBackClicked()              { ShowMainPanel(); }
    public void OnReturnConfirmed()          { StartCoroutine(LoadMainMenu()); }
    public void OnReturnCancelled()          { ShowMainPanel(); }

    public void OnStartHostClicked()
    {
        NetworkManager nm = NetworkManager.singleton;
        if (nm == null)
        {
            Debug.LogError("[PauseMenu] No NetworkManager found.");
            return;
        }

        // If already a client, stop before hosting
        if (NetworkClient.isConnected && !NetworkServer.active)
            nm.StopClient();

        nm.StartHost();

        // Open Steam friends overlay so you can send invites
        if (SteamManager.Initialized)
        {
            SteamFriends.ActivateGameOverlay("Friends");
            Debug.Log("[PauseMenu] Steam overlay opened — invite friends.");
        }

        Debug.Log("[PauseMenu] Started as Mirror host.");
        ShowMainPanel();
    }

    public void OnJoinAsClientClicked()
    {
        // Joining is handled via Steam invite — just notify and close
        if (SteamManager.Initialized)
            SteamFriends.ActivateGameOverlay("Friends");

        Debug.Log("[PauseMenu] Waiting for Steam invite to join as client.");
        ShowMainPanel();
    }

    // ── Panel Helpers ─────────────────────────────────────────────────────────

    private void ShowMainPanel()
    {
        Show(pauseMainPanel);
        Hide(settingsPanel);
        Hide(hostPanel);
        Hide(returnConfirmPanel);
        _activeSubPanel = null;
    }

    private void ShowSubPanel(GameObject panel)
    {
        Hide(pauseMainPanel);
        Hide(settingsPanel);
        Hide(hostPanel);
        Hide(returnConfirmPanel);
        Show(panel);
        _activeSubPanel = panel;
    }

    private void Show(GameObject go)          { if (go != null) go.SetActive(true);  }
    private void Hide(GameObject go)          { if (go != null) go.SetActive(false); }
    private void SetCanvasVisible(bool v)     { if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(v); }

    // ── Multiplayer Check ─────────────────────────────────────────────────────

    private bool IsMultiplayerActive()
    {
        // Host or server with at least one remote connection
        if (NetworkServer.active)
            return NetworkServer.connections.Count > 1;

        // Connected as a client
        if (NetworkClient.isConnected)
            return true;

        return false;
    }

    // ── Scene Load ────────────────────────────────────────────────────────────

    private IEnumerator LoadMainMenu()
    {
        Time.timeScale = 1f;

        // Cleanly shut down Mirror session
        NetworkManager nm = NetworkManager.singleton;
        if (nm != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
                nm.StopHost();
            else if (NetworkServer.active)
                nm.StopServer();
            else if (NetworkClient.isConnected)
                nm.StopClient();
        }

        // Wait for Mirror to clean up
        yield return new WaitForSecondsRealtime(0.2f);

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy() { Time.timeScale = 1f; }
}