using System.Collections.Generic;
using UnityEngine;

public abstract class DuodouMotionBase : MonoBehaviour
{
    public enum MotionDataMode
    {
        LocalSimulation,
        RealtimeSpeed,
        RealtimeProgress
    }

    protected struct MotionStep
    {
        public Vector3 LocalDirection;
        public float Distance;
        public float RotateAfterZ;

        public MotionStep(Vector3 localDirection, float distance, float rotateAfterZ = 0f)
        {
            LocalDirection = localDirection;
            Distance = distance;
            RotateAfterZ = rotateAfterZ;
        }
    }

    [Header("Data Mode")]
    public MotionDataMode dataMode = MotionDataMode.LocalSimulation;
    public bool isRunning = true;
    public float realtimeSpeedMultiplier = 1f;
    [Range(0f, 1f)] public float realtimeProgress01;

    [Header("Chain Style Motion")]
    public bool reverseDirection = false;
    public float turnBlendDistance = 0.08f;

    [Header("Group Control")]
    [SerializeField] private bool controlledByGroup = false;

    private readonly List<MotionStep> steps = new List<MotionStep>(8);
    private Vector3[] segmentStarts;
    private Vector3[] segmentDirections;
    private Quaternion[] segmentRotations;
    private Quaternion[] segmentEndRotations;
    private float[] segmentLengths;
    private float[] cumulativeDistances;
    private float totalPathLength;
    private float travelledDistance;
    private bool initialized;

    public bool IsInitialized => initialized;
    public bool IsControlledByGroup => controlledByGroup;
    public float TotalPathLength => totalPathLength;
    public float TravelledDistance => travelledDistance;
    public float CurrentProgress01 => totalPathLength > 0.0001f ? travelledDistance / totalPathLength : 0f;

    protected abstract float MoveSpeed { get; }
    protected abstract float RotationSpeed { get; }
    protected abstract void BuildMotionSteps(List<MotionStep> motionSteps);

    protected virtual void Start()
    {
        RebuildPath();
        ApplyDistanceToTransform(travelledDistance);
    }

    protected virtual void Update()
    {
        if (controlledByGroup || !initialized || !isRunning)
        {
            return;
        }

        if (dataMode == MotionDataMode.RealtimeProgress)
        {
            travelledDistance = Mathf.Repeat(realtimeProgress01, 1f) * totalPathLength;
            ApplyDistanceToTransform(travelledDistance);
            return;
        }

        float direction = reverseDirection ? -1f : 1f;
        travelledDistance = Mathf.Repeat(
            travelledDistance + direction * MoveSpeed * GetSpeedMultiplier() * Time.deltaTime,
            totalPathLength);

        ApplyDistanceToTransform(travelledDistance);
    }

    public void SetGroupControlled(bool value)
    {
        controlledByGroup = value;
    }

    public void InitializeForGroupControl()
    {
        if (!initialized)
        {
            RebuildPath();
        }

        ApplyDistanceToTransform(travelledDistance);
    }

    public void AddGroupDistance(float signedDistance)
    {
        if (!initialized)
        {
            RebuildPath();
        }

        if (!initialized)
        {
            return;
        }

        travelledDistance = Mathf.Repeat(travelledDistance + signedDistance, totalPathLength);
        ApplyDistanceToTransform(travelledDistance);
    }

    public void ApplyGroupProgress01(float progress01)
    {
        if (!initialized)
        {
            RebuildPath();
        }

        if (!initialized)
        {
            return;
        }

        travelledDistance = Mathf.Repeat(progress01, 1f) * totalPathLength;
        ApplyDistanceToTransform(travelledDistance);
    }

    public void ApplyGroupSpeed(float speed)
    {
        SetLocalMoveSpeed(speed);
    }

