using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shortcutkeys : MonoBehaviour
{
    public GameObject pipe; // 需要处理的管道对象
    private Color originalColor; // 用于存储原始颜色
    private Renderer renderer; // 渲染器组件

    void Start()
    {
        renderer = pipe.GetComponent<Renderer>();
        if (renderer != null)
        {
            originalColor = renderer.material.color; // 存储原始颜色
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            SetTransparent();
        }
        else if (Input.GetKey(KeyCode.E))
        {
            SetOpaque();
        }
    }

    private void SetTransparent()
    {
        if (renderer != null)
        {
            Material material = renderer.material;
            if (material != null)
            {
                material.SetFloat("_Mode", 3); // 设置为透明模式
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            Color transparent = new Color32(255, 255, 255, 74); // 透明颜色
            pipe.GetComponent<MeshRenderer>().material.color = transparent;
        }
    }

    private void SetOpaque()
    {
        if (renderer != null)
        {
            Material material = renderer.material;
            if (material != null)
            {
                material.SetFloat("_Mode", 0); // 设置为不透明模式
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }
            renderer.material.color = originalColor; // 恢复原始颜色
        }
    }
}
