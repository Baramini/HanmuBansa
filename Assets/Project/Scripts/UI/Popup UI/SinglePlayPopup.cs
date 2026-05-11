using UnityEngine;
using BrmnModules.UI;
using UnityEngine.UI;

public class SingleplayPopup : PopupUI
{
    [SerializeField] private Button[] difficultyButtons;
    public override void Initialize()
    {
        base.Initialize();

        difficultyButtons[0]?.onClick.AddListener(OnEasyButton);
        difficultyButtons[1]?.onClick.AddListener(OnNormalButton);
        difficultyButtons[2]?.onClick.AddListener(OnHardButton);
    }

    public void OnEasyButton() => StartSingleplay(Difficulty.Easy);
    public void OnNormalButton() => StartSingleplay(Difficulty.Normal);
    public void OnHardButton() => StartSingleplay(Difficulty.Hard);

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    private void StartSingleplay(Difficulty difficulty)
    {
        // TODO: Feat singleplay
    }
}

public enum Difficulty { Easy, Normal, Hard }