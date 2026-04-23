using UnityEngine;
using TMPro;
using BrmnModules.UI;

public class MultiplayPopup : PopupUI
{
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TextMeshProUGUI statusText;

    public override void Initialize()
    {
        base.Initialize();

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnRoomCodeGenerated += OnRoomCodeGenerated;
            MatchManager.Instance.OnMatchError += OnMatchError;
            MatchManager.Instance.OnMatchStarted += OnMatchStarted;
        }
    }

    // -- Create room button --
    public void OnCreateRoomButton()
    {
        SetStatus("Creating room...");
        _ = MatchManager.Instance?.CreateRoomAsync();
    }

    // -- Join by code button --
    public void OnJoinButton()
    {
        if (roomCodeInput == null) return;
        string code = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code)) return;

        SetStatus("Joining...");
        _ = MatchManager.Instance?.JoinByCodeAsync(code);
    }

    // -- Random match button --
    public void OnRandomMatchButton()
    {
        SetStatus("Finding match...");
        _ = MatchManager.Instance?.AutoMatchAsync();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    private void OnRoomCodeGenerated(string code) => SetStatus($"Code: {code}");
    private void OnMatchError(string msg) => SetStatus($"Error: {msg}");
    private void OnMatchStarted() => SetStatus("Connected!");

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    private void OnDestroy()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnRoomCodeGenerated -= OnRoomCodeGenerated;
            MatchManager.Instance.OnMatchError -= OnMatchError;
            MatchManager.Instance.OnMatchStarted -= OnMatchStarted;
        }
    }
}