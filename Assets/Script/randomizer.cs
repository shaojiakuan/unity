using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class randomizer : MonoBehaviour
{
    public GameObject ball;
    public float TimeJG;
    public int buildNumbers;
    public float x1;
    public float x2;
    public float y1;
    public float y2;
    public float z1;
    public float z2;
    public float destroyTime;
    public int maxActiveObjects = 200;
    private SimpleGameObjectPool pool;
    private Coroutine spawnCoroutine;
    private readonly List<GameObject> activeObjects = new List<GameObject>(256);

    void Start()
    {
        if (ball != null)
        {
            pool = new SimpleGameObjectPool(ball, transform);
        }
        // StartCoroutine(enumerator());
    }

    IEnumerator enumerator()
    {
        while (true)
        {
            CleanupInactiveObjects();
            int remainingCapacity = Mathf.Max(0, maxActiveObjects - activeObjects.Count);
            int spawnCount = Mathf.Min(buildNumbers, remainingCapacity);
            for (int i = 0; i < spawnCount; i++)
            {
                float X = Random.Range(x1, x2);
                float Y = Random.Range(y1, y2);
                float Z = Random.Range(z1,z2);
                GameObject newball = pool.Get(new Vector3(X, Y, Z), Quaternion.identity);
                activeObjects.Add(newball);
                StartCoroutine(ReleaseTracked(newball, destroyTime));
            }
            yield return new WaitForSeconds(Mathf.Max(0.02f, TimeJG));
        }

    }
    public void start(){
        if (pool == null && ball != null)
        {
            pool = new SimpleGameObjectPool(ball, transform);
        }

        if (pool != null && spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(enumerator());
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
