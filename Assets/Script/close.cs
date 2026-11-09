using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class close : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Hello();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void GetFromWeb(string str)
    {
        Hello();
    }
}
