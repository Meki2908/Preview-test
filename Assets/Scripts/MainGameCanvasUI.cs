using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tìm và wire UI trên MainGame Canvas prefab.
/// </summary>
public static class MainGameCanvasUI
{
    public const string PrefabPath = "Assets/Prefabs/MainGame Canvas.prefab";
    public const string ResourcesPrefabName = "MainGame Canvas";

    public static GameObject LoadCanvasPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabName);
#if UNITY_EDITOR
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }
#endif
        return prefab;
    }

    private static readonly string[] OverlayPanelNames =
    {
        "Sound Settings",
        "Settings Panel",
        "SettingsPanel",
        "Pause Panel",
        "Lost Panel",
        "Record",
        "Records",
        "Record Panel",
        "Records Panel",
        "Guide Panel",
        "First Guide",
        "Second Guide",
        "Guide 2"
    };

    public static GameObject FindChild(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return children[i].gameObject;
        }

        return null;
    }

    public static Button FindMenuButton(string exactName)
    {
        return FindMenuButtonInActiveScene(exactName);
    }

    /// <summary>Chỉ tìm nút trong scene đang active — tránh wire nhầm canvas DDOL / ingame.</summary>
    public static Button FindMenuButtonInActiveScene(params string[] objectNames)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || objectNames == null || objectNames.Length == 0)
            return null;

        Button fallback = null;
        for (int n = 0; n < objectNames.Length; n++)
        {
            Button found = FindMenuButtonInScene(scene, objectNames[n], ref fallback);
            if (found != null)
                return found;
        }

        return fallback;
    }

    private static Button FindMenuButtonInScene(Scene scene, string exactName, ref Button fallback)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate.gameObject.scene != scene)
                    continue;

                if (!candidate.name.Equals(exactName, StringComparison.OrdinalIgnoreCase))
                    continue;

                Button button = candidate.GetComponent<Button>();
                if (button == null)
                    continue;

                if (IsInsideIngameCanvas(candidate))
                    continue;

                if (IsInsideOverlayPanel(candidate))
                {
                    fallback ??= button;
                    continue;
                }

                return button;
            }
        }

        return null;
    }

    private static bool IsInsideIngameCanvas(Transform transform)
    {
        Canvas canvas = transform.GetComponentInParent<Canvas>();
        if (canvas == null)
            return false;

        return FindChild(canvas.transform, "Pause Button") != null
            || FindChild(canvas.transform, "Pause Panel") != null
            || FindChild(canvas.transform, "Lost Panel") != null;
    }

    public static void ConfigureSettingsOnlyMode(Transform canvasRoot, Action onBack)
    {
        if (canvasRoot == null)
            return;

        GameObject settingsPanel = FindChild(canvasRoot, "Settings Panel");
        if (settingsPanel == null)
        {
            Debug.LogWarning("MainGameCanvasUI: không tìm thấy Settings Panel.", canvasRoot);
            return;
        }

        for (int i = 0; i < canvasRoot.childCount; i++)
        {
            Transform child = canvasRoot.GetChild(i);
            child.gameObject.SetActive(child == settingsPanel.transform);
        }

        settingsPanel.SetActive(true);
        WireSettingsPanel(settingsPanel, onBack);
    }

    public static void WireIngameCanvas(Transform canvasRoot, IngameUI ingameUi)
    {
        if (canvasRoot == null || ingameUi == null)
            return;

        GameObject pauseButtonObject = FindChild(canvasRoot, "Pause Button");
        GameObject pausePanel = FindChild(canvasRoot, "Pause Panel");
        GameObject settingsPanel = FindChild(canvasRoot, "Settings Panel");

        if (pauseButtonObject == null || pausePanel == null)
            return;

        ingameUi.Configure(pausePanel, settingsPanel);

        WireButton(pauseButtonObject.GetComponent<Button>(), ingameUi.TogglePause);
        WirePausePanel(pausePanel.transform, ingameUi);
        WireSettingsPanel(settingsPanel, ingameUi.CloseSettings);
        WireLostPanel(canvasRoot);
    }

    public static void RetryBindLostPanels()
    {
        LostPanel[] panels = UnityEngine.Object.FindObjectsByType<LostPanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
            panels[i].BindToGameManager();
    }

    public static bool HasLostPanelInScene()
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (FindChild(canvases[i].transform, "Lost Panel") != null)
                return true;
        }

        return false;
    }

    private static void WireLostPanel(Transform canvasRoot)
    {
        GameObject lostPanel = FindChild(canvasRoot, "Lost Panel");
        if (lostPanel == null)
            return;

        LostPanel controller = GetOrAdd<LostPanel>(lostPanel);
        Transform root = lostPanel.transform;

        WireButton(GetButton(root, "Retry") ?? GetButton(root, "Retry Button"), controller.Retry);
        WireButton(GetButton(root, "Menu") ?? GetButton(root, "Menu Button"), controller.Menu);

        controller.BindToGameManager();
        lostPanel.SetActive(false);
    }

    private static void WirePausePanel(Transform pausePanel, IngameUI ingameUi)
    {
        PausePanel menuController = GetOrAdd<PausePanel>(pausePanel.gameObject);

        WireButton(GetButton(pausePanel, "Continue"), ingameUi.Continue);
        WireButton(GetButton(pausePanel, "Settings"), ingameUi.Settings);
        WireButton(GetButton(pausePanel, "Menu"), menuController.Menu);
    }

    public static void WireSettingsPanel(GameObject settingsPanel, Action onBack)
    {
        if (settingsPanel == null)
            return;

        Transform root = settingsPanel.transform;
        Slider musicSlider = GetSlider(root, "Music Slider");
        Slider sfxSlider = GetSlider(root, "SFX Adjust") ?? GetSlider(root, "SFX Slider");

        SettingsManager settings = GetOrAdd<SettingsManager>(settingsPanel);
        settings.Configure(musicSlider, sfxSlider);

        WireSlider(musicSlider, settings.ApplyMusicVolume);
        WireSlider(sfxSlider, settings.ApplySFXVolume);

        Button backButton = GetButton(root, "Back") ?? GetButton(root, "Close");
        if (backButton != null && onBack != null)
            WireButton(backButton, () => onBack());
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null || button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(action);
    }

    private static void WireSlider(Slider slider, UnityEngine.Events.UnityAction action)
    {
        if (slider == null || action == null || slider.onValueChanged.GetPersistentEventCount() > 0)
            return;

        slider.onValueChanged.AddListener(_ => action());
    }

    private static Button GetButton(Transform root, string objectName)
    {
        GameObject found = FindChild(root, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Slider GetSlider(Transform root, string objectName)
    {
        GameObject found = FindChild(root, objectName);
        return found != null ? found.GetComponent<Slider>() : null;
    }

    private static bool IsInsideOverlayPanel(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            string name = current.name;
            for (int i = 0; i < OverlayPanelNames.Length; i++)
            {
                if (name.Equals(OverlayPanelNames[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
