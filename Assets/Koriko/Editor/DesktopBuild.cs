using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Koriko.Editor
{
    public static class DesktopBuild
    {
        // Release players by default: no "Development Build" watermark and no player connection.
        // The opt-in --koriko-* checks still work in release players. KORIKO_DEV_BUILD=1 keeps
        // a development player for profiling.
        static BuildOptions Options=>Environment.GetEnvironmentVariable("KORIKO_DEV_BUILD")=="1"?BuildOptions.Development:BuildOptions.None;
        [MenuItem("Koriko/Build Mac prototype")]
        public static void Mac()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,2);
            Build(BuildTarget.StandaloneOSX,"Builds/mac/Kiki’s Delivery Service.app");
        }
        [MenuItem("Koriko/Build Windows prototype")]
        public static void Windows()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneWindows64,"Builds/windows/KikiDelivery.exe");
        }
        [MenuItem("Koriko/Build Linux prototype")]
        public static void Linux()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneLinux64,"Builds/linux/KikiDelivery.x86_64");
        }
        static void Build(BuildTarget target,string path)
        {
            // This prototype scene is generated from authored source; include the latest art/camera setup.
            SceneBuilder.Build();Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{SceneBuilder.ScenePath},locationPathName=path,target=target,options=Options});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Desktop build failed: "+report.summary.result);
            Debug.Log("KORIKO_BUILD_READY "+Path.GetFullPath(path)+(Options==BuildOptions.Development?" (development)":" (release)"));
        }
    }
}
