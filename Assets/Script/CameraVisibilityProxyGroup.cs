using System.Collections.Generic;
using UnityEngine;

public class CameraVisibilityProxyGroup : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;
    public string cameraTag = "MainCamera";

    [Header("Visibility Proxies")]
    public Transform[] visibilityPoints;
    public Collider visibilityBounds;
    public Renderer visibilityRenderer;
    public float viewportMargin = 0.08f;

    [Header("Distance")]
    public float showDistance = 35f;
    public float hideDistance = 42f;
    public bool requireCameraFrustum = true;

    [Header("Targets")]
    public GameObject[] targetRoots;
    [Tooltip("Deprecated. Targets are now direct-only to avoid parent/child visibility conflicts.")]
    public bool includeChildren = false;
    public bool hideOnStart = true;

    [Header("Performance")]
    public float checkInterval = 0.12f;
    public bool keepMotionRunningWhenHidden = true;
    public bool stopParticleSystemsWhenHidden = true;
    public bool throttleParticleSystems = true;
    public int maxVisibleParticleSystems = 12;
    public int maxParticlesPerSystem = 80;
    public float emissionRateScale = 0.35f;
    public DuodouGroupMotionController[] motionControllers;

    [Header("Debug")]
    public bool logStateChanges = false;

    private static readonly Dictionary<Renderer, RendererVisibilityRecord> sharedRendererStates = new Dictionary<Renderer, RendererVisibilityRecord>();
    private readonly List<RendererState> rendererStates = new List<RendererState>(512);
    private readonly List<ParticleSystemState> particleSystemStates = new List<ParticleSystemState>(64);
    private readonly Plane[] frustumPlanes = new Plane[6];
    private float nextCheckTime;
    private bool isVisible;
    private bool hasAppliedState;

    private struct RendererState
    {
        public Renderer Renderer;
        public bool OriginalEnabled;
    }

    private struct ParticleSystemState
    {
        public ParticleSystem ParticleSystem;
        public int OriginalMaxParticles;
        public ParticleSystem.MinMaxCurve OriginalRateOverTime;
        public bool OriginalRendererEnabled;
    }

    private class RendererVisibilityRecord
    {
        public bool OriginalEnabled;
        public readonly Dictionary<CameraVisibilityProxyGroup, bool> GroupVisibility = new Dictionary<CameraVisibilityProxyGroup, bool>();
    }

    private void Awake()
    {
        ResolveCamera();
        CacheTargets();
        NormalizeDistances();
    }

    private void Start()
    {
        if (hideOnStart)
        {
            SetTargetsVisible(false);
        }
    }

    private void Update()
    {
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

        bool shouldShow = EvaluateVisibility();
        if (shouldShow == isVisible && hasAppliedState)
        {
            return;
        }

        SetTargetsVisible(shouldShow);
    }

    private void OnDisable()
    {
        RestoreOriginalState();
    }

    [ContextMenu("Refresh Target Cache")]
    public void CacheTargets()
    {
        UnregisterSharedRenderers();
        rendererStates.Clear();
        RestoreParticleSystems();
        particleSystemStates.Clear();

        if (targetRoots == null)
        {
            return;
        }

        HashSet<Renderer> cachedRenderers = new HashSet<Renderer>();
        for (int i = 0; i < targetRoots.Length; i++)
        {
            GameObject root = targetRoots[i];
            if (root == null)
            {
                continue;
            }

            Renderer[] renderers = root.GetComponents<Renderer>();
            if (renderers.Length == 0 && logStateChanges)
            {
                Debug.Log($"{name}: target '{root.name}' has no direct Renderer. Add its visible child objects directly to Target Roots.", root);
            }

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                {
                    continue;
                }

                if (IsExcludedFromCulling(renderer))
                {
                    continue;
                }

                if (!cachedRenderers.Add(renderer))
                {
                    continue;
                }

                rendererStates.Add(new RendererState
                {
                    Renderer = renderer,
                    OriginalEnabled = renderer.enabled
                });
            }

            ParticleSystem particleSystem = root.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                ParticleSystem.EmissionModule emission = particleSystem.emission;
                ParticleSystem.MainModule main = particleSystem.main;
                particleSystemStates.Add(new ParticleSystemState
                {
                    ParticleSystem = particleSystem,
                    OriginalMaxParticles = main.maxParticles,
                    OriginalRateOverTime = emission.rateOverTime,
                    OriginalRendererEnabled = particleRenderer == null || particleRenderer.enabled
                });
            }
        }
    }

    [ContextMenu("Show Targets")]
    public void ShowTargets()
    {
        SetTargetsVisible(true);
    }

    [ContextMenu("Hide Targets")]
    public void HideTargets()
    {
        SetTargetsVisible(false);
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

    private void NormalizeDistances()
    {
        showDistance = Mathf.Max(0f, showDistance);
        hideDistance = Mathf.Max(showDistance, hideDistance);
    }

    private bool EvaluateVisibility()
    {
        NormalizeDistances();

        float threshold = isVisible ? hideDistance : showDistance;
        if (!IsWithinDistance(threshold))
        {
            return false;
        }

        if (!requireCameraFrustum)
        {
            return true;
        }

        GeometryUtility.CalculateFrustumPlanes(targetCamera, frustumPlanes);

        if (HasVisibleProxyPoint())
        {
            return true;
        }

        if (visibilityBounds != null && GeometryUtility.TestPlanesAABB(frustumPlanes, visibilityBounds.bounds))
        {
            return true;
        }

        if (visibilityRenderer != null && GeometryUtility.TestPlanesAABB(frustumPlanes, visibilityRenderer.bounds))
        {
            return true;
        }

        return visibilityPoints == null || visibilityPoints.Length == 0;
    }

    private bool IsWithinDistance(float threshold)
    {
        if (threshold <= 0f)
        {
            return true;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        float sqrThreshold = threshold * threshold;

        if (visibilityPoints != null)
        {
            for (int i = 0; i < visibilityPoints.Length; i++)
            {
                Transform point = visibilityPoints[i];
                if (point != null && (point.position - cameraPosition).sqrMagnitude <= sqrThreshold)
                {
                    return true;
                }
            }
        }

        if (visibilityBounds != null)
        {
            return (visibilityBounds.bounds.ClosestPoint(cameraPosition) - cameraPosition).sqrMagnitude <= sqrThreshold;
        }

        if (visibilityRenderer != null)
        {
            return (visibilityRenderer.bounds.ClosestPoint(cameraPosition) - cameraPosition).sqrMagnitude <= sqrThreshold;
        }

        return true;
    }

    private bool HasVisibleProxyPoint()
    {
        if (visibilityPoints == null || visibilityPoints.Length == 0)
        {
            return false;
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

    private void SetTargetsVisible(bool visible)
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
                    RemoveSharedRendererVote(state.Renderer);
                    continue;
                }

                ApplySharedRendererVote(state.Renderer, state.OriginalEnabled, visible);
            }
        }

        if (!keepMotionRunningWhenHidden && motionControllers != null)
        {
            for (int i = 0; i < motionControllers.Length; i++)
            {
                DuodouGroupMotionController controller = motionControllers[i];
                if (controller != null)
                {
                    controller.enabled = visible;
                }
            }
        }

        ApplyParticleSystems(visible);

        if (logStateChanges)
        {
            Debug.Log($"{name}: camera visibility targets visible = {visible}", this);
        }
    }

    private void RestoreOriginalState()
    {
        UnregisterSharedRenderers();
        RestoreParticleSystems();
    }

    private void ApplyParticleSystems(bool visible)
    {
        for (int i = 0; i < particleSystemStates.Count; i++)
        {
            ParticleSystemState state = particleSystemStates[i];
            ParticleSystem particleSystem = state.ParticleSystem;
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            if (!visible)
            {
                if (stopParticleSystemsWhenHidden)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                if (particleRenderer != null)
                {
                    particleRenderer.enabled = false;
                }

                continue;
            }

            bool underVisibleBudget = maxVisibleParticleSystems <= 0 || i < maxVisibleParticleSystems;
            if (particleRenderer != null)
            {
                particleRenderer.enabled = state.OriginalRendererEnabled && underVisibleBudget;
            }

            if (!underVisibleBudget)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                continue;
            }

            if (throttleParticleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.maxParticles = maxParticlesPerSystem > 0
                    ? Mathf.Min(state.OriginalMaxParticles, maxParticlesPerSystem)
                    : state.OriginalMaxParticles;

                ParticleSystem.EmissionModule emission = particleSystem.emission;
                emission.rateOverTime = ScaleCurve(state.OriginalRateOverTime, emissionRateScale);
            }

            if (!particleSystem.isPlaying)
            {
                particleSystem.Play(true);
            }
        }
    }

    private void RestoreParticleSystems()
    {
        for (int i = 0; i < particleSystemStates.Count; i++)
        {
            ParticleSystemState state = particleSystemStates[i];
            ParticleSystem particleSystem = state.ParticleSystem;
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            main.maxParticles = state.OriginalMaxParticles;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = state.OriginalRateOverTime;

            ParticleSystemRenderer particleRenderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.enabled = state.OriginalRendererEnabled;
            }
        }
    }

    private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float scale)
    {
        scale = Mathf.Clamp01(scale);
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return new ParticleSystem.MinMaxCurve(curve.constant * scale);
            case ParticleSystemCurveMode.TwoConstants:
                return new ParticleSystem.MinMaxCurve(curve.constantMin * scale, curve.constantMax * scale);
            default:
                return curve;
        }
    }

    private void ApplySharedRendererVote(Renderer renderer, bool originalEnabled, bool visible)
    {
        if (renderer == null)
        {
            return;
        }

        if (!sharedRendererStates.TryGetValue(renderer, out RendererVisibilityRecord record))
        {
            record = new RendererVisibilityRecord
            {
                OriginalEnabled = originalEnabled
            };

            sharedRendererStates.Add(renderer, record);
        }

        record.GroupVisibility[this] = visible;
        ApplySharedRendererState(renderer, record);
    }

    private void RemoveSharedRendererVote(Renderer renderer)
    {
        if (renderer == null || !sharedRendererStates.TryGetValue(renderer, out RendererVisibilityRecord record))
        {
            return;
        }

        if (!record.GroupVisibility.Remove(this))
        {
            return;
        }

        if (record.GroupVisibility.Count == 0)
        {
            renderer.enabled = record.OriginalEnabled;
            sharedRendererStates.Remove(renderer);
            return;
        }

        ApplySharedRendererState(renderer, record);
    }

    private void UnregisterSharedRenderers()
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

                RemoveSharedRendererVote(state.Renderer);
            }
        }
    }

    private static void ApplySharedRendererState(Renderer renderer, RendererVisibilityRecord record)
    {
        bool anyGroupWantsVisible = false;
        foreach (bool groupVisible in record.GroupVisibility.Values)
        {
            if (groupVisible)
            {
                anyGroupWantsVisible = true;
                break;
            }
        }

        renderer.enabled = record.OriginalEnabled && anyGroupWantsVisible;
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
