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

    // Event để báo cho các class khác biết restart
    public static event System.Action OnRestart;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Sửa lỗi dấu ngoặc: nếu không có ngoặc {} thì chỉ dòng đầu thuộc if
        if (gameOverPanel != null)
        {
            isGameOver = false;
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (!TryUseScenePlayer())
            SpawnPlayer();
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
                PlayerTarget.Register(currentPlayer.transform);
        }
        else
            Debug.LogWarning("PlayerSpawner chưa được gán trong GameManager!");
    }

    public void GameOver(int score)
    {
        if (isGameOver) return;

        isGameOver = true;

        if (themeMusic != null) themeMusic.Stop();
        if (overMusic != null) overMusic.Play();

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
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (overMusic != null) overMusic.Stop();

        isGameOver = false;
        Time.timeScale = 1f;

        PlayerTarget.Clear();

        // Gọi sự kiện reset trước khi load scene
        OnRestart?.Invoke();

        if (currentPlayer != null)
            Destroy(currentPlayer);

        // Load lại scene gameplay → sau khi load xong sẽ tự Spawn lại player ở Start()
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (overMusic != null) overMusic.Stop();

        isGameOver = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene("MenuScene");
    }
    public void SpawnNewPlayer()
    {
        if (currentPlayer != null)
            Destroy(currentPlayer);

        currentPlayer = playerSpawner.SpawnPlayer();
        if (currentPlayer != null)
            PlayerTarget.Register(currentPlayer.transform);

        TopDownCinemachineCamera topDownCamera = FindAnyObjectByType<TopDownCinemachineCamera>();
        if (topDownCamera != null && currentPlayer != null)
            topDownCamera.SetFollowTarget(CameraTargetAnchor.GetOrCreate(currentPlayer.transform));
    }
}
