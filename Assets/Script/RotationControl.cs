using UnityEngine;

public class RotationControl : MonoBehaviour
{
    public GameObject targetObject; // 另一个物体的引用

    void Update()
    {
        // 检查当前物体的 Rotation X 值
        if (transform.rotation.eulerAngles.x == 50f)
        {
            // 如果 Rotation X 值接近 0，则显示目标物体
            if (targetObject != null)
                targetObject.SetActive(true);
        }
        else
        {
            // 如果 Rotation X 值不为 0，则隐藏目标物体
            if (targetObject != null)
                targetObject.SetActive(false);
        }
    }
}
