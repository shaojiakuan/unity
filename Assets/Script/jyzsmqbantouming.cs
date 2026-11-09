using UnityEngine;

public class jyzsmqbantouming : MonoBehaviour
{
    [Header("材质设置")]
    [Tooltip("拖拽这里指定要切换的新材质")]
    public Material newMaterial; // 通过Inspector指定的新材质

    private Material originalMaterial; // 保存原始材质
    private Renderer objectRenderer; // 物体的Renderer组件

    void Start()
    {
        // 获取物体上的Renderer组件
        objectRenderer = GetComponent<Renderer>();

        // 检查是否找到了Renderer组件
        if (objectRenderer == null)
        {
            Debug.LogError("未找到Renderer组件！请确保脚本挂载在具有Renderer组件的物体上。");
            return;
        }

        // 保存原始材质
        originalMaterial = objectRenderer.material;

        // 检查是否指定了新材质
        if (newMaterial == null)
        {
            Debug.LogWarning("未指定新材质！请通过Inspector拖拽指定材质。");
        }
    }

    void Update()
    {
        // 检测R键按下，切换为新材质
        if (Input.GetKeyDown(KeyCode.R))
        {
            SwitchToNewMaterial();
        }

        // 检测T键按下，还原为原始材质
        if (Input.GetKeyDown(KeyCode.T))
        {
            RevertToOriginalMaterial();
        }
    }

    /// <summary>
    /// 切换到新材质
    /// </summary>
    public void SwitchToNewMaterial()
    {
        if (newMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = newMaterial;
            Debug.Log("已切换到新材质");
        }
        else
        {
            Debug.LogWarning("无法切换材质：新材质未指定或Renderer组件缺失");
        }
    }

    /// <summary>
    /// 还原为原始材质
    /// </summary>
    public void RevertToOriginalMaterial()
    {
        if (originalMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = originalMaterial;
            Debug.Log("已还原为原始材质");
        }
        else
        {
            Debug.LogWarning("无法还原材质：原始材质未保存或Renderer组件缺失");
        }
    }

    /// <summary>
    /// 通过代码设置新材质（可选功能）
    /// </summary>
    /// <param name="material">要设置的新材质</param>
    public void SetNewMaterial(Material material)
    {
        newMaterial = material;
    }

    /// <summary>
    /// 在脚本禁用或物体销毁时，如果需要可以还原材质
    /// </summary>
    void OnDisable()
    {
        // 可选：在脚本禁用时自动还原材质
        // RevertToOriginalMaterial();
    }
}