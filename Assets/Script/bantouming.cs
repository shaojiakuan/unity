using System.Collections.Generic;
using UnityEngine;

public class bantouming : MonoBehaviour
{
    public List<GameObject> parentObjects; // 需要改变透明度的父物体列表
    public Color transparentColor = new Color(1f, 1f, 1f, 0.5f); // 通用半透明颜色（Alpha=0.5）

    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
    private bool isTransparent = false;

    void Start()
    {
        // 存储原始颜色
        foreach (var parentObject in parentObjects)
        {
            var renderers = parentObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material material = renderer.material;
                if (material != null && !originalColors.ContainsKey(material))
                {
                    originalColors[material] = material.color;
                }
            }
        }
    }

    void Update()
    {
        // 按下 Q 键切换半透明状态
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetTransparency(!isTransparent);
        }
    }

    // 统一设置透明状态
    void SetTransparency(bool transparent)
    {
        foreach (var parentObject in parentObjects)
        {
            var renderers = parentObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material material = renderer.material;
                if (material != null)
                {
                    if (transparent)
                    {
                        SetMaterialTransparency(material, true);
                    }
                    else
                    {
                        SetMaterialTransparency(material, false);
                    }
                }
            }
        }
        isTransparent = transparent;
    }

    // 设置材质透明模式
    private void SetMaterialTransparency(Material material, bool transparent)
    {
        if (transparent)
        {
            // 启用透明模式
            material.SetFloat("_Mode", 3); // Fade模式
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // 设置半透明颜色（保留原始RGB，仅修改Alpha）
            Color newColor = originalColors[material];
            newColor.a = transparentColor.a;
            material.color = newColor;
        }
        else
        {
            // 恢复不透明模式
            material.SetFloat("_Mode", 0); // Opaque模式
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHABLEND_ON");
            material.renderQueue = -1;

            // 恢复原始颜色
            if (originalColors.ContainsKey(material))
            {
                material.color = originalColors[material];
            }
        }
    }
}