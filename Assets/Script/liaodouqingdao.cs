using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class liaodouqingdao : MonoBehaviour
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
    private SimpleGameObjectPool pool;

    void Start()
    {
        if (ball == null)
        {
            enabled = false;
            return;
        }

        pool = new SimpleGameObjectPool(ball, transform);
        StartCoroutine(enumerator());
    }

    IEnumerator enumerator()
    {
        while (true)
        {
            for (int i = 0; i < buildNumbers; i++)
            {
                float X = Random.Range(x1, x2);
                float Y = Random.Range(y1, y2);
                float Z = Random.Range(z1,z2);
                GameObject newball = pool.Get(new Vector3(X, Y, Z), Quaternion.identity);
                StartCoroutine(pool.ReleaseAfter(newball, destroyTime));
            }
            yield return new WaitForSeconds(TimeJG);
        }

    }


}
