using UnityEngine;
using System.Collections;

public class AutoMovingObject : MonoBehaviour
{
    public enum MoveDirection
    {
        Forward, Back, Up, Down, Right, Left
    }

    public MoveDirection direction = MoveDirection.Forward;
    public float moveDistance = 5f;
    public float moveSpeed = 3f;
    public float waitTime = 0.5f;

    private Vector3 startPos;
    private Vector3 endPos;

    void Start()
    {
        startPos = transform.position;
        endPos = startPos + GetDirection() * moveDistance;
        StartCoroutine(MoveLoop());
    }

    Vector3 GetDirection()
    {
        switch (direction)
        {
            case MoveDirection.Forward: return Vector3.forward;
            case MoveDirection.Back: return Vector3.back;
            case MoveDirection.Up: return Vector3.up;
            case MoveDirection.Down: return Vector3.down;
            case MoveDirection.Right: return Vector3.right;
            case MoveDirection.Left: return Vector3.left;
            default: return Vector3.forward;
        }
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            // 移动到终点
            while (Vector3.Distance(transform.position, endPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    endPos,
                    moveSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.position = endPos;
            yield return new WaitForSeconds(waitTime);

            // 回到起点
            transform.position = startPos;
            yield return new WaitForSeconds(waitTime);
        }
    }
}