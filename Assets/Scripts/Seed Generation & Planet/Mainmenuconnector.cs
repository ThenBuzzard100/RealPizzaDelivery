using UnityEngine;

/// <summary>
/// MainMenuConnector: Add this to the MenuManager GameObject in your Main Menu scene.
/// 
/// This bridges the existing PlayPanelController (New World / Load World) to GalaxyManager seed-system.
/// 
/// HOW TO WIRE
///     In PlayPanelController, the "CreateButton" calls OnCreateWorldConfirmed().
///     Override that flow by calling MainMenuConnector.StartNewWorld(worldName) instead,
///     OR simply let this script hook into the existing PlayerPrefs keys that PlayPanelController already sets.
/// 
/// The simplest approach: add this to MenuManager and it auto-reads PlayerPrefs on scene load, so no refactoring of PlayPanelController is needed.
/// </summary>
public class Mainmenuconnector : MonoBehaviour
{
    [Header("Galaxy Settings")]
    [Tooltip("If true, a random seed is generated for each new world. " + "If false, seedOverride is used (useful for testing).")]
    public bool useRandomSeed = true;

    [Tooltip("Fixed seed used when useRandomSeed is false")]
    public long seedOverride = 12345L;

    [Tooltip("Which planet to travel to from the main menu")]
    public PlanetType startingPlanet = PlanetType.Earth;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this instead of (or after) PlayPanelController.OnCreateWorldConfirmed().
    /// Generates a fresh seed and begins travel.
    /// </summary>
    public void StartNewWorld(string worldName)
    {
        long seed = useRandomSeed ? GenerateRandomSeed(worldName) : seedOverride;

        // Save so it can be loaded later
        SaveSeed(worldName, seed);

        BeginTravel(seed);
    }

    /// <summary>
    /// Call this when the player selects a saved world from the load list.
    /// Reads the seed that was saved when the world was first created.
    /// </summary>
    public void LoadSavedWorld(string worldName)
    {
        long seed = LoadSeed(worldName);

        if (seed == 0L)
        {
            Debug.LogWarning($"[MainMenuConnector] No seed found for '{worldName}'." + "Generating a new one.");
            seed = GenerateRandomSeed(worldName);
            SaveSeed(worldName, seed);
        }

        BeginTravel(seed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Private Helpers

    private void BeginTravel(long seed)
    {
        if (GalaxyManager.Instance == null)
        {
            Debug.LogError("[MainMenuConnector] GalaxyManager not found in scene. " + "Make sure it exists on a DontDestroyOnLoad GameObject.");
            return;
        }

        GalaxyManager.Instance.InitialiseSeed(seed);
        GalaxyManager.Instance.TravelToPlanet(startingPlanet);
    }

    /// <summary>
    /// Derives a deterministic 64-bit seed from the world name + current time.
    /// Two worlds with the same name will have different seed (time component), but a saved world always reloads identically (seed is stored in PlayerPrefs).
    /// </summary>
    private long GenerateRandomSeed(string worldName)
    {
        // Hash the world name for a name-based component
        long nameHash = 0L;
        foreach (char c in worldName)
        {
            nameHash = nameHash * 31L + c;
        }

        // XOR with current tick count for uniqueness
        long timePart = System.DateTime.Now.Ticks;

        return nameHash ^ timePart;
    }

    private void SaveSeed(string worldName, long seed)
    {
        // Store high and low 32 bits separately since PlayerPrefs only support int
        int high = (int)(seed >> 32);
        int low = (int)(seed & 0xFFFFFFFL);
        PlayerPrefs.SetInt($"Seed_High_{worldName}", high);
        PlayerPrefs.SetInt($"Seed_Low_{worldName}", low);
        PlayerPrefs.Save();
        Debug.Log($"[MainMenuConnector] Saved seed {seed} for world '{worldName}'");
    }

    private long LoadSeed(string worldName)
    {
        if (!PlayerPrefs.HasKey($"Seed_High_{worldName}")) return 0L;
        int high = PlayerPrefs.GetInt($"Seed_High_{worldName}");
        int low = PlayerPrefs.GetInt($"Seed_Low_{worldName}");
        long seed = ((long)high << 32) | ((long)low & 0xFFFFFFFL);
        Debug.Log($"[MainMenuConnector] Loaded seed {seed} for world '{worldName}'");
        return seed;
    }

    #endregion
}