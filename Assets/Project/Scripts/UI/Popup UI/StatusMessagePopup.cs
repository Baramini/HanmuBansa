using UnityEngine;
using TMPro;
using System;
using System.Collections;
using BrmnModules.UI;

public class StatusMessagePopup : PopupUI
{
    [SerializeField] private TextMeshProUGUI messageText;
    
    private Coroutine autoCloseCoroutine;

    public override void OnCloseButton() { }

    public void ShowMessage(string message, float duration, Action onClose = null)
    {
        if (messageText != null) messageText.text = message;
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);

        base.Show();

        autoCloseCoroutine = StartCoroutine(AutoCloseCoroutine(duration, onClose));
    }

    public void OnClose()
    {
        if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
        
        UIManager.Instance?.HidePopup(this);
        autoCloseCoroutine = null;
    }

    private IEnumerator AutoCloseCoroutine(float duration, Action onClose)
    {
        yield return new WaitForSecondsRealtime(duration);
        onClose?.Invoke();
        
        UIManager.Instance?.HidePopup(this);
        autoCloseCoroutine = null;
    }
}