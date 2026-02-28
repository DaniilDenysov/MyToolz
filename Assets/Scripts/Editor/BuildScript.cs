using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using System.Linq;

public class BuildScript
{
    public static void BuildAddressablesAndPlayer()
    {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
            throw new System.Exception("Addressables build failed: " + result.Error);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        options.target = EditorUserBuildSettings.activeBuildTarget;
        options.locationPathName = "build";

        var buildResult = BuildPipeline.BuildPlayer(options);
        if (buildResult.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed");
    }
}