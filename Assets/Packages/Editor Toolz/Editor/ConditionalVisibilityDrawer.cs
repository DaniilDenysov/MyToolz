#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using MyToolz.EditorToolz;
using UnityEditor;
using UnityEngine;

namespace MyToolz.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    internal sealed class ConditionalVisibilityDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ShouldDraw(property))
                return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ShouldDraw(property))
                return 0f;

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool ShouldDraw(SerializedProperty property) =>
            ConditionalVisibility.IsVisible(fieldInfo, property.serializedObject.targetObjects);
    }

    /// <summary>
    /// Shared visibility logic for <see cref="ShowIfAttribute"/>/<see cref="HideIfAttribute"/>.
    /// Used both by the per-field <see cref="ConditionalVisibilityDrawer"/> (default and nested
    /// inspectors) and by <see cref="MyToolzInspector"/> when it lays out grouped fields, so a
    /// field hides identically no matter which path renders it.
    /// </summary>
    internal static class ConditionalVisibility
    {
        public static bool IsVisible(MemberInfo member, UnityEngine.Object[] targets)
        {
            if (member == null || targets == null)
                return true;

            // ShowIf/HideIf allow multiple instances, so read them all and AND the
            // results (matching Odin). The generic GetCustomAttributes<T> avoids the
            // AmbiguousMatch that the singular GetCustomAttribute throws on duplicates.
            var showAttrs = new List<ShowIfAttribute>(member.GetCustomAttributes<ShowIfAttribute>(true));
            var hideAttrs = new List<HideIfAttribute>(member.GetCustomAttributes<HideIfAttribute>(true));

            if (showAttrs.Count == 0 && hideAttrs.Count == 0)
                return true;

            bool visible = true;

            foreach (var target in targets)
            {
                if (target == null)
                    continue;

                foreach (var showAttr in showAttrs)
                    visible &= EvaluateCondition(target, showAttr.MemberName, showAttr.CompareValue, safeDefault: true);

                foreach (var hideAttr in hideAttrs)
                    visible &= !EvaluateCondition(target, hideAttr.MemberName, hideAttr.CompareValue, safeDefault: false);
            }

            return visible;
        }

        private static bool EvaluateCondition(object target, string memberName, object compareValue, bool safeDefault)
        {
            if (string.IsNullOrWhiteSpace(memberName))
                return true;

            // Odin-style "@expression" — evaluate the inline boolean expression.
            if (memberName[0] == '@')
            {
                try
                {
                    return ExpressionEvaluator.Evaluate(memberName.Substring(1), target);
                }
                catch (Exception e)
                {
                    // Safe default keeps the field visible if parsing fails: ShowIf wants
                    // `true` (shown), HideIf passes `false` so `!condition` shows it.
                    WarnOnce(memberName, target as UnityEngine.Object, e);
                    return safeDefault;
                }
            }

            return EvaluateMember(target, memberName, compareValue);
        }

        private static bool EvaluateMember(object target, string memberName, object compareValue)
        {
            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            var type = target.GetType();

            var field = type.GetField(memberName, flags);
            if (field != null)
                return Compare(field.GetValue(target), compareValue);

            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead)
                return Compare(prop.GetValue(target), compareValue);

            var method = type.GetMethod(memberName, flags);
            if (method != null && method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                return (bool)method.Invoke(target, null);

            Debug.LogWarning($"[ShowIf/HideIf] Member '{memberName}' not found on {type.Name}", target as UnityEngine.Object);
            return true;
        }

        private static bool Compare(object value, object compareValue)
        {
            if (compareValue == null)
            {
                if (value is bool b)
                    return b;

                return value != null;
            }

            if (value == null)
                return false;

            if (value.GetType().IsEnum)
                return value.Equals(compareValue);

            return Equals(value, compareValue);
        }

        private static readonly HashSet<string> WarnedExpressions = new();

        private static void WarnOnce(string expression, UnityEngine.Object context, Exception e)
        {
            if (!WarnedExpressions.Add(expression))
                return;

            Debug.LogWarning($"[ShowIf/HideIf] Could not evaluate expression '{expression}': {e.Message}", context);
        }
    }

    /// <summary>
    /// Minimal recursive-descent evaluator for the Odin <c>"@expression"</c> subset used
    /// across the project: member references, <c>!</c>, <c>==</c>/<c>!=</c>,
    /// <c>&amp;&amp;</c>/<c>||</c>, parentheses, enum/number/bool/string literals.
    /// It resolves members (including private and inherited ones) against the inspected
    /// object via reflection.
    /// </summary>
    internal sealed class ExpressionEvaluator
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<string, Type> EnumTypeCache = new();

        private readonly object _target;
        private readonly List<string> _tokens;
        private int _pos;

        private ExpressionEvaluator(object target, List<string> tokens)
        {
            _target = target;
            _tokens = tokens;
        }

        public static bool Evaluate(string expression, object target)
        {
            var evaluator = new ExpressionEvaluator(target, Tokenize(expression));
            object result = evaluator.ParseOr();

            if (evaluator._pos != evaluator._tokens.Count)
                throw new FormatException($"Unexpected token '{evaluator.Peek()}'.");

            return ToBool(result);
        }

        // ---- Parsing (precedence: || < && < ==/!= < unary < primary) --------

        private object ParseOr()
        {
            object value = ParseAnd();
            while (Peek() == "||")
            {
                Next();
                object right = ParseAnd();
                value = ToBool(value) || ToBool(right);
            }
            return value;
        }

        private object ParseAnd()
        {
            object value = ParseEquality();
            while (Peek() == "&&")
            {
                Next();
                object right = ParseEquality();
                value = ToBool(value) && ToBool(right);
            }
            return value;
        }

        private object ParseEquality()
        {
            object left = ParseUnary();

            while (Peek() == "==" || Peek() == "!=")
            {
                string op = Next();
                object right;

                // `field == EnumType.Member` — resolve the literal against the
                // left operand's actual enum type so we never guess the type.
                string rawRight = Peek();
                if (left is Enum && IsDottedIdentifier(rawRight) && !ResolvesAsMember(rawRight))
                {
                    Next();
                    string member = rawRight.Substring(rawRight.LastIndexOf('.') + 1);
                    right = Enum.Parse(left.GetType(), member);
                }
                else
                {
                    right = ParseUnary();
                }

                bool equal = ValuesEqual(left, right);
                left = op == "==" ? equal : !equal;
            }

            return left;
        }

        private object ParseUnary()
        {
            if (Peek() == "!")
            {
                Next();
                return !ToBool(ParseUnary());
            }

            return ParsePrimary();
        }

        private object ParsePrimary()
        {
            string token = Peek() ?? throw new FormatException("Unexpected end of expression.");

            if (token == "(")
            {
                Next();
                object value = ParseOr();
                Expect(")");
                return value;
            }

            Next();

            switch (token)
            {
                case "true": return true;
                case "false": return false;
                case "null": return null;
            }

            if (token[0] == '"')
                return token.Substring(1, token.Length - 2);

            if (char.IsDigit(token[0]) || token[0] == '.')
                return ParseNumber(token);

            return ResolveIdentifier(token);
        }

        // ---- Identifier / member resolution ---------------------------------

        private object ResolveIdentifier(string name)
        {
            if (TryResolveMemberChain(name, out object value))
                return value;

            int lastDot = name.LastIndexOf('.');
            if (lastDot > 0)
            {
                string typePart = name.Substring(0, lastDot);
                string memberPart = name.Substring(lastDot + 1);
                string simpleTypeName = typePart.Substring(typePart.LastIndexOf('.') + 1);

                Type enumType = FindEnumType(simpleTypeName);
                if (enumType != null)
                    return Enum.Parse(enumType, memberPart);
            }

            throw new FormatException($"Could not resolve '{name}'.");
        }

        private bool ResolvesAsMember(string name) =>
            IsDottedIdentifier(name) && TryResolveMemberChain(name, out _);

        private bool TryResolveMemberChain(string path, out object value)
        {
            value = null;
            object current = _target;

            foreach (string part in path.Split('.'))
            {
                if (current == null || !TryGetMember(current, part, out current))
                    return false;
            }

            value = current;
            return true;
        }

        private static bool TryGetMember(object obj, string name, out object value)
        {
            value = null;

            for (var type = obj.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                var field = type.GetField(name, MemberFlags);
                if (field != null)
                {
                    value = field.GetValue(obj);
                    return true;
                }

                var prop = type.GetProperty(name, MemberFlags);
                if (prop != null && prop.CanRead)
                {
                    value = prop.GetValue(obj);
                    return true;
                }

                var method = type.GetMethod(name, MemberFlags, null, Type.EmptyTypes, null);
                if (method != null && method.GetParameters().Length == 0)
                {
                    value = method.Invoke(obj, null);
                    return true;
                }
            }

            return false;
        }

        private static Type FindEnumType(string simpleName)
        {
            if (EnumTypeCache.TryGetValue(simpleName, out var cached))
                return cached;

            Type found = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch { continue; }

                if (types == null)
                    continue;

                foreach (var type in types)
                {
                    if (type != null && type.IsEnum && type.Name == simpleName)
                    {
                        found = type;
                        break;
                    }
                }

                if (found != null)
                    break;
            }

            EnumTypeCache[simpleName] = found;
            return found;
        }

        // ---- Value helpers --------------------------------------------------

        private static bool ValuesEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            if (a.GetType() == b.GetType())
                return a.Equals(b);

            if (a is Enum && b is string bStr) return string.Equals(a.ToString(), bStr);
            if (b is Enum && a is string aStr) return string.Equals(b.ToString(), aStr);

            if (IsNumeric(a) && IsNumeric(b))
                return Convert.ToDouble(a).Equals(Convert.ToDouble(b));

            return a.Equals(b);
        }

        private static bool IsNumeric(object value) =>
            value is sbyte or byte or short or ushort or int or uint or
                long or ulong or float or double or decimal;

        private static bool ToBool(object value)
        {
            if (value is bool b)
                return b;

            return value != null;
        }

        private static object ParseNumber(string token)
        {
            string s = token;
            if (s.EndsWith("f") || s.EndsWith("F"))
                s = s.Substring(0, s.Length - 1);

            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                return iv;

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double dv))
                return dv;

            throw new FormatException($"Invalid number '{token}'.");
        }

        private static bool IsDottedIdentifier(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            char c = token[0];
            return (char.IsLetter(c) || c == '_') && token.IndexOf('.') >= 0;
        }

        // ---- Token cursor ---------------------------------------------------

        private string Peek() => _pos < _tokens.Count ? _tokens[_pos] : null;

        private string Next() => _tokens[_pos++];

        private void Expect(string token)
        {
            if (Peek() != token)
                throw new FormatException($"Expected '{token}' but found '{Peek() ?? "<end>"}'.");

            _pos++;
        }

        // ---- Tokenizer ------------------------------------------------------

        private static List<string> Tokenize(string s)
        {
            var tokens = new List<string>();
            int i = 0;

            while (i < s.Length)
            {
                char c = s[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }

                if (c == '&' && Lookahead(s, i) == '&') { tokens.Add("&&"); i += 2; continue; }
                if (c == '|' && Lookahead(s, i) == '|') { tokens.Add("||"); i += 2; continue; }
                if (c == '=' && Lookahead(s, i) == '=') { tokens.Add("=="); i += 2; continue; }
                if (c == '!' && Lookahead(s, i) == '=') { tokens.Add("!="); i += 2; continue; }
                if (c == '!') { tokens.Add("!"); i++; continue; }
                if (c == '(') { tokens.Add("("); i++; continue; }
                if (c == ')') { tokens.Add(")"); i++; continue; }

                if (c == '"')
                {
                    int j = i + 1;
                    var sb = new StringBuilder("\"");
                    while (j < s.Length && s[j] != '"') sb.Append(s[j++]);
                    sb.Append('"');
                    tokens.Add(sb.ToString());
                    i = j + 1;
                    continue;
                }

                if (char.IsDigit(c) || (c == '.' && char.IsDigit(Lookahead(s, i))))
                {
                    int j = i;
                    while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '.')) j++;
                    tokens.Add(s.Substring(i, j - i));
                    i = j;
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int j = i;
                    while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_' || s[j] == '.')) j++;
                    tokens.Add(s.Substring(i, j - i));
                    i = j;
                    continue;
                }

                throw new FormatException($"Unexpected character '{c}'.");
            }

            return tokens;
        }

        private static char Lookahead(string s, int i) => i + 1 < s.Length ? s[i + 1] : '\0';
    }
}
#endif
