using UnityEngine;

public class DuodouGroupMotionController : MonoBehaviour
{
    [Header("Targets")]
    public bool autoCollectChildren = true;
    public bool includeInactiveChildren = true;
    public DuodouMotionBase[] buckets;

    [Header("Run")]
    public DuodouMotionBase.MotionDataMode dataMode = DuodouMotionBase.MotionDataMode.LocalSimulation;
    public bool isRunning = true;
    public bool reverseDirection = false;
    public float moveSpeed = 0.5f;
    public float realtimeSpeedMultiplier = 1f;
    [Range(0f, 1f)] public float realtimeProgress01;

    [Header("Options")]
    public bool applySpeedToChildren = true;
    public bool useChildReverseDirection = false;
    public bool keepInitialProgressOffset = true;

    private float[] initialProgressOffsets;

    private void Awake()
    {
        PrepareBuckets();
    }

    private void Update()
    {
        if (!isRunning || buckets == null || buckets.Length == 0)
        {
            return;
        }

        if (dataMode == DuodouMotionBase.MotionDataMode.RealtimeProgress)
        {
            ApplyProgressToBuckets();
            return;
        }

        float multiplier = dataMode == DuodouMotionBase.MotionDataMode.RealtimeSpeed
            ? Mathf.Max(0f, realtimeSpeedMultiplier)
            : 1f;
        float signedDelta = (reverseDirection ? -1f : 1f) * Mathf.Max(0f, moveSpeed) * multiplier * Time.deltaTime;

        for (int i = 0; i < buckets.Length; i++)
        {
            DuodouMotionBase bucket = buckets[i];
            if (bucket == null)
            {
                continue;
            }

            float childDelta = useChildReverseDirection && bucket.reverseDirection ? -signedDelta : signedDelta;
            bucket.AddGroupDistance(childDelta);
        }
    }

    [ContextMenu("Collect Child Buckets")]
    public void CollectChildBuckets()
    {
        buckets = GetComponentsInChildren<DuodouMotionBase>(includeInactiveChildren);
    }

    [ContextMenu("Prepare Buckets")]
    public void PrepareBuckets()
    {
        if (autoCollectChildren)
        {
            CollectChildBuckets();
        }

        if (buckets == null)
        {
            initialProgressOffsets = new float[0];
            return;
        }

        initialProgressOffsets = new float[buckets.Length];

        for (int i = 0; i < buckets.Length; i++)
        {
            DuodouMotionBase bucket = buckets[i];
            if (bucket == null)
            {
                continue;
            }

            bucket.SetGroupControlled(true);
            bucket.InitializeForGroupControl();
            initialProgressOffsets[i] = bucket.CurrentProgress01;

            if (applySpeedToChildren)
            {
                bucket.ApplyGroupSpeed(moveSpeed);
            }
        }
    }

    public void ApplyCloudJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        DuodouCloudDataBridge.CloudPayload payload = JsonUtility.FromJson<DuodouCloudDataBridge.CloudPayload>(json);
        ApplyCloudPayload(payload);
    }

    public void ApplyCloudPayload(DuodouCloudDataBridge.CloudPayload payload)
    {
        if (payload == null)
        {
            return;
        }

        isRunning = payload.running;

        if (payload.speed >= 0f)
        {
            moveSpeed = payload.speed;
            dataMode = DuodouMotionBase.MotionDataMode.RealtimeSpeed;

            if (applySpeedToChildren)
            {
                ApplySpeedToAllBuckets(moveSpeed);
            }
        }
        else
        {
            realtimeSpeedMultiplier = payload.speedMultiplier;
            dataMode = DuodouMotionBase.MotionDataMode.RealtimeSpeed;
        }

        if (payload.useProgress && payload.progress01 >= 0f)
        {
            realtimeProgress01 = Mathf.Repeat(payload.progress01, 1f);
            dataMode = DuodouMotionBase.MotionDataMode.RealtimeProgress;
            ApplyProgressToBuckets();
        }
    }

    public void ApplyCloudValues(bool running, float speed, float progress01 = -1f)
    {
        isRunning = running;

        if (speed >= 0f)
        {
            moveSpeed = speed;
            dataMode = DuodouMotionBase.MotionDataMode.RealtimeSpeed;

            if (applySpeedToChildren)
            {
                ApplySpeedToAllBuckets(moveSpeed);
            }
        }

        if (progress01 >= 0f)
        {
            realtimeProgress01 = Mathf.Repeat(progress01, 1f);
            dataMode = DuodouMotionBase.MotionDataMode.RealtimeProgress;
            ApplyProgressToBuckets();
        }
    }

    private void ApplyProgressToBuckets()
    {
        for (int i = 0; i < buckets.Length; i++)
        {
            DuodouMotionBase bucket = buckets[i];
            if (bucket == null)
            {
                continue;
            }

            float offset = keepInitialProgressOffset && initialProgressOffsets != null && i < initialProgressOffsets.Length
                ? initialProgressOffsets[i]
                : 0f;
            float progress = reverseDirection
                ? offset - realtimeProgress01
                : offset + realtimeProgress01;

            bucket.ApplyGroupProgress01(progress);
        }
    }

    private void ApplySpeedToAllBuckets(float speed)
    {
        if (buckets == null)
        {
            return;
        }

        for (int i = 0; i < buckets.Length; i++)
        {
            if (buckets[i] != null)
            {
                buckets[i].ApplyGroupSpeed(speed);
            }
        }
    }
}
