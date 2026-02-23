#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using MyToolz.EditorToolz;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    internal class FoldoutGroupMonoBehaviourEditor : UnityEditor.Editor
    {
        private FoldoutGroupDrawer _drawer;

        private void OnEnable() => _drawer = new FoldoutGroupDrawer(serializedObject, targets);
        public override void OnInspectorGUI() => _drawer.Draw(this);
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    internal class FoldoutGroupScriptableObjectEditor : UnityEditor.Editor
    {
        private FoldoutGroupDrawer _drawer;

        private void OnEnable() => _drawer = new FoldoutGroupDrawer(serializedObject, targets);
        public override void OnInspectorGUI() => _drawer.Draw(this);
    }

    internal class FoldoutGroupDrawer
    {
        private readonly SerializedObject _so;
        private readonly UnityEngine.Object[] _targets;

        private Dictionary<string, GroupMeta> _metaByPath;
        private bool _hasAnyGroups;

        private static readonly Dictionary<string, bool> FoldoutState = new();

        internal FoldoutGroupDrawer(SerializedObject so, UnityEngine.Object[] targets)
        {
            _so = so;
            _targets = targets;
            RebuildCache();
        }

        private void DrawNonSerializedShowInInspector()
        {
            var type = _targets[0].GetType();
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            var members = type.GetMembers(flags);

            foreach (var member in members)
            {
                var attr = member.GetCustomAttribute<ShowInInspectorAttribute>(true);
                if (attr == null)
                    continue;

                if (member is FieldInfo field)
                {
                    DrawField(field, attr);
                }
                else if (member is PropertyInfo prop)
                {
                    DrawProperty(prop, attr);
                }
            }
        }

        private void DrawField(FieldInfo field, ShowInInspectorAttribute attr)
        {
            var target = _targets[0];
            var value = field.GetValue(target);
            var newValue = DrawValue(field.Name, value, field.FieldType, attr.ReadOnly || field.IsInitOnly);

            if (!Equals(value, newValue) && !attr.ReadOnly)
            {
                Undo.RecordObject(target, $"Modify {field.Name}");
                field.SetValue(target, newValue);
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawProperty(PropertyInfo prop, ShowInInspectorAttribute attr)
        {
            if (!prop.CanRead)
                return;

            var target = _targets[0];
            var value = prop.GetValue(target);

            bool readOnly = attr.ReadOnly || !prop.CanWrite;

            var newValue = DrawValue(prop.Name, value, prop.PropertyType, readOnly);

            if (!Equals(value, newValue) && !readOnly)
            {
                Undo.RecordObject(target, $"Modify {prop.Name}");
                prop.SetValue(target, newValue);
                EditorUtility.SetDirty(target);
            }
        }

        private object DrawValue(string label, object value, Type type, bool readOnly)
        {
            using (new EditorGUI.DisabledScope(readOnly))
            {
                if (type == typeof(int))
                    return EditorGUILayout.IntField(label, value != null ? (int)value : 0);

                if (type == typeof(float))
                    return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);

                if (type == typeof(double))
                    return EditorGUILayout.DoubleField(label, value != null ? (double)value : 0d);

                if (type == typeof(bool))
                    return EditorGUILayout.Toggle(label, value != null && (bool)value);

                if (type == typeof(string))
                    return EditorGUILayout.TextField(label, value as string ?? "");

                if (type == typeof(Vector2))
                    return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : default);

                if (type == typeof(Vector3))
                    return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : default);

                if (type == typeof(Color))
                    return EditorGUILayout.ColorField(label, value != null ? (Color)value : default);

                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);

                EditorGUILayout.LabelField(label, $"Type not supported ({type.Name})");
                return value;
            }
        }


        public void Draw(UnityEditor.Editor editor)
        {
            if (_metaByPath == null)
                RebuildCache();

            if (!_hasAnyGroups)
            {
                editor.DrawDefaultInspector();
                return;
            }

            _so.Update();

            DrawScriptFieldIfPresent();

            var ungrouped = new List<SerializedProperty>();
            var grouped = new Dictionary<string, List<SerializedProperty>>();

            using (var it = _so.GetIterator())
            {
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (it.propertyPath == "m_Script")
                        continue;

                    var prop = _so.FindProperty(it.propertyPath);
                    if (prop == null)
                        continue;

                    if (!_metaByPath.TryGetValue(prop.propertyPath, out var meta) || !meta.IsGrouped)
                    {
                        ungrouped.Add(prop);
                        continue;
                    }

                    if (!grouped.TryGetValue(meta.GroupName, out var list))
                    {
                        list = new List<SerializedProperty>();
                        grouped.Add(meta.GroupName, list);
                    }

                    list.Add(prop);
                }
            }

            foreach (var p in ungrouped)
                EditorGUILayout.PropertyField(p, includeChildren: true);

            EditorGUILayout.Space(6);

            foreach (var kv in grouped)
            {
                var groupName = kv.Key;
                var props = kv.Value;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool expanded = GetFoldout(groupName, defaultExpanded: GetDefaultExpanded(groupName, props));
                    var newExpanded = EditorGUILayout.Foldout(expanded, groupName, toggleOnLabelClick: true);
                    SetFoldout(groupName, newExpanded);

                    if (newExpanded)
                    {
                        EditorGUILayout.Space(2);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            foreach (var p in props)
                                EditorGUILayout.PropertyField(p, includeChildren: true);
                        }
                    }
                }
            }

            _so.ApplyModifiedProperties();
        }

        private void RebuildCache()
        {
            _metaByPath = new Dictionary<string, GroupMeta>();
            _hasAnyGroups = false;

            var commonType = GetCommonMostDerivedType(_targets);
            if (commonType == null)
                return;

            var fieldMap = BuildFieldMap(commonType);

            using (var it = _so.GetIterator())
            {
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (it.propertyPath == "m_Script")
                        continue;

                    var topName = GetTopLevelName(it.propertyPath);

                    if (fieldMap.TryGetValue(topName, out var attr) && attr != null && !string.IsNullOrWhiteSpace(attr.GroupName))
                    {
                        _metaByPath[it.propertyPath] = new GroupMeta(attr.GroupName.Trim(), attr.ExpandedByDefault);
                        _hasAnyGroups = true;
                    }
                    else
                    {
                        _metaByPath[it.propertyPath] = GroupMeta.Ungrouped;
                    }
                }
            }
        }

        private static string GetTopLevelName(string propertyPath)
        {
            int dot = propertyPath.IndexOf('.');
            return dot >= 0 ? propertyPath.Substring(0, dot) : propertyPath;
        }

        private static Dictionary<string, FoldoutGroupAttribute> BuildFieldMap(Type type)
        {
            var dict = new Dictionary<string, FoldoutGroupAttribute>();

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    if (f.IsStatic) continue;
                    if (f.IsNotSerialized) continue;

                    bool isPublic = f.IsPublic;
                    bool hasSerializeField = f.GetCustomAttribute<SerializeField>(true) != null;

                    if (!isPublic && !hasSerializeField) continue;

                    var group = f.GetCustomAttribute<FoldoutGroupAttribute>(true);
                    if (!dict.ContainsKey(f.Name))
                        dict.Add(f.Name, group);
                }
            }

            return dict;
        }

        private static Type GetCommonMostDerivedType(UnityEngine.Object[] targets)
        {
            if (targets == null || targets.Length == 0) return null;

            var t = targets[0]?.GetType();
            if (t == null) return null;

            for (int i = 1; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                if (targets[i].GetType() != t)
                    return t;
            }

            return t;
        }

        private void DrawScriptFieldIfPresent()
        {
            var scriptProp = _so.FindProperty("m_Script");
            if (scriptProp != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(scriptProp);
            }
        }

        private bool GetDefaultExpanded(string groupName, List<SerializedProperty> props)
        {
            foreach (var p in props)
            {
                if (_metaByPath.TryGetValue(p.propertyPath, out var meta) && meta.IsGrouped)
                {
                    if (!meta.ExpandedByDefault)
                        return false;
                }
            }
            return true;
        }

        private bool GetFoldout(string groupName, bool defaultExpanded)
        {
            var key = MakeKey(groupName);
            if (FoldoutState.TryGetValue(key, out var val))
                return val;

            FoldoutState[key] = defaultExpanded;
            return defaultExpanded;
        }

        private void SetFoldout(string groupName, bool value)
        {
            FoldoutState[MakeKey(groupName)] = value;
        }

        private string MakeKey(string groupName)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < _targets.Length; i++)
                    h = h * 31 + (_targets[i] ? _targets[i].GetInstanceID() : 0);

                return $"{h}:{groupName}";
            }
        }

        private readonly struct GroupMeta
        {
            public static readonly GroupMeta Ungrouped = new GroupMeta(null, true);

            public readonly string GroupName;
            public readonly bool ExpandedByDefault;

            public bool IsGrouped => !string.IsNullOrEmpty(GroupName);

            public GroupMeta(string groupName, bool expandedByDefault)
            {
                GroupName = groupName;
                ExpandedByDefault = expandedByDefault;
            }
        }
    }
}
#endif
