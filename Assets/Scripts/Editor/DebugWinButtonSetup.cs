#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DebugWinButtonSetup
{
    private const string CanvasPrefabPath = MainGameCanvasUI.PrefabPath;
    private const string ButtonName = "Debug Win Button";

    [MenuItem("Tools/UI/Add Debug Win Button To Pause Panel")]
    public static void AddDebugWinButtonFromMenu()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("Debug Win Button", $"Không mở được prefab:\n{CanvasPrefabPath}", "OK");
            return;
        }

        Transform pausePanel = FindChild(prefabRoot.transform, "Pause Panel");
        if (pausePanel == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            EditorUtility.DisplayDialog("Debug Win Button", "Không tìm thấy Pause Panel.", "OK");
            return;
        }

        Transform existing = FindChild(pausePanel, ButtonName);
        if (existing != null)
        {
            ConfigureButton(existing.gameObject);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Debug Win Button", "Debug Win Button đã có — đã refresh label/OnClick.", "OK");
            return;
        }

        Button template = FindTemplateButton(pausePanel);
        if (template == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            EditorUtility.DisplayDialog("Debug Win Button", "Không tìm thấy nút mẫu (Continue/Menu) trong Pause Panel.", "OK");
            return;
        }

        GameObject debugButton = Object.Instantiate(template.gameObject, pausePanel);
        debugButton.name = ButtonName;
        ConfigureButton(debugButton);

        RectTransform rect = debugButton.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition += new Vector2(0f, -70f);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Debug Win Button",
            "Đã thêm Debug Win Button vào Pause Panel.\n\n" +
            "Lv1: Pause → Cheat Skip → sang Level 2\n" +
            "Lv2: Pause → Cheat Skip → Victory panel",
            "OK");
    }

    private static void ConfigureButton(GameObject debugButton)
    {
        Button button = debugButton.GetComponent<Button>();
        if (button != null)
            button.onClick = new Button.ButtonClickedEvent();

        TextMeshProUGUI label = debugButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "Cheat Skip";

        if (debugButton.GetComponent<DebugWinButtonVisibility>() == null)
            debugButton.AddComponent<DebugWinButtonVisibility>();
    }

    private static Button FindTemplateButton(Transform pausePanel)
    {
        Button continueButton = FindChild(pausePanel, "Continue")?.GetComponent<Button>();
        if (continueButton != null)
            return continueButton;

        return FindChild(pausePanel, "Menu")?.GetComponent<Button>();
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                return children[i];
        }

        return null;
    }
}
#endif
