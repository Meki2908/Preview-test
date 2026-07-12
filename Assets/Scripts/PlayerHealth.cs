using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float deathAnimationDuration = 3f;
    [SerializeField] private GameObject bloodEffectPrefab;

    public UnityEvent<int, int> OnHealthChanged;

    private int currentHealth;
    private bool isDead;

    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int DieTypeHash = Animator.StringToHash("Die_Type");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int WeaponTypeHash = Animator.StringToHash("WeaponType");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private static readonly string[] DieStateNames =
    {
        "Falling Forward Death",
        "Falling Back Death",
        "Die",
        "Death"
    };

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void OnEnable()
    {
        GameManager.OnRestart += ResetHealth;
    }

    private void OnDisable()
    {
        GameManager.OnRestart -= ResetHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        NotifyHealthChanged();
        SpawnBloodEffect();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int healAmount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        NotifyHealthChanged();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        DisableSoldierGameplay();

        if (TryGetComponent<Animator>(out Animator animator))
            PlayDeathAnimation(animator);

        StartCoroutine(HandleDeathSequence());
    }

    private void DisableSoldierGameplay()
    {
        Transform weaponHolder = transform.Find("WeaponHolder");
        if (weaponHolder != null)
            weaponHolder.gameObject.SetActive(false);

        if (TryGetComponent<SoldierController>(out SoldierController soldier))
        {
            soldier.SetInputEnabled(false);
            soldier.enabled = false;
        }

        if (TryGetComponent<SoldierShooting>(out SoldierShooting shooting))
        {
            shooting.SetDead(true);
            shooting.enabled = false;
        }

        if (TryGetComponent<FootstepController>(out FootstepController footsteps))
            footsteps.enabled = false;

        if (TryGetComponent<CharacterController>(out CharacterController controller))
            controller.enabled = false;
    }

    private void PlayDeathAnimation(Animator animator)
    {
        int dieType = Random.Range(0, 2);
        animator.applyRootMotion = false;
        animator.SetFloat(SpeedHash, 0f);
        animator.SetFloat(MoveXHash, 0f);
        animator.SetFloat(MoveZHash, 0f);
        animator.SetFloat(WeaponTypeHash, 0f);
        animator.ResetTrigger(ShootHash);
        animator.SetInteger(DieTypeHash, dieType);
        animator.ResetTrigger(DieHash);
        animator.SetTrigger(DieHash);

        TryPlayDeathState(animator, dieType);
    }

    private static bool TryPlayDeathState(Animator animator, int dieType)
    {
        int preferredIndex = Mathf.Clamp(dieType, 0, DieStateNames.Length - 1);

        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            for (int offset = 0; offset < DieStateNames.Length; offset++)
            {
                int index = (preferredIndex + offset) % DieStateNames.Length;
                string stateName = DieStateNames[index];
                int stateHash = Animator.StringToHash(stateName);
                if (!animator.HasState(layer, stateHash))
                    continue;

                for (int i = 0; i < animator.layerCount; i++)
                    animator.SetLayerWeight(i, 0f);

                animator.SetLayerWeight(layer, 1f);
                animator.Play(stateHash, layer, 0f);
                animator.Update(0f);
                return true;
            }
        }

        return false;
    }

    private IEnumerator HandleDeathSequence()
    {
        yield return new WaitForSeconds(deathAnimationDuration);

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (GameManager.Instance != null)
            GameManager.Instance.GameOver(finalScore);

        Destroy(gameObject);
    }

    private void SpawnBloodEffect()
    {
        if (bloodEffectPrefab == null)
            return;

        Vector3 spawnPos = transform.position + Vector3.up * 1f;
        GameObject fx = Instantiate(bloodEffectPrefab, spawnPos, Quaternion.identity);
        Destroy(fx, 2f);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        NotifyHealthChanged();
        if (GameManager.Instance != null)
            GameManager.Instance.isGameOver = false;
    }

    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
