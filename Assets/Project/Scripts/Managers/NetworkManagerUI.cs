using UnityEngine;
using Unity.Netcode;

// Temporary matching UI for testing.
// Will be replaced with proper UI in Week 3.
public class NetworkManagerUI : MonoBehaviour
{
    private string _inputCode = "";
    private string _statusMessage = "Ready.";
    private string _roomCode = "";

    private void Start()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnRoomCodeGenerated += code =>
            {
                _roomCode = code;
                _statusMessage = $"Room created. Code: {code}";
            };
            MatchManager.Instance.OnMatchError += msg =>
            {
                _statusMessage = $"Error: {msg}";
            };
            MatchManager.Instance.OnMatchStarted += () =>
            {
                _statusMessage = "Connected!";
            };
        }
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.black;

        GUILayout.BeginArea(new Rect(10, 10, 280, 300));
        GUILayout.Label(_statusMessage, labelStyle);
        

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // -- Create room --
            if (GUILayout.Button("Create Room (Host)", labelStyle))
                _ = MatchManager.Instance.CreateRoomAsync();

            // -- Join by code --
            GUILayout.Label("Room Code:", labelStyle);
            _inputCode = GUILayout.TextField(_inputCode, 6);
            if (GUILayout.Button("Join by Code"))
                _ = MatchManager.Instance.JoinByCodeAsync(_inputCode);

            // -- Auto match --
            if (GUILayout.Button("Auto Match", labelStyle))
                _ = MatchManager.Instance.AutoMatchAsync();
        }
        else
        {
            string role = NetworkManager.Singleton.IsHost ? "Host" : "Client";
            GUILayout.Label($"Role: {role}", labelStyle);
            GUILayout.Label($"Players: {NetworkManager.Singleton.ConnectedClients.Count}", labelStyle);

            if (!string.IsNullOrEmpty(_roomCode))
                GUILayout.Label($"Room Code: {_roomCode}", labelStyle);

            // -- Only host sees start button --
            if (NetworkManager.Singleton.IsHost)
            {
                int count = NetworkManager.Singleton.ConnectedClients.Count;
                string startLabel = count >= 2
                    ? $"Start Game ({count} players)"
                    : $"Waiting... ({count}/2 min)";

                GUI.enabled = count >= 2;
                if (GUILayout.Button(startLabel))
                    MatchManager.Instance.RequestStartGame();
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label("Waiting for host to start...", labelStyle);
            }

            // -- Timer + alive count (game started) --
            if (GameManager.Instance != null)
            {
                float remaining = GameManager.Instance.GetRemainingTime();
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                GUILayout.Label($"Time: {minutes:00}:{seconds:00}", labelStyle);
                GUILayout.Label($"Alive: {GameManager.Instance.GetAliveCount()}", labelStyle);
            }

            if (GUILayout.Button("Leave Room"))
                _ = MatchManager.Instance.LeaveRoomAsync();
        }

        GUILayout.EndArea();
    }
}