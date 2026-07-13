#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level2SceneSetup
{
    private const string ScenePath = "Assets/Scenes/Level 2.unity";

    [MenuItem("Tools/Level 2/Setup Level2 Director")]
    public static void SetupFromMenu()
    {
        SetupInternal(showDialog: true);
    }

    private static void SetupInternal(bool showDialog)
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Level 2 Setup", $"Không tìm thấy scene:\n{ScenePath}", "OK");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Level 2 Director Setup ===");

        ZombieSpawner waveSpawner = Object.FindAnyObjectByType<ZombieSpawner>();
        if (waveSpawner != null)
        {
            waveSpawner.enabled = false;
            EditorUtility.SetDirty(waveSpawner);
            report.AppendLine("ZombieSpawner: DISABLED (dùng Level2Director thay wave Lv1)");
        }
        else
        {
            report.AppendLine("ZombieSpawner: not found (OK)");
        }

        GameObject directorHost = GameObject.Find("[DIRECTOR]");
        if (directorHost == null)
            directorHost = new GameObject("[DIRECTOR]");

        Level2Director director = directorHost.GetComponent<Level2Director>();
        if (director == null)
            director = directorHost.AddComponent<Level2Director>();

        Mutants boss = Object.FindAnyObjectByType<Mutants>();
        if (boss != null)
        {
            SerializedObject serializedDirector = new SerializedObject(director);
            serializedDirector.FindProperty("bossMutant").objectReferenceValue = boss;
            serializedDirector.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine($"Boss Mutant: assigned ({boss.name})");
        }
        else
        {
            report.AppendLine("WARNING: Không tìm thấy Mutants trong scene — gán bossMutant tay.");
        }

        ZombieSpawner spawnerForRefs = waveSpawner;
        if (spawnerForRefs != null)
        {
            SerializedObject serializedDirector = new SerializedObject(director);
            SerializedProperty zombiePrefab = serializedDirector.FindProperty("zombiePrefab");
            SerializedProperty spawnPoints = serializedDirector.FindProperty("spawnPoints");
            SerializedProperty groundMask = serializedDirector.FindProperty("groundMask");

            if (spawnerForRefs.zombieTypes != null && spawnerForRefs.zombieTypes.Length > 0
                && spawnerForRefs.zombieTypes[0].prefab != null)
            {
                zombiePrefab.objectReferenceValue = spawnerForRefs.zombieTypes[0].prefab;
                report.AppendLine($"zombiePrefab: {spawnerForRefs.zombieTypes[0].prefab.name}");
            }

            if (spawnerForRefs.spawnPoints != null && spawnerForRefs.spawnPoints.Length > 0)
            {
                spawnPoints.arraySize = spawnerForRefs.spawnPoints.Length;
                for (int i = 0; i < spawnerForRefs.spawnPoints.Length; i++)
                    spawnPoints.GetArrayElementAtIndex(i).objectReferenceValue = spawnerForRefs.spawnPoints[i];

                report.AppendLine($"spawnPoints: {spawnerForRefs.spawnPoints.Length}");
            }

            SerializedObject spawnerSerialized = new SerializedObject(spawnerForRefs);
            groundMask.intValue = spawnerSerialized.FindProperty("groundMask").intValue;
            serializedDirector.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(directorHost);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(report.ToString());
        if (showDialog)
            EditorUtility.DisplayDialog("Level 2 Setup", report.ToString(), "OK");
    }
}
#endif
