using UnityEngine;

/// <summary>
/// Tự gắn DamageDealer + wire hitbox khi spawn (dùng khi prefab chưa setup tay).
/// </summary>
[DisallowMultipleComponent]
public class EnemyMeleeBootstrap : MonoBehaviour
{
    [SerializeField] private int meleeDamage = 12;

    private void Awake()
    {
        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy == null)
            return;

        DamageDealer damageDealer = GetComponent<DamageDealer>();
        if (damageDealer == null)
            damageDealer = gameObject.AddComponent<DamageDealer>();

        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.GetComponent<EnemyAnimationEvents>() == null)
            animator.gameObject.AddComponent<EnemyAnimationEvents>();

        DamageCollider[] colliders = GetComponentsInChildren<DamageCollider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].SetDamage(meleeDamage);
    }
}
