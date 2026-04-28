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

        Debug.Log($"SpawnAllPlayers called. Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");

        int spawnIndex = 0;

        // -- _connectedClients 대신 NetworkManager에서 직접 가져옴 --
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Debug.Log($"Spawning for clientId: {clientId}");

            GameObject prefab = TankSelectManager.Instance?.GetTankPrefab(clientId);
            Debug.Log($"Prefab: {prefab}");

            Vector3 pos = spawnIndex < spawnPoints.Count
                ? spawnPoints[spawnIndex].position
                : Vector3.zero;

            GameObject tank = Instantiate(prefab, pos, Quaternion.identity);
            NetworkObject netObj = tank.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId);

            TankHealth health = tank.GetComponent<TankHealth>();
            GameManager.Instance?.SetPlayer(health);

            Debug.Log($"SetPlayer called for clientId: {clientId}");
            spawnIndex++;
        }
    }
}