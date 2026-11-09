using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneRendererDiagnostics
{
    private class RendererInfo
    {
        public Renderer Renderer;
        public Mesh Mesh;
        public int Triangles;
        public int Vertices;
        public int MaterialCount;
        public bool CastsShadows;
        public bool IsStatic;
        public string Path;
    }

    [MenuItem("Tools/Performance/Export Scene Renderer Report")]
    public static void ExportSceneRendererReport()
    {
        RendererInfo[] infos = Object.FindObjectsOfType<Renderer>()
            .Where(IsSceneRenderer)
            .Select(CreateInfo)
            .Where(info => info != null)
            .OrderByDescending(info => info.Triangles)
            .ToArray();

        int totalRenderers = infos.Length;
        int meshRenderers = infos.Count(info => info.Renderer is MeshRenderer);
        int skinnedMeshRenderers = infos.Count(info => info.Renderer is SkinnedMeshRenderer);
        int shadowCasters = infos.Count(info => info.CastsShadows);
        int staticRenderers = infos.Count(info => info.IsStatic);
        int totalTriangles = infos.Sum(info => info.Triangles);
        int totalVertices = infos.Sum(info => info.Vertices);
        int uniqueMeshes = infos.Where(info => info.Mesh != null).Select(info => info.Mesh).Distinct().Count();
        int uniqueMaterials = infos
            .SelectMany(info => info.Renderer.sharedMaterials)
            .Where(material => material != null)
            .Distinct()
            .Count();

        string reportPath = "Assets/Editor/SceneRendererReport.csv";
        StringBuilder builder = new StringBuilder(1024 * 512);

        builder.AppendLine("Summary,Value");
        builder.AppendLine($"Scene,{Escape(SceneManager.GetActiveScene().path)}");
        builder.AppendLine($"Total Renderers,{totalRenderers}");
        builder.AppendLine($"MeshRenderers,{meshRenderers}");
        builder.AppendLine($"SkinnedMeshRenderers,{skinnedMeshRenderers}");
        builder.AppendLine($"Static Renderers,{staticRenderers}");
        builder.AppendLine($"Shadow Casters,{shadowCasters}");
        builder.AppendLine($"Unique Meshes,{uniqueMeshes}");
        builder.AppendLine($"Unique Materials,{uniqueMaterials}");
        builder.AppendLine($"Total Triangles,{totalTriangles}");
        builder.AppendLine($"Total Vertices,{totalVertices}");
        builder.AppendLine();
        builder.AppendLine("Rank,Triangles,Vertices,Materials,CastsShadows,Static,RendererType,Mesh,Path");

        for (int i = 0; i < Mathf.Min(500, infos.Length); i++)
        {
            RendererInfo info = infos[i];
            builder.AppendLine(string.Join(",",
                i + 1,
                info.Triangles,
                info.Vertices,
                info.MaterialCount,
                info.CastsShadows ? "Yes" : "No",
                info.IsStatic ? "Yes" : "No",
                Escape(info.Renderer.GetType().Name),
                Escape(info.Mesh != null ? info.Mesh.name : string.Empty),
                Escape(info.Path)));
        }

        File.WriteAllText(reportPath, builder.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        Debug.Log($"Scene renderer report exported: {reportPath}\nRenderers: {totalRenderers}, Triangles: {totalTriangles}, Materials: {uniqueMaterials}");
    }

    [MenuItem("Tools/Performance/Select Top Triangle Renderers")]
    public static void SelectTopTriangleRenderers()
    {
        Object[] selection = Object.FindObjectsOfType<Renderer>()
            .Where(IsSceneRenderer)
            .Select(CreateInfo)
            .Where(info => info != null)
            .OrderByDescending(info => info.Triangles)
            .Take(50)
            .Select(info => info.Renderer.gameObject)
            .Cast<Object>()
            .ToArray();

        Selection.objects = selection;
        Debug.Log($"Selected top {selection.Length} triangle-heavy renderers.");
    }

    [MenuItem("Tools/Performance/Select Shadow Casters")]
    public static void SelectShadowCasters()
    {
        Object[] selection = Object.FindObjectsOfType<Renderer>()
            .Where(IsSceneRenderer)
            .Where(renderer => renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
            .Select(renderer => renderer.gameObject)
            .Cast<Object>()
            .Take(500)
            .ToArray();

        Selection.objects = selection;
        Debug.Log($"Selected {selection.Length} shadow casting renderers.");
    }

    private static bool IsSceneRenderer(Renderer renderer)
    {
        return renderer != null
            && renderer.gameObject.scene.IsValid()
            && renderer.gameObject.scene.isLoaded
            && !(renderer is ParticleSystemRenderer)
            && renderer.GetComponentInParent<Canvas>() == null;
    }

    private static RendererInfo CreateInfo(Renderer renderer)
    {
        Mesh mesh = GetMesh(renderer);
        int triangles = mesh != null ? mesh.triangles.Length / 3 : 0;
        int vertices = mesh != null ? mesh.vertexCount : 0;

        return new RendererInfo
        {
            Renderer = renderer,
            Mesh = mesh,
            Triangles = triangles,
            Vertices = vertices,
            MaterialCount = renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0,
            CastsShadows = renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off,
            IsStatic = renderer.gameObject.isStatic,
            Path = GetPath(renderer.transform)
        };
    }

    private static Mesh GetMesh(Renderer renderer)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter != null)
        {
            return filter.sharedMesh;
        }

        SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
        return skinned != null ? skinned.sharedMesh : null;
    }

    private static string GetPath(Transform transform)
    {
        Stack<string> names = new Stack<string>();
        Transform current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names.ToArray());
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
