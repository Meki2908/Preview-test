#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Level1SceneAudit
{
    private const string ScenePath = "Assets/Scenes/Level 1.unity";
    private const string CanvasPrefabPath = "Assets/Prefabs/MainGame Canvas.prefab";

    [MenuItem("Tools/Level 1/Audit Scene Setup")]
    public static void AuditScene()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Audit", $"Không tìm thấy scene: {ScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Level 1 Audit ===");

        SoldierController soldier = Object.FindAnyObjectByType<SoldierController>();
        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HealthUIBootstrap[] bootstraps = Object.FindObjectsByType<HealthUIBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HealthBar[] healthBars = Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        report.AppendLine($"Scene: {scene.path}");
        report.AppendLine($"Soldier: {(soldier != null ? soldier.name + " @ " + soldier.transform.position : "MISSING")}");
        report.AppendLine($"Canvas count: {canvases.Length}");
        report.AppendLine($"HealthUIBootstrap: {bootstraps.Length}");
        report.AppendLine($"HealthBar: {healthBars.Length}");
        report.AppendLine($"Enemies: {enemies.Length}");

        if (soldier != null)
        {
            report.AppendLine($"  PlayerHealth: {soldier.GetComponent<PlayerHealth>() != null}");
            report.AppendLine($"  Animator: {soldier.GetComponent<Animator>() != null}");
            report.AppendLine($"  Tag: {soldier.tag}");
        }

        foreach (Canvas canvas in canvases)
        {
            Transform health = canvas.transform.Find("Health");
            report.AppendLine($"Canvas '{canvas.name}': Health child={(health != null)}");
        }

        if (soldier != null)
        {
            foreach (EnemyBase enemy in enemies)
            {
                float dist = Vector3.Distance(enemy.transform.position, soldier.transform.position);
                bool inDetection = dist <= enemy.detectionRange;
                report.AppendLine(
                    $"  {enemy.name}: dist={dist:F1}m | detection={enemy.detectionRange}m | attack={enemy.attackRange}m | inDetection={inDetection}");
            }
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Level 1 Audit", report.ToString(), "OK");
    }

    [MenuItem("Tools/Level 1/Fix Canvas Health UI")]
    public static void FixCanvasHealthUi()
    {
        FixCanvasPrefab();
        FixCanvasInOpenScene();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Fix", "Đã setup HealthUIBootstrap + HealthBar trên Canvas prefab và scene mở.", "OK");
    }

    [MenuItem("Tools/Level 1/Move Zombies Near Soldier (test)")]
    public static void MoveZombiesNearSoldier()
    {
        SoldierController soldier = Object.FindAnyObjectByType<SoldierController>();
        if (soldier == null)
        {
            EditorUtility.DisplayDialog("Move", "Không tìm thấy Soldier trong scene.", "OK");
            return;
        }

        EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int moved = 0;
        Vector3 basePos = soldier.transform.position + soldier.transform.forward * 8f;

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyBase enemy = enemies[i];
            Undo.RecordObject(enemy.transform, "Move zombie near soldier");
            Vector3 offset = new Vector3((i % 2 == 0 ? -3f : 3f), 0f, i * 2f);
            enemy.transform.position = basePos + offset;
            moved++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[SETUP] Moved {moved} enemies near Soldier at {basePos}");
        EditorUtility.DisplayDialog("Move", $"Đã đặt {moved} enemy cách Soldier ~8m để test chase.", "OK");
    }

    private static void FixCanvasPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        bool changed = EnsureCanvasHealthSetup(prefabRoot);
        if (changed)
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void FixCanvasInOpenScene()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (EnsureCanvasHealthSetup(canvas.gameObject))
                EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }
    }

    private static bool EnsureCanvasHealthSetup(GameObject canvasRoot)
    {
        bool changed = false;

        if (canvasRoot.GetComponent<HealthUIBootstrap>() == null)
        {
            canvasRoot.AddComponent<HealthUIBootstrap>();
            changed = true;
        }

        Transform health = canvasRoot.transform.Find("Health");
        if (health == null)
        {
            Debug.LogWarning($"[SETUP] Canvas '{canvasRoot.name}' không có child Health");
            return changed;
        }

        HealthBar healthBar = health.GetComponent<HealthBar>();
        if (healthBar == null)
        {
            healthBar = health.gameObject.AddComponent<HealthBar>();
            changed = true;
        }

        Transform fill = health.Find("Fill");
        Image fillImage = fill != null ? fill.GetComponent<Image>() : health.GetComponentInChildren<Image>(true);
        if (fillImage != null)
        {
            SerializedObject serializedHealthBar = new SerializedObject(healthBar);
            SerializedProperty fillProperty = serializedHealthBar.FindProperty("healthFill");
            if (fillProperty != null && fillProperty.objectReferenceValue == null)
            {
                fillProperty.objectReferenceValue = fillImage;
                serializedHealthBar.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        EditorUtility.SetDirty(canvasRoot);
        return changed;
    }
}
#endif
