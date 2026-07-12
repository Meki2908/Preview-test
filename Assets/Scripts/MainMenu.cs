using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel Settings/Records/Guide trên Menu. Play nên dùng MainMenuController.StartGame (hoặc gọi chung logic bên dưới).
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private MenuSettingsOverlay settingsOverlay;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject recordPanel;
    [SerializeField] private GameObject firstGuidePanel;
    [SerializeField] private GameObject secondGuidePanel;

    private MenuSettingsOverlay Overlay =>
        settingsOverlay != null
            ? settingsOverlay
            : settingsOverlay = GetComponent<MenuSettingsOverlay>()
                ?? FindAnyObjectByType<MenuSettingsOverlay>();

    public void StartGame()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ResetPlayerData();

        CursorController.HideCursor();
        SceneManager.LoadScene(GameSceneIndex.Level1);
    }

    public void OpenSettings()
    {
        if (Overlay != null)
        {
            Overlay.Open();
            return;
        }

        SetPanelActive(settingsPanel, true);
    }

    public void CloseSettings()
    {
        if (Overlay != null && Overlay.IsOpen)
        {
            Overlay.Close();
            return;
        }

        SetPanelActive(settingsPanel, false);
    }

    public void OpenRecordsPanel() => SetPanelActive(recordPanel, true);

    public void CloseRecordsPanel() => SetPanelActive(recordPanel, false);

    public void OpenFirstGuidePanel() => SetPanelActive(firstGuidePanel, true);

    public void CloseFirstGuidePanel() => SetPanelActive(firstGuidePanel, false);

    public void OpenSecondGuidePanel() => SetPanelActive(secondGuidePanel, true);

    public void CloseSecondGuidePanel() => SetPanelActive(secondGuidePanel, false);

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    /// <summary>Gán panel nếu Inspector còn trống (MenuSceneBootstrap / Editor setup).</summary>
    public void TryBindPanels(params GameObject[] candidates)
    {
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Length; i++)
        {
            GameObject candidate = candidates[i];
            if (candidate == null)
                continue;

            string name = candidate.name;
            if (settingsPanel == null && IsName(name, "Sound Settings", "Settings Panel", "SettingsPanel"))
                settingsPanel = candidate;
            else if (recordPanel == null && IsName(name, "Record", "Records", "Record Panel", "Records Panel"))
                recordPanel = candidate;
            else if (firstGuidePanel == null && IsName(name, "Guide Panel", "First Guide", "FirstGuide"))
                firstGuidePanel = candidate;
            else if (secondGuidePanel == null && IsName(name, "Second Guide", "SecondGuide", "Guide 2"))
                secondGuidePanel = candidate;
        }
    }

    public void HideAllPanels()
    {
        CloseSettings();
        SetPanelActive(recordPanel, false);
        SetPanelActive(firstGuidePanel, false);
        SetPanelActive(secondGuidePanel, false);
    }

    private static bool IsName(string actual, params string[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            if (actual == options[i])
                return true;
        }

        return false;
    }
}
