using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DeviceDataSystem
{
    // Ƕ�������ռ��ڵ�DeviceValue1�࣬�����ͻ
    [System.Serializable]
    public class DeviceValueData
    {
        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("updateTime")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("unitName")]
        public string UnitName { get; set; }

        [JsonProperty("deviceId")]
        public int? DeviceId { get; set; }

        [JsonProperty("highLimit")]
        public decimal? HighLimit { get; set; }

        [JsonProperty("lowLimit")]
        public decimal? LowLimit { get; set; }

        [JsonProperty("alarmTag")]
        public string AlarmTag { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }
    }

    public static class DeviceDataFetcher
    {
        public static async Task<DeviceValueData[]> GetDeviceValuesAsync(int deviceId)
        {
            try
            {
                string url = $"https://yunchunremote.cn:5656/deviceValue/selectValue/{deviceId}";
                Debug.Log($"���������豸����: {url}");

                using var request = UnityWebRequest.Get(url);
                request.timeout = 10;

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"��������ʧ��: {request.error}");
                    throw new Exception($"��������ʧ��: {request.error}");
                }

                var json = request.downloadHandler.text;
                var serializer = JsonSerializer.Create();
                using var stringReader = new StringReader(json);
                using var jsonReader = new JsonTextReader(stringReader);
                return serializer.Deserialize<DeviceValueData[]>(jsonReader);
            }
            catch (Exception ex)
            {
                Debug.LogError($"��ȡ�豸����ʱ�쳣: {ex.Message}");
                return Array.Empty<DeviceValueData>();
            }
        }
    }
}

