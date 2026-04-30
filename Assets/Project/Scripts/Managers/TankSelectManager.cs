using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Manages tank selection state across all clients.
// Prevents duplicate selections and syncs state to all players.
public class TankSelectManager : NetworkBehaviour
{
    public static TankSelectManager Instance { get; private set; }

    // -- Tank prefabs: index matches thumbnail index --
    [SerializeField] private List<GameObject> tankPrefabs;

    // -- clientId -> tank index (-1 = not selected) --
    private Dictionary<ulong, int> _selections = new();

    // -- Notify UI to refresh --
    public event System.Action OnSelectionChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -- Called by client when player selects a tank --
    [ServerRpc(RequireOwnership = false)]
    public void SelectTankServerRpc(int tankIndex, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // -- Check if tank already taken by another player --
        foreach (var kvp in _selections)
        {
            if (kvp.Key != clientId && kvp.Value == tankIndex)
            {
                // -- Already taken: reject and notify caller --
                NotifySelectionRejectedClientRpc(clientId);
                return;
            }
        }

        // -- Register selection --
        _selections[clientId] = tankIndex;

        // -- Sync current selections to all clients --
        SyncSelectionsClientRpc(BuildSelectionArray());
    }

    // -- Build flat array for RPC: [clientId0, index0, clientId1, index1, ...] --
    private ulong[] BuildSelectionArray()
    {
        List<ulong> data = new();
        foreach (var kvp in _selections)
        {
            data.Add(kvp.Key);
            data.Add((ulong)kvp.Value);
        }
        return data.ToArray();
    }

    [ClientRpc]
    private void SyncSelectionsClientRpc(ulong[] data)
    {
        _selections.Clear();
        for (int i = 0; i < data.Length; i += 2)
            _selections[data[i]] = (int)data[i + 1];

        // -- Notify UI to refresh selection state --
        OnSelectionChanged?.Invoke();
    }

    [ClientRpc]
    private void NotifySelectionRejectedClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;
        Debug.Log("Tank already taken. Please choose another.");
        // -- TODO: show message in UI --
    }

    // -- Check if tank index is taken by another player --
    public bool IsTankTaken(int tankIndex)
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        foreach (var kvp in _selections)
        {
            if (kvp.Key != localId && kvp.Value == tankIndex)
                return true;
        }
        return false;
    }

    // -- Get local player's selected tank index (-1 if none) --
    public int GetLocalSelection()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        return _selections.TryGetValue(localId, out int index) ? index : -1;
    }

    // -- Get tank prefab for given client (called by SpawnManager) --
    public GameObject GetTankPrefab(ulong clientId)
    {
        if (_selections.TryGetValue(clientId, out int index))
            return tankPrefabs[index];

        // -- Fallback: first tank if no selection --
        return tankPrefabs[0];
    }

    // -- Check if all connected clients have selected --
    public bool AllPlayersSelected()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!_selections.ContainsKey(clientId))
                return false;
        }
        return NetworkManager.Singleton.ConnectedClientsIds.Count > 0;
    }

    public int GetSelectionByClientId(ulong clientId)
    {
        return _selections.TryGetValue(clientId, out int index) ? index : -1;
    }

    public void ResetSelections()
    {
        _selections.Clear();
        SyncSelectionsClientRpc(new ulong[0]);
    }
}