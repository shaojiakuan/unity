using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public bool Generat = true;
    public GameObject fuel;
    public float spawnInterval = 0.02f;
    public float objectLifetime = 8f;
    public int spawnCount = 5;
    public int maxActiveObjects = 200;

    private SimpleGameObjectPool pool;
    private float spawnTimer;
    private readonly List<GameObject> activeObjects = new List<GameObject>(256);

    void Start()
    {
        if (fuel != null)
        {
            pool = new SimpleGameObjectPool(fuel, transform);
        }
    }

    void Update()
    {
        if (!Generat || fuel == null || pool == null)
        {
            return;
        }

        CleanupInactiveObjects();
        if (activeObjects.Count >= maxActiveObjects)
        {
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval)
        {
            return;
        }

        spawnTimer = 0f;
        int remainingCapacity = Mathf.Max(0, maxActiveObjects - activeObjects.Count);
        int count = Mathf.Min(spawnCount, remainingCapacity);
        for (int i = 0; i < count; i++)
        {
            Vector3 position = transform.position + new Vector3(0, 0, Random.Range(-0.6f, 0.6f));
            GameObject obj = pool.Get(position, Quaternion.identity);
            activeObjects.Add(obj);
            StartCoroutine(ReleaseTracked(obj, objectLifetime));
        }
    }

    private IEnumerator ReleaseTracked(GameObject obj, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        activeObjects.Remove(obj);
        if (pool != null)
        {
            pool.Release(obj);
        }
    }

    private void CleanupInactiveObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            if (obj == null || !obj.activeInHierarchy)
            {
                activeObjects.RemoveAt(i);
            }
        }
    }
}
