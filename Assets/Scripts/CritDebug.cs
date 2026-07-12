using UnityEngine;

/// <summary>
/// Log debug chase zombie — filter Console bằng "[CRIT][CHASE]".
/// </summary>
public static class CritDebug
{
    public static bool Enabled = true;

    public static void Log(string message, Object context = null)
    {
        if (!Enabled)
            return;

        Debug.Log($"[CRIT][CHASE] {message}", context);
    }

    public static void Warn(string message, Object context = null)
    {
        if (!Enabled)
            return;

        Debug.LogWarning($"[CRIT][CHASE] {message}", context);
    }
}
