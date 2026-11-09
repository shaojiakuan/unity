//using UnityEngine;
//using Newtonsoft.Json;
//using UnityEngine.Networking;
//using System;
//using System.IO;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//namespace DeviceDataSystem
//{
//    // ʹ�ò�ͬ�����������ͻ
//    [System.Serializable]
//    public class DeviceTempData
//    {
//        [JsonProperty("id")]
//        public int? DataId { get; set; }

//        [JsonProperty("name")]
//        public string DataName { get; set; }

//        [JsonProperty("updateTime")]
//        public DateTime UpdateTime { get; set; }

//        [JsonProperty("value")]
//        public decimal TemperatureValue { get; set; }

//        [JsonProperty("unitName")]
//        public string Unit { get; set; }

//        [JsonProperty("deviceId")]
//        public int? SourceDeviceId { get; set; }

//        [JsonProperty("highLimit")]
//        public decimal? MaxLimit { get; set; }

//        [JsonProperty("lowLimit")]
//        public decimal? MinLimit { get; set; }

//        [JsonProperty("alarmTag")]
//        public string AlarmStatus { get; set; }

//        [JsonProperty("notes")]
//        public string Description { get; set; }
//    }

//    public static class TemperatureDataService
//    {
//        public static async Task<DeviceTempData[]> FetchTemperatureDataAsync(int deviceId)
//        {
//            try
//            {
//                string url = $"https://yunchunremote.cn:5656/deviceValue/selectValue/{deviceId}";
//                Debug.Log($"���ڻ�ȡ�¶�����: {url}");

//                using var request = UnityWebRequest.Get(url);
//                request.timeout = 10;

//                var operation = request.SendWebRequest();
//                while (!operation.isDone)
//                {
//                    await Task.Yield();
//                }

//                if (request.result != UnityWebRequest.Result.Success)
//                {
//                    Debug.LogError($"�¶���������ʧ��: {request.error}");
//                    throw new Exception($"�¶���������ʧ��: {request.error}");
//                }

//                var json = request.downloadHandler.text;
//                var serializer = JsonSerializer.Create();
//                using var stringReader = new StringReader(json);
//                using var jsonReader = new JsonTextReader(stringReader);
//                return serializer.Deserialize<DeviceTempData[]>(jsonReader);
//            }
//            catch (Exception ex)
//            {
//                Debug.LogError($"��ȡ�¶������쳣: {ex.Message}");
//                return Array.Empty<DeviceTempData>();
//            }
//        }
//    }
//}

//// Y����ת�����������������¶ȿ��ƣ�
//public class NetworkControlledRotateYAxis : MonoBehaviour
//{
//    public enum RotationDirection
//    {
//        Clockwise,
//        CounterClockwise
//    }

//    // �������н�����ö��
//    public enum MoveDirection
//    {
//        X_Positive,     // X������
//        X_Negative,     // X�Ḻ��
//        Y_Positive,     // Y���������ϣ�
//        Y_Negative,     // Y�Ḻ�����£�
//        Z_Positive,     // Z������Ĭ�ϣ�
//        Z_Negative      // Z�Ḻ��
//    }

//    [Header("��ת����")]
//    public float rotationSpeed = 90f;

//    [Header("�н�����")]
//    public float moveSpeed = 2f;

//    [Header("�н�����")]
//    [Tooltip("ѡ�������н�����")]
//    public MoveDirection moveDirection = MoveDirection.Z_Positive;

//    [Header("�������ڣ����ӣ�")]
//    public float lifeTime = 3f;

//    [Header("��������")]
//    public float spawnInterval = 1f;
//    public bool isEmitter = true;

//    [Header("��ת����")]
//    public RotationDirection direction = RotationDirection.Clockwise;

//    [Header("�����¶�����")]
//    [Tooltip("�Ƿ�������ȡ�¶�")]
//    public bool useNetworkTemperature = true;
//    public int temperatureDeviceId = 88;
//    public int temperatureDataId = 17;
//    public float temperatureUpdateInterval = 2f;

//    [Header("�����¶�����")]
//    [Range(0, 100)]
//    public float temperature = 70f;
//    public bool autoChangeTemp = false; // Ĭ��ʹ�������¶�
//    public float tempChangeSpeed = 10f;

//    [Header("�¶Ȳ���")]
//    public Material blueMaterial;
//    public Material greenMaterial;
//    public Material yellowMaterial;
//    public Material redMaterial;

