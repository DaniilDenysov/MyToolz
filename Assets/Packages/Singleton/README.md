# Singleton

A generic MonoBehaviour singleton base class with configurable `DontDestroyOnLoad` and duplicate handling.

## Dependencies

None.

## Structure

```
Runtime/
└── Singleton.cs   Abstract Singleton<T> : MonoBehaviour base class
```

## Usage

```csharp
public class GameManager : Singleton<GameManager>
{
    public override void Awake()
    {
        base.Awake();
        // initialization
    }
}
```

Inspector fields control whether the singleton persists across scenes (`dontDestroyOnLoad`) and whether duplicate GameObjects are destroyed entirely or just the component (`destroyGameObjectOnDuplicate`).
