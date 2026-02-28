# MyToolz

**MyToolz** is a fully open-source set of modular tools for Unity, designed to help you start projects faster, enforce good development practices, and set up a solid foundation from day one.

Every tool is distributed as a standalone UPM package under the `com.mytoolz.*` namespace. Each one can be used independently, though some depend on other tools within the set or on external libraries.

> **📖 Full Documentation:** [teamo1.gitbook.io/mytoolz](https://teamo1.gitbook.io/mytoolz/)

---

## Philosophy

- Good practices should be easy, not enforced through heavy rules.
- Project setup should be fast, repeatable, and predictable.
- Tools should stay flexible, not lock you into a rigid workflow.
- Developers should focus on building their ideas, not reinventing foundations.

MyToolz does not try to replace your stack. It helps you organize it, bootstrap it, and use it better.

---

## Requirements

- **Unity** 2022.3+
- **License:** MIT

---

## Packages

### Design Patterns

| Package | Description |
|---|---|
| **Adapter** | Simple adapter pattern for cleanly accessing non-serializable inspector references |
| **Command Pipeline** | Command pattern implementation with input action support |
| **Event Bus** | Generic event bus extending [Unity-Event-Bus](https://github.com/adammyhre/Unity-Event-Bus) |
| **MVP** | Model-View-Presenter core interfaces and abstract base classes |
| **Object Pool** | Generic object pool with IPoolable interface and Zenject integration |
| **Prototype** | Prototype design pattern implementation |
| **Singleton** | MonoBehaviour singleton implementation |
| **State Machine** | State machine with priority-based and hierarchical variants |

### Systems

| Package | Description |
|---|---|
| **Free Camera** | Lightweight camera controller with WASD, mouse look, and InputCommandSO bindings |
| **Input Commands** | ScriptableObject-based input command definitions |
| **Input Command Pipeline** | Bridges Input Commands with the Command Pipeline for input-driven execution |
| **Input Management** | Core input state management with input mode configuration and device detection |
| **MVP Clock** | Stopwatch and timer with MVP views |
| **MVP Game Settings** | ScriptableObject-driven settings with save/load and UI views |
| **MVP Health System** | Health bars, hit boxes, and healable components |
| **MVP Inventory System** | Drag-and-drop inventory with stacking, persistence, and pooled UI |
| **MVP Loading Screen** | Async scene management with loading screen UI |
| **MVP Notifications** | Priority queue notifications with deduplication and overflow policies |
| **MVP UI Management System** | Full UI lifecycle management with panel transitions and input blocking |
| **Tooltip System** | Event-driven tooltips with pointer-based triggers and positioning |

### Animations

| Package | Description |
|---|---|
| **Animations** | Priority-based animator state machine built on State Machine |
| **Tweener** | Base tweening abstraction layer built on DOTween |
| **UI Tweener** | Strategy-based UI tweening with ScriptableObject configurations |

### Utilities

| Package | Description |
|---|---|
| **Audio** | ScriptableObject-based audio system with pooling, randomization, and extension methods |
| **Auto Logger** | Automatic log-to-file writer |
| **Debug Utility** | Configurable logger with namespace tags, styling, and editor toggle |
| **Editor Toolz** | Custom inspector attributes (Required, ShowIf, etc.) as a lightweight Odin alternative |
| **Extensions** | Internal extension methods used across the framework |
| **IO** | Save/load system with pluggable Newtonsoft JSON and Unity JSON backends |

### Algorithms

| Package | Description |
|---|---|
| **AStar** | Multi-threaded A* pathfinding |

---

## External Dependencies

Some packages require third-party libraries that are not distributed through the Unity Package Manager registry. These must be installed separately.

| Dependency | Used By | Source |
|---|---|---|
| **Zenject** | Animations, Audio, IO, Input Command Pipeline, MVP Health System, MVP Inventory System, MVP Loading Screen, MVP Notifications, MVP UI Management System, Object Pool, State Machine | [GitHub](https://github.com/modesttree/Zenject) |
| **DOTween** | Audio, Tweener, UI Tweener, MVP Health System, MVP Notifications | [Website](https://dotween.demigiant.com/) |
| **UniTask** | Audio, Auto Logger, MVP Clock, MVP Loading Screen, State Machine | [GitHub](https://github.com/Cysharp/UniTask) |
| **SerializeReferenceExtensions** | IO, Tweener, MVP Clock, MVP Inventory System, MVP Loading Screen, MVP UI Management System | [GitHub](https://github.com/mackysoft/Unity-SerializeReferenceExtensions) |

Unity registry dependencies (Input System, TextMeshPro, Newtonsoft JSON) are resolved automatically through each package's `package.json`.

---

## Installation

1. Clone or download this repository into your Unity project's `Assets/Packages` folder.
2. Install the required external dependencies listed above.
3. Unity will detect the `package.json` files and resolve inner dependencies automatically.

Each package can also be referenced individually by downloading a .zip file with all the packages available as .unitypackage files.

---

## License

MIT — free to use in personal, commercial, and open-source projects with no royalties or licensing fees.

---

## Documentation

For full and up-to-date documentation, including API references, usage examples, and architectural guides, visit:

**[teamo1.gitbook.io/mytoolz](https://teamo1.gitbook.io/mytoolz/)**
