using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Manages player spawn positions.
// Assigns a unique spawn point to each connecting client.
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private Transform projectileParent;
    [SerializeField] private List<Transform> spawnPoints;

    public override void OnNetworkSpawn()
    {
        // -- Only the server manages spawn logic --
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // -- Get the player object for this client --
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(
                clientId, out NetworkClient client)) return;

        // -- Assign spawn position based on connection order --
        int index = NetworkManager.Singleton.ConnectedClients.Count - 1;
        index = Mathf.Clamp(index, 0, spawnPoints.Count - 1);

        client.PlayerObject.transform.position = spawnPoints[index].position;

        // -- Assign GameManager players
        GameManager.Instance.SetPlayer(client.PlayerObject.GetComponent<TankHealth>());
        client.PlayerObject.GetComponent<TankShooter>().SetProjectileParent(projectileParent);
    }
}