using UnityEngine;

/// <summary>
/// BackgroundCameraController: Slowly pans/rotates the background camera
/// to give the right-side of the main menu a "living world" feel.
/// 
/// Attach to you background Camera GameObject
/// 
/// The camera's rendered output is visible on the right half of the screen
/// because the Canvas/Camera Viewport or a RenderTexture is set up to show it there.
/// </summary>
public class BackgroundCameraController : MonoBehaviour
{
    // ── Pan Settings ──────────────────────────────────────────────────────────
    [Header("Pan / Orbit")]
    [Tooltip("World-space point the camera slowly orbits around")]
    public Transform orbitTarget;

    [Tooltip("Degrees per second the camera orbits horizontally")]
    public float orbitSpeed = 4f;

    [Tooltip("Radius of the orbit path")]
    public float orbitRadius = 10f;

    [Tooltip("Height offset above the orbit target")]
    public float heightOffset = 2f;

    // ── Tilt Breathing ────────────────────────────────────────────────────────
    [Header("Vertical Breathing")]
    [Tooltip("Enable a gentle up/down tilt oscillation")]
    public bool enableBreathing = true;

    [Tooltip("Maximum degrees of vertical oscillation")]
    public float breathingAmplitude = 3f;

    [Tooltip("Speed of the breathing oscillation")]
    public float breathingSpeed = 0.3f;

    // ── Field of View Pulse ───────────────────────────────────────────────────
    [Header("FOV Pulse (optional)")]
    public bool enableFovPulse = false;

    [Tooltip("Base FOV value")]
    public float baseFov = 60f;

    [Tooltip("Max deviation from base FOV")]
    public float fovAmplitude = 2f;

    public float fovSpeed = 0.2f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private Camera _cam;
    private float _orbitAngle;
    private float _timeOffset;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _timeOffset = Random.Range(0f, 100f); // Randomize phase so it doesn't always start the same
        _orbitAngle = 0f;
    }

    private void update()
    {
        float t = Time.time + _timeOffset;

        // ── Orbit ─────────────────────────────────────────────────────────────
        _orbitAngle += orbitSpeed * Time.deltaTime;

        Vector3 center = orbitTarget != null ? orbitTarget.position : Vector3.zero;

        float x = center.x + Mathf.Sin(_orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float z = center.z + Mathf.Cos(_orbitAngle * Mathf.Deg2Rad) * orbitRadius;
        float y = center.y + heightOffset;

        transform.position = new Vector3(x, y, z);

        // ── Always look at the target ─────────────────────────────────────────
        Vector3 lookDir = center - transform.position;
        if (lookDir != Vector3.zero)
        {
            Quaternion baseLook = Quaternion.LookRotation(lookDir, Vector3.up);

            // ── Breathing tilt ────────────────────────────────────────────────
            float tiltDelta = 0f;
            if (enableBreathing)
                tiltDelta = Mathf.Sin(t * breathingSpeed * Mathf.PI * 2f) * breathingAmplitude;

            transform.rotation = baseLook * Quaternion.Euler(tiltDelta, 0f, 0f);
        }

        // ── FOV pulse ─────────────────────────────────────────────────────────
        if (_cam != null && enableFovPulse)
        {
            _cam.fieldOfView = baseFov + Mathf.Sin(t * fovSpeed * Mathf.PI * 2f) * fovAmplitude;
        }
    }
}