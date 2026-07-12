using UnityEngine;

/// <summary>
/// Grenade aim joystick: kéo để xoay nhân vật, thả để ném (lực theo độ kéo).
/// Gắn lên GameObject có Terresquall.VirtualJoystick.
/// </summary>
[RequireComponent(typeof(Terresquall.VirtualJoystick))]
public class GrenadeJoystick : MonoBehaviour
{
    [SerializeField] private SoldierShooting soldierShooting;
    [SerializeField] private SoldierController soldierController;
    [SerializeField] private Terresquall.VirtualJoystick joystick;

    [Header("Throw")]
    [SerializeField] private float minThrowMagnitude = 0.25f;
    [SerializeField] private float rotationSpeed = 720f;

    private bool wasHeld;
    private Vector2 lastAxis;

    private void Awake()
    {
        if (joystick == null)
            joystick = GetComponent<Terresquall.VirtualJoystick>();

        if (soldierShooting == null)
            soldierShooting = FindAnyObjectByType<SoldierShooting>();

        if (soldierController == null && soldierShooting != null)
            soldierController = soldierShooting.GetComponent<SoldierController>();
    }

    private void Update()
    {
        if (joystick == null || soldierShooting == null)
            return;

        bool isHeld = joystick.currentPointerId > -2;

        if (isHeld)
        {
            lastAxis = joystick.GetAxis();
            soldierShooting.SetGrenadeAiming(true);
            RotateTowardAim(lastAxis);
        }
        else if (wasHeld)
        {
            soldierShooting.SetGrenadeAiming(false);
            TryThrow(lastAxis);
            lastAxis = Vector2.zero;
        }

        wasHeld = isHeld;
    }

    private void RotateTowardAim(Vector2 axis)
    {
        Vector3 aimDir = new Vector3(axis.x, 0f, axis.y);
        if (aimDir.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(aimDir.normalized, Vector3.up);
        Transform target = soldierController != null ? soldierController.transform : soldierShooting.transform;
        target.rotation = Quaternion.RotateTowards(target.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void TryThrow(Vector2 axis)
    {
        float magnitude = axis.magnitude;
        if (magnitude < minThrowMagnitude)
            return;

        Vector3 throwDir = new Vector3(axis.x, 0f, axis.y);
        soldierShooting.CommitGrenadeThrow(throwDir, magnitude);
    }
}
