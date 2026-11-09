using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class MovementPhase
{
    [Header("移动设置")]
    public Vector3 moveDirection = Vector3.zero;          // 移动方向
    public float duration = 1f;                           // 持续时间(秒)

    [Header("旋转设置")]
    public Vector3 rotation = Vector3.zero;              // 旋转角度(欧拉角)

    [Header("缓动设置")]
    public AnimationCurve speedCurve = AnimationCurve.Linear(0, 1, 1, 1); // 速度曲线
    public bool useRotationLerp = false;                  // 是否使用旋转过渡
    public float rotationLerpSpeed = 5f;                  // 旋转过渡速度
}

public class ArrowMovement : MonoBehaviour
{
    [Header("移动阶段配置")]
    [SerializeField] private List<MovementPhase> movementPhases = new List<MovementPhase>();

    [Header("全局设置")]
    [SerializeField] private float moveSpeed = 2f;        // 基础移动速度
    [SerializeField] private bool loopMovement = false;   // 是否循环移动
    [SerializeField] private bool startOnAwake = true;    // 是否在唤醒时开始移动

    private int currentPhaseIndex = 0;                    // 当前阶段索引
    private float phaseTimer = 0f;                        // 阶段计时器
    private float totalTimer = 0f;                        // 总计时器
    private bool isMoving = false;                        // 是否正在移动
    private Vector3 initialRotation;                      // 初始旋转

    void Awake()
    {
        // 保存初始旋转
        initialRotation = transform.eulerAngles;

        // 如果列表为空，添加两个默认阶段
        if (movementPhases.Count == 0)
        {
            AddDefaultPhases();
        }
    }

    void Start()
    {
        if (startOnAwake)
        {
            StartMovement();
        }
    }

    void Update()
    {
        if (!isMoving || movementPhases.Count == 0) return;

        // 获取当前阶段
        MovementPhase currentPhase = movementPhases[currentPhaseIndex];

        // 更新计时器
        phaseTimer += Time.deltaTime;
        totalTimer += Time.deltaTime;

        // 计算当前阶段的进度(0到1)
        float phaseProgress = Mathf.Clamp01(phaseTimer / currentPhase.duration);

        // 获取速度曲线值
        float speedMultiplier = currentPhase.speedCurve.Evaluate(phaseProgress);

        // 移动物体
        Vector3 moveDirection = currentPhase.moveDirection.normalized;
        transform.position += moveDirection * moveSpeed * speedMultiplier * Time.deltaTime;

        // 处理旋转
        HandleRotation(currentPhase, phaseProgress);

        // 检查是否应该切换到下一阶段
        if (phaseTimer >= currentPhase.duration)
        {
            MoveToNextPhase();
        }
    }