// ����������
public class NetworkControlledRotateAxis : MonoBehaviour
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
    public bool isEmitter = true;

    [Header("��ת����")]
    public RotationDirection direction = RotationDirection.Clockwise;

    [Header("�����¶�����")]
    [Tooltip("�Ƿ�������ȡ�¶�")]
    public bool useNetworkTemperature = true;
    public int deviceId = 88;
    public int dataId = 17;
    public float networkUpdateInterval = 2f;

    [Header("�����¶�����")]
    [Range(0, 100)]
    public float temperature = 0f;
    public bool autoChangeTemp = false;
    public float tempChangeSpeed = 10f;

    [Header("�¶�ͬ��")]
    public bool syncTemperatureWithEmitter = false;
    public NetworkControlledRotateAxis temperatureSource;

    [Header("�¶Ȳ���")]
    public Material blueMaterial;
    public Material greenMaterial;
    public Material yellowMaterial;
    public Material redMaterial;

    protected float lifeTimer = 0f;
    private float spawnTimer = 0f;
    private float networkUpdateTimer = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Renderer objectRenderer;
    private Material currentMaterial;
    private List<NetworkControlledRotateAxis> spawnedParticles = new List<NetworkControlledRotateAxis>();
    private float lastEmittedTemperature = 0f;
    private SimpleGameObjectPool clonePool;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogWarning("δ�ҵ�Renderer�������ȷ��������MeshRenderer��SkinnedMeshRenderer");
        }

        ApplyMaterialBasedOnTemperature();

        if (isEmitter)
        {
            clonePool = new SimpleGameObjectPool(gameObject, transform.parent);
            spawnedParticles.Clear();
            lastEmittedTemperature = temperature;

            // ����Ƿ�������ʹ�������¶ȣ���ʼ�������
            if (useNetworkTemperature)
            {
                UpdateTemperatureFromNetworkAsync();
            }
        }
    }

    async void Update()
    {
        RotateObject();
        MoveForwardX();

        // �����¶ȸ����߼�
        if (isEmitter && useNetworkTemperature)
        {
            networkUpdateTimer += Time.deltaTime;
            if (networkUpdateTimer >= networkUpdateInterval)
            {
                networkUpdateTimer = 0f;
                await UpdateTemperatureFromNetworkAsync();
            }
        }

        // �¶�ͬ���߼�
        if (isEmitter && syncTemperatureWithEmitter)
        {
            SyncTemperatureToParticles();
        }

        if (!isEmitter && temperatureSource != null)
        {
            SyncTemperatureFromSource();
        }

        // �����¶ȱ仯�߼�
        if (autoChangeTemp && !isEmitter && temperatureSource == null)
        {
            UpdateLocalTemperature();
        }

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

    // �������ȡ�¶�
    private async Task UpdateTemperatureFromNetworkAsync()
    {
        try
        {
            var values = await DeviceDataSystem.DeviceDataFetcher.GetDeviceValuesAsync(deviceId);

            if (values == null || values.Length == 0)
            {
                Debug.LogWarning($"û���ҵ��豸ID {deviceId} ������");
                return;
            }

            DeviceDataSystem.DeviceValueData targetValue = null;
            foreach (var value in values)
            {
                if (value.Id == dataId)
                {
                    targetValue = value;
                    break;
                }
            }

            if (targetValue != null)
            {
                // ��decimalת��Ϊfloat�������¶�
                float newTemp = (float)targetValue.Value;
                Temperature = newTemp;

                Debug.Log($"����������¶�: {newTemp}��C (�����豸{deviceId}, ����{dataId})");
            }
            else
            {
                Debug.LogWarning($"û���ҵ�����ID {dataId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"�����¶ȸ���ʧ��: {ex.Message}");
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
            NetworkControlledRotateAxis script = clone.GetComponent<NetworkControlledRotateAxis>();
            script.isEmitter = false;
            script.SetLifeTimer(0f);

            // ������������
            script.useNetworkTemperature = useNetworkTemperature;
            script.deviceId = deviceId;
            script.dataId = dataId;
            script.networkUpdateInterval = networkUpdateInterval;

            // �����¶�Դ
            if (syncTemperatureWithEmitter)
            {
                script.temperatureSource = this;
                script.syncTemperatureWithEmitter = false;
                script.autoChangeTemp = false;
                script.Temperature = temperature;
                spawnedParticles.Add(script);
            }
            else
            {
                // �������Է�������ǰ�¶�Ϊ���
                script.Temperature = temperature;
                script.autoChangeTemp = autoChangeTemp;
                script.tempChangeSpeed = tempChangeSpeed;

                if (script.autoChangeTemp)
                {
                    script.temperatureSource = null;
                }
            }

            // ���Ʋ���
            script.SetTemperatureMaterials(blueMaterial, greenMaterial, yellowMaterial, redMaterial);
            lastEmittedTemperature = temperature;
            script.ApplyMaterialBasedOnTemperature();
        }
    }

    void UpdateLocalTemperature()
    {
        temperature += tempChangeSpeed * Time.deltaTime;
        temperature = Mathf.Clamp(temperature, 0f, 100f);
    }

    void ApplyMaterialBasedOnTemperature()
    {
        if (objectRenderer == null) return;

        Material newMaterial = GetMaterialForTemperature(temperature);

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

    // ��������������
    public void SetTemperature(float newTemp)
    {
        temperature = Mathf.Clamp(newTemp, 0f, 100f);
        ApplyMaterialBasedOnTemperature();

        if (syncTemperatureWithEmitter)
        {
            SyncTemperatureToParticles();
        }
    }

    public void SetTemperatureMaterials(Material blue, Material green, Material yellow, Material red)
    {
        blueMaterial = blue;
        greenMaterial = green;
        yellowMaterial = yellow;
        redMaterial = red;
        ApplyMaterialBasedOnTemperature();
    }

    public void SetLifeTimer(float value)
    {
        lifeTimer = value;
    }

    public float GetLifeTimer()
    {
        return lifeTimer;
    }

    public void ResetLifeTimer()
    {
        lifeTimer = 0f;
    }

    public float Temperature
    {
        get { return temperature; }
        set
        {
            temperature = Mathf.Clamp(value, 0f, 100f);
            ApplyMaterialBasedOnTemperature();
        }
    }

    public float LastEmittedTemperature
    {
        get { return lastEmittedTemperature; }
    }

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

    // �༭��Ԥ��
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
            if (AreMaterialsAssigned())
            {
                Material previewMaterial = GetMaterialForTemperature(temperature);
                if (previewMaterial != null)
                {
                    objectRenderer.sharedMaterial = previewMaterial;
                }
            }
        }

        if (isEmitter)
        {
            clonePool = new SimpleGameObjectPool(gameObject, transform.parent);
            lastEmittedTemperature = temperature;
        }
    }

    bool AreMaterialsAssigned()
    {
        return blueMaterial != null && greenMaterial != null &&
               yellowMaterial != null && redMaterial != null;
    }
}
