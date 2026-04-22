using BrmnModules.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : PersistentUI
{
    [SerializeField] private Image chargeFilled;
    [SerializeField] private Image heatFilled;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI aliveText;

    public void SetChargeRatio(float ratio) => chargeFilled.fillAmount = ratio;
    public void SetHeatRatio(float ratio) => heatFilled.fillAmount = ratio;
    public void SetTimer(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{m:00} : {s:00}";
    }
    public void SetAliveCount(int count) => aliveText.text = $"Alive: {count}";

    public void SetOverheated(bool isOverheated)
    {
        // TODO: Add Overheat UI expression
    }
}