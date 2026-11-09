using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conveyer : MonoBehaviour
{
    public float Speed = 0.05f;
    public carrier carrier;
    public float x1;
    public float x2;
    public float y1;
    public float z1;
    public float z2;
    public float waitTime = 3f;
    public int buildNumbers = 10;
    public int maxMovingMaterials = 120;
    public int maxStackedMaterials = 200;
    public int maxMaterialsPerLayer = 100; // 每层的最大堆叠数量
    public float layerHeight = 0.1f; // 每层的高度
    public GameObject Material;

    private List<GameObject> stackedMaterials = new List<GameObject>(); // 记录堆叠的 Material
    private int currentLayer = 0; // 当前堆叠层数
    private int materialsInCurrentLayer = 0; // 当前层中的 Material 数量
    private int maxLayers; // 最大堆叠层数
    private bool hasStarted = false; // 标记是否已过初始延迟

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

    void Start()
    {
        maxLayers = Mathf.FloorToInt(0.4f / layerHeight); // 计算最大堆叠层数
        if (Material != null)
        {
            materialPool = new SimpleGameObjectPool(Material, transform);
        }

        // Initialize all particle system modules
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

        // Start the delayed initialization
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(waitTime); // Wait for 3 seconds before starting
        hasStarted = true;
        StartSpawner(); // Start the fuel generation
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
        if (running && hasStarted) // Only respond if we've passed the initial delay
        {
            StartSpawner();
        }
    }

    void Update()
    {
        carrier.speed = Speed;

        if (!beishuCalculated)
        {
            beishu = Speed / 0.014f;

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
            flameThirdMainModule.startDelay = 41f / beishu + 2f;
            flameLastMainModule.startDelay = 41f / beishu + 15f;
            flameSmokeFirst1MainModule.startDelay = 41f / beishu;
            flameSmokeFirst2MainModule.startDelay = 41f / beishu;
            flameSmokeSecond1MainModule.startDelay = 41f / beishu;
            flameSmokeSecond2MainModule.startDelay = 41f / beishu;
            flameSmokeThirdMainModule.startDelay = 41f / beishu + 2f;
            flameSmokeLastMainModule.startDelay = 41f / beishu + 15f;
            flameNOxSmokeFirst1MainModule.startDelay = 41f / beishu;
            flameNOxSmokeFirst2MainModule.startDelay = 41f / beishu;
            flameNOxSmokeSecond1MainModule.startDelay = 41f / beishu;
            flameNOxSmokeSecond2MainModule.startDelay = 41f / beishu;
            flameNOxSmokeThirdMainModule.startDelay = 41f / beishu + 2f;
            flameNOxSmokeLastMainModule.startDelay = 41f / beishu + 15f;

            beishuCalculated = true;
        }

        for (int i = BurningMaterial.ActiveMaterials.Count - 1; i >= 0; i--)
        {
            BurningMaterial burningMaterial = BurningMaterial.ActiveMaterials[i];
            if (burningMaterial != null)
            {
                burningMaterial.Speed11 = Speed == 0 ? 0 : Speed * 3;
            }
        }

        if (Speed != 0)
        {
            foreach (GameObject stackedMaterial in stackedMaterials)
            {
                if (materialPool != null) materialPool.Release(stackedMaterial); else stackedMaterial.SetActive(false);
            }
            stackedMaterials.Clear();
            currentLayer = 0;
            materialsInCurrentLayer = 0;
        }
    }

    IEnumerator enumerator()
    {
        while (true)
        {
            if (!hasStarted) // Don't generate if we haven't passed the initial delay
            {
                yield return null;
                continue;
            }

            if (Speed > 0)
            {
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
                CleanupInactiveObjects(stackedMaterials);
                int stackedCapacity = Mathf.Max(0, maxStackedMaterials - stackedMaterials.Count);
                int spawnCount = Mathf.Min(buildNumbers, stackedCapacity);
                for (int i = 0; i < spawnCount; i++)
                {
                    if (materialsInCurrentLayer >= maxMaterialsPerLayer)
                    {
                        currentLayer++;
                        materialsInCurrentLayer = 0;

                        if (currentLayer >= maxLayers)
                        {
                            currentLayer = 0;
                        }
                    }

                    float X = UnityEngine.Random.Range(x1 - 0.1f, x1 + 0.3f);
                    float Y = y1 + currentLayer * layerHeight;
                    float Z = UnityEngine.Random.Range(z1, z2);

                    GameObject newball = materialPool != null ? materialPool.Get(new Vector3(X, Y, Z), Quaternion.identity) : Instantiate(Material, new Vector3(X, Y, Z), Quaternion.identity);
                    BurningMaterial burningMaterial = newball.GetComponent<BurningMaterial>();
                    if (burningMaterial != null)
                    {
                        burningMaterial.Speed11 = 0;
                    }
                    stackedMaterials.Add(newball);

                    materialsInCurrentLayer++;
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

    public void setSpeed(float newSpeed)
    {
        Speed = newSpeed;
        carrier.speed = Speed;
        beishuCalculated = true;

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
        if (hasStarted) // Only start if we've passed the initial delay
        {
            StartSpawner();
        }
    }
}