//    protected float lifeTimer;
//    private float spawnTimer;
//    private float networkUpdateTimer;
//    private Vector3 startPosition;
//    private Quaternion startRotation;
//    private Renderer objectRenderer;
//    private Material currentMaterial;
//    private float lastEmittedTemperature = 0f;
//    void Start()
//    {
//        startPosition = transform.position;
//        startRotation = transform.rotation;

//        objectRenderer = GetComponent<Renderer>();
//        if (objectRenderer == null)
//        {
//            Debug.LogWarning("δ�ҵ�Renderer�������ȷ��������MeshRenderer��SkinnedMeshRenderer");
//        }

//        ApplyMaterialBasedOnTemperature();

//        if (isEmitter)
//        {
//            lastEmittedTemperature = temperature;

//            if (useNetworkTemperature)
//            {
//                UpdateTemperatureFromNetworkAsync();
//            }
//        }
//    }

//    async void Update()
//    {
//        RotateObject();
//        MoveObject();  // �޸�Ϊ�µ��ƶ�����

//        // �����¶ȸ���
//        if (isEmitter && useNetworkTemperature)
//        {
//            networkUpdateTimer += Time.deltaTime;
//            if (networkUpdateTimer >= temperatureUpdateInterval)
//            {
//                networkUpdateTimer = 0f;
//                await UpdateTemperatureFromNetworkAsync();
//            }
//        }

//        // �����¶ȸ���
//        if (autoChangeTemp && !isEmitter)
//        {
//            UpdateLocalTemperature();
//        }

//        ApplyMaterialBasedOnTemperature();

//        if (!isEmitter)
//        {
//            LifeTimer();
//        }

//        if (isEmitter)
//        {
//            SpawnClone();
//        }
//    }

//    // �������ȡ�¶�
//    private async Task UpdateTemperatureFromNetworkAsync()
//    {
//        try
//        {
//            var values = await DeviceDataSystem.TemperatureDataService.FetchTemperatureDataAsync(temperatureDeviceId);

//            if (values == null || values.Length == 0)
//            {
//                Debug.LogWarning($"û���ҵ��¶��豸 {temperatureDeviceId} ������");
//                return;
//            }

//            DeviceDataSystem.DeviceTempData targetData = null;
//            foreach (var data in values)
//            {
//                if (data.DataId == temperatureDataId)
//                {
//                    targetData = data;
//                    break;
//                }
//            }

//            if (targetData != null)
//            {
//                float newTemperature = (float)targetData.TemperatureValue;
//                SetTemperature(newTemperature);

//                Debug.Log($"�����¶ȸ���: {newTemperature}��C (�豸{temperatureDeviceId}, ����{temperatureDataId})");
//            }
//            else
//            {
//                Debug.LogWarning($"û���ҵ��¶�����ID {temperatureDataId}");
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"�����¶ȸ���ʧ��: {ex.Message}");
//        }
//    }

//    void RotateObject()
//    {
//        float speed = direction == RotationDirection.Clockwise ? -rotationSpeed : rotationSpeed;
//        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
//    }

//    // �޸ĺ���ƶ�����
//    void MoveObject()
//    {
//        Vector3 directionVector = GetMoveDirectionVector();
//        transform.Translate(directionVector * moveSpeed * Time.deltaTime, Space.World);
//    }

//    // ��ȡ��ǰ�н����������
//    Vector3 GetMoveDirectionVector()
//    {
//        switch (moveDirection)
//        {
//            case MoveDirection.X_Positive:
//                return Vector3.right;      // (1, 0, 0)

//            case MoveDirection.X_Negative:
//                return Vector3.left;       // (-1, 0, 0)

//            case MoveDirection.Y_Positive:
//                return Vector3.up;         // (0, 1, 0)

//            case MoveDirection.Y_Negative:
//                return Vector3.down;       // (0, -1, 0)

//            case MoveDirection.Z_Positive:
//                return Vector3.forward;    // (0, 0, 1)

//            case MoveDirection.Z_Negative:
//                return Vector3.back;       // (0, 0, -1)

//            default:
//                return Vector3.forward;    // Ĭ��Z������
//        }
//    }

