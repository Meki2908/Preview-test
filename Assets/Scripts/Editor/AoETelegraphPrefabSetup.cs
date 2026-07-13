#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AoETelegraphPrefabSetup
{
    private const string PrefabPath = "Assets/Prefabs/AoE Indicator.prefab";
    private const string OuterMaterialPath = "Assets/Prefabs/AoE Indicator/AoE_OuterRing.mat";
    private const string InnerMaterialPath = "Assets/Prefabs/AoE Indicator/AoE_InnerRing.mat";
    private const string DiskMeshPath = "Assets/Prefabs/AoE Indicator/AoE_DiskMesh.asset";
    private const string MutantPrefabPath = "Assets/Prefabs/Zombies/Mutant zombie.prefab";

    static AoETelegraphPrefabSetup()
    {
        EditorApplication.delayCall += EnsurePrefabExists;
    }

    private static void EnsurePrefabExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        CreatePrefabInternal(showDialog: false);
    }

    [MenuItem("Tools/Combat/Create AoE Indicator Prefab")]
    public static void CreatePrefabFromMenu()
    {
        CreatePrefabInternal(showDialog: true);
    }

    public static void CreatePrefabSilent()
    {
        CreatePrefabInternal(showDialog: false);
    }

    private static void CreatePrefabInternal(bool showDialog)
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/AoE Indicator");

        Material outerMaterial = EnsureMaterial(OuterMaterialPath, new Color(1f, 0.15f, 0.15f, 0.35f));
        Material innerMaterial = EnsureMaterial(InnerMaterialPath, new Color(1f, 0.45f, 0.1f, 0.55f));
        Mesh diskMesh = EnsureDiskMeshAsset();

        GameObject root = new GameObject("AoE Indicator");
        AoETelegraph telegraph = root.AddComponent<AoETelegraph>();

        GameObject outerRing = CreateRing("Outer Ring", root.transform, diskMesh, outerMaterial);
        GameObject innerRing = CreateRing("Inner Ring", root.transform, diskMesh, innerMaterial);

        SerializedObject serializedTelegraph = new SerializedObject(telegraph);
        serializedTelegraph.FindProperty("outerRing").objectReferenceValue = outerRing.transform;
        serializedTelegraph.FindProperty("innerRing").objectReferenceValue = innerRing.transform;
        serializedTelegraph.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = SaveOrReplacePrefab(root, PrefabPath);
        Object.DestroyImmediate(root);

        int wiredMutants = WireMutantPrefab(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message =
            $"Created/updated:\n{PrefabPath}\n\nWired Mutant prefabs: {wiredMutants}";

        Debug.Log($"[AoE Indicator] {message}");
        if (showDialog)
            EditorUtility.DisplayDialog("AoE Indicator Prefab", message, "OK");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Material EnsureMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        ConfigureTransparentMaterial(material, color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureTransparentMaterial(Material material, Color color)
    {
        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static Mesh EnsureDiskMeshAsset()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(DiskMeshPath);
        if (mesh != null)
            return mesh;

        mesh = AoETelegraphRingBuilder.CreateDiskMesh();
        AssetDatabase.CreateAsset(mesh, DiskMeshPath);
        return mesh;
    }

    private static GameObject CreateRing(string name, Transform parent, Mesh mesh, Material material)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);

        MeshFilter meshFilter = ring.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = ring.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return ring;
    }

    private static GameObject SaveOrReplacePrefab(GameObject source, string path)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return PrefabUtility.SaveAsPrefabAsset(source, path);

        return PrefabUtility.SaveAsPrefabAsset(source, path);
    }

    private static int WireMutantPrefab(GameObject telegraphPrefab)
    {
        int wired = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            GameObject mutantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (mutantPrefab == null || mutantPrefab.GetComponent<Mutants>() == null)
                continue;

            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            Mutants mutants = instance.GetComponent<Mutants>();
            if (mutants == null)
            {
                PrefabUtility.UnloadPrefabContents(instance);
                continue;
            }

            SerializedObject serializedMutants = new SerializedObject(mutants);
            SerializedProperty prefabProp = serializedMutants.FindProperty("aoeTelegraphPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue != telegraphPrefab)
            {
                prefabProp.objectReferenceValue = telegraphPrefab;
                serializedMutants.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                wired++;
            }

            PrefabUtility.UnloadPrefabContents(instance);
        }

        return wired;
    }
}
#endif
