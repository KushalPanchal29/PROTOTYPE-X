using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HeightIsTime.EditorTools
{
    /// <summary>
    /// WebGL build for GitHub Pages. Compression is off because GitHub Pages cannot send the headers that
    /// compressed Unity builds need, which is the usual cause of "the link loads forever".
    /// </summary>
    public static class BuildScript
    {
        const string OutputPath = "Builds/WebGL";

        [MenuItem("Prototype/Build WebGL")]
        public static void BuildWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("[BuildScript] WebGL build failed: " + report.summary.result);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // Stops GitHub Pages' Jekyll step from touching the build files.
            File.WriteAllText(Path.Combine(OutputPath, ".nojekyll"), string.Empty);
            Debug.Log("[BuildScript] WebGL build ready in " + OutputPath);
        }
    }
}
