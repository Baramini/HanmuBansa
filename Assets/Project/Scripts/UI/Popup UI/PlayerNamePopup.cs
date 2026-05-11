using UnityEngine;
using TMPro;
using BrmnModules.UI;
using UnityEngine.UI;

public class PlayerNamePopup : PopupUI
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button okButton;

    private const int MIN_LENGTH = 2;

    public override void Initialize()
    {
        base.Initialize();

        if (nameInput != null) nameInput.text = PlayerPrefs.GetString("PlayerName", "");

        okButton?.onClick.AddListener(OnConfirmButton);
    }

    public void OnConfirmButton()
    {
        string name = nameInput?.text.Trim();

        if (string.IsNullOrEmpty(name) || name.Length < MIN_LENGTH)
        {
            UIManager.Instance.ShowPopup<ErrorMessagePopup>(popup => popup.ShowMessage($"Name must be at least {MIN_LENGTH} characters."));
            return;
        }

        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();

        UIManager.Instance?.HidePopup(this);
        UIManager.Instance?.ShowPopup<MultiplayPopup>();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }
}