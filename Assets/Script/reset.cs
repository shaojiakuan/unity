using UnityEngine;
using System.Collections;

public class reset : MonoBehaviour
{
    private Vector3 initialPosition; // 初始位置
    public float resetInterval = 14f; // 恢复间隔

    void Start()
    {
        // 记录物体的初始位置
        initialPosition = transform.position;

        // 启动协程
        StartCoroutine(ResetCoroutine());
    }

    private IEnumerator ResetCoroutine()
    {
        while (true) // 无限循环
        {
            yield return new WaitForSeconds(resetInterval); // 等待指定的时间

            // 恢复物体的位置
            transform.position = initialPosition;

            // 如果需要，您还可以重置物体的速度等
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero; // 重置速度
                rb.angularVelocity = Vector3.zero; // 重置角速度
            }
        }
    }
}