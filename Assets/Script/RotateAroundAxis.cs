using UnityEngine;
using System.Collections.Generic;

public class RotateAroundAxis : MonoBehaviour
{
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    [Header("��ת����")]
    public float rotationSpeed = 90f;

    [Header("ǰ������")]
    public float moveSpeed = 2f;

    [Header("�������ڣ����ӣ�")]
    public float lifeTime = 3f;

    [Header("��������")]
    public float spawnInterval = 1f;
    public bool isEmitter = true; // �Ƿ�Ϊ������

    [Header("��ת����")]
    public RotationDirection direction = RotationDirection.Clockwise;

    [Header("�¶�����")]
    [Range(0, 100)]
    public float temperature = 0f; // �¶�ֵ����Χ0-100
    public bool autoChangeTemp = true; // �Ƿ��Զ��仯�¶�
    public float tempChangeSpeed = 10f; // �¶ȱ仯�ٶȣ���λ/�룩

    [Header("�¶�ͬ��")]
    public bool syncTemperatureWithEmitter = false; // �Ƿ��뷢����ͬ���¶�
    public RotateAroundAxis temperatureSource; // �¶�Դ����������

    [Header("�¶Ȳ���")]
    public Material blueMaterial;    // 0-25��
    public Material greenMaterial;   // 25-50��
    public Material yellowMaterial;  // 50-75��
    public Material redMaterial;     // 75-100��

    // ��lifeTimer��Ϊprotected�Ա��������
    protected float lifeTimer = 0f;
    private float spawnTimer = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Renderer objectRenderer;
    private Material currentMaterial;

    // �������ɵ������б�
    private List<RotateAroundAxis> spawnedParticles = new List<RotateAroundAxis>();

