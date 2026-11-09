using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing; // 需要引入这个命名空间

public class DynamicPostProcessing : MonoBehaviour
{
    [Header("后处理Volume")]
    public PostProcessVolume globalVolume; // 拖拽你的全局Post Process Volume到这里

    // 后处理配置文件引用
    private ColorGrading colorGrading;
    private Bloom bloom;
    private Vignette vignette;

    [Header("动态效果参数")]
    public bool dangerZone = false;
    public float transitionSpeed = 1.0f;

    void Start()
    {
        // 从Volume中获取配置文件的引用
        if (globalVolume != null)
        {
            globalVolume.profile.TryGetSettings(out colorGrading);
            globalVolume.profile.TryGetSettings(out bloom);
            globalVolume.profile.TryGetSettings(out vignette);
        }
    }

    void Update()
    {
        // 示例：按D键切换“危险区域”效果
        if (Input.GetKeyDown(KeyCode.D))
        {
            dangerZone = !dangerZone;
        }

        // 根据状态平滑过渡后处理效果
        UpdatePostEffects();
    }

    void UpdatePostEffects()
    {
        if (colorGrading == null || bloom == null) return;

        float targetTemperature = dangerZone ? 30f : 0f; // 危险区偏蓝（正值偏黄，负值偏蓝）
        float targetSaturation = dangerZone ? -20f : 0f;
        float targetBloomIntensity = dangerZone ? 15f : 5f; // 危险区Bloom更强烈

        // 使用Lerp平滑过渡
        colorGrading.temperature.value = Mathf.Lerp(colorGrading.temperature.value, targetTemperature, Time.deltaTime * transitionSpeed);
        colorGrading.saturation.value = Mathf.Lerp(colorGrading.saturation.value, targetSaturation, Time.deltaTime * transitionSpeed);
        bloom.intensity.value = Mathf.Lerp(bloom.intensity.value, targetBloomIntensity, Time.deltaTime * transitionSpeed);

        // 可以同样控制暗角，在危险时增加
        if (vignette != null)
        {
            float targetVignetteIntensity = dangerZone ? 0.4f : 0.2f;
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetVignetteIntensity, Time.deltaTime * transitionSpeed);
        }
    }
}