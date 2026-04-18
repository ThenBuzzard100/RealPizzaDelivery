using UnityEngine;

/// <summary>
/// First-person player controller using CharacterController.
/// Supports WASD movement, mouse look, jumping, sprinting, step climbing, and gravity.
///
/// Setup:
///   1. Attach this script to your Player GameObject.
///   2. Add a CharacterController component to the same GameObject.
///   3. Make the Main Camera a CHILD of this GameObject (first-person view).
///      Position the camera at roughly eye height (e.g. local Y = 0.6).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Normal walking speed in units/sec.")]
    [SerializeField] private float walkSpeed = 5f;

    [Tooltip("Speed multiplier while holding Sprint.")]
    [SerializeField] private float sprintMultiplier = 1.8f;

    [Header("Jumping")]
    [Tooltip("How high the player can jump.")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Tooltip("Gravity magnitude (positive value, applied downward).")]
    [SerializeField] private float gravity = 20f;

    [Header("Step Climbing")]
    [Tooltip("Max obstacle height the player can step over without jumping.")]
    [SerializeField] private float stepHeight = 0.5f;

    [Header("Mouse Look")]
    [Tooltip("Mouse sensitivity for camera rotation.")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Tooltip("Maximum vertical look angle (degrees).")]
    [SerializeField] private float maxLookAngle = 80f;

    [Tooltip("Reference to the camera transform. Auto-finds Main Camera if left empty.")]
    [SerializeField] private Transform cameraTransform;

    // ── Private State ──────────────────────────────────────────
    private CharacterController controller;
    private Vector3 velocity;          // tracks vertical velocity (gravity + jump)
    private float xRotation;           // accumulated pitch for vertical mouse look
    private float yRotation;           // accumulated yaw for horizontal mouse look

    // ── Unity Lifecycle ────────────────────────────────────────

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Apply step height to the CharacterController
        controller.stepOffset = stepHeight;

        // Grab the main camera if none was assigned in the Inspector
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
            else
                Debug.LogWarning("PlayerMovement: No camera assigned and no MainCamera found.");
        }
    }

    private void Start()
    {
        // Lock and hide the cursor for FPS-style control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw from the player's current facing direction
        yRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    // ── Mouse Look ─────────────────────────────────────────────

    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Accumulate horizontal rotation (yaw) — rotates the whole player body
        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Accumulate vertical rotation (pitch) — tilts the camera up/down, clamped
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    // ── Movement & Gravity ─────────────────────────────────────

    private void HandleMovement()
    {
        // --- Grounded check ---
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
        {
            // Small downward nudge keeps the controller snapped to the ground
            velocity.y = -2f;
        }

        // --- Input axes ---
        float inputX = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float inputZ = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

        // Build a direction relative to where the player is facing
        Vector3 moveDirection = transform.right * inputX + transform.forward * inputZ;

        // Clamp so diagonal movement isn't faster
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // --- Sprint ---
        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= sprintMultiplier;

        // --- Jump ---
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            // v = sqrt(2 * g * h) — physics formula for jump velocity
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        // --- Gravity ---
        velocity.y -= gravity * Time.deltaTime;

        // --- Apply all movement in one call ---
        Vector3 finalMove = (moveDirection * currentSpeed + velocity) * Time.deltaTime;
        controller.Move(finalMove);
    }
}
