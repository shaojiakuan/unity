using System.Collections;
using UnityEngine;

public class MoveUpAndDown2 : MonoBehaviour
{
    public float riseDistance = 2.3f;          // 上升的距离
    public float rotationAngle = 120f;         // 沿X轴旋转的角度
    public float rotationHoldTime = 3.2f;        // 保持旋转状态的持续时间
    public float moveSpeed = 1f;               // 上升和下降的速度
    public float rotationSpeed = 100f;         // 旋转速度

    public GameObject secondaryObject;         // 第二物体
    private Vector3 startPosition;             // 初始位置
    private Quaternion startRotation;          // 初始旋转角度

    void Start()
    {
        startPosition = transform.position;    // 记录初始位置
        startRotation = transform.rotation;    // 记录初始旋转角度
        SetObjectAndChildrenActive(secondaryObject, false); // 初始状态下隐藏第二物体及其附属物体

        StartCoroutine(LoopMovementRoutine()); // 启动协程
    }

    IEnumerator LoopMovementRoutine()
    {
        while (true)
        {
            // 1. 匀速上升 2.3 单位
            Vector3 targetPosition = startPosition + Vector3.up * riseDistance;
            yield return MoveToPosition(targetPosition);

            // 2. 显示第二物体及其附属物体
            SetObjectAndChildrenActive(secondaryObject, true);

            // 3. 沿X轴旋转 120 度
            Quaternion targetRotation = startRotation * Quaternion.Euler(-rotationAngle, 0, 0);
            yield return RotateToRotation(targetRotation);

            // 4. 保持旋转状态 2 秒
            yield return new WaitForSeconds(rotationHoldTime);

            // 5. 旋转回初始角度
            yield return RotateToRotation(startRotation);

            // 6. 隐藏第二物体及其附属物体
            SetObjectAndChildrenActive(secondaryObject, false);

            // 7. 返回到初始位置
            yield return MoveToPosition(startPosition);
        }
    }

    // 匀速移动到目标位置
    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // 旋转到目标角度
    IEnumerator RotateToRotation(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // 递归设置对象及其所有子对象的激活状态
    void SetObjectAndChildrenActive(GameObject obj, bool isActive)
    {
        obj.SetActive(isActive);
        foreach (Transform child in obj.transform)
        {
            child.gameObject.SetActive(isActive);
        }
    }
}