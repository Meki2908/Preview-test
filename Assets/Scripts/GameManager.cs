using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private SpawnManager playerSpawner;
    [SerializeField] private AudioSource themeMusic;
    [SerializeField] private AudioSource overMusic;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPanel;

    [Header("Lives")]
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int currentLives = 3;

    private TextMeshProUGUI livesText;

    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;

    [Header("Cursor")]
    [Tooltip("Tắt nếu cần dùng chuột với Virtual Joystick (Editor/test). Bật cho cảm giác PC thuần.")]
    [SerializeField] private bool hideCursorDuringGameplay = true;

    public bool HideCursorDuringGameplay => hideCursorDuringGameplay;

    public void SetHideCursorDuringGameplay(bool hide)
    {
        hideCursorDuringGameplay = hide;
        CursorController.SetHideDuringGameplay(hide);
    }

    private GameObject currentPlayer;
    [HideInInspector] public bool isGameOver = false;

    public bool HasGameOverPanel => gameOverPanel != null;
    public bool HasVictoryPanel => victoryPanel != null;

    public static event System.Action OnRestart;

    private void Awake()
    {
        ApplyMobileLandscapeLock();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        CursorController.SetHideDuringGameplay(hideCursorDuringGameplay);

        if (gameOverPanel != null)
        {
            isGameOver = false;
            gameOverPanel.SetActive(false);
        }

        if (victoryPanel != null)
        {
            isGameOver = false;
            victoryPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        SetupPlayerForCurrentScene();
    }

    /// <summary>Gọi từ MainMenu khi bấm Play — reset state trước khi load Level 1.</summary>
    public void ResetPlayerData()
    {
        Time.timeScale = 1f;
        isGameOver = false;

        HideGameOverPanel(clearReference: false);
        HideVictoryPanel(clearReference: false);

        if (overMusic != null)
            overMusic.Stop();

        PlayerTarget.Clear();
        currentLives = maxLives;
        UpdateLivesUI();
        OnRestart?.Invoke();
    }

    public void RegisterLivesUI(TextMeshProUGUI textUI)
    {
        if (textUI == null)
            return;

        livesText = textUI;
        UpdateLivesUI();
    }

    public void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = "x" + currentLives;
    }

    /// <summary>Trừ 1 mạng và cho phép revive. Trả về false nếu hết mạng.</summary>
    public bool TryConsumeLifeForRevive()
    {
        if (currentLives <= 0)
            return false;

        currentLives--;
        UpdateLivesUI();
        return true;
    }

    public bool HasLifeRemaining()
    {
        return currentLives > 0;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMobileLandscapeLock();

        if (scene.buildIndex == GameSceneIndex.Menu)
        {
            PrepareForMenuScene();
            return;
        }

        Time.timeScale = 1f;
        isGameOver = false;

        HideVictoryPanel(clearReference: false);
        CursorController.ApplyGameplayCursor();

        SetupPlayerForCurrentScene();
        HealthBar.RefreshAll();
        UpdateLivesUI();
    }

    /// <summary>Reset UI/state khi về Menu — tránh panel game over / timeScale sót lại.</summary>
    public void PrepareForMenuScene()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        CursorController.ShowCursor();

        if (overMusic != null)
            overMusic.Stop();

        HideGameOverPanel(clearReference: true);
        HideVictoryPanel(clearReference: true);

        if (MenuSettingsOverlay.Instance != null)
            MenuSettingsOverlay.Instance.Close();
    }

    private void HideGameOverPanel(bool clearReference)
    {
        if (gameOverPanel == null)
            return;

        gameOverPanel.SetActive(false);

        if (clearReference)
            gameOverPanel = null;
    }

    private void HideVictoryPanel(bool clearReference)
    {
        if (victoryPanel == null)
            return;

        victoryPanel.SetActive(false);

        if (clearReference)
            victoryPanel = null;
    }

    private void SetupPlayerForCurrentScene()
    {
        if (SceneManager.GetActiveScene().buildIndex == GameSceneIndex.Menu)
            return;

        if (!TryUseScenePlayer())
            SpawnPlayer();

        HealthBar.RefreshAll();
    }

    private bool TryUseScenePlayer()
    {
        SoldierController soldier = FindAnyObjectByType<SoldierController>();
        if (soldier != null)
        {
            UseExistingPlayer(soldier.gameObject);
            return true;
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        if (taggedPlayer != null)
        {
            UseExistingPlayer(taggedPlayer);
            return true;
        }

        return false;
    }

    private void UseExistingPlayer(GameObject player)
    {
        currentPlayer = player;
        PlayerTarget.Register(player.transform);

        TopDownCinemachineCamera topDownCamera = FindAnyObjectByType<TopDownCinemachineCamera>();
        if (topDownCamera != null)
            topDownCamera.SetFollowTarget(CameraTargetAnchor.GetOrCreate(player.transform));
    }

    private void SpawnPlayer()
    {
        if (playerSpawner != null)
        {
            currentPlayer = playerSpawner.SpawnPlayer();
            if (currentPlayer != null)
                UseExistingPlayer(currentPlayer);
        }
        else
            Debug.LogWarning("PlayerSpawner chưa được gán trong GameManager!", this);
    }

    /// <summary>Đăng ký Lost Panel từ MainGame Canvas khi load level (GameManager là DontDestroyOnLoad).</summary>
    public void RegisterGameOverUI(GameObject panel)
    {
        if (panel == null)
            return;

        gameOverPanel = panel;

        if (!isGameOver)
            gameOverPanel.SetActive(false);
    }

    public void RegisterVictoryUI(GameObject panel)
    {
        if (panel == null)
            return;

        victoryPanel = panel;

        if (!isGameOver)
            victoryPanel.SetActive(false);
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        HideVictoryPanel(clearReference: false);

        if (themeMusic != null)
            themeMusic.Stop();

        if (overMusic != null)
            overMusic.Play();

        if (gameOverPanel != null)
        {
            BringOverlayToFront(gameOverPanel);
            gameOverPanel.SetActive(true);
        }

        CursorController.ShowCursor();
        Time.timeScale = 0f;

        Debug.Log("Game Over");
    }

    public void Victory()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        HideGameOverPanel(clearReference: false);

        if (themeMusic != null)
            themeMusic.Stop();

        if (victoryPanel != null)
        {
            BringOverlayToFront(victoryPanel);
            victoryPanel.SetActive(true);
        }

        CursorController.ShowCursor();
        Time.timeScale = 0f;

        Debug.Log("VICTORY TRIGGERED!");
    }

    public void Restart()
    {
        HideGameOverPanel(clearReference: false);
        HideVictoryPanel(clearReference: false);

        if (overMusic != null)
            overMusic.Stop();

        ResetPlayerData();

        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RestartCampaign()
    {
        HideGameOverPanel(clearReference: false);
        HideVictoryPanel(clearReference: false);

        if (overMusic != null)
            overMusic.Stop();

        ResetPlayerData();

        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        SceneManager.LoadScene(GameSceneIndex.Level1);
    }

    public void MainMenu()
    {
        ResetPlayerData();
        SceneManager.LoadScene(GameSceneIndex.Menu);
    }

    public void SpawnNewPlayer()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer);

        if (playerSpawner == null)
        {
            Debug.LogWarning("SpawnNewPlayer: playerSpawner null", this);
            return;
        }

        currentPlayer = playerSpawner.SpawnPlayer();
        if (currentPlayer != null)
            UseExistingPlayer(currentPlayer);
    }

    /// <summary>
    /// Debug: Level 1 → load Level 2. Level 2 → Victory panel (test end game).
    /// </summary>
    public void DebugAdvanceLevel()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Time.timeScale = 1f;

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == GameSceneIndex.Level1)
        {
            SceneManager.LoadScene(GameSceneIndex.Level2);
            return;
        }

        if (sceneIndex == GameSceneIndex.Level2)
            Victory();
#endif
    }

    private static void ApplyMobileLandscapeLock()
    {
#if UNITY_ANDROID || UNITY_IOS
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
    }

    private static void BringOverlayToFront(GameObject panel)
    {
        if (panel == null)
            return;

        panel.transform.SetAsLastSibling();
    }
}
