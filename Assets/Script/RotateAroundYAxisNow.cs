using UnityEngine;

public class RotateAroundYAxisNow : MonoBehaviour
{
    [Header("��ת����")]
    public float rotationSpeed = 90f;

    [Header("ǰ������")]
    public float moveSpeed = 2f;

    [Header("�������ڣ����ӣ�")]
    public float lifeTime = 3f;

    [Header("��������")]
    public float spawnInterval = 1f;
    public bool isEmitter = true; // ֻ��ԴͷΪ true

    [Header("��ת����")]
    public RotationDirection direction = RotationDirection.Clockwise;

    [Header("�¶�����")]
    [Range(0, 100)]
    public float temperature = 70f; // �¶�ֵ����Χ0-100
    public bool autoChangeTemp = true; // �Ƿ��Զ��仯�¶�
    public float tempChangeSpeed = 10f; // �¶ȱ仯�ٶȣ���λ/�룩

    [Header("�¶Ȳ���")]
    public Material blueMaterial;    // 0-25��
    public Material greenMaterial;   // 25-50��
    public Material yellowMaterial;  // 50-75��
    public Material redMaterial;     // 75-100��

    // ��lifeTimer��Ϊprotected�������ӹ���������
    protected float lifeTimer;
    private float spawnTimer;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Renderer objectRenderer;
    private Material currentMaterial;

    // ����������׷�ٷ��������¶ȱ仯
    private float lastEmittedTemperature = 0f;
    private SimpleGameObjectPool clonePool;

    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

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

        // ��¼��ʼ�¶���Ϊ�����¶�
        if (isEmitter)
        {
            clonePool = new SimpleGameObjectPool(gameObject, transform.parent);
            lastEmittedTemperature = temperature;
        }
    }

    void Update()
    {
        // ���ж���Ҫ��
        RotateObject();
        MoveForwardZ();

        // �����¶ȣ���������Զ��仯��
        if (autoChangeTemp && !isEmitter)
        {
            UpdateTemperature();
        }

        // ���²���
        ApplyMaterialBasedOnTemperature();

        // ֻ�����Ӳ����٣�������������
        if (!isEmitter)
        {
            LifeTimer();
        }

        // ֻ�з�������������
        if (isEmitter)
        {
            SpawnClone();
        }
    }

    void RotateObject()
    {
        float speed = direction == RotationDirection.Clockwise ? -rotationSpeed : rotationSpeed;
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }

    void MoveForwardZ()
    {
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.World);
    }

    void LifeTimer()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
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

            RotateAroundYAxisNow script = clone.GetComponent<RotateAroundYAxisNow>();
            script.isEmitter = false; // �����ɵ�������
            script.SetLifeTimer(0f);  // ʹ�ù�����������lifeTimer

            // �ؼ��޸ģ�ʹ�÷�������ǰ�¶ȣ�������0
            script.temperature = temperature; // ʹ�÷�������ǰ�¶�

            // ���Ʋ������õ���¡��
            script.blueMaterial = blueMaterial;
            script.greenMaterial = greenMaterial;
            script.yellowMaterial = yellowMaterial;
            script.redMaterial = redMaterial;

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

    // �ⲿ���������¶ȵķ���
    public void SetTemperature(float newTemp)
    {
        temperature = Mathf.Clamp(newTemp, 0f, 100f);
        ApplyMaterialBasedOnTemperature();
    }

    // ��ȡ��ǰ�¶ȶ�Ӧ�Ĳ���
    public Material GetCurrentTemperatureMaterial()
    {
        return GetMaterialForTemperature(temperature);
    }

    // ���������¶Ȳ��ʵķ���
    public void SetTemperatureMaterials(Material blue, Material green, Material yellow, Material red)
    {
        blueMaterial = blue;
        greenMaterial = green;
        yellowMaterial = yellow;
        redMaterial = red;
        ApplyMaterialBasedOnTemperature();
    }

    // �������Ƿ��ѷ���
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

    // ������������ȡ�������ĵ�ǰ�¶ȣ�����Ԥ����
    public float GetEmitterTemperature()
    {
        return temperature;
    }

    // ������������ȡ�ϴη���ʱ���¶�
    public float GetLastEmittedTemperature()
    {
        return lastEmittedTemperature;
    }

    // �ڱ༭��ģʽ�µ�Ԥ��
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
