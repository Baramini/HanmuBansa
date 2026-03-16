using UnityEngine;
using BrmnModules.Pool;

public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;
    private GameObject _prefabRef;   // 풀 반환 시 필요
    private float _timer;

    public void Init(GameObject prefabRef, Vector3 velocity)
    {
        _prefabRef = prefabRef;
        _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = velocity;
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {

    }

    private void ReturnToPool()
    {
        PoolManager.Instance.Release(_prefabRef, gameObject);
    }
}