//    // ��ȡ�н�������ַ�����������ѡ�����ڵ��Ի�UI��ʾ��
//    public string GetMoveDirectionDescription()
//    {
//        switch (moveDirection)
//        {
//            case MoveDirection.X_Positive:
//                return "X������ (��)";
//            case MoveDirection.X_Negative:
//                return "X�Ḻ�� (��)";
//            case MoveDirection.Y_Positive:
//                return "Y������ (��)";
//            case MoveDirection.Y_Negative:
//                return "Y�Ḻ�� (��)";
//            case MoveDirection.Z_Positive:
//                return "Z������ (ǰ)";
//            case MoveDirection.Z_Negative:
//                return "Z�Ḻ�� (��)";
//            default:
//                return "δ֪����";
//        }
//    }

//    void LifeTimer()
//    {
//        lifeTimer += Time.deltaTime;
//        if (lifeTimer >= lifeTime)
//        {
//            ReleaseOrDestroy();
//        }
//    }

//    void SpawnClone()
//    {
//        spawnTimer += Time.deltaTime;
//        if (spawnTimer >= spawnInterval)
//        {
//            spawnTimer = 0f;

//            GameObject clone = clonePool != null ? clonePool.Get(startPosition, startRotation) : Instantiate(gameObject, startPosition, startRotation);
//            NetworkControlledRotateYAxis script = clone.GetComponent<NetworkControlledRotateYAxis>();
//            script.isEmitter = false;
//            script.SetLifeTimer(0f);

//            // ���������¶ȣ�ʹ�÷�������ǰ�¶ȣ�
//            script.temperature = temperature;

//            // ������������
//            script.useNetworkTemperature = useNetworkTemperature;
//            script.temperatureDeviceId = temperatureDeviceId;
//            script.temperatureDataId = temperatureDataId;
//            script.temperatureUpdateInterval = temperatureUpdateInterval;

//            // �����н�����
//            script.moveDirection = moveDirection;

//            // ���Ʋ���
//            script.SetTemperatureMaterials(blueMaterial, greenMaterial, yellowMaterial, redMaterial);

//            lastEmittedTemperature = temperature;

//            // ����Ӧ�ò���
//            script.ApplyMaterialBasedOnTemperature();
//        }
//    }

//    void UpdateLocalTemperature()
//    {
//        temperature += tempChangeSpeed * Time.deltaTime;
//        temperature = Mathf.Clamp(temperature, 0f, 100f);
//    }

//    void ApplyMaterialBasedOnTemperature()
//    {
//        if (objectRenderer == null) return;

//        Material newMaterial = GetMaterialForTemperature(temperature);

//        if (newMaterial != null && newMaterial != currentMaterial)
//        {
//            objectRenderer.sharedMaterial = newMaterial;
//            currentMaterial = newMaterial;
//        }
//    }

//    Material GetMaterialForTemperature(float temp)
//    {
//        if (temp < 25f)
//        {
//            return blueMaterial;
//        }
//        else if (temp < 50f)
//        {
//            return greenMaterial;
//        }
//        else if (temp < 75f)
//        {
//            return yellowMaterial;
//        }
//        else
//        {
//            return redMaterial;
//        }
//    }

//    // ���������������н�����
//    public void SetMoveDirection(MoveDirection newDirection)
//    {
//        moveDirection = newDirection;
//    }

//    // ���������������¶�
//    public void SetTemperature(float newTemp)
//    {
//        temperature = Mathf.Clamp(newTemp, 0f, 100f);
//        ApplyMaterialBasedOnTemperature();
//    }

//    // ��ȡ��ǰ�¶ȶ�Ӧ�Ĳ���
//    public Material GetCurrentTemperatureMaterial()
//    {
//        return GetMaterialForTemperature(temperature);
//    }

//    // ���������¶Ȳ���
//    public void SetTemperatureMaterials(Material blue, Material green, Material yellow, Material red)
//    {
//        blueMaterial = blue;
//        greenMaterial = green;
//        yellowMaterial = yellow;
//        redMaterial = red;
//        ApplyMaterialBasedOnTemperature();
//    }

//    // �������Ƿ��ѷ���
//    public bool AreMaterialsAssigned()
//    {
//        return blueMaterial != null && greenMaterial != null &&
//               yellowMaterial != null && redMaterial != null;
//    }

//    // �������ڼ�ʱ������
//    public void SetLifeTimer(float value)
//    {
//        lifeTimer = value;
//    }

