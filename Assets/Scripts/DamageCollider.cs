using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger melee damage lên Player. Gắn trên collider hitbox (vd. xương tay).
/// DamageDealer có thể tự thêm component này nếu bật Auto Add Damage Colliders.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class DamageCollider : MonoBehaviour
{
    [SerializeField] private int damage = 12;
    [SerializeField] private float hitCooldown = 0.65f;
    [SerializeField] private string playerTag = "Player";

    private readonly Dictionary<int, float> nextHitTimeByTarget = new Dictionary<int, float>();
    private Collider hitCollider;
    private Health ownerHealth;
    private DamageDealer ownerDealer;

    public int Damage => damage;

    private void Awake()
    {
        hitCollider = GetComponent<Collider>();
        ownerHealth = GetComponentInParent<Health>();
        ownerDealer = GetComponentInParent<DamageDealer>();

        if (hitCollider != null)
            hitCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (!enabled || hitCollider == null || !hitCollider.enabled)
            return;

        if (ownerHealth != null && ownerHealth.IsDead)
            return;

        if (!other.CompareTag(playerTag))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        int targetId = playerHealth.GetInstanceID();
        if (nextHitTimeByTarget.TryGetValue(targetId, out float nextHitTime) && Time.time < nextHitTime)
            return;

        nextHitTimeByTarget[targetId] = Time.time + hitCooldown;
        playerHealth.TakeDamage(damage);
    }

    public void SetDamage(int value)
    {
        damage = Mathf.Max(1, value);
    }
}
