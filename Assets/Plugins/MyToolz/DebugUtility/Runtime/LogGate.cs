using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyToolz.Utilities.Debug
{
    public static class LogGate
    {
        private const string ResourcesName = "LogGateSettings";
        private static LogGateSettingsSO _settings;

        private static readonly Dictionary<Type, bool> _typeCache = new Dictionary<Type, bool>(256);

        public static LogGateSettingsSO Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = Resources.Load<LogGateSettingsSO>(ResourcesName);
                }
                return _settings;
            }
            set
            {
                _settings = value;
                _typeCache.Clear();
            }
        }

        public static void ClearCache() => _typeCache.Clear();

        public static bool ShouldLog(object context)
        {
            if (context == null) return true;

            var t = context as Type ?? context.GetType();
            return ShouldLog(t);
        }

        public static bool ShouldLog(Type type)
        {
            if (type == null) return true;

            if (_typeCache.TryGetValue(type, out var cached))
                return cached;

            bool result = Evaluate(type);
            _typeCache[type] = result;
            return result;
        }

        private static bool Evaluate(Type type)
        {
            var s = Settings;
            if (s == null)
                return true;

            string full = type.FullName ?? type.Name;
            if (s.TryGet(full, out bool enabledExact))
                return enabledExact;

            string ns = type.Namespace;
            while (!string.IsNullOrEmpty(ns))
            {
                if (s.TryGet(ns, out bool enabledNs))
                    return enabledNs;

                int lastDot = ns.LastIndexOf('.');
                if (lastDot < 0) break;
                ns = ns.Substring(0, lastDot);
            }

            if (s.TryGet("*", out bool enabledGlobal))
                return enabledGlobal;

            return s.DefaultEnabled;
        }
    }
}
