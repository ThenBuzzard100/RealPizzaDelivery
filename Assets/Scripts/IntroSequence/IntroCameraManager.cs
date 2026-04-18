using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Drives the Main Camera along a smooth spline path for cinematic intro sequences.
/// Uses Catmull-Rom interpolation for buttery-smooth curves instead of linear point-to-point.
/// Timing is duration-based so you can set exactly how long the full path takes.
/// </summary>
public class IntroCameraManager : MonoBehaviour
{
    // ── Path Settings ──────────────────────────────────────────

    [Header("Path")]
    [Tooltip("Name of the GameObject whose children define the camera path waypoints.")]
    [SerializeField] private string pathName = "IntroPath";

    [Tooltip("If true, the camera will loop back to the start when it reaches the end.")]
    [SerializeField] private bool loop = false;

    // ── Timing ─────────────────────────────────────────────────

    [Header("Timing")]
    [Tooltip("Total duration in seconds for the camera to travel the full path.")]
    [Range(5f, 180f)]
    [SerializeField] private float totalDuration = 60f;

    // ── Smoothing ──────────────────────────────────────────────

    [Header("Smoothing")]
    [Tooltip("How smoothly the camera rotates to face forward along the path.")]
    [Range(0.5f, 20f)]
    [SerializeField] private float rotationSmooth = 4f;

    [Tooltip("How far ahead on the spline the camera looks (0-1 normalized). Higher = smoother turns.")]
    [Range(0.01f, 0.15f)]
    [SerializeField] private float lookAheadAmount = 0.05f;

    [Tooltip("Catmull-Rom spline tension. 0 = smooth, 1 = tight corners.")]
    [Range(0f, 1f)]
    [SerializeField] private float splineTension = 0f;

    // ── Checkpoints ────────────────────────────────────────────

    [Header("Checkpoints")]
    [SerializeField] private List<Checkpoint> checkpoints = new List<Checkpoint>();

    [System.Serializable]
    public class Checkpoint
    {
        public string triggerName;
        [Range(0f, 1f)] public float pathPosition; // 0-1 normalized position on path
    }

    // ── Events ─────────────────────────────────────────────────

    [Header("Events")]
    [Tooltip("Disable this GameObject when the intro finishes (useful for handing off to player).")]
    [SerializeField] private bool disableOnFinish = false;

    // ── Runtime State ──────────────────────────────────────────

    private List<Vector3> waypoints = new List<Vector3>();
    private float currentTime = 0f;
    private Camera mainCamera;
    private bool isPlaying = true;

    // ── Public API ─────────────────────────────────────────────

    public void Play() => isPlaying = true;
    public void Pause() => isPlaying = false;
    public bool IsPlaying => isPlaying;
    public float Progress => totalDuration > 0 ? Mathf.Clamp01(currentTime / totalDuration) : 0f;

    public void Stop()
    {
        isPlaying = false;
        currentTime = 0f;
        SnapToProgress(0f);
    }

    public void TeleportToProgress(float t)
    {
        currentTime = Mathf.Clamp01(t) * totalDuration;
        SnapToProgress(t);
    }

    public void SetDuration(float duration)
    {
        totalDuration = Mathf.Max(1f, duration);
    }

    // ── Unity Lifecycle ────────────────────────────────────────

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("IntroCameraManager: No Main Camera found.");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        BuildPath();

