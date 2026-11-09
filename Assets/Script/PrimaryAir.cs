using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Ò»´Î·ç
public class PrimaryAir : MonoBehaviour
{
    private Slider slider;
    public ParticleSystem wind;
    public ParticleSystem fire_1;
    public ParticleSystem fire_2;
    public ParticleSystem fireSmoke_1;
    public ParticleSystem fireSmoke_2;
    public ParticleSystem fireSmoke_NOx_1;
    public ParticleSystem fireSmoke_NOx_2;

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        wind.startSpeed = slider.value / 10;
        fire_1.startSpeed = slider.value / 10;
        fire_2.startSpeed = slider.value / 10;
        fireSmoke_1.startSpeed = slider.value / 10;
        fireSmoke_2.startSpeed = slider.value / 10;
        fireSmoke_NOx_1.startSpeed = slider.value / 10;
        fireSmoke_NOx_2.startSpeed = slider.value / 10;
    }
}
