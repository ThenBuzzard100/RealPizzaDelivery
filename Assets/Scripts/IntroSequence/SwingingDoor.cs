using UnityEngine;

public class SwingingDoor : MonoBehaviour
{
    [Header("Swing Settings")]
    [SerializeField] private float swingAngle  = 90f;
    [SerializeField] private float swingSpeed  = 180f;
    [SerializeField] private float closeDelay  = 2.5f;
    [SerializeField] private Vector3 swingAxis = Vector3.up;
    [SerializeField] private string triggerTag = "Player";

    [Header("Damping")]
    [SerializeField] private float damping = 5f;

    [Header("Camera Proximity")]
    [SerializeField] private bool  reactToCamera      = true;
    [SerializeField] private float cameraOpenDistance = 4f;

    private float      targetAngle        = 0f;
    private float      currentAngle       = 0f;
    private float      angularVelocity    = 0f;
    private float      closeTimer         = -1f;
    private bool       isSwinging         = false;
    private Quaternion initialRotation;

    // Store the door's INITIAL world-space vectors so they never change
    // regardless of how far the door has swung
    private Vector3 initialRight;
    private Vector3 initialForward;

    private Camera mainCam;
    private bool   cameraTriggerActive = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        initialRotation = transform.localRotation;

        // Capture rest-pose directions ONCE — never read transform.right/forward again
        initialRight   = transform.right;
        initialForward = transform.forward;

        mainCam = Camera.main;
    }

    private void Update()
    {
        HandleCameraProximity();
        HandleTimer();
        ApplyPhysics();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void HandleCameraProximity()
    {
        if (!reactToCamera || mainCam == null) return;

        float dist = Vector3.Distance(mainCam.transform.position, transform.position);

        if (dist < cameraOpenDistance && !cameraTriggerActive)
        {
            cameraTriggerActive = true;
            TryTriggerOpen(mainCam.transform.position, mainCam.transform.forward);
        }
        else if (dist >= cameraOpenDistance * 1.5f && cameraTriggerActive)
        {
            // Player left range — reset so it can trigger again next approach
            cameraTriggerActive = false;
        }
    }

    private void HandleTimer()
    {
        if (closeTimer > 0f)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer <= 0f) targetAngle = 0f;
        }
    }

    private void ApplyPhysics()
    {
        float diff      = targetAngle - currentAngle;
        angularVelocity += diff * swingSpeed * Time.deltaTime;
        angularVelocity *= Mathf.Clamp01(1f - damping * Time.deltaTime);
        currentAngle    += angularVelocity * Time.deltaTime;

        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, swingAxis);

        // Consider the door settled when it's close to target and nearly stopped
        isSwinging = Mathf.Abs(diff) > 8f || Mathf.Abs(angularVelocity) > 5f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Uses the door's INITIAL (rest-pose) vectors so the calculation is never
    /// affected by how far the door has already swung.
    ///
    /// Side:   which side of the door the player is on in rest-pose space.
    ///         Positive = player is to the initial-right of the door pivot.
    ///         Negative = player is to the initial-left of the door pivot.
    ///
    /// Facing: whether the camera is pointing roughly the same way as the door's
    ///         initial forward, or opposite.
    ///         Positive = player faces same direction as door forward (front→back).
    ///         Negative = player faces against door forward (back→front).
    ///
    /// Result: swing direction = side × facing
    ///   Front-right approach → swing right (+)
    ///   Front-left  approach → swing left  (-)
    ///   Back-right  approach → swing left  (-)  ← reverses because facing flips
    ///   Back-left   approach → swing right (+)  ← reverses because facing flips
    /// This always pushes the door away from the player.
    /// </summary>
    private void TryTriggerOpen(Vector3 approachPosition, Vector3 cameraForward)
    {
        // Don't accept new triggers while the door is still moving
        if (isSwinging) return;
        // Use INITIAL vectors — never the current transform vectors
        Vector3 toPlayer = approachPosition - transform.position;

        float side      = Vector3.Dot(initialRight,   toPlayer.normalized);
        float facingDot = Vector3.Dot(initialForward, cameraForward);

        // Combine to get final swing direction
        float swingDir = Mathf.Sign(side) * Mathf.Sign(facingDot);

        // If either is exactly zero (player dead-centre) default to +1
        if (Mathf.Approximately(swingDir, 0f)) swingDir = 1f;

        float newTarget = swingDir * swingAngle;

        // Only retrigger if door is near closed OR player is pushing it the other way
        if (Mathf.Abs(currentAngle) < 5f || Mathf.Sign(newTarget) != Mathf.Sign(targetAngle))
        {
            targetAngle = newTarget;
            closeTimer  = closeDelay;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;

        Vector3 approachPos = mainCam != null
            ? mainCam.transform.position
            : other.transform.position;

        Vector3 facingDir = mainCam != null
            ? mainCam.transform.forward
            : other.transform.forward;

        TryTriggerOpen(approachPos, facingDir);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        closeTimer = closeDelay * 0.5f;
    }
}