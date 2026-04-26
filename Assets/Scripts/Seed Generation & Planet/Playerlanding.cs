using UnityEngine;

/// <summary>
/// PlayerLanding: Attach to the player prefab in each planet scene.
/// On Start it reads GalaxyManager.LandingOffset and teleports the player there.
/// If no GalaxyManager exists (editor testing), the player stays at their default position.
/// </summary>
public class PlayerLanding : MonoBehaviour
{
    [Tooltip("If true, a smoke/crash particle effect is spawned on bad landings")]
    public bool spawnCrashEffect = true;

    [Tooltip("Crash particle prefab (optional)")]
    public GameObject crashEffectPrefab;

    [Tooltip("Distance threshold at which a landing is considered a 'crash' (meters)")]
    public float crashThreshold = 100f;

    private void Start()
    {
        if (GalaxyManager.Instance == null) return;

        Vector3 landingPos = GalaxyManager.Instance.LandingOffset;

        // Only reposition if a real offset was set
        if (landingPos == Vector3.zero) return;

        transform.position = landingPos;

        float distFromHome = Vector3.Distance(new Vector3(landingPos.x, 0f, landingPos.z), Vector3.zero);

        if (distFromHome > crashThreshold)
        {
            Debug.Log($"[PlayerLanding] Crash Landing! {distFromHome:F0}m from Pizzeria.");
            if (spawnCrashEffect && crashEffectPrefab != null) Instantiate(crashEffectPrefab, landingPos, Quaternion.identity);
        }
        else
        {
            Debug.Log($"[PlayerLanding] Smooth landing at {landingPos}");
        }
    }
}