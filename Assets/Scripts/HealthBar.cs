using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private bool smoothDamageLerp = false;
    [SerializeField] private float lerpSpeed = 12f;

    private bool loggedMissingFill;
    private bool loggedBindFail;
    private float nextBindRetryLogTime;
    private float targetFillAmount = 1f;

    public void SetHealthFill(Image fill)
    {
        healthFill = fill;
        loggedMissingFill = false;
    }

    public static void RefreshAll()
    {
        HealthBar[] bars = FindObjectsByType<HealthBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < bars.Length; i++)
            bars[i].Refresh();
    }

    public void Refresh()
    {
        UnbindPlayerHealth();
        BindPlayerHealth();
    }

    private void Awake()
    {
        if (healthFill == null)
        {
            healthFill = GetComponent<Image>();
            if (healthFill == null)
                healthFill = GetComponentInChildren<Image>(true);
        }

        if (healthFill == null && !loggedMissingFill)
        {
            loggedMissingFill = true;
            Debug.LogWarning(
                "HealthBar thiếu healthFill — kéo Image Fill vào Inspector hoặc dùng HealthUIBootstrap",
                this);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindPlayerHealth();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindPlayerHealth();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == GameSceneIndex.Menu)
            return;

        Refresh();
    }

    private void Update()
    {
        if (playerHealth == null)
            BindPlayerHealth();

        if (!smoothDamageLerp || healthFill == null)
            return;

        healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);
    }

    private void BindPlayerHealth()
    {
        if (playerHealth != null)
            return;

        playerHealth = PlayerHealth.Instance;
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(HandleHealthChanged);
            loggedBindFail = false;
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            return;
        }

        if (Time.time >= nextBindRetryLogTime)
        {
            nextBindRetryLogTime = Time.time + 2f;
            if (!loggedBindFail)
            {
                loggedBindFail = true;
                Debug.LogWarning(
                    "HealthBar CHƯA tìm thấy PlayerHealth — Soldier có PlayerHealth chưa?",
                    this);
            }
        }
    }

    private void UnbindPlayerHealth()
    {
        if (playerHealth == null)
            return;

        playerHealth.OnHealthChanged.RemoveListener(HandleHealthChanged);
        playerHealth = null;
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (healthFill == null || max <= 0)
            return;

        float ratio = (float)current / max;
        targetFillAmount = ratio;

        if (!smoothDamageLerp)
            healthFill.fillAmount = ratio;
    }
}
