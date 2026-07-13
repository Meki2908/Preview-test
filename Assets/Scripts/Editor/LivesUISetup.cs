#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public static class LivesUISetup
{
    private const string CanvasPrefabPath = MainGameCanvasUI.PrefabPath;

    [MenuItem("Tools/UI/Attach LivesUI To x3 Text")]
    public static void AttachFromMenu()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("Lives UI Setup", $"Không mở được:\n{CanvasPrefabPath}", "OK");
            return;
        }

        int attached = 0;
        TextMeshProUGUI[] labels = prefabRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (!IsLivesLabel(labels[i]))
                continue;

            if (labels[i].GetComponent<LivesUI>() == null)
            {
                labels[i].gameObject.AddComponent<LivesUI>();
                attached++;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.SaveAssets();

        string message = attached > 0
            ? $"Đã gắn LivesUI lên {attached} text."
            : "Không tìm thấy text 'x3' hoặc LivesUI đã có sẵn.";
        Debug.Log($"[Lives UI Setup] {message}");
        EditorUtility.DisplayDialog("Lives UI Setup", message, "OK");
    }

    private static bool IsLivesLabel(TextMeshProUGUI label)
    {
        if (label == null)
            return false;

        string name = label.gameObject.name;
        if (name.Equals("x3", System.StringComparison.OrdinalIgnoreCase)
            || name.Equals("Lives", System.StringComparison.OrdinalIgnoreCase)
            || name.Equals("Lives Text", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return label.text != null
            && label.text.StartsWith("x", System.StringComparison.OrdinalIgnoreCase)
            && label.text.Length <= 3;
    }
}
#endif
