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

        // -- Load saved name if exists --
        if (nameInput != null)
            nameInput.text = PlayerPrefs.GetString("PlayerName", "");

        okButton?.onClick.AddListener(OnConfirmButton);
    }

    // -- Confirm button --
    public void OnConfirmButton()
    {
        string name = nameInput?.text.Trim();

        // -- Validation --
        if (string.IsNullOrEmpty(name) || name.Length < MIN_LENGTH)
        {
            UIManager.Instance.ShowPopup<ErrorMessagePopup>(popup =>
                popup.ShowMessage($"Name must be at least {MIN_LENGTH} characters."));
            return;
        }

        // -- Save name --
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();

        // -- Proceed to multiplayer panel --
        UIManager.Instance?.HidePopup(this);
        UIManager.Instance?.ShowPopup<MultiplayPopup>();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }
}