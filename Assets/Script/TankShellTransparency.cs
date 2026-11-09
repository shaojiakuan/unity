using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 用运行时材质副本显示水箱外壁；原材质资产和未绑定的 Renderer 保持原样。
/// 由 TankLiquidLevel 持有，并在禁用或退出播放时恢复材质绑定。
/// </summary>
public sealed class TankShellTransparency : IDisposable
{
    private sealed class RendererState
    {
        public Renderer renderer;
        public Material[] originals;
        public Material[] transparent;
    }

    private readonly List<RendererState> renderers = new List<RendererState>();
    private readonly Dictionary<Material, Material> copies = new Dictionary<Material, Material>();
    public Transform Root { get; private set; }
    public bool IsTransparent { get; private set; }
    public int SupportedMaterialCount { get; private set; }

    public TankShellTransparency(Transform shellRoot, Transform excludedLiquidRoot)
    {
        Root = shellRoot;
        if (shellRoot == null) return;
        foreach (Renderer renderer in shellRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (excludedLiquidRoot != null &&
                (renderer.transform == excludedLiquidRoot || renderer.transform.IsChildOf(excludedLiquidRoot)))
                continue;
            renderers.Add(new RendererState
            {
                renderer = renderer,
                originals = renderer.sharedMaterials
            });
        }
    }

    public bool SetTransparent(bool transparent, float alpha)
    {
        if (!transparent)
        {
            RestoreBindings();
            return true;
        }

        alpha = float.IsNaN(alpha) || float.IsInfinity(alpha) ? 0.2f : Mathf.Clamp01(alpha);
        SupportedMaterialCount = 0;
        foreach (RendererState state in renderers)
        {
            if (state.renderer == null) continue;
            if (state.transparent == null) state.transparent = new Material[state.originals.Length];
            for (int index = 0; index < state.originals.Length; index++)
            {
                Material original = state.originals[index];
                if (!SupportsFade(original))
                {
                    state.transparent[index] = original;
                    continue;
                }

                Material copy;
                if (!copies.TryGetValue(original, out copy) || copy == null)
                {
                    copy = new Material(original);
                    copy.name = original.name + " (Tank inspection)";
                    copy.hideFlags = HideFlags.DontSave;
                    copies[original] = copy;
                }
                // 每次开启都保留当前原材质的颜色、贴图和其他参数，只调整透明状态。
                copy.CopyPropertiesFromMaterial(original);
                ConfigureFade(copy, alpha);
                state.transparent[index] = copy;
                SupportedMaterialCount++;
            }
            state.renderer.sharedMaterials = state.transparent;
        }

        IsTransparent = SupportedMaterialCount > 0;
        if (!IsTransparent) RestoreBindings();
        return IsTransparent;
    }

    public void Dispose()
    {
        RestoreBindings();
        foreach (Material copy in copies.Values)
        {
            if (copy == null) continue;
            if (Application.isPlaying) UnityEngine.Object.Destroy(copy);
            else UnityEngine.Object.DestroyImmediate(copy);
        }
        copies.Clear();
        renderers.Clear();
        Root = null;
    }

    private void RestoreBindings()
    {
        foreach (RendererState state in renderers)
        {
            if (state.renderer != null && state.transparent != null)
                state.renderer.sharedMaterials = state.originals;
        }
        IsTransparent = false;
    }

    private static bool SupportsFade(Material material)
    {
        if (material == null || material.shader == null) return false;
        string name = material.shader.name;
        return name == "Standard" || name == "Standard (Specular setup)";
    }

    private static void ConfigureFade(Material material, float alpha)
    {
        // Standard 的 Fade 模式连高光一起减弱，适合观察内部液位。
        material.SetFloat("_Mode", 2f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
        Color color = material.color;
        color.a = alpha;
        material.color = color;
    }
}
