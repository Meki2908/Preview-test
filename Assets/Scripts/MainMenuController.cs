using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên Canvas hoặc [MENU_CONTROLLER]. Wire nút Play/Quit vào StartGame / QuitGame.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public void StartGame()
    {
        Time.timeScale = 1f;
        CursorController.ShowCursor();

        if (GameManager.Instance != null)
            GameManager.Instance.ResetPlayerData();

        CursorController.HideCursor();
        SceneManager.LoadScene(GameSceneIndex.Level1);
    }

    public void QuitGame()
    {
        Debug.Log("Game Quit!");
        Application.Quit();
    }
}
