using UnityEngine;

public class openthedoor : MonoBehaviour
{
    // 要控制的物体
    public GameObject targetObject;

    // 是否在开始时隐藏物体
    public bool startHidden = false;

    void Start()
    {
        // 如果指定了目标物体，根据startHidden设置初始状态
        if (targetObject != null)
        {
            targetObject.SetActive(!startHidden);
        }
    }

    void Update()
    {
        // 检测是否按下O键
        if (Input.GetKeyDown(KeyCode.O))
        {
            ToggleObjectVisibility();
        }
    }

    void ToggleObjectVisibility()
    {
        // 确保目标物体不为空
        if (targetObject != null)
        {
            // 切换物体的激活状态
            targetObject.SetActive(!targetObject.activeSelf);

            // 可选：在控制台输出当前状态
            Debug.Log(targetObject.name + " 状态: " + (targetObject.activeSelf ? "显示" : "隐藏"));
        }
        else
        {
            Debug.LogWarning("目标物体未分配！");
        }
    }
}