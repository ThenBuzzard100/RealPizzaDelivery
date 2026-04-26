using UnityEngine;

// [MIRROR] This imports the Mirror networking library.
// Every Mirror feature — NetworkBehaviour, SyncVar, Command, isLocalPlayer, etc —
// lives in this namespace. Without this line, none of the multiplayer code compiles.
using Mirror;

/// <summary>
/// A "Funky" first-person player controller — now with Mirror multiplayer support.
/// Add procedural bobbing, waddling, and squash/stretch effects to the base movement.
/// Only the local player processes input and controls the camera/visuals.
/// </summary>
[RequireComponent(typeof(CharacterController))]

// [MIRROR] NetworkBehaviour replaces MonoBehaviour as the base class.
// It gives us access to all Mirror features:
//   - isLocalPlayer      : is this object owned by the local client?
//   - isServer           : is this code running on the server?
//   - [SyncVar]          : automatically sync a variable from server → all clients
//   - [Command]          : run a method on the server, called from a client
//   - [ClientRpc]        : run a method on all clients, called from the server
//   - OnStartLocalPlayer : lifecycle callback that fires only for our own player
//   - OnStartClient      : lifecycle callback that fires for every player object
// Without NetworkBehaviour, none of those things exist.
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxWalkSpeed = 5f;
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
    private Vector3 velocity;         // Vertical physics velocity
    private float currentSpeed;
    private float xRotation;          // Pitch (Up/Down)
    private float yRotation;          // Yaw (Left/Right)
    private float bobTimer;           // Tracks the progress of the sine wave
    private Vector3 defaultMeshScale; // Original scale of the model
    private Vector3 targetMeshScale;  // The scale we are currently "jiggling" towards

    // [MIRROR] SyncVar — a special Mirror attribute that makes a variable
    // automatically replicated from the SERVER to ALL CLIENTS whenever its value changes.
    //
    // Why we need these two variables:
    //   ApplyFunkyVisuals() needs to know how fast each player is moving so it can
    //   drive the bob animation. Normally you'd just read controller.velocity, but
    //   that only works on the machine that is physically running the CharacterController.
    //   On every OTHER client, the CharacterController isn't being moved locally —
    //   Mirror just syncs the transform position via NetworkTransform. So controller.velocity
    //   reads as zero on remote players, the bob condition never triggers, and nobody
    //   sees any bobbing.
    //
    // The fix:
    //   The local player writes their real speed and grounded state into these variables
    //   every frame (via a [Command] — see CmdUpdateMovementState below).
    //   Mirror then replicates the new values to every client automatically.
    //   ApplyFunkyVisuals() reads from these SyncVars instead of controller.velocity,
    //   so it has correct data for every player on every machine.
    [SyncVar] private float syncedHorizontalSpeed;
    [SyncVar] private bool syncedIsGrounded;

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

    // [MIRROR] OnStartLocalPlayer() is a Mirror lifecycle callback.
    // Mirror calls this automatically once, on the ONE client whose player this is,
    // at the moment their player object is spawned into the scene.
    // It does NOT fire on any other client's machine for this object.
    //
    // Think of it as Start() but with the guarantee that isLocalPlayer == true.
    // This is the right place for one-time setup that should only happen for our
    // own player: locking the cursor, initialising the camera angle, etc.
    public override void OnStartLocalPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yRotation = transform.eulerAngles.y;

        // [MIRROR] Every player prefab in the scene has a camera as a child object.
        // If we left all of them active, every client would render the scene from
        // multiple cameras simultaneously — one for each player in the game.
        // By only activating the camera here, inside OnStartLocalPlayer(), we ensure
        // that each client only ever renders through their own eyes. All other players'
        // cameras are disabled in OnStartClient() below.
        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(true);
    }

    // [MIRROR] OnStartClient() is a Mirror lifecycle callback.
    // Unlike OnStartLocalPlayer(), this fires for EVERY player object on EVERY client —
    // including objects that belong to other players. This makes it the right place
    // to handle setup that needs to treat remote players differently from the local one.
    //
    // Here we use it to disable cameras on player objects that don't belong to us.
    // Without this, every spawned player object would have an active camera fighting
    // for control of the screen.
    public override void OnStartClient()
    {
        // [MIRROR] isLocalPlayer is a Mirror built-in bool.
        // It is TRUE only on the one client who owns this particular player object.
        // On every other client's machine it is FALSE for this same object.
        // We check !isLocalPlayer here so we skip our own object and only disable
        // cameras that belong to other players.
        if (!isLocalPlayer && cameraTransform != null)
            cameraTransform.gameObject.SetActive(false);
    }

    private void Update()
    {
        // [MIRROR] ApplyFunkyVisuals() is called BEFORE the isLocalPlayer guard below.
        // This is intentional — it means the bob/waddle animation runs for every player
        // object on screen, not just our own. Each remote player's bob is driven by
        // the SyncVars above which Mirror keeps up to date. If this call were placed
        // after the guard, remote players would never animate and would slide around
        // stiffly with no bobbing visible.
        ApplyFunkyVisuals(); // This creates the Wobbly Life flavor

        // [MIRROR] isLocalPlayer guard.
        // Everything below this line — reading input, moving the character, rotating
        // the camera — must ONLY run for the player object that belongs to us.
        //
        // Each client in the game has a copy of every player object in the scene.
        // Without this guard, your keyboard would move ALL of those objects, not
        // just yours. The guard makes sure each client only drives their own character.
        if (!isLocalPlayer) return;

        if (Input.GetMouseButtonDown(0)) // If I left-click...
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
        HandleMovement();
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
        // todo: make sure input actions are being used properly
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = transform.right * inputX + transform.forward * inputZ;

        if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();

        // todo: make acceleration and deacceleration work
        currentSpeed += .1f;
        if (currentSpeed > maxWalkSpeed)
        {
            currentSpeed = maxWalkSpeed;
        }

        // 1. Jump logic (ONLY handles the upward boost)
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            targetMeshScale = new Vector3(defaultMeshScale.x * 0.8f, defaultMeshScale.y * 1.3f, defaultMeshScale.z * 0.8f);
        }

        // 2. Apply gravity (Runs every frame)
        // Note: if you want to instantly change velocity, then you have to change v0 & v1 at the same time
        float v0_vertical = velocity.y; // Initial vertical velocity
        velocity.y -= gravity * Time.deltaTime; // Apply gravity
        float v1_vertical = velocity.y; // Final vertical velocity

        // 3. Calculate move (Runs every frame and applies sprintMultiplier if sprint button is pressed)
        // D = (V0 + V1) * 0.5 (average velocity formula)
        float horizontalSpeed = currentSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        float v0_horizontal = horizontalSpeed; // Initial horizontal velocity
        float v1_horizontal = horizontalSpeed; // Final horizontal velocity (unchanged mid-frame)

        Vector3 horizontalMove = moveDirection * (v0_horizontal + v1_horizontal) * 0.5f * Time.deltaTime;
        float verticalMove = (v0_vertical + v1_vertical) * 0.5f * Time.deltaTime;
        Vector3 finalMove = new Vector3(horizontalMove.x, verticalMove, horizontalMove.z);

        // 4. Actually Move (Runs every frame)
        controller.Move(finalMove);

        // [MIRROR] After moving, we need to tell all other clients how fast we are
        // moving so they can animate our bob correctly. We do this by calling a
        // [Command], which sends the data up to the server.
        //
        // Why not just write to the SyncVars directly here?
        //   SyncVars can ONLY be written on the server. The host can write them
        //   directly because the host IS the server. But a joining client is not —
        //   if a client writes a SyncVar directly it just changes their local copy
        //   and Mirror never sees it, so nobody else ever receives the update.
        //   The [Command] is the correct bridge: client calls it → server runs it →
        //   server updates the SyncVar → Mirror pushes the new value to all clients.
        CmdUpdateMovementState(
            new Vector2(controller.velocity.x, controller.velocity.z).magnitude,
            controller.isGrounded
        );
    }

    // [MIRROR] [Command] — this method does NOT run on the machine that calls it.
    // When our local player calls CmdUpdateMovementState(), Mirror intercepts the call
    // and executes it on the SERVER instead. The server then writes to the SyncVars,
    // and Mirror automatically replicates those updated values out to every connected client.
    //
    // Full data flow for the bob sync:
    //
    //   1. Local client moves their character (HandleMovement runs)
    //   2. Local client calls CmdUpdateMovementState() with their real speed
    //   3. Mirror sends that call across the network to the server
    //   4. Server runs CmdUpdateMovementState() and writes to syncedHorizontalSpeed / syncedIsGrounded
    //   5. Mirror sees the SyncVar changed and replicates the new values to all clients
    //   6. All clients now have the correct speed, so ApplyFunkyVisuals() bobs correctly
    //
    // The "Cmd" prefix is a Mirror naming convention for Commands. It's not required,
    // but it makes it immediately obvious at a glance that this method runs on the server.
    [Command]
    private void CmdUpdateMovementState(float speed, bool grounded)
    {
        // These assignments happen on the SERVER.
        // Mirror detects the SyncVar change and pushes the new values to all clients automatically.
        syncedHorizontalSpeed = speed;
        syncedIsGrounded = grounded;
    }

    /// <summary>
    /// Handles the procedural "Wobble" and "Squish" effects.
    /// </summary>
    private void ApplyFunkyVisuals()
    {
        if (visualMesh == null) return;

        // [MIRROR] We read from the SyncVars here instead of controller.velocity directly.
        // On the local player these SyncVars are always fresh because CmdUpdateMovementState
        // is called every frame in HandleMovement(). On remote players, Mirror keeps these
        // SyncVars up to date automatically whenever the server pushes a new value.
        // Reading controller.velocity here would always be zero for remote players because
        // their CharacterController is never driven locally — NetworkTransform just moves
        // the transform directly, it doesn't feed the CharacterController.
        float horizontalSpeed = syncedHorizontalSpeed;
        bool isGrounded = syncedIsGrounded;

        if (horizontalSpeed > 0.1f && isGrounded)
        {
            // Calculate how fast we are moving relative to normal walk speed
            // This creates a multiplier (e.g., 1.0 for walking, 1.8 for sprinting)
            float speedIntensity = horizontalSpeed / maxWalkSpeed;

            // Multiply the frequency (speed of jiggle) and amplitudes (height of jiggle)
            bobTimer += Time.deltaTime * (bobFrequency * speedIntensity);
            
            // Apply intensity to the body bob
            float meshBob = Mathf.Sin(bobTimer) * (meshBobAmplitude * speedIntensity);
            visualMesh.localPosition = new Vector3(0, meshBob + bobHeightOffset, 0);

            // [MIRROR] isLocalPlayer guard on the camera bob.
            // ApplyFunkyVisuals() runs for ALL player objects on our machine, but we
            // only want to physically move the camera for our own player object.
            // On a remote player's object, cameraTransform still exists but its
            // GameObject is disabled (handled in OnStartClient above). Moving a disabled
            // object's transform is harmless, but it's unnecessary and confusing, so
            // we skip it for anyone who isn't us.
            if (isLocalPlayer && cameraTransform != null)
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

            // [MIRROR] Same isLocalPlayer guard as above — only reset our own camera
            // back to eye level. No need to touch cameras belonging to remote players.
            if (isLocalPlayer && cameraTransform != null)
            {
                Vector3 targetCamPos = new Vector3(0, 0.8f, 0); 
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetCamPos, Time.deltaTime * 5f);
            }
        }
    }   
}