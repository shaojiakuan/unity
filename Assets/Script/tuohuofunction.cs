using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class tuohuofunction : MonoBehaviour
{
    private Slider slider;
    public ParticleSystem flame_first_2;
    public ParticleSystem flame_second_1;
    public ParticleSystem smoke_first_2;
    public ParticleSystem smoke_second_1;
    public ParticleSystem smoke_NOx_first_2;
    public ParticleSystem smoke_NOx_second_1;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        var vel_flame_second_1 = flame_second_1.velocityOverLifetime;
        vel_flame_second_1.radial = slider.value / 10;
        var vel_flame_first_2 = flame_first_2.velocityOverLifetime;
        vel_flame_first_2.radial = slider.value / 10;
        var vel_smoke_first_2 = smoke_first_2.velocityOverLifetime;
        vel_smoke_first_2.radial = slider.value / 10;
        var vel_smoke_second_1 = smoke_second_1.velocityOverLifetime;
        vel_smoke_second_1.radial = slider.value / 10;
        var vel_smoke_NOx_first_2 = smoke_NOx_first_2.velocityOverLifetime;
        vel_smoke_NOx_first_2.radial = slider.value / 10;
        var vel_smoke_NOx_second_1 = smoke_NOx_second_1.velocityOverLifetime;
        vel_smoke_NOx_second_1.radial = slider.value / 10;
    }
}
