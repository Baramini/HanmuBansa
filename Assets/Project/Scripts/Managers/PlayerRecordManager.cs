using UnityEngine;

public class RecordManager : MonoBehaviour
{
    public static RecordManager Instance { get; private set; }

    public int Wins => PlayerPrefs.GetInt("TotalWins", 0);
    public int Losses => PlayerPrefs.GetInt("TotalLosses", 0);
    public int Draws => PlayerPrefs.GetInt("TotalDraws", 0);

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
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

    public string GetRecordString() => $"Win: {Wins}  Lose: {Losses}  Draw: {Draws}";
}