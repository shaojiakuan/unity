using System.Collections.Generic;
using UnityEngine;

public class InteriorRendererVisibilityZone : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;
    public string cameraTag = "MainCamera";

    [Header("Zone")]
    public Collider triggerZone;
    public float checkInterval = 0.15f;

    [Header("Targets")]
    public GameObject[] targetRoots;
    public bool includeChildren = true;
    public bool visibleWhenInside = true;
    public bool hideOnStart = true;

    [Header("Optional")]
    public bool pauseAnimatorsWhenHidden = false;
    public bool stopParticleSystemsWhenHidden = false;
    public bool logStateChanges = false;

    private readonly List<RendererState> rendererStates = new List<RendererState>(256);
    private readonly List<AnimatorState> animatorStates = new List<AnimatorState>(16);
    private readonly List<ParticleState> particleStates = new List<ParticleState>(16);
    private float nextCheckTime;
    private bool currentlyInside;
    private bool hasAppliedState;

    private struct RendererState
    {
        public Renderer renderer;
        public bool originalEnabled;
    }

    private struct AnimatorState
    {
        public Animator animator;
        public bool originalEnabled;
    }

    private struct ParticleState
    {
        public ParticleSystem particleSystem;
        public bool wasPlaying;
    }

    private void Awake()
    {
        if (triggerZone == null)
        {
            triggerZone = GetComponent<Collider>();
        }

        if (triggerZone != null)
        {
            triggerZone.isTrigger = true;
        }

        ResolveCamera();
        CacheTargets();
    }

    private void Start()
    {
        if (hideOnStart)
        {
            SetTargetVisible(!visibleWhenInside);
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

        if (targetCamera == null || triggerZone == null)
        {
            return;
        }

        bool inside = IsCameraInsideZone();
        if (inside == currentlyInside && hasAppliedState)
        {
            return;
        }

        currentlyInside = inside;
        hasAppliedState = true;
        SetTargetVisible(inside == visibleWhenInside);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCameraCollider(other))
        {
            currentlyInside = true;
            hasAppliedState = true;
            SetTargetVisible(visibleWhenInside);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsCameraCollider(other))
        {
            currentlyInside = false;
            hasAppliedState = true;
            SetTargetVisible(!visibleWhenInside);
        }
    }

    private void OnDisable()
    {
        RestoreOriginalState();
    }

    [ContextMenu("Refresh Target Cache")]
    public void CacheTargets()
    {
        rendererStates.Clear();
        animatorStates.Clear();
        particleStates.Clear();

        if (targetRoots == null)
        {
            return;
        }

        for (int i = 0; i < targetRoots.Length; i++)
        {
            GameObject root = targetRoots[i];
            if (root == null)
            {
                continue;
            }

            CacheRenderers(root);
            CacheAnimators(root);
            CacheParticles(root);
        }
    }

    [ContextMenu("Show Targets")]
    public void ShowTargets()
    {
        SetTargetVisible(true);
    }

    [ContextMenu("Hide Targets")]
    public void HideTargets()
    {
        SetTargetVisible(false);
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

    private bool IsCameraInsideZone()
    {
        Vector3 cameraPosition = targetCamera.transform.position;
        return triggerZone.bounds.Contains(cameraPosition);
    }

    private bool IsCameraCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (targetCamera != null && other.transform.IsChildOf(targetCamera.transform))
        {
            return true;
        }

        return other.CompareTag(cameraTag);
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
                renderer = renderer,
                originalEnabled = renderer.enabled
            });
        }
    }

    private void CacheAnimators(GameObject root)
    {
        if (!pauseAnimatorsWhenHidden)
        {
            return;
        }

        Animator[] animators = includeChildren
            ? root.GetComponentsInChildren<Animator>(true)
            : root.GetComponents<Animator>();

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
            {
                continue;
            }

            animatorStates.Add(new AnimatorState
            {
                animator = animator,
                originalEnabled = animator.enabled
            });
        }
    }

    private void CacheParticles(GameObject root)
    {
        if (!stopParticleSystemsWhenHidden)
        {
            return;
        }

        ParticleSystem[] particleSystems = includeChildren
            ? root.GetComponentsInChildren<ParticleSystem>(true)
            : root.GetComponents<ParticleSystem>();

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleStates.Add(new ParticleState
            {
                particleSystem = particleSystem,
                wasPlaying = particleSystem.isPlaying
            });
        }
    }

    private void SetTargetVisible(bool visible)
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.renderer != null)
            {
                if (IsExcludedFromCulling(state.renderer))
                {
                    state.renderer.enabled = true;
                    continue;
                }

                state.renderer.enabled = visible && state.originalEnabled;
            }
        }

        if (pauseAnimatorsWhenHidden)
        {
            for (int i = 0; i < animatorStates.Count; i++)
            {
                AnimatorState state = animatorStates[i];
                if (state.animator != null)
                {
                    state.animator.enabled = visible && state.originalEnabled;
                }
            }
        }

        if (stopParticleSystemsWhenHidden)
        {
            for (int i = 0; i < particleStates.Count; i++)
            {
                ParticleState state = particleStates[i];
                if (state.particleSystem == null)
                {
                    continue;
                }

                if (visible && state.wasPlaying)
                {
                    state.particleSystem.Play(true);
                }
                else if (!visible)
                {
                    state.wasPlaying = state.particleSystem.isPlaying;
                    state.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleStates[i] = state;
                }
            }
        }

        if (logStateChanges)
        {
            Debug.Log($"{name}: interior targets visible = {visible}", this);
        }
    }

    private void RestoreOriginalState()
    {
        for (int i = 0; i < rendererStates.Count; i++)
        {
            RendererState state = rendererStates[i];
            if (state.renderer != null)
            {
                if (IsExcludedFromCulling(state.renderer))
                {
                    state.renderer.enabled = true;
                    continue;
                }

                state.renderer.enabled = state.originalEnabled;
            }
        }

        for (int i = 0; i < animatorStates.Count; i++)
        {
            AnimatorState state = animatorStates[i];
            if (state.animator != null)
            {
                state.animator.enabled = state.originalEnabled;
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
