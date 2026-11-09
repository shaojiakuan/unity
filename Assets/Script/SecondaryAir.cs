using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ¶þ´Î·ç
public class SecondaryAir : MonoBehaviour
{
    private Slider slider;
    public ParticleSystem wind;
    public ParticleSystem flame_first_1;
    public ParticleSystem flame_first_2;
    public ParticleSystem flame_second_1;
    public ParticleSystem flame_second_2;
    public ParticleSystem flame_third;
    public ParticleSystem flame_last;
    public ParticleSystem smoke_first_1;
    public ParticleSystem smoke_first_2;
    public ParticleSystem smoke_second_1;
    public ParticleSystem smoke_second_2;
    public ParticleSystem smoke_third;
    public ParticleSystem smoke_last;
    public ParticleSystem smoke_NOx_first_1;
    public ParticleSystem smoke_NOx_first_2;
    public ParticleSystem smoke_NOx_second_1;
    public ParticleSystem smoke_NOx_second_2;
    public ParticleSystem smoke_NOx_third;
    public ParticleSystem smoke_NOx_last;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        var vel_flame_first_1 = flame_first_1.velocityOverLifetime;
        var vel_flame_first_2 = flame_first_2.velocityOverLifetime;
        var vel_flame_second_1 = flame_second_1.velocityOverLifetime;
        var vel_flame_second_2 = flame_second_2.velocityOverLifetime;
        var vel_flame_third = flame_third.velocityOverLifetime;
        var vel_flame_last = flame_last.velocityOverLifetime;
        var vel_smoke_first_1 = smoke_first_1.velocityOverLifetime;
        var vel_smoke_first_2 = smoke_first_2.velocityOverLifetime;
        var vel_smoke_second_1 = smoke_second_1.velocityOverLifetime;
        var vel_smoke_second_2 = smoke_second_2.velocityOverLifetime;
        var vel_smoke_third = smoke_third.velocityOverLifetime;
        var vel_smoke_last = smoke_last.velocityOverLifetime;

        var vel_smoke_NOx_first_1 = smoke_NOx_first_1.velocityOverLifetime;
        var vel_smoke_NOx_first_2 = smoke_NOx_first_2.velocityOverLifetime;
        var vel_smoke_NOx_second_1 = smoke_NOx_second_1.velocityOverLifetime;
        var vel_smoke_NOx_second_2 = smoke_NOx_second_2.velocityOverLifetime;
        var vel_smoke_NOx_third = smoke_NOx_third.velocityOverLifetime;
        var vel_smoke_NOx_last = smoke_NOx_last.velocityOverLifetime;

        var vel_wind = wind.velocityOverLifetime;
        vel_flame_first_1.y = -slider.value / 10;
        vel_flame_first_2.y = -slider.value / 10;
        vel_flame_second_1.y = -slider.value / 10;
        vel_flame_second_2.y = -slider.value / 10;
        vel_flame_third.y = -slider.value / 10;
        vel_flame_last.y = -slider.value / 10;
        vel_smoke_first_1.y = -slider.value / 10;
        vel_smoke_first_2.y = -slider.value / 10;
        vel_smoke_second_1.y = -slider.value / 10;
        vel_smoke_second_2.y = -slider.value / 10;
        vel_smoke_third.y = -slider.value / 10;
        vel_smoke_last.y = -slider.value / 10;
        vel_wind.y = -slider.value / 10;

        vel_smoke_NOx_first_1.y = -slider.value / 10;
        vel_smoke_NOx_first_2.y = -slider.value / 10;
        vel_smoke_NOx_second_1.y = -slider.value / 10;
        vel_smoke_NOx_second_2.y = -slider.value / 10;
        vel_smoke_NOx_third.y = -slider.value / 10;
        vel_smoke_NOx_last.y = -slider.value / 10;
    }
}
