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
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject prefab = TankSelectManager.Instance?.GetTankPrefab(clientId);
            Vector3 pos = spawnIndex < spawnPoints.Count
                ? spawnPoints[spawnIndex].position
                : Vector3.zero;

            GameObject tank = Instantiate(prefab, pos, Quaternion.identity);
            NetworkObject netObj = tank.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId);

            // -- Get player name from Lobby data --
            string playerName = GetPlayerName(clientId);
            GameManager.Instance?.SetPlayer(
                tank.GetComponent<TankHealth>(), clientId, playerName);

            spawnIndex++;
        }
    }

    private string GetPlayerName(ulong clientId)
    {
        var lobby = MatchManager.Instance?.CurrentLobby;
        if (lobby == null) return $"Player {clientId}";

        foreach (var player in lobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName"))
                return player.Data["PlayerName"].Value;
        }
        return $"Player {clientId}";
    }
}