using System.Collections.Generic;
using UnityEngine;

public class duodouzuo : DuodouMotionBase
{
    [Header("Speed")]
    public float moveSpeed = 0.5f;
    public float rotationSpeed = 500f;

    [Header("Distances")]
    public float distanceY1 = 6.2f;
    public float distanceX1 = 0.55f;
    public float distanceY2 = 6.6f;
    public float distanceX2 = 0.55f;
    public float distanceY3 = 0.4f;

    protected override float MoveSpeed => moveSpeed;
    protected override float RotationSpeed => rotationSpeed;

    protected override void SetLocalMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    protected override void BuildMotionSteps(List<MotionStep> motionSteps)
    {
        motionSteps.Add(new MotionStep(Vector3.up, distanceY1, -180f));
        motionSteps.Add(new MotionStep(Vector3.right, distanceX1));
        motionSteps.Add(new MotionStep(Vector3.up, distanceY2));
        motionSteps.Add(new MotionStep(Vector3.left, distanceX2, -180f));
        motionSteps.Add(new MotionStep(Vector3.up, distanceY3));
    }
}
