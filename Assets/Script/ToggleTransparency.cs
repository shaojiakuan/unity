using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 引入UI命名空间

public class ToggleTransparency : MonoBehaviour
{
    public List<GameObject> parentObjects; // 需要改变透明度的父物体列表
    public Button toggleButton; // 用于切换透明与恢复颜色的按钮

    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>(); // 存储每个材质的原始颜色
    private bool isTransparent = false; // 标记当前物体是否是半透明

    void Start()
    {
        // 存储每个材质的原始颜色
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

        // 绑定按钮点击事件
        toggleButton.onClick.AddListener(ToggleTransparencyAndColor);
    }

    // 切换物体的透明与恢复原始颜色
    void ToggleTransparencyAndColor()
    {
        foreach (var parentObject in parentObjects)
        {
            var renderers = parentObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material material = renderer.material;
                if (material != null)
                {
                    if (!isTransparent)
                    {
                        // 设置材质为半透明
                        material.SetFloat("_Mode", 3); // 3 对应透明模式
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                        // 设置半透明颜色
                        Color transparent = new Color32(250, 250, 250, 74);
                        material.color = transparent;
                    }
                    else
                    {
                        // 恢复原始颜色
                        material.SetFloat("_Mode", 0); // 0 对应不透明模式
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;

                        if (originalColors.ContainsKey(material))
                        {
                            material.color = originalColors[material];
                        }
                    }
                }
            }
        }

        // 切换透明状态
        isTransparent = !isTransparent;
    }
}