    /// <summary>
    /// 处理旋转
    /// </summary>
    private void HandleRotation(MovementPhase phase, float progress)
    {
        if (phase.useRotationLerp && progress < 0.99f)
        {
            // 使用平滑过渡
            Quaternion targetRotation = Quaternion.Euler(initialRotation + phase.rotation);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                phase.rotationLerpSpeed * Time.deltaTime
            );
        }
        else
        {
            // 直接设置旋转
            transform.rotation = Quaternion.Euler(initialRotation + phase.rotation);
        }
    }

    /// <summary>
    /// 切换到下一阶段
    /// </summary>
    private void MoveToNextPhase()
    {
        currentPhaseIndex++;

        if (currentPhaseIndex >= movementPhases.Count)
        {
            if (loopMovement)
            {
                // 循环：回到第一阶段
                currentPhaseIndex = 0;
                ResetToInitialState();
                Debug.Log($"循环移动，重新开始第一阶段");
            }
            else
            {
                // 完成所有阶段
                isMoving = false;
                Debug.Log($"所有移动阶段完成！总耗时: {totalTimer:F2}秒");
                return;
            }
        }
        else
        {
            Debug.Log($"切换到第{currentPhaseIndex + 1}阶段: " +
                     $"方向={movementPhases[currentPhaseIndex].moveDirection}, " +
                     $"持续时间={movementPhases[currentPhaseIndex].duration}秒");
        }

        // 重置阶段计时器
        phaseTimer = 0f;
    }

    /// <summary>
    /// 重置到初始状态
    /// </summary>
    private void ResetToInitialState()
    {
        phaseTimer = 0f;
        totalTimer = 0f;
        transform.rotation = Quaternion.Euler(initialRotation);
    }

    /// <summary>
    /// 开始移动
    /// </summary>
    public void StartMovement()
    {
        if (movementPhases.Count == 0)
        {
            Debug.LogWarning("没有配置移动阶段！");
            return;
        }

        isMoving = true;
        currentPhaseIndex = 0;
        ResetToInitialState();

        Debug.Log($"开始移动，共{movementPhases.Count}个阶段");
    }

    /// <summary>
    /// 暂停移动
    /// </summary>
    public void PauseMovement()
    {
        isMoving = false;
        Debug.Log("移动已暂停");
    }

    /// <summary>
    /// 恢复移动
    /// </summary>
    public void ResumeMovement()
    {
        isMoving = true;
        Debug.Log("移动已恢复");
    }

    /// <summary>
    /// 停止移动并重置
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
        currentPhaseIndex = 0;
        ResetToInitialState();
        Debug.Log("移动已停止并重置");
    }

    /// <summary>
    /// 跳转到指定阶段
    /// </summary>
    public void JumpToPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= movementPhases.Count)
        {
            Debug.LogError($"阶段索引{phaseIndex}无效，有效范围: 0-{movementPhases.Count - 1}");
            return;
        }

        currentPhaseIndex = phaseIndex;
        phaseTimer = 0f;

        // 立即应用该阶段的旋转
        MovementPhase phase = movementPhases[phaseIndex];
        transform.rotation = Quaternion.Euler(initialRotation + phase.rotation);

        Debug.Log($"跳转到第{phaseIndex + 1}阶段");
    }

    /// <summary>
    /// 添加一个移动阶段
    /// </summary>
    public void AddPhase(Vector3 direction, float duration, Vector3 rotation)
    {
        MovementPhase newPhase = new MovementPhase
        {
            moveDirection = direction,
            duration = duration,
            rotation = rotation
        };

        movementPhases.Add(newPhase);
        Debug.Log($"添加了新阶段: 方向={direction}, 持续时间={duration}秒, 旋转={rotation}");
    }

    /// <summary>
    /// 移除指定阶段
    /// </summary>
    public void RemovePhase(int index)
    {
        if (index >= 0 && index < movementPhases.Count)
        {
            movementPhases.RemoveAt(index);
            Debug.Log($"移除了第{index + 1}阶段");
        }
    }

    /// <summary>
    /// 获取当前阶段信息
    /// </summary>
    public string GetCurrentPhaseInfo()
    {
        if (!isMoving || movementPhases.Count == 0) return "未在移动";

        MovementPhase current = movementPhases[currentPhaseIndex];
        float progress = Mathf.Clamp01(phaseTimer / current.duration) * 100f;

        return $"阶段 {currentPhaseIndex + 1}/{movementPhases.Count}\n" +
               $"方向: {current.moveDirection}\n" +
               $"进度: {progress:F1}%\n" +
               $"时间: {phaseTimer:F1}/{current.duration:F1}秒";
    }

    /// <summary>
    /// 添加默认阶段（你原来的需求）
    /// </summary>
    private void AddDefaultPhases()
    {
        // 第一阶段：Z轴负向，3秒
        MovementPhase phase1 = new MovementPhase
        {
            moveDirection = Vector3.back,
            duration = 3f,
            rotation = new Vector3(-90f, 0f, 0f)
        };

        // 第二阶段：X轴负向，2秒
        MovementPhase phase2 = new MovementPhase
        {
            moveDirection = Vector3.left,
            duration = 2f,
            rotation = new Vector3(-90f, 0f, 90f)
        };

        movementPhases.Add(phase1);
        movementPhases.Add(phase2);
    }

    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !isMoving || movementPhases.Count == 0) return;

        // 绘制当前移动方向
        MovementPhase current = movementPhases[currentPhaseIndex];
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, current.moveDirection.normalized * 2f);

        // 绘制前进方向（实际朝向）
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);

        // 绘制阶段指示球
        float sphereSize = 0.2f + (currentPhaseIndex * 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, sphereSize);

        // 绘制阶段路径预览
        DrawPathPreview();
    }

    /// <summary>
    /// 绘制路径预览
    /// </summary>
    private void DrawPathPreview()
    {
        Gizmos.color = Color.yellow;
        Vector3 startPos = transform.position;
        Vector3 currentPos = startPos;

        for (int i = currentPhaseIndex; i < movementPhases.Count; i++)
        {
            MovementPhase phase = movementPhases[i];
            Vector3 endPos = currentPos + phase.moveDirection.normalized * moveSpeed * phase.duration;

            Gizmos.DrawLine(currentPos, endPos);
            Gizmos.DrawSphere(endPos, 0.1f);

            currentPos = endPos;
        }
    }
}