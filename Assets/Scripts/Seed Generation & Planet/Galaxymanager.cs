using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GalaxyManager: Singleton. Source of truth for the entire game's seed system.
/// Place this on a persistent GameObject in your bootstrap/main-menu scene.
/// Call GalaxyManager.Instance.TravelToPlanet() to begin a transition.
/// </summary>
public class GalaxyManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GalaxyManager Instance { get; private set; }

    // ── Master Seed ───────────────────────────────────────────────────────────
    /// <summary>
    /// The 64-bit master seed for the entire session.
    /// Set this from the main menu (new game = random, load game = saved value).
    /// </summary>
    [Header("Master Seed")]
    public long masterSeed = 0L;

    /// <summary>
    /// The planet the player is currently on or travelling to.
    /// </summary>
    [Header("Current State")]
    public PlanetType currentPlanet = PlanetType.Earth;

    // ── Scene Names ───────────────────────────────────────────────────────────
    [Header("Scene Names (must match Build Settings)")]
    public string transitPodSceneName  = "TransitPod";
    public string earthSceneName       = "Earth";
    public string moonSceneName        = "Moon";

    // ── Internal ──────────────────────────────────────────────────────────────
    /// <summary>The sub-seed for the destination planet, passed to TransitEmergencyManager.</summary>
    public long CurrentPlanetSubSeed { get; private set; }

    /// <summary>Landing offset in world units. Set by TransitEmergencyManager based on repair success.</summary>
    public Vector3 LandingOffset { get; set; } = Vector3.zero;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Standard singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Call from the main menu with the seed the player chose/loaded.
    /// </summary>
    public void InitialiseSeed(long seed)
    {
        masterSeed = seed;
        Debug.Log($"[GalaxyManager] Master seed set: {masterSeed}");
    }

    /// <summary>
    /// Derives a deterministic, unique sub-seed for a given planet from the master seed.
    /// Same master seed always produces the same planet sub-seed.
    /// Uses a simple but effective hash combining the master seed and planet index.
    /// </summary>
    public long DerivePlanetSubSeed(PlanetType planet)
    {
        // Mix master seed with planet enum value using prime multipliers
        long planetIndex = (long)planet + 1L;
        long hash = masterSeed;
        hash ^= hash << 13;
        hash ^= hash >> 7;
        hash ^= hash << 17;
        hash *= 6364136223846793005L + (planetIndex * 1442695040888963407L);
        hash ^= hash >> 33;
        hash *= unchecked((long)0xff51afd7ed558ccdL);
        hash ^= hash >> 33;
        hash *= unchecked((long)0xc4ceb9fe1a85ec53L);
        hash ^= hash >> 33;
        return hash;
    }

    /// <summary>
    /// Begins the full travel sequence.
    /// Earth: loads directly — no transit pod (initial game load from main menu).
    /// Moon: loads TransitPod scene first, then Moon in background.
    /// </summary>
    public void TravelToPlanet(PlanetType destination)
    {
        currentPlanet        = destination;
        CurrentPlanetSubSeed = DerivePlanetSubSeed(destination);
        LandingOffset        = Vector3.zero;

        Debug.Log($"[GalaxyManager] Travelling to {destination}. Sub-seed: {CurrentPlanetSubSeed}");

        if (destination == PlanetType.Moon)
            StartCoroutine(LoadTransitPodThenPlanet(destination));
        else
            StartCoroutine(LoadPlanetDirectly(destination));
    }

    /// <summary>
    /// Called by TransitEmergencyManager when the pod minigame is finished.
    /// Loads the actual planet scene.
    /// </summary>
    public void ArriveAtPlanet()
    {
        string sceneName = currentPlanet == PlanetType.Moon ? moonSceneName : earthSceneName;
        StartCoroutine(LoadPlanetScene(sceneName));
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Private Coroutines

    private IEnumerator LoadPlanetDirectly(PlanetType destination)
    {
        string sceneName = destination == PlanetType.Moon ? moonSceneName : earthSceneName;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        Debug.Log($"[GalaxyManager] Arrived directly at {destination}.");
    }

    private IEnumerator LoadTransitPodThenPlanet(PlanetType destination)
    {
        // Load transit pod scene
        AsyncOperation podLoad = SceneManager.LoadSceneAsync(transitPodSceneName);
        podLoad.allowSceneActivation = true;
        while (!podLoad.isDone) yield return null;

        // Planet scene loads in the background while the pod plays
        string planetScene = destination == PlanetType.Moon ? moonSceneName : earthSceneName;
        AsyncOperation planetLoad = SceneManager.LoadSceneAsync(planetScene, LoadSceneMode.Additive);
        planetLoad.allowSceneActivation = false; // hold until pod is done

        // Store for TransitEmergencyManager to query progress
        BackgroundLoadProgress = planetLoad;

        // Wait until planet is fully loaded (90% = Unity's threshold before allowSceneActivation)
        while (planetLoad.progress < 0.9f)
        {
            Debug.Log($"[GalaxyManager] Background load: {planetLoad.progress * 100f:F0}%");
            yield return null;
        }

        Debug.Log("[GalaxyManager] Planet scene ready. Waiting for pod to finish...");
        // TransitEmergencyManager calls ArriveAtPlanet() which releases allowSceneActivation
    }

    private IEnumerator LoadPlanetScene(string sceneName)
    {
        if (BackgroundLoadProgress != null)
        {
            // Release the held planet scene
            BackgroundLoadProgress.allowSceneActivation = true;
            while (!BackgroundLoadProgress.isDone) yield return null;
        }
        else
        {
            // Fallback direct load
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
        }

        // Unload transit pod
        AsyncOperation unload = SceneManager.UnloadSceneAsync(transitPodSceneName);
        while (unload != null && !unload.isDone) yield return null;

        Debug.Log($"[GalaxyManager] Arrived at {currentPlanet}. Landing offset: {LandingOffset}");
    }

    #endregion

    /// <summary>Exposed so TransitEmergencyManager can poll load progress.</summary>
    public AsyncOperation BackgroundLoadProgress { get; private set; }
}