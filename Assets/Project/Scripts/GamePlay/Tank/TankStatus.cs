using UnityEngine;
using Unity.Netcode;

public class TankStatus : NetworkBehaviour
{
    // Shield
    public bool HasShield { get; private set; }

    // Booster
    public float SpeedMultiplier { get; private set; } = 1f;
    private float boostTimer;

    // Visual event
    public event System.Action<bool> OnShieldChanged;
    public event System.Action<float> OnSpeedChanged;

    private void Update()
    {
        // Only owner
        if (!IsOwner) return;

        if (boostTimer > 0f)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f) SpeedMultiplier = 1f;
        }
    }

    public void ActivateShield()
    {
        // Only server process
        if (!IsServer) return;
        SyncShieldClientRpc(true);
    }

    public void ConsumeShield()
    {
        // Only server process
        if (!IsServer) return;
        SyncShieldClientRpc(false);
    }

    public void ActivateBooster(float multiplier, float duration)
    {
        // Only server process
        if (!IsServer) return;
        SyncBoosterClientRpc(multiplier, duration);
    }

    public void ResetHeat()
    {
        // Only server process
        if (!IsServer) return;

        GetComponent<TankShooter>()?.ResetHeat();
        ResetHeatClientRpc();
    }

    [ClientRpc]
    private void SyncShieldClientRpc(bool active)
    {
        HasShield = active;
        OnShieldChanged?.Invoke(active);
        // TODO: shield visual
    }

    [ClientRpc]
    private void SyncBoosterClientRpc(float multiplier, float duration)
    {
        SpeedMultiplier = multiplier;
        boostTimer = duration;
        OnSpeedChanged?.Invoke(multiplier);
        // TODO: booster visual
    }

    [ClientRpc]
    private void ResetHeatClientRpc()
    {
        GetComponent<TankShooter>()?.ResetHeat();
    }
}