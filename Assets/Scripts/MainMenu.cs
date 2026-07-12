using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject recordPanel;
    [SerializeField] private GameObject firstGuidePanel;
    [SerializeField] private GameObject secondGuidePanel;

    public void StartGame()
    {
        SceneManager.LoadScene("GamePlayScene");

        // Reset cursor
        CursorController.HideCursor();

        // Đảm bảo Player được spawn khi vào game
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GamePlayScene" && GameManager.Instance != null)
        {
            GameManager.Instance.SpawnNewPlayer();
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
        }
    }


    public void OpenSettings() => settingsPanel.SetActive(true);
    
    public void CloseSettings() => settingsPanel.SetActive(false);

    public void OpenRecordsPanel() => recordPanel.SetActive(true);
    
    public void CloseRecordsPanel() => recordPanel.SetActive(false);

    public void OpenFirstGuidePanel() => firstGuidePanel.SetActive(true);
    
    public void CloseFirstGuidePanel() => firstGuidePanel.SetActive(false);

    public void OpenSecondGuidePanel() => secondGuidePanel.SetActive(true);
    
    public void CloseSecondGuidePanel() => secondGuidePanel.SetActive(false);

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
