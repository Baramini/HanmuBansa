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

// Manages room creation, joining, and auto-matching.
// Uses UGS Lobby + Relay for P2P connection without dedicated server.
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    // -- Max players per room (keeps within UGS free tier) --
    private const int MAX_PLAYERS = 4;

    // -- Lobby data key for storing Relay join code --
    public string CurrentLobbyCode => _currentLobby?.LobbyCode ?? "";
    public Unity.Services.Lobbies.Models.Lobby CurrentLobby => _currentLobby;

    private const string RELAY_CODE_KEY = "RelayCode";
    private Lobby _currentLobby;
    private Coroutine _heartbeatCoroutine;
    private bool _isLeavingIntentionally = false;

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

    // -- Events for UI --
    public event System.Action<string> OnMatchError;
    public event System.Action<string> OnRoomCodeGenerated;
    public event System.Action OnMatchStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void Start()
    {
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    // -------------------------------------------------------
    // -- CREATE ROOM (Host) ---------------------------------
    // -------------------------------------------------------
    public async Task CreateRoomAsync()
    {
        try
        {
            // -- 1. Create Relay allocation (max 3 connections + host = 4 players) --
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS - 1);

            // -- 2. Get join code for Relay --
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // -- 3. Create Lobby with Relay code stored in data --
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { RELAY_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                },
                Player = new Player(data: GetLocalPlayerData())
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "HanmuRoom",
                MAX_PLAYERS,
                options
            );

            // -- 4. Start heartbeat to keep lobby alive --
            _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());

            // -- 5. Configure Relay transport and start Host --
            string connectionType = Application.isEditor ? "udp" : "dtls";
            RelayServerData relayData = GetHostRelayData(allocation, connectionType);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();

            // -- 6. Notify UI with lobby code --
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
            // -- 1. Join Lobby by code --
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player(data: GetLocalPlayerData())
            };
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.ToUpper(), joinOptions);

            // -- 2. Get Relay join code from Lobby data --
            string relayCode = _currentLobby.Data[RELAY_CODE_KEY].Value;

            // -- 3. Join Relay allocation --
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            // -- 4. Configure Relay transport and start Client --
            string connectionType = Application.isEditor ? "udp" : "dtls";
            RelayServerData relayData = GetClientRelayData(joinAllocation, connectionType);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
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
    // -- AUTO MATCH (Quick Join) ----------------------------
    // -------------------------------------------------------
    public async Task AutoMatchAsync()
    {
        try
        {
            // -- 1. Try to find an existing lobby with open slots --
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
            _currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);

            // -- 2. Found a lobby: join via Relay --
            string relayCode = _currentLobby.Data[RELAY_CODE_KEY].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            string connectionType = Application.isEditor ? "udp" : "dtls";
            RelayServerData relayData = GetClientRelayData(joinAllocation, connectionType);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            OnMatchStarted?.Invoke();
            Debug.Log("Auto match: joined existing room.");
        }
        catch (LobbyServiceException)
        {
            // -- 3. No lobby found: create a new one and wait --
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
    // -- SELECT MAP -----------------------------------------
    // -------------------------------------------------------

    private const string MAP_KEY = "SelectedMap";

    // -- Update lobby data with selected map --
    public async Task SetSelectedMapAsync(int mapIndex)
    {
        if (_currentLobby == null) return;

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                // -- Keep existing relay code --
                { RELAY_CODE_KEY, new DataObject(
                    DataObject.VisibilityOptions.Public,
                    _currentLobby.Data[RELAY_CODE_KEY].Value) },
                // -- Add map index --
                { MAP_KEY, new DataObject(
                    DataObject.VisibilityOptions.Member,
                    mapIndex.ToString()) }
            }
            };

            _currentLobby = await LobbyService.Instance
                .UpdateLobbyAsync(_currentLobby.Id, options);

            Debug.Log($"Map set to index: {mapIndex}");
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
    // -- LEAVE ROOM -----------------------------------------
    // -------------------------------------------------------
    public async Task LeaveRoomAsync()
    {
        _isLeavingIntentionally = true;

        try
        {
            // -- Reset game state --
            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();
                
            // -- Stop heartbeat --
            if (_heartbeatCoroutine != null)
                StopCoroutine(_heartbeatCoroutine);

            if (_currentLobby == null) return;

            string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;

            // -- Host deletes lobby, client just leaves --
            if (_currentLobby.HostId == playerId)
                await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
            else
                await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerId);

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
    // -- HEARTBEAT ------------------------------------------
    // -------------------------------------------------------
    // Lobby auto-expires after 30s without heartbeat.
    // Send ping every 15s to keep it alive.
    private IEnumerator HeartbeatCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(15f);

            if (_currentLobby != null)
                LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
        }
    }

    //public override void OnNetworkSpawn()
    //{
    //    //if (!IsServer) return;

    //    // -- Watch for player connections --
    //    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedForGame;
    //}

    //public override void OnNetworkDespawn()
    //{
    //    //if (!IsServer) return;

    //    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedForGame;
    //}

    private void OnClientStopped(bool isHost)
    {
        if (isHost) return;

        // -- Ignore if leaving intentionally --
        if (_isLeavingIntentionally)
        {
            _isLeavingIntentionally = false;
            return;
        }

        _ = ForceLeaveAsync();
    }

    private async System.Threading.Tasks.Task ForceLeaveAsync()
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
                Debug.LogError($"ForceLeave lobby error: {e.Message}");
            }
            _currentLobby = null;
        }

        if (NetworkManager.Singleton.IsConnectedClient ||
            NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        UIManager.Instance?.ShowPopup<ErrorMessagePopup>(p =>
            p.ShowMessage("Host has disconnected."));

        UIManager.Instance?.HidePopup<LobbyPopup>();
    }

    // -- Called by host start button --
    public void RequestStartGame()
    {
        if (!IsServer) return;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCount < 2)
        {
            Debug.Log("Need at least 2 players to start.");
            return;
        }

        // -- Check all players have selected a tank --
        if (!TankSelectManager.Instance.AllPlayersSelected())
        {
            Debug.Log("Not all players have selected a tank.");
            return;
        }

        ShowLoadingClientRpc();
        StartCoroutine(LoadSceneAfterDelayCoroutine());
    }

    private IEnumerator LoadSceneAfterDelayCoroutine()
    {
        // -- Wait for loading panel to appear --
        yield return new WaitForSeconds(1.5f);

        int mapIndex = GetSelectedMapIndex();
        int sceneIndex = mapIndex + 1;
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnMapSceneLoaded;
        NetworkManager.Singleton.SceneManager.LoadScene(
            System.IO.Path.GetFileNameWithoutExtension(
                UnityEngine.SceneManagement.SceneUtility
                    .GetScenePathByBuildIndex(sceneIndex)),
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    [ClientRpc]
    private void ShowLoadingClientRpc()
    {
        UIManager.Instance?.ShowPopup<LoadingPopup>();
        UIManager.Instance?.HidePopup<LobbyPopup>();
    }

    private void OnMapSceneLoaded(ulong clientId, string sceneName,
    UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        Debug.Log($"OnMapSceneLoaded clientId:{clientId} sceneName:{sceneName}");

        if (!IsServer) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnMapSceneLoaded;

        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnEachClientLoaded;
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnEachClientLoaded(ulong clientId, string sceneName,
    UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        Debug.Log($"Client {clientId} finished loading {sceneName}");
        Debug.Log($"Total connected: {NetworkManager.Singleton.ConnectedClients.Count}");
    }

    private void OnLoadEventCompleted(string sceneName,
    UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
    System.Collections.Generic.List<ulong> clientsCompleted,
    System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        var spawnManager = FindFirstObjectByType<SpawnManager>();
        spawnManager?.SpawnAllPlayers();
        GameManager.Instance?.StartGame();
    }

    private RelayServerData GetHostRelayData(Allocation allocation, string connectionType)
    {
        // -- Convert allocation data to RelayServerData manually --
        return new RelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.ConnectionData,
            allocation.ConnectionData,   // -- Host: hostConnectionData = connectionData --
            allocation.Key,
            connectionType == "dtls"     // -- isSecure --
        );
    }

    private RelayServerData GetClientRelayData(JoinAllocation joinAllocation, string connectionType)
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

    public override void OnDestroy()
    {
        // -- Release Relay/Lobby on app quit --
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;

        if (NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsListening)
        {
            base.OnDestroy();
            return;
        }

        _ = LeaveRoomAsync();
        base.OnDestroy();
    }
}