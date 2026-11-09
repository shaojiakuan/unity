using System.Collections.Generic;
using UnityEngine;

public class MaterialRendererVisibilityCuller : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;
    public string cameraTag = "MainCamera";

    [Header("Visibility")]
    public Transform[] visibilityPoints;
    public float showDistance = 35f;
    public float hideDistance = 45f;
    public float viewportMargin = 0.08f;
    public bool requireCameraFrustum = true;
    public bool hideOnStart = true;

    [Header("Targets")]
    public GameObject[] targetRoots;
    public bool includeChildren = true;

    [Header("Dynamic Cache")]
    public float checkInterval = 0.12f;
    public float rebuildCacheInterval = 1.0f;

    [Header("Optional Spawn Control")]
    public bool pauseGeneratorsWhenHidden = true;
    public createranliao[] continuousGenerators;
    public ranliao4stage[] stageGenerators;
    public MaterialPile[] materialPiles;

    [Header("Debug")]
    public bool logStateChanges = false;

    private readonly List<RendererState> rendererStates = new List<RendererState>(2048);
    private float nextCheckTime;
    private float nextRebuildTime;
    private bool isVisible;
    private bool hasAppliedState;

    private struct RendererState
    {
        public Renderer Renderer;
        public bool OriginalEnabled;
    }

    private void Awake()
    {
        ResolveCamera();
        CacheTargets();
    }

    private void Start()
    {
        if (hideOnStart)
        {
            SetVisible(false);
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRebuildTime)
        {
            CacheTargets();
        }

        if (Time.unscaledTime < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.unscaledTime + Mathf.Max(0.02f, checkInterval);

        if (targetCamera == null)
        {
            ResolveCamera();
        }

        if (targetCamera == null)
        {
            return;
        }

        bool shouldShow = ShouldShow();
        if (shouldShow == isVisible && hasAppliedState)
        {
            return;
        }

        SetVisible(shouldShow);
    }

    private void OnDisable()
    {
        RestoreOriginalState();
        SetGeneratorsEnabled(true);
    }

    [ContextMenu("Refresh Target Cache")]
    public void CacheTargets()
    {
        rendererStates.Clear();
        nextRebuildTime = Time.unscaledTime + Mathf.Max(0.2f, rebuildCacheInterval);

        if (targetRoots == null || targetRoots.Length == 0)
        {
            CacheRenderers(gameObject);
            return;
        }

        for (int i = 0; i < targetRoots.Length; i++)
        {
            GameObject root = targetRoots[i];
            if (root != null)
            {
                CacheRenderers(root);
            }
        }
    }

    [ContextMenu("Show Materials")]
    public void ShowMaterials()
    {
        SetVisible(true);
    }

    [ContextMenu("Hide Materials")]
    public void HideMaterials()
    {
        SetVisible(false);
    }

    private void ResolveCamera()
    {
        if (targetCamera != null)
        {
            return;
        }

        targetCamera = Camera.main;
        if (targetCamera != null)
        {
            return;
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera sceneCamera = cameras[i];
            if (sceneCamera != null && sceneCamera.isActiveAndEnabled && sceneCamera.CompareTag(cameraTag))
            {
                targetCamera = sceneCamera;
                return;
            }
        }
    }

    private void CacheRenderers(GameObject root)
    {
        Renderer[] renderers = includeChildren
            ? root.GetComponentsInChildren<Renderer>(true)
            : root.GetComponents<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (IsExcludedFromCulling(renderer))
            {
                continue;
            }

            rendererStates.Add(new RendererState
            {
                Renderer = renderer,
                OriginalEnabled = renderer.enabled
            });
        }
    }

    private bool ShouldShow()
    {
        float threshold = isVisible ? hideDistance : showDistance;
        if (!IsNearEnough(threshold))
        {
            return false;
        }

        if (!requireCameraFrustum)
        {
            return true;
        }

        return HasPointInView();
    }

    private bool IsNearEnough(float distance)
    {
        if (distance <= 0f || visibilityPoints == null || visibilityPoints.Length == 0)
        {
            return true;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        float sqrDistance = distance * distance;

        for (int i = 0; i < visibilityPoints.Length; i++)
        {
            Transform point = visibilityPoints[i];
            if (point != null && (point.position - cameraPosition).sqrMagnitude <= sqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPointInView()
    {
        if (visibilityPoints == null || visibilityPoints.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < visibilityPoints.Length; i++)
        {
            Transform point = visibilityPoints[i];
            if (point == null)
            {
                continue;
            }

            Vector3 viewport = targetCamera.WorldToViewportPoint(point.position);
            if (viewport.z <= targetCamera.nearClipPlane || viewport.z >= targetCamera.farClipPlane)
            {
                continue;
            }

            if (viewport.x >= -viewportMargin
                && viewport.x <= 1f + viewportMargin
                && viewport.y >= -viewportMargin
                && viewport.y <= 1f + viewportMargin)
            {
                return true;
            }
        }

        return false;
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        hasAppliedState = true;

        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.Renderer != null)
            {
                if (IsExcludedFromCulling(state.Renderer))
                {
                    state.Renderer.enabled = true;
                    continue;
                }

                state.Renderer.enabled = visible && state.OriginalEnabled;
            }
        }

        if (pauseGeneratorsWhenHidden)
        {
            SetGeneratorsEnabled(visible);
        }

        if (logStateChanges)
        {
            Debug.Log($"{name}: material renderers visible = {visible}", this);
        }
    }

    private void SetGeneratorsEnabled(bool enabled)
    {
        if (continuousGenerators != null)
        {
            for (int i = 0; i < continuousGenerators.Length; i++)
            {
                if (continuousGenerators[i] != null)
                {
                    continuousGenerators[i].enabled = enabled;
                }
            }
        }

        if (stageGenerators != null)
        {
            for (int i = 0; i < stageGenerators.Length; i++)
            {
                if (stageGenerators[i] != null)
                {
                    stageGenerators[i].enabled = enabled;
                }
            }
        }

        if (materialPiles != null)
        {
            for (int i = 0; i < materialPiles.Length; i++)
            {
                if (materialPiles[i] != null)
                {
                    materialPiles[i].enabled = enabled;
                }
            }
        }
    }

    private void RestoreOriginalState()
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.Renderer != null)
            {
                if (IsExcludedFromCulling(state.Renderer))
                {
                    state.Renderer.enabled = true;
                    continue;
                }

                state.Renderer.enabled = state.OriginalEnabled;
            }
        }
    }

    private static bool IsExcludedFromCulling(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        RendererCullingExclusion[] exclusions = renderer.GetComponentsInParent<RendererCullingExclusion>(true);
        for (int i = 0; i < exclusions.Length; i++)
        {
            RendererCullingExclusion exclusion = exclusions[i];
            if (exclusion == null)
            {
                continue;
            }

            if (exclusion.transform == renderer.transform || IsSmokeBoxName(exclusion.name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSmokeBoxName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string lowerName = objectName.ToLowerInvariant();
        return lowerName.Contains("烟箱")
            || lowerName.Contains("后烟箱")
            || lowerName.Contains("前烟箱")
            || lowerName.Contains("smokebox")
            || lowerName.Contains("smoke box");
    }
}
