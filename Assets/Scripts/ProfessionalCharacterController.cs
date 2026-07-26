using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class ProfessionalCharacterController : MonoBehaviour
{
    private enum MovementState
    {
        Idle,
        Walk,
        Run,
        RunBoost,
        Jump
    }

    [Header("Visual Model")]
    [Tooltip("Animator/model child objesini buraya ver. Player root değil, sadece görsel model dönsün.")]
    [SerializeField] private Transform characterModel;

    [Header("Movement Settings")]
    [SerializeField, Min(0f)] private float walkSpeed = 3.5f;
    [SerializeField, Min(0f)] private float runSpeed = 6.5f;
    [SerializeField, Range(0.01f, 0.5f)] private float rotationSmoothTime = 0.12f;
    [SerializeField, Range(0f, 1f)] private float airControlFactor = 0.5f;

    [Header("Run Boost")]
    [SerializeField, Min(0f)] private float boostSpeed = 9f;
    [SerializeField, Min(0.01f)] private float boostRampTime = 1.5f;

    [Header("Jump Settings")]
    [SerializeField, Min(0f)] private float jumpHeight = 1.6f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Min(0.01f)] private float groundCheckRadius = 0.22f;
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.18f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Ground Normal")]
    [SerializeField, Range(0f, 89f)] private float maxGroundSlopeAngle = 55f;

    [SerializeField, Tooltip("Ground check flicker sorununu azaltır")]
    [Min(0f)] private float groundedLossBuffer = 0.05f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    [Header("Mobile Input Optional")]
    [SerializeField, Tooltip("Atanırsa joystick kullanılır, atanmazsa klavye kullanılır.")]
    private FixedJoystick joystick;

    [Header("Joystick Run Settings")]
    [SerializeField, Range(0.1f, 1f)] private float joystickMoveDeadzone = 0.15f;
    [SerializeField, Range(0.1f, 1f)] private float joystickRunThreshold = 0.65f;

    [Header("Physics Stability")]
    [SerializeField] private bool freezeAllRigidbodyRotation = true;
    [SerializeField] private bool clearAngularVelocityEveryFixedUpdate = true;
    [SerializeField] private bool clampPositiveGroundVelocityY = true;
    [SerializeField] private float groundedStickVelocity = -1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugOverlay = false;

    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimVerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int AnimJump = Animator.StringToHash("Jump");

    private Rigidbody rb;

    private Vector2 rawInput;
    private Vector3 moveDirection;

    private bool isMoving;
    private bool isRunning;
    private bool mobileRunHeld;

    private bool isGroundedRaw;
    private bool isGrounded;
    private float groundedLossTimer;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private float runHeldTime;
    private float boostBlend;
    private float visualRotationVelocity;
    private float inputAmount;

    private MovementState currentState;

    private void Awake()
    {

        if (animator == null)
        {
            if (characterModel != null)
            {
                animator = characterModel.GetComponentInChildren<Animator>();
            }
            else
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        if (characterModel == null && animator != null)
        {
             characterModel = animator.transform;
        }
        
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (freezeAllRigidbodyRotation)
        {
            rb.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY |
                RigidbodyConstraints.FreezeRotationZ;
        }

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (characterModel == null && animator != null)
            characterModel = animator.transform;

        if (characterModel == transform)
        {
            Debug.LogWarning(
                $"{nameof(ProfessionalCharacterController)}: CharacterModel Player root ile aynı. " +
                "Çarpışmada dönme sorununu çözmek için modeli child objeye alıp CharacterModel alanına o child'ı ver.",
                this
            );

            characterModel = null;
        }

        if (groundCheck == null)
        {
            Debug.LogWarning(
                $"{nameof(ProfessionalCharacterController)}: GroundCheck atanmadı.",
                this
            );
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers();
        UpdateBoost();
        UpdateState();
        HandleAnimations();
    }

    private void FixedUpdate()
    {
        GroundCheck();

        if (clearAngularVelocityEveryFixedUpdate)
            rb.angularVelocity = Vector3.zero;

        Move();
        TryConsumeJump();

        if (clearAngularVelocityEveryFixedUpdate)
            rb.angularVelocity = Vector3.zero;
    }

    private void ReadInput()
    {
        if (joystick != null)
        {
            rawInput.x = joystick.Horizontal;
            rawInput.y = joystick.Vertical;
        }
        else
        {
            rawInput.x = ReadKeyboardHorizontal();
            rawInput.y = ReadKeyboardVertical();
        }

        inputAmount = Mathf.Clamp01(rawInput.magnitude);

        Vector3 inputDirection = new Vector3(rawInput.x, 0f, rawInput.y).normalized;
        isMoving = inputAmount >= joystickMoveDeadzone;

        bool keyboardRunHeld =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed;

        bool joystickAutoRun =
            joystick != null &&
            inputAmount >= joystickRunThreshold;

        isRunning = keyboardRunHeld || mobileRunHeld || joystickAutoRun;

        if (isMoving)
        {
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;

            if (cameraTransform != null)
                targetAngle += cameraTransform.eulerAngles.y;

            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            RequestJump();
    }

    private static float ReadKeyboardHorizontal()
    {
        if (Keyboard.current == null)
            return 0f;

        float value = 0f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            value += 1f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            value -= 1f;

        return value;
    }

    private static float ReadKeyboardVertical()
    {
        if (Keyboard.current == null)
            return 0f;

        float value = 0f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            value += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            value -= 1f;

        return value;
    }

    public void OnJumpButtonPressed()
    {
        RequestJump();
    }

    public void OnRunButtonDown()
    {
        mobileRunHeld = true;
    }

    public void OnRunButtonUp()
    {
        mobileRunHeld = false;
    }

    private void RequestJump()
    {
        jumpBufferCounter = jumpBufferTime;
    }


    private void GroundCheck()
    {
    isGroundedRaw = false;

    if (groundCheck == null)
    {
        isGrounded = false;
        return;
    }

    Vector3 origin = groundCheck.position + Vector3.up * 0.05f;
    Vector3 direction = Vector3.down;

    bool hitGround = Physics.SphereCast(
        origin,
        groundCheckRadius,
        direction,
        out RaycastHit hit,
        groundCheckDistance,
        groundLayer,
        QueryTriggerInteraction.Ignore
    );

    if (hitGround)
    {
        float groundAngle = Vector3.Angle(hit.normal, Vector3.up);

        // Sadece üst yüzeye basıyorsa grounded olsun.
        // Duvara/vücuda yandan temas ederse grounded olmaz.
        isGroundedRaw = groundAngle <= maxGroundSlopeAngle;
    }

    if (isGroundedRaw)
    {
        groundedLossTimer = 0f;
        isGrounded = true;
    }
    else
    {
        groundedLossTimer += Time.fixedDeltaTime;

        if (groundedLossTimer >= groundedLossBuffer)
            isGrounded = false;
    }
    }

    

    private void UpdateTimers()
    {
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;

        jumpBufferCounter -= Time.deltaTime;
    }

    private void UpdateBoost()
    {
        bool canBuildBoost = isGrounded && isMoving && isRunning;

        if (canBuildBoost)
            runHeldTime += Time.deltaTime;
        else
            runHeldTime = 0f;

        boostBlend = Mathf.Clamp01(runHeldTime / boostRampTime);
    }

    private void Move()
    {
        Vector3 horizontalVelocity = Vector3.zero;

        if (isMoving)
        {
            RotateVisualModelToMoveDirection();

            float currentSpeed = walkSpeed;

            if (isRunning)
                currentSpeed = Mathf.Lerp(runSpeed, boostSpeed, boostBlend);

            float controlFactor = isGrounded ? 1f : airControlFactor;
            horizontalVelocity = moveDirection.normalized * (currentSpeed * controlFactor);
        }

        Vector3 velocity = rb.linearVelocity;

        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        if (clampPositiveGroundVelocityY && isGrounded && velocity.y > 0f)
            velocity.y = groundedStickVelocity;

        rb.linearVelocity = velocity;
    }

    private void RotateVisualModelToMoveDirection()
    {
        if (characterModel == null)
            return;

        float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;

        float smoothAngle = Mathf.SmoothDampAngle(
            characterModel.eulerAngles.y,
            targetAngle,
            ref visualRotationVelocity,
            rotationSmoothTime
        );

        characterModel.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
    }

    private void TryConsumeJump()
    {
        bool canJump = coyoteTimeCounter > 0f && jumpBufferCounter > 0f;

        if (!canJump)
            return;

        float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpVelocity;
        rb.linearVelocity = velocity;

        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;
        isGrounded = false;

        if (animator != null)
        {
            animator.ResetTrigger(AnimJump);
            animator.SetTrigger(AnimJump);
        }
    }

    private void UpdateState()
    {
        if (!isGrounded)
        {
            currentState = MovementState.Jump;
            return;
        }

        if (!isMoving)
        {
            currentState = MovementState.Idle;
            return;
        }

        if (isRunning && boostBlend >= 1f)
        {
            currentState = MovementState.RunBoost;
            return;
        }

        if (isRunning)
        {
            currentState = MovementState.Run;
            return;
        }

        currentState = MovementState.Walk;
    }

    private void HandleAnimations()
{
    if (animator == null)
        return;

    float animationSpeed = 0f;

    if (!isMoving)
    {
        animationSpeed = 0f;
    }
    else if (!isRunning)
    {
        animationSpeed = 0.45f;
    }
    else
    {
        animationSpeed = boostBlend >= 0.65f ? 1.25f : 0.85f;
    }

    animator.SetFloat(AnimSpeed, animationSpeed);
    animator.SetBool(AnimIsGrounded, isGrounded);
    animator.SetFloat(AnimVerticalSpeed, rb.linearVelocity.y);
}

    private void OnCollisionEnter(Collision collision)
    {
        if (clearAngularVelocityEveryFixedUpdate && rb != null)
            rb.angularVelocity = Vector3.zero;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (clearAngularVelocityEveryFixedUpdate && rb != null)
            rb.angularVelocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
    if (groundCheck == null)
        return;

    Gizmos.color = isGroundedRaw ? Color.green : Color.red;

    Vector3 origin = groundCheck.position + Vector3.up * 0.05f;
    Vector3 end = origin + Vector3.down * groundCheckDistance;

    Gizmos.DrawWireSphere(origin, groundCheckRadius);
    Gizmos.DrawWireSphere(end, groundCheckRadius);
    Gizmos.DrawLine(origin, end);
    }
   
    private void OnGUI()
    {
        if (!showDebugOverlay)
            return;

        GUI.Label(new Rect(10, 10, 500, 20), $"isGroundedRaw: {isGroundedRaw}");
        GUI.Label(new Rect(10, 30, 500, 20), $"isGrounded: {isGrounded}");
        GUI.Label(new Rect(10, 50, 500, 20), $"velocity.y: {rb.linearVelocity.y:F3}");
        GUI.Label(new Rect(10, 70, 500, 20), $"angularVelocity: {rb.angularVelocity}");
        GUI.Label(new Rect(10, 90, 500, 20), $"Speed Param: {GetAnimatorSpeedValue():F2}");
        GUI.Label(new Rect(10, 110, 500, 20), $"currentState: {currentState}");
    }

    private float GetAnimatorSpeedValue()
    {
        if (!isMoving)
            return 0f;

        if (!isRunning)
            return 0.5f;

        return Mathf.Lerp(1f, 1.5f, boostBlend);
    }
}
