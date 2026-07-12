using UnityEngine;

/// <summary>
/// Hiển thị Transform Player trên Inspector (field Target).
/// Runtime: FindObjectByType liên tục — không cần gán tay (gán tay chỉ dùng khi test tắt auto find).
/// </summary>
public class ZombieVisual : MonoBehaviour
{
    [Header("Player Target")]
    [Tooltip("Transform của Player/Soldier. Để trống — Play sẽ tự gán bằng FindObjectByType mỗi interval.")]
    [SerializeField] private Transform target;

    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private float targetSearchInterval = 0.25f;

    [Header("Low health blink")]
    [SerializeField] private Renderer zombieRenderer;
    [SerializeField] private Color hurtColor = Color.red;
    [SerializeField] private float blinkSpeed = 0.2f;
    [SerializeField] private float healthThreshold = 0.2f;

    private Color originalColor;
    private Material mat;
    private bool isBlinking;
    private Health health;
    private Transform lastLoggedTarget;
    private float nextSearchTime;

    public Transform Target => target;

    public void RefreshTarget()
    {
        if (!autoFindTarget)
            return;

        Transform found = FindPlayerTransform();
        if (found != target)
        {
            target = found;
            LogTargetIfChanged();
        }
    }

    private void Awake()
    {
        health = GetComponentInParent<Health>();

        if (zombieRenderer != null)
        {
            mat = zombieRenderer.material;
            originalColor = mat.color;
        }
    }

    private void OnEnable()
    {
        RefreshTarget();
    }

    private void Update()
    {
        if (Time.time >= nextSearchTime)
        {
            nextSearchTime = Time.time + targetSearchInterval;
            RefreshTarget();
        }

        if (health == null || health.IsDead || mat == null)
            return;

        if (!isBlinking && health.HealthPoint <= health.MaxHealthPoint * healthThreshold)
            StartCoroutine(BlinkRed());
    }

    private static Transform FindPlayerTransform()
    {
        SoldierController soldier = FindAnyObjectByType<SoldierController>();
        if (soldier != null)
            return soldier.transform;

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
            return playerHealth.transform;

        GameObject tagged = GameObject.FindWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    private void LogTargetIfChanged()
    {
        if (target == lastLoggedTarget)
            return;

        lastLoggedTarget = target;

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            CritDebug.Log($"{name} Target = {target.name} (FindObjectByType) | dist={dist:F1}m", this);
        }
        else
        {
            CritDebug.Warn($"{name} Target = null — FindObjectByType không thấy Player", this);
        }
    }

    private System.Collections.IEnumerator BlinkRed()
    {
        isBlinking = true;

        while (health != null && health.HealthPoint > 0)
        {
            mat.color = hurtColor;
            yield return new WaitForSeconds(blinkSpeed);
            mat.color = originalColor;
            yield return new WaitForSeconds(blinkSpeed);
        }

        isBlinking = false;
    }
}
