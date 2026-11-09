using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleGameObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Queue<GameObject> inactiveObjects = new Queue<GameObject>();
    private readonly HashSet<GameObject> inactiveSet = new HashSet<GameObject>();

    public SimpleGameObjectPool(GameObject prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = inactiveObjects.Count > 0
            ? TakeInactive()
            : Object.Instantiate(prefab, parent);

        EnsurePoolReference(obj);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public GameObject GetLocal(Vector3 localPosition, Quaternion localRotation)
    {
        GameObject obj = inactiveObjects.Count > 0
            ? TakeInactive()
            : Object.Instantiate(prefab, parent);

        EnsurePoolReference(obj);
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = localRotation;
        obj.SetActive(true);
        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        if (inactiveSet.Contains(obj))
        {
            return;
        }

        obj.SetActive(false);
        inactiveObjects.Enqueue(obj);
        inactiveSet.Add(obj);
    }

    private void EnsurePoolReference(GameObject obj)
    {
        PooledGameObject pooledObject = obj.GetComponent<PooledGameObject>();
        if (pooledObject == null)
        {
            pooledObject = obj.AddComponent<PooledGameObject>();
        }

        pooledObject.SetPool(this);
    }

    private GameObject TakeInactive()
    {
        GameObject obj = inactiveObjects.Dequeue();
        inactiveSet.Remove(obj);
        return obj;
    }

    public IEnumerator ReleaseAfter(GameObject obj, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Release(obj);
    }
}

public class PooledGameObject : MonoBehaviour
{
    private SimpleGameObjectPool pool;

    public void SetPool(SimpleGameObjectPool pool)
    {
        this.pool = pool;
    }

    public void ReturnToPool()
    {
        if (pool != null)
        {
            pool.Release(gameObject);
        }
        else
        {
            Object.Destroy(gameObject);
        }
    }
}
