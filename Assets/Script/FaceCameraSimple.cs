using UnityEngine;

public class FaceCameraSimple : MonoBehaviour
{
    [Header("目标相机")]
    public Camera targetCamera;

    [Header("跟随设置")]
    public bool followRotation = true;
    public bool followPosition = false;
    public Vector3 positionOffset = Vector3.zero;

    [Header("旋转设置")]
    public bool flipForward = true;  // 是否翻转使得正面朝向相机
    public Vector3 rotationOffset = Vector3.zero;

    void Start()
    {
        // 如果没有指定相机，使用主相机
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("没有找到相机！请指定一个相机或确保场景中有Main Camera标签的相机。");
        }
    }

    void Update()
    {
        if (targetCamera == null) return;

        UpdatePlaneTransform();
    }

    void LateUpdate()
    {
        // 也可以放在LateUpdate中，确保在相机移动后更新
        // if (targetCamera == null) return;
        // UpdatePlaneTransform();
    }

    void UpdatePlaneTransform()
    {
        // 跟随位置
        if (followPosition)
        {
            transform.position = targetCamera.transform.position + positionOffset;
        }

        // 跟随旋转
        if (followRotation)
        {
            Vector3 directionToCamera;

            if (flipForward)
            {
                // 让Plane的正面朝向相机（通常Plane的法线是向上的，但正面是向前的）
                // 这里计算从相机指向Plane的方向
                directionToCamera = transform.position - targetCamera.transform.position;
            }
            else
            {
                // 或者让Plane的背面朝向相机
                directionToCamera = targetCamera.transform.position - transform.position;
            }

            // 使用LookRotation使物体的前方向（Z轴正方向）朝向目标方向
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);

            // 应用旋转偏移
            if (rotationOffset != Vector3.zero)
            {
                targetRotation *= Quaternion.Euler(rotationOffset);
            }

            transform.rotation = targetRotation;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("测试面向主相机")]
    void TestFaceMainCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        if (targetCamera != null)
        {
            UpdatePlaneTransform();
            Debug.Log($"Plane已面向相机: {targetCamera.name}");
        }
    }
#endif
}