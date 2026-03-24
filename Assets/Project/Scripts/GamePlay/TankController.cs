using UnityEngine.InputSystem;
using UnityEngine;

public class TankController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;

    // divide 1p, 2p
    [SerializeField] private bool isPlayer2 = false;

    private Rigidbody _rb;
    private PlayerInputActions _inputActions;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        if (isPlayer2)
        {
            _inputActions.Player2.Move.Enable();
            _inputActions.Player2.Move.performed += OnMove;
            _inputActions.Player2.Move.canceled += OnMove;
        }
        else
        {
            _inputActions.Player.Move.Enable();
            _inputActions.Player.Move.performed += OnMove;
            _inputActions.Player.Move.canceled += OnMove;
        }
    }

    private void OnDisable()
    {
        if (isPlayer2)
        {
            _inputActions.Player2.Move.performed -= OnMove;
            _inputActions.Player2.Move.canceled -= OnMove;
            _inputActions.Player2.Move.Disable();
        }
        else
        {
            _inputActions.Player.Move.performed -= OnMove;
            _inputActions.Player.Move.canceled -= OnMove;
            _inputActions.Player.Move.Disable();
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // forward & backward
        Vector3 moveDir = transform.forward * _moveInput.y;
        _rb.MovePosition(_rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

        // left & right rotation
        float rotate = _moveInput.x * rotateSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotate, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRotation);
    }
}