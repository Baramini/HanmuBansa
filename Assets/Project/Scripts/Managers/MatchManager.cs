using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Collections;
using System.Collections.Generic;
using BrmnModules.UI;

// Manages room creation, joining, auto-matching, and scene transitions.
// Uses UGS Lobby + Relay for P2P connection without dedicated server.
// Inherits MonoBehaviour (not NetworkBehaviour) to avoid NGO lifecycle issues.
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    private const string MAIN_MENU_SCENE = "Main";
    private const string MAP_WAREHOUSE = "Map_Spaceship_Warehouse";
    private const string MAP_ARENA = "Map_Spaceship_Arena";

    private const int MAX_PLAYERS = 4;
    private const string RELAY_CODE_KEY = "RelayCode";
    private const string MAP_KEY = "SelectedMap";

    private Lobby _currentLobby = null;
    private Coroutine _heartbeatCoroutine = null;
    private bool _isLeavingIntentionally = false;
    private bool _isReturningToLobby = false;

    public string CurrentLobbyCode => _currentLobby?.LobbyCode ?? "";
    public Lobby CurrentLobby => _currentLobby;

    public event System.Action<string> OnMatchError;
    public event System.Action<string> OnRoomCodeGenerated;
    public event System.Action OnMatchStarted;

    // -------------------------------------------------------
    // -- Lifecycle ------------------------------------------
    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SceneManager != null);

        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkBridge.Instance?.NotifyPlayerJoinedClientRpc();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkBridge.Instance?.NotifyPlayerLeftClientRpc(clientId);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;

            if (NetworkManager.Singleton.SceneManager != null)
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted
                    -= OnSceneLoadCompleted;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            _ = LeaveRoomAsync();
    }

    // -------------------------------------------------------
    // -- Scene Load Completed -------------------------------
    // -------------------------------------------------------

    private void OnSceneLoadCompleted(string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        Debug.Log($"OnSceneLoadCompleted: {sceneName}");

        if (sceneName == MAIN_MENU_SCENE)
        {
            if (_currentLobby != null && _isReturningToLobby)
            {
                _isReturningToLobby = false;
                OpenLobbyOnAllClients();
            }
        }
        else
        {
            // -- Map scene loaded --
            if (!NetworkManager.Singleton.IsServer) return;

            StartCoroutine(SpawnAndStartCoroutine());
        }
    }

    private IEnumerator SpawnAndStartCoroutine()
    {
        // -- Wait for SpawnManager and GameManager to be ready --
        SpawnManager spawnManager = null;
        yield return new WaitUntil(() =>
        {
            spawnManager = FindFirstObjectByType<SpawnManager>();
            return spawnManager != null && GameManager.Instance != null;
        });

        Debug.Log("Spawning players and starting game...");
        spawnManager.SpawnAllPlayers();
        GameManager.Instance.StartGame();
    }

    // -- Cannot use ClientRpc (not NetworkBehaviour) --
    // -- Use NetworkManager to find MatchManagerNetworkBridge instead --
    private void OpenLobbyOnAllClients()
    {
        NetworkBridge.Instance?.OpenLobbyClientRpc();
    }

    // -------------------------------------------------------
    // -- CREATE ROOM (Host) ---------------------------------
    // -------------------------------------------------------

    public async Task CreateRoomAsync()
    {
        try
        {
            Allocation allocation = await RelayService.Instance
                .CreateAllocationAsync(MAX_PLAYERS - 1);

            string relayCode = await RelayService.Instance
                .GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { RELAY_CODE_KEY, new DataObject(
                        DataObject.VisibilityOptions.Public, relayCode) }
                },
                Player = new Player(data: GetLocalPlayerData())
            };

            _currentLobby = await LobbyService.Instance
                .CreateLobbyAsync("HanmuRoom", MAX_PLAYERS, options);

            _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());

            string connectionType = Application.isEditor ? "udp" : "dtls";
            RelayServerData relayData = GetHostRelayData(allocation, connectionType);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();

            OnRoomCodeGenerated?.Invoke(_currentLobby.LobbyCode);
            Debug.Log($"Room created. Code: {_currentLobby.LobbyCode}");
        }
        catch (System.Exception e)
        {
            OnMatchError?.Invoke($"Failed to create room: {e.Message}");
            Debug.LogError(e);
        }
    }

    // -------------------------------------------------------
    // -- JOIN BY CODE (Client) ------------------------------
    // -------------------------------------------------------

    public async Task JoinByCodeAsync(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player(data: GetLocalPlayerData())
            };

            _currentLobby = await LobbyService.Instance
                .JoinLobbyByCodeAsync(lobbyCode.ToUpper(), joinOptions);

            string relayCode = _currentLobby.Data[RELAY_CODE_KEY].Value;
            JoinAllocation joinAllocation = await RelayService.Instance
                .JoinAllocationAsync(relayCode);

            string connectionType = Application.isEditor ? "udp" : "dtls";
            RelayServerData relayData = GetClientRelayData(joinAllocation, connectionType);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            OnMatchStarted?.Invoke();
            Debug.Log($"Joined room: {lobbyCode}");
        }
        catch (System.Exception e)
        {
            OnMatchError?.Invoke($"Failed to join room: {e.Message}");
            Debug.LogError(e);
        }
    }

    // -------------------------------------------------------
    // -- AUTO MATCH -----------------------------------------
    // -------------------------------------------------------

    public async Task AutoMatchAsync()
    {
        try
        {
            _currentLobby = await LobbyService.Instance
                .QuickJoinLobbyAsync(new QuickJoinLobbyOptions());

            string relayCode = _currentLobby.Data[RELAY_CODE_KEY].Value;
            JoinAllocation joinAllocation = await RelayService.Instance
                .JoinAllocationAsync(relayCode);

            string connectionType = Application.isEditor ? "udp" : "dtls";
            RelayServerData relayData = GetClientRelayData(joinAllocation, connectionType);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            OnMatchStarted?.Invoke();
            Debug.Log("Auto match: joined existing room.");
        }
        catch (LobbyServiceException)
        {
            Debug.Log("Auto match: no room found. Creating new room...");
            await CreateRoomAsync();
        }
        catch (System.Exception e)
        {
            OnMatchError?.Invoke($"Auto match failed: {e.Message}");
            Debug.LogError(e);
        }
    }

    // -------------------------------------------------------
    // -- START GAME -----------------------------------------
    // -------------------------------------------------------

    public void RequestStartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCount < 2)
        {
            Debug.Log("Need at least 2 players to start.");
            return;
        }

        if (!TankSelectManager.Instance.AllPlayersSelected())
        {
            Debug.Log("Not all players have selected a tank.");
            return;
        }

        NetworkBridge.Instance?.ShowLoadingClientRpc();

        StartCoroutine(LoadSceneAfterDelayCoroutine());
    }

    private IEnumerator LoadSceneAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        int mapIndex = GetSelectedMapIndex();
        string sceneName = mapIndex == 0 ? MAP_WAREHOUSE : MAP_ARENA;

        Debug.Log($"Loading scene: {sceneName}");

        NetworkManager.Singleton.SceneManager.LoadScene(
            sceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    // -------------------------------------------------------
    // -- RETURN TO LOBBY ------------------------------------
    // -------------------------------------------------------

    public void ReturnToLobby()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Time.timeScale = 1f;
        _isReturningToLobby = true;

        NetworkManager.Singleton.SceneManager.LoadScene(
            MAIN_MENU_SCENE,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    // -------------------------------------------------------
    // -- LEAVE ROOM -----------------------------------------
    // -------------------------------------------------------

    public async Task LeaveRoomAsync()
    {
        _isLeavingIntentionally = true;

        try
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();

            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);

            if (_currentLobby == null) return;

            string playerId = Unity.Services.Authentication
                .AuthenticationService.Instance.PlayerId;

            if (_currentLobby.HostId == playerId)
                await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            else
                await LobbyService.Instance
                    .RemovePlayerAsync(_currentLobby.Id, playerId);

            _currentLobby = null;
            NetworkManager.Singleton.Shutdown();

            Debug.Log("Left room.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Leave room error: {e.Message}");
        }
    }

    // -------------------------------------------------------
    // -- FORCE LEAVE ----------------------------------------
    // -------------------------------------------------------

    private void OnClientStopped(bool isHost)
    {
        if (isHost) return;

        if (_isLeavingIntentionally)
        {
            _isLeavingIntentionally = false;
            return;
        }

        _ = ForceLeaveAsync();
    }

    private async Task ForceLeaveAsync()
    {
        if (_currentLobby != null)
        {
            try
            {
                string playerId = Unity.Services.Authentication
                    .AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance
                    .RemovePlayerAsync(_currentLobby.Id, playerId);
            }
            catch (LobbyServiceException)
            {
                Debug.Log("Lobby already deleted by host.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ForceLeave error: {e.Message}");
            }
            _currentLobby = null;
        }

        if (NetworkManager.Singleton.IsConnectedClient ||
            NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Time.timeScale = 1f;

        UIManager.Instance?.HidePopup<GameOverPopup>();
        UIManager.Instance?.HidePopup<ResultPopup>();
        UIManager.Instance?.HidePopup<LobbyPopup>();

        string message = GameManager.Instance != null && GameManager.Instance.IsGameStarted
            ? "Host has left the game.\nReturning to main menu."
            : "Host has disconnected.\nReturning to main menu.";

        UIManager.Instance?.ShowPopup<ErrorMessagePopup>(p =>
            p.ShowMessage(message, onClose: () =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(0)));
    }

    // -------------------------------------------------------
    // -- MAP SELECT -----------------------------------------
    // -------------------------------------------------------

    public async Task SetSelectedMapAsync(int mapIndex)
    {
        if (_currentLobby == null) return;

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { RELAY_CODE_KEY, new DataObject(
                        DataObject.VisibilityOptions.Public,
                        _currentLobby.Data[RELAY_CODE_KEY].Value) },
                    { MAP_KEY, new DataObject(
                        DataObject.VisibilityOptions.Member,
                        mapIndex.ToString()) }
                }
            };

            _currentLobby = await LobbyService.Instance
                .UpdateLobbyAsync(_currentLobby.Id, options);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SetSelectedMap error: {e.Message}");
        }
    }

    public int GetSelectedMapIndex()
    {
        if (_currentLobby?.Data == null) return 0;
        if (!_currentLobby.Data.ContainsKey(MAP_KEY)) return 0;
        return int.Parse(_currentLobby.Data[MAP_KEY].Value);
    }

    // -------------------------------------------------------
    // -- HEARTBEAT ------------------------------------------
    // -------------------------------------------------------

    private IEnumerator HeartbeatCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(15f);
            if (_currentLobby != null)
                LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
    }

    // -------------------------------------------------------
    // -- Relay Helpers --------------------------------------
    // -------------------------------------------------------

    private RelayServerData GetHostRelayData(Allocation allocation, string connectionType)
    {
        return new RelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData,
            allocation.Key,
            connectionType == "dtls"
        );
    }

    private RelayServerData GetClientRelayData(JoinAllocation joinAllocation,
        string connectionType)
    {
        return new RelayServerData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData,
            joinAllocation.Key,
            connectionType == "dtls"
        );
    }

    // -------------------------------------------------------
    // -- Player Data ----------------------------------------
    // -------------------------------------------------------

    private Dictionary<string, PlayerDataObject> GetLocalPlayerData()
    {
        return new Dictionary<string, PlayerDataObject>
        {
            { "PlayerName", new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                PlayerPrefs.GetString("PlayerName", "Player")) },
            { "Wins", new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                RecordManager.Instance?.Wins.ToString() ?? "0") },
            { "Losses", new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                RecordManager.Instance?.Losses.ToString() ?? "0") },
            { "Draws", new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                RecordManager.Instance?.Draws.ToString() ?? "0") },
        };
    }
}