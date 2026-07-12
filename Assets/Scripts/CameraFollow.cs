using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = Vector3.zero;
    [SerializeField] private float smoothPos = 10f;
    [SerializeField] private float smoothRot = 15f;

    void LateUpdate()
    {
        if (target == null) return;

        // Follow vị trí
        Vector3 desiredPos = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothPos * Time.deltaTime);

        // Follow rotation (match hướng ngắm bắn)
        transform.rotation = target.rotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
