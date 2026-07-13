using System.Collections;
using UnityEngine;

/// <summary>
/// Ground AoE warning: inner ring expands to outer radius, then deals damage.
/// Uses collider-free mesh visuals from the AoE Indicator prefab.
/// </summary>
public class AoETelegraph : MonoBehaviour
{
    [Header("Ring Visuals (assign from prefab)")]
    [SerializeField] private Transform outerRing;
    [SerializeField] private Transform innerRing;

    [Header("Damage")]
    [SerializeField] private int damage = 30;

    [Header("Ground Snap")]
    [SerializeField] private float groundRayHeight = 5f;
    [SerializeField] private float groundYOffset = 0.05f;
    [SerializeField] private LayerMask groundMask = ~0;

    private float chargeDuration;
    private float targetRadius;
    private bool isActive;

    public void StartTelegraph(float duration, float radius)
    {
        if (!EnsureRings())
        {
            Debug.LogError("AoETelegraph: missing ring visuals. Assign AoE Indicator prefab.", this);
            Destroy(gameObject);
            return;
        }

        chargeDuration = Mathf.Max(0.05f, duration);
        targetRadius = Mathf.Max(0.1f, radius);
        isActive = true;

        SnapToGround();
        SetupRingScales();

        StopAllCoroutines();
        StartCoroutine(TelegraphRoutine());
    }

    private bool EnsureRings()
    {
        if (outerRing == null)
            outerRing = transform.Find("Outer Ring");

        if (innerRing == null)
            innerRing = transform.Find("Inner Ring");

        return outerRing != null && innerRing != null;
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 pos = hit.point;
            pos.y += groundYOffset;
            transform.position = pos;
        }
    }

    private void SetupRingScales()
    {
        float diameter = targetRadius * 2f;
        Vector3 outerScale = new Vector3(diameter, 0.02f, diameter);
        outerRing.localScale = outerScale;
        innerRing.localScale = Vector3.zero;
    }

    private IEnumerator TelegraphRoutine()
    {
        float elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / chargeDuration);
            float currentDiameter = targetRadius * 2f * t;
            innerRing.localScale = new Vector3(currentDiameter, 0.02f, currentDiameter);
            yield return null;
        }

        innerRing.localScale = new Vector3(targetRadius * 2f, 0.02f, targetRadius * 2f);
        ApplyDamage();
        isActive = false;
        Destroy(gameObject, 0.15f);
    }

    private void ApplyDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetRadius, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            PlayerHealth playerHealth = hits[i].GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!isActive && targetRadius <= 0f)
            return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, targetRadius > 0f ? targetRadius : 1f);
    }
}
