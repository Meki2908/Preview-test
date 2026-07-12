using UnityEngine;

public class GunRaycasting : MonoBehaviour
{
    [SerializeField] private Transform _firingPos;
    [SerializeField] private Transform _aimingCamera;
    [SerializeField] private Transform _hitMarkerPrefab;
    [SerializeField] private int _damage;

    public void PerformRaycast()
    {
        Ray aimingRay = new(_aimingCamera.position, _aimingCamera.forward);
        if (Physics.Raycast(aimingRay, out var raycasthit))
        {
            Instantiate(_hitMarkerPrefab, raycasthit.point, Quaternion.LookRotation(raycasthit.normal), parent: raycasthit.collider.transform);
            if (raycasthit.collider.TryGetComponent<Health>(out var health))
            {
                health.TakeDamage(_damage);
            }
        }
    }
}