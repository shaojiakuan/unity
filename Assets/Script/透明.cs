using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 透明 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
