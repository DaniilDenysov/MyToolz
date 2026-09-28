using System;
using System.Reflection;
using MyToolz.EditorToolz;
using NUnit.Framework;
using UnityEngine;

namespace MyToolz.Tests.EditMode
{
    /// <summary>
    /// The EditorToolz drawers live in an Editor-only assembly, but the attributes themselves are
    /// plain runtime types that call sites depend on. These tests pin down that public surface —
    /// constructor defaults, stored values, and the PropertyAttribute inheritance / AttributeUsage
    /// that decide whether Unity will actually invoke each drawer.
    /// </summary>
    public class EditorToolzAttributeTests
    {
        [Test]
        public void Button_Defaults_AreNormalAlwaysAndNull()
        {
            var attr = new ButtonAttribute();

            Assert.IsNull(attr.Name);
            Assert.AreEqual(ButtonSize.Normal, attr.Size);
            Assert.AreEqual(ButtonMode.Always, attr.Mode);
            Assert.IsNull(attr.HexColor);
        }

        [Test]
        public void Button_StoresAllArguments()
        {
            var attr = new ButtonAttribute("Do It", ButtonSize.Large, ButtonMode.PlaymodeOnly, "#FF0000");

            Assert.AreEqual("Do It", attr.Name);
            Assert.AreEqual(ButtonSize.Large, attr.Size);
            Assert.AreEqual(ButtonMode.PlaymodeOnly, attr.Mode);
            Assert.AreEqual("#FF0000", attr.HexColor);
        }

        [Test]
        public void Button_TargetsMethodsOnly()
        {
            var usage = typeof(ButtonAttribute).GetCustomAttribute<AttributeUsageAttribute>();
            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Method, usage.ValidOn);
        }

        [Test]
        public void FoldoutGroup_NullName_BecomesEmpty_AndExpandsByDefault()
        {
            var attr = new FoldoutGroupAttribute(null);

            Assert.AreEqual(string.Empty, attr.GroupName, "a null group name is coalesced to empty");
            Assert.IsTrue(attr.ExpandedByDefault);
        }

        [Test]
        public void FoldoutGroup_StoresNameAndCollapsedState()
        {
            var attr = new FoldoutGroupAttribute("Stats", expandedByDefault: false);

            Assert.AreEqual("Stats", attr.GroupName);
            Assert.IsFalse(attr.ExpandedByDefault);
        }

        [Test]
        public void OnValueChanged_Defaults_IncludeChildrenTrue_InitialDrawFalse()
        {
            var attr = new OnValueChangedAttribute("OnChanged");

            Assert.AreEqual("OnChanged", attr.MethodName);
            Assert.IsTrue(attr.IncludeChildren);
            Assert.IsFalse(attr.InvokeOnInitialDraw);
        }

        [Test]
        public void OnValueChanged_NullMethod_BecomesEmpty()
        {
            Assert.AreEqual(string.Empty, new OnValueChangedAttribute(null).MethodName);
        }

        [Test]
        public void ShowIf_SingleArg_LeavesCompareValueNull()
        {
            var attr = new ShowIfAttribute("_enabled");

            Assert.AreEqual("_enabled", attr.MemberName);
            Assert.IsNull(attr.CompareValue);
        }

        [Test]
        public void ShowIf_TwoArgs_StoresCompareValue()
        {
            var attr = new ShowIfAttribute("_mode", "Advanced");

            Assert.AreEqual("_mode", attr.MemberName);
            Assert.AreEqual("Advanced", attr.CompareValue);
        }

        [Test]
        public void HideIf_TwoArgs_StoresCompareValue()
        {
            var attr = new HideIfAttribute("_mode", "Basic");

            Assert.AreEqual("_mode", attr.MemberName);
            Assert.AreEqual("Basic", attr.CompareValue);
        }

        [Test]
        public void ShowIf_And_HideIf_AllowMultiple_SoConditionsCanStack()
        {
            Assert.IsTrue(typeof(ShowIfAttribute).GetCustomAttribute<AttributeUsageAttribute>().AllowMultiple);
            Assert.IsTrue(typeof(HideIfAttribute).GetCustomAttribute<AttributeUsageAttribute>().AllowMultiple);
        }

