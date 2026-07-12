using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

/// <summary>
/// Top-down Cinemachine rig: follows the soldier from above and keeps a fixed world orientation.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
[DefaultExecutionOrder(50)]
public class TopDownCinemachineCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool autoFindSoldier = true;
    [SerializeField] private string soldierTag = "Player";

    [Header("Top-Down Framing")]
    [SerializeField] private float cameraHeight = 18f;
    [SerializeField] private float cameraBackOffset = 6f;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 positionDamping = new Vector3(0.35f, 0.35f, 0.35f);
    [SerializeField] private Vector2 rotationDamping = new Vector2(0.5f, 0.5f);
    [SerializeField] private int cameraPriority = 10;

    private CinemachineCamera vcam;
    private CinemachineFollow follow;
    private CinemachineRotationComposer composer;

    private void Reset()
    {
        EnsureComponents();
        ApplySettings();
    }

    private void Awake()
    {
        EnsureComponents();

        if (followTarget == null && autoFindSoldier)
            followTarget = FindSoldierTarget();

        ApplySettings();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        EnsureComponents();
        ApplySettings();
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        ApplySettings();
    }

    private Transform FindSoldierTarget()
    {
        if (!string.IsNullOrEmpty(soldierTag))
        {
            var taggedSoldier = GameObject.FindWithTag(soldierTag);
            if (taggedSoldier != null)
                return CameraTargetAnchor.GetOrCreate(taggedSoldier.transform);
        }

        var soldierController = FindAnyObjectByType<SoldierController>();
        if (soldierController != null)
            return CameraTargetAnchor.GetOrCreate(soldierController.transform);

        return null;
    }

    private void EnsureComponents()
    {
        vcam ??= GetComponent<CinemachineCamera>();

        RemoveBodyComponent<CinemachineOrbitalFollow>();
        RemoveBodyComponent<CinemachineThirdPersonFollow>();

        follow = GetComponent<CinemachineFollow>();
        if (follow == null)
            follow = gameObject.AddComponent<CinemachineFollow>();

        composer = GetComponent<CinemachineRotationComposer>();
        if (composer == null)
            composer = gameObject.AddComponent<CinemachineRotationComposer>();
    }

    private void RemoveBodyComponent<T>() where T : Component
    {
        var component = GetComponent<T>();
        if (component == null)
            return;

        if (Application.isPlaying)
            Destroy(component);
        else
            DestroyImmediate(component);
    }

    private void ApplySettings()
    {
        if (vcam == null || follow == null || composer == null)
            return;

        vcam.Priority = cameraPriority;

        if (followTarget != null)
        {
            vcam.Follow = followTarget;
            vcam.LookAt = followTarget;
            vcam.Target.TrackingTarget = followTarget;
            vcam.Target.LookAtTarget = followTarget;
        }

        follow.FollowOffset = new Vector3(0f, cameraHeight, -Mathf.Max(0f, cameraBackOffset));
        follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
        follow.TrackerSettings.PositionDamping = positionDamping;

        composer.TargetOffset = lookAtOffset;
        composer.Damping = rotationDamping;
        composer.CenterOnActivate = true;
    }
}
