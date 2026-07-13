using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie spawn → chase Soldier. Dùng Normal Zombie.controller qua ZombieAnimator (Speed / Attack / Hit / Die).
/// </summary>
[RequireComponent(typeof(ZombieAnimator))]
public class Runner : EnemyBase
{
    [Header("Chase")]
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float runAcceleration = 12f;

    [Header("Spawn Alert")]
    [SerializeField] private bool screamOnSpawn = true;
    [SerializeField] private float screamDuration = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip clipScream;
    [SerializeField] private AudioClip clipDie;

    [Header("Melee")]
    [SerializeField] private DamageDealer damageDealer;
    [SerializeField] private float hitboxActiveDelay = 0.35f;
    [SerializeField] private float hitboxActiveDuration = 0.5f;

    private bool isDead;
    private bool isScreaming;
    private Coroutine attackTimeoutRoutine;
    private Coroutine hitboxRoutine;
    private Coroutine screamRoutine;

    protected override void ConfigureChaseDefaults()
    {
        neverLoseAggro = true;
        skipPatrolWhenHunting = true;
    }

    protected override bool UseAnimatorForAgentSpeed() => false;

    protected override void Start()
    {
        base.Start();

        if (damageDealer == null)
            damageDealer = GetComponent<DamageDealer>();

        if (agent != null && NavMesh.SamplePosition(transform.position, out navHit, 50f, NavMesh.AllAreas))
            agent.Warp(navHit.position);

        if (screamOnSpawn)
            screamRoutine = StartCoroutine(SpawnScreamRoutine());
    }

    protected override void Update()
    {
        if (isDead || isScreaming)
            return;

        base.Update();
    }

    protected override void MoveTowardsPlayer()
    {
        base.MoveTowardsPlayer();

        if (agent != null)
        {
            agent.speed = runSpeed;
            agent.acceleration = runAcceleration;
        }
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

    protected override void OnDeath()
    {
        if (isDead)
            return;

        isDead = true;
        StopHitboxWindow();

        if (screamRoutine != null)
        {
            StopCoroutine(screamRoutine);
            screamRoutine = null;
        }

        isScreaming = false;

        if (audioSource != null && clipDie != null)
            audioSource.PlayOneShot(clipDie);

        base.OnDeath();
    }

    private IEnumerator SpawnScreamRoutine()
    {
        isScreaming = true;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        zombieAnimator?.SetLocomotionEnabled(false);
        zombieAnimator?.PlayScream();

        if (audioSource != null && clipScream != null)
            audioSource.PlayOneShot(clipScream);

        yield return new WaitForSeconds(screamDuration);

        isScreaming = false;
        zombieAnimator?.SetLocomotionEnabled(true);

        if (agent != null)
            agent.isStopped = false;
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

    private IEnumerator HitboxWindowRoutine()
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

    private IEnumerator AttackTimeout(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        OnAttackEnd();
    }
}
