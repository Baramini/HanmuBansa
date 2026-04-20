using UnityEngine;
using Unity.Netcode;

// Manages all item-related status effects on the tank.
// Pre-attached to tank prefab — no dynamic AddComponent needed.
public class TankStatus : NetworkBehaviour
{
    // -- Shield --
    public bool HasShield { get; private set; }

    // -- Booster --
    public float SpeedMultiplier { get; private set; } = 1f;
    private float _boostTimer;

    // -- Events for visual updates --
    public event System.Action<bool> OnShieldChanged;
    public event System.Action<float> OnSpeedChanged;

    private void Update()
    {
        if (!IsOwner) return;

        if (_boostTimer > 0f)
        {
            _boostTimer -= Time.deltaTime;
            if (_boostTimer <= 0f)
                SpeedMultiplier = 1f;
        }
    }

    // -- Called by server when item is picked up --
    public void ActivateShield()
    {
        if (!IsServer) return;
        SyncShieldClientRpc(true);
    }

    public void ConsumeShield()
    {
        if (!IsServer) return;
        SyncShieldClientRpc(false);
    }

    public void ActivateBooster(float multiplier, float duration)
    {
        if (!IsServer) return;
        SyncBoosterClientRpc(multiplier, duration);
    }

    public void ResetHeat()
    {
        if (!IsServer) return;

        // -- Coolant: call TankShooter directly on server --
        GetComponent<TankShooter>()?.ResetHeat();
        // -- Sync to owner client --
        ResetHeatClientRpc();
    }

    [ClientRpc]
    private void SyncShieldClientRpc(bool active)
    {
        HasShield = active;
        OnShieldChanged?.Invoke(active);
        // -- TODO: shield visual --
    }

    [ClientRpc]
    private void SyncBoosterClientRpc(float multiplier, float duration)
    {
        SpeedMultiplier = multiplier;
        _boostTimer = duration;
        OnSpeedChanged?.Invoke(multiplier);
        // -- TODO: booster visual --
    }

    [ClientRpc]
    private void ResetHeatClientRpc()
    {
        GetComponent<TankShooter>()?.ResetHeat();
    }
}