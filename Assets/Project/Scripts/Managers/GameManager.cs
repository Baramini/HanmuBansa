using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using BrmnModules.UI;
using BrmnModules.Audio;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public event System.Action OnGameStarted;
    public bool IsGameStarted => _networkGameStarted.Value;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 300f;
    [SerializeField] private float specialItemTime = 180f;

    private NetworkVariable<float> _networkTimer = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> _networkAliveCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> _networkGameStarted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event System.Action<float> OnTimerChanged;
    public event System.Action<int> OnAliveCountChanged;
    public event System.Action OnSpecialItemWarning;
    public event System.Action OnSpecialItemSpawn;
    public event System.Action<string> OnGameEnd;

    // -- clientId -> playerName mapping (server only) --
    private Dictionary<ulong, string> _playerNames = new();
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
        _networkTimer.OnValueChanged += (prev, cur) =>
        {
            OnTimerChanged?.Invoke(cur);
            UIManager.Instance?.GetPersistent<HUD>()?.SetTimer(gameDuration - cur);
        };
        _networkAliveCount.OnValueChanged += (prev, cur) =>
        {
            OnAliveCountChanged?.Invoke(cur);
            UIManager.Instance?.GetPersistent<HUD>()?.SetAliveCount(cur);
        };

        StartCoroutine(InitHUDCoroutine());

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string bgmName = sceneName == "Map_Spaceship_Warehouse" ? "Warehouse" : "Arena";
        AudioManager.Instance?.PlayBGM(bgmName);
    }

    private System.Collections.IEnumerator InitHUDCoroutine()
    {
        yield return null;
        UIManager.Instance?.GetPersistent<HUD>()
            ?.SetTimer(gameDuration - _networkTimer.Value);
        UIManager.Instance?.GetPersistent<HUD>()
            ?.SetAliveCount(_networkAliveCount.Value);
    }

    // -- Called from SpawnManager on server --
    // playerName: PlayerPrefs name sent from client
    public void SetPlayer(TankHealth tank, ulong clientId, string playerName)
    {
        if (!IsServer) return;

        _tanks.Add(tank);
        _playerNames[clientId] = playerName;
        _networkAliveCount.Value = _tanks.Count;
        tank.OnDead += () => OnTankDead(tank, clientId);

        Debug.Log($"Player registered: {playerName} (clientId:{clientId})");
    }

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
        UIManager.Instance?.HidePopup<LoadingPopup>();
        OnGameStarted?.Invoke();
    }

    private void Update()
    {
        if (!IsServer) return;
        if (!_networkGameStarted.Value) return;
        if (_gameEnded) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        _networkTimer.Value += Time.deltaTime;

        if (!_specialItemWarned && _networkTimer.Value >= specialItemTime - 30f)
        {
            _specialItemWarned = true;
            NotifySpecialItemWarningClientRpc();
        }

        if (!_specialItemSpawned && _networkTimer.Value >= specialItemTime)
        {
            _specialItemSpawned = true;
            NotifySpecialItemSpawnClientRpc();
        }

        if (_networkTimer.Value >= gameDuration)
            EndGame("", true);
    }

    private void OnTankDead(TankHealth deadTank, ulong deadClientId)
    {
        if (!IsServer) return;

        _networkAliveCount.Value--;

        // -- Get dead player's NetworkObjectId --
        ulong networkObjectId = deadTank.GetComponent<NetworkObject>().NetworkObjectId;
        NotifyTankDeadClientRpc(networkObjectId, deadClientId);

        if (_networkAliveCount.Value <= 1)
        {
            // -- Find winner --
            TankHealth winnerTank = _tanks.Find(t => !t.IsDead);

            // -- Get winner's clientId and name --
            string winnerName = "";
            if (winnerTank != null)
            {
                NetworkObject winnerNetObj = winnerTank.GetComponent<NetworkObject>();
                if (_playerNames.TryGetValue(winnerNetObj.OwnerClientId, out string name))
                    winnerName = name;
            }

            EndGame(winnerName, false);
        }
    }

    private void EndGame(string winnerName, bool isDraw)
    {
        if (_gameEnded) return;
        _gameEnded = true;
        EndGameClientRpc(winnerName, isDraw);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestReturnToLobbyServerRpc()
    {
        MatchManager.Instance?.ReturnToLobby();
    }

    public void ResetGame()
    {
        if (!IsServer) return;
        _networkGameStarted.Value = false;
        _networkTimer.Value = 0f;
        _gameEnded = false;
        _tanks.Clear();
        _playerNames.Clear();
    }

    [ClientRpc]
    private void NotifyTankDeadClientRpc(ulong networkObjectId, ulong deadClientId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject netObj)) return;

        bool isMyTank = netObj.IsOwner;
        netObj.gameObject.SetActive(false);

        if (isMyTank)
            UIManager.Instance?.ShowPopup<GameOverPopup>(p => p.Setup(false));
    }

    [ClientRpc]
    private void EndGameClientRpc(string winnerName, bool isDraw)
    {
        _gameEnded = true;
        Time.timeScale = 0f;

        // -- Save record --
        string localName = PlayerPrefs.GetString("PlayerName", "");
        bool isWinner = !isDraw && winnerName == localName;

        if (isDraw) RecordManager.Instance?.RecordDraw();
        else if (isWinner) RecordManager.Instance?.RecordWin();
        else RecordManager.Instance?.RecordLoss();

        // -- Only show ResultPopup if not showing GameOverPopup --
        bool hasGameOver = UIManager.Instance?.IsPopupOpen<GameOverPopup>() ?? false;
        if (!hasGameOver)
            UIManager.Instance?.ShowPopup<ResultPopup>(p =>
                p.SetResult(isDraw ? "Draw" : winnerName));
    }

    [ClientRpc]
    private void NotifySpecialItemWarningClientRpc()
    {
        OnSpecialItemWarning?.Invoke();
    }

    [ClientRpc]
    private void NotifySpecialItemSpawnClientRpc()
    {
        OnSpecialItemSpawn?.Invoke();
    }

    public float GetRemainingTime() => Mathf.Max(0f, gameDuration - _networkTimer.Value);
    public int GetAliveCount() => _networkAliveCount.Value;
}