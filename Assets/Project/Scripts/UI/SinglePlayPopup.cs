using UnityEngine;
using BrmnModules.UI;

public class SingleplayPopup : PopupUI
{
    // -- Difficulty buttons --
    public void OnEasyButton() => StartSingleplay(Difficulty.Easy);
    public void OnNormalButton() => StartSingleplay(Difficulty.Normal);
    public void OnHardButton() => StartSingleplay(Difficulty.Hard);

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    private void StartSingleplay(Difficulty difficulty)
    {
        // -- TODO: start singleplay with selected difficulty --
        Debug.Log($"Starting singleplay: {difficulty}");
    }
}

public enum Difficulty { Easy, Normal, Hard }