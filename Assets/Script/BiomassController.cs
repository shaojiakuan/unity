using UnityEngine;

public class BiomassController : MonoBehaviour
{
    // 生物质燃料的预制件（Prefab）
    public GameObject biomassPrefab;

    // 生物质燃料生成的间隔时间
    public float spawnInterval = 2f;

    // 生物质燃料的移动速度
    public float moveSpeed = 5f;

    // 初始化计时器
    private float timer = 0f;

    void Update()
    {
        // 更新计时器
        timer += Time.deltaTime;

        // 检查是否到了生成生物质燃料的时间
        if (timer >= spawnInterval)
        {
            // 生成生物质燃料对象
            SpawnBiomass();

            // 重置计时器
            timer = 0f;
        }
    }

    void SpawnBiomass()
    {
        // 在当前物体的位置生成一个生物质燃料对象
        GameObject biomass = Instantiate(biomassPrefab, transform.position, Quaternion.identity);

        // 将其添加一个向后运动的组件
        biomass.AddComponent<BiomassMovement>().moveSpeed = moveSpeed;
    }
}
