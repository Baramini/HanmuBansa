using UnityEngine;
using BrmnModules.UI;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class SingleplayPopup : PopupUI
{
    [Header("Difficulty")]
    [SerializeField] private List<Toggle> diffToggles;
    private Difficulty currentDiff;

    [Space]
    [Header("Map")]
    [SerializeField] private Image mapPreviewImage;
    [SerializeField] private List<Sprite> mapSprites;
    [SerializeField] private TextMeshProUGUI mapName;
    private int currentMapIndex;

    [Space] 
    [Header("Player Count")]
    [SerializeField] private TextMeshProUGUI countText;
    private int currentPlayerCount;
    private int minPlayers = 1;
    private int maxPlayers = 3;

    [Space]
    [Header("Buttons")]
    [SerializeField] private Button startButton;

    private void Start()
    {
        OnDiffChanged(1);
    }

    public override void Initialize()
    {
        base.Initialize();

        currentMapIndex = 0;
        currentPlayerCount = 1;
        UpdateMapPreview();
        UpdatePlayerCount();
    }

    private Color normal = Color.white;
    private Color selected = Color.yellow;
    public void OnDiffChanged(int index)
    {
        if (!diffToggles[index].isOn) return;

        currentDiff = index switch
        {
            0 => Difficulty.Easy,
            2 => Difficulty.Hard,
            _ => Difficulty.Normal
        };

        for (int i = 0; i < diffToggles.Count; i++)
        {
            if (i == index) diffToggles[i].GetComponent<Image>().color = selected;
            else diffToggles[i].GetComponent<Image>().color = normal;
        }
    }

    public void OnMapNextButton()
    {
        currentMapIndex = (currentMapIndex + 1) % mapSprites.Count;
        UpdateMapPreview();
    }

    public void OnMapPrevButton()
    {
        currentMapIndex = (currentMapIndex + mapSprites.Count - 1) % mapSprites.Count;
        UpdateMapPreview();
    }

    private void UpdateMapPreview()
    {
        if (mapPreviewImage == null || mapSprites == null) return;
        if (currentMapIndex < 0 || currentMapIndex >= mapSprites.Count) return;

        mapPreviewImage.sprite = mapSprites[currentMapIndex];
        mapName.text = mapSprites[currentMapIndex].name;
    }

    public void OnPlayerAddButton()
    {
        currentPlayerCount = Mathf.Min(maxPlayers, currentPlayerCount + 1);
        UpdatePlayerCount();
    }
    public void OnPlayerSubButton()
    {
        currentPlayerCount = Mathf.Max(minPlayers, currentPlayerCount - 1);
        UpdatePlayerCount();
    }

    private void UpdatePlayerCount()
    {
        countText.text = currentPlayerCount.ToString();
    }

    public override void OnCloseButton()
    {
        base.OnCloseButton();
    }

    public void OnGameStartButton()
    {
        if (MatchManager.Instance == null) return;

        SingleplaySettings.AICount = currentPlayerCount;
        SingleplaySettings.MapIndex = (currentMapIndex == mapSprites.Count - 1) ? Random.Range(0, mapSprites.Count - 1) : currentMapIndex;
        SingleplaySettings.Diff = currentDiff;

        UIManager.Instance?.HidePopup<SingleplayPopup>();
        UIManager.Instance?.ShowPopup<LoadingPopup>();
        MatchManager.Instance.RequestStartSingleplay();
    }
}

public enum Difficulty { Easy, Normal, Hard }

public static class SingleplaySettings
{
    public static int AICount  { get; set; } = 1;
    public static int MapIndex { get; set; } = 0;
    public static Difficulty Diff { get; set; } = Difficulty.Normal;
}