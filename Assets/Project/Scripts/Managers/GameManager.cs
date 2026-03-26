using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// GameManager runs on all clients but game logic
// is only processed on the server (Host).
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    private List<TankHealth> _tanks = new();
    private int _aliveCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SetPlayer(TankHealth tank)
    {
        // -- SetPlayer is called on server only --
        if (!IsServer) return;

        _tanks.Add(tank);
        _aliveCount = _tanks.Count;
        tank.OnDead += () => OnTankDead(tank);
    }

    private void OnTankDead(TankHealth deadTank)
    {
        // -- Death logic runs on server only --
        if (!IsServer) return;

        _aliveCount--;

        // -- Notify all clients this tank is dead --
        NotifyTankDeadClientRpc(deadTank.GetComponent<NetworkObject>().NetworkObjectId);

        if (_aliveCount <= 1)
            EndGame();
    }

    // -- ClientRpc: called on ALL clients including host --
    [ClientRpc]
    private void NotifyTankDeadClientRpc(ulong networkObjectId)
    {
        // -- Find and deactivate the dead tank on all clients --
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            netObj.gameObject.SetActive(false);
        }
    }

    private void EndGame()
    {
        // -- Find the winner --
        TankHealth winner = null;
        foreach (var tank in _tanks)
        {
            if (!tank.IsDead)
            {
                winner = tank;
                break;
            }
        }

        string winnerName = winner != null ? winner.name : "Draw";

        // -- Stop game on all clients --
        EndGameClientRpc(winnerName);
    }

    [ClientRpc]
    private void EndGameClientRpc(string winnerName)
    {
        // -- This runs on ALL clients --
        Debug.Log($"Winner: {winnerName}");
        Time.timeScale = 0f;
        // TODO: Show winner UI
    }
}