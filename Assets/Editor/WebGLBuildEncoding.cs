using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Keep the Windows WebGL Python/Node toolchain on UTF-8.</summary>
[InitializeOnLoad]
public sealed class WebGLBuildEncoding : IPreprocessBuildWithReport
{
    static WebGLBuildEncoding()
    {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            EnableUtf8();
    }

    public int callbackOrder { get { return -10000; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL)
            EnableUtf8();
    }

    private static void EnableUtf8()
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
            return;

        // Python 3.7 otherwise decodes Node's UTF-8 output using Windows CP936.
        // Set the editor process environment so its build children inherit it.
        if (Environment.GetEnvironmentVariable("PYTHONUTF8") != "1")
        {
            Environment.SetEnvironmentVariable("PYTHONUTF8", "1", EnvironmentVariableTarget.Process);
            Debug.Log("[WebGLBuildEncoding] Enabled PYTHONUTF8=1 for this editor's WebGL build tools.");
        }
    }
}
