using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;

public class BuildScript
{
    public static void BuildAddressablesAndPlayer()
    {
        EditorApplication.LockReloadAssemblies();

        try
        {
            AddressableAssetSettings.CleanPlayerContent(
                AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder
            );

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressablesResult);

            if (!string.IsNullOrEmpty(addressablesResult.Error))
                throw new Exception("Addressables build failed: " + addressablesResult.Error);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new Exception("No scenes enabled in Build Settings.");

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string extension = target switch
            {
                BuildTarget.StandaloneWindows => ".exe",
                BuildTarget.StandaloneWindows64 => ".exe",
                BuildTarget.StandaloneOSX => ".app",
                BuildTarget.StandaloneLinux64 => "",
                _ => ""
            };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                locationPathName = "build/Game" + extension,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Player build failed: " + report.summary.result);
        }
        finally
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}