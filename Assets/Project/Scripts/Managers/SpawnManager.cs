using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Spawns player tanks when game starts using selected tank prefabs.
// Spawn happens on game start, not on client connect.
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;

    private List<ulong> _connectedClients = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        // -- Just track connections, don't spawn yet --
        if (!_connectedClients.Contains(clientId))
            _connectedClients.Add(clientId);

        Debug.Log($"Client connected: {clientId}. Total: {_connectedClients.Count}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        _connectedClients.Remove(clientId);
    }

    // -- Called by MatchManager when host clicks start --
    public void SpawnAllPlayers()
    {
        if (!IsServer) return;

        int spawnIndex = 0;

        foreach (ulong clientId in _connectedClients)
        {
            // -- Get selected tank prefab --
            GameObject prefab = TankSelectManager.Instance.GetTankPrefab(clientId);

            // -- Spawn at assigned position --
            Vector3 pos = spawnIndex < spawnPoints.Count
                ? spawnPoints[spawnIndex].position
                : Vector3.zero;

            GameObject tank = Instantiate(prefab, pos, Quaternion.identity);
            NetworkObject netObj = tank.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId);

            GameManager.Instance.SetPlayer(tank.GetComponent<TankHealth>());

            spawnIndex++;
        }
    }
}