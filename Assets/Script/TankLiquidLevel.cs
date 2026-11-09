using UnityEngine;

/// <summary>
/// 矩形内腔的液位显示。Liquid Body 使用中心轴点、单位大小的 Unity Cube。
/// 控制器位于箱底中心，沿局部 Y 轴填充；根对象应保持竖直、正缩放。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class TankLiquidLevel : MonoBehaviour
{
    [Header("液体对象")]
    [Tooltip("仅指定本控制器直接子级的液体 Cube，不要指定水箱外壳。")]
    public Transform liquidBody;

    [Header("内腔尺寸：宽 X / 高 Y / 深 Z")]
    [Tooltip("以本对象的局部单位填写。菜单创建的根对象 Scale 为 1，对应 Unity 的米制单位。")]
    public Vector3 innerSize = new Vector3(1f, 2f, 1f);

    [Header("液位：0 为空，1 为满")]
    [Range(0f, 1f)] public float initialLevel01 = 0.5f;
    [Range(0f, 1f)] public float targetLevel01 = 0.5f;
    [Min(0f)] public float smoothTime = 0.6f;

    [Header("循环演示：接入真实数据后关闭")]
    public bool animateDemo;
    [Range(0f, 1f)] public float demoMinimum01 = 0.2f;
    [Range(0f, 1f)] public float demoMaximum01 = 0.8f;
    [Min(0.1f)] public float demoCycleSeconds = 8f;

    [Header("箱内处理演示")]
    public bool enableTreatmentDemo;

    [Header("水箱外壁透明切换")]
    [Tooltip("指定水箱外壳模型根对象；透明切换会排除本液体对象。")]
    public Transform tankShellRoot;
    public bool enableShellHotkey = true;
    public KeyCode shellTransparencyKey = KeyCode.Q;
    [Range(0f, 1f)] public float shellTransparentAlpha = 0.2f;

    private float currentLevel01;
    private float levelVelocity;
    private float demoStartTime;
    private bool wasDemoEnabled;
    private bool previewDirty = true;
    private Transform cachedBody;
    private Renderer bodyRenderer;
    private TankShellTransparency shellTransparency;
    private float treatmentPreviewLevel = -1f;

    public float CurrentLevel01 { get { return currentLevel01; } }
    public bool IsShellTransparent
    {
        get { return shellTransparency != null && shellTransparency.IsTransparent; }
    }

    public float MaximumHeightMeters
    {
        get { return innerSize.y * transform.TransformVector(Vector3.up).magnitude; }
    }

    public float CurrentHeightMeters
    {
        get { return MaximumHeightMeters * currentLevel01; }
    }

    private void OnEnable()
    {
        RestoreShellMaterials();
        SanitizeSettings();
        currentLevel01 = Application.IsPlaying(gameObject) ? initialLevel01 : targetLevel01;
        levelVelocity = 0f;
        demoStartTime = Time.time;
        wasDemoEnabled = animateDemo;
        ApplyVisual();
    }

    private void OnValidate()
    {
        // OnValidate 可能在加载线程调用，这里只处理字段，不创建对象或修改 Transform。
        SanitizeSettings();
        previewDirty = true;
    }

    private void Start()
    {
        if (Application.IsPlaying(gameObject) && enableTreatmentDemo)
            gameObject.AddComponent<TankTreatmentDemo>();
    }

    private void Update()
    {
        if (!Application.IsPlaying(gameObject))
        {
            if (previewDirty) RefreshPreview();
            return;
        }

        if (enableShellHotkey && shellTransparencyKey != KeyCode.None)
        {
            if (shellTransparencyKey == KeyCode.Q)
            {
                if (Input.GetKeyDown(KeyCode.E)) SetShellTransparent(false);
                else if (Input.GetKeyDown(KeyCode.Q)) SetShellTransparent(true);
            }
            else if (Input.GetKeyDown(shellTransparencyKey))
                ToggleShellTransparency();
        }

        if (animateDemo && !wasDemoEnabled) demoStartTime = Time.time;
        wasDemoEnabled = animateDemo;
        float goal = animateDemo
            ? SampleDemoLevel(Time.time - demoStartTime, demoCycleSeconds, demoMinimum01, demoMaximum01)
            : targetLevel01;
        AdvanceLevel(Time.deltaTime, goal);
    }

    /// <summary>设置 0~1 的目标液位，自动退出循环演示，随后平滑过渡。</summary>
    public void SetLevel01(float level)
    {
        if (!IsFinite(level)) return;
        animateDemo = false;
        targetLevel01 = Mathf.Clamp01(level);
        previewDirty = true;
    }

    /// <summary>可直接接到 0~100 的 UI Slider 或百分比数据。</summary>
    public void SetLevelPercent(float percent)
    {
        if (IsFinite(percent)) SetLevel01(percent / 100f);
    }

    /// <summary>从箱底起算的液位高度，单位为米，超出范围会钳制到空/满。</summary>
    public void SetLevelMeters(float height)
    {
        float maximum = MaximumHeightMeters;
        if (IsFinite(height) && maximum > 0f) SetLevel01(height / maximum);
    }

    /// <summary>立即显示指定液位，便于首次校准和初始化。</summary>
    public void SetLevelInstant01(float level)
    {
        if (!IsFinite(level)) return;
        SetLevel01(level);
        currentLevel01 = targetLevel01;
        levelVelocity = 0f;
        ApplyVisual();
    }

    /// <summary>编辑器预览：立即反映 Inspector 中的目标液位，不运行循环演示。</summary>
    public void RefreshPreview()
    {
        SanitizeSettings();
        previewDirty = false;
        if (!Application.IsPlaying(gameObject))
        {
            currentLevel01 = targetLevel01;
            levelVelocity = 0f;
        }
        ApplyVisual();
    }

    public void ToggleShellTransparency()
    {
        SetShellTransparent(!IsShellTransparent);
    }

    public bool SetShellTransparent(bool transparent)
    {
        if (!transparent)
            return shellTransparency == null || shellTransparency.SetTransparent(false, shellTransparentAlpha);
        if (tankShellRoot == null)
        {
            Debug.LogWarning("请在 TankLiquid 上指定 Tank Shell Root，或选中水箱后使用 Bind Q To Selected Tank 菜单。", this);
            return false;
        }
        if (shellTransparency == null || shellTransparency.Root != tankShellRoot)
        {
            RestoreShellMaterials();
            shellTransparency = new TankShellTransparency(tankShellRoot, transform);
        }
        bool applied = shellTransparency.SetTransparent(true, shellTransparentAlpha);
        if (!applied)
            Debug.LogWarning("绑定的水箱外壳需要使用 Standard 或 Standard (Specular setup) 材质。", this);
        return applied;
    }

    public void RestoreShellMaterials()
    {
        if (shellTransparency == null) return;
        shellTransparency.Dispose();
        shellTransparency = null;
    }

    private void OnDisable()
    {
        RestoreShellMaterials();
    }

    private void OnDestroy()
    {
        RestoreShellMaterials();
    }

    public string GetSetupError()
    {
        if (liquidBody == null) return "请指定 Liquid Body，或通过 Tools/Boiler/Liquid Level 菜单创建。";
        if (liquidBody == transform || liquidBody.parent != transform)
            return "Liquid Body 必须是本对象的直接子级 Cube，不能是水箱外壳或控制器自身。";

        MeshFilter filter = liquidBody.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return "液体对象需要 Mesh Filter 和网格。";
        Bounds bounds = filter.sharedMesh.bounds;
        if (bounds.center.sqrMagnitude > 0.000001f ||
            (bounds.size - Vector3.one).sqrMagnitude > 0.000001f)
            return "液体对象请使用 Unity 创建的单位 Cube；导入水箱模型的轴点和尺寸不能直接用于本组件。";
        if (liquidBody.GetComponent<Renderer>() == null) return "液体对象需要 Mesh Renderer。";

        Vector3 scale = transform.lossyScale;
        if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            return "液体根对象及其父级需要正缩放；建议将液体根对象放在场景根级并保持 Scale 为 1。";
        if (Vector3.Dot(transform.up, Vector3.up) < 0.999f)
            return "液体根对象需要保持 Y 轴竖直。可绕 Y 轴旋转对齐水箱，不要沿 X/Z 轴倾斜。";
        return null;
    }

    public static float SampleDemoLevel(float elapsed, float period, float minimum, float maximum)
    {
        minimum = ClampFinite01(minimum, 0.2f);
        maximum = ClampFinite01(maximum, 0.8f);
        if (minimum > maximum)
        {
            float temporary = minimum;
            minimum = maximum;
            maximum = temporary;
        }
        period = Mathf.Max(0.1f, FiniteOrDefault(period, 8f));
        elapsed = FiniteOrDefault(elapsed, 0f);
        float phase = Mathf.Repeat(elapsed, period) / period;
        float blend = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
        return Mathf.Lerp(minimum, maximum, blend);
    }

    internal void SetTreatmentPreviewLevel(float level01)
    {
        treatmentPreviewLevel = level01;
    }

    private void AdvanceLevel(float deltaTime, float goal)
    {
        goal = ClampFinite01(goal, currentLevel01);
        if (smoothTime <= 0f)
        {
            currentLevel01 = goal;
            levelVelocity = 0f;
        }
        else if (deltaTime > 0f)
        {
            currentLevel01 = Mathf.SmoothDamp(
                currentLevel01, goal, ref levelVelocity, smoothTime, Mathf.Infinity, deltaTime);
        }
        currentLevel01 = Mathf.Clamp01(currentLevel01);
        if (Mathf.Abs(currentLevel01 - goal) < 0.0001f)
        {
            currentLevel01 = goal;
            levelVelocity = 0f;
        }
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (GetSetupError() != null) return;
        if (cachedBody != liquidBody || bodyRenderer == null)
        {
            cachedBody = liquidBody;
            bodyRenderer = liquidBody.GetComponent<Renderer>();
        }

        float displayLevel = treatmentPreviewLevel >= 0f ? treatmentPreviewLevel : currentLevel01;
        float height = Mathf.Max(0.000001f, innerSize.y * displayLevel);
        Vector3 size = new Vector3(innerSize.x, height, innerSize.z);
        // Cube 轴点在中心：中心抬高高度的一半，底部才能始终停留在根对象原点。
        Vector3 center = new Vector3(0f, height * 0.5f, 0f);
        if (liquidBody.localScale != size) liquidBody.localScale = size;
        if (liquidBody.localPosition != center) liquidBody.localPosition = center;
        if (liquidBody.localRotation != Quaternion.identity) liquidBody.localRotation = Quaternion.identity;
        bool visible = displayLevel > 0f;
        if (bodyRenderer.enabled != visible) bodyRenderer.enabled = visible;
    }

    private void SanitizeSettings()
    {
        innerSize.x = Mathf.Max(0.001f, FiniteOrDefault(innerSize.x, 1f));
        innerSize.y = Mathf.Max(0.001f, FiniteOrDefault(innerSize.y, 2f));
        innerSize.z = Mathf.Max(0.001f, FiniteOrDefault(innerSize.z, 1f));
        initialLevel01 = ClampFinite01(initialLevel01, 0.5f);
        targetLevel01 = ClampFinite01(targetLevel01, 0.5f);
        smoothTime = Mathf.Max(0f, FiniteOrDefault(smoothTime, 0.6f));
        demoMinimum01 = ClampFinite01(demoMinimum01, 0.2f);
        demoMaximum01 = ClampFinite01(demoMaximum01, 0.8f);
        demoCycleSeconds = Mathf.Max(0.1f, FiniteOrDefault(demoCycleSeconds, 8f));
        shellTransparentAlpha = ClampFinite01(shellTransparentAlpha, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        if (innerSize.x <= 0f || innerSize.y <= 0f || innerSize.z <= 0f) return;
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.15f, 0.7f, 0.9f, 0.8f);
        Gizmos.DrawWireCube(Vector3.up * innerSize.y * 0.5f, innerSize);
        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static float FiniteOrDefault(float value, float fallback)
    {
        return IsFinite(value) ? value : fallback;
    }

    private static float ClampFinite01(float value, float fallback)
    {
        return Mathf.Clamp01(FiniteOrDefault(value, fallback));
    }
}
