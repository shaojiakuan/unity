using System.Collections;
using UnityEngine;

public class Rotate2 : MonoBehaviour
{
    public float riseDistance = 2.3f;          // 上升的距离
    public float rotationAngle = 120f;         // 沿X轴旋转的角度
    public float rotationHoldTime = 3.2f;        // 保持旋转状态的持续时间
    public float moveSpeed = 1f;               // 上升和下降的速度
    public float rotationSpeed = 100f;         // 旋转速度
    private Vector3 startPosition;             // 初始位置
    private Quaternion startRotation;          // 初始旋转角度

    void Start()
    {
        startPosition = transform.position;    // 记录初始位置
        startRotation = transform.rotation;    // 记录初始旋转角度

        StartCoroutine(LoopMovementRoutine()); // 启动协程
    }

    IEnumerator LoopMovementRoutine()
    {
        while (true)
        {
            // 4. 保持旋转状态 2 秒
            yield return new WaitForSeconds(1.7f);

            // 3. 沿X轴旋转 120 度
            yield return RotateToRotation(1);

            // 4. 保持旋转状态 2 秒
            yield return new WaitForSeconds(2f);

            // 5. 旋转回初始角度
            yield return RotateToRotation(-1);

            // 4. 保持旋转状态 2 秒
            yield return new WaitForSeconds(1.7f);
        }
    }

    // 旋转到目标角度
    IEnumerator RotateToRotation(int direction)
    {
        for (int i = 0; i < 60; i++)
        {
            transform.Rotate(new Vector3(0, 0, 1), 2f * direction, Space.World);
            yield return new WaitForSeconds(0.01f);
        }
    }
}
