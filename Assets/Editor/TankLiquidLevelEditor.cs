using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(TankLiquidLevel))]
public sealed class TankLiquidLevelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TankLiquidLevel liquid = (TankLiquidLevel)target;
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            RecordBodyUndo(liquid);
            liquid.RefreshPreview();
        }

        string error = liquid.GetSetupError();
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Warning);
        EditorGUILayout.HelpBox(
            "根对象放在内腔箱底中心。Inner Size 填可盛水区域尺寸，不能直接照抄模型 Transform 的 Scale。" +
            "本组件适用于矩形等截面内腔，Cube 顶面本身就是水面。", MessageType.Info);
        EditorGUILayout.LabelField("当前液位", (liquid.CurrentLevel01 * 100f).ToString("F1") + "%");
        EditorGUILayout.LabelField("当前高度 / 最大高度",
            liquid.CurrentHeightMeters.ToString("F3") + " / " + liquid.MaximumHeightMeters.ToString("F3") + " m");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("空箱 0%")) SetInstant(liquid, 0f);
            if (GUILayout.Button("半箱 50%")) SetInstant(liquid, 0.5f);
            if (GUILayout.Button("满箱 100%")) SetInstant(liquid, 1f);
        }
        EditorGUILayout.HelpBox(
            "播放时按 Q 切换水箱外壁透明。Tank Shell Root 指向外壳模型，液体本身保持可见。" +
            "键盘输入与项目其他 Q 功能同时响应。", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.IsPlaying(liquid.gameObject)))
        {
            if (GUILayout.Button(liquid.IsShellTransparent ? "恢复外壁材质" : "外壁透明预览"))
                liquid.ToggleShellTransparency();
        }
    }

    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }

    private static void SetInstant(TankLiquidLevel liquid, float level)
    {
        Undo.RecordObject(liquid, "Set tank liquid level");
        RecordBodyUndo(liquid);
        liquid.SetLevelInstant01(level);
        EditorUtility.SetDirty(liquid);
        PrefabUtility.RecordPrefabInstancePropertyModifications(liquid);
    }

    private static void RecordBodyUndo(TankLiquidLevel liquid)
    {
        if (liquid.liquidBody == null) return;
        Undo.RecordObject(liquid.liquidBody, "Adjust tank liquid body");
        Renderer renderer = liquid.liquidBody.GetComponent<Renderer>();
        if (renderer != null) Undo.RecordObject(renderer, "Adjust tank liquid visibility");
    }
}

public static class TankLiquidLevelTools
{
    private const string MaterialPath = "Assets/Materials_SJK/TankLiquidWater.mat";

    [MenuItem("Tools/Boiler/Liquid Level/Create For Selected Tank")]
    public static void CreateForSelectedTank()
    {
        GameObject tank = Selection.activeGameObject;
        if (tank == null || !tank.scene.IsValid() || EditorUtility.IsPersistent(tank))
        {
            EditorUtility.DisplayDialog("创建液位", "请先在 Hierarchy 中选中场景里的水箱模型。", "确定");
            return;
        }

        Renderer[] renderers = tank.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds();
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!found || bounds.size.sqrMagnitude < 0.000001f)
        {
            EditorUtility.DisplayDialog("创建液位", "选中的对象没有可用的模型边界，请选择水箱模型或其父对象。", "确定");
            return;
        }

