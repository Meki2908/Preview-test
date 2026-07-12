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
    [SerializeField] private TextMeshProUGUI scoreText;

    private GameObject currentPlayer;
    [HideInInspector] public bool isGameOver = false;

    public bool HasGameOverPanel => gameOverPanel != null;

    public static event System.Action OnRestart;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (gameOverPanel != null)
        {
            isGameOver = false;
            gameOverPanel.SetActive(false);
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

    /// <summary>Gọi từ MainMenu khi bấm Play — reset score/health state trước khi load Level 1.</summary>
    public void ResetPlayerData()
    {
        Time.timeScale = 1f;
        isGameOver = false;

        HideGameOverPanel(clearReference: false);

        if (overMusic != null)
            overMusic.Stop();

        PlayerTarget.Clear();
        OnRestart?.Invoke();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == GameSceneIndex.Menu)
        {
            PrepareForMenuScene();
            return;
        }

        Time.timeScale = 1f;
        isGameOver = false;

        SetupPlayerForCurrentScene();
        HealthBar.RefreshAll();
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
    public void RegisterGameOverUI(GameObject panel, TextMeshProUGUI scoreLabel = null)
    {
        if (panel == null)
            return;

        gameOverPanel = panel;

        if (scoreLabel != null)
            scoreText = scoreLabel;

        if (!isGameOver)
            gameOverPanel.SetActive(false);
    }

    public void GameOver(int score)
    {
        if (isGameOver)
            return;

        isGameOver = true;

        if (themeMusic != null)
            themeMusic.Stop();

        if (overMusic != null)
            overMusic.Play();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (scoreText != null)
            scoreText.text = "Final Score: " + score;

        CursorController.ShowCursor();
        Time.timeScale = 0f;

        Debug.Log("Game Over");
    }

    public void Restart()
    {
        HideGameOverPanel(clearReference: false);

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
}
