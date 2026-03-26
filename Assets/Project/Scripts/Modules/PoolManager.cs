using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using Unity.Netcode;

namespace BrmnModules.Pool
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        // -- Pool storage: prefab -> pool --
        private Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        // -- Parent storage: prefab -> parent transform --
        // Stored separately because NGO.Despawn() resets the parent,
        // so we need to re-apply it on Release()
        private Dictionary<GameObject, Transform> _poolParents = new();

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

        // -- Get an object from the pool --
        // If the pool does not exist yet, create it first
        // parent: the transform to place the object under in the hierarchy
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_pools.ContainsKey(prefab))
                CreatePool(prefab, parent);

            GameObject obj = _pools[prefab].Get();
            obj.transform.SetPositionAndRotation(position, rotation);

            // -- Place under the designated parent --
            if (parent != null)
                obj.transform.SetParent(parent);

            return obj;
        }

        // -- Return an object to the pool --
        public void Release(GameObject prefab, GameObject obj)
        {
            if (!_pools.ContainsKey(prefab))
            {
                // -- No pool found: just destroy --
                Destroy(obj);
                return;
            }

            // -- NGO.Despawn() resets the parent to scene root --
            // Re-apply the stored parent so hierarchy stays organized
            if (_poolParents.TryGetValue(prefab, out Transform parent) && parent != null)
                obj.transform.SetParent(parent);

            _pools[prefab].Release(obj);
        }

        // -- Create a new pool for the given prefab --
        private void CreatePool(GameObject prefab, Transform parent)
        {
            // -- Store parent reference for use in Release() --
            if (parent != null)
                _poolParents[prefab] = parent;

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