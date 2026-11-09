using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// WebGL 正式版构建脚本。
/// 命令行用法（在项目根目录）：
///   "E:\4SB\unityEditer\2021.3.19f1c1\Editor\Unity.exe" ^
///     -batchmode -quit -projectPath "<项目路径>" ^
///     -executeMethod BuildWebGL.Build -logFile "<日志路径>"
/// 输出目录：项目根目录/WebGL
/// </summary>
public static class BuildWebGL
{
    private const string TemplateName = "PROJECT:BoilerWeb";
    private const string OutputRelative = "WebGL";

    public static void Build()
    {
        try
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var outputDir = Path.Combine(projectRoot, OutputRelative);

            // ---------- 场景：取构建设置中启用的场景 ----------
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            // SampleScene 置顶作为启动场景：登录已由网页端 HTML 完成，Unity 直接进入主场景，
            // 避免 Inlog 登录场景在 WebGL 下阻塞主场景加载。
            var ordered = scenes
                .Where(s => s.Contains("SampleScene"))
                .Concat(scenes.Where(s => !s.Contains("SampleScene")))
                .ToArray();

            if (ordered.Length == 0)
            {
                throw new Exception("EditorBuildSettings 中没有启用任何场景，请先在 Build Settings 勾选场景。");
            }

            Debug.Log($"[BuildWebGL] 场景列表: {string.Join(" | ", scenes)}");
            Debug.Log($"[BuildWebGL] 输出目录: {outputDir}");

            // ---------- WebGL 平台设置 ----------
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            PlayerSettings.WebGL.template = TemplateName;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.memorySize = 1024;   // MB，大场景按需调整（最大 2048）
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            // Keep new data/metadata from reusing filenames of an older WASM build.
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.dataCaching = true;

            // ---------- 清理旧产物，避免残留 ----------
            if (Directory.Exists(outputDir))
            {
                Debug.Log("[BuildWebGL] 清理旧输出目录...");
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            // ---------- 执行构建 ----------
            var options = new BuildPlayerOptions
            {
                scenes = ordered,
                locationPathName = outputDir,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var sizeMb = summary.totalSize / (1024f * 1024f);
                Debug.Log($"[BuildWebGL] 构建成功！输出: {outputDir}，大小: {sizeMb:F1} MB，" +
                          $"耗时: {summary.totalTime.TotalMinutes:F1} 分钟。");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildWebGL] 构建失败: {summary.result}，" +
                               $"错误数: {summary.totalErrors}，警告数: {summary.totalWarnings}");
                EditorApplication.Exit(1);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[BuildWebGL] 构建过程异常: " + e);
            EditorApplication.Exit(1);
        }
    }
}
