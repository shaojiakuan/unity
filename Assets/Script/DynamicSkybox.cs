using UnityEngine;

public class DynamicSkybox : MonoBehaviour
{
    [Header("天空盒材质")]
    public Material daySkybox;
    public Material nightSkybox;
    public Material cloudySkybox;

    void Update()
    {
        // 示例：按1，2，3键切换天空盒
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetSkybox(daySkybox);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetSkybox(nightSkybox);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetSkybox(cloudySkybox);
        }
    }

    public void SetSkybox(Material newSkybox)
    {
        if (newSkybox == null) return;

        RenderSettings.skybox = newSkybox;
        // 重要：当动态改变天空盒时，通常需要重新计算环境光照
        DynamicGI.UpdateEnvironment();
    }
}