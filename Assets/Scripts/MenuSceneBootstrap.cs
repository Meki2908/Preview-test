using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tự setup Menu scene khi load (wire nút + panel) nếu Inspector chưa gán.
/// </summary>
public static class MenuSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMenuScene(scene))
            return;

        SetupMenuScene();
    }

    private static void SetupMenuScene()
    {
        Time.timeScale = 1f;
        CursorController.ShowCursor();

        if (GameManager.Instance != null)
            GameManager.Instance.PrepareForMenuScene();

        MenuSettingsOverlay existingOverlay = Object.FindAnyObjectByType<MenuSettingsOverlay>();
        if (existingOverlay != null)
            existingOverlay.Close();

        GameObject host = FindOrCreateControllerHost();
        MainMenuController controller = GetOrAdd<MainMenuController>(host);
        MainMenu mainMenu = GetOrAdd<MainMenu>(host);
        MenuSettingsOverlay settingsOverlay = GetOrAdd<MenuSettingsOverlay>(host);
        settingsOverlay.EnsurePrefabAssigned();

        mainMenu.TryBindPanels(
            FindObjectInActiveScene("Record"),
            FindObjectInActiveScene("Records"),
            FindObjectInActiveScene("Guide Panel"),
            FindObjectInActiveScene("First Guide"),
            FindObjectInActiveScene("Second Guide"),
            FindObjectInActiveScene("Guide 2"));
        mainMenu.HideAllPanels();
        DeactivateLegacyPanel("Sound Settings");
        DeactivateLegacyPanel("Lost Panel");
        DeactivateLegacyPanel("Victory Panel");

        WireOnce(MainGameCanvasUI.FindMenuButtonInActiveScene("Play", "Play Button"), controller.StartGame);
        WireOnce(MainGameCanvasUI.FindMenuButtonInActiveScene("Start", "Start Button"), controller.StartGame);
        WireOnce(MainGameCanvasUI.FindMenuButtonInActiveScene("Exit", "Exit Button"), controller.QuitGame);
        WireOnce(MainGameCanvasUI.FindMenuButtonInActiveScene("Settings", "Settings Button"), mainMenu.OpenSettings);
        WireOnce(MainGameCanvasUI.FindMenuButtonInActiveScene("Guide", "Guide Button"), mainMenu.OpenFirstGuidePanel);
        WireOnce(MainGameCanvasUI.FindMenuButtonInActiveScene("Records", "Records Button"), mainMenu.OpenRecordsPanel);
    }

    private static GameObject FindOrCreateControllerHost()
    {
        MainMenuController existing = Object.FindAnyObjectByType<MainMenuController>();
        if (existing != null)
            return existing.gameObject;

        GameObject host = GameObject.Find("[MENU_CONTROLLER]");
        if (host == null)
            host = new GameObject("[MENU_CONTROLLER]");

        return host;
    }

    private static T GetOrAdd<T>(GameObject host) where T : Component
    {
        T onHost = host.GetComponent<T>();
        if (onHost != null)
            return onHost;

        T inScene = Object.FindAnyObjectByType<T>();
        return inScene != null ? inScene : host.AddComponent<T>();
    }

    private static void WireOnce(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void DeactivateLegacyPanel(string panelName)
    {
        GameObject panel = FindObjectInActiveScene(panelName);
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }

    private static GameObject FindObjectInActiveScene(string exactName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int r = 0; r < roots.Length; r++)
        {
            Transform[] transforms = roots[r].GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate.gameObject.scene != scene)
                    continue;

                if (candidate.name == exactName)
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static bool IsMenuScene(Scene scene)
    {
        if (scene.buildIndex == GameSceneIndex.Menu)
            return true;

        if (scene.name == "Menu")
            return true;

        return !string.IsNullOrEmpty(scene.path)
            && scene.path.Replace('\\', '/').EndsWith("Scenes/Menu.unity");
    }
}
