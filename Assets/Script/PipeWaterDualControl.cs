using UnityEngine;

/// <summary>
/// 单套水管材质控制：Q切换管壁透明，X切换水流显示
/// 退出播放自动还原为编辑模式默认不透明、无水流状态
/// </summary>
public class PipeWaterDualControl : MonoBehaviour
{
    [Header("目标水管材质")]
    [Tooltip("拖入 Mat_PipeWater")]
    public Material pipeMaterial;

    [Header("快捷键设置")]
    public KeyCode transparentKey = KeyCode.Q;
    public KeyCode flowKey = KeyCode.X;

    [Header("效果参数")]
    [Tooltip("开启透明后的管壁透明度")]
    [Range(0f, 1f)] public float targetAlpha = 0.3f;
    [Tooltip("开启后的水流显示强度")]
    [Range(0f, 1f)] public float targetWaterIntensity = 0.8f;

    // 编辑模式默认基准值
    private const float EDITOR_ALPHA = 1f;
    private const float EDITOR_WATER = 0f;

    private bool _isTransparent;
    private bool _isFlowOn;

    void Update()
    {
        // Q键：切换管壁透明/不透明
        if (Input.GetKeyDown(transparentKey))
        {
            _isTransparent = !_isTransparent;
            pipeMaterial.SetFloat("_PipeAlpha", _isTransparent ? targetAlpha : EDITOR_ALPHA);
        }

        // X键：切换水流开/关
        if (Input.GetKeyDown(flowKey))
        {
            _isFlowOn = !_isFlowOn;
            pipeMaterial.SetFloat("_WaterIntensity", _isFlowOn ? targetWaterIntensity : EDITOR_WATER);
        }
    }

    // 退出播放、禁用物体时自动还原材质
    void OnDisable()
    {
        RestoreEditorDefault();
    }

    void OnDestroy()
    {
        RestoreEditorDefault();
    }

    void OnApplicationQuit()
    {
        RestoreEditorDefault();
    }

    /// <summary>
    /// 强制还原为编辑模式默认值
    /// </summary>
    void RestoreEditorDefault()
    {
        if (pipeMaterial != null)
        {
            pipeMaterial.SetFloat("_PipeAlpha", EDITOR_ALPHA);
            pipeMaterial.SetFloat("_WaterIntensity", EDITOR_WATER);
        }
    }
}
