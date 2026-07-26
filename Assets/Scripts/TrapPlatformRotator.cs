using UnityEngine;

[DisallowMultipleComponent]
public class TrapPlatformAngleRotator : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation Axis")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;

    [Tooltip("Açılar objenin local rotation değerine göre hesaplansın.")]
    [SerializeField] private bool useLocalRotation = true;

    [Header("Angle Range")]
    [SerializeField] private float firstAngle = -35f;
    [SerializeField] private float secondAngle = 35f;

    [Header("Movement")]
    [SerializeField, Min(1f)] private float rotationSpeed = 70f;
    [SerializeField, Min(0f)] private float waitTimeAtAngles = 0.25f;

    [Header("Smooth")]
    [SerializeField] private bool useSmoothCurve = true;
    [SerializeField] private AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool startFromFirstAngle = true;

    [Header("Rigidbody Settings")]
    [SerializeField] private bool autoConfigureRigidbody = true;

    [Tooltip("Trap hareketli platform/engel ise Rigidbody kinematic olmalı.")]
    [SerializeField] private bool forceKinematic = true;

    [Header("Debug")]
    [SerializeField] private bool drawDebugAxis = true;

    private Rigidbody rb;

    private Quaternion baseLocalRotation;
    private Quaternion baseWorldRotation;

    private float progress;
    private float direction;
    private float waitTimer;

    private bool isActive;
    private bool isWaiting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        ConfigureRigidbody();

        baseLocalRotation = transform.localRotation;
        baseWorldRotation = transform.rotation;

        progress = startFromFirstAngle ? 0f : 1f;
        direction = startFromFirstAngle ? 1f : -1f;

        ApplyRotationByProgress(progress);

        if (startOnAwake)
            StartTrap();
    }

    private void Update()
    {
        if (rb != null)
            return;

        Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Tick(Time.fixedDeltaTime);
    }

    private void ConfigureRigidbody()
    {
        if (rb == null || !autoConfigureRigidbody)
            return;

        rb.useGravity = false;

        if (forceKinematic)
            rb.isKinematic = true;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Tick(float deltaTime)
    {
        if (!isActive)
            return;

        if (Mathf.Approximately(firstAngle, secondAngle))
            return;

        if (isWaiting)
        {
            waitTimer -= deltaTime;

            if (waitTimer <= 0f)
                isWaiting = false;

            return;
        }

        float angleDistance = Mathf.Abs(secondAngle - firstAngle);
        float duration = angleDistance / Mathf.Max(rotationSpeed, 0.01f);

        progress += direction * deltaTime / duration;
        progress = Mathf.Clamp01(progress);

        ApplyRotationByProgress(progress);

        if (progress >= 1f)
        {
            progress = 1f;
            direction = -1f;
            StartWait();
        }
        else if (progress <= 0f)
        {
            progress = 0f;
            direction = 1f;
            StartWait();
        }
    }

    private void ApplyRotationByProgress(float rawProgress)
    {
        float evaluatedProgress = useSmoothCurve
            ? smoothCurve.Evaluate(rawProgress)
            : rawProgress;

        float currentAngle = Mathf.Lerp(firstAngle, secondAngle, evaluatedProgress);

        Quaternion targetRotation = CreateRotation(currentAngle);

        ApplyRotation(targetRotation);
    }

    private Quaternion CreateRotation(float angle)
    {
        Vector3 axis = GetAxis();

        if (useLocalRotation)
        {
            return baseLocalRotation * Quaternion.AngleAxis(angle, axis);
        }

        return baseWorldRotation * Quaternion.AngleAxis(angle, axis);
    }

    private void ApplyRotation(Quaternion targetRotation)
    {
        if (rb != null)
        {
            Quaternion worldRotation = targetRotation;

            if (useLocalRotation && transform.parent != null)
            {
                worldRotation = transform.parent.rotation * targetRotation;
            }

            rb.MoveRotation(worldRotation);
            return;
        }

        if (useLocalRotation)
            transform.localRotation = targetRotation;
        else
            transform.rotation = targetRotation;
    }

    private Vector3 GetAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;

            case RotationAxis.Y:
                return Vector3.up;

            case RotationAxis.Z:
                return Vector3.forward;

            default:
                return Vector3.forward;
        }
    }

    private void StartWait()
    {
        if (waitTimeAtAngles <= 0f)
            return;

        isWaiting = true;
        waitTimer = waitTimeAtAngles;
    }

    public void StartTrap()
    {
        isActive = true;
    }

    public void StopTrap()
    {
        isActive = false;
    }

    public void ResetTrap()
    {
        isActive = false;
        isWaiting = false;
        waitTimer = 0f;

        progress = startFromFirstAngle ? 0f : 1f;
        direction = startFromFirstAngle ? 1f : -1f;

        ApplyRotationByProgress(progress);
    }

    public void SetAngles(float newFirstAngle, float newSecondAngle)
    {
        firstAngle = newFirstAngle;
        secondAngle = newSecondAngle;

        progress = Mathf.Clamp01(progress);
        ApplyRotationByProgress(progress);
    }

    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = Mathf.Max(1f, newSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugAxis)
            return;

        Gizmos.color = Color.cyan;

        Vector3 axis = GetAxis();

        Vector3 worldAxis = useLocalRotation
            ? transform.TransformDirection(axis)
            : axis;

        Gizmos.DrawLine(transform.position - worldAxis * 2f, transform.position + worldAxis * 2f);
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}