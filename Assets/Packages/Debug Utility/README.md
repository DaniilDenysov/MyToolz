# Debug Utility

Structured logging framework with namespace-based tagging, log styling, and editor gating. Logs can be enabled or disabled per-namespace through a ScriptableObject configuration, and the editor provides a hierarchy window for managing log gates.

## Dependencies

None (foundational package).

## Structure

```
Editor/
├── LogGateSettingsAssetUtility.cs    Asset creation utility for LogGateSettingsSO
└── LoggingHierarchyWindow.cs         Editor window displaying log gate hierarchy
Runtime/
├── DebugUtility.cs                   Static logging API with namespace tags and styling
├── DebugUtilityPreferencesSO.cs      Global debug preferences ScriptableObject
├── DebugUtilityMessageSO.cs          Reusable log message template
├── LogGate.cs                        Per-namespace enable/disable toggle
└── LogGateSettingsSO.cs              ScriptableObject holding all log gate configurations
```

## Usage

Call `DebugUtility.Log(...)` with a namespace tag instead of `Debug.Log`. Create a `LogGateSettingsSO` asset to control which namespaces produce output. Use the Logging Hierarchy editor window for an overview of all active gates.
