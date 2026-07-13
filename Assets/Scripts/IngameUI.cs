using UnityEngine;

public class IngameUI : MonoBehaviour
{
    public static IngameUI Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CursorController.ApplyGameplayCursor();
        Time.timeScale = 1f;
        SetPanelsActive(false, false);
    }

    private void Update()
    {
        bool isGameOver = GameManager.Instance != null && GameManager.Instance.isGameOver;
        if (isGameOver && isPaused)
        {
            isPaused = false;
            SetPanelsActive(false, false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && !isGameOver)
            TogglePause();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(GameObject pause, GameObject settings)
    {
        pausePanel = pause;
        settingsPanel = settings;
        SetPanelsActive(false, false);
    }

    public void TogglePause()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            return;

        isPaused = !isPaused;
        SetPanelsActive(isPaused, false);

        Time.timeScale = isPaused ? 0 : 1;
        if (isPaused)
            CursorController.ShowCursor();
        else
            CursorController.ApplyGameplayCursor();
    }

    public void Continue()
    {
        if (isPaused)
            TogglePause();
    }

    public void Settings()
    {
        if (isPaused && settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void SetPanelsActive(bool showPause, bool showSettings)
    {
        if (pausePanel != null)
            pausePanel.SetActive(showPause);

        if (settingsPanel != null)
            settingsPanel.SetActive(showSettings);
    }
}
