using UnityEngine;

public class FlowParticle : MonoBehaviour
{
    [Header("旋转设置")]
    public float rotationSpeed = 90f;

    [Header("前进设置")]
    public float moveSpeed = 2f;

    [Header("生命周期")]
    public float lifeTime = 3f;

    [Header("旋转方向")]
    public bool clockwise = true;

    private float timer;

    void Update()
    {
        // 旋转
        float speed = clockwise ? -rotationSpeed : rotationSpeed;
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);

        // 沿世界Z轴前进
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.World);

        // 生命周期
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}