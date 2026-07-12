#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MenuSceneSetup
{
    private const string ScenePath = "Assets/Scenes/Menu.unity";
    private const string ControllerName = "[MENU_CONTROLLER]";

    private static readonly string[] BuildScenePaths =
    {
        "Assets/Scenes/Menu.unity",
        "Assets/Scenes/Level 1.unity",
        "Assets/Scenes/Level 2.unity"
    };

    [MenuItem("Tools/Menu/Setup Menu Scene")]
    public static void SetupMenuScene()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Menu Setup", $"Không tìm thấy: {ScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Menu Scene Setup ===");

        EnsureEventSystem(report);
        EnsureGameManager(report);
        MainMenuController controller = EnsureMenuController(report);
        MainMenu mainMenu = EnsureMainMenu(controller, report);
        EnsureMenuSettingsOverlay(controller, report);

        WirePlayButtons(controller, report);
        WireButton("Exit", controller.QuitGame, report);
        if (mainMenu != null)
        {
            WireButton("Settings", mainMenu.OpenSettings, report);
            WireButton("Guide", mainMenu.OpenFirstGuidePanel, report);
            WireButton("Records", mainMenu.OpenRecordsPanel, report);
            WireCloseButtons(mainMenu, report);
        }

        HideOverlayPanels(mainMenu, report);

        int buildChanges = EnsureBuildSettings(report);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        report.AppendLine($"Build Settings: {buildChanges} scene(s) updated");
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Menu Setup", report.ToString(), "OK");
    }

    [MenuItem("Tools/Menu/Audit Menu Scene")]
    public static void AuditMenuScene()
    {
        if (!System.IO.File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Menu Audit", $"Không tìm thấy: {ScenePath}", "OK");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Menu Audit ===");
        report.AppendLine($"EventSystem: {Object.FindAnyObjectByType<EventSystem>() != null}");
        report.AppendLine($"GameManager: {Object.FindAnyObjectByType<GameManager>() != null}");
        report.AppendLine($"MainMenuController: {Object.FindAnyObjectByType<MainMenuController>() != null}");
        report.AppendLine($"MainMenu: {Object.FindAnyObjectByType<MainMenu>() != null}");

        foreach (string buttonName in new[] { "Play", "Start", "Exit", "Settings", "Guide", "Records" })
        {
            Button button = FindButton(buttonName);
            report.AppendLine($"Button '{buttonName}': {(button != null ? CountListeners(button) + " listener(s)" : "MISSING")}");
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Menu Audit", report.ToString(), "OK");
    }

    private static void EnsureEventSystem(System.Text.StringBuilder report)
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
        {
            report.AppendLine("EventSystem: OK");
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        report.AppendLine("EventSystem: CREATED");
    }

    private static void EnsureGameManager(System.Text.StringBuilder report)
    {
        GameManager existing = Object.FindAnyObjectByType<GameManager>();
        if (existing != null)
        {
            if (existing.gameObject.name != "Game Manager" && existing.gameObject.name != "[GAME_MANAGER]")
                existing.gameObject.name = "[GAME_MANAGER]";
            report.AppendLine($"GameManager: OK ({existing.gameObject.name})");
            return;
        }

        GameObject go = new GameObject("[GAME_MANAGER]");
        go.AddComponent<GameManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create GameManager");
        report.AppendLine("GameManager: CREATED");
    }

    private static MainMenuController EnsureMenuController(System.Text.StringBuilder report)
    {
        MainMenuController controller = Object.FindAnyObjectByType<MainMenuController>();
        if (controller != null)
        {
            if (controller.gameObject.name != ControllerName)
                controller.gameObject.name = ControllerName;
            report.AppendLine("MainMenuController: OK");
            return controller;
        }

        GameObject go = new GameObject(ControllerName);
        controller = go.AddComponent<MainMenuController>();
        Undo.RegisterCreatedObjectUndo(go, "Create Menu Controller");
        report.AppendLine("MainMenuController: CREATED");
        return controller;
    }

    private static MainMenu EnsureMainMenu(MainMenuController controller, System.Text.StringBuilder report)
    {
        MainMenu mainMenu = Object.FindAnyObjectByType<MainMenu>();
        if (mainMenu == null)
            mainMenu = controller.gameObject.AddComponent<MainMenu>();

        SerializedObject serialized = new SerializedObject(mainMenu);
        AssignPanelRef(serialized, "recordPanel", FindPanel("Record", "Records", "Record Panel", "Records Panel"), report);
        AssignPanelRef(serialized, "firstGuidePanel", FindPanel("Guide Panel", "First Guide", "FirstGuide"), report);
        AssignPanelRef(serialized, "secondGuidePanel", FindPanel("Second Guide", "SecondGuide", "Guide 2"), report);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        report.AppendLine("MainMenu: wired panel refs");
        return mainMenu;
    }

    private static void AssignPanelRef(SerializedObject serialized, string propertyName, GameObject panel, System.Text.StringBuilder report)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || panel == null)
            return;

        if (property.objectReferenceValue != panel)
        {
            property.objectReferenceValue = panel;
            report.AppendLine($"  {propertyName} -> {panel.name}");
        }
    }

    private static void WirePlayButtons(MainMenuController controller, System.Text.StringBuilder report)
    {
        int wired = 0;
        foreach (string name in new[] { "Play", "Start" })
        {
            Button button = FindButton(name);
            if (button != null && WireButtonListener(button, controller.StartGame))
            {
                wired++;
                report.AppendLine($"Button '{name}' -> MainMenuController.StartGame");
            }
        }

        if (wired == 0)
            report.AppendLine("WARNING: Không tìm thấy nút Play/Start");
    }

    private static void EnsureMenuSettingsOverlay(MainMenuController controller, System.Text.StringBuilder report)
    {
        MenuSettingsOverlay overlay = controller.GetComponent<MenuSettingsOverlay>();
        if (overlay == null)
            overlay = controller.gameObject.AddComponent<MenuSettingsOverlay>();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainGameCanvasUI.PrefabPath);
        if (prefab == null)
        {
            report.AppendLine($"WARNING: Không tìm thấy {MainGameCanvasUI.PrefabPath}");
            return;
        }

        SerializedObject serialized = new SerializedObject(overlay);
        SerializedProperty prefabProperty = serialized.FindProperty("mainGameCanvasPrefab");
        if (prefabProperty != null && prefabProperty.objectReferenceValue != prefab)
        {
            prefabProperty.objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            report.AppendLine("MenuSettingsOverlay: assigned MainGame Canvas prefab");
        }
        else
        {
            report.AppendLine("MenuSettingsOverlay: OK");
        }
    }

    private static void WireButton(string buttonName, UnityEngine.Events.UnityAction method, System.Text.StringBuilder report)
    {
        Button button = buttonName == "Settings"
            ? MainGameCanvasUI.FindMenuButton(buttonName)
            : FindButton(buttonName);
        if (button == null)
            return;

        if (WireButtonListener(button, method))
            report.AppendLine($"Button '{buttonName}' -> {method.Method.Name}");
    }

    private static void WireCloseButtons(MainMenu mainMenu, System.Text.StringBuilder report)
    {
        WireButtonsByNameContains("Close", mainMenu.CloseSettings, report);
        WireButtonsByNameContains("Back", mainMenu.CloseSettings, report);

        Button closeRecords = FindButton("Close Records");
        if (closeRecords != null)
            WireButtonListener(closeRecords, mainMenu.CloseRecordsPanel);

        Button closeGuide = FindButton("Close Guide");
        if (closeGuide != null)
            WireButtonListener(closeGuide, mainMenu.CloseFirstGuidePanel);
    }

    private static void WireButtonsByNameContains(string namePart, UnityEngine.Events.UnityAction method, System.Text.StringBuilder report)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0
                && WireButtonListener(button, method))
            {
                report.AppendLine($"Button '{button.name}' -> {method.Method.Name}");
            }
        }
    }

    private static bool WireButtonListener(Button button, UnityEngine.Events.UnityAction method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == method.Method.Name)
                return false;
        }

        UnityEventTools.AddPersistentListener(button.onClick, method);
        EditorUtility.SetDirty(button);
        return true;
    }

    private static void HideOverlayPanels(MainMenu mainMenu, System.Text.StringBuilder report)
    {
        if (mainMenu == null)
            return;

        SerializedObject serialized = new SerializedObject(mainMenu);
        HidePanel(serialized.FindProperty("recordPanel"), report);
        HidePanel(serialized.FindProperty("firstGuidePanel"), report);
        HidePanel(serialized.FindProperty("secondGuidePanel"), report);
    }

    private static void HidePanel(SerializedProperty property, System.Text.StringBuilder report)
    {
        if (property?.objectReferenceValue is not GameObject panel)
            return;

        if (panel.activeSelf)
        {
            panel.SetActive(false);
            report.AppendLine($"Panel '{panel.name}' set inactive");
        }
    }

    private static int EnsureBuildSettings(System.Text.StringBuilder report)
    {
        var scenes = new List<EditorBuildSettingsScene>();
        int changes = 0;

        foreach (string path in BuildScenePaths)
        {
            if (!System.IO.File.Exists(path))
            {
                report.AppendLine($"Build: SKIP missing {path}");
                continue;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
            changes++;
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        report.AppendLine("Build order: 0=Menu, 1=Level 1, 2=Level 2");
        return changes;
    }

    private static Button FindButton(string exactName)
    {
        GameObject go = FindObject(exactName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private static GameObject FindPanel(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject go = FindObject(name);
            if (go != null)
                return go;
        }

        return null;
    }

    private static GameObject FindObject(string exactName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return transforms.FirstOrDefault(t => t.name == exactName)?.gameObject;
    }

    private static int CountListeners(Button button)
    {
        return button.onClick.GetPersistentEventCount();
    }
}
#endif
