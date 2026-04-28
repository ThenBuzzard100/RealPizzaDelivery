using UnityEngine;
using Mirror;

/// <summary>
/// PlayerSpawner: Attach to an empty GameObject in your game scene.
/// Automatically spawns the player in single player mode.
/// In multiplayer, Mirror handles spawning via NetworkManager — this script
/// does nothing so Mirror's system takes over cleanly.
///
/// SETUP:
///   - Assign playerPrefab (must have NetworkIdentity for Mirror)
///   - Assign spawnPoint transform (or leave null to spawn at origin)
///   - NetworkManager must have the same prefab in its Player Prefab slot
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Your player prefab — must have NetworkIdentity for Mirror")]
    public GameObject playerPrefab;

    [Tooltip("Where to spawn the player. If null, spawns at world origin.")]
    public Transform spawnPoint;

    [Tooltip("If GalaxyManager has a landing offset, use that instead of spawnPoint")]
    public bool useLandingOffset = true;

    private void Start()
    {
        // If Mirror is already running as host or client, it handles spawning
        if (NetworkServer.active || NetworkClient.isConnected)
        {
            Debug.Log("[PlayerSpawner] Mirror active — skipping single player spawn.");
            return;
        }

        // Start Mirror as host even in single player so all NetworkBehaviours
        // initialise correctly — without this, scripts that require a network
        // connection won't run at all
        NetworkManager nm = NetworkManager.singleton;
        if (nm != null)
        {
            nm.StartHost();
            Debug.Log("[PlayerSpawner] Started Mirror as single player host.");
            // Mirror will spawn the player via NetworkManager.playerPrefab automatically
            // so we don't need to call SpawnSinglePlayer() here
        }
        else
        {
            // No NetworkManager — pure single player, spawn directly
            SpawnSinglePlayer();
        }
    }

    private void SpawnSinglePlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] No player prefab assigned.");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition();
        Quaternion spawnRot = spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;

        GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
        Debug.Log($"[PlayerSpawner] Single player spawned at {spawnPos}");
    }

    private Vector3 GetSpawnPosition()
    {
        // Priority 1: GalaxyManager landing offset (set by transit pod)
        if (useLandingOffset && GalaxyManager.Instance != null
            && GalaxyManager.Instance.LandingOffset != Vector3.zero)
        {
            return GalaxyManager.Instance.LandingOffset;
        }

        // Priority 2: Assigned spawn point
        if (spawnPoint != null)
            return spawnPoint.position;

        // Priority 3: World origin
        return Vector3.zero;
    }
}