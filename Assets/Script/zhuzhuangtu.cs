using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

public class zhuzhuangtu : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void Hello();

    public Image image;
    public Image image1;
    public Image image2;
    public Image image3;
    public Image image4;
    public Image image5;
    private string str_test;
    private string pattern;

    void Start()
    {
        str_test = "[{ \"id\": 39,\"name\": \"variable204\",\"updateTime\": \"2024-07-22T06:46:16.341+00:00\",\"value\": \"0.00179944\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\":\" null\",\"notes\": \"��������\"},{ \"id\": 23,\"name\": \"variable274\",\"updateTime\": \"2024-07-22T06:46:16.231+00:00\",\"value\": \"0.253187\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"���ιķ�Ƶ�ʷ���\"},{ \"id\": 17,\"name\": \"variable272\",\"updateTime\": \"2024-07-22T06:46:16.205+00:00\",\"value\": \"0.458902\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"¯��Ƶ�ʷ���\"},{ \"id\": 11,\"name\": \"variable205\",\"updateTime\": \"2024-07-22T06:46:16.183+00:00\",\"value\": \"27.5579\",\"unitName\": \"���϶�\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"���������¶�\"},{ \"id\": 34,\"name\": \"variable283\",\"updateTime\": \"2024-07-22T06:46:16.311+00:00\",\"value\": \"0\",\"unitName\": \"��\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"����ѹ��\"},{ \"id\": 20,\"name\": \"variable273\",\"updateTime\": \"2024-07-22T06:46:16.213+00:00\",\"value\": \"0.0881635\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"������Ƶ�ʷ���\"},{ \"id\": 37,\"name\": \"variable202\",\"updateTime\": \"2024-07-22T06:46:16.332+00:00\",\"value\": \"32.5255\",\"unitName\": \"���϶�\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"ʡú����ˮ��\"},{ \"id\": 32,\"name\": \"variable287\",\"updateTime\": \"2024-07-22T06:46:16.293+00:00\",\"value\": \"500\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"�ŷ���ˮλ\"},{ \"id\": 33,\"name\": \"variable282\",\"updateTime\": \"2024-07-22T06:46:16.298+00:00\",\"value\": \"33.2082\",\"unitName\": \"���϶�\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"ʡú���������\"},{ \"id\": 35,\"name\": \"variable200\",\"updateTime\": \"2024-07-22T06:46:16.321+00:00\",\"value\": \"0\",\"unitName\": \"��\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"�̳����ѹ��\"},{ \"id\": 38,\"name\": \"variable203\",\"updateTime\": \"2024-07-22T06:46:16.337+00:00\",\"value\": \"19.4488\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"����������\"},{ \"id\": 16,\"name\": \"variable206\",\"updateTime\": \"2024-07-22T06:46:16.186+00:00\",\"value\": \"465.554\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"�����ۼ�����\"},{ \"id\": 36,\"name\": \"variable201\",\"updateTime\": \"2024-07-22T06:46:16.328+00:00\",\"value\": \"32.5075\",\"unitName\": \"���϶�\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"�̳�����¶�\"},{ \"id\": 24,\"name\": \"variable275\",\"updateTime\": \"2024-07-22T06:46:16.244+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"һ�ιķ�Ƶ�ʷ���\"},{ \"id\": 25,\"name\": \"variable276\",\"updateTime\": \"2024-07-22T06:46:16.250+00:00\",\"value\": \"0.0745999\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"�����Ƶ�ʷ���\"},{ \"id\": 26,\"name\": \"variable277\",\"updateTime\": \"2024-07-22T06:46:16.255+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"1����Ƶ�ʷ���\"},{ \"id\": 28,\"name\": \"variable278\",\"updateTime\": \"2024-07-22T06:46:16.260+00:00\",\"value\": \"0\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"2����Ƶ�ʷ���\"},{ \"id\": 29,\"name\": \"variable279\",\"updateTime\": \"2024-07-22T06:46:16.264+00:00\",\"value\": \"0.113698\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"3����Ƶ�ʷ���\"},{ \"id\": 30,\"name\": \"variable280\",\"updateTime\": \"2024-07-22T06:46:16.285+00:00\",\"value\": \"0.0972059\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"1��ѹ��Ƶ�ʷ���\"},{ \"id\": 31,\"name\": \"variable281\",\"updateTime\": \"2024-07-22T06:46:16.289+00:00\",\"value\": \"45.0877\",\"unitName\": \"null\",\"deviceId\": \"88\",\"highLimit\": \"null\",\"lowLimit\": \"null\",\"alarmTag\": \"null\",\"notes\": \"2��ѹ��Ƶ�ʷ���\"}]";
        pattern = @"\{([^\{\}]+)\}";
    }

    void Update()
    {
        MatchCollection matches = Regex.Matches(str_test, pattern);
        Dictionary<string, string> keyValuePairs = ParseKeyValuePairs(matches[0].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                image.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
            }
        }
        keyValuePairs = ParseKeyValuePairs(matches[1].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                image1.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
            }
        }
        keyValuePairs = ParseKeyValuePairs(matches[2].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                image2.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
            }
        }
        keyValuePairs = ParseKeyValuePairs(matches[3].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                image3.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
            }
        }
        keyValuePairs = ParseKeyValuePairs(matches[4].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                image4.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
            }
        }
        keyValuePairs = ParseKeyValuePairs(matches[5].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
            if (pair.Key == "\"value\"")
            {
                string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                image5.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
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
        pattern = @"\{([^\{\}]+)\}";
        //Debug.Log(str);
        MatchCollection matches = Regex.Matches(str, pattern);
        //Debug.Log(matches);
        Dictionary<string, string> keyValuePairs = ParseKeyValuePairs(matches[0].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
           if (pair.Key == "\"value\"")
             {
                        string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                        //Debug.Log(float.Parse(digitsOnly));
                        image.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
                    }
        }
        keyValuePairs = ParseKeyValuePairs(matches[1].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
           if (pair.Key == "\"value\"")
             {
                        string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                        //Debug.Log(float.Parse(digitsOnly));
                        image1.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
                    }
        }
        keyValuePairs = ParseKeyValuePairs(matches[2].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
           if (pair.Key == "\"value\"")
             {
                        string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                        //Debug.Log(float.Parse(digitsOnly));
                        image2.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
                    }
        }
        keyValuePairs = ParseKeyValuePairs(matches[3].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
           if (pair.Key == "\"value\"")
             {
                        string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                        //Debug.Log(float.Parse(digitsOnly));
                        image3.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
                    }
        }
        keyValuePairs = ParseKeyValuePairs(matches[4].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
           if (pair.Key == "\"value\"")
             {
                        string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                        //Debug.Log(float.Parse(digitsOnly));
                        image4.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
                    }
        }
        keyValuePairs = ParseKeyValuePairs(matches[5].Groups[1].Value);
        foreach (var pair in keyValuePairs)
        {
           if (pair.Key == "\"value\"")
             {
                        string digitsOnly = Regex.Replace(pair.Value, @"[^\d\.]", "");
                        //Debug.Log(float.Parse(digitsOnly));
                        image5.fillAmount = (float.Parse(digitsOnly)) / (float)(50.0);
                    }
        }

    }
}
