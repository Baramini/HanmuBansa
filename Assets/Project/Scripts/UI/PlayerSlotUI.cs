using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI recordText;
    [SerializeField] private Image tankThumbnail;
    [SerializeField] private GameObject hostBadge;

    public void SetPlayer(string playerName, string record, bool isHost)
    {
        if (emptyState != null) emptyState.SetActive(false);

        if (nameText != null) nameText.text = playerName;
        if (recordText != null) recordText.text = record;
        if (hostBadge != null) hostBadge.SetActive(isHost);
    }

    public void SetTank(Sprite sprite)
    {
        if (tankThumbnail == null) return;
        tankThumbnail.sprite = sprite;
        tankThumbnail.enabled = sprite != null;
    }

    public void SetEmpty()
    {
        if (emptyState != null) emptyState.SetActive(true);

        if (nameText != null) nameText.text = "";
        if (recordText != null) recordText.text = "";
        if (hostBadge != null) hostBadge.SetActive(false);
        
        SetTank(null);
    }
}