        [Test]
        public void MinValue_And_MaxValue_StoreBounds()
        {
            Assert.AreEqual(0d, new MinValueAttribute(0).Value);
            Assert.AreEqual(100d, new MaxValueAttribute(100).Value);
        }

        [Test]
        public void TitleGroup_Defaults_NullSubtitle_ZeroOrder()
        {
            var attr = new TitleGroupAttribute("Combat");

            Assert.AreEqual("Combat", attr.Title);
            Assert.IsNull(attr.Subtitle);
            Assert.AreEqual(0f, attr.Order);
        }

        [Test]
        public void TitleGroup_NullTitle_BecomesEmpty_AndOrderIsSettable()
        {
            var attr = new TitleGroupAttribute(null, "sub", 3f) { Order = 5f };

            Assert.AreEqual(string.Empty, attr.Title);
            Assert.AreEqual("sub", attr.Subtitle);
            Assert.AreEqual(5f, attr.Order, "Order has a public setter");
        }

        [Test]
        public void LabelText_StoresText()
        {
            Assert.AreEqual("Health Points", new LabelTextAttribute("Health Points").Text);
        }

        [Test]
        public void SuffixLabel_Defaults_NotOverlay()
        {
            var attr = new SuffixLabelAttribute("m/s");

            Assert.AreEqual("m/s", attr.Label);
            Assert.IsFalse(attr.Overlay);
        }

        [Test]
        public void SuffixLabel_OverlayFlag_IsStored()
        {
            Assert.IsTrue(new SuffixLabelAttribute("s", overlay: true).Overlay);
        }

        [Test]
        public void PropertyOrder_Default_IsZero()
        {
            Assert.AreEqual(0f, new PropertyOrderAttribute().Order);
            Assert.AreEqual(-10f, new PropertyOrderAttribute(-10f).Order);
        }

        [Test]
        public void Required_Defaults_ToNullMessage()
        {
            Assert.IsNull(new RequiredAttribute().Message);
            Assert.AreEqual("assign me", new RequiredAttribute("assign me").Message);
        }

        [Test]
        public void ShowInInspector_Default_IsNotReadOnly()
        {
            Assert.IsFalse(new ShowInInspectorAttribute().ReadOnly);
            Assert.IsTrue(new ShowInInspectorAttribute(true).ReadOnly);
        }

        [Test]
        public void RequireInterface_StoresInterfaceType()
        {
            var attr = new RequireInterfaceAttribute(typeof(IDisposable));
            Assert.AreEqual(typeof(IDisposable), attr.InterfaceType);
        }

        [Test]
        public void OnInspectorGUI_Constructors_AssignPrependAndAppend()
        {
            var empty = new OnInspectorGUIAttribute();
            Assert.IsNull(empty.Prepend);
            Assert.IsNull(empty.Append);

            var appendOnly = new OnInspectorGUIAttribute("After");
            Assert.IsNull(appendOnly.Prepend);
            Assert.AreEqual("After", appendOnly.Append);

            var both = new OnInspectorGUIAttribute("Before", "After");
            Assert.AreEqual("Before", both.Prepend);
            Assert.AreEqual("After", both.Append);
        }

