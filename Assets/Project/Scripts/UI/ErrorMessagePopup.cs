using UnityEngine;
using TMPro;
using BrmnModules.UI;

public class ErrorMessagePopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI messageText;

    public void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        base.Show();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }
}