using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 控制多个独立链节沿当前排布形成的闭合路径循环运动。
///
/// 功能：
/// 1. 自动收集完整链节父对象；
/// 2. 自动生成闭合路径；
/// 3. 链节沿路径等间距运行；
/// 4. 链节经过主动轮、从动轮时跟随路径转向；
/// 5. 将相差180度的路径方向视为同一轴向，避免链节变歪或翻面；
/// 6. 所有链节使用统一姿态偏移，避免不同链节原始旋转不一致。
/// </summary>
public class ClosedChainLoop : MonoBehaviour
{
    [Header("链节识别")]

    [Tooltip("完整链节名称中包含的关键字。留空时收集所有带网格的直接子对象。")]
    [SerializeField]
    private string linkNameKeyword = "链条";

    [Header("路径排序辅助")]

    [Tooltip("指定一个完整链节作为闭合路径起点。")]
    [SerializeField]
    private Transform startLink;

    [Tooltip("指定与起点实际相邻的下一个完整链节，用来确定路径方向。")]
    [SerializeField]
    private Transform secondLink;

    [Header("运行参数")]

    [Tooltip("链条沿闭合路径移动的线速度。")]
    [Min(0f)]
    [SerializeField]
    private float moveSpeed = 0.15f;

    [Tooltip("反转链条运行方向。")]
    [SerializeField]
    private bool reverseDirection;

    [Tooltip("进入播放模式后自动运行。")]
    [SerializeField]
    private bool playOnStart = true;

    [Header("链节姿态")]

    [Tooltip(
        "链条所在平面的局部法向轴。" +
        "局部XY平面通常为(0,0,1)，" +
        "局部XZ平面通常为(0,1,0)，" +
        "局部YZ平面通常为(1,0,0)。"
    )]
    [SerializeField]
    private Vector3 localPlaneNormal = Vector3.forward;

    [Tooltip("链节整体朝向不正确时，通过这里修正。")]
    [SerializeField]
    private Vector3 rotationCorrection = Vector3.zero;

    [Tooltip("链节是否跟随路径方向转动。")]
    [SerializeField]
    private bool followPathRotation = true;

    [Header("切线平滑")]

    [Tooltip("切线采样距离占链节间距的比例。")]
    [Range(0.05f, 1f)]
    [SerializeField]
    private float tangentSampleRatio = 0.4f;

    [Header("运行时生成的信息")]

    [Tooltip("运行时自动生成，不需要手动填写。")]
    [SerializeField]
    private Transform[] orderedLinks;

    // 闭合路径上的局部位置
    private Vector3[] pathPositions;

    // 每一段路径的长度
    private float[] segmentLengths;

    // 每个路径节点的累计距离
    private float[] cumulativeDistances;

    // 闭合路径总长度
    private float totalPathLength;

    // 链节统一间距
    private float linkSpacing;

    // 当前链条已经运动的距离
    private float travelledDistance;

    // 稳定旋转使用的参考路径切线
    private Vector3 referenceTangent;

    // 稳定旋转使用的参考路径旋转
    private Quaternion referencePathRotation;

    // 所有链节共同使用的模型姿态偏移
    private Quaternion commonRotationOffset;

    private bool initialized;
    private bool running;

    private void Awake()
    {
        InitializeChain();

        running =
            initialized &&
            playOnStart;
    }

    private void Update()
    {
        if (!initialized || !running)
        {
            return;
        }

        MoveChain();
    }

