using UnityEngine;
using UnityEngine.SceneManagement;
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
using BrmnModules.Pool;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    private const string MAIN_MENU_SCENE = "Main";
    private const string MAP_WAREHOUSE = "Map_Spaceship_Warehouse";
    private const string MAP_ARENA = "Map_Spaceship_Arena";

    private const int MAX_PLAYERS = 4;
    private const string RELAY_CODE_KEY = "RelayCode";

    private Lobby currentLobby = null;
    private Coroutine heartbeatCoroutine = null;
    private bool isLeavingIntentionally = false;
    private bool isReturningToLobby = false;

    public string CurrentLobbyCode => currentLobby?.LobbyCode ?? "";
    public Lobby CurrentLobby => currentLobby;

    public event System.Action<string> OnMatchError;
    public event System.Action<string> OnRoomCodeGenerated;
    public event System.Action OnMatchStarted;

    // -- Lifecycle --
    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
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
            NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null);

        SubscribeNetworkEvents();
    }

    private void SubscribeNetworkEvents()
    {
        // To avoid duplication, cancel current subscription first
        UnsubscribeNetworkEvents();

        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // SceneManager can be recreated later
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
    }

    private void UnsubscribeNetworkEvents()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (GameMode.IsSingleplay) return;

        NetworkBridge.Instance?.NotifyPlayerJoinedClientRpc();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (GameMode.IsSingleplay) return;
        if (!NetworkManager.Singleton.IsListening) return;

        NetworkBridge.Instance?.NotifyPlayerLeftClientRpc(clientId);
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();

        if (GameMode.IsSingleplay) return;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) _ = LeaveRoomAsync();
    }

    // -- Scene Load Completed --
    private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == MAIN_MENU_SCENE)
        {
            if (currentLobby != null && isReturningToLobby)
            {
                isReturningToLobby = false;
                OpenLobbyOnAllClients();
            }
        }
        else
        {
            if (!NetworkManager.Singleton.IsServer) return;
            StartCoroutine(SpawnAndStartCoroutine());
        }
    }

    private IEnumerator SpawnAndStartCoroutine()
    {
        SpawnManager spawnManager = null;
        yield return new WaitUntil(() =>
        {
            spawnManager = FindFirstObjectByType<SpawnManager>();
            return spawnManager != null && GameManager.Instance != null;
        });

        spawnManager.SpawnAllPlayers();
        GameManager.Instance.StartGame();
    }

    private void OpenLobbyOnAllClients()
    {
        NetworkBridge.Instance?.OpenLobbyClientRpc();
    }

    // -- CREATE ROOM (Host) --
    public async Task CreateRoomAsync()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS - 1);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { RELAY_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Public, relayCode) }
                },
                Player = new Player(data: GetLocalPlayerData())
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync("HanmuRoom", MAX_PLAYERS, options);
            heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());

            SetupTransport();
            Unity.Services.Multiplayer.RelayProtocol connectionType = GetConnectionType();
            // UDP required the creation of a separate RelayServerData object
            if (connectionType == Unity.Services.Multiplayer.RelayProtocol.UDP)
            {
                var relayServerData = new RelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.ConnectionData,
                    allocation.ConnectionData,
                    allocation.Key,
                    false
                );
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
                NetworkManager.Singleton.StartHost();
            }

            // Resubscribe why regenerated after StartHost
            SubscribeNetworkEvents();

            OnRoomCodeGenerated?.Invoke(currentLobby.LobbyCode);
        }
        catch (System.Exception e)
        {
            OnMatchError?.Invoke($"Failed to create room: {e.Message}");
            Debug.LogError(e);
        }
    }

    // -- JOIN BY CODE (Client) --
    public async Task JoinByCodeAsync(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions
            {
                Player = new Player(data: GetLocalPlayerData())
            };

            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.ToUpper(), joinOptions);

            string relayCode = currentLobby.Data[RELAY_CODE_KEY].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            SetupTransport();
            Unity.Services.Multiplayer.RelayProtocol connectionType = GetConnectionType();
            if (connectionType == Unity.Services.Multiplayer.RelayProtocol.UDP)
            {
                var relayServerData = new RelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData,
                    joinAllocation.Key,
                    false
                );
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, connectionType));
                NetworkManager.Singleton.StartClient();
            }

            // Resubscribe why regenerated after StartClient
            SubscribeNetworkEvents();

            OnMatchStarted?.Invoke();
        }
        catch (System.Exception e)
        {
            OnMatchError?.Invoke($"Failed to join room: {e.Message}");
            Debug.LogError(e);
        }
    }

    // -- AUTO MATCH --
    public async Task AutoMatchAsync()
    {
        try
        {
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions());

            string relayCode = currentLobby.Data[RELAY_CODE_KEY].Value;
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);

            SetupTransport();
            Unity.Services.Multiplayer.RelayProtocol connectionType = GetConnectionType();
            if (connectionType == Unity.Services.Multiplayer.RelayProtocol.UDP)
            {
                var relayServerData = new RelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.ConnectionData,
                    joinAllocation.HostConnectionData,
                    joinAllocation.Key,
                    false
                );
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartClient();
            }
            else
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, connectionType));
                NetworkManager.Singleton.StartClient();
            }

            SubscribeNetworkEvents();

            OnMatchStarted?.Invoke();
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

    // -- START GAME --
    public void RequestStartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCount < 2) return;
        if (!TankSelectManager.Instance.AllPlayersSelected()) return;

        NetworkBridge.Instance?.ShowLoadingClientRpc();
        StartCoroutine(LoadSceneAfterDelayCoroutine());
    }

    private IEnumerator LoadSceneAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(1.5f);

        int mapIndex = GetSelectedMapIndex();
        string sceneName = mapIndex == 0 ? MAP_WAREHOUSE : MAP_ARENA;

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // -- RETURN TO LOBBY --
    public void ReturnToLobby()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ResetGame();
        
        if (!GameMode.IsSingleplay) isReturningToLobby = true;

        NetworkManager.Singleton.SceneManager.LoadScene(MAIN_MENU_SCENE, LoadSceneMode.Single);
    }

    // -- LEAVE ROOM --
    public async Task LeaveRoomAsync()
    {
        isLeavingIntentionally = true;

        try
        {
            if (GameManager.Instance != null) GameManager.Instance.ResetGame();
            if (heartbeatCoroutine != null) StopCoroutine(heartbeatCoroutine);

            PoolManager.Instance?.ClearAllPools();
            UIManager.Instance?.ResetPopupState();

            if (GameMode.IsSingleplay)
            {
                GameMode.SetMultiplay();
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
                return;
            }

            if (currentLobby == null) return;

            string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
            if (currentLobby.HostId == playerId) await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            else await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);

            currentLobby = null;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Leave room error: {e.Message}");
        }
    }

    // -- FORCE LEAVE --
    private void OnClientStopped(bool isHost)
    {
        if (isLeavingIntentionally)
        {
            isLeavingIntentionally = false;
            return;
        }

        if (isHost) return;

        _ = ForceLeaveAsync();
    }

    private async Task ForceLeaveAsync()
    {
        if (currentLobby != null)
        {
            try
            {
                string playerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, playerId);
            }
            catch (LobbyServiceException)
            {
                Debug.Log("Lobby already deleted by host.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ForceLeave error: {e.Message}");
            }
            currentLobby = null;
        }

        if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();

        Time.timeScale = 1f;

        UIManager.Instance?.HidePopup<GameOverPopup>();
        UIManager.Instance?.HidePopup<ResultPopup>();
        UIManager.Instance?.HidePopup<LobbyPopup>();

        string message = GameManager.Instance != null && GameManager.Instance.IsGameStarted
            ? "Host has left the game.\nReturning to main menu."
            : "Host has disconnected.\nReturning to main menu.";

        UIManager.Instance?.ShowPopup<ErrorMessagePopup>(p =>
            p.ShowMessage(message, onClose: () => SceneManager.LoadScene(0)));
    }

    // -- SINGLEPLAY --
    public void RequestStartSingleplay()
    {
        StartCoroutine(WaitUGSAndStartSingleplay());
    }
    
    private IEnumerator WaitUGSAndStartSingleplay()
    {
        float elapsed = 0f;
        while (UGSManager.Instance != null && !UGSManager.Instance.IsInitialized && elapsed < 15f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    
        if (UGSManager.Instance != null && !UGSManager.Instance.IsInitialized)
        {
            Debug.LogError("UGS Timeout(Singleplay)");
            UIManager.Instance?.HidePopup<LoadingPopup>();
            yield break;
        }
    
        // WebGL does not support local server sockets → Must go through Relay
        // A problem that arose because I developed the multiplayer mode first and then tried to develop the single-player
        if (Application.platform == RuntimePlatform.WebGLPlayer) _ = StartSingleplayWithRelayAsync();
        else StartSingleplay();
    }
    
    // WebGL-only
    private async Task StartSingleplayWithRelayAsync()
    {
        try
        {
            // Because 'Single' play
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
    
            GameMode.SetSingleplay();
    
            SetupTransport();
            Unity.Services.Multiplayer.RelayProtocol connectionType = GetConnectionType();
            if (connectionType == Unity.Services.Multiplayer.RelayProtocol.UDP)
            {
                var relayServerData = new RelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.ConnectionData,
                    allocation.ConnectionData,
                    allocation.Key,
                    false
                );
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>()
                    .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
                NetworkManager.Singleton.StartHost();
            }
    
            SubscribeNetworkEvents();
    
            int mapIndex = SingleplaySettings.MapIndex;
            string sceneName = mapIndex == 0 ? MAP_WAREHOUSE : MAP_ARENA;
    
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Relay failure(Singleplay): {e.Message}");
            GameMode.SetMultiplay();
            UIManager.Instance?.HidePopup<LoadingPopup>();
        }
    }
    
    // Not WebGL
    public void StartSingleplay()
    {
        GameMode.SetSingleplay();
        SetupTransport();
        NetworkManager.Singleton.StartHost();
        SubscribeNetworkEvents();
    
        int mapIndex = SingleplaySettings.MapIndex;
        string sceneName = mapIndex == 0 ? MAP_WAREHOUSE : MAP_ARENA;
    
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // -- MAP SELECT --
    private const string MAP_KEY = "SelectedMap";
    private const string MAP_RANDOM_KEY = "IsRandomMap";

    public async Task SetSelectedMapAsync(int mapIndex, bool isRandom = false)
    {
        if (currentLobby == null) return;

        try
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject>
                {
                    { RELAY_CODE_KEY, new DataObject(
                        DataObject.VisibilityOptions.Public,
                        currentLobby.Data[RELAY_CODE_KEY].Value) },
                    { MAP_KEY, new DataObject(
                        DataObject.VisibilityOptions.Member,
                        mapIndex.ToString()) },
                    { MAP_RANDOM_KEY, new DataObject(
                        DataObject.VisibilityOptions.Member,
                        isRandom.ToString()) }
                }
            };

            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SetSelectedMap error: {e.Message}");
        }
    }

    public bool GetIsRandomMap()
    {
        if (currentLobby?.Data == null) return false;
        if (!currentLobby.Data.ContainsKey(MAP_RANDOM_KEY)) return false;
        return bool.Parse(currentLobby.Data[MAP_RANDOM_KEY].Value);
    }

    public int GetSelectedMapIndex()
    {
        if (currentLobby?.Data == null) return 0;
        if (!currentLobby.Data.ContainsKey(MAP_KEY)) return 0;
        return int.Parse(currentLobby.Data[MAP_KEY].Value);
    }

    // -- Others --
    private IEnumerator HeartbeatCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(15f);
            if (currentLobby != null) LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
    }

    private Dictionary<string, PlayerDataObject> GetLocalPlayerData()
    {
        return new Dictionary<string, PlayerDataObject>
        {
            { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                PlayerPrefs.GetString("PlayerName", "Player")) },
            { "Wins", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                RecordManager.Instance?.Wins.ToString() ?? "0") },
            { "Losses", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                RecordManager.Instance?.Losses.ToString() ?? "0") },
            { "Draws", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                RecordManager.Instance?.Draws.ToString() ?? "0") },
        };
    }

    private void SetupTransport()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        bool useWebSocket = Application.platform == RuntimePlatform.WebGLPlayer;
        transport.UseWebSockets = useWebSocket;
    }

    private Unity.Services.Multiplayer.RelayProtocol GetConnectionType()
    {
        Unity.Services.Multiplayer.RelayProtocol type;
        if (Application.isEditor) type = Unity.Services.Multiplayer.RelayProtocol.UDP;
        else if (Application.platform == RuntimePlatform.WebGLPlayer) type = Unity.Services.Multiplayer.RelayProtocol.WSS;
        else type = Unity.Services.Multiplayer.RelayProtocol.DTLS;

        return type;
    }
}