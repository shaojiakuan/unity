using System.Collections.Generic;
using UnityEngine;

public class waterColorBianHua : MonoBehaviour
{
    [Header("目标对象设置")]
    [Tooltip("控制当前物体")]
    public bool controlSelf = true;

    [Tooltip("需要控制的其他GameObjects")]
    public GameObject[] targetObjects;

    [Header("基础设置")]
    [Tooltip("渐变速度")]
    public float changeSpeed = 1.0f;

    [Tooltip("初始颜色")]
    public Color startColor = Color.red;

    [Tooltip("是否同步所有对象")]
    public bool synchronizeAll = true;

    [Header("预设渐变类型")]
    [Tooltip("颜色循环模式")]
    public ColorMode colorMode = ColorMode.Rainbow;

    [Header("自定义渐变")]
    [Tooltip("自定义颜色列表")]
    public Color[] customColors = new Color[] { Color.red, Color.green, Color.blue };

    [Tooltip("渐变是否循环")]
    public bool loop = true;

    [Header("随机化设置")]
    [Tooltip("是否启用随机相位偏移")]
    public bool enableRandomPhase = false;

    [Tooltip("随机相位偏移范围")]
    [Range(0f, 1f)]
    public float randomPhaseRange = 0.5f;

    private List<Renderer> targetRenderers = new List<Renderer>();
    private List<Material> materials = new List<Material>();
    private float hueValue = 0f;
    private int currentColorIndex = 0;
    private float lerpTime = 0f;
    private List<float> phaseOffsets = new List<float>();

    public enum ColorMode
    {
        Rainbow,            // 彩虹色循环
        SmoothTransition,   // 自定义颜色平滑过渡
        Pulse,              // 脉冲效果
        Temperature         // 温度变化（红到蓝）
    }

    void Start()
    {
        InitializeTargets();

        if (materials.Count == 0)
        {
            Debug.LogWarning("没有找到有效的Renderer或Material组件");
            enabled = false;
        }

        // 设置初始颜色
        SetAllColors(startColor);
    }

    void InitializeTargets()
    {
        // 清空列表
        targetRenderers.Clear();
        materials.Clear();
        phaseOffsets.Clear();

        // 添加自身
        if (controlSelf)
        {
            Renderer selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                targetRenderers.Add(selfRenderer);
                materials.Add(selfRenderer.material);
                phaseOffsets.Add(0f);
            }
        }

