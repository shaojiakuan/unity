using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_WEBGL || UNITY_EDITOR
using Cinemachine;
#endif

/// <summary>
/// 视角切换器：自动创建右下角"查看锅炉背面"按钮，
/// 点击后相机以当前注视点为中心、绕 Y 轴丝滑旋转 180°，
/// 再点一次转回正面。完全运行时生成，无需修改任何场景。
/// </summary>
public class CameraViewSwitcher : MonoBehaviour
{
    public const float TransitionDuration = 1.2f;

    private Canvas uiCanvas;
    private Button viewButton;
    private Text buttonText;

    private Transform camCtrl;   // 相机控制对象（活动虚拟相机或 MainCamera）
    private Camera mainCam;

    private bool hasViews;
    private Vector3 frontPos, backPos;
    private Quaternion frontRot, backRot;
    private Vector3 pivot;

    private bool transitioning;
    private bool atFront = true;
    private bool uiCreated;

    // 已弃用：转背面功能被"UGUI 点位按钮列表"取代，禁用运行时自启，避免额外创建按钮。
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // private static void AutoCreate() { EnsureCreated(); }

    /// <summary>
    /// 幂等创建：创建组件并同步初始化 UI（不依赖 Start 回调，避免场景切换时 Start 丢失）。
    /// 由确定执行的脚本（LoginClass / CameraSystem）显式调用，同时形成引用链防止 IL2CPP 剥离。
    /// </summary>
    public static void EnsureCreated()
    {
        var existing = FindObjectOfType<CameraViewSwitcher>();
        if (existing != null)
        {
            existing.Init();
            WebGLBridge.SendToWeb("{\"type\":\"switcher\",\"status\":\"existing\",\"ui\":\"" + (existing.uiCreated ? "yes" : "no") + "\"}");
            return;
        }
        var go = new GameObject("CameraViewSwitcher");
        DontDestroyOnLoad(go);
        go.AddComponent<CameraViewSwitcher>().Init();
        Debug.Log("[CameraViewSwitcher] EnsureCreated + Init OK");
        WebGLBridge.SendToWeb("{\"type\":\"switcher\",\"status\":\"created\"}");
    }

    private void Start()
    {
        Init();
    }

    /// <summary>初始化 UI（幂等，可被 Start 或 EnsureCreated 同步调用）。</summary>
    private void Init()
    {
        if (uiCreated) return;
        uiCreated = true;
        EnsureEventSystem();
        CreateUI();
        Debug.Log("[CameraViewSwitcher] UI created: " + (viewButton != null && buttonText != null));
        Invoke(nameof(SendUiProbe), 0.6f);
    }

    /// <summary>布局完成后上报 UI 渲染关键状态（调试探针）。</summary>
    private void SendUiProbe()
    {
        try
        {
            var rt = viewButton != null ? viewButton.GetComponent<RectTransform>() : null;
            var info = "{\"type\":\"switcher\",\"status\":\"ui\",\"canvasActive\":\"" +
                       (uiCanvas != null && uiCanvas.gameObject.activeInHierarchy) +
                       "\",\"sorting\":" + (uiCanvas != null ? uiCanvas.sortingOrder : -1) +
                       ",\"btnPos\":\"" + (rt != null ? rt.anchoredPosition.ToString() : "null") +
                       "\",\"btnSize\":\"" + (rt != null ? rt.sizeDelta.ToString() : "null") +
                       "\",\"canvasSize\":\"" + (uiCanvas != null ? uiCanvas.pixelRect.ToString() : "null") +
                       "\",\"camMain\":\"" + (Camera.main != null ? Camera.main.name : "null") + "\"}";
            WebGLBridge.SendToWeb(info);
        }
        catch (System.Exception e)
        {
            WebGLBridge.SendToWeb("{\"type\":\"switcher\",\"status\":\"probe-error\",\"msg\":\"" + e.Message + "\"}");
        }
    }

    // ---------- UI ----------

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    private Font ResolveFont()
    {
        // 复用场景中已有 UGUI 文本的字体（项目自带的微软雅黑等）
        var existing = FindObjectOfType<Text>();
        if (existing != null && existing.font != null) return existing.font;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void CreateUI()
    {
        // Canvas
        var canvasGo = new GameObject("ViewSwitchCanvas");
        canvasGo.transform.SetParent(transform, false);
        uiCanvas = canvasGo.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 30000;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 按钮
        var btnGo = new GameObject("ViewSwitchButton");
        btnGo.transform.SetParent(canvasGo.transform, false);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24f, 24f);
        rt.sizeDelta = new Vector2(176f, 44f);

        var img = btnGo.AddComponent<Image>();
        img.sprite = CreateRoundedSprite();
        img.color = new Color(0.05f, 0.10f, 0.16f, 0.86f);

        var outline = btnGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.20f, 0.83f, 0.93f, 0.85f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        viewButton = btnGo.AddComponent<Button>();
        viewButton.targetGraphic = img;
        var colors = viewButton.colors;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.85f, 0.95f, 1f, 1f);
        colors.normalColor = Color.white;
        viewButton.colors = colors;

