using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Bakes only pipe-material submeshes; no runtime CAD mesh readback is required.</summary>
public static class WaterTreatmentFlowSetup
{
    private const string AssetFolder = "Assets/Generated/WaterTreatmentFlow";
    private static readonly string[] PipeNames = {
        "省煤器与水处理室连接管道", "供水管道与回水管道联通", "补水管道与软化水箱联通", "红色供水管道", "银色暖气管道"
    };
    private sealed class Geometry
    {
        public Vector3[] vertices, normals;
        public int[][] indices;
    }

    [MenuItem("Tools/Boiler/Install Water Treatment Flow")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") throw new InvalidOperationException("Open SampleScene first.");
        if (scene.isDirty) throw new InvalidOperationException("Save existing scene edits before installing water flow.");
        var roots = scene.GetRootGameObjects();
        var pipes = PipeNames.Select(name => roots.FirstOrDefault(root => root.name == name)).ToArray();
        if (pipes.Any(pipe => pipe == null)) throw new InvalidOperationException("One or more target pipe groups are missing.");
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets/Generated", "WaterTreatmentFlow");
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/PipeFlowOverlay.shader");
        if (shader == null) throw new InvalidOperationException("Pipe flow shader was not imported.");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(AssetFolder + "/WaterRipples.mat");
        if (material == null) { material = new Material(shader); material.name = "Water Ripples"; AssetDatabase.CreateAsset(material, AssetFolder + "/WaterRipples.mat"); }

        GameObject visualRoot = roots.FirstOrDefault(root => root.name == "Water Treatment Flow");
        if (visualRoot == null) { visualRoot = new GameObject("Water Treatment Flow"); Undo.RegisterCreatedObjectUndo(visualRoot, "Install pipe water flow"); }
        var controller = visualRoot.GetComponent<WaterTreatmentPipeFlow>();
        if (controller == null) controller = visualRoot.AddComponent<WaterTreatmentPipeFlow>();
        var geometryCache = new Dictionary<Mesh, Geometry>();
        var circuits = new List<WaterTreatmentPipeFlow.Circuit>();
        for (int group = 0; group < pipes.Length; group++)
        {
            GameObject pipe = pipes[group];
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            var sources = new List<Renderer>();
            foreach (var renderer in pipe.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                Mesh mesh = filter.sharedMesh;
                Material[] materials = renderer.sharedMaterials;
                var slots = Enumerable.Range(0, Math.Min(mesh.subMeshCount, materials.Length))
                    .Where(index => WaterTreatmentPipeFlow.IsPipeMaterial(materials[index])).ToArray();
                if (slots.Length == 0) continue;
                Geometry geometry;
                if (!geometryCache.TryGetValue(mesh, out geometry)) { geometry = ReadGeometry(mesh); geometryCache.Add(mesh, geometry); }
                Matrix4x4 matrix = pipe.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                Matrix4x4 normalMatrix = matrix.inverse.transpose;
                bool mirrored = matrix.determinant < 0;
                foreach (var mediumSlots in slots.GroupBy(slot => WaterTreatmentPipeFlow.GetPipeMedium(materials[slot])))
                {
                    // A source vertex may be shared by red and blue submeshes.
                    // Remap it separately for each medium so the colors never blend across pipe sections.
                    var remap = new int[geometry.vertices.Length];
                    var color = new Color32(
                        mediumSlots.Key == WaterTreatmentPipeFlow.PipeMedium.Hot ? (byte)255 : (byte)0,
                        0,
                        mediumSlots.Key == WaterTreatmentPipeFlow.PipeMedium.Cold ? (byte)255 : (byte)0,
                        255);
                    foreach (int slot in mediumSlots)
                    {
                        int[] indices = geometry.indices[slot];
                        for (int index = 0; index < indices.Length; index += 3)
                        {
                            int first = AddVertex(indices[index]);
                            int second = AddVertex(indices[index + 1]);
                            int third = AddVertex(indices[index + 2]);
                            triangles.Add(first);
                            triangles.Add(mirrored ? third : second);
                            triangles.Add(mirrored ? second : third);
                        }
                    }

                    int AddVertex(int index)
                    {
                        if (remap[index] != 0) return remap[index] - 1;
                        int target = vertices.Count;
                        remap[index] = target + 1;
                        vertices.Add(matrix.MultiplyPoint3x4(geometry.vertices[index]));
                        normals.Add(normalMatrix.MultiplyVector(geometry.normals[index]).normalized);
                        colors.Add(color);
                        return target;
                    }
                }
                sources.Add(renderer);
            }
            if (triangles.Count == 0) throw new InvalidOperationException("No pipe surfaces found: " + pipe.name);
            var baked = new Mesh { name = "PipeWaterFlow_" + group, indexFormat = IndexFormat.UInt32 };
            baked.SetVertices(vertices); baked.SetNormals(normals); baked.SetColors(colors); baked.SetTriangles(triangles, 0); baked.RecalculateBounds();
            string meshPath = AssetFolder + "/PipeWaterFlow_" + group + ".asset";
            Mesh stored = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (stored == null) { stored = baked; AssetDatabase.CreateAsset(stored, meshPath); }
            else { EditorUtility.CopySerialized(baked, stored); UnityEngine.Object.DestroyImmediate(baked); EditorUtility.SetDirty(stored); }
            Transform existing = visualRoot.transform.Find("Circuit " + group);
            GameObject overlayObject = existing == null ? new GameObject("Circuit " + group) : existing.gameObject;
            overlayObject.transform.SetParent(visualRoot.transform, false);
            overlayObject.transform.SetPositionAndRotation(pipe.transform.position, pipe.transform.rotation);
            overlayObject.transform.localScale = pipe.transform.lossyScale;
            var meshFilter = overlayObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = overlayObject.AddComponent<MeshFilter>();
            var overlay = overlayObject.GetComponent<MeshRenderer>();
            if (overlay == null) overlay = overlayObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = stored; overlay.sharedMaterial = material;
            overlay.enabled = false;
            overlay.shadowCastingMode = ShadowCastingMode.Off; overlay.receiveShadows = false;
            overlay.lightProbeUsage = LightProbeUsage.Off; overlay.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var circuit = controller.circuits.FirstOrDefault(item => item != null && item.pipeRoot == pipe.transform)
                ?? new WaterTreatmentPipeFlow.Circuit();
            circuit.label = pipe.name;
            circuit.pipeRoot = pipe.transform;
            circuit.overlay = overlay;
            circuit.pipeRenderers = sources.ToArray();
            circuits.Add(circuit);
        }
        controller.circuits = circuits.ToArray();
        controller.legacyArrowRoots = roots.Where(root => root.name.StartsWith("水流箭头", StringComparison.Ordinal)
            || root.name == "水流方向" || root.name == "管道水流特效").ToArray();
        foreach (var arrow in controller.legacyArrowRoots) arrow.SetActive(false);
        controller.RefreshFlowSettings();
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save the pipe flow scene.");
        Debug.Log("[WaterTreatmentFlow] Installed continuous water ripples on five pipe groups; legacy water arrows disabled.");
    }

    private static Geometry ReadGeometry(Mesh mesh)
    {
        using (var snapshot = MeshUtility.AcquireReadOnlyMeshData(mesh))
        {
            var data = snapshot[0];
            var vertices = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            var normals = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            try
            {
                data.GetVertices(vertices);
                if (!mesh.HasVertexAttribute(VertexAttribute.Normal)) throw new InvalidOperationException("Pipe mesh lacks normals: " + mesh.name);
                data.GetNormals(normals);
                var result = new Geometry { vertices = vertices.ToArray(), normals = normals.ToArray(), indices = new int[data.subMeshCount][] };
                for (int slot = 0; slot < data.subMeshCount; slot++)
                {
                    var indices = new NativeArray<int>(data.GetSubMesh(slot).indexCount, Allocator.Temp);
                    try { data.GetIndices(indices, slot, true); result.indices[slot] = indices.ToArray(); }
                    finally { indices.Dispose(); }
                }
                return result;
            }
            finally { vertices.Dispose(); normals.Dispose(); }
        }
    }
}
