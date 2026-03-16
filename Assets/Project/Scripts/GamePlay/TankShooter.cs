using UnityEngine;
using UnityEngine.InputSystem;
using BrmnModules.Pool;

public class TankShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform projectileParent;

    [Header("Projectile Speed")]
    [SerializeField] private float minSpeed = 8f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Charge")]
    [SerializeField] private float maxChargeTime = 2f;

    [Header("Overheat")]
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float heatCooldownRate = 15f;

    public float ChargeRatio => _chargeTime / maxChargeTime;
    public float HeatRatio => _currentHeat / maxHeat;
    public bool IsOverheated => _isOverheated;

    private float _chargeTime;
    private bool _isCharging;
    private float _currentHeat;
    private bool _isOverheated;
    private float _overheatTimer;

    // Input Action 직접 참조
    private PlayerInputActions _inputActions;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Fire.Enable();
        _inputActions.Player.Fire.started += OnFireStarted;   // 누르는 순간
        _inputActions.Player.Fire.canceled += OnFireCanceled;  // 떼는 순간
    }

    private void OnDisable()
    {
        _inputActions.Player.Fire.started -= OnFireStarted;
        _inputActions.Player.Fire.canceled -= OnFireCanceled;
        _inputActions.Player.Fire.Disable();
    }

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (_isOverheated) return;
        _isCharging = true;
        _chargeTime = 0f;
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (!_isCharging) return;
        Fire();
        _isCharging = false;
    }

    private void Update()
    {
        HandleOverheat();

        if (_isCharging)
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);

        // Freezing
        if (!_isOverheated && _currentHeat > 0f)
            _currentHeat = Mathf.Max(0f, _currentHeat - heatCooldownRate * Time.deltaTime);
    }

    private void Fire()
    {
        float ratio = _chargeTime / maxChargeTime;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, ratio);
        Vector3 velocity = firePoint.forward * speed;

        GameObject obj = PoolManager.Instance.Get(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation,
            projectileParent
        );
        obj.GetComponent<Projectile>().Init(projectilePrefab, velocity);

        AddHeat(ratio);
    }

    private void AddHeat(float chargeRatio)
    {
        _currentHeat = Mathf.Clamp(_currentHeat + chargeRatio * 35f, 0f, maxHeat);
        if (_currentHeat >= maxHeat)
            TriggerOverheat();
    }

    private void TriggerOverheat()
    {
        _isOverheated = true;
        _overheatTimer = 5f;
        _currentHeat = maxHeat;
    }

    private void HandleOverheat()
    {
        if (!_isOverheated) return;
        _overheatTimer -= Time.deltaTime;
        if (_overheatTimer <= 0f)
        {
            _isOverheated = false;
            _currentHeat = 0f;
        }
    }
}