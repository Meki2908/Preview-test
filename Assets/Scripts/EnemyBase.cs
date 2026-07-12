using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private bool useAnimatorMove = false;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float loseAggroMultiplier = 2f;

    [Header("Movement Settings")]
    [SerializeField] protected float walkSpeed = 3f;
    [SerializeField] protected float walkAcceleration = 4f;

    protected Rigidbody rb;
    protected Animator animator;
    protected Transform player;
    protected NavMeshAgent agent;
    protected AudioSource audioSource;

    protected UnityEngine.AI.NavMeshHit navHit;

    [Header("Combat Settings")]
    public float detectionRange = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;

    [Header("Chase / Target")]
    [SerializeField] protected bool neverLoseAggro;
    [SerializeField] protected bool skipPatrolWhenHunting;
    [SerializeField] protected float playerSearchInterval = 0.25f;

    [Header("Patrol Settings")]
    public LayerMask isGround;
    public Vector3 walkPoint;
    public bool walkPointSet = false;
    public float walkPointRange = 5f;
    public bool isWaiting = false;

    private float lastAttackTime = -999f;
    protected bool isAttacking = false;
    protected bool hasAggro;

    private Health health;
    private ZombieRagdoll zombieRagdoll;
    protected ZombieAnimator zombieAnimator;
    protected ZombieVisual zombieVisual;
    private float nextPlayerSearchTime;
    private Coroutine patrolWaitRoutine;

    private Transform critLastTarget;
    private string critLastChaseState = "";
    private float critNextChaseHeartbeat;
    private float critNextMissingTargetLog;
    private bool critLoggedMissingTarget;
    private string critLastNavPathState = "";
    private float critNextNavPathLog;

    public bool IsDeathInProgress { get; private set; }
    public bool IsDeathComplete { get; private set; }

    private void OnEnable()
    {
        PlayerTarget.OnAssigned += HandlePlayerAssigned;
        TryAssignPlayer();
    }

    private void OnDisable()
    {
        PlayerTarget.OnAssigned -= HandlePlayerAssigned;
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        zombieRagdoll = GetComponent<ZombieRagdoll>();
        zombieAnimator = GetComponent<ZombieAnimator>();
        if (zombieAnimator == null)
            zombieAnimator = GetComponentInChildren<ZombieAnimator>(true);

        zombieVisual = GetComponent<ZombieVisual>();
        if (zombieVisual == null)
            zombieVisual = GetComponentInChildren<ZombieVisual>(true);
        if (zombieVisual == null)
            zombieVisual = gameObject.AddComponent<ZombieVisual>();

        if (GetComponent<FootstepController>() == null)
            gameObject.AddComponent<FootstepController>();

        if (GetComponent<EnemyMeleeBootstrap>() == null)
            gameObject.AddComponent<EnemyMeleeBootstrap>();

        if (health != null)
            health.OnDead.AddListener(HandleDeath);
        else
            Debug.LogWarning($"{name} thiếu component Health.", this);

        int ragdollBodyCount = zombieRagdoll != null ? zombieRagdoll.RagdollBodyCount : 0;
        Debug.Log(
            $"[DEATH] Awake {name} | health={(health != null)} | zombieRagdoll={(zombieRagdoll != null)} | ragdollBodies={ragdollBodyCount} | enabled={enabled}",
            this);

        TryAssignPlayer();
    }

    private void HandlePlayerAssigned(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        player = playerTransform;
        CritChaseLog($"{name} target event → {player.name}");
    }

    protected void TryAssignPlayer()
    {
        Transform found = FindPlayerTransform();
        if (found != null)
            player = found;
    }

    protected Transform FindPlayerTransform()
    {
        zombieVisual?.RefreshTarget();
        if (zombieVisual != null && zombieVisual.Target != null)
            return zombieVisual.Target;

        SoldierController soldier = FindAnyObjectByType<SoldierController>();
        if (soldier != null)
            return soldier.transform;

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
            return playerHealth.transform;

        GameObject tagged = GameObject.FindWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    protected virtual void ConfigureChaseDefaults()
    {
    }

    protected bool EnsurePlayerTarget()
    {
        zombieVisual?.RefreshTarget();

        if (zombieVisual != null && zombieVisual.Target != null)
        {
            Transform visualTarget = zombieVisual.Target;
            if (visualTarget.gameObject.activeInHierarchy)
            {
                if (visualTarget != player)
                {
                    player = visualTarget;
                    critLastTarget = visualTarget;
                    critLoggedMissingTarget = false;
                    float dist = Vector3.Distance(transform.position, visualTarget.position);
                    CritChaseLog($"{name} chase dùng ZombieVisual.Target → {visualTarget.name} | dist={dist:F1}m");
                }
                else
                {
                    player = visualTarget;
                }

                return true;
            }

            CritChaseWarn($"{name} ZombieVisual.Target inactive — clear");
            player = null;
            critLastTarget = null;
        }

        if (player != null)
        {
            if (player.gameObject.activeInHierarchy)
                return true;

            CritChaseWarn($"{name} target inactive — clear reference");
            player = null;
            critLastTarget = null;
        }

        if (Time.time < nextPlayerSearchTime)
            return false;

        nextPlayerSearchTime = Time.time + playerSearchInterval;

        Transform found = FindPlayerTransform();
        if (found == null)
        {
            CritChaseWarnMissingTarget();
            return false;
        }

        if (found != critLastTarget)
        {
            float dist = Vector3.Distance(transform.position, found.position);
            CritChaseLog($"{name} target → {found.name} | tag={found.tag} | dist={dist:F1}m");
            critLastTarget = found;
            critLoggedMissingTarget = false;
        }

        player = found;
        return true;
    }

    protected virtual void Start()
    {
        TryAssignPlayer();

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        // Warp agent xuống NavMesh để tránh spawn trên trời
        if (NavMesh.SamplePosition(transform.position, out navHit, 50f, NavMesh.AllAreas))
        {
            agent.Warp(navHit.position);
        }

        ConfigureAgentMovement();
        ConfigureChaseDefaults();

        if (neverLoseAggro)
            hasAggro = true;

        CritChaseLog(
            $"start | neverLoseAggro={neverLoseAggro} | visualTarget={(zombieVisual != null && zombieVisual.Target != null ? zombieVisual.Target.name : "null")} | detection={detectionRange:F1}m | attack={attackRange:F1}m | onNavMesh={(agent != null && agent.isOnNavMesh)}");
    }

    private void ConfigureAgentMovement()
    {
        if (agent == null)
            return;

        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.autoBraking = true;
        agent.stoppingDistance = Mathf.Max(0.05f, attackRange * 0.35f);

        if (animator != null)
            animator.applyRootMotion = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        zombieAnimator?.ConfigureNavMeshAgent();
    }

    protected virtual void Update()
    {
        if (!EnsurePlayerTarget())
            return;

        if (health == null || health.IsDead)
            return;

        float distToPlayer = GetDistanceToPlayer();
        UpdateAggro(distToPlayer);

        if (isAttacking && distToPlayer > attackRange * 1.25f)
            CancelAttack();

        ExecuteMovement(distToPlayer);
        CritLogChase(distToPlayer);
        HandleMovementAnimation();
    }

    private void UpdateAggro(float distToPlayer)
    {
        if (neverLoseAggro)
        {
            hasAggro = true;
            return;
        }

        float loseAggroRange = detectionRange * loseAggroMultiplier;
        if (distToPlayer <= detectionRange)
            hasAggro = true;
        else if (distToPlayer > loseAggroRange)
            hasAggro = false;
    }

    private void ExecuteMovement(float distToPlayer)
    {
        if (distToPlayer <= attackRange)
        {
            HandleAttackRange(distToPlayer);
            return;
        }

        if (hasAggro && !isAttacking)
        {
            StopAllPatrolCoroutines();
            MoveTowardsPlayer();
            return;
        }

        if (!isAttacking && !skipPatrolWhenHunting)
            Patroling();
        else if (skipPatrolWhenHunting && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private bool ShouldLogChase()
    {
        return zombieVisual != null;
    }

    private void CritChaseLog(string message)
    {
        if (!ShouldLogChase())
            return;

        CritDebug.Log(message, this);
    }

    private void CritChaseWarn(string message)
    {
        if (!ShouldLogChase())
            return;

        CritDebug.Warn(message, this);
    }

    private void CritChaseWarnMissingTarget()
    {
        if (!ShouldLogChase())
            return;

        if (critLoggedMissingTarget && Time.time < critNextMissingTargetLog)
            return;

        critLoggedMissingTarget = true;
        critNextMissingTargetLog = Time.time + 2f;
        CritDebug.Warn(
            $"{name} không tìm thấy Soldier | PlayerTarget={(PlayerTarget.Transform != null ? PlayerTarget.Transform.name : "null")}",
            this);
    }

    private void CritLogChase(float distToPlayer)
    {
        if (!ShouldLogChase())
            return;

        string state;
        if (distToPlayer <= attackRange)
            state = isAttacking ? "Attack" : "InAttackRange";
        else if (hasAggro && !isAttacking)
            state = "Chase";
        else if (!isAttacking)
            state = skipPatrolWhenHunting ? "IdleHunt" : "Patrol";
        else
            state = "AttackingFar";

        if (state != critLastChaseState)
        {
            critLastChaseState = state;
            float vel = agent != null ? agent.velocity.magnitude : -1f;
            CritDebug.Log(
                $"{name} state={state} | dist={distToPlayer:F2}m | aggro={hasAggro} | atk={isAttacking} | vel={vel:F2} | stopped={(agent != null && agent.isStopped)}",
                this);
        }

        if (Time.time < critNextChaseHeartbeat)
            return;

        critNextChaseHeartbeat = Time.time + 1.5f;
        float stop = agent != null ? agent.stoppingDistance : -1f;
        float rem = agent != null ? agent.remainingDistance : -1f;
        float heartbeatVel = agent != null ? agent.velocity.magnitude : -1f;
        bool onMesh = agent != null && agent.isOnNavMesh;
        string targetName = player != null ? player.name : "null";
        CritDebug.Log(
            $"[heartbeat] {name} | state={state} | target={targetName} | dist={distToPlayer:F2}m | vel={heartbeatVel:F2} | rem={rem:F2} | onNavMesh={onMesh} | stopped={(agent != null && agent.isStopped)} | stopDist={stop:F2}",
            this);

        CritLogNavMeshChase();
    }

    protected virtual void CancelAttack()
    {
        isAttacking = false;
    }

    public void ReactToHit()
    {
        if (health != null && health.IsDead)
            return;

        CancelAttack();
    }


    #region Patrol
    protected virtual void Patroling()
    {
        if (isWaiting) return;

        agent.speed = walkSpeed;
        agent.acceleration = walkAcceleration;

        if (!walkPointSet) SearchWalkPoint();

        NavMeshHit hit;
        if (walkPointSet && NavMesh.SamplePosition(walkPoint, out hit, 1f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            walkPointSet = false;

        // Nếu đã đến walkPoint
        if (Vector3.Distance(transform.position, walkPoint) < 1f && !isWaiting)
        {
            patrolWaitRoutine = StartCoroutine(WaitBeforeNextMove());
        }
    }

    protected virtual void SearchWalkPoint()
    {
        Vector3 randomPoint = transform.position + new Vector3(
            Random.Range(-walkPointRange, walkPointRange),
            0,
            Random.Range(-walkPointRange, walkPointRange)
        );

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
        {
            walkPoint = hit.position;
            walkPointSet = true;
        }
    }

    private IEnumerator WaitBeforeNextMove()
    {
        isWaiting = true;
        if (agent != null)
            agent.isStopped = true;

        yield return new WaitForSeconds(2f);

        isWaiting = false;
        walkPointSet = false;
        patrolWaitRoutine = null;
        if (agent != null)
            agent.isStopped = false;
    }
    protected void StopAllPatrolCoroutines()
    {
        if (patrolWaitRoutine != null)
        {
            StopCoroutine(patrolWaitRoutine);
            patrolWaitRoutine = null;
        }

        isWaiting = false;
        walkPointSet = false;
    }

    #endregion

    #region Combat
    protected abstract void Attack();

    protected virtual void HandleAttackRange(float distToPlayer)
    {
        StopAllPatrolCoroutines();
        FacePlayerSmooth();
        TryStartAttackIfReady();

        if (zombieAnimator != null)
        {
            MoveTowardsPlayer();
            return;
        }

        bool shouldCloseGap = distToPlayer > attackRange * 0.72f && !isAttacking;
        if (shouldCloseGap)
            MoveTowardsPlayer();
        else if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    protected void TryStartAttackIfReady()
    {
        if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;
        }
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (agent != null && player != null)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.acceleration = walkAcceleration;
            agent.SetDestination(GetChaseDestination());

            CritLogNavMeshChase();
            FacePlayerSmooth();
        }
        else if (agent == null && ShouldLogChase())
        {
            CritChaseWarn($"{name} thiếu NavMeshAgent — không chase được");
        }
    }

    protected float GetDistanceToPlayer()
    {
        if (player == null)
            return float.MaxValue;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null)
            playerCollider = player.GetComponentInChildren<Collider>();

        if (playerCollider != null)
            return Vector3.Distance(transform.position, playerCollider.ClosestPoint(transform.position));

        return Vector3.Distance(transform.position, player.position);
    }

    protected Vector3 GetChaseDestination()
    {
        if (player == null)
            return transform.position;

        Collider playerCollider = player.GetComponent<Collider>();
        if (playerCollider == null)
            playerCollider = player.GetComponentInChildren<Collider>();

        if (playerCollider != null)
            return playerCollider.ClosestPoint(transform.position);

        return player.position;
    }

    private void CritLogNavMeshChase()
    {
        if (agent == null || !ShouldLogChase() || Time.time < critNextNavPathLog)
            return;

        string pathState;
        if (!agent.isOnNavMesh)
            pathState = "OFF_NAVMESH";
        else if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            pathState = "PATH_INVALID";
        else if (agent.pathPending)
            pathState = "PATH_PENDING";
        else if (agent.remainingDistance > 0.1f && agent.velocity.sqrMagnitude < 0.01f)
            pathState = $"STUCK rem={agent.remainingDistance:F1}";
        else
            pathState = $"OK vel={agent.velocity.magnitude:F2} rem={agent.remainingDistance:F1}";

        if (pathState == critLastNavPathState)
            return;

        critLastNavPathState = pathState;
        critNextNavPathLog = Time.time + 1.5f;

        if (pathState.StartsWith("OFF") || pathState.StartsWith("PATH_INVALID") || pathState.StartsWith("STUCK"))
            CritDebug.Warn($"{name} nav={pathState}", this);
        else
            CritDebug.Log($"{name} nav={pathState}", this);
    }

    protected void FacePlayerSmooth()
    {
        if (player == null)
            return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeed * 90f * Time.deltaTime);
    }
    #endregion

    #region Animation
    private void HandleMovementAnimation()
    {
        if (animator == null || agent == null)
            return;

        if (zombieAnimator != null)
        {
            zombieAnimator.UpdateLocomotion();
            return;
        }

        bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
        animator.SetBool("isWalking", isMoving);
    }

    private void OnAnimatorMove()
    {
        if (!UseAnimatorForAgentSpeed() || animator == null || agent == null)
            return;

        if (agent.velocity.sqrMagnitude > 0.01f)
            agent.speed = (animator.deltaPosition / Time.deltaTime).magnitude;
    }

    /// <summary>Override false khi dùng NavMesh + blend Speed thay vì root motion.</summary>
    protected virtual bool UseAnimatorForAgentSpeed() => useAnimatorMove;
    #endregion

    #region Death
    public void NotifyDeath()
    {
        if (IsDeathComplete || IsDeathInProgress)
            return;

        HandleDeath();
    }

    private void HandleDeath()
    {
        if (IsDeathComplete)
            return;

        if (IsDeathInProgress)
        {
            Debug.LogWarning($"[DEATH] HandleDeath skip — đang xử lý trên {name}", this);
            return;
        }

        IsDeathInProgress = true;
        GetComponent<DamageDealer>()?.DisableHitbox();
        Debug.Log($"[DEATH] HandleDeath() {name} | enabled={enabled}", this);

        try
        {
            OnDeath();
            IsDeathComplete = true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[DEATH] OnDeath lỗi trên {name}: {exception}", this);
            IsDeathInProgress = false;
        }
    }

    public void FreezeAtDeathPosition()
    {
        isAttacking = false;
        hasAggro = false;

        zombieAnimator?.StopHitReaction();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();

            if (agent.isOnNavMesh)
                agent.Warp(transform.position);

            agent.enabled = false;
        }

        if (animator != null)
            animator.applyRootMotion = false;
    }

    protected virtual void OnDeath()
    {
        Debug.Log(
            $"[DEATH] OnDeath() {name} | ragdoll={(zombieRagdoll != null)} | setup={zombieRagdoll != null && zombieRagdoll.HasRagdollSetup}",
            this);

        Vector3? explosionOrigin = health != null && health.HasLastDamageSource
            ? health.LastDamageSource
            : null;

        Debug.Log($"[DEATH] Trước TryEnableRagdoll trên {name}", this);

        bool ragdollEnabled = zombieRagdoll != null && zombieRagdoll.TryEnableRagdoll(explosionOrigin);
        Debug.Log($"[DEATH] Sau TryEnableRagdoll result={ragdollEnabled} trên {name}", this);

        if (ragdollEnabled)
        {
            PlayDeathAudioSafe();
            enabled = false;
            return;
        }

        Debug.Log($"[DEATH] FreezeAtDeathPosition() trên {name}", this);
        FreezeAtDeathPosition();

        bool ragdollActive = zombieRagdoll != null && zombieRagdoll.IsRagdollActive;
        Debug.Log($"[DEATH] TryEnableRagdoll result=False | ragdollActive={ragdollActive} on {name}", this);

        if (ragdollActive)
        {
            PlayDeathAudioSafe();
            enabled = false;
            return;
        }

        Debug.Log($"[DEATH] Fallback PlayDeathAnimation() trên {name}", this);
        PlayDeathAnimation();
        PlayDeathAudioSafe();

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        if (rb != null)
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        enabled = false;
        StartCoroutine(DieAndRemove());
    }

    private void PlayDeathAudioSafe()
    {
        if (audioSource == null)
            return;

        try
        {
            if (audioSource.clip != null)
                audioSource.Play();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[DEATH] Audio death bỏ qua trên {name}: {exception.Message}", this);
        }
    }

    protected void PlayDeathAnimation()
    {
        if (animator == null)
            return;

        if (zombieAnimator != null)
        {
            zombieAnimator.PlayDeath();
            return;
        }

        int rand = Random.Range(0, 2);
        animator.SetTrigger(rand == 0 ? "die1" : "die2");
    }

    private IEnumerator DieAndRemove()
    {
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }
    #endregion

    public void TemporarilyDisableNavMesh(float delay)
    {
        if (agent == null) return;

        agent.enabled = false;
        StartCoroutine(ReenableAgentAfterDelay(delay));
    }

    private IEnumerator ReenableAgentAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (agent != null) agent.enabled = true;
    }
}
