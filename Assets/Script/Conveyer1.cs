using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conveyer1 : MonoBehaviour
{
    public float Speed = 0.05f;
    public carrier carrier;
    public float x1 = 10.3f;
    public float x2 = 10.3f;
    public float y1 = 1.5f;
    public float z1 = 4.05f;
    public float z2 = 5.2f;
    public int buildNumbers = 100;
    public int maxMovingMaterials = 120;
    public int maxStackedMaterials = 200;
    public int maxMaterialsPerLayer = 100; // 每层的最大堆叠数量
    public float layerHeight = 5f; // 每层的高度
    public GameObject Material;

    private List<GameObject> stackedMaterials = new List<GameObject>(); // 记录堆叠的 Material
    private int currentLayer = 0; // 当前堆叠层数
    private int materialsInCurrentLayer = 0; // 当前层中的 Material 数量
    private int maxLayers; // 最大堆叠层数

    public float beishu;
    public ParticleSystem smoke1;
    private ParticleSystem.MainModule smoke1MainModule;
    public ParticleSystem smoke2;
    private ParticleSystem.MainModule smoke2MainModule;
    public ParticleSystem smoke3;
    private ParticleSystem.MainModule smoke3MainModule;
    public ParticleSystem smoke4;
    private ParticleSystem.MainModule smoke4MainModule;
    public ParticleSystem smoke5;
    private ParticleSystem.MainModule smoke5MainModule;
    public ParticleSystem smokeNOx1;
    private ParticleSystem.MainModule smokeNOx1MainModule;
    public ParticleSystem smokeNOx2;
    private ParticleSystem.MainModule smokeNOx2MainModule;
    public ParticleSystem smokeNOx3;
    private ParticleSystem.MainModule smokeNOx3MainModule;
    public ParticleSystem smokeNOx4;
    private ParticleSystem.MainModule smokeNOx4MainModule;
    public ParticleSystem smokeNOx5;
    private ParticleSystem.MainModule smokeNOx5MainModule;

    public ParticleSystem flameFirst1;
    public ParticleSystem flameFirst2;
    public ParticleSystem flameSecond1;
    public ParticleSystem flameSecond2;
    public ParticleSystem flameThird;
    public ParticleSystem flameLast;
    public ParticleSystem flameSmokeFirst1;
    public ParticleSystem flameSmokeFirst2;
    public ParticleSystem flameSmokeSecond1;
    public ParticleSystem flameSmokeSecond2;
    public ParticleSystem flameSmokeThird;
    public ParticleSystem flameSmokeLast;
    public ParticleSystem flameNOxSmokeFirst1;
    public ParticleSystem flameNOxSmokeFirst2;
    public ParticleSystem flameNOxSmokeSecond1;
    public ParticleSystem flameNOxSmokeSecond2;
    public ParticleSystem flameNOxSmokeThird;
    public ParticleSystem flameNOxSmokeLast;
    private ParticleSystem.MainModule flameFirst1MainModule;
    private ParticleSystem.MainModule flameFirst2MainModule;
    private ParticleSystem.MainModule flameSecond1MainModule;
    private ParticleSystem.MainModule flameSecond2MainModule;
    private ParticleSystem.MainModule flameThirdMainModule;
    private ParticleSystem.MainModule flameLastMainModule;
    private ParticleSystem.MainModule flameSmokeFirst1MainModule;
    private ParticleSystem.MainModule flameSmokeFirst2MainModule;
    private ParticleSystem.MainModule flameSmokeSecond1MainModule;
    private ParticleSystem.MainModule flameSmokeSecond2MainModule;
    private ParticleSystem.MainModule flameSmokeThirdMainModule;
    private ParticleSystem.MainModule flameSmokeLastMainModule;
    private ParticleSystem.MainModule flameNOxSmokeFirst1MainModule;
    private ParticleSystem.MainModule flameNOxSmokeFirst2MainModule;
    private ParticleSystem.MainModule flameNOxSmokeSecond1MainModule;
    private ParticleSystem.MainModule flameNOxSmokeSecond2MainModule;
    private ParticleSystem.MainModule flameNOxSmokeThirdMainModule;
    private ParticleSystem.MainModule flameNOxSmokeLastMainModule;

    private bool beishuCalculated = false; // 标记变量，确保 beishu 计算只执行一次
    private Coroutine spawnCoroutine;
    private SimpleGameObjectPool materialPool;
    private readonly List<GameObject> movingMaterials = new List<GameObject>(128);

    // Start is called before the first frame update
    void Start()
    {
        maxLayers = Mathf.FloorToInt(0.4f / layerHeight); // 计算最大堆叠层数
        if (Material != null)
        {
            materialPool = new SimpleGameObjectPool(Material, transform);
        }
        start();
        smoke1MainModule = smoke1.main;
        smoke2MainModule = smoke2.main;
        smoke3MainModule = smoke3.main;
        smoke4MainModule = smoke4.main;
        smoke5MainModule = smoke5.main;
        smokeNOx1MainModule = smokeNOx1.main;
        smokeNOx2MainModule = smokeNOx2.main;
        smokeNOx3MainModule = smokeNOx3.main;
        smokeNOx4MainModule = smokeNOx4.main;
        smokeNOx5MainModule = smokeNOx5.main;

        flameFirst1MainModule = flameFirst1.main;
        flameFirst2MainModule = flameFirst2.main;
        flameSecond1MainModule = flameSecond1.main;
        flameSecond2MainModule = flameSecond2.main;
        flameThirdMainModule = flameThird.main;
        flameLastMainModule = flameLast.main;
        flameSmokeFirst1MainModule = flameSmokeFirst1.main;
        flameSmokeFirst2MainModule = flameSmokeFirst2.main;
        flameSmokeSecond1MainModule = flameSmokeSecond1.main;
        flameSmokeSecond2MainModule = flameSmokeSecond2.main;
        flameSmokeThirdMainModule = flameSmokeThird.main;
        flameSmokeLastMainModule = flameSmokeLast.main;
        flameNOxSmokeFirst1MainModule = flameNOxSmokeFirst1.main;
        flameNOxSmokeFirst2MainModule = flameNOxSmokeFirst2.main;
        flameNOxSmokeSecond1MainModule = flameNOxSmokeSecond1.main;
        flameNOxSmokeSecond2MainModule = flameNOxSmokeSecond2.main;
        flameNOxSmokeThirdMainModule = flameNOxSmokeThird.main;
        flameNOxSmokeLastMainModule = flameNOxSmokeLast.main;

    }
    private void StartSpawner()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(enumerator());
        }
    }


    public void SetRunning(bool running)
    {
        if (running)
        {
            StartSpawner();
        }
    }

    // Update 每一帧调用一次
    void Update()
    {
        carrier.speed = Speed;

        if (!beishuCalculated)
        {
            beishu = Speed / 0.015f;
            //smoke1MainModule.startDelay = 1f / beishu;
            //smoke2MainModule.startDelay = 6f / beishu;
            //smoke3MainModule.startDelay = 12f / beishu;
            //smoke4MainModule.startDelay = 18f / beishu;
            //smoke5MainModule.startDelay = 24f / beishu;

            smoke1MainModule.startDelay = 1f / beishu;
            smoke2MainModule.startDelay = 6f / beishu;
            smoke3MainModule.startDelay = 12f / beishu;
            smoke4MainModule.startDelay = 18f / beishu;
            smoke5MainModule.startDelay = 24f / beishu;

            smokeNOx1MainModule.startDelay = 1f / beishu;
            smokeNOx2MainModule.startDelay = 6f / beishu;
            smokeNOx3MainModule.startDelay = 12f / beishu;
            smokeNOx4MainModule.startDelay = 18f / beishu;
            smokeNOx5MainModule.startDelay = 24f / beishu;

            flameFirst1MainModule.startDelay = 41f / beishu;
            flameFirst2MainModule.startDelay = 41f / beishu;
            flameSecond1MainModule.startDelay = 41f / beishu;
            flameSecond2MainModule.startDelay = 41f / beishu;
            flameThirdMainModule.startDelay = 41f / beishu;
            flameLastMainModule.startDelay = 41f / beishu;
            flameSmokeFirst1MainModule.startDelay = 41f / beishu;
            flameSmokeFirst2MainModule.startDelay = 41f / beishu;
            flameSmokeSecond1MainModule.startDelay = 41f / beishu;
            flameSmokeSecond2MainModule.startDelay = 41f / beishu;
            flameSmokeThirdMainModule.startDelay = 41f / beishu;
            flameSmokeLastMainModule.startDelay = 41f / beishu;
            flameNOxSmokeFirst1MainModule.startDelay = 41f / beishu;
            flameNOxSmokeFirst2MainModule.startDelay = 41f / beishu;
            flameNOxSmokeSecond1MainModule.startDelay = 41f / beishu;
            flameNOxSmokeSecond2MainModule.startDelay = 41f / beishu;
            flameNOxSmokeThirdMainModule.startDelay = 41f / beishu;
            flameNOxSmokeLastMainModule.startDelay = 41f / beishu;

            beishuCalculated = true; // 标记为已计算
        }

        // 使用 beishu 更新 smoke1MainModule.startDelay
        //smoke1MainModule.startDelay = beishu;

        for (int i = BurningMaterial.ActiveMaterials.Count - 1; i >= 0; i--)
        {
            BurningMaterial burningMaterial = BurningMaterial.ActiveMaterials[i];
            if (burningMaterial != null)
            {
                burningMaterial.Speed11 = Speed == 0 ? 0 : Speed * 3;
            }
        }

        // 当 Speed 不为 0 时，隐藏所有堆叠的 Material
        if (Speed != 0)
        {
            foreach (GameObject stackedMaterial in stackedMaterials)
            {
                if (materialPool != null) materialPool.Release(stackedMaterial); else stackedMaterial.SetActive(false);
            }
            stackedMaterials.Clear(); // 清空已堆叠列表
            currentLayer = 0; // 重置层数
            materialsInCurrentLayer = 0; // 重置当前层的计数
        }
    }

    IEnumerator enumerator()
    {
        while (true)
        {
            if (Speed > 0)
            {
                // 原始生成逻辑，当 Speed 大于 0 时正常生成材质
                CleanupInactiveObjects(movingMaterials);
                int movingCapacity = Mathf.Max(0, maxMovingMaterials - movingMaterials.Count);
                int spawnCount = Mathf.Min((int)(buildNumbers * 7 * Speed), movingCapacity);
                for (int i = 0; i < spawnCount; i++)
                {
                    float X = UnityEngine.Random.Range(x1, x2);
                    float Y = UnityEngine.Random.Range(y1, y1 + layerHeight);
                    float Z = UnityEngine.Random.Range(z1, z2);
                    float r = UnityEngine.Random.Range(0, 360);
                    GameObject newball = materialPool != null ? materialPool.Get(new Vector3(X, Y, Z), Quaternion.Euler((X + Y) * 200 * r, (Y + Z) * 300, Z * r)) : Instantiate(Material, new Vector3(X, Y, Z), Quaternion.Euler((X + Y) * 200 * r, (Y + Z) * 300, Z * r));
                    BurningMaterial burningMaterial = newball.GetComponent<BurningMaterial>();
                    if (burningMaterial != null)
                    {
                        burningMaterial.Speed11 = Speed * 3;
                    }
                    movingMaterials.Add(newball);
                }
            }
            else
            {
                // 当 Speed 为 0 时，逐层堆叠 Material，但堆叠高度不超过 0.4 个单位
                CleanupInactiveObjects(stackedMaterials);
                int stackedCapacity = Mathf.Max(0, maxStackedMaterials - stackedMaterials.Count);
                int spawnCount = Mathf.Min(buildNumbers, stackedCapacity);
                for (int i = 0; i < spawnCount; i++)
                {
                    if (materialsInCurrentLayer >= maxMaterialsPerLayer)
                    {
                        // 当前层已满，增加层数
                        currentLayer++;
                        materialsInCurrentLayer = 0; // 重置当前层的 Material 数量

                        // 如果超过最大层数，循环回到第一层
                        if (currentLayer >= maxLayers)
                        {
                            currentLayer = 0;
                        }
                    }

                    // 在 x 方向一个单位范围内生成 Material
                    float X = UnityEngine.Random.Range(x1 - 0.1f, x1 + 0.3f);
                    float Y = y1 + currentLayer * layerHeight; // 根据当前层数设置高度
                    float Z = UnityEngine.Random.Range(z1, z2);

                    GameObject newball = materialPool != null ? materialPool.Get(new Vector3(X, Y, Z), Quaternion.identity) : Instantiate(Material, new Vector3(X, Y, Z), Quaternion.identity);
                    BurningMaterial burningMaterial = newball.GetComponent<BurningMaterial>();
                    if (burningMaterial != null)
                    {
                        burningMaterial.Speed11 = 0;
                    } // 物体不移动
                    stackedMaterials.Add(newball); // 将生成的物体加入堆叠列表

                    materialsInCurrentLayer++; // 增加当前层的 Material 数量
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void CleanupInactiveObjects(List<GameObject> objects)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] == null || !objects[i].activeInHierarchy)
            {
                objects.RemoveAt(i);
            }
        }
    }

    // 从外部更新 Speed 值并同时更新 beishu 值
    public void setSpeed(float newSpeed)
    {
        Speed = newSpeed;
        //beishu = Speed / 0.015f;  // 将外部传来的 Speed 赋值给 beishu
        carrier.speed = Speed;
        beishuCalculated = true; // 更新 beishu 后设为 true

        for (int i = BurningMaterial.ActiveMaterials.Count - 1; i >= 0; i--)
        {
            BurningMaterial burningMaterial = BurningMaterial.ActiveMaterials[i];
            if (burningMaterial != null)
            {
                burningMaterial.Speed11 = newSpeed * 3;
            }
        }
    }

    public void start()
    {
        StartSpawner();
    }
}




