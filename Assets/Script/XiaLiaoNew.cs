using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XiaLiaoNew : MonoBehaviour
{
    //public GameObject ball;
    public float TimeJG = 1;
    public GameObject[] Materials;
    public Vector3[] Positions;
    public Quaternion[] Rotations;

    public float destroyTime;
    private double tim_tem;
    private int time1;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Materials.Length; i++)
        {
            Positions[i] = Materials[i].transform.localPosition;
            Rotations[i] = Materials[i].transform.localRotation;
            Materials[i].SetActive(true);
        }
    }
    IEnumerator enumerator()
    {
        // while (true)
        int i = 0;
        int num = Materials.Length;
        yield return new WaitForSeconds(.5f);
        while (i < num)
        {
            for (int j = 0; j < 200; j++)
            {
                if (i >= num)
                    break;
                Materials[i].AddComponent<Rigidbody>().useGravity = true;
                Materials[i].GetComponent<Rigidbody>().AddForce(new Vector3(80, 34, 0));
                i++;
            }
            yield return new WaitForEndOfFrame();
        }
        yield return new WaitForSeconds(5);
        for (i = 0; i < num; i++)
        {
            Destroy(Materials[i].GetComponent<Rigidbody>());
            Materials[i].transform.localRotation = Rotations[i];
            Materials[i].transform.localPosition = Positions[i];
        }
        yield return new WaitForSeconds(1);
        for (i = 0; i < num; i++)
        {
            Destroy(Materials[i].GetComponent<Rigidbody>());
            Materials[i].transform.localRotation = Rotations[i];
            Materials[i].transform.localPosition = Positions[i];
        }
    }
    public void start()
    {
        StartCoroutine(enumerator());
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
        }
        else if (time1 == 19)
        {
            start();
            tim_tem = 6;
        }
    }
}
