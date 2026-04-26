using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TransitEmergencyManager: Place in the TransitPod scene.
/// Reads the planet sub-seed from GalaxyManager to deterministically select which emergencies trigger and in what order.
/// Tracks repair success when the pod arrives.
/// </summary>
public class TransitWmergencyManager : MonoBehaviour
{
    // ── Emergency Prop References ─────────────────────────────────────────────
    [Header("Duct Tape Fix")]
    public ParticleSystem ductTapeSteamParticles;
    public Transform pipeLeakTrigger;
    public GameObject ductTapeProp;
    [Tooltip("Seconds the tape must be held against the leak")]
    public float ductTapeHoldTime = 4f;

    [Header("Engine Grease Fire")]
    public ParticleSystem fireParticles;
    public Transform fireConsole;
    public GameObject fireExtinguisherProp;
    [Tooltip("Seconds until fire spreads to next stage")]
    public float fireSpreadTime = 8f;

    [Header("Loose Lug Nut")]
    public Rigidbody lugNutRigidbody;
    public Transform lugNutSocket;
    [Tooltip("Force applied when ejecting the lug nut")]
    public float ejectionForce = 20f;

    [Header("Slop Clog")]
    public Transform fanTransform;
    public Rigidbody slopPropRigidbody;
    public float fanNormalSpeed = 360f; // Degress per second

    [Header("Hull Breach")]
    public Transform hullBreachPoint;
    public float suctionForce = 5f;
    public LayerMask suctionAffectedLayers;
    public float suctionRadius = 3f;

    [Header("Gravity Hiccup")]
    [Tooltip("Duration of zero-gravity event in seconds")]
    public float gravityHiccupDuration = 6f;

    [Header("Landing")]
    [Tooltip("Max distance from Pizzeria if all emergencies are failed (meters)")]
    public float maxLandingOffset = 5000f;

    [Tooltip("World position of the Pizzeria (should be 0,0,0 on Earth)")]
    public Vector3 pizzeriaPosition = Vector3.zero;

    [Tooltip("Perfect landing position just outside the Pizzeria")]
    public Vector3 perfectLandingPosition = new Vector3(0f, 5f, 0f);

    [Header("Pod Complete")]
    [Tooltip("Seconds after all emergencies resolve before calling ArriveAtPlanet")]
    public float landingDelay = 3f;

    // ── Internal State ────────────────────────────────────────────────────────
    private System.Random _rng;
    private long _seed;

    private int _totalEmergencies = 0;
    private int _resolvedCount = 0;
    private int _failedCount = 0;
    private bool _podFinished = false;

    // Per-emergency resolved flags
    private bool _ductTapeFixed = false;
    private bool _fireExtinguished = false;
    private bool _lugNutSocketed = false;
    private bool _slopCleared = false;
    private bool _breachPlugged = false;
    private bool _gravityRestored = false; // Auto-resolves after duration

    // Fan state
    private float _fanCurrentSpeed = 0f;
    private bool _fanClogged = false;

    // Hull breach suction active
    private bool _breachActive = false;

    private void Start()
    {
        _seed = GalaxyManager.Instance != null ? GalaxyManager.Instance.CurrentPlanetSubSeed
        : System.DateTime.Now.Ticks;

        _rng = new System.Random((int)(_seed & 0x7FFFFFFF));

        StartCoroutine(RunEmergencySequence());
    }

    // ─────────────────────────────────────────────────────────────────────────
    #region Emergency Sequence

