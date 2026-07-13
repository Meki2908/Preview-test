using UnityEngine;

public static class CursorController
{
    public static bool HideDuringGameplay { get; private set; } = true;

    public static void SetHideDuringGameplay(bool hide)
    {
        HideDuringGameplay = hide;

        if (!hide)
            ShowCursor();
    }

    /// <summary>Áp dụng trạng thái chuột khi đang chơi (không pause / không game over).</summary>
    public static void ApplyGameplayCursor()
    {
        if (HideDuringGameplay)
            HideCursor();
        else
            ShowCursor();
    }

    public static void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public static void HideCursor()
    {
        if (!HideDuringGameplay)
            return;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
