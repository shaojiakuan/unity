using UnityEngine;
using System.Collections.Generic;

public class ToggleParticleSystem : MonoBehaviour
{
    [Header("粒子系统列表")]
    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();

    [Header("其他组件列表")]
    public List<Component> components = new List<Component>();

    [Header("游戏对象列表")]
    public List<GameObject> gameObjects = new List<GameObject>();

    [Header("启用状态")]
    [SerializeField] private bool isActive = true;

    [Header("按键设置")]
    public KeyCode toggleKey = KeyCode.H; // 改为H键

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) // 使用H键触发
        {
            ToggleAll();
        }
    }

    void ToggleAll()
    {
        isActive = !isActive;

        // 切换所有粒子系统
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                if (isActive)
                {
                    ps.Play();
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null) renderer.enabled = true;
                }
                else
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    var renderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null) renderer.enabled = false;
                }
            }
        }

        // 切换其他组件
        foreach (var component in components)
        {
            if (component == null) continue;

            // 处理不同类型的组件
            if (component is Behaviour behaviour)
            {
                behaviour.enabled = isActive;
            }
            else if (component is Renderer renderer)
            {
                renderer.enabled = isActive;
            }
            else if (component is Collider collider)
            {
                collider.enabled = isActive;
            }
            else if (component is Light light)
            {
                light.enabled = isActive;
            }
        }

        // 切换游戏对象的激活状态
        foreach (var go in gameObjects)
        {
            if (go != null)
            {
                go.SetActive(isActive);
            }
        }

        Debug.Log($"按H键触发: 所有效果已{(isActive ? "显示" : "隐藏")}");
    }

    // 编辑器辅助方法
#if UNITY_EDITOR
    [ContextMenu("添加当前游戏对象")]
    void AddCurrentGameObject()
    {
        if (gameObject != null)
        {
            gameObjects.Add(gameObject);
        }
    }
    
    [ContextMenu("添加所有子对象的粒子系统")]
    void AddAllChildParticleSystems()
    {
        particleSystems.Clear();
        particleSystems.AddRange(GetComponentsInChildren<ParticleSystem>());
    }
#endif
}