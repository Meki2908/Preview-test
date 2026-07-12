using UnityEngine;

/// <summary>
/// Pivot point on the soldier used by the top-down Cinemachine camera.
/// </summary>
public class CameraTargetAnchor : MonoBehaviour
{
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1f, 0f);

    public static Transform GetOrCreate(Transform soldierRoot)
    {
        if (soldierRoot == null)
            return null;

        var anchors = soldierRoot.GetComponentsInChildren<CameraTargetAnchor>(true);
        if (anchors.Length > 0)
            return anchors[0].transform;

        var targetObject = new GameObject("CameraTarget");
        targetObject.transform.SetParent(soldierRoot, false);
        var anchor = targetObject.AddComponent<CameraTargetAnchor>();
        targetObject.transform.localPosition = anchor.pivotOffset;
        return targetObject.transform;
    }

    private void Awake()
    {
        transform.localPosition = pivotOffset;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        transform.localPosition = pivotOffset;
    }
#endif
}
