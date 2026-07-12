using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public Transform cameraHolder;
    public static PlayerMovement Instance { get; private set; }
    private GameManager manager;
    private Stamina _stamina;
    [SerializeField] private Vector3 defaultPosition;

    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _sprintAction;
    [SerializeField] private CharacterController _controller;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _sprintSpeed;
    void Awake()
    {
        // Soldier (top-down) là player chính. Nếu có Soldier trong scene thì đây là
        // player FPS cũ → tự tắt để tránh tồn tại hai player song song.
        if (GetComponent<SoldierController>() == null && FindAnyObjectByType<SoldierController>() != null)
        {
            enabled = false;
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // giữ Player qua scene
    }
    private void Start()
    {
        manager = FindAnyObjectByType<GameManager>();
        _stamina = FindAnyObjectByType<Stamina>();
        // Khi bắt đầu game thì set về vị trí mặc định
        transform.position = defaultPosition;
    }
    private void OnEnable()
    {
        GameManager.OnRestart += ResetPosition;
    }

    private void OnDisable()
    {
        GameManager.OnRestart -= ResetPosition;
    }
    public void ResetPosition()
    {
        transform.position = defaultPosition;
        Debug.Log("Reset Position");
    }
    private void Update()
    {
        Vector2 input = _moveAction.action.ReadValue<Vector2>();
        Vector3 direction = transform.forward * input.y + transform.right * input.x;
        
        Move(direction, input);
    }

    private void Move(Vector3 direction, Vector2 input)
    {
        if (input == Vector2.zero) return;

        bool wantsToSprint = _sprintAction.action.IsPressed();
        bool canSprint = wantsToSprint && !_stamina.IsEmpty;

        float speed = canSprint ? _sprintSpeed : _moveSpeed;

        if (canSprint)
        {
            _stamina.UseStamina(15f);
        }

        _controller.SimpleMove(direction * speed);
    }
}
