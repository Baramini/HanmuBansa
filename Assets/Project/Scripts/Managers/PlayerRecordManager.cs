using UnityEngine;

// Manages local player record using PlayerPrefs.
// Saved permanently on device.
public class RecordManager : MonoBehaviour
{
    public static RecordManager Instance { get; private set; }

    // -- Record data --
    public int Wins => PlayerPrefs.GetInt("TotalWins", 0);
    public int Losses => PlayerPrefs.GetInt("TotalLosses", 0);
    public int Draws => PlayerPrefs.GetInt("TotalDraws", 0);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RecordWin()
    {
        PlayerPrefs.SetInt("TotalWins", Wins + 1);
        PlayerPrefs.Save();
    }

    public void RecordLoss()
    {
        PlayerPrefs.SetInt("TotalLosses", Losses + 1);
        PlayerPrefs.Save();
    }

    public void RecordDraw()
    {
        PlayerPrefs.SetInt("TotalDraws", Draws + 1);
        PlayerPrefs.Save();
    }

    // -- Formatted string for UI display --
    public string GetRecordString()
        => $"Win: {Wins}  Lose: {Losses}  Draw: {Draws}";
}