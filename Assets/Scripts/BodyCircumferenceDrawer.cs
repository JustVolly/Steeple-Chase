using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BodyCircumferenceDrawer : MonoBehaviour
{
    public enum CirclePlane
    {
        LocalXZ, // Local Y eksenine dik çevre
        LocalXY, // Local Z eksenine dik çevre
        LocalYZ  // Local X eksenine dik çevre
    }

    [Header("Circle Plane")]
    [SerializeField] private CirclePlane circlePlane = CirclePlane.LocalXZ;

    [Tooltip("Çemberin başlangıç yönünü kendi düzlemi içinde döndürür.")]
    [SerializeField] private float circleAngleOffset = 0f;

    [Header("Circle Center")]
    [Tooltip("Açık olduğunda çember merkezi doğrudan Gövde pivotudur.")]
    [SerializeField] private bool useTransformPositionAsCenter = true;

    [Tooltip("Kapalı merkez modunda Collider kullanılır.")]
    [SerializeField] private bool useColliderBounds = true;

    [Tooltip("Çemberi seçilen düzlemin normal ekseninde yükseltir.")]
    [SerializeField] private float heightOffset = 0f;

    [Tooltip("Çember merkezine Gövdenin local eksenlerinde ekstra konum verir.")]
    [SerializeField] private Vector3 localCenterOffset = Vector3.zero;

    [Header("Radius")]
    [Tooltip("Açık olduğunda Manual Radius kullanılır.")]
    [SerializeField] private bool useManualRadius = true;

    [Tooltip("Gövdenin çevre yarıçapı.")]
    [SerializeField, Min(0.001f)] private float manualRadius = 0.610f;

    [Tooltip("Otomatik yarıçap hesabında iki eksenin ortalamasını kullanır.")]
    [SerializeField] private bool useAverageRadius = true;

    [Header("Gizmo")]
    [SerializeField] private bool drawCircle = true;
    [SerializeField] private Color circleColor = Color.yellow;

    [SerializeField, Range(8, 256)]
    private int segments = 96;

    [SerializeField] private bool showCenterPoint = true;
    [SerializeField] private Color centerColor = Color.red;

    [SerializeField] private bool showRadiusLine = true;
    [SerializeField] private Color radiusLineColor = Color.cyan;

    [SerializeField] private bool showPlaneNormal = true;
    [SerializeField] private Color planeNormalColor = Color.green;

    [Header("Information")]
    [SerializeField] private bool printInfoToConsole = false;

    public float Radius => GetEffectiveRadius();

    public float Diameter => Radius * 2f;

    public float Circumference => 2f * Mathf.PI * Radius;

    public float HeightOffset
    {
        get => heightOffset;
        set => heightOffset = value;
    }

    public float CircleAngleOffset
    {
        get => circleAngleOffset;
        set => circleAngleOffset = value;
    }

    public Vector3 WorldCenter => CalculateWorldCenter();

    public Vector3 WorldPlaneNormal
    {
        get
        {
            GetWorldPlaneFrame(
                out _,
                out _,
                out Vector3 normal
            );

            return normal;
        }
    }

    public Quaternion BodyWorldRotation => transform.rotation;

    private void OnValidate()
    {
        manualRadius = Mathf.Max(0.001f, manualRadius);
        segments = Mathf.Clamp(segments, 8, 256);
    }

    private void OnDrawGizmos()
    {
        float radius = Radius;
        Vector3 center = WorldCenter;

        if (drawCircle)
            DrawWorldCircle(center, radius);

        if (showCenterPoint)
        {
            Gizmos.color = centerColor;

            Gizmos.DrawWireSphere(
                center,
                Mathf.Max(0.025f, radius * 0.04f)
            );
        }

        if (showRadiusLine)
        {
            Gizmos.color = radiusLineColor;

            Gizmos.DrawLine(
                center,
                GetWorldPoint(0f)
            );
        }

        if (showPlaneNormal)
        {
            Gizmos.color = planeNormalColor;

            Gizmos.DrawLine(
                center,
                center + WorldPlaneNormal * Mathf.Max(radius * 0.4f, 0.2f)
            );
        }

        if (printInfoToConsole)
        {
            Debug.Log(
                $"{gameObject.name} | " +
                $"Radius: {Radius:F3} | " +
                $"Diameter: {Diameter:F3} | " +
                $"Circumference: {Circumference:F3} | " +
                $"Height: {heightOffset:F3} | " +
                $"Rotation: {transform.eulerAngles}",
                this
            );

            printInfoToConsole = false;
        }
    }

    public Vector3 GetWorldPoint(
        float angleDegrees,
        float additionalRadius = 0f)
    {
        float finalRadius =
            Mathf.Max(0.001f, Radius + additionalRadius);

        Vector3 radialDirection =
            GetWorldRadialDirection(angleDegrees);

        return WorldCenter + radialDirection * finalRadius;
    }

    public Vector3 GetWorldRadialDirection(float angleDegrees)
    {
        GetWorldPlaneFrame(
            out Vector3 axisA,
            out Vector3 axisB,
            out _
        );

        float finalAngle =
            (angleDegrees + circleAngleOffset) * Mathf.Deg2Rad;

        Vector3 direction =
            axisA * Mathf.Cos(finalAngle) +
            axisB * Mathf.Sin(finalAngle);

        return direction.normalized;
    }

    public Vector3 GetWorldTangentDirection(
        float angleDegrees,
        float movementDirection = 1f)
    {
        GetWorldPlaneFrame(
            out Vector3 axisA,
            out Vector3 axisB,
            out _
        );

        float finalAngle =
            (angleDegrees + circleAngleOffset) * Mathf.Deg2Rad;

        Vector3 tangent =
            -axisA * Mathf.Sin(finalAngle) +
            axisB * Mathf.Cos(finalAngle);

        float directionSign =
            movementDirection >= 0f ? 1f : -1f;

        return (tangent * directionSign).normalized;
    }

    public Quaternion GetWorldTangentRotation(
        float angleDegrees,
        float movementDirection = 1f)
    {
        Vector3 tangent =
            GetWorldTangentDirection(
                angleDegrees,
                movementDirection
            );

        return Quaternion.LookRotation(
            tangent,
            WorldPlaneNormal
        );
    }

    public Quaternion GetWorldRadialRotation(
        float angleDegrees,
        bool lookAtCenter)
    {
        Vector3 radial =
            GetWorldRadialDirection(angleDegrees);

        Vector3 forward =
            lookAtCenter ? -radial : radial;

        return Quaternion.LookRotation(
            forward,
            WorldPlaneNormal
        );
    }

    public void GetWorldPlaneFrame(
        out Vector3 axisA,
        out Vector3 axisB,
        out Vector3 normal)
    {
        switch (circlePlane)
        {
            case CirclePlane.LocalXZ:
                axisA = transform.right.normalized;
                axisB = -transform.forward.normalized;
                normal = transform.up.normalized;
                break;

            case CirclePlane.LocalXY:
                axisA = transform.right.normalized;
                axisB = transform.up.normalized;
                normal = transform.forward.normalized;
                break;

            case CirclePlane.LocalYZ:
                axisA = transform.up.normalized;
                axisB = transform.forward.normalized;
                normal = transform.right.normalized;
                break;

            default:
                axisA = transform.right.normalized;
                axisB = -transform.forward.normalized;
                normal = transform.up.normalized;
                break;
        }
    }

    private Vector3 CalculateWorldCenter()
    {
        Vector3 baseCenter;

        if (useTransformPositionAsCenter)
        {
            baseCenter = transform.position;
        }
        else if (TryGetBounds(out Bounds bounds))
        {
            baseCenter = bounds.center;
        }
        else
        {
            baseCenter = transform.position;
        }

        Vector3 localOffsetInWorld =
            transform.TransformDirection(localCenterOffset);

        return baseCenter
               + WorldPlaneNormal * heightOffset
               + localOffsetInWorld;
    }

    private float GetEffectiveRadius()
    {
        if (useManualRadius)
            return Mathf.Max(0.001f, manualRadius);

        if (TryGetBounds(out Bounds bounds))
            return Mathf.Max(0.001f, CalculateRadius(bounds));

        return Mathf.Max(0.001f, manualRadius);
    }

    private bool TryGetBounds(out Bounds bounds)
    {
        if (useColliderBounds)
        {
            Collider colliderComponent =
                GetComponentInChildren<Collider>();

            if (colliderComponent != null)
            {
                bounds = colliderComponent.bounds;
                return true;
            }
        }

        Renderer rendererComponent =
            GetComponentInChildren<Renderer>();

        if (rendererComponent != null)
        {
            bounds = rendererComponent.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private float CalculateRadius(Bounds bounds)
    {
        float diameterA;
        float diameterB;

        switch (circlePlane)
        {
            case CirclePlane.LocalXZ:
                diameterA = bounds.size.x;
                diameterB = bounds.size.z;
                break;

            case CirclePlane.LocalXY:
                diameterA = bounds.size.x;
                diameterB = bounds.size.y;
                break;

            case CirclePlane.LocalYZ:
                diameterA = bounds.size.y;
                diameterB = bounds.size.z;
                break;

            default:
                diameterA = bounds.size.x;
                diameterB = bounds.size.z;
                break;
        }

        if (useAverageRadius)
            return (diameterA + diameterB) * 0.25f;

        return Mathf.Max(diameterA, diameterB) * 0.5f;
    }

    private void DrawWorldCircle(
        Vector3 center,
        float radius)
    {
        Gizmos.color = circleColor;

        Vector3 previousPoint =
            GetWorldPoint(0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                i / (float)segments * 360f;

            Vector3 nextPoint =
                GetWorldPoint(angle);

            Gizmos.DrawLine(
                previousPoint,
                nextPoint
            );

            previousPoint = nextPoint;
        }
    }

    public void SetRadius(float newRadius)
    {
        manualRadius = Mathf.Max(0.001f, newRadius);
        useManualRadius = true;
    }

    public void SetHeightOffset(float newHeightOffset)
    {
        heightOffset = newHeightOffset;
    }

    public void SetCircleAngleOffset(float newAngleOffset)
    {
        circleAngleOffset = newAngleOffset;
    }

    public void SetLocalCenterOffset(Vector3 newOffset)
    {
        localCenterOffset = newOffset;
    }
}