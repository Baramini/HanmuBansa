using UnityEngine;
using Unity.Netcode;
using BrmnModules.Audio;

[RequireComponent(typeof(ItemBase))]
public class ItemPickup : NetworkBehaviour
{
    private ItemBase item;
    private bool isPickedUp = false;

    private void Awake()
    {
        item = GetComponent<ItemBase>();
    }

    public override void OnNetworkSpawn()
    {
        isPickedUp = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only server processes
        if (!IsServer) return;
        if (isPickedUp) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Tank"))
        {
            isPickedUp = true;
            item.Apply(other.transform.parent.parent.gameObject);

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