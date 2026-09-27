# Input (Commands, Modes & Command Pipeline)

Input is built in layers, each its own package. Understand which layer you're touching:

| Package | Namespace | What it gives you |
|---------|-----------|-------------------|
| Command Pipeline | `MyToolz.DesignPatterns.Command` | Generic `ICommand` + `CommandPipeline<T>` (queue + concurrent execution). Not input-specific. |
| Input Commands | `MyToolz.InputManagement`, `MyToolz.InputManagement.Commands` | `InputCommandSO` (one asset per action), `IInputCommand`, `InputPhase`. |
| Input Command Pipeline | `MyToolz.InputManagement.Commands.Pipeline` | `InputCommandPipeline` + installer — for input-driven commands that run over time. |
| Input Management | `MyToolz.InputManagement` | `InputModeSO`, `InputStateManager`, `InputDeviceTracker` + installer — the everyday layer. |

All of this sits on top of Unity's **Input System** (`UnityEngine.InputSystem`, `InputActionAsset`). MyToolz does not replace it — it wraps actions in assets you can bind, gate, and react to through DI/events.

## The everyday layer: commands + modes

For 90% of gameplay/UI input you only need **Input Management**. The mental model:

- An **`InputCommandSO`** is one asset per logical action ("Jump", "Fire", "Pause"). It wraps an `InputActionReference` and re-broadcasts it as C# events, so consumers never touch the raw Input System.
- An **`InputModeSO`** is one asset per input *context* ("Gameplay", "Menu", "Dead"). It lists which commands are active in that context and sets cursor lock/visibility. Entering a mode enables exactly those actions and disables the rest.
- **`InputStateManager`** is the single-active-mode switch. `ChangeState(mode)` exits the old mode and enters the new one.

### `InputCommandSO`

```csharp
using MyToolz.InputManagement.Commands;
using Zenject;

public class JumpHandler : MonoBehaviour
{
    [SerializeField] private InputCommandSO jumpCommand;

    void OnEnable()  => jumpCommand.OnPerformed += Jump;   // event style
    void OnDisable() => jumpCommand.OnPerformed -= Jump;

    void Jump() { /* … */ }
}
```

Each `InputCommandSO` exposes a rich event set — pick the pair that fits:

- `OnPressed` / `OnReleased`, `OnStarted` / `OnPerformed` / `OnCanceled` (parameterless).
- `OnInputPressed(this)` … the same set carrying the `InputCommandSO`, plus
  `OnInputAction(CallbackContext, this)` and `OnInput()` for the raw callback.

Or **poll** instead of subscribing: `WasPressedThisFrame()`, `IsPressed()`,
`WasPerformedThisFrame()`, `WasReleasedThisFrame()`, and `ReadValue<T>()` (e.g.
`moveCommand.ReadValue<Vector2>()`). `inputActionPhase` on the asset picks which phase
the pooled events fire on.

You don't call `Initialize`/`Register` yourself — the installer does it (see below).

### `InputModeSO`

`[CreateAssetMenu … "MyToolz/InputManagement/InputModeSO"]`. Configure per mode:

- `cursorVisible`, `cursorLockMode` — applied in `OnEnter`.
- `commands` — the `InputCommandSO`s whose actions are enabled in this mode (all others are
  left disabled). `InputModeSO` implements `IPlayerInputState` (`OnEnter`/`OnExit`).

### `InputStateManager`

Plain C# class, injected. Holds one active `IPlayerInputState` (your `InputModeSO`):

```csharp
using MyToolz.InputManagement;
using Zenject;

public class PauseController
{
    InputStateManager _input;
    [SerializeField] InputModeSO menuMode;

    [Inject] void Construct(InputStateManager input) => _input = input;

    public void OpenMenu() => _input.ChangeState(menuMode);   // exits current, enters menu
}
```

`ChangeState` no-ops if you pass the current mode, fires `OnStateChanged(prev, next)`, and
never accepts null.

### Wiring: `InputStateManagementInstaller`

Add `InputStateManagementInstaller` to a context and assign in the Inspector:

- `inputActions` (`InputActionAsset`, `[Required]`) and `defaultInputModeSO` (`[Required]`).
- `inputCommands` / `inputModeSOs` — populate via the **Rebuild** context-menu (it finds all
  `InputCommandSO`/`InputModeSO` assets in the project).

`InstallBindings()` enables the asset, initializes + registers every command, initializes
every mode, binds `InputStateManager`, `InputDeviceTracker`, and the `InputActionAsset` as
singletons, then enters `defaultInputModeSO`. So after install, inject `InputStateManager`
to switch modes and `InputCommandSO`s (or inject nothing and drag the SO into a field).

### `InputDeviceTracker`

Injected singleton that watches every action and fires `OnInputDeviceChanged(InputDevice)`
when the player switches devices (KBM ↔ gamepad). Use it to swap prompt glyphs. `LastDevice`
holds the most recent. It's `IDisposable`; the installer owns its lifetime.

```csharp
[Inject] void Construct(InputDeviceTracker tracker)
    => tracker.OnInputDeviceChanged += d => RefreshPrompts(d);
```

## The command-pipeline layer (advanced: time-based / buffered input)

Reach for this only when input maps to **commands that run over multiple frames** (combos,
charged attacks, buffered actions) — not for simple button→callback wiring.

- `ICommand` is `void Execute()`. `CommandPipeline<T>` queues commands (`queueSize`, oldest
  dropped when full) and runs up to `callStackSize` concurrently: `Enqueue` runs `Update`
  automatically; `Update` promotes queued → executing.
- `IInputCommand : ICommand` adds `void Update()` and `bool IsFinished` — a command that
  ticks itself and reports completion.
- `InputCommandPipeline : CommandPipeline<IInputCommand>` holds a `register` of
  `InputCommandSO`s, owns an `InputDeviceTracker`, and each `Update()` ticks executing
  commands and drops finished ones. **You must pump `pipeline.Update()` every frame** (from a
  MonoBehaviour) for time-based commands to advance.
- `InputCommandPipelineInstaller` binds `ICommandPipeline<IInputCommand>` and
  `InputCommandPipeline`, initializes it with the `InputActionAsset`, and binds the asset
  `IfNotBound`.

```csharp
[Inject] void Construct(ICommandPipeline<IInputCommand> pipeline) => _pipeline = pipeline;
_pipeline.Enqueue(new DashCommand(...));   // runs now if a slot is free, else buffered
void Update() => _pipeline.Update();       // tick executing commands
```

## Do / Don't

- ✅ One `InputCommandSO` per action, one `InputModeSO` per context; switch contexts through
  `InputStateManager.ChangeState`.
- ✅ Consume input via an `InputCommandSO`'s events or polling — never subscribe to the raw
  `InputAction` directly in gameplay code.
- ✅ Let the installer register commands/modes; inject `InputStateManager` /
  `InputDeviceTracker` where you need them.
- ✅ Use `InputDeviceTracker.OnInputDeviceChanged` for device-adaptive UI, not per-frame guesses.
- ❌ Don't `Enable()`/`Disable()` `InputAction`s by hand — that's what `InputModeSO` is for.
- ❌ Don't reach for the command-pipeline layer for simple button presses; it's for multi-frame
  commands and needs a per-frame `Update()` pump.
- ❌ Don't `new` an `InputCommandSO`/`InputModeSO` — they're assets you author and reference.
