using UnityEngine;
using UnityEngine.InputSystem;
using BrmnModules.Pool;
using System;

public class TankShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform projectileParent;
    [SerializeField] private Transform turret;
    [SerializeField] private Renderer turretRenderer;
    [SerializeField] private Renderer barrelRenderer;

    [Header("Projectile Varialbes")]
    [SerializeField] private float minSpeed = 4f;
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float fireCoolTime = 1f;

    [Header("Overheat")]
    [SerializeField] private Material[] overheatMaterials;
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float heatCooldownRate = 4f;
    [SerializeField] private float[] _overheatRanges;

    public float ChargeRatio => _chargeTime / maxChargeTime;
    public float HeatRatio => _currentHeat / maxHeat;
    public bool IsOverheated => _isOverheated;

    private int _currentMaterialIndex = -1;

    private float _chargeTime;
    private float _currentHeat;
    private float _overheatTimer;
    private float _fireCoolTimer;

    private bool _isCharging;
    private bool _isOverheated;

    private PlayerInputActions _inputActions;
    private Camera _mainCamera;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _mainCamera = Camera.main;

        Debug.Assert(_overheatRanges.Length == 4,
            "TankShooter: _overheatRanges 배열 크기는 반드시 4여야 합니다.");
        Debug.Assert(overheatMaterials.Length == 5,
            "TankShooter: overheatMaterials 배열 크기는 반드시 5여야 합니다.");
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

    private void Update()
    {
        HandleAim();
        HandleOverheat();
        SetTurretMaterial();

        if (_fireCoolTimer > 0f)
            _fireCoolTimer -= Time.deltaTime;

        if (_isCharging)
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);

        // Freezing
        if (!_isOverheated && _currentHeat > 0f)
            _currentHeat = Mathf.Max(0f, _currentHeat - heatCooldownRate * Time.deltaTime);
    }

    private void HandleAim()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mouseScreen);

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 direction = worldPoint - turret.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                turret.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (_isOverheated || _fireCoolTimer > 0f) return;
        _isCharging = true;
        _chargeTime = 0f;
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (!_isCharging) return;
        Fire();
        _isCharging = false;
        _fireCoolTimer = fireCoolTime;
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
        _currentHeat = Mathf.Clamp(_currentHeat + 15f + chargeRatio * 10f, 0f, maxHeat);

        if (_currentHeat >= maxHeat)
            TriggerOverheat();
    }

    private void TriggerOverheat()
    {
        _isOverheated = true;
        _overheatTimer = 5f;
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

    private void SetTurretMaterial()
    {
        int index = _currentHeat switch
        {
            var f when f >= _overheatRanges[3] => 4,
            var f when f >= _overheatRanges[2] => 3,
            var f when f >= _overheatRanges[1] => 2,
            var f when f >= _overheatRanges[0] => 1,
            _ => 0,
        };

        if (index == _currentMaterialIndex) return;
        _currentMaterialIndex = index;

        turretRenderer.material = overheatMaterials[index];
        barrelRenderer.material = overheatMaterials[index];
    }
}