        Vector3 size = Vector3.Max(bounds.size, Vector3.one * 0.05f);
        Vector3 bottom = new Vector3(bounds.center.x, bounds.min.y + size.y * 0.08f, bounds.center.z);
        // 外形边界仅用于提供初值。壁厚、底座和四周凹腔都需要依据实际内壁手动校准。
        TankLiquidLevel liquid = CreateLiquid(bottom, new Vector3(size.x * 0.8f, size.y * 0.8f, size.z * 0.8f), false);
        BindShell(liquid, tank.transform);
    }

    [MenuItem("Tools/Boiler/Liquid Level/Create For Selected Tank", true)]
    private static bool ValidateSelection()
    {
        GameObject selected = Selection.activeGameObject;
        return !Application.isPlaying && selected != null &&
            selected.scene.IsValid() && !EditorUtility.IsPersistent(selected);
    }

    [MenuItem("Tools/Boiler/Liquid Level/Create Demo")]
    public static void CreateDemo()
    {
        Vector3 bottom = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        CreateLiquid(bottom, new Vector3(1.4f, 2f, 1.4f), true);
    }

    [MenuItem("Tools/Boiler/Liquid Level/Create Demo", true)]
    private static bool ValidateDemo()
    {
        return !Application.isPlaying;
    }

    public static TankLiquidLevel CreateLiquid(Vector3 bottom, Vector3 innerSize, bool demo)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create tank liquid level");
        Material material = GetOrCreateWaterMaterial();

        GameObject root = new GameObject("TankLiquid");
        root.transform.position = bottom;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(root, "Create tank liquid root");

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "LiquidBody";
        body.transform.SetParent(root.transform, false);
        Collider collider = body.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = body.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Undo.RegisterCreatedObjectUndo(body, "Create tank liquid body");

        TankLiquidLevel liquid = Undo.AddComponent<TankLiquidLevel>(root);
        Undo.RecordObject(liquid, "Configure tank liquid");
        liquid.liquidBody = body.transform;
        liquid.innerSize = innerSize;
        liquid.initialLevel01 = demo ? 0.2f : 0.5f;
        liquid.targetLevel01 = demo ? 0.8f : 0.5f;
        liquid.animateDemo = demo;
        liquid.enableShellHotkey = !demo;
        liquid.RefreshPreview();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        Undo.CollapseUndoOperations(group);
        return liquid;
    }

    [MenuItem("Tools/Boiler/Liquid Level/Bind Q To Selected Tank")]
    public static void BindQToSelectedTank()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.scene.IsValid() || EditorUtility.IsPersistent(selected)) return;
        TankLiquidLevel nearest = null;
        float nearestDistance = Mathf.Infinity;
        foreach (TankLiquidLevel liquid in Object.FindObjectsOfType<TankLiquidLevel>(true))
        {
            if (liquid.gameObject.scene != selected.scene) continue;
            float distance = (liquid.transform.position - selected.transform.position).sqrMagnitude;
            if (distance < nearestDistance) { nearest = liquid; nearestDistance = distance; }
        }
        if (nearest == null)
        {
            EditorUtility.DisplayDialog("绑定 Q", "当前场景还没有 TankLiquid 液位对象。", "确定");
            return;
        }
        BindShell(nearest, selected.transform);
        Selection.activeGameObject = nearest.gameObject;
    }

    [MenuItem("Tools/Boiler/Liquid Level/Bind Q To Selected Tank", true)]
    private static bool ValidateShellBinding()
    {
        return ValidateSelection();
    }

    public static void BindShell(TankLiquidLevel liquid, Transform shell)
    {
        Undo.RecordObject(liquid, "Bind tank shell transparency");
        liquid.tankShellRoot = shell;
        liquid.enableShellHotkey = true;
        liquid.shellTransparencyKey = KeyCode.Q;
        EditorUtility.SetDirty(liquid);
        PrefabUtility.RecordPrefabInstancePropertyModifications(liquid);
        EditorSceneManager.MarkSceneDirty(liquid.gameObject.scene);
    }

    private static Material GetOrCreateWaterMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder("Assets/Materials_SJK"))
            AssetDatabase.CreateFolder("Assets", "Materials_SJK");

        Shader shader = Shader.Find("Boiler/Tank Flow Water");
        if (shader == null) throw new System.InvalidOperationException("找不到 Boiler/Tank Flow Water Shader，请检查 Assets/Shaders。");
        Material material = new Material(shader);
        material.name = "TankLiquidWater";
        material.enableInstancing = true;
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        return material;
    }
}

/// <summary>将已有液位对象绑定到本项目的新水箱模型，绑定后由用户保存场景。</summary>
[InitializeOnLoad]
public static class TankLiquidShellAutoBinding
{
    private const string TankAssetPath = "Assets/model_SJK/Water Tank/Water Tank.fbx";

    static TankLiquidShellAutoBinding()
    {
        EditorApplication.delayCall += BindLoadedTanks;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += BindLoadedTanks;
    }

    public static void BindLoadedTanks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (TankLiquidLevel liquid in Object.FindObjectsOfType<TankLiquidLevel>(true))
        {
            if (!liquid.enableShellHotkey || liquid.tankShellRoot != null || !liquid.gameObject.scene.IsValid() ||
                EditorUtility.IsPersistent(liquid)) continue;
            Transform nearest = null;
            float bestDistance = Mathf.Infinity;
            var pending = new System.Collections.Generic.Stack<Transform>();
            foreach (GameObject root in liquid.gameObject.scene.GetRootGameObjects()) pending.Push(root.transform);
            while (pending.Count > 0)
            {
                Transform node = pending.Pop();
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(node.gameObject);
                string path = source != null ? AssetDatabase.GetAssetPath(source) : null;
                if (path == TankAssetPath)
                {
                    float distance = (node.position - liquid.transform.position).sqrMagnitude;
                    if (distance < bestDistance) { bestDistance = distance; nearest = node; }
                    continue;
                }
                // FBX 内部不包含其他独立导入的 FBX 实例，无需遍历大型锅炉模型的网格节点。
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                for (int index = 0; index < node.childCount; index++) pending.Push(node.GetChild(index));
            }
            if (nearest != null) TankLiquidLevelTools.BindShell(liquid, nearest);
        }
    }
}
