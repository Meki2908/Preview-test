using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MutantAnimator), typeof(Health))]
public class Mutants : EnemyBase
{
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;

    [Header("Punch")]
    [SerializeField] private float punchCooldown = 0.85f;
    [SerializeField] private float punchTimeout = 1.2f;

    [Header("Swipe")]
    [SerializeField] private float swipeCooldown = 2.4f;
    [SerializeField] private float swipeTimeout = 2.4f;
    [SerializeField] private float movingSpeedThreshold = 0.25f;

    [Header("Melee hitbox (Animation Events)")]
    [SerializeField] private DamageDealer damageDealer;
    [SerializeField] private float punchHitboxDelay = 0.2f;
    [SerializeField] private float punchHitboxDuration = 0.35f;
    [SerializeField] private float swipeHitboxDelay = 0.45f;
    [SerializeField] private float swipeHitboxDuration = 0.4f;

    private MutantAnimator mutantAnimator;
    private Health health;
    private bool isDead;
    private bool isSwipeAttack;
    private float lastPunchTime = -999f;
    private float lastSwipeTime = -999f;
    private float nextPlayerSearchTime;
    private Coroutine attackTimeoutRoutine;
    private Coroutine hitboxRoutine;

    protected override bool UseAnimatorForAgentSpeed() => false;

    protected override void Start()
    {
        base.Start();
        mutantAnimator = GetComponent<MutantAnimator>();
        health = GetComponent<Health>();
        mutantAnimator.ConfigureNavMeshAgent();

        if (damageDealer == null)
            damageDealer = GetComponent<DamageDealer>();

        damageDealer?.DisableHitbox();
    }

    protected override void Update()
    {
        if (player == null)
        {
            if (Time.time >= nextPlayerSearchTime)
            {
                TryAssignPlayer();
                nextPlayerSearchTime = Time.time + 0.5f;
            }

            if (player == null)
                return;
        }

        if (health == null || health.IsDead)
            return;

        mutantAnimator.RefreshAttackState();

        float distToPlayer = GetDistanceToPlayer();
        float loseAggroRange = detectionRange * 2f;

        if (distToPlayer <= detectionRange)
            hasAggro = true;
        else if (distToPlayer > loseAggroRange)
            hasAggro = false;

        if (isAttacking && distToPlayer > attackRange * 1.25f)
            CancelAttack();

        if (distToPlayer <= attackRange)
            HandleAttackRange();
        else if (hasAggro && !IsBusyWithSwipe())
            ChasePlayer();
        else if (!IsBusyWithSwipe() && !isAttacking)
            Patroling();

        mutantAnimator.UpdateLocomotion();
    }

    private bool IsBusyWithSwipe()
    {
        return mutantAnimator.IsSwiping || (isAttacking && isSwipeAttack);
    }

    private void HandleAttackRange()
    {
        StopAllPatrolCoroutines();
        FacePlayerSmooth();

        if (IsBusyWithSwipe())
        {
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            return;
        }

        if (!isAttacking)
            TryStartMeleeAttack();

        if (isAttacking && !isSwipeAttack)
            ChasePlayer();
        else if (!isAttacking)
            ChasePlayer();
    }

    private void TryStartMeleeAttack()
    {
        if (mutantAnimator.IsSwiping)
            return;

        bool canPunch = Time.time - lastPunchTime >= punchCooldown;
        bool canSwipe = Time.time - lastSwipeTime >= swipeCooldown;

        if (!canPunch && !canSwipe)
            return;

        bool isMoving = agent != null && agent.velocity.magnitude >= movingSpeedThreshold;

        // Đang di chuyển → punch (upper body). Đứng yên → swipe (upper body, chân dừng).
        if (isMoving)
        {
            if (canPunch)
                StartPunchAttack();
            else if (canSwipe)
                StartSwipeAttack();
        }
        else
        {
            if (canSwipe)
                StartSwipeAttack();
            else if (canPunch)
                StartPunchAttack();
        }
    }

    private void StartPunchAttack()
    {
        if (mutantAnimator.IsSwiping || !mutantAnimator.TryPlayPunch())
            return;

        isAttacking = true;
        isSwipeAttack = false;
        lastPunchTime = Time.time;

        if (agent != null)
            agent.isStopped = false;

        RestartAttackTimeout(punchTimeout);
        RestartMeleeHitboxWindow(punchHitboxDelay, punchHitboxDuration);
    }

    private void StartSwipeAttack()
    {
        if (mutantAnimator.IsSwiping)
            return;

        mutantAnimator.PlaySwipe();
        isAttacking = true;
        isSwipeAttack = true;
        lastSwipeTime = Time.time;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        RestartAttackTimeout(swipeTimeout);
        RestartMeleeHitboxWindow(swipeHitboxDelay, swipeHitboxDuration);
    }

    private void ChasePlayer()
    {
        MoveTowardsPlayer();
    }

    protected override void MoveTowardsPlayer()
    {
        base.MoveTowardsPlayer();

        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.acceleration = walkAcceleration * 1.5f;
        }
    }

    protected override void Patroling()
    {
        base.Patroling();

        if (agent != null && !agent.isStopped)
        {
            agent.speed = walkSpeed;
            agent.acceleration = walkAcceleration;
        }
    }

    protected override void Attack()
    {
        // Combat được điều khiển trong Update — không dùng flow Attack() mặc định của EnemyBase.
    }

    protected override void CancelAttack()
    {
        base.CancelAttack();
        isSwipeAttack = false;
        mutantAnimator?.ResetAttackTriggers();
        ClearAttackTimeout();
        damageDealer?.DisableHitbox();
        StopMeleeHitboxWindow();

        if (agent != null)
            agent.isStopped = false;
    }

    public void OnAttackEnd()
    {
        CancelAttack();
    }

    public void EnableHitbox()
    {
        damageDealer?.EnableHitbox();
    }

    public void DisableHitbox()
    {
        damageDealer?.DisableHitbox();
    }

    private void RestartMeleeHitboxWindow(float delay, float duration)
    {
        if (hitboxRoutine != null)
        {
            StopCoroutine(hitboxRoutine);
            hitboxRoutine = null;
        }

        hitboxRoutine = StartCoroutine(MeleeHitboxWindowRoutine(delay, duration));
    }

    private void StopMeleeHitboxWindow()
    {
        if (hitboxRoutine != null)
        {
            StopCoroutine(hitboxRoutine);
            hitboxRoutine = null;
        }

        DisableHitbox();
    }

    private IEnumerator MeleeHitboxWindowRoutine(float delay, float duration)
    {
        DisableHitbox();

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        EnableHitbox();

        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        DisableHitbox();
        hitboxRoutine = null;
    }

    protected override void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;
        damageDealer?.DisableHitbox();
        mutantAnimator?.SetLocomotionEnabled(false);
        mutantAnimator?.ResetAttackTriggers();
        ScoreManager.Instance?.AddScore(500);
        base.OnDeath();
    }

    private void RestartAttackTimeout(float seconds)
    {
        ClearAttackTimeout();
        attackTimeoutRoutine = StartCoroutine(AttackTimeoutRoutine(seconds));
    }

    private void ClearAttackTimeout()
    {
        if (attackTimeoutRoutine != null)
        {
            StopCoroutine(attackTimeoutRoutine);
            attackTimeoutRoutine = null;
        }
    }

    private IEnumerator AttackTimeoutRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        OnAttackEnd();
    }
}
