using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Spawns player tanks
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private List<GameObject> aiTankPrefabs;

    private List<ulong> connectedClients = new();

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

        if (!connectedClients.Contains(clientId)) connectedClients.Add(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
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

        if (GameMode.IsSingleplay)
        {
            SpawnAITanks(ref spawnIndex);
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

    private void SpawnAITanks(ref int spawnIndex)
    {
        int aiCount = SingleplaySettings.AICount;

        for (int i = 0; i < aiCount; i++)
        {
            if (aiTankPrefabs == null || aiTankPrefabs.Count == 0) break;

            Vector3 pos = spawnIndex < spawnPoints.Count
                ? spawnPoints[spawnIndex].position
                : Vector3.zero;

            int prefabIndex = Random.Range(0, aiTankPrefabs.Count);
            GameObject aiTank = Instantiate(aiTankPrefabs[prefabIndex], pos, Quaternion.identity);

            NetworkObject netObj = aiTank.GetComponent<NetworkObject>();
            netObj.Spawn();

            string aiName = $"AI_{i + 1}";
            GameManager.Instance?.SetPlayer(aiTank.GetComponent<TankHealth>(), ulong.MaxValue - (ulong)i, aiName);

            spawnIndex++;
        }
    }
}