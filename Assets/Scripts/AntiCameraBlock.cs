using UnityEngine;

public class AntiCameraBlock : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    [SerializeField] private float _radius;
    [SerializeField] private LayerMask _hitMask;

    private float _cameraMaxDistance;

    private void Start() => _cameraMaxDistance = Mathf.Abs(_camera.localPosition.z);

    private void Update()
    {
        var direction = -transform.forward;
        Ray lookingRay = new(transform.position, direction);
        Debug.DrawRay(transform.position, direction, Color.red, 5);
        if (Physics.SphereCast(lookingRay, _radius, out var raycastHit,
            _cameraMaxDistance, _hitMask))
        {
            _camera.position = raycastHit.point;
        }
        else
        {
            _camera.localPosition = new(0, 0, -_cameraMaxDistance);
        }
    }
}