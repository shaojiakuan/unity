using System.Collections.Generic;
using UnityEngine;

public class BucketBiomassDropLimiter : MonoBehaviour
{
    public enum LimitMode
    {
        RendererOnly,
        SetActive
    }

    [Header("Search")]
    public string materialContainerName = "生物质Drop";
    public bool includeInactive = true;
    public bool applyOnStart = true;

    [Header("Limit")]
    public int maxVisibleDropsPerContainer = 18;
    public LimitMode limitMode = LimitMode.SetActive;

    [Header("Renderer Optimization")]
    public bool disableShadows = true;
    public bool disableMotionVectors = true;
    public bool disableDynamicOcclusion = false;

    [Header("Runtime")]
    public bool refreshDuringPlay = true;
    public float refreshInterval = 1.0f;
    public int containersPerRefresh = 2;
    public bool rebuildCacheDuringPlay = false;
    public float cacheRebuildInterval = 10.0f;

    [Header("Debug")]
    public bool logResult = false;

    private readonly List<Transform> materialContainers = new List<Transform>(256);
    private float nextRefreshTime;
    private float nextCacheRebuildTime;
    private int nextContainerIndex;

    private void Start()
    {
        RebuildContainerCache();

        if (applyOnStart)
        {
            ApplyLimitToAllCachedContainers();
        }
    }

    private void Update()
    {
        if (!refreshDuringPlay || Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.2f, refreshInterval);

        if (rebuildCacheDuringPlay && Time.unscaledTime >= nextCacheRebuildTime)
        {
            RebuildContainerCache();
        }

        ApplyLimitToContainerBatch();
    }

    [ContextMenu("Apply Biomass Drop Limit")]
    public void ApplyLimit()
    {
        RebuildContainerCache();
        ApplyLimitToAllCachedContainers();
    }

    [ContextMenu("Rebuild Container Cache")]
    public void RebuildContainerCache()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>(includeInactive);
        materialContainers.Clear();
        nextContainerIndex = 0;
        nextCacheRebuildTime = Time.unscaledTime + Mathf.Max(1f, cacheRebuildInterval);

        for (int i = 0; i < allChildren.Length; i++)
        {
            Transform child = allChildren[i];
            if (child == null || child == transform)
            {
                continue;
            }

            if (!IsMaterialContainer(child))
            {
                continue;
            }

            materialContainers.Add(child);
        }

        if (logResult)
        {
            Debug.Log($"{name}: cached biomass containers={materialContainers.Count}", this);
        }
    }

    private bool IsMaterialContainer(Transform target)
    {
        return !string.IsNullOrWhiteSpace(materialContainerName)
            && target.name.Contains(materialContainerName)
            && !target.name.Contains("(Clone)");
    }

    private void ApplyLimitToContainer(Transform container, ref int visibleCount, ref int hiddenCount)
    {
        int maxVisible = Mathf.Max(0, maxVisibleDropsPerContainer);

        for (int i = 0; i < container.childCount; i++)
        {
            Transform drop = container.GetChild(i);
            if (drop == null)
            {
                continue;
            }

            bool shouldShow = i < maxVisible;
            ApplyDropVisible(drop.gameObject, shouldShow);

            if (shouldShow)
            {
                visibleCount++;
                OptimizeRenderers(drop.gameObject);
            }
            else
            {
                hiddenCount++;
            }
        }
    }

    private void ApplyLimitToAllCachedContainers()
    {
        int visibleCount = 0;
        int hiddenCount = 0;

        for (int i = 0; i < materialContainers.Count; i++)
        {
            Transform container = materialContainers[i];
            if (container != null)
            {
                ApplyLimitToContainer(container, ref visibleCount, ref hiddenCount);
            }
        }

        if (logResult)
        {
            Debug.Log($"{name}: biomass containers={materialContainers.Count}, visible drops={visibleCount}, hidden drops={hiddenCount}", this);
        }
    }

    private void ApplyLimitToContainerBatch()
    {
        if (materialContainers.Count == 0)
        {
            return;
        }

        int visibleCount = 0;
        int hiddenCount = 0;
        int count = Mathf.Min(Mathf.Max(1, containersPerRefresh), materialContainers.Count);

        for (int i = 0; i < count; i++)
        {
            if (nextContainerIndex >= materialContainers.Count)
            {
                nextContainerIndex = 0;
            }

            Transform container = materialContainers[nextContainerIndex];
            nextContainerIndex++;

            if (container != null)
            {
                ApplyLimitToContainer(container, ref visibleCount, ref hiddenCount);
            }
        }
    }

    private void ApplyDropVisible(GameObject drop, bool visible)
    {
        if (limitMode == LimitMode.SetActive)
        {
            if (drop.activeSelf != visible)
            {
                drop.SetActive(visible);
            }

            return;
        }

        Renderer renderer = drop.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
            return;
        }

        Renderer[] childRenderers = drop.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i] != null)
            {
                childRenderers[i].enabled = visible;
            }
        }
    }

    private void OptimizeRenderers(GameObject drop)
    {
        Renderer renderer = drop.GetComponent<Renderer>();
        if (renderer != null)
        {
            OptimizeRenderer(renderer);
            return;
        }

        Renderer[] childRenderers = drop.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
        {
            OptimizeRenderer(childRenderers[i]);
        }
    }

    private void OptimizeRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        if (disableShadows)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (disableMotionVectors)
        {
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        if (disableDynamicOcclusion)
        {
            renderer.allowOcclusionWhenDynamic = false;
        }
    }
}
