using UnityEngine;
using Unity.Netcode;
using BrmnModules.Pool;

public class Projectile : NetworkBehaviour
{
    private Rigidbody _rb;
    private GameObject _prefabRef;

    private float _speed;
    private Vector3 _lastVelocity;
    private bool _isReturned = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // -- Called on server after Spawn() --
    // Sends velocity to all clients explicitly
    public void InitOnServer(GameObject prefabRef, Vector3 velocity)
    {
        _prefabRef = prefabRef;
        _speed = velocity.magnitude;
        _isReturned = false;

        // -- Set velocity on server --
        _rb.linearVelocity = velocity;

        // -- Explicitly sync velocity to all clients --
        SetVelocityClientRpc(velocity);
    }

    [ClientRpc]
    private void SetVelocityClientRpc(Vector3 velocity)
    {
        // -- Set velocity on each client's Rigidbody --
        // Skip server as it's already set in InitOnServer()
        if (IsServer) return;

        _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = velocity;
        _speed = velocity.magnitude;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        _lastVelocity = _rb.linearVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // -- Only server processes collision --
        if (!IsServer) return;

        GameObject hit = collision.gameObject;

        if (hit.layer == LayerMask.NameToLayer("Wall"))
        {
            Reflect(collision.GetContact(0).normal);
            return;
        }

        if (hit.layer == LayerMask.NameToLayer("Projectile"))
        {
            hit.GetComponent<Projectile>()?.ReturnToPool();
            ReturnToPool();
            return;
        }

        if (hit.layer == LayerMask.NameToLayer("Tank"))
        {
            hit.GetComponent<TankHealth>()?.TakeDamage(1);
            ReturnToPool();
            return;
        }
    }

    private void Reflect(Vector3 normal)
    {
        Vector3 inDirection = _lastVelocity.normalized;
        Vector3 reflected = Vector3.Reflect(inDirection, normal);
        transform.rotation = Quaternion.LookRotation(reflected);

        reflected.y = 0f;
        reflected.Normalize();
        _rb.linearVelocity = reflected * _speed;
    }

    public void ReturnToPool()
    {
        if (_isReturned) return;

        // -- Despawn from network before returning to pool --
        if (IsSpawned)
            NetworkObject.Despawn(false);

        _isReturned = true;
        PoolManager.Instance.Release(_prefabRef, gameObject);
    }
}