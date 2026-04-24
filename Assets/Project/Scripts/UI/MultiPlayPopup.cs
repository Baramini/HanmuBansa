using UnityEngine;
using TMPro;
using BrmnModules.UI;
using UnityEngine.UI;
using Mono.Cecil.Cil;

public class MultiplayPopup : PopupUI
{
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private Button roomCreateButton;
    [SerializeField] private Button roomJoinButton;
    [SerializeField] private Button randomMatchButton;

    public override void Initialize()
    {
        base.Initialize();

        roomCreateButton?.onClick.AddListener(OnCreateRoomButton);
        roomJoinButton?.onClick.AddListener(OnJoinRoomButton);
        randomMatchButton?.onClick.AddListener(OnRandomMatchButton);

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnRoomCodeGenerated += OnRoomCodeGenerated;
            MatchManager.Instance.OnMatchError += OnMatchError;
            MatchManager.Instance.OnMatchStarted += OnMatchStarted;
        }
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    public void OnCreateRoomButton()
    {
        if (roomCodeInput == null) return;
        string code = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            UIManager.Instance?.ShowPopup<ErrorMessagePopup>(popup =>
                popup.ShowMessage("Please enter a room code."));
            return;
        }

        UIManager.Instance?.ShowPopup<StatusMessagePopup>(popup =>
            popup.ShowMessage("Creating room...", 10f));

        _ = MatchManager.Instance?.CreateRoomAsync();
    }

    public void OnJoinRoomButton()
    {
        if (roomCodeInput == null) return;
        string code = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            UIManager.Instance?.ShowPopup<ErrorMessagePopup>(popup =>
                popup.ShowMessage("Please enter a room code."));
            return;
        }

        UIManager.Instance?.ShowPopup<StatusMessagePopup>(popup =>
            popup.ShowMessage("Joining room...", 10f));

        _ = MatchManager.Instance?.JoinByCodeAsync(code);
    }

    public void OnRandomMatchButton()
    {
        UIManager.Instance?.ShowPopup<StatusMessagePopup>(popup =>
            popup.ShowMessage("Finding match...", 10f));

        _ = MatchManager.Instance?.AutoMatchAsync();
    }

    private void OnRoomCodeGenerated(string code)
    {
        // -- Update existing status message --
        UIManager.Instance?.GetPopup<StatusMessagePopup>()
            ?.ShowMessage($"Room Code: {code}", 10f);
    }

    private void OnMatchError(string msg)
    {
        // -- Close status first, then show error --
        UIManager.Instance?.GetPopup<StatusMessagePopup>()?.OnClose();
        UIManager.Instance?.ShowPopup<ErrorMessagePopup>(popup => popup.ShowMessage(msg));
    }

    private void OnMatchStarted()
    {
        UIManager.Instance?.GetPopup<StatusMessagePopup>()
            ?.ShowMessage("Connected!", 1.5f, onClose: () =>
            {
                // -- TODO: transition to lobby panel --
            });
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