//    public float GetLifeTimer()
//    {
//        return lifeTimer;
//    }

//    public void ResetLifeTimer()
//    {
//        lifeTimer = 0f;
//    }

//    // �¶�����
//    public float CurrentTemperature
//    {
//        get { return temperature; }
//        set { SetTemperature(value); }
//    }

//    public float EmitterTemperature
//    {
//        get { return temperature; }
//    }

//    public float LastEmittedTemperature
//    {
//        get { return lastEmittedTemperature; }
//    }

//    // �����¶�״̬
//    public string NetworkTemperatureStatus
//    {
//        get
//        {
//            return useNetworkTemperature ?
//                $"���� (�豸:{temperatureDeviceId}, ����:{temperatureDataId})" :
//                "����";
//        }
//    }

//    // �н�����״̬
//    public string MoveDirectionStatus
//    {
//        get
//        {
//            return GetMoveDirectionDescription();
//        }
//    }

//    // �༭��Ԥ��
//    void OnValidate()
//    {
//        if (!Application.isPlaying && objectRenderer != null)
//        {
//            if (AreMaterialsAssigned())
//            {
//                Material previewMaterial = GetMaterialForTemperature(temperature);
//                if (previewMaterial != null)
//                {
//                    objectRenderer.sharedMaterial = previewMaterial;
//                }
//            }
//        }

//        if (isEmitter)
//        {
//            lastEmittedTemperature = temperature;
//        }
//    }
//}


using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DeviceDataSystem
{
    // ʹ�ò�ͬ�����������ͻ
    [System.Serializable]
    public class DeviceTempData
    {
        [JsonProperty("id")]
        public int? DataId { get; set; }

        [JsonProperty("name")]
        public string DataName { get; set; }

        [JsonProperty("updateTime")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("value")]
        public decimal TemperatureValue { get; set; }

        [JsonProperty("unitName")]
        public string Unit { get; set; }

        [JsonProperty("deviceId")]
        public int? SourceDeviceId { get; set; }

        [JsonProperty("highLimit")]
        public decimal? MaxLimit { get; set; }

        [JsonProperty("lowLimit")]
        public decimal? MinLimit { get; set; }

        [JsonProperty("alarmTag")]
        public string AlarmStatus { get; set; }

        [JsonProperty("notes")]
        public string Description { get; set; }
    }

    public static class TemperatureDataService
    {
        public static async Task<DeviceTempData[]> FetchTemperatureDataAsync(int deviceId)
        {
            try
            {
                string url = $"https://yunchunremote.cn:5656/deviceValue/selectValue/{deviceId}";
                Debug.Log($"���ڻ�ȡ�¶�����: {url}");

                using var request = UnityWebRequest.Get(url);
                request.timeout = 10;

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"�¶���������ʧ��: {request.error}");
                    throw new Exception($"�¶���������ʧ��: {request.error}");
                }

                var json = request.downloadHandler.text;
                var serializer = JsonSerializer.Create();
                using var stringReader = new StringReader(json);
                using var jsonReader = new JsonTextReader(stringReader);
                return serializer.Deserialize<DeviceTempData[]>(jsonReader);
            }
            catch (Exception ex)
            {
                Debug.LogError($"��ȡ�¶������쳣: {ex.Message}");
                return Array.Empty<DeviceTempData>();
            }
        }
    }
}

// �н���������������
[System.Serializable]
public class MovementSequence
{
    public NetworkControlledRotateYAxis.MoveDirection direction;
    public float duration; // �÷��������ʱ�䣨�룩
}

// Y����ת�����������������¶ȿ��ƣ�
public class NetworkControlledRotateYAxis : MonoBehaviour
{
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    // �н�����ö��
    public enum MoveDirection
    {
        X_Positive,     // X������
        X_Negative,     // X�Ḻ��
        Y_Positive,     // Y���������ϣ�
        Y_Negative,     // Y�Ḻ�����£�
        Z_Positive,     // Z������Ĭ�ϣ�
        Z_Negative      // Z�Ḻ��
    }

    [Header("��ת����")]
    public float rotationSpeed = 90f;

    [Header("�н�����")]
    public float moveSpeed = 2f;

    [Header("�н�����ģʽ")]
    [Tooltip("ѡ���н��������ģʽ")]
    public MoveMode moveMode = MoveMode.SingleDirection;

