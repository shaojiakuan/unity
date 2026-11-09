using System.Collections;
using UnityEngine;

public enum AlarmPattern
{
    Alternating,    // 交替闪烁（警车模式）
    Synchronous,    // 同步闪烁
    Sequential,     // 顺序闪烁
    Random,         // 随机闪烁
    Emergency       // 紧急模式（所有灯快速同步）
}

public class ThreeAlarmLightController : MonoBehaviour
{
    [Header("报警灯引用")]
    [SerializeField] private MeshRenderer[] alarmLights = new MeshRenderer[3];  // 三个报警灯

    [Header("材质设置")]
    [SerializeField] private Material lightOnMaterial;    // 亮灯材质
    [SerializeField] private Material lightOffMaterial;   // 灭灯材质
    [SerializeField] private Color alarmColor = Color.red; // 报警颜色

    [Header("报警模式")]
    [SerializeField] private AlarmPattern currentPattern = AlarmPattern.Alternating;
    [SerializeField] private bool isAlarming = false;      // 是否正在报警

    [Header("闪烁参数")]
    [SerializeField] private float baseFlashSpeed = 0.5f;  // 基础闪烁速度
    [SerializeField] private float flashDuration = 2f;     // 每次闪烁持续时间
    [SerializeField] private float sequenceDelay = 0.1f;   // 顺序闪烁时的延迟

    [Header("紧急模式参数")]
    [SerializeField] private float emergencySpeedMultiplier = 3f;  // 紧急模式速度倍数

    private Coroutine alarmCoroutine;
    private Material[] onMaterials;  // 存储动态创建的亮灯材质（用于修改颜色）
    private bool[] lightStates = new bool[3];  // 三个灯的当前状态

    void Start()
    {
        // 初始化数组
        onMaterials = new Material[3];

        // 为每个灯创建带颜色的亮灯材质
        for (int i = 0; i < 3; i++)
        {
            if (alarmLights[i] != null && lightOnMaterial != null)
            {
                // 创建材质实例（避免共享材质）
                onMaterials[i] = new Material(lightOnMaterial);
                onMaterials[i].color = alarmColor;

                // 设置自发光颜色
                if (onMaterials[i].HasProperty("_EmissionColor"))
                {
                    onMaterials[i].SetColor("_EmissionColor", alarmColor * 0.8f);
                }
            }
        }

        // 初始状态：所有灯熄灭
        TurnOffAllLights();
    }

    /// <summary>
    /// 开始报警
    /// </summary>
    public void StartAlarm()
    {
        if (isAlarming) return;

        isAlarming = true;

        // 停止现有的协程
        if (alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
        }

        // 根据模式启动对应的协程
        switch (currentPattern)
        {
            case AlarmPattern.Alternating:
                alarmCoroutine = StartCoroutine(AlternatingPattern());
                break;
            case AlarmPattern.Synchronous:
                alarmCoroutine = StartCoroutine(SynchronousPattern());
                break;
            case AlarmPattern.Sequential:
                alarmCoroutine = StartCoroutine(SequentialPattern());
                break;
            case AlarmPattern.Random:
                alarmCoroutine = StartCoroutine(RandomPattern());
                break;
            case AlarmPattern.Emergency:
                alarmCoroutine = StartCoroutine(EmergencyPattern());
                break;
        }
    }

    /// <summary>
    /// 停止报警
    /// </summary>
    public void StopAlarm()
    {
        isAlarming = false;

        if (alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
            alarmCoroutine = null;
        }

        TurnOffAllLights();
    }

    /// <summary>
    /// 设置报警模式
    /// </summary>
    public void SetPattern(AlarmPattern pattern)
    {
        bool wasAlarming = isAlarming;

        if (wasAlarming)
        {
            StopAlarm();
        }

        currentPattern = pattern;

        if (wasAlarming)
        {
            StartAlarm();
        }
    }

    /// <summary>
    /// 切换报警状态
    /// </summary>
    public void ToggleAlarm()
    {
        if (isAlarming)
        {
            StopAlarm();
        }
        else
        {
            StartAlarm();
        }
    }

    /// <summary>
    /// 交替闪烁模式（经典警车模式）
    /// </summary>
    IEnumerator AlternatingPattern()
    {
        while (isAlarming)
        {
            // 模式1：灯0和灯2亮，灯1灭
            SetLightState(0, true);
            SetLightState(1, false);
            SetLightState(2, true);
            yield return new WaitForSeconds(flashDuration);

            // 模式2：灯0和灯2灭，灯1亮
            SetLightState(0, false);
            SetLightState(1, true);
            SetLightState(2, false);
            yield return new WaitForSeconds(flashDuration);

            // 模式3：所有灯同时亮
            SetLightState(0, true);
            SetLightState(1, true);
            SetLightState(2, true);
            yield return new WaitForSeconds(flashDuration / 2);

            // 模式4：所有灯灭
            SetLightState(0, false);
            SetLightState(1, false);
            SetLightState(2, false);
            yield return new WaitForSeconds(flashDuration / 2);
        }
    }

    /// <summary>
    /// 同步闪烁模式（所有灯同时闪烁）
    /// </summary>
    IEnumerator SynchronousPattern()
    {
        while (isAlarming)
        {
            // 所有灯亮
            TurnOnAllLights();
            yield return new WaitForSeconds(flashDuration);

            // 所有灯灭
            TurnOffAllLights();
            yield return new WaitForSeconds(flashDuration);
        }
    }

