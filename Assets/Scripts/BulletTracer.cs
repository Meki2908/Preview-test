using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.1f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
    }

    public void Show(Vector3 start, Vector3 end)
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        Destroy(gameObject, lifetime);
    }

    public static void Spawn(GameObject prefab, Vector3 start, Vector3 end)
    {
        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab);
        if (instance.TryGetComponent<BulletTracer>(out BulletTracer tracer))
        {
            tracer.Show(start, end);
            return;
        }

        if (instance.TryGetComponent<LineRenderer>(out LineRenderer lineRenderer))
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            Destroy(instance, 0.1f);
        }
    }
}
