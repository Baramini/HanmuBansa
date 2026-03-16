using UnityEngine;
using BrmnModules.Input;

public class TankController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;

    private Rigidbody _rb;
    private IMovementInput _movementInput;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _movementInput = GetComponent<IMovementInput>();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 input = _movementInput.MovementDirection;

        // forward & backward
        Vector3 moveDir = transform.forward * input.y;
        _rb.MovePosition(_rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);

        // L, R Rotation
        float rotate = input.x * rotateSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotate, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRotation);
    }
}