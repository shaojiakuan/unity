using UnityEngine;

public class FlowEmitter : MonoBehaviour
{
    [Header("粒子预制体")]
    public FlowParticle particlePrefab;

    [Header("生成间隔")]
    public float spawnInterval = 1f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnParticle();
        }
    }

    void SpawnParticle()
    {
        FlowParticle p = Instantiate(
            particlePrefab,
            transform.position,
            transform.rotation
        );

        // 不需要任何额外处理，粒子会自动运动 & 自动销毁
    }
}
