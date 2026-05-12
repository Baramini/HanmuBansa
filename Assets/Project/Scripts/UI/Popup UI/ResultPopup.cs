using UnityEngine;
using TMPro;
using BrmnModules.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Multiplay")]
    [SerializeField] private GameObject multiplayUI;

    [Header("Singleplay")]
    [SerializeField] private GameObject singleplayUI;

    public override void Initialize()
    {
        base.Initialize();
        RefreshButtons();
    }

    public void SetResult(string resultLabel)
    {
        if (resultText != null) 
        {
            if (GameMode.IsSingleplay) resultText.text = resultLabel;
            else resultText.text = resultLabel == "" ? "DRAW!" : $"Player {resultLabel} Win!!!";
        }

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        bool isSingle = GameMode.IsSingleplay;

        multiplayUI?.SetActive(!isSingle);
        singleplayUI?.SetActive(isSingle);
    }

    // -- Multi play -- 

    public void OnReturnToLobbyButton()
    {
        Time.timeScale = 1f;
        UIManager.Instance?.ShowPopup<LoadingPopup>();
        GameManager.Instance?.RequestReturnToLobbyServerRpc();
    }

    public void OnLeaveButton()
    {
        UIManager.Instance?.ShowPopup<LoadingPopup>();
        _ = LeaveAndLoadMainMenuAsync();
    }

    private async System.Threading.Tasks.Task LeaveAndLoadMainMenuAsync()
    {
        await System.Threading.Tasks.Task.Delay(500);
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
        while (NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsListening
            && elapsed < 5f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(0);
    }
}