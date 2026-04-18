using UnityEngine;

/// <summary>
/// Attach this to a trigger collider in the scene. When the camera enters it,
/// the IntroCameraManager teleports the camera to the configured checkpoint.
///
/// Setup:
///   1. Create a GameObject with a Collider set to "Is Trigger".
///   2. Attach this script.
///   3. Name the GameObject to match one of the checkpoint trigger names in IntroCameraManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class IntroCameraCheckpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Camera>() == null && !other.CompareTag("MainCamera"))
            return;

        IntroCameraManager manager = FindObjectOfType<IntroCameraManager>();
        if (manager != null)
            manager.OnCheckpointHit(gameObject.name);
    }
}
