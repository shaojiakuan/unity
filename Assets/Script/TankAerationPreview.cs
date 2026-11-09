using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(TankLiquidLevel))]
public sealed class TankAerationPreview : MonoBehaviour
{
    public const string PointName = "WaterAerationPoint";
    public const string MixingPointName = "WaterMixingPoint";
    public Shader bubbleShader;
    public bool IsActive { get; private set; }

    private TankLiquidLevel tank;
    private Renderer water;
    private Material originalWater;
    private Material previewWater;
    private Material bubbleMaterial;
    private Mesh bubbleMesh;
    private GameObject bubbles;
    private bool wasTransparent;

    public void Begin()
    {
        BeginPreview(false);
    }

    public void BeginMixing()
    {
        BeginPreview(true);
    }

    private void BeginPreview(bool mixing)
    {
        if (IsActive || bubbleShader == null) return;
        tank = GetComponent<TankLiquidLevel>();
        water = tank.liquidBody != null ? tank.liquidBody.GetComponent<Renderer>() : null;
        if (water == null || water.sharedMaterial == null || !water.sharedMaterial.HasProperty("_Aeration")) return;
        wasTransparent = tank.IsShellTransparent;
        if (!tank.SetShellTransparent(true)) return;

        var previousDemo = GetComponent<TankTreatmentDemo>();
        if (previousDemo != null) previousDemo.StopDemo();
        if (bubbles == null) CreateBubbles();

        originalWater = water.sharedMaterial;
        previewWater = new Material(originalWater) { renderQueue = 2988 };
        previewWater.SetFloat("_Aeration", mixing ? 0f : 1f);
        previewWater.SetFloat("_Mixing", mixing ? 1f : 0f);
        bubbleMaterial.SetFloat("_Mixing", mixing ? 1f : 0f);
        float mixingStart = Time.time;
        previewWater.SetFloat("_MixingStartTime", mixingStart);
        bubbleMaterial.SetFloat("_MixingStartTime", mixingStart);
        bubbleMaterial.SetColor("_MixingWaterTint", originalWater.GetColor("_Color"));
        previewWater.SetInt("_WaterSrcBlend", (int)BlendMode.SrcAlpha);
        previewWater.SetInt("_WaterDstBlend", (int)BlendMode.OneMinusSrcAlpha);
        previewWater.SetInt("_WaterZWrite", 0);
        water.sharedMaterial = previewWater;
        bubbles.SetActive(water.enabled);
        IsActive = true;
    }

    public void End()
    {
        if (!IsActive) return;
        IsActive = false;
        if (bubbles != null) bubbles.SetActive(false);
        if (water != null && water.sharedMaterial == previewWater) water.sharedMaterial = originalWater;
        if (previewWater != null) Destroy(previewWater);
        if (tank != null)
        {
            tank.SetShellTransparent(wasTransparent);
        }
    }

    private void LateUpdate()
    {
        if (!IsActive) return;
        if (Input.GetKeyDown(KeyCode.E) || !tank.IsShellTransparent)
        {
            End();
            tank.SetShellTransparent(false);
            return;
        }

        // The bubble mesh is parented to the liquid body, so PLC-driven height
        // changes also resize the volume in which bubbles rise.
        if (bubbles.activeSelf != water.enabled) bubbles.SetActive(water.enabled);
    }

    private void OnDisable() => End();

    private void OnDestroy()
    {
        if (bubbles != null) Destroy(bubbles);
        if (bubbleMesh != null) Destroy(bubbleMesh);
        if (bubbleMaterial != null) Destroy(bubbleMaterial);
    }

    private void CreateBubbles()
    {
        const int count = 132;
        var positions = new Vector3[count * 4];
        var corners = new Vector2[count * 4];
        var seeds = new List<Vector4>(count * 4);
        var indices = new int[count * 6];
        var random = new System.Random(341);
        for (int i = 0; i < count; i++)
        {
            int vertex = i * 4;
            int triangle = i * 6;
            float x = ((i % 5) - 2) * 0.15f + ((float)random.NextDouble() - 0.5f) * 0.09f;
            float z = ((i / 5 % 4) - 1.5f) * 0.18f + ((float)random.NextDouble() - 0.5f) * 0.09f;
            var seed = new Vector4(x, z, (float)random.NextDouble(), (float)random.NextDouble());
            for (int j = 0; j < 4; j++) seeds.Add(seed);
            corners[vertex] = new Vector2(-1f, -1f);
            corners[vertex + 1] = new Vector2(1f, -1f);
            corners[vertex + 2] = new Vector2(1f, 1f);
            corners[vertex + 3] = new Vector2(-1f, 1f);
            indices[triangle] = vertex;
            indices[triangle + 1] = vertex + 1;
            indices[triangle + 2] = vertex + 2;
            indices[triangle + 3] = vertex;
            indices[triangle + 4] = vertex + 2;
            indices[triangle + 5] = vertex + 3;
        }

        bubbleMesh = new Mesh { name = "Aeration bubbles", vertices = positions, uv = corners, triangles = indices };
        bubbleMesh.SetUVs(1, seeds);
        bubbleMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1.16f);
        bubbleMaterial = new Material(bubbleShader);
        bubbles = new GameObject("Aeration bubbles");
        bubbles.transform.SetParent(tank.liquidBody, false);
        bubbles.AddComponent<MeshFilter>().sharedMesh = bubbleMesh;
        var renderer = bubbles.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = bubbleMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}
