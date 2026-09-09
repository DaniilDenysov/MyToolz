# DebugUtility

Package: `Assets/Packages/Debug Utility` · Namespace: `MyToolz.Utilities.Debug`

A static wrapper over `UnityEngine.Debug` that auto-tags every message (namespace / type /
member), styles it (color, size, bold/italic), and — crucially — lets you turn logging on
or off per type or per namespace without touching code. **Always use it instead of
`UnityEngine.Debug`.**

## The API

```csharp
public static class DebugUtility
{
    // With a context object (PREFERRED — enables gating + type tagging):
    void Log(object context, string message, AutoTag auto = AutoTag.Default, [CallerMemberName] string member = "");
    void LogWarning(object context, string message, AutoTag auto = AutoTag.Default, ...);
    void LogError(object context, string message, AutoTag auto = AutoTag.Default, ...);

    // Context-less overloads (always log, cannot be gated — use sparingly):
    void Log(string message, AutoTag auto = AutoTag.Default, ...);
    void LogWarning(string message, ...);
    void LogError(string message, ...);
}
```

There are `object` and `UnityEngine.Object` context overloads; passing `this` from a
MonoBehaviour resolves to one of them.

## Standard usage

```csharp
using MyToolz.Utilities.Debug;

public class WeaponModel : MonoBehaviour
{
    void Fire()
    {
        DebugUtility.Log(this, "Weapon fired");
        DebugUtility.LogWarning(this, "Ammo low");
        DebugUtility.LogError(this, $"Reload failed: {reason}");
    }
}
```

Output is prefixed like `[MyToolz,Player,FPS,WeaponModel] Weapon fired` and colored per
the preferences asset.

## AutoTag

```csharp
[Flags] enum AutoTag { None, NamespaceSegments, TypeName, MemberName, Default = NamespaceSegments | TypeName }
```

- Default tags namespace segments + type name.
- Add the calling method name when it helps: `DebugUtility.Log(this, "hit", AutoTag.Default | AutoTag.MemberName)`.
- `AutoTag.None` suppresses the prefix.

## Gating (LogGate) — why to pass `this`

`LogGate` reads a `LogGateSettingsSO` from `Resources/LogGateSettings`. When you pass a
context, `DebugUtility` asks the gate whether that type should log. Resolution order:

1. Exact full type name (e.g. `MyToolz.Player.FPS.WeaponModel`)
2. Each parent namespace, innermost first (`MyToolz.Player.FPS` → `MyToolz.Player` → `MyToolz`)
3. The global `*` entry
4. `DefaultEnabled` on the settings asset

This means you can silence one noisy class or a whole namespace from the inspector — but
**only if you passed a context**. The context-less `DebugUtility.Log("msg")` overloads
skip the gate and always print, so prefer `DebugUtility.Log(this, "msg")`.

Editor tooling: a **Logging Hierarchy** window (`Assets/Packages/Debug Utility/Editor`)
lets you toggle types/namespaces visually.

## Preferences asset

Styling comes from `DebugUtilityPreferencesSO` at
`Assets/Resources/MyToolz/Debug/DebugUtilityPreferences.asset` (auto-created in the Editor
if missing). It holds three `DebugUtilityMessageSO` references — Log / Warning / Error —
each with a color, font size, and font style (bold/italic).

## Do / Don't

- ✅ `DebugUtility.Log(this, "…")` from anything with a `this`.
- ✅ Context-less overload only for truly static contexts where no `this` exists.
- ❌ `UnityEngine.Debug.Log(...)` — bypasses tagging, styling, and gating entirely.
