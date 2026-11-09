using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 链条分段循环运动：
/// 左侧直线 → 从动轮圆弧 → 右侧直线 → 主动轮圆弧。
///
/// 使用要求：
/// 1. 脚本挂载对象的直接子物体是完整链节；
/// 2. 创建主动轮、从动轮圆心标记；
/// 3. 创建四个链轮切点标记；
/// 4. 链节只在链轮圆弧区域改变方向；
/// 5. 两侧直线区域保持固定方向。
/// </summary>
public class ChainStraightArcLoop : MonoBehaviour
{
    [Header("链节识别")]

    [Tooltip("完整链节名称中包含的关键字。留空则收集所有带网格的直接子对象。")]
    [SerializeField]
    private string linkNameKeyword = "链条";

    [Header("主动轮：通常位于下方")]

    [Tooltip("主动轮圆心")]
    [SerializeField]
    private Transform drivingWheelCenter;

    [Tooltip("主动轮左侧链条接触点")]
    [SerializeField]
    private Transform drivingLeftTangent;

    [Tooltip("主动轮右侧链条接触点")]
    [SerializeField]
    private Transform drivingRightTangent;

    [Header("从动轮：通常位于上方")]

    [Tooltip("从动轮圆心")]
    [SerializeField]
    private Transform drivenWheelCenter;

    [Tooltip("从动轮左侧链条接触点")]
    [SerializeField]
    private Transform drivenLeftTangent;

    [Tooltip("从动轮右侧链条接触点")]
    [SerializeField]
    private Transform drivenRightTangent;

    [Header("圆弧方向")]

    [Tooltip("从左切点绕到右切点时，从动轮圆弧是否顺时针")]
    [SerializeField]
    private bool drivenArcClockwise = true;

    [Tooltip("从右切点绕到左切点时，主动轮圆弧是否顺时针")]
    [SerializeField]
    private bool drivingArcClockwise = true;

    [Header("运行参数")]

    [Tooltip("链条线速度，单位为Unity单位/秒")]
    [Min(0f)]
    [SerializeField]
    private float moveSpeed = 0.15f;

    [Tooltip("反转链条运行方向")]
    [SerializeField]
    private bool reverseDirection;

    [Tooltip("进入播放模式后自动运行")]
    [SerializeField]
    private bool playOnStart = true;

    [Header("链条所在平面")]

    [Tooltip(
        "链条所在平面的局部法向。" +
        "局部XY平面一般为(0,0,1)，" +
        "局部XZ平面一般为(0,1,0)，" +
        "局部YZ平面一般为(1,0,0)。"
    )]
    [SerializeField]
    private Vector3 localPlaneNormal = Vector3.forward;

    [Header("链节姿态")]

    [Tooltip("模型整体方向不正确时使用该参数修正")]
    [SerializeField]
    private Vector3 rotationCorrection = Vector3.zero;

    [Tooltip("链节是否根据路径改变方向")]
    [SerializeField]
    private bool followPathRotation = true;

    [Header("运行时信息")]

    [Tooltip("运行时自动生成，不需要手动填写")]
    [SerializeField]
    private Transform[] orderedLinks;

    [Header("链条平面自动计算")]

    [Tooltip("圆弧方向完全相反时勾选")]
    [SerializeField]
    private bool invertAutoPlaneNormal;

    // 主动轮路径数据
    private Vector3 drivingCenterLocal;
    private Vector3 drivingLeftLocal;
    private Vector3 drivingRightLocal;
    private Vector3 drivingArcStartRadial;
    private float drivingRadius;
    private float drivingArcDegrees;

    // 从动轮路径数据
    private Vector3 drivenCenterLocal;
    private Vector3 drivenLeftLocal;
    private Vector3 drivenRightLocal;
    private Vector3 drivenArcStartRadial;
    private float drivenRadius;
    private float drivenArcDegrees;

    // 四段路径长度
    private float leftLineLength;
    private float drivenArcLength;
    private float rightLineLength;
    private float drivingArcLength;

    private float totalPathLength;
    private float linkSpacing;
    private float travelledDistance;
    private float initialPhase;

    // 链节统一姿态
    private Vector3 planeNormal;
    private Vector3 referenceTangent;
    private Quaternion referencePathRotation;
    private Quaternion commonModelOffset;

    private bool initialized;
    private bool running;

    private sealed class LinkEntry
    {
        public Transform Link;
        public float Distance;
    }

    private void Awake()
    {
        InitializeChain();
        running = initialized && playOnStart;
    }

