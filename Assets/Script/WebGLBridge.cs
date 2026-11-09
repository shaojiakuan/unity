using System;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Unity WebGL 与网页的双向通信桥。
///
/// 桥实例会自动创建并常驻（DontDestroyOnLoad），无需挂到任何场景物体上。
///
/// 用法：
///  1. C# → JS：WebGLBridge.SendToWeb("{\"type\":\"xxx\",...}")，
///     网页侧在 window.onUnityMessage(json) 中接收。
///  2. JS → C#：unityInstance.SendMessage("WebGLBridgeInstance",
///     "OnWebCommand", "{\"type\":\"xxx\",...}")，
///     场景脚本订阅 WebGLBridge.WebCommandReceived 事件接收。
/// </summary>
public class WebGLBridge : MonoBehaviour
{
    /// <summary>网页发来的命令（JSON 字符串）。</summary>
    public static event Action<string> WebCommandReceived;

    public static WebGLBridge Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void NotifyWeb(string json);

    [DllImport("__Internal")]
    private static extern int GetUrlParam(string name, IntPtr buffer, int bufferSize);
#else
    // 编辑器 / 非 WebGL 平台下的替身，便于本地调试
    private static void NotifyWeb(string json)
    {
        Debug.Log("[WebGLBridge -> Web] " + json);
    }

    private static int GetUrlParam(string name, IntPtr buffer, int bufferSize)
    {
        return 0;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        // 保持无场景引用的自启脚本不被 IL2CPP 剥离（CameraPointSwitcher 等）
        _ = typeof(CameraPointSwitcher);

        if (Instance != null) return;
        var go = new GameObject("WebGLBridgeInstance");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<WebGLBridge>();
    }

    /// <summary>JS → C# 入口：unityInstance.SendMessage("WebGLBridgeInstance", "OnWebCommand", json)</summary>
    public void OnWebCommand(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        try
        {
            Debug.Log("[Web -> WebGLBridge] " + json);
            WebCommandReceived?.Invoke(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[WebGLBridge] 命令处理异常: " + e.Message);
        }
    }

    /// <summary>C# → JS：推送 JSON 消息给网页。</summary>
    public static void SendToWeb(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        NotifyWeb(json);
    }

    /// <summary>读取网页 URL 查询参数（如 ?user=admin）。非 WebGL 平台返回空字符串。</summary>
    public static string GetUrlQueryParam(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
#if UNITY_WEBGL && !UNITY_EDITOR
        const int capacity = 512;
        var buffer = Marshal.AllocHGlobal(capacity);
        try
        {
            int len = GetUrlParam(name, buffer, capacity);
            if (len <= 0) return string.Empty;
            return Marshal.PtrToStringAnsi(buffer, len);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
#else
        return string.Empty;
#endif
    }

    /// <summary>当前 Web 登录用户名（网页登录成功后由模板推送）。</summary>
    public string WebUserName { get; private set; }

    private void Start()
    {
        // 若网页模板已推送过登录信息，则订阅事件即可；此处登记初始值
        var queryUser = GetUrlQueryParam("user");
        if (!string.IsNullOrEmpty(queryUser)) WebUserName = queryUser;

        WebCommandReceived += OnWebCommandDispatch;
    }

    private void OnWebCommandDispatch(string json)
    {
        // 解析简单的 { "type": "login", "username": "xx" } 命令
        try
        {
            var data = SimpleJson.ParseObject(json);
            if (data == null) return;
            var type = SimpleJson.GetString(data, "type");
            if (type == "login")
            {
                WebUserName = SimpleJson.GetString(data, "username");
                Debug.Log("[WebGLBridge] Web 登录用户: " + WebUserName);
            }
            else if (type == "stopTreatmentPreview")
            {
                var switcher = FindObjectOfType<CameraPointSwitcher>();
                if (switcher != null) switcher.StopTreatmentPreview();
            }
            else if (type == "switchPoint")
            {
                var switcher = FindObjectOfType<CameraPointSwitcher>();
                if (switcher == null)
                {
                    Debug.LogWarning("[WebGLBridge] switchPoint: CameraPointSwitcher 未找到");
                    return;
                }
                // 优先按点位索引直接切换（顶部视角按钮）；无 index 时回退为方向循环切换（J/K）
                var indexStr = SimpleJson.GetString(data, "index");
                if (int.TryParse(indexStr, out var idx))
                {
                    switcher.SwitchPointFromWeb(idx);
                    return;
                }
                int dir = 1;
                var dirStr = SimpleJson.GetString(data, "dir");
                if (int.TryParse(dirStr, out var d) && (d == 1 || d == -1)) dir = d;
                switcher.SwitchRelativePointFromWeb(dir);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[WebGLBridge] JSON 解析失败: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

/// <summary>极简 JSON 辅助（避免引入第三方库，仅解析本模板约定的简单结构）。</summary>
public static class SimpleJson
{
    /// <summary>把形如 {"a":"b","c":"d"} 的 JSON 解析为不区分大小写的字典。</summary>
    public static System.Collections.Generic.Dictionary<string, string> ParseObject(string json)
    {
        var result = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(json)) return null;

        int i = 0;
        SkipWs(json, ref i);
        if (i >= json.Length || json[i] != '{') return null;
        i++;

        while (i < json.Length)
        {
            SkipWs(json, ref i);
            if (i >= json.Length) break;
            if (json[i] == '}') { i++; return result; }
            if (json[i] != '"') return null;
            string key = ReadString(json, ref i);
            SkipWs(json, ref i);
            if (i >= json.Length || json[i] != ':') return null;
            i++;
            SkipWs(json, ref i);
            string value;
            if (i < json.Length && json[i] == '"') value = ReadString(json, ref i);
            else value = ReadPrimitive(json, ref i);
            result[key] = value;
            SkipWs(json, ref i);
            if (i < json.Length && json[i] == ',') { i++; }
        }
        return result;
    }

    public static string GetString(System.Collections.Generic.Dictionary<string, string> data, string key)
    {
        if (data == null) return string.Empty;
        return data.TryGetValue(key, out var v) ? v : string.Empty;
    }

    private static void SkipWs(string s, ref int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
    }

    private static string ReadString(string s, ref int i)
    {
        // 进入时 s[i] == '"'
        i++;
        var sb = new System.Text.StringBuilder();
        while (i < s.Length && s[i] != '"')
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                i++;
                switch (s[i])
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case 'u':
                        if (i + 4 < s.Length &&
                            int.TryParse(s.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture, out var code))
                        {
                            sb.Append((char)code);
                            i += 4;
                        }
                        break;
                    default: sb.Append(s[i]); break;
                }
                i++;
            }
            else
            {
                sb.Append(s[i]);
                i++;
            }
        }
        i++; // 跳过收尾引号
        return sb.ToString();
    }

    private static string ReadPrimitive(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && s[i] != ',' && s[i] != '}' && s[i] != ' ') i++;
        return s.Substring(start, i - start).Trim();
    }
}
