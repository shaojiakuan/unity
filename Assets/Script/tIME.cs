using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tIME : MonoBehaviour
{
    public randomizer s1;
    public shengcheng1 s2;
    public shengcheng2 s3;
    public shengcheng3 s4;
    public double tim_tem;
    public int time1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        tim_tem += Time.deltaTime;
        time1 = (int)tim_tem;
        if (time1 == 0)
        {
            s1.start();
            tim_tem = 1;
        
        }else if (time1 == 3){
            s2.start();
            tim_tem = 6;
        }else if (time1 == 12)
        { 
            s3.start();
            tim_tem = 13;
        }else if (time1 == 20)
        {
            s4.start();
            tim_tem = 25;
        }
    }
}
