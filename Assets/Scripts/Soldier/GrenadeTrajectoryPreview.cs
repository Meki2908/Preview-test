using UnityEngine;

/// <summary>
/// Dự đoán quỹ đạo lựu đạn bằng cùng vận tốc đầu và gravity với Rigidbody thật.
/// Tự tạo LineRenderer cho đường bay và vòng tròn điểm rơi.
/// </summary>
[DisallowMultipleComponent]
public class GrenadeTrajectoryPreview : MonoBehaviour
{
    [Header("Simulation")]
    [SerializeField, Min(8)] private int segmentCount = 32;
    [SerializeField, Min(0.01f)] private float timeStep = 0.06f;
    [SerializeField] private LayerMask collisionMask = ~0;

    [Header("Visual")]
    [SerializeField, Min(0.01f)] private float lineWidth = 0.08f;
    [SerializeField] private Color trajectoryColor = new Color(1f, 0.75f, 0.1f, 0.9f);
    [SerializeField] private Color landingColor = new Color(1f, 0.2f, 0.05f, 0.95f);
    [SerializeField, Min(0.1f)] private float landingRadius = 0.55f;
    [SerializeField, Min(8)] private int landingSegments = 32;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.03f;

    private readonly RaycastHit[] raycastHits = new RaycastHit[16];
    private readonly Vector3[] trajectoryPoints = new Vector3[64];

    private LineRenderer trajectoryLine;
    private LineRenderer landingLine;
    private Transform owner;
    private Material runtimeMaterial;

    private void Awake()
    {
        EnsureRenderers();
        Hide();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    public void Configure(Transform grenadeOwner)
    {
        owner = grenadeOwner;
        EnsureRenderers();
    }

    public void Show(Vector3 start, Vector3 launchVelocity)
    {
        EnsureRenderers();

        int requestedSegments = Mathf.Clamp(segmentCount, 8, trajectoryPoints.Length);
        int pointCount = 1;
        Vector3 previous = start;
        trajectoryPoints[0] = start;
        bool foundLanding = false;
        RaycastHit landingHit = default;

        for (int i = 1; i < requestedSegments; i++)
        {
            float time = i * timeStep;
            Vector3 next = start
                + launchVelocity * time
                + 0.5f * Physics.gravity * time * time;

            if (TryFindCollision(previous, next, out RaycastHit hit))
            {
                trajectoryPoints[pointCount++] = hit.point;
                landingHit = hit;
                foundLanding = true;
                break;
            }

            trajectoryPoints[pointCount++] = next;
            previous = next;
        }

        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
            trajectoryLine.SetPosition(i, trajectoryPoints[i]);

        if (foundLanding)
            ShowLandingMarker(landingHit.point, landingHit.normal);
        else
            landingLine.enabled = false;
    }

    public void Hide()
    {
        if (trajectoryLine != null)
            trajectoryLine.enabled = false;
        if (landingLine != null)
            landingLine.enabled = false;
    }

    private bool TryFindCollision(Vector3 from, Vector3 to, out RaycastHit closestHit)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        closestHit = default;

        if (distance <= Mathf.Epsilon)
            return false;

        int hitCount = Physics.RaycastNonAlloc(
            from,
            delta / distance,
            raycastHits,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null || IsOwnerCollider(hit.collider))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private bool IsOwnerCollider(Collider col)
    {
        if (owner == null || col == null)
            return false;

        Transform hitTransform = col.transform;
        return hitTransform == owner
            || hitTransform.IsChildOf(owner)
            || owner.IsChildOf(hitTransform);
    }

    private void ShowLandingMarker(Vector3 point, Vector3 normal)
    {
        landingLine.enabled = true;
        landingLine.positionCount = landingSegments;

        Vector3 up = normal.sqrMagnitude > 0.01f ? normal.normalized : Vector3.up;
        Vector3 tangent = Vector3.Cross(up, Vector3.forward);
        if (tangent.sqrMagnitude < 0.01f)
            tangent = Vector3.Cross(up, Vector3.right);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(up, tangent).normalized;

        Vector3 center = point + up * surfaceOffset;
        for (int i = 0; i < landingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / landingSegments;
            Vector3 offset = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * landingRadius;
            landingLine.SetPosition(i, center + offset);
        }
    }

    private void EnsureRenderers()
    {
        if (runtimeMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (shader != null)
            {
                runtimeMaterial = new Material(shader)
                {
                    name = "GrenadeTrajectoryRuntimeMaterial",
                    hideFlags = HideFlags.DontSave
                };
            }
        }

        if (trajectoryLine == null)
            trajectoryLine = CreateLine("GrenadeTrajectoryLine", trajectoryColor, false);

        if (landingLine == null)
            landingLine = CreateLine("GrenadeLandingMarker", landingColor, true);
    }

    private LineRenderer CreateLine(string objectName, Color color, bool loop)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = loop;
        line.widthMultiplier = lineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.startColor = color;
        line.endColor = color;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = runtimeMaterial;
        line.enabled = false;
        return line;
    }
}
