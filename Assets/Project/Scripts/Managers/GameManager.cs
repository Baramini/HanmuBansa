using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public event System.Action OnGameStarted;
    public bool IsGameStarted => _networkGameStarted.Value;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 300f;    // -- 5 minutes --
    [SerializeField] private float specialItemTime = 180f; // -- 3 minutes --

    // -- NetworkVariables: server writes, all clients read --
    private NetworkVariable<float> _networkTimer = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<int> _networkAliveCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> _networkGameStarted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // -- Events for HUD --
    public event System.Action<float> OnTimerChanged;
    public event System.Action<int> OnAliveCountChanged;
    public event System.Action OnSpecialItemWarning;  // -- 2:30 warning --
    public event System.Action OnSpecialItemSpawn;    // -- 3:00 spawn --
    public event System.Action<string> OnGameEnd;

    private List<TankHealth> _tanks = new();
    private bool _specialItemWarned = false;
    private bool _specialItemSpawned = false;
    private bool _gameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        // -- Subscribe on all clients for HUD updates --
        _networkTimer.OnValueChanged += (prev, cur) => OnTimerChanged?.Invoke(cur);
        _networkAliveCount.OnValueChanged += (prev, cur) => OnAliveCountChanged?.Invoke(cur);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        _networkTimer.OnValueChanged -= (prev, cur) => OnTimerChanged?.Invoke(cur);
        _networkAliveCount.OnValueChanged -= (prev, cur) => OnAliveCountChanged?.Invoke(cur);
    }

    // -- Called from SpawnManager on server --
    public void SetPlayer(TankHealth tank)
    {
        if (!IsServer) return;

        _tanks.Add(tank);
        _networkAliveCount.Value = _tanks.Count;
        tank.OnDead += () => OnTankDead(tank);

        Debug.Log($"Player registered. Count: {_tanks.Count}");
    }

    // -- Called by host to start game --
    public void StartGame()
    {
        if (!IsServer) return;
        if (_networkGameStarted.Value) return;

        _networkGameStarted.Value = true;
        _networkTimer.Value = 0f;
        _gameEnded = false;
        _specialItemWarned = false;
        _specialItemSpawned = false;

        NotifyGameStartedClientRpc();
    }

    [ClientRpc]
    private void NotifyGameStartedClientRpc()
    {
        OnGameStarted?.Invoke();
    }

    private void Update()
    {
        // -- Timer only runs on server --
        if (!IsServer) return;
        if (!_networkGameStarted.Value) return;
        if (_gameEnded) return;

        _networkTimer.Value += Time.deltaTime;
        float remaining = gameDuration - _networkTimer.Value;

        // -- 2:30 warning: 30s before special item spawn --
        if (!_specialItemWarned && _networkTimer.Value >= specialItemTime - 30f)
        {
            _specialItemWarned = true;
            NotifySpecialItemWarningClientRpc();
        }

        // -- 3:00 special item spawn --
        if (!_specialItemSpawned && _networkTimer.Value >= specialItemTime)
        {
            _specialItemSpawned = true;
            NotifySpecialItemSpawnClientRpc();
        }

        // -- 5:00 time over --
        if (_networkTimer.Value >= gameDuration)
            EndGame("Draw");
    }

    private void OnTankDead(TankHealth deadTank)
    {
        if (!IsServer) return;

        _networkAliveCount.Value--;
        NotifyTankDeadClientRpc(deadTank.GetComponent<NetworkObject>().NetworkObjectId);

        if (_networkAliveCount.Value <= 1)
        {
            // -- Find winner --
            TankHealth winner = _tanks.Find(t => !t.IsDead);
            string winnerName = winner != null ? winner.name : "Unknown";
            EndGame(winnerName);
        }
    }

    private void EndGame(string winnerName)
    {
        if (_gameEnded) return;
        _gameEnded = true;
        EndGameClientRpc(winnerName);
    }

    [ClientRpc]
    private void NotifyTankDeadClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            netObj.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void NotifySpecialItemWarningClientRpc()
    {
        // -- 2:30 warning: show banner + map marker --
        OnSpecialItemWarning?.Invoke();
        Debug.Log("Special item incoming in 30s!");
    }

    [ClientRpc]
    private void NotifySpecialItemSpawnClientRpc()
    {
        // -- 3:00 spawn --
        OnSpecialItemSpawn?.Invoke();
        Debug.Log("Special item spawned!");
    }

    [ClientRpc]
    private void EndGameClientRpc(string winnerName)
    {
        _gameEnded = true;
        OnGameEnd?.Invoke(winnerName);
        Time.timeScale = 0f;
        Debug.Log($"Game over. Winner: {winnerName}");
    }

    // -- Utility: remaining time in seconds --
    public float GetRemainingTime() => Mathf.Max(0f, gameDuration - _networkTimer.Value);

    // -- Utility: alive count --
    public int GetAliveCount() => _networkAliveCount.Value;
}