    // �������������¶ȱ仯��¼
    private float lastEmittedTemperature = 0f;
    private SimpleGameObjectPool clonePool;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        // ��ȡ��Ⱦ�����
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogWarning("δ�ҵ�Renderer�������ȷ��������MeshRenderer��SkinnedMeshRenderer");
        }

        // ��ʼ������
        ApplyMaterialBasedOnTemperature();

        // ����Ƿ����������������б�
        if (isEmitter)
        {
            clonePool = new SimpleGameObjectPool(gameObject, transform.parent);
            spawnedParticles.Clear();
            lastEmittedTemperature = temperature;
        }
    }

    void Update()
    {
        RotateObject();
        MoveForwardX();

        // ����Ƿ��������������¶�ͬ��
        if (isEmitter && syncTemperatureWithEmitter)
        {
            SyncTemperatureToParticles();
        }

        // ������Ƿ����������¶�Դ��ͬ���¶�
        if (!isEmitter && temperatureSource != null)
        {
            SyncTemperatureFromSource();
        }

        // �����¶ȣ���������Զ��仯��û���¶�Դ��
        if (autoChangeTemp && !isEmitter && temperatureSource == null)
        {
            UpdateTemperature();
        }

        // ���²���
        ApplyMaterialBasedOnTemperature();

        if (!isEmitter)
        {
            LifeTimer();
        }

        if (isEmitter)
        {
            SpawnClone();
        }
    }

    void SyncTemperatureToParticles()
    {
        // �Ƴ������ٵ�����
        for (int i = spawnedParticles.Count - 1; i >= 0; i--)
        {
            if (spawnedParticles[i] == null)
            {
                spawnedParticles.RemoveAt(i);
            }
        }

        // ͬ���¶ȵ����д�������
        foreach (var particle in spawnedParticles)
        {
            if (particle != null)
            {
                particle.Temperature = temperature;
            }
        }
    }

    void SyncTemperatureFromSource()
    {
        if (temperatureSource != null)
        {
            Temperature = temperatureSource.Temperature;
        }
    }

    void RotateObject()
    {
        float speed = direction == RotationDirection.Clockwise ? -rotationSpeed : rotationSpeed;
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }

    void MoveForwardX()
    {
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime, Space.World);
    }

    void LifeTimer()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            // �ӷ������������б����Ƴ��Լ�
            if (temperatureSource != null && temperatureSource.spawnedParticles.Contains(this))
            {
                temperatureSource.spawnedParticles.Remove(this);
            }
            ReleaseOrDestroy();
        }
    }

    void SpawnClone()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            GameObject clone = clonePool != null ? clonePool.Get(startPosition, startRotation) : Instantiate(gameObject, startPosition, startRotation);
            RotateAroundAxis script = clone.GetComponent<RotateAroundAxis>();
            script.isEmitter = false;

            // �ؼ��޸ģ������Ƿ�ͬ���������Ӷ��Է�������ǰ�¶�Ϊ���
            // ʹ�ù�����������lifeTimer
            script.SetLifeTimer(0f);

            // �����¶�Դ
            if (syncTemperatureWithEmitter)
            {
                script.temperatureSource = this;
                script.syncTemperatureWithEmitter = false; // ���Ӳ���Ҫͬ������������
                script.autoChangeTemp = false; // ���Ӳ��Զ��仯�¶ȣ���Դ��ȡ
                script.Temperature = temperature; // ��ʼ����Ϊ���������¶�

                // ���ӵ������б�
                spawnedParticles.Add(script);
            }
            else
            {
                // �ؼ��޸ģ���ʹ������ͬ����Ҳʹ�÷�������ǰ�¶�
                script.Temperature = temperature; // ʹ�÷�������ǰ�¶�

                // �������Ҫ�Զ��仯�¶�
                script.autoChangeTemp = autoChangeTemp;
                script.tempChangeSpeed = tempChangeSpeed;

                // ȷ���������Լ����¶ȱ仯�߼�
                if (script.autoChangeTemp)
                {
                    script.temperatureSource = null; // ȷ�����Ӳ����ⲿԴ��ȡ�¶�
                }
            }

            // ʹ�ù����������ò���
            script.SetTemperatureMaterials(blueMaterial, greenMaterial, yellowMaterial, redMaterial);

            // ��¼���η�����¶�
            lastEmittedTemperature = temperature;

            // ����Ӧ�ò���
            script.ApplyMaterialBasedOnTemperature();
        }
    }

    void UpdateTemperature()
    {
        // ����ʱ�������¶�
        temperature += tempChangeSpeed * Time.deltaTime;

        // �����¶���0-100��Χ��
        temperature = Mathf.Clamp(temperature, 0f, 100f);
    }

    void ApplyMaterialBasedOnTemperature()
    {
        if (objectRenderer == null) return;

        Material newMaterial = GetMaterialForTemperature(temperature);

        // ������ʱ仯�˲Ÿ���
        if (newMaterial != null && newMaterial != currentMaterial)
        {
            objectRenderer.sharedMaterial = newMaterial;
            currentMaterial = newMaterial;
        }
    }

    Material GetMaterialForTemperature(float temp)
    {
        if (temp < 25f)
        {
            return blueMaterial;
        }
        else if (temp < 50f)
        {
            return greenMaterial;
        }
        else if (temp < 75f)
        {
            return yellowMaterial;
        }
        else
        {
            return redMaterial;
        }
    }

    // ���������������¶�
    public void SetTemperature(float newTemp)
    {
        temperature = Mathf.Clamp(newTemp, 0f, 100f);
        ApplyMaterialBasedOnTemperature();

        // ���������ͬ����Ҳͬ������������
        if (syncTemperatureWithEmitter)
        {
            SyncTemperatureToParticles();
        }
    }

    // ������������ȡ��ǰ�¶ȶ�Ӧ�Ĳ���
    public Material GetCurrentTemperatureMaterial()
    {
        return GetMaterialForTemperature(temperature);
    }

    // �������������������¶Ȳ���
    public void SetTemperatureMaterials(Material blue, Material green, Material yellow, Material red)
    {
        blueMaterial = blue;
        greenMaterial = green;
        yellowMaterial = yellow;
        redMaterial = red;
        ApplyMaterialBasedOnTemperature();
    }

    // ������������ȡ�����¶Ȳ���
    public void GetTemperatureMaterials(out Material blue, out Material green, out Material yellow, out Material red)
    {
        blue = blueMaterial;
        green = greenMaterial;
        yellow = yellowMaterial;
        red = redMaterial;
    }

    // �����������������Ƿ��ѷ���
    public bool AreMaterialsAssigned()
    {
        return blueMaterial != null &&
               greenMaterial != null &&
               yellowMaterial != null &&
               redMaterial != null;
    }

    // ��������������lifeTimer��ֵ
    public void SetLifeTimer(float value)
    {
        lifeTimer = value;
    }

    // ������������ȡlifeTimer��ֵ
    public float GetLifeTimer()
    {
        return lifeTimer;
    }

    // ���������������������ڼ�ʱ��
    public void ResetLifeTimer()
    {
        lifeTimer = 0f;
    }

    // �������ԣ���ȡ�������¶�
    public float Temperature
    {
        get { return temperature; }
        set
        {
            temperature = Mathf.Clamp(value, 0f, 100f);
            ApplyMaterialBasedOnTemperature();
        }
    }

    // �������ԣ���ȡ�����¶Ȳ���
    public Material BlueMaterial
    {
        get { return blueMaterial; }
        set { blueMaterial = value; }
    }

    public Material GreenMaterial
    {
        get { return greenMaterial; }
        set { greenMaterial = value; }
    }

    public Material YellowMaterial
    {
        get { return yellowMaterial; }
        set { yellowMaterial = value; }
    }

    public Material RedMaterial
    {
        get { return redMaterial; }
        set { redMaterial = value; }
    }

    // �����������ԣ���ȡ�ϴη�����¶�
    public float LastEmittedTemperature
    {
        get { return lastEmittedTemperature; }
    }

    // ����������������������ɵ�����
    public void ClearAllParticles()
    {
        foreach (var particle in spawnedParticles)
        {
            if (particle != null)
            {
                ReleaseParticle(particle.gameObject);
            }
        }
        spawnedParticles.Clear();
    }

    // �༭��ģʽ�µ�Ԥ��
    private void ReleaseOrDestroy()
    {
        PooledGameObject pooledObject = GetComponent<PooledGameObject>();
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ReleaseParticle(GameObject particleObject)
    {
        if (particleObject == null)
        {
            return;
        }

        PooledGameObject pooledObject = particleObject.GetComponent<PooledGameObject>();
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            Destroy(particleObject);
        }
    }
    void OnValidate()
    {
        if (!Application.isPlaying && objectRenderer != null)
        {
            // �������Ƿ��ѷ���
            if (AreMaterialsAssigned())
            {
                Material previewMaterial = GetMaterialForTemperature(temperature);
                if (previewMaterial != null)
                {
                    objectRenderer.sharedMaterial = previewMaterial;
                }
            }
        }

        // �ڱ༭���м�¼�¶ȱ仯
        if (isEmitter)
        {
            clonePool = new SimpleGameObjectPool(gameObject, transform.parent);
            lastEmittedTemperature = temperature;
        }
    }
}
