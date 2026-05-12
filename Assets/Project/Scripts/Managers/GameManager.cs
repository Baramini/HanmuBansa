using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using BrmnModules.UI;
using BrmnModules.Audio;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public event System.Action OnGameStarted;
    public bool IsGameStarted => networkGameStarted.Value;

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 300f;
    [SerializeField] private float specialItemTime = 180f;

    private NetworkVariable<float> networkTimer = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> networkAliveCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> networkGameStarted = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event System.Action<float> OnTimerChanged;
    public event System.Action<int> OnAliveCountChanged;
    public event System.Action OnSpecialItemSpawn;
    //public event System.Action<string> OnGameEnd;

    private Dictionary<ulong, string> playerNames = new();
    private List<TankHealth> tanks = new();
    private bool specialItemWarned = false;
    private bool specialItemSpawned = false;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        networkTimer.OnValueChanged += (prev, cur) =>
        {
            OnTimerChanged?.Invoke(cur);
            UIManager.Instance?.GetPersistent<HUD>()?.SetTimer(gameDuration - cur);
        };
        networkAliveCount.OnValueChanged += (prev, cur) =>
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
        UIManager.Instance?.GetPersistent<HUD>()?.SetTimer(gameDuration - networkTimer.Value);
        UIManager.Instance?.GetPersistent<HUD>()?.SetAliveCount(networkAliveCount.Value);
    }

    public void SetPlayer(TankHealth tank, ulong clientId, string playerName)
    {
        if (!IsServer) return;

        tanks.Add(tank);
        playerNames[clientId] = playerName;
        networkAliveCount.Value = tanks.Count;
        tank.OnDead += () => OnTankDead(tank);
    }

    public void StartGame()
    {
        if (!IsServer) return;
        if (networkGameStarted.Value) return;

        networkGameStarted.Value = true;
        networkTimer.Value = 0f;
        gameEnded = false;
        specialItemWarned = false;
        specialItemSpawned = false;

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
        // Only server process
        if (!IsServer) return;

        if (!networkGameStarted.Value) return;

        if (gameEnded) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        networkTimer.Value += Time.deltaTime;

        if (!specialItemWarned && networkTimer.Value >= specialItemTime - 30f)
        {
            specialItemWarned = true;
            NotifySpecialItemWarningClientRpc();
        }

        if (!specialItemSpawned && networkTimer.Value >= specialItemTime)
        {
            specialItemSpawned = true;
            NotifySpecialItemSpawnClientRpc();
        }

        if (networkTimer.Value >= gameDuration) EndGame("", true);
    }

    private void OnTankDead(TankHealth deadTank)
    {
        if (!IsServer) return;

        networkAliveCount.Value--;

        ulong networkObjectId = deadTank.GetComponent<NetworkObject>().NetworkObjectId;
        NotifyTankDeadClientRpc(networkObjectId);

        if (networkAliveCount.Value <= 1)
        {
            // Find winner
            TankHealth winnerTank = tanks.Find(t => !t.IsDead);
            string winnerName = "";
            if (winnerTank != null)
            {
                NetworkObject winnerNetObj = winnerTank.GetComponent<NetworkObject>();
                if (playerNames.TryGetValue(winnerNetObj.OwnerClientId, out string name)) winnerName = name;
            }

            EndGame(winnerName, false);
        }
    }

    private void EndGame(string winnerName, bool isDraw)
    {
        if (gameEnded) return;
        gameEnded = true;
        EndGameClientRpc(winnerName, isDraw);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestReturnToLobbyServerRpc()
    {
        MatchManager.Instance?.ReturnToLobby();
    }

    public void ResetGame()
    {
        if (!IsServer) return;
    
        foreach (TankHealth tank in tanks)
        {
            if (tank == null) continue;
    
            NetworkObject netObj = tank.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
        }
    
        tanks.Clear();
        playerNames.Clear();
    
        networkGameStarted.Value = false;
        networkTimer.Value = 0f;
        gameEnded = false;
    }

    [ClientRpc]
    private void NotifyTankDeadClientRpc(ulong networkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj)) return;

        bool isMyTank = netObj.IsOwner && netObj.IsPlayerObject;
        netObj.gameObject.SetActive(false);

        if (isMyTank) UIManager.Instance?.ShowPopup<GameOverPopup>(p => p.Setup());
    }

    [ClientRpc]
    private void EndGameClientRpc(string winnerName, bool isDraw)
    {
        gameEnded = true;
        Time.timeScale = 0f;
        
        string localName = PlayerPrefs.GetString("PlayerName", "");
        bool isWinner = !isDraw && winnerName == localName;

        if (GameMode.IsSingleplay)
        {
            bool hasGameOver = UIManager.Instance?.IsPopupOpen<GameOverPopup>() ?? false;
            if (!hasGameOver)
            {
                string resultLabel = isDraw ? "Draw" : "Last standing!";
                UIManager.Instance?.ShowPopup<ResultPopup>(p => p.SetResult(resultLabel));
            }
        }
        else
        {
            // Save record only multi
            if (isDraw) RecordManager.Instance?.RecordDraw();
            else if (isWinner) RecordManager.Instance?.RecordWin();
            else RecordManager.Instance?.RecordLoss();
    
            bool hasGameOver = UIManager.Instance?.IsPopupOpen<GameOverPopup>() ?? false;
            if (!hasGameOver) UIManager.Instance?.ShowPopup<ResultPopup>(p => p.SetResult(isDraw ? "Draw" : winnerName));
        }
    }

    [ClientRpc]
    private void NotifySpecialItemWarningClientRpc()
    {
        UIManager.Instance?.ShowPopup<ItemWarningPopup>();
        AudioManager.Instance?.PlaySFX("Warning");
    }

    [ClientRpc]
    private void NotifySpecialItemSpawnClientRpc()
    {
        OnSpecialItemSpawn?.Invoke();
    }

    public float GetRemainingTime() => Mathf.Max(0f, gameDuration - networkTimer.Value);
    public int GetAliveCount() => networkAliveCount.Value;
}