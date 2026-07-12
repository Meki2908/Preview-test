using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tan biến zombie bằng _Dissolve khi chết. Chạy song song với ragdoll.
/// Gắn cùng Health trên prefab zombie (material dissolve, _Dissolve = 0 lúc sống).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class ZombieDissolveOnDeath : MonoBehaviour
{
    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

    [Header("Dissolve")]
    [SerializeField] private float dissolveDelay = 1f;
    [SerializeField] private float dissolveDuration = 2f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool destroyWhenComplete = true;

    private readonly List<Material> dissolveMaterials = new List<Material>();
    private Health health;
    private bool isDissolving;

    private void Awake()
    {
        health = GetComponent<Health>();
        CacheDissolveMaterials();
        SetDissolve(0f);

        ZombieRagdoll ragdoll = GetComponent<ZombieRagdoll>();
        if (ragdoll != null)
            ragdoll.UseExternalDestroy = true;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDead.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDead.RemoveListener(HandleDeath);
    }

    private void CacheDissolveMaterials()
    {
        dissolveMaterials.Clear();

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material != null && material.HasProperty(DissolveId))
                    dissolveMaterials.Add(material);
            }
        }
    }

    private void HandleDeath()
    {
        if (isDissolving || dissolveMaterials.Count == 0)
            return;

        isDissolving = true;
        EnemyLowHealthFlash flash = GetComponent<EnemyLowHealthFlash>();
        if (flash != null)
            flash.enabled = false;

        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        if (dissolveDelay > 0f)
            yield return new WaitForSeconds(dissolveDelay);

        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = dissolveDuration > 0f ? Mathf.Clamp01(elapsed / dissolveDuration) : 1f;
            float value = dissolveCurve.Evaluate(t);
            SetDissolve(value);
            yield return null;
        }

        SetDissolve(1f);

        if (destroyWhenComplete)
            Destroy(gameObject);
    }

    private void SetDissolve(float value)
    {
        for (int i = 0; i < dissolveMaterials.Count; i++)
            dissolveMaterials[i].SetFloat(DissolveId, value);
    }
}
