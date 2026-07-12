using UnityEngine;

public class IngameUI : MonoBehaviour
{
    public static IngameUI Instance;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    private bool isPaused = false;
    public bool isGameOver = false;
    void Start()
    {
        CursorController.HideCursor();
        Time.timeScale = 1;
        pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);

        if (settingsPanel != null && !isPaused)
            settingsPanel.SetActive(false);

        Time.timeScale = isPaused ? 0 : 1;
        if (isPaused) CursorController.ShowCursor();
        else CursorController.HideCursor();
    }

    public void Continue() => TogglePause();
    public void Settings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }
}
