#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup một lần trên MainGame Canvas prefab: SafeAreaContainer + kéo HUD root vào container.
/// Không reparent runtime — tránh méo RectTransform / tách icon khỏi nút.
/// </summary>
public static class SafeAreaContainerSetup
{
    private const string CanvasPrefabPath = MainGameCanvasUI.PrefabPath;

    /// <summary>HUD root — chỉ tên cha, không liệt kê icon con.</summary>
    private static readonly string[] HudRootNames =
    {
        "Pause Button",
        "Left Joystick",
        "Right Joystick",
        "Ammo",
        "Reload button",
        "Switch guns",
        "Throw grenade",
        "Lives",
        "Health"
    };

    private static readonly string[] OverlayPanelNames =
    {
        "Pause Panel",
        "Lost Panel",
        "Victory Panel",
        "Settings Panel",
        "Sound Settings",
        "SettingsPanel",
        "Record",
        "Records",
        "Record Panel",
        "Records Panel",
        "Guide Panel",
        "First Guide",
        "Second Guide",
        "Guide 2",
        MainGameCanvasMobileLayout.ContainerName
    };

    [MenuItem("Tools/UI/Setup Mobile Safe Area On MainGame Canvas")]
    public static void SetupFromMenu()
    {
        ApplyToPrefab(showDialog: true);
    }

    /// <summary>Unity -batchmode -executeMethod SafeAreaContainerSetup.RunFromCommandLine</summary>
    public static void RunFromCommandLine()
    {
        ApplyToPrefab(showDialog: false);
        EditorApplication.Exit(0);
    }

    private static void ApplyToPrefab(bool showDialog)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        if (prefabRoot == null)
        {
            string message = $"Không mở được prefab:\n{CanvasPrefabPath}";
            Debug.LogError(message);
            if (showDialog)
                EditorUtility.DisplayDialog("Safe Area Setup", message, "OK");
            return;
        }

        Transform canvasRoot = prefabRoot.transform;
        bool changed = MainGameCanvasMobileLayout.Apply(canvasRoot);
        changed |= EnsureSafeAreaContainer(canvasRoot);
        changed |= RepairDetachedIcons(canvasRoot);
        changed |= MoveHudRootsUnderContainer(canvasRoot);
        changed |= LeftTouchZoneSetup.ApplyToOpenPrefabContents(canvasRoot);

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
            "Safe Area Setup",
            changed
                ? "Đã setup SafeAreaContainer + Canvas Scaler + Left Touch Zone.\n\nTest Game view 2960×1440 hoặc build APK."
                : "Prefab đã đúng cấu hình — không có thay đổi.",
            "OK");
    }

    private static bool EnsureSafeAreaContainer(Transform canvasRoot)
    {
        bool changed = false;
        Transform container = FindChild(canvasRoot, MainGameCanvasMobileLayout.ContainerName);

        if (container == null)
        {
            GameObject containerObject = new GameObject(
                MainGameCanvasMobileLayout.ContainerName,
                typeof(RectTransform),
                typeof(SafeArea));

            container = containerObject.transform;
            container.SetParent(canvasRoot, false);
            changed = true;
        }
        else if (container.GetComponent<SafeArea>() == null)
        {
            container.gameObject.AddComponent<SafeArea>();
            changed = true;
        }

        RectTransform rect = container as RectTransform;
        if (rect != null)
        {
            if (rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one
                || rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                changed = true;
            }

            if (rect.localScale != Vector3.one)
            {
                rect.localScale = Vector3.one;
                changed = true;
            }
        }

        if (container.GetSiblingIndex() != 0)
        {
            container.SetAsFirstSibling();
            changed = true;
        }

        Image image = container.GetComponent<Image>();
        if (image != null)
        {
            Object.DestroyImmediate(image);
            changed = true;
        }

        return changed;
    }

    /// <summary>Sửa icon bị tách khỏi nút do lần reparent runtime trước đó.</summary>
    private static bool RepairDetachedIcons(Transform canvasRoot)
    {
        bool changed = false;
        changed |= TryReparentIcon(canvasRoot, "Reload icon", "Reload button");
        changed |= TryReparentIcon(canvasRoot, "Guns icon", "Switch guns");
        return changed;
    }

    private static bool TryReparentIcon(Transform canvasRoot, string iconName, string buttonName)
    {
        Transform icon = FindChild(canvasRoot, iconName);
        Transform button = FindChild(canvasRoot, buttonName);
        if (icon == null || button == null)
            return false;

        if (icon.parent == button)
            return false;

        icon.SetParent(button, false);
        return true;
    }

    private static bool MoveHudRootsUnderContainer(Transform canvasRoot)
    {
        Transform container = FindChild(canvasRoot, MainGameCanvasMobileLayout.ContainerName);
        if (container == null)
            return false;

        bool changed = false;

        for (int i = 0; i < HudRootNames.Length; i++)
        {
            Transform hudRoot = FindChild(canvasRoot, HudRootNames[i]);
            if (hudRoot == null || hudRoot == container || hudRoot.parent == container)
                continue;

            if (IsOverlay(hudRoot))
                continue;

            if (hudRoot.parent != canvasRoot && !hudRoot.parent.IsChildOf(container))
                continue;

            hudRoot.SetParent(container, false);
            changed = true;
        }

        changed |= MoveDirectChildWithComponent<HoldButton>(canvasRoot, container);
        changed |= MoveDirectChildWithComponent<GrenadeJoystick>(canvasRoot, container);
        changed |= MoveDirectChildWithComponent<AmmoDisplay>(canvasRoot, container);
        changed |= MoveDirectChildWithComponent<SoldierCombatUI>(canvasRoot, container);

        return changed;
    }

    private static bool MoveDirectChildWithComponent<T>(Transform canvasRoot, Transform container)
        where T : Component
    {
        bool changed = false;
        T[] components = canvasRoot.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Transform target = components[i].transform;
            if (target.parent != canvasRoot)
                continue;

            if (target == container || target.IsChildOf(container))
                continue;

            if (IsOverlay(target))
                continue;

            target.SetParent(container, false);
            changed = true;
        }

        return changed;
    }

    private static bool IsOverlay(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.name;
            for (int i = 0; i < OverlayPanelNames.Length; i++)
            {
                if (name.Equals(OverlayPanelNames[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            current = current.parent;
        }

        return false;
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
