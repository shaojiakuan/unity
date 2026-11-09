using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 读取桥接 JSON，或从云春设备接口按测点 ID 读取毫米液位。
/// 也可关闭轮询，通过 ApplyCloudJson 接收网页 / WebSocket 转发的数据。
/// </summary>
[DisallowMultipleComponent]
public sealed class TankLiquidCloudClient : MonoBehaviour
{
    public enum ResponseFormat { BridgeJson, YunChunDeviceValuesMillimeters }

    [Header("液位对象")]
    public TankLiquidLevel tankLiquid;

    [Header("液位接口")]
    public bool pollAutomatically = true;
    [Tooltip("填写液位接口的完整 HTTP/HTTPS 地址。")]
    public string apiUrl = "";
    public ResponseFormat responseFormat = ResponseFormat.BridgeJson;
    [Tooltip("云春设备接口 data 数组中的测点 ID。该模式将 value 按毫米换算为米。")]
    [Min(1)] public int deviceValueId = 32;
    [Min(0.2f)] public float requestInterval = 1f;
    [Min(1)] public int requestTimeout = 5;
    [Tooltip("允许的源数据年龄。应大于设备实际采集 / 上报间隔，并确保设备与电脑时间准确。")]
    [Min(1f)] public float maximumDataAge = 30f;

    [Header("连接信息：由组件更新")]
    [SerializeField] private string connectionStatus = "尚未接收";
    [SerializeField] private string lastError = "";
    [SerializeField] private float latestLevelMeters;
    [SerializeField] private long latestSampleUnixMs;

    public string ConnectionStatus { get { return connectionStatus; } }
    public string LastError { get { return lastError; } }
    public bool HasReceivedData { get; private set; }
    public bool HasFreshData
    {
        get
        {
            return HasReceivedData && connectionStatus == "数据正常" &&
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - latestSampleUnixMs <= MaximumAgeMilliseconds;
        }
    }

    private UnityWebRequest activeRequest;
    private Coroutine polling;
    private static readonly string[] SourceTimeFormats = {
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
    };
    private long MaximumAgeMilliseconds
    {
        get { return (long)(SafePositive(maximumDataAge, 30f, 1f) * 1000.0); }
    }

