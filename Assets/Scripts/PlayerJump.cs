using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerJump : MonoBehaviour
{
    [SerializeField] private InputActionReference jumpAction; // Gắn action "Jump" từ InputActionAsset
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = Physics.gravity.y;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // giữ dính mặt đất
        }

        // Nếu nhấn jump
        if (jumpAction.action.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // công thức vật lý
        }

        // Áp gravity
        velocity.y += gravity * Time.deltaTime;

        // Apply di chuyển
        controller.Move(velocity * Time.deltaTime);
    }
}