    /// <summary>
    /// Uses the seed to pick a subset of emergencies and trigger them with delays.
    /// Every playthrough with the same seed gets the same events in the same order.
    /// </summary>
    private IEnumerator RunEmergencySequence()
    {
        // Build a shuffled list of all emergency types using the seed
        var allEmergencies = new List<System.Action>
        {
            TriggerDuctTapeFix,
            TriggerGreaseFire,
            TriggerLooseLugNut,
            TriggerSlopClog,
            TriggerHullBreach,
            TriggerGravityHiccup,
        };

        // Seed-based shuffle (Fisher-Yates)
        for (int i = allEmergencies.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(0, i + 1);
            (allEmergencies[i], allEmergencies[j]) = (allEmergencies[j], allEmergencies[i]);
        }

        // Pick 2-4 emergencies per trip based on seed
        int count = 2 + (_rng.Next(0, 3)); // 2, 3, or 4
        _totalEmergencies = count;

        Debug.Log($"[TransitPod] Seed {_seed} → {count} emergencies selected");

        for (int i = 0; i < count; i++)
        {
            // Wait a random seed-deter,omed delay between emergencies
            float delay = 5f + (float)(_rng.NextDouble() * 10f);
            yield return new WaitForSeconds(delay);

            allEmergencies[i].Invoke();

            // Wait for this emergency to resolve or timeout
            yield return StartCoroutine(WaitForEmergencyResolve(i));
        }

        // All emergencies done - calculate landing and depart
        yield return new WaitForSeconds(landingDelay);
        FinaliseLanding();
    }

    /// <summary>Waits up to a timeout for each emergency to be resolved.</summary>
    private IEnumerator WaitForEmergencyResolve(int index)
    {
        float timeout = 30f; // Players have 30 seconds per emergency
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;

            // Check if the specific emergency was resolved
            bool resolved = index switch
            {
                0 => _ductTapeFixed,
                1 => _fireExtinguished,
                2 => _lugNutSocketed,
                3 => _slopCleared,
                4 => _breachPlugged,
                5 => _gravityRestored,
                _ => true
            };

            if (resolved)
            {
                _resolvedCount++;
                Debug.Log($"[TransitPod] Emergency {index} resovled! ({_resolvedCount}/{_totalEmergencies})");
                yield break;
            }

            yield return null;
        }

        // Timeout = failed
        _failedCount++;
        Debug.Log($"[TransitPod] Emergency {index} FAILED. ({_failedCount} failures so far)");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Individual Emergency Triggers

    // ── 1. Duct Tape Fix ─────────────────────────────────────────────────────
    private void TriggerDuctTapeFix()
    {
        Debug.Log("[TransitPod] EMERGENCY: Duct Tape Fix!");
        if (ductTapeSteamParticles != null) ductTapeSteamParticles.Play();
        if (ductTapeProp != null) ductTapeProp.SetActive(true);
        StartCoroutine(MonitorDuctTape());
    }

    private IEnumerator MonitorDuctTape()
    {
        // Continuously check if the duct tape prop is close to the leak trigger
        while (!_ductTapeFixed)
        {
            if (ductTapeProp != null && pipeLeakTrigger != null)
            {
                float dist = Vector3.Distance(ductTapeProp.transform.position, pipeLeakTrigger.position);
                if (dist < 0.5f)
                {
                    yield return StartCoroutine(HoldDuctTape());
                    yield break;
                }
            }
            yield return null;
        }
    }

    private IEnumerator HoldDuctTape()
    {
        float held = 0f;
        while (held < ductTapeHoldTime)
        {
            if (ductTapeProp == null) yield break;
            float dist = Vector3.Distance(ductTapeProp.transform.position, pipeLeakTrigger.position);
            if (dist > 0.5f) { held = 0f ;} // Moved away, reset timer
            else { held += Time.deltaTime; }
            yield return null;
        }

        // Fixed!
        _ductTapeFixed = true;
        if (ductTapeSteamParticles != null) ductTapeSteamParticles.Stop();
        Debug.Log("[TransitPod] Pipe patched!");
    }

    // ── 2. Engine Grease Fire ────────────────────────────────────────────────
    private void TriggerGreaseFire()
    {
        Debug.Log("[TransitPod] EMERGENCY: Engine Grease Fire!");
        if (fireParticles != null) fireParticles.Play();
        if (fireExtinguisherProp != null) fireExtinguisherProp.SetActive(true);
        StartCoroutine(MonitorFire());
    }

