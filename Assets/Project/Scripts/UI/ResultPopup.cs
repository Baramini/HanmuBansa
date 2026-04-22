using BrmnModules.UI;
using TMPro;
using UnityEngine;

public class ResultPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI winnerText;

    public void Show(string winnerName)
    {
        winnerText.text = winnerName == "Draw" ? "DRAW!" : $"Winner: {winnerName}";
        base.Show();
    }
}