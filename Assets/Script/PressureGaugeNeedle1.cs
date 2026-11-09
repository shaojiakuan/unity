//using UnityEngine;

//public class PressureGaugeNeedle1 : MonoBehaviour
//{
//    [Header("指针对象")]
//    [SerializeField] private Transform needle;

//    [Header("压力表参数")]
//    [SerializeField] private float minPressure = 0f;
//    [SerializeField] private float maxPressure = 200f;

//    // 每增加1个压力单位，指针旋转1.37度
//    [SerializeField] private float anglePerPressure = 1.37f;

//    [Header("旋转方向")]
//    [Tooltip("顺时针通常设置为-1，逆时针设置为1")]
//    [SerializeField] private float rotationDirection = 1f;

//    [Header("模拟数据")]
//    [SerializeField] private bool useSimulation = true;

//    [Tooltip("完成一次0→200→0所需的时间，单位：秒")]
//    [SerializeField] private float simulationCycleTime = 20f;

//    [Header("指针平滑")]
//    [SerializeField] private float smoothTime = 0.15f;

//    [Header("当前压力，仅用于观察")]
//    [SerializeField] private float currentPressure;

//    // 保存导入模型时原来的旋转角度
//    private Vector3 initialEulerAngles;

//    private float currentZAngle;
//    private float zVelocity;

//    private void Awake()
//    {
//        // 脚本直接挂在指针上时，可以不手动赋值
//        if (needle == null)
//        {
//            needle = transform;
//        }

//        // 保存指针原来的X、Y、Z旋转
//        initialEulerAngles = needle.localEulerAngles;
//        currentZAngle = initialEulerAngles.z;
//    }

//    private void Update()
//    {
//        if (useSimulation)
//        {
//            GenerateSimulationPressure();
//        }

//        RotateNeedle(currentPressure);
//    }

//    /// <summary>
//    /// 生成0～200～0的平滑模拟压力数据。
//    /// </summary>
//    private void GenerateSimulationPressure()
//    {
//        float cycleTime = Mathf.Max(simulationCycleTime, 0.1f);

//        // 0 → 1 → 0循环变化
//        float normalizedPressure = Mathf.PingPong(
//            Time.time * 2f / cycleTime,
//            1f
//        );

//        currentPressure = Mathf.Lerp(
//            minPressure,
//            maxPressure,
//            normalizedPressure
//        );
//    }

//    /// <summary>
//    /// 根据压力值驱动表针。
//    /// 后续接入PLC、数据库或FastAPI时，可直接调用该方法。
//    /// </summary>
//    public void SetPressure(float pressure)
//    {
//        useSimulation = false;
//        currentPressure = Mathf.Clamp(
//            pressure,
//            minPressure,
//            maxPressure
//        );
//    }

//    /// <summary>
//    /// 只修改指针局部坐标的Z轴旋转。
//    /// </summary>
//    private void RotateNeedle(float pressure)
//    {
//        pressure = Mathf.Clamp(
//            pressure,
//            minPressure,
//            maxPressure
//        );

//        float pressureOffset = pressure - minPressure;

//        float targetZAngle =
//            initialEulerAngles.z
//            + pressureOffset
//            * anglePerPressure
//            * rotationDirection;

//        currentZAngle = Mathf.SmoothDampAngle(
//            currentZAngle,
//            targetZAngle,
//            ref zVelocity,
//            smoothTime
//        );

//        // X和Y始终保持模型导入时的原始值，只改变Z
//        needle.localRotation = Quaternion.Euler(
//            initialEulerAngles.x,
//            initialEulerAngles.y,
//            currentZAngle
//        );
//    }
//}



using UnityEngine;

public class PressureGaugeNeedle1 : MonoBehaviour
{
    [Header("指针对象")]
    [SerializeField]
    private Transform needle;

    [Header("压力表参数")]
    [SerializeField]
    private float minPressure = 0f;

    [SerializeField]
    private float maxPressure = 200f;

    [Tooltip("每增加1个压力单位，指针旋转的角度")]
    [SerializeField]
    private float anglePerPressure = 1.37f;

    [Header("旋转方向")]
    [Tooltip("顺时针通常设置为-1，逆时针设置为1")]
    [SerializeField]
    private float rotationDirection = 1f;


    [Header("数据模式")]

    [Tooltip(
        "勾选：使用Unity内部模拟数据\n" +
        "取消：等待FastAPI/PLC等外部数据"
    )]
    [SerializeField]
    private bool useSimulation = false;


    [Header("模拟参数")]

    [Tooltip("完成一次0→200→0所需的时间")]
    [SerializeField]
    private float simulationCycleTime = 20f;


    [Header("指针平滑")]

    [SerializeField]
    private float smoothTime = 0.15f;


    [Header("运行时状态")]

    [SerializeField]
    private float currentPressure;

    // 模型原始角度
    private Vector3 initialEulerAngles;

    private float currentZAngle;

    private float zVelocity;


    private void Awake()
    {
        if (needle == null)
        {
            needle = transform;
        }

        initialEulerAngles =
            needle.localEulerAngles;

        currentZAngle =
            initialEulerAngles.z;
    }


    private void Update()
    {
        // =========================
        // 模拟模式
        // =========================

        if (useSimulation)
        {
            GenerateSimulationPressure();
        }

        // =========================
        // 根据当前压力更新表针
        // =========================

        RotateNeedle(
            currentPressure
        );
    }


    /// <summary>
    /// 生成0 → 200 → 0的模拟压力。
    /// </summary>
    private void GenerateSimulationPressure()
    {
        float cycleTime =
            Mathf.Max(
                simulationCycleTime,
                0.1f
            );

        float normalizedPressure =
            Mathf.PingPong(
                Time.time * 2f / cycleTime,
                1f
            );

        currentPressure =
            Mathf.Lerp(
                minPressure,
                maxPressure,
                normalizedPressure
            );
    }


    /// <summary>
    /// 外部压力数据入口。
    /// FastAPI、PLC、数据库等均调用这个方法。
    /// </summary>
    public void SetPressure(
        float pressure
    )
    {
        // 一旦收到外部数据，
        // 自动停止内部模拟。
        useSimulation = false;

        currentPressure =
            Mathf.Clamp(
                pressure,
                minPressure,
                maxPressure
            );
    }


    /// <summary>
    /// 根据压力值旋转指针。
    /// </summary>
    private void RotateNeedle(
        float pressure
    )
    {
        pressure =
            Mathf.Clamp(
                pressure,
                minPressure,
                maxPressure
            );

        float pressureOffset =
            pressure - minPressure;

        float targetZAngle =
            initialEulerAngles.z
            + pressureOffset
            * anglePerPressure
            * rotationDirection;


        currentZAngle =
            Mathf.SmoothDampAngle(
                currentZAngle,
                targetZAngle,
                ref zVelocity,
                smoothTime
            );


        needle.localRotation =
            Quaternion.Euler(
                initialEulerAngles.x,
                initialEulerAngles.y,
                currentZAngle
            );
    }


    /// <summary>
    /// 手动切换模拟模式。
    /// </summary>
    public void SetSimulationMode(
        bool enabled
    )
    {
        useSimulation = enabled;
    }
}
