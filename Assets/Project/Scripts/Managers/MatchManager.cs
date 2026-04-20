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

// Manages room creation, joining, and auto-matching.
// Uses UGS Lobby + Relay for P2P connection without dedicated server.
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    // -- Max players per room (keeps within UGS free tier) --
    private const int MAX_PLAYERS = 4;

    // -- Lobby data key for storing Relay join code --
    private const string RELAY_CODE_KEY = "RelayCode";

    private Lobby _currentLobby;
    private Coroutine _heartbeatCoroutine;

    // -- Events for UI --
    public event System.Action<string> OnMatchError;
    public event System.Action<string> OnRoomCodeGenerated;
    public event System.Action OnMatchStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
                }
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
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode.ToUpper());

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
    // -- LEAVE ROOM -----------------------------------------
    // -------------------------------------------------------
    public async Task LeaveRoomAsync()
    {
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

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // -- Watch for player connections --
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedForGame;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedForGame;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedForGame;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedForGame;
    }

    private void OnClientConnectedForGame(ulong clientId)
    {
        if (!IsServer) return;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"Player connected. Total: {playerCount}");

        // -- No auto start: host manually starts game --
    }

    private void OnClientDisconnectedForGame(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log("Player disconnected.");
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

        // -- Spawn players first, then start game --
        FindFirstObjectByType<SpawnManager>()?.SpawnAllPlayers();
        GameManager.Instance.StartGame();
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
            joinAllocation.HostConnectionData,  // -- Client: hostConnectionData ���� ���� --
            joinAllocation.Key,
            connectionType == "dtls"
        );
    }

    public override void OnDestroy()
    {
        // -- Release Relay/Lobby on app quit --
        _ = LeaveRoomAsync();
    }
}