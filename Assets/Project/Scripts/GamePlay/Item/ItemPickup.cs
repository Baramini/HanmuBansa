using UnityEngine;
using Unity.Netcode;
using BrmnModules.Audio;

// Attached to item prefabs.
// Detects tank collision and triggers item effect on server.
[RequireComponent(typeof(ItemBase))]
public class ItemPickup : NetworkBehaviour
{
    private ItemBase _item;
    private bool _isPickedUp = false;

    private void Awake()
    {
        _item = GetComponent<ItemBase>();
    }

    // -- Reset state when spawned from pool --
    public override void OnNetworkSpawn()
    {
        _isPickedUp = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // -- Only server processes pickup --
        if (!IsServer) return;
        if (_isPickedUp) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Tank"))
        {
            _isPickedUp = true;

            // -- Apply item effect to the tank --
            _item.Apply(other.transform.parent.parent.gameObject);

            // -- Notify all clients to hide item --
            HideItemClientRpc();

            AudioManager.Instance?.PlaySFX("GetItem");
        }
    }

    [ClientRpc]
    private void HideItemClientRpc()
    {
        gameObject.SetActive(false);
    }
}