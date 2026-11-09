using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject cube;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //用H键控制物体隐藏
        if (Input.GetKeyDown(KeyCode.H))
        {
            cube.SetActive(false);
        }
        //用S键控制物体显示
        if (Input.GetKeyDown(KeyCode.J))
        {
            cube.SetActive(true);
        }
    }
}
