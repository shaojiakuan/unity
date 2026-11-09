using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class AlarmLightUIController : MonoBehaviour
{
    [System.Serializable]
    public class AlarmElement
    {
        public int elementIndex = 1;      // 材质元素索引（1, 2, 3）
        public bool isActive = true;      // 是否激活
        public Color alarmColor = Color.red; // 报警颜色

        [HideInInspector] public Material originalMaterial;
        [HideInInspector] public Material alarmMaterial;
        [HideInInspector] public bool currentState = false;
    }

    [Header("报警元素配置")]
    [SerializeField]
    private AlarmElement[] alarmElements = new AlarmElement[]
    {
        new AlarmElement { elementIndex = 1, alarmColor = Color.red },    // Element 1 (Material #36)
        new AlarmElement { elementIndex = 2, alarmColor = Color.blue },   // Element 2 (YellowGrid)
        new AlarmElement { elementIndex = 3, alarmColor = Color.red }     // Element 3 (Material #32)
    };

    [Header("报警模式")]
    [SerializeField] private AlarmMode currentMode = AlarmMode.Alternating;
    [SerializeField] private bool autoStart = false;  // 是否自动开始报警

    [Header("闪烁参数")]
    [SerializeField] private float flashInterval = 0.3f;
    [SerializeField] private float onDuration = 0.2f;   // 亮灯持续时间
    [SerializeField] private float offDuration = 0.1f;  // 灭灯持续时间

    [Header("紧急模式")]
    [SerializeField] private float emergencySpeed = 2f;  // 紧急模式速度倍数

    private MeshRenderer meshRenderer;
    private Coroutine alarmCoroutine;
    private bool isAlarming = false;

    public enum AlarmMode
    {
        AllSync,        // 所有元素同步闪烁
        Alternating,    // 交替闪烁（1&3 vs 2）
        Sequential,     // 顺序闪烁 1→2→3
    }

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            Debug.LogError("找不到MeshRenderer组件！");
            enabled = false;
            return;
        }

        InitializeElements();

        if (autoStart)
        {
            StartAlarm();
        }
    }

    /// <summary>
    /// 初始化所有元素
    /// </summary>
    void InitializeElements()
    {
        Material[] materials = meshRenderer.materials;

        foreach (var element in alarmElements)
        {
            if (element.elementIndex >= 0 && element.elementIndex < materials.Length)
            {
                // 保存原始材质
                element.originalMaterial = materials[element.elementIndex];

                // 创建报警材质
                element.alarmMaterial = new Material(element.originalMaterial);

                // 设置报警颜色和自发光
                SetElementColor(element, element.alarmColor);
            }
            else
            {
                Debug.LogWarning($"元素索引 {element.elementIndex} 超出范围！");
                element.isActive = false;
            }
        }
    }

    /// <summary>
    /// 设置元素颜色
    /// </summary>
    void SetElementColor(AlarmElement element, Color color)
    {
        if (element.alarmMaterial != null)
        {
            element.alarmMaterial.color = color;

            // 启用自发光
            if (element.alarmMaterial.HasProperty("_EmissionColor"))
            {
                element.alarmMaterial.SetColor("_EmissionColor", color * 1.5f);
                element.alarmMaterial.EnableKeyword("_EMISSION");
            }
            else if (element.alarmMaterial.HasProperty("_Emission"))
            {
                element.alarmMaterial.SetColor("_Emission", color * 1.5f);
            }
        }
    }

    /// <summary>
    /// 开始报警
    /// </summary>
    public void StartAlarm()
    {
        if (isAlarming || meshRenderer == null) return;

        isAlarming = true;

        if (alarmCoroutine != null)
        {
            StopCoroutine(alarmCoroutine);
        }

        alarmCoroutine = StartCoroutine(AlarmRoutine());
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

        // 关闭所有元素
        TurnOffAllElements();
    }

    /// <summary>
    /// 报警主协程
    /// </summary>
    IEnumerator AlarmRoutine()
    {
        while (isAlarming)
        {
            switch (currentMode)
            {
                case AlarmMode.AllSync:
                    yield return StartCoroutine(AllSyncPattern());
                    break;

                case AlarmMode.Alternating:
                    yield return StartCoroutine(AlternatingPattern());
                    break;

                case AlarmMode.Sequential:
                    yield return StartCoroutine(SequentialPattern());
                    break;

                //case AlarmMode.Emergency:
                //    yield return StartCoroutine(EmergencyPattern());
                //    break;

                //case AlarmMode.TestPattern:
                //    yield return StartCoroutine(TestPattern());
                //    break;
            }
        }
    }

    /// <summary>
    /// 所有元素同步闪烁
    /// </summary>
    IEnumerator AllSyncPattern()
    {
        // 所有元素亮
        SetAllElementsState(true);
        yield return new WaitForSeconds(onDuration);

        // 所有元素灭
        SetAllElementsState(false);
        yield return new WaitForSeconds(offDuration);
    }

    /// <summary>
    /// 交替闪烁（警车模式）
    /// Element 1&3 vs Element 2
    /// </summary>
    IEnumerator AlternatingPattern()
    {
        // 阶段1：Element 1&3亮，Element 2灭
        SetElementState(0, true);  // Element 1
        SetElementState(1, false); // Element 2
        SetElementState(2, true);  // Element 3
        yield return new WaitForSeconds(flashInterval);

        // 阶段2：Element 1&3灭，Element 2亮
        SetElementState(0, false); // Element 1
        SetElementState(1, true);  // Element 2
        SetElementState(2, false); // Element 3
        yield return new WaitForSeconds(flashInterval);

        // 阶段3：所有元素亮
        SetAllElementsState(true);
        yield return new WaitForSeconds(flashInterval / 2);

        // 阶段4：所有元素灭
        SetAllElementsState(false);
        yield return new WaitForSeconds(flashInterval / 2);
    }

    /// <summary>
    /// 顺序闪烁
    /// </summary>
    IEnumerator SequentialPattern()
    {
        // 从左到右依次亮起
        for (int i = 0; i < alarmElements.Length; i++)
        {
            if (alarmElements[i].isActive)
            {
                SetElementState(i, true);
                yield return new WaitForSeconds(flashInterval / 3);
            }
        }

        // 保持全亮
        yield return new WaitForSeconds(onDuration);

        // 从左到右依次熄灭
        for (int i = 0; i < alarmElements.Length; i++)
        {
            if (alarmElements[i].isActive)
            {
                SetElementState(i, false);
                yield return new WaitForSeconds(flashInterval / 3);
            }
        }

        // 间隔
        yield return new WaitForSeconds(offDuration);
    }

    /// <summary>
    /// 紧急模式（快速闪烁）
    /// </summary>
    IEnumerator EmergencyPattern()
    {
        float emergencyInterval = flashInterval / emergencySpeed;

        // 快速闪烁3次
        for (int flashCount = 0; flashCount < 3; flashCount++)
        {
            SetAllElementsState(true);
            yield return new WaitForSeconds(emergencyInterval / 2);

            SetAllElementsState(false);
            yield return new WaitForSeconds(emergencyInterval / 2);
        }

        // 短暂停顿
        yield return new WaitForSeconds(emergencyInterval * 2);
    }

    /// <summary>
    /// 测试模式（显示所有状态）
    /// </summary>
    IEnumerator TestPattern()
    {
        // 单个测试每个元素
        for (int i = 0; i < alarmElements.Length; i++)
        {
            if (alarmElements[i].isActive)
            {
                SetElementState(i, true);
                yield return new WaitForSeconds(0.5f);
                SetElementState(i, false);
            }
        }

        // 全亮测试
        SetAllElementsState(true);
        yield return new WaitForSeconds(1f);
        SetAllElementsState(false);
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 设置单个元素状态
    /// </summary>
    void SetElementState(int elementIndex, bool isOn)
    {
        if (elementIndex >= 0 && elementIndex < alarmElements.Length)
        {
            var element = alarmElements[elementIndex];
            if (element.isActive)
            {
                element.currentState = isOn;
                UpdateElementMaterial(element);
            }
        }
    }

    /// <summary>
    /// 设置所有元素状态
    /// </summary>
    void SetAllElementsState(bool isOn)
    {
        foreach (var element in alarmElements)
        {
            if (element.isActive)
            {
                element.currentState = isOn;
                UpdateElementMaterial(element);
            }
        }
    }

    /// <summary>
    /// 关闭所有元素
    /// </summary>
    void TurnOffAllElements()
    {
        foreach (var element in alarmElements)
        {
            element.currentState = false;
            UpdateElementMaterial(element);
        }
    }

    /// <summary>
    /// 更新元素材质
    /// </summary>
    void UpdateElementMaterial(AlarmElement element)
    {
        if (meshRenderer != null && element.alarmMaterial != null && element.originalMaterial != null)
        {
            Material[] materials = meshRenderer.materials;

            if (element.elementIndex >= 0 && element.elementIndex < materials.Length)
            {
                materials[element.elementIndex] = element.currentState ?
                    element.alarmMaterial : element.originalMaterial;

                meshRenderer.materials = materials;
            }
        }
    }

    /// <summary>
    /// 更新所有元素材质
    /// </summary>
    void UpdateAllElements()
    {
        if (meshRenderer == null) return;

        Material[] materials = meshRenderer.materials;
        bool changed = false;

        foreach (var element in alarmElements)
        {
            if (element.elementIndex >= 0 && element.elementIndex < materials.Length)
            {
                materials[element.elementIndex] = element.currentState ?
                    element.alarmMaterial : element.originalMaterial;
                changed = true;
            }
        }

        if (changed)
        {
            meshRenderer.materials = materials;
        }
    }

    /// <summary>
    /// 设置报警模式
    /// </summary>
    public void SetAlarmMode(AlarmMode newMode)
    {
        bool wasAlarming = isAlarming;

        if (wasAlarming)
        {
            StopAlarm();
        }

        currentMode = newMode;

        if (wasAlarming)
        {
            StartAlarm();
        }
    }

    /// <summary>
    /// 设置元素报警颜色
    /// </summary>
    public void SetElementColor(int elementIndex, Color color)
    {
        if (elementIndex >= 0 && elementIndex < alarmElements.Length)
        {
            alarmElements[elementIndex].alarmColor = color;
            SetElementColor(alarmElements[elementIndex], color);

            // 如果该元素当前是亮的，更新显示
            if (alarmElements[elementIndex].currentState)
            {
                UpdateElementMaterial(alarmElements[elementIndex]);
            }
        }
    }

    /// <summary>
    /// 设置所有元素颜色
    /// </summary>
    public void SetAllElementsColor(Color color)
    {
        foreach (var element in alarmElements)
        {
            element.alarmColor = color;
            SetElementColor(element, color);
        }

        UpdateAllElements();
    }

    /// <summary>
    /// 设置闪烁速度
    /// </summary>
    public void SetFlashSpeed(float speedMultiplier)
    {
        flashInterval = 0.5f / Mathf.Max(0.1f, speedMultiplier);
        onDuration = flashInterval * 0.7f;
        offDuration = flashInterval * 0.3f;
    }

    /// <summary>
    /// 获取报警状态
    /// </summary>
    public bool IsAlarming()
    {
        return isAlarming;
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
    /// 手动触发一次闪烁
    /// </summary>
    public void ManualFlash()
    {
        if (isAlarming) return;

        StartCoroutine(SingleFlashRoutine());
    }

    IEnumerator SingleFlashRoutine()
    {
        SetAllElementsState(true);
        yield return new WaitForSeconds(0.2f);
        SetAllElementsState(false);
    }

    void OnDestroy()
    {
        // 清理动态材质
        foreach (var element in alarmElements)
        {
            if (element.alarmMaterial != null)
            {
                Destroy(element.alarmMaterial);
            }
        }
    }
}