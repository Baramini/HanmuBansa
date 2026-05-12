using UnityEngine;
using Unity.Netcode;
using System.Collections;
using BrmnModules.AI;

// Only server process
[RequireComponent(typeof(AITankController))]
[RequireComponent(typeof(TankHealth))]
public class AITankBrain : NetworkBehaviour
{
    [Header("Tick Settings")]
    [SerializeField] private float tickInterval = 0.1f; // Behavior tree traversal cycle(seconds)

    [Header("Item Detection")]
    [SerializeField] private float itemDetectRange = 15f;

    private AITankController ai;
    private TankHealth health;

    private Node rootNode;
    private Coroutine tickCoroutine;

    private bool isInterrupted = false; // Interrupt flag for smart AI

    private void Awake()
    {
        ai = GetComponent<AITankController>();
        health = GetComponent<TankHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        rootNode = BuildTree();

        health.OnDamaged += _ => RequestInterrupt();
        health.OnDead += OnDead;

        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted += OnGameStarted;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (GameManager.Instance != null) GameManager.Instance.OnGameStarted -= OnGameStarted;

        StopTickCoroutine();
    }

    private void OnGameStarted()
    {
        StartTickCoroutine();
    }

    private void OnDead()
    {
        StopTickCoroutine();
    }

    private void StartTickCoroutine()
    {
        StopTickCoroutine();
        tickCoroutine = StartCoroutine(TickCoroutine());
    }

    private void StopTickCoroutine()
    {
        if (tickCoroutine != null)
        {
            StopCoroutine(tickCoroutine);
            tickCoroutine = null;
        }
    }

    private IEnumerator TickCoroutine()
    {
        yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.IsGameStarted);

        while (true)
        {
            isInterrupted = false;

            UpdateTarget();

            rootNode.Tick();

            float elapsed = 0f;
            while (elapsed < tickInterval)
            {
                if (isInterrupted) break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void RequestInterrupt()
    {
        isInterrupted = true;
    }

    // Priorities:
    // 1. If health is low and enemies are nearby → Flee
    // 2. If overheated → Pick up items or wander
    // 3. If enemies are within range → Aim + Fire
    // 4. If enemies are present but far away → toward
    // 5. If items are nearby → Pick up
    // 6. If nothing is nearby → wander

    private Node BuildTree()
    {
        return new Selector(
            new Sequence( // -1-
                new Condition(() => ai.IsLowHp()),
                new Condition(() => ai.IsTargetInFleeRange()),
                new ActionNode(() =>
                {
                    ai.FleeFromTarget();
                    return NodeStatus.Running;
                })
            ),
            new Sequence( // -2-
                new Condition(() => ai.IsOverheated()),
                new Selector(
                    new Sequence(
                        new Condition(() => FindNearestItem() != null),
                        new ActionNode(() =>
                        {
                            ai.MoveTowardItem(FindNearestItem());
                            return NodeStatus.Running;
                        })
                    ),
                    new ActionNode(() =>
                    {
                        ai.Wander();
                        return NodeStatus.Running;
                    })
                )
            ),
            new Sequence( // -3-
                new Condition(() => !ai.IsTargetDead()),
                new Condition(() => ai.IsTargetInAttackRange()),
                new ActionNode(() => ai.AimAndFire())
            ),
            new Sequence( // -4-
                new Condition(() => !ai.IsTargetDead()),
                new ActionNode(() =>
                {
                    ai.MoveTowardTarget();
                    return NodeStatus.Running;
                })
            ),
            new Sequence( // -5-
                new Condition(() => FindNearestItem() != null),
                new ActionNode(() =>
                {
                    ai.MoveTowardItem(FindNearestItem());
                    return NodeStatus.Running;
                })
            ),
            new ActionNode(() => // -6-
            {
                ai.Wander();
                return NodeStatus.Running;
            })
        );
    }

    private void UpdateTarget()
    {
        TankHealth[] allTanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (TankHealth t in allTanks)
        {
            if (t.gameObject == gameObject) continue;
            if (t.IsDead) continue;

            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = t.transform;
            }
        }

        ai.SetTarget(nearest);
    }

    private Transform FindNearestItem()
    {
        ItemPickup[] items = FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (ItemPickup item in items)
        {
            if (!item.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < itemDetectRange && dist < minDist)
            {
                minDist = dist;
                nearest = item.transform;
            }
        }

        return nearest;
    }
}
