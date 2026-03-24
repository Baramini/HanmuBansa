using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private List<TankHealth> tanks;

    private int _aliveCount;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _aliveCount = tanks.Count;

        foreach (var tank in tanks)
            tank.OnDead += () => OnTankDead(tank);
    }

    private void OnTankDead(TankHealth deadTank)
    {
        _aliveCount--;
        deadTank.gameObject.SetActive(false);

        Debug.Log($"{deadTank.name} Game Over!");

        if (_aliveCount <= 1)
            EndGame();
    }

    private void EndGame()
    {
        // look alive tank
        foreach (var tank in tanks)
        {
            if (!tank.IsDead)
            {
                Debug.Log($"Winner: {tank.name}");
                break;
            }
        }

        // TODO: Show Victory UI
        Time.timeScale = 0f;   // pause game
    }
}