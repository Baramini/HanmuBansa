using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace BrmnModules.Pool
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_pools.ContainsKey(prefab))
                CreatePool(prefab);

            GameObject obj = _pools[prefab].Get();
            obj.transform.SetPositionAndRotation(position, rotation);

            if (parent != null)
                obj.transform.SetParent(parent);

            return obj;
        }

        public void Release(GameObject prefab, GameObject obj)
        {
            obj.transform.SetParent(null);

            if (_pools.ContainsKey(prefab))
                _pools[prefab].Release(obj);
            else
                Destroy(obj);
        }

        private void CreatePool(GameObject prefab)
        {
            _pools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                defaultCapacity: 20,
                maxSize: 500
            );
        }
    }
}