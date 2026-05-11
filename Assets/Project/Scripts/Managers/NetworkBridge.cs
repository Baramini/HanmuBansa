using UnityEngine;
using Unity.Netcode;
using BrmnModules.UI;

// Bridge NetworkBehaviour by MonoBehaviour
public class NetworkBridge : NetworkBehaviour
{
    public static NetworkBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [ClientRpc]
    public void ShowLoadingClientRpc()
    {
        UIManager.Instance?.HidePopup<LobbyPopup>();
        UIManager.Instance?.ShowPopup<LoadingPopup>();
    }

    [ClientRpc]
    public void OpenLobbyClientRpc()
    {
        Time.timeScale = 1f;

        TankSelectManager.Instance?.ResetSelections();

        UIManager.Instance?.HidePopup<LoadingPopup>();
        UIManager.Instance?.HidePopup<ResultPopup>();
        UIManager.Instance?.HidePopup<GameOverPopup>();
        UIManager.Instance?.ShowPopup<LobbyPopup>();
    }

    [ClientRpc]
    public void NotifyPlayerJoinedClientRpc()
    {
        var lobbyPopup = UIManager.Instance?.GetPopup<LobbyPopup>();
        if (lobbyPopup != null && lobbyPopup.gameObject.activeInHierarchy) lobbyPopup.RefreshPlayerSlots();
    }

    [ClientRpc]
    public void NotifyPlayerLeftClientRpc(ulong leftClientId)
    {
        var lobbyPopup = UIManager.Instance?.GetPopup<LobbyPopup>();
        if (lobbyPopup != null && lobbyPopup.gameObject.activeInHierarchy) lobbyPopup.OnPlayerLeftNotified(leftClientId);
    }
}