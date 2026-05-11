using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using BrmnModules.Pool;
using BrmnModules.UI;
using BrmnModules.Audio;

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
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float heatCooldownRate = 4f;

    private HUD hud;
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    // NetworkVariable sync all clients automatically
    private NetworkVariable<float> networkHeat = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> networkIsOverheated = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Local cache for owner input handling
    private float localHeat;
    private bool localIsOverheated;

    public float ChargeRatio => chargeTime / maxChargeTime;
    public float HeatRatio => localHeat / maxHeat;

    private float lastSyncedHeat = -1f;
    private bool lastSyncedOverheated = false;
    private const float HEAT_SYNC_THRESHOLD = 1f;

    private float chargeTime;
    private float overheatTimer;
    private float fireCoolTimer;
    private bool isCharging;


    public override void OnNetworkSpawn()
    {
        // Only owner
        if (!IsOwner) return;

        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
        hud = UIManager.Instance?.GetPersistent<HUD>();

        networkIsOverheated.OnValueChanged += OnOverheatedChanged;

        inputActions.Player.Fire.Enable();
        inputActions.Player.Fire.started += OnFireStarted;
        inputActions.Player.Fire.canceled += OnFireCanceled;
    }

    public override void OnNetworkDespawn()
    {
        // Only owner
        if (!IsOwner) return;

        networkIsOverheated.OnValueChanged -= OnOverheatedChanged;

        inputActions.Player.Fire.started -= OnFireStarted;
        inputActions.Player.Fire.canceled -= OnFireCanceled;
        inputActions.Player.Fire.Disable();
    }

    // Notify aLL clients overheat
    private void OnOverheatedChanged(bool previous, bool current)
    {
        // TODO: add overheat visual effect
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted) return;

        HandleAim();

        if (fireCoolTimer > 0f) fireCoolTimer -= Time.deltaTime;
        if (isCharging) chargeTime = Mathf.Min(chargeTime + Time.deltaTime, maxChargeTime);
        if (!localIsOverheated && localHeat > 0f) localHeat = Mathf.Max(0f, localHeat - heatCooldownRate * Time.deltaTime);

        SyncHeatIfNeeded();
        HandleOverheatTimer();

        hud?.SetChargeRatio(ChargeRatio);
        hud?.SetHeatRatio(HeatRatio);
    }

    private void HandleOverheatTimer()
    {
        if (!localIsOverheated) return;

        overheatTimer -= Time.deltaTime;
        if (overheatTimer <= 0f)
        {
            localIsOverheated = false;
            localHeat = 0f;
        }
    }

    private void SyncHeatIfNeeded()
    {
        bool heatChanged = Mathf.Abs(localHeat - lastSyncedHeat) >= HEAT_SYNC_THRESHOLD;
        bool overheatChanged = localIsOverheated != lastSyncedOverheated;

        if (!heatChanged && !overheatChanged) return;

        lastSyncedHeat = localHeat;
        lastSyncedOverheated = localIsOverheated;

        if (IsServer) UpdateHeatOnServer(localHeat, localIsOverheated);
        else SyncHeatServerRpc(localHeat, localIsOverheated);
    }

    private void HandleAim()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mouseScreen);

        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            Vector3 direction = worldPoint - turret.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f) turret.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnFireStarted(InputAction.CallbackContext inputCallback)
    {
        if (UIManager.Instance?.IsAnyPopupOpen ?? false) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameStarted) return;

        if (localIsOverheated || fireCoolTimer > 0f) return;
        isCharging = true;
        chargeTime = 0f;
    }

    private void OnFireCanceled(InputAction.CallbackContext inputCallback)
    {
        if (!isCharging) return;

        float ratio = chargeTime / maxChargeTime;
        float speed = Mathf.Lerp(minSpeed, maxSpeed, ratio);
        Vector3 velocity = firePoint.forward * speed;

        FireServerRpc(firePoint.position, firePoint.rotation, velocity);

        AddHeatLocally(ratio);
        isCharging = false;
        fireCoolTimer = fireCoolTime;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
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

    // Update NetworkVariables
    private void UpdateHeatOnServer(float heat, bool isOverheated)
    {
        networkHeat.Value = heat;
        networkIsOverheated.Value = isOverheated;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SyncHeatServerRpc(float heat, bool isOverheated)
    {
        networkHeat.Value = heat;
        networkIsOverheated.Value = isOverheated;
    }

    private void AddHeatLocally(float chargeRatio)
    {
        localHeat = Mathf.Clamp(localHeat + 15f + chargeRatio * 10f, 0f, maxHeat);

        if (localHeat >= maxHeat) TriggerOverheatLocally();
    }

    private void TriggerOverheatLocally()
    {
        localIsOverheated = true;
        overheatTimer = 5f;
    }

    public void ResetHeat()
    {
        localHeat = 0f;
        localIsOverheated = false;
        overheatTimer = 0f;
    }
}