using UnityEngine;

/// <summary>
/// Ẩn nút cheat win trên bản release (chỉ Editor / Development Build).
/// </summary>
public class DebugWinButtonVisibility : MonoBehaviour
{
    private void Awake()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        gameObject.SetActive(false);
#endif
    }
}
