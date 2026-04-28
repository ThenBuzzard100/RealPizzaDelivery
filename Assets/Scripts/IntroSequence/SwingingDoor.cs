using UnityEngine;

public class SwingingDoor : MonoBehaviour
{
    [Header("Swing Settings")]
    [SerializeField] private float swingAngle = 90f;
    [SerializeField] private float swingSpeed = 180f;
    [SerializeField] private float closeDelay = 2.5f;
    [SerializeField] private Vector3 swingAxis = Vector3.up;
    [SerializeField] private string triggerTag = "Player";

    [Header("Damping")]
    [SerializeField] private float damping = 5f;

    [Header("Camera Proximity")]
    [SerializeField] private bool reactToCamera = true;
    [SerializeField] private float cameraOpenDistance = 4f;

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
        HandleCameraProximity();
        HandleTimer();
        ApplyPhysics();
    }

    private void HandleCameraProximity()
    {
        if (!reactToCamera || mainCam == null) return;

        float dist = Vector3.Distance(mainCam.transform.position, transform.position);

        if (dist < cameraOpenDistance && !cameraTriggerActive)
        {
            cameraTriggerActive = true;
            // Only trigger if door is closed OR we want to change direction
            TryTriggerOpen(mainCam.transform.forward);
        }
        else if (dist >= cameraOpenDistance * 1.5f && cameraTriggerActive)
        {
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
        float diff = targetAngle - currentAngle;
        angularVelocity += diff * swingSpeed * Time.deltaTime;
        angularVelocity *= Mathf.Clamp01(1f - damping * Time.deltaTime);
        currentAngle += angularVelocity * Time.deltaTime;
        
        transform.localRotation = initialRotation * Quaternion.AngleAxis(currentAngle, swingAxis);
    }

    private void TryTriggerOpen(Vector3 viewDir)
    {
        // Determine intended swing based on camera look direction
        float dot = Vector3.Dot(transform.forward, viewDir);
        float newTarget = dot >= 0 ? swingAngle : -swingAngle;

        // "Strictness" Fix: Allow triggering if door is closed OR if 
        // the camera direction would push it the OTHER way mid-swing.
        if (Mathf.Abs(currentAngle) < 5f || Mathf.Sign(newTarget) != Mathf.Sign(targetAngle))
        {
            targetAngle = newTarget;
            closeTimer = closeDelay;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        Vector3 dir = (mainCam != null) ? mainCam.transform.forward : other.transform.forward;
        TryTriggerOpen(dir);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
        closeTimer = closeDelay * 0.5f;
    }
}