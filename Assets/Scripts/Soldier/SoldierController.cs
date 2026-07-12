using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class SoldierController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Virtual Joysticks")]
    public Terresquall.VirtualJoystick leftJoystick;
    public Terresquall.VirtualJoystick rightJoystick; // Chỉ dùng để xoay mặt / ngắm (twin-stick)

    private CharacterController characterController;
    private Animator animator;
    private PlayerHealth playerHealth;

    public Terresquall.VirtualJoystick LeftJoystick => leftJoystick;
    public Terresquall.VirtualJoystick RightJoystick => rightJoystick;
    public bool InputEnabled { get; private set; } = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Soldier là player chính của game.
        gameObject.tag = "Player";

        if (GetComponent<PlayerHealth>() == null)
            gameObject.AddComponent<PlayerHealth>();

        playerHealth = GetComponent<PlayerHealth>();

        if (GetComponent<SoldierShooting>() == null)
            gameObject.AddComponent<SoldierShooting>();

        if (GetComponent<FootstepController>() == null)
            gameObject.AddComponent<FootstepController>();

        PlayerTarget.Register(transform);
    }

    private void Start()
    {
        TopDownCinemachineCamera topDownCamera = FindAnyObjectByType<TopDownCinemachineCamera>();
        if (topDownCamera != null)
            topDownCamera.SetFollowTarget(CameraTargetAnchor.GetOrCreate(transform));
    }

    private void Update()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null && playerHealth.IsDead)
            return;

        if (!InputEnabled)
            return;

        HandleAiming();
        HandleMovement();
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
    }

    private Vector2 LeftAxis => leftJoystick != null ? leftJoystick.GetAxis() : Vector2.zero;
    private Vector2 RightAxis => rightJoystick != null ? rightJoystick.GetAxis() : Vector2.zero;

    private void HandleMovement()
    {
        Vector2 input = LeftAxis;
        Vector3 inputDir = new Vector3(input.x, 0f, input.y);

        // Di chuyển nhân vật: kéo joystick nhẹ = chậm, kéo max = nhanh
        if (inputDir.magnitude > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(inputDir.magnitude);
            characterController.Move(inputDir.normalized * moveSpeed * speedFactor * Time.deltaTime);
        }

        // Hướng tương đối so với mặt của Soldier để blend animation 8 hướng
        float currentSpeed = Mathf.Clamp01(inputDir.magnitude);
        Vector3 worldMoveDir = currentSpeed > 0.1f ? inputDir.normalized : Vector3.zero;
        Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir);

        animator.SetFloat("Speed", currentSpeed > 0.1f ? currentSpeed : 0f);
        animator.SetFloat("MoveX", localMoveDir.x);
        animator.SetFloat("MoveZ", localMoveDir.z);
    }

    private void HandleAiming()
    {
        Vector2 aim = RightAxis;
        Vector3 aimDir = new Vector3(aim.x, 0f, aim.y);

        // Twin-stick: chỉ joystick phải xoay mặt. Trái chỉ di chuyển (strafe/lùi).
        if (aimDir.magnitude <= 0.1f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(aimDir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
    }
}
