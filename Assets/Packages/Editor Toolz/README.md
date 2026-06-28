# Editor Toolz

Custom inspector attributes and a unified inspector that replicate a subset of Odin Inspector functionality, keeping the framework free of paid plugin dependencies.

## Dependencies

| Package | ID |
|---|---|
| Debug Utility | `com.mytoolz.debugutility` |

## Attributes

| Attribute | Purpose |
|---|---|
| `[ReadOnly]` | Renders the field as non-editable in the inspector |
| `[Button]` | Draws a method as a clickable button (supports `Size`, `Mode`, hex tint, custom name) |
| `[ShowIf]` / `[HideIf]` | Conditionally shows or hides a field. Accepts a member name, a `name, value` pair, or an Odin-style `"@expression"` |
| `[FoldoutGroup]` | Groups fields under a collapsible box. Supports nested paths via `"Parent/Child"` |
| `[TitleGroup]` | Groups fields under a bold, always-visible title with a horizontal rule. Supports nested paths and an optional subtitle |
| `[OnValueChanged]` | Invokes a callback when the field value changes in the inspector |
| `[Required]` | Validates that a reference field is assigned |
| `[ShowInInspector]` | Exposes a non-serialized field or property in the inspector |
| `[LabelText]` | Overrides the displayed label of a field |
| `[SuffixLabel]` | Draws a trailing unit/hint label on a field (e.g. `"s"`, `"m/s"`); `overlay` draws it inside the field |
| `[MinValue]` / `[MaxValue]` | Clamps a numeric field without forcing a slider |
| `[PropertyOrder]` | Controls draw order relative to sibling members (lower draws first) |
| `[ListDrawerSettings]` | Mirrors common Odin list options so call sites compile unchanged |
| `[OnInspectorGUI]` | Marks a parameterless method that draws custom IMGUI at its position in the member order |

### `[ShowIf]` / `[HideIf]` expressions

The `"@expression"` form is evaluated by a small recursive-descent parser supporting the
subset used across the project:

- member references (fields, properties, parameterless `bool` methods — including private and inherited)
- logical `!`, `&&`, `||` and parentheses
- equality `==` / `!=`, including `enumField == EnumType.Member`
- bool / number / string / enum literals

Examples that work today: `"@enableStun"`, `"@!random"`, `"@weaponType != WeaponType.Melee"`,
`"@!randomize && !useAudioConfigPerClip"`.

## Architecture

Unity instantiates exactly **one** editor per type, so every attribute is served by a single
inspector — `MyToolzInspector` — registered for `UnityEngine.Object` with `editorForChildClasses`.
A second `[CustomEditor]` (for example, a separate button editor) would silently suppress the
others; that was the original cause of `[FoldoutGroup]` not rendering.

`MyToolzInspector` builds an `InspectorLayout`: a reflection-derived, ordered tree of groups and
members (serialized fields, `[ShowInInspector]` members and `[OnInspectorGUI]` methods). Members
are ordered by `[PropertyOrder]` then declaration order, grouped into nested foldout/title boxes,
and decorated (`[LabelText]`, `[SuffixLabel]`, `[MinValue]`/`[MaxValue]`, conditional visibility).
Methods marked `[Button]` are drawn at the bottom. When a type uses none of these features the
inspector falls back to Unity's default rendering, so unaffected components are untouched.

Per-field property drawers (`[ReadOnly]`, `[ShowIf]`/`[HideIf]`, `[OnValueChanged]`) remain
registered so they also work inside the default inspector and inside nested serialized types.

## Structure

```
Editor/
├── ButtonDrawer.cs                 ButtonGUI helper — resolves & draws [Button] methods
├── ConditionalVisibilityDrawer.cs  [ShowIf]/[HideIf] drawer + shared expression evaluator
├── FoldoutGroupDrawer.cs           MyToolzInspector — the unified inspector + layout engine
├── OnValueChangedDrawer.cs         PropertyDrawer for [OnValueChanged]
└── ReadOnlyDrawer.cs               PropertyDrawer for [ReadOnly]
Runtime/
├── ButtonAttribute.cs
├── FoldoutGroupAttribute.cs
├── TitleGroupAttribute.cs
├── LabelTextAttribute.cs
├── SuffixLabelAttribute.cs
├── MinMaxValueAttribute.cs         [MinValue] and [MaxValue]
├── PropertyOrderAttribute.cs
├── ListDrawerSettingsAttribute.cs
├── OnInspectorGUIAttribute.cs
├── OnValueChangedAttribute.cs
├── ReadOnlyAttribute.cs
├── RequiredAttribute.cs
├── RequiredDrawer.cs
├── ShowHideIfAttribute.cs          [ShowIf] and [HideIf]
└── ShowInInspector.cs
```
