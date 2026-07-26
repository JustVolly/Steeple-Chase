using UnityEngine;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public class HammerOrbitAroundBody : MonoBehaviour
{
    public enum MotionMode
    {
        ContinuousOrbit,
        PingPongBetweenAngles,
        GoToTargetAngle
    }

    public enum HammerRotationMode
    {
        FollowCircleTangent,
        MatchBodyRotation,
        LookAtBody,
        LookAwayFromBody,
        KeepInitialRotation
    }

    [Header("Body Reference")]
    [Tooltip("BodyCircumferenceDrawer bulunan Gövde objesini ver.")]
    [SerializeField] private Transform bodyObject;

    [Tooltip("Gövde üzerindeki BodyCircumferenceDrawer componenti.")]
    [SerializeField] private BodyCircumferenceDrawer orbitPath;

    [Header("Orbit Distance")]
    [Tooltip("Hammer'ı gövde yüzeyinden dışarı taşır.")]
    [SerializeField, Min(0f)] private float radialClearance = 0.15f;

    [Tooltip("BodyCircumferenceDrawer yüksekliğine ek yükseklik uygular.")]
    [SerializeField] private float additionalHeightOffset = 0f;

    [Header("Motion")]
    [SerializeField] private MotionMode motionMode =
        MotionMode.ContinuousOrbit;

    [SerializeField, Min(1f)]
    [Tooltip("Derece/saniye.")]
    private float angularSpeed = 120f;

    [Header("Mirror")]
    [Tooltip(
        "Açıldığında bütün açılar Mirror Center Angle değerine göre ayna karşılığına çevrilir."
    )]
    [SerializeField] private bool mirrorOrbit = false;

    [Tooltip(
        "Aynalama merkez açısı. Genellikle 0 bırakılır. " +
        "Örneğin 0 etrafında 60 derece, -60 dereceye aynalanır."
    )]
    [SerializeField] private float mirrorCenterAngle = 0f;

    [Header("Continuous Orbit")]
    [SerializeField] private bool clockwise = true;

    [Header("Ping Pong")]
    [SerializeField] private float minAngle = -180f;
    [SerializeField] private float maxAngle = 180f;

    [SerializeField, Min(0f)]
    private float waitAtEnds = 0f;

    [SerializeField] private bool startFromMinAngle = true;

    [Header("Target Angle")]
    [SerializeField] private float targetAngle = 90f;

    [Header("Hammer Rotation")]
    [SerializeField] private HammerRotationMode rotationMode =
        HammerRotationMode.FollowCircleTangent;

    [Tooltip("Model yanlış yöne bakıyorsa buradan düzelt.")]
    [SerializeField] private Vector3 rotationOffsetEuler =
        Vector3.zero;

    [Header("Start")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private float startAngle = 0f;

    [Header("Physics")]
    [Tooltip(
        "İlk testte kapalı bırak. Açıldığında kinematic Rigidbody üzerinden hareket eder."
    )]
    [SerializeField] private bool useRigidbodyMotion = false;

    [SerializeField] private bool autoConfigureRigidbody = true;

    [Tooltip("Hammer Rigidbody üzerindeki Freeze constraintlerini kaldırır.")]
    [SerializeField] private bool clearRigidbodyConstraints = true;

    [Header("Runtime Debug")]
    [SerializeField] private bool drawDebug = true;

    [Tooltip("Inspector'da mantıksal açıyı gösterir.")]
    [SerializeField] private float currentAngle;

    [Tooltip("Mirror uygulandıktan sonra çevrede kullanılan gerçek açıyı gösterir.")]
    [SerializeField] private float effectiveOrbitAngle;

    [SerializeField] private bool isActive;

    private Rigidbody rb;
    private Quaternion initialWorldRotation;

    private float pingPongDirection = 1f;
    private float movementDirection = 1f;
    private float waitTimer;

    private bool isWaiting;
    private bool initialized;
    private bool warningPrinted;

    public float CurrentAngle => currentAngle;
    public float EffectiveOrbitAngle => GetEffectiveOrbitAngle(currentAngle);
    public bool IsActive => isActive;
    public bool MirrorOrbit => mirrorOrbit;

    private void Reset()
    {
        ResolveOrbitPath();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        ResolveOrbitPath();
        ConfigureRigidbody();

        initialWorldRotation = transform.rotation;

        InitializeMovement();
        initialized = true;

        ApplyOrbit();

        if (startOnAwake)
            StartOrbit();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying || !initialized)
            return;

        if (startOnAwake)
            StartOrbit();
    }

    private void OnValidate()
    {
        angularSpeed = Mathf.Max(1f, angularSpeed);
        radialClearance = Mathf.Max(0f, radialClearance);

        if (maxAngle < minAngle)
        {
            float oldMin = minAngle;
            minAngle = maxAngle;
            maxAngle = oldMin;
        }

        effectiveOrbitAngle = GetEffectiveOrbitAngle(currentAngle);

        if (orbitPath == null)
            ResolveOrbitPath();
    }

    private void Update()
    {
        if (useRigidbodyMotion && rb != null)
            return;

        UpdateMovement(Time.deltaTime);

        // Açı sabit olsa bile Gövde rotation değişimini takip eder.
        ApplyOrbit();
    }

    private void FixedUpdate()
    {
        if (!useRigidbodyMotion || rb == null)
            return;

        UpdateMovement(Time.fixedDeltaTime);
        ApplyOrbit();
    }

    private void ResolveOrbitPath()
    {
        if (orbitPath != null)
            return;

        if (bodyObject != null)
        {
            orbitPath =
                bodyObject.GetComponent<BodyCircumferenceDrawer>();

            if (orbitPath == null)
            {
                orbitPath =
                    bodyObject.GetComponentInChildren<BodyCircumferenceDrawer>(
                        true
                    );
            }
        }

        // Gövde ve Hammer aynı Obstacle altında sibling ise otomatik bulur.
        if (orbitPath == null && transform.parent != null)
        {
            orbitPath =
                transform.parent.GetComponentInChildren<BodyCircumferenceDrawer>(
                    true
                );
        }
    }

    private void ConfigureRigidbody()
    {
        if (rb == null || !autoConfigureRigidbody)
            return;

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        if (clearRigidbodyConstraints)
            rb.constraints = RigidbodyConstraints.None;
    }

    private void InitializeMovement()
    {
        isWaiting = false;
        waitTimer = 0f;

        switch (motionMode)
        {
            case MotionMode.ContinuousOrbit:
                currentAngle = startAngle;
                movementDirection = clockwise ? -1f : 1f;
                break;

            case MotionMode.PingPongBetweenAngles:
                currentAngle =
                    startFromMinAngle ? minAngle : startAngle;

                pingPongDirection =
                    Mathf.Approximately(currentAngle, maxAngle)
                        ? -1f
                        : 1f;

                movementDirection = pingPongDirection;
                break;

            case MotionMode.GoToTargetAngle:
                currentAngle = startAngle;

                movementDirection =
                    Mathf.Sign(targetAngle - currentAngle);

                if (Mathf.Approximately(movementDirection, 0f))
                    movementDirection = 1f;

                break;
        }

        effectiveOrbitAngle =
            GetEffectiveOrbitAngle(currentAngle);
    }

    private void UpdateMovement(float deltaTime)
    {
        if (!isActive)
            return;

        if (!PathIsValid())
            return;

        switch (motionMode)
        {
            case MotionMode.ContinuousOrbit:
                UpdateContinuous(deltaTime);
                break;

            case MotionMode.PingPongBetweenAngles:
                UpdatePingPong(deltaTime);
                break;

            case MotionMode.GoToTargetAngle:
                UpdateTargetAngle(deltaTime);
                break;
        }
    }

    private void UpdateContinuous(float deltaTime)
    {
        movementDirection = clockwise ? -1f : 1f;

        currentAngle +=
            movementDirection *
            angularSpeed *
            deltaTime;

        if (Mathf.Abs(currentAngle) >= 3600f)
            currentAngle %= 360f;
    }

    private void UpdatePingPong(float deltaTime)
    {
        if (isWaiting)
        {
            waitTimer -= deltaTime;

            if (waitTimer <= 0f)
                isWaiting = false;

            return;
        }

        float destination =
            pingPongDirection > 0f
                ? maxAngle
                : minAngle;

        movementDirection =
            Mathf.Sign(destination - currentAngle);

        if (Mathf.Approximately(movementDirection, 0f))
            movementDirection = pingPongDirection;

        currentAngle = Mathf.MoveTowards(
            currentAngle,
            destination,
            angularSpeed * deltaTime
        );

        if (Mathf.Abs(currentAngle - destination) <= 0.001f)
        {
            currentAngle = destination;

            pingPongDirection *= -1f;
            movementDirection = pingPongDirection;

            if (waitAtEnds > 0f)
            {
                isWaiting = true;
                waitTimer = waitAtEnds;
            }
        }
    }

    private void UpdateTargetAngle(float deltaTime)
    {
        float difference =
            targetAngle - currentAngle;

        if (Mathf.Abs(difference) <= 0.001f)
        {
            currentAngle = targetAngle;
            return;
        }

        movementDirection = Mathf.Sign(difference);

        currentAngle = Mathf.MoveTowards(
            currentAngle,
            targetAngle,
            angularSpeed * deltaTime
        );
    }

    private void ApplyOrbit()
    {
        if (!PathIsValid())
            return;

        effectiveOrbitAngle =
            GetEffectiveOrbitAngle(currentAngle);

        Vector3 targetPosition =
            orbitPath.GetWorldPoint(
                effectiveOrbitAngle,
                radialClearance
            );

        targetPosition +=
            orbitPath.WorldPlaneNormal *
            additionalHeightOffset;

        Quaternion targetRotation =
            CalculateHammerRotation(effectiveOrbitAngle);

        if (useRigidbodyMotion && rb != null)
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(targetRotation);
        }
        else
        {
            transform.SetPositionAndRotation(
                targetPosition,
                targetRotation
            );
        }
    }

    private float GetEffectiveOrbitAngle(float sourceAngle)
    {
        if (!mirrorOrbit)
            return sourceAngle;

        // Bir açının belirlenen merkez etrafındaki ayna karşılığı:
        // mirrored = 2 * center - original
        return 2f * mirrorCenterAngle - sourceAngle;
    }

    private float GetEffectiveMovementDirection()
    {
        float direction = movementDirection;

        if (Mathf.Approximately(direction, 0f))
            direction = 1f;

        // Ayna dönüşümünde teğet yön de tersine döner.
        if (mirrorOrbit)
            direction *= -1f;

        return direction;
    }

    private Quaternion CalculateHammerRotation(float orbitAngle)
    {
        Quaternion rotationOffset =
            Quaternion.Euler(rotationOffsetEuler);

        float effectiveDirection =
            GetEffectiveMovementDirection();

        switch (rotationMode)
        {
            case HammerRotationMode.FollowCircleTangent:
                return orbitPath.GetWorldTangentRotation(
                           orbitAngle,
                           effectiveDirection
                       ) * rotationOffset;

            case HammerRotationMode.MatchBodyRotation:
                return orbitPath.BodyWorldRotation *
                       rotationOffset;

            case HammerRotationMode.LookAtBody:
                return orbitPath.GetWorldRadialRotation(
                           orbitAngle,
                           true
                       ) * rotationOffset;

            case HammerRotationMode.LookAwayFromBody:
                return orbitPath.GetWorldRadialRotation(
                           orbitAngle,
                           false
                       ) * rotationOffset;

            case HammerRotationMode.KeepInitialRotation:
                return initialWorldRotation *
                       rotationOffset;

            default:
                return orbitPath.GetWorldTangentRotation(
                           orbitAngle,
                           effectiveDirection
                       ) * rotationOffset;
        }
    }

    private bool PathIsValid()
    {
        if (orbitPath != null)
        {
            warningPrinted = false;
            return true;
        }

        ResolveOrbitPath();

        if (orbitPath != null)
        {
            warningPrinted = false;
            return true;
        }

        if (!warningPrinted)
        {
            Debug.LogError(
                $"{name}: BodyCircumferenceDrawer bulunamadı. " +
                "Body Object alanına Gövdeyi veya Orbit Path alanına " +
                "BodyCircumferenceDrawer componentini ver.",
                this
            );

            warningPrinted = true;
        }

        return false;
    }

    public void StartOrbit()
    {
        isActive = true;
    }

    public void StopOrbit()
    {
        isActive = false;
    }

    public void ResetOrbit()
    {
        isActive = false;

        InitializeMovement();
        ApplyOrbit();
    }

    public void SetTargetAngle(float newTargetAngle)
    {
        targetAngle = newTargetAngle;
        motionMode = MotionMode.GoToTargetAngle;

        isWaiting = false;
        isActive = true;
    }

    public void SetPingPongAngles(
        float newMinAngle,
        float newMaxAngle)
    {
        minAngle = Mathf.Min(newMinAngle, newMaxAngle);
        maxAngle = Mathf.Max(newMinAngle, newMaxAngle);

        currentAngle = minAngle;
        pingPongDirection = 1f;
        movementDirection = 1f;

        motionMode =
            MotionMode.PingPongBetweenAngles;

        isWaiting = false;
        isActive = true;

        ApplyOrbit();
    }

    public void SetContinuousOrbit(
        bool rotateClockwise,
        float speed)
    {
        clockwise = rotateClockwise;
        angularSpeed = Mathf.Max(1f, speed);

        movementDirection =
            clockwise ? -1f : 1f;

        motionMode =
            MotionMode.ContinuousOrbit;

        isWaiting = false;
        isActive = true;
    }

    public void SetMirror(bool enabled)
    {
        mirrorOrbit = enabled;
        ApplyOrbit();
    }

    public void ToggleMirror()
    {
        mirrorOrbit = !mirrorOrbit;
        ApplyOrbit();
    }

    public void SetMirrorCenterAngle(float newCenterAngle)
    {
        mirrorCenterAngle = newCenterAngle;
        ApplyOrbit();
    }

    public void SetRadialClearance(float newClearance)
    {
        radialClearance = Mathf.Max(0f, newClearance);
        ApplyOrbit();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug || orbitPath == null)
            return;

        float previewCurrentAngle =
            GetEffectiveOrbitAngle(currentAngle);

        float previewMinAngle =
            GetEffectiveOrbitAngle(minAngle);

        float previewMaxAngle =
            GetEffectiveOrbitAngle(maxAngle);

        Vector3 targetPoint =
            orbitPath.GetWorldPoint(
                previewCurrentAngle,
                radialClearance
            );

        targetPoint +=
            orbitPath.WorldPlaneNormal *
            additionalHeightOffset;

        Vector3 minPoint =
            orbitPath.GetWorldPoint(
                previewMinAngle,
                radialClearance
            );

        minPoint +=
            orbitPath.WorldPlaneNormal *
            additionalHeightOffset;

        Vector3 maxPoint =
            orbitPath.GetWorldPoint(
                previewMaxAngle,
                radialClearance
            );

        maxPoint +=
            orbitPath.WorldPlaneNormal *
            additionalHeightOffset;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(targetPoint, 0.08f);
        Gizmos.DrawLine(orbitPath.WorldCenter, targetPoint);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(minPoint, 0.06f);
        Gizmos.DrawLine(orbitPath.WorldCenter, minPoint);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(maxPoint, 0.06f);
        Gizmos.DrawLine(orbitPath.WorldCenter, maxPoint);
    }
}