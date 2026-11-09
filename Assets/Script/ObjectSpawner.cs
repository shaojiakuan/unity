using UnityEngine;
using System.Collections;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject objectPrefab;
    public float spawnInterval = 0.2f;
    public float objectLifetime = 3f;
    private Vector3 spawnPosition;
    private SimpleGameObjectPool pool;

    void Start()
    {
        spawnPosition = transform.position;
        if (objectPrefab == null)
        {
            enabled = false;
            return;
        }

        pool = new SimpleGameObjectPool(objectPrefab, transform);
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            GameObject obj = pool.Get(spawnPosition, Quaternion.identity);
            StartCoroutine(pool.ReleaseAfter(obj, objectLifetime));
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
