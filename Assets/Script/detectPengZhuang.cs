using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class detectPengZhuang : MonoBehaviour
{
    private Slider slider;
    float slider_value;
    public ParticleSystem particleSystem_1;
    public ParticleSystem particleSystem_2;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        slider_value = slider.value / 10;
        if (slider_value > 0.45) {
            particleSystem_1.Play();
            particleSystem_2.Play();
        }
        else
        {
            particleSystem_1.Stop();
            particleSystem_2.Stop();
        }
    }
}