    private void Update()
    {
        if (!initialized || !running)
        {
            return;
        }

        float direction = reverseDirection ? -1f : 1f;

        travelledDistance = Mathf.Repeat(
            travelledDistance
            + direction * moveSpeed * Time.deltaTime,
            totalPathLength
        );

        ApplyChainTransforms();
    }

    /// <summary>
    /// 初始化路径和链节。
    /// </summary>
    private void InitializeChain()
    {
        initialized = false;

        if (!ValidateMarkers())
        {
            return;
        }

        // 根据六个定位点自动计算链条平面，
        // 避免父对象旋转后手动填写轴向错误。
        if (!CalculatePlaneNormalFromMarkers())
        {
            return;
        }

        BuildSegmentedPath();

        if (totalPathLength <= 0.0001f)
        {
            Debug.LogError(
                $"{name}：链条路径总长度无效。",
                this
            );

            return;
        }

        List<Transform> links = CollectChainLinks();

        if (links.Count < 3)
        {
            Debug.LogError(
                $"{name}：只找到 {links.Count} 个完整链节，" +
                "请检查脚本挂载层级和链节名称关键字。",
                this
            );

            return;
        }

        // 根据链节当前所处的路径位置排序
        List<LinkEntry> entries = new List<LinkEntry>();

        foreach (Transform link in links)
        {
            entries.Add(
                new LinkEntry
                {
                    Link = link,
                    Distance = FindNearestDistanceOnPath(
                        link.localPosition
                    )
                }
            );
        }

        entries.Sort(
            (a, b) => a.Distance.CompareTo(b.Distance)
        );

        orderedLinks = new Transform[entries.Count];

        for (int i = 0; i < entries.Count; i++)
        {
            orderedLinks[i] = entries[i].Link;
        }

        linkSpacing =
            totalPathLength / orderedLinks.Length;

        // 尽量保持链条初始相位
        initialPhase = entries[0].Distance;

        // 使用第一个链节建立模型方向基准
        EvaluatePath(
            initialPhase,
            out _,
            out referenceTangent
        );

        referenceTangent =
            Vector3.ProjectOnPlane(
                referenceTangent,
                planeNormal
            ).normalized;

        if (referenceTangent.sqrMagnitude < 0.000001f)
        {
            Debug.LogError(
                $"{name}：无法计算参考路径方向。",
                this
            );

            return;
        }

        referencePathRotation =
            Quaternion.LookRotation(
                referenceTangent,
                planeNormal
            );

        commonModelOffset =
            Quaternion.Inverse(referencePathRotation)
            * orderedLinks[0].localRotation;

        travelledDistance = 0f;
        initialized = true;

        ApplyChainTransforms();

        Debug.Log(
            $"{name}：分段链条初始化完成。" +
            $"链节数量={orderedLinks.Length}，" +
            $"路径长度={totalPathLength:F3}，" +
            $"链节间距={linkSpacing:F3}",
            this
        );
    }

    /// <summary>
    /// 根据主动轮、从动轮的圆心和左右切点，
    /// 自动计算链条所在平面的局部法向。
    /// </summary>
    private bool CalculatePlaneNormalFromMarkers()
    {
        Vector3 drivingCenter =
            transform.InverseTransformPoint(
                drivingWheelCenter.position
            );

        Vector3 drivenCenter =
            transform.InverseTransformPoint(
                drivenWheelCenter.position
            );

        Vector3 drivingLeft =
            transform.InverseTransformPoint(
                drivingLeftTangent.position
            );

        Vector3 drivingRight =
            transform.InverseTransformPoint(
                drivingRightTangent.position
            );

        Vector3 drivenLeft =
            transform.InverseTransformPoint(
                drivenLeftTangent.position
            );

        Vector3 drivenRight =
            transform.InverseTransformPoint(
                drivenRightTangent.position
            );

        // 左切点指向右切点，表示链轮的横向方向
        Vector3 drivingWidthDirection =
            drivingRight - drivingLeft;

        Vector3 drivenWidthDirection =
            drivenRight - drivenLeft;

        // 取主动轮和从动轮横向方向的平均值
        Vector3 widthDirection =
            drivingWidthDirection.normalized +
            drivenWidthDirection.normalized;

        // 主动轮圆心指向从动轮圆心
        Vector3 centerDirection =
            drivenCenter - drivingCenter;

        if (
            widthDirection.sqrMagnitude < 0.000001f ||
            centerDirection.sqrMagnitude < 0.000001f
        )
        {
            Debug.LogError(
                $"{name}：无法计算链条平面。" +
                "请检查主动轮、从动轮圆心以及左右切点位置。",
                this
            );

            return false;
        }

        widthDirection.Normalize();
        centerDirection.Normalize();

        // 横向方向 × 上下方向 = 链条平面法向
        planeNormal =
            Vector3.Cross(
                widthDirection,
                centerDirection
            );

        if (planeNormal.sqrMagnitude < 0.000001f)
        {
            Debug.LogError(
                $"{name}：链条平面法向计算失败。" +
                "左右切点方向可能与两个轮心方向平行。",
                this
            );

            return false;
        }

        planeNormal.Normalize();

        if (invertAutoPlaneNormal)
        {
            planeNormal = -planeNormal;
        }

        Debug.Log(
            $"{name}：自动计算的链条局部平面法向为 " +
            $"{planeNormal}。",
            this
        );

        return true;
    }

