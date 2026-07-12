using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreGameplayUI;

    public int CurrentScore { get; private set; } = 0;

    private void Awake()
    {
        // Singleton để không mất khi đổi scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    private void OnEnable()
    {
        GameManager.OnRestart += ResetScore; // Nghe sự kiện restart
    }

    private void OnDisable()
    {
        GameManager.OnRestart -= ResetScore;
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        UpdateScoreUI();
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreGameplayUI != null)
        {
            scoreGameplayUI.text = "Score: " + CurrentScore;
        }
    }

    public void SaveHighScore()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (CurrentScore > highScore)
        {
            PlayerPrefs.SetInt("HighScore", CurrentScore);
            PlayerPrefs.Save();
        }
    }

    public int GetHighScore()
    {
        return PlayerPrefs.GetInt("HighScore", 0);
    }
}