    /// <summary>
    /// 顺序闪烁模式（灯依次亮起）
    /// </summary>
    IEnumerator SequentialPattern()
    {
        while (isAlarming)
        {
            // 顺序亮起
            for (int i = 0; i < 3; i++)
            {
                SetLightState(i, true);
                yield return new WaitForSeconds(sequenceDelay);
            }

            // 保持全亮片刻
            yield return new WaitForSeconds(flashDuration / 2);

            // 顺序熄灭
            for (int i = 0; i < 3; i++)
            {
                SetLightState(i, false);
                yield return new WaitForSeconds(sequenceDelay);
            }

            // 间隔
            yield return new WaitForSeconds(flashDuration);
        }
    }

    /// <summary>
    /// 随机闪烁模式
    /// </summary>
    IEnumerator RandomPattern()
    {
        while (isAlarming)
        {
            // 随机设置每个灯的状态
            for (int i = 0; i < 3; i++)
            {
                SetLightState(i, Random.value > 0.5f);
            }

            // 随机间隔
            float randomInterval = Random.Range(flashDuration * 0.5f, flashDuration * 1.5f);
            yield return new WaitForSeconds(randomInterval);
        }
    }

    /// <summary>
    /// 紧急模式（快速同步闪烁）
    /// </summary>
    IEnumerator EmergencyPattern()
    {
        float emergencyFlashDuration = flashDuration / emergencySpeedMultiplier;

        while (isAlarming)
        {
            // 快速亮灭
            for (int i = 0; i < 3; i++)
            {
                TurnOnAllLights();
                yield return new WaitForSeconds(emergencyFlashDuration / 2);

                TurnOffAllLights();
                yield return new WaitForSeconds(emergencyFlashDuration / 2);
            }

            // 短暂暂停
            yield return new WaitForSeconds(emergencyFlashDuration * 2);
        }
    }

    /// <summary>
    /// 设置单个灯的状态
    /// </summary>
    void SetLightState(int lightIndex, bool isOn)
    {
        if (lightIndex >= 0 && lightIndex < 3 && alarmLights[lightIndex] != null)
        {
            lightStates[lightIndex] = isOn;

            // 获取当前所有材质
            Material[] materials = alarmLights[lightIndex].materials;

            // 设置第一个元素（Element 0）的材质
            if (materials.Length > 0)
            {
                materials[0] = isOn ? onMaterials[lightIndex] : lightOffMaterial;
                alarmLights[lightIndex].materials = materials;
            }
        }
    }

    /// <summary>
    /// 打开所有灯
    /// </summary>
    void TurnOnAllLights()
    {
        for (int i = 0; i < 3; i++)
        {
            SetLightState(i, true);
        }
    }

    /// <summary>
    /// 关闭所有灯
    /// </summary>
    void TurnOffAllLights()
    {
        for (int i = 0; i < 3; i++)
        {
            SetLightState(i, false);
        }
    }

    /// <summary>
    /// 设置报警灯颜色
    /// </summary>
    public void SetAlarmColor(Color color)
    {
        alarmColor = color;

        // 更新所有亮灯材质的颜色
        for (int i = 0; i < 3; i++)
        {
            if (onMaterials[i] != null)
            {
                onMaterials[i].color = alarmColor;

                if (onMaterials[i].HasProperty("_EmissionColor"))
                {
                    onMaterials[i].SetColor("_EmissionColor", alarmColor * 0.8f);
                }
            }
        }

        // 如果正在报警，更新显示
        if (isAlarming)
        {
            for (int i = 0; i < 3; i++)
            {
                if (lightStates[i])
                {
                    SetLightState(i, true);
                }
            }
        }
    }

    /// <summary>
    /// 获取报警状态
    /// </summary>
    public bool IsAlarming()
    {
        return isAlarming;
    }

    /// <summary>
    /// 获取当前模式
    /// </summary>
    public AlarmPattern GetCurrentPattern()
    {
        return currentPattern;
    }

    /// <summary>
    /// 设置闪烁速度
    /// </summary>
    public void SetFlashSpeed(float speedMultiplier)
    {
        flashDuration = baseFlashSpeed / Mathf.Max(0.1f, speedMultiplier);

        // 如果正在报警，重新启动以应用新速度
        if (isAlarming)
        {
            bool wasAlarming = isAlarming;
            StopAlarm();
            if (wasAlarming)
            {
                StartAlarm();
            }
        }
    }

    /// <summary>
    /// 测试方法：循环切换所有模式
    /// </summary>
    public void TestAllPatterns()
    {
        StartCoroutine(TestPatternsRoutine());
    }

    IEnumerator TestPatternsRoutine()
    {
        AlarmPattern[] patterns = (AlarmPattern[])System.Enum.GetValues(typeof(AlarmPattern));

        foreach (var pattern in patterns)
        {
            SetPattern(pattern);
            StartAlarm();
            Debug.Log($"测试模式: {pattern}");
            yield return new WaitForSeconds(3f);
        }

        StopAlarm();
    }

    void OnDestroy()
    {
        // 清理动态创建的材质
        foreach (var mat in onMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
    }
}