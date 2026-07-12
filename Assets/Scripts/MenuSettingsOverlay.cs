using UnityEngine;

/// <summary>
/// Spawn MainGame Canvas trên Menu và chỉ hiện Settings Panel.
/// </summary>
public class MenuSettingsOverlay : MonoBehaviour
{
    public static MenuSettingsOverlay Instance { get; private set; }

    [SerializeField] private GameObject mainGameCanvasPrefab;

    private GameObject spawnedCanvas;

    public bool IsOpen => spawnedCanvas != null;

    private void Awake()
    {
        Instance = this;
        EnsurePrefabAssigned();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        Close();
    }

    public void EnsurePrefabAssigned()
    {
        if (mainGameCanvasPrefab != null)
            return;

        mainGameCanvasPrefab = MainGameCanvasUI.LoadCanvasPrefab();
    }

    public void Open()
    {
        if (spawnedCanvas != null)
            return;

        EnsurePrefabAssigned();
        if (mainGameCanvasPrefab == null)
        {
            Debug.LogWarning(
                $"MenuSettingsOverlay: không tìm thấy prefab tại {MainGameCanvasUI.PrefabPath}",
                this);
            return;
        }

        spawnedCanvas = Instantiate(mainGameCanvasPrefab);
        MainGameCanvasUI.ConfigureSettingsOnlyMode(spawnedCanvas.transform, Close);
    }

    public void Close()
    {
        if (spawnedCanvas == null)
            return;

        Destroy(spawnedCanvas);
        spawnedCanvas = null;
    }
}
