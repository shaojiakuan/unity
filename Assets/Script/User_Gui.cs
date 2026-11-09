using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User_Gui : MonoBehaviour
{
    public fire f1;
    public fire f2;
    public fire f3;
    public xialiao x1;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 30, 60, 30), "左风"))
        {
            f1.LeftWind();
            f2.LeftWind();
            f3.LeftWind();
        }

        if (GUI.Button(new Rect(10, 70, 60, 30), "右风"))
        {
            f1.RightWind();
            f2.RightWind();
            f3.RightWind();
        }
        /**if (GUI.Button(new Rect(10, 110, 60, 30), "下料"))
        {
            x1.start();
        }*/
    }
}
