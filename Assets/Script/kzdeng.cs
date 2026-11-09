using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class kzdeng : MonoBehaviour
{
    [Header("材质设置")]
    [SerializeField] private int targetElementIndex = 0;  // 要控制的材质元素索引

    [Header("指示灯状态")]
    [SerializeField] private bool isOn = true;
    [SerializeField] private Material onMaterial;     // 亮灯材质
    [SerializeField] private Material offMaterial;    // 灭灯材质

    [Header("闪烁设置")]
    [SerializeField] private bool enableBlink = false;
    [SerializeField] private float blinkInterval = 0.5f;

    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private float blinkTimer = 0f;

    void Start()
    {
        // 获取MeshRenderer组件
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            Debug.LogError("找不到MeshRenderer组件！");
            enabled = false;
            return;
        }

        // 保存原始材质
        if (meshRenderer.materials.Length > targetElementIndex)
        {
            originalMaterial = meshRenderer.materials[targetElementIndex];
        }
        else
        {
            Debug.LogWarning($"材质数组长度{meshRenderer.materials.Length}小于目标索引{targetElementIndex}");
            enabled = false;
            return;
        }

        // 初始化状态
        UpdateIndicator();
    }

    void Update()
    {
        // 处理闪烁效果
        if (enableBlink)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                Toggle();
            }
        }
    }

    /// <summary>
    /// 更新指示灯状态
    /// </summary>
    void UpdateIndicator()
    {
        if (meshRenderer != null)
        {
            // 获取当前所有材质
            Material[] materials = meshRenderer.materials;

            // 确保索引有效
            if (targetElementIndex >= 0 && targetElementIndex < materials.Length)
            {
                // 设置目标元素材质
                materials[targetElementIndex] = isOn ? onMaterial : offMaterial;

                // 应用材质数组
                meshRenderer.materials = materials;
            }
            else
            {
                Debug.LogWarning($"无效的材质索引: {targetElementIndex}");
            }
        }
    }

    /// <summary>
    /// 切换指示灯状态
    /// </summary>
    public void Toggle()
    {
        isOn = !isOn;
        UpdateIndicator();
    }

    /// <summary>
    /// 设置指示灯状态
    /// </summary>
    /// <param name="state">true=亮, false=灭</param>
    public void SetState(bool state)
    {
        isOn = state;
        UpdateIndicator();
    }

    /// <summary>
    /// 开灯
    /// </summary>
    public void TurnOn()
    {
        SetState(true);
    }

    /// <summary>
    /// 关灯
    /// </summary>
    public void TurnOff()
    {
        SetState(false);
    }

    /// <summary>
    /// 启用/禁用闪烁
    /// </summary>
    /// <param name="blink">是否闪烁</param>
    public void SetBlink(bool blink)
    {
        enableBlink = blink;
        blinkTimer = 0f;
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    /// <returns>是否亮灯</returns>
    public bool GetState()
    {
        return isOn;
    }

    /// <summary>
    /// 恢复原始材质
    /// </summary>
    public void RestoreOriginalMaterial()
    {
        if (meshRenderer != null && originalMaterial != null)
        {
            Material[] materials = meshRenderer.materials;
            if (targetElementIndex >= 0 && targetElementIndex < materials.Length)
            {
                materials[targetElementIndex] = originalMaterial;
                meshRenderer.materials = materials;
            }
        }
    }

    /// <summary>
    /// 设置要控制的材质元素索引
    /// </summary>
    /// <param name="index">新的索引值</param>
    public void SetTargetElementIndex(int index)
    {
        if (meshRenderer != null && index >= 0 && index < meshRenderer.materials.Length)
        {
            targetElementIndex = index;
            UpdateIndicator();
        }
        else
        {
            Debug.LogWarning($"无效的索引: {index}");
        }
    }

    /// <summary>
    /// 动态更换材质
    /// </summary>
    /// <param name="newOnMaterial">新的亮灯材质</param>
    /// <param name="newOffMaterial">新的灭灯材质</param>
    public void SetMaterials(Material newOnMaterial, Material newOffMaterial)
    {
        onMaterial = newOnMaterial;
        offMaterial = newOffMaterial;
        UpdateIndicator();
    }

    void OnDestroy()
    {
        // 可选：在脚本销毁时恢复原始材质
        // RestoreOriginalMaterial();
    }
}