        [Test]
        public void ListDrawerSettings_HasOdinCompatibleDefaults()
        {
            var attr = new ListDrawerSettingsAttribute();

            Assert.IsTrue(attr.DraggableItems);
            Assert.IsFalse(attr.ShowIndexLabels);
            Assert.IsTrue(attr.ShowPagingControls);
            Assert.IsTrue(attr.ShowItemCount);
            Assert.IsFalse(attr.HideAddButton);
            Assert.IsFalse(attr.IsReadOnly);
            Assert.AreEqual(0, attr.NumberOfItemsPerPage);
        }
    }

    /// <summary>
    /// Unity only routes a field through a custom drawer when its attribute derives from
    /// <see cref="PropertyAttribute"/>. These are the EditorToolz attributes that MUST, so a
    /// refactor that quietly changes a base class would break their inspectors silently.
    /// </summary>
    public class EditorToolzPropertyAttributeInheritanceTests
    {
        [Test]
        public void DrawerBackedAttributes_DeriveFromPropertyAttribute()
        {
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new ReadOnlyAttribute());
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new RequiredAttribute());
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new ShowIfAttribute("x"));
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new HideIfAttribute("x"));
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new OnValueChangedAttribute("m"));
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new ShowInInspectorAttribute());
            Assert.IsInstanceOf<UnityEngine.PropertyAttribute>(new RequireInterfaceAttribute(typeof(IDisposable)));
        }
    }

    /// <summary>
    /// <see cref="SelfValidationResult"/> is deliberately editor-free logic, so it is exercised
    /// directly: each Add* helper records the message with the matching severity, in order.
    /// </summary>
    public class SelfValidationTests
    {
        [Test]
        public void NewResult_HasNoMessages()
        {
            Assert.AreEqual(0, new SelfValidationResult().Messages.Count);
        }

        [Test]
        public void AddError_RecordsErrorSeverity()
        {
            var result = new SelfValidationResult();
            result.AddError("missing prefab");

            Assert.AreEqual(1, result.Messages.Count);
            Assert.AreEqual("missing prefab", result.Messages[0].Message);
            Assert.AreEqual(ValidationSeverity.Error, result.Messages[0].Severity);
        }

        [Test]
        public void AddWarning_And_AddInfo_UseMatchingSeverities()
        {
            var result = new SelfValidationResult();
            result.AddWarning("could be faster");
            result.AddInfo("just so you know");

            Assert.AreEqual(ValidationSeverity.Warning, result.Messages[0].Severity);
            Assert.AreEqual(ValidationSeverity.Info, result.Messages[1].Severity);
        }

        [Test]
        public void Messages_AccumulateInInsertionOrder()
        {
            var result = new SelfValidationResult();
            result.AddInfo("one");
            result.AddError("two");
            result.AddWarning("three");

            Assert.AreEqual(3, result.Messages.Count);
            Assert.AreEqual("one", result.Messages[0].Message);
            Assert.AreEqual("two", result.Messages[1].Message);
            Assert.AreEqual("three", result.Messages[2].Message);
        }

        private sealed class FakeValidator : ISelfValidator
        {
            public void Validate(SelfValidationResult result)
            {
                result.AddError("speed must be positive");
            }
        }

        [Test]
        public void ISelfValidator_Validate_PopulatesTheProvidedResult()
        {
            ISelfValidator validator = new FakeValidator();
            var result = new SelfValidationResult();

            validator.Validate(result);

            Assert.AreEqual(1, result.Messages.Count);
            Assert.AreEqual(ValidationSeverity.Error, result.Messages[0].Severity);
            Assert.AreEqual("speed must be positive", result.Messages[0].Message);
        }
    }

    /// <summary>Value matching behind [ShowIf]/[HideIf] (the drawer lives in the editor assembly).</summary>
    public class ConditionalVisibilityCompareTests
    {
        private enum Mode { Simple, Advanced }

        private static bool Compare(object value, object compareValue)
        {
            var drawer = System.Type.GetType("MyToolz.Editor.ConditionalVisibilityDrawer, MyToolz.EditorToolz.Editor");
            Assert.NotNull(drawer, "ConditionalVisibilityDrawer not found — was it renamed or moved?");
            var method = drawer.GetMethod("Compare", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (bool)method.Invoke(null, new[] { value, compareValue });
        }

        [Test] public void Enum_MatchesSameEnumValue() => Assert.IsTrue(Compare(Mode.Advanced, Mode.Advanced));
        [Test] public void Enum_MatchesItsNameAsString() => Assert.IsTrue(Compare(Mode.Advanced, "advanced"));
        [Test] public void Enum_DoesNotMatchOtherName() => Assert.IsFalse(Compare(Mode.Simple, "Advanced"));
        [Test] public void Float_MatchesIntArgument() => Assert.IsTrue(Compare(3f, 3));
        [Test] public void Enum_MatchesUnderlyingIntArgument() => Assert.IsTrue(Compare(Mode.Advanced, 1));
        [Test] public void String_ComparesExactly() => Assert.IsFalse(Compare("abc", "ABC"));
        [Test] public void NullCompareValue_UsesBoolOrNonNull()
        {
            Assert.IsTrue(Compare(true, null));
            Assert.IsFalse(Compare(false, null));
            Assert.IsTrue(Compare(new object(), null));
        }
    }
}
