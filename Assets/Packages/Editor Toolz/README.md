# Editor Toolz

Custom inspector attributes and property drawers that replicate a subset of Odin Inspector functionality, keeping the framework free of paid plugin dependencies.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

## Attributes

| Attribute | Purpose |
|---|---|
| `[ReadOnly]` | Renders the field as non-editable in the inspector |
| `[Button]` | Draws a method as a clickable button |
| `[ShowHideIf]` | Conditionally shows or hides a field based on another field's value |
| `[FoldoutGroup]` | Groups fields under a collapsible foldout |
| `[OnValueChanged]` | Invokes a callback when the field value changes in the inspector |
| `[Required]` | Validates that a reference field is assigned |
| `[ShowInInspector]` | Exposes a non-serialized property in the inspector |

## Structure

```
Editor/
├── ButtonDrawer.cs                 PropertyDrawer for [Button]
├── ConditionalVisibilityDrawer.cs  PropertyDrawer for [ShowHideIf]
├── FoldoutGroupDrawer.cs           PropertyDrawer for [FoldoutGroup]
├── OnValueChangedDrawer.cs         PropertyDrawer for [OnValueChanged]
└── ReadOnlyDrawer.cs               PropertyDrawer for [ReadOnly]
Runtime/
├── ButtonAttribute.cs
├── FoldoutGroupAttribute.cs
├── OnValueChangedAttribute.cs
├── ReadOnlyAttribute.cs
├── RequiredAttribute.cs
├── RequiredDrawer.cs
├── ShowHideIfAttribute.cs
└── ShowInInspector.cs
```
