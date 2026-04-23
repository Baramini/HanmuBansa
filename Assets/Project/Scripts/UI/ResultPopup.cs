using UnityEngine;
using TMPro;
using BrmnModules.UI;

public class ResultPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI resultText;

    // -- Override Show --
    public void SetResult(string winnerName)
    {
        if (resultText != null) resultText.text = winnerName == "" ? "DRAW!" : $"Player {winnerName} Win!!!";
    }
}