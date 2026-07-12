using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Bật/tắt hitbox melee trên zombie. Hỗ trợ nhiều hitbox (vd. 2 tay mutant).
/// </summary>
[DisallowMultipleComponent]
public class DamageDealer : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Mỗi phần tử = 1 vùng đánh (vd. tay trái, tay phải). Collider đặt trên chính object đó hoặc child.")]
    [FormerlySerializedAs("hitbox")]
    [SerializeField] private GameObject[] hitboxes;
    [FormerlySerializedAs("autoFindHitbox")]
    [SerializeField] private bool autoFindHitboxes = true;
    [Tooltip("Tự gắn script DamageCollider lên collider có sẵn.")]
    [SerializeField] private bool autoAddDamageColliders = true;

    private Collider[] hitboxColliders;
    private DamageCollider[] damageColliders;
    private bool lastHitboxActive;
    private float hitboxEnabledTime = -1f;

    public float SecondsSinceHitboxEnabled => hitboxEnabledTime < 0f ? -1f : Time.time - hitboxEnabledTime;

    private void Awake()
    {
        if ((hitboxes == null || hitboxes.Length == 0) && autoFindHitboxes)
            hitboxes = FindHitboxObjects();

        CacheHitboxComponents();
        SetHitboxActive(false);

        if (hitboxColliders.Length == 0)
            Debug.LogWarning($"{name} không có collider hitbox — gán hitboxes[] hoặc đặt tên child chứa 'hitbox'", this);
    }

    public void EnableHitbox() => SetHitboxActive(true);

    public void DisableHitbox() => SetHitboxActive(false);

    public void SetHitboxActive(bool active)
    {
        if (hitboxColliders == null || hitboxColliders.Length == 0)
            CacheHitboxComponents();

        if (hitboxColliders != null)
        {
            for (int i = 0; i < hitboxColliders.Length; i++)
            {
                if (hitboxColliders[i] != null)
                    hitboxColliders[i].enabled = active;
            }
        }

        if (damageColliders != null)
        {
            for (int i = 0; i < damageColliders.Length; i++)
            {
                if (damageColliders[i] != null)
                    damageColliders[i].enabled = active;
            }
        }

        if (active != lastHitboxActive)
        {
            lastHitboxActive = active;
            hitboxEnabledTime = active ? Time.time : -1f;
        }
    }

    public void AssignHitboxes(GameObject[] hitboxObjects)
    {
        hitboxes = hitboxObjects;
        CacheHitboxComponents();
        SetHitboxActive(false);
    }

    private void CacheHitboxComponents()
    {
        if (hitboxes == null || hitboxes.Length == 0)
        {
            hitboxColliders = System.Array.Empty<Collider>();
            damageColliders = System.Array.Empty<DamageCollider>();
            return;
        }

        var colliderList = new System.Collections.Generic.List<Collider>();
        var damageList = new System.Collections.Generic.List<DamageCollider>();

        for (int i = 0; i < hitboxes.Length; i++)
        {
            GameObject hitbox = hitboxes[i];
            if (hitbox == null)
                continue;

            Collider[] colliders = hitbox.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < colliders.Length; c++)
            {
                Collider col = colliders[c];
                if (col == null)
                    continue;

                col.isTrigger = true;
                colliderList.Add(col);

                DamageCollider damageCollider = col.GetComponent<DamageCollider>();
                if (damageCollider == null && autoAddDamageColliders)
                    damageCollider = col.gameObject.AddComponent<DamageCollider>();

                if (damageCollider != null)
                    damageList.Add(damageCollider);
            }
        }

        hitboxColliders = colliderList.ToArray();
        damageColliders = damageList.ToArray();
    }

    private GameObject[] FindHitboxObjects()
    {
        var found = new System.Collections.Generic.List<GameObject>();
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform)
                continue;

            string name = child.name.ToLowerInvariant();
            if (!name.Contains("hitbox") && !name.Contains("damage"))
                continue;

            if (!found.Contains(child.gameObject))
                found.Add(child.gameObject);
        }

        return found.ToArray();
    }
}
