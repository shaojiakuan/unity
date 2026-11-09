using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class UsrCloudPressureClient1 : MonoBehaviour
{
    [Header("FastAPI接口")]
    [SerializeField]
    private string apiUrl =
        "http://127.0.0.1:8000/api/pressure";

    [Header("压力表")]
    [SerializeField]
    private PressureGaugeNeedle1 pressureGauge;

    [Header("读取间隔")]
    [SerializeField]
    private float requestInterval = 1.0f;

    [Header("调试信息")]
    [SerializeField]
    private float latestPressure;

    [System.Serializable]
    private class PressureResponse
    {
        public float pressure;
    }

    private void Start()
    {
        if (pressureGauge == null)
        {
            Debug.LogError(
                "没有设置 PressureGaugeNeedle1！"
            );

            enabled = false;
            return;
        }

        StartCoroutine(
            RequestPressureLoop()
        );
    }

    private IEnumerator RequestPressureLoop()
    {
        while (true)
        {
            yield return GetPressure();

            yield return new WaitForSeconds(
                requestInterval
            );
        }
    }

    private IEnumerator GetPressure()
    {
        using (
            UnityWebRequest request =
                UnityWebRequest.Get(apiUrl)
        )
        {
            request.timeout = 5;

            yield return request.SendWebRequest();

            if (
                request.result ==
                UnityWebRequest.Result.Success
            )
            {
                string json =
                    request.downloadHandler.text;

                PressureResponse result =
                    JsonUtility.FromJson<PressureResponse>(
                        json
                    );

                if (result != null)
                {
                    latestPressure =
                        result.pressure;

                    pressureGauge.SetPressure(
                        latestPressure
                    );

                    Debug.Log(
                        "当前有人云压力：" +
                        latestPressure
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "压力数据获取失败：" +
                    request.error
                );
            }
        }
    }
}