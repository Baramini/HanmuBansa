using UnityEngine;
using TMPro;
using System;
using System.Collections;
using BrmnModules.UI;

public class StatusMessagePopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI messageText;

    private Coroutine _autoCloseCoroutine;

    public override void OnCloseButton() { }

    // -- Show with duration and optional callback on close --
    public void ShowMessage(string message, float duration, Action onClose = null)
    {
        if (messageText != null)
            messageText.text = message;

        // -- Cancel previous coroutine if running --
        if (_autoCloseCoroutine != null)
            StopCoroutine(_autoCloseCoroutine);

        base.Show();

        _autoCloseCoroutine = StartCoroutine(AutoCloseCoroutine(duration, onClose));
    }

    public void OnClose()
    {
        if (_autoCloseCoroutine != null)
            StopCoroutine(_autoCloseCoroutine);
        
        UIManager.Instance?.HidePopup(this);
        _autoCloseCoroutine = null;
    }

    private IEnumerator AutoCloseCoroutine(float duration, Action onClose)
    {
        yield return new WaitForSecondsRealtime(duration);
        onClose?.Invoke();
        
        UIManager.Instance?.HidePopup(this);
        _autoCloseCoroutine = null;
    }
}