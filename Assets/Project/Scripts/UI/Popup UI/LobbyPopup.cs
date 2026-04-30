using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;
using BrmnModules.UI;
using Unity.Services.Lobbies;

public class LobbyPopup : PopupUI
{
    [Header("Room Info")]
    [SerializeField] private TextMeshProUGUI roomCodeText;

    [Header("Player List")]
    [SerializeField] private PlayerSlotUI[] playerSlots;

    [Header("Player Log")]
    [SerializeField] private LobbyLog lobbyLog;

    [Header("Bottom Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private Button leaveButton;

    [Header("Map Select")]
    [SerializeField] private Image mapPreviewImage;
    [SerializeField] private GameObject mapControlButtons;
    [SerializeField] private List<Sprite> mapSprites;
    [SerializeField] private TextMeshProUGUI mapName;

    private int _currentMapIndex = 0;
    private float _lobbyPollTimer = 0f;
    private const float LOBBY_POLL_INTERVAL = 2f;

    private Dictionary<ulong, int> _clientSlotMap = new();

    public override void Initialize()
    {
        base.Initialize();

        var tankSelectPanel = UIManager.Instance?.GetPopup<TankSelectPopup>();
        if (tankSelectPanel != null)
            tankSelectPanel.OnTankSelected += OnTankSelectConfirmed;

        if (TankSelectManager.Instance != null)
            TankSelectManager.Instance.OnSelectionChanged += OnSelectionChanged;
    }

    public override void Show()
    {
        base.Show();
        RefreshAll();
    }

    private void Update()
    {
        // -- Only non-host clients poll for lobby changes --
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsHost) return;
        if (MatchManager.Instance?.CurrentLobby == null) return;

        _lobbyPollTimer += Time.deltaTime;
        if (_lobbyPollTimer >= LOBBY_POLL_INTERVAL)
        {
            _lobbyPollTimer = 0f;
            _ = PollLobbyAsync();
        }
    }

    private async System.Threading.Tasks.Task PollLobbyAsync()
    {
        if (MatchManager.Instance?.CurrentLobby == null) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        try
        {
            var lobby = await LobbyService.Instance
                .GetLobbyAsync(MatchManager.Instance.CurrentLobby.Id);

            if (lobby.Data == null) return;

            bool isRandom = lobby.Data.ContainsKey("IsRandomMap") &&
                            bool.Parse(lobby.Data["IsRandomMap"].Value);

            if (lobby.Data.ContainsKey("SelectedMap"))
            {
                int mapIndex = int.Parse(lobby.Data["SelectedMap"].Value);

                if (isRandom)
                {
                    // -- Show random sprite regardless of actual map index --
                    _currentMapIndex = mapSprites.Count - 1;
                }
                else
                {
                    _currentMapIndex = mapIndex;
                }

                UpdateMapPreview(_currentMapIndex);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log($"Lobby no longer exists. Stopping poll. ({e.Message})");
            _lobbyPollTimer = float.MaxValue;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PollLobby error: {e.Message}");
        }
    }

    private void RefreshAll()
    {
        RefreshRoomCode();
        ClearAllSlots();
        RefreshPlayerSlots();
        RefreshStartButton();
        RefreshMapSelect();
    }

    // -------------------------------------------------------
    // -- Room Code ------------------------------------------
    // -------------------------------------------------------

    private void RefreshRoomCode()
    {
        if (roomCodeText == null) return;
        string code = MatchManager.Instance?.CurrentLobbyCode ?? "";
        roomCodeText.text = string.IsNullOrEmpty(code) ? "" : $"Room Code: {code}";
    }

    // -------------------------------------------------------
    // -- Player Slots ---------------------------------------
    // -------------------------------------------------------

    private void ClearAllSlots()
    {
        _clientSlotMap.Clear();
        foreach (var slot in playerSlots)
            slot?.SetEmpty();
    }

    private void OnPlayerJoined(ulong clientId)
    {
        string name = GetPlayerName(clientId);
        lobbyLog?.LogJoined(name);

        RefreshPlayerSlots();
        RefreshStartButton();
    }

    private void OnPlayerLeft(ulong clientId)
    {
        string name = GetPlayerName(clientId);
        lobbyLog?.LogLeft(name);

        // -- Clear that player's slot --
        if (_clientSlotMap.TryGetValue(clientId, out int slotIndex))
        {
            playerSlots[slotIndex]?.SetEmpty();
            _clientSlotMap.Remove(clientId);
        }

        RefreshStartButton();
    }

    public void OnPlayerLeftNotified(ulong leftClientId)
    {
        if (_clientSlotMap.TryGetValue(leftClientId, out int slotIndex))
        {
            playerSlots[slotIndex]?.SetEmpty();
            _clientSlotMap.Remove(leftClientId);
        }
        RefreshStartButton();
    }

    private string GetPlayerName(ulong clientId)
    {
        // -- TODO: get from Lobby player data --
        // -- Fallback: use clientId --
        return clientId == NetworkManager.Singleton.LocalClientId
            ? PlayerPrefs.GetString("PlayerName", "Me")
            : $"Player {clientId}";
    }

    public void RefreshPlayerSlots()
    {
        var lobby = MatchManager.Instance?.CurrentLobby;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (_clientSlotMap.ContainsKey(clientId)) continue;

            int emptySlot = -1;
            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (!_clientSlotMap.ContainsValue(i))
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot == -1) break;

            string playerName = GetPlayerData(lobby, clientId, "PlayerName", $"Player {clientId}");
            string wins = GetPlayerData(lobby, clientId, "Wins", "0");
            string losses = GetPlayerData(lobby, clientId, "Losses", "0");
            string draws = GetPlayerData(lobby, clientId, "Draws", "0");
            string record = $"Win: {wins}  Lose: {losses}  Draw: {draws}";

            bool isHost = NetworkManager.Singleton.IsHost
                          && clientId == NetworkManager.Singleton.LocalClientId;

            playerSlots[emptySlot]?.SetPlayer(playerName, record, isHost);
            _clientSlotMap[clientId] = emptySlot;
        }
    }

