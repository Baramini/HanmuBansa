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

    private float _lastTimer = -1f;
    private int _lastAliveCount = -1;
    private float _lastChargeRatio = -1f;
    private float _lastHeatRatio = -1f;

    private System.Text.StringBuilder _sb = new System.Text.StringBuilder(8);

    public void SetTimer(float remainingSeconds)
    {
        if (Mathf.Abs(remainingSeconds - _lastTimer) < 1f) return;
        _lastTimer = remainingSeconds;

        int m = Mathf.FloorToInt(remainingSeconds / 60f);
        int s = Mathf.FloorToInt(remainingSeconds % 60f);

        _sb.Clear();
        _sb.Append(m.ToString("00"));
        _sb.Append(':');
        _sb.Append(s.ToString("00"));

        if (timerText != null)
            timerText.text = _sb.ToString();
    }

    public void SetAliveCount(int count)
    {
        if (count == _lastAliveCount) return;
        _lastAliveCount = count;

        if (aliveText != null)
            aliveText.text = $"{count}";
    }

    public void SetChargeRatio(float ratio)
    {
        if (Mathf.Abs(ratio - _lastChargeRatio) < 0.01f) return;
        _lastChargeRatio = ratio;

        chargeFilled.fillAmount = ratio;
    }

    public void SetHeatRatio(float ratio)
    {
        if (Mathf.Abs(ratio - _lastHeatRatio) < 0.01f) return;
        _lastHeatRatio = ratio;

        heatFilled.fillAmount = ratio;

        if (heatFilled != null)
        {
            Color targetColor = ratio < 0.3f ? Color.lightBlue
                              : ratio < 0.5f ? Color.yellow
                              : ratio < 0.8f ? Color.orange
                              : ratio < 0.8f ? Color.orangeRed
                              : Color.gray;

            if (heatFilled.color != targetColor)
                heatFilled.color = targetColor;
        }
    }
}