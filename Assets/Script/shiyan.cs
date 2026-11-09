using UnityEngine;

public class shiyan : MonoBehaviour
{
    [Header("移动速度（单位/秒）")]
    public float moveSpeed = 0.5f;

    [Header("每个阶段旋转总角度（绕自身X轴逆时针）")]
    public float rotationPerStage = 90f;

    [Header("各阶段位移距离（单位）")]
    public float forwardDistance = 0.55f;
    public float upDistance = 6.6f;
    public float backDistance = 0.55f;
    public float downDistance = 6.6f;

    private Vector3[] directions;
    private float[] distances;

    private int currentStage = 0;
    private Vector3 startPos;
    private Vector3 targetPos;

    private float rotatedAngle = 0f;
    private float currentRotationSpeed = 0f;
    private bool isMoving = true;

    void Start()
    {
        directions = new Vector3[]
        {
            Vector3.forward, // 第1阶段
            Vector3.up,      // 第2阶段
            Vector3.back,    // 第3阶段
            Vector3.down     // 第4阶段
        };

        distances = new float[]
        {
            forwardDistance,
            upDistance,
            backDistance,
            downDistance
        };

        // 初始化移动目标
        startPos = transform.position;
        SetTargetForStage(currentStage);
    }

    void Update()
    {
        if (!isMoving) return;

        // 移动到目标点
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // 计算旋转角速度（匀速旋转）
        float segmentDistance = distances[currentStage];
        float moveDuration = segmentDistance / moveSpeed;
        currentRotationSpeed = rotationPerStage / moveDuration;

        // 每帧旋转绕 X 轴（逆时针：Vector3.right）
        float deltaRotation = currentRotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.right, deltaRotation, Space.Self);
        rotatedAngle += deltaRotation;

        // 判断是否完成当前位置与目标点的移动和旋转
        if (Vector3.Distance(transform.position, targetPos) < 0.01f && rotatedAngle >= rotationPerStage)
        {
            currentStage = (currentStage + 1) % directions.Length;
            startPos = transform.position;
            SetTargetForStage(currentStage);
            rotatedAngle = 0f;
        }
    }

    void SetTargetForStage(int stage)
    {
        // 获取当前阶段方向（局部方向）并计算目标位置
        Vector3 localDir = directions[stage];
        float distance = distances[stage];
        Vector3 worldDir = transform.TransformDirection(localDir);
        targetPos = startPos + worldDir * distance;
    }
}
