using UnityEngine;
using BrmnModules.Pool;

public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;
    private GameObject _prefabRef;

    private float _speed;
    private Vector3 _lastVelocity;

    public void Init(GameObject prefabRef, Vector3 velocity)
    {
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

        if (hit.layer == LayerMask.NameToLayer("Wall") || hit.layer == LayerMask.NameToLayer("Projectile"))
        {
            Reflect(collision.GetContact(0).normal);
            Debug.DrawRay(collision.GetContact(0).point, collision.GetContact(0).normal, Color.red, 1.0f);
            return;
        }

        if (hit.layer == LayerMask.NameToLayer("Tank"))
        {
            hit.GetComponent<TankHealth>()?.TakeDamage(1);
            ReturnToPool();
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
        PoolManager.Instance.Release(_prefabRef, gameObject);
    }
}