    // -- Called when tank selection changes --
    private void OnSelectionChanged()
    {
        foreach (var kvp in _clientSlotMap)
        {
            int tankIndex = TankSelectManager.Instance
                ?.GetSelectionByClientId(kvp.Key) ?? -1;

            playerSlots[kvp.Value]?.SetTank(
                TankSpriteContainer.Instance?.GetSprite(tankIndex));
        }

        RefreshStartButton();
    }

    private void OnTankSelectConfirmed(Sprite sprite, int index)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (_clientSlotMap.TryGetValue(localId, out int slotIndex))
            playerSlots[slotIndex]?.SetTank(sprite);
    }

    public void OnTankSelectionPopup()
    {
        UIManager.Instance?.ShowPopup<TankSelectPopup>();
    }

    // -------------------------------------------------------
    // -- SELECT MAP -----------------------------------------
    // -------------------------------------------------------

    private void RefreshMapSelect()
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        if (mapControlButtons != null)
            mapControlButtons.SetActive(isHost);

        UpdateMapPreview(_currentMapIndex);
    }

    private void UpdateMapPreview(int index)
    {
        if (mapPreviewImage == null || mapSprites == null) return;
        if (index < 0 || index >= mapSprites.Count) return;

        mapPreviewImage.sprite = mapSprites[index];
        mapName.text = mapSprites[index].name;
    }

    public void OnMapNextButton()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        _currentMapIndex = (_currentMapIndex + 1) % mapSprites.Count;
        UpdateMapPreview(_currentMapIndex);

        bool isRandom = IsRandomIndex(_currentMapIndex);
        int sendIndex = isRandom
            ? Random.Range(0, mapSprites.Count - 1)
            : _currentMapIndex;

        _ = MatchManager.Instance?.SetSelectedMapAsync(sendIndex, isRandom);
    }

    public void OnMapPrevButton()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        _currentMapIndex = (_currentMapIndex - 1 + mapSprites.Count) % mapSprites.Count;
        UpdateMapPreview(_currentMapIndex);

        bool isRandom = IsRandomIndex(_currentMapIndex);
        int sendIndex = isRandom
            ? Random.Range(0, mapSprites.Count - 1)
            : _currentMapIndex;

        _ = MatchManager.Instance?.SetSelectedMapAsync(sendIndex, isRandom);
    }

    private bool IsRandomIndex(int index) => index == mapSprites.Count - 1;

    // -------------------------------------------------------
    // -- Start Button ---------------------------------------
    // -------------------------------------------------------

    private void RefreshStartButton()
    {
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        int playerCount = NetworkManager.Singleton?.ConnectedClients.Count ?? 0;
        bool allSelected = TankSelectManager.Instance?.AllPlayersSelected() ?? false;
        bool canStart = playerCount >= 2 && allSelected;

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isHost);
            startButton.interactable = canStart;
        }

        if (startButtonText != null)
        {
            if (playerCount < 2)
                startButtonText.text = $"Waiting... ({playerCount}/2)";
            else if (!allSelected)
                startButtonText.text = "Waiting for tank selection...";
            else
                startButtonText.text = $"Start ({playerCount} players)";
        }
    }

    public void OnStartButton()
    {
        MatchManager.Instance?.RequestStartGame();
    }

    // -------------------------------------------------------
    // -- Leave ----------------------------------------------
    // -------------------------------------------------------

    public void OnLeaveButton()
    {
        _ = MatchManager.Instance?.LeaveRoomAsync();
        ClearAllSlots();
        UIManager.Instance?.HidePopup(this);
    }

    public override void OnCloseButton()
    {
        OnLeaveButton();
    }

    public override void Hide()
    {
        var tankSelectPanel = UIManager.Instance?.GetPopup<TankSelectPopup>();
        if (tankSelectPanel != null)
            tankSelectPanel.OnTankSelected -= OnTankSelectConfirmed;

        if (TankSelectManager.Instance != null)
            TankSelectManager.Instance.OnSelectionChanged -= OnSelectionChanged;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
        }

        _lobbyPollTimer = float.MaxValue;
        lobbyLog?.Clear();
        base.Hide();
    }

    // -------------------------------------------------------
    // -- Helpers --------------------------------------------
    // -------------------------------------------------------

    private string GetPlayerData(Unity.Services.Lobbies.Models.Lobby lobby,
        ulong clientId, string key, string fallback)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (key == "PlayerName") return PlayerPrefs.GetString("PlayerName", fallback);
            if (key == "Wins") return RecordManager.Instance?.Wins.ToString() ?? fallback;
            if (key == "Losses") return RecordManager.Instance?.Losses.ToString() ?? fallback;
            if (key == "Draws") return RecordManager.Instance?.Draws.ToString() ?? fallback;
        }

        if (lobby == null) return fallback;

        foreach (var player in lobby.Players)
            if (player.Data != null && player.Data.ContainsKey(key))
                return player.Data[key].Value;

        return fallback;
    }
}