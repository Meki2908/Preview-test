using UnityEngine;

/// <summary>
/// Builds flat ground-disk visuals with MeshFilter + MeshRenderer only (no colliders).
/// </summary>
public static class AoETelegraphRingBuilder
{
    public const string DiskMeshName = "AoETelegraphDisk";

    public static Mesh CreateDiskMesh(int segments = 48)
    {
        segments = Mathf.Max(8, segments);

        var mesh = new Mesh { name = DiskMeshName };
        var vertices = new Vector3[segments + 1];
        var uvs = new Vector2[segments + 1];
        var triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x, 0f, z);
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i == segments - 1 ? 1 : i + 2;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static GameObject CreateRingVisual(string objectName, Transform parent, Material material)
    {
        var ring = new GameObject(objectName);
        ring.transform.SetParent(parent, false);
        ring.transform.localRotation = Quaternion.identity;

        var meshFilter = ring.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateDiskMesh();

        var meshRenderer = ring.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return ring;
    }
}
