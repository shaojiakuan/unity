using UnityEngine;
using System.Collections;  // 需要添加这个命名空间

public class FuelGeneration : MonoBehaviour
{
    public GameObject fuelPrefab;   // 燃料物体预制件（如油桶、液体等）
    public Transform container;     // 料斗容器

    public float spacing = 0.5f;    // 物体之间的间隔
    public float r_cylinder = 5.0f; // 圆柱部分的半径
    public float h_cylinder = 5.0f; // 圆柱部分的高度
    public float r_cone = 5.0f;     // 圆锥部分的底部半径
    public float h_cone = 5.0f;     // 圆锥部分的高度
    public float fillSpeed = 1.0f;  // 生成速度，决定燃料填充的速度

    private float currentHeight = 0.0f; // 当前已生成的高度

    void Start()
    {
        StartCoroutine(GenerateFuel());
    }

    // 生成燃料的协程
    IEnumerator GenerateFuel()
    {
        while (currentHeight < h_cylinder + h_cone)
        {
            GenerateFuelAtHeight(currentHeight);

            // 更新当前高度
            currentHeight += spacing;

            // 等待下一帧生成
            yield return new WaitForSeconds(fillSpeed);
        }
    }

    // 在指定高度生成燃料
    void GenerateFuelAtHeight(float height)
    {
        Vector3 containerCenter = container.position; // 料斗中心位置
        Vector3 objectSize = fuelPrefab.GetComponent<Collider>().bounds.size; // 获取燃料物体的尺寸（使用Collider来获取尺寸）

        // 确保物体生成在圆柱部分
        if (height < containerCenter.y + h_cylinder) // 在圆柱部分生成
        {
            float radiusAtHeight = r_cylinder;

            // 生成该层燃料
            int numObjectsInLayer = Mathf.FloorToInt(radiusAtHeight * 2 / (objectSize.x + spacing));

            for (int i = 0; i < numObjectsInLayer; i++)
            {
                for (int j = 0; j < numObjectsInLayer; j++)
                {
                    float angle = UnityEngine.Random.Range(0, Mathf.PI * 2); // 明确使用 UnityEngine.Random
                    float radius = UnityEngine.Random.Range(0, radiusAtHeight); // 明确使用 UnityEngine.Random

                    float posX = containerCenter.x + radius * Mathf.Cos(angle);
                    float posZ = containerCenter.z + radius * Mathf.Sin(angle);

                    // 生成物体
                    Instantiate(fuelPrefab, new Vector3(posX, height, posZ), Quaternion.identity, container);
                }
            }
        }
        else // 在圆锥部分生成
        {
            // 在圆锥部分，随着高度变化，半径逐渐缩小
            float radiusAtHeight = r_cone * (1 - (height - containerCenter.y - h_cylinder) / h_cone);

            int numObjectsInLayer = Mathf.FloorToInt(radiusAtHeight * 2 / (objectSize.x + spacing));

            for (int i = 0; i < numObjectsInLayer; i++)
            {
                for (int j = 0; j < numObjectsInLayer; j++)
                {
                    float angle = UnityEngine.Random.Range(0, Mathf.PI * 2); // 明确使用 UnityEngine.Random
                    float radius = UnityEngine.Random.Range(0, radiusAtHeight); // 明确使用 UnityEngine.Random

                    float posX = containerCenter.x + radius * Mathf.Cos(angle);
                    float posZ = containerCenter.z + radius * Mathf.Sin(angle);

                    // 生成物体
                    Instantiate(fuelPrefab, new Vector3(posX, height, posZ), Quaternion.identity, container);
                }
            }
        }
    }
}
