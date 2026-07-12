using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delay = 3f;
    private float countdown;
    private bool hasExploded;

    [Header("Proximity Detonation")]
    [SerializeField] private bool useProximityDetonation = true;
    [SerializeField, Min(0.1f)] private float detectionRadius = 1.5f;
    [SerializeField] private LayerMask enemyLayer = ~0;
    [SerializeField, Min(0f)] private float settleDelayAfterBounce = 0.35f;
    [SerializeField, Min(0.01f)] private float stoppedSpeed = 0.35f;
    [SerializeField, Min(0.02f)] private float proximityCheckInterval = 0.1f;

    [Header("Explosion Stats")]
    [SerializeField] public float radius = 5f;
    [SerializeField] private float force = 500f;
    [SerializeField] private int damage = 50;

    [Header("FX")]
    [SerializeField] private GameObject explosionEffect;

    private readonly Collider[] proximityResults = new Collider[16];
    private Rigidbody body;
    private bool hasBounced;
    private float firstBounceTime;
    private float nextProximityCheckTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

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
        {
            TriggerExplode();
            return;
        }

        CheckProximityDetonation();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasBounced)
        {
            hasBounced = true;
            firstBounceTime = Time.time;
        }

        if (ShouldExplodeOnContact(collision.collider))
            TriggerExplode();
    }

    private bool ShouldExplodeOnContact(Collider col)
    {
        if (hasExploded || col == null)
            return false;

        if (col.CompareTag("Player"))
            return false;

        if (col.CompareTag("Enemy") || col.CompareTag("Headhitbox"))
            return true;

        if (col.GetComponentInParent<EnemyBase>() != null)
            return true;

        return false;
    }

    private void CheckProximityDetonation()
    {
        if (!useProximityDetonation
            || !hasBounced
            || Time.time < firstBounceTime + settleDelayAfterBounce
            || Time.time < nextProximityCheckTime)
        {
            return;
        }

        nextProximityCheckTime = Time.time + proximityCheckInterval;

        if (body != null && body.linearVelocity.sqrMagnitude > stoppedSpeed * stoppedSpeed)
            return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            proximityResults,
            enemyLayer,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider col = proximityResults[i];
            if (col == null)
                continue;

            EnemyBase enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy == null || enemy.IsDeathInProgress || enemy.IsDeathComplete)
                continue;

            Health health = enemy.GetComponent<Health>();
            if (health != null && !health.IsDead)
            {
                TriggerExplode();
                return;
            }
        }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.05f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