    /// <summary>
    /// 检查六个路径标记是否完整。
    /// </summary>
    private bool ValidateMarkers()
    {
        if (
            drivingWheelCenter == null
            || drivingLeftTangent == null
            || drivingRightTangent == null
            || drivenWheelCenter == null
            || drivenLeftTangent == null
            || drivenRightTangent == null
        )
        {
            Debug.LogError(
                $"{name}：主动轮、从动轮圆心或切点没有设置完整。",
                this
            );

            return false;
        }

        return true;
    }

    /// <summary>
    /// 构建两条直线和两个圆弧。
    /// </summary>
    private void BuildSegmentedPath()
    {
        drivingCenterLocal =
            transform.InverseTransformPoint(
                drivingWheelCenter.position
            );

        drivenCenterLocal =
            transform.InverseTransformPoint(
                drivenWheelCenter.position
            );

        Vector3 drivingLeftVector =
            Vector3.ProjectOnPlane(
                transform.InverseTransformPoint(
                    drivingLeftTangent.position
                ) - drivingCenterLocal,
                planeNormal
            );

        Vector3 drivingRightVector =
            Vector3.ProjectOnPlane(
                transform.InverseTransformPoint(
                    drivingRightTangent.position
                ) - drivingCenterLocal,
                planeNormal
            );

        Vector3 drivenLeftVector =
            Vector3.ProjectOnPlane(
                transform.InverseTransformPoint(
                    drivenLeftTangent.position
                ) - drivenCenterLocal,
                planeNormal
            );

        Vector3 drivenRightVector =
            Vector3.ProjectOnPlane(
                transform.InverseTransformPoint(
                    drivenRightTangent.position
                ) - drivenCenterLocal,
                planeNormal
            );

        if (
            drivingLeftVector.sqrMagnitude < 0.000001f
            || drivingRightVector.sqrMagnitude < 0.000001f
            || drivenLeftVector.sqrMagnitude < 0.000001f
            || drivenRightVector.sqrMagnitude < 0.000001f
        )
        {
            Debug.LogError(
                $"{name}：某个切点与链轮圆心重合。",
                this
            );

            return;
        }

        // 使用两个切点距离的平均值作为轮半径
        drivingRadius =
            (
                drivingLeftVector.magnitude
                + drivingRightVector.magnitude
            ) * 0.5f;

        drivenRadius =
            (
                drivenLeftVector.magnitude
                + drivenRightVector.magnitude
            ) * 0.5f;

        drivingLeftLocal =
            drivingCenterLocal
            + drivingLeftVector.normalized
            * drivingRadius;

        drivingRightLocal =
            drivingCenterLocal
            + drivingRightVector.normalized
            * drivingRadius;

        drivenLeftLocal =
            drivenCenterLocal
            + drivenLeftVector.normalized
            * drivenRadius;

        drivenRightLocal =
            drivenCenterLocal
            + drivenRightVector.normalized
            * drivenRadius;

        // 路径顺序：
        // 主动轮左切点 → 从动轮左切点
        leftLineLength =
            Vector3.Distance(
                drivingLeftLocal,
                drivenLeftLocal
            );

        // 从动轮左切点 → 从动轮右切点
        drivenArcStartRadial =
            drivenLeftLocal - drivenCenterLocal;

        drivenArcDegrees =
            CalculateDirectedArcDegrees(
                drivenArcStartRadial,
                drivenRightLocal - drivenCenterLocal,
                drivenArcClockwise
            );

        drivenArcLength =
            Mathf.Abs(drivenArcDegrees)
            * Mathf.Deg2Rad
            * drivenRadius;

        // 从动轮右切点 → 主动轮右切点
        rightLineLength =
            Vector3.Distance(
                drivenRightLocal,
                drivingRightLocal
            );

        // 主动轮右切点 → 主动轮左切点
        drivingArcStartRadial =
            drivingRightLocal - drivingCenterLocal;

        drivingArcDegrees =
            CalculateDirectedArcDegrees(
                drivingArcStartRadial,
                drivingLeftLocal - drivingCenterLocal,
                drivingArcClockwise
            );

        drivingArcLength =
            Mathf.Abs(drivingArcDegrees)
            * Mathf.Deg2Rad
            * drivingRadius;

        totalPathLength =
            leftLineLength
            + drivenArcLength
            + rightLineLength
            + drivingArcLength;
    }

