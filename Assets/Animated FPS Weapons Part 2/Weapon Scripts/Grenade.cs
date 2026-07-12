using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delay = 3f;
    private float countdown;
    private bool hasExploded;

    [Header("Explosion Stats")]
    [SerializeField] public float radius = 5f;
    [SerializeField] private float force = 500f;
    [SerializeField] private int damage = 50;

    [Header("FX")]
    [SerializeField] private GameObject explosionEffect;

    private void Start()
    {
        countdown = delay;
    }

    private void Update()
    {
        if (hasExploded)
            return;

        countdown -= Time.deltaTime;
        if (countdown <= 0f)
            TriggerExplode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ShouldExplodeOnContact(collision.collider))
            TriggerExplode();
    }

    private bool ShouldExplodeOnContact(Collider col)
    {
        if (hasExploded || col == null)
            return false;

        if (col.CompareTag("Player"))
            return false;

        if (col.CompareTag("Enemy") || col.CompareTag("Wall") || col.CompareTag("Ground") || col.CompareTag("Headhitbox"))
            return true;

        if (col.GetComponentInParent<EnemyBase>() != null)
            return true;

        // Tường / sàn không gắn tag: collider tĩnh (không trigger)
        if (!col.isTrigger && col.attachedRigidbody == null)
            return true;

        return false;
    }

    private void TriggerExplode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        Explode();
    }

    private void Explode()
    {
        Vector3 explosionPosition = transform.position;

        if (explosionEffect != null)
            Instantiate(explosionEffect, explosionPosition, transform.rotation);

        Collider[] affectedObjects = Physics.OverlapSphere(
            explosionPosition,
            radius,
            ~0,
            QueryTriggerInteraction.Collide);

        Debug.Log($"[GRENADE] Nổ tại {explosionPosition}, overlap {affectedObjects.Length} collider.", this);

        HashSet<Health> damagedTargets = new HashSet<Health>();

        foreach (Collider col in affectedObjects)
        {
            Health health = col.GetComponentInParent<Health>();
            if (health != null && !health.IsDead && damagedTargets.Add(health))
            {
                health.TakeDamage(damage, explosionPosition);
                Debug.Log($"[GRENADE] Gây {damage} dmg cho {health.name}. HP: {health.HealthPoint}/{health.MaxHealthPoint}", health);
            }

            DestructibleObject destructible = col.GetComponent<DestructibleObject>();
            if (destructible != null)
                destructible.Destroy();

            Rigidbody rb = col.attachedRigidbody;
            if (rb != null)
                rb.AddExplosionForce(force, explosionPosition, radius);
        }

        Destroy(gameObject);
    }
}
