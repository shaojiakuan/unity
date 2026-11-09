using UnityEngine;

/// <summary>Illustrative treatment sequence layered over the tank's ordinary level display.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-20)]
public sealed class TankTreatmentDemo : MonoBehaviour
{
    private const float FillSeconds = 8f;
    private const float MixEnd = FillSeconds + 10f;
    private const float SettleEnd = MixEnd + 7f;
    private const float DrainEnd = SettleEnd + 8f;
    private const float LowLevel = 0.22f;
    private const float HighLevel = 0.65f;
    private static readonly int TreatmentState = Shader.PropertyToID("_TreatmentState");
    private static readonly int TreatmentTime = Shader.PropertyToID("_TreatmentTime");

    private TankLiquidLevel tank;
    private TankAerationPreview aeration;
    private Renderer water;
    private MaterialPropertyBlock properties;
    private GUIStyle label;
    private GUIStyle button;
    private bool active;
    private bool inView;
    private float startTime;
    private float previewLevel;
    private string stage = "";

    private void Awake()
    {
        tank = GetComponent<TankLiquidLevel>();
        aeration = GetComponent<TankAerationPreview>();
        water = tank != null && tank.liquidBody != null ? tank.liquidBody.GetComponent<Renderer>() : null;
        if (water == null) enabled = false;
    }

    private void Update()
    {
        if (aeration != null && aeration.IsActive)
        {
            inView = false;
            return;
        }
        bool transparent = tank != null && tank.isActiveAndEnabled && tank.IsShellTransparent;
        inView = transparent && IsTankInView();
        if (active && (!transparent || Input.GetKeyDown(KeyCode.E))) StopDemo();
        else if (inView && Input.GetKeyDown(KeyCode.Y)) ToggleDemo();

        if (active) ApplyStage(Time.unscaledTime - startTime);
    }

    private bool IsTankInView()
    {
        Camera camera = Camera.main;
        if (camera == null) return false;
        Vector3 center = transform.TransformPoint(Vector3.up * (tank.innerSize.y * 0.5f));
        if ((camera.transform.position - center).sqrMagnitude > 18f * 18f) return false;
        Vector3 screen = camera.WorldToViewportPoint(center);
        return screen.z > 0f && screen.x >= -0.1f && screen.x <= 1.1f && screen.y >= -0.1f && screen.y <= 1.1f;
    }

    private void ToggleDemo()
    {
        if (active)
        {
            StopDemo();
            return;
        }
        active = true;
        startTime = Time.unscaledTime;
        ApplyStage(0f);
    }

    private void ApplyStage(float elapsed)
    {
        float mix = 0f;
        float inlet = 0f;
        float outlet = 0f;
        previewLevel = HighLevel;
        if (elapsed < FillSeconds)
        {
            stage = "进水";
            inlet = 1f;
            previewLevel = Mathf.Lerp(LowLevel, HighLevel, elapsed / FillSeconds);
        }
        else if (elapsed < MixEnd)
        {
            stage = "箱内处理";
            mix = Mathf.SmoothStep(0.35f, 1f, (elapsed - FillSeconds) / (MixEnd - FillSeconds));
        }
        else if (elapsed < SettleEnd)
        {
            stage = "静置";
            mix = 1f - Mathf.SmoothStep(0f, 1f, (elapsed - MixEnd) / (SettleEnd - MixEnd));
        }
        else if (elapsed < DrainEnd)
        {
            stage = "出水";
            outlet = 1f;
            previewLevel = Mathf.Lerp(HighLevel, LowLevel, (elapsed - SettleEnd) / (DrainEnd - SettleEnd));
        }
        else
        {
            stage = "演示完成";
            previewLevel = LowLevel;
        }

        tank.SetTreatmentPreviewLevel(previewLevel);
        if (inView) SetWaterEffect(new Vector4(mix, inlet, outlet, 1f), elapsed);
    }

    internal void StopDemo()
    {
        if (!active) return;
        active = false;
        if (tank != null) tank.SetTreatmentPreviewLevel(-1f);
        SetWaterEffect(Vector4.zero, 0f);
    }

    private void SetWaterEffect(Vector4 state, float time)
    {
        if (water == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        water.GetPropertyBlock(properties);
        properties.SetVector(TreatmentState, state);
        properties.SetFloat(TreatmentTime, time);
        water.SetPropertyBlock(properties);
    }

    private void OnDisable()
    {
        StopDemo();
    }

    private void OnGUI()
    {
        if (!inView || tank == null || !tank.IsShellTransparent) return;
        if (label == null)
        {
            Font font = Resources.Load<Font>("Fonts/MSYHMONO");
            label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 14 };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 13 };
        }

        float scale = Mathf.Clamp(Screen.width / 1440f, 0.8f, 1.8f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * 0.5f, Screen.height - 156f * scale, 0f),
            Quaternion.identity, new Vector3(scale, scale, 1f));
        GUI.Box(new Rect(-218f, 0f, 436f, 62f), GUIContent.none);
        GUI.Label(new Rect(-205f, 4f, 412f, 22f), "软化水箱 · 工艺演示（模拟）", label);
        string status = active
            ? stage + "  |  模拟液位 " + Mathf.RoundToInt(previewLevel * 100f) + "%"
            : "进水  →  处理  →  静置  →  出水";
        GUI.Label(new Rect(-205f, 30f, 299f, 25f), status, label);
        if (GUI.Button(new Rect(92f, 28f, 116f, 25f), active ? "退出演示 [Y]" : "开始演示 [Y]", button))
            ToggleDemo();
        GUI.matrix = previousMatrix;
    }
}
