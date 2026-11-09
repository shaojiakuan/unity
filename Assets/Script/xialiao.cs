using System.Net.NetworkInformation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Random = UnityEngine.Random;

public class xialiao : MonoBehaviour
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
    private double tim_tem;
    private int time1;
    private SimpleGameObjectPool pool;
    private Coroutine spawnCoroutine;
    // Start is called before the first frame update
    void Start()
    {
        if (ball != null)
        {
            pool = new SimpleGameObjectPool(ball, transform);
        }
    }
        IEnumerator enumerator()
    {
        // while (true)
        for (int j = 0; j < 5; j++)
        {
            for (int i = 0; i < buildNumbers; i++)
            {
                float X = Random.Range(x1, x2);
                float Y = Random.Range(y1, y2);
                float Z = Random.Range(z1,z2);
                GameObject newball = pool.Get(new Vector3(X, Y, Z), Quaternion.identity);
                if (destroyTime > 0f)
                {
                    StartCoroutine(pool.ReleaseAfter(newball, destroyTime));
                }
            }
            yield return new WaitForSeconds(TimeJG);
        }

        spawnCoroutine = null;
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
    // Update is called once per frame
    void Update()
    {
        tim_tem += Time.deltaTime;
        time1 = (int)tim_tem;
        if (time1 == 5)
        {
            start();
            tim_tem = 6;
        }else if (time1 == 19)
        {
            start();
            tim_tem = 6;
        }
    }
}
