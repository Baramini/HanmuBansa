using Unity.Netcode;

public class ShieldEffect : NetworkBehaviour
{
    public bool IsActive { get; private set; } = false;

    public void Activate()
    {
        IsActive = true;
        // Show all clients shield visual
        ActivateClientRpc();
    }

    public void ConsumeShield()
    {
        IsActive = false;
        // Show all clients shield visual
        DeactivateClientRpc();
        Destroy(this);
    }

    [ClientRpc]
    private void ActivateClientRpc()
    {
        IsActive = true;
        // TODO: Add shield visual
    }

    [ClientRpc]
    private void DeactivateClientRpc()
    {
        IsActive = false;
        // TODO: Add shield visual
    }
}