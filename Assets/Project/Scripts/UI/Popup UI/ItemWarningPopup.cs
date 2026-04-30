using UnityEngine;
using System.Collections;
using BrmnModules.UI;

public class ItemWarningPopup : PopupUI
{
    [SerializeField] private float duration = 4f;
    public override void Show()
    {
        base.Show();
        StartCoroutine(AutoCloseCoroutine());
    }

    private IEnumerator AutoCloseCoroutine()
    {
        yield return new WaitForSecondsRealtime(duration);
        UIManager.Instance?.HidePopup(this);
    }
}