using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class createranliao : MonoBehaviour
{
    public GameObject prefab_ball;
    public int buildNumbers;
    public float x1;
    public float x2;
    public float y1;
    public float y2;
    public float z1;
    public float z2;
    public float spawnInterval = 0.1f;
    public int maxSpawnPerTick = 12;
    public int maxActiveObjects = 120;
    public float releaseDelay = 2f;

    private GameObject ranliaoban;
    private GameObject prefab_ball_instance;
    private SimpleGameObjectPool pool;
    private readonly List<GameObject> activeObjects = new List<GameObject>(128);
    private float nextSpawnTime;

    void Start()
    {
        ranliaoban = GameObject.Find("ranliaoban2");
        if (prefab_ball != null)
        {
            Transform parent = ranliaoban != null ? ranliaoban.transform : transform;
            pool = new SimpleGameObjectPool(prefab_ball, parent);
        }
    }

    void Update()
    {
        if (pool == null || Time.time < nextSpawnTime)
        {
            return;
        }

        CleanupInactiveObjects();
        nextSpawnTime = Time.time + Mathf.Max(0.02f, spawnInterval);

        int remainingCapacity = Mathf.Max(0, maxActiveObjects - activeObjects.Count);
        int spawnCount = Mathf.Min(buildNumbers, maxSpawnPerTick, remainingCapacity);
        for (int i = 0; i < spawnCount; i++)
        {
            float X = Random.Range(x1, x2);
            float Z = Random.Range(z1, z2);
            prefab_ball_instance = pool.GetLocal(new Vector3(X, 0, Z), Quaternion.identity);
            activeObjects.Add(prefab_ball_instance);
            StartCoroutine(ReleaseTracked(prefab_ball_instance, releaseDelay));
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
