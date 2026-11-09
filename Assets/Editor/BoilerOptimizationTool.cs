using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class BoilerOptimizationTool : EditorWindow
{
    private GameObject boilerRoot;
    private bool includeInactive = true;
    private float smallRendererBoundsSize = 2.5f;
    private int smallRendererTriangleLimit = 2500;
    private bool disableSmallRendererShadows = true;
    private bool disableReceiveShadows = true;
    private bool disableMotionVectors = true;
    private bool disableProbeUsage = false;
    private float smallComponentShowDistance = 24f;
    private float smallComponentHideDistance = 34f;
    private int maxSmallComponentTargets = 5000;

    private int rendererCount;
    private int particleSystemCount;
    private int shadowCasterCount;
    private int materialSlotCount;
    private int uniqueMaterialCount;
    private long triangleCount;
    private long vertexCount;

    [MenuItem("Tools/Boiler Optimization Tool")]
    private static void Open()
    {
        GetWindow<BoilerOptimizationTool>("Boiler Optimizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Boiler Root", EditorStyles.boldLabel);
        boilerRoot = (GameObject)EditorGUILayout.ObjectField("Root", boilerRoot, typeof(GameObject), true);
        includeInactive = EditorGUILayout.Toggle("Include Inactive", includeInactive);

        if (Selection.activeGameObject != null && GUILayout.Button("Use Selected Object"))
        {
            boilerRoot = Selection.activeGameObject;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Small Renderer Settings", EditorStyles.boldLabel);
        smallRendererBoundsSize = EditorGUILayout.FloatField("Bounds Size Limit", smallRendererBoundsSize);
        smallRendererTriangleLimit = EditorGUILayout.IntField("Triangle Limit", smallRendererTriangleLimit);
        disableSmallRendererShadows = EditorGUILayout.Toggle("Disable Cast Shadows", disableSmallRendererShadows);
        disableReceiveShadows = EditorGUILayout.Toggle("Disable Receive Shadows", disableReceiveShadows);
        disableMotionVectors = EditorGUILayout.Toggle("Disable Motion Vectors", disableMotionVectors);
        disableProbeUsage = EditorGUILayout.Toggle("Disable Probe Usage", disableProbeUsage);
        smallComponentShowDistance = EditorGUILayout.FloatField("Small Detail Show Distance", smallComponentShowDistance);
        smallComponentHideDistance = EditorGUILayout.FloatField("Small Detail Hide Distance", smallComponentHideDistance);
        maxSmallComponentTargets = EditorGUILayout.IntField("Max Small Detail Targets", maxSmallComponentTargets);

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(boilerRoot == null))
        {
            if (GUILayout.Button("Analyze Boiler Root"))
            {
                Analyze();
            }

            if (GUILayout.Button("Apply Small Renderer Optimization"))
            {
                ApplySmallRendererOptimization();
                Analyze();
            }

            if (GUILayout.Button("Fix Redundant Material Slots"))
            {
                FixRedundantMaterialSlots();
                Analyze();
            }

            if (GUILayout.Button("Select Small Component Candidates"))
            {
                SelectSmallComponentCandidates();
            }

            if (GUILayout.Button("Export Small Component Candidates CSV"))
            {
                ExportSmallComponentCandidates();
            }

            if (GUILayout.Button("Add Small Component Visibility Group"))
            {
                AddSmallComponentVisibilityGroup();
            }

            if (GUILayout.Button("Add Distance Visibility Groups"))
            {
                AddDistanceVisibilityGroups();
            }

            if (GUILayout.Button("Split Visibility Groups Into Child Objects"))
            {
                SplitVisibilityGroupsIntoChildObjects();
            }

            if (GUILayout.Button("Restore Smoke Box / Structure Renderers"))
            {
                RestoreStructureRenderers();
                RemoveStructureRootsFromVisibilityGroups();
                Analyze();
            }

            if (GUILayout.Button("Fix Smoke Box Culling Stability"))
            {
                FixStructureCullingStability();
                Analyze();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Last Analysis", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Renderers", rendererCount.ToString());
        EditorGUILayout.LabelField("Particle Systems", particleSystemCount.ToString());
        EditorGUILayout.LabelField("Shadow Casters", shadowCasterCount.ToString());
        EditorGUILayout.LabelField("Material Slots", materialSlotCount.ToString());
        EditorGUILayout.LabelField("Unique Materials", uniqueMaterialCount.ToString());
        EditorGUILayout.LabelField("Triangles", triangleCount.ToString());
        EditorGUILayout.LabelField("Vertices", vertexCount.ToString());

        EditorGUILayout.HelpBox(
            "Recommended flow: select the boiler parent, analyze it, apply small renderer optimization, then add visibility groups. Review the added CameraVisibilityProxyGroup components before play mode.",
            MessageType.Info);
    }

    private void Analyze()
    {
        rendererCount = 0;
        particleSystemCount = 0;
        shadowCasterCount = 0;
        materialSlotCount = 0;
        uniqueMaterialCount = 0;
        triangleCount = 0;
        vertexCount = 0;

        if (boilerRoot == null)
        {
            return;
        }

        HashSet<Material> uniqueMaterials = new HashSet<Material>();
        Renderer[] renderers = boilerRoot.GetComponentsInChildren<Renderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            rendererCount++;
            if (renderer.shadowCastingMode != ShadowCastingMode.Off)
            {
                shadowCasterCount++;
            }

            Material[] materials = renderer.sharedMaterials;
            materialSlotCount += materials.Length;
            for (int m = 0; m < materials.Length; m++)
            {
                if (materials[m] != null)
                {
                    uniqueMaterials.Add(materials[m]);
                }
            }

            Mesh mesh = GetRendererMesh(renderer);
            if (mesh != null)
            {
                triangleCount += GetTriangleCount(mesh);
                vertexCount += mesh.vertexCount;
            }
        }

        particleSystemCount = boilerRoot.GetComponentsInChildren<ParticleSystem>(includeInactive).Length;
        uniqueMaterialCount = uniqueMaterials.Count;
        Repaint();
    }

    private void ApplySmallRendererOptimization()
    {
        if (boilerRoot == null)
        {
            return;
        }

        Renderer[] renderers = boilerRoot.GetComponentsInChildren<Renderer>(includeInactive);
        int changed = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Optimize boiler small renderers");

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                continue;
            }

            Mesh mesh = GetRendererMesh(renderer);
            int triangles = mesh != null ? GetTriangleCount(mesh) : 0;
            bool isSmallByBounds = renderer.bounds.size.magnitude <= smallRendererBoundsSize;
            bool isSmallByMesh = triangles > 0 && triangles <= smallRendererTriangleLimit;
            bool isDetailByName = IsDetailName(renderer.name);

            if (!isSmallByBounds && !isSmallByMesh && !isDetailByName)
            {
                continue;
            }

            Undo.RecordObject(renderer, "Optimize renderer");

            if (disableSmallRendererShadows)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            if (disableReceiveShadows)
            {
                renderer.receiveShadows = false;
            }

            if (disableMotionVectors)
            {
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }

            if (disableProbeUsage)
            {
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            EditorUtility.SetDirty(renderer);
            changed++;
        }

        Debug.Log($"BoilerOptimizationTool optimized {changed} small/detail renderers under {boilerRoot.name}.", boilerRoot);
    }

    private void FixRedundantMaterialSlots()
    {
        if (boilerRoot == null)
        {
            return;
        }

        Renderer[] renderers = boilerRoot.GetComponentsInChildren<Renderer>(includeInactive);
        int changedRenderers = 0;
        int removedSlots = 0;
        string reportPath = "Assets/Editor/RedundantMaterialSlotsReport.csv";
        StringBuilder builder = new StringBuilder(1024 * 64);
        builder.AppendLine("RemovedSlots,OldMaterialSlots,NewMaterialSlots,SubMeshes,RendererType,Mesh,Path");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Fix redundant material slots");

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                continue;
            }

            Mesh mesh = GetRendererMesh(renderer);
            if (mesh == null || mesh.subMeshCount <= 0)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length <= mesh.subMeshCount)
            {
                continue;
            }

            int oldSlotCount = materials.Length;
            int newSlotCount = mesh.subMeshCount;
            Material[] trimmedMaterials = new Material[newSlotCount];
            for (int m = 0; m < newSlotCount; m++)
            {
                trimmedMaterials[m] = materials[m];
            }

            Undo.RecordObject(renderer, "Fix redundant material slots");
            renderer.sharedMaterials = trimmedMaterials;
            EditorUtility.SetDirty(renderer);

            int removed = oldSlotCount - newSlotCount;
            changedRenderers++;
            removedSlots += removed;

            builder.AppendLine(string.Join(",",
                removed,
                oldSlotCount,
                newSlotCount,
                mesh.subMeshCount,
                Escape(renderer.GetType().Name),
                Escape(mesh.name),
                Escape(GetPath(renderer.transform))));
        }

        File.WriteAllText(reportPath, builder.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"BoilerOptimizationTool fixed {changedRenderers} renderers and removed {removedSlots} redundant material slots. Report: {reportPath}", boilerRoot);
    }

    private void AddDistanceVisibilityGroups()
    {
        if (boilerRoot == null)
        {
            return;
        }

        CreateVisibilityGroup(
            "Boiler Biomass Visibility",
            new[] { "生物质Drop", "生物质阶段", "燃料", "料斗", "多斗", "biomass", "fuel", "bucket" },
            32f,
            42f,
            true);

        CreateVisibilityGroup(
            "Boiler Chain Visibility",
            new[] { "链条", "链", "上料", "除渣", "皮带", "chain", "conveyer", "conveyor" },
            45f,
            60f,
            true);

        CreateVisibilityGroup(
            "Boiler Particle Visibility",
            new[] { "燃烧", "火焰", "烟气", "烟雾", "flame", "smoke", "fire", "particle" },
            new[] { "烟箱", "后烟箱", "前烟箱" },
            38f,
            52f,
            true,
            true);

        EditorUtility.SetDirty(boilerRoot);
        Debug.Log($"BoilerOptimizationTool added visibility groups to {boilerRoot.name}. Review their Target Roots before play mode.", boilerRoot);
    }

    private void SelectSmallComponentCandidates()
    {
        List<SmallComponentCandidate> candidates = FindSmallComponentCandidates();
        int selectionCount = Mathf.Min(candidates.Count, 1000);
        Object[] selection = new Object[selectionCount];

        for (int i = 0; i < selectionCount; i++)
        {
            selection[i] = candidates[i].Renderer.gameObject;
        }

        Selection.objects = selection;
        Debug.Log($"BoilerOptimizationTool selected {selectionCount} small/detail candidates. Total candidates: {candidates.Count}.", boilerRoot);
    }

    private void ExportSmallComponentCandidates()
    {
        if (boilerRoot == null)
        {
            return;
        }

        List<SmallComponentCandidate> candidates = FindSmallComponentCandidates();
        string reportPath = "Assets/Editor/SmallComponentCandidates.csv";
        StringBuilder builder = new StringBuilder(1024 * 512);

        builder.AppendLine("Rank,Triangles,Vertices,BoundsMagnitude,Materials,CastsShadows,ReceiveShadows,MotionVectors,RendererType,Mesh,Path");
        for (int i = 0; i < candidates.Count; i++)
        {
            SmallComponentCandidate candidate = candidates[i];
            Renderer renderer = candidate.Renderer;
            Mesh mesh = candidate.Mesh;

            builder.AppendLine(string.Join(",",
                i + 1,
                candidate.Triangles,
                mesh != null ? mesh.vertexCount : 0,
                candidate.BoundsMagnitude.ToString("F3"),
                renderer.sharedMaterials != null ? renderer.sharedMaterials.Length : 0,
                renderer.shadowCastingMode != ShadowCastingMode.Off ? "Yes" : "No",
                renderer.receiveShadows ? "Yes" : "No",
                renderer.motionVectorGenerationMode.ToString(),
                Escape(renderer.GetType().Name),
                Escape(mesh != null ? mesh.name : string.Empty),
                Escape(GetPath(renderer.transform))));
        }

        File.WriteAllText(reportPath, builder.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"BoilerOptimizationTool exported {candidates.Count} small/detail candidates: {reportPath}", boilerRoot);
    }

    private void AddSmallComponentVisibilityGroup()
    {
        if (boilerRoot == null)
        {
            return;
        }

        List<SmallComponentCandidate> candidates = FindSmallComponentCandidates();
        if (candidates.Count == 0)
        {
            Debug.LogWarning("BoilerOptimizationTool found no small/detail candidates.", boilerRoot);
            return;
        }

        int targetCount = Mathf.Min(candidates.Count, Mathf.Max(1, maxSmallComponentTargets));
        GameObject[] targets = new GameObject[targetCount];
        HashSet<GameObject> usedTargets = new HashSet<GameObject>();
        List<GameObject> uniqueTargets = new List<GameObject>(targetCount);

        for (int i = 0; i < candidates.Count && uniqueTargets.Count < targetCount; i++)
        {
            GameObject target = candidates[i].Renderer.gameObject;
            if (target != null && usedTargets.Add(target))
            {
                uniqueTargets.Add(target);
            }
        }

        targets = uniqueTargets.ToArray();

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add small component visibility group");

        CameraVisibilityProxyGroup group = Undo.AddComponent<CameraVisibilityProxyGroup>(boilerRoot);
        group.targetRoots = targets;
        group.showDistance = smallComponentShowDistance;
        group.hideDistance = Mathf.Max(smallComponentShowDistance, smallComponentHideDistance);
        group.requireCameraFrustum = false;
        group.hideOnStart = true;
        group.checkInterval = 0.2f;
        group.keepMotionRunningWhenHidden = true;
        group.logStateChanges = false;
        group.visibilityPoints = new[] { boilerRoot.transform };
        group.name = "Boiler Small Detail Visibility";
        group.CacheTargets();

        EditorUtility.SetDirty(group);
        EditorUtility.SetDirty(boilerRoot);

        Debug.Log($"BoilerOptimizationTool added small/detail visibility group. Targets: {targets.Length}, candidates found: {candidates.Count}. Review Target Roots before play mode.", boilerRoot);
    }

    private void SplitVisibilityGroupsIntoChildObjects()
    {
        if (boilerRoot == null)
        {
            return;
        }

        CameraVisibilityProxyGroup[] groups = boilerRoot.GetComponents<CameraVisibilityProxyGroup>();
        if (groups.Length <= 1)
        {
            Debug.Log($"BoilerOptimizationTool found {groups.Length} CameraVisibilityProxyGroup on {boilerRoot.name}. Nothing to split.", boilerRoot);
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Split visibility groups into child objects");

        int created = 0;
        for (int i = 0; i < groups.Length; i++)
        {
            CameraVisibilityProxyGroup source = groups[i];
            if (source == null)
            {
                continue;
            }

            GameObject child = new GameObject(GetVisibilityGroupObjectName(source, i));
            Undo.RegisterCreatedObjectUndo(child, "Create visibility group child");
            child.transform.SetParent(boilerRoot.transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            CameraVisibilityProxyGroup target = Undo.AddComponent<CameraVisibilityProxyGroup>(child);
            CopyVisibilityGroupSettings(source, target);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(child);

            Undo.DestroyObjectImmediate(source);
            created++;
        }

        EditorUtility.SetDirty(boilerRoot);
        Debug.Log($"BoilerOptimizationTool split {created} CameraVisibilityProxyGroup components from {boilerRoot.name} into child objects.", boilerRoot);
    }

    private void RestoreStructureRenderers()
    {
        if (boilerRoot == null)
        {
            return;
        }

        Transform[] transforms = boilerRoot.GetComponentsInChildren<Transform>(true);
        int restored = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Restore boiler structure renderers");

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || !IsAlwaysVisibleStructure(current))
            {
                continue;
            }

            Renderer[] renderers = current.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                {
                    continue;
                }

                Undo.RecordObject(renderer, "Restore renderer");
                renderer.enabled = true;
                EditorUtility.SetDirty(renderer);
                restored++;
            }
        }

        Debug.Log($"BoilerOptimizationTool restored {restored} structure renderers under {boilerRoot.name}.", boilerRoot);
    }

    private void FixStructureCullingStability()
    {
        if (boilerRoot == null)
        {
            return;
        }

        Transform[] transforms = boilerRoot.GetComponentsInChildren<Transform>(true);
        int rendererChanges = 0;
        int meshBoundsChanges = 0;
        int exclusionMarkersAdded = 0;
        int staticFlagsChanged = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Fix smoke box culling stability");

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || !IsAlwaysVisibleStructure(current))
            {
                continue;
            }

            if (current.GetComponent<RendererCullingExclusion>() == null)
            {
                RendererCullingExclusion marker = Undo.AddComponent<RendererCullingExclusion>(current.gameObject);
                EditorUtility.SetDirty(marker);
                exclusionMarkersAdded++;
            }

            staticFlagsChanged += ClearOcclusionStaticFlags(current.gameObject);

            Renderer[] renderers = current.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                {
                    continue;
                }

                Undo.RecordObject(renderer, "Fix renderer culling");
                renderer.enabled = true;
                renderer.allowOcclusionWhenDynamic = false;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                EditorUtility.SetDirty(renderer);
                rendererChanges++;

                staticFlagsChanged += ClearOcclusionStaticFlags(renderer.gameObject);
            }

            MeshFilter[] meshFilters = current.GetComponentsInChildren<MeshFilter>(true);
            for (int m = 0; m < meshFilters.Length; m++)
            {
                MeshFilter meshFilter = meshFilters[m];
                Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (mesh == null || !mesh.isReadable)
                {
                    continue;
                }

                Undo.RecordObject(mesh, "Recalculate mesh bounds");
                mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);
                meshBoundsChanges++;
            }
        }

        RemoveStructureRootsFromVisibilityGroups();
        AssetDatabase.SaveAssets();

        Debug.Log($"BoilerOptimizationTool fixed smoke-box culling stability. Renderers updated: {rendererChanges}, mesh bounds recalculated: {meshBoundsChanges}, culling exclusion markers added: {exclusionMarkersAdded}, static flags changed: {staticFlagsChanged}.", boilerRoot);
    }

    private void RemoveStructureRootsFromVisibilityGroups()
    {
        if (boilerRoot == null)
        {
            return;
        }

        CameraVisibilityProxyGroup[] groups = boilerRoot.GetComponents<CameraVisibilityProxyGroup>();
        int removed = 0;

        for (int i = 0; i < groups.Length; i++)
        {
            CameraVisibilityProxyGroup group = groups[i];
            if (group == null || group.targetRoots == null || group.targetRoots.Length == 0)
            {
                continue;
            }

            List<GameObject> filteredTargets = new List<GameObject>(group.targetRoots.Length);
            for (int t = 0; t < group.targetRoots.Length; t++)
            {
                GameObject target = group.targetRoots[t];
                if (target == null)
                {
                    continue;
                }

                if (IsAlwaysVisibleStructure(target.transform))
                {
                    removed++;
                    continue;
                }

                filteredTargets.Add(target);
            }

            if (filteredTargets.Count != group.targetRoots.Length)
            {
                Undo.RecordObject(group, "Remove structure roots from visibility group");
                group.targetRoots = filteredTargets.ToArray();
                group.CacheTargets();
                EditorUtility.SetDirty(group);
            }
        }

        Debug.Log($"BoilerOptimizationTool removed {removed} smoke-box/structure roots from visibility groups.", boilerRoot);
    }

    private void CreateVisibilityGroup(string componentName, string[] keywords, float showDistance, float hideDistance, bool requireFrustum, bool includeParticles = false)
    {
        CreateVisibilityGroup(componentName, keywords, null, showDistance, hideDistance, requireFrustum, includeParticles);
    }

    private void CreateVisibilityGroup(string componentName, string[] keywords, string[] excludedKeywords, float showDistance, float hideDistance, bool requireFrustum, bool includeParticles = false)
    {
        GameObject[] targets = FindTargetRoots(keywords, excludedKeywords, includeParticles);
        if (targets.Length == 0)
        {
            Debug.LogWarning($"BoilerOptimizationTool found no targets for {componentName}.", boilerRoot);
            return;
        }

        CameraVisibilityProxyGroup group = boilerRoot.AddComponent<CameraVisibilityProxyGroup>();
        Undo.RegisterCreatedObjectUndo(group, $"Add {componentName}");
        group.targetRoots = targets;
        group.showDistance = showDistance;
        group.hideDistance = hideDistance;
        group.requireCameraFrustum = requireFrustum;
        group.hideOnStart = true;
        group.checkInterval = 0.15f;
        group.keepMotionRunningWhenHidden = true;
        group.logStateChanges = false;
        group.visibilityPoints = new[] { boilerRoot.transform };
        group.name = componentName;

        EditorUtility.SetDirty(group);
    }

    private GameObject[] FindTargetRoots(string[] keywords, string[] excludedKeywords, bool includeParticles)
    {
        List<GameObject> targets = new List<GameObject>();
        Transform[] transforms = boilerRoot.GetComponentsInChildren<Transform>(includeInactive);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current == boilerRoot.transform)
            {
                continue;
            }

            if (NameContainsAny(current.name, excludedKeywords) || IsAlwaysVisibleStructure(current))
            {
                continue;
            }

            bool match = NameContainsAny(current.name, keywords);
            if (!match && includeParticles)
            {
                match = current.GetComponent<ParticleSystem>() != null || current.GetComponent<ParticleSystemRenderer>() != null;
            }

            if (!match || IsChildOfExistingTarget(current, targets))
            {
                continue;
            }

            targets.Add(current.gameObject);
        }

        return targets.ToArray();
    }

    private static bool IsChildOfExistingTarget(Transform current, List<GameObject> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target != null && current.IsChildOf(target.transform))
            {
                return true;
            }
        }

        return false;
    }

    private static void CopyVisibilityGroupSettings(CameraVisibilityProxyGroup source, CameraVisibilityProxyGroup target)
    {
        target.targetCamera = source.targetCamera;
        target.cameraTag = source.cameraTag;
        target.visibilityPoints = source.visibilityPoints;
        target.visibilityBounds = source.visibilityBounds;
        target.visibilityRenderer = source.visibilityRenderer;
        target.viewportMargin = source.viewportMargin;
        target.showDistance = source.showDistance;
        target.hideDistance = source.hideDistance;
        target.requireCameraFrustum = source.requireCameraFrustum;
        target.targetRoots = source.targetRoots;
        target.includeChildren = source.includeChildren;
        target.hideOnStart = source.hideOnStart;
        target.checkInterval = source.checkInterval;
        target.keepMotionRunningWhenHidden = source.keepMotionRunningWhenHidden;
        target.motionControllers = source.motionControllers;
        target.logStateChanges = source.logStateChanges;
    }

    private static string GetVisibilityGroupObjectName(CameraVisibilityProxyGroup group, int index)
    {
        string suffix = GetVisibilityGroupNameSuffix(group);
        return string.IsNullOrEmpty(suffix)
            ? $"Visibility_Group_{index + 1}"
            : $"Visibility_{suffix}";
    }

    private static string GetVisibilityGroupNameSuffix(CameraVisibilityProxyGroup group)
    {
        if (group == null || group.targetRoots == null || group.targetRoots.Length == 0)
        {
            return string.Empty;
        }

        for (int i = 0; i < group.targetRoots.Length; i++)
        {
            GameObject target = group.targetRoots[i];
            if (target != null && !string.IsNullOrEmpty(target.name))
            {
                return SanitizeObjectName(target.name);
            }
        }

        return string.Empty;
    }

    private static string SanitizeObjectName(string objectName)
    {
        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        StringBuilder builder = new StringBuilder(objectName.Length);
        for (int i = 0; i < objectName.Length; i++)
        {
            char current = objectName[i];
            bool invalid = false;
            for (int c = 0; c < invalidCharacters.Length; c++)
            {
                if (current == invalidCharacters[c])
                {
                    invalid = true;
                    break;
                }
            }

            builder.Append(invalid ? '_' : current);
        }

        return builder.ToString();
    }

    private static bool NameContainsAny(string name, string[] keywords)
    {
        if (keywords == null)
        {
            return false;
        }

        string lowerName = name.ToLowerInvariant();
        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];
            if (!string.IsNullOrEmpty(keyword) && lowerName.Contains(keyword.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDetailName(string name)
    {
        return NameContainsAny(name, new[]
        {
            "螺栓", "螺母", "螺丝", "把手", "门把", "卡扣", "固定", "扶手", "栏杆", "管件", "阀门", "法兰", "接头", "支架", "链条", "链",
            "bolt", "nut", "handle", "screw", "fix", "handrail", "rail", "valve", "flange", "bracket", "pipe"
        });
    }

    private List<SmallComponentCandidate> FindSmallComponentCandidates()
    {
        List<SmallComponentCandidate> candidates = new List<SmallComponentCandidate>();
        if (boilerRoot == null)
        {
            return candidates;
        }

        Renderer[] renderers = boilerRoot.GetComponentsInChildren<Renderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!ShouldUseAsSmallComponentCandidate(renderer))
            {
                continue;
            }

            Mesh mesh = GetRendererMesh(renderer);
            int triangles = mesh != null ? GetTriangleCount(mesh) : 0;
            float boundsMagnitude = renderer.bounds.size.magnitude;
            bool isSmallByBounds = boundsMagnitude <= smallRendererBoundsSize;
            bool isSmallByMesh = triangles > 0 && triangles <= smallRendererTriangleLimit;
            bool isDetailByName = IsDetailName(GetPath(renderer.transform));

            if (!isSmallByBounds && !isSmallByMesh && !isDetailByName)
            {
                continue;
            }

            candidates.Add(new SmallComponentCandidate
            {
                Renderer = renderer,
                Mesh = mesh,
                Triangles = triangles,
                BoundsMagnitude = boundsMagnitude
            });
        }

        candidates.Sort((left, right) =>
        {
            int triangleCompare = right.Triangles.CompareTo(left.Triangles);
            if (triangleCompare != 0)
            {
                return triangleCompare;
            }

            return left.BoundsMagnitude.CompareTo(right.BoundsMagnitude);
        });

        return candidates;
    }

    private static bool ShouldUseAsSmallComponentCandidate(Renderer renderer)
    {
        if (renderer == null
            || renderer is ParticleSystemRenderer
            || renderer is TrailRenderer
            || renderer is LineRenderer
            || renderer.GetComponentInParent<Canvas>() != null
            || IsSmokeBoxStructure(renderer.transform))
        {
            return false;
        }

        Mesh mesh = GetRendererMesh(renderer);
        return mesh != null && renderer is MeshRenderer;
    }

    private static bool IsAlwaysVisibleStructureName(string name)
    {
        return NameContainsAny(name, new[]
        {
            "烟箱", "后烟箱", "前烟箱", "boiler body", "smokebox", "smoke box"
        });
    }

    private static bool IsAlwaysVisibleStructure(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (IsAlwaysVisibleStructureName(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsSmokeBoxStructure(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (NameContainsAny(current.name, new[] { "烟箱", "后烟箱", "前烟箱", "smokebox", "smoke box" }))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static int ClearOcclusionStaticFlags(GameObject target)
    {
        if (target == null)
        {
            return 0;
        }

        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
        StaticEditorFlags clearedFlags = flags & ~StaticEditorFlags.OccluderStatic & ~StaticEditorFlags.OccludeeStatic;
        if (flags == clearedFlags)
        {
            return 0;
        }

        Undo.RecordObject(target, "Clear occlusion static flags");
        GameObjectUtility.SetStaticEditorFlags(target, clearedFlags);
        EditorUtility.SetDirty(target);
        return 1;
    }

    private static Mesh GetRendererMesh(Renderer renderer)
    {
        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            return meshFilter.sharedMesh;
        }

        SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
        if (skinned != null)
        {
            return skinned.sharedMesh;
        }

        return null;
    }

    private static int GetTriangleCount(Mesh mesh)
    {
        if (mesh == null)
        {
            return 0;
        }

        int triangles = 0;
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            triangles += (int)mesh.GetIndexCount(i) / 3;
        }

        return triangles;
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

    private class SmallComponentCandidate
    {
        public Renderer Renderer;
        public Mesh Mesh;
        public int Triangles;
        public float BoundsMagnitude;
    }
}
