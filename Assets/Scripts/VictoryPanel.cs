using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryPanel : MonoBehaviour
{
    public void BindToGameManager()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.RegisterVictoryUI(gameObject);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.RestartCampaign();
        else
            SceneManager.LoadScene(GameSceneIndex.Level1);
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