    /// <summary>
    /// 根据圆弧方向计算有符号角度。
    /// 顺时针为负角度，逆时针为正角度。
    /// </summary>
    private float CalculateDirectedArcDegrees(
        Vector3 startRadial,
        Vector3 endRadial,
        bool clockwise
    )
    {
        float angle =
            Vector3.SignedAngle(
                startRadial,
                endRadial,
                planeNormal
            );

        if (clockwise)
        {
            if (angle > 0f)
            {
                angle -= 360f;
            }
        }
        else
        {
            if (angle < 0f)
            {
                angle += 360f;
            }
        }

        return angle;
    }

    /// <summary>
    /// 收集完整链节父对象。
    /// </summary>
    private List<Transform> CollectChainLinks()
    {
        List<Transform> links =
            new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (
                !string.IsNullOrWhiteSpace(linkNameKeyword)
                && !child.name.Contains(linkNameKeyword)
            )
            {
                continue;
            }

            MeshRenderer renderer =
                child.GetComponentInChildren<MeshRenderer>(true);

            if (renderer != null)
            {
                links.Add(child);
            }
        }

        return links;
    }

    /// <summary>
    /// 根据路径距离计算位置和切线。
    /// 两侧直线段方向固定；
    /// 只有主动轮、从动轮圆弧段方向连续变化。
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

        // 第一段：左侧直线，从主动轮向从动轮
        if (distance < leftLineLength)
        {
            float t =
                leftLineLength > 0.0001f
                    ? distance / leftLineLength
                    : 0f;

            position =
                Vector3.Lerp(
                    drivingLeftLocal,
                    drivenLeftLocal,
                    t
                );

            tangent =
                (
                    drivenLeftLocal
                    - drivingLeftLocal
                ).normalized;

            return;
        }

        distance -= leftLineLength;

        // 第二段：从动轮圆弧
        if (distance < drivenArcLength)
        {
            EvaluateArc(
                drivenCenterLocal,
                drivenArcStartRadial,
                drivenRadius,
                drivenArcDegrees,
                distance,
                drivenArcLength,
                out position,
                out tangent
            );

            return;
        }

        distance -= drivenArcLength;

        // 第三段：右侧直线，从从动轮向主动轮
        if (distance < rightLineLength)
        {
            float t =
                rightLineLength > 0.0001f
                    ? distance / rightLineLength
                    : 0f;

            position =
                Vector3.Lerp(
                    drivenRightLocal,
                    drivingRightLocal,
                    t
                );

            tangent =
                (
                    drivingRightLocal
                    - drivenRightLocal
                ).normalized;

            return;
        }

        distance -= rightLineLength;

        // 第四段：主动轮圆弧
        EvaluateArc(
            drivingCenterLocal,
            drivingArcStartRadial,
            drivingRadius,
            drivingArcDegrees,
            distance,
            drivingArcLength,
            out position,
            out tangent
        );
    }

    /// <summary>
    /// 计算圆弧上的位置与切线。
    /// </summary>
    private void EvaluateArc(
        Vector3 center,
        Vector3 startRadial,
        float radius,
        float arcDegrees,
        float distance,
        float arcLength,
        out Vector3 position,
        out Vector3 tangent
    )
    {
        float t =
            arcLength > 0.0001f
                ? Mathf.Clamp01(distance / arcLength)
                : 0f;

        float currentAngle =
            arcDegrees * t;

        Vector3 radial =
            Quaternion.AngleAxis(
                currentAngle,
                planeNormal
            )
            * startRadial.normalized
            * radius;

        position =
            center + radial;

        Vector3 positiveTangent =
            Vector3.Cross(
                planeNormal,
                radial
            ).normalized;

        tangent =
            arcDegrees >= 0f
                ? positiveTangent
                : -positiveTangent;
    }

    /// <summary>
    /// 更新所有链节的位置与方向。
    /// </summary>
    private void ApplyChainTransforms()
    {
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
                    initialPhase
                    + travelledDistance
                    + i * linkSpacing,
                    totalPathLength
                );

            EvaluatePath(
                distance,
                out Vector3 position,
                out Vector3 tangent
            );

            link.localPosition = position;

            if (followPathRotation)
            {
                Quaternion pathRotation =
                    BuildStableRotation(tangent);

                link.localRotation =
                    pathRotation
                    * commonModelOffset
                    * correction;
            }
        }
    }

    /// <summary>
    /// 只围绕链条平面法向旋转，
    /// 防止链节发生空间翻转。
    /// </summary>
    private Quaternion BuildStableRotation(
        Vector3 tangent
    )
    {
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

        return
            Quaternion.AngleAxis(
                angle,
                planeNormal
            )
            * referencePathRotation;
    }

    /// <summary>
    /// 查找链节当前位置对应的最近路径距离。
    /// </summary>
    private float FindNearestDistanceOnPath(
        Vector3 point
    )
    {
        float bestDistance = 0f;
        float bestSqrDistance = float.MaxValue;

        // 左侧直线
        EvaluateNearestLinePoint(
            point,
            drivingLeftLocal,
            drivenLeftLocal,
            0f,
            ref bestDistance,
            ref bestSqrDistance
        );

        // 从动轮圆弧
        EvaluateNearestArcPoint(
            point,
            drivenCenterLocal,
            drivenArcStartRadial,
            drivenRadius,
            drivenArcDegrees,
            leftLineLength,
            ref bestDistance,
            ref bestSqrDistance
        );

        // 右侧直线
        EvaluateNearestLinePoint(
            point,
            drivenRightLocal,
            drivingRightLocal,
            leftLineLength + drivenArcLength,
            ref bestDistance,
            ref bestSqrDistance
        );

        // 主动轮圆弧
        EvaluateNearestArcPoint(
            point,
            drivingCenterLocal,
            drivingArcStartRadial,
            drivingRadius,
            drivingArcDegrees,
            leftLineLength
            + drivenArcLength
            + rightLineLength,
            ref bestDistance,
            ref bestSqrDistance
        );

        return bestDistance;
    }

    private void EvaluateNearestLinePoint(
        Vector3 point,
        Vector3 start,
        Vector3 end,
        float pathStartDistance,
        ref float bestDistance,
        ref float bestSqrDistance
    )
    {
        Vector3 line = end - start;

        float lineLengthSquared =
            line.sqrMagnitude;

        if (lineLengthSquared < 0.000001f)
        {
            return;
        }

        float t =
            Mathf.Clamp01(
                Vector3.Dot(
                    point - start,
                    line
                )
                / lineLengthSquared
            );

        Vector3 closest =
            start + line * t;

        float sqrDistance =
            (point - closest).sqrMagnitude;

        if (sqrDistance < bestSqrDistance)
        {
            bestSqrDistance = sqrDistance;

            bestDistance =
                pathStartDistance
                + Mathf.Sqrt(lineLengthSquared)
                * t;
        }
    }

    private void EvaluateNearestArcPoint(
        Vector3 point,
        Vector3 center,
        Vector3 startRadial,
        float radius,
        float arcDegrees,
        float pathStartDistance,
        ref float bestDistance,
        ref float bestSqrDistance
    )
    {
        Vector3 targetRadial =
            Vector3.ProjectOnPlane(
                point - center,
                planeNormal
            );

        if (targetRadial.sqrMagnitude < 0.000001f)
        {
            return;
        }

        bool clockwise =
            arcDegrees < 0f;

        float targetDegrees =
            CalculateDirectedArcDegrees(
                startRadial,
                targetRadial,
                clockwise
            );

        float arcMagnitude =
            Mathf.Abs(arcDegrees);

        float targetMagnitude =
            Mathf.Clamp(
                Mathf.Abs(targetDegrees),
                0f,
                arcMagnitude
            );

        float signedDegrees =
            Mathf.Sign(arcDegrees)
            * targetMagnitude;

        Vector3 radial =
            Quaternion.AngleAxis(
                signedDegrees,
                planeNormal
            )
            * startRadial.normalized
            * radius;

        Vector3 closest =
            center + radial;

        float sqrDistance =
            (point - closest).sqrMagnitude;

        if (sqrDistance < bestSqrDistance)
        {
            bestSqrDistance =
                sqrDistance;

            bestDistance =
                pathStartDistance
                + targetMagnitude
                * Mathf.Deg2Rad
                * radius;
        }
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
}