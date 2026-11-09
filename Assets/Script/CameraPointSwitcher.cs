using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_WEBGL || UNITY_EDITOR
using Cinemachine;
#endif

/// <summary>
/// 相机点位键盘切换器：按 J 键切换到上一个点位、K 键切换到下一个点位（循环），
/// 相机平滑过渡到目标点位的位姿。不再创建鼠标按钮。
/// 点位来自场景中 CameraPoints 下的所有子物体，新增点位无需改代码。
/// 由主场景脚本（CameraSystem.Start）显式调用 EnsureCreated，形成引用防止 IL2CPP 剥离。
/// </summary>
public class CameraPointSwitcher : MonoBehaviour
{
    public const float TransitionDuration = 1.2f;

    private readonly List<Transform> points = new List<Transform>();
    private int currentIndex = 0;
    private bool pointsLoaded;

    private Transform camCtrl;
    private bool transitioning;
    private TankAerationPreview aeration;

    /// <summary>幂等创建（不依赖 RuntimeInitializeOnLoadMethod，避免场景切换时序问题）。</summary>
    public static void EnsureCreated()
    {
        if (FindObjectOfType<CameraPointSwitcher>() != null) return;
        var go = new GameObject("CameraPointSwitcher");
        DontDestroyOnLoad(go);
        go.AddComponent<CameraPointSwitcher>();
        Debug.Log("[CameraPointSwitcher] EnsureCreated OK");
    }

    private void Start()
    {
        LoadPoints();
    }

    private void LoadPoints()
    {
        var root = GameObject.Find("CameraPoints");
        if (root == null)
        {
            Debug.LogWarning("[CameraPointSwitcher] 场景中未找到 CameraPoints 层级，请先在主场景创建点位");
            return;
        }

        points.Clear();
        for (int i = 0; i < root.transform.childCount; i++)
        {
            points.Add(root.transform.GetChild(i));
        }
        pointsLoaded = points.Count > 0;
        aeration = FindObjectOfType<TankAerationPreview>();
        // 推送点位名称列表，供网页端渲染顶部视角按钮（新增点位无需改代码）
        var sb = new System.Text.StringBuilder();
        sb.Append("{\"type\":\"points\",\"count\":").Append(points.Count).Append(",\"names\":[");
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"').Append(points[i].name).Append('"');
        }
        sb.Append("]}");
        WebGLBridge.SendToWeb(sb.ToString());
    }

    private void Update()
    {
        // 临时诊断：Unity 是否收到任何输入（验证浏览器 → Unity 输入链路）
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            string keys = "";
            if (Input.GetKeyDown(KeyCode.K)) keys += "K;";
            if (Input.GetKeyDown(KeyCode.J)) keys += "J;";
            WebGLBridge.SendToWeb("{\"type\":\"input-raw\",\"any\":\"" + Input.anyKeyDown + "\",\"keys\":\"" + keys + "\",\"mouse0\":\"" + Input.GetMouseButtonDown(0) + "\"}");
        }

        if (!pointsLoaded) return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            SwitchPoint(1);
        }
        else if (Input.GetKeyDown(KeyCode.J))
        {
            SwitchPoint(-1);
        }
    }

    public void SwitchRelativePointFromWeb(int direction)
    {
        if (pointsLoaded && (direction == 1 || direction == -1)) SwitchPoint(direction);
    }

    /// <summary>Switch to the absolute index advertised to the WebGL view picker.</summary>
    public void SwitchPointFromWeb(int index)
    {
        if (!pointsLoaded) return;
        if (index < 0 || index >= points.Count) return;
        if (transitioning)
        {
            StopAllCoroutines();
            transitioning = false;
        }
        currentIndex = index;
        WebGLBridge.SendToWeb("{\"type\":\"point-clicked\",\"source\":\"button\",\"name\":\"" + points[currentIndex].name + "\",\"index\":" + currentIndex + "}");
        GoToPoint(points[currentIndex]);
    }

    public void StopTreatmentPreview()
    {
        if (aeration != null) aeration.End();
        if (transitioning && currentIndex < points.Count && points[currentIndex] != null
            && (points[currentIndex].name == TankAerationPreview.PointName
                || points[currentIndex].name == TankAerationPreview.MixingPointName))
        {
            StopAllCoroutines();
            transitioning = false;
            SyncCameraSystem();
        }
    }

    /// <summary>按方向切换到下一个/上一个点位（循环）。</summary>
    private void SwitchPoint(int dir)
    {
        if (transitioning) return;

        currentIndex = (currentIndex + dir + points.Count) % points.Count;
        Transform target = points[currentIndex];
        WebGLBridge.SendToWeb("{\"type\":\"point-clicked\",\"source\":\"key\",\"name\":\"" + target.name + "\",\"index\":" + currentIndex + "}");
        GoToPoint(target);
    }

    private void GoToPoint(Transform point)
    {
        if (point == null) return;
        if (aeration != null) aeration.End();
        ResolveCameraControl();
        if (camCtrl == null)
        {
            WebGLBridge.SendToWeb("{\"type\":\"point-clicked\",\"error\":\"camCtrl null\"}");
            return;
        }
        WebGLBridge.SendToWeb("{\"type\":\"go-to-point\",\"name\":\"" + point.name + "\",\"cam\":\"" + camCtrl.name + "\",\"toPos\":\"" + point.position + "\",\"toRot\":\"" + point.rotation.eulerAngles + "\"}");
        StartCoroutine(SmoothTo(point.position, point.rotation, point.name));
    }

    private void ResolveCameraControl()
    {
#if UNITY_WEBGL || UNITY_EDITOR
        var brain = FindObjectOfType<CinemachineBrain>();
        if (brain != null && brain.ActiveVirtualCamera != null)
        {
            camCtrl = brain.ActiveVirtualCamera.VirtualCameraGameObject.transform;
            return;
        }
#endif
        var cam = Camera.main;
        camCtrl = cam != null ? cam.transform : null;
    }

    /// <summary>平滑过渡：位置沿"指向目标点"的弧线收敛（Vector3.Slerp），旋转 Quaternion.Slerp，缓动 easeInOutCubic。</summary>
    private IEnumerator SmoothTo(Vector3 toPos, Quaternion toRot, string previewPoint)
    {
        transitioning = true;

        Vector3 fromPos = camCtrl.position;
        Quaternion fromRot = camCtrl.rotation;
        Vector3 fromDir = fromPos - toPos; // 相对目标点的方向

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / TransitionDuration;
            float e = EaseInOutCubic(Mathf.Clamp01(t));

            Vector3 dir = Vector3.Slerp(fromDir, Vector3.zero, e);
            camCtrl.position = toPos + dir;
            camCtrl.rotation = Quaternion.Slerp(fromRot, toRot, e);
            yield return null;
        }

        camCtrl.position = toPos;
        camCtrl.rotation = toRot;

        transitioning = false;
        SyncCameraSystem();
        if (aeration != null)
        {
            if (previewPoint == TankAerationPreview.PointName) aeration.Begin();
            else if (previewPoint == TankAerationPreview.MixingPointName) aeration.BeginMixing();
        }
        WebGLBridge.SendToWeb("{\"type\":\"transition-done\",\"pos\":\"" + camCtrl.position + "\",\"rot\":\"" + camCtrl.rotation.eulerAngles + "\"}");
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
