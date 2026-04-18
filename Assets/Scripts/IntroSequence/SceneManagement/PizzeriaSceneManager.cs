using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene transitions between the three pizzeria scenes.
/// Attach to a persistent GameObject (e.g. with DontDestroyOnLoad).
///
/// Scenes (add all three to Build Settings → Scenes In Build):
///   0 - PizzeriaLobby     : The main restaurant interior / menu screen
///   1 - PizzeriaKitchen   : The kitchen where pizzas are prepared
///   2 - DeliveryRoute     : The delivery driving/walking route
/// </summary>
public class PizzeriaSceneManager : MonoBehaviour
{
    public const string SCENE_LOBBY    = "PizzeriaLobby";
    public const string SCENE_KITCHEN  = "PizzeriaKitchen";
    public const string SCENE_DELIVERY = "DeliveryRoute";

    private static PizzeriaSceneManager instance;

    private void Awake()
    {
        // Singleton — persist across scenes
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public Scene Transitions ───────────────────────────────

    /// <summary>Load the pizzeria lobby / front-of-house scene.</summary>
    public static void GoToLobby()
    {
        SceneManager.LoadScene(SCENE_LOBBY);
    }

    /// <summary>Load the kitchen prep scene.</summary>
    public static void GoToKitchen()
    {
        SceneManager.LoadScene(SCENE_KITCHEN);
    }

    /// <summary>Load the delivery route scene.</summary>
    public static void GoToDelivery()
    {
        SceneManager.LoadScene(SCENE_DELIVERY);
    }

    /// <summary>Load any scene by exact name.</summary>
    public static void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
