using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class yincang : MonoBehaviour
{
    public GameObject FurnaceWall;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Q)) {
            FurnaceWall.SetActive(false);
        }
        else if (Input.GetKey(KeyCode.E))
        {
            FurnaceWall.SetActive(true);
        }

    }
}
