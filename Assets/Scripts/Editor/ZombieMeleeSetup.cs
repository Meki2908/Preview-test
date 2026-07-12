#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ZombieMeleeSetup
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Zombies/Zombie.prefab",
        "Assets/Prefabs/Zombies/Mutant zombie.prefab"
    };

    [MenuItem("Tools/Zombies/Setup Melee Damage On Prefabs")]
    public static void SetupMeleeOnPrefabs()
    {
        int updated = 0;
        foreach (string path in PrefabPaths)
        {
            if (SetupPrefab(path))
                updated++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Melee Setup",
            $"Đã setup melee damage cho {updated}/{PrefabPaths.Length} prefab.",
            "OK");
    }

    [MenuItem("Tools/Zombies/Setup Melee Damage In Scene")]
    public static void SetupMeleeInScene()
    {
        int updated = 0;
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (EnemyBase enemy in enemies)
        {
            if (SetupEnemyGameObject(enemy.gameObject))
                updated++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog(
            "Melee Setup",
            $"Đã setup melee damage cho {updated} enemy trong scene.",
            "OK");
    }

    private static bool SetupPrefab(string path)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null)
        {
            Debug.LogWarning($"[MeleeSetup] Không tìm thấy prefab: {path}");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(root);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        bool changed = SetupEnemyGameObject(prefabRoot);
        if (changed)
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);

        PrefabUtility.UnloadPrefabContents(prefabRoot);
        return changed;
    }

    private static bool SetupEnemyGameObject(GameObject enemyObject)
    {
        bool changed = false;

        if (enemyObject.GetComponent<DamageDealer>() == null)
        {
            Undo.RecordObject(enemyObject, "Add DamageDealer");
            enemyObject.AddComponent<DamageDealer>();
            changed = true;
        }

        if (enemyObject.GetComponent<EnemyMeleeBootstrap>() == null)
        {
            Undo.RecordObject(enemyObject, "Add EnemyMeleeBootstrap");
            enemyObject.AddComponent<EnemyMeleeBootstrap>();
            changed = true;
        }

        Animator animator = enemyObject.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.GetComponent<EnemyAnimationEvents>() == null)
        {
            Undo.RecordObject(animator.gameObject, "Add EnemyAnimationEvents");
            animator.gameObject.AddComponent<EnemyAnimationEvents>();
            changed = true;
        }

        DamageDealer dealer = enemyObject.GetComponent<DamageDealer>();
        GameObject[] hitboxes = FindHitboxObjects(enemyObject);
        if (dealer != null && hitboxes.Length > 0)
        {
            SerializedObject serializedDealer = new SerializedObject(dealer);
            SerializedProperty hitboxesProperty = serializedDealer.FindProperty("hitboxes");
            if (hitboxesProperty != null && hitboxesProperty.arraySize == 0)
            {
                hitboxesProperty.arraySize = hitboxes.Length;
                for (int i = 0; i < hitboxes.Length; i++)
                    hitboxesProperty.GetArrayElementAtIndex(i).objectReferenceValue = hitboxes[i];

                serializedDealer.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        foreach (GameObject hitbox in hitboxes)
        {
            if (hitbox == null)
                continue;

            Collider[] colliders = hitbox.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                    continue;

                Undo.RecordObject(collider, "Configure melee collider");
                collider.isTrigger = true;

                if (collider.GetComponent<DamageCollider>() == null)
                {
                    collider.gameObject.AddComponent<DamageCollider>();
                    changed = true;
                }
            }
        }

        if (changed)
            EditorUtility.SetDirty(enemyObject);

        return changed;
    }

    private static GameObject[] FindHitboxObjects(GameObject root)
    {
        var found = new List<GameObject>();
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == root.transform)
                continue;

            string name = child.name.ToLowerInvariant();
            if (!name.Contains("hitbox") && !name.Contains("damage"))
                continue;

            if (!found.Contains(child.gameObject))
                found.Add(child.gameObject);
        }

        return found.ToArray();
    }
}
#endif
