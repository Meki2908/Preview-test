using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private InputActionReference _lookAction;
    [SerializeField] private float _rotateSpeed;
    public Transform _cameraHolder;
    [SerializeField] private float _minPitch;
    [SerializeField] private float _maxPitch;

    [SerializeField] private Transform weaponHolder;

    private float _pitch;

    private void Start()
    {
        // Nếu có Soldier (top-down player) trong scene thì camera/look FPS cũ không cần chạy.
        if (FindAnyObjectByType<SoldierController>() != null)
        {
            enabled = false;
            return;
        }

        if (_cameraHolder != null)
            _pitch = _cameraHolder.transform.localEulerAngles.x;
    }

    private void Update()
    {
        if (_cameraHolder == null) return;

        var input = _lookAction.action.ReadValue<Vector2>();
        UpdateYaw(input.x);
        UpdatePitch(input.y);
    }

    private void UpdateYaw(float inputX)
    {
        var deltaYaw = inputX * _rotateSpeed * Time.deltaTime;
        transform.Rotate(0, deltaYaw, 0);
    }
    private void UpdatePitch(float inputY)
    {
        var deltaPitch = -inputY * _rotateSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch + deltaPitch, _minPitch, _maxPitch);
        _cameraHolder.localRotation = Quaternion.Euler(_pitch, 0, 0);

        // Weapon xoay theo camera
        if (weaponHolder != null)
            weaponHolder.localRotation = Quaternion.Euler(_pitch, 0, 0);
    }
}
