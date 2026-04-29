using UnityEngine;
using TMPro;
using BrmnModules.UI;
using UnityEngine.UI;

public class ErrorMessagePopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private UnityEngine.Events.UnityAction closeAction;

    public void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        base.Show();
    }

    public void ShowMessage(string message, UnityEngine.Events.UnityAction onClose = null)
    {
        if (messageText != null)
            messageText.text = message;

        base.Show();

        if (onClose != null)
        {
            closeAction = onClose;
            closeButton.onClick.AddListener(closeAction);
        }
    }

    public override void OnCloseButton()
    {
        closeButton.onClick.RemoveListener(closeAction);
        closeAction = null;
        base.OnCloseButton();
    }
}