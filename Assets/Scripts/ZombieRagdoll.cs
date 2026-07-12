using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ragdoll zombie: tắt physics lúc sống, bật khi chết (ăn lựu / hết máu).
/// Gắn lên root zombie (cùng Animator, NavMeshAgent, Health).
/// </summary>
[DisallowMultipleComponent]
public class ZombieRagdoll : MonoBehaviour
{
    [Header("Explosion knockback")]
    [SerializeField] private float explosionForce = 450f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float upwardModifier = 0.4f;

    [Header("Cleanup")]
    [SerializeField] private float destroyDelay = 8f;
    [SerializeField] private bool destroyOnRagdoll = true;

    [Header("Optional")]
    [SerializeField] private Rigidbody hipsRigidbody;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Animator animator;
    private NavMeshAgent agent;
    private Collider mainCollider;
    private bool isActive;

    public bool HasRagdollSetup => ragdollBodies != null && ragdollBodies.Length > 0;
    public bool IsRagdollActive => isActive;
    public int RagdollBodyCount => ragdollBodies != null ? ragdollBodies.Length : 0;
    public bool UseExternalDestroy { get; set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        mainCollider = GetComponent<Collider>();

        CacheRagdollBodies();
        ConfigureAliveState();
    }

    public void ConfigureAliveState()
    {
        SetRagdollPhysicsActive(false);
        SetRagdollCollidersActive(false);

        Rigidbody rootBody = GetComponent<Rigidbody>();
        if (rootBody != null)
        {
            rootBody.isKinematic = true;
            rootBody.useGravity = false;
            rootBody.interpolation = RigidbodyInterpolation.None;
        }

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = false;
            agent.autoBraking = true;
        }

        if (mainCollider != null)
            mainCollider.enabled = true;
    }

    private void CacheRagdollBodies()
    {
        Rigidbody[] allBodies = GetComponentsInChildren<Rigidbody>(true);
        if (allBodies.Length == 0)
        {
            ragdollBodies = System.Array.Empty<Rigidbody>();
            ragdollColliders = System.Array.Empty<Collider>();
            return;
        }

        int childCount = 0;
        foreach (Rigidbody body in allBodies)
        {
            if (body.transform != transform)
                childCount++;
        }

        if (childCount > 0)
        {
            ragdollBodies = new Rigidbody[childCount];
            ragdollColliders = new Collider[childCount];
            int index = 0;

            foreach (Rigidbody body in allBodies)
            {
                if (body.transform == transform)
                    continue;

                ragdollBodies[index] = body;
                ragdollColliders[index] = body.GetComponent<Collider>();
                index++;
            }
        }
        else
        {
            ragdollBodies = allBodies;
            ragdollColliders = new Collider[allBodies.Length];
            for (int i = 0; i < allBodies.Length; i++)
                ragdollColliders[i] = allBodies[i].GetComponent<Collider>();
        }

        if (hipsRigidbody == null)
        {
            foreach (Rigidbody body in ragdollBodies)
            {
                string boneName = body.name.ToLowerInvariant();
                if (boneName.Contains("hip") || boneName.Contains("pelvis"))
                {
                    hipsRigidbody = body;
                    break;
                }
            }
        }

        if (hipsRigidbody == null && ragdollBodies.Length > 0)
            hipsRigidbody = ragdollBodies[0];
    }

    private void SetRagdollPhysicsActive(bool active)
    {
        foreach (Rigidbody body in ragdollBodies)
        {
            if (body == null)
                continue;

            body.isKinematic = !active;
            body.useGravity = active;
            body.detectCollisions = true;
            body.interpolation = active ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        }
    }

    private void SetRagdollCollidersActive(bool active)
    {
        if (ragdollColliders == null)
            return;

        foreach (Collider col in ragdollColliders)
        {
            if (col == null || col == mainCollider)
                continue;

            col.enabled = active;
        }
    }

    public bool TryEnableRagdoll(Vector3? explosionOrigin = null)
    {
        Debug.Log($"[RAGDOLL] TryEnableRagdoll enter {name}", this);

        if (isActive)
        {
            Debug.Log($"[RAGDOLL] Skip {name}: đã active.", this);
            return false;
        }

        if (!HasRagdollSetup)
        {
            Debug.LogWarning(
                $"[RAGDOLL] Skip {name}: chưa setup (bodies={RagdollBodyCount}). Cần Ragdoll Wizard + component ZombieRagdoll.",
                this);
            return false;
        }

        try
        {
            isActive = true;
            Debug.Log($"[RAGDOLL] Bật ragdoll {name} | bodies={RagdollBodyCount} | force={explosionForce}", this);

            if (animator != null)
            {
                animator.Update(0f);
                animator.enabled = false;
            }

            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }

            if (mainCollider != null)
                mainCollider.enabled = false;

            SetRagdollCollidersActive(true);
            SetRagdollPhysicsActive(true);

            Vector3 forceOrigin = explosionOrigin ?? transform.position + Vector3.up;
            foreach (Rigidbody body in ragdollBodies)
            {
                if (body == null)
                    continue;

                body.AddExplosionForce(explosionForce, forceOrigin, explosionRadius, upwardModifier, ForceMode.Impulse);
            }

            if (destroyOnRagdoll && !UseExternalDestroy)
                Destroy(gameObject, destroyDelay);

            Debug.Log(
                $"[RAGDOLL] OK {name} | animator={(animator != null ? animator.enabled.ToString() : "null")} | agent={(agent != null && agent.enabled)}",
                this);
            return true;
        }
        catch (System.Exception exception)
        {
            isActive = false;
            Debug.LogError($"[RAGDOLL] Lỗi khi bật ragdoll {name}: {exception}", this);
            return false;
        }
    }
}