    public enum MoveMode
    {
        SingleDirection,    // ��һ�̶�����
        TimeSequence        // ʱ�����з����л�
    }

    [Header("��һ��������")]
    [Tooltip("ѡ�������н����򣨽��ڵ�һ����ģʽ����Ч��")]
    public MoveDirection singleMoveDirection = MoveDirection.Z_Positive;

    [Header("ʱ�����з�������")]
    [Tooltip("�н������ʱ������")]
    public List<MovementSequence> movementSequence = new List<MovementSequence>();

    [Tooltip("�Ƿ�ѭ��ִ������")]
    public bool loopSequence = true;

    [Tooltip("���п�ʼǰ���ӳ�ʱ�䣨�룩")]
    public float sequenceStartDelay = 0f;

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
    public int temperatureDeviceId = 88;
    public int temperatureDataId = 17;
    public float temperatureUpdateInterval = 2f;

    [Header("�����¶�����")]
    [Range(0, 100)]
    public float temperature = 70f;
    public bool autoChangeTemp = false; // Ĭ��ʹ�������¶�
    public float tempChangeSpeed = 10f;

    [Header("�¶Ȳ���")]
    public Material blueMaterial;
    public Material greenMaterial;
    public Material yellowMaterial;
    public Material redMaterial;

    protected float lifeTimer;
    private float spawnTimer;
    private float networkUpdateTimer;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Renderer objectRenderer;
    private Material currentMaterial;
    private float lastEmittedTemperature = 0f;
    private SimpleGameObjectPool clonePool;

    // ʱ��������ر���
    private int currentSequenceIndex = 0;
    private float currentDirectionTimer = 0f;
    private float sequenceDelayTimer = 0f;
    private bool isSequenceActive = false;
    private MoveDirection currentMoveDirection;

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

        // ��ʼ���ƶ�����
        if (moveMode == MoveMode.SingleDirection)
        {
            currentMoveDirection = singleMoveDirection;
            isSequenceActive = true;
        }
        else if (moveMode == MoveMode.TimeSequence)
        {
            InitializeMovementSequence();
        }

