using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BrmnModules.UI;

public class TankSelectPopup : PopupUI
{
    [SerializeField] private List<TankSlotUI> tankSlots;

    public event System.Action<Sprite, int> OnTankSelected;

    public override void Initialize()
    {
        base.Initialize();

        for (int i = 0; i < tankSlots.Count; i++) tankSlots[i]?.SetSprite(TankSpriteContainer.Instance?.GetSprite(i));
        for (int i = 0; i < tankSlots.Count; i++)
        {
            int index = i;
            tankSlots[i]?.SetButtonCallback(() => OnSlotButton(index));
        }

        if (TankSelectManager.Instance != null) TankSelectManager.Instance.OnSelectionChanged += RefreshSlots;
    }

    public override void Show()
    {
        if (TankSelectManager.Instance != null)
        {
            TankSelectManager.Instance.OnSelectionChanged -= RefreshSlots;
            TankSelectManager.Instance.OnSelectionChanged += RefreshSlots;
        }

        base.Show();
        RefreshSlots();
    }

    public void OnSlotButton(int index)
    {
        if (TankSelectManager.Instance?.IsTankTaken(index) ?? false) return;

        TankSelectManager.Instance?.SelectTankServerRpc(index);

        Sprite sprite = (index < TankSpriteContainer.Instance?.Count) ? TankSpriteContainer.Instance?.GetSprite(index) : null;
        OnTankSelected?.Invoke(sprite, index);

        UIManager.Instance?.HidePopup(this);
    }

    private void RefreshSlots()
    {
        if (tankSlots == null) return;

        for (int i = 0; i < tankSlots.Count; i++)
        {
            bool isTaken = TankSelectManager.Instance?.IsTankTaken(i) ?? false;
            tankSlots[i]?.Refresh(isTaken);
        }
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    public override void Hide(bool fade)
    {
        if (TankSelectManager.Instance != null) TankSelectManager.Instance.OnSelectionChanged -= RefreshSlots;

        base.Hide(false);
    }
}