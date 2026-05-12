using UnityEngine;
using Unity.Netcode;
using BrmnModules.AI;

// Because replace TankController
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankShooter))]
[RequireComponent(typeof(TankStatus))]
[RequireComponent(typeof(TankHealth))]
public class AITankController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 150f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float aimTolerance = 8f; // Permissible launch angle (degrees)
    [SerializeField] private float fireCooldown = 1.5f;

    [Header("Flee")]
    [SerializeField] private int fleeHpThreshold = 1;
    [SerializeField] private float fleeRange = 15f; // Flee detection distance

    private Rigidbody rb;
    private TankShooter shooter;
    private TankStatus status;
    private TankHealth health;

    private float fireTimer;
    private Transform currentTarget;

    public float AttackRange => attackRange;
    public float FleeRange => fleeRange;
    public int FleeHpThreshold => fleeHpThreshold;
    public Transform CurrentTarget => currentTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shooter = GetComponent<TankShooter>();
        status = GetComponent<TankStatus>();
        health = GetComponent<TankHealth>();
    }

    private void Update()
    {
        if (fireTimer > 0f) fireTimer -= Time.deltaTime;
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;
    }

    public void MoveTowardTarget()
    {
        if (currentTarget == null) return;
        MoveToward(currentTarget.position);
    }

    public void FleeFromTarget()
    {
        if (currentTarget == null) return;

        Vector3 dir = (transform.position - currentTarget.position).normalized;
        Vector3 fleePos = transform.position + dir * 10f;
        MoveToward(fleePos);
    }

    public void MoveTowardItem(Transform item)
    {
        if (item == null) return;
        MoveToward(item.position);
    }

    public void Wander()
    {
        MoveToward(Vector3.zero);
    }

    public NodeStatus AimAndFire()
    {
        if (currentTarget == null) return NodeStatus.Failure;
        if (fireTimer > 0f) return NodeStatus.Running;

        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > attackRange) return NodeStatus.Failure;

        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return NodeStatus.Failure;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > aimTolerance) return NodeStatus.Running;

        shooter.AIFire();
        fireTimer = fireCooldown;
        return NodeStatus.Success;
    }

    public bool IsTargetInAttackRange()
    {
        if (currentTarget == null) return false;
        return Vector3.Distance(transform.position, currentTarget.position) <= attackRange;
    }

    public bool IsTargetInFleeRange()
    {
        if (currentTarget == null) return false;
        return Vector3.Distance(transform.position, currentTarget.position) <= fleeRange;
    }

    public bool IsLowHp()
    {
        return health.CurrentHp <= fleeHpThreshold;
    }

    public bool IsOverheated()
    {
        return shooter.Overheat;
    }

    public bool IsTargetDead()
    {
        if (currentTarget == null) return true;
        return currentTarget.GetComponent<TankHealth>()?.IsDead ?? true;
    }

    private void MoveToward(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.5f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));

        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle < 45f)
        {
            float speedMult = status?.SpeedMultiplier ?? 1f;
            rb.MovePosition(rb.position + transform.forward * moveSpeed * speedMult * Time.fixedDeltaTime);
        }
    }
}