        // 添加目标对象
        if (targetObjects != null && targetObjects.Length > 0)
        {
            foreach (GameObject obj in targetObjects)
            {
                if (obj != null)
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        targetRenderers.Add(renderer);
                        materials.Add(renderer.material);

                        // 设置相位偏移
                        float phaseOffset = enableRandomPhase ? Random.Range(0f, randomPhaseRange) : 0f;
                        phaseOffsets.Add(phaseOffset);
                    }
                }
            }
        }

        // 如果启用了随机相位，重新计算相位偏移
        if (enableRandomPhase && synchronizeAll)
        {
            for (int i = 0; i < phaseOffsets.Count; i++)
            {
                phaseOffsets[i] = Random.Range(0f, randomPhaseRange);
            }
        }
    }

    void Update()
    {
        if (materials.Count == 0) return;

        switch (colorMode)
        {
            case ColorMode.Rainbow:
                RainbowEffect();
                break;

            case ColorMode.SmoothTransition:
                SmoothTransitionEffect();
                break;

            case ColorMode.Pulse:
                PulseEffect();
                break;

            case ColorMode.Temperature:
                TemperatureEffect();
                break;
        }
    }

    // 彩虹色循环效果
    void RainbowEffect()
    {
        hueValue += Time.deltaTime * changeSpeed * 0.1f;
        if (hueValue > 1f) hueValue = 0f;

        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
            {
                float phase = synchronizeAll ? hueValue : (hueValue + phaseOffsets[i]) % 1f;
                Color rainbowColor = Color.HSVToRGB(phase, 1f, 1f);
                materials[i].color = rainbowColor;
            }
        }
    }

    // 自定义颜色平滑过渡
    void SmoothTransitionEffect()
    {
        if (customColors.Length < 2) return;

        lerpTime += Time.deltaTime * changeSpeed * 0.5f;

        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
            {
                float t;
                if (synchronizeAll)
                {
                    t = Mathf.Sin(lerpTime) * 0.5f + 0.5f;
                }
                else
                {
                    t = Mathf.Sin(lerpTime + phaseOffsets[i] * Mathf.PI * 2f) * 0.5f + 0.5f;
                }

                int nextIndex = (currentColorIndex + 1) % customColors.Length;
                Color lerpedColor = Color.Lerp(customColors[currentColorIndex], customColors[nextIndex], t);
                materials[i].color = lerpedColor;
            }
        }

        // 检查是否需要切换到下一个颜色
        float baseT = Mathf.Sin(lerpTime) * 0.5f + 0.5f;
        if (baseT >= 1f)
        {
            currentColorIndex = (currentColorIndex + 1) % customColors.Length;
            if (!loop && currentColorIndex == customColors.Length - 1)
            {
                enabled = false; // 停止更新
            }
        }
    }

    // 脉冲效果（颜色明暗变化）
    void PulseEffect()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
            {
                float timeOffset = synchronizeAll ? 0f : phaseOffsets[i] * Mathf.PI * 2f;
                float pulse = Mathf.Sin(Time.time * changeSpeed + timeOffset) * 0.3f + 0.7f;
                Color pulseColor = new Color(startColor.r * pulse, startColor.g * pulse, startColor.b * pulse);
                materials[i].color = pulseColor;
            }
        }
    }

    // 温度变化效果（红->黄->绿->蓝）
    void TemperatureEffect()
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
            {
                float timeOffset = synchronizeAll ? 0f : phaseOffsets[i];
                float temperature = Mathf.PingPong(Time.time * changeSpeed * 0.2f + timeOffset, 1f);
                Color tempColor = CalculateTemperatureColor(temperature);
                materials[i].color = tempColor;
            }
        }
    }

    Color CalculateTemperatureColor(float temperature)
    {
        if (temperature < 0.33f)
        {
            // 红到黄
            return Color.Lerp(Color.red, Color.yellow, temperature * 3f);
        }
        else if (temperature < 0.66f)
        {
            // 黄到绿
            return Color.Lerp(Color.yellow, Color.green, (temperature - 0.33f) * 3f);
        }
        else
        {
            // 绿到蓝
            return Color.Lerp(Color.green, Color.blue, (temperature - 0.66f) * 3f);
        }
    }

    // 设置所有对象的颜色
    public void SetAllColors(Color color)
    {
        for (int i = 0; i < materials.Count; i++)
        {
            if (materials[i] != null)
            {
                materials[i].color = color;
            }
        }
    }

    // 设置特定对象的颜色
    public void SetColorAtIndex(int index, Color color)
    {
        if (index >= 0 && index < materials.Count && materials[index] != null)
        {
            materials[index].color = color;
        }
    }

    // 添加新的目标对象
    public void AddTarget(GameObject target)
    {
        if (target == null) return;

        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            targetRenderers.Add(renderer);
            materials.Add(renderer.material);

            float phaseOffset = enableRandomPhase ? Random.Range(0f, randomPhaseRange) : 0f;
            phaseOffsets.Add(phaseOffset);
        }
    }

    // 移除目标对象
    public void RemoveTarget(GameObject target)
    {
        for (int i = 0; i < targetRenderers.Count; i++)
        {
            if (targetRenderers[i].gameObject == target)
            {
                targetRenderers.RemoveAt(i);
                materials.RemoveAt(i);
                phaseOffsets.RemoveAt(i);
                break;
            }
        }
    }

    // 重新初始化目标列表
    [ContextMenu("重新初始化目标")]
    public void ReinitializeTargets()
    {
        InitializeTargets();
    }

    // 在Inspector中测试颜色
    [ContextMenu("测试彩虹效果")]
    public void TestRainbow()
    {
        colorMode = ColorMode.Rainbow;
        changeSpeed = 1f;
        enabled = true;
    }

    [ContextMenu("测试脉冲效果")]
    public void TestPulse()
    {
        colorMode = ColorMode.Pulse;
        changeSpeed = 2f;
        enabled = true;
    }

    [ContextMenu("重置颜色")]
    public void ResetColor()
    {
        SetAllColors(startColor);
    }

    [ContextMenu("切换同步/异步")]
    public void ToggleSynchronization()
    {
        synchronizeAll = !synchronizeAll;
        if (enableRandomPhase && !synchronizeAll)
        {
            // 重新随机化相位
            for (int i = 0; i < phaseOffsets.Count; i++)
            {
                phaseOffsets[i] = Random.Range(0f, randomPhaseRange);
            }
        }
    }

    void OnDestroy()
    {
        // 清理（如果需要创建了材质实例）
        // 注意：如果使用的是共享材质，不应该在这里销毁
    }
}