    /// <summary>
    /// 初始化链节、路径、间距和旋转姿态。
    /// </summary>
    private void InitializeChain()
    {
        initialized = false;

        List<Transform> allLinks =
            CollectChainLinks();

        if (allLinks.Count < 3)
        {
            Debug.LogError(
                $"{name}：只找到 {allLinks.Count} 个完整链节。" +
                "闭合链条至少需要3个链节，请检查脚本挂载层级。",
                this
            );

            return;
        }

        List<Transform> sortedLinks =
            BuildSpatialOrder(allLinks);

        if (sortedLinks.Count != allLinks.Count)
        {
            Debug.LogError(
                $"{name}：链节自动排序失败。" +
                $"检测到 {allLinks.Count} 个链节，" +
                $"但只排列了 {sortedLinks.Count} 个。",
                this
            );

            return;
        }

        orderedLinks =
            sortedLinks.ToArray();

        int linkCount =
            orderedLinks.Length;

        pathPositions =
            new Vector3[linkCount];

        segmentLengths =
            new float[linkCount];

        cumulativeDistances =
            new float[linkCount];

        for (int i = 0; i < linkCount; i++)
        {
            if (orderedLinks[i] == null)
            {
                Debug.LogError(
                    $"{name}：第 {i} 个链节为空。",
                    this
                );

                return;
            }

            pathPositions[i] =
                orderedLinks[i].localPosition;
        }

        BuildPathLengths();

        if (totalPathLength <= 0.0001f)
        {
            Debug.LogError(
                $"{name}：闭合路径总长度无效。",
                this
            );

            return;
        }

        // 让所有链节沿闭合路径保持相同间距
        linkSpacing =
            totalPathLength /
            linkCount;

        Vector3 planeNormal =
            GetValidPlaneNormal();

        // 获取路径起始位置的参考切线
        EvaluateSmoothTangent(
            0f,
            out Vector3 initialTangent
        );

        referenceTangent =
            Vector3.ProjectOnPlane(
                initialTangent,
                planeNormal
            );

        if (referenceTangent.sqrMagnitude < 0.000001f)
        {
            Debug.LogError(
                $"{name}：无法计算有效参考切线，" +
                "请检查 Local Plane Normal。",
                this
            );

            return;
        }

        referenceTangent.Normalize();

        // 只在初始化时使用一次LookRotation建立参考坐标系
        referencePathRotation =
            Quaternion.LookRotation(
                referenceTangent,
                planeNormal
            );

        // 使用第一个完整链节计算统一的模型姿态偏移
        EvaluateSmoothTangent(
            0f,
            out Vector3 startTangent
        );

        Quaternion startPathRotation =
            BuildStablePathRotation(
                startTangent,
                planeNormal
            );

        commonRotationOffset =
            Quaternion.Inverse(startPathRotation) *
            orderedLinks[0].localRotation;

        travelledDistance = 0f;
        initialized = true;

        ApplyChainTransforms();

        Debug.Log(
            $"{name}：链条初始化完成。" +
            $"链节数量：{linkCount}，" +
            $"路径长度：{totalPathLength:F3}，" +
            $"链节间距：{linkSpacing:F3}。",
            this
        );
    }

    /// <summary>
    /// 收集脚本对象下面的完整链节。
    /// 只把直接子对象作为完整链节，
    /// 不会收集“切除-拉伸”“凸台-拉伸”等内部网格。
    /// </summary>
    private List<Transform> CollectChainLinks()
    {
        List<Transform> links =
            new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child =
                transform.GetChild(i);

            if (child == null)
            {
                continue;
            }

            if (
                !string.IsNullOrWhiteSpace(linkNameKeyword) &&
                !child.name.Contains(linkNameKeyword)
            )
            {
                continue;
            }

            MeshRenderer renderer =
                child.GetComponentInChildren<MeshRenderer>(true);

            if (renderer == null)
            {
                continue;
            }

            links.Add(child);
        }

        Debug.Log(
            $"{name}：找到 {links.Count} 个完整链节。",
            this
        );

