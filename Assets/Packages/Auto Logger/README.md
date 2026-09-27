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

Open **Project Settings > Log File Writer**. It creates `Assets/Resources/LogFileWriterPreferences.asset`
on first use. Configure the output folder, retention and which statistics to collect. Once active, every
`Debug.Log`, `Debug.LogWarning` and `Debug.LogError` in a player build is captured and appended to a
per-session file. The logger never runs inside the editor.

### Activation switches

| Field | Default | Effect |
|---|---|---|
| `enabled` | on | Master switch. Off means the logger never hooks the console or touches the disk. |
| `enabledInReleaseBuilds` | on | Turn off to log only in development builds. |
| `enabledOnWebGL` | off | WebGL has no thread pool and each flush becomes an IndexedDB write. When enabled there, batches are written on the main thread. |

### Notes

- Batches are written every ~32 ms on a thread-pool thread (main thread on WebGL); a lock guards the file so
  the final drain in `Shutdown` never races the writer loop.
- The "most frequent messages" table is bounded by `maxTrackedMessages` (default 512); further distinct
  messages are counted in one "(other messages)" bucket.
