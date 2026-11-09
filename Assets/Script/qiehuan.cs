using UnityEngine;
using System.Collections;

public class qiehuan : MonoBehaviour
{
    [Header("粒子系统设置")]
    public ParticleSystem[] particleSystems = new ParticleSystem[4];
    public KeyCode hideKey = KeyCode.M;
    public KeyCode showKey = KeyCode.N;
    public KeyCode toggleKey = KeyCode.B;

    private Renderer[] particleRenderers;

    void Start()
    {
        // 初始化渲染器数组
        particleRenderers = new Renderer[particleSystems.Length];

        // 获取每个粒子系统的渲染器组件
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                particleRenderers[i] = particleSystems[i].GetComponent<Renderer>();
                // 确保开始时粒子系统是可见的
                if (particleRenderers[i] != null)
                {
                    particleRenderers[i].enabled = true;
                }
            }
        }

        Debug.Log($"已初始化 {particleSystems.Length} 个粒子系统");
    }

    void Update()
    {
        // 按下H键隐藏所有粒子系统
        if (Input.GetKeyDown(hideKey))
        {
            HideAllParticleSystems();
        }

        // 按下S键显示所有粒子系统
        if (Input.GetKeyDown(showKey))
        {
            ShowAllParticleSystems();
        }

        // 按下T键切换所有粒子系统的显示状态
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleAllParticleSystems();
        }

        // 数字键1-4控制单个粒子系统
        for (int i = 0; i < Mathf.Min(4, particleSystems.Length); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ToggleSingleParticleSystem(i);
            }
        }
    }

    // 隐藏所有粒子系统
    public void HideAllParticleSystems()
    {
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            if (particleRenderers[i] != null)
            {
                particleRenderers[i].enabled = false;
            }
        }
        Debug.Log("所有粒子系统已隐藏");
    }

    // 显示所有粒子系统
    public void ShowAllParticleSystems()
    {
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            if (particleRenderers[i] != null)
            {
                particleRenderers[i].enabled = true;
            }
        }
        Debug.Log("所有粒子系统已显示");
    }

    // 切换所有粒子系统的显示状态
    public void ToggleAllParticleSystems()
    {
        bool allHidden = true;

        for (int i = 0; i < particleRenderers.Length; i++)
        {
            if (particleRenderers[i] != null)
            {
                particleRenderers[i].enabled = !particleRenderers[i].enabled;
                if (particleRenderers[i].enabled)
                {
                    allHidden = false;
                }
            }
        }

        Debug.Log("所有粒子系统状态: " + (allHidden ? "隐藏" : "显示"));
    }

    // 切换单个粒子系统的显示状态
    public void ToggleSingleParticleSystem(int index)
    {
        if (index >= 0 && index < particleRenderers.Length && particleRenderers[index] != null)
        {
            particleRenderers[index].enabled = !particleRenderers[index].enabled;
            Debug.Log($"粒子系统 {index + 1} 状态: " + (particleRenderers[index].enabled ? "显示" : "隐藏"));
        }
    }

    // 隐藏单个粒子系统
    public void HideSingleParticleSystem(int index)
    {
        if (index >= 0 && index < particleRenderers.Length && particleRenderers[index] != null)
        {
            particleRenderers[index].enabled = false;
            Debug.Log($"粒子系统 {index + 1} 已隐藏");
        }
    }

    // 显示单个粒子系统
    public void ShowSingleParticleSystem(int index)
    {
        if (index >= 0 && index < particleRenderers.Length && particleRenderers[index] != null)
        {
            particleRenderers[index].enabled = true;
            Debug.Log($"粒子系统 {index + 1} 已显示");
        }
    }

    // 获取粒子系统的当前状态
    public bool IsParticleSystemVisible(int index)
    {
        if (index >= 0 && index < particleRenderers.Length && particleRenderers[index] != null)
        {
            return particleRenderers[index].enabled;
        }
        return false;
    }
}