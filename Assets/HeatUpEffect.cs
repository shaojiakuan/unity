using System.Collections;
using System.Collections.Generic;
using UnityEngine; // 必须要有这个，Color 就定义在 UnityEngine 命名空间中

public class HeatUpEffect : MonoBehaviour
{
    public Material material; // 引用材质
    public Color coldColor = Color.gray; // 初始颜色
    public Color hotColor = Color.red; // 热颜色（发红）
    public float heatTime = 5.0f; // 加热时间（秒）

    private float timer = 0f;
    private bool heating = false;

    void Update()
    {
        if (heating)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / heatTime); // 计算时间比例

            // 插值颜色，从冷到热
            Color currentColor = Color.Lerp(coldColor, hotColor, t);
            material.color = currentColor;

            // 增加发光
            material.SetColor("_EmissionColor", currentColor * Mathf.Lerp(0f, 2f, t)); // 随着时间增加发光强度
        }
    }

    public void StartHeating()
    {
        heating = true;
        timer = 0f;
    }
}