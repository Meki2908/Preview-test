using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đảm bảo Canvas có HealthUIBootstrap khi vào Play / mỗi lần load level.
/// </summary>
public class SceneSetupValidator : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Object.FindAnyObjectByType<SceneSetupValidator>() != null)
            return;

        GameObject host = new GameObject(nameof(SceneSetupValidator));
        host.AddComponent<SceneSetupValidator>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureCanvasHealthUi();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == GameSceneIndex.Menu)
            return;

        EnsureCanvasHealthUi();
        HealthBar.RefreshAll();
    }

    private void EnsureCanvasHealthUi()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas.GetComponent<HealthUIBootstrap>() == null)
                canvas.gameObject.AddComponent<HealthUIBootstrap>();
            else
                canvas.GetComponent<HealthUIBootstrap>().EnsureHealthBar();
        }
    }
}
