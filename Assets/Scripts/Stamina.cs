using UnityEngine;
using UnityEngine.UI;

public class Stamina : MonoBehaviour
{
    private PlayerMovement playerMovement;
    public static Stamina Instance { get; private set; }

    [Header("UI")]
    public Image _staminaBar; // có thể assign sẵn hoặc null

    [Header("Stats")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _regenRate = 10f;
    [SerializeField] private float _regenDelay = 2f;

    private float regenTimer;
    public bool IsEmpty => CurrentStamina <= 0f;
    public float CurrentStamina { get; private set; }

    private void Awake()
    {
        // Singleton an toàn
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentStamina = _maxStamina;
        regenTimer = 0f;
    }
    private void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = PlayerMovement.Instance;
        }
    }
    private void Update()
    {
        // Nếu chưa có reference PlayerMovement → tìm lại
        if (playerMovement == null)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement == null && PlayerMovement.Instance != null)
                playerMovement = PlayerMovement.Instance;
        }

        // Hồi stamina
        if (regenTimer > 0)
            regenTimer -= Time.deltaTime;
        else
            RegenStamina();

        // Giới hạn stamina
        CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, _maxStamina);

        // Update UI
        if (_staminaBar != null)
        {
            _staminaBar.fillAmount = Mathf.Lerp(
                _staminaBar.fillAmount,
                CurrentStamina / _maxStamina,
                10f * Time.deltaTime
            );
        }
    }

    public void ResetStamina()
    {
        CurrentStamina = _maxStamina;
        regenTimer = 0f;

        if (_staminaBar != null)
            _staminaBar.fillAmount = 1f;
    }

    private void RegenStamina()
    {
        if (CurrentStamina < _maxStamina)
            CurrentStamina += _regenRate * Time.deltaTime;
    }

    public void UseStamina(float amount)
    {
        if (CurrentStamina <= 0f) return;

        CurrentStamina -= amount * Time.deltaTime;
        regenTimer = _regenDelay;
    }
}
