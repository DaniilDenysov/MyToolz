# SaveLoadBase — Setup & Usage

## Required Dependencies

**Newtonsoft.Json for Unity** must be installed before using this system.

Install via the Unity Package Manager using the following package name:

```
com.unity.nuget.newtonsoft-json
```

Or add it manually to `Packages/manifest.json`:

```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

Without this package the project will not compile, as `NewtonsoftJsonStrategy` references `Newtonsoft.Json` directly.

---

## Serialization Strategies

Select a strategy from the **Save Settings** foldout in the inspector via the SubclassSelector dropdown.

| Strategy | When to use |
|---|---|
| `NewtonsoftJsonStrategy` | Default. Supports collections, dictionaries, polymorphism, non-public fields. |
| `UnityJsonStrategy` | Only use if Newtonsoft is genuinely unavailable. Logs a warning on every read and write. Does not support collections or non-public fields. |

To add a custom strategy, subclass `SerializationStrategy<T>` and mark it `[Serializable]`. It will appear automatically in the inspector dropdown.

```csharp
[Serializable]
public sealed class MessagePackStrategy<T> : SerializationStrategy<T> where T : class
{
    public override string FileExtension => ".msgpack";

    public override string Serialize(T data) { ... }
    public override T Deserialize(string raw) { ... }
}
```
