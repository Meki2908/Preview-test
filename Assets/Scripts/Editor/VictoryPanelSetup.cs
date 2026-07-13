#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class VictoryPanelSetup
{
    private const string CanvasPrefabPath = MainGameCanvasUI.PrefabPath;

    [MenuItem("Tools/UI/Add Victory Panel To MainGame Canvas")]
    public static void AddVictoryPanelFromMenu()
    {
        AddVictoryPanelInternal(showDialog: true);
    }

    [MenuItem("Tools/UI/Repair Victory Panel On MainGame Canvas")]
    public static void RepairVictoryPanelFromMenu()
    {
        RepairVictoryPanelInternal(showDialog: true);
    }

    public static void AddVictoryPanelSilent()
    {
        AddVictoryPanelInternal(showDialog: false);
    }

    private static void AddVictoryPanelInternal(bool showDialog)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (prefabRoot == null)
        {
            string message = $"Không mở được prefab:\n{CanvasPrefabPath}";
            Debug.LogError(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Victory Panel Setup", message, "OK");
            return;
        }

        Transform existingVictory = FindChild(prefabRoot.transform, "Victory Panel");
        if (existingVictory != null)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            string existsMessage =
                "Victory Panel đã tồn tại.\n\nDùng Tools → UI → Repair Victory Panel On MainGame Canvas để sửa OnClick / thứ tự layer.";
            Debug.Log($"[Victory Panel Setup] {existsMessage}");
            if (showDialog)
                EditorUtility.DisplayDialog("Victory Panel Setup", existsMessage, "OK");
            return;
        }

        Transform lostPanel = FindChild(prefabRoot.transform, "Lost Panel");
        if (lostPanel == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            string message = "Không tìm thấy Lost Panel để duplicate.";
            Debug.LogError(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Victory Panel Setup", message, "OK");
            return;
        }

        GameObject victoryObject = Object.Instantiate(lostPanel.gameObject, lostPanel.parent);
        victoryObject.name = "Victory Panel";
        victoryObject.SetActive(false);

        RenameButton(victoryObject.transform, "Retry", "Play Again");
        RenameButton(victoryObject.transform, "Retry Button", "Play Again Button");
        TintVictoryText(victoryObject.transform);
        CleanupVictoryPanelEditor(victoryObject);

        SavePrefab(prefabRoot, showDialog, "Đã tạo Victory Panel trên MainGame Canvas prefab.");
    }

    private static void RepairVictoryPanelInternal(bool showDialog)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (prefabRoot == null)
        {
            string message = $"Không mở được prefab:\n{CanvasPrefabPath}";
            Debug.LogError(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Victory Panel Repair", message, "OK");
            return;
        }

        GameObject victoryObject = FindChild(prefabRoot.transform, "Victory Panel")?.gameObject;
        if (victoryObject == null)
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            string message = "Không tìm thấy Victory Panel. Chạy Add Victory Panel trước.";
            Debug.LogError(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Victory Panel Repair", message, "OK");
            return;
        }

        CleanupVictoryPanelEditor(victoryObject);
        SavePrefab(
            prefabRoot,
            showDialog,
            "Đã repair Victory Panel:\n- Xóa LostPanel + OnClick cũ\n- Gắn VictoryPanel\n- Đưa panel xuống cuối Hierarchy");
    }

    private static void CleanupVictoryPanelEditor(GameObject victoryObject)
    {
        LostPanel staleLostPanel = victoryObject.GetComponent<LostPanel>();
        if (staleLostPanel != null)
            Object.DestroyImmediate(staleLostPanel);

        if (victoryObject.GetComponent<VictoryPanel>() == null)
            victoryObject.AddComponent<VictoryPanel>();

        Button[] buttons = victoryObject.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].onClick = new Button.ButtonClickedEvent();

        victoryObject.transform.SetAsLastSibling();
    }

    private static void SavePrefab(GameObject prefabRoot, bool showDialog, string success)
    {
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Victory Panel Setup] {success}");
        if (showDialog)
            EditorUtility.DisplayDialog("Victory Panel", success, "OK");
    }

    private static void RenameButton(Transform root, string oldName, string newName)
    {
        Transform button = FindChild(root, oldName);
        if (button != null)
            button.name = newName;
    }

    private static void TintVictoryText(Transform root)
    {
        TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        Color victoryGold = new Color(1f, 0.85f, 0.2f, 1f);

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].GetComponentInParent<Button>() != null)
                continue;

            labels[i].color = victoryGold;
            if (labels[i].text.Contains("Lost", System.StringComparison.OrdinalIgnoreCase)
                || labels[i].text.Contains("Game Over", System.StringComparison.OrdinalIgnoreCase))
            {
                labels[i].text = "CHIẾN THẮNG!";
            }
        }
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