        // 文字
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        buttonText = textGo.AddComponent<Text>();
        buttonText.font = ResolveFont();
        buttonText.text = "查看锅炉背面";
        buttonText.fontSize = 17;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        buttonText.raycastTarget = false;

        viewButton.onClick.AddListener(OnViewButtonClick);
    }

    private Sprite CreateRoundedSprite()
    {
        const int size = 24;
        const float radius = 7f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                float cy = Mathf.Min(y + 0.5f, size - y - 0.5f);
                float alpha = 1f;
                if (cx < radius && cy < radius)
                {
                    float d = Mathf.Sqrt((radius - cx) * (radius - cx) + (radius - cy) * (radius - cy));
                    alpha = Mathf.Clamp01(radius - d + 0.5f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    // ---------- 视角切换 ----------

    private void OnViewButtonClick()
    {
        if (transitioning) return;
        ResolveCameraControl();
        if (camCtrl == null) return;
        if (!hasViews) CaptureViews();
        StartCoroutine(SmoothTransition(atFront ? backPos : frontPos, atFront ? backRot : frontRot));
    }

    private void ResolveCameraControl()
    {
        mainCam = Camera.main;
#if UNITY_WEBGL || UNITY_EDITOR
        var brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null && brain.ActiveVirtualCamera != null)
        {
            camCtrl = brain.ActiveVirtualCamera.VirtualCameraGameObject.transform;
            return;
        }
#endif
        camCtrl = mainCam != null ? mainCam.transform : null;
    }

    /// <summary>记录正面姿态，并以当前视线方向求注视点，推算背面（绕 Y 轴 180°）姿态。</summary>
    private void CaptureViews()
    {
        frontPos = camCtrl.position;
        frontRot = camCtrl.rotation;

        Vector3 fwd = frontRot * Vector3.forward;
        pivot = frontPos + fwd * 8f;
        if (Physics.Raycast(frontPos, fwd, out RaycastHit hit, 60f))
        {
            pivot = hit.point;
        }
        // 以相机高度做水平旋转，避免穿模与俯仰突变
        pivot = new Vector3(pivot.x, frontPos.y, pivot.z);

        Vector3 dir = new Vector3(frontPos.x - pivot.x, 0f, frontPos.z - pivot.z);
        float dist = dir.magnitude;
        if (dist < 0.01f) dist = 8f;
        dir.Normalize();

        backPos = new Vector3(pivot.x, frontPos.y, pivot.z) - dir * dist;
        backRot = Quaternion.LookRotation((pivot - backPos).normalized, Vector3.up);
        hasViews = true;
    }

    private IEnumerator SmoothTransition(Vector3 toPos, Quaternion toRot)
    {
        transitioning = true;

        Vector3 fromPos = camCtrl.position;
        Quaternion fromRot = camCtrl.rotation;
        Vector3 fromDir = new Vector3(fromPos.x - pivot.x, 0f, fromPos.z - pivot.z);
        Vector3 toDir = new Vector3(toPos.x - pivot.x, 0f, toPos.z - pivot.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / TransitionDuration;
            float e = EaseInOutCubic(Mathf.Clamp01(t));

            // 方向沿圆弧（Slerp），高度线性过渡
            Vector3 dir = Vector3.Slerp(fromDir, toDir, e);
            camCtrl.position = new Vector3(pivot.x + dir.x, Mathf.Lerp(fromPos.y, toPos.y, e), pivot.z + dir.z);
            camCtrl.rotation = Quaternion.Slerp(fromRot, toRot, e);
            yield return null;
        }

        camCtrl.position = toPos;
        camCtrl.rotation = toRot;

        transitioning = false;
        atFront = !atFront;
        buttonText.text = atFront ? "查看锅炉背面" : "查看锅炉正面";
        SyncCameraSystem();
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    /// <summary>切换完成后同步 CameraSystem 的 yaw/pitch，避免后续手动旋转跳变。</summary>
    private void SyncCameraSystem()
    {
        var cs = FindObjectOfType<CameraSystem>();
        if (cs != null) cs.SyncRotationFromTransform();
    }
}
