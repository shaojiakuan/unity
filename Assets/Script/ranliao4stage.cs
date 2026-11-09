using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using Random = UnityEngine.Random;

public class ranliao4stage : MonoBehaviour
{
    public GameObject ball;
    public float TimeJG, tim_tem;

    public int buildNumbers;
    public float x1;
    public float x2;
    public float y1;
    public float y2;
    public float z1;
    public float z2;
    public float destroyTime;
    public int maxSpawnPerStage = 40;
    public int maxActiveObjects = 80;
    public int time1 = 0;
    private GameObject lupaiti;
    MeshRenderer render;
    GameObject newball;

    List<GameObject> balllist;
    SimpleGameObjectPool pool;
    MaterialPropertyBlock materialPropertyBlock;
    int lastSecond = -1;

    private bool flag = true;
    
    void Start()
    {

        balllist = new List<GameObject>();
        if (ball != null)
        {
            pool = new SimpleGameObjectPool(ball, transform);
        }
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    private void showA()
    {
        if (pool == null)
        {
            return;
        }

        CleanupInactiveObjects();

        int remainingCapacity = Mathf.Max(0, maxActiveObjects - balllist.Count);
        int spawnCount = Mathf.Min(buildNumbers, maxSpawnPerStage, remainingCapacity);
        for (int i = 0; i < spawnCount; i++)
        {

            float X = Random.Range(x1, x2);
            float Y = Random.Range(y1, y2);
            float Z = Random.Range(z1, z2);

            newball = pool.GetLocal(new Vector3(X, Y, Z), Quaternion.identity);
            render = newball.GetComponent<MeshRenderer>();
            if (render != null)
            {
                render.SetPropertyBlock(null);
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                render.receiveShadows = false;
            }

            balllist.Add(newball);
        }
    }
    private void showB()
    {
        
        foreach (var ball in balllist)
        {
            render = ball.GetComponent<MeshRenderer>();
            if (render != null)
            {
                render.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor("_Color", Color.grey);
                materialPropertyBlock.SetColor("_BaseColor", Color.grey);
                render.SetPropertyBlock(materialPropertyBlock);
            }
        }

    }
    private void showC()
    {
        foreach (var ball in balllist)
        {
            ball.transform.localScale *= 0.8f;
           
        }
        flag = false;
    }
    private void showD()
    {
        foreach (var ball in balllist)
        {
            if (pool != null)
            {
                StartCoroutine(pool.ReleaseAfter(ball, destroyTime));
            }
        }
       
        
    }
    private void showE()
    {
        tim_tem = 0;
        time1 = 0;
        lastSecond = -1;
        balllist.Clear();
        flag = true;
    }

    private void CleanupInactiveObjects()
    {
        for (int i = balllist.Count - 1; i >= 0; i--)
        {
            GameObject item = balllist[i];
            if (item == null || !item.activeInHierarchy)
            {
                balllist.RemoveAt(i);
            }
        }
    }

    IEnumerator enumerator()
    {
        while (true)
        {
            for (int i = 0; i < buildNumbers; i++)
            {
                float X = Random.Range(x1, x2);
                float Y = Random.Range(y1, y2);
                float Z = Random.Range(z1, z2);
                GameObject newball = pool.Get(new Vector3(X, Y, Z), Quaternion.identity);
                StartCoroutine(pool.ReleaseAfter(newball, destroyTime));
            }
            yield return new WaitForSeconds(TimeJG);
        }

    }


    void Update()
    {

        tim_tem += Time.deltaTime;
        time1 = (int)tim_tem;
        if (time1 == lastSecond)
        {
            return;
        }

        lastSecond = time1;
        if (time1 == 3)
        {
            showA();
        }
        else if (time1 == 8)
        {
            showB();
        }
        else if (time1 == 12)
        {
            if (flag) { 
            showC();
            }
        }
        else if (time1 == 18)
        {
            showD();
        }
        else if (time1 == 21)
        {
            showE();
        }

    }

}
