# Bootstrap

A minimal application entry point plus editor toolbar tools for always launching
play mode through your boot scene.

## Dependencies

- `MyToolz.Singleton`
- `MyToolz.SceneManagement` (editor tools only: scene groups and the boot override)

## Structure

```
Runtime/
└── Bootstrapper.cs      PrivateSingleton<Bootstrapper> entry point
Editor/
└── BootstrapButton.cs   Boot, Boot Scene and Scenes controls on the main toolbar
```

## Usage

`Bootstrapper` is a `PrivateSingleton<Bootstrapper>` intended to live in a scene
named `Bootstrapper` that is the first scene in your build. Extend it (or attach
it) to perform one-time application setup. Put cross-cutting initialization
(service installers, persistent managers) and the `MultiSceneLoader` on that scene.

### Editor toolbar

`BootstrapButton` adds three controls to the main editor toolbar:

| Control | What it does |
|---|---|
| **Boot** | Saves modified scenes (if you agree), opens the scene named `Bootstrapper` and enters play mode. `MultiSceneLoader` starts with its first scene group, as in a build. |
| **Boot Scene** | Same, but `MultiSceneLoader` starts with the `SceneGroupSO` that contains the scene you are editing: the group where it is the `ActiveScene`, otherwise the first group that lists it. A scene in no group shows a dialog instead. |
| **Scenes** | A dropdown that opens the `Bootstrapper` scene or any scene listed in a `SceneGroupSO`, grouped by scene group and tagged with its `SceneType`. The scene being edited is ticked. |

Leaving play mode reopens the scene you were editing. The Boot Scene choice reaches
the game through `MultiSceneLoader.EditorSceneGroupOverrideKey` in `SessionState`;
it applies to one boot and is cleared when play mode ends. Builds never read it.

The controls are shown automatically the first time the package loads. If one is
missing (Unity's toolbar API is internal and may change), right-click the toolbar
and enable **MyToolz -> BootLauncher**, **BootCurrentScene** and **SceneNavigator**.
Hidden controls stay hidden: each is shown automatically only once per machine.
