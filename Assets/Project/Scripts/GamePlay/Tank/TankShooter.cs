using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using BrmnModules.Pool;
using BrmnModules.UI;

public class TankShooter : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform turret;
    [SerializeField] private Renderer barrelRenderer;

    [Header("Projectile Variables")]
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
    public float HeatRatio => _localHeat / maxHeat;

    private float _lastSyncedHeat = -1f;
    private bool _lastSyncedOverheated = false;
    private const float HEAT_SYNC_THRESHOLD = 1f;

    // -- NetworkVariable: synced across all clients automatically --
    // Server writes, all clients read
    private NetworkVariable<float> _networkHeat = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> _networkIsOverheated = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // -- Local cache for owner's input handling --
    // Owner uses these for immediate response without waiting for server
    private float _localHeat;
    private bool _localIsOverheated;

    private int _currentMaterialIndex = -1;
    private float _chargeTime;
    private float _overheatTimer;
    private float _fireCoolTimer;
    private bool _isCharging;

    private HUD _hud;

    private Transform projectileParent;
    private PlayerInputActions _inputActions;
    private Camera _mainCamera;

    public void SetProjectileParent(Transform parent)
    {
        projectileParent = parent;
    }

    public override void OnNetworkSpawn()
    {
        _networkHeat.OnValueChanged += OnHeatChanged;
        _networkIsOverheated.OnValueChanged += OnOverheatedChanged;

        if (!IsOwner) return;

        _inputActions = new PlayerInputActions();
        _mainCamera = Camera.main;

        // -- Cache HUD reference --
        _hud = UIManager.Instance?.GetPersistent<HUD>();

        _inputActions.Player.Fire.Enable();
        _inputActions.Player.Fire.started += OnFireStarted;
        _inputActions.Player.Fire.canceled += OnFireCanceled;
    }

    public override void OnNetworkDespawn()
    {
        // -- Always unsubscribe to prevent memory leaks --
        _networkHeat.OnValueChanged -= OnHeatChanged;
        _networkIsOverheated.OnValueChanged -= OnOverheatedChanged;

        if (!IsOwner) return;

        _inputActions.Player.Fire.started -= OnFireStarted;
        _inputActions.Player.Fire.canceled -= OnFireCanceled;
        _inputActions.Player.Fire.Disable();
    }

    // -- Called on ALL clients when _networkHeat changes --
    private void OnHeatChanged(float previous, float current)
    {
        SetTurretMaterial(current);
    }

    // -- Called on ALL clients when _networkIsOverheated changes --
    private void OnOverheatedChanged(bool previous, bool current)
    {
        // -- TODO: overheat visual effect (smoke, red color) --
        // Will be implemented in art pass
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted) return;

        HandleAim();

        if (_fireCoolTimer > 0f)
            _fireCoolTimer -= Time.deltaTime;

        if (_isCharging)
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);

        if (!_localIsOverheated && _localHeat > 0f)
            _localHeat = Mathf.Max(0f, _localHeat - heatCooldownRate * Time.deltaTime);

        SyncHeatIfNeeded();
        HandleOverheatTimer();

        // -- Use cached HUD reference --
        _hud?.SetChargeRatio(ChargeRatio);
        _hud?.SetHeatRatio(HeatRatio);
        //UIManager.Instance?.GetPersistent<HUD>()?.SetOverheated(_localIsOverheated);
    }

    private void HandleOverheatTimer()
    {
        if (!_localIsOverheated) return;

        _overheatTimer -= Time.deltaTime;
        if (_overheatTimer <= 0f)
        {
            _localIsOverheated = false;
            _localHeat = 0f;
        }
    }

    private void SyncHeatIfNeeded()
    {
        bool heatChanged = Mathf.Abs(_localHeat - _lastSyncedHeat) >= HEAT_SYNC_THRESHOLD;
        bool overheatChanged = _localIsOverheated != _lastSyncedOverheated;

        if (!heatChanged && !overheatChanged) return;

        _lastSyncedHeat = _localHeat;
        _lastSyncedOverheated = _localIsOverheated;

        if (IsServer)
            UpdateHeatOnServer(_localHeat, _localIsOverheated);
        else
            SyncHeatServerRpc(_localHeat, _localIsOverheated);
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
                turret.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted) return;

        // -- Check local state for immediate response --
        if (_localIsOverheated || _fireCoolTimer > 0f) return;
        _isCharging = true;
        _chargeTime = 0f;
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (!_isCharging) return;

        float ratio = _chargeTime / maxChargeTime;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, ratio);
        Vector3 velocity = firePoint.forward * speed;

        FireServerRpc(firePoint.position, firePoint.rotation, velocity);

        AddHeatLocally(ratio);
        _isCharging = false;
        _fireCoolTimer = fireCoolTime;
    }

    [ServerRpc(RequireOwnership = true)]
    private void FireServerRpc(Vector3 spawnPosition, Quaternion spawnRotation, Vector3 velocity)
    {
        GameObject obj = PoolManager.Instance.Get(
            projectilePrefab,
            spawnPosition,
            spawnRotation,
            null
        );
        obj.GetComponent<NetworkObject>().Spawn();
        obj.GetComponent<Projectile>().InitOnServer(projectilePrefab, velocity);
    }

    // -- Update NetworkVariables on server directly (Host) --
    private void UpdateHeatOnServer(float heat, bool isOverheated)
    {
        _networkHeat.Value = heat;
        _networkIsOverheated.Value = isOverheated;
    }

    // -- Send heat state to server from client --
    [ServerRpc(RequireOwnership = true)]
    private void SyncHeatServerRpc(float heat, bool isOverheated)
    {
        _networkHeat.Value = heat;
        _networkIsOverheated.Value = isOverheated;
    }

    private void AddHeatLocally(float chargeRatio)
    {
        _localHeat = Mathf.Clamp(_localHeat + 15f + chargeRatio * 10f, 0f, maxHeat);

        if (_localHeat >= maxHeat)
            TriggerOverheatLocally();
    }

    private void TriggerOverheatLocally()
    {
        _localIsOverheated = true;
        _overheatTimer = 5f;
    }

    private void SetTurretMaterial(float heat)
    {
        int index = heat switch
        {
            var f when f >= _overheatRanges[3] => 4,
            var f when f >= _overheatRanges[2] => 3,
            var f when f >= _overheatRanges[1] => 2,
            var f when f >= _overheatRanges[0] => 1,
            _ => 0,
        };

        if (index == _currentMaterialIndex) return;
        _currentMaterialIndex = index;

        barrelRenderer.material = overheatMaterials[index];
    }

    public void ResetHeat()
    {
        _localHeat = 0f;
        _localIsOverheated = false;
        _overheatTimer = 0f;
    }
}