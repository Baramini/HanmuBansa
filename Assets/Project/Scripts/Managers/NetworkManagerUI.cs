using UnityEngine;
using Unity.Netcode;

// Temporary network connection test script.
// Will be replaced with full matchmaking UI later.
public class NetworkManagerUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 150));

        if (!NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsServer)
        {
            // -- Not connected: show connect buttons --
            if (GUILayout.Button("Host"))
                NetworkManager.Singleton.StartHost();

            if (GUILayout.Button("Client"))
                NetworkManager.Singleton.StartClient();
        }
        else
        {
            // -- Connected: show status --
            string role = NetworkManager.Singleton.IsHost
                ? "Host" : "Client";
            GUILayout.Label($"Role: {role}");
            GUILayout.Label($"ClientID: {NetworkManager.Singleton.LocalClientId}");
        }

        GUILayout.EndArea();
    }
}