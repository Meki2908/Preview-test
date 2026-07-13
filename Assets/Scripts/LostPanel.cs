using UnityEngine;
using UnityEngine.SceneManagement;

public class LostPanel : MonoBehaviour
{
    public void BindToGameManager()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.RegisterGameOverUI(gameObject);
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.Restart();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Menu()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.MainMenu();
        else
            SceneManager.LoadScene(GameSceneIndex.Menu);
    }
}
