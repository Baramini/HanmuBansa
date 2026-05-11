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

    private float lastTimer = -1f;
    private int lastAliveCount = -1;
    private float lastChargeRatio = -1f;
    private float lastHeatRatio = -1f;

    private System.Text.StringBuilder sb = new System.Text.StringBuilder(8);

    public void SetTimer(float remainingSeconds)
    {
        if (Mathf.Abs(remainingSeconds - lastTimer) < 1f) return;
        lastTimer = remainingSeconds;

        int m = Mathf.FloorToInt(remainingSeconds / 60f);
        int s = Mathf.FloorToInt(remainingSeconds % 60f);

        sb.Clear();
        sb.Append(m.ToString("00"));
        sb.Append(':');
        sb.Append(s.ToString("00"));

        if (timerText != null) timerText.text = sb.ToString();
    }

    public void SetAliveCount(int count)
    {
        if (count == lastAliveCount) return;
        lastAliveCount = count;

        if (aliveText != null) aliveText.text = $"{count}";
    }

    public void SetChargeRatio(float ratio)
    {
        if (Mathf.Abs(ratio - lastChargeRatio) < 0.01f) return;

        lastChargeRatio = ratio;
        chargeFilled.fillAmount = ratio;
    }

    public void SetHeatRatio(float ratio)
    {
        if (Mathf.Abs(ratio - lastHeatRatio) < 0.01f) return;
        lastHeatRatio = ratio;

        heatFilled.fillAmount = ratio;

        if (heatFilled != null)
        {
            Color targetColor = ratio < 0.3f ? Color.lightBlue
                              : ratio < 0.5f ? Color.yellow
                              : ratio < 0.8f ? Color.orange
                              : ratio < 1.0f ? Color.orangeRed
                              : Color.gray;

            if (heatFilled.color != targetColor) heatFilled.color = targetColor;
        }
    }
}