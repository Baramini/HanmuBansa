using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Cinemachine;
using System.Collections.Generic;
using BrmnModules.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Multiplay")]
    [SerializeField] private Button spectateButton;
    [SerializeField] private Button nextPlayerButton;
    [SerializeField] private GameObject multiplayUI;

    [Header("Singleplay")]
    [SerializeField] private GameObject singleplayUI;

    private CinemachineCamera cinemachineCamera;
    private List<GameObject> aliveTanks = new();
    private int spectateIndex = 0;

    public override void Initialize()
    {
        base.Initialize();
        cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
    }

    public void Setup()
    {
        if (messageText != null) messageText.text = "You are dead";

        bool isSingle = GameMode.IsSingleplay;

        // Only multi can spectate
        if (spectateButton != null)
        {
            if (isSingle)
            {
                spectateButton.gameObject.SetActive(false);
            }
            else
            {
                RefreshAliveTanks();
                spectateButton.gameObject.SetActive(aliveTanks.Count > 1);
            }
        }
        if (nextPlayerButton != null) nextPlayerButton.gameObject.SetActive(false);

        multiplayUI?.SetActive(!isSingle);
        singleplayUI?.SetActive(isSingle);
    }

    // -- Multi play --
    public void OnSpectateButton()
    {
        if (aliveTanks.Count == 0) return;

        spectateIndex = 0;
        SetCameraTarget(aliveTanks[spectateIndex]);

        if (nextPlayerButton != null) nextPlayerButton.gameObject.SetActive(aliveTanks.Count > 1);
    }

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
        SceneManager.LoadScene(0);
    }

    // -- Single play --
    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        UIManager.Instance?.ShowPopup<LoadingPopup>();
        UIManager.Instance?.StartCoroutine(MainMenuCoroutine());
    }

    private IEnumerator MainMenuCoroutine()
    {
        _ = MatchManager.Instance?.LeaveRoomAsync();

        float elapsed = 0f;
        while (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && elapsed < 5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(0);
    }

    // -- others --
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