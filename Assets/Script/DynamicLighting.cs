using UnityEngine;

public class DynamicLighting : MonoBehaviour
{
    [Header("日光控制")]
    public Light sunLight; // 拖拽你的Directional Light到这里
    public Gradient sunColorOverTime; // 根据时间定义光的颜色
    public AnimationCurve sunIntensityOverTime; // 根据时间定义光强度

    [Header("时间控制")]
    public float timeOfDay = 12.0f; // 0-24小时
    public float timeSpeed = 0.1f;

    [Header("人工光源控制")]
    public Light[] artificialLights; // 锅炉火焰、室内灯等
    private bool lightsAreOn = false;

    void Update()
    {
        // --- 动态控制日光 ---
        UpdateSunLighting();

        // --- 示例：按键盘L开关人工光源 ---
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleArtificialLights();
        }
    }

    void UpdateSunLighting()
    {
        // 更新时间
        timeOfDay += Time.deltaTime * timeSpeed;
        if (timeOfDay >= 24.0f) timeOfDay = 0.0f;

        // 将时间（0-24）转换为0-1的比率
        float timeRatio = timeOfDay / 24.0f;

        // 根据时间和曲线/渐变设置光的属性和角度
        sunLight.color = sunColorOverTime.Evaluate(timeRatio);
        sunLight.intensity = sunIntensityOverTime.Evaluate(timeRatio);

        // 旋转太阳模拟日出日落 (根据时间比率旋转0~180度)
        float sunAngle = Mathf.Lerp(-90, 270, timeRatio);
        sunLight.transform.rotation = Quaternion.Euler(new Vector3(sunAngle, 170, 0));
    }

    void ToggleArtificialLights()
    {
        lightsAreOn = !lightsAreOn;
        foreach (Light light in artificialLights)
        {
            if (light != null)
            {
                light.enabled = lightsAreOn;

                // 如果是点光源或聚光灯，可以添加闪烁效果
                if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    // 可以在这里调用一个协程来模拟闪烁
                    StartCoroutine(FlickerLight(light));
                }
            }
        }
    }

    // 灯光闪烁协程，让锅炉火焰等光源更真实
    System.Collections.IEnumerator FlickerLight(Light light)
    {
        float originalIntensity = light.intensity;
        while (light.enabled) // 只有当灯开着的时候才闪烁
        {
            // 在一个基础强度上随机波动
            light.intensity = originalIntensity + Random.Range(-0.1f, 0.1f);
            // 等待一个随机的时间，模拟不稳定的火焰
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }
        // 循环结束后恢复原强度
        light.intensity = originalIntensity;
    }
}