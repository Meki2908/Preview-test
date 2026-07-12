using System;
using UnityEngine;

/// <summary>
/// Điểm truy cập chung tới player hiện tại (scene sẵn hoặc spawn sau).
/// </summary>
public static class PlayerTarget
{
    public static Transform Transform { get; private set; }

    public static event Action<Transform> OnAssigned;

    public static void Register(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        Transform = playerTransform;
        OnAssigned?.Invoke(Transform);
    }

    public static void Clear()
    {
        Transform = null;
    }

    public static bool TryGet(out Transform target)
    {
        if (Transform != null && Transform.gameObject.activeInHierarchy)
        {
            target = Transform;
            return true;
        }

        if (Transform != null && !Transform.gameObject.activeInHierarchy)
            Transform = null;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            Register(playerObject.transform);
            target = Transform;
            return true;
        }

        SoldierController soldier = UnityEngine.Object.FindAnyObjectByType<SoldierController>();
        if (soldier != null)
        {
            Register(soldier.transform);
            target = Transform;
            return true;
        }

        target = null;
        return false;
    }
}
