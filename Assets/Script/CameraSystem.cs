using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CameraSystem : MonoBehaviour
{
    public Button button_up;
    public Button button_down;
    public Button button_left;
    public Button button_right;
    public Button button_up_rotate;
    public Button button_down_rotate;
    public Button button_left_rotate;
    public Button button_right_rotate;

    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [Header("Camera Control Speed")]
    [FormerlySerializedAs("moveSpeed")]
    [SerializeField] private float cameraMoveSpeed = 1f;
    [SerializeField] private float keyboardRotationSpeed = 35f;
    [SerializeField] private float mouseRotationSpeed = 3f;
    [SerializeField] private Transform controlTarget;
    [SerializeField] private bool preferActiveMainCamera = true;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private Vector3 followOffset;
    private float yaw;
    private float pitch;
    private bool mouseLookActive;
    private EventSystem pointerEventSystem;
    private PointerEventData pointerEventData;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private void Start()
    {
        ResolveControlTarget();

        if (virtualCamera != null)
        {
            CinemachineTransposer transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                followOffset = transposer.m_FollowOffset;
            }
        }

        SyncRotationFromTransform();

        BindButtons();

        // 主场景内创建相机点位切换按钮列表（幂等；同时形成引用，防止 IL2CPP 剥离）
        CameraPointSwitcher.EnsureCreated();
    }

    private void ResolveControlTarget()
    {
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineVirtualCamera>();
        }

        Transform resolvedTarget = controlTarget;
        if (preferActiveMainCamera)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
                if (brain != null && brain.isActiveAndEnabled)
                {
                    // On scene load the Brain may not have completed its first update yet.
                    // Never move its output camera: the Brain will overwrite that transform.
                    ICinemachineCamera activeCamera = brain.ActiveVirtualCamera;
                    resolvedTarget = activeCamera != null
                        ? activeCamera.VirtualCameraGameObject.transform
                        : (virtualCamera != null ? virtualCamera.transform : null);
                }
                else
                {
                    resolvedTarget = mainCamera.transform;
                }
            }
            else
            {
                resolvedTarget = virtualCamera != null ? virtualCamera.transform : transform;
            }
        }
        else if (resolvedTarget == null)
        {
            resolvedTarget = virtualCamera != null ? virtualCamera.transform : transform;
        }

        if (controlTarget == resolvedTarget) return;
        controlTarget = resolvedTarget;
        mouseLookActive = false;
        SyncRotationFromTransform();
        if (controlTarget != null)
        {
            Debug.Log($"CameraSystem target: {controlTarget.name}", controlTarget);
        }
    }

    private void Update()
    {
        // UI uses the left button; camera look uses an unlocked right-button drag.
        EnsureCursorVisible();
        // Re-evaluate after Brain initialization and whenever the live virtual camera changes.
        ResolveControlTarget();
        if (controlTarget == null || !Application.isFocused || IsTextInputFocused())
        {
            mouseLookActive = false;
            return;
        }

        HandleCameraMovement();
        HandleMouseLookMode();
        HandleCameraRotation();
    }

    /// <summary>确保光标始终可见且不被指针锁定（含其他脚本/运行时尝试锁定的情况）。</summary>
    private void EnsureCursorVisible()
    {
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void BindButtons()
    {
        if (button_up != null) button_up.onClick.AddListener(OnButtonClick_up);
        if (button_down != null) button_down.onClick.AddListener(OnButtonClick_down);
        if (button_left != null) button_left.onClick.AddListener(OnButtonClick_left);
        if (button_right != null) button_right.onClick.AddListener(OnButtonClick_right);
        if (button_up_rotate != null) button_up_rotate.onClick.AddListener(OnButtonClick_up_rotate);
        if (button_down_rotate != null) button_down_rotate.onClick.AddListener(OnButtonClick_down_rotate);
        if (button_left_rotate != null) button_left_rotate.onClick.AddListener(OnButtonClick_left_rotate);
        if (button_right_rotate != null) button_right_rotate.onClick.AddListener(OnButtonClick_right_rotate);
    }

    private void OnButtonClick_up()
    {
        MoveCamera(0f, 0.1f, 0.1f);
    }

    private void OnButtonClick_down()
    {
        MoveCamera(0f, -0.1f, 0.1f);
    }

    private void OnButtonClick_left()
    {
        MoveCamera(-0.1f, 0f, 0.1f);
    }

    private void OnButtonClick_right()
    {
        MoveCamera(0.1f, 0f, 0.1f);
    }

    private void OnButtonClick_up_rotate()
    {
        ApplyRotation(0f, -0.1f * Time.deltaTime * 3f);
    }

    private void OnButtonClick_down_rotate()
    {
        ApplyRotation(0f, 0.1f * Time.deltaTime * 3f);
    }

    private void OnButtonClick_left_rotate()
    {
        ApplyRotation(-0.1f * Time.deltaTime * 3f, 0f);
    }

    private void OnButtonClick_right_rotate()
    {
        ApplyRotation(0.1f * Time.deltaTime * 3f, 0f);
    }

    private void HandleCameraMovement()
    {
        Vector3 inputDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) inputDir.z = 1f;
        if (Input.GetKey(KeyCode.S)) inputDir.z = -1f;
        if (Input.GetKey(KeyCode.D)) inputDir.x = 1f;
        if (Input.GetKey(KeyCode.A)) inputDir.x = -1f;

        MoveCamera(inputDir.x, inputDir.z, cameraMoveSpeed);
    }

    private void HandleCameraRotation()
    {
        float yawInput = 0f;
        float pitchInput = 0f;

        // J / K 键已让给视角切换（CameraPointSwitcher），这里只保留 L（右转）与 I（上俯）
        if (Input.GetKey(KeyCode.L)) yawInput = 1f;
        if (Input.GetKey(KeyCode.I)) pitchInput = -1f;

        ApplyRotation(yawInput * Time.deltaTime * keyboardRotationSpeed, pitchInput * Time.deltaTime * keyboardRotationSpeed);

        if (!ShouldReadMouseRotation())
        {
            return;
        }

        float mouseYaw = Input.GetAxis("Mouse X") * mouseRotationSpeed;
        float mousePitch = -Input.GetAxis("Mouse Y") * mouseRotationSpeed;
        ApplyRotation(mouseYaw, mousePitch);
    }

    private void MoveCamera(float horizontal, float forward, float speed)
    {
        if (controlTarget == null) return;
        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(forward, 0f))
        {
            return;
        }

        Vector3 moveDir = controlTarget.forward * forward + controlTarget.right * horizontal;
        controlTarget.position += moveDir * speed * Time.deltaTime;
    }

    private void HandleMouseLookMode()
    {
        Vector3 position = Input.mousePosition;
        if (!Input.GetMouseButton(1) || Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) ||
            position.x < 0f || position.x >= Screen.width ||
            position.y < 0f || position.y >= Screen.height || IsPointerOverUI())
        {
            mouseLookActive = false;
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            // Camera point switches can change the transform between drags.
            SyncRotationFromTransform();
            mouseLookActive = true;
        }
    }

    private bool ShouldReadMouseRotation()
    {
        // Ignore the press-frame delta so starting a drag cannot jump the view.
        return mouseLookActive && !Input.GetMouseButtonDown(1);
    }

    private bool IsPointerOverUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (pointerEventSystem != eventSystem || pointerEventData == null)
        {
            pointerEventSystem = eventSystem;
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.Reset();
        pointerEventData.position = Input.mousePosition;
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, uiRaycastResults);
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            RaycastResult hit = uiRaycastResults[i];
            // Ignore boiler meshes and decorative text overlays; protect interactive UI.
            if (!(hit.module is GraphicRaycaster)) continue;
            GameObject target = hit.gameObject;
            if (target.GetComponentInParent<Selectable>() != null ||
                ExecuteEvents.GetEventHandler<IPointerDownHandler>(target) != null ||
                ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null ||
                ExecuteEvents.GetEventHandler<IDragHandler>(target) != null ||
                ExecuteEvents.GetEventHandler<IScrollHandler>(target) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTextInputFocused()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null) return false;

        InputField input = selected.GetComponentInParent<InputField>();
        if (input != null && input.isFocused) return true;
        TMPro.TMP_InputField tmpInput = selected.GetComponentInParent<TMPro.TMP_InputField>();
        return tmpInput != null && tmpInput.isFocused;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) mouseLookActive = false;
    }

    private void OnDisable()
    {
        mouseLookActive = false;
        EnsureCursorVisible();
    }

    private void ApplyRotation(float yawDelta, float pitchDelta)
    {
        if (controlTarget == null) return;
        if (Mathf.Approximately(yawDelta, 0f) && Mathf.Approximately(pitchDelta, 0f))
        {
            return;
        }

        yaw += yawDelta;
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
        controlTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    /// <summary>
    /// 从当前控制目标（虚拟相机/主相机）的欧拉角重新同步内部 yaw/pitch。
    /// 供外部脚本（如 CameraViewSwitcher）完成视角切换后调用，防止后续手动旋转跳变。
    /// </summary>
    public void SyncRotationFromTransform()
    {
        if (controlTarget == null) return;
        Vector3 angles = controlTarget.eulerAngles;
        yaw = angles.y;
        pitch = NormalizeAngle(angles.x);
    }
}
