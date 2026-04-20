using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

// NetworkBehaviour: MonoBehaviour + network functionality
// IsOwner: true only for the local player who owns this object
public class TankController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;

    private Rigidbody _rb;
    private PlayerInputActions _inputActions;
    private Vector2 _moveInput;

    private float _speedMultiplier = 1f;
    private float _boostTimer = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _inputActions = new PlayerInputActions();
    }

    // OnNetworkSpawn: called when this object is spawned on the network
    // Use this instead of Start() for network objects
    public override void OnNetworkSpawn()
    {
        // -- Only the owner registers input --
        // Other clients' tanks are controlled by NetworkTransform
        if (!IsOwner) return;

        _inputActions.Player.Move.Enable();
        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        _inputActions.Player.Move.performed -= OnMove;
        _inputActions.Player.Move.canceled -= OnMove;
        _inputActions.Player.Move.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    private void Update()
    {
        if (_boostTimer > 0f)
        {
            _boostTimer -= Time.deltaTime;
            if (_boostTimer <= 0f)
                _speedMultiplier = 1f;  // -- Reset speed --
        }
    }

    private void FixedUpdate()
    {
        // -- Only the owner moves their own tank --
        if (!IsOwner) return;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float speedMult = GetComponent<TankStatus>()?.SpeedMultiplier ?? 1f;
        float currentSpeed = moveSpeed * speedMult;

        Vector3 moveDir = transform.forward * _moveInput.y;
        _rb.MovePosition(_rb.position + moveDir * currentSpeed * Time.fixedDeltaTime);

        float rotate = _moveInput.x * rotateSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotate, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // -- Cancel any velocity gained from wall collision --
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        _speedMultiplier = multiplier;
        _boostTimer = duration;
    }
}