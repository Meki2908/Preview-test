using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MutantAnimator), typeof(Health))]
public class Mutants : EnemyBase
{
    [Header("Boss Phases")]
    [SerializeField] private float phase1Speed = 2f;
    [SerializeField] private float phase2Speed = 6f;

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

    [Header("AoE Heavy Attack")]
    [SerializeField] private bool aoeEnabled = true;
    [SerializeField] private GameObject aoeTelegraphPrefab;
    [SerializeField] private float aoeRadius = 5f;
    [SerializeField] private float aoeChargeTime = 1.5f;
    [SerializeField] private float aoeCooldown = 6f;
    [SerializeField] private float aoeForwardOffset = 1.5f;
    [Tooltip("Chỉ bật AoE khi máu boss dưới ngưỡng này (0..1). Đặt 1 để luôn dùng.")]
    [SerializeField, Range(0f, 1f)] private float aoeHealthThreshold = 0.5f;

    [Header("Enrage (Phase 2)")]
    [SerializeField] private bool enrageEnabled = true;
    [SerializeField, Range(0f, 1f)] private float enrageHealthThreshold = 0.5f;
    [SerializeField] private float enragedPunchCooldownMultiplier = 0.7f;
    [SerializeField] private float enragedSwipeCooldownMultiplier = 0.7f;

    private MutantAnimator mutantAnimator;
    private Health health;
    private bool isDead;
    private bool isSwipeAttack;
    private float lastPunchTime = -999f;
    private float lastSwipeTime = -999f;
    private float nextPlayerSearchTime;
    private Coroutine attackTimeoutRoutine;
    private Coroutine hitboxRoutine;
    private Coroutine aoeRoutine;
    private float lastAoETime = -999f;
    private bool isAoEActive;
    private bool enraged;
    private float chaseSpeed;
    private float basePunchCooldown;
    private float baseSwipeCooldown;

    protected override bool UseAnimatorForAgentSpeed() => false;

    protected override void ConfigureChaseDefaults()
    {
        neverLoseAggro = true;
        skipPatrolWhenHunting = true;
    }

    protected override void Start()
    {
        base.Start();
        mutantAnimator = GetComponent<MutantAnimator>();
        health = GetComponent<Health>();
        mutantAnimator.ConfigureNavMeshAgent();

        if (damageDealer == null)
            damageDealer = GetComponent<DamageDealer>();

        damageDealer?.DisableHitbox();

        chaseSpeed = phase1Speed;
        basePunchCooldown = punchCooldown;
        baseSwipeCooldown = swipeCooldown;
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

        UpdateEnrageState();
        ApplyPhaseChaseSpeed();

        mutantAnimator.RefreshAttackState();

        // Đang gồng chiêu AoE → đứng yên, không xử lý hành vi thường.
        if (isAoEActive)
        {
            FacePlayerSmooth();
            mutantAnimator.UpdateLocomotion();
            return;
        }

        hasAggro = true;

        float distToPlayer = GetDistanceToPlayer();

        if (isAttacking && distToPlayer > attackRange * 1.25f)
            CancelAttack();

        if (TryTriggerHeavyAoE(distToPlayer))
            return;

        if (distToPlayer <= attackRange)
            HandleAttackRange();
        else if (!IsBusyWithSwipe())
            ChasePlayer();

        mutantAnimator.UpdateLocomotion();
    }

    private void ApplyPhaseChaseSpeed()
    {
        if (!enraged)
            chaseSpeed = phase1Speed;
    }

    private float HealthRatio =>
        health != null && health.MaxHealthPoint > 0
            ? (float)health.HealthPoint / health.MaxHealthPoint
            : 1f;

    private void UpdateEnrageState()
    {
        if (!enrageEnabled || enraged)
            return;

        if (HealthRatio > enrageHealthThreshold)
            return;

        enraged = true;
        chaseSpeed = phase2Speed;
        punchCooldown = basePunchCooldown * enragedPunchCooldownMultiplier;
        swipeCooldown = baseSwipeCooldown * enragedSwipeCooldownMultiplier;
        mutantAnimator.SetEnraged(true);

        Debug.Log("[Mutants] BOSS ENRAGED — Phase 2!", this);
    }

    private bool TryTriggerHeavyAoE(float distToPlayer)
    {
        if (!aoeEnabled || isAttacking || IsBusyWithSwipe())
            return false;

        if (HealthRatio > aoeHealthThreshold)
            return false;

        if (Time.time - lastAoETime < aoeCooldown)
            return false;

        if (distToPlayer > aoeRadius)
            return false;

        TriggerHeavyAoEAttack();
        return true;
    }

    public void TriggerHeavyAoEAttack()
    {
        if (isAoEActive || isAttacking)
            return;

        if (aoeRoutine != null)
            StopCoroutine(aoeRoutine);

        aoeRoutine = StartCoroutine(HeavyAoERoutine());
    }

    private IEnumerator HeavyAoERoutine()
    {
        isAoEActive = true;
        isAttacking = true;
        lastAoETime = Time.time;

        StopAllPatrolCoroutines();
        FacePlayerSmooth();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SpawnAoETelegraph();

        yield return new WaitForSeconds(aoeChargeTime);

        if (!isDead)
            mutantAnimator?.PlaySwipe();

        yield return new WaitForSeconds(swipeTimeout);

        isAoEActive = false;
        isAttacking = false;
        aoeRoutine = null;

        if (agent != null && !isDead)
            agent.isStopped = false;
    }

    private void SpawnAoETelegraph()
    {
        if (aoeTelegraphPrefab == null)
        {
            Debug.LogError("Mutants: aoeTelegraphPrefab is not assigned. Run Tools/Combat/Create AoE Indicator Prefab.", this);
            return;
        }

        Vector3 spawnPos = transform.position + transform.forward * aoeForwardOffset;
        GameObject aoeObject = Instantiate(aoeTelegraphPrefab, spawnPos, Quaternion.identity);

        AoETelegraph telegraph = aoeObject.GetComponent<AoETelegraph>();
        if (telegraph == null)
        {
            Debug.LogError("Mutants: AoE Indicator prefab is missing AoETelegraph component.", this);
            Destroy(aoeObject);
            return;
        }

        telegraph.StartTelegraph(aoeChargeTime, aoeRadius);
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
        isAoEActive = false;

        if (aoeRoutine != null)
        {
            StopCoroutine(aoeRoutine);
            aoeRoutine = null;
        }

        damageDealer?.DisableHitbox();
        mutantAnimator?.SetLocomotionEnabled(false);
        mutantAnimator?.ResetAttackTriggers();
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (aoeTelegraphPrefab == null)
        {
            aoeTelegraphPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/AoE Indicator.prefab");
        }
    }
#endif
}
