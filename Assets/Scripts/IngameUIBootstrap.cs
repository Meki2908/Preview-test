using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự nối Pause/Settings UI trong MainGame Canvas prefab theo tên object.
/// </summary>
public sealed class IngameUIBootstrap : MonoBehaviour
{
    private static IngameUIBootstrap instance;
    private Coroutine setupRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("[INGAME_UI_BOOTSTRAP]");
        instance = host.AddComponent<IngameUIBootstrap>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        if (setupRoutine == null)
            setupRoutine = StartCoroutine(SetupWhenReady());
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (setupRoutine != null)
            StopCoroutine(setupRoutine);

        setupRoutine = StartCoroutine(SetupWhenReady());
    }

    private static bool IsMenuScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.buildIndex == GameSceneIndex.Menu || scene.name == "Menu";
    }

    private IEnumerator SetupWhenReady()
    {
        if (IsMenuScene())
            yield break;

        bool wired = false;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (TrySetup())
            {
                wired = true;
                break;
            }

            yield return null;
        }

        if (!wired)
        {
            Debug.LogWarning(
                "IngameUIBootstrap không tìm thấy Canvas có Pause Button và Pause Panel.");
            yield break;
        }

        bool needsLost = MainGameCanvasUI.HasLostPanelInScene();
        bool needsVictory = MainGameCanvasUI.HasVictoryPanelInScene();

        if (!needsLost && !needsVictory)
            yield break;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            if (needsLost)
                MainGameCanvasUI.RetryBindLostPanels();

            if (needsVictory)
                MainGameCanvasUI.RetryBindVictoryPanels();

            bool lostReady = !needsLost
                || (GameManager.Instance != null && GameManager.Instance.HasGameOverPanel);
            bool victoryReady = !needsVictory
                || (GameManager.Instance != null && GameManager.Instance.HasVictoryPanel);

            if (lostReady && victoryReady)
                yield break;

            yield return null;
        }

        for (int attempt = 0; attempt < 15; attempt++)
        {
            LivesUI[] livesLabels = FindObjectsByType<LivesUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < livesLabels.Length; i++)
                livesLabels[i].BindToGameManager();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateLivesUI();
                yield break;
            }

            yield return null;
        }
    }

    private static bool TrySetup()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Transform root = canvases[i].transform;
            if (MainGameCanvasUI.FindChild(root, "Pause Button") == null
                || MainGameCanvasUI.FindChild(root, "Pause Panel") == null)
            {
                continue;
            }

            MainGameCanvasMobileLayout.Apply(root);

            IngameUI ingameUi = GetOrAdd<IngameUI>(root.gameObject);
            MainGameCanvasUI.WireIngameCanvas(root, ingameUi);
            return true;
        }

        return false;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
