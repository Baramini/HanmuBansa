using UnityEngine;
using TMPro;
using System.Collections;
using BrmnModules.UI;

public class LoadingPopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private float dotInterval = 0.5f;

    private Coroutine dotCoroutine;

    public override void Show()
    {
        base.Show();
        dotCoroutine = StartCoroutine(AnimateDotsCoroutine());
    }

    public override void Hide()
    {
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);

        base.Hide();
    }

    private IEnumerator AnimateDotsCoroutine()
    {
        int dots = 0;
        while (true)
        {
            dots = (dots % 3) + 1;
            if (loadingText != null) loadingText.text = "Loading" + new string('.', dots);

            yield return new WaitForSecondsRealtime(dotInterval);
        }
    }
}