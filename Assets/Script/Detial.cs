using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detial : MonoBehaviour
{
    bool isShowTip;
    void Start()
    {
        isShowTip = false;
    }
    void OnMouseEnter()
    {
        isShowTip = true;
    }
    void OnMouseExit()
    {
        isShowTip = false;
    }
    void OnGUI()
    { 
        if(isShowTip)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 100;
            style.normal.textColor = Color.blue;
            GUI.Label(new Rect(Input.mousePosition.x, Screen.height-Input.mousePosition.y, 500, 500), "ÕπÃ®-¿≠…Ï9",style);
        }
    
    }
}