    private void OnEnable()
    {
        if (tankLiquid == null) tankLiquid = GetComponent<TankLiquidLevel>();
        HasReceivedData = false;
        latestSampleUnixMs = 0;
        connectionStatus = "等待数据";
        lastError = "";
        if (tankLiquid == null)
        {
            Reject("配置错误", "请将 Tank Liquid 指向已有的 TankLiquidLevel 组件。");
            return;
        }
        // 等待网络或断线时保持原液位，停止模拟数据循环。
        tankLiquid.animateDemo = false;
        if (!pollAutomatically) return;
        Uri uri;
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            Reject("等待配置接口", "Api Url 需要填写自己后端的完整 HTTP/HTTPS 地址。");
            return;
        }
        polling = StartCoroutine(Poll());
    }

    private void OnDisable()
    {
        if (polling != null) StopCoroutine(polling);
        polling = null;
        if (activeRequest != null)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            activeRequest = null;
        }
    }

    private void Update()
    {
        if (HasReceivedData && connectionStatus == "数据正常" && !HasFreshData)
            Reject("数据过期", "数据已超过允许年龄，保持最后一次有效液位。");
    }

    private IEnumerator Poll()
    {
        while (true)
        {
            // 上一个请求结束后再等待间隔，不叠加请求；Time.timeScale=0 时仍可收数。
            activeRequest = UnityWebRequest.Get(apiUrl);
            activeRequest.timeout = Mathf.Max(1, requestTimeout);
            activeRequest.SetRequestHeader("Accept", "application/json");
            yield return activeRequest.SendWebRequest();
            if (activeRequest.result == UnityWebRequest.Result.Success)
                TryApplyPolledJson(activeRequest.downloadHandler.text);
            else
                Reject("请求失败", "液位接口请求失败：" + activeRequest.error);
            activeRequest.Dispose();
            activeRequest = null;
            yield return new WaitForSecondsRealtime(SafePositive(requestInterval, 1f, 0.2f));
        }
    }

    private bool TryApplyPolledJson(string json)
    {
        if (responseFormat == ResponseFormat.BridgeJson) return TryApplyCloudJson(json);
        if (!isActiveAndEnabled || tankLiquid == null) return false;
        try
        {
            JObject payload;
            using (var reader = new JsonTextReader(new StringReader(json ?? "")))
            {
                reader.DateParseHandling = DateParseHandling.None;
                payload = JObject.Load(reader, new JsonLoadSettings {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (reader.Read()) return Reject("数据无效", "云春接口响应包含多余内容。");
            }
            JToken code = payload["code"];
            var rows = payload["data"] as JArray;
            if (code?.Type != JTokenType.Integer || code.Value<long>() != 200 || rows == null)
                return Reject("数据无效", "云春接口需要返回 code=200 和 data 数组。");

            JObject reading = null;
            foreach (JToken entry in rows)
            {
                var row = entry as JObject;
                JToken id = row?["id"];
                if (id?.Type != JTokenType.Integer || id.Value<long>() != deviceValueId) continue;
                if (reading != null) return Reject("数据无效", "云春接口返回了重复的液位测点 ID。");
                reading = row;
            }
            if (reading == null)
                return Reject("测点未找到", "云春接口中没有液位测点 ID " + deviceValueId + "。");

            JToken value = reading["value"];
            double millimeters;
            if (value?.Type == JTokenType.Integer || value?.Type == JTokenType.Float)
                millimeters = value.Value<double>();
            else if (value?.Type != JTokenType.String || !double.TryParse(value.Value<string>(),
                NumberStyles.Float, CultureInfo.InvariantCulture, out millimeters))
                return Reject("数据无效", "云春液位 value 不是有效数值。");
            JToken unitToken = reading["unitName"];
            string unit = unitToken?.Type == JTokenType.String ? unitToken.Value<string>().Trim() : null;
            if (unitToken != null && unitToken.Type != JTokenType.Null && unitToken.Type != JTokenType.String)
                return Reject("单位不匹配", "云春液位单位字段不是文本。");
            if (!string.IsNullOrEmpty(unit) && !unit.Equals("null", StringComparison.OrdinalIgnoreCase) &&
                !unit.Equals("mm", StringComparison.OrdinalIgnoreCase) && unit != "毫米")
                return Reject("单位不匹配", "云春液位目前按毫米读取，请核对接口单位。");

            JToken timestamp = reading["updateTime"];
            DateTimeOffset sampleTime;
            if (timestamp?.Type != JTokenType.String || !DateTimeOffset.TryParseExact(timestamp.Value<string>(),
                SourceTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out sampleTime))
                return Reject("数据无效", "云春液位缺少带时区的 updateTime。");
            return TryApplyReading(millimeters / 1000.0, sampleTime.ToUnixTimeMilliseconds(), true);
        }
        catch (JsonException)
        {
            return Reject("数据无效", "云春接口未返回有效的 JSON 对象。");
        }
        catch (OverflowException)
        {
            return Reject("数据无效", "云春液位数据数值溢出。");
        }
    }

    /// <summary>可由网页 unityInstance.SendMessage 调用，也可由主线程上的其他数据脚本调用。</summary>
    public void ApplyCloudJson(string json)
    {
        TryApplyCloudJson(json);
    }

    public bool TryApplyCloudJson(string json)
    {
        if (!isActiveAndEnabled) return false;
        if (tankLiquid == null) return Reject("配置错误", "请先指定 Tank Liquid。");
        if (string.IsNullOrWhiteSpace(json)) return Reject("数据无效", "液位接口返回了空响应。");
        double levelMeters;
        long sampleUnixMs;
        bool valid;
        try
        {
            // 使用项目已有的 Json.NET，并检查 JSON 类型，避免把错误字符串转换为 0。
            JObject data = JObject.Parse(json, new JsonLoadSettings {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            JToken level = data["levelMeters"];
            JToken timestamp = data["sampleUnixMs"];
            JToken validity = data["valid"];
            if (level == null || (level.Type != JTokenType.Integer && level.Type != JTokenType.Float) ||
                timestamp == null || timestamp.Type != JTokenType.Integer ||
                validity == null || validity.Type != JTokenType.Boolean)
                return Reject("数据无效", "需要数值 levelMeters、整数 sampleUnixMs 与布尔 valid 字段。");
            levelMeters = level.Value<double>();
            sampleUnixMs = timestamp.Value<long>();
            valid = validity.Value<bool>();
        }
        catch (JsonException)
        {
            return Reject("数据无效", "液位接口需要返回约定的 JSON 对象。");
        }
        catch (OverflowException)
        {
            return Reject("数据无效", "液位数据数值溢出。");
        }
        return TryApplyReading(levelMeters, sampleUnixMs, valid);
    }

    private bool TryApplyReading(double levelMeters, long sampleUnixMs, bool valid)
    {
        if (double.IsNaN(levelMeters) || double.IsInfinity(levelMeters))
            return Reject("数据无效", "levelMeters 缺失或不是有限数值。");
        if (sampleUnixMs <= 0)
            return Reject("数据无效", "缺少源数据时间戳 sampleUnixMs（UTC Unix 毫秒）。");
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (sampleUnixMs > now + 60000)
            return Reject("数据无效", "源数据时间超过当前时间，请检查时钟。");
        if (now - sampleUnixMs > MaximumAgeMilliseconds)
            return Reject("数据过期", "源数据已过期，保持最后一次有效液位。");
        if (!valid) return Reject("源数据不可用", "后端未确认液位读数有效，保持原液位。");
        if (HasReceivedData && sampleUnixMs < latestSampleUnixMs)
            return Reject("数据无效", "收到较旧的源数据，保持最后一次有效液位。");
        float maximum = tankLiquid.MaximumHeightMeters;
        if (float.IsNaN(maximum) || float.IsInfinity(maximum) || maximum <= 0 ||
            levelMeters < 0 || levelMeters > maximum)
            return Reject("超出显示范围", "液位超出 0～内腔高度，请核对单位、传感器零点和 Inner Size。");

        latestLevelMeters = (float)levelMeters;
        latestSampleUnixMs = sampleUnixMs;
        HasReceivedData = true;
        connectionStatus = "数据正常";
        lastError = "";
        tankLiquid.SetLevelMeters(latestLevelMeters);
        return true;
    }

    private bool Reject(string status, string error)
    {
        bool changed = connectionStatus != status || lastError != error;
        connectionStatus = status;
        lastError = error;
        if (changed) Debug.LogWarning(error, this);
        return false;
    }

    private static float SafePositive(float value, float fallback, float minimum)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(minimum, value);
    }
}