        if (isEmitter)
        {
            clonePool = new SimpleGameObjectPool(gameObject, transform.parent);
            lastEmittedTemperature = temperature;

            if (useNetworkTemperature)
            {
                UpdateTemperatureFromNetworkAsync();
            }
        }
    }

    // ��ʼ��ʱ������
    void InitializeMovementSequence()
    {
        if (movementSequence == null || movementSequence.Count == 0)
        {
            Debug.LogWarning("ʱ������Ϊ�գ�ʹ��Ĭ�Ϸ���");
            movementSequence = new List<MovementSequence>
            {
                new MovementSequence { direction = MoveDirection.Z_Positive, duration = 2f },
                new MovementSequence { direction = MoveDirection.X_Positive, duration = 2f },
                new MovementSequence { direction = MoveDirection.Z_Negative, duration = 2f },
                new MovementSequence { direction = MoveDirection.X_Negative, duration = 2f }
            };
        }

        currentSequenceIndex = 0;
        currentDirectionTimer = 0f;
        sequenceDelayTimer = 0f;
        isSequenceActive = false;

        if (sequenceStartDelay <= 0f)
        {
            StartNextSequence();
        }
    }

    void StartNextSequence()
    {
        if (movementSequence.Count == 0) return;

        if (currentSequenceIndex >= movementSequence.Count)
        {
            if (loopSequence)
            {
                currentSequenceIndex = 0;
            }
            else
            {
                isSequenceActive = false;
                return;
            }
        }

        currentMoveDirection = movementSequence[currentSequenceIndex].direction;
        currentDirectionTimer = 0f;
        isSequenceActive = true;

        Debug.Log($"��ʼ�������� {currentSequenceIndex + 1}/{movementSequence.Count}: {GetMoveDirectionDescription(currentMoveDirection)} ���� {movementSequence[currentSequenceIndex].duration}��");
    }

    async void Update()
    {
        RotateObject();

        // �����н���������
        if (moveMode == MoveMode.TimeSequence)
        {
            UpdateMovementSequence();
        }

        MoveObject();

        // �����¶ȸ���
        if (isEmitter && useNetworkTemperature)
        {
            networkUpdateTimer += Time.deltaTime;
            if (networkUpdateTimer >= temperatureUpdateInterval)
            {
                networkUpdateTimer = 0f;
                await UpdateTemperatureFromNetworkAsync();
            }
        }

        // �����¶ȸ���
        if (autoChangeTemp && !isEmitter)
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

    // ����ʱ������
    void UpdateMovementSequence()
    {
        if (!isSequenceActive && sequenceStartDelay > 0f)
        {
            sequenceDelayTimer += Time.deltaTime;
            if (sequenceDelayTimer >= sequenceStartDelay)
            {
                StartNextSequence();
            }
            return;
        }

        if (!isSequenceActive) return;

        currentDirectionTimer += Time.deltaTime;

        if (currentDirectionTimer >= movementSequence[currentSequenceIndex].duration)
        {
            currentSequenceIndex++;
            if (currentSequenceIndex >= movementSequence.Count && !loopSequence)
            {
                isSequenceActive = false;
                Debug.Log("��������ִ�����");
                return;
            }

            StartNextSequence();
        }
    }

    // �������ȡ�¶�
    private async Task UpdateTemperatureFromNetworkAsync()
    {
        try
        {
            var values = await DeviceDataSystem.TemperatureDataService.FetchTemperatureDataAsync(temperatureDeviceId);

            if (values == null || values.Length == 0)
            {
                Debug.LogWarning($"û���ҵ��¶��豸 {temperatureDeviceId} ������");
                return;
            }

            DeviceDataSystem.DeviceTempData targetData = null;
            foreach (var data in values)
            {
                if (data.DataId == temperatureDataId)
                {
                    targetData = data;
                    break;
                }
            }

            if (targetData != null)
            {
                float newTemperature = (float)targetData.TemperatureValue;
                SetTemperature(newTemperature);

                Debug.Log($"�����¶ȸ���: {newTemperature}��C (�豸{temperatureDeviceId}, ����{temperatureDataId})");
            }
            else
            {
                Debug.LogWarning($"û���ҵ��¶�����ID {temperatureDataId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"�����¶ȸ���ʧ��: {ex.Message}");
        }
    }

    void RotateObject()
    {
        float speed = direction == RotationDirection.Clockwise ? -rotationSpeed : rotationSpeed;
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }

    // �ƶ�����
    void MoveObject()
    {
        if (!isSequenceActive && moveMode == MoveMode.TimeSequence) return;

        Vector3 directionVector = GetMoveDirectionVector(currentMoveDirection);
        transform.Translate(directionVector * moveSpeed * Time.deltaTime, Space.World);
    }

    // ��ȡָ���н����������
    Vector3 GetMoveDirectionVector(MoveDirection moveDir)
    {
        switch (moveDir)
        {
            case MoveDirection.X_Positive:
                return Vector3.right;      // (1, 0, 0)

            case MoveDirection.X_Negative:
                return Vector3.left;       // (-1, 0, 0)

            case MoveDirection.Y_Positive:
                return Vector3.up;         // (0, 1, 0)

            case MoveDirection.Y_Negative:
                return Vector3.down;       // (0, -1, 0)

            case MoveDirection.Z_Positive:
                return Vector3.forward;    // (0, 0, 1)

            case MoveDirection.Z_Negative:
                return Vector3.back;       // (0, 0, -1)

            default:
                return Vector3.forward;    // Ĭ��Z������
        }
    }

    // ��ȡ�н�������ַ�������
    public string GetMoveDirectionDescription(MoveDirection moveDir)
    {
        switch (moveDir)
        {
            case MoveDirection.X_Positive:
                return "X������ (��)";
            case MoveDirection.X_Negative:
                return "X�Ḻ�� (��)";
            case MoveDirection.Y_Positive:
                return "Y������ (��)";
            case MoveDirection.Y_Negative:
                return "Y�Ḻ�� (��)";
            case MoveDirection.Z_Positive:
                return "Z������ (ǰ)";
            case MoveDirection.Z_Negative:
                return "Z�Ḻ�� (��)";
            default:
                return "δ֪����";
        }
    }

    // ��ȡ��ǰ�н���������
    public string GetCurrentDirectionDescription()
    {
        if (moveMode == MoveMode.SingleDirection)
        {
            return GetMoveDirectionDescription(singleMoveDirection);
        }
        else
        {
            if (!isSequenceActive) return "����δ����";
            return $"{GetMoveDirectionDescription(currentMoveDirection)} ({currentDirectionTimer:F1}/{movementSequence[currentSequenceIndex].duration:F1}��)";
        }
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
            NetworkControlledRotateYAxis script = clone.GetComponent<NetworkControlledRotateYAxis>();
            script.isEmitter = false;
            script.SetLifeTimer(0f);

            // ���������¶ȣ�ʹ�÷�������ǰ�¶ȣ�
            script.temperature = temperature;

            // ������������
            script.useNetworkTemperature = useNetworkTemperature;
            script.temperatureDeviceId = temperatureDeviceId;
            script.temperatureDataId = temperatureDataId;
            script.temperatureUpdateInterval = temperatureUpdateInterval;

            // �����н���������
            script.moveMode = moveMode;
            script.singleMoveDirection = singleMoveDirection;

            // ����ʱ�����У���ȸ��ƣ�
            if (movementSequence != null && movementSequence.Count > 0)
            {
                script.movementSequence = new List<MovementSequence>();
                foreach (var seq in movementSequence)
                {
                    script.movementSequence.Add(new MovementSequence
                    {
                        direction = seq.direction,
                        duration = seq.duration
                    });
                }
            }

            script.loopSequence = loopSequence;
            script.sequenceStartDelay = sequenceStartDelay;

            // ���Ʋ���
            script.SetTemperatureMaterials(blueMaterial, greenMaterial, yellowMaterial, redMaterial);

            lastEmittedTemperature = temperature;

            // ����Ӧ�ò���
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

    // ���������������н�����
    public void ResetMovementSequence()
    {
        if (moveMode == MoveMode.TimeSequence)
        {
            InitializeMovementSequence();
        }
    }

    // ������������ת�������ض�λ��
    public void JumpToSequenceIndex(int index)
    {
        if (moveMode == MoveMode.TimeSequence && index >= 0 && index < movementSequence.Count)
        {
            currentSequenceIndex = index;
            StartNextSequence();
        }
    }

    // ���������������н���������
    public void AddMovementSequence(MoveDirection direction, float duration)
    {
        movementSequence.Add(new MovementSequence { direction = direction, duration = duration });
    }

    // ���������������������
    public void ClearMovementSequence()
    {
        movementSequence.Clear();
    }

    // ���������������н�����ģʽ
    public void SetMoveMode(MoveMode newMode)
    {
        moveMode = newMode;
        if (moveMode == MoveMode.SingleDirection)
        {
            currentMoveDirection = singleMoveDirection;
            isSequenceActive = true;
        }
        else if (moveMode == MoveMode.TimeSequence)
        {
            InitializeMovementSequence();
        }
    }

    // �������������õ�һ����
    public void SetSingleMoveDirection(MoveDirection newDirection)
    {
        singleMoveDirection = newDirection;
        if (moveMode == MoveMode.SingleDirection)
        {
            currentMoveDirection = singleMoveDirection;
        }
    }

    // ���������������¶�
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

    // ���������¶Ȳ���
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
        return blueMaterial != null && greenMaterial != null &&
               yellowMaterial != null && redMaterial != null;
    }

    // �������ڼ�ʱ������
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

    // �¶�����
    public float CurrentTemperature
    {
        get { return temperature; }
        set { SetTemperature(value); }
    }

    public float EmitterTemperature
    {
        get { return temperature; }
    }

    public float LastEmittedTemperature
    {
        get { return lastEmittedTemperature; }
    }

    // �����¶�״̬
    public string NetworkTemperatureStatus
    {
        get
        {
            return useNetworkTemperature ?
                $"���� (�豸:{temperatureDeviceId}, ����:{temperatureDataId})" :
                "����";
        }
    }

    // �н�����״̬
    public string MoveDirectionStatus
    {
        get
        {
            return GetCurrentDirectionDescription();
        }
    }

    // ����״̬��Ϣ
    public string SequenceStatus
    {
        get
        {
            if (moveMode == MoveMode.SingleDirection)
            {
                return "��һ����ģʽ";
            }
            else
            {
                if (!isSequenceActive)
                {
                    if (sequenceDelayTimer < sequenceStartDelay)
                    {
                        return $"�ȴ���ʼ: {sequenceDelayTimer:F1}/{sequenceStartDelay:F1}��";
                    }
                    else
                    {
                        return "�����ѽ���";
                    }
                }
                return $"���� {currentSequenceIndex + 1}/{movementSequence.Count}";
            }
        }
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
}
