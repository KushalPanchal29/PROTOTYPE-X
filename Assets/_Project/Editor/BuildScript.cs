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
            AddControlsToPage(Path.Combine(OutputPath, "index.html"));
            Debug.Log("[BuildScript] WebGL build ready in " + OutputPath);
        }

        // The game has no text (no fonts allowed), so the page under the game shows the title and controls.
        static void AddControlsToPage(string indexPath)
        {
            string html = File.ReadAllText(indexPath);
            string product = PlayerSettings.productName;
            html = html.Replace("<title>Unity Web Player | " + product + "</title>", "<title>Height Is Time</title>");
            html = html.Replace("<div id=\"unity-build-title\">" + product + "</div>",
                "<div id=\"unity-build-title\">Height Is Time</div>");
            const string controls =
                "    <p style=\"font-family:sans-serif;text-align:center;color:#ccc\">" +
                "<b>A / D</b> move &nbsp;·&nbsp; <b>Space</b> jump &nbsp;·&nbsp; <b>hold Shift</b> freeze time" +
                " &nbsp;·&nbsp; <b>R</b> respawn<br>Your height is the clock: climb to move time forward, " +
                "fall to rewind it.</p>\n";
            int firstScript = html.IndexOf("    <script>", System.StringComparison.Ordinal);
            if (firstScript >= 0) html = html.Insert(firstScript, controls);
            File.WriteAllText(indexPath, html);
        }
    }
}
