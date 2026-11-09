using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Lightweight water visualization; the shader animates five baked pipe overlays.</summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class WaterTreatmentPipeFlow : MonoBehaviour
{
    public enum PipeMedium { Other, Neutral, Hot, Cold }

    [Serializable]
    public sealed class Circuit
    {
        public string label;
        public Transform pipeRoot;
        public MeshRenderer overlay;
        public Renderer[] pipeRenderers;
        [Tooltip("World-axis flow signs. This is a visualization setting, not a measured flow direction.")]
        public Vector3 flowDirection = Vector3.one;
        public float speed = 0.35f;
        [Min(0.05f)] public float rippleSpacing = 0.55f;
        [Range(0f, 1f)] public float strength = 0.8f;
    }

    public Circuit[] circuits = new Circuit[0];
    public GameObject[] legacyArrowRoots = new GameObject[0];
    public bool flowEnabled = true;
    [Range(0.1f, 0.95f)] public float pipeWallAlpha = 0.42f;

    private readonly List<WallState> walls = new List<WallState>();
    private readonly Dictionary<Circuit, HashSet<Material>> circuitWalls = new Dictionary<Circuit, HashSet<Material>>();
    private MaterialPropertyBlock properties;
    private bool initialized;
    private float nextVisibilityCheck;
    private readonly Plane[] frustumPlanes = new Plane[6];

    private sealed class WallState
    {
        public Material material;
        public Color color;
        public PipeMedium medium;
        public float mode, sourceBlend, destinationBlend, zWrite;
        public int queue;
        public string[] keywords;
    }

    public static PipeMedium GetPipeMedium(Material material)
    {
        if (material == null) return PipeMedium.Other;
        string name = material.name.Replace(" (Instance)", "").Trim();
        switch (name)
        {
            case "红":
            case "红 1":
                return PipeMedium.Hot;
            case "蓝色水管材质":
                return PipeMedium.Cold;
            case "银色":
            case "银色 1":
                return PipeMedium.Neutral;
            default:
                return PipeMedium.Other;
        }
    }

    public static bool IsPipeMaterial(Material material) => GetPipeMedium(material) != PipeMedium.Other;

    private void Start()
    {
        // Existing Q/E controllers cache their original colors before this Start executes.
        var seen = new HashSet<Material>();
        foreach (var circuit in circuits)
        {
            if (circuit == null || circuit.pipeRenderers == null) continue;
            var materials = new HashSet<Material>();
            circuitWalls.Add(circuit, materials);
            foreach (var renderer in circuit.pipeRenderers)
            {
                if (renderer == null) continue;
                foreach (var material in renderer.materials)
                {
                    PipeMedium medium = GetPipeMedium(material);
                    if (medium == PipeMedium.Other || !material.HasProperty("_Mode")) continue;
                    materials.Add(material);
                    if (!seen.Add(material)) continue;
                    walls.Add(new WallState {
                        material = material, color = material.color, medium = medium, mode = material.GetFloat("_Mode"),
                        sourceBlend = material.GetFloat("_SrcBlend"), destinationBlend = material.GetFloat("_DstBlend"),
                        zWrite = material.GetFloat("_ZWrite"), queue = material.renderQueue, keywords = material.shaderKeywords
                    });
                }
            }
        }
        initialized = true;
        RefreshFlowSettings();
        UpdateVisibility();
    }

    private void OnEnable()
    {
        if (!initialized) return;
        RefreshFlowSettings();
        UpdateVisibility();
    }

    private void LateUpdate()
    {
        // Run after the existing Q/E handlers and cover any pipe material slots they missed.
        bool restore = Input.GetKeyDown(KeyCode.E);
        bool makeTransparent = Input.GetKeyDown(KeyCode.Q);
        if (restore) RestoreWalls();
        else if (makeTransparent) ApplyTransparentWalls();

        if (!restore && !makeTransparent && Time.unscaledTime < nextVisibilityCheck) return;
        nextVisibilityCheck = Time.unscaledTime + 0.15f;
        UpdateVisibility();
    }

    public void RefreshFlowSettings()
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        foreach (var circuit in circuits)
        {
            if (circuit == null || circuit.overlay == null) continue;
            circuit.overlay.GetPropertyBlock(properties);
            Vector3 direction = circuit.flowDirection.sqrMagnitude > 0.001f ? circuit.flowDirection : Vector3.one;
            properties.SetVector("_FlowVector", new Vector4(direction.x, direction.y, direction.z, 0));
            properties.SetFloat("_Speed", circuit.speed);
            properties.SetFloat("_Spacing", Mathf.Max(0.05f, circuit.rippleSpacing));
            properties.SetFloat("_Opacity", Mathf.Clamp01(circuit.strength));
            circuit.overlay.SetPropertyBlock(properties);
        }
    }

    private void UpdateVisibility()
    {
        foreach (var arrow in legacyArrowRoots)
            if (arrow != null && arrow.activeSelf) arrow.SetActive(false);

        Camera camera = Camera.main;
        if (camera != null) GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);
        foreach (var circuit in circuits)
        {
            if (circuit == null || circuit.overlay == null) continue;
            bool visible = false;
            if (flowEnabled && circuit.pipeRoot != null && circuit.pipeRoot.gameObject.activeInHierarchy)
            {
                circuit.overlay.transform.SetPositionAndRotation(circuit.pipeRoot.position, circuit.pipeRoot.rotation);
                circuit.overlay.transform.localScale = circuit.pipeRoot.lossyScale;
                Bounds bounds = circuit.overlay.bounds;
                // Wake just before the pipe enters the view during a camera move.
                bounds.Expand(2f);
                if ((camera == null || GeometryUtility.TestPlanesAABB(frustumPlanes, bounds)) && HasTransparentWalls(circuit))
                {
                    foreach (var renderer in circuit.pipeRenderers)
                        if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy) { visible = true; break; }
                }
            }
            circuit.overlay.enabled = visible;
        }
    }

    private bool HasTransparentWalls(Circuit circuit)
    {
        if (!circuitWalls.TryGetValue(circuit, out var materials) || materials.Count == 0) return false;
        foreach (var material in materials)
            if (!IsTransparentWall(material)) return false;
        return true;
    }

    private static bool IsTransparentWall(Material material)
    {
        return material != null && material.color.a < 0.999f
            && material.renderQueue >= (int)RenderQueue.Transparent
            && material.GetFloat("_ZWrite") == 0f;
    }

    private void ApplyTransparentWalls()
    {
        foreach (var state in walls)
        {
            var material = state.material;
            if (material == null) continue;
            Color tint = state.color;
            if (state.medium == PipeMedium.Hot)
                tint = Color.Lerp(tint, new Color(1f, 0.34f, 0.28f), 0.28f);
            else if (state.medium == PipeMedium.Cold)
                tint = Color.Lerp(tint, new Color(0.20f, 0.60f, 1f), 0.28f);
            tint.a = pipeWallAlpha;
            material.color = tint;
            material.SetFloat("_Mode", 2);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }

    private void OnDisable()
    {
        foreach (var circuit in circuits)
            if (circuit != null && circuit.overlay != null) circuit.overlay.enabled = false;
    }

    private void RestoreWalls()
    {
        foreach (var state in walls)
        {
            if (state.material == null) continue;
            state.material.color = state.color;
            state.material.SetFloat("_Mode", state.mode);
            state.material.SetFloat("_SrcBlend", state.sourceBlend);
            state.material.SetFloat("_DstBlend", state.destinationBlend);
            state.material.SetFloat("_ZWrite", state.zWrite);
            state.material.renderQueue = state.queue;
            state.material.shaderKeywords = state.keywords;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && initialized) RefreshFlowSettings();
    }
}
