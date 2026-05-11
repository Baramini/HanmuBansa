using UnityEngine;
using Unity.Netcode;
using BrmnModules.Pool;
using BrmnModules.Audio;

public class Projectile : NetworkBehaviour
{
    private Rigidbody rb;
    private GameObject prefab;

    private float speed;
    private Vector3 lastVelocity;
    private bool isReturned = false;

    private float reflectCooldown = 0f;
    private const float REFLECT_INTERVAL = 0.02f; // Reflect cooltime

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Called after Spawn()
    public void InitOnServer(GameObject prefabRef, Vector3 velocity)
    {
        prefab = prefabRef;
        speed = velocity.magnitude;
        isReturned = false;

        rb.linearVelocity = velocity;

        // Sync velocity all clients
        SetVelocityClientRpc(velocity);
    }

    [ClientRpc]
    private void SetVelocityClientRpc(Vector3 velocity)
    {
        // At server already set in InitOnServer()
        if (IsServer) return;

        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = velocity;
        speed = velocity.magnitude;

        AudioManager.Instance?.PlaySFXAtPosition("Fire", transform.position);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        lastVelocity = rb.linearVelocity;

        if (reflectCooldown > 0f) reflectCooldown -= Time.fixedDeltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only server processes
        if (!IsServer) return;

        GameObject hit = collision.gameObject;

        if (hit.layer == LayerMask.NameToLayer("Wall"))
        {
            if (reflectCooldown > 0f) return;

            Vector3 normal = collision.GetContact(0).normal;
            Reflect(normal);
            reflectCooldown = REFLECT_INTERVAL;

            return;
        }

        if (hit.layer == LayerMask.NameToLayer("Projectile"))
        {
            hit.GetComponent<Projectile>()?.ReturnToPool();
            ReturnToPool();

            PlayCollisionSFXClientRpc(transform.position);

            return;
        }

        if (hit.layer == LayerMask.NameToLayer("Tank"))
        {
            TankStatus status = hit.GetComponent<TankStatus>();

            if (status != null && status.HasShield)
            {
                status.ConsumeShield();
                Reflect(collision.GetContact(0).normal);
            }
            else
            {
                hit.GetComponent<TankHealth>()?.TakeDamage(1);
                ReturnToPool();
                PlayCollisionSFXClientRpc(status.transform.position);
            }
            return;
        }
    }

    [ClientRpc]
    private void PlayCollisionSFXClientRpc(Vector3 position)
    {
        AudioManager.Instance?.PlaySFXAtPosition("Explosion", position);
    }

    private void Reflect(Vector3 normal)
    {
        // Use lastVelocity for correct reflection
        Vector3 inDirection = lastVelocity.normalized;
        Vector3 reflected = Vector3.Reflect(inDirection, normal);
        transform.rotation = Quaternion.LookRotation(reflected);

        reflected.y = 0f;
        reflected.Normalize();
        rb.linearVelocity = reflected * speed;
    }

    public void ReturnToPool()
    {
        if (isReturned) return;

        if (IsSpawned) NetworkObject.Despawn(false);

        isReturned = true;
        PoolManager.Instance.Release(prefab, gameObject);
    }
}