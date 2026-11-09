using System.Collections.Generic;
using UnityEngine;

public class duodouyouxia : DuodouMotionBase
{
    [Header("Speed")]
    public float moveSpeed = 0.5f;
    public float rotationSpeed = 180f;

    [Header("Distances")]
    public float yDistance = 6.6f;
    public float xDistance = 0.55f;

    protected override float MoveSpeed => moveSpeed;
    protected override float RotationSpeed => rotationSpeed;

    protected override void SetLocalMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    protected override void BuildMotionSteps(List<MotionStep> motionSteps)
    {
        motionSteps.Add(new MotionStep(Vector3.up, yDistance));
        motionSteps.Add(new MotionStep(Vector3.right, xDistance, 180f));
        motionSteps.Add(new MotionStep(Vector3.up, yDistance, 180f));
        motionSteps.Add(new MotionStep(-Vector3.right, xDistance));
    }
}
