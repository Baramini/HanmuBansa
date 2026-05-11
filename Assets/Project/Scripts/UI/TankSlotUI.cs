using UnityEngine;
using UnityEngine.UI;

public class TankSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image tankImage;
    [SerializeField] private GameObject takenOverlay;

    public void SetSprite(Sprite sprite)
    {
        if (tankImage != null) tankImage.sprite = sprite;
    }

    public void Refresh(bool isTaken)
    {
        if (button != null) button.interactable = !isTaken;
        if (takenOverlay != null) takenOverlay.SetActive(isTaken);
        if (tankImage != null) tankImage.color = isTaken ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
    }

    public void SetButtonCallback(System.Action callback)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }
    }
}