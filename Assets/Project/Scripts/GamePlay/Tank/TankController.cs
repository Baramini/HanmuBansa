using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using BrmnModules.UI;

public class TankController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;

    private Rigidbody rb;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;

    private bool isLocalPlayer = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputActions = new PlayerInputActions();
    }

    public override void OnNetworkSpawn()
    {
        // Only owner
        isLocalPlayer = IsOwner && NetworkObject.IsPlayerObject;
        if (!isLocalPlayer) return;

        inputActions.Player.Move.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
    }

    public override void OnNetworkDespawn()
    {
        // Only owner
        if (!isLocalPlayer) return;

        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Move.Disable();
        inputActions.Dispose();

        isLocalPlayer = false;
    }

    private void OnMove(InputAction.CallbackContext inputCallback)
    {
        moveInput = inputCallback.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // Only owner
        if (!isLocalPlayer) return;

        if (UIManager.Instance?.IsAnyPopupOpen ?? false) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        float speedMult = GetComponent<TankStatus>()?.SpeedMultiplier ?? 1f;
        float currentSpeed = moveSpeed * speedMult;

        Vector3 moveDir = transform.forward * moveInput.y;
        rb.MovePosition(rb.position + moveDir * currentSpeed * Time.fixedDeltaTime);

        float rotate = moveInput.x * rotateSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotate, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Do not transform from wall collision
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}