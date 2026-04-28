using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LobbyLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxEntries = 10;

    private Queue<string> _logs = new();

    public void LogJoined(string playerName)
        => AddLog($"{playerName} has joined.");

    public void LogLeft(string playerName)
        => AddLog($"{playerName} has left.");

    private void AddLog(string message)
    {
        if (_logs.Count >= maxEntries)
            _logs.Dequeue();

        _logs.Enqueue(message);
        logText.text = string.Join("\n", _logs);
    }

    public void Clear()
    {
        _logs.Clear();
        logText.text = "";
    }
}