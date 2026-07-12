#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ZombieDissolveSetup
{
    private const string DissolveShaderGraphPath =
        "Assets/ShaderGraph_Dissolve/URP/ShaderGraph/Dissolve_Metallic.shadergraph";

    private static readonly MaterialBinding[] MaterialBindings =
    {
        new MaterialBinding(
            "Assets/Models/Zombies/Normal zombie/Materials/Ch10_1001_Diffuse.mat",
            "Assets/Models/Zombies/Normal zombie/Normal zombie.fbm/Ch10_1001_Diffuse.png",
            "Assets/Models/Zombies/Normal zombie/Normal zombie.fbm/Ch10_1001_Normal.png"),
        new MaterialBinding(
            "Assets/Models/Zombies/Normal zombie/Materials/Ch10_1002_Diffuse.mat",
            "Assets/Models/Zombies/Normal zombie/Normal zombie.fbm/Ch10_1002_Diffuse.png",
            "Assets/Models/Zombies/Normal zombie/Normal zombie.fbm/Ch10_1002_Normal.png"),
        new MaterialBinding(
            "Assets/Models/Zombies/Mutant zombie/Materials/skeletonZombie_diffuse.mat",
            "Assets/Models/Zombies/Mutant zombie/Mutant zombie.fbm/skeletonZombie_diffuse.png",
            "Assets/Models/Zombies/Mutant zombie/Mutant zombie.fbm/skeletonZombie_normal.png"),
        new MaterialBinding(
            "Assets/Models/Zombies/Mutant zombie/Materials/skeletonZombie_body_diffuse.mat",
            "Assets/Models/Zombies/Mutant zombie/Mutant zombie.fbm/skeletonZombie_body_diffuse.png",
            "Assets/Models/Zombies/Mutant zombie/Mutant zombie.fbm/skeletonZombie_normal.png")
    };

    [MenuItem("Tools/Zombies/Setup Dissolve Materials")]
    public static void SetupDissolveMaterials()
    {
        Shader dissolveShader = AssetDatabase.LoadAssetAtPath<Shader>(DissolveShaderGraphPath);
        if (dissolveShader == null)
            dissolveShader = Shader.Find("Shader Graphs/Dissolve_Dissolve_Metallic");

        if (dissolveShader == null)
        {
            EditorUtility.DisplayDialog(
                "Dissolve Setup",
                "Không tìm thấy shader Dissolve_Metallic.",
                "OK");
            return;
        }

        int updated = 0;
        foreach (MaterialBinding binding in MaterialBindings)
        {
            if (TryConfigureMaterial(binding, dissolveShader))
                updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Dissolve Setup",
            $"Đã cập nhật {updated}/{MaterialBindings.Length} material dissolve.",
            "OK");
    }

    [MenuItem("Tools/Zombies/Add Dissolve Components To Scene Enemies")]
    public static void AddDissolveComponentsToSceneEnemies()
    {
        Health[] healthComponents = Object.FindObjectsByType<Health>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int added = 0;
        foreach (Health health in healthComponents)
        {
            if (health.GetComponent<EnemyBase>() == null)
                continue;

            GameObject target = health.gameObject;
            Undo.RecordObject(target, "Add Dissolve Components");

            if (target.GetComponent<ZombieDissolveOnDeath>() == null)
            {
                target.AddComponent<ZombieDissolveOnDeath>();
                added++;
            }

            if (target.GetComponent<EnemyLowHealthFlash>() == null)
                target.AddComponent<EnemyLowHealthFlash>();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Dissolve Setup",
            $"Đã thêm component dissolve/flash lên {added} enemy trong scene.",
            "OK");
    }

    [MenuItem("Tools/Zombies/Fix Selected Renderer Materials")]
    public static void FixSelectedRendererMaterials()
    {
        Shader dissolveShader = AssetDatabase.LoadAssetAtPath<Shader>(DissolveShaderGraphPath);
        if (dissolveShader == null)
            dissolveShader = Shader.Find("Shader Graphs/Dissolve_Dissolve_Metallic");

        if (dissolveShader == null)
            return;

        foreach (Object selected in Selection.objects)
        {
            GameObject gameObject = selected as GameObject;
            if (gameObject == null)
                continue;

            SkinnedMeshRenderer renderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null)
                continue;

            Undo.RecordObject(renderer, "Fix Dissolve Materials");

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                if (!material.HasProperty("_Dissolve"))
                {
                    Texture baseMap = material.HasProperty("_BaseMap")
                        ? material.GetTexture("_BaseMap")
                        : material.mainTexture;

                    Texture normalMap = material.HasProperty("_BumpMap")
                        ? material.GetTexture("_BumpMap")
                        : null;

                    material.shader = dissolveShader;
                    if (baseMap != null)
                        material.SetTexture("_BaseMap", baseMap);
                    if (normalMap != null)
                        material.SetTexture("_NormalMap", normalMap);
                }

                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Dissolve", 0f);
                EditorUtility.SetDirty(material);
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static bool TryConfigureMaterial(MaterialBinding binding, Shader dissolveShader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(binding.MaterialPath);
        if (material == null)
        {
            Debug.LogWarning($"[DissolveSetup] Không tìm thấy material: {binding.MaterialPath}");
            return false;
        }

        Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(binding.DiffusePath);
        Texture2D normal = string.IsNullOrEmpty(binding.NormalPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<Texture2D>(binding.NormalPath);

        material.shader = dissolveShader;
        if (diffuse != null)
            material.SetTexture("_BaseMap", diffuse);
        if (normal != null)
            material.SetTexture("_NormalMap", normal);

        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Dissolve", 0f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.25f);

        EditorUtility.SetDirty(material);
        return true;
    }

    private readonly struct MaterialBinding
    {
        public readonly string MaterialPath;
        public readonly string DiffusePath;
        public readonly string NormalPath;

        public MaterialBinding(string materialPath, string diffusePath, string normalPath)
        {
            MaterialPath = materialPath;
            DiffusePath = diffusePath;
            NormalPath = normalPath;
        }
    }
}
#endif
