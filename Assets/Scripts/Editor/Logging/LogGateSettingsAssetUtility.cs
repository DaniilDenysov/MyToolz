#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using MyToolz.Utilities.Debug;

namespace MyToolz.EditorToolz.Logging
{
    internal static class LogGateSettingsAssetUtility
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/LogGateSettings.asset";

        public static LogGateSettingsSO GetOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LogGateSettingsSO>(AssetPath);
            if (asset != null) return asset;

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            asset = ScriptableObject.CreateInstance<LogGateSettingsSO>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }
    }
}
#endif
