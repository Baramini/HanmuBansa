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

    private CinemachineCamera cinemachineCamera;
    private List<GameObject> aliveTanks = new();
    private int spectateIndex = 0;
    private bool isSpectating = false;

    public override void Initialize()
    {
        base.Initialize();
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
    }

    public void Setup(bool isWinner)
    {
        if (messageText != null) messageText.text = "You are dead";

        RefreshAliveTanks();

        // Show spectate button only game not end
        if (spectateButton != null) spectateButton.gameObject.SetActive(aliveTanks.Count > 0);

        if (nextPlayerButton != null) nextPlayerButton.gameObject.SetActive(false);
    }

    public void OnSpectateButton()
    {
        if (aliveTanks.Count == 0) return;

        isSpectating = true;
        spectateIndex = 0;

        SetCameraTarget(aliveTanks[spectateIndex]);
    }

    // Next player button
    public void OnNextPlayerButton()
    {
        RefreshAliveTanks();
        if (aliveTanks.Count == 0) return;

        spectateIndex = (spectateIndex + 1) % aliveTanks.Count;
        SetCameraTarget(aliveTanks[spectateIndex]);
    }

    public void OnLeaveButton()
    {
        UIManager.Instance?.ShowPopup<LoadingPopup>();

        _ = LeaveAndLoadMainMenuAsync();
    }

    private async System.Threading.Tasks.Task LeaveAndLoadMainMenuAsync()
    {
        await System.Threading.Tasks.Task.Delay(1000);

        await MatchManager.Instance?.LeaveRoomAsync();

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void SetCameraTarget(GameObject tank)
    {
        if (cinemachineCamera == null || tank == null) return;
        cinemachineCamera.Follow = tank.transform;
    }

    private void RefreshAliveTanks()
    {
        aliveTanks.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            GameObject playerObj = client.Value.PlayerObject?.gameObject;
            if (playerObj == null) continue;

            TankHealth health = playerObj.GetComponentInChildren<TankHealth>();
            if (health != null && !health.IsDead) aliveTanks.Add(playerObj);
        }
    }
}