        return links;
    }

    /// <summary>
    /// 根据链节之间的空间距离建立链条连接顺序。
    /// </summary>
    private List<Transform> BuildSpatialOrder(
        List<Transform> allLinks
    )
    {
        List<Transform> result =
            new List<Transform>();

        HashSet<Transform> remaining =
            new HashSet<Transform>(allLinks);

        Transform firstLink;

        if (
            startLink != null &&
            remaining.Contains(startLink)
        )
        {
            firstLink = startLink;
        }
        else
        {
            firstLink = allLinks[0];

            if (startLink != null)
            {
                Debug.LogWarning(
                    $"{name}：Start Link无效，" +
                    $"自动使用 {firstLink.name} 作为起点。",
                    this
                );
            }
        }

        result.Add(firstLink);
        remaining.Remove(firstLink);

        if (
            secondLink != null &&
            remaining.Contains(secondLink)
        )
        {
            result.Add(secondLink);
            remaining.Remove(secondLink);
        }
        else if (secondLink != null)
        {
            Debug.LogWarning(
                $"{name}：Second Link无效，" +
                "将自动根据空间位置确定方向。",
                this
            );
        }

        while (remaining.Count > 0)
        {
            Transform current =
                result[result.Count - 1];

            Transform previous =
                result.Count >= 2
                    ? result[result.Count - 2]
                    : null;

            Transform bestCandidate = null;
            float bestScore = float.MaxValue;

            foreach (Transform candidate in remaining)
            {
                if (candidate == null)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        current.localPosition,
                        candidate.localPosition
                    );

                float directionPenalty = 0f;

                if (previous != null)
                {
                    Vector3 previousDirection =
                        current.localPosition -
                        previous.localPosition;

                    Vector3 candidateDirection =
                        candidate.localPosition -
                        current.localPosition;

                    if (
                        previousDirection.sqrMagnitude > 0.000001f &&
                        candidateDirection.sqrMagnitude > 0.000001f
                    )
                    {
                        previousDirection.Normalize();
                        candidateDirection.Normalize();

                        float similarity =
                            Vector3.Dot(
                                previousDirection,
                                candidateDirection
                            );

                        directionPenalty =
                            (1f - similarity) *
                            0.25f;
                    }
                }

                float score =
                    distance *
                    (1f + directionPenalty);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate == null)
            {
                Debug.LogError(
                    $"{name}：无法找到下一个链节。",
                    this
                );

                break;
            }

            result.Add(bestCandidate);
            remaining.Remove(bestCandidate);
        }

        return result;
    }

    /// <summary>
    /// 计算每段路径长度和闭合路径总长度。
    /// </summary>
    private void BuildPathLengths()
    {
        totalPathLength = 0f;

        for (int i = 0; i < pathPositions.Length; i++)
        {
            int nextIndex =
                (i + 1) %
                pathPositions.Length;

            cumulativeDistances[i] =
                totalPathLength;

            float length =
                Vector3.Distance(
                    pathPositions[i],
                    pathPositions[nextIndex]
                );

            segmentLengths[i] =
                length;

            totalPathLength +=
                length;
        }
    }

    /// <summary>
    /// 更新链条运行距离。
    /// </summary>
    private void MoveChain()
    {
        float direction =
            reverseDirection
                ? -1f
                : 1f;

        travelledDistance =
            Mathf.Repeat(
                travelledDistance +
                direction *
                moveSpeed *
                Time.deltaTime,
                totalPathLength
            );

        ApplyChainTransforms();
    }

    /// <summary>
    /// 根据路径距离更新所有链节的位置和旋转。
    /// </summary>
    private void ApplyChainTransforms()
    {
        if (
            orderedLinks == null ||
            orderedLinks.Length == 0
        )
        {
            return;
        }

        Vector3 planeNormal =
            GetValidPlaneNormal();

        Quaternion correction =
            Quaternion.Euler(
                rotationCorrection
            );

        for (int i = 0; i < orderedLinks.Length; i++)
        {
            Transform link =
                orderedLinks[i];

            if (link == null)
            {
                continue;
            }

            float distance =
                Mathf.Repeat(
                    travelledDistance +
                    i * linkSpacing,
                    totalPathLength
                );

            EvaluatePath(
                distance,
                out Vector3 position,
                out _
            );

            EvaluateSmoothTangent(
                distance,
                out Vector3 tangent
            );

            link.localPosition =
                position;

            if (followPathRotation)
            {
                Quaternion pathRotation =
                    BuildStablePathRotation(
                        tangent,
                        planeNormal
                    );

                // 所有链节使用统一姿态偏移
                link.localRotation =
                    pathRotation *
                    commonRotationOffset *
                    correction;
            }
        }
    }

    /// <summary>
    /// 根据闭合路径距离计算位置和路径切线。
    /// </summary>
    private void EvaluatePath(
        float distance,
        out Vector3 position,
        out Vector3 tangent
    )
    {
        distance =
            Mathf.Repeat(
                distance,
                totalPathLength
            );

        for (int i = 0; i < segmentLengths.Length; i++)
        {
            float startDistance =
                cumulativeDistances[i];

            float endDistance =
                startDistance +
                segmentLengths[i];

            if (distance <= endDistance)
            {
                int nextIndex =
                    (i + 1) %
                    pathPositions.Length;

                float segmentLength =
                    segmentLengths[i];

                float t =
                    segmentLength > 0.0001f
                        ? (distance - startDistance) /
                          segmentLength
                        : 0f;

                position =
                    Vector3.Lerp(
                        pathPositions[i],
                        pathPositions[nextIndex],
                        t
                    );

                tangent =
                    pathPositions[nextIndex] -
                    pathPositions[i];

                if (tangent.sqrMagnitude > 0.000001f)
                {
                    tangent.Normalize();
                }
                else
                {
                    tangent =
                        referenceTangent.sqrMagnitude > 0.000001f
                            ? referenceTangent
                            : Vector3.forward;
                }

                return;
            }
        }

        position =
            pathPositions[0];

        tangent =
            pathPositions[1] -
            pathPositions[0];

        if (tangent.sqrMagnitude > 0.000001f)
        {
            tangent.Normalize();
        }
        else
        {
            tangent = Vector3.forward;
        }
    }

    /// <summary>
    /// 根据路径前后采样点计算平滑切线。
    /// </summary>
    private void EvaluateSmoothTangent(
        float distance,
        out Vector3 tangent
    )
    {
        float sampleDistance =
            Mathf.Max(
                linkSpacing *
                tangentSampleRatio,
                0.001f
            );

        EvaluatePath(
            distance - sampleDistance,
            out Vector3 previousPosition,
            out _
        );

        EvaluatePath(
            distance + sampleDistance,
            out Vector3 nextPosition,
            out _
        );

        tangent =
            nextPosition -
            previousPosition;

        if (tangent.sqrMagnitude < 0.000001f)
        {
            EvaluatePath(
                distance,
                out _,
                out tangent
            );
        }

        if (tangent.sqrMagnitude > 0.000001f)
        {
            tangent.Normalize();
        }
        else
        {
            tangent =
                referenceTangent.sqrMagnitude > 0.000001f
                    ? referenceTangent
                    : Vector3.forward;
        }
    }

    /// <summary>
    /// 根据路径切线生成稳定旋转。
    ///
    /// 链节只需要沿路径轴线排列，
    /// 不区分切线正方向和反方向。
    /// 因此将相差180度的方向视为同一种链节姿态。
    /// </summary>
    private Quaternion BuildStablePathRotation(
        Vector3 tangent,
        Vector3 planeNormal
    )
    {
        if (referenceTangent.sqrMagnitude < 0.000001f)
        {
            return Quaternion.identity;
        }

        planeNormal =
            planeNormal.normalized;

        // 强制切线保持在链条平面内
        tangent =
            Vector3.ProjectOnPlane(
                tangent,
                planeNormal
            );

        if (tangent.sqrMagnitude < 0.000001f)
        {
            return referencePathRotation;
        }

        tangent.Normalize();

        float angle =
            Vector3.SignedAngle(
                referenceTangent,
                tangent,
                planeNormal
            );

        // 将旋转角限制在-90到90度。
        // 避免链节从上行段进入下行段时翻转180度。
        if (angle > 90f)
        {
            angle -= 180f;
        }
        else if (angle < -90f)
        {
            angle += 180f;
        }

        return
            Quaternion.AngleAxis(
                angle,
                planeNormal
            ) *
            referencePathRotation;
    }

    /// <summary>
    /// 获取有效的链条平面法向。
    /// </summary>
    private Vector3 GetValidPlaneNormal()
    {
        Vector3 normal =
            localPlaneNormal;

        if (normal.sqrMagnitude < 0.000001f)
        {
            normal =
                Vector3.forward;
        }

        return normal.normalized;
    }

    public void SetRunning(bool value)
    {
        running = value;
    }

    public void SetReverse(bool value)
    {
        reverseDirection = value;
    }

    public void SetSpeed(float value)
    {
        moveSpeed =
            Mathf.Max(0f, value);
    }

    public void ReinitializeChain()
    {
        InitializeChain();

        running =
            initialized &&
            playOnStart;
    }
}