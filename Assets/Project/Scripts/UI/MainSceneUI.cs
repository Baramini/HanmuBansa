using UnityEngine;
using BrmnModules.UI;

public class MainSceneUI : PersistentUI
{
    public void OnMultiplayButton()
    {
        // -- Check if name already saved --
        string savedName = PlayerPrefs.GetString("PlayerName", "");

        if (string.IsNullOrEmpty(savedName) || savedName.Length < 2)
        {
            UIManager.Instance?.ShowPopup<PlayerNamePopup>();
        }
        else
        {
            UIManager.Instance?.ShowPopup<MultiplayPopup>();
        }
    }

    public void OnSingleplayButton()
    {
        UIManager.Instance?.ShowPopup<SingleplayPopup>();
    }

    public void OnSettingsButton()
    {
        UIManager.Instance?.ShowPopup<SettingsPopup>();
    }
}