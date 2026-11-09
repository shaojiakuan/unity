using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FurnaceNegativePressure : MonoBehaviour
{
    private Slider slider;
    public ParticleSystem flame_1;
    public ParticleSystem flame_2;
    public ParticleSystem flame_3;

    public ParticleSystem smoke_1;
    public ParticleSystem smoke_2;
    public ParticleSystem smoke_3;

    public ParticleSystem smoke_NOx_1;
    public ParticleSystem smoke_NOx_2;
    public ParticleSystem smoke_NOx_3;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        var vel_flame_1 = flame_1.velocityOverLifetime;
        var vel_flame_2 = flame_2.velocityOverLifetime;
        var vel_flame_3 = flame_3.velocityOverLifetime;
        vel_flame_1.x = slider.value / 10;
        vel_flame_2.x = slider.value / 10;
        vel_flame_3.x = slider.value / 10;

        var vel_smoke_1 = smoke_1.velocityOverLifetime;
        var vel_smoke_2 = smoke_2.velocityOverLifetime;
        var vel_smoke_3 = smoke_3.velocityOverLifetime;
        vel_smoke_1.x = slider.value / 10;
        vel_smoke_2.x = slider.value / 10;
        vel_smoke_3.x = slider.value / 10;

        var vel_smoke_NOx_1 = smoke_NOx_1.velocityOverLifetime;
        var vel_smoke_NOx_2 = smoke_NOx_2.velocityOverLifetime;
        var vel_smoke_NOx_3 = smoke_NOx_3.velocityOverLifetime;
        vel_smoke_NOx_1.x = slider.value / 10;
        vel_smoke_NOx_2.x = slider.value / 10;
        vel_smoke_NOx_3.x = slider.value / 10;
    }
}
