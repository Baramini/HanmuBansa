using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections.Generic;
using BrmnModules.UI;

public class GameOverPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button spectateButton;
    [SerializeField] private Button nextPlayerButton;

    private CinemachineCamera _cinemachineCamera;
    private List<GameObject> _aliveTanks = new();
    private int _spectateIndex = 0;
    private bool _isSpectating = false;

    public override void Initialize()
    {
        base.Initialize();
        _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
    }

    public void Setup(bool isWinner)
    {
        if (messageText != null)
            messageText.text = "You are dead";

        // -- Collect alive tanks --
        RefreshAliveTanks();

        // -- Show spectate button only if there are alive players --
        if (spectateButton != null)
            spectateButton.gameObject.SetActive(_aliveTanks.Count > 0);

        if (nextPlayerButton != null)
            nextPlayerButton.gameObject.SetActive(false);
    }

    // -- Spectate button --
    public void OnSpectateButton()
    {
        if (_aliveTanks.Count == 0) return;

        _isSpectating = true;
        _spectateIndex = 0;

        SetCameraTarget(_aliveTanks[_spectateIndex]);
    }

    // -- Next player button --
    public void OnNextPlayerButton()
    {
        RefreshAliveTanks();
        if (_aliveTanks.Count == 0) return;

        _spectateIndex = (_spectateIndex + 1) % _aliveTanks.Count;
        SetCameraTarget(_aliveTanks[_spectateIndex]);
    }

    public void OnLeaveButton()
    {
        // -- Show loading then return to main menu --
        UIManager.Instance?.ShowPopup<LoadingPopup>();

        _ = LeaveAndLoadMainMenuAsync();
    }

    private async System.Threading.Tasks.Task LeaveAndLoadMainMenuAsync()
    {
        await System.Threading.Tasks.Task.Delay(1000);

        await MatchManager.Instance?.LeaveRoomAsync();

        // -- Load main menu --
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void SetCameraTarget(GameObject tank)
    {
        if (_cinemachineCamera == null || tank == null) return;
        _cinemachineCamera.Follow = tank.transform;
    }

    private void RefreshAliveTanks()
    {
        _aliveTanks.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            GameObject playerObj = client.Value.PlayerObject?.gameObject;
            if (playerObj == null) continue;

            TankHealth health = playerObj.GetComponentInChildren<TankHealth>();
            if (health != null && !health.IsDead)
                _aliveTanks.Add(playerObj);
        }
    }
}