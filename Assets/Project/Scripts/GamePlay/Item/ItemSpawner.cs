using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

// Manages item spawn timing and positions.
// Normal items: random item at random position every 20s from game start +20s.
// Special item: spawned at 3min, position revealed at 2:30.
public class ItemSpawner : NetworkBehaviour
{
    [SerializeField] private Transform itemParent;

    [Header("Normal Items")]
    [SerializeField] private List<GameObject> normalItemPrefabs;
    [SerializeField] private Transform normalSpawnPointsParent;
    [SerializeField] private float spawnInterval = 20f;
    private List<Transform> normalSpawnPoints = new();

    [Header("Special Item")]
    [SerializeField] private GameObject specialItemPrefab;
    [SerializeField] private Transform specialSpawnPointsParent;
    private List<Transform> specialSpawnPoints = new();
    
    private bool specialItemSpawned = false;

    private void Awake()
    {
        // -- Collect all children as spawn points --
        foreach (Transform child in normalSpawnPointsParent)
            normalSpawnPoints.Add(child);

        foreach (Transform child in specialSpawnPointsParent)
            specialSpawnPoints.Add(child);
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // -- Subscribe to game timer events --
        GameManager.Instance.OnSpecialItemSpawn   += OnSpecialItemSpawn;

        StartCoroutine(NormalItemSpawnCoroutine());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSpecialItemSpawn   -= OnSpecialItemSpawn;
        }
    }

    // -- Spawn normal item every 20s starting at 20s --
    private IEnumerator NormalItemSpawnCoroutine()
    {
        // -- Wait until NetworkManager is listening (Host/Server started) --
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening);

        // -- Then wait for game to start --
        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.IsGameStarted);

        yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            SpawnNormalItem();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnNormalItem()
    {
        if (normalItemPrefabs.Count == 0 || normalSpawnPoints.Count == 0) return;

        // -- Pick random item and position --
        int itemIndex  = Random.Range(0, normalItemPrefabs.Count);
        int pointIndex = Random.Range(0, normalSpawnPoints.Count);

        GameObject obj = Instantiate(
            normalItemPrefabs[itemIndex],
            normalSpawnPoints[pointIndex].position,
            Quaternion.identity,
            itemParent
        );
        obj.GetComponent<NetworkObject>().Spawn();
    }

    private void OnSpecialItemWarning()
    {
        // -- GameManager already handles ClientRpc for banner/marker --
        // -- Just prepare spawn point marker here if needed --
    }

    private void OnSpecialItemSpawn()
    {
        if (specialItemSpawned) return;
        specialItemSpawned = true;

        int pointIndex = Random.Range(0, specialSpawnPoints.Count);

        GameObject obj = Instantiate(
            specialItemPrefab,
            specialSpawnPoints[pointIndex].position,
            Quaternion.identity
        );
        obj.GetComponent<NetworkObject>().Spawn();
    }
}