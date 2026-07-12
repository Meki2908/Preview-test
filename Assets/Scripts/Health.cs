using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int _maxHealthPoint = 100;

    public UnityEvent OnDead;
    public UnityEvent<float> OnHealthRatioChanged;
    public int MaxHealthPoint => _maxHealthPoint;
    public int HealthPoint => _healthPoint;
    public Vector3 LastDamageSource { get; private set; }
    public bool HasLastDamageSource { get; private set; }

    private int _healthPoint;

    private void Awake()
    {
        _healthPoint = _maxHealthPoint;
        NotifyHealthRatioChanged();
    }

    public bool IsDead => _healthPoint <= 0;

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position + Vector3.up);
    }

    public void TakeDamage(int damage, Vector3 damageSource)
    {
        if (IsDead || damage <= 0)
            return;

        LastDamageSource = damageSource;
        HasLastDamageSource = true;
        _healthPoint = Mathf.Max(0, _healthPoint - damage);
        Debug.Log($"[DAMAGE] {name} nhận {damage} sát thương. HP: {_healthPoint}/{_maxHealthPoint}", this);

        if (!IsDead)
        {
            ZombieAnimator zombieAnimator = GetComponent<ZombieAnimator>();
            if (zombieAnimator != null)
                zombieAnimator.PlayHit();
        }

        if (IsDead)
            Die();
        else
            NotifyHealthRatioChanged();
    }
    public void Heal(int healamount)
    {
        if (IsDead) { return; }
        _healthPoint = Mathf.Min(_healthPoint + healamount, _maxHealthPoint);
        NotifyHealthRatioChanged();
    }

    /// <summary>
    /// Bridge cho Easy Weapons (Projectile / Explosion) gọi qua SendMessage("ChangeHealth").
    /// amount âm = trừ máu, amount dương = hồi máu.
    /// </summary>
    public void ChangeHealth(float amount)
    {
        if (IsDead || Mathf.Approximately(amount, 0f))
            return;

        if (amount < 0f)
            TakeDamage(Mathf.RoundToInt(-amount), transform.position + Vector3.up);
        else
            Heal(Mathf.RoundToInt(amount));
    }

    private bool deathHandled;

    private void Die()
    {
        if (deathHandled)
        {
            Debug.LogWarning($"[DEATH] Die() bị gọi lại trên {name} — bỏ qua.", this);
            return;
        }

        deathHandled = true;
        NotifyHealthRatioChanged();

        Debug.Log(
            $"[DEATH] Die() {name} | HP={_healthPoint}/{_maxHealthPoint} | persistentListeners={OnDead.GetPersistentEventCount()}",
            this);

        OnDead.Invoke();

        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy == null)
            return;

        if (enemy.IsDeathComplete)
            return;

        if (enemy.IsDeathInProgress)
        {
            Debug.LogWarning(
                $"[DEATH] Die() chờ HandleDeath hoàn tất trên {name} — không gọi fallback.",
                this);
            return;
        }

        Debug.LogWarning(
            $"[DEATH] Fallback NotifyDeath() — death chưa hoàn tất trên {name}.",
            this);
        enemy.NotifyDeath();
    }

    private void NotifyHealthRatioChanged()
    {
        if (_maxHealthPoint <= 0)
            return;

        float ratio = Mathf.Clamp01((float)_healthPoint / _maxHealthPoint);
        OnHealthRatioChanged?.Invoke(ratio);
    }
}