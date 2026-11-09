using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;

public class abcde : MonoBehaviour
{
    public float num;

    [Header("网络设置")]
    public int deviceId = 88;  // 设备ID
    public int targetId = 17;  // 目标数据ID
    public float updateInterval = 2f; // 更新间隔（秒）

    private float updateTimer = 0f;
    private DeviceValue1 lastDeviceValue = null;

    async void Start()
    {
        Debug.Log("开始监控设备数据...");
        // 初始更新一次
        await UpdateDeviceDataAsync();
    }

    async void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            await UpdateDeviceDataAsync();
        }
    }

    private async Task UpdateDeviceDataAsync()
    {
        try
        {
            // 获取设备数据
            DeviceValue1[] values = await TestHttp1.GetDeviceValues1Async(deviceId);

            if (values == null || values.Length == 0)
            {
                Debug.LogWarning($"没有找到设备ID {deviceId} 的数据");
                return;
            }

            // 查找目标数据
            DeviceValue1 targetValue = null;
            foreach (var value in values)
            {
                if (value.Id1 == targetId)
                {
                    targetValue = value;
                    break;
                }
            }

            if (targetValue == null)
            {
                Debug.LogWarning($"在设备 {deviceId} 中没有找到ID为 {targetId} 的数据");
                return;
            }

            // 输出到控制台
            OutputToConsole(targetValue);

            // 更新最后的值
            lastDeviceValue = targetValue;

            // 检查数据是否有变化
            CheckForChanges(targetValue);

        }
        catch (Exception ex)
        {
            Debug.LogError($"更新设备数据时出错: {ex.Message}");
        }
    }

    private void OutputToConsole(DeviceValue1 deviceValue)
    {
        // 格式化输出信息
        string output = $"\n=== 设备数据更新 ===\n" +
                       $"设备ID: {deviceValue.DeviceId1}\n" +
                       $"数据ID: {deviceValue.Id1}\n" +
                       $"名称: {deviceValue.Name1}\n" +
                       $"值: {deviceValue.Value1}\n" +
                       $"单位: {deviceValue.UnitName1}\n" +
                       $"备注: {deviceValue.Notes1}\n" +
                       $"高限: {deviceValue.HighLimit1}\n" +
                       $"低限: {deviceValue.LowLimit1}\n" +
                       $"报警标签: {deviceValue.AlarmTag1}\n" +
                       $"=====================\n";

        Debug.Log(output);

        // 也可以使用简单的单行输出
        Debug.Log($"[{deviceValue.UpdateTime1:HH:mm:ss}] ID:{deviceValue.Id1} {deviceValue.Name1} = {deviceValue.Value1}{deviceValue.UnitName1} ({deviceValue.Notes1})");
    }

    private void CheckForChanges(DeviceValue1 currentValue)
    {
        if (lastDeviceValue == null) return;

        // 检查值是否有变化
        if (currentValue.Value1 != lastDeviceValue.Value1)
        {
            Debug.Log($"数值变化: {lastDeviceValue.Value1} → {currentValue.Value1} {currentValue.UnitName1}");
        }

        // 检查报警状态变化
        if (currentValue.AlarmTag1 != lastDeviceValue.AlarmTag1)
        {
            if (!string.IsNullOrEmpty(currentValue.AlarmTag1))
            {
                Debug.LogError($"报警状态变化: {currentValue.AlarmTag1}");
            }
            else
            {
                Debug.Log("报警已清除");
            }
        }
    }
}

public record DeviceValue1
{
    [property: JsonProperty("id")]
    public int? Id1 { get; set; }

    [property: JsonProperty("name")]
    public string? Name1 { get; set; }

    [property: JsonProperty("updateTime")]
    public DateTime UpdateTime1 { get; set; }

    [property: JsonProperty("value")]
    public decimal Value1 { get; set; }

    [property: JsonProperty("unitName")]
    public string? UnitName1 { get; set; }

    [property: JsonProperty("deviceId")]
    public int? DeviceId1 { get; set; }

    [property: JsonProperty("highLimit")]
    public decimal? HighLimit1 { get; set; }

    [property: JsonProperty("lowLimit")]
    public decimal? LowLimit1 { get; set; }

    [property: JsonProperty("alarmTag")]
    public string? AlarmTag1 { get; set; }

    [property: JsonProperty("notes")]
    public string? Notes1 { get; set; }
}

public static class TestHttp1
{
    public static async Task<DeviceValue1[]> GetDeviceValues1Async(int deviceId)
    {
        try
        {
            string url = $"https://yunchunremote.cn:5656/deviceValue/selectValue/{deviceId}";
            Debug.Log($"正在请求: {url}");

            using var request = UnityWebRequest.Get(url);

            // 设置超时时间（可选）
            request.timeout = 10;

            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield(); // 让出控制权，避免阻塞
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"网络请求失败: {request.error}");
                throw new Exception($"网络请求失败: {request.error}");
            }

            var json = request.downloadHandler.text;
            //Debug.Log($"收到响应: {json}");

            var serializer = JsonSerializer.Create();
            using var stringReader = new StringReader(json);
            using var jsonReader = new JsonTextReader(stringReader);
            return serializer.Deserialize<DeviceValue1[]>(jsonReader);
        }
        catch (Exception ex)
        {
            Debug.LogError($"获取设备数据时异常: {ex.Message}");
            return Array.Empty<DeviceValue1>();
        }
    }
}