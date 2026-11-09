using System.Net.NetworkInformation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Random = UnityEngine.Random;

public class shengcheng1 : MonoBehaviour
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
    public double time1 = 0;
    MeshRenderer render;
    List<GameObject> balllist;
    SimpleGameObjectPool pool;
    Coroutine spawnCoroutine;
    void Start()
    {
        // StartCoroutine(enumerator());
        balllist = new List<GameObject>();
        if (ball != null)
        {
            pool = new SimpleGameObjectPool(ball, transform);
        }
    }

    IEnumerator enumerator()
    {
        while (true)
        // for (int j = 0; j < 15; j++)
        {
            CleanupInactiveObjects();
            int remainingCapacity = Mathf.Max(0, maxActiveObjects - balllist.Count);
            int spawnCount = Mathf.Min(buildNumbers, remainingCapacity);
            for (int i = 0; i < spawnCount; i++)
            {
                float X = Random.Range(x1, x2);
                float Y = Random.Range(y1, y2);
                float Z = Random.Range(z1,z2);
                GameObject newball = pool.Get(new Vector3(X, Y, Z), Quaternion.identity);
                balllist.Add(newball);
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

        balllist.Remove(obj);
        if (pool != null)
        {
            pool.Release(obj);
        }
    }

    private void CleanupInactiveObjects()
    {
        for (int i = balllist.Count - 1; i >= 0; i--)
        {
            GameObject obj = balllist[i];
            if (obj == null || !obj.activeInHierarchy)
            {
                balllist.RemoveAt(i);
            }
        }
    }
    
}   
