using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class TankSelectManager : NetworkBehaviour
{
    public static TankSelectManager Instance { get; private set; }
    [SerializeField] private List<GameObject> tankPrefabs;

    // -1 => not selected
    private Dictionary<ulong, int> selections = new();

    // Notify UI to refresh
    public event System.Action OnSelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // When player select tank
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SelectTankServerRpc(int tankIndex, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // Check already selected
        foreach (var kvp in selections)
        {
            if (kvp.Key != clientId && kvp.Value == tankIndex)
            {
                NotifySelectionRejectedClientRpc(clientId);
                return;
            }
        }

        selections[clientId] = tankIndex;
        SyncSelectionsClientRpc(BuildSelectionArray());
    }

    // Build array for RPC
    private ulong[] BuildSelectionArray()
    {
        List<ulong> data = new();
        foreach (var kvp in selections)
        {
            data.Add(kvp.Key);
            data.Add((ulong)kvp.Value);
        }
        return data.ToArray();
    }

    [ClientRpc]
    private void SyncSelectionsClientRpc(ulong[] data)
    {
        selections.Clear();
        for (int i = 0; i < data.Length; i += 2) selections[data[i]] = (int)data[i + 1];

        OnSelectionChanged?.Invoke();
    }

    [ClientRpc]
    private void NotifySelectionRejectedClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        // TODO: show message
    }

    public bool IsTankTaken(int tankIndex)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        foreach (var kvp in selections)
        {
            if (kvp.Key != localId && kvp.Value == tankIndex)
                return true;
        }
        return false;
    }

    public int GetLocalSelection()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        return selections.TryGetValue(localId, out int index) ? index : -1;
    }

    // Get tank prefab
    public GameObject GetTankPrefab(ulong clientId)
    {
        if (selections.TryGetValue(clientId, out int index)) return tankPrefabs[index];

        // Not select first tank callback
        return tankPrefabs[0];
    }

    // Check if all connected clients have selected
    public bool AllPlayersSelected()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!selections.ContainsKey(clientId)) return false;
        }
        return NetworkManager.Singleton.ConnectedClientsIds.Count > 0;
    }

    public int GetSelectionByClientId(ulong clientId)
    {
        return selections.TryGetValue(clientId, out int index) ? index : -1;
    }

    public void ResetSelections()
    {
        selections.Clear();
        SyncSelectionsClientRpc(new ulong[0]);
    }
}