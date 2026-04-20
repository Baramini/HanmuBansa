using UnityEngine;
using Unity.Netcode;

// Attached to tank when shield item is picked up.
// Reflects the next incoming projectile once, then removes itself.
public class ShieldEffect : NetworkBehaviour
{
    public bool IsActive { get; private set; } = false;

    public void Activate()
    {
        IsActive = true;
        // -- Notify all clients to show shield visual --
        ActivateClientRpc();
    }

    // -- Called by Projectile when it hits a shielded tank --
    public void ConsumeShield()
    {
        IsActive = false;
        // -- Notify all clients to hide shield visual --
        DeactivateClientRpc();
        Destroy(this);
    }

    [ClientRpc]
    private void ActivateClientRpc()
    {
        IsActive = true;
        // -- TODO: show shield visual (attach object to tank) --
    }

    [ClientRpc]
    private void DeactivateClientRpc()
    {
        IsActive = false;
        // -- TODO: hide shield visual --
    }
}