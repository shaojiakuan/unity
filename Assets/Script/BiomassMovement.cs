using UnityEngine;

public class BiomassMovement : MonoBehaviour
{
    // 生物质燃料的移动速度
    public float moveSpeed = 5f;

    void Update()
    {
        // 生物质燃料对象向后移动
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
    }
}
