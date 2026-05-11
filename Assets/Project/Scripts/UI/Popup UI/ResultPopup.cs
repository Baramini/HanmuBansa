using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrmnModules.UI;

public class ResultPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI resultText;

    public void SetResult(string winnerName)
    {
        if (resultText != null) resultText.text = winnerName == "" ? "DRAW!" : $"Player {winnerName} Win!!!";
    }

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
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}