using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drive Mutant zombie.controller: Speed (idle vs moving), IsEnraged (walk vs run),
/// Attack / Swipe (upper body).
/// </summary>
[DisallowMultipleComponent]
public class MutantAnimator : MonoBehaviour
{
    [Header("Layers & states")]
    [SerializeField] private int upperBodyLayerIndex = 1;
    [SerializeField] private string punchStateName = "Mutant zombie punch";
    [SerializeField] private string swipeStateName = "Mutant zombie swipe";
    [SerializeField] private float attackEndNormalizedTime = 0.92f;

    [Header("Locomotion (Speed = stopped vs moving)")]
    [SerializeField] private float movingSpeedValue = 1f;
    [SerializeField] private float stopSpeedEpsilon = 0.05f;
    [SerializeField] private float speedDampTime = 0.12f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IdleTypeHash = Animator.StringToHash("Idle_Type");
    private static readonly int IsEnragedHash = Animator.StringToHash("IsEnraged");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SwipeHash = Animator.StringToHash("Swipe");

    private Animator animator;
    private NavMeshAgent agent;
    private bool locomotionEnabled = true;
    private Vector3 lastPosition;

    public bool IsSwiping { get; private set; }
    public bool IsPunching { get; private set; }
    public bool IsInAttackAnimation => IsSwiping || IsPunching;
    public bool IsEnraged { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
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

    public void SetEnraged(bool enraged)
    {
        IsEnraged = enraged;

        if (animator == null)
            return;

        animator.SetBool(IsEnragedHash, enraged);
    }

    public void SetLocomotionEnabled(bool enabled)
    {
        locomotionEnabled = enabled;

        if (!enabled && animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(IdleTypeHash, 0f);
        }
    }

    public void RefreshAttackState()
    {
        IsSwiping = false;
        IsPunching = false;

        if (animator == null)
            return;

        AnimatorStateInfo upperBody = animator.GetCurrentAnimatorStateInfo(upperBodyLayerIndex);

        if (upperBody.IsName(swipeStateName) && upperBody.normalizedTime < attackEndNormalizedTime)
            IsSwiping = true;
        else if (upperBody.IsName(punchStateName) && upperBody.normalizedTime < attackEndNormalizedTime)
            IsPunching = true;
    }

    public bool TryPlayPunch()
    {
        if (animator == null || IsSwiping)
            return false;

        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
        return true;
    }

    public void PlaySwipe()
    {
        if (animator == null || IsSwiping)
            return;

        animator.ResetTrigger(SwipeHash);
        animator.SetTrigger(SwipeHash);
    }

    public void ResetAttackTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(SwipeHash);
    }

    public void UpdateLocomotion()
    {
        RefreshAttackState();

        if (!locomotionEnabled || animator == null || agent == null)
            return;

        if (IsSwiping)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetFloat(IdleTypeHash, 0f);
            return;
        }

        // Punch trên upper body: chân base layer vẫn dùng Speed (idle vs moving).
        float targetSpeed = CalculateTargetAnimSpeed();
        animator.SetFloat(SpeedHash, targetSpeed, speedDampTime, Time.deltaTime);

        if (animator.GetFloat(SpeedHash) <= stopSpeedEpsilon)
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

        return movingSpeedValue;
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
}
