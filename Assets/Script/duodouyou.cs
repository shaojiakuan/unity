using System.Collections.Generic;
using UnityEngine;

public class duodouyou : DuodouMotionBase
{
    [Header("Speed")]
    public float moveSpeed = 0.5f;
    public float rotationSpeed = 180f;

    [Header("Distances")]
    public float y1Distance = 6.0f;
    public float xDistance = 0.55f;
    public float y2Distance = 6.6f;
    public float y3Distance = 0.6f;

    protected override float MoveSpeed => moveSpeed;
    protected override float RotationSpeed => rotationSpeed;

    protected override void SetLocalMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    protected override void BuildMotionSteps(List<MotionStep> motionSteps)
    {
        motionSteps.Add(new MotionStep(Vector3.up, y1Distance));
        motionSteps.Add(new MotionStep(Vector3.right, xDistance, 180f));
        motionSteps.Add(new MotionStep(Vector3.up, y2Distance, 180f));
        motionSteps.Add(new MotionStep(-Vector3.right, xDistance));
        motionSteps.Add(new MotionStep(Vector3.up, y3Distance));
    }
}
