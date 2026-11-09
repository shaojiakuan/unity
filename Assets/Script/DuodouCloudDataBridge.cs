using System;
using UnityEngine;

public class DuodouCloudDataBridge : MonoBehaviour
{
    [Serializable]
    public class CloudPayload
    {
        public bool running = true;
        public float speed = -1f;
        public float speedMultiplier = 1f;
        public float progress01 = -1f;
        public bool useProgress = false;
    }

    [Header("Targets")]
    public DuodouGroupMotionController[] groupTargets;
    public DuodouMotionBase[] duodouTargets;

    [Header("Debug Input")]
    public bool logIncomingData = false;

    public void ApplyCloudJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        CloudPayload payload = JsonUtility.FromJson<CloudPayload>(json);
        if (payload == null)
        {
            return;
        }

        ApplyCloudPayload(payload);
    }

    public void ApplyCloudPayload(CloudPayload payload)
    {
        if (payload == null)
        {
            return;
        }

        if (logIncomingData)
        {
            Debug.Log($"Duodou cloud data: running={payload.running}, speed={payload.speed}, speedMultiplier={payload.speedMultiplier}, progress01={payload.progress01}, useProgress={payload.useProgress}", this);
        }

        if (groupTargets != null)
        {
            for (int i = 0; i < groupTargets.Length; i++)
            {
                DuodouGroupMotionController group = groupTargets[i];
                if (group != null)
                {
                    group.ApplyCloudPayload(payload);
                }
            }
        }

        if (duodouTargets == null)
        {
            return;
        }

        for (int i = 0; i < duodouTargets.Length; i++)
        {
            DuodouMotionBase target = duodouTargets[i];
            if (target == null)
            {
                continue;
            }

            target.SetCloudRunning(payload.running);

            if (payload.speed >= 0f)
            {
                target.SetCloudSpeed(payload.speed);
            }
            else
            {
                target.SetCloudSpeedMultiplier(payload.speedMultiplier);
            }

            if (payload.useProgress && payload.progress01 >= 0f)
            {
                target.SetCloudProgress01(payload.progress01);
            }
        }
    }

    public void ApplyCloudValues(bool running, float speed, float progress01 = -1f)
    {
        if (groupTargets != null)
        {
            for (int i = 0; i < groupTargets.Length; i++)
            {
                DuodouGroupMotionController group = groupTargets[i];
                if (group != null)
                {
                    group.ApplyCloudValues(running, speed, progress01);
                }
            }
        }

        if (duodouTargets == null)
        {
            return;
        }

        for (int i = 0; i < duodouTargets.Length; i++)
        {
            DuodouMotionBase target = duodouTargets[i];
            if (target != null)
            {
                target.SetCloudData(running, speed, progress01);
            }
        }
    }
}
