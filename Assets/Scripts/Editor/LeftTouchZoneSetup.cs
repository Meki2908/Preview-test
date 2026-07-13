#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Terresquall;

/// <summary>
/// Tạo Left Touch Zone trên MainGame Canvas prefab (dynamic left joystick).
/// </summary>
public static class LeftTouchZoneSetup
{
    private const string CanvasPrefabPath = MainGameCanvasUI.PrefabPath;
    private const string ZoneName = "Left Touch Zone";
    private const string JoystickName = "Left Joystick";

    [MenuItem("Tools/UI/Setup Left Touch Zone On MainGame Canvas")]
    public static void SetupFromMenu()
    {
        ApplyToPrefab(showDialog: true);
    }

    public static void RunFromCommandLine()
    {
        ApplyToPrefab(showDialog: false);
        EditorApplication.Exit(0);
    }

    public static bool ApplyToOpenPrefabContents(Transform canvasRoot)
    {
        if (canvasRoot == null)
            return false;

        Transform container = FindChild(canvasRoot, MainGameCanvasMobileLayout.ContainerName);
        if (container == null)
            return false;

        Transform joystick = FindChild(container, JoystickName);
        if (joystick == null)
            joystick = FindChild(canvasRoot, JoystickName);

        if (joystick == null)
        {
            Debug.LogError($"[LeftTouchZone] Không tìm thấy '{JoystickName}'.");
            return false;
        }

        bool changed = ConfigureJoystickRaycast(joystick);
        changed |= EnsureTouchZone(container, joystick);
        return changed;
    }

    private static void ApplyToPrefab(bool showDialog)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (prefabRoot == null)
        {
            string message = $"Không mở được prefab:\n{CanvasPrefabPath}";
            Debug.LogError(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Left Touch Zone", message, "OK");
            return;
        }

        bool changed = ApplyToOpenPrefabContents(prefabRoot.transform);

        if (changed)
        {
            EditorUtility.SetDirty(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasPrefabPath);
            AssetDatabase.SaveAssets();
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);

        if (!showDialog)
            return;

        EditorUtility.DisplayDialog(
            "Left Touch Zone",
            changed
                ? "Đã tạo Left Touch Zone (nửa trái màn hình).\n\nChạm bất kỳ đâu bên trái để dời Left Joystick."
                : "Left Touch Zone đã được setup — không có thay đổi.",
            "OK");
    }

    private static bool EnsureTouchZone(Transform container, Transform joystick)
    {
        bool changed = false;
        Transform zoneTransform = FindChild(container, ZoneName);

        GameObject zoneObject;
        if (zoneTransform == null)
        {
            zoneObject = new GameObject(ZoneName, typeof(RectTransform), typeof(Image), typeof(DynamicTouchZone));
            zoneTransform = zoneObject.transform;
            zoneTransform.SetParent(container, false);
            changed = true;
        }
        else
        {
            zoneObject = zoneTransform.gameObject;
            if (zoneObject.GetComponent<Image>() == null)
                zoneObject.AddComponent<Image>();
            if (zoneObject.GetComponent<DynamicTouchZone>() == null)
                zoneObject.AddComponent<DynamicTouchZone>();
        }

        RectTransform zoneRect = zoneTransform as RectTransform;
        if (zoneRect != null)
        {
            if (zoneRect.anchorMin != new Vector2(0f, 0f) || zoneRect.anchorMax != new Vector2(0.5f, 1f)
                || zoneRect.offsetMin != Vector2.zero || zoneRect.offsetMax != Vector2.zero)
            {
                zoneRect.anchorMin = new Vector2(0f, 0f);
                zoneRect.anchorMax = new Vector2(0.5f, 1f);
                zoneRect.offsetMin = Vector2.zero;
                zoneRect.offsetMax = Vector2.zero;
                zoneRect.localScale = Vector3.one;
                changed = true;
            }
        }

        Image zoneImage = zoneObject.GetComponent<Image>();
        if (zoneImage != null)
        {
            Color color = zoneImage.color;
            if (color.a != 0f)
            {
                color.a = 0f;
                zoneImage.color = color;
                changed = true;
            }

            if (!zoneImage.raycastTarget)
            {
                zoneImage.raycastTarget = true;
                changed = true;
            }
        }

        DynamicTouchZone touchZone = zoneObject.GetComponent<DynamicTouchZone>();
        VirtualJoystick virtualJoystick = joystick.GetComponent<VirtualJoystick>();
        if (touchZone != null)
        {
            touchZone.Configure(joystick as RectTransform, virtualJoystick);
            changed = true;
        }

        if (zoneTransform.GetSiblingIndex() != container.childCount - 1)
        {
            zoneTransform.SetAsLastSibling();
            changed = true;
        }

        return changed;
    }

    private static bool ConfigureJoystickRaycast(Transform joystick)
    {
        Image joystickImage = joystick.GetComponent<Image>();
        if (joystickImage == null || !joystickImage.raycastTarget)
            return false;

        joystickImage.raycastTarget = false;
        return true;
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
