using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialPile : MonoBehaviour
{
    public GameObject Material;
    public List<GameObject> piles;
    public bool IsGenerating;
    float generateTime;
    public int Index;
    // Start is called before the first frame update
    void Start()
    {
        generateTime = Time.time + 0.1f;
        Index = 0;
    }
 
    // Update is called once per frame
    void Update()
    {
        if (IsGenerating && Time.time > generateTime&& Index< piles.Count)
        {
            //GameObject obj = GameObject.Instantiate(Material, transform.position + new Vector3(0, 0.5f, 0), Quaternion.Euler(0, 0, 0));
            //piles.Add(obj);
            piles[Index].SetActive(true);
            Index++;
            generateTime = Time.time + 0.01f;
            if (Index >= piles.Count)
            {
                foreach (GameObject obj in piles)
                    obj.SetActive(false);
                Index = 0;
            }
        }

    }
}
