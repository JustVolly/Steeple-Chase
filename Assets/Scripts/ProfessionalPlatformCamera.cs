using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ProfessionalPlatformCamera : MonoBehaviour
{
    public enum RotationMode
    {
        ManualEuler,
        LookAtTarget,
        LookAtWorldPoint
    }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetCenterOffset = new Vector3(0f, 1.2f, 0f);

    [Header("World Position Offset")]
    [Tooltip("Kamera karakter yönüne göre değil, dünya eksenine göre bu offset ile takip eder.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 5f, -8f);

    [Header("Follow Axes")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = true;

    [Header("Fixed Height Optional")]
    [SerializeField] private bool useFixedWorldHeight = false;
    [SerializeField] private float fixedWorldHeight = 6f;

    [Header("Height Limits")]
    [SerializeField] private bool useHeightLimit = true;
    [SerializeField] private float minCameraY = 2f;
    [SerializeField] private float maxCameraY = 20f;

    [Header("Position Smooth")]
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.18f;
    [SerializeField] private bool snapOnStart = true;

    [Header("Dead Zone")]
    [Tooltip("Küçük hareketlerde kamera kıpırdamasın diye kullanılır.")]
    [SerializeField] private bool useDeadZone = false;
    [SerializeField] private Vector3 deadZoneSize = new Vector3(0.2f, 0.1f, 0.2f);

    [Header("Look Ahead Optional")]
    [Tooltip("Kapalıysa kamera karakter hareketinden bağımsız kalır.")]
    [SerializeField] private bool useMovementLookAhead = false;

    [SerializeField] private Vector3 lookAheadMultiplier = new Vector3(0.25f, 0f, 0.25f);
    [SerializeField, Min(0f)] private float maxLookAheadDistance = 2f;
    [SerializeField, Min(0f)] private float lookAheadSmoothTime = 0.2f;

    [Header("Rotation")]
    [SerializeField] private RotationMode rotationMode = RotationMode.ManualEuler;

    [Tooltip("ManualEuler modunda kameranın gerçek rotation değeri budur.")]
    [SerializeField] private Vector3 manualEulerRotation = new Vector3(25f, 0f, 0f);

    [Tooltip("LookAtTarget modunda hedefin neresine bakacağını belirler.")]
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.3f, 0f);

    [Tooltip("LookAtWorldPoint modunda kameranın bakacağı dünya noktası.")]
    [SerializeField] private Vector3 worldLookPoint = Vector3.zero;

    [SerializeField, Min(0.01f)] private float rotationSharpness = 12f;

    [Header("Zoom")]
    [SerializeField] private bool controlCameraZoom = true;
    [SerializeField, Range(15f, 90f)] private float fieldOfView = 55f;
    [SerializeField, Min(0.1f)] private float orthographicSize = 6f;
    [SerializeField, Min(0.01f)] private float zoomSharpness = 10f;

    [Header("Position Bounds Optional")]
    [SerializeField] private bool usePositionBounds = false;
    [SerializeField] private Vector3 minPositionBounds = new Vector3(-100f, -100f, -100f);
    [SerializeField] private Vector3 maxPositionBounds = new Vector3(100f, 100f, 100f);

    [Header("Camera Collision Optional")]
    [SerializeField] private bool useCameraCollision = true;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.35f;
    [SerializeField, Min(0f)] private float collisionOffset = 0.25f;
    [SerializeField, Min(0f)] private float collisionSmoothTime = 0.06f;

    [Header("Finish Focus Optional")]
    [SerializeField] private Transform finishTarget;
    [SerializeField] private bool useFinishFocus = false;
    [SerializeField] private Vector3 finishLookOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField, Min(0.01f)] private float finishFocusSpeed = 2f;

    [Header("Shake Optional")]
    [SerializeField, Min(0.01f)] private float shakeRecoverSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugGizmos = true;

    private Camera cam;

    private Vector3 currentTrackedPoint;
    private Vector3 previousTargetPosition;
    private Vector3 currentLookAhead;

    private Vector3 positionVelocity;
    private Vector3 lookAheadVelocity;
    private Vector3 collisionVelocity;

    private float finishFocusWeight;
    private float shakeIntensity;
    private float shakeTimer;

    private bool initialized;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (target == null)
        {
            Debug.LogWarning($"{nameof(ProfessionalPlatformCamera)}: Target atanmadı.", this);
            return;
        }

        Vector3 startPoint = GetTargetPoint();

        currentTrackedPoint = startPoint;
        previousTargetPosition = target.position;

        if (snapOnStart)
        {
            Vector3 startPosition = CalculateDesiredPosition();
            transform.position = startPosition;
            transform.rotation = CalculateDesiredRotation(startPosition);
        }

        initialized = true;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (!initialized)
        {
            currentTrackedPoint = GetTargetPoint();
            previousTargetPosition = target.position;
            initialized = true;
        }

        UpdateTrackedPoint();
        UpdateLookAhead();
        UpdateFinishFocus();
        UpdateZoom();

        Vector3 desiredPosition = CalculateDesiredPosition();

        desiredPosition = ApplyFollowAxes(desiredPosition);
        desiredPosition = ApplyHeightRules(desiredPosition);
        desiredPosition = ApplyPositionBounds(desiredPosition);

        if (useCameraCollision)
            desiredPosition = ResolveCameraCollision(desiredPosition);

        Vector3 finalPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime
        );

        finalPosition += GetShakeOffset();

        transform.position = finalPosition;

        Quaternion desiredRotation = CalculateDesiredRotation(finalPosition);

        float rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationT
        );

        previousTargetPosition = target.position;
    }

    private Vector3 GetTargetPoint()
    {
        return target.position + targetCenterOffset;
    }

    private void UpdateTrackedPoint()
    {
        Vector3 targetPoint = GetTargetPoint();

        if (!useDeadZone)
        {
            currentTrackedPoint = targetPoint;
            return;
        }

        Vector3 delta = targetPoint - currentTrackedPoint;

        if (Mathf.Abs(delta.x) > deadZoneSize.x)
            currentTrackedPoint.x += delta.x - Mathf.Sign(delta.x) * deadZoneSize.x;

        if (Mathf.Abs(delta.y) > deadZoneSize.y)
            currentTrackedPoint.y += delta.y - Mathf.Sign(delta.y) * deadZoneSize.y;

        if (Mathf.Abs(delta.z) > deadZoneSize.z)
            currentTrackedPoint.z += delta.z - Mathf.Sign(delta.z) * deadZoneSize.z;
    }

    private void UpdateLookAhead()
    {
        if (!useMovementLookAhead)
        {
            currentLookAhead = Vector3.SmoothDamp(
                currentLookAhead,
                Vector3.zero,
                ref lookAheadVelocity,
                lookAheadSmoothTime
            );

            return;
        }

        Vector3 velocity = (target.position - previousTargetPosition) / Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 desiredLookAhead = new Vector3(
            velocity.x * lookAheadMultiplier.x,
            velocity.y * lookAheadMultiplier.y,
            velocity.z * lookAheadMultiplier.z
        );

        desiredLookAhead = Vector3.ClampMagnitude(desiredLookAhead, maxLookAheadDistance);

        currentLookAhead = Vector3.SmoothDamp(
            currentLookAhead,
            desiredLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );
    }

    private Vector3 CalculateDesiredPosition()
    {
        return currentTrackedPoint + worldOffset + currentLookAhead;
    }

    private Vector3 ApplyFollowAxes(Vector3 desiredPosition)
    {
        Vector3 result = desiredPosition;

        if (!followX)
            result.x = transform.position.x;

        if (!followY)
            result.y = transform.position.y;

        if (!followZ)
            result.z = transform.position.z;

        return result;
    }

    private Vector3 ApplyHeightRules(Vector3 position)
    {
        if (useFixedWorldHeight)
            position.y = fixedWorldHeight;

        if (useHeightLimit)
            position.y = Mathf.Clamp(position.y, minCameraY, maxCameraY);

        return position;
    }

    private Vector3 ApplyPositionBounds(Vector3 position)
    {
        if (!usePositionBounds)
            return position;

        position.x = Mathf.Clamp(position.x, minPositionBounds.x, maxPositionBounds.x);
        position.y = Mathf.Clamp(position.y, minPositionBounds.y, maxPositionBounds.y);
        position.z = Mathf.Clamp(position.z, minPositionBounds.z, maxPositionBounds.z);

        return position;
    }

    private Quaternion CalculateDesiredRotation(Vector3 cameraPosition)
    {
        if (rotationMode == RotationMode.ManualEuler)
            return Quaternion.Euler(manualEulerRotation);

        Vector3 lookPoint;

        if (rotationMode == RotationMode.LookAtWorldPoint)
        {
            lookPoint = worldLookPoint;
        }
        else
        {
            Vector3 normalLookPoint = target.position + lookAtOffset;

            if (finishTarget != null)
            {
                Vector3 finishPoint = finishTarget.position + finishLookOffset;

                normalLookPoint = Vector3.Lerp(
                    normalLookPoint,
                    finishPoint,
                    finishFocusWeight
                );
            }

            lookPoint = normalLookPoint;
        }

        Vector3 direction = lookPoint - cameraPosition;

        if (direction.sqrMagnitude < 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Vector3 ResolveCameraCollision(Vector3 desiredPosition)
    {
        Vector3 pivot = currentTrackedPoint;
        Vector3 direction = desiredPosition - pivot;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return desiredPosition;

        direction.Normalize();

        bool hitSomething = Physics.SphereCast(
            pivot,
            collisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!hitSomething)
            return desiredPosition;

        Vector3 correctedPosition = hit.point - direction * collisionOffset;

        return Vector3.SmoothDamp(
            transform.position,
            correctedPosition,
            ref collisionVelocity,
            collisionSmoothTime
        );
    }

    private void UpdateFinishFocus()
    {
        float targetWeight = useFinishFocus && finishTarget != null ? 1f : 0f;

        finishFocusWeight = Mathf.MoveTowards(
            finishFocusWeight,
            targetWeight,
            finishFocusSpeed * Time.deltaTime
        );
    }

    private void UpdateZoom()
    {
        if (!controlCameraZoom || cam == null)
            return;

        float zoomT = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);

        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Lerp(
                cam.orthographicSize,
                orthographicSize,
                zoomT
            );
        }
        else
        {
            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                fieldOfView,
                zoomT
            );
        }
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f || shakeIntensity <= 0f)
            return Vector3.zero;

        shakeTimer -= Time.deltaTime;

        shakeIntensity = Mathf.MoveTowards(
            shakeIntensity,
            0f,
            shakeRecoverSpeed * Time.deltaTime
        );

        return Random.insideUnitSphere * shakeIntensity;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target == null)
            return;

        currentTrackedPoint = GetTargetPoint();
        previousTargetPosition = target.position;
    }

    public void SetWorldOffset(Vector3 newOffset)
    {
        worldOffset = newOffset;
    }

    public void SetManualRotation(Vector3 newEulerRotation)
    {
        manualEulerRotation = newEulerRotation;
        rotationMode = RotationMode.ManualEuler;
    }

    public void SetFixedHeight(bool enabled, float height)
    {
        useFixedWorldHeight = enabled;
        fixedWorldHeight = height;
    }

    public void SetFinishFocus(bool enabled)
    {
        useFinishFocus = enabled;
    }

    public void CameraShake(float intensity, float duration)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeTimer = Mathf.Max(shakeTimer, duration);
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        currentTrackedPoint = GetTargetPoint();

        Vector3 desiredPosition = CalculateDesiredPosition();
        desiredPosition = ApplyFollowAxes(desiredPosition);
        desiredPosition = ApplyHeightRules(desiredPosition);
        desiredPosition = ApplyPositionBounds(desiredPosition);

        transform.position = desiredPosition;
        transform.rotation = CalculateDesiredRotation(desiredPosition);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos || target == null)
            return;

        Vector3 targetPoint = target.position + targetCenterOffset;
        Vector3 desiredCameraPosition = targetPoint + worldOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetPoint, 0.2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(targetPoint, desiredCameraPosition);
        Gizmos.DrawWireSphere(desiredCameraPosition, 0.35f);

        if (usePositionBounds)
        {
            Gizmos.color = Color.green;

            Vector3 center = (minPositionBounds + maxPositionBounds) * 0.5f;
            Vector3 size = maxPositionBounds - minPositionBounds;

            Gizmos.DrawWireCube(center, size);
        }
    }
}