    [ContextMenu("Rebuild Duodou Path")]
    public void RebuildPath()
    {
        steps.Clear();
        BuildMotionSteps(steps);

        int count = steps.Count;
        if (count == 0)
        {
            initialized = false;
            return;
        }

        segmentStarts = new Vector3[count];
        segmentDirections = new Vector3[count];
        segmentRotations = new Quaternion[count];
        segmentEndRotations = new Quaternion[count];
        segmentLengths = new float[count];
        cumulativeDistances = new float[count];

        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;
        totalPathLength = 0f;

        for (int i = 0; i < count; i++)
        {
            MotionStep step = steps[i];
            float distance = Mathf.Max(0f, step.Distance);
            Vector3 localDirection = step.LocalDirection.sqrMagnitude > 0.000001f
                ? step.LocalDirection.normalized
                : Vector3.up;

            segmentStarts[i] = currentPosition;
            segmentDirections[i] = currentRotation * localDirection;
            segmentRotations[i] = currentRotation;
            segmentEndRotations[i] = currentRotation * Quaternion.AngleAxis(step.RotateAfterZ, Vector3.forward);
            segmentLengths[i] = distance;
            cumulativeDistances[i] = totalPathLength;

            currentPosition += segmentDirections[i] * distance;
            currentRotation = segmentEndRotations[i];
            totalPathLength += distance;
        }

        travelledDistance = Mathf.Repeat(travelledDistance, Mathf.Max(totalPathLength, 0.0001f));
        initialized = totalPathLength > 0.0001f;
    }

    public void SetCloudRunning(bool running)
    {
        isRunning = running;
    }

    public void SetCloudSpeed(float speed)
    {
        SetLocalMoveSpeed(speed);
        dataMode = MotionDataMode.RealtimeSpeed;
    }

    public void SetCloudSpeedMultiplier(float multiplier)
    {
        realtimeSpeedMultiplier = Mathf.Max(0f, multiplier);
        dataMode = MotionDataMode.RealtimeSpeed;
    }

    public void SetCloudProgress01(float progress01)
    {
        realtimeProgress01 = Mathf.Repeat(progress01, 1f);
        dataMode = MotionDataMode.RealtimeProgress;
    }

    public void SetCloudData(bool running, float speed, float progress01 = -1f)
    {
        isRunning = running;
        if (speed >= 0f)
        {
            SetLocalMoveSpeed(speed);
        }

        if (progress01 >= 0f)
        {
            realtimeProgress01 = Mathf.Repeat(progress01, 1f);
            dataMode = MotionDataMode.RealtimeProgress;
        }
        else
        {
            dataMode = MotionDataMode.RealtimeSpeed;
        }
    }

    protected virtual void SetLocalMoveSpeed(float speed)
    {
    }

    private void ApplyDistanceToTransform(float distance)
    {
        if (!initialized)
        {
            return;
        }

        float pathDistance = Mathf.Repeat(distance, totalPathLength);
        int segmentIndex = FindSegmentIndex(pathDistance);
        float localDistance = pathDistance - cumulativeDistances[segmentIndex];
        float segmentLength = segmentLengths[segmentIndex];

        if (segmentLength <= 0.0001f)
        {
            transform.position = segmentStarts[segmentIndex];
            transform.rotation = segmentEndRotations[segmentIndex];
            return;
        }

        float t = Mathf.Clamp01(localDistance / segmentLength);
        transform.position = segmentStarts[segmentIndex] + segmentDirections[segmentIndex] * localDistance;
        transform.rotation = EvaluateSegmentRotation(segmentIndex, localDistance, segmentLength, t);
    }

    private int FindSegmentIndex(float pathDistance)
    {
        for (int i = segmentLengths.Length - 1; i >= 0; i--)
        {
            if (pathDistance >= cumulativeDistances[i])
            {
                return i;
            }
        }

        return 0;
    }

    private Quaternion EvaluateSegmentRotation(int segmentIndex, float localDistance, float segmentLength, float t)
    {
        Quaternion startRotation = segmentRotations[segmentIndex];
        Quaternion endRotation = segmentEndRotations[segmentIndex];

        if (Quaternion.Angle(startRotation, endRotation) <= 0.01f)
        {
            return startRotation;
        }

        float rotationAngle = Quaternion.Angle(startRotation, endRotation);
        float speedBasedBlend = RotationSpeed > 0.001f
            ? MoveSpeed * rotationAngle / RotationSpeed
            : turnBlendDistance;
        float blendDistance = Mathf.Clamp(Mathf.Max(turnBlendDistance, speedBasedBlend), 0.001f, segmentLength);
        float blendStart = Mathf.Max(0f, segmentLength - blendDistance);
        float rotateT = localDistance <= blendStart
            ? 0f
            : Mathf.InverseLerp(blendStart, segmentLength, localDistance);

        rotateT = rotateT * rotateT * (3f - 2f * rotateT);
        return Quaternion.Slerp(startRotation, endRotation, rotateT);
    }

    private float GetSpeedMultiplier()
    {
        return dataMode == MotionDataMode.RealtimeSpeed ? Mathf.Max(0f, realtimeSpeedMultiplier) : 1f;
    }
}
