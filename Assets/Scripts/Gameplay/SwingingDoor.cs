using UnityEngine;

/// <summary>
/// Animated swinging kitchen door. Opens for player triggers AND camera proximity.
/// Uses spring physics for natural bounce. Supports double doors (pair two of these).
/// </summary>
public class SwingingDoor : MonoBehaviour
{
    [Header("Swing Settings")]
    [Tooltip("How far the door swings open (degrees).")]
    [SerializeField] private float swingAngle = 90f;

    [Tooltip("How fast the door swings (degrees/sec).")]
    [SerializeField] private float swingSpeed = 180f;

    [Tooltip("Seconds before the door swings closed.")]
    [SerializeField] private float closeDelay = 2.5f;

    [Tooltip("Axis to rotate around (local space).")]
    [SerializeField] private Vector3 swingAxis = Vector3.up;

    [Tooltip("Tag that triggers the door. Leave empty to trigger on anything.")]
    [SerializeField] private string triggerTag = "";

    [Header("Damping")]
    [Tooltip("Higher = less bounce.")]
    [SerializeField] private float damping = 5f;

    [Header("Camera Proximity")]
    [Tooltip("If true, the door also opens when the main camera gets close (for intro sequences).")]
    [SerializeField] private bool reactToCamera = true;

    [Tooltip("Distance at which the camera triggers the door.")]
    [SerializeField] private float cameraOpenDistance = 4f;

    // ── State ──
    private float targetAngle = 0f;
    private float currentAngle = 0f;
    private float angularVelocity = 0f;
    private float closeTimer = -1f;
    private Quaternion initialRotation;
    private Camera mainCam;
    private bool cameraTriggerActive = false;

    private void Start()
    {
        initialRotation = transform.localRotation;
        mainCam = Camera.main;
    }

    private void Update()
    {
        // Camera proximity check
        if (reactToCamera && mainCam != null)
        {
            float dist = Vector3.Distance(mainCam.transform.position, transform.position);
            if (dist < cameraOpenDistance && !cameraTriggerActive)
            {
                cameraTriggerActive = true;
                Vector3 toCamera = mainCam.transform.position - transform.position;
                float dot = Vector3.Dot(transform.forward, toCamera);
                targetAngle = dot >= 0 ? swingAngle : -swingAngle;
                closeTimer = closeDelay;
            }
            else if (dist >= cameraOpenDistance * 1.5f && cameraTriggerActive)
            {
                cameraTriggerActive = false;
            }
        }

        // Close timer
        if (closeTimer > 0f)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0f)
                targetAngle = 0f;
        }

        // Spring physics
        float diff = targetAngle - currentAngle;
        angularVelocity += diff * swingSpeed * Time.deltaTime;
        angularVelocity *= Mathf.Clamp01(1f - damping * Time.deltaTime);
        currentAngle += angularVelocity * Time.deltaTime;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, swingAxis);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
            return;

        Vector3 toOther = other.transform.position - transform.position;
        float dot = Vector3.Dot(transform.forward, toOther);
        targetAngle = dot >= 0 ? swingAngle : -swingAngle;
        closeTimer = closeDelay;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
            return;

        closeTimer = closeDelay * 0.5f;
    }

    public void Open(bool fromFront = true)
    {
        targetAngle = fromFront ? swingAngle : -swingAngle;
        closeTimer = closeDelay;
    }

    public void Close()
    {
        targetAngle = 0f;
        closeTimer = -1f;
    }
}
