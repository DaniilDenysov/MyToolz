#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyToolz.EditorToolz;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Editor
{
    /// <summary>
    /// Single inspector that replaces Odin for the MyToolz attribute set. Unity only
    /// ever instantiates ONE editor per type, so foldout groups, title groups, buttons,
    /// inspector-only members and custom GUI callbacks all have to be served from the
    /// same editor — previously a separate button editor and a fallback foldout editor
    /// fought over the same types and the foldouts lost.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), editorForChildClasses: true)]
    public class MyToolzInspector : UnityEditor.Editor
    {
        private InspectorLayout _layout;
        private List<ButtonGUI.ButtonMethod> _buttons;

        private void OnEnable()
        {
            if (target == null)
                return;

            var type = target.GetType();
            _buttons = ButtonGUI.Resolve(type);
            _layout = InspectorLayout.Build(serializedObject, type);
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                DrawDefaultInspector();
                return;
            }

            _layout ??= InspectorLayout.Build(serializedObject, target.GetType());

            if (!_layout.HasCustomContent)
            {
                DrawDefaultInspector();
            }
            else
            {
                serializedObject.Update();
                _layout.Draw(serializedObject, targets);
                serializedObject.ApplyModifiedProperties();
            }

            ButtonGUI.DrawButtons(_buttons, targets);
        }
    }

    /// <summary>
    /// Pre-computed, reflection-derived description of how a type should be laid out:
    /// the ordered tree of groups and members. Built once per editor instance; the
    /// live <see cref="SerializedProperty"/> objects are resolved fresh every repaint.
    /// </summary>
    internal sealed class InspectorLayout
    {
        private enum GroupKind { Foldout, Title }

        private interface ILayoutItem
        {
            float Order { get; }
            int DeclIndex { get; }
        }

        private abstract class MemberEntry : ILayoutItem
        {
            public float Order { get; set; }
            public int DeclIndex { get; set; }

            public string GroupPath;
            public GroupKind GroupKind;
            public string GroupSubtitle;
            public bool GroupDefaultExpanded = true;

            public bool HasGroup => !string.IsNullOrEmpty(GroupPath);
        }

        private sealed class FieldEntry : MemberEntry
        {
            public string PropertyPath;
            public FieldInfo Field;
            public string LabelOverride;
            public string Suffix;
            public bool SuffixOverlay;
            public bool HasMin;
            public double Min;
            public bool HasMax;
            public double Max;
            public bool HasListSettings;

            public bool HasDecorator =>
                LabelOverride != null || Suffix != null || HasMin || HasMax || HasListSettings;
        }

        private sealed class InspectorValueEntry : MemberEntry
        {
            public MemberInfo Member;
            public bool ReadOnly;
            public string LabelOverride;
        }

        private sealed class GuiCallbackEntry : MemberEntry
        {
            public MethodInfo Method;
        }

        private sealed class GroupNode : ILayoutItem
        {
            public string Path;
            public string DisplayName;
            public GroupKind Kind;
            public string Subtitle;
            public bool DefaultExpanded = true;

            public float Order { get; set; } = float.MaxValue;
            public int DeclIndex { get; set; } = int.MaxValue;

            public readonly List<ILayoutItem> Children = new();
            public readonly Dictionary<string, GroupNode> ChildGroups = new(StringComparer.Ordinal);
        }

        private static readonly Dictionary<string, bool> FoldoutState = new();

        private static GUIStyle _foldoutStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _suffixStyle;

        private readonly List<ILayoutItem> _topLevel;
        public bool HasCustomContent { get; }

        private InspectorLayout(List<ILayoutItem> topLevel, bool hasCustomContent)
        {
            _topLevel = topLevel;
            HasCustomContent = hasCustomContent;
        }

        // ---- Building -------------------------------------------------------

        public static InspectorLayout Build(SerializedObject so, Type type)
        {
            var serializedNames = CollectSerializedNames(so);

            var entries = new List<MemberEntry>();
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var seenFields = new HashSet<string>(StringComparer.Ordinal);
            bool hasCustom = false;
            int declIndex = 0;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            // Walk base -> derived so inherited fields keep their natural order on top.
            var chain = new List<Type>();
            for (var t = type; t != null && t != typeof(object); t = t.BaseType)
                chain.Add(t);
            chain.Reverse();

            foreach (var t in chain)
            {
                foreach (var member in t.GetMembers(flags).OrderBy(m => m.MetadataToken))
                {
                    switch (member)
                    {
                        case FieldInfo field when serializedNames.Contains(field.Name):
                            if (!seenFields.Add(field.Name))
                                break;

                            var fe = BuildFieldEntry(field);
                            fe.DeclIndex = declIndex++;
                            entries.Add(fe);
                            consumed.Add(field.Name);
                            if (fe.HasGroup || fe.HasDecorator)
                                hasCustom = true;
                            break;

                        case FieldInfo field:
                        {
                            var sii = field.GetCustomAttribute<ShowInInspectorAttribute>(true);
                            if (sii == null)
                                break;
                            if (!seenFields.Add(field.Name))
                                break;

                            var ve = BuildValueEntry(field, field.FieldType, sii);
                            ve.DeclIndex = declIndex++;
                            entries.Add(ve);
                            hasCustom = true;
                            break;
                        }

                        case PropertyInfo prop:
                        {
                            var sii = prop.GetCustomAttribute<ShowInInspectorAttribute>(true);
                            if (sii == null || !prop.CanRead)
                                break;

                            var ve = BuildValueEntry(prop, prop.PropertyType, sii);
                            ve.DeclIndex = declIndex++;
                            entries.Add(ve);
                            hasCustom = true;
                            break;
                        }

                        case MethodInfo method:
                        {
                            var gui = method.GetCustomAttribute<OnInspectorGUIAttribute>(true);
                            if (gui == null || method.GetParameters().Length != 0)
                                break;

                            var ge = new GuiCallbackEntry { Method = method };
                            ReadCommon(method, ge);
                            ge.DeclIndex = declIndex++;
                            entries.Add(ge);
                            hasCustom = true;
                            break;
                        }
                    }
                }
            }

            // Safety net: never let a serialized field vanish if reflection missed it.
            foreach (var name in serializedNames)
            {
                if (consumed.Contains(name))
                    continue;
                entries.Add(new FieldEntry { PropertyPath = name, DeclIndex = declIndex++ });
            }

            var topLevel = BuildTree(entries);
            return new InspectorLayout(topLevel, hasCustom);
        }

        private static HashSet<string> CollectSerializedNames(SerializedObject so)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            using var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.propertyPath == "m_Script")
                    continue;
                names.Add(it.name);
            }
            return names;
        }

        private static FieldEntry BuildFieldEntry(FieldInfo field)
        {
            var e = new FieldEntry { PropertyPath = field.Name, Field = field };
            ReadCommon(field, e);

            var label = field.GetCustomAttribute<LabelTextAttribute>(true);
            if (label != null)
                e.LabelOverride = label.Text;

            var suffix = field.GetCustomAttribute<SuffixLabelAttribute>(true);
            if (suffix != null)
            {
                e.Suffix = suffix.Label;
                e.SuffixOverlay = suffix.Overlay;
            }

            var min = field.GetCustomAttribute<MinValueAttribute>(true);
            if (min != null)
            {
                e.HasMin = true;
                e.Min = min.Value;
            }

            var max = field.GetCustomAttribute<MaxValueAttribute>(true);
            if (max != null)
            {
                e.HasMax = true;
                e.Max = max.Value;
            }

            e.HasListSettings = field.GetCustomAttribute<ListDrawerSettingsAttribute>(true) != null;
            return e;
        }

        private static InspectorValueEntry BuildValueEntry(MemberInfo member, Type _, ShowInInspectorAttribute attr)
        {
            var e = new InspectorValueEntry
            {
                Member = member,
                ReadOnly = attr.ReadOnly || member.GetCustomAttribute<ReadOnlyAttribute>(true) != null,
            };
            ReadCommon(member, e);

            var label = member.GetCustomAttribute<LabelTextAttribute>(true);
            if (label != null)
                e.LabelOverride = label.Text;

            return e;
        }

        private static void ReadCommon(MemberInfo member, MemberEntry e)
        {
            var order = member.GetCustomAttribute<PropertyOrderAttribute>(true);
            e.Order = order?.Order ?? 0f;

            var title = member.GetCustomAttribute<TitleGroupAttribute>(true);
            if (title != null && !string.IsNullOrWhiteSpace(title.Title))
            {
                e.GroupPath = title.Title.Trim();
                e.GroupKind = GroupKind.Title;
                e.GroupSubtitle = title.Subtitle;
                e.GroupDefaultExpanded = true;
                return;
            }

            var foldout = member.GetCustomAttribute<FoldoutGroupAttribute>(true);
            if (foldout != null && !string.IsNullOrWhiteSpace(foldout.GroupName))
            {
                e.GroupPath = foldout.GroupName.Trim();
                e.GroupKind = GroupKind.Foldout;
                e.GroupDefaultExpanded = foldout.ExpandedByDefault;
            }
        }

        private static List<ILayoutItem> BuildTree(List<MemberEntry> entries)
        {
            var topLevel = new List<ILayoutItem>();
            var topGroups = new Dictionary<string, GroupNode>(StringComparer.Ordinal);

            foreach (var e in entries)
            {
                if (!e.HasGroup)
                {
                    topLevel.Add(e);
                    continue;
                }

                var segments = e.GroupPath.Split('/');
                GroupNode parent = null;
                var level = topGroups;
                string accum = null;
                GroupNode node = null;

                for (int s = 0; s < segments.Length; s++)
                {
                    var seg = segments[s].Trim();
                    accum = accum == null ? seg : accum + "/" + seg;

                    if (!level.TryGetValue(seg, out node))
                    {
                        node = new GroupNode
                        {
                            Path = accum,
                            DisplayName = seg,
                            Kind = e.GroupKind,
                            DefaultExpanded = e.GroupDefaultExpanded,
                        };
                        level[seg] = node;

                        if (parent == null)
                            topLevel.Add(node);
                        else
                            parent.Children.Add(node);
                    }

                    parent = node;
                    level = node.ChildGroups;
                }

                // The leaf segment carries the real kind/subtitle for this member.
                node.Kind = e.GroupKind;
                node.DefaultExpanded = e.GroupDefaultExpanded;
                if (e.GroupKind == GroupKind.Title && !string.IsNullOrEmpty(e.GroupSubtitle))
                    node.Subtitle = e.GroupSubtitle;

                node.Children.Add(e);
            }

            foreach (var g in topGroups.Values)
                ComputeOrder(g);

            topLevel.Sort(CompareItems);
            return topLevel;
        }

        private static void ComputeOrder(GroupNode g)
        {
            float minOrder = float.MaxValue;
            int minDecl = int.MaxValue;

            foreach (var child in g.Children)
            {
                if (child is GroupNode sub)
                    ComputeOrder(sub);

                if (child.Order < minOrder) minOrder = child.Order;
                if (child.DeclIndex < minDecl) minDecl = child.DeclIndex;
            }

            g.Order = minOrder == float.MaxValue ? 0f : minOrder;
            g.DeclIndex = minDecl == int.MaxValue ? 0 : minDecl;
            g.Children.Sort(CompareItems);
        }

        private static int CompareItems(ILayoutItem a, ILayoutItem b)
        {
            int c = a.Order.CompareTo(b.Order);
            return c != 0 ? c : a.DeclIndex.CompareTo(b.DeclIndex);
        }

        // ---- Drawing --------------------------------------------------------

        public void Draw(SerializedObject so, UnityEngine.Object[] targets)
        {
            EnsureStyles();
            DrawScriptField(so);

            foreach (var item in _topLevel)
                DrawItem(item, so, targets);
        }

        private void DrawItem(ILayoutItem item, SerializedObject so, UnityEngine.Object[] targets)
        {
            switch (item)
            {
                case GroupNode g:
                    DrawGroup(g, so, targets);
                    break;
                case FieldEntry f:
                    DrawField(f, so, targets);
                    break;
                case InspectorValueEntry v:
                    DrawValueEntry(v, targets);
                    break;
                case GuiCallbackEntry c:
                    DrawGuiCallback(c, targets);
                    break;
            }
        }

        private void DrawGroup(GroupNode g, SerializedObject so, UnityEngine.Object[] targets)
        {
            if (g.Kind == GroupKind.Title)
            {
                DrawTitle(g.DisplayName, g.Subtitle);
                foreach (var child in g.Children)
                    DrawItem(child, so, targets);
                EditorGUILayout.Space(2);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string key = MakeKey(targets, g.Path);
                bool expanded = GetFoldout(key, g.DefaultExpanded);
                bool newExpanded = EditorGUILayout.Foldout(expanded, g.DisplayName, true, _foldoutStyle);
                SetFoldout(key, newExpanded);

                if (!newExpanded)
                    return;

                EditorGUILayout.Space(2);
                using (new EditorGUI.IndentLevelScope())
                {
                    foreach (var child in g.Children)
                        DrawItem(child, so, targets);
                }
            }
        }

        private static void DrawField(FieldEntry e, SerializedObject so, UnityEngine.Object[] targets)
        {
            if (!ConditionalVisibility.IsVisible(e.Field, targets))
                return;

            var prop = so.FindProperty(e.PropertyPath);
            if (prop == null)
                return;

            GUIContent label = e.LabelOverride != null
                ? new GUIContent(e.LabelOverride, prop.tooltip)
                : null;

            EditorGUI.BeginChangeCheck();

            if (!string.IsNullOrEmpty(e.Suffix))
            {
                var content = label ?? new GUIContent(prop.displayName, prop.tooltip);
                float h = EditorGUI.GetPropertyHeight(prop, content, true);
                Rect rect = EditorGUILayout.GetControlRect(true, h);
                EditorGUI.PropertyField(rect, prop, content, true);
                DrawSuffix(rect, e.Suffix);
            }
            else if (label != null)
            {
                EditorGUILayout.PropertyField(prop, label, true);
            }
            else
            {
                EditorGUILayout.PropertyField(prop, true);
            }

            if (EditorGUI.EndChangeCheck())
                ApplyClamp(prop, e);
        }

        private static void ApplyClamp(SerializedProperty prop, FieldEntry e)
        {
            if (!e.HasMin && !e.HasMax)
                return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float when prop.numericType == SerializedPropertyNumericType.Double:
                {
                    double v = prop.doubleValue;
                    if (e.HasMin && v < e.Min) v = e.Min;
                    if (e.HasMax && v > e.Max) v = e.Max;
                    prop.doubleValue = v;
                    break;
                }
                case SerializedPropertyType.Float:
                {
                    float v = prop.floatValue;
                    if (e.HasMin && v < (float)e.Min) v = (float)e.Min;
                    if (e.HasMax && v > (float)e.Max) v = (float)e.Max;
                    prop.floatValue = v;
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    long v = prop.longValue;
                    if (e.HasMin && v < (long)e.Min) v = (long)e.Min;
                    if (e.HasMax && v > (long)e.Max) v = (long)e.Max;
                    prop.longValue = v;
                    break;
                }
            }
        }

        private static void DrawSuffix(Rect rect, string suffix)
        {
            var line = new Rect(rect.x, rect.y, rect.width - 4f, EditorGUIUtility.singleLineHeight);
            GUI.Label(line, suffix, _suffixStyle);
        }

        private static void DrawValueEntry(InspectorValueEntry e, UnityEngine.Object[] targets)
        {
            var target = targets.Length > 0 ? targets[0] : null;
            if (target == null)
                return;

            if (!ConditionalVisibility.IsVisible(e.Member, targets))
                return;

            string label = e.LabelOverride ?? ObjectNames.NicifyVariableName(e.Member.Name);

            if (e.Member is FieldInfo field)
            {
                var value = field.GetValue(target);
                bool readOnly = e.ReadOnly || field.IsInitOnly;
                var newValue = DrawValue(label, value, field.FieldType, readOnly);

                if (!readOnly && !Equals(value, newValue))
                {
                    Undo.RecordObject(target, $"Modify {field.Name}");
                    field.SetValue(target, newValue);
                    EditorUtility.SetDirty(target);
                }
            }
            else if (e.Member is PropertyInfo prop)
            {
                if (!prop.CanRead)
                    return;

                var value = prop.GetValue(target);
                bool readOnly = e.ReadOnly || !prop.CanWrite;
                var newValue = DrawValue(label, value, prop.PropertyType, readOnly);

                if (!readOnly && !Equals(value, newValue))
                {
                    Undo.RecordObject(target, $"Modify {prop.Name}");
                    prop.SetValue(target, newValue);
                    EditorUtility.SetDirty(target);
                }
            }
        }

        private static object DrawValue(string label, object value, Type type, bool readOnly)
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

                if (type.IsEnum)
                    return EditorGUILayout.EnumPopup(label, value as Enum ?? (Enum)Enum.ToObject(type, 0));

                if (type == typeof(Vector2))
                    return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : default);

                if (type == typeof(Vector3))
                    return EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : default);

                if (type == typeof(Color))
                    return EditorGUILayout.ColorField(label, value != null ? (Color)value : default);

                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    return EditorGUILayout.ObjectField(label, value as UnityEngine.Object, type, true);

                EditorGUILayout.LabelField(label, value != null ? value.ToString() : $"({type.Name})");
                return value;
            }
        }

        private static void DrawGuiCallback(GuiCallbackEntry e, UnityEngine.Object[] targets)
        {
            var target = targets.Length > 0 ? targets[0] : null;
            if (target == null && !e.Method.IsStatic)
                return;

            try
            {
                e.Method.Invoke(e.Method.IsStatic ? null : target, null);
            }
            catch (TargetInvocationException tie)
            {
                Debug.LogException(tie.InnerException ?? tie, target);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, target);
            }
        }

        private static void DrawTitle(string title, string subtitle)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(title, _titleStyle);

            if (!string.IsNullOrEmpty(subtitle))
                EditorGUILayout.LabelField(subtitle, _subtitleStyle);

            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.15f));
            EditorGUILayout.Space(2);
        }

        private static void DrawScriptField(SerializedObject so)
        {
            var scriptProp = so.FindProperty("m_Script");
            if (scriptProp == null)
                return;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(scriptProp);
        }

        // ---- Foldout persistence & styles -----------------------------------

        private static string MakeKey(UnityEngine.Object[] targets, string path)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < targets.Length; i++)
                    h = h * 31 + (targets[i] ? targets[i].GetInstanceID() : 0);

                return h + ":" + path;
            }
        }

        private static bool GetFoldout(string key, bool defaultExpanded)
        {
            if (FoldoutState.TryGetValue(key, out var v))
                return v;

            FoldoutState[key] = defaultExpanded;
            return defaultExpanded;
        }

        private static void SetFoldout(string key, bool value) => FoldoutState[key] = value;

        private static void EnsureStyles()
        {
            _foldoutStyle ??= new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
            };

            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
            };

            _subtitleStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.5f) },
            };

            _suffixStyle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(1f, 1f, 1f, 0.5f) },
            };
        }
    }
}
#endif
