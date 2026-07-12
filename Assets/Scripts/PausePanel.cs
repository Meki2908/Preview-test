using UnityEngine;
using UnityEngine.SceneManagement;

public class PausePanel : MonoBehaviour
{
    public void Menu()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.MainMenu();
        else
            SceneManager.LoadScene(GameSceneIndex.Menu);
    }
}
