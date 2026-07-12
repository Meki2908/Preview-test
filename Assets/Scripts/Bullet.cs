using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private GameObject dmgTextPrefab;
    [SerializeField] private GunData gunData;
    [Header("Bullet stats")]
    [SerializeField] private float lifeTime = 5f;

    [Header("Bullet effects")]
    [SerializeField] private GameObject bloodFX;
    [SerializeField] private GameObject wallFX;
    [SerializeField] private GameObject hitImpact;

    private bool hasHit;

    public void Initialize(GunData data)
    {
        if (data != null)
            gunData = data;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || gunData == null)
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 randomness = new Vector3(Random.Range(0f, 0.25f), Random.Range(0f, 0.25f), Random.Range(0f, 0.25f));

        if (other.CompareTag("Headhitbox"))
        {
            Hitbox hitbox = other.GetComponent<Hitbox>();
            if (hitbox != null)
            {
                hitbox.TakeHit(gunData.damage);
                SpawnBlood(hitPoint);
                DamageDisplay.current?.CreatePopUp(
                    hitPoint + randomness,
                    (gunData.damage * hitbox.damageMultiplier).ToString(),
                    Color.red, 1.5f, 1.5f);
            }

            DestroyBullet();
            return;
        }

        if (other.CompareTag("Enemy") || other.GetComponentInParent<EnemyBase>() != null)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(gunData.damage, transform.position);
                SpawnBlood(hitPoint);
                DamageDisplay.current?.CreatePopUp(hitPoint + randomness, gunData.damage.ToString(), Color.yellow);
            }

            DestroyBullet();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || gunData == null)
            return;

        if (collision.collider.CompareTag("Wall"))
        {
            Vector3 hitPoint = collision.GetContact(0).point;

            if (wallFX != null)
            {
                GameObject fx = Instantiate(wallFX, hitPoint, Quaternion.identity);
                Destroy(fx, 5f);
            }

            if (hitImpact != null)
            {
                GameObject impact = Instantiate(hitImpact, hitPoint, Quaternion.identity);
                Destroy(impact, 5f);
            }

            DestroyBullet();
        }
    }

    private void SpawnBlood(Vector3 hitPoint)
    {
        if (bloodFX != null)
            Instantiate(bloodFX, hitPoint, Quaternion.identity);
    }

    private void DestroyBullet()
    {
        hasHit = true;
        Destroy(gameObject);
    }
}
