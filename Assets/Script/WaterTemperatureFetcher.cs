using UnityEngine;
using UnityEngine.Networking;
using System.Xml.Linq;
using System.Linq;
using System.Collections;

public class WaterTemperatureFetcher : MonoBehaviour
{
    private string url = "https://yunchunremote.cn:5656/deviceValue/selectValue/88";

    void Start()
    {
        StartCoroutine(FetchWaterTemperature());
    }

    IEnumerator FetchWaterTemperature()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("请求失败: " + request.error);
            yield break;
        }

        string xmlContent = request.downloadHandler.text;

        string value = ParseValueByNotes(xmlContent, "省煤出口水温");
        if (value != null)
        {
            Debug.Log($"出口水温: {value}");
        }
        else
        {
            Debug.Log("找不到对应的notes项。");
        }
    }

    string ParseValueByNotes(string xmlContent, string targetNotes)
    {
        XDocument doc = XDocument.Parse(xmlContent);

        var item = doc.Descendants("item")
                      .FirstOrDefault(x => (string)x.Element("notes") == targetNotes);

        if (item != null)
        {
            var valueElement = item.Element("value");
            if (valueElement != null)
                return valueElement.Value;
        }

        return null;
    }
}
