using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Object Pooler
    /// - allows the reuse of frequently "spawned" objects for optimization
    /// </summary>

    public class ObjectPooler : MonoBehaviour
    {
        [System.Serializable]
        public class Pool
        {
            public string poolName;
            public List<GameObject> pooledObjects;
            public GameObject prefab;
            public Transform startingParent;
            public int startingQuantity = 10;
        }
        public Pool[] pools;

        // Use this for initialization
        void Start()
        {
            for (int p = 0; p < pools.Length; p++)
            {
                for (int i = 0; i < pools[p].startingQuantity; i++)
                {
                    GameObject o = Instantiate(pools[p].prefab, Vector3.zero, Quaternion.identity, pools[p].startingParent ? pools[p].startingParent : null);
                    o.SetActive(false);
                    pools[p].pooledObjects.Add(o);
                }
            }
        }

        public GameObject Spawn(string poolName, Vector3 position, Quaternion? rotation = null, Transform parentTransform = null)
        {

            // Find the pool that matches the pool name:
            int pool = 0;
            for (int i = 0; i < pools.Length; i++)
            {
                if (pools[i].poolName == poolName)
                {
                    pool = i;
                    break;
                }
                if (i == pools.Length - 1)
                {
                    Helper.LogError(this, "There's no pool named \"" + poolName + "\"! Check the spelling or add a new pool with this name.");
                    return null;
                }
            }

            // Proceed if found:
            Quaternion finalRot = rotation.GetValueOrDefault(Quaternion.identity);
            for (int i = 0; i < pools[pool].pooledObjects.Count; i++)
            {
                if (!pools[pool].pooledObjects[i].activeSelf)
                {
                    // Set active:
                    pools[pool].pooledObjects[i].SetActive(true);
                    pools[pool].pooledObjects[i].transform.localPosition = position;
                    pools[pool].pooledObjects[i].transform.localRotation = finalRot;
                    // Set parent:
                    if (parentTransform)
                    {
                        pools[pool].pooledObjects[i].transform.SetParent(parentTransform, false);
                    }

                    return pools[pool].pooledObjects[i];
                }
            }
            // If there's no game object available then expand the list by creating a new one:
            GameObject o = Instantiate(pools[pool].prefab, position, finalRot);

            // Add newly instantiated object to pool:
            pools[pool].pooledObjects.Add(o);
            return o;
        }
    }
}