    private IEnumerator MonitorFire()
    {
        float spreadTimer = 0f;
        while (!_fireExtinguished)
        {
            spreadTimer += Time.deltaTime;
            // Scale fire particles to show spreading
            if (fireParticles != null && spreadTimer < fireSpreadTime)
            {
                float scale = 1f + (spreadTimer / fireSpreadTime) * 2f;
                var main = fireParticles.main;
                main.startSizeMultiplier = scale;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Call this from the fire extinguisher's interaction script when sprayed.
    /// The interaction script should call this each frame the spray hits the fire console.
    /// </summary>
    public void OnExtinguisherSpray(Transform spraySource)
    {
        if (!_fireExtinguished) return;
        if (fireConsole == null) return;

        float dist = Vector3.Distance(spraySource.position, fireConsole.position);
        if (dist < 2.5f)
        {
            _fireExtinguished = true;
            if (fireParticles != null) fireParticles.Stop();
            Debug.Log("[TransitPod] Fire extinguished");
        }
    }

    // ── 3. Loose Lug Nut ────────────────────────────────────────────────────
    private void TriggerLooseLugNut()
    {
        Debug.Log("[TansitPod] EMERGENCY: Loose Lug Nut");
        if(lugNutRigidbody == null) return;

        lugNutRigidbody.isKinematic = false;
        Vector3 randomDir = new Vector3(
            (float)(_rng.NextDouble() * 2 - 1),
            (float)(_rng.NextDouble() * 2 - 1),
            (float)(_rng.NextDouble() * 2 - 1)).normalized;
        lugNutRigidbody.AddForce(randomDir * ejectionForce, ForceMode.Impulse);

        StartCoroutine(MonitorLugNut());
    }

    private IEnumerator MonitorLugNut()
    {
        while (!_lugNutSocketed)
        {
            if (lugNutRigidbody != null && lugNutSocket != null)
            {
                float dist = Vector3.Distance(lugNutRigidbody.transform.position, lugNutSocket.position);
                if (dist < 0.3f)
                {
                    // Snap into socket
                    lugNutRigidbody.isKinematic = true;
                    lugNutRigidbody.transform.position = lugNutSocket.position;
                    lugNutRigidbody.transform.rotation = lugNutSocket.rotation;
                    _lugNutSocketed = true;
                    Debug.Log("[TransitPod] Lug nut socketed!");
                }
            }
            yield return null;
        }
    }

    // ── 4. Slop Clog ─────────────────────────────────────────────────────────
    private void TriggerSlopClog()
    {
        Debug.Log("[TransitPod] EMERGENCY: Slop Clog!");
        _fanClogged = true;
        _fanCurrentSpeed = 0f;

        // Move slop prop in front of fan
        if (slopPropRigidbody != null && fanTransform != null)
        {
            slopPropRigidbody.transform.position = fanTransform.position + fanTransform.forward * 0.3f;
            slopPropRigidbody.isKinematic = false;
        }

        StartCoroutine(MonitorSlopClog());
    }

    private void Update()
    {
        // Spin the fan if not clogged
        if (!_fanClogged && fanTransform != null)
        {
            _fanCurrentSpeed = Mathf.MoveTowards(_fanCurrentSpeed, fanNormalSpeed, Time.deltaTime * 180f);
            fanTransform.Rotate(Vector3.forward, _fanCurrentSpeed * Time.deltaTime);
        }
    }

    private IEnumerator MonitorSlopClog()
    {
        while (!_slopCleared)
        {
            if (slopPropRigidbody != null && fanTransform != null)
            {
                float dist = Vector3.Distance(slopPropRigidbody.transform.position, fanTransform.position);
                // Cleared if prop is kicked far enough away
                if (dist > 1.5f)
                {
                    _slopCleared = true;
                    _fanClogged = false;
                    Debug.Log("[TansitPod] Slop cleared!");
                }
            }
            yield return null;
        }
    }

    // ── 5. Hull Breach ───────────────────────────────────────────────────────
    private void TriggerHullBreach()
    {
        Debug.Log("[TransitPod] EMERGENCY: Hull Breach!");
        _breachActive = true;
        StartCoroutine(RunHullBreach());
    }

    private IEnumerator RunHullBreach()
    {
        while (!_breachPlugged)
        {
            // Suck nearby physics objects toward the breach
            if (hullBreachPoint != null)
            {
                Collider[] nearby = Physics.OverlapSphere(hullBreachPoint.position, suctionRadius, suctionAffectedLayers);

                foreach (Collider col in nearby)
                {
                    Rigidbody rb = col.GetComponent<Rigidbody>();
                    if (rb == null || rb.isKinematic) continue;

                    Vector3 dir = (hullBreachPoint.position - rb.position).normalized;
                    rb.AddForce(dir * suctionForce, ForceMode.Force);
                }
            }
            yield return new WaitForSeconds(0.05f);
        }

        _breachActive = false;
        Debug.Log("[TransitPod] Breach plugged!");
    }

    /// <summary>
    /// Call from trigger/collider on the breach point.
    /// Triggers when a heavy object or player stands on/covers it.
    /// </summary>
    public void OnBreachCovered(Collider other)
    {
        if (_breachPlugged) return;
        // Accept player or any Rigidbody with mass > 5kg as a plug
        Rigidbody rb = other.GetComponent<Rigidbody>();
        bool isPlayer = other.CompareTag("Player");

        if (isPlayer || (rb != null && rb.mass > 5f))
        {
            _breachPlugged = true;
        }
    }

    // ── 6. Gravity Hiccup ────────────────────────────────────────────────────
    private void TriggerGravityHiccup()
    {
        Debug.Log("[TransitPod] EMERGENCY: Gravity Hiccup!");
        StartCoroutine(GravityHiccupRoutine());
    }

    private IEnumerator GravityHiccupRoutine()
    {
        Vector3 originalGravity = Physics.gravity;
        Physics.gravity = Vector3.zero; // Set gravity

        yield return new WaitForSeconds(gravityHiccupDuration);

        Physics.gravity = originalGravity;
        _gravityRestored = true;
        Debug.Log("[TransitPod] Gravity restored!");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Landing Calculation

    private void FinaliseLanding()
    {
        if (_podFinished) return;
        _podFinished = true;

        // Calculate offset based on failures
        float failRatio = _totalEmergencies > 0 ? (float)_failedCount / _totalEmergencies : 0f;

        Vector3 landingPos;

        if (failRatio <= 0f)
        {
            // Perfect landing - right outside the Pizzeria
            landingPos = perfectLandingPosition;
            Debug.Log("[TansitPod] Perfect landing! Right outside the Pizzeria.");
        }
        else
        {
            // Random direction from Pizzeria, scaled by failure ratio
            // Use seed-derived angle for determinism
            float angle = (float)(_rng.NextDouble() * Mathf.PI * 2f);
            float dist = failRatio * maxLandingOffset;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * dist,
                0f,
                Mathf.Sin(angle) * dist);

                landingPos = pizzeriaPosition + offset;
                landingPos.y = 5f; // above terrain, will fall down

                Debug.Log($"[TransitPod] Crash landing! {dist:F0}m from Pizzeria. " + $"Failures: {_failedCount}/{_totalEmergencies}");
        }

        // Pass offest to GalaxyManager
        if (GalaxyManager.Instance != null)
        {
            GalaxyManager.Instance.LandingOffset = landingPos;
            GalaxyManager.Instance.ArriveAtPlanet();
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public Queries (for UI/ other scripts)

    /// <summary>0-1 progress of background planet load.</summary>
    public float GetLoadProgress()
    {
        if (GalaxyManager.Instance?.BackgroundLoadProgress == null) return 0f;
        return GalaxyManager.Instance.BackgroundLoadProgress.progress / 0.9f;
    }

    /// <summary>How many emergencies have been resolved so far.</summary>
    public int ResolvedCount => _resolvedCount;

    /// <summary>How many emergencies have failed so far.</summary>
    public int FailedCount => _failedCount;

    /// <summary>Total emergencies this trip.</summary>
    public int TotalEmergencies => _totalEmergencies;

    #endregion
}