using UnityEngine;
using System.Collections;

public class fall : MonoBehaviour
{
    public GameObject targetObjectAll; // 另一个物体的引用
    public GameObject[] targetObjects; // 支持多个物体
    public float moveSpeed = 2f;       // 垂直移动的速度

    private Vector3 initialPosition;        // 该脚本物体的初始位置
    private Vector3 targetObjectAllInitialPosition; // targetObjectAll 的初始位置
    private bool isMovingDown = false;      // 是否开始向下移动

    void Start()
    {
        // 记录初始位置信息
        initialPosition = transform.position;
        targetObjectAllInitialPosition = targetObjectAll.transform.position;
    }

    void Update()
    {
        // 检测当前物体的 rotation.x 是否大于 50
        if (transform.rotation.eulerAngles.x > 50f)
        {
            if (!isMovingDown)
            {
                isMovingDown = true;
                StartCoroutine(MoveDownAndHide());
            }
        }

        // 检测物体是否回到初始位置
        if (Vector3.Distance(transform.position, initialPosition) < 0.01f)
        {
            ResetTargetObjectAll();
        }
    }

    // 向下移动 0.5 秒并隐藏的协程
    private IEnumerator MoveDownAndHide()
    {
        float startTime = Time.time;

        foreach (GameObject targetObject in targetObjects)
        {
            Vector3 initialPosition = targetObject.transform.position;

            // 向下移动 0.5 秒
            float elapsedTime = 0f;
            while (elapsedTime < 0.5f)
            {
                targetObject.transform.position = initialPosition + Vector3.down * moveSpeed * elapsedTime;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 确保物体在 0.5 秒后准确地到达最终位置
            targetObject.transform.position = initialPosition + Vector3.down * moveSpeed * 0.5f;

            // 隐藏物体
            targetObject.SetActive(false);
        }

        isMovingDown = false;
    }

    // 重置 targetObjectAll 的状态
    private void ResetTargetObjectAll()
    {
        targetObjectAll.transform.position = targetObjectAllInitialPosition; // 恢复初始位置
        targetObjectAll.SetActive(true); // 设置为显示状态
    }
}
