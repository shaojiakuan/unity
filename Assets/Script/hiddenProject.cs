using UnityEngine;

/// <summary>
/// 按下快捷键，批量切换多个物体的显示/隐藏
/// 适合统一控制一组锅炉零件、阀门、附件等
/// </summary>
public class hiddenProject : MonoBehaviour
{
    [Header("目标物体列表")]
    [Tooltip("把所有要控制的物体都拖拽进来")]
    public GameObject[] targetObjects;

    [Header("触发快捷键")]
    public KeyCode hotKey = KeyCode.X;

    void Update()
    {
        // 按下快捷键时，批量切换所有物体的显隐
        if (Input.GetKeyDown(hotKey))
        {
            foreach (GameObject obj in targetObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }
    }
}
