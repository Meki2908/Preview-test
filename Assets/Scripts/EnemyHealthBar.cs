using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu world-space trên đầu enemy. Gắn trên child UI (Canvas World Space).
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.1f, 0f);

    private Health health;
    private Transform followTarget;
    private Camera mainCamera;

    private void Awake()
    {
        health = GetComponentInParent<Health>();
        followTarget = health != null ? health.transform : transform.parent;
        mainCamera = Camera.main;

        if (barRoot == null)
            barRoot = gameObject;

        UpdateBar(force: true);
    }

    private void LateUpdate()
    {
        if (health == null || followTarget == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        transform.position = followTarget.position + worldOffset;

        if (mainCamera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

        UpdateBar();
    }

    private void UpdateBar(bool force = false)
    {
        if (health == null || fillImage == null)
            return;

        if (health.IsDead)
        {
            barRoot.SetActive(false);
            return;
        }

        float ratio = health.MaxHealthPoint > 0
            ? (float)health.HealthPoint / health.MaxHealthPoint
            : 0f;

        fillImage.fillAmount = ratio;

        if (hideWhenFull)
            barRoot.SetActive(ratio < 0.999f);
        else if (force)
            barRoot.SetActive(true);
    }
}
