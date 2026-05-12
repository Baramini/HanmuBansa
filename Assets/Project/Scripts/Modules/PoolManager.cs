using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace BrmnModules.Pool
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private Dictionary<GameObject, ObjectPool<GameObject>> pools = new();
        private Dictionary<GameObject, Transform> poolParents = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!pools.ContainsKey(prefab)) CreatePool(prefab, parent);

            GameObject obj = pools[prefab].Get();
            obj.transform.SetPositionAndRotation(position, rotation);

            if (parent != null) obj.transform.SetParent(parent);

            return obj;
        }

        public void Release(GameObject prefab, GameObject obj)
        {
            if (!pools.ContainsKey(prefab))
            {
                Destroy(obj);
                return;
            }

            if (poolParents.TryGetValue(prefab, out Transform parent) && parent != null) obj.transform.SetParent(parent);

            pools[prefab].Release(obj);
        }

        public void ClearAllPools()
        {
            foreach (var pool in pools.Values) pool.Clear();

            pools.Clear();
            poolParents.Clear();
        }

        private void CreatePool(GameObject prefab, Transform parent)
        {
            if (parent != null) poolParents[prefab] = parent;

            pools[prefab] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: obj => Destroy(obj),
                defaultCapacity: 50,
                maxSize: 1000 // Free UGS issue...
            );
        }
    }
}