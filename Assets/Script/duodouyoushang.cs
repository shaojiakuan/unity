using System.Collections.Generic;
using UnityEngine;

public class duodouyoushang : DuodouMotionBase
{
    [Header("Speed")]
    public float speed = 0.5f;

    [Header("Distances")]
    public float backDistance = 0.55f;
    public float downDistance = 6.6f;
    public float forwardDistance = 0.55f;
    public float upDistance = 6.6f;

    protected override float MoveSpeed => speed;
    protected override float RotationSpeed => 360f;

    protected override void SetLocalMoveSpeed(float value)
    {
        speed = value;
    }

    protected override void BuildMotionSteps(List<MotionStep> motionSteps)
    {
        motionSteps.Add(new MotionStep(Vector3.back, backDistance));
        motionSteps.Add(new MotionStep(Vector3.down, downDistance));
        motionSteps.Add(new MotionStep(Vector3.forward, forwardDistance));
        motionSteps.Add(new MotionStep(Vector3.up, upDistance));
    }
}
