using System.Collections.Generic;
using UnityEngine;

public class ToggleTransparencyQE : MonoBehaviour
{
    public List<GameObject> parentObjects; // ��Ҫ�ı�͸���ȵĸ������б�
    public Color blueTransparentColor = new Color(0.12f, 0.94f, 0.95f, 0.5f); // ��ɫ͸����ɫ
    public Color transparentColor = new Color(1f, 1f, 1f, 0.29f); // Ĭ��͸����ɫ������������͸����ɫ��

    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>(); // �洢ÿ�����ʵ�ԭʼ��ɫ
    private bool isTransparent = false; // ��ǵ�ǰ�����Ƿ��ǰ�͸��

    void Start()
    {
        // �洢ÿ�����ʵ�ԭʼ��ɫ
        foreach (var parentObject in parentObjects)
        {
            var renderers = parentObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material material = renderer.material;
                if (material != null && !originalColors.ContainsKey(material))
                {
                    if (material.HasProperty("_Color"))
                    {
                        originalColors[material] = material.color;
                    }
                }
            }
        }
    }

    void Update()
    {
        // ���� Q ���л�����ɫ͸��״̬
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetTransparency(true, blueTransparentColor); // ����Ϊ��ɫ͸��
        }

        // ���� E ���ָ�ԭʼ״̬
        if (Input.GetKeyDown(KeyCode.E))
        {
            SetTransparency(false, transparentColor); // �ָ�ԭʼ͸������ɫ
        }
    }

    // ���������͸��״̬
    void SetTransparency(bool transparent, Color color)
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
                        // ����Ϊ͸��ģʽ
                        SetMaterialTransparency(material, true, color);
                    }
                    else
                    {
                        // �ָ�ԭʼ��ɫ�Ͳ�͸��ģʽ
                        SetMaterialTransparency(material, false, color);
                    }
                }
            }
        }

        // �л�͸��״̬
        isTransparent = transparent;
    }

    // ���ò��ʵ�͸����
    private void SetMaterialTransparency(Material material, bool transparent, Color color)
    {
        if (transparent)
        {
            // ���ò���Ϊ͸��ģʽ
            material.SetFloat("_Mode", 3); // 3 = ͸��ģʽ
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // ����͸����ɫΪ�������ɫ����ɫ��������
            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }
        }
        else
        {
            // �ָ�ԭʼ��ɫ�Ͳ�͸��ģʽ
            material.SetFloat("_Mode", 0); // 0 = ��͸��ģʽ
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;

            // �ָ�ԭʼ��ɫ
            if (originalColors.ContainsKey(material) && material.HasProperty("_Color"))
            {
                material.color = originalColors[material];
            }
        }
    }
}
