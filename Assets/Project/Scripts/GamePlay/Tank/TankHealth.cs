using UnityEngine;
using Unity.Netcode;
using System;

public class TankHealth : NetworkBehaviour
{
    [SerializeField] private int maxHp = 2;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Material damagedMaterial;

    public int CurrentHp { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int> OnDamaged;
    public event Action OnDead;

    public override void OnNetworkSpawn()
    {
        CurrentHp = maxHp;
    }

    public void TakeDamage(int amount = 1)
    {
        // -- Damage is only processed on the server --
        if (!IsServer) return;
        if (IsDead) return;

        CurrentHp = Mathf.Max(0, CurrentHp - amount);

        if (CurrentHp <= 0)
        {
            // -- Notify all clients this tank is dead --
            DieClientRpc();
        }
        else
        {
            // -- Notify all clients of damage visual --
            ApplyDamageClientRpc(CurrentHp);
        }
    }

    [ClientRpc]
    private void ApplyDamageClientRpc(int remainingHp)
    {
        // -- Update visuals on all clients --
        OnDamaged?.Invoke(remainingHp);

        if (remainingHp == 1 && bodyRenderer != null)
            bodyRenderer.material = damagedMaterial;
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        // -- Runs on ALL clients including server --
        // -- Each client handles death locally --
        IsDead = true;
        OnDead?.Invoke();
    }
}