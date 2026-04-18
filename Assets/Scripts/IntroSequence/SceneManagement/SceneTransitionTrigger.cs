using UnityEngine;

/// <summary>
/// Trigger-based scene transition. Place at doorways, exits, or zone boundaries.
/// When the player enters the trigger, it loads the target scene.
///
/// Setup:
///   1. Create a GameObject with a Collider set to "Is Trigger".
///   2. Attach this script.
///   3. Set the target scene name in the Inspector.
///   4. Tag your player as "Player" (or change the required tag below).
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("The exact scene name to load (must be in Build Settings).")]
    [SerializeField] private string targetScene = "";

    [Tooltip("Tag required on the entering object to trigger the transition.")]
    [SerializeField] private string requiredTag = "Player";

    [Header("Optional Fade")]
    [Tooltip("Seconds to wait before loading (gives time for a fade-out effect).")]
    [SerializeField] private float delay = 0f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag(requiredTag)) return;

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("SceneTransitionTrigger: No target scene set.");
            return;
        }

        triggered = true;

        if (delay > 0f)
            Invoke(nameof(LoadTarget), delay);
        else
            LoadTarget();
    }

    private void LoadTarget()
    {
        PizzeriaSceneManager.GoToScene(targetScene);
    }
}
