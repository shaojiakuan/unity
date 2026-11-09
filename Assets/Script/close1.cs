using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class close1 : MonoBehaviour
{
    
        public GameObject panel01;
        
        public void OnCloseClick01()
        {
            panel01.gameObject.SetActive(false);
        }
        
    
}
