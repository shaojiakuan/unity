using UnityEngine;

/// <summary>
/// 相机距离/视锥可见性剔除优化器。
/// 注：原版本用于控制 Obi 流体组件，因 Obi 插件已从项目中移除，
/// 现改为仅对自身 Renderer 列表做显隐剔除，功能保持通用。
/// </summary>
public class ObiVisibilityOptimizer : MonoBehaviour
{
    [Header("Visibility")]
    public Camera targetCamera;
    public Renderer[] visibilityRenderers;
    public Vector3 fallbackBoundsSize = new Vector3(8f, 8f, 8f);
    public float checkInterval = 0.2f;
    public float maxVisibleDistance = 120f;

    private float nextCheckTime;
    private bool isVisible = true;

    private void Awake()
    {
        if (visibilityRenderers == null || visibilityRenderers.Length == 0)
        {
            visibilityRenderers = GetComponentsInChildren<Renderer>();
        }
    }

    private void OnEnable()
    {
        SetRenderersVisible(true);
        nextCheckTime = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.unscaledTime + Mathf.Max(0.05f, checkInterval);

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                SetRenderersVisible(true);
                return;
            }
        }

        bool shouldBeVisible = IsEffectVisible(targetCamera);
        if (shouldBeVisible != isVisible)
        {
            SetRenderersVisible(shouldBeVisible);
        }
    }

    private bool IsEffectVisible(Camera cameraToUse)
    {
        Bounds bounds = GetVisibilityBounds();

        if (maxVisibleDistance > 0f)
        {
            float sqrDistance = (cameraToUse.transform.position - bounds.center).sqrMagnitude;
            if (sqrDistance > maxVisibleDistance * maxVisibleDistance)
            {
                return false;
            }
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cameraToUse);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    private Bounds GetVisibilityBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(transform.position, fallbackBoundsSize);

        if (visibilityRenderers != null)
        {
            for (int i = 0; i < visibilityRenderers.Length; i++)
            {
                Renderer renderer = visibilityRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(transform.position, fallbackBoundsSize);
        }

        return bounds;
    }

    private void SetRenderersVisible(bool visible)
    {
        isVisible = visible;

        if (visibilityRenderers == null)
        {
            return;
        }

        for (int i = 0; i < visibilityRenderers.Length; i++)
        {
            if (visibilityRenderers[i] != null)
            {
                visibilityRenderers[i].enabled = visible;
            }
        }
    }
}
