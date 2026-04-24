using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

// Handles UGS initialization and anonymous sign-in.
// Must complete before any Lobby or Relay calls.
public class UGSManager : MonoBehaviour
{
    public static UGSManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // -- Set player name after sign in --
        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);

        IsInitialized = true;
    }
}