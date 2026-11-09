using System.Collections.Generic;
using UnityEngine;

public class SceneRendererDistanceCuller : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;

    [Header("Culling")]
    public bool enableCulling = true;
    public float maxVisibleDistance = 180f;
    public float alwaysVisibleDistance = 20f;
    public float boundsPadding = 6f;
    public bool useFrustumCulling = true;
    public LayerMask cullingMask = ~0;

    [Header("Performance")]
    public int checksPerFrame = 500;
    public float rebuildInterval = 8f;

    private readonly List<RendererEntry> renderers = new List<RendererEntry>(8192);
    private readonly Plane[] frustumPlanes = new Plane[6];
    private int nextRendererIndex;
    private float nextRebuildTime;

    private struct RendererEntry
    {
        public Renderer renderer;
        public Bounds bounds;
        public bool originalEnabled;
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        RebuildRendererList();
    }

    private void Update()
    {
        if (!enableCulling || targetCamera == null)
        {
            RestoreRenderers();
            return;
        }

        if (Time.unscaledTime >= nextRebuildTime)
        {
            RebuildRendererList();
        }

        GeometryUtility.CalculateFrustumPlanes(targetCamera, frustumPlanes);

        int checks = Mathf.Min(checksPerFrame, renderers.Count);
        for (int i = 0; i < checks; i++)
        {
            if (renderers.Count == 0)
            {
                return;
            }

            if (nextRendererIndex >= renderers.Count)
            {
                nextRendererIndex = 0;
            }

            RendererEntry entry = renderers[nextRendererIndex];
            nextRendererIndex++;

            Renderer renderer = entry.renderer;
            if (renderer == null || !entry.originalEnabled)
            {
                continue;
            }

            if (IsExcludedFromCulling(renderer))
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                }

                continue;
            }

            bool shouldRender = ShouldRender(entry.bounds);
            if (renderer.enabled != shouldRender)
            {
                renderer.enabled = shouldRender;
            }
        }
    }

    private void OnDisable()
    {
        RestoreRenderers();
    }

    public void RebuildRendererList()
    {
        RestoreRenderers();
        renderers.Clear();
        nextRendererIndex = 0;
        nextRebuildTime = Time.unscaledTime + Mathf.Max(1f, rebuildInterval);

        Renderer[] sceneRenderers = FindObjectsOfType<Renderer>();
        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            Renderer renderer = sceneRenderers[i];
            if (!ShouldManageRenderer(renderer))
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            bounds.Expand(boundsPadding);

            renderers.Add(new RendererEntry
            {
                renderer = renderer,
                bounds = bounds,
                originalEnabled = renderer.enabled
            });
        }
    }

    private bool ShouldManageRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (((1 << renderer.gameObject.layer) & cullingMask.value) == 0)
        {
            return false;
        }

        if (renderer is SkinnedMeshRenderer || renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
        {
            return false;
        }

        if (renderer.GetComponentInParent<Canvas>() != null)
        {
            return false;
        }

        if (IsExcludedFromCulling(renderer))
        {
            return false;
        }

        if (targetCamera != null && renderer.transform.IsChildOf(targetCamera.transform))
        {
            return false;
        }

        return renderer is MeshRenderer;
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

    private bool ShouldRender(Bounds bounds)
    {
        Vector3 cameraPosition = targetCamera.transform.position;
        float sqrDistance = (bounds.ClosestPoint(cameraPosition) - cameraPosition).sqrMagnitude;

        if (sqrDistance <= alwaysVisibleDistance * alwaysVisibleDistance)
        {
            return true;
        }

        if (maxVisibleDistance > 0f && sqrDistance > maxVisibleDistance * maxVisibleDistance)
        {
            return false;
        }

        if (useFrustumCulling && !GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
        {
            return false;
        }

        return true;
    }

    private void RestoreRenderers()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            RendererEntry entry = renderers[i];
            if (entry.renderer != null)
            {
                entry.renderer.enabled = entry.originalEnabled;
            }
        }
    }
}
