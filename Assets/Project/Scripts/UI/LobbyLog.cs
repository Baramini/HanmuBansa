using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LobbyLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxEntries = 10;

    private Queue<string> logs = new();

    public void LogJoined(string playerName) => AddLog($"{playerName} has joined.");

    public void LogLeft(string playerName) => AddLog($"{playerName} has left.");

    private void AddLog(string message)
    {
        if (logs.Count >= maxEntries) logs.Dequeue();

        logs.Enqueue(message);
        logText.text = string.Join("\n", logs);
    }

    public void Clear()
    {
        logs.Clear();
        logText.text = "";
    }
}