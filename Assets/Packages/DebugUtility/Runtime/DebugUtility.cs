#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using MyToolz.ScriptableObjects.Utilites.Debug;


namespace MyToolz.Utilities.Debug
{
    [Flags]
    public enum AutoTag
    {
        None = 0,
        NamespaceSegments = 1 << 0,
        TypeName = 1 << 1,
        MemberName = 1 << 2,
        Default = NamespaceSegments | TypeName
    }
    /// <summary>
    /// https://www.reddit.com/r/gamedev/comments/1b3uo85/unity_console_logs_formatting/
    /// </summary>
    public static class LogExtensions
    {
        public static string Bold(this string str) => $"<b>{str}</b>";
        public static string Italic(this string str) => $"<i>{str}</i>";
        //public static string Underline(this string str) => $"<u>{str}</u>";
        //public static string Strikethrough(this string str) => $"<s>{str}</s>";
        public static string Size(this string str, float size) => $"<size={size}>{str}</size>";

        public static string Color(this string str, Color color)
        => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
    }

    public static class DebugUtility
    {
        private static readonly Dictionary<CacheKey, string> _prefixCache = new(256);

        private const string ResourcePath = "MyToolz/Debug/DebugUtilityPreferences";
        private const string AssetPath = "Assets/Resources/MyToolz/Debug/DebugUtilityPreferences.asset";

        private static DebugUtilityPreferencesSO Cached
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<DebugUtilityPreferencesSO>(ResourcePath);

                    if (cached == null)
                    {
                        UnityEngine.Debug.LogError($"[DebugUtility] Unable to find DebugUtilityPreferences on the path {ResourcePath}, trying to create an instance!");
#if UNITY_EDITOR
                        cached = CreatePreferencesAsset();
#endif
                    }
                }

                return cached;
            }
        }
        private static DebugUtilityPreferencesSO cached;


