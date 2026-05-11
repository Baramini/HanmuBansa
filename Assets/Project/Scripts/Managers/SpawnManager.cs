using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Spawns player tanks
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;

    private List<ulong> connectedClients = new();

    public override void OnNetworkSpawn()
    {
        // Only server process
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        // Only server process
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Only server process
        if (!IsServer) return;

        if (!connectedClients.Contains(clientId)) connectedClients.Add(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Only server process
        if (!IsServer) return;

        connectedClients.Remove(clientId);
    }

    // When host clicks start
    public void SpawnAllPlayers()
    {
        if (!IsServer) return;

        int spawnIndex = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject prefab = TankSelectManager.Instance?.GetTankPrefab(clientId);
            Vector3 pos = spawnIndex < spawnPoints.Count ? spawnPoints[spawnIndex].position : Vector3.zero;

            GameObject tank = Instantiate(prefab, pos, Quaternion.identity);
            NetworkObject netObj = tank.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId);

            string playerName = GetPlayerName(clientId);
            GameManager.Instance?.SetPlayer(tank.GetComponent<TankHealth>(), clientId, playerName);

            spawnIndex++;
        }
    }

    private string GetPlayerName(ulong clientId)
    {
        var lobby = MatchManager.Instance?.CurrentLobby;
        if (lobby == null) return $"Player {clientId}";

        foreach (var player in lobby.Players)
        {
            if (player.Data != null && player.Data.ContainsKey("PlayerName")) return player.Data["PlayerName"].Value;
        }
        return $"Player {clientId}";
    }
}