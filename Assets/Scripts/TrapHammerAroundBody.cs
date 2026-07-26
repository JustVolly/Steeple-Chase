using UnityEngine;

[DisallowMultipleComponent]
public class TrapHammerAroundBody : MonoBehaviour
{
    [Header("Body / Pivot")]
    [Tooltip("Gövde üzerindeki menteşe/dönüş merkezi.")]
    [SerializeField] private Transform pivot;

    [Header("Target Positions")]
    [Tooltip("Hammer'ın gideceği birinci pozisyon.")]
    [SerializeField] private Transform positionA;

    [Tooltip("Hammer'ın gideceği ikinci pozisyon.")]
    [SerializeField] private Transform positionB;

    [Header("Movement")]
    [SerializeField, Min(0.05f)] private float moveDuration = 1.0f;
    [SerializeField, Min(0f)] private float waitAtEnds = 0.15f;

    [Header("Rotation")]
    [Tooltip("Hammer kendi rotation değerini PosA/PosB rotationlarına göre alsın.")]
    [SerializeField] private bool usePoseRotation = true;

    [Tooltip("Açık olursa Hammer hareket yönüne doğru bakar.")]
    [SerializeField] private bool faceMovementDirection = false;

    [Header("Smooth")]
    [SerializeField] private bool useSmoothCurve = true;
    [SerializeField] private AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool startFromA = true;

    [Header("Physics")]
    [SerializeField] private bool autoConfigureRigidbody = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Rigidbody rb;

    private float progress;
    private float direction;
    private float waitTimer;

    private bool isActive;
    private bool isWaiting;

    private Vector3 lastPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (autoConfigureRigidbody && rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        progress = startFromA ? 0f : 1f;
        direction = startFromA ? 1f : -1f;

        ApplyHammerPose(progress);
        lastPosition = transform.position;

        if (startOnAwake)
            StartHammer();
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

    private void Tick(float deltaTime)
    {
        if (!isActive)
            return;

        if (!ReferencesAreValid())
            return;

        if (isWaiting)
        {
            waitTimer -= deltaTime;

            if (waitTimer <= 0f)
                isWaiting = false;

            return;
        }

        progress += direction * deltaTime / moveDuration;
        progress = Mathf.Clamp01(progress);

        ApplyHammerPose(progress);

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

    private void ApplyHammerPose(float rawProgress)
    {
        if (!ReferencesAreValid())
            return;

        float t = useSmoothCurve ? smoothCurve.Evaluate(rawProgress) : rawProgress;

        Vector3 pivotPos = pivot.position;

        Vector3 dirA = positionA.position - pivotPos;
        Vector3 dirB = positionB.position - pivotPos;

        if (dirA.sqrMagnitude < 0.0001f || dirB.sqrMagnitude < 0.0001f)
            return;

        float radiusA = dirA.magnitude;
        float radiusB = dirB.magnitude;

        Vector3 normalizedDirA = dirA.normalized;
        Vector3 normalizedDirB = dirB.normalized;

        // PosA ve PosB farklı uzaklıktaysa aradaki mesafe yumuşak geçer.
        // Aynı uzaklıkta koyarsan tam dairesel bağlı hareket olur.
        float radius = Mathf.Lerp(radiusA, radiusB, t);

        Vector3 currentDirection = Vector3.Slerp(
            normalizedDirA,
            normalizedDirB,
            t
        ).normalized;

        Vector3 targetPosition = pivotPos + currentDirection * radius;

        Quaternion targetRotation = CalculateTargetRotation(t, targetPosition);

        ApplyTransform(targetPosition, targetRotation);

        lastPosition = targetPosition;
    }

    private Quaternion CalculateTargetRotation(float t, Vector3 targetPosition)
    {
        if (faceMovementDirection)
        {
            Vector3 movementDirection = targetPosition - lastPosition;

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(movementDirection.normalized, Vector3.up);
            }
        }

        if (usePoseRotation)
        {
            return Quaternion.Slerp(
                positionA.rotation,
                positionB.rotation,
                t
            );
        }

        return transform.rotation;
    }

    private void ApplyTransform(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (rb != null)
        {
            rb.MovePosition(targetPosition);
            rb.MoveRotation(targetRotation);
            return;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private void StartWait()
    {
        if (waitAtEnds <= 0f)
            return;

        isWaiting = true;
        waitTimer = waitAtEnds;
    }

    private bool ReferencesAreValid()
    {
        return pivot != null && positionA != null && positionB != null;
    }

    public void StartHammer()
    {
        isActive = true;
    }

    public void StopHammer()
    {
        isActive = false;
    }

    public void ResetHammer()
    {
        isActive = false;
        isWaiting = false;
        waitTimer = 0f;

        progress = startFromA ? 0f : 1f;
        direction = startFromA ? 1f : -1f;

        ApplyHammerPose(progress);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        if (pivot == null || positionA == null || positionB == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot.position, 0.15f);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(positionA.position, 0.18f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(positionB.position, 0.18f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pivot.position, positionA.position);
        Gizmos.DrawLine(pivot.position, positionB.position);

        DrawArcPreview();
    }

    private void DrawArcPreview()
    {
        Vector3 pivotPos = pivot.position;

        Vector3 dirA = positionA.position - pivotPos;
        Vector3 dirB = positionB.position - pivotPos;

        if (dirA.sqrMagnitude < 0.0001f || dirB.sqrMagnitude < 0.0001f)
            return;

        Vector3 previousPoint = positionA.position;

        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;

            Vector3 currentDirection = Vector3.Slerp(
                dirA.normalized,
                dirB.normalized,
                t
            ).normalized;

            float radius = Mathf.Lerp(dirA.magnitude, dirB.magnitude, t);

            Vector3 currentPoint = pivotPos + currentDirection * radius;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}