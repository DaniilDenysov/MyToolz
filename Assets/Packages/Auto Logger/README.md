# Auto Logger

Automatically captures all Unity console log output and writes it to a file. Configurable through an editor settings provider.

## Dependencies

None (internal).

External: UniTask (`Cysharp.Threading.Tasks`).

## Structure

```
Editor/
└── LogFileWriterSettingsProvider.cs   Editor preferences UI for log file configuration
Runtime/
├── LogFileWriterPreferences.cs        Serializable preferences for log file path and behavior
└── Logger.cs                          Core logger that hooks into Unity's log callback and writes to disk
```

## Setup

Open **Edit > Preferences** (or **Unity > Settings** on macOS) and navigate to the Auto Logger section. Configure the output file path and enable/disable logging. Once enabled, all `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError` calls are captured and appended to the configured file.
