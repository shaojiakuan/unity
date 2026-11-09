using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

public class Simulatet : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Hello();

    private string str_test;
    private string pattern;

    private bool isShowTip;
    private string data;
    private string digitsData;
    private string nameData;
    private string noNameData;
    private string dataUnit;
    private string noDataUnit;

    void Start()
    {
        isShowTip = false;
        str_test = "[{ \"id\": 39,\"name\": \"variable204\",\"updateTime\": \"2024-07-22T06:46:16.341+00:00\",\"value\": \"88.17\",\"unitName\": \"摄氏度\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\":\" null\",\"notes\": \"火焰温度\"},{ \"id\": 23,\"name\": \"variable274\",\"updateTime\": \"2024-07-22T06:46:16.231+00:00\",\"value\": \"0.253187\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"二次鼓风频率反馈\"},{ \"id\": 17,\"name\": \"variable272\",\"updateTime\": \"2024-07-22T06:46:16.205+00:00\",\"value\": \"0.458902\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"炉排频率反馈\"},{ \"id\": 11,\"name\": \"variable205\",\"updateTime\": \"2024-07-22T06:46:16.183+00:00\",\"value\": \"27.5579\",\"unitName\": \"摄氏度\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"布袋出口温度\"},{ \"id\": 34,\"name\": \"variable283\",\"updateTime\": \"2024-07-22T06:46:16.311+00:00\",\"value\": \"0\",\"unitName\": \"帕\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"蒸汽压力\"},{ \"id\": 20,\"name\": \"variable273\",\"updateTime\": \"2024-07-22T06:46:16.213+00:00\",\"value\": \"0.0881635\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"控料器频率反馈\"},{ \"id\": 37,\"name\": \"variable202\",\"updateTime\": \"2024-07-22T06:46:16.332+00:00\",\"value\": \"32.5255\",\"unitName\": \"摄氏度\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"省煤出口水温\"},{ \"id\": 32,\"name\": \"variable287\",\"updateTime\": \"2024-07-22T06:46:16.293+00:00\",\"value\": \"500\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"磁翻板水位\"},{ \"id\": 33,\"name\": \"variable282\",\"updateTime\": \"2024-07-22T06:46:16.298+00:00\",\"value\": \"33.2082\",\"unitName\": \"摄氏度\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"省煤器入口烟温\"},{ \"id\": 35,\"name\": \"variable200\",\"updateTime\": \"2024-07-22T06:46:16.321+00:00\",\"value\": \"640.23\",\"unitName\": \"帕\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"烟尘入口压力\"},{ \"id\": 38,\"name\": \"variable203\",\"updateTime\": \"2024-07-22T06:46:16.337+00:00\",\"value\": \"19.4488\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"烟气氧含量\"},{ \"id\": 16,\"name\": \"variable206\",\"updateTime\": \"2024-07-22T06:46:16.186+00:00\",\"value\": \"465.554\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"蒸汽累计流量\"},{ \"id\": 36,\"name\": \"variable201\",\"updateTime\": \"2024-07-22T06:46:16.328+00:00\",\"value\": \"32.5075\",\"unitName\": \"摄氏度\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"烟尘入口温度\"},{ \"id\": 24,\"name\": \"variable275\",\"updateTime\": \"2024-07-22T06:46:16.244+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"一次鼓风频率反馈\"},{ \"id\": 25,\"name\": \"variable276\",\"updateTime\": \"2024-07-22T06:46:16.250+00:00\",\"value\": \"0.0745999\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"引风机频率反馈\"},{ \"id\": 26,\"name\": \"variable277\",\"updateTime\": \"2024-07-22T06:46:16.255+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"1立泵频率反馈\"},{ \"id\": 28,\"name\": \"variable278\",\"updateTime\": \"2024-07-22T06:46:16.260+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"2立泵频率反馈\"},{ \"id\": 29,\"name\": \"variable279\",\"updateTime\": \"2024-07-22T06:46:16.264+00:00\",\"value\": \"0.113698\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"3立泵频率反馈\"},{ \"id\": 30,\"name\": \"variable280\",\"updateTime\": \"2024-07-22T06:46:16.285+00:00\",\"value\": \"0.0972059\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"1增压泵频率反馈\"},{ \"id\": 31,\"name\": \"variable281\",\"updateTime\": \"2024-07-22T06:46:16.289+00:00\",\"value\": \"45.0877\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"2增压泵频率反馈\"}]";
        pattern = @"\{([^\{\}]+)\}";
    }

    void OnMouseEnter()
    {
        isShowTip = true;
    }

    void OnMouseExit()
    {
        isShowTip = false;
    }

    void OnGUI()
    {
        if (isShowTip)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 100;
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(Input.mousePosition.x, Screen.height - Input.mousePosition.y, 100, 100), data, style);
        }

    }

    void Update()
    {
        MatchCollection matches = Regex.Matches(str_test, pattern);
        foreach (Match match in matches)
        {
            if (match.Groups[1].Value.Contains("39"))
            {
                Dictionary<string, string> keyValuePairs = ParseKeyValuePairs(match.Groups[1].Value);
                foreach (var pair in keyValuePairs)
                {
                    if (pair.Key == "\"value\"")
                    {
                        digitsData = Regex.Replace(pair.Value, @"[^\d\.]", "");
                    }
                    if (pair.Key == "\"notes\"")
                    {
                        nameData = pair.Value;
                        noNameData = nameData.Replace("\"", "");
                    }
                    if (pair.Key == "\"unitName\"")
                    {
                        dataUnit = pair.Value;
                        noDataUnit = dataUnit.Replace("\"", "");
                    }
                    data = noNameData + "的数值是：" + digitsData;
                }
            }
        }
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
        Hello();
    }
}