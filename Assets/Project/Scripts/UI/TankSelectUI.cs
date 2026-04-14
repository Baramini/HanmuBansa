using UnityEngine;
using Unity.Netcode;

// Temporary tank selection UI using OnGUI.
// Will be replaced with proper UI Canvas in Week 3.
public class TankSelectUI : MonoBehaviour
{
    [SerializeField] private Texture2D[] tankThumbnails;  // -- 4 tank thumbnails --
    [SerializeField] private string[] tankNames;          // -- Tank display names --

    private bool _isVisible = false;
    private string _statusMessage = "";

    private void Start()
    {
        if (TankSelectManager.Instance != null)
            TankSelectManager.Instance.OnSelectionChanged += Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted += Hide;
    }

    private void OnDestroy()
    {
        if (TankSelectManager.Instance != null)
            TankSelectManager.Instance.OnSelectionChanged -= Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStarted -= Hide;
    }

    public void Show() => _isVisible = true;
    public void Hide() => _isVisible = false;

    private void Refresh() { }  // -- Force repaint on selection change --

    private void OnGUI()
    {
        if (!_isVisible) return;
        if (TankSelectManager.Instance == null) return;

        // -- Background --
        GUI.Box(new Rect(Screen.width / 2 - 310, Screen.height / 2 - 120, 500, 260),
            "Select Your Tank");

        int localSelection = TankSelectManager.Instance.GetLocalSelection();

        for (int i = 0; i < 4; i++)
        {
            float x = Screen.width / 2 - 300 + i * 120;
            float y = Screen.height / 2 - 80;

            bool isTaken = TankSelectManager.Instance.IsTankTaken(i);
            bool isMyPick = localSelection == i;

            // -- Taken by others: gray out --
            GUI.enabled = !isTaken;
            GUI.color = isTaken ? Color.gray : isMyPick ? Color.green : Color.white;

            // -- Thumbnail button --
            bool clicked = tankThumbnails != null && tankThumbnails.Length > i && tankThumbnails[i] != null
                ? GUI.Button(new Rect(x, y, 100, 100), tankThumbnails[i])
                : GUI.Button(new Rect(x, y, 100, 100), $"Tank {i + 1}");

            if (clicked && !isTaken)
            {
                TankSelectManager.Instance.SelectTankServerRpc(i);
                _statusMessage = $"Selected: {(tankNames != null && tankNames.Length > i ? tankNames[i] : $"Tank {i + 1}")}";
            }

            // -- Tank name label --
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + 105, 100, 20),
                tankNames != null && tankNames.Length > i ? tankNames[i] : $"Tank {i + 1}");

            // -- Taken label --
            if (isTaken)
                GUI.Label(new Rect(x + 20, y + 40, 60, 20), "TAKEN");

            GUI.enabled = true;
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 110, 200, 30), _statusMessage);
    }
}