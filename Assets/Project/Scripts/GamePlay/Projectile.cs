using UnityEngine;
using BrmnModules.Pool;

public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;
    private GameObject _prefabRef;

    private float _speed;
    private Vector3 _lastVelocity;
    private bool _isReturned = false;

    public void Init(GameObject prefabRef, Vector3 velocity)
    {
        _isReturned = false;
        _prefabRef = prefabRef;
        _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = velocity;
        _speed = _rb.linearVelocity.magnitude;
    }

    private void FixedUpdate()
    {
        _lastVelocity = _rb.linearVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
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

        reflected.y = 0f;
        reflected.Normalize();
        _rb.linearVelocity = reflected * _speed;
    }

    public void ReturnToPool()
    {
        if (_isReturned) return;
        _isReturned = true;
        PoolManager.Instance.Release(_prefabRef, gameObject);
    }
}