        if (waypoints.Count >= 2)
            SnapToProgress(0f);
        else
        {
            Debug.LogWarning($"IntroCameraManager: Path '{pathName}' needs at least 2 waypoints.");
            isPlaying = false;
        }
    }

    private void Update()
    {
        if (!isPlaying || waypoints.Count < 2) return;

        currentTime += Time.deltaTime;

        float t = currentTime / totalDuration;

        if (t >= 1f)
        {
            if (loop)
            {
                currentTime = 0f;
                t = 0f;
            }
            else
            {
                t = 1f;
                isPlaying = false;

                if (disableOnFinish)
                    gameObject.SetActive(false);
            }
        }

        // Position on spline
        Vector3 pos = EvaluateSpline(t);
        mainCamera.transform.position = pos;

        // Smooth look-ahead rotation
        float lookT = Mathf.Min(t + lookAheadAmount, 1f);
        Vector3 lookPos = EvaluateSpline(lookT);
        Vector3 lookDir = lookPos - pos;

        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRot,
                rotationSmooth * Time.deltaTime
            );
        }
    }

    // ── Checkpoint Handling ────────────────────────────────────

    public void OnCheckpointHit(string triggerName)
    {
        foreach (var cp in checkpoints)
        {
            if (cp.triggerName == triggerName)
            {
                TeleportToProgress(cp.pathPosition);
                return;
            }
        }
    }

    // ── Path Building ──────────────────────────────────────────

    private void BuildPath()
    {
        waypoints.Clear();

        GameObject pathObj = GameObject.Find(pathName);
        if (pathObj == null)
        {
            Debug.LogError($"IntroCameraManager: Could not find '{pathName}'.");
            return;
        }

        // Try LineRenderer
        LineRenderer lr = pathObj.GetComponent<LineRenderer>();
        if (lr != null && lr.positionCount >= 2)
        {
            Vector3[] positions = new Vector3[lr.positionCount];
            lr.GetPositions(positions);
            for (int i = 0; i < positions.Length; i++)
                waypoints.Add(lr.useWorldSpace ? positions[i] : pathObj.transform.TransformPoint(positions[i]));
            return;
        }

        // Child waypoints
        if (pathObj.transform.childCount >= 2)
        {
            for (int i = 0; i < pathObj.transform.childCount; i++)
                waypoints.Add(pathObj.transform.GetChild(i).position);
            return;
        }

        Debug.LogWarning($"IntroCameraManager: '{pathName}' has no valid path data.");
    }

    // ── Catmull-Rom Spline ─────────────────────────────────────

    private Vector3 EvaluateSpline(float t)
    {
        if (waypoints.Count < 2) return Vector3.zero;

        t = Mathf.Clamp01(t);

        int segCount = waypoints.Count - 1;
        float scaledT = t * segCount;
        int seg = Mathf.FloorToInt(scaledT);
        float segT = scaledT - seg;

        if (seg >= segCount)
        {
            seg = segCount - 1;
            segT = 1f;
        }

        // Get 4 control points (clamped at edges)
        Vector3 p0 = waypoints[Mathf.Max(seg - 1, 0)];
        Vector3 p1 = waypoints[seg];
        Vector3 p2 = waypoints[Mathf.Min(seg + 1, waypoints.Count - 1)];
        Vector3 p3 = waypoints[Mathf.Min(seg + 2, waypoints.Count - 1)];

        return CatmullRom(p0, p1, p2, p3, segT, splineTension);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
    {
        float s = (1f - tension) * 0.5f;

        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 a = -s * p0 + (2f - s) * p1 + (s - 2f) * p2 + s * p3;
        Vector3 b = 2f * s * p0 + (s - 3f) * p1 + (3f - 2f * s) * p2 - s * p3;
        Vector3 c = -s * p0 + s * p2;
        Vector3 d = p1;

        return a * t3 + b * t2 + c * t + d;
    }

    private void SnapToProgress(float t)
    {
        if (waypoints.Count < 2) return;

        Vector3 pos = EvaluateSpline(t);
        mainCamera.transform.position = pos;

        float lookT = Mathf.Min(t + lookAheadAmount, 1f);
        Vector3 lookPos = EvaluateSpline(lookT);
        Vector3 dir = lookPos - pos;
        if (dir.sqrMagnitude > 0.0001f)
            mainCamera.transform.rotation = Quaternion.LookRotation(dir);
    }

    // ── Gizmos ─────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Draw the smooth spline in editor
        if (waypoints == null || waypoints.Count < 2) return;

        // Spline curve
        Gizmos.color = Color.cyan;
        int steps = waypoints.Count * 20;
        Vector3 prev = EvaluateSpline(0f);
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 curr = EvaluateSpline(t);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        // Waypoint spheres
        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.color = (i == 0) ? Color.green : (i == waypoints.Count - 1) ? Color.red : Color.white;
            Gizmos.DrawWireSphere(waypoints[i], 0.25f);
        }

        // Checkpoint markers
        Gizmos.color = Color.yellow;
        foreach (var cp in checkpoints)
        {
            Vector3 cpPos = EvaluateSpline(cp.pathPosition);
            Gizmos.DrawWireCube(cpPos, Vector3.one * 0.4f);
        }
    }
}
