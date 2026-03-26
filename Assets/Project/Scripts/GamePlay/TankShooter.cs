using UnityEngine;
using UnityEngine.InputSystem;
using BrmnModules.Pool;
using System.Globalization;
using Unity.Netcode;

public class TankShooter : NetworkBehaviour
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

    public override void OnNetworkSpawn()
    {
        // -- Only the owner registers input and aim --
        if (!IsOwner) return;

        _inputActions = new PlayerInputActions();
        _mainCamera = Camera.main;

        _inputActions.Player.Fire.Enable();
        _inputActions.Player.Fire.started += OnFireStarted;
        _inputActions.Player.Fire.canceled += OnFireCanceled;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        _inputActions.Player.Fire.started -= OnFireStarted;
        _inputActions.Player.Fire.canceled -= OnFireCanceled;
        _inputActions.Player.Fire.Disable();
    }

    private void Update()
    {
        // -- Only the owner handles input --
        if (!IsOwner) return;

        HandleAim();
        HandleOverheat();
        SetTurretMaterial();

        if (_fireCoolTimer > 0f)
            _fireCoolTimer -= Time.deltaTime;

        if (_isCharging)
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);

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

        float ratio = _chargeTime / maxChargeTime;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, ratio);
        Vector3 velocity = firePoint.forward * speed;

        // -- Request server to spawn projectile --
        FireServerRpc(velocity);

        AddHeat(ratio);
        _isCharging = false;
        _fireCoolTimer = fireCoolTime;
    }

    // -- ServerRpc: runs on server, called by owner client --
    // RequireOwnership: only the owner of this object can call this
    [ServerRpc(RequireOwnership = true)]
    private void FireServerRpc(Vector3 velocity)
    {
        GameObject obj = PoolManager.Instance.Get(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation,
            projectileParent
        );
        obj.GetComponent<Projectile>().Init(projectilePrefab, velocity);
        obj.GetComponent<NetworkObject>().Spawn();
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