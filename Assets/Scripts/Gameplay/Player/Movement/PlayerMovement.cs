using UnityEngine;

/// <summary>
/// A "Funky" first-person player controller.
/// Add procedural bobbing, waddling, and squash/stretch effects to the base movement.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.8f;
    
    [Header("Bounciness & Bobbing")]
    [Tooltip("How fast the character bobs (rhythm of the funk).")]
    [SerializeField] private float bobFrequency = 10f;
    [Tooltip("How much the character's BODY bobs up and down (Visible to others).")]
    [SerializeField] private float meshBobAmplitude = 0.15f;
    [Tooltip("How much the CAMERA bobs. Keep this VERY low (e.g., 0.02) for First Person.")]
    [SerializeField] private float cameraBobAmplitude = 0.02f;
    [Tooltip("The side-to-side 'waddle' rotation intensity.")]
    [SerializeField] private float tiltAmount = 3f;
    [Tooltip("Adjusts the base height of the bobbing (useful for floating characters).")]
    [SerializeField] private float bobHeightOffset = 0.5f; 

    [Header("Visual Juice")]
    [Tooltip("The 3D model/mesh child object that will jiggle. DO NOT use the root player object.")]
    [SerializeField] private Transform visualMesh;
    [Tooltip("How quickly the mesh returns to its original shape.")]
    [SerializeField] private float squashElasticity = 10f;

    [Header("Physics")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float stepHeight = 0.5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private Transform cameraTransform;

    // --- Private Internal State ---
    private CharacterController controller;
    private Vector3 velocity; // Vertical physics velocity
    private float xRotation; // Pitch (Up/Down)
    private float yRotation; // Yaw (Left/Right)
    private float bobTimer; // Tracks the progress of the sine wave
    private Vector3 defaultMeshScale; // Original scale of the model
    private Vector3 targetMeshScale; // The scale we are currently "jiggling" towards

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.stepOffset = stepHeight;

        // Store the original scale so we can return to it after a "squish"
        if (visualMesh != null)
        {
            defaultMeshScale = visualMesh.localScale;
            targetMeshScale = defaultMeshScale;
        }

        // Auto-assign camera if empty
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // If I left-click...
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
        HandleMovement();
        ApplyFunkyVisuals(); // This creates the Wobbly Life flavor
    }
    
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        // Landing logic
        if (isGrounded && velocity.y < 0f)
        {
            // Squash: If falling fast, flatten the mesh briefly upon impact
            if (velocity.y < -5f)
                targetMeshScale = new Vector3(defaultMeshScale.x * 1.2f, defaultMeshScale.y * 0.7f, defaultMeshScale.z * 1.2f);

            velocity.y = -2f;
        }

        // Input processing
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = transform.right * inputX + transform.forward * inputZ;

        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        float currentSpeed = walkSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        // 1. Jump logic (ONLY handles the upward boost)
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            targetMeshScale = new Vector3(defaultMeshScale.x * 0.8f, defaultMeshScale.y * 1.3f, defaultMeshScale.z * 0.8f);
        }

        // 2. Apply gravity (Runs every frame)
        velocity.y -= gravity * Time.deltaTime;

        // 3. Calculate move (Runs every frame)
        Vector3 finalMove = (moveDirection * currentSpeed + velocity) * Time.deltaTime;

        // 4. Actually Move (Runs every frame)
        controller.Move(finalMove);
    }

    /// <summary>
    /// Handles the procedural "Wobble" and "Squish" effects.
    /// </summary>
    private void ApplyFunkyVisuals()
    {
        if (visualMesh == null) return;

        // Get current horizontal speed
        float horizontalSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;

        if (horizontalSpeed > 0.1f && controller.isGrounded)
        {
            // Calculate how fast we are moving relative to normal walk speed
            // This creates a multiplier (e.g., 1.0 for walking, 1.8 for sprinting)
            float speedIntensity = horizontalSpeed / walkSpeed;

            // Multiply the frequency (speed of jiggle) and amplitudes (height of jiggle)
            bobTimer += Time.deltaTime * (bobFrequency * speedIntensity);
            
            // Apply intensity to the body bob
            float meshBob = Mathf.Sin(bobTimer) * (meshBobAmplitude * speedIntensity);
            visualMesh.localPosition = new Vector3(0, meshBob + bobHeightOffset, 0);

            // Apply intensity to the camera bob
            if (cameraTransform != null)
            {
                float camBob = Mathf.Sin(bobTimer) * (cameraBobAmplitude * speedIntensity);
                cameraTransform.localPosition = new Vector3(0, 0.8f + camBob, 0);
            }
            
            // Make the waddle tilt more aggressive when running
            float tilt = Mathf.Cos(bobTimer) * (tiltAmount * speedIntensity);
            visualMesh.localRotation = Quaternion.Euler(0, 0, tilt);
        }
        else
        {
            // RESET logic when standing still
            bobTimer = 0;
            
            // Smoothly return body to base height
            Vector3 targetMeshPos = new Vector3(0, bobHeightOffset, 0);
            visualMesh.localPosition = Vector3.Lerp(visualMesh.localPosition, targetMeshPos, Time.deltaTime * 5f);
            visualMesh.localRotation = Quaternion.Lerp(visualMesh.localRotation, Quaternion.identity, Time.deltaTime * 5f);

            // Smoothly return camera to eye level
            if (cameraTransform != null)
            {
                Vector3 targetCamPos = new Vector3(0, 0.8f, 0); 
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetCamPos, Time.deltaTime * 5f);
            }
        }
    }   
}