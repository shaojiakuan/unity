using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class raodong : MonoBehaviour
{
    private Slider slider;
    public ParticleSystem particleSystem; // 在Inspector中分配粒子系统组件
    public ParticleSystem particleSystem1; // 在Inspector中分配粒子系统组件
    public ParticleSystem particleSystem2; // 在Inspector中分配粒子系统组件
    //float noiseValue = 0.0f;
    ParticleSystem.NoiseModule noiseModule;

void Start()
{
    slider = GetComponent<Slider>();

}

void Update()
    {
        // 获取粒子系统的噪声模块  
        var noiseModule = particleSystem.noise;
        // 设置噪声模块的振幅  
        noiseModule.strength = slider.value/100; // 设置振幅，这会影响噪声的强度
        var noiseModule1 = particleSystem1.noise; // 设置噪声模块的振幅  
        noiseModule1.strength = slider.value / 100; // 设置振幅，这会影响噪声的强度
        var noiseModule2 = particleSystem2.noise; // 设置噪声模块的振幅  
        noiseModule2.strength = slider.value / 100; // 设置振幅，这会影响噪声的强度 
    }
}
