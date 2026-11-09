using UnityEngine;

public class bujiansu : MonoBehaviour
{
    public ParticleSystem particleSystem; // 粒子系统
    public float initialSpeed = 5f; // 初始速度

    void Start()
    {
        // 获取粒子系统的主要设置模块
        var main = particleSystem.main;

        // 设置粒子的初始速度
        main.startSpeed = initialSpeed;

        // 确保粒子系统在世界坐标系中模拟，防止受到物体移动的影响
        main.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    void Update()
    {
        // 不断地将粒子的速度保持为初始速度
        var particles = new ParticleSystem.Particle[particleSystem.particleCount];
        int numParticlesAlive = particleSystem.GetParticles(particles);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            // 设置每个粒子的速度为初始速度方向
            particles[i].velocity = particles[i].velocity.normalized * initialSpeed;
        }

        // 将更新后的粒子设置回粒子系统
        particleSystem.SetParticles(particles, numParticlesAlive);
    }
}