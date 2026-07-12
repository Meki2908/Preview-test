using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự gắn HealthBar + wire Fill trên Canvas (prefab/scene).
/// Gắn lên root Canvas hoặc object Health.
/// </summary>
[DisallowMultipleComponent]
public class HealthUIBootstrap : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private string healthObjectName = "Health";
    [SerializeField] private string fillObjectName = "Fill";

    private void Awake()
    {
        EnsureHealthBar();
    }

    public void EnsureHealthBar()
    {
        Transform healthRoot = transform;
        if (!healthRoot.name.Equals(healthObjectName, System.StringComparison.OrdinalIgnoreCase))
        {
            Transform found = transform.Find(healthObjectName);
            if (found == null)
                found = FindChildByName(transform, healthObjectName);

            if (found != null)
                healthRoot = found;
        }

        if (healthFill == null)
        {
            Transform fill = healthRoot.Find(fillObjectName);
            if (fill == null)
                fill = FindChildByName(healthRoot, fillObjectName);

            if (fill != null)
                healthFill = fill.GetComponent<Image>();
        }

        HealthBar healthBar = healthRoot.GetComponent<HealthBar>();
        if (healthBar == null)
            healthBar = healthRoot.gameObject.AddComponent<HealthBar>();

        if (healthFill != null)
            healthBar.SetHealthFill(healthFill);

        if (healthFill == null)
        {
            Debug.LogWarning(
                $"HealthUIBootstrap không tìm thấy Image '{fillObjectName}' dưới '{healthObjectName}'",
                this);
        }
    }

    private static Transform FindChildByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                return children[i];
        }

        return null;
    }
}
