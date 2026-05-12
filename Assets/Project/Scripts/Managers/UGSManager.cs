using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class UGSManager : MonoBehaviour
{
    public static UGSManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
 
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
 
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
 
            IsInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"UGS initialization failed: {e.Message}");
            IsInitialized = true;
        }
    }
}