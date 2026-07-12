using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nhấp nháy đỏ nhẹ khi enemy yếu máu. Dùng MaterialPropertyBlock, không đụng shared material.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class EnemyLowHealthFlash : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Low health flash")]
    [SerializeField] [Range(0.05f, 1f)] private float lowHealthThreshold = 0.35f;
    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.1f, 1f);
    [SerializeField] [Range(0f, 1f)] private float flashStrength = 0.55f;
    [SerializeField] private float pulseSpeed = 5f;

    private struct FlashTarget
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public Color BaseColor;
        public int ColorPropertyId;
    }

    private readonly List<FlashTarget> targets = new List<FlashTarget>();
    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    private Health health;
    private bool isLowHealth;

    private void Awake()
    {
        health = GetComponent<Health>();
        CacheFlashTargets();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthRatioChanged.AddListener(HandleHealthRatioChanged);

        RefreshLowHealthState();
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthRatioChanged.RemoveListener(HandleHealthRatioChanged);

        ClearFlash();
    }

    private void Update()
    {
        if (!isLowHealth || health == null || health.IsDead)
        {
            if (!isLowHealth)
                ClearFlash();
            return;
        }

        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        ApplyFlash(pulse);
    }

    private void HandleHealthRatioChanged(float ratio)
    {
        isLowHealth = ratio > 0f && ratio <= lowHealthThreshold;
        if (!isLowHealth)
            ClearFlash();
    }

    private void RefreshLowHealthState()
    {
        if (health == null || health.MaxHealthPoint <= 0)
        {
            isLowHealth = false;
            return;
        }

        float ratio = (float)health.HealthPoint / health.MaxHealthPoint;
        isLowHealth = ratio > 0f && ratio <= lowHealthThreshold;
    }

    private void CacheFlashTargets()
    {
        targets.Clear();

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                int propertyId = 0;
                Color baseColor = Color.white;

                if (material.HasProperty(BaseColorId))
                {
                    propertyId = BaseColorId;
                    baseColor = material.GetColor(BaseColorId);
                }
                else if (material.HasProperty(ColorId))
                {
                    propertyId = ColorId;
                    baseColor = material.GetColor(ColorId);
                }
                else
                {
                    continue;
                }

                targets.Add(new FlashTarget
                {
                    Renderer = renderer,
                    MaterialIndex = i,
                    BaseColor = baseColor,
                    ColorPropertyId = propertyId
                });
            }
        }
    }

    private void ApplyFlash(float pulse)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            FlashTarget target = targets[i];
            if (target.Renderer == null)
                continue;

            Color color = Color.Lerp(target.BaseColor, flashColor, pulse * flashStrength);
            target.Renderer.GetPropertyBlock(propertyBlock, target.MaterialIndex);
            propertyBlock.SetColor(target.ColorPropertyId, color);
            target.Renderer.SetPropertyBlock(propertyBlock, target.MaterialIndex);
        }
    }

    private void ClearFlash()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            FlashTarget target = targets[i];
            if (target.Renderer == null)
                continue;

            target.Renderer.SetPropertyBlock(null, target.MaterialIndex);
        }
    }
}
