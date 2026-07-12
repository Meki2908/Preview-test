using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drive animator Normal Zombie.controller: Speed, Attack, Die, Die_Type, Hit.
/// </summary>
[DisallowMultipleComponent]
public class ZombieAnimator : MonoBehaviour
{
    [Header("Locomotion blend (Speed parameter)")]
    [SerializeField] private float walkAnimSpeed = 0.5f;
    [SerializeField] private float runAnimSpeed = 1f;
    [SerializeField] private float stopSpeedEpsilon = 0.05f;
    [SerializeField] private float speedDampTime = 0.12f;

    [Header("Hit reaction")]
    [SerializeField] private float hitLocomotionPause = 0.35f;
    [SerializeField] private float hitCooldown = 0.2f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IdleTypeHash = Animator.StringToHash("Idle_Type");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int ScreamHash = Animator.StringToHash("Scream");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int DieTypeHash = Animator.StringToHash("Die_Type");

    private Animator animator;
    private NavMeshAgent agent;
    private bool locomotionEnabled = true;
    private Vector3 lastPosition;
    private float displayedSpeed;
    private float nextHitTime;
    private Coroutine hitPauseRoutine;

    public void StopHitReaction()
    {
        if (hitPauseRoutine != null)
        {
            StopCoroutine(hitPauseRoutine);
            hitPauseRoutine = null;
        }

        SetLocomotionEnabled(false);
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        agent = GetComponentInParent<NavMeshAgent>();
        lastPosition = transform.position;

        if (animator != null)
            animator.applyRootMotion = false;
    }

    public void ConfigureNavMeshAgent()
    {
        if (agent == null)
            return;

        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.autoBraking = true;
    }

    public void SetLocomotionEnabled(bool enabled)
    {
        locomotionEnabled = enabled;

        if (!enabled && animator != null)
        {
            displayedSpeed = 0f;
            animator.SetFloat(SpeedHash, 0f);
        }
    }

    public void UpdateLocomotion()
    {
        if (!locomotionEnabled || animator == null || agent == null)
            return;

        float targetSpeed = CalculateTargetAnimSpeed();
        animator.SetFloat(SpeedHash, targetSpeed, speedDampTime, Time.deltaTime);
        displayedSpeed = animator.GetFloat(SpeedHash);

        if (displayedSpeed <= stopSpeedEpsilon)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(IdleTypeHash, 0f);
        }
    }

    private float CalculateTargetAnimSpeed()
    {
        if (agent.isStopped)
            return 0f;

        float movementSpeed = MeasureMovementSpeed();
        if (movementSpeed <= stopSpeedEpsilon)
            return 0f;

        float referenceSpeed = Mathf.Max(agent.speed, 0.1f);
        float normalized = Mathf.Clamp01(movementSpeed / referenceSpeed);

        if (normalized < 0.35f)
            return walkAnimSpeed;

        return Mathf.Lerp(walkAnimSpeed, runAnimSpeed, Mathf.InverseLerp(0.35f, 1f, normalized));
    }

    private float MeasureMovementSpeed()
    {
        float velocitySpeed = agent.velocity.magnitude;
        float desiredSpeed = agent.desiredVelocity.magnitude;

        Vector3 flatDelta = transform.position - lastPosition;
        flatDelta.y = 0f;
        float deltaSpeed = Time.deltaTime > 0f ? flatDelta.magnitude / Time.deltaTime : 0f;
        lastPosition = transform.position;

        return Mathf.Max(velocitySpeed, desiredSpeed, deltaSpeed);
    }

    public void PlayAttack()
    {
        if (animator != null)
            animator.SetTrigger(AttackHash);
    }

    public void PlayScream()
    {
        if (animator == null || !HasAnimatorParameter(ScreamHash))
            return;

        SetLocomotionEnabled(false);
        animator.ResetTrigger(ScreamHash);
        animator.SetTrigger(ScreamHash);
    }

    private bool HasAnimatorParameter(int nameHash)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == nameHash)
                return true;
        }

        return false;
    }

    public void PlayHit()
    {
        if (animator == null || Time.time < nextHitTime)
            return;

        Health health = GetComponent<Health>();
        if (health != null && health.IsDead)
            return;

        nextHitTime = Time.time + hitCooldown;

        GetComponent<EnemyBase>()?.ReactToHit();
        ResetAttackTriggers();
        SetLocomotionEnabled(false);

        if (hitPauseRoutine != null)
            StopCoroutine(hitPauseRoutine);

        hitPauseRoutine = StartCoroutine(ResumeLocomotionAfterHit());

        animator.ResetTrigger(HitHash);
        animator.SetTrigger(HitHash);
    }

    private IEnumerator ResumeLocomotionAfterHit()
    {
        yield return new WaitForSeconds(hitLocomotionPause);

        Health health = GetComponent<Health>();
        if (health == null || !health.IsDead)
            SetLocomotionEnabled(true);

        hitPauseRoutine = null;
    }

    public void PlayDeath(int dieType = -1)
    {
        if (animator == null)
            return;

        if (hitPauseRoutine != null)
        {
            StopCoroutine(hitPauseRoutine);
            hitPauseRoutine = null;
        }

        SetLocomotionEnabled(false);

        EnemyBase enemy = GetComponent<EnemyBase>();
        enemy?.FreezeAtDeathPosition();

        ZombieRagdoll ragdoll = GetComponent<ZombieRagdoll>();
        Health health = GetComponent<Health>();
        Vector3? explosionOrigin = health != null && health.HasLastDamageSource
            ? health.LastDamageSource
            : null;

        if (ragdoll != null && (ragdoll.IsRagdollActive || ragdoll.TryEnableRagdoll(explosionOrigin)))
        {
            if (enemy != null)
                enemy.enabled = false;
            return;
        }

        if (dieType < 0)
            dieType = Random.Range(0, 2);

        animator.SetInteger(DieTypeHash, dieType);
        animator.SetTrigger(DieHash);
    }

    public void ResetAttackTriggers()
    {
        if (animator != null)
            animator.ResetTrigger(AttackHash);
    }
}