#if UNITY_EDITOR
            private static DebugUtilityPreferencesSO CreatePreferencesAsset()
            {
                string directory = Path.GetDirectoryName(AssetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                var asset = ScriptableObject.CreateInstance<DebugUtilityPreferencesSO>();

                AssetDatabase.CreateAsset(asset, AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                UnityEngine.Debug.Log(
                    $"[MyToolz] Created DebugUtilityPreferencesSO at {AssetPath}"
                );

                return asset;
            }
#endif

        [ThreadStatic] private static StringBuilder _sb;

        private static DebugUtilityMessageSO errorMessage = Cached.ErrorMessage;
        private static DebugUtilityMessageSO logMessage = Cached.LogMessage;
        private static DebugUtilityMessageSO warningMessage = Cached.WarningMessage;

        private readonly struct CacheKey
        {
            public readonly Type Type;
            public readonly AutoTag Auto;

            public CacheKey(Type type, AutoTag auto)
            {
                Type = type;
                Auto = auto;
            }
        }

//#if UNITY_EDITOR
//        [MenuItem("Tools/Enable Debugging")]
//        public static void EnableDebug()
//        {
//            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
//            if (!symbols.Contains("ENABLE_DEBUG"))
//            {
//                symbols += ";ENABLE_DEBUG";
//                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
//            }
//        }

//        [MenuItem("Tools/Disable Debugging")]
//        public static void DisableDebug()
//        {
//            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
//            symbols = symbols.Replace("ENABLE_DEBUG", "").Replace(";;", ";").Trim(';');
//            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
//        }
//#endif

        #region Static
        public static void Log(string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            UnityEngine.Debug.Log(Format(message, auto, logMessage, memberName));
        }

        public static void LogWarning(string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            UnityEngine.Debug.LogWarning(Format(message, auto, warningMessage, memberName));
        }

        public static void LogError(string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            UnityEngine.Debug.LogError(Format(message, auto, errorMessage, memberName));
        }
        #endregion

        #region Object
        public static void Log(object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            if (!LogGate.ShouldLog(context)) return;
            UnityEngine.Debug.Log(Format(message, auto, logMessage, memberName, context));
        }

        public static void LogWarning(object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            if (!LogGate.ShouldLog(context)) return;
            UnityEngine.Debug.LogWarning(Format(message, auto, warningMessage, memberName, context));
        }

        public static void LogError(object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            if (!LogGate.ShouldLog(context)) return;
            UnityEngine.Debug.LogError(Format(message, auto, errorMessage, memberName, context));
        }
        #endregion

        #region UnityObject
        public static void Log(UnityEngine.Object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            if (!LogGate.ShouldLog(context)) return;
            UnityEngine.Debug.Log(Format(message, auto, logMessage, memberName, context));
        }

        public static void LogWarning(UnityEngine.Object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            if (!LogGate.ShouldLog(context)) return;
            UnityEngine.Debug.LogWarning(Format(message, auto, warningMessage, memberName, context));
        }

        public static void LogError(UnityEngine.Object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string memberName = "")
        {
            if (Cached == null) return;
            if (!LogGate.ShouldLog(context)) return;
            UnityEngine.Debug.LogError(Format(message, auto, errorMessage, memberName, context));
        }
        #endregion

        private static string Format(string message, AutoTag auto, DebugUtilityMessageSO messageSO, string memberName, object context = default)
        {
            string prefix = GetOrCreatePrefix(context?.GetType(), auto);

            bool includeMember = (auto & AutoTag.MemberName) != 0;
            if (!includeMember && messageSO == null)
                return prefix + message;

            var sb = _sb ??= new StringBuilder(256);
            sb.Clear();

            sb.Append(prefix);
            if (includeMember)
            {
                sb.Append(memberName);
                sb.Append("] ");
            }
            sb.Append(message);

            return FormatFinalMessage(messageSO, sb.ToString());
        }

        private static string FormatFinalMessage(DebugUtilityMessageSO messageSO, string msg)
        {
            if (messageSO == null) return msg;
            switch (messageSO.FontStyle)
            {
                case ScriptableObjects.Utilites.Debug.FontStyle.Bold:
                    return msg.Bold().Size(messageSO.FontSize).Color(messageSO.Color);
                case ScriptableObjects.Utilites.Debug.FontStyle.Italic:
                    return msg.Italic().Size(messageSO.FontSize).Color(messageSO.Color);
                //case ScriptableObjects.Utilites.Debug.FontStyle.UnderLine:
                //    return msg.Underline().Color(messageSO.Color);
                //case ScriptableObjects.Utilites.Debug.FontStyle.StrikeThrough:
                //    return msg.Strikethrough().Color(messageSO.Color);
                default:
                    return msg.Size(messageSO.FontSize).Color(messageSO.Color);
            }
        }

        private static string GetOrCreatePrefix(Type type, AutoTag auto)
        {
            bool includeMember = (auto & AutoTag.MemberName) != 0;

            AutoTag cacheAuto = auto & ~AutoTag.MemberName;
            var key = new CacheKey(type, cacheAuto);

            if (!_prefixCache.TryGetValue(key, out string basePrefix))
            {
                basePrefix = BuildBasePrefix(type, cacheAuto);
                _prefixCache[key] = basePrefix;
            }

            if (basePrefix.Length == 0)
                return includeMember ? "[" : string.Empty;

            return includeMember ? basePrefix + "," : basePrefix + "] ";
        }

        private static string BuildBasePrefix(Type type, AutoTag auto)
        {

            if (type == null || auto == AutoTag.None)
                return string.Empty;

            var sb = _sb ??= new StringBuilder(128);
            sb.Clear();
            sb.Append('[');

            bool wroteAny = false;

            if ((auto & AutoTag.NamespaceSegments) != 0)
            {
                string ns = type.Namespace;
                if (!string.IsNullOrEmpty(ns))
                {
                    int start = 0;
                    for (int i = 0; i <= ns.Length; i++)
                    {
                        if (i == ns.Length || ns[i] == '.')
                        {
                            int len = i - start;
                            if (len > 0)
                            {
                                if (wroteAny) sb.Append(',');
                                sb.Append(ns, start, len);
                                wroteAny = true;
                            }
                            start = i + 1;
                        }
                    }
                }
            }

            if ((auto & AutoTag.TypeName) != 0)
            {
                if (wroteAny) sb.Append(',');
                sb.Append(type.Name);
                wroteAny = true;
            }

            if (!wroteAny)
                return string.Empty;


            return sb.ToString();
        }
    }
}

