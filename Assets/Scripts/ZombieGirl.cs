using UnityEngine;

[RequireComponent(typeof(ZombieAnimator))]
public class ZombieGirl : EnemyBase
{
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4.5f;

    [Header("Melee")]
    [SerializeField] private DamageDealer damageDealer;
    [SerializeField] private float hitboxActiveDelay = 0.35f;
    [SerializeField] private float hitboxActiveDuration = 0.5f;

    private bool isDead;
    private Coroutine attackTimeoutRoutine;
    private Coroutine hitboxRoutine;

    protected override void Start()
    {
        base.Start();

        if (damageDealer == null)
            damageDealer = GetComponent<DamageDealer>();
    }

    protected override void ConfigureChaseDefaults()
    {
        neverLoseAggro = true;
        skipPatrolWhenHunting = true;
    }

    protected override void Attack()
    {
        zombieAnimator?.PlayAttack();
        isAttacking = true;

        if (attackTimeoutRoutine != null)
            StopCoroutine(attackTimeoutRoutine);

        attackTimeoutRoutine = StartCoroutine(AttackTimeout(1.8f));
        RestartHitboxWindow();
    }

    public void EnableHitbox()
    {
        damageDealer?.EnableHitbox();
    }

    public void DisableHitbox()
    {
        damageDealer?.DisableHitbox();
    }

    public void OnAttackEnd()
    {
        CancelAttack();
    }

    protected override void CancelAttack()
    {
        base.CancelAttack();
        zombieAnimator?.ResetAttackTriggers();
        StopHitboxWindow();

        if (attackTimeoutRoutine != null)
        {
            StopCoroutine(attackTimeoutRoutine);
            attackTimeoutRoutine = null;
        }

        if (agent != null)
            agent.isStopped = false;
    }

    protected override void MoveTowardsPlayer()
    {
        base.MoveTowardsPlayer();

        if (agent != null)
        {
            float distance = GetDistanceToPlayer();
            float slowRadius = Mathf.Max(attackRange * 2f, attackRange + 0.1f);
            float speedBlend = Mathf.InverseLerp(attackRange, slowRadius, distance);

            agent.speed = Mathf.Lerp(walkSpeed, chaseSpeed, speedBlend);
            agent.acceleration = walkAcceleration * 1.5f;
        }
    }

    protected override void OnDeath()
    {
        if (isDead)
        {
            ZombieRagdoll ragdoll = GetComponent<ZombieRagdoll>();
            if (ragdoll != null && !ragdoll.IsRagdollActive)
            {
                Debug.LogWarning($"[DEATH] ZombieGirl retry base.OnDeath() trên {name}", this);
                base.OnDeath();
            }
            else
            {
                Debug.LogWarning($"[DEATH] ZombieGirl.OnDeath skip — isDead=true trên {name}", this);
            }

            return;
        }

        Debug.Log($"[DEATH] ZombieGirl.OnDeath() {name}", this);
        isDead = true;
        StopHitboxWindow();
        base.OnDeath();
    }

    private void RestartHitboxWindow()
    {
        StopHitboxWindow();
        hitboxRoutine = StartCoroutine(HitboxWindowRoutine());
    }

    private void StopHitboxWindow()
    {
        if (hitboxRoutine != null)
        {
            StopCoroutine(hitboxRoutine);
            hitboxRoutine = null;
        }

        DisableHitbox();
    }

    private System.Collections.IEnumerator HitboxWindowRoutine()
    {
        DisableHitbox();

        if (hitboxActiveDelay > 0f)
            yield return new WaitForSeconds(hitboxActiveDelay);

        EnableHitbox();

        if (hitboxActiveDuration > 0f)
            yield return new WaitForSeconds(hitboxActiveDuration);

        DisableHitbox();
        hitboxRoutine = null;
    }

    private System.Collections.IEnumerator AttackTimeout(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        OnAttackEnd();
    }
}
