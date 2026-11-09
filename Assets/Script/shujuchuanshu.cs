using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Threading;
using System;
using Newtonsoft.Json.Serialization;
using System.IO;

public class shujuchuanshu : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            TestTest.Show();
        }
    }
}

public record DeviceValue
{
    [property: JsonProperty("id")]
    public int? id { get; set; }

    [property: JsonProperty("name")]
    public string? name { get; set; }

    [property: JsonProperty("updateTime")]
    public System.DateTime updateTime { get; set; }

    [property: JsonProperty("value")]
    public decimal Value { get; set; }

    [property: JsonProperty("unitName")]
    public string? unitName { get; set; }

    [property: JsonProperty("deviceId")]
    public int? deviceId { get; set; }

    [property: JsonProperty("highLimit")]
    public decimal? highLimit { get; set; }

    [property: JsonProperty("lowLimit")]
    public decimal? lowLimit { get; set; }

    [property: JsonProperty("alarmTag")]
    public string? alarmTag { get; set; }

    [property: JsonProperty("notes")]
    public string? notes { get; set; }
}

public static class TestHttp
{

    public static async Task<DeviceValue[]> GetDeviceValuesAsync(int deviceId)
    {
        try
        {
            using var request = UnityWebRequest.Get($"https://yunchuncloud.com:5656/deviceValue/selectValue/{deviceId}");
            var connection = request.SendWebRequest();
            while (!connection.isDone)
            {
                await Task.Yield();
            }

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.DataProcessingError)
            {
                throw new Exception(request.error);
            }

            using var stream = new MemoryStream(request.downloadHandler.data);
            using var reader = new StreamReader(stream);
            using var json = new JsonTextReader(reader);
            var serializer = JsonSerializer.Create();
            return serializer.Deserialize<DeviceValue[]>(json);
        }
        catch (Exception)
        {
            return Array.Empty<DeviceValue>();
        }
    }
}

public static class TestTest
{
    public static async void Show()
    {
        DeviceValue[] values = await TestHttp.GetDeviceValuesAsync(88); ;
        foreach (var value in values)
        {
            if (value.id == 31) {

            }
            
        }
    }
}

