using UnityEngine;

[DisallowMultipleComponent]
public class TwoPointMover : MonoBehaviour
{
    public enum StartingPoint
    {
        PointA,
        PointB
    }

    public enum MovementType
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        SmoothStep,
        SmootherStep,
        CustomCurve
    }

    private enum MovementState
    {
        WaitingAtA,
        MovingAToB,
        WaitingAtB,
        MovingBToA,
        Stopped
    }

    [Header("Movement Points")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Start Settings")]
    [SerializeField] private StartingPoint startingPoint = StartingPoint.PointA;
    [SerializeField] private bool snapToStartingPoint = true;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool moveBackAndForth = true;

    [Header("A -> B Movement")]
    [SerializeField, Min(0.01f)] private float speedAToB = 2f;
    [SerializeField] private MovementType movementTypeAToB = MovementType.EaseInOut;
    [SerializeField] private AnimationCurve customCurveAToB =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("B -> A Movement")]
    [SerializeField, Min(0.01f)] private float speedBToA = 2f;
    [SerializeField] private MovementType movementTypeBToA = MovementType.EaseInOut;
    [SerializeField] private AnimationCurve customCurveBToA =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Wait Times")]
    [SerializeField, Min(0f)] private float waitAtPointA = 0.5f;
    [SerializeField, Min(0f)] private float waitAtPointB = 0.5f;

    [Header("Physics")]
    [Tooltip("Objede Rigidbody varsa MovePosition kullanır.")]
    [SerializeField] private bool useRigidbodyMovement = true;
    [SerializeField] private bool autoConfigureRigidbody = true;

    [Header("Debug")]
    [SerializeField] private bool drawPath = true;
    [SerializeField] private Color pathColor = Color.yellow;

    private Rigidbody rb;
    private MovementState currentState;

    private Vector3 movementStartPosition;
    private Vector3 movementEndPosition;

    private MovementType activeMovementType;
    private AnimationCurve activeCustomCurve;

    private float movementProgress;
    private float movementDuration;
    private float waitTimer;
    private bool isPlaying;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ConfigureRigidbody();
    }

    private void Start()
    {
        if (!ReferencesAreValid())
        {
            enabled = false;
            return;
        }

        InitializePosition();

        if (playOnStart)
            StartMovement();
    }

    private void Update()
    {
        if (!ShouldUseRigidbody())
            Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (ShouldUseRigidbody())
            Tick(Time.fixedDeltaTime);
    }

    private void ConfigureRigidbody()
    {
        if (rb == null || !autoConfigureRigidbody)
            return;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void InitializePosition()
    {
        bool startsAtA = startingPoint == StartingPoint.PointA;
        Vector3 startPosition = startsAtA ? pointA.position : pointB.position;

        if (snapToStartingPoint)
            SetPositionImmediate(startPosition);

        currentState = startsAtA
            ? MovementState.WaitingAtA
            : MovementState.WaitingAtB;

        waitTimer = startsAtA ? waitAtPointA : waitAtPointB;
    }

    private void Tick(float deltaTime)
    {
        if (!isPlaying)
            return;

        switch (currentState)
        {
            case MovementState.WaitingAtA:
                TickWait(deltaTime, true);
                break;

            case MovementState.WaitingAtB:
                TickWait(deltaTime, false);
                break;

            case MovementState.MovingAToB:
            case MovementState.MovingBToA:
                TickMovement(deltaTime);
                break;
        }
    }

    private void TickWait(float deltaTime, bool waitingAtA)
    {
        waitTimer -= deltaTime;

        if (waitTimer > 0f)
            return;

        if (waitingAtA)
            BeginMoveAToB();
        else
            BeginMoveBToA();
    }

    private void BeginMoveAToB()
    {
        BeginMovement(
            pointB.position,
            speedAToB,
            movementTypeAToB,
            customCurveAToB,
            MovementState.MovingAToB
        );
    }

    private void BeginMoveBToA()
    {
        BeginMovement(
            pointA.position,
            speedBToA,
            movementTypeBToA,
            customCurveBToA,
            MovementState.MovingBToA
        );
    }

    private void BeginMovement(
        Vector3 destination,
        float speed,
        MovementType movementType,
        AnimationCurve customCurve,
        MovementState newState)
    {
        movementStartPosition = GetCurrentPosition();
        movementEndPosition = destination;

        float distance = Vector3.Distance(
            movementStartPosition,
            movementEndPosition
        );

        movementDuration = distance / Mathf.Max(speed, 0.01f);
        movementDuration = Mathf.Max(movementDuration, 0.001f);

        activeMovementType = movementType;
        activeCustomCurve = customCurve;
        movementProgress = 0f;
        currentState = newState;
    }

    private void TickMovement(float deltaTime)
    {
        movementProgress += deltaTime / movementDuration;
        movementProgress = Mathf.Clamp01(movementProgress);

        float transitionValue = EvaluateMovement(
            movementProgress,
            activeMovementType,
            activeCustomCurve
        );

        Vector3 nextPosition = Vector3.LerpUnclamped(
            movementStartPosition,
            movementEndPosition,
            transitionValue
        );

        ApplyPosition(nextPosition);

        if (movementProgress >= 1f)
            CompleteMovement();
    }

    private static float EvaluateMovement(
        float t,
        MovementType movementType,
        AnimationCurve customCurve)
    {
        t = Mathf.Clamp01(t);

        switch (movementType)
        {
            case MovementType.EaseIn:
                return t * t;

            case MovementType.EaseOut:
                return 1f - (1f - t) * (1f - t);

            case MovementType.EaseInOut:
                return t < 0.5f
                    ? 2f * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            case MovementType.SmoothStep:
                return t * t * (3f - 2f * t);

            case MovementType.SmootherStep:
                return t * t * t * (t * (6f * t - 15f) + 10f);

            case MovementType.CustomCurve:
                return customCurve != null
                    ? customCurve.Evaluate(t)
                    : t;

            case MovementType.Linear:
            default:
                return t;
        }
    }

    private void CompleteMovement()
    {
        SetPositionImmediate(movementEndPosition);

        if (currentState == MovementState.MovingAToB)
        {
            if (!moveBackAndForth)
            {
                StopMovement();
                return;
            }

            currentState = MovementState.WaitingAtB;
            waitTimer = waitAtPointB;
        }
        else
        {
            currentState = MovementState.WaitingAtA;
            waitTimer = waitAtPointA;
        }
    }

    private void ApplyPosition(Vector3 position)
    {
        if (ShouldUseRigidbody())
            rb.MovePosition(position);
        else
            transform.position = position;
    }

    private void SetPositionImmediate(Vector3 position)
    {
        if (rb != null)
            rb.position = position;

        transform.position = position;
    }

    private Vector3 GetCurrentPosition()
    {
        return ShouldUseRigidbody()
            ? rb.position
            : transform.position;
    }

    private bool ShouldUseRigidbody()
    {
        return useRigidbodyMovement && rb != null;
    }

    private bool ReferencesAreValid()
    {
        if (pointA != null && pointB != null)
            return true;

        Debug.LogError(
            $"{name}: Point A ve Point B alanlarını atamalısın.",
            this
        );

        return false;
    }

    public void StartMovement()
    {
        if (!ReferencesAreValid())
            return;

        isPlaying = true;

        if (currentState == MovementState.Stopped)
            InitializePosition();

        if (currentState == MovementState.WaitingAtA)
            BeginMoveAToB();
        else if (currentState == MovementState.WaitingAtB)
            BeginMoveBToA();
    }

    public void PauseMovement()
    {
        isPlaying = false;
    }

    public void ResumeMovement()
    {
        isPlaying = true;
    }

    public void StopMovement()
    {
        isPlaying = false;
        currentState = MovementState.Stopped;
    }

    public void ResetMovement()
    {
        isPlaying = false;
        InitializePosition();
    }

    public void SetForwardSpeed(float newSpeed)
    {
        speedAToB = Mathf.Max(0.01f, newSpeed);
    }

    public void SetReturnSpeed(float newSpeed)
    {
        speedBToA = Mathf.Max(0.01f, newSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawPath || pointA == null || pointB == null)
            return;

        Gizmos.color = pathColor;
        Gizmos.DrawLine(pointA.position, pointB.position);
        Gizmos.DrawWireSphere(pointA.position, 0.15f);
        Gizmos.DrawWireSphere(pointB.position, 0.15f);
    }
}