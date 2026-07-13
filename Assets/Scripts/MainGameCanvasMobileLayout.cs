using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Landscape mobile: chỉ cấu hình CanvasScaler Match Height.
/// HUD hierarchy (SafeAreaContainer) được setup một lần trên prefab qua Editor menu.
/// </summary>
public static class MainGameCanvasMobileLayout
{
    public const string ContainerName = "SafeAreaContainer";

    public static bool Apply(Transform canvasRoot)
    {
        if (canvasRoot == null)
            return false;

        return ConfigureCanvasScaler(canvasRoot);
    }

    private static bool ConfigureCanvasScaler(Transform canvasRoot)
    {
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        if (scaler == null)
            return false;

        bool changed = false;
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            changed = true;
        }

        if (scaler.referenceResolution != new Vector2(1920f, 1080f))
        {
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            changed = true;
        }

        if (!Mathf.Approximately(scaler.matchWidthOrHeight, 1f))
        {
            scaler.matchWidthOrHeight = 1f;
            changed = true;
        }

        return changed;
    }
}
