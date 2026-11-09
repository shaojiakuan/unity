using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

public class dataAssociation : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Hello();

    public ParticleSystem ParticleSystemNOx;
    public ParticleSystem ParticleSystemNOx1;
    public ParticleSystem ParticleSystemNOx2;
    public ParticleSystem ParticleSystemNOx3;
    public ParticleSystem ParticleSystemNOx4;
    public ParticleSystem ParticleSystemNOx5;

    public ParticleSystem ParticleSystemWater;

    private string str_test;
    private string pattern;

    public Conveyer conveyer;
    public MoveUpAndDown moveupanddown;

    public ParticleSystem ParticleSystemSomke1;

    void Start()
    {
        str_test = "[{ \"id\": 39,\"name\": \"variable204\",\"updateTime\": \"2024-07-22T06:46:16.341+00:00\",\"value\": \"3\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\":\" null\",\"notes\": \"烟尘入口压力\"}," +
            "{ \"id\": 23,\"name\": \"variable274\",\"updateTime\": \"2024-07-22T06:46:16.231+00:00\",\"value\": \"2\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"烟尘入口温度\"}," +
            "{ \"id\": 17,\"name\": \"variable272\",\"updateTime\": \"2024-07-22T06:46:16.205+00:00\",\"value\": \"0.458902\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"省煤出口水温\"}," +
            "{ \"id\": 11,\"name\": \"variable205\",\"updateTime\": \"2024-07-22T06:46:16.183+00:00\",\"value\": \"2\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"烟气氧含量\"}," +
            "{ \"id\": 34,\"name\": \"variable283\",\"updateTime\": \"2024-07-22T06:46:16.311+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"蒸汽流量\"}," +
            "{ \"id\": 20,\"name\": \"variable273\",\"updateTime\": \"2024-07-22T06:46:16.213+00:00\",\"value\": \"0.0881635\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"布袋出口温度\"}," +
            "{ \"id\": 37,\"name\": \"variable202\",\"updateTime\": \"2024-07-22T06:46:16.332+00:00\",\"value\": \"32.5255\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"蒸汽累计流量\"}," +
            "{ \"id\": 32,\"name\": \"variable287\",\"updateTime\": \"2024-07-22T06:46:16.293+00:00\",\"value\": \"0.01\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"炉排频率反馈\"}," +
            "{ \"id\": 33,\"name\": \"variable282\",\"updateTime\": \"2024-07-22T06:46:16.298+00:00\",\"value\": \"1\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"控料器频率反馈\"}," +
            "{ \"id\": 35,\"name\": \"variable200\",\"updateTime\": \"2024-07-22T06:46:16.321+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"二次鼓风频率反馈\"}," +
            "{ \"id\": 38,\"name\": \"variable203\",\"updateTime\": \"2024-07-22T06:46:16.337+00:00\",\"value\": \"19.4488\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"省煤器入口烟温\"}," +
            "{ \"id\": 16,\"name\": \"variable206\",\"updateTime\": \"2024-07-22T06:46:16.186+00:00\",\"value\": \"465.554\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"蒸汽压力\"}," +
            "{ \"id\": 36,\"name\": \"variable201\",\"updateTime\": \"2024-07-22T06:46:16.328+00:00\",\"value\": \"32.5075\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"磁翻板水位\"}," +
            "{ \"id\": 24,\"name\": \"variable275\",\"updateTime\": \"2024-07-22T06:46:16.244+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"1立泵频率反馈\"}," +
            "{ \"id\": 25,\"name\": \"variable276\",\"updateTime\": \"2024-07-22T06:46:16.250+00:00\",\"value\": \"0.0745999\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"2立泵频率反馈\"}," +
            "{ \"id\": 26,\"name\": \"variable277\",\"updateTime\": \"2024-07-22T06:46:16.255+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"3立泵频率反馈\"}," +
            "{ \"id\": 28,\"name\": \"variable278\",\"updateTime\": \"2024-07-22T06:46:16.260+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"1增压泵频率反馈\"}," +
            "{ \"id\": 29,\"name\": \"variable279\",\"updateTime\": \"2024-07-22T06:46:16.264+00:00\",\"value\": \"0.113698\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"2增压泵频率反馈\"}," +
            "{ \"id\": 30,\"name\": \"variable280\",\"updateTime\": \"2024-07-22T06:46:16.285+00:00\",\"value\": \"0.0972059\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"一次鼓风频率反馈\"}," +
            "{ \"id\": 31,\"name\": \"variable281\",\"updateTime\": \"2024-07-22T06:46:16.289+00:00\",\"value\": \"45.0877\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"引风机频率反馈\"}]";
        pattern = @"\{([^\{\}]+)\}";
        GetFromWeb(str_test);
    }


    private static Dictionary<string, string> ParseKeyValuePairs(string input)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        string[] pairs = input.Split(',');
        foreach (var pair in pairs)
        {
            string[] parts = pair.Split(':');
            if (parts.Length == 2)
            {
                result[parts[0]] = parts[1];
            }
        }
        return result;
    }

    public void GetFromWeb(string str)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Hello();
#endif
        pattern = @"\{([^\{\}]+)\}";

        var emissionNOx = ParticleSystemNOx.main;
        var emissionNOx1 = ParticleSystemNOx1.main;
        var emissionNOx2 = ParticleSystemNOx2.main;
        var emissionNOx3 = ParticleSystemNOx3.main;
        var emissionNOx4 = ParticleSystemNOx4.main;
        var emissionNOx5 = ParticleSystemNOx5.main;

        var speedWater = ParticleSystemWater.main;

        MatchCollection matches = Regex.Matches(str, pattern);
        Dictionary<string, string> keyValuePairs = ParseKeyValuePairs(matches[3].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                emissionNOx.startSpeed = float.Parse(digitsOnly);
                emissionNOx1.startSpeed = float.Parse(digitsOnly);
                emissionNOx2.startSpeed = float.Parse(digitsOnly) + float.Parse("0.1");
                emissionNOx3.startSpeed = float.Parse(digitsOnly) + float.Parse("0.3");
                emissionNOx4.startSpeed = float.Parse(digitsOnly) + float.Parse("0.5");
                emissionNOx5.startSpeed = float.Parse(digitsOnly) + float.Parse("0.35");
            }
        }
        Dictionary<string, string> keyValuePairs1 = ParseKeyValuePairs(matches[1].Groups[1].Value);
        foreach (var pair1 in keyValuePairs1)
        {
            if (pair1.Key == "\"value\"")
            {
                string digitsOnly1 = Regex.Replace(pair1.Value, @"[^\d\.]", "");
                speedWater.startSpeed = float.Parse(digitsOnly1);
            }
        }
        Dictionary<string, string> keyValuePairs2 = ParseKeyValuePairs(matches[7].Groups[1].Value);
        foreach (var pair2 in keyValuePairs2)
        {
            if (pair2.Key == "\"value\"")
            {
                string digitsOnly2 = Regex.Replace(pair2.Value, @"[^\d\.]", "");
                conveyer.Speed = float.Parse(digitsOnly2);
            }
        }
        Dictionary<string, string> keyValuePairs3 = ParseKeyValuePairs(matches[8].Groups[1].Value);
        foreach (var pair3 in keyValuePairs3)
        {
            if (pair3.Key == "\"value\"")
            {
                string digitsOnly3 = Regex.Replace(pair3.Value, @"[^\d\.]", "");
                moveupanddown.moveSpeed = float.Parse(digitsOnly3);
            }
        }
    }
}

