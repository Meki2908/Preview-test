using UnityEngine;

/// <summary>
/// Nhận Animation Events melee từ model zombie (EnableHitbox / DisableHitbox / OnAttackEnd).
/// Gắn lên object có Animator (thường là mesh Ch10 / skeleton).
/// </summary>
[DisallowMultipleComponent]
public class EnemyAnimationEvents : MonoBehaviour
{
    public void EnableHitbox()
    {
        Mutants mutant = GetComponentInParent<Mutants>();
        if (mutant != null)
        {
            mutant.EnableHitbox();
            return;
        }

        ZombieGirl zombieGirl = GetComponentInParent<ZombieGirl>();
        if (zombieGirl != null)
        {
            zombieGirl.EnableHitbox();
            return;
        }

        Runner runner = GetComponentInParent<Runner>();
        if (runner != null)
            runner.EnableHitbox();
    }

    public void DisableHitbox()
    {
        Mutants mutant = GetComponentInParent<Mutants>();
        if (mutant != null)
        {
            mutant.DisableHitbox();
            return;
        }

        ZombieGirl zombieGirl = GetComponentInParent<ZombieGirl>();
        if (zombieGirl != null)
        {
            zombieGirl.DisableHitbox();
            return;
        }

        Runner runner = GetComponentInParent<Runner>();
        if (runner != null)
            runner.DisableHitbox();
    }

    public void OnAttackEnd()
    {
        Mutants mutant = GetComponentInParent<Mutants>();
        if (mutant != null)
        {
            mutant.OnAttackEnd();
            return;
        }

        ZombieGirl zombieGirl = GetComponentInParent<ZombieGirl>();
        if (zombieGirl != null)
        {
            zombieGirl.OnAttackEnd();
            return;
        }

        Runner runner = GetComponentInParent<Runner>();
        if (runner != null)
            runner.OnAttackEnd();
    }
}
