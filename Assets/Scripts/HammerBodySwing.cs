using UnityEngine;

[DisallowMultipleComponent]
public class HammerBodySwing : MonoBehaviour
{
    public enum RotationAxis
    {
        LocalX,
        LocalY,
        LocalZ
    }

    [Header("Main Rotating Object")]
    [Tooltip("Bu script hangi objedeyse o obje ana dönen objedir. Genelde Hammer.")]
    [SerializeField] private bool rotateThisObject = true;

    [Header("Synced Body")]
    [Tooltip("Hammer ile aynı açı, aynı hız ve aynı yönde dönecek gövde/pivot objesi.")]
    [SerializeField] private Transform syncedBody;

    [Header("Position Lock")]
    [Tooltip("Hammer'ın local position değerini sabit tutar, sadece rotation uygular.")]
    [SerializeField] private bool lockMainObjectPosition = false;

    [Tooltip("Gövde'nin local position değerini sabit tutar, sadece rotation uygular.")]
    [SerializeField] private bool lockSyncedBodyPosition = true;

    [Header("Rotation Axis")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.LocalZ;

    [Header("Angle Range")]
    [SerializeField] private float minAngle = -180f;
    [SerializeField] private float maxAngle = 180f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float swingDuration = 1.5f;
    [SerializeField, Min(0f)] private float waitAtEnds = 0f;

    [Header("Smooth")]
    [SerializeField] private bool useSmoothCurve = true;
    [SerializeField] private AnimationCurve smoothCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool startFromMinAngle = true;

    [Header("Physics Optional")]
    [SerializeField] private bool autoConfigureRigidbody = true;
    [SerializeField] private bool autoConfigureSyncedBodyRigidbody = true;

    private Rigidbody rb;
    private Rigidbody syncedBodyRb;

    private Quaternion baseLocalRotation;
    private Quaternion syncedBodyBaseLocalRotation;

    private Vector3 mainStartLocalPosition;
    private Vector3 syncedBodyStartLocalPosition;

    private float progress;
    private float direction;
    private float waitTimer;

    private bool isActive;
    private bool isWaiting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (syncedBody != null)
            syncedBodyRb = syncedBody.GetComponent<Rigidbody>();

        ConfigureRigidbody(rb, autoConfigureRigidbody);
        ConfigureRigidbody(syncedBodyRb, autoConfigureSyncedBodyRigidbody);

        baseLocalRotation = transform.localRotation;
        mainStartLocalPosition = transform.localPosition;

        if (syncedBody != null)
        {
            syncedBodyBaseLocalRotation = syncedBody.localRotation;
            syncedBodyStartLocalPosition = syncedBody.localPosition;
        }

        progress = startFromMinAngle ? 0f : 1f;
        direction = startFromMinAngle ? 1f : -1f;

        ApplyRotation(progress);

        if (startOnAwake)
            StartSwing();
    }

    private void Update()
    {
        bool usePhysicsUpdate = rb != null || syncedBodyRb != null;

        if (usePhysicsUpdate)
            return;

        Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        bool usePhysicsUpdate = rb != null || syncedBodyRb != null;

        if (!usePhysicsUpdate)
            return;

        Tick(Time.fixedDeltaTime);
    }

    private void ConfigureRigidbody(Rigidbody targetRb, bool shouldConfigure)
    {
        if (targetRb == null || !shouldConfigure)
            return;

        targetRb.useGravity = false;
        targetRb.isKinematic = true;
        targetRb.interpolation = RigidbodyInterpolation.Interpolate;
        targetRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Tick(float deltaTime)
    {
        if (!isActive)
            return;

        if (isWaiting)
        {
            waitTimer -= deltaTime;

            if (waitTimer <= 0f)
                isWaiting = false;

            return;
        }

        progress += direction * deltaTime / swingDuration;
        progress = Mathf.Clamp01(progress);

        ApplyRotation(progress);

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

    private void ApplyRotation(float rawProgress)
    {
        float t = useSmoothCurve ? smoothCurve.Evaluate(rawProgress) : rawProgress;
        float currentAngle = Mathf.Lerp(minAngle, maxAngle, t);

        Vector3 axis = GetAxis();

        if (rotateThisObject)
        {
            Quaternion targetLocalRotation =
                baseLocalRotation * Quaternion.AngleAxis(currentAngle, axis);

            ApplyObjectRotation(
                transform,
                rb,
                targetLocalRotation,
                lockMainObjectPosition,
                mainStartLocalPosition
            );
        }

        if (syncedBody != null)
        {
            Quaternion syncedTargetLocalRotation =
                syncedBodyBaseLocalRotation * Quaternion.AngleAxis(currentAngle, axis);

            ApplyObjectRotation(
                syncedBody,
                syncedBodyRb,
                syncedTargetLocalRotation,
                lockSyncedBodyPosition,
                syncedBodyStartLocalPosition
            );
        }
    }

    private void ApplyObjectRotation(
        Transform target,
        Rigidbody targetRb,
        Quaternion targetLocalRotation,
        bool lockPosition,
        Vector3 lockedLocalPosition
    )
    {
        if (targetRb != null)
        {
            Quaternion targetWorldRotation;
            Vector3 targetWorldPosition;

            if (target.parent != null)
            {
                targetWorldRotation = target.parent.rotation * targetLocalRotation;
                targetWorldPosition = target.parent.TransformPoint(lockedLocalPosition);
            }
            else
            {
                targetWorldRotation = targetLocalRotation;
                targetWorldPosition = lockedLocalPosition;
            }

            if (lockPosition)
                targetRb.MovePosition(targetWorldPosition);

            targetRb.MoveRotation(targetWorldRotation);

            return;
        }

        if (lockPosition)
            target.localPosition = lockedLocalPosition;

        target.localRotation = targetLocalRotation;
    }

    private Vector3 GetAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.LocalX:
                return Vector3.right;

            case RotationAxis.LocalY:
                return Vector3.up;

            case RotationAxis.LocalZ:
                return Vector3.forward;

            default:
                return Vector3.forward;
        }
    }

    private void StartWait()
    {
        if (waitAtEnds <= 0f)
            return;

        isWaiting = true;
        waitTimer = waitAtEnds;
    }

    public void StartSwing()
    {
        isActive = true;
    }

    public void StopSwing()
    {
        isActive = false;
    }

    public void ResetSwing()
    {
        isActive = false;
        isWaiting = false;
        waitTimer = 0f;

        progress = startFromMinAngle ? 0f : 1f;
        direction = startFromMinAngle ? 1f : -1f;

        ApplyRotation(progress);
    }
}