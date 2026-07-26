using UnityEngine;

[DisallowMultipleComponent]
public class TrapHammerTwoPoseSwing : MonoBehaviour
{
    [Header("Target Poses")]
    [SerializeField] private Transform poseA;
    [SerializeField] private Transform poseB;

    [Header("Motion")]
    [SerializeField] private bool movePosition = true;
    [SerializeField] private bool rotateRotation = true;

    [SerializeField, Min(0.05f)] private float moveDuration = 1.2f;
    [SerializeField, Min(0f)] private float waitAtEnds = 0.15f;

    [Header("Smooth")]
    [SerializeField] private bool useSmoothCurve = true;
    [SerializeField] private AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool startFromPoseA = true;

    [Header("Physics")]
    [SerializeField] private bool autoConfigureRigidbody = true;

    private Rigidbody rb;

    private float progress;
    private float direction;
    private float waitTimer;

    private bool isActive;
    private bool isWaiting;

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

        progress = startFromPoseA ? 0f : 1f;
        direction = startFromPoseA ? 1f : -1f;

        ApplyPose(progress);

        if (startOnAwake)
            StartSwing();
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

        if (poseA == null || poseB == null)
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

        ApplyPose(progress);

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

    private void ApplyPose(float rawProgress)
    {
        if (poseA == null || poseB == null)
            return;

        float t = useSmoothCurve ? smoothCurve.Evaluate(rawProgress) : rawProgress;

        Vector3 targetPosition = Vector3.Lerp(
            poseA.position,
            poseB.position,
            t
        );

        Quaternion targetRotation = Quaternion.Slerp(
            poseA.rotation,
            poseB.rotation,
            t
        );

        if (rb != null)
        {
            if (movePosition && rotateRotation)
            {
                rb.MovePosition(targetPosition);
                rb.MoveRotation(targetRotation);
            }
            else if (movePosition)
            {
                rb.MovePosition(targetPosition);
            }
            else if (rotateRotation)
            {
                rb.MoveRotation(targetRotation);
            }

            return;
        }

        if (movePosition)
            transform.position = targetPosition;

        if (rotateRotation)
            transform.rotation = targetRotation;
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

        progress = startFromPoseA ? 0f : 1f;
        direction = startFromPoseA ? 1f : -1f;

        ApplyPose(progress);
    }

    private void OnDrawGizmosSelected()
    {
        if (poseA == null || poseB == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(poseA.position, 0.15f);
        Gizmos.DrawWireSphere(poseB.position, 0.15f);
        Gizmos.DrawLine(poseA.position, poseB.position);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(poseA.position, poseA.position + poseA.forward * 0.7f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(poseB.position, poseB.position + poseB.forward * 0.7f);
    }
}