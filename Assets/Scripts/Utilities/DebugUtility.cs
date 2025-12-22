#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MyToolz.Utilities.Debug
{
    public static class DebugUtility
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Enable Debugging")]
        public static void EnableDebug()
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (!symbols.Contains("ENABLE_DEBUG"))
            {
                symbols += ";ENABLE_DEBUG";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
            }
        }

        [MenuItem("Tools/Disable Debugging")]
        public static void DisableDebug()
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            symbols = symbols.Replace("ENABLE_DEBUG", "").Replace(";;", ";");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
        }
#endif
        public static void Log(string message)
        {
#if ENABLE_DEBUG
            UnityEngine.Debug.Log(message);
#endif
        }

        public static void LogWarning(string message)
        {
#if ENABLE_DEBUG
            UnityEngine.Debug.LogWarning(message);
#endif
        }

        public static void LogError(string message)
        {
#if ENABLE_DEBUG
            UnityEngine.Debug.LogError(message);
#endif
        }
    }
}
