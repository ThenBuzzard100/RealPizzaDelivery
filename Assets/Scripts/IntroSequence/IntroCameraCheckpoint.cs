using UnityEngine;

/// <summary>
/// Attach this to a trigger collider in the scene. When the camera enters it,
/// the IntroCameraManager teleports the camera to the configured checkpoint.
///
/// Now respects IntroSkipManager — checkpoints are blocked on first play
/// (so the player can't accidentally skip by walking through a trigger).
///
/// Setup:
///   1. Create a GameObject with a Collider set to "Is Trigger".
///   2. Attach this script.
///   3. Name the GameObject to match one of the checkpoint trigger names in IntroCameraManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IntroCameraCheckpoint : MonoBehaviour
{
    [Tooltip("If true, this checkpoint can still fire even on first play. Leave false for normal behavior.")]
    [SerializeField] private bool ignoreFirstPlayLock = false;

    private IntroSkipManager skipManager;

    private void Awake()
    {
        // Cache — it's fine if none exists (skipManager stays null = no restriction)
        skipManager = FindObjectOfType<IntroSkipManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Camera>() == null && !other.CompareTag("MainCamera"))
            return;

        // Block checkpoint teleport on first play unless explicitly overridden
        if (!ignoreFirstPlayLock && skipManager != null && skipManager.IsFirstPlay)
        {
            Debug.Log($"[IntroCameraCheckpoint] '{gameObject.name}' blocked — first play, no skipping.");
            return;
        }

        IntroCameraManager manager = FindObjectOfType<IntroCameraManager>();
        if (manager != null)
            manager.OnCheckpointHit(gameObject.name);
    }
}