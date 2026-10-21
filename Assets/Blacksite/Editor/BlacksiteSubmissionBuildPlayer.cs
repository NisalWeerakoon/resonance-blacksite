using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Blacksite.Editor
{
    public static class BlacksiteSubmissionBuildPlayer
    {
        public static void BuildWindows()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "BLACKSITE_Submission_Windows"));
            Directory.CreateDirectory(root);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Blacksite/Scenes/Integration/SubmissionPlayable.unity" },
                locationPathName = Path.Combine(root, "RESONANCE_BLACKSITE.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"BLACKSITE_WINDOWS_BUILD result={report.summary.result} errors={report.summary.totalErrors} output={options.locationPathName}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("BLACKSITE Windows build failed; see Unity build log.");
        }
    }
}
