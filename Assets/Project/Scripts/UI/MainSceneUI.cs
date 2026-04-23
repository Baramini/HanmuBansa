using UnityEngine;
using BrmnModules.UI;

public class MainSceneUI : PersistentUI
{
    public void OnMultiplayButton()
    {
        UIManager.Instance?.ShowPopup